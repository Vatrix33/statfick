using Statventure.Audio;
using Statventure.Editor.Preview;
using Statventure.Editor.Session;
using Statventure.Graphics;
using Statventure.Imaging;
using Statventure.Packs;
using Statventure.Platform;
using Statventure.Records;
using Statventure.Tests.Storage;
using Statventure.UI;
using Statventure.UI.Dom;
using Statventure.UI.Paint;
using CanvasRenderer = Statventure.Canvas.Canvas;

namespace Statventure.Tests.Editor.Preview;

using PreviewPart = Statventure.Editor.Preview.PreviewPart;

/// <summary>
/// The editor with its preview connected as the program connects it, but silent and windowless: the repo's packs (the
/// dev setup) with a scratch mod open over them, the player's folder a scratch one (the editor's <c>editor/</c> files
/// there), the sound an audio system with no device and no thread whose output the test renders itself (nothing reaches
/// a device: no test can be heard), the 3D view on the tests' headless GPU when one is given.
/// </summary>
sealed class PreviewEditor : IDisposable
{
    /// <summary>The offline sound's rate (as the reload tests').</summary>
    public const float Rate = 22050;

    public PreviewEditor(Action<PackWriter> mod, bool sound = true, GpuDevice? gpu = null, float width = 1280, float height = 900, float scale = 1,
        string? playerFolder = null)
        : this(file =>
        {
            var writer = new PackWriter();
            mod(writer);
            writer.Write(file);
        }, sound, gpu, width, height, scale, playerFolder)
    {
    }

    /// <summary>The editor over the mod <paramref name="pack"/> (a pack file's bytes, written once for many tests).</summary>
    public PreviewEditor(byte[] pack, bool sound = true, GpuDevice? gpu = null, float width = 1280, float height = 900, float scale = 1, string? playerFolder = null)
        : this(file => File.WriteAllBytes(file, pack), sound, gpu, width, height, scale, playerFolder)
    {
    }

    PreviewEditor(Action<string> write, bool sound, GpuDevice? gpu, float width, float height, float scale, string? playerFolder)
    {
        Editor = new EditorHarness(width, height, scale);
        Session.PlayerFolder = playerFolder ?? Folder["player"];
        Mod = Folder["player/mods/test.pak"];
        Directory.CreateDirectory(Path.GetDirectoryName(Mod)!);
        write(Mod);
        Session.Open(Mod);
        Audio = sound ? Offline() : null;
        Views = gpu is not null ? new SurfaceViews(Editor.Document) : null;
        Gpu = gpu;
        Preview.Connect(gpu, Views, Audio);
        Editor.Frame();
    }

    public StorageFolder Folder { get; } = new();
    public EditorHarness Editor { get; }
    public string Mod { get; }
    public AudioSystem? Audio { get; }
    public SurfaceViews? Views { get; }
    public GpuDevice? Gpu { get; }
    public EditorSession Session => Editor.Session;
    public PreviewPart Preview => Editor.View.Part<PreviewPart>();
    public UiDocument Document => Editor.Document;

    /// <summary>A pack's bytes as <paramref name="fill"/> makes it (compressed once: a large model takes seconds).</summary>
    public static byte[] PackBytes(Action<PackWriter> fill)
    {
        var writer = new PackWriter();
        fill(writer);
        var file = Path.Combine(Path.GetTempPath(), $"statventure-preview-{Guid.NewGuid():N}.pak");
        try
        {
            writer.Write(file);
            return File.ReadAllBytes(file);
        }
        finally
        {
            File.Delete(file);
        }
    }

    /// <summary>An audio system on a graph of its own, no device, no thread: only what the test renders is made, and never played.</summary>
    public static AudioSystem Offline(MusicLibrary? music = null) => new(new AudioGraph(Rate, 2), music ?? MusicLibrary.Empty);

    /// <summary><paramref name="quanta"/> quanta of <paramref name="system"/>'s output, interleaved left and right (the audio thread's steps, run here).</summary>
    public static float[] Render(AudioSystem system, int quanta)
    {
        var bus = new AudioBus(2, AudioGraph.RenderQuantumFrames);
        var result = new List<float>();
        for (var q = 0; q < quanta; q++)
        {
            system.BeforeQuantum();
            system.Graph.RenderQuantum(bus);
            result.AddRange(bus.Channel(0).Data);
            result.AddRange(bus.Channel(1).Data);
        }
        return [.. result];
    }

    /// <summary>The entry at <paramref name="path"/> selected, as the list selects it, and a frame.</summary>
    public EditorEntry Select(string path)
    {
        var entry = Session.Entry(path) ?? throw new InvalidOperationException($"no entry {path}");
        Session.Select(entry);
        Editor.Frame();
        return entry;
    }

    /// <summary>A new value for the entry at <paramref name="path"/> committed, as the form commits one, and a frame.</summary>
    public void Commit<T>(string path, T value) where T : notnull
    {
        var entry = Session.Entry(path)!;
        Assert.True(Session.Commit(entry, RecordDraft.From(value)));
        Editor.Frame();
    }

    /// <summary>The entry's draft changed so it doesn't build (a required field emptied), and a frame.</summary>
    public void Break(string path, string field)
    {
        var entry = Session.Entry(path)!;
        Assert.True(Session.Commit(entry, entry.Draft!.With(field, DraftValue.Missing)));
        Assert.False(entry.Builds);
        Editor.Frame();
    }

    /// <summary>A control of the preview by its <c>data-preview</c> name.</summary>
    public UiElement Control(string name) =>
        Document.QuerySelector($".preview-part [data-preview={name}]") ?? throw new InvalidOperationException($"the preview has no control {name}");

    /// <summary>A key pressed on a control of the preview (it focused first by a press).</summary>
    public void Key(UiElement control, string key, int times = 1)
    {
        if (Document.FocusedElement != control)
            Editor.Focus(control);
        for (var i = 0; i < times; i++)
            Editor.Key(key, key);
    }

    /// <summary>The preview's note ("" when there is nothing to say).</summary>
    public string Note => Document.QuerySelector(".preview-part .preview-note")?.TextContent ?? "";

    /// <summary>The whole document painted with the 3D views shown (device pixels of the harness's viewport).</summary>
    public RgbaImage Paint(int width, int height)
    {
        var gpu = Gpu ?? throw new InvalidOperationException("no GPU");
        using var painter = new UiPainter(Document) { SurfaceTexture = Views!.Texture };
        using var canvas = new CanvasRenderer(gpu);
        using var target = GpuImage.CreateRenderTarget(gpu, width, height);
        var frame = gpu.BeginFrame();
        Views.Update(frame);
        canvas.Begin(width, height);
        painter.Paint(canvas);
        canvas.Render(frame, target);
        gpu.EndFrame(frame);
        gpu.WaitIdle();
        return new RgbaImage(width, height, target.ReadPixels());
    }

    /// <summary>The 3D views drawn as the host draws them before painting (their pictures made), the GPU waited for.</summary>
    public void DrawViews()
    {
        var gpu = Gpu ?? throw new InvalidOperationException("no GPU");
        var frame = gpu.BeginFrame();
        Views!.Update(frame);
        gpu.EndFrame(frame);
        gpu.WaitIdle();
    }

    public void Dispose()
    {
        Editor.Dispose();
        Audio?.Dispose();
        Folder.Dispose();
    }
}

static class PreviewEditorExtensions
{
    /// <summary>The element focused, as a press focuses it (no click: a press on a range would move its thumb).</summary>
    public static void Focus(this EditorHarness editor, UiElement element)
    {
        element.ScrollIntoView(ScrollAlign.Center);
        editor.Frame();
        editor.Document.RunScript(() => element.Focus());
        editor.Frame();
    }
}

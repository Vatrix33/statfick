// The sandbox's figure mode (GM2-P14; Docs/Game/Hero.md, "How it works", step 3a): a figure in a look playing its clips
// in turn in the engine's own view, the playing view the user approves before the game's screens exist. The view fills
// the window (a surface element, as the game's screens show their figures), at the window's size and scale. Keys:
// Space plays and pauses; Right and Left step a frame (1/60 s) and pause; Down and Up go to the next and the previous
// clip's start; A and D turn the figure 15° (the camera stays the view's); 1–5 show the views front, three-quarter,
// side, back and close-up; F11 full screen; Escape quits. A caption at the top left names the clip and its time.
// --dump writes frames instead and quits: one cycle of every clip in turn at --fps (30 by default), or the --times
// given, each frame-NNNN.png with frames.txt (each frame's time, clip and clip time), and with --scene-file the
// frames' scenes as Blender's harness renders them (Tools/Models/humanoid/render.py --scene renders that file); with
// --label (GM2 step 4b) each frame names its clip, its time and the view in the caption (otherwise a dump is the view's
// picture alone). The clips in turn are the library's, or those --play names (FigurePlay).
using System.Globalization;
using Statventure.Core;
using Statventure.Graphics;
using Statventure.Platform;
using Statventure.Text;
using Statventure.UI;
using Statventure.UI.Dom;

namespace Statventure.Sandbox.Figure;

/// <summary>How the figure mode runs: in a window to watch, or writing frames.</summary>
public sealed record FigureRun
{
    /// <summary>The folder for --dump (null: play in the window).</summary>
    public string? Dump { get; init; }
    public double Fps { get; init; } = 30;
    /// <summary>The playing times to dump (null: one cycle at <see cref="Fps"/>).</summary>
    public IReadOnlyList<double>? Times { get; init; }
    /// <summary>Where to write the dumped frames' scenes (render_scenes.py's format), or null.</summary>
    public string? SceneFile { get; init; }
    /// <summary>The fonts' folder for the caption (null: no caption).</summary>
    public string? Fonts { get; init; }
    /// <summary>--label: a dump's frames name the clip, its time and the view in a caption (the window's caption without the keys' line).</summary>
    public bool Label { get; init; }
    public bool Visible { get; init; } = true;
    public string? GameFolder { get; init; }
    public string? LogName { get; init; }
    /// <summary>--capture: the window's frame N saved there, then it quits.</summary>
    public string? Capture { get; init; }
    public int CaptureFrame { get; init; } = 30;
    /// <summary>The window's size when it isn't the picture's (the tests: a window a compositor sized otherwise).</summary>
    public (int Width, int Height)? WindowSize { get; init; }
}

public sealed class FigureSandbox(FigureSpec spec, FigureRun run) : Application(new ApplicationOptions
{
    Window = new WindowOptions
    {
        Title = $"Statventure: figure mode ({Path.GetFileName(spec.Body)})", Width = run.WindowSize?.Width ?? spec.Width, Height = run.WindowSize?.Height ?? spec.Height,
        Visible = run.Visible,
        // A picture smaller than the engine's usual window minimum (320 × 240) keeps its size.
        MinWidth = Math.Min(320, spec.Width), MinHeight = Math.Min(240, spec.Height),
    },
    LogName = run.LogName,
    GameFolder = run.GameFolder,
    // A tool: no splash, no sound. A dump draws as fast as it can (and a hidden window's surface, which a compositor
    // doesn't pace, would hold a waiting present for ever: Docs/Editor.md, "How it works", step 3).
    Splash = false,
    Audio = false,
    VSync = run.Dump is null,
})
{
    static readonly Key[] ControlKeys = [Key.Escape, Key.Space, Key.Right, Key.Left, Key.Down, Key.Up, Key.A, Key.D, Key.D1, Key.D2, Key.D3, Key.D4, Key.D5];

    UiHost _host = null!;
    UiDocument _document = null!;
    UiElement? _caption;
    FigureView _figure = null!;
    FigureControls _controls = null!;
    double _clock;
    readonly Queue<(double Time, string File)> _dumps = new();
    readonly List<string> _index = [];
    readonly List<object> _scenes = [];
    Task<string>? _dumping;

    /// <summary>The figure's scene (for the tests: the same view the window shows).</summary>
    public FigureView Figure => _figure;

    protected override void Load()
    {
        var fonts = new FontLibrary();
        // A caption in the window, and on a dump's frames only when asked (--label): a dump is otherwise the view's picture alone.
        var captioned = run.Fonts is not null && (run.Dump is null || run.Label);
        if (captioned)
            fonts.AddFile(Path.Combine(run.Fonts!, "Lato-Regular.ttf"), "Lato", 400);
        _document = new UiDocument(fonts);
        _document.AddStylesheet($$"""
            html { background: {{spec.Look.Background}}; }
            body { margin: 0; }
            surface { position: absolute; inset: 0; display: block; width: 100%; height: 100%; }
            .caption { position: absolute; left: 8px; top: 8px; padding: 3px 8px; border-radius: 3px; font: 12px/16px Lato; color: #f3e8cc; background: rgba(18, 13, 10, 0.72); white-space: pre; }
            """, "figure");
        Markup.SetInnerHtml(_document.Body, captioned ? """<surface></surface><div class="caption"></div>""" : "<surface></surface>");
        _document.Update(0);
        var surface = _document.AllElements().Single(e => e.Tag == "surface");
        _caption = _document.AllElements().SingleOrDefault(e => e.HasClass("caption"));
        _host = new UiHost(Window, Gpu, _document);
        _figure = new FigureView(Gpu, spec);
        _controls = new FigureControls(_figure);
        _host.Views.Show(surface, _figure.View);
        if (run.Dump is { } dump)
        {
            Directory.CreateDirectory(dump);
            var times = run.Times ?? Enumerable.Range(0, Math.Max(1, (int)Math.Ceiling(_figure.Clips.Cycle * run.Fps - 1e-9))).Select(k => k / run.Fps).ToList();
            for (var k = 0; k < times.Count; k++)
                _dumps.Enqueue((times[k], $"frame-{k:D4}.png"));
            Log.Info("Figure", $"dumping {times.Count} frames of {spec.Body} ({_figure.Clips.Clips.Count} clips, {_figure.Clips.Cycle:0.###} s) in view {spec.View} into {dump}");
        }
        else
            Log.Info("Figure", $"{spec.Body}: {_figure.Clips.Clips.Count} clips ({string.Join(", ", _figure.Clips.Clips.Select(c => c.Name))}), look {spec.Look.Name}, view {spec.View}");
    }

    protected override void Update(in GameTime time)
    {
        if (run.Dump is not null)
        {
            Dump();
            return;
        }
        foreach (var key in ControlKeys)
            if (Input.WasPressed(key))
                _controls.Press(key);
        if (_controls.Quit)
            Quit();
        if (Input.WasPressed(Key.F11))
            Window.Fullscreen = !Window.Fullscreen;
        _controls.Advance(time.Delta);
        if (_caption is not null)
            _caption.TextContent = $"{_figure.Describe()} · {_figure.ViewName} · turn {_figure.Turn:0}°{(_controls.Playing ? "" : " · paused")}\n"
                + "Space play/pause · ←/→ frame · ↑/↓ clip · A/D turn · 1–5 views · Esc quit";
        _clock += time.Delta;
        _host.Update(_clock);
        if (run.Capture is not null && time.Frame == run.CaptureFrame)
            CaptureNextFrame(run.Capture).ContinueWith(_ => Quit());
    }

    /// <summary>
    /// --dump: each frame posed, drawn and captured before the next; with the last, the index and the scenes are written
    /// and it quits (that frame drawn and saved first, as a capture's run ends).
    /// </summary>
    void Dump()
    {
        if (_dumping is { IsCompleted: false })
        {
            _host.Update(_clock);
            return;
        }
        if (_dumps.Count == 0)
            return;
        if (Window.FramebufferWidth != spec.Width || Window.FramebufferHeight != spec.Height)
        {
            // A compositor that sizes windows itself (cage gives a window its whole output): the frames would be of
            // another size and framing. play-figure.sh dumps in a cage whose output is the size.
            Log.Error("Figure", $"the window is {Window.FramebufferWidth}x{Window.FramebufferHeight}, not {spec.Width}x{spec.Height}: no frames dumped");
            Environment.ExitCode = 1;
            _dumps.Clear();
            Quit();
            return;
        }
        var (at, file) = _dumps.Dequeue();
        _figure.Seek(at);
        var (clip, clipTime) = _figure.Clips.At(at);
        _index.Add(string.Create(CultureInfo.InvariantCulture,
            $"{file} {at:0.######} {(clip < 0 ? "-" : _figure.Clips.NameOf(clip))} {clipTime:0.######}"));
        if (_caption is not null)
            _caption.TextContent = Label(_figure);
        if (run.SceneFile is { } path)
            _scenes.Add(_figure.SceneOf(Path.GetFileNameWithoutExtension(file), Path.GetDirectoryName(Path.GetFullPath(path))!, _document.Zoom));
        // Each frame a step on the document's clock (its own, as the cutscene mode's dump keeps).
        _clock += 1.0 / 60;
        _host.Update(_clock);
        _dumping = CaptureNextFrame(Path.Combine(run.Dump!, file));
        if (_dumps.Count > 0)
            return;
        File.WriteAllLines(Path.Combine(run.Dump!, "frames.txt"), _index);
        if (run.SceneFile is { } sceneFile)
        {
            var folder = Path.GetDirectoryName(Path.GetFullPath(sceneFile))!;
            File.WriteAllText(sceneFile, FigureView.SceneFile(_scenes, folder, folder));
        }
        Log.Info("Figure", $"dumped {_index.Count} frames into {run.Dump}");
        Quit();
    }

    /// <summary>A dumped frame's caption (--label): the clip (with the one laid over it), its time in it and the view.</summary>
    public static string Label(FigureView figure) => $"{figure.Describe()} · {figure.ViewName}";

    protected override void Render(Frame frame, GpuImage target, in GameTime time) => _host.Render(frame, target);

    protected override void Unload()
    {
        _host.Dispose();
        _figure.Dispose();
    }
}

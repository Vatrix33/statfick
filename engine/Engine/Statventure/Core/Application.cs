using System.Diagnostics;
using Brutal.VulkanApi;
using Statventure.Audio;
using Statventure.Graphics;
using Statventure.Imaging;
using Statventure.Packs;
using Statventure.Platform;

namespace Statventure.Core;

public sealed record ApplicationOptions
{
    public WindowOptions Window { get; init; } = new();
    public bool VSync { get; init; } = true;
    /// <summary>Where the log file goes (null: console only).</summary>
    public string? LogFile { get; init; }
    /// <summary>Whether to open the sound output (<see cref="Application.Audio"/>).</summary>
    public bool Audio { get; init; } = true;
    /// <summary>
    /// The development folder (Docs/Assets.md, Q7, S3-5): loose entries in their own forms (a record as
    /// its JSON form at <c>&lt;path&gt;.json</c>) mounted on top of the game's data and re-read on Reload; the
    /// game's command line fills it. Null: the folder <c>$STATVENTURE_DEV_FOLDER</c> names, if it is set;
    /// else none. A folder named that doesn't exist stops the start.
    /// </summary>
    public string? DevelopmentFolder { get; init; }
}

/// <summary>Time for one update.</summary>
public readonly record struct GameTime(double Total, double Delta, long Frame);

/// <summary>
/// The engine's main loop: one window, the GPU device and the window's swapchain. Subclasses load
/// their content, update once per frame and record their drawing into the frame's command list.
/// </summary>
public abstract class Application : IDisposable
{
    string? _capturePath;
    TaskCompletionSource<string>? _captureDone;

    public ApplicationOptions Options { get; }
    public Window Window { get; private set; } = null!;
    public GpuDevice Gpu { get; private set; } = null!;
    public Swapchain Swapchain { get; private set; } = null!;
    public Input Input => Window.Input;
    /// <summary>The sound: music and effects (null when <see cref="ApplicationOptions.Audio"/> is off). Paused while the window is minimized.</summary>
    public AudioSystem? Audio { get; private set; }
    /// <summary>
    /// The game's data (Docs/Assets.md, "The virtual file system"; Q3): one file system for the whole
    /// application, mounted when <see cref="Run"/> starts, before the window, the sound and
    /// <see cref="Load"/> (see <see cref="DataMounts"/>). Loaders are given it.
    /// </summary>
    public VirtualFileSystem Data { get; private set; } = null!;
    public bool Running { get; private set; }

    /// <summary>The area packs in <c>packs/</c>, in the order they mount (S3-4); each mounts only if it is there.</summary>
    public static IReadOnlyList<string> AreaPacks { get; } = ["models", "audio", "ui", "world", "data"];

    /// <summary>The environment variable naming the development folder when the options don't (S3-5).</summary>
    public const string DevelopmentFolderVariable = "STATVENTURE_DEV_FOLDER";

    /// <summary>
    /// What <see cref="Data"/> mounts, bottom to top (Q2, Q27, S3-4, S3-5): the area packs of
    /// <c>packs/</c> in <paramref name="gameFolder"/> (beside the executable; each if it is there, other
    /// files ignored, no folder meaning no packs), then its <c>classes/</c> folder at <c>classes</c> if there is
    /// one (so a mod or the development folder can override a class), then the development folder
    /// (<see cref="ApplicationOptions.DevelopmentFolder"/>, else the folder <paramref name="environment"/>
    /// gives for <see cref="DevelopmentFolderVariable"/>). Mods come in between with step 9.
    /// </summary>
    internal static List<Mount> DataMounts(ApplicationOptions options, string gameFolder, Func<string, string?> environment)
    {
        var mounts = new List<Mount>();
        var packs = Path.Combine(gameFolder, "packs");
        foreach (var area in AreaPacks)
            if (File.Exists(Path.Combine(packs, area + ".pak")))
                mounts.Add(Mount.Pack(Path.Combine(packs, area + ".pak")));
        var classes = Path.Combine(gameFolder, "classes");
        if (Directory.Exists(classes))
            mounts.Add(Mount.Folder(classes, "classes"));
        var development = options.DevelopmentFolder ?? (environment(DevelopmentFolderVariable) is { Length: > 0 } named ? named : null);
        if (development is not null)
            mounts.Add(Mount.Folder(Path.GetFullPath(development)));
        return mounts;
    }

    static List<Mount> DataMounts(ApplicationOptions options) =>
        DataMounts(options, AppContext.BaseDirectory, Environment.GetEnvironmentVariable);

    protected Application(ApplicationOptions options)
    {
        Options = options;
    }

    protected virtual void Load() { }
    /// <summary>
    /// Once per frame; while the window is minimized also at the wake-ups asked for with
    /// <see cref="Platform.Window.RequestWakeUp"/> (and on events), without drawing.
    /// </summary>
    protected virtual void Update(in GameTime time) { }
    /// <summary>Draws the frame into <paramref name="target"/> (the swapchain image).</summary>
    protected abstract void Render(Frame frame, GpuImage target, in GameTime time);
    protected virtual void Unload() { }

    /// <summary>Saves the next presented frame as a PNG; the task finishes with the path once written.</summary>
    public Task<string> CaptureNextFrame(string path)
    {
        _capturePath = Path.GetFullPath(path);
        _captureDone = new TaskCompletionSource<string>();
        return _captureDone.Task;
    }

    /// <summary>Stops the loop after the current frame.</summary>
    public void Quit() => Running = false;

    public void Run()
    {
        NativeLibraries.Install();
        if (Options.LogFile is { } logFile)
            Log.OpenFile(logFile);
        try
        {
            // The data first: a damaged pack stops the start before a window opens.
            var mounts = DataMounts(Options);
            foreach (var mount in mounts)
                Log.Info("Data", mount.ToString());
            Data = new VirtualFileSystem(mounts);
            Window = Window.Create(Options.Window);
            Gpu = GpuDevice.Create(new GpuDeviceOptions { ApplicationName = Options.Window.Title });
            Swapchain = new Swapchain(Gpu, Window, Options.VSync);
            if (Options.Audio)
                Audio = new AudioSystem();
            Load();
            Running = true;
            var clock = Stopwatch.StartNew();
            var last = 0.0;
            long frames = 0;
            var pausedAudio = false;
            while (Running && !Window.CloseRequested)
            {
                Window.PollEvents();
                if (Window.IsMinimized)
                {
                    // All sound pauses while minimized and goes on where it was (step 7's decision).
                    if (Audio is { Paused: false } audio)
                    {
                        audio.Pause();
                        pausedAudio = true;
                    }
                    // No frames while minimized, but updates go on (a hidden document's tasks and its
                    // throttled timers, Docs/Scripting.md "Hidden documents"); the loop sleeps until
                    // the next wake-up asked for (Window.RequestWakeUp) or an event.
                    Input.BeginFrame();
                    var hiddenNow = clock.Elapsed.TotalSeconds;
                    Update(new GameTime(hiddenNow, Math.Min(hiddenNow - last, 0.25), frames));
                    last = hiddenNow;
                    if (!Running || Window.CloseRequested)
                        break;
                    if (Window.TakeWakeUp() is { } wait)
                        Window.WaitEvents(wait);
                    else
                        Window.WaitEvents();
                    continue;
                }
                if (pausedAudio)
                {
                    Audio?.Resume();
                    pausedAudio = false;
                }
                Input.BeginFrame();
                var now = clock.Elapsed.TotalSeconds;
                var time = new GameTime(now, Math.Min(now - last, 0.25), frames);
                last = now;
                Update(time);

                var frame = Gpu.BeginFrame();
                var image = Swapchain.Acquire(frame);
                if (image is { } target)
                {
                    Render(frame, target.Image, time);
                    GpuBuffer? readback = null;
                    var capture = _capturePath;
                    if (capture is not null && target.Image.Usage.HasFlag(VkImageUsageFlags.TransferSrcBit))
                    {
                        readback = GpuBuffer.Create(Gpu, (ulong)(target.Image.Width * target.Image.Height * 4), 0, MemoryKind.Readback);
                        frame.Commands.Transition(target.Image, VkImageLayout.TransferSrcOptimal);
                        frame.Commands.CopyImageToBuffer(target.Image, readback, 0);
                    }
                    Gpu.EndFrame(frame, image);
                    if (readback is not null && capture is not null)
                        SaveCapture(target.Image, readback, capture);
                }
                else
                    Gpu.EndFrame(frame);
                frames++;
            }
            Gpu.WaitIdle();
            Unload();
        }
        catch (Exception exception)
        {
            Log.Error("Application", exception);
            throw;
        }
        finally
        {
            Dispose();
        }
    }

    void SaveCapture(GpuImage image, GpuBuffer readback, string path)
    {
        Gpu.WaitFor(Gpu.SubmittedValue);
        var pixels = new byte[image.Width * image.Height * 4];
        readback.Read<byte>(pixels);
        readback.Dispose();
        if (Formats.IsBgra(image.Format))
            for (var i = 0; i < pixels.Length; i += 4)
                (pixels[i], pixels[i + 2]) = (pixels[i + 2], pixels[i]);
        for (var i = 3; i < pixels.Length; i += 4)
            pixels[i] = 255;
        ImageFile.SavePng(path, new RgbaImage(image.Width, image.Height, pixels));
        Log.Info("Application", "captured " + path);
        _capturePath = null;
        _captureDone?.TrySetResult(path);
    }

    public virtual void Dispose()
    {
        if (Gpu is not null)
        {
            Gpu.WaitIdle();
            Swapchain?.Dispose();
            Gpu.Dispose();
        }
        Audio?.Dispose();
        Audio = null;
        Window?.Dispose();
        Data?.Dispose();
        Log.CloseFile();
        Gpu = null!;
        Window = null!;
        Data = null!;
        GC.SuppressFinalize(this);
    }
}

using System.Diagnostics;
using System.Runtime.ExceptionServices;
using Brutal.VulkanApi;
using Statventure.Audio;
using Statventure.Graphics;
using Statventure.Imaging;
using Statventure.Packs;
using Statventure.Platform;
using Statventure.Storage;

namespace Statventure.Core;

public sealed record ApplicationOptions
{
    public WindowOptions Window { get; init; } = new();
    public bool VSync { get; init; } = true;
    /// <summary>
    /// The program's name for its logs (Docs/Packaging.md, "Logs"; M12-8, Q21): <c>stat-fick</c> for the game (and the
    /// sandbox standing in for it), <c>statventure-editor</c> for the editor. With a name, <see cref="Application.Run"/>
    /// writes the run's log in the player's game folder's <c>logs/</c> (<see cref="LogFiles"/>), and a crash writes a
    /// crash file there and its message on the terminal (<see cref="Application.Crash"/>). Null, the default: the console
    /// only, no files, nothing on the terminal (tests' applications write none unless they ask: the programs set it on
    /// their own path only, the editor in its public constructor).
    /// </summary>
    public string? LogName { get; init; }
    /// <summary>Whether to open the sound output (<see cref="Application.Audio"/>).</summary>
    public bool Audio { get; init; } = true;
    /// <summary>
    /// The development folder (Docs/Assets.md, Q7, S3-5): loose entries in their own forms (a record as
    /// its JSON form at <c>&lt;path&gt;.json</c>) mounted on top of the game's data and re-read on Reload; the
    /// game's command line fills it. Null: the folder <c>$STATVENTURE_DEV_FOLDER</c> names, if it is set;
    /// else none. A folder named that doesn't exist stops the start.
    /// </summary>
    public string? DevelopmentFolder { get; init; }
    /// <summary>
    /// The player's game folder (Docs/Assets.md, "The game folder"; S9-B): settings, save slots and mods,
    /// <see cref="Application.Storage"/>. Null: this machine's (<see cref="Storage.GameFolder.ForThisMachine"/>).
    /// </summary>
    public string? GameFolder { get; init; }
    /// <summary>
    /// Whether <see cref="Application.Data"/> holds the game's area packs only (Docs/Editor.md, step 3; the editor,
    /// which shows one pack over the game's packs, P6): no <c>classes/</c>, no mods folder, no development folder
    /// (neither <see cref="DevelopmentFolder"/> nor <c>$STATVENTURE_DEV_FOLDER</c>), so nothing of the player's can
    /// stop the start; Reload re-reads the same. False, the default: the game's data (<c>Application.DataMounts</c>).
    /// The game doesn't set it.
    /// </summary>
    public bool GamePacksOnly { get; init; }
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
    /// <summary>
    /// The player's game folder (Docs/Assets.md, "The game folder"; S9-B): settings, save slots and mods,
    /// made when <see cref="Run"/> starts (before <see cref="Data"/>, which mounts its mods) for
    /// <see cref="ApplicationOptions.GameFolder"/>, else this machine's. When the application ends, after
    /// <see cref="Unload"/> (where the window's document unloads) or after a failed start or loop, what is
    /// pending is written (<see cref="GameStorage.Flush"/>) and it takes no more.
    /// </summary>
    public GameStorage Storage { get; private set; } = null!;
    public bool Running { get; private set; }

    bool _remounted, _musicFailed;

    /// <summary>The application whose <see cref="Run"/> is going on (the first, if several are), which a crash on another thread is handed to.</summary>
    static Application? s_running;
    static int s_crashHandlerSet;
    readonly object _crashGate = new();
    readonly ManualResetEventSlim _crashTaken = new();
    CrashReport? _crash;
    /// <summary>Another thread's crash, for the main loop to take (<see cref="TakeCrashFromElsewhere"/>).</summary>
    Exception? _elsewhere;
    bool _ended, _ownsLog, _told;
    string? _logsFolder;

    /// <summary>
    /// The run's crash (Docs/Packaging.md, "Crashes"; M12-10), once there is one: its exception, where it happened, the one
    /// line naming the error and the crash file's path. The first crash of a run is the one; later ones are only logged.
    /// </summary>
    public CrashReport? Crash
    {
        get
        {
            lock (_crashGate)
                return _crash;
        }
    }

    /// <summary>
    /// The program's version and commit for its logs' and crash files' first lines (Q3): what the program file of the
    /// application's own class records (<see cref="ProgramInfo.Of"/>).
    /// </summary>
    ProgramInfo ThisProgram => _program ??= ProgramInfo.Of(GetType().Assembly);
    ProgramInfo? _program;

    /// <summary>Where a crash's message goes when no window can show it (Q24): the terminal (standard error); tests catch it.</summary>
    internal Action<string> Terminal { get; init; } = text => Console.Error.WriteLine(text);

    /// <summary>How long another thread's crash waits for the main loop to take it before the process ends at once (the crash file written).</summary>
    internal TimeSpan CrashAnswerTime { get; init; } = TimeSpan.FromSeconds(10);

    /// <summary>The area packs in <c>packs/</c>, in the order they mount (S3-4); each mounts only if it is there.</summary>
    public static IReadOnlyList<string> AreaPacks { get; } = ["models", "audio", "ui", "world", "data"];

    /// <summary>The environment variable naming the development folder when the options don't (S3-5).</summary>
    public const string DevelopmentFolderVariable = "STATVENTURE_DEV_FOLDER";

    /// <summary>
    /// What <see cref="Data"/> mounts, bottom to top (Q2, Q8, Q27, S3-4, S3-5): the area packs of
    /// <c>packs/</c> in <paramref name="executableFolder"/> (each if it is there, other files ignored, no
    /// folder meaning no packs), then its <c>classes/</c> folder at <c>classes</c> if there is one (so a mod
    /// or the development folder can override a class), then the mods in <paramref name="modsFolder"/>
    /// (the player's game folder's <c>mods/</c>; mounted even when missing, so a mod put there later
    /// appears on Reload), then the development folder (<see cref="ApplicationOptions.DevelopmentFolder"/>,
    /// else the folder <paramref name="environment"/> gives for <see cref="DevelopmentFolderVariable"/>).
    /// With <see cref="ApplicationOptions.GamePacksOnly"/> the area packs only. Which packs and folders mount
    /// is decided here, once: Reload re-reads these.
    /// </summary>
    internal static List<Mount> DataMounts(ApplicationOptions options, string executableFolder, Func<string, string?> environment, string? modsFolder = null)
    {
        var mounts = new List<Mount>();
        var packs = Path.Combine(executableFolder, "packs");
        foreach (var area in AreaPacks)
            if (File.Exists(Path.Combine(packs, area + ".pak")))
                mounts.Add(Mount.Pack(Path.Combine(packs, area + ".pak")));
        if (options.GamePacksOnly)
            return mounts;
        var classes = Path.Combine(executableFolder, "classes");
        if (Directory.Exists(classes))
            mounts.Add(Mount.Folder(classes, "classes"));
        if (modsFolder is not null)
            mounts.Add(Mount.Mods(Path.GetFullPath(modsFolder)));
        var development = options.DevelopmentFolder ?? (environment(DevelopmentFolderVariable) is { Length: > 0 } named ? named : null);
        if (development is not null)
            mounts.Add(Mount.Folder(Path.GetFullPath(development)));
        return mounts;
    }

    /// <summary>The player's game folder for <paramref name="options"/> (S9-B): the option's, else this machine's.</summary>
    internal static string StorageFolder(ApplicationOptions options) => options.GameFolder ?? GameFolder.ForThisMachine();

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

    /// <summary>Stops the loop after the current frame (without asking <see cref="ConfirmClose"/>).</summary>
    public void Quit() => Running = false;

    /// <summary>
    /// Asked when the window's close was requested (the player closed the window: <see cref="Platform.Window.CloseRequested"/>),
    /// before the loop ends for it: before the next frame, and while the window is minimized after the update
    /// (Docs/Editor.md, step 3). True, the default, ends the loop as it always has; false keeps the application
    /// running and clears the request, so the next close asks again (the editor answers false while it shows its
    /// "unsaved changes" dialog, M11-17). An application that refused and then wants to end calls <see cref="Quit"/>
    /// or requests the close again. Asked once per request, on the loop's thread.
    /// </summary>
    protected virtual bool ConfirmClose() => true;

    /// <summary>Whether the loop ends for a close request: one is there and <see cref="ConfirmClose"/> agrees; a refused one is cleared.</summary>
    bool CloseAccepted()
    {
        if (!Window.CloseRequested)
            return false;
        if (ConfirmClose())
            return true;
        Window.CloseRequested = false;
        return false;
    }

    /// <summary>
    /// Reloads the game's data (Docs/Assets.md, step 9; S9-C), on the game's thread: <see cref="Data"/> mounts
    /// everything afresh (packs reopened and checked, <c>classes/</c> and the development folder rescanned, the
    /// mods folder listed again), the sound takes the new music (<see cref="AudioSystem"/>: the theme playing
    /// restarts if its music changed, S9-D), then <see cref="VirtualFileSystem.Reloaded"/>'s other listeners run
    /// (the game's systems reload what they read). Gives whether it worked. When the data can't be mounted it
    /// stays as it was; when only the music doesn't load, the data is the new one and the sound keeps the music it
    /// had; either is logged. The set of packs and folders is the one found at the start (a pack first put in
    /// <c>packs/</c> later needs a restart). A listener's exception isn't caught.
    /// </summary>
    public bool ReloadData()
    {
        if (Data is null)
            throw new InvalidOperationException("The data is reloaded only while the application runs");
        _remounted = _musicFailed = false;
        try
        {
            Data.Reload();
        }
        catch (Exception problem) when (!_remounted)
        {
            Log.Error("Data", $"not reloaded, the data stays as it was: {problem.Message}");
            return false;
        }
        Log.Info("Data", _musicFailed ? "reloaded, but not the music" : "reloaded");
        return !_musicFailed;
    }

    /// <summary>The first listener of <see cref="VirtualFileSystem.Reloaded"/>: the sound's music from the new data (S9-C, S9-D).</summary>
    void DataReloaded()
    {
        _remounted = true;
        if (Audio is not { } audio)
            return;
        MusicLibrary music;
        try
        {
            music = MusicLibrary.Load(Data);
        }
        catch (IOException problem)
        {
            Log.Error("Data", $"the music isn't reloaded, the sound keeps what it had: {problem.Message}");
            _musicFailed = true;
            return;
        }
        audio.Reload(music);
    }

    /// <summary>
    /// Runs the application: the player's game folder (with <see cref="ApplicationOptions.LogName"/>, the run's log in its
    /// <c>logs/</c>, its first lines the program, version and commit, the start time and the system), the data, the
    /// window, the device, the sound, <see cref="Load"/>, the loop, <see cref="Unload"/>, then the teardown
    /// (<see cref="Dispose"/>: the pending saves written, S9-B). A crash (Docs/Packaging.md, "Crashes"; M12-10), an
    /// exception out of any of that or an unhandled exception on another thread while it runs (<see cref="CrashedElsewhere"/>),
    /// writes the crash file first (with a log name), then the application is torn down, its message goes to the
    /// terminal (with a log name; Q24) and the exception leaves <see cref="Run"/>.
    /// </summary>
    public void Run()
    {
        NativeLibraries.Install();
        SetCrashHandler();
        Interlocked.CompareExchange(ref s_running, this, null);
        var crashed = false;
        try
        {
            // The player's game folder: the run's log in it first, then the storage, whose mods are part of the data (S9-B).
            var folder = StorageFolder(Options);
            if (Options.LogName is { } program)
                StartLog(folder, program);
            Storage = new GameStorage(folder);
            Log.Info("Storage", "game folder " + Storage.Folder);
            // The data first: a damaged pack stops the start before a window opens.
            var mounts = DataMounts(Options, AppContext.BaseDirectory, Environment.GetEnvironmentVariable, Storage.ModsFolder);
            foreach (var mount in mounts)
                Log.Info("Data", mount.ToString());
            Data = new VirtualFileSystem(mounts);
            Data.Reloaded += DataReloaded;
            Window = Window.Create(Options.Window);
            Gpu = GpuDevice.Create(new GpuDeviceOptions { ApplicationName = Options.Window.Title });
            Swapchain = new Swapchain(Gpu, Window, Options.VSync);
            if (Options.Audio)
                Audio = new AudioSystem(MusicLibrary.Load(Data));
            Load();
            Running = true;
            var clock = Stopwatch.StartNew();
            var last = 0.0;
            long frames = 0;
            var pausedAudio = false;
            while (Running && !CloseAccepted())
            {
                TakeCrashFromElsewhere();
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
                    if (!Running || CloseAccepted())
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
            crashed = true;
            Crashed(exception, "on the main thread");
            throw;
        }
        finally
        {
            try
            {
                Dispose();
            }
            catch (Exception exception)
            {
                // A teardown that fails is a crash too (after one, the crash file has the first and the log this one).
                crashed = true;
                Crashed(exception, "on the main thread");
                throw;
            }
            finally
            {
                Ended(crashed);
            }
        }
    }

    /// <summary>Opens the run's log in <paramref name="gameFolder"/>'s <c>logs/</c>; a log that can't be made is a warning, not a crash.</summary>
    void StartLog(string gameFolder, string program)
    {
        _logsFolder = LogFiles.Folder(gameFolder);
        try
        {
            LogFiles.Start(_logsFolder, program, ThisProgram, DateTimeOffset.Now);
            _ownsLog = true;
        }
        catch (Exception problem) when (problem is IOException or UnauthorizedAccessException)
        {
            Log.Warning("Application", $"no log file in {_logsFolder}: {problem.Message}");
        }
    }

    /// <summary>
    /// Records a crash: the run's first writes its crash file at once (with a log name; <see cref="LogFiles.CrashText"/>,
    /// the log's end being the lines before it), then logs the error and the file. A later crash of the same run is only
    /// logged. Gives the run's crash.
    /// </summary>
    CrashReport Crashed(Exception exception, string where)
    {
        lock (_crashGate)
        {
            if (_crash is { } first)
            {
                if (!ReferenceEquals(first.Exception, exception))
                    Log.Error("Application", $"another crash, {where}: {exception}");
                return first;
            }
            var time = DateTimeOffset.Now;
            string? file = null;
            if (_logsFolder is { } folder)
            {
                try
                {
                    file = LogFiles.WriteCrashFile(folder, time, LogFiles.CrashText(Options.LogName!, ThisProgram, time, where, exception, Log.Tail(), Log.FilePath));
                }
                catch (Exception problem) when (problem is IOException or UnauthorizedAccessException)
                {
                    Log.Error("Application", $"the crash file couldn't be written in {folder}: {problem.Message}");
                }
            }
            Log.Error("Application", $"crashed {where}: {exception}");
            if (file is not null)
                Log.Info("Application", "crash file " + file);
            return _crash = new CrashReport(exception, where, time, file);
        }
    }

    /// <summary>
    /// Sets .NET's handler for exceptions no code catches on threads other than the main one (the audio's warm-up and
    /// room threads, the thread pool, any other), once for the process: <see cref="CrashedElsewhere"/>.
    /// </summary>
    static void SetCrashHandler()
    {
        if (Interlocked.Exchange(ref s_crashHandlerSet, 1) == 1)
            return;
        try
        {
            ExceptionHandling.SetUnhandledExceptionHandler(CrashedElsewhere);
        }
        catch (InvalidOperationException)
        {
            Log.Warning("Application", "another handler takes unhandled exceptions: a crash on another thread ends the program without a crash file");
        }
    }

    /// <summary>
    /// An exception no code caught on a thread other than the main one: true when the running application took it
    /// (<see cref="CrashedOnAnotherThread"/>), and the thread just ends; false with none running, and .NET ends the
    /// process as it does.
    /// </summary>
    internal static bool CrashedElsewhere(Exception exception)
    {
        var thread = Thread.CurrentThread;
        var where = thread.IsThreadPoolThread ? "on a thread-pool thread"
            : thread.Name is { Length: > 0 } name ? $"on the thread \"{name}\"" : "on an unnamed thread";
        return Volatile.Read(ref s_running) is { } application && application.CrashedOnAnotherThread(exception, where);
    }

    /// <summary>
    /// Another thread's crash (Docs/Packaging.md, "Crashes"): its crash file written at once (<see cref="Crashed"/>), then
    /// handed to the main loop, which ends the run in order (the teardown, the pending saves; <see cref="Run"/> throws
    /// it); the loop is woken if it sleeps (a minimized window). True once the main loop has taken it or the run has
    /// ended; false when the main loop doesn't take it within <see cref="CrashAnswerTime"/> (its message then goes to
    /// the terminal, and the process ends at once with the crash file written) or the run had already ended.
    /// </summary>
    internal bool CrashedOnAnotherThread(Exception exception, string where)
    {
        lock (_crashGate)
        {
            if (_ended)
                return false;
            Crashed(exception, where);
            _elsewhere ??= exception;
        }
        if (Window is not null)
        {
            try
            {
                Window.PostEmptyEvent();
            }
            catch (Exception)
            {
                // GLFW ended already (the teardown): nothing sleeps.
            }
        }
        if (_crashTaken.Wait(CrashAnswerTime))
            return true;
        TellTheTerminal();
        return false;
    }

    /// <summary>On the main thread: another thread's crash, if there is one, thrown here, so the run ends as for its own.</summary>
    void TakeCrashFromElsewhere()
    {
        if (Volatile.Read(ref _elsewhere) is { } exception)
        {
            _crashTaken.Set();
            ExceptionDispatchInfo.Throw(exception);
        }
    }

    /// <summary>
    /// The end of <see cref="Run"/>, after the teardown (even a failing one): a crash on another thread that the loop
    /// didn't take (it came after the loop, in <see cref="Unload"/> or the teardown) ends the run all the same; any thread
    /// waiting is let go; the crash's message goes to the terminal (Q24).
    /// </summary>
    void Ended(bool crashed)
    {
        Exception? untaken;
        lock (_crashGate)
        {
            _ended = true;
            untaken = crashed ? null : _elsewhere;
        }
        _crashTaken.Set();
        Interlocked.CompareExchange(ref s_running, null, this);
        TellTheTerminal();
        if (untaken is not null)
            ExceptionDispatchInfo.Throw(untaken);
    }

    /// <summary>The crash's message on the terminal (Q24; with a log name only), once.</summary>
    void TellTheTerminal()
    {
        string? text;
        lock (_crashGate)
        {
            text = Options.LogName is not null && !_told && _crash is { } crash ? crash.TerminalText(Options.Window.Title) : null;
            _told |= text is not null;
        }
        if (text is not null)
            Terminal(text);
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
        // The saves and settings the game asked for (in its Unload too, where the document unloads) are
        // written before the application ends (Q16, S9-B), the window already closed.
        Storage?.Dispose();
        Data?.Dispose();
        if (_ownsLog)
            Log.CloseFile();
        _ownsLog = false;
        Gpu = null!;
        Window = null!;
        Data = null!;
        Storage = null!;
        GC.SuppressFinalize(this);
    }
}

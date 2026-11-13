using Statventure.Core;
using Statventure.Game.Mods;
using Statventure.Game.Random;
using Statventure.Game.Screens.About;
using Statventure.Game.Screens.Menu;
using Statventure.Game.Screens.Picker;
using Statventure.Game.Screens.Settings;
using Statventure.Game.Screens.Sheet;
using Statventure.Game.Screens.Slots;
using Statventure.Game.Screens.Start;
using Statventure.Game.Session;
using Statventure.Game.Settings;
using Statventure.Game.Ui;
using Statventure.Graphics;
using Statventure.Packs;
using Statventure.Platform;
using Statventure.Storage;

namespace Statventure.Game;

/// <summary>
/// Stat Fick on the engine (Docs/Game/Foundation.md, "The program's flow" and "The seams"): the application class, whose
/// start sequence and wiring run the program's parts. <b>Before <c>Run</c></b> (<see cref="Create"/>): the per-user
/// folder's settings peeked (<see cref="GameSettings.Peek"/>: the window as the player left it, the mods' switches), the
/// engine's options made from them (<see cref="OptionsFor"/>: the window titled "Stat Fick", 1440 × 950 the first time,
/// at least 820 × 600, logging as <c>stat-fick</c>) and passed through <see cref="GameMods.ApplyTo"/>. <b>Then the engine's
/// start</b> (the log, the per-user folder, the data with the mods, the window, the device, the BRUTAL splash, the
/// sound), and at <c>Load</c> (<see cref="Compose"/>): <see cref="GameSettings.Load"/> on the engine's storage, the mods
/// given the settings, <see cref="GameSession"/>, <see cref="GameRandom.FromSystem"/> (its seed logged),
/// <see cref="GameUi.Load"/>, every screen attached, the three volumes put on the sound and the window's maximized and
/// full screen states on the window (both following <see cref="GameSettings.Changed"/>), the command <c>start-screen</c>,
/// then the command line's <c>--open</c> and <c>--about</c>. Each update keeps the window's state in the settings and
/// brings the window forward when a later start asked (<see cref="SingleInstance"/>); <c>Unload</c> leaves the open
/// character (saved), whether the run ends by Quit or by the window's close (which agrees at once: no question). No
/// theme is started (Q15), and the data is read at the start only (Q18: no Reload).
/// </summary>
public sealed class StatFickGame : Application
{
    /// <summary>The window's title (M12 Q3), the credits' and the crash window's name.</summary>
    public const string Title = "Stat Fick";

    /// <summary>The program's name for its logs (M12-8).</summary>
    public const string LogName = "stat-fick";

    /// <summary>The window the first time (Q19), as the old shell's (<c>main.cjs</c> 63–67).</summary>
    public const int FirstWidth = 1440, FirstHeight = 950;

    /// <summary>The window's smallest size, the old shell's.</summary>
    public const int MinWidth = 820, MinHeight = 600;

    /// <summary>The command every start runs once the screens are attached.</summary>
    public const string StartCommand = "start-screen";

    readonly GameOptions _options;
    readonly GameMods _mods;
    readonly IReadOnlyList<IGameScreen> _screens;
    (int Width, int Height) _windowed;
    int _forwardAsked;

    internal StatFickGame(ApplicationOptions options, GameOptions game, SettingsSnapshot snapshot, GameMods mods, IReadOnlyList<IGameScreen>? screens = null)
        : base(options)
    {
        _options = game;
        _mods = mods;
        _screens = screens ?? NewScreens();
        Snapshot = snapshot;
        _windowed = (options.Window.Width, options.Window.Height);
        CrashCapture = game.Capture;
    }

    /// <summary>
    /// The game for <paramref name="gameFolder"/>, the per-user folder (<see cref="GameFolder.ForThisMachine"/> for the
    /// program): its settings peeked, its options made from them and passed through the mods (before <c>Run</c>).
    /// </summary>
    public static StatFickGame Create(GameOptions options, string gameFolder)
    {
        var snapshot = GameSettings.Peek(gameFolder);
        var mods = new GameMods();
        var game = new StatFickGame(mods.ApplyTo(OptionsFor(snapshot, gameFolder), snapshot), options, snapshot, mods);
        if (options.CaptureSplash is { } splash)
            _ = game.CaptureNextFrame(splash);
        return game;
    }

    /// <summary>The engine's options for a start whose settings said <paramref name="settings"/> (before the mods').</summary>
    internal static ApplicationOptions OptionsFor(SettingsSnapshot settings, string gameFolder) => new()
    {
        Window = new WindowOptions
        {
            Title = Title,
            Width = settings.Window?.Width ?? FirstWidth,
            Height = settings.Window?.Height ?? FirstHeight,
            MinWidth = MinWidth,
            MinHeight = MinHeight,
        },
        LogName = LogName,
        GameFolder = gameFolder,
    };

    /// <summary>One of each screen, in the order they attach (Docs/Game/Foundation.md, "The seams").</summary>
    internal static IReadOnlyList<IGameScreen> NewScreens() =>
        [new StartScreen(), new SheetScreen(), new MenuScreen(), new AboutScreen(), new SettingsScreen(), new SlotsScreen(), new PickerScreen()];

    /// <summary>What the settings said before <c>Run</c>.</summary>
    public SettingsSnapshot Snapshot { get; }

    /// <summary>The parts, once <c>Load</c> has made them (null before).</summary>
    public GameParts? Parts { get; private set; }

    /// <summary>Tests: called with the run's settings as soon as they are loaded, before the rest of the start uses them.</summary>
    internal Action<GameSettings>? SettingsLoaded { get; init; }

    /// <summary>What kept this start from holding its per-user folder alone (<see cref="SingleInstance.Problem"/>), logged at <c>Load</c>; null when nothing did.</summary>
    internal string? SingleInstanceProblem { get; set; }

    /// <summary>The window as the game uses it (the engine's at <c>Load</c>; a test's in place of it).</summary>
    internal IGameWindow? GameWindow { get; set; }

    /// <summary>Where the volumes go (the engine's sound at <c>Load</c>, none without sound; a test's in place of it).</summary>
    internal IGameVolumes? Volumes { get; set; }

    /// <summary>
    /// A later start asked for the window (<see cref="SingleInstance.ForwardAsked"/>, from the listener's thread): it is
    /// brought forward at the next update; a loop asleep (minimized) is woken.
    /// </summary>
    public void AskForward()
    {
        Interlocked.Exchange(ref _forwardAsked, 1);
        if (Running)
            Window.PostEmptyEvent();
    }

    protected override void Load()
    {
        foreach (var problem in _options.Problems)
            Log.Warning("Game", $"the command line: {problem} (ignored)");
        if (SingleInstanceProblem is { } alone)
            Log.Warning("Game", $"one instance per player: {alone}");
        GameWindow ??= new EngineGameWindow(Window);
        Volumes ??= Audio is { } audio ? new AudioVolumes(audio) : null;
        Parts = Compose(Storage, Data);
    }

    /// <summary>The start sequence at <c>Load</c> ("Who calls whom"), on <paramref name="storage"/> and <paramref name="data"/> (the engine's).</summary>
    internal GameParts Compose(GameStorage storage, VirtualFileSystem data)
    {
        var settings = GameSettings.Load(storage);
        SettingsLoaded?.Invoke(settings);
        _mods.Attach(settings);
        var session = new GameSession(storage, settings);
        var random = GameRandom.FromSystem();
        Log.Info("Game", $"the run's random seed {random.Seed}");
        var ui = GameUi.Load(this, settings, data);
        var parts = new GameParts(settings, session, _mods, ui, random, this);
        Parts = parts;
        foreach (var screen in _screens)
            screen.Attach(parts);
        ApplyVolumes(settings);
        if (GameWindow is { } window)
        {
            if (settings.Window is { Maximized: true } && !window.Maximized)
                window.Maximize();
            if (settings.Fullscreen && !window.Fullscreen)
                window.Fullscreen = true;
        }
        settings.Changed += key => SettingChanged(settings, key);
        ui.Commands.Run(StartCommand);
        if (_options.OpenCommand is { } open)
            ui.Commands.Run(open);
        if (_options.About)
            ui.Commands.Run("about");
        return parts;
    }

    /// <summary>A setting changed: the volumes, full screen and the window's maximized state follow it.</summary>
    void SettingChanged(GameSettings settings, SettingKey key)
    {
        switch (key)
        {
            case SettingKey.MasterVolume or SettingKey.MusicVolume or SettingKey.EffectsVolume:
                ApplyVolumes(settings);
                break;
            case SettingKey.Fullscreen when GameWindow is { } window && window.Fullscreen != settings.Fullscreen:
                window.Fullscreen = settings.Fullscreen;
                break;
            case SettingKey.Window when GameWindow is { } window && !window.Fullscreen && settings.Window is { } state && state.Maximized != window.Maximized:
                if (state.Maximized)
                    window.Maximize();
                else
                    window.Restore();
                break;
        }
    }

    void ApplyVolumes(GameSettings settings) => Volumes?.Apply(settings.MasterVolume, settings.MusicVolume, settings.EffectsVolume);

    /// <summary>
    /// The window's state written back to the settings when it changed (Q19): its size while neither maximized, minimized
    /// nor full screen, and whether it is maximized (the size kept then is the one it is restored to). Full screen is a
    /// setting of its own.
    /// </summary>
    internal void KeepWindowState()
    {
        if (GameWindow is not { } window || Parts is not { } parts || window.Minimized || window.Fullscreen)
            return;
        if (!window.Maximized)
            _windowed = (window.Width, window.Height);
        // The settings change, and say so, only when the value does; this write is never applied back to the window, its
        // maximized state being the window's own at this moment (SettingChanged).
        parts.Settings.Window = new WindowState(_windowed.Width, _windowed.Height, window.Maximized);
    }

    /// <summary>A later start's request, taken on the game's thread.</summary>
    internal void TakeForwardRequest()
    {
        if (Interlocked.Exchange(ref _forwardAsked, 0) == 1 && GameWindow is { } window)
            Log.Info("Game", $"another start asked for the window: {window.BringForward()}");
    }

    protected override void Update(in GameTime time)
    {
        if (_options.Crash != CrashOnPurpose.None && time.Frame == 0)
            CrashOnPurposeNow(_options.Crash);
        TakeForwardRequest();
        KeepWindowState();
        Parts!.Ui.Update(time.Total);
        if (_options.Capture is { } capture && time.Frame == _options.Frames)
        {
            Log.Info("Game", $"window {Window.Width}x{Window.Height}, framebuffer {Window.FramebufferWidth}x{Window.FramebufferHeight}, scale {Window.ContentScale:0.###}, swapchain {Swapchain.Width}x{Swapchain.Height}");
            CaptureNextFrame(capture).ContinueWith(_ => Quit());
        }
    }

    /// <summary>The game's own crash, for the checks (<c>--crash</c>, <c>--crash-thread</c>): on the main thread, or on a thread of its own.</summary>
    static void CrashOnPurposeNow(CrashOnPurpose where)
    {
        if (where == CrashOnPurpose.MainThread)
            throw new InvalidOperationException("the game crashed on purpose (--crash)");
        new Thread(() => throw new InvalidOperationException("the game crashed on purpose on a thread of its own (--crash-thread)"))
        {
            Name = "Game crash",
            IsBackground = true,
        }.Start();
    }

    protected override void Render(Frame frame, GpuImage target, in GameTime time) => Parts!.Ui.Render(frame, target);

    protected override void Unload()
    {
        if (Parts is not { } parts)
            return;
        parts.Session.Leave();
        parts.Ui.Dispose();
    }
}

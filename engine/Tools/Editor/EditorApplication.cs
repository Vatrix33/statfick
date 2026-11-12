using Statventure.Core;
using Statventure.Editor.Browser;
using Statventure.Editor.Game;
using Statventure.Editor.Preview;
using Statventure.Editor.Session;
using Statventure.Editor.Settings;
using Statventure.Editor.Shell;
using Statventure.Graphics;
using Statventure.Platform;
using Statventure.Storage;
using Statventure.UI;

namespace Statventure.Editor;

/// <summary>
/// The editor's program: one window showing a document through a <see cref="UiHost"/>: the editor's view on its game
/// (P1, P4), or, while it knows no game, the "Where is Stat Fick?" screen (<see cref="GameScreen"/>, Q16; M12 step 6).
/// The game comes from the command line (<c>--repo</c>, <c>--game</c>), else from the folder remembered in
/// <c>editor/game.json</c>, else from the screen; Game folder… in the editor changes it (<see cref="GamePart"/>), the
/// session and view made anew. The window's title is the view's (before one, "Statventure Editor &lt;version&gt;", Q3), its
/// page zoom the view's. Its data is the game's packs only (<see cref="ApplicationOptions.GamePacksOnly"/>, P6: nothing of
/// the player's can stop its start); files dropped on the window open or import (P27); closing with unsaved work asks
/// first (M11-17, <see cref="ConfirmClose"/>); the page zoom and the window's last normal size are remembered in
/// <c>editor/shell.json</c> (P4, G6): read before the window opens, written when the editor closes. With a capture asked
/// for (<c>--capture</c>, Q20) it draws that many frames, writes the last and quits.
/// </summary>
public sealed class EditorApplication : Application
{
    readonly GameData? _given;
    readonly ShellSettings _shell;
    EditorSession? _session;
    EditorView? _view;
    GameScreen? _screen;
    UiHost? _host;
    GameData? _switchTo;
    int _width, _height;

    /// <summary>
    /// The editor as its program makes it: its window shown, its logs written (M12-8, <see cref="ApplicationOptions.LogName"/>),
    /// the BRUTAL splash first (step 5's, with a log name), on <paramref name="game"/> (null: the one it remembers, or the
    /// screen asks), drawing <paramref name="frames"/> frames after the splash into <paramref name="capture"/> and quitting
    /// when a capture is asked for (a crash's window then saved there too, <see cref="Application.CrashCapture"/>).
    /// </summary>
    public EditorApplication(GameData? game, string? playerFolder, string? capture = null, int frames = EditorArguments.DefaultFrames)
        : this(game, playerFolder, visible: true, logName: "statventure-editor")
    {
        CaptureFile = capture;
        CaptureFrames = frames;
        // A capture run nobody watches ends after a crash too: the crash window's first frame saved there (step 5's).
        CrashCapture = capture;
    }

    /// <summary>The editor with its window <paramref name="visible"/> or not (tests make theirs hidden), writing no log unless given <paramref name="logName"/>.</summary>
    internal EditorApplication(GameData? game, string? playerFolder, bool visible, string? logName = null)
        : this(game, ShellSettings.Read(playerFolder ?? GameFolder.ForThisMachine()), playerFolder, visible, logName)
    {
    }

    EditorApplication(GameData? game, ShellSettings shell, string? playerFolder, bool visible, string? logName)
        : base(OptionsFor(shell, playerFolder, visible) with { LogName = logName })
    {
        _given = game;
        _shell = shell;
        _width = Options.Window.Width;
        _height = Options.Window.Height;
    }

    /// <summary>
    /// The application's options: the window (step 4's: titled "Statventure Editor &lt;version&gt;" (Q3, the title before
    /// a view gives its own), 1280 × 800 unless <paramref name="shell"/> remembers a size, at least 800 × 500), the sound
    /// (the preview's, step 13), the player's folder, and the game's packs only for its data (P6).
    /// </summary>
    internal static ApplicationOptions OptionsFor(ShellSettings shell, string? playerFolder, bool visible = true) => new()
    {
        Window = new WindowOptions
        {
            Title = EditorView.TitleOf(EditorView.Version, null, unsaved: false),
            Width = shell.Width ?? ShellSettings.DefaultWidth,
            Height = shell.Height ?? ShellSettings.DefaultHeight,
            MinWidth = ShellSettings.MinWidth,
            MinHeight = ShellSettings.MinHeight,
            Visible = visible,
        },
        // The preview's sound (step 13).
        Audio = true,
        GameFolder = playerFolder,
        GamePacksOnly = true,
    };

    /// <summary>The session (tests), null while the screen shows.</summary>
    internal EditorSession? Session => _session;

    /// <summary>The view (tests), null while the screen shows.</summary>
    internal EditorView? View => _view;

    /// <summary>The "Where is Stat Fick?" screen while it shows (tests), else null.</summary>
    internal GameScreen? Screen => _screen;

    /// <summary>The file a capture is written into (<c>--capture</c>), or null for none.</summary>
    internal string? CaptureFile { get; init; }

    /// <summary>The frame the capture is taken at (<c>--frames</c>; 30, the sandbox's, unless given).</summary>
    internal int CaptureFrames { get; init; } = EditorArguments.DefaultFrames;

    /// <summary>Called after each update (tests drive the editor from it).</summary>
    internal Action<EditorApplication>? AfterUpdate { get; init; }

    /// <summary>Called at the end of <see cref="Load"/> (tests).</summary>
    internal Action<EditorApplication>? AfterLoad { get; init; }

    /// <summary>Called when the screen shows, before the first update (tests set its browser's places).</summary>
    internal Action<GameScreen>? ScreenShown { get; init; }

    protected override void Load()
    {
        Window.PathsDropped += paths => _view?.Part<Opener>().Drop(paths);
        if (_given is { } game)
        {
            LogGivenLibrary(game);
            ShowEditor(game);
        }
        else
        {
            var remembered = GameLookup.Remembered(Storage.Folder, EditorView.Version);
            if (remembered.Game is { } found)
                ShowEditor(found);
            else
                ShowScreen(remembered.Problem);
        }
        AfterLoad?.Invoke(this);
    }

    /// <summary>
    /// The game the command line named (<c>--game</c>, <c>--repo</c>) was found and its data library loaded before the run's
    /// log opened (a game the editor can't use is refused before any window), so the library's line, which the loader logs
    /// for a remembered game, is logged again now in the same words: both starts name it in the log (M12 step 8).
    /// </summary>
    static void LogGivenLibrary(GameData game)
    {
        if (game.Library is not { } file)
            return;
        try
        {
            var library = GameLibrary.Read(file);
            Log.Info("Editor", $"the game's data library {library.File}: {library.Version}{(library.Program.Commit is { } commit ? ", commit " + commit : "")}, {game.Types.ByName.Count} record kinds");
        }
        catch (Exception problem) when (problem is GameLibraryException or IOException or UnauthorizedAccessException)
        {
            Log.Warning("Editor", $"the game's data library {file} can't be read again: {problem.Message}");
        }
    }

    /// <summary>The editor's view on <paramref name="game"/>: what was shown before (the screen, or the view on another game) closed first.</summary>
    void ShowEditor(GameData game)
    {
        var zoom = _view?.ZoomLevel ?? _shell.ZoomLevel;
        CloseShown();
        Log.Info("Editor", game.DevSetup ? $"the dev setup: the repo's packs in {game.PacksFolder}" : $"the game in {game.Folder}");
        _session = new EditorSession(game) { PlayerFolder = Storage.Folder };
        var view = _view = new EditorView(_session);
        var host = _host = new UiHost(Window, Gpu, view.Document) { Audio = Audio };
        view.Part<PreviewPart>().Connect(Gpu, host.Views, Audio);
        view.ApplyZoom = level => host.PageZoom = level;
        view.ZoomLevel = zoom;
        view.TitleChanged += () => Window.Title = view.Title;
        view.Part<GamePart>().Chosen += chosen => _switchTo = chosen;
        Window.Title = view.Title;
    }

    /// <summary>The "Where is Stat Fick?" screen, saying <paramref name="problem"/> about the remembered folder.</summary>
    void ShowScreen(GameProblem? problem)
    {
        CloseShown();
        Log.Info("Editor", problem is null ? "no game's folder known: \"Where is Stat Fick?\"" : $"\"Where is Stat Fick?\": {problem.Text(EditorView.Version, remembered: true)}");
        var screen = _screen = new GameScreen(Storage.Folder, problem, EditorView.Version, OperatingSystem.IsWindows());
        screen.Chosen += chosen => _switchTo = chosen;
        // The page zoom remembered (the shell's, P4), so the screen reads as the editor will.
        _host = new UiHost(Window, Gpu, screen.Document) { PageZoom = (float)Math.Pow(1.2, _shell.ZoomLevel) };
        ScreenShown?.Invoke(screen);
    }

    /// <summary>Closes the screen or the view shown (its host first, once the device is done with it), and the session.</summary>
    void CloseShown()
    {
        if (_host is { } host)
        {
            Gpu.WaitIdle();
            host.Dispose();
            _host = null;
        }
        _view?.Dispose();
        _view = null;
        _session?.Dispose();
        _session = null;
        _screen?.Dispose();
        _screen = null;
    }

    protected override void Update(in GameTime time)
    {
        _host!.Update(time.Total);
        if (_switchTo is { } game)
        {
            // Chosen on the screen or from Game folder…: the view on that game, from this frame on.
            _switchTo = null;
            ShowEditor(game);
            _host!.Update(time.Total);
        }
        KeepNormalSize();
        if (CaptureFile is { } file && time.Frame == CaptureFrames)
            CaptureNextFrame(file).ContinueWith(_ => Quit());
        AfterUpdate?.Invoke(this);
    }

    protected override void Render(Frame frame, GpuImage target, in GameTime time) => _host!.Render(frame, target);

    /// <summary>Closing with unsaved work asks first (M11-17): the view's question, the loop going on until it is answered; the screen closes at once.</summary>
    protected override bool ConfirmClose() => _view is null || _view.Part<Opener>().ConfirmClose(Quit);

    /// <summary>The window's size while it is normal (not minimized, maximized or full screen): what closing remembers.</summary>
    void KeepNormalSize()
    {
        var (width, height, normal) = WindowStateForTests?.Invoke()
            ?? (Window.Width, Window.Height, !Window.Native.IsIconified && !Window.Native.IsMaximised && !Window.Fullscreen);
        if (!normal || width < 1 || height < 1)
            return;
        _width = width;
        _height = height;
    }

    /// <summary>The window's size and whether it is normal, in place of the window's own (tests: a hidden window can't be resized or maximized everywhere).</summary>
    internal Func<(int Width, int Height, bool Normal)>? WindowStateForTests { get; set; }

    protected override void Unload()
    {
        KeepNormalSize();
        new ShellSettings(_view?.ZoomLevel ?? _shell.ZoomLevel, _width, _height).Write(Storage.Folder);
        CloseShown();
    }
}

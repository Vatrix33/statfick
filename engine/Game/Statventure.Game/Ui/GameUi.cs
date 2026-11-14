using Statventure.Core;
using Statventure.Game.Icons;
using Statventure.Game.Settings;
using Statventure.Game.Sounds;
using Statventure.Graphics;
using Statventure.Packs;
using Statventure.Platform;
using Statventure.Text;
using Statventure.UI;
using Statventure.UI.Dom;
using Statventure.UI.Style;

namespace Statventure.Game.Ui;

/// <summary>What a status line says (the old <c>#backup-status</c>'s messages): news, or a problem.</summary>
public enum StatusKind
{
    Info,
    Problem,
}

/// <summary>
/// The game's one document and what manages it (GM1-P7, GM1-P9; Docs/Game/Foundation.md, "Dialogs, tooltips and the
/// window manager", "The port's stylesheet in <c>ui.pak</c>"; "How it works", step 8). <see cref="Load"/> makes it: the
/// port's stylesheets from <c>ui.pak</c> in a fixed order (<see cref="Stylesheets"/>, then those the screens ask for with
/// <see cref="UseStylesheet"/>, in the order asked), the fonts of their <c>@font-face</c> rules (<c>serif</c> STIX Two Text,
/// <c>sans-serif</c> Lato, each the other's fallback, no system fonts) with <see cref="GameIcons.AddTo"/>, the theme's
/// class (<c>html.dark-mode</c> while the theme is dark) and the page zoom (1.2 to the zoom level's power), both following
/// <see cref="GameSettings.Changed"/>. <see cref="Windows"/> opens and closes the game's windows as the old
/// <c>.modal-layer</c>s; <see cref="Commands"/> runs the commands, the game's keys bound to them (<see cref="Keys"/>);
/// <see cref="Dialogs"/> asks the confirm and prompt dialogs; <see cref="Status"/> says a file message; <see cref="Sound"/>
/// plays an interface sound by name, one per action, and a listener on the document plays <c>ui-press</c> and
/// <c>ui-toggle</c>. The part registers <c>fullscreen</c>, <c>zoom-in</c>, <c>zoom-out</c>, <c>zoom-reset</c>,
/// <c>open-game-folder</c> and <c>quit</c>. It starts no music (Q15).
/// </summary>
public sealed class GameUi : IDisposable
{
    /// <summary>The port's own sheets the document always has, in their order: the game's (GM1-P8), the document's own (its page filling the window), then the dialogs' and the controls'.</summary>
    public static IReadOnlyList<string> Stylesheets { get; } = ["ui/styles/game", "ui/styles/document", "ui/styles/dialogs", "ui/styles/controls"];

    /// <summary>The theme's class on the root element (the old <c>updateTheme</c>, <c>app.js</c> 1632–1638).</summary>
    public const string DarkModeClass = "dark-mode";

    /// <summary>The fonts' families (<c>ui.pak</c>'s <c>@font-face</c> rules).</summary>
    public const string Lato = "Lato", Stix = "STIX Two Text";

    /// <summary>
    /// The game's keys and their commands (Docs/Game/Foundation.md, "Keys"): the old menu's Ctrl+S and Ctrl+O
    /// (<c>main.cjs</c> 82–83), the roles' keys on this system (<see cref="MenuRoles"/>), and plain Ctrl+= zooming in too
    /// (the user, Q30, 2026-11-14: Electron's Ctrl+Plus is Ctrl+Shift+=); no Reload (Q18). Escape isn't a binding: it closes
    /// the topmost window, else runs <c>open-menu</c>.
    /// </summary>
    public static IReadOnlyList<(string Keys, string Command)> Keys(bool? windows = null) =>
    [
        ("Ctrl+S", "save-character"),
        ("Ctrl+O", "load-character"),
        .. MenuRoles.All(windows).Where(r => r.Keys is not null).Select(r => (r.Keys!, r.Command)),
        ("Ctrl+=", MenuRoles.ZoomIn.Command),
    ];

    /// <summary>The command Escape runs when no window is open (Q7).</summary>
    public const string MenuCommand = "open-menu";

    readonly FontLibrary _fonts;
    readonly GameSettings _settings;
    readonly GameSounds _sounds;
    readonly List<string> _sheets = [];

    GameUi(FontLibrary fonts, UiDocument document, UiHost? host, GameSettings settings, Audio.SoundEffects? effects)
    {
        _fonts = fonts;
        _settings = settings;
        Document = document;
        Host = host;
        _sounds = new GameSounds(document, effects);
        _sounds.Played += name => SoundPlayed?.Invoke(name);
        Windows = new GameWindows(document, Sound);
        Commands = new GameCommands();
        Dialogs = new GameDialogs(document, Windows, Sound);
    }

    /// <summary>
    /// The game's document for <paramref name="application"/>'s window (none when it has no window yet: a test's), its
    /// stylesheets and fonts from <paramref name="data"/>, its look following <paramref name="settings"/>; the part's
    /// commands registered and the game's keys bound. A sheet that isn't in the data is logged and left out (a test's
    /// empty data: the engine's Lato then draws the text).
    /// </summary>
    public static GameUi Load(Application application, GameSettings settings, VirtualFileSystem data) => Load(application, settings, data, application.Audio);

    /// <summary><see cref="Load(Application, GameSettings, VirtualFileSystem)"/> with the sound <paramref name="audio"/> (the application's; a test's own).</summary>
    internal static GameUi Load(Application application, GameSettings settings, VirtualFileSystem data, Audio.AudioSystem? audio)
    {
        var fonts = NewFonts();
        var document = new UiDocument(fonts) { Files = data };
        var host = application.Window is { } window && application.Gpu is { } gpu ? new UiHost(window, gpu, document) { Audio = audio } : null;
        var ui = new GameUi(fonts, document, host, settings, audio?.Effects)
        {
            OpenGameFolder = () => application.Storage.OpenInFileManager(),
            QuitGame = application.Quit,
        };
        foreach (var sheet in Stylesheets)
            ui.UseStylesheet(sheet);
        ui.ApplyTheme();
        ui.ApplyZoom();
        settings.Changed += ui.SettingChanged;
        ui.RegisterCommands();
        foreach (var (keys, command) in Keys())
            ui.Commands.Bind(keys, command);
        ui.Listen();
        return ui;
    }

    /// <summary>
    /// The document's fonts: the engine's Lato as the faces added in code (the sheets' Lato, once loaded, hides them), the
    /// generic families mapped to the game's (<c>serif</c> STIX Two Text, <c>sans-serif</c> Lato), Lato and STIX each the
    /// other's fallback, the game's icons (GM1-7); no system font.
    /// </summary>
    internal static FontLibrary NewFonts()
    {
        var fonts = new FontLibrary();
        EngineFonts.AddTo(fonts);
        fonts.SetGeneric("serif", Stix);
        fonts.SetGeneric("sans-serif", Lato);
        fonts.AddFallback(Lato);
        fonts.AddFallback(Stix);
        GameIcons.AddTo(fonts);
        return fonts;
    }

    /// <summary>The game's document.</summary>
    public UiDocument Document { get; }

    /// <summary>The document's host on the application's window (null without a window).</summary>
    public UiHost? Host { get; }

    /// <summary>The game's windows.</summary>
    public GameWindows Windows { get; }

    /// <summary>The commands by name, and the keys bound to them.</summary>
    public GameCommands Commands { get; }

    /// <summary>The confirm and prompt dialogs.</summary>
    public GameDialogs Dialogs { get; }

    /// <summary>The stylesheets the document has from the data, in their order (<see cref="Stylesheets"/>, then the screens').</summary>
    public IReadOnlyList<string> Sheets => _sheets;

    /// <summary>
    /// Adds the compiled stylesheet at <paramref name="path"/> in the game's data (<c>ui/styles/&lt;name&gt;</c>, a screen's
    /// own sheet, step 9 and 10) after those the document has; a sheet it has already is left where it is. A sheet that
    /// isn't in the data is logged and left out.
    /// </summary>
    public void UseStylesheet(string path)
    {
        if (_sheets.Contains(path))
            return;
        var data = Document.Files!;
        if (!data.Exists(path))
        {
            Log.Error("Ui", $"the stylesheet {path} isn't in the game's data: left out");
            return;
        }
        Document.AddStylesheet(Stylesheet.Load(data, path));
        _sheets.Add(path);
    }

    /// <summary>A file message was said (<see cref="Status"/>): its text and kind, for the status line where the action was taken.</summary>
    public event Action<string, StatusKind>? StatusShown;

    /// <summary>Says a file message ("Character saved", "That file is not a valid character save").</summary>
    public void Status(string text, StatusKind kind = StatusKind.Info)
    {
        Log.Info("Ui", $"status ({kind}): {text}");
        StatusShown?.Invoke(text, kind);
    }

    /// <summary>
    /// Plays the interface sound <paramref name="name"/> (<see cref="SoundNames"/>) through the effects bus: one per action,
    /// the one of the action's sounds that says most (an event sound, else a window opening, else closing, else a switch,
    /// else a press; Docs/Game/Foundation.md, "How it works", step 8).
    /// </summary>
    public void Sound(string name) => _sounds.Ask(name);

    /// <summary>A sound was played (after its action's sounds were weighed), with or without a sound device: the checks count them.</summary>
    public event Action<string>? SoundPlayed;

    /// <summary>What <c>open-game-folder</c> does (the per-user folder in the system's file manager, M9); the tests give their own.</summary>
    internal Func<bool> OpenGameFolder { get; set; } = () => false;

    /// <summary>What <c>quit</c> does (the application's <c>Quit</c>; Unload leaves the character, saved).</summary>
    internal Action QuitGame { get; set; } = () => { };

    /// <summary>Once per update: the document's tasks, input and layout.</summary>
    public void Update(double time)
    {
        if (Host is { } host)
            host.Update(time);
        else
            Document.Update(time);
    }

    /// <summary>Draws the document into the frame.</summary>
    public void Render(Frame frame, GpuImage target) => Host?.Render(frame, target);

    public void Dispose()
    {
        _settings.Changed -= SettingChanged;
        Host?.Dispose();
        _fonts.Dispose();
    }

    void SettingChanged(SettingKey key)
    {
        if (key == SettingKey.Theme)
            ApplyTheme();
        else if (key == SettingKey.ZoomLevel)
            ApplyZoom();
    }

    /// <summary><c>html.dark-mode</c> while the theme is dark (GM1-4).</summary>
    void ApplyTheme() => Document.Root.ToggleClass(DarkModeClass, _settings.Theme == GameTheme.Dark);

    /// <summary>The page zoom: 1.2 to the zoom level's power (Electron's zoom factor), on the window's scale.</summary>
    void ApplyZoom()
    {
        var zoom = (float)Math.Pow(1.2, _settings.ZoomLevel);
        if (Host is { } host)
            host.PageZoom = zoom;
        else
            Document.SetZoom(Document.DeviceScale, zoom);
    }

    /// <summary>The part's own commands (Docs/Game/Foundation.md, "The commands and who registers them": step 8's).</summary>
    void RegisterCommands()
    {
        Commands.Register("fullscreen", _ => _settings.Fullscreen = !_settings.Fullscreen);
        Commands.Register("zoom-in", _ => StepZoom(+0.5));
        Commands.Register("zoom-out", _ => StepZoom(-0.5));
        Commands.Register("zoom-reset", _ => _settings.ZoomLevel = 0);
        Commands.Register("open-game-folder", _ =>
        {
            if (!OpenGameFolder())
                Log.Warning("Ui", "the game folder couldn't be opened in a file manager");
        });
        Commands.Register("quit", _ => QuitGame());
    }

    /// <summary>
    /// The zoom roles' half step (<c>webContents.zoomLevel ± 0.5</c>), clamped as Electron's <c>SetZoomLevel</c> clamps it to
    /// Blink's limits (25 % and 500 %, <see cref="GameSettings.MinZoomLevel"/> and <see cref="GameSettings.MaxZoomLevel"/>).
    /// </summary>
    void StepZoom(double step) =>
        _settings.ZoomLevel = Math.Clamp(_settings.ZoomLevel + step, GameSettings.MinZoomLevel, GameSettings.MaxZoomLevel);

    /// <summary>The document's listeners: the keys, Tab in a window, and the presses and switches that sound.</summary>
    void Listen()
    {
        Document.AddEventListener<UiKeyboardEvent>("keydown", _ => _sounds.BeginAction(), capture: true);
        Document.AddEventListener<UiKeyboardEvent>("keydown", TrapTab, capture: true);
        Document.AddEventListener<UiKeyboardEvent>("keydown", KeyDown);
        Document.AddEventListener("click", Clicked, capture: true);
        Document.AddEventListener("change", Changed, capture: true);
    }

    /// <summary>Tab with a window open stays among its controls (<see cref="GameWindows.Tab"/>).</summary>
    void TrapTab(UiKeyboardEvent e)
    {
        if (e.Key != "Tab" || e.CtrlKey || e.AltKey || e.MetaKey || e.DefaultPrevented)
            return;
        if (Windows.Tab(backwards: e.ShiftKey))
            e.PreventDefault();
    }

    /// <summary>
    /// A key the page didn't handle (Electron's menu takes the keys the page left: its accelerators come after the page's
    /// listeners): Escape closes the topmost window, else opens the menu; a bound key runs its command.
    /// </summary>
    void KeyDown(UiKeyboardEvent e)
    {
        if (e.DefaultPrevented)
            return;
        if (e.Key == "Escape")
        {
            if (!Windows.Escape())
                Commands.Run(MenuCommand);
            e.PreventDefault();
            return;
        }
        if (Commands.CommandFor(e) is { } command)
        {
            Commands.Run(command);
            e.PreventDefault();
        }
    }

    /// <summary>A click begins an action; on an enabled button it asks for <c>ui-press</c> (a switch's for <c>ui-toggle</c>), which a window opening or closing, or an event, outweighs.</summary>
    void Clicked(UiEvent e)
    {
        _sounds.BeginAction();
        for (var element = e.Target; element is not null; element = element.Parent)
        {
            if (!element.IsButtonControl)
                continue;
            if (!element.IsDisabled)
                Sound(element.GetAttribute("role") == "switch" ? SoundNames.Toggle : SoundNames.Press);
            return;
        }
    }

    /// <summary>A box, a radio button or a select changed: <c>ui-toggle</c> (a slider's change is the settings screen's press, Q13).</summary>
    void Changed(UiEvent e)
    {
        if (e.Target is { } target && (target.Tag == "select" || target.IsCheckable))
            Sound(SoundNames.Toggle);
    }
}

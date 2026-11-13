using Statventure.Core;
using Statventure.Game.Settings;
using Statventure.Graphics;
using Statventure.Packs;
using Statventure.Text;
using Statventure.UI;
using Statventure.UI.Dom;

namespace Statventure.Game.Ui;

/// <summary>What a status line says (the old <c>#backup-status</c>'s messages): news, or a problem.</summary>
public enum StatusKind
{
    Info,
    Problem,
}

/// <summary>
/// The game's one document and what manages it (GM1-P7, GM1-P9; Docs/Game/Foundation.md, "Dialogs, tooltips and the
/// window manager"; step 8): <see cref="Load"/> makes it (the port's stylesheets from <c>ui.pak</c> in order, the fonts,
/// <c>GameIcons.AddTo</c>, the theme's class and the page zoom, both following the settings); <see cref="Windows"/> opens
/// and closes the game's windows as the old <c>.modal-layer</c>s; <see cref="Commands"/> runs the commands the screens and
/// this part register, bound to their keys; <see cref="Dialogs"/> asks the confirm and prompt dialogs; <see cref="Status"/>
/// says a file message; <see cref="Sound"/> plays an interface sound by name. <b>Step 2's stub:</b> a bare document with
/// the game's name on a parchment page in the engine's Lato; windows only kept in a list (no Escape, focus or sounds);
/// commands that run when registered and are logged when not; dialogs answered "no"; no keys, theme, zoom or sound.
/// </summary>
public sealed class GameUi : IDisposable
{
    /// <summary>The stub's page: the game's name on the Gothic ink theme's parchment (<c>--paper</c>, <c>--ink</c>; step 8 brings the port's stylesheet).</summary>
    const string StubCss = """
        html { background: #d5c39b; }
        body { margin: 0; color: #22160d; font: 15px Lato; }
        .game-name { margin: 0; padding: 40px 48px; font: 700 40px/48px Lato; }
        """;

    readonly FontLibrary _fonts;

    GameUi(FontLibrary fonts, UiDocument document, UiHost? host)
    {
        _fonts = fonts;
        Document = document;
        Host = host;
        Windows = new GameWindows(document);
        Commands = new GameCommands();
        Dialogs = new GameDialogs();
    }

    /// <summary>
    /// The game's document for <paramref name="application"/>'s window (none when it has no window yet: a test's), its
    /// look following <paramref name="settings"/>, its stylesheets from <paramref name="data"/>.
    /// </summary>
    public static GameUi Load(Application application, GameSettings settings, VirtualFileSystem data)
    {
        var fonts = new FontLibrary();
        EngineFonts.AddTo(fonts);
        var document = new UiDocument(fonts);
        document.AddStylesheet(StubCss, "game-stub");
        Markup.SetInnerHtml(document.Body, $"""<main class="game-page"><h1 class="game-name">{StatFickGame.Title}</h1></main>""");
        var host = application.Window is { } window && application.Gpu is { } gpu ? new UiHost(window, gpu, document) { Audio = application.Audio } : null;
        return new GameUi(fonts, document, host);
    }

    /// <summary>The game's document.</summary>
    public UiDocument Document { get; }

    /// <summary>The document's host on the application's window (null without a window).</summary>
    public UiHost? Host { get; }

    /// <summary>The game's windows.</summary>
    public GameWindows Windows { get; }

    /// <summary>The commands by name.</summary>
    public GameCommands Commands { get; }

    /// <summary>The confirm and prompt dialogs.</summary>
    public GameDialogs Dialogs { get; }

    /// <summary>A file message was said (<see cref="Status"/>): its text and kind, for the status line where the action was taken.</summary>
    public event Action<string, StatusKind>? StatusShown;

    /// <summary>Says a file message ("Character saved", "That file is not a valid character save").</summary>
    public void Status(string text, StatusKind kind = StatusKind.Info)
    {
        Log.Info("Ui", $"status ({kind}): {text}");
        StatusShown?.Invoke(text, kind);
    }

    /// <summary>Plays the interface sound <paramref name="name"/> (<c>Sounds.SoundNames</c>; the stub plays nothing).</summary>
    public void Sound(string name) => Log.Debug("Ui", $"sound {name}: no sounds yet");

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
        Host?.Dispose();
        _fonts.Dispose();
    }
}

/// <summary>
/// The game's windows (step 8: open and close a layer as the old <c>.modal-layer</c>s, <c>body.modal-open</c>, Escape
/// closing the topmost, focus kept and given back, the sounds of opening and closing). <b>Step 2's stub:</b> a layer put
/// in the document and kept in a list, the last opened on top.
/// </summary>
public sealed class GameWindows(UiDocument document)
{
    readonly List<UiElement> _open = [];

    /// <summary>The topmost open window, or null.</summary>
    public UiElement? Top => _open.Count > 0 ? _open[^1] : null;

    /// <summary>Opens <paramref name="layer"/> on top (put in the document's body if it isn't in the document).</summary>
    public void Open(UiElement layer)
    {
        if (layer.Parent is null)
            document.Body.Append(layer);
        _open.Remove(layer);
        _open.Add(layer);
    }

    /// <summary>Closes <paramref name="layer"/> (taken out of the document).</summary>
    public void Close(UiElement layer)
    {
        _open.Remove(layer);
        layer.Remove();
    }

    /// <summary>Closes the topmost window; false when none is open.</summary>
    public bool CloseTop()
    {
        if (Top is not { } top)
            return false;
        Close(top);
        return true;
    }
}

/// <summary>
/// The commands (Docs/Game/Foundation.md, "Who calls whom", the commands and who registers them): each registered once by
/// its part or screen, run by name with an optional argument (<c>open-slot</c>'s slot); a command not registered yet does
/// nothing and is logged, so every step runs with the others missing. Step 8 binds the keys to them.
/// </summary>
public sealed class GameCommands
{
    readonly Dictionary<string, (Action<string?> Run, Func<bool>? Enabled)> _commands = new(StringComparer.Ordinal);

    /// <summary>The names registered.</summary>
    public IReadOnlyCollection<string> Names => _commands.Keys;

    /// <summary>Registers <paramref name="name"/>; <paramref name="enabled"/> says when it can run (always without it). A name registered twice is a mistake.</summary>
    public void Register(string name, Action<string?> run, Func<bool>? enabled = null)
    {
        if (!_commands.TryAdd(name, (run, enabled)))
            throw new InvalidOperationException($"the command {name} is registered already");
    }

    /// <summary>Whether <paramref name="name"/> is registered.</summary>
    public bool IsRegistered(string name) => _commands.ContainsKey(name);

    /// <summary>Whether <paramref name="name"/> is registered and can run now.</summary>
    public bool IsEnabled(string name) => _commands.TryGetValue(name, out var command) && command.Enabled?.Invoke() != false;

    /// <summary>Runs <paramref name="name"/>; false (and logged) when it isn't registered or can't run now.</summary>
    public bool Run(string name, string? argument = null)
    {
        if (!_commands.TryGetValue(name, out var command))
        {
            Log.Info("Commands", $"{name}: not registered yet, nothing done");
            return false;
        }
        if (command.Enabled?.Invoke() == false)
        {
            Log.Info("Commands", $"{name}: not possible now, nothing done");
            return false;
        }
        command.Run(argument);
        return true;
    }
}

/// <summary>
/// The confirm and prompt dialogs in place of the old <c>window.confirm</c> and <c>window.prompt</c> (step 8), each a task
/// finished when the player answers. <b>Step 2's stub:</b> none shown; each answered at once as if cancelled (logged).
/// </summary>
public sealed class GameDialogs
{
    /// <summary>Asks <paramref name="question"/> with a button <paramref name="yes"/> and Cancel: true for yes.</summary>
    public Task<bool> Confirm(string question, string yes)
    {
        Log.Info("Ui", $"confirm \"{question}\": no dialogs yet, answered Cancel");
        return Task.FromResult(false);
    }

    /// <summary>Asks <paramref name="question"/> with a text field holding <paramref name="value"/>, <paramref name="ok"/> and Cancel: the text, or null for Cancel.</summary>
    public Task<string?> Prompt(string question, string value, string ok)
    {
        Log.Info("Ui", $"prompt \"{question}\": no dialogs yet, answered Cancel");
        return Task.FromResult<string?>(null);
    }
}

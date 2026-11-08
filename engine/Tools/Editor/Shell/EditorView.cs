using System.Reflection;
using Statventure.Core;
using Statventure.Editor.Session;
using Statventure.Packs;
using Statventure.Text;
using Statventure.UI;
using Statventure.UI.Dom;

namespace Statventure.Editor.Shell;

/// <summary>
/// The editor's document (P2): the shell (a top bar with the commands' buttons, the three panes with splitters
/// between them, a status line; M11-6), its stylesheets, the parts found in the editor's assembly, the commands
/// and keys, dialogs, the window's title and the page zoom. It works on a document of its own without a window
/// (the tests drive it with the UI's events); the program shows it through a <c>UiHost</c>.
/// </summary>
public sealed partial class EditorView : IDisposable
{
    /// <summary>The editor's name, its window's title before the version (M11-19).</summary>
    public const string Name = "Statventure Editor";

    /// <summary>The editor's version as its build recorded it (M12-6, Q1): a release's <c>2026.11.8.1</c>, else <c>dev</c>; in the title (Q3).</summary>
    public static string Version { get; } = ProgramInfo.Of(typeof(EditorView).Assembly).Version;

    /// <summary>The stylesheet holding the shell's @font-face rules (the engine's embedded Lato at ui.pak's paths, P4, Q17); the only one that may.</summary>
    public const string FontsSheet = "Statventure.Editor.Shell.Fonts.css";

    /// <summary>The shell's own stylesheet, added right after the fonts.</summary>
    public const string ShellSheet = "Statventure.Editor.Shell.Shell.css";

    /// <summary>The engine's credits' stylesheet (<see cref="Credits.Css"/>, its rules under <c>.credits</c>), added after the shell's: the About dialog shows the credits (Q30).</summary>
    public const string CreditsSheet = "Statventure.UI.Credits";

    /// <summary>A pane's narrowest width by a splitter (CSS px); the middle one's is <see cref="MinFormWidth"/>.</summary>
    public const float MinSideWidth = 120;

    /// <summary>The middle pane's narrowest width by a splitter (CSS px).</summary>
    public const float MinFormWidth = 200;

    readonly FontLibrary _fonts = new();
    readonly VirtualFileSystem _files = FontFiles();
    readonly Dictionary<EditorPane, UiElement> _panes = [];
    readonly Dictionary<EditorCommand, UiElement> _buttons = [];
    readonly List<EditorPart> _parts = [];
    readonly UiElement _status;
    string _title = "";

    /// <param name="session">The session the view shows.</param>
    /// <param name="extraParts">Parts besides those of the editor's assembly (tests).</param>
    public EditorView(EditorSession session, IEnumerable<Type>? extraParts = null)
    {
        Session = session;
        Document = MakeDocument(_fonts, _files);
        Markup.SetInnerHtml(Document.Body, ShellMarkup);
        Shell = Document.Body.ElementChildren.Single();
        foreach (var pane in Enum.GetValues<EditorPane>())
            _panes[pane] = Document.QuerySelector($"[data-pane={PaneName(pane)}]")!;
        foreach (var button in Document.QuerySelectorAll("[data-command]"))
        {
            var command = Enum.Parse<EditorCommand>(button.GetAttribute("data-command")!.Replace("-", ""), ignoreCase: true);
            _buttons[command] = button;
            button.AddEventListener("click", _ => Commands.Run(new CommandRequest(command, Target: button)));
        }
        _status = Document.QuerySelector(".status")!;
        Dialogs = new EditorDialogs(this);
        foreach (var splitter in Document.QuerySelectorAll("[data-splitter]"))
            _ = new Splitter(this, splitter);
        ListenToKeys();
        Commands.Changed += Refresh;
        Session.Changed += Refresh;
        foreach (var type in EditorPart.Find(typeof(EditorView).Assembly).Concat(extraParts ?? []))
        {
            var part = (EditorPart)Activator.CreateInstance(type)!;
            part.Session = session;
            part.View = this;
            _parts.Add(part);
        }
        _parts.Sort((a, b) => a.Pane != b.Pane ? (a.Pane is { } x ? (int)x + 1 : 0).CompareTo(b.Pane is { } y ? (int)y + 1 : 0)
            : a.Order != b.Order ? a.Order.CompareTo(b.Order)
            : string.CompareOrdinal(a.GetType().FullName, b.GetType().FullName));
        foreach (var part in _parts)
            part.Attach(part.Pane is { } pane ? _panes[pane].Append("div", "part") : null);
        Refresh();
    }

    public EditorSession Session { get; }

    /// <summary>The editor's document.</summary>
    public UiDocument Document { get; }

    /// <summary>The shell's root element (made inert while a dialog is open).</summary>
    public UiElement Shell { get; }

    /// <summary>The commands: what each part handles, what the buttons and keys ask for.</summary>
    public EditorCommands Commands { get; } = new();

    /// <summary>The dialogs open over the shell.</summary>
    public EditorDialogs Dialogs { get; }

    /// <summary>The parts, in the order they were attached.</summary>
    public IReadOnlyList<EditorPart> Parts => _parts;

    /// <summary>The part of type <typeparamref name="T"/> (how parts reach each other).</summary>
    public T Part<T>() where T : EditorPart => _parts.OfType<T>().Single();

    /// <summary>A pane's element.</summary>
    public UiElement Pane(EditorPane pane) => _panes[pane];

    /// <summary>A command's button in the top bar, or null for commands without one.</summary>
    public UiElement? Button(EditorCommand command) => _buttons.GetValueOrDefault(command);

    /// <summary>The status line's text.</summary>
    public string Status
    {
        get => _status.TextContent;
        set => _status.TextContent = value;
    }

    /// <summary>The window's title (<see cref="TitleOf"/> for this editor's <see cref="Version"/>, the session's pack and unsaved work).</summary>
    public string Title => _title;

    /// <summary>
    /// The window's title for the editor of <paramref name="version"/>: "Statventure Editor 2026.11.8.1" (Q3), then
    /// " — &lt;pack&gt;" with <paramref name="pack"/> open (M11-19), and "• " first while there is unsaved work (M11-17).
    /// </summary>
    public static string TitleOf(string version, string? pack, bool unsaved)
    {
        var title = $"{Name} {version}";
        if (pack is not null)
            title += $" — {pack}";
        return unsaved ? "• " + title : title;
    }

    /// <summary>The title changed (the program sets the window's).</summary>
    public event Action? TitleChanged;

    /// <summary>Brings the title and the buttons up to date with the session and the commands (parts call it after changing what they say).</summary>
    public void Refresh()
    {
        foreach (var (command, button) in _buttons)
            button.ToggleAttribute("disabled", !Commands.CanRun(command));
        var title = TitleOf(Version, Session.PackName, Session.HasUnsavedWork);
        if (title == _title)
            return;
        _title = title;
        TitleChanged?.Invoke();
    }

    static string PaneName(EditorPane pane) => pane.ToString().ToLowerInvariant();

    const string ShellMarkup = """
        <div class="shell"><header class="topbar"><button type="button" data-command="new-mod">New mod</button><button type="button" data-command="open" title="Open (Ctrl+O)">Open</button><span class="topbar-gap"></span><button type="button" data-command="save" title="Save (Ctrl+S)">Save</button><button type="button" data-command="save-as" title="Save As (Ctrl+Shift+S)">Save As</button><span class="topbar-gap"></span><button type="button" data-command="new-entry" title="New entry (Ctrl+N)">New entry</button><button type="button" data-command="import">Import</button><span class="topbar-gap"></span><button type="button" data-command="undo" title="Undo (Ctrl+Z)">Undo</button><button type="button" data-command="redo" title="Redo (Ctrl+Y)">Redo</button><span class="topbar-fill"></span><button type="button" data-command="game-folder">Game folder…</button><button type="button" data-command="about">About</button></header><main class="panes"><section class="pane" data-pane="entries" style="width: 280px"></section><div class="splitter" data-splitter="entries" role="separator" aria-orientation="vertical"></div><section class="pane" data-pane="form"></section><div class="splitter" data-splitter="preview" role="separator" aria-orientation="vertical"></div><section class="pane" data-pane="preview" style="width: 360px"></section></main><footer class="status">No pack open</footer></div>
        """;

    /// <summary>
    /// The editor's stylesheets in the order the document has them: <see cref="FontsSheet"/>, <see cref="ShellSheet"/>,
    /// the engine's credits' (<see cref="CreditsSheet"/>), then every other <c>.css</c> file of the editor's folders by its
    /// resource name (ordinal).
    /// </summary>
    public static IReadOnlyList<(string Name, string Css)> Stylesheets { get; } = LoadStylesheets();

    static List<(string, string)> LoadStylesheets()
    {
        var assembly = typeof(EditorView).Assembly;
        var names = assembly.GetManifestResourceNames().Where(n => n.EndsWith(".css", StringComparison.Ordinal)).ToList();
        var parts = names.Where(n => n is not FontsSheet and not ShellSheet).OrderBy(n => n, StringComparer.Ordinal);
        return [(FontsSheet, Read(assembly, FontsSheet)), (ShellSheet, Read(assembly, ShellSheet)), (CreditsSheet, Credits.Css),
            .. parts.Select(name => (name, Read(assembly, name)))];
    }

    /// <summary>
    /// The files an editor document reads its fonts from (Q17): the engine's embedded Lato at ui.pak's paths
    /// (<see cref="EngineFonts.Mount"/>), which <see cref="FontsSheet"/>'s @font-face rules name; the editor draws before
    /// it knows where the game is.
    /// </summary>
    public static VirtualFileSystem FontFiles() => new([EngineFonts.Mount()]);

    /// <summary>A document in the editor's look: <paramref name="fonts"/> its library, <paramref name="files"/> (<see cref="FontFiles"/>) its files, every one of <see cref="Stylesheets"/> in order.</summary>
    public static UiDocument MakeDocument(FontLibrary fonts, VirtualFileSystem files)
    {
        var document = new UiDocument(fonts) { Files = files };
        foreach (var (name, css) in Stylesheets)
            document.AddStylesheet(css, name);
        return document;
    }

    static string Read(Assembly assembly, string name)
    {
        using var stream = assembly.GetManifestResourceStream(name) ?? throw new InvalidOperationException($"the editor's stylesheet {name} isn't in its assembly");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    /// <summary>Gives back the fonts and their files (the document is no longer drawn after this).</summary>
    public void Dispose()
    {
        _fonts.Dispose();
        _files.Dispose();
    }
}

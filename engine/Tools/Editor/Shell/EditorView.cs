using System.Reflection;
using Statventure.Editor.Session;
using Statventure.Packs;
using Statventure.Text;
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
    /// <summary>The window's title without an open pack (M11-19).</summary>
    public const string Name = "Statventure Editor";

    /// <summary>The stylesheet holding the shell's @font-face rules (the game's Lato from ui.pak, P4); the only one that may.</summary>
    public const string FontsSheet = "Statventure.Editor.Shell.Fonts.css";

    /// <summary>The shell's own stylesheet, added right after the fonts.</summary>
    public const string ShellSheet = "Statventure.Editor.Shell.Shell.css";

    /// <summary>A pane's narrowest width by a splitter (CSS px); the middle one's is <see cref="MinFormWidth"/>.</summary>
    public const float MinSideWidth = 120;

    /// <summary>The middle pane's narrowest width by a splitter (CSS px).</summary>
    public const float MinFormWidth = 200;

    readonly FontLibrary _fonts = new();
    readonly Dictionary<EditorPane, UiElement> _panes = [];
    readonly Dictionary<EditorCommand, UiElement> _buttons = [];
    readonly List<EditorPart> _parts = [];
    readonly UiElement _status;
    string _title = "";

    /// <param name="session">The session the view shows.</param>
    /// <param name="uiData">The data the document reads its fonts from (a file system holding the game's ui.pak).</param>
    /// <param name="extraParts">Parts besides those of the editor's assembly (tests).</param>
    public EditorView(EditorSession session, VirtualFileSystem uiData, IEnumerable<Type>? extraParts = null)
    {
        Session = session;
        Document = new UiDocument(_fonts) { Files = uiData };
        foreach (var (name, css) in Stylesheets)
            Document.AddStylesheet(css, name);
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

    /// <summary>The window's title: "Statventure Editor — &lt;pack&gt;", with "• " first while there is unsaved work (M11-17).</summary>
    public string Title => _title;

    /// <summary>The title changed (the program sets the window's).</summary>
    public event Action? TitleChanged;

    /// <summary>Brings the title and the buttons up to date with the session and the commands (parts call it after changing what they say).</summary>
    public void Refresh()
    {
        foreach (var (command, button) in _buttons)
            button.ToggleAttribute("disabled", !Commands.CanRun(command));
        var title = Session.PackName is { } pack ? $"{Name} — {pack}" : Name;
        if (Session.HasUnsavedWork)
            title = "• " + title;
        if (title == _title)
            return;
        _title = title;
        TitleChanged?.Invoke();
    }

    static string PaneName(EditorPane pane) => pane.ToString().ToLowerInvariant();

    const string ShellMarkup = """
        <div class="shell"><header class="topbar"><button type="button" data-command="new-mod">New mod</button><button type="button" data-command="open" title="Open (Ctrl+O)">Open</button><span class="topbar-gap"></span><button type="button" data-command="save" title="Save (Ctrl+S)">Save</button><button type="button" data-command="save-as" title="Save As (Ctrl+Shift+S)">Save As</button><span class="topbar-gap"></span><button type="button" data-command="new-entry" title="New entry (Ctrl+N)">New entry</button><button type="button" data-command="import">Import</button><span class="topbar-gap"></span><button type="button" data-command="undo" title="Undo (Ctrl+Z)">Undo</button><button type="button" data-command="redo" title="Redo (Ctrl+Y)">Redo</button></header><main class="panes"><section class="pane" data-pane="entries" style="width: 280px"></section><div class="splitter" data-splitter="entries" role="separator" aria-orientation="vertical"></div><section class="pane" data-pane="form"></section><div class="splitter" data-splitter="preview" role="separator" aria-orientation="vertical"></div><section class="pane" data-pane="preview" style="width: 360px"></section></main><footer class="status">No pack open</footer></div>
        """;

    /// <summary>
    /// The editor's stylesheets in the order the document has them: <see cref="FontsSheet"/>, <see cref="ShellSheet"/>,
    /// then every other <c>.css</c> file of the editor's folders by its resource name (ordinal).
    /// </summary>
    public static IReadOnlyList<(string Name, string Css)> Stylesheets { get; } = LoadStylesheets();

    static List<(string, string)> LoadStylesheets()
    {
        var assembly = typeof(EditorView).Assembly;
        var names = assembly.GetManifestResourceNames().Where(n => n.EndsWith(".css", StringComparison.Ordinal)).ToList();
        var ordered = new[] { FontsSheet, ShellSheet }.Concat(names.Where(n => n is not FontsSheet and not ShellSheet).OrderBy(n => n, StringComparer.Ordinal));
        return [.. ordered.Select(name => (name, Read(assembly, name)))];
    }

    static string Read(Assembly assembly, string name)
    {
        using var stream = assembly.GetManifestResourceStream(name) ?? throw new InvalidOperationException($"the editor's stylesheet {name} isn't in its assembly");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    /// <summary>Gives back the fonts (the document is no longer drawn after this).</summary>
    public void Dispose() => _fonts.Dispose();
}

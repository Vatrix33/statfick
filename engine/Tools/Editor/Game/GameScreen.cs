using Statventure.Editor.Browser;
using Statventure.Editor.Session;
using Statventure.Editor.Settings;
using Statventure.Editor.Shell;
using Statventure.Packs;
using Statventure.Storage;
using Statventure.Text;
using Statventure.UI.Dom;

namespace Statventure.Editor.Game;

/// <summary>
/// "Where is Stat Fick?" (Q16; M12 step 6): the editor's first screen while it knows no game: on its first start, and when
/// the game's folder it remembers no longer holds the game it works with (gone, no longer the game, another version: said
/// on the screen). A page of its own in the editor's look and the engine's Lato (it draws before any game is known, Q17):
/// a heading, what to choose (the folder Stat Fick was unpacked into, holding its program and its packs folder), the
/// editor's version (the game must be of the same, Q18), the problem with the remembered folder, and the file browser
/// built into the page picking a folder (<see cref="FileBrowserView"/>, "Use this folder"), starting beside the
/// remembered folder when there is one. A folder the editor can't work on is said under the listing; the game found is
/// remembered (<see cref="RememberedGame"/>) and given to the program (<see cref="Chosen"/>), which shows the editor.
/// </summary>
public sealed class GameScreen : IDisposable, IFileBrowserHost
{
    readonly FontLibrary _fonts = new();
    readonly VirtualFileSystem _files = EditorView.FontFiles();
    readonly string _playerFolder;
    RecentPlaces? _recent;

    /// <param name="playerFolder">The player's game folder (its mods folder a place, the remembered folder and recent places in its <c>editor/</c>).</param>
    /// <param name="problem">Why the remembered folder can't be used, or null (none remembered).</param>
    /// <param name="editorVersion">The editor's version (the game must be of the same).</param>
    /// <param name="windows">Whether the program is Windows' (the game's program named as it is there).</param>
    /// <param name="setUp">Called before the browser is built (the screens' fixtures set its places and listing).</param>
    public GameScreen(string playerFolder, GameProblem? problem, string editorVersion, bool windows = false, Action<GameScreen>? setUp = null)
    {
        _playerFolder = Path.GetFullPath(playerFolder);
        EditorVersion = editorVersion;
        setUp?.Invoke(this);
        Document = EditorView.MakeDocument(_fonts, _files);
        var screen = Document.Body.Append("div", "game-screen");
        var page = screen.Append("div", "game-page");
        page.Append("h1", text: Heading);
        page.Append("p", "game-lead", LeadLine(windows));
        page.Append("p", "game-version", $"This is Statventure Editor {editorVersion}, which works with Stat Fick {editorVersion}.");
        Problem = problem?.Sentence(editorVersion, remembered: true) ?? "";
        if (Problem.Length > 0)
            page.Append("p", "game-problem", Problem);
        var buttons = new UiElement("div") { ClassName = "game-buttons" };
        var remembered = problem?.Folder;
        Browser = new ScreenBrowser(this, new BrowserRequest(Heading, "Use this folder", Folder: true,
            StartFolder: StartFolder ?? (remembered is not null && Path.GetDirectoryName(remembered) is { } beside && Directory.Exists(beside) ? beside : null),
            Fallback: BrowserPlaceKind.Home, Accept: Accept), page, buttons);
        page.Append(buttons);
        Document.UpdateLayout();
        Browser.Listing.Focus();
    }

    /// <summary>The screen's heading (Q16's words).</summary>
    public const string Heading = "Where is Stat Fick?";

    /// <summary>What to choose, naming the game's program as it is on Windows or Linux (M12-2).</summary>
    public static string LeadLine(bool windows) =>
        $"Statventure Editor makes mods for Stat Fick and works on the game's data, so it needs the game: choose the folder Stat Fick was unpacked into, the one holding {(windows ? "Stat Fick.exe" : "stat-fick")} and its packs folder.";

    /// <summary>The editor's version the screen asks a game of.</summary>
    public string EditorVersion { get; }

    /// <summary>The screen's document.</summary>
    public UiDocument Document { get; }

    /// <summary>The file browser in the page.</summary>
    public FileBrowserView Browser { get; }

    /// <summary>What the screen says about the remembered folder (empty when nothing).</summary>
    public string Problem { get; }

    /// <summary>The game chosen and found, once one is.</summary>
    public GameData? Game { get; private set; }

    /// <summary>A game was chosen and found: the program shows the editor on it.</summary>
    public event Action<GameData>? Chosen;

    /// <summary>The places to list instead of this machine's (the screens' fixture), or null.</summary>
    public Func<IReadOnlyList<BrowserPlaceItem>>? PlacesOverride { get; set; }

    /// <summary>A folder's listing in place of this machine's (the screens' fixture), or null.</summary>
    public Func<string, IReadOnlyCollection<string>?, bool, IReadOnlyList<FolderEntry>>? ListOverride { get; set; }

    /// <summary>The time zone of the listing's dates (this machine's; the fixture gives UTC).</summary>
    public TimeZoneInfo TimeZone { get; set; } = TimeZoneInfo.Local;

    /// <summary>The folder the browser starts in instead of beside the remembered one (the screens' fixture), or null.</summary>
    public string? StartFolder { get; set; }

    GameFound? _found;

    /// <summary>A folder picked: the game in it found (its kinds loaded), or why not, said under the listing.</summary>
    string? Accept(string folder)
    {
        _found = GameLookup.Find(folder, EditorVersion);
        return _found.Problem?.Sentence(EditorVersion);
    }

    void Done(string folder)
    {
        RememberedGame.Write(_playerFolder, folder);
        Game = _found!.Game;
        Chosen?.Invoke(Game!);
    }

    /// <summary>The places: the mods folder, Documents, home, the roots and the recent places (no game packs: no game yet).</summary>
    public IReadOnlyList<BrowserPlaceItem> Places() => PlacesOverride?.Invoke()
        ?? [.. FileBrowser.Places(Path.Combine(_playerFolder, GameStorage.ModsName), Recent).Select(p => new BrowserPlaceItem(FileBrowserPart.Label(p), p.Folder, p.Kind))];

    public IReadOnlyList<FolderEntry> List(string folder, IReadOnlyCollection<string>? endings, bool showHidden) =>
        ListOverride is { } list ? list(folder, endings, showHidden) : FileBrowser.List(folder, endings, showHidden);

    /// <summary>The recent places (read from the player's <c>editor/recent-places.json</c>).</summary>
    public RecentPlaces Recent => _recent ??= RecentPlacesFile.Read(_playerFolder);

    Task<string?> IFileBrowserHost.Ask(string title, string message, params (string Text, string? Value)[] buttons) => Task.FromResult<string?>(null);

    public void Picked(string path)
    {
        if (Path.GetDirectoryName(path) is not { Length: > 0 } folder)
            return;
        Recent.Add(folder);
        RecentPlacesFile.Write(_playerFolder, Recent);
    }

    public void Dispose()
    {
        _fonts.Dispose();
        _files.Dispose();
    }

    sealed class ScreenBrowser(GameScreen screen, BrowserRequest request, UiElement content, UiElement buttons)
        : FileBrowserView(screen, request, content, buttons)
    {
        protected override void Done(string path) => screen.Done(path);
    }
}

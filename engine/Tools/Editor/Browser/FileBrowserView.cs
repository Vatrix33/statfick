using System.Globalization;
using Statventure.Editor.Shell;
using Statventure.Storage;
using Statventure.UI.Dom;

namespace Statventure.Editor.Browser;

/// <summary>
/// What the file browser works with besides its request: its places, a folder's listing, the recent places and their
/// keeping, the time zone of its dates, the document it is in and a question asked over it (<see cref="FileBrowserPart"/>
/// in the editor; the "Where is Stat Fick?" screen before the editor has a game, <see cref="Game.GameScreen"/>).
/// </summary>
public interface IFileBrowserHost
{
    /// <summary>The places, in the browser's order.</summary>
    IReadOnlyList<BrowserPlaceItem> Places();

    /// <summary>A folder's listing (given the folder, the endings and whether hidden ones show).</summary>
    IReadOnlyList<FolderEntry> List(string folder, IReadOnlyCollection<string>? endings, bool showHidden);

    /// <summary>The recent places.</summary>
    RecentPlaces Recent { get; }

    /// <summary>The time zone the listing's dates are shown in.</summary>
    TimeZoneInfo TimeZone { get; }

    /// <summary>The document the browser is in.</summary>
    UiDocument Document { get; }

    /// <summary>A question over the browser (an export over a file that is there), giving the value of the button clicked.</summary>
    Task<string?> Ask(string title, string message, params (string Text, string? Value)[] buttons);

    /// <summary>A path was picked: its folder becomes the most recent place, and the recent places are kept.</summary>
    void Picked(string path);
}

/// <summary>
/// The file browser (<see cref="FileBrowserPart"/>), built into an element of its host's document: the places on the
/// left; on the right the folder shown (named from the place it is in), Up and "Hidden", the listing
/// (<see cref="FileBrowser.List"/>: folders, then files with size and date) and the path field; the request's button
/// beside its host's others. The listing is one stop for Tab: the arrow keys, Home and End move its selection, Enter
/// opens a folder or picks a file (as a double click does), Backspace goes up. The path field reads what is typed
/// (<see cref="FileBrowser.ReadPath(string, string)"/>) on Enter or the button: a folder is gone to, a file the request
/// shows is picked (an export's new name too); Tab completes its last name when that changes it
/// (<see cref="FileBrowser.Complete(string, string, IReadOnlyCollection{string}?, bool)"/>), else moves the focus. What
/// can't be done is said under the listing. <b>Picking a folder</b> (<see cref="BrowserRequest.Folder"/>; the game's
/// folder, M12 step 6): the button picks what the path field names, else the selected folder, else the folder shown; a
/// file picked stands for the folder holding it; the request's <see cref="BrowserRequest.Accept"/> may refuse a pick,
/// saying why under the listing. The dialog over the shell is <see cref="FileBrowserDialog"/>.
/// </summary>
public abstract class FileBrowserView
{
    readonly IFileBrowserHost _host;
    readonly UiElement _places, _where, _up, _hidden, _listing, _path, _message;
    readonly List<UiElement> _rows = [];
    IReadOnlyList<FolderEntry> _entries = [];
    IReadOnlyList<BrowserPlaceItem> _placeItems = [];

    /// <summary>Builds the browser into <paramref name="content"/> (its lead line, if the request has one, first) and its button into <paramref name="buttons"/>, and shows the start folder.</summary>
    protected FileBrowserView(IFileBrowserHost host, BrowserRequest request, UiElement content, UiElement buttons)
    {
        _host = host;
        Request = request;
        if (request.Lead is { } lead)
            content.Append("p", "browser-lead", lead);
        var browser = content.Append("div", "browser");
        _places = browser.Append("div", "browser-places");
        _places.SetAttribute("aria-label", "Places");
        var main = browser.Append("div", "browser-main");
        var bar = main.Append("div", "browser-bar");
        _up = bar.Append("button", "browser-up", "Up");
        _up.SetAttribute("type", "button");
        _up.SetAttribute("title", "The folder above (Backspace)");
        _up.AddEventListener("click", _ => Up());
        _where = bar.Append("span", "browser-where");
        var hidden = bar.Append("label", "browser-hidden");
        _hidden = hidden.Append("input");
        _hidden.SetAttribute("type", "checkbox");
        hidden.AppendText("Hidden");
        _hidden.AddEventListener("change", _ => Refresh());
        _listing = main.Append("div", "browser-listing");
        _listing.SetAttribute("role", "listbox");
        _listing.SetAttribute("tabindex", "0");
        _listing.SetAttribute("aria-label", "Files");
        _listing.AddEventListener<UiKeyboardEvent>("keydown", ListingKey);
        var head = main.Append("div", "browser-head");
        head.Append("span", "name", "Name");
        head.Append("span", "size", "Size");
        head.Append("span", "date", "Modified");
        main.InsertBefore(head, _listing);
        var pathLine = main.Append("div", "browser-path");
        _path = pathLine.Append("input");
        _path.SetAttribute("type", "text");
        _path.SetAttribute("spellcheck", "false");
        _path.SetAttribute("aria-label", "Path");
        _path.SetAttribute("placeholder", request.NewFile ? "A file name, or a path" : "Type a path, or a name in this folder");
        _path.AddEventListener<UiKeyboardEvent>("keydown", PathKey);
        _message = main.Append("div", "browser-message");
        Button = buttons.Append("button", "primary", request.Button);
        Button.SetAttribute("type", "button");
        Button.AddEventListener("click", _ => Choose());
        if (request.SuggestedName is { } name)
        {
            _path.Value = name;
            _path.SetAttribute("autofocus", "");
        }
        else
            _listing.SetAttribute("autofocus", "");
        Places();
        GoTo(StartFolder(), first: true);
    }

    /// <summary>The path picked (a full path; a folder's for a folder request), once the request accepted it.</summary>
    protected abstract void Done(string path);

    /// <summary>The request's button (Open, Export, Use this folder).</summary>
    public UiElement Button { get; }

    /// <summary>What is being picked.</summary>
    public BrowserRequest Request { get; }

    /// <summary>The folder shown (a full path).</summary>
    public string Folder { get; private set; } = "";

    /// <summary>What the listing shows, in its order.</summary>
    public IReadOnlyList<FolderEntry> Entries => _entries;

    /// <summary>The listing's selected row, or -1.</summary>
    public int Selected { get; private set; } = -1;

    /// <summary>What the browser says under the listing (empty when nothing).</summary>
    public string Message => _message.TextContent;

    /// <summary>The path field.</summary>
    public UiElement PathField => _path;

    /// <summary>The listing (its rows are its children).</summary>
    public UiElement Listing => _listing;

    /// <summary>The "Hidden" checkbox.</summary>
    public UiElement HiddenBox => _hidden;

    /// <summary>The places' buttons, in order.</summary>
    public IReadOnlyList<UiElement> PlaceButtons => [.. _places.ElementChildren.Where(e => e.Tag == "button")];

    /// <summary>The places listed, in order (as <see cref="PlaceButtons"/>).</summary>
    public IReadOnlyList<BrowserPlaceItem> PlaceItems => _placeItems;

    /// <summary>
    /// The folder to start in: the request's, else the most recent place that is a folder now, else the request's
    /// fallback place (the mods folder to open, Documents to export), else the first place that is a folder.
    /// </summary>
    string StartFolder()
    {
        if (Request.StartFolder is { } start)
            return start;
        if (_host.Recent.Folders.FirstOrDefault(Directory.Exists) is { } recent)
            return recent;
        if (_placeItems.FirstOrDefault(p => p.Kind == Request.Fallback && Directory.Exists(p.Folder)) is { } fallback)
            return fallback.Folder;
        return _placeItems.FirstOrDefault(p => Directory.Exists(p.Folder))?.Folder ?? _placeItems.FirstOrDefault()?.Folder ?? Path.GetFullPath(".");
    }

    void Places()
    {
        _placeItems = _host.Places();
        _places.ClearChildren();
        var recentShown = false;
        foreach (var place in _placeItems)
        {
            if (place.Kind == BrowserPlaceKind.Recent && !recentShown)
            {
                _places.Append("div", "browser-places-title", "Recent");
                recentShown = true;
            }
            var button = _places.Append("button", "browser-place", place.Label);
            button.SetAttribute("type", "button");
            button.SetAttribute("title", place.Folder);
            button.AddEventListener("click", _ => GoTo(place.Folder));
        }
    }

    /// <summary>Shows <paramref name="folder"/>; one that can't be listed is said under the listing (the first one shown is listed as empty).</summary>
    public void GoTo(string folder, bool first = false)
    {
        var full = Path.TrimEndingDirectorySeparator(Path.GetFullPath(folder));
        if (full.Length == 0)
            full = Path.GetFullPath(folder);
        IReadOnlyList<FolderEntry> entries;
        var said = "";
        try
        {
            entries = _host.List(full, Request.Endings, _hidden.Checked);
        }
        catch (Exception problem) when (problem is IOException or UnauthorizedAccessException)
        {
            var why = problem is DirectoryNotFoundException ? $"{full} isn't there." : $"{full} can't be read: {problem.Message}";
            if (!first)
            {
                Say(why);
                return;
            }
            entries = [];
            said = problem is DirectoryNotFoundException ? "This folder isn't there yet." : why;
        }
        Folder = full;
        _entries = entries;
        Selected = -1;
        Render();
        Say(said);
    }

    /// <summary>Lists the folder shown again (the "Hidden" box changed, or files came or went).</summary>
    public void Refresh()
    {
        var name = Selected >= 0 ? _entries[Selected].Name : null;
        try
        {
            _entries = _host.List(Folder, Request.Endings, _hidden.Checked);
        }
        catch (Exception problem) when (problem is IOException or UnauthorizedAccessException)
        {
            _entries = [];
        }
        Render();
        if (name is not null)
            Select(_entries.ToList().FindIndex(e => e.Name == name));
    }

    /// <summary>Goes to the folder above the one shown (nothing at a root).</summary>
    public void Up()
    {
        if (Path.GetDirectoryName(Folder) is { } parent)
            GoTo(parent);
    }

    void Render()
    {
        _where.TextContent = Where(Folder);
        _where.SetAttribute("title", Folder);
        _up.ToggleAttribute("disabled", Path.GetDirectoryName(Folder) is null);
        foreach (var button in PlaceButtons)
            button.ToggleClass("current", string.Equals(button.GetAttribute("title"), Folder, FileBrowser.NameComparison));
        _listing.ClearChildren();
        _rows.Clear();
        for (var i = 0; i < _entries.Count; i++)
        {
            var entry = _entries[i];
            var row = _listing.Append("div", entry.IsFolder ? "browser-row folder" : "browser-row file");
            row.SetAttribute("role", "option");
            row.SetAttribute("aria-selected", "false");
            row.Append("span", "name", entry.IsFolder ? entry.Name + "/" : entry.Name);
            row.Append("span", "size", entry.IsFolder ? "" : Size(entry.Size));
            row.Append("span", "date", entry.IsFolder ? "" : Date(entry.LastWrittenUtc));
            var index = i;
            row.AddEventListener<UiMouseEvent>("mousedown", e =>
            {
                if (e.Button == 0)
                    Select(index);
            });
            row.AddEventListener("dblclick", _ => Activate(index));
            _rows.Add(row);
        }
        if (_entries.Count == 0)
            _listing.Append("div", "browser-empty", Request.Endings is { Count: > 0 } endings
                ? $"No folders or {string.Join(" or ", endings)} files here."
                : "Nothing here.");
    }

    /// <summary>
    /// The folder as the browser names it: from the place it is in (the deepest of the mods folder, the game's packs,
    /// Documents and home that holds it: "Mods / old"), else its full path.
    /// </summary>
    public string Where(string folder)
    {
        BrowserPlaceItem? best = null;
        foreach (var place in _placeItems)
        {
            if (place.Kind is BrowserPlaceKind.Root or BrowserPlaceKind.Recent)
                continue;
            if (Relative(place.Folder, folder) is not null && (best is null || place.Folder.Length > best.Folder.Length))
                best = place;
        }
        if (best is null)
            return folder;
        var rest = Relative(best.Folder, folder)!;
        return rest.Length == 0 ? best.Label : best.Label + " / " + string.Join(" / ", rest.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries));
    }

    /// <summary><paramref name="folder"/> relative to <paramref name="place"/> ("" for the place itself), or null when it isn't in it.</summary>
    static string? Relative(string place, string folder)
    {
        var root = Path.TrimEndingDirectorySeparator(place);
        if (string.Equals(root, folder, FileBrowser.NameComparison))
            return "";
        var prefix = root + Path.DirectorySeparatorChar;
        return folder.StartsWith(prefix, FileBrowser.NameComparison) ? folder[prefix.Length..] : null;
    }

    /// <summary>A file's size as the listing writes it: bytes below 1 KB, then KB, MB and GB of 1,024 with one decimal below 10.</summary>
    public static string Size(long bytes)
    {
        if (bytes < 1024)
            return bytes == 1 ? "1 byte" : $"{bytes} bytes";
        string[] units = ["KB", "MB", "GB", "TB"];
        double value = bytes;
        var unit = -1;
        do
        {
            value /= 1024;
            unit++;
        }
        while (value >= 1024 && unit < units.Length - 1);
        return (value < 10 ? value.ToString("0.#", CultureInfo.InvariantCulture) : Math.Round(value).ToString("0", CultureInfo.InvariantCulture)) + " " + units[unit];
    }

    /// <summary>A file's date as the listing writes it, in the browser's time zone: "2026-11-07 16:50".</summary>
    public string Date(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), _host.TimeZone).ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);

    /// <summary>Selects the listing's row <paramref name="index"/> (-1: none), scrolled into view.</summary>
    public void Select(int index)
    {
        if (index < -1 || index >= _entries.Count)
            index = -1;
        if (Selected >= 0 && Selected < _rows.Count)
        {
            _rows[Selected].RemoveClass("selected");
            _rows[Selected].SetAttribute("aria-selected", "false");
        }
        Selected = index;
        if (index < 0)
            return;
        _rows[index].AddClass("selected");
        _rows[index].SetAttribute("aria-selected", "true");
        _host.Document.UpdateLayout();
        _rows[index].ScrollIntoView(ScrollAlign.Nearest);
    }

    /// <summary>Opens the row's folder or picks its file (a double click, Enter).</summary>
    public void Activate(int index)
    {
        if (index < 0 || index >= _entries.Count)
            return;
        var entry = _entries[index];
        if (entry.IsFolder)
            GoTo(entry.FullPath);
        else
            _ = Pick(entry.FullPath);
    }

    /// <summary>
    /// The request's button: what the path field names when something is typed, else the selected row (a folder request
    /// picks a selected folder rather than going into it), else, picking a folder, the folder shown.
    /// </summary>
    public void Choose()
    {
        if (_path.Value.Length > 0)
            ReadTyped(choosing: true);
        else if (Selected >= 0 && Request.Folder && _entries[Selected].IsFolder)
            _ = Pick(_entries[Selected].FullPath);
        else if (Selected >= 0)
            Activate(Selected);
        else if (Request.Folder)
            _ = Pick(Folder);
        else
            Say(Request.NewFile ? "Type a file name." : "Choose a file.");
    }

    /// <summary>
    /// Reads the path field: a folder is gone to (the field emptied; picked instead when it is the button that reads it
    /// for a folder request), a file is picked when the request shows it, a new name when it may be.
    /// </summary>
    public void ReadTyped() => ReadTyped(choosing: false);

    void ReadTyped(bool choosing)
    {
        var typed = _path.Value;
        if (typed.Length == 0)
        {
            Choose();
            return;
        }
        var read = FileBrowser.ReadPath(Folder, typed);
        switch (read.Kind)
        {
            case TypedPathKind.Folder when choosing && Request.Folder:
                _ = Pick(read.FullPath);
                break;
            case TypedPathKind.Folder:
                GoTo(read.FullPath);
                if (Folder == read.FullPath)
                    _path.Value = "";
                break;
            case TypedPathKind.File:
                if (FileBrowser.Shows(Path.GetFileName(read.FullPath), Request.Endings))
                    _ = Pick(read.FullPath);
                else
                    Say($"{Path.GetFileName(read.FullPath)} isn't a {string.Join(" or ", Request.Endings!)} file.");
                break;
            case TypedPathKind.New when Request.NewFile:
                _ = Pick(read.FullPath);
                break;
            case TypedPathKind.New:
            case TypedPathKind.Missing:
                Say($"{typed} isn't there.");
                break;
            default:
                Say($"{typed} isn't a path here.");
                break;
        }
    }

    /// <summary>
    /// Picks <paramref name="path"/> (for a folder request a file stands for the folder holding it): an export over an
    /// existing file asks first (the browser is inert meanwhile, so nothing else is picked); the request's
    /// <see cref="BrowserRequest.Accept"/> may refuse it, said under the listing; then its folder becomes the most recent
    /// place and the browser is done with it.
    /// </summary>
    async Task Pick(string path)
    {
        if (Request.Folder && !Directory.Exists(path) && Path.GetDirectoryName(path) is { Length: > 0 } holding)
            path = holding;
        if (Request.NewFile && File.Exists(path))
        {
            var answer = await _host.Ask("Replace the file?", $"{Path.GetFileName(path)} is there already. Replace it?", ("Replace", "replace"), ("Cancel", null));
            if (answer is null)
                return;
        }
        if (Request.Accept?.Invoke(path) is { } refused)
        {
            Say(refused);
            return;
        }
        _host.Picked(path);
        Done(path);
    }

    void Say(string text) => _message.TextContent = text;

    void ListingKey(UiKeyboardEvent e)
    {
        if (e.CtrlKey || e.AltKey || e.MetaKey)
            return;
        var last = _entries.Count - 1;
        switch (e.Key)
        {
            case "ArrowDown":
                Select(Math.Min(Selected + 1, last));
                break;
            case "ArrowUp":
                Select(Selected < 0 ? last : Math.Max(Selected - 1, 0));
                break;
            case "Home":
                Select(_entries.Count > 0 ? 0 : -1);
                break;
            case "End":
                Select(last);
                break;
            case "Enter":
                Activate(Selected);
                break;
            case "Backspace":
                Up();
                break;
            default:
                return;
        }
        e.PreventDefault();
    }

    void PathKey(UiKeyboardEvent e)
    {
        if (e.CtrlKey || e.AltKey || e.MetaKey)
            return;
        if (e.Key == "Enter" && !e.ShiftKey)
        {
            ReadTyped();
            e.PreventDefault();
        }
        else if (e.Key == "Tab" && !e.ShiftKey && _path.Value.Length > 0)
        {
            var completion = FileBrowser.Complete(Folder, _path.Value, Request.Endings, _hidden.Checked);
            if (completion.Text == _path.Value)
                return;
            _path.Value = completion.Text;
            _path.SetSelectionRange(completion.Text.Length, completion.Text.Length);
            e.PreventDefault();
        }
    }
}

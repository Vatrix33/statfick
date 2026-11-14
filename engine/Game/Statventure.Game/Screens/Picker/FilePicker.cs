using System.Globalization;
using Statventure.Game.Screens.Slots;
using Statventure.Storage;
using Statventure.UI.Dom;

namespace Statventure.Game.Screens.Picker;

/// <summary>What the file picker is asked for: a character file to write (Save Character) or to read (Load Character).</summary>
public enum PickerMode
{
    Save,
    Load,
}

/// <summary>A place the picker offers on its left: what it is, its folder, the label it shows.</summary>
public sealed record PickerPlace(BrowserPlaceKind Kind, string Folder, string Label);

/// <summary>
/// What the file picker works with besides its request (<see cref="PickerScreen"/>): the places, a folder's listing, the
/// time zone of its dates, and the question asked before a file is replaced. The tests give their own (a listing of the
/// fixture's own, so no path of the machine is in a screen).
/// </summary>
public interface IPickerHost
{
    /// <summary>The places, in the picker's order: Documents, home, the roots, the recent places that are folders now.</summary>
    IReadOnlyList<PickerPlace> Places();

    /// <summary>A folder's entries as the picker lists them (folders, then the <c>.json</c> files).</summary>
    /// <exception cref="IOException">It can't be listed (<see cref="DirectoryNotFoundException"/>: it isn't there).</exception>
    /// <exception cref="UnauthorizedAccessException">It may not be listed.</exception>
    IReadOnlyList<FolderEntry> List(string folder);

    /// <summary>The time zone the dates are written in.</summary>
    TimeZoneInfo TimeZone { get; }

    /// <summary>Whether a file is there (to save over one, a question first).</summary>
    bool FileExists(string file);

    /// <summary>Whether a folder is there (where the picker starts when it is given none).</summary>
    bool FolderExists(string folder);

    /// <summary>Asks whether <paramref name="file"/> (it is there) may be replaced.</summary>
    Task<bool> Replace(string file);
}

/// <summary>
/// The in-game file picker (GM1-P15; Docs/Game/Foundation.md, "Save Character and Load Character"): the game's own view
/// on the engine's file browser model (<see cref="FileBrowser"/>), a window in the old windows' look. On the left the
/// places (Documents, home, the roots, then "Recent" and the recent places); on the right the folder shown (named from the
/// place that holds it: "Documents / old saves"), Up, the listing (folders, then <c>.json</c> files, each with its size and
/// date; only the rows near the view are made, so a long folder costs no more than a short one) and the file name field;
/// under them what can't be done; Cancel and the request's button. The listing is one stop for Tab: the arrow keys, Home
/// and End select, Enter opens a folder or picks a file (as a double click), Backspace goes up. The name field reads what
/// is typed (<see cref="FileBrowser.ReadPath(string, string)"/>) on Enter or the button: a folder is gone to, a file
/// picked (to save: a new name too; a file that is there replaced only after a question); Tab completes its last name.
/// <see cref="Picked"/> finishes with the file, or null when cancelled.
/// </summary>
public sealed class FilePicker
{
    /// <summary>A row's height in the listing (CSS px; picker.css's <c>.picker-row</c>).</summary>
    public const int RowHeight = 30;

    /// <summary>How many rows are made beyond the view on each side.</summary>
    public const int Overscan = 10;

    /// <summary>The endings the picker shows (the old native dialogs' filter "Stat Fick character", <c>.json</c>).</summary>
    public static readonly IReadOnlyCollection<string> Endings = [".json"];

    readonly IPickerHost _host;
    readonly TaskCompletionSource<string?> _picked = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly UiElement _places, _where, _up, _listing, _name, _message, _accept;
    IReadOnlyList<FolderEntry> _entries = [];
    IReadOnlyList<PickerPlace> _placeItems = [];
    (int First, int Last) _made = (-1, -1);
    bool _asking;

    /// <summary>The picker for <paramref name="mode"/>, starting in <paramref name="start"/> (else the first place that is there), its name field holding <paramref name="name"/>.</summary>
    public FilePicker(IPickerHost host, PickerMode mode, string? start, string name = "")
    {
        _host = host;
        Mode = mode;
        Layer = Markup.Element(mode == PickerMode.Save ? SaveFrame : LoadFrame);
        UiElement Find(string id) => Layer.Descendants().First(e => e.Id == id);
        _places = Find("picker-places");
        _where = Find("picker-where");
        _up = Find("picker-up");
        _listing = Find("picker-listing");
        _name = Find("picker-name");
        _message = Find("picker-message");
        _accept = Find("picker-accept");
        foreach (var cancel in Layer.Descendants().Where(e => e.HasAttribute("data-picker-cancel")))
            cancel.AddEventListener("click", _ => Cancel());
        _up.AddEventListener("click", _ => Up());
        _accept.AddEventListener("click", _ => Choose());
        _listing.AddEventListener<UiKeyboardEvent>("keydown", ListingKey);
        _listing.AddEventListener("scroll", _ => MakeRows());
        _name.AddEventListener<UiKeyboardEvent>("keydown", NameKey);
        _name.Value = name;
        ShowPlaces();
        GoTo(start ?? _placeItems.FirstOrDefault(p => host.FolderExists(p.Folder))?.Folder ?? _placeItems.FirstOrDefault()?.Folder ?? Path.GetFullPath("."), first: true);
    }

    /// <summary>What it picks a file for.</summary>
    public PickerMode Mode { get; }

    /// <summary>Its window (a <c>.modal-layer</c>).</summary>
    public UiElement Layer { get; }

    /// <summary>Finished with the file picked (a full path), or null when cancelled.</summary>
    public Task<string?> Picked => _picked.Task;

    /// <summary>The folder shown (a full path).</summary>
    public string Folder { get; private set; } = "";

    /// <summary>What the listing shows, in its order.</summary>
    public IReadOnlyList<FolderEntry> Entries => _entries;

    /// <summary>The selected entry's index, or -1.</summary>
    public int Selected { get; private set; } = -1;

    /// <summary>What the picker says under the listing (empty when nothing).</summary>
    public string Message => _message.TextContent;

    /// <summary>The file name field.</summary>
    public UiElement NameField => _name;

    /// <summary>The listing (its rows between two spacers).</summary>
    public UiElement Listing => _listing;

    /// <summary>The element the focus goes to when the picker opens: the name field to save (its name before the ending selected), the listing to load.</summary>
    public UiElement Focus => Mode == PickerMode.Save ? _name : _listing;

    /// <summary>The rows made now, in order (those near the view).</summary>
    public IReadOnlyList<UiElement> Rows => [.. _listing.ElementChildren.Where(e => e.HasClass("picker-row"))];

    /// <summary>The places' buttons, in order.</summary>
    public IReadOnlyList<UiElement> PlaceButtons => [.. _places.ElementChildren.Where(e => e.Tag == "button")];

    /// <summary>Selects the name before its ending in the name field (to save), once the field is shown and focused.</summary>
    public void SelectName()
    {
        var name = _name.Value;
        var stem = Path.GetFileNameWithoutExtension(name);
        _name.SetSelectionRange(0, stem.Length);
    }

    /// <summary>Cancelled: finishes with null.</summary>
    public void Cancel() => _picked.TrySetResult(null);

    void ShowPlaces()
    {
        _placeItems = _host.Places();
        _places.ClearChildren();
        var recentShown = false;
        foreach (var place in _placeItems)
        {
            if (place.Kind == BrowserPlaceKind.Recent && !recentShown)
            {
                _places.Append("p", "picker-places-title", "Recent");
                recentShown = true;
            }
            var button = _places.Append("button", "picker-place", place.Label);
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
            entries = _host.List(full);
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
            said = why;
        }
        Folder = full;
        _entries = entries;
        Selected = -1;
        _where.TextContent = Where(full);
        _where.SetAttribute("title", full);
        _up.ToggleAttribute("disabled", Path.GetDirectoryName(full) is null);
        foreach (var button in PlaceButtons)
            button.ToggleClass("current", string.Equals(button.GetAttribute("title"), full, FileBrowser.NameComparison));
        _listing.ScrollTop = 0;
        _made = (-1, -1);
        MakeRows();
        Say(said);
    }

    /// <summary>Goes to the folder above the one shown (nothing at a root).</summary>
    public void Up()
    {
        if (Path.GetDirectoryName(Folder) is { } parent)
            GoTo(parent);
    }

    /// <summary>
    /// The folder as the picker names it: from the place that holds it (the deepest of Documents and home: "Documents /
    /// old saves"), else its full path.
    /// </summary>
    public string Where(string folder)
    {
        PickerPlace? best = null;
        foreach (var place in _placeItems)
        {
            if (place.Kind is not (BrowserPlaceKind.Documents or BrowserPlaceKind.Home))
                continue;
            if (Relative(place.Folder, folder) is not null && (best is null || place.Folder.Length > best.Folder.Length))
                best = place;
        }
        if (best is null)
            return folder;
        var rest = Relative(best.Folder, folder)!;
        return rest.Length == 0 ? best.Label : best.Label + " / " + string.Join(" / ", rest.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries));
    }

    static string? Relative(string place, string folder)
    {
        var root = Path.TrimEndingDirectorySeparator(place);
        if (string.Equals(root, folder, FileBrowser.NameComparison))
            return "";
        var prefix = root + Path.DirectorySeparatorChar;
        return folder.StartsWith(prefix, FileBrowser.NameComparison) ? folder[prefix.Length..] : null;
    }

    /// <summary>
    /// Makes the rows near the view (those in it and <see cref="Overscan"/> on each side), between two spacers as high as
    /// the rows they stand for; again whenever the listing scrolls past them. An empty folder says so.
    /// </summary>
    void MakeRows()
    {
        if (_entries.Count == 0)
        {
            if (_made == (0, 0))
                return;
            _listing.ClearChildren();
            _listing.Append("p", "picker-empty", "No folders or .json files here.");
            _made = (0, 0);
            return;
        }
        var view = Math.Max(_listing.ClientHeight, RowHeight);
        var top = (int)Math.Max(0, _listing.ScrollTop);
        var first = Math.Max(0, top / RowHeight - Overscan);
        var last = Math.Min(_entries.Count, (top + view + RowHeight - 1) / RowHeight + Overscan);
        if ((first, last) == _made)
            return;
        _made = (first, last);
        _listing.ClearChildren();
        Spacer(first);
        for (var i = first; i < last; i++)
            _listing.Append(Row(i));
        Spacer(_entries.Count - last);
    }

    void Spacer(int rows)
    {
        if (rows <= 0)
            return;
        var spacer = _listing.Append("div", "picker-spacer");
        spacer.SetAttribute("aria-hidden", "true");
        spacer.SetAttribute("style", $"height: {rows * RowHeight}px");
    }

    UiElement Row(int index)
    {
        var entry = _entries[index];
        var row = new UiElement("div") { ClassName = entry.IsFolder ? "picker-row is-folder" : "picker-row" };
        row.SetAttribute("role", "option");
        row.SetAttribute("data-index", index.ToString(CultureInfo.InvariantCulture));
        var selected = index == Selected;
        row.SetAttribute("aria-selected", selected ? "true" : "false");
        if (selected)
            row.AddClass("selected");
        row.Append("span", "picker-name-cell", entry.IsFolder ? entry.Name + "/" : entry.Name);
        row.Append("span", "picker-size-cell", entry.IsFolder ? "" : Size(entry.Size));
        row.Append("span", "picker-date-cell", entry.IsFolder ? "" : ScreenTimes.Write(new DateTimeOffset(DateTime.SpecifyKind(entry.LastWrittenUtc, DateTimeKind.Utc)), _host.TimeZone));
        row.AddEventListener<UiMouseEvent>("mousedown", e =>
        {
            if (e.Button == 0)
                Select(index);
        });
        row.AddEventListener("dblclick", _ => Activate(index));
        return row;
    }

    /// <summary>A file's size as the picker writes it: bytes below 1 KB, then KB, MB, GB and TB of 1,024, one decimal below 10.</summary>
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

    /// <summary>
    /// Selects the entry <paramref name="index"/> (-1: none) and scrolls it into view (the nearest edge). To save, a file
    /// selected puts its name into the name field.
    /// </summary>
    public void Select(int index)
    {
        if (index < -1 || index >= _entries.Count)
            index = -1;
        Selected = index;
        // The rows made keep their elements (a double click's second press meets the row of its first).
        foreach (var row in Rows)
        {
            var selected = row.GetAttribute("data-index") == index.ToString(CultureInfo.InvariantCulture);
            row.ToggleClass("selected", selected);
            row.SetAttribute("aria-selected", selected ? "true" : "false");
        }
        if (index < 0)
            return;
        var view = _listing.ClientHeight;
        var rowTop = index * RowHeight;
        if (rowTop < _listing.ScrollTop)
            _listing.ScrollTop = rowTop;
        else if (view > 0 && rowTop + RowHeight > _listing.ScrollTop + view)
            _listing.ScrollTop = rowTop + RowHeight - view;
        if (Mode == PickerMode.Save && !_entries[index].IsFolder)
            _name.Value = _entries[index].Name;
        MakeRows();
    }

    /// <summary>The picker is shown (laid out in its window): the rows for its view are made.</summary>
    public void Shown()
    {
        _made = (-1, -1);
        MakeRows();
    }

    /// <summary>Opens the entry's folder or picks its file (a double click, Enter).</summary>
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
    /// The request's button: what the name field names when something is typed, else the selected entry, else what is
    /// missing ("Choose a file." / "Type a file name.").
    /// </summary>
    public void Choose()
    {
        if (_name.Value.Length > 0)
            ReadTyped();
        else if (Selected >= 0)
            Activate(Selected);
        else
            Say(Mode == PickerMode.Save ? "Type a file name." : "Choose a file.");
    }

    /// <summary>Reads the name field: a folder is gone to (the field emptied), a file picked, to save a new name too.</summary>
    public void ReadTyped()
    {
        var typed = _name.Value;
        if (typed.Length == 0)
        {
            Choose();
            return;
        }
        var read = FileBrowser.ReadPath(Folder, typed);
        switch (read.Kind)
        {
            case TypedPathKind.Folder:
                GoTo(read.FullPath);
                if (Folder == read.FullPath)
                    _name.Value = "";
                break;
            case TypedPathKind.File:
                _ = Pick(read.FullPath);
                break;
            case TypedPathKind.New when Mode == PickerMode.Save:
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

    /// <summary>Picks <paramref name="file"/>: to save over a file that is there, asked first (nothing else is picked meanwhile).</summary>
    async Task Pick(string file)
    {
        if (_asking || _picked.Task.IsCompleted)
            return;
        if (Mode == PickerMode.Save && _host.FileExists(file))
        {
            _asking = true;
            try
            {
                if (!await _host.Replace(file))
                    return;
            }
            finally
            {
                _asking = false;
            }
        }
        _picked.TrySetResult(file);
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

    void NameKey(UiKeyboardEvent e)
    {
        if (e.CtrlKey || e.AltKey || e.MetaKey)
            return;
        if (e.Key == "Enter" && !e.ShiftKey)
        {
            ReadTyped();
            e.PreventDefault();
        }
        else if (e.Key == "Tab" && !e.ShiftKey && _name.Value.Length > 0)
        {
            var completion = FileBrowser.Complete(Folder, _name.Value, Endings);
            if (completion.Text == _name.Value)
                return;
            _name.Value = completion.Text;
            _name.SetSelectionRange(completion.Text.Length, completion.Text.Length);
            e.PreventDefault();
        }
    }

    const string Body = """
            <div class="picker">
              <nav class="picker-places" id="picker-places" aria-label="Places"></nav>
              <div class="picker-main">
                <div class="picker-bar">
                  <button class="backup-button picker-up" id="picker-up" type="button" title="The folder above (Backspace)">Up</button>
                  <span class="picker-where" id="picker-where"></span>
                </div>
                <div class="picker-head" aria-hidden="true"><span class="picker-name-cell">Name</span><span class="picker-size-cell">Size</span><span class="picker-date-cell">Modified</span></div>
                <div class="picker-listing" id="picker-listing" role="listbox" tabindex="0" aria-label="Folders and character files"></div>
                <label class="picker-field" for="picker-name"><span>File name</span><input id="picker-name" type="text" spellcheck="false" autocomplete="off" /></label>
                <p class="picker-message" id="picker-message" role="status" aria-live="polite"></p>
              </div>
            </div>
        """;

    const string SaveFrame = $$"""
        <div class="modal-layer picker-layer" hidden>
          <button class="modal-backdrop" type="button" data-picker-cancel aria-label="Cancel"></button>
          <section class="spell-picker-dialog picker-dialog" role="dialog" aria-modal="true" aria-labelledby="picker-title">
            <header class="dialog-header compact-dialog-header">
              <div><p class="eyebrow">STAT FICK CHARACTER</p><h2 id="picker-title">Save <em>character</em></h2></div>
              <button class="modal-close" type="button" data-picker-cancel aria-label="Cancel">×</button>
            </header>
            {{Body}}
            <div class="picker-actions">
              <button class="backup-button picker-cancel" type="button" data-picker-cancel>Cancel</button>
              <button class="backup-button picker-accept" id="picker-accept" type="button">Save</button>
            </div>
          </section>
        </div>
        """;

    const string LoadFrame = $$"""
        <div class="modal-layer picker-layer" hidden>
          <button class="modal-backdrop" type="button" data-picker-cancel aria-label="Cancel"></button>
          <section class="spell-picker-dialog picker-dialog" role="dialog" aria-modal="true" aria-labelledby="picker-title">
            <header class="dialog-header compact-dialog-header">
              <div><p class="eyebrow">STAT FICK CHARACTER</p><h2 id="picker-title">Load <em>character</em></h2></div>
              <button class="modal-close" type="button" data-picker-cancel aria-label="Cancel">×</button>
            </header>
            {{Body}}
            <div class="picker-actions">
              <button class="backup-button picker-cancel" type="button" data-picker-cancel>Cancel</button>
              <button class="backup-button picker-accept" id="picker-accept" type="button">Load</button>
            </div>
          </section>
        </div>
        """;
}

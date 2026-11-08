using Statventure.Editor.Form;
using Statventure.Editor.Settings;
using Statventure.Editor.Shell;
using Statventure.Storage;
using Statventure.UI.Dom;

namespace Statventure.Editor.Browser;

/// <summary>
/// What the file browser is asked to pick: the dialog's title and its button's text, the endings of the files it
/// shows (none: any file), whether a name that isn't there yet may be picked (an export's new file), the name to
/// start with in the path field, and the folder to start in (none: the most recent place, else <paramref name="Fallback"/>);
/// whether it picks a folder rather than a file (<paramref name="Folder"/>: the game's folder, M12 step 6), a line said
/// above it (<paramref name="Lead"/>), and a check of what is picked (<paramref name="Accept"/>: why it can't be, said
/// under the listing with the browser left open, or null).
/// </summary>
public sealed record BrowserRequest(string Title, string Button, IReadOnlyCollection<string>? Endings = null, bool NewFile = false,
    string? SuggestedName = null, string? StartFolder = null, BrowserPlaceKind Fallback = BrowserPlaceKind.Mods,
    bool Folder = false, string? Lead = null, Func<string, string?>? Accept = null);

/// <summary>
/// A place the browser lists: its label and folder, and what it is (null: the game's packs, P5, which the editor adds
/// to step 3's places itself).
/// </summary>
public sealed record BrowserPlaceItem(string Label, string Folder, BrowserPlaceKind? Kind);

/// <summary>
/// The editor's file browser (M11-9, P26; Docs/Editor.md, "Opening packs (step 8)"): step 3's model
/// (<see cref="FileBrowser"/>, <see cref="RecentPlaces"/>) on a dialog over the shell (the manager's decision of
/// 2026-11-07: the rest inert, the focus kept inside, Escape cancels): places (the mods folder, the game's packs, the
/// Documents folder, home, the roots, recent places), a folder's listing (folders first, then files with size and
/// date, hidden ones when asked), a path field (a folder goes there, a file is picked, Tab completes its last name)
/// and the endings of what is being picked (<c>.pak</c> to open; any to export). Open, New mod's folder, Save As,
/// import and export all ask through it; it is the JSON tab's export picker (<see cref="IExportFilePicker"/>). The
/// recent places are kept in <c>editor/recent-places.json</c> (G6): read when it first opens, written at each pick.
/// </summary>
public sealed class FileBrowserPart : EditorPart, IExportFilePicker, IFileBrowserHost
{
    /// <summary>The endings of the files a pack can be opened from.</summary>
    public static readonly IReadOnlyCollection<string> PackEndings = [".pak"];

    RecentPlaces? _recent;

    protected internal override void Attach(UiElement? element)
    {
    }

    /// <summary>The time zone the listing's dates are shown in (this machine's; the screens' fixture gives UTC).</summary>
    public TimeZoneInfo TimeZone { get; set; } = TimeZoneInfo.Local;

    /// <summary>The places to list instead of this machine's (the screens' fixture), or null.</summary>
    public Func<IReadOnlyList<BrowserPlaceItem>>? PlacesOverride { get; set; }

    /// <summary>
    /// A folder's listing in place of <see cref="FileBrowser.List"/> (given the folder, the endings and whether hidden
    /// ones show; the screens' fixture lists a folder of its own, so no path of this machine's is in its markup), or null.
    /// </summary>
    public Func<string, IReadOnlyCollection<string>?, bool, IReadOnlyList<FolderEntry>>? ListOverride { get; set; }

    /// <summary>A folder's listing (<see cref="ListOverride"/>, else <see cref="FileBrowser.List"/>).</summary>
    public IReadOnlyList<FolderEntry> List(string folder, IReadOnlyCollection<string>? endings, bool showHidden) =>
        ListOverride is { } list ? list(folder, endings, showHidden) : FileBrowser.List(folder, endings, showHidden);

    UiDocument IFileBrowserHost.Document => View.Document;

    Task<string?> IFileBrowserHost.Ask(string title, string message, params (string Text, string? Value)[] buttons) => View.Dialogs.Ask(title, message, buttons);

    /// <summary>The browser's dialog while it is open, else null.</summary>
    public FileBrowserDialog? Current { get; private set; }

    /// <summary>The recent places (read from the player's <c>editor/recent-places.json</c> when first asked for).</summary>
    public RecentPlaces Recent => _recent ??= RecentPlacesFile.Read(Session.PlayerFolder);

    /// <summary>
    /// The places, in the browser's order: the mods folder, the game's packs (labelled read-only outside the dev setup,
    /// P5), Documents, home, the roots, then the recent places that are folders now (step 3's <see cref="FileBrowser.Places(string, RecentPlaces?)"/>).
    /// </summary>
    public IReadOnlyList<BrowserPlaceItem> Places()
    {
        if (PlacesOverride is { } given)
            return given();
        var places = new List<BrowserPlaceItem>();
        foreach (var place in FileBrowser.Places(Session.ModsFolder, Recent))
        {
            places.Add(new BrowserPlaceItem(Label(place), place.Folder, place.Kind));
            if (place.Kind == BrowserPlaceKind.Mods)
                places.Add(new BrowserPlaceItem(Session.Game.DevSetup ? "Game packs" : "Game packs (read-only)", Session.Game.PacksFolder, null));
        }
        return places;
    }

    /// <summary>How the browser names a place of step 3's model.</summary>
    public static string Label(BrowserPlace place) => place.Kind switch
    {
        BrowserPlaceKind.Mods => "Mods",
        BrowserPlaceKind.Documents => "Documents",
        BrowserPlaceKind.Home => "Home",
        BrowserPlaceKind.Recent => Path.GetFileName(place.Folder) is { Length: > 0 } name ? name : place.Folder,
        _ => place.Folder,
    };

    /// <summary>
    /// Shows the browser for <paramref name="request"/> and gives the file picked (a full path), or null when it is
    /// cancelled. One browser at a time: while one is open, another request gives null at once.
    /// </summary>
    public async Task<string?> Pick(BrowserRequest request)
    {
        if (Current is not null)
            return null;
        var dialog = new FileBrowserDialog(this, request);
        Current = dialog;
        try
        {
            return await dialog.Closed;
        }
        finally
        {
            if (Current == dialog)
                Current = null;
        }
    }

    /// <summary>Asks for a pack to open (<c>.pak</c> files; from the mods folder unless a recent place is known).</summary>
    public Task<string?> PickPack() => Pick(new BrowserRequest("Open a pack", "Open", PackEndings));

    /// <summary>The JSON tab's export (step 9): any file, a new name allowed (an existing one replaced after asking), from Documents unless a recent place is known.</summary>
    public Task<string?> PickExportFile(string title, string suggestedName) =>
        Pick(new BrowserRequest(title, "Export", null, NewFile: true, SuggestedName: suggestedName, Fallback: BrowserPlaceKind.Documents));

    /// <summary>A file (or a folder) was picked: the folder holding it becomes the most recent place, and the recent places are written.</summary>
    public void Picked(string file)
    {
        if (Path.GetDirectoryName(file) is not { Length: > 0 } folder)
            return;
        Recent.Add(folder);
        RecentPlacesFile.Write(Session.PlayerFolder, Recent);
    }
}

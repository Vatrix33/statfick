using Statventure.Editor.Browser;
using Statventure.Editor.Form;
using Statventure.Editor.Settings;
using Statventure.Editor.Shell;
using Statventure.Platform;
using Statventure.Storage;
using Statventure.Tests.Storage;
using Statventure.UI.Dom;
using static Statventure.Tests.Editor.Opening.OpeningTesting;

namespace Statventure.Tests.Editor.Opening;

/// <summary>
/// The file browser (M11-9, P26; Docs/Editor.md, "Opening packs (step 8)"): a dialog over the shell on step 3's model,
/// its places, listing, path field and filters, its keys, and the recent places kept in <c>editor/recent-places.json</c>;
/// on scratch folders, the session's player folder a scratch one.
/// </summary>
public class BrowserTests
{
    /// <summary>A scratch tree: <c>player/mods</c> with a folder, packs and other files, a hidden folder and file.</summary>
    sealed class Scratch : IDisposable
    {
        public readonly StorageFolder Folder = new();

        public Scratch()
        {
            Folder.Write("player/mods/old/older.pak", new byte[3]);
            Folder.Write("player/mods/alpha.pak", new byte[1536]);
            Folder.Write("player/mods/My Mod.pak", new byte[12]);
            Folder.Write("player/mods/notes.txt", new byte[5]);
            Folder.Write("player/mods/.hidden.pak", new byte[1]);
            Folder.Write("player/mods/.cache/x", new byte[1]);
            Folder.Write("player/mods/Zeta.PAK", new byte[2]);
            File.SetLastWriteTimeUtc(Folder["player/mods/alpha.pak"], new DateTime(2026, 11, 7, 16, 50, 0, DateTimeKind.Utc));
        }

        public string Mods => Folder["player/mods"];

        public EditorHarness Editor()
        {
            var editor = new EditorHarness(1000, 640);
            editor.Session.PlayerFolder = Folder["player"];
            editor.Browser().TimeZone = TimeZoneInfo.Utc;
            return editor;
        }

        public void Dispose() => Folder.Dispose();
    }

    static string[] Names(FileBrowserDialog dialog) => [.. dialog.Entries.Select(e => e.Name)];

    static string[] RowTexts(FileBrowserDialog dialog, int row) =>
        [.. dialog.Listing.ElementChildren.ElementAt(row).ElementChildren.Select(c => c.TextContent)];

    [Fact]
    public void OpenShowsTheModsFolderWithFoldersAndPacks()
    {
        using var scratch = new Scratch();
        using var editor = scratch.Editor();
        var picked = editor.Run(() => editor.Browser().PickPack());
        var dialog = editor.Browser().Current!;
        Assert.Same(dialog.Dialog, editor.View.Dialogs.Top);
        Assert.True(dialog.Dialog.Element.HasClass("browser-dialog"));
        Assert.Equal("Open a pack", editor.Asked().Title);
        Assert.Equal(["Open", "Cancel"], editor.Buttons());
        Assert.Equal(Path.GetFullPath(scratch.Mods), dialog.Folder);
        // Folders first, then the .pak files (any case), each ignoring case; hidden ones and other files left out.
        Assert.Equal(["old", "alpha.pak", "My Mod.pak", "Zeta.PAK"], Names(dialog));
        Assert.Equal(["old/", "", ""], RowTexts(dialog, 0));
        Assert.Equal(["alpha.pak", "1.5 KB", "2026-11-07 16:50"], RowTexts(dialog, 1));
        Assert.Equal("12 bytes", RowTexts(dialog, 2)[1]);
        Assert.Equal(["Name", "Size", "Modified"], editor.Document.QuerySelectorAll(".browser-head span").Select(s => s.TextContent));
        Assert.Equal("Mods", editor.Q(".browser-where").TextContent);
        Assert.Equal(dialog.Folder, editor.Q(".browser-where").GetAttribute("title"));
        // The listing has the focus (it was autofocused), nothing selected; the rest of the editor is inert.
        Assert.Same(dialog.Listing, editor.Document.FocusedElement);
        Assert.Equal(-1, dialog.Selected);
        Assert.True(editor.View.Shell.Inert);
        // Hidden ones when asked.
        editor.Click(dialog.HiddenBox);
        Assert.Equal([".cache", "old", ".hidden.pak", "alpha.pak", "My Mod.pak", "Zeta.PAK"], Names(dialog));
        // Gone into a folder with the box ticked, its hidden ones too; the box unticked, the row selected stays so.
        dialog.GoTo(scratch.Folder["player"]);
        dialog.GoTo(scratch.Mods);
        Assert.Equal([".cache", "old", ".hidden.pak", "alpha.pak", "My Mod.pak", "Zeta.PAK"], Names(dialog));
        dialog.Select(3);
        editor.Click(dialog.HiddenBox);
        Assert.Equal(["old", "alpha.pak", "My Mod.pak", "Zeta.PAK"], Names(dialog));
        Assert.Equal(1, dialog.Selected);
        Assert.True(dialog.Listing.ElementChildren.ElementAt(1).HasClass("selected"));
        editor.Key("Escape", "Escape");
        Assert.True(picked.IsCompleted);
        Assert.Null(picked.Done());
        Assert.Null(editor.Browser().Current);
    }

    [Fact]
    public void ThePlacesAreTheModsTheGamesPacksDocumentsHomeTheRootsAndRecentPlaces()
    {
        using var scratch = new Scratch();
        using var editor = scratch.Editor();
        var places = editor.Browser().Places();
        var model = FileBrowser.Places(editor.Session.ModsFolder, editor.Browser().Recent);
        Assert.Equal(model.Count + 1, places.Count);
        Assert.Equal(("Mods", Path.GetFullPath(scratch.Mods), (BrowserPlaceKind?)BrowserPlaceKind.Mods), (places[0].Label, places[0].Folder, places[0].Kind));
        // The dev setup's game packs take changes; a player's are read-only.
        Assert.Equal(("Game packs", editor.Session.Game.PacksFolder, (BrowserPlaceKind?)null), (places[1].Label, places[1].Folder, places[1].Kind));
        Assert.Equal(model.Skip(1).Select(p => (FileBrowserPart.Label(p), p.Folder, (BrowserPlaceKind?)p.Kind)), places.Skip(2).Select(p => (p.Label, p.Folder, p.Kind)));
        Assert.Equal(["Documents", "Home", "/"], places.Skip(2).Select(p => p.Label));
        using var game = new ScratchGame();
        using var session = game.Session(devSetup: false);
        using var view = new EditorView(session, editor.UiData);
        Assert.Equal("Game packs (read-only)", view.Part<FileBrowserPart>().Places()[1].Label);
        // Each place a button; a click goes there.
        var picked = editor.Run(() => editor.Browser().PickPack());
        var dialog = editor.Browser().Current!;
        Assert.Equal(places.Select(p => p.Label), dialog.PlaceButtons.Select(b => b.TextContent));
        Assert.Equal(places.Select(p => p.Folder), dialog.PlaceButtons.Select(b => b.GetAttribute("title")));
        Assert.True(dialog.PlaceButtons[0].HasClass("current"));
        editor.Click(dialog.PlaceButtons[1]);
        Assert.Equal(editor.Session.Game.PacksFolder, dialog.Folder);
        Assert.Equal(["audio.pak", "ui.pak"], Names(dialog));
        Assert.Equal("Game packs", editor.Q(".browser-where").TextContent);
        Assert.True(dialog.PlaceButtons[1].HasClass("current"));
        Assert.False(dialog.PlaceButtons[0].HasClass("current"));
        editor.Answer("Cancel");
        Assert.Null(picked.Done());
    }

    [Fact]
    public void TheKeysMoveTheSelectionEnterAFolderAndPickAFile()
    {
        using var scratch = new Scratch();
        using var editor = scratch.Editor();
        var picked = editor.Run(() => editor.Browser().PickPack());
        var dialog = editor.Browser().Current!;
        // Up with nothing selected: the last; keys with Ctrl or Alt aren't the listing's.
        editor.Key("ArrowUp", "ArrowUp");
        Assert.Equal(3, dialog.Selected);
        editor.Key("Home", "Home", KeyModifiers.Control);
        editor.Key("ArrowUp", "ArrowUp", KeyModifiers.Alt);
        Assert.Equal(3, dialog.Selected);
        editor.Key("Home", "Home");
        Assert.Equal(0, dialog.Selected);
        editor.Key("ArrowUp", "ArrowUp");
        Assert.Equal(0, dialog.Selected);
        dialog.Select(-1);
        editor.Key("ArrowDown", "ArrowDown");
        Assert.Equal(0, dialog.Selected);
        Assert.True(dialog.Listing.ElementChildren.First().HasClass("selected"));
        Assert.Equal("true", dialog.Listing.ElementChildren.First().GetAttribute("aria-selected"));
        editor.Key("End", "End");
        Assert.Equal(3, dialog.Selected);
        editor.Key("ArrowDown", "ArrowDown");
        Assert.Equal(3, dialog.Selected);
        editor.Key("ArrowUp", "ArrowUp");
        Assert.Equal(2, dialog.Selected);
        Assert.False(dialog.Listing.ElementChildren.ElementAt(3).HasClass("selected"));
        editor.Key("Home", "Home");
        Assert.Equal(0, dialog.Selected);
        // Enter goes into a folder; Backspace goes up (the folder is then where it was).
        editor.Key("Enter", "Enter");
        Assert.Equal(Path.Combine(Path.GetFullPath(scratch.Mods), "old"), dialog.Folder);
        Assert.Equal("Mods / old", editor.Q(".browser-where").TextContent);
        Assert.Equal(["older.pak"], Names(dialog));
        editor.Key("Backspace", "Backspace");
        Assert.Equal(Path.GetFullPath(scratch.Mods), dialog.Folder);
        // The Up button too, and at a root it is disabled.
        editor.Click(editor.Q(".browser-up"));
        Assert.Equal(scratch.Folder["player"], dialog.Folder);
        dialog.GoTo("/");
        Assert.Equal(("/", ""), (dialog.Folder, dialog.Message));
        Assert.True(editor.Q(".browser-up").HasAttribute("disabled"));
        Assert.Equal("/", editor.Q(".browser-where").TextContent);
        dialog.GoTo(scratch.Mods);
        // Enter on a file picks it: the dialog closes with it, its folder the most recent place, written.
        dialog.Listing.Focus();
        editor.Key("ArrowDown", "ArrowDown");
        editor.Key("ArrowDown", "ArrowDown");
        editor.Key("Enter", "Enter");
        Assert.True(picked.IsCompleted);
        Assert.Equal(Path.Combine(Path.GetFullPath(scratch.Mods), "alpha.pak"), picked.Done());
        Assert.Null(editor.View.Dialogs.Top);
        Assert.Equal([Path.GetFullPath(scratch.Mods)], editor.Browser().Recent.Folders);
        Assert.Equal([Path.GetFullPath(scratch.Mods)], RecentPlacesFile.Read(scratch.Folder["player"]).Folders);
    }

    [Fact]
    public void AClickSelectsADoubleClickOpensAndTheButtonActs()
    {
        using var scratch = new Scratch();
        using var editor = scratch.Editor();
        var picked = editor.Run(() => editor.Browser().PickPack());
        var dialog = editor.Browser().Current!;
        // The button with nothing chosen says so.
        editor.Answer("Open");
        Assert.Equal("Choose a file.", dialog.Message);
        Assert.False(picked.IsCompleted);
        editor.Click(dialog.Listing.ElementChildren.First());
        Assert.Equal(0, dialog.Selected);
        Assert.Same(dialog.Listing, editor.Document.FocusedElement);
        editor.Answer("Open");
        Assert.EndsWith("old", dialog.Folder);
        Assert.Equal("", dialog.Message);
        var older = dialog.Listing.ElementChildren.First();
        editor.Input.PointerMove(editor.Middle(older));
        editor.Input.PointerDown(editor.Middle(older), MouseButton.Left);
        editor.Input.PointerUp(editor.Middle(older), MouseButton.Left);
        editor.Input.PointerDown(editor.Middle(older), MouseButton.Left);
        editor.Input.PointerUp(editor.Middle(older), MouseButton.Left);
        editor.Frame();
        Assert.True(picked.IsCompleted);
        Assert.Equal(Path.Combine(Path.GetFullPath(scratch.Mods), "old", "older.pak"), picked.Done());
    }

    [Fact]
    public void ThePathFieldGoesToAFolderPicksAFileAndSaysWhatItCant()
    {
        using var scratch = new Scratch();
        using var editor = scratch.Editor();
        var picked = editor.Run(() => editor.Browser().PickPack());
        var dialog = editor.Browser().Current!;
        editor.Click(dialog.PathField);
        editor.TypeKeys("old");
        editor.Key("Enter", "Enter");
        Assert.EndsWith("old", dialog.Folder);
        Assert.Equal("", dialog.PathField.Value);
        editor.TypeKeys("../notes.txt");
        editor.Key("Enter", "Enter");
        Assert.Equal("notes.txt isn't a .pak file.", dialog.Message);
        editor.Key("a", "KeyA", KeyModifiers.Control);
        editor.TypeKeys("gone.pak");
        editor.Key("Enter", "Enter");
        Assert.Equal("gone.pak isn't there.", dialog.Message);
        editor.Key("a", "KeyA", KeyModifiers.Control);
        editor.TypeKeys("nothing/");
        editor.Key("Enter", "Enter");
        Assert.Equal("nothing/ isn't there.", dialog.Message);
        dialog.PathField.Value = "bad\0name";
        editor.Answer("Open");
        Assert.Equal("bad\0name isn't a path here.", dialog.Message);
        Assert.False(picked.IsCompleted);
        // Tab completes the last name when it can, else moves the focus.
        dialog.PathField.Focus();
        editor.Key("a", "KeyA", KeyModifiers.Control);
        editor.TypeKeys("../al");
        editor.Key("Tab", "Tab");
        Assert.Equal("../alpha.pak", dialog.PathField.Value);
        Assert.Same(dialog.PathField, editor.Document.FocusedElement);
        // Shift+Tab never completes.
        editor.Key("Backspace", "Backspace");
        editor.Key("Backspace", "Backspace");
        editor.Key("Tab", "Tab", KeyModifiers.Shift);
        Assert.NotSame(dialog.PathField, editor.Document.FocusedElement);
        Assert.Equal("../alpha.p", dialog.PathField.Value);
        dialog.PathField.Focus();
        editor.Key("Tab", "Tab");
        Assert.Equal("../alpha.pak", dialog.PathField.Value);
        editor.Key("Tab", "Tab");
        Assert.NotSame(dialog.PathField, editor.Document.FocusedElement);
        Assert.Equal("../alpha.pak", dialog.PathField.Value);
        dialog.PathField.Focus();
        editor.Key("Enter", "Enter");
        Assert.True(picked.IsCompleted);
        Assert.Equal(Path.Combine(Path.GetFullPath(scratch.Mods), "alpha.pak"), picked.Done());
    }

    [Fact]
    public void AnExportTakesANewNameAndAsksBeforeReplacingAFile()
    {
        using var scratch = new Scratch();
        using var editor = scratch.Editor();
        // Documents (the run's redirected one) when no place is recent; any file is listed.
        var first = editor.Run(() => editor.Browser().PickExportFile("Export audio/rooms/hall", "hall.json"));
        var dialog = editor.Browser().Current!;
        Assert.Equal(Path.TrimEndingDirectorySeparator(Path.GetFullPath(DocumentsFolder.ForThisMachine())), dialog.Folder);
        Assert.Equal("Export audio/rooms/hall", editor.Asked().Title);
        Assert.Equal(["Export", "Cancel"], editor.Buttons());
        Assert.Same(dialog.PathField, editor.Document.FocusedElement);
        Assert.Equal("hall.json", dialog.PathField.Value);
        Assert.Equal((0, 4), (dialog.PathField.SelectionStart, dialog.PathField.SelectionEnd));
        dialog.GoTo(scratch.Mods);
        Assert.Equal(["old", "alpha.pak", "My Mod.pak", "notes.txt", "Zeta.PAK"], Names(dialog));
        editor.Key("Enter", "Enter");
        Assert.True(first.IsCompleted);
        Assert.Equal(Path.Combine(Path.GetFullPath(scratch.Mods), "hall.json"), first.Done());
        // The next export starts in the most recent place; an existing file asks first.
        var second = editor.Run(() => editor.Browser().PickExportFile("Export notes", "notes.txt"));
        dialog = editor.Browser().Current!;
        Assert.Equal(Path.GetFullPath(scratch.Mods), dialog.Folder);
        editor.Answer("Export");
        Assert.Equal(("Replace the file?", "notes.txt is there already. Replace it?"), editor.Asked());
        editor.Answer("Cancel");
        Assert.False(second.IsCompleted);
        Assert.Same(dialog.Dialog, editor.View.Dialogs.Top);
        editor.Answer("Export");
        editor.Answer("Replace");
        Assert.True(second.IsCompleted);
        Assert.Equal(Path.Combine(Path.GetFullPath(scratch.Mods), "notes.txt"), second.Done());
        // With nothing typed, the button says so.
        var third = editor.Run(() => editor.Browser().PickExportFile("Export", "x.json"));
        editor.Key("a", "KeyA", KeyModifiers.Control);
        editor.Key("Delete", "Delete");
        editor.Answer("Export");
        Assert.Equal("Type a file name.", editor.Browser().Current!.Message);
        editor.Key("Escape", "Escape");
        Assert.Null(third.Done());
    }

    [Fact]
    public void TheRecentPlacesAreReadWhenTheBrowserFirstOpensAndOneBrowserAtATime()
    {
        using var scratch = new Scratch();
        var old = Path.Combine(Path.GetFullPath(scratch.Mods), "old");
        RecentPlacesFile.Write(scratch.Folder["player"], new RecentPlaces([scratch.Folder["gone"], old]));
        using var editor = scratch.Editor();
        var picked = editor.Run(() => editor.Browser().PickPack());
        var dialog = editor.Browser().Current!;
        // The first recent place that is a folder now; the places list it under "Recent".
        Assert.Equal(old, dialog.Folder);
        Assert.Equal("Mods / old", editor.Q(".browser-where").TextContent);
        Assert.Equal("Recent", editor.Q(".browser-places-title").TextContent);
        Assert.Equal(("old", old, (BrowserPlaceKind?)BrowserPlaceKind.Recent), (dialog.PlaceItems[^1].Label, dialog.PlaceItems[^1].Folder, dialog.PlaceItems[^1].Kind));
        // Another request while one is open gives nothing.
        var another = editor.Run(() => editor.Browser().PickPack());
        Assert.True(another.IsCompleted);
        Assert.Null(another.Done());
        Assert.Same(dialog, editor.Browser().Current);
        editor.Key("Escape", "Escape");
        Assert.Null(picked.Done());
        // A folder that isn't there is said, and the browser stays where it was.
        picked = editor.Run(() => editor.Browser().PickPack());
        dialog = editor.Browser().Current!;
        dialog.GoTo(scratch.Folder["nowhere"]);
        Assert.Equal($"{scratch.Folder["nowhere"]} isn't there.", dialog.Message);
        Assert.Equal(old, dialog.Folder);
        editor.Key("Escape", "Escape");
    }

    [Fact]
    public void AModsFolderThatIsntThereYetIsShownEmpty()
    {
        using var folder = new StorageFolder();
        using var editor = new EditorHarness(1000, 640);
        editor.Session.PlayerFolder = folder["player"];
        var picked = editor.Run(() => editor.Browser().Pick(new BrowserRequest("Open a pack", "Open", FileBrowserPart.PackEndings, StartFolder: folder["player/elsewhere"])));
        var dialog = editor.Browser().Current!;
        Assert.Equal(folder["player/elsewhere"], dialog.Folder);
        Assert.Empty(dialog.Entries);
        Assert.Equal("This folder isn't there yet.", dialog.Message);
        Assert.Equal("No folders or .pak files here.", editor.Q(".browser-empty").TextContent);
        Assert.False(Directory.Exists(folder["player/elsewhere"]));
        editor.Key("Escape", "Escape");
        Assert.Null(picked.Done());
    }

    [Fact]
    public void TheBrowserIsTheJsonTabsExportPicker()
    {
        using var editor = new EditorHarness(1000, 640);
        Assert.Same(editor.Browser(), editor.View.Part<FormPart>().ExportPicker);
    }

    [Theory]
    [InlineData(0, "0 bytes")]
    [InlineData(1, "1 byte")]
    [InlineData(1023, "1023 bytes")]
    [InlineData(1024, "1 KB")]
    [InlineData(1536, "1.5 KB")]
    [InlineData(10_240, "10 KB")]
    [InlineData(10_700, "10 KB")]
    [InlineData(10_800, "11 KB")]
    [InlineData(1_040_000, "1016 KB")]
    [InlineData(1_048_576, "1 MB")]
    [InlineData(5_000_000_000, "4.7 GB")]
    [InlineData(2_000_000_000_000_000, "1819 TB")]
    public void SizesAreWrittenInBytesKbMbAndGb(long bytes, string text) => Assert.Equal(text, FileBrowserDialog.Size(bytes));

    [Fact]
    public void AFolderIsNamedFromThePlaceItIsIn()
    {
        using var scratch = new Scratch();
        using var editor = scratch.Editor();
        var mods = Path.GetFullPath(scratch.Mods);
        var home = scratch.Folder["home"];
        editor.Browser().PlacesOverride = () =>
        [
            new("Mods", mods, BrowserPlaceKind.Mods),
            new("Game packs", editor.Session.Game.PacksFolder, null),
            new("Documents", Path.Combine(home, "Documents"), BrowserPlaceKind.Documents),
            new("Home", home, BrowserPlaceKind.Home),
            new("/", "/", BrowserPlaceKind.Root),
            new("old", Path.Combine(mods, "old"), BrowserPlaceKind.Recent),
        ];
        var picked = editor.Run(() => editor.Browser().PickPack());
        var dialog = editor.Browser().Current!;
        Assert.Equal(["Mods", "Game packs", "Documents", "Home", "/", "Recent", "old"], editor.Document.QuerySelectorAll(".browser-places > *").Select(e => e.TextContent));
        // The deepest of the places that holds it (not a root or a recent place), else the full path.
        Assert.Equal("Mods", dialog.Where(mods));
        Assert.Equal("Mods / old / deeper", dialog.Where(Path.Combine(mods, "old", "deeper")));
        Assert.Equal(scratch.Folder["player"], dialog.Where(scratch.Folder["player"]));
        Assert.Equal(mods + "-other", dialog.Where(mods + "-other"));
        Assert.Equal("Game packs", dialog.Where(editor.Session.Game.PacksFolder));
        Assert.Equal("Home / projects", dialog.Where(Path.Combine(home, "projects")));
        Assert.Equal("Documents / letters", dialog.Where(Path.Combine(home, "Documents", "letters")));
        Assert.Equal("Home / Documents-old", dialog.Where(Path.Combine(home, "Documents-old")));
        Assert.Equal("/usr/share", dialog.Where("/usr/share"));
        // Dates in the browser's time zone.
        editor.Browser().TimeZone = TimeZoneInfo.CreateCustomTimeZone("plus two", TimeSpan.FromHours(2), "plus two", "plus two");
        Assert.Equal("2026-11-07 18:50", dialog.Date(new DateTime(2026, 11, 7, 16, 50, 0, DateTimeKind.Utc)));
        editor.Key("Escape", "Escape");
        Assert.Null(picked.Done());
    }

    [Fact]
    public void TheSelectedRowIsScrolledIntoView()
    {
        using var scratch = new Scratch();
        for (var i = 0; i < 40; i++)
            scratch.Folder.Write($"player/mods/many/pack-{i:00}.pak", [1]);
        using var editor = scratch.Editor();
        var picked = editor.Run(() => editor.Browser().Pick(new BrowserRequest("Open a pack", "Open", FileBrowserPart.PackEndings, StartFolder: scratch.Folder["player/mods/many"])));
        var dialog = editor.Browser().Current!;
        Assert.Equal(0, dialog.Listing.ScrollTop);
        editor.Key("End", "End");
        Assert.Equal(39, dialog.Selected);
        var listing = dialog.Listing.GetBoundingClientRect();
        var row = dialog.Listing.ElementChildren.Last().GetBoundingClientRect();
        Assert.True(dialog.Listing.ScrollTop > 0);
        Assert.Equal(listing.Y + dialog.Listing.ClientTop + dialog.Listing.ClientHeight, row.Y + row.Height, 0.5f);
        editor.Key("Escape", "Escape");
        Assert.Null(picked.Done());
    }
}

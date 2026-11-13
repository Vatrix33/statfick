using Statventure.Audio;
using Statventure.Editor.Entries;
using Statventure.Editor.Session;
using Statventure.Editor.Shell;
using Statventure.Platform;
using Statventure.Records;
using Statventure.Tests.Storage;
using Statventure.UI.Dom;
using static Statventure.Tests.Editor.Opening.OpeningTesting;

namespace Statventure.Tests.Editor.Opening;

/// <summary>
/// The entry list (P8; Docs/Editor.md, "Opening packs (step 8)"): a tree by path of the open pack's entries over the
/// game's (the repo's packs, the harness's dev setup), the kinds' names, the marks, the head and its notice, the
/// filter and the keys, through the UI's events and the session's changes.
/// </summary>
public class EntryListTests
{
    /// <summary>A mod in a scratch folder: a copy of the game's hall, a room of its own and a text file.</summary>
    static string Mod(StorageFolder folder, Action<Statventure.Packs.PackWriter>? more = null)
    {
        var writer = new Statventure.Packs.PackWriter();
        writer.AddRecord("audio/rooms/hall", new RoomSpec(9, 0, 0));
        writer.AddRecord("audio/rooms/crypt", new RoomSpec(2, 0.5, 0.5));
        writer.Add("notes/todo.txt", "Tune the crypt."u8);
        more?.Invoke(writer);
        var file = folder["my-mod.pak"];
        writer.Write(file);
        return file;
    }

    /// <summary>The first element below <paramref name="element"/> with every class of <paramref name="classes"/> (".a.b").</summary>
    static UiElement? FindIn(UiElement element, string classes) =>
        element.Descendants().FirstOrDefault(e => classes.Split('.', StringSplitOptions.RemoveEmptyEntries).All(e.HasClass));

    static string[] Marks(UiElement row) => [.. row.ElementChildren.First(c => c.HasClass("marks")).ElementChildren.Select(m => m.TextContent)];

    [Fact]
    public void WithNoPackTheListSaysHowToOpenOne()
    {
        using var editor = new EditorHarness();
        var list = editor.List();
        Assert.Equal("No pack open", editor.Q(".entries-pack").TextContent);
        Assert.Equal("", editor.Q(".entries-state").TextContent);
        Assert.Equal("Open a pack with Ctrl+O, or drop a .pak file on the window.", editor.Q(".entries-notice").TextContent);
        Assert.False(editor.Q(".entries-notice").HasAttribute("hidden"));
        Assert.Empty(list.Visible());
        Assert.Equal(("tree", "0"), (list.Tree.GetAttribute("role"), list.Tree.GetAttribute("tabindex")));
    }

    [Fact]
    public void TheTreeListsThePacksEntriesOverTheGamesByPath()
    {
        using var folder = new StorageFolder();
        using var editor = new EditorHarness(1000, 640);
        editor.Session.Open(Mod(folder));
        editor.Frame();
        var list = editor.List();
        Assert.Equal("my-mod.pak", editor.Q(".entries-pack").TextContent);
        Assert.Equal("mod", editor.Q(".entries-state").TextContent);
        Assert.True(editor.Q(".entries-notice").HasAttribute("hidden"));
        // Folders first, then entries, each by name; the top folders open, the rest closed.
        Assert.Equal(["audio", "audio/effects", "audio/instruments", "audio/percussion", "audio/rooms", "audio/themes", "audio/theme-order",
            "notes", "notes/todo.txt", "ui", "ui/fonts", "ui/styles"], list.Visible());
        Assert.True(list.IsOpen("audio"));
        Assert.False(list.IsOpen("audio/rooms"));
        var rooms = list.Folder("audio/rooms")!;
        Assert.Equal(("div", "treeitem", "false", "2", "rooms"), (rooms.Tag, rooms.GetAttribute("role"), rooms.GetAttribute("aria-expanded"), rooms.GetAttribute("aria-level"), rooms.TextContent));
        Assert.False(rooms.HasClass("open"));
        Assert.Null(rooms.GetAttribute("tabindex"));
        Assert.Equal("true", list.Folder("audio")!.GetAttribute("aria-expanded"));
        Assert.True(list.Folder("audio")!.HasClass("open"));
        Assert.Same(list.Tree, rooms.Parent);
        list.Toggle("audio/rooms");
        Assert.True(list.Folder("audio/rooms")!.HasClass("open"));
        var shown = list.Visible();
        var roomsInGame = editor.Session.Entries.Where(e => e.Path.StartsWith("audio/rooms/", StringComparison.Ordinal)).Select(e => e.Path).ToArray();
        Assert.Contains("audio/rooms/crypt", roomsInGame);
        Assert.Equal(roomsInGame.Order(StringComparer.Ordinal), shown.SkipWhile(p => p != "audio/rooms").Skip(1).TakeWhile(p => p.StartsWith("audio/rooms/", StringComparison.Ordinal)));
        // An entry's row: its name, its marks, its kind's name; indented by its depth.
        var crypt = list.Row(editor.Session.Entry("audio/rooms/crypt")!)!;
        Assert.Equal(("div", "treeitem", "false"), (crypt.Tag, crypt.GetAttribute("role"), crypt.GetAttribute("aria-selected")));
        Assert.Equal(["crypt", "", editor.Session.Entry("audio/rooms/crypt")!.KindName], crypt.ElementChildren.Select(c => c.TextContent));
        Assert.Equal("3", crypt.GetAttribute("aria-level"));
        Assert.Equal("4px", list.Folder("audio")!.GetStyle("padding-left"));
        Assert.Equal("18px", rooms.GetStyle("padding-left"));
        Assert.Equal("46px", crypt.GetStyle("padding-left"));
        Assert.Equal("32px", list.Row(editor.Session.Entry("notes/todo.txt")!)!.GetStyle("padding-left"));
        Assert.Equal("File", list.Row(editor.Session.Entry("notes/todo.txt")!)!.ElementChildren.Last().TextContent);
        // The game's entries dimmed; the pack's own, and its copy of the game's hall, not.
        Assert.True(list.Row(editor.Session.Entry("audio/theme-order")!)!.HasClass("game"));
        Assert.False(crypt.HasClass("game"));
        var hall = list.Row(editor.Session.Entry("audio/rooms/hall")!)!;
        Assert.False(hall.HasClass("game"));
        Assert.Equal(["↑"], Marks(hall));
        Assert.Equal("The mod's own copy of the game's entry", FindIn(hall, ".mark")!.GetAttribute("title"));
        Assert.Equal("audio/rooms/hall", hall.GetAttribute("title"));
        // Closing a folder hides what it holds and keeps across rebuilds.
        list.Toggle("audio");
        Assert.Equal(["audio", "notes", "notes/todo.txt", "ui", "ui/fonts", "ui/styles"], list.Visible());
        editor.Session.Add("notes/more", RecordDraft.Blank(typeof(RoomSpec)));
        Assert.False(list.IsOpen("audio"));
        // An entry added in a closed folder opens it (and its folders).
        editor.Session.Add("audio/percussion/new", RecordDraft.Blank(typeof(RoomSpec)));
        Assert.True(list.IsOpen("audio"));
        Assert.True(list.IsOpen("audio/percussion"));
        Assert.Contains("audio/percussion/new", list.Visible());
        list.Toggle("audio");
        Assert.Equal(["audio", "notes", "notes/more", "notes/todo.txt", "ui", "ui/fonts", "ui/styles"], list.Visible());
    }

    [Fact]
    public void TheMarksFollowTheEntries()
    {
        using var folder = new StorageFolder();
        using var editor = new EditorHarness(1000, 640);
        editor.Session.Open(Mod(folder));
        var list = editor.List();
        var session = editor.Session;
        var crypt = session.Entry("audio/rooms/crypt")!;
        UiElement Row(string path) => list.Row(session.Entry(path)!)!;
        list.Toggle("audio/rooms");
        Assert.Empty(Marks(Row("audio/rooms/crypt")));
        // Changed (•) by a commit; an undo takes it back.
        session.Commit(crypt, crypt.Draft!.Set("Seconds", 4.0));
        Assert.Equal(["•"], Marks(Row("audio/rooms/crypt")));
        Assert.Equal("Changed, not saved", FindIn(Row("audio/rooms/crypt"), ".mark.changed")!.GetAttribute("title"));
        session.Undo(crypt);
        Assert.Empty(Marks(Row("audio/rooms/crypt")));
        // Added (+), with a problem (!) while its draft doesn't build (its required fields empty).
        var added = session.Add("audio/rooms/new", RecordDraft.Blank(typeof(RoomSpec)));
        Assert.Equal(["+", "!"], Marks(Row("audio/rooms/new")));
        Assert.Equal("3 problems", FindIn(Row("audio/rooms/new"), ".mark.problem")!.GetAttribute("title"));
        Assert.True(list.IsOpen("audio/rooms"));
        var draft = added.Draft!.Set("Seconds", 1.0).Set("Dark", 0.0);
        session.Commit(added, draft);
        Assert.Equal("A problem", FindIn(Row("audio/rooms/new"), ".mark.problem")!.GetAttribute("title"));
        session.Commit(added, draft.Set("Wet", 0.0));
        Assert.Equal(["+"], Marks(Row("audio/rooms/new")));
        // A game entry changed is the mod's copy now (↑ •), not dimmed; renamed is changed too.
        var theGames = session.Entries.First(e => e.Source == EntrySource.Game && e.Path.StartsWith("audio/rooms/", StringComparison.Ordinal) && e.Path != "audio/rooms/hall");
        Assert.True(Row(theGames.Path).HasClass("game"));
        session.Commit(theGames, theGames.Draft!.Set("Seconds", 7.5));
        Assert.Equal(["↑", "•"], Marks(Row(theGames.Path)));
        Assert.False(Row(theGames.Path).HasClass("game"));
        // The active item (here not the selection) follows its entry when renamed.
        list.Tree.Focus();
        while (list.Active != "audio/rooms/crypt")
            editor.Key("ArrowDown", "ArrowDown");
        Assert.Null(session.Selected);
        session.Rename(crypt, "crypts/deep");
        Assert.Equal("crypts/deep", list.Active);
        Assert.True(list.IsOpen("crypts"));
        Assert.Equal(["•"], Marks(Row("crypts/deep")));
        // Removed: its row goes; the game's hall shows again (dimmed) when the copy goes.
        session.Remove(session.Entry("audio/rooms/hall")!);
        Assert.Empty(Marks(Row("audio/rooms/hall")));
        Assert.True(Row("audio/rooms/hall").HasClass("game"));
        session.Remove(crypt);
        Assert.Null(list.Row(crypt));
        Assert.Null(list.Folder("crypts"));
    }

    [Fact]
    public void APackItCantFullyReadListsEveryUnreadableEntryWithItsReason()
    {
        using var folder = new StorageFolder();
        var file = Mod(folder, w =>
        {
            AddUnreadable(w, "bad/damaged", "damaged");
            AddUnreadable(w, "bad/newer", "newer version");
        });
        Damage(file, "bad/damaged");
        using var editor = new EditorHarness(1000, 640);
        editor.Session.Open(file);
        editor.Frame();
        Assert.Equal("read-only", editor.Q(".entries-state").TextContent);
        var notes = editor.Document.QuerySelectorAll(".entries-note").ToList();
        Assert.Equal(["Read-only: 2 entries can't be read.",
            "bad/damaged: the content doesn't match its hash (damaged)",
            "bad/newer: not a readable audio.room record: audio.room version 2 is newer than this engine reads (version 1)"], notes.Select(n => n.TextContent));
        Assert.Equal([false, true, true], notes.Select(n => n.HasClass("unreadable")));
        var row = editor.List().Row(editor.Session.Entry("bad/damaged")!)!;
        Assert.Equal(["×"], Marks(row));
        Assert.True(row.HasClass("unreadable"));
        Assert.Equal("Unreadable", row.ElementChildren.Last().TextContent);
        Assert.Equal("bad/damaged: the content doesn't match its hash (damaged)", row.GetAttribute("title"));
        Assert.Equal("Can't be read: the content doesn't match its hash (damaged)", FindIn(row, ".mark")!.GetAttribute("title"));
    }

    [Fact]
    public void TheGamesPacksStateAndProblemsShowInTheHead()
    {
        using var editor = new EditorHarness();
        editor.Session.Open(Path.Combine(editor.Session.Game.PacksFolder, "audio.pak"));
        Assert.Equal(("audio.pak", "game pack"), (editor.Q(".entries-pack").TextContent, editor.Q(".entries-state").TextContent));
        Assert.True(editor.Q(".entries-notice").HasAttribute("hidden"));
        // A game pack that can't be read is named.
        using var game = new ScratchGame();
        File.WriteAllText(game.Folder["packs/ui.pak"], "damaged");
        using var session = game.Session();
        var view = new EditorView(session);
        try
        {
            session.Open(game.Pack("player/mods/a.pak", w => w.Add("notes/a.txt", "a"u8)));
            Assert.StartsWith("The game's pack ui.pak: ", view.Document.QuerySelector(".entries-note")!.TextContent);
        }
        finally
        {
            view.Dispose();
        }
    }

    [Fact]
    public void AClickSelectsAnEntryAndOpensAndClosesAFolder()
    {
        using var folder = new StorageFolder();
        using var editor = new EditorHarness(1000, 640);
        editor.Session.Open(Mod(folder));
        editor.Frame();
        var list = editor.List();
        editor.Click(list.Folder("audio/rooms")!);
        Assert.True(list.IsOpen("audio/rooms"));
        Assert.Equal("audio/rooms", list.Active);
        Assert.Same(list.Tree, editor.Document.FocusedElement);
        Assert.Null(editor.Session.Selected);
        var crypt = editor.Session.Entry("audio/rooms/crypt")!;
        editor.Click(list.Row(crypt)!);
        Assert.Same(crypt, editor.Session.Selected);
        Assert.Equal("audio/rooms/crypt", list.Active);
        Assert.True(list.Row(crypt)!.HasClass("selected"));
        Assert.Equal("true", list.Row(crypt)!.GetAttribute("aria-selected"));
        Assert.True(list.Row(crypt)!.HasClass("active"));
        Assert.Same(list.Tree, editor.Document.FocusedElement);
        editor.Click(list.Folder("audio/rooms")!);
        Assert.False(list.IsOpen("audio/rooms"));
        Assert.Equal("false", list.Folder("audio/rooms")!.GetAttribute("aria-expanded"));
        Assert.Same(crypt, editor.Session.Selected);
        // Another selection (the session's) moves the highlight and opens the folders that hold it.
        editor.Session.Select(editor.Session.Entry("audio/rooms/hall"));
        Assert.True(list.IsOpen("audio/rooms"));
        Assert.Equal("audio/rooms/hall", list.Active);
        Assert.False(list.Row(crypt)!.HasClass("selected"));
        Assert.True(list.Row(editor.Session.Entry("audio/rooms/hall")!)!.HasClass("selected"));
        editor.Session.Select(null);
        Assert.Empty(editor.Document.QuerySelectorAll(".entries-item.selected"));
        // Closing a folder that holds the active item makes the folder active.
        editor.Click(list.Row(crypt)!);
        list.Toggle("audio/rooms");
        Assert.Equal("audio/rooms", list.Active);
        Assert.True(list.Folder("audio/rooms")!.HasClass("active"));
    }

    [Fact]
    public void TheKeysMoveThroughTheTreeAndEnterOpens()
    {
        using var folder = new StorageFolder();
        using var editor = new EditorHarness(1000, 640);
        editor.Session.Open(Mod(folder));
        editor.Frame();
        var list = editor.List();
        list.Tree.Focus();
        void Press(string key) => editor.Key(key, key);
        Press("ArrowDown");
        Assert.Equal("audio", list.Active);
        Press("ArrowDown");
        Press("ArrowDown");
        Assert.Equal("audio/instruments", list.Active);
        Press("ArrowUp");
        Assert.Equal("audio/effects", list.Active);
        Press("End");
        Assert.Equal("ui/styles", list.Active);
        Press("Home");
        Assert.Equal("audio", list.Active);
        Assert.Single(list.Tree.ElementChildren, e => e.HasClass("active"));
        // Left closes an open folder; Right opens it, then goes to its first item.
        Press("ArrowLeft");
        Assert.False(list.IsOpen("audio"));
        Press("ArrowDown");
        Assert.Equal("notes", list.Active);
        Press("ArrowUp");
        Press("ArrowRight");
        Assert.True(list.IsOpen("audio"));
        Assert.Equal("audio", list.Active);
        Press("ArrowRight");
        Assert.Equal("audio/effects", list.Active);
        // Left from an item goes to its folder; closing a folder moves the active item out of it.
        Press("ArrowLeft");
        Assert.Equal("audio", list.Active);
        // Enter opens a folder or an entry.
        Press("ArrowDown");
        Press("ArrowDown");
        Press("ArrowDown");
        Press("ArrowDown");
        Assert.Equal("audio/rooms", list.Active);
        Press("Enter");
        Assert.True(list.IsOpen("audio/rooms"));
        Press("ArrowDown");
        Assert.StartsWith("audio/rooms/", list.Active);
        Press("ArrowLeft");
        Assert.Equal("audio/rooms", list.Active);
        Press("ArrowRight");
        Press("ArrowRight");
        var first = list.Active!;
        Press("ArrowRight");
        Assert.Equal(first, list.Active);
        Press("Enter");
        Assert.Equal(first, editor.Session.Selected!.Path);
        Assert.True(list.Row(editor.Session.Selected)!.HasClass("active"));
        Press("ArrowLeft");
        Press("Enter");
        Assert.False(list.IsOpen("audio/rooms"));
        // An arrow doesn't select (Enter does); keys with Ctrl are the shell's.
        Press("ArrowDown");
        Assert.Equal(first, editor.Session.Selected!.Path);
        Assert.Equal("audio/themes", list.Active);
        editor.Key("ArrowUp", "ArrowUp", KeyModifiers.Control);
        Assert.Equal("audio/themes", list.Active);
    }

    [Fact]
    public void TypingFindsTheNextItemByItsName()
    {
        using var folder = new StorageFolder();
        using var editor = new EditorHarness(1000, 640);
        editor.Session.Open(Mod(folder));
        editor.Frame();
        var list = editor.List();
        list.Tree.Focus();
        editor.Key("t", "KeyT");
        Assert.Equal("audio/themes", list.Active);
        editor.Key("h", "KeyH");
        Assert.Equal("audio/themes", list.Active);
        editor.Key("e", "KeyE");
        editor.Key("m", "KeyM");
        editor.Key("e", "KeyE");
        editor.Key("-", "Minus");
        Assert.Equal("audio/theme-order", list.Active);
        // After a pause a letter starts again, from the item after the active one, going round.
        for (var i = 0; i < 61; i++)
            editor.Frame();
        editor.Key("T", "KeyT", KeyModifiers.Shift);
        Assert.Equal("notes/todo.txt", list.Active);
        for (var i = 0; i < 61; i++)
            editor.Frame();
        editor.Key("a", "KeyA");
        Assert.Equal("audio", list.Active);
        // Nothing starts so: the active item stays.
        editor.Key("q", "KeyQ");
        Assert.Equal("audio", list.Active);
        Assert.Null(editor.Session.Selected);
    }

    [Fact]
    public void TheFilterKeepsWhatMatchesByPathOrKind()
    {
        using var folder = new StorageFolder();
        using var editor = new EditorHarness(1000, 640);
        editor.Session.Open(Mod(folder));
        editor.Frame();
        var list = editor.List();
        // Ctrl+F goes to the filter (from anywhere but a dialog).
        list.Tree.Focus();
        editor.Key("f", "KeyF", KeyModifiers.Control);
        Assert.Same(list.Filter, editor.Document.FocusedElement);
        editor.TypeKeys("CRYPT");
        Assert.Equal(["audio", "audio/rooms", "audio/rooms/crypt"], list.Visible());
        Assert.True(list.IsOpen("audio/rooms"));
        // By the kind's name too.
        editor.Key("a", "KeyA", KeyModifiers.Control);
        editor.TypeKeys("file");
        Assert.Equal(["notes", "notes/todo.txt", "ui", "ui/fonts", "ui/fonts/lato-bold.ttf", "ui/fonts/lato-ofl.txt",
            "ui/fonts/lato-regular.ttf", "ui/fonts/lato-semibold.ttf", "ui/fonts/stix-ofl.txt", "ui/fonts/stixtwotext-italic.otf",
            "ui/fonts/stixtwotext-regular.otf"], list.Visible());
        // Down goes into the tree, to its first item; while filtering, Left on a folder goes to its parent (all stay open).
        editor.Key("ArrowDown", "ArrowDown");
        Assert.Same(list.Tree, editor.Document.FocusedElement);
        Assert.Equal("notes", list.Active);
        editor.Key("End", "End");
        editor.Key("ArrowUp", "ArrowUp");
        Assert.Equal("ui/fonts/stixtwotext-italic.otf", list.Active);
        editor.Key("ArrowLeft", "ArrowLeft");
        Assert.Equal("ui/fonts", list.Active);
        editor.Key("ArrowLeft", "ArrowLeft");
        Assert.Equal("ui", list.Active);
        Assert.True(list.IsOpen("ui/fonts"));
        // Nothing matches: it says so.
        list.Filter.Focus();
        editor.Key("a", "KeyA", KeyModifiers.Control);
        editor.TypeKeys("zzz");
        Assert.Empty(list.Visible());
        Assert.Equal("No entry matches the filter.", editor.Q(".entries-none").TextContent);
        // Escape empties it: the folders as they were.
        editor.Key("Escape", "Escape");
        Assert.Equal("", list.Filter.Value);
        Assert.Equal(["audio", "audio/effects", "audio/instruments", "audio/percussion", "audio/rooms", "audio/themes", "audio/theme-order",
            "notes", "notes/todo.txt", "ui", "ui/fonts", "ui/styles"], list.Visible());
        Assert.Same(list.Filter, editor.Document.FocusedElement);
    }

    [Fact]
    public void OnlyTheRowsNearThePanesViewAreMade()
    {
        using var folder = new StorageFolder();
        var file = Mod(folder, w =>
        {
            for (var i = 0; i < 300; i++)
                w.AddRecord($"rooms/many/room-{i:000}", new RoomSpec(1, 0, 0));
        });
        using var editor = new EditorHarness(1000, 640);
        editor.Session.Open(file);
        editor.Frame();
        var list = editor.List();
        var pane = editor.View.Pane(EditorPane.Entries);
        list.Toggle("rooms/many");
        editor.Frame();
        var shown = list.Visible();
        Assert.Equal(314, shown.Count);
        // The rows made: those whose box meets the pane's view, and ten more on either side, between two spacers.
        void Check()
        {
            var (first, last) = list.Made;
            var view = pane.GetBoundingClientRect();
            var tree = list.Tree.GetBoundingClientRect();
            var top = view.Y - tree.Y;
            // Ten rows of overscan (the step's choice).
            Assert.Equal(Math.Max(0, (int)Math.Floor(top / EntryList.RowHeight) - 10), first);
            Assert.Equal(Math.Min(shown.Count, (int)Math.Ceiling((top + pane.ClientHeight) / EntryList.RowHeight) + 10), last);
            var children = list.Tree.ElementChildren.ToList();
            Assert.Equal(shown.Skip(first).Take(last - first), children.Skip(1).SkipLast(1).Select(c => c.GetAttribute("data-path")));
            Assert.Equal($"{first * EntryList.RowHeight}px", children[0].GetStyle("height"));
            Assert.Equal($"{(shown.Count - last) * EntryList.RowHeight}px", children[^1].GetStyle("height"));
            Assert.All(children.Skip(1).SkipLast(1), row => Assert.Equal(EntryList.RowHeight, row.GetBoundingClientRect().Height));
            // The tree is as high as all its rows.
            Assert.Equal(shown.Count * EntryList.RowHeight, children.Sum(c => c.GetBoundingClientRect().Height));
        }
        Assert.Equal(0, list.Made.First);
        Check();
        // A scroll within the rows made makes none again.
        var made = list.Folder("audio");
        pane.ScrollTop = 3;
        editor.Frame();
        Assert.Same(made, list.Folder("audio"));
        // Scrolling the pane makes the rows it brings into view.
        pane.ScrollTop = 2400;
        editor.Frame();
        Assert.True(list.Made.First > 80);
        Check();
        // End makes the last item active, scrolled into view at the pane's bottom; Home goes back to the top.
        list.Tree.Focus();
        editor.Key("End", "End");
        Assert.Equal("ui/styles", list.Active);
        Check();
        Assert.True(list.Folder("ui/styles")!.HasClass("active"));
        var row = list.Folder("ui/styles")!.GetBoundingClientRect();
        var paneBox = pane.GetBoundingClientRect();
        Assert.Equal(paneBox.Y + pane.ClientHeight, row.Y + row.Height, 0.5f);
        editor.Key("Home", "Home");
        Assert.Equal("audio", list.Active);
        // Scrolled as little as shows it: at the pane's top (the head and filter above it scrolled away).
        Assert.Equal(paneBox.Y, list.Folder("audio")!.GetBoundingClientRect().Y, 0.5f);
        Check();
        // An entry selected far down is shown and made.
        var far = editor.Session.Entry("rooms/many/room-200")!;
        editor.Session.Select(far);
        editor.Frame();
        Check();
        Assert.True(list.Row(far)!.HasClass("selected"));
        Assert.True(list.Row(far)!.HasClass("active"));
        var farBox = list.Row(far)!.GetBoundingClientRect();
        Assert.InRange(farBox.Y, paneBox.Y, paneBox.Y + pane.ClientHeight - farBox.Height);
        // Arrow keys from there move within the rows made, and an item past them is made when it comes into view.
        for (var i = 0; i < 40; i++)
            editor.Key("ArrowDown", "ArrowDown");
        Assert.Equal("rooms/many/room-240", list.Active);
        Check();
        Assert.True(list.Row(editor.Session.Entry("rooms/many/room-240")!)!.HasClass("active"));
        // The window resized: the rows of the new view.
        editor.Document.SetViewport(1000, 900);
        editor.Frame();
        Check();
    }
}

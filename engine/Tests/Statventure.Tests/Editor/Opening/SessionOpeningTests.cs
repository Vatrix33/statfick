using Statventure.Audio;
using Statventure.Editor.Session;
using Statventure.Packs;
using Statventure.Records;
using Statventure.Tests.Records;
using static Statventure.Tests.Editor.Opening.OpeningTesting;

namespace Statventure.Tests.Editor.Opening;

/// <summary>
/// The session's opening (Docs/Editor.md, "Opening packs (step 8)"; <c>EditorSession.Opening.cs</c>): one pack over
/// the game's packs (P6), every entry read and classified (a record, a file, or unreadable with its reason, M11-16),
/// read-only when any can't be read or when it is one of the game's packs outside the dev setup (M11-5), the base of
/// the game's other packs, a pack <c>Open</c> refuses, and the import queue drops fill.
/// </summary>
public class SessionOpeningTests
{
    [Fact]
    public void AModOpensOverTheGamesPacks()
    {
        using var game = new ScratchGame();
        var mod = game.Pack("player/mods/my-mod.pak", w =>
        {
            w.AddRecord("audio/rooms/hall", new RoomSpec(9, 0, 0));
            w.AddRecord("audio/rooms/crypt", new RoomSpec(2, 0.5, 0.5));
            w.Add("notes/todo.txt", "Tune the crypt."u8);
        });
        using var session = game.Session();
        var changes = new List<EntriesChangeKind>();
        session.EntriesChanged += change => changes.Add(change.Kind);
        var opened = 0;
        session.PackOpened += () => opened++;
        var pack = session.Open(mod);
        Assert.Equal(1, opened);
        Assert.Equal([EntriesChangeKind.Shown], changes);
        Assert.Same(pack, session.Pack);
        Assert.Equal((Path.GetFullPath(mod), "my-mod.pak", false, true, null), (pack.File, pack.Name, pack.IsGamePack, pack.Writable, pack.ReadOnlyReason));
        Assert.Empty(pack.Unreadable);
        Assert.True(session.Writable);
        Assert.Equal("my-mod.pak", session.PackName);
        using (var file = PackFile.Open(mod))
            Assert.Equal(file.IndexHashHex, pack.IndexHashHex);
        Assert.Equal(Path.GetFullPath(mod), session.PackFile!.FilePath);
        // The pack's entries over the game's: the pack's room at hall, the game's under it (shown again if it goes).
        Assert.Equal(["audio/rooms/cave", "audio/rooms/crypt", "audio/rooms/hall", "notes/todo.txt", "ui/readme.txt"], session.Entries.Select(e => e.Path));
        var bySource = session.Entries.ToDictionary(e => e.Path, e => (e.Source, e.Content, e.ReadOnly));
        Assert.Equal((EntrySource.Game, EntryContent.Record, false), bySource["audio/rooms/cave"]);
        Assert.Equal((EntrySource.Pack, EntryContent.Record, false), bySource["audio/rooms/crypt"]);
        Assert.Equal((EntrySource.Pack, EntryContent.Record, false), bySource["audio/rooms/hall"]);
        Assert.Equal((EntrySource.Pack, EntryContent.File, true), bySource["notes/todo.txt"]);
        Assert.Equal((EntrySource.Game, EntryContent.File, true), bySource["ui/readme.txt"]);
        Assert.Equal(new RoomSpec(9, 0, 0), session.Entry("audio/rooms/hall")!.Saved);
        Assert.Equal(Hall, session.Overridden("audio/rooms/hall")!.Saved);
        Assert.Equal(Cave, session.Entry("audio/rooms/cave")!.Saved);
        Assert.Equal("Tune the crypt.".Length, session.Entry("notes/todo.txt")!.FileSize);
        Assert.True(session.IsModCopy(session.Entry("audio/rooms/hall")!));
        Assert.False(session.HasUnsavedWork);
        // The base: the game's packs, read as the game reads them.
        Assert.Equal(Cave, session.Base!.Load<RoomSpec>("audio/rooms/cave"));
        Assert.Equal(Hall, session.Base.Load<RoomSpec>("audio/rooms/hall"));
        Assert.True(session.Base.Exists("ui/readme.txt"));
        Assert.Empty(session.GamePackProblems);
    }

    [Fact]
    public void ThePlayersFoldersAndTheGamesPacks()
    {
        using var game = new ScratchGame();
        using var session = new EditorSession(game.Game());
        // By default this machine's (the run's redirected one); the application gives its own.
        Assert.Equal(Statventure.Storage.GameFolder.ForThisMachine(), session.PlayerFolder);
        session.PlayerFolder = "relative/player";
        Assert.Equal(Path.GetFullPath("relative/player"), session.PlayerFolder);
        session.PlayerFolder = game.Player + "/";
        Assert.Equal(Path.GetFullPath(game.Player + "/"), session.PlayerFolder);
        Assert.Equal(Path.Combine(session.PlayerFolder, "mods"), session.ModsFolder);
        Assert.Equal(Path.Combine(session.PlayerFolder, "editor"), session.SettingsFolder);
        // The area packs there, in the game's order; other packs there aren't the game's.
        game.Pack("packs/models.pak", w => w.Add("models/readme.txt", "models"u8));
        game.Pack("packs/extra.pak", w => w.Add("extra/readme.txt", "extra"u8));
        Assert.Equal([game.Folder["packs/models.pak"], game.Folder["packs/audio.pak"], game.Folder["packs/ui.pak"]], session.GamePacks);
        Assert.True(session.IsGamePack(game.Folder["packs/audio.pak"]));
        Assert.True(session.IsGamePack(game.Folder["packs/../packs/ui.pak"]));
        Assert.False(session.IsGamePack(game.Folder["packs/extra.pak"]));
        Assert.Null(session.Pack);
        Assert.Null(session.Base);
        Assert.Null(session.PackFile);
    }

    [Theory]
    [InlineData("damaged")]
    [InlineData("not a record")]
    [InlineData("unknown kind")]
    [InlineData("newer version")]
    [InlineData("refused values")]
    public void APackWithAnEntryItCantReadOpensReadOnlyListingItWithItsReason(string how)
    {
        using var game = new ScratchGame();
        var mod = game.Pack("player/mods/broken.pak", w =>
        {
            w.AddRecord("audio/rooms/crypt", new RoomSpec(2, 0.5, 0.5));
            AddUnreadable(w, "bad/entry", how);
        });
        if (how == "damaged")
            Damage(mod, "bad/entry");
        using var session = game.Session();
        var pack = session.Open(mod);
        var entry = Assert.Single(pack.Unreadable);
        Assert.Same(session.Entry("bad/entry"), entry);
        Assert.Equal((EntryContent.Unreadable, EntrySource.Pack, true), (entry.Content, entry.Source, entry.ReadOnly));
        Assert.StartsWith(UnreadableWays.Single(w => w.How == how).Reason, entry.Reason);
        Assert.Equal("an entry can't be read", pack.ReadOnlyReason);
        Assert.False(pack.Writable);
        Assert.False(session.Writable);
        // What can be read is listed (read-only through the session: nothing is writable).
        Assert.Equal(EntryContent.Record, session.Entry("audio/rooms/crypt")!.Content);
        Assert.False(session.CanEdit(session.Entry("audio/rooms/crypt")!));
    }

    [Fact]
    public void EveryUnreadableEntryIsListedAndTheReasonCountsThem()
    {
        using var game = new ScratchGame();
        var mod = game.Pack("player/mods/broken.pak", w =>
        {
            foreach (var (how, _) in UnreadableWays)
                AddUnreadable(w, "bad/" + how.Replace(' ', '-'), how);
        });
        Damage(mod, "bad/damaged");
        using var session = game.Session();
        var pack = session.Open(mod);
        Assert.Equal(UnreadableWays.Select(w => "bad/" + w.How.Replace(' ', '-')).Order(StringComparer.Ordinal), pack.Unreadable.Select(e => e.Path));
        Assert.Equal("5 entries can't be read", pack.ReadOnlyReason);
        // ReadEntry gives the same, entry by entry, with the dump's words.
        using var file = PackFile.Open(mod);
        foreach (var (how, reason) in UnreadableWays)
        {
            var read = EditorSession.ReadEntry(file, file.Find("bad/" + how.Replace(' ', '-'))!, EntrySource.Game, RecordTypes.Engine);
            Assert.Equal((EntryContent.Unreadable, EntrySource.Game), (read.Content, read.Source));
            Assert.StartsWith(reason, read.Reason);
        }
    }

    [Fact]
    public void ReadingAnEntryGivesARecordOfAKnownKindOrAFile()
    {
        using var game = new ScratchGame();
        var mod = game.Pack("player/mods/parts.pak", w =>
        {
            w.AddRecord("parts/arm", new Part { Name = "arm", Weight = 2 });
            w.Add("parts/notes.txt", "notes"u8);
            w.Add("parts/long.txt", System.Text.Encoding.UTF8.GetBytes(new string('a', 10_000)));
        });
        using var file = PackFile.Open(mod);
        var types = new RecordTypes(RecordFixtures.Types);
        var arm = EditorSession.ReadEntry(file, file.Find("parts/arm")!, EntrySource.Pack, types);
        Assert.Equal((EntryContent.Record, new Part { Name = "arm", Weight = 2 }, false), (arm.Content, arm.Saved, arm.ReadOnly));
        Assert.True(EditorSession.ReadEntry(file, file.Find("parts/arm")!, EntrySource.Game, types, readOnly: true).ReadOnly);
        var notes = EditorSession.ReadEntry(file, file.Find("parts/notes.txt")!, EntrySource.Pack, types);
        Assert.Equal((EntryContent.File, 5L), (notes.Content, notes.FileSize));
        // A file's size is its content's, however it is stored (this one compressed).
        Assert.True(file.Find("parts/long.txt")!.Compressed);
        Assert.Equal(10_000L, EditorSession.ReadEntry(file, file.Find("parts/long.txt")!, EntrySource.Pack, types).FileSize);
        // The session's kinds decide: the test kinds known, the part opens as a record.
        using var session = game.Session();
        session.Types = types;
        session.Open(mod);
        Assert.True(session.Writable);
        Assert.Equal(EntryContent.Record, session.Entry("parts/arm")!.Content);
    }

    [Fact]
    public void APackOpenRefusesChangesNothing()
    {
        using var game = new ScratchGame();
        var mod = game.Pack("player/mods/fine.pak", w => w.AddRecord("audio/rooms/crypt", new RoomSpec(2, 0.5, 0.5)));
        using var session = game.Session();
        session.Open(mod);
        var before = session.Entries.ToList();
        var refused = Refused(game.Folder, "player/mods/refused.pak");
        Assert.Throws<DataFileException>(() => session.Open(refused));
        Assert.ThrowsAny<IOException>(() => session.Open(game.Folder["player/mods/missing.pak"]));
        Assert.Equal("fine.pak", session.Pack!.Name);
        Assert.Equal(before, session.Entries);
        Assert.Equal(Path.GetFullPath(mod), session.PackFile!.FilePath);
    }

    [Fact]
    public void TheGamesPacksOpenReadOnlyOutsideTheDevSetup()
    {
        using var game = new ScratchGame();
        using var player = game.Session(devSetup: false);
        var pack = player.Open(game.Folder["packs/audio.pak"]);
        Assert.True(pack.IsGamePack);
        Assert.Equal("the game's packs open read-only (only the repo's dev setup saves into them)", pack.ReadOnlyReason);
        Assert.False(player.Writable);
        Assert.Equal(EntrySource.Pack, player.Entry("audio/rooms/hall")!.Source);
        // In the dev setup it takes changes; the game's other packs are shown beside it read-only, and aren't in the base twice.
        using var dev = game.Session(devSetup: true);
        var devPack = dev.Open(game.Folder["packs/audio.pak"]);
        Assert.Equal((true, true, null), (devPack.IsGamePack, devPack.Writable, devPack.ReadOnlyReason));
        Assert.True(dev.Writable);
        Assert.False(dev.Entry("audio/rooms/hall")!.ReadOnly);
        game.Pack("packs/data.pak", w => w.AddRecord("data/rooms/vault", new RoomSpec(4, 0, 0)));
        dev.Open(game.Folder["packs/audio.pak"]);
        var vault = dev.Entry("data/rooms/vault")!;
        Assert.Equal((EntrySource.Game, true), (vault.Source, vault.ReadOnly));
        Assert.False(dev.CanEdit(vault));
        Assert.False(dev.Base!.Exists("audio/rooms/hall"));
        Assert.True(dev.Base.Exists("data/rooms/vault"));
        // A mod in the dev setup: the game's records can be copied (P7), so they aren't read-only.
        var mod = game.Pack("player/mods/my-mod.pak", w => w.Add("notes/a.txt", "a"u8));
        dev.Open(mod);
        Assert.False(dev.Entry("data/rooms/vault")!.ReadOnly);
        Assert.True(dev.CanEdit(dev.Entry("data/rooms/vault")!));
    }

    [Fact]
    public void AGamePackThatCantBeReadIsLeftOutAndNamed()
    {
        using var game = new ScratchGame();
        File.WriteAllText(game.Folder["packs/ui.pak"], "damaged");
        var mod = game.Pack("player/mods/my-mod.pak", w => w.Add("notes/a.txt", "a"u8));
        using var session = game.Session();
        session.Open(mod);
        Assert.True(session.Writable);
        Assert.Equal(["audio/rooms/cave", "audio/rooms/hall", "notes/a.txt"], session.Entries.Select(e => e.Path));
        var said = Assert.Single(session.GamePackProblems);
        Assert.StartsWith("ui.pak: ", said);
        // The pack's problem without its folder (the line names the pack itself).
        Assert.DoesNotContain(game.Packs, said);
        Assert.False(session.Base!.Exists("ui/readme.txt"));
        Assert.True(session.Base.Exists("audio/rooms/hall"));
        // Read again on the next opening: when it is mended, it is back.
        game.Pack("packs/ui.pak", w => w.Add("ui/readme.txt", "The game's UI."u8));
        session.Open(mod);
        Assert.Empty(session.GamePackProblems);
        Assert.NotNull(session.Entry("ui/readme.txt"));
    }

    [Fact]
    public void OpeningAnotherPackReplacesTheOpenOne()
    {
        using var game = new ScratchGame();
        var first = game.Pack("player/mods/first.pak", w => w.AddRecord("audio/rooms/crypt", new RoomSpec(2, 0.5, 0.5)));
        var second = game.Pack("player/mods/second.pak", w => AddUnreadable(w, "bad/entry", "not a record"));
        using var session = game.Session();
        session.Open(first);
        var crypt = session.Entry("audio/rooms/crypt")!;
        session.Select(crypt);
        session.Commit(crypt, crypt.Draft!.Set("Seconds", 5.0));
        Assert.True(session.HasUnsavedWork);
        var firstFile = session.PackFile!;
        var firstBase = session.Base!;
        session.Open(second);
        Assert.Equal("second.pak", session.PackName);
        Assert.Null(session.Selected);
        Assert.Null(session.Entry("audio/rooms/crypt"));
        Assert.False(session.HasUnsavedWork);
        Assert.False(session.Writable);
        Assert.NotSame(firstFile, session.PackFile);
        Assert.NotSame(firstBase, session.Base);
        // The first pack's file and base were closed.
        Assert.Throws<ObjectDisposedException>(() => firstFile.Read("audio/rooms/crypt"));
        Assert.Throws<ObjectDisposedException>(() => firstBase.Load<RoomSpec>("audio/rooms/hall"));
        // A writable pack again after it.
        session.Open(first);
        Assert.True(session.Writable);
        Assert.Null(session.Pack!.ReadOnlyReason);
    }

    [Fact]
    public void TheSessionClosesItsPackWhenDisposed()
    {
        using var game = new ScratchGame();
        var mod = game.Pack("player/mods/my-mod.pak", w => w.AddRecord("audio/rooms/crypt", new RoomSpec(2, 0.5, 0.5)));
        var session = game.Session();
        session.Open(mod);
        session.Open(mod);
        var file = session.PackFile!;
        var data = session.Base!;
        session.Dispose();
        Assert.Throws<ObjectDisposedException>(() => file.Read("audio/rooms/crypt"));
        Assert.Throws<ObjectDisposedException>(() => data.Load<RoomSpec>("audio/rooms/hall"));
    }

    [Fact]
    public void DroppedFilesWaitForImportInOrder()
    {
        using var game = new ScratchGame();
        using var session = game.Session();
        var queued = 0;
        Assert.False(session.CanImport);
        session.QueueImports([]);
        Assert.Empty(session.PendingImports);
        void Listener() => queued++;
        session.ImportsQueued += Listener;
        Assert.True(session.CanImport);
        session.QueueImports(["/a/arena.glb", "/a/style.css"]);
        session.QueueImports([]);
        session.QueueImports(["/b/font.ttf"]);
        Assert.Equal(2, queued);
        Assert.Equal(["/a/arena.glb", "/a/style.css", "/b/font.ttf"], session.PendingImports);
        Assert.Equal(["/a/arena.glb", "/a/style.css", "/b/font.ttf"], session.TakeImports());
        Assert.Empty(session.PendingImports);
        Assert.Empty(session.TakeImports());
        session.ImportsQueued -= Listener;
        Assert.False(session.CanImport);
    }
}

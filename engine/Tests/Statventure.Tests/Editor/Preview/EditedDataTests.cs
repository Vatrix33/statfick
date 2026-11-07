using Statventure.Audio;
using Statventure.Editor.Session;
using Statventure.Packs;
using Statventure.Records;
using Statventure.Render3D;
using Statventure.Tests.Editor.Opening;
using static Statventure.Tests.Editor.Opening.OpeningTesting;

namespace Statventure.Tests.Editor.Preview;

/// <summary>
/// The data as edited (Docs/Editor.md, "How it works", step 13, "The data as edited"; P38): the game's packs the open
/// pack is seen over with the session's entries as edited on top, as the game would see them after saving, kept up to
/// date as entries change, an entry that doesn't build given as its last built value (M11-12).
/// </summary>
public class EditedDataTests
{
    static readonly RoomSpec Crypt = new(2, 0.5, 0.5), OwnHall = new(9, 0.1, 0.9);

    /// <summary>The scratch game and a mod over it: a room of its own, the hall overridden, a text file.</summary>
    static (ScratchGame Game, EditorSession Session, string Mod) Opened()
    {
        var game = new ScratchGame();
        var mod = game.Pack("player/mods/mine.pak", w =>
        {
            w.AddRecord("audio/rooms/crypt", Crypt);
            w.AddRecord("audio/rooms/hall", OwnHall);
            w.Add("audio/notes.txt", "Notes."u8);
        });
        var session = game.Session();
        session.Open(mod);
        return (game, session, mod);
    }

    static RoomSpec Room(EditorSession session, string name) => session.EditedData!.Load<RoomSpec>("audio/rooms/" + name);

    static void Commit(EditorSession session, string path, Func<RecordDraft, RecordDraft> change)
    {
        var entry = session.Entry(path)!;
        Assert.True(session.Commit(entry, change(entry.Draft!)));
    }

    [Fact]
    public void ThereIsNoneWithoutAnOpenPack()
    {
        using var game = new ScratchGame();
        using var session = game.Session();
        Assert.Null(session.EditedData);
        Assert.Null(session.EditedDataProblem);
    }

    [Fact]
    public void ItIsTheGamesPacksWithTheOpenPacksEntriesOnTop()
    {
        var (game, session, mod) = Opened();
        using var _g = game;
        using var _s = session;
        var data = session.EditedData!;
        Assert.Equal(Cave, Room(session, "cave"));
        Assert.Equal(OwnHall, Room(session, "hall"));
        Assert.Equal(Crypt, Room(session, "crypt"));
        Assert.Equal("Notes."u8.ToArray(), data.ReadAllBytes("audio/notes.txt"));
        Assert.False(data.IsRecord("audio/notes.txt"));
        Assert.Equal("The game's UI."u8.ToArray(), data.ReadAllBytes("ui/readme.txt"));
        // Below, the game's packs in their order; on top the open pack's entries, named by its file.
        Assert.Equal([Path.Combine(game.Packs, "audio.pak"), Path.Combine(game.Packs, "ui.pak"), mod], data.Mounts.Select(m => m.Path));
        Assert.Equal(MountKind.Custom, data.Mounts[^1].Kind);
        Assert.Equal(mod, data.Origin("audio/rooms/crypt"));
        Assert.Equal(mod, data.Origin("audio/rooms/hall"));
        Assert.Equal(Path.Combine(game.Packs, "audio.pak"), data.Origin("audio/rooms/cave"));
        // Unchanged entries as the pack stores them; each read a fresh array.
        using (var pack = PackFile.Open(mod))
            Assert.Equal(pack.Read("audio/rooms/crypt"), data.Mounts[^1].Entries!.Read("audio/rooms/crypt"));
        data.ReadAllBytes("audio/notes.txt")[0] = 0;
        Assert.Equal("Notes."u8.ToArray(), data.ReadAllBytes("audio/notes.txt"));
        // A record as edited too (its binary form kept for the value built).
        Commit(session, "audio/rooms/crypt", d => d.Set("Seconds", 4.0));
        var custom = data.Mounts[^1].Entries!;
        var binary = custom.Read("audio/rooms/crypt");
        Array.Clear(binary);
        Assert.Equal(RecordSerializer.ToBinary(Crypt with { Seconds = 4 }), custom.Read("audio/rooms/crypt"));
        Assert.Equal(Crypt with { Seconds = 4 }, Room(session, "crypt"));
        Assert.Same(data, session.EditedData);
    }

    [Fact]
    public void AnEditIsReadAtOnceAndAnEntryThatDoesntBuildGivesItsLastBuiltValue()
    {
        var (game, session, _) = Opened();
        using var _g = game;
        using var _s = session;
        var data = session.EditedData!;
        var reloads = 0;
        data.Reloaded += () => reloads++;
        Commit(session, "audio/rooms/crypt", d => d.Set("Seconds", 4.0));
        Assert.Equal(Crypt with { Seconds = 4 }, Room(session, "crypt"));
        Assert.Equal(RecordSerializer.ToBinary(Crypt with { Seconds = 4 }), session.EditedData!.Mounts[^1].Entries!.Read("audio/rooms/crypt"));
        Commit(session, "audio/rooms/crypt", d => d.With("Wet", DraftValue.Missing));
        Assert.False(session.Entry("audio/rooms/crypt")!.Builds);
        Assert.Equal(Crypt with { Seconds = 4 }, Room(session, "crypt"));
        session.Undo(session.Entry("audio/rooms/crypt")!);
        session.Undo(session.Entry("audio/rooms/crypt")!);
        Assert.Equal(Crypt, Room(session, "crypt"));
        // Its paths didn't change: no reload, the same file system.
        Assert.Same(data, session.EditedData);
        Assert.Equal(0, reloads);
    }

    [Fact]
    public void AGameEntryChangedIsTheModsCopyUntilItIsTakenBack()
    {
        var (game, session, mod) = Opened();
        using var _g = game;
        using var _s = session;
        var data = session.EditedData!;
        var reloads = 0;
        data.Reloaded += () => reloads++;
        Commit(session, "audio/rooms/cave", d => d.Set("Dark", 0.5));
        Assert.Equal(Cave with { Dark = 0.5 }, Room(session, "cave"));
        Assert.Equal(mod, data.Origin("audio/rooms/cave"));
        Assert.Equal(1, reloads);
        // Changed again: read at once, no reload.
        Commit(session, "audio/rooms/cave", d => d.Set("Dark", 0.25));
        Assert.Equal(Cave with { Dark = 0.25 }, Room(session, "cave"));
        Assert.Equal(1, reloads);
        // Taken back by undo: the game's again, from its pack.
        var cave = session.Entry("audio/rooms/cave")!;
        session.Undo(cave);
        session.Undo(cave);
        Assert.Equal(Cave, Room(session, "cave"));
        Assert.Equal(Path.Combine(game.Packs, "audio.pak"), data.Origin("audio/rooms/cave"));
        Assert.Equal(2, reloads);
        Assert.Same(data, session.EditedData);
    }

    [Fact]
    public void RenamedRemovedAndNewEntriesAreWhereTheSessionHasThem()
    {
        var (game, session, mod) = Opened();
        using var _g = game;
        using var _s = session;
        var data = session.EditedData!;
        session.Rename(session.Entry("audio/rooms/crypt")!, "audio/rooms/tomb");
        Assert.False(session.EditedData!.Exists("audio/rooms/crypt"));
        Assert.Equal(Crypt, Room(session, "tomb"));
        Assert.Equal(mod, data.Origin("audio/rooms/tomb"));
        // The mod's hall removed: the game's shows through, as in the game after saving.
        session.Remove(session.Entry("audio/rooms/hall")!);
        Assert.Equal(Hall, Room(session, "hall"));
        session.Remove(session.Entry("audio/notes.txt")!);
        Assert.False(session.EditedData!.Exists("audio/notes.txt"));
        // A new entry is there once it has built; one that never built isn't.
        var added = session.Add("audio/rooms/vault", RecordDraft.Blank(typeof(RoomSpec)));
        Assert.False(added.Builds);
        Assert.False(session.EditedData!.Exists("audio/rooms/vault"));
        Assert.True(session.Commit(added, added.Draft!.Set("Seconds", 1.0).Set("Dark", 0.0).Set("Wet", 0.0)));
        Assert.Equal(new RoomSpec(1, 0, 0), Room(session, "vault"));
        Assert.Equal(mod, data.Origin("audio/rooms/vault"));
        Assert.Same(data, session.EditedData);
    }

    [Fact]
    public void AFileImportedInTheSessionIsGivenWithItsBytes()
    {
        var (game, session, mod) = Opened();
        using var _g = game;
        using var _s = session;
        var file = game.Folder.Write("drops/readme.txt", "Imported."u8.ToArray());
        var source = session.ImportSourceOf(file);
        var (path, _) = session.SuggestImportPath(source);
        var data = session.EditedData!;
        Assert.False(data.Exists(path));
        session.Import(session.PlanImport(source, path));
        Assert.Same(data, session.EditedData);
        Assert.Equal("Imported."u8.ToArray(), data.ReadAllBytes(path));
        Assert.False(data.IsRecord(path));
        Assert.Equal(mod, data.Origin(path));
        data.ReadAllBytes(path)[0] = 0;
        Assert.Equal("Imported."u8.ToArray(), data.ReadAllBytes(path));
    }

    [Fact]
    public void ANewEntryThatNeverBuiltIsntThere()
    {
        var (game, session, _) = Opened();
        using var _g = game;
        using var _s = session;
        var blank = RecordDraft.Blank(typeof(RoomSpec)).With("Seconds", DraftValue.Missing);
        var added = session.Add("audio/rooms/vault", blank);
        Assert.False(added.Builds);
        Assert.False(session.EditedData!.Exists("audio/rooms/vault"));
        Assert.True(session.Commit(added, blank.Set("Seconds", 3.0).Set("Dark", 0.0).Set("Wet", 0.0)));
        Assert.Equal(new RoomSpec(3, 0, 0), Room(session, "vault"));
        // Broken again later: its last built value stays.
        Assert.True(session.Commit(added, added.Draft!.With("Seconds", DraftValue.Missing)));
        Assert.Equal(new RoomSpec(3, 0, 0), Room(session, "vault"));
    }

    [Fact]
    public void AnotherPackOpenedGivesANewFileSystemAndTheOldOneIsClosed()
    {
        var (game, session, _) = Opened();
        using var _g = game;
        using var _s = session;
        var first = session.EditedData!;
        var other = game.Pack("player/mods/other.pak", w => w.AddRecord("audio/rooms/well", new RoomSpec(5, 0, 0)));
        session.Open(other);
        var second = session.EditedData!;
        Assert.NotSame(first, second);
        Assert.Throws<ObjectDisposedException>(() => first.Exists("audio/rooms/crypt"));
        Assert.False(second.Exists("audio/rooms/crypt"));
        Assert.Equal(new RoomSpec(5, 0, 0), Room(session, "well"));
        Assert.Equal(Hall, Room(session, "hall"));
        session.Dispose();
        Assert.Throws<ObjectDisposedException>(() => second.Exists("audio/rooms/well"));
    }

    [Fact]
    public void InTheDevSetupAGamePackOpenIsGivenByTheSessionNotItsFile()
    {
        using var game = new ScratchGame();
        using var session = game.Session(devSetup: true);
        session.Open(Path.Combine(game.Packs, "audio.pak"));
        var data = session.EditedData!;
        Assert.Equal([Path.Combine(game.Packs, "ui.pak"), Path.Combine(game.Packs, "audio.pak")], data.Mounts.Select(m => m.Path));
        Assert.Equal(MountKind.Custom, data.Mounts[^1].Kind);
        Commit(session, "audio/rooms/hall", d => d.Set("Wet", 0.75));
        Assert.Equal(Hall with { Wet = 0.75 }, Room(session, "hall"));
        Assert.Equal(Cave, Room(session, "cave"));
        Assert.Equal("The game's UI."u8.ToArray(), data.ReadAllBytes("ui/readme.txt"));
    }

    [Fact]
    public void EntriesNoPackCanHoldTogetherGiveTheProblemUntilTheyAreGone()
    {
        using var game = new ScratchGame();
        var mod = game.Pack("player/mods/mine.pak", w => w.Add("audio/rooms/crypt.json", "{}"u8));
        using var session = game.Session();
        session.Open(mod);
        Assert.NotNull(session.EditedData);
        var added = session.Add("audio/rooms/crypt", RecordDraft.From(Crypt));
        Assert.Null(session.EditedData);
        Assert.Equal($"{mod}: entry 'audio/rooms/crypt': a record beside the file 'audio/rooms/crypt.json', which is its loose form", session.EditedDataProblem);
        session.Remove(added);
        Assert.Equal(Hall, Room(session, "hall"));
        Assert.Null(session.EditedDataProblem);
        // A rename the mount refuses: the file system mounted keeps its entries as they were mounted, readable.
        var crypt = session.Add("audio/rooms/crypt2", RecordDraft.From(Crypt));
        var held = session.EditedData!;
        Assert.Equal(Crypt, held.Load<RoomSpec>("audio/rooms/crypt2"));
        session.Rename(crypt, "audio/rooms/crypt");
        Assert.Null(session.EditedData);
        Assert.Equal(Crypt, held.Load<RoomSpec>("audio/rooms/crypt2"));
        session.Rename(crypt, "audio/rooms/crypt2");
        Assert.Same(held, session.EditedData);
        Assert.Null(session.EditedDataProblem);
        // Not mountable from the start (a fresh session's first read).
        using var fresh = game.Session();
        fresh.Open(mod);
        fresh.Add("audio/rooms/crypt", RecordDraft.From(Crypt));
        Assert.Null(fresh.EditedData);
        Assert.NotNull(fresh.EditedDataProblem);
        fresh.Remove(fresh.Entry("audio/rooms/crypt")!);
        Assert.Equal(Hall, fresh.EditedData!.Load<RoomSpec>("audio/rooms/hall"));
        Assert.Null(fresh.EditedDataProblem);
    }

    [Fact]
    public void ARecordStoredInAnOlderVersionIsGivenAsStored()
    {
        using var game = new ScratchGame();
        var data = ModelData.Load(Path.Combine(Statventure.Tests.Render3D.Render3DReferenceTests.ModelDirectory, "hierarchy.glb"));
        var old = RecordSerializer.WriteBinary(typeof(ModelData), data, new RecordWriteOptions
        {
            Versions = new Dictionary<string, int> { ["model"] = 1 },
            Downgrade = (value, version) => value is ModelData d
                ? new ModelData.V1 { Nodes = d.Nodes, Meshes = d.Meshes, Materials = d.Materials, Skins = d.Skins, Animations = d.Animations, Roots = d.Roots }
                : value,
        });
        Assert.NotEqual(RecordSerializer.ToBinary(data), old);
        var mod = game.Pack("player/mods/mine.pak", w => w.AddRecordBinary("models/old", old));
        using var session = game.Session();
        session.Open(mod);
        var edited = session.EditedData!;
        Assert.Equal(old, edited.Mounts[^1].Entries!.Read("models/old"));
        Assert.Equal(data.Nodes.Count, edited.Load<ModelData>("models/old").Nodes.Count);
    }

    [Fact]
    public void ADamagedEntryReadsAsThePacksError()
    {
        using var game = new ScratchGame();
        var mod = game.Pack("player/mods/mine.pak", w => AddUnreadable(w, "audio/rooms/broken", "damaged"));
        Damage(mod, "audio/rooms/broken");
        using var session = game.Session();
        session.Open(mod);
        var data = session.EditedData!;
        Assert.True(data.Exists("audio/rooms/broken"));
        var problem = Assert.Throws<DataFileException>(() => data.ReadAllBytes("audio/rooms/broken"));
        Assert.Contains(mod, problem.Message);
        Assert.Contains("audio/rooms/broken", problem.Message);
    }
}

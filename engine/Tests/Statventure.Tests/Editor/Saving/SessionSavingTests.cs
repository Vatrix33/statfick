using Statventure.Audio;
using Statventure.Editor.Session;
using Statventure.Packs;
using Statventure.Records;
using Statventure.Tests.Infrastructure;
using Statventure.Tests.Packs;
using Statventure.Tests.Records;
using static Statventure.Tests.Editor.Checks.CheckTesting;
using static Statventure.Tests.Editor.Saving.SavingTesting;
using static Statventure.Tests.Packs.PackTestData;

namespace Statventure.Tests.Editor.Saving;

/// <summary>
/// Saving the open pack (M11-17, P23; Docs/Editor.md, "How it works", step 11): the whole pack written, every unchanged
/// entry as it was stored (an older record version, bytes stored as the writer wouldn't store them now), edited, new and
/// copied records in the current versions, imported files with their bytes, renamed ones at their new paths, removed ones
/// left out, the new file swapped in; refused while an entry has a problem (M11-12); the file's index hash compared
/// (P23); after a save the pack read again and the entries saved, their histories kept (the manager's decision Q6).
/// </summary>
public class SessionSavingTests
{
    const string Ogre = "creatures/_ogre", Roster = "roster/ogre", Crypt = "audio/rooms/crypt", Raw = "notes/raw.txt",
        Moved = "notes/moved.txt", Gone = "notes/removed.txt", Attic = "audio/rooms/attic", Readme = "notes/readme.txt";

    static readonly RoomSpec CryptRoom = new(2.5, 0.5, 0.25), AtticRoom = new(0.75, 0.25, 0.5);

    /// <summary>
    /// A mod made by hand, each entry stored as the editor's writer wouldn't store it now (a test creature in its version 1,
    /// text compressed at a lower quality, text stored raw that Brotli would shrink), so only keeping the stored bytes gives
    /// them back; its test kinds are the session's.
    /// </summary>
    static RawPack HandMade()
    {
        var raw = new RawPack();
        raw.Raw(Crypt, RecordSerializer.ToBinary(CryptRoom));
        raw.Entries[^1].Flags = 2;
        raw.Raw(Ogre, RecordFixtures.Read("test.creature", "creature-1", ".rec"));
        raw.Entries[^1].Flags = 2;
        raw.Raw(Raw, Prose(2000, 2));
        raw.Raw(Gone, Noise(50, 4));
        raw.Brotli(Roster, Prose(3000, 1), quality: 5);
        return raw;
    }

    /// <summary>The hand-made mod open, then every kind of change: an edit, a rename, a removal, a new entry, an imported file and the mod's copy of a game room.</summary>
    static EditorSession Changed(Scratch scratch, out string mod)
    {
        mod = scratch.Mod(HandMade());
        var session = scratch.Session(mod, TestTypes);
        Assert.True(session.CanSave);
        session.Change(session.At(Crypt), "Seconds", 3.5);
        session.Rename(session.At(Raw), Moved);
        session.Remove(session.At(Gone));
        session.Add(Attic, RecordDraft.From(AtticRoom));
        var readme = scratch.Folder.Write("downloads/readme.txt", "Read me first.");
        session.Import(session.PlanImport(session.ImportSourceOf(readme), Readme));
        session.Change(session.At(GameRoom), "Seconds", 9.0);
        Assert.Equal(EntrySource.Game, session.At(GameRoom).Source);
        return session;
    }

    [Fact]
    public void ASaveKeepsEveryUnchangedEntryAsStoredAndWritesTheRest()
    {
        using var scratch = new Scratch();
        using var session = Changed(scratch, out var mod);
        var before = Stored(mod);
        Assert.Empty(session.SaveProblems());
        var oldPack = session.PackFile!;
        session.Save();
        var after = Stored(mod);
        var contents = Contents(mod);
        Assert.Equal(new[] { Crypt, GameRoom, Attic, Ogre, Moved, Readme, Roster }.Order(StringComparer.Ordinal), after.Keys.Order(StringComparer.Ordinal));
        // Unchanged: as stored (the old version, the lower quality, raw text), renamed ones too.
        Assert.Equal(before[Ogre], after[Ogre]);
        Assert.Equal(before[Roster], after[Roster]);
        Assert.Equal(before[Raw], after[Moved]);
        // Edited, new and copied records in the current versions; the imported file with its bytes.
        Assert.Equal((true, Binary(CryptRoom with { Seconds = 3.5 })), contents[Crypt]);
        Assert.Equal((true, Binary(AtticRoom)), contents[Attic]);
        Assert.Equal((true, Binary(GameRoomSpec() with { Seconds = 9.0 })), contents[GameRoom]);
        Assert.Equal((false, Convert.ToHexStringLower("Read me first."u8)), contents[Readme]);
        // The new file swapped in: nothing left beside it.
        Assert.Equal(["my-mod.pak"], Directory.GetFiles(scratch.Mods).Select(Path.GetFileName));
        // The pack is read again: the new file, its index hash the one compared next.
        Assert.NotSame(oldPack, session.PackFile);
        using (var now = PackFile.Open(mod))
            Assert.Equal(now.IndexHashHex, session.Pack!.IndexHashHex);
        Assert.Null(session.ChangedOnDisk());
        Assert.Equal(contents[Readme].Content, Convert.ToHexStringLower(session.PackFile!.Read(Readme)));
    }

    [Fact]
    public void AfterASaveTheEntriesLoseTheirMarksAndKeepTheirHistories()
    {
        using var scratch = new Scratch();
        using var session = Changed(scratch, out var mod);
        var crypt = session.At(Crypt);
        var hall = session.At(GameRoom);
        var readme = session.At(Readme);
        Assert.True(session.HasUnsavedWork);
        var reported = new List<string>();
        session.EntryChanged += entry => reported.Add(entry.Path);
        var notified = 0;
        session.Changed += () => notified++;
        session.Save();
        Assert.False(session.HasUnsavedWork);
        Assert.Empty(session.Removed);
        Assert.All(session.SaveEntries, e =>
        {
            Assert.Equal(EntrySource.Pack, e.Source);
            Assert.False(e.IsChanged, e.Path);
            Assert.False(e.IsRenamed, e.Path);
            Assert.Equal(e.Path, e.SavedPath);
        });
        // Each entry whose marks changed is reported, the others not; the title follows.
        Assert.Equal([Attic, Crypt, GameRoom, Moved, Readme], reported.Order(StringComparer.Ordinal));
        Assert.True(notified > 0);
        // The imported file is the pack's now, read from it.
        Assert.Null(readme.ImportedFile);
        Assert.Equal("Read me first."u8.ToArray(), session.EditedData!.ReadAllBytes(Readme));
        // The mod holds its own copy of the game's room: the game's under it, as read from the game.
        Assert.Same(hall, session.At(GameRoom));
        Assert.True(session.IsModCopy(hall));
        Assert.Equal(EntrySource.Game, session.Overridden(GameRoom)!.Source);
        Assert.Equal(GameRoomSpec(), session.Overridden(GameRoom)!.Saved);
        Assert.Equal(GameRoomSpec() with { Seconds = 9.0 }, hall.Saved);
        // Histories kept (Q6): undo goes back past the save, and the entry is changed again.
        Assert.True(crypt.CanUndo);
        session.Undo(crypt);
        Assert.True(crypt.IsChanged);
        Assert.True(session.HasUnsavedWork);
        session.Redo(crypt);
        Assert.False(crypt.IsChanged);
        Assert.False(session.HasUnsavedWork);
    }

    /// <summary>"Revert to the game's" on the mod's copy saved before (P7, step 9's revert wired to saving): the next save leaves the copy out, the game's entry shows through.</summary>
    [Fact]
    public void ARevertedCopyIsLeftOutOfTheNextSave()
    {
        using var scratch = new Scratch();
        using var session = Changed(scratch, out var mod);
        session.Save();
        var game = session.Revert(session.At(GameRoom));
        Assert.Equal(EntrySource.Game, game.Source);
        Assert.True(session.HasUnsavedWork);
        session.Save();
        Assert.False(Contents(mod).ContainsKey(GameRoom));
        Assert.Same(game, session.At(GameRoom));
        Assert.False(session.HasUnsavedWork);
        // A game entry changed and taken back before any save never reaches the pack.
        session.Change(session.At("audio/rooms/cavern"), "Seconds", 4.0);
        session.Revert(session.At("audio/rooms/cavern"));
        Assert.False(session.HasUnsavedWork);
        Assert.DoesNotContain("audio/rooms/cavern", session.SaveEntries.Select(e => e.Path));
    }

    /// <summary>M11-12: an entry with a problem refuses the save, naming it with its first problem (the checks across entries run first); nothing written.</summary>
    [Fact]
    public void ASaveIsRefusedWhileAnEntryHasAProblem()
    {
        using var scratch = new Scratch();
        var mod = scratch.Mod(w =>
        {
            w.AddRecord(EntryChecksBoom, Patch());
            w.AddRecord("world/cutscenes/intro", Cutscene(Sound("thud")));
        });
        using var session = scratch.Session(mod);
        var bytes = File.ReadAllBytes(mod);
        session.Change(session.At(EntryChecksBoom), "MaxVoices", 0);
        // The cutscene's problem is found across entries: SaveProblems runs them first.
        Assert.True(session.Checks.Pending);
        Assert.Equal([
            new SaveProblem(EntryChecksBoom, "allows no voices (at $.maxVoices)"),
            new SaveProblem("world/cutscenes/intro", "the sequence intro, track 0 (audio): cue 0 plays the effect thud, which isn't in the game's data (audio/effects/thud) (at $.tracks[0].cues[0].name)"),
        ], session.SaveProblems());
        Assert.False(session.Checks.Pending);
        var refused = Assert.Throws<InvalidOperationException>(() => session.Save());
        Assert.Contains("audio/effects/boom: allows no voices", refused.Message);
        Assert.Equal(bytes, File.ReadAllBytes(mod));
        Assert.True(session.HasUnsavedWork);
        session.Undo(session.At(EntryChecksBoom));
        session.Add("audio/effects/thud", RecordDraft.From(Patch()));
        Assert.Empty(session.SaveProblems());
        session.Save();
        Assert.Contains("audio/effects/thud", Contents(mod).Keys);
    }

    const string EntryChecksBoom = "audio/effects/boom";

    /// <summary>An entry with several problems is named with its first (the kind's own, as the form shows it first).</summary>
    [Fact]
    public void EachEntryIsNamedWithItsFirstProblem()
    {
        using var scratch = new Scratch();
        var mod = scratch.Mod(w => w.AddRecord("audio/themes/alder-vale", Game<ThemeSpec>("audio/themes/alder-vale") with { Mode = "blues", Id = "vale" }));
        using var session = scratch.Session(mod);
        var vale = session.At("audio/themes/alder-vale");
        Assert.Equal(2, vale.Problems.Count);
        Assert.Equal([new SaveProblem("audio/themes/alder-vale", "Unknown mode blues (at $.mode)")], session.SaveProblems());
        Assert.Equal("audio/themes/alder-vale: Unknown mode blues (at $.mode)", session.SaveProblems()[0].ToString());
        Assert.Equal("a problem of the data", new SaveProblem(null, "a problem of the data").ToString());
    }

    /// <summary>The data's problem no entry shows (a file the music's loading refuses) refuses it too, as the last line.</summary>
    [Fact]
    public void TheDatasProblemRefusesASave()
    {
        using var scratch = new Scratch();
        var mod = scratch.Mod(w => w.Add("audio/rooms/echo", "Not a room."u8));
        using var session = scratch.Session(mod);
        session.Add(Attic, RecordDraft.From(AtticRoom));
        var problems = session.SaveProblems();
        var problem = Assert.Single(problems);
        Assert.Null(problem.Entry);
        Assert.Equal(session.Checks.DataProblem, problem.Problem);
        Assert.Contains("audio/rooms/echo", problem.ToString());
        Assert.Throws<InvalidOperationException>(() => session.Save());
    }

    /// <summary>P23: the file's index hash compared with the one read when it was opened (or last saved).</summary>
    [Fact]
    public void AFileChangedOnDiskIsSaid()
    {
        using var scratch = new Scratch();
        var mod = scratch.Mod(w => w.AddRecord(Crypt, CryptRoom));
        using var session = scratch.Session(mod);
        Assert.Null(session.ChangedOnDisk());
        // Another program writes the same file again: the same index, nothing to say.
        var again = new PackWriter();
        again.AddRecord(Crypt, CryptRoom);
        again.Write(mod);
        Assert.Null(session.ChangedOnDisk());
        var other = new PackWriter();
        other.AddRecord(Crypt, CryptRoom with { Wet = 1 });
        other.Write(mod);
        Assert.Equal("was changed by another program since it was opened here", session.ChangedOnDisk());
        File.WriteAllText(mod, "not a pack any more");
        Assert.Equal("can't be read now: " + Assert.Throws<DataFileException>(() => PackFile.Open(mod)).Problem, session.ChangedOnDisk());
        File.Delete(mod);
        Assert.Equal("isn't there any more", session.ChangedOnDisk());
        Directory.Delete(scratch.Mods);
        Assert.Equal("isn't there any more", session.ChangedOnDisk());
        // Overwriting: the save writes the editor's version (its unchanged entries as it read them).
        session.Change(session.At(Crypt), "Dark", 0.75);
        session.Save();
        Assert.Equal((true, Binary(CryptRoom with { Dark = 0.75 })), Contents(mod)[Crypt]);
        Assert.Null(session.ChangedOnDisk());
    }

    /// <summary>Only a pack that takes changes can be saved over (M11-16: one that can't be fully read; M11-5: the game's packs outside the dev setup).</summary>
    [Fact]
    public void OnlyAPackThatTakesChangesCanBeSavedOver()
    {
        using var scratch = new Scratch();
        var session = new EditorSession(GameData.Repo(EditorHarness.EngineFolder)) { PlayerFolder = scratch.Player };
        Assert.False(session.CanSave);
        Assert.False(session.CanSaveAs);
        Assert.Null(session.ChangedOnDisk());
        Assert.Throws<InvalidOperationException>(() => session.Save());
        session.Dispose();
        var unreadable = scratch.Mod(w =>
        {
            w.AddRecord(Crypt, CryptRoom);
            Opening.OpeningTesting.AddUnreadable(w, "parts/arm", "unknown kind");
        });
        using var readOnly = scratch.Session(unreadable);
        Assert.False(readOnly.CanSave);
        Assert.True(readOnly.CanSaveAs);
        Assert.Throws<InvalidOperationException>(() => readOnly.Save());
        // An installed game (the repo's packs, not the dev setup): its packs open read-only.
        var game = new EditorSession(new GameData(TestPaths.Packs, DevSetup: false)) { PlayerFolder = scratch.Player };
        game.Open(Path.Combine(TestPaths.Packs, "audio.pak"));
        Assert.False(game.Writable);
        Assert.False(game.CanSave);
        game.Dispose();
    }

    /// <summary>The dev setup (P5): one of the game's packs saved in place, here a scratch copy of the repo's; the game's other packs untouched.</summary>
    [Fact]
    public void TheDevSetupSavesTheGamesPackInPlace()
    {
        using var scratch = new Scratch();
        var repo = scratch.Repo();
        var audio = Path.Combine(repo.PacksFolder, "audio.pak");
        var others = Directory.GetFiles(repo.PacksFolder).Where(f => f != audio).ToDictionary(f => f, File.ReadAllBytes);
        var before = Stored(audio);
        using var session = new EditorSession(repo) { PlayerFolder = scratch.Player };
        session.Open(audio);
        Assert.True(session.CanSave);
        session.Change(session.At(GameRoom), "Seconds", 6.0);
        Assert.Equal(EntrySource.Pack, session.At(GameRoom).Source);
        session.Save();
        var after = Stored(audio);
        Assert.Equal(before.Keys.Order(StringComparer.Ordinal), after.Keys.Order(StringComparer.Ordinal));
        foreach (var path in before.Keys.Where(p => p != GameRoom))
            Assert.Equal(before[path], after[path]);
        Assert.Equal((true, Binary(GameRoomSpec() with { Seconds = 6.0 })), Contents(audio)[GameRoom]);
        foreach (var (file, bytes) in others)
            Assert.Equal(bytes, File.ReadAllBytes(file));
        // The repo's own pack is untouched.
        Assert.Equal(GameRoomSpec(), (RoomSpec)Form.FormTesting.AudioPack()[GameRoom]);
    }
}

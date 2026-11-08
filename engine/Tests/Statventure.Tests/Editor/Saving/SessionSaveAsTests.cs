using Statventure.Audio;
using Statventure.Editor.Session;
using Statventure.Packs;
using Statventure.Records;
using Statventure.Tests.Infrastructure;
using static Statventure.Tests.Editor.Checks.CheckTesting;
using static Statventure.Tests.Editor.Opening.OpeningTesting;
using static Statventure.Tests.Editor.Saving.SavingTesting;
using static Statventure.Tests.Packs.PackTestData;

namespace Statventure.Tests.Editor.Saving;

/// <summary>
/// Save As (M11-16, P24; the manager's decisions Q3–Q5 of 2026-11-08; Docs/Editor.md, "How it works", step 11): the entries
/// ticked written to a new file as a save writes them and opened in place of the open pack; what a pack that can't be
/// fully read offers; the entries ticked checked as the game would see them; the files it refuses.
/// </summary>
public class SessionSaveAsTests
{
    const string Crypt = "audio/rooms/crypt", Notes = "notes/raw.txt", Boom = "audio/effects/boom", Bad = "audio/effects/bad",
        Intro = "world/cutscenes/intro";

    static readonly RoomSpec CryptRoom = new(2.5, 0.5, 0.25);

    [Fact]
    public void TheEntriesTickedAreWrittenAsASaveWritesThemAndTheNewFileOpens()
    {
        using var scratch = new Scratch();
        var raw = new Packs.RawPack();
        raw.Raw(Boom, RecordSerializer.ToBinary(Patch()));
        raw.Entries[^1].Flags = 2;
        raw.Raw(Crypt, RecordSerializer.ToBinary(CryptRoom));
        raw.Entries[^1].Flags = 2;
        raw.Raw(Notes, Prose(2000, 2));
        var mod = scratch.Mod(raw);
        using var session = scratch.Session(mod);
        session.Change(session.At(Crypt), "Wet", 0.75);
        session.Rename(session.At(Notes), "notes/moved.txt");
        session.Change(session.At(GameRoom), "Dark", 0.125);
        var original = File.ReadAllBytes(mod);
        var before = Stored(mod);
        var copy = scratch["player/mods/copy.pak"];
        Assert.Equal([Boom, Crypt, GameRoom, "notes/moved.txt"], session.SaveEntries.Select(e => e.Path));
        var opened = session.SaveAs(copy, [session.At(Crypt), session.At("notes/moved.txt"), session.At(GameRoom)]);
        var contents = Contents(copy);
        Assert.Equal([Crypt, GameRoom, "notes/moved.txt"], contents.Keys.Order(StringComparer.Ordinal));
        Assert.Equal((true, Binary(CryptRoom with { Wet = 0.75 })), contents[Crypt]);
        Assert.Equal((true, Binary(GameRoomSpec() with { Dark = 0.125 })), contents[GameRoom]);
        Assert.Equal(before[Notes], Stored(copy)["notes/moved.txt"]);
        // The open pack's file is as it was; the new one is open, read again, with nothing unsaved.
        Assert.Equal(original, File.ReadAllBytes(mod));
        Assert.Equal(copy, opened.File);
        Assert.Same(opened, session.Pack);
        Assert.Equal(copy, session.PackFile!.FilePath);
        Assert.False(session.HasUnsavedWork);
        Assert.True(session.CanSave);
        Assert.False(session.At(Crypt).CanUndo);
        Assert.Equal(EntrySource.Pack, session.At(GameRoom).Source);
        Assert.Null(session.Entry(Boom));
        // Saving as over the open pack (it takes changes) writes the entries ticked over it.
        session.Change(session.At(Crypt), "Seconds", 1.0);
        session.SaveAs(copy, [session.At(Crypt)]);
        Assert.Equal([Crypt], Contents(copy).Keys);
        Assert.Equal(copy, session.Pack!.File);
        // Only entries a save writes can be ticked.
        Assert.Throws<ArgumentException>(() => session.SaveAs(copy, [session.At("audio/rooms/cavern")]));
    }

    /// <summary>
    /// A pack that can't be fully read (M11-16, P24, Q4): the entries it reads offered ticked, those it can't read as records
    /// (a kind not known here, a newer version, a record that doesn't read) offered unticked with why and kept unread when
    /// ticked, a damaged one not offered (last). A new file keeping one opens read-only; without, it takes changes.
    /// </summary>
    [Fact]
    public void APackItCantFullyReadOffersWhatItCanKeep()
    {
        using var scratch = new Scratch();
        var mod = scratch.Mod(w =>
        {
            w.AddRecord(Crypt, CryptRoom);
            w.Add(Notes, "Some notes."u8);
            foreach (var (how, _) in UnreadableWays)
                AddUnreadable(w, $"parts/{how.Replace(' ', '-')}", how);
        });
        Damage(mod, "parts/damaged");
        using var session = scratch.Session(mod);
        Assert.False(session.CanSave);
        Assert.True(session.CanSaveAs);
        var choices = session.SaveAsChoices();
        Assert.Equal([Crypt, Notes, "parts/damaged", "parts/newer-version", "parts/not-a-record", "parts/refused-values", "parts/unknown-kind"],
            choices.Select(c => c.Entry.Path));
        Assert.All(choices.Take(2), c => Assert.Equal((true, true, null), (c.Offered, c.Ticked, c.Reason)));
        var damaged = choices[2];
        Assert.False(damaged.Offered);
        Assert.False(damaged.Ticked);
        Assert.False(damaged.KeptUnread);
        Assert.Equal("the content doesn't match its hash (damaged)", damaged.Reason);
        foreach (var choice in choices.Skip(3))
        {
            Assert.True(choice.Offered, choice.Entry.Path);
            Assert.False(choice.Ticked, choice.Entry.Path);
            Assert.True(choice.KeptUnread, choice.Entry.Path);
            Assert.Equal(choice.Entry.Reason, choice.Reason);
        }
        Assert.False(choices[0].KeptUnread);
        var before = Stored(mod);
        // Kept unread: as stored, and the new file opens read-only.
        var kept = scratch["player/mods/kept.pak"];
        session.SaveAs(kept, [session.At(Crypt), session.At("parts/unknown-kind"), session.At("parts/newer-version")]);
        Assert.Equal(before["parts/unknown-kind"], Stored(kept)["parts/unknown-kind"]);
        Assert.Equal(before["parts/newer-version"], Stored(kept)["parts/newer-version"]);
        Assert.Equal("2 entries can't be read", session.Pack!.ReadOnlyReason);
        Assert.False(session.CanSave);
        // Without them: the new file takes changes.
        var clean = scratch["player/mods/clean.pak"];
        session.SaveAs(clean, [session.At(Crypt)]);
        Assert.True(session.CanSave);
        Assert.Equal([Crypt], Contents(clean).Keys);
    }

    /// <summary>
    /// Q5: the entries ticked checked as the game would see them with only them over its packs: a cutscene whose effect is
    /// left out is refused in the sequence's words; with the effect it isn't, though an entry left out has a problem of its
    /// own; a draft that doesn't build gives its first build problem; refused, nothing is written.
    /// </summary>
    [Fact]
    public void TheEntriesTickedAreCheckedAsTheGameWouldSeeThem()
    {
        using var scratch = new Scratch();
        var mod = scratch.Mod(w =>
        {
            w.AddRecord(Boom, Patch());
            w.AddRecord(Bad, Patch());
            w.AddRecord(Intro, Cutscene(Sound("boom")));
        });
        using var session = scratch.Session(mod);
        session.Change(session.At(Bad), "MaxVoices", 0);
        var copy = scratch["player/mods/copy.pak"];
        var (boom, bad, intro) = (session.At(Boom), session.At(Bad), session.At(Intro));
        Assert.Equal([new SaveProblem(Intro,
            "the sequence intro, track 0 (audio): cue 0 plays the effect boom, which isn't in the game's data (audio/effects/boom) (at $.tracks[0].cues[0].name)")],
            session.SaveAsProblems([intro], copy));
        Assert.Empty(session.SaveAsProblems([intro, boom], copy));
        Assert.Equal([new SaveProblem(Bad, "allows no voices (at $.maxVoices)")], session.SaveAsProblems([boom, bad], copy));
        Assert.Throws<InvalidOperationException>(() => session.SaveAs(copy, [intro]));
        Assert.False(File.Exists(copy));
        // A draft that doesn't build: its build problem, before any check.
        session.Commit(intro, intro.Draft!.With("Name", DraftValue.Missing));
        Assert.False(intro.Builds);
        Assert.Equal([new SaveProblem(Intro, intro.Problems[0].ToString())], session.SaveAsProblems([intro, boom], copy));
        session.Undo(intro);
        session.SaveAs(copy, [intro, boom]);
        Assert.Equal([Boom, Intro], Contents(copy).Keys.Order(StringComparer.Ordinal));
    }

    /// <summary>
    /// Q5's data: the game's packs with only the entries ticked on top, each as Save As writes it (an edited record as built,
    /// an unchanged one as stored, an imported file with its bytes, a renamed one at its path now), named by the new file.
    /// </summary>
    [Fact]
    public void TheDataCheckedIsTheGamesWithTheEntriesTicked()
    {
        using var scratch = new Scratch();
        var mod = scratch.Mod(w =>
        {
            w.AddRecord(Boom, Patch());
            w.AddRecord(Crypt, CryptRoom);
            w.Add(Notes, "Raw notes."u8);
        });
        using var session = scratch.Session(mod);
        session.Change(session.At(Crypt), "Wet", 0.75);
        session.Rename(session.At(Notes), "notes/moved.txt");
        var readme = scratch.Folder.Write("downloads/readme.txt", "Read me first.");
        session.Import(session.PlanImport(session.ImportSourceOf(readme), "notes/readme.txt"));
        var copy = scratch["player/mods/copy.pak"];
        using var data = session.SaveAsData([session.At(Crypt), session.At("notes/moved.txt"), session.At("notes/readme.txt"), session.At(Boom)], copy);
        Assert.Equal(CryptRoom with { Wet = 0.75 }, data.Load<RoomSpec>(Crypt));
        Assert.Equal(Binary(Patch()), Binary(data.Load<EffectPatch>(Boom)));
        Assert.Equal("Raw notes."u8.ToArray(), data.ReadAllBytes("notes/moved.txt"));
        Assert.Equal("Read me first."u8.ToArray(), data.ReadAllBytes("notes/readme.txt"));
        Assert.False(data.Exists(Notes));
        Assert.Equal(GameRoomSpec(), data.Load<RoomSpec>(GameRoom));
        using var only = session.SaveAsData([session.At(Crypt)], copy);
        Assert.False(only.Exists(Boom));
        // Named by the new file, as the saved pack's would be.
        var wrong = Assert.Throws<DataFileException>(() => only.ReadAllBytes(Crypt));
        Assert.Contains(copy, wrong.Message);
    }

    /// <summary>Q5: a kind's check that throws is the entry's problem in its words, not the end of the checks.</summary>
    [Fact]
    public void ACheckThatFailsIsTheEntrysProblem()
    {
        using var scratch = new Scratch();
        var mod = scratch.Mod(w =>
        {
            w.AddRecord("checks/across", new Records.Variants.BreakingCheck { Breaks = "across" });
            w.AddRecord("checks/fine", new Records.Variants.BreakingCheck());
        });
        using var session = scratch.Session(mod, new RecordTypes(new Dictionary<string, Type>(Records.RecordFixtures.Types) { ["variant.breaking-check"] = typeof(Records.Variants.BreakingCheck) }));
        Assert.True(session.CanSave);
        Assert.Equal([new SaveProblem("checks/across", "its check failed: the check broke")],
            session.SaveAsProblems([session.At("checks/across"), session.At("checks/fine")], scratch["player/mods/copy.pak"]));
    }

    /// <summary>
    /// Q5, references resolved against the data of the entries ticked: a theme's room left out isn't there; a game entry
    /// under a copy left out is (the game's own shows through).
    /// </summary>
    [Fact]
    public void ReferencesResolveAgainstTheEntriesTickedOverTheGames()
    {
        using var scratch = new Scratch();
        var vale = Game<ThemeSpec>("audio/themes/alder-vale");
        var mod = scratch.Mod(w =>
        {
            w.AddRecord("audio/rooms/attic", CryptRoom);
            w.AddRecord("audio/themes/alder-vale", vale with { Room = "attic" });
            w.AddRecord("audio/themes/forest-theme", vale with { Id = "forest-theme", Room = "hall" });
            w.AddRecord(GameRoom, GameRoomSpec() with { Wet = 0.5 });
        });
        using var session = scratch.Session(mod);
        Assert.Empty(session.SaveProblems());
        var copy = scratch["player/mods/copy.pak"];
        Assert.Equal([new SaveProblem("audio/themes/alder-vale", "no room \"attic\" (audio/rooms/attic) (at $.room)")],
            session.SaveAsProblems([session.At("audio/themes/alder-vale")], copy));
        Assert.Empty(session.SaveAsProblems([session.At("audio/themes/alder-vale"), session.At("audio/rooms/attic")], copy));
        // The mod's hall left out: the game's hall is there.
        Assert.Empty(session.SaveAsProblems([session.At("audio/themes/forest-theme")], copy));
    }

    /// <summary>
    /// Q5: a game entry under a ticked one isn't what the game sees, so it isn't checked: the game's own room of a theme taken
    /// by an effect breaks the game's themes playing in it, but not the theme the mod's copy (ticked) moves to another room.
    /// </summary>
    [Fact]
    public void AGameEntryUnderATickedOneIsntChecked()
    {
        using var scratch = new Scratch();
        var vale = Game<ThemeSpec>("audio/themes/alder-vale");
        var room = $"audio/rooms/{vale.Room}";
        var mod = scratch.Mod(w =>
        {
            w.AddRecord("audio/rooms/attic", CryptRoom);
            w.AddRecord("audio/themes/alder-vale", vale with { Room = "attic" });
            w.AddRecord(room, Patch());
        });
        using var session = scratch.Session(mod);
        var problems = session.SaveAsProblems([session.At("audio/rooms/attic"), session.At("audio/themes/alder-vale"), session.At(room)], scratch["player/mods/copy.pak"]);
        Assert.NotEmpty(problems);
        Assert.All(problems, p => Assert.StartsWith("audio/themes/", p.Entry));
        Assert.DoesNotContain("audio/themes/alder-vale", problems.Select(p => p.Entry));
    }

    /// <summary>Q5: the music's rule only a theme's path tells (step 10's own rule for it), at its id, in the entry's words.</summary>
    [Fact]
    public void AThemesIdIsCheckedAgainstItsPath()
    {
        using var scratch = new Scratch();
        var mod = scratch.Mod(w => w.AddRecord("audio/themes/my-vale", Game<ThemeSpec>("audio/themes/alder-vale")));
        using var session = scratch.Session(mod);
        Assert.Equal([new SaveProblem("audio/themes/my-vale", "the theme's id is 'alder-vale', but its path holds 'my-vale' (a theme's path is made from its id) (at $.id)")],
            session.SaveAsProblems([session.At("audio/themes/my-vale")], scratch["player/mods/copy.pak"]));
    }

    /// <summary>
    /// Q5, the game's entries checked against the entries ticked (as step 10 checks the session): a mod's instrument that makes
    /// no sound breaks every game theme playing it, the game's own of a theme whose copy is left out too; the music's
    /// loading runs only when no music entry has a problem.
    /// </summary>
    [Fact]
    public void TheGamesEntriesAreCheckedAgainstTheEntriesTicked()
    {
        using var scratch = new Scratch();
        var cellos = Game<InstrumentSpec>("audio/instruments/cellos");
        var broken = cellos with { Voices = [] };
        using var data = new VirtualFileSystem([Mount.Pack(Path.Combine(TestPaths.Packs, "audio.pak")), Mount.Pack(scratch.Mod(w => w.AddRecord("audio/instruments/cellos", broken), "only.pak"))]);
        var refused = Form.FormTesting.AudioPack().Where(p => p.Value is ThemeSpec t && RecordChecks.Check(t, data).Count > 0).Select(p => p.Key).ToList();
        Assert.True(refused.Count >= 2);
        var copied = refused[0];
        var mod = scratch.Mod(w =>
        {
            w.AddRecord("audio/instruments/cellos", broken);
            // A copy of a theme playing them, the same as the game's: left out, the game's own is what the game plays.
            w.AddRecord(copied, Game<ThemeSpec>(copied));
        });
        using var session = scratch.Session(mod);
        var problems = session.SaveAsProblems([session.At("audio/instruments/cellos")], scratch["player/mods/copy.pak"]);
        Assert.Equal(refused, problems.Select(p => p.Entry));
        Assert.All(problems, p => Assert.StartsWith("Instrument cellos of kind bowed makes no sound (at $.", p.Problem));
    }

    /// <summary>Q5, the music's loading last: another kind in a music folder at the entry it names; a file there, which no record entry can show, as the data's problem naming the new file.</summary>
    [Fact]
    public void TheMusicsLoadingRunsLast()
    {
        using var scratch = new Scratch();
        var mod = scratch.Mod(w =>
        {
            w.AddRecord("audio/rooms/echo", Patch());
            w.Add("audio/rooms/noise", "Not a room."u8);
            w.Add("notes/x.json", "{}"u8);
        });
        using var session = scratch.Session(mod);
        var copy = scratch["player/mods/copy.pak"];
        var echo = Assert.Single(session.SaveAsProblems([session.At("audio/rooms/echo")], copy));
        Assert.Equal("audio/rooms/echo", echo.Entry);
        using (var both = new VirtualFileSystem([Mount.Pack(Path.Combine(TestPaths.Packs, "audio.pak")), Mount.Pack(mod)]))
            Assert.Equal(Assert.Throws<DataFileException>(() => MusicLibrary.Load(both)).Problem, echo.Problem);
        var noise = Assert.Single(session.SaveAsProblems([session.At("audio/rooms/noise")], copy));
        Assert.Null(noise.Entry);
        Assert.Contains(copy, noise.Problem);
        Assert.Contains("audio/rooms/noise", noise.Problem);
        // Entries that can't be mounted together: the data's problem.
        var x = session.Add("notes/x", RecordDraft.From(Patch()));
        var clash = Assert.Single(session.SaveAsProblems([x, session.At("notes/x.json")], copy));
        Assert.Null(clash.Entry);
        Assert.Contains("notes/x", clash.Problem);
        Assert.Empty(session.SaveAsProblems([x], copy));
    }

    /// <summary>M11-5 and M11-16: the game's packs aren't saved over outside the dev setup, nor the open pack when it can't be.</summary>
    [Fact]
    public void SaveAsRefusesTheGamesPacksAndAPackThatCantBeSavedOver()
    {
        using var scratch = new Scratch();
        var mod = scratch.Mod(w => w.AddRecord(Crypt, CryptRoom));
        var installed = new GameData(scratch.Repo().PacksFolder, DevSetup: false);
        var audio = Path.Combine(installed.PacksFolder, "audio.pak");
        var audioBytes = File.ReadAllBytes(audio);
        using (var session = new EditorSession(installed) { PlayerFolder = scratch.Player })
        {
            session.Open(mod);
            Assert.Equal("audio.pak is one of the game's packs, which are read-only (only the repo's dev setup saves into them)", session.SaveAsFileProblem(audio));
            Assert.Null(session.SaveAsFileProblem(Path.Combine(installed.PacksFolder, "extra.pak")));
            Assert.Null(session.SaveAsFileProblem(mod));
            var refused = Assert.Throws<InvalidOperationException>(() => session.SaveAs(audio, [session.At(Crypt)]));
            Assert.StartsWith("audio.pak is one of the game's packs", refused.Message);
            Assert.Equal(audioBytes, File.ReadAllBytes(audio));
            // A game pack open read-only can't be saved over either, by either rule.
            session.Open(audio);
            Assert.False(session.CanSave);
            Assert.NotNull(session.SaveAsFileProblem(audio));
        }
        // In the dev setup a game pack may be written.
        using (var dev = new EditorSession(GameData.Repo(scratch["repo"])) { PlayerFolder = scratch.Player })
        {
            dev.Open(mod);
            Assert.Null(dev.SaveAsFileProblem(audio));
        }
        // The open pack when it can't be fully read.
        var unreadable = scratch.Mod(w =>
        {
            w.AddRecord(Crypt, CryptRoom);
            AddUnreadable(w, "parts/arm", "unknown kind");
        }, "broken.pak");
        using var broken = scratch.Session(unreadable);
        Assert.Equal("broken.pak is the pack open here, which can't be saved over: an entry can't be read", broken.SaveAsFileProblem(unreadable));
        Assert.Throws<InvalidOperationException>(() => broken.SaveAs(unreadable, [broken.At(Crypt)]));
        Assert.Null(broken.SaveAsFileProblem(scratch["player/mods/other.pak"]));
    }
}

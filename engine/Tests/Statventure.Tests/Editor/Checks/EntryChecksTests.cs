using Statventure.Audio;
using Statventure.Editor.Checks;
using Statventure.Editor.Session;
using Statventure.Packs;
using Statventure.Records;
using Statventure.Tests.Infrastructure;
using static Statventure.Tests.Editor.Checks.CheckTesting;

namespace Statventure.Tests.Editor.Checks;

/// <summary>
/// The session's checks (M11-12, P16, P17; Docs/Editor.md, "How it works", step 10): each entry's own check at every
/// change of its draft; the checks across entries (the data as edited, the session's references, the game's music
/// loading) when they run, over every record entry, the game's too; only the first problem at each path; what saving
/// reads (step 11).
/// </summary>
public class EntryChecksTests
{
    const string Boom = "audio/effects/boom";

    static RecordPath At(string field) => RecordPath.Root.Field(field);

    /// <summary>
    /// An entry's own check runs at each commit (no pause): the kind's rule at its field, in the words the game logs;
    /// the declared limit holding the same mistake at the same path isn't shown (the first problem per field, the
    /// manager's decision); undo takes it back at once.
    /// </summary>
    [Fact]
    public void TheEntrysOwnCheckRunsAtEachCommitShowingTheFirstProblemPerField()
    {
        using var session = Session(w => w.AddRecord(Boom, Patch()));
        var boom = session.At(Boom);
        Assert.Empty(boom.Problems);
        session.Change(boom, "MaxVoices", 0);
        // Both the kind's rule and the limit report it (the kind's first)...
        Assert.Equal(["allows no voices (at $.maxVoices)", "must be 1 or more (it is 0) (at $.maxVoices)"],
            RecordChecks.Check(boom.Build!.Value!).Select(p => p.ToString()));
        // ...the entry shows the first only, at once.
        Assert.Equal(["allows no voices (at $.maxVoices)"], boom.Shown());
        Assert.True(session.Checks.Pending);
        session.Undo(boom);
        Assert.Empty(boom.Problems);
    }

    [Fact]
    public void OnlyTheFirstProblemAtEachPathIsKept()
    {
        var a = At("a");
        var b = At("b").Index(0);
        Assert.Equal(
            [new RecordProblem(a, "one"), new RecordProblem(b, "two"), new RecordProblem(RecordPath.Root, "four")],
            EntryChecks.FirstPerPath([new(a, "one"), new(b, "two"), new(At("a"), "three"), new(RecordPath.Root, "four"), new(RecordPath.Root, "five")]));
        Assert.Empty(EntryChecks.FirstPerPath([]));
    }

    /// <summary>
    /// A cutscene naming a sound effect the data hasn't: no problem until the checks across entries run; then one at
    /// the cue's name, the sequence's own words (its check against the data), not the reference mark's at the same
    /// path. A new effect of that name, unsaved, is in the data as edited: the next run takes the problem away.
    /// </summary>
    [Fact]
    public void ChecksAcrossEntriesRunOnTheDataAsEditedWithTheKindsWordsFirst()
    {
        using var session = Session(w => w.AddRecord("world/cutscenes/intro", Cutscene(Sound("boom"))));
        var intro = session.At("world/cutscenes/intro");
        Assert.Empty(intro.Problems);
        Assert.True(session.Checks.Pending);
        session.Checks.Run();
        Assert.False(session.Checks.Pending);
        var cue = At("tracks").Index(0).Field("cues").Index(0).Field("name");
        var all = RecordChecks.Check(intro.Build!.Value!, session.EditedData, new Statventure.Editor.References.SessionReferences(session));
        Assert.Equal(2, all.Count(p => p.Path.Equals(cue)));
        Assert.Equal(["the sequence intro, track 0 (audio): cue 0 plays the effect boom, which isn't in the game's data (audio/effects/boom)"],
            intro.Problems.Select(p => p.Message).ToArray());
        Assert.Equal(cue, intro.Problems[0].Path);
        session.Add(Boom, RecordDraft.From(Patch()));
        Assert.Single(intro.Problems);
        session.Checks.Run();
        Assert.Empty(intro.Problems);
    }

    /// <summary>
    /// After a commit the entry's own problems are fresh at once, and those across entries from the last run stay until
    /// the next run replaces them (the manager's decision Q-B, 2026-11-07).
    /// </summary>
    [Fact]
    public void ProblemsAcrossEntriesStayUntilTheNextRun()
    {
        var theme = Game<ThemeSpec>("audio/themes/alder-vale") with { Room = "attic" };
        using var session = Session(w => w.AddRecord("audio/themes/alder-vale", theme));
        var entry = session.At("audio/themes/alder-vale");
        Assert.Empty(entry.Problems);
        session.Checks.Run();
        const string room = "no room \"attic\" (audio/rooms/attic) (at $.room)";
        Assert.Equal([room], entry.Shown());
        // An own problem joins it at once; the room's stays (first: the own one).
        session.Change(entry, "Mode", "blues");
        Assert.Equal(["Unknown mode blues (at $.mode)", room], entry.Shown());
        // The room put right: its problem stays until the run.
        session.Change(entry, "Room", "cavern");
        Assert.Equal(["Unknown mode blues (at $.mode)", room], entry.Shown());
        session.Checks.Run();
        Assert.Equal(["Unknown mode blues (at $.mode)"], entry.Shown());
        // An own problem put right goes at once, also after a run (what a run keeps is only what it adds).
        session.Change(entry, "Mode", "dorian");
        Assert.Empty(entry.Problems);
    }

    /// <summary>
    /// The game's own entries are checked across entries too: a mod's copy of an instrument that makes no sound breaks
    /// every game theme playing it (as the game's music loading would refuse the mod), each at its layer's instrument.
    /// </summary>
    [Fact]
    public void TheGamesEntriesAreCheckedAgainstTheMod()
    {
        var cellos = Game<InstrumentSpec>("audio/instruments/cellos");
        Assert.Equal("bowed", cellos.Kind);
        var mod = Mod(w => w.AddRecord("audio/instruments/cellos", cellos with { Voices = [] }));
        using var session = Session(_ => { });
        session.Open(mod);
        session.Checks.Run();
        var broken = session.EntriesWithProblems;
        Assert.NotEmpty(broken);
        Assert.All(broken, e =>
        {
            Assert.Equal(EntrySource.Game, e.Source);
            Assert.StartsWith("audio/themes/", e.Path);
            Assert.Equal("Instrument cellos of kind bowed makes no sound", e.Problems[0].Message);
            Assert.EndsWith(".instrument", e.Problems[0].Path.ToString());
        });
        // Exactly the themes the game's check refuses with the mod mounted over its packs.
        using var data = new VirtualFileSystem([Mount.Pack(Path.Combine(TestPaths.Packs, "audio.pak")), Mount.Pack(mod)]);
        var refused = Form.FormTesting.AudioPack().Where(p => p.Value is ThemeSpec t && RecordChecks.Check(t, data).Count > 0).Select(p => p.Key);
        Assert.Equal(refused, broken.Select(e => e.Path));
        Assert.Equal([.. broken.Select(e => e.Path).Order(StringComparer.Ordinal)], broken.Select(e => e.Path));
    }

    /// <summary>
    /// The music loading's rule only an entry's path tells (a theme's id against its path) is the entry's own: at its
    /// id at once, in the loader's words; a rename to the path its id makes takes it away at once; outside the music's
    /// folder the rule doesn't hold.
    /// </summary>
    [Fact]
    public void AThemesIdIsCheckedAgainstItsPath()
    {
        var theme = Game<ThemeSpec>("audio/themes/alder-vale") with { Id = "alder-vail" };
        using var session = Session(w =>
        {
            w.AddRecord("audio/themes/my-vale", theme);
            w.AddRecord("notes/vale", theme);
        });
        var entry = session.At("audio/themes/my-vale");
        Assert.Equal(["the theme's id is 'alder-vail', but its path holds 'my-vale' (a theme's path is made from its id) (at $.id)"], entry.Shown());
        Assert.Empty(session.At("notes/vale").Problems);
        session.Rename(entry, "audio/themes/alder-vail");
        Assert.Empty(entry.Problems);
        session.Change(entry, "Id", "combat@alder-vail");
        Assert.Equal(["the theme's id is 'combat@alder-vail', but its path holds 'alder-vail' (a theme's path is made from its id) (at $.id)"], entry.Shown());
        session.Rename(entry, "audio/themes/combat.alder-vail");
        Assert.Empty(entry.Problems);
        // What the game's music loading says of the same: the same words.
        var refused = Assert.Throws<DataFileException>(() =>
        {
            using var data = new VirtualFileSystem([Mount.Pack(Path.Combine(TestPaths.Packs, "audio.pak")), Mount.Pack(Mod(w => w.AddRecord("audio/themes/my-vale", theme)))]);
            MusicLibrary.Load(data);
        });
        Assert.Equal("the theme's id is 'alder-vail', but its path holds 'my-vale' (a theme's path is made from its id)", refused.Problem);
    }

    /// <summary>
    /// The game's music loading runs last, while no music entry has a problem: what only it refuses (a music folder
    /// holding another kind) is shown at the root of the entry it names, in its words; with a problem in a music entry
    /// it doesn't run; a file it refuses, which can't show a problem, is the data's problem.
    /// </summary>
    [Fact]
    public void TheGamesMusicLoadingRunsLast()
    {
        var mod = Mod(w => w.AddRecord("audio/rooms/echo", Patch()));
        using var session = Session(_ => { });
        session.Open(mod);
        session.Checks.Run();
        var echo = session.At("audio/rooms/echo");
        string refusedProblem;
        using (var data = new VirtualFileSystem([Mount.Pack(Path.Combine(TestPaths.Packs, "audio.pak")), Mount.Pack(mod)]))
        {
            var refused = Assert.Throws<DataFileException>(() => MusicLibrary.Load(data));
            Assert.Equal("audio/rooms/echo", refused.Entry);
            Assert.Equal([new RecordProblem(RecordPath.Root, refused.Problem)], echo.Problems);
            refusedProblem = refused.Problem;
        }
        Assert.Null(session.Checks.DataProblem);
        Assert.Equal([echo], session.EntriesWithProblems);
        // Another entry's problem (not under audio/) doesn't hold it back.
        var intro = session.Add("world/cutscenes/intro", RecordDraft.From(Cutscene(Sound("nothing"))));
        session.Checks.Run();
        Assert.Single(intro.Problems);
        Assert.Equal([new RecordProblem(RecordPath.Root, refusedProblem)], echo.Problems);
        session.Remove(intro);
        // A theme with a problem found across entries (a room that isn't there): the music's loading doesn't run.
        var valley = session.At("audio/themes/alder-vale");
        session.Change(valley, "Room", "attic");
        session.Checks.Run();
        Assert.Equal(["no room \"attic\" (audio/rooms/attic) (at $.room)"], valley.Shown());
        Assert.Empty(echo.Problems);
        session.Change(valley, "Room", "hall");
        session.Checks.Run();
        Assert.Single(echo.Problems);
        // A theme with a problem of its own: the music's loading doesn't run, so nothing is said of the echo.
        var vale = session.At("audio/themes/alder-vale");
        session.Change(vale, "Mode", "blues");
        session.Checks.Run();
        Assert.Empty(echo.Problems);
        Assert.Equal([vale], session.EntriesWithProblems);
        // A file it refuses: no entry shows it, the data has it.
        session.Open(Mod(w => w.Add("audio/rooms/echo", "Not a room."u8)));
        session.Checks.Run();
        Assert.Empty(session.EntriesWithProblems);
        Assert.Contains("audio/rooms/echo", session.Checks.DataProblem);
        session.Open(Mod(w => w.AddRecord("audio/rooms/echo", Game<RoomSpec>("audio/rooms/hall"))));
        session.Checks.Run();
        Assert.Null(session.Checks.DataProblem);
        Assert.Empty(session.EntriesWithProblems);
    }

    /// <summary>
    /// References resolve against the session's own entries (M11-11, P18): a name no entry has is a problem; a new entry
    /// of the name, unsaved, resolves it; an entry of another kind there is one too.
    /// </summary>
    [Fact]
    public void ReferencesResolveAgainstTheSessionsEntries()
    {
        var theme = Game<ThemeSpec>("audio/themes/alder-vale") with { Room = "attic" };
        using var session = Session(w => w.AddRecord("audio/themes/alder-vale", theme));
        var entry = session.At("audio/themes/alder-vale");
        session.Checks.Run();
        Assert.Equal(["no room \"attic\" (audio/rooms/attic) (at $.room)"], entry.Shown());
        var attic = session.Add("audio/rooms/attic", RecordDraft.From(Game<RoomSpec>("audio/rooms/cavern")));
        session.Checks.Run();
        Assert.Empty(entry.Problems);
        session.Remove(attic);
        session.Add("audio/rooms/attic", RecordDraft.From(Patch()));
        session.Checks.Run();
        Assert.Equal("audio/rooms/attic isn't a room: it holds a audio.effect record", entry.Problems[0].Message);
        Assert.Equal(At("room"), entry.Problems[0].Path);
    }

    /// <summary>
    /// A run reports only the entries whose problems changed (the list and the form redraw only those), each once;
    /// <see cref="EntryChecks.RunPending"/> runs only when something changed; a removed entry and a pack opened again
    /// leave nothing of the last run behind.
    /// </summary>
    [Fact]
    public void ARunReportsOnlyWhatChanged()
    {
        var theme = Game<ThemeSpec>("audio/themes/alder-vale") with { Room = "attic" };
        using var session = Session(w =>
        {
            w.AddRecord("audio/themes/alder-vale", theme);
            w.AddRecord(Boom, Patch());
        });
        var changed = new List<string>();
        var ran = 0;
        session.EntryChanged += e => changed.Add(e.Path);
        session.Checks.Ran += () => ran++;
        session.Checks.RunPending();
        Assert.Equal(["audio/themes/alder-vale"], changed);
        Assert.Equal(1, ran);
        Assert.False(session.Checks.Pending);
        session.Checks.RunPending();
        Assert.Equal(1, ran);
        changed.Clear();
        session.Checks.Run();
        Assert.Empty(changed);
        Assert.Equal(2, ran);
        // A change elsewhere: due again, and only the entry changed is reported by its commit.
        var boom = session.At(Boom);
        session.Change(boom, "Level", 0.5);
        Assert.True(session.Checks.Pending);
        Assert.Equal([Boom], changed);
        changed.Clear();
        session.Checks.RunPending();
        Assert.Empty(changed);
        // Removed: no longer among those with problems; the run doesn't bring it back.
        var vale = session.At("audio/themes/alder-vale");
        Assert.Single(vale.Problems);
        session.Remove(vale);
        session.Checks.Run();
        Assert.DoesNotContain(vale, session.EntriesWithProblems);
        Assert.Empty(session.EntriesWithProblems);
    }

    /// <summary>What saving reads (step 11): the entries with problems in path order, each with its first problem first; build problems too.</summary>
    [Fact]
    public void TheEntriesWithProblemsAreInPathOrder()
    {
        using var session = Session(w =>
        {
            w.AddRecord("audio/effects/zap", Patch() with { MaxVoices = 0 });
            w.AddRecord(Boom, Patch() with { Layers = [] });
            w.AddRecord("audio/effects/fine", Patch());
        });
        var blank = session.Add("audio/rooms/new", RecordDraft.Blank(typeof(RoomSpec)));
        session.Checks.Run();
        Assert.Equal([Boom, "audio/effects/zap", "audio/rooms/new"], session.EntriesWithProblems.Select(e => e.Path));
        Assert.Equal("has no layers (at $.layers)", session.At(Boom).Problems[0].ToString());
        Assert.Equal("allows no voices (at $.maxVoices)", session.At("audio/effects/zap").Problems[0].ToString());
        Assert.False(blank.Builds);
        Assert.Equal(blank.Build!.Problems, blank.Problems);
        // Made to build after the run: its own problems only, nothing of the run.
        session.Commit(blank, RecordDraft.From(Game<RoomSpec>("audio/rooms/hall")));
        Assert.True(blank.Builds);
        Assert.Empty(blank.Problems);
    }

    /// <summary>A kind's check that throws is the entry's problem, at its root, in the exception's words, and the other entries are still checked.</summary>
    [Fact]
    public void ACheckThatFailsIsTheEntrysProblem()
    {
        var session = new EditorSession(GameData.Repo(EditorHarness.EngineFolder)) { Types = Form.FormTesting.Types, Writable = true };
        using (session)
        {
            _ = session.Checks;
            session.ShowEntries([
                Form.FormTesting.Entry("checks/across", new Records.Variants.BreakingCheck { Breaks = "across" }),
                Form.FormTesting.Entry("checks/fine", new Records.Variants.BreakingCheck()),
                Form.FormTesting.Entry("parts/arm", new Records.Part { Name = "leg" }),
            ]);
            Assert.Empty(session.EntriesWithProblems);
            var fine = session.At("checks/fine");
            session.Change(fine, "Breaks", "always");
            Assert.Equal(["its check failed: the check broke"], fine.Shown());
            session.Checks.Run();
            Assert.Equal(["its check failed: the check broke"], session.At("checks/across").Shown());
            Assert.Equal(["no body part \"leg\" (parts/leg) (at $.name)"], session.At("parts/arm").Shown());
            Assert.Equal(["checks/across", "checks/fine", "parts/arm"], session.EntriesWithProblems.Select(e => e.Path));
        }
    }

    /// <summary>
    /// Entries that can't be mounted together (a record beside a file of its path and ".json", which no pack holds): the
    /// data as edited isn't there, and why is the data's problem until they can be.
    /// </summary>
    [Fact]
    public void EntriesThatCantBeMountedTogetherAreTheDatasProblem()
    {
        using var session = Session(w => w.Add("notes/x.json", "{}"u8));
        session.Checks.Run();
        Assert.Null(session.Checks.DataProblem);
        var x = session.Add("notes/x", RecordDraft.From(Patch()));
        session.Checks.Run();
        Assert.Null(session.EditedData);
        Assert.NotNull(session.EditedDataProblem);
        Assert.Equal(session.EditedDataProblem, session.Checks.DataProblem);
        session.Remove(x);
        session.Checks.Run();
        Assert.Null(session.Checks.DataProblem);
    }

    /// <summary>A session without a pack open has no data as edited: references still resolve against its entries.</summary>
    [Fact]
    public void WithoutAPackReferencesStillResolve()
    {
        var session = new EditorSession(GameData.Repo(EditorHarness.EngineFolder)) { Types = Form.FormTesting.Types, Writable = true };
        using (session)
        {
            _ = session.Checks;
            session.ShowEntries([
                Form.FormTesting.Entry("parts/arm", new Records.Part { Name = "leg" }),
                Form.FormTesting.Entry("parts/leg", new Records.Part { Name = "arm" }),
            ]);
            session.Checks.Run();
            Assert.Null(session.EditedData);
            Assert.Null(session.Checks.DataProblem);
            Assert.Empty(session.EntriesWithProblems);
            session.Change(session.At("parts/arm"), "Name", "tail");
            session.Checks.Run();
            Assert.Equal(["no body part \"tail\" (parts/tail) (at $.name)"], session.At("parts/arm").Shown());
        }
    }
}

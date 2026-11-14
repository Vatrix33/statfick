using Statventure.Audio;
using Statventure.Editor.Session;
using Statventure.Records;
using Statventure.Tests.Packs;
using Statventure.Tests.Records.Variants;

namespace Statventure.Tests.Editor.Checks;

/// <summary>
/// The editor's checks across entries with the game's side taken once (GM1 step 6b; <c>EntryChecks.GameSideShortcut</c>):
/// after every change of a session every entry shows exactly what the plain run shows, and the data's problem is the same;
/// a game entry is checked again only when the session's own entries touch what its check asked the data about.
/// </summary>
public class EntryChecksGameSideTests
{
    /// <summary>
    /// Each entry's problems (path and words) and the data's problem, after a run with the shortcut and after a plain run
    /// (then, unless <paramref name="runAgain"/> is false, a run with the shortcut again, which the session keeps).
    /// </summary>
    static void SameAsPlain(EditorSession session, string when, bool runAgain = true)
    {
        session.Checks.GameSideShortcut = true;
        session.Checks.Run();
        var fast = Shown(session);
        session.Checks.GameSideShortcut = false;
        session.Checks.Run();
        var plain = Shown(session);
        session.Checks.GameSideShortcut = true;
        if (runAgain)
            session.Checks.Run();
        Assert.True(plain.SequenceEqual(fast), $"{when}: plain [{string.Join(" | ", plain)}] but [{string.Join(" | ", fast)}]");
    }

    static List<string> Shown(EditorSession session) =>
        [.. session.Entries.Where(e => e.Problems.Count > 0).Select(e => $"{e.Path}: {string.Join("; ", e.Problems)}"), $"data: {session.Checks.DataProblem}"];

    [Fact]
    public void EveryChangeShowsWhatThePlainRunShows()
    {
        var theme = ModCheckTesting.GameRecord<ThemeSpec>("audio/themes/alder-vale");
        var instrumentPath = MusicNames.PathOf(MusicNames.Instruments, theme.Sections.Values.SelectMany(s => s.Layers ?? []).First(l => l.Type == "pad").Instrument!);
        var instrument = ModCheckTesting.GameRecord<InstrumentSpec>(instrumentPath);
        var order = ModCheckTesting.GameRecord<ThemeOrder>("audio/theme-order");
        using var session = CheckTesting.Session(w => w.AddRecord("audio/effects/chime", ModCheckTesting.Chime));
        SameAsPlain(session, "opened");
        Assert.Empty(session.EntriesWithProblems);
        // Nothing of the game's touched: no game entry checked again.
        Assert.Equal(0, session.Checks.GameRechecked);

        // An edit no game entry asks about (an effect: only the music lists them): still none.
        session.Change(session.At("audio/effects/chime"), "Level", 0.5);
        SameAsPlain(session, "the chime's level");
        Assert.Equal(0, session.Checks.GameRechecked);

        // The game's instrument the themes play changed in the session, made silent: the themes are refused, as in the
        // plain run, each checked again.
        session.Commit(session.At(instrumentPath), RecordDraft.From(instrument with { Voices = [] }));
        SameAsPlain(session, "the instrument silent");
        Assert.Contains(session.EntriesWithProblems, e => e.Path == "audio/themes/alder-vale");
        Assert.True(session.Checks.GameRechecked > 0);

        // Back as the game has it: nothing touched again, the problems gone.
        session.Commit(session.At(instrumentPath), RecordDraft.From(instrument));
        Assert.False(session.At(instrumentPath).IsChanged);
        SameAsPlain(session, "the instrument back");
        Assert.Empty(session.EntriesWithProblems);
        var rechecked = session.Checks.GameRechecked;
        session.Checks.Run();
        Assert.Equal(rechecked, session.Checks.GameRechecked);

        // A game entry changed in the session (the mod's copy of the theme order, naming a theme the data lacks).
        session.Commit(session.At("audio/theme-order"), RecordDraft.From(order with { Themes = [.. order.Themes, "nowhere"] }));
        SameAsPlain(session, "the order changed");
        Assert.Contains(session.EntriesWithProblems, e => e.Path == "audio/theme-order");

        // A new music entry that doesn't build (it holds the music back), then removed.
        var blank = session.Add("audio/rooms/new", RecordDraft.Blank(typeof(RoomSpec)));
        SameAsPlain(session, "a room that doesn't build");
        session.Remove(blank);
        SameAsPlain(session, "the room removed");
    }

    /// <summary>The music's loading refusing a file the mod puts in a folder it lists: the data's problem is the plain run's.</summary>
    [Fact]
    public void ADataProblemIsThePlainRunsToo()
    {
        using var session = CheckTesting.Session(w => w.Add("audio/rooms/echo", "Not a room."u8));
        SameAsPlain(session, "a file the music refuses");
        Assert.Contains("audio/rooms/echo", session.Checks.DataProblem);
    }

    /// <summary>
    /// A game of the tests' own whose records ask the data in several ways (<see cref="AsksData"/>: whether a path is there,
    /// a folder's paths two levels down, a record loaded, the mounts): the session's own entries added where they ask, a
    /// game entry one of them loads changed, and entries that can't be mounted together (no data as edited: the plain
    /// run's checks without the data, the game's side not used).
    /// </summary>
    [Fact]
    public void AGameAskingTheDataInAnyWayShowsWhatThePlainRunShows()
    {
        static AsksData Asks(string way, string at = "") => new() { Way = way, At = at };
        var game = CheckTesting.Mod(w =>
        {
            w.AddRecord("asks/exists", Asks("exists", "data/new"));
            w.AddRecord("asks/list", Asks("list", "deep"));
            w.AddRecord("asks/load", Asks("load", "targets/one"));
            w.AddRecord("asks/mounts", Asks("mounts"));
            // One the session never touches (its answer over the game's packs kept throughout).
            w.AddRecord("asks/elsewhere", Asks("exists", "elsewhere/x"));
            w.AddRecord("targets/one", Asks("first"));
            w.Add("deep/x/y.txt", "y"u8);
        }, "data.pak");
        using var session = new EditorSession(new GameData(Path.GetDirectoryName(game)!, false) { Types = AsksData.Kinds });
        _ = session.Checks;
        session.Open(CheckTesting.Mod(w => w.Add("notes/x.json", "{}"u8)));
        SameAsPlain(session, "opened");
        // The record asking for the mounts is checked again (the open pack is one more mount, whatever it holds).
        Assert.True(session.Checks.GameRechecked > 0);
        Assert.Contains(session.EntriesWithProblems, e => e.Path == "asks/mounts");
        Assert.Contains(session.EntriesWithProblems, e => e.Path == "asks/elsewhere");
        var added = session.Add("data/new", RecordDraft.From(Asks("added")));
        SameAsPlain(session, "a path asked about added");
        var deep = session.Add("deep/x/z", RecordDraft.From(Asks("deeper")));
        SameAsPlain(session, "a path below a folder listed added");
        session.Commit(session.At("targets/one"), RecordDraft.From(Asks("changed")));
        SameAsPlain(session, "a game entry loaded changed");
        // No data as edited: the checks without the data, as the plain run's. (Two runs only: a third read of the session's
        // EditedData with the same entries gives the data mounted before them, with no problem, an editor bug found here and
        // reported, outside this step.)
        var beside = session.Add("notes/x", RecordDraft.From(Asks("beside")));
        SameAsPlain(session, "entries that can't be mounted together", runAgain: false);
        Assert.NotNull(session.Checks.DataProblem);
        Assert.DoesNotContain(session.EntriesWithProblems, e => e.Path.StartsWith("asks/", StringComparison.Ordinal));
        session.Remove(beside);
        session.Remove(deep);
        session.Remove(added);
        SameAsPlain(session, "back");
    }

    /// <summary>Another pack opened in the same session: its game entries checked over its own game's packs, as the plain run does.</summary>
    [Fact]
    public void AnotherPackOpenedIsCheckedOverItsOwnBase()
    {
        var theme = ModCheckTesting.GameRecord<ThemeSpec>("audio/themes/alder-vale");
        var instrumentPath = MusicNames.PathOf(MusicNames.Instruments, theme.Sections.Values.SelectMany(s => s.Layers ?? []).First(l => l.Type == "pad").Instrument!);
        var instrument = ModCheckTesting.GameRecord<InstrumentSpec>(instrumentPath);
        using var session = CheckTesting.Session(w => w.AddRecord("audio/effects/chime", ModCheckTesting.Chime));
        SameAsPlain(session, "the first pack");
        Assert.Empty(session.EntriesWithProblems);
        session.Open(CheckTesting.Mod(w => w.AddRecord(instrumentPath, instrument with { Voices = [] })));
        SameAsPlain(session, "another pack");
        Assert.Contains(session.EntriesWithProblems, e => e.Path == "audio/themes/alder-vale");
    }
}

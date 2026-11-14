using System.Runtime.CompilerServices;
using Statventure.Audio;
using Statventure.Editor.References;
using Statventure.Editor.Session;
using Statventure.Packs;
using Statventure.Records;

namespace Statventure.Editor.Checks;

/// <summary>
/// The editor's checks of its entries (M11-12, P16, P17; Docs/Editor.md, "How it works", step 10). Each record entry
/// that builds shows the problems of two checks:
/// <list type="bullet">
/// <item><b>Its own</b>, run at every change of its draft (each commit, undo and redo; <see cref="EditorSession.AddCheck"/>):
/// its kind's checks and its marks' (limits, choices) on the value alone (<see cref="RecordChecks.Check"/> with no data
/// and no lookup), and the rule the music's loading keeps that only its path can tell (a theme's id against its path).</item>
/// <item><b>Across entries</b>, run for every record entry, the game's too, a pause after the last change (<see cref="Pause"/>;
/// <see cref="Run"/>, scheduled by the view's <see cref="ChecksPart"/>): the same with the data as the game would see it
/// after saving (<see cref="EditorSession.EditedData"/>) and the references resolved against the session's entries
/// (<see cref="SessionReferences"/>), and then, while no music entry has a problem, the game's own music loading
/// (<see cref="MusicLibrary.Load"/>) on that data, its first problem on the entry it names.</item>
/// </list>
/// What the second adds to the first (its cross-entry problems) stays shown after a commit until the next pause
/// replaces it (the manager's decision Q-B, 2026-11-07). Only the first problem at each path shows (the manager's
/// decision of 2026-11-06): the kind's own, in the words the game logs. Both checks are the engine's shared mod check's
/// (<see cref="ModCheck"/>, GM1-P13: <see cref="ModCheck.Problems"/>, <see cref="ModCheck.CheckRecords"/>), the one the
/// game runs on each mod at its start, so what the editor refuses to save the game leaves out.
/// </summary>
public sealed class EntryChecks
{
    /// <summary>How long after the last change the checks across entries run (P17), in milliseconds.</summary>
    public const double Pause = 200;

    readonly EditorSession _session;
    /// <summary>Each entry's own check of its last value (gone with the entry).</summary>
    readonly ConditionalWeakTable<EditorEntry, OwnResult> _own = [];
    /// <summary>What the last run found across entries besides each entry's own problems (entries with none left out).</summary>
    Dictionary<EditorEntry, IReadOnlyList<RecordProblem>> _cross = [];
    bool _running;

    sealed record OwnResult(object Value, string Path, IReadOnlyList<RecordProblem> Problems);

    internal EntryChecks(EditorSession session)
    {
        _session = session;
        session.EntryChanged += _ => Changed();
        session.EntriesChanged += change =>
        {
            Changed();
            // Its own check reads its path too (a theme's id): checked again at once.
            if (change.Kind == EntriesChangeKind.Renamed && change.Entry is { IsRecord: true } renamed)
                session.Recheck(renamed);
        };
        session.AddCheck(Shown);
    }

    /// <summary>Whether something changed since the checks across entries last ran (<see cref="Run"/> is due).</summary>
    public bool Pending { get; private set; }

    /// <summary>The checks across entries became due (the view starts its pause, <see cref="Pause"/>, again).</summary>
    public event Action? Due;

    /// <summary>
    /// A problem of the data as edited that no record entry can show (a file or an unreadable entry the game's music
    /// loading refuses; the session's entries that can't be mounted together, <see cref="EditorSession.EditedDataProblem"/>),
    /// as of the last <see cref="Run"/>; null when there is none. Saving (step 11) refuses while it is set, as for an
    /// entry with a problem.
    /// </summary>
    public string? DataProblem { get; private set; }

    /// <summary>The checks across entries ran (each entry whose problems changed was reported changed first).</summary>
    public event Action? Ran;

    void Changed()
    {
        if (_running)
            return;
        Pending = true;
        Due?.Invoke();
    }

    /// <summary>Runs the checks across entries if something changed since they last ran (saving asks first, step 11).</summary>
    public void RunPending()
    {
        if (Pending)
            Run();
    }

    /// <summary>
    /// The checks across entries, now: every record entry that builds checked with the data as edited and the
    /// session's references, then the game's music loading; each entry whose problems change is reported changed
    /// (<see cref="EditorSession.EntryChanged"/>).
    /// </summary>
    public void Run()
    {
        Pending = false;
        _running = true;
        try
        {
            var data = _session.EditedData;
            var references = new SessionReferences(_session);
            // Every record entry, the game's too; one whose draft doesn't build has no value (it holds the music back).
            var entries = _session.Entries.Where(e => e.IsRecord).ToList();
            var records = entries.Select(e => e.Builds ? new RecordToCheck(e.Path, e.Build!.Value!, Own(e, e.Build!.Value!)) : new RecordToCheck(e.Path, null, [])).ToList();
            // The game's side (GM1 step 6b): the game's entries the session doesn't change, checked over the game's packs once.
            var game = data is not null && GameSideShortcut ? GameSide() : null;
            var changedPaths = game is null ? null : ChangedPaths();
            var result = ModCheck.CheckRecords(records, data, references,
                game is null ? null : i => Known(game, entries[i], records[i], changedPaths!),
                game is null ? null : () => game.MusicFor(data!, changedPaths!));
            var cross = new Dictionary<EditorEntry, IReadOnlyList<RecordProblem>>();
            for (var i = 0; i < entries.Count; i++)
                if (records[i].Value is not null)
                    cross[entries[i]] = result.Added[i];
            DataProblem = data is null ? _session.EditedDataProblem : result.DataProblem;
            _cross = cross;
            // Reported when what it shows changes.
            var changed = _session.Entries.Where(e => e.Builds && !e.Problems.SequenceEqual(Shown(e, e.Build!.Value!))).ToList();
            foreach (var entry in changed)
                _session.Recheck(entry);
        }
        finally
        {
            _running = false;
        }
        Ran?.Invoke();
    }

    /// <summary>
    /// Whether the checks across entries take the game's side once (GM1 step 6b, the shared mod check's <c>ModChecker</c>
    /// shortcut): each game entry the session doesn't change checked with the data over the game's packs alone, once per
    /// pack opened, its questions to the data recorded (<see cref="DataReads"/>), and checked again with the data as edited
    /// only when the session's own entries (the open pack's, the new ones, the game's it changed) hold a path it asked
    /// about or one below a folder it listed; the music's loading the same way. Each entry's problems are what the plain
    /// run gives. On by default; the tests compare it with the plain run.
    /// </summary>
    internal bool GameSideShortcut { get; set; } = true;

    /// <summary>How many game entries were checked again with the data as edited since the session's checks began (tests).</summary>
    internal int GameRechecked { get; private set; }

    GameChecks? _game;

    /// <summary>The game's side for the pack open now (made again when another pack is opened).</summary>
    GameChecks? GameSide()
    {
        if (_session.Base is not { } over)
            return null;
        if (_game?.Data != over)
        {
            // The kinds of the game's record entries, shown or under the pack's (what the session's references give where
            // the session changes nothing).
            var kinds = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var entry in _session.GameEntries())
                if (entry.IsRecord && entry.Kind is { } kind)
                    kinds[entry.Path] = kind.Name;
            _game = new GameChecks(over, new ModCheck.KindLookup(kinds));
        }
        return _game;
    }

    /// <summary>The paths where the data as edited and its references differ from the game's packs: every entry shown that isn't a game entry as the game has it.</summary>
    HashSet<string> ChangedPaths() =>
        new(_session.Entries.Where(e => e.Source != EntrySource.Game || e.IsChanged).Select(e => e.Path), StringComparer.Ordinal);

    /// <summary>What the data adds to a game entry the session doesn't change, when the session's entries can't change it; null to check it (any other entry too).</summary>
    IReadOnlyList<RecordProblem>? Known(GameChecks game, EditorEntry entry, RecordToCheck record, IReadOnlySet<string> changed)
    {
        if (entry.Source != EntrySource.Game || entry.IsChanged || record.Value is not { } value)
            return null;
        var result = game.Results.TryGetValue(entry, out var known) && ReferenceEquals(known.Value, value) && known.Path == entry.Path
            ? known
            : game.Check(entry, value, record.Own);
        if (result.Reads.Touches(changed))
        {
            GameRechecked++;
            return null;
        }
        return result.Added;
    }

    /// <summary>The game's packs (the session's base) with the game's record kinds; each game entry's check over them, and the music's loading over them, once.</summary>
    sealed class GameChecks(VirtualFileSystem data, ModCheck.KindLookup lookup)
    {
        public VirtualFileSystem Data { get; } = data;
        public ConditionalWeakTable<EditorEntry, GameResult> Results { get; } = [];
        DataReads? _musicReads;
        DataFileException? _music;

        public GameResult Check(EditorEntry entry, object value, IReadOnlyList<RecordProblem> own)
        {
            var added = DataReads.During(() => ModCheck.Without(ModCheck.Problems(entry.Path, value, Data, lookup), own), out var reads);
            var result = new GameResult(value, entry.Path, added, reads);
            Results.AddOrUpdate(entry, result);
            return result;
        }

        public DataFileException? MusicFor(VirtualFileSystem edited, IReadOnlySet<string> changed)
        {
            if (_musicReads is null)
                _music = DataReads.During(() => ModCheck.Music(Data), out _musicReads);
            return _musicReads.Touches(changed) ? ModCheck.Music(edited) : _music;
        }
    }

    /// <summary>A game entry's check over the game's packs: for the value and path it was made with, what it added and what it asked.</summary>
    sealed record GameResult(object Value, string Path, IReadOnlyList<RecordProblem> Added, DataReads Reads);

    /// <summary>The problems an entry shows (the session's check): its own, then those across entries of the last pause, the first at each path.</summary>
    IEnumerable<RecordProblem> Shown(EditorEntry entry, object value)
    {
        var own = Own(entry, value);
        return FirstPerPath(_cross.TryGetValue(entry, out var cross) ? own.Concat(cross) : own);
    }

    /// <summary>The entry's own check on its value (kept while the value and the path are the same).</summary>
    IReadOnlyList<RecordProblem> Own(EditorEntry entry, object value)
    {
        if (_own.TryGetValue(entry, out var known) && ReferenceEquals(known.Value, value) && known.Path == entry.Path)
            return known.Problems;
        // The kind's checks and the marks' alone, and the path's rules (the shared check's, ModCheck.Problems).
        var problems = ModCheck.Problems(entry.Path, value);
        _own.AddOrUpdate(entry, new(value, entry.Path, problems));
        return problems;
    }

    /// <summary>The first problem at each path, in order (the manager's decision of 2026-11-06; the shared check's, <see cref="ModCheck.FirstPerPath"/>).</summary>
    public static IReadOnlyList<RecordProblem> FirstPerPath(IEnumerable<RecordProblem> problems) => ModCheck.FirstPerPath(problems);
}

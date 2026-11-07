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
/// decision of 2026-11-06): the kind's own, in the words the game logs.
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
            var cross = new Dictionary<EditorEntry, IReadOnlyList<RecordProblem>>();
            foreach (var entry in _session.Entries)
            {
                if (!entry.IsRecord || !entry.Builds)
                    continue;
                var value = entry.Build!.Value!;
                var own = Own(entry, value);
                cross[entry] = Without(Checked(entry, value, data, references), own);
            }
            DataProblem = data is null ? _session.EditedDataProblem : Music(data, cross);
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
        var problems = Checked(entry, value, null, null);
        _own.AddOrUpdate(entry, new(value, entry.Path, problems));
        return problems;
    }

    /// <summary>
    /// The kind's checks and the marks' (<see cref="RecordChecks.Check"/>) and the path's rules. A check that fails
    /// (a kind's own check throwing: a bug, or an entry it reads that the data can't give) is the entry's problem, in
    /// its words, rather than the end of the editor's checks.
    /// </summary>
    static IReadOnlyList<RecordProblem> Checked(EditorEntry entry, object value, VirtualFileSystem? data, IReferenceLookup? references)
    {
        try
        {
            return [.. RecordChecks.Check(value, data, references), .. PathRules(entry.Path, value)];
        }
        catch (Exception problem) when (problem is not OutOfMemoryException)
        {
            return [new(RecordPath.Root, $"its check failed: {problem.Message}")];
        }
    }

    /// <summary>
    /// The rules the game's loading keeps outside the kinds' checks because only the entry's path tells them (Docs/Editor.md,
    /// "How it works", step 5): a theme in the music's folder must hold the id its path stores (<see cref="MusicLibrary.Load"/>'s words).
    /// </summary>
    internal static IEnumerable<RecordProblem> PathRules(string path, object value)
    {
        if (value is ThemeSpec theme && path.StartsWith(MusicNames.Themes + "/", StringComparison.Ordinal)
            && MusicNames.FromPathName(path[(MusicNames.Themes.Length + 1)..]) is { } name && theme.Id != name)
            yield return new(RecordPath.Root.Field(nameof(ThemeSpec.Id)), $"the theme's id is '{theme.Id}', but its path holds '{name}' (a theme's path is made from its id)");
    }

    /// <summary>
    /// The game's music loading on the data as edited, while no music entry has a problem (what its rules hold
    /// that no kind's check does: a music folder holding another kind or a name the music's rule can't store, music
    /// without a theme order): its first problem, at the root of the record entry it names, or as the data's problem
    /// when no record entry can show it.
    /// </summary>
    string? Music(VirtualFileSystem data, Dictionary<EditorEntry, IReadOnlyList<RecordProblem>> cross)
    {
        const string audio = "audio/";
        foreach (var entry in _session.Entries)
        {
            if (!entry.Path.StartsWith(audio, StringComparison.Ordinal) || !entry.IsRecord)
                continue;
            if (!entry.Builds || Own(entry, entry.Build!.Value!).Count > 0 || cross.GetValueOrDefault(entry) is { Count: > 0 })
                return null;
        }
        try
        {
            MusicLibrary.Load(data);
            return null;
        }
        catch (DataFileException problem)
        {
            if (problem.Entry is { } path && _session.Entry(path) is { IsRecord: true, Builds: true } named)
            {
                cross[named] = [.. cross.GetValueOrDefault(named) ?? [], new(RecordPath.Root, problem.Problem)];
                return null;
            }
            return problem.Message;
        }
    }

    /// <summary>The first problem at each path, in order (the manager's decision of 2026-11-06).</summary>
    public static IReadOnlyList<RecordProblem> FirstPerPath(IEnumerable<RecordProblem> problems)
    {
        var seen = new HashSet<RecordPath>();
        return [.. problems.Where(p => seen.Add(p.Path))];
    }

    /// <summary><paramref name="all"/> without the problems of <paramref name="some"/> (by path and words).</summary>
    static IReadOnlyList<RecordProblem> Without(IReadOnlyList<RecordProblem> all, IReadOnlyList<RecordProblem> some)
    {
        var known = new HashSet<RecordProblem>(some);
        return [.. all.Where(p => !known.Contains(p))];
    }
}

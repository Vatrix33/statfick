using Statventure.Editor.Checks;

namespace Statventure.Editor.Session;

/// <summary>
/// The session's checks (step 10; Docs/Editor.md, "How it works", step 10): each entry's own check at every change of
/// its draft, the checks across entries a pause after the last change (<see cref="EntryChecks"/>), and what saving reads
/// (step 11): the entries with problems, each with its first.
/// </summary>
public sealed partial class EditorSession
{
    EntryChecks? _entryChecks;

    /// <summary>
    /// The entries' checks (M11-12, P16, P17), started the first time they are asked for (the view's checks part asks
    /// when it is attached; a session without a view, when its user asks). Their problems are each entry's
    /// <see cref="EditorEntry.Problems"/>.
    /// </summary>
    public EntryChecks Checks => _entryChecks ??= new EntryChecks(this);

    /// <summary>
    /// The entries shown that have a problem (M11-12: saving refuses while there is one, naming them; P16), in path order;
    /// each one's first problem is <c>Problems[0]</c>. As of the last checks across entries: run
    /// <see cref="EntryChecks.RunPending"/> first to have them up to date (saving does). A removed entry isn't among them
    /// (saving leaves it out); a problem no entry can show is <see cref="EntryChecks.DataProblem"/>.
    /// </summary>
    public IReadOnlyList<EditorEntry> EntriesWithProblems => [.. Entries.Where(e => e.Problems.Count > 0)];
}

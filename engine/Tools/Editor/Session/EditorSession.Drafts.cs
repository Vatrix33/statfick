using Statventure.Packs;
using Statventure.Records;

namespace Statventure.Editor.Session;

/// <summary>What changed in the session's set of entries (<see cref="EditorSession.EntriesChanged"/>).</summary>
public enum EntriesChangeKind
{
    /// <summary>The whole set was replaced (<see cref="EditorSession.ShowEntries"/>); no entry given.</summary>
    Shown,
    /// <summary>An entry joined: made by New or Duplicate, or a game entry shown again when the pack's entry over it went.</summary>
    Added,
    /// <summary>An entry went (<see cref="EditorSession.Remove"/>).</summary>
    Removed,
    /// <summary>An entry's path changed (<see cref="EditorSession.Rename"/>); <see cref="EntriesChange.OldPath"/> is where it was.</summary>
    Renamed,
}

/// <summary>A change of the session's set of entries.</summary>
public sealed record EntriesChange(EntriesChangeKind Kind, EditorEntry? Entry = null, string? OldPath = null);

/// <summary>
/// The session's entries, their drafts and histories, the selection, and the changes of the set of entries (step 9;
/// Docs/Editor.md, "How it works", step 9, "The session's entries"): the API the entry list (step 8), the form
/// (step 9), checks (step 10), saving (step 11) and the preview (step 13) share. No DOM: the tests drive it as
/// the view does.
/// </summary>
public sealed partial class EditorSession
{
    readonly List<EditorEntry> _entries = [];
    readonly Dictionary<string, EditorEntry> _shadowed = new(StringComparer.Ordinal);
    readonly List<EditorEntry> _removed = [];
    readonly List<Func<EditorEntry, object, IEnumerable<RecordProblem>>> _checks = [];
    EditorEntry? _selected;
    bool _registeredUnsavedWork;
    bool _writable;

    /// <summary>The record kinds the session knows (the engine's unless the editor is given others, G3); New offers their plain kinds.</summary>
    public RecordTypes Types { get; set; } = RecordTypes.Engine;

    /// <summary>
    /// Whether the open pack takes changes: a mod, or in the dev setup one of the game's packs (M11-5); false when no
    /// pack is open or it can't be fully read (M11-16). The entries can be edited (game entries too: the mod's
    /// copy, P7), added, renamed and removed only while it is true. Set by whoever opens packs (step 8).
    /// </summary>
    public bool Writable
    {
        get => _writable;
        set
        {
            if (_writable == value)
                return;
            _writable = value;
            NotifyChanged();
        }
    }

    /// <summary>The entries shown, ordered by path (ordinal): the open pack's over the game's at the same path.</summary>
    public IReadOnlyList<EditorEntry> Entries => _entries;

    /// <summary>The open pack's entries removed since it was opened (saving leaves them out).</summary>
    public IReadOnlyList<EditorEntry> Removed => _removed;

    /// <summary>The entry shown at <paramref name="path"/>, or null.</summary>
    public EditorEntry? Entry(string path)
    {
        var index = IndexOf(path);
        return index >= 0 ? _entries[index] : null;
    }

    /// <summary>
    /// Replaces the set of entries (step 8, when a pack opens): the open pack's entries (<see cref="EntrySource.Pack"/>)
    /// and the game's (<see cref="EntrySource.Game"/>); where both have a path, the pack's is shown and the game's
    /// kept under it (shown again if the pack's is removed or renamed). The selection is cleared, the removed list
    /// emptied; <see cref="EntriesChanged"/> reports <see cref="EntriesChangeKind.Shown"/>.
    /// </summary>
    public void ShowEntries(IEnumerable<EditorEntry> entries)
    {
        var byPath = new Dictionary<string, EditorEntry>(StringComparer.Ordinal);
        _shadowed.Clear();
        foreach (var entry in entries)
        {
            if (entry.Source == EntrySource.New)
                throw new ArgumentException($"{entry.Path} is a new entry: entries shown come from packs", nameof(entries));
            if (PackPath.Problem(entry.Path) is { } problem)
                throw new ArgumentException($"'{entry.Path}' isn't a data path: {problem}", nameof(entries));
            if (!byPath.TryGetValue(entry.Path, out var there))
            {
                byPath[entry.Path] = entry;
                continue;
            }
            var (pack, game) = (there.Source, entry.Source) switch
            {
                (EntrySource.Pack, EntrySource.Game) => (there, entry),
                (EntrySource.Game, EntrySource.Pack) => (entry, there),
                _ => throw new ArgumentException($"two entries at {entry.Path} from the same place", nameof(entries)),
            };
            byPath[entry.Path] = pack;
            _shadowed[entry.Path] = game;
        }
        _entries.Clear();
        _entries.AddRange(byPath.Values.OrderBy(e => e.Path, StringComparer.Ordinal));
        _removed.Clear();
        foreach (var entry in _entries)
            entry.Refresh(_checks);
        RegisterUnsavedWork();
        _selected = null;
        EntriesChanged?.Invoke(new(EntriesChangeKind.Shown));
        SelectionChanged?.Invoke();
        NotifyChanged();
    }

    /// <summary>The game's entry under the pack's at <paramref name="path"/> (the one a mod's copy overrides), or null.</summary>
    public EditorEntry? Overridden(string path) => _shadowed.GetValueOrDefault(path);

    // ---------- Selection ----------

    /// <summary>The entry the form shows (and the list highlights), or null.</summary>
    public EditorEntry? Selected => _selected;

    /// <summary>Shows <paramref name="entry"/> (one of <see cref="Entries"/>) in the form, or nothing (null).</summary>
    public void Select(EditorEntry? entry)
    {
        if (entry is not null && !_entries.Contains(entry))
            throw new ArgumentException($"{entry.Path} isn't one of the session's entries", nameof(entry));
        if (_selected == entry)
            return;
        _selected = entry;
        SelectionChanged?.Invoke();
        NotifyChanged();
    }

    /// <summary>The selected entry changed (to another, or to none).</summary>
    public event Action? SelectionChanged;

    // ---------- Editing ----------

    /// <summary>Whether <paramref name="entry"/> can be edited now: a record, not read-only, and the open pack takes changes.</summary>
    public bool CanEdit(EditorEntry entry) => Writable && entry.IsRecord && !entry.ReadOnly;

    /// <summary>
    /// A commit (P12, P13): <paramref name="draft"/> becomes the entry's next step (its kind's draft; one step of its
    /// history). False when it is the current draft already. Refused (an exception) when the entry can't be edited.
    /// </summary>
    public bool Commit(EditorEntry entry, RecordDraft draft)
    {
        CheckEditable(entry);
        if (!entry.Commit(draft))
            return false;
        EntryWasChanged(entry);
        return true;
    }

    /// <summary>Takes the entry back one step of its history (M11-7); false when there is none.</summary>
    public bool Undo(EditorEntry entry)
    {
        CheckEditable(entry);
        if (!entry.Undo())
            return false;
        EntryWasChanged(entry);
        return true;
    }

    /// <summary>Takes the entry forward one step of its history; false when there is none.</summary>
    public bool Redo(EditorEntry entry)
    {
        CheckEditable(entry);
        if (!entry.Redo())
            return false;
        EntryWasChanged(entry);
        return true;
    }

    /// <summary>An entry's draft or its problems changed (a commit, undo, redo, or the checks run again).</summary>
    public event Action<EditorEntry>? EntryChanged;

    /// <summary>
    /// Adds a check (step 10's): given an entry and its built value, the problems to show (each at its path). Checks
    /// run whenever an entry's draft changes and it builds; <see cref="Recheck"/> runs them again (a check across
    /// entries, after a pause, P17).
    /// </summary>
    public void AddCheck(Func<EditorEntry, object, IEnumerable<RecordProblem>> check)
    {
        _checks.Add(check);
        foreach (var entry in _entries)
            entry.Refresh(_checks);
    }

    /// <summary>Runs the checks of <paramref name="entry"/> again and reports it changed.</summary>
    public void Recheck(EditorEntry entry) => EntryWasChanged(entry);

    void EntryWasChanged(EditorEntry entry)
    {
        entry.Refresh(_checks);
        EntryChanged?.Invoke(entry);
        NotifyChanged();
    }

    void CheckEditable(EditorEntry entry)
    {
        if (!_entries.Contains(entry))
            throw new ArgumentException($"{entry.Path} isn't one of the session's entries", nameof(entry));
        if (!CanEdit(entry))
            throw new InvalidOperationException($"{entry.Path} can't be edited now");
    }

    // ---------- New, duplicate, rename, remove (M11-7, P14, P15: not undoable; the view asks first) ----------

    /// <summary>
    /// The plain record kinds New offers (unions have no blank: their subtypes are offered), by display name; not a kind
    /// marked <see cref="NotAPackKindAttribute"/> (the save: the user's Q26 of GM1).
    /// </summary>
    public IReadOnlyList<RecordShape> NewKinds() =>
        [.. Types.ByName.Values.Select(RecordShape.Of).Where(s => !s.IsUnion && s.IsPackKind).OrderBy(s => s.DisplayName, StringComparer.Ordinal).ThenBy(s => s.Name, StringComparer.Ordinal)];

    /// <summary>Whether entries can be added now (New, Duplicate).</summary>
    public bool CanAdd => Writable;

    /// <summary>Whether <paramref name="entry"/> can be renamed or removed now: the open pack's own or a new one (a game entry can't be, P7: mods can't delete).</summary>
    public bool CanRenameOrRemove(EditorEntry entry) => Writable && _entries.Contains(entry) && entry.Source != EntrySource.Game;

    /// <summary>
    /// Why <paramref name="path"/> can't be given to an entry (null when it can): the path rule, another entry
    /// there, or an entry that is a folder of it or below it (a pack can't hold both). <paramref name="except"/>
    /// is the entry being renamed.
    /// </summary>
    public string? PathProblem(string path, EditorEntry? except = null)
    {
        if (PackPath.Problem(path) is { } problem)
            return problem;
        foreach (var entry in _entries)
        {
            if (entry == except)
                continue;
            if (entry.Path == path)
                return $"there is already an entry at {path}";
            if (entry.Path.StartsWith(path + "/", StringComparison.Ordinal))
                return $"{path} is the folder of {entry.Path}";
            if (path.StartsWith(entry.Path + "/", StringComparison.Ordinal))
                return $"{entry.Path} is an entry, so it can't be a folder";
        }
        return null;
    }

    /// <summary>A new entry at <paramref name="path"/> holding <paramref name="draft"/> (New: a kind's blank draft; Duplicate: a copy's).</summary>
    public EditorEntry Add(string path, RecordDraft draft)
    {
        if (!CanAdd)
            throw new InvalidOperationException("no pack that takes new entries is open");
        if (PathProblem(path) is { } problem)
            throw new ArgumentException(problem, nameof(path));
        var entry = EditorEntry.ForNew(path, draft);
        entry.Refresh(_checks);
        Insert(entry);
        RegisterUnsavedWork();
        EntriesChanged?.Invoke(new(EntriesChangeKind.Added, entry));
        NotifyChanged();
        return entry;
    }

    /// <summary>A copy of <paramref name="entry"/>'s current draft as a new entry at <paramref name="path"/> (P15).</summary>
    public EditorEntry Duplicate(EditorEntry entry, string path) =>
        entry.Draft is { } draft ? Add(path, draft) : throw new InvalidOperationException($"{entry.Path} isn't a record");

    /// <summary>
    /// Moves <paramref name="entry"/> to <paramref name="path"/> (P14: not undoable; its history goes with it). A game
    /// entry the pack's entry was over shows again at the old path.
    /// </summary>
    public void Rename(EditorEntry entry, string path)
    {
        if (!CanRenameOrRemove(entry))
            throw new InvalidOperationException($"{entry.Path} can't be renamed now");
        if (path == entry.Path)
            return;
        if (PathProblem(path, entry) is { } problem)
            throw new ArgumentException(problem, nameof(path));
        var old = entry.Path;
        _entries.Remove(entry);
        entry.Path = path;
        Insert(entry);
        EntriesChanged?.Invoke(new(EntriesChangeKind.Renamed, entry, old));
        Reveal(old);
        NotifyChanged();
    }

    /// <summary>
    /// Takes <paramref name="entry"/> out (M11-7: not undoable). An entry of the open pack is listed in
    /// <see cref="Removed"/> for saving; a game entry it was over shows again. The selection goes if it was the entry.
    /// </summary>
    public void Remove(EditorEntry entry)
    {
        if (!CanRenameOrRemove(entry))
            throw new InvalidOperationException($"{entry.Path} can't be removed now");
        _entries.Remove(entry);
        if (entry.Source == EntrySource.Pack)
            _removed.Add(entry);
        if (_selected == entry)
        {
            _selected = null;
            SelectionChanged?.Invoke();
        }
        EntriesChanged?.Invoke(new(EntriesChangeKind.Removed, entry, entry.Path));
        Reveal(entry.Path);
        NotifyChanged();
    }

    /// <summary>
    /// Whether <paramref name="entry"/> is the open pack's own copy of a game entry (P7): an entry of the pack over a
    /// game entry at its path, or a game entry changed in this session (its first change made the copy).
    /// </summary>
    public bool IsModCopy(EditorEntry entry) =>
        _entries.Contains(entry) && (entry.Source == EntrySource.Pack ? _shadowed.ContainsKey(entry.Path) : entry.Source == EntrySource.Game && entry.IsChanged);

    /// <summary>Whether <paramref name="entry"/> can be reverted to the game's now (it is a copy, and the pack takes changes).</summary>
    public bool CanRevert(EditorEntry entry) => Writable && IsModCopy(entry);

    /// <summary>
    /// "Revert to the game's" (P7; the manager's decision of 2026-11-06): drops the pack's copy and shows the game's
    /// entry again with its history cleared; not undoable (the view asks first, as for a removal, M11-7). A copy the
    /// pack holds goes to <see cref="Removed"/> (the game's entry is added back, both reported); a game entry
    /// changed in this session is put back to the game's draft (reported as changed). The selection follows to the
    /// game's entry. Gives the entry shown at the path afterwards.
    /// </summary>
    public EditorEntry Revert(EditorEntry entry)
    {
        if (!CanRevert(entry))
            throw new InvalidOperationException($"{entry.Path} isn't a copy of a game entry that can be reverted now");
        if (entry.Source == EntrySource.Game)
        {
            entry.Reset();
            EntryWasChanged(entry);
            return entry;
        }
        var game = _shadowed[entry.Path];
        var selected = _selected == entry;
        _entries.Remove(entry);
        _removed.Add(entry);
        EntriesChanged?.Invoke(new(EntriesChangeKind.Removed, entry, entry.Path));
        // (The game's entry was under the copy, never shown, so it has no history to clear.)
        Reveal(entry.Path);
        if (selected)
        {
            _selected = game;
            SelectionChanged?.Invoke();
        }
        NotifyChanged();
        return game;
    }

    /// <summary>The set of entries changed (New, Duplicate, Rename, Remove, Revert, or a new set shown).</summary>
    public event Action<EntriesChange>? EntriesChanged;

    /// <summary>Whether the entries hold unsaved work: one changed, renamed or added, or one of the pack's removed.</summary>
    public bool HasUnsavedEntries => _removed.Count > 0 || _entries.Exists(e => e.IsChanged || e.IsRenamed);

    void RegisterUnsavedWork()
    {
        if (_registeredUnsavedWork)
            return;
        _registeredUnsavedWork = true;
        AddUnsavedWork(() => HasUnsavedEntries);
    }

    void Reveal(string path)
    {
        if (!_shadowed.Remove(path, out var game) || IndexOf(path) >= 0)
            return;
        game.Refresh(_checks);
        Insert(game);
        EntriesChanged?.Invoke(new(EntriesChangeKind.Added, game));
    }

    void Insert(EditorEntry entry)
    {
        var index = ~IndexOf(entry.Path);
        _entries.Insert(index, entry);
    }

    int IndexOf(string path)
    {
        int low = 0, high = _entries.Count - 1;
        while (low <= high)
        {
            var middle = (low + high) / 2;
            var order = string.CompareOrdinal(_entries[middle].Path, path);
            if (order == 0)
                return middle;
            if (order < 0)
                low = middle + 1;
            else
                high = middle - 1;
        }
        return ~low;
    }
}

using Statventure.Records;

namespace Statventure.Editor.Session;

/// <summary>Where an entry's saved state comes from (Docs/Editor.md, "The session").</summary>
public enum EntrySource
{
    /// <summary>An entry of the open pack.</summary>
    Pack,
    /// <summary>An entry of the game's packs the open pack is seen over; editing it in a mod makes the mod's own copy (P7).</summary>
    Game,
    /// <summary>Made in this session (New, Duplicate), not saved yet.</summary>
    New,
}

/// <summary>What an entry holds.</summary>
public enum EntryContent
{
    /// <summary>A record of a kind the session knows: it has a draft and a history.</summary>
    Record,
    /// <summary>A file that isn't a record (a font, an image, a text): shown, not edited.</summary>
    File,
    /// <summary>An entry that couldn't be read (<see cref="EditorEntry.Reason"/>): listed, not edited (M11-16).</summary>
    Unreadable,
}

/// <summary>
/// A working entry of the session (Docs/Editor.md, "How it works", step 9): its path, where it came from, what it
/// holds, and for a record its draft (P10) with the entry's own history of drafts (M11-7: undo and redo per
/// entry) and the problems of the draft shown. Made by whoever lists entries (step 8 when a pack opens:
/// <see cref="ForRecord"/>, <see cref="ForFile"/>, <see cref="ForUnreadable"/>) or by the session (New and
/// Duplicate); changed only through the session, which reports every change.
/// </summary>
public sealed partial class EditorEntry
{
    readonly List<RecordDraft> _history = [];
    int _position;
    RecordDraft? _built;
    DraftBuild? _build;
    IReadOnlyList<RecordProblem> _problems = [];
    object? _lastBuilt;
    RecordDraft? _sameAsSavedFor;
    bool _sameAsSaved;
    byte[]? _savedBinary;

    EditorEntry(string path, EntrySource source, EntryContent content)
    {
        Path = path;
        SavedPath = source == EntrySource.New ? null : path;
        Source = source;
        Content = content;
    }

    /// <summary>
    /// A record entry as read from its pack (<paramref name="record"/>: a value of a current record type, as
    /// <c>RecordSerializer.FromBinary</c> gives it, of any version). <paramref name="readOnly"/>: the form shows
    /// it without letting it change (a game entry that can't be copied, P7).
    /// </summary>
    public static EditorEntry ForRecord(string path, object record, EntrySource source, bool readOnly = false)
    {
        ArgumentNullException.ThrowIfNull(record);
        if (source == EntrySource.New)
            throw new ArgumentException("a new entry is made by the session (EditorSession.Add)", nameof(source));
        var entry = new EditorEntry(path, source, EntryContent.Record) { Saved = record, ReadOnly = readOnly };
        var draft = RecordDraft.From(record);
        entry.Kind = draft.Shape;
        entry.SavedDraft = draft;
        entry.Start(draft);
        return entry;
    }

    /// <summary>A file entry (not a record) of <paramref name="size"/> bytes.</summary>
    public static EditorEntry ForFile(string path, long size, EntrySource source) =>
        source == EntrySource.New ? throw new ArgumentException("files come from packs", nameof(source))
            : new EditorEntry(path, source, EntryContent.File) { FileSize = size, ReadOnly = true };

    /// <summary>An entry that couldn't be read, with the reason (M11-16: listed with it, never edited).</summary>
    public static EditorEntry ForUnreadable(string path, string reason, EntrySource source) =>
        source == EntrySource.New ? throw new ArgumentException("unreadable entries come from packs", nameof(source))
            : new EditorEntry(path, source, EntryContent.Unreadable) { Reason = reason, ReadOnly = true };

    /// <summary>A new record entry holding <paramref name="draft"/> (the session's New and Duplicate).</summary>
    internal static EditorEntry ForNew(string path, RecordDraft draft)
    {
        var entry = new EditorEntry(path, EntrySource.New, EntryContent.Record) { Kind = draft.Shape, SavedDraft = draft };
        entry.Start(draft);
        return entry;
    }

    void Start(RecordDraft draft)
    {
        _history.Add(draft);
        _position = 0;
        Refresh();
    }

    /// <summary>The entry's path in the data (changed by a rename).</summary>
    public string Path { get; internal set; }

    /// <summary>The path it has in its pack as saved; null for a new entry (a rename leaves it, so saving knows what to replace).</summary>
    public string? SavedPath { get; internal set; }

    public EntrySource Source { get; internal set; }

    public EntryContent Content { get; }

    public bool IsRecord => Content == EntryContent.Record;

    /// <summary>The record kind (the draft's shape: a plain record or a union's subtype); null for files and unreadable entries.</summary>
    public RecordShape? Kind { get; private set; }

    /// <summary>The name of the kind as tools show it, "File" for files and "Unreadable" for unreadable entries.</summary>
    public string KindName => Content switch
    {
        EntryContent.Record => Kind!.DisplayName,
        EntryContent.File => "File",
        _ => "Unreadable",
    };

    /// <summary>The entry's name in its kind's folder (P9), else its last path segment.</summary>
    public string Name => Kind?.NameOf(Path) ?? Path[(Path.LastIndexOf('/') + 1)..];

    /// <summary>The record as saved (read from its pack); null for new entries, files and unreadable ones.</summary>
    public object? Saved { get; private set; }

    /// <summary>The draft the history started from (the saved record's, or a new entry's first).</summary>
    public RecordDraft? SavedDraft { get; private set; }

    /// <summary>The draft shown and edited: the history's current step; null for files and unreadable entries.</summary>
    public RecordDraft? Draft => IsRecord ? _history[_position] : null;

    /// <summary>The current draft built (P10): the record, or the problems that keep it from being built.</summary>
    public DraftBuild? Build => _build;

    /// <summary>The last value that built of the drafts this entry has shown (the JSON view and the preview's last valid state).</summary>
    public object? LastBuilt => _lastBuilt;

    /// <summary>Whether the current draft builds into <see cref="LastBuilt"/> (false while it can't be built).</summary>
    public bool Builds => _build is { Built: true };

    /// <summary>
    /// The problems shown for the entry: the draft's build problems while it doesn't build, else the problems the
    /// session's checks give (step 10's; none until then), each at its path.
    /// </summary>
    public IReadOnlyList<RecordProblem> Problems => _problems;

    /// <summary>The form shows the entry without letting it change (files and unreadable entries always).</summary>
    public bool ReadOnly { get; set; }

    /// <summary>A file's size in bytes.</summary>
    public long FileSize { get; private set; }

    /// <summary>Why an unreadable entry couldn't be read.</summary>
    public string? Reason { get; private set; }

    public bool CanUndo => _position > 0;

    public bool CanRedo => _position < _history.Count - 1;

    /// <summary>The steps of the history (the first is <see cref="SavedDraft"/>) and where the entry is in it.</summary>
    public IReadOnlyList<RecordDraft> History => _history;

    public int HistoryPosition => _position;

    /// <summary>Renamed since it was opened (its path differs from <see cref="SavedPath"/>).</summary>
    public bool IsRenamed => SavedPath is not null && SavedPath != Path;

    /// <summary>
    /// Changed from what was saved: a new entry always; a record whose draft isn't the saved one and doesn't build
    /// into the same binary form as the saved record (an edit taken back by another isn't a change). For a game
    /// entry in a mod, being changed makes it the mod's own copy (P7).
    /// </summary>
    public bool IsChanged
    {
        get
        {
            if (Source == EntrySource.New)
                return true;
            if (!IsRecord)
                return false;
            var draft = Draft!;
            if (draft == SavedDraft)
                return false;
            if (_sameAsSavedFor != draft)
            {
                _sameAsSavedFor = draft;
                try
                {
                    _sameAsSaved = _build is { Built: true } build && Saved is not null
                        && RecordSerializer.ToBinary(Kind!.Type, build.Value!).AsSpan().SequenceEqual(_savedBinary ??= RecordSerializer.ToBinary(Kind!.Type, Saved));
                }
                catch (RecordFormatException)
                {
                    // A saved record the serializer can't write (made in code, not read from a pack) differs from any that it can.
                    _sameAsSaved = false;
                }
            }
            return !_sameAsSaved;
        }
    }

    /// <summary>
    /// Makes <paramref name="draft"/> the next step of the history (the steps after the current one go); false when
    /// it is the current draft already. The session refreshes the problems after it.
    /// </summary>
    internal bool Commit(RecordDraft draft)
    {
        if (!IsRecord)
            throw new InvalidOperationException($"{Path} isn't a record");
        if (draft.Shape != Kind)
            throw new ArgumentException($"a draft of {draft.Shape.Name} for {Path}, which holds {Kind!.Name}", nameof(draft));
        if (draft == Draft)
            return false;
        _history.RemoveRange(_position + 1, _history.Count - _position - 1);
        _history.Add(draft);
        _position++;
        return true;
    }

    /// <summary>Back to the saved draft with no history (a revert to the game's).</summary>
    internal void Reset()
    {
        if (!IsRecord)
            return;
        _history.Clear();
        _history.Add(SavedDraft!);
        _position = 0;
    }

    internal bool Undo()
    {
        if (!CanUndo)
            return false;
        _position--;
        return true;
    }

    internal bool Redo()
    {
        if (!CanRedo)
            return false;
        _position++;
        return true;
    }

    /// <summary>Builds the current draft and works out its problems (after every change of the draft, and when the session's checks ask again).</summary>
    internal void Refresh(IReadOnlyList<Func<EditorEntry, object, IEnumerable<RecordProblem>>>? checks = null)
    {
        if (!IsRecord)
            return;
        var draft = Draft!;
        if (_built != draft)
        {
            _built = draft;
            _build = draft.Build();
            if (_build.Built)
                _lastBuilt = _build.Value;
        }
        if (!_build!.Built)
            _problems = _build.Problems;
        else if (checks is { Count: > 0 })
            _problems = [.. checks.SelectMany(check => check(this, _build.Value!))];
        else
            _problems = [];
    }

    public override string ToString() => Path;
}

using Statventure.Editor.Session;
using Statventure.Records;
using Statventure.UI.Dom;

namespace Statventure.Editor.Form;

/// <summary>
/// The form of one record entry (Docs/Editor.md, "The form (step 9)"): its rows, made from its kind's shape and
/// brought up to date in place after every change of its draft (a commit, undo, redo), so focus, open groups and a
/// field's text that doesn't read survive; the problems under their rows (P16); and each text field's typing state
/// for the shell's Undo and Redo (P13: a field's own typing is undone first).
/// </summary>
sealed class FormView
{
    readonly EditorSession _session;
    readonly RecordEditor _root;
    readonly UiElement _top;
    readonly List<(UiElement Row, Slot Slot)> _rows = [];
    readonly Dictionary<UiElement, TypingState> _fields = [];
    readonly Dictionary<UiElement, string> _parseProblems = [];

    sealed class TypingState
    {
        public string Committed = "";
        public int OwnUndos;
    }

    public FormView(EditorSession session, EditorEntry entry, UiElement top, UiElement rows)
    {
        _session = session;
        Entry = entry;
        ReadOnly = !session.CanEdit(entry);
        Document = rows.Document!;
        _top = top;
        _root = new RecordEditor(this, Slot.Root, entry.Kind!, null);
        rows.Append(_root.Body!);
        Update();
    }

    public EditorEntry Entry { get; }

    /// <summary>The entry can't be changed: every control disabled, no add, remove or move.</summary>
    public bool ReadOnly { get; }

    public UiDocument Document { get; }

    /// <summary>Brings every row up to date with the entry's draft and shows its problems.</summary>
    public void Update()
    {
        _root.Update(Entry.Draft!);
        ShowProblems();
    }

    /// <summary>A change by the user: the value at <paramref name="slot"/> becomes <paramref name="value"/> (one step of the entry's history).</summary>
    public void Commit(Slot slot, DraftValue value) =>
        _session.Commit(Entry, slot.Set(Entry.Draft!, value));

    /// <summary>A row of <paramref name="slot"/>'s value (problems at its path show under it).</summary>
    public void Row(UiElement row, Slot slot) => _rows.Add((row, slot));

    // ---------- Typing (P12, P13) ----------

    /// <summary>A field whose typing is kept apart from commits: its own undo runs before the entry's history.</summary>
    public void TextField(UiElement input)
    {
        var state = new TypingState();
        _fields[input] = state;
        input.AddEventListener<UiEvent>("input", e =>
        {
            if (e is UiInputEvent typed)
                state.OwnUndos = typed.InputType switch
                {
                    "historyUndo" => state.OwnUndos + 1,
                    "historyRedo" => Math.Max(0, state.OwnUndos - 1),
                    _ => 0,
                };
        });
    }

    /// <summary>The field now shows the draft's value as <paramref name="text"/>: nothing typed, nothing to redo, no mark.</summary>
    public void Committed(UiElement input, string text)
    {
        if (_fields.TryGetValue(input, out var state))
        {
            state.Committed = text;
            state.OwnUndos = 0;
        }
        ParseProblem(input, null);
    }

    /// <summary>Whether <paramref name="element"/> is one of the form's fields with typing not committed (its own undo goes first, P13).</summary>
    public bool HasTypingToUndo(UiElement? element) =>
        element is not null && _fields.TryGetValue(element, out var state) && element.EditorText != state.Committed;

    /// <summary>Whether <paramref name="element"/> is one of the form's fields with typing to redo (its own undos since it was last typed in, or typing not committed).</summary>
    public bool HasTypingToRedo(UiElement? element) =>
        element is not null && _fields.TryGetValue(element, out var state) && (state.OwnUndos > 0 || element.EditorText != state.Committed);

    /// <summary>Marks a field whose text doesn't read (P12) with why under its row, or clears the mark (null).</summary>
    public void ParseProblem(UiElement input, string? message)
    {
        var had = _parseProblems.TryGetValue(input, out var before);
        if (message is null ? !had : had && before == message)
            return;
        if (message is null)
            _parseProblems.Remove(input);
        else
            _parseProblems[input] = message;
        input.ToggleClass("bad", message is not null);
        ShowProblems();
    }

    /// <summary>The fields marked as not reading, with why.</summary>
    public IReadOnlyDictionary<UiElement, string> ParseProblems => _parseProblems;

    // ---------- Problems (P16) ----------

    /// <summary>
    /// Shows the entry's problems: each under the row of its path (a path without a row of its own, under its nearest
    /// row above), those of the whole entry at the form's top; and under a field that doesn't read, why.
    /// </summary>
    public void ShowProblems()
    {
        foreach (var old in _top.Document is null ? [] : _top.Parent!.Descendants().Where(e => e.HasClass("problem")).ToList())
            old.Remove();
        _rows.RemoveAll(r => !r.Row.IsDescendantOf(_top.Parent!));
        var draft = Entry.Draft!;
        var byPath = new Dictionary<string, UiElement>(StringComparer.Ordinal);
        foreach (var (row, slot) in _rows)
        {
            var path = slot.Path(draft).ToString();
            row.SetAttribute("data-path", path);
            byPath.TryAdd(path, row);
        }
        foreach (var problem in Entry.Problems)
            Show(RowOf(problem.Path, byPath), problem.Message);
        foreach (var (input, message) in _parseProblems)
            if (input.IsDescendantOf(_top.Parent!))
                Show(input.Ancestors().FirstOrDefault(a => a.HasAttribute("data-path")), message);
        foreach (var row in _rows.Select(r => r.Row))
            row.ToggleClass("has-problem", row.ElementChildren.Any(c => c.HasClass("problem")));
    }

    UiElement? RowOf(RecordPath path, Dictionary<string, UiElement> byPath)
    {
        for (var segments = path.Segments.Count; segments > 0; segments--)
        {
            var at = RecordPath.Root;
            foreach (var segment in path.Segments.Take(segments))
                at = segment.Kind switch
                {
                    RecordPathKind.Field => at.Field(segment.Name!),
                    RecordPathKind.Index => at.Index(segment.Index),
                    _ => at.Key(segment.Name!),
                };
            if (byPath.TryGetValue(at.ToString(), out var row))
                return row;
        }
        return null;
    }

    void Show(UiElement? row, string message)
    {
        if (row is null)
        {
            _top.Append("div", "problem", message);
            return;
        }
        var line = new UiElement("div") { ClassName = "problem" };
        line.TextContent = message;
        // Under the row's own line (a group's summary), before a group's nested rows.
        row.InsertBefore(line, row.ElementChildren.ElementAtOrDefault(1));
    }
}

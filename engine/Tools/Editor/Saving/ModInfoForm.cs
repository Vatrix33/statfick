using Statventure.Editor.Form;
using Statventure.Editor.Session;
using Statventure.Editor.Shell;
using Statventure.Packs;
using Statventure.Records;
using Statventure.UI.Dom;

namespace Statventure.Editor.Saving;

/// <summary>
/// The mod's info in a form of its own (M11-15, G1; the manager's decision Q1 of 2026-11-08): the entry <c>mod/info</c>
/// (<see cref="ModInfo"/>) selected shows in the form pane through step 9's hook (<see cref="IEntryForm"/>) instead of the
/// generic rows: a head ("Mod info", its path, "read-only" when it can't change), what it is for, then Name, Author and
/// Version as text fields and Description as a textarea. Each field commits on its change event (Enter, leaving it) as
/// the generic form's do: one step of the entry's history, undone and redone per entry (M11-7), its own typing first
/// (P13); the list marks it and saving writes it as any entry.
/// </summary>
public sealed class ModInfoForm : EditorPart, IEntryForm
{
    /// <summary>The fields in their order, by their code names, with their labels.</summary>
    public static readonly IReadOnlyList<(string Field, string Label)> Fields =
        [(nameof(ModInfo.Name), "Name"), (nameof(ModInfo.Author), "Author"), (nameof(ModInfo.Version), "Version"), (nameof(ModInfo.Description), "Description")];

    sealed class FieldState(string field, UiElement input)
    {
        public readonly string Field = field;
        public readonly UiElement Input = input;
        /// <summary>The text the draft holds, as last shown.</summary>
        public string? Committed;
        /// <summary>The field's own undos since it was last typed in.</summary>
        public int OwnUndos;
    }

    readonly List<FieldState> _fields = [];
    EditorEntry? _entry;

    protected internal override void Attach(UiElement? element)
    {
    }

    /// <summary>The mod's info: the record at <see cref="ModInfo.Path"/> of the engine's kind <c>mod.info</c>.</summary>
    public bool Shows(EditorEntry entry) => entry.Path == ModInfo.Path && entry.Kind?.Type == typeof(ModInfo);

    public void Show(EditorEntry entry, UiElement element)
    {
        _entry = entry;
        _fields.Clear();
        var form = element.Append("div", "mod-info");
        var head = form.Append("div", "mod-info-head");
        head.Append("span", "mod-info-title", "Mod info");
        head.Append("span", "mod-info-path", entry.Path);
        var readOnly = !Session.CanEdit(entry);
        if (readOnly)
            head.Append("span", "mod-info-state", "read-only");
        form.Append("p", "mod-info-about", "What players will see of this mod in a mod list (the game doesn't show it yet). It is saved with the mod.");
        foreach (var (field, label) in Fields)
        {
            var row = form.Append("label", "mod-info-row");
            row.Append("span", "mod-info-label", label);
            var input = field == nameof(ModInfo.Description) ? row.Append("textarea", "mod-info-field") : row.Append("input", "mod-info-field");
            if (input.Tag == "input")
                input.SetAttribute("type", "text");
            input.ToggleAttribute("disabled", readOnly);
            var state = new FieldState(field, input);
            _fields.Add(state);
            input.AddEventListener("change", _ => Commit(state));
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
        Update();
    }

    /// <summary>The field of <paramref name="field"/> (a code name of <see cref="Fields"/>) while the form shows.</summary>
    public UiElement Field(string field) => _fields.Single(f => f.Field == field).Input;

    /// <summary>The entry shown (the last one <see cref="Show"/> was given).</summary>
    public EditorEntry? Entry => _entry;

    /// <summary>A field's text committed: the draft with it, one step of the entry's history.</summary>
    void Commit(FieldState state)
    {
        // (Its change event comes only when its text differs from the draft's: the text it had when typing began.)
        var entry = _entry!;
        var text = state.Input.EditorText;
        Session.Commit(entry, entry.Draft!.With(state.Field, DraftValue.Of(text)));
    }

    /// <summary>Each field shows the draft's text, put back only where the draft's value changed (typing elsewhere survives).</summary>
    public void Update()
    {
        if (_entry?.Draft is not { } draft)
            return;
        foreach (var state in _fields)
        {
            var text = TextOf(draft, state.Field);
            if (text == state.Committed)
                continue;
            state.Committed = text;
            state.Input.Value = text;
            state.OwnUndos = 0;
        }
    }

    /// <summary>A field's text in a draft (a value not given is its default, empty text).</summary>
    static string TextOf(RecordDraft draft, string field) => draft[field] is DraftLeaf { Value: string text } ? text : "";

    public bool HasTypingToUndo(UiElement? element) =>
        _fields.Find(f => f.Input == element) is { } state && element!.EditorText != state.Committed;

    public bool HasTypingToRedo(UiElement? element) =>
        _fields.Find(f => f.Input == element) is { } state && (state.OwnUndos > 0 || element!.EditorText != state.Committed);
}

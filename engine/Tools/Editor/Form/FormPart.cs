using System.Globalization;
using Statventure.Editor.Session;
using Statventure.Editor.Shell;
using Statventure.Records;
using Statventure.UI.Dom;

namespace Statventure.Editor.Form;

/// <summary>The form pane's two tabs (M11-18, P22).</summary>
public enum FormTab
{
    Form,
    Json,
}

/// <summary>
/// The form pane (Docs/Editor.md, "The form (step 9)"): the selected entry's path, kind and actions (Duplicate,
/// Rename, Remove), a Form tab with a row per field (P11) and a JSON tab with the entry's JSON form, copy and export
/// (M11-18, P22). Nothing shows while no entry is selected. It handles Undo and Redo for the selected entry (M11-7),
/// a form field's own typing first (P13).
/// </summary>
public sealed class FormPart : EditorPart
{
    UiElement _element = null!;
    UiElement? _view;
    UiElement? _head;
    UiElement? _path;
    readonly Dictionary<string, UiElement> _actions = [];
    UiElement? _panel;
    readonly List<UiElement> _tabs = [];
    FormView? _form;
    UiElement? _json;
    EditorEntry? _shown;
    FormTab _tab = FormTab.Form;

    public override EditorPane? Pane => EditorPane.Form;

    protected internal override void Attach(UiElement? element)
    {
        _element = element!;
        _element.AddClass("form-part");
        Session.SelectionChanged += Show;
        Session.EntryChanged += entry =>
        {
            if (entry == _shown)
                Refresh();
        };
        Session.EntriesChanged += change =>
        {
            if (change.Entry is { } entry && entry == _shown)
                HeadState();
        };
        View.Commands.Handle(EditorCommand.Undo, request => UndoOrRedo(request, redo: false), () => Session.Selected is { CanUndo: true } entry && Session.CanEdit(entry));
        View.Commands.Handle(EditorCommand.Redo, request => UndoOrRedo(request, redo: true), () => Session.Selected is { CanRedo: true } entry && Session.CanEdit(entry));
        Show();
    }

    /// <summary>The entry shown (the session's selection when it was last shown), or null.</summary>
    public EditorEntry? Shown => _shown;

    /// <summary>The tab shown (kept when another entry is selected).</summary>
    public FormTab Tab
    {
        get => _tab;
        set
        {
            if (_tab == value)
                return;
            _tab = value;
            Tabs();
        }
    }

    /// <summary>
    /// Undo or Redo for the selected entry (M11-7), unless the key went to one of the form's fields with typing of
    /// its own to undo or redo (P13): then the field takes it (the handler says it didn't act).
    /// </summary>
    bool UndoOrRedo(CommandRequest request, bool redo)
    {
        if (Session.Selected is not { } entry)
            return false;
        if (request.Key is not null && _form is { } form && (redo ? form.HasTypingToRedo(request.Target) : form.HasTypingToUndo(request.Target)))
            return false;
        return redo ? Session.Redo(entry) : Session.Undo(entry);
    }

    /// <summary>Shows the session's selected entry (rebuilt from its shape) or nothing.</summary>
    void Show()
    {
        var entry = Session.Selected;
        _shown = entry;
        _element.ClearChildren();
        _form = null;
        _json = null;
        _view = null;
        _head = null;
        _path = null;
        _panel = null;
        _tabs.Clear();
        if (entry is null)
            return;
        _view = _element.Append("div", "form-view");
        _head = _view.Append("div", "form-head");
        Head();
        if (entry.IsRecord)
        {
            var tabs = _view.Append("div", "form-tabs");
            tabs.SetAttribute("role", "tablist");
            foreach (var tab in Enum.GetValues<FormTab>())
            {
                var button = tabs.Append("button", "tab", tab == FormTab.Form ? "Form" : "JSON");
                _tabs.Add(button);
                button.SetAttribute("type", "button");
                button.SetAttribute("role", "tab");
                button.SetAttribute("data-tab", tab.ToString().ToLowerInvariant());
                button.AddEventListener("click", _ => Tab = tab);
                button.AddEventListener<UiKeyboardEvent>("keydown", e =>
                {
                    if (e.Key is "ArrowLeft" or "ArrowRight" && !e.CtrlKey && !e.AltKey && !e.MetaKey && !e.ShiftKey)
                    {
                        Tab = Tab == FormTab.Form ? FormTab.Json : FormTab.Form;
                        _tabs[(int)Tab].Focus();
                        e.PreventDefault();
                    }
                });
            }
            _panel = _view.Append("div", "form-panel");
            _panel.SetAttribute("role", "tabpanel");
            var top = _panel.Append("div", "form-problems");
            var rows = _panel.Append("div", "form-rows");
            _form = new FormView(Session, entry, top, rows);
            _json = _view.Append("div", "json-view");
            _json.SetAttribute("role", "tabpanel");
            Tabs();
        }
        else
        {
            var about = _view.Append("div", "form-about");
            about.TextContent = entry.Content == EntryContent.File
                ? $"A file of {entry.FileSize.ToString("N0", CultureInfo.InvariantCulture)} bytes. It changes only by importing it again."
                : $"This entry can't be read: {entry.Reason}";
        }
        View.Pane(EditorPane.Form).ScrollTop = 0;
    }

    /// <summary>The entry's path, kind and actions.</summary>
    void Head()
    {
        if (_head is not { } head || _shown is not { } entry)
            return;
        head.ClearChildren();
        var title = head.Append("div", "form-title");
        _path = title.Append("span", "form-path", entry.Path);
        title.Append("span", "form-kind", entry.KindName);
        if (!Session.CanEdit(entry) && entry.IsRecord)
            title.Append("span", "form-state", "read-only");
        var actions = View.Part<EntryActions>();
        var buttons = head.Append("div", "form-actions");
        _actions.Clear();
        void Action(string text, string name, Func<Task> run)
        {
            var button = buttons.Append("button", null, text);
            button.SetAttribute("type", "button");
            button.SetAttribute("data-action", name);
            button.AddEventListener("click", e => _ = run());
            _actions[name] = button;
        }
        Action("Revert to the game's", "revert", () => actions.Revert(entry));
        Action("Duplicate", "duplicate", () => actions.Duplicate(entry));
        Action("Rename", "rename", () => actions.Rename(entry));
        Action("Remove", "remove", () => actions.Remove(entry));
        HeadState();
    }

    /// <summary>
    /// The head's path and what its actions can do now: Revert only for the pack's own copy of a game entry (P7),
    /// Duplicate for records while entries can be added, Rename and Remove for the pack's own and new entries.
    /// </summary>
    void HeadState()
    {
        if (_shown is not { } entry || _path is null)
            return;
        _path.TextContent = entry.Path;
        _actions["revert"].ToggleAttribute("hidden", !Session.IsModCopy(entry));
        _actions["revert"].ToggleAttribute("disabled", !Session.CanRevert(entry));
        _actions["duplicate"].ToggleAttribute("disabled", !(entry.IsRecord && Session.CanAdd));
        _actions["rename"].ToggleAttribute("disabled", !Session.CanRenameOrRemove(entry));
        _actions["remove"].ToggleAttribute("disabled", !Session.CanRenameOrRemove(entry));
    }

    void Tabs()
    {
        if (_view is null || _form is null)
            return;
        foreach (var button in _tabs)
        {
            var selected = button.GetAttribute("data-tab") == _tab.ToString().ToLowerInvariant();
            button.SetAttribute("aria-selected", selected ? "true" : "false");
            button.SetAttribute("tabindex", selected ? "0" : "-1");
            button.ToggleClass("selected", selected);
        }
        _panel!.ToggleAttribute("hidden", _tab != FormTab.Form);
        _json!.ToggleAttribute("hidden", _tab != FormTab.Json);
        if (_tab == FormTab.Json)
            Json();
    }

    /// <summary>The entry changed (a commit, undo, redo, a new check): its rows, problems and JSON.</summary>
    void Refresh()
    {
        HeadState();
        _form?.Update();
        if (_tab == FormTab.Json)
            Json();
    }

    // ---------- The JSON tab (M11-18, P22) ----------

    /// <summary>The most lines the JSON tab shows (copy and export give all of it).</summary>
    public const int JsonLines = 2000;

    /// <summary>The entry's JSON form as the JSON tab shows it, copies and exports it: of its draft when it builds, else of the last draft that did; null when none has.</summary>
    public string? JsonOf(EditorEntry entry) =>
        entry.LastBuilt is { } value ? RecordSerializer.ToJson(entry.Kind!.Type, value) : null;

    void Json()
    {
        if (_json is null || _shown is not { } entry)
            return;
        _json.ClearChildren();
        var bar = _json.Append("div", "json-bar");
        var text = JsonOf(entry);
        bar.Append("span", "json-note", text is null ? "The entry doesn't build yet, so it has no JSON form."
            : entry.Builds ? "The entry's JSON form, read-only. Import a JSON file to bring one in."
            : "The entry doesn't build now: this is the last version that did.");
        var copy = bar.Append("button", null, "Copy");
        copy.SetAttribute("type", "button");
        copy.SetAttribute("data-action", "copy");
        copy.ToggleAttribute("disabled", text is null);
        copy.AddEventListener("click", _ => CopyJson());
        var export = bar.Append("button", null, "Export");
        export.SetAttribute("type", "button");
        export.SetAttribute("data-action", "export");
        var picker = ExportPicker;
        export.ToggleAttribute("disabled", text is null || picker is null);
        if (picker is null)
            export.SetAttribute("title", "Export needs the file browser");
        export.AddEventListener("click", e => _ = ExportJson());
        if (!entry.Builds && text is not null)
            _json.AddClass("stale");
        else
            _json.RemoveClass("stale");
        if (text is null)
            return;
        var lines = text.Split('\n');
        var shown = lines.Length <= JsonLines ? text : string.Join('\n', lines.Take(JsonLines));
        _json.Append("pre", "json-text", shown);
        if (lines.Length > JsonLines)
            _json.Append("div", "json-more", $"{(lines.Length - JsonLines).ToString("N0", CultureInfo.InvariantCulture)} more lines: copy or export the entry for all of it.");
    }

    IExportFilePicker? _picker;

    /// <summary>Who picks an export's file: the one set, else the first part that is a picker (the file browser, step 8); null: Export is unavailable.</summary>
    public IExportFilePicker? ExportPicker
    {
        get => _picker ?? View.Parts.OfType<IExportFilePicker>().FirstOrDefault();
        set
        {
            _picker = value;
            if (_tab == FormTab.Json)
                Json();
        }
    }

    /// <summary>Copies the shown entry's JSON form to the clipboard (the document's: the system's in the program).</summary>
    public bool CopyJson()
    {
        if (_shown is not { } entry || JsonOf(entry) is not { } text)
            return false;
        View.Document.Clipboard.WriteText(text);
        View.Status = $"Copied the JSON form of {entry.Path}";
        return true;
    }

    /// <summary>
    /// Exports the shown entry's JSON form to a file the file browser picks (<see cref="IExportFilePicker"/>; the
    /// entry's name with <c>.json</c> suggested). Gives the file written, or null (cancelled, no picker, nothing to
    /// export, or the write failed: then a dialog says why).
    /// </summary>
    public async Task<string?> ExportJson()
    {
        if (_shown is not { } entry || JsonOf(entry) is not { } text || ExportPicker is not { } picker)
            return null;
        var file = await picker.PickExportFile($"Export {entry.Path}", entry.Name + ".json");
        if (file is null)
            return null;
        try
        {
            File.WriteAllText(file, text);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            await View.Dialogs.Ask("Export failed", $"The JSON form of {entry.Path} couldn't be written to {file}: {e.Message}", ("OK", null));
            return null;
        }
        View.Status = $"Exported {entry.Path} to {file}";
        return file;
    }

    // ---------- For tests and other parts ----------

    /// <summary>The form of the shown record entry (null for none, a file or an unreadable entry).</summary>
    internal FormView? Form => _form;
}

/// <summary>
/// Picks the file an export goes to: the file browser (step 8, M11-9) implements it as a part; the JSON tab's
/// Export asks the first part that does (none: Export is unavailable).
/// </summary>
public interface IExportFilePicker
{
    /// <summary>The file to write (a full path), or null when the user cancels. <paramref name="suggestedName"/> is a file name (<c>boom.json</c>).</summary>
    Task<string?> PickExportFile(string title, string suggestedName);
}

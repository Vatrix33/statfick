using Statventure.Editor.Session;
using Statventure.Editor.Shell;
using Statventure.Records;
using Statventure.UI.Dom;

namespace Statventure.Editor.Form;

/// <summary>What an entry dialog is for.</summary>
public enum EntryAction
{
    New,
    Duplicate,
    Rename,
    Remove,
    Revert,
}

/// <summary>
/// An entry dialog about to show (<see cref="EntryActions.Asking"/>): other parts may add to its content (step 10:
/// the entries that refer to the one renamed or removed, P19) and to what happens once the user confirms, after
/// the session's change (<see cref="Confirmed"/>, given the new path; null for a removal).
/// </summary>
public sealed class EntryDialog(EntryAction action, EditorEntry? entry, EditorDialog dialog)
{
    public EntryAction Action { get; } = action;

    /// <summary>The entry renamed, removed or duplicated (null for New).</summary>
    public EditorEntry? Entry { get; } = entry;

    public EditorDialog Dialog { get; } = dialog;

    public List<Action<string?>> Confirmed { get; } = [];
}

/// <summary>
/// New, Duplicate, Rename and Remove (M11-7, P14, P15), each asking first in a dialog, none undoable; F2 renames and
/// Delete removes the selected entry (Delete only from outside the form's rows, where it belongs to the controls).
/// The entry list (step 8) asks through these methods as the form's buttons do; the session reports the change
/// (<see cref="EditorSession.EntriesChanged"/>).
/// </summary>
public sealed class EntryActions : EditorPart
{
    protected internal override void Attach(UiElement? element)
    {
        View.Commands.Handle(EditorCommand.Rename, request =>
        {
            _ = Rename(Session.Selected!);
            return true;
        }, () => Session.Selected is { } entry && Session.CanRenameOrRemove(entry));
        View.Commands.Handle(EditorCommand.Remove, request =>
        {
            if (request.Target is { } target && target.Ancestors().Any(a => a.HasClass("form-rows")))
                return false;
            _ = Remove(Session.Selected!);
            return true;
        }, () => Session.Selected is { } entry && Session.CanRenameOrRemove(entry));
    }

    /// <summary>An entry dialog is about to show (other parts add to it).</summary>
    public event Action<EntryDialog>? Asking;

    /// <summary>
    /// Asks for a new entry's kind and name (P15) and adds it, its blank draft (the required fields empty, shown as
    /// problems), selected. <paramref name="kind"/> is the kind chosen first (default: the selected entry's).
    /// Gives the new entry, or null when cancelled.
    /// </summary>
    public async Task<EditorEntry?> New(RecordShape? kind = null)
    {
        if (!Session.CanAdd)
            return null;
        var kinds = Session.NewKinds();
        if (kinds.Count == 0)
            return null;
        kind ??= Session.Selected?.Kind;
        var chosen = kind is not null && kinds.Contains(kind) ? kind : kinds[0];
        var dialog = View.Dialogs.Create("New entry");
        var form = new NameForm(dialog, "Choose its kind and name.");
        var select = form.Field("Kind", "select");
        select.AddClass("kind");
        foreach (var shape in kinds)
            select.Append("option", text: $"{shape.DisplayName} ({shape.Name})");
        select.SelectedIndex = kinds.ToList().IndexOf(chosen);
        var name = form.Name(chosen, "");
        select.AddEventListener("change", _ =>
        {
            chosen = kinds[select.SelectedIndex];
            form.Label(chosen);
        });
        EditorEntry? made = null;
        form.Primary("Create", () =>
        {
            var (path, problem) = Resolve(chosen, name.Value, null);
            if (path is null)
                return problem;
            made = Session.Add(path, RecordDraft.Blank(chosen));
            Session.Select(made);
            return null;
        });
        await Show(EntryAction.New, null, dialog, () => made?.Path, confirmedIf: () => made is not null);
        return made;
    }

    /// <summary>Asks for the copy's name and adds a copy of <paramref name="entry"/>'s current draft (P15), selected. Gives the copy, or null.</summary>
    public async Task<EditorEntry?> Duplicate(EditorEntry entry)
    {
        if (!Session.CanAdd || entry.Kind is not { } kind)
            return null;
        var dialog = View.Dialogs.Create("Duplicate entry");
        var form = new NameForm(dialog, $"A copy of {entry.Path}, as it is now.");
        var inFolder = kind.NameOf(entry.Path) is not null;
        var name = form.Name(inFolder ? kind : null, FreeName(kind, inFolder, entry));
        EditorEntry? made = null;
        form.Primary("Duplicate", () =>
        {
            var (path, problem) = Resolve(inFolder ? kind : null, name.Value, null);
            if (path is null)
                return problem;
            made = Session.Duplicate(entry, path);
            Session.Select(made);
            return null;
        });
        await Show(EntryAction.Duplicate, entry, dialog, () => made?.Path, confirmedIf: () => made is not null);
        return made;
    }

    /// <summary>Asks for <paramref name="entry"/>'s new name and renames it (P14: not undoable). Gives whether it was renamed.</summary>
    public async Task<bool> Rename(EditorEntry entry)
    {
        if (!Session.CanRenameOrRemove(entry))
            return false;
        var dialog = View.Dialogs.Create("Rename entry");
        var form = new NameForm(dialog, $"Rename {entry.Path}. This can't be undone.");
        var shape = entry.Kind is { } kind && kind.NameOf(entry.Path) is not null ? kind : null;
        var name = form.Name(shape, shape is null ? entry.Path : entry.Name);
        var renamed = false;
        form.Primary("Rename", () =>
        {
            var (path, problem) = Resolve(shape, name.Value, entry);
            if (path is null)
                return problem;
            if (path != entry.Path)
            {
                Session.Rename(entry, path);
                renamed = true;
            }
            return null;
        });
        await Show(EntryAction.Rename, entry, dialog, () => entry.Path, confirmedIf: () => renamed);
        return renamed;
    }

    /// <summary>Asks before removing <paramref name="entry"/> (M11-7: not undoable) and removes it. Gives whether it was removed.</summary>
    public async Task<bool> Remove(EditorEntry entry)
    {
        if (!Session.CanRenameOrRemove(entry))
            return false;
        var dialog = View.Dialogs.Create("Remove entry");
        dialog.Content.Append("p", text: Session.Overridden(entry.Path) is not null
            ? $"Remove {entry.Path} from this pack? The game's own entry shows again. This can't be undone."
            : $"Remove {entry.Path}? This can't be undone.");
        var removed = false;
        var button = dialog.Buttons.Append("button", "primary", "Remove");
        button.SetAttribute("type", "button");
        button.AddEventListener("click", _ =>
        {
            Session.Remove(entry);
            removed = true;
            dialog.Close("remove");
        });
        dialog.AddButton("Cancel", null);
        await Show(EntryAction.Remove, entry, dialog, () => null, confirmedIf: () => removed);
        return removed;
    }

    /// <summary>
    /// Asks before reverting <paramref name="entry"/>, the pack's own copy of a game entry, to the game's (P7: not
    /// undoable) and reverts it. Gives whether it was reverted.
    /// </summary>
    public async Task<bool> Revert(EditorEntry entry)
    {
        if (!Session.CanRevert(entry))
            return false;
        var dialog = View.Dialogs.Create("Revert to the game's");
        dialog.Content.Append("p", text: $"Drop this pack's copy of {entry.Path} and show the game's own entry? Its changes and their history go. This can't be undone.");
        var reverted = false;
        var button = dialog.Buttons.Append("button", "primary", "Revert");
        button.SetAttribute("type", "button");
        button.AddEventListener("click", _ =>
        {
            Session.Revert(entry);
            reverted = true;
            dialog.Close("revert");
        });
        dialog.AddButton("Cancel", null);
        await Show(EntryAction.Revert, entry, dialog, () => entry.Path, confirmedIf: () => reverted);
        return reverted;
    }

    async Task Show(EntryAction action, EditorEntry? entry, EditorDialog dialog, Func<string?> result, Func<bool>? confirmedIf = null)
    {
        var asking = new EntryDialog(action, entry, dialog);
        Asking?.Invoke(asking);
        dialog.Show();
        var value = await dialog.Closed;
        if (value is null || (confirmedIf is not null && !confirmedIf()))
            return;
        var path = result();
        foreach (var confirmed in asking.Confirmed)
            confirmed(path);
    }

    /// <summary>
    /// The path a name gives in <paramref name="kind"/>'s folder (a whole path when null), or what is wrong with it:
    /// empty, one the kind's rule can't store, or a path the session refuses (the path rule, taken, a folder clash).
    /// </summary>
    (string? Path, string? Problem) Resolve(RecordShape? kind, string name, EditorEntry? renaming)
    {
        if (name.Length == 0)
            return (null, kind?.Folder is null ? "Give the entry a path." : "Give the entry a name.");
        var path = kind?.Folder is null ? name : kind.PathOf(name);
        if (path is null)
            return (null, $"\"{name}\" can't name a {kind!.DisplayName.ToLowerInvariant()} in {kind.Folder}.");
        if (Session.PathProblem(path, renaming) is { } problem)
            return (null, $"That can't be its path: {problem}{(problem.EndsWith('.') ? "" : ".")}");
        return (path, null);
    }

    /// <summary>A name for a copy: the entry's name with "-copy" (then "-copy-2" …), free in the data.</summary>
    string FreeName(RecordShape kind, bool inFolder, EditorEntry entry)
    {
        var name = inFolder ? entry.Name : entry.Path;
        for (var n = 1; n < 1000; n++)
        {
            var copy = name + (n == 1 ? "-copy" : $"-copy-{n}");
            var path = inFolder ? kind.PathOf(copy) : copy;
            if (path is not null && Session.PathProblem(path) is null)
                return copy;
        }
        return name;
    }

    /// <summary>
    /// A dialog's fields for an entry's name: a label naming the kind's folder (or "Path" for a whole path), the
    /// field, a line for what is wrong with it, and the primary button, which acts only when the name is right.
    /// </summary>
    sealed class NameForm
    {
        readonly EditorDialog _dialog;
        readonly UiElement _problem;
        UiElement? _label;
        UiElement? _name;
        Func<string?>? _act;

        public NameForm(EditorDialog dialog, string message)
        {
            _dialog = dialog;
            dialog.Content.Append("p", text: message);
            _problem = new UiElement("div") { ClassName = "dialog-problem" };
        }

        public UiElement Field(string label, string tag)
        {
            var line = _dialog.Content.Append("label", "dialog-field");
            line.Append("span", "dialog-label", label);
            return line.Append(tag);
        }

        public UiElement Name(RecordShape? kind, string value)
        {
            var line = _dialog.Content.Append("label", "dialog-field");
            _label = line.Append("span", "dialog-label");
            _name = line.Append("input", "name");
            _name.SetAttribute("type", "text");
            _name.SetAttribute("autofocus", "");
            _name.SetAttribute("spellcheck", "false");
            _name.Value = value;
            Label(kind);
            _dialog.Content.Append(_problem);
            _name.AddEventListener<UiKeyboardEvent>("keydown", e =>
            {
                if (e.Key == "Enter" && !e.CtrlKey && !e.AltKey && !e.MetaKey && !e.ShiftKey)
                {
                    e.PreventDefault();
                    Act();
                }
            });
            _name.AddEventListener("input", _ => _problem.TextContent = "");
            return _name;
        }

        public void Label(RecordShape? kind) =>
            _label!.TextContent = kind?.Folder is { } folder ? $"Name in {folder}" : "Path";

        public void Primary(string text, Func<string?> act)
        {
            _act = act;
            var button = _dialog.Buttons.Append("button", "primary", text);
            button.SetAttribute("type", "button");
            button.AddEventListener("click", _ => Act());
            _dialog.AddButton("Cancel", null);
        }

        void Act()
        {
            if (_act!() is { } problem)
                _problem.TextContent = problem;
            else
                _dialog.Close("ok");
        }
    }
}

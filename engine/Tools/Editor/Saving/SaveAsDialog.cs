using System.Globalization;
using Statventure.Editor.Session;
using Statventure.Editor.Shell;
using Statventure.UI.Dom;

namespace Statventure.Editor.Saving;

/// <summary>
/// The Save As dialog (M11-16, P24; the manager's decisions Q3 and Q4 of 2026-11-08): the entries a save writes, each with a
/// tick (those the editor reads ticked; those it can't read as records unticked, with why, kept unread when ticked; a
/// damaged one at the end, not offered), All and None, a note naming what is let go (entries left out with unsaved
/// changes), that the new pack opens read-only (an entry kept unread is ticked) or that nothing is ticked, then "Choose
/// file…" (the file browser next) or Cancel. Gives the entries ticked, or null when cancelled.
/// </summary>
public sealed class SaveAsDialog
{
    /// <summary>The most entries the note names as let go (then how many more).</summary>
    public const int Named = 8;

    readonly EditorDialog _dialog;
    readonly List<(SaveAsChoice Choice, UiElement Box)> _rows = [];
    readonly UiElement _note;
    readonly UiElement _choose;

    internal SaveAsDialog(SavingPart part, IReadOnlyList<SaveAsChoice> choices)
    {
        var pack = part.Session.Pack!;
        _dialog = part.View.Dialogs.Create("Save As");
        _dialog.Element.AddClass("saving-save-as");
        _dialog.Content.Append("p", text: pack.ReadOnlyReason is { } why
            ? $"{pack.Name} is read-only ({why}). Tick the entries to keep in a new pack; it opens in place of {pack.Name} once saved."
            : $"Tick the entries of {pack.Name} to save in a new pack; it opens in place of {pack.Name} once saved.");
        var tools = _dialog.Content.Append("div", "saving-tools");
        Tool(tools, "All", true);
        Tool(tools, "None", false);
        var list = _dialog.Content.Append("div", "saving-entries");
        foreach (var choice in choices.Where(c => c.Offered).Concat(choices.Where(c => !c.Offered)))
        {
            var row = list.Append("div", choice.Offered ? "saving-entry" : "saving-entry not-offered");
            var line = row.Append("label", "saving-line");
            var box = line.Append("input");
            box.SetAttribute("type", "checkbox");
            box.Checked = choice.Ticked;
            box.ToggleAttribute("disabled", !choice.Offered);
            box.AddEventListener("change", _ => Note());
            line.Append("span", "saving-path", choice.Entry.Path).SetAttribute("title", choice.Entry.Path);
            line.Append("span", "saving-kind", choice.Entry.KindName);
            if (choice.Reason is { } reason)
                row.Append("div", "saving-reason", choice.Offered ? $"Kept as it is, unread: {reason}" : $"Not offered: {reason}");
            _rows.Add((choice, box));
        }
        _note = _dialog.Content.Append("div", "saving-note");
        _choose = _dialog.Buttons.Append("button", "primary", "Choose file…");
        _choose.SetAttribute("type", "button");
        _choose.SetAttribute("autofocus", "");
        _choose.AddEventListener("click", _ => Choose());
        _dialog.AddButton("Cancel", null);
        _dialog.Show();
        Note();
    }

    void Tool(UiElement tools, string text, bool ticked)
    {
        var button = tools.Append("button", null, text);
        button.SetAttribute("type", "button");
        button.AddEventListener("click", _ => TickAll(ticked));
    }

    public EditorDialog Dialog => _dialog;

    /// <summary>The choices in the order shown (the offered ones by path, then those not offered).</summary>
    public IReadOnlyList<SaveAsChoice> Choices => [.. _rows.Select(r => r.Choice)];

    /// <summary>The tick of <paramref name="entry"/>'s row.</summary>
    public UiElement Box(EditorEntry entry) => _rows.Single(r => r.Choice.Entry == entry).Box;

    /// <summary>The entries ticked now, in the order shown.</summary>
    public IReadOnlyList<EditorEntry> Ticked => [.. _rows.Where(r => r.Box.Checked).Select(r => r.Choice.Entry)];

    /// <summary>The note's lines (what is let go, that the new pack opens read-only, that nothing is ticked).</summary>
    public IReadOnlyList<string> NoteLines => [.. _note.ElementChildren.Select(line => line.TextContent)];

    /// <summary>The "Choose file…" button.</summary>
    public UiElement ChooseButton => _choose;

    /// <summary>Ticks every offered entry (All) or none (None).</summary>
    public void TickAll(bool ticked)
    {
        foreach (var (choice, box) in _rows)
            if (choice.Offered)
                box.Checked = ticked;
        Note();
    }

    /// <summary>Ticks <paramref name="entry"/>'s row or not (an offered one), as a click on it does.</summary>
    public void Tick(EditorEntry entry, bool ticked)
    {
        var (choice, box) = _rows.Single(r => r.Choice.Entry == entry);
        if (!choice.Offered)
            return;
        box.Checked = ticked;
        Note();
    }

    /// <summary>"Choose file…": closes with the entries ticked (nothing ticked: it stays).</summary>
    public void Choose()
    {
        if (Ticked.Count > 0)
            _dialog.Close("choose");
    }

    void Note()
    {
        _note.ClearChildren();
        var ticked = Ticked;
        var letGo = _rows.Where(r => !r.Box.Checked && (r.Choice.Entry.IsChanged || r.Choice.Entry.IsRenamed)).Select(r => r.Choice.Entry.Path).ToList();
        if (letGo.Count > 0)
        {
            var named = string.Join(", ", letGo.Take(Named));
            if (letGo.Count > Named)
                named += $" and {(letGo.Count - Named).ToString("N0", CultureInfo.InvariantCulture)} more";
            _note.Append("div", null, $"Left out, their unsaved changes are let go: {named}.");
        }
        if (_rows.Exists(r => r.Box.Checked && r.Choice.KeptUnread))
            _note.Append("div", null, "The new pack keeps entries the editor can't read, so it opens read-only.");
        if (ticked.Count == 0)
            _note.Append("div", null, "Tick the entries to save.");
        _choose.ToggleAttribute("disabled", ticked.Count == 0);
    }

    /// <summary>The entries ticked when "Choose file…" closed it, or null (cancelled).</summary>
    internal Task<IReadOnlyList<EditorEntry>?> Closed => Wait();

    async Task<IReadOnlyList<EditorEntry>?> Wait() => await _dialog.Closed == "choose" ? Ticked : null;
}

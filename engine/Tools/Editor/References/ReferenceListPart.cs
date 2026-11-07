using Statventure.Editor.Session;
using Statventure.Editor.Shell;
using Statventure.Records;
using Statventure.UI.Dom;

namespace Statventure.Editor.References;

/// <summary>
/// The reference field's list (M11-11; Docs/Editor.md, "How it works", step 10): while a form field naming another
/// entry has the focus (a text field whose row's path a <see cref="ReferencesAttribute"/> mark holds for, a map's key
/// too), a list of the entries it can name opens under it: the session's record entries of the mark's kind (the game's
/// and the pack's) that its rule names, by name, filtered by the text typed since it opened (a name holding it, ignoring case); its last
/// line flags a text that names no such entry. Down and Up move through it, Enter or a click picks a name (the field's
/// own change commits it, P12), Enter without a name moved to commits the text as typed and Escape closes it; typing or
/// Down opens it again. It attaches to the form's documented markup (a
/// row's <c>data-path</c>, its text input) without changing the form (the manager's decision, 2026-11-07).
/// </summary>
public sealed class ReferenceListPart : EditorPart
{
    /// <summary>The most options shown at once (the list scrolls past them).</summary>
    public const int Rows = 8;

    UiElement? _input;
    UiElement? _row;
    UiElement? _list;
    ReferenceShape? _reference;
    readonly List<(string Name, EditorEntry Entry, UiElement Option)> _options = [];
    int _active = -1;
    bool _typed;

    protected internal override void Attach(UiElement? element)
    {
        var pane = View.Pane(EditorPane.Form);
        pane.AddEventListener<UiFocusEvent>("focusin", e => Focused(e.Target));
        pane.AddEventListener<UiFocusEvent>("focusout", e =>
        {
            if (e.Target == _input)
                Close();
        });
        pane.AddEventListener("input", e =>
        {
            if (e.Target is not { } target)
                return;
            if (_input is null && target == View.Document.FocusedElement)
                Focused(target);
            if (target != _input)
                return;
            _typed = true;
            Fill();
        });
        pane.AddEventListener<UiKeyboardEvent>("keydown", Key, capture: true);
        Session.SelectionChanged += Close;
        Session.EntryChanged += _ => FollowSoon();
        Session.EntriesChanged += _ => FollowSoon();
    }

    bool _following;

    /// <summary>Follows a change once the form has followed it (a task after the listeners of the change: the form may make the field's row again).</summary>
    void FollowSoon()
    {
        if (_input is null || _following)
            return;
        _following = true;
        View.Document.SetTimeout(() =>
        {
            _following = false;
            Follow();
        });
    }

    /// <summary>The field the list is open for, or null.</summary>
    public UiElement? Field => _input;

    /// <summary>The list's element while it is open, or null.</summary>
    public UiElement? List => _list;

    /// <summary>The names the list offers now, in order.</summary>
    public IReadOnlyList<string> Names => [.. _options.Select(o => o.Name)];

    /// <summary>The option moved to (Down, Up), or -1.</summary>
    public int Active => _active;

    /// <summary>Opens the list for <paramref name="target"/> when it is a reference field of the shown entry's form.</summary>
    void Focused(UiElement? target)
    {
        if (target is null)
            return;
        Close();
        if (target.Tag != "input" || target.InputType != "text" || Session.Selected is not { Draft: { } draft }
            || target.Ancestors().FirstOrDefault(a => a.HasAttribute("data-path")) is not { } row)
            return;
        var key = target.Parent is { } parent && parent.HasClass("key");
        if (EntryReferences.At(draft, row.GetAttribute("data-path")!, key) is not { } at)
            return;
        _input = target;
        _reference = at.Reference;
        _typed = false;
        target.SetAttribute("aria-autocomplete", "list");
        target.SetAttribute("aria-expanded", "true");
        _row = row;
        row.AddClass("reference-open");
        _list = new UiElement("div") { ClassName = "reference-list" };
        _list.SetAttribute("role", "listbox");
        _list.SetAttribute("aria-label", $"{at.Reference.Kind.DisplayName} entries");
        // A press in the list keeps the focus in the field (and so the list open).
        _list.AddEventListener<UiMouseEvent>("mousedown", e => e.PreventDefault());
        // In the row's flow under its line (the rows below make room), at the field's left and as wide: an overlay would
        // paint over the form pane's scroller from outside it, which Chromium composites apart (step 10, "Found").
        row.InsertBefore(_list, row.ElementChildren.ElementAtOrDefault(1));
        View.Document.UpdateLayout();
        var field = target.GetBoundingClientRect();
        var left = field.X - row.GetBoundingClientRect().X;
        _list.SetStyle("margin-left", Px(left));
        _list.SetStyle("width", Px(field.Width));
        Fill();
    }

    /// <summary>
    /// The options: every name the field can take, or once something is typed since the list opened, those holding the
    /// text; and the flag when the field's text names no entry.
    /// </summary>
    void Fill()
    {
        if (_input is null || _list is null || _reference is null)
            return;
        var text = _input.EditorText;
        _list.ClearChildren();
        _options.Clear();
        _active = -1;
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var (name, entry) in EntryReferences.Candidates(Session, _reference))
        {
            names.Add(name);
            if (_typed && text.Length > 0 && name.IndexOf(text, StringComparison.OrdinalIgnoreCase) < 0)
                continue;
            var option = _list.Append("div", "reference-option", name);
            option.SetAttribute("role", "option");
            option.SetAttribute("aria-selected", "false");
            option.SetAttribute("title", entry.Path);
            if (entry.Source == EntrySource.Game && !Session.IsModCopy(entry))
                option.AddClass("game");
            var index = _options.Count;
            option.AddEventListener("click", _ => Pick(index));
            _options.Add((name, entry, option));
        }
        var kind = Words(_reference.Kind.DisplayName);
        if (text.Length > 0 && !names.Contains(text))
            _list.Append("div", "reference-flag", $"\"{text}\" names no {kind}");
        else if (names.Count == 0)
            _list.Append("div", "reference-flag", $"No {kind} in the data");
    }

    /// <summary>The keys while the list is open: Down and Up move, Enter picks, Escape closes; Down opens it again.</summary>
    void Key(UiKeyboardEvent e)
    {
        if (e.Target is not { } target || e.CtrlKey || e.AltKey || e.MetaKey || e.ShiftKey)
            return;
        if (_input is null)
        {
            if (e.Key == "ArrowDown" && target == View.Document.FocusedElement)
            {
                Focused(target);
                if (_input is not null)
                    e.PreventDefault();
            }
            return;
        }
        switch (e.Key)
        {
            case "ArrowDown" or "ArrowUp" when _options.Count > 0:
                Move(e.Key == "ArrowDown" ? Math.Min(_active + 1, _options.Count - 1) : Math.Max(_active - 1, 0));
                e.PreventDefault();
                break;
            case "Enter" when _active >= 0:
                // The pick commits at once, so the field's own Enter and change after it find the value it gave: one commit.
                Pick(_active);
                break;
            case "Enter":
                // The field commits its text (P12); the list closes, as after a pick.
                Close();
                break;
            case "Escape":
                e.PreventDefault();
                Close();
                break;
        }
    }

    void Move(int index)
    {
        if (_active >= 0)
        {
            _options[_active].Option.RemoveClass("active");
            _options[_active].Option.SetAttribute("aria-selected", "false");
        }
        _active = index;
        var option = _options[index].Option;
        option.AddClass("active");
        option.SetAttribute("aria-selected", "true");
        // Scrolled as little as shows the option whole in the list's scrollport (Blink's "nearest").
        var list = _list!;
        View.Document.UpdateLayout();
        var at = option.GetBoundingClientRect();
        var top = list.GetBoundingClientRect().Y + list.ClientTop;
        var bottom = top + list.ClientHeight;
        if (at.Y < top)
            list.ScrollTop -= top - at.Y;
        else if (at.Y + at.Height > bottom)
            list.ScrollTop += at.Y + at.Height - bottom;
    }

    /// <summary>Puts the option's name in the field and commits it as the field commits typing (its change event); the list closes.</summary>
    void Pick(int index)
    {
        var input = _input!;
        var name = _options[index].Name;
        Close();
        input.Value = name;
        input.DispatchEvent(new UiEvent("change", bubbles: true, cancelable: false));
    }

    /// <summary>After a change of the entries: the list follows its field (gone with its row: closed; else filled again).</summary>
    void Follow()
    {
        if (_input is null)
            return;
        if (!_input.IsDescendantOf(View.Pane(EditorPane.Form)))
            Close();
        else
            Fill();
    }

    /// <summary>Closes the list.</summary>
    public void Close()
    {
        if (_input is { } input)
        {
            input.RemoveAttribute("aria-expanded");
            input.RemoveAttribute("aria-autocomplete");
            _row?.RemoveClass("reference-open");
        }
        _list?.Remove();
        _list = null;
        _row = null;
        _input = null;
        _reference = null;
        _options.Clear();
        _active = -1;
    }

    static string Px(float value) => Math.Round(value, 2).ToString(System.Globalization.CultureInfo.InvariantCulture) + "px";

    /// <summary>A display name inside a sentence (as the engine's checks write it): its first letter small unless it is an acronym.</summary>
    static string Words(string displayName) =>
        displayName.Length > 1 && !char.IsUpper(displayName[1]) ? char.ToLowerInvariant(displayName[0]) + displayName[1..] : displayName;
}

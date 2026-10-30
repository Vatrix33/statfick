using System.Globalization;
using System.Numerics;
using Statventure.Core;
using Statventure.Platform;

namespace Statventure.UI.Dom;

/// <summary>
/// Drop-down selects (Blink's MenuListSelectType with its list picker popup). Closed, a focused
/// select changes with the arrows (Page Up/Down by three, Home, End) and type-ahead, firing input
/// and change; a press, Space, Enter, Alt+Up/Down or F4 opens its list. The list is the game's
/// own element in the top layer (&lt;sv-select-popup&gt; with &lt;sv-select-option&gt; rows), but it
/// behaves as Chromium's popup window: the page sees no events from it and no keys while it is
/// open; the row under the pointer is highlighted; a release on a row picks it, as Enter and Tab
/// pick the highlighted one; arrows move the highlight and that choice is kept even when the list
/// is cancelled (Escape, Alt+Up/Down, a press elsewhere on the page, the wheel outside it, the
/// window losing focus).
/// </summary>
public sealed partial class UiInput
{
    /// <summary>Blink's type-ahead: letters typed within a second make a prefix; repeating one letter cycles through its options.</summary>
    sealed class TypeAhead
    {
        const double Timeout = 1;
        readonly System.Text.StringBuilder _buffer = new();
        double? _last;
        char _repeating;

        public bool HasActiveSession(double now) => _last is { } last && now - last <= Timeout;

        public void Reset()
        {
            _last = null;
            _buffer.Clear();
        }

        /// <summary>The list index the typed character leads to (-1: none); <paramref name="labels"/> are the list items' labels (null for those that can't be picked).</summary>
        public int Handle(double now, char character, IReadOnlyList<string?> labels, int selected)
        {
            if (_last is { } last)
            {
                if (now < last)
                    return -1;
                if (now - last > Timeout)
                    _buffer.Clear();
            }
            _last = now;
            _buffer.Append(character);
            if (labels.Count < 1)
                return -1;
            var offset = 1;
            string prefix;
            if (character == _repeating)
                prefix = character.ToString();
            else
            {
                prefix = _buffer.ToString();
                if (_buffer.Length > 1)
                {
                    _repeating = '\0';
                    offset = 0;
                }
                else
                    _repeating = character;
            }
            var index = ((selected < 0 ? 0 : selected) + offset) % labels.Count;
            for (var i = 0; i < labels.Count; i++, index = (index + 1) % labels.Count)
                if (labels[index] is { } label && label.TrimStart(' ', '\t', '\n', '\f', '\r', ' ').StartsWith(prefix, StringComparison.InvariantCultureIgnoreCase))
                    return index;
            return -1;
        }
    }

    readonly Dictionary<UiElement, TypeAhead> _typeAheads = [];

    TypeAhead TypeAheadOf(UiElement select)
    {
        if (!_typeAheads.TryGetValue(select, out var typeAhead))
            _typeAheads[select] = typeAhead = new TypeAhead();
        return typeAhead;
    }

    // ---------- The closed select ----------

    /// <summary>
    /// Keys on a select (HTMLSelectElement::DefaultEventHandler): repeats are ignored; Alt+Up/Down
    /// and F4 open the list, the arrows and page keys move the selection (input and change).
    /// </summary>
    bool SelectKeyDown(UiElement select, UiKeyboardEvent e)
    {
        if (!e.Repeat)
            select.SetUserHasEditedTheField();
        if (select.IsDisabled || e.Repeat || select.Box is null || !select.UsesMenuList)
            return false;
        if (((e.Key is "ArrowDown" or "ArrowUp") && e.AltKey) || (!e.AltKey && !e.CtrlKey && e.Key == "F4"))
        {
            OpenSelectByKeyboard(select);
            e.SetDefaultHandled();
            return true;
        }
        if (e.ShiftKey || e.CtrlKey || e.AltKey || e.MetaKey)
            return false;
        var items = select.ListItems;
        var current = select.SelectedOption is { } selected ? items.IndexOf(selected) : -1;
        UiElement? option;
        switch (e.Key)
        {
            case "ArrowDown" or "ArrowRight":
                option = NextValidOption(items, current, 1, 1);
                break;
            case "ArrowUp" or "ArrowLeft":
                option = NextValidOption(items, current, -1, 1);
                break;
            case "PageDown":
                option = NextValidOption(items, current, 1, 3);
                break;
            case "PageUp":
                option = NextValidOption(items, current, -1, 3);
                break;
            case "Home":
                option = NextValidOption(items, -1, 1, 1);
                break;
            case "End":
                option = NextValidOption(items, items.Count, -1, 1);
                break;
            default:
                return false;
        }
        if (option is not null)
            SelectOptionWithEvents(select, option);
        e.SetDefaultHandled();
        return true;
    }

    /// <summary>Keypress on a closed select: Space (outside a type-ahead) and Enter open the list; other characters type ahead.</summary>
    bool SelectKeyPress(UiElement select, UiKeyboardEvent e)
    {
        if (select.IsDisabled || e.Repeat || select.Box is null || !select.UsesMenuList)
            return false;
        if ((e.Key == " " && !TypeAheadOf(select).HasActiveSession(Now)) || e.Key == "Enter")
        {
            OpenSelectByKeyboard(select);
            e.SetDefaultHandled();
            return true;
        }
        if (e.Key.Length == 1 && !char.IsControl(e.Key[0]) && !e.CtrlKey && !e.AltKey && !e.MetaKey)
        {
            var items = select.ListItems;
            var index = TypeAheadOf(select).Handle(Now, e.Key[0], Labels(items), select.SelectedOption is { } selected ? items.IndexOf(selected) : -1);
            if (index >= 0 && items[index].Tag == "option")
                SelectOptionWithEvents(select, items[index]);
            e.SetDefaultHandled();
            return true;
        }
        return false;
    }

    /// <summary>A press on a select (MenuListSelectType): it takes focus as a click would and opens its list (closes it if open).</summary>
    internal void SelectMouseDown(UiElement select, UiMouseEvent e)
    {
        if (e.Button != 0 || !select.UsesMenuList || select.IsDisabled)
            return;
        Focus(select, FocusOrigin.Pointer);
        // The press that just closed this select's list doesn't open it again.
        if (_popupJustClosed == select)
            _popupJustClosed = null;
        else if (select.Box is not null)
            OpenSelect(select);
        e.SetDefaultHandled();
    }

    void OpenSelectByKeyboard(UiElement select)
    {
        Focus(select, FocusOrigin.Script);
        if (!select.IsDisabled && select.Box is not null)
            OpenSelect(select);
    }

    /// <summary>Selects the option as the user did: input and change when it differs from the last one they were fired for.</summary>
    static void SelectOptionWithEvents(UiElement select, UiElement option)
    {
        var dispatch = select.LastOnChangeOption != option;
        option.SetSelectedness(true);
        select.LastOnChangeOption = option;
        if (dispatch)
        {
            select.DispatchInputEvent();
            select.DispatchChangeEvent();
        }
    }

    /// <summary>SelectType::NextValidOption: from a list index, the option <paramref name="skip"/> enabled, shown options away (or the last one found).</summary>
    static UiElement? NextValidOption(IReadOnlyList<UiElement> items, int index, int direction, int skip)
    {
        UiElement? last = null;
        for (index += direction; index >= 0 && index < items.Count; index += direction)
        {
            skip--;
            var item = items[index];
            if (item.Tag != "option" || item.Style?.Display == Style.Display.None || item.IsOptionDisabledForUser)
                continue;
            last = item;
            if (skip <= 0)
                break;
        }
        return last;
    }

    /// <summary>The list items' labels for type-ahead (null for groups and disabled options).</summary>
    static List<string?> Labels(IReadOnlyList<UiElement> items) =>
        items.Select(i => i.Tag == "option" && !i.IsOptionDisabledForUser ? i.OptionLabel : null).ToList();

    // ---------- The popup ----------

    UiElement? _popupSelect, _popupJustClosed;
    UiElement? _popup;
    readonly List<(UiElement Row, UiElement Option)> _popupRows = [];
    int _popupHighlight = -1;
    int _indexToSelectOnCancel = -1;
    bool _popupPressed;

    /// <summary>The select whose list is open (null when none).</summary>
    public UiElement? OpenSelectElement => _popupSelect is { } select && InDocument(select) ? select : null;

    /// <summary>The open list's element in the top layer.</summary>
    public UiElement? SelectPopup => OpenSelectElement is null ? null : _popup;

    void OpenSelect(UiElement select)
    {
        CloseSelectPopup(commit: false);
        select.LastOnChangeOption = select.SelectedOption;
        _indexToSelectOnCancel = -1;
        _popupSelect = select;
        _popupPressed = false;
        UpdateFocusVisible();
        BuildPopup(select);
        _document.StateChanged(select, [Css.PseudoClassKind.Open]);
        TypeAheadOf(select).Reset();
    }

    /// <summary>The list's rows: the select's groups (a label row and their options) and options, as Chromium's list picker.</summary>
    void BuildPopup(UiElement select)
    {
        _popup?.Remove();
        _popupRows.Clear();
        var popup = new UiElement("sv-select-popup");
        // The list takes the select's font (Chromium's popup uses the select's style).
        if (select.Style is { } style)
        {
            popup.SetStyle("font-family", string.Join(", ", style.FontFamilies.Select(f => "\"" + f + "\"")));
            popup.SetStyle("font-size", style.FontSize.ToString(CultureInfo.InvariantCulture) + "px");
            popup.SetStyle("font-weight", style.FontWeight.ToString(CultureInfo.InvariantCulture));
        }
        if (select.Id is { Length: > 0 } id)
            popup.SetAttribute("for", id);
        UiElement? group = null;
        foreach (var item in select.ListItems)
        {
            if (item.Tag == "optgroup")
            {
                group = popup.Append("sv-select-group");
                // The list picker gives a group its title even when empty (so its options show none of the page's).
                group.SetAttribute("title", item.GetAttribute("title") ?? "");
                group.Append("sv-select-group-label", text: item.GetAttribute("label") ?? "");
                if (item.HasAttribute("disabled"))
                    group.SetAttribute("disabled", "");
                continue;
            }
            if (item.Style?.Display == Style.Display.None)
                continue;
            var parent = item.Parent?.Tag == "optgroup" && group is not null ? group : popup;
            var row = parent.Append("sv-select-option", text: item.OptionLabel);
            if (item.GetAttribute("title") is { Length: > 0 } title)
                row.SetAttribute("title", title);
            if (item.IsOptionDisabledForUser)
                row.SetAttribute("disabled", "");
            _popupRows.Add((row, item));
        }
        _popup = popup;
        _document.TopLayer.InsertBefore(popup, null);
        SetHighlight(_popupRows.FindIndex(r => r.Option == select.SelectedOption), scroll: true);
        _document.PlaceSelectPopup(select, popup);
    }

    void SetHighlight(int index, bool scroll)
    {
        if (index == _popupHighlight)
            return;
        if (_popupHighlight >= 0 && _popupHighlight < _popupRows.Count)
            _popupRows[_popupHighlight].Row.RemoveAttribute("highlighted");
        _popupHighlight = index;
        if (index >= 0 && index < _popupRows.Count)
        {
            _popupRows[index].Row.SetAttribute("highlighted", "");
            if (scroll)
                _scrollHighlightPending = true;
        }
    }

    bool _scrollHighlightPending;

    /// <summary>Called by the document after the popup is laid out: keeps the highlighted row in view (at the bottom, as scrollIntoView(false)).</summary>
    internal void ScrollPopupHighlightIntoView()
    {
        if (!_scrollHighlightPending || _popup?.Box is not { } box || _popupHighlight < 0 || _popupRows[_popupHighlight].Row.Box is not { } row)
            return;
        _scrollHighlightPending = false;
        var scroll = _popup.LayoutScroll;
        var top = row.ViewportRect.Y - box.ViewportRect.Y - box.Border.Top + scroll.Y;
        var bottom = top + row.ViewportRect.Height;
        var client = box.PaddingBoxHeight;
        if (bottom > scroll.Y + client)
            _document.LayoutScrollTo(_popup, scroll with { Y = bottom - client });
        else if (top < scroll.Y)
            _document.LayoutScrollTo(_popup, scroll with { Y = top });
    }

    /// <summary>
    /// The list closes. Committed (a row picked), the select takes that option (input and change if
    /// it is new); cancelled, an option the keys moved to is kept the same way.
    /// </summary>
    void CloseSelectPopup(bool commit, int pick = -1, bool byKeyboard = true)
    {
        if (_popupSelect is not { } select)
            return;
        var rows = _popupRows.ToList();
        var cancelIndex = _indexToSelectOnCancel;
        _popupSelect = null;
        _popup?.Remove();
        _popup = null;
        _popupRows.Clear();
        _popupHighlight = -1;
        _indexToSelectOnCancel = -1;
        _popupPressed = false;
        if (!InDocument(select))
            return;
        UpdateFocusVisible();
        _document.StateChanged(select, [Css.PseudoClassKind.Open]);
        var index = commit ? pick : cancelIndex;
        // HTMLSelectElement::SelectOptionByPopup: nothing when it is already the selected one.
        if (index >= 0 && index < rows.Count && rows[index].Option is { } option && option.SelectOwner == select && option != select.SelectedOption)
            SelectOptionWithEvents(select, option);
        if (commit && InDocument(select))
        {
            // InternalPopupMenu::SetValueAndClosePopup: the select then gets a click (and a mouseup
            // first when the pointer picked), at its top left corner.
            var corner = _document.LayoutBoundingClientRect(select).Position;
            if (!byKeyboard)
                select.DispatchEvent(MouseEvent("mouseup", corner, 0, KeyModifiers.None));
            select.DispatchEvent(MouseEvent("click", corner, 0, KeyModifiers.None));
        }
    }

    /// <summary>The row under a point in the open list (-1: none, or a group label).</summary>
    int PopupRowAt(Vector2 position)
    {
        for (var i = 0; i < _popupRows.Count; i++)
            if (_popupRows[i].Row.Box is { } box && box.ViewportRect.Contains(position) && _popup?.Box is { } popup && popup.ViewportRect.Contains(position))
                return i;
        return -1;
    }

    bool OverPopup(Vector2 position) => SelectPopup?.Box is { } box && box.ViewportRect.Contains(position);

    /// <summary>The pointer moved over the open list: the row under it is highlighted (unless disabled).</summary>
    bool PopupPointerMove(Vector2 position)
    {
        if (SelectPopup is null || !OverPopup(position))
            return false;
        // The pointer is in the list's window: the page lost it.
        if (_hovered is not null)
            UpdateHover(null, position, KeyModifiers.None);
        Cursor = "default";
        var row = PopupRowAt(position);
        if (row >= 0 && !_popupRows[row].Row.HasAttribute("disabled"))
            SetHighlight(row, scroll: false);
        return true;
    }

    /// <summary>A button went down: in the list it is the list's; elsewhere a main-button press cancels the list (and goes on to the page).</summary>
    bool PopupPointerDown(Vector2 position, int button)
    {
        if (SelectPopup is null)
            return false;
        if (OverPopup(position))
        {
            if (button == 0)
                _popupPressed = true;
            return true;
        }
        if (button == 0)
        {
            _popupJustClosed = _popupSelect;
            CloseSelectPopup(commit: false);
        }
        return false;
    }

    /// <summary>A release in the list (after a press in it) on an enabled row picks it.</summary>
    bool PopupPointerUp(Vector2 position, int button)
    {
        if (SelectPopup is null || !_popupPressed)
            return false;
        if (button == 0)
            _popupPressed = false;
        if (!OverPopup(position))
            return true;
        var row = PopupRowAt(position);
        if (button == 0 && row >= 0 && !_popupRows[row].Row.HasAttribute("disabled"))
            CloseSelectPopup(commit: true, pick: row, byKeyboard: false);
        return true;
    }

    /// <summary>The wheel scrolls the list over it, and cancels it elsewhere (then scrolls the page).</summary>
    bool PopupWheel(Vector2 position, Vector2 delta)
    {
        if (SelectPopup is not { } popup)
            return false;
        if (OverPopup(position))
        {
            _document.UserScroll(popup, new Vector2(0, delta.Y), ScrollKind.Wheel);
            return true;
        }
        CloseSelectPopup(commit: false);
        return false;
    }

    /// <summary>Keys while the list is open are the list's (the page sees none).</summary>
    bool PopupKeyDown(string key, KeyModifiers modifiers)
    {
        if (SelectPopup is null || _popupSelect is not { } select)
            return false;
        var alt = modifiers.HasFlag(KeyModifiers.Alt);
        switch (key)
        {
            case "Escape":
                CloseSelectPopup(commit: false);
                return true;
            case "Tab" or "Enter":
                CloseSelectPopup(commit: true, pick: _popupHighlight);
                return true;
            case "ArrowDown" or "ArrowUp" when alt:
                CloseSelectPopup(commit: false);
                return true;
        }
        if ((modifiers & (KeyModifiers.Control | KeyModifiers.Alt | KeyModifiers.Super)) != 0)
            return true;
        int target;
        switch (key)
        {
            case "ArrowDown":
                target = NextEnabledRow(_popupHighlight, 1, 1);
                break;
            case "ArrowUp":
                target = NextEnabledRow(_popupHighlight, -1, 1);
                break;
            case "PageDown":
                // The list is a 20-row list box: a page is 19 rows.
                target = NextEnabledRow(_popupHighlight, 1, 19);
                break;
            case "PageUp":
                target = NextEnabledRow(_popupHighlight, -1, 19);
                break;
            case "Home":
                target = NextEnabledRow(-1, 1, 1);
                break;
            case "End":
                target = NextEnabledRow(_popupRows.Count, -1, 1);
                break;
            default:
                if (key.Length == 1 && !char.IsControl(key[0]))
                {
                    var labels = _popupRows.Select(r => r.Row.HasAttribute("disabled") ? null : r.Option.OptionLabel).ToList();
                    target = TypeAheadOf(select).Handle(Now, key[0], labels, _popupHighlight);
                }
                else
                    target = -1;
                break;
        }
        // The list box moved its selection: the choice is kept even if the list is cancelled.
        if (target >= 0 && target != _popupHighlight)
        {
            SetHighlight(target, scroll: true);
            _indexToSelectOnCancel = target;
        }
        return true;
    }

    int NextEnabledRow(int from, int direction, int skip)
    {
        var last = -1;
        for (var i = from + direction; i >= 0 && i < _popupRows.Count; i += direction)
        {
            skip--;
            if (_popupRows[i].Row.HasAttribute("disabled"))
                continue;
            last = i;
            if (skip <= 0)
                break;
        }
        return last >= 0 ? last : from >= 0 && from < _popupRows.Count ? from : -1;
    }

    /// <summary>Each frame: a list whose select went away, was disabled or hidden closes.</summary>
    void CheckSelectPopup()
    {
        if (_popupSelect is not { } select)
            return;
        if (!InDocument(select) || select.IsDisabled || select.Box is null)
            CloseSelectPopup(commit: false);
    }
}

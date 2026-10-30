namespace Statventure.UI.Dom;

/// <summary>How a selection change scrolls the caret into view: not at all (script), the least that shows it, or to the top (a page move).</summary>
internal enum SelectionReveal { None, IfNeeded, Top }

/// <summary>
/// A text control's selection, as Blink's TextControlElement keeps it: UTF-16 offsets into the
/// editor's text and a direction (forward unless the user or a script made it backward). The
/// control keeps it while unfocused; script changes (setSelectionRange, select) fire a select
/// event at the next frame; every change of the offsets fires selectionchange at the control.
/// </summary>
public partial class UiElement
{
    int _selectionStart, _selectionEnd;
    bool _selectionBackward;
    bool _selectionChangePending;

    /// <summary>Text controls that offer the selection API (number and the others edit, but don't expose it).</summary>
    internal bool SupportsSelectionApi => Tag == "textarea" || (Tag == "input" && InputType is "text" or "search" or "url" or "tel" or "password");

    /// <summary>The selection in the editor's text, clamped to it.</summary>
    internal (int Start, int End, bool Backward) EditorSelection
    {
        get
        {
            var length = EditorText.Length;
            var end = Math.Min(_selectionEnd, length);
            var start = Math.Min(_selectionStart, end);
            return (start, end, _selectionBackward && start != end);
        }
    }

    /// <summary>The fixed end of the selection (where Shift+arrows extend from) and the moving one.</summary>
    internal (int Anchor, int Focus) EditorAnchorAndFocus
    {
        get
        {
            var (start, end, backward) = EditorSelection;
            return backward ? (end, start) : (start, end);
        }
    }

    /// <summary>
    /// Sets the selection (Blink's SetSelectionRange). Returns whether it changed; a change fires
    /// selectionchange at the control (once per frame), and one the user made that selects a
    /// range fires select at once.
    /// </summary>
    internal bool SetEditorSelection(int start, int end, bool backward, bool byUser = false, bool textReplaced = false, SelectionReveal reveal = SelectionReveal.None)
    {
        var length = EditorText.Length;
        end = Math.Clamp(end, 0, length);
        start = Math.Clamp(start, 0, end);
        backward &= start != end;
        var changed = start != _selectionStart || end != _selectionEnd || backward != _selectionBackward;
        _selectionStart = start;
        _selectionEnd = end;
        _selectionBackward = backward;
        if (Document is { } document)
        {
            document.SelectionMoved(this);
            // In the focused field the page's selection moves: new text is a new selection, except
            // that an emptied field's announces nothing (as Chromium does); elsewhere the cache changed.
            var announce = IsFocused && document.FocusedElement == this && textReplaced ? length > 0 : changed;
            if (announce)
                ScheduleSelectionChange(document);
            // The user's moves keep the caret in view (FrameSelection::SetSelection by the user, when it changed).
            if (changed)
                document.RevealSelection(this, reveal);
            if (byUser && start != end && changed)
                DispatchEvent(new UiEvent("select", bubbles: true, cancelable: false) { IsTrusted = true });
        }
        return changed;
    }

    /// <summary>
    /// The value changed other than by typing (script, a step, the cancel button, a reset): the caret
    /// goes to the end (Blink's kSetSelectionToEnd); in the focused field the selection is new.
    /// </summary>
    internal void ValueReplaced()
    {
        if (!IsTextField)
            return;
        Document?.CloseTyping();
        SetEditorSelection(EditorText.Length, EditorText.Length, false, textReplaced: true);
    }

    /// <summary>Focus brought the page's selection into this control: selectionchange, even with the same offsets.</summary>
    internal void SelectionEntered()
    {
        if (Document is { } document)
            ScheduleSelectionChange(document);
    }

    /// <summary>selectionchange at this control at the next frame (one per frame).</summary>
    void ScheduleSelectionChange(UiDocument document)
    {
        if (_selectionChangePending)
            return;
        _selectionChangePending = true;
        document.PostLateTask(() =>
        {
            _selectionChangePending = false;
            DispatchEvent(new UiEvent("selectionchange", bubbles: true, cancelable: false) { IsTrusted = true });
        });
    }

    /// <summary>select at the next frame (Blink's ScheduleSelectEvent, for script changes).</summary>
    void ScheduleSelect()
    {
        Document?.PostTask(() => DispatchEvent(new UiEvent("select", bubbles: true, cancelable: false) { IsTrusted = true }));
    }

    /// <summary>selectionStart (null for inputs without the selection API).</summary>
    public int? SelectionStart
    {
        get => SupportsSelectionApi ? EditorSelection.Start : null;
        set => SetSelectionRange(value ?? 0, Math.Max(value ?? 0, EditorSelection.End), SelectionDirection ?? "none");
    }

    public int? SelectionEnd
    {
        get => SupportsSelectionApi ? EditorSelection.End : null;
        set => SetSelectionRange(EditorSelection.Start, value ?? 0, SelectionDirection ?? "none");
    }

    /// <summary>selectionDirection: "forward" or "backward" (a collapsed or undirected selection is forward, as on Linux and Windows).</summary>
    public string? SelectionDirection
    {
        get => SupportsSelectionApi ? EditorSelection.Backward ? "backward" : "forward" : null;
        set => SetSelectionRange(EditorSelection.Start, EditorSelection.End, value ?? "none");
    }

    /// <summary>setSelectionRange(): selects from start to end (clamped); a change fires select at the next frame.</summary>
    public void SetSelectionRange(int start, int end, string direction = "none")
    {
        if (!SupportsSelectionApi)
            throw new InvalidOperationException($"The input element's type ('{InputType}') does not support selection.");
        if (SetEditorSelection(start, end, direction == "backward"))
            ScheduleSelect();
    }

    /// <summary>Keyboard focus selects the input's text (as select(), without focusing again).</summary>
    internal void SelectAllForFocus()
    {
        if (SetEditorSelection(0, EditorText.Length, backward: false))
            ScheduleSelect();
    }

    /// <summary>select(): selects all the text (select fires at the next frame) and focuses the control.</summary>
    public void Select()
    {
        if (!IsTextField)
            return;
        if (SetEditorSelection(0, EditorText.Length, backward: false))
            ScheduleSelect();
        Focus();
    }
}

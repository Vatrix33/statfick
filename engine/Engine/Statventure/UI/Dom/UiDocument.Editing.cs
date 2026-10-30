using Statventure.Core;
using Statventure.Text;
using Statventure.UI.Paint;
using Statventure.UI.Style;

namespace Statventure.UI.Dom;

/// <summary>The clipboard the editor copies to and pastes from (the window host gives the system's).</summary>
public interface IUiClipboard
{
    string? ReadText();
    void WriteText(string text);
}

/// <summary>A clipboard of the document's own (until the host gives the system's).</summary>
public sealed class MemoryClipboard : IUiClipboard
{
    string? _text;
    public string? ReadText() => _text;
    public void WriteText(string text) => _text = text;
}

/// <summary>
/// Editing text fields as Blink's editor does on Linux: its key bindings (arrows by graphemes,
/// Ctrl+arrows by words, Home and End, Shift extending, Backspace and Delete by character or word,
/// Ctrl+Shift+Backspace to the line's start, Ctrl+A, Ctrl+C/X/V and Ctrl+Insert, Shift+Insert,
/// Shift+Delete, Ctrl+Z, Ctrl+Shift+Z and Ctrl+Y), typed characters on keypress. Every edit offers
/// itself first (beforeinput, cancelable), passes the field's filters (a number field's characters,
/// maxlength, no line breaks in one-line fields), changes the text and fires input; the frame keeps
/// one undo stack in which typing runs into one step as Blink's typing commands do.
/// </summary>
public sealed partial class UiDocument
{
    /// <summary>Where copies go and pastes come from.</summary>
    public IUiClipboard Clipboard { get; set; } = new MemoryClipboard();

    /// <summary>How long the caret shows and hides (seconds; Blink's default, the host sets the system's).</summary>
    public double CaretBlinkInterval { get; set; } = 0.5;

    double _caretBlinkStart;

    /// <summary>A field's selection moved: the caret shows again (its blink restarts).</summary>
    internal void SelectionMoved(UiElement field)
    {
        _caretBlinkStart = Animations.Time;
        // FrameSelection::SetSelection forgets the x that up and down keep (they set it again).
        _verticalX = null;
        Repaint();
    }

    /// <summary>The x up and down move the caret at (Blink's x_pos_for_vertical_arrow_navigation_), viewport units.</summary>
    float? _verticalX;

    /// <summary>Whether a field's caret is drawn now: focused, window focused, editable, a collapsed selection, the blink's on half.</summary>
    internal bool CaretShown(UiElement field)
    {
        if (field != FocusedElement || !WindowFocused || !IsEditable(field) || field.EditorSelection.Start != field.EditorSelection.End)
            return false;
        if (CaretBlinkInterval <= 0)
            return true;
        var phase = (Animations.Time - _caretBlinkStart) / CaretBlinkInterval;
        return ((long)Math.Floor(phase) & 1) == 0;
    }

    /// <summary>The caret blinks: the next frame differs when the blink turns.</summary>
    bool CaretBlinking => _caretPainting && FocusedElement is { } focused && focused.IsTextField && WindowFocused && CaretBlinkInterval > 0 && CaretShown(focused) != _caretPaintedShown;

    bool _caretPaintedShown, _caretPainting;

    /// <summary>The painter tells the document whether it drew the caret.</summary>
    internal void CaretPainted(bool shown)
    {
        _caretPainting = true;
        _caretPaintedShown = shown;
    }

    internal static bool IsEditable(UiElement field) => field.IsTextField && !field.IsDisabled && !field.ReadOnly;

    /// <summary>
    /// Keeps the focused field's caret (the selection's moving end) in view, as
    /// FrameSelection::RevealSelection scrolls its rect into view: in the field (a one-line field's
    /// editor sideways, a textarea's box), then in every scroller around it out to the document;
    /// the least scroll that shows it, or with it at the top (after a page move).
    /// </summary>
    internal void RevealSelection(UiElement field, SelectionReveal reveal)
    {
        if (reveal == SelectionReveal.None || field != FocusedElement || field.Box is not { } box)
            return;
        UpdateLayout();
        if (FieldGeometry.Of(this, box, field) is not { } geometry)
            return;
        var (_, focus) = field.EditorAnchorAndFocus;
        var exact = geometry.CaretRect(focus);
        // AbsoluteCaretBounds: on whole pixels around the caret.
        var caret = RectF.FromEdges(MathF.Floor(exact.Left), MathF.Floor(exact.Top), MathF.Ceiling(exact.Right), MathF.Ceiling(exact.Bottom));
        var align = reveal == SelectionReveal.Top ? ScrollAlign.Start : ScrollAlign.Nearest;
        if (field.Tag == "textarea")
            caret = ScrollIntoViewIn(field, geometry.Clip, caret, default, align, align, ScrollMode.Auto);
        else
            caret = RevealInEditor(field, geometry, caret);
        ScrollRectIntoView(box, caret, default, align, align, ScrollMode.Auto);
    }

    /// <summary>Scrolls a one-line field's editor sideways by the least that shows the caret; returns where the caret shows then.</summary>
    RectF RevealInEditor(UiElement field, FieldGeometry geometry, RectF caret)
    {
        var scroll = field.EditorScrollLeft;
        var left = geometry.Editor.X;
        var right = geometry.Editor.Right;
        if (caret.Right > right)
            scroll += caret.Right - right;
        else if (caret.X < left)
            scroll -= left - caret.X;
        // The inner editor scrolls as any element (PaintLayerScrollableArea::ScrollIntoView): to the
        // rounded offset, at most its pixel-snapped content (the text and the caret) past its width.
        var max = MathF.Max(0, MathF.Round(geometry.Layout.Width + geometry.CaretWidth, MidpointRounding.AwayFromZero) - MathF.Round(geometry.Editor.Width, MidpointRounding.AwayFromZero));
        scroll = MathF.Max(0, MathF.Min(MathF.Round(scroll, MidpointRounding.AwayFromZero), max));
        var moved = scroll - field.EditorScrollLeft;
        if (moved != 0)
        {
            field.EditorScrollLeft = scroll;
            InnerEditorScrolled(field);
        }
        return caret.Offset(-moved, 0);
    }

    // ---------- Undo ----------

    enum UndoKind { InsertText, DeleteKey, ForwardDeleteKey, Other }

    sealed class UndoStep
    {
        public required UiElement Field;
        public required string BeforeText;
        public required (int Start, int End, bool Backward) BeforeSelection;
        public required string AfterText;
        public required (int Start, int End, bool Backward) AfterSelection;
        public required UndoKind Kind;
    }

    readonly List<UndoStep> _undo = [];
    readonly List<UndoStep> _redo = [];
    UndoStep? _openTyping;

    /// <summary>Typing stops running into the last undo step (the selection moved, focus moved, another kind of edit).</summary>
    internal void CloseTyping() => _openTyping = null;

    /// <summary>
    /// Records an edit for undo (Blink's TypingCommand rules): typing joins the open typing step;
    /// a Backspace only one of Backspaces; a Delete any typing step; word deletions and other edits
    /// start their own (typing ones stay open for more).
    /// </summary>
    void RecordUndo(UiElement field, string before, (int, int, bool) beforeSelection, string after, (int, int, bool) afterSelection, UndoKind kind, bool characterGranularity, bool typing)
    {
        var open = _openTyping is { } step && step.Field == field && _undo.Count > 0 && _undo[^1] == step ? step : null;
        var joins = open is not null && typing && kind switch
        {
            UndoKind.InsertText => true,
            UndoKind.DeleteKey => characterGranularity && open.Kind == UndoKind.DeleteKey,
            UndoKind.ForwardDeleteKey => characterGranularity,
            _ => false,
        };
        _redo.Clear();
        if (joins)
        {
            open!.AfterText = after;
            open.AfterSelection = afterSelection;
            return;
        }
        var created = new UndoStep { Field = field, BeforeText = before, BeforeSelection = beforeSelection, AfterText = after, AfterSelection = afterSelection, Kind = kind };
        _undo.Add(created);
        // Blink's UndoStack keeps 1000 steps.
        if (_undo.Count > 1000)
            _undo.RemoveAt(0);
        _openTyping = typing ? created : null;
    }

    /// <summary>Undo (or redo): historyUndo/historyRedo beforeinput and input around putting the field back.</summary>
    bool UndoOrRedo(UiElement focused, bool redo)
    {
        var stack = redo ? _redo : _undo;
        if (stack.Count == 0)
            return false;
        var inputType = redo ? "historyRedo" : "historyUndo";
        if (!focused.DispatchEvent(new UiInputEvent("beforeinput", cancelable: true) { InputType = inputType, IsTrusted = true }))
            return true;
        if (stack.Count == 0)
            return true;
        var step = stack[^1];
        stack.RemoveAt(stack.Count - 1);
        (redo ? _undo : _redo).Add(step);
        CloseTyping();
        var field = step.Field;
        if (field.Document != this)
            return true;
        var (text, selection) = redo ? (step.AfterText, step.AfterSelection) : (step.BeforeText, step.BeforeSelection);
        var changed = field.EditorText != text;
        field.SetEditorText(text);
        field.SetEditorSelection(selection.Start, selection.End, selection.Backward);
        if (changed)
            field.DispatchEvent(new UiInputEvent("input", cancelable: false) { InputType = inputType, IsTrusted = true });
        return true;
    }

    // ---------- Edits ----------

    /// <summary>
    /// The user replaces [start, end) of a field's text with <paramref name="insert"/>: beforeinput,
    /// the field's filters on inserted text, the change, input, the caret after it. Returns false
    /// when nothing at all happened (no beforeinput either).
    /// </summary>
    bool UserEdit(UiElement field, string inputType, string? data, int start, int end, string insert, UndoKind kind, bool characterGranularity = true, bool typing = true)
    {
        if (!field.DispatchEvent(new UiInputEvent("beforeinput", cancelable: true) { InputType = inputType, Data = data, IsTrusted = true }))
            return true;
        if (field.Document != this)
            return true;
        var before = field.EditorText;
        start = Math.Clamp(start, 0, before.Length);
        end = Math.Clamp(end, start, before.Length);
        if (insert.Length > 0)
            insert = FilterInsertion(field, before, start, end, insert);
        if (insert.Length == 0 && start == end)
            return true;
        var after = before[..start] + insert + before[end..];
        var beforeSelection = field.EditorSelection;
        var caret = start + insert.Length;
        RecordUndo(field, before, beforeSelection, after, (caret, caret, false), kind, characterGranularity, typing);
        field.SetEditorText(after);
        field.SetUserHasEditedTheField();
        field.SetEditorSelection(caret, caret, false);
        // Editor::RevealSelectionAfterEditingOperation.
        RevealSelection(field, SelectionReveal.IfNeeded);
        field.DispatchEvent(new UiInputEvent("input", cancelable: false) { InputType = inputType, Data = data, IsTrusted = true });
        return true;
    }

    /// <summary>
    /// Blink's BeforeTextInserted handling: a number field keeps only what can be part of a number
    /// where it goes; a one-line field turns line breaks into spaces (trailing ones dropped); maxlength
    /// cuts what doesn't fit (never half a surrogate pair).
    /// </summary>
    static string FilterInsertion(UiElement field, string text, int start, int end, string insert)
    {
        if (field.InputType == "number")
            insert = NumberFilter(text[..start], text[end..], insert);
        if (field.Tag == "textarea")
        {
            if (field.MaxLength is >= 0 and var maxArea)
            {
                var baseLength = text.Length - (end - start);
                insert = LimitLength(insert, maxArea > baseLength ? maxArea - baseLength : 0);
            }
            return insert;
        }
        var length = insert.Length;
        while (length > 0 && insert[length - 1] is '\r' or '\n')
            length--;
        insert = insert[..length].Replace("\r\n", " ").Replace('\r', ' ').Replace('\n', ' ');
        var max = field.MaxLength >= 0 ? field.MaxLength : int.MaxValue;
        var remaining = text.Length - (end - start);
        return LimitLength(insert, max > remaining ? max - remaining : 0);
    }

    static string LimitLength(string text, int max)
    {
        var length = Math.Min(max, text.Length);
        if (length == text.Length)
            return text;
        if (length > 0 && char.IsHighSurrogate(text[length - 1]))
            length--;
        return text[..length];
    }

    /// <summary>NumberInputType::HandleBeforeTextInsertedEvent with the en-US locale's single-character filtering.</summary>
    static string NumberFilter(string left, string right, string insert)
    {
        static bool IsE(char c) => c is 'e' or 'E';
        static bool IsSign(char c) => c is '+' or '-';
        static bool IsDecimal(char c) => c == '.';
        static bool HasTwoSigns(string s)
        {
            var first = s.IndexOfAny(['+', '-']);
            return first >= 0 && s.IndexOfAny(['+', '-'], first + 1) >= 0;
        }
        static bool HasSignNotAfterE(string s)
        {
            var position = s.IndexOfAny(['+', '-']);
            return position >= 0 && (position == 0 || !IsE(s[position - 1]));
        }
        var result = new System.Text.StringBuilder();
        foreach (var c in insert)
        {
            if (!"0123456789.Ee-+".Contains(c))
                continue;
            if (IsDecimal(c))
            {
                if (left.Contains('.') || right.Contains('.') || left.Any(IsE) || HasSignNotAfterE(right))
                    continue;
            }
            else if (IsE(c))
            {
                if (left.Any(IsE) || right.Any(IsE) || right.Contains('.'))
                    continue;
            }
            else if (IsSign(c))
            {
                var both = left + right;
                if (HasTwoSigns(both) || (both.Any(IsE) && !(left.Length == 0 || IsE(left[^1])))
                    || (right.Length > 0 && IsSign(right[0])) || (c == '+' && both.Any(IsE) && left.Length == 0))
                    continue;
            }
            else if (char.IsAsciiDigit(c))
            {
                if ((left.Length == 0 && right.Length > 0 && IsSign(right[0])) || (left.Length > 0 && IsE(left[^1]) && right.Length > 0 && IsSign(right[0])))
                    continue;
            }
            left += c;
            result.Append(c);
        }
        return result.ToString();
    }

    // ---------- Keys ----------

    /// <summary>
    /// A keydown in a focused text field: the editing command its key binding names (Blink's
    /// kKeyboardCodeKeyDownEntries for Linux). Returns whether a command ran (then the page does
    /// nothing more with the key).
    /// </summary>
    internal bool EditorKeyDown(UiElement field, UiKeyboardEvent e)
    {
        var command = CommandFor(e);
        return command is not null && ExecuteCommand(field, command);
    }

    /// <summary>A keypress in a focused text field: a character is typed (or a textarea's Enter starts a new line).</summary>
    internal bool EditorKeyPress(UiElement field, UiKeyboardEvent e)
    {
        if (!IsEditable(field) || e.CtrlKey || e.AltKey || e.MetaKey)
            return false;
        if (e.Key == "Enter")
        {
            if (field.Tag != "textarea")
                return false;
            var (start, end, _) = field.EditorSelection;
            return UserEdit(field, "insertLineBreak", null, start, end, "\n", UndoKind.InsertText);
        }
        if (e.Key.Length == 0 || char.IsControl(e.Key[0]))
            return false;
        var (from, to, _) = field.EditorSelection;
        return UserEdit(field, "insertText", e.Key, from, to, e.Key, UndoKind.InsertText);
    }

    static string? CommandFor(UiKeyboardEvent e)
    {
        var ctrl = e.CtrlKey;
        var shift = e.ShiftKey;
        if (e.AltKey || e.MetaKey)
            return null;
        var key = e.Key.Length == 1 ? e.Key.ToLowerInvariant() : e.Key;
        return (key, ctrl, shift) switch
        {
            ("ArrowLeft", false, false) => "MoveLeft",
            ("ArrowLeft", false, true) => "MoveLeftAndModifySelection",
            ("ArrowLeft", true, false) => "MoveWordLeft",
            ("ArrowLeft", true, true) => "MoveWordLeftAndModifySelection",
            ("ArrowRight", false, false) => "MoveRight",
            ("ArrowRight", false, true) => "MoveRightAndModifySelection",
            ("ArrowRight", true, false) => "MoveWordRight",
            ("ArrowRight", true, true) => "MoveWordRightAndModifySelection",
            ("ArrowUp", false, false) => "MoveUp",
            ("ArrowUp", false, true) => "MoveUpAndModifySelection",
            ("ArrowUp", true, false) => "MoveParagraphBackward",
            ("ArrowUp", true, true) => "MoveParagraphBackwardAndModifySelection",
            ("ArrowDown", false, false) => "MoveDown",
            ("ArrowDown", false, true) => "MoveDownAndModifySelection",
            ("ArrowDown", true, false) => "MoveParagraphForward",
            ("ArrowDown", true, true) => "MoveParagraphForwardAndModifySelection",
            ("PageUp", false, false) => "MovePageUp",
            ("PageUp", false, true) => "MovePageUpAndModifySelection",
            ("PageDown", false, false) => "MovePageDown",
            ("PageDown", false, true) => "MovePageDownAndModifySelection",
            ("Home", false, false) => "MoveToBeginningOfLine",
            ("Home", false, true) => "MoveToBeginningOfLineAndModifySelection",
            ("Home", true, false) => "MoveToBeginningOfDocument",
            ("Home", true, true) => "MoveToBeginningOfDocumentAndModifySelection",
            ("End", false, false) => "MoveToEndOfLine",
            ("End", false, true) => "MoveToEndOfLineAndModifySelection",
            ("End", true, false) => "MoveToEndOfDocument",
            ("End", true, true) => "MoveToEndOfDocumentAndModifySelection",
            ("Backspace", false, _) => "DeleteBackward",
            ("Backspace", true, false) => "DeleteWordBackward",
            ("Backspace", true, true) => "DeleteToBeginningOfLine",
            ("Delete", false, false) => "DeleteForward",
            ("Delete", true, false) => "DeleteWordForward",
            ("Delete", false, true) => "Cut",
            ("Insert", true, false) => "Copy",
            ("Insert", false, true) => "Paste",
            ("c", true, false) => "Copy",
            ("v", true, false) => "Paste",
            ("v", true, true) => "PasteAndMatchStyle",
            ("x", true, false) => "Cut",
            ("a", true, false) => "SelectAll",
            ("z", true, false) => "Undo",
            ("z", true, true) => "Redo",
            ("y", true, false) => "Redo",
            _ => null,
        };
    }

    /// <summary>Runs an editing command in a field; false when it doesn't apply (not enabled).</summary>
    bool ExecuteCommand(UiElement field, string command)
    {
        var editable = IsEditable(field);
        var text = field.EditorText;
        var (start, end, _) = field.EditorSelection;
        var (anchor, focus) = field.EditorAnchorAndFocus;
        var range = start != end;
        switch (command)
        {
            case "SelectAll":
                CloseTyping();
                field.SetEditorSelection(0, text.Length, false, byUser: true, reveal: SelectionReveal.IfNeeded);
                return true;
            case "Undo" or "Redo":
                return editable && UndoOrRedo(field, command == "Redo");
            case "Copy":
                if (!range || field.InputType == "password")
                    return false;
                if (field.DispatchEvent(new UiClipboardEvent("copy") { IsTrusted = true }))
                    Clipboard.WriteText(text[start..end]);
                return true;
            case "Cut":
                if (!range || !editable || field.InputType == "password")
                    return false;
                if (!field.DispatchEvent(new UiClipboardEvent("cut") { IsTrusted = true }))
                    return true;
                Clipboard.WriteText(text[start..end]);
                CloseTyping();
                UserEdit(field, "deleteByCut", null, start, end, "", UndoKind.Other, typing: false);
                return true;
            case "Paste" or "PasteAndMatchStyle":
            {
                if (!editable)
                    return false;
                var pasted = Clipboard.ReadText();
                if (!field.DispatchEvent(new UiClipboardEvent("paste") { Data = pasted, IsTrusted = true }) || string.IsNullOrEmpty(pasted))
                    return true;
                CloseTyping();
                UserEdit(field, "insertFromPaste", pasted, start, end, pasted, UndoKind.Other, typing: false);
                return true;
            }
        }
        if (command.StartsWith("Delete", StringComparison.Ordinal))
        {
            if (!editable)
                return false;
            switch (command)
            {
                case "DeleteBackward":
                    if (range)
                        return UserEdit(field, "deleteContentBackward", null, start, end, "", UndoKind.DeleteKey);
                    if (start == 0)
                        return true;
                    return UserEdit(field, "deleteContentBackward", null, TextBoundaries.BackspaceStart(text, start), start, "", UndoKind.DeleteKey);
                case "DeleteForward":
                    if (range)
                        return UserEdit(field, "deleteContentForward", null, start, end, "", UndoKind.ForwardDeleteKey);
                    if (start == text.Length)
                        return true;
                    return UserEdit(field, "deleteContentForward", null, start, TextBoundaries.NextGrapheme(text, start), "", UndoKind.ForwardDeleteKey);
                case "DeleteWordBackward":
                    if (range)
                        return UserEdit(field, "deleteWordBackward", null, start, end, "", UndoKind.DeleteKey, characterGranularity: false);
                    return UserEdit(field, "deleteWordBackward", null, TextEditing.PreviousWordPosition(text, start), start, "", UndoKind.DeleteKey, characterGranularity: false);
                case "DeleteWordForward":
                    if (range)
                        return UserEdit(field, "deleteWordForward", null, start, end, "", UndoKind.ForwardDeleteKey, characterGranularity: false);
                    return UserEdit(field, "deleteWordForward", null, start, TextEditing.NextWordPosition(text, start), "", UndoKind.ForwardDeleteKey, characterGranularity: false);
                case "DeleteToBeginningOfLine":
                {
                    var lineStart = range ? start : LineStart(field, start);
                    return UserEdit(field, "deleteSoftLineBackward", null, lineStart, range ? end : start, "", UndoKind.DeleteKey, characterGranularity: false);
                }
            }
            return false;
        }
        if (!command.StartsWith("Move", StringComparison.Ordinal))
            return false;
        // Moving the caret: the moving end goes, the anchor stays when extending.
        var extend = command.EndsWith("AndModifySelection", StringComparison.Ordinal);
        var name = extend ? command[..^"AndModifySelection".Length] : command;
        int target;
        float? verticalX = null;
        switch (name)
        {
            case "MoveLeft":
                target = !extend && range ? start : TextBoundaries.PreviousGrapheme(text, focus);
                break;
            case "MoveRight":
                target = !extend && range ? end : TextBoundaries.NextGrapheme(text, focus);
                break;
            case "MoveWordLeft":
                target = TextEditing.PreviousWordPosition(text, extend ? focus : start);
                break;
            case "MoveWordRight":
                target = TextEditing.NextWordPosition(text, extend ? focus : end);
                break;
            case "MoveToBeginningOfLine":
                target = LineStart(field, extend ? focus : start);
                break;
            case "MoveToEndOfLine":
                target = LineEnd(field, extend ? focus : end);
                break;
            case "MoveToBeginningOfDocument" or "MoveParagraphBackward" when field.Tag != "textarea":
                target = 0;
                break;
            case "MoveToEndOfDocument" or "MoveParagraphForward" when field.Tag != "textarea":
                target = text.Length;
                break;
            case "MoveToBeginningOfDocument":
                target = 0;
                break;
            case "MoveToEndOfDocument":
                target = text.Length;
                break;
            case "MoveParagraphBackward":
                target = ParagraphStart(text, focus > 0 && text[focus - 1] == '\n' ? focus - 1 : focus);
                break;
            case "MoveParagraphForward":
                target = ParagraphEnd(text, focus < text.Length && text[focus] == '\n' ? focus + 1 : focus);
                break;
            case "MoveUp" or "MoveDown":
                target = VerticalTarget(field, extend ? focus : name == "MoveUp" ? start : end, name == "MoveUp", out verticalX);
                break;
            case "MovePageUp" or "MovePageDown":
                // A page move that finds no line to go to isn't handled (a one-line field's: the page scrolls).
                if (PageTarget(field, extend ? focus : name == "MovePageUp" ? start : end, name == "MovePageUp", out verticalX) is not { } page)
                    return false;
                target = page;
                break;
            default:
                return false;
        }
        CloseTyping();
        // A page move without Shift reveals the caret at the top (CursorAlignOnScroll::kAlways).
        var reveal = name is "MovePageUp" or "MovePageDown" && !extend ? SelectionReveal.Top : SelectionReveal.IfNeeded;
        if (extend)
            field.SetEditorSelection(Math.Min(anchor, target), Math.Max(anchor, target), target < anchor, byUser: true, reveal: reveal);
        else
            field.SetEditorSelection(target, target, false, byUser: true, reveal: reveal);
        // Up, down and page moves keep the x they went at.
        if (verticalX is { } kept)
            _verticalX = kept;
        return true;
    }

    static int ParagraphStart(string text, int offset) => offset <= 0 ? 0 : text.LastIndexOf('\n', offset - 1) + 1;

    static int ParagraphEnd(string text, int offset)
    {
        var next = text.IndexOf('\n', offset);
        return next < 0 ? text.Length : next;
    }

    /// <summary>Where the line holding an offset starts (a textarea's visual line; all of a one-line field).</summary>
    int LineStart(UiElement field, int offset)
    {
        if (field.Tag != "textarea" || Geometry(field) is not { } geometry || geometry.Layout.Lines.Count == 0)
            return 0;
        var line = geometry.Layout.LineOf(offset);
        return line < 0 ? 0 : geometry.Layout.CaretStops(line).Offsets[0];
    }

    int LineEnd(UiElement field, int offset)
    {
        if (field.Tag != "textarea" || Geometry(field) is not { } geometry || geometry.Layout.Lines.Count == 0)
            return field.EditorText.Length;
        var line = geometry.Layout.LineOf(offset);
        return line < 0 ? field.EditorText.Length : geometry.Layout.CaretStops(line).Offsets[^1];
    }

    /// <summary>
    /// Up and Down (Blink's line granularity): a textarea's caret goes to the line above or below at
    /// the x up and down keep (else the caret's), past the first or last line to the text's start or
    /// end; a one-line field's to its start or end.
    /// </summary>
    int VerticalTarget(UiElement field, int offset, bool up, out float? x)
    {
        x = null;
        if (field.Tag != "textarea" || Geometry(field) is not { } geometry || geometry.Layout.Lines.Count == 0)
            return up ? 0 : field.EditorText.Length;
        var layout = geometry.Layout;
        x = _verticalX ?? CaretX(geometry, offset);
        var target = layout.LineOf(offset) + (up ? -1 : 1);
        if (target < 0)
            return 0;
        if (target >= layout.Lines.Count)
            return field.EditorText.Length;
        return layout.OffsetInLine(target, x.Value - geometry.Origin.X);
    }

    /// <summary>The x of the caret before an offset (LineDirectionPointForBlockDirectionNavigation), viewport units.</summary>
    static float CaretX(FieldGeometry geometry, int offset) => MathF.Floor(geometry.CaretRect(offset).X);

    /// <summary>
    /// Page Up and Page Down (MoveCommands::ExecuteMovePage*, SelectionModifier::ModifyWithPageGranularity):
    /// in a scrollable field (a textarea) the caret goes line by line at the kept x as far as 87.5%
    /// of the field's (or the viewport's, if smaller) height takes it, measured between the caret's
    /// middles; null when that isn't a scroller or no line is in reach.
    /// </summary>
    int? PageTarget(UiElement field, int offset, bool up, out float? x)
    {
        x = null;
        if (field.Tag != "textarea" || field.Box is not { } box || box.Style.OverflowY is not (Overflow.Auto or Overflow.Scroll)
            || Geometry(field) is not { } geometry || geometry.Layout.Lines.Count == 0)
            return null;
        var height = Math.Min((int)box.PaddingBoxHeight, (int)Layout.Viewport.PaddingBoxHeight);
        var distance = (int)MathF.Max(height * 0.875f, 1);
        var layout = geometry.Layout;
        x = _verticalX ?? CaretX(geometry, offset);
        int CaretY(int at)
        {
            var rect = geometry.CaretRect(at);
            var top = (int)MathF.Floor(rect.Top);
            return top + ((int)MathF.Ceiling(rect.Bottom) - top) / 2;
        }
        var startY = up ? -CaretY(offset) : CaretY(offset);
        var lastY = startY;
        int? result = null;
        var position = offset;
        for (var line = layout.LineOf(offset); ; )
        {
            // Past the first or last line the next line position is the text's start or end (as for up and down).
            line += up ? -1 : 1;
            var next = line < 0 ? 0 : line >= layout.Lines.Count ? field.EditorText.Length : layout.OffsetInLine(line, x.Value - geometry.Origin.X);
            if (next == position)
                break;
            var nextY = up ? -CaretY(next) : CaretY(next);
            if (nextY - startY > distance)
                break;
            if (nextY >= lastY)
            {
                lastY = nextY;
                result = next;
            }
            position = next;
        }
        return result;
    }

    FieldGeometry? Geometry(UiElement field)
    {
        UpdateLayout();
        return field.Box is { } box ? FieldGeometry.Of(this, box, field) : null;
    }
}

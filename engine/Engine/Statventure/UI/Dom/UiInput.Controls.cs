using System.Globalization;
using System.Numerics;
using Statventure.Core;
using Statventure.UI.Css;
using Statventure.UI.Paint;
using Statventure.UI.Style;

namespace Statventure.UI.Dom;

/// <summary>
/// The parts of controls that Blink builds as shadow elements, driven as they are: the number
/// field's spin button (a press steps, holds repeat after 250 ms every 50 ms, the half under the
/// pointer decides the direction, the release commits with change; the wheel steps a focused
/// field; ArrowUp and ArrowDown step), the range slider (a press on the track jumps the thumb
/// there, dragging moves it, the release commits; arrows, Page Up/Down, Home and End), and the
/// search field's cancel button (a click clears the field; so does Escape).
/// </summary>
public sealed partial class UiInput
{
    /// <summary>Blink's ScrollbarTheme autoscroll timer: the first repeat after 250 ms, then every 50 ms.</summary>
    const double SpinInitialDelay = 0.25, SpinRepeatDelay = 0.05;

    enum ControlPart { None, SpinButton, RangeThumb, RangeTrack, CancelButton }

    UiElement? _spinOwner;
    bool _spinUp;
    bool _spinRecalc;
    double _spinRepeatAt;
    UiElement? _rangeDrag;
    UiElement? _cancelPress;

    /// <summary>The point in the element's own (untransformed) space.</summary>
    static Vector2 LocalPoint(UiElement element, Vector2 position)
    {
        if (element.Box is not { } box || !Matrix3x2.Invert(UiDocument.TransformOf(box), out var inverse))
            return position;
        return Vector2.Transform(position, inverse);
    }

    /// <summary>The control part at a point (viewport units): parts hidden or with pointer-events none don't count.</summary>
    ControlPart PartAt(UiElement element, Vector2 position)
    {
        if (element.Tag != "input" || element.Box is not { } box)
            return ControlPart.None;
        var local = LocalPoint(element, position);
        switch (element.InputType)
        {
            case "number":
            {
                var part = element.Styles?.Part(PseudoElement.InnerSpinButton);
                if (part is null || part.UsedPointerEvents == PointerEvents.None || part.Visibility != Visibility.Visible)
                    return ControlPart.None;
                return UiPainter.SpinButtonOf(box, element) is { } spin && spin.Contains(local) ? ControlPart.SpinButton : ControlPart.None;
            }
            case "search":
            {
                var part = element.Styles?.Part(PseudoElement.SearchCancelButton);
                // The button is hidden (visibility) while the field is empty.
                if (part is null || part.UsedPointerEvents == PointerEvents.None || element.Value.Length == 0)
                    return ControlPart.None;
                return UiPainter.CancelButtonOf(box, element) is { } cancel && cancel.Button.Contains(local) ? ControlPart.CancelButton : ControlPart.None;
            }
            case "range":
                return UiPainter.RangeThumb(box, element).Contains(local) ? ControlPart.RangeThumb : ControlPart.RangeTrack;
            default:
                return ControlPart.None;
        }
    }

    // ---------- Mouse ----------

    /// <summary>A mousedown reached an input uncancelled (its parts' and its own default handlers).</summary>
    internal void ControlMouseDown(UiElement element, UiMouseEvent e)
    {
        if (e.Button != 0 || element.Tag != "input")
            return;
        switch (PartAt(element, e.LayoutPosition))
        {
            case ControlPart.SpinButton when !element.IsDisabled && !element.ReadOnly:
            {
                // FocusAndSelectSpinButtonOwner, then a step and the repeat timer; the button holds the pointer.
                Focus(element, FocusOrigin.Script);
                if (_spinRecalc || _spinOwner is null)
                {
                    _spinRecalc = false;
                    _spinUp = SpinUpAt(element, e.LayoutPosition);
                }
                _spinRepeatAt = Now + SpinInitialDelay;
                _spinOwner = element;
                element.StepFromUserWithEvents(_spinUp ? 1 : -1);
                SetPointerCapture(element);
                RefreshPartStates();
                e.SetDefaultHandled();
                break;
            }
            case ControlPart.RangeThumb when !element.IsDisabled:
                // The thumb starts a drag where it is.
                _rangeDrag = element;
                _thumbPressed = true;
                SetPointerCapture(element);
                RefreshPartStates();
                break;
            case ControlPart.RangeTrack when !element.IsDisabled:
                // A press on the track drags the thumb from there (DragFrom).
                _rangeDrag = element;
                SetPointerCapture(element);
                SetRangeFromPoint(element, e.LayoutPosition);
                RefreshPartStates();
                break;
            case ControlPart.CancelButton when !element.IsDisabled && !element.ReadOnly:
                _cancelPress = element;
                RefreshPartStates();
                break;
        }
    }

    /// <summary>The pointer moved over (or while held by) an input: the spin button's half follows it, a dragged thumb follows it.</summary>
    internal void ControlMouseMove(UiElement element, UiMouseEvent e)
    {
        if (element.Tag != "input")
            return;
        if (_rangeDrag == element)
            SetRangeFromPoint(element, e.LayoutPosition);
        if (element.InputType == "number" && !element.IsDisabled && !element.ReadOnly && (_spinOwner == element || PartAt(element, e.LayoutPosition) == ControlPart.SpinButton))
            _spinUp = SpinUpAt(element, e.LayoutPosition);
        RefreshPartStates();
    }

    /// <summary>The main button went up: a held spin button or dragged thumb lets go and commits (change).</summary>
    internal void ControlMouseUp(UiElement element, UiMouseEvent e)
    {
        if (e.Button != 0)
            return;
        if (_spinOwner == element)
            ReleaseSpinButton(dispatch: true);
        if (_rangeDrag == element)
            StopRangeDrag(dispatch: true);
    }

    /// <summary>A click on the search field's cancel button empties it (input while focused, change otherwise) and fires search.</summary>
    internal void ControlClick(UiElement element, UiMouseEvent e)
    {
        if (e.Button != 0 || element.InputType != "search" || element.IsDisabled || element.ReadOnly)
            return;
        if (PartAt(element, e.LayoutPosition) != ControlPart.CancelButton)
            return;
        SetValueForUser(element, "");
        element.DispatchEvent(new UiEvent("search", bubbles: true, cancelable: false) { IsTrusted = true });
        e.SetDefaultHandled();
    }

    /// <summary>A wheel turn over a focused, editable number field steps it (one step per event, by the vertical direction).</summary>
    internal void ControlWheel(UiElement element, UiWheelEvent e)
    {
        if (element.InputType != "number" || element.IsDisabled || element.ReadOnly || !element.IsFocused || element.Box is null)
            return;
        // Blink steps by wheelDeltaY: positive (up) steps up.
        var amount = -e.DeltaY;
        if (amount == 0)
            return;
        element.StepFromUserWithEvents(amount > 0 ? 1 : -1);
        e.SetDefaultHandled();
    }

    /// <summary>The upper half of the spin button is under the point (Blink rounds the point to whole pixels).</summary>
    static bool SpinUpAt(UiElement element, Vector2 position)
    {
        if (element.Box is not { } box || UiPainter.SpinButtonOf(box, element) is not { } spin)
            return false;
        var local = LocalPoint(element, position);
        var y = MathF.Round(local.Y - spin.Y, MidpointRounding.AwayFromZero);
        return y < spin.Height / 2;
    }

    void ReleaseSpinButton(bool dispatch)
    {
        if (_spinOwner is not { } owner)
            return;
        _spinOwner = null;
        ReleasePointerCapture(owner);
        RefreshPartStates();
        if (dispatch)
            owner.DispatchFormControlChangeEvent();
    }

    void StopRangeDrag(bool dispatch)
    {
        if (_rangeDrag is not { } range)
            return;
        _rangeDrag = null;
        ReleasePointerCapture(range);
        RefreshPartStates();
        if (dispatch)
            range.DispatchFormControlChangeEvent();
    }

    /// <summary>
    /// Blink's SliderThumbElement::SetPositionFromPoint: the thumb's centre goes under the point
    /// (in layout units), clamped to the track; the value is that share of the range, clamped
    /// and rounded to the step. A new value fires input.
    /// </summary>
    void SetRangeFromPoint(UiElement element, Vector2 position)
    {
        if (element.Box is not { } box)
            return;
        var thumb = UiPainter.RangeThumb(box, element);
        var rect = box.ViewportRect;
        var trackX = rect.X + box.ContentLeft;
        var trackSize = Layout.LayoutUnit.Truncate(box.ContentWidth - thumb.Width);
        var local = LocalPoint(element, position);
        // PhysicalOffset::FromPointFFloor, then the thumb's half width off.
        var point = MathF.Floor((local.X - trackX) * 64) / 64;
        var half = MathF.Floor(thumb.Width * 64 / 2) / 64;
        var at = MathF.Max(0, MathF.Min(point - half, trackSize));
        var range = element.StepRangeOf(rejectAny: true);
        // Decimal::FromDouble keeps the double's shortest form.
        var ratio = trackSize <= 0 ? 0m : decimal.Parse(((double)at / trackSize).ToString("R", CultureInfo.InvariantCulture), NumberStyles.Float, CultureInfo.InvariantCulture);
        var value = range.Clamp(range.Minimum + ratio * (range.Maximum - range.Minimum));
        var text = UiElement.Serialize(value);
        if (text == element.Value)
            return;
        element.SetValueInternal(text, userEdit: true);
        element.DispatchInputEvent();
    }

    /// <summary>SetValueForUser: a text field's value changed by a control (input while focused, else change).</summary>
    static void SetValueForUser(UiElement element, string value)
    {
        var before = element.Value;
        element.SetValueInternal(value, userEdit: true);
        if (element.Value == before)
            return;
        element.ValueReplaced();
        if (element.IsFocused)
            element.DispatchInputEvent();
        else
            element.DispatchFormControlChangeEvent();
    }

    /// <summary>The spin button repeats while held (it steps by the half under the pointer now).</summary>
    void AdvanceControls(double time)
    {
        if (_spinOwner is not { } owner)
            return;
        if (!InDocument(owner) || owner.IsDisabled || owner.ReadOnly)
        {
            ReleaseSpinButton(dispatch: InDocument(owner));
            return;
        }
        while (time >= _spinRepeatAt && _spinOwner == owner)
        {
            _spinRepeatAt += SpinRepeatDelay;
            owner.StepFromUserWithEvents(_spinUp ? 1 : -1);
        }
    }

    /// <summary>The painted part states: hovered and pressed spin button and its half, hovered and active thumb, pressed cancel button.</summary>
    void RefreshPartStates()
    {
        var hovered = _hovered is { Tag: "input" } input && InDocument(input) && PointerInside ? input : null;
        var part = hovered is null ? ControlPart.None : PartAt(hovered, PointerPosition);
        _document.SpinButtonHovered = _spinOwner ?? (part == ControlPart.SpinButton ? hovered : null);
        _document.SpinButtonPressed = _spinOwner;
        _document.SpinButtonUpHalf = _spinUp;
        _document.RangeThumbHovered = _rangeDrag ?? (part == ControlPart.RangeThumb ? hovered : null);
        _document.RangeThumbActive = _thumbPressed ? _rangeDrag : null;
        _document.CancelButtonPressed = _cancelPress;
    }

    bool _thumbPressed;

    /// <summary>The main button went up anywhere: pressed parts are released.</summary>
    void ReleaseParts()
    {
        _cancelPress = null;
        _thumbPressed = false;
        RefreshPartStates();
    }

    /// <summary>Focus came to an element: a number field's spin button picks its half anew on the next press.</summary>
    void PartsFocused(UiElement element)
    {
        if (element.InputType == "number")
            _spinRecalc = true;
    }

    /// <summary>Focus left an element: its held spin button lets go (with change), as Blink's HandleBlurEvent.</summary>
    void PartsBlurred(UiElement element)
    {
        // A select losing focus closes its list (as cancelled).
        if (_popupSelect == element)
            CloseSelectPopup(commit: false);
        if (_spinOwner == element)
            ReleaseSpinButton(dispatch: true);
    }

    // ---------- Keys ----------

    /// <summary>
    /// Keys on number and range inputs (and Escape in a search field): the number steps with
    /// ArrowUp/ArrowDown (input, then change); the range moves by its step (arrows), a tenth of
    /// the range (Page Up/Down) or to its ends (Home, End), firing input and change.
    /// </summary>
    bool ControlKeyDown(UiElement element, UiKeyboardEvent e)
    {
        if (element.Tag != "input")
            return false;
        switch (element.InputType)
        {
            case "number":
            {
                if (element.IsDisabled || element.ReadOnly)
                    return false;
                int amount;
                if (e.Key == "ArrowUp")
                    amount = 1;
                else if (e.Key == "ArrowDown" && !e.AltKey)
                    amount = -1;
                else
                    return false;
                element.StepFromUserWithEvents(amount);
                element.DispatchFormControlChangeEvent();
                e.SetDefaultHandled();
                return true;
            }
            case "range":
            {
                if (element.IsDisabled)
                    return false;
                var range = element.StepRangeOf(rejectAny: true);
                var current = UiElement.ParseNumber(element.Value) ?? range.DefaultValue;
                var any = string.Equals(element.GetAttribute("step")?.Trim(), "any", StringComparison.OrdinalIgnoreCase);
                var step = any ? (range.Maximum - range.Minimum) / 100 : range.Step;
                var bigStep = Math.Max((range.Maximum - range.Minimum) / 10, step);
                decimal next;
                switch (e.Key)
                {
                    case "ArrowUp" or "ArrowRight":
                        next = current + step;
                        break;
                    case "ArrowDown" or "ArrowLeft":
                        next = current - step;
                        break;
                    case "PageUp":
                        next = current + bigStep;
                        break;
                    case "PageDown":
                        next = current - bigStep;
                        break;
                    case "Home":
                        next = range.Minimum;
                        break;
                    case "End":
                        next = range.Maximum;
                        break;
                    default:
                        return false;
                }
                next = range.Clamp(next);
                if (next != current)
                {
                    var before = element.Value;
                    element.SetValueInternal(UiElement.Serialize(next), userEdit: true);
                    if (element.Value != before)
                    {
                        element.DispatchInputEvent();
                        element.DispatchFormControlChangeEvent();
                    }
                }
                e.SetDefaultHandled();
                return true;
            }
            case "search" when e.Key == "Escape" && !element.IsDisabled && !element.ReadOnly && element.EditorText.Length > 0:
                SetValueForUser(element, "");
                element.DispatchEvent(new UiEvent("search", bubbles: true, cancelable: false) { IsTrusted = true });
                e.SetDefaultHandled();
                return true;
            default:
                return false;
        }
    }
}

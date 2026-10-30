using System.Numerics;
using Statventure.Platform;
using Statventure.UI.Layout;
using Statventure.UI.Style;

namespace Statventure.UI.Dom;

/// <summary>A cursor image to show: its url and hotspot (image pixels from its top left).</summary>
public sealed record UiCursorImage(string Url, Vector2 Hotspot);

/// <summary>How focus moved: by script, by a click, or by the keyboard (decides :focus-visible).</summary>
public enum FocusOrigin
{
    Script,
    Pointer,
    Keyboard,
}

/// <summary>
/// A document's input, as Chromium handles it: the pointer (hit testing, :hover and :active,
/// pointer, mouse and boundary events, clicks and double clicks, pointer capture), focus
/// (focus events, :focus-visible, Tab navigation) and the keyboard (key events at the focused
/// element, Enter and Space activating buttons). The window host feeds it; tests can too.
/// Positions are in viewport units.
/// </summary>
public sealed partial class UiInput
{
    readonly UiDocument _document;
    readonly List<UiElement> _hoverChain = [];
    readonly List<UiElement> _activeChain = [];
    readonly UiElement?[] _downTargets = new UiElement?[5];
    UiElement? _hovered;
    UiElement? _capture;
    int _buttons;
    bool _suppressMouse;
    double _lastDownTime = double.NegativeInfinity;
    Vector2 _lastDownPosition;
    // Where the pointer is in window units (DIPs), for movement and double clicks.
    Vector2 _pointerDip;
    int _lastDownButton = -1;
    int _clickCount;
    // Blink's :focus-visible inputs: how the user last moved focus, and whether a key went down since a click focused something.
    FocusOrigin? _lastUserFocus;
    bool _hadKeyboardEvent;
    readonly HashSet<UiElement> _hoverSet = [];
    readonly HashSet<UiElement> _activeSet = [];

    internal UiInput(UiDocument document)
    {
        _document = document;
    }

    /// <summary>Where the pointer is (layout units: device pixels), and whether it is over the viewport.</summary>
    public Vector2 PointerPosition { get; private set; }
    public bool PointerInside { get; private set; }
    /// <summary>The element under the pointer (or holding the pointer capture).</summary>
    public UiElement? Hovered => _hovered;
    /// <summary>The CSS cursor to show (default, pointer, text, not-allowed, crosshair, grabbing ...).</summary>
    public string Cursor { get; private set; } = "default";
    /// <summary>The image to show instead of <see cref="Cursor"/> (cursor: url(...)): its url and hotspot; null for the keyword's cursor.</summary>
    public UiCursorImage? CursorImage { get; private set; }
    /// <summary>Two presses closer than this (seconds, GTK's default) and <see cref="DoubleClickDistance"/> make a double click.</summary>
    public double DoubleClickTime { get; set; } = 0.4;
    public float DoubleClickDistance { get; set; } = 5;

    double Now => _document.Animations.Time;

    static int ButtonOf(MouseButton button) => button switch
    {
        MouseButton.Left => 0,
        MouseButton.Middle => 1,
        MouseButton.Right => 2,
        MouseButton.Back => 3,
        _ => 4,
    };

    static int MaskOf(int button) => button switch
    {
        0 => 1,
        1 => 4,
        2 => 2,
        3 => 8,
        _ => 16,
    };

    T Mouse<T>(T e) where T : UiEvent
    {
        e.IsTrusted = true;
        return e;
    }

    /// <summary>A layout position (device pixels) in the DOM's CSS pixels.</summary>
    Vector2 ToCss(Vector2 position) => position / _document.Zoom;

    UiPointerEvent PointerEvent(string type, Vector2 position, int button, KeyModifiers modifiers, bool bubbles = true, bool cancelable = true, UiElement? related = null, Vector2 movement = default) =>
        Mouse(new UiPointerEvent(type, bubbles, cancelable)
        {
            Position = ToCss(position),
            LayoutPosition = position,
            Movement = movement,
            Button = button,
            Buttons = _buttons,
            ShiftKey = modifiers.HasFlag(KeyModifiers.Shift),
            CtrlKey = modifiers.HasFlag(KeyModifiers.Control),
            AltKey = modifiers.HasFlag(KeyModifiers.Alt),
            MetaKey = modifiers.HasFlag(KeyModifiers.Super),
            RelatedTarget = related,
        });

    UiMouseEvent MouseEvent(string type, Vector2 position, int button, KeyModifiers modifiers, int detail = 0, bool bubbles = true, bool cancelable = true, UiElement? related = null, Vector2 movement = default) =>
        Mouse(new UiMouseEvent(type, bubbles, cancelable)
        {
            Position = ToCss(position),
            LayoutPosition = position,
            Movement = movement,
            Button = button,
            Buttons = _buttons,
            ShiftKey = modifiers.HasFlag(KeyModifiers.Shift),
            CtrlKey = modifiers.HasFlag(KeyModifiers.Control),
            AltKey = modifiers.HasFlag(KeyModifiers.Alt),
            MetaKey = modifiers.HasFlag(KeyModifiers.Super),
            Detail = detail,
            RelatedTarget = related,
        });

    bool InDocument(UiElement element) => element.Document == _document;

    /// <summary>The element events go to at a point: the one hit, or the root over the viewport's scrollbars (as in Chromium).</summary>
    UiElement? TargetAt(HitResult? hit, Vector2 position) =>
        hit?.Element ?? (_document.OverViewportScrollbars(position) ? _document.Root : null);

    // ---------- Pointer ----------

    /// <summary>
    /// The pointer moved (or the content under it did) to <paramref name="position"/> in window
    /// units (DIPs). The page works in device pixels (× the device scale); its events say CSS
    /// pixels, and movement in whole window units (Blink's truncated screen positions).
    /// </summary>
    public void PointerMove(Vector2 position, KeyModifiers modifiers = KeyModifiers.None)
    {
        var movement = PointerInside ? Truncate(position) - Truncate(_pointerDip) : Vector2.Zero;
        _pointerDip = position;
        position *= _document.DeviceScale;
        PointerPosition = position;
        PointerInside = true;
        // The window sees the move before the page (tooltips).
        TooltipPointerMoved(position);
        // A pressed scrollbar takes the moves (the page doesn't see them).
        if (_scrollbarPress is { } press)
        {
            MoveScrollbar(press);
            return;
        }
        // An open select list is a window of its own.
        if (_buttons == 0 && PopupPointerMove(position))
        {
            PageTooltipUnderCursor(position, _document.HitTest(position));
            return;
        }
        ProcessPendingPointerCapture();
        var hit = _document.HitTest(position);
        var target = _capture is { } captured && InDocument(captured) ? captured : TargetAt(hit, position);
        // Resizing follows the pointer before the page sees the move.
        if (_resizing is not null && _buttons != 0)
            Resize(position);
        UpdateHover(target, position, modifiers);
        UpdateCursor(hit, position);
        UpdateScrollbarHover(position);
        RefreshPartStates();
        if (target is not null)
        {
            target.DispatchEvent(PointerEvent("pointermove", position, -1, modifiers, movement: movement));
            if (!_suppressMouse && target.DispatchEvent(MouseEvent("mousemove", position, 0, modifiers, movement: movement)) && (_buttons & 1) != 0)
            {
                FieldDrag(position);
                PageDrag(position);
            }
        }
        // Then the page passes on the tooltip text under the pointer.
        PageTooltipUnderCursor(position, hit);
    }

    /// <summary>The pointer left the window.</summary>
    public void PointerLeave()
    {
        PointerInside = false;
        SetTooltipObserved(false);
        ClearPageTooltip();
        if (_scrollbarPress is null)
            _document.ScrollbarHover = null;
        if (_capture is null)
            UpdateHover(null, PointerPosition, KeyModifiers.None);
    }

    static Vector2 Truncate(Vector2 v) => new(MathF.Truncate(v.X), MathF.Truncate(v.Y));

    /// <summary>A button went down at <paramref name="position"/> (window units, DIPs).</summary>
    public void PointerDown(Vector2 position, MouseButton platformButton, KeyModifiers modifiers = KeyModifiers.None)
    {
        _pointerDip = position;
        position *= _document.DeviceScale;
        TooltipPointerPressed();
        if (PopupPointerDown(position, ButtonOf(platformButton)))
            return;
        ProcessPendingPointerCapture();
        PressCore(position, platformButton, modifiers);
        // (A select list closed by this press only kept its select from reopening during it.)
        _popupJustClosed = null;
        // A capture taken while handling the press starts now (gotpointercapture).
        ProcessPendingPointerCapture();
    }

    void PressCore(Vector2 position, MouseButton platformButton, KeyModifiers modifiers)
    {
        PointerPosition = position;
        PointerInside = true;
        var button = ButtonOf(platformButton);
        var hit = _document.HitTest(position);
        var target = _capture is { } captured && InDocument(captured) ? captured : TargetAt(hit, position);
        UpdateHover(target, position, modifiers);
        var first = _buttons == 0;
        _buttons |= MaskOf(button);
        // A main-button press on a scrollbar is the scrollbar's (after the page's events).
        var scrollbar = button == 0 && first && _capture is null && _scrollbarPress is null ? _document.ScrollbarAt(position) : null;
        if (scrollbar is { } over && (!over.Geometry.Enabled || over.Part == ScrollbarPart.None))
            scrollbar = null;
        if (target is null)
        {
            if (scrollbar is { } bare)
                PressScrollbar(bare, modifiers);
            return;
        }
        _mousePressNode = target;
        // Clicks in a row: same button, quickly, close by.
        // (The distance is the window's, in window units.)
        if (button == _lastDownButton && Now - _lastDownTime <= DoubleClickTime && Vector2.Distance(_pointerDip, _lastDownPosition) <= DoubleClickDistance)
            _clickCount++;
        else
            _clickCount = 1;
        _lastDownTime = Now;
        _lastDownPosition = _pointerDip;
        _lastDownButton = button;
        _downTargets[button] = target;
        // :active comes with the press itself, before any event (a cancelled press is still active).
        if (button == 0)
            SetActive(target);

        // The first button down is a pointerdown; more buttons are a pointermove (chorded buttons).
        var pointerAllowed = target.DispatchEvent(PointerEvent(first ? "pointerdown" : "pointermove", position, button, modifiers));
        if (first)
            _suppressMouse = !pointerAllowed;
        if (_suppressMouse)
        {
            if (scrollbar is { } suppressed)
                PressScrollbar(suppressed, modifiers);
            return;
        }
        var wasFocused = _document.FocusedElement == target;
        var mousedown = MouseEvent("mousedown", position, button, modifiers, _clickCount);
        target.DispatchEvent(mousedown);
        // A press on a resizer the page lets through resizes (no focus, no selection).
        if (!mousedown.DefaultPrevented && !mousedown.DefaultHandled && scrollbar is null && ResizerAt(hit, position) is { } resizable)
        {
            StartResize(resizable, position);
            if (button == 2)
                target.DispatchEvent(MouseEvent("contextmenu", position, button, modifiers));
            return;
        }
        // Focus follows unless the press was cancelled or a control handled it (a spin button focuses its field itself).
        if (!mousedown.DefaultPrevented && !mousedown.DefaultHandled && button == 0)
        {
            // Focus goes to the nearest focusable ancestor; clicking elsewhere blurs. On a scrollbar
            // focus only moves to a focusable element (the viewport's scrollbars leave it alone).
            var focusable = target;
            while (focusable is not null && !focusable.IsFocusable)
                focusable = focusable.Parent;
            if (scrollbar is null || (focusable is not null && scrollbar.Value.Scroller != _document.Root))
                Focus(focusable, FocusOrigin.Pointer);
            // Then the selection (Blink's SelectionController): in a text field, at the press.
            if (scrollbar is null && target.IsTextField && _document.FocusedElement == target && PartAt(target, position) is not (ControlPart.SpinButton or ControlPart.CancelButton))
                FieldPress(target, position, _clickCount, modifiers.HasFlag(KeyModifiers.Shift), wasFocused);
            else if (scrollbar is null && !target.IsTextField && UiDocument.CanStartSelection(target))
                PagePress(hit, position, _clickCount, modifiers.HasFlag(KeyModifiers.Shift));
        }
        if (scrollbar is { } pressed)
        {
            PressScrollbar(pressed, modifiers);
            return;
        }
        // Linux and macOS open the context menu on the press.
        if (button == 2)
            target.DispatchEvent(MouseEvent("contextmenu", position, button, modifiers));
    }

    /// <summary>A button went up at <paramref name="position"/> (window units, DIPs).</summary>
    public void PointerUp(Vector2 position, MouseButton platformButton, KeyModifiers modifiers = KeyModifiers.None)
    {
        _pointerDip = position;
        position *= _document.DeviceScale;
        if (PopupPointerUp(position, ButtonOf(platformButton)))
            return;
        PointerPosition = position;
        var button = ButtonOf(platformButton);
        _buttons &= ~MaskOf(button);
        var hit = _document.HitTest(position);
        var target = _capture is { } captured && InDocument(captured) ? captured : TargetAt(hit, position);
        if (button == 0 && _scrollbarPress is not null)
        {
            // Releasing a scrollbar: the page gets pointerup and mouseup, but no click.
            ReleaseScrollbar();
            if (target is not null)
            {
                target.DispatchEvent(PointerEvent(_buttons == 0 ? "pointerup" : "pointermove", position, button, modifiers));
                if (!_suppressMouse)
                    target.DispatchEvent(MouseEvent("mouseup", position, button, modifiers, _clickCount));
            }
            _downTargets[button] = null;
            SetActive(null);
            if (_buttons == 0)
            {
                _suppressMouse = false;
                ReleaseCaptureNow();
                UpdateHover(TargetAt(_document.HitTest(position), position), position, modifiers);
            }
            return;
        }
        if (target is not null)
            target.DispatchEvent(PointerEvent(_buttons == 0 ? "pointerup" : "pointermove", position, button, modifiers));
        // Resizing ends with the release (after pointerup, before mouseup).
        _resizing = null;
        if (target is not null && !_suppressMouse)
            target.DispatchEvent(MouseEvent("mouseup", position, button, modifiers, _clickCount));
        // Capture ends with the last button (lostpointercapture comes before the click).
        if (_buttons == 0)
            _pendingCapture = null;
        ProcessPendingPointerCapture();
        if (target is not null)
        {
            if (!_suppressMouse)
            {
                // The click goes to the nearest element holding both the press and the release.
                if (_downTargets[button] is { } down && InDocument(down) && CommonAncestor(down, target) is { } clicked)
                {
                    if (button == 0)
                    {
                        Click(clicked, position, modifiers, _clickCount);
                        if (_clickCount == 2)
                            clicked.DispatchEvent(MouseEvent("dblclick", position, button, modifiers, 2));
                    }
                    else
                        clicked.DispatchEvent(MouseEvent("auxclick", position, button, modifiers, _clickCount));
                }
            }
        }
        _downTargets[button] = null;
        if (button == 0)
        {
            // The selection's release handling comes after the click.
            FieldRelease();
            PageRelease();
            SetActive(null);
            ReleaseParts();
        }
        if (_buttons == 0)
        {
            _suppressMouse = false;
            UpdateHover(TargetAt(hit, position), position, modifiers);
        }
    }

    /// <summary>A click on an element (its activation follows unless a listener prevents it).</summary>
    void Click(UiElement element, Vector2 position, KeyModifiers modifiers, int detail)
    {
        element.DispatchEvent(MouseEvent("click", position, 0, modifiers, detail));
    }

    /// <summary>Sends an element a click as a key press does (detail 0, at no position).</summary>
    public void SyntheticClick(UiElement element) => Click(element, Vector2.Zero, KeyModifiers.None, 0);

    static UiElement? CommonAncestor(UiElement a, UiElement b)
    {
        for (var x = a; x is not null; x = x.Parent)
            if (x == b || b.IsDescendantOf(x))
                return x;
        return null;
    }

    /// <summary>Boundary events and :hover when the element under the pointer changes.</summary>
    void UpdateHover(UiElement? target, Vector2 position, KeyModifiers modifiers)
    {
        var old = _hovered is { } previous && InDocument(previous) ? previous : null;
        if (old == target)
        {
            SetHoverChain(target);
            return;
        }
        _hovered = target;
        var common = old is not null && target is not null ? CommonAncestor(old, target) : null;
        List<UiElement> Chain(UiElement? from)
        {
            var chain = new List<UiElement>();
            for (var e = from; e is not null && e != common; e = e.Parent)
                chain.Add(e);
            return chain;
        }
        var leaving = Chain(old);
        var entering = Chain(target);
        entering.Reverse();
        // Pointer boundary events, then the mouse ones: out, leave (deepest first), over, enter (outermost first).
        foreach (var prefix in new[] { "pointer", "mouse" })
        {
            if (prefix == "mouse" && _suppressMouse)
                break;
            UiMouseEvent Make(string type, bool bubbles, UiElement? related) => prefix == "pointer"
                ? PointerEvent(type, position, -1, modifiers, bubbles, bubbles, related)
                : MouseEvent(type, position, 0, modifiers, 0, bubbles, bubbles, related);
            old?.DispatchEvent(Make(prefix + "out", true, target));
            foreach (var element in leaving)
                element.DispatchEvent(Make(prefix + "leave", false, target));
            target?.DispatchEvent(Make(prefix + "over", true, old));
            foreach (var element in entering)
                element.DispatchEvent(Make(prefix + "enter", false, old));
        }
        SetHoverChain(target);
    }

    /// <summary>:hover holds for the element under the pointer and its ancestors (and a hovered label's control).</summary>
    void SetHoverChain(UiElement? target)
    {
        _hoverChain.Clear();
        for (var e = target; e is not null; e = e.Parent)
            _hoverChain.Add(e);
        ApplyStates(_hoverSet, _hoverChain, static (e, on) => e.IsHovered = on);
    }

    /// <summary>:active holds for the pressed element and its ancestors while the main button is down.</summary>
    void SetActive(UiElement? target)
    {
        _activeChain.Clear();
        for (var e = target; e is not null; e = e.Parent)
            _activeChain.Add(e);
        RefreshActiveStates();
    }

    void RefreshActiveStates()
    {
        var active = new List<UiElement>(_activeChain);
        if (_keyActive is { } key && InDocument(key))
            active.Add(key);
        ApplyStates(_activeSet, active, static (e, on) => e.IsActive = on);
    }

    /// <summary>Sets a state on the elements (labels pass theirs to their controls, as Blink's HTMLLabelElement), clearing it where it no longer holds.</summary>
    static void ApplyStates(HashSet<UiElement> current, List<UiElement> elements, Action<UiElement, bool> set)
    {
        var next = new HashSet<UiElement>(elements);
        foreach (var element in elements)
            if (element.Tag == "label" && element.Control is { } control)
                next.Add(control);
        foreach (var element in current)
            if (!next.Contains(element))
                set(element, false);
        foreach (var element in next)
            set(element, true);
        current.Clear();
        current.UnionWith(next);
    }

    void UpdateCursor(HitResult? hit, Vector2 position)
    {
        // While resizing the cursor stays.
        if (_resizing is not null)
            return;
        CursorImage = null;
        // Scrollbars show the arrow.
        if (_document.OverViewportScrollbars(position) || (hit is { Box.IsScrollContainer: true } && _document.ScrollbarAt(position) is not null))
        {
            Cursor = "default";
            return;
        }
        // Resizers show the directions they resize.
        if (ResizerAt(hit, position) is { } resizable)
        {
            Cursor = ResizeCursor(resizable);
            return;
        }
        var style = hit?.Element.Style;
        if (style is not null)
            CursorImage = CursorImageOf(style, position);
        var cursor = style?.Cursor ?? "auto";
        if (cursor == "auto")
        {
            // Text that can be selected shows the I-beam; everything else the arrow.
            cursor = hit is { Box.Type: BoxType.Text } && style!.UsedUserSelect != UserSelect.None ? "text" : "default";
        }
        Cursor = cursor;
    }

    /// <summary>
    /// Blink's EventHandler::SelectCursor for cursor images: the first one loaded, at most 128 px a
    /// side, at its hotspot (else its corner); one over 32 px only where it fits in the viewport.
    /// </summary>
    UiCursorImage? CursorImageOf(Style.ComputedStyle style, Vector2 position)
    {
        foreach (var image in style.CursorImages)
        {
            if (_document.Layout.Context.ImageSize?.Invoke(image.Url) is not { X: > 0, Y: > 0 } size)
                continue;
            if (MathF.Ceiling(size.X) > 128 || MathF.Ceiling(size.Y) > 128)
                continue;
            var hotspot = image.Hotspot ?? Vector2.Zero;
            if (size.X > 32 || size.Y > 32)
            {
                // The cursor as the screen shows it (scaled by the device scale), at the pointer in layout units.
                var scale = _document.DeviceScale;
                var left = position.X - MathF.Floor(MathF.Round(hotspot.X) * scale);
                var top = position.Y - MathF.Floor(MathF.Round(hotspot.Y) * scale);
                var viewport = _document.Layout.Viewport;
                if (left < 0 || top < 0 || left + MathF.Floor(size.X * scale) > viewport.PaddingBoxWidth || top + MathF.Floor(size.Y * scale) > viewport.PaddingBoxHeight)
                    continue;
            }
            return new UiCursorImage(image.Url, hotspot);
        }
        return null;
    }

    /// <summary>
    /// The content under a still pointer changed: after layout and scrolling (<paramref name="rehover"/>)
    /// :hover and the boundary events follow it, as Chromium's fake mouse moves do; otherwise only
    /// the cursor does.
    /// </summary>
    public void Refresh(bool rehover = true)
    {
        // Removed elements are no longer hovered or active.
        if (_hoverChain.RemoveAll(e => !InDocument(e)) > 0 || _hoverSet.Any(e => !InDocument(e)))
            ApplyStates(_hoverSet, _hoverChain, static (e, on) => e.IsHovered = on);
        if (_keyActive is { } key && !InDocument(key))
            _keyActive = null;
        if (_activeChain.RemoveAll(e => !InDocument(e)) > 0 || _activeSet.Any(e => !InDocument(e)))
            RefreshActiveStates();
        if (!PointerInside || _capture is not null || _scrollbarPress is not null)
            return;
        var hit = _document.HitTest(PointerPosition);
        if (rehover)
            UpdateHover(TargetAt(hit, PointerPosition), PointerPosition, KeyModifiers.None);
        UpdateCursor(hit, PointerPosition);
        if (!rehover)
            return;
        UpdateScrollbarHover(PointerPosition);
        RefreshPartStates();
        PageTooltipUnderCursor(PointerPosition, hit);
    }

    // ---------- Pointer capture ----------

    UiElement? _pendingCapture;

    /// <summary>
    /// Pointer events go to <paramref name="element"/> until the buttons are released (or
    /// <see cref="ReleasePointerCapture"/>). As in Chromium the change is pending: it takes effect,
    /// with gotpointercapture, when the current mouse event has been handled or before the next one.
    /// Only while a button is held.
    /// </summary>
    public void SetPointerCapture(UiElement element)
    {
        if (!InDocument(element) || _buttons == 0)
            return;
        _pendingCapture = element;
    }

    /// <summary>Ends the element's capture (pending, as setting it is).</summary>
    public void ReleasePointerCapture(UiElement element)
    {
        if (_pendingCapture == element)
            _pendingCapture = null;
    }

    /// <summary>Whether the element has (or is about to get) the capture.</summary>
    public bool HasPointerCapture(UiElement element) => _pendingCapture == element;

    /// <summary>Applies a pending capture change: lostpointercapture on the old target, gotpointercapture on the new one.</summary>
    void ProcessPendingPointerCapture()
    {
        if (_pendingCapture is { } pending && !InDocument(pending))
            _pendingCapture = null;
        if (_pendingCapture == _capture)
            return;
        var old = _capture;
        _capture = _pendingCapture;
        if (old is not null && InDocument(old))
        {
            // A dragged slider thumb loses its drag with the capture (and commits).
            if (_rangeDrag == old)
                StopRangeDrag(dispatch: true);
            old.DispatchEvent(PointerEvent("lostpointercapture", PointerPosition, -1, KeyModifiers.None, cancelable: false));
        }
        if (_capture is { } now)
            now.DispatchEvent(PointerEvent("gotpointercapture", PointerPosition, -1, KeyModifiers.None, cancelable: false));
    }

    /// <summary>The capture ends at once (the last button went up, the window lost focus).</summary>
    void ReleaseCaptureNow()
    {
        _pendingCapture = null;
        ProcessPendingPointerCapture();
    }

    // ---------- Focus ----------

    internal void Focus(UiElement? element, FocusOrigin origin)
    {
        if (element is not null && (!InDocument(element) || !element.IsFocusable))
            return;
        var old = _document.FocusedElement;
        if (old is not null && !InDocument(old))
            old = null;
        if (old == element)
            return;
        _document.FocusedElement = element;
        // Focus moved by the user counts for :focus-visible (a click also forgets earlier keys).
        if (origin != FocusOrigin.Script)
            _lastUserFocus = origin;
        if (origin == FocusOrigin.Pointer)
            _hadKeyboardEvent = false;
        if (old is not null)
        {
            old.IsFocused = false;
            old.FocusVisible = false;
            // Leaving: a text field commits (change), a pressed control is released.
            old.FocusChanged(false);
            ClearKeyboardActive(old);
            PartsBlurred(old);
            old.DispatchEvent(Mouse(new UiFocusEvent("blur", bubbles: false) { RelatedTarget = element }));
            old.DispatchEvent(Mouse(new UiFocusEvent("focusout", bubbles: true) { RelatedTarget = element }));
        }
        // A listener may have moved focus meanwhile.
        if (element is null || _document.FocusedElement != element)
            return;
        element.IsFocused = true;
        UpdateFocusVisible();
        PartsFocused(element);
        _document.CloseTyping();
        element.DispatchEvent(Mouse(new UiFocusEvent("focus", bubbles: false) { RelatedTarget = old }));
        element.DispatchEvent(Mouse(new UiFocusEvent("focusin", bubbles: true) { RelatedTarget = old }));
        // UpdateSelectionOnFocus: keyboard focus selects an input's text; a click places the caret itself; script focus keeps the selection.
        if (element.IsTextField && _document.FocusedElement == element)
        {
            _document.SelectionMovedIntoField(element);
            element.SelectionEntered();
            if (origin == FocusOrigin.Keyboard && element.Tag == "input")
                element.SelectAllForFocus();
            else if (origin != FocusOrigin.Pointer)
                _document.SelectionMoved(element);
            // HTMLInputElement::UpdateSelectionOnFocus: unless a click focused it, the selection is revealed.
            if (origin != FocusOrigin.Pointer)
                _document.RevealSelection(element, SelectionReveal.IfNeeded);
        }
    }

    /// <summary>
    /// :focus-visible (Blink's MatchesFocusVisiblePseudoClass): the focused element shows focus if it
    /// takes text, or the user last moved focus other than with the pointer, or a key went down
    /// since (focus from script keeps whatever the user did last).
    /// </summary>
    void UpdateFocusVisible()
    {
        if (_document.FocusedElement is not { } focused || !InDocument(focused))
            return;
        // (While a select's list is open the page's frame isn't the active one, so a click's focus shows too.)
        focused.FocusVisible = focused.IsFocused && (TakesText(focused) || _lastUserFocus != FocusOrigin.Pointer || _hadKeyboardEvent || _popupSelect is not null);
    }

    /// <summary>Elements that take text (Blink's MayTriggerVirtualKeyboard): text fields, textareas, contenteditable.</summary>
    static bool TakesText(UiElement element) =>
        element.IsTextField || element.GetAttribute("contenteditable") is { } editable && editable.ToLowerInvariant() is "" or "true" or "plaintext-only";

    /// <summary>The focusable elements in Tab order: positive tabindex ascending, then the rest in tree order.</summary>
    public IReadOnlyList<UiElement> TabOrder()
    {
        var all = _document.AllElements().Where(e => e.IsFocusable && (e.TabIndex ?? 0) >= 0).ToList();
        return all.Where(e => e.TabIndex > 0).OrderBy(e => e.TabIndex).Concat(all.Where(e => (e.TabIndex ?? 0) == 0)).ToList();
    }

    void MoveFocus(bool backwards)
    {
        var order = TabOrder();
        if (order.Count == 0)
            return;
        var index = _document.FocusedElement is { } current ? order.ToList().IndexOf(current) : -1;
        int next;
        if (index < 0)
            next = backwards ? order.Count - 1 : 0;
        else
            next = (index + (backwards ? order.Count - 1 : 1)) % order.Count;
        Focus(order[next], FocusOrigin.Keyboard);
    }

    // ---------- Keyboard ----------

    UiElement KeyTarget => _document.FocusedElement is { } focused && InDocument(focused) ? focused : _document.Body;

    UiKeyboardEvent Key(string type, string key, string code, KeyModifiers modifiers, bool repeat) => Mouse(new UiKeyboardEvent(type)
    {
        Key = key,
        Code = code,
        Repeat = repeat,
        ShiftKey = modifiers.HasFlag(KeyModifiers.Shift),
        CtrlKey = modifiers.HasFlag(KeyModifiers.Control),
        AltKey = modifiers.HasFlag(KeyModifiers.Alt),
        MetaKey = modifiers.HasFlag(KeyModifiers.Super),
    });

    /// <summary>
    /// A key went down: <paramref name="key"/> is the DOM key value ("a", "Enter", "ArrowLeft" ...),
    /// <paramref name="code"/> the physical key ("KeyA" ...). As in Chromium, keydown goes to the
    /// focused element (its default actions: Tab, arrows, a radio's arrows ...); unless cancelled or
    /// handled, a key that types (or Enter) then sends keypress (Enter activating, Space scrolling).
    /// Returns false if a listener prevented the default action.
    /// </summary>
    public bool KeyDown(string key, string code, KeyModifiers modifiers = KeyModifiers.None, bool repeat = false)
    {
        HideTooltip();
        // A key without Ctrl, Alt or Meta makes focus visible (Blink's UpdateHadKeyboardEvent).
        if ((modifiers & (KeyModifiers.Control | KeyModifiers.Alt | KeyModifiers.Super)) == 0 && !_hadKeyboardEvent)
        {
            _hadKeyboardEvent = true;
            UpdateFocusVisible();
        }
        // An open select list takes the keys (the page sees none).
        if (PopupKeyDown(key, modifiers))
            return true;
        var down = Key("keydown", key, code, modifiers, repeat);
        KeyTarget.DispatchEvent(down);
        if (down.DefaultPrevented)
            return false;
        if (down.DefaultHandled || !TypesCharacter(key, modifiers))
            return true;
        // The keypress goes to whatever has focus now.
        return KeyTarget.DispatchEvent(Key("keypress", key, code, modifiers, repeat));
    }

    /// <summary>Keys that send keypress (Chromium's keys with text): printable characters and Enter, without Ctrl, Alt or Meta.</summary>
    static bool TypesCharacter(string key, KeyModifiers modifiers) =>
        (modifiers & (KeyModifiers.Control | KeyModifiers.Alt | KeyModifiers.Super)) == 0
        && (key == "Enter" || (key.Length > 0 && key.Length <= 2 && !char.IsControl(key[0]) && (key.Length == 1 || char.IsSurrogatePair(key, 0))));

    public bool KeyUp(string key, string code, KeyModifiers modifiers = KeyModifiers.None)
    {
        EndKeyScroll(code);
        if (SelectPopup is not null)
            return true;
        return KeyTarget.DispatchEvent(Key("keyup", key, code, modifiers, false));
    }

    // Default actions for keys (called from the elements' default event handlers).

    /// <summary>An element's own keydown action (Blink's per-element HandleKeydownEvent).</summary>
    internal void ElementKeyDown(UiElement element, UiKeyboardEvent e)
    {
        if (element.Tag == "select")
        {
            SelectKeyDown(element, e);
            return;
        }
        if (ControlKeyDown(element, e))
            return;
        // A text field's editor takes the keys its bindings name.
        if (element.IsTextField && _document.EditorKeyDown(element, e))
        {
            e.SetDefaultHandled();
            return;
        }
        var plain = !e.CtrlKey && !e.AltKey && !e.MetaKey;
        if (e.Key == " " && (IsKeyboardClickable(element) || element.IsCheckable))
        {
            // Space presses the control; its release clicks it.
            SetKeyboardActive(element);
            return;
        }
        if (element.Tag == "a" && element.HasAttribute("href") && e.Key == "Enter")
        {
            element.DispatchSimulatedClick(e);
            e.SetDefaultHandled();
            return;
        }
        if (element.InputType == "radio" && plain && e.Key is "ArrowUp" or "ArrowDown" or "ArrowLeft" or "ArrowRight")
            RadioArrow(element, e);
    }

    /// <summary>An element's own keypress action: Enter clicks buttons and submits forms from fields; Space is kept from scrolling.</summary>
    internal void ElementKeyPress(UiElement element, UiKeyboardEvent e)
    {
        if (element.Tag == "select")
        {
            SelectKeyPress(element, e);
            return;
        }
        // Characters typed into a text field (and a textarea's Enter).
        if (element.IsTextField && _document.EditorKeyPress(element, e))
        {
            e.SetDefaultHandled();
            return;
        }
        if (IsKeyboardClickable(element))
        {
            if (e.Key == "Enter")
            {
                element.DispatchSimulatedClick(e);
                e.SetDefaultHandled();
            }
            else if (e.Key == " ")
                e.SetDefaultHandled();
            return;
        }
        if (element.IsCheckable && e.Key == " ")
        {
            e.SetDefaultHandled();
            return;
        }
        if (element.Tag == "input" && e.Key == "Enter")
        {
            // In an editable field the editor first offers a line break (beforeinput); a single-line
            // field turns it into implicit submission, unless the beforeinput was cancelled.
            if (element.IsTextField && element.IsReadWrite)
            {
                var lineBreak = new UiInputEvent("beforeinput", cancelable: true) { InputType = "insertLineBreak", IsTrusted = true };
                if (!element.DispatchEvent(lineBreak))
                {
                    e.SetDefaultHandled();
                    return;
                }
            }
            // Implicit submission (HTMLInputElement::DefaultEventHandler): a search event, the change, the form.
            if (element.InputType == "search")
                _document.PostTask(() => element.DispatchEvent(new UiEvent("search", bubbles: true, cancelable: false) { IsTrusted = true }));
            element.DispatchFormControlChangeEvent();
            element.Form?.SubmitImplicitly(e, element.CanTriggerImplicitSubmission);
            e.SetDefaultHandled();
        }
    }

    /// <summary>An element's own keyup action: Space releases a pressed button or checkbox, clicking it.</summary>
    internal void ElementKeyUp(UiElement element, UiKeyboardEvent e)
    {
        if (e.Key != " ")
            return;
        if (element.InputType == "radio" && element.Checked)
        {
            // A checked radio isn't clicked again.
            ClearKeyboardActive(element);
            e.SetDefaultHandled();
            return;
        }
        if (IsKeyboardClickable(element) || element.IsCheckable)
        {
            if (_keyActive == element)
                element.DispatchSimulatedClick(e);
            e.SetDefaultHandled();
        }
    }

    /// <summary>The page's key actions at the target (Blink's DefaultKeyboardEventHandler): Tab moves focus, arrows and page keys scroll, Space pages.</summary>
    internal void DefaultKeyboardEventHandler(UiElement target, UiKeyboardEvent e)
    {
        var plain = !e.CtrlKey && !e.AltKey && !e.MetaKey;
        var modifiers = (e.ShiftKey ? KeyModifiers.Shift : 0) | (e.CtrlKey ? KeyModifiers.Control : 0) | (e.AltKey ? KeyModifiers.Alt : 0) | (e.MetaKey ? KeyModifiers.Super : 0);
        switch (e.Type)
        {
            case "keydown" when e.Key == "Tab":
                if (plain)
                {
                    MoveFocus(e.ShiftKey);
                    e.SetDefaultHandled();
                }
                break;
            case "keydown" when e.CtrlKey && !e.AltKey && !e.MetaKey && !e.ShiftKey && e.Key is "a" or "A":
                // SelectAll outside a text field: the page.
                _document.SelectAllPage();
                e.SetDefaultHandled();
                break;
            case "keydown" when e.CtrlKey && !e.AltKey && !e.MetaKey && !e.ShiftKey && e.Key is "c" or "C" or "Insert":
                if (_document.PageSelectionRange is { } range && UiDocument.Compare(range.Start, range.End) != 0)
                {
                    var at = range.Start.Node as UiElement ?? range.Start.Node.Parent ?? _document.Body;
                    if (at.DispatchEvent(new UiClipboardEvent("copy") { IsTrusted = true }))
                        _document.Clipboard.WriteText(_document.PageSelectionText());
                    e.SetDefaultHandled();
                }
                break;
            case "keydown":
                if (ScrollKey(e.Key, modifiers) is { } scroll && !UsesNavigationKeys(target, e.Key) && KeyboardScroll(scroll.Direction, scroll.Granularity, e.Code))
                    e.SetDefaultHandled();
                break;
            case "keypress" when e.Key == " " && plain && !UsesNavigationKeys(target, e.Key):
                // Space pages down (Shift+Space up).
                if (KeyboardScroll(new Vector2(0, e.ShiftKey ? -1 : 1), "page", e.Code))
                    e.SetDefaultHandled();
                break;
        }
    }

    /// <summary>Buttons and summaries: Space presses and clicks on release, Enter clicks (Blink's keyboard activation).</summary>
    static bool IsKeyboardClickable(UiElement element) =>
        element.Tag == "button" || (element.Tag == "input" && element.InputType is "button" or "submit" or "reset" or "image")
        || (element.Tag == "summary" && element.Parent is { Tag: "details" } details && details.ElementChildren.FirstOrDefault(c => c.Tag == "summary") == element);

    /// <summary>Arrows in a radio group: the next (right, down) or previous (left, up) focusable radio is focused and clicked, wrapping around.</summary>
    void RadioArrow(UiElement radio, UiKeyboardEvent e)
    {
        var forward = e.Key is "ArrowDown" or "ArrowRight";
        var group = radio.RadioGroup().ToList();
        var index = group.IndexOf(radio);
        if (index < 0)
            return;
        UiElement? next = null;
        for (var i = 1; i < group.Count && next is null; i++)
        {
            var candidate = group[((forward ? index + i : index - i) % group.Count + group.Count) % group.Count];
            if (candidate.IsFocusable)
                next = candidate;
        }
        if (next is null)
            return;
        // Blink focuses it as no particular user action (FocusType::kNone).
        Focus(next, FocusOrigin.Script);
        next.DispatchSimulatedClick(e);
        e.SetDefaultHandled();
    }

    // ---------- Keyboard :active ----------

    UiElement? _keyActive;

    /// <summary>Space holds a control :active (the element only, not its ancestors).</summary>
    void SetKeyboardActive(UiElement element)
    {
        if (_keyActive == element)
            return;
        _keyActive = element;
        RefreshActiveStates();
    }

    internal void ClearKeyboardActive(UiElement element)
    {
        if (_keyActive != element)
            return;
        _keyActive = null;
        RefreshActiveStates();
    }

    /// <summary>The window got the system's focus back: the focused element shows focus again.</summary>
    public void WindowFocus() => _document.SetWindowFocused(true);

    /// <summary>The window lost focus: buttons and keys are released and focus stops showing (as when the browser window deactivates).</summary>
    public void WindowBlur()
    {
        HideTooltip();
        CloseSelectPopup(commit: false);
        ReleaseSpinButton(dispatch: true);
        StopRangeDrag(dispatch: true);
        ReleaseParts();
        ReleaseScrollbar();
        EndWheelTransaction();
        if (_keyScroll is { } held)
            EndKeyScroll(held.Code);
        _buttons = 0;
        _suppressMouse = false;
        _keyActive = null;
        SetActive(null);
        ReleaseCaptureNow();
        _document.SetWindowFocused(false);
    }
}

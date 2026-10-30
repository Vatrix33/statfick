using System.Numerics;
using Statventure.Platform;
using Statventure.UI.Layout;
using Statventure.UI.Style;

namespace Statventure.UI.Dom;

/// <summary>
/// Scrolling by input, as Chromium 152 does it on Linux and Windows: the wheel (latched to one
/// scroller for half a second, chaining outwards only when a turn starts), the keyboard (arrows,
/// Page Up/Down, Home/End, Space) and the scrollbars (thumb drags, arrow buttons and the track,
/// which keep scrolling while held).
/// </summary>
public sealed partial class UiInput
{
    /// <summary>A wheel scroll stays with its scroller this long after the last turn (Chromium's wheel transaction).</summary>
    public const double WheelLatchTime = 0.5;
    /// <summary>A held scrollbar button or track starts scrolling on its own after this (seconds).</summary>
    public const double AutoscrollDelay = 0.25;
    const float AutoscrollMultiplier = 20, PixelsPerLine = 40, PageFraction = 0.875f;

    /// <summary>A line: 40 window units (Blink's PixelsPerLineStep through WindowToViewportScalar), in device pixels.</summary>
    float LineStep => PixelsPerLine * _document.DeviceScale;

    /// <summary>
    /// The thumb jumps back to where its drag started when the pointer strays far from the bar
    /// (Chromium does it on Windows, not on Linux).
    /// </summary>
    public bool ScrollbarSnapBack { get; set; } = Platform.PlatformBehaviour.Windows;

    /// <summary>Ctrl+wheel doesn't scroll: Chromium zooms the page instead. The host decides (the delta in pixels).</summary>
    public event Action<Vector2>? ZoomWheel;

    // The wheel transaction.
    UiElement? _wheelTarget, _wheelScroller;
    double _lastWheelTime = double.NegativeInfinity;
    bool _wheelNonBlocking, _wheelLatched;

    // The scrollbar being pressed.
    sealed class ScrollbarPress
    {
        public required UiElement Scroller;
        public required bool Vertical;
        public required ScrollbarPart Part;
        public required Matrix3x2 ToLocal;
        /// <summary>Thumb drags: the pointer and the scroll position at the start, along the bar (from its start).</summary>
        public float Origin, StartOffset;
        public bool Dragging;
        /// <summary>Buttons and track: the speed a held press scrolls at, and when it was pressed.</summary>
        public float Velocity;
        public double PressTime;
        public bool AutoscrollReady;
    }

    ScrollbarPress? _scrollbarPress;
    (string Code, UiElement Scroller)? _keyScroll;
    UiElement? _mousePressNode;

    /// <summary>Input needs frames to come (a wheel transaction to end, a held scrollbar to repeat).</summary>
    internal bool NeedsFrames => _wheelTarget is not null || _scrollbarPress is { Dragging: false } || _spinOwner is not null || TooltipPending;

    /// <summary>The document's clock moved on: wheel transactions end, held scrollbars start or stop scrolling.</summary>
    internal void Advance(double time)
    {
        AdvanceControls(time);
        AdvanceTooltip(time);
        CheckSelectPopup();
        if (_wheelTarget is not null && time - _lastWheelTime > WheelLatchTime)
            EndWheelTransaction();
        if (_scrollbarPress is { Dragging: false } press && InDocument(press.Scroller))
        {
            if (!press.AutoscrollReady && time - press.PressTime >= AutoscrollDelay)
                press.AutoscrollReady = true;
            if (press.AutoscrollReady)
                UpdateAutoscroll(press);
        }
    }

    // ---------- Wheel ----------

    /// <summary>
    /// A wheel turn (or touchpad scroll when <paramref name="precise"/>) at a position, by a delta in
    /// pixels (positive: down and right; Chromium scrolls 120 px per notch on Linux, 100 on
    /// Windows). The wheel event goes to the element under the pointer (the same one for a whole
    /// turn); unless cancelled, the scroller the turn latched to scrolls. Shift turns a vertical
    /// wheel horizontal; Ctrl zooms instead (<see cref="ZoomWheel"/>). Returns false when cancelled.
    /// </summary>
    public bool Wheel(Vector2 position, Vector2 delta, KeyModifiers modifiers = KeyModifiers.None, bool precise = false)
    {
        // Window units in, device pixels out (Chromium scales wheel events by the device scale);
        // the DOM's deltas are CSS pixels (delta / devicePixelRatio).
        _pointerDip = position;
        var windowDelta = delta;
        position *= _document.DeviceScale;
        delta *= _document.DeviceScale;
        PointerPosition = position;
        PointerInside = true;
        TooltipWheel();
        if (PopupWheel(position, delta))
            return true;
        var time = Now;
        var starts = _wheelTarget is not { } latched || !InDocument(latched) || time - _lastWheelTime > WheelLatchTime;
        if (starts)
        {
            EndWheelTransaction();
            var hit = _document.HitTest(position);
            _wheelTarget = hit?.Element ?? (_document.OverViewportScrollbars(position) ? _document.Root : null);
            _wheelNonBlocking = false;
        }
        _lastWheelTime = time;
        if (_wheelTarget is not { } target)
            return true;
        var e = Mouse(new UiWheelEvent("wheel", cancelable: !_wheelNonBlocking)
        {
            Position = ToCss(position),
            LayoutPosition = position,
            Buttons = _buttons,
            ShiftKey = modifiers.HasFlag(KeyModifiers.Shift),
            CtrlKey = modifiers.HasFlag(KeyModifiers.Control),
            AltKey = modifiers.HasFlag(KeyModifiers.Alt),
            MetaKey = modifiers.HasFlag(KeyModifiers.Super),
            DeltaX = delta.X / _document.Zoom,
            DeltaY = delta.Y / _document.Zoom,
        });
        var allowed = target.DispatchEvent(e);
        if (!allowed)
            return false;
        // A control used the turn (a focused number field steps).
        if (e.DefaultHandled)
            return true;
        if (modifiers.HasFlag(KeyModifiers.Control) && !precise)
        {
            ZoomWheel?.Invoke(windowDelta);
            return true;
        }
        var scroll = modifiers.HasFlag(KeyModifiers.Shift) && delta.X == 0 ? new Vector2(delta.Y, 0) : delta;
        if (scroll == Vector2.Zero)
            return true;
        if (!_wheelLatched)
        {
            // The first scroll of a turn picks the scroller; the rest of the turn's events no longer wait for listeners.
            _wheelLatched = true;
            _wheelNonBlocking = true;
            _wheelScroller = LatchScroller(target, scroll);
            if (_wheelScroller is { } latchedScroller)
                _document.HoldScroll(latchedScroller);
        }
        if (_wheelScroller is { } scroller && InDocument(scroller))
            _document.UserScroll(scroller, UserAxes(scroller, scroll), ScrollKind.Wheel, animate: !precise);
        return true;
    }

    void EndWheelTransaction()
    {
        if (_wheelScroller is { } scroller)
            _document.ReleaseScroll(scroller);
        _wheelTarget = _wheelScroller = null;
        _wheelLatched = false;
    }

    /// <summary>The delta in the axes the user may scroll.</summary>
    Vector2 UserAxes(UiElement scroller, Vector2 delta)
    {
        var (x, y) = _document.UserScrollableAxes(scroller);
        return new Vector2(x ? delta.X : 0, y ? delta.Y : 0);
    }

    /// <summary>
    /// cc's FindNodeToLatch: from the element under the pointer outwards along containing blocks,
    /// the first scroller that can scroll that way; one with overscroll-behavior other than auto
    /// stops the search (it takes the turn even at its end); the document takes what is left.
    /// </summary>
    UiElement? LatchScroller(UiElement target, Vector2 delta)
    {
        var start = target;
        while (start is not null && start.Box is null)
            start = start.Parent;
        for (var box = start?.Box; box is not null; box = box.Container)
        {
            if (box.IsViewport)
                break;
            if (!box.IsScrollContainer || box.Element is not { } scroller || scroller == _document.Root)
                continue;
            var s = box.Style;
            var chains = s.OverscrollBehaviorX == OverscrollBehavior.Auto && s.OverscrollBehaviorY == OverscrollBehavior.Auto;
            var (userX, userY) = _document.UserScrollableAxes(scroller);
            if (!userX && !userY && chains)
                continue;
            if (_document.CanScroll(scroller, new Vector2(userX ? delta.X : 0, userY ? delta.Y : 0)))
                return scroller;
            var propagates = (delta.X == 0 || s.OverscrollBehaviorX == OverscrollBehavior.Auto) && (delta.Y == 0 || s.OverscrollBehaviorY == OverscrollBehavior.Auto);
            if (!propagates)
                return scroller;
        }
        return _document.Root;
    }

    // ---------- Keyboard ----------

    /// <summary>
    /// The scrolling a key does when nothing handled it (Blink's MapKeyCodeForScroll): arrows by a
    /// line (40 px), Page Up/Down by 87.5% of the scrollport, Home/End (also with Ctrl) to the
    /// ends; Shift, Meta and Alt combinations don't scroll.
    /// </summary>
    static (Vector2 Direction, string Granularity)? ScrollKey(string key, KeyModifiers modifiers)
    {
        if ((modifiers & (KeyModifiers.Shift | KeyModifiers.Super | KeyModifiers.Alt)) != 0)
            return null;
        if (modifiers.HasFlag(KeyModifiers.Control) && key is not ("Home" or "End"))
            return null;
        return key switch
        {
            "ArrowLeft" => (new Vector2(-1, 0), "line"),
            "ArrowRight" => (new Vector2(1, 0), "line"),
            "ArrowUp" => (new Vector2(0, -1), "line"),
            "ArrowDown" => (new Vector2(0, 1), "line"),
            "Home" => (new Vector2(0, -1), "document"),
            "End" => (new Vector2(0, 1), "document"),
            "PageUp" => (new Vector2(0, -1), "page"),
            "PageDown" => (new Vector2(0, 1), "page"),
            _ => null,
        };
    }

    /// <summary>Controls that use these keys themselves (text fields move the caret, selects and sliders change).</summary>
    static bool UsesNavigationKeys(UiElement element, string key)
    {
        if (element.Tag is "textarea" or "select" || element.HasAttribute("contenteditable"))
            return true;
        if (element.Tag != "input")
            return false;
        var type = (element.GetAttribute("type") ?? "text").ToLowerInvariant();
        return type switch
        {
            "range" or "radio" => key.StartsWith("Arrow", StringComparison.Ordinal) || key is "Home" or "End" || (type == "range" && key is "PageUp" or "PageDown"),
            "number" => key is "ArrowUp" or "ArrowDown" or "Home" or "End" or "ArrowLeft" or "ArrowRight",
            "checkbox" or "button" or "submit" or "reset" or "image" or "color" or "file" or "hidden" => false,
            _ => true,
        };
    }

    /// <summary>
    /// Scrolls by keyboard (Blink's LogicalScroll): starting at the focused element (else where the
    /// mouse last pressed, else the document), the innermost scroller along the containing blocks
    /// that can still move that way scrolls; overscroll-behavior other than auto stops the search.
    /// </summary>
    bool KeyboardScroll(Vector2 direction, string granularity, string code)
    {
        var start = _document.FocusedElement is { } focused && InDocument(focused) ? focused
            : _mousePressNode is { } pressed && InDocument(pressed) ? pressed
            : _document.Root;
        _document.UpdateLayout();
        while (start is not null && start.Box is null)
            start = start.Parent;
        var vertical = direction.Y != 0;
        var chain = new List<UiElement>();
        for (var box = start?.Box; box is not null; box = box.Container)
        {
            if (box.IsViewport)
                break;
            if (!box.IsScrollContainer || box.Element is not { } scroller || scroller == _document.Root)
                continue;
            var (userX, userY) = _document.UserScrollableAxes(scroller);
            var max = _document.LayoutMaxScroll(scroller);
            if (!(vertical ? userY && max.Y > 0 : userX && max.X > 0))
                continue;
            chain.Add(scroller);
            var behavior = vertical ? box.Style.OverscrollBehaviorY : box.Style.OverscrollBehaviorX;
            if (behavior != OverscrollBehavior.Auto)
                break;
        }
        var bubbles = chain.Count == 0 || chain[^1].Box is not { } last || (vertical ? last.Style.OverscrollBehaviorY : last.Style.OverscrollBehaviorX) == OverscrollBehavior.Auto;
        if (bubbles)
            chain.Add(_document.Root);
        foreach (var scroller in chain)
        {
            var (userX, userY) = _document.UserScrollableAxes(scroller);
            if (!(vertical ? userY : userX))
                continue;
            var step = granularity switch
            {
                "line" => LineStep,
                "page" => PageStep(scroller, vertical),
                _ => vertical ? _document.LayoutMaxScroll(scroller).Y : _document.LayoutMaxScroll(scroller).X,
            };
            if (_document.UserScroll(scroller, direction * step, ScrollKind.Keyboard))
            {
                // Its scrollend waits for the key to come up.
                if (_keyScroll is { } previous)
                    _document.ReleaseScroll(previous.Scroller);
                _keyScroll = (code, scroller);
                _document.HoldScroll(scroller);
                return true;
            }
        }
        return false;
    }

    /// <summary>cc::ScrollUtils::CalculatePageStep: 87.5% of the scrollport less its scroll-padding, in whole pixels.</summary>
    float PageStep(UiElement scroller, bool vertical)
    {
        float length;
        ComputedStyle? style = scroller.Style;
        if (scroller == _document.Root)
        {
            var viewport = _document.Layout.Viewport;
            length = vertical ? viewport.PaddingBoxHeight : viewport.PaddingBoxWidth;
        }
        else if (scroller.Box is { } box)
            length = vertical ? box.PaddingBoxHeight : box.PaddingBoxWidth;
        else
            return 1;
        if (style is not null)
            length -= vertical
                ? style.ScrollPaddingTop.ResolveOr(length, 0) + style.ScrollPaddingBottom.ResolveOr(length, 0)
                : style.ScrollPaddingLeft.ResolveOr(length, 0) + style.ScrollPaddingRight.ResolveOr(length, 0);
        var whole = (int)MathF.Round(length, MidpointRounding.AwayFromZero);
        return Math.Max((int)(whole * PageFraction), 1);
    }

    void EndKeyScroll(string code)
    {
        if (_keyScroll is { } held && held.Code == code)
        {
            _keyScroll = null;
            _document.ReleaseScroll(held.Scroller);
        }
    }

    // ---------- Scrollbars ----------

    /// <summary>The pointer over a scrollbar shows its part hovered (none while a press holds it).</summary>
    void UpdateScrollbarHover(Vector2 position)
    {
        if (_scrollbarPress is not null)
            return;
        _document.ScrollbarHover = _document.ScrollbarAt(position) is { Part: not ScrollbarPart.None } bar
            ? (bar.Scroller, bar.Geometry.Vertical, bar.Part)
            : null;
    }

    /// <summary>
    /// A main-button press on an enabled scrollbar (cc's ScrollbarController): the thumb starts a
    /// drag; a button scrolls a line (40 px), the track a page towards the pointer (Shift+click on
    /// the track jumps the thumb there and drags it), and held, they keep scrolling.
    /// </summary>
    bool PressScrollbar(UiDocument.ScrollbarHit bar, KeyModifiers modifiers)
    {
        if (!bar.Geometry.Enabled || bar.Part == ScrollbarPart.None)
            return false;
        var geometry = bar.Geometry;
        var local = Vector2.Transform(PointerPosition, bar.ToLocal);
        var press = new ScrollbarPress
        {
            Scroller = bar.Scroller,
            Vertical = geometry.Vertical,
            Part = bar.Part,
            ToLocal = bar.ToLocal,
            PressTime = Now,
        };
        var offset = _document.LayoutScrollOffsetOf(bar.Scroller);
        var along = geometry.Vertical ? offset.Y : offset.X;
        var barStart = geometry.Vertical ? geometry.Bar.Y : geometry.Bar.X;
        var jump = modifiers.HasFlag(KeyModifiers.Shift) && bar.Part is ScrollbarPart.BackTrack or ScrollbarPart.ForwardTrack;
        if (bar.Part == ScrollbarPart.Thumb || jump)
        {
            press.Dragging = true;
            press.StartOffset = along;
            if (jump)
            {
                // The thumb's centre goes to the pointer, then follows it as in a drag from there.
                var thumb = geometry.ThumbArea;
                var thumbStart = geometry.Vertical ? thumb.Y : thumb.X;
                press.Origin = thumbStart + geometry.ThumbLength / 2f - barStart;
                var distance = MathF.Round(MathF.Abs(geometry.Along(local) - geometry.ThumbLength / 2f - thumbStart)) * geometry.ContentPerThumbPixel;
                var sign = bar.Part == ScrollbarPart.BackTrack ? -1 : 1;
                _document.UserScroll(bar.Scroller, Axis(geometry.Vertical, sign * distance), ScrollKind.Wheel, animate: false);
            }
            else
                press.Origin = geometry.Along(local) - barStart;
        }
        else
        {
            var distance = bar.Part is ScrollbarPart.BackButton or ScrollbarPart.ForwardButton ? LineStep : PageStepOf(bar.Scroller, geometry.Vertical);
            var sign = bar.Part is ScrollbarPart.BackButton or ScrollbarPart.BackTrack ? -1 : 1;
            _document.UserScroll(bar.Scroller, Axis(geometry.Vertical, sign * distance), ScrollKind.Wheel);
            press.Velocity = sign * distance * AutoscrollMultiplier;
        }
        _scrollbarPress = press;
        _document.HoldScroll(bar.Scroller);
        _document.ScrollbarPress = (bar.Scroller, geometry.Vertical, bar.Part);
        _document.ScrollbarHover = (bar.Scroller, geometry.Vertical, bar.Part);
        return true;
    }

    static Vector2 Axis(bool vertical, float distance) => vertical ? new Vector2(0, distance) : new Vector2(distance, 0);

    /// <summary>A page for the scrollbar's track: 87.5% of the scroller's client length (whole pixels).</summary>
    float PageStepOf(UiElement scroller, bool vertical)
    {
        var viewport = _document.Layout.Viewport;
        var length = scroller == _document.Root
            ? (vertical ? viewport.PaddingBoxHeight : viewport.PaddingBoxWidth)
            : scroller.Box is { } box ? (vertical ? box.PaddingBoxHeight : box.PaddingBoxWidth) : 0;
        return Math.Max((int)((int)length * PageFraction), 1);
    }

    /// <summary>The pointer moved while a scrollbar is pressed: a drag moves the thumb with it.</summary>
    void MoveScrollbar(ScrollbarPress press)
    {
        if (!InDocument(press.Scroller) || _document.ScrollbarOf(press.Scroller, press.Vertical) is not { } geometry)
            return;
        var local = Vector2.Transform(PointerPosition, press.ToLocal);
        if (!press.Dragging)
        {
            // Hovering over the pressed part or elsewhere decides whether a held press keeps scrolling.
            _document.ScrollbarHover = geometry.PartAt(local) is var part and not ScrollbarPart.None ? (press.Scroller, press.Vertical, part) : null;
            if (press.AutoscrollReady)
                UpdateAutoscroll(press);
            return;
        }
        var offset = _document.LayoutScrollOffsetOf(press.Scroller);
        var current = press.Vertical ? offset.Y : offset.X;
        float target;
        if (ScrollbarSnapBack && OutsideDragGutter(geometry, local))
            target = press.StartOffset;
        else
        {
            var barStart = press.Vertical ? geometry.Bar.Y : geometry.Bar.X;
            var pointer = geometry.Along(local) - barStart;
            target = press.StartOffset + (pointer - press.Origin) * geometry.ContentPerThumbPixel;
        }
        var max = _document.LayoutMaxScroll(press.Scroller);
        target = Math.Clamp(target, 0, press.Vertical ? max.Y : max.X);
        if (target != current)
            _document.UserScroll(press.Scroller, Axis(press.Vertical, target - current), ScrollKind.Wheel, animate: false);
    }

    /// <summary>Windows' snap-back gutter: eight bar thicknesses beside the track.</summary>
    static bool OutsideDragGutter(ScrollbarGeometry geometry, Vector2 local)
    {
        var thickness = geometry.Vertical ? geometry.Bar.Width : geometry.Bar.Height;
        var gutter = 8 * thickness;
        var across = geometry.Vertical ? local.X - geometry.Bar.X : local.Y - geometry.Bar.Y;
        return across < -gutter || across > thickness + gutter;
    }

    /// <summary>
    /// A held button or track scrolls on (linearly, at 20 times its step per second) while the
    /// pointer stays on the pressed part, and the track until the thumb reaches the pointer.
    /// </summary>
    void UpdateAutoscroll(ScrollbarPress press)
    {
        if (!InDocument(press.Scroller) || _document.ScrollbarOf(press.Scroller, press.Vertical) is not { } geometry)
            return;
        var local = Vector2.Transform(PointerPosition, press.ToLocal);
        var pointer = geometry.Along(local);
        var thumb = geometry.ThumbArea;
        var thumbStart = press.Vertical ? thumb.Y : thumb.X;
        var thumbEnd = thumbStart + geometry.ThumbLength;
        var reached = press.Velocity > 0 ? thumbEnd > pointer : thumbStart < pointer;
        var onPart = ScrollbarGeometry.Contains(geometry.RectOf(press.Part), local);
        var tracks = press.Part is ScrollbarPart.BackTrack or ScrollbarPart.ForwardTrack;
        if (!onPart || (tracks && reached))
            _document.StopAutoscroll(press.Scroller);
        else if (!_document.IsAutoscrolling(press.Scroller))
            _document.StartAutoscroll(press.Scroller, press.Vertical, press.Velocity);
    }

    void ReleaseScrollbar()
    {
        if (_scrollbarPress is not { } press)
            return;
        _scrollbarPress = null;
        _document.StopAutoscroll(press.Scroller);
        _document.ReleaseScroll(press.Scroller);
        _document.ScrollbarPress = null;
        UpdateScrollbarHover(PointerPosition);
    }
}

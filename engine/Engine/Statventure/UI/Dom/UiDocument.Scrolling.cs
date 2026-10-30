using System.Numerics;
using Statventure.Core;
using Statventure.UI.Layout;
using Statventure.UI.Paint;
using Statventure.UI.Style;

namespace Statventure.UI.Dom;

/// <summary>What caused a scroll: Chromium animates each kind its own way.</summary>
public enum ScrollKind
{
    /// <summary>Script with smooth behavior: √distance frames, at most 1.5 s (cubic-bezier(0.4, 0, 0, 1)).</summary>
    Programmatic,
    /// <summary>Arrow keys, Page Up/Down, Home/End and Space: 0.15 s, ease-in-out.</summary>
    Keyboard,
    /// <summary>The mouse wheel and the scrollbar's buttons and track: 6 to 12 frames (shorter for longer scrolls), ease-in-out.</summary>
    Wheel,
    /// <summary>A held scrollbar button or track: linear, at a constant speed.</summary>
    Autoscroll,
}

/// <summary>How a scroll by script moves (the DOM's ScrollBehavior): as the scroller's scroll-behavior says, at once, or smoothly.</summary>
public enum ScrollMode
{
    Auto,
    Instant,
    Smooth,
}

/// <summary>Scrolling into view: where the target ends up in a scroller (scrollIntoView's block and inline).</summary>
public enum ScrollAlign
{
    Start,
    Center,
    End,
    /// <summary>Only as far as needed to show it (not at all when it is visible).</summary>
    Nearest,
}

public sealed partial class UiDocument
{
    // ---------- Scroll positions ----------

    /// <summary>
    /// Smooth scrolling for user scrolls (wheel, keyboard, scrollbar): on, as in Chromium on Linux
    /// and Windows. Off, they jump.
    /// </summary>
    public bool SmoothScrolling { get; set; } = true;

    readonly HashSet<UiElement> _scrolledElements = [];
    readonly Dictionary<UiElement, ScrollCurve> _scrollCurves = [];
    readonly List<UiElement> _scrollEventQueue = [];
    readonly HashSet<UiElement> _scrollEndPending = [];
    readonly Dictionary<UiElement, int> _scrollHolds = [];
    bool _stickyDirty = true;

    double Now => Animations.Time;

    /// <summary>How far the document is scrolled (window.scrollX and scrollY, in CSS px).</summary>
    public Vector2 ScrollPosition
    {
        get => Layout.ViewportScroll / Zoom;
        set => ScrollTo(Root, value);
    }

    /// <summary>How far a scroller (an element that scrolls, or the root for the document) can scroll, in CSS px.</summary>
    public Vector2 MaxScroll(UiElement scroller) => LayoutMaxScroll(scroller) / Zoom;

    /// <summary>Where a scroller is scrolled to now, in CSS px (Blink's AdjustScroll: the offset over the zoom).</summary>
    public Vector2 ScrollOffsetOf(UiElement scroller) => LayoutScrollOffsetOf(scroller) / Zoom;

    /// <summary>Scrolls an element (the root: the document) to an offset in CSS px (times the zoom in device pixels, as Blink's scroll setters do).</summary>
    public void ScrollTo(UiElement scroller, Vector2 offset, ScrollMode mode = ScrollMode.Auto) => LayoutScrollTo(scroller, offset * Zoom, mode);

    /// <summary>Scrolls by a distance in CSS px.</summary>
    public void ScrollBy(UiElement scroller, Vector2 delta, ScrollMode mode = ScrollMode.Auto) => LayoutScrollBy(scroller, delta * Zoom, mode);

    /// <summary>How far a scroller can scroll, in device pixels.</summary>
    internal Vector2 LayoutMaxScroll(UiElement scroller)
    {
        if (scroller == Root)
        {
            var viewport = Layout.Viewport;
            return new Vector2(MathF.Max(0, viewport.ScrollWidth - viewport.PaddingBoxWidth), MathF.Max(0, viewport.ScrollHeight - viewport.PaddingBoxHeight));
        }
        if (scroller.Document != this || scroller.Box is not { IsScrollContainer: true } box)
            return Vector2.Zero;
        return new Vector2(MathF.Max(0, box.ScrollWidth - box.PaddingBoxWidth), MathF.Max(0, box.ScrollHeight - box.PaddingBoxHeight));
    }

    /// <summary>Where a scroller is scrolled to now, in device pixels.</summary>
    internal Vector2 LayoutScrollOffsetOf(UiElement scroller) => scroller == Root ? Layout.ViewportScroll : scroller.LayoutScroll;

    /// <summary>Where a scroller is going: the end of its running scroll animation, else where it is.</summary>
    internal Vector2 ScrollTargetOf(UiElement scroller) =>
        _scrollCurves.TryGetValue(scroller, out var curve) && curve.Kind != ScrollKind.Autoscroll ? curve.Target : LayoutScrollOffsetOf(scroller);

    /// <summary>
    /// Scrolls an element (the root: the document) to an offset, clamped to what it can scroll;
    /// layout is brought up to date first. Smooth (by <paramref name="mode"/>, or for Auto the
    /// scroller's scroll-behavior) animates as Chromium's programmatic scrolls do.
    /// </summary>
    internal void LayoutScrollTo(UiElement scroller, Vector2 offset, ScrollMode mode = ScrollMode.Auto)
    {
        UpdateLayout();
        if (!float.IsFinite(offset.X))
            offset.X = 0;
        if (!float.IsFinite(offset.Y))
            offset.Y = 0;
        var target = Vector2.Clamp(offset, Vector2.Zero, LayoutMaxScroll(scroller));
        if (mode == ScrollMode.Smooth || (mode == ScrollMode.Auto && ScrollBehaviorOf(scroller) == ScrollBehavior.Smooth))
        {
            var from = LayoutScrollOffsetOf(scroller);
            _scrollCurves.Remove(scroller);
            if (from != target)
            {
                _scrollCurves[scroller] = ScrollCurve.Create(ScrollKind.Programmatic, from, target, Now);
                Repaint();
            }
            return;
        }
        _scrollCurves.Remove(scroller);
        // The document (Blink's RootFrameViewport, which never rounds) takes any change of the
        // unrounded offset as a scroll: one that rounds back to where it is still ends, with a
        // scrollend and no scroll.
        if (scroller == Root && target != LayoutScrollOffsetOf(Root) && Rounded(target) == LayoutScrollOffsetOf(Root))
        {
            _scrollEndPending.Add(Root);
            Repaint();
            return;
        }
        SetScrollOffset(scroller, target);
    }

    static Vector2 Rounded(Vector2 offset) => new(MathF.Round(offset.X, MidpointRounding.AwayFromZero), MathF.Round(offset.Y, MidpointRounding.AwayFromZero));

    /// <summary>Scrolls by a distance (from where a running smooth scroll is going).</summary>
    internal void LayoutScrollBy(UiElement scroller, Vector2 delta, ScrollMode mode = ScrollMode.Auto)
    {
        UpdateLayout();
        LayoutScrollTo(scroller, ScrollTargetOf(scroller) + delta, mode);
    }

    /// <summary>The scroll-behavior that applies to a scroller (the root element's for the document).</summary>
    static ScrollBehavior ScrollBehaviorOf(UiElement scroller) => scroller.Style?.ScrollBehavior ?? ScrollBehavior.Auto;

    /// <summary>
    /// A user scroll (wheel, keyboard, scrollbar) by a distance: animated when smooth scrolling is
    /// on (and <paramref name="animate"/>: precise touchpad scrolls jump), continuing a running user
    /// animation from where it is going. Returns false when the scroller can't move that way.
    /// </summary>
    internal bool UserScroll(UiElement scroller, Vector2 delta, ScrollKind kind, bool animate = true)
    {
        UpdateLayout();
        var running = _scrollCurves.GetValueOrDefault(scroller);
        var user = running is { Kind: ScrollKind.Wheel or ScrollKind.Keyboard } && !running.Done(Now);
        var start = user ? running!.Target : LayoutScrollOffsetOf(scroller);
        var target = Vector2.Clamp(start + delta, Vector2.Zero, LayoutMaxScroll(scroller));
        if (MathF.Abs(target.X - start.X) < 1e-3f && MathF.Abs(target.Y - start.Y) < 1e-3f)
            return false;
        if (!SmoothScrolling || !animate)
        {
            _scrollCurves.Remove(scroller);
            SetScrollOffset(scroller, target);
            return true;
        }
        if (user)
            running!.UpdateTarget(Now, target);
        else
            _scrollCurves[scroller] = ScrollCurve.Create(kind, LayoutScrollOffsetOf(scroller), target, Now);
        Repaint();
        return true;
    }

    /// <summary>
    /// The axes the user can scroll (overflow auto or scroll; for the document, the overflow the
    /// viewport took from the root or body). overflow: hidden scrolls only by script.
    /// </summary>
    internal (bool X, bool Y) UserScrollableAxes(UiElement scroller)
    {
        static bool User(Overflow o) => o is Overflow.Auto or Overflow.Scroll;
        if (scroller == Root)
        {
            var (x, y) = Layout.ViewportOverflowValues;
            return (User(x), User(y));
        }
        return scroller.Box is { IsScrollContainer: true } box ? (User(box.Style.OverflowX), User(box.Style.OverflowY)) : (false, false);
    }

    /// <summary>Could the scroller move by this delta (in either axis that has one)?</summary>
    internal bool CanScroll(UiElement scroller, Vector2 delta)
    {
        var at = ScrollTargetOf(scroller);
        var max = LayoutMaxScroll(scroller);
        return (delta.X < 0 && at.X > 0) || (delta.X > 0 && at.X < max.X) || (delta.Y < 0 && at.Y > 0) || (delta.Y > 0 && at.Y < max.Y);
    }

    /// <summary>Scrolls steadily towards one end at a speed (px/s; negative towards the start), until stopped.</summary>
    internal void StartAutoscroll(UiElement scroller, bool vertical, float velocity)
    {
        var from = LayoutScrollOffsetOf(scroller);
        var max = LayoutMaxScroll(scroller);
        var end = velocity < 0 ? 0 : vertical ? max.Y : max.X;
        var target = vertical ? new Vector2(from.X, end) : new Vector2(end, from.Y);
        _scrollCurves.Remove(scroller);
        if (target != from)
        {
            _scrollCurves[scroller] = ScrollCurve.Create(ScrollKind.Autoscroll, from, target, Now, MathF.Abs(velocity));
            Repaint();
        }
    }

    internal void StopAutoscroll(UiElement scroller)
    {
        if (_scrollCurves.TryGetValue(scroller, out var curve) && curve.Kind == ScrollKind.Autoscroll)
            _scrollCurves.Remove(scroller);
    }

    internal bool IsAutoscrolling(UiElement scroller) => _scrollCurves.TryGetValue(scroller, out var curve) && curve.Kind == ScrollKind.Autoscroll;

    /// <summary>Input is still scrolling this scroller (a wheel turn, a held key or scrollbar): its scrollend waits.</summary>
    internal void HoldScroll(UiElement scroller) => _scrollHolds[scroller] = _scrollHolds.GetValueOrDefault(scroller) + 1;

    internal void ReleaseScroll(UiElement scroller)
    {
        if (!_scrollHolds.TryGetValue(scroller, out var count))
            return;
        if (count <= 1)
            _scrollHolds.Remove(scroller);
        else
            _scrollHolds[scroller] = count - 1;
        Repaint();
    }

    void SetScrollOffset(UiElement scroller, Vector2 offset)
    {
        // Chromium 152 scrolls by whole device pixels (FractionalScrollOffsets is off:
        // ScrollableArea::ScrollOffsetChanged rounds the offset, halves away from zero).
        offset = Rounded(offset);
        if (LayoutScrollOffsetOf(scroller) == offset)
            return;
        if (scroller == Root)
            Layout.ViewportScroll = offset;
        else
        {
            scroller.SetScrollOffsets(offset.X, offset.Y);
            if (offset == Vector2.Zero)
                _scrolledElements.Remove(scroller);
            else
                _scrolledElements.Add(scroller);
        }
        if (!_scrollEventQueue.Contains(scroller))
            _scrollEventQueue.Add(scroller);
        _scrollEndPending.Add(scroller);
        _stickyDirty = true;
        // ScrollableArea::ScrollOffsetChanged dirties the hover state.
        _hoverDirty = true;
        Repaint();
    }

    /// <summary>Advances the running scroll animations to the document's time.</summary>
    void AdvanceScrolling(double time)
    {
        if (_scrollCurves.Count == 0)
            return;
        foreach (var (scroller, curve) in _scrollCurves.ToList())
        {
            if (scroller.Document != this)
            {
                _scrollCurves.Remove(scroller);
                continue;
            }
            SetScrollOffset(scroller, Vector2.Clamp(curve.ValueAt(time), Vector2.Zero, LayoutMaxScroll(scroller)));
            if (curve.Done(time))
                _scrollCurves.Remove(scroller);
        }
    }

    /// <summary>Something about scrolling needs another frame (animations, events to fire).</summary>
    bool ScrollingPending => _scrollCurves.Count > 0 || _scrollEventQueue.Count > 0 || _scrollEndPending.Count > 0;

    /// <summary>After a layout, scroll positions beyond what can scroll now are pulled back (as Chromium clamps).</summary>
    void ClampScrollOffsets()
    {
        foreach (var element in _scrolledElements.ToList())
        {
            if (element.Document != this)
            {
                _scrolledElements.Remove(element);
                continue;
            }
            // An element without a scrolling box keeps its position for when it gets one back.
            if (element.Box is not { IsScrollContainer: true })
                continue;
            var offset = LayoutScrollOffsetOf(element);
            var clamped = Vector2.Clamp(offset, Vector2.Zero, LayoutMaxScroll(element));
            if (clamped != offset)
                SetScrollOffset(element, clamped);
        }
        var viewportOffset = Layout.ViewportScroll;
        var viewportClamped = Vector2.Clamp(viewportOffset, Vector2.Zero, LayoutMaxScroll(Root));
        if (viewportClamped != viewportOffset)
            SetScrollOffset(Root, viewportClamped);
    }

    /// <summary>Removed elements forget their scroll positions and animations (as Chromium's do).</summary>
    void ForgetScrolling(UiElement removed)
    {
        removed.SetScrollOffsets(0, 0);
        _scrolledElements.Remove(removed);
        _scrollCurves.Remove(removed);
        _scrollEndPending.Remove(removed);
        _scrollEventQueue.Remove(removed);
        _scrollHolds.Remove(removed);
    }

    /// <summary>
    /// The frame's scroll events: a scroll event for each scroller that moved since the last frame
    /// (the document's at the document), then scrollend for those that stopped moving.
    /// </summary>
    void DispatchScrollEvents()
    {
        bool EndsNow(UiElement scroller) =>
            _scrollEndPending.Contains(scroller) && !_scrollCurves.ContainsKey(scroller) && !_scrollHolds.ContainsKey(scroller) && !_scrollEventQueue.Contains(scroller);
        void End(UiElement scroller)
        {
            _scrollEndPending.Remove(scroller);
            if (scroller.Document == this)
                FireScrollEvent(scroller, "scrollend");
        }
        if (_scrollEventQueue.Count > 0)
        {
            var scrolled = _scrollEventQueue.ToList();
            _scrollEventQueue.Clear();
            // A scroll that ends at once (an instant programmatic one) queues its scrollend right after its scroll.
            foreach (var scroller in scrolled)
            {
                if (scroller.Document == this)
                    FireScrollEvent(scroller, "scroll");
                if (EndsNow(scroller))
                    End(scroller);
            }
        }
        foreach (var scroller in _scrollEndPending.ToList())
            if (EndsNow(scroller))
                End(scroller);
    }

    /// <summary>A one-line field's inner editor scrolled: the field gets its scroll events (they're its inner editor's, retargeted).</summary>
    internal void InnerEditorScrolled(UiElement field)
    {
        if (!_scrollEventQueue.Contains(field))
            _scrollEventQueue.Add(field);
        _scrollEndPending.Add(field);
        Repaint();
    }

    void FireScrollEvent(UiElement scroller, string type)
    {
        var e = new UiEvent(type, bubbles: scroller == Root, cancelable: false) { IsTrusted = true };
        if (scroller == Root)
            DispatchEvent(e);
        else
            scroller.DispatchEvent(e);
    }

    // ---------- Scroll into view ----------

    /// <summary>
    /// Scrolls every scroller around an element so that it shows (Element.scrollIntoView, as
    /// Blink does it): from the nearest scroller out to the document, each one aligns the element's
    /// border box (grown by its scroll-margin) in its scrollport (shrunk by its scroll-padding).
    /// </summary>
    public void ScrollIntoView(UiElement element, ScrollAlign block = ScrollAlign.Start, ScrollAlign inline = ScrollAlign.Nearest, ScrollMode mode = ScrollMode.Auto)
    {
        UpdateLayout();
        if (element.Document != this || element.Box is not { } box)
            return;
        var s = box.Style;
        var rect = LayoutBoundingClientRect(element);
        var margin = new Edges(s.ScrollMarginTop, s.ScrollMarginRight, s.ScrollMarginBottom, s.ScrollMarginLeft);
        ScrollRectIntoView(box, rect, margin, block, inline, mode);
    }

    /// <summary>Scrolls every scroller around a box, from the nearest out to the document, to show a rectangle (viewport units) inside the box.</summary>
    internal void ScrollRectIntoView(LayoutBox box, RectF rect, Edges margin, ScrollAlign block, ScrollAlign inline, ScrollMode mode)
    {
        for (var container = box.Container; ; container = container?.Container)
        {
            UiElement scroller;
            RectF scrollport;
            if (container is null || container.IsViewport)
            {
                scroller = Root;
                var viewport = Layout.Viewport;
                scrollport = new RectF(0, 0, viewport.PaddingBoxWidth, viewport.PaddingBoxHeight);
            }
            else if (container.IsScrollContainer && container.Element is { } owner)
            {
                scroller = owner;
                var r = container.ViewportRect;
                scrollport = new RectF(r.X + container.Border.Left, r.Y + container.Border.Top, container.PaddingBoxWidth, container.PaddingBoxHeight);
            }
            else
                continue;
            rect = ScrollIntoViewIn(scroller, scrollport, rect, margin, block, inline, mode);
            if (scroller == Root)
                break;
            // A fixed box doesn't move with the document.
            if (container!.Style.Position == PositionKind.Fixed && container.Container is null)
                break;
        }
    }

    /// <summary>Scrolls one scroller to show a rectangle (viewport units); returns where it shows afterwards, cut to the scrollport.</summary>
    RectF ScrollIntoViewIn(UiElement scroller, RectF scrollport, RectF target, Edges margin, ScrollAlign block, ScrollAlign inline, ScrollMode mode)
    {
        var style = scroller.Style;
        var current = LayoutScrollOffsetOf(scroller);
        // The snapport: the scrollport without its scroll-padding.
        var snapport = scrollport;
        if (style is not null)
            snapport = RectF.FromEdges(
                snapport.Left + style.ScrollPaddingLeft.ResolveOr(scrollport.Width, 0), snapport.Top + style.ScrollPaddingTop.ResolveOr(scrollport.Height, 0),
                snapport.Right - style.ScrollPaddingRight.ResolveOr(scrollport.Width, 0), snapport.Bottom - style.ScrollPaddingBottom.ResolveOr(scrollport.Height, 0));
        if (snapport.Width <= 0)
            snapport = snapport with { Width = 1 / 64f };
        if (snapport.Height <= 0)
            snapport = snapport with { Height = 1 / 64f };
        var expose = RectF.FromEdges(target.Left - margin.Left, target.Top - margin.Top, target.Right + margin.Right, target.Bottom + margin.Bottom);

        // Blink's ScrollAlignment: start, center and end always scroll; nearest doesn't when the
        // target (with its margin) shows fully, else goes to the closest edge.
        static float Axis(ScrollAlign align, float portStart, float portEnd, float start, float end, float offset)
        {
            var portLength = portEnd - portStart;
            var length = end - start;
            var intersection = MathF.Min(portEnd, end) - MathF.Max(portStart, start);
            if (align == ScrollAlign.Nearest)
            {
                if (intersection >= 0 && (MathF.Abs(intersection - portLength) < 1e-3f || MathF.Abs(intersection - length) < 1e-3f))
                    return offset;
                var toEnd = (end > portEnd && length < portLength) || (end < portEnd && length > portLength);
                align = toEnd ? ScrollAlign.End : ScrollAlign.Start;
            }
            return align switch
            {
                ScrollAlign.End => offset + end - portEnd,
                ScrollAlign.Center => offset + (start + end - (portStart + portEnd)) / 2,
                _ => offset + start - portStart,
            };
        }

        var x = Axis(inline, snapport.Left, snapport.Right, expose.Left, expose.Right, current.X);
        var y = Axis(block, snapport.Top, snapport.Bottom, expose.Top, expose.Bottom, current.Y);
        // An element rounds the offset first (PaintLayerScrollableArea::ScrollIntoView); the
        // document takes it unrounded (RootFrameViewport::ScrollIntoView).
        var unrounded = new Vector2(x, y);
        var offset = Vector2.Clamp(scroller == Root ? unrounded : Rounded(unrounded), Vector2.Zero, LayoutMaxScroll(scroller));
        LayoutScrollTo(scroller, offset, mode);
        // Where the target shows once scrolled, cut to what the scroller shows (all of it if none shows).
        var moved = Rounded(offset) - current;
        var after = new RectF(target.X - moved.X, target.Y - moved.Y, target.Width, target.Height);
        var visible = after.Intersect(scrollport);
        return visible.Width > 0 && visible.Height > 0 ? visible : after;
    }

    // ---------- Geometry ----------

    /// <summary>
    /// An element's border box as getBoundingClientRect gives it, in CSS px: the bounds of the box
    /// after its and its ancestors' transforms (inline boxes: of all their fragments), scaled by
    /// one over the zoom. Empty for elements without boxes.
    /// </summary>
    public RectF BoundingClientRect(UiElement element) => Unzoomed(LayoutBoundingClientRect(element));

    /// <summary>getClientRects in CSS px.</summary>
    public IReadOnlyList<RectF> ClientRects(UiElement element) => LayoutClientRects(element).Select(Unzoomed).ToList();

    RectF Unzoomed(RectF rect)
    {
        var zoom = Zoom;
        return zoom == 1 ? rect : new RectF(rect.X / zoom, rect.Y / zoom, rect.Width / zoom, rect.Height / zoom);
    }

    /// <summary>The bounding client rect in device pixels.</summary>
    internal RectF LayoutBoundingClientRect(UiElement element)
    {
        UpdateLayout();
        if (element.Document != this)
            return default;
        if (element.Box is not { } box)
            return SvgClientRect(element) ?? default;
        var rects = LocalRects(box);
        if (rects.Count == 0)
            return default;
        var transform = TransformOf(box);
        RectF? bounds = null;
        foreach (var rect in rects)
        {
            var mapped = rect.Transform(transform);
            bounds = bounds is { } b ? RectF.FromEdges(MathF.Min(b.Left, mapped.Left), MathF.Min(b.Top, mapped.Top), MathF.Max(b.Right, mapped.Right), MathF.Max(b.Bottom, mapped.Bottom)) : mapped;
        }
        return bounds ?? default;
    }

    /// <summary>getClientRects in device pixels: a rectangle per fragment (one for block-level boxes, none without a box).</summary>
    internal IReadOnlyList<RectF> LayoutClientRects(UiElement element)
    {
        UpdateLayout();
        if (element.Document != this)
            return [];
        if (element.Box is not { } box)
            return SvgClientRect(element) is { } svgRect ? [svgRect] : [];
        var transform = TransformOf(box);
        return LocalRects(box).Select(r => r.Transform(transform)).ToList();
    }

    /// <summary>
    /// An element inside an inline svg (Element::ClientQuads for SVG elements): its object bounding box
    /// through its transforms, the svg's viewBox and the CSS transforms of the svg's box, in device
    /// pixels; null when it isn't rendered.
    /// </summary>
    RectF? SvgClientRect(UiElement element)
    {
        for (var ancestor = element.Parent; ancestor is not null; ancestor = ancestor.Parent)
            if (ancestor.Tag == "svg" && ancestor.Box is { Type: BoxType.Replaced } box)
            {
                _hitPainter ??= new UiPainter(this);
                return _hitPainter.SvgElementGeometry(box, element) is { } geometry ? geometry.Box.Transform(geometry.ToViewport * TransformOf(box)) : null;
            }
        return null;
    }

    /// <summary>An element's border-box rectangles in viewport units, before transforms (offsetWidth and friends).</summary>
    internal IReadOnlyList<RectF> ClientRectsUntransformed(UiElement element) =>
        element.Document == this && element.Box is { } box ? LocalRects(box) : [];

    /// <summary>A box's border-box rectangles in viewport units before transforms.</summary>
    static List<RectF> LocalRects(LayoutBox box)
    {
        // An inline box (and a line break: its place at the line's end, as tall as its font) is its fragments.
        if (box.Type is BoxType.Inline or BoxType.LineBreak && box.InlineRoot is { } root && box.Fragments is { Count: > 0 } fragments)
        {
            var origin = root.ViewportPosition;
            var offset = Vector2.Zero;
            for (var b = box; b is not null && b != root; b = b.Parent)
                if (b.Type == BoxType.Inline)
                    offset += b.RelativeOffset + b.StickyOffset;
            return fragments.Select(f => new RectF(origin.X + offset.X + f.Rect.X, origin.Y + offset.Y + f.Rect.Y, f.Rect.Width, f.Rect.Height)).ToList();
        }
        return [box.ViewportRect];
    }

    /// <summary>The transform a box is painted with: its own and its ancestors' CSS transforms.</summary>
    internal static Matrix3x2 TransformOf(LayoutBox box)
    {
        var transform = Matrix3x2.Identity;
        for (var b = box; b is not null; b = b.Parent)
            if (b.Type is not (BoxType.Text or BoxType.LineBreak) && b.Style.HasTransform)
                transform *= UiPainter.TransformFor(b, b.ViewportRect);
        return transform;
    }

    // ---------- Scrollbars ----------

    /// <summary>A scrollbar under the pointer: whose it is (the root for the viewport's), its geometry, the part, and the map into its space.</summary>
    internal readonly record struct ScrollbarHit(UiElement Scroller, ScrollbarGeometry Geometry, ScrollbarPart Part, Matrix3x2 ToLocal);

    (UiElement Scroller, bool Vertical, ScrollbarPart Part)? _scrollbarHover, _scrollbarPress;

    /// <summary>The scrollbar part shown hovered.</summary>
    internal (UiElement Scroller, bool Vertical, ScrollbarPart Part)? ScrollbarHover
    {
        get => _scrollbarHover;
        set
        {
            if (_scrollbarHover == value)
                return;
            _scrollbarHover = value;
            Repaint();
        }
    }

    /// <summary>The scrollbar part shown pressed.</summary>
    internal (UiElement Scroller, bool Vertical, ScrollbarPart Part)? ScrollbarPress
    {
        get => _scrollbarPress;
        set
        {
            if (_scrollbarPress == value)
                return;
            _scrollbarPress = value;
            Repaint();
        }
    }

    internal (ScrollbarPart Hovered, ScrollbarPart Pressed) ScrollbarStatesOf(UiElement scroller, bool vertical)
    {
        var hovered = _scrollbarHover is { } h && h.Scroller == scroller && h.Vertical == vertical ? h.Part : ScrollbarPart.None;
        var pressed = _scrollbarPress is { } p && p.Scroller == scroller && p.Vertical == vertical ? p.Part : ScrollbarPart.None;
        return (hovered, pressed);
    }

    /// <summary>Is the point over the viewport's scrollbars or their corner?</summary>
    internal bool OverViewportScrollbars(Vector2 point)
    {
        var viewport = Layout.Viewport;
        return point.X >= 0 && point.Y >= 0 && point.X < viewport.Width && point.Y < viewport.Height
            && (point.X >= viewport.Width - viewport.VerticalScrollbar || point.Y >= viewport.Height - viewport.HorizontalScrollbar);
    }

    /// <summary>A scroller's scrollbar as it is now (viewport units, before the scroller's transforms).</summary>
    internal ScrollbarGeometry? ScrollbarOf(UiElement scroller, bool vertical)
    {
        UpdateLayout();
        if (scroller == Root)
        {
            var (v, h) = ScrollbarGeometry.ForViewport(Layout.Viewport, Root.Style?.ScrollbarWidth == "thin", DeviceScale);
            return vertical ? v : h;
        }
        if (scroller.Box is not { IsScrollContainer: true } box)
            return null;
        var (ev, eh) = ScrollbarGeometry.For(box, SnapRect(box.ViewportRect), scroller.LayoutScroll.X, scroller.LayoutScroll.Y, DeviceScale);
        return vertical ? ev : eh;
    }

    static RectF SnapRect(RectF r)
    {
        static float S(float v) => MathF.Round(v, MidpointRounding.AwayFromZero);
        return RectF.FromEdges(S(r.Left), S(r.Top), S(r.Right), S(r.Bottom));
    }

    /// <summary>The scrollbar at a viewport point: the viewport's, or one of the scroll container painted on top there.</summary>
    internal ScrollbarHit? ScrollbarAt(Vector2 point)
    {
        UpdateLayout();
        if (OverViewportScrollbars(point))
        {
            foreach (var vertical in new[] { true, false })
                if (ScrollbarOf(Root, vertical) is { } bar && ScrollbarGeometry.Contains(bar.Bar, point))
                    return new ScrollbarHit(Root, bar, bar.PartAt(point), Matrix3x2.Identity);
            return null;
        }
        if (HitTest(point) is not { Box: { IsScrollContainer: true } box } hit || box.Element is not { } scroller || box.Style.Visibility != Visibility.Visible)
            return null;
        if (!Matrix3x2.Invert(hit.Transform, out var toLocal))
            return null;
        var local = Vector2.Transform(point, toLocal);
        foreach (var vertical in new[] { true, false })
            if (ScrollbarOf(scroller, vertical) is { } bar && ScrollbarGeometry.Contains(bar.Bar, local))
                return new ScrollbarHit(scroller, bar, bar.PartAt(local), toLocal);
        return null;
    }

    // ---------- Sticky positioning ----------

    /// <summary>After a layout: the sticky boxes, in tree order (outer ones first); boxes no longer sticky lose their offsets.</summary>
    void CollectStickyBoxes()
    {
        var viewport = Layout.Viewport;
        var list = viewport.StickyBoxes ??= [];
        list.Clear();
        var stack = new Stack<LayoutBox>();
        for (var i = viewport.Children.Count - 1; i >= 0; i--)
            stack.Push(viewport.Children[i]);
        while (stack.Count > 0)
        {
            var box = stack.Pop();
            if (box.Type is not (BoxType.Text or BoxType.LineBreak))
            {
                if (!box.IsAnonymous && box.Style.Position == PositionKind.Sticky)
                    list.Add(box);
                else if (box.StickyOffset != Vector2.Zero)
                {
                    box.StickyOffset = Vector2.Zero;
                    Repaint();
                }
            }
            for (var i = box.Children.Count - 1; i >= 0; i--)
                stack.Push(box.Children[i]);
        }
        _stickyDirty = true;
    }

    /// <summary>Recomputes the sticky offsets (after layout and whenever something scrolled).</summary>
    void UpdateStickyOffsets()
    {
        _stickyDirty = false;
        if (Layout.Viewport.StickyBoxes is not { Count: > 0 } boxes)
            return;
        var changed = false;
        foreach (var box in boxes)
        {
            var offset = StickyOffsetOf(box);
            if (offset != box.StickyOffset)
            {
                box.StickyOffset = offset;
                changed = true;
            }
        }
        if (changed)
            Repaint();
    }

    /// <summary>
    /// position: sticky as Blink computes it (StickyPositionScrollingConstraints): the box shifts to
    /// keep its inset from the nearest scroll container's content box (its padding box without
    /// padding), staying inside its containing block's content box less its own margins. Top wins
    /// over bottom and left over right.
    /// </summary>
    Vector2 StickyOffsetOf(LayoutBox box)
    {
        LayoutBox? scroller = null;
        for (var c = box.Container; c is not null; c = c.Container)
            if (c.IsScrollContainer)
            {
                scroller = c;
                break;
            }
        RectF constraining;
        if (scroller is null)
        {
            var viewport = Layout.Viewport;
            constraining = new RectF(0, 0, viewport.PaddingBoxWidth, viewport.PaddingBoxHeight);
        }
        else
        {
            var r = scroller.ViewportRect;
            constraining = new RectF(r.X + scroller.ContentLeft, r.Y + scroller.ContentTop,
                MathF.Max(0, scroller.PaddingBoxWidth - scroller.Padding.Horizontal), MathF.Max(0, scroller.PaddingBoxHeight - scroller.Padding.Vertical));
        }

        // The sticky container: the containing block (anonymous ones skipped).
        var container = box.Container;
        while (container is { IsAnonymous: true, IsViewport: false })
            container = container.Container;
        RectF block;
        if (container is null || container.IsViewport)
        {
            var viewport = Layout.Viewport;
            var scroll = viewport.DocumentScroll;
            block = new RectF(-scroll.X, -scroll.Y, MathF.Max(viewport.ScrollWidth, viewport.PaddingBoxWidth), MathF.Max(viewport.ScrollHeight, viewport.PaddingBoxHeight));
        }
        else if (container == scroller && container.Element is { } owner)
        {
            // The scroller's whole scrollable overflow, less its padding.
            var r = container.ViewportRect;
            var x = r.X + container.Border.Left - owner.LayoutScroll.X;
            var y = r.Y + container.Border.Top - owner.LayoutScroll.Y;
            block = RectF.FromEdges(x + container.Padding.Left, y + container.Padding.Top,
                x + MathF.Max(container.ScrollWidth, container.PaddingBoxWidth) - container.Padding.Right,
                y + MathF.Max(container.ScrollHeight, container.PaddingBoxHeight) - container.Padding.Bottom);
        }
        else
        {
            var r = container.ViewportRect;
            block = new RectF(r.X + container.ContentLeft, r.Y + container.ContentTop, container.ContentWidth, container.ContentHeight);
        }
        // Less the sticky box's margins (Blink still applies them: LayoutIgnoreMarginsForSticky is off).
        var s = box.Style;
        var basis = container?.ContentWidth ?? 0;
        static float Margin(Length l, float basis) => l.IsAuto ? 0 : l.ResolveOr(basis, 0);
        block = RectF.FromEdges(block.Left + Margin(s.MarginLeft, basis), block.Top + Margin(s.MarginTop, basis),
            block.Right - Margin(s.MarginRight, basis), block.Bottom - Margin(s.MarginBottom, basis));

        // The box where it would be without its own sticky offset (its ancestors' apply already).
        RectF own;
        if (box.Type == BoxType.Inline)
        {
            var rects = LocalRects(box);
            own = rects.Count == 0 ? default : rects.Aggregate((a, b) => RectF.FromEdges(MathF.Min(a.Left, b.Left), MathF.Min(a.Top, b.Top), MathF.Max(a.Right, b.Right), MathF.Max(a.Bottom, b.Bottom)));
        }
        else
            own = box.ViewportRect;
        var normal = new RectF(own.X - box.StickyOffset.X, own.Y - box.StickyOffset.Y, own.Width, own.Height);

        var dx = AxisOffset(s.Left, s.Right, constraining.Left, constraining.Right, normal.Left, normal.Right, block.Left, block.Right);
        var dy = AxisOffset(s.Top, s.Bottom, constraining.Top, constraining.Bottom, normal.Top, normal.Bottom, block.Top, block.Bottom);
        return new Vector2(dx, dy);
    }

    /// <summary>StickyPositionScrollingConstraints::PerAxisData::ComputeOffset along one axis.</summary>
    static float AxisOffset(Length minLength, Length maxLength, float contentStart, float contentEnd, float boxStart, float boxEnd, float blockStart, float blockEnd)
    {
        var available = contentEnd - contentStart;
        float? minInset = minLength.IsAuto ? null : minLength.ResolveOr(available, 0);
        float? maxInset = maxLength.IsAuto ? null : maxLength.ResolveOr(available, 0);
        // Not enough room for both insets: the end one gives way.
        if (minInset is { } a && maxInset is { } b)
        {
            var free = available - (boxEnd - boxStart) - a - b;
            if (free < 0)
                maxInset = b + free;
        }
        float offset = 0;
        if (maxInset is { } max)
        {
            var delta = MathF.Min(0, contentEnd - max - boxEnd);
            var room = MathF.Min(0, blockStart - boxStart);
            if (delta < room)
                delta = room;
            offset += delta;
        }
        if (minInset is { } min)
        {
            var delta = MathF.Max(0, contentStart + min - boxStart);
            var room = MathF.Max(0, blockEnd - boxEnd);
            if (delta > room)
                delta = room;
            offset += delta;
        }
        return offset;
    }
}

/// <summary>
/// A scroll animation as Chromium's cc::ScrollOffsetAnimationCurve runs it: an ease-in-out from
/// where the scroller is to its target, retargeted (keeping its speed) when more input comes in;
/// or a linear autoscroll at a constant speed. Times are in seconds on the document's clock.
/// </summary>
internal sealed class ScrollCurve
{
    const double FramesPerSecond = 60, ConstantFrames = 9, MaxProgrammaticFrames = 90;
    const double RampStart = 120, RampEnd = 480, MinFrames = 6, MaxFrames = 12;
    const double Epsilon = 0.01;

    public ScrollKind Kind;
    public Vector2 Initial, Target;
    double _origin, _total, _lastRetarget;
    CubicBezier _timing;
    float _velocity;

    public static ScrollCurve Create(ScrollKind kind, Vector2 from, Vector2 to, double now, float velocity = 0)
    {
        var curve = new ScrollCurve { Kind = kind, Initial = from, Target = to, _origin = now, _velocity = velocity };
        curve._timing = curve.Easing(null);
        curve._total = curve.SegmentDuration(to - from);
        return curve;
    }

    static double MaximumDimension(Vector2 delta) => MathF.Abs(delta.X) > MathF.Abs(delta.Y) ? delta.X : delta.Y;

    double SegmentDuration(Vector2 delta)
    {
        var distance = Math.Abs(MaximumDimension(delta));
        switch (Kind)
        {
            case ScrollKind.Programmatic:
                return Math.Min(Math.Sqrt(distance), MaxProgrammaticFrames) / FramesPerSecond;
            case ScrollKind.Keyboard:
                return ConstantFrames / FramesPerSecond;
            case ScrollKind.Wheel:
            {
                var slope = (MinFrames - MaxFrames) / (RampEnd - RampStart);
                var frames = Math.Clamp(MaxFrames - RampStart * slope + distance * slope, MinFrames, MaxFrames);
                return frames / FramesPerSecond;
            }
            default:
                return _velocity > 0 ? distance / _velocity : 0;
        }
    }

    CubicBezier Easing(double? slope)
    {
        if (Kind == ScrollKind.Autoscroll)
            return new CubicBezier(0, 0, 1, 1);
        var (x1, y1, x2, y2) = Kind == ScrollKind.Programmatic ? (0.4, 0.0, 0.0, 1.0) : (0.42, 0.0, 0.58, 1.0);
        if (slope is { } s)
        {
            s = Math.Clamp(s, -1000, 1000);
            return new CubicBezier(x1, x1 * s, x2, y2);
        }
        return new CubicBezier(x1, y1, x2, y2);
    }

    public Vector2 ValueAt(double now)
    {
        var t = now - _origin - _lastRetarget;
        var duration = _total - _lastRetarget;
        if (duration <= 0 || t >= duration)
            return Target;
        if (t <= 0)
            return Initial;
        var progress = (float)(Kind == ScrollKind.Autoscroll ? t / duration : _timing.Value(t / duration));
        return Initial + (Target - Initial) * progress;
    }

    public bool Done(double now) => now - _origin >= _total;

    /// <summary>The speed along the main axis (px/s) at curve time <paramref name="t"/>.</summary>
    double VelocityAt(double t)
    {
        var duration = _total - _lastRetarget;
        if (duration <= 0)
            return 0;
        var slope = _timing.Slope((t - _lastRetarget) / duration);
        return slope * (MaximumDimension(Target - Initial) / duration);
    }

    /// <summary>ScrollOffsetAnimationCurve::UpdateTarget: head for a new target from where the scroll is now, at its current speed.</summary>
    public void UpdateTarget(double now, Vector2 newTarget)
    {
        if (Kind == ScrollKind.Autoscroll)
            return;
        var t = Math.Max(now - _origin, _lastRetarget);
        if (Math.Abs(MaximumDimension(Target - newTarget)) < Epsilon)
        {
            Target = newTarget;
            return;
        }
        var current = ValueAt(_origin + t);
        var newDelta = newTarget - current;
        if (Math.Abs(MaximumDimension(newDelta)) < Epsilon)
        {
            _lastRetarget = t;
            _total = t;
            Target = newTarget;
            return;
        }
        if (_total - _lastRetarget <= 0)
        {
            _total = SegmentDuration(newDelta);
            Target = newTarget;
            return;
        }
        var velocity = VelocityAt(t);
        var newDuration = Math.Min(SegmentDuration(newDelta), VelocityBound(velocity, newDelta));
        if (newDuration < Epsilon)
        {
            Target = newTarget;
            _total = t;
            return;
        }
        var newSlope = velocity * (newDuration / MaximumDimension(newDelta));
        _timing = Easing(newSlope);
        Initial = current;
        Target = newTarget;
        _total = t + newDuration;
        _lastRetarget = t;
    }

    static double VelocityBound(double velocity, Vector2 newDelta)
    {
        var distance = MaximumDimension(newDelta);
        if (Math.Abs(distance) < Epsilon)
            return 0;
        if (Math.Abs(velocity) < Epsilon)
            return double.PositiveInfinity;
        var bound = distance / velocity * 2.5;
        return bound < 0 ? double.PositiveInfinity : bound;
    }
}

/// <summary>A CSS cubic Bézier easing in double precision with its slope (gfx::CubicBezier).</summary>
internal readonly struct CubicBezier
{
    readonly double _ax, _bx, _cx, _ay, _by, _cy;

    public CubicBezier(double x1, double y1, double x2, double y2)
    {
        _cx = 3 * x1;
        _bx = 3 * (x2 - x1) - _cx;
        _ax = 1 - _cx - _bx;
        _cy = 3 * y1;
        _by = 3 * (y2 - y1) - _cy;
        _ay = 1 - _cy - _by;
    }

    double SampleX(double t) => ((_ax * t + _bx) * t + _cx) * t;
    double SampleY(double t) => ((_ay * t + _by) * t + _cy) * t;
    double DerivativeX(double t) => (3 * _ax * t + 2 * _bx) * t + _cx;
    double DerivativeY(double t) => (3 * _ay * t + 2 * _by) * t + _cy;

    /// <summary>The curve parameter where x(t) = x (Newton's method, then bisection).</summary>
    double Solve(double x)
    {
        var t = x;
        for (var i = 0; i < 8; i++)
        {
            var error = SampleX(t) - x;
            if (Math.Abs(error) < 1e-7)
                return t;
            var d = DerivativeX(t);
            if (Math.Abs(d) < 1e-6)
                break;
            t -= error / d;
        }
        double lo = 0, hi = 1;
        t = x;
        while (hi - lo > 1e-9)
        {
            var value = SampleX(t);
            if (Math.Abs(value - x) < 1e-7)
                return t;
            if (x > value)
                lo = t;
            else
                hi = t;
            t = (hi - lo) / 2 + lo;
        }
        return t;
    }

    public double Value(double x) => x <= 0 ? 0 : x >= 1 ? 1 : SampleY(Solve(x));

    public double Slope(double x)
    {
        var t = Solve(Math.Clamp(x, 0, 1));
        var dx = DerivativeX(t);
        var dy = DerivativeY(t);
        if (dx == 0 && dy == 0)
            return 0;
        return dy / dx;
    }
}

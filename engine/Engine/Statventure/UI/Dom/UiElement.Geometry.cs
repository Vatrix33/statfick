using System.Numerics;
using Statventure.Core;
using Statventure.UI.Layout;
using Statventure.UI.Style;

namespace Statventure.UI.Dom;

/// <summary>
/// The element's geometry as CSSOM View gives it in Chromium (whole pixels where the DOM has
/// integers), and scrolling it, in CSS px: layout's device pixels over the zoom, rounded as
/// Blink's AdjustForAbsoluteZoom does. Reading geometry brings layout up to date first.
/// </summary>
public partial class UiElement
{
    LayoutBox? LaidOutBox()
    {
        if (Document is not { } document)
            return null;
        document.UpdateLayout();
        return Box;
    }

    bool IsRoot => Document?.Root == this;

    /// <summary>AdjustForAbsoluteZoom::AdjustLayoutUnit then LayoutUnit::Round: the value over the zoom in 1/64 px (truncated), rounded half up.</summary>
    static int Adjusted(float value, float zoom)
    {
        var units = (long)(value / zoom * 64);
        return (int)((units + 32) >> 6);
    }

    /// <summary>AdjustForAbsoluteZoom::AdjustInt: a whole number of device pixels over the zoom (half a pixel added when zooming in, then RoundForImpreciseConversion).</summary>
    static int AdjustInt(int value, float zoom)
    {
        if (zoom == 1)
            return value;
        float v = value;
        if (zoom > 1)
            v += value < 0 ? -0.5f : 0.5f;
        double scaled = v / zoom;
        scaled += scaled < 0 ? -0.01 : 0.01;
        return (int)scaled;
    }

    /// <summary>A box that has client and scroll sizes (not inline boxes, not text).</summary>
    static bool HasBoxGeometry(LayoutBox box) => box.Type is not (BoxType.Inline or BoxType.Text or BoxType.LineBreak);

    /// <summary>clientWidth: the padding box without its scrollbar (on the root: the viewport's).</summary>
    public int ClientWidth
    {
        get
        {
            if (LaidOutBox() is not { } box)
                return 0;
            if (IsRoot)
                return AdjustInt((int)Document!.Layout.Viewport.PaddingBoxWidth, Zoom);
            return HasBoxGeometry(box) ? Adjusted(box.PaddingBoxWidth, Zoom) : 0;
        }
    }

    public int ClientHeight
    {
        get
        {
            if (LaidOutBox() is not { } box)
                return 0;
            if (IsRoot)
                return AdjustInt((int)Document!.Layout.Viewport.PaddingBoxHeight, Zoom);
            return HasBoxGeometry(box) ? Adjusted(box.PaddingBoxHeight, Zoom) : 0;
        }
    }

    /// <summary>clientLeft: the left border's width.</summary>
    public int ClientLeft => LaidOutBox() is { } box && HasBoxGeometry(box) ? Adjusted(box.Border.Left, Zoom) : 0;

    public int ClientTop => LaidOutBox() is { } box && HasBoxGeometry(box) ? Adjusted(box.Border.Top, Zoom) : 0;

    /// <summary>scrollWidth: the scrollable overflow's width (at least the client width; on the root: the document's).</summary>
    public int ScrollWidth
    {
        get
        {
            if (LaidOutBox() is not { } box)
                return 0;
            if (IsRoot)
            {
                var viewport = Document!.Layout.Viewport;
                return AdjustInt((int)MathF.Round(MathF.Max(viewport.ScrollWidth, viewport.PaddingBoxWidth), MidpointRounding.AwayFromZero), Zoom);
            }
            if (!HasBoxGeometry(box))
                return 0;
            var width = box.IsScrollContainer ? box.ScrollWidth : Document!.Layout.ScrollableOverflow(box).X;
            return Adjusted(MathF.Max(width, box.PaddingBoxWidth), Zoom);
        }
    }

    public int ScrollHeight
    {
        get
        {
            if (LaidOutBox() is not { } box)
                return 0;
            if (IsRoot)
            {
                var viewport = Document!.Layout.Viewport;
                return AdjustInt((int)MathF.Round(MathF.Max(viewport.ScrollHeight, viewport.PaddingBoxHeight), MidpointRounding.AwayFromZero), Zoom);
            }
            if (!HasBoxGeometry(box))
                return 0;
            var height = box.IsScrollContainer ? box.ScrollHeight : Document!.Layout.ScrollableOverflow(box).Y;
            return Adjusted(MathF.Max(height, box.PaddingBoxHeight), Zoom);
        }
    }

    /// <summary>offsetWidth: the border box's width (inline boxes: of their fragments' bounds).</summary>
    public int OffsetWidth
    {
        get
        {
            if (LaidOutBox() is null)
                return 0;
            return Adjusted(UntransformedBounds().Width, Zoom);
        }
    }

    public int OffsetHeight
    {
        get
        {
            if (LaidOutBox() is null)
                return 0;
            return Adjusted(UntransformedBounds().Height, Zoom);
        }
    }

    /// <summary>
    /// offsetParent (Chromium's LayoutObject::OffsetParent): the nearest ancestor that contains
    /// absolutely positioned boxes (positioned, or transformed or filtered), else the body; for
    /// static elements also a table or table cell. Null for the root, the body, elements without a
    /// box, and fixed elements without a containing ancestor.
    /// </summary>
    public UiElement? OffsetParent
    {
        get
        {
            if (LaidOutBox() is not { } box || IsRoot || this == Document!.Body)
                return null;
            var fixedPosition = box.Style.Position == PositionKind.Fixed;
            var positioned = box.Style.Position != PositionKind.Static;
            for (var ancestor = Parent; ancestor is not null; ancestor = ancestor.Parent)
            {
                if (ancestor.Style is not { } s || ancestor.Box is null)
                    continue;
                if (fixedPosition)
                {
                    if (StyleDifference.ContainsFixed(s))
                        return ancestor;
                }
                else
                {
                    if (StyleDifference.ContainsPositioned(s) || ancestor == Document.Body)
                        return ancestor;
                    if (!positioned && ancestor.Tag is "table" or "td" or "th")
                        return ancestor;
                }
                fixedPosition |= s.Position == PositionKind.Fixed;
            }
            return null;
        }
    }

    /// <summary>offsetLeft: from the offset parent's padding edge to the border box (scrolling and transforms aside).</summary>
    public int OffsetLeft => Offset().X;

    public int OffsetTop => Offset().Y;

    (int X, int Y) Offset()
    {
        if (LaidOutBox() is null)
            return (0, 0);
        var rect = UntransformedBounds();
        var position = rect.Position + ScrollShift(Box!);
        if (OffsetParent is { Box: { } parent } offsetParent && offsetParent != Document!.Body)
        {
            var origin = parent.ViewportPosition + ScrollShift(parent) + new Vector2(parent.Border.Left, parent.Border.Top);
            position -= origin;
        }
        return (Adjusted(position.X, Zoom), Adjusted(position.Y, Zoom));
    }

    /// <summary>The scroll offsets between a box and the viewport (offset* ignore scrolling).</summary>
    static Vector2 ScrollShift(LayoutBox box)
    {
        var shift = Vector2.Zero;
        for (var container = box.Container; container is not null; container = container.Container)
        {
            if (container.IsScrollContainer && container.Element is { } scroller)
                shift += scroller.LayoutScroll;
            else if (container.IsViewport)
                shift += container.DocumentScroll;
        }
        return shift;
    }

    RectF UntransformedBounds()
    {
        var rects = Document!.ClientRectsUntransformed(this);
        if (rects.Count == 0)
            return default;
        var bounds = rects[0];
        foreach (var r in rects)
            bounds = RectF.FromEdges(MathF.Min(bounds.Left, r.Left), MathF.Min(bounds.Top, r.Top), MathF.Max(bounds.Right, r.Right), MathF.Max(bounds.Bottom, r.Bottom));
        return bounds;
    }

    /// <summary>getBoundingClientRect: the border box after transforms, in viewport units.</summary>
    public RectF GetBoundingClientRect() => Document?.BoundingClientRect(this) ?? default;

    /// <summary>getClientRects: a rectangle per box fragment (none without a box).</summary>
    public IReadOnlyList<RectF> GetClientRects() => Document?.ClientRects(this) ?? [];

    /// <summary>Element.scrollTo: scrolls this element (the root: the document) to an offset.</summary>
    public void ScrollTo(float left, float top, ScrollMode mode = ScrollMode.Auto) => Document?.ScrollTo(this, new Vector2(left, top), mode);

    public void ScrollBy(float left, float top, ScrollMode mode = ScrollMode.Auto) => Document?.ScrollBy(this, new Vector2(left, top), mode);

    /// <summary>Element.scrollIntoView: scrolls the scrollers around the element so that it shows.</summary>
    public void ScrollIntoView(ScrollAlign block = ScrollAlign.Start, ScrollAlign inline = ScrollAlign.Nearest, ScrollMode mode = ScrollMode.Auto) =>
        Document?.ScrollIntoView(this, block, inline, mode);
}

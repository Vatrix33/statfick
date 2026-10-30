using System.Numerics;
using Statventure.Core;
using Statventure.UI.Style;

namespace Statventure.UI.Layout;

/// <summary>The parts of a scrollbar, as Chromium names them.</summary>
public enum ScrollbarPart
{
    None,
    /// <summary>The arrow button at the start (up or left).</summary>
    BackButton,
    /// <summary>The arrow button at the end (down or right).</summary>
    ForwardButton,
    /// <summary>The track before the thumb.</summary>
    BackTrack,
    /// <summary>The track after the thumb.</summary>
    ForwardTrack,
    Thumb,
}

/// <summary>
/// A scrollbar laid out as Chromium 152's Fluent scrollbars (Linux and Windows): a 15 px bar (10 px
/// for scrollbar-width: thin) with an 18 px arrow button at each end (12 px thin; half the bar each
/// when it is too short for both), the track between, and in it a thumb as long as the visible
/// share of the content (at least 17 px, 11 px thin), 9 px thick (6 px thin) in the middle of the
/// bar; all those sizes times the device scale (ScaleFromDIP), rounded as Chromium rounds them.
/// Everything is in whole device pixels, as Chromium's are.
/// </summary>
public readonly record struct ScrollbarGeometry(
    RectF Bar, bool Vertical, bool Thin, RectF BackButton, RectF ForwardButton, RectF Track,
    int ThumbPosition, int ThumbLength, int ThumbThickness, int Visible, int Total)
{
    /// <summary>Chromium's Fluent sizes for scrollbar-width: auto (thin is two thirds), in window units.</summary>
    public const int ButtonLength = 18, MinimumThumbLength = 17, ThumbThicknessAuto = 9, TrackThickness = 15;
    const float ThinProportion = 2f / 3f;

    static int Round(float v) => (int)MathF.Round(v, MidpointRounding.AwayFromZero);

    /// <summary>ScrollbarThemeFluent::ScrollbarThickness: the track's thickness times the proportion and the device scale, rounded.</summary>
    public static int Thickness(bool thin, float scale) => Round(TrackThickness * (thin ? ThinProportion : 1f) * scale);

    /// <summary>There is something to scroll (Chromium disables the scrollbar otherwise: no thumb, no input).</summary>
    public bool Enabled => Total > Visible;
    public bool HasThumb => ThumbLength > 0;
    public int TrackLength => (int)(Vertical ? Track.Height : Track.Width);
    public float Proportion => Thin ? ThinProportion : 1f;

    /// <summary>The thumb as painted (narrower than the bar, in its middle).</summary>
    public RectF Thumb
    {
        get
        {
            if (!HasThumb)
                return default;
            var thickness = (int)(Vertical ? Bar.Width : Bar.Height);
            var inset = (thickness - ThumbThickness) / 2;
            return Vertical
                ? new RectF(Bar.X + inset, Track.Y + ThumbPosition, ThumbThickness, ThumbLength)
                : new RectF(Track.X + ThumbPosition, Bar.Y + inset, ThumbLength, ThumbThickness);
        }
    }

    /// <summary>The thumb across the whole bar (where it takes the pointer).</summary>
    public RectF ThumbArea => Vertical
        ? new RectF(Bar.X, Track.Y + ThumbPosition, Bar.Width, ThumbLength)
        : new RectF(Track.X + ThumbPosition, Bar.Y, ThumbLength, Bar.Height);

    public RectF BackTrack => Vertical
        ? new RectF(Track.X, Track.Y, Track.Width, ThumbPosition)
        : new RectF(Track.X, Track.Y, ThumbPosition, Track.Height);

    public RectF ForwardTrack => Vertical
        ? RectF.FromEdges(Track.Left, Track.Top + ThumbPosition + ThumbLength, Track.Right, Track.Bottom)
        : RectF.FromEdges(Track.Left + ThumbPosition + ThumbLength, Track.Top, Track.Right, Track.Bottom);

    /// <summary>How far the content scrolls per pixel the thumb moves (0 when the thumb fills the track).</summary>
    public float ContentPerThumbPixel => TrackLength == ThumbLength ? 0 : (Total - Visible) / (float)(TrackLength - ThumbLength);

    /// <summary>A position along the bar's axis.</summary>
    public float Along(Vector2 point) => Vertical ? point.Y : point.X;

    public RectF RectOf(ScrollbarPart part) => part switch
    {
        ScrollbarPart.BackButton => BackButton,
        ScrollbarPart.ForwardButton => ForwardButton,
        ScrollbarPart.BackTrack => BackTrack,
        ScrollbarPart.ForwardTrack => ForwardTrack,
        ScrollbarPart.Thumb => ThumbArea,
        _ => default,
    };

    /// <summary>The part under a point (none outside the bar, and none while disabled, as in Chromium).</summary>
    public ScrollbarPart PartAt(Vector2 point)
    {
        if (!Enabled || !Contains(Bar, point))
            return ScrollbarPart.None;
        if (Contains(BackButton, point))
            return ScrollbarPart.BackButton;
        if (Contains(ForwardButton, point))
            return ScrollbarPart.ForwardButton;
        if (HasThumb && Contains(ThumbArea, point))
            return ScrollbarPart.Thumb;
        if (Contains(BackTrack, point))
            return ScrollbarPart.BackTrack;
        if (Contains(ForwardTrack, point))
            return ScrollbarPart.ForwardTrack;
        return ScrollbarPart.None;
    }

    public static bool Contains(RectF rect, Vector2 p) => p.X >= rect.Left && p.X < rect.Right && p.Y >= rect.Top && p.Y < rect.Bottom;

    /// <summary>
    /// A scrollbar on <paramref name="bar"/> (snapped to whole pixels) for <paramref name="visible"/>
    /// of <paramref name="total"/> pixels of content, scrolled to <paramref name="offset"/>, at
    /// <paramref name="scale"/> device pixels per window unit.
    /// </summary>
    public static ScrollbarGeometry Of(RectF bar, bool vertical, bool thin, float visible, float total, float offset, float scale = 1)
    {
        bar = RectF.FromEdges(Round(bar.Left), Round(bar.Top), Round(bar.Right), Round(bar.Bottom));
        var proportion = (thin ? ThinProportion : 1f) * scale;
        var length = (int)(vertical ? bar.Height : bar.Width);
        var thickness = (int)(vertical ? bar.Width : bar.Height);

        // Buttons: their length scales with the bar; a bar too short for both splits in half.
        var desired = Round(ButtonLength * proportion);
        var button = length < 2 * desired ? length / 2 : desired;
        var trackLength = Math.Max(0, length - 2 * button);
        RectF back, forward, track;
        if (vertical)
        {
            back = new RectF(bar.X, bar.Y, bar.Width, button);
            forward = new RectF(bar.X, bar.Bottom - button, bar.Width, button);
            track = new RectF(bar.X, bar.Y + button, bar.Width, trackLength);
        }
        else
        {
            back = new RectF(bar.X, bar.Y, button, bar.Height);
            forward = new RectF(bar.Right - button, bar.Y, button, bar.Height);
            track = new RectF(bar.X + button, bar.Y, trackLength, bar.Height);
        }

        // The thumb (cc::ScrollUtils::CalculateScrollbarThumbLength, ScrollbarTheme::ThumbPosition).
        var visibleSize = Round(visible);
        var totalSize = Round(total);
        int thumbLength = 0, position = 0;
        if (totalSize > visibleSize && trackLength > 0)
        {
            var minimum = (int)(MinimumThumbLength * proportion);
            var share = totalSize > 0 ? visibleSize / (float)totalSize : 0;
            thumbLength = Math.Max(Round(share * trackLength), minimum);
            if (thumbLength > trackLength)
                thumbLength = trackLength;
            float size = totalSize - visibleSize;
            var pos = MathF.Max(0, offset) * (trackLength - thumbLength) / size;
            position = pos is > 0 and < 1 ? 1 : (int)pos;
        }
        // The thumb's thickness keeps an even gap to both sides of the bar.
        var thumbThickness = Round(ThumbThicknessAuto * proportion);
        thumbThickness -= (thickness - thumbThickness) % 2;
        return new ScrollbarGeometry(bar, vertical, thin, back, forward, track, position, thumbLength, thumbThickness, visibleSize, totalSize);
    }

    /// <summary>An element's scrollbars, in device pixels (null where it has none); scroll offsets in device pixels.</summary>
    public static (ScrollbarGeometry? Vertical, ScrollbarGeometry? Horizontal) For(LayoutBox box, RectF borderBox, float scrollLeft, float scrollTop, float scale = 1)
    {
        ScrollbarGeometry? vertical = null, horizontal = null;
        var thin = box.Style.ScrollbarWidth == "thin";
        // A bar stops at the scroll corner (RectForVerticalScrollbar and RectForHorizontalScrollbar).
        var corner = CornerOf(box, borderBox);
        // RectForVerticalScrollbar and RectForHorizontalScrollbar: in the pixel-snapped border box, against
        // its right and bottom edges, borders in whole pixels.
        var snapped = RectF.FromEdges(Round(borderBox.Left), Round(borderBox.Top), Round(borderBox.Right), Round(borderBox.Bottom));
        int borderLeft = (int)box.Border.Left, borderTop = (int)box.Border.Top, borderRight = (int)box.Border.Right, borderBottom = (int)box.Border.Bottom;
        if (box.VerticalScrollbar > 0)
            vertical = Of(new RectF(snapped.Right - borderRight - box.VerticalScrollbar, snapped.Y + borderTop, box.VerticalScrollbar,
                    snapped.Height - (borderTop + borderBottom) - corner.Height),
                true, thin, box.PaddingBoxHeight, MathF.Max(box.ScrollHeight, box.PaddingBoxHeight), scrollTop, scale);
        if (box.HorizontalScrollbar > 0)
            horizontal = Of(new RectF(snapped.X + borderLeft, snapped.Bottom - borderBottom - box.HorizontalScrollbar,
                    snapped.Width - (borderLeft + borderRight) - corner.Width, box.HorizontalScrollbar),
                false, thin, box.PaddingBoxWidth, MathF.Max(box.ScrollWidth, box.PaddingBoxWidth), scrollLeft, scale);
        return (vertical, horizontal);
    }

    /// <summary>A scroller with resize other than none has a resizer (LayoutBox::CanResize).</summary>
    public static bool CanResize(LayoutBox box) => box.IsScrollContainer && box.Style.Resize != ResizeKind.None;

    /// <summary>
    /// The scroll corner (PaintLayerScrollableArea::ScrollCornerRect): where the bars don't reach,
    /// when it has both, or a resizer and one.
    /// </summary>
    public static RectF CornerOf(LayoutBox box, RectF borderBox)
    {
        var bars = (box.VerticalScrollbar > 0 ? 1 : 0) + (box.HorizontalScrollbar > 0 ? 1 : 0);
        return bars == 2 || (bars == 1 && CanResize(box)) ? CornerRect(box, borderBox, 0) : default;
    }

    /// <summary>The resizer's square (ResizerCornerRect): the corner, sized by the bars there or, without bars, by the page's scrollbar width.</summary>
    public static RectF ResizerOf(LayoutBox box, RectF borderBox, float pageScrollbar) =>
        CanResize(box) ? CornerRect(box, borderBox, pageScrollbar) : default;

    /// <summary>PaintLayerScrollableArea::CornerRect: the bottom-right square inside the border, as wide as the vertical bar and as tall as the horizontal one (a missing one takes the other's size).</summary>
    static RectF CornerRect(LayoutBox box, RectF borderBox, float pageScrollbar)
    {
        var (width, height) = (box.VerticalScrollbar, box.HorizontalScrollbar) switch
        {
            (0, 0) => (pageScrollbar, pageScrollbar),
            (var v, 0) => (v, v),
            (0, var h) => (h, h),
            var (v, h) => (v, h),
        };
        static float Round(float v) => MathF.Round(v, MidpointRounding.AwayFromZero);
        var right = Round(borderBox.Right - box.Border.Right);
        var bottom = Round(borderBox.Bottom - box.Border.Bottom);
        return RectF.FromEdges(right - width, bottom - height, right, bottom);
    }

    /// <summary>The viewport's scrollbars (along its right and bottom edges); the root element styles them.</summary>
    public static (ScrollbarGeometry? Vertical, ScrollbarGeometry? Horizontal) ForViewport(LayoutBox viewport, bool thin, float scale = 1)
    {
        ScrollbarGeometry? vertical = null, horizontal = null;
        var scroll = viewport.DocumentScroll;
        if (viewport.VerticalScrollbar > 0)
            vertical = Of(new RectF(viewport.PaddingBoxWidth, 0, viewport.VerticalScrollbar, viewport.PaddingBoxHeight), true, thin, viewport.PaddingBoxHeight, viewport.ScrollHeight, scroll.Y, scale);
        if (viewport.HorizontalScrollbar > 0)
            horizontal = Of(new RectF(0, viewport.PaddingBoxHeight, viewport.PaddingBoxWidth, viewport.HorizontalScrollbar), false, thin, viewport.PaddingBoxWidth, viewport.ScrollWidth, scroll.X, scale);
        return (vertical, horizontal);
    }

    /// <summary>The viewport's scrollbar corner (empty unless it has both scrollbars).</summary>
    public static RectF ViewportCorner(LayoutBox viewport) =>
        viewport.VerticalScrollbar > 0 && viewport.HorizontalScrollbar > 0
            ? new RectF(viewport.PaddingBoxWidth, viewport.PaddingBoxHeight, viewport.VerticalScrollbar, viewport.HorizontalScrollbar)
            : default;
}

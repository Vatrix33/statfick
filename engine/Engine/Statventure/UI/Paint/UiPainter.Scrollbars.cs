using System.Numerics;
using Statventure.Canvas;
using Statventure.Core;
using Statventure.UI.Dom;
using Statventure.UI.Layout;
using Statventure.UI.Style;

namespace Statventure.UI.Paint;

public sealed partial class UiPainter
{
    // ---------- Scrollbars ----------

    /// <summary>A scroll container's scrollbars, at its padding box's edges.</summary>
    /// <summary>
    /// A scroller's overflow controls (ScrollableAreaPainter::PaintOverflowControls): its bars, the
    /// scroll corner where they don't reach, and the resizer on top.
    /// </summary>
    void PaintScrollbars(LayoutBox box)
    {
        var resizes = ScrollbarGeometry.CanResize(box);
        if (!box.IsScrollContainer || box.Style.Visibility != Visibility.Visible || (box.VerticalScrollbar <= 0 && box.HorizontalScrollbar <= 0 && !resizes)
            || box.Element is not { } element)
            return;
        var border = Snap(box.ViewportRect);
        var (vertical, horizontal) = ScrollbarGeometry.For(box, border, element.LayoutScroll.X, element.LayoutScroll.Y, _document.DeviceScale);
        // (Their chunks were recorded with the box's background, where Blink paints them.)
        _canvas.SuppressChunks();
        PaintScrollbars(box, element, vertical, horizontal, border, resizes);
        _canvas.ResumeChunks();
    }

    void PaintScrollbars(LayoutBox box, UiElement element, ScrollbarGeometry? vertical, ScrollbarGeometry? horizontal, RectF border, bool resizes)
    {
        PaintScrollbar(vertical, box.Style, element);
        PaintScrollbar(horizontal, box.Style, element);
        PaintScrollCorner(ScrollbarGeometry.CornerOf(box, border), box.Style);
        if (resizes)
            PaintResizer(ScrollbarGeometry.ResizerOf(box, border, _document.Layout.Context.Scrollbar), box.VerticalScrollbar > 0 || box.HorizontalScrollbar > 0);
    }

    /// <summary>
    /// The resizer (ScrollableAreaPainter::DrawPlatformResizerImage): two diagonal grip lines, dark
    /// with a light line a pixel below, stroked without antialiasing; with scrollbars a grey frame
    /// on its top and left edges.
    /// </summary>
    void PaintResizer(RectF corner, bool frame)
    {
        if (corner.Width <= 0 || corner.Height <= 0 || !_canvas.Drawing)
            return;
        int x = (int)corner.X, y = (int)corner.Y, w = (int)corner.Width, h = (int)corner.Height;
        const int Edge = 1;
        (int X, int Y) p0 = (x + w - Edge, y + h / 2), p1 = (x + w / 2, y + h - Edge), p2 = (x + w - Edge, y + h * 3 / 4), p3 = (x + w * 3 / 4, y + h - Edge);
        void Line((int X, int Y) a, (int X, int Y) b, Color color)
        {
            // A 1 px stroke with butt ends, filled without antialiasing.
            var d = new Vector2(b.X - a.X, b.Y - a.Y);
            if (d == Vector2.Zero)
                return;
            var n = Vector2.Normalize(new Vector2(-d.Y, d.X)) * 0.5f;
            ReadOnlySpan<(float, float)> quad = [(a.X + n.X, a.Y + n.Y), (b.X + n.X, b.Y + n.Y), (b.X - n.X, b.Y - n.Y), (a.X - n.X, a.Y - n.Y)];
            foreach (var span in ScrollbarTheme.Rasterize(quad))
                _canvas.FillRect(span, color);
        }
        var dark = Color.FromBytes(0, 0, 0, 153);
        Line(p0, p1, dark);
        Line(p2, p3, dark);
        var light = Color.FromBytes(255, 255, 255, 153);
        Line((p0.X, p0.Y + Edge), (p1.X + Edge, p1.Y), light);
        Line((p2.X, p2.Y + Edge), (p3.X + Edge, p3.Y), light);
        if (!frame)
            return;
        var grey = Color.FromBytes(217, 217, 217);
        _canvas.FillRect(new RectF(x, y, 1, h), grey);
        _canvas.FillRect(new RectF(x + 1, y, w - 1, 1), grey);
    }

    /// <summary>The viewport's scrollbars, over everything, styled by the root element.</summary>
    void PaintViewportScrollbars(LayoutBox viewport, LayoutBox root)
    {
        if (root.Element is not { } element)
            return;
        var (vertical, horizontal) = ScrollbarGeometry.ForViewport(viewport, root.Style.ScrollbarWidth == "thin", _document.DeviceScale);
        if (PropertiesOf(viewport) is { ScrollTranslation: { } rootScroll } view)
        {
            if (vertical is { } v)
                _canvas.RecordScrollbar(view.BorderBox, rootScroll, viewport, v.Bar, false);
            if (horizontal is { } h)
                _canvas.RecordScrollbar(view.BorderBox, rootScroll, viewport, h.Bar, false);
        }
        _canvas.SuppressChunks();
        PaintScrollbar(vertical, root.Style, element);
        PaintScrollbar(horizontal, root.Style, element);
        PaintScrollCorner(ScrollbarGeometry.ViewportCorner(viewport), root.Style);
        _canvas.ResumeChunks();
    }

    /// <summary>
    /// A scrollbar as Chromium's Fluent theme paints it: the arrow buttons, the track between them,
    /// then the pill-shaped thumb, each in the colour of its state (hovered, pressed, disabled).
    /// </summary>
    void PaintScrollbar(ScrollbarGeometry? geometry, ComputedStyle s, UiElement scroller)
    {
        if (geometry is not { } bar || !_canvas.Drawing)
            return;
        var (hovered, pressed) = _document.ScrollbarStatesOf(scroller, bar.Vertical);
        ScrollbarPartState StateOf(ScrollbarPart part) =>
            !bar.Enabled ? ScrollbarPartState.Disabled
            : part == pressed ? ScrollbarPartState.Pressed
            : part == hovered ? ScrollbarPartState.Hovered
            : ScrollbarPartState.Normal;

        // Buttons: disabled at the end they point to.
        var offset = scroller == _document.Root
            ? (bar.Vertical ? _document.Layout.ViewportScroll.Y : _document.Layout.ViewportScroll.X)
            : (bar.Vertical ? scroller.LayoutScroll.Y : scroller.LayoutScroll.X);
        var max = bar.Total - bar.Visible;
        PaintScrollbarButton(bar, ScrollbarPart.BackButton, offset <= 0 ? ScrollbarPartState.Disabled : StateOf(ScrollbarPart.BackButton), s);
        PaintScrollbarButton(bar, ScrollbarPart.ForwardButton, offset >= max ? ScrollbarPartState.Disabled : StateOf(ScrollbarPart.ForwardButton), s);
        if (bar.Track.Width > 0 && bar.Track.Height > 0)
            _canvas.FillRect(bar.Track, ScrollbarTheme.TrackColor(s.ScrollbarTrack));
        if (bar.HasThumb)
        {
            var thumb = bar.Thumb;
            var color = ScrollbarTheme.ThumbColor(s.ScrollbarThumb, s.ScrollbarTrack, StateOf(ScrollbarPart.Thumb));
            // Fully rounded: Skia fits the 999 px radii to the rect's shorter side.
            _canvas.DrawBox(thumb, Brush.Solid(color), new CornerRadii(MathF.Min(thumb.Width, thumb.Height) / 2), Border.None);
        }
    }

    void PaintScrollbarButton(ScrollbarGeometry bar, ScrollbarPart part, ScrollbarPartState state, ComputedStyle s)
    {
        var rect = bar.RectOf(part);
        if (rect.Width <= 0 || rect.Height <= 0)
            return;
        _canvas.FillRect(rect, ScrollbarTheme.ButtonBackground(s.ScrollbarTrack));
        var color = ScrollbarTheme.ArrowColor(s.ScrollbarThumb, s.ScrollbarTrack, state);
        foreach (var span in ScrollbarTheme.ArrowPixels(rect, part, bar.Vertical, state))
            _canvas.FillRect(span, color);
    }

    void PaintScrollCorner(RectF corner, ComputedStyle s)
    {
        if (corner.Width > 0 && corner.Height > 0 && _canvas.Drawing)
            _canvas.FillRect(corner, ScrollbarTheme.CornerColor(s.ScrollbarTrack));
    }
}

using System.Numerics;
using Statventure.Canvas;
using Statventure.Core;
using Statventure.Graphics;
using Statventure.UI.Dom;
using Statventure.UI.Layout;
using Statventure.UI.Paint.Compositing;
using Statventure.UI.Style;

namespace Statventure.UI.Paint;

/// <summary>
/// Background image layers as Blink 152 paints them (BoxPainterBase::PaintFillLayer,
/// BackgroundImageGeometry, DrawTiledBackground; the decision of 2026-11-05): each layer is one
/// draw over its pixel-snapped destination rect. The bottom layer, when one tile covers the box,
/// is drawn as the box's rect itself, rounded or square (the fast path); other layers of a square
/// box aren't clipped, of a rounded box are drawn under the antialiased rounded clip. A layer that fits in one tile draws that tile; repeated tiles are a
/// pattern: a raster image repeats through the sampler, anything else (a gradient, an SVG, spaced
/// tiles) is drawn once into a tile texture of the size Skia rasterizes a picture shader at
/// (cc's PaintRecord::GetFixedScaleBounds: the tile's device size rounded up) and repeated from it,
/// so tiles have no seams. background-clip and background-origin aren't supported (the border box
/// and the padding box, their initial values). Geometry is in device pixels with Blink's
/// LayoutUnit arithmetic (1/64 px).
/// </summary>
public sealed partial class UiPainter
{
    /// <summary>BoxDecorationData::ComputeBleedAvoidance (no table cells, border images or block fragmentation here).</summary>
    enum BleedAvoidance { None, ShrinkBackground, ClipLayer }

    /// <summary>
    /// The box a background is painted for, in device pixels: its border box unsnapped (the paint
    /// rect), its positioning area before background-origin, its borders (unsnapped widths of the
    /// sides it has, and the style's whole widths for the inner border), which sides hide the
    /// background, whether border-derived adjustments are off (the view), and whether fixed layers
    /// stay with the viewport (HasBackgroundFixedToViewport: no transform around the box).
    /// </summary>
    sealed class FillBox
    {
        public RectF Paint, Area, Viewport;
        public Edges Outsets, Widths;
        public bool ObscuresTop, ObscuresRight, ObscuresBottom, ObscuresLeft;
        public bool Disallow;
        public bool FixedToViewport = true;
    }

    /// <summary>
    /// A layer's image: a gradient, an SVG image or a raster texture, with its natural size and aspect
    /// ratio (device pixels) and the box's zoom (an SVG's user unit, as SVGImageForContainer's).
    /// </summary>
    sealed record LayerImage(ImageValue Value, UiElement? Svg, CanvasImage? Texture, Vector2? Natural, Vector2? Ratio, float Zoom)
    {
        /// <summary>Image::HasIntrinsicSize: raster images and SVGs with a width and height (not gradients).</summary>
        public bool Intrinsic => Natural is not null;
    }

    /// <summary>One layer's BackgroundImageGeometry, in device pixels.</summary>
    sealed class LayerGeometry
    {
        public RectF UnsnappedDest, SnappedDest;
        public Vector2 Tile, Phase, Space;

        /// <summary>ComputePhase: where the first tile starts from the snapped dest's corner (zero or before it).</summary>
        public Vector2 TilePhase => new(IntMod(-Phase.X, Tile.X + Space.X), IntMod(-Phase.Y, Tile.Y + Space.Y));
    }

    // ---------- LayoutUnit arithmetic ----------

    /// <summary>A value on LayoutUnit's 1/64 px grid (float noise off).</summary>
    static float Lu(float v) => MathF.Round(v * 64) / 64;

    /// <summary>LayoutUnit::FromFloatFloor.</summary>
    static float LuFloor(float v) => MathF.Floor(v * 64) / 64;

    /// <summary>LayoutUnit::Round: to the nearest whole pixel, halves up.</summary>
    static float RoundHalfUp(float v) => MathF.Floor(v + 0.5f);

    /// <summary>LayoutUnit's IntMod: the raw values' remainder, truncated toward zero (the dividend's sign).</summary>
    static float IntMod(float a, float b) => b == 0 ? 0 : a % b;

    /// <summary>LayoutUnit / LayoutUnit.</summary>
    static float LuDivide(float a, float b) => b == 0 ? 0 : (float)(Math.Truncate((double)a * 64 / b) / 64);

    /// <summary>LayoutUnit / int.</summary>
    static float LuDivide(float a, int n) => (float)(Math.Truncate((double)a * 64 / n) / 64);

    /// <summary>LayoutUnit::MulDiv.</summary>
    static float LuMulDiv(float a, float m, float d) => d == 0 ? 0 : (float)(Math.Truncate((double)a * m * 64 / d) / 64);

    /// <summary>SnapSizeToPixel (allowZero: SnapSizeToPixelAllowingZero).</summary>
    static float SnapSize(float size, float location, bool allowZero = false)
    {
        var fraction = location % 1;
        var result = RoundHalfUp(fraction + size) - RoundHalfUp(fraction);
        if (!allowZero && result == 0 && MathF.Abs(size) > 4f / 64)
            return size > 0 ? 1 : -1;
        return result;
    }

    /// <summary>ToPixelSnappedRect for a PhysicalRect.</summary>
    static RectF PixelSnap(RectF r, bool allowZero = false) =>
        new(RoundHalfUp(r.X), RoundHalfUp(r.Y), SnapSize(r.Width, r.X, allowZero), SnapSize(r.Height, r.Y, allowZero));

    /// <summary>PhysicalRect::Contract by a strut.</summary>
    static RectF Contract(RectF r, Edges e) => new(r.X + e.Left, r.Y + e.Top, r.Width - e.Left - e.Right, r.Height - e.Top - e.Bottom);

    static RectF ClampSize(RectF r) => new(r.X, r.Y, MathF.Max(0, r.Width), MathF.Max(0, r.Height));

    /// <summary>PhysicalRect::Contains (an empty rect isn't contained for free).</summary>
    static bool Encloses(RectF outer, RectF inner) => outer.X <= inner.X && outer.Y <= inner.Y && outer.Right >= inner.Right && outer.Bottom >= inner.Bottom;

    /// <summary>PhysicalRect::Intersect: a clean empty rect when they don't overlap.</summary>
    static RectF IntersectPhysical(RectF a, RectF b)
    {
        float left = MathF.Max(a.X, b.X), top = MathF.Max(a.Y, b.Y), right = MathF.Min(a.Right, b.Right), bottom = MathF.Min(a.Bottom, b.Bottom);
        return left >= right || top >= bottom ? default : RectF.FromEdges(left, top, right, bottom);
    }

    static RectF Device(RectF r, float k) => RectF.FromEdges(Lu(r.Left * k), Lu(r.Top * k), Lu(r.Right * k), Lu(r.Bottom * k));

    static RectF ToCanvasUnits(RectF r, float k) => k == 1 ? r : new RectF(r.X / k, r.Y / k, r.Width / k, r.Height / k);

    // ---------- Boxes ----------

    /// <summary>A box's background: its colour and image layers (bottom first), as BoxPainterBase::PaintFillLayers paints them.</summary>
    /// <param name="owner">The layout box it is (none for a control's part).</param>
    /// <param name="appearance">The box has an effective appearance.</param>
    void PaintBoxBackground(ComputedStyle s, RectF unsnapped, RectF snapped, CornerRadii radii, BorderSides sides, Color background, BackgroundLayer[] layers,
        LayoutBox? owner, bool appearance)
    {
        if (!_canvas.Drawing)
            return;
        var k = _canvas.Scale;
        var fixedToViewport = FixedToViewport(owner);
        var viewport = new RectF(0, 0, _document.Layout.Viewport.PaddingBoxWidth, _document.Layout.Viewport.PaddingBoxHeight);
        if (fixedToViewport && CompositesFixedBackground(s, radii, layers, owner, appearance))
        {
            // BoxFragmentPainter::PaintCompositeBackgroundAttachmentFixed: the background is painted
            // over the viewport, in its space, clipped to the box's snapped border box (its
            // BackgroundClip node), with no border-derived adjustments.
            _canvas.PushClip(snapped);
            var view = new FillBox { Paint = Device(viewport, k), Area = Device(viewport, k), Viewport = Device(viewport, k), Disallow = true };
            PaintFillLayers(s, view, viewport, CornerRadii.Zero, viewport, CornerRadii.Zero, BleedAvoidance.None, background, layers);
            _canvas.PopClip();
            return;
        }
        // A side is there unless the box is a fragment without it (its width cut to zero).
        bool Present(float styleWidth, float width) => !(styleWidth > 0 && width <= 0);
        bool presentTop = Present(s.BorderTop, sides.Top.Width), presentRight = Present(s.BorderRight, sides.Right.Width);
        bool presentBottom = Present(s.BorderBottom, sides.Bottom.Width), presentLeft = Present(s.BorderLeft, sides.Left.Width);
        var box = new FillBox
        {
            Paint = Device(unsnapped, k),
            Area = Device(unsnapped, k),
            Viewport = Device(viewport, k),
            FixedToViewport = fixedToViewport,
            Outsets = new Edges(Lu(sides.Top.Width * k), Lu(sides.Right.Width * k), Lu(sides.Bottom.Width * k), Lu(sides.Left.Width * k)),
            // ComputedStyle's border widths are whole device pixels (PhysicalBoxStrut::FromInts).
            Widths = new Edges(MathF.Truncate(s.BorderTop * k), MathF.Truncate(s.BorderRight * k), MathF.Truncate(s.BorderBottom * k), MathF.Truncate(s.BorderLeft * k)),
            ObscuresTop = presentTop && Obscures(s.BorderTopStyle, s.BorderTop, s.BorderTopColor ?? s.Color, edge: false),
            ObscuresRight = presentRight && Obscures(s.BorderRightStyle, s.BorderRight, s.BorderRightColor ?? s.Color, edge: false),
            ObscuresBottom = presentBottom && Obscures(s.BorderBottomStyle, s.BorderBottom, s.BorderBottomColor ?? s.Color, edge: false),
            ObscuresLeft = presentLeft && Obscures(s.BorderLeftStyle, s.BorderLeft, s.BorderLeftColor ?? s.Color, edge: false),
        };
        // Bleed avoidance: clipping the whole background to the rounded border box (a layer, approximated
        // by the same clip on each draw), or shrinking it under an opaque border.
        var bleed = BleedAvoidance.None;
        var hasRadius = !radii.IsZero;
        var paintsBorder = s.BorderTop > 0 || s.BorderRight > 0 || s.BorderBottom > 0 || s.BorderLeft > 0;
        if (!paintsBorder || !hasRadius)
        {
            // (A gradient or an SVG never occludes the layers below: its size without a box is empty or it isn't known opaque.)
            if (hasRadius && layers.Length > 0 && (background.A > 0 || layers.Length > 1))
                bleed = BleedAvoidance.ClipLayer;
        }
        else
        {
            var obscuresEdges = (!presentTop || Obscures(s.BorderTopStyle, s.BorderTop, s.BorderTopColor ?? s.Color, edge: true))
                && (!presentRight || Obscures(s.BorderRightStyle, s.BorderRight, s.BorderRightColor ?? s.Color, edge: true))
                && (!presentBottom || Obscures(s.BorderBottomStyle, s.BorderBottom, s.BorderBottomColor ?? s.Color, edge: true))
                && (!presentLeft || Obscures(s.BorderLeftStyle, s.BorderLeft, s.BorderLeftColor ?? s.Color, edge: true));
            // (A side a fragment doesn't have isn't present, so it doesn't obscure.)
            obscuresEdges &= presentTop && presentRight && presentBottom && presentLeft;
            bleed = obscuresEdges ? BleedAvoidance.ShrinkBackground : BleedAvoidance.ClipLayer;
        }
        // The rounded rect a rounded fill is clipped to (ContouredBorderRectForClip): the border box's,
        // inset by half the borders' widths when shrinking (a sixth with a double border).
        var clipRect = snapped;
        var clipRadii = radii;
        if (bleed == BleedAvoidance.ShrinkBackground)
        {
            var fraction = s.BorderTopStyle == BorderLineStyle.Double || s.BorderRightStyle == BorderLineStyle.Double
                || s.BorderBottomStyle == BorderLineStyle.Double || s.BorderLeftStyle == BorderLineStyle.Double ? 1f / 6 : 1f / 2;
            float top = MathF.Truncate(sides.Top.Width * k) * fraction / k, right = MathF.Truncate(sides.Right.Width * k) * fraction / k;
            float bottom = MathF.Truncate(sides.Bottom.Width * k) * fraction / k, left = MathF.Truncate(sides.Left.Width * k) * fraction / k;
            clipRect = Contract(snapped, new Edges(top, right, bottom, left));
            clipRadii = radii.Shrink(top, right, bottom, left);
        }
        PaintFillLayers(s, box, snapped, radii, clipRect, clipRadii, bleed, background, layers, owner);
    }

    /// <summary>
    /// BoxBackgroundPaintContext::HasBackgroundFixedToViewport: fixed layers stay with the viewport
    /// unless the box or a box around it has a transform (or will-change of one); then they scroll.
    /// </summary>
    static bool FixedToViewport(LayoutBox? box)
    {
        for (var b = box; b is not null; b = b.Parent)
            if (b.Type != BoxType.Inline && (b.Style.HasTransform || b.Style.WillChange.Any(p => p is "transform" or "translate" or "rotate" or "scale")))
                return false;
        return true;
    }

    /// <summary>How many boxes have a fixed background image this paint (-1: not counted yet).</summary>
    int _fixedBackgroundObjects = -1;

    /// <summary>
    /// LayoutBox::CanCompositeBackgroundAttachmentFixed (with LocalFrameView's limit of 8 such boxes):
    /// a fixed background, its box's only layer, is painted in the viewport's space unless LCD text
    /// is strongly preferred, the box has a shadow, an appearance or rounded corners.
    /// </summary>
    bool CompositesFixedBackground(ComputedStyle s, CornerRadii radii, BackgroundLayer[] layers, LayoutBox? owner, bool appearance)
    {
        if (owner is null || layers.Length != 1 || !layers[0].Fixed || LcdPreference == LcdTextPreference.StronglyPreferred
            || s.BoxShadows.Length > 0 || appearance || !radii.IsZero || IsFixedBackgroundOfView(owner))
            return false;
        if (_fixedBackgroundObjects < 0)
            _fixedBackgroundObjects = _document.Layout.Viewport.Descendants().Count(b => !IsFixedBackgroundOfView(b) && b.Style.Backgrounds.Any(l => l.Fixed && l.Image.Kind != ImageKind.None));
        return _fixedBackgroundObjects <= 8;
    }

    /// <summary>The root's background, or the body's it takes, goes to the view (LayoutBoxModelObject::BackgroundTransfersToView).</summary>
    bool IsFixedBackgroundOfView(LayoutBox box) => box.Element is { Parent: null } && box.Pseudo == PseudoKind.None || box == _backgroundSource;

    /// <summary>BorderEdge::ObscuresBackground (edge: ObscuresBackgroundEdge, which lets a double border through).</summary>
    static bool Obscures(BorderLineStyle style, float width, Color color, bool edge)
    {
        if (color.A < 1 || style == BorderLineStyle.Hidden)
            return false;
        // BorderEdge::EffectiveStyle: thin double, ridge and groove borders are solid.
        if (style == BorderLineStyle.Double && width < 3 || style is BorderLineStyle.Ridge or BorderLineStyle.Groove && width <= 1)
            style = BorderLineStyle.Solid;
        return style is not (BorderLineStyle.Dotted or BorderLineStyle.Dashed) && (edge || style != BorderLineStyle.Double);
    }

    /// <summary>
    /// The canvas's background layers (the root's or the body's): over the document's background
    /// rect, positioned in <paramref name="area"/>, with no border-derived adjustments (the view).
    /// </summary>
    void PaintCanvasBackgroundLayers(ComputedStyle s, RectF canvasRect, RectF area)
    {
        if (!_canvas.Drawing)
            return;
        var k = _canvas.Scale;
        var box = new FillBox
        {
            Paint = Device(canvasRect, k),
            Area = Device(area, k),
            Viewport = Device(new RectF(0, 0, _document.Layout.Viewport.PaddingBoxWidth, _document.Layout.Viewport.PaddingBoxHeight), k),
            Disallow = true,
        };
        PaintFillLayers(s, box, canvasRect, CornerRadii.Zero, canvasRect, CornerRadii.Zero, BleedAvoidance.None, Color.Transparent, s.Backgrounds);
    }

    /// <param name="owner">The box whose text a layer with background-clip: text is painted through (none for the view, whose clip is always the border box).</param>
    void PaintFillLayers(ComputedStyle s, FillBox box, RectF snapped, CornerRadii radii, RectF clipRect, CornerRadii clipRadii,
        BleedAvoidance bleed, Color background, BackgroundLayer[] layers, LayoutBox? owner = null)
    {
        var roundedFill = !radii.IsZero && bleed != BleedAvoidance.ClipLayer;
        if (bleed == BleedAvoidance.ClipLayer)
            _canvas.PushClip(snapped, radii);
        // The colour is painted with the bottom layer: the last one with an image unless the bottom layer has none and
        // another clip (then the colour alone, first). Only text clips here (padding-box and content-box are painted
        // as border-box: the decision on background layers, 2026-11-05).
        var colorClip = ClipOf(s.BackgroundColorClip, owner);
        var colorAlone = background.A > 0 && (layers.Length == 0 || ClipOf(layers[^1].Clip, owner) != colorClip);
        if (colorAlone)
        {
            if (colorClip == BackgroundClip.Text)
                PaintTextFill(owner!, snapped, radii, background, null, null);
            else
                PaintFastBottomLayer(null, null, roundedFill, roundedFill ? clipRect : snapped, roundedFill ? clipRadii : CornerRadii.Zero, background);
        }
        for (var i = layers.Length - 1; i >= 0; i--)
        {
            var layer = layers[i];
            var bottom = i == layers.Length - 1;
            var image = LayerImageOf(layer, s.Zoom);
            var paintColor = bottom && background.A > 0 && !colorAlone;
            if (image is null && !paintColor)
                continue;
            var geometry = image is not null ? Geometry(layer, box, image) : null;
            if (ClipOf(layer.Clip, owner) == BackgroundClip.Text)
            {
                PaintTextFill(owner!, snapped, radii, paintColor ? background : null, image, geometry);
                continue;
            }
            // The bottom layer when one tile covers it: drawn as the (rounded) rect itself (PaintFastBottomLayer;
            // ContouredRect::HasRoundCurvature holds for a square rect too, so square boxes take it as well).
            if (bottom && (image is null || bleed != BleedAvoidance.ShrinkBackground)
                && PaintFastBottomLayer(image, geometry, roundedFill, roundedFill ? clipRect : snapped, roundedFill ? clipRadii : CornerRadii.Zero, paintColor ? background : null))
                continue;
            if (roundedFill)
                _canvas.PushClip(clipRect, clipRadii);
            if (paintColor)
                _canvas.FillRect(snapped, background);
            if (image is not null && geometry is { } g && !g.SnappedDest.IsEmpty && g.Tile.X > 0 && g.Tile.Y > 0)
                DrawTiledBackground(image, g);
            if (roundedFill)
                _canvas.PopClip();
        }
        if (bleed == BleedAvoidance.ClipLayer)
            _canvas.PopClip();
    }

    /// <summary>
    /// The clip a layer is painted with: text for a box (BoxBackgroundPaintContext::EffectiveClip: the view's background
    /// covers everything), the border box otherwise.
    /// </summary>
    static BackgroundClip ClipOf(BackgroundClip clip, LayoutBox? owner) =>
        owner is not null && clip == BackgroundClip.Text ? BackgroundClip.Text : BackgroundClip.BorderBox;

    /// <summary>
    /// A layer (its colour and image) painted through its box's text, as BoxPainterBase::PaintFillLayer paints one with
    /// background-clip: text: no fast path; a rounded box clipped to its snapped rounded border box (its fill is
    /// rounded: not a border fill); then PaintFillLayerTextFillBox: the snapped border box clipped, the layer drawn in a
    /// layer of its own, the box's text drawn in black in a layer composited onto it with destination-in.
    /// </summary>
    void PaintTextFill(LayoutBox owner, RectF snapped, CornerRadii radii, Color? color, LayerImage? image, LayerGeometry? geometry)
    {
        if (!radii.IsZero)
            _canvas.PushClip(snapped, radii);
        _canvas.PushTextClipLayer(snapped);
        if (color is { } fill)
            _canvas.FillRect(snapped, fill);
        if (image is not null && geometry is { } g && !g.SnappedDest.IsEmpty && g.Tile.X > 0 && g.Tile.Y > 0)
            DrawTiledBackground(image, g);
        _canvas.PushTextMask(snapped);
        PaintTextClip(owner);
        _canvas.PopTextMask();
        _canvas.PopTextClipLayer();
        if (!radii.IsZero)
            _canvas.PopClip();
    }

    /// <summary>A layer's image, or null when it can't be drawn (no stops, not loaded).</summary>
    LayerImage? LayerImageOf(BackgroundLayer layer, float zoom)
    {
        var k = _canvas.Scale;
        var image = layer.Image;
        if (image.Kind is ImageKind.LinearGradient or ImageKind.RadialGradient)
            return image.Stops.Length > 0 ? new LayerImage(image, null, null, null, null, zoom) : null;
        if (image.Kind != ImageKind.Url)
            return null;
        if (SvgImage(image.Url) is { } svg)
        {
            // The aspect ratio as a size (NaturalSizingInfo's): the width and height, else the viewBox's.
            Vector2? ratio = svg.Size is { X: > 0, Y: > 0 } size ? size
                : ParseNumbers(svg.Svg.GetAttribute("viewbox")) is [_, _, var vw, var vh] && vw > 0 && vh > 0 ? new Vector2(vw, vh) : null;
            return new LayerImage(image, svg.Svg, null, svg.Size is { } natural ? natural * zoom * k : null, ratio, zoom);
        }
        if (ImageTexture?.Invoke(image.Url) is { } texture && _document.Layout.Context.ImageSize?.Invoke(image.Url) is { X: > 0, Y: > 0 } pixels)
            return new LayerImage(image, null, texture, pixels * zoom * k, pixels, zoom);
        return null;
    }

    // ---------- Geometry (BackgroundImageGeometry::Calculate) ----------

    LayerGeometry Geometry(BackgroundLayer layer, FillBox box, LayerImage image)
    {
        var k = _canvas.Scale;
        var g = new LayerGeometry();
        RectF unsnappedArea, snappedArea;
        Vector2 unsnappedOffset = default, snappedOffset = default;
        var fixedLayer = layer.Fixed && box.FixedToViewport;
        if (fixedLayer)
        {
            // background-attachment: fixed, against the viewport.
            unsnappedArea = snappedArea = g.UnsnappedDest = g.SnappedDest = box.Viewport;
        }
        else
        {
            unsnappedArea = box.Area;
            var paint = box.Paint;
            Edges destUnsnapped = default, destSnapped = default, areaUnsnapped = box.Outsets, areaSnapped = box.Outsets;
            if (!box.Disallow)
            {
                // The border-snapped inner border (PixelSnappedContouredInnerBorder).
                var inner = PixelSnap(ClampSize(Contract(unsnappedArea, box.Widths)), allowZero: true);
                // background-clip: border-box. Edges a border hides end at the snapped inner border (ObscuredBorderOutsets).
                // A layer clipped to text keeps the whole border box (ComputeDestRectAdjustments: none for kText).
                if (layer.Clip != BackgroundClip.Text)
                {
                    if (box.ObscuresTop)
                        (destSnapped.Top, destUnsnapped.Top) = (inner.Top - paint.Top, box.Outsets.Top);
                    if (box.ObscuresRight)
                        (destSnapped.Right, destUnsnapped.Right) = (paint.Right - inner.Right, box.Outsets.Right);
                    if (box.ObscuresBottom)
                        (destSnapped.Bottom, destUnsnapped.Bottom) = (paint.Bottom - inner.Bottom, box.Outsets.Bottom);
                    if (box.ObscuresLeft)
                        (destSnapped.Left, destUnsnapped.Left) = (inner.Left - paint.Left, box.Outsets.Left);
                }
                // background-origin: padding-box, snapped to the inner border (InnerBorderOutsets).
                areaSnapped = new Edges(inner.Top - unsnappedArea.Top, unsnappedArea.Right - inner.Right, unsnappedArea.Bottom - inner.Bottom, inner.Left - unsnappedArea.Left);
            }
            unsnappedOffset = new Vector2(areaUnsnapped.Left - destUnsnapped.Left, areaUnsnapped.Top - destUnsnapped.Top);
            snappedOffset = new Vector2(areaSnapped.Left - destSnapped.Left, areaSnapped.Top - destSnapped.Top);
            g.SnappedDest = ClampSize(PixelSnap(Contract(paint, destSnapped)));
            g.UnsnappedDest = ClampSize(Contract(paint, destUnsnapped));
            snappedArea = ClampSize(PixelSnap(Contract(unsnappedArea, areaSnapped)));
            unsnappedArea = ClampSize(Contract(unsnappedArea, areaUnsnapped));
        }
        g.Tile = FillTileSize(layer, image, image.Intrinsic ? unsnappedArea.Size : snappedArea.Size, snappedArea.Size, k);
        RepeatAndPosition(layer, g, unsnappedArea.Size, snappedArea.Size, unsnappedOffset, snappedOffset, k);
        if (fixedLayer)
            g.Phase += Vector2.Max(Vector2.Zero, box.Paint.Position - g.UnsnappedDest.Position);
        g.UnsnappedDest = IntersectPhysical(g.UnsnappedDest, box.Paint);
        g.SnappedDest = PixelSnap(IntersectPhysical(g.SnappedDest, box.Paint));
        return g;
    }

    /// <summary>CalculateFillTileSize: background-size against the positioning area (snapped for images without a natural size).</summary>
    static Vector2 FillTileSize(BackgroundLayer layer, LayerImage image, Vector2 area, Vector2 snappedArea, float k)
    {
        var ratio = image.Ratio is { X: > 0, Y: > 0 } r ? new Vector2(LuFloor(r.X), LuFloor(r.Y)) : (Vector2?)null;
        if (layer.SizeKeyword is "contain" or "cover")
        {
            if (ratio is not { } aspect)
                return snappedArea;
            // PhysicalSize::FitToAspectRatio: the limiting dimension fills the area exactly.
            var cover = layer.SizeKeyword == "cover";
            float heightScale = snappedArea.Y / aspect.Y, widthScale = snappedArea.X / aspect.X;
            var tile = (widthScale > heightScale) != cover
                ? new Vector2(LayoutUnit.Truncate(aspect.X * heightScale), snappedArea.Y)
                : new Vector2(snappedArea.X, LayoutUnit.Truncate(aspect.Y * widthScale));
            // The dependent dimension: whole pixels for contain, at least one for cover.
            if (tile.X != snappedArea.X)
                tile.X = cover ? MathF.Max(1, tile.X) : MathF.Max(1, RoundHalfUp(tile.X));
            if (tile.Y != snappedArea.Y)
                tile.Y = cover ? MathF.Max(1, tile.Y) : MathF.Max(1, RoundHalfUp(tile.Y));
            return tile;
        }
        var size = area;
        if (!layer.SizeX.IsAuto)
            size.X = LayoutUnit.Truncate(layer.SizeX.ResolveOr(area.X / k, 0) * k);
        if (!layer.SizeY.IsAuto)
            size.Y = LayoutUnit.Truncate(layer.SizeY.ResolveOr(area.Y / k, 0) * k);
        if (layer.SizeX.IsAuto && !layer.SizeY.IsAuto)
            size.X = ratio is { } aspect ? ClampedWidthForRatio(size.Y, aspect) : image.Natural is { } n ? LuFloor(n.X) : area.X;
        else if (!layer.SizeX.IsAuto && layer.SizeY.IsAuto)
            size.Y = ratio is { } aspect ? ClampedHeightForRatio(size.X, aspect) : image.Natural is { } n ? LuFloor(n.Y) : area.Y;
        else if (layer.SizeX.IsAuto && layer.SizeY.IsAuto)
        {
            // The concrete object size (StyleImage::ImageSize): the natural size, else the ratio
            // contained in the area, else the area.
            var concrete = area;
            if (image.Natural is { } natural)
                concrete = natural;
            else if (image.Svg is not null && ratio is { } aspect && area.X > 0 && area.Y > 0)
                concrete = area.X / area.Y > aspect.X / aspect.Y ? new Vector2(area.Y * aspect.X / aspect.Y, area.Y) : new Vector2(area.X, area.X * aspect.Y / aspect.X);
            size = new Vector2(LuFloor(concrete.X), LuFloor(concrete.Y));
        }
        return Vector2.Max(size, Vector2.Zero);
    }

    static float ClampedWidthForRatio(float height, Vector2 ratio)
    {
        var width = LuMulDiv(height, ratio.X, ratio.Y);
        return ratio.X >= 1 && width < 1 ? 1 : width;
    }

    static float ClampedHeightForRatio(float width, Vector2 ratio)
    {
        var height = LuMulDiv(width, ratio.Y, ratio.X);
        return ratio.Y >= 1 && height < 1 ? 1 : height;
    }

    /// <summary>ComputeTilePhase: how much of the partial tile before the position is visible.</summary>
    static float TilePhase(float position, float extent) => extent != 0 ? extent - IntMod(position, extent) : 0;

    /// <summary>CalculateRepeatAndPosition: background-repeat and background-position.</summary>
    static void RepeatAndPosition(BackgroundLayer layer, LayerGeometry g, Vector2 unsnappedArea, Vector2 snappedArea, Vector2 unsnappedOffset, Vector2 snappedOffset, float k)
    {
        // Positions against the area less the tile (before round changes it): unsnapped for the
        // phase, snapped for what must fill the painted area.
        var unsnappedAvailable = unsnappedArea - g.Tile;
        var snappedAvailable = snappedArea - g.Tile;
        float Position(Length position, float available) => LayoutUnit.Truncate(position.ResolveOr(available / k, 0) * k);
        var repeatX = layer.RepeatX;
        var repeatY = layer.RepeatY;
        if (repeatX == BackgroundRepeat.Round && snappedArea.X > 0 && g.Tile.X > 0)
        {
            var width = RoundedTileSize(snappedArea.X, g.Tile.X);
            if (layer.SizeY.IsAuto && repeatY != BackgroundRepeat.Round)
                g.Tile.Y = ClampedHeightForRatio(width, g.Tile);
            g.Tile.X = width;
            g.Phase.X = TilePhase(Position(layer.PositionX, snappedAvailable.X) + unsnappedOffset.X, g.Tile.X);
            g.Space = Vector2.Zero;
        }
        if (repeatY == BackgroundRepeat.Round && snappedArea.Y > 0 && g.Tile.Y > 0)
        {
            var height = RoundedTileSize(snappedArea.Y, g.Tile.Y);
            if (layer.SizeX.IsAuto && repeatX != BackgroundRepeat.Round)
                g.Tile.X = ClampedWidthForRatio(height, g.Tile);
            g.Tile.Y = height;
            g.Phase.Y = TilePhase(Position(layer.PositionY, snappedAvailable.Y) + unsnappedOffset.Y, g.Tile.Y);
            g.Space = Vector2.Zero;
        }
        if (repeatX == BackgroundRepeat.Repeat)
        {
            g.Phase.X = TilePhase(unsnappedOffset.X + Position(layer.PositionX, unsnappedAvailable.X), g.Tile.X);
            g.Space.X = 0;
        }
        else if (repeatX == BackgroundRepeat.Space && g.Tile.X > 0)
        {
            var space = SpaceBetweenTiles(snappedArea.X, g.Tile.X);
            if (space >= 0)
            {
                g.Space.X = space;
                g.Phase.X = TilePhase(snappedOffset.X, g.Tile.X + space);
            }
            else
                repeatX = BackgroundRepeat.NoRepeat;
        }
        if (repeatX == BackgroundRepeat.NoRepeat)
        {
            var offset = unsnappedOffset.X + Position(layer.PositionX, unsnappedAvailable.X);
            var snappedOffsetX = snappedOffset.X + Position(layer.PositionX, snappedAvailable.X);
            if (offset > 0)
            {
                // The dest moves to the tile, on the nearest pixel, a tile wide.
                g.UnsnappedDest = new RectF(g.UnsnappedDest.X + offset, g.UnsnappedDest.Y, g.Tile.X, g.UnsnappedDest.Height);
                g.SnappedDest = new RectF(RoundHalfUp(g.UnsnappedDest.X), g.SnappedDest.Y, g.Tile.X, g.SnappedDest.Height);
                g.Phase.X = 0;
            }
            else
            {
                g.Phase.X = -offset;
                g.UnsnappedDest = g.UnsnappedDest with { Width = g.Tile.X + offset };
                g.SnappedDest = g.SnappedDest with { Width = g.Tile.X + snappedOffsetX };
            }
            g.Space.X = 0;
        }
        if (repeatY == BackgroundRepeat.Repeat)
        {
            g.Phase.Y = TilePhase(unsnappedOffset.Y + Position(layer.PositionY, unsnappedAvailable.Y), g.Tile.Y);
            g.Space.Y = 0;
        }
        else if (repeatY == BackgroundRepeat.Space && g.Tile.Y > 0)
        {
            var space = SpaceBetweenTiles(snappedArea.Y, g.Tile.Y);
            if (space >= 0)
            {
                g.Space.Y = space;
                g.Phase.Y = TilePhase(snappedOffset.Y, g.Tile.Y + space);
            }
            else
                repeatY = BackgroundRepeat.NoRepeat;
        }
        if (repeatY == BackgroundRepeat.NoRepeat)
        {
            var offset = unsnappedOffset.Y + Position(layer.PositionY, unsnappedAvailable.Y);
            var snappedOffsetY = snappedOffset.Y + Position(layer.PositionY, snappedAvailable.Y);
            if (offset > 0)
            {
                g.UnsnappedDest = new RectF(g.UnsnappedDest.X, g.UnsnappedDest.Y + offset, g.UnsnappedDest.Width, g.Tile.Y);
                g.SnappedDest = new RectF(g.SnappedDest.X, RoundHalfUp(g.UnsnappedDest.Y), g.SnappedDest.Width, g.Tile.Y);
                g.Phase.Y = 0;
            }
            else
            {
                g.Phase.Y = -offset;
                g.UnsnappedDest = g.UnsnappedDest with { Height = g.Tile.Y + offset };
                g.SnappedDest = g.SnappedDest with { Height = g.Tile.Y + snappedOffsetY };
            }
            g.Space.Y = 0;
        }
    }

    /// <summary>ComputeRoundedTileSize: the area over a whole number of tiles.</summary>
    static float RoundedTileSize(float area, float tile)
    {
        var count = Math.Max(1, (int)RoundHalfUp(LuDivide(area, tile)));
        return LuDivide(area, count);
    }

    /// <summary>GetSpaceBetweenImageTiles: -1 when fewer than two tiles fit.</summary>
    static float SpaceBetweenTiles(float area, float tile)
    {
        var count = (int)MathF.Truncate(LuDivide(area, tile));
        return count > 1 ? LuDivide(area - count * tile, count - 1) : -1;
    }

    // ---------- Drawing ----------

    /// <summary>
    /// PaintFastBottomLayer: the colour over <paramref name="colorRect"/> (a rounded fill's rounded
    /// rect, else the snapped border box) and a tile that covers the image's rect (the same rounded
    /// rect, else the snapped dest), drawn as that rect itself. False (nothing drawn) when one tile
    /// doesn't cover it.
    /// </summary>
    bool PaintFastBottomLayer(LayerImage? image, LayerGeometry? geometry, bool rounded, RectF colorRect, CornerRadii colorRadii, Color? color)
    {
        var k = _canvas.Scale;
        var imageRect = rounded ? Device(colorRect, k) : geometry?.SnappedDest ?? default;
        RectF? source = null;
        if (image is not null && geometry is { } g && !imageRect.IsEmpty)
        {
            if (g.Tile.X < imageRect.Width || g.Tile.Y < imageRect.Height)
                return false;
            // The rect the tile must cover: the rounded rect's, else GetSubsetDestRectForImage's (the snapped
            // dest's place, with the unsnapped dest's size for an image of a natural size).
            var cover = rounded ? imageRect : image.Intrinsic ? new RectF(g.SnappedDest.X, g.SnappedDest.Y, g.UnsnappedDest.Width, g.UnsnappedDest.Height) : g.SnappedDest;
            if (SingleTileSource(g, image, cover) is not { } src)
                return false;
            source = src;
        }
        if (color is { } fill)
            _canvas.DrawBox(colorRect, Brush.Solid(fill), colorRadii, Border.None);
        if (image is not null && geometry is { } layout && source is { } s && !s.IsEmpty)
            DrawSingleTile(image, layout, imageRect, s, rounded ? colorRadii : CornerRadii.Zero, rounded);
        return true;
    }

    /// <summary>
    /// OptimizeToSingleTileDraw: the part of the tile (in its own units) that maps onto
    /// <paramref name="dest"/> when one tile covers it; null when it doesn't.
    /// </summary>
    static RectF? SingleTileSource(LayerGeometry g, LayerImage image, RectF dest)
    {
        var tile = new RectF(g.SnappedDest.X + g.TilePhase.X, g.SnappedDest.Y + g.TilePhase.Y, g.Tile.X, g.Tile.Y);
        if (!Encloses(tile, dest))
            return null;
        var offset = dest.Position - tile.Position;
        // A generated image is the tile's size: the snapped dest's size from the tile; an image
        // with a natural size maps the (unsnapped) dest's size.
        return image.Intrinsic ? new RectF(offset.X, offset.Y, dest.Width, dest.Height) : new RectF(offset.X, offset.Y, g.SnappedDest.Width, g.SnappedDest.Height);
    }

    /// <summary>DrawTiledBackground: one tile drawn when it covers the dest, else the pattern.</summary>
    void DrawTiledBackground(LayerImage image, LayerGeometry g)
    {
        var dest = g.SnappedDest;
        // The single-tile test uses the snapped place and the unsnapped size.
        var subset = new RectF(dest.X, dest.Y, g.UnsnappedDest.Width, g.UnsnappedDest.Height);
        if (SingleTileSource(g, image, subset) is { } source)
            DrawSingleTile(image, g, dest, source, CornerRadii.Zero, rounded: false);
        else
            DrawPattern(image, g);
    }

    /// <summary>
    /// The image drawn from <paramref name="source"/> (in the tile's units) onto <paramref name="dest"/>
    /// (device pixels): GraphicsContext::DrawImage, or DrawImageRRect for a rounded rect.
    /// </summary>
    void DrawSingleTile(LayerImage image, LayerGeometry g, RectF dest, RectF source, CornerRadii radii, bool rounded)
    {
        var k = _canvas.Scale;
        var scale = new Vector2(source.Width > 0 ? dest.Width / source.Width : 1, source.Height > 0 ? dest.Height / source.Height : 1);
        // Where the whole tile lands.
        var tile = new RectF(dest.X - source.X * scale.X, dest.Y - source.Y * scale.Y, g.Tile.X * scale.X, g.Tile.Y * scale.Y);
        var uv = new RectF(source.X / g.Tile.X, source.Y / g.Tile.Y, source.Width / g.Tile.X, source.Height / g.Tile.Y);
        if (image.Svg is null && image.Texture is null)
        {
            // A gradient's rect is the tile's part of the dest (GradientGeneratedImage::Draw); a rounded rect takes the shader whole.
            var drawn = rounded ? dest : IntersectPhysical(dest, tile);
            if (drawn.IsEmpty || GradientBrush(image.Value, g.Tile / k) is not { } brush)
                return;
            _canvas.DrawBox(ToCanvasUnits(drawn, k), PlaceBrush(brush, (tile.Position - drawn.Position) / k, scale), radii, Border.None);
        }
        else if (image.Svg is { } svg)
        {
            if (FilterImage(image.Value.Url, svg, ToCanvasUnits(tile, k)) is { } filtered)
                _canvas.DrawImage(filtered, ToCanvasUnits(dest, k), uv, radii);
            else
            {
                _canvas.PushClip(ToCanvasUnits(dest, k), radii);
                PaintSvgImage(image, ToCanvasUnits(tile, k), clip: false);
                _canvas.PopClip();
            }
        }
        else
            _canvas.DrawImage(image.Texture!, ToCanvasUnits(dest, k), uv, radii);
    }

    /// <summary>
    /// Image::DrawPattern: the tile repeated over the dest from its phase. A raster image without
    /// spacing repeats through the sampler; anything else is recorded once into a tile texture as
    /// Skia rasterizes a picture shader (its device size rounded up) and repeated from that.
    /// </summary>
    void DrawPattern(LayerImage image, LayerGeometry g)
    {
        var k = _canvas.Scale;
        var dest = g.SnappedDest;
        var phase = dest.Position + g.TilePhase;
        // The tile's drawn size: the dest's, where they're within half a pixel (no lone repeated row or column).
        var drawn = new Vector2(
            MathF.Abs(g.Tile.X - dest.Width) <= 0.5f ? dest.Width : g.Tile.X,
            MathF.Abs(g.Tile.Y - dest.Height) <= 0.5f ? dest.Height : g.Tile.Y);
        var generated = image.Svg is null && image.Texture is null;
        // One step of the pattern (tile and space) where it's drawn; a generated image's space is in its own units.
        var step = generated ? (g.Tile + g.Space) * drawn / g.Tile : drawn + g.Space;
        var uv = new RectF((dest.X - phase.X) / step.X, (dest.Y - phase.Y) / step.Y, dest.Width / step.X, dest.Height / step.Y);
        if (image.Texture is { } texture && g.Space == Vector2.Zero)
        {
            // ComputeTileMode: an axis that stays in one tile clamps.
            var clamps = dest.X >= phase.X && dest.Right <= phase.X + drawn.X && dest.Y >= phase.Y && dest.Bottom <= phase.Y + drawn.Y;
            _canvas.DrawImage(texture, ToCanvasUnits(dest, k), uv, sampler: clamps ? SamplerKind.LinearClamp : SamplerKind.LinearRepeat);
            return;
        }
        GpuImage? tile = null;
        if (_canvas.Draws)
        {
            // The picture's raster size: the step through the canvas's transform (SkMatrix::decomposeScale), rounded up, at most 2048² px.
            var transform = _canvas.Transform;
            var raster = step * new Vector2(new Vector2(transform.M11, transform.M12).Length(), new Vector2(transform.M21, transform.M22).Length());
            if (raster.X * raster.Y > 2048 * 2048)
                raster *= MathF.Sqrt(2048f * 2048 / (raster.X * raster.Y));
            var texels = new Vector2(MathF.Max(1, MathF.Ceiling(raster.X)), MathF.Max(1, MathF.Ceiling(raster.Y)));
            _canvas.PushTile((int)texels.X, (int)texels.Y);
            // The content's own units: the generated image's (its tile and space), else the drawn tile's.
            var bounds = generated ? g.Tile + g.Space : step;
            _canvas.PushTransform(Matrix3x2.CreateScale(texels.X / (bounds.X / k), texels.Y / (bounds.Y / k)));
            if (generated)
            {
                if (GradientBrush(image.Value, g.Tile / k) is { } brush)
                    _canvas.DrawBox(new RectF(0, 0, g.Tile.X / k, g.Tile.Y / k), brush, CornerRadii.Zero, Border.None);
            }
            else
            {
                var content = new RectF(0, 0, drawn.X / k, drawn.Y / k);
                if (image.Texture is { } spaced)
                    _canvas.DrawImage(spaced, content);
                else if (FilterImage(image.Value.Url, image.Svg!, content) is { } filtered)
                    _canvas.DrawImage(filtered, content);
                else if (g.Space != Vector2.Zero)
                {
                    // Spaced tiles don't draw into the space: SVGImage::DrawPatternForContainer clips the
                    // recording to the tile without antialiasing, so the clip's edges round to the
                    // tile texture's pixels (SkRect::round).
                    float scaleX = texels.X / (bounds.X / k), scaleY = texels.Y / (bounds.Y / k);
                    _canvas.PushClip(new RectF(0, 0, RoundHalfUp(content.Width * scaleX) / scaleX, RoundHalfUp(content.Height * scaleY) / scaleY));
                    PaintSvgImage(image, content, clip: false);
                    _canvas.PopClip();
                }
                else
                    PaintSvgImage(image, content, clip: false);
            }
            _canvas.PopTransform();
            tile = _canvas.PopTile();
        }
        _canvas.DrawTile(tile, ToCanvasUnits(dest, k), uv);
    }

    /// <summary>
    /// An SVG image drawn over <paramref name="rect"/>: one with a natural size is drawn at that size and
    /// scaled like any image (SVGImage::Draw from its container to the dest); one without fills the rect.
    /// </summary>
    void PaintSvgImage(LayerImage image, RectF rect, bool clip)
    {
        if (image.Natural is not { } natural)
        {
            PaintSvgTree(image.Svg!, rect, Color.Black, clip, zoom: image.Zoom);
            return;
        }
        var size = natural / _canvas.Scale;
        _canvas.PushTransform(Matrix3x2.CreateScale(rect.Width / size.X, rect.Height / size.Y) * Matrix3x2.CreateTranslation(rect.X, rect.Y));
        PaintSvgTree(image.Svg!, new RectF(0, 0, size.X, size.Y), Color.Black, clip, zoom: image.Zoom);
        _canvas.PopTransform();
    }

    /// <summary>A brush (geometry in its shape's box) for a shape whose box starts <paramref name="offset"/> before the image's and draws it scaled.</summary>
    static Brush PlaceBrush(Brush brush, Vector2 offset, Vector2 scale) =>
        offset == Vector2.Zero && scale == Vector2.One ? brush : brush with
        {
            Start = brush.Start * scale + offset,
            End = brush.End * scale + offset,
            Radii = brush.Radii * scale,
        };
}

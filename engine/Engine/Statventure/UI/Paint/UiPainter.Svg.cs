using System.Globalization;
using System.Numerics;
using Statventure.Canvas;
using Statventure.Core;
using Statventure.UI.Css;
using Statventure.UI.Dom;
using Statventure.UI.Layout;
using Statventure.UI.Style;
using CanvasPath = Statventure.Canvas.Path;

namespace Statventure.UI.Paint;

/// <summary>
/// Inline SVG for vector icons: shapes (path, rect, circle, ellipse, line, polyline, polygon), groups,
/// use, transforms, fill and stroke (colours, currentColor, linear and radial gradients), opacities,
/// fill rules, line caps and joins, and the root's viewBox with preserveAspectRatio.
/// Transforms come from the elements' computed styles (the transform attribute is a presentation
/// attribute of CSS transform), with transform-origin and transform-box as Blink's TransformHelper
/// resolves them; fill, stroke and opacity come from attributes and style attributes. Each shape is a
/// display item whose visual rect is Blink's: its stroke bounding box enclosed in whole user units.
/// </summary>
public sealed partial class UiPainter
{
    sealed record SvgPaint(Brush? Fill, float FillOpacity, Brush? Stroke, float StrokeOpacity, float StrokeWidth, float Opacity, FillRule Rule, LineCap Cap, LineJoin Join, Color CurrentColor, string? FillUrl, string? StrokeUrl);

    /// <summary>What walking an svg needs: its ids (for use and paint servers) and its viewport in user units (the view-box reference box).</summary>
    readonly record struct SvgScope(Dictionary<string, UiElement> Ids, Vector2 Viewport);

    /// <summary>Blink's shape geometry types, as far as stroke bounds care (LayoutSVGShape::GeometryType).</summary>
    enum SvgGeometry { Simple, Line, Path }

    /// <summary>The text of a file resource by url (SVG images).</summary>
    public Func<string, string?>? TextResource { get; set; }

    /// <summary>An SVG element's shape, its bounding box and its transform attribute, made again when its attributes change.</summary>
    sealed class SvgShape
    {
        public int Version = -1;
        public CanvasPath? Path;
        /// <summary>The transform attribute, for elements without a computed style (SVG images).</summary>
        public Matrix3x2? AttributeTransform;
        /// <summary>The object bounding box (Blink's fill bounding box), null for an empty shape.</summary>
        public RectF? FillBox;
        public SvgGeometry Geometry;
        List<(List<Vector2> Points, bool Closed)>? _outline;
        RectF _bounds;

        /// <summary>The shape flattened in its own units (for hit testing).</summary>
        public List<(List<Vector2> Points, bool Closed)> Outline()
        {
            if (_outline is null)
            {
                _outline = Path?.Flatten(Matrix3x2.Identity, 0.05f) ?? [];
                var min = new Vector2(float.MaxValue);
                var max = new Vector2(float.MinValue);
                foreach (var (points, _) in _outline)
                    foreach (var point in points)
                    {
                        min = Vector2.Min(min, point);
                        max = Vector2.Max(max, point);
                    }
                _bounds = min.X <= max.X ? RectF.FromEdges(min.X, min.Y, max.X, max.Y) : new RectF(0, 0, -1, -1);
            }
            return _outline;
        }

        /// <summary>The outline's bounds (negative width when empty).</summary>
        public RectF Bounds
        {
            get
            {
                Outline();
                return _bounds;
            }
        }

        public void Reset()
        {
            _outline = null;
        }
        // The paint inherited from PaintParent (within the SVG whose ids are PaintIds).
        public SvgPaint? Paint, PaintParent;
        public Dictionary<string, UiElement>? PaintIds;
    }

    readonly System.Runtime.CompilerServices.ConditionalWeakTable<UiElement, SvgShape> _svgShapes = new();
    readonly System.Runtime.CompilerServices.ConditionalWeakTable<UiElement, Tuple<int, Dictionary<string, UiElement>>> _svgIds = new();

    SvgShape ShapeOf(UiElement element)
    {
        var shape = _svgShapes.GetOrCreateValue(element);
        if (shape.Version != element.AttributeVersion)
        {
            shape.Version = element.AttributeVersion;
            shape.Reset();
            shape.Path = ShapePath(element);
            shape.AttributeTransform = AttributeTransform(element.GetAttribute("transform"));
            (shape.FillBox, shape.Geometry) = FillBoxOf(element, shape.Path);
            shape.Paint = null;
        }
        return shape;
    }

    /// <summary>The paint an element inherits from <paramref name="parent"/>, kept while neither changes.</summary>
    SvgPaint InheritKept(SvgPaint parent, UiElement element, Dictionary<string, UiElement> ids)
    {
        var shape = ShapeOf(element);
        if (shape.Paint is { } kept && ReferenceEquals(shape.PaintParent, parent) && ReferenceEquals(shape.PaintIds, ids))
            return kept;
        var paint = Inherit(parent, element, ids);
        shape.Paint = paint;
        shape.PaintParent = parent;
        shape.PaintIds = ids;
        return paint;
    }

    readonly System.Runtime.CompilerServices.ConditionalWeakTable<UiElement, SvgPaint> _svgRootPaints = new();

    /// <summary>The elements with ids in an SVG (for use, gradients), gathered again when anything in it changes.</summary>
    Dictionary<string, UiElement> SvgIds(UiElement svg)
    {
        if (_svgIds.TryGetValue(svg, out var cached) && cached.Item1 == svg.SubtreeVersion)
            return cached.Item2;
        var ids = new Dictionary<string, UiElement>(StringComparer.Ordinal);
        foreach (var element in svg.Descendants())
            if (element.Id is { } id)
                ids.TryAdd(id, element);
        _svgIds.AddOrUpdate(svg, Tuple.Create(svg.SubtreeVersion, ids));
        return ids;
    }

    /// <summary>An inline svg's content: each shape a display item of its own (the replaced content isn't one item).</summary>
    void PaintSvg(LayoutBox box, UiElement svg, RectF content) =>
        PaintSvgTree(svg, content, box.Style.Color, clip: box.Style.OverflowX != Overflow.Visible, zoom: box.Style.Zoom, items: true);

    readonly Dictionary<string, (UiElement Svg, Vector2? Size, float Ratio)?> _svgImages = new(StringComparer.Ordinal);

    /// <summary>
    /// An SVG image (a data URL or an .svg file), parsed once: its natural size when it has absolute
    /// width and height, and its aspect ratio (from those or the viewBox).
    /// </summary>
    (UiElement Svg, Vector2? Size, float Ratio)? SvgImage(string url)
    {
        if (_svgImages.TryGetValue(url, out var cached))
            return cached;
        (UiElement, Vector2?, float)? result = null;
        const string prefix = "data:image/svg+xml";
        string? text = null;
        if (url.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            var comma = url.IndexOf(',');
            var meta = comma > 0 ? url[..comma] : "";
            var data = comma > 0 ? url[(comma + 1)..] : "";
            text = meta.EndsWith(";base64", StringComparison.OrdinalIgnoreCase)
                ? System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(data))
                : Uri.UnescapeDataString(data);
        }
        else if (url.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
            text = TextResource?.Invoke(url);
        if (text is not null)
        {
            var svg = Markup.Parse(text).OfType<UiElement>().FirstOrDefault(e => e.Tag == "svg");
            if (svg is not null)
            {
                Vector2? size = null;
                var ratio = 0f;
                var w = svg.GetAttribute("width");
                var h = svg.GetAttribute("height");
                if (w is not null && h is not null && !w.EndsWith('%') && !h.EndsWith('%'))
                {
                    size = new Vector2(Parse(w.Replace("px", "")), Parse(h.Replace("px", "")));
                    ratio = size.Value.Y > 0 ? size.Value.X / size.Value.Y : 0;
                }
                else if (ParseNumbers(svg.GetAttribute("viewbox")) is [_, _, var vw, var vh] && vh > 0)
                    ratio = vw / vh;
                result = (svg, size, ratio);
            }
        }
        _svgImages[url] = result;
        return result;
    }

    readonly Dictionary<(string Url, int Width, int Height), Graphics.GpuImage?> _filterImages = [];

    /// <summary>
    /// An SVG image whose content is a rectangle filled through feTurbulence (and feColorMatrix):
    /// rendered to a texture as the filter defines it (linearRGB, converted to sRGB). Null for other SVGs.
    /// </summary>
    Graphics.GpuImage? FilterImage(string url, UiElement svg, RectF tile)
    {
        if (!_canvas.Draws)
            return null;
        var rect = svg.ElementChildren.FirstOrDefault(e => e.Tag == "rect" && e.HasAttribute("filter"));
        if (rect is null)
            return null;
        var width = Math.Max(1, (int)MathF.Round(tile.Width));
        var height = Math.Max(1, (int)MathF.Round(tile.Height));
        var key = (url, width, height);
        if (_filterImages.TryGetValue(key, out var cached))
            return cached;
        Graphics.GpuImage? image = null;
        var filterRef = rect.GetAttribute("filter") ?? "";
        var id = filterRef.StartsWith("url(#") ? filterRef[5..^1] : "";
        var filter = svg.Descendants().FirstOrDefault(e => e.Tag == "filter" && e.Id == id);
        var turbulence = filter?.ElementChildren.FirstOrDefault(e => e.Tag == "feturbulence");
        if (turbulence is not null && width * height <= 4096 * 4096)
        {
            var frequencies = ParseNumbers(turbulence.GetAttribute("basefrequency")) ?? [0];
            var fx = frequencies.Length > 0 ? frequencies[0] : 0;
            var fy = frequencies.Length > 1 ? frequencies[1] : fx;
            var octaves = int.TryParse(turbulence.GetAttribute("numoctaves"), out var n) ? n : 1;
            var seed = (long)Parse(turbulence.GetAttribute("seed") ?? "0");
            var fractal = turbulence.GetAttribute("type") == "fractalNoise";
            var stitch = turbulence.GetAttribute("stitchtiles") == "stitch";
            // The SVG's user units across the tile.
            var viewBox = ParseNumbers(svg.GetAttribute("viewbox"));
            var unitsWide = viewBox is [_, _, var vw, _] ? vw : svg.GetAttribute("width") is { } w ? Parse(w) : width;
            var scale = width / MathF.Max(unitsWide, 1e-3f);
            var noise = new Imaging.Turbulence(seed).Render(width, height, fx, fy, octaves, fractal, stitch, scale);
            var matrix = filter!.ElementChildren.FirstOrDefault(e => e.Tag == "fecolormatrix");
            var values = ParseNumbers(matrix?.GetAttribute("values"));
            var opacity = Math.Clamp(Parse(rect.GetAttribute("opacity") ?? "1"), 0, 1);
            var pixels = new byte[width * height * 4];
            static float ToSrgb(float c) => c <= 0.0031308f ? c * 12.92f : 1.055f * MathF.Pow(c, 1 / 2.4f) - 0.055f;
            for (var i = 0; i < width * height; i++)
            {
                float r = noise[i * 4], g = noise[i * 4 + 1], b = noise[i * 4 + 2], a = noise[i * 4 + 3];
                if (values is { Length: 20 } m)
                {
                    var nr = m[0] * r + m[1] * g + m[2] * b + m[3] * a + m[4];
                    var ng = m[5] * r + m[6] * g + m[7] * b + m[8] * a + m[9];
                    var nb = m[10] * r + m[11] * g + m[12] * b + m[13] * a + m[14];
                    var na = m[15] * r + m[16] * g + m[17] * b + m[18] * a + m[19];
                    (r, g, b, a) = (Math.Clamp(nr, 0, 1), Math.Clamp(ng, 0, 1), Math.Clamp(nb, 0, 1), Math.Clamp(na, 0, 1));
                }
                pixels[i * 4] = (byte)MathF.Round(ToSrgb(r) * 255);
                pixels[i * 4 + 1] = (byte)MathF.Round(ToSrgb(g) * 255);
                pixels[i * 4 + 2] = (byte)MathF.Round(ToSrgb(b) * 255);
                pixels[i * 4 + 3] = (byte)MathF.Round(a * opacity * 255);
            }
            image = _canvas.CreateTexture(width, height, pixels);
        }
        _filterImages[key] = image;
        return image;
    }

    void PaintSvgTree(UiElement svg, RectF content, Color currentColor, bool clip, float zoom = 1, bool items = false)
    {
        if (content.Width <= 0 || content.Height <= 0 || !_canvas.Drawing)
            return;
        var scope = Scope(svg, content, zoom);
        var transform = SvgViewTransform(svg, content, zoom);
        if (clip)
            _canvas.PushClip(content);
        _canvas.PushTransform(transform);
        var rootPaint = SvgRootPaint(svg, currentColor, scope.Ids);
        foreach (var child in svg.ElementChildren)
            PaintSvgElement(child, rootPaint, scope, 0, items);
        _canvas.PopTransform();
        if (clip)
            _canvas.PopClip();
    }

    /// <summary>An svg's ids and its viewport in user units: the viewBox's size, else its content box in CSS px.</summary>
    SvgScope Scope(UiElement svg, RectF content, float zoom) =>
        new(SvgIds(svg), ParseNumbers(svg.GetAttribute("viewbox")) is [_, _, var vw, var vh] && vw > 0 && vh > 0 ? new Vector2(vw, vh) : content.Size / zoom);

    /// <summary>
    /// The root's viewBox and preserveAspectRatio: user units to the content box. Without a viewBox a
    /// user unit is a CSS px, zoomed (LayoutSVGRoot's local-to-border-box transform scales by the zoom).
    /// </summary>
    static Matrix3x2 SvgViewTransform(UiElement svg, RectF content, float zoom = 1)
    {
        var viewBox = ParseNumbers(svg.GetAttribute("viewbox"));
        if (viewBox is not [var vx, var vy, var vw, var vh] || vw <= 0 || vh <= 0)
            return Matrix3x2.CreateScale(zoom) * Matrix3x2.CreateTranslation(content.X, content.Y);
        var preserve = (svg.GetAttribute("preserveaspectratio") ?? "xMidYMid meet").Trim();
        var parts = preserve.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var align = parts.Length > 0 ? parts[0] : "xMidYMid";
        var slice = parts.Length > 1 && parts[1] == "slice";
        float sx = content.Width / vw, sy = content.Height / vh;
        if (align != "none")
        {
            var scale = slice ? MathF.Max(sx, sy) : MathF.Min(sx, sy);
            sx = sy = scale;
        }
        var tx = -vx * sx;
        var ty = -vy * sy;
        if (align != "none")
        {
            var extraX = content.Width - vw * sx;
            var extraY = content.Height - vh * sy;
            tx += align.Contains("xMid") ? extraX / 2 : align.Contains("xMax") ? extraX : 0;
            ty += align.Contains("YMid") ? extraY / 2 : align.Contains("YMax") ? extraY : 0;
        }
        return Matrix3x2.CreateScale(sx, sy) * Matrix3x2.CreateTranslation(content.X + tx, content.Y + ty);
    }

    /// <summary>The paint the root starts with (the same instance from frame to frame, so the paints inherited from it are kept).</summary>
    SvgPaint SvgRootPaint(UiElement svg, Color currentColor, Dictionary<string, UiElement> ids)
    {
        if (!_svgRootPaints.TryGetValue(svg, out var paint) || paint.CurrentColor != currentColor)
        {
            paint = new SvgPaint(Brush.Solid(Color.Black), 1, null, 1, 1, 1, FillRule.NonZero, LineCap.Butt, LineJoin.Miter, currentColor, null, null);
            _svgRootPaints.AddOrUpdate(svg, paint);
        }
        return InheritKept(paint, svg, ids);
    }

    /// <summary>An inline SVG's content box, in the space it is painted in.</summary>
    static RectF SvgContent(LayoutBox box)
    {
        var rect = box.ViewportRect;
        return new RectF(rect.X + box.ContentLeft, rect.Y + box.ContentTop, box.ContentWidth, box.ContentHeight);
    }

    // ---------- Transforms and bounding boxes ----------

    /// <summary>
    /// An element's transform into its parent's user space (TransformHelper::ComputeTransform): its
    /// computed transform, translate, rotate and scale about its transform-origin, against its
    /// transform-box (fill-box: its object bounding box; stroke-box: its stroke bounding box;
    /// view-box: the svg's viewport from 0, 0). Computed lengths carry the zoom, user units don't: the
    /// reference box is zoomed and the result's translation unzoomed, as Blink does. An element
    /// without a computed style (in an SVG image) takes its transform attribute as it is.
    /// </summary>
    Matrix3x2? SvgTransform(UiElement element, SvgPaint paint, SvgScope scope)
    {
        if (element.Style is not { } s)
            return ShapeOf(element).AttributeTransform;
        if (!s.HasTransform)
            return null;
        var box = s.TransformBox switch
        {
            TransformBox.ContentBox or TransformBox.FillBox => SvgBounds(element, paint, scope, SvgBoxKind.Fill, 0),
            TransformBox.BorderBox or TransformBox.StrokeBox => SvgBounds(element, paint, scope, SvgBoxKind.Stroke, 0),
            _ => new RectF(0, 0, scope.Viewport.X, scope.Viewport.Y),
        } ?? new RectF(0, 0, 0, 0);
        var zoom = s.Zoom;
        var m = CssTransform(s, new RectF(box.X * zoom, box.Y * zoom, box.Width * zoom, box.Height * zoom));
        m.M31 /= zoom;
        m.M32 /= zoom;
        return m;
    }

    /// <summary>An element's transform with a use element's x and y (applied first, in its transformed space).</summary>
    Matrix3x2? SvgLocalToParent(UiElement element, SvgPaint paint, SvgScope scope)
    {
        var transform = SvgTransform(element, paint, scope);
        if (element.Tag != "use" || (Number(element, "x") == 0 && Number(element, "y") == 0))
            return transform;
        var offset = Matrix3x2.CreateTranslation(Number(element, "x"), Number(element, "y"));
        return transform is { } m ? offset * m : offset;
    }

    enum SvgBoxKind { Fill, Stroke }

    /// <summary>
    /// Blink's bounding boxes in an element's own user space: a shape's object (fill) bounding box or
    /// its stroke bounding box (LayoutSVGShape::ApproximateStrokeBoundingBox, which is also its
    /// visual rect); a container's is the union of its children's in its space; null when empty.
    /// (Blink strokes a path to find its exact stroke-box reference box; the approximation stands in.)
    /// </summary>
    RectF? SvgBounds(UiElement element, SvgPaint paint, SvgScope scope, SvgBoxKind kind, int depth)
    {
        if (depth > 32)
            return null;
        switch (element.Tag)
        {
            case "g":
            case "svg":
            case "a":
            case "switch":
            {
                RectF? union = null;
                foreach (var child in element.ElementChildren)
                {
                    if (SvgSkipped.Contains(child.Tag) || child.GetAttribute("display") == "none")
                        continue;
                    var childPaint = InheritKept(paint, child, scope.Ids);
                    if (SvgBounds(child, childPaint, scope, kind, depth + 1) is not { } rect)
                        continue;
                    if (SvgLocalToParent(child, childPaint, scope) is { } m)
                        rect = rect.Transform(m);
                    union = union is { } u ? u.Union(rect) : rect;
                }
                return union;
            }
            case "use":
            {
                var href = element.GetAttribute("href") ?? element.GetAttribute("xlink:href");
                if (href is not { Length: > 1 } || href[0] != '#' || !scope.Ids.TryGetValue(href[1..], out var target))
                    return null;
                RectF? union = null;
                foreach (var part in target.Tag == "symbol" ? target.ElementChildren : [target])
                {
                    var partPaint = InheritKept(paint, part, scope.Ids);
                    if (SvgSkipped.Contains(part.Tag) || SvgBounds(part, partPaint, scope, kind, depth + 1) is not { } rect)
                        continue;
                    if (SvgTransform(part, partPaint, scope) is { } m)
                        rect = rect.Transform(m);
                    union = union is { } u ? u.Union(rect) : rect;
                }
                return union;
            }
            default:
                var shape = ShapeOf(element);
                return shape.FillBox is { } fill ? kind == SvgBoxKind.Fill ? fill : StrokeBox(fill, paint, shape.Geometry) : null;
        }
    }

    /// <summary>
    /// LayoutSVGShape::ApproximateStrokeBoundingBox (the fill box when there's no stroke): half the
    /// stroke width around the fill box, for paths with miter joins times the miter limit (√2 for
    /// square caps under a limit below √2), for lines and paths with square caps times √2.
    /// </summary>
    static RectF StrokeBox(RectF fill, SvgPaint paint, SvgGeometry geometry)
    {
        const float Sqrt2 = 1.41421356f;
        if (paint.Stroke is null || paint.StrokeWidth <= 0)
            return fill;
        var delta = paint.StrokeWidth / 2;
        if (geometry is SvgGeometry.Path or SvgGeometry.Line)
        {
            // The engine reads no stroke-miterlimit: SVG's initial 4.
            const float miter = 4;
            if (geometry == SvgGeometry.Path && paint.Join == LineJoin.Miter)
                delta *= miter < Sqrt2 && paint.Cap == LineCap.Square ? Sqrt2 : MathF.Max(miter, 1);
            else if (paint.Cap == LineCap.Square)
                delta *= Sqrt2;
        }
        return RectF.FromEdges(fill.Left - delta, fill.Top - delta, fill.Right + delta, fill.Bottom + delta);
    }

    /// <summary>
    /// A shape's object bounding box as Blink makes it (UpdateShapeFromElement): a rect's x, y, width
    /// and height, an ellipse's or circle's centre and radii, a path's tight bounds; null when the shape
    /// is empty (it renders nothing).
    /// </summary>
    static (RectF?, SvgGeometry) FillBoxOf(UiElement e, CanvasPath? path)
    {
        if (path is null)
            return (null, SvgGeometry.Simple);
        switch (e.Tag)
        {
            case "rect":
                return (new RectF(Number(e, "x"), Number(e, "y"), Number(e, "width"), Number(e, "height")), SvgGeometry.Simple);
            case "circle":
            {
                var r = Number(e, "r");
                return (new RectF(Number(e, "cx") - r, Number(e, "cy") - r, 2 * r, 2 * r), SvgGeometry.Simple);
            }
            case "ellipse":
            {
                float rx = Number(e, "rx"), ry = Number(e, "ry");
                return (new RectF(Number(e, "cx") - rx, Number(e, "cy") - ry, 2 * rx, 2 * ry), SvgGeometry.Simple);
            }
            default:
                return path.IsEmpty ? (null, SvgGeometry.Path) : (path.TightBounds, path.IsLine ? SvgGeometry.Line : SvgGeometry.Path);
        }
    }

    /// <summary>gfx::ToEnclosingRect.</summary>
    static RectF Enclosing(RectF r) => RectF.FromEdges(MathF.Floor(r.Left), MathF.Floor(r.Top), MathF.Ceiling(r.Right), MathF.Ceiling(r.Bottom));

    /// <summary>
    /// An element inside an inline svg: its object bounding box in its own user space and the matrix
    /// from there to the space the svg's box is painted in (before the box's CSS transforms); null
    /// when it isn't rendered (inside defs or another element that renders nothing, display none,
    /// an empty shape).
    /// </summary>
    internal (RectF Box, Matrix3x2 ToViewport)? SvgElementGeometry(LayoutBox svgBox, UiElement element)
    {
        var svg = svgBox.Element!;
        var chain = new List<UiElement>();
        for (var e = element; e != svg; e = e.Parent)
        {
            if (e is null)
                return null;
            chain.Add(e);
        }
        var content = SvgContent(svgBox);
        if (content.Width <= 0 || content.Height <= 0)
            return null;
        var scope = Scope(svg, content, svgBox.Style.Zoom);
        var paint = SvgRootPaint(svg, svgBox.Style.Color, scope.Ids);
        var toViewport = SvgViewTransform(svg, content, svgBox.Style.Zoom);
        for (var i = chain.Count - 1; i >= 0; i--)
        {
            var e = chain[i];
            if (SvgSkipped.Contains(e.Tag) || e.GetAttribute("display") == "none")
                return null;
            paint = InheritKept(paint, e, scope.Ids);
            if (SvgLocalToParent(e, paint, scope) is { } local)
                toViewport = local * toViewport;
        }
        return SvgBounds(element, paint, scope, SvgBoxKind.Fill, 0) is { } box ? (box, toViewport) : null;
    }

    // ---------- Hit testing ----------

    /// <summary>
    /// What a point hits inside an inline SVG (in the space the box is painted in), as Blink finds it:
    /// the topmost shape whose fill or stroke covers the point (a use counts as itself), else the svg
    /// itself inside its border box, else nothing (its overflowing area is empty there).
    /// </summary>
    internal UiElement? HitSvg(LayoutBox box, Vector2 point)
    {
        if (box.Element is not { } svg)
            return null;
        var content = SvgContent(box);
        if (content.Width > 0 && content.Height > 0)
        {
            var scope = Scope(svg, content, box.Style.Zoom);
            var root = SvgRootPaint(svg, box.Style.Color, scope.Ids);
            var transform = SvgViewTransform(svg, content, box.Style.Zoom);
            for (var i = svg.Children.Count - 1; i >= 0; i--)
                if (svg.Children[i] is UiElement child && HitSvgElement(child, root, scope, transform, point, 0) is { } hit)
                    return hit;
        }
        return HitRegion.Inside(box.ViewportRect, BorderRadii(box, box.ViewportRect.Size), point) ? svg : null;
    }

    UiElement? HitSvgElement(UiElement element, SvgPaint parent, SvgScope scope, Matrix3x2 transform, Vector2 point, int depth)
    {
        if (depth > 32 || SvgSkipped.Contains(element.Tag) || element.GetAttribute("display") == "none" || element.GetAttribute("visibility") == "hidden"
            || Attribute(element, "pointer-events") == "none")
            return null;
        var paint = InheritKept(parent, element, scope.Ids);
        var shape = ShapeOf(element);
        if (SvgTransform(element, paint, scope) is { } local)
            transform = local * transform;
        switch (element.Tag)
        {
            case "g":
            case "svg":
            case "a":
                for (var i = element.Children.Count - 1; i >= 0; i--)
                    if (element.Children[i] is UiElement child && HitSvgElement(child, paint, scope, transform, point, depth + 1) is { } hit)
                        return hit;
                return null;
            case "use":
            {
                var href = element.GetAttribute("href") ?? element.GetAttribute("xlink:href");
                if (href is not { Length: > 1 } || href[0] != '#' || !scope.Ids.TryGetValue(href[1..], out var target))
                    return null;
                var placed = Matrix3x2.CreateTranslation(Number(element, "x"), Number(element, "y")) * transform;
                if (target.Tag == "symbol")
                {
                    for (var i = target.Children.Count - 1; i >= 0; i--)
                        if (target.Children[i] is UiElement child && HitSvgElement(child, paint, scope, placed, point, depth + 1) is not null)
                            return element;
                    return null;
                }
                return HitSvgElement(target, paint, scope, placed, point, depth + 1) is not null ? element : null;
            }
            default:
                return shape.Path is not null && Matrix3x2.Invert(transform, out var inverse) && HitShape(shape, paint, Vector2.Transform(point, inverse), element.Tag is "line" or "polyline")
                    ? element
                    : null;
        }
    }

    /// <summary>Does a point (in the shape's units) fall in its fill (by the fill rule) or within its stroke?</summary>
    static bool HitShape(SvgShape shape, SvgPaint paint, Vector2 p, bool openShape)
    {
        var outline = shape.Outline();
        var bounds = shape.Bounds;
        var halfStroke = paint.Stroke is not null ? paint.StrokeWidth / 2 : 0;
        if (p.X < bounds.Left - halfStroke || p.X > bounds.Right + halfStroke || p.Y < bounds.Top - halfStroke || p.Y > bounds.Bottom + halfStroke)
            return false;
        if (!openShape && paint.Fill is not null)
        {
            var winding = 0;
            var crossings = 0;
            foreach (var (points, _) in outline)
                for (var i = 0; i < points.Count; i++)
                {
                    // Every subpath is closed for filling.
                    var a = points[i];
                    var b = points[(i + 1) % points.Count];
                    if ((a.Y <= p.Y) != (b.Y <= p.Y))
                    {
                        var x = a.X + (p.Y - a.Y) / (b.Y - a.Y) * (b.X - a.X);
                        if (x > p.X)
                        {
                            crossings++;
                            winding += b.Y > a.Y ? 1 : -1;
                        }
                    }
                }
            if (paint.Rule == FillRule.EvenOdd ? crossings % 2 == 1 : winding != 0)
                return true;
        }
        if (halfStroke > 0)
            foreach (var (points, closed) in outline)
            {
                var count = closed ? points.Count : points.Count - 1;
                for (var i = 0; i < count; i++)
                {
                    var a = points[i];
                    var b = points[(i + 1) % points.Count];
                    var ab = b - a;
                    var t = ab.LengthSquared() > 0 ? Math.Clamp(Vector2.Dot(p - a, ab) / ab.LengthSquared(), 0, 1) : 0;
                    if (Vector2.Distance(p, a + ab * t) <= halfStroke)
                        return true;
                }
            }
        return false;
    }

    /// <summary>
    /// The area an SVG's shapes cover (in the space the box is painted in), for an svg whose overflow
    /// is visible: its border box and its children's visual rects.
    /// </summary>
    internal RectF SvgOverflow(LayoutBox box)
    {
        var svg = box.Element!;
        var content = SvgContent(box);
        var bounds = box.ViewportRect;
        if (content.Width > 0 && content.Height > 0)
        {
            var scope = Scope(svg, content, box.Style.Zoom);
            var root = SvgRootPaint(svg, box.Style.Color, scope.Ids);
            var view = SvgViewTransform(svg, content, box.Style.Zoom);
            foreach (var child in svg.ElementChildren)
            {
                if (SvgSkipped.Contains(child.Tag) || child.GetAttribute("display") == "none")
                    continue;
                var paint = InheritKept(root, child, scope.Ids);
                if (SvgBounds(child, paint, scope, SvgBoxKind.Stroke, 0) is { } rect)
                    bounds = bounds.Union(rect.Transform(SvgLocalToParent(child, paint, scope) is { } m ? m * view : view));
            }
        }
        return bounds;
    }

    static readonly HashSet<string> SvgSkipped = ["defs", "lineargradient", "radialgradient", "stop", "clippath", "mask", "filter", "pattern", "title", "desc", "style", "symbol", "metadata", "text", "image", "foreignobject", "marker"];

    void PaintSvgElement(UiElement element, SvgPaint parent, SvgScope scope, int depth, bool items)
    {
        // Filter effects (feTurbulence noise and the like) aren't drawn: such shapes are left out.
        if (depth > 32 || SvgSkipped.Contains(element.Tag) || element.GetAttribute("display") == "none" || element.GetAttribute("visibility") == "hidden" || element.HasAttribute("filter"))
            return;
        var paint = InheritKept(parent, element, scope.Ids);
        var shape = ShapeOf(element);
        var transform = SvgTransform(element, paint, scope);
        if (transform is { } m)
            _canvas.PushTransform(m);
        switch (element.Tag)
        {
            case "g":
            case "svg":
            case "a":
                foreach (var child in element.ElementChildren)
                    PaintSvgElement(child, paint, scope, depth + 1, items);
                break;
            case "use":
                var href = element.GetAttribute("href") ?? element.GetAttribute("xlink:href");
                if (href is { Length: > 1 } && href[0] == '#' && scope.Ids.TryGetValue(href[1..], out var target))
                {
                    var offset = Matrix3x2.CreateTranslation(Number(element, "x"), Number(element, "y"));
                    _canvas.PushTransform(offset);
                    if (target.Tag == "symbol")
                        foreach (var child in target.ElementChildren)
                            PaintSvgElement(child, paint, scope, depth + 1, items);
                    else
                        PaintSvgElement(target, paint, scope, depth + 1, items);
                    _canvas.PopTransform();
                }
                break;
            default:
                if (shape.Path is { } path && shape.FillBox is { } fill)
                {
                    // SVGDrawingRecorder: a display item per shape, its visual rect enclosed in whole user units.
                    if (items)
                        _canvas.BeginItem(visualRect: Enclosing(StrokeBox(fill, paint, shape.Geometry)));
                    DrawSvgShape(path, paint, fill, scope.Ids, element.Tag is "line" or "polyline");
                    if (items)
                        _canvas.EndItem();
                }
                break;
        }
        if (transform is not null)
            _canvas.PopTransform();
    }

    /// <summary>A shape's fill and stroke (<paramref name="box"/>: its object bounding box, for objectBoundingBox paint servers).</summary>
    void DrawSvgShape(CanvasPath path, SvgPaint paint, RectF box, Dictionary<string, UiElement> ids, bool openShape)
    {
        if (!openShape && paint.Fill is not null && paint.FillOpacity * paint.Opacity > 0)
            _canvas.FillPath(path, Fade(ResolvePaint(paint.Fill, paint.FillUrl, box, ids), paint.FillOpacity * paint.Opacity), paint.Rule);
        if (paint.Stroke is not null && paint.StrokeWidth > 0 && paint.StrokeOpacity * paint.Opacity > 0)
            _canvas.StrokePath(path, Fade(ResolvePaint(paint.Stroke, paint.StrokeUrl, box, ids), paint.StrokeOpacity * paint.Opacity), new StrokeStyle(paint.StrokeWidth, paint.Join, paint.Cap));
    }

    static Brush Fade(Brush brush, float alpha)
    {
        if (alpha >= 1)
            return brush;
        if (!brush.IsGradient)
            return Brush.Solid(brush.Color with { A = brush.Color.A * alpha });
        return brush with { Stops = brush.Stops!.Select(s => s with { Color = s.Color with { A = s.Color.A * alpha } }).ToArray() };
    }

    /// <summary>A url(#gradient) paint, in the shape's user space (objectBoundingBox units map onto the shape's bounding box).</summary>
    static Brush ResolvePaint(Brush fallback, string? url, RectF bounds, Dictionary<string, UiElement> ids)
    {
        if (url is null || !ids.TryGetValue(url, out var gradient) || gradient.Tag is not ("lineargradient" or "radialgradient"))
            return fallback;
        var stops = gradient.ElementChildren.Where(e => e.Tag == "stop").Select(stop =>
        {
            var offset = stop.GetAttribute("offset") is { } o ? (o.EndsWith('%') ? Parse(o[..^1]) / 100 : Parse(o)) : 0;
            var color = SvgColor(stop.GetAttribute("stop-color") ?? StyleValue(stop, "stop-color") ?? "black", Color.Black) ?? Color.Black;
            var opacity = Parse(stop.GetAttribute("stop-opacity") ?? StyleValue(stop, "stop-opacity") ?? "1");
            return new GradientStop(Math.Clamp(offset, 0, 1), color with { A = color.A * opacity });
        }).ToArray();
        if (stops.Length == 0)
            return fallback;
        var userSpace = gradient.GetAttribute("gradientunits") == "userSpaceOnUse";
        Vector2 Point(string xName, string yName, float dx, float dy)
        {
            float Coordinate(string name, float d, float origin, float extent)
            {
                var text = gradient.GetAttribute(name);
                var value = text is null ? d : text.EndsWith('%') ? Parse(text[..^1]) / 100 : Parse(text);
                return userSpace ? (text is not null && text.EndsWith('%') ? origin + value * extent : value) : origin + value * extent;
            }
            return new Vector2(Coordinate(xName, dx, bounds.X, bounds.Width), Coordinate(yName, dy, bounds.Y, bounds.Height));
        }
        if (gradient.Tag == "lineargradient")
            return Brush.Linear(Point("x1", "y1", 0, 0), Point("x2", "y2", 1, 0), stops);
        var center = Point("cx", "cy", 0.5f, 0.5f);
        var rText = gradient.GetAttribute("r");
        var r = rText is null ? 0.5f : rText.EndsWith('%') ? Parse(rText[..^1]) / 100 : Parse(rText);
        var radii = userSpace ? new Vector2(r) : new Vector2(r * bounds.Width, r * bounds.Height);
        return Brush.Radial(center, radii, stops);
    }

    static CanvasPath? ShapePath(UiElement e)
    {
        switch (e.Tag)
        {
            case "path":
                return e.GetAttribute("d") is { Length: > 0 } d ? CanvasPath.ParseSvg(d) : null;
            case "rect":
            {
                var rect = new RectF(Number(e, "x"), Number(e, "y"), Number(e, "width"), Number(e, "height"));
                if (rect.Width <= 0 || rect.Height <= 0)
                    return null;
                var rx = e.HasAttribute("rx") ? Number(e, "rx") : Number(e, "ry");
                var ry = e.HasAttribute("ry") ? Number(e, "ry") : rx;
                rx = MathF.Min(rx, rect.Width / 2);
                ry = MathF.Min(ry, rect.Height / 2);
                if (rx <= 0 || ry <= 0)
                    return CanvasPath.Rectangle(rect);
                var radii = new Vector2(rx, ry);
                return new CanvasPath()
                    .MoveTo(new Vector2(rect.X + rx, rect.Y))
                    .LineTo(new Vector2(rect.Right - rx, rect.Y)).ArcTo(radii, 0, false, true, new Vector2(rect.Right, rect.Y + ry))
                    .LineTo(new Vector2(rect.Right, rect.Bottom - ry)).ArcTo(radii, 0, false, true, new Vector2(rect.Right - rx, rect.Bottom))
                    .LineTo(new Vector2(rect.X + rx, rect.Bottom)).ArcTo(radii, 0, false, true, new Vector2(rect.X, rect.Bottom - ry))
                    .LineTo(new Vector2(rect.X, rect.Y + ry)).ArcTo(radii, 0, false, true, new Vector2(rect.X + rx, rect.Y))
                    .Close();
            }
            case "circle":
                return Number(e, "r") > 0 ? CanvasPath.Circle(new Vector2(Number(e, "cx"), Number(e, "cy")), Number(e, "r")) : null;
            case "ellipse":
                return Number(e, "rx") > 0 && Number(e, "ry") > 0 ? CanvasPath.Ellipse(new Vector2(Number(e, "cx"), Number(e, "cy")), new Vector2(Number(e, "rx"), Number(e, "ry"))) : null;
            case "line":
                return new CanvasPath().MoveTo(new Vector2(Number(e, "x1"), Number(e, "y1"))).LineTo(new Vector2(Number(e, "x2"), Number(e, "y2")));
            case "polyline":
            case "polygon":
            {
                var numbers = ParseNumbers(e.GetAttribute("points"));
                if (numbers is null || numbers.Length < 4)
                    return null;
                var path = new CanvasPath().MoveTo(new Vector2(numbers[0], numbers[1]));
                for (var i = 2; i + 1 < numbers.Length; i += 2)
                    path.LineTo(new Vector2(numbers[i], numbers[i + 1]));
                if (e.Tag == "polygon")
                    path.Close();
                return path;
            }
        }
        return null;
    }

    // ---------- Presentation attributes ----------

    static string? StyleValue(UiElement element, string property) => element.GetStyle(property);

    static string? Attribute(UiElement element, string name) => element.GetStyle(name) ?? element.GetAttribute(name);

    SvgPaint Inherit(SvgPaint parent, UiElement e, Dictionary<string, UiElement> ids)
    {
        var paint = parent;
        var current = Attribute(e, "color") is { } colorText && SvgColor(colorText, parent.CurrentColor) is { } colorValue ? colorValue : parent.CurrentColor;
        paint = paint with { CurrentColor = current };
        if (Attribute(e, "fill") is { } fill)
            paint = ParseSvgPaint(fill, current, out var fillUrl) is var brush ? paint with { Fill = brush, FillUrl = fillUrl } : paint;
        if (Attribute(e, "stroke") is { } stroke)
            paint = ParseSvgPaint(stroke, current, out var strokeUrl) is var brush ? paint with { Stroke = brush, StrokeUrl = strokeUrl } : paint;
        if (Attribute(e, "fill-opacity") is { } fo)
            paint = paint with { FillOpacity = Math.Clamp(Parse(fo), 0, 1) };
        if (Attribute(e, "stroke-opacity") is { } so)
            paint = paint with { StrokeOpacity = Math.Clamp(Parse(so), 0, 1) };
        if (Attribute(e, "stroke-width") is { } sw)
            paint = paint with { StrokeWidth = MathF.Max(0, Parse(sw.Replace("px", ""))) };
        // Group opacity is applied to each shape (not composited as a group).
        if (Attribute(e, "opacity") is { } op)
            paint = paint with { Opacity = parent.Opacity * Math.Clamp(Parse(op), 0, 1) };
        if (Attribute(e, "fill-rule") is { } rule)
            paint = paint with { Rule = rule == "evenodd" ? FillRule.EvenOdd : FillRule.NonZero };
        if (Attribute(e, "stroke-linecap") is { } cap)
            paint = paint with { Cap = cap switch { "round" => LineCap.Round, "square" => LineCap.Square, _ => LineCap.Butt } };
        if (Attribute(e, "stroke-linejoin") is { } join)
            paint = paint with { Join = join switch { "round" => LineJoin.Round, "bevel" => LineJoin.Bevel, _ => LineJoin.Miter } };
        return paint;
    }

    static Brush? ParseSvgPaint(string value, Color current, out string? url)
    {
        url = null;
        var text = value.Trim();
        if (text == "none")
            return null;
        if (text.StartsWith("url(", StringComparison.Ordinal))
        {
            var close = text.IndexOf(')');
            var inner = text[4..(close < 0 ? text.Length : close)].Trim().Trim('"', '\'');
            url = inner.StartsWith('#') ? inner[1..] : inner;
            // A fallback after the url, else black.
            var rest = close >= 0 ? text[(close + 1)..].Trim() : "";
            return rest.Length > 0 && SvgColor(rest, current) is { } fallback ? Brush.Solid(fallback) : Brush.Solid(Color.Black);
        }
        return SvgColor(text, current) is { } color ? Brush.Solid(color) : null;
    }

    static Color? SvgColor(string text, Color current)
    {
        var reader = new ValueReader(CssParser.Trim(CssTokenizer.Tokenize(text)));
        return reader.TryColor(current, out var color) ? color : null;
    }

    static float Number(UiElement e, string name) => e.GetAttribute(name) is { } v ? Parse(v.Replace("px", "")) : 0;

    static float Parse(string text) => float.TryParse(text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : 0;

    static float[]? ParseNumbers(string? text)
    {
        if (text is null)
            return null;
        var numbers = new List<float>();
        var i = 0;
        while (i < text.Length)
        {
            while (i < text.Length && (char.IsWhiteSpace(text[i]) || text[i] == ','))
                i++;
            var start = i;
            if (i < text.Length && text[i] is '-' or '+')
                i++;
            while (i < text.Length && (char.IsDigit(text[i]) || text[i] == '.' || text[i] is 'e' or 'E' || (i > start && text[i] is '-' or '+' && text[i - 1] is 'e' or 'E')))
            {
                // A second dot starts a new number ("1.5.5").
                if (text[i] == '.' && text[start..i].Contains('.'))
                    break;
                i++;
            }
            if (i == start)
            {
                i++;
                continue;
            }
            numbers.Add(Parse(text[start..i]));
        }
        return numbers.ToArray();
    }

    /// <summary>A transform attribute as a matrix (SVGTransformList::Concatenate), null when it has no functions.</summary>
    static Matrix3x2? AttributeTransform(string? text)
    {
        if (text is null || SvgTransformList.Parse(text) is not { Count: > 0 } items)
            return null;
        var result = Matrix3x2.Identity;
        foreach (var item in items)
        {
            var v = item.Values;
            var m = item.Kind switch
            {
                SvgTransformList.Kind.Matrix => new Matrix3x2(v[0], v[1], v[2], v[3], v[4], v[5]),
                SvgTransformList.Kind.Translate => Matrix3x2.CreateTranslation(v[0], v[1]),
                SvgTransformList.Kind.Scale => Matrix3x2.CreateScale(v[0], v[1]),
                SvgTransformList.Kind.Rotate => Matrix3x2.CreateRotation(v[0] * MathF.PI / 180f, new Vector2(v[1], v[2])),
                SvgTransformList.Kind.SkewX => Matrix3x2.CreateSkew(v[0] * MathF.PI / 180f, 0),
                _ => Matrix3x2.CreateSkew(0, v[0] * MathF.PI / 180f),
            };
            // Listed transforms apply right to left (the first is outermost).
            result = m * result;
        }
        return result;
    }
}

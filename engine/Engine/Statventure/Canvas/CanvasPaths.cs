using System.Numerics;
using Statventure.Core;

namespace Statventure.Canvas;

public sealed partial class Canvas
{
    readonly record struct PathKey(Path Path, Vector4 Linear, Vector2 Fraction, FillRule Rule, StrokeStyle? Stroke);

    readonly Dictionary<PathKey, (int Generation, AtlasEntry Entry, Vector2 Origin)> _pathCache = [];
    TextureAtlas? _atlas;

    /// <summary>The coverage atlas shared by rasterized paths and (later) glyphs.</summary>
    public TextureAtlas Atlas => _atlas ??= new TextureAtlas(_device);

    public void FillPath(Path path, Brush brush, FillRule rule = FillRule.NonZero) => DrawPath(path, brush, rule, null);

    public void StrokePath(Path path, Brush brush, StrokeStyle stroke) => DrawPath(path, brush, FillRule.NonZero, stroke);

    void DrawPath(Path path, Brush brush, FillRule rule, StrokeStyle? stroke) => DrawPath(path, brush, rule, stroke, _transform * Matrix3x2.CreateScale(Scale));

    /// <summary>A path drawn with <paramref name="m"/> taking its units to this canvas's pixels (an icon's to where its glyph goes).</summary>
    void DrawPath(Path path, Brush brush, FillRule rule, StrokeStyle? stroke, Matrix3x2 m)
    {
        if (path.IsEmpty)
            return;
        // Cache per shape, scale/rotation and quarter-pixel position.
        var fraction = new Vector2(Quantize(m.M31 - MathF.Floor(m.M31), 4), Quantize(m.M32 - MathF.Floor(m.M32), 4));
        var linear = new Vector4(Quantize(m.M11, 1000), Quantize(m.M12, 1000), Quantize(m.M21, 1000), Quantize(m.M22, 1000));
        var key = new PathKey(path, linear, fraction, rule, stroke);
        var whole = new Vector2(MathF.Floor(m.M31), MathF.Floor(m.M32));
        if (!_pathCache.TryGetValue(key, out var cached) || cached.Generation != Atlas.Generation)
        {
            var local = new Matrix3x2(linear.X, linear.Y, linear.Z, linear.W, fraction.X, fraction.Y);
            var contours = path.Flatten(local);
            // Strokes are built in pixels, so their width follows the transform's scale.
            var pixelStroke = stroke is null ? null : stroke with { Width = stroke.Width * MathF.Sqrt(MathF.Abs(linear.X * linear.W - linear.Y * linear.Z)) };
            IEnumerable<IReadOnlyList<Vector2>> polygons = pixelStroke is null
                ? contours.Select(c => (IReadOnlyList<Vector2>)c.Points)
                : PathRasterizer.Stroke(contours, pixelStroke);
            var list = polygons.ToList();
            if (list.Count == 0)
                return;
            var min = new Vector2(float.MaxValue);
            var max = new Vector2(float.MinValue);
            foreach (var polygon in list)
                foreach (var point in polygon)
                {
                    min = Vector2.Min(min, point);
                    max = Vector2.Max(max, point);
                }
            var origin = new Vector2(MathF.Floor(min.X) - 1, MathF.Floor(min.Y) - 1);
            var width = (int)(MathF.Ceiling(max.X) - origin.X) + 1;
            var height = (int)(MathF.Ceiling(max.Y) - origin.Y) + 1;
            if (width <= 0 || height <= 0 || width > Atlas.PageSize - 2 || height > Atlas.PageSize - 2)
                return;
            var shifted = list.Select(polygon => (IReadOnlyList<Vector2>)polygon.Select(p => p - origin).ToList());
            var coverage = PathRasterizer.Fill(shifted, width, height, rule);
            cached = (Atlas.Generation, Atlas.Add(width, height, coverage), origin);
            _pathCache[key] = cached;
        }
        var entry = cached.Entry;
        var pixels = new RectF(whole.X + cached.Origin.X, whole.Y + cached.Origin.Y, entry.Width, entry.Height);
        var primitive = PixelPrimitive(pixels, KindCoverage);
        primitive.Texture = (uint)entry.TextureIndex;
        primitive.Uv = new Vector4(entry.U0, entry.V0, entry.U1, entry.V1);
        primitive.ColorMatrix = CurrentMatrix();
        if (brush.IsGradient)
        {
            // Gradient geometry is in the path's units; express it in this pixel box.
            var toBox = m * Matrix3x2.CreateTranslation(-pixels.Position);
            var start = Vector2.Transform(brush.Start, toBox);
            var end = Vector2.Transform(brush.End, toBox);
            var radii = brush.Radii * MathF.Sqrt(MathF.Abs(m.GetDeterminant()));
            primitive.Brush = AddGradient(brush with { Start = start, End = end, Radii = radii });
            primitive.Color = Vector4.One;
        }
        else
            primitive.Color = brush.Color.Premultiplied;
        Add(primitive);
    }

    static float Quantize(float value, float steps) => MathF.Round(value * steps) / steps;
}

using System.Numerics;

namespace Statventure.Canvas;

/// <summary>
/// Rasterizes polygons into coverage (0..255 per pixel) by accumulating the exact area each edge
/// covers in every pixel (the font-rs method), so antialiasing is exact area coverage, like Skia's.
/// </summary>
public static class PathRasterizer
{
    /// <summary>Fills contours (pixel coordinates; all inside 0..width, 0..height) into a coverage bitmap.</summary>
    public static byte[] Fill(IEnumerable<IReadOnlyList<Vector2>> contours, int width, int height, FillRule rule)
    {
        var stride = width + 2;
        var accumulation = new float[stride * height + 2];
        foreach (var contour in contours)
            for (var i = 0; i < contour.Count; i++)
                Line(accumulation, stride, height, contour[i], contour[(i + 1) % contour.Count]);
        var coverage = new byte[width * height];
        for (var y = 0; y < height; y++)
        {
            var sum = 0f;
            for (var x = 0; x < stride; x++)
            {
                sum += accumulation[y * stride + x];
                if (x >= width)
                    continue;
                float value;
                if (rule == FillRule.NonZero)
                    value = MathF.Min(MathF.Abs(sum), 1f);
                else
                {
                    var folded = MathF.Abs(sum) % 2f;
                    value = folded > 1f ? 2f - folded : folded;
                }
                coverage[y * width + x] = (byte)MathF.Round(value * 255f);
            }
        }
        return coverage;
    }

    static void Line(float[] a, int stride, int height, Vector2 p0, Vector2 p1)
    {
        if (MathF.Abs(p0.Y - p1.Y) <= 1e-7f)
            return;
        var dir = 1f;
        if (p0.Y > p1.Y)
        {
            (p0, p1) = (p1, p0);
            dir = -1f;
        }
        var dxdy = (p1.X - p0.X) / (p1.Y - p0.Y);
        var x = p0.X;
        if (p0.Y < 0)
            x -= p0.Y * dxdy;
        var yStart = Math.Max(0, (int)p0.Y);
        var yEnd = Math.Min(height, (int)MathF.Ceiling(p1.Y));
        for (var y = yStart; y < yEnd; y++)
        {
            var rowStart = y * stride;
            var dy = MathF.Min(y + 1, p1.Y) - MathF.Max(y, p0.Y);
            var xNext = x + dxdy * dy;
            var d = dy * dir;
            var (x0, x1) = x < xNext ? (x, xNext) : (xNext, x);
            var x0Floor = MathF.Floor(x0);
            var x0i = (int)x0Floor;
            var x1Ceil = MathF.Ceiling(x1);
            var x1i = (int)x1Ceil;
            if (x0i < 0)
            {
                x = xNext;
                continue;
            }
            if (x1i <= x0i + 1)
            {
                var xmf = 0.5f * (x + xNext) - x0Floor;
                a[rowStart + x0i] += d - d * xmf;
                a[rowStart + x0i + 1] += d * xmf;
            }
            else
            {
                var s = 1f / (x1 - x0);
                var x0f = x0 - x0Floor;
                var a0 = 0.5f * s * (1 - x0f) * (1 - x0f);
                var x1f = x1 - x1Ceil + 1;
                var am = 0.5f * s * x1f * x1f;
                a[rowStart + x0i] += d * a0;
                if (x1i == x0i + 2)
                    a[rowStart + x0i + 1] += d * (1 - a0 - am);
                else
                {
                    var a1 = s * (1.5f - x0f);
                    a[rowStart + x0i + 1] += d * (a1 - a0);
                    for (var xi = x0i + 2; xi < x1i - 1; xi++)
                        a[rowStart + xi] += d * s;
                    var a2 = a1 + (x1i - x0i - 3) * s;
                    a[rowStart + x1i - 1] += d * (1 - a2 - am);
                }
                a[rowStart + x1i] += d * am;
            }
            x = xNext;
        }
    }

    /// <summary>
    /// Turns polylines into polygons that cover their stroke: a quad per segment, joins and caps.
    /// Every polygon winds the same way, so filling them non-zero gives the union.
    /// </summary>
    public static List<IReadOnlyList<Vector2>> Stroke(IEnumerable<(List<Vector2> Points, bool Closed)> contours, StrokeStyle style)
    {
        var polygons = new List<IReadOnlyList<Vector2>>();
        var half = style.Width / 2f;
        void Add(List<Vector2> polygon)
        {
            var area = 0f;
            for (var i = 0; i < polygon.Count; i++)
            {
                var a = polygon[i];
                var b = polygon[(i + 1) % polygon.Count];
                area += a.X * b.Y - b.X * a.Y;
            }
            if (MathF.Abs(area) < 1e-9f)
                return;
            if (area < 0)
                polygon.Reverse();
            polygons.Add(polygon);
        }
        List<Vector2> Disc(Vector2 center)
        {
            var count = Math.Clamp((int)MathF.Ceiling(half * 2f), 8, 64);
            var disc = new List<Vector2>(count);
            for (var i = 0; i < count; i++)
            {
                var angle = i * MathF.Tau / count;
                disc.Add(center + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * half);
            }
            return disc;
        }

        foreach (var (raw, closed) in contours)
        {
            // Drop repeated points.
            var points = new List<Vector2>(raw.Count);
            foreach (var point in raw)
                if (points.Count == 0 || Vector2.DistanceSquared(points[^1], point) > 1e-8f)
                    points.Add(point);
            if (closed && points.Count > 1 && Vector2.DistanceSquared(points[0], points[^1]) < 1e-8f)
                points.RemoveAt(points.Count - 1);
            if (points.Count == 1)
            {
                if (style.Cap == LineCap.Round)
                    Add(Disc(points[0]));
                continue;
            }
            var segmentCount = closed ? points.Count : points.Count - 1;
            for (var i = 0; i < segmentCount; i++)
            {
                var a = points[i];
                var b = points[(i + 1) % points.Count];
                var direction = Vector2.Normalize(b - a);
                var normal = new Vector2(-direction.Y, direction.X) * half;
                if (!closed && style.Cap == LineCap.Square)
                {
                    if (i == 0)
                        a -= direction * half;
                    if (i == segmentCount - 1)
                        b += direction * half;
                }
                Add([a + normal, b + normal, b - normal, a - normal]);
            }
            // Joins at every vertex between two segments.
            for (var i = closed ? 0 : 1; i < (closed ? points.Count : points.Count - 1); i++)
            {
                var previous = points[(i - 1 + points.Count) % points.Count];
                var vertex = points[i];
                var next = points[(i + 1) % points.Count];
                var d0 = Vector2.Normalize(vertex - previous);
                var d1 = Vector2.Normalize(next - vertex);
                var cross = d0.X * d1.Y - d0.Y * d1.X;
                if (MathF.Abs(cross) < 1e-6f && Vector2.Dot(d0, d1) > 0)
                    continue;
                if (style.Join == LineJoin.Round)
                {
                    Add(Disc(vertex));
                    continue;
                }
                // The outer side of the turn.
                var side = cross > 0 ? -1f : 1f;
                var n0 = new Vector2(-d0.Y, d0.X) * half * side;
                var n1 = new Vector2(-d1.Y, d1.X) * half * side;
                var bevel = new List<Vector2> { vertex, vertex + n0, vertex + n1 };
                if (style.Join == LineJoin.Miter)
                {
                    var cosHalf = MathF.Sqrt(MathF.Max(0, (1 + Vector2.Dot(-d0, d1)) / 2f));
                    var miterRatio = 1f / MathF.Max(MathF.Sqrt(MathF.Max(0, 1 - cosHalf * cosHalf)), 1e-6f);
                    if (miterRatio <= style.MiterLimit)
                    {
                        var bisector = Vector2.Normalize(n0 + n1);
                        var tip = vertex + bisector * half * miterRatio;
                        bevel = [vertex, vertex + n0, tip, vertex + n1];
                    }
                }
                Add(bevel);
            }
            if (!closed && style.Cap == LineCap.Round)
            {
                Add(Disc(points[0]));
                Add(Disc(points[^1]));
            }
        }
        return polygons;
    }
}

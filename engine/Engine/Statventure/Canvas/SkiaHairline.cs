using System.Numerics;

namespace Statventure.Canvas;

/// <summary>
/// A rounded rect as Skia's SkRRect holds one (SkRRect.cpp, Skia at Chromium 152's pin): its rect's edges and each
/// corner's radii (top left, top right, bottom right, bottom left), fitted as setRectRadii fits them, and its type.
/// </summary>
internal readonly struct SkRRect
{
    public enum Kind { Empty, Rect, Oval, Simple, Complex }

    public readonly float Left, Top, Right, Bottom;
    readonly Vector2 _r0, _r1, _r2, _r3;
    public readonly Kind Type;

    SkRRect(float l, float t, float r, float b, Vector2 r0, Vector2 r1, Vector2 r2, Vector2 r3, Kind type)
    {
        (Left, Top, Right, Bottom) = (l, t, r, b);
        (_r0, _r1, _r2, _r3) = (r0, r1, r2, r3);
        Type = type;
    }

    public Vector2 Radius(int corner) => corner switch { 0 => _r0, 1 => _r1, 2 => _r2, _ => _r3 };

    public bool IsRect => Type == Kind.Rect;
    public bool IsOval => Type == Kind.Oval;
    public bool IsEmpty => Type == Kind.Empty;

    /// <summary>SkRRect::setRectRadii: square corners for a radius not above zero, all radii scaled down to fit, the type.</summary>
    public static SkRRect Make(float l, float t, float r, float b, ReadOnlySpan<Vector2> radii)
    {
        if (l > r)
            (l, r) = (r, l);
        if (t > b)
            (t, b) = (b, t);
        if (!(r > l) || !(b > t))
            return new SkRRect(l, t, r, b, default, default, default, default, Kind.Empty);
        Span<Vector2> rad = stackalloc Vector2[4];
        radii.CopyTo(rad);
        if (ClampToZero(rad))
            return new SkRRect(l, t, r, b, default, default, default, default, Kind.Rect);
        // scaleRadii: the CSS rule (the smallest side over its two radii), in doubles.
        var scale = 1.0;
        var width = (double)r - l;
        var height = (double)b - t;
        scale = MinScale(rad[0].X, rad[1].X, width, scale);
        scale = MinScale(rad[1].Y, rad[2].Y, height, scale);
        scale = MinScale(rad[2].X, rad[3].X, width, scale);
        scale = MinScale(rad[3].Y, rad[0].Y, height, scale);
        FlushToZero(ref rad[0].X, ref rad[1].X);
        FlushToZero(ref rad[1].Y, ref rad[2].Y);
        FlushToZero(ref rad[2].X, ref rad[3].X);
        FlushToZero(ref rad[3].Y, ref rad[0].Y);
        if (scale < 1.0)
        {
            AdjustRadii(width, scale, ref rad[0].X, ref rad[1].X);
            AdjustRadii(height, scale, ref rad[1].Y, ref rad[2].Y);
            AdjustRadii(width, scale, ref rad[2].X, ref rad[3].X);
            AdjustRadii(height, scale, ref rad[3].Y, ref rad[0].Y);
        }
        ClampToZero(rad);
        return WithType(l, t, r, b, rad);
    }

    /// <summary>SkRRect::inset: the rect inset by d on every side, each radius that isn't zero less d, fitted again.</summary>
    public SkRRect Inset(float d)
    {
        float l = Left + d, t = Top + d, r = Right - d, b = Bottom - d;
        if (r <= l || b <= t)
            return new SkRRect(l, t, r, b, default, default, default, default, Kind.Empty);
        Span<Vector2> rad = [_r0, _r1, _r2, _r3];
        for (var i = 0; i < 4; i++)
        {
            if (rad[i].X != 0)
                rad[i].X -= d;
            if (rad[i].Y != 0)
                rad[i].Y -= d;
        }
        return Make(l, t, r, b, rad);
    }

    /// <summary>clamp_to_zero: a corner with a radius not above zero is square; true when every corner is.</summary>
    static bool ClampToZero(Span<Vector2> radii)
    {
        var allSquare = true;
        for (var i = 0; i < 4; i++)
        {
            if (radii[i].X <= 0 || radii[i].Y <= 0)
                radii[i] = Vector2.Zero;
            else
                allSquare = false;
        }
        return allSquare;
    }

    static double MinScale(double a, double b, double limit, double current) => a + b > limit ? Math.Min(current, limit / (a + b)) : current;

    /// <summary>flush_to_zero: a radius that can't be told apart beside the other's is zero.</summary>
    static void FlushToZero(ref float a, ref float b)
    {
        if (a + b == a)
            b = 0;
        else if (a + b == b)
            a = 0;
    }

    /// <summary>SkScaleToSides::AdjustRadii: both scaled, the larger nudged down an ulp at a time until they fit.</summary>
    static void AdjustRadii(double limit, double scale, ref float a, ref float b)
    {
        a = (float)(a * scale);
        b = (float)(b * scale);
        if (a + b > limit)
        {
            if (a > b)
            {
                var max = (float)(limit - b);
                while (max + b > limit)
                    max = MathF.BitDecrement(max);
                a = max;
            }
            else
            {
                var max = (float)(limit - a);
                while (max + a > limit)
                    max = MathF.BitDecrement(max);
                b = max;
            }
        }
    }

    /// <summary>computeType: square, an oval (equal radii reaching the middle), simple (equal radii) or complex.</summary>
    static SkRRect WithType(float l, float t, float r, float b, Span<Vector2> rad)
    {
        var allSquare = rad[0].X == 0 || rad[0].Y == 0;
        var allEqual = true;
        for (var i = 1; i < 4; i++)
        {
            if (rad[i].X != 0 && rad[i].Y != 0)
                allSquare = false;
            if (rad[i] != rad[i - 1])
                allEqual = false;
        }
        if (allSquare)
            return new SkRRect(l, t, r, b, default, default, default, default, Kind.Rect);
        if (allEqual)
        {
            if (rad[0].X >= (r - l) / 2f && rad[0].Y >= (b - t) / 2f)
            {
                var half = new Vector2((float)(((double)r - l) * 0.5), (float)(((double)b - t) * 0.5));
                return new SkRRect(l, t, r, b, half, half, half, half, Kind.Oval);
            }
            return new SkRRect(l, t, r, b, rad[0], rad[1], rad[2], rad[3], Kind.Simple);
        }
        return new SkRRect(l, t, r, b, rad[0], rad[1], rad[2], rad[3], Kind.Complex);
    }
}

/// <summary>
/// Skia's antialiased hairline, as Chromium 152's raster draws a stroke at most one device pixel wide
/// (skcpu::Draw::drawPath through modifyPaintForHairlines and DrawTreatAsHairline, SkDraw.cpp: the stroke drawn with
/// width 0, a narrower one keeping its width as the paint's alpha). The path, in device pixels, goes through
/// SkScan::AntiHairPath (SkScan_Hairline.cpp, hair_path with butt caps): lines as they are; a conic cut into 2^n quads
/// within a quarter pixel (SkAutoConicToQuads), each quad into 2^level lines (compute_quad_level, hair_quad); every line
/// through SkScan::AntiHairLineRgn and do_anti_hairline (SkScan_Antihair.cpp): its points as 26.6 fixed point, a
/// pixel column (or row) at a time two pixels across the line, split by the fraction of its 16.16 position, the end
/// pixels weighed by the line's part of them. Every blit is one src-over of the colour, so a pixel where two lines meet
/// is covered twice. Rounded rects are the paths SkPath::Oval and SkPath::RRect make (SkPathRawShapes.cpp).
/// </summary>
internal static class SkiaHairline
{
    /// <summary>SK_ScalarRoot2Over2: a quarter circle's conic weight.</summary>
    const float QuarterWeight = 0.707106781f;

    /// <summary>A pixel grid the blits land on: each pixel keeps the share of what was under it still showing.</summary>
    public sealed class Mask(int x, int y, int width, int height, float alpha)
    {
        public readonly int X = x, Y = y, Width = width, Height = height;
        public readonly float Alpha = alpha;
        readonly float[] _showing = Fill(width * height);

        static float[] Fill(int n)
        {
            var a = new float[n];
            Array.Fill(a, 1f);
            return a;
        }

        public void Blit(int px, int py, int a)
        {
            if (a <= 0)
                return;
            int ix = px - X, iy = py - Y;
            if ((uint)ix >= (uint)Width || (uint)iy >= (uint)Height)
                return;
            _showing[iy * Width + ix] *= 1 - Alpha * a / 255f;
        }

        /// <summary>The colour's coverage at a pixel (0..1): what the blits laid over it, over the paint's alpha.</summary>
        public float Coverage(int ix, int iy) => Alpha > 0 ? MathF.Min(1, (1 - _showing[iy * Width + ix]) / Alpha) : 0;
    }

    /// <summary>
    /// SkCanvas::drawRRect of a stroke drawn as a hairline: a rect is Skia's drawRect (not a hairline: the caller's), an oval
    /// SkPath::Oval (clockwise from its right end's middle), anything else SkPath::RRect (clockwise from the left side's
    /// lower end). Returns the mask the hairline covers (the path's bounds and a pixel around), or null for an empty or
    /// square rrect.
    /// </summary>
    /// <param name="map">Skia's matrix from the rrect's units to device pixels, when it isn't the identity: the path's
    /// points are mapped before the hairline is drawn (SkPathData::MakeTransform; a conic keeps its weight).</param>
    public static Mask? RRect(in SkRRect rrect, float alpha, Matrix3x2? map = null)
    {
        if (rrect.IsEmpty || rrect.IsRect)
            return null;
        Span<Vector2> box = [new(rrect.Left, rrect.Top), new(rrect.Right, rrect.Top), new(rrect.Right, rrect.Bottom), new(rrect.Left, rrect.Bottom)];
        float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
        foreach (var corner in box)
        {
            var p = Map(corner, map);
            (minX, minY, maxX, maxY) = (MathF.Min(minX, p.X), MathF.Min(minY, p.Y), MathF.Max(maxX, p.X), MathF.Max(maxY, p.Y));
        }
        var x = (int)MathF.Floor(minX) - 2;
        var y = (int)MathF.Floor(minY) - 2;
        var mask = new Mask(x, y, (int)MathF.Ceiling(maxX) + 2 - x, (int)MathF.Ceiling(maxY) + 2 - y, alpha);
        if (rrect.IsOval)
            Oval(rrect.Left, rrect.Top, rrect.Right, rrect.Bottom, mask, map);
        else
            RoundRect(rrect, mask, map);
        return mask;
    }

    /// <summary>SkMatrix::mapPoints (affine: sx x + kx y + tx).</summary>
    static Vector2 Map(Vector2 p, Matrix3x2? map) =>
        map is { } m ? new Vector2(m.M11 * p.X + m.M21 * p.Y + m.M31, m.M12 * p.X + m.M22 * p.Y + m.M32) : p;

    /// <summary>SkPath::Oval(rect) (clockwise, start index 1): four quarter-circle conics from the right end's middle.</summary>
    static void Oval(float l, float t, float r, float b, Mask mask, Matrix3x2? map)
    {
        var cx = (float)(((double)l + r) * 0.5);
        var cy = (float)(((double)t + b) * 0.5);
        Span<Vector2> ends = [new(cx, t), new(r, cy), new(cx, b), new(l, cy)];
        Span<Vector2> corners = [new(l, t), new(r, t), new(r, b), new(l, b)];
        var current = ends[1];
        for (var i = 0; i < 4; i++)
        {
            var next = ends[(2 + i) % 4];
            Conic(Map(current, map), Map(corners[(2 + i) % 4], map), Map(next, map), QuarterWeight, mask);
            current = next;
        }
        // The close: a line from the last point to the first, of no length.
    }

    /// <summary>SkPath::RRect(rrect, clockwise, 6): from the left side's lower end, a line and a conic for each side and corner.</summary>
    static void RoundRect(in SkRRect rr, Mask mask, Matrix3x2? map)
    {
        float l = rr.Left, t = rr.Top, r = rr.Right, b = rr.Bottom;
        Span<Vector2> p =
        [
            new(l + rr.Radius(0).X, t), new(r - rr.Radius(1).X, t), new(r, t + rr.Radius(1).Y), new(r, b - rr.Radius(2).Y),
            new(r - rr.Radius(2).X, b), new(l + rr.Radius(3).X, b), new(l, b - rr.Radius(3).Y), new(l, t + rr.Radius(0).Y),
        ];
        for (var i = 0; i < p.Length; i++)
            p[i] = Map(p[i], map);
        Span<Vector2> corners = [Map(new(l, t), map), Map(new(r, t), map), Map(new(r, b), map), Map(new(l, b), map)];
        var start = p[6];
        var current = start;
        var index = 6;
        for (var i = 0; i < 4; i++)
        {
            index = (index + 1) % 8;
            Line(current, p[index], mask);
            current = p[index];
            index = (index + 1) % 8;
            Conic(current, corners[i], p[index], QuarterWeight, mask);
            current = p[index];
        }
        // The close: back to the start, where the last conic ended.
        Line(current, start, mask);
    }

    // ---------- Conics and quads (SkGeometry.cpp, SkScan_Hairline.cpp) ----------

    /// <summary>hairconic: SkAutoConicToQuads at a quarter pixel's tolerance, each quad drawn as lines.</summary>
    /// <remarks>(chopIntoQuadsPOW2's case of an extreme weight whose first cut makes a pair of lines isn't here: a rounded
    /// rect's quarter circles never cut so, their halves' control points staying a radius' fraction off their ends.)</remarks>
    static void Conic(Vector2 p0, Vector2 p1, Vector2 p2, float w, Mask mask)
    {
        var pow2 = QuadPow2(p0, p1, p2, w, 0.25f);
        var points = new List<Vector2>((2 << pow2) + 1) { p0 };
        Subdivide(p0, p1, p2, w, points, pow2);
        for (var i = 0; i < 1 << pow2; i++)
        {
            var q0 = points[2 * i];
            var q1 = points[2 * i + 1];
            var q2 = points[2 * i + 2];
            Quad(q0, q1, q2, QuadLevel(q0, q1, q2), mask);
        }
    }

    /// <summary>SkConic::computeQuadPOW2: the cuts needed for the quads to stay within the tolerance (at most 2^5 quads).</summary>
    static int QuadPow2(Vector2 p0, Vector2 p1, Vector2 p2, float w, float tolerance)
    {
        var a = w - 1;
        var k = a / (4 * (2 + a));
        var x = k * (p0.X - 2 * p1.X + p2.X);
        var y = k * (p0.Y - 2 * p1.Y + p2.Y);
        var error = MathF.Sqrt(x * x + y * y);
        var pow2 = 0;
        for (; pow2 < 5; pow2++)
        {
            if (error <= tolerance)
                break;
            error *= 0.25f;
        }
        return pow2;
    }

    readonly record struct ConicPart(Vector2 P0, Vector2 P1, Vector2 P2, float W);

    /// <summary>SkConic::chop: the conic cut at its middle.</summary>
    static (ConicPart, ConicPart) Chop(Vector2 p0, Vector2 p1, Vector2 p2, float w)
    {
        var scale = 1f / (1f + w);
        var t0 = p0 * scale;
        var t1 = p1 * (w * scale);
        var t2 = p2 * scale;
        var a = t0 + t1;
        var c = t1 + t2;
        var m = 0.5f * t0 + t1 + 0.5f * t2;
        var newW = MathF.Sqrt(0.5f + w * 0.5f);
        return (new ConicPart(p0, a, m, newW), new ConicPart(m, c, p2, newW));
    }

    static bool Between(float a, float b, float c) => (a - b) * (c - b) <= 0;

    /// <summary>subdivide: the conic cut in halves level times, each half's y kept in order where the conic's is.</summary>
    static void Subdivide(Vector2 p0, Vector2 p1, Vector2 p2, float w, List<Vector2> points, int level)
    {
        if (level == 0)
        {
            points.Add(p1);
            points.Add(p2);
            return;
        }
        var (d0, d1) = Chop(p0, p1, p2, w);
        var startY = p0.Y;
        var endY = p2.Y;
        if (Between(startY, p1.Y, endY))
        {
            var midY = d0.P2.Y;
            if (!Between(startY, midY, endY))
            {
                var closerY = MathF.Abs(midY - startY) < MathF.Abs(midY - endY) ? startY : endY;
                d0 = d0 with { P2 = new Vector2(d0.P2.X, closerY) };
                d1 = d1 with { P0 = new Vector2(d1.P0.X, closerY) };
            }
            if (!Between(startY, d0.P1.Y, d0.P2.Y))
                d0 = d0 with { P1 = new Vector2(d0.P1.X, startY) };
            if (!Between(d1.P0.Y, d1.P1.Y, endY))
                d1 = d1 with { P1 = new Vector2(d1.P1.X, endY) };
        }
        Subdivide(d0.P0, d0.P1, d0.P2, d0.W, points, level - 1);
        Subdivide(d1.P0, d1.P1, d1.P2, d1.W, points, level - 1);
    }

    /// <summary>compute_quad_level: cuts until the control point is within a pixel of the chord (each quarters it), at most 5.</summary>
    static int QuadLevel(Vector2 p0, Vector2 p1, Vector2 p2)
    {
        var dx = MathF.Abs((p0.X + p2.X) / 2f - p1.X);
        var dy = MathF.Abs((p0.Y + p2.Y) / 2f - p1.Y);
        var idx = (uint)(int)MathF.Ceiling(dx);
        var idy = (uint)(int)MathF.Ceiling(dy);
        var d = idx > idy ? idx + (idy >> 1) : idy + (idx >> 1);
        var level = (33 - BitOperations.LeadingZeroCount(d)) >> 1;
        return Math.Min(level, 5);
    }

    /// <summary>hair_quad: the quad evaluated at 2^level even steps of t (A t² + B t + C), the lines between drawn.</summary>
    static void Quad(Vector2 p0, Vector2 p1, Vector2 p2, int level, Mask mask)
    {
        var lines = 1 << level;
        var a = p2 - 2 * p1 + p0;
        var b = 2 * (p1 - p0);
        var c = p0;
        var dt = 1f / lines;
        var t = 0f;
        var previous = p0;
        for (var i = 1; i < lines; i++)
        {
            t += dt;
            var point = new Vector2((a.X * t + b.X) * t + c.X, (a.Y * t + b.Y) * t + c.Y);
            Line(previous, point, mask);
            previous = point;
        }
        Line(previous, p2, mask);
    }

    // ---------- Lines (SkScan_Antihair.cpp) ----------

    /// <summary>SkScalarToFDot6: 26.6 fixed point, truncated.</summary>
    static int FDot6(float v) => (int)(v * 64);

    /// <summary>AntiHairLineRgn for one line (no clip): its ends as 26.6 fixed point.</summary>
    static void Line(Vector2 a, Vector2 b, Mask mask) => AntiHairline(FDot6(a.X), FDot6(a.Y), FDot6(b.X), FDot6(b.Y), mask);

    /// <summary>
    /// do_anti_hairline. (Its halving of a line longer than 511 px isn't here: Chromium rasters in tiles of 256 px and clips a
    /// line to its tile first, SkLineClipper::IntersectLine, so no line it draws is that long; halved, the halves' ends
    /// would meet in a pixel and cover it twice, which Chromium's pictures don't show.)
    /// </summary>
    static void AntiHairline(int x0, int y0, int x1, int y1, Mask mask)
    {
        int startCoverage, stopCoverage, istart, istop, fstart, slope;
        Orientation kind;
        if (Math.Abs(x1 - x0) > Math.Abs(y1 - y0))
        {
            // Mostly horizontal: left to right.
            if (x0 > x1)
            {
                (x0, x1) = (x1, x0);
                (y0, y1) = (y1, y0);
            }
            istart = x0 >> 6;
            istop = (x1 + 63) >> 6;
            if (y0 == y1)
            {
                slope = 0;
                kind = Orientation.Horizontal;
                fstart = y0 << 10;
            }
            else
            {
                slope = (y1 - y0 << 16) / (x1 - x0);
                // The y at the middle of the first pixel column.
                var toCentre = 32 - (x0 & 63);
                fstart = (y0 << 10) + ((slope * toCentre + 32) >> 6);
                kind = Orientation.MostlyHorizontal;
            }
            if (istop - istart == 1)
            {
                startCoverage = x1 - x0;
                stopCoverage = 0;
            }
            else
            {
                startCoverage = 64 - (x0 & 63);
                stopCoverage = x1 & 63;
            }
        }
        else
        {
            // Mostly vertical: top to bottom.
            if (y0 > y1)
            {
                (x0, x1) = (x1, x0);
                (y0, y1) = (y1, y0);
            }
            istart = y0 >> 6;
            istop = (y1 + 63) >> 6;
            if (x0 == x1)
            {
                if (y0 == y1)
                    return;
                slope = 0;
                kind = Orientation.Vertical;
                fstart = x0 << 10;
            }
            else
            {
                slope = (x1 - x0 << 16) / (y1 - y0);
                var toCentre = 32 - (y0 & 63);
                fstart = (x0 << 10) + ((slope * toCentre + 32) >> 6);
                kind = Orientation.MostlyVertical;
            }
            if (istop - istart == 1)
            {
                startCoverage = y1 - y0;
                stopCoverage = 0;
            }
            else
            {
                startCoverage = 64 - (y0 & 63);
                stopCoverage = y1 & 63;
            }
        }
        fstart = Cap(kind, istart, fstart, slope, startCoverage, mask);
        istart++;
        var fullSpans = istop - istart - (stopCoverage > 0 ? 1 : 0);
        if (fullSpans > 0)
            fstart = Run(kind, istart, istart + fullSpans, fstart, slope, mask);
        if (stopCoverage > 0)
            Cap(kind, istop - 1, fstart, slope, stopCoverage, mask);
    }

    enum Orientation { Horizontal, MostlyHorizontal, Vertical, MostlyVertical }

    const int FixedHalf = 1 << 15;

    /// <summary>The high 8 bits of a 16.16 number's fraction.</summary>
    static int Alpha(int f) => (f >> 8) & 0xFF;

    /// <summary>scale_alpha_by_coverage: an alpha times a coverage in 64ths.</summary>
    static int Scale(int value, int coverage) => (value * coverage) >> 6;

    /// <summary>An end pixel of a line, weighed by the line's part of it (the blitters' drawCap).</summary>
    static int Cap(Orientation kind, int i, int f, int slope, int coverage, Mask mask)
    {
        f += FixedHalf;
        var p = f >> 16;
        var a = Alpha(f);
        switch (kind)
        {
            case Orientation.Horizontal:
                mask.Blit(i, p, Scale(a, coverage));
                mask.Blit(i, p - 1, Scale(255 - a, coverage));
                return f - FixedHalf;
            case Orientation.MostlyHorizontal:
                mask.Blit(i, p - 1, Scale(255 - a, coverage));
                mask.Blit(i, p, Scale(a, coverage));
                return f + slope - FixedHalf;
            case Orientation.Vertical:
                mask.Blit(p, i, Scale(a, coverage));
                mask.Blit(p - 1, i, Scale(255 - a, coverage));
                return f - FixedHalf;
            default:
                mask.Blit(p - 1, i, Scale(255 - a, coverage));
                mask.Blit(p, i, Scale(a, coverage));
                return f + slope - FixedHalf;
        }
    }

    /// <summary>The line's whole pixels from <paramref name="i"/> to <paramref name="stop"/> (the blitters' drawLine).</summary>
    static int Run(Orientation kind, int i, int stop, int f, int slope, Mask mask)
    {
        f += FixedHalf;
        switch (kind)
        {
            case Orientation.Horizontal:
            {
                var p = f >> 16;
                var a = Alpha(f);
                for (var k = i; k < stop; k++)
                {
                    mask.Blit(k, p, a);
                    mask.Blit(k, p - 1, 255 - a);
                }
                return f - FixedHalf;
            }
            case Orientation.Vertical:
            {
                var p = f >> 16;
                var a = Alpha(f);
                for (var k = i; k < stop; k++)
                {
                    mask.Blit(p, k, a);
                    mask.Blit(p - 1, k, 255 - a);
                }
                return f - FixedHalf;
            }
            case Orientation.MostlyHorizontal:
                for (var k = i; k < stop; k++)
                {
                    var p = f >> 16;
                    var a = Alpha(f);
                    mask.Blit(k, p - 1, 255 - a);
                    mask.Blit(k, p, a);
                    f += slope;
                }
                return f - FixedHalf;
            default:
                for (var k = i; k < stop; k++)
                {
                    var p = f >> 16;
                    var a = Alpha(f);
                    mask.Blit(p - 1, k, 255 - a);
                    mask.Blit(p, k, a);
                    f += slope;
                }
                return f - FixedHalf;
        }
    }
}

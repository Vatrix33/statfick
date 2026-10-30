namespace Statventure.Text.Autohint;

/// <summary>Hinting directions (FreeType's AF_Direction): opposite directions sum to zero.</summary>
internal enum Direction : sbyte
{
    None = 4,
    Right = 1,
    Left = -1,
    Up = 2,
    Down = -2,
}

internal static class Directions
{
    /// <summary>The direction of a vector, None when its arms don't differ enough (af_direction_compute).</summary>
    public static Direction Of(int dx, int dy)
    {
        Direction dir;
        int longArm, shortArm;
        if (dy >= dx)
        {
            if (dy >= -dx)
                (dir, longArm, shortArm) = (Direction.Up, dy, dx);
            else
                (dir, longArm, shortArm) = (Direction.Left, -dx, dy);
        }
        else if (dy >= -dx)
            (dir, longArm, shortArm) = (Direction.Right, dx, dy);
        else
            (dir, longArm, shortArm) = (Direction.Down, -dy, dx);
        return longArm <= 14 * Math.Abs(shortArm) ? Direction.None : dir;
    }

    public static bool IsOpposite(this Direction a, Direction b) => (sbyte)a + (sbyte)b == 0;

    public static bool IsSameAxis(this Direction a, Direction b) => Math.Abs((sbyte)a) == Math.Abs((sbyte)b);

    /// <summary>FreeType takes the absolute value: left becomes right, down becomes up.</summary>
    public static Direction Normalize(this Direction d) => d switch
    {
        Direction.Left => Direction.Right,
        Direction.Down => Direction.Up,
        _ => d,
    };
}

internal enum Orientation { Clockwise, CounterClockwise }

/// <summary>An outline point with everything hinting needs (FreeType's AF_PointRec).</summary>
internal struct Point
{
    public PointFlags Flags;
    /// <summary>Coordinates in font units.</summary>
    public int Fx, Fy;
    /// <summary>Scaled coordinates (26.6).</summary>
    public int Ox, Oy;
    /// <summary>Hinted coordinates (26.6).</summary>
    public int X, Y;
    public Direction InDir, OutDir;
    /// <summary>Coordinates that depend on the context (and, while the outline is built, links to non-near points).</summary>
    public int U, V;
    public int NextIx, PrevIx;

    public readonly bool IsOnCurve => (Flags & PointFlags.OnCurve) != 0;
    public readonly bool Has(PointFlags marker) => (Flags & marker) != 0;
}

internal readonly record struct Contour(int First, int Last)
{
    public int Next(int index) => index >= Last ? First : index + 1;
    public int Prev(int index) => index <= First ? Last : index - 1;
}

/// <summary>
/// A glyph outline prepared for hinting (skrifa's autohint Outline, FreeType's AF_GlyphHints
/// points): linked contours, near points, in and out directions, weak points, orientation.
/// </summary>
internal sealed class Outline
{
    public int UnitsPerEm;
    public Orientation? Orientation;
    public Point[] Points = [];
    public int PointCount;
    public readonly List<Contour> Contours = [];
    public int Advance;

    readonly List<UnscaledPoint> _unscaled = [];

    /// <summary>Fills the outline from a glyph; false when it can't be loaded.</summary>
    public bool Fill(AutohintFont font, uint glyph, QuirksMode quirks)
    {
        Clear();
        _unscaled.Clear();
        if (!font.DrawUnscaled(glyph, _unscaled, out var advance))
            return false;
        Advance = advance;
        UnitsPerEm = font.UnitsPerEm;
        if (Points.Length < _unscaled.Count)
            Points = new Point[_unscaled.Count];
        foreach (var p in _unscaled)
        {
            var ix = PointCount;
            if (p.IsContourStart || Contours.Count == 0)
                Contours.Add(new Contour(ix, ix));
            else
                Contours[^1] = Contours[^1] with { Last = Contours[^1].Last + 1 };
            Points[PointCount++] = new Point { Flags = p.Flags & (PointFlags.OnCurve | PointFlags.OffCurveCubic), Fx = p.X, Fy = p.Y };
        }
        // Heuristic value.
        var nearLimit = 20 * UnitsPerEm / 2048;
        LinkPoints();
        MarkNearPoints(nearLimit);
        ComputeDirections(nearLimit);
        SimplifyTopology();
        CheckRemainingWeakPoints(quirks == QuirksMode.Aot ? IsCornerFlatAot : IsCornerFlatJit);
        ComputeOrientation();
        return true;
    }

    /// <summary>Applies each dimension's scale and delta to the points.</summary>
    public void Scale(in Scale scale)
    {
        for (var i = 0; i < PointCount; i++)
        {
            ref var point = ref Points[i];
            var x = Fixed.Mul(point.Fx, scale.XScale) + scale.XDelta;
            var y = Fixed.Mul(point.Fy, scale.YScale) + scale.YDelta;
            point.Ox = x;
            point.X = x;
            point.Oy = y;
            point.Y = y;
        }
    }

    public void Clear()
    {
        UnitsPerEm = 0;
        PointCount = 0;
        Contours.Clear();
        Advance = 0;
        Orientation = null;
    }

    public Span<Point> Span => Points.AsSpan(0, PointCount);

    void LinkPoints()
    {
        foreach (var contour in Contours)
        {
            var prev = contour.Last;
            for (var ix = contour.First; ix <= contour.Last; ix++)
            {
                Points[ix].PrevIx = prev;
                prev = ix;
                Points[ix].NextIx = ix + 1;
            }
            Points[contour.Last].NextIx = contour.First;
        }
    }

    /// <summary>Marks points that are near their successor (afhints.c).</summary>
    void MarkNearPoints(int nearLimit)
    {
        foreach (var contour in Contours)
        {
            var prevIx = contour.Last;
            for (var ix = contour.First; ix <= contour.Last; ix++)
            {
                var point = Points[ix];
                ref var prev = ref Points[prevIx];
                var outX = point.Fx - prev.Fx;
                var outY = point.Fy - prev.Fy;
                if (Math.Abs(outX) + Math.Abs(outY) < nearLimit)
                    prev.Flags |= PointFlags.Near;
                prevIx = ix;
            }
        }
    }

    /// <summary>Computes the directions of in and out vectors (afhints.c).</summary>
    void ComputeDirections(int nearLimit)
    {
        var nearLimit2 = 2 * nearLimit - 1;
        foreach (var contour in Contours)
        {
            // Walk backward to find the first non-near point.
            var firstIx = contour.First;
            var ix = firstIx;
            var prevIx = contour.Prev(firstIx);
            var point = Points[firstIx];
            while (prevIx != firstIx)
            {
                var prev = Points[prevIx];
                var dx = point.Fx - prev.Fx;
                var dy = point.Fy - prev.Fy;
                if (Math.Abs(dx) + Math.Abs(dy) >= nearLimit2)
                    break;
                point = prev;
                ix = prevIx;
                prevIx = contour.Prev(prevIx);
            }
            firstIx = ix;
            // u and v hold the next and previous non-near points for now.
            Points[firstIx].U = firstIx;
            Points[firstIx].V = firstIx;
            var nextIx = firstIx;
            ix = firstIx;
            int outX = 0, outY = 0;
            while (true)
            {
                var pointIx = nextIx;
                nextIx = contour.Next(pointIx);
                var current = Points[pointIx];
                ref var next = ref Points[nextIx];
                // Accumulate the deltas until we surpass the near limit.
                outX += next.Fx - current.Fx;
                outY += next.Fy - current.Fy;
                if (Math.Abs(outX) + Math.Abs(outY) < nearLimit)
                {
                    next.Flags |= PointFlags.WeakInterpolation;
                    // (A do-while loop in FreeType.)
                    if (nextIx == firstIx)
                        break;
                    continue;
                }
                var outDir = Directions.Of(outX, outY);
                next.InDir = outDir;
                next.V = ix;
                Points[ix].U = nextIx;
                Points[ix].OutDir = outDir;
                // Intermediate points take the direction too.
                for (var inter = contour.Next(ix); inter != nextIx; inter = contour.Next(inter))
                {
                    Points[inter].InDir = outDir;
                    Points[inter].OutDir = outDir;
                }
                ix = nextIx;
                Points[ix].U = firstIx;
                Points[firstIx].V = ix;
                outX = 0;
                outY = 0;
                if (nextIx == firstIx)
                    break;
            }
        }
    }

    /// <summary>Weakens points between vectors into the same quadrant, so local extrema show reliably (afhints.c).</summary>
    void SimplifyTopology()
    {
        for (var i = 0; i < PointCount; i++)
        {
            var point = Points[i];
            if (point.Has(PointFlags.WeakInterpolation))
                continue;
            if (point.InDir == Direction.None && point.OutDir == Direction.None)
            {
                var uIndex = point.U;
                var vIndex = point.V;
                var nextU = Points[uIndex];
                var prevV = Points[vIndex];
                var inX = point.Fx - prevV.Fx;
                var inY = point.Fy - prevV.Fy;
                var outX = nextU.Fx - point.Fx;
                var outY = nextU.Fy - point.Fy;
                if ((inX ^ outX) >= 0 && (inY ^ outY) >= 0)
                {
                    // Both vectors point into the same quadrant.
                    Points[i].Flags |= PointFlags.WeakInterpolation;
                    Points[vIndex].U = uIndex;
                    Points[uIndex].V = vIndex;
                }
            }
        }
    }

    /// <summary>Marks the remaining weak points: control points, points along a straight run, flat corners, spikes (afhints.c).</summary>
    void CheckRemainingWeakPoints(Func<int, int, int, int, bool> isCornerFlat)
    {
        for (var i = 0; i < PointCount; i++)
        {
            var point = Points[i];
            if (point.Has(PointFlags.WeakInterpolation))
                continue;
            var makeWeak = false;
            if (!point.IsOnCurve)
                makeWeak = true;
            else if (point.OutDir == point.InDir)
            {
                if (point.OutDir != Direction.None)
                    makeWeak = true;
                else
                {
                    var uIndex = point.U;
                    var vIndex = point.V;
                    var nextU = Points[uIndex];
                    var prevV = Points[vIndex];
                    if (isCornerFlat(point.Fx - prevV.Fx, point.Fy - prevV.Fy, nextU.Fx - point.Fx, nextU.Fy - point.Fy))
                    {
                        // One of the vectors is more dominant.
                        makeWeak = true;
                        Points[vIndex].U = uIndex;
                        Points[uIndex].V = vIndex;
                    }
                }
            }
            else if (point.InDir.IsOpposite(point.OutDir))
                makeWeak = true;
            if (makeWeak)
                Points[i].Flags |= PointFlags.WeakInterpolation;
        }
    }

    /// <summary>The outline's overall winding (FT_Outline_Get_Orientation).</summary>
    void ComputeOrientation()
    {
        Orientation = null;
        if (PointCount == 0)
            return;
        long area = 0;
        foreach (var contour in Contours)
        {
            long prevX = Points[contour.Last].Fx, prevY = Points[contour.Last].Fy;
            for (var ix = contour.First; ix <= contour.Last; ix++)
            {
                long x = Points[ix].Fx, y = Points[ix].Fy;
                area += (y - prevY) * (x + prevX);
                (prevX, prevY) = (x, y);
            }
        }
        Orientation = area < 0 ? Autohint.Orientation.CounterClockwise : area > 0 ? Autohint.Orientation.Clockwise : null;
    }

    /// <summary>ttfautohint's ("ahead of time") flat corner test.</summary>
    static bool IsCornerFlatAot(int inX, int inY, int outX, int outY)
    {
        var dIn = Math.Abs(inX) + Math.Abs(inY);
        var dOut = Math.Abs(outX) + Math.Abs(outY);
        var dCorner = Math.Abs(inX + outX) + Math.Abs(inY + outY);
        return dIn + dOut - dCorner < dCorner >> 4;
    }

    /// <summary>FreeType's runtime flat corner test (ft_corner_is_flat).</summary>
    static bool IsCornerFlatJit(int inX, int inY, int outX, int outY)
    {
        static int Hypot(int x, int y)
        {
            x = Math.Abs(x);
            y = Math.Abs(y);
            return x > y ? x + (3 * y >> 3) : y + (3 * x >> 3);
        }
        var dIn = Hypot(inX, inY);
        var dOut = Hypot(outX, outY);
        var dHypot = Hypot(inX + outX, inY + outY);
        return dIn + dOut - dHypot < dHypot >> 4;
    }
}

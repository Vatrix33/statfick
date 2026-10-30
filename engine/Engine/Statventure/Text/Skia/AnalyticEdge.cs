using System.Numerics;

namespace Statventure.Text.Skia;

/// <summary>
/// SkAnalyticEdge and its quadratic and cubic kinds (one class holds the fields of all three):
/// an edge of the path from fUpperY to fLowerY in 16.16 fixed point, y snapped to quarter
/// pixels, curves walked as short lines by forward differencing.
/// </summary>
sealed class AnalyticEdge
{
    public enum Kind : sbyte { Line, Quad, Cubic }

    public const int DefaultAccuracy = 2;

    public AnalyticEdge? Next, Prev;
    public int X, DX, UpperX, Y, UpperY, LowerY, DY;
    public Kind EdgeType;
    public sbyte CurveCount;
    public byte CurveShift;
    public sbyte Winding;

    // Quadratic.
    public int QX, QY, QDx, QDy, QDDx, QDDy, QLastX, QLastY, SnappedX, SnappedY;

    // Cubic (SnappedY is shared).
    public int CX, CY, CDx, CDy, CDDx, CDDy, CDDDx, CDDDy, CLastX, CLastY;
    public byte ToFixedShift;

    public static int SnapY(int y)
    {
        const int accuracy = DefaultAccuracy;
        return (int)((unchecked((uint)y + (uint)(Fixed.One >> (accuracy + 1))) >> (16 - accuracy)) << (16 - accuracy));
    }

    public void GoY(int y)
    {
        if (y == unchecked(Y + Fixed.One))
        {
            X = unchecked(X + DX);
            Y = y;
        }
        else if (y != Y)
        {
            X = unchecked(UpperX + Fixed.Mul(DX, y - UpperY));
            Y = y;
        }
    }

    public void GoY(int y, int yShift)
    {
        Y = y;
        X = unchecked(X + (DX >> yShift));
    }

    public bool SetLine(Vector2 p0, Vector2 p1)
    {
        const int accuracy = DefaultAccuracy;
        const float multiplier = 1 << DefaultAccuracy;
        var x0 = Fixed.FDot6ToFixed(Fixed.FloatToFDot6(p0.X * multiplier)) >> accuracy;
        var y0 = SnapY(Fixed.FDot6ToFixed(Fixed.FloatToFDot6(p0.Y * multiplier)) >> accuracy);
        var x1 = Fixed.FDot6ToFixed(Fixed.FloatToFDot6(p1.X * multiplier)) >> accuracy;
        var y1 = SnapY(Fixed.FDot6ToFixed(Fixed.FloatToFDot6(p1.Y * multiplier)) >> accuracy);
        sbyte winding = 1;
        if (y0 > y1)
        {
            (x0, x1) = (x1, x0);
            (y0, y1) = (y1, y0);
            winding = -1;
        }
        var dy = Fixed.ToFDot6(y1 - y0);
        if (dy == 0)
            return false;
        var dx = Fixed.ToFDot6(x1 - x0);
        var slope = Fixed.QuickDiv(dx, dy);
        var absSlope = Fixed.Abs(slope);
        X = x0;
        DX = slope;
        UpperX = x0;
        Y = y0;
        UpperY = y0;
        LowerY = y1;
        DY = dx == 0 || slope == 0 ? Fixed.MaxS32 : absSlope < 1024 ? Fixed.QuickInverse(absSlope) : Fixed.Abs(Fixed.QuickDiv(dy, dx));
        EdgeType = Kind.Line;
        CurveCount = 0;
        Winding = winding;
        CurveShift = 0;
        return true;
    }

    bool UpdateLine(int x0, int y0, int x1, int y1, int slope)
    {
        if (y0 > y1)
        {
            (x0, x1) = (x1, x0);
            (y0, y1) = (y1, y0);
            Winding = (sbyte)-Winding;
        }
        var dx = Fixed.ToFDot6(x1 - x0);
        var dy = Fixed.ToFDot6(y1 - y0);
        if (dy == 0)
            return false;
        var absSlope = Fixed.Abs(Fixed.ToFDot6(slope));
        X = x0;
        DX = slope;
        UpperX = x0;
        Y = y0;
        UpperY = y0;
        LowerY = y1;
        DY = dx == 0 || slope == 0 ? Fixed.MaxS32 : absSlope < 1024 ? Fixed.QuickInverse(absSlope) : Fixed.Abs(Fixed.QuickDiv(dy, dx));
        return true;
    }

    /// <summary>Moves to the curve's next line; false when the edge is done.</summary>
    public bool Update(int lastY) =>
        CurveCount < 0 ? UpdateCubic() : CurveCount > 0 && UpdateQuadratic();

    const int MaxCoeffShift = 6;

    static int CheapDistance(int dx, int dy)
    {
        dx = Fixed.Abs(dx);
        dy = Fixed.Abs(dy);
        return dx > dy ? dx + (dy >> 1) : dy + (dx >> 1);
    }

    static int DiffToShift(int dx, int dy, int shiftAA)
    {
        var dist = CheapDistance(dx, dy);
        dist = unchecked(dist + (1 << (2 + shiftAA))) >> (3 + shiftAA);
        return (32 - Fixed.Clz((uint)dist)) >> 1;
    }

    static int FDot6ToFixedDiv2(int value) => Fixed.LeftShift(value, 16 - 6 - 1);

    bool SetQuadraticWithoutUpdate(ReadOnlySpan<Vector2> pts, int shift)
    {
        var scale = (float)(1 << (shift + 6));
        var x0 = (int)(pts[0].X * scale);
        var y0 = (int)(pts[0].Y * scale);
        var x1 = (int)(pts[1].X * scale);
        var y1 = (int)(pts[1].Y * scale);
        var x2 = (int)(pts[2].X * scale);
        var y2 = (int)(pts[2].Y * scale);
        sbyte winding = 1;
        if (y0 > y2)
        {
            (x0, x2) = (x2, x0);
            (y0, y2) = (y2, y0);
            winding = -1;
        }
        var top = Fixed.FDot6Round(y0);
        var bot = Fixed.FDot6Round(y2);
        if (top == bot)
            return false;
        {
            var dx = (Fixed.LeftShift(x1, 1) - x0 - x2) >> 2;
            var dy = (Fixed.LeftShift(y1, 1) - y0 - y2) >> 2;
            shift = DiffToShift(dx, dy, shift);
        }
        if (shift == 0)
            shift = 1;
        else if (shift > MaxCoeffShift)
            shift = MaxCoeffShift;
        Winding = winding;
        EdgeType = Kind.Quad;
        CurveCount = (sbyte)(1 << shift);
        CurveShift = (byte)(shift - 1);
        var a = FDot6ToFixedDiv2(x0 - x1 - x1 + x2);
        var b = Fixed.FDot6ToFixed(x1 - x0);
        QX = Fixed.FDot6ToFixed(x0);
        QDx = b + (a >> shift);
        QDDx = a >> (shift - 1);
        a = FDot6ToFixedDiv2(y0 - y1 - y1 + y2);
        b = Fixed.FDot6ToFixed(y1 - y0);
        QY = Fixed.FDot6ToFixed(y0);
        QDy = b + (a >> shift);
        QDDy = a >> (shift - 1);
        QLastX = Fixed.FDot6ToFixed(x2);
        QLastY = Fixed.FDot6ToFixed(y2);
        return true;
    }

    public bool SetQuadratic(ReadOnlySpan<Vector2> pts)
    {
        if (!SetQuadraticWithoutUpdate(pts, DefaultAccuracy))
            return false;
        QX >>= DefaultAccuracy;
        QY >>= DefaultAccuracy;
        QDx >>= DefaultAccuracy;
        QDy >>= DefaultAccuracy;
        QDDx >>= DefaultAccuracy;
        QDDy >>= DefaultAccuracy;
        QLastX >>= DefaultAccuracy;
        QLastY >>= DefaultAccuracy;
        QY = SnapY(QY);
        QLastY = SnapY(QLastY);
        EdgeType = Kind.Quad;
        SnappedX = QX;
        SnappedY = QY;
        return UpdateQuadratic();
    }

    public bool UpdateQuadratic()
    {
        var success = false;
        int count = CurveCount;
        var oldx = QX;
        var oldy = QY;
        var dx = QDx;
        var dy = QDy;
        int newx, newy, newSnappedX, newSnappedY;
        int shift = CurveShift;
        do
        {
            int slope;
            if (--count > 0)
            {
                newx = unchecked(oldx + (dx >> shift));
                newy = unchecked(oldy + (dy >> shift));
                if (Fixed.Abs(dy >> shift) >= Fixed.One * 2 && ((long)Fixed.Abs(dy) << 6) > Fixed.Abs(dx))
                {
                    var diffY = Fixed.ToFDot6(newy - SnappedY);
                    slope = diffY != 0 ? Fixed.QuickDiv(Fixed.ToFDot6(newx - SnappedX), diffY) : Fixed.MaxS32;
                    newSnappedY = Math.Min(QLastY, Fixed.RoundToFixed(newy));
                    newSnappedX = newx - Fixed.Mul(slope, newy - newSnappedY);
                }
                else
                {
                    newSnappedY = Math.Min(QLastY, SnapY(newy));
                    newSnappedX = newx;
                    var diffY = Fixed.ToFDot6(newSnappedY - SnappedY);
                    slope = diffY != 0 ? Fixed.QuickDiv(Fixed.ToFDot6(newx - SnappedX), diffY) : Fixed.MaxS32;
                }
                dx = unchecked(dx + QDDx);
                dy = unchecked(dy + QDDy);
            }
            else
            {
                newx = QLastX;
                newy = QLastY;
                newSnappedY = newy;
                newSnappedX = newx;
                var diffY = Fixed.ToFDot6(newy - SnappedY);
                slope = diffY != 0 ? Fixed.QuickDiv(Fixed.ToFDot6(newx - SnappedX), diffY) : Fixed.MaxS32;
            }
            if (slope < Fixed.MaxS32)
                success = UpdateLine(SnappedX, SnappedY, newSnappedX, newSnappedY, slope);
            oldx = newx;
            oldy = newy;
        } while (count > 0 && !success);
        QX = newx;
        QY = newy;
        QDx = dx;
        QDy = dy;
        SnappedX = newSnappedX;
        SnappedY = newSnappedY;
        CurveCount = (sbyte)count;
        return success;
    }

    public void KeepContinuousQuad()
    {
        SnappedX = X;
        SnappedY = Y;
    }

    static int CubicDeltaFromLine(int a, int b, int c, int d)
    {
        var oneThird = unchecked((a * 8 - b * 15 + 6 * c + d) * 19) >> 9;
        var twoThird = unchecked((a + 6 * b - c * 15 + d * 8) * 19) >> 9;
        return Math.Max(Fixed.Abs(oneThird), Fixed.Abs(twoThird));
    }

    bool SetCubicWithoutUpdate(ReadOnlySpan<Vector2> pts, int shift)
    {
        var scale = (float)(1 << (shift + 6));
        var x0 = (int)(pts[0].X * scale);
        var y0 = (int)(pts[0].Y * scale);
        var x1 = (int)(pts[1].X * scale);
        var y1 = (int)(pts[1].Y * scale);
        var x2 = (int)(pts[2].X * scale);
        var y2 = (int)(pts[2].Y * scale);
        var x3 = (int)(pts[3].X * scale);
        var y3 = (int)(pts[3].Y * scale);
        sbyte winding = 1;
        if (y0 > y3)
        {
            (x0, x3) = (x3, x0);
            (x1, x2) = (x2, x1);
            (y0, y3) = (y3, y0);
            (y1, y2) = (y2, y1);
            winding = -1;
        }
        var top = Fixed.FDot6Round(y0);
        var bot = Fixed.FDot6Round(y3);
        if (top == bot)
            return false;
        {
            var dx = CubicDeltaFromLine(x0, x1, x2, x3);
            var dy = CubicDeltaFromLine(y0, y1, y2, y3);
            shift = DiffToShift(dx, dy, 2) + 1;
        }
        if (shift > MaxCoeffShift)
            shift = MaxCoeffShift;
        var upShift = 6;
        var downShift = shift + upShift - 10;
        if (downShift < 0)
        {
            downShift = 0;
            upShift = 10 - shift;
        }
        Winding = winding;
        EdgeType = Kind.Cubic;
        CurveCount = (sbyte)Fixed.LeftShift(-1, shift);
        CurveShift = (byte)shift;
        ToFixedShift = (byte)downShift;
        var b = Fixed.LeftShift(3 * (x1 - x0), upShift);
        var c = Fixed.LeftShift(3 * (x0 - x1 - x1 + x2), upShift);
        var d = Fixed.LeftShift(x3 + 3 * (x1 - x2) - x0, upShift);
        CX = Fixed.FDot6ToFixed(x0);
        CDx = b + (c >> shift) + (d >> 2 * shift);
        CDDx = (int)(2L * c + ((3L * d) >> (shift - 1)));
        CDDDx = (int)((3L * d) >> (shift - 1));
        b = Fixed.LeftShift(3 * (y1 - y0), upShift);
        c = Fixed.LeftShift(3 * (y0 - y1 - y1 + y2), upShift);
        d = Fixed.LeftShift(y3 + 3 * (y1 - y2) - y0, upShift);
        CY = Fixed.FDot6ToFixed(y0);
        CDy = b + (c >> shift) + (d >> 2 * shift);
        CDDy = (int)(2L * c + ((3L * d) >> (shift - 1)));
        CDDDy = (int)((3L * d) >> (shift - 1));
        CLastX = Fixed.FDot6ToFixed(x3);
        CLastY = Fixed.FDot6ToFixed(y3);
        return true;
    }

    public bool SetCubic(ReadOnlySpan<Vector2> pts)
    {
        if (!SetCubicWithoutUpdate(pts, DefaultAccuracy))
            return false;
        CX >>= DefaultAccuracy;
        CY >>= DefaultAccuracy;
        CDx >>= DefaultAccuracy;
        CDy >>= DefaultAccuracy;
        CDDx >>= DefaultAccuracy;
        CDDy >>= DefaultAccuracy;
        CDDDx >>= DefaultAccuracy;
        CDDDy >>= DefaultAccuracy;
        CLastX >>= DefaultAccuracy;
        CLastY >>= DefaultAccuracy;
        CY = SnapY(CY);
        SnappedY = CY;
        CLastY = SnapY(CLastY);
        EdgeType = Kind.Cubic;
        return UpdateCubic();
    }

    public bool UpdateCubic()
    {
        bool success;
        int count = CurveCount;
        var oldx = CX;
        var oldy = CY;
        int newx, newy;
        int ddshift = CurveShift;
        int dshift = ToFixedShift;
        do
        {
            if (++count < 0)
            {
                newx = unchecked(oldx + (CDx >> dshift));
                CDx = unchecked(CDx + (CDDx >> ddshift));
                CDDx = unchecked(CDDx + CDDDx);
                newy = unchecked(oldy + (CDy >> dshift));
                CDy = unchecked(CDy + (CDDy >> ddshift));
                CDDy = unchecked(CDDy + CDDDy);
            }
            else
            {
                newx = CLastX;
                newy = CLastY;
            }
            if (newy < oldy)
                newy = oldy;
            var newSnappedY = SnapY(newy);
            if (CLastY < newSnappedY)
            {
                newSnappedY = CLastY;
                count = 0;
            }
            var slope = Fixed.ToFDot6(newSnappedY - SnappedY) == 0
                ? Fixed.MaxS32
                : Fixed.FDot6Div(Fixed.ToFDot6(newx - oldx), Fixed.ToFDot6(newSnappedY - SnappedY));
            success = UpdateLine(oldx, SnappedY, newx, newSnappedY, slope);
            oldx = newx;
            oldy = newy;
            SnappedY = newSnappedY;
        } while (count < 0 && !success);
        CX = newx;
        CY = newy;
        CurveCount = (sbyte)count;
        return success;
    }

    public void KeepContinuousCubic()
    {
        CX = X;
        SnappedY = Y;
    }
}

/// <summary>
/// SkAnalyticEdgeBuilder for a path that lies inside its clip: lines (vertical ones combined),
/// quadratics and cubics chopped where y turns, then sorted as SkTQSort sorts them.
/// </summary>
static class AnalyticEdgeBuilder
{
    public static List<AnalyticEdge> Build(SkiaPath path)
    {
        var list = new List<AnalyticEdge>();
        Span<Vector2> mono = stackalloc Vector2[10];
        var quads = new List<Vector2>();
        foreach (var (kind, points, weight) in Edges(path))
        {
            switch (kind)
            {
                case PathVerb.Line:
                    AddLine(list, points[0], points[1]);
                    break;
                case PathVerb.Quad:
                {
                    var n = ChopQuadAtYExtrema(points, mono);
                    for (var i = 0; i <= n; i++)
                        AddQuad(list, mono.Slice(i * 2, 3));
                    break;
                }
                case PathVerb.Conic:
                {
                    // SkAutoConicToQuads with kConicTol 0.25.
                    var conic = new SkiaGeometry.Conic(points[0], points[1], points[2], weight);
                    quads.Clear();
                    var count = conic.ChopIntoQuadsPow2(quads, conic.ComputeQuadPow2(0.25f));
                    for (var q = 0; q < count; q++)
                    {
                        Vector2[] quad = [quads[q * 2], quads[q * 2 + 1], quads[q * 2 + 2]];
                        var n = ChopQuadAtYExtrema(quad, mono);
                        for (var i = 0; i <= n; i++)
                            AddQuad(list, mono.Slice(i * 2, 3));
                    }
                    break;
                }
                case PathVerb.Cubic:
                {
                    var n = ChopCubicAtYExtrema(points, mono);
                    for (var i = 0; i <= n; i++)
                        AddCubic(list, mono.Slice(i * 3, 4));
                    break;
                }
            }
        }
        return list;
    }

    /// <summary>SkPathEdgeIter: the path's segments, each contour closed by a line back to its start.</summary>
    static IEnumerable<(PathVerb Kind, Vector2[] Points, float Weight)> Edges(SkiaPath path)
    {
        var points = path.Points;
        var p = 0;
        var conic = 0;
        var moveTo = 0;
        var needsCloseLine = false;
        foreach (var verb in path.Verbs)
        {
            switch (verb)
            {
                case PathVerb.Move:
                    if (needsCloseLine)
                    {
                        needsCloseLine = false;
                        yield return (PathVerb.Line, [points[p - 1], points[moveTo]], 1);
                    }
                    moveTo = p++;
                    break;
                case PathVerb.Close:
                    if (needsCloseLine)
                    {
                        needsCloseLine = false;
                        yield return (PathVerb.Line, [points[p - 1], points[moveTo]], 1);
                    }
                    break;
                default:
                {
                    var count = verb switch { PathVerb.Line => 1, PathVerb.Quad or PathVerb.Conic => 2, _ => 3 };
                    needsCloseLine = true;
                    var segment = new Vector2[count + 1];
                    for (var i = 0; i <= count; i++)
                        segment[i] = points[p - 1 + i];
                    p += count;
                    yield return (verb, segment, verb == PathVerb.Conic ? path.ConicWeights[conic++] : 1);
                    break;
                }
            }
        }
        if (needsCloseLine)
            yield return (PathVerb.Line, [points[p - 1], points[moveTo]], 1);
    }

    enum Combine { No, Partial, Total }

    static Combine CombineVertical(AnalyticEdge edge, AnalyticEdge last)
    {
        static bool ApproximatelyEqual(int a, int b) => Fixed.Abs(a - b) < 0x100;
        if (last.EdgeType != AnalyticEdge.Kind.Line || last.DX != 0 || edge.X != last.X)
            return Combine.No;
        if (edge.Winding == last.Winding)
        {
            if (edge.LowerY == last.UpperY)
            {
                last.UpperY = edge.UpperY;
                last.Y = last.UpperY;
                return Combine.Partial;
            }
            if (ApproximatelyEqual(edge.UpperY, last.LowerY))
            {
                last.LowerY = edge.LowerY;
                return Combine.Partial;
            }
            return Combine.No;
        }
        if (ApproximatelyEqual(edge.UpperY, last.UpperY))
        {
            if (ApproximatelyEqual(edge.LowerY, last.LowerY))
                return Combine.Total;
            if (edge.LowerY < last.LowerY)
            {
                last.UpperY = edge.LowerY;
                last.Y = last.UpperY;
                return Combine.Partial;
            }
            last.UpperY = last.LowerY;
            last.Y = last.UpperY;
            last.LowerY = edge.LowerY;
            last.Winding = edge.Winding;
            return Combine.Partial;
        }
        if (ApproximatelyEqual(edge.LowerY, last.LowerY))
        {
            if (edge.UpperY > last.UpperY)
            {
                last.LowerY = edge.UpperY;
                return Combine.Partial;
            }
            last.LowerY = last.UpperY;
            last.UpperY = edge.UpperY;
            last.Y = last.UpperY;
            last.Winding = edge.Winding;
            return Combine.Partial;
        }
        return Combine.No;
    }

    static void AddLine(List<AnalyticEdge> list, Vector2 p0, Vector2 p1)
    {
        var edge = new AnalyticEdge();
        if (!edge.SetLine(p0, p1))
            return;
        var vertical = edge.DX == 0 && edge.EdgeType == AnalyticEdge.Kind.Line;
        var combine = vertical && list.Count > 0 ? CombineVertical(edge, list[^1]) : Combine.No;
        switch (combine)
        {
            case Combine.Total:
                list.RemoveAt(list.Count - 1);
                break;
            case Combine.No:
                list.Add(edge);
                break;
        }
    }

    static void AddQuad(List<AnalyticEdge> list, ReadOnlySpan<Vector2> pts)
    {
        var edge = new AnalyticEdge();
        if (edge.SetQuadratic(pts))
            list.Add(edge);
    }

    static void AddCubic(List<AnalyticEdge> list, ReadOnlySpan<Vector2> pts)
    {
        var edge = new AnalyticEdge();
        if (edge.SetCubic(pts))
            list.Add(edge);
    }

    static bool IsNotMonotonic(float a, float b, float c)
    {
        var ab = a - b;
        var bc = b - c;
        if (ab < 0)
            bc = -bc;
        return ab == 0 || bc < 0;
    }

    static bool ValidUnitDivide(float numer, float denom, out float ratio)
    {
        ratio = 0;
        if (numer < 0)
        {
            numer = -numer;
            denom = -denom;
        }
        if (denom == 0 || numer == 0 || numer >= denom)
            return false;
        var r = numer / denom;
        if (float.IsNaN(r) || r == 0)
            return false;
        ratio = r;
        return true;
    }

    static Vector2 Interp(Vector2 v0, Vector2 v1, float t) => new(v0.X + (v1.X - v0.X) * t, v0.Y + (v1.Y - v0.Y) * t);

    static int ChopQuadAtYExtrema(ReadOnlySpan<Vector2> src, Span<Vector2> dst)
    {
        float a = src[0].Y, b = src[1].Y, c = src[2].Y;
        if (IsNotMonotonic(a, b, c))
        {
            if (ValidUnitDivide(a - b, a - b - b + c, out var t))
            {
                var p01 = Interp(src[0], src[1], t);
                var p12 = Interp(src[1], src[2], t);
                dst[0] = src[0];
                dst[1] = p01;
                dst[2] = Interp(p01, p12, t);
                dst[3] = p12;
                dst[4] = src[2];
                // Flatten the double extremum.
                dst[1].Y = dst[3].Y = dst[2].Y;
                return 1;
            }
            b = MathF.Abs(a - b) < MathF.Abs(b - c) ? a : c;
        }
        dst[0] = new Vector2(src[0].X, a);
        dst[1] = new Vector2(src[1].X, b);
        dst[2] = new Vector2(src[2].X, c);
        return 0;
    }

    static int FindUnitQuadRoots(float a, float b, float c, Span<float> roots)
    {
        if (a == 0)
            return ValidUnitDivide(-c, b, out roots[0]) ? 1 : 0;
        var dr = (double)b * b - 4 * (double)a * c;
        if (dr < 0)
            return 0;
        var r = (float)Math.Sqrt(dr);
        if (!float.IsFinite(r))
            return 0;
        var q = b < 0 ? -(b - r) / 2 : -(b + r) / 2;
        var n = 0;
        if (ValidUnitDivide(q, a, out var r0))
            roots[n++] = r0;
        if (ValidUnitDivide(c, q, out var r1))
            roots[n++] = r1;
        if (n == 2)
        {
            if (roots[0] > roots[1])
                (roots[0], roots[1]) = (roots[1], roots[0]);
            else if (roots[0] == roots[1])
                n--;
        }
        return n;
    }

    static Vector2 Mix(Vector2 a, Vector2 b, float t) => new((b.X - a.X) * t + a.X, (b.Y - a.Y) * t + a.Y);

    static void ChopCubicAt(ReadOnlySpan<Vector2> src, Span<Vector2> dst, float t)
    {
        if (t == 1)
        {
            src[..4].CopyTo(dst);
            dst[4] = dst[5] = dst[6] = src[3];
            return;
        }
        var ab = Mix(src[0], src[1], t);
        var bc = Mix(src[1], src[2], t);
        var cd = Mix(src[2], src[3], t);
        var abc = Mix(ab, bc, t);
        var bcd = Mix(bc, cd, t);
        dst[0] = src[0];
        dst[1] = ab;
        dst[2] = abc;
        dst[3] = Mix(abc, bcd, t);
        dst[4] = bcd;
        dst[5] = cd;
        dst[6] = src[3];
    }

    static void ChopCubicAt(ReadOnlySpan<Vector2> src, Span<Vector2> dst, float t0, float t1)
    {
        if (t1 == 1)
        {
            ChopCubicAt(src, dst, t0);
            dst[7] = dst[8] = dst[9] = src[3];
            return;
        }
        Vector2 ab0 = Mix(src[0], src[1], t0), ab1 = Mix(src[0], src[1], t1);
        Vector2 bc0 = Mix(src[1], src[2], t0), bc1 = Mix(src[1], src[2], t1);
        Vector2 cd0 = Mix(src[2], src[3], t0), cd1 = Mix(src[2], src[3], t1);
        Vector2 abc0 = Mix(ab0, bc0, t0), abc1 = Mix(ab1, bc1, t1);
        Vector2 bcd0 = Mix(bc0, cd0, t0), bcd1 = Mix(bc1, cd1, t1);
        dst[0] = src[0];
        dst[1] = ab0;
        dst[2] = abc0;
        dst[3] = Mix(abc0, bcd0, t0);
        dst[4] = Mix(abc0, bcd0, t1);
        dst[5] = Mix(abc1, bcd1, t0);
        dst[6] = Mix(abc1, bcd1, t1);
        dst[7] = bcd1;
        dst[8] = cd1;
        dst[9] = src[3];
    }

    static int ChopCubicAtYExtrema(ReadOnlySpan<Vector2> src, Span<Vector2> dst)
    {
        Span<float> t = stackalloc float[2];
        float a = src[0].Y, b = src[1].Y, c = src[2].Y, d = src[3].Y;
        var roots = FindUnitQuadRoots(d - a + 3 * (b - c), 2 * (a - b - b + c), b - a, t);
        if (roots == 0)
            src[..4].CopyTo(dst);
        else if (roots == 2)
            ChopCubicAt(src, dst, t[0], t[1]);
        else
            ChopCubicAt(src, dst, t[0]);
        if (roots > 0)
        {
            // Flatten the y extrema (flatten_double_cubic_extrema: the y on either side of each chop point).
            dst[2].Y = dst[4].Y = dst[3].Y;
            if (roots == 2)
                dst[5].Y = dst[7].Y = dst[6].Y;
        }
        return roots;
    }

    static bool Less(AnalyticEdge a, AnalyticEdge b)
    {
        if (a.UpperY != b.UpperY)
            return a.UpperY < b.UpperY;
        if (a.X != b.X)
            return a.X < b.X;
        return a.DX < b.DX;
    }

    /// <summary>SkTQSort (introsort: insertion sort up to 32, partition around the middle, heap sort when too deep).</summary>
    public static void Sort(List<AnalyticEdge> list)
    {
        var n = list.Count;
        if (n <= 1)
            return;
        var depth = 2 * (32 - Fixed.Clz((uint)(n - 2)));
        IntroSort(list, depth, 0, n);
    }

    static void IntroSort(List<AnalyticEdge> a, int depth, int left, int count)
    {
        for (;;)
        {
            if (count <= 32)
            {
                InsertionSort(a, left, count);
                return;
            }
            if (depth == 0)
            {
                HeapSort(a, left, count);
                return;
            }
            depth--;
            var middle = left + ((count - 1) >> 1);
            var pivot = Partition(a, left, count, middle);
            var pivotCount = pivot - left;
            IntroSort(a, depth, left, pivotCount);
            left += pivotCount + 1;
            count -= pivotCount + 1;
        }
    }

    static void InsertionSort(List<AnalyticEdge> a, int left, int count)
    {
        var right = left + count - 1;
        for (var next = left + 1; next <= right; next++)
        {
            if (!Less(a[next], a[next - 1]))
                continue;
            var insert = a[next];
            var hole = next;
            do
            {
                a[hole] = a[hole - 1];
                hole--;
            } while (left < hole && Less(insert, a[hole - 1]));
            a[hole] = insert;
        }
    }

    static int Partition(List<AnalyticEdge> a, int left, int count, int pivot)
    {
        var right = left + count - 1;
        var pivotValue = a[pivot];
        (a[pivot], a[right]) = (a[right], a[pivot]);
        var newPivot = left;
        while (left < right)
        {
            if (Less(a[left], pivotValue))
            {
                (a[left], a[newPivot]) = (a[newPivot], a[left]);
                newPivot++;
            }
            left++;
        }
        (a[newPivot], a[right]) = (a[right], a[newPivot]);
        return newPivot;
    }

    static void HeapSort(List<AnalyticEdge> a, int offset, int count)
    {
        for (var i = count >> 1; i > 0; i--)
            SiftDown(a, offset, i, count);
        for (var i = count - 1; i > 0; i--)
        {
            (a[offset], a[offset + i]) = (a[offset + i], a[offset]);
            SiftUp(a, offset, 1, i);
        }
    }

    static void SiftDown(List<AnalyticEdge> a, int o, int root, int bottom)
    {
        var x = a[o + root - 1];
        var child = root << 1;
        while (child <= bottom)
        {
            if (child < bottom && Less(a[o + child - 1], a[o + child]))
                child++;
            if (Less(x, a[o + child - 1]))
            {
                a[o + root - 1] = a[o + child - 1];
                root = child;
                child = root << 1;
            }
            else
                break;
        }
        a[o + root - 1] = x;
    }

    static void SiftUp(List<AnalyticEdge> a, int o, int root, int bottom)
    {
        var x = a[o + root - 1];
        var start = root;
        var j = root << 1;
        while (j <= bottom)
        {
            if (j < bottom && Less(a[o + j - 1], a[o + j]))
                j++;
            a[o + root - 1] = a[o + j - 1];
            root = j;
            j = root << 1;
        }
        j = root >> 1;
        while (j >= start)
        {
            if (Less(a[o + j - 1], x))
            {
                a[o + root - 1] = a[o + j - 1];
                root = j;
                j = root >> 1;
            }
            else
                break;
        }
        a[o + root - 1] = x;
    }
}

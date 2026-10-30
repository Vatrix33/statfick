using System.Numerics;

namespace Statventure.Text.Skia;

/// <summary>
/// The parts of Skia's SkGeometry.cpp and SkPoint the stroker needs (Chromium 152's Skia), with
/// the same float arithmetic: quad and cubic evaluation, chopping, inflections, curvature maxima
/// and cusps, unit quadratic roots, and conics (evaluation, chopping into quads, unit arcs).
/// </summary>
static class SkiaGeometry
{
    public const float NearlyZero = 1f / (1 << 12);
    public const float Root2Over2 = 0.707106781f;

    /// <summary>SkPoint::setLength: the vector scaled (in double) to the length; false (and zero) if that fails.</summary>
    public static bool SetLength(ref Vector2 v, float x, float y, float length)
    {
        double xx = x, yy = y;
        var dmag = Math.Sqrt(xx * xx + yy * yy);
        var dscale = length / dmag;
        x = (float)(x * dscale);
        y = (float)(y * dscale);
        if (!float.IsFinite(x) || !float.IsFinite(y) || (x == 0 && y == 0))
        {
            v = Vector2.Zero;
            return false;
        }
        v = new Vector2(x, y);
        return true;
    }

    public static bool SetLength(ref Vector2 v, float length) => SetLength(ref v, v.X, v.Y, length);

    public static bool SetNormalize(ref Vector2 v, float x, float y) => SetLength(ref v, x, y, 1);

    /// <summary>SkPointPriv::CanNormalize.</summary>
    public static bool CanNormalize(float dx, float dy) => float.IsFinite(dx) && float.IsFinite(dy) && (dx != 0 || dy != 0);

    /// <summary>SkPointPriv::EqualsWithinTolerance without a tolerance: the difference can't be normalized.</summary>
    public static bool EqualsWithinTolerance(Vector2 a, Vector2 b) => !CanNormalize(a.X - b.X, a.Y - b.Y);

    public static bool EqualsWithinTolerance(Vector2 a, Vector2 b, float tolerance) =>
        MathF.Abs(a.X - b.X) <= tolerance && MathF.Abs(a.Y - b.Y) <= tolerance;

    public static Vector2 RotateCCW(Vector2 v) => new(v.Y, -v.X);
    public static Vector2 RotateCW(Vector2 v) => new(-v.Y, v.X);
    public static float Cross(Vector2 a, Vector2 b) => a.X * b.Y - a.Y * b.X;
    public static float Dot(Vector2 a, Vector2 b) => a.X * b.X + a.Y * b.Y;
    public static float DistanceToSqd(Vector2 a, Vector2 b)
    {
        var dx = a.X - b.X;
        var dy = a.Y - b.Y;
        return dx * dx + dy * dy;
    }
    public static float LengthSqd(Vector2 v) => Dot(v, v);
    public static bool IsFinite(Vector2 v) => float.IsFinite(v.X) && float.IsFinite(v.Y);

    // ---------- Roots ----------

    static int ValidUnitDivide(float numer, float denom, Span<float> ratio)
    {
        if (numer < 0)
        {
            numer = -numer;
            denom = -denom;
        }
        if (denom == 0 || numer == 0 || numer >= denom)
            return 0;
        var r = numer / denom;
        if (float.IsNaN(r) || r == 0)
            return 0;
        ratio[0] = r;
        return 1;
    }

    /// <summary>SkFindUnitQuadRoots: the roots of A t² + B t + C in (0, 1), ascending, a double root once.</summary>
    public static int FindUnitQuadRoots(float a, float b, float c, Span<float> roots)
    {
        if (a == 0)
            return ValidUnitDivide(-c, b, roots);
        var dr = (double)b * b - 4 * (double)a * c;
        if (dr < 0)
            return 0;
        var r = (float)Math.Sqrt(dr);
        if (!float.IsFinite(r))
            return 0;
        var q = b < 0 ? -(b - r) / 2 : -(b + r) / 2;
        var n = ValidUnitDivide(q, a, roots);
        n += ValidUnitDivide(c, q, roots[n..]);
        if (n == 2)
        {
            if (roots[0] > roots[1])
                (roots[0], roots[1]) = (roots[1], roots[0]);
            else if (roots[0] == roots[1])
                n = 1;
        }
        return n;
    }

    // ---------- Quads ----------

    /// <summary>SkEvalQuadAt (SkQuadCoeff: (A t + B) t + C).</summary>
    public static Vector2 EvalQuadAt(ReadOnlySpan<Vector2> q, float t)
    {
        var c = q[0];
        var b = 2 * (q[1] - c);
        var a = q[2] - 2 * q[1] + c;
        return (a * t + b) * t + c;
    }

    /// <summary>SkEvalQuadTangentAt.</summary>
    public static Vector2 EvalQuadTangentAt(ReadOnlySpan<Vector2> q, float t)
    {
        if ((t == 0 && q[0] == q[1]) || (t == 1 && q[1] == q[2]))
            return q[2] - q[0];
        var b = q[1] - q[0];
        var a = q[2] - q[1] - b;
        var tt = a * t + b;
        return tt + tt;
    }

    /// <summary>SkFindQuadMaxCurvature.</summary>
    public static float FindQuadMaxCurvature(ReadOnlySpan<Vector2> q)
    {
        var ax = q[1].X - q[0].X;
        var ay = q[1].Y - q[0].Y;
        var bx = q[0].X - q[1].X - q[1].X + q[2].X;
        var by = q[0].Y - q[1].Y - q[1].Y + q[2].Y;
        var numer = -(ax * bx + ay * by);
        var denom = bx * bx + by * by;
        if (denom < 0)
        {
            numer = -numer;
            denom = -denom;
        }
        if (numer <= 0)
            return 0;
        if (numer >= denom)
            return 1;
        return numer / denom;
    }

    // ---------- Cubics ----------

    /// <summary>SkEvalCubicAt's point (SkCubicCoeff: ((A t + B) t + C) t + D).</summary>
    public static Vector2 EvalCubicAt(ReadOnlySpan<Vector2> c, float t)
    {
        var a = c[3] + 3 * (c[1] - c[2]) - c[0];
        var b = 3 * (c[2] - 2 * c[1] + c[0]);
        var cc = 3 * (c[1] - c[0]);
        return ((a * t + b) * t + cc) * t + c[0];
    }

    /// <summary>eval_cubic_derivative (a third of the derivative).</summary>
    public static Vector2 EvalCubicDerivative(ReadOnlySpan<Vector2> c, float t)
    {
        var a = c[3] + 3 * (c[1] - c[2]) - c[0];
        var b = 2 * (c[2] - 2 * c[1] + c[0]);
        var cc = c[1] - c[0];
        return (a * t + b) * t + cc;
    }

    /// <summary>SkEvalCubicAt's tangent.</summary>
    public static Vector2 EvalCubicTangentAt(ReadOnlySpan<Vector2> c, float t)
    {
        if ((t == 0 && c[0] == c[1]) || (t == 1 && c[2] == c[3]))
        {
            var tangent = t == 0 ? c[2] - c[0] : c[3] - c[1];
            if (tangent.X == 0 && tangent.Y == 0)
                tangent = c[3] - c[0];
            return tangent;
        }
        return EvalCubicDerivative(c, t);
    }

    /// <summary>SkChopCubicAt at one t (unchecked_mix: (b − a) t + a).</summary>
    public static void ChopCubicAt(ReadOnlySpan<Vector2> src, Span<Vector2> dst, float t)
    {
        if (t == 1)
        {
            src[..4].CopyTo(dst);
            dst[4] = dst[5] = dst[6] = src[3];
            return;
        }
        static Vector2 Mix(Vector2 a, Vector2 b, float t) => (b - a) * t + a;
        var ab = Mix(src[0], src[1], t);
        var bc = Mix(src[1], src[2], t);
        var cd = Mix(src[2], src[3], t);
        var abc = Mix(ab, bc, t);
        var bcd = Mix(bc, cd, t);
        var abcd = Mix(abc, bcd, t);
        dst[0] = src[0];
        dst[1] = ab;
        dst[2] = abc;
        dst[3] = abcd;
        dst[4] = bcd;
        dst[5] = cd;
        dst[6] = src[3];
    }

    /// <summary>SkFindCubicInflections.</summary>
    public static int FindCubicInflections(ReadOnlySpan<Vector2> s, Span<float> t)
    {
        var ax = s[1].X - s[0].X;
        var ay = s[1].Y - s[0].Y;
        var bx = s[2].X - 2 * s[1].X + s[0].X;
        var by = s[2].Y - 2 * s[1].Y + s[0].Y;
        var cx = s[3].X + 3 * (s[1].X - s[2].X) - s[0].X;
        var cy = s[3].Y + 3 * (s[1].Y - s[2].Y) - s[0].Y;
        return FindUnitQuadRoots(bx * cy - by * cx, ax * cy - ay * cx, ax * by - ay * bx, t);
    }

    static void FormulateF1DotF2(float p0, float p1, float p2, float p3, Span<float> coeff)
    {
        var a = p1 - p0;
        var b = p2 - 2 * p1 + p0;
        var c = p3 + 3 * (p1 - p2) - p0;
        coeff[0] = c * c;
        coeff[1] = 3 * b * c;
        coeff[2] = 2 * b * b + c * a;
        coeff[3] = a * b;
    }

    static int SolveCubicPoly(ReadOnlySpan<float> coeff, Span<float> t)
    {
        if (MathF.Abs(coeff[0]) <= NearlyZero)
            return FindUnitQuadRoots(coeff[1], coeff[2], coeff[3], t);
        var inva = 1 / coeff[0];
        var a = coeff[1] * inva;
        var b = coeff[2] * inva;
        var c = coeff[3] * inva;
        var q = (a * a - b * 3) / 9;
        var r = (2 * a * a * a - 9 * a * b + 27 * c) / 54;
        var q3 = q * q * q;
        var r2MinusQ3 = r * r - q3;
        var adiv3 = a / 3;
        if (r2MinusQ3 < 0)
        {
            var theta = MathF.Acos(Math.Clamp(r / MathF.Sqrt(q3), -1f, 1f));
            var neg2RootQ = -2 * MathF.Sqrt(q);
            t[0] = Math.Clamp(neg2RootQ * MathF.Cos(theta / 3) - adiv3, 0f, 1f);
            t[1] = Math.Clamp(neg2RootQ * MathF.Cos((theta + 2 * MathF.PI) / 3) - adiv3, 0f, 1f);
            t[2] = Math.Clamp(neg2RootQ * MathF.Cos((theta - 2 * MathF.PI) / 3) - adiv3, 0f, 1f);
            // bubble_sort, then collaps_duplicates.
            for (var i = 2; i > 0; --i)
                for (var j = i; j > 0; --j)
                    if (t[j] < t[j - 1])
                        (t[j], t[j - 1]) = (t[j - 1], t[j]);
            var count = 3;
            var offset = 0;
            for (var n = 3; n > 1; --n)
            {
                if (t[offset] == t[offset + 1])
                {
                    for (var i = 1; i < n; ++i)
                        t[offset + i - 1] = t[offset + i];
                    count--;
                }
                else
                    offset++;
            }
            return count;
        }
        var aa = MathF.Abs(r) + MathF.Sqrt(r2MinusQ3);
        aa = MathF.Pow(aa, 0.3333333f);
        if (r > 0)
            aa = -aa;
        if (aa != 0)
            aa += q / aa;
        t[0] = Math.Clamp(aa - adiv3, 0f, 1f);
        return 1;
    }

    /// <summary>SkFindCubicMaxCurvature: where F'·F'' = 0.</summary>
    public static int FindCubicMaxCurvature(ReadOnlySpan<Vector2> s, Span<float> t)
    {
        Span<float> cx = stackalloc float[4];
        Span<float> cy = stackalloc float[4];
        FormulateF1DotF2(s[0].X, s[1].X, s[2].X, s[3].X, cx);
        FormulateF1DotF2(s[0].Y, s[1].Y, s[2].Y, s[3].Y, cy);
        for (var i = 0; i < 4; i++)
            cx[i] += cy[i];
        return SolveCubicPoly(cx, t);
    }

    /// <summary>SkFindCubicCusp: the t of a cusp, or −1.</summary>
    public static float FindCubicCusp(ReadOnlySpan<Vector2> s)
    {
        if (s[0] == s[1] || s[2] == s[3])
            return -1;
        static bool OnSameSide(ReadOnlySpan<Vector2> s, int test, int line)
        {
            var origin = s[line];
            var l = s[line + 1] - origin;
            var c0 = Cross(l, s[test] - origin);
            var c1 = Cross(l, s[test + 1] - origin);
            return c0 * c1 >= 0;
        }
        if (OnSameSide(s, 0, 2) || OnSameSide(s, 2, 0))
            return -1;
        Span<float> max = stackalloc float[3];
        var roots = FindCubicMaxCurvature(s, max);
        for (var i = 0; i < roots; i++)
        {
            var t = max[i];
            if (0 >= t || t >= 1)
                continue;
            var d = EvalCubicDerivative(s, t);
            var precision = (DistanceToSqd(s[1], s[0]) + DistanceToSqd(s[2], s[1]) + DistanceToSqd(s[3], s[2])) * 1e-8f;
            if (LengthSqd(d) < precision)
                return t;
        }
        return -1;
    }

    // ---------- Conics ----------

    /// <summary>SkConic: three points and a weight.</summary>
    public struct Conic(Vector2 p0, Vector2 p1, Vector2 p2, float w)
    {
        public Vector2 P0 = p0, P1 = p1, P2 = p2;
        public float W = w;

        /// <summary>SkConic::evalAt (SkConicCoeff: numerator over denominator).</summary>
        public readonly Vector2 EvalAt(float t)
        {
            var p1w = P1 * W;
            var nc = P0;
            var na = P2 - 2 * p1w + P0;
            var nb = 2 * (p1w - P0);
            const float dc = 1;
            var db = 2 * (W - dc);
            var da = 0 - db;
            var numer = (na * t + nb) * t + nc;
            var denom = (da * t + db) * t + dc;
            return numer / denom;
        }

        /// <summary>SkConic::evalTangentAt.</summary>
        public readonly Vector2 EvalTangentAt(float t)
        {
            if ((t == 0 && P0 == P1) || (t == 1 && P1 == P2))
                return P2 - P0;
            var p20 = P2 - P0;
            var p10 = P1 - P0;
            var c = W * p10;
            var a = W * p20 - p20;
            var b = p20 - c - c;
            return (a * t + b) * t + c;
        }

        /// <summary>SkConic::chop (the non-legacy version).</summary>
        public readonly (Conic, Conic) Chop()
        {
            var scale = 1 / (1 + W);
            var t0 = P0 * scale;
            var t1 = P1 * (W * scale);
            var t2 = P2 * scale;
            var p1 = t0 + t1;
            var p3 = t1 + t2;
            var p2 = 0.5f * t0 + t1 + 0.5f * t2;
            var w = MathF.Sqrt(0.5f + W * 0.5f);
            return (new Conic(P0, p1, p2, w), new Conic(p2, p3, P2, w));
        }

        /// <summary>SkConic::computeQuadPOW2.</summary>
        public readonly int ComputeQuadPow2(float tolerance)
        {
            if (tolerance < 0 || !float.IsFinite(tolerance) || !IsFinite(P0) || !IsFinite(P1) || !IsFinite(P2) || W < 0 || !float.IsFinite(W))
                return 0;
            var a = W - 1;
            var k = a / (4 * (2 + a));
            var x = k * (P0.X - 2 * P1.X + P2.X);
            var y = k * (P0.Y - 2 * P1.Y + P2.Y);
            var error = MathF.Sqrt(x * x + y * y);
            int pow2;
            for (pow2 = 0; pow2 < 5; ++pow2)
            {
                if (error <= tolerance)
                    break;
                error *= 0.25f;
            }
            return pow2;
        }

        /// <summary>SkConic::chopIntoQuadsPOW2: 2·2^pow2 + 1 points (shared ends).</summary>
        public readonly int ChopIntoQuadsPow2(List<Vector2> points, int pow2)
        {
            var start = points.Count;
            if (W < 0 || !float.IsFinite(W))
                pow2 = 0;
            points.Add(P0);
            var done = false;
            if (pow2 == 5)
            {
                var (d0, d1) = Chop();
                if (EqualsWithinTolerance(d0.P1, d0.P2) && EqualsWithinTolerance(d1.P0, d1.P1))
                {
                    points.Add(d0.P1);
                    points.Add(d0.P1);
                    points.Add(d0.P1);
                    points.Add(d1.P2);
                    pow2 = 1;
                    done = true;
                }
            }
            if (!done)
                Subdivide(this, points, pow2);
            var count = 2 * (1 << pow2) + 1;
            var finite = true;
            for (var i = start; i < start + count; i++)
                finite &= IsFinite(points[i]);
            if (!finite)
                for (var i = start + 1; i < start + count - 1; i++)
                    points[i] = P1;
            return 1 << pow2;
        }

        static bool Between(float a, float b, float c) => (a - b) * (c - b) <= 0;

        static void Subdivide(Conic src, List<Vector2> points, int level)
        {
            if (level == 0)
            {
                points.Add(src.P1);
                points.Add(src.P2);
                return;
            }
            var (d0, d1) = src.Chop();
            var startY = src.P0.Y;
            var endY = src.P2.Y;
            if (Between(startY, src.P1.Y, endY))
            {
                var midY = d0.P2.Y;
                if (!Between(startY, midY, endY))
                {
                    var closerY = MathF.Abs(midY - startY) < MathF.Abs(midY - endY) ? startY : endY;
                    d0.P2.Y = d1.P0.Y = closerY;
                }
                if (!Between(startY, d0.P1.Y, d0.P2.Y))
                    d0.P1.Y = startY;
                if (!Between(d1.P0.Y, d1.P1.Y, endY))
                    d1.P1.Y = endY;
            }
            --level;
            Subdivide(d0, points, level);
            Subdivide(d1, points, level);
        }
    }

    /// <summary>
    /// SkConic::BuildUnitArc: the conics of the arc from the unit vector <paramref name="start"/> to
    /// <paramref name="stop"/>, scaled by <paramref name="radius"/> about <paramref name="center"/>
    /// (the round joiner's user matrix).
    /// </summary>
    public static int BuildUnitArc(Vector2 start, Vector2 stop, bool clockwise, float radius, Vector2 center, Span<Conic> dst)
    {
        var x = Dot(start, stop);
        var y = Cross(start, stop);
        var absY = MathF.Abs(y);
        if (absY <= NearlyZero && x > 0 && ((y >= 0 && clockwise) || (y <= 0 && !clockwise)))
            return 0;
        if (!clockwise)
            y = -y;
        var quadrant = 0;
        if (y == 0)
            quadrant = 2;
        else if (x == 0)
            quadrant = y > 0 ? 1 : 3;
        else
        {
            if (y < 0)
                quadrant += 2;
            if ((x < 0) != (y < 0))
                quadrant += 1;
        }
        ReadOnlySpan<Vector2> quadrantPts = [new(1, 0), new(1, 1), new(0, 1), new(-1, 1), new(-1, 0), new(-1, -1), new(0, -1), new(1, -1)];
        var count = quadrant;
        for (var i = 0; i < count; i++)
            dst[i] = new Conic(quadrantPts[i * 2], quadrantPts[i * 2 + 1], quadrantPts[(i * 2 + 2) % 8], Root2Over2);
        var finalP = new Vector2(x, y);
        var lastQ = quadrantPts[quadrant * 2];
        var dot = Dot(lastQ, finalP);
        if (float.IsNaN(dot))
            return 0;
        if (dot < 1)
        {
            var offCurve = new Vector2(lastQ.X + x, lastQ.Y + y);
            var cosThetaOver2 = MathF.Sqrt((1 + dot) / 2);
            SetLength(ref offCurve, 1 / cosThetaOver2);
            if (!EqualsWithinTolerance(lastQ, offCurve))
                dst[count++] = new Conic(lastQ, offCurve, finalP, cosThetaOver2);
        }
        // SkMatrix::setSinCos(start.y, start.x), preScale(1, −1) counter-clockwise, then the user
        // matrix (scale by the radius, translate to the centre).
        float sin = start.Y, cos = start.X;
        float m00 = cos, m01 = -sin, m10 = sin, m11 = cos;
        if (!clockwise)
        {
            m01 = -m01;
            m11 = -m11;
        }
        m00 *= radius;
        m01 *= radius;
        m10 *= radius;
        m11 *= radius;
        Vector2 Map(Vector2 p) => new(p.X * m00 + (p.Y * m01 + center.X), p.Y * m11 + (p.X * m10 + center.Y));
        for (var i = 0; i < count; i++)
            dst[i] = new Conic(Map(dst[i].P0), Map(dst[i].P1), Map(dst[i].P2), dst[i].W);
        return count;
    }
}

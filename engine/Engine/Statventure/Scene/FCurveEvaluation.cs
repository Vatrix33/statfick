namespace Statventure.Scene;

// Blender's F-curve evaluation (blenkernel/intern/fcurve.cc, 5.2.0: fcurve_eval_keyframes,
// fcurve_eval_keyframes_extrapolate and _interpolate, BKE_fcurve_bezt_binarysearch_index_ex,
// BKE_fcurve_correct_bezpart, findzero, berekeny), in double precision and in frames. Left out: the
// property flags for discrete and integer values (a curve here is a number) and the modifiers (Q7).
// Unreachable here, so not ported: the branch Blender takes when its search fails (a time outside the
// segment: the keys are in order); its second on-key test (within 1e-8 frame of the key after: the
// search has compared that key with its 0.0001-frame threshold whenever it returns it); its first
// comparisons with the end keys (the search compares every key next to the frame anyway); and the
// guards for segments and lines of no length (keys are at least 0.01 frame apart).

sealed partial class FCurveResolved
{
    /// <summary>Times within this of a key (frames) give its value (fcurve_eval_keyframes_interpolate's search threshold).</summary>
    const double SearchThreshold = 0.0001;
    /// <summary>FLT_EPSILON: Blender's test for a flat Bezier segment.</summary>
    const double FlatEpsilon = 1.1920928955078125e-7;

    /// <summary>fcurve_eval_keyframes at <paramref name="frame"/>.</summary>
    public double Evaluate(double frame, CurveExtrapolation extrapolation)
    {
        var n = _x.Length;
        if (frame <= _x[0])
            return Extrapolate(frame, 0, +1, extrapolation);
        if (_x[n - 1] <= frame)
            return Extrapolate(frame, n - 1, -1, extrapolation);
        return Interpolate(frame);
    }

    /// <summary>
    /// fcurve_eval_keyframes_extrapolate: the end key's value when it is constant or the curve's
    /// extrapolation is; the line to its neighbour when it is linear; else its outer handle's slope.
    /// </summary>
    double Extrapolate(double frame, int end, int toNeighbour, CurveExtrapolation extrapolation)
    {
        var interpolation = _interpolation[end];
        double x = _x[end], y = _y[end];
        if (interpolation == KeyInterpolation.Constant || extrapolation == CurveExtrapolation.Constant)
            return y;
        var dx = x - frame;
        if (interpolation == KeyInterpolation.Linear)
        {
            if (_x.Length == 1)
                return y;
            var neighbour = end + toNeighbour;
            return y - (_y[neighbour] - y) / (_x[neighbour] - x) * dx;
        }
        // The first key's left handle, the last key's right handle.
        double hx = toNeighbour > 0 ? _lx[end] : _rx[end], hy = toNeighbour > 0 ? _ly[end] : _ry[end];
        var fac = x - hx;
        return fac == 0 ? y : y - (y - hy) / fac * dx;
    }

    static bool Near(double a, double b, double threshold) => a > b ? a - b <= threshold : b - a <= threshold;

    /// <summary>
    /// BKE_fcurve_bezt_binarysearch_index_ex for a frame between the end keys: the key within the
    /// threshold of it (exact), else the first key after it. A key next to the frame is always one the
    /// search compares, so which one is within the threshold decides, as in Blender.
    /// </summary>
    int Search(double frame, out bool exact)
    {
        exact = false;
        int start = 0, end = _x.Length;
        while (start <= end)
        {
            var middle = start + (end - start) / 2;
            if (Near(frame, _x[middle], SearchThreshold))
            {
                exact = true;
                return middle;
            }
            if (frame > _x[middle])
                start = middle + 1;
            else
                end = middle - 1;
        }
        return start;
    }

    /// <summary>fcurve_eval_keyframes_interpolate: a frame strictly between the first and last keys.</summary>
    double Interpolate(double frame)
    {
        var a = Search(frame, out var exact);
        if (exact)
            return _y[a];
        // The segment from the key before.
        var p = a - 1;
        double begin = _y[p], change = _y[a] - _y[p], duration = _x[a] - _x[p], time = frame - _x[p];
        var interpolation = _interpolation[p];
        if (interpolation == KeyInterpolation.Constant)
            return begin;
        var easing = _easing[p];
        return interpolation switch
        {
            KeyInterpolation.Bezier => Bezier(p, a, frame),
            KeyInterpolation.Linear => FCurveEasing.Linear(time, begin, change, duration),
            KeyInterpolation.Back => easing switch
            {
                KeyEasing.In => FCurveEasing.BackIn(time, begin, change, duration, _back[p]),
                KeyEasing.InOut => FCurveEasing.BackInOut(time, begin, change, duration, _back[p]),
                _ => FCurveEasing.BackOut(time, begin, change, duration, _back[p]),
            },
            KeyInterpolation.Bounce => easing switch
            {
                KeyEasing.In => FCurveEasing.BounceIn(time, begin, change, duration),
                KeyEasing.InOut => FCurveEasing.BounceInOut(time, begin, change, duration),
                _ => FCurveEasing.BounceOut(time, begin, change, duration),
            },
            KeyInterpolation.Elastic => easing switch
            {
                KeyEasing.In => FCurveEasing.ElasticIn(time, begin, change, duration, _amplitude[p], _period[p]),
                KeyEasing.InOut => FCurveEasing.ElasticInOut(time, begin, change, duration, _amplitude[p], _period[p]),
                _ => FCurveEasing.ElasticOut(time, begin, change, duration, _amplitude[p], _period[p]),
            },
            // The others' automatic easing is ease in.
            KeyInterpolation.Circ => easing switch
            {
                KeyEasing.Out => FCurveEasing.CircOut(time, begin, change, duration),
                KeyEasing.InOut => FCurveEasing.CircInOut(time, begin, change, duration),
                _ => FCurveEasing.CircIn(time, begin, change, duration),
            },
            KeyInterpolation.Cubic => easing switch
            {
                KeyEasing.Out => FCurveEasing.CubicOut(time, begin, change, duration),
                KeyEasing.InOut => FCurveEasing.CubicInOut(time, begin, change, duration),
                _ => FCurveEasing.CubicIn(time, begin, change, duration),
            },
            KeyInterpolation.Expo => easing switch
            {
                KeyEasing.Out => FCurveEasing.ExpoOut(time, begin, change, duration),
                KeyEasing.InOut => FCurveEasing.ExpoInOut(time, begin, change, duration),
                _ => FCurveEasing.ExpoIn(time, begin, change, duration),
            },
            KeyInterpolation.Quad => easing switch
            {
                KeyEasing.Out => FCurveEasing.QuadOut(time, begin, change, duration),
                KeyEasing.InOut => FCurveEasing.QuadInOut(time, begin, change, duration),
                _ => FCurveEasing.QuadIn(time, begin, change, duration),
            },
            KeyInterpolation.Quart => easing switch
            {
                KeyEasing.Out => FCurveEasing.QuartOut(time, begin, change, duration),
                KeyEasing.InOut => FCurveEasing.QuartInOut(time, begin, change, duration),
                _ => FCurveEasing.QuartIn(time, begin, change, duration),
            },
            KeyInterpolation.Quint => easing switch
            {
                KeyEasing.Out => FCurveEasing.QuintOut(time, begin, change, duration),
                KeyEasing.InOut => FCurveEasing.QuintInOut(time, begin, change, duration),
                _ => FCurveEasing.QuintIn(time, begin, change, duration),
            },
            _ => easing switch
            {
                KeyEasing.Out => FCurveEasing.SineOut(time, begin, change, duration),
                KeyEasing.InOut => FCurveEasing.SineInOut(time, begin, change, duration),
                _ => FCurveEasing.SineIn(time, begin, change, duration),
            },
        };
    }

    /// <summary>
    /// A Bezier segment from key <paramref name="p"/> (and its right handle) to key <paramref name="a"/>
    /// (and its left handle): flat when all four values are within FLT_EPSILON; else the handles
    /// shortened to the segment (correct_bezpart), the curve's parameter found for the frame
    /// (findzero) and its value taken there (berekeny).
    /// </summary>
    double Bezier(int p, int a, double frame)
    {
        double v1x = _x[p], v1y = _y[p], v2x = _rx[p], v2y = _ry[p], v3x = _lx[a], v3y = _ly[a], v4x = _x[a], v4y = _y[a];
        if (Math.Abs(v1y - v4y) < FlatEpsilon && Math.Abs(v2y - v3y) < FlatEpsilon && Math.Abs(v3y - v4y) < FlatEpsilon)
            return v1y;
        CorrectSegment(v1x, v1y, ref v2x, ref v2y, ref v3x, ref v3y, v4x, v4y);
        var t = ParameterAt(frame, v1x, v2x, v3x, v4x);
        double c0 = v1y, c1 = 3 * (v2y - v1y), c2 = 3 * (v1y - 2 * v2y + v3y), c3 = v4y - v1y + 3 * (v2y - v3y);
        return c0 + t * c1 + t * t * c2 + t * t * t * c3;
    }

    /// <summary>BKE_fcurve_correct_bezpart: handles longer in time than the segment are scaled back to it, so the curve can't loop.</summary>
    static void CorrectSegment(double v1x, double v1y, ref double v2x, ref double v2y, ref double v3x, ref double v3y, double v4x, double v4y)
    {
        double h1x = v1x - v2x, h1y = v1y - v2y, h2x = v4x - v3x, h2y = v4y - v3y;
        double length = v4x - v1x, length1 = Math.Abs(h1x), length2 = Math.Abs(h2x);
        if (length1 > length)
        {
            var fac = length / length1;
            (v2x, v2y) = (v1x - fac * h1x, v1y - fac * h1y);
        }
        if (length2 > length)
        {
            var fac = length / length2;
            (v3x, v3y) = (v4x - fac * h2x, v4y - fac * h2y);
        }
    }

    /// <summary>
    /// findzero: the Bezier's parameter t in [0, 1] where its time x(t) is <paramref name="frame"/>
    /// (strictly between the segment's keys). The corrected handles lie within the segment in time, so
    /// x(t) never decreases (x'(t) = 3[(1−t)²(q1−q0) + 2t(1−t)(q2−q1) + t²(q3−q2)] ≥ 0 whenever q1 and
    /// q2 lie between q0 and q3) and the root is the only one in the interval: the one Blender's
    /// solve_cubic finds. Blender takes it from the cubic formula in double from coefficients rounded to
    /// float; from double coefficients that formula can lose it (where the time runs linearly, as the
    /// continuous-acceleration handles make it, c2 and c3 are rounding residues, and with c3 exactly 0
    /// the quadratic branch's cancellation errs by up to half the segment), so the engine finds the same
    /// root by Newton's method kept inside a shrinking bracket, to the last bit.
    /// </summary>
    static double ParameterAt(double frame, double q0, double q1, double q2, double q3)
    {
        double c0 = q0 - frame, c1 = 3 * (q1 - q0), c2 = 3 * (q0 - 2 * q1 + q2), c3 = q3 - q0 + 3 * (q1 - q2);
        double low = 0, high = 1, t = (frame - q0) / (q3 - q0);
        for (var i = 0; i < 100; i++)
        {
            var x = ((c3 * t + c2) * t + c1) * t + c0;
            if (x == 0)
                return t;
            if (x < 0)
                low = t;
            else
                high = t;
            var slope = (3 * c3 * t + 2 * c2) * t + c1;
            var next = slope > 0 ? t - x / slope : double.NaN;
            if (!(next > low && next < high))
                next = low + (high - low) / 2;
            if (next == t || next <= low || next >= high)
                return next;
            t = next;
        }
        return t;
    }
}

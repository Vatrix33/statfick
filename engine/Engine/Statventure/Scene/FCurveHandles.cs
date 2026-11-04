namespace Statventure.Scene;

// Blender's handles as its update after an edit leaves them (rna_FCurve_update_data_ex: sort_time_fcurve,
// BKE_fcurve_deduplicate_keys, BKE_fcurve_handles_recalc), ported from Blender 5.2.0's
// blenkernel/intern/fcurve.cc and curve.cc (calchandleNurb_intern, BKE_nurb_handle_smooth_fcurve) and
// blenlib/intern/math_solvers.cc (BLI_tridiagonal_solve), in double precision and in frames: the
// thresholds are frames and aligned handles keep their length in the (frame, value) plane. Sorting and
// merging have nothing to do here: a curve's keys are in order and apart (FCurve's rules, S2-3). No
// cyclic paths: there are no modifiers (Q7), so no Cycles modifier. Keys are never selected here, so
// where Blender's order depends on the left handle's selection it takes the unselected one. Blender's
// guards for lengths of zero (a neighbour at the key's time, an auto handle's length factor, handles
// shorter than 1e-5 to align by) can't be met here: keys are at least 0.01 frame apart and every handle
// is at least 0.001 frame off its key's time, so they aren't ported.

/// <summary>A curve's keys with their handles resolved, in frames, ready to evaluate.</summary>
sealed partial class FCurveResolved
{
    /// <summary>How far (frames) recalculation keeps every handle off its key's time (BKE_fcurve_handles_recalc_ex).</summary>
    const double HandleClearance = 0.001;
    /// <summary>Blender's auto handle length factor (calchandleNurb_intern).</summary>
    const double AutoHandleFactor = 2.5614;

    /// <summary>One BezTriple: the key and its handles in frames, the handle types, and whether its handles are final (HD_AUTOTYPE_LOCKED_FINAL).</summary>
    sealed class Knot
    {
        public double X, Y, LX, LY, RX, RY;
        public HandleType H1, H2;
        public bool Locked;
    }

    readonly double[] _x, _y, _lx, _ly, _rx, _ry, _back, _amplitude, _period;
    readonly KeyInterpolation[] _interpolation;
    readonly KeyEasing[] _easing;

    public IReadOnlyList<KeyHandles> Handles { get; }

    public FCurveResolved(FCurveKey[] keys, CurveExtrapolation extrapolation, CurveSmoothing smoothing)
    {
        const double fps = FCurve.FramesPerSecond;
        var n = keys.Length;
        var knots = new Knot[n];
        for (var i = 0; i < n; i++)
        {
            var key = keys[i];
            double x = key.Time * fps, y = key.Value;
            // A side whose position isn't stored starts where Blender's key insertion puts it (a frame to
            // either side, at the key's value): what a curve of one key keeps, as Blender recalculates
            // nothing for fewer than two keys; with more it is worked out again.
            knots[i] = new Knot
            {
                X = x, Y = y, H1 = key.Left, H2 = key.Right,
                LX = key.LeftTime is { } lt ? lt * fps : x - 1, LY = key.LeftValue ?? y,
                RX = key.RightTime is { } rt ? rt * fps : x + 1, RY = key.RightValue ?? y,
            };
        }
        // sort_time_fcurve: handles that both crossed to the wrong side are swapped, else each is
        // clamped to its side of the key.
        foreach (var k in knots)
        {
            if (k.LX > k.X && k.RX < k.X)
            {
                (k.LX, k.RX) = (k.RX, k.LX);
                (k.LY, k.RY) = (k.RY, k.LY);
            }
            else
            {
                k.LX = Math.Min(k.LX, k.X);
                k.RX = Math.Max(k.RX, k.X);
            }
        }
        if (n >= 2)
            Recalculate(knots, extrapolation, smoothing);

        _x = new double[n];
        _y = new double[n];
        _lx = new double[n];
        _ly = new double[n];
        _rx = new double[n];
        _ry = new double[n];
        _back = new double[n];
        _amplitude = new double[n];
        _period = new double[n];
        _interpolation = new KeyInterpolation[n];
        _easing = new KeyEasing[n];
        var handles = new KeyHandles[n];
        for (var i = 0; i < n; i++)
        {
            var k = knots[i];
            (_x[i], _y[i], _lx[i], _ly[i], _rx[i], _ry[i]) = (k.X, k.Y, k.LX, k.LY, k.RX, k.RY);
            _interpolation[i] = keys[i].Interpolation;
            _easing[i] = keys[i].Easing;
            _back[i] = keys[i].Back;
            _amplitude[i] = keys[i].Amplitude;
            _period[i] = keys[i].Period * fps;
            handles[i] = new KeyHandles(k.LX / fps, k.LY, k.RX / fps, k.RY);
        }
        Handles = handles;
    }

    static bool IsAuto(HandleType type) => type is HandleType.Auto or HandleType.AutoClamped;

    /// <summary>BEZT_IS_AUTOH: both sides auto or auto-clamped.</summary>
    static bool IsAuto(Knot k) => IsAuto(k.H1) && IsAuto(k.H2);

    /// <summary>
    /// BKE_fcurve_handles_recalc_ex: each key's handles kept 0.001 frame off its time, its handles worked
    /// out (calchandleNurb_intern), the end keys of a constant-extrapolated curve made flat when both
    /// their sides are auto; then, with smoothing, the runs of auto keys solved together.
    /// </summary>
    static void Recalculate(Knot[] knots, CurveExtrapolation extrapolation, CurveSmoothing smoothing)
    {
        var n = knots.Length;
        for (var i = 0; i < n; i++)
        {
            var k = knots[i];
            k.LX = Math.Min(k.LX, Math.BitDecrement(k.X - HandleClearance));
            k.RX = Math.Max(k.RX, Math.BitIncrement(k.X + HandleClearance));
            CalculateHandles(k, i > 0 ? knots[i - 1] : null, i < n - 1 ? knots[i + 1] : null, smoothing);
            // "For automatic ease in and out": a held end stays level.
            if (IsAuto(k) && (i == 0 || i == n - 1) && extrapolation == CurveExtrapolation.Constant)
            {
                k.LY = k.RY = k.Y;
                k.Locked = true;
            }
            // (Blender also locks both keys of a pair whose times aren't increasing; a curve's keys always are.)
        }
        if (smoothing != CurveSmoothing.None)
            Smooth(knots);
    }

    /// <summary>calchandleNurb_intern for an F-curve (is_fcurve), the left handle never selected.</summary>
    static void CalculateHandles(Knot b, Knot? prev, Knot? next, CurveSmoothing smoothing)
    {
        b.Locked = false;
        if (b.H1 == HandleType.Free && b.H2 == HandleType.Free)
            return;
        // A missing neighbour is the other one mirrored through the key.
        double p1x, p1y, p3x, p3y;
        if (prev == null)
        {
            (p3x, p3y) = (next!.X, next.Y);
            (p1x, p1y) = (2 * b.X - p3x, 2 * b.Y - p3y);
        }
        else
            (p1x, p1y) = (prev.X, prev.Y);
        if (next == null)
            (p3x, p3y) = (2 * b.X - p1x, 2 * b.Y - p1y);
        else
            (p3x, p3y) = (next.X, next.Y);
        double ax = b.X - p1x, ay = b.Y - p1y, bx = p3x - b.X, by = p3y - b.Y;
        // An F-curve measures the neighbours' distances in time only.
        double lenA = ax, lenB = bx;

        if (IsAuto(b.H1) || IsAuto(b.H2))
        {
            double tx = bx / lenB + ax / lenA, ty = by / lenB + ay / lenA;
            // With smoothing the handles span a third of each interval (the time a linear spline).
            var len = (smoothing != CurveSmoothing.None ? 6.0 / AutoHandleFactor : tx) * AutoHandleFactor;
            bool leftViolate = false, rightViolate = false;
            if (smoothing == CurveSmoothing.None)
            {
                lenA = Math.Min(lenA, 5 * lenB);
                lenB = Math.Min(lenB, 5 * lenA);
            }
            if (IsAuto(b.H1))
            {
                lenA /= len;
                b.LX = b.X - tx * lenA;
                b.LY = b.Y - ty * lenA;
                if (b.H1 == HandleType.AutoClamped && next != null && prev != null)
                {
                    double ydiff1 = prev.Y - b.Y, ydiff2 = next.Y - b.Y;
                    if (ydiff1 <= 0 && ydiff2 <= 0 || ydiff1 >= 0 && ydiff2 >= 0)
                    {
                        // A peak or a dip: kept level.
                        b.LY = b.Y;
                        b.Locked = true;
                    }
                    else if (ydiff1 <= 0 ? prev.Y > b.LY : prev.Y < b.LY)
                    {
                        // Not beyond the neighbour's value.
                        b.LY = prev.Y;
                        leftViolate = true;
                    }
                }
            }
            if (IsAuto(b.H2))
            {
                lenB /= len;
                b.RX = b.X + tx * lenB;
                b.RY = b.Y + ty * lenB;
                if (b.H2 == HandleType.AutoClamped && next != null && prev != null)
                {
                    double ydiff1 = prev.Y - b.Y, ydiff2 = next.Y - b.Y;
                    if (ydiff1 <= 0 && ydiff2 <= 0 || ydiff1 >= 0 && ydiff2 >= 0)
                    {
                        b.RY = b.Y;
                        b.Locked = true;
                    }
                    else if (ydiff1 <= 0 ? next.Y < b.RY : next.Y > b.RY)
                    {
                        b.RY = next.Y;
                        rightViolate = true;
                    }
                }
            }
            if (leftViolate || rightViolate)
            {
                // The other side turned to line up with the clamped one.
                double h1x = b.LX - b.X, h2x = b.X - b.RX;
                if (leftViolate)
                    b.RY = b.Y + (b.Y - b.LY) / h1x * h2x;
                else
                    b.LY = b.Y + (b.Y - b.RY) / h2x * h1x;
            }
        }
        if (b.H1 == HandleType.Vector)
            (b.LX, b.LY) = (b.X - ax / 3, b.Y - ay / 3);
        if (b.H2 == HandleType.Vector)
            (b.RX, b.RY) = (b.X + bx / 3, b.Y + by / 3);

        // Aligning: not with a free side, and only when a side is aligned.
        if (b.H1 == HandleType.Free || b.H2 == HandleType.Free || b.H1 != HandleType.Aligned && b.H2 != HandleType.Aligned)
            return;
        // Lengths in the (frame, value) plane, the other side's kept.
        lenA = Math.Sqrt((b.X - b.LX) * (b.X - b.LX) + (b.Y - b.LY) * (b.Y - b.LY));
        lenB = Math.Sqrt((b.X - b.RX) * (b.X - b.RX) + (b.Y - b.RY) * (b.Y - b.RY));
        var ratio = lenA / lenB;
        // The left handle unselected: the left side lines up with the right first.
        if (b.H1 == HandleType.Aligned)
            (b.LX, b.LY) = (b.X + ratio * (b.X - b.RX), b.Y + ratio * (b.Y - b.RY));
        if (b.H2 == HandleType.Aligned)
            (b.RX, b.RY) = (b.X + (b.X - b.LX) / ratio, b.Y + (b.Y - b.LY) / ratio);
    }

    /// <summary>A key whose both sides are auto and not locked (is_free_auto_point).</summary>
    static bool IsFreeAuto(Knot k) => IsAuto(k) && !k.Locked;

    /// <summary>BKE_nurb_handle_smooth_fcurve (not cyclic): each run of free auto keys between other keys solved.</summary>
    static void Smooth(Knot[] knots)
    {
        int start = 0, count = 1;
        for (var j = 1; j < knots.Length; j++)
        {
            if (!IsFreeAuto(knots[j]))
            {
                SmoothRun(knots, start, count + 1);
                start = j;
                count = 1;
            }
            else
                count++;
        }
        if (count > 1)
            SmoothRun(knots, start, count);
    }

    /// <summary>bezier_check_solve_end_handle: whether a run's end handle is solved for (vector, or auto at the curve's end) rather than kept.</summary>
    static bool SolvesEnd(Knot k, HandleType type, bool end) => type == HandleType.Vector || end && IsAuto(type) && !k.Locked;

    /// <summary>
    /// bezier_handle_calc_smooth_fcurve: the right handle's value offsets h of the run's keys solved for
    /// a continuous second derivative (handles a third of each interval long), clamped where a key is
    /// auto-clamped; the run's end handles kept (with the correction for a length other than a third)
    /// or solved for zero acceleration.
    /// </summary>
    static void SmoothRun(Knot[] knots, int start, int count)
    {
        if (count < 2)
            return;
        var total = knots.Length;
        Knot first = knots[start], last = knots[start + count - 1];
        var solveFirst = SolvesEnd(first, first.H2, start == 0);
        var solveLast = SolvesEnd(last, last.H1, start + count == total);
        if (count == 2 && solveFirst == solveLast)
            return;
        double[] dx = new double[count], dy = new double[count], l = new double[count], a = new double[count], b = new double[count],
            c = new double[count], d = new double[count], h = new double[count], hmax = new double[count], hmin = new double[count];
        dx[0] = dy[0] = double.NaN;
        for (var i = 1; i < count; i++)
        {
            var j = start + i;
            dx[i] = knots[j].X - knots[j - 1].X;
            dy[i] = knots[j].Y - knots[j - 1].Y;
        }
        l[0] = l[count - 1] = 1;
        for (var i = 1; i < count - 1; i++)
            l[i] = dx[i + 1] / dx[i];

        // Clamp ranges: an auto-clamped key's handle goes neither backwards nor past its neighbours.
        Array.Fill(hmax, float.MaxValue);
        Array.Fill(hmin, -float.MaxValue);
        bool clampedPrev, clampedCur = first.H1 == HandleType.AutoClamped || first.H2 == HandleType.AutoClamped;
        for (var i = 1; i < count; i++)
        {
            var k = knots[start + i];
            clampedPrev = clampedCur;
            clampedCur = k.H1 == HandleType.AutoClamped || k.H2 == HandleType.AutoClamped;
            Clamp(hmax, hmin, i - 1, dy[i], clampedPrev);
            Clamp(hmax, hmin, i, dy[i] * l[i], clampedCur);
        }

        // Boundary conditions: a fixed handle, or zero acceleration.
        double firstAdjust = 0, lastAdjust = 0;
        if (!solveFirst)
        {
            double sx = first.RX - first.X, sy = first.RY - first.Y;
            firstAdjust = HandleAdjust(ref sx, ref sy, dx[1]);
            Lock(a, b, c, d, 0, sy);
        }
        else
            (a[0], b[0], c[0], d[0]) = (0, 2, 1 / l[1], dy[1]);
        if (!solveLast)
        {
            double sx = last.X - last.LX, sy = last.Y - last.LY;
            lastAdjust = HandleAdjust(ref sx, ref sy, dx[count - 1]);
            Lock(a, b, c, d, count - 1, sy);
        }
        else
        {
            var m = count - 1;
            (a[m], b[m], c[m], d[m]) = (l[m] * l[m], 2 * l[m], 0, dy[m] * l[m] * l[m]);
        }
        for (var i = 1; i < count - 1; i++)
            (a[i], b[i], c[i], d[i]) = (l[i] * l[i], 2 * (l[i] + 1), 1 / l[i + 1], dy[i] * l[i] * l[i] + dy[i + 1]);
        // Kept handles whose length in time isn't a third of their interval.
        if (count > 2 || solveLast)
            b[1] += l[1] * firstAdjust;
        if (count > 2 || solveFirst)
            b[count - 2] += lastAdjust;

        if (!SolveWithLimits(a, b, c, d, h, hmin, hmax))
            return;
        for (var i = 1; i < count - 1; i++)
        {
            var k = knots[start + i];
            Output(k, false, -h[i] / l[i], false);
            Output(k, true, h[i], false);
        }
        if (solveFirst)
            Output(first, true, h[0], start == 0);
        if (solveLast)
            Output(last, false, -h[count - 1] / l[count - 1], start + count == total);
    }

    /// <summary>bezier_clamp (no_reverse and no_overshoot are the same flag here).</summary>
    static void Clamp(double[] hmax, double[] hmin, int i, double dy, bool clamped)
    {
        if (dy > 0)
        {
            if (clamped)
            {
                hmax[i] = Math.Min(hmax[i], dy);
                hmin[i] = 0;
            }
        }
        else if (dy < 0)
        {
            if (clamped)
            {
                hmax[i] = 0;
                hmin[i] = Math.Max(hmin[i], dy);
            }
        }
        else if (clamped)
            hmax[i] = hmin[i] = 0;
    }

    /// <summary>bezier_calc_handle_adj: a kept handle scaled to fit its interval, and how far its length in time is from a third.</summary>
    static double HandleAdjust(ref double sx, ref double sy, double dx)
    {
        var fac = dx / (sx + dx / 3);
        if (fac < 1)
        {
            sx *= fac;
            sy *= fac;
        }
        return 1 - 3 * sx / dx;
    }

    static void Lock(double[] a, double[] b, double[] c, double[] d, int i, double value) => (a[i], b[i], c[i], d[i]) = (0, 1, 0, value);

    /// <summary>
    /// tridiagonal_solve_with_limits: solve, lock the unknowns that leave their range at the limit
    /// (those going the right way first), release locked ones that want back in (at most twice each),
    /// until nothing changes.
    /// </summary>
    static bool SolveWithLimits(double[] a, double[] b, double[] c, double[] d, double[] h, double[] hmin, double[] hmax)
    {
        var count = a.Length;
        double[] a0 = (double[])a.Clone(), b0 = (double[])b.Clone(), c0 = (double[])c.Clone(), d0 = (double[])d.Clone();
        var locked = new bool[count];
        var unlocks = new int[count];
        bool overshoot, unlocked;
        do
        {
            if (!SolveTridiagonal(a, b, c, d, h))
                return false;
            bool all = false, anyLocked = false;
            overshoot = unlocked = false;
            do
            {
                for (var i = 0; i < count; i++)
                {
                    if (h[i] >= hmin[i] && h[i] <= hmax[i])
                        continue;
                    overshoot = true;
                    var target = h[i] > hmax[i] ? hmax[i] : hmin[i];
                    if (target != 0 || all)
                    {
                        locked[i] = true;
                        Lock(a, b, c, d, i, target);
                        anyLocked = true;
                    }
                }
                all = true;
            }
            while (overshoot && !anyLocked);
            if (!anyLocked)
            {
                for (var i = 0; i < count; i++)
                {
                    if (!locked[i] || unlocks[i] >= 2)
                        continue;
                    // bezier_relax_direction: which way h[i] would move to satisfy its own equation.
                    var state = a0[i] * h[(i + count - 1) % count] + b0[i] * h[i] + c0[i] * h[(i + 1) % count] - d0[i];
                    var relax = -state * b0[i];
                    if (relax > 0 && h[i] < hmax[i] || relax < 0 && h[i] > hmin[i])
                    {
                        (a[i], b[i], c[i], d[i]) = (a0[i], b0[i], c0[i], d0[i]);
                        locked[i] = false;
                        unlocks[i]++;
                        unlocked = true;
                    }
                }
            }
        }
        while (overshoot || unlocked);
        return true;
    }

    /// <summary>
    /// BLI_tridiagonal_solve (the Thomas algorithm). Blender calls its cyclic form, which for a run that
    /// isn't cyclic (a[0] and c[last] zero) is this one.
    /// </summary>
    static bool SolveTridiagonal(double[] a, double[] b, double[] c, double[] d, double[] x)
    {
        var count = a.Length;
        var c1 = new double[count];
        var d1 = new double[count];
        double cPrev = c1[0] = c[0] / b[0], dPrev = d1[0] = d[0] / b[0];
        for (var i = 1; i < count; i++)
        {
            var denominator = b[i] - a[i] * cPrev;
            cPrev = c1[i] = c[i] / denominator;
            dPrev = d1[i] = (d[i] - a[i] * dPrev) / denominator;
        }
        var xPrev = x[count - 1] = dPrev;
        for (var i = count - 2; i >= 0; i--)
            xPrev = x[i] = d1[i] - c1[i] * xPrev;
        return double.IsFinite(xPrev);
    }

    /// <summary>
    /// bezier_output_handle: a solved handle's value offset written to an auto or vector side (its time
    /// kept); an aligned other side turned to line up keeping its length, or at the curve's end an auto
    /// or vector other side mirrored.
    /// </summary>
    static void Output(Knot k, bool right, double dy, bool endpoint)
    {
        var side = right ? k.H2 : k.H1;
        var other = right ? k.H1 : k.H2;
        if (!(IsAuto(side) || side == HandleType.Vector))
            return;
        if (right)
            k.RY = k.Y + dy;
        else
            k.LY = k.Y + dy;
        double sx = right ? k.RX : k.LX, sy = right ? k.RY : k.LY;
        if (other == HandleType.Aligned)
        {
            double ox = right ? k.LX : k.RX, oy = right ? k.LY : k.RY;
            var otherLength = Math.Sqrt((k.X - ox) * (k.X - ox) + (k.Y - oy) * (k.Y - oy));
            var sideLength = Math.Sqrt((k.X - sx) * (k.X - sx) + (k.Y - sy) * (k.Y - sy));
            var f = otherLength / sideLength;
            SetOther(k, right, k.X + (k.X - sx) * f, k.Y + (k.Y - sy) * f);
        }
        else if (endpoint && (IsAuto(other) || other == HandleType.Vector))
            SetOther(k, right, k.X + (k.X - sx), k.Y + (k.Y - sy));
    }

    static void SetOther(Knot k, bool right, double x, double y)
    {
        if (right)
            (k.LX, k.LY) = (x, y);
        else
            (k.RX, k.RY) = (x, y);
    }
}

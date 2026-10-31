namespace Statventure.Audio;

/// <summary>
/// fdlibm's exp and pow as Chromium builds them (third_party/fdlibm/ieee754.cc, from V8): Blink's
/// Web Audio uses these rather than the C library, so the results are the same on every platform
/// (Docs/Audio.md). A line-for-line port on the IEEE words.
/// </summary>
internal static class Fdlibm
{
    static int High(double d) => (int)(BitConverter.DoubleToInt64Bits(d) >> 32);
    static uint Low(double d) => (uint)BitConverter.DoubleToInt64Bits(d);
    static double Words(int high, uint low) => BitConverter.Int64BitsToDouble(((long)high << 32) | low);
    static double WithHigh(double d, int high) => Words(high, Low(d));
    static double WithLow(double d, uint low) => Words(High(d), low);

    public static double Exp(double x)
    {
        const double one = 1.0, oThreshold = 7.09782712893383973096e+02, uThreshold = -7.45133219101941108420e+02,
            invln2 = 1.44269504088896338700e+00, P1 = 1.66666666666666019037e-01, P2 = -2.77777777770155933842e-03,
            P3 = 6.61375632143793436117e-05, P4 = -1.65339022054652515390e-06, P5 = 4.13813679705723846039e-08,
            E = 2.718281828459045, huge = 1.0e+300, twom1000 = 9.33263618503218878990e-302, two1023 = 8.988465674311579539e307;
        ReadOnlySpan<double> halF = [0.5, -0.5];
        ReadOnlySpan<double> ln2HI = [6.93147180369123816490e-01, -6.93147180369123816490e-01];
        ReadOnlySpan<double> ln2LO = [1.90821492927058770002e-10, -1.90821492927058770002e-10];
        double y, hi = 0.0, lo = 0.0, c, t, twopk;
        int k = 0;
        var hx = (uint)High(x);
        var xsb = (int)((hx >> 31) & 1);
        hx &= 0x7FFFFFFF;
        if (hx >= 0x40862E42)
        {
            if (hx >= 0x7FF00000)
            {
                var lx = Low(x);
                if (((hx & 0xFFFFF) | lx) != 0)
                    return x + x;
                return xsb == 0 ? x : 0.0;
            }
            if (x > oThreshold)
                return huge * huge;
            if (x < uThreshold)
                return twom1000 * twom1000;
        }
        if (hx > 0x3FD62E42)
        {
            if (hx < 0x3FF0A2B2)
            {
                if (x == 1.0)
                    return E;
                hi = x - ln2HI[xsb];
                lo = ln2LO[xsb];
                k = 1 - xsb - xsb;
            }
            else
            {
                k = (int)(invln2 * x + halF[xsb]);
                t = k;
                hi = x - t * ln2HI[0];
                lo = t * ln2LO[0];
            }
            x = hi - lo;
        }
        else if (hx < 0x3E300000)
        {
            if (huge + x > one)
                return one + x;
        }
        else
            k = 0;
        t = x * x;
        twopk = k >= -1021 ? Words(0x3FF00000 + (int)((uint)k << 20), 0) : Words(0x3FF00000 + (int)((uint)(k + 1000) << 20), 0);
        c = x - t * (P1 + t * (P2 + t * (P3 + t * (P4 + t * P5))));
        if (k == 0)
            return one - ((x * c) / (c - 2.0) - x);
        y = one - ((lo - (x * c) / (2.0 - c)) - hi);
        if (k >= -1021)
        {
            if (k == 1024)
                return y * 2.0 * two1023;
            return y * twopk;
        }
        return y * twopk * twom1000;
    }

    public static double Pow(double x, double y)
    {
        ReadOnlySpan<double> bp = [1.0, 1.5];
        ReadOnlySpan<double> dpH = [0.0, 5.84962487220764160156e-01];
        ReadOnlySpan<double> dpL = [0.0, 1.35003920212974897128e-08];
        const double zero = 0.0, one = 1.0, two = 2.0, two53 = 9007199254740992.0, huge = 1.0e300, tiny = 1.0e-300,
            L1 = 5.99999999999994648725e-01, L2 = 4.28571428578550184252e-01, L3 = 3.33333329818377432918e-01,
            L4 = 2.72728123808534006489e-01, L5 = 2.30660745775561754067e-01, L6 = 2.06975017800338417784e-01,
            P1 = 1.66666666666666019037e-01, P2 = -2.77777777770155933842e-03, P3 = 6.61375632143793436117e-05,
            P4 = -1.65339022054652515390e-06, P5 = 4.13813679705723846039e-08, lg2 = 6.93147180559945286227e-01,
            lg2H = 6.93147182464599609375e-01, lg2L = -1.90465429995776804525e-09, ovt = 8.0085662595372944372e-0017,
            cp = 9.61796693925975554329e-01, cpH = 9.61796700954437255859e-01, cpL = -7.02846165095275826516e-09,
            ivln2 = 1.44269504088896338700e+00, ivln2H = 1.44269502162933349609e+00, ivln2L = 1.92596299112661746887e-08;
        double z, ax, zH, zL, pH, pL, y1, t1, t2, r, s, t, u, v, w;
        int i, j, k, yisint, n;
        int hx = High(x), hy = High(y);
        uint lx = Low(x), ly = Low(y);
        int ix = hx & 0x7fffffff, iy = hy & 0x7fffffff;
        if ((iy | (int)ly) == 0)
            return one;
        if (ix > 0x7ff00000 || (ix == 0x7ff00000 && lx != 0) || iy > 0x7ff00000 || (iy == 0x7ff00000 && ly != 0))
            return x + y;
        yisint = 0;
        if (hx < 0)
        {
            if (iy >= 0x43400000)
                yisint = 2;
            else if (iy >= 0x3ff00000)
            {
                k = (iy >> 20) - 0x3ff;
                if (k > 20)
                {
                    j = (int)(ly >> (52 - k));
                    if ((j << (52 - k)) == (int)ly)
                        yisint = 2 - (j & 1);
                }
                else if (ly == 0)
                {
                    j = iy >> (20 - k);
                    if ((j << (20 - k)) == iy)
                        yisint = 2 - (j & 1);
                }
            }
        }
        if (ly == 0)
        {
            if (iy == 0x7ff00000)
            {
                if (((ix - 0x3ff00000) | (int)lx) == 0)
                    return y - y;
                if (ix >= 0x3ff00000)
                    return hy >= 0 ? y : zero;
                return hy < 0 ? -y : zero;
            }
            if (iy == 0x3ff00000)
                return hy < 0 ? one / x : x;
            if (hy == 0x40000000)
                return x * x;
            if (hy == 0x3fe00000 && hx >= 0)
                return Math.Sqrt(x);
        }
        ax = Math.Abs(x);
        if (lx == 0 && (ix == 0x7ff00000 || ix == 0 || ix == 0x3ff00000))
        {
            z = ax;
            if (hy < 0)
                z = one / z;
            if (hx < 0)
            {
                if (((ix - 0x3ff00000) | yisint) == 0)
                    z = double.NaN;
                else if (yisint == 1)
                    z = -z;
            }
            return z;
        }
        n = (hx >> 31) + 1;
        if ((n | yisint) == 0)
            return double.NaN;
        s = one;
        if ((n | (yisint - 1)) == 0)
            s = -one;
        if (iy > 0x41e00000)
        {
            if (iy > 0x43f00000)
            {
                if (ix <= 0x3fefffff)
                    return hy < 0 ? huge * huge : tiny * tiny;
                if (ix >= 0x3ff00000)
                    return hy > 0 ? huge * huge : tiny * tiny;
            }
            if (ix < 0x3fefffff)
                return hy < 0 ? s * huge * huge : s * tiny * tiny;
            if (ix > 0x3ff00000)
                return hy > 0 ? s * huge * huge : s * tiny * tiny;
            t = ax - one;
            w = (t * t) * (0.5 - t * (0.3333333333333333333333 - t * 0.25));
            u = ivln2H * t;
            v = t * ivln2L - w * ivln2;
            t1 = WithLow(u + v, 0);
            t2 = v - (t1 - u);
        }
        else
        {
            double ss, s2, sH, sL, tH, tL;
            n = 0;
            if (ix < 0x00100000)
            {
                ax *= two53;
                n -= 53;
                ix = High(ax);
            }
            n += (ix >> 20) - 0x3ff;
            j = ix & 0x000fffff;
            ix = j | 0x3ff00000;
            if (j <= 0x3988E)
                k = 0;
            else if (j < 0xBB67A)
                k = 1;
            else
            {
                k = 0;
                n += 1;
                ix -= 0x00100000;
            }
            ax = WithHigh(ax, ix);
            u = ax - bp[k];
            v = one / (ax + bp[k]);
            ss = u * v;
            sH = WithLow(ss, 0);
            tH = WithHigh(zero, ((ix >> 1) | 0x20000000) + 0x00080000 + (k << 18));
            tL = ax - (tH - bp[k]);
            sL = v * ((u - sH * tH) - sH * tL);
            s2 = ss * ss;
            r = s2 * s2 * (L1 + s2 * (L2 + s2 * (L3 + s2 * (L4 + s2 * (L5 + s2 * L6)))));
            r += sL * (sH + ss);
            s2 = sH * sH;
            tH = WithLow(3.0 + s2 + r, 0);
            tL = r - ((tH - 3.0) - s2);
            u = sH * tH;
            v = sL * tH + tL * ss;
            pH = WithLow(u + v, 0);
            pL = v - (pH - u);
            zH = cpH * pH;
            zL = cpL * pH + pL * cp + dpL[k];
            t = n;
            t1 = WithLow(((zH + zL) + dpH[k]) + t, 0);
            t2 = zL - (((t1 - t) - dpH[k]) - zH);
        }
        y1 = WithLow(y, 0);
        pL = (y - y1) * t1 + y * t2;
        pH = y1 * t1;
        z = pL + pH;
        j = High(z);
        i = (int)Low(z);
        if (j >= 0x40900000)
        {
            if (((j - 0x40900000) | i) != 0)
                return s * huge * huge;
            if (pL + ovt > z - pH)
                return s * huge * huge;
        }
        else if ((j & 0x7fffffff) >= 0x4090cc00)
        {
            if (((j - unchecked((int)0xc090cc00)) | i) != 0)
                return s * tiny * tiny;
            if (pL <= z - pH)
                return s * tiny * tiny;
        }
        i = j & 0x7fffffff;
        k = (i >> 20) - 0x3ff;
        n = 0;
        if (i > 0x3fe00000)
        {
            n = j + (0x00100000 >> (k + 1));
            k = ((n & 0x7fffffff) >> 20) - 0x3ff;
            t = WithHigh(zero, n & ~(0x000fffff >> k));
            n = ((n & 0x000fffff) | 0x00100000) >> (20 - k);
            if (j < 0)
                n = -n;
            pH -= t;
        }
        t = WithLow(pL + pH, 0);
        u = t * lg2H;
        v = (pL - (t - pH)) * lg2 + t * lg2L;
        z = u + v;
        w = v - (z - u);
        t = z * z;
        t1 = z - t * (P1 + t * (P2 + t * (P3 + t * (P4 + t * P5))));
        r = (z * t1) / ((t1 - two) - (w + z * w));
        z = one - (r - z);
        j = High(z);
        j += (int)((uint)n << 20);
        if ((j >> 20) <= 0)
            z = Math.ScaleB(z, n);
        else
            z = WithHigh(z, High(z) + (int)((uint)n << 20));
        return s * z;
    }
}

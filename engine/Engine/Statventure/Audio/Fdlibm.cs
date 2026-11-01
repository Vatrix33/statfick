namespace Statventure.Audio;

/// <summary>
/// fdlibm's exp, pow, log, sin, cos and expf as Chromium builds them (third_party/fdlibm/ieee754.cc, from V8): Blink's
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

    /// <summary>fdlibm's expf: exp in double, rounded to float (Blink's biquad pow10).</summary>
    public static float Expf(float x) => (float)Exp(x);

    public static double Log(double x)
    {
        const double ln2Hi = 6.93147180369123816490e-01, ln2Lo = 1.90821492927058770002e-10, two54 = 1.80143985094819840000e+16,
            Lg1 = 6.666666666666735130e-01, Lg2 = 3.999999999940941908e-01, Lg3 = 2.857142874366239149e-01, Lg4 = 2.222219843214978396e-01,
            Lg5 = 1.818357216161805012e-01, Lg6 = 1.531383769920937332e-01, Lg7 = 1.479819860511658591e-01;
        int hx = High(x);
        var lx = Low(x);
        var k = 0;
        if (hx < 0x00100000)
        {
            if (((hx & 0x7FFFFFFF) | (int)lx) == 0)
                return double.NegativeInfinity;
            if (hx < 0)
                return double.NaN;
            k -= 54;
            x *= two54;
            hx = High(x);
        }
        if (hx >= 0x7FF00000)
            return x + x;
        k += (hx >> 20) - 1023;
        hx &= 0x000FFFFF;
        var i = (hx + 0x95F64) & 0x100000;
        x = WithHigh(x, hx | (i ^ 0x3FF00000));
        k += i >> 20;
        var f = x - 1.0;
        double dk;
        if ((0x000FFFFF & (2 + hx)) < 3)
        {
            if (f == 0)
            {
                if (k == 0)
                    return 0;
                dk = k;
                return dk * ln2Hi + dk * ln2Lo;
            }
            var r = f * f * (0.5 - 0.33333333333333333 * f);
            if (k == 0)
                return f - r;
            dk = k;
            return dk * ln2Hi - ((r - dk * ln2Lo) - f);
        }
        var s = f / (2.0 + f);
        dk = k;
        var z = s * s;
        i = hx - 0x6147A;
        var w = z * z;
        var j = 0x6B851 - hx;
        var t1 = w * (Lg2 + w * (Lg4 + w * Lg6));
        var t2 = z * (Lg1 + w * (Lg3 + w * (Lg5 + w * Lg7)));
        i |= j;
        var R = t2 + t1;
        if (i > 0)
        {
            var hfsq = 0.5 * f * f;
            if (k == 0)
                return f - (hfsq - s * (hfsq + R));
            return dk * ln2Hi - ((hfsq - (s * (hfsq + R) + dk * ln2Lo)) - f);
        }
        if (k == 0)
            return f - s * (f - R);
        return dk * ln2Hi - ((s * (f - R) - dk * ln2Lo) - f);
    }

    public static double Sin(double x)
    {
        var ix = High(x) & 0x7FFFFFFF;
        if (ix <= 0x3FE921FB)
            return KernelSin(x, 0, 0);
        if (ix >= 0x7FF00000)
            return x - x;
        var n = RemPio2(x, out var y0, out var y1);
        return (n & 3) switch
        {
            0 => KernelSin(y0, y1, 1),
            1 => KernelCos(y0, y1),
            2 => -KernelSin(y0, y1, 1),
            _ => -KernelCos(y0, y1),
        };
    }

    public static double Cos(double x)
    {
        var ix = High(x) & 0x7FFFFFFF;
        if (ix <= 0x3FE921FB)
            return KernelCos(x, 0);
        if (ix >= 0x7FF00000)
            return x - x;
        var n = RemPio2(x, out var y0, out var y1);
        return (n & 3) switch
        {
            0 => KernelCos(y0, y1),
            1 => -KernelSin(y0, y1, 1),
            2 => -KernelCos(y0, y1),
            _ => KernelSin(y0, y1, 1),
        };
    }

    static double KernelCos(double x, double y)
    {
        const double C1 = 4.16666666666666019037e-02, C2 = -1.38888888888741095749e-03, C3 = 2.48015872894767294178e-05,
            C4 = -2.75573143513906633035e-07, C5 = 2.08757232129817482790e-09, C6 = -1.13596475577881948265e-11;
        var ix = High(x) & 0x7FFFFFFF;
        if (ix < 0x3E400000 && (int)x == 0)
            return 1;
        var z = x * x;
        var r = z * (C1 + z * (C2 + z * (C3 + z * (C4 + z * (C5 + z * C6)))));
        if (ix < 0x3FD33333)
            return 1 - (0.5 * z - (z * r - x * y));
        var qx = ix > 0x3FE90000 ? 0.28125 : Words(ix - 0x00200000, 0);
        var iz = 0.5 * z - qx;
        var a = 1 - qx;
        return a - (iz - (z * r - x * y));
    }

    static double KernelSin(double x, double y, int iy)
    {
        const double S1 = -1.66666666666666324348e-01, S2 = 8.33333333332248946124e-03, S3 = -1.98412698298579493134e-04,
            S4 = 2.75573137070700676789e-06, S5 = -2.50507602534068634195e-08, S6 = 1.58969099521155010221e-10;
        var ix = High(x) & 0x7FFFFFFF;
        if (ix < 0x3E400000 && (int)x == 0)
            return x;
        var z = x * x;
        var v = z * x;
        var r = S2 + z * (S3 + z * (S4 + z * (S5 + z * S6)));
        if (iy == 0)
            return x + v * (S1 + z * r);
        return x - ((z * (0.5 * y - v * r) - y) - v * S1);
    }

    static readonly int[] NPio2Hw =
    [
        0x3FF921FB, 0x400921FB, 0x4012D97C, 0x401921FB, 0x401F6A7A, 0x4022D97C, 0x4025FDBB, 0x402921FB, 0x402C463A, 0x402F6A7A,
        0x4031475C, 0x4032D97C, 0x40346B9C, 0x4035FDBB, 0x40378FDB, 0x403921FB, 0x403AB41B, 0x403C463A, 0x403DD85A, 0x403F6A7A,
        0x40407E4C, 0x4041475C, 0x4042106C, 0x4042D97C, 0x4043A28C, 0x40446B9C, 0x404534AC, 0x4045FDBB, 0x4046C6CB, 0x40478FDB,
        0x404858EB, 0x404921FB,
    ];

    /// <summary>__ieee754_rem_pio2 up to 2^19 · π/2 (Web Audio's angles are within π); larger arguments aren't ported.</summary>
    static int RemPio2(double x, out double y0, out double y1)
    {
        const double invpio2 = 6.36619772367581382433e-01, pio2_1 = 1.57079632673412561417e+00, pio2_1t = 6.07710050650619224932e-11,
            pio2_2 = 6.07710050630396597660e-11, pio2_2t = 2.02226624879595063154e-21, pio2_3 = 2.02226624871116645580e-21,
            pio2_3t = 8.47842766036889956997e-32;
        var hx = High(x);
        var ix = hx & 0x7FFFFFFF;
        double z;
        if (ix <= 0x3FE921FB)
        {
            y0 = x;
            y1 = 0;
            return 0;
        }
        if (ix < 0x4002D97C)
        {
            if (hx > 0)
            {
                z = x - pio2_1;
                if (ix != 0x3FF921FB)
                {
                    y0 = z - pio2_1t;
                    y1 = (z - y0) - pio2_1t;
                }
                else
                {
                    z -= pio2_2;
                    y0 = z - pio2_2t;
                    y1 = (z - y0) - pio2_2t;
                }
                return 1;
            }
            z = x + pio2_1;
            if (ix != 0x3FF921FB)
            {
                y0 = z + pio2_1t;
                y1 = (z - y0) + pio2_1t;
            }
            else
            {
                z += pio2_2;
                y0 = z + pio2_2t;
                y1 = (z - y0) + pio2_2t;
            }
            return -1;
        }
        if (ix > 0x413921FB)
            throw new NotSupportedException("fdlibm's reduction of arguments beyond 2^19·π/2 isn't ported");
        var t = Math.Abs(x);
        var n = (int)(t * invpio2 + 0.5);
        double fn = n;
        var r = t - fn * pio2_1;
        var w = fn * pio2_1t;
        if (n < 32 && ix != NPio2Hw[n - 1])
            y0 = r - w;
        else
        {
            var j = ix >> 20;
            y0 = r - w;
            var i = j - ((High(y0) >> 20) & 0x7FF);
            if (i > 16)
            {
                t = r;
                w = fn * pio2_2;
                r = t - w;
                w = fn * pio2_2t - ((t - r) - w);
                y0 = r - w;
                i = j - ((High(y0) >> 20) & 0x7FF);
                if (i > 49)
                {
                    t = r;
                    w = fn * pio2_3;
                    r = t - w;
                    w = fn * pio2_3t - ((t - r) - w);
                    y0 = r - w;
                }
            }
        }
        y1 = (r - y0) - w;
        if (hx < 0)
        {
            y0 = -y0;
            y1 = -y1;
            return -n;
        }
        return n;
    }

    /// <summary>fdlibm's float wrappers: the double functions rounded to float.</summary>
    public static float Powf(float x, float y) => (float)Pow(x, y);
    public static float Asinf(double x) => (float)Asin(x);

    public static double Asin(double x)
    {
        const double one = 1.0, huge = 1.000e+300, pio2_hi = 1.57079632679489655800e+00, pio2_lo = 6.12323399573676603587e-17,
            pio4_hi = 7.85398163397448278999e-01, pS0 = 1.66666666666666657415e-01, pS1 = -3.25565818622400915405e-01,
            pS2 = 2.01212532134862925881e-01, pS3 = -4.00555345006794114027e-02, pS4 = 7.91534994289814532176e-04,
            pS5 = 3.47933107596021167570e-05, qS1 = -2.40339491173441421878e+00, qS2 = 2.02094576023350569471e+00,
            qS3 = -6.88283971605453293030e-01, qS4 = 7.70381505559019352791e-02;
        double t = 0, w, p, q, c, r, s;
        var hx = High(x);
        var ix = hx & 0x7FFFFFFF;
        if (ix >= 0x3FF00000)
        {
            var lx = Low(x);
            if (((ix - 0x3FF00000) | (int)lx) == 0)
                return x * pio2_hi + x * pio2_lo;
            return double.NaN;
        }
        if (ix < 0x3FE00000)
        {
            if (ix < 0x3E400000)
            {
                if (huge + x > one)
                    return x;
            }
            else
                t = x * x;
            p = t * (pS0 + t * (pS1 + t * (pS2 + t * (pS3 + t * (pS4 + t * pS5)))));
            q = one + t * (qS1 + t * (qS2 + t * (qS3 + t * qS4)));
            w = p / q;
            return x + x * w;
        }
        w = one - Math.Abs(x);
        t = w * 0.5;
        p = t * (pS0 + t * (pS1 + t * (pS2 + t * (pS3 + t * (pS4 + t * pS5)))));
        q = one + t * (qS1 + t * (qS2 + t * (qS3 + t * qS4)));
        s = Math.Sqrt(t);
        if (ix >= 0x3FEF3333)
        {
            w = p / q;
            t = pio2_hi - (2.0 * (s + s * w) - pio2_lo);
        }
        else
        {
            w = WithLow(s, 0);
            c = (t - w * w) / (s + w);
            r = p / q;
            p = 2.0 * s * r - (pio2_lo - 2.0 * c);
            q = pio4_hi - 2.0 * w;
            t = pio4_hi - (p - q);
        }
        return hx > 0 ? t : -t;
    }
}

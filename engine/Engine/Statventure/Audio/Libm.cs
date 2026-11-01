namespace Statventure.Audio;

/// <summary>
/// The C library's float exp2f, log2f, log10f and powf as Chromium on this PC gets them: glibc 2.43
/// (sysdeps/ieee754/flt-32). Blink's oscillator and periodic waves call exp2f and log2f, its
/// decibel conversions powf(10, x) and log10f.
/// x86-64 glibc picks variants built with FMA on CPUs that have it; ported with those fused steps,
/// though the unfused SSE2 variants give the same float for every input. Checked on all 2³² floats
/// against the system's glibc (Docs/Audio.md).
/// </summary>
internal static partial class Libm
{
    static uint AsUInt(float f) => BitConverter.SingleToUInt32Bits(f);
    static float AsFloat(uint u) => BitConverter.UInt32BitsToSingle(u);
    static ulong AsULong(double d) => BitConverter.DoubleToUInt64Bits(d);
    static double AsDouble(ulong u) => BitConverter.UInt64BitsToDouble(u);

    static readonly ulong[] Exp2Table =
    [
        0x3ff0000000000000, 0x3fefd9b0d3158574, 0x3fefb5586cf9890f, 0x3fef9301d0125b51,
        0x3fef72b83c7d517b, 0x3fef54873168b9aa, 0x3fef387a6e756238, 0x3fef1e9df51fdee1,
        0x3fef06fe0a31b715, 0x3feef1a7373aa9cb, 0x3feedea64c123422, 0x3feece086061892d,
        0x3feebfdad5362a27, 0x3feeb42b569d4f82, 0x3feeab07dd485429, 0x3feea47eb03a5585,
        0x3feea09e667f3bcd, 0x3fee9f75e8ec5f74, 0x3feea11473eb0187, 0x3feea589994cce13,
        0x3feeace5422aa0db, 0x3feeb737b0cdc5e5, 0x3feec49182a3f090, 0x3feed503b23e255d,
        0x3feee89f995ad3ad, 0x3feeff76f2fb5e47, 0x3fef199bdd85529c, 0x3fef3720dcef9069,
        0x3fef5818dcfba487, 0x3fef7c97337b9b5f, 0x3fefa4afa2a490da, 0x3fefd0765b6e4540,
    ];

    static double D(ulong bits) => BitConverter.UInt64BitsToDouble(bits);

    static readonly double Exp2C0 = D(0x3fac6af84b912394); // 0x1.c6af84b912394p-5
    static readonly double Exp2C1 = D(0x3fcebfce50fac4f3); // 0x1.ebfce50fac4f3p-3
    static readonly double Exp2C2 = D(0x3fe62e42ff0c52d6); // 0x1.62e42ff0c52d6p-1

    public static float Exp2f(float x)
    {
        double xd = x;
        var abstop = (AsUInt(x) >> 20) & 0x7ff;
        if (abstop >= AsUInt(128.0f) >> 20)
        {
            if (AsUInt(x) == AsUInt(float.NegativeInfinity))
                return 0.0f;
            if (abstop >= AsUInt(float.PositiveInfinity) >> 20)
                return x + x;
            if (x > 0.0f)
                return float.PositiveInfinity;
            if (x <= -150.0f)
                return 0.0f;
        }
        const double Shift = 211106232532992.0; // 0x1.8p+52 / N, exact
        var kd = xd + Shift;
        var ki = AsULong(kd);
        kd -= Shift;
        var r = xd - kd;
        var t = Exp2Table[ki % 32];
        t += ki << (52 - 5);
        var s = AsDouble(t);
        var z = Math.FusedMultiplyAdd(Exp2C0, r, Exp2C1);
        var r2 = r * r;
        var y = Math.FusedMultiplyAdd(Exp2C2, r, 1);
        y = Math.FusedMultiplyAdd(z, r2, y);
        y *= s;
        return (float)y;
    }

    static readonly (double InvC, double LogC)[] Log2Table =
    [
        (D(0x3ff661ec79f8f3be), D(0xbfdefec65b963019)), // 0x1.661ec79f8f3bep+0, -0x1.efec65b963019p-2
        (D(0x3ff571ed4aaf883d), D(0xbfdb0b6832d4fca4)), // 0x1.571ed4aaf883dp+0, -0x1.b0b6832d4fca4p-2
        (D(0x3ff49539f0f010b0), D(0xbfd7418b0a1fb77b)), // 0x1.49539f0f010bp+0, -0x1.7418b0a1fb77bp-2
        (D(0x3ff3c995b0b80385), D(0xbfd39de91a6dcf7b)), // 0x1.3c995b0b80385p+0, -0x1.39de91a6dcf7bp-2
        (D(0x3ff30d190c8864a5), D(0xbfd01d9bf3f2b631)), // 0x1.30d190c8864a5p+0, -0x1.01d9bf3f2b631p-2
        (D(0x3ff25e227b0b8ea0), D(0xbfc97c1d1b3b7af0)), // 0x1.25e227b0b8eap+0, -0x1.97c1d1b3b7afp-3
        (D(0x3ff1bb4a4a1a343f), D(0xbfc2f9e393af3c9f)), // 0x1.1bb4a4a1a343fp+0, -0x1.2f9e393af3c9fp-3
        (D(0x3ff12358f08ae5ba), D(0xbfb960cbbf788d5c)), // 0x1.12358f08ae5bap+0, -0x1.960cbbf788d5cp-4
        (D(0x3ff0953f419900a7), D(0xbfaa6f9db6475fce)), // 0x1.0953f419900a7p+0, -0x1.a6f9db6475fcep-5
        (D(0x3ff0000000000000), D(0x0000000000000000)), // 0x1p+0, 0x0p+0
        (D(0x3fee608cfd9a47ac), D(0x3fb338ca9f24f53d)), // 0x1.e608cfd9a47acp-1, 0x1.338ca9f24f53dp-4
        (D(0x3feca4b31f026aa0), D(0x3fc476a9543891ba)), // 0x1.ca4b31f026aap-1, 0x1.476a9543891bap-3
        (D(0x3feb2036576afce6), D(0x3fce840b4ac4e4d2)), // 0x1.b2036576afce6p-1, 0x1.e840b4ac4e4d2p-3
        (D(0x3fe9c2d163a1aa2d), D(0x3fd40645f0c6651c)), // 0x1.9c2d163a1aa2dp-1, 0x1.40645f0c6651cp-2
        (D(0x3fe886e6037841ed), D(0x3fd88e9c2c1b9ff8)), // 0x1.886e6037841edp-1, 0x1.88e9c2c1b9ff8p-2
        (D(0x3fe767dcf5534862), D(0x3fdce0a44eb17bcc)), // 0x1.767dcf5534862p-1, 0x1.ce0a44eb17bccp-2
    ];

    static readonly double Log2A0 = D(0xbfd712b6f70a7e4d); // -0x1.712b6f70a7e4dp-2
    static readonly double Log2A1 = D(0x3fdecabf496832e0); // 0x1.ecabf496832ep-2
    static readonly double Log2A2 = D(0xbfe715479ffae3de); // -0x1.715479ffae3dep-1
    static readonly double Log2A3 = D(0x3ff715475f35c8b8); // 0x1.715475f35c8b8p0

    public static float Log2f(float x)
    {
        var ix = AsUInt(x);
        if (ix == 0x3f800000)
            return 0;
        if (ix - 0x00800000 >= 0x7f800000 - 0x00800000)
        {
            if (ix * 2 == 0)
                return float.NegativeInfinity;
            if (ix == 0x7f800000)
                return x;
            if ((ix & 0x80000000) != 0 || ix * 2 >= 0xff000000)
                return float.NaN;
            ix = AsUInt(x * 8388608.0f); // 0x1p23f
            ix -= 23 << 23;
        }
        var tmp = ix - 0x3f330000;
        var i = (int)((tmp >> (23 - 4)) % 16);
        var top = tmp & 0xff800000;
        var iz = ix - top;
        var k = (int)tmp >> 23;
        var (invc, logc) = Log2Table[i];
        double z = AsFloat(iz);
        var r = Math.FusedMultiplyAdd(z, invc, -1);
        var y0 = logc + k;
        var r2 = r * r;
        var y = Math.FusedMultiplyAdd(Log2A1, r, Log2A2);
        y = Math.FusedMultiplyAdd(Log2A0, r2, y);
        var p = Math.FusedMultiplyAdd(Log2A3, r, y0);
        y = Math.FusedMultiplyAdd(y, r2, p);
        return (float)y;
    }

    // ---------- log10f: glibc 2.43's (CORE-MATH, correctly rounded) ----------

    static readonly double[] Log10TR = [
        D(0x3ff0000000000000), D(0x3fef81f820000000), D(0x3fef07c1f0000000),
        D(0x3fee9131ac000000), D(0x3fee1e1e1e000000), D(0x3fedae6077000000),
        D(0x3fed41d41d000000), D(0x3fecd85689000000), D(0x3fec71c71c000000),
        D(0x3fec0e0704000000), D(0x3febacf915000000), D(0x3feb4e81b5000000),
        D(0x3feaf286bd000000), D(0x3fea98ef60000000), D(0x3fea41a41a000000),
        D(0x3fe9ec8e95000000), D(0x3fe999999a000000), D(0x3fe948b0fd000000),
        D(0x3fe8f9c190000000), D(0x3fe8acb90f000000), D(0x3fe8618618000000),
        D(0x3fe8181818000000), D(0x3fe7d05f41000000), D(0x3fe78a4c81000000),
        D(0x3fe745d174000000), D(0x3fe702e05c000000), D(0x3fe6c16c17000000),
        D(0x3fe6816817000000), D(0x3fe642c859000000), D(0x3fe6058160000000),
        D(0x3fe5c9882c000000), D(0x3fe58ed231000000), D(0x3fe5555555000000),
        D(0x3fe51d07eb000000), D(0x3fe4e5e0a7000000), D(0x3fe4afd6a0000000),
        D(0x3fe47ae148000000), D(0x3fe446f865000000), D(0x3fe4141414000000),
        D(0x3fe3e22cbd000000), D(0x3fe3b13b14000000), D(0x3fe3813814000000),
        D(0x3fe3521cfb000000), D(0x3fe323e34a000000), D(0x3fe2f684be000000),
        D(0x3fe2c9fb4e000000), D(0x3fe29e412a000000), D(0x3fe27350b9000000),
        D(0x3fe2492492000000), D(0x3fe21fb781000000), D(0x3fe1f7047e000000),
        D(0x3fe1cf06ae000000), D(0x3fe1a7b961000000), D(0x3fe1811812000000),
        D(0x3fe15b1e5f000000), D(0x3fe135c811000000), D(0x3fe1111111000000),
        D(0x3fe0ecf56c000000), D(0x3fe0c97150000000), D(0x3fe0a6810a000000),
        D(0x3fe0842108000000), D(0x3fe0624dd3000000), D(0x3fe0410410000000),
        D(0x3fe0204081000000), D(0x3fe0000000000000),
    ];
    static readonly double[] Log10TL = [
        D(0xbd0d45fd6237ebe3), D(0x3f7b947689311b6e), D(0x3f8b5e909c96d7d5),
        D(0x3f945f4f59ed2165), D(0x3f9af5f92cbd8f1e), D(0x3fa0ba01a606de8c),
        D(0x3fa3ed119b9a2b7b), D(0x3fa714834298eec2), D(0x3faa30a9d98357fb),
        D(0x3fad41d512670813), D(0x3fb02428c0f65519), D(0x3fb1a23444eecc3e),
        D(0x3fb31b30543f4cb4), D(0x3fb48f3ed39bfd04), D(0x3fb5fe8049a0e423),
        D(0x3fb769140a6aa008), D(0x3fb8cf1836c98cb3), D(0x3fba30a9d55541a1),
        D(0x3fbb8de4d1ee823e), D(0x3fbce6e4202ca2e6), D(0x3fbe3bc1accace07),
        D(0x3fbf8c9683b5abd4), D(0x3fc06cbd68ca9a6e), D(0x3fc11142f19df730),
        D(0x3fc1b3e71fa7a97f), D(0x3fc254b4d37a46e3), D(0x3fc2f3b6912cbf07),
        D(0x3fc390f683115886), D(0x3fc42c7e7fffc5a8), D(0x3fc4c65808c78d3c),
        D(0x3fc55e8c50751c55), D(0x3fc5f52445dec3d8), D(0x3fc68a288c3f1200),
        D(0x3fc71da17bdf0d19), D(0x3fc7af973608afd9), D(0x3fc84011952a2579),
        D(0x3fc8cf1837a7ea60), D(0x3fc95cb2891e43d6), D(0x3fc9e8e7b0f869e0),
        D(0x3fca73beaa5db18d), D(0x3fcafd3e394558d3), D(0x3fcb856cf060d9f1),
        D(0x3fcc0c5134de1ffc), D(0x3fcc91f1371bc99f), D(0x3fcd1652ffcd3f53),
        D(0x3fcd997c6f635e75), D(0x3fce1b733ab90f3b), D(0x3fce9c3ceadac856),
        D(0x3fcf1bdeec43a305), D(0x3fcf9a5e7a5fa3fe), D(0x3fd00be05ac02f2b),
        D(0x3fd04a054d81a2d4), D(0x3fd087a0835957fb), D(0x3fd0c4b457099517),
        D(0x3fd101431aa1fe51), D(0x3fd13d4f08b98dd8), D(0x3fd178da53edb892),
        D(0x3fd1b3e71e9f9d58), D(0x3fd1ee777defdeed), D(0x3fd2288d7b48e23b),
        D(0x3fd2622b0f52e49f), D(0x3fd29b522a4c6314), D(0x3fd2d404b0e30f80),
        D(0x3fd30c4478f3fbe5), D(0x3fd34413509f7915),
    ];
    static readonly double[] Log10B = [
        D(0x3fdbcb7b15c5a2f8), D(0xbfcbcbb1dbb88eba), D(0x3fc2871c39d521c6),
    ];
    static readonly double[] Log10C = [
        D(0x3fdbcb7b1526e50e), D(0xbfcbcb7b1526e53d), D(0x3fc287a7636f3fa2),
        D(0xbfbbcb7b146a14b3), D(0x3fb63c627d5219cb), D(0xbfb2880736c8762d),
        D(0x3fafc1ecf913961a),
    ];
    // st[]: the exact powers of ten as floats (0 for the unused slots).
    static readonly float[] Log10St = [1.0f, 10.0f, 100.0f, 1000.0f, 10000.0f, 100000.0f, 1000000.0f, 10000000.0f, 100000000.0f, 1000000000.0f, 10000000000.0f, 0f, 0f, 0f, 0f, 0f];
    static readonly (double InvC, double LogC)[] PowfLog2Table = [
        (D(0x3ff661ec79f8f3be), D(0xbfdefec65b963019)),
        (D(0x3ff571ed4aaf883d), D(0xbfdb0b6832d4fca4)),
        (D(0x3ff49539f0f010b0), D(0xbfd7418b0a1fb77b)),
        (D(0x3ff3c995b0b80385), D(0xbfd39de91a6dcf7b)),
        (D(0x3ff30d190c8864a5), D(0xbfd01d9bf3f2b631)),
        (D(0x3ff25e227b0b8ea0), D(0xbfc97c1d1b3b7af0)),
        (D(0x3ff1bb4a4a1a343f), D(0xbfc2f9e393af3c9f)),
        (D(0x3ff12358f08ae5ba), D(0xbfb960cbbf788d5c)),
        (D(0x3ff0953f419900a7), D(0xbfaa6f9db6475fce)),
        (D(0x3ff0000000000000), D(0x0000000000000000)),
        (D(0x3fee608cfd9a47ac), D(0x3fb338ca9f24f53d)),
        (D(0x3feca4b31f026aa0), D(0x3fc476a9543891ba)),
        (D(0x3feb2036576afce6), D(0x3fce840b4ac4e4d2)),
        (D(0x3fe9c2d163a1aa2d), D(0x3fd40645f0c6651c)),
        (D(0x3fe886e6037841ed), D(0x3fd88e9c2c1b9ff8)),
        (D(0x3fe767dcf5534862), D(0x3fdce0a44eb17bcc)),
    ];
    static readonly double[] PowfLog2Poly = [D(0x3fd27616c9496e0b), D(0xbfd71969a075c67a), D(0x3fdec70a6ca7badd), D(0xbfe7154748bef6c8), D(0x3ff71547652ab82b)];


    static uint FloatBits(float f) => BitConverter.SingleToUInt32Bits(f);

    public static float Log10f(float x)
    {
        var ux = AsUInt(x);
        if (ux < (1u << 23) || ux >= 0x7f800000u)
        {
            if (ux == 0 || ux >= 0x7f800000u)
            {
                if (ux == 0x7f800000u)
                    return x;
                var ax = ux << 1;
                if (ax == 0u)
                    return float.NegativeInfinity;
                if (ax > 0xff000000u)
                    return x + x;
                return float.NaN;
            }
            // Subnormal: normalized.
            var n = System.Numerics.BitOperations.LeadingZeroCount(ux) - 8;
            ux <<= n;
            ux -= (uint)(n << 23);
        }
        uint m = ux & ((1u << 23) - 1), j = (m + (1u << (23 - 7))) >> (23 - 6);
        double ix = Log10TR[j], l = Log10TL[j];
        var e = ((int)ux >> 23) - 127;
        var je = (uint)(e + 1);
        je = unchecked(je * 0x4d104d4u) >> 28;
        if (ux == FloatBits(Log10St[je]))
            return je;
        var tz = AsDouble(((ulong)m | (1023ul << 23)) << (52 - 23));
        double z = tz * ix - 1, z2 = z * z;
        var r = ((e * D(0x3fd34413509f79ff) + l) + z * Log10B[0]) + z2 * (Log10B[1] + z * Log10B[2]);
        float ub = (float)r, lb = (float)(r + D(0x3ddb008000000000));
        if (ub != lb)
        {
            var c = Log10C;
            var f = z * ((c[0] + z * c[1]) + z2 * ((c[2] + z * c[3]) + z2 * (c[4] + z * c[5] + z2 * c[6])));
            f -= D(0x3c90cee0ed4ca7e9) * e;
            f += l - Log10TL[0];
            var el = e * D(0x3fd34413509f7a00);
            r = el + f;
            ub = (float)r;
            tz = r;
            if ((AsULong(tz) & ((1ul << 28) - 1)) == 0)
            {
                var dr = (el - r) + f;
                r += dr * 32;
                ub = (float)r;
            }
        }
        return ub;
    }

    // ---------- powf: glibc 2.43's (sysdeps/ieee754/flt-32/e_powf.c, fused as its FMA variant) ----------

    static int CheckInt(uint iy)
    {
        var e = (int)(iy >> 23 & 0xff);
        if (e < 0x7f)
            return 0;
        if (e > 0x7f + 23)
            return 2;
        if ((iy & ((1u << (0x7f + 23 - e)) - 1)) != 0)
            return 0;
        if ((iy & (1u << (0x7f + 23 - e))) != 0)
            return 1;
        return 2;
    }

    static bool ZeroInfNan(uint ix) => 2 * ix - 1 >= 2u * 0x7f800000 - 1;

    static double PowfLog2(uint ix)
    {
        var tmp = ix - 0x3f330000;
        var i = (int)((tmp >> (23 - 4)) % 16);
        var top = tmp & 0xff800000;
        var iz = ix - top;
        var k = (int)top >> 23;
        var (invc, logc) = PowfLog2Table[i];
        double z = AsFloat(iz);
        var a = PowfLog2Poly;
        var r = Math.FusedMultiplyAdd(z, invc, -1);
        var y0 = logc + k;
        var r2 = r * r;
        var y = Math.FusedMultiplyAdd(a[0], r, a[1]);
        var p = Math.FusedMultiplyAdd(a[2], r, a[3]);
        var r4 = r2 * r2;
        var q = Math.FusedMultiplyAdd(a[4], r, y0);
        q = Math.FusedMultiplyAdd(p, r2, q);
        y = Math.FusedMultiplyAdd(y, r4, q);
        return y;
    }

    const uint SignBias = 1u << (5 + 11);

    static double PowfExp2(double xd, uint signBias)
    {
        const double Shift = 211106232532992.0; // 0x1.8p+52 / 32
        var kd = xd + Shift;
        var ki = AsULong(kd);
        kd -= Shift;
        var r = xd - kd;
        var t = Exp2Table[ki % 32];
        var ski = ki + signBias;
        t += ski << (52 - 5);
        var s = AsDouble(t);
        var z = Math.FusedMultiplyAdd(Exp2C0, r, Exp2C1);
        var r2 = r * r;
        var y = Math.FusedMultiplyAdd(Exp2C2, r, 1);
        y = Math.FusedMultiplyAdd(z, r2, y);
        return y * s;
    }

    public static float Powf(float x, float y)
    {
        uint signBias = 0;
        uint ix = AsUInt(x), iy = AsUInt(y);
        if (ix - 0x00800000 >= 0x7f800000 - 0x00800000 || ZeroInfNan(iy))
        {
            if (ZeroInfNan(iy))
            {
                if (2 * iy == 0)
                    return 1.0f;
                if (ix == 0x3f800000)
                    return 1.0f;
                if (2 * ix > 2u * 0x7f800000 || 2 * iy > 2u * 0x7f800000)
                    return x + y;
                if (2 * ix == 2 * 0x3f800000)
                    return 1.0f;
                if ((2 * ix < 2 * 0x3f800000) == ((iy & 0x80000000) == 0))
                    return 0.0f;
                return y * y;
            }
            if (ZeroInfNan(ix))
            {
                var x2 = x * x;
                if ((ix & 0x80000000) != 0 && CheckInt(iy) == 1)
                    x2 = -x2;
                return (iy & 0x80000000) != 0 ? 1 / x2 : x2;
            }
            if ((ix & 0x80000000) != 0)
            {
                var yint = CheckInt(iy);
                if (yint == 0)
                    return float.NaN;
                if (yint == 1)
                    signBias = SignBias;
                ix &= 0x7fffffff;
            }
            if (ix < 0x00800000)
            {
                ix = AsUInt(x * 8388608.0f) & 0x7fffffff;
                ix -= 23 << 23;
            }
        }
        var ylogx = y * PowfLog2(ix);
        if ((AsULong(ylogx) >> 47 & 0xffff) >= AsULong(126.0) >> 47)
        {
            var negative = signBias != 0;
            if (ylogx <= -150.0)
                return negative ? -0.0f : 0.0f;
            if (ylogx > D(0x405fffffffa3aae2))
            {
                if (ylogx > D(0x405fffffffd1d571))
                    return negative ? float.NegativeInfinity : float.PositiveInfinity;
                // Round to nearest: x^y just under the largest float rounds to it.
                return negative ? -float.MaxValue : float.MaxValue;
            }
        }
        return (float)PowfExp2(ylogx, signBias);
    }
}

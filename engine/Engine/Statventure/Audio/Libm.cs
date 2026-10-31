namespace Statventure.Audio;

/// <summary>
/// The C library's float exp2f and log2f as Chromium on this PC gets them: glibc 2.43
/// (sysdeps/ieee754/flt-32). Blink's oscillator and periodic waves call these (std::exp2, log2f).
/// x86-64 glibc picks variants built with FMA on CPUs that have it; ported with those fused steps,
/// though the unfused SSE2 variants give the same float for every input. Checked on all 2³² floats
/// against the system's glibc (Docs/Audio.md).
/// </summary>
internal static class Libm
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
}

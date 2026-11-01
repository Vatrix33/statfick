namespace Statventure.Audio;

/// <summary>
/// The JavaScript number functions the old music scripts call, as Chromium 152's V8 15.2 computes
/// them (Docs/Audio.md, step 6): <c>Math.exp</c> is LLVM libc's correctly rounded exp; <c>**</c> and
/// <c>Math.pow</c> are V8's math::pow (src/numbers/ieee754.cc: NaN exponents, ±∞ exponents of ±1,
/// x² and √x special cases, then std::pow, glibc's pow); <c>Math.round</c> is V8's (ceiling, then
/// one less if that is more than half away).
/// </summary>
internal static class JsMath
{
    public static double Exp(double x) => LlvmLibc.Exp(x);

    public static double Pow(double x, double y)
    {
        if (double.IsNaN(y))
            return double.NaN;
        if (double.IsInfinity(y) && (x == 1 || x == -1))
            return double.NaN;
        if (double.IsNaN(x))
            x = double.NaN;
        if (y == 2)
            return x * x;
        if (y == 0.5)
            return double.IsInfinity(x) ? double.PositiveInfinity : Math.Sqrt(x + 0);
        return Libm.Pow(x, y);
    }

    public static double Round(double x)
    {
        var up = Math.Ceiling(x);
        return up - 0.5 <= x ? up : up - 1;
    }
}

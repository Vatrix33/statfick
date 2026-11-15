namespace Statventure.UI.Layout;

/// <summary>
/// Blink lays out in LayoutUnits: fixed point with 1/64 px (<c>platform/geometry/layout_unit.h</c>), at every device
/// scale (its lengths are zoomed by the device scale and the page zoom before layout, as the engine's are). Where it turns
/// a float into one, the value is truncated; where it divides one by a whole number, the raw 1/64 count is divided as an
/// integer. Doing the same keeps the engine's boxes on Chromium's grid, so pixel snapping rounds the same way
/// (Docs/Compositing-Port.md, "Layout units").
/// </summary>
public static class LayoutUnit
{
    public const float Epsilon = 1f / 64;

    /// <summary>Truncation toward zero to 1/64 px (LayoutUnit(float)).</summary>
    public static float Truncate(float value) => MathF.Truncate(value * 64) / 64;

    /// <summary>LayoutUnit::FromFloatRound.</summary>
    public static float Round(float value) => MathF.Round(value * 64, MidpointRounding.AwayFromZero) / 64;

    /// <summary>LayoutUnit::FromDoubleRound.</summary>
    public static float Round(double value) => (float)(Math.Round(value * 64, MidpointRounding.AwayFromZero) / 64);

    /// <summary>LayoutUnit::FromFloatCeil.</summary>
    public static float Ceil(float value) => MathF.Ceiling(value * 64) / 64;

    /// <summary>The raw 1/64 count of a LayoutUnit (<c>RawValue()</c>), the value truncated first if it isn't one.</summary>
    public static int Raw(float value) => (int)(value * 64);

    /// <summary>A LayoutUnit from its raw 1/64 count (<c>FromRawValue</c>).</summary>
    public static float FromRaw(long raw) => raw / 64f;

    /// <summary>
    /// A LayoutUnit divided by a whole number (<c>operator/(LayoutUnit, int)</c>): the raw 1/64 count divided as integers,
    /// so the quotient is truncated toward zero to a 1/64 step (Blink's <c>free_space / 2</c> of every centring).
    /// </summary>
    public static float Divide(float value, int divisor) => FromRaw(Raw(value) / divisor);

    /// <summary>
    /// A ratio's two terms as LayoutUnits, as Blink makes them for layout (<c>LayoutRatioFromSizeF</c>,
    /// <c>platform/geometry/physical_size.cc</c>): the terms themselves when both are LayoutUnits exactly, 1:1 for equal
    /// terms, else the continued fraction's convergent within a millionth (at most 16 steps), as raw values.
    /// </summary>
    public static System.Numerics.Vector2 Ratio(float width, float height)
    {
        var (w, h) = (Truncate(width), Truncate(height));
        if ((w == width && h == height) || width <= 0 || height <= 0)
            return new(w, h);
        if (width == height)
            return new(1, 1);
        var initial = width / height;
        var x = initial;
        long h0 = 0, h1 = 1, k0 = 1, k1 = 0;
        for (var i = 0; i < 16; i++)
        {
            if (!float.IsFinite(x))
                break;
            var estimate = (float)h1 / k1;
            if (MathF.Abs(initial - estimate) < 0.000001f)
                break;
            var a = (long)MathF.Floor(x);
            long h2 = Math.Min(h1 * a + h0, int.MaxValue), k2 = Math.Min(k1 * a + k0, int.MaxValue);
            if (h2 == int.MaxValue || k2 == int.MaxValue)
                break;
            (h0, k0, h1, k1) = (h1, k1, h2, k2);
            x = 1 / (x - a);
        }
        return h1 == 0 || k1 == 0 ? new(w, h) : new(FromRaw(h1), FromRaw(k1));
    }

    /// <summary>
    /// <c>LayoutUnit::MulDiv</c>: <paramref name="value"/> × <paramref name="multiplier"/> ÷ <paramref name="divisor"/> on
    /// their raw counts in 64-bit integers, truncated (an aspect ratio applied to a size).
    /// </summary>
    public static float MulDiv(float value, float multiplier, float divisor)
    {
        var d = (long)Raw(divisor);
        return d == 0 ? 0 : FromRaw((long)Raw(value) * Raw(multiplier) / d);
    }
}

/// <summary>
/// Blink's LayoutUnitDiffuser (<c>core/layout/geometry/layout_unit_diffuser.h</c>): a non-negative LayoutUnit shared out
/// over a number of buckets, each the quotient and the remainder's 1/64 steps spread across them (a Bresenham walk), so
/// the parts add up to the whole. Flex's <c>space-*</c> distributions, main-axis auto margins and stretched lines.
/// </summary>
public struct LayoutUnitDiffuser
{
    readonly float _base;
    readonly long _dx, _dy;
    long _x, _y, _count;

    public LayoutUnitDiffuser(float size, int buckets)
    {
        var raw = LayoutUnit.Raw(size);
        _base = LayoutUnit.FromRaw(raw / buckets);
        _dx = raw % buckets * 2L;
        _dy = buckets * 2L;
        _x = 0;
        _y = buckets;
        _count = buckets;
    }

    /// <summary>The quotient each bucket gets at least.</summary>
    public readonly float BaseSize => _base;

    /// <summary>The next bucket's share (nothing once every bucket has had one, or for a diffuser of nothing).</summary>
    public float Next()
    {
        if (_count == 0)
            return 0;
        _count--;
        _x += _dx;
        if (_x >= _y)
        {
            _y += _dy;
            return _base + LayoutUnit.Epsilon;
        }
        return _base;
    }
}

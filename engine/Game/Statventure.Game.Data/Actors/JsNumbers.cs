namespace Statventure.Game.Data.Actors;

/// <summary>
/// The JavaScript number steps the sheet's rules need beside step 1's <see cref="Effects.JsValues"/> (whose
/// <c>toNumber</c> and <c>toInteger</c> the rules use; GM2-P2: JavaScript's rounding kept in one small helper), as V8 15.2
/// computes them in Chromium 152, the old game's browser.
/// </summary>
public static class JsNumbers
{
    /// <summary><c>Math.round</c>: halves toward +∞ (V8: the ceiling, then one less if that is more than half away; C#'s <see cref="Math.Round(double)"/> rounds halves to even).</summary>
    public static double Round(double x)
    {
        var up = Math.Ceiling(x);
        return up - 0.5 <= x ? up : up - 1;
    }

    /// <summary><c>value || 0</c>: 0 for the values JavaScript counts false (0, −0 and NaN), else the value.</summary>
    public static double OrZero(double value) => value == 0 || double.IsNaN(value) ? 0 : value;

    /// <summary>
    /// A whole number the old game computes as a double, held in an <c>int</c> field of a record: saturated at the ends of
    /// <c>int</c> (no sheet comes near them in play), NaN as 0.
    /// </summary>
    public static int ToInt(double value) =>
        double.IsNaN(value) ? 0 : value >= int.MaxValue ? int.MaxValue : value <= int.MinValue ? int.MinValue : (int)value;
}

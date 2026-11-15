using System.Globalization;
using System.Text;

namespace Statventure.Game.Data.Effects;

/// <summary>
/// The old game's small value helpers as JavaScript computes them (Docs/Game/Hero.md, "How it works", step 1): its
/// <c>toNumber</c> and <c>toInteger</c> (<c>old-game/scripts/app.js</c> 1618–1625), <c>String.prototype.trim</c> and
/// <c>toLowerCase</c>, so the rules that use them give the old game's results.
/// </summary>
public static class JsValues
{
    /// <summary><c>toNumber(value, fallback)</c> for a number: the value when it is finite, else the fallback.</summary>
    public static double ToNumber(double value, double fallback = 0) => double.IsFinite(value) ? value : fallback;

    /// <summary><c>toInteger(value, fallback)</c> for a number: <c>Math.floor(toNumber(value, fallback))</c>.</summary>
    public static double ToInteger(double value, double fallback = 0) => Math.Floor(ToNumber(value, fallback));

    /// <summary>
    /// Whether <paramref name="c"/> is what JavaScript's <c>trim</c> takes away (ECMAScript's WhiteSpace and
    /// LineTerminator): tab, vertical tab, form feed, U+FEFF, every space separator (Zs), line feed, carriage return,
    /// U+2028 and U+2029. Not U+0085 (NEL), which .NET's <see cref="string.Trim()"/> takes and JavaScript keeps.
    /// </summary>
    public static bool IsWhiteSpace(char c) =>
        c is '\t' or '\v' or '\f' or '\xFEFF' or '\n' or '\r' or '\x2028' or '\x2029' ||
        CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.SpaceSeparator;

    /// <summary><c>text.trim()</c>: <see cref="IsWhiteSpace"/> characters taken off both ends.</summary>
    public static string Trim(string text)
    {
        var start = 0;
        var end = text.Length;
        while (start < end && IsWhiteSpace(text[start]))
            start++;
        while (end > start && IsWhiteSpace(text[end - 1]))
            end--;
        return text[start..end];
    }

    /// <summary>
    /// <c>text.toLowerCase()</c>: Unicode's lower case, with JavaScript's two special mappings that .NET's simple mapping
    /// lacks: U+0130 (İ) becomes "i" and a combining dot above (U+0307), and a capital sigma ending a word (a letter before
    /// it, none after) becomes the final sigma ς (Unicode's Final_Sigma, read here without its case-ignorable characters
    /// such as apostrophes between the letters).
    /// </summary>
    public static string ToLowerCase(string text)
    {
        var lower = new StringBuilder(text.Length);
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (c == '\x0130')
                lower.Append("i\x0307");
            else if (c == 'Σ' && i > 0 && char.IsLetter(text[i - 1]) && (i + 1 == text.Length || !char.IsLetter(text[i + 1])))
                lower.Append('ς');
            else if (char.IsHighSurrogate(c) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
            {
                lower.Append(text.AsSpan(i, 2).ToString().ToLowerInvariant());
                i++;
            }
            else
                lower.Append(char.ToLowerInvariant(c));
        }
        return lower.ToString();
    }
}

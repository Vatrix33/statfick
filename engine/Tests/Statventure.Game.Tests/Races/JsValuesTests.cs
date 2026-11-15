using Statventure.Game.Data.Effects;

namespace Statventure.Game.Tests;

/// <summary>
/// JavaScript's value rules the races' rules use (<see cref="JsValues"/>): <c>toNumber</c>, <c>toInteger</c>, <c>trim</c>
/// (ECMAScript's WhiteSpace and LineTerminator) and <c>toLowerCase</c> (with its special mappings), each as the language
/// defines it; the oracle's cases (names, slugs, spell names' lower case) check them against Chromium too.
/// </summary>
public class JsValuesTests
{
    [Fact]
    public void ToNumberAndToIntegerTakeTheFallbackForWhatIsntFinite()
    {
        Assert.Equal((2.5, 7.0, 7.0, 7.0), (JsValues.ToNumber(2.5, 7), JsValues.ToNumber(double.NaN, 7), JsValues.ToNumber(double.PositiveInfinity, 7), JsValues.ToNumber(double.NegativeInfinity, 7)));
        Assert.Equal(0, JsValues.ToNumber(double.NaN));
        Assert.Equal((2.0, -3.0, 1.0, 0.0), (JsValues.ToInteger(2.9, 1), JsValues.ToInteger(-2.1, 1), JsValues.ToInteger(double.NaN, 1), JsValues.ToInteger(double.NaN)));
    }

    /// <summary>The characters <c>trim</c> takes: tab, VT, FF, LF, CR, U+FEFF, U+2028, U+2029 and every space separator; not NEL (U+0085), U+180E or U+200B.</summary>
    [Fact]
    public void TrimTakesJavaScriptsWhiteSpace()
    {
        int[] taken = [0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x20, 0xA0, 0x1680, 0x2000, 0x2001, 0x2002, 0x2003, 0x2004, 0x2005, 0x2006, 0x2007, 0x2008, 0x2009, 0x200A, 0x2028, 0x2029, 0x202F, 0x205F, 0x3000, 0xFEFF];
        for (var c = 0; c <= 0xFFFF; c++)
            Assert.True(taken.Contains(c) == JsValues.IsWhiteSpace((char)c), $"U+{c:X4}");
        foreach (var c in taken)
            Assert.Equal("a b", JsValues.Trim($"{(char)c}{(char)c}a b{(char)c}"));
        Assert.Equal("\x0085", JsValues.Trim("\x0085"));
        Assert.Equal("", JsValues.Trim(" \t\xFEFF "));
        Assert.Equal("", JsValues.Trim(""));
    }

    /// <summary>
    /// <c>toLowerCase</c>: Unicode's lower case; U+0130 to "i" and U+0307; a capital sigma ending a word to ς, elsewhere
    /// σ; letters outside the BMP too (Deseret).
    /// </summary>
    [Fact]
    public void ToLowerCaseIsJavaScripts()
    {
        Assert.Equal("fire holy", JsValues.ToLowerCase("FIRE Holy"));
        Assert.Equal("i\x0307ce lance", JsValues.ToLowerCase("\x0130ce Lance"));
        Assert.Equal("k", JsValues.ToLowerCase("\x212A"));
        Assert.Equal("οδος σας", JsValues.ToLowerCase("ΟΔΟΣ ΣΑΣ"));
        Assert.Equal("σ", JsValues.ToLowerCase("Σ"));
        Assert.Equal("ας.", JsValues.ToLowerCase("ΑΣ."));
        Assert.Equal("ασα", JsValues.ToLowerCase("ΑΣΑ"));
        Assert.Equal("\U00010428x", JsValues.ToLowerCase("\U00010400X"));
    }
}

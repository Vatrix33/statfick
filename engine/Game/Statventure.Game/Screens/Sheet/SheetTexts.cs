using System.Globalization;
using Statventure.Game.Data.Actors;
using Statventure.Game.Data.Effects;
using Statventure.Game.Data.Races;

namespace Statventure.Game.Screens.Sheet;

/// <summary>
/// The sheet's words and numbers as the old page writes them (<c>old-game/scripts/app.js</c>: <c>formatValue</c>, 1627;
/// <c>updateDerivedValues</c>, 1762–1837; <c>updateRaceHero</c>, 2965–2983): numbers as JavaScript turns them into text
/// (<see cref="Number"/>, <c>String(n)</c>), the EXP card's lines, the race seal's title, and the heading's race and level
/// line (Q30). Checked against the old page's own texts (<c>Reference/old-game/sheet-values.json</c>, <c>texts</c>).
/// </summary>
public static class SheetTexts
{
    /// <summary>
    /// A number as JavaScript's <c>String(n)</c> writes it (ECMA-262 Number::toString): the shortest digits that read back
    /// as the same double, in plain notation from 10⁻⁶ up to below 10²¹ and as "1.5e+21", "1e-7" outside; "NaN",
    /// "Infinity", "-Infinity"; −0 as "0".
    /// </summary>
    public static string Number(double value)
    {
        if (double.IsNaN(value))
            return "NaN";
        if (double.IsInfinity(value))
            return value > 0 ? "Infinity" : "-Infinity";
        if (value == 0)
            return "0";
        var sign = value < 0 ? "-" : "";
        // The shortest round-trip digits and the decimal point's place (value = 0.d1d2…dk × 10^n).
        var (digits, n) = Digits(Math.Abs(value).ToString("R", CultureInfo.InvariantCulture));
        var k = digits.Length;
        string body;
        if (k <= n && n <= 21)
            body = digits + new string('0', n - k);
        else if (0 < n && n <= 21)
            body = digits[..n] + "." + digits[n..];
        else if (-6 < n && n <= 0)
            body = "0." + new string('0', -n) + digits;
        else
        {
            var exponent = n - 1;
            body = digits[..1] + (k > 1 ? "." + digits[1..] : "") + "e" + (exponent >= 0 ? "+" : "-") + Math.Abs(exponent).ToString(CultureInfo.InvariantCulture);
        }
        return sign + body;
    }

    /// <summary>A positive number's round-trip text ("123.45", "1.5E+21", "1E-07") as its significant digits and the decimal point's place.</summary>
    static (string Digits, int Point) Digits(string text)
    {
        var exponent = 0;
        var e = text.IndexOfAny(['E', 'e']);
        if (e >= 0)
        {
            exponent = int.Parse(text[(e + 1)..], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);
            text = text[..e];
        }
        var dot = text.IndexOf('.');
        var whole = dot >= 0 ? text[..dot] : text;
        var fraction = dot >= 0 ? text[(dot + 1)..] : "";
        var all = whole + fraction;
        var point = whole.Length + exponent;
        var leading = 0;
        while (leading < all.Length - 1 && all[leading] == '0')
            leading++;
        all = all[leading..];
        point -= leading;
        return (all.TrimEnd('0') is { Length: > 0 } trimmed ? trimmed : "0", point);
    }

    /// <summary>The old <c>formatValue</c>: rounded to tenths (JavaScript's round), a whole number as <c>String</c>, else with one decimal (<c>toFixed(1)</c>).</summary>
    public static string Value(double value)
    {
        var rounded = JsNumbers.Round(value * 10) / 10;
        return rounded == Math.Floor(rounded) || !double.IsFinite(rounded)
            ? Number(rounded)
            : rounded.ToString("F1", CultureInfo.InvariantCulture);
    }

    /// <summary>The Level card's cap ("CAP 100").</summary>
    public static string Cap(SheetValues values) => $"CAP {values.LevelCap}";

    /// <summary>The EXP card's threshold beside the EXP: "/ 2790 EXP", or "MAX LEVEL" at the cap.</summary>
    public static string Threshold(SheetValues values) => values.AtLevelCap ? "MAX LEVEL" : $"/ {Value(values.ExperienceRequired)} EXP";

    /// <summary>
    /// The EXP card's helper line: "Level cap reached · 100" at the cap; in Dev Mode "Dev-mode level up enabled · 2790 EXP
    /// target"; else "2790 EXP to reach Level 31".
    /// </summary>
    public static string Helper(ActorSheet sheet, SheetValues values, bool devMode) =>
        values.AtLevelCap ? $"Level cap reached · {values.LevelCap}"
        : devMode ? $"Dev-mode level up enabled · {Value(values.ExperienceRequired)} EXP target"
        : $"{Value(values.ExperienceRemaining)} EXP to reach Level {Value(sheet.Level + 1)}";

    /// <summary>The EXP bar's width: "100%" at the cap, else the share as JavaScript writes it ("33.33333333333333%").</summary>
    public static string BarWidth(SheetValues values) => values.AtLevelCap ? "100%" : Number(values.ExperiencePercent) + "%";

    /// <summary>The EXP bar's <c>aria-valuemax</c>: 1 at the cap, else the EXP the level needs.</summary>
    public static string BarMaximum(SheetValues values) => values.AtLevelCap ? "1" : Number(values.ExperienceRequired);

    /// <summary>The EXP bar's <c>aria-valuenow</c>: 1 at the cap, else the EXP up to what the level needs.</summary>
    public static string BarValue(ActorSheet sheet, SheetValues values) => values.AtLevelCap ? "1" : Number(Math.Min(sheet.Experience, values.ExperienceRequired));

    /// <summary>A resource meter's <c>transform</c> (the old page's presentation of HP and Mana): "scaleX(0.7045454545454546)".</summary>
    public static string Meter(double current, double maximum) =>
        "scaleX(" + Number(maximum > 0 ? Math.Min(1, Math.Max(0, current / maximum)) : 0) + ")";

    /// <summary>An attribute card's bar: its current value against 25, from 7 % to 100 %.</summary>
    public static string StatBar(double current) => Number(Math.Min(100, Math.Max(7, Math.Abs(current) / 25 * 100))) + "%";

    /// <summary>The race's name as players see it (trimmed as the old loader trimmed it), "Unknown race" for a race the data doesn't have.</summary>
    public static string RaceName(Race? race) => race is null ? "Unknown race" : JsValues.Trim(race.Name);

    /// <summary>
    /// The race seal's title and label (the old <c>updateRaceHero</c> for a hero with a race and a look): "Human · Race
    /// locked" outside Dev Mode, "Human · Change race" in it; a race the data doesn't have "Race locked" or "Change race".
    /// </summary>
    public static string SealTitle(Race? race, bool devMode)
    {
        var what = devMode ? "Change race" : "Race locked";
        return race is null ? what : $"{RaceName(race)} · {what}";
    }

    /// <summary>The line under the hero's name (Q30): the race and the level, "Human · Level 30".</summary>
    public static string Standing(Race? race, ActorSheet sheet) => $"{RaceName(race)} · Level {Value(sheet.Level)}";
}

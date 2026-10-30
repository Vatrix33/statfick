using System.Globalization;
using System.Text;

namespace Statventure.UI.Style;

/// <summary>
/// An SVG <c>transform</c> attribute as Blink 152 reads it: parsed as SVGTransformList::ParseInternal
/// does (a list that doesn't parse is empty), and mapped onto the CSS <c>transform</c> property as
/// its presentation attribute (SVGTransformList::CssValue). Lengths are user units, which CSS
/// treats as px; the SVG painter takes the zoom back out of the computed transform
/// (TransformHelper::ComputeTransform).
/// </summary>
public static class SvgTransformList
{
    public enum Kind { Matrix, Translate, Scale, Rotate, SkewX, SkewY }

    /// <summary>One function: its kind and its values (matrix a–f; translate x, y; scale x, y; rotate angle, cx, cy; skew angle).</summary>
    public readonly record struct Item(Kind Kind, float[] Values);

    /// <summary>The functions of an attribute value (empty when it is empty or doesn't parse).</summary>
    public static List<Item> Parse(string text)
    {
        var items = new List<Item>();
        var position = 0;
        var delimiter = false;
        while (SkipSpaces(text, ref position))
        {
            delimiter = false;
            if (TypeAt(text, ref position) is not { } kind || !SkipSpaces(text, ref position) || text[position] != '(')
                return [];
            position++;
            var (required, optional) = kind switch
            {
                Kind.Matrix => (6, 0),
                Kind.Translate or Kind.Scale => (1, 1),
                Kind.Rotate => (1, 2),
                _ => (1, 0),
            };
            var arguments = new List<float>(6);
            var trailing = false;
            while (arguments.Count < required + optional)
            {
                if (!ParseNumber(text, ref position, out var value))
                    break;
                arguments.Add(value);
                trailing = false;
                if (arguments.Count == required + optional)
                    break;
                if (SkipSpaces(text, ref position) && text[position] == ',')
                {
                    position++;
                    trailing = true;
                }
            }
            if ((arguments.Count != required && arguments.Count != required + optional) || trailing)
                return [];
            if (!SkipSpaces(text, ref position) || text[position] != ')')
                return [];
            position++;
            items.Add(kind switch
            {
                Kind.Translate => new Item(kind, [arguments[0], arguments.Count > 1 ? arguments[1] : 0]),
                Kind.Scale => new Item(kind, [arguments[0], arguments.Count > 1 ? arguments[1] : arguments[0]]),
                Kind.Rotate => new Item(kind, [arguments[0], arguments.Count > 1 ? arguments[1] : 0, arguments.Count > 1 ? arguments[2] : 0]),
                _ => new Item(kind, arguments.ToArray()),
            });
            if (SkipSpaces(text, ref position) && text[position] == ',')
            {
                position++;
                delimiter = true;
            }
        }
        return delimiter ? [] : items;
    }

    /// <summary>
    /// The CSS value the attribute stands for ("none" for an empty list). A rotation about a point
    /// (Blink's internal three-argument rotate) is written as translate, rotate, translate back,
    /// which is the same matrix.
    /// </summary>
    public static string CssText(List<Item> items)
    {
        if (items.Count == 0)
            return "none";
        var text = new StringBuilder();
        static string N(float v) => v.ToString("0.#########", CultureInfo.InvariantCulture);
        foreach (var item in items)
        {
            if (text.Length > 0)
                text.Append(' ');
            var v = item.Values;
            switch (item.Kind)
            {
                case Kind.Matrix:
                    text.Append($"matrix({N(v[0])}, {N(v[1])}, {N(v[2])}, {N(v[3])}, {N(v[4])}, {N(v[5])})");
                    break;
                case Kind.Translate:
                    text.Append($"translate({N(v[0])}px, {N(v[1])}px)");
                    break;
                case Kind.Scale:
                    text.Append($"scale({N(v[0])}, {N(v[1])})");
                    break;
                case Kind.Rotate when v[1] != 0 || v[2] != 0:
                    text.Append($"translate({N(v[1])}px, {N(v[2])}px) rotate({N(v[0])}deg) translate({N(-v[1])}px, {N(-v[2])}px)");
                    break;
                case Kind.Rotate:
                    text.Append($"rotate({N(v[0])}deg)");
                    break;
                case Kind.SkewX:
                    text.Append($"skewX({N(v[0])}deg)");
                    break;
                case Kind.SkewY:
                    text.Append($"skewY({N(v[0])}deg)");
                    break;
            }
        }
        return text.ToString();
    }

    static Kind? TypeAt(string text, ref int position)
    {
        foreach (var (name, kind) in Names)
            if (string.CompareOrdinal(text, position, name, 0, name.Length) == 0)
            {
                position += name.Length;
                return kind;
            }
        return null;
    }

    static readonly (string Name, Kind Kind)[] Names =
        [("skewX", Kind.SkewX), ("skewY", Kind.SkewY), ("scale", Kind.Scale), ("translate", Kind.Translate), ("rotate", Kind.Rotate), ("matrix", Kind.Matrix)];

    /// <summary>SkipOptionalSVGSpaces: past HTML spaces; whether anything is left.</summary>
    static bool SkipSpaces(string text, ref int position)
    {
        while (position < text.Length && text[position] is ' ' or '\t' or '\n' or '\r' or '\f')
            position++;
        return position < text.Length;
    }

    /// <summary>Blink's GenericParseNumber with leading white space allowed (and no trailing).</summary>
    static bool ParseNumber(string text, ref int position, out float value)
    {
        value = 0;
        var at = position;
        SkipSpaces(text, ref at);
        var start = at;
        if (at < text.Length && text[at] is '+' or '-')
            at++;
        if (at >= text.Length || (!char.IsAsciiDigit(text[at]) && text[at] != '.'))
            return false;
        while (at < text.Length && char.IsAsciiDigit(text[at]))
            at++;
        if (at < text.Length && text[at] == '.')
        {
            at++;
            // There must be at least one digit following the dot.
            if (at >= text.Length || !char.IsAsciiDigit(text[at]))
                return false;
            while (at < text.Length && char.IsAsciiDigit(text[at]))
                at++;
        }
        // An exponent, unless the e starts a unit ("em", "ex").
        if (at + 1 < text.Length && text[at] is 'e' or 'E' && text[at + 1] != 'x' && text[at + 1] != 'm')
        {
            at++;
            if (text[at] is '+' or '-')
                at++;
            if (at >= text.Length || !char.IsAsciiDigit(text[at]))
                return false;
            while (at < text.Length && char.IsAsciiDigit(text[at]))
                at++;
        }
        if (!double.TryParse(text.AsSpan(start, at - start), NumberStyles.Float, CultureInfo.InvariantCulture, out var number) || !double.IsFinite((float)number))
            return false;
        value = (float)number;
        position = at;
        return true;
    }
}

using System.Globalization;
using Statventure.Core;
using Statventure.UI.Css;

namespace Statventure.UI.Style;

/// <summary>
/// Reads CSS component values from a token list (whitespace-skipping cursor): lengths with
/// calc/min/max/clamp, colours (hex, named, rgb/rgba, hsl/hsla, color-mix in srgb), angles, times.
/// </summary>
public sealed class ValueReader
{
    readonly List<CssToken> _tokens;
    int _at;

    public ValueReader(List<CssToken> tokens)
    {
        _tokens = tokens;
    }

    public bool AtEnd
    {
        get
        {
            SkipSpace();
            return _at >= _tokens.Count;
        }
    }

    public int Position
    {
        get => _at;
        set => _at = value;
    }

    public CssToken Peek()
    {
        SkipSpace();
        return _at < _tokens.Count ? _tokens[_at] : CssToken.Eof;
    }

    public CssToken Next()
    {
        SkipSpace();
        return _at < _tokens.Count ? _tokens[_at++] : CssToken.Eof;
    }

    void SkipSpace()
    {
        while (_at < _tokens.Count && _tokens[_at].Type == CssTokenType.Whitespace)
            _at++;
    }

    /// <summary>The rest of the tokens (whitespace kept), consumed.</summary>
    public List<CssToken> Remaining()
    {
        var rest = CssParser.Trim(_tokens.GetRange(_at, _tokens.Count - _at));
        _at = _tokens.Count;
        return rest;
    }

    public bool TryComma()
    {
        if (Peek().Type != CssTokenType.Comma)
            return false;
        _at++;
        return true;
    }

    public bool TryDelim(char c)
    {
        if (!Peek().IsDelim(c))
            return false;
        _at++;
        return true;
    }

    public bool TryIdent(string name)
    {
        if (!Peek().IsIdent(name))
            return false;
        _at++;
        return true;
    }

    public string? TryAnyIdent()
    {
        var token = Peek();
        if (token.Type != CssTokenType.Ident)
            return null;
        _at++;
        return token.Value.ToLowerInvariant();
    }

    /// <summary>The tokens of the function at the cursor (the cursor must be on its Function token), consumed.</summary>
    public List<CssToken> FunctionArguments()
    {
        SkipSpace();
        _at++; // the Function token
        var depth = 1;
        var arguments = new List<CssToken>();
        while (_at < _tokens.Count)
        {
            var token = _tokens[_at++];
            if (token.Type is CssTokenType.Function or CssTokenType.LeftParen)
                depth++;
            else if (token.Type == CssTokenType.RightParen && --depth == 0)
                break;
            arguments.Add(token);
        }
        return CssParser.Trim(arguments);
    }

    public bool TryNumber(out float value)
    {
        var token = Peek();
        if (token.Type == CssTokenType.Number)
        {
            _at++;
            value = (float)token.Number;
            return true;
        }
        if (token.Type == CssTokenType.Function && token.Value is "calc" or "min" or "max" or "clamp")
        {
            var save = _at;
            var arguments = FunctionArguments();
            var term = CalcEvaluator.Evaluate(token.Value, arguments, default);
            if (term is { IsNumber: true } t)
            {
                value = t.Number;
                return true;
            }
            _at = save;
        }
        value = 0;
        return false;
    }

    public bool TryPercentage(out float value)
    {
        var token = Peek();
        if (token.Type == CssTokenType.Percentage)
        {
            _at++;
            value = (float)token.Number;
            return true;
        }
        value = 0;
        return false;
    }

    /// <summary>A length or percentage (unitless 0 allowed), including calc/min/max/clamp.</summary>
    public bool TryLength(in UnitContext context, out Length length, bool allowUnitlessNumbers = false)
    {
        var token = Peek();
        switch (token.Type)
        {
            case CssTokenType.Dimension:
            {
                var pixels = context.Resolve(token.Number, token.Unit);
                if (float.IsNaN(pixels))
                    break;
                _at++;
                length = Length.Px(pixels);
                return true;
            }
            case CssTokenType.Percentage:
                _at++;
                length = Length.Pct((float)token.Number);
                return true;
            case CssTokenType.Number when token.Number == 0 || allowUnitlessNumbers:
                _at++;
                length = Length.Px((float)token.Number);
                return true;
            case CssTokenType.Function when token.Value is "calc" or "min" or "max" or "clamp":
            {
                var save = _at;
                var arguments = FunctionArguments();
                var result = CalcEvaluator.EvaluateLength(token.Value, arguments, context);
                if (result is { } value)
                {
                    length = value;
                    return true;
                }
                _at = save;
                break;
            }
        }
        length = Length.Zero;
        return false;
    }

    /// <summary>A length, or auto / none / min-content / max-content / fit-content.</summary>
    public bool TryLengthOrKeyword(in UnitContext context, out Length length)
    {
        var token = Peek();
        if (token.Type == CssTokenType.Ident)
        {
            Length? keyword = token.Value.ToLowerInvariant() switch
            {
                "auto" => Length.Auto,
                "none" => Length.None,
                "min-content" => Length.MinContent,
                "max-content" => Length.MaxContent,
                "fit-content" => Length.FitContent,
                _ => null,
            };
            if (keyword is { } value)
            {
                _at++;
                length = value;
                return true;
            }
        }
        return TryLength(context, out length);
    }

    public bool TryAngle(out float degrees)
    {
        var token = Peek();
        if (token.Type == CssTokenType.Dimension)
        {
            float? value = token.Unit switch
            {
                "deg" => (float)token.Number,
                "rad" => (float)(token.Number * 180 / Math.PI),
                "grad" => (float)(token.Number * 0.9),
                "turn" => (float)(token.Number * 360),
                _ => null,
            };
            if (value is { } v)
            {
                _at++;
                degrees = v;
                return true;
            }
        }
        if (token.Type == CssTokenType.Number && token.Number == 0)
        {
            _at++;
            degrees = 0;
            return true;
        }
        if (token.Type == CssTokenType.Function && token.Value == "calc")
        {
            var save = _at;
            var term = CalcEvaluator.Evaluate("calc", FunctionArguments(), default);
            if (term is { IsAngle: true } t)
            {
                degrees = t.Number;
                return true;
            }
            _at = save;
        }
        degrees = 0;
        return false;
    }

    public bool TryTime(out float seconds)
    {
        var token = Peek();
        if (token.Type == CssTokenType.Dimension && token.Unit is "s" or "ms")
        {
            _at++;
            seconds = (float)(token.Unit == "ms" ? token.Number / 1000 : token.Number);
            return true;
        }
        if (token.Type == CssTokenType.Function && token.Value == "calc")
        {
            var save = _at;
            var term = CalcEvaluator.Evaluate("calc", FunctionArguments(), default);
            if (term is { IsTime: true } t)
            {
                seconds = t.Number;
                return true;
            }
            _at = save;
        }
        seconds = 0;
        return false;
    }

    /// <summary>A colour; "currentcolor" gives <paramref name="current"/>.</summary>
    public bool TryColor(Color current, out Color color)
    {
        var token = Peek();
        switch (token.Type)
        {
            case CssTokenType.Hash:
                try
                {
                    color = Color.FromHex(token.Value);
                    _at++;
                    return true;
                }
                catch (FormatException)
                {
                    break;
                }
            case CssTokenType.Ident:
            {
                var name = token.Value.ToLowerInvariant();
                if (name == "currentcolor")
                {
                    _at++;
                    color = current;
                    return true;
                }
                if (name == "transparent")
                {
                    _at++;
                    color = Color.Transparent;
                    return true;
                }
                if (NamedColors.Table.TryGetValue(name, out var named))
                {
                    _at++;
                    color = Color.FromHex(named);
                    return true;
                }
                break;
            }
            case CssTokenType.Function when token.Value is "rgb" or "rgba" or "hsl" or "hsla" or "color-mix":
            {
                var save = _at;
                var arguments = FunctionArguments();
                if (ColorFunction(token.Value, arguments, current) is { } value)
                {
                    color = value;
                    return true;
                }
                _at = save;
                break;
            }
        }
        color = default;
        return false;
    }

    static float ChannelValue(CssToken token, float scale) => token.Type switch
    {
        CssTokenType.Percentage => (float)token.Number / 100f,
        CssTokenType.Number => (float)token.Number / scale,
        _ => float.NaN,
    };

    static Color? ColorFunction(string name, List<CssToken> arguments, Color current)
    {
        if (name == "color-mix")
            return ColorMix(arguments, current);
        var values = arguments.Where(t => t.Type is not (CssTokenType.Whitespace or CssTokenType.Comma) && !t.IsDelim('/')).ToList();
        if (values.Count is < 3 or > 4)
            return null;
        var alpha = values.Count == 4 ? Math.Clamp(ChannelValue(values[3], 1f), 0, 1) : 1f;
        if (name is "rgb" or "rgba")
        {
            var r = ChannelValue(values[0], 255);
            var g = ChannelValue(values[1], 255);
            var b = ChannelValue(values[2], 255);
            if (float.IsNaN(r) || float.IsNaN(g) || float.IsNaN(b) || float.IsNaN(alpha))
                return null;
            return new Color(Math.Clamp(r, 0, 1), Math.Clamp(g, 0, 1), Math.Clamp(b, 0, 1), alpha);
        }
        // hsl
        var hue = values[0].Type switch
        {
            CssTokenType.Number => (float)values[0].Number,
            CssTokenType.Dimension when values[0].Unit == "deg" => (float)values[0].Number,
            CssTokenType.Dimension when values[0].Unit == "turn" => (float)values[0].Number * 360,
            CssTokenType.Dimension when values[0].Unit == "rad" => (float)(values[0].Number * 180 / Math.PI),
            _ => float.NaN,
        };
        var s = ChannelValue(values[1], 100);
        var l = ChannelValue(values[2], 100);
        if (float.IsNaN(hue) || float.IsNaN(s) || float.IsNaN(l))
            return null;
        hue = ((hue % 360) + 360) % 360 / 360f;
        float HueToRgb(float p, float q, float t)
        {
            if (t < 0) t += 1;
            if (t > 1) t -= 1;
            if (t < 1f / 6) return p + (q - p) * 6 * t;
            if (t < 1f / 2) return q;
            if (t < 2f / 3) return p + (q - p) * (2f / 3 - t) * 6;
            return p;
        }
        var q2 = l < 0.5f ? l * (1 + s) : l + s - l * s;
        var p2 = 2 * l - q2;
        return new Color(HueToRgb(p2, q2, hue + 1f / 3), HueToRgb(p2, q2, hue), HueToRgb(p2, q2, hue - 1f / 3), alpha);
    }

    /// <summary>color-mix(in srgb, a [p%], b [p%]) (CSS Color 5): premultiplied mix in sRGB.</summary>
    static Color? ColorMix(List<CssToken> arguments, Color current)
    {
        var parts = CssParser.SplitOnCommas(arguments);
        if (parts.Count != 3)
            return null;
        var space = parts[0].Where(t => t.Type == CssTokenType.Ident).Select(t => t.Value.ToLowerInvariant()).ToList();
        if (space.Count < 2 || space[0] != "in" || space[1] is not ("srgb" or "srgb-linear"))
            return null;
        (Color Color, float? Percent)? Part(List<CssToken> tokens)
        {
            var reader = new ValueReader(tokens);
            float? percent = null;
            if (reader.TryPercentage(out var p1))
                percent = p1;
            if (!reader.TryColor(current, out var color))
                return null;
            if (reader.TryPercentage(out var p2))
                percent = p2;
            return (color, percent);
        }
        if (Part(parts[1]) is not { } a || Part(parts[2]) is not { } b)
            return null;
        var pa = a.Percent ?? (b.Percent is { } bp ? 100 - bp : 50);
        var pb = b.Percent ?? 100 - pa;
        var total = pa + pb;
        if (total <= 0)
            return null;
        var alphaMultiplier = total < 100 ? total / 100f : 1f;
        var mixed = Color.Lerp(a.Color, b.Color, pb / total);
        if (space[1] == "srgb-linear")
        {
            static float ToLinear(float c) => c <= 0.04045f ? c / 12.92f : MathF.Pow((c + 0.055f) / 1.055f, 2.4f);
            static float ToGamma(float c) => c <= 0.0031308f ? c * 12.92f : 1.055f * MathF.Pow(c, 1 / 2.4f) - 0.055f;
            var la = new Color(ToLinear(a.Color.R), ToLinear(a.Color.G), ToLinear(a.Color.B), a.Color.A);
            var lb = new Color(ToLinear(b.Color.R), ToLinear(b.Color.G), ToLinear(b.Color.B), b.Color.A);
            var lm = Color.Lerp(la, lb, pb / total);
            mixed = new Color(ToGamma(lm.R), ToGamma(lm.G), ToGamma(lm.B), lm.A);
        }
        return mixed with { A = mixed.A * alphaMultiplier };
    }
}

/// <summary>Evaluates calc(), min(), max() and clamp() (CSS Values 4).</summary>
public static class CalcEvaluator
{
    public enum TermKind
    {
        Number,
        Length,
        Angle,
        Time,
    }

    /// <summary>A term: a number, or pixels + percent, or degrees, or seconds.</summary>
    public readonly record struct Term(TermKind Kind, float Number, float Pixels = 0, float Percent = 0)
    {
        public bool IsNumber => Kind == TermKind.Number;
        public bool IsAngle => Kind == TermKind.Angle;
        public bool IsTime => Kind == TermKind.Time;
    }

    /// <summary>A length from calc/min/max/clamp; min/max/clamp over percentages stay unresolved.</summary>
    public static Length? EvaluateLength(string function, List<CssToken> arguments, in UnitContext context)
    {
        if (function is "min" or "max" or "clamp")
        {
            var parts = CssParser.SplitOnCommas(arguments);
            if (function == "clamp" && parts.Count != 3)
                return null;
            var lengths = new List<Length>();
            foreach (var part in parts)
            {
                var reader = new ValueReader(part);
                if (!reader.TryLength(context, out var length) || !reader.AtEnd)
                {
                    // A bare calc expression without a function wrapper.
                    if (Evaluate("calc", part, context) is { Kind: TermKind.Length } term)
                        length = new Length(term.Pixels, term.Percent);
                    else if (Evaluate("calc", part, context) is { Kind: TermKind.Number, Number: 0 })
                        length = Length.Zero;
                    else
                        return null;
                }
                lengths.Add(length);
            }
            if (lengths.All(l => !l.HasPercent))
            {
                var values = lengths.Select(l => l.Resolve(0)).ToList();
                var value = function switch
                {
                    "min" => values.Min(),
                    "max" => values.Max(),
                    _ => MathF.Max(values[0], MathF.Min(values[1], values[2])),
                };
                return Length.Px(value);
            }
            var kind = function switch { "min" => LengthKind.Min, "max" => LengthKind.Max, _ => LengthKind.Clamp };
            return new Length(0, 0, kind, lengths.ToArray());
        }
        return Evaluate(function, arguments, context) switch
        {
            { Kind: TermKind.Length } t => new Length(t.Pixels, t.Percent),
            { Kind: TermKind.Number, Number: 0 } => Length.Zero,
            _ => null,
        };
    }

    public static Term? Evaluate(string function, List<CssToken> arguments, in UnitContext context)
    {
        if (function is "min" or "max" or "clamp")
        {
            var units = context;
            var parts = CssParser.SplitOnCommas(arguments).Select(p => Evaluate("calc", p, units)).ToList();
            if (parts.Any(p => p is null) || parts.Count == 0)
                return null;
            var terms = parts.Select(p => p!.Value).ToList();
            if (terms.Any(t => t.Percent != 0))
                return null;
            float Value(Term t) => t.Kind == TermKind.Length ? t.Pixels : t.Number;
            var values = terms.Select(Value).ToList();
            var result = function switch
            {
                "min" => values.Min(),
                "max" => values.Max(),
                _ when values.Count == 3 => MathF.Max(values[0], MathF.Min(values[1], values[2])),
                _ => float.NaN,
            };
            if (float.IsNaN(result))
                return null;
            var kind = terms[0].Kind;
            return kind == TermKind.Length ? new Term(kind, 0, result) : new Term(kind, result);
        }
        var tokens = arguments.Where(t => t.Type != CssTokenType.Whitespace || true).ToList();
        var position = 0;
        var context2 = context;
        return Sum(tokens, ref position, context2);
    }

    static void Skip(List<CssToken> tokens, ref int position)
    {
        while (position < tokens.Count && tokens[position].Type == CssTokenType.Whitespace)
            position++;
    }

    static Term? Sum(List<CssToken> tokens, ref int position, UnitContext context)
    {
        var left = Product(tokens, ref position, context);
        if (left is null)
            return null;
        while (true)
        {
            Skip(tokens, ref position);
            if (position >= tokens.Count || !(tokens[position].IsDelim('+') || tokens[position].IsDelim('-')))
                return left;
            var subtract = tokens[position].IsDelim('-');
            position++;
            var right = Product(tokens, ref position, context);
            if (right is null)
                return null;
            var a = left.Value;
            var b = right.Value;
            var sign = subtract ? -1 : 1;
            if (a.Kind == TermKind.Number && b.Kind == TermKind.Number)
                left = new Term(TermKind.Number, a.Number + sign * b.Number);
            else if (a.Kind == TermKind.Length && b.Kind == TermKind.Length)
                left = new Term(TermKind.Length, 0, a.Pixels + sign * b.Pixels, a.Percent + sign * b.Percent);
            else if (a.Kind == b.Kind)
                left = new Term(a.Kind, a.Number + sign * b.Number);
            else
                return null;
        }
    }

    static Term? Product(List<CssToken> tokens, ref int position, UnitContext context)
    {
        var left = Value(tokens, ref position, context);
        if (left is null)
            return null;
        while (true)
        {
            Skip(tokens, ref position);
            if (position >= tokens.Count || !(tokens[position].IsDelim('*') || tokens[position].IsDelim('/')))
                return left;
            var divide = tokens[position].IsDelim('/');
            position++;
            var right = Value(tokens, ref position, context);
            if (right is null)
                return null;
            var a = left.Value;
            var b = right.Value;
            if (divide)
            {
                if (b.Kind != TermKind.Number || b.Number == 0)
                    return null;
                left = Scale(a, 1 / b.Number);
            }
            else if (a.Kind == TermKind.Number)
                left = Scale(b, a.Number);
            else if (b.Kind == TermKind.Number)
                left = Scale(a, b.Number);
            else
                return null;
        }
    }

    static Term Scale(Term t, float factor) => t.Kind == TermKind.Length
        ? new Term(TermKind.Length, 0, t.Pixels * factor, t.Percent * factor)
        : new Term(t.Kind, t.Number * factor);

    static Term? Value(List<CssToken> tokens, ref int position, UnitContext context)
    {
        Skip(tokens, ref position);
        if (position >= tokens.Count)
            return null;
        var token = tokens[position++];
        switch (token.Type)
        {
            case CssTokenType.Number:
                return new Term(TermKind.Number, (float)token.Number);
            case CssTokenType.Percentage:
                return new Term(TermKind.Length, 0, 0, (float)token.Number);
            case CssTokenType.Dimension:
            {
                switch (token.Unit)
                {
                    case "deg": return new Term(TermKind.Angle, (float)token.Number);
                    case "rad": return new Term(TermKind.Angle, (float)(token.Number * 180 / Math.PI));
                    case "turn": return new Term(TermKind.Angle, (float)token.Number * 360);
                    case "grad": return new Term(TermKind.Angle, (float)token.Number * 0.9f);
                    case "s": return new Term(TermKind.Time, (float)token.Number);
                    case "ms": return new Term(TermKind.Time, (float)token.Number / 1000);
                }
                var pixels = context.Resolve(token.Number, token.Unit);
                return float.IsNaN(pixels) ? null : new Term(TermKind.Length, 0, pixels);
            }
            case CssTokenType.LeftParen:
            {
                var inner = new List<CssToken>();
                var depth = 1;
                while (position < tokens.Count)
                {
                    var t = tokens[position++];
                    if (t.Type is CssTokenType.LeftParen or CssTokenType.Function)
                        depth++;
                    else if (t.Type == CssTokenType.RightParen && --depth == 0)
                        break;
                    inner.Add(t);
                }
                var p = 0;
                return Sum(inner, ref p, context);
            }
            case CssTokenType.Function when token.Value is "calc" or "min" or "max" or "clamp":
            {
                var inner = new List<CssToken>();
                var depth = 1;
                while (position < tokens.Count)
                {
                    var t = tokens[position++];
                    if (t.Type is CssTokenType.LeftParen or CssTokenType.Function)
                        depth++;
                    else if (t.Type == CssTokenType.RightParen && --depth == 0)
                        break;
                    inner.Add(t);
                }
                return Evaluate(token.Value, CssParser.Trim(inner), context);
            }
            default:
                return null;
        }
    }
}

public static class NamedColors
{
    public static readonly Dictionary<string, string> Table = new()
    {
        ["aliceblue"] = "#f0f8ff", ["antiquewhite"] = "#faebd7", ["aqua"] = "#00ffff", ["aquamarine"] = "#7fffd4", ["azure"] = "#f0ffff",
        ["beige"] = "#f5f5dc", ["bisque"] = "#ffe4c4", ["black"] = "#000000", ["blanchedalmond"] = "#ffebcd", ["blue"] = "#0000ff",
        ["blueviolet"] = "#8a2be2", ["brown"] = "#a52a2a", ["burlywood"] = "#deb887", ["cadetblue"] = "#5f9ea0", ["chartreuse"] = "#7fff00",
        ["chocolate"] = "#d2691e", ["coral"] = "#ff7f50", ["cornflowerblue"] = "#6495ed", ["cornsilk"] = "#fff8dc", ["crimson"] = "#dc143c",
        ["cyan"] = "#00ffff", ["darkblue"] = "#00008b", ["darkcyan"] = "#008b8b", ["darkgoldenrod"] = "#b8860b", ["darkgray"] = "#a9a9a9",
        ["darkgreen"] = "#006400", ["darkgrey"] = "#a9a9a9", ["darkkhaki"] = "#bdb76b", ["darkmagenta"] = "#8b008b", ["darkolivegreen"] = "#556b2f",
        ["darkorange"] = "#ff8c00", ["darkorchid"] = "#9932cc", ["darkred"] = "#8b0000", ["darksalmon"] = "#e9967a", ["darkseagreen"] = "#8fbc8f",
        ["darkslateblue"] = "#483d8b", ["darkslategray"] = "#2f4f4f", ["darkslategrey"] = "#2f4f4f", ["darkturquoise"] = "#00ced1", ["darkviolet"] = "#9400d3",
        ["deeppink"] = "#ff1493", ["deepskyblue"] = "#00bfff", ["dimgray"] = "#696969", ["dimgrey"] = "#696969", ["dodgerblue"] = "#1e90ff",
        ["firebrick"] = "#b22222", ["floralwhite"] = "#fffaf0", ["forestgreen"] = "#228b22", ["fuchsia"] = "#ff00ff", ["gainsboro"] = "#dcdcdc",
        ["ghostwhite"] = "#f8f8ff", ["gold"] = "#ffd700", ["goldenrod"] = "#daa520", ["gray"] = "#808080", ["green"] = "#008000",
        ["greenyellow"] = "#adff2f", ["grey"] = "#808080", ["honeydew"] = "#f0fff0", ["hotpink"] = "#ff69b4", ["indianred"] = "#cd5c5c",
        ["indigo"] = "#4b0082", ["ivory"] = "#fffff0", ["khaki"] = "#f0e68c", ["lavender"] = "#e6e6fa", ["lavenderblush"] = "#fff0f5",
        ["lawngreen"] = "#7cfc00", ["lemonchiffon"] = "#fffacd", ["lightblue"] = "#add8e6", ["lightcoral"] = "#f08080", ["lightcyan"] = "#e0ffff",
        ["lightgoldenrodyellow"] = "#fafad2", ["lightgray"] = "#d3d3d3", ["lightgreen"] = "#90ee90", ["lightgrey"] = "#d3d3d3", ["lightpink"] = "#ffb6c1",
        ["lightsalmon"] = "#ffa07a", ["lightseagreen"] = "#20b2aa", ["lightskyblue"] = "#87cefa", ["lightslategray"] = "#778899", ["lightslategrey"] = "#778899",
        ["lightsteelblue"] = "#b0c4de", ["lightyellow"] = "#ffffe0", ["lime"] = "#00ff00", ["limegreen"] = "#32cd32", ["linen"] = "#faf0e6",
        ["magenta"] = "#ff00ff", ["maroon"] = "#800000", ["mediumaquamarine"] = "#66cdaa", ["mediumblue"] = "#0000cd", ["mediumorchid"] = "#ba55d3",
        ["mediumpurple"] = "#9370db", ["mediumseagreen"] = "#3cb371", ["mediumslateblue"] = "#7b68ee", ["mediumspringgreen"] = "#00fa9a", ["mediumturquoise"] = "#48d1cc",
        ["mediumvioletred"] = "#c71585", ["midnightblue"] = "#191970", ["mintcream"] = "#f5fffa", ["mistyrose"] = "#ffe4e1", ["moccasin"] = "#ffe4b5",
        ["navajowhite"] = "#ffdead", ["navy"] = "#000080", ["oldlace"] = "#fdf5e6", ["olive"] = "#808000", ["olivedrab"] = "#6b8e23",
        ["orange"] = "#ffa500", ["orangered"] = "#ff4500", ["orchid"] = "#da70d6", ["palegoldenrod"] = "#eee8aa", ["palegreen"] = "#98fb98",
        ["paleturquoise"] = "#afeeee", ["palevioletred"] = "#db7093", ["papayawhip"] = "#ffefd5", ["peachpuff"] = "#ffdab9", ["peru"] = "#cd853f",
        ["pink"] = "#ffc0cb", ["plum"] = "#dda0dd", ["powderblue"] = "#b0e0e6", ["purple"] = "#800080", ["rebeccapurple"] = "#663399",
        ["red"] = "#ff0000", ["rosybrown"] = "#bc8f8f", ["royalblue"] = "#4169e1", ["saddlebrown"] = "#8b4513", ["salmon"] = "#fa8072",
        ["sandybrown"] = "#f4a460", ["seagreen"] = "#2e8b57", ["seashell"] = "#fff5ee", ["sienna"] = "#a0522d", ["silver"] = "#c0c0c0",
        ["skyblue"] = "#87ceeb", ["slateblue"] = "#6a5acd", ["slategray"] = "#708090", ["slategrey"] = "#708090", ["snow"] = "#fffafa",
        ["springgreen"] = "#00ff7f", ["steelblue"] = "#4682b4", ["tan"] = "#d2b48c", ["teal"] = "#008080", ["thistle"] = "#d8bfd8",
        ["tomato"] = "#ff6347", ["turquoise"] = "#40e0d0", ["violet"] = "#ee82ee", ["wheat"] = "#f5deb3", ["white"] = "#ffffff",
        ["whitesmoke"] = "#f5f5f5", ["yellow"] = "#ffff00", ["yellowgreen"] = "#9acd32",
        // CSS system colours as Chromium's light scheme has them.
        ["canvas"] = "#ffffff", ["canvastext"] = "#000000", ["linktext"] = "#0000ee", ["visitedtext"] = "#551a8b",
        ["activetext"] = "#ff0000", ["buttonface"] = "#efefef", ["buttontext"] = "#000000", ["buttonborder"] = "#767676",
        ["field"] = "#ffffff", ["fieldtext"] = "#000000", ["graytext"] = "#808080", ["mark"] = "#ffff00", ["marktext"] = "#000000",
        ["selecteditem"] = "#1967d2", ["selecteditemtext"] = "#ffffff", ["highlight"] = "#0041c6cc", ["highlighttext"] = "#ffffff",
        ["-webkit-focus-ring-color"] = "#e59700",
    };
}

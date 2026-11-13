using System.Globalization;
using Statventure.Core;
using Statventure.UI.Css;

namespace Statventure.UI.Style;

/// <summary>
/// A custom property registered by a @property rule (CSS Properties and Values API 1, as Blink 152's
/// PropertyRegistration::MaybeCreateForDeclaredProperty takes one): its syntax, whether it inherits, and its initial
/// value, computed. The engine registers the syntax today's game stylesheet uses, <c>&lt;color&gt;</c>: a registered
/// colour's value is computed (a colour keyword or function to its colour, <c>currentcolor</c> kept), its computed
/// value is what var() substitutes (Color::SerializeAsCSSColor's text), a value that isn't a colour is invalid at
/// computed-value time (the property then inherits, or takes its initial value when it doesn't inherit), it inherits
/// only when registered so, every element without a value has the initial one, and animations and transitions
/// interpolate it as a colour (<see cref="AnimationEngine"/>). Other syntaxes are refused (<see cref="Validate"/>).
/// </summary>
public sealed class PropertyRegistration
{
    public string Name { get; }
    public bool Inherits { get; }
    /// <summary>The computed initial value as the tokens var() substitutes.</summary>
    public List<CssToken> Initial { get; }

    PropertyRegistration(string name, bool inherits, List<CssToken> initial)
    {
        Name = name;
        Inherits = inherits;
        Initial = initial;
    }

    /// <summary>What a @property rule comes to: its registration, or why there is none and whether Chromium ignores it too.</summary>
    public readonly record struct Validation(PropertyRegistration? Registration, string? Problem, bool ChromiumIgnoresIt);

    /// <summary>
    /// A @property rule as Blink 152 takes it (CSSParserImpl::ConsumePropertyRule, AtRuleDescriptorParser::
    /// ParseAtPropertyDescriptor, PropertyRegistration::ConvertSyntax, ConvertInherits, ConvertInitial): the prelude one
    /// custom property name; the last valid of each descriptor (syntax: a string that parses as a syntax definition;
    /// inherits: true or false; initial-value: tokens, required unless the syntax is universal, parsing for the syntax
    /// and computationally independent), an !important one or one with anything after its value not valid, other
    /// descriptors ignored; the rule invalid without a syntax or inherits or the initial value it needs. An invalid rule
    /// is ignored, as Chromium ignores it. A valid rule whose syntax isn't <c>&lt;color&gt;</c> isn't the engine's.
    /// </summary>
    public static Validation Validate(CssPropertyRule rule)
    {
        if (rule.Name is not { } name)
            return Ignored($"its prelude '{Text(rule.Prelude)}' isn't one custom property name");
        string? syntax = null;
        bool? inherits = null;
        List<CssToken>? initial = null;
        foreach (var descriptor in rule.Descriptors)
        {
            if (descriptor.Important)
                continue;
            var value = CssParser.Trim(descriptor.Value);
            switch (descriptor.Property)
            {
                case "syntax":
                    if (value is [{ Type: CssTokenType.String } text] && SyntaxDefinition.Parse(text.Value) is not null)
                        syntax = text.Value;
                    break;
                case "inherits":
                    if (value is [{ Type: CssTokenType.Ident } keyword] && (keyword.IsIdent("true") || keyword.IsIdent("false")))
                        inherits = keyword.IsIdent("true");
                    break;
                case "initial-value":
                    initial = value;
                    break;
            }
        }
        if (syntax is null)
            return Ignored($"{name} has no valid syntax descriptor");
        if (inherits is null)
            return Ignored($"{name} has no valid inherits descriptor");
        var definition = SyntaxDefinition.Parse(syntax)!;
        if (initial is null && !definition.Universal)
            return Ignored($"{name} has no initial-value");
        if (!definition.IsColor)
            return new(null, $"{name}'s syntax \"{syntax}\" isn't one the engine registers (only \"<color>\")", false);
        if (ComputeColor(initial!) is not { } computed)
            return Ignored($"{name}'s initial-value '{Text(initial!)}' isn't a <color>");
        return new(new PropertyRegistration(name, inherits.Value, computed), null, false);
    }

    static Validation Ignored(string why) => new(null, why, true);

    static string Text(IEnumerable<CssToken> tokens) => string.Concat(tokens.Select(t => t.ToString())).Trim();

    /// <summary>
    /// A registered colour's computed value as the tokens var() substitutes (ComputeRegisteredPropertyValue, then
    /// CSSValue::CssText): currentcolor stays the keyword; a colour function using currentcolor stays as written (Blink
    /// keeps it unresolved); color-mix() gives a colour in its space, <c>color(srgb r g b)</c>; anything else the
    /// engine parses as a colour gives <c>rgb(r, g, b)</c> or <c>rgba(r, g, b, a)</c>. Null when the value isn't a colour
    /// (invalid at computed-value time).
    /// </summary>
    public static List<CssToken>? ComputeColor(List<CssToken> value)
    {
        value = CssParser.Trim(value);
        if (value.Count == 0)
            return null;
        if (value is [{ Type: CssTokenType.Ident } keyword] && keyword.IsIdent("currentcolor"))
            return [new CssToken(CssTokenType.Ident, "currentcolor")];
        var reader = new ValueReader(value);
        if (!reader.TryColor(Color.Black, out var color) || !reader.AtEnd)
            return null;
        if (value.Any(t => t.IsIdent("currentcolor")))
            return value;
        // color-mix() and color() make colours in an RGB space (not legacy colours): color(<space> …).
        var text = value[0] is { Type: CssTokenType.Function, Value: "color-mix" or "color" }
            ? SerializeSrgb(color, value.Any(t => t.IsIdent("srgb-linear")) ? "srgb-linear" : "srgb")
            : SerializeLegacy(color);
        return CssParser.Trim(CssTokenizer.Tokenize(text));
    }

    /// <summary>Color::SerializeLegacyColorAsCSSColor: channels rounded to 0–255; alpha with two decimals when that is exact, else three.</summary>
    public static string SerializeLegacy(Color c) => SerializeLegacy(c.R * 255.0, c.G * 255.0, c.B * 255.0, c.A);

    /// <summary><see cref="SerializeLegacy(Color)"/> of channels in 0–255 (Blink's legacy colour parameters).</summary>
    public static string SerializeLegacy(double r, double g, double b, double a)
    {
        static int Channel(double v) => (int)Math.Round(Math.Clamp(v, 0, 255), MidpointRounding.AwayFromZero);
        if (a >= 1)
            return $"rgb({Channel(r)}, {Channel(g)}, {Channel(b)})";
        var alpha = (int)Math.Clamp(Math.Round(((float)a + 1e-7f) * 255.0, MidpointRounding.AwayFromZero), 0, 255);
        // (Blink keeps the rounded alphas as floats: 0.7 times 255 is 178.5 there.)
        var two = (float)(Math.Round(alpha * 100.0 / 255.0, MidpointRounding.AwayFromZero) / 100.0);
        var text = MathF.Round(two * 255f, MidpointRounding.AwayFromZero) == alpha
            ? Number(two, 2)
            : Number((float)(Math.Round(alpha * 1000.0 / 255.0, MidpointRounding.AwayFromZero) / 1000.0), 3);
        return $"rgba({Channel(r)}, {Channel(g)}, {Channel(b)}, {text})";
    }

    /// <summary>Color::SerializeInternal for a colour in an RGB space: <c>color(&lt;space&gt; r g b[ / a])</c>, six significant digits.</summary>
    static string SerializeSrgb(Color c, string space)
    {
        if (space == "srgb-linear")
            c = new Color(ToLinear(c.R), ToLinear(c.G), ToLinear(c.B), c.A);
        var text = $"color({space} {Number(c.R, 6)} {Number(c.G, 6)} {Number(c.B, 6)}";
        return c.A != 1 ? text + $" / {Number(c.A, 6)})" : text + ")";
    }

    static float ToLinear(float c) => c <= 0.04045f ? c / 12.92f : MathF.Pow((c + 0.055f) / 1.055f, 2.4f);

    /// <summary>StringBuilder::AppendNumber with a precision: that many significant digits, trailing zeros dropped.</summary>
    static string Number(double value, int precision)
    {
        if (value == 0)
            return "0";
        var rounded = double.Parse(value.ToString("G" + precision, CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
        return rounded.ToString("0.##########", CultureInfo.InvariantCulture);
    }
}

/// <summary>
/// A @property syntax definition as Blink 152's CSSSyntaxStringParser reads one: <c>*</c>, or components separated by
/// <c>|</c>, each a data type name in angle brackets (one of fifteen, case-sensitive) or an identifier (not a CSS-wide
/// keyword or "default"), followed by <c>+</c> or <c>#</c> (not after <c>&lt;transform-list&gt;</c>); whitespace
/// around the parts.
/// </summary>
public sealed class SyntaxDefinition
{
    static readonly HashSet<string> TypeNames =
    [
        "length", "number", "percentage", "length-percentage", "color", "image", "url", "integer", "angle", "time",
        "resolution", "transform-function", "transform-list", "custom-ident", "string",
    ];

    /// <summary>The syntax is <c>*</c>.</summary>
    public bool Universal { get; private init; }

    /// <summary>Each component: a data type name (without brackets) or an identifier, and its multiplier ('+', '#' or none).</summary>
    public IReadOnlyList<(string? Type, string? Ident, char Multiplier)> Components { get; private init; } = [];

    /// <summary>Exactly <c>&lt;color&gt;</c>.</summary>
    public bool IsColor => Components is [{ Type: "color", Multiplier: '\0' }];

    public static SyntaxDefinition? Parse(string text)
    {
        var at = 0;
        static bool IsSpace(char c) => c is ' ' or '\t' or '\n' or '\r' or '\f';
        void SkipSpace()
        {
            while (at < text.Length && IsSpace(text[at]))
                at++;
        }
        static bool IsNameStart(char c) => char.IsAsciiLetter(c) || c == '_' || c >= 0x80;
        static bool IsName(char c) => IsNameStart(c) || char.IsAsciiDigit(c) || c == '-';
        SkipSpace();
        if (at >= text.Length)
            return null;
        if (text[at] == '*')
        {
            at++;
            SkipSpace();
            return at >= text.Length ? new SyntaxDefinition { Universal = true } : null;
        }
        var components = new List<(string?, string?, char)>();
        while (true)
        {
            SkipSpace();
            if (at >= text.Length)
                return null;
            string? type = null, ident = null;
            if (text[at] == '<')
            {
                var end = at + 1;
                while (end < text.Length && IsName(text[end]))
                    end++;
                if (end >= text.Length || text[end] != '>' || !TypeNames.Contains(text[(at + 1)..end]))
                    return null;
                type = text[(at + 1)..end];
                at = end + 1;
            }
            else if (IsNameStart(text[at]))
            {
                var end = at;
                while (end < text.Length && IsName(text[end]))
                    end++;
                ident = text[at..end];
                if (ident.ToLowerInvariant() is "initial" or "inherit" or "unset" or "revert" or "revert-layer" or "default")
                    return null;
                at = end;
            }
            else
                return null;
            var multiplier = '\0';
            if (type != "transform-list" && at < text.Length && text[at] is '+' or '#')
                multiplier = text[at++];
            components.Add((type, ident, multiplier));
            SkipSpace();
            if (at >= text.Length)
                break;
            if (text[at] != '|')
                return null;
            at++;
        }
        return new SyntaxDefinition { Components = components };
    }
}

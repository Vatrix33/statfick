using Statventure.UI.Css;

namespace Statventure.UI.Style;

public enum StyleOrigin
{
    UserAgent,
    Author,
}

/// <summary>A declaration ready for the cascade: shorthands expanded, aliases resolved.</summary>
public sealed class PreparedDeclaration
{
    /// <summary>A canonical longhand name, or a custom property ("--name").</summary>
    public required string Property { get; init; }
    public required List<CssToken> Value { get; init; }
    public bool Important { get; init; }
    /// <summary>The value contains var(): substituted when the style is computed.</summary>
    public bool HasVar { get; init; }
    /// <summary>For a longhand of a shorthand that contains var(): the shorthand, expanded after substitution.</summary>
    public string? PendingShorthand { get; init; }
    /// <summary>inherit / initial / unset / revert, if the value is just that keyword.</summary>
    public string? Keyword { get; init; }

    public bool IsCustom => Property.StartsWith("--", StringComparison.Ordinal);
}

/// <summary>
/// A parsed stylesheet: style rules with the media query they sit under, keyframes and font faces.
/// Declarations are prepared once (shorthands expanded, invalid values dropped) when the sheet loads.
/// </summary>
public sealed class Stylesheet
{
    public sealed record Rule(CssStyleRule Source, MediaQuery? Media, List<PreparedDeclaration> Declarations);

    public string Name { get; }
    public StyleOrigin Origin { get; }
    public List<Rule> Rules { get; } = [];
    public List<(CssKeyframesRule Rule, MediaQuery? Media)> Keyframes { get; } = [];
    public List<CssFontFaceRule> FontFaces { get; } = [];
    /// <summary>Properties the engine doesn't know (for diagnostics; the game's styles should have none).</summary>
    public SortedSet<string> UnknownProperties { get; } = [];
    /// <summary>Selectors the engine couldn't parse.</summary>
    public int DroppedRules { get; private set; }

    Stylesheet(string name, StyleOrigin origin)
    {
        Name = name;
        Origin = origin;
    }

    public static Stylesheet Parse(string text, string name = "", StyleOrigin origin = StyleOrigin.Author)
    {
        var sheet = new Stylesheet(name, origin);
        var order = 0;
        var rules = CssParser.ParseStylesheet(text, ref order);
        sheet.Collect(rules, null);
        return sheet;
    }

    void Collect(List<CssRule> rules, MediaQuery? media)
    {
        foreach (var rule in rules)
        {
            switch (rule)
            {
                case CssStyleRule style:
                    Rules.Add(new Rule(style, media, Prepare(style.Declarations, UnknownProperties)));
                    break;
                case CssMediaRule mediaRule:
                    // Nested media rules: the inner query applies (the game doesn't nest them).
                    Collect(mediaRule.Rules, mediaRule.Query);
                    break;
                case CssKeyframesRule keyframes:
                    Keyframes.Add((keyframes, media));
                    break;
                case CssFontFaceRule fontFace:
                    FontFaces.Add(fontFace);
                    break;
            }
        }
    }

    static readonly HashSet<string> CssWideKeywords = ["inherit", "initial", "unset", "revert", "revert-layer"];

    public static bool ContainsVar(List<CssToken> tokens) => tokens.Any(t => t.Type == CssTokenType.Function && t.Value == "var");

    static string? WideKeyword(List<CssToken> value)
    {
        var trimmed = CssParser.Trim(value);
        if (trimmed.Count == 1 && trimmed[0].Type == CssTokenType.Ident)
        {
            var keyword = trimmed[0].Value.ToLowerInvariant();
            if (CssWideKeywords.Contains(keyword))
                return keyword == "revert-layer" ? "revert" : keyword;
        }
        return null;
    }

    /// <summary>A declaration list prepared for the cascade (also used for inline styles and keyframes).</summary>
    public static List<PreparedDeclaration> Prepare(List<CssDeclaration> declarations, ISet<string>? unknown = null)
    {
        var prepared = new List<PreparedDeclaration>();
        foreach (var declaration in declarations)
        {
            if (declaration.Property.StartsWith("--", StringComparison.Ordinal))
            {
                prepared.Add(new PreparedDeclaration
                {
                    Property = declaration.Property,
                    Value = declaration.Value,
                    Important = declaration.Important,
                    HasVar = ContainsVar(declaration.Value),
                    Keyword = WideKeyword(declaration.Value),
                });
                continue;
            }
            var name = Properties.Canonical(declaration.Property);
            var keyword = WideKeyword(declaration.Value);
            var hasVar = ContainsVar(declaration.Value);
            if (Properties.IsShorthand(name))
            {
                var longhands = Properties.LonghandsOf(name);
                if (keyword is not null || hasVar)
                {
                    foreach (var longhand in longhands)
                        prepared.Add(new PreparedDeclaration
                        {
                            Property = longhand,
                            Value = declaration.Value,
                            Important = declaration.Important,
                            HasVar = hasVar,
                            PendingShorthand = keyword is null ? name : null,
                            Keyword = keyword,
                        });
                    continue;
                }
                var expanded = Properties.Expand(name, declaration.Value);
                if (expanded is null || !expanded.All(e => Validate(e.Name, e.Value)))
                    continue;
                foreach (var (longhand, value) in expanded)
                    prepared.Add(new PreparedDeclaration
                    {
                        Property = longhand,
                        Value = value,
                        Important = declaration.Important,
                        Keyword = WideKeyword(value),
                    });
                continue;
            }
            if (Properties.Get(name) is null)
            {
                if (!Properties.IsIgnored(declaration.Property) && !declaration.Property.StartsWith("-webkit-scrollbar"))
                    unknown?.Add(declaration.Property);
                continue;
            }
            if (keyword is null && !hasVar && !Validate(name, declaration.Value))
                continue;
            prepared.Add(new PreparedDeclaration
            {
                Property = name,
                Value = declaration.Value,
                Important = declaration.Important,
                HasVar = hasVar,
                Keyword = keyword,
            });
        }
        return prepared;
    }

    static readonly ComputedStyle ValidationParent = new();

    /// <summary>Does the value parse for the property (with a neutral context, as at parse time)?</summary>
    public static bool Validate(string property, List<CssToken> value)
    {
        if (WideKeyword(value) is not null)
            return true;
        var definition = Properties.Get(property);
        if (definition is null)
            return false;
        var context = new StyleContext
        {
            Units = new UnitContext(16, 16, 1000, 1000),
            Parent = ValidationParent,
            Style = new ComputedStyle(),
        };
        return definition.Parse(new ValueReader(value), context);
    }
}

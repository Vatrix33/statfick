using Statventure.Packs;

namespace Statventure.UI.Css;

/// <summary>
/// What a @font-face rule declares, as the engine honours it (Docs/Assets.md, "UI"; Q24, step 7): the
/// family, the font's data path, the weight range and whether it is italic. The descriptors taken are
/// font-family (a string, or identifiers joined by single spaces; not a generic family or a CSS-wide
/// keyword), src (one <c>url()</c> naming a data path, with an optional <c>format("truetype")</c> or
/// <c>format("opentype")</c>), font-style (normal or italic), font-weight (a whole number from 1 to
/// 1000, normal or bold, or two of them as a range, swapped when the first is larger, as Blink does) and
/// font-display (no effect: the engine loads every face when its sheet is added). Anything else (local(),
/// several sources, other formats, data: urls, oblique, unicode-range, font-stretch, !important ...) is
/// refused, so a face is either honoured as written or the sheet doesn't load.
/// </summary>
public sealed record FontFaceDescriptors(string Family, string Source, int Weight, int WeightMax, bool Italic)
{
    static readonly HashSet<string> GenericFamilies =
    [
        "serif", "sans-serif", "cursive", "fantasy", "monospace", "system-ui", "math", "emoji", "fangsong",
        "ui-serif", "ui-sans-serif", "ui-monospace", "ui-rounded",
    ];

    static readonly HashSet<string> Keywords = ["inherit", "initial", "unset", "revert", "revert-layer", "default"];

    static readonly HashSet<string> Displays = ["auto", "block", "swap", "fallback", "optional"];

    /// <summary>The descriptors of <paramref name="rule"/>; an <see cref="InvalidDataException"/> naming what the engine can't honour.</summary>
    public static FontFaceDescriptors Of(CssFontFaceRule rule) => Of(rule.Declarations);

    /// <inheritdoc cref="Of(CssFontFaceRule)"/>
    public static FontFaceDescriptors Of(IReadOnlyList<CssDeclaration> declarations)
    {
        string? family = null, source = null;
        int weight = 400, weightMax = 400;
        var italic = false;
        foreach (var declaration in declarations)
        {
            if (declaration.Important)
                throw new InvalidDataException($"@font-face: '{declaration.Property}' is !important, which a descriptor can't be");
            var value = CssParser.Trim(declaration.Value);
            switch (declaration.Property)
            {
                case "font-family":
                    family = ReadFamily(value);
                    break;
                case "src":
                    source = ReadSource(value);
                    break;
                case "font-style":
                    italic = ReadStyle(value);
                    break;
                case "font-weight":
                    (weight, weightMax) = ReadWeights(value);
                    break;
                case "font-display":
                    if (value is not [{ Type: CssTokenType.Ident } display] || !Displays.Contains(display.Value.ToLowerInvariant()))
                        throw Invalid(declaration);
                    break;
                default:
                    throw new InvalidDataException($"@font-face: the descriptor '{declaration.Property}' isn't one the engine honours (font-family, src, font-style, font-weight, font-display)");
            }
        }
        if (family is null)
            throw new InvalidDataException("@font-face without a font-family");
        if (source is null)
            throw new InvalidDataException($"@font-face for {family} without a src");
        return new FontFaceDescriptors(family, source, weight, weightMax, italic);
    }

    static InvalidDataException Invalid(CssDeclaration declaration) =>
        new($"@font-face: '{declaration.Property}: {declaration.ValueText}' isn't a value the engine honours");

    static string ReadFamily(List<CssToken> value)
    {
        if (value is [{ Type: CssTokenType.String } quoted])
            return quoted.Value.Length > 0 ? quoted.Value : throw new InvalidDataException("@font-face: font-family is an empty string");
        var names = new List<string>();
        for (var i = 0; i < value.Count; i++)
        {
            var token = value[i];
            if (i % 2 == 1)
            {
                if (token.Type != CssTokenType.Whitespace)
                    throw new InvalidDataException($"@font-face: 'font-family: {Text(value)}' isn't a family name");
                continue;
            }
            if (token.Type != CssTokenType.Ident || Keywords.Contains(token.Value.ToLowerInvariant()))
                throw new InvalidDataException($"@font-face: 'font-family: {Text(value)}' isn't a family name");
            names.Add(token.Value);
        }
        if (names.Count == 0)
            throw new InvalidDataException("@font-face: font-family is empty");
        if (names.Count == 1 && GenericFamilies.Contains(names[0].ToLowerInvariant()))
            throw new InvalidDataException($"@font-face: font-family can't be the generic family {names[0]}");
        return string.Join(' ', names);
    }

    static string ReadSource(List<CssToken> value)
    {
        var parts = CssParser.SplitOnCommas(value);
        if (parts.Count != 1)
            throw new InvalidDataException($"@font-face: 'src: {Text(value)}' names several sources (the engine takes one url())");
        var tokens = parts[0].Where(t => t.Type != CssTokenType.Whitespace).ToList();
        var at = 0;
        string? url = null;
        if (at < tokens.Count && tokens[at].Type == CssTokenType.Url)
            url = tokens[at++].Value;
        else if (at + 2 < tokens.Count && tokens[at] is { Type: CssTokenType.Function, Value: "url" } && tokens[at + 1].Type == CssTokenType.String && tokens[at + 2].Type == CssTokenType.RightParen)
        {
            url = tokens[at + 1].Value;
            at += 3;
        }
        if (url is null)
            throw new InvalidDataException($"@font-face: 'src: {Text(value)}' isn't a url() (the engine has no local() fonts)");
        if (at < tokens.Count)
        {
            if (at + 3 == tokens.Count && tokens[at] is { Type: CssTokenType.Function, Value: "format" }
                && tokens[at + 1] is { Type: CssTokenType.String or CssTokenType.Ident } format && tokens[at + 2].Type == CssTokenType.RightParen)
            {
                if (format.Value.ToLowerInvariant() is not ("truetype" or "opentype"))
                    throw new InvalidDataException($"@font-face: the format '{format.Value}' isn't one the engine reads (truetype, opentype)");
            }
            else
                throw new InvalidDataException($"@font-face: 'src: {Text(value)}' isn't a url() with an optional format()");
        }
        if (url.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("@font-face: a data: url isn't taken as a font (name a font file in the data)");
        if (PackPath.Problem(url) is { } problem)
            throw new InvalidDataException($"@font-face: url(\"{url}\") isn't a data path: {problem}");
        return url;
    }

    static bool ReadStyle(List<CssToken> value)
    {
        if (value is [{ Type: CssTokenType.Ident } style])
            switch (style.Value.ToLowerInvariant())
            {
                case "normal":
                    return false;
                case "italic":
                    return true;
            }
        throw new InvalidDataException($"@font-face: 'font-style: {Text(value)}' isn't one the engine honours (normal, italic)");
    }

    static (int, int) ReadWeights(List<CssToken> value)
    {
        var parts = value.Where(t => t.Type != CssTokenType.Whitespace).ToList();
        if (parts.Count is 1 or 2 && value.Count == 2 * parts.Count - 1)
        {
            var weights = parts.Select(ReadWeight).ToArray();
            if (weights.All(w => w is not null))
            {
                var first = weights[0]!.Value;
                var last = weights[^1]!.Value;
                return (Math.Min(first, last), Math.Max(first, last));
            }
        }
        throw new InvalidDataException($"@font-face: 'font-weight: {Text(value)}' isn't one or two weights (whole numbers from 1 to 1000, normal, bold)");
    }

    static int? ReadWeight(CssToken token)
    {
        if (token.IsIdent("normal"))
            return 400;
        if (token.IsIdent("bold"))
            return 700;
        if (token.Type == CssTokenType.Number && token.Number is >= 1 and <= 1000 && token.Number == Math.Floor(token.Number))
            return (int)token.Number;
        return null;
    }

    static string Text(List<CssToken> tokens) => string.Concat(tokens.Select(t => t.ToString())).Trim();
}

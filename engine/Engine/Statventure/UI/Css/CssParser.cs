namespace Statventure.UI.Css;

public sealed record CssDeclaration(string Property, List<CssToken> Value, bool Important)
{
    public string ValueText => string.Concat(Value.Select(t => t.ToString())).Trim();
}

public abstract record CssRule;

/// <summary>A style rule; <see cref="Order"/> is its position in the cascade (later wins at equal specificity).</summary>
public sealed record CssStyleRule(SelectorList Selectors, List<CssDeclaration> Declarations, int Order) : CssRule
{
    /// <summary>The selector list's tokens, as parsed (what a compiled stylesheet keeps of it).</summary>
    public List<CssToken> SelectorTokens { get; init; } = [];
}

public sealed record CssMediaRule(MediaQuery Query, List<CssRule> Rules) : CssRule;

public sealed record CssKeyframe(float[] Offsets, List<CssDeclaration> Declarations);

public sealed record CssKeyframesRule(string Name, List<CssKeyframe> Frames) : CssRule;

public sealed record CssFontFaceRule(List<CssDeclaration> Declarations) : CssRule;

/// <summary>
/// A @property rule (CSS Properties and Values API): its prelude's tokens (a custom property's name when it is valid)
/// and its descriptors as written. Whether it registers anything is decided by
/// <see cref="Style.PropertyRegistration"/>, as Blink validates the rule after parsing it.
/// </summary>
public sealed record CssPropertyRule(List<CssToken> Prelude, List<CssDeclaration> Descriptors) : CssRule
{
    /// <summary>
    /// The custom property's name: the prelude's one identifier starting with "--", three characters or more
    /// (CSSVariableParser::IsValidVariableName, nothing after it: ConsumeEndOfPreludeForAtRuleWithBlock); null otherwise.
    /// </summary>
    public string? Name => CssParser.Trim(Prelude) is [{ Type: CssTokenType.Ident } name] && name.Value.Length >= 3 && name.Value.StartsWith("--", StringComparison.Ordinal) ? name.Value : null;
}

/// <summary>A media query list: matches when any of its queries does.</summary>
public sealed class MediaQuery
{
    readonly List<List<(string Feature, double Value, string Keyword)>> _queries = [];
    readonly List<bool> _negated = [];
    readonly List<string> _types = [];
    public string Text { get; }

    /// <summary>The query's tokens, as parsed (what a compiled stylesheet keeps of it).</summary>
    public IReadOnlyList<CssToken> Tokens { get; }

    MediaQuery(string text, List<CssToken> tokens)
    {
        Text = text;
        Tokens = tokens;
    }

    public static MediaQuery Parse(List<CssToken> prelude)
    {
        var query = new MediaQuery(string.Concat(prelude.Select(t => t.ToString())).Trim(), [.. prelude]);
        foreach (var part in CssParser.SplitOnCommas(prelude))
        {
            var features = new List<(string, double, string)>();
            var type = "all";
            var negated = false;
            for (var i = 0; i < part.Count; i++)
            {
                var token = part[i];
                if (token.IsIdent("not"))
                    negated = true;
                else if (token.IsIdent("only") || token.IsIdent("and"))
                    continue;
                else if (token.Type == CssTokenType.Ident)
                    type = token.Value.ToLowerInvariant();
                else if (token.Type == CssTokenType.LeftParen)
                {
                    var inner = new List<CssToken>();
                    for (i++; i < part.Count && part[i].Type != CssTokenType.RightParen; i++)
                        if (part[i].Type != CssTokenType.Whitespace)
                            inner.Add(part[i]);
                    if (inner.Count >= 3 && inner[0].Type == CssTokenType.Ident && inner[1].Type == CssTokenType.Colon)
                    {
                        var name = inner[0].Value.ToLowerInvariant();
                        var value = inner[2];
                        features.Add((name, value.Type is CssTokenType.Dimension or CssTokenType.Number ? CssLength.ToPixelsAbsolute(value) : 0, value.Type == CssTokenType.Ident ? value.Value.ToLowerInvariant() : ""));
                    }
                    else if (inner.Count == 1 && inner[0].Type == CssTokenType.Ident)
                        features.Add((inner[0].Value.ToLowerInvariant(), 0, "true"));
                }
            }
            query._queries.Add(features);
            query._negated.Add(negated);
            query._types.Add(type);
        }
        return query;
    }

    public bool Matches(MediaEnvironment environment)
    {
        for (var q = 0; q < _queries.Count; q++)
        {
            var type = _types[q];
            var matches = type is "all" or "screen";
            foreach (var (feature, value, keyword) in _queries[q])
            {
                matches &= feature switch
                {
                    "max-width" => environment.Width <= value,
                    "min-width" => environment.Width >= value,
                    "max-height" => environment.Height <= value,
                    "min-height" => environment.Height >= value,
                    "prefers-reduced-motion" => (keyword == "reduce") == environment.ReducedMotion,
                    "hover" => keyword == "hover",
                    "pointer" => keyword == "fine",
                    "orientation" => (keyword == "landscape") == (environment.Width >= environment.Height),
                    _ => false,
                };
            }
            if (_negated[q])
                matches = !matches;
            if (matches)
                return true;
        }
        return false;
    }
}

/// <summary>
/// What media queries are evaluated against: the viewport in CSS px, the reduced-motion preference,
/// and the zoom (device pixels per CSS px: the screen's scale times the page zoom), which lengths are
/// computed with, and the screen's scale alone (text positioning depends on it).
/// </summary>
public readonly record struct MediaEnvironment(float Width, float Height, bool ReducedMotion = false, float Zoom = 1, float DeviceScale = 1);

/// <summary>
/// What parsing a stylesheet left out, as the UI importer checks it (Docs/Assets.md, "UI"; S7-2): rules
/// whose selectors the engine can't parse, and everything else skipped (at-rules it doesn't handle,
/// declarations and keyframe selectors that don't parse, stray tokens).
/// </summary>
public sealed class CssParseDiagnostics
{
    /// <summary>The selector lists whose rules were dropped, as written.</summary>
    public List<string> DroppedSelectors { get; } = [];

    /// <summary>What else was left out, in words.</summary>
    public List<string> Skipped { get; } = [];
}

/// <summary>Parses stylesheets (CSS Syntax Level 3 §5) into style, media, keyframes and font-face rules.</summary>
public static class CssParser
{
    public static List<CssRule> ParseStylesheet(string text, ref int order) => ParseStylesheet(text, ref order, null);

    /// <summary>Parses a stylesheet, noting in <paramref name="diagnostics"/> what it leaves out.</summary>
    public static List<CssRule> ParseStylesheet(string text, ref int order, CssParseDiagnostics? diagnostics)
    {
        var tokens = CssTokenizer.Tokenize(text);
        var position = 0;
        return ParseRules(tokens, ref position, topLevel: true, ref order, diagnostics);
    }

    static string Text(IEnumerable<CssToken> tokens) => string.Concat(tokens.Select(t => t.ToString())).Trim();

    public static List<CssRule> ParseStylesheet(string text)
    {
        var order = 0;
        return ParseStylesheet(text, ref order);
    }

    static List<CssRule> ParseRules(List<CssToken> tokens, ref int position, bool topLevel, ref int order, CssParseDiagnostics? diagnostics)
    {
        var rules = new List<CssRule>();
        while (position < tokens.Count)
        {
            var token = tokens[position];
            if (token.Type == CssTokenType.Whitespace)
            {
                position++;
                continue;
            }
            if (token.Type == CssTokenType.RightBrace)
            {
                if (!topLevel)
                {
                    position++;
                    return rules;
                }
                position++;
                diagnostics?.Skipped.Add("an unmatched '}'");
                continue;
            }
            if (token.Type == CssTokenType.AtKeyword)
            {
                position++;
                var name = token.Value.ToLowerInvariant();
                var prelude = new List<CssToken>();
                while (position < tokens.Count && tokens[position].Type is not (CssTokenType.LeftBrace or CssTokenType.Semicolon))
                    prelude.Add(tokens[position++]);
                if (position >= tokens.Count || tokens[position].Type == CssTokenType.Semicolon)
                {
                    // @charset only names the file's encoding (the engine reads UTF-8).
                    if (name != "charset" || position >= tokens.Count)
                        diagnostics?.Skipped.Add(position >= tokens.Count ? $"an @{name} rule without a block" : $"the statement @{name} {Text(prelude)}");
                    position++;
                    continue;
                }
                position++; // {
                switch (name)
                {
                    case "media":
                        rules.Add(new CssMediaRule(MediaQuery.Parse(Trim(prelude)), ParseRules(tokens, ref position, topLevel: false, ref order, diagnostics)));
                        break;
                    case "keyframes":
                    case "-webkit-keyframes":
                        rules.Add(ParseKeyframes(string.Concat(Trim(prelude).Select(t => t.Type == CssTokenType.String ? t.Value : t.ToString())), tokens, ref position, diagnostics));
                        break;
                    case "font-face":
                        rules.Add(new CssFontFaceRule(ParseDeclarations(BlockContents(tokens, ref position), diagnostics)));
                        break;
                    case "property":
                        rules.Add(new CssPropertyRule(Trim(prelude), ParseDeclarations(BlockContents(tokens, ref position), diagnostics)));
                        break;
                    default:
                        BlockContents(tokens, ref position);
                        diagnostics?.Skipped.Add($"an @{name} rule (the engine doesn't handle it)");
                        break;
                }
                continue;
            }
            // A qualified (style) rule.
            var selectorTokens = new List<CssToken>();
            while (position < tokens.Count && tokens[position].Type != CssTokenType.LeftBrace)
                selectorTokens.Add(tokens[position++]);
            if (position >= tokens.Count)
            {
                diagnostics?.Skipped.Add($"a rule without a block: {Text(selectorTokens)}");
                break;
            }
            position++;
            var declarations = ParseDeclarations(BlockContents(tokens, ref position), diagnostics);
            var selectors = SelectorParser.ParseList(Trim(selectorTokens));
            if (selectors is not null)
                rules.Add(new CssStyleRule(selectors, declarations, order++) { SelectorTokens = Trim(selectorTokens) });
            else
                diagnostics?.DroppedSelectors.Add(Text(selectorTokens));
        }
        return rules;
    }

    /// <summary>The tokens up to the matching close brace (consumed).</summary>
    static List<CssToken> BlockContents(List<CssToken> tokens, ref int position)
    {
        var depth = 1;
        var contents = new List<CssToken>();
        while (position < tokens.Count)
        {
            var token = tokens[position++];
            if (token.Type == CssTokenType.LeftBrace)
                depth++;
            else if (token.Type == CssTokenType.RightBrace && --depth == 0)
                break;
            contents.Add(token);
        }
        return contents;
    }

    static CssKeyframesRule ParseKeyframes(string name, List<CssToken> tokens, ref int position, CssParseDiagnostics? diagnostics)
    {
        if (name.Trim().Length == 0)
            diagnostics?.Skipped.Add("a @keyframes rule without a name");
        var frames = new List<CssKeyframe>();
        var body = BlockContents(tokens, ref position);
        var at = 0;
        while (at < body.Count)
        {
            var selector = new List<CssToken>();
            while (at < body.Count && body[at].Type != CssTokenType.LeftBrace)
                selector.Add(body[at++]);
            if (at >= body.Count)
            {
                if (Trim(selector).Count > 0)
                    diagnostics?.Skipped.Add($"a keyframe without a block in @keyframes {name.Trim()}: {Text(selector)}");
                break;
            }
            at++;
            var declarations = ParseDeclarations(BlockContents(body, ref at), diagnostics);
            var parts = SplitOnCommas(Trim(selector));
            // Chromium drops a keyframe whose selector list has an invalid part, or an offset outside 0%–100%.
            if (parts.Any(part => KeyframeOffset(part) is not (>= 0 and <= 1)))
                diagnostics?.Skipped.Add($"a keyframe selector that doesn't parse in @keyframes {name.Trim()}: {Text(selector)}");
            var offsets = parts.Select(part =>
            {
                var t = Trim(part);
                if (t.Count == 0)
                    return -1f;
                if (t[0].IsIdent("from"))
                    return 0f;
                if (t[0].IsIdent("to"))
                    return 1f;
                return t[0].Type == CssTokenType.Percentage ? (float)(t[0].Number / 100) : -1f;
            }).Where(o => o >= 0).ToArray();
            if (offsets.Length > 0)
                frames.Add(new CssKeyframe(offsets, declarations));
        }
        return new CssKeyframesRule(name.Trim(), frames);
    }

    /// <summary>A keyframe selector's offset (from = 0, to = 1, a percentage / 100), or null when it isn't one.</summary>
    static double? KeyframeOffset(List<CssToken> part)
    {
        var t = Trim(part);
        if (t.Count != 1)
            return null;
        if (t[0].IsIdent("from"))
            return 0;
        if (t[0].IsIdent("to"))
            return 1;
        return t[0].Type == CssTokenType.Percentage ? t[0].Number / 100 : null;
    }

    /// <summary>Parses "prop: value; prop: value !important" (also used for inline styles).</summary>
    public static List<CssDeclaration> ParseDeclarations(List<CssToken> tokens) => ParseDeclarations(tokens, null);

    /// <summary>Parses declarations, noting in <paramref name="diagnostics"/> the ones that don't parse.</summary>
    public static List<CssDeclaration> ParseDeclarations(List<CssToken> tokens, CssParseDiagnostics? diagnostics)
    {
        var declarations = new List<CssDeclaration>();
        var at = 0;
        while (at < tokens.Count)
        {
            while (at < tokens.Count && tokens[at].Type is CssTokenType.Whitespace or CssTokenType.Semicolon)
                at++;
            if (at >= tokens.Count)
                break;
            var start = at;
            if (tokens[at].Type != CssTokenType.Ident)
            {
                // Skip to the next declaration.
                while (at < tokens.Count && tokens[at].Type != CssTokenType.Semicolon)
                    at++;
                diagnostics?.Skipped.Add($"a declaration that doesn't parse: {Text(tokens.GetRange(start, at - start))}");
                continue;
            }
            var name = tokens[at++].Value;
            while (at < tokens.Count && tokens[at].Type == CssTokenType.Whitespace)
                at++;
            if (at >= tokens.Count || tokens[at].Type != CssTokenType.Colon)
            {
                while (at < tokens.Count && tokens[at].Type != CssTokenType.Semicolon)
                    at++;
                diagnostics?.Skipped.Add($"a declaration that doesn't parse: {Text(tokens.GetRange(start, at - start))}");
                continue;
            }
            at++;
            var value = new List<CssToken>();
            var depth = 0;
            while (at < tokens.Count && !(depth == 0 && tokens[at].Type == CssTokenType.Semicolon))
            {
                var token = tokens[at++];
                if (token.Type is CssTokenType.LeftParen or CssTokenType.Function or CssTokenType.LeftBracket or CssTokenType.LeftBrace)
                    depth++;
                else if (token.Type is CssTokenType.RightParen or CssTokenType.RightBracket or CssTokenType.RightBrace)
                    depth--;
                value.Add(token);
            }
            value = Trim(value);
            var important = false;
            // "! important" at the end.
            for (var i = value.Count - 1; i >= 1; i--)
            {
                if (value[i].Type == CssTokenType.Whitespace)
                    continue;
                if (value[i].IsIdent("important"))
                {
                    var j = i - 1;
                    while (j >= 0 && value[j].Type == CssTokenType.Whitespace)
                        j--;
                    if (j >= 0 && value[j].IsDelim('!'))
                    {
                        important = true;
                        value = Trim(value.GetRange(0, j));
                    }
                }
                break;
            }
            declarations.Add(new CssDeclaration(name.StartsWith("--") ? name : name.ToLowerInvariant(), value, important));
        }
        return declarations;
    }

    public static List<CssDeclaration> ParseInlineStyle(string text) => ParseDeclarations(CssTokenizer.Tokenize(text));

    public static List<CssToken> Trim(List<CssToken> tokens)
    {
        var start = 0;
        var end = tokens.Count;
        while (start < end && tokens[start].Type == CssTokenType.Whitespace)
            start++;
        while (end > start && tokens[end - 1].Type == CssTokenType.Whitespace)
            end--;
        return start == 0 && end == tokens.Count ? tokens : tokens.GetRange(start, end - start);
    }

    /// <summary>Splits at top-level commas (not inside functions or brackets).</summary>
    public static List<List<CssToken>> SplitOnCommas(List<CssToken> tokens)
    {
        var parts = new List<List<CssToken>> { new() };
        var depth = 0;
        foreach (var token in tokens)
        {
            if (token.Type is CssTokenType.LeftParen or CssTokenType.Function or CssTokenType.LeftBracket)
                depth++;
            else if (token.Type is CssTokenType.RightParen or CssTokenType.RightBracket)
                depth--;
            if (depth == 0 && token.Type == CssTokenType.Comma)
            {
                parts.Add([]);
                continue;
            }
            parts[^1].Add(token);
        }
        return parts.Select(Trim).ToList();
    }
}

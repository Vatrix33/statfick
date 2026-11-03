using Statventure.Packs;
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
/// What compiling a stylesheet gave (<see cref="Stylesheet.Compile"/>; Docs/Assets.md, "UI"; Q23, S7-2):
/// the compiled form, what refuses it, what was dropped as Chromium drops it, and the files it names.
/// </summary>
public sealed class StylesheetCompilation
{
    /// <summary>The compiled form of what parsed (also when there are problems).</summary>
    public required StylesheetRecord Record { get; init; }

    /// <summary>What keeps the sheet from compiling, in words; empty when it compiles.</summary>
    public required IReadOnlyList<string> Problems { get; init; }

    /// <summary>Rules dropped as Chromium drops them (selectors of other browsers' pseudo-classes and pseudo-elements), as written.</summary>
    public required IReadOnlyList<string> Dropped { get; init; }

    /// <summary>The data paths its url()s name (data: urls aside), each once, in ordinal order.</summary>
    public required IReadOnlyList<string> Files { get; init; }
}

/// <summary>
/// A parsed stylesheet: style rules with the media query they sit under, keyframes and font faces.
/// Declarations are prepared once (shorthands expanded, invalid values dropped) when the sheet loads.
/// A sheet is parsed from text (<see cref="Parse"/>) or loaded from its compiled form in the game's data
/// (<see cref="Load"/>; Docs/Assets.md, "UI"), which <see cref="Compile"/> makes and checks.
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
    /// <summary>Rules dropped because the engine couldn't parse their selectors (a sheet parsed from text).</summary>
    public int DroppedRules { get; private set; }

    /// <summary>The file system and path a compiled sheet was loaded from (null for one parsed from text): its fonts' problems name it.</summary>
    internal (VirtualFileSystem Files, string Path)? LoadedFrom { get; private set; }

    Stylesheet(string name, StyleOrigin origin)
    {
        Name = name;
        Origin = origin;
    }

    public static Stylesheet Parse(string text, string name = "", StyleOrigin origin = StyleOrigin.Author) => Parse(text, name, origin, null);

    static Stylesheet Parse(string text, string name, StyleOrigin origin, Checks? checks)
    {
        var sheet = new Stylesheet(name, origin);
        var order = 0;
        var diagnostics = checks?.Diagnostics ?? new CssParseDiagnostics();
        var rules = CssParser.ParseStylesheet(text, ref order, diagnostics);
        sheet.DroppedRules = diagnostics.DroppedSelectors.Count;
        sheet.Collect(rules, null, checks);
        return sheet;
    }

    void Collect(List<CssRule> rules, MediaQuery? media, Checks? checks)
    {
        foreach (var rule in rules)
        {
            switch (rule)
            {
                case CssStyleRule style:
                    Rules.Add(new Rule(style, media, Prepare(style.Declarations, UnknownProperties, checks?.Dropped(style.SelectorTokens))));
                    break;
                case CssMediaRule mediaRule:
                    // Nested media rules: the inner query applies (the game doesn't nest them).
                    Collect(mediaRule.Rules, mediaRule.Query, checks);
                    break;
                case CssKeyframesRule keyframes:
                    Keyframes.Add((keyframes, media));
                    if (checks is not null)
                        foreach (var frame in keyframes.Frames)
                            Prepare(frame.Declarations.Where(d => !d.Important).ToList(), null, checks.Dropped($"@keyframes {keyframes.Name}"));
                    break;
                case CssFontFaceRule fontFace:
                    FontFaces.Add(fontFace);
                    // The engine registers every face, whatever the media: one under a media query would be a difference.
                    if (media is not null)
                        checks?.Problems.Add($"a @font-face inside @media {media.Text} (the engine doesn't make fonts conditional)");
                    break;
            }
        }
    }

    /// <summary>What compiling collects beside the sheet.</summary>
    sealed class Checks
    {
        public CssParseDiagnostics Diagnostics { get; } = new();
        public List<string> Problems { get; } = [];

        public Action<CssDeclaration, string> Dropped(List<CssToken> selectors) => Dropped(Text(selectors));

        public Action<CssDeclaration, string> Dropped(string where) =>
            (declaration, why) => Problems.Add($"{why} in {where}: {declaration.Property}: {declaration.ValueText}");
    }

    static string Text(IEnumerable<CssToken> tokens) => string.Concat(tokens.Select(t => t.ToString())).Trim();

    // ---------- Compiled stylesheets (Docs/Assets.md, "UI"; Q23, S7-1–S7-3) ----------

    /// <summary>Other browsers' prefixes: a selector naming a pseudo-class or pseudo-element with one is dropped by Chromium too (S7-2).</summary>
    static readonly string[] OtherBrowsers = ["-moz-", "-ms-", "-o-"];

    /// <summary>
    /// Compiles a stylesheet's text into its record (the UI importer): parsed and prepared as
    /// <see cref="Parse"/> does, and checked (S7-2). Problems are a rule whose selector the engine can't
    /// parse (except one naming another browser's pseudo-class or pseudo-element, which Chromium drops too:
    /// those are dropped and listed in <see cref="StylesheetCompilation.Dropped"/>), an at-rule the engine
    /// doesn't handle (@charset aside), anything else the parser skips (a declaration or keyframe selector
    /// that doesn't parse, a stray brace), an unknown property, a value the engine can't parse for a property
    /// it takes (properties it deliberately ignores are dropped, as parsing drops them), a @font-face it
    /// can't honour (<see cref="FontFaceDescriptors"/>) or one inside @media, and a url() that isn't a
    /// <c>data:</c> url or a data path under the path rule (S7-1), or names a file
    /// <paramref name="fileExists"/> doesn't know.
    /// </summary>
    public static StylesheetCompilation Compile(string text, Func<string, bool>? fileExists = null)
    {
        var checks = new Checks();
        var sheet = Parse(text, "", StyleOrigin.Author, checks);
        var problems = new List<string>();
        var dropped = new List<string>();
        foreach (var selector in checks.Diagnostics.DroppedSelectors)
            if (NamesOtherBrowser(selector))
                dropped.Add(selector);
            else
                problems.Add($"a rule whose selector the engine can't parse: {selector}");
        problems.AddRange(checks.Diagnostics.Skipped);
        problems.AddRange(checks.Problems);
        foreach (var face in sheet.FontFaces)
        {
            try
            {
                FontFaceDescriptors.Of(face);
            }
            catch (InvalidDataException problem)
            {
                problems.Add(problem.Message);
            }
        }
        var files = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var url in sheet.Urls().Distinct())
        {
            if (IsDataUrl(url))
                continue;
            if (PackPath.Problem(url) is { } problem)
                problems.Add($"url(\"{url}\") isn't a data path: {problem}");
            else
            {
                files.Add(url);
                if (fileExists?.Invoke(url) == false)
                    problems.Add($"url(\"{url}\") names no file in the data being packed");
            }
        }
        return new StylesheetCompilation
        {
            Record = sheet.ToRecord(),
            Problems = problems,
            Dropped = dropped,
            Files = [.. files],
        };
    }

    static bool IsDataUrl(string url) => url.StartsWith("data:", StringComparison.OrdinalIgnoreCase);

    /// <summary>Whether a selector names a pseudo-class or pseudo-element with another browser's prefix (":-moz-focusring", "::-ms-clear").</summary>
    static bool NamesOtherBrowser(string selector)
    {
        var tokens = CssTokenizer.Tokenize(selector);
        for (var i = 0; i + 1 < tokens.Count; i++)
        {
            if (tokens[i].Type != CssTokenType.Colon)
                continue;
            // A pseudo-element's "::" is two colons: the second one's next token is the name.
            var name = tokens[i + 1];
            if (name.Type is CssTokenType.Ident or CssTokenType.Function
                && OtherBrowsers.Any(prefix => name.Value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
                return true;
        }
        return false;
    }

    /// <summary>Every url() in the sheet's declarations, keyframes and font faces, as written (an unquoted url token or url("…")).</summary>
    internal IEnumerable<string> Urls()
    {
        IEnumerable<List<CssToken>> values = Rules.SelectMany(r => r.Declarations.Select(d => d.Value))
            .Concat(Keyframes.SelectMany(k => k.Rule.Frames.SelectMany(f => f.Declarations.Select(d => d.Value))))
            .Concat(FontFaces.SelectMany(f => f.Declarations.Select(d => d.Value)));
        foreach (var value in values)
            for (var i = 0; i < value.Count; i++)
            {
                if (value[i].Type == CssTokenType.Url)
                    yield return value[i].Value;
                else if (value[i] is { Type: CssTokenType.Function, Value: "url" })
                {
                    var j = i + 1;
                    while (j < value.Count && value[j].Type == CssTokenType.Whitespace)
                        j++;
                    if (j < value.Count && value[j].Type == CssTokenType.String)
                        yield return value[j].Value;
                }
            }
    }

    /// <summary>The compiled form of the sheet: its rules, keyframes and font faces, each part as its tokens.</summary>
    public StylesheetRecord ToRecord() => new(
        [.. Rules.Select(rule => new StyleRuleRecord(
            CssTokenRecord.From(rule.Source.SelectorTokens),
            [.. rule.Declarations.Select(d => new DeclarationRecord(d.Property, CssTokenRecord.From(d.Value), d.Important, d.PendingShorthand))],
            rule.Media is { } media ? CssTokenRecord.From(media.Tokens) : null))],
        [.. Keyframes.Select(k => new KeyframesRecord(k.Rule.Name,
            [.. k.Rule.Frames.Select(f => new KeyframeRecord(f.Offsets, [.. f.Declarations.Select(Written)]))],
            k.Media is { } media ? CssTokenRecord.From(media.Tokens) : null))],
        [.. FontFaces.Select(f => new FontFaceRecord([.. f.Declarations.Select(Written)]))]);

    static DeclarationRecord Written(CssDeclaration declaration) =>
        new(declaration.Property, CssTokenRecord.From(declaration.Value), declaration.Important);

    /// <summary>
    /// The compiled stylesheet at <paramref name="path"/> in <paramref name="files"/> (<c>ui/styles/game</c>,
    /// made from <c>ui/styles/game.css</c> by the UI importer, or its JSON form in the development folder),
    /// checked as <see cref="FromRecord"/> checks it: a <see cref="DataFileException"/> naming the pack and
    /// entry or the loose file when it isn't a readable stylesheet record or breaks a rule.
    /// </summary>
    public static Stylesheet Load(VirtualFileSystem files, string path)
    {
        var record = files.Load<StylesheetRecord>(path);
        Stylesheet sheet;
        try
        {
            sheet = FromRecord(record, path);
        }
        catch (InvalidDataException problem)
        {
            throw files.Problem(path, $"not a valid stylesheet: {problem.Message}", problem);
        }
        sheet.LoadedFrom = (files, path);
        return sheet;
    }

    /// <summary>
    /// A stylesheet from its compiled form, the same as the text it was compiled from parses to (its rules
    /// keep no unprepared declarations). Its selectors and media queries are parsed from their tokens, and
    /// what the importer checks is checked again (S5-1's rule: a hand-edited form could break it): an
    /// <see cref="InvalidDataException"/> for a token whose fields don't fit its kind, a selector that doesn't
    /// parse, a declaration the cascade can't take (a property that isn't a longhand by its canonical name or
    /// a custom property, a value the engine can't parse, a pending shorthand that isn't the property's or
    /// whose value has no var()), a keyframe offset outside 0–1 or a keyframe declaration that parsing would
    /// drop, a @font-face the engine can't honour, a url() that is neither a data: url nor a data path.
    /// </summary>
    public static Stylesheet FromRecord(StylesheetRecord record, string name = "", StyleOrigin origin = StyleOrigin.Author)
    {
        var sheet = new Stylesheet(name, origin);
        for (var i = 0; i < record.Rules.Count; i++)
        {
            var rule = record.Rules[i];
            var tokens = Tokens(rule.Selectors, $"rule {i + 1}");
            var selectors = SelectorParser.ParseList(CssParser.Trim(tokens))
                ?? throw new InvalidDataException($"rule {i + 1}: the selector '{Text(tokens)}' doesn't parse");
            var where = $"rule {i + 1} ({Text(tokens)})";
            var declarations = rule.Declarations.Select(d => Prepared(d, where)).ToList();
            sheet.Rules.Add(new Rule(new CssStyleRule(selectors, [], i) { SelectorTokens = tokens }, Media(rule.Media, where), declarations));
        }
        foreach (var keyframes in record.Keyframes)
        {
            var where = $"@keyframes {keyframes.Name}";
            var frames = new List<CssKeyframe>();
            foreach (var frame in keyframes.Frames)
            {
                if (frame.Offsets.Count == 0 || frame.Offsets.Any(o => o is not (>= 0 and <= 1)))
                    throw new InvalidDataException($"{where}: a keyframe's offsets [{string.Join(", ", frame.Offsets)}] aren't from 0 to 1");
                var declarations = frame.Declarations.Select(d => AsWritten(d, where)).ToList();
                var refused = new List<string>();
                Prepare(declarations.Where(d => !d.Important).ToList(), null, (d, why) => refused.Add($"{why}: {d.Property}: {d.ValueText}"));
                if (refused.Count > 0)
                    throw new InvalidDataException($"{where}: {refused[0]}");
                frames.Add(new CssKeyframe([.. frame.Offsets], declarations));
            }
            sheet.Keyframes.Add((new CssKeyframesRule(keyframes.Name, frames), Media(keyframes.Media, where)));
        }
        foreach (var face in record.FontFaces)
        {
            var rule = new CssFontFaceRule([.. face.Descriptors.Select(d => AsWritten(d, "@font-face"))]);
            FontFaceDescriptors.Of(rule);
            sheet.FontFaces.Add(rule);
        }
        foreach (var url in sheet.Urls())
            if (!IsDataUrl(url) && PackPath.Problem(url) is { } problem)
                throw new InvalidDataException($"url(\"{url}\") isn't a data path: {problem}");
        return sheet;
    }

    static List<CssToken> Tokens(IReadOnlyList<CssTokenRecord> records, string where)
    {
        try
        {
            return CssTokenRecord.ToTokens(records);
        }
        catch (InvalidDataException problem)
        {
            throw new InvalidDataException($"{where}: {problem.Message}", problem);
        }
    }

    static MediaQuery? Media(IReadOnlyList<CssTokenRecord>? media, string where) =>
        media is null ? null : MediaQuery.Parse(Tokens(media, $"{where}, its media query"));

    static CssDeclaration AsWritten(DeclarationRecord declaration, string where)
    {
        if (declaration.Shorthand is not null)
            throw new InvalidDataException($"{where}: '{declaration.Property}' names a pending shorthand outside a style rule");
        return new CssDeclaration(declaration.Property, Tokens(declaration.Value, where), declaration.Important);
    }

    /// <summary>A style rule's declaration as <see cref="Prepare"/> made it, checked.</summary>
    static PreparedDeclaration Prepared(DeclarationRecord declaration, string where)
    {
        var value = Tokens(declaration.Value, where);
        var property = declaration.Property;
        var hasVar = ContainsVar(value);
        var keyword = WideKeyword(value);
        string Problem(string why) => $"{where}: '{property}: {Text(value)}' {why}";
        if (property.StartsWith("--", StringComparison.Ordinal))
        {
            if (property.Length == 2)
                throw new InvalidDataException(Problem("has no custom property name"));
            if (declaration.Shorthand is not null)
                throw new InvalidDataException(Problem("is a custom property with a pending shorthand"));
        }
        else if (Properties.Get(property) is null || Properties.Canonical(property) != property)
            throw new InvalidDataException(Problem("isn't a property the cascade takes (a longhand by its canonical name)"));
        else if (declaration.Shorthand is { } shorthand)
        {
            if (Properties.Canonical(shorthand) != shorthand || !Properties.LonghandsOf(shorthand).Contains(property))
                throw new InvalidDataException(Problem($"names '{shorthand}', which isn't a shorthand setting it"));
            if (!hasVar)
                throw new InvalidDataException(Problem($"waits for its shorthand '{shorthand}' without a var() to substitute"));
        }
        else if (keyword is null && !hasVar && !Validate(property, value))
            throw new InvalidDataException(Problem("isn't a value the engine can parse"));
        return new PreparedDeclaration
        {
            Property = property,
            Value = value,
            Important = declaration.Important,
            HasVar = hasVar,
            PendingShorthand = declaration.Shorthand,
            Keyword = keyword,
        };
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
    public static List<PreparedDeclaration> Prepare(List<CssDeclaration> declarations, ISet<string>? unknown = null) => Prepare(declarations, unknown, null);

    /// <summary>
    /// Prepares declarations, telling <paramref name="dropped"/> each one left out for an unknown property
    /// or a value the engine can't parse (not those of properties it deliberately ignores).
    /// </summary>
    static List<PreparedDeclaration> Prepare(List<CssDeclaration> declarations, ISet<string>? unknown, Action<CssDeclaration, string>? dropped)
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
                {
                    dropped?.Invoke(declaration, "a value the engine can't parse");
                    continue;
                }
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
                {
                    unknown?.Add(declaration.Property);
                    dropped?.Invoke(declaration, "an unknown property");
                }
                continue;
            }
            if (keyword is null && !hasVar && !Validate(name, declaration.Value))
            {
                dropped?.Invoke(declaration, "a value the engine can't parse");
                continue;
            }
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

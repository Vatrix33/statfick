using Statventure.Packs;
using Statventure.Records;
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
    public static Stylesheet FromRecord(StylesheetRecord record, string name = "", StyleOrigin origin = StyleOrigin.Author) =>
        new RecordReading(null).Read(record, name, origin);

    /// <summary>
    /// Every problem <see cref="FromRecord"/> refuses a compiled stylesheet for, with its words, at the path of
    /// what it names (P20; Docs/Editor.md, step 6): a token of a selector, a media query or a value
    /// (<c>$.rules[0].selectors[2]</c>), a selector that doesn't parse, a declaration's property, pending
    /// shorthand or value, a keyframe's offsets, a @font-face, a url()'s token. A rule, a keyframe and a
    /// @font-face stop at their first problem, a declaration of a rule at its own; the rest is read on.
    /// </summary>
    internal static IReadOnlyList<RecordProblem> ProblemsOf(StylesheetRecord record)
    {
        var problems = new List<RecordProblem>();
        new RecordReading(problems).Read(record, "", StyleOrigin.Author);
        return problems;
    }

    /// <summary>
    /// Reading a compiled stylesheet (<see cref="FromRecord"/>): without a list, its first problem is an
    /// <see cref="InvalidDataException"/>; with one, each problem goes into it at its path and the part it
    /// is in is left out (<see cref="ProblemsOf"/>).
    /// </summary>
    sealed class RecordReading(List<RecordProblem>? problems)
    {
        /// <summary>Ends the part being read after its problem.</summary>
        sealed class Stop : Exception;

        /// <summary>The problem, as what to throw: an <see cref="InvalidDataException"/>, or (reporting) what ends the part.</summary>
        Exception Fail(RecordPath path, string message, Exception? inner = null)
        {
            if (problems == null)
                return new InvalidDataException(message, inner);
            problems.Add(new(path, message));
            return new Stop();
        }

        void Part(Action read)
        {
            if (problems == null)
            {
                read();
                return;
            }
            try
            {
                read();
            }
            catch (Stop)
            {
            }
        }

        public Stylesheet Read(StylesheetRecord record, string name, StyleOrigin origin)
        {
            var sheet = new Stylesheet(name, origin);
            for (var i = 0; i < record.Rules.Count; i++)
            {
                var rule = record.Rules[i];
                var at = RecordPath.Root.Field("rules").Index(i);
                var index = i;
                Part(() =>
                {
                    var tokens = Tokens(rule.Selectors, $"rule {index + 1}", at.Field("selectors"));
                    var selectors = SelectorParser.ParseList(CssParser.Trim(tokens))
                        ?? throw Fail(at.Field("selectors"), $"rule {index + 1}: the selector '{Text(tokens)}' doesn't parse");
                    var where = $"rule {index + 1} ({Text(tokens)})";
                    var declarations = new List<PreparedDeclaration>();
                    for (var k = 0; k < rule.Declarations.Count; k++)
                    {
                        var declaration = rule.Declarations[k];
                        var path = at.Field("declarations").Index(k);
                        Part(() => declarations.Add(Prepared(declaration, where, path)));
                    }
                    sheet.Rules.Add(new Rule(new CssStyleRule(selectors, [], index) { SelectorTokens = tokens }, Media(rule.Media, where, at.Field("media")), declarations));
                });
            }
            for (var n = 0; n < record.Keyframes.Count; n++)
            {
                var keyframes = record.Keyframes[n];
                var at = RecordPath.Root.Field("keyframes").Index(n);
                Part(() =>
                {
                    var where = $"@keyframes {keyframes.Name}";
                    var frames = new List<CssKeyframe>();
                    for (var m = 0; m < keyframes.Frames.Count; m++)
                    {
                        var frame = keyframes.Frames[m];
                        var path = at.Field("frames").Index(m);
                        Part(() => frames.Add(Keyframe(frame, where, path)));
                    }
                    sheet.Keyframes.Add((new CssKeyframesRule(keyframes.Name, frames), Media(keyframes.Media, where, at.Field("media"))));
                });
            }
            for (var f = 0; f < record.FontFaces.Count; f++)
            {
                var face = record.FontFaces[f];
                var at = RecordPath.Root.Field("fontFaces").Index(f);
                Part(() =>
                {
                    var rule = new CssFontFaceRule([.. face.Descriptors.Select((d, k) => AsWritten(d, "@font-face", at.Field("descriptors").Index(k)))]);
                    try
                    {
                        FontFaceDescriptors.Of(rule);
                    }
                    catch (InvalidDataException problem) when (problems != null)
                    {
                        throw Fail(at, problem.Message);
                    }
                    sheet.FontFaces.Add(rule);
                });
            }
            // The sheet's urls (Urls()'s order and rule), each at its token in the record.
            foreach (var (url, path) in RecordUrls(record))
                if (!IsDataUrl(url) && PackPath.Problem(url) is { } problem)
                    Part(() => throw Fail(path, $"url(\"{url}\") isn't a data path: {problem}"));
            return sheet;
        }

        CssKeyframe Keyframe(KeyframeRecord frame, string where, RecordPath at)
        {
            if (frame.Offsets.Count == 0 || frame.Offsets.Any(o => o is not (>= 0 and <= 1)))
                throw Fail(at.Field("offsets"), $"{where}: a keyframe's offsets [{string.Join(", ", frame.Offsets)}] aren't from 0 to 1");
            var declarations = frame.Declarations.Select((d, k) => AsWritten(d, where, at.Field("declarations").Index(k))).ToList();
            var refused = new List<(CssDeclaration Declaration, string Why)>();
            Prepare(declarations.Where(d => !d.Important).ToList(), null, (d, why) => refused.Add((d, why)));
            if (refused.Count > 0)
            {
                var (declaration, why) = refused[0];
                var field = why == "an unknown property" ? "property" : "value";
                throw Fail(at.Field("declarations").Index(declarations.IndexOf(declaration)).Field(field), $"{where}: {why}: {declaration.Property}: {declaration.ValueText}");
            }
            return new CssKeyframe([.. frame.Offsets], declarations);
        }

        List<CssToken> Tokens(IReadOnlyList<CssTokenRecord> records, string where, RecordPath at)
        {
            var tokens = new List<CssToken>(records.Count);
            for (var j = 0; j < records.Count; j++)
            {
                try
                {
                    tokens.Add(records[j].ToToken());
                }
                catch (InvalidDataException problem)
                {
                    throw Fail(at.Index(j), $"{where}: {problem.Message}", problem);
                }
            }
            return tokens;
        }

        MediaQuery? Media(IReadOnlyList<CssTokenRecord>? media, string where, RecordPath at) =>
            media is null ? null : MediaQuery.Parse(Tokens(media, $"{where}, its media query", at));

        CssDeclaration AsWritten(DeclarationRecord declaration, string where, RecordPath at)
        {
            if (declaration.Shorthand is not null)
                throw Fail(at.Field("shorthand"), $"{where}: '{declaration.Property}' names a pending shorthand outside a style rule");
            return new CssDeclaration(declaration.Property, Tokens(declaration.Value, where, at.Field("value")), declaration.Important);
        }

        /// <summary>A style rule's declaration as <see cref="Prepare"/> made it, checked.</summary>
        PreparedDeclaration Prepared(DeclarationRecord declaration, string where, RecordPath at)
        {
            var value = Tokens(declaration.Value, where, at.Field("value"));
            var property = declaration.Property;
            var hasVar = ContainsVar(value);
            var keyword = WideKeyword(value);
            Exception Problem(string field, string why) => Fail(field.Length == 0 ? at : at.Field(field), $"{where}: '{property}: {Text(value)}' {why}");
            if (property.StartsWith("--", StringComparison.Ordinal))
            {
                if (property.Length == 2)
                    throw Problem("property", "has no custom property name");
                if (declaration.Shorthand is not null)
                    throw Problem("shorthand", "is a custom property with a pending shorthand");
            }
            else if (Properties.Get(property) is null || Properties.Canonical(property) != property)
                throw Problem("property", "isn't a property the cascade takes (a longhand by its canonical name)");
            else if (declaration.Shorthand is { } shorthand)
            {
                if (Properties.Canonical(shorthand) != shorthand || !Properties.LonghandsOf(shorthand).Contains(property))
                    throw Problem("shorthand", $"names '{shorthand}', which isn't a shorthand setting it");
                if (!hasVar)
                    throw Problem("", $"waits for its shorthand '{shorthand}' without a var() to substitute");
            }
            else if (keyword is null && !hasVar && !Validate(property, value))
                throw Problem("value", "isn't a value the engine can parse");
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
    }

    /// <summary>
    /// The url()s of a compiled sheet's declarations and keyframes, in <see cref="Urls"/>' order and by its rule (an
    /// unquoted url token, or url("…")'s string), each with the path of its token in the record. Not a @font-face's:
    /// the one url a face the engine reads can hold is its src, which <see cref="FontFaceDescriptors.Of"/> has checked
    /// by the same rule (a face it refused isn't the sheet's, and isn't reported twice).
    /// </summary>
    static IEnumerable<(string Url, RecordPath Path)> RecordUrls(StylesheetRecord record)
    {
        IEnumerable<(IReadOnlyList<CssTokenRecord> Value, RecordPath Path)> values = record.Rules
            .SelectMany((rule, i) => rule.Declarations.Select((d, k) => (d.Value, RecordPath.Root.Field("rules").Index(i).Field("declarations").Index(k).Field("value"))))
            .Concat(record.Keyframes.SelectMany((keyframes, n) => keyframes.Frames.SelectMany((frame, m) => frame.Declarations.Select((d, k) =>
                (d.Value, RecordPath.Root.Field("keyframes").Index(n).Field("frames").Index(m).Field("declarations").Index(k).Field("value"))))));
        foreach (var (value, path) in values)
            for (var i = 0; i < value.Count; i++)
            {
                if (value[i].Kind == CssTokenKind.Url)
                    yield return (value[i].Text ?? "", path.Index(i));
                else if (value[i] is { Kind: CssTokenKind.Function, Text: "url" })
                {
                    var j = i + 1;
                    while (j < value.Count && value[j].Kind == CssTokenKind.Whitespace)
                        j++;
                    if (j < value.Count && value[j].Kind == CssTokenKind.String)
                        yield return (value[j].Text ?? "", path.Index(j));
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

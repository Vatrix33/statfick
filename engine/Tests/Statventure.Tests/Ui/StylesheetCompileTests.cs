using System.Text;
using Statventure.Packs;
using Statventure.Records;
using Statventure.Tests.Packs;
using Statventure.Tests.Records;
using Statventure.UI;
using Statventure.UI.Css;
using Statventure.UI.Style;

namespace Statventure.Tests.Ui;

/// <summary>
/// Compiled stylesheets (Docs/Assets.md, "UI"; Q23, S7-1–S7-3): the record form (every token kind, both
/// record forms), a sheet loaded from it equal to the sheet its text parses to, what compiling refuses and
/// drops, what loading checks, loading from packs and loose folders, and the UI importer.
/// </summary>
public class StylesheetCompileTests
{
    /// <summary>A sheet with every part and every token kind (at-keywords, semicolons and braces live in a custom property's value).</summary>
    const string EveryKind = """
        @charset "utf-8";
        @font-face { font-family: Lato; src: url("ui/fonts/lato-regular.ttf") format("truetype"); font-weight: 500 600; font-display: swap; }
        @media (max-width: 600px) {
          #menu > .item:hover, a[href^="x"]::before { color: #3a0 !important; margin: var(--gap) 2px; width: calc(50% - 1.5em); z-index: 2; content: "x"; opacity: 0.5; }
        }
        @keyframes pulse { from { opacity: 0 } 50%, to { opacity: 1; transform: scale(-0) } }
        @media (prefers-reduced-motion: reduce) { @keyframes calm { to { opacity: 1 !important; opacity: 0.5 } } }
        .a { --gap: 4px; --odd: @x { a; b }; background: url(ui/images/a.png) no-repeat; border: inherit; }
        """;

    static StylesheetRecord Sample()
    {
        var compiled = Stylesheet.Compile(EveryKind, _ => true);
        Assert.Empty(compiled.Problems);
        return compiled.Record;
    }

    [Fact]
    public void TheRecordFixtureIsRecorded() => RecordFixtures.Ensure("every-kind", Sample());

    static IEnumerable<CssTokenRecord> AllTokens(StylesheetRecord record) =>
        record.Rules.SelectMany(r => r.Selectors.Concat(r.Media ?? []).Concat(r.Declarations.SelectMany(d => d.Value)))
            .Concat(record.Keyframes.SelectMany(k => (k.Media ?? []).Concat(k.Frames.SelectMany(f => f.Declarations.SelectMany(d => d.Value)))))
            .Concat(record.FontFaces.SelectMany(f => f.Descriptors.SelectMany(d => d.Value)));

    [Fact]
    public void TheSampleHoldsEveryTokenKindAndEveryField()
    {
        var record = Sample();
        Assert.Equal(Enum.GetValues<CssTokenKind>().Order(), AllTokens(record).Select(t => t.Kind).Distinct().Order());
        Assert.Contains(record.Rules, r => r.Media is not null);
        Assert.Contains(record.Rules.SelectMany(r => r.Declarations), d => d.Important);
        Assert.Contains(record.Rules.SelectMany(r => r.Declarations), d => d.Shorthand == "margin");
        Assert.Equal(2, record.Keyframes.Count);
        Assert.Contains(record.Keyframes, k => k.Media is not null);
        Assert.Contains(record.Keyframes.SelectMany(k => k.Frames).SelectMany(f => f.Declarations), d => d.Important);
        Assert.Single(record.FontFaces);
    }

    /// <summary>Sheets loaded from their compiled form hold what their text parses to, part for part.</summary>
    [Theory]
    [InlineData("every-kind")]
    [InlineData("user-agent")]
    [InlineData("game")]
    public void ASheetFromItsRecordIsTheSheetItsTextParsesTo(string which)
    {
        var text = which switch
        {
            "every-kind" => EveryKind,
            "user-agent" => UserAgentStyles.Css,
            _ => GameStylesheets.CompilableText,
        };
        var compiled = Stylesheet.Compile(text, _ => true);
        // The engine's own sheet stays embedded and parsed (decision "Embedded stays embedded"); it sets a
        // few properties the engine doesn't know (word-spacing, text-indent): the record holds what parsed all the same.
        if (which == "user-agent")
            Assert.All(compiled.Problems, p => Assert.StartsWith("an unknown property in ", p));
        else
            Assert.Empty(compiled.Problems);
        var loaded = Stylesheet.FromRecord(compiled.Record, "x");
        Assert.Equal(StylesheetText.Of(Stylesheet.Parse(text), unknown: which != "user-agent"), StylesheetText.Of(loaded, unknown: which != "user-agent"));
        Assert.Equal(0, loaded.DroppedRules);
        Assert.All(loaded.Rules, r => Assert.Empty(r.Source.Declarations));
        // And through both record forms.
        var binary = RecordSerializer.ToBinary(compiled.Record);
        var json = RecordSerializer.ToJson(compiled.Record);
        Assert.Equal(binary, RecordSerializer.ToBinary(RecordSerializer.FromBinary<StylesheetRecord>(binary)));
        Assert.Equal(binary, RecordSerializer.ToBinary(RecordSerializer.FromJson<StylesheetRecord>(json)));
        Assert.Equal(StylesheetText.Of(loaded), StylesheetText.Of(Stylesheet.FromRecord(RecordSerializer.FromJson<StylesheetRecord>(json))));
    }

    [Fact]
    public void EveryTokenHasAnExactForm()
    {
        var tokens = CssTokenizer.Tokenize(EveryKind + " .b { width: 1e3px; height: -0px; x: 1.0 7 +3 }").ToList();
        // Tokens the shorthands make (a comma with its text, a number flagged integer).
        tokens.AddRange(Properties.Expand("transition", CssTokenizer.Tokenize("opacity 1s, color 2s"))!.SelectMany(e => e.Value));
        tokens.AddRange(Properties.Expand("flex", CssTokenizer.Tokenize("2"))!.SelectMany(e => e.Value));
        Assert.Contains(tokens, t => t is { Type: CssTokenType.Comma, Value: "," });
        Assert.Contains(tokens, t => t is { Type: CssTokenType.Number, IsInteger: true });
        Assert.Contains(tokens, t => t.Type == CssTokenType.Dimension && double.IsNegative(t.Number));
        foreach (var token in tokens)
        {
            var record = CssTokenRecord.From(token);
            Assert.Equal(token, record.ToToken());
            var again = RecordSerializer.FromJson<StylesheetRecord>(RecordSerializer.ToJson(new StylesheetRecord([new StyleRuleRecord([record], [])], [], [])));
            Assert.Equal(record, again.Rules[0].Selectors[0]);
            Assert.Equal(BitConverter.DoubleToInt64Bits(token.Number), BitConverter.DoubleToInt64Bits(again.Rules[0].Selectors[0].ToToken().Number));
        }
    }

    [Fact]
    public void TokensWithoutAnExactFormAreRefused()
    {
        foreach (var token in new[]
        {
            new CssToken(CssTokenType.Percentage, Number: 5, IsInteger: true),
            new CssToken(CssTokenType.Ident, "a", Number: 1),
            new CssToken(CssTokenType.Ident, "a", Unit: "px"),
            new CssToken(CssTokenType.Ident, "a", HashIsId: true),
            new CssToken(CssTokenType.Number, Number: 1, Unit: "px"),
            new CssToken(CssTokenType.Dimension, Number: 1),
            new CssToken(CssTokenType.EndOfFile),
        })
            Assert.Throws<InvalidOperationException>(() => CssTokenRecord.From(token));
    }

    public static TheoryData<CssTokenRecord, string> UnfitTokens => new()
    {
        { new CssTokenRecord(CssTokenKind.Integer), "without a number" },
        { new CssTokenRecord(CssTokenKind.Percentage), "without a number" },
        { new CssTokenRecord(CssTokenKind.Ident, "a", 1), "with a number" },
        { new CssTokenRecord(CssTokenKind.Number, null, double.NaN), "NaN" },
        { new CssTokenRecord(CssTokenKind.IntegerDimension, null, 1), "without a unit" },
        { new CssTokenRecord(CssTokenKind.Dimension, null, 1, ""), "without a unit" },
        { new CssTokenRecord(CssTokenKind.Number, null, 1, "px"), "with a unit" },
        { new CssTokenRecord(CssTokenKind.Ident, "a", null, "px"), "with a unit" },
        { new CssTokenRecord((CssTokenKind)23), "of kind 23" },
    };

    [Theory]
    [MemberData(nameof(UnfitTokens))]
    public void ATokenThatDoesntFitItsKindIsRefused(CssTokenRecord token, string problem) =>
        Assert.Contains(problem, Assert.Throws<InvalidDataException>(() => token.ToToken()).Message);

    [Fact]
    public void TokenKindsKeepTheirNumbers()
    {
        // The binary form stores a kind by its number, the JSON form by its name: both are the layout's.
        Assert.Equal(Enumerable.Range(0, 23), Enum.GetValues<CssTokenKind>().Select(k => (int)k));
        Assert.Equal("{\n  \"$types\": {\n    \"ui.token\": 1\n  },\n  \"kind\": \"integerDimension\",\n  \"number\": 12,\n  \"unit\": \"px\"\n}\n",
            RecordSerializer.ToJson(CssTokenRecord.From(CssTokenizer.Tokenize("12px")[0])));
    }

    // ---------- Compiling: what refuses a sheet (S7-2) ----------

    public static TheoryData<string, string> Problems => new()
    {
        { ".a:hover-x { color: red }", "a rule whose selector the engine can't parse: .a:hover-x" },
        { "{ color: red }", "a rule whose selector the engine can't parse: " },
        { "@supports (display: grid) { .a { color: red } }", "an @supports rule (the engine doesn't handle it)" },
        { "@container (width > 1px) { .a { color: red } }", "an @container rule" },
        { "@import url(\"x.css\");", "the statement @import url(\"x.css\")" },
        { "@layer base;", "the statement @layer base" },
        { "@font-face;", "the statement @font-face" },
        { ".a { color: red } @media screen", "an @media rule without a block" },
        { ".a { color red }", "a declaration that doesn't parse: color red" },
        { ".a { 12px: red }", "a declaration that doesn't parse: 12px: red" },
        { ".a { color: red; .b { color: blue } }", "a declaration that doesn't parse: .b" },
        { ".a { color: red } }", "an unmatched '}'" },
        { ".a { color: red } .b", "a rule without a block: .b" },
        { ".a { colr: red }", "an unknown property in .a: colr: red" },
        { "@media print { .a, .b { -moz-appearance: none } }", "an unknown property in .a, .b: -moz-appearance: none" },
        { ".a { color: 12px }", "a value the engine can't parse in .a: color: 12px" },
        { ".a { margin: 1px 2px 3px 4px 5px }", "a value the engine can't parse in .a: margin: 1px 2px 3px 4px 5px" },
        { ".a { color: }", "a value the engine can't parse in .a: color: " },
        { "@keyframes k { 0% { colr: red } }", "an unknown property in @keyframes k: colr: red" },
        { "@keyframes k { 0% { opacity: x } }", "a value the engine can't parse in @keyframes k: opacity: x" },
        { "@keyframes k { 0%, foo { opacity: 1 } }", "a keyframe selector that doesn't parse in @keyframes k: 0%, foo" },
        { "@keyframes k { 150% { opacity: 1 } }", "a keyframe selector that doesn't parse in @keyframes k: 150%" },
        { "@keyframes k { -5% { opacity: 1 } }", "a keyframe selector that doesn't parse in @keyframes k: -5%" },
        { "@keyframes k { 10% 20% { opacity: 1 } }", "a keyframe selector that doesn't parse in @keyframes k: 10% 20%" },
        { "@keyframes k { 0% { opacity: 1 } 50% }", "a keyframe without a block in @keyframes k: 50%" },
        { "@keyframes { 0% { opacity: 1 } }", "a @keyframes rule without a name" },
        { "@media (max-width: 10px) { @font-face { font-family: A; src: url(\"a.ttf\") } }", "a @font-face inside @media (max-width: 10px)" },
        { "@font-face { font-family: A }", "@font-face for A without a src" },
        { "@font-face { font-family: A; src: url(\"ui/fonts/a.ttf\"); 12px }", "a declaration that doesn't parse: 12px" },
        { "@font-face { font-family: A; src: local(Arial) }", "@font-face: 'src: local(Arial)' isn't a url()" },
        { ".a { background-image: url(\"../x.png\") }", "url(\"../x.png\") isn't a data path: " },
        { ".a { background-image: url(X.png) }", "url(\"X.png\") isn't a data path: 'X' isn't allowed" },
        { ".a { background-image: url(\"http://e.org/x.png\") }", "url(\"http://e.org/x.png\") isn't a data path" },
        { ".a { background-image: url(\"\") }", "url(\"\") isn't a data path: the path is empty" },
        { ".a { cursor: url(\"ui/images/gone.png\") 4 4, auto }", "url(\"ui/images/gone.png\") names no file in the data being packed" },
        { ".a { --icon: url(\"ui/images/gone.png\") }", "url(\"ui/images/gone.png\") names no file in the data being packed" },
        { ".a { background-image: url(  \"ui/images/gone.png\"  ) }", "url(\"ui/images/gone.png\") names no file in the data being packed" },
        { "@keyframes k { 0% { background-image: url(\"ui/images/gone.png\") } }", "url(\"ui/images/gone.png\") names no file" },
        { "@font-face { font-family: A; src: url(\"ui/fonts/gone.ttf\") }", "url(\"ui/fonts/gone.ttf\") names no file" },
    };

    [Theory]
    [MemberData(nameof(Problems))]
    public void CompilingRefuses(string css, string problem)
    {
        var compiled = Stylesheet.Compile(css, path => path.StartsWith("ui/fonts/a", StringComparison.Ordinal));
        Assert.Contains(compiled.Problems, p => p.StartsWith(problem, StringComparison.Ordinal));
        Assert.Empty(compiled.Dropped);
    }

    public static TheoryData<string> Fine => new()
    {
        "@charset \"utf-8\"; .a { color: red }",
        // Properties the engine deliberately ignores are dropped as parsing drops them.
        ".a { fill: red; stroke-width: 4px; touch-action: none; -webkit-scrollbar-width: 1px; color-scheme: dark }",
        // !important in a keyframe is ignored (CSS Animations §4), its value unchecked.
        "@keyframes k { 0% { stroke-dashoffset: 5; opacity: 1; color: 12px !important } }",
        // A pseudo-element of Chromium's own kind it doesn't know is kept (and matches nothing), as in Blink.
        "input::-webkit-unknown-part { color: red }",
        ".a { background-image: url(\"data:image/svg+xml,%3Csvg%3E%3C/svg%3E\"); cursor: url(DATA:image/png;base64,AA==), auto }",
        ".a { background-image: url(ui/images/a.png) }",
        "@font-face { font-family: A; src: url(\"ui/fonts/a.ttf\") }",
    };

    [Theory]
    [MemberData(nameof(Fine))]
    public void CompilingTakes(string css)
    {
        var compiled = Stylesheet.Compile(css, path => path is "ui/images/a.png" or "ui/fonts/a.ttf");
        Assert.Empty(compiled.Problems);
        Assert.Empty(compiled.Dropped);
    }

    [Fact]
    public void OtherBrowsersSelectorsAreDroppedAsChromiumDropsThem()
    {
        var css = """
            progress::-moz-progress-bar { color: red }
            .a:-moz-focusring, .b { color: red }
            input::-MS-clear { color: red }
            x:-o-prefocus { color: red }
            .c:not(:-moz-any-link) { color: red }
            .d { color: blue }
            """;
        var compiled = Stylesheet.Compile(css);
        Assert.Empty(compiled.Problems);
        Assert.Equal(["progress::-moz-progress-bar", ".a:-moz-focusring, .b", "input::-MS-clear", "x:-o-prefocus", ".c:not(:-moz-any-link)"], compiled.Dropped);
        Assert.Equal(".d", Assert.Single(Stylesheet.FromRecord(compiled.Record).Rules).Source.Selectors.ToString());
        // A prefix elsewhere (a class named like one) is no other browser's selector.
        Assert.Contains("a rule whose selector the engine can't parse: .-moz-x:bogus", Stylesheet.Compile(".-moz-x:bogus { color: red }").Problems);
    }

    [Fact]
    public void TheCompilationListsTheFilesItsUrlsName()
    {
        var css = """
            .a { background: url(ui/images/b.png), url("ui/images/a.png"); cursor: url("ui/images/a.png"), auto; }
            .b { --x: url("data:image/png;base64,AA=="); colr: none }
            @font-face { font-family: A; src: url("ui/fonts/a.ttf") format("opentype") }
            """;
        var compiled = Stylesheet.Compile(css, _ => true);
        Assert.Equal(["ui/fonts/a.ttf", "ui/images/a.png", "ui/images/b.png"], compiled.Files);
        Assert.Equal(["an unknown property in .b: colr: none"], compiled.Problems);
    }

    [Fact]
    public void TheGameStylesheetCompiles()
    {
        var compiled = Stylesheet.Compile(GameStylesheets.CompilableText, _ => true);
        Assert.Empty(compiled.Problems);
        Assert.Equal([".combat-resource progress::-moz-progress-bar", ".combat-resource.mana progress::-moz-progress-bar"], compiled.Dropped);
        Assert.Equal(GameStylesheets.Fonts.Select(f => f.Path).Order(StringComparer.Ordinal), compiled.Files);
        // As the old game has it, its font urls aren't data paths (S7-1): those are its only problems.
        var old = Stylesheet.Compile(GameStylesheets.Text, _ => true);
        Assert.Equal(GameStylesheets.Fonts.Select(f => $"url(\"{f.Url}\") isn't a data path"),
            old.Problems.Where(p => p.StartsWith("url(", StringComparison.Ordinal)).Select(p => p[..p.IndexOf(':')]));
        Assert.All(old.Problems, p => Assert.Contains("isn't a data path", p));
    }

    // ---------- Loading: what is checked again (S5-1's rule) ----------

    const string Base = """
        .a { color: red; margin: var(--m); --m: 1px; background-image: url("ui/images/a.png") }
        @keyframes k { 0% { opacity: 0 } }
        @font-face { font-family: A; src: url("ui/fonts/a.ttf") }
        """;

    static StylesheetRecord BaseRecord()
    {
        var compiled = Stylesheet.Compile(Base, _ => true);
        Assert.Empty(compiled.Problems);
        return compiled.Record;
    }

    static List<CssTokenRecord> Tokens(string text) => [.. CssTokenRecord.From(CssTokenizer.Tokenize(text))];

    static StylesheetRecord WithDeclaration(DeclarationRecord declaration)
    {
        var record = BaseRecord();
        return record with { Rules = [record.Rules[0] with { Declarations = [declaration] }] };
    }

    static StylesheetRecord WithKeyframe(KeyframeRecord frame)
    {
        var record = BaseRecord();
        return record with { Keyframes = [record.Keyframes[0] with { Frames = [frame] }] };
    }

    public static TheoryData<string, string> Unloadable => new()
    {
        { "selector", "rule 1: the selector '>' doesn't parse" },
        { "token", "rule 1: a Integer token without a number" },
        { "media token", "rule 1 (.a), its media query: a Dimension token without a unit" },
        { "shorthand", "rule 1 (.a): 'margin: 1px' isn't a property the cascade takes" },
        { "alias", "rule 1 (.a): 'inline-size: 1px' isn't a property the cascade takes" },
        { "unknown", "rule 1 (.a): 'colr: red' isn't a property the cascade takes" },
        { "ignored", "rule 1 (.a): 'fill: red' isn't a property the cascade takes" },
        { "value", "rule 1 (.a): 'color: 12px' isn't a value the engine can parse" },
        { "not a shorthand", "rule 1 (.a): 'margin-top: var(--m)' names 'margin-x', which isn't a shorthand setting it" },
        { "other shorthand", "rule 1 (.a): 'margin-top: var(--m)' names 'padding', which isn't a shorthand setting it" },
        { "alias shorthand", "rule 1 (.a): 'row-gap: var(--m)' names 'grid-gap', which isn't a shorthand setting it" },
        { "pending without var", "rule 1 (.a): 'margin-top: 1px' waits for its shorthand 'margin' without a var() to substitute" },
        { "custom with shorthand", "rule 1 (.a): '--m: var(--n)' is a custom property with a pending shorthand" },
        { "custom without a name", "rule 1 (.a): '--: 1px' has no custom property name" },
        { "no offsets", "@keyframes k: a keyframe's offsets [] aren't from 0 to 1" },
        { "offset", "@keyframes k: a keyframe's offsets [0, 1.5] aren't from 0 to 1" },
        { "NaN offset", "aren't from 0 to 1" },
        { "keyframe unknown", "@keyframes k: an unknown property: colr: red" },
        { "keyframe value", "@keyframes k: a value the engine can't parse: opacity: x" },
        { "keyframe shorthand", "@keyframes k: 'margin-top' names a pending shorthand outside a style rule" },
        { "font face", "@font-face for A without a src" },
        { "url", "url(\"../a.png\") isn't a data path" },
        { "font url", "@font-face: url(\"A.ttf\") isn't a data path" },
    };

    static StylesheetRecord Broken(string which)
    {
        var record = BaseRecord();
        return which switch
        {
            "selector" => record with { Rules = [record.Rules[0] with { Selectors = Tokens(">") }] },
            "token" => record with { Rules = [record.Rules[0] with { Selectors = [new CssTokenRecord(CssTokenKind.Integer)] }] },
            "media token" => record with { Rules = [record.Rules[0] with { Media = [new CssTokenRecord(CssTokenKind.Dimension, null, 1)] }] },
            "shorthand" => WithDeclaration(new DeclarationRecord("margin", Tokens("1px"))),
            "alias" => WithDeclaration(new DeclarationRecord("inline-size", Tokens("1px"))),
            "unknown" => WithDeclaration(new DeclarationRecord("colr", Tokens("red"))),
            "ignored" => WithDeclaration(new DeclarationRecord("fill", Tokens("red"))),
            "value" => WithDeclaration(new DeclarationRecord("color", Tokens("12px"))),
            "not a shorthand" => WithDeclaration(new DeclarationRecord("margin-top", Tokens("var(--m)"), Shorthand: "margin-x")),
            "other shorthand" => WithDeclaration(new DeclarationRecord("margin-top", Tokens("var(--m)"), Shorthand: "padding")),
            "alias shorthand" => WithDeclaration(new DeclarationRecord("row-gap", Tokens("var(--m)"), Shorthand: "grid-gap")),
            "pending without var" => WithDeclaration(new DeclarationRecord("margin-top", Tokens("1px"), Shorthand: "margin")),
            "custom with shorthand" => WithDeclaration(new DeclarationRecord("--m", Tokens("var(--n)"), Shorthand: "margin")),
            "custom without a name" => WithDeclaration(new DeclarationRecord("--", Tokens("1px"))),
            "no offsets" => WithKeyframe(new KeyframeRecord([], [])),
            "offset" => WithKeyframe(new KeyframeRecord([0, 1.5f], [])),
            "NaN offset" => WithKeyframe(new KeyframeRecord([float.NaN], [])),
            "keyframe unknown" => WithKeyframe(new KeyframeRecord([0], [new DeclarationRecord("colr", Tokens("red"))])),
            "keyframe value" => WithKeyframe(new KeyframeRecord([0], [new DeclarationRecord("opacity", Tokens("x"))])),
            "keyframe shorthand" => WithKeyframe(new KeyframeRecord([0], [new DeclarationRecord("margin-top", Tokens("var(--m)"), Shorthand: "margin")])),
            "font face" => record with { FontFaces = [new FontFaceRecord([new DeclarationRecord("font-family", Tokens("A"))])] },
            "url" => WithDeclaration(new DeclarationRecord("background-image", Tokens("url(\"../a.png\")"))),
            _ => record with { FontFaces = [new FontFaceRecord([new DeclarationRecord("font-family", Tokens("A")), new DeclarationRecord("src", Tokens("url(A.ttf)"))])] },
        };
    }

    /// <summary>The path of what each refusal names in the record (M11 step 6, P20).</summary>
    static readonly Dictionary<string, string> UnloadablePaths = new()
    {
        ["selector"] = "$.rules[0].selectors", ["token"] = "$.rules[0].selectors[0]", ["media token"] = "$.rules[0].media[0]",
        ["shorthand"] = "$.rules[0].declarations[0].property", ["alias"] = "$.rules[0].declarations[0].property",
        ["unknown"] = "$.rules[0].declarations[0].property", ["ignored"] = "$.rules[0].declarations[0].property",
        ["value"] = "$.rules[0].declarations[0].value", ["not a shorthand"] = "$.rules[0].declarations[0].shorthand",
        ["other shorthand"] = "$.rules[0].declarations[0].shorthand", ["alias shorthand"] = "$.rules[0].declarations[0].shorthand",
        ["pending without var"] = "$.rules[0].declarations[0]", ["custom with shorthand"] = "$.rules[0].declarations[0].shorthand",
        ["custom without a name"] = "$.rules[0].declarations[0].property", ["no offsets"] = "$.keyframes[0].frames[0].offsets",
        ["offset"] = "$.keyframes[0].frames[0].offsets", ["NaN offset"] = "$.keyframes[0].frames[0].offsets",
        ["keyframe unknown"] = "$.keyframes[0].frames[0].declarations[0].property", ["keyframe value"] = "$.keyframes[0].frames[0].declarations[0].value",
        ["keyframe shorthand"] = "$.keyframes[0].frames[0].declarations[0].shorthand", ["font face"] = "$.fontFaces[0]",
        ["url"] = "$.rules[0].declarations[0].value[1]", ["font url"] = "$.fontFaces[0]",
    };

    /// <summary>Each refusal of loading, which the sheet's check (M11 step 6) reports first, with its words, at the path of what it names.</summary>
    [Theory]
    [MemberData(nameof(Unloadable))]
    public void LoadingRefuses(string which, string problem)
    {
        Stylesheet.FromRecord(BaseRecord());
        Assert.Empty(RecordChecks.Check(BaseRecord()));
        var thrown = Assert.Throws<InvalidDataException>(() => Stylesheet.FromRecord(Broken(which))).Message;
        Assert.Contains(problem, thrown);
        var first = RecordChecks.Check(Broken(which))[0];
        Assert.Equal((UnloadablePaths[which], thrown), (first.Path.ToString(), first.Message));
    }

    /// <summary>
    /// What the sheet's check names besides: a value's token, an unquoted url and a keyframe's url at their tokens, a
    /// keyframe's refused declaration at its own place among its frame's, and a @font-face refused for its url once.
    /// </summary>
    [Fact]
    public void TheCheckNamesTokensAndUrlsWhereTheyAre()
    {
        string Paths(StylesheetRecord record) => string.Join(" | ", RecordChecks.Check(record).Select(p => p.Path.ToString()));
        Assert.Equal("$.rules[0].declarations[0].value[0]", Paths(WithDeclaration(new DeclarationRecord("color", [new CssTokenRecord(CssTokenKind.Integer)]))));
        Assert.Equal("$.rules[0].declarations[0].value[0]", Paths(WithDeclaration(new DeclarationRecord("background-image", Tokens("url(../u.png)")))));
        Assert.Equal("$.keyframes[0].frames[0].declarations[0].value[1]",
            Paths(WithKeyframe(new KeyframeRecord([0], [new DeclarationRecord("background-image", Tokens("url(\"../k.png\")"))]))));
        Assert.Equal("$.keyframes[0].frames[0].declarations[1].value",
            Paths(WithKeyframe(new KeyframeRecord([0], [new DeclarationRecord("opacity", Tokens("0")), new DeclarationRecord("opacity", Tokens("x"))]))));
        Assert.Equal("$.fontFaces[0]", Paths(Broken("font url")));
    }

    /// <summary>
    /// The sheet's check goes on past a refusal: a declaration refused leaves its rule's other declarations read, a rule or
    /// keyframe refused the others, a font face the urls; each problem at its path, in the order loading meets them.
    /// </summary>
    [Fact]
    public void TheCheckReportsEveryRefusal()
    {
        var record = BaseRecord();
        record = record with
        {
            Rules = [record.Rules[0] with { Declarations = [new DeclarationRecord("colr", Tokens("red")), new DeclarationRecord("color", Tokens("12px")), .. record.Rules[0].Declarations] },
                record.Rules[0] with { Selectors = Tokens(">") }, record.Rules[0] with { Media = [new CssTokenRecord(CssTokenKind.Dimension, null, 1)] }],
            Keyframes = [record.Keyframes[0] with { Frames = [new KeyframeRecord([2], []), new KeyframeRecord([0], [new DeclarationRecord("opacity", Tokens("x"))])] }],
            FontFaces = [new FontFaceRecord([new DeclarationRecord("font-family", Tokens("A"))]), .. record.FontFaces],
        };
        record = record with { Rules = [.. record.Rules, new StyleRuleRecord(Tokens(".b"), [new DeclarationRecord("background-image", Tokens("url(\"../b.png\")"))])] };
        Assert.Equal(["$.rules[0].declarations[0].property", "$.rules[0].declarations[1].value", "$.rules[1].selectors", "$.rules[2].media[0]",
                "$.keyframes[0].frames[0].offsets", "$.keyframes[0].frames[1].declarations[0].value", "$.fontFaces[0]", "$.rules[3].declarations[0].value[1]"],
            RecordChecks.Check(record).Select(p => p.Path.ToString()));
    }

    [Fact]
    public void LoadingTakesWhatPreparingMakes()
    {
        // A CSS-wide keyword on a shorthand sets its longhands with no pending shorthand; var() in a
        // custom property and in a longhand; the media query's own tokens.
        var css = "@media (min-width: 1px) { .a { border: inherit; --x: var(--y); color: var(--c, red); margin: 1px var(--m) } }";
        var sheet = Stylesheet.FromRecord(Stylesheet.Compile(css).Record);
        Assert.Equal(StylesheetText.Of(Stylesheet.Parse(css)), StylesheetText.Of(sheet));
        Assert.Contains(sheet.Rules[0].Declarations, d => d is { Property: "border-top-width", Keyword: "inherit", PendingShorthand: null });
    }

    static string PackOf(TempFolder folder, Action<PackWriter> add)
    {
        var writer = new PackWriter();
        add(writer);
        var file = folder["styles.pak"];
        writer.Write(file);
        return file;
    }

    [Fact]
    public void ASheetLoadsFromAPackAndFromItsJsonFormInALooseFolder()
    {
        using var folder = new TempFolder();
        var record = Stylesheet.Compile(EveryKind, _ => true).Record;
        var pack = PackOf(folder, writer => writer.AddRecord("ui/styles/every", record));
        folder.Write("loose/ui/styles/every.json", RecordSerializer.ToJson(record));
        using var packed = new VirtualFileSystem([Mount.Pack(pack)]);
        using var loose = new VirtualFileSystem([Mount.Folder(folder["loose"])]);
        var fromPack = Stylesheet.Load(packed, "ui/styles/every");
        var fromFolder = Stylesheet.Load(loose, "ui/styles/every");
        Assert.Equal("ui/styles/every", fromPack.Name);
        Assert.Equal(StylesheetText.Of(Stylesheet.Parse(EveryKind)), StylesheetText.Of(fromPack));
        Assert.Equal(StylesheetText.Of(fromPack), StylesheetText.Of(fromFolder));
        Assert.Equal((packed, "ui/styles/every"), fromPack.LoadedFrom);
    }

    [Fact]
    public void ASheetThatDoesntLoadIsNamedByItsPackAndEntryOrItsFile()
    {
        using var folder = new TempFolder();
        var broken = Broken("value");
        var pack = PackOf(folder, writer =>
        {
            writer.AddRecord("ui/styles/broken", broken);
            writer.Add("ui/styles/text", "x"u8);
        });
        folder.Write("loose/ui/styles/broken.json", RecordSerializer.ToJson(broken));
        using var packed = new VirtualFileSystem([Mount.Pack(pack)]);
        var problem = Assert.Throws<DataFileException>(() => Stylesheet.Load(packed, "ui/styles/broken"));
        Assert.Equal((pack, "ui/styles/broken"), (problem.File, problem.Entry));
        Assert.StartsWith("not a valid stylesheet: rule 1 (.a): 'color: 12px' isn't a value the engine can parse", problem.Problem);
        Assert.IsType<InvalidDataException>(problem.InnerException);
        using var loose = new VirtualFileSystem([Mount.Folder(folder["loose"])]);
        var looseProblem = Assert.Throws<DataFileException>(() => Stylesheet.Load(loose, "ui/styles/broken"));
        Assert.Equal((folder["loose/ui/styles/broken.json"], null), (looseProblem.File, looseProblem.Entry));
        // A file where a sheet should be, or nothing there: the file system's own errors.
        Assert.Contains("a file, not a record (expected a ui.stylesheet)", Assert.Throws<DataFileException>(() => Stylesheet.Load(packed, "ui/styles/text")).Message);
        Assert.Throws<FileNotFoundException>(() => Stylesheet.Load(packed, "ui/styles/gone"));
    }

    // ---------- The UI importer ----------

    [Fact]
    public void TheEnginesUiImporterTakesStylesheetsOnly()
    {
        Assert.Equal("ui", Importers.Engine.For("ui/styles/game.css")?.Importer.Name);
        Assert.Equal(typeof(StylesheetImporter), Importers.Engine.For("ui/styles/game.css")?.Importer.Type);
        foreach (var other in new[] { "ui/fonts/a.ttf", "ui/fonts/a.otf", "ui/images/a.png", "ui/images/a.svg", "ui/fonts/lato-ofl.txt", "ui/styles/game.css.json" })
            Assert.Null(Importers.Engine.For(other));
    }

    [Fact]
    public void PackingAFolderCompilesEachStylesheetBesideTheFilesItNames()
    {
        using var folder = new TempFolder();
        folder.Write("source/ui/styles/game.css", GameStylesheets.CompilableText);
        folder.Write("source/ui/styles/menu/small.css", "\uFEFF.a { background: url(\"ui/images/a.png\") }");
        foreach (var (_, path, file) in GameStylesheets.Fonts)
            folder.Write("source/" + path, File.ReadAllBytes(GameStylesheets.FontFile(file)));
        folder.Write("source/ui/images/a.png", [1, 2, 3]);
        var writer = new PackWriter();
        writer.AddFolder(folder["source"]);
        writer.Write(folder["ui.pak"]);
        using var pack = PackFile.Open(folder["ui.pak"]);
        Assert.Equal(
            GameStylesheets.Fonts.Select(f => (f.Path, false))
                .Concat([("ui/images/a.png", false), ("ui/styles/game", true), ("ui/styles/menu/small", true)])
                .OrderBy(e => e.Item1, StringComparer.Ordinal),
            pack.Entries.Select(e => (e.Path, e.IsRecord)));
        using var files = new VirtualFileSystem([Mount.Pack(folder["ui.pak"])]);
        Assert.Equal(StylesheetText.Of(GameStylesheets.Compiled), StylesheetText.Of(Stylesheet.Load(files, "ui/styles/game")));
        Assert.Equal(".a", Assert.Single(Stylesheet.Load(files, "ui/styles/menu/small").Rules).Source.Selectors.ToString());
        Assert.Equal(File.ReadAllBytes(GameStylesheets.FontFile("Lato-Bold.ttf")), files.ReadAllBytes("ui/fonts/lato-bold.ttf"));
    }

    [Fact]
    public void TheImporterRefusesASheetNamingEveryProblem()
    {
        using var folder = new TempFolder();
        var css = folder.Write("source/ui/styles/bad.css", ".a { colr: red; background: url(\"ui/images/gone.png\") } .b:bogus { color: red }");
        var problem = Assert.Throws<DataFileException>(() => new PackWriter().AddFolder(folder["source"]));
        Assert.Equal((css, null), (problem.File, problem.Entry));
        Assert.Equal("the ui importer: 3 problems: a rule whose selector the engine can't parse: .b:bogus; an unknown property in .a: colr: red; " +
            "url(\"ui/images/gone.png\") names no file in the data being packed", problem.Problem);
        Assert.IsType<InvalidDataException>(problem.InnerException);
    }

    [Fact]
    public void TheImporterShowsTwentyProblemsAndCountsTheRest()
    {
        using var folder = new TempFolder();
        folder.Write("source/ui/a.css", string.Concat(Enumerable.Range(0, 25).Select(i => $".a{i} {{ colr: red }}\n")));
        var problem = Assert.Throws<DataFileException>(() => new PackWriter().AddFolder(folder["source"]));
        Assert.StartsWith("the ui importer: 25 problems: an unknown property in .a0: colr: red;", problem.Problem);
        Assert.EndsWith("an unknown property in .a19: colr: red; and 5 more", problem.Problem);
        folder.Write("one/ui/a.css", ".a { colr: red }");
        Assert.Equal("the ui importer: 1 problem: an unknown property in .a: colr: red", Assert.Throws<DataFileException>(() => new PackWriter().AddFolder(folder["one"])).Problem);
    }

    [Fact]
    public void TheImporterReadsUtf8Text()
    {
        using var folder = new TempFolder();
        folder.Write("source/ui/a.css", [.. Encoding.UTF8.GetBytes(".a { content: \"ä\" }"), 0xFF]);
        Assert.Equal("the ui importer: the stylesheet isn't UTF-8 text", Assert.Throws<DataFileException>(() => new PackWriter().AddFolder(folder["source"])).Problem);
        folder.Write("bom/ui/a.css", [0xEF, 0xBB, 0xBF, .. Encoding.UTF8.GetBytes(".a { content: \"ä\" }")]);
        var writer = new PackWriter();
        writer.AddFolder(folder["bom"]);
        writer.Write(folder["bom.pak"]);
        using var files = new VirtualFileSystem([Mount.Pack(folder["bom.pak"])]);
        var declaration = Assert.Single(Stylesheet.Load(files, "ui/a").Rules[0].Declarations);
        Assert.Equal("ä", declaration.Value.Single(t => t.Type == CssTokenType.String).Value);
        Assert.Equal(".a", Stylesheet.Load(files, "ui/a").Rules[0].Source.Selectors.ToString());
    }
}

using Statventure.Records;
using Statventure.Tests.Canvas;
using Statventure.Tests.Records;
using Statventure.UI.Css;
using Statventure.UI.Dom;
using Statventure.UI.Style;

namespace Statventure.Tests.Ui;

/// <summary>
/// Registered custom properties (@property, GM1 step 1a) beside their Chromium fixture
/// (<see cref="StylesheetRuleReferenceTests"/>): Blink's syntax strings and validation, the computed colour's text,
/// compiling, the record's second version, the registry, animated custom properties in the cascade and the
/// background shorthand's clip.
/// </summary>
public class PropertyRuleTests
{
    // ---------- Syntax strings (CSSSyntaxStringParser) ----------

    [Theory]
    [InlineData("<color>", "color", "", '\0')]
    [InlineData("  <color>\t", "color", "", '\0')]
    [InlineData("<length>+", "length", "", '+')]
    [InlineData("<length>#", "length", "", '#')]
    [InlineData("<transform-list>", "transform-list", "", '\0')]
    [InlineData("auto", "", "auto", '\0')]
    [InlineData("_x", "", "_x", '\0')]
    public void ASyntaxComponentIsATypeOrAnIdentifier(string syntax, string type, string ident, char multiplier)
    {
        var definition = SyntaxDefinition.Parse(syntax)!;
        var component = Assert.Single(definition.Components);
        Assert.Equal((type.Length > 0 ? type : null, ident.Length > 0 ? ident : null, multiplier), component);
        Assert.False(definition.Universal);
    }

    [Fact]
    public void ASyntaxIsUniversalOrComponentsBetweenBars()
    {
        Assert.True(SyntaxDefinition.Parse(" * ")!.Universal);
        Assert.Empty(SyntaxDefinition.Parse("*")!.Components);
        Assert.Equal([("color", null, '\0'), (null, "none", '\0'), ("length", null, '#')], SyntaxDefinition.Parse("<color> | none|<length>#")!.Components);
        Assert.True(SyntaxDefinition.Parse("<color>")!.IsColor);
        Assert.False(SyntaxDefinition.Parse("<color>+")!.IsColor);
        Assert.False(SyntaxDefinition.Parse("<color> | none")!.IsColor);
        Assert.False(SyntaxDefinition.Parse("<length>")!.IsColor);
        Assert.False(SyntaxDefinition.Parse("color")!.IsColor);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("* *")]
    [InlineData("*|<color>")]
    [InlineData("<colour>")]
    [InlineData("<Color>")]
    [InlineData("<color")]
    [InlineData("< color>")]
    [InlineData("<color> <length>")]
    [InlineData("<color> |")]
    [InlineData("| <color>")]
    [InlineData("<transform-list>+")]
    [InlineData("<color>++")]
    [InlineData("inherit")]
    [InlineData("INITIAL")]
    [InlineData("revert-layer")]
    [InlineData("default")]
    [InlineData("-x")]
    [InlineData("1x")]
    public void ASyntaxThatDoesntParseIsNone(string syntax) => Assert.Null(SyntaxDefinition.Parse(syntax));

    // ---------- The rule (PropertyRegistration) ----------

    static CssPropertyRule Rule(string css) => (CssPropertyRule)CssParser.ParseStylesheet(css).Single();

    [Theory]
    [InlineData("@property --a { syntax: \"<color>\"; inherits: true; initial-value: red; }", "--a", true, "rgb(255, 0, 0)")]
    [InlineData("@property --a { inherits: FALSE; initial-value: #0000ff80; syntax: ' <color> '; }", "--a", false, "rgba(0, 0, 255, 0.5)")]
    [InlineData("@property --a { syntax: \"<color>\"; syntax: 12px; inherits: true; initial-value: currentColor; }", "--a", true, "currentcolor")]
    [InlineData("@property --a { syntax: \"<color>\"; inherits: true; inherits: 2; initial-value: red; initial-value: blue; colour: x; }", "--a", true, "rgb(0, 0, 255)")]
    [InlineData("@property --a { syntax: \"<color>\"; inherits: true; initial-value: transparent; }", "--a", true, "rgba(0, 0, 0, 0)")]
    [InlineData("@property --a { syntax: \"<length>\"; inherits: false; initial-value: red; inherits: true; syntax: \"<color>\"; }", "--a", true, "rgb(255, 0, 0)")]
    public void AValidRuleRegistersItsProperty(string css, string name, bool inherits, string initial)
    {
        var validation = PropertyRegistration.Validate(Rule(css));
        Assert.Null(validation.Problem);
        var registration = validation.Registration!;
        Assert.Equal((name, inherits, initial), (registration.Name, registration.Inherits, Text(registration.Initial)));
    }

    [Theory]
    [InlineData("@property a { syntax: \"<color>\"; inherits: true; initial-value: red; }", "its prelude 'a' isn't one custom property name")]
    [InlineData("@property -- { syntax: \"<color>\"; inherits: true; initial-value: red; }", "its prelude '--'")]
    [InlineData("@property abc { syntax: \"<color>\"; inherits: true; initial-value: red; }", "its prelude 'abc'")]
    [InlineData("@property --a b { syntax: \"<color>\"; inherits: true; initial-value: red; }", "its prelude '--a b'")]
    [InlineData("@property { syntax: \"<color>\"; inherits: true; initial-value: red; }", "its prelude ''")]
    [InlineData("@property --a { syntax: <color>; inherits: true; initial-value: red; }", "--a has no valid syntax descriptor")]
    [InlineData("@property --a { syntax: \"<colour>\"; inherits: true; initial-value: red; }", "--a has no valid syntax descriptor")]
    [InlineData("@property --a { syntax: \"<color>\" \"x\"; inherits: true; initial-value: red; }", "--a has no valid syntax descriptor")]
    [InlineData("@property --a { syntax: \"<color>\" !important; inherits: true; initial-value: red; }", "--a has no valid syntax descriptor")]
    [InlineData("@property --a { syntax: \"<color>\"; initial-value: red; }", "--a has no valid inherits descriptor")]
    [InlineData("@property --a { syntax: \"<color>\"; inherits: yes; initial-value: red; }", "--a has no valid inherits descriptor")]
    [InlineData("@property --a { syntax: \"<color>\"; inherits: true false; initial-value: red; }", "--a has no valid inherits descriptor")]
    [InlineData("@property --a { syntax: \"<color>\"; inherits: true; }", "--a has no initial-value")]
    [InlineData("@property --a { syntax: \"<color>\"; inherits: true; initial-value: red !important; }", "--a has no initial-value")]
    [InlineData("@property --a { syntax: \"<color>\"; inherits: true; initial-value: 12px; }", "--a's initial-value '12px' isn't a <color>")]
    [InlineData("@property --a { syntax: \"<color>\"; inherits: true; initial-value: var(--b); }", "--a's initial-value 'var(--b)' isn't a <color>")]
    [InlineData("@property --a { syntax: \"<color>\"; inherits: true; initial-value: red blue; }", "--a's initial-value 'red blue' isn't a <color>")]
    [InlineData("@property --a { syntax: \"<color>\"; inherits: true; initial-value: ; }", "--a's initial-value '' isn't a <color>")]
    public void ARuleChromiumIgnoresRegistersNothing(string css, string problem)
    {
        var validation = PropertyRegistration.Validate(Rule(css));
        Assert.Null(validation.Registration);
        Assert.True(validation.ChromiumIgnoresIt);
        Assert.StartsWith(problem, validation.Problem);
    }

    [Theory]
    [InlineData("@property --a { syntax: \"<length>\"; inherits: true; initial-value: 4px; }", "--a's syntax \"<length>\" isn't one the engine registers")]
    [InlineData("@property --a { syntax: \"*\"; inherits: true; }", "--a's syntax \"*\" isn't one")]
    [InlineData("@property --a { syntax: \"<color>+\"; inherits: true; initial-value: red; }", "--a's syntax \"<color>+\"")]
    [InlineData("@property --a { syntax: \"<color> | none\"; inherits: true; initial-value: red; }", "--a's syntax \"<color> | none\"")]
    public void ASyntaxOtherThanColourIsntTheEngines(string css, string problem)
    {
        var validation = PropertyRegistration.Validate(Rule(css));
        Assert.Null(validation.Registration);
        Assert.False(validation.ChromiumIgnoresIt);
        Assert.StartsWith(problem, validation.Problem);
    }

    // ---------- The computed colour ----------

    static string Text(IEnumerable<CssToken> tokens) => string.Concat(tokens.Select(t => t.ToString()));

    static string? Computed(string value) => PropertyRegistration.ComputeColor(CssTokenizer.Tokenize(value)) is { } tokens ? Text(tokens) : null;

    [Theory]
    [InlineData("#14b351", "rgb(20, 179, 81)")]
    [InlineData(" RED ", "rgb(255, 0, 0)")]
    [InlineData("rgb(10.4 20.5 30)", "rgb(10, 21, 30)")]
    [InlineData("rgba(1, 2, 3, 0.5)", "rgba(1, 2, 3, 0.5)")]
    [InlineData("rgb(1 2 3 / 40%)", "rgba(1, 2, 3, 0.4)")]
    [InlineData("rgb(1 2 3 / 0.333)", "rgba(1, 2, 3, 0.333)")]
    [InlineData("#00000001", "rgba(0, 0, 0, 0.004)")]
    [InlineData("hsl(30deg 60% 40%)", "rgb(163, 102, 41)")]
    [InlineData("currentcolor", "currentcolor")]
    [InlineData("CurrentColor", "currentcolor")]
    [InlineData("color-mix(in srgb, red 25%, blue)", "color(srgb 0.25 0 0.75)")]
    [InlineData("color-mix(in srgb, red, transparent)", "color(srgb 1 0 0 / 0.5)")]
    [InlineData("color-mix(in srgb, currentcolor, red)", "color-mix(in srgb, currentcolor, red)")]
    [InlineData("color(srgb 0.5 0 1)", "color(srgb 0.5 0 1)")]
    [InlineData("rgb(1 2 3 / 0.7)", "rgba(1, 2, 3, 0.7)")]
    [InlineData("color(srgb-linear 0.214041 0 0)", "color(srgb-linear 0.214041 0 0)")]
    public void ARegisteredColoursComputedValueIsChromiumsText(string value, string computed) => Assert.Equal(computed, Computed(value));

    [Theory]
    [InlineData("")]
    [InlineData("12px")]
    [InlineData("red blue")]
    [InlineData("var(--x)")]
    [InlineData("rgb(1, 2)")]
    [InlineData("color(display-p3 1 0 0)")]
    public void AValueThatIsntAColourHasNone(string value) => Assert.Null(Computed(value));

    [Fact]
    public void AColourInSrgbParsesAsTheColourItNames()
    {
        Assert.Equal(new Statventure.Core.Color(0.25f, 0, 0.75f, 0.5f), Color("color(srgb 0.25 0 75% / 0.5)"));
        Assert.Null(Color("color(srgb 0.25 0)"));
        Assert.Null(Color("color(srgb 0.25 0 1 0.5)"));
        Assert.Null(Color("color(srgb 0.25 0 1 / 0.5 / 1)"));
        Assert.Null(Color("color(srgb 0.25 0 1 0.5 1)"));
        Assert.Null(Color("color(xyz 0.25 0 1)"));
        Assert.Null(Color("color(srgb a 0 1)"));
        Assert.Equal(1, Color("color(srgb-linear 1 1 1)")!.Value.R, 4);
        Assert.Equal(0.5f, Color("color(srgb-linear 0.214041 0 0)")!.Value.R, 3);
    }

    static Statventure.Core.Color? Color(string text)
    {
        var reader = new ValueReader(CssTokenizer.Tokenize(text));
        return reader.TryColor(Statventure.Core.Color.Black, out var color) && reader.AtEnd ? color : null;
    }

    // ---------- Compiling and the record ----------

    const string Sample = """
        @property --tone { syntax: "<color>"; inherits: true; initial-value: #14b351; }
        @property --flat { syntax: " <color> "; inherits: false; initial-value: rgb(10 20 30 / 50%); color: red; }
        .a { --tone: #8a3cf0; color: var(--tone); -webkit-text-fill-color: transparent; background: linear-gradient(red, blue) text; }
        """;

    static StylesheetRecord SampleRecord()
    {
        var compiled = Stylesheet.Compile(Sample, _ => true);
        Assert.Empty(compiled.Problems);
        return compiled.Record;
    }

    [Fact]
    public void TheVersion2RecordFixtureIsRecorded() => RecordFixtures.Ensure("with-properties", SampleRecord());

    [Fact]
    public void ASheetsPropertyRulesAreInItsRecordAndBack()
    {
        var record = SampleRecord();
        Assert.Equal(["--tone", "--flat"], record.Properties.Select(p => p.Name));
        Assert.Equal(["syntax", "inherits", "initial-value", "color"], record.Properties[1].Descriptors.Select(d => d.Property));
        var loaded = Stylesheet.FromRecord(record, "x");
        Assert.Equal(StylesheetText.Of(Stylesheet.Parse(Sample)), StylesheetText.Of(loaded));
        var binary = RecordSerializer.ToBinary(record);
        Assert.Equal(binary, RecordSerializer.ToBinary(RecordSerializer.FromBinary<StylesheetRecord>(binary)));
        Assert.Equal(binary, RecordSerializer.ToBinary(RecordSerializer.FromJson<StylesheetRecord>(RecordSerializer.ToJson(record))));
    }

    [Fact]
    public void AVersion1SheetHasNoPropertyRules()
    {
        var old = new StylesheetRecord.V1([], [], []);
        Assert.Empty(old.Upgrade().Properties);
        Assert.Empty(new StylesheetRecord([], [], []).Properties);
        // The version 1 fixture read keeps its parts.
        var read = RecordSerializer.FromBinary<StylesheetRecord>(File.ReadAllBytes(RecordFixtures.PathOf("ui.stylesheet", "every-kind", ".rec")));
        Assert.Equal((2, 2, 1, 0), (read.Rules.Count, read.Keyframes.Count, read.FontFaces.Count, read.Properties.Count));
    }

    [Theory]
    [InlineData("@property --a { syntax: \"<colour>\"; inherits: true; initial-value: red; }", "a @property rule Chromium ignores: --a has no valid syntax descriptor")]
    [InlineData("@property a { syntax: \"<color>\"; inherits: true; initial-value: red; }", "a @property rule Chromium ignores: its prelude 'a'")]
    [InlineData("@property --a { syntax: \"<length>\"; inherits: true; initial-value: 1px; }", "a @property rule the engine can't take: --a's syntax \"<length>\"")]
    [InlineData("@media (min-width: 1px) { @property --a { syntax: \"<color>\"; inherits: true; initial-value: red; } }", "a @property inside @media (min-width: 1px)")]
    public void CompilingRefusesARuleThatRegistersNothing(string css, string problem) =>
        Assert.Contains(Stylesheet.Compile(css).Problems, p => p.StartsWith(problem, StringComparison.Ordinal));

    [Theory]
    [InlineData("name", "--", "'--' isn't a custom property's name")]
    [InlineData("descriptors", "--a", "@property --a: --a has no valid inherits descriptor")]
    public void LoadingRefusesAPropertyRuleThatRegistersNothing(string which, string name, string problem)
    {
        var syntax = new DeclarationRecord("syntax", CssTokenRecord.From(CssTokenizer.Tokenize("\"<color>\"")));
        var inherits = new DeclarationRecord("inherits", CssTokenRecord.From(CssTokenizer.Tokenize("true")));
        var initial = new DeclarationRecord("initial-value", CssTokenRecord.From(CssTokenizer.Tokenize("red")));
        var record = new StylesheetRecord([], [], [])
        {
            Properties = [new PropertyRuleRecord(name, which == "name" ? [syntax, inherits, initial] : [syntax, initial])],
        };
        Assert.Contains(problem, Assert.Throws<InvalidDataException>(() => Stylesheet.FromRecord(record)).Message);
        var reported = Assert.Single(Stylesheet.ProblemsOf(record));
        Assert.Equal(which == "name" ? "$.properties[0].name" : "$.properties[0]", reported.Path.ToString());
    }

    // ---------- The registry and the cascade ----------

    static UiDocument Document(string css, string html)
    {
        var document = new UiDocument(TestFonts.Library);
        document.SetViewport(400, 300);
        document.AddStylesheet(css, "t");
        Markup.SetInnerHtml(document.Body, html);
        document.Update(0);
        return document;
    }

    static string Custom(UiDocument document, string id, string name) => ComputedValues.Of(document.GetElementById(id)!, name);

    [Fact]
    public void ALaterValidRuleWinsAndALaterSheetsToo()
    {
        var document = Document("""
            @property --a { syntax: "<color>"; inherits: true; initial-value: red; }
            @property --a { syntax: "<color>"; inherits: false; initial-value: blue; }
            @property --a { syntax: "<color>"; inherits: true; initial-value: 4px; }
            .p { --a: lime; }
            """, """<div class="p"><div id="c"></div></div><div id="o"></div>""");
        // The second rule: blue, not inherited.
        Assert.Equal("rgb(0, 0, 255)", Custom(document, "c", "--a"));
        Assert.Equal("rgb(0, 0, 255)", Custom(document, "o", "--a"));
        var later = document.AddStylesheet("@property --a { syntax: \"<color>\"; inherits: true; initial-value: gray; }", "later");
        document.Update(0);
        Assert.Equal("rgb(0, 255, 0)", Custom(document, "c", "--a"));
        // The initial style is made again: the root's value is the new initial one, inherited.
        Assert.Equal("rgb(128, 128, 128)", Custom(document, "o", "--a"));
        document.RemoveStylesheet(later);
        document.Update(0);
        Assert.Equal("rgb(0, 0, 255)", Custom(document, "c", "--a"));
        Assert.Equal("rgb(0, 0, 255)", Custom(document, "o", "--a"));
    }

    [Fact]
    public void AnInheritedRegisteredValueIsItsComputedValue()
    {
        var document = Document("""
            @property --a { syntax: "<color>"; inherits: true; initial-value: red; }
            .p { --a: hsl(120 100% 25%); --b: var(--a); }
            .k { --a: inherit; --c: var(--a, x); }
            .w { --a: var(--b); }
            .v { --x: hsl(120 100% 25%); --a: var(--x); }
            """, """<div class="p" id="p"><div id="c"></div><div class="k" id="k"></div><div class="w" id="w"></div><div class="v" id="v"></div></div><div id="root-child"></div>""");
        Assert.Equal("rgb(0, 128, 0)", Custom(document, "p", "--a"));
        Assert.Equal("rgb(0, 128, 0)", Custom(document, "p", "--b"));
        Assert.Equal("rgb(0, 128, 0)", Custom(document, "c", "--a"));
        Assert.Equal("rgb(0, 128, 0)", Custom(document, "k", "--c"));
        Assert.Equal("rgb(0, 128, 0)", Custom(document, "w", "--a"));
        // A value substituted by var() is computed too.
        Assert.Equal("rgb(0, 128, 0)", Custom(document, "v", "--a"));
        Assert.Equal("rgb(255, 0, 0)", Custom(document, "root-child", "--a"));
    }

    [Fact]
    public void ACycleOrAMissingReferenceGivesTheUnsetValue()
    {
        var document = Document("""
            @property --a { syntax: "<color>"; inherits: true; initial-value: red; }
            @property --n { syntax: "<color>"; inherits: false; initial-value: blue; }
            .p { --a: lime; --n: lime; }
            .q { --a: var(--z); --n: var(--z); }
            .r { --a: var(--b); --b: var(--a); }
            .s { --a: var(--none, initial); --n: var(--none, inherit); }
            .t { --px: 12px; --a: var(--px); --n: var(--px); }
            """, """<div class="p"><div class="q" id="q"></div><div class="r" id="r"></div><div class="s" id="s"></div><div class="t" id="t"></div></div>""");
        Assert.Equal("rgb(0, 255, 0)", Custom(document, "q", "--a"));
        Assert.Equal("rgb(0, 0, 255)", Custom(document, "q", "--n"));
        Assert.Equal("rgb(0, 255, 0)", Custom(document, "r", "--a"));
        Assert.Equal("", Custom(document, "r", "--b"));
        Assert.Equal("rgb(255, 0, 0)", Custom(document, "s", "--a"));
        Assert.Equal("rgb(0, 255, 0)", Custom(document, "s", "--n"));
        // A substituted value that isn't a colour: the unset value too.
        Assert.Equal("rgb(0, 255, 0)", Custom(document, "t", "--a"));
        Assert.Equal("rgb(0, 0, 255)", Custom(document, "t", "--n"));
    }

    [Fact]
    public void AnAnimatedRegisteredColourReachesWhatUsesItAndTheChildren()
    {
        var document = Document("""
            @property --tone { syntax: "<color>"; inherits: true; initial-value: #14b351; }
            @keyframes tone { 0%, 100% { --tone: #14b351; } 50% { --tone: #8a3cf0; } }
            .t { animation: tone 4s linear infinite; background-color: var(--tone); transition: background-color 1s; }
            .i { --tone: red !important; animation: tone 4s linear infinite; }
            .v { --base: #c00; --tone: var(--base); animation: tone 4s linear infinite; }
            @keyframes gone { from, to { --x: initial; } }
            .g { --y: red; --x: var(--y); animation: gone 4s linear infinite; }
            """, """<div class="t" id="t"><div id="c" style="color: var(--tone)">x</div></div><div class="i" id="i"></div><div class="v" id="v"></div><div class="g" id="g"></div>""");
        var t = document.GetElementById("t")!;
        document.Update(1);
        Assert.Equal("rgb(79, 120, 161)", Custom(document, "t", "--tone"));
        // The animation's value is over a cascaded one that uses var(), even when it is the guaranteed-invalid value.
        Assert.Equal("rgb(79, 120, 161)", Custom(document, "v", "--tone"));
        Assert.Equal("", Custom(document, "g", "--x"));
        Assert.Equal("rgb(79, 120, 161)", Custom(document, "t", "background-color"));
        Assert.Equal("rgb(79, 120, 161)", Custom(document, "c", "color"));
        document.Update(2);
        Assert.Equal("rgb(138, 60, 240)", Custom(document, "t", "background-color"));
        Assert.Equal("rgb(138, 60, 240)", Custom(document, "c", "color"));
        // The dependent property doesn't transition as the animation moves it (animation-only restyles start none).
        Assert.False(document.Animations.HasCurrentAnimation(t, "", "background-color"));
        // An !important declaration isn't animated.
        Assert.Equal("rgb(255, 0, 0)", Custom(document, "i", "--tone"));
        // At the same time again nothing is computed again.
        var computed = document.Styles.StylesComputed;
        document.Update(2);
        Assert.Equal(computed, document.Styles.StylesComputed);
    }

    [Fact]
    public void AKeyframesValuesAreTheElementsComputedOnes()
    {
        var document = Document("""
            @property --tone { syntax: "<color>"; inherits: true; initial-value: #14b351; }
            @keyframes part { 50% { --tone: #8a3cf0; } }
            @keyframes via { from { --tone: var(--from); } to { --tone: #8a3cf0; } }
            @keyframes init { from { --tone: initial; } to { --tone: #8a3cf0; } }
            @keyframes inh { from { --tone: inherit; } to { --tone: #8a3cf0; } }
            @keyframes cur { from { --tone: currentcolor; } to { --tone: #00f; } }
            @keyframes fade { from { --tone: rgba(255, 0, 0, 1); } to { --tone: rgba(0, 0, 255, 0); } }
            @keyframes clear { from { --tone: rgba(255, 0, 0, 0); } to { --tone: rgba(0, 0, 255, 0); } }
            @keyframes w { from { --w: 10px; } to { --w: 40px; } }
            @keyframes stepped { from { --tone: #000; animation-timing-function: steps(1, end); } to { --tone: #c8643c; } }
            .pp { --tone: #cc0000; }
            #part { --tone: #cc0000; animation: part 2s linear paused -1.5s; }
            #via { --from: #cc0000; animation: via 2s linear paused -1s; }
            #init { --tone: #cc0000; animation: init 2s linear paused -1s; }
            #inh { --tone: #14b351; animation: inh 2s linear paused -1s; }
            #cur { color: #f00; animation: cur 2s linear paused -1s; }
            #fade { animation: fade 2s linear paused -1s; }
            #clear { animation: clear 2s linear paused -1s; }
            #w { animation: w 2s linear paused -1s; }
            #stepped { animation: stepped 2s linear paused -1s; }
            """, """<div class="pp"><div id="part"></div><div id="via"></div><div id="init"></div><div id="inh"></div><div id="cur"></div><div id="fade"></div><div id="clear"></div><div id="w"></div><div id="stepped"></div></div>""");
        // A missing 100 % keyframe is the underlying value (at 75 %: half way back to it).
        Assert.Equal("rgb(171, 30, 120)", Custom(document, "part", "--tone"));
        // var() substituted from the element; initial and inherit as the registration and the parent give them.
        Assert.Equal("rgb(171, 30, 120)", Custom(document, "via", "--tone"));
        Assert.Equal("rgb(79, 120, 161)", Custom(document, "init", "--tone"));
        Assert.Equal("rgb(171, 30, 120)", Custom(document, "inh", "--tone"));
        // currentcolor is the element's colour; colours blend premultiplied; no alpha left is transparent black.
        Assert.Equal("rgb(128, 0, 128)", Custom(document, "cur", "--tone"));
        Assert.Equal("rgba(255, 0, 0, 0.5)", Custom(document, "fade", "--tone"));
        Assert.Equal("rgba(0, 0, 0, 0)", Custom(document, "clear", "--tone"));
        // A value that doesn't interpolate flips half way.
        Assert.Equal("40px", Custom(document, "w", "--w"));
        // A keyframe's own timing function eases its segment.
        Assert.Equal("rgb(0, 0, 0)", Custom(document, "stepped", "--tone"));
    }

    [Fact]
    public void ATransitionOfARegisteredColourStartsFromWhereItIs()
    {
        var document = Document("""
            @property --tone { syntax: "<color>"; inherits: true; initial-value: #14b351; }
            .tr { --tone: rgb(0, 0, 0); --u: rgb(0, 0, 0); transition: --tone 10s linear, --u 10s linear; }
            .tr.go { --tone: rgb(200, 100, 50); --u: rgb(200, 100, 50); }
            .tr.again { --tone: rgb(0, 0, 255); }
            """, """<div class="tr" id="tr"></div>""");
        var tr = document.GetElementById("tr")!;
        document.Update(1);
        tr.AddClass("go");
        document.Update(1);
        document.Update(4);
        Assert.Equal("rgb(60, 30, 15)", Custom(document, "tr", "--tone"));
        // An unregistered property doesn't transition.
        Assert.Equal("rgb(200, 100, 50)", Custom(document, "tr", "--u"));
        document.Update(6);
        Assert.Equal("rgb(100, 50, 25)", Custom(document, "tr", "--tone"));
        // A new value: the next transition starts from the value shown.
        tr.RemoveClass("go");
        tr.AddClass("again");
        document.Update(6);
        document.Update(11);
        Assert.Equal("rgb(50, 25, 140)", Custom(document, "tr", "--tone"));
    }

    // ---------- -webkit-text-fill-color ----------

    [Fact]
    public void ADecorationsCurrentColorIsTheFill()
    {
        var document = Document("", """<p id="u" style="color: #333; -webkit-text-fill-color: #36c; text-decoration: underline currentcolor">x</p>""");
        Assert.Equal(new Statventure.Core.Color(0x33 / 255f, 0x66 / 255f, 0xcc / 255f, 1), document.GetElementById("u")!.Style!.AppliedDecorations[^1].Color);
    }

    [Fact]
    public void AChangedFillReachesTheChildren()
    {
        var document = Document("", """<div id="p" style="-webkit-text-fill-color: red"><span id="c">x</span></div>""");
        Assert.Equal("rgb(255, 0, 0)", Custom(document, "c", "-webkit-text-fill-color"));
        document.GetElementById("p")!.SetAttribute("style", "-webkit-text-fill-color: blue");
        document.Update(0);
        Assert.Equal("rgb(0, 0, 255)", Custom(document, "c", "-webkit-text-fill-color"));
    }

    [Fact]
    public void AFillOrAClipChangesOnlyPainting()
    {
        var a = new ComputedStyle();
        var fill = a.Clone();
        fill.TextFillColor = Statventure.Core.Color.Black;
        var clips = a.Clone();
        clips.BackgroundClips = [BackgroundClip.Text];
        var colorClip = a.Clone();
        colorClip.BackgroundColorClip = BackgroundClip.Text;
        Assert.Equal(StyleChange.Paint, StyleDifference.Compare(a, fill));
        Assert.Equal(StyleChange.Paint, StyleDifference.Compare(a, clips));
        Assert.Equal(StyleChange.Paint, StyleDifference.Compare(a, colorClip));
        // Their animations repaint only.
        Assert.Contains("-webkit-text-fill-color", Interpolation.PaintOnly);
        Assert.Contains("background-clip", Interpolation.PaintOnly);
    }

    // ---------- background-clip and the shorthand ----------

    [Theory]
    [InlineData("red", "border-box")]
    [InlineData("red text", "text")]
    [InlineData("red content-box", "content-box")]
    [InlineData("red padding-box content-box", "content-box")]
    [InlineData("text padding-box", "text")]
    [InlineData("linear-gradient(red, blue) text, red", "text,border-box")]
    public void TheBackgroundShorthandSetsTheClip(string value, string clip)
    {
        var expanded = Properties.Expand("background", CssTokenizer.Tokenize(value))!;
        var clips = expanded.Single(e => e.Name == "background-clip").Value;
        Assert.Equal(clip, Text(clips));
    }

    [Fact]
    public void ABackgroundShorthandWithVarSetsTheClipToo()
    {
        var document = Document("""
            .a { --b: linear-gradient(red, blue) text; background: var(--b); }
            .k { --c: red; background-clip: text; background: var(--c); }
            """, """<div class="a" id="a"></div><div class="k" id="k"></div>""");
        Assert.Equal("text", Custom(document, "a", "background-clip"));
        Assert.Equal("border-box", Custom(document, "k", "background-clip"));
    }

    [Fact]
    public void TheWebkitAliasIsBackgroundClip()
    {
        Assert.Equal("background-clip", Properties.Canonical("-webkit-background-clip"));
        Assert.False(Properties.IsIgnored("background-clip"));
        Assert.True(Properties.IsIgnored("background-origin"));
        Assert.False(Stylesheet.Validate("background-clip", CssTokenizer.Tokenize("border-area")));
        Assert.False(Stylesheet.Validate("background-clip", CssTokenizer.Tokenize("text text")));
        Assert.True(Stylesheet.Validate("background-clip", CssTokenizer.Tokenize("text, padding-box")));
    }
}

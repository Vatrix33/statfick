using System.IO.Compression;
using System.Reflection;
using System.Text.Json;
using Statventure.Tests.Infrastructure;
using Statventure.UI.Css;
using Statventure.UI.Dom;
using Statventure.UI.Style;

namespace Statventure.Tests.Ui;

/// <summary>
/// The old game's stylesheet compiled by the UI importer is the stylesheet it was parsed from (Docs/Assets.md,
/// "UI"; Q23): every captured game screen (the 11 states at device scale 1 and at 1.65) gets the same
/// computed styles from both, element by element and pseudo-element by pseudo-element, and the two sheets
/// hold the same rules part for part. (The game screens' own tests, layout, paint, hit testing and
/// compositing, run on the compiled sheet.)
/// </summary>
public class CompiledGameStylesTests
{
    static readonly Dictionary<float, Lazy<JsonElement>> Captures = new[] { 1f, 1.65f }.ToDictionary(scale => scale, scale => new Lazy<JsonElement>(() =>
    {
        var name = scale == 1 ? "game-ui.json.gz" : $"game-ui@{scale.ToString(System.Globalization.CultureInfo.InvariantCulture)}.json.gz";
        using var stream = new GZipStream(File.OpenRead(Path.Combine(Path.GetDirectoryName(Golden.GoldenPath("x"))!, name)), CompressionMode.Decompress);
        return JsonDocument.Parse(stream).RootElement;
    }));

    public static TheoryData<string, float> States
    {
        get
        {
            var data = new TheoryData<string, float>();
            foreach (var (scale, capture) in Captures)
                foreach (var state in capture.Value.GetProperty("states").EnumerateArray())
                    data.Add(state.GetProperty("name").GetString()!, scale);
            return data;
        }
    }

    static readonly PseudoElement[] Parts =
    [
        PseudoElement.Marker, PseudoElement.ProgressInnerElement, PseudoElement.ProgressBar, PseudoElement.ProgressValue,
        PseudoElement.InnerSpinButton, PseudoElement.SearchCancelButton, PseudoElement.SliderContainer,
        PseudoElement.SliderRunnableTrack, PseudoElement.SliderThumb, PseudoElement.FileUploadButton,
    ];

    [Theory]
    [MemberData(nameof(States))]
    public void TheCompiledSheetGivesTheParsedSheetsComputedStyles(string name, float scale)
    {
        var state = Captures[scale].Value.GetProperty("states").EnumerateArray().Single(s => s.GetProperty("name").GetString() == name);
        var parsed = GameLayoutTests.Build(state, deviceScale: scale, styles: GameStylesheets.Parsed);
        var compiled = GameLayoutTests.Build(state, deviceScale: scale, styles: GameStylesheets.Compiled);
        var a = parsed.AllElements().ToList();
        var b = compiled.AllElements().ToList();
        Assert.Equal(a.Count, b.Count);
        var problems = new List<string>();
        var compared = 0;
        for (var i = 0; i < a.Count; i++)
        {
            var (x, y) = (a[i].Styles, b[i].Styles);
            // Elements below display: none aren't styled.
            if (x is null || y is null)
            {
                if (x is not null || y is not null)
                    problems.Add($"element {i} ({a[i].Tag}) is styled from one sheet only");
                continue;
            }
            void Check(string what, ComputedStyle? p, ComputedStyle? q)
            {
                compared++;
                if (!StyleDifference.Identical(p, q))
                    problems.Add($"element {i} ({a[i].Tag}{(a[i].Id is { } id ? "#" + id : "")}) {what}: {Differences(p, q)}");
            }
            Check("style", x.Style, y.Style);
            Check("::before", x.Before, y.Before);
            Check("::after", x.After, y.After);
            Check("::placeholder", x.Placeholder, y.Placeholder);
            Check("::selection", x.Selection, y.Selection);
            foreach (var part in Parts)
                Check("::" + part, x.PartSource?.Invoke(part, x.Style), y.PartSource?.Invoke(part, y.Style));
            if (!x.Important.SetEquals(y.Important))
                problems.Add($"element {i} ({a[i].Tag}): important longhands differ");
        }
        Assert.True(problems.Count == 0, $"{name} at {scale}: {problems.Count} of {compared} styles differ\n" + string.Join("\n", problems.Take(30)));
    }

    /// <summary>The fields of two computed styles that differ, by name.</summary>
    static string Differences(ComputedStyle? a, ComputedStyle? b)
    {
        if (a is null || b is null)
            return a is null ? "only the compiled sheet's exists" : "only the parsed sheet's exists";
        var fields = typeof(ComputedStyle).GetFields(BindingFlags.Public | BindingFlags.Instance)
            .Where(f => !StyleDifference.Identical(Only(f, a), Only(f, b)))
            .Select(f => f.Name)
            .ToList();
        return fields.Count == 0 ? "custom properties" : string.Join(", ", fields);
    }

    /// <summary>A style with only one field copied from <paramref name="from"/>.</summary>
    static ComputedStyle Only(FieldInfo field, ComputedStyle from)
    {
        var style = new ComputedStyle();
        field.SetValue(style, field.GetValue(from));
        return style;
    }

    /// <summary>
    /// The two sheets hold the same rules: every style rule's selectors (their parsed structure), media
    /// query, cascade order and prepared declarations, every keyframes rule and font face; only the
    /// font faces' sources differ (the data paths of the old files).
    /// </summary>
    [Fact]
    public void TheCompiledSheetHoldsTheParsedSheetsRules()
    {
        var parsed = GameStylesheets.Parsed;
        var compiled = GameStylesheets.Compiled;
        Assert.Equal(3159, parsed.Rules.Count);
        Assert.Equal(StylesheetText.Of(parsed, fontSources: false), StylesheetText.Of(compiled, fontSources: false));
        Assert.Equal(
            [
                new FontFaceDescriptors("Lato", "ui/fonts/lato-regular.ttf", 400, 400, false),
                new FontFaceDescriptors("Lato", "ui/fonts/lato-semibold.ttf", 500, 600, false),
                new FontFaceDescriptors("Lato", "ui/fonts/lato-bold.ttf", 700, 900, false),
                new FontFaceDescriptors("STIX Two Text", "ui/fonts/stixtwotext-regular.otf", 400, 400, false),
                new FontFaceDescriptors("STIX Two Text", "ui/fonts/stixtwotext-italic.otf", 400, 400, true),
            ],
            compiled.FontFaces.Select(FontFaceDescriptors.Of));
        Assert.Equal(GameStylesheets.Path, compiled.Name);
        Assert.Equal(2, parsed.DroppedRules);
    }
}

/// <summary>A stylesheet as text, every part spelled out (for comparing sheets part by part).</summary>
public static class StylesheetText
{
    /// <param name="unknown">Whether to list the unknown properties (a compiled sheet has none: compiling refuses them).</param>
    public static string Of(Stylesheet sheet, bool fontSources = true, bool unknown = true)
    {
        var text = new System.Text.StringBuilder();
        foreach (var rule in sheet.Rules)
        {
            text.Append("rule ").Append(rule.Source.Order).Append(' ').Append(Selectors(rule.Source.Selectors));
            text.Append(" tokens ").Append(Tokens(rule.Source.SelectorTokens));
            if (rule.Media is { } media)
                text.Append(" @media ").Append(media.Text).Append(' ').Append(Tokens(media.Tokens));
            text.Append('\n');
            foreach (var d in rule.Declarations)
                text.Append($"  {d.Property} important={d.Important} var={d.HasVar} pending={d.PendingShorthand} keyword={d.Keyword}: {Tokens(d.Value)}\n");
        }
        foreach (var (rule, media) in sheet.Keyframes)
        {
            text.Append($"@keyframes {rule.Name}{(media is null ? "" : " @media " + media.Text + " " + Tokens(media.Tokens))}\n");
            foreach (var frame in rule.Frames)
            {
                text.Append("  ").Append(string.Join(",", frame.Offsets.Select(o => BitConverter.SingleToInt32Bits(o).ToString("X8")))).Append('\n');
                foreach (var d in frame.Declarations)
                    text.Append($"    {d.Property} important={d.Important}: {Tokens(d.Value)}\n");
            }
        }
        foreach (var face in sheet.FontFaces)
        {
            text.Append("@font-face\n");
            foreach (var d in face.Declarations)
                if (fontSources || d.Property != "src")
                    text.Append($"  {d.Property} important={d.Important}: {Tokens(d.Value)}\n");
        }
        foreach (var rule in sheet.PropertyRules)
        {
            text.Append($"@property {Tokens(rule.Prelude)}\n");
            foreach (var d in rule.Descriptors)
                text.Append($"  {d.Property} important={d.Important}: {Tokens(d.Value)}\n");
        }
        if (unknown)
            text.Append($"unknown {string.Join(",", sheet.UnknownProperties)}\n");
        return text.ToString();
    }

    static string Tokens(IEnumerable<CssToken> tokens) =>
        string.Join(" ", tokens.Select(t => $"[{t.Type} {t.Value} {BitConverter.DoubleToInt64Bits(t.Number):X} {t.Unit} {(t.IsInteger ? "i" : "")}{(t.HashIsId ? "id" : "")}]"));

    static string Selectors(SelectorList list) => "{" + string.Join(" | ", list.Selectors.Select(Selector)) + "}";

    static string Selector(Selector s) =>
        $"'{s.Text}' {s.Specificity} ::{s.PseudoElement} " + string.Join(" ", s.Compounds.Select((c, i) =>
            (i > 0 ? s.Combinators[i - 1] + " " : "") + Compound(c)));

    static string Compound(CompoundSelector c) =>
        $"({c.Tag}#{c.Id}.{string.Join(".", c.Classes)} [{string.Join(";", c.Attributes)}] " +
        string.Join(" ", c.PseudoClasses.Select(p => $":{p.Kind}({p.A},{p.B},{p.HasChildCombinator}{(p.Arguments is { } a ? "," + Selectors(a) : "")})")) + ")";
}

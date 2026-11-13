using System.Globalization;
using System.Text.Json;
using Statventure.Tests.Infrastructure;
using Statventure.UI.Dom;
using Statventure.UI.Paint;

namespace Statventure.Tests.Ui;

/// <summary>
/// The rules today's old stylesheet brought (GM1 step 1a; <see cref="StylesheetRuleFixtures"/>): registered custom
/// properties, <c>-webkit-text-fill-color</c> and <c>background-clip: text</c>, each painted as Chromium 152 paints its
/// fixture at 1 and 1.65 (the paint fixtures' limits: 12 levels, 0.2 % of the pixels) and with Chromium's computed
/// values for the fixture's elements.
/// </summary>
[Collection("GPU")]
public class StylesheetRuleReferenceTests(GpuFixture gpu)
{
    static readonly float[] Scales = [1, 1.65f];

    public static TheoryData<string, float> Cases
    {
        get
        {
            var data = new TheoryData<string, float>();
            foreach (var fixture in StylesheetRuleFixtures.All)
                foreach (var scale in Scales)
                    data.Add(fixture.Name, scale);
            return data;
        }
    }

    static string Number(float value) => value.ToString(CultureInfo.InvariantCulture);

    static string ReferenceDirectory => Path.Combine(Path.GetDirectoryName(Golden.GoldenPath("x"))!, "..", "Reference");

    [Fact]
    public void TheFixtureListIsUpToDate()
    {
        var path = Path.Combine(ReferenceDirectory, "stylesheet-rule-fixtures.json");
        var json = JsonSerializer.Serialize(StylesheetRuleFixtures.All.Select(f => new
        {
            name = f.Name, width = f.Width, height = f.Height, css = LayoutFixtures.BaseCss + f.Css, html = f.Html,
            scrollX = 0, scrollY = 0, windowFocused = true, computed = f.Computed, trigger = f.Trigger,
        }));
        if (!File.Exists(path) || File.ReadAllText(path) != json)
            File.WriteAllText(path, json);
        Assert.NotEmpty(json);
    }

    /// <summary>
    /// The fixture at a device scale, its trigger class added after the first style pass and its selections made (as
    /// the harness does: <c>data-select="start end"</c> on a focused field, <c>data-select-from</c> and
    /// <c>data-select-to</c> the page's range in the first text nodes of two elements).
    /// </summary>
    public static UiDocument Build(StylesheetRuleFixture fixture, float scale)
    {
        var document = PaintReferenceTests.Build(fixture.Paint, scale);
        foreach (var element in document.AllElements().ToList())
            if (element.GetAttribute("data-select") is { } range && range.Split(' ') is [var start, var end])
                element.SetSelectionRange(int.Parse(start, CultureInfo.InvariantCulture), int.Parse(end, CultureInfo.InvariantCulture));
        if (document.QuerySelector("[data-select-from]") is { } from && document.QuerySelector("[data-select-to]") is { } to)
            document.SetPageSelection(new DomPosition(from.Children[0], int.Parse(from.GetAttribute("data-select-from")!, CultureInfo.InvariantCulture)),
                new DomPosition(to.Children[0], int.Parse(to.GetAttribute("data-select-to")!, CultureInfo.InvariantCulture)));
        document.Update(0);
        if (fixture.Trigger is { } trigger)
        {
            document.Body.AddClass(trigger);
            document.Update(0);
        }
        return document;
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void TheFixturePaintsAsInChromium(string name, float scale)
    {
        var fixture = StylesheetRuleFixtures.All.Single(f => f.Name == name);
        var document = Build(fixture, scale);
        var image = PaintReferenceTests.Render(gpu.Device, document, (int)MathF.Round(fixture.Width * scale), (int)MathF.Round(fixture.Height * scale));
        Golden.Match($"ui-{name}@{Number(scale)}", image, tolerance: 12, maxDifferentRatio: 0.002);
    }

    /// <summary>
    /// An outside list marker's text is in its list's text mask, as in Chromium (its first capture of
    /// <c>background-clip-text</c> showed "1." through the gradient). Checked in the engine alone: the engine places an
    /// outside marker closer to its item than Chromium does, mask or not, so the fixture's list marker is inside.
    /// </summary>
    [Fact]
    public void AnOutsideMarkersTextIsInTheMask()
    {
        var fixture = new PaintFixture("marker-mask", 200, 50, """
            body { margin: 0; font: 700 26px/32px Lato; background: #fff; }
            ol { margin: 0; padding-left: 60px; background: linear-gradient(#c33, #c33); background-clip: text; -webkit-text-fill-color: transparent; }
            """, "<ol><li>Item</li></ol>");
        var image = PaintReferenceTests.Render(gpu.Device, PaintReferenceTests.Build(fixture, 1), 200, 50);
        var red = 0;
        // Left of the item's content box only the marker is painted.
        for (var y = 0; y < 50; y++)
            for (var x = 0; x < 60; x++)
                if (image[x, y] is { R: > 150, G: < 100 })
                    red++;
        Assert.True(red > 20, $"{red} pixels of the gradient in the marker");
    }

    /// <summary>
    /// A layer clipped to text makes nothing known to be opaque: its save layer ends the display item's opaqueness
    /// analysis (DrawingDisplayItem::CalculateRectKnownToBeOpaqueForRecord). So the composited layer of
    /// <c>background-clip-text</c>'s opaque colour through text isn't opaque, and its text isn't known to be on an
    /// opaque background (Chromium draws it grayscale; what else keeps it so in the engine doesn't show this rule).
    /// </summary>
    [Fact]
    public void ATextClipMakesNothingKnownOpaque()
    {
        var document = Build(StylesheetRuleFixtures.BackgroundClipText, 1);
        using var painter = new UiPainter(document);
        var box = document.GetElementById("layer")!.Box!;
        var layer = painter.AnalyzeLayers().Single(l => l.Chunks.Any(c => c.Owner == box));
        Assert.False(layer.ContentsOpaque);
        Assert.False(layer.TextKnownToBeOnOpaqueBackground);
        Assert.True(layer.RectKnownToBeOpaque.IsEmpty, $"known opaque: {layer.RectKnownToBeOpaque}");
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void TheComputedValuesAreChromiums(string name, float scale)
    {
        var fixture = StylesheetRuleFixtures.All.Single(f => f.Name == name);
        var document = Build(fixture, scale);
        var reference = JsonDocument.Parse(File.ReadAllText(Path.Combine(ReferenceDirectory, "scaled", $"ui-{name}@{Number(scale)}.styles.json"))).RootElement;
        var problems = new List<string>();
        var count = 0;
        foreach (var element in reference.EnumerateObject())
        {
            var el = document.GetElementById(element.Name) ?? throw new InvalidOperationException("no element " + element.Name);
            foreach (var property in element.Value.EnumerateObject())
            {
                count++;
                var expected = property.Value.GetString()!;
                var actual = ComputedValues.Of(el, property.Name);
                if (!Same(property.Name, expected, actual))
                    problems.Add($"#{element.Name} {property.Name}: engine '{actual}', Chromium '{expected}'");
            }
        }
        Assert.True(count > 0);
        Assert.True(problems.Count == 0, $"{problems.Count} of {count} values differ:\n" + string.Join("\n", problems));
    }

    /// <summary>
    /// Equal values: the same text, or for a standard property a colour Chromium keeps in sRGB (a color-mix()'s,
    /// printed as color(srgb r g b)) that the engine's rgb() rounds to (its colours carry no colour space).
    /// </summary>
    static bool Same(string property, string expected, string actual)
    {
        if (expected == actual)
            return true;
        if (property.StartsWith("--", StringComparison.Ordinal) || !expected.StartsWith("color(srgb ", StringComparison.Ordinal) || !actual.StartsWith("rgb(", StringComparison.Ordinal))
            return false;
        var want = expected["color(srgb ".Length..^1].Split(' ').Select(v => double.Parse(v, CultureInfo.InvariantCulture) * 255).ToArray();
        var got = actual["rgb(".Length..^1].Split(", ").Select(v => double.Parse(v, CultureInfo.InvariantCulture)).ToArray();
        return want.Length == 3 && got.Length == 3 && want.Zip(got).All(p => Math.Abs(Math.Round(p.First, MidpointRounding.AwayFromZero) - p.Second) == 0);
    }
}

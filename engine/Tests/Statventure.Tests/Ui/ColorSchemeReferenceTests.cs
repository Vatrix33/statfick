using System.Globalization;
using System.Text.Json;
using Statventure.Tests.Infrastructure;

namespace Statventure.Tests.Ui;

/// <summary>
/// The colour scheme's pages (<see cref="ColorSchemeFixtures"/>), each painted as Chromium 152 paints it at 1 and 1.65
/// (the paint fixtures' limits: the page's tolerance, 0.2 % of the pixels) and with Chromium's computed values for its
/// elements with an id.
/// </summary>
[Collection("GPU")]
public class ColorSchemeReferenceTests(GpuFixture gpu)
{
    static readonly float[] Scales = [1, 1.65f];

    public static TheoryData<string, float> Cases
    {
        get
        {
            var data = new TheoryData<string, float>();
            foreach (var fixture in ColorSchemeFixtures.All)
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
        var path = Path.Combine(ReferenceDirectory, "color-scheme-fixtures.json");
        var json = JsonSerializer.Serialize(ColorSchemeFixtures.All.Select(f => new
        {
            name = f.Name, width = f.Width, height = f.Height, css = LayoutFixtures.BaseCss + f.Css, html = f.Html,
            scrollX = 0, scrollY = 0, windowFocused = f.WindowFocused, computed = f.Computed,
        }));
        if (!File.Exists(path) || File.ReadAllText(path) != json)
            File.WriteAllText(path, json);
        Assert.NotEmpty(json);
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void TheFixturePaintsAsInChromium(string name, float scale)
    {
        var fixture = ColorSchemeFixtures.All.Single(f => f.Name == name);
        var document = PaintReferenceTests.Build(fixture.Paint, scale);
        var image = PaintReferenceTests.Render(gpu.Device, document, (int)MathF.Round(fixture.Width * scale), (int)MathF.Round(fixture.Height * scale));
        Golden.Match($"ui-{name}@{Number(scale)}", image, tolerance: fixture.Tolerance, maxDifferentRatio: 0.002);
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void TheComputedValuesAreChromiums(string name, float scale)
    {
        var fixture = ColorSchemeFixtures.All.Single(f => f.Name == name);
        var document = PaintReferenceTests.Build(fixture.Paint, scale);
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
                if (!StylesheetRuleReferenceTests.Same(property.Name, expected, actual))
                    problems.Add($"#{element.Name} {property.Name}: engine '{actual}', Chromium '{expected}'");
            }
        }
        Assert.True(count > 0);
        Assert.True(problems.Count == 0, $"{problems.Count} of {count} values differ:\n" + string.Join("\n", problems));
    }
}

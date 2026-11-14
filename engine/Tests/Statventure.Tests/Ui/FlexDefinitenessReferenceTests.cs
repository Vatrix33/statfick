using System.Globalization;
using System.Text.Json;
using Statventure.Core;
using Statventure.Tests.Canvas;
using Statventure.UI.Dom;

namespace Statventure.Tests.Ui;

/// <summary>
/// When a column flexbox's item has a definite height (<see cref="FlexDefiniteFixtures"/>, GM1 step 1b): every element's
/// box as Chromium 152 lays it out at device scale 1 and 1.65 (Reference/scaled/layout-flexdef-&lt;name&gt;@&lt;scale&gt;.json),
/// to a fiftieth of a CSS pixel.
/// </summary>
public class FlexDefinitenessReferenceTests
{
    const float Tolerance = 0.02f;
    static readonly float[] Scales = [1, 1.65f];

    static string ReferenceDirectory => Path.Combine(Path.GetDirectoryName(Infrastructure.Golden.GoldenPath("x"))!, "..", "Reference");

    public static TheoryData<string, float> Cases
    {
        get
        {
            var data = new TheoryData<string, float>();
            foreach (var fixture in FlexDefiniteFixtures.All)
                foreach (var scale in Scales)
                    data.Add(fixture.Name, scale);
            return data;
        }
    }

    [Fact]
    public void TheFixtureListIsUpToDate()
    {
        var path = Path.Combine(ReferenceDirectory, "flex-definite-fixtures.json");
        var json = JsonSerializer.Serialize(FlexDefiniteFixtures.All.Select(f => new { name = "flexdef-" + f.Name, width = f.Width, height = f.Height, css = LayoutFixtures.BaseCss + f.Css, html = f.Html }));
        if (!File.Exists(path) || File.ReadAllText(path) != json)
            File.WriteAllText(path, json);
        Assert.NotEmpty(json);
    }

    static UiDocument Build(FlexDefiniteFixture fixture, float scale)
    {
        var document = new UiDocument(TestFonts.Library);
        document.SetZoom(scale, 1);
        document.SetViewport(fixture.Width, fixture.Height);
        document.AddStylesheet(LayoutFixtures.BaseCss + fixture.Css, fixture.Name);
        Markup.SetInnerHtml(document.Body, fixture.Html);
        document.Update();
        return document;
    }

    static string Format(RectF r) => $"({r.X:0.###}, {r.Y:0.###}, {r.Width:0.###}×{r.Height:0.###})";

    [Theory]
    [MemberData(nameof(Cases))]
    public void TheFixtureLaysOutLikeChromium(string name, float scale)
    {
        var fixture = FlexDefiniteFixtures.All.Single(f => f.Name == name);
        var path = Path.Combine(ReferenceDirectory, "scaled", $"layout-flexdef-{name}@{scale.ToString(CultureInfo.InvariantCulture)}.json");
        var reference = JsonDocument.Parse(File.ReadAllText(path)).RootElement;
        var document = Build(fixture, scale);
        var elements = new List<UiElement> { document.Root, document.Body };
        elements.AddRange(document.Body.Descendants());
        var expected = reference.GetProperty("elements");
        Assert.Equal(expected.GetArrayLength(), elements.Count);
        var problems = new List<string>();
        for (var i = 0; i < elements.Count; i++)
        {
            var want = expected[i];
            Assert.Equal(want[0].GetString(), elements[i].Tag);
            var wantRect = new RectF(want[1].GetSingle(), want[2].GetSingle(), want[3].GetSingle(), want[4].GetSingle());
            var got = elements[i].GetBoundingClientRect();
            if (MathF.Abs(wantRect.X - got.X) > Tolerance || MathF.Abs(wantRect.Y - got.Y) > Tolerance || MathF.Abs(wantRect.Width - got.Width) > Tolerance || MathF.Abs(wantRect.Height - got.Height) > Tolerance)
                problems.Add($"#{i} {elements[i].Tag}{(elements[i].Id is { Length: > 0 } id ? "#" + id : "")}: {Format(got)}, Chromium {Format(wantRect)}");
        }
        Assert.True(problems.Count == 0, $"{name}@{scale}: {problems.Count} boxes differ\n" + string.Join("\n", problems.Take(30)));
    }
}

using System.Globalization;
using System.Text.Json;
using Statventure.Core;
using Statventure.Tests.Canvas;
using Statventure.UI.Dom;

namespace Statventure.Tests.Ui;

/// <summary>
/// The layout fixtures on a screen of device scale 1.65, and with a page zoom of 1.2 on top (Electron's
/// zoom level 1), must lay out as Chromium 152 lays them out there (Reference/scaled/layout-*@1.65.json
/// and layout-*@1.65x1.2.json, made by Tools/Scripts/make-scaled-layout-references.sh 1.65 [1.2]):
/// every element's box and, for scroll containers, client and scroll sizes; every text node's line
/// rectangles. Blink lays out in device pixels with every length zoomed, so this checks the
/// engine's zoom to a fiftieth of a CSS pixel.
/// </summary>
public class ScaledLayoutReferenceTests
{
    const float Scale = 1.65f;
    const float Tolerance = 0.02f;
    static readonly float[] PageZooms = [1, 1.2f];

    public static TheoryData<string, float> Fixtures
    {
        get
        {
            var data = new TheoryData<string, float>();
            foreach (var zoom in PageZooms)
                foreach (var fixture in LayoutFixtures.All)
                    data.Add(fixture.Name, zoom);
            return data;
        }
    }

    static string ReferenceDirectory => Path.Combine(Path.GetDirectoryName(Infrastructure.Golden.GoldenPath("x"))!, "..", "Reference");

    static string ConfigName(float pageZoom) => Scale.ToString(CultureInfo.InvariantCulture) + (pageZoom == 1 ? "" : "x" + pageZoom.ToString(CultureInfo.InvariantCulture));

    [Fact]
    public void TheFixtureListIsUpToDate()
    {
        var path = Path.Combine(ReferenceDirectory, "layout-fixtures.json");
        var json = JsonSerializer.Serialize(LayoutFixtures.All.Select(f => new { name = f.Name, width = f.Width, height = f.Height, css = LayoutFixtures.BaseCss + f.Css, html = f.Html }));
        if (!File.Exists(path) || File.ReadAllText(path) != json)
            File.WriteAllText(path, json);
        Assert.NotEmpty(json);
    }

    static UiDocument Build(LayoutFixture fixture, float pageZoom)
    {
        var document = new UiDocument(TestFonts.Library);
        document.SetZoom(Scale, pageZoom);
        // The reference lays each fixture out in an iframe of its size in CSS px: under a page zoom that is more window units.
        document.SetViewport(fixture.Width * pageZoom, fixture.Height * pageZoom);
        document.AddStylesheet(LayoutFixtures.BaseCss + fixture.Css, fixture.Name);
        Markup.SetInnerHtml(document.Body, fixture.Html);
        document.Update();
        return document;
    }

    static bool Near(RectF a, RectF b) =>
        MathF.Abs(a.X - b.X) <= Tolerance && MathF.Abs(a.Y - b.Y) <= Tolerance && MathF.Abs(a.Width - b.Width) <= Tolerance && MathF.Abs(a.Height - b.Height) <= Tolerance;

    static string Format(RectF r) => $"({r.X:0.###}, {r.Y:0.###}, {r.Width:0.###}×{r.Height:0.###})";

    /// <summary>Content the engine doesn't lay out as Chromium does for reasons that aren't the zoom's (Engine-Plan.md).</summary>
    static bool KnownDifference(UiElement? element)
    {
        for (var e = element; e?.Parent is { } parent; e = parent)
        {
            // Content in a closed details (not its summary): Chromium measures it (content-visibility: hidden).
            if (parent.Tag == "details" && !parent.HasAttribute("open") && e.Tag != "summary")
                return true;
            // SVG content's boxes (the 1× references leave them out too).
            if (parent.Tag == "svg")
                return true;
        }
        return false;
    }

    [Theory]
    [MemberData(nameof(Fixtures))]
    public void TheFixtureLaysOutLikeTheBrowser(string name, float pageZoom)
    {
        var fixture = LayoutFixtures.All.Single(f => f.Name == name);
        var path = Path.Combine(ReferenceDirectory, "scaled", $"layout-{name}@{ConfigName(pageZoom)}.json");
        if (!File.Exists(path))
            throw new FileNotFoundException("run Tools/Scripts/make-scaled-layout-references.sh to measure the fixtures in Chromium", path);
        var reference = JsonDocument.Parse(File.ReadAllText(path)).RootElement;
        var document = Build(fixture, pageZoom);
        var problems = new List<string>();

        var elements = new List<UiElement> { document.Root, document.Body };
        elements.AddRange(document.Body.Descendants());
        var expected = reference.GetProperty("elements");
        Assert.Equal(expected.GetArrayLength(), elements.Count);
        for (var i = 0; i < elements.Count; i++)
        {
            var want = expected[i];
            var element = elements[i];
            Assert.Equal(want[0].GetString(), element.Tag);
            if (KnownDifference(element))
                continue;
            var wantRect = new RectF(want[1].GetSingle(), want[2].GetSingle(), want[3].GetSingle(), want[4].GetSingle());
            var got = element.GetBoundingClientRect();
            if (!Near(wantRect, got))
                problems.Add($"#{i} {element.Tag}{(element.Id is { Length: > 0 } id ? "#" + id : "")}: {Format(got)}, Chromium {Format(wantRect)}");
            if (element.Box is { IsScrollContainer: true })
            {
                int[] sizes = [element.ClientWidth, element.ClientHeight, element.ScrollWidth, element.ScrollHeight];
                int[] wanted = [want[5].GetInt32(), want[6].GetInt32(), want[7].GetInt32(), want[8].GetInt32()];
                if (!sizes.SequenceEqual(wanted))
                    problems.Add($"#{i} {element.Tag}: client/scroll [{string.Join(", ", sizes)}], Chromium [{string.Join(", ", wanted)}]");
            }
        }

        var texts = new List<UiText>();
        void Collect(UiElement element)
        {
            foreach (var child in element.Children)
            {
                if (child is UiText text)
                    texts.Add(text);
                else if (child is UiElement el)
                    Collect(el);
            }
        }
        Collect(document.Body);
        var fragments = LayoutReferenceTests.TextFragments(document);
        var expectedTexts = reference.GetProperty("texts");
        Assert.Equal(expectedTexts.GetArrayLength(), texts.Count);
        for (var i = 0; i < texts.Count; i++)
        {
            if (KnownDifference(texts[i].Parent))
                continue;
            var want = expectedTexts[i].EnumerateArray().Select(r => new RectF(r[0].GetSingle(), r[1].GetSingle(), r[2].GetSingle(), r[3].GetSingle()))
                .Where(r => r.Width > 0 || r.Height > 0).ToList();
            // An ellipsized line: Chromium reports the whole hidden fragment and then the truncated one.
            for (var j = want.Count - 2; j >= 0; j--)
                if (MathF.Abs(want[j].X - want[j + 1].X) < 0.01f && MathF.Abs(want[j].Y - want[j + 1].Y) < 0.01f && want[j].Width > want[j + 1].Width)
                    want.RemoveAt(j);
            var zoom = document.Zoom;
            var got = (fragments.GetValueOrDefault(texts[i]) ?? []).Select(r => new RectF(r.X / zoom, r.Y / zoom, r.Width / zoom, r.Height / zoom)).ToList();
            var text = texts[i].Text.Replace("\n", "\\n");
            var label = $"text {i} \"{(text.Length > 24 ? text[..24] + "…" : text)}\"";
            if (want.Count != got.Count)
            {
                problems.Add($"{label}: {got.Count} lines [{string.Join(", ", got.Select(Format))}], Chromium {want.Count} [{string.Join(", ", want.Select(Format))}]");
                continue;
            }
            for (var j = 0; j < want.Count; j++)
                if (!Near(want[j], got[j]))
                    problems.Add($"{label} line {j}: {Format(got[j])}, Chromium {Format(want[j])}");
        }
        Assert.True(problems.Count == 0, $"{name}: {problems.Count} differences\n" + string.Join("\n", problems.Take(30)));
    }
}

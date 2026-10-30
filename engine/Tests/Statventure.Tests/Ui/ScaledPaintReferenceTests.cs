using System.Globalization;
using System.Text.Json;
using Statventure.Graphics;
using Statventure.Imaging;
using Statventure.Tests.Infrastructure;
using Statventure.UI.Dom;
using Statventure.UI.Paint;
using CanvasRenderer = Statventure.Canvas.Canvas;

namespace Statventure.Tests.Ui;

/// <summary>
/// The paint fixtures on a screen of device scale 1.65 (this machine's), and with a page zoom of 1.2
/// on top, must look and lay out as Chromium 152 shows them there (Golden/ui-*@1.65[x1.2].png and
/// Reference/scaled/ui-*@1.65[x1.2].json, made by Tools/Scripts/make-scaled-paint-references.sh
/// 1.65 [1.2]): Blink lays the page out in device pixels with every length zoomed (zoom-for-DSF),
/// so this checks the engine's zoom.
/// </summary>
[Collection("GPU")]
public class ScaledPaintReferenceTests(GpuFixture gpu)
{
    const float Scale = 1.65f;

    static readonly float[] PageZooms = [1, 1.2f];

    public static TheoryData<string, float> Fixtures
    {
        get
        {
            var data = new TheoryData<string, float>();
            foreach (var zoom in PageZooms)
                foreach (var fixture in PaintFixtures.All)
                    data.Add(fixture.Name, zoom);
            return data;
        }
    }

    static string ReferenceDirectory => Path.Combine(Path.GetDirectoryName(Golden.GoldenPath("x"))!, "..", "Reference");

    static string ConfigName(float pageZoom) => Scale.ToString(CultureInfo.InvariantCulture) + (pageZoom == 1 ? "" : "x" + pageZoom.ToString(CultureInfo.InvariantCulture));

    [Fact]
    public void TheFixtureListIsUpToDate()
    {
        var path = Path.Combine(ReferenceDirectory, "paint-fixtures.json");
        var json = JsonSerializer.Serialize(PaintFixtures.All.Select(f => new
        {
            name = f.Name, width = f.Width, height = f.Height, css = LayoutFixtures.BaseCss + f.Css, html = f.Html,
            scrollX = f.ScrollX, scrollY = f.ScrollY, windowFocused = f.WindowFocused,
        }));
        if (!File.Exists(path) || File.ReadAllText(path) != json)
            File.WriteAllText(path, json);
        Assert.NotEmpty(json);
    }

    static (UiDocument Document, RgbaImage Image) Render(GpuDevice device, PaintFixture fixture, float pageZoom)
    {
        var document = PaintReferenceTests.Build(fixture, Scale, pageZoom);
        var width = (int)MathF.Round(fixture.Width * Scale * pageZoom);
        var height = (int)MathF.Round(fixture.Height * Scale * pageZoom);
        using var painter = new UiPainter(document);
        using var canvas = new CanvasRenderer(device);
        using var target = GpuImage.CreateRenderTarget(device, width, height);
        var frame = device.BeginFrame();
        canvas.Begin(width, height);
        painter.Paint(canvas);
        canvas.Render(frame, target);
        device.EndFrame(frame);
        device.WaitIdle();
        return (document, new RgbaImage(width, height, target.ReadPixels()));
    }

    [Theory]
    [MemberData(nameof(Fixtures))]
    public void TheFixturePaintsLikeTheBrowser(string name, float pageZoom)
    {
        var fixture = PaintFixtures.All.Single(f => f.Name == name);
        var (_, image) = Render(gpu.Device, fixture, pageZoom);
        Golden.Match($"ui-{name}@{ConfigName(pageZoom)}", image, tolerance: fixture.Tolerance, maxDifferentRatio: 0.002);
    }

    /// <summary>Elements whose boxes differ from Chromium's for reasons that aren't the zoom's (listed in Engine-Plan.md).</summary>
    static bool KnownDifference(UiElement element)
    {
        // Content in a closed details (not its summary): Chromium measures it (content-visibility: hidden), the engine has no box.
        for (var e = element; e.Parent is { } parent; e = parent)
            if (parent.Tag == "details" && !parent.HasAttribute("open"))
                return e.Tag != "summary";
        return false;
    }

    [Theory]
    [MemberData(nameof(Fixtures))]
    public void TheFixtureLaysOutLikeTheBrowser(string name, float pageZoom)
    {
        var fixture = PaintFixtures.All.Single(f => f.Name == name);
        var document = PaintReferenceTests.Build(fixture, Scale, pageZoom);
        var reference = JsonDocument.Parse(File.ReadAllText(Path.Combine(ReferenceDirectory, "scaled", $"ui-{name}@{ConfigName(pageZoom)}.json"))).RootElement;
        var elements = new[] { document.Body }.Concat(document.Body.Descendants()).ToList();
        Assert.Equal(reference.GetArrayLength(), elements.Count);
        var problems = new List<string>();
        for (var i = 0; i < elements.Count; i++)
        {
            var want = reference[i];
            var element = elements[i];
            Assert.Equal(want[0].GetString(), element.Tag);
            if (KnownDifference(element))
                continue;
            var rect = element.GetBoundingClientRect();
            float[] got = [rect.X, rect.Y, rect.Width, rect.Height];
            for (var k = 0; k < 4; k++)
                if (MathF.Abs(got[k] - want[k + 1].GetSingle()) > 0.02f)
                {
                    problems.Add($"#{i} {element.Tag}: Chromium [{string.Join(", ", Enumerable.Range(1, 4).Select(j => want[j].GetSingle()))}], engine [{string.Join(", ", got)}]");
                    break;
                }
        }
        Assert.True(problems.Count == 0, string.Join("\n", problems));
    }
}

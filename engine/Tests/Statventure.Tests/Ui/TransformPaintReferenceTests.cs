using System.Globalization;
using System.Text;
using System.Text.Json;
using Statventure.Tests.Infrastructure;

namespace Statventure.Tests.Ui;

/// <summary>
/// Boxes under transforms that scale one axis more than the other (scaleX/scaleY, a rotation
/// scaled afterwards, skews), as the game's meters (`scaleX(ratio)`) and the boss intro's
/// progress bar use them: each edge must be antialiased over one device pixel across itself,
/// whatever the other axis does (Golden/ui-box-*@1.png and @1.65.png, made by
/// Tools/Scripts/make-scaled-paint-references.sh 1 1 transform-fixtures.json and 1.65 1
/// transform-fixtures.json: Chromium 152 under X11, CPU raster). Every transformed box sits at a
/// multiple of 20 CSS px, a whole device pixel at 1.65 too: Blink rounds a transformed box's paint
/// offset and drops its fraction along the axes the transform scales, which the engine doesn't
/// yet (Engine-Plan.md, Progress, "Canvas: box antialiasing under non-uniform transforms").
/// </summary>
[Collection("GPU")]
public class TransformPaintReferenceTests(GpuFixture gpu)
{
    static readonly float[] Factors = [0.1f, 0.25f, 0.5f, 0.75f, 1.5f, 3];

    static string Number(float value) => value.ToString(CultureInfo.InvariantCulture);

    /// <summary>Thin bars scaled along their length (and across it), boxes with radii, borders and shadows under one-axis scales.</summary>
    static PaintFixture Scale()
    {
        var html = new StringBuilder();
        // Bars 2 px tall scaled along x from their left end (the meters' transform-origin), 1 px
        // tall ones, and some scaled about their centre (fractional ends) or mirrored.
        var top = 20;
        foreach (var f in Factors)
        {
            html.Append($"""<div class="h" style="top: {top}px; transform: scaleX({Number(f)})"></div>""");
            top += 20;
        }
        foreach (var f in new[] { 0.2f, 2.5f })
        {
            html.Append($"""<div class="h thin" style="top: {top}px; transform: scaleX({Number(f)})"></div>""");
            top += 20;
        }
        foreach (var f in new[] { 0.37f, 1.7f, -0.6f })
        {
            html.Append($"""<div class="h mid" style="top: {top}px; transform: scaleX({Number(f)})"></div>""");
            top += 20;
        }
        // The same across: bars 2 px wide scaled along y.
        var left = 400;
        foreach (var f in Factors)
        {
            html.Append($"""<div class="v" style="left: {left}px; transform: scaleY({Number(f)})"></div>""");
            left += 20;
        }
        foreach (var f in new[] { 0.2f, 2.5f })
        {
            html.Append($"""<div class="v thin" style="left: {left}px; transform: scaleY({Number(f)})"></div>""");
            left += 20;
        }
        foreach (var f in new[] { 0.37f, 1.7f })
        {
            html.Append($"""<div class="v mid" style="left: {left}px; transform: scaleY({Number(f)})"></div>""");
            left += 20;
        }
        // Radii (circular and elliptical), borders, shadows and the meters' look under one-axis
        // scales, beside the same boxes untransformed and uniformly scaled.
        html.Append("""
            <div class="round" style="left: 600px; top: 20px; transform: scaleY(2.5)"></div>
            <div class="round" style="left: 20px; top: 240px; transform: scaleX(2.2)"></div>
            <div class="round" style="left: 200px; top: 240px; transform: scaleY(0.35)"></div>
            <div class="ellipse" style="left: 300px; top: 240px; transform: scaleX(0.4)"></div>
            <div class="bordered" style="left: 360px; top: 240px; transform: scaleY(1.6)"></div>
            <div class="round" style="left: 460px; top: 240px"></div>
            <div class="bordered" style="left: 560px; top: 240px; transform: scale(0.8)"></div>
            <div class="bordered" style="left: 20px; top: 300px; transform: scaleX(2.5)"></div>
            <div class="bordered" style="left: 200px; top: 300px; transform: scaleY(0.3)"></div>
            <div class="rounded-border" style="left: 280px; top: 300px; transform: scale(1.8, 0.6)"></div>
            <div class="shadow" style="left: 400px; top: 300px; transform: scaleX(0.5)"></div>
            <div class="shadow" style="left: 460px; top: 300px; transform: scaleY(2)"></div>
            <div class="bordered" style="left: 560px; top: 300px"></div>
            <div class="meter" style="left: 20px; top: 360px; transform: scaleX(0.42)"></div>
            <div class="meter" style="left: 100px; top: 360px; transform: scaleX(1.3)"></div>
            <div class="h" style="left: 300px; top: 360px"></div>
            """);
        const string css = """
            body { background: #fff; }
            div { position: absolute; }
            .h { left: 20px; width: 120px; height: 2px; background: #1d3f7a; transform-origin: 0 0; }
            .v { top: 20px; width: 2px; height: 60px; background: #7a1d2b; transform-origin: 0 0; }
            .h.thin { height: 1px; }
            .v.thin { width: 1px; }
            .mid { transform-origin: 50% 50%; }
            .h.mid { left: 140px; }
            .v.mid { top: 40px; }
            .round { width: 70px; height: 40px; border-radius: 12px; background: #2a7a4a; transform-origin: 0 0; }
            .ellipse { width: 80px; height: 50px; border-radius: 30px / 12px; background: #8a5a1a; transform-origin: 0 0; }
            .bordered { width: 60px; height: 30px; border: 2px solid #222; background: #f2d98a; transform-origin: 0 0; }
            .rounded-border { width: 50px; height: 36px; border: 3px solid #7a2a2a; border-radius: 10px; background: #e8eef8; transform-origin: 0 0; }
            .shadow { width: 60px; height: 24px; background: #c8b48a; box-shadow: 3px 3px 0 1px #3a3a3a; transform-origin: 0 0; }
            .meter { width: 120px; height: 6px; background: linear-gradient(180deg, #c4473a, #7d1d18); box-shadow: inset 0 1px 0 #ffffff26; transform-origin: 0 0; }
            """;
        return new PaintFixture("box-scale", 680, 380, css, html.ToString());
    }

    /// <summary>
    /// Boxes whose edges slant under the transform: rotated after a one-axis scale, scaled after a
    /// rotation, skewed, beside a uniformly scaled rotation. Chromium's CPU raster fills a box that
    /// doesn't stay a rectangle as a path (Skia's analytic scan converter: area coverage, the path's
    /// points snapped to a quarter pixel down the screen), the canvas by a ramp one pixel wide across
    /// each edge; on the uniformly scaled rotation, which the transforms' rule doesn't touch, that
    /// alone leaves 49 pixels beyond 12 levels at 1× (worst 18) and 124 at 1.65 (worst 27), and the
    /// rounded boxes with borders add Blink's background bleed avoidance, which the painter doesn't
    /// do. So this fixture is held as CanvasReferenceTests holds its rotated box (12 levels, 1% of
    /// the pixels): it catches a misplaced or misshapen box; BoxTransformTests holds the slanted
    /// edges' rule itself.
    /// </summary>
    static PaintFixture Slant()
    {
        const string css = """
            body { background: #fff; }
            div { position: absolute; }
            .b { width: 60px; height: 40px; background: #3a5a8a; }
            .t { width: 120px; height: 2px; background: #222; }
            .tv { width: 2px; height: 70px; background: #222; }
            .r { border-radius: 10px; }
            .w { border: 2px solid #7a2a2a; background: #f2d98a; }
            """;
        const string html = """
            <div class="b" style="left: 40px; top: 40px; transform: rotate(30deg) scaleX(0.3)"></div>
            <div class="b" style="left: 200px; top: 40px; transform: scale(2, 0.5) rotate(30deg)"></div>
            <div class="b" style="left: 360px; top: 40px; transform: skewX(30deg)"></div>
            <div class="b" style="left: 500px; top: 40px; transform: skewY(-20deg)"></div>
            <div class="t" style="left: 20px; top: 160px; transform: rotate(20deg) scaleX(0.5)"></div>
            <div class="tv" style="left: 220px; top: 120px; transform: skewX(40deg)"></div>
            <div class="b r" style="left: 340px; top: 140px; transform: scale(2, 0.5) rotate(30deg)"></div>
            <div class="b w" style="left: 500px; top: 140px; transform: skewX(-25deg)"></div>
            <div class="b" style="left: 60px; top: 240px; transform: rotate(20deg) scale(1.5)"></div>
            <div class="t" style="left: 200px; top: 260px; transform: rotate(-35deg) scaleY(1.8)"></div>
            <div class="b r w" style="left: 380px; top: 240px; transform: rotate(-15deg) scale(1.6, 0.7)"></div>
            """;
        return new PaintFixture("box-slant", 640, 320, css, html);
    }

    public static readonly PaintFixture[] All = [Scale(), Slant()];

    /// <summary>The share of pixels allowed past the tolerance: the paint fixtures' 0.2%, and 1% for the slanted edges (see <see cref="Slant"/>).</summary>
    static double MaxDifferentRatio(string name) => name == "box-slant" ? 0.01 : 0.002;

    static readonly float[] Scales = [1, 1.65f];

    public static TheoryData<string, float> Fixtures
    {
        get
        {
            var data = new TheoryData<string, float>();
            foreach (var fixture in All)
                foreach (var scale in Scales)
                    data.Add(fixture.Name, scale);
            return data;
        }
    }

    static string ReferenceDirectory => Path.Combine(Path.GetDirectoryName(Golden.GoldenPath("x"))!, "..", "Reference");

    [Fact]
    public void TheFixtureListIsUpToDate()
    {
        var path = Path.Combine(ReferenceDirectory, "transform-fixtures.json");
        var json = JsonSerializer.Serialize(All.Select(f => new
        {
            name = f.Name, width = f.Width, height = f.Height, css = LayoutFixtures.BaseCss + f.Css, html = f.Html,
            scrollX = f.ScrollX, scrollY = f.ScrollY, windowFocused = f.WindowFocused,
        }));
        if (!File.Exists(path) || File.ReadAllText(path) != json)
            File.WriteAllText(path, json);
        Assert.NotEmpty(json);
    }

    [Theory]
    [MemberData(nameof(Fixtures))]
    public void TheBoxesLookAsInChromium(string name, float scale)
    {
        var fixture = All.Single(f => f.Name == name);
        var document = PaintReferenceTests.Build(fixture, scale);
        var image = PaintReferenceTests.Render(gpu.Device, document, (int)MathF.Round(fixture.Width * scale), (int)MathF.Round(fixture.Height * scale));
        Golden.Match($"ui-{name}@{Number(scale)}", image, tolerance: fixture.Tolerance, maxDifferentRatio: MaxDifferentRatio(name));
    }
}

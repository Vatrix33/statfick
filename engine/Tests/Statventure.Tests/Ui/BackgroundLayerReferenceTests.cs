using System.Globalization;
using System.Text.Json;
using Statventure.Tests.Infrastructure;

namespace Statventure.Tests.Ui;

/// <summary>
/// Background image layers (gradients, SVG images with and without a natural size) at fractional edges,
/// untransformed and under scaleX, square and rounded, with opaque, translucent and dashed borders;
/// sized, contained and covering tiles; repeat, round, space and no-repeat at fractional places; a
/// single tile drawn from inside it; two layers over a colour; fixed attachment (composited at 1.65,
/// scrolled under a transform); and the game's meter look, as Chromium 152 paints them
/// (Golden/ui-bg-layers@1.png and @1.65.png, made by Tools/Scripts/make-scaled-paint-references.sh 1 1
/// background-fixtures.json and 1.65 1 background-fixtures.json).
/// </summary>
[Collection("GPU")]
public class BackgroundLayerReferenceTests(GpuFixture gpu)
{
    const string Checker = "url(\"data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='10' height='10'%3E%3Crect width='10' height='10' fill='%23e8d9b8'/%3E%3Crect width='5' height='5' fill='%237a4a2a'/%3E%3Crect x='5' y='5' width='5' height='5' fill='%237a4a2a'/%3E%3C/svg%3E\")";

    /// <summary>An SVG with a viewBox only: an aspect ratio (4:3) and no natural size.</summary>
    const string Picture = "url(\"data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 4 3'%3E%3Crect width='4' height='3' fill='%23e8d9b8'/%3E%3Ccircle cx='2' cy='1.5' r='1.2' fill='%232a5a8a'/%3E%3C/svg%3E\")";

    static readonly string Css = $$"""
        body { background: #fff; }
        .t { position: absolute; transform-origin: 0 0; }
        .m { position: absolute; width: 120px; height: 8px; border: 1px solid #3a2410aa; background: #3a26148a; overflow: hidden; }
        .m span { display: block; width: 100%; height: 100%; transform-origin: left; background: linear-gradient(180deg, #c4473a, #7d1d18); box-shadow: inset 0 1px 0 #ffffff26; }
        .g { position: absolute; transform-origin: 0 0; width: 90px; height: 20px; background: linear-gradient(90deg, #2a7a4a, #c8b48a); }
        .r { border-radius: 6px; }
        .b { border: 2px solid #333; }
        .sized { background-size: 30px 100%; background-repeat: repeat-x; }
        .placed { background-size: 40% 60%; background-repeat: no-repeat; background-position: 3.5px 2.5px; background-color: #eee; }
        .checker { background: {{Checker}}; background-size: 10px 10px; }
        .radial { background: radial-gradient(circle at 30% 40%, #fd8, #a53 70%); }
        .round { background: linear-gradient(90deg, #2a7a4a, #c8b48a) 0 0 / 24px 100% round; }
        .space { background: {{Checker}} 0 0 / 12px 12px space; }
        .two { background: linear-gradient(90deg, #2a7a4a, #c8b48a) 0 0 / 50% 100% no-repeat, #8ac; }
        .tb { border: 2px solid rgba(51, 51, 51, 0.33); }
        .dots { background: radial-gradient(circle, #a53 40%, rgba(255, 221, 136, 0) 42%) 5.5px 3.25px / 18px 14px; }
        .corner { background: linear-gradient(#2a7a4a, #c8b48a) right 7.5px bottom 3px / 30px 10px no-repeat #eee; }
        .b3 { border: 3px solid #333; background-size: 20px 100%; }
        .d3 { border: 3px dashed #333; background-size: 20px 100%; }
        .fix { background: {{Checker}} 0 0 / 10px 10px fixed; }
        .contain { background: {{Checker}} center / contain no-repeat #eee; }
        .cover { background: {{Picture}} center / cover no-repeat; }
        .containv { background: {{Picture}} 0 0 / contain; }
        .autov { background: {{Picture}}; }
        .nudge { background: linear-gradient(90deg, #2a7a4a, #c8b48a) 3.3px 2.3px / 35.3px 10.3px no-repeat #eee; }
        .inside { background: linear-gradient(90deg, #2a7a4a, #c8b48a) -12.25px -3.5px / 110px 26px; }
        """;

    const string Html = """
        <div class="m" style="left: 20.5px; top: 20.3px"><span style="transform: scaleX(0.42)"></span></div>
        <div class="m" style="left: 20.5px; top: 40.6px"><span style="transform: scaleX(0.75)"></span></div>
        <div class="g" style="left: 180.3px; top: 20.6px"></div>
        <div class="g" style="left: 300.5px; top: 20.5px; transform: scaleX(0.6)"></div>
        <div class="g r" style="left: 420.3px; top: 20.6px"></div>
        <div class="g r" style="left: 540.5px; top: 20.5px; transform: scaleX(0.6)"></div>
        <div class="g r b" style="left: 180.3px; top: 60.6px"></div>
        <div class="g r b" style="left: 300.5px; top: 60.5px; transform: scaleX(0.6)"></div>
        <div class="g sized" style="left: 420.3px; top: 60.6px"></div>
        <div class="g sized" style="left: 540.5px; top: 60.5px; transform: scaleX(0.6)"></div>
        <div class="g placed" style="left: 20.5px; top: 100.3px"></div>
        <div class="g checker" style="left: 180.3px; top: 100.6px"></div>
        <div class="g checker" style="left: 300.5px; top: 100.5px; transform: scaleX(0.6)"></div>
        <div class="g r radial" style="left: 420.3px; top: 100.6px"></div>
        <div class="g placed" style="left: 540.5px; top: 100.5px; transform: scaleX(0.6)"></div>
        <div class="g round" style="left: 20.3px; top: 140.6px"></div>
        <div class="g space" style="left: 180.5px; top: 140.3px"></div>
        <div class="g r two" style="left: 300.3px; top: 140.6px"></div>
        <div class="g r tb" style="left: 420.5px; top: 140.5px"></div>
        <div class="g dots" style="left: 540.3px; top: 140.6px"></div>
        <div class="g corner" style="left: 20.5px; top: 180.3px"></div>
        <div class="g b3" style="left: 180.3px; top: 180.6px"></div>
        <div class="g d3" style="left: 300.5px; top: 180.5px"></div>
        <div class="g fix" style="left: 420.3px; top: 180.6px"></div>
        <div class="g dots" style="left: 540.5px; top: 180.5px; transform: scaleX(0.6)"></div>
        <div class="g contain" style="left: 20.3px; top: 220.6px"></div>
        <div class="g cover" style="left: 180.5px; top: 220.3px"></div>
        <div class="g containv" style="left: 300.3px; top: 220.6px"></div>
        <div class="g autov" style="left: 420.5px; top: 220.5px"></div>
        <div class="g fix" style="left: 540.3px; top: 220.6px; transform: scaleX(0.6)"></div>
        <div class="g nudge" style="left: 20px; top: 260px"></div>
        <div class="g inside" style="left: 180.3px; top: 260.6px"></div>
        <div class="g r inside" style="left: 300.5px; top: 260.5px"></div>
        <div class="g r b" style="left: 420.5px; top: 260.5px; background-color: #e0f"></div>
        """;

    public static readonly PaintFixture[] All = [new("bg-layers", 660, 300, Css, Html)];

    static readonly float[] Scales = [1, 1.65f];

    public static TheoryData<float> ScaleData => new(Scales);

    static string Number(float value) => value.ToString(CultureInfo.InvariantCulture);

    static string ReferenceDirectory => Path.Combine(Path.GetDirectoryName(Golden.GoldenPath("x"))!, "..", "Reference");

    [Fact]
    public void TheFixtureListIsUpToDate()
    {
        var path = Path.Combine(ReferenceDirectory, "background-fixtures.json");
        var json = JsonSerializer.Serialize(All.Select(f => new
        {
            name = f.Name, width = f.Width, height = f.Height, css = LayoutFixtures.BaseCss + f.Css, html = f.Html,
            scrollX = f.ScrollX, scrollY = f.ScrollY, windowFocused = f.WindowFocused,
        }));
        if (!File.Exists(path) || File.ReadAllText(path) != json)
            File.WriteAllText(path, json);
        Assert.NotEmpty(json);
    }

    /// <summary>
    /// A pattern is its box's drawing (Blink records the pattern's DrawRect in the box's display item):
    /// a box whose only paint is a repeated gradient, in a layer of its own, draws content.
    /// </summary>
    [Fact]
    public void APatternIsItsBoxsDrawing()
    {
        var fixture = new PaintFixture("bg-pattern-layer", 200, 80, ".p { position: absolute; left: 10px; top: 10px; width: 90px; height: 20px; will-change: transform; background: linear-gradient(90deg, #2a7a4a, #c8b48a) 0 0 / 30px 100%; }",
            "<div class=\"p\"></div>");
        var document = PaintReferenceTests.Build(fixture, 1);
        using var painter = new Statventure.UI.Paint.UiPainter(document);
        var layers = painter.AnalyzeLayers();
        Assert.Contains(layers, layer => layer.DrawsContent && layer.Chunks.Any(chunk => chunk.Owner?.Element?.GetAttribute("class") == "p"));
    }

    [Theory]
    [MemberData(nameof(ScaleData))]
    public void TheLayersLookAsInChromium(float scale)
    {
        var fixture = All.Single();
        var document = PaintReferenceTests.Build(fixture, scale);
        var image = PaintReferenceTests.Render(gpu.Device, document, (int)MathF.Round(fixture.Width * scale), (int)MathF.Round(fixture.Height * scale));
        Golden.Match($"ui-{fixture.Name}@{Number(scale)}", image, tolerance: fixture.Tolerance, maxDifferentRatio: 0.002);
    }

    /// <summary>The fixture's elements: border box (CSS px), scaleX, border radius, and whether the per-element check covers it.</summary>
    static readonly (string Name, float X, float Y, float Width, float Height, float ScaleX, float Radius, bool Checked)[] Elements =
    [
        ("m1", 20.5f, 20.3f, 122, 10, 1, 0, true), ("m2", 20.5f, 40.6f, 122, 10, 1, 0, true),
        ("g", 180.3f, 20.6f, 90, 20, 1, 0, true), ("g.6", 300.5f, 20.5f, 90, 20, 0.6f, 0, true),
        ("gr", 420.3f, 20.6f, 90, 20, 1, 6, true), ("gr.6", 540.5f, 20.5f, 90, 20, 0.6f, 6, true),
        ("grb", 180.3f, 60.6f, 94, 24, 1, 6, true), ("grb.6", 300.5f, 60.5f, 94, 24, 0.6f, 6, true),
        ("sized", 420.3f, 60.6f, 90, 20, 1, 0, true), ("sized.6", 540.5f, 60.5f, 90, 20, 0.6f, 0, true),
        ("placed", 20.5f, 100.3f, 90, 20, 1, 0, true), ("checker", 180.3f, 100.6f, 90, 20, 1, 0, true),
        ("checker.6", 300.5f, 100.5f, 90, 20, 0.6f, 0, true), ("radial", 420.3f, 100.6f, 90, 20, 1, 6, true),
        ("placed.6", 540.5f, 100.5f, 90, 20, 0.6f, 0, true),
        ("round", 20.3f, 140.6f, 90, 20, 1, 0, true), ("space", 180.5f, 140.3f, 90, 20, 1, 0, true),
        ("two", 300.3f, 140.6f, 90, 20, 1, 6, true), ("tb", 420.5f, 140.5f, 94, 24, 1, 6, true),
        ("dots", 540.3f, 140.6f, 90, 20, 1, 0, true), ("corner", 20.5f, 180.3f, 90, 20, 1, 0, true),
        ("b3", 180.3f, 180.6f, 96, 26, 1, 0, true),
        // (The dashed border's dash ends differ from Chromium's at 1.65: the border painter's, not the background's.)
        ("d3", 300.5f, 180.5f, 96, 26, 1, 0, false),
        ("fix", 420.3f, 180.6f, 90, 20, 1, 0, true), ("dots.6", 540.5f, 180.5f, 90, 20, 0.6f, 0, true),
        // (The circle in the ratio-only SVG is a path: the canvas's path antialiasing, a known difference.)
        ("contain", 20.3f, 220.6f, 90, 20, 1, 0, true), ("cover", 180.5f, 220.3f, 90, 20, 1, 0, false),
        ("containv", 300.3f, 220.6f, 90, 20, 1, 0, false), ("autov", 420.5f, 220.5f, 90, 20, 1, 0, false),
        ("fix.6", 540.3f, 220.6f, 90, 20, 0.6f, 0, true),
        ("nudge", 20, 260, 90, 20, 1, 0, true), ("inside", 180.3f, 260.6f, 90, 20, 1, 0, true),
        ("inside.r", 300.5f, 260.5f, 90, 20, 1, 6, true),
        // (A colour under an opaque rounded border: shrinking bleed avoidance keeps it out of the border's outer edge.)
        ("grbc", 420.5f, 260.5f, 94, 24, 1, 6, true),
    ];

    /// <summary>
    /// Element by element (its box and 2 px around it), at most 2 pixels differ from Chromium by more
    /// than 12 levels outside its rounded corners (rounded corners: the canvas's antialiasing, a known
    /// difference; the 2 pixels: a square corner's product coverage, also known). The whole-image limit
    /// above can't see one element's edges going wrong (a column of 20 pixels is 0.01 % of the image).
    /// </summary>
    [Theory]
    [MemberData(nameof(ScaleData))]
    public void EachElementMatchesChromium(float scale)
    {
        var fixture = All.Single();
        var document = PaintReferenceTests.Build(fixture, scale);
        var image = PaintReferenceTests.Render(gpu.Device, document, (int)MathF.Round(fixture.Width * scale), (int)MathF.Round(fixture.Height * scale));
        var golden = Statventure.Imaging.ImageFile.Load(Golden.GoldenPath($"ui-{fixture.Name}@{Number(scale)}"));
        Assert.Equal((golden.Width, golden.Height), (image.Width, image.Height));
        var problems = new List<string>();
        foreach (var e in Elements.Where(e => e.Checked))
        {
            float x0 = e.X * scale, y0 = e.Y * scale, x1 = (e.X + e.Width * e.ScaleX) * scale, y1 = (e.Y + e.Height) * scale;
            float rx = e.Radius * e.ScaleX * scale, ry = e.Radius * scale;
            var beyond = 0;
            var worst = 0;
            for (var y = (int)MathF.Floor(y0) - 2; y <= (int)MathF.Ceiling(y1) + 2; y++)
                for (var x = (int)MathF.Floor(x0) - 2; x <= (int)MathF.Ceiling(x1) + 2; x++)
                {
                    if (x < 0 || y < 0 || x >= image.Width || y >= image.Height)
                        continue;
                    var corner = rx > 0 && (x < x0 + rx + 1 || x > x1 - rx - 2) && (y < y0 + ry + 1 || y > y1 - ry - 2);
                    if (corner)
                        continue;
                    var i = (y * image.Width + x) * 4;
                    var d = 0;
                    for (var c = 0; c < 4; c++)
                        d = Math.Max(d, Math.Abs(image.Pixels[i + c] - golden.Pixels[i + c]));
                    worst = Math.Max(worst, d);
                    if (d > 12)
                        beyond++;
                }
            if (beyond > 2)
                problems.Add($"{e.Name}: {beyond} pixels beyond 12 levels (worst {worst})");
        }
        Assert.True(problems.Count == 0, $"@{Number(scale)}: {string.Join("; ", problems)}");
    }

    /// <summary>
    /// Shrinking bleed avoidance (BoxDecorationData::ComputeBleedAvoidance under an opaque rounded border):
    /// the magenta under the grey border is clipped half the border in, so none shows in the border's
    /// antialiased outer edge at the rounded corners, where Chromium shows none (blue over green stays
    /// within 12 levels of Chromium's in every pixel of the box). The corners are left out of the
    /// per-element check, so this one looks at them for the colour alone.
    /// </summary>
    [Theory]
    [MemberData(nameof(ScaleData))]
    public void AColourDoesNotBleedThroughAnOpaqueRoundedBorder(float scale)
    {
        var fixture = All.Single();
        var document = PaintReferenceTests.Build(fixture, scale);
        var image = PaintReferenceTests.Render(gpu.Device, document, (int)MathF.Round(fixture.Width * scale), (int)MathF.Round(fixture.Height * scale));
        var golden = Statventure.Imaging.ImageFile.Load(Golden.GoldenPath($"ui-{fixture.Name}@{Number(scale)}"));
        var e = Elements.Single(e => e.Name == "grbc");
        int Magenta(byte[] pixels, int i) => pixels[i + 2] - pixels[i + 1];
        var worst = int.MinValue;
        for (var y = (int)MathF.Floor(e.Y * scale) - 2; y <= (int)MathF.Ceiling((e.Y + e.Height) * scale) + 2; y++)
            for (var x = (int)MathF.Floor(e.X * scale) - 2; x <= (int)MathF.Ceiling((e.X + e.Width) * scale) + 2; x++)
            {
                var i = (y * image.Width + x) * 4;
                worst = Math.Max(worst, Magenta(image.Pixels, i) - Magenta(golden.Pixels, i));
            }
        Assert.True(worst <= 12, $"@{Number(scale)}: the colour shows {worst} levels more than in Chromium");
    }
}

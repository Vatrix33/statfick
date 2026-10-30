using System.Text;
using System.Text.Json;
using Statventure.Tests.Infrastructure;

namespace Statventure.Tests.Ui;

/// <summary>
/// Glyphs at a device scale of 1, where Chromium 152 on Linux hints text (fontconfig's slight
/// hinting: skrifa's autohinter for TrueType fonts) and puts glyphs on whole pixels: sheets of
/// every letter, digit and sign the game writes, at the sizes it uses, spaced apart so each glyph
/// shows on its own (Golden/ui-text-*@1.png, made by Tools/Scripts/make-scaled-paint-references.sh
/// 1 1 text-fixtures.json: Chromium under X11, CPU raster).
/// </summary>
[Collection("GPU")]
public class TextHintingReferenceTests(GpuFixture gpu)
{
    const string Characters = "abcdefghijklmnopqrstuvwxyz ABCDEFGHIJKLMNOPQRSTUVWXYZ 0123456789 !\"#$%&'()*+,-./:;<=>?@[\\]^_`{|}~ äöüÄÖÜß€éèàçñ–—…·×";

    static readonly int[] Sizes = [9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 20, 22, 24, 28];

    static PaintFixture Sheet(string name, string family, int weight, string style = "normal")
    {
        var html = new StringBuilder();
        foreach (var size in Sizes)
            html.Append($"<p style=\"font-size: {size}px\">{System.Net.WebUtility.HtmlEncode(Characters)}</p>");
        var css = $"html {{ overflow: hidden; }} body {{ margin: 4px; font-family: {family}; font-weight: {weight}; font-style: {style}; color: #000; background: #fff; }} p {{ margin: 0 0 2px; letter-spacing: 3px; white-space: nowrap; }}";
        return new PaintFixture(name, 1100, 360, css, html.ToString(), Tolerance: 0);
    }

    const string OpacityCss = "body { margin: 6px; font: 16px Lato; color: #000; background: #fff; } p { margin: 0 0 6px; } .box { padding: 4px; margin-bottom: 6px; }";

    const string OpacityHtml = """
        <p>Plain text on the page</p>
        <div class="box" style="opacity: 0.5"><p>Half opaque text</p></div>
        <div class="box" style="opacity: 0.8; background: #fdc"><p>Opacity over its own background</p><div style="opacity: 0.6"><p>Nested opacity, <b>bold</b></p></div></div>
        <div class="box" style="opacity: 0.7"><p style="color: #a33">Coloured text at 0.7</p></div>
        """;

    public static readonly PaintFixture[] All =
    [
        Sheet("text-lato", "Lato", 400),
        Sheet("text-lato-bold", "Lato", 700),
        // STIX Two Text has no bold face: bold is synthetic (Skia strokes the outline, stroke-and-fill).
        Sheet("text-stix", "'STIX Two Text'", 400),
        Sheet("text-stix-bold", "'STIX Two Text'", 700),
        // Chromium keeps LCD text in opacity layers (cc starts them from the backdrop)…
        // (Composited layers round differently in Skia's 8-bit CPU pipeline: up to 3 levels.)
        // Opacity layers: the GPU blend differs from Skia's CPU blend by up to 3 levels here (radv) and 4 on Mesa's CPU Vulkan.
        new("text-opacity", 400, 180, OpacityCss, OpacityHtml, Tolerance: 4),
        // …unless the layer's display list also has a blend mode or filter layer; then the
        // isolation group Blink makes around the blending element turns the page's text grayscale.
        new("text-opacity-blend", 400, 210, OpacityCss, OpacityHtml + """<div style="mix-blend-mode: multiply; background: #cdf; height: 20px"></div>""", Tolerance: 4),
    ];

    public static TheoryData<string> Fixtures => new(All.Select(f => f.Name));

    static string ReferenceDirectory => Path.Combine(Path.GetDirectoryName(Golden.GoldenPath("x"))!, "..", "Reference");

    [Fact]
    public void TheFixtureListIsUpToDate()
    {
        var path = Path.Combine(ReferenceDirectory, "text-fixtures.json");
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
    public void TheGlyphsLookAsInChromium(string name)
    {
        var fixture = All.Single(f => f.Name == name);
        var document = PaintReferenceTests.Build(fixture);
        // Skia's CPU blitter floors the LCD blend, the GPU rounds it (one level); Blink sums glyph
        // positions in another order, so a glyph within a float ulp of a quarter pixel can land on
        // the next (about one in 2,500).
        Golden.Match($"ui-{name}@1", PaintReferenceTests.Render(gpu.Device, document, fixture.Width, fixture.Height), tolerance: fixture.Tolerance == 0 ? 1 : fixture.Tolerance, maxDifferentRatio: 0.0005);
    }

    /// <summary>
    /// The STIX sheets on this machine's 1.65 screen as well (Golden/ui-text-stix*@1.65.png,
    /// make-scaled-paint-references.sh 1.65 1 with those two fixtures): Chromium doesn't hint there,
    /// so the CFF outlines agree, and synthetic bold is Skia's stroke-and-fill of the outline.
    /// </summary>
    [Theory]
    [InlineData("text-stix")]
    [InlineData("text-stix-bold")]
    [InlineData("text-lato")]
    public void TheGlyphsLookAsInChromiumOnTheScaledScreen(string name)
    {
        const float scale = 1.65f;
        var fixture = All.Single(f => f.Name == name);
        var document = PaintReferenceTests.Build(fixture, scale);
        var image = PaintReferenceTests.Render(gpu.Device, document, (int)MathF.Round(fixture.Width * scale), (int)MathF.Round(fixture.Height * scale));
        Golden.Match($"ui-{name}@1.65", image, tolerance: 1, maxDifferentRatio: 0.0005);
    }
}

using System.Globalization;
using System.Text.Json;
using Statventure.Imaging;
using Statventure.Tests.Infrastructure;

namespace Statventure.Tests.Ui;

/// <summary>
/// Text that is the first drawing of a paint chunk takes that chunk's LCD verdict, as Chromium 152 rasters
/// each display item in the cc layer its chunk went to (cc's tile raster: <c>use_lcd_text</c> per layer):
/// text first in an <c>overflow: hidden</c> or <c>clip</c> box (with and without its own background), in a
/// clip-path box, under a CSS clip, in a scroller, in a <c>will-change: transform</c> box (grayscale by rule),
/// in an opacity box (an opacity layer keeps LCD), after another
/// drawing in the same chunk, after a stacked layer's subsequence (in other properties, and in the same ones:
/// a subsequence made for its hit testing), after a layer with LCD text in a layer without it, and the
/// editor's status line;
/// each on the page's opaque background (left) and in a transparent composited layer of its own (right),
/// as Chromium 152 paints them (Golden/ui-chunk-text-lcd@1.png and @1.65.png, made by
/// Tools/Scripts/make-scaled-paint-references.sh 1 1 chunk-text-fixtures.json and 1.65 1 chunk-text-fixtures.json).
/// </summary>
[Collection("GPU")]
public class ChunkTextLcdReferenceTests(GpuFixture gpu)
{
    const string Css = """
        html { background: #fff; }
        body { font: 13px/20px Lato; color: #222; }
        .k { position: absolute; width: 170px; height: 20px; padding: 0 4px; box-sizing: border-box; white-space: nowrap; }
        .w { position: absolute; width: 170px; height: 20px; will-change: opacity; }
        .w > .k { left: 0; top: 0; }
        .hidden { overflow: hidden; }
        .clip { overflow: clip; }
        .paper { background: #f4ecd8; }
        .cp { clip-path: inset(0); }
        .cssclip { clip: rect(0px, 170px, 20px, 0px); }
        .scroll { overflow: auto; }
        .wct { will-change: transform; }
        .op { opacity: 0.6; }
        .dot { display: inline-block; width: 8px; height: 8px; margin-right: 4px; background: #c84; }
        .p { z-index: 0; padding-left: 18px; }
        .s { position: absolute; left: 4px; top: 5px; width: 10px; height: 10px; z-index: -1; opacity: 0.9; background: #8ac; }
        .rev { will-change: opacity; padding-left: 60px; }
        .sl { position: absolute; left: 0; top: 0; width: 56px; height: 20px; padding-left: 4px; box-sizing: border-box; z-index: -1; will-change: opacity; background: #e8f0e0; }
        .h { position: absolute; left: 4px; top: 5px; width: 10px; height: 10px; z-index: -1; pointer-events: none; background: #8ac; }
        .dark { background: #1a1c1a; color: #aaa9a1; }
        """;

    /// <summary>The cases: name, the case box's classes, its content.</summary>
    static readonly (string Name, string Classes, string Content)[] Cases =
    [
        ("hidden", "hidden", "Hidden: text first"),
        ("clip", "clip", "Clip: text first"),
        ("hidden-paper", "hidden paper", "Hidden on its own paper"),
        ("clip-path", "cp", "Clip-path: text first"),
        ("css-clip", "cssclip", "CSS clip: text first"),
        ("scroller", "scroll", "Scroller: text first<br>and a second line"),
        ("will-change", "hidden wct", "Will-change: transform"),
        ("opacity", "hidden op", "Opacity 0.6: text first"),
        ("after-drawing", "hidden", "<span class=\"dot\"></span>After a drawing"),
        ("after-layer", "p", "<div class=\"s\"></div>After a layer"),
        ("after-hit-layer", "p", "<div class=\"h\"></div>After a layer, same state"),
        ("after-lcd-layer", "rev", "<div class=\"sl\">Layer</div>After LCD text"),
        ("status", "hidden dark", "No pack open"),
    ];

    const int Left = 20, Right = 220, Top = 20, Pitch = 40;

    static string Html()
    {
        var html = new System.Text.StringBuilder();
        // The page's opaque background: every case straight on the page.
        for (var i = 0; i < Cases.Length; i++)
            html.Append($"<div class=\"k {Cases[i].Classes}\" style=\"left: {Left}px; top: {Top + i * Pitch}px\">{Cases[i].Content}</div>\n");
        // A transparent background: every case in a composited layer of its own without a background.
        for (var i = 0; i < Cases.Length; i++)
            html.Append($"<div class=\"w\" style=\"left: {Right}px; top: {Top + i * Pitch}px\"><div class=\"k {Cases[i].Classes}\">{Cases[i].Content}</div></div>\n");
        return html.ToString();
    }

    public static readonly PaintFixture[] All = [new("chunk-text-lcd", 420, 560, Css, Html())];

    static readonly float[] Scales = [1, 1.65f];

    public static TheoryData<float> ScaleData => new(Scales);

    static string Number(float value) => value.ToString(CultureInfo.InvariantCulture);

    static string ReferenceDirectory => Path.Combine(Path.GetDirectoryName(Golden.GoldenPath("x"))!, "..", "Reference");

    [Fact]
    public void TheFixtureListIsUpToDate()
    {
        var path = Path.Combine(ReferenceDirectory, "chunk-text-fixtures.json");
        var json = JsonSerializer.Serialize(All.Select(f => new
        {
            name = f.Name, width = f.Width, height = f.Height, css = LayoutFixtures.BaseCss + f.Css, html = f.Html,
            scrollX = f.ScrollX, scrollY = f.ScrollY, windowFocused = f.WindowFocused,
        }));
        if (!File.Exists(path) || File.ReadAllText(path) != json)
            File.WriteAllText(path, json);
        Assert.NotEmpty(json);
    }

    RgbaImage Render(float scale)
    {
        var fixture = All.Single();
        var document = PaintReferenceTests.Build(fixture, scale);
        return PaintReferenceTests.Render(gpu.Device, document, (int)MathF.Round(fixture.Width * scale), (int)MathF.Round(fixture.Height * scale));
    }

    [Theory]
    [MemberData(nameof(ScaleData))]
    public void TheTextLooksAsInChromium(float scale)
    {
        var fixture = All.Single();
        Golden.Match($"ui-{fixture.Name}@{Number(scale)}", Render(scale), tolerance: fixture.Tolerance, maxDifferentRatio: 0.002);
    }

    /// <summary>
    /// Case by case, in both columns, no pixel of the case's box differs from Chromium's by more than 12
    /// levels: text drawn grayscale where Chromium draws it with LCD antialiasing (or the other way) differs
    /// by tens of levels in hundreds of pixels, which the whole-image limit above can let through for one case
    /// (the status line's text alone is 435 pixels at 1×, 0.18 % of the image). The scroller's bar is left to
    /// the scrollbar fixtures.
    /// </summary>
    [Theory]
    [MemberData(nameof(ScaleData))]
    public void EachCaseMatchesChromium(float scale)
    {
        var fixture = All.Single();
        var image = Render(scale);
        var golden = ImageFile.Load(Golden.GoldenPath($"ui-{fixture.Name}@{Number(scale)}"));
        Assert.Equal((golden.Width, golden.Height), (image.Width, image.Height));
        const int scrollbar = 15;
        var problems = new List<string>();
        foreach (var (column, left) in new[] { ("opaque", Left), ("transparent", Right) })
            for (var i = 0; i < Cases.Length; i++)
            {
                var width = Cases[i].Name == "scroller" ? 170 - scrollbar : 170;
                var top = Top + i * Pitch;
                var beyond = 0;
                var worst = 0;
                for (var y = (int)MathF.Floor(top * scale); y < (int)MathF.Ceiling((top + 20) * scale); y++)
                    for (var x = (int)MathF.Floor(left * scale); x < (int)MathF.Ceiling((left + width) * scale); x++)
                    {
                        var p = (y * image.Width + x) * 4;
                        var d = 0;
                        for (var c = 0; c < 4; c++)
                            d = Math.Max(d, Math.Abs(image.Pixels[p + c] - golden.Pixels[p + c]));
                        worst = Math.Max(worst, d);
                        if (d > 12)
                            beyond++;
                    }
                if (beyond > 0)
                    problems.Add($"{column} {Cases[i].Name}: {beyond} pixels beyond 12 levels (worst {worst})");
            }
        Assert.True(problems.Count == 0, $"@{Number(scale)}: {string.Join("; ", problems)}");
    }

    /// <summary>
    /// A control's label (laid-out text the painter draws whole) takes its own chunk's verdict too, not the
    /// verdict of the text drawn before it: a select after grayscale text (a <c>will-change: transform</c>
    /// box) on the page's opaque background is drawn with LCD antialiasing (colour fringes), one in a
    /// <c>will-change: transform</c> box after LCD text is drawn grayscale (none). The engine alone, at 1 and
    /// 1.65: the verdicts are those the fixture above checks against Chromium, and a select's label sits a
    /// device pixel below Chromium's at 1.65 (the control painter's baseline, rounded in CSS pixels), which
    /// would hide this behind another difference there.
    /// </summary>
    [Theory]
    [MemberData(nameof(ScaleData))]
    public void AControlsLabelTakesItsChunksVerdict(float scale)
    {
        const string css = """
            html { background: #fff; }
            body { font: 13px/20px Lato; color: #222; }
            .a { position: absolute; left: 10px; width: 160px; height: 20px; }
            .wct { will-change: transform; }
            select { appearance: none; margin: 0; border: 0; padding: 0 4px; height: 20px; font: inherit; color: inherit; background: #fff; }
            """;
        const string html = """
            <div class="a wct" style="top: 20px">Grayscale text</div>
            <select class="a" style="top: 60px"><option>A label with LCD text</option></select>
            <div class="a" style="top: 100px">LCD text</div>
            <div class="a wct" style="top: 140px"><select><option>A grayscale label</option></select></div>
            """;
        var fixture = new PaintFixture("control-label-lcd", 200, 180, css, html);
        var document = PaintReferenceTests.Build(fixture, scale);
        var image = PaintReferenceTests.Render(gpu.Device, document, (int)MathF.Round(fixture.Width * scale), (int)MathF.Round(fixture.Height * scale));
        int Spread(int top)
        {
            var spread = 0;
            for (var y = (int)(top * scale); y < (int)((top + 20) * scale); y++)
                for (var x = (int)(10 * scale); x < (int)(170 * scale); x++)
                {
                    var p = (y * image.Width + x) * 4;
                    var (r, g, b) = (image.Pixels[p], image.Pixels[p + 1], image.Pixels[p + 2]);
                    spread = Math.Max(spread, Math.Max(r, Math.Max(g, b)) - Math.Min(r, Math.Min(g, b)));
                }
            return spread;
        }
        // Dark grey on white: LCD antialiasing gives fringes of a hundred levels and more, grayscale none.
        Assert.True(Spread(20) <= 8, $"@{Number(scale)}: the grayscale text has colour fringes ({Spread(20)})");
        Assert.True(Spread(60) >= 64, $"@{Number(scale)}: the select's label after grayscale text has no LCD fringes ({Spread(60)})");
        Assert.True(Spread(100) >= 64, $"@{Number(scale)}: the LCD text has no fringes ({Spread(100)})");
        Assert.True(Spread(140) <= 8, $"@{Number(scale)}: the grayscale select's label after LCD text has colour fringes ({Spread(140)})");
    }
}

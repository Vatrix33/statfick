using System.Globalization;
using System.Text.Json;
using Statventure.Core;
using Statventure.Tests.Infrastructure;
using Statventure.UI.Dom;
using Statventure.UI.Paint;

namespace Statventure.Tests.Ui;

/// <summary>
/// Thin rounded borders as Chromium 152 paints them (GM2's engine task, found by step 8's race strip; Docs/Compositing-Port.md,
/// "Thin rounded borders"): a border Blink draws as one ring (BoxBorderPainter::PaintBorderFastPath, then FillDRRect's
/// simple case) is one stroked rrect, which Skia draws as an antialiased hairline when it is one device pixel wide (an
/// outline around rounded corners and the theme's rounded frames the same); the background under a rounded border that
/// hides its edge shrinks by half the border (bleed avoidance); an outer shadow under an opaque background is cut out one
/// device pixel inside the box (ClipToBorderEdge); a box's radii are constrained to its snapped border box. The pages are
/// Chromium main frames captured through Tools/PaintReference with the flags of Tools/Scripts/make-scaled-paint-references.sh
/// &lt;scale&gt; 1 round-border-fixtures.json (on the PC inside a headless cage): every box Chromium's at 1, 1.25, 1.5, 1.65
/// and 1.75 (LayoutUnitReferenceTests' tolerance), the pages painted as Chromium's at 1 and 1.65 (12 levels, 0.2 %), and
/// box by box the rule's boxes with no pixel past 12 levels and its guards within the per-control limit
/// (EveryBoxPaintsLikeChromium). Sizes are multiples of 20 px.
/// </summary>
[Collection("GPU")]
public class RoundBorderReferenceTests(GpuFixture gpu)
{
    const string Base = """
        html { background: #f4efe2; }
        body { margin: 0; font: 16px/20px Lato; color: #222; }
        """;

    /// <summary>
    /// Circles of six sizes (the odd ones centred on half pixels at 1) on a light and a dark ground, a row per case: a 1 px
    /// border alone, over a fill (shrunk under it), over a fill with an inset shadow (the strip's medallion), translucent
    /// with no fill, at opacity .8, with a 2 px ring of box-shadow over an opaque fill (the shown medallion: its shadow cut
    /// a pixel inside the box), a 1 px outline alone and with an offset round a bordered fill, a .5 px border (1 device
    /// px at every scale). Avoided (known differences, Docs/Compositing-Port.md, "Thin rounded borders", "Left as they
    /// were"): wider rings and fills alone on these grounds (Skia fills a curve's edge through quads within a quarter
    /// pixel, the canvas by its distance: up to 0.2 of a pixel's coverage, past 12 levels on a high contrast) and a
    /// translucent border over a fill (Blink's clip layer, approximated per draw); the ring row's shadow edge is such an
    /// edge, so that row isn't held to the hairline check.
    /// </summary>
    static readonly LayoutUnitFixture Circles = new("rborder-circles", 600, 760, Base + """
        .col { position: absolute; top: 0; width: 300px; height: 760px; padding: 6px 10px; box-sizing: border-box; }
        .light { left: 0; --line: #85673f; --fill: #dfcfa9; --well: inset 0 1px 3px #4a301830; --soft: #85673f99; --ring: #6b8f3a; }
        .dark { left: 300px; background: #1e1611; --line: #6e5538; --fill: #0c0806; --well: inset 0 1px 4px #000000b3; --soft: #6e553899; --ring: #93b86a; }
        .row { display: flex; align-items: center; gap: 10px; height: 64px; margin-bottom: 10px; }
        .row > i { display: block; flex: none; border-radius: 50%; box-sizing: border-box; }
        .s13 { width: 13px; height: 13px; } .s25 { width: 25px; height: 25px; } .s30 { width: 30px; height: 30px; }
        .s38 { width: 38px; height: 38px; } .s51 { width: 51px; height: 51px; } .s64 { width: 64px; height: 64px; }
        .b > i { border: 1px solid var(--line); }
        .bf > i { border: 1px solid var(--line); background: var(--fill); }
        .bfs > i { border: 1px solid var(--line); background: var(--fill); box-shadow: var(--well); }
        .t > i { border: 1px solid var(--soft); }
        .o > i { opacity: .8; }
        .ring > i { border: 1px solid var(--ring); background: var(--fill); box-shadow: 0 0 0 2px var(--ring), var(--well); }
        .ol > i { outline: 1px solid var(--line); }
        .olo > i { border: 1px solid var(--line); outline: 1px solid var(--ring); outline-offset: 2px; background: var(--fill); }
        .half > i { border: .5px solid var(--line); background: var(--fill); }
        .sc > i { transform: scale(.5); }
        """, Column("light") + Column("dark"));

    static string Column(string theme) =>
        $"<div class=\"col {theme}\">"
        + string.Concat(new[] { "b", "bf", "bfs", "t", "bf o", "ring", "ol", "olo", "half", "bf sc" }.Select(row =>
            $"<div class=\"row {row}\">" + string.Concat(new[] { 13, 25, 30, 38, 51, 64 }.Select(size =>
                $"<i class=\"s{size}{(row == "ring" ? "" : row == "bf sc" ? " z1" : " z")}\"></i>")) + "</div>"))
        + "</div>";

    /// <summary>The radii page's columns: 1, 2, 4 and 10 px, a pill, 10 px and square corners mixed, square and 12 and 4 px (and 15 px).</summary>
    static readonly string[] AllRadii = ["r1", "r2", "r4", "r10", "rp", "rm", "rx"];

    /// <summary>A row of the radii page where every box is the same case, checked one by one as <paramref name="check"/>.</summary>
    static (string Radius, string Case, string Check)[] Row(string name, string check) => [.. AllRadii.Append("rh").Select(r => (r, name, check))];

    /// <summary>
    /// The radii page's rows of boxes: each box's radius, case and how it is checked on its own
    /// (<see cref="EveryBoxPaintsLikeChromium"/>: "z" no pixel past 12 levels, "z1" so at 1 only, "g" a guard within the
    /// per-control limit).
    /// </summary>
    static readonly (string Radius, string Case, string Check)[][] Rows =
    [
        Row("plain", "z"), Row("nofill", "z"), Row("sq", "z"), Row("big", "z"), Row("tr", "z"), Row("styled", "z"),
        [("r1", "w2", "g"), ("r2", "wn", "g"), ("r2", "cn", "g"), ("r2", "left", "g"), ("r075", "tight", "z"), ("re", "ell", "g"), ("r2", "trf", "z"), ("rmx", "olm", "z")],
    ];


    /// <summary>
    /// Rounded boxes, a row per case and a column per radius (1, 2, 4 and 10 px, a pill, 10 px and square corners mixed,
    /// two square corners and two of 12 and 4 px, 15 px): a 1 px border over a fill (rings, each a hairline), a 1 px
    /// border with no fill, squares (the pill and 15 px circles) and boxes twice as tall, a translucent 1 px border with no
    /// fill (a hairline: no bleed avoidance without a background), 1 px double, groove and ridge borders (solid, BorderEdge's
    /// effective style); and guards that aren't rings, a box each: a 2 px border
    /// (a stroke, not a hairline), widths not all one (a 3 px bottom: the general path), a top side of another colour,
    /// a left side alone, a radius under the width (.75 px with 1 px at 1: FillDRRect's path), an elliptical corner
    /// (4 px / 2 px: its path), a translucent border over a fill (Blink's clip layer, the band); and an outline round radii
    /// that overflow their box (30 and 6 px on a 30 px box: constrained to the box before the outline grows them). And a bar 581 px wide
    /// (do_anti_hairline would halve a line longer than 511 px, the halves meeting in a pixel at 1; Chromium's tiles keep
    /// its lines shorter). Avoided: guards with larger radii and rings wider than a pixel on curves (the known difference
    /// of Skia's curve edges, see the circles), which would cost the page most of its allowance; and rounded boxes under a
    /// scale (their hairlines' straight sides lie a quarter pixel from Chromium's, up to 28 pixels past 12 levels a box at
    /// 1, cause not found: "Left as they were"; the scaled circles of the circles page match at 1).
    /// </summary>
    static readonly LayoutUnitFixture Radii = new("rborder-radii", 600, 380, Base + """
        .grid { position: absolute; left: 10px; top: 10px; display: grid; grid-template-columns: repeat(8, 64px); gap: 10px 8px; }
        .grid > i { display: block; height: 30px; box-sizing: border-box; background: #dfcfa9; border: 1px solid #85673f; }
        .r1 { border-radius: 1px; } .r2 { border-radius: 2px; } .r4 { border-radius: 4px; } .r10 { border-radius: 10px; }
        .rp { border-radius: 999px; } .rm { border-radius: 10px 0; } .rx { border-radius: 0 12px 4px 0; } .rh { border-radius: 15px; }
        .re { border-radius: 4px / 2px; } .r075 { border-radius: .75px; } .rmx { border-radius: 30px 6px; }
        .grid > i.w2 { border-width: 2px; }
        .grid > i.wn { border-width: 1px 1px 3px 1px; }
        .grid > i.cn { border-top-color: #4a3018; }
        .grid > i.left { border: 0; border-left: 3px solid #85673f; }
        .grid > i.nofill { background: none; }
        .grid > i.sq { width: 30px; height: 30px; }
        .grid > i.big { height: 60px; }
        .grid > i.tr { border-color: #85673f80; background: none; }
        .grid > i.trf { border-color: #85673f80; }
        .grid > i.olm { border: 0; background: none; outline: 1px solid #6b8f3a; outline-offset: 2px; }
        .styled.r1, .styled.r2, .styled.r4 { border-style: double; } .styled.r10, .styled.rp { border-style: groove; } .styled.rm, .styled.rx, .styled.rh { border-style: ridge; }
        .bar { position: absolute; left: 10px; top: 330px; width: 581px; height: 30px; box-sizing: border-box; border: 1px solid #85673f;
          border-radius: 6px; background: #dfcfa9; }
        """, "<div class=\"grid\">"
        + string.Concat(Rows.Select(row =>
            string.Concat(row.Select(box => $"<i class=\"{box.Radius} {box.Case} {box.Check}\"></i>"))
            + string.Concat(Enumerable.Repeat("<b></b>", 8 - row.Length))))
        + "</div><div class=\"bar z\"></div>");

    /// <summary>
    /// The race strip of GM2 step 8 in Dev Mode, as its races.css draws it (its rules as test content): 28 medallions of 38
    /// and 30 px with 1 px borders over a fill and an inset shadow, the enemy-only ones at opacity .8, the shown one with a
    /// 2 px ring of box-shadow, on the dialog's grain, light and dark. Avoided: the emblems' placeholder glyphs (not
    /// borders, and their font isn't the tests').
    /// </summary>
    static readonly LayoutUnitFixture Strip = new("rborder-strip", 900, 260, Base + """
        .panel { height: 130px; box-sizing: border-box; padding: 18px 24px 16px; display: flex; flex-direction: column; align-items: center;
          background: var(--grain) 0 0 / 240px 240px, var(--surface); }
        .light { --surface: #ecdfbf; --line-strong: #85673f; --paper-deep: #dfcfa9; --well-shadow: inset 0 1px 3px #4a301830; --race-accent: #6b8f3a;
          --grain: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='240' height='240'%3E%3Cfilter id='n'%3E%3CfeTurbulence type='fractalNoise' baseFrequency='.8' numOctaves='3' stitchTiles='stitch'/%3E%3CfeColorMatrix values='0 0 0 0 .2 0 0 0 0 .12 0 0 0 0 .05 0 0 0 .16 0'/%3E%3C/filter%3E%3Crect width='240' height='240' filter='url(%23n)'/%3E%3C/svg%3E"); }
        .dark { --surface: #1e1611; --line-strong: #6e5538; --paper-deep: #0c0806; --well-shadow: inset 0 1px 4px #000000b3; --race-accent: #93b86a;
          --grain: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='240' height='240'%3E%3Cfilter id='n'%3E%3CfeTurbulence type='fractalNoise' baseFrequency='.8' numOctaves='3' stitchTiles='stitch'/%3E%3CfeColorMatrix values='0 0 0 0 .9 0 0 0 0 .8 0 0 0 0 .6 0 0 0 .06 0'/%3E%3C/filter%3E%3Crect width='240' height='240' filter='url(%23n)'/%3E%3C/svg%3E"); }
        .strip { display: flex; flex-wrap: wrap; justify-content: center; align-items: center; gap: 8px; max-width: 820px; }
        .item { display: grid; width: 38px; height: 38px; padding: 0; place-items: center; border: 1px solid var(--line-strong); border-radius: 50%;
          background: var(--paper-deep); box-shadow: var(--well-shadow); font-size: 1rem; line-height: 1; }
        .item.enemy { width: 30px; height: 30px; opacity: .8; }
        .item.current { border-color: var(--race-accent); box-shadow: 0 0 0 2px var(--race-accent), var(--well-shadow); opacity: 1; }
        .divider { width: 1px; height: 28px; margin: 0 6px; background: var(--line-strong); }
        """, StripPanel("light") + StripPanel("dark"));

    static string StripPanel(string theme) =>
        $"<div class=\"panel {theme}\"><div class=\"strip\">"
        + string.Concat(Enumerable.Repeat("<button class=\"item\"></button>", 8))
        + "<span class=\"divider\"></span><button class=\"item enemy current\"></button>"
        + string.Concat(Enumerable.Repeat("<button class=\"item enemy\"></button>", 18))
        + "<span class=\"divider\"></span><button class=\"item\"></button>"
        + "</div></div>";

    /// <summary>
    /// The same Skia rule where the theme draws (form controls with their native look): buttons' and a text field's rounded
    /// frames (NativeThemeBase's drawRoundRect, 1 device px at both scales), a checkbox and a radio. The field, the checkbox
    /// and the radio are held to the hairline check at 1 only: at 1.65 they differ from Chromium otherwise (the field's text
    /// by a few pixels, the checkbox's and the radio's own drawing, 20 and 55 pixels past 12 levels, as before). Avoided: a
    /// focus ring (DrawFocusRingRect strokes its inner 1 px ring as Skia's hairline too, but a focused button's ring at 1
    /// is no nearer Chromium's for it, and at 1.65 the focus ring's known difference, 587 pixels, would take the page's
    /// allowance: "Three text rules", "Left as they were").
    /// </summary>
    static readonly LayoutUnitFixture Theme = new("rborder-theme", 400, 100, Base + """
        .t { position: absolute; margin: 0; font: 13px Lato; }
        """, """
        <button class="t z" style="left: 10px; top: 10px; width: 90px; height: 28px">Button</button>
        <button class="t z" style="left: 110px; top: 10px; width: 61px; height: 25px">Odd</button>
        <input class="t z1" type="text" value="Field" style="left: 10px; top: 60px; width: 150px">
        <input class="t z1" type="checkbox" style="left: 190px; top: 60px">
        <input class="t z1" type="radio" style="left: 230px; top: 60px">
        """);

    public static readonly LayoutUnitFixture[] All = [Circles, Radii, Strip, Theme];

    /// <summary>The configurations the boxes are compared at: the device scales (no page zoom).</summary>
    static readonly float[] BoxScales = [1, 1.25f, 1.5f, 1.65f, 1.75f];

    static readonly float[] PaintScales = [1, 1.65f];

    /// <summary>A box may be this far from Chromium's in device px (LayoutUnitReferenceTests').</summary>
    const float Tolerance = 0.002f;

    static string ReferenceDirectory => Path.Combine(Path.GetDirectoryName(Golden.GoldenPath("x"))!, "..", "Reference");

    static string ScaleName(float scale) => scale.ToString(CultureInfo.InvariantCulture);

    public static TheoryData<string, float> BoxCases
    {
        get
        {
            var data = new TheoryData<string, float>();
            foreach (var fixture in All)
                foreach (var scale in BoxScales)
                    data.Add(fixture.Name, scale);
            return data;
        }
    }

    public static TheoryData<string, float> PaintCases
    {
        get
        {
            var data = new TheoryData<string, float>();
            foreach (var fixture in All)
                foreach (var scale in PaintScales)
                    data.Add(fixture.Name, scale);
            return data;
        }
    }

    public static TheoryData<string, float> BoxPaintCases
    {
        get
        {
            var data = new TheoryData<string, float>();
            foreach (var fixture in new[] { Circles, Radii, Theme })
                foreach (var scale in PaintScales)
                    data.Add(fixture.Name, scale);
            return data;
        }
    }

    [Fact]
    public void TheFixtureListIsUpToDate()
    {
        var path = Path.Combine(ReferenceDirectory, "round-border-fixtures.json");
        var json = JsonSerializer.Serialize(All.Select(f => new { name = f.Name, width = f.Width, height = f.Height, css = f.Css, html = f.Html, gameFonts = true, mainFrame = true }));
        if (!File.Exists(path) || File.ReadAllText(path) != json)
            File.WriteAllText(path, json);
        Assert.NotEmpty(json);
    }

    [Theory]
    [MemberData(nameof(BoxCases))]
    public void EveryBoxIsChromiums(string name, float scale)
    {
        var fixture = All.Single(f => f.Name == name);
        var path = Path.Combine(ReferenceDirectory, "scaled", $"ui-{name}@{ScaleName(scale)}.json");
        if (!File.Exists(path))
            throw new FileNotFoundException("capture the fixture in Chromium (Tools/PaintReference with round-border-fixtures.json)", path);
        var reference = JsonDocument.Parse(File.ReadAllText(path)).RootElement;
        var document = LayoutUnitReferenceTests.Build(fixture, scale);
        var zoom = document.Zoom;
        var elements = new List<UiElement> { document.Body };
        elements.AddRange(document.Body.Descendants().OfType<UiElement>());
        Assert.Equal(reference.GetArrayLength(), elements.Count);
        var problems = new List<string>();
        for (var i = 0; i < elements.Count; i++)
        {
            var want = reference[i];
            Assert.Equal(want[0].GetString(), elements[i].Tag);
            var expected = new RectF(want[1].GetSingle(), want[2].GetSingle(), want[3].GetSingle(), want[4].GetSingle());
            var got = elements[i].GetBoundingClientRect();
            float[] off = [got.X - expected.X, got.Y - expected.Y, got.Width - expected.Width, got.Height - expected.Height];
            if (off.Any(d => MathF.Abs(d) * zoom > Tolerance))
                problems.Add($"#{i} {elements[i].Tag}.{elements[i].ClassName}: ({got.X * zoom:0.####}, {got.Y * zoom:0.####}, {got.Width * zoom:0.####}×{got.Height * zoom:0.####}) device px, Chromium ({expected.X * zoom:0.####}, {expected.Y * zoom:0.####}, {expected.Width * zoom:0.####}×{expected.Height * zoom:0.####})");
        }
        Assert.True(problems.Count == 0, $"{name}@{ScaleName(scale)}: {problems.Count} boxes off Chromium's\n" + string.Join("\n", problems.Take(30)));
    }

    [Theory]
    [MemberData(nameof(PaintCases))]
    public void ThePagePaintsLikeChromium(string name, float scale)
    {
        var fixture = All.Single(f => f.Name == name);
        var document = LayoutUnitReferenceTests.Build(fixture, scale);
        var image = PaintReferenceTests.Render(gpu.Device, document, (int)MathF.Round(fixture.Width * scale), (int)MathF.Round(fixture.Height * scale));
        Golden.Match($"ui-{name}@{ScaleName(scale)}", image, tolerance: 12, maxDifferentRatio: 0.002);
    }

    /// <summary>
    /// The opaqueness analysis reads a rounded fill's rrect as drawn, its radii constrained to it (Blink's DrawRRectOp holds
    /// the constrained rrect; DrawingDisplayItem's known-opaque rect is the rrect's rect inset by 0.3 of its radii): a pill
    /// whose 999 px radii fit it as 10 px is known opaque but for 3 px at each side, with no border (one primitive) and
    /// with an opaque 1 px border (its colour shrunk by half a pixel under it, radii 9.5 px).
    /// </summary>
    [Fact]
    public void APillIsKnownOpaqueToItsConstrainedRadii()
    {
        var fixture = new LayoutUnitFixture("rborder-pill", 200, 100, Base + """
            i { position: absolute; display: block; width: 100px; height: 20px; border-radius: 999px; background: #dfcfa9; will-change: transform; }
            """, """<i id="a" style="left: 10px; top: 10px"></i><i id="b" style="left: 10px; top: 50px; border: 1px solid #85673f; box-sizing: border-box"></i>""");
        var document = LayoutUnitReferenceTests.Build(fixture, 1);
        using var painter = new UiPainter(document);
        var chunks = painter.AnalyzeLayers().SelectMany(l => l.Chunks).ToList();
        RectF OpaqueOf(string id) => chunks.Single(c => c.Owner == document.GetElementById(id)!.Box).RectKnownToBeOpaque;
        Assert.Equal(RectF.FromEdges(13, 13, 107, 27), OpaqueOf("a"));
        // The shrunk fill (10.5 to 109.5 by 50.5 to 69.5, radii 9.5): inset 2.85, whole pixels inside.
        Assert.Equal(RectF.FromEdges(14, 54, 106, 66), OpaqueOf("b"));
    }

    /// <summary>The pixels past 12 levels a guard (class "g") may have: LayoutUnitReferenceTests' per-control limit.</summary>
    const int GuardLimit = 60;

    /// <summary>
    /// Box by box, its box and 3 px round it against Chromium's: a box the rule draws (class "z", and "z1" at 1 only) with
    /// no pixel past 12 levels, the pages' levels with no share (a ring a hairline misses by a fraction of a pixel moves a
    /// few dozen pixels of a box, which a page's 0.2 % lets through); a guard, a box that isn't a ring (class "g"), within
    /// the per-control limit (its own known differences, Skia's path edges, take up to 15 pixels; drawn as a ring it moves
    /// a side or a curve). Class "z1": at 1.65 a hairline under a 2D scale lies a little apart from Chromium's at its
    /// extreme pixels (up to 14 levels in 4 pixels, cause not found), and the theme's field, checkbox and radio differ
    /// otherwise (Docs/Compositing-Port.md, "Thin rounded borders", "Left as they were").
    /// </summary>
    [Theory]
    [MemberData(nameof(BoxPaintCases))]
    public void EveryBoxPaintsLikeChromium(string name, float scale)
    {
        var fixture = All.Single(f => f.Name == name);
        var document = LayoutUnitReferenceTests.Build(fixture, scale);
        var image = PaintReferenceTests.Render(gpu.Device, document, (int)MathF.Round(fixture.Width * scale), (int)MathF.Round(fixture.Height * scale));
        var golden = Statventure.Imaging.ImageFile.Load(Golden.GoldenPath($"ui-{name}@{ScaleName(scale)}"));
        var boxes = new List<(UiElement Box, int Limit)>();
        foreach (var element in document.Body.Descendants().OfType<UiElement>())
        {
            var classes = element.ClassName.Split(' ');
            if (classes.Contains("z") || scale == 1 && classes.Contains("z1"))
                boxes.Add((element, 0));
            else if (classes.Contains("g"))
                boxes.Add((element, GuardLimit));
        }
        Assert.NotEmpty(boxes);
        var problems = new List<string>();
        foreach (var (box, limit) in boxes)
        {
            var r = box.GetBoundingClientRect();
            var x0 = Math.Max(0, (int)MathF.Floor((r.X - 3) * scale));
            var y0 = Math.Max(0, (int)MathF.Floor((r.Y - 3) * scale));
            var x1 = Math.Min(image.Width, (int)MathF.Ceiling((r.Right + 3) * scale));
            var y1 = Math.Min(image.Height, (int)MathF.Ceiling((r.Bottom + 3) * scale));
            var beyond = 0;
            var worst = 0;
            for (var y = y0; y < y1; y++)
                for (var x = x0; x < x1; x++)
                {
                    var p = (y * image.Width + x) * 4;
                    var d = 0;
                    for (var c = 0; c < 3; c++)
                        d = Math.Max(d, Math.Abs(image.Pixels[p + c] - golden.Pixels[p + c]));
                    worst = Math.Max(worst, d);
                    if (d > 12)
                        beyond++;
                }
            if (beyond > limit)
                problems.Add($"{box.ClassName} at ({r.X}, {r.Y}): {beyond} pixels past 12 levels (at most {limit}), the worst {worst}");
        }
        Assert.True(problems.Count == 0, $"{name}@{ScaleName(scale)}: {problems.Count} of {boxes.Count} boxes\n" + string.Join("\n", problems));
    }
}

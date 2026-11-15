using System.Globalization;
using System.Text.Json;
using Statventure.Core;
using Statventure.Tests.Canvas;
using Statventure.Tests.Infrastructure;
using Statventure.UI.Dom;

namespace Statventure.Tests.Ui;

/// <summary>A page captured as Chromium's main frame (the game's document is one): its size in CSS px, whole device pixels at every scale used.</summary>
public sealed record LayoutUnitFixture(string Name, int Width, int Height, string Css, string Html);

/// <summary>
/// Layout at fractional scales on Blink's LayoutUnit grid (Docs/Compositing-Port.md, "Layout units"; GM1's engine task of
/// Q34): every value a whole number of 1/64 device px, a centring or share the raw count divided as an integer, the
/// leftovers handed on (flex lines, grid tracks), intrinsic sums of truncated lengths, aspect ratios applied with MulDiv,
/// and a control's one line of text (an input button's, a menu list's, a text field's) on its inner block's line box.
/// The fixtures are Chromium 152 pages captured as the main frame by Tools/Scripts/make-scaled-paint-references.sh
/// &lt;scale&gt; &lt;page zoom&gt; layout-unit-fixtures.json (Golden/ui-units-*@&lt;config&gt;.png and
/// Reference/scaled/ui-units-*@&lt;config&gt;.json, every element's getBoundingClientRect): every box must be Chromium's to a
/// fraction of 1/64 device px at 1, 1.25, 1.5, 1.65, 1.75 and 1.65 with a page zoom of 1.2, and the pages paint as
/// Chromium's at 1 and 1.65 within the paint fixtures' limits (12 levels, 0.2 %).
/// </summary>
[Collection("GPU")]
public class LayoutUnitReferenceTests(GpuFixture gpu)
{
    const string Base = """
        html { background: #f4efe2; }
        body { margin: 0; font: 13px/20px Lato; color: #222; }
        """;

    /// <summary>
    /// A start dialog as the game's: a grid centring a dialog whose max-content holds a flex row with a gap (an intrinsic
    /// sum), so the dialog sits at a fractional offset; bordered buttons centred in it; a block centred by auto margins
    /// with a percentage width; centred text.
    /// </summary>
    static readonly LayoutUnitFixture Centring = new("units-centring", 400, 300, Base + """
        .screen { display: grid; min-height: 100vh; place-items: center; padding: 23px; box-sizing: border-box; }
        .dialog { width: min(251px, 100%); border: 1px solid #998; background: #fff; display: flex; flex-direction: column; overflow: hidden; }
        .head { display: flex; gap: 16px; align-items: center; padding: 9px 13px; border-bottom: 1px solid #998; }
        .mark { flex: 0 0 auto; width: 25px; height: 25px; border: 1px solid #998; display: grid; place-items: center; }
        .mark i { width: 7px; height: 7px; background: #a64; }
        .head p { margin: 0; }
        .title { font-size: 19px; line-height: 1.3; }
        .buttons { display: grid; gap: 7px; padding: 11px 13px; }
        .buttons button { display: flex; justify-content: center; align-items: center; margin: 0; padding: 5px; border: 1px solid #998; background: #faf6ee; font: inherit; color: inherit; }
        .centred { width: 37%; margin: 9px auto 0; height: 11px; background: #cdb; }
        .note { margin: 5px 0 9px; text-align: center; }
        """, """
        <section class="screen"><div class="dialog">
          <header class="head"><span class="mark"><i></i></span><div><p>PERSONAL RECORD</p><p class="title">Stat Fick</p></div></header>
          <nav class="buttons"><button>Continue</button><button>New character</button><button>Load</button></nav>
          <div class="centred"></div><p class="note"><span>A centred line</span></p>
        </div></section>
        """);

    /// <summary>
    /// Flex rows: justify-content's distributions (their spaces shared by Blink's diffuser), centring, main-axis auto
    /// margins, flexible lengths grown and shrunk (LineFlexer's shares), cross-axis auto margins, wrapped lines stretched
    /// and spaced, a centred column.
    /// </summary>
    static readonly LayoutUnitFixture Flex = new("units-flex", 500, 400, Base + """
        .row { display: flex; width: 437px; height: 27px; margin: 0 0 5px 7px; background: #e8e0d0; align-items: center; gap: 3px; }
        .row > b { display: block; border: 1px solid #765; padding: 2px 5px; font-weight: 400; background: #fff; }
        .sb { justify-content: space-between; } .sa { justify-content: space-around; } .se { justify-content: space-evenly; } .ce { justify-content: center; }
        .auto > b:nth-child(2) { margin-left: auto; } .auto2 > b { margin: 0 auto; }
        .grow > b { flex: 1 1 0; } .grow2 > b:nth-child(1) { flex-grow: 1; } .grow2 > b:nth-child(2) { flex-grow: 2; } .grow2 > b:nth-child(3) { flex-grow: 4; }
        .shrink > b { flex: 0 1 200px; } .shrink > b:nth-child(2) { flex-shrink: 3; }
        .cross > b { margin: auto 0; align-self: auto; }
        .wrap { flex-wrap: wrap; height: 61px; align-content: stretch; } .wrap > b { width: 97px; }
        .wrap2 { flex-wrap: wrap; height: 61px; align-content: space-between; } .wrap2 > b { width: 97px; }
        .col { flex-direction: column; height: 61px; justify-content: center; }
        """, """
        <div class="row sb"><b>One</b><b>Two</b><b>Three</b><b>Four</b></div>
        <div class="row sa"><b>One</b><b>Two</b><b>Three</b><b>Four</b></div>
        <div class="row se"><b>One</b><b>Two</b><b>Three</b><b>Four</b></div>
        <div class="row ce"><b>One</b><b>Two</b><b>Three</b></div>
        <div class="row auto"><b>One</b><b>Two</b><b>Three</b></div>
        <div class="row auto2"><b>One</b><b>Two</b><b>Three</b></div>
        <div class="row grow"><b>One</b><b>Two</b><b>Three</b></div>
        <div class="row grow2"><b>One</b><b>Two</b><b>Three</b></div>
        <div class="row shrink"><b>One</b><b>Two</b><b>Three</b></div>
        <div class="row cross"><b>One</b><b>Two</b></div>
        <div class="row wrap"><b>A</b><b>B</b><b>C</b><b>D</b><b>E</b><b>F</b><b>G</b></div>
        <div class="row wrap2"><b>A</b><b>B</b><b>C</b><b>D</b><b>E</b><b>F</b><b>G</b></div>
        <div class="row col"><b>One</b><b>Two</b></div>
        """);

    /// <summary>
    /// Nested percentages and auto margins in blocks, auto margins of absolutely positioned boxes (one taller than its
    /// containing block: the block direction keeps its negative halves), a static position centred in a flex container,
    /// vertical-align: middle, grid tracks shared out (fr tracks, auto tracks stretched, a distributed track list), aspect
    /// ratios and centred text.
    /// </summary>
    static readonly LayoutUnitFixture Margins = new("units-margins", 500, 400, Base + """
        .box { width: 333px; margin: 3px 0 0 7px; border: 1px solid #765; }
        .m { width: 41.3%; margin: 3px auto; height: 9px; background: #cdb; }
        .m .m { width: 77.7%; background: #bcd; }
        .m2 { width: 117px; margin-left: auto; height: 9px; background: #bcd; }
        .wrap { position: relative; height: 47px; margin: 5px 0 0 7px; width: 331px; background: #eee; }
        .abs { position: absolute; inset: 0; width: 111px; height: 11px; margin: auto; background: #a64; }
        .tall { position: absolute; left: 150px; top: 0; bottom: 0; width: 43px; height: 61px; margin: auto; background: #6a4; }
        .fx { display: flex; justify-content: center; align-items: center; position: relative; width: 333px; height: 31px; margin: 5px 0 0 7px; background: #ddd; }
        .fx > i { position: absolute; width: 23px; height: 7px; background: #46a; }
        .va { margin: 5px 0 0 7px; line-height: 23px; }
        .va i { display: inline-block; width: 17px; height: 9px; vertical-align: middle; background: #a46; }
        .grid { display: grid; gap: 7px; width: 437px; margin: 5px 0 0 7px; }
        .grid > b { display: block; height: 9px; background: #789; }
        .g3 { grid-template-columns: repeat(3, 1fr); } .g6 { grid-template-columns: repeat(6, minmax(0, 1fr)); gap: 6px; }
        .g123 { grid-template-columns: 1fr 2fr 3fr; } .gauto { grid-template-columns: auto auto auto; }
        .gsa { grid-template-columns: 61px 61px 61px; justify-content: space-around; } .gc { grid-template-columns: 61px 61px; justify-content: center; }
        .ar { width: 133px; aspect-ratio: 24 / 5; margin: 5px 0 0 7px; background: #ccc; } .ar2 { width: 101px; aspect-ratio: 16 / 9; margin: 5px 0 0 7px; background: #bbb; }
        .tac { width: 211px; margin: 5px 0 0 7px; text-align: center; background: #eee; }
        """, """
        <div class="box"><div class="m"><div class="m"></div></div><div class="m2"></div></div>
        <div class="wrap"><div class="abs"></div><div class="tall"></div></div>
        <div class="fx"><i></i><span>Static</span></div>
        <div class="va"><i></i> middle aligned</div>
        <div class="grid g3"><b></b><b></b><b></b></div>
        <div class="grid g6"><b></b><b></b><b></b><b></b><b></b><b></b></div>
        <div class="grid g123"><b></b><b></b><b></b></div>
        <div class="grid gauto"><b>a</b><b>bb</b><b>c</b></div>
        <div class="grid gsa"><b></b><b></b><b></b></div>
        <div class="grid gc"><b></b><b></b></div>
        <div class="ar"></div><div class="ar2"></div>
        <div class="tac"><span>Centred text</span></div>
        """);

    /// <summary>
    /// Controls' one line of text: menu lists without their look, themed and with an author background (line-height
    /// reset), input buttons at line heights over and under their height, text fields at line heights under the font's
    /// (a stat input's), normal and with a fixed height taller than their line; a button's content centred in its height.
    /// </summary>
    static readonly LayoutUnitFixture Controls = new("units-controls", 300, 500, Base + """
        .r { position: absolute; left: 10px; width: 260px; }
        .n { appearance: none; margin: 0; border: 0; padding: 0 6px; font: inherit; color: inherit; background: #e8e0d0; }
        .g { margin: 0; padding: 9px; border: 1px solid #998; border-radius: 3px; background: #fff; color: #222; font: 16px/1.5 Lato; min-height: 40px; }
        .stat { font: 700 26px/1.2 Lato; width: 120px; padding: 0 6px; border: 1px solid #998; background: #fff; color: #222; }
        """, """
        <select class="r n" style="top: 20px; height: 20px"><option>A select's label</option></select>
        <select class="r n" style="top: 60px; height: 23px"><option>A select, 23 px</option></select>
        <select class="r n" style="top: 100px; height: 20px; line-height: 30px"><option>A select, 30 px lines</option></select>
        <select class="r" style="top: 140px; font: 16px/1.5 Lato"><option>A themed select</option></select>
        <select class="r g" style="top: 180px"><option>A menulist-button</option></select>
        <input class="r n" type="button" value="A button, 30 px lines" style="top: 240px; height: 20px; line-height: 30px">
        <input class="r" type="button" value="A themed button" style="top: 280px; height: 30px; font: 16px/1.5 Lato">
        <input class="r stat" type="text" value="12345678" style="top: 320px">
        <input class="r stat" type="text" value="34567890" style="top: 370px; line-height: normal">
        <input class="r stat" type="text" value="56789012" style="top: 420px; height: 44px">
        <button class="r" style="top: 470px; height: 25px; padding: 0 6px; border: 1px solid #998; background: #fff; font: inherit; color: inherit">A button's content, centred</button>
        """);

    /// <summary>
    /// The cases each rule needs to show (found by the mutations): a grid item centred by auto margins, three wrapped flex
    /// lines stretched, auto grid tracks maximized, fixed minmax tracks maximized with one capped (the others share what it
    /// leaves), a button's content centred, a fixed width, flex bases and a gap in shrink-to-fit boxes, controls in a line
    /// (their baselines; two whose free space is an odd count of 1/64 px at 1 and at 1.65), an input button's own height, a
    /// line 1/64 px tighter than its font, a focused field with a selection (its text drawn from the field's geometry, at a
    /// height where its line box's baseline and its glyphs' centre round to different pixels at 1).
    /// </summary>
    static readonly LayoutUnitFixture Cases = new("units-cases", 500, 400, Base + """
        .g { display: grid; width: 437px; margin: 5px 0 0 7px; background: #eee; }
        .gm { grid-template-columns: 101px; height: 30px; } .gm > b { display: block; margin: auto; width: 45px; height: 11px; background: #789; }
        .gmax { grid-template-columns: auto auto auto; width: 211px; } .gmax > b { display: block; font-weight: 400; background: #cdb; }
        .gcap { grid-template-columns: minmax(10px, 20px) minmax(10px, 100px) minmax(10px, 100px); width: 151px; } .gcap > b { display: block; height: 9px; background: #789; }
        .w3 { display: flex; flex-wrap: wrap; width: 437px; height: 70px; gap: 3px; margin: 5px 0 0 7px; background: #e8e0d0; }
        .w3 > i { width: 97px; height: 9px; background: #a64; }
        .shrink { margin: 5px 0 0 7px; }
        .ib { display: inline-block; vertical-align: top; background: #ddd; } .ib > i { display: block; width: 13.3px; height: 5px; background: #a46; }
        .if { display: inline-flex; vertical-align: top; height: 9px; background: #ccc; } .if > i { flex: 0 0 13.3px; background: #46a; }
        .if.gap { column-gap: 3.3px; }
        .n { appearance: none; margin: 0; border: 0; padding: 0 6px; font: inherit; color: inherit; background: #e8e0d0; }
        .line { margin: 5px 0 0 7px; }
        .tight { margin: 5px 0 0 7px; font: 16px/18.984375px Lato; }
        .stat { font: 700 26px/1.2 Lato; width: 160px; padding: 0 6px; border: 1px solid #998; background: #fff; color: #222; margin: 5px 0 0 7px; outline: none; }
        button.c { height: 26px; padding: 0 6px; border: 1px solid #998; background: #fff; font: inherit; color: inherit; margin: 5px 0 0 7px; }
        """, """
        <div class="g gm"><b></b></div>
        <div class="g gmax"><b>aa bb cc dd</b><b>eee fff ggg</b><b>h ii jjj kk</b></div>
        <div class="g gcap"><b></b><b></b><b></b></div>
        <div class="w3"><i></i><i></i><i></i><i></i><i></i><i></i><i></i><i></i><i></i></div>
        <div class="shrink"><div class="ib"><i></i></div> <div class="if"><i></i><i></i></div> <div class="if gap"><i></i><i></i><i></i></div></div>
        <div class="line">Go <input class="n" type="button" value="Go" style="height: 23px"> and <select class="n" style="height: 23px"><option>One</option></select> <input class="n" type="button" value="Auto" style="line-height: 1.3; font-size: 17px"></div>
        <div class="line">Go <input class="n" type="button" value="Go" style="height: 22px"> <span>and</span> <input class="n" type="button" value="Go" style="height: 22.3px"></div>
        <div class="tight"><span>A tight line</span></div>
        <button class="c"><span>Centred content</span></button>
        <div><input class="stat" type="text" value="12345678" style="margin-top: 5.25px" data-focus data-select="2 6"></div>
        """);

    public static readonly LayoutUnitFixture[] All = [Centring, Flex, Margins, Controls, Cases];

    /// <summary>The configurations the boxes are compared at: device scale and page zoom.</summary>
    static readonly (float Scale, float PageZoom)[] Configs = [(1, 1), (1.25f, 1), (1.5f, 1), (1.65f, 1), (1.75f, 1), (1.65f, 1.2f)];

    /// <summary>The device scales the pages are painted at.</summary>
    static readonly float[] PaintScales = [1, 1.65f];

    /// <summary>A box of the engine may be this far from Chromium's in device px (a 1/64 step is 0.0156; float noise is under 0.0005).</summary>
    const float Tolerance = 0.002f;

    static string ReferenceDirectory => Path.Combine(Path.GetDirectoryName(Golden.GoldenPath("x"))!, "..", "Reference");

    static string ConfigName(float scale, float pageZoom) =>
        scale.ToString(CultureInfo.InvariantCulture) + (pageZoom == 1 ? "" : "x" + pageZoom.ToString(CultureInfo.InvariantCulture));

    public static TheoryData<string, float, float> BoxCases
    {
        get
        {
            var data = new TheoryData<string, float, float>();
            foreach (var fixture in All)
                foreach (var (scale, pageZoom) in Configs)
                    data.Add(fixture.Name, scale, pageZoom);
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

    [Fact]
    public void TheFixtureListIsUpToDate()
    {
        var path = Path.Combine(ReferenceDirectory, "layout-unit-fixtures.json");
        var json = JsonSerializer.Serialize(All.Select(f => new { name = f.Name, width = f.Width, height = f.Height, css = f.Css, html = f.Html, gameFonts = true, mainFrame = true }));
        if (!File.Exists(path) || File.ReadAllText(path) != json)
            File.WriteAllText(path, json);
        Assert.NotEmpty(json);
    }

    /// <summary>The fixture as the engine shows it, at a device scale and a page zoom (its size in CSS px, as Chromium's main frame).</summary>
    public static UiDocument Build(LayoutUnitFixture fixture, float scale, float pageZoom = 1)
    {
        var document = new UiDocument(TestFonts.Library);
        document.SetViewport(fixture.Width * pageZoom, fixture.Height * pageZoom);
        document.SetZoom(scale, pageZoom);
        document.AddStylesheet(fixture.Css, fixture.Name);
        Markup.SetInnerHtml(document.Body, fixture.Html);
        document.Update(0);
        // The focus and a field's selection as PaintReference sets them (data-focus, data-select "start end").
        foreach (var element in document.AllElements().ToList())
        {
            if (element.HasAttribute("data-focus"))
                element.Focus();
            if (element.GetAttribute("data-select") is { } select && select.Split(' ') is [var start, var end])
                element.SetSelectionRange(int.Parse(start, CultureInfo.InvariantCulture), int.Parse(end, CultureInfo.InvariantCulture));
        }
        document.Update(0);
        return document;
    }

    [Theory]
    [MemberData(nameof(BoxCases))]
    public void EveryBoxIsChromiums(string name, float scale, float pageZoom)
    {
        var fixture = All.Single(f => f.Name == name);
        var config = ConfigName(scale, pageZoom);
        var path = Path.Combine(ReferenceDirectory, "scaled", $"ui-{name}@{config}.json");
        if (!File.Exists(path))
            throw new FileNotFoundException("run Tools/Scripts/make-scaled-paint-references.sh <scale> <page zoom> layout-unit-fixtures.json to capture the fixtures in Chromium", path);
        var reference = JsonDocument.Parse(File.ReadAllText(path)).RootElement;
        var document = Build(fixture, scale, pageZoom);
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
        Assert.True(problems.Count == 0, $"{name}@{config}: {problems.Count} boxes off Chromium's\n" + string.Join("\n", problems.Take(30)));
    }

    public static TheoryData<float> ScaleData => new(PaintScales);

    /// <summary>The pixels past 12 levels one control of <c>units-controls</c> may have (EachControlPaintsLikeChromium).</summary>
    const int ControlLimit = 60;

    /// <summary>
    /// Control by control in <c>units-controls</c>, its box's pixels against Chromium's: a label a device row off moves
    /// about a thousand pixels of one control (955 to 2,290 before the line box was ported), which the whole page's 0.2 %
    /// can let through; the themes' own parts (a select's arrow, a push button's face) and a glyph a subpixel aside differ
    /// in a few dozen at most (34 on the GPU and 44 on lavapipe for the overflowing button's label at 1.65), so at most
    /// <see cref="ControlLimit"/> pass.
    /// </summary>
    [Theory]
    [MemberData(nameof(ScaleData))]
    public void EachControlPaintsLikeChromium(float scale)
    {
        var fixture = Controls;
        var document = Build(fixture, scale);
        var image = PaintReferenceTests.Render(gpu.Device, document, (int)MathF.Round(fixture.Width * scale), (int)MathF.Round(fixture.Height * scale));
        var golden = Statventure.Imaging.ImageFile.Load(Golden.GoldenPath($"ui-{fixture.Name}@{ConfigName(scale, 1)}"));
        var rects = JsonDocument.Parse(File.ReadAllText(Path.Combine(ReferenceDirectory, "scaled", $"ui-{fixture.Name}@{ConfigName(scale, 1)}.json"))).RootElement;
        var problems = new List<string>();
        foreach (var r in rects.EnumerateArray())
        {
            if (r[0].GetString() is not ("select" or "input" or "button"))
                continue;
            var x0 = (int)(r[1].GetSingle() * scale);
            var y0 = (int)(r[2].GetSingle() * scale);
            var x1 = Math.Min(image.Width, (int)MathF.Ceiling((r[1].GetSingle() + r[3].GetSingle()) * scale));
            var y1 = Math.Min(image.Height, (int)MathF.Ceiling((r[2].GetSingle() + r[4].GetSingle()) * scale));
            var beyond = 0;
            for (var y = y0; y < y1; y++)
                for (var x = x0; x < x1; x++)
                {
                    var p = (y * image.Width + x) * 4;
                    var d = 0;
                    for (var c = 0; c < 3; c++)
                        d = Math.Max(d, Math.Abs(image.Pixels[p + c] - golden.Pixels[p + c]));
                    if (d > 12)
                        beyond++;
                }
            if (beyond > ControlLimit)
                problems.Add($"the {r[0].GetString()} at {r[2].GetSingle()} px: {beyond} pixels past 12 levels");
        }
        Assert.True(problems.Count == 0, $"@{ConfigName(scale, 1)}: {string.Join("; ", problems)}");
    }

    [Theory]
    [MemberData(nameof(PaintCases))]
    public void ThePagePaintsLikeChromium(string name, float scale)
    {
        var fixture = All.Single(f => f.Name == name);
        var document = Build(fixture, scale);
        var image = PaintReferenceTests.Render(gpu.Device, document, (int)MathF.Round(fixture.Width * scale), (int)MathF.Round(fixture.Height * scale));
        Golden.Match($"ui-{name}@{ConfigName(scale, 1)}", image, tolerance: 12, maxDifferentRatio: 0.002);
    }
}

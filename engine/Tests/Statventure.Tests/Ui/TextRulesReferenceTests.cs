using System.Globalization;
using System.Text.Json;
using Statventure.Core;
using Statventure.Tests.Canvas;
using Statventure.Tests.Infrastructure;
using Statventure.UI.Dom;

namespace Statventure.Tests.Ui;

/// <summary>
/// Three of Blink's rules GM2's character sheet met (its step 7; the engine fixes of GM2, Docs/Compositing-Port.md,
/// "Three text rules"), each a Chromium 152 main-frame page of its own with its neighbouring cases: words broken only on
/// a line that can wrap (LineBreaker::SetCurrentStyleForce), a number field's spin button keeping its room whatever the
/// field's appearance (TextFieldInputType::CreateShadowSubtree, LayoutTheme), a number line-height kept as Blink keeps it
/// (StyleBuilderConverter::ConvertLineHeight). Captured by the GM2 engine fixes' cage script through Tools/PaintReference
/// (the flags of Tools/Scripts/make-scaled-paint-references.sh &lt;scale&gt; &lt;page zoom&gt; text-rules-fixtures.json):
/// every box Chromium's to a fraction of 1/64 device px at the configurations of <see cref="LayoutUnitReferenceTests"/>,
/// the pages painted as Chromium's at 1 and 1.65 (12 levels, 0.2 %), and each field of the spin page on its own. The
/// pages' sizes are multiples of 100 px, whole device pixels at every configuration (1.65 × 1.2 too).
/// </summary>
[Collection("GPU")]
public class TextRulesReferenceTests(GpuFixture gpu)
{
    const string Base = """
        html { background: #f4efe2; }
        body { margin: 0; font: 16px/20px Lato; color: #222; }
        """;

    /// <summary>
    /// Words broken or kept whole: overflow-wrap: anywhere and break-word and word-break: break-word and break-all under
    /// white-space nowrap and pre (kept whole, overflowing), and a nowrap span inside a breaking block; the same where
    /// lines wrap (normal, pre-wrap, pre-line: broken where a word doesn't fit), word-break: break-word against a later
    /// overflow-wrap: normal (two properties), keep-all (as normal here); the min-content size (anywhere, word-break:
    /// break-word and break-all count every letter, overflow-wrap: break-word doesn't, nowrap the whole line) and a flex
    /// item's automatic minimum from it; break-all's opportunities between letters, digits and punctuation in lines, and
    /// pair by pair in min-content boxes (each pair after an unbreakable "(((" so the box is as wide as that first part
    /// unless the pair breaks): after a letter before an opening bracket, a digit, a prefix sign or a vertical line; after
    /// a digit before a letter or an opening bracket; after an infix stop, a closing bracket or brace, an exclamation, a
    /// solidus or a postfix sign before a letter or a digit; postfix before prefix and prefix before postfix; a hyphen
    /// before a digit; '+' and '&amp;' as letters.
    /// Shaped around two differences of the engine that aren't these rules (Docs/Compositing-Port.md, "Three text rules"):
    /// the words measured letter by letter for the min-content size have their widest letter first ("MILL": Blink measures
    /// a later letter as the difference of two ceiled positions, ShapeResult::CachedWidth, where the engine ceils each
    /// letter), and break-all breaks lines only between letters that don't kern (Blink reshapes a line that ends inside a
    /// word, the engine keeps the kerning to the next letter).
    /// </summary>
    static readonly LayoutUnitFixture NoWrap = new("textrule-nowrap", 400, 700, Base + """
        .col { position: absolute; top: 0; width: 124px; }
        .b { width: 40px; margin: 0 0 6px 6px; background: #e3dccb; }
        .nw { white-space: nowrap; } .pre { white-space: pre; } .pw { white-space: pre-wrap; } .pl { white-space: pre-line; }
        .any { overflow-wrap: anywhere; } .bw { overflow-wrap: break-word; } .wbw { word-break: break-word; } .wba { word-break: break-all; } .wka { word-break: keep-all; }
        .mc { width: min-content; }
        .fx { display: flex; width: 40px; margin: 0 0 6px 6px; background: #e3dccb; } .fx > span { background: #cdb; }
        .w70 { width: 70px; } .w50 { width: 50px; } .w34 { width: 34px; }
        .pairs { position: absolute; left: 6px; top: 548px; width: 388px; display: flex; flex-wrap: wrap; gap: 4px 6px; }
        .pairs > div { width: min-content; word-break: break-all; background: #e3dccb; }
        """, """
        <div class="col" style="left: 0">
          <div class="b nw any"><span>PLATINUM</span></div>
          <div class="b nw bw"><span>PLATINUM</span></div>
          <div class="b nw wbw"><span>PLATINUM</span></div>
          <div class="b nw wba"><span>PLATINUM</span></div>
          <div class="b pre any"><span>PLATINUM</span></div>
          <div class="b any"><span>ab <span class="nw">PLATINUM</span></span></div>
          <div class="b mc nw any"><span>GOLD ORE</span></div>
          <div class="b nw any wbw wba"><span>PLATINUM</span></div>
        </div>
        <div class="col" style="left: 130px">
          <div class="b any"><span>PLATINUM</span></div>
          <div class="b bw"><span>PLATINUM</span></div>
          <div class="b wbw"><span>PLATINUM</span></div>
          <div class="b pw any"><span>PLATINUM</span></div>
          <div class="b pl any"><span>PLATINUM</span></div>
          <div class="b" style="word-break: break-word; overflow-wrap: normal"><span>PLATINUM</span></div>
          <div class="b wka"><span>PLATINUM</span></div>
        </div>
        <div class="col" style="left: 260px">
          <div class="b mc any"><span>MILL</span></div>
          <div class="b mc bw"><span>PLATINUM GOLD</span></div>
          <div class="b mc wbw"><span>MILL</span></div>
          <div class="b mc wba"><span>MILL</span></div>
          <div class="fx any"><span>PLATINUM</span></div>
          <div class="fx bw"><span>PLATINUM</span></div>
          <div class="b wba w70"><span>ab cdefghij</span></div>
          <div class="b wba w50"><span>12in.nn(i)n%n$4</span></div>
          <div class="b wba w34"><span>nn.4(n)i%n$1 n|n</span></div>
        </div>
        <div class="pairs">
          <div>(((n(n</div><div>(((n4)</div><div>(((4n)</div><div>(((n$4</div><div>(((n.n</div><div>(((n.4</div><div>(((n)n</div>
          <div>(((n)4</div><div>{{{n}n</div><div>(((n!n</div><div>(((4%n</div><div>(((4%$4</div><div>((($%)</div><div>(((-4)</div>
          <div>(((n/n</div><div>(((|n)</div><div>(((n|)</div><div>(((4(n</div><div>(((+n</div><div>(((n&amp;n</div><div>(((n)|</div>
          <div>(((n)$4</div><div>(((n!|</div><div>(((n!4</div><div>(((4%%</div><div>(((n.|</div><div>(((44</div>
        </div>
        """);

    /// <summary>
    /// Number fields with their text at the end of the editor: the field's appearance auto, textfield (as the old game's
    /// stat inputs), none (styled), -webkit-appearance: textfield, -moz-appearance (not Blink's), menulist-button, centred
    /// text; the spin button's own appearance: none (no room), display: none (no room), margins and paddings (more room), an
    /// author width (the theme's wins) and an author width under appearance: none (its own room); a border, and paddings
    /// under box-sizing: border-box (inside the theme's width); the game's own rule (textfield, the button's margin 0); a
    /// text field as textfield (no button); buttons shown by an author opacity (the theme paints them under the field's
    /// textfield and none, where their margins, paddings and border put them, and nothing under their own appearance:
    /// none); a long number clipped at the editor's end beside a button with wide margins; a focused field without
    /// appearance, its button shown, with a margin (the caret hidden, and its focus ring:
    /// at 1.65 the ring of a bordered field at 220 px lies a fraction of a pixel from Chromium's, a difference that isn't
    /// this rule).
    /// </summary>
    static readonly LayoutUnitFixture SpinRoom = new("textrule-spin-room", 500, 300, Base + """
        input { position: absolute; width: 120px; margin: 0; font: 16px Lato; text-align: right; color: #222; }
        .tf { appearance: textfield; } .no { appearance: none; border: 1px solid #998; background: #fff; } .wk { -webkit-appearance: textfield; } .moz { -moz-appearance: textfield; } .ml { appearance: menulist-button; }
        .c { text-align: center; }
        .sn::-webkit-inner-spin-button { appearance: none; } .sd::-webkit-inner-spin-button { display: none; } .sm::-webkit-inner-spin-button { margin: 0 6px; } .sw::-webkit-inner-spin-button { width: 30px; } .sp::-webkit-inner-spin-button { padding: 0 4px; } .snw::-webkit-inner-spin-button { appearance: none; width: 10px; }
        .sb::-webkit-inner-spin-button { border-left: 4px solid transparent; } .sbx::-webkit-inner-spin-button { box-sizing: border-box; padding: 0 4px; }
        .sm2::-webkit-inner-spin-button { margin: 0 12px; }
        .op::-webkit-inner-spin-button { opacity: 1; }
        .game { appearance: textfield; } .game::-webkit-inner-spin-button { margin: 0; }
        .cc { caret-color: transparent; outline: none; } .cc::-webkit-inner-spin-button { margin-right: 5px; }
        """, """
        <input type="number" value="42" style="left: 10px; top: 10px">
        <input type="number" value="42" class="tf" style="left: 10px; top: 40px">
        <input type="number" value="42" class="no" style="left: 10px; top: 70px">
        <input type="number" value="42" class="wk" style="left: 10px; top: 100px">
        <input type="number" value="42" class="moz" style="left: 10px; top: 130px">
        <input type="number" value="42" class="ml" style="left: 10px; top: 160px">
        <input type="number" value="42" class="tf c" style="left: 10px; top: 190px">
        <input type="number" value="42" class="game" style="left: 10px; top: 220px">
        <input type="number" value="42" class="tf sn" style="left: 170px; top: 10px">
        <input type="number" value="42" class="tf sd" style="left: 170px; top: 40px">
        <input type="number" value="42" class="sm" style="left: 170px; top: 70px">
        <input type="number" value="42" class="sw" style="left: 170px; top: 100px">
        <input type="number" value="42" class="sp" style="left: 170px; top: 130px">
        <input type="number" value="42" class="snw" style="left: 170px; top: 160px">
        <input type="text" value="42" class="tf" style="left: 170px; top: 190px">
        <input type="number" value="42" class="no cc" style="left: 170px; top: 220px" data-focus>
        <input type="number" value="42" class="tf op" style="left: 330px; top: 10px">
        <input type="number" value="42" class="no op" style="left: 330px; top: 40px">
        <input type="number" value="42" class="sm op" style="left: 330px; top: 70px">
        <input type="number" value="42" class="sp op" style="left: 330px; top: 100px">
        <input type="number" value="42" class="sb op" style="left: 330px; top: 130px">
        <input type="number" value="42" class="sbx op" style="left: 330px; top: 160px">
        <input type="number" value="42" class="snw op" style="left: 330px; top: 190px">
        <input type="number" value="1234567890123456789" class="sm2" style="left: 330px; top: 220px; text-align: left">
        """);

    /// <summary>
    /// Number line-heights whose float isn't their double (1.05: 40 px headings 42 px high, as the old game's sheet's,
    /// where 1.05 as a float makes 41.984375), one where both agree (1.06 at 20 px), the number through the font
    /// shorthand and inherited as a number by a larger font, the lh unit of a number line-height, text fields at 20 px
    /// whose inner editor keeps a 21 px line (ComputedLineHeight, an int, over the font size) instead of a normal one, and
    /// an empty text area two 42 px lines high (its rows times ComputedLineHeightAsFixed; empty, since the engine lays a
    /// text area's text out at the normal line height whatever the author's, a known difference: Docs/Editor.md, step 11).
    /// </summary>
    static readonly LayoutUnitFixture LineHeight = new("textrule-line-height", 300, 600, Base + """
        h1 { margin: 0; font: 400 40px/1.05 Lato; background: #e3dccb; }
        .n { font-size: 20px; line-height: 1.06; background: #ddd; }
        .inh { line-height: 1.05; } .inh p { margin: 0; font-size: 40px; background: #cdb; }
        .lh { display: flex; gap: 4px; font: 40px/1.05 Lato; } .lh i { width: 20px; background: #a64; } .lh .one { height: 1lh; } .lh .two { height: 2.5lh; }
        input { display: block; margin: 2px 0; font: 20px/1.05 Lato; width: 200px; }
        textarea { display: block; margin: 2px 0; font: 40px/1.05 Lato; width: 200px; resize: none; }
        """, """
        <h1>Heading one</h1><h1>Heading two</h1><h1>Heading three</h1>
        <div class="n">A line at 1.06</div>
        <div class="inh"><p>Inherited</p><p>as a number</p></div>
        <div class="lh"><i class="one"></i><i class="two"></i></div>
        <input type="text" value="A field at 1.05"><input type="number" value="12">
        <textarea rows="2"></textarea>
        """);

    public static readonly LayoutUnitFixture[] All = [NoWrap, SpinRoom, LineHeight];

    /// <summary>The configurations the boxes are compared at (LayoutUnitReferenceTests'): device scale and page zoom.</summary>
    static readonly (float Scale, float PageZoom)[] Configs = [(1, 1), (1.25f, 1), (1.5f, 1), (1.65f, 1), (1.75f, 1), (1.65f, 1.2f)];

    static readonly float[] PaintScales = [1, 1.65f];

    /// <summary>A box may be this far from Chromium's in device px (LayoutUnitReferenceTests').</summary>
    const float Tolerance = 0.002f;

    /// <summary>The pixels past 12 levels one field of the spin page may have (LayoutUnitReferenceTests.EachControlPaintsLikeChromium's).</summary>
    const int FieldLimit = 60;

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

    public static TheoryData<float> ScaleData => new(PaintScales);

    [Fact]
    public void TheFixtureListIsUpToDate()
    {
        var path = Path.Combine(ReferenceDirectory, "text-rules-fixtures.json");
        var json = JsonSerializer.Serialize(All.Select(f => new { name = f.Name, width = f.Width, height = f.Height, css = f.Css, html = f.Html, gameFonts = true, mainFrame = true }));
        if (!File.Exists(path) || File.ReadAllText(path) != json)
            File.WriteAllText(path, json);
        Assert.NotEmpty(json);
    }

    /// <summary>
    /// The text style a computed style hands the text layout carries a number line-height as Blink resolves it
    /// (FloatValueForLength: the font size times the percentage over 100): the first heading's, Chromium's 42 px box.
    /// </summary>
    [Fact]
    public void TheTextStyleKeepsANumbersLineHeight()
    {
        var reference = JsonDocument.Parse(File.ReadAllText(Path.Combine(ReferenceDirectory, "scaled", $"ui-{LineHeight.Name}@1.json"))).RootElement;
        var document = LayoutUnitReferenceTests.Build(LineHeight, 1);
        var heading = document.Body.Descendants().OfType<UiElement>().First(e => e.Tag == "h1");
        Assert.Equal("h1", reference[1][0].GetString());
        Assert.Equal(reference[1][4].GetSingle(), heading.Styles!.Style.ToTextStyle().LineHeight);
    }

    [Theory]
    [MemberData(nameof(BoxCases))]
    public void EveryBoxIsChromiums(string name, float scale, float pageZoom)
    {
        var fixture = All.Single(f => f.Name == name);
        var config = ConfigName(scale, pageZoom);
        var path = Path.Combine(ReferenceDirectory, "scaled", $"ui-{name}@{config}.json");
        if (!File.Exists(path))
            throw new FileNotFoundException("capture the fixture in Chromium (Tools/PaintReference with text-rules-fixtures.json)", path);
        var reference = JsonDocument.Parse(File.ReadAllText(path)).RootElement;
        var document = LayoutUnitReferenceTests.Build(fixture, scale, pageZoom);
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

    [Theory]
    [MemberData(nameof(PaintCases))]
    public void ThePagePaintsLikeChromium(string name, float scale)
    {
        var fixture = All.Single(f => f.Name == name);
        var document = LayoutUnitReferenceTests.Build(fixture, scale);
        var image = PaintReferenceTests.Render(gpu.Device, document, (int)MathF.Round(fixture.Width * scale), (int)MathF.Round(fixture.Height * scale));
        Golden.Match($"ui-{name}@{ConfigName(scale, 1)}", image, tolerance: 12, maxDifferentRatio: 0.002);
    }

    /// <summary>
    /// Field by field on the spin page, its box's pixels against Chromium's: the text of a field whose editor is 15 px off
    /// moves a couple of hundred pixels of it, which the whole page's 0.2 % can let through.
    /// </summary>
    [Theory]
    [MemberData(nameof(ScaleData))]
    public void EachSpinFieldPaintsLikeChromium(float scale)
    {
        var fixture = SpinRoom;
        var document = LayoutUnitReferenceTests.Build(fixture, scale);
        var image = PaintReferenceTests.Render(gpu.Device, document, (int)MathF.Round(fixture.Width * scale), (int)MathF.Round(fixture.Height * scale));
        var golden = Statventure.Imaging.ImageFile.Load(Golden.GoldenPath($"ui-{fixture.Name}@{ConfigName(scale, 1)}"));
        var rects = JsonDocument.Parse(File.ReadAllText(Path.Combine(ReferenceDirectory, "scaled", $"ui-{fixture.Name}@{ConfigName(scale, 1)}.json"))).RootElement;
        var problems = new List<string>();
        foreach (var r in rects.EnumerateArray())
        {
            if (r[0].GetString() != "input")
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
            if (beyond > FieldLimit)
                problems.Add($"the field at ({r[1].GetSingle()}, {r[2].GetSingle()}): {beyond} pixels past 12 levels");
        }
        Assert.True(problems.Count == 0, $"@{ConfigName(scale, 1)}: {string.Join("; ", problems)}");
    }
}

using System.Globalization;
using System.Text.Json;
using Statventure.Tests.Infrastructure;
using Statventure.UI.Dom;
using Statventure.UI.Paint;
using Statventure.UI.Paint.Compositing;

namespace Statventure.Tests.Ui;

/// <summary>
/// A box in the top layer over the page, as the game's tooltip is (an opaque leather background, a 2 px radius, a deep
/// shadow, Lato; one line and two), painted as Chromium 152 paints the same markup in a page's own main frame, the game's
/// document (Docs/Compositing-Port.md, "The iframe and the main frame"): the top layer (fixed, the whole view) is a cc
/// layer of its own there, not opaque, and the boxes' text isn't known to be on an opaque background (the radius and the
/// shadow each end the box's opaque analysis), so its LCD text is refused ("background-color-not-opaque") and the text is
/// grayscale, while the page's own text keeps LCD. The fixtures are main-frame ones (<c>mainFrame</c>: Tools/PaintReference
/// captures them as a page's own document, not in its iframe, where the top layer would merge into the host's opaque layer
/// and keep LCD text): Golden/ui-top-layer-*@1.png and @1.65.png, made by Tools/Scripts/make-scaled-paint-references.sh
/// 1 1 top-layer-fixtures.json and 1.65 1 top-layer-fixtures.json; compared at the paint fixtures' limits (12 levels,
/// 0.2 % of the pixels).
/// </summary>
[Collection("GPU")]
public class TopLayerReferenceTests(GpuFixture gpu)
{
    /// <summary>A page with two lines of text (whole device pixels at 1 and 1.65: sizes and places in multiples of 20 px).</summary>
    const string PageCss = """
        html { background: #f3e8cc; }
        body { margin: 0; font: 16px/20px Lato; color: #22160d; }
        p { position: absolute; left: 40px; margin: 0; }
        """;

    const string PageHtml = """<p style="top: 20px">Forage · 1 hour</p><p style="top: 140px">Rest at the fire · 8 hours</p>""";

    /// <summary>The engine's user-agent rules for the top layer and its tooltip (UserAgentStyles), for Chromium's side, where they are a page's rules of no specificity.</summary>
    const string TopLayerUaCss = """
        :where(sv-top-layer) { display: block; position: fixed; left: 0; top: 0; right: 0; bottom: 0; z-index: 2147483647; pointer-events: none; }
        :where(sv-top-layer > *) { position: absolute; }
        :where(sv-tooltip) {
            display: block; box-sizing: content-box; padding: 3px 7px 4px; border: 1px solid rgb(60, 60, 60); background-color: white;
            color: rgb(60, 60, 60); font: 12px sans-serif; white-space: pre-wrap; overflow-wrap: break-word; pointer-events: none;
        }
        """;

    /// <summary>A fixture: its page and the boxes in its top layer (each a style and its text), and the LCD verdict Chromium's main frame gives the top layer's cc layer.</summary>
    public sealed record TopLayerFixture(string Name, int Width, int Height, string TooltipCss, (string Style, string Text)[] Tooltips, string Lcd);

    const string Leather = "padding: 11px 13px; border: 1px solid #6e5538; color: #f0dcb4; background: linear-gradient(180deg, #4a3624, #2a1d14); font: 400 14px/1.6 Lato;";

    static readonly (string Style, string Text)[] TwoAndOne =
    [
        ("left: 60px; top: 40px; max-width: 260px;", "Search the ground here for potion ingredients: one hour, and the area's encounter risk"),
        ("left: 60px; top: 160px;", "Rest until morning"),
    ];

    /// <summary>The game's tooltip (controls.css's <c>sv-tooltip</c> in fixed colours), two lines and one: its radius and its shadow each end its opaque analysis.</summary>
    static readonly TopLayerFixture Tooltip = new("top-layer-tooltip", 480, 240,
        $"sv-tooltip {{ {Leather} border-radius: 2px; box-shadow: 0 10px 30px #000000b0; }}", TwoAndOne, "background-color-not-opaque");

    /// <summary>
    /// The variations that decide it, the two-line box alone in each (a layer has one rect known to be opaque, so a second
    /// box's text would be off it, grayscale in Chromium's main frame and in the engine whatever the box): neither radius
    /// nor shadow (its text known to be on its opaque background: LCD), the radius alone, the shadow alone (each grayscale).
    /// The radius's box has a height of whole pixels: under its opaque border the rounded gradient is painted under a
    /// rounded clip (the bleed avoidance's shrinking), and with the box's own height (89.17 px over a tile of 89) one tile
    /// wouldn't cover it, so it would be a pattern, not known to be opaque either (DrawTiledBackground), as in the tooltip.
    /// </summary>
    static readonly TopLayerFixture[] Variations =
    [
        new("top-layer-plain", 480, 240, $"sv-tooltip {{ {Leather} }}", TwoAndOne[..1], "none"),
        new("top-layer-radius", 480, 240, $"sv-tooltip {{ {Leather} border-radius: 2px; height: 67px; }}", TwoAndOne[..1], "background-color-not-opaque"),
        new("top-layer-shadow", 480, 240, $"sv-tooltip {{ {Leather} box-shadow: 0 10px 30px #000000b0; }}", TwoAndOne[..1], "background-color-not-opaque"),
    ];

    public static readonly TopLayerFixture[] All = [Tooltip, .. Variations];

    public static TheoryData<string, float> Cases
    {
        get
        {
            var data = new TheoryData<string, float>();
            foreach (var fixture in All)
                foreach (var scale in new[] { 1f, 1.65f })
                    data.Add(fixture.Name, scale);
            return data;
        }
    }

    static string Number(float value) => value.ToString(CultureInfo.InvariantCulture);

    static string ReferenceDirectory => Path.Combine(Path.GetDirectoryName(Golden.GoldenPath("x"))!, "..", "Reference");

    /// <summary>The list Chromium captures: each fixture as a main frame, its top layer's markup at the body's end with the engine's top-layer rules.</summary>
    [Fact]
    public void TheFixtureListIsUpToDate()
    {
        var path = Path.Combine(ReferenceDirectory, "top-layer-fixtures.json");
        var json = JsonSerializer.Serialize(All.Select(f => new
        {
            name = f.Name, width = f.Width, height = f.Height, css = LayoutFixtures.BaseCss + PageCss + TopLayerUaCss + f.TooltipCss,
            html = PageHtml + "<sv-top-layer>" + string.Concat(f.Tooltips.Select(t => $"<sv-tooltip style=\"{t.Style}\">{t.Text}</sv-tooltip>")) + "</sv-top-layer>",
            scrollX = 0, scrollY = 0, windowFocused = true, mainFrame = true,
        }));
        if (!File.Exists(path) || File.ReadAllText(path) != json)
            File.WriteAllText(path, json);
        Assert.All(JsonDocument.Parse(json).RootElement.EnumerateArray(), f => Assert.True(f.GetProperty("mainFrame").GetBoolean()));
    }

    /// <summary>The fixture in the engine: the page, and the boxes in the document's own top layer.</summary>
    public static UiDocument Build(TopLayerFixture fixture, float scale)
    {
        var document = PaintReferenceTests.Build(new PaintFixture(fixture.Name, fixture.Width, fixture.Height, PageCss + fixture.TooltipCss, PageHtml), scale);
        foreach (var (style, text) in fixture.Tooltips)
        {
            var tooltip = new UiElement("sv-tooltip");
            tooltip.SetAttribute("style", style);
            tooltip.Append(new UiText(text));
            document.TopLayer.Append(tooltip);
        }
        document.Update(0);
        return document;
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void TheTopLayerPaintsAsInChromiumsMainFrame(string name, float scale)
    {
        var fixture = All.Single(f => f.Name == name);
        var image = PaintReferenceTests.Render(gpu.Device, Build(fixture, scale), (int)MathF.Round(fixture.Width * scale), (int)MathF.Round(fixture.Height * scale));
        Golden.Match($"ui-{name}@{Number(scale)}", image, tolerance: 12, maxDifferentRatio: 0.002);
    }

    /// <summary>
    /// The layers Chromium's main frame makes of it (its cc layer trace, Docs/Compositing-Port.md): the page's scrolling
    /// contents, opaque with LCD text, and the top layer apart, an overlap layer not opaque, whose LCD text is refused as
    /// "background-color-not-opaque" when a radius or a shadow ends its boxes' opaque analysis, and kept without either.
    /// </summary>
    [Theory]
    [MemberData(nameof(Cases))]
    public void TheTopLayerIsALayerOfItsOwnWithItsLcdVerdict(string name, float scale)
    {
        var fixture = All.Single(f => f.Name == name);
        using var painter = new UiPainter(Build(fixture, scale));
        var layers = painter.AnalyzeLayers().Where(l => l.DrawsContent && l.Type != LayerType.ScrollHitTest).ToList();
        Assert.Equal(2, layers.Count);
        Assert.True(layers[0].ContentsOpaque && layers[0].LcdAllowed, $"the page's layer: {layers[0]}");
        var top = layers[1];
        Assert.Equal("sv-top-layer", top.Chunks[0].Owner?.Element?.Tag);
        Assert.Equal(LayerType.Overlap, top.Type);
        Assert.False(top.ContentsOpaque);
        Assert.Equal(fixture.Lcd, top.LcdReason);
        var lcd = fixture.Lcd == "none";
        Assert.Equal(lcd, top.LcdAllowed);
        Assert.Contains(top.Chunks, c => c.HasText);
        Assert.All(top.Chunks.Where(c => c.HasText), c => Assert.Equal(lcd, c.LcdText));
    }
}

using System.Globalization;
using System.Text.Json;
using Statventure.Tests.Infrastructure;
using Statventure.Text;
using Statventure.UI.Dom;
using Statventure.UI.Layout;
using Statventure.UI.Paint;
using Statventure.UI.Paint.Compositing;

namespace Statventure.Tests.Ui;

/// <summary>
/// Chromium 152's DrawingDisplayItem::TightenVisualRect (drawing_display_item.h 80-100, drawing_display_item.cc 285-321;
/// Docs/Compositing-Port.md, "Blink rules collected"): a drawing display item whose record is one op, a fill of a rect or a
/// rounded rect with a plain colour (fill style, no looper, colour or image filter, blend src or src-over, no shader),
/// takes that rect as its visual rect instead of the box's ink overflow rect, so its paint chunk's bounds are the snapped
/// rect the box painted. GM2 step 12 found it in the editor's <c>preview-effect</c> at 1.65: a status line under a list
/// that scrolls, its top at 1014.75 device px where the list ends; Chromium keeps the line in the page's layer (its border
/// one FillRect from 1015), the engine made it an overlap layer (the box's visual rect from 1014) with grayscale text.
/// <para>
/// The page: a top bar, a list that overflows and scrolls (composited at both scales, its contents an overlap layer over
/// its scrollbar), and a 25 px status line at the bottom of a 640 px page, so at 1.65 the list ends where the line begins,
/// at 1014.75. One case per fixture: the line with one plain fill (a border's top side, a background, a rounded
/// background: no layer of its own) and with records that aren't one (a background and a border, a gradient, a
/// translucent side, drawn as a path: an overlap layer at 1.65); and a line that really overlaps the list (an overlap
/// layer at both scales). The layers are Chromium's cc trace's (the layer probe, main frame, 2026-11-15); the pictures
/// Golden/ui-tight-*@1.png and @1.65.png (Tools/Scripts/make-scaled-paint-references.sh 1 1 visual-rect-fixtures.json and
/// 1.65 1 visual-rect-fixtures.json, main frames), at the paint fixtures' limits.
/// </para>
/// </summary>
[Collection("GPU")]
public class VisualRectTighteningReferenceTests(GpuFixture gpu)
{
    /// <summary>
    /// A case: the status line's own rule, whether Chromium makes it an overlap layer at 1 and at 1.65, and whether that
    /// layer may draw LCD text (its trace's lcd_text_disallowed_reason: none, or background-color-not-opaque) and is opaque
    /// (contents_opaque).
    /// </summary>
    public sealed record TightFixture(string Name, string StatusCss, bool LayerAt1, bool LayerAt165, bool LayerLcd = false, bool LayerOpaque = false);

    const int Width = 400, Height = 640;

    const string PageCss = """
        html, body { margin: 0; height: 100%; }
        body { font-family: Lato; font-size: 13px; background: #f3e8cc; color: #22160d; }
        .shell { display: flex; flex-direction: column; height: 100%; }
        .top { flex: none; height: 33px; background: #e9dcbc; }
        .pane { flex: 1; min-height: 0; overflow: auto; background: #efe2c2; }
        .row { height: 30px; line-height: 30px; padding: 0 8px; white-space: nowrap; }
        .status { flex: none; height: 25px; line-height: 25px; padding: 0 8px; overflow: hidden; white-space: nowrap; }
        """;

    static readonly string PageHtml = "<div class=\"shell\"><div class=\"top\">The top bar</div><div class=\"pane\">"
        + string.Concat(Enumerable.Range(1, 30).Select(i => $"<div class=\"row\">Row {i}: an entry of the list</div>"))
        + "</div><div class=\"status\">Opened the pack: 1 entry</div></div>";

    public static readonly TightFixture[] All =
    [
        // One FillRect of the border's top side (BoxBorderPainter's general path: one side, solid, opaque, square).
        new("tight-border", ".status { border-top: 1px solid #8a7356; }", false, false),
        // One FillRect of the background.
        new("tight-background", ".status { background: #e4d3a8; }", false, false),
        // One FillRRect (a DrawRRect) of the rounded background.
        new("tight-rounded", ".status { background: #e4d3a8; border-radius: 4px; }", false, false),
        // Two ops: the background and the border's side.
        new("tight-background-border", ".status { background: #e4d3a8; border-top: 1px solid #8a7356; }", false, true, LayerLcd: true),
        // Two ops, the first a plain fill: the background's colour, then its image (a gradient) over it.
        new("tight-background-image", ".status { background: #e4d3a8 linear-gradient(rgba(138, 115, 86, 0.2), rgba(138, 115, 86, 0)); }", false, true, LayerLcd: true),
        // One op with a shader.
        new("tight-gradient", ".status { background: linear-gradient(#e4d3a8, #d8c595); }", false, true, LayerLcd: true),
        // One op, a path (PaintBorderFastPath: fewer than four sides, solid, translucent, square).
        new("tight-translucent-border", ".status { border-top: 1px solid rgba(138, 115, 86, 0.5); }", false, true),
        // A real overlap: the line 6 px over the list.
        new("tight-overlap", ".status { border-top: 1px solid #8a7356; margin-top: -6px; }", true, true),
        // A real overlap (the list 6 px over the line) of one plain fill whose top, 1014.27 device px at 1.65, the snapped
        // rect rounds down and the box's whole pixels inside it don't: its layer's bounds start at 1014, and its fill,
        // clipped by its tightened visual rect, covers them (contents opaque).
        new("tight-background-overlap", ".status { height: 25.3px; background: #e4d3a8; margin-top: -6px; }", true, true, LayerLcd: true, LayerOpaque: true),
    ];

    static readonly float[] Scales = [1, 1.65f];

    public static TheoryData<string, float> Cases
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

    static string Number(float value) => value.ToString(CultureInfo.InvariantCulture);

    static string ReferenceDirectory => Path.Combine(Path.GetDirectoryName(Golden.GoldenPath("x"))!, "..", "Reference");

    static string CssOf(TightFixture fixture) => LayoutFixtures.BaseCss + PageCss + "\n" + fixture.StatusCss;

    [Fact]
    public void TheFixtureListIsUpToDate()
    {
        var path = Path.Combine(ReferenceDirectory, "visual-rect-fixtures.json");
        var json = JsonSerializer.Serialize(All.Select(f => new
        {
            name = f.Name, width = Width, height = Height, css = CssOf(f), html = PageHtml,
            scrollX = 0, scrollY = 0, windowFocused = true, mainFrame = true,
        }));
        if (!File.Exists(path) || File.ReadAllText(path) != json)
            File.WriteAllText(path, json);
        Assert.All(JsonDocument.Parse(json).RootElement.EnumerateArray(), f => Assert.True(f.GetProperty("mainFrame").GetBoolean()));
    }

    static readonly Lazy<FontLibrary> Fonts = new(() =>
    {
        var fonts = new FontLibrary();
        var dir = Path.Combine(AppContext.BaseDirectory, "Data", "Fonts");
        fonts.AddFile(Path.Combine(dir, "Lato-Regular.ttf"), "Lato", 400);
        fonts.AddFile(Path.Combine(dir, "Lato-Bold.ttf"), "Lato", 700, weightMax: 900);
        return fonts;
    });

    static UiDocument Build(TightFixture fixture, float scale)
    {
        var document = new UiDocument(Fonts.Value);
        document.SetViewport(Width, Height);
        document.SetZoom(scale);
        document.AddStylesheet(CssOf(fixture), fixture.Name);
        Markup.SetInnerHtml(document.Body, PageHtml);
        document.Update(0);
        return document;
    }

    /// <summary>A drawing layer by what it holds, in the words of Chromium's trace: the page's, the scrollbar, the list's contents, the status line.</summary>
    static string Describe(PendingLayer layer) => layer.Type switch
    {
        LayerType.Scrollbar => "scrollbar",
        _ when layer.Chunks[0].Owner?.Element?.GetAttribute("class") is { } name => $"{name} {layer.Type.ToString().ToLowerInvariant()}",
        _ => $"page {layer.Type.ToString().ToLowerInvariant()}",
    };

    /// <summary>
    /// Chromium's drawing layers (the cc trace, main frame, at both scales): the page's (Scrolling background of
    /// LayoutView, opaque), the list's vertical scrollbar, the list's contents (LayoutBlockFlow DIV class='pane',
    /// Overlap, opaque), and the status line (LayoutBlockFlow DIV class='status', Overlap) where the case makes it one.
    /// </summary>
    static string[] ChromiumLayers(TightFixture fixture, float scale) =>
        ["page other", "scrollbar", "pane overlap", .. (scale == 1 ? fixture.LayerAt1 : fixture.LayerAt165) ? new[] { "status overlap" } : []];

    [Theory]
    [MemberData(nameof(Cases))]
    public void TheLayersAreChromiums(string name, float scale)
    {
        var fixture = All.Single(f => f.Name == name);
        using var painter = new UiPainter(Build(fixture, scale));
        var layers = painter.AnalyzeLayers().Where(l => l.DrawsContent && l.Type != LayerType.ScrollHitTest).ToList();
        Assert.Equal(ChromiumLayers(fixture, scale), layers.Select(Describe));
        // The status line's text: in the page's layer, opaque and with LCD text, where Chromium keeps it there; else in
        // its own layer with Chromium's LCD verdict.
        var status = layers.Single(l => l.Chunks.Any(c => c.HasText && c.Owner?.Element?.GetAttribute("class") == "status"));
        if (Describe(status) == "page other")
            Assert.True(status.ContentsOpaque && status.LcdAllowed, $"the page's layer: {status}");
        else
            Assert.True(status.LcdAllowed == fixture.LayerLcd && status.ContentsOpaque == fixture.LayerOpaque,
                $"the status line's layer: {status}, opaque {status.ContentsOpaque}; Chromium's LCD {fixture.LayerLcd}, opaque {fixture.LayerOpaque}");
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void ThePageLooksAsInChromium(string name, float scale)
    {
        var fixture = All.Single(f => f.Name == name);
        var document = Build(fixture, scale);
        var width = (int)MathF.Round(Width * scale);
        var height = (int)MathF.Round(Height * scale);
        var render = PaintReferenceTests.Render(gpu.Device, document, width, height);
        Golden.Match($"ui-{name}@{Number(scale)}", render, tolerance: 12, maxDifferentRatio: 0.002);
    }
}

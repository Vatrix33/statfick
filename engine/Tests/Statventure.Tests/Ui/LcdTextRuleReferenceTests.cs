using System.Globalization;
using System.Numerics;
using System.Text.Json;
using Statventure.Core;
using Statventure.Imaging;
using Statventure.Tests.Infrastructure;
using Statventure.Tests.Text;
using Statventure.Text;
using Statventure.UI.Dom;
using Statventure.UI.Layout;
using Statventure.UI.Paint;
using Statventure.UI.Paint.Compositing;
using CanvasRenderer = Statventure.Canvas.Canvas;

namespace Statventure.Tests.Ui;

/// <summary>
/// Two of Chromium 152's rules for LCD text that GM1 step 9's screens met (Docs/Compositing-Port.md, "Blink rules
/// collected"), on a page's own main frame as the game's document is:
/// <list type="bullet">
/// <item>(A) Skia never makes a glyph larger than 48 device pixels an LCD mask (<c>SK_MAX_SIZE_FOR_LCDTEXT</c>,
/// <c>too_big_for_lcd</c>): text at 47 and 48 device px keeps LCD, at 49 and above it is grayscale, at both scales, an
/// icon (always grayscale, GM1 step 7) in each line.</item>
/// <item>(B) in one cc layer, text in no save layer keeps the layer's verdict; text in a filter's or a blend mode's save
/// layer is grayscale; text in opacity layers keeps LCD unless the layer holds a filter's or a blend mode's save layer
/// (cc's <c>save_layer_alpha_should_preserve_lcd_text</c>, Skia's layers without <c>kPreserveLCDText</c>). The root
/// element is the isolation group around blending descendants no nearer stacking context holds (Blink's
/// <c>ForcesStackingContext</c> for the document element), and the view's background is painted in it (ViewPainter's Root
/// Element Group), so a blending descendant of the root turns the page's own text grayscale, and one inside a stacking
/// context of its own doesn't. A layer a decomposited blend mode was merged into knows no text on an opaque ground
/// (PendingLayer::Merge), so where LCD text is strongly preferred (below 1.5 device px per CSS px) the opacity box after
/// the root's blend is a layer of its own, and keeps LCD (at 1, not at 1.65).</item>
/// </list>
/// Golden/ui-lcd-*@1.png and @1.65.png, made by Tools/Scripts/make-scaled-paint-references.sh 1 1
/// lcd-text-rule-fixtures.json and 1.65 1 lcd-text-rule-fixtures.json (main frames); compared at the paint fixtures'
/// limits as a whole, and case by case by the verdict the pixels show (<see cref="Fringes"/>: LCD text leaves the line
/// from its ground to its ink, grayscale text stays on it; the icons' boxes left out): Chromium's is the case's
/// expectation (so the fixture exercises the rule) and the engine's is Chromium's. The layers are Chromium's cc trace's
/// (the layer probe, 2026-11-14), with each (B) case's text chunks given the case's verdict.
/// </summary>
[Collection("GPU")]
public class LcdTextRuleReferenceTests(GpuFixture gpu)
{
    /// <summary>
    /// A fixture: its page and its cases: name, box in CSS px and the case's text's LCD verdict in Chromium at 1 and at
    /// 1.65.
    /// </summary>
    public sealed record LcdFixture(string Name, int Width, int Height, string Css, string Html, (string Case, RectF Box, bool At1, bool At165)[] Cases);

    const string Page = "html { background: #f3e8cc; } body { margin: 0; font-family: Lato; color: #22160d; } ";

    // (A): a line at each size, its text then an icon (✦, one of the game's) and the size. Sizes in CSS px that give 47,
    // 48 and 49 device px at 1 (left) and at 1.65 (right: Blink's effective font size, to 0.01 px truncated), and well
    // above; and two under a scale of 2 (Skia's text size the font's, the matrix the scale: 24 px is 48 device px at 1,
    // 25 px 50), without the icon (the engine draws an icon under a scale hollow: GM1 step 9's finding); LCD at 1 and at
    // 1.65.
    const string SizeCss = Page + "p { position: absolute; margin: 0; white-space: nowrap; } .x2 { transform: scale(2); transform-origin: 0 0; }";

    static readonly (string Size, float Top, float LineHeight, float Left, bool Scaled, bool At1, bool At165)[] Sizes =
    [
        ("47", 20, 60, 20, false, true, false), ("48", 90, 60, 20, false, true, false), ("49", 160, 60, 20, false, false, false),
        ("72", 230, 100, 20, false, false, false),
        ("28.4849", 20, 40, 460, false, true, true), ("29.091", 70, 40, 460, false, true, true), ("29.697", 120, 40, 460, false, true, false),
        ("16", 170, 20, 460, false, true, true),
        ("24", 210, 30, 460, true, true, false), ("25", 280, 30, 460, true, false, false),
    ];

    static string SizeHtml() => string.Concat(Sizes.Select(s =>
        $"<p{(s.Scaled ? " class=\"x2\"" : "")} style=\"left: {s.Left}px; top: {s.Top}px; font-size: {s.Size}px; line-height: {s.LineHeight}px\">Ember{(s.Scaled ? "" : " ✦")} {s.Size}</p>"));

    // (B): boxes of one line of 16 px text in one layer.
    const string BoxCss = Page + """
        body { font-size: 16px; line-height: 20px; }
        .k { position: absolute; left: 20px; width: 440px; height: 40px; padding: 10px; box-sizing: border-box; white-space: nowrap; }
        .f { filter: grayscale(1); opacity: 0.5; background: #e9dcbc; }
        .o { opacity: 0.6; background: #e9dcbc; }
        .b { mix-blend-mode: multiply; background: #e9dcbc; }
        .i { isolation: isolate; background: #efe2c2; }
        """;

    static RectF Row(int index) => new(20, 20 + index * 60, 440, 40);

    static string Box(int index, string classes, string text) => $"<div class=\"k {classes}\" style=\"top: {20 + index * 60}px\">{text}</div>";

    public static readonly LcdFixture[] All =
    [
        new("lcd-size", 800, 360, SizeCss, SizeHtml(),
            [.. Sizes.Select(s => ($"size {s.Size}{(s.Scaled ? " ×2" : "")}", new RectF(s.Left, s.Top, 340, s.LineHeight * (s.Scaled ? 2 : 1)), s.At1, s.At165))]),
        // The page's disabled "Return to combat": a filter (and opacity) on one box, the page's text beside it.
        new("lcd-filter", 480, 220, BoxCss,
            Box(0, "", "On the page: the layer's verdict") + Box(1, "f", "In a filter: grayscale") + Box(2, "o", "In an opacity layer, a filter in the layer"),
            [("page", Row(0), true, true), ("filter", Row(1), false, false), ("opacity", Row(2), false, false)]),
        new("lcd-opacity", 480, 160, BoxCss,
            Box(0, "", "On the page: the layer's verdict") + Box(1, "o", "In an opacity layer: LCD"),
            [("page", Row(0), true, true), ("opacity", Row(1), true, true)]),
        // A blending descendant of the root: the root is its isolation group, the page's own text in it. The opacity box
        // after it: in the same layer at 1.65, a layer of its own at 1 (LCD text strongly preferred).
        new("lcd-blend", 480, 220, BoxCss,
            Box(0, "", "On the page: in the root's isolation group") + Box(1, "b", "In a blend mode: grayscale") + Box(2, "o", "In an opacity layer after the blend"),
            [("page", Row(0), false, false), ("blend", Row(1), false, false), ("opacity", Row(2), true, false)]),
        // The same blend inside a stacking context of its own: that is the isolation group, the page's text outside it.
        // The group has an opaque ground of its own, as the game's isolation groups have (the shop hall's, the battle
        // stage's, the cinematics'): the engine draws an isolation group in the page's own pixels, not apart (a blend in it
        // sees what is beneath the group; GM1 step 9's finding D), which an opaque ground makes no matter.
        new("lcd-blend-isolated", 480, 220, BoxCss,
            Box(0, "", "On the page: the layer's verdict") + $"<div class=\"i\" style=\"position: absolute; left: 0; top: 80px; width: 480px; height: 100px\">{Box(0, "b", "In a blend mode: grayscale")}{Box(1, "", "Beside it in the group: grayscale")}</div>",
            [("page", Row(0), true, true), ("blend", new RectF(20, 100, 440, 40), false, false), ("group", new RectF(20, 160, 440, 40), false, false)]),
    ];

    /// <summary>
    /// Chromium's layers (its cc trace, main frame): one, the view's scrolling contents, opaque, its LCD text allowed;
    /// except lcd-blend at 1, three: the view's base backdrop (opaque), the root's isolation group (not opaque: its text
    /// grayscale) and the opacity box (Overlap, LCD).
    /// </summary>
    static int ChromiumLayers(string name, float scale) => name == "lcd-blend" && scale == 1 ? 3 : 1;

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

    [Fact]
    public void TheFixtureListIsUpToDate()
    {
        var path = Path.Combine(ReferenceDirectory, "lcd-text-rule-fixtures.json");
        var json = JsonSerializer.Serialize(All.Select(f => new
        {
            name = f.Name, width = f.Width, height = f.Height, css = LayoutFixtures.BaseCss + f.Css, html = f.Html,
            scrollX = 0, scrollY = 0, windowFocused = true, mainFrame = true,
        }));
        if (!File.Exists(path) || File.ReadAllText(path) != json)
            File.WriteAllText(path, json);
        Assert.All(JsonDocument.Parse(json).RootElement.EnumerateArray(), f => Assert.True(f.GetProperty("mainFrame").GetBoolean()));
    }

    /// <summary>The engine's library: the game's Lato and its icons (GM1 step 7), which Chromium draws with its fallback fonts in the same boxes.</summary>
    static readonly Lazy<FontLibrary> Fonts = new(() =>
    {
        var fonts = new FontLibrary();
        var dir = Path.Combine(AppContext.BaseDirectory, "Data", "Fonts");
        fonts.AddFile(Path.Combine(dir, "Lato-Regular.ttf"), "Lato", 400);
        fonts.AddFile(Path.Combine(dir, "Lato-Bold.ttf"), "Lato", 700, weightMax: 900);
        fonts.AddIcons(IconFiles.All.Select(i => i.Glyph));
        return fonts;
    });

    public static UiDocument Build(LcdFixture fixture, float scale)
    {
        var document = new UiDocument(Fonts.Value);
        document.SetViewport(fixture.Width, fixture.Height);
        document.SetZoom(scale);
        document.AddStylesheet(LayoutFixtures.BaseCss + fixture.Css, fixture.Name);
        Markup.SetInnerHtml(document.Body, fixture.Html);
        document.Update(0);
        return document;
    }

    /// <summary>
    /// The engine's render with the icons' boxes (device px, a pixel around: the cluster by its line) taken from the
    /// golden, and those boxes.
    /// </summary>
    (RgbaImage Render, RgbaImage Golden, List<RectF> Icons) Rendered(LcdFixture fixture, float scale)
    {
        var document = Build(fixture, scale);
        var width = (int)MathF.Round(fixture.Width * scale);
        var height = (int)MathF.Round(fixture.Height * scale);
        var render = PaintReferenceTests.Render(gpu.Device, document, width, height);
        var golden = ImageFile.Load(Golden.GoldenPath($"ui-{fixture.Name}@{Number(scale)}"));
        Assert.Equal((width, height), (golden.Width, golden.Height));
        var icons = IconBoxes(document);
        foreach (var box in icons)
            for (var y = Math.Max(0, (int)MathF.Floor(box.Top)); y < Math.Min(height, (int)MathF.Ceiling(box.Bottom)); y++)
                for (var x = Math.Max(0, (int)MathF.Floor(box.Left)); x < Math.Min(width, (int)MathF.Ceiling(box.Right)); x++)
                    Array.Copy(golden.Pixels, (y * width + x) * 4, render.Pixels, (y * width + x) * 4, 4);
        return (render, golden, icons);
    }

    static List<RectF> IconBoxes(UiDocument document)
    {
        var boxes = new List<RectF>();
        var icons = IconFiles.All.Select(i => i.Glyph.CodePoint).ToHashSet();
        foreach (var root in document.Layout.Viewport.Descendants())
        {
            if (root.Inline is not { } inline)
                continue;
            var origin = root.ViewportRect;
            foreach (var line in inline.Lines)
                foreach (var item in line.Items)
                {
                    if (item.Kind != LineItemKind.Text || item.Box is not { } box || item.Clusters is not { } clusters)
                        continue;
                    var processed = InlineLayout.TextOf.TryGetValue(box, out var p) ? p : box.Text;
                    foreach (var cluster in clusters)
                        if (HasIcon(processed.AsSpan(cluster.Start, cluster.End - cluster.Start), icons))
                            boxes.Add(new RectF(origin.X + cluster.X - 1, origin.Y + line.Top - 1, cluster.Width + 2, line.Height + 2));
                }
        }
        return boxes;
    }

    static bool HasIcon(ReadOnlySpan<char> text, HashSet<int> icons)
    {
        foreach (var rune in text.EnumerateRunes())
            if (icons.Contains(rune.Value))
                return true;
        return false;
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void TheTextLooksAsInChromium(string name, float scale)
    {
        var fixture = All.Single(f => f.Name == name);
        var (render, _, _) = Rendered(fixture, scale);
        Golden.Match($"ui-{name}@{Number(scale)}", render, tolerance: 12, maxDifferentRatio: 0.002);
    }

    /// <summary>
    /// Case by case, LCD or grayscale as the pixels show it (<see cref="Fringes"/>), the icons' boxes left out:
    /// Chromium's is the case's expectation, and the engine's is Chromium's. A line drawn grayscale where Chromium's is LCD
    /// (or the other way) can stay under the whole picture's limit; its fringes can't.
    /// </summary>
    [Theory]
    [MemberData(nameof(Cases))]
    public void EachCaseIsLcdOrGrayscaleAsInChromium(string name, float scale)
    {
        var fixture = All.Single(f => f.Name == name);
        var (render, golden, icons) = Rendered(fixture, scale);
        var problems = new List<string>();
        foreach (var (@case, box, at1, at165) in fixture.Cases)
        {
            var expected = scale == 1 ? at1 : at165;
            var chromium = Fringes(golden, box, scale, icons);
            var engine = Fringes(render, box, scale, icons);
            if (chromium > FringesOfLcd == expected && engine > FringesOfLcd == expected)
                continue;
            problems.Add($"{@case}: LCD expected {expected}, Chromium's {chromium} fringe pixels, the engine's {engine}");
        }
        Assert.True(problems.Count == 0, $"{name}@{Number(scale)} (LCD past {FringesOfLcd} fringe pixels): " + string.Join("; ", problems));
    }

    /// <summary>More fringe pixels than this, LCD (Chromium's grayscale lines here have none, its LCD lines 330 and more).</summary>
    const int FringesOfLcd = 32;

    /// <summary>
    /// Pixels of a box (CSS px) off the line from its ground (its top-left pixel inside) to its ink (its darkest pixel)
    /// by more than 16 levels in a channel. Grayscale text covers the three channels alike, so every pixel of it is the
    /// ground and the ink mixed, on that line, whatever opacity, filter or blend mode the line is in; LCD text covers
    /// each channel apart, and its fringes leave it. The icons' boxes are left out.
    /// </summary>
    static int Fringes(RgbaImage image, RectF box, float scale, List<RectF> icons)
    {
        var (left, top) = ((int)MathF.Floor(box.Left * scale), (int)MathF.Floor(box.Top * scale));
        var (right, bottom) = ((int)MathF.Ceiling(box.Right * scale), (int)MathF.Ceiling(box.Bottom * scale));
        bool Counted(int x, int y) => !icons.Any(i => x + 0.5f >= i.Left && x + 0.5f < i.Right && y + 0.5f >= i.Top && y + 0.5f < i.Bottom);
        Vector3 At(int x, int y)
        {
            var i = (y * image.Width + x) * 4;
            return new Vector3(image.Pixels[i], image.Pixels[i + 1], image.Pixels[i + 2]);
        }
        var ground = At(left + 1, top + 1);
        var ink = ground;
        for (var y = top; y < bottom; y++)
            for (var x = left; x < right; x++)
                if (Counted(x, y) && At(x, y) is var p && p.X + p.Y + p.Z < ink.X + ink.Y + ink.Z)
                    ink = p;
        var line = ink - ground;
        var length = Vector3.Dot(line, line);
        var fringes = 0;
        for (var y = top; y < bottom; y++)
            for (var x = left; x < right; x++)
            {
                if (!Counted(x, y))
                    continue;
                var p = At(x, y);
                var t = length == 0 ? 0 : Math.Clamp(Vector3.Dot(p - ground, line) / length, 0, 1);
                var off = Vector3.Abs(p - (ground + t * line));
                if (MathF.Max(off.X, MathF.Max(off.Y, off.Z)) > 16)
                    fringes++;
            }
        return fringes;
    }

    /// <summary>
    /// The layers as Chromium's main frame has them (<see cref="ChromiumLayers"/>): the first opaque, LCD text allowed
    /// where it holds text; and each (B) case's text chunks with the case's verdict, in whichever layer.
    /// </summary>
    [Theory]
    [MemberData(nameof(Cases))]
    public void TheLayersAreChromiumsWithEachCasesVerdict(string name, float scale)
    {
        var fixture = All.Single(f => f.Name == name);
        using var painter = new UiPainter(Build(fixture, scale));
        var layers = painter.AnalyzeLayers().Where(l => l.DrawsContent && l.Type != LayerType.ScrollHitTest).ToList();
        Assert.True(layers.Count == ChromiumLayers(name, scale), $"{layers.Count} layers, Chromium's {ChromiumLayers(name, scale)}: {string.Join(" | ", layers)}");
        if (layers.Count == 1)
            Assert.True(layers[0].ContentsOpaque && layers[0].LcdAllowed, $"the page's layer: {layers[0]}");
        else
        {
            // lcd-blend at 1: the view's base backdrop (no text: Chromium's verdict none), the root's isolation group not
            // opaque, the opacity box a layer of its own over it.
            Assert.True(!layers[0].HasText && layers[0].RectKnownToBeOpaque == layers[0].Bounds, $"the base backdrop: {layers[0]}");
            Assert.True(!layers[1].ContentsOpaque && !layers[1].LcdAllowed, $"the root's group: {layers[1]}");
            Assert.True(layers[2].Type == LayerType.Overlap && layers[2].LcdAllowed, $"the opacity box: {layers[2]}");
            Assert.All(layers[2].Chunks.Where(c => c.HasText), c => Assert.Equal("opacity", CaseOf(c)));
        }
        if (name == "lcd-size")
            return;
        foreach (var (@case, _, at1, at165) in fixture.Cases)
        {
            var expected = scale == 1 ? at1 : at165;
            var chunks = layers.SelectMany(l => l.Chunks).Where(c => c.HasText && CaseOf(c) == @case).ToList();
            Assert.True(chunks.Count > 0, $"{@case}: no text chunk of it");
            Assert.All(chunks, c => Assert.True(c.LcdText == expected, $"{@case}: {c.Owner} LCD {c.LcdText}, Chromium's {expected}"));
        }
    }

    /// <summary>
    /// ViewPainter's Root Group and Root Element Group (<c>view_painter.cc</c> 170–218 and 385–448) when the root element is
    /// an isolation group of its own: the base background alone in the view's contents state (the root effect), then the
    /// root's background in the root element's state without its own transform (<c>PreTransform</c>) and without the base
    /// (its separate effect paints the root's colour alone). So a translucent root background stays translucent in the
    /// group, and a transformed root's background still covers the canvas.
    /// </summary>
    [Fact]
    public void TheRootElementGroupPaintsTheBaseApartAndTheRootsBackgroundBeforeTheRootsTransform()
    {
        var fixture = new LcdFixture("root-group", 480, 220,
            "html { background: rgba(243, 232, 204, 0.5); transform: scale(0.5); transform-origin: 0 0; } body { margin: 0; } "
            + ".b { position: absolute; left: 20px; top: 20px; width: 200px; height: 40px; mix-blend-mode: multiply; background: #e9dcbc; }",
            "<div class=\"b\"></div>", []);
        using var painter = new UiPainter(Build(fixture, 1));
        var chunks = painter.AnalyzeLayers().SelectMany(l => l.Chunks).Where(c => c.DrawsContent).ToList();
        var canvas = new RectF(0, 0, 480, 220);
        // The Root Group: the base, opaque, over the canvas, in the root effect.
        var base0 = chunks[0];
        Assert.True(base0.State.Effect.Owner is null && base0.RectKnownToBeOpaque == canvas, $"the base: {base0} opaque {base0.RectKnownToBeOpaque}");
        // The Root Element Group: the root's own effect, the canvas whole (not scaled with the root), nothing known opaque.
        var group = chunks[1];
        Assert.Equal("html", group.State.Effect.Owner?.Element?.Tag);
        Assert.False(group.State.Transform.Owner?.Element?.Tag == "html" && group.State.Transform.Kind == "transform",
            $"the group's transform: the root's own ({group.State.Transform.Kind}), not the one before it");
        Assert.Equal(canvas, group.Bounds);
        Assert.True(Gfx.IsEmpty(group.RectKnownToBeOpaque), $"the root's background: opaque {group.RectKnownToBeOpaque}");
    }

    /// <summary>Which case a text chunk is: by the box whose effect it is in (none, or the root's isolation group: the page's own text).</summary>
    static string CaseOf(PaintChunk chunk)
    {
        if (chunk.State.Effect.Owner?.Element is not { Tag: not "html" } owner)
            return "page";
        var classes = owner.ClassName.Split(' ');
        return classes.Contains("f") ? "filter" : classes.Contains("o") ? "opacity" : classes.Contains("b") ? "blend" : classes.Contains("i") ? "group" : owner.ClassName;
    }

    // ---------- (A) the rule itself ----------

    static Matrix3x2 Matrix(float scaleX, float skewY, float skewX, float scaleY) => new(scaleX, skewY, skewX, scaleY, 7, 9);

    /// <summary>
    /// Skia's <c>too_big_for_lcd</c>: with a matrix that only translates, the size past 48 (48 itself keeps LCD); with one
    /// that scales or skews, the size squared times the 2×2 part's signed area (each entry to 1/1024) past 48².
    /// </summary>
    [Fact]
    public void TextBiggerThan48DevicePixelsIsTooBigForLcdAsSkiaHasIt()
    {
        var translate = Matrix3x2.CreateTranslation(3.5f, 2);
        Assert.False(CanvasRenderer.TooBigForLcd(47.99f, translate));
        Assert.False(CanvasRenderer.TooBigForLcd(48, translate));
        Assert.True(CanvasRenderer.TooBigForLcd(48.01f, translate));
        // A scale: the area, 4 at twice the size (24 px is 48 device px, kept; a hair more is too big).
        Assert.False(CanvasRenderer.TooBigForLcd(24, Matrix(2, 0, 0, 2)));
        Assert.True(CanvasRenderer.TooBigForLcd(24.01f, Matrix(2, 0, 0, 2)));
        Assert.True(CanvasRenderer.TooBigForLcd(34, Matrix(1, 0, 0, 2)));
        Assert.False(CanvasRenderer.TooBigForLcd(33.9f, Matrix(1, 0, 0, 2)));
        Assert.False(CanvasRenderer.TooBigForLcd(48, Matrix(1, 0, 0, 0.99f)));
        // Each entry relaxed to 1/1024 (sk_relax): 1.0004 is 1, so 48 px stays LCD; 1.001 is 1025/1024, and it doesn't.
        Assert.False(CanvasRenderer.TooBigForLcd(48, Matrix(1, 0, 0, 1.0004f)));
        Assert.True(CanvasRenderer.TooBigForLcd(48, Matrix(1, 0, 0, 1.001f)));
        // A skew alone keeps the area 1: the size decides, through the area.
        Assert.False(CanvasRenderer.TooBigForLcd(48, Matrix(1, 0, 0.5f, 1)));
        Assert.True(CanvasRenderer.TooBigForLcd(48.01f, Matrix(1, 0, 0.5f, 1)));
        Assert.True(CanvasRenderer.TooBigForLcd(44, Matrix(1, -0.5f, 0.5f, 1)));
        Assert.False(CanvasRenderer.TooBigForLcd(42, Matrix(1, -0.5f, 0.5f, 1)));
        // The signed area: a mirrored matrix's is negative, never past 48² (Skia's own comparison).
        Assert.False(CanvasRenderer.TooBigForLcd(100, Matrix(-1, 0, 0, 1)));
    }
}

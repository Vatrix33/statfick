using System.Numerics;
using Statventure.Canvas;
using Statventure.Core;
using Statventure.Graphics;
using Statventure.Imaging;
using Statventure.Tests.Infrastructure;
using Statventure.Tests.Text;
using Statventure.Tests.Ui;
using Statventure.Text;
using Statventure.UI.Dom;
using CanvasRenderer = Statventure.Canvas.Canvas;
using Path = Statventure.Canvas.Path;

namespace Statventure.Tests.Canvas;

/// <summary>
/// How the canvas draws an icon (GM1 step 7; Docs/Game/Foundation.md, "How it works", step 7): its outline filled by the
/// path fill (nonzero) in the text's colour, at the text's size, where its glyph would land (the baseline on a pixel,
/// the pen on a quarter pixel; exactly under a rotation), grayscale whatever the text's LCD verdict; and in a document
/// as a glyph is: its fill colour, the selection's colour, a text mask, shadows, opacity. Checked here against the
/// engine's own path fill and plain text; the game's icons against Chromium in <c>Ui/IconReferenceTests</c>.
/// </summary>
[Collection("GPU")]
public sealed class IconDrawingTests(GpuFixture gpu) : IDisposable
{
    readonly FontLibrary _fonts = NewFonts();

    public void Dispose() => _fonts.Dispose();

    static FontLibrary NewFonts()
    {
        var fonts = new FontLibrary();
        var dir = System.IO.Path.Combine(AppContext.BaseDirectory, "Data", "Fonts");
        fonts.AddFile(System.IO.Path.Combine(dir, "Lato-Regular.ttf"), "Lato", 400);
        fonts.AddFile(System.IO.Path.Combine(dir, "Lato-Bold.ttf"), "Lato", 700, weightMax: 900);
        fonts.AddIcons(IconFaceTests.All);
        return fonts;
    }

    static readonly Color Ink = new(0.2f, 0.5f, 0.8f);

    RgbaImage Render(int width, int height, Action<CanvasRenderer> draw, Action<CanvasRenderer>? setup = null)
    {
        using var canvas = new CanvasRenderer(gpu.Device);
        setup?.Invoke(canvas);
        using var target = GpuImage.CreateRenderTarget(gpu.Device, width, height);
        var frame = gpu.Device.BeginFrame();
        canvas.Begin(width, height);
        canvas.FillRect(new RectF(0, 0, width, height), Color.White);
        draw(canvas);
        canvas.Render(frame, target);
        gpu.Device.EndFrame(frame);
        gpu.Device.WaitIdle();
        return new RgbaImage(width, height, target.ReadPixels());
    }

    /// <summary>The largest difference of a channel between two pictures, and how many pixels differ at all.</summary>
    static (int Worst, int Count) Difference(RgbaImage a, RgbaImage b)
    {
        int worst = 0, count = 0;
        for (var i = 0; i < a.Pixels.Length; i += 4)
        {
            var pixel = 0;
            for (var c = 0; c < 4; c++)
                pixel = Math.Max(pixel, Math.Abs(a.Pixels[i + c] - b.Pixels[i + c]));
            worst = Math.Max(worst, pixel);
            count += pixel > 0 ? 1 : 0;
        }
        return (worst, count);
    }

    static void AssertSame(RgbaImage expected, RgbaImage actual, int tolerance, string what)
    {
        var (worst, count) = Difference(expected, actual);
        if (worst > tolerance && Environment.GetEnvironmentVariable("STATVENTURE_ICON_DEBUG") is { Length: > 0 } folder)
        {
            var name = string.Concat(what.Where(char.IsLetterOrDigit));
            ImageFile.SavePng(System.IO.Path.Combine(folder, name + "-expected.png"), expected);
            ImageFile.SavePng(System.IO.Path.Combine(folder, name + "-actual.png"), actual);
        }
        Assert.True(worst <= tolerance, $"{what}: {count} pixels differ, by up to {worst} levels");
    }

    static int Inked(RgbaImage image) => Enumerable.Range(0, image.Width * image.Height).Count(i => image.Pixels[i * 4] < 250 || image.Pixels[i * 4 + 2] < 250);

    GlyphRun IconRun(IconGlyph icon, float size)
    {
        var layout = TextLayout.Create(_fonts, char.ConvertFromUtf32(icon.CodePoint), new TextStyle { Families = ["Lato"], Size = size });
        return layout.Lines[0].Runs.Single();
    }

    /// <summary>The outline in its units (1/1000 em) filled by the canvas through <paramref name="transform"/> (em units to the run's place).</summary>
    static void FillOutline(CanvasRenderer canvas, IconGlyph icon, float size, Matrix3x2 place, Color color)
    {
        canvas.PushTransform(Matrix3x2.CreateScale(size / IconGlyph.UnitsPerEm) * place);
        canvas.FillPath(Path.ParseSvg(icon.Outline), Brush.Solid(color), FillRule.NonZero);
        canvas.PopTransform();
    }

    /// <summary>
    /// The outline filled in the colour at the size, its pen where Skia puts a glyph's: the baseline rounded to a pixel,
    /// x to the quarter pixel at or after it (an eighth's tie goes up: 10.125 to 10.25, where the path cache's own
    /// quarter pixels round to even, 10).
    /// </summary>
    [Theory]
    [InlineData(10f, 30f, 40f, 10f, 30f)]
    [InlineData(10.125f, 30f, 40f, 10.25f, 30f)]
    [InlineData(10.3f, 30.4f, 40f, 10.25f, 30f)]
    [InlineData(10.6f, 30.6f, 20f, 10.5f, 31f)]
    [InlineData(11.9f, 29.5f, 33f, 12f, 30f)]
    public void AnIconIsItsOutlineFilledWhereItsGlyphGoes(float x, float y, float size, float penX, float baseline)
    {
        foreach (var icon in new[] { IconFaceTests.Square, IconFaceTests.Ring, IconFaceTests.Overlap, IconFaceTests.WhiteDiamond })
        {
            var run = IconRun(icon, size);
            var drawn = Render(80, 60, canvas => canvas.DrawGlyphs(run, new Vector2(x, y), Ink));
            var expected = Render(80, 60, canvas => FillOutline(canvas, icon, size, Matrix3x2.CreateTranslation(penX, baseline), Ink));
            Assert.True(Inked(drawn) > 20, $"U+{icon.CodePoint:X4} drew nothing");
            AssertSame(expected, drawn, 2, $"U+{icon.CodePoint:X4} at ({x}, {y}), {size} px");
        }
    }

    /// <summary>On a canvas of 2 pixels a unit, a 10 px icon at (10.3, 20.1) is the 20 px icon at (20.6, 40.2) of a canvas of 1, snapped in pixels.</summary>
    [Fact]
    public void AnIconScalesWithTheCanvas()
    {
        var drawn = Render(80, 60, canvas => canvas.DrawGlyphs(IconRun(IconFaceTests.Ring, 20), new Vector2(20.6f, 40.2f), Ink));
        var scaled = RenderAtScale(80, 60, 2, canvas => canvas.DrawGlyphs(IconRun(IconFaceTests.Ring, 10), new Vector2(10.3f, 20.1f), Ink));
        Assert.True(Inked(scaled) > 50);
        AssertSame(drawn, scaled, 0, "at 2 pixels a unit");
    }

    RgbaImage RenderAtScale(int width, int height, float scale, Action<CanvasRenderer> draw)
    {
        using var canvas = new CanvasRenderer(gpu.Device);
        using var target = GpuImage.CreateRenderTarget(gpu.Device, width, height);
        var frame = gpu.Device.BeginFrame();
        canvas.Begin(width, height, scale);
        canvas.FillRect(new RectF(0, 0, width / scale, height / scale), Color.White);
        draw(canvas);
        canvas.Render(frame, target);
        gpu.Device.EndFrame(frame);
        gpu.Device.WaitIdle();
        return new RgbaImage(width, height, target.ReadPixels());
    }

    /// <summary>Nonzero: two squares wound the same way overlap filled, not as a hole.</summary>
    [Fact]
    public void AnOutlineIsFilledNonzero()
    {
        var image = Render(60, 60, canvas => canvas.DrawGlyphs(IconRun(IconFaceTests.Overlap, 50), new Vector2(0, 50), Ink));
        // The overlap of the squares: x 0.3–0.6 em from the pen, y 0.6–0.3 em above the baseline; at 50 px (22, 27) is inside.
        var (r, g, b, _) = image[22, 27];
        Assert.True(Math.Abs(r - 51) <= 1 && Math.Abs(g - 128) <= 1 && Math.Abs(b - 204) <= 1, $"the overlap is ({r}, {g}, {b})");
    }

    /// <summary>
    /// Under a rotation the outline goes exactly where the glyph's pen is (nothing to round to). (The reference fills the
    /// outline in em, scaled by the size: the canvas's path cache keeps a transform's linear part to 1/1000, which for
    /// the outline's own 1/1000 em units under a rotation would be the size to 1 %.)
    /// </summary>
    [Fact]
    public void AnIconFollowsARotationExactly()
    {
        var rotation = Matrix3x2.CreateRotation(-11 * MathF.PI / 180, new Vector2(40, 40));
        var run = IconRun(IconFaceTests.Ring, 36);
        var drawn = Render(80, 80, canvas =>
        {
            canvas.PushTransform(rotation);
            canvas.DrawGlyphs(run, new Vector2(20.3f, 50.6f), Ink);
            canvas.PopTransform();
        });
        var em = new Path();
        var units = Path.ParseSvg(IconFaceTests.Ring.Outline);
        var at = 0;
        foreach (var verb in units.Verbs)
            _ = verb switch
            {
                Path.Verb.Move => em.MoveTo(units.Points[at++] / IconGlyph.UnitsPerEm),
                Path.Verb.Line => em.LineTo(units.Points[at++] / IconGlyph.UnitsPerEm),
                _ => em.Close(),
            };
        var expected = Render(80, 80, canvas =>
        {
            canvas.PushTransform(Matrix3x2.CreateScale(36) * Matrix3x2.CreateTranslation(20.3f, 50.6f) * rotation);
            canvas.FillPath(em, Brush.Solid(Ink));
            canvas.PopTransform();
        });
        Assert.True(Inked(drawn) > 50);
        AssertSame(expected, drawn, 2, "rotated");
    }

    /// <summary>Grayscale whatever the text's verdict (as Chromium fills an inline SVG path): the same with LCD text allowed as without.</summary>
    [Fact]
    public void AnIconIsGrayscaleWhateverTheTextsVerdict()
    {
        var lcd = FontRenderSettings.Grayscale with { Antialias = TextAntialias.SubpixelRgb };
        RgbaImage Draw(bool allowed, string text) => Render(120, 60, canvas =>
        {
            canvas.TextLcd = allowed;
            canvas.DrawText(TextLayout.Create(_fonts, text, new TextStyle { Families = ["Lato"], Size = 36 }), new Vector2(4.3f, 2), Color.Black);
        }, canvas => canvas.FontSettings = lcd);
        var icon = Draw(true, "⚙◇");
        AssertSame(Draw(false, "⚙◇"), icon, 0, "the icons with LCD text allowed");
        for (var i = 0; i < icon.Pixels.Length; i += 4)
            Assert.True(icon.Pixels[i] == icon.Pixels[i + 1] && icon.Pixels[i + 1] == icon.Pixels[i + 2], "a coloured fringe on a black icon");
        // The same canvas draws Lato's letters with LCD antialiasing then.
        if (gpu.Device.SupportsDualSourceBlend)
        {
            var letters = Draw(true, "xo");
            Assert.Contains(Enumerable.Range(0, letters.Width * letters.Height), i => letters.Pixels[i * 4] != letters.Pixels[i * 4 + 2]);
        }
    }

    [Fact]
    public void ASpaceAfterAnIconDrawsNothingAndATransparentIconNothing()
    {
        var run = TextLayout.Create(_fonts, "⚙ ", new TextStyle { Families = ["Lato"], Size = 30 }, new ParagraphStyle { WhiteSpace = WhiteSpace.Pre }).Lines[0].Runs.Single();
        Assert.Equal(2, run.Glyphs.Count);
        var both = Render(80, 40, canvas => canvas.DrawGlyphs(run, new Vector2(5, 30), Ink));
        var icon = Render(80, 40, canvas => canvas.DrawGlyphs(IconRun(IconFaceTests.Square, 30), new Vector2(5, 30), Ink));
        AssertSame(icon, both, 0, "the space");
        Assert.Equal(0, Inked(Render(80, 40, canvas => canvas.DrawGlyphs(run, new Vector2(5, 30), Ink with { A = 0 }))));
    }

    // ---------- In a document, as a glyph is ----------

    UiDocument Document(string css, string html, float scale)
    {
        var document = new UiDocument(_fonts);
        document.SetViewport(160, 60);
        document.SetZoom(scale);
        document.AddStylesheet("html, body { margin: 0; background: #fff; } div { font: 40px/50px Lato; padding: 4px 10px; }" + css, "icons");
        Markup.SetInnerHtml(document.Body, html);
        document.Update(0);
        return document;
    }

    RgbaImage Paint(UiDocument document, float scale) =>
        PaintReferenceTests.Render(gpu.Device, document, (int)MathF.Round(160 * scale), (int)MathF.Round(60 * scale));

    void AssertPaintsAs(string css, string html, string expectedCss, string expectedHtml, int tolerance, Action<UiDocument>? prepare = null)
    {
        foreach (var scale in new[] { 1f, 1.65f })
        {
            var document = Document(css, html, scale);
            prepare?.Invoke(document);
            var drawn = Paint(document, scale);
            var expected = Paint(Document(expectedCss, expectedHtml, scale), scale);
            Assert.True(Inked(drawn) > 100, $"nothing drawn at {scale}");
            AssertSame(expected, drawn, tolerance, $"at {scale}");
        }
    }

    [Fact]
    public void AnIconTakesTheTextsFillColour() =>
        AssertPaintsAs(".t { color: #c00; -webkit-text-fill-color: #06c; }", "<div class=t>⚙◆◌</div>", ".t { color: #06c; }", "<div class=t>⚙◆◌</div>", 0);

    [Fact]
    public void ASelectedIconTakesTheSelectionsColour() =>
        AssertPaintsAs(".t { color: #c00; } .t::selection { color: #06c; background: transparent; }", "<div class=t>⚙◆◌</div>",
            ".t { color: #06c; }", "<div class=t>⚙◆◌</div>", 0, document =>
            {
                var text = document.Body.Children.OfType<UiElement>().Single().Children.Single();
                document.SetPageSelection(new DomPosition(text, 0), new DomPosition(text, 3));
                document.Update(0);
            });

    /// <summary>In a box clipped to its text (<c>background-clip: text</c>), an icon is part of the mask its background shows through.</summary>
    [Fact]
    public void AnIconIsInItsBoxsTextMask() =>
        AssertPaintsAs(".t { color: #c00; background: linear-gradient(#06c, #06c); background-clip: text; -webkit-text-fill-color: transparent; }", "<div class=t>⚙◆◌</div>",
            ".t { color: #06c; }", "<div class=t>⚙◆◌</div>", 2);

    [Fact]
    public void AnIconCastsItsTextsShadow() =>
        AssertPaintsAs(".t { color: transparent; text-shadow: 20px 0 0 #06c; }", "<div class=t>⚙◆◌</div>",
            ".t { color: #06c; margin-left: 20px; }", "<div class=t>⚙◆◌</div>", 0);

    [Fact]
    public void AnIconTakesItsElementsOpacity() =>
        AssertPaintsAs(".t { color: #06c; opacity: 0.5; }", "<div class=t>⚙◆◌</div>",
            ".t { color: rgba(0, 102, 204, 0.5); }", "<div class=t>⚙◆◌</div>", 2);
}

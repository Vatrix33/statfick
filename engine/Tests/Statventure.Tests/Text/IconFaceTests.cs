using HarfBuzzSharp;
using Statventure.Core;
using Statventure.Text;

namespace Statventure.Tests.Text;

/// <summary>
/// The icon face (GM1 step 7; Docs/Game/Foundation.md, "How it works", step 7): icons added to a font library are taken
/// for their characters before any font, shaped as single glyphs, measured by their own boxes, and a space after one
/// keeps the text's font. Icons of made-up shapes here; the game's own are checked against Chromium in
/// <c>Ui/IconReferenceTests</c> and <c>Ui/IconLayoutTests</c>.
/// </summary>
public sealed class IconFaceTests : IDisposable
{
    static string FontPath(string file) => Path.Combine(AppContext.BaseDirectory, "Data", "Fonts", file);

    // A square in an Adwaita Mono box, a ring (◌, which Lato has) in Liberation Serif's, two diamonds sharing Adwaita
    // Sans's, an arrow in Noto Sans Math's (taller and deeper than Lato's), and two overlapping squares wound the same way.
    internal static readonly IconGlyph Square = new(0x2699, "M100 -700 500 -700 500 -100 100 -100Z", 0.6, 0.965, 0.285, 0);
    internal static readonly IconGlyph Ring = new(0x25CC, "M100 -600 500 -600 500 -200 100 -200ZM200 -500 200 -300 400 -300 400 -500Z", 0.5942382812, 0.8911132812, 0.2163085938, 0.04248046875);
    internal static readonly IconGlyph Diamond = new(0x25C6, "M525 -800 950 -350 525 100 100 -350Z", 1.05078125, 0.96875, 0.2412109375, 0);
    internal static readonly IconGlyph WhiteDiamond = new(0x25C7, "M525 -800 950 -350 525 100 100 -350ZM525 -650 250 -350 525 -50 800 -350Z", 1.05078125, 0.96875, 0.2412109375, 0);
    internal static readonly IconGlyph Arrow = new(0x21B6, "M100 -900 1100 -900 1100 300 100 300Z", 1.257, 1.069, 0.423, 0);
    internal static readonly IconGlyph Overlap = new(0x2726, "M100 -800 600 -800 600 -300 100 -300ZM300 -600 800 -600 800 -100 300 -100Z", 0.9, 0.965, 0.285, 0);

    internal static IconGlyph[] All => [Square, Ring, Diamond, WhiteDiamond, Arrow, Overlap];

    readonly FontLibrary _fonts = new();
    readonly ResolvedFont _lato;

    public IconFaceTests()
    {
        _fonts.AddFile(FontPath("Lato-Regular.ttf"), "Lato", 400);
        _lato = _fonts.Resolve("Lato", 400, false)!.Value;
    }

    public void Dispose() => _fonts.Dispose();

    static TextStyle Style(float size, int weight = 400) => new() { Families = ["Lato"], Size = size, Weight = weight };

    TextLayout Layout(string text, float size, int weight = 400) =>
        TextLayout.Create(_fonts, text, Style(size, weight), new ParagraphStyle { WhiteSpace = WhiteSpace.Pre });

    [Fact]
    public void AnIconIsTakenBeforeAnyFontTheTextsOwnToo()
    {
        Assert.True(_lato.Face.Has(Ring.CodePoint), "Lato has ◌");
        // Before the icons: Lato's ring; a square Lato lacks has no face here.
        Assert.Equal(_lato.Face, _fonts.ResolveFor(Ring.CodePoint, _lato)?.Face);
        _fonts.AddIcons(All);
        foreach (var icon in All)
        {
            var resolved = _fonts.ResolveFor(icon.CodePoint, _lato);
            Assert.True(resolved is { Face.IsIconFace: true }, $"U+{icon.CodePoint:X4}");
            Assert.Equal(icon, resolved!.Value.Face.IconOf(resolved.Value.Face.GlyphFor(icon.CodePoint)));
        }
        Assert.Equal(_lato.Face, _fonts.ResolveFor('A', _lato)?.Face);
        Assert.Equal([0x21B6, 0x25C6, 0x25C7, 0x25CC, 0x2699, 0x2726], _fonts.IconCodePoints.Order());
    }

    [Fact]
    public void IconsOfOneBoxShareAFaceAndRunAsOne()
    {
        // Two more of the diamonds' ascent: one with another descent, one with another line gap.
        _fonts.AddIcons([.. All, Square with { CodePoint = 0x2605, Ascent = Diamond.Ascent, Descent = 0.3 }, Square with { CodePoint = 0x2606, Ascent = Diamond.Ascent, Descent = Diamond.Descent, LineGap = 0.1 }]);
        var diamond = _fonts.ResolveFor(Diamond.CodePoint, _lato)!.Value.Face;
        Assert.Same(diamond, _fonts.ResolveFor(WhiteDiamond.CodePoint, _lato)!.Value.Face);
        Assert.NotSame(diamond, _fonts.ResolveFor(Square.CodePoint, _lato)!.Value.Face);
        Assert.NotSame(diamond, _fonts.ResolveFor(0x2605, _lato)!.Value.Face);
        Assert.NotSame(diamond, _fonts.ResolveFor(0x2606, _lato)!.Value.Face);
        Assert.Single(Layout("◆◇", 20).Lines[0].Runs);
        Assert.Equal(2, Layout("◆⚙", 20).Lines[0].Runs.Count);
    }

    [Fact]
    public void AnIconIsOneGlyphOfItsOwnInTheText()
    {
        _fonts.AddIcons(All);
        var runs = Layout("a⚙b", 20).Lines[0].Runs;
        Assert.Equal(3, runs.Count);
        Assert.False(runs[0].Font.Face.IsIconFace);
        var icon = Assert.Single(runs[1].Glyphs);
        Assert.Equal(Square, runs[1].Font.Face.IconOf(icon.Glyph));
        Assert.Equal((0f, 0f), (icon.X, icon.Y));
        Assert.Same(_lato.Face, runs[2].Font.Face);
    }

    /// <summary>
    /// An icon's advance is its box's times the text's size as a font's glyphs are scaled: the size to 0.01 px, truncated
    /// (12 px at 1.65 is 19.79), then in 26.6, truncated (26.4 px advances as 26.390625 px, 19.79 px as 19.78125 px); the
    /// result to 1/65536 px (0.6 em at 9 px is 353894/65536 px, not 5.4). No weight changes it.
    /// </summary>
    [Theory]
    [InlineData(0.75, 16f, 12f)]
    [InlineData(0.75, 26.4f, 19.79296875f)]
    [InlineData(0.75, 19.8f, 14.8359375f)]
    [InlineData(0.9541015625, 16f, 15.265625f)]
    [InlineData(1.030761719, 16f, 16.4921875f)]
    [InlineData(1.257, 16f, 20.11199951171875f)]
    [InlineData(0.6, 9f, 5.399993896484375f)]
    public void TheAdvanceIsTheBoxsAtTheTextsSize(double advance, float size, float pixels)
    {
        _fonts.AddIcons([Square with { Advance = advance }]);
        Assert.Equal(pixels, Layout("⚙", size).Width);
        Assert.Equal(pixels, Layout("⚙", size, weight: 700).Width);
        Assert.Equal(pixels * 3, Layout("⚙⚙⚙", size).Width, 4);
    }

    /// <summary>The box's ascent, descent and line gap at a size, rounded as a font's are (Blink's, halves up; a descent rounded down takes a pixel from the ascent with text at subpixel positions on Linux).</summary>
    [Fact]
    public void TheBoxGivesTheMetrics()
    {
        _fonts.AddIcons(All);
        FontFace Face(IconGlyph icon) => _fonts.ResolveFor(icon.CodePoint, _lato)!.Value.Face;
        Assert.Equal((17f, 7f, 0f), TextLayout.Metrics(Face(Arrow), 16));
        Assert.Equal((18f, 4f, 1f), TextLayout.Metrics(Face(Ring), 20));
        Assert.Equal((17f, 5f, 1f), TextLayout.Metrics(Face(Ring), 20, subpixelPositioning: true));
        // The exact box, not its rounding to the outlines' units (500.5 px rounds to 501; 0.5005 em is 500.5 units).
        _fonts.AddIcons([new IconGlyph(0x2605, Square.Outline, 1, 0.5005, 0.2505, 0.0015)]);
        Assert.Equal((501f, 251f, 2f), TextLayout.Metrics(_fonts.ResolveFor(0x2605, _lato)!.Value.Face, 1000));
    }

    /// <summary>The canvas's text layout: a line's box takes each run's (the icon's whole box, with its line gap).</summary>
    [Fact]
    public void ALineOfTheCanvasLayoutTakesTheIconsBox()
    {
        _fonts.AddIcons(All);
        var (ascent, descent, gap) = TextLayout.Metrics(_lato.Face, 16);
        // Lato's box with its line gap shared out (the top half floored), the arrow's 17 above and 7 below.
        var above = MathF.Max(ascent + MathF.Floor(gap / 2), 17);
        var below = MathF.Max(descent + gap - MathF.Floor(gap / 2), 7);
        var line = Layout("x↶", 16).Lines[0];
        Assert.Equal(above, line.Baseline);
        Assert.Equal(above + below, line.Height);
        Assert.True(above + below > ascent + descent + gap, "the arrow's box makes the line taller than Lato's");
    }

    /// <summary>
    /// A space after an icon is the text's font's (Blink shapes a space with the first font that has one, the primary;
    /// the layout keeps it in the run before it, here the icon's): its advance is the font's.
    /// </summary>
    [Fact]
    public void ASpaceAfterAnIconIsTheTextsFonts()
    {
        _fonts.AddIcons(All);
        var expected = Layout("⚙", 20).Width + Layout(" x", 20).Width;
        Assert.Equal(expected, Layout("⚙ x", 20).Width, 4);
        var run = Layout("⚙ ", 20).Lines[0].Runs.Single(r => r.Font.Face.IsIconFace);
        Assert.Equal(2, run.Glyphs.Count);
        Assert.Null(run.Font.Face.IconOf(run.Glyphs[1].Glyph));
    }

    /// <summary>An icon is drawn as its outline whatever the weight and style: no synthetic bold or oblique.</summary>
    [Fact]
    public void IconsTakeNoSyntheticStyle()
    {
        _fonts.AddIcons(All);
        var bold = _fonts.Resolve("Lato", 700, true)!.Value;
        Assert.True(bold.SyntheticBold && bold.SyntheticOblique);
        Assert.Equal((false, false), (_fonts.ResolveFor(Square.CodePoint, bold)!.Value.SyntheticBold, _fonts.ResolveFor(Square.CodePoint, bold)!.Value.SyntheticOblique));
    }

    /// <summary>HarfBuzz's view of an icon: its glyph for the character and its ink's extents, in the outline's units (y up).</summary>
    [Fact]
    public void AnIconsGlyphHasItsOutlinesExtents()
    {
        _fonts.AddIcons(All);
        var face = _fonts.ResolveFor(Square.CodePoint, _lato)!.Value.Face;
        Assert.Equal(1000, face.UnitsPerEm);
        var glyph = face.GlyphFor(Square.CodePoint);
        Assert.NotEqual(0u, glyph);
        Assert.True(face.HbFont.TryGetGlyphExtents(glyph, out GlyphExtents extents));
        Assert.Equal((100, 700, 400, -600), (extents.XBearing, extents.YBearing, extents.Width, extents.Height));
        Assert.Equal((965, 285), (face.Ascent, face.Descent));
    }

    /// <summary>Text decorations skip an icon's ink as a glyph's: its outline's span between two heights.</summary>
    [Fact]
    public void DecorationsSkipAnIconsInk()
    {
        _fonts.AddIcons(All);
        var face = _fonts.ResolveFor(Square.CodePoint, _lato)!.Value.Face;
        var glyph = face.GlyphFor(Square.CodePoint);
        var (begin, end) = GlyphOutlines.Intercept(face, glyph, 10, false, false, -2, 0)!.Value;
        Assert.Equal(1f, begin, 4);
        Assert.Equal(5f, end, 4);
        Assert.Null(GlyphOutlines.Intercept(face, glyph, 10, false, false, 0, 1));
    }

    [Fact]
    public void ACodePointIsAnIconOnceAndAnOutlineMustBePathData()
    {
        _fonts.AddIcons([Square]);
        Assert.Throws<ArgumentException>(() => _fonts.AddIcons([Ring, Square with { Advance = 1 }]));
        Assert.Throws<ArgumentException>(() => _fonts.AddIcons([Ring, Ring]));
        Assert.ThrowsAny<FormatException>(() => _fonts.AddIcons([Ring with { Outline = "100 100 200 200" }]));
        // Nothing of a refused call was added.
        Assert.Equal([Square.CodePoint], _fonts.IconCodePoints);
    }

    [Fact]
    public void AnIconFaceHasNoFontOutlinesToRender()
    {
        _fonts.AddIcons(All);
        var face = _fonts.ResolveFor(Square.CodePoint, _lato)!.Value.Face;
        Assert.Throws<InvalidOperationException>(() => GlyphRasterizer.Render(face, 1, 20, 0, false, false));
        Assert.Throws<InvalidOperationException>(() => Statventure.Text.Skia.SkiaGlyphs.Render(face, 1, 20, 0, false, false, FontRenderSettings.Grayscale, false, 0));
    }
}

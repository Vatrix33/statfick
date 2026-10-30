using Statventure.Text;

namespace Statventure.Tests.Canvas;

public class TextTests
{
    static FontLibrary Fonts => TestFonts.Library;
    static TextStyle Lato(float size) => new() { Families = ["Lato"], Size = size };

    [Fact]
    public void FontsLoadWithTheirMetrics()
    {
        var lato = Fonts.Resolve("Lato", 400, false)!.Value.Face;
        Assert.Equal(2000, lato.UnitsPerEm);
        Assert.True(lato.Ascent > 0 && lato.Descent > 0);
        Assert.NotEqual(0u, lato.GlyphFor('A'));
    }

    [Fact]
    public void FontMatchingFollowsCss()
    {
        Assert.Equal(400, Fonts.Resolve("Lato", 400, false)!.Value.Face.Weight);
        Assert.Equal(500, Fonts.Resolve("Lato", 600, false)!.Value.Face.Weight);
        Assert.Equal(700, Fonts.Resolve("Lato", 800, false)!.Value.Face.Weight);
        var stixBold = Fonts.Resolve("STIX Two Text", 700, false)!.Value;
        Assert.True(stixBold.SyntheticBold);
        var latoItalic = Fonts.Resolve("Lato", 400, true)!.Value;
        Assert.True(latoItalic.SyntheticOblique);
        Assert.False(Fonts.Resolve("STIX Two Text", 400, true)!.Value.SyntheticOblique);
    }

    [Fact]
    public void ShapingAppliesKerning()
    {
        var kerned = TextLayout.Create(Fonts, "AV", Lato(40));
        var a = TextLayout.Create(Fonts, "A", Lato(40));
        var v = TextLayout.Create(Fonts, "V", Lato(40));
        Assert.True(kerned.Width < a.Width + v.Width - 1, $"AV {kerned.Width} vs {a.Width + v.Width}");
    }

    [Fact]
    public void GlyphsRasterize()
    {
        var face = Fonts.Resolve("Lato", 400, false)!.Value.Face;
        var bitmap = GlyphRasterizer.Render(face, face.GlyphFor('H'), 16, 0, false, false);
        Assert.True(bitmap.Width > 4 && bitmap.Height > 8);
        Assert.Contains(bitmap.Coverage, b => b == 255);
    }

    [Fact]
    public void ParagraphsWrapAtSpaces()
    {
        var layout = TextLayout.Create(Fonts, "one two three four five six", Lato(16), new ParagraphStyle { MaxWidth = 80 });
        Assert.True(layout.Lines.Count >= 3);
        Assert.All(layout.Lines, line => Assert.True(line.Width <= 80.01f));
        Assert.Equal(layout.Lines.Count * layout.Lines[0].Height, layout.Height, 2);
    }
}

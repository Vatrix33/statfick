using Statventure.Game.Icons;
using Statventure.Text;

namespace Statventure.Game.Tests.Icons;

/// <summary>
/// <see cref="GameIcons.AddTo"/> (GM1 step 7): a font library given the game's icons draws each of the 28 symbols with
/// the engine's icon face before any of its fonts, ◌ too, which Lato has, by the table's outline and box.
/// </summary>
public sealed class GameIconsTests : IDisposable
{
    readonly FontLibrary _fonts = new();
    readonly ResolvedFont _lato;

    public GameIconsTests()
    {
        _fonts.AddFile(Path.Combine(AppContext.BaseDirectory, "Data", "Fonts", "Lato-Regular.ttf"), "Lato");
        _lato = _fonts.Resolve("Lato", 400, false)!.Value;
    }

    public void Dispose() => _fonts.Dispose();

    [Fact]
    public void AddToDrawsEverySymbolAsItsIconBeforeAnyFont()
    {
        Assert.True(_lato.Face.Has('◌'));
        GameIcons.AddTo(_fonts);
        Assert.Equal(GameIcons.Table.Keys.Order(), _fonts.IconCodePoints.Order());
        foreach (var (codePoint, icon) in GameIcons.Table)
        {
            var face = _fonts.ResolveFor(codePoint, _lato)!.Value.Face;
            Assert.True(face.IsIconFace, char.ConvertFromUtf32(codePoint));
            var glyph = face.IconOf(face.GlyphFor(codePoint))!;
            Assert.Equal((icon.Outline, icon.Advance, icon.Ascent, icon.Descent, icon.LineGap), (glyph.Outline, glyph.Advance, glyph.Ascent, glyph.Descent, glyph.LineGap));
        }
        Assert.Same(_lato.Face, _fonts.ResolveFor('A', _lato)!.Value.Face);
    }

    /// <summary>The table's box lays the symbols out: ⚔'s advance (Noto Emoji's 2600/2048 em), ♜'s ascent and descent (Noto Sans Symbols 2's), the line gap of ◌'s (Liberation Serif's).</summary>
    [Fact]
    public void TheIconsLayOutByTheTablesBoxes()
    {
        GameIcons.AddTo(_fonts);
        var style = new TextStyle { Families = ["Lato"], Size = 16 };
        Assert.Equal(20.3125f, TextLayout.Create(_fonts, "⚔", style).Width);
        Assert.Equal((17f, 10f, 0f), TextLayout.Metrics(_fonts.ResolveFor('♜', _lato)!.Value.Face, 16));
        Assert.Equal(1f, TextLayout.Metrics(_fonts.ResolveFor('◌', _lato)!.Value.Face, 24).LineGap);
    }

    [Fact]
    public void AddToIsOncePerLibrary()
    {
        GameIcons.AddTo(_fonts);
        Assert.Throws<ArgumentException>(() => GameIcons.AddTo(_fonts));
    }
}

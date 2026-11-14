using Statventure.Tests.Canvas;
using Statventure.UI.Css;
using Statventure.UI.Dom;
using Statventure.UI.Style;

namespace Statventure.Tests.Ui;

/// <summary>
/// A percentage stays one at zero (GM1 step 1b; Blink's Length of type percent): <c>0%</c> isn't <c>0px</c>. Against a
/// reference that isn't known it resolves to nothing, so <c>flex: 1</c>'s 0 % basis in a column flexbox of indefinite
/// height is the content's height (the game's equipment slots; <see cref="FlexDefinitenessReferenceTests"/> against
/// Chromium).
/// </summary>
public class ZeroPercentTests
{
    static Length Read(string text)
    {
        var reader = new ValueReader(CssTokenizer.Tokenize(text));
        Assert.True(reader.TryLength(new UnitContext(16, 16, 800, 600), out var length));
        return length;
    }

    [Fact]
    public void ZeroPercentIsAPercentage()
    {
        var zero = Read("0%");
        Assert.True(zero.HasPercent);
        Assert.True(zero.IsZero);
        Assert.True(float.IsNaN(zero.Resolve(float.NaN)));
        Assert.Equal(0, zero.Resolve(200));
        Assert.Equal("0%", zero.ToString());
        Assert.False(Read("0px").HasPercent);
        Assert.Equal(0, Read("0px").Resolve(float.NaN));
        Assert.NotEqual(Read("0px"), zero);
        Assert.Equal(Length.Pct(0), zero);
    }

    [Fact]
    public void ArithmeticKeepsThePercentage()
    {
        var zero = Length.Pct(0);
        Assert.True((zero + Length.Px(10)).HasPercent);
        Assert.True((Length.Px(10) - zero).HasPercent);
        Assert.True((zero * 2).HasPercent);
        Assert.True(Length.Lerp(zero, Length.Px(10), 0.5f).HasPercent);
        Assert.True(float.IsNaN((zero + Length.Px(10)).Resolve(float.NaN)));
        Assert.Equal(10, (zero + Length.Px(10)).Resolve(50));
        Assert.False((Length.Px(1) + Length.Px(2)).HasPercent);
    }

    [Fact]
    public void AZeroPercentBasisInAColumnOfAutoHeightIsTheContents()
    {
        var document = new UiDocument(TestFonts.Library);
        document.SetViewport(300, 400);
        document.AddStylesheet("""
            body { margin: 0; font: 16px/20px Lato; }
            .c { display: flex; flex-direction: column; }
            .g { flex: 1; display: flex; flex-direction: column; }
            .h { flex: 1; min-height: 30px; }
            .p { flex: 1 1 0px; min-height: 30px; }
            i { display: block; }
            """, "t");
        Markup.SetInnerHtml(document.Body, """
            <div class="c" id="c"><div class="g"><div class="h" id="h"><i>1</i><i>2</i><i>3</i><i>4</i></div></div></div>
            <div class="c" id="px"><div class="p" id="p"><i>1</i><i>2</i><i>3</i><i>4</i></div></div>
            """);
        document.Update(0);
        // 0 %: the content's 80 px. 0px: a definite basis, the item its min-height.
        Assert.Equal(80, document.GetElementById("h")!.GetBoundingClientRect().Height, 2);
        Assert.Equal(80, document.GetElementById("c")!.GetBoundingClientRect().Height, 2);
        Assert.Equal(30, document.GetElementById("p")!.GetBoundingClientRect().Height, 2);
    }
}

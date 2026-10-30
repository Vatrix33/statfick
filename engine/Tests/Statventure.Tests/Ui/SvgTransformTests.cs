using System.Numerics;
using Statventure.Tests.Canvas;
using Statventure.UI.Dom;
using Statventure.UI.Style;

namespace Statventure.Tests.Ui;

/// <summary>
/// The SVG transform attribute as Blink 152 reads it: parsed as SVGTransformList does (a list that
/// doesn't parse is empty), mapped onto CSS transform as its presentation attribute (below every
/// author rule, on SVG elements only), restyled when it changes; SVG elements transform about 0 0
/// unless a rule says otherwise. Painting is checked against Chromium by the "svg-transforms"
/// paint fixture, layer bounds by the shop's compositing reference.
/// </summary>
public class SvgTransformTests
{
    [Theory]
    [InlineData("translate(10)", "translate(10px, 0px)")]
    [InlineData("translate(10, 20) scale(2)", "translate(10px, 20px) scale(2, 2)")]
    [InlineData("rotate(30 20 20)", "translate(20px, 20px) rotate(30deg) translate(-20px, -20px)")]
    [InlineData("rotate(30, 0, 0)", "rotate(30deg)")]
    [InlineData("matrix(1 0.2 0 1 2 0)", "matrix(1, 0.2, 0, 1, 2, 0)")]
    [InlineData("  skewX(20)skewY(-5)  ", "skewX(20deg) skewY(-5deg)")]
    [InlineData("scale(1e1)", "scale(10, 10)")]
    [InlineData("", "none")]
    // One or three arguments for rotate; no trailing comma inside or after the list; a dot needs a
    // digit after it; the names are case-sensitive; anything that doesn't parse empties the list.
    [InlineData("rotate(30 20)", "none")]
    [InlineData("translate(10,)", "none")]
    [InlineData("translate(10) ,", "none")]
    [InlineData("translate(1.)", "none")]
    [InlineData("Translate(10)", "none")]
    [InlineData("translate(10) foo", "none")]
    public void TheAttributeParsesAsBlinkDoes(string attribute, string css) =>
        Assert.Equal(css, SvgTransformList.CssText(SvgTransformList.Parse(attribute)));

    static UiDocument Load(string css, string html)
    {
        var document = new UiDocument(TestFonts.Library);
        document.SetViewport(400, 300);
        document.AddStylesheet(css);
        Markup.SetInnerHtml(document.Body, html);
        document.Update(0);
        return document;
    }

    static Vector2 Translation(UiDocument document, string selector)
    {
        var style = document.QuerySelector(selector)!.Style!;
        Assert.Equal(TransformKind.Translate, Assert.Single(style.Transform).Kind);
        var m = style.Transform[0].ToMatrix(Vector2.Zero);
        return new Vector2(m.M31, m.M32);
    }

    [Fact]
    public void TheAttributeIsAPresentationAttributeOfCssTransform()
    {
        var document = Load(".flat { transform: none; } .turn { transform: rotate(10deg); } .frozen { transform: none !important; }", """
            <svg id="s" viewBox="0 0 10 10" transform="translate(1 2)">
              <rect id="a" transform="translate(3 4)"/><rect id="b" class="flat" transform="translate(3 4)"/>
              <g id="c" class="turn" transform="translate(3 4)"></g><rect id="d" transform="translate(3 4) nonsense"/>
              <circle id="e" class="frozen" style="transform: translate(5px, 5px)" transform="translate(3 4)"/>
            </svg>
            <div id="f" transform="translate(3 4)"></div>
            """);
        Assert.Equal(new Vector2(3, 4), Translation(document, "#a"));
        // Any author rule wins over the attribute; one that doesn't parse leaves no transform.
        Assert.False(document.QuerySelector("#b")!.Style!.HasTransform);
        Assert.Equal(TransformKind.Rotate, Assert.Single(document.QuerySelector("#c")!.Style!.Transform).Kind);
        Assert.False(document.QuerySelector("#d")!.Style!.HasTransform);
        Assert.False(document.QuerySelector("#e")!.Style!.HasTransform);
        // The outermost svg maps it too (it's an SVG element); an HTML element has no such attribute.
        Assert.Equal(new Vector2(1, 2), Translation(document, "#s"));
        Assert.False(document.QuerySelector("#f")!.Style!.HasTransform);
    }

    [Fact]
    public void SvgElementsTransformAboutTheirOrigin()
    {
        var document = Load(".spin { transform-box: fill-box; transform-origin: 50% 50%; }", """
            <svg id="s" viewBox="0 0 10 10"><g id="g"><svg id="inner"><rect id="r" class="spin"/></svg></g></svg>
            """);
        foreach (var id in new[] { "#g", "#inner" })
        {
            var style = document.QuerySelector(id)!.Style!;
            Assert.Equal((Length.Px(0), Length.Px(0)), (style.TransformOriginX, style.TransformOriginY));
            Assert.Equal(TransformBox.ViewBox, style.TransformBox);
        }
        var outer = document.QuerySelector("#s")!.Style!;
        Assert.Equal((Length.Pct(50), Length.Pct(50)), (outer.TransformOriginX, outer.TransformOriginY));
        var spin = document.QuerySelector("#r")!.Style!;
        Assert.Equal(TransformBox.FillBox, spin.TransformBox);
        Assert.Equal((Length.Pct(50), Length.Pct(50)), (spin.TransformOriginX, spin.TransformOriginY));
    }

    [Fact]
    public void ChangingTheAttributeRestylesTheElement()
    {
        var document = Load("", """<svg viewBox="0 0 10 10"><rect id="a" transform="translate(3 4)"/></svg>""");
        var a = document.QuerySelector("#a")!;
        a.SetAttribute("transform", "translate(5, 6)");
        document.Update(0);
        Assert.Equal(new Vector2(5, 6), Translation(document, "#a"));
        a.RemoveAttribute("transform");
        document.Update(0);
        Assert.False(a.Style!.HasTransform);
    }
}

using System.Numerics;
using Statventure.Tests.Canvas;
using Statventure.UI.Dom;

namespace Statventure.Tests.Ui;

/// <summary>
/// The cursor property's images as Blink chooses them (EventHandler::SelectCursor): the first one
/// loaded, at most 128 px a side, at its hotspot; one larger than 32 px only where it fits in the
/// viewport; a list without its keyword is no declaration.
/// </summary>
public class UiCursorTests
{
    static UiDocument Load(string css, Dictionary<string, Vector2> images)
    {
        var document = new UiDocument(TestFonts.Library);
        document.SetViewport(400, 300);
        document.AddStylesheet("body { margin: 0; } div { position: absolute; left: 0; top: 0; width: 400px; height: 300px; } " + css);
        Markup.SetInnerHtml(document.Body, """<div id="a"></div>""");
        document.Layout.Context.ImageSize = url => images.TryGetValue(url, out var size) ? size : null;
        document.Update(0);
        return document;
    }

    static (string Cursor, UiCursorImage? Image) At(UiDocument document, float x, float y)
    {
        document.Input.PointerMove(new Vector2(x, y));
        return (document.Input.Cursor, document.Input.CursorImage);
    }

    [Fact]
    public void TheFirstLoadedImageShowsAtItsHotspot()
    {
        var document = Load("#a { cursor: url(missing.png) 1 1, url(hand.png) 4 5, url(other.png), pointer; }",
            new() { ["hand.png"] = new(16, 16), ["other.png"] = new(16, 16) });
        Assert.Equal(("pointer", new UiCursorImage("hand.png", new Vector2(4, 5))), At(document, 100, 100));
    }

    [Fact]
    public void WithoutAHotspotItIsTheCorner()
    {
        var document = Load("#a { cursor: url(hand.png), default; }", new() { ["hand.png"] = new(16, 16) });
        Assert.Equal(new UiCursorImage("hand.png", Vector2.Zero), At(document, 100, 100).Image);
    }

    [Fact]
    public void ImagesOver128PixelsAreSkipped()
    {
        var document = Load("#a { cursor: url(huge.png), url(small.png), crosshair; }", new() { ["huge.png"] = new(129, 20), ["small.png"] = new(20, 20) });
        Assert.Equal("small.png", At(document, 100, 100).Image?.Url);
    }

    [Fact]
    public void LargeImagesOnlyWhereTheyFitInTheViewport()
    {
        var document = Load("#a { cursor: url(large.png) 2 2, crosshair; }", new() { ["large.png"] = new(48, 48) });
        Assert.Equal("large.png", At(document, 100, 100).Image?.Url);
        Assert.Equal(("crosshair", (UiCursorImage?)null), At(document, 380, 100));
        Assert.Equal(("crosshair", (UiCursorImage?)null), At(document, 1, 100));
    }

    [Fact]
    public void AListWithoutItsKeywordIsIgnored()
    {
        var document = Load("#a { cursor: crosshair; } #a { cursor: url(hand.png); } #a { cursor: url(hand.png) 3, pointer; } #a { cursor: wobble; }",
            new() { ["hand.png"] = new(16, 16) });
        Assert.Equal(("crosshair", (UiCursorImage?)null), At(document, 100, 100));
    }
}

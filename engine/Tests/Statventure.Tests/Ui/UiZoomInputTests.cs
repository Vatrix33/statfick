using System.Numerics;
using Statventure.Platform;
using Statventure.Tests.Canvas;
using Statventure.UI.Dom;

namespace Statventure.Tests.Ui;

/// <summary>
/// Input on a zoomed page, as Chromium 152 takes it with zoom for device scale: the window's
/// input in window units (DIPs) is scaled to device pixels for hit testing and scrolling, the DOM
/// sees CSS pixels (MouseEvent::SetCoordinatesFromWebPointerProperties, WheelEvent's delta /
/// devicePixelRatio), lines are 40 window units (PixelsPerLineStep through WindowToViewportScalar).
/// </summary>
public class UiZoomInputTests
{
    const string Css = """
        body { margin: 0; font: 16px Lato; }
        .scroller { width: 200px; height: 200px; overflow: auto; }
        .tall { height: 1000px; }
        #b { position: absolute; left: 300px; top: 100px; width: 100px; height: 50px; }
        """;

    static UiDocument Make(string html, float deviceScale, float pageZoom = 1)
    {
        var document = new UiDocument(TestFonts.Library);
        document.SetZoom(deviceScale, pageZoom);
        document.SetViewport(800, 600);
        document.AddStylesheet(Css);
        Markup.SetInnerHtml(document.Body, html);
        document.Update(0);
        return document;
    }

    [Fact]
    public void PointerPositionsAreWindowUnitsAndEventsSayCssPixels()
    {
        var document = Make("""<div id="b"></div>""", 1.65f, 1.25f);
        var events = new List<UiMouseEvent>();
        var b = document.GetElementById("b")!;
        b.AddEventListener<UiMouseEvent>("mousemove", events.Add);
        b.AddEventListener<UiMouseEvent>("pointermove", events.Add);
        // The box is at 300..400 CSS px, which is 375..500 window units at a page zoom of 1.25.
        document.Input.PointerMove(new Vector2(370, 140));
        Assert.Empty(events);
        document.Input.PointerMove(new Vector2(380.5f, 140.25f));
        Assert.Equal(b, document.Input.Hovered);
        Assert.Equal(2, events.Count);
        // Unrounded CSS px: the window position over the page zoom.
        Assert.Equal(304.4f, events[0].Position.X, 3);
        Assert.Equal(112.2f, events[0].Position.Y, 3);
        // Pointer events keep fractions; mouse events floor.
        Assert.Equal(304.4f, events[0].ClientX, 3);
        Assert.Equal(304, events[1].ClientX);
        Assert.Equal(112, events[1].ClientY);
        // Movement: whole window units (truncated screen positions).
        Assert.Equal(new Vector2(10, 0), events[0].Movement);
    }

    [Fact]
    public void ANotchScrolls120CssPixelsAndSaysSo()
    {
        var document = Make("""<div id="s" class="scroller"><div class="tall"></div></div>""", 1.65f);
        var scroller = document.GetElementById("s")!;
        var deltas = new List<float>();
        scroller.AddEventListener<UiWheelEvent>("wheel", e => deltas.Add(e.DeltaY));
        document.Input.Wheel(new Vector2(50, 50), new Vector2(0, 120));
        document.Update(1);
        Assert.Equal([120f], deltas);
        Assert.Equal(120, scroller.ScrollTop, 2);
    }

    [Fact]
    public void PageZoomShrinksWheelDeltasAndScrolls()
    {
        var document = Make("""<div id="s" class="scroller"><div class="tall"></div></div>""", 1.65f, 1.2f);
        var scroller = document.GetElementById("s")!;
        var deltas = new List<float>();
        scroller.AddEventListener<UiWheelEvent>("wheel", e => deltas.Add(e.DeltaY));
        document.Input.Wheel(new Vector2(50, 50), new Vector2(0, 120));
        document.Update(1);
        // 120 window units are 198 device px; devicePixelRatio is 1.98.
        Assert.Single(deltas);
        Assert.Equal(100f, deltas[0], 3);
        Assert.Equal(100, scroller.ScrollTop, 2);
    }

    [Fact]
    public void ArrowKeysScrollFortyWindowUnits()
    {
        var document = Make("""<div class="tall"></div><div class="tall"></div>""", 1.65f, 1.2f);
        document.Input.KeyDown("ArrowDown", "ArrowDown");
        document.Update(1);
        document.Input.KeyUp("ArrowDown", "ArrowDown");
        // 40 × 1.65 = 66 device px, over a zoom of 1.98.
        Assert.Equal(66 / 1.98f, document.ScrollPosition.Y, 3);
    }

    [Fact]
    public void DoubleClicksMeasureDistanceInWindowUnits()
    {
        var document = Make("""<div id="b"></div>""", 2f);
        var details = new List<int>();
        document.GetElementById("b")!.AddEventListener<UiMouseEvent>("mousedown", e => details.Add(e.Detail));
        document.Input.PointerDown(new Vector2(310, 110), MouseButton.Left);
        document.Input.PointerUp(new Vector2(310, 110), MouseButton.Left);
        document.Update(0.1);
        // 4 window units away (8 device px): still a double click.
        document.Input.PointerDown(new Vector2(314, 110), MouseButton.Left);
        document.Input.PointerUp(new Vector2(314, 110), MouseButton.Left);
        Assert.Equal([1, 2], details);
    }
}

using System.Numerics;
using Statventure.Platform;
using Statventure.Tests.Canvas;
using Statventure.UI.Dom;
using Statventure.UI.Layout;
using Statventure.UI.Style;

namespace Statventure.Tests.Ui;

/// <summary>
/// Scrolling as in Chromium 152: the scroll API and its clamping, scroll and scrollend events,
/// the smooth scroll curves, wheel latching and chaining, keyboard scrolling, the scrollbars'
/// thumb, buttons and track, the CSSOM geometry API and scrollIntoView.
/// </summary>
public class UiScrollingTests
{
    const string Css = """
        body { margin: 0; font: 16px Lato; }
        .scroller { width: 200px; height: 200px; overflow: auto; }
        .tall { height: 1000px; }
        .wide { width: 1000px; height: 10px; }
        """;

    static UiDocument Make(string html, string css = "")
    {
        var document = new UiDocument(TestFonts.Library);
        document.SetViewport(800, 600);
        document.AddStylesheet(Css + css);
        Markup.SetInnerHtml(document.Body, html);
        document.Update(0);
        return document;
    }

    static UiElement Get(UiDocument document, string id) => document.GetElementById(id)!;

    // ---------- The scroll API ----------

    [Fact]
    public void ScrollPositionsClampToWhatCanScroll()
    {
        var document = Make("""<div id="s" class="scroller"><div class="tall"></div></div><div style="height: 2000px"></div>""");
        var scroller = Get(document, "s");
        scroller.ScrollTop = 5000;
        Assert.Equal(800, scroller.ScrollTop);
        scroller.ScrollTop = -20;
        Assert.Equal(0, scroller.ScrollTop);
        // The root element's scroll position is the document's.
        document.Root.ScrollTop = 300;
        Assert.Equal(300, document.ScrollPosition.Y);
        Assert.Equal(300, document.Root.ScrollTop);
        // Elements that don't scroll stay at 0.
        document.Body.ScrollTop = 50;
        Assert.Equal(0, document.Body.ScrollTop);
    }

    [Fact]
    public void ScrollPositionsFollowContentThatShrinks()
    {
        var document = Make("""<div id="s" class="scroller"><div id="content" class="tall"></div></div>""");
        var scroller = Get(document, "s");
        scroller.ScrollTop = 800;
        document.Update(0.016);
        Get(document, "content").SetStyle("height", "500px");
        document.Update(0.032);
        Assert.Equal(300, scroller.ScrollTop);
    }

    [Fact]
    public void RemovedElementsForgetTheirScrollPositionButHiddenOnesKeepIt()
    {
        var document = Make("""<div id="s" class="scroller"><div class="tall"></div></div>""");
        var scroller = Get(document, "s");
        scroller.ScrollTop = 120;
        scroller.SetStyle("display", "none");
        document.Update(0.016);
        scroller.SetStyle("display", null);
        document.Update(0.032);
        Assert.Equal(120, scroller.ScrollTop);
        scroller.Remove();
        document.Body.InsertBefore(scroller, null);
        document.Update(0.048);
        Assert.Equal(0, scroller.ScrollTop);
    }

    [Fact]
    public void ScrollEventsComeOncePerFrameThenScrollend()
    {
        var document = Make("""<div id="s" class="scroller"><div class="tall"></div></div><div style="height: 2000px"></div>""");
        var scroller = Get(document, "s");
        var log = new List<string>();
        scroller.AddEventListener("scroll", e => log.Add("scroll " + scroller.ScrollTop));
        scroller.AddEventListener("scrollend", e => log.Add("scrollend"));
        document.AddEventListener("scroll", e => log.Add("document scroll"));
        scroller.ScrollTop = 10;
        scroller.ScrollTop = 20;
        document.ScrollPosition = new Vector2(0, 100);
        Assert.Empty(log);
        document.Update(0.016);
        // Blink's per-frame event queue: an instant scroll queues scroll then scrollend at once (once each per frame).
        Assert.Equal(["scroll 20", "scrollend", "document scroll"], log);
        log.Clear();
        document.Update(0.032);
        Assert.Empty(log);
    }

    [Fact]
    public void SmoothScrollsFollowChromiumsProgrammaticCurve()
    {
        var document = Make("""<div id="s" class="scroller" style="scroll-behavior: smooth"><div class="tall"></div></div>""");
        var scroller = Get(document, "s");
        var ended = 0;
        scroller.AddEventListener("scrollend", e => ended++);
        scroller.ScrollTop = 400;
        Assert.Equal(0, scroller.ScrollTop);
        // √400 = 20 frames: a third of a second, on cubic-bezier(0.4, 0, 0, 1).
        document.Update(10 / 60.0);
        Assert.InRange(scroller.ScrollTop, 400 * 0.85f, 400 * 0.87f);
        // The offsets are whole pixels (the curve is within half a pixel of its end here, but still running).
        document.Update(19 / 60.0);
        Assert.Equal(MathF.Round(scroller.ScrollTop), scroller.ScrollTop);
        Assert.Equal(0, ended);
        document.Update(20 / 60.0);
        Assert.Equal(400, scroller.ScrollTop);
        document.Update(21 / 60.0);
        Assert.Equal(1, ended);
        // Auto follows scroll-behavior; an instant scroll ignores it.
        scroller.ScrollTo(0, 100);
        Assert.Equal(400, scroller.ScrollTop);
        scroller.ScrollTo(0, 100, ScrollMode.Instant);
        Assert.Equal(100, scroller.ScrollTop);
    }

    // ---------- The wheel ----------

    [Fact]
    public void TheWheelScrollsTheScrollerUnderThePointerSmoothly()
    {
        var document = Make("""<div id="s" class="scroller"><div class="tall"></div></div><div style="height: 2000px"></div>""");
        var scroller = Get(document, "s");
        var deltas = new List<float>();
        scroller.AddEventListener<UiWheelEvent>("wheel", e => deltas.Add(e.DeltaY));
        Assert.True(document.Input.Wheel(new Vector2(50, 50), new Vector2(0, 120)));
        Assert.Equal([120f], deltas);
        // 120 px is 12 frames (0.2 s), ease-in-out: halfway at half time.
        document.Update(0.1);
        Assert.InRange(scroller.ScrollTop, 59, 61);
        document.Update(0.2);
        Assert.Equal(120, scroller.ScrollTop);
        Assert.Equal(0, document.ScrollPosition.Y);
    }

    [Fact]
    public void ACancelledWheelDoesNotScroll()
    {
        var document = Make("""<div id="s" class="scroller"><div class="tall"></div></div>""");
        var scroller = Get(document, "s");
        scroller.AddEventListener("wheel", e => e.PreventDefault());
        Assert.False(document.Input.Wheel(new Vector2(50, 50), new Vector2(0, 120)));
        document.Update(0.3);
        Assert.Equal(0, scroller.ScrollTop);
    }

    [Fact]
    public void OnlyTheFirstWheelEventOfATurnIsCancelableWhenItScrolled()
    {
        var document = Make("""<div id="s" class="scroller"><div class="tall"></div></div>""");
        var cancelable = new List<bool>();
        Get(document, "s").AddEventListener("wheel", e => cancelable.Add(e.Cancelable));
        document.Input.Wheel(new Vector2(50, 50), new Vector2(0, 120));
        document.Update(0.05);
        document.Input.Wheel(new Vector2(50, 50), new Vector2(0, 120));
        Assert.Equal([true, false], cancelable);
    }

    [Fact]
    public void AWheelTurnStaysWithItsScrollerAndChainsOnlyWhenItStarts()
    {
        var document = Make("""<div id="s" class="scroller"><div class="tall"></div></div><div style="height: 2000px"></div>""");
        var scroller = Get(document, "s");
        scroller.ScrollTop = 760;
        document.Update(0.016);
        // The turn latches to the scroller, which still can scroll: no chaining when it reaches its end.
        document.Input.Wheel(new Vector2(50, 50), new Vector2(0, 120));
        document.Update(0.1);
        document.Input.Wheel(new Vector2(50, 50), new Vector2(0, 120));
        document.Update(0.4);
        Assert.Equal(800, scroller.ScrollTop);
        Assert.Equal(0, document.ScrollPosition.Y);
        // Half a second later a new turn starts: the scroller is at its end, so the document scrolls.
        document.Update(1.0);
        document.Input.Wheel(new Vector2(50, 50), new Vector2(0, 120));
        document.Update(1.3);
        Assert.Equal(120, document.ScrollPosition.Y);
    }

    [Fact]
    public void OverscrollBehaviorStopsChaining()
    {
        var document = Make("""<div id="s" class="scroller" style="overscroll-behavior: contain"><div class="tall"></div></div><div style="height: 2000px"></div>""");
        Get(document, "s").ScrollTop = 800;
        document.Update(0.016);
        document.Input.Wheel(new Vector2(50, 50), new Vector2(0, 120));
        document.Update(0.5);
        Assert.Equal(0, document.ScrollPosition.Y);
    }

    [Fact]
    public void OverflowHiddenIsNotScrolledByTheWheel()
    {
        var document = Make("""<div id="s" class="scroller" style="overflow: hidden"><div class="tall"></div></div><div style="height: 2000px"></div>""");
        document.Input.Wheel(new Vector2(50, 50), new Vector2(0, 120));
        document.Update(0.5);
        Assert.Equal(0, Get(document, "s").ScrollTop);
        Assert.Equal(120, document.ScrollPosition.Y);
    }

    [Fact]
    public void ShiftTurnsTheWheelHorizontalAndCtrlZooms()
    {
        var document = Make("""<div id="s" class="scroller"><div class="wide"></div><div class="tall"></div></div>""");
        var scroller = Get(document, "s");
        var reported = new List<(float X, float Y)>();
        scroller.AddEventListener<UiWheelEvent>("wheel", e => reported.Add((e.DeltaX, e.DeltaY)));
        document.Input.Wheel(new Vector2(50, 50), new Vector2(0, 120), KeyModifiers.Shift);
        document.Update(0.3);
        Assert.Equal(120, scroller.ScrollLeft);
        Assert.Equal(0, scroller.ScrollTop);
        Assert.Equal([(0f, 120f)], reported);
        Vector2? zoom = null;
        document.Input.ZoomWheel += delta => zoom = delta;
        document.Update(1.0);
        document.Input.Wheel(new Vector2(50, 50), new Vector2(0, -120), KeyModifiers.Control);
        document.Update(1.3);
        Assert.Equal(new Vector2(0, -120), zoom);
        Assert.Equal(0, scroller.ScrollTop);
    }

    [Fact]
    public void MoreWheelTurnsRetargetTheRunningScroll()
    {
        var document = Make("""<div id="s" class="scroller"><div class="tall"></div></div>""");
        var scroller = Get(document, "s");
        document.Input.Wheel(new Vector2(50, 50), new Vector2(0, 120));
        document.Update(0.05);
        var before = scroller.ScrollTop;
        document.Input.Wheel(new Vector2(50, 50), new Vector2(0, 120));
        document.Update(0.06);
        // Still moving forward from where it was (no jump back, no stop).
        Assert.True(scroller.ScrollTop > before);
        document.Update(1.0);
        Assert.Equal(240, scroller.ScrollTop);
    }

    // ---------- The keyboard ----------

    [Fact]
    public void KeysScrollTheDocumentByLinesPagesAndToTheEnds()
    {
        var document = Make("""<div style="height: 3000px"></div>""");
        document.Input.KeyDown("ArrowDown", "ArrowDown");
        document.Update(0.15);
        Assert.Equal(40, document.ScrollPosition.Y);
        document.Input.KeyUp("ArrowDown", "ArrowDown");
        document.Input.KeyDown("PageDown", "PageDown");
        document.Update(0.4);
        // 87.5% of the 600 px viewport.
        Assert.Equal(40 + 525, document.ScrollPosition.Y);
        document.Input.KeyDown("End", "End");
        document.Update(0.7);
        Assert.Equal(2400, document.ScrollPosition.Y);
        document.Input.KeyDown(" ", "Space", KeyModifiers.Shift);
        document.Update(1.0);
        Assert.Equal(2400 - 525, document.ScrollPosition.Y);
        document.Input.KeyDown("Home", "Home");
        document.Update(1.3);
        Assert.Equal(0, document.ScrollPosition.Y);
    }

    [Fact]
    public void KeysScrollTheFocusedElementsScrollerFirst()
    {
        var document = Make("""<div id="s" class="scroller" tabindex="0"><div class="tall"></div></div><div style="height: 2000px"></div>""");
        var scroller = Get(document, "s");
        scroller.Focus();
        document.Input.KeyDown("ArrowDown", "ArrowDown");
        document.Update(0.2);
        Assert.Equal(40, scroller.ScrollTop);
        Assert.Equal(0, document.ScrollPosition.Y);
        // At its end, the scroll goes on to the document.
        scroller.ScrollTop = 800;
        document.Input.KeyDown("ArrowDown", "ArrowDown", repeat: true);
        document.Update(0.5);
        Assert.Equal(40, document.ScrollPosition.Y);
    }

    [Fact]
    public void TextFieldsKeepTheirKeys()
    {
        var document = Make("""<input id="field"><div style="height: 3000px"></div>""");
        Get(document, "field").Focus();
        document.Input.KeyDown("ArrowDown", "ArrowDown");
        document.Input.KeyDown(" ", "Space");
        document.Update(0.5);
        Assert.Equal(0, document.ScrollPosition.Y);
    }

    [Fact]
    public void PreventedKeysDoNotScroll()
    {
        var document = Make("""<div style="height: 3000px"></div>""");
        document.Body.AddEventListener("keydown", e => e.PreventDefault());
        document.Input.KeyDown("PageDown", "PageDown");
        document.Update(0.5);
        Assert.Equal(0, document.ScrollPosition.Y);
    }

    // ---------- Scrollbars ----------

    /// <summary>A 200×200 scroller of 1000 px at the top left: its 15 px bar is at x 185, the track from y 18 to 182, a 33 px thumb.</summary>
    const string ScrollerPage = """<div id="s" class="scroller"><div class="tall"></div></div>""";

    [Fact]
    public void TheScrollbarHasChromiumsGeometry()
    {
        var document = Make(ScrollerPage);
        var box = Get(document, "s").Box!;
        var (vertical, horizontal) = ScrollbarGeometry.For(box, box.ViewportRect, 0, 0);
        Assert.Null(horizontal);
        var bar = vertical!.Value;
        Assert.Equal(new Statventure.Core.RectF(185, 0, 15, 200), bar.Bar);
        Assert.Equal(new Statventure.Core.RectF(185, 0, 15, 18), bar.BackButton);
        Assert.Equal(new Statventure.Core.RectF(185, 182, 15, 18), bar.ForwardButton);
        Assert.Equal(164, bar.TrackLength);
        Assert.Equal(33, bar.ThumbLength);
        Assert.Equal(new Statventure.Core.RectF(188, 18, 9, 33), bar.Thumb);
        Assert.Equal(ScrollbarPart.Thumb, bar.PartAt(new Vector2(186, 30)));
        Assert.Equal(ScrollbarPart.ForwardTrack, bar.PartAt(new Vector2(192, 100)));
        // Scrolled halfway: the thumb is halfway along the rest of the track (positions truncate).
        var half = ScrollbarGeometry.For(box, box.ViewportRect, 0, 400).Vertical!.Value;
        Assert.Equal(65, half.ThumbPosition);
        // Thin bars are two thirds: 10 px, 12 px buttons, a 6 px thumb.
        Get(document, "s").SetStyle("scrollbar-width", "thin");
        document.Update(0.016);
        var thin = ScrollbarGeometry.For(box, box.ViewportRect, 0, 0).Vertical!.Value;
        Assert.Equal(10, thin.Bar.Width);
        Assert.Equal(12, thin.BackButton.Height);
        Assert.Equal(6, thin.ThumbThickness);
    }

    [Fact]
    public void DraggingTheThumbScrollsInProportion()
    {
        var document = Make(ScrollerPage);
        var scroller = Get(document, "s");
        var clicks = 0;
        var downs = 0;
        scroller.AddEventListener("click", e => clicks++);
        scroller.AddEventListener("mousedown", e => downs++);
        document.Input.PointerMove(new Vector2(192, 30));
        document.Input.PointerDown(new Vector2(192, 30), MouseButton.Left);
        document.Input.PointerMove(new Vector2(192, 40));
        // (1000 - 200) / (164 - 33) px of content per pixel of thumb, in whole pixels (Chromium 152 has no fractional scroll offsets).
        Assert.Equal(MathF.Round(10 * 800f / 131), scroller.ScrollTop);
        // Far off the bar the drag goes on (no snap-back on Linux).
        document.Input.ScrollbarSnapBack = false;
        document.Input.PointerMove(new Vector2(600, 50));
        Assert.Equal(MathF.Round(20 * 800f / 131), scroller.ScrollTop);
        document.Input.PointerUp(new Vector2(600, 50), MouseButton.Left);
        Assert.Equal(1, downs);
        Assert.Equal(0, clicks);
    }

    [Fact]
    public void WindowsSnapsTheThumbBackFarFromTheBar()
    {
        var document = Make(ScrollerPage);
        var scroller = Get(document, "s");
        document.Input.ScrollbarSnapBack = true;
        document.Input.PointerDown(new Vector2(192, 30), MouseButton.Left);
        document.Input.PointerMove(new Vector2(192, 60));
        Assert.True(scroller.ScrollTop > 0);
        // Beyond eight bar thicknesses beside it.
        document.Input.PointerMove(new Vector2(185 - 8 * 15 - 5, 60));
        Assert.Equal(0, scroller.ScrollTop);
    }

    [Fact]
    public void ArrowButtonsScrollALineAndKeepScrollingWhileHeld()
    {
        var document = Make(ScrollerPage);
        var scroller = Get(document, "s");
        document.Input.PointerDown(new Vector2(192, 190), MouseButton.Left);
        Assert.Equal((Get(document, "s"), true, ScrollbarPart.ForwardButton), document.ScrollbarPress);
        document.Update(0.2);
        Assert.Equal(40, scroller.ScrollTop);
        // After a quarter second it scrolls on at 20 lines a second.
        document.Update(0.25);
        document.Update(0.35);
        Assert.InRange(scroller.ScrollTop, 40 + 80 - 2, 40 + 80 + 2);
        document.Input.PointerUp(new Vector2(192, 190), MouseButton.Left);
        var stopped = scroller.ScrollTop;
        document.Update(0.6);
        Assert.Equal(stopped, scroller.ScrollTop);
        Assert.Null(document.ScrollbarPress);
    }

    [Fact]
    public void TheTrackPagesTowardsThePointerUntilTheThumbReachesIt()
    {
        var document = Make(ScrollerPage);
        var scroller = Get(document, "s");
        document.Input.PointerDown(new Vector2(192, 120), MouseButton.Left);
        document.Update(0.2);
        // 87.5% of the 200 px scrollport.
        Assert.Equal(175, scroller.ScrollTop);
        // Held, it pages on until the thumb is under the pointer.
        for (var t = 0.25; t < 2; t += 0.016)
            document.Update(t);
        var geometry = ScrollbarGeometry.For(scroller.Box!, scroller.Box!.ViewportRect, 0, scroller.ScrollTop).Vertical!.Value;
        Assert.True(geometry.ThumbArea.Bottom > 120);
        Assert.True(scroller.ScrollTop < 800);
        document.Input.PointerUp(new Vector2(192, 120), MouseButton.Left);
    }

    [Fact]
    public void ScrollbarsShowHoveredPartsAndTheArrowCursor()
    {
        var document = Make(ScrollerPage);
        document.Input.PointerMove(new Vector2(192, 30));
        Assert.Equal((Get(document, "s"), true, ScrollbarPart.Thumb), document.ScrollbarHover);
        Assert.Equal("default", document.Input.Cursor);
        document.Input.PointerMove(new Vector2(50, 30));
        Assert.Null(document.ScrollbarHover);
    }

    [Fact]
    public void TheViewportsScrollbarScrollsTheDocument()
    {
        var document = Make("""<div style="height: 3000px"></div>""");
        var targets = new List<UiElement?>();
        document.AddEventListener("mousedown", e => targets.Add(e.Target));
        // The viewport's bar is at x 785; its down button at the bottom.
        document.Input.PointerDown(new Vector2(792, 590), MouseButton.Left);
        document.Update(0.2);
        document.Input.PointerUp(new Vector2(792, 590), MouseButton.Left);
        Assert.Equal(40, document.ScrollPosition.Y);
        // The page sees the press on the root element.
        Assert.Equal([document.Root], targets);
    }

    // ---------- Geometry ----------

    [Fact]
    public void TheGeometryApiMatchesCssomView()
    {
        var document = Make("""
            <div id="pos" style="position: relative; margin: 10px; padding: 5px; border: 3px solid; width: 300px">
              <div id="s" class="scroller" style="border: 2px solid; padding: 4px"><div class="tall"></div></div>
              <span id="hidden" style="display: none">x</span>
              <div id="moved" style="width: 50px; height: 20px; transform: translateX(30px) scale(2)"></div>
            </div>
            """);
        var scroller = Get(document, "s");
        Assert.Equal(200 + 8 - 15, scroller.ClientWidth);
        Assert.Equal(208, scroller.ClientHeight);
        Assert.Equal(2, scroller.ClientLeft);
        Assert.Equal(1004 + 4, scroller.ScrollHeight);
        Assert.Equal(212, scroller.OffsetHeight);
        Assert.Equal(Get(document, "pos"), scroller.OffsetParent);
        Assert.Equal(5, scroller.OffsetTop);
        Assert.Equal(5, scroller.OffsetLeft);
        Assert.Equal(document.Body, Get(document, "pos").OffsetParent);
        Assert.Equal(10, Get(document, "pos").OffsetTop);
        Assert.Null(Get(document, "hidden").OffsetParent);
        Assert.Empty(Get(document, "hidden").GetClientRects());
        Assert.Equal(0, Get(document, "hidden").OffsetWidth);
        // Transforms change the bounding rect, not the offsets.
        var moved = Get(document, "moved");
        var rect = moved.GetBoundingClientRect();
        Assert.Equal(100, rect.Width, 3);
        Assert.Equal(40, rect.Height, 3);
        Assert.Equal(50, moved.OffsetWidth);
        // Scrolling moves bounding rects, not offsets.
        var top = scroller.GetBoundingClientRect().Y;
        document.ScrollPosition = new Vector2(0, 0);
        Assert.Equal(top, scroller.GetBoundingClientRect().Y);
        Assert.Equal(600, document.Root.ClientHeight);
    }

    // ---------- Scroll into view ----------

    [Fact]
    public void ScrollIntoViewAlignsWithScrollMarginAndPadding()
    {
        var document = Make("""
            <div id="s" class="scroller" style="scroll-padding-top: 30px">
              <div style="height: 500px"></div><div id="t" style="height: 20px; scroll-margin-top: 10px"></div><div style="height: 500px"></div>
            </div>
            """);
        var scroller = Get(document, "s");
        var target = Get(document, "t");
        target.ScrollIntoView();
        Assert.Equal(500 - 10 - 30, scroller.ScrollTop);
        // Nearest: already showing, nothing moves.
        target.ScrollIntoView(ScrollAlign.Nearest);
        Assert.Equal(460, scroller.ScrollTop);
        target.ScrollIntoView(ScrollAlign.End);
        Assert.Equal(520 - 200, scroller.ScrollTop);
        target.ScrollIntoView(ScrollAlign.Center);
        // The target with its margin (490..520) centred in the snapport (30..200).
        Assert.Equal(505 - 115, scroller.ScrollTop);
        scroller.ScrollTop = 0;
        target.ScrollIntoView(ScrollAlign.Nearest);
        Assert.Equal(520 - 200, scroller.ScrollTop);
    }

    [Fact]
    public void ScrollIntoViewScrollsEveryScrollerOutToTheDocument()
    {
        var document = Make("""
            <div style="height: 1000px"></div>
            <div id="s" class="scroller"><div style="height: 600px"></div><div id="t" style="height: 20px"></div><div style="height: 600px"></div></div>
            <div style="height: 1000px"></div>
            """);
        var target = Get(document, "t");
        target.ScrollIntoView();
        Assert.Equal(600, Get(document, "s").ScrollTop);
        Assert.Equal(1000, document.ScrollPosition.Y);
        Assert.Equal(0, target.GetBoundingClientRect().Y, 3);
    }

    // ---------- Sticky ----------

    [Fact]
    public void StickyBoxesFollowScrollingWithinTheirContainingBlock()
    {
        var document = Make("""
            <div id="s" class="scroller">
              <div style="height: 300px"><div id="head" style="position: sticky; top: 0; height: 20px"></div></div>
              <div style="height: 1000px"></div>
            </div>
            """);
        var scroller = Get(document, "s");
        var head = Get(document, "head");
        scroller.ScrollTop = 100;
        Assert.Equal(0, head.GetBoundingClientRect().Y, 3);
        // The end of its section pushes it out.
        scroller.ScrollTop = 290;
        Assert.Equal(-10, head.GetBoundingClientRect().Y, 3);
        document.Update(0.016);
        Assert.Equal(head, document.HitTest(new Vector2(50, 5))!.Value.Element);
    }
}

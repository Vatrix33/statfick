using System.Numerics;
using Statventure.Platform;
using Statventure.Tests.Canvas;
using Statventure.UI.Dom;

namespace Statventure.Tests.Ui;

/// <summary>
/// Pointer, focus and keyboard input behave as in Chromium: event order and phases, boundary
/// events, clicks and double clicks, focus moves and :focus-visible, Tab order, keyboard
/// activation, pointer capture, :hover and :active.
/// </summary>
public class UiInputTests
{
    const string Css = """
        body { margin: 0; font: 16px Lato; }
        .box { width: 200px; height: 100px; background: #ddd; }
        .inner { width: 100px; height: 50px; margin: 10px; background: #bbb; }
        .box:hover { background: rgb(1, 2, 3); }
        .inner:active { background: rgb(4, 5, 6); }
        button { width: 80px; height: 30px; cursor: pointer; }
        """;

    static UiDocument Make(string html)
    {
        var document = new UiDocument(TestFonts.Library);
        document.SetViewport(800, 600);
        document.AddStylesheet(Css);
        Markup.SetInnerHtml(document.Body, html);
        document.Update(0);
        return document;
    }

    static void Click(UiDocument document, Vector2 at)
    {
        document.Input.PointerMove(at);
        document.Input.PointerDown(at, MouseButton.Left);
        document.Input.PointerUp(at, MouseButton.Left);
        document.Update();
    }

    [Fact]
    public void AClickSendsTheEventsInChromiumsOrder()
    {
        var document = Make("""<div id="outer" class="box"><div id="inner" class="inner"></div></div>""");
        var log = new List<string>();
        var inner = document.GetElementById("inner")!;
        foreach (var type in new[] { "pointerdown", "mousedown", "pointerup", "mouseup", "click" })
            inner.AddEventListener(type, e => log.Add(e.Type));
        Click(document, new Vector2(20, 20));
        Assert.Equal(["pointerdown", "mousedown", "pointerup", "mouseup", "click"], log);
    }

    [Fact]
    public void EventsCaptureDownAndBubbleUp()
    {
        var document = Make("""<div id="outer" class="box"><div id="inner" class="inner"></div></div>""");
        var log = new List<string>();
        var outer = document.GetElementById("outer")!;
        var inner = document.GetElementById("inner")!;
        document.AddEventListener("click", e => log.Add("document capture"), capture: true);
        outer.AddEventListener("click", e => log.Add("outer capture"), capture: true);
        inner.AddEventListener("click", e => log.Add("inner " + e.Phase));
        outer.AddEventListener("click", e => log.Add("outer bubble"));
        document.AddEventListener("click", e => log.Add("document bubble"));
        Click(document, new Vector2(20, 20));
        Assert.Equal(["document capture", "outer capture", "inner AtTarget", "outer bubble", "document bubble"], log);
    }

    [Fact]
    public void StopPropagationAndPreventDefault()
    {
        var document = Make("""<div id="outer" class="box"><div id="inner" class="inner"></div></div>""");
        var outer = document.GetElementById("outer")!;
        var inner = document.GetElementById("inner")!;
        var reached = false;
        inner.AddEventListener("click", e => e.StopPropagation());
        outer.AddEventListener("click", e => reached = true);
        Click(document, new Vector2(20, 20));
        Assert.False(reached);

        var second = false;
        inner.AddEventListener("mousedown", e => e.StopImmediatePropagation());
        inner.AddEventListener("mousedown", e => second = true);
        Click(document, new Vector2(20, 20));
        Assert.False(second);

        var custom = new UiEvent("custom");
        outer.AddEventListener("custom", e => e.PreventDefault());
        Assert.False(inner.DispatchEvent(custom));
        Assert.True(custom.DefaultPrevented);
    }

    [Fact]
    public void BoundaryEventsFollowThePointer()
    {
        var document = Make("""<div id="outer" class="box"><div id="inner" class="inner"></div></div><div id="other" class="box"></div>""");
        var log = new List<string>();
        foreach (var element in document.AllElements().Where(e => e.Id is not null))
            foreach (var type in new[] { "pointerover", "pointerout", "pointerenter", "pointerleave", "mouseover", "mouseout", "mouseenter", "mouseleave" })
                element.AddEventListener(type, e => log.Add($"{e.Type} {((UiElement)e.CurrentTarget!).Id}"));
        document.Input.PointerMove(new Vector2(150, 80)); // outer only
        Assert.Equal(["pointerover outer", "pointerenter outer", "mouseover outer", "mouseenter outer"], log);
        log.Clear();
        document.Input.PointerMove(new Vector2(20, 20)); // into inner
        Assert.Equal(["pointerout outer", "pointerover inner", "pointerover outer", "pointerenter inner", "mouseout outer", "mouseover inner", "mouseover outer", "mouseenter inner"], log);
        log.Clear();
        document.Input.PointerMove(new Vector2(20, 150)); // to other
        Assert.Equal(["pointerout inner", "pointerout outer", "pointerleave inner", "pointerleave outer", "pointerover other", "pointerenter other",
            "mouseout inner", "mouseout outer", "mouseleave inner", "mouseleave outer", "mouseover other", "mouseenter other"], log);
    }

    [Fact]
    public void HoverAndActiveStatesStyleTheElements()
    {
        var document = Make("""<div id="outer" class="box"><div id="inner" class="inner"></div></div>""");
        var outer = document.GetElementById("outer")!;
        var inner = document.GetElementById("inner")!;
        document.Input.PointerMove(new Vector2(20, 20));
        document.Update();
        Assert.True(outer.IsHovered && inner.IsHovered && document.Body.IsHovered);
        Assert.Equal(new Statventure.Core.Color(1 / 255f, 2 / 255f, 3 / 255f), outer.Style!.BackgroundColor);
        document.Input.PointerDown(new Vector2(20, 20), MouseButton.Left);
        document.Update();
        Assert.Equal(new Statventure.Core.Color(4 / 255f, 5 / 255f, 6 / 255f), inner.Style!.BackgroundColor);
        document.Input.PointerUp(new Vector2(20, 20), MouseButton.Left);
        document.Update();
        Assert.False(inner.IsActive);
        document.Input.PointerLeave();
        document.Update();
        Assert.False(outer.IsHovered);
    }

    [Fact]
    public void ADoubleClickCountsAndSendsDblclick()
    {
        var document = Make("""<div id="outer" class="box"></div>""");
        var outer = document.GetElementById("outer")!;
        var details = new List<int>();
        var doubles = 0;
        outer.AddEventListener<UiMouseEvent>("click", e => details.Add(e.Detail));
        outer.AddEventListener("dblclick", e => doubles++);
        Click(document, new Vector2(50, 50));
        Click(document, new Vector2(51, 50));
        Assert.Equal([1, 2], details);
        Assert.Equal(1, doubles);
        // Too far apart in time: a new first click.
        document.Update(1.0);
        Click(document, new Vector2(51, 50));
        Assert.Equal([1, 2, 1], details);
    }

    [Fact]
    public void TheClickGoesToTheCommonAncestorOfPressAndRelease()
    {
        var document = Make("""<div id="outer" class="box"><div id="inner" class="inner"></div></div>""");
        var targets = new List<string>();
        document.AddEventListener("click", e => targets.Add(e.Target!.Id!));
        document.Input.PointerDown(new Vector2(20, 20), MouseButton.Left);
        document.Input.PointerUp(new Vector2(150, 80), MouseButton.Left);
        Assert.Equal(["outer"], targets);
    }

    [Fact]
    public void ClickingFocusesAndClickingElsewhereBlurs()
    {
        var document = Make("""<div id="outer" class="box"><button id="button">Go</button></div><input id="field">""");
        var button = document.GetElementById("button")!;
        var field = document.GetElementById("field")!;
        var log = new List<string>();
        foreach (var element in new[] { button, field })
            foreach (var type in new[] { "focus", "blur", "focusin", "focusout" })
                element.AddEventListener(type, e => log.Add($"{e.Type} {((UiElement)e.CurrentTarget!).Id}"));
        Click(document, button.Box!.ViewportRect.Center);
        Assert.Equal(button, document.FocusedElement);
        Assert.False(button.FocusVisible); // a click doesn't show focus on a button
        Click(document, field.Box!.ViewportRect.Center);
        Assert.Equal(["focus button", "focusin button", "blur button", "focusout button", "focus field", "focusin field"], log);
        Assert.True(field.FocusVisible); // text fields show it
        Click(document, new Vector2(700, 500));
        Assert.Null(document.FocusedElement);
    }

    [Fact]
    public void PreventingMousedownKeepsFocus()
    {
        var document = Make("""<button id="a">A</button><button id="b">B</button>""");
        var a = document.GetElementById("a")!;
        var b = document.GetElementById("b")!;
        a.Focus();
        b.AddEventListener("mousedown", e => e.PreventDefault());
        Click(document, b.Box!.ViewportRect.Center);
        Assert.Equal(a, document.FocusedElement);
    }

    [Fact]
    public void TabFollowsTabindexThenTreeOrder()
    {
        var document = Make("""
            <button id="one">1</button><input id="two"><div id="plain">no</div>
            <div id="three" tabindex="0">3</div><button id="first" tabindex="2">f</button><button id="skip" tabindex="-1">s</button>
            <button id="earlier" tabindex="1">e</button><button id="off" disabled>d</button>
            """);
        var order = new List<string>();
        for (var i = 0; i < 6; i++)
        {
            document.Input.KeyDown("Tab", "Tab");
            order.Add(document.FocusedElement!.Id!);
        }
        Assert.Equal(["earlier", "first", "one", "two", "three", "earlier"], order);
        Assert.True(document.FocusedElement!.FocusVisible);
        document.Input.KeyDown("Tab", "Tab", KeyModifiers.Shift);
        Assert.Equal("three", document.FocusedElement!.Id);
    }

    [Fact]
    public void EnterAndSpaceActivateButtons()
    {
        var document = Make("""<button id="button">Go</button>""");
        var button = document.GetElementById("button")!;
        var clicks = 0;
        button.AddEventListener("click", e => clicks++);
        button.Focus();
        document.Input.KeyDown("Enter", "Enter");
        Assert.Equal(1, clicks);
        document.Input.KeyDown(" ", "Space");
        Assert.Equal(1, clicks);
        Assert.True(button.IsActive);
        document.Input.KeyUp(" ", "Space");
        Assert.Equal(2, clicks);
        // Keys go to the focused element; a prevented keydown has no default action.
        var keys = new List<string>();
        button.AddEventListener<UiKeyboardEvent>("keydown", e =>
        {
            keys.Add(e.Key);
            e.PreventDefault();
        });
        document.Input.KeyDown("Enter", "Enter");
        Assert.Equal(["Enter"], keys);
        Assert.Equal(2, clicks);
    }

    [Fact]
    public void PointerCaptureKeepsThePointerEvents()
    {
        var document = Make("""<div id="outer" class="box"></div><div id="other" class="box"></div>""");
        var outer = document.GetElementById("outer")!;
        var moves = new List<string>();
        document.AddEventListener("pointermove", e => moves.Add(e.Target!.Id!));
        document.Input.PointerMove(new Vector2(10, 10));
        document.Input.PointerDown(new Vector2(10, 10), MouseButton.Left);
        document.Input.SetPointerCapture(outer);
        document.Input.PointerMove(new Vector2(10, 150));
        document.Input.PointerUp(new Vector2(10, 150), MouseButton.Left);
        document.Input.PointerMove(new Vector2(10, 160));
        Assert.Equal(["outer", "outer", "other"], moves);
        Assert.False(document.Input.HasPointerCapture(outer));
    }

    [Fact]
    public void TheCursorFollowsCss()
    {
        var document = Make("""<button id="button">Go</button><p id="text">Some text to point at</p>""");
        document.Input.PointerMove(document.GetElementById("button")!.Box!.ViewportRect.Center);
        Assert.Equal("pointer", document.Input.Cursor);
        var text = document.GetElementById("text")!.Box!.ViewportRect;
        document.Input.PointerMove(new Vector2(text.X + 20, text.Y + 8));
        Assert.Equal("text", document.Input.Cursor);
        document.Input.PointerMove(new Vector2(700, 500));
        Assert.Equal("default", document.Input.Cursor);
    }

    [Fact]
    public void HoverFollowsContentThatChangesUnderAStillPointer()
    {
        var document = Make("""<div id="outer" class="box"><div id="inner" class="inner"></div></div>""");
        var outer = document.GetElementById("outer")!;
        var inner = document.GetElementById("inner")!;
        document.Input.PointerMove(new Vector2(20, 20));
        document.Update();
        Assert.True(inner.IsHovered);
        var entered = new List<string>();
        outer.AddEventListener("mouseover", e => entered.Add(e.Target!.Id!));
        inner.Remove();
        document.Update();
        Assert.False(inner.IsHovered);
        Assert.Equal(outer, document.Input.Hovered);
        Assert.Equal(["outer"], entered);
    }
}

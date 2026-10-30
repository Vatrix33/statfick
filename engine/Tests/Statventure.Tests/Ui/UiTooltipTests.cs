using System.Numerics;
using Statventure.Platform;
using Statventure.Tests.Canvas;
using Statventure.UI.Dom;

namespace Statventure.Tests.Ui;

/// <summary>
/// Title tooltips as Chromium 152 shows them (Blink's ChromeClient and views' TooltipController,
/// read from the source: the browser shows them in a window of its own, which the harness can't
/// capture): the text, when they come and go, and where they go.
/// </summary>
public class UiTooltipTests
{
    const string Css = """
        body { margin: 0; }
        div { position: absolute; width: 100px; height: 50px; }
        """;

    sealed class Page(UiDocument document)
    {
        public readonly UiDocument Document = document;
        public double Time;
        public UiInput Input => Document.Input;
        public string? Text => Input.Tooltip?.TextContent;

        public void Run(double seconds)
        {
            var end = Time + seconds;
            while (Time < end - 1e-9)
            {
                Time = Math.Min(end, Time + 1 / 60.0);
                Document.Update(Time);
            }
        }

        public void Move(float x, float y)
        {
            Input.PointerMove(new Vector2(x, y));
            Run(1 / 60.0);
        }
    }

    static Page Load(string html, int width = 400, int height = 300)
    {
        var document = new UiDocument(TestFonts.Library);
        document.SetViewport(width, height);
        document.AddStylesheet(Css);
        Markup.SetInnerHtml(document.Body, html);
        document.Update(0);
        return new Page(document);
    }

    [Fact]
    public void ATitleShowsHalfASecondAfterThePointerRests()
    {
        var page = Load("""<div id="a" title="Hello" style="left: 0; top: 0"><span>text</span></div>""");
        page.Move(20, 20);
        page.Run(0.3);
        // Moving within it before then starts the wait again.
        page.Move(30, 25);
        page.Run(0.4);
        Assert.Null(page.Text);
        page.Run(0.15);
        Assert.Equal("Hello", page.Text);
        // At the pointer where it came to rest, 10 px right and 15 px down.
        var box = page.Input.Tooltip!.Box!.ViewportRect;
        Assert.Equal(new Vector2(40, 40), new Vector2(box.Left, box.Top));
        // Moving within the element keeps it where it is.
        page.Move(50, 30);
        Assert.Equal("Hello", page.Text);
        Assert.Equal(40, page.Input.Tooltip!.Box!.ViewportRect.Left);
    }

    [Fact]
    public void TheNearestTitleCountsAndAnEmptyOneSaysNone()
    {
        var page = Load("""
            <section title="Outer">
              <div id="a" style="left: 0; top: 0"><b>inner</b></div>
              <div id="b" title="" style="left: 150px; top: 0"></div>
            </section>
            """);
        page.Move(20, 20);
        page.Run(0.6);
        Assert.Equal("Outer", page.Text);
        page.Move(170, 20);
        page.Run(0.6);
        Assert.Null(page.Text);
    }

    [Fact]
    public void AnotherTitleReplacesItAfterTheDelay()
    {
        var page = Load("""<div title="One" style="left: 0; top: 0"></div><div title="Two" style="left: 150px; top: 0"></div>""");
        page.Move(20, 20);
        page.Run(0.6);
        Assert.Equal("One", page.Text);
        page.Move(170, 20);
        Assert.Null(page.Text);
        page.Run(0.6);
        Assert.Equal("Two", page.Text);
    }

    [Fact]
    public void APressHidesItUntilTheTextUnderThePointerChanges()
    {
        var page = Load("""<div title="One" style="left: 0; top: 0"></div><div title="Two" style="left: 150px; top: 0"></div>""");
        page.Move(20, 20);
        page.Run(0.6);
        page.Input.PointerDown(new Vector2(20, 20), MouseButton.Left);
        page.Input.PointerUp(new Vector2(20, 20), MouseButton.Left);
        Assert.Null(page.Text);
        page.Move(25, 25);
        page.Run(0.6);
        Assert.Null(page.Text);
        page.Move(170, 20);
        page.Run(0.6);
        Assert.Equal("Two", page.Text);
        page.Move(20, 20);
        page.Run(0.6);
        Assert.Equal("One", page.Text);
    }

    [Fact]
    public void KeysAndTheWheelHideIt()
    {
        var page = Load("""<div title="One" style="left: 0; top: 0"></div><div title="Two" style="left: 150px; top: 0"></div>""");
        page.Move(20, 20);
        page.Run(0.6);
        page.Input.KeyDown("a", "KeyA");
        page.Input.KeyUp("a", "KeyA");
        Assert.Null(page.Text);
        // The same text doesn't come back; another one does.
        page.Move(30, 30);
        page.Run(0.6);
        Assert.Null(page.Text);
        page.Move(170, 20);
        page.Run(0.6);
        Assert.Equal("Two", page.Text);
        page.Input.Wheel(new Vector2(170, 20), new Vector2(0, 120));
        Assert.Null(page.Text);
    }

    [Fact]
    public void LeavingTheWindowHidesIt()
    {
        var page = Load("""<div title="One" style="left: 0; top: 0"></div>""");
        page.Move(20, 20);
        page.Run(0.6);
        page.Input.PointerLeave();
        Assert.Null(page.Text);
        // (Coming back to the very same point passes nothing on: Blink's last point and text are the same.)
        page.Move(25, 25);
        page.Run(0.6);
        Assert.Equal("One", page.Text);
    }

    [Fact]
    public void AnInvalidControlShowsItsValidationMessage()
    {
        var page = Load("""
            <input required style="position: absolute; left: 0; top: 0; width: 100px">
            <input required title="Your name" style="position: absolute; left: 150px; top: 0; width: 100px">
            <form novalidate><input required style="position: absolute; left: 0; top: 100px; width: 100px"></form>
            <input pattern="[a-z]+" value="123" style="position: absolute; left: 150px; top: 100px; width: 100px">
            """);
        page.Move(20, 10);
        page.Run(0.6);
        Assert.Equal("Please fill out this field.", page.Text);
        page.Move(170, 10);
        page.Run(0.6);
        Assert.Equal("Your name", page.Text);
        page.Move(20, 110);
        page.Run(0.6);
        Assert.Null(page.Text);
        // Only the first line of the message.
        page.Move(170, 110);
        page.Run(0.6);
        Assert.Equal("Please match the requested format.", page.Text);
    }

    [Fact]
    public void AnOpenSelectListShowsItsOptionsTitles()
    {
        var page = Load("""
            <select title="Pick" style="position: absolute; left: 0; top: 0; width: 120px">
              <option title="First one">a</option><option>b</option><optgroup label="G"><option>c</option></optgroup>
            </select>
            """);
        page.Move(20, 10);
        page.Run(0.6);
        Assert.Equal("Pick", page.Text);
        page.Input.PointerDown(new Vector2(20, 10), MouseButton.Left);
        page.Input.PointerUp(new Vector2(20, 10), MouseButton.Left);
        page.Run(0.1);
        var rows = page.Input.SelectPopup!.Descendants().Where(e => e.Tag == "sv-select-option").Select(e => e.Box!.ViewportRect).ToList();
        // The list is a page of its own: the select's title isn't its options'.
        foreach (var (row, text) in rows.Zip(new string?[] { "First one", null, null }))
        {
            page.Move(row.Left + 20, row.Top + row.Height / 2);
            page.Run(0.6);
            Assert.Equal(text, page.Text);
        }
    }

    [Fact]
    public void ItStaysInTheWindow()
    {
        var page = Load("""<div title="A tooltip with some words" style="left: 0; top: 0; width: 400px; height: 300px"></div>""");
        page.Move(390, 290);
        page.Run(0.6);
        var box = page.Input.Tooltip!.Box!.ViewportRect;
        // Shifted left to the window's edge, and above the pointer.
        Assert.Equal(400, box.Right, 1f);
        Assert.Equal(290, box.Bottom, 1f);
    }

    [Fact]
    public void ItsTextIsTrimmedWithTabsAsSpaces()
    {
        var page = Load("<div title=\"  a\tb  \" style=\"left: 0; top: 0\"></div><div title=\"   \" style=\"left: 150px; top: 0\"></div>");
        page.Move(20, 20);
        page.Run(0.6);
        Assert.Equal("a        b", page.Text);
        page.Move(170, 20);
        page.Run(0.6);
        Assert.Null(page.Text);
    }
}

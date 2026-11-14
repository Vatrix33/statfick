using System.Numerics;
using Statventure.Platform;
using Statventure.Tests.Canvas;
using Statventure.UI.Dom;

namespace Statventure.Tests.Ui;

/// <summary>
/// An open select's list takes the select's own font at any zoom (Blink gives the list picker the select's style): its
/// size the select's in CSS px, so at a device scale or a page zoom other than 1 the list's text and rows are the zoomed
/// size once, as the select's (GM1 step 8 found them zoomed twice: the device-pixel size written back as CSS px).
/// </summary>
public class UiSelectListZoomTests
{
    const string Css = """
        body { margin: 0; }
        select { position: absolute; left: 10px; top: 10px; width: 160px; font: 14px Lato; }
        """;

    /// <summary>A select opened by a click, at <paramref name="deviceScale"/> and <paramref name="pageZoom"/>: the list, the select's style and the rows' heights (device pixels).</summary>
    static (UiElement Popup, UiElement Select, float[] Rows) Open(float deviceScale, float pageZoom)
    {
        var document = new UiDocument(TestFonts.Library);
        document.SetViewport(400, 300);
        document.SetZoom(deviceScale, pageZoom);
        document.AddStylesheet(Css);
        Markup.SetInnerHtml(document.Body, "<select><option>First</option><option>Second</option><option>Third</option></select>");
        document.Update(0);
        var select = document.QuerySelector("select")!;
        var middle = new Vector2(50, 20) * pageZoom;
        document.Input.PointerDown(middle, MouseButton.Left);
        document.Input.PointerUp(middle, MouseButton.Left);
        document.Update(0.1);
        var popup = document.Input.SelectPopup!;
        var rows = popup.Descendants().Where(e => e.Tag == "sv-select-option").Select(e => e.Box!.ViewportRect.Height).ToArray();
        return (popup, select, rows);
    }

    [Theory]
    [InlineData(1f, 1f)]
    [InlineData(1.65f, 1f)]
    [InlineData(1f, 1.44f)]
    [InlineData(1.65f, 1.44f)]
    public void TheListHasTheSelectsFontSizeZoomedOnce(float deviceScale, float pageZoom)
    {
        var (popup, select, rows) = Open(deviceScale, pageZoom);
        Assert.Equal("14px", popup.GetStyle("font-size"));
        Assert.Equal(select.Style!.SpecifiedFontSize, popup.Style!.SpecifiedFontSize);
        Assert.Equal(select.Style.FontSize, popup.Style.FontSize);
        Assert.Equal(14 * deviceScale * pageZoom, popup.Style.FontSize, 3);
        // The rows as at 1, zoomed once (within the layout's rounding).
        var (_, _, plain) = Open(1, 1);
        Assert.Equal(3, rows.Length);
        foreach (var (row, at1) in rows.Zip(plain))
            Assert.InRange(row, at1 * deviceScale * pageZoom - 1, at1 * deviceScale * pageZoom + 1);
    }
}

using System.Numerics;
using Statventure.Platform;

namespace Statventure.Tests.Editor.Shell;

/// <summary>
/// The splitters between the panes, dragged with the pointer: the side pane follows the pointer in whole CSS px
/// (at any device scale and page zoom), within the panes' narrowest widths; pointer capture holds the drag
/// until the release; the press moves neither the focus nor a selection.
/// </summary>
public class SplitterTests
{
    static float Width(EditorHarness editor, string pane) => editor.Rect($"[data-pane={pane}]").Width;

    static Vector2 SplitterAt(EditorHarness editor, string splitter, float dy = 0) => editor.Middle(editor.Q($"[data-splitter={splitter}]")) + new Vector2(0, dy);

    [Fact]
    public void TheLeftSplitterSizesTheEntryListAndTheRightOneThePreview()
    {
        using var editor = new EditorHarness(1000, 640);
        var left = SplitterAt(editor, "entries");
        editor.Drag(left, left + new Vector2(20, 0), left + new Vector2(50.4f, 30));
        Assert.Equal(330, Width(editor, "entries"));
        Assert.Equal(360, Width(editor, "preview"));
        Assert.Equal("width: 330px;", editor.Q("[data-pane=entries]").InlineStyle);
        var right = SplitterAt(editor, "preview");
        editor.Drag(right, right - new Vector2(40, 0));
        Assert.Equal(400, Width(editor, "preview"));
        Assert.Equal(330, Width(editor, "entries"));
        Assert.Equal(1000 - 330 - 400 - 8, Width(editor, "form"));
    }

    [Fact]
    public void ThePanesKeepTheirNarrowestWidths()
    {
        using var editor = new EditorHarness(1000, 640);
        var left = SplitterAt(editor, "entries");
        editor.Drag(left, left - new Vector2(400, 0));
        Assert.Equal(120, Width(editor, "entries"));
        // The splitter sets that width itself (the pane's min-width alone would hide a narrower one).
        Assert.Equal("width: 120px;", editor.Q("[data-pane=entries]").InlineStyle);
        left = SplitterAt(editor, "entries");
        editor.Drag(left, left + new Vector2(900, 0));
        // The middle pane keeps 200 px: 1000 - 360 - 8 - 200.
        Assert.Equal(432, Width(editor, "entries"));
        Assert.Equal(200, Width(editor, "form"));
        var right = SplitterAt(editor, "preview");
        editor.Drag(right, right + new Vector2(900, 0));
        Assert.Equal(120, Width(editor, "preview"));
        right = SplitterAt(editor, "preview");
        editor.Drag(right, right - new Vector2(900, 0));
        Assert.Equal(1000 - 432 - 8 - 200, Width(editor, "preview"));
    }

    [Theory]
    [InlineData(1.65f, 1f)]
    [InlineData(1f, 1.2f)]
    public void TheDragIsInCssPixelsAtAnyScaleAndZoom(float scale, float zoom)
    {
        // A window as wide in CSS px as the others.
        using var editor = new EditorHarness(1000 * zoom, 640 * zoom, scale);
        editor.Document.SetZoom(scale, zoom);
        editor.Frame();
        var left = SplitterAt(editor, "entries");
        editor.Drag(left, left + new Vector2(60 * zoom, 0));
        Assert.Equal(340, Width(editor, "entries"));
    }

    [Fact]
    public void TheCaptureHoldsTheDragUntilTheRelease()
    {
        using var editor = new EditorHarness(1000, 640);
        var splitter = editor.Q("[data-splitter=entries]");
        var left = SplitterAt(editor, "entries");
        editor.Input.PointerMove(left);
        editor.Input.PointerDown(left, MouseButton.Left);
        Assert.True(splitter.HasClass("dragging"));
        Assert.True(editor.View.Shell.HasClass("resizing"));
        // Far from the splitter, over another pane: still the drag.
        editor.Input.PointerMove(left + new Vector2(100, 200));
        Assert.Equal(380, Width(editor, "entries"));
        editor.Input.PointerUp(left + new Vector2(100, 200), MouseButton.Left);
        editor.Frame();
        Assert.False(splitter.HasClass("dragging"));
        Assert.False(editor.View.Shell.HasClass("resizing"));
        // Moves after the release resize nothing, over the splitter or not.
        editor.Input.PointerMove(SplitterAt(editor, "entries"));
        editor.Input.PointerMove(SplitterAt(editor, "entries") + new Vector2(-50, 0));
        Assert.Equal(380, Width(editor, "entries"));
    }

    [Fact]
    public void OnlyTheMainButtonDragsAndThePressKeepsTheFocus()
    {
        using var editor = new EditorHarness(1000, 640, parts: typeof(RecordingPart));
        var field = editor.View.Part<RecordingPart>().Field;
        field.Focus();
        var left = SplitterAt(editor, "entries");
        editor.Input.PointerMove(left);
        editor.Input.PointerDown(left, MouseButton.Right);
        editor.Input.PointerMove(left + new Vector2(50, 0));
        editor.Input.PointerUp(left + new Vector2(50, 0), MouseButton.Right);
        editor.Frame();
        Assert.Equal(280, Width(editor, "entries"));
        Assert.False(editor.Q("[data-splitter=entries]").HasClass("dragging"));
        editor.Drag(left, left + new Vector2(50, 0));
        Assert.Equal(330, Width(editor, "entries"));
        Assert.Same(field, editor.Document.FocusedElement);
        Assert.Equal("", editor.Document.PageSelectionText());
    }

    [Fact]
    public void LosingTheCaptureEndsTheDrag()
    {
        using var editor = new EditorHarness(1000, 640);
        var splitter = editor.Q("[data-splitter=preview]");
        var right = SplitterAt(editor, "preview");
        editor.Input.PointerMove(right);
        editor.Input.PointerDown(right, MouseButton.Left);
        editor.Input.PointerMove(right - new Vector2(10, 0));
        Assert.Equal(370, Width(editor, "preview"));
        // The window loses the focus: the capture goes, and with it the drag.
        editor.Input.WindowBlur();
        Assert.False(splitter.HasClass("dragging"));
        editor.Input.WindowFocus();
        editor.Input.PointerMove(right - new Vector2(60, 0));
        Assert.Equal(370, Width(editor, "preview"));
    }
}

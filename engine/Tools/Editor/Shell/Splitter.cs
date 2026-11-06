using Statventure.UI.Dom;

namespace Statventure.Editor.Shell;

/// <summary>
/// A splitter between two panes, dragged with the pointer: it takes the pointer's capture on the main button's
/// press (cancelling the press, so focus and the selection stay where they are) and sets the side pane's width
/// as the pointer moves, in whole CSS px, the side pane at least <see cref="EditorView.MinSideWidth"/> and the
/// middle pane at least <see cref="EditorView.MinFormWidth"/>; losing the capture (the release) ends the drag. The left splitter
/// sizes the entry list, the right one the preview.
/// </summary>
sealed class Splitter
{
    readonly EditorView _view;
    readonly UiElement _element;
    readonly UiElement _pane;
    readonly bool _right;
    float _startX, _startWidth;
    bool _dragging;

    public Splitter(EditorView view, UiElement element)
    {
        _view = view;
        _element = element;
        _right = element.GetAttribute("data-splitter") == "preview";
        _pane = view.Pane(_right ? EditorPane.Preview : EditorPane.Entries);
        element.AddEventListener<UiPointerEvent>("pointerdown", Down);
        element.AddEventListener<UiPointerEvent>("pointermove", Move);
        // The capture ends with the release (lostpointercapture after pointerup) or when the window loses the focus.
        element.AddEventListener("lostpointercapture", _ => End());
    }

    void Down(UiPointerEvent e)
    {
        if (e.Button != 0)
            return;
        e.PreventDefault();
        _view.Document.Input.SetPointerCapture(_element);
        _startX = e.ClientX;
        _startWidth = _pane.GetBoundingClientRect().Width;
        _dragging = true;
        _element.AddClass("dragging");
        _view.Shell.AddClass("resizing");
    }

    void Move(UiPointerEvent e)
    {
        if (!_dragging)
            return;
        var moved = e.ClientX - _startX;
        var width = _startWidth + (_right ? -moved : moved);
        var other = _view.Pane(_right ? EditorPane.Entries : EditorPane.Preview).GetBoundingClientRect().Width;
        var splitters = _element.Parent!.ElementChildren.Where(c => c.HasClass("splitter")).Sum(s => s.GetBoundingClientRect().Width);
        var room = _element.Parent!.GetBoundingClientRect().Width - other - splitters - EditorView.MinFormWidth;
        width = MathF.Round(Math.Clamp(width, EditorView.MinSideWidth, Math.Max(room, EditorView.MinSideWidth)));
        _pane.SetStyle("width", $"{width}px");
    }

    void End()
    {
        if (!_dragging)
            return;
        _dragging = false;
        _element.RemoveClass("dragging");
        _view.Shell.RemoveClass("resizing");
    }
}

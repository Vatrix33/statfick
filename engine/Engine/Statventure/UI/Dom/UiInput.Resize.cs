using System.Globalization;
using System.Numerics;
using Statventure.UI.Layout;
using Statventure.UI.Style;

namespace Statventure.UI.Dom;

/// <summary>
/// Resizing a box by its resizer (EventHandler and PaintLayerScrollableArea::Resize): a press on
/// the resizer that the page doesn't cancel starts resizing instead of focusing or selecting;
/// moves with a button down set the box's width and height in its style attribute (whole CSS
/// pixels, never smaller than the resizer), keeping the corner the same distance from the pointer as at
/// the press; the release ends it. Over the resizer the cursor shows the directions it resizes.
/// </summary>
public sealed partial class UiInput
{
    UiElement? _resizing;
    Vector2 _resizeOffset;

    /// <summary>
    /// The element whose resizer is under a point (the hit box's enclosing layer's, as Blink asks
    /// IsAbsolutePointInResizeControl), or null.
    /// </summary>
    UiElement? ResizerAt(HitResult? hit, Vector2 position)
    {
        for (var box = hit?.Box; box is not null; box = box.Parent)
        {
            var s = box.Style;
            if (!(box.IsScrollContainer || s.Position != PositionKind.Static || s.CreatesStackingContext))
                continue;
            if (box.Element is not { } element || s.Visibility != Visibility.Visible)
                return null;
            var local = LocalPoint(element, Floored(position));
            var point = new Vector2(MathF.Round(local.X, MidpointRounding.AwayFromZero), MathF.Round(local.Y, MidpointRounding.AwayFromZero));
            var resizer = ScrollbarGeometry.ResizerOf(box, Paint.UiPainter.SnapRect(box.ViewportRect, 1), _document.Layout.Context.Scrollbar);
            return ScrollbarGeometry.Contains(resizer, point) ? element : null;
        }
        return null;
    }

    /// <summary>InitializeResizeTransform: the size the pointer gives is its point in the box plus how far the corner was from the press.</summary>
    void StartResize(UiElement element, Vector2 position)
    {
        if (element.Box is not { } box)
            return;
        _resizing = element;
        var local = LocalPoint(element, Floored(position)) - box.ViewportPosition;
        var start = new Vector2(MathF.Round(local.X, MidpointRounding.AwayFromZero), MathF.Round(local.Y, MidpointRounding.AwayFromZero));
        _resizeOffset = new Vector2(box.Width, box.Height) - start;
    }

    /// <summary>
    /// PaintLayerScrollableArea::Resize at a pointer position: sizes in CSS px (the box's size,
    /// the pointer's and the resizer's over the zoom, as Blink's LayoutUnits), the style's in
    /// whole CSS px.
    /// </summary>
    void Resize(Vector2 position)
    {
        if (_resizing is not { Box: { } box } element || !ScrollbarGeometry.CanResize(box))
            return;
        var s = box.Style;
        var zoom = s.Zoom;
        var current = new Vector2(LayoutUnit.Truncate(box.Width * (1 / zoom)), LayoutUnit.Truncate(box.Height * (1 / zoom)));
        var local = LocalPoint(element, Floored(position)) - box.ViewportPosition + _resizeOffset;
        var point = new Vector2(MathF.Round(local.X, MidpointRounding.AwayFromZero), MathF.Round(local.Y, MidpointRounding.AwayFromZero));
        var corner = ScrollbarGeometry.ResizerOf(box, Paint.UiPainter.SnapRect(box.ViewportRect, 1), _document.Layout.Context.Scrollbar);
        var width = MathF.Max((int)(point.X / zoom), LayoutUnit.Truncate(corner.Width / zoom));
        var height = MathF.Max((int)(point.Y / zoom), LayoutUnit.Truncate(corner.Height / zoom));
        var difference = new Vector2(width - current.X, height - current.Y);
        var borderBox = s.BoxSizing == BoxSizing.BorderBox;
        // UsedResize: block and inline are vertical and horizontal in horizontal text.
        var resize = s.Resize switch { ResizeKind.Block => ResizeKind.Vertical, ResizeKind.Inline => ResizeKind.Horizontal, var r => r };
        if (resize != ResizeKind.Vertical && difference.X != 0)
        {
            var baseWidth = LayoutUnit.Truncate((box.Width - (borderBox ? 0 : box.Border.Horizontal + box.Padding.Horizontal)) / zoom);
            element.SetStyle("width", Pixels(baseWidth + difference.X));
        }
        if (resize != ResizeKind.Horizontal && difference.Y != 0)
        {
            var baseHeight = LayoutUnit.Truncate((box.Height - (borderBox ? 0 : box.Border.Vertical + box.Padding.Vertical)) / zoom);
            element.SetStyle("height", Pixels(baseHeight + difference.Y));
        }
        _document.UpdateLayout();
    }

    /// <summary>The pointer as Blink's EventHandler gives it to resizing: on whole device pixels (floored).</summary>
    static Vector2 Floored(Vector2 position) => new(MathF.Floor(position.X), MathF.Floor(position.Y));

    static string Pixels(float value) => MathF.Round(value, MidpointRounding.AwayFromZero).ToString(CultureInfo.InvariantCulture) + "px";

    /// <summary>The cursor over a resizer: the directions it resizes (south-east for both).</summary>
    static string ResizeCursor(UiElement element) => element.Style?.Resize switch
    {
        ResizeKind.Vertical or ResizeKind.Block => "ns-resize",
        ResizeKind.Horizontal or ResizeKind.Inline => "ew-resize",
        ResizeKind.Both => "se-resize",
        _ => "default",
    };
}

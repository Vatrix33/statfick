using System.Numerics;
using Statventure.Graphics;
using Statventure.Render3D;
using Statventure.UI.Dom;

namespace Statventure.UI;

/// <summary>
/// The 3D views a document shows (Docs/Renderer-3D.md, "3D views in the UI"): each in a
/// <c>surface</c> element, a replaced element the UI lays out, clips and layers like an image, the
/// view's picture filling its content box. Each frame, before painting, every shown view whose
/// element is laid out renders if it changed, at its content box's size in device pixels and the
/// document's zoom (the ink lines' scale). Picking maps a point of the viewport through the
/// element's transforms into the view's picture.
/// </summary>
public sealed class SurfaceViews(UiDocument document)
{
    readonly Dictionary<UiElement, View3D> _views = new(ReferenceEqualityComparer.Instance);

    /// <summary>Shows <paramref name="view"/> in <paramref name="surface"/> (a <c>surface</c> element) in place of what it showed.</summary>
    public void Show(UiElement surface, View3D view) => _views[surface] = view;

    /// <summary>Stops showing a view in <paramref name="surface"/> (the view stays the caller's).</summary>
    public void Hide(UiElement surface) => _views.Remove(surface);

    /// <summary>The view shown in <paramref name="surface"/>, if any.</summary>
    public View3D? ViewOf(UiElement surface) => _views.GetValueOrDefault(surface);

    /// <summary>The picture a surface element shows (for <see cref="Paint.UiPainter.SurfaceTexture"/>).</summary>
    public int? Texture(UiElement element) => _views.TryGetValue(element, out var view) ? view.Texture : null;

    /// <summary>Renders the views whose elements are laid out and that changed (call after the document's update, before painting).</summary>
    public void Update(Frame frame)
    {
        foreach (var (element, view) in _views)
            if (element.Document == document && element.Box is { } box)
                view.Update(frame, (int)MathF.Round(box.ContentWidth), (int)MathF.Round(box.ContentHeight), document.Zoom);
    }

    /// <summary>
    /// What the view in <paramref name="surface"/> shows at <paramref name="point"/> (viewport device
    /// pixels, as <see cref="UiInput.PointerPosition"/>; a mouse event's client point times the
    /// document's zoom): null over its background, outside its content box, or without a view.
    /// </summary>
    public Pick3D? Pick(UiElement surface, Vector2 point)
    {
        if (!_views.TryGetValue(surface, out var view) || surface.Box is not { } box)
            return null;
        var local = Matrix3x2.Invert(UiDocument.TransformOf(box), out var inverse) ? Vector2.Transform(point, inverse) : point;
        var rect = box.ViewportRect;
        var origin = new Vector2(rect.X + box.ContentLeft, rect.Y + box.ContentTop);
        var size = new Vector2(box.ContentWidth, box.ContentHeight);
        var inside = local - origin;
        if (inside.X < 0 || inside.Y < 0 || inside.X >= size.X || inside.Y >= size.Y)
            return null;
        // The picture is the content box rounded to whole pixels, stretched over it.
        var pictureSize = new Vector2(MathF.Max(1, MathF.Round(size.X)), MathF.Max(1, MathF.Round(size.Y)));
        return view.Pick(inside * pictureSize / size);
    }
}

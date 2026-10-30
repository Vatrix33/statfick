using System.Numerics;
using Statventure.Canvas;
using Statventure.Core;
using Statventure.UI.Dom;
using Statventure.UI.Layout;
using Statventure.UI.Style;
using CanvasPath = Statventure.Canvas.Path;

namespace Statventure.UI.Paint;

/// <summary>
/// Outlines. A styled outline follows the border box, grown by its offset (the radii grown to
/// match). outline-style: auto is the focus ring, drawn as Blink's OutlinePainter draws it: two
/// strokes 2 px wide, white outside and the outline colour inside, around everything the box
/// shows (its border box and, unless it clips, its descendants' boxes and text), pulled 1 px
/// over a border, with rounded corners (a themed control's own radius, the author's radii, or 3 px).
/// </summary>
public sealed partial class UiPainter
{
    // ---------- Outlines ----------

    /// <summary>Blink's focus ring stroke: 3 px, split 2:1 between the outer and the inner ring.</summary>
    /// <summary>FocusRingStrokeWidth: 3 px times the zoom (floored, AdjustBorderWidthByZoom), never thinner than at zoom 1.</summary>
    static float FocusRingStrokeWidth(ComputedStyle s) => s.Zoom >= 1 ? ControlTheme.BorderWidthByZoom(3, s.Zoom) : 3;
    // The ring is two strokes in a 2:1 ratio.
    static float FocusRingOuterWidth(ComputedStyle s) => FocusRingStrokeWidth(s) / 3 * 2;
    static float FocusRingInnerWidth(ComputedStyle s) => FocusRingStrokeWidth(s) / 3;

    void PaintOutline(LayoutBox box)
    {
        var s = box.Style;
        if (box.Type is BoxType.Text or BoxType.LineBreak or BoxType.Inline || !HasPaintedOutline(box, s))
            return;
        var rect = box.ViewportRect;
        if (s.OutlineStyle == BorderLineStyle.Auto)
        {
            var rects = new List<RectF>();
            AddBoxOutlineRects(box, rects, IsAnchor(box));
            PaintFocusRing(box, rects, rect);
            return;
        }
        DrawOutline(s, rect, BorderRadii(box, rect.Size));
    }

    void PaintInlineOutline(LayoutBox root, LayoutBox inlineBox)
    {
        var s = inlineBox.Style;
        if (!HasPaintedOutline(inlineBox, s) || inlineBox.Fragments is not { } fragments)
            return;
        if (s.OutlineStyle == BorderLineStyle.Auto)
        {
            var rects = new List<RectF>();
            AddInlineBoxOutlineRects(root, inlineBox, rects, IsAnchor(inlineBox));
            if (rects.Count > 0)
                PaintFocusRing(inlineBox, rects, rects[0]);
            return;
        }
        var origin = root.ViewportPosition + InlineOffset(inlineBox, root);
        foreach (var fragment in fragments)
            DrawOutline(s, fragment.Rect.Offset(origin), CornerRadii.Zero);
    }

    void DrawOutline(ComputedStyle s, RectF rect, CornerRadii radii)
    {
        rect = Snap(rect);
        // Outlines follow the border radius, grown by the offset (as Chromium draws them).
        var grow = s.OutlineOffset + s.OutlineWidth;
        var outer = RectF.FromEdges(rect.Left - grow, rect.Top - grow, rect.Right + grow, rect.Bottom + grow);
        if (outer.Width <= 0 || outer.Height <= 0)
            return;
        var outerRadii = radii.IsZero ? CornerRadii.Zero : new CornerRadii(
            Grow(radii.TopLeft, grow), Grow(radii.TopRight, grow), Grow(radii.BottomRight, grow), Grow(radii.BottomLeft, grow));
        var color = s.OutlineColor ?? s.Color;
        _canvas.DrawBox(outer, null, outerRadii, new Border(s.OutlineWidth, color, ToCanvas(s.OutlineStyle)));
    }

    static Vector2 Grow(Vector2 radius, float by) => radius.X <= 0 && radius.Y <= 0 ? Vector2.Zero : Vector2.Max(Vector2.Zero, radius + new Vector2(by));

    /// <summary>
    /// Whether the box paints its outline (Blink's HasPaintedOutline): a visible outline; a focus
    /// ring except on a focused control with its native look that doesn't match :focus-visible.
    /// </summary>
    static bool HasPaintedOutline(LayoutBox box, ComputedStyle s)
    {
        if (s.OutlineStyle == BorderLineStyle.None || (s.OutlineWidth <= 0 && s.OutlineStyle != BorderLineStyle.Auto) || s.Visibility != Visibility.Visible)
            return false;
        if (s.OutlineStyle != BorderLineStyle.Auto || box.Pseudo != PseudoKind.None || box.Element is not { } element)
            return true;
        return !(element.IsFormControlElement && element.IsFocused && !element.FocusVisible && ControlAppearance.Effective(element, s) != ControlLook.None);
    }

    /// <summary>An a element's focus ring also takes in its out-of-flow descendants.</summary>
    static bool IsAnchor(LayoutBox box) => box.Pseudo == PseudoKind.None && box.Element?.Tag == "a";

    // ---------- What a focus ring goes around ----------

    /// <summary>
    /// Has a paint layer of its own (Blink's LayerTypeRequired), so its outline rects are mapped
    /// through its transform, and an inline box's own fragments count.
    /// </summary>
    static bool HasLayer(LayoutBox box)
    {
        var s = box.Style;
        if (box.Type == BoxType.Inline)
            return s.Position is PositionKind.Relative or PositionKind.Sticky || CreatesGroup(s);
        return s.Position != PositionKind.Static || s.CreatesStackingContext || box.IsScrollContainer;
    }

    /// <summary>Blink's HasGroupingProperty: opacity, filters, clip paths, isolation, blending, or overflow other than visible and clip.</summary>
    static bool CreatesGroup(ComputedStyle s) =>
        s.Opacity < 1 || s.Filters.Length > 0 || s.BackdropFilters.Length > 0 || s.ClipPath is not null || s.Isolate || s.MixBlendMode != CssBlendMode.Normal
        || s.OverflowX is not (Overflow.Visible or Overflow.Clip) || s.OverflowY is not (Overflow.Visible or Overflow.Clip);

    /// <summary>
    /// Whether an inline box has fragments of its own, rather than being culled (Blink's
    /// ShouldCreateBoxFragment): once it has had them, it keeps them.
    /// </summary>
    static bool HasBoxFragments(LayoutBox inline)
    {
        if (inline.KeepsBoxFragments)
            return true;
        var s = inline.Style;
        bool MayHave(Length top, Length right, Length bottom, Length left) =>
            !top.IsZero || !right.IsZero || !bottom.IsZero || !left.IsZero || top.IsAuto || right.IsAuto || bottom.IsAuto || left.IsAuto;
        var decorated = s.BackgroundColor.A > 0 || s.Backgrounds.Any(b => b.Image.Kind != ImageKind.None) || s.BorderTop > 0 || s.BorderRight > 0
            || s.BorderBottom > 0 || s.BorderLeft > 0 || s.BoxShadows.Length > 0;
        var creates = HasLayer(inline) || decorated || MayHave(s.PaddingTop, s.PaddingRight, s.PaddingBottom, s.PaddingLeft)
            || MayHave(s.MarginTop, s.MarginRight, s.MarginBottom, s.MarginLeft) || HasPaintedOutline(inline, s)
            || inline.Pseudo is PseudoKind.Before or PseudoKind.After;
        if (creates)
            inline.KeepsBoxFragments = true;
        return creates;
    }

    /// <summary>
    /// A block-level or atomic box's outline rects (PhysicalBoxFragment::AddOutlineRects with
    /// block ink overflow): its border box (not an anonymous block's), then, unless it clips its
    /// overflow, its in-flow descendants' (and an anchor's out-of-flow ones).
    /// </summary>
    void AddBoxOutlineRects(LayoutBox box, List<RectF> rects, bool anchor)
    {
        if (!box.IsAnonymous)
            rects.Add(box.ViewportRect);
        var s = box.Style;
        if (box.Type == BoxType.Replaced || s.OverflowX != Overflow.Visible || s.OverflowY != Overflow.Visible)
            return;
        if (box.Inline is { } inline)
            AddLineOutlineRects(box, inline, null, rects, anchor);
        else
            foreach (var child in box.Children)
                if (!child.IsOutOfFlow && child.Type is not (BoxType.Text or BoxType.LineBreak))
                    AddDescendantOutlineRects(child, rects, anchor, upgradeAnchor: true);
        if (anchor && box.PositionedDescendants is { } positioned)
            foreach (var child in positioned)
                AddDescendantOutlineRects(child, rects, anchor, upgradeAnchor: true);
    }

    /// <summary>
    /// A descendant box's rects; one with a layer of its own goes through its transform (to the
    /// bounding box). A descendant a element takes in its out-of-flow descendants too, unless it
    /// has a layer or comes through an inline box's children.
    /// </summary>
    void AddDescendantOutlineRects(LayoutBox box, List<RectF> rects, bool anchor, bool upgradeAnchor)
    {
        var start = rects.Count;
        var layer = HasLayer(box);
        AddBoxOutlineRects(box, rects, anchor || (upgradeAnchor && !layer && IsAnchor(box)));
        if (!layer || !box.Style.HasTransform)
            return;
        var transform = TransformFor(box, box.ViewportRect);
        for (var i = start; i < rects.Count; i++)
            rects[i] = rects[i].Transform(transform);
    }

    /// <summary>
    /// An inline box's own outline rects (AddOutlineRectsForInlineBox): its fragments with some
    /// size, then what's inside it on each line.
    /// </summary>
    void AddInlineBoxOutlineRects(LayoutBox root, LayoutBox inlineBox, List<RectF> rects, bool anchor)
    {
        if (inlineBox.Fragments is not { } fragments || root.Inline is not { } inline)
            return;
        var origin = root.ViewportPosition + InlineOffset(inlineBox, root);
        foreach (var fragment in fragments)
            if (fragment.Rect.Width != 0 || fragment.Rect.Height != 0)
                rects.Add(fragment.Rect.Offset(origin));
        AddLineOutlineRects(root, inline, inlineBox, rects, anchor);
        if (anchor && inlineBox.PositionedDescendants is { } positioned)
            foreach (var child in positioned)
                AddDescendantOutlineRects(child, rects, anchor, upgradeAnchor: true);
    }

    /// <summary>
    /// The rects of what's on an IFC's lines (AddOutlineRectsForCursor), within an inline box or
    /// the whole root: text, atomic inlines, and each unculled inline box once (an inline box with
    /// fragments stands for everything inside it).
    /// </summary>
    void AddLineOutlineRects(LayoutBox root, InlineContent inline, LayoutBox? within, List<RectF> rects, bool anchor)
    {
        var origin = root.ViewportPosition;
        HashSet<LayoutBox>? done = null;
        foreach (var line in inline.Lines)
            foreach (var item in line.Items)
            {
                var box = item.Kind == LineItemKind.Generated ? null : item.Box;
                // The outermost inline box with fragments that the item is in (or is), inside the context.
                LayoutBox? owner = null;
                var inContext = within is null;
                for (var b = box; b is not null && b != root; b = b.Parent)
                {
                    if (b == within)
                    {
                        inContext = true;
                        break;
                    }
                    if (b.Type == BoxType.Inline && HasBoxFragments(b))
                        owner = b;
                }
                if (!inContext || (box is null && within is not null))
                    continue;
                if (owner is not null)
                {
                    if ((done ??= []).Add(owner))
                        AddInlineDescendantOutlineRects(root, owner, rects, anchor);
                    continue;
                }
                switch (item.Kind)
                {
                    case LineItemKind.Text:
                    case LineItemKind.Generated:
                    {
                        var at = origin + (box is null ? Vector2.Zero : InlineOffset(box, root)) + new Vector2(item.X, item.Baseline);
                        rects.Add(new RectF(at.X, at.Y - item.Ascent, item.Width, item.Ascent + item.Descent));
                        break;
                    }
                    case LineItemKind.Atomic when box is not null && !box.IsOutOfFlow:
                        AddDescendantOutlineRects(box, rects, anchor, upgradeAnchor: true);
                        break;
                }
            }
    }

    /// <summary>
    /// An inline box inside the ring's box: with a layer, its own outline rects; otherwise only the
    /// boxes among its children (LayoutInline::AddOutlineRectsForNormalChildren, which skips text).
    /// </summary>
    void AddInlineDescendantOutlineRects(LayoutBox root, LayoutBox inlineBox, List<RectF> rects, bool anchor)
    {
        if (HasLayer(inlineBox))
        {
            AddInlineBoxOutlineRects(root, inlineBox, rects, anchor);
            return;
        }
        AddInlineChildrenOutlineRects(root, inlineBox, rects, anchor);
    }

    void AddInlineChildrenOutlineRects(LayoutBox root, LayoutBox inlineBox, List<RectF> rects, bool anchor)
    {
        foreach (var child in inlineBox.Children)
        {
            if (child.IsOutOfFlow || child.Type is BoxType.Text or BoxType.LineBreak)
                continue;
            if (child.Type != BoxType.Inline)
            {
                AddDescendantOutlineRects(child, rects, anchor, upgradeAnchor: false);
                continue;
            }
            if (HasLayer(child) && child.Fragments is { } fragments)
            {
                // LayoutInline::AddOutlineRects: its line box rects, then its children's boxes.
                var origin = root.ViewportPosition + InlineOffset(child, root);
                foreach (var fragment in fragments)
                    rects.Add(fragment.Rect.Offset(origin));
            }
            AddInlineChildrenOutlineRects(root, child, rects, anchor);
        }
    }

    // ---------- Painting the focus ring ----------

    /// <summary>
    /// Blink's PaintFocusRing: an outer white ring (offset by the inner ring's width) under an
    /// inner ring in the outline colour, both strokes of the outer ring's width.
    /// </summary>
    void PaintFocusRing(LayoutBox box, List<RectF> outlineRects, RectF reference)
    {
        var s = box.Style;
        var rects = new List<RectF>();
        foreach (var rect in outlineRects)
        {
            var snapped = Snap(rect);
            if (!snapped.IsEmpty)
                rects.Add(snapped);
        }
        if (rects.Count == 0)
            return;
        // FocusRingOffset: the ring takes a (zoomed) pixel from a border at least that wide on every side.
        var offset = s.OutlineOffset;
        var insideBorder = ControlTheme.BorderWidthByZoom(1, s.Zoom);
        if (MathF.Min(MathF.Min(s.BorderTop, s.BorderBottom), MathF.Min(s.BorderLeft, s.BorderRight)) >= insideBorder)
            offset -= insideBorder;
        var radii = FocusRingRadii(box, s, Snap(reference));
        PaintSingleFocusRing(rects, FocusRingOuterWidth(s), offset + MathF.Ceiling(FocusRingInnerWidth(s)), radii, Color.White);
        PaintSingleFocusRing(rects, FocusRingOuterWidth(s), offset, radii, s.OutlineColor ?? s.Color);
    }

    /// <summary>
    /// The ring's corner radii (GetFocusRingCornerRadii): the author's border radii grown by the
    /// outline offset (at least the default); a themed control's own radius; otherwise the stroke width.
    /// </summary>
    CornerRadii FocusRingRadii(LayoutBox box, ComputedStyle s, RectF reference)
    {
        if (s.HasBorderRadius && s.HasAuthorBorderRadius)
        {
            var borderRadii = BorderRadii(box, reference.Size);
            var offset = s.OutlineOffset;
            var grown = new CornerRadii(Grow(borderRadii.TopLeft, offset), Grow(borderRadii.TopRight, offset), Grow(borderRadii.BottomRight, offset), Grow(borderRadii.BottomLeft, offset))
                .Fit(reference.Inflate(offset).Size);
            var minimum = new Vector2(FocusRingStrokeWidth(s));
            return new CornerRadii(Vector2.Max(grown.TopLeft, minimum), Vector2.Max(grown.TopRight, minimum), Vector2.Max(grown.BottomRight, minimum), Vector2.Max(grown.BottomLeft, minimum));
        }
        if (!s.HasAuthorBorder && box.Pseudo == PseudoKind.None && box.Element is { } element)
        {
            // NativeThemeBase::GetBorderRadiusForPart, zoomed as AdjustBorderRadiusByZoom zooms it.
            float? radius = ControlAppearance.Effective(element, s) switch
            {
                ControlLook.Checkbox or ControlLook.PushButton or ControlLook.Button or ControlLook.TextField or ControlLook.TextArea or ControlLook.SearchField => ControlTheme.BorderWidthByZoom(2, s.Zoom),
                ControlLook.Radio => MathF.Max(reference.Width, reference.Height) * 0.5f,
                _ => null,
            };
            if (radius is { } native)
                return new CornerRadii(native);
        }
        return new CornerRadii(FocusRingStrokeWidth(s));
    }

    /// <summary>
    /// One ring (PaintSingleFocusRing): a stroke along the outer edge of the rects' union, each rect
    /// grown by the offset (a negative one no further than to its middle). A single rectangle
    /// takes the radii as a rounded rectangle; another shape rounds its corners.
    /// </summary>
    void PaintSingleFocusRing(List<RectF> rects, float width, float offset, CornerRadii radii, Color color)
    {
        var scale = _canvas.Scale;
        var grown = new List<RectF>(rects.Count);
        foreach (var rect in rects)
        {
            // AdjustedOutlineOffset: halves in whole device pixels.
            var vertical = MathF.Max(offset, -MathF.Truncate(rect.Height * scale / 2) / scale);
            var horizontal = MathF.Max(offset, -MathF.Truncate(rect.Width * scale / 2) / scale);
            grown.Add(rect.Inflate(horizontal, vertical, horizontal, vertical));
        }
        var contours = RegionBoundary(grown);
        if (contours.Count == 0)
            return;
        if (contours is [{ Count: 4 } only] && IsRectangle(only))
        {
            var rect = RectF.FromEdges(only.Min(p => p.X), only.Min(p => p.Y), only.Max(p => p.X), only.Max(p => p.Y));
            // A stroke centred on the rounded rectangle: a border half the width outside it.
            var fitted = radii.Fit(rect.Size);
            var half = width / 2;
            var outer = new CornerRadii(Grow(fitted.TopLeft, half), Grow(fitted.TopRight, half), Grow(fitted.BottomRight, half), Grow(fitted.BottomLeft, half));
            _canvas.DrawBox(rect.Inflate(half), null, outer, new Border(width, color));
            return;
        }
        var path = new CanvasPath();
        var uniform = radii.TopLeft.X;
        if (new[] { radii.TopLeft, radii.TopRight, radii.BottomRight, radii.BottomLeft }.All(r => r.X == uniform && r.Y == uniform))
            foreach (var contour in contours)
                AddCornerEffectContour(path, contour, uniform);
        else
            foreach (var contour in contours)
                AddRoundedContour(path, contour, radii);
        _canvas.StrokePath(path, Brush.Solid(color), new StrokeStyle(width));
    }

    static bool IsRectangle(List<Vector2> points) =>
        points.Select(p => p.X).Distinct().Count() == 2 && points.Select(p => p.Y).Distinct().Count() == 2;

    /// <summary>
    /// The outline of the union of rectangles (SkRegion::getBoundaryPath): closed contours with
    /// the inside on the right (clockwise around the outside, the other way around holes),
    /// corners only.
    /// </summary>
    internal static List<List<Vector2>> RegionBoundary(List<RectF> rects)
    {
        var xs = rects.SelectMany(r => new[] { r.Left, r.Right }).Distinct().Order().ToArray();
        var ys = rects.SelectMany(r => new[] { r.Top, r.Bottom }).Distinct().Order().ToArray();
        var covered = new bool[Math.Max(0, xs.Length - 1), Math.Max(0, ys.Length - 1)];
        foreach (var r in rects)
        {
            if (r.IsEmpty)
                continue;
            var x0 = Array.BinarySearch(xs, r.Left);
            var x1 = Array.BinarySearch(xs, r.Right);
            var y0 = Array.BinarySearch(ys, r.Top);
            var y1 = Array.BinarySearch(ys, r.Bottom);
            for (var i = x0; i < x1; i++)
                for (var j = y0; j < y1; j++)
                    covered[i, j] = true;
        }
        bool Covered(int i, int j) => i >= 0 && j >= 0 && i < covered.GetLength(0) && j < covered.GetLength(1) && covered[i, j];
        // Directed unit edges between grid points, the covered cell on their right.
        var edges = new Dictionary<(int, int), List<(int, int)>>();
        void Edge((int, int) from, (int, int) to)
        {
            if (!edges.TryGetValue(from, out var list))
                edges[from] = list = [];
            list.Add(to);
        }
        for (var i = 0; i < covered.GetLength(0); i++)
            for (var j = 0; j < covered.GetLength(1); j++)
            {
                if (!covered[i, j])
                    continue;
                if (!Covered(i, j - 1))
                    Edge((i, j), (i + 1, j));
                if (!Covered(i + 1, j))
                    Edge((i + 1, j), (i + 1, j + 1));
                if (!Covered(i, j + 1))
                    Edge((i + 1, j + 1), (i, j + 1));
                if (!Covered(i - 1, j))
                    Edge((i, j + 1), (i, j));
            }
        var contours = new List<List<Vector2>>();
        while (edges.Count > 0)
        {
            var start = edges.Keys.OrderBy(k => k.Item2).ThenBy(k => k.Item1).First();
            var points = new List<(int X, int Y)>();
            var at = start;
            (int, int)? previous = null;
            do
            {
                points.Add(at);
                var outgoing = edges[at];
                // Where two contours touch at a corner, keep to the one coming in (turn right).
                var next = outgoing[0];
                if (outgoing.Count > 1 && previous is { } p)
                {
                    var (dx, dy) = (at.Item1 - p.Item1, at.Item2 - p.Item2);
                    next = outgoing.FirstOrDefault(o => (o.Item1 - at.Item1, o.Item2 - at.Item2) == (-dy, dx), outgoing[0]);
                }
                outgoing.Remove(next);
                if (outgoing.Count == 0)
                    edges.Remove(at);
                previous = at;
                at = next;
            }
            while (at != start);
            // Corners only: drop points in the middle of a straight run.
            var corners = new List<Vector2>();
            for (var k = 0; k < points.Count; k++)
            {
                var before = points[(k + points.Count - 1) % points.Count];
                var point = points[k];
                var after = points[(k + 1) % points.Count];
                var straight = (before.X == point.X && point.X == after.X) || (before.Y == point.Y && point.Y == after.Y);
                if (!straight)
                    corners.Add(new Vector2(xs[point.X], ys[point.Y]));
            }
            if (corners.Count >= 4)
                contours.Add(corners);
        }
        return contours;
    }

    /// <summary>
    /// A contour with each corner rounded as Skia's SkCornerPathEffect rounds it: the lines are
    /// cut back by the radius (half a line too short for two) and a quadratic curve through the corner joins them.
    /// </summary>
    static void AddCornerEffectContour(CanvasPath path, List<Vector2> points, float radius)
    {
        var count = points.Count;
        Vector2 Step(Vector2 a, Vector2 b, out bool drawLine)
        {
            var distance = Vector2.Distance(a, b);
            drawLine = distance > radius * 2;
            return drawLine ? (b - a) * (radius / distance) : (b - a) * 0.5f;
        }
        var firstStep = Step(points[0], points[1], out var drawFirst);
        path.MoveTo(points[0] + firstStep);
        if (drawFirst)
            path.LineTo(points[1] - firstStep);
        for (var i = 1; i < count; i++)
        {
            var corner = points[i];
            var next = points[(i + 1) % count];
            var step = Step(corner, next, out var draw);
            path.QuadTo(corner, corner + step);
            if (draw)
                path.LineTo(next - step);
        }
        path.QuadTo(points[0], points[0] + firstStep);
        path.Close();
    }

    /// <summary>
    /// A right-angled contour with each corner rounded by the radii of its kind (AddCornerRadiiToPath:
    /// the same radii for convex and concave corners), cutting the lines back to fit.
    /// </summary>
    static void AddRoundedContour(CanvasPath path, List<Vector2> points, CornerRadii radii)
    {
        var count = points.Count;
        // GetRadiiCorner: the radii of the corner at p2, by the directions in and out of it.
        Vector2 CornerAt(Vector2 p1, Vector2 p2, Vector2 p3)
        {
            if (p1.X == p2.X)
            {
                if (p1.Y == p2.Y || p2.X == p3.X)
                    return Vector2.Zero;
                if (p1.Y < p2.Y)
                    return p2.X < p3.X ? radii.BottomLeft : radii.BottomRight;
                return p2.X < p3.X ? radii.TopLeft : radii.TopRight;
            }
            if (p2.X != p3.X || p2.Y == p3.Y)
                return Vector2.Zero;
            if (p1.X < p2.X)
                return p2.Y < p3.Y ? radii.TopRight : radii.BottomRight;
            return p2.Y < p3.Y ? radii.TopLeft : radii.BottomLeft;
        }
        // Each line from points[i] to points[i + 1], shortened by the corners at both ends.
        var starts = new Vector2[count];
        var ends = new Vector2[count];
        for (var i = 0; i < count; i++)
        {
            var start = points[i];
            var end = points[(i + 1) % count];
            var corner1 = CornerAt(points[(i + count - 1) % count], start, end);
            var corner2 = CornerAt(start, end, points[(i + 2) % count]);
            if (start.X == end.X)
            {
                var length = MathF.Abs(end.Y - start.Y);
                float a = corner1.Y, b = corner2.Y;
                if (a + b > length)
                {
                    var scale = length / (a + b);
                    (a, b) = (MathF.Floor(a * scale), MathF.Floor(b * scale));
                }
                var direction = MathF.Sign(end.Y - start.Y);
                starts[i] = start + new Vector2(0, a * direction);
                ends[i] = end - new Vector2(0, b * direction);
            }
            else
            {
                var length = MathF.Abs(end.X - start.X);
                float a = corner1.X, b = corner2.X;
                if (a + b > length)
                {
                    var scale = length / (a + b);
                    (a, b) = (MathF.Floor(a * scale), MathF.Floor(b * scale));
                }
                var direction = MathF.Sign(end.X - start.X);
                starts[i] = start + new Vector2(a * direction, 0);
                ends[i] = end - new Vector2(b * direction, 0);
            }
        }
        // A conic of weight 1/√2 is a quarter ellipse: the cubic with the circle constant.
        const float Kappa = 0.5522847f;
        path.MoveTo(ends[count - 1]);
        for (var i = 0; i < count; i++)
        {
            var from = i == 0 ? ends[count - 1] : ends[i - 1];
            var corner = points[i];
            path.CubicTo(from + (corner - from) * Kappa, starts[i] + (corner - starts[i]) * Kappa, starts[i]);
            path.LineTo(ends[i]);
        }
        path.Close();
    }
}

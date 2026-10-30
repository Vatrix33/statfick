using Statventure.Core;

namespace Statventure.UI.Paint.Compositing;

/// <summary>A mapped rect and whether it is exact (Blink's FloatClipRect: rounded clips and effects loosen it).</summary>
internal readonly record struct FloatClipRect(RectF Rect, bool Tight = true, bool Infinite = false)
{
    public static readonly FloatClipRect InfiniteLoose = new(new RectF(-1e9f, -1e9f, 2e9f, 2e9f), false, true);

    public FloatClipRect Intersect(FloatClipRect clip)
    {
        if (clip.Infinite)
            return this with { Tight = Tight && clip.Tight };
        if (Infinite)
            return clip with { Tight = Tight && clip.Tight };
        return new FloatClipRect(Gfx.Intersect(Rect, clip.Rect), Tight && clip.Tight);
    }
}

/// <summary>gfx::RectF's semantics where they matter (empty rects, inclusive edges), and Blink's GeometryMapper in screen space.</summary>
internal static class Gfx
{
    public static bool IsEmpty(RectF r) => r.Width <= 0 || r.Height <= 0;

    public static RectF Intersect(RectF a, RectF b)
    {
        float x = MathF.Max(a.X, b.X), y = MathF.Max(a.Y, b.Y);
        float r = MathF.Min(a.Right, b.Right), bottom = MathF.Min(a.Bottom, b.Bottom);
        return x >= r || y >= bottom ? RectF.Empty : RectF.FromEdges(x, y, r, bottom);
    }

    static RectF InclusiveIntersect(RectF a, RectF b)
    {
        float x = MathF.Max(a.X, b.X), y = MathF.Max(a.Y, b.Y);
        float r = MathF.Min(a.Right, b.Right), bottom = MathF.Min(a.Bottom, b.Bottom);
        return x > r || y > bottom ? RectF.Empty : RectF.FromEdges(x, y, r, bottom);
    }

    public static RectF Union(RectF a, RectF b)
    {
        if (IsEmpty(a))
            return b;
        if (IsEmpty(b))
            return a;
        return RectF.FromEdges(MathF.Min(a.X, b.X), MathF.Min(a.Y, b.Y), MathF.Max(a.Right, b.Right), MathF.Max(a.Bottom, b.Bottom));
    }

    /// <summary>gfx::RectF::Contains(rect).</summary>
    public static bool Contains(RectF outer, RectF inner) =>
        inner.X >= outer.X && inner.Right <= outer.Right && inner.Y >= outer.Y && inner.Bottom <= outer.Bottom;

    public static bool Intersects(RectF a, RectF b) =>
        !IsEmpty(a) && !IsEmpty(b) && b.X < a.Right && a.X < b.Right && b.Y < a.Bottom && a.Y < b.Bottom;

    static float Area(RectF r) => MathF.Max(0, r.Width) * MathF.Max(0, r.Height);

    /// <summary>gfx::MaximumCoveredRect: the larger of two rects, or the expanded intersection when bigger.</summary>
    public static RectF MaximumCoveredRect(RectF a, RectF b)
    {
        var maximum = a;
        var maximumArea = Area(a);
        if (Area(b) > maximumArea)
        {
            maximum = b;
            maximumArea = Area(b);
        }
        var intersection = InclusiveIntersect(a, b);
        if (intersection.Width != 0 || intersection.Height != 0)
        {
            var vertical = RectF.FromEdges(intersection.X, MathF.Min(a.Y, b.Y), intersection.Right, MathF.Max(a.Bottom, b.Bottom));
            if (Area(vertical) > maximumArea)
            {
                maximum = vertical;
                maximumArea = Area(vertical);
            }
            var horizontal = RectF.FromEdges(MathF.Min(a.X, b.X), intersection.Y, MathF.Max(a.Right, b.Right), intersection.Bottom);
            if (Area(horizontal) > maximumArea)
                maximum = horizontal;
        }
        return maximum;
    }

    /// <summary>Whether the transforms from <paramref name="local"/> up to <paramref name="ancestor"/> include an animation or sticky offset (overlap must assume anything).</summary>
    static bool PathHasAnimationOrSticky(TransformNode local, TransformNode ancestor)
    {
        for (var node = local; node is not null && node != ancestor; node = node.Parent)
            if ((node.DirectReasons & (CompositingReasons.ActiveTransformAnimation | CompositingReasons.StickyPosition)) != 0)
                return true;
        return false;
    }

    /// <summary>GeometryMapper::LocalToAncestorClipRect: the clips from <paramref name="local"/> up to (not including) <paramref name="ancestor"/>.</summary>
    static FloatClipRect ClipRect(ClipNode local, ClipNode ancestor, TransformNode ancestorTransform, bool forOverlap)
    {
        var clip = FloatClipRect.InfiniteLoose with { Tight = true };
        for (var node = local; node is not null && node != ancestor; node = node.Parent)
        {
            if (node.IsRoot)
                break;
            if (forOverlap && PathHasAnimationOrSticky(node.LocalTransform, ancestorTransform))
                continue;
            clip = clip.Intersect(new FloatClipRect(node.Rect, node.Tight));
        }
        return clip;
    }

    /// <summary>
    /// GeometryMapper::LocalToAncestorVisualRect in screen space: the rect clipped by the clips
    /// between the states (pixel-moving filters between them spread it out).
    /// </summary>
    public static FloatClipRect LocalToAncestorVisualRect(RectF rect, PropertyState local, PropertyState ancestor, bool forOverlap = false)
    {
        var result = new FloatClipRect(rect, local.Effect == ancestor.Effect);
        if (local.Transform == ancestor.Transform && local.Clip == ancestor.Clip)
            return result;
        for (var effect = local.Effect; effect is not null && effect != ancestor.Effect; effect = effect.Parent)
            if (effect.PixelMovingOutset is { } outset)
                result = new FloatClipRect(result.Rect.Inflate(outset.Left, outset.Top, outset.Right, outset.Bottom), false);
        if (forOverlap && PathHasAnimationOrSticky(local.Transform, ancestor.Transform))
            result = FloatClipRect.InfiniteLoose;
        return result.Intersect(ClipRect(local.Clip, ancestor.Clip, ancestor.Transform, forOverlap));
    }

    /// <summary>GeometryMapper::VisibilityLimit: what of a state can ever show.</summary>
    public static RectF? VisibilityLimit(PropertyState state)
    {
        if (state.Clip.LocalTransform == state.Transform)
            return state.Clip.IsRoot ? null : state.Clip.Rect;
        if (state.Transform.Scroll is { } scroll)
            return scroll.ContentsRect;
        return null;
    }

    static RectF VisualRectForCompositingOverlap(RectF rect, PropertyState local, PropertyState ancestor)
    {
        var visual = LocalToAncestorVisualRect(rect, local, ancestor, forOverlap: true);
        var r = visual.Rect;
        if (VisibilityLimit(ancestor) is { } limit)
            r = Intersect(r, limit);
        return r;
    }

    /// <summary>
    /// GeometryMapper::MightOverlapForCompositing: whether two rects can overlap on screen, with
    /// content in other scrollers taken over its whole scroll range.
    /// </summary>
    public static bool MightOverlapForCompositing(RectF rect1, PropertyState state1, RectF rect2, PropertyState state2, EffectNode rootEffect)
    {
        if (state1.Transform == state2.Transform && state1.Clip == state2.Clip)
            return Intersects(rect1, rect2);
        var common = new PropertyState(state1.Transform.LowestCommonAncestor(state2.Transform), state1.Clip.LowestCommonAncestor(state2.Clip), rootEffect);
        var scroll1 = state1.Transform.NearestScrollTranslation;
        var scroll2 = state2.Transform.NearestScrollTranslation;
        var newState1 = state1;
        var newState2 = state2;
        // Clips in other scroll spaces can move: ignore them (a fixed box clipped in a scroller…).
        bool AdjustForClips(TransformNode scrollTranslation, ref PropertyState state)
        {
            for (var clip = state.Clip; clip != common.Clip && clip.Parent is not null; clip = clip.Parent)
                if (clip.LocalTransform.NearestScrollTranslation != scrollTranslation)
                {
                    state = state with { Clip = clip.Parent };
                    return false;
                }
            return false;
        }
        if (AdjustForClips(scroll1, ref newState1) || AdjustForClips(scroll2, ref newState2))
            return true;
        if (scroll1 == scroll2)
            return Intersects(VisualRectForCompositingOverlap(rect1, state1, common), VisualRectForCompositingOverlap(rect2, state2, common));
        var lca = common.Transform.NearestScrollTranslation;
        var newRect1 = rect1;
        var newRect2 = rect2;
        void AdjustRectAndState(TransformNode? scrollTranslation, ref RectF rect, ref PropertyState state)
        {
            for (; scrollTranslation is not null && scrollTranslation != lca; scrollTranslation = scrollTranslation.ParentScrollTranslation)
                MapVisualRectAboveScroll(scrollTranslation, ref rect, ref state, rootEffect);
        }
        AdjustRectAndState(scroll1, ref newRect1, ref newState1);
        AdjustRectAndState(scroll2, ref newRect2, ref newState2);
        return Intersects(VisualRectForCompositingOverlap(newRect1, newState1, common), VisualRectForCompositingOverlap(newRect2, newState2, common));
    }

    /// <summary>
    /// GeometryMapper::MapVisualRectAboveScrollForCompositingOverlap: a rect under a scroll
    /// translation in its container's space, over every scroll offset.
    /// </summary>
    static void MapVisualRectAboveScroll(TransformNode scrollTranslation, ref RectF rect, ref PropertyState state, EffectNode rootEffect)
    {
        var scroll = scrollTranslation.Scroll!;
        var contentsState = new PropertyState(scrollTranslation, scroll.OverflowClip, rootEffect);
        rect = VisualRectForCompositingOverlap(rect, state, contentsState);
        // In screen space the contents sit at the current offset: all offsets from 0 to the maximum
        // put the rect anywhere from (current offset) further down-right to max − current up-left.
        var current = scroll.CurrentOffset;
        var max = scroll.MaxOffset;
        rect = RectF.FromEdges(rect.X - (max.X - current.X), rect.Y - (max.Y - current.Y), rect.Right + current.X, rect.Bottom + current.Y);
        rect = Intersect(rect, scroll.ContainerRect);
        state = new PropertyState(scrollTranslation.Parent!, scroll.OverflowClip.Parent!, rootEffect);
    }
}

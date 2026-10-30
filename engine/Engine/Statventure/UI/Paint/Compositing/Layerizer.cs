using Statventure.Core;
using Statventure.UI.Layout;

namespace Statventure.UI.Paint.Compositing;

internal enum ChunkKind { Normal, ScrollHitTest, Scrollbar, Foreign }

/// <summary>cc::HitTestOpaqueness, in its order (a union keeps the larger unless it can't).</summary>
internal enum HitTestOpaqueness { Transparent, Mixed, Opaque }

/// <summary>Chromium's preference for LCD text over composited layers (WidgetBase::ComputeLCDTextPreference).</summary>
internal enum LcdTextPreference
{
    /// <summary>Below a device scale of 1.5: keep layers that can draw LCD text apart.</summary>
    StronglyPreferred,
    /// <summary>At 1.5 and above: composite freely.</summary>
    Ignored,
}

/// <summary>
/// A run of drawings in one property tree state (Blink's PaintChunk), in screen space: its bounds
/// (drawings and hit test rects, unclipped), what of it is known to be opaque, whether it has text
/// and whether that text is known to sit on an opaque background.
/// </summary>
internal sealed class PaintChunk
{
    public required PropertyState State;
    public ChunkKind Kind;
    /// <summary>Scroll hit test and scrollbar chunks: the scroller's scroll translation.</summary>
    public TransformNode? ScrollTranslation;
    public LayoutBox? Owner;
    public RectF Bounds;
    public HitTestOpaqueness HitTestOpaqueness;
    public RectF DrawableBounds;
    public RectF RectKnownToBeOpaque;
    public bool HasText;
    public bool TextKnownToBeOnOpaqueBackground = true;

    /// <summary>PaintChunker::UnionBounds: a rect joins the bounds, with its hit test opaqueness.</summary>
    public void UnionBounds(RectF rect, HitTestOpaqueness opaqueness)
    {
        HitTestOpaqueness = UnionHitTestOpaqueness(Bounds, HitTestOpaqueness, rect, opaqueness);
        Bounds = Gfx.Union(Bounds, rect);
    }

    /// <summary>cc::UnionHitTestOpaqueness.</summary>
    static HitTestOpaqueness UnionHitTestOpaqueness(RectF rect1, HitTestOpaqueness opaqueness1, RectF rect2, HitTestOpaqueness opaqueness2)
    {
        if (Gfx.IsEmpty(rect1))
            return opaqueness2;
        if (Gfx.IsEmpty(rect2))
            return opaqueness1;
        if (opaqueness1 < opaqueness2)
            return UnionHitTestOpaqueness(rect2, opaqueness2, rect1, opaqueness1);
        return opaqueness1 switch
        {
            HitTestOpaqueness.Transparent => HitTestOpaqueness.Transparent,
            HitTestOpaqueness.Mixed => HitTestOpaqueness.Mixed,
            _ when opaqueness2 == HitTestOpaqueness.Opaque =>
                Gfx.Union(rect1, rect2) == Gfx.MaximumCoveredRect(rect1, rect2) ? HitTestOpaqueness.Opaque : HitTestOpaqueness.Mixed,
            _ => Gfx.Contains(rect1, rect2) ? HitTestOpaqueness.Opaque : HitTestOpaqueness.Mixed,
        };
    }
    /// <summary>The colour of a drawing that covers the chunk (PaintChunker's background colour candidate), or null.</summary>
    public Color? BackgroundColor;
    public float BackgroundColorArea;

    public PendingLayer? Layer;
    /// <summary>The layerization's verdict: this chunk's text is drawn with LCD antialiasing.</summary>
    public bool LcdText;

    public bool DrawsContent => !Gfx.IsEmpty(DrawableBounds);

    public override string ToString() => $"{Kind} {Owner} {Bounds} {State}";
}

internal enum LayerType { Other, Overlap, ScrollHitTest, Foreign, Scrollbar }

/// <summary>A cc layer in the making (Blink's PendingLayer).</summary>
internal sealed class PendingLayer
{
    public readonly List<PaintChunk> Chunks = [];
    public PropertyState State;
    public RectF Bounds;
    public RectF RectKnownToBeOpaque;
    public bool HasText;
    public bool TextKnownToBeOnOpaqueBackground;
    public bool DrawsContent;
    public LayerType Type;
    public bool HasDecompositedBlendMode;
    public readonly List<TransformNode> NonCompositedScrollTranslations = [];

    /// <summary>The cc layer can draw LCD text (contents opaque, or its text known to be on opaque backgrounds, and nothing else forbids it).</summary>
    public bool LcdAllowed;
    public bool ContentsOpaque;
    public string LcdReason = "";

    public bool RequiresOwnLayer => Type is LayerType.ScrollHitTest or LayerType.Foreign or LayerType.Scrollbar;

    public PendingLayer(PaintChunk chunk, LayerType type)
    {
        Chunks.Add(chunk);
        State = chunk.State;
        Bounds = chunk.Bounds;
        RectKnownToBeOpaque = chunk.RectKnownToBeOpaque;
        Type = type;
        HasText = chunk.HasText;
        DrawsContent = chunk.DrawsContent;
        TextKnownToBeOnOpaqueBackground = chunk.TextKnownToBeOnOpaqueBackground;
        if (Gfx.VisibilityLimit(State) is { } limit)
        {
            Bounds = Gfx.Intersect(Bounds, limit);
            if (Gfx.IsEmpty(Bounds))
                DrawsContent = false;
        }
        RectKnownToBeOpaque = Gfx.Intersect(RectKnownToBeOpaque, Bounds);
        if (type == LayerType.Other && chunk.Kind == ChunkKind.ScrollHitTest && chunk.ScrollTranslation is { } scroll)
            NonCompositedScrollTranslations.Add(scroll);
    }

    public override string ToString() => $"{Type} {Chunks[0].Owner} {Bounds} {(LcdAllowed ? "lcd" : "gray " + LcdReason)}";
}

/// <summary>
/// Blink's PaintArtifactCompositor layerization (Layerizer::LayerizeGroup, PendingLayer::Merge and
/// friends, Chromium 152 with MergeFixedLayers, MergeStickyLayers and RasterInducingScroll): which
/// paint chunks share a cc layer, then which layers can draw LCD text.
/// </summary>
internal sealed class Layerizer(List<PaintChunk> chunks, LcdTextPreference preference, float devicePixelRatio, EffectNode rootEffect)
{
    const float MergeSparsityAreaTolerance = 10000;

    readonly List<PendingLayer> _layers = [];
    readonly HashSet<TransformNode> _directlyCompositedTransforms = [];
    readonly Dictionary<TransformNode, bool> _paintedScrollTranslations = [];
    int _cursor;

    public List<PendingLayer> Layerize()
    {
        LayerizeGroup(rootEffect, false);
        foreach (var layer in _layers)
        {
            foreach (var chunk in layer.Chunks)
                chunk.Layer = layer;
            ComputeLcd(layer);
        }
        foreach (var chunk in chunks)
            chunk.LcdText = chunk.HasText && chunk.Layer is { LcdAllowed: true } layer && TextKeepsLcdInLayer(chunk, layer);
        return _layers;
    }

    // ---------- Composited scrolling ----------

    public bool NeedsCompositedScrolling(TransformNode scrollTranslation)
    {
        if (scrollTranslation.HasDirectReasons)
            return true;
        if (!_paintedScrollTranslations.TryGetValue(scrollTranslation, out var composited))
            return preference != LcdTextPreference.StronglyPreferred;
        return composited;
    }

    /// <summary>PaintArtifactCompositor::ComputeNeedsCompositedScrolling for a scroll hit test chunk.</summary>
    bool ComputeNeedsCompositedScrolling(int index)
    {
        var hitTest = chunks[index];
        var scrollTranslation = hitTest.ScrollTranslation!;
        if (scrollTranslation.HasDirectReasons)
            return true;
        var scroll = scrollTranslation.Scroll!;
        if (!scroll.UserScrollable || scroll.PrefersNonComposited)
            return false;
        if (preference != LcdTextPreference.StronglyPreferred)
            return true;
        // Composite only without losing LCD text: when the scrolling background is opaque over the contents.
        for (var i = index + 1; i < chunks.Count; i++)
        {
            var next = chunks[i];
            if (next.State.Transform == hitTest.State.Transform)
                continue;
            return next.State.Transform == scrollTranslation && next.State.Clip == scroll.OverflowClip
                && next.State.Effect == hitTest.State.Effect && Gfx.Contains(next.RectKnownToBeOpaque, scroll.ContentsRect);
        }
        return true;
    }

    LayerType ChunkCompositingType(PaintChunk chunk) => chunk.Kind switch
    {
        ChunkKind.ScrollHitTest when NeedsCompositedScrolling(chunk.ScrollTranslation!) => LayerType.ScrollHitTest,
        ChunkKind.Foreign => LayerType.Foreign,
        // RasterInducingScroll: every scrollbar of a scroller is a layer of its own.
        ChunkKind.Scrollbar when chunk.ScrollTranslation is not null => LayerType.Scrollbar,
        _ => LayerType.Other,
    };

    // ---------- Layerization ----------

    static EffectNode? StrictChildOfAlongPath(EffectNode ancestor, EffectNode node)
    {
        for (var n = node; n is not null; n = n.Parent)
            if (n.Parent == ancestor)
                return n;
        return null;
    }

    void LayerizeGroup(EffectNode currentGroup, bool forceDrawsContent)
    {
        var firstLayerInCurrentGroup = _layers.Count;
        while (_cursor < chunks.Count)
        {
            var chunk = chunks[_cursor];
            if (chunk.State.Effect == currentGroup)
            {
                if (chunk.Kind == ChunkKind.ScrollHitTest && chunk.ScrollTranslation is { } scrollTranslation)
                {
                    var composited = ComputeNeedsCompositedScrolling(_cursor);
                    if (!_paintedScrollTranslations.TryGetValue(scrollTranslation, out var known) || composited)
                        _paintedScrollTranslations[scrollTranslation] = composited || known;
                }
                _layers.Add(new PendingLayer(chunk, ChunkCompositingType(chunk)));
                _cursor++;
                if (_layers[^1].RequiresOwnLayer)
                    continue;
            }
            else
            {
                var subgroup = StrictChildOfAlongPath(currentGroup, chunk.State.Effect);
                if (subgroup is null)
                    break;
                var firstLayerInSubgroup = _layers.Count;
                LayerizeGroup(subgroup, forceDrawsContent || subgroup.DrawsContent);
                // Two or more layers that can't merge: leave the subgroup composited.
                if (firstLayerInSubgroup != _layers.Count - 1)
                    continue;
                if (!DecompositeEffect(currentGroup, firstLayerInCurrentGroup, subgroup, firstLayerInSubgroup))
                    continue;
            }
            var newLayer = _layers[^1];
            if (forceDrawsContent)
                newLayer.DrawsContent = true;
            // The first layer under a directly composited transform can't merge into earlier ones
            // (except fixed-only and sticky-only transforms, which may merge with their kind).
            if (newLayer.State.Transform.NearestDirectlyCompositedAncestor is { } compositedTransform
                && !compositedTransform.RequiresCompositingForFixedPositionOnly
                && !compositedTransform.RequiresCompositingForStickyPositionOnly
                && _directlyCompositedTransforms.Add(compositedTransform))
                continue;
            var candidateIndex = _layers.Count - 1;
            while (candidateIndex > firstLayerInCurrentGroup)
            {
                candidateIndex--;
                var candidate = _layers[candidateIndex];
                if (Merge(candidate, newLayer))
                {
                    _layers.RemoveAt(_layers.Count - 1);
                    break;
                }
                if (MightOverlap(newLayer, candidate))
                {
                    newLayer.Type = LayerType.Overlap;
                    break;
                }
            }
        }
    }

    /// <summary>Layerizer::DecompositeEffect: folds a subgroup's single layer into its parent group when the effect needn't be composited.</summary>
    bool DecompositeEffect(EffectNode parentEffect, int firstLayerInParentGroup, EffectNode effect, int layerIndex)
    {
        var layer = _layers[layerIndex];
        if (layer.State.Effect != effect || layer.RequiresOwnLayer || effect.HasDirectReasons)
            return false;
        var groupState = new PropertyState(effect.LocalTransform, effect.OutputClip ?? layer.State.Clip, effect);
        if (CanUpcastWith(groupState, layer.State) is not { } upcasted)
            return false;
        upcasted = upcasted with { Effect = parentEffect };
        // An exotic blend mode needs its destination in the same layer.
        if (effect.ExoticBlendMode)
        {
            var previousSiblings = layerIndex - firstLayerInParentGroup;
            if (previousSiblings > 0)
            {
                if (previousSiblings > 2)
                    return false;
                if (previousSiblings == 2 && _layers[firstLayerInParentGroup].DrawsContent)
                    return false;
                var previous = _layers[layerIndex - 1];
                if (previous.DrawsContent && CanUpcastWith(previous, layer, upcasted) is null)
                    return false;
            }
        }
        Upcast(layer, upcasted);
        return true;
    }

    // ---------- PendingLayer ----------

    static void Upcast(PendingLayer layer, PropertyState newState)
    {
        if (layer.State.Effect.ExoticBlendMode)
            layer.HasDecompositedBlendMode = true;
        var mapped = Gfx.LocalToAncestorVisualRect(layer.Bounds, layer.State, newState);
        layer.RectKnownToBeOpaque = MapRectKnownToBeOpaque(layer, newState, mapped);
        layer.Bounds = mapped.Rect;
        layer.State = newState;
    }

    static RectF MapRectKnownToBeOpaque(PendingLayer layer, PropertyState newState, FloatClipRect mappedLayerBounds)
    {
        if (!mappedLayerBounds.Tight || Gfx.IsEmpty(layer.RectKnownToBeOpaque))
            return RectF.Empty;
        if (layer.RectKnownToBeOpaque == layer.Bounds)
            return mappedLayerBounds.Rect;
        var mapped = Gfx.LocalToAncestorVisualRect(layer.RectKnownToBeOpaque, layer.State, newState);
        return Gfx.Intersect(mapped.Rect, mappedLayerBounds.Rect);
    }

    bool Merge(PendingLayer home, PendingLayer guest)
    {
        if (CanUpcastWith(home, guest, guest.State) is not { } mergedState)
            return false;
        var visibilityLimit = Gfx.VisibilityLimit(mergedState);
        RectF mergedBounds, mergedOpaque;
        bool mergedTextKnown;
        if (!guest.HasDecompositedBlendMode && visibilityLimit is { } limit && limit == home.Bounds && mergedState == home.State && home.RectKnownToBeOpaque == home.Bounds)
        {
            mergedBounds = mergedOpaque = home.Bounds;
            mergedTextKnown = true;
        }
        else
        {
            var newHome = Gfx.LocalToAncestorVisualRect(home.Bounds, home.State, mergedState);
            if (visibilityLimit is { } l1)
                newHome = newHome with { Rect = Gfx.Intersect(newHome.Rect, l1) };
            var newGuest = Gfx.LocalToAncestorVisualRect(guest.Bounds, guest.State, mergedState);
            if (visibilityLimit is { } l2)
                newGuest = newGuest with { Rect = Gfx.Intersect(newGuest.Rect, l2) };
            mergedBounds = Gfx.Union(newHome.Rect, newGuest.Rect);
            mergedOpaque = RectF.Empty;
            mergedTextKnown = home.TextKnownToBeOnOpaqueBackground;
            if (!guest.HasDecompositedBlendMode)
            {
                static float Area(RectF r) => MathF.Max(0, r.Width) * MathF.Max(0, r.Height);
                var sum = Area(newHome.Rect) + Area(newGuest.Rect);
                if (Area(mergedBounds) - sum > MergeSparsityAreaTolerance * devicePixelRatio * devicePixelRatio)
                    return false;
                var homeOpaque = MapRectKnownToBeOpaque(home, mergedState, newHome);
                var guestOpaque = MapRectKnownToBeOpaque(guest, mergedState, newGuest);
                mergedOpaque = Gfx.MaximumCoveredRect(homeOpaque, guestOpaque);
                if (home.TextKnownToBeOnOpaqueBackground != guest.TextKnownToBeOnOpaqueBackground)
                {
                    if (!home.TextKnownToBeOnOpaqueBackground)
                    {
                        if (Gfx.Contains(mergedOpaque, newHome.Rect))
                            mergedTextKnown = true;
                    }
                    else if (!Gfx.Contains(mergedOpaque, newGuest.Rect))
                        mergedTextKnown = false;
                }
                // Don't merge away a layer's LCD text where LCD text is strongly preferred.
                if (preference == LcdTextPreference.StronglyPreferred && !mergedTextKnown)
                {
                    if (home.HasText && home.TextKnownToBeOnOpaqueBackground)
                        return false;
                    if (guest.HasText && guest.TextKnownToBeOnOpaqueBackground)
                        return false;
                }
            }
        }
        home.Chunks.AddRange(guest.Chunks);
        home.Bounds = mergedBounds;
        home.State = mergedState;
        home.DrawsContent |= guest.DrawsContent;
        home.RectKnownToBeOpaque = mergedOpaque;
        home.TextKnownToBeOnOpaqueBackground = mergedTextKnown;
        home.HasText |= guest.HasText;
        home.NonCompositedScrollTranslations.AddRange(guest.NonCompositedScrollTranslations);
        return true;
    }

    /// <summary>PendingLayer::CanUpcastWith.</summary>
    PropertyState? CanUpcastWith(PendingLayer home, PendingLayer guest, PropertyState guestState)
    {
        if (home.RequiresOwnLayer || guest.RequiresOwnLayer || home.State.Effect != guestState.Effect)
            return null;
        if (CanUpcastWith(home.State, guestState) is not { } result)
            return null;
        // The scroll translations must stay covered by the merged layer's scroll hit testing.
        var homeScroll = home.State.Transform.NearestScrollTranslation;
        var guestScroll = guest.State.Transform.NearestScrollTranslation;
        if (homeScroll == guestScroll)
            return result;
        var lca = result.Transform.NearestScrollTranslation;
        if ((guestScroll == lca || home.NonCompositedScrollTranslations.Contains(guestScroll))
            && (homeScroll == lca || guest.NonCompositedScrollTranslations.Contains(homeScroll)))
            return result;
        return null;
    }

    enum Boundary { NotSame, Same, ThroughMergeableComposited }

    TransformNode? NearestCompositedScrollTranslation(TransformNode scrollTranslation)
    {
        for (var t = scrollTranslation; t.Parent is not null; t = t.Parent.NearestScrollTranslation)
            if (NeedsCompositedScrolling(t))
                return t;
        return null;
    }

    Boundary InSameTransformCompositingBoundary(TransformNode t1, TransformNode t2)
    {
        var composited1 = t1.NearestDirectlyCompositedAncestor;
        var composited2 = t2.NearestDirectlyCompositedAncestor;
        if (composited1 != composited2)
        {
            if (composited1 is not null && composited2 is not null && composited1.CanMergeForFixedPosition(composited2))
                return Boundary.ThroughMergeableComposited;
            // (Sticky-only transforms with sticky constraints never merge here: cc's
            // StickyPositionConstraint::CanMerge needs matching constraints, which the game doesn't have.)
            return Boundary.NotSame;
        }
        var scroll1 = t1.NearestScrollTranslation;
        var scroll2 = t2.NearestScrollTranslation;
        if (scroll1 == scroll2 || NearestCompositedScrollTranslation(scroll1) == NearestCompositedScrollTranslation(scroll2))
            return Boundary.Same;
        return Boundary.NotSame;
    }

    bool ClipChainInBoundary(ClipNode node, ClipNode ancestor, TransformNode transform)
    {
        for (var n = node; n != ancestor && n is not null; n = n.Parent)
            if (InSameTransformCompositingBoundary(transform, n.LocalTransform) != Boundary.Same)
                return false;
        return true;
    }

    /// <summary>PropertyTreeState::CanUpcastWith.</summary>
    PropertyState? CanUpcastWith(PropertyState home, PropertyState guest)
    {
        TransformNode upcastTransform;
        if (home.Transform == guest.Transform)
            upcastTransform = home.Transform;
        else
        {
            var boundary = InSameTransformCompositingBoundary(home.Transform, guest.Transform);
            if (boundary == Boundary.NotSame)
                return null;
            upcastTransform = boundary == Boundary.Same ? home.Transform.LowestCommonAncestor(guest.Transform) : home.Transform.NearestDirectlyCompositedAncestor!;
        }
        ClipNode upcastClip;
        if (home.Clip == guest.Clip)
            upcastClip = home.Clip;
        else
        {
            upcastClip = home.Clip.LowestCommonAncestor(guest.Clip);
            if (!ClipChainInBoundary(home.Clip, upcastClip, upcastTransform) || !ClipChainInBoundary(guest.Clip, upcastClip, upcastTransform))
                return null;
        }
        return new PropertyState(upcastTransform, upcastClip, home.Effect);
    }

    bool MightOverlap(PendingLayer a, PendingLayer b) => Gfx.MightOverlapForCompositing(a.Bounds, a.State, b.Bounds, b.State, rootEffect);

    // ---------- LCD text ----------

    /// <summary>
    /// Whether the cc layer can draw LCD text (PictureLayerImpl::ComputeLCDTextDisallowedReason with
    /// the opaqueness ContentLayerClientImpl gives it): contents opaque, or its text known to be on
    /// opaque backgrounds; not under a will-change: transform or transform animation, nor a filter.
    /// </summary>
    void ComputeLcd(PendingLayer layer)
    {
        layer.LcdAllowed = false;
        if (!layer.HasText)
        {
            layer.LcdReason = "no-text";
            return;
        }
        for (var t = layer.State.Transform; t is not null; t = t.Parent)
            if ((t.DirectReasons & (CompositingReasons.WillChangeTransform | CompositingReasons.ActiveTransformAnimation)) != 0)
            {
                layer.LcdReason = "will-change-transform";
                return;
            }
        for (var e = layer.State.Effect; e is not null; e = e.Parent)
            if (e.HasFilter && e.HasDirectReasons)
            {
                layer.LcdReason = "pixel-or-color-effect";
                return;
            }
        var rect = LayerRect(layer);
        var background = BackgroundColor(layer);
        layer.ContentsOpaque = background is { A: > 0 } && Gfx.Contains(layer.RectKnownToBeOpaque, rect);
        if (layer.ContentsOpaque || layer.TextKnownToBeOnOpaqueBackground)
        {
            layer.LcdAllowed = true;
            layer.LcdReason = "none";
            return;
        }
        layer.LcdReason = background is { A: 1 } ? "contents-not-opaque" : "background-color-not-opaque";
    }

    /// <summary>PendingLayer::Bounds: the layer's rect, on whole pixels (enclosing).</summary>
    static RectF LayerRect(PendingLayer layer)
    {
        var b = layer.Bounds;
        return RectF.FromEdges(MathF.Floor(b.X), MathF.Floor(b.Y), MathF.Ceiling(b.Right), MathF.Ceiling(b.Bottom));
    }

    /// <summary>
    /// PendingLayer::ComputeBackgroundColor: the chunks' background colour candidates covering at least
    /// half the layer, from the top down to the first opaque one (through opacity-only child effects),
    /// blended.
    /// </summary>
    static Color? BackgroundColor(PendingLayer layer)
    {
        var colors = new List<Color>();
        var minArea = 0.5f * layer.Bounds.Width * layer.Bounds.Height;
        for (var i = layer.Chunks.Count - 1; i >= 0; i--)
        {
            var chunk = layer.Chunks[i];
            if (chunk.BackgroundColor is not { A: > 0 } color || chunk.BackgroundColorArea < minArea)
                continue;
            var effect = chunk.State.Effect;
            if (effect != layer.State.Effect)
            {
                if (effect.Parent != layer.State.Effect || effect.HasFilter || effect.ExoticBlendMode || effect.HasBackdropFilter)
                    continue;
                color = color with { A = color.A * effect.Opacity };
            }
            colors.Add(color);
            if (color.A >= 1)
                break;
        }
        if (colors.Count == 0)
            return null;
        var result = colors[^1];
        for (var i = colors.Count - 2; i >= 0; i--)
            result = Over(colors[i], result);
        return result;
    }

    /// <summary>color_utils::GetResultingPaintColor: one colour painted over another.</summary>
    internal static Color Over(Color top, Color bottom)
    {
        var a = top.A + bottom.A * (1 - top.A);
        if (a <= 0)
            return new Color(0, 0, 0, 0);
        return new Color((top.R * top.A + bottom.R * bottom.A * (1 - top.A)) / a, (top.G * top.A + bottom.G * bottom.A * (1 - top.A)) / a, (top.B * top.A + bottom.B * bottom.A * (1 - top.A)) / a, a);
    }

    /// <summary>
    /// Whether a chunk's text keeps LCD in its layer's display list: the effects between it and the
    /// layer are opacity layers (SaveLayerAlpha, started from the backdrop) and the list has no
    /// filter or blend layer (a SaveLayerOp, which would stop that).
    /// </summary>
    static bool TextKeepsLcdInLayer(PaintChunk chunk, PendingLayer layer)
    {
        for (var e = chunk.State.Effect; e is not null && e != layer.State.Effect; e = e.Parent)
            if (e.HasFilter || e.ExoticBlendMode)
                return false;
        foreach (var other in layer.Chunks)
            for (var e = other.State.Effect; e is not null && e != layer.State.Effect; e = e.Parent)
                if (e.HasFilter || e.ExoticBlendMode)
                    return false;
        return true;
    }
}

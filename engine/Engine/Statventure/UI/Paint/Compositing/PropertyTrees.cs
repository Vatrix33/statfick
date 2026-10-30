using System.Numerics;
using Statventure.Core;
using Statventure.UI.Layout;

namespace Statventure.UI.Paint.Compositing;

/// <summary>Why Blink composites a property node (the reasons the layerizer and cc layers use; CompositingReason).</summary>
[Flags]
internal enum CompositingReasons : uint
{
    None = 0,
    FixedPosition = 1 << 0,
    StickyPosition = 1 << 1,
    RootScroller = 1 << 2,
    WillChangeTransform = 1 << 3,
    WillChangeOpacity = 1 << 4,
    WillChangeFilter = 1 << 5,
    WillChangeBackdropFilter = 1 << 6,
    WillChangeOther = 1 << 7,
    ThreeDTransform = 1 << 8,
    ActiveTransformAnimation = 1 << 9,
    ActiveOpacityAnimation = 1 << 10,
    ActiveFilterAnimation = 1 << 11,
    ActiveBackdropFilterAnimation = 1 << 12,
    BackdropFilter = 1 << 13,
    Canvas = 1 << 14,
    Overlap = 1 << 15,
    OverflowScrolling = 1 << 16,
    Scrollbar = 1 << 17,

    FixedPositionReasons = FixedPosition,
    TransformAnimations = ActiveTransformAnimation,
}

/// <summary>
/// A transform space (Blink's TransformPaintPropertyNode). Geometry stays in screen (viewport)
/// space throughout, so a node keeps what the layerizer asks of it: its parent, whether it's a 2D
/// translation, its direct compositing reasons, and for scroll translations their scroll node.
/// </summary>
internal sealed class TransformNode(TransformNode? parent, string kind, LayoutBox? owner = null)
{
    public readonly TransformNode? Parent = parent;
    public readonly string Kind = kind;
    public readonly LayoutBox? Owner = owner;
    public readonly int Depth = parent is null ? 0 : parent.Depth + 1;
    public CompositingReasons DirectReasons;
    public bool Is2dTranslation = true;
    public ScrollNode? Scroll;

    public bool IsRoot => Parent is null;
    public bool HasDirectReasons => DirectReasons != CompositingReasons.None;

    /// <summary>The node itself or the nearest ancestor with direct compositing reasons, or null.</summary>
    public TransformNode? NearestDirectlyCompositedAncestor
    {
        get
        {
            for (var node = this; node is not null; node = node.Parent)
                if (node.HasDirectReasons)
                    return node;
            return null;
        }
    }

    /// <summary>The nearest scroll translation at or above this node (the root counts as one).</summary>
    public TransformNode NearestScrollTranslation
    {
        get
        {
            var node = this;
            while (node.Scroll is null && node.Parent is not null)
                node = node.Parent;
            return node;
        }
    }

    public TransformNode? ParentScrollTranslation => Parent?.NearestScrollTranslation;

    public bool RequiresCompositingForFixedPositionOnly =>
        (DirectReasons & CompositingReasons.FixedPosition) != 0 && (DirectReasons & ~CompositingReasons.FixedPositionReasons) == 0;

    public bool RequiresCompositingForStickyPositionOnly => DirectReasons == CompositingReasons.StickyPosition;

    /// <summary>TransformPaintPropertyNode::CanMergeForFixedPosition.</summary>
    public bool CanMergeForFixedPosition(TransformNode other) =>
        DirectReasons == other.DirectReasons && RequiresCompositingForFixedPositionOnly && other.RequiresCompositingForFixedPositionOnly
        && ParentScrollTranslation == other.ParentScrollTranslation && Parent == other.Parent;

    public TransformNode LowestCommonAncestor(TransformNode other) => Trees.LowestCommonAncestor(this, other, n => n.Parent, n => n.Depth);

    public override string ToString() => $"{Kind}{(Owner is { } o ? " " + o : "")}{(HasDirectReasons ? " [" + DirectReasons + "]" : "")}";
}

/// <summary>A scroll container's scrolling (Blink's ScrollPaintPropertyNode), in screen space.</summary>
internal sealed class ScrollNode
{
    public required LayoutBox Box;
    /// <summary>The scrollport.</summary>
    public required RectF ContainerRect;
    /// <summary>The scrollable contents at the current scroll offset.</summary>
    public required RectF ContentsRect;
    public required Vector2 MaxOffset;
    public required Vector2 CurrentOffset;
    public required bool UserScrollable;
    public required ClipNode OverflowClip;
    /// <summary>Selects and text fields prefer non-composited scrolling.</summary>
    public bool PrefersNonComposited;
}

/// <summary>A clip (Blink's ClipPaintPropertyNode): a rect in screen space, in a transform space.</summary>
internal sealed class ClipNode(ClipNode? parent, TransformNode localTransform, RectF rect, bool tight, string kind, LayoutBox? owner = null)
{
    public readonly ClipNode? Parent = parent;
    public readonly TransformNode LocalTransform = localTransform;
    public readonly RectF Rect = rect;
    /// <summary>A plain rect (not rounded): mapped rects stay exact.</summary>
    public readonly bool Tight = tight;
    public readonly string Kind = kind;
    public readonly LayoutBox? Owner = owner;
    public readonly int Depth = parent is null ? 0 : parent.Depth + 1;

    public bool IsRoot => Parent is null;

    public ClipNode LowestCommonAncestor(ClipNode other) => Trees.LowestCommonAncestor(this, other, n => n.Parent, n => n.Depth);

    public override string ToString() => $"{Kind}{(Owner is { } o ? " " + o : "")} {Rect}";
}

/// <summary>An effect (Blink's EffectPaintPropertyNode): opacity, blend mode, filters, backdrop filters, isolation.</summary>
internal sealed class EffectNode(EffectNode? parent, TransformNode localTransform, ClipNode? outputClip, string kind, LayoutBox? owner = null)
{
    public readonly EffectNode? Parent = parent;
    public readonly TransformNode LocalTransform = localTransform;
    public readonly ClipNode? OutputClip = outputClip;
    public readonly string Kind = kind;
    public readonly LayoutBox? Owner = owner;
    public readonly int Depth = parent is null ? 0 : parent.Depth + 1;
    public float Opacity = 1;
    public bool ExoticBlendMode;
    public bool HasFilter;
    public bool HasBackdropFilter;
    /// <summary>How far a blur or drop shadow filter spreads its content (a pixel-moving filter), or null.</summary>
    public Outset? PixelMovingOutset;
    public CompositingReasons DirectReasons;

    public bool IsRoot => Parent is null;
    public bool HasDirectReasons => DirectReasons != CompositingReasons.None;
    /// <summary>EffectPaintPropertyNode::DrawsContent: filters and backdrop filters can draw without content.</summary>
    public bool DrawsContent => HasFilter || HasBackdropFilter;

    public override string ToString() => $"{Kind}{(Owner is { } o ? " " + o : "")}{(HasDirectReasons ? " [" + DirectReasons + "]" : "")}";
}

internal readonly record struct Outset(float Left, float Top, float Right, float Bottom);

/// <summary>The property tree state a paint chunk paints in (Blink's PropertyTreeState).</summary>
internal readonly record struct PropertyState(TransformNode Transform, ClipNode Clip, EffectNode Effect)
{
    public override string ToString() => $"t:{Transform} | c:{Clip} | e:{Effect}";
}

internal static class Trees
{
    public static T LowestCommonAncestor<T>(T a, T b, Func<T, T?> parent, Func<T, int> depth) where T : class
    {
        while (depth(a) > depth(b))
            a = parent(a)!;
        while (depth(b) > depth(a))
            b = parent(b)!;
        while (a != b)
        {
            a = parent(a)!;
            b = parent(b)!;
        }
        return a;
    }

    /// <summary>Whether <paramref name="node"/> is <paramref name="ancestor"/> or below it.</summary>
    public static bool IsAtOrBelow(ClipNode node, ClipNode ancestor)
    {
        for (var n = node; n is not null; n = n.Parent)
            if (n == ancestor)
                return true;
        return false;
    }
}

using System.Numerics;
using Statventure.Core;
using Statventure.UI.Dom;
using Statventure.UI.Layout;
using Statventure.UI.Style;

namespace Statventure.UI.Paint.Compositing;

/// <summary>Where Blink paints a scroller's background (LayoutBox::ComputeBackgroundPaintLocation).</summary>
[Flags]
internal enum BackgroundPaintLocation { BorderBox = 1, Contents = 2, Both = BorderBox | Contents }

/// <summary>A box's property tree states: for its own painting and for its contents (inside its overflow clip and scroll).</summary>
internal sealed class BoxProperties
{
    public required PropertyState BorderBox;
    public required PropertyState Contents;
    public TransformNode? ScrollTranslation;
    public BackgroundPaintLocation BackgroundLocation = BackgroundPaintLocation.BorderBox;
}

/// <summary>
/// Builds Blink-like paint property trees from the layout (PaintPropertyTreeBuilder, for what the
/// engine supports): the viewport's clip and root scroll translation, fixed-position translations
/// (composited when the viewport has overflow), sticky translations, CSS transforms, effects
/// (opacity, blend modes, backdrop filters, isolation around blending descendants), filters,
/// clip-path and clip, overflow clips and scroll translations. Transforms and clips follow the
/// containing block chain (absolute and fixed boxes take their containing block's), effects the
/// tree.
/// </summary>
internal sealed class PropertyTreeBuilder
{
    public readonly TransformNode RootTransform = new(null, "root");
    public readonly ClipNode RootClip;
    public readonly EffectNode RootEffect;
    readonly Dictionary<LayoutBox, BoxProperties> _boxes = [];
    readonly HashSet<LayoutBox> _blendIsolated = [];
    readonly UiDocument _document;
    PropertyState _fixed;
    bool _viewportHasOverflow;

    public PropertyTreeBuilder(UiDocument document)
    {
        _document = document;
        RootClip = new ClipNode(null, RootTransform, new RectF(-1e9f, -1e9f, 2e9f, 2e9f), true, "root");
        RootEffect = new EffectNode(null, RootTransform, null, "root");
    }

    public BoxProperties? For(LayoutBox box) => _boxes.GetValueOrDefault(box);

    /// <summary>The state text and inline content of a box paint in: its nearest box's contents.</summary>
    public PropertyState ContentsOf(LayoutBox box)
    {
        for (var b = box; b is not null; b = b.Parent)
            if (_boxes.TryGetValue(b, out var properties))
                return properties.Contents;
        return new PropertyState(RootTransform, RootClip, RootEffect);
    }

    public void Build(LayoutBox viewport)
    {
        _boxes.Clear();
        _blendIsolated.Clear();
        var root = new PropertyState(RootTransform, RootClip, RootEffect);
        // The viewport: its overflow clip and the root scroller's scroll translation.
        var clipRect = new RectF(0, 0, viewport.PaddingBoxWidth, viewport.PaddingBoxHeight);
        var viewportClip = new ClipNode(RootClip, RootTransform, clipRect, true, "viewport", viewport);
        var max = new Vector2(MathF.Max(0, viewport.ScrollWidth - viewport.PaddingBoxWidth), MathF.Max(0, viewport.ScrollHeight - viewport.PaddingBoxHeight));
        _viewportHasOverflow = max.X > 0 || max.Y > 0;
        var rootScroll = new TransformNode(RootTransform, "scroll", viewport) { DirectReasons = CompositingReasons.RootScroller };
        rootScroll.Scroll = new ScrollNode
        {
            Box = viewport,
            ContainerRect = clipRect,
            // (Scroll nodes' rects are whole pixels: the contents size pixel-snapped.)
            ContentsRect = new RectF(-viewport.DocumentScroll.X, -viewport.DocumentScroll.Y, Snap(MathF.Max(viewport.ScrollWidth, clipRect.Width)), Snap(MathF.Max(viewport.ScrollHeight, clipRect.Height))),
            MaxOffset = max,
            CurrentOffset = viewport.DocumentScroll,
            UserScrollable = true,
            OverflowClip = viewportClip,
        };
        _boxes[viewport] = new BoxProperties
        {
            BorderBox = root,
            Contents = new PropertyState(rootScroll, viewportClip, RootEffect),
            ScrollTranslation = rootScroll,
            BackgroundLocation = BackgroundPaintLocation.Contents,
        };
        _fixed = new PropertyState(RootTransform, viewportClip, RootEffect);
        foreach (var child in viewport.Children)
            FindBlendIsolation(child, child);
        foreach (var child in viewport.Children)
            Visit(child, _boxes[viewport].Contents, RootEffect);
    }

    static bool CreatesStackingContext(LayoutBox box) =>
        box.Type is not (BoxType.Text or BoxType.LineBreak or BoxType.Inline) && box.Style.CreatesStackingContext;

    /// <summary>A stacking context with a blending descendant (not inside a nearer stacking context) is an isolation group.</summary>
    void FindBlendIsolation(LayoutBox box, LayoutBox context)
    {
        if (box.Type is BoxType.Text or BoxType.LineBreak)
            return;
        if (box.Style.MixBlendMode != CssBlendMode.Normal && box != context)
            _blendIsolated.Add(context);
        var next = CreatesStackingContext(box) ? box : context;
        foreach (var child in box.Children)
            FindBlendIsolation(child, next);
    }

    bool Animates(LayoutBox box, string property) =>
        box.Element is { } element && _document.Animations.HasCurrentAnimation(element, PseudoName(box), property);

    static string PseudoName(LayoutBox box) => box.Pseudo switch
    {
        PseudoKind.Before => "before",
        PseudoKind.After => "after",
        PseudoKind.Marker => "marker",
        _ => "",
    };

    void Visit(LayoutBox box, PropertyState inherited, EffectNode effect)
    {
        if (box.Type is BoxType.Text or BoxType.LineBreak)
            return;
        var s = box.Style;
        // The containing block's space: absolute boxes take their container's contents, fixed boxes the viewport's fixed context.
        var (transform, clip) = (inherited.Transform, inherited.Clip);
        var fixedToView = false;
        if (s.Position == PositionKind.Fixed)
        {
            if (box.Container is null or { IsViewport: true })
            {
                (transform, clip) = (_fixed.Transform, _fixed.Clip);
                fixedToView = true;
            }
            else if (_boxes.TryGetValue(box.Container, out var container))
                (transform, clip) = (container.Contents.Transform, container.Contents.Clip);
        }
        else if (s.Position == PositionKind.Absolute && box.Container is { } absoluteContainer && _boxes.TryGetValue(absoluteContainer, out var absolute))
            (transform, clip) = (absolute.Contents.Transform, absolute.Contents.Clip);

        // A box fixed to the view has a paint offset translation of its own, composited when the page scrolls.
        if (fixedToView)
            transform = new TransformNode(transform, "fixed", box) { DirectReasons = _viewportHasOverflow ? CompositingReasons.FixedPosition : 0 };
        var sticky = s.Position == PositionKind.Sticky && StickyScrolls(box);
        var willChangeTransform = s.WillChangeProperty("transform") || s.WillChangeProperty("translate") || s.WillChangeProperty("rotate") || s.WillChangeProperty("scale");
        var animatesTransform = Animates(box, "transform") || Animates(box, "translate") || Animates(box, "rotate") || Animates(box, "scale");
        var svgRoot = box.Type == BoxType.Replaced && box.Element?.Tag == "svg";
        var needsScroll = NeedsScrollTranslation(box);
        // NeedsPaintOffsetTranslation: other boxes that get a translation node of their own (the
        // node matters to which clips limit a layer's bounds). Fixed, sticky and transformed boxes
        // have their own nodes below anyway.
        if (!fixedToView && !sticky && !s.HasTransform && !willChangeTransform && !animatesTransform
            && (needsScroll || box.VerticalScrollbar > 0 || box.HorizontalScrollbar > 0 || ScrollbarGeometry.CanResize(box) || svgRoot
                || s.BackdropFilters.Length > 0 || s.WillChangeProperty("opacity") || s.WillChangeProperty("filter") || s.WillChangeProperty("backdrop-filter")
                || Animates(box, "opacity") || Animates(box, "filter")))
            transform = new TransformNode(transform, "paint-offset", box);
        if (sticky)
            transform = new TransformNode(transform, "sticky", box) { DirectReasons = CompositingReasons.StickyPosition };
        if (s.HasTransform || willChangeTransform || animatesTransform)
        {
            var matrix = s.HasTransform ? UiPainter.TransformFor(box, box.ViewportRect) : Matrix3x2.Identity;
            transform = new TransformNode(transform, "transform", box)
            {
                Is2dTranslation = matrix.M11 == 1 && matrix.M22 == 1 && matrix.M12 == 0 && matrix.M21 == 0,
                DirectReasons = (willChangeTransform ? CompositingReasons.WillChangeTransform : 0) | (animatesTransform ? CompositingReasons.ActiveTransformAnimation : 0),
            };
        }

        // A stacking context is an isolation group with an effect of its own only around blending
        // descendants (isolation: isolate alone makes none; NeedsEffectIgnoringClipPathAnd2DScale).
        var isolated = CreatesStackingContext(box) && _blendIsolated.Contains(box);
        var animatesOpacity = Animates(box, "opacity");
        if (s.Opacity < 1 || s.MixBlendMode != CssBlendMode.Normal || s.BackdropFilters.Length > 0 || s.WillChangeProperty("opacity")
            || s.WillChangeProperty("backdrop-filter") || animatesOpacity || isolated)
            effect = new EffectNode(effect, transform, clip, "effect", box)
            {
                Opacity = s.Opacity,
                ExoticBlendMode = s.MixBlendMode != CssBlendMode.Normal,
                HasBackdropFilter = s.BackdropFilters.Length > 0,
                DirectReasons = (s.BackdropFilters.Length > 0 ? CompositingReasons.BackdropFilter : 0)
                    | (s.WillChangeProperty("opacity") ? CompositingReasons.WillChangeOpacity : 0)
                    | (s.WillChangeProperty("backdrop-filter") ? CompositingReasons.WillChangeBackdropFilter : 0)
                    | (animatesOpacity ? CompositingReasons.ActiveOpacityAnimation : 0),
            };
        var animatesFilter = Animates(box, "filter");
        if (s.Filters.Length > 0 || s.WillChangeProperty("filter") || animatesFilter)
            effect = new EffectNode(effect, transform, clip, "filter", box)
            {
                HasFilter = s.Filters.Length > 0,
                PixelMovingOutset = PixelMovingOutset(s.Filters),
                DirectReasons = (s.WillChangeProperty("filter") ? CompositingReasons.WillChangeFilter : 0) | (animatesFilter ? CompositingReasons.ActiveFilterAnimation : 0),
            };

        var rect = box.ViewportRect;
        if (s.ClipPath is { } clipPath)
        {
            var inset = RectF.FromEdges(
                rect.X + clipPath.Left.ResolveOr(rect.Width, 0), rect.Y + clipPath.Top.ResolveOr(rect.Height, 0),
                rect.Right - clipPath.Right.ResolveOr(rect.Width, 0), rect.Bottom - clipPath.Bottom.ResolveOr(rect.Height, 0));
            clip = new ClipNode(clip, transform, ScreenRect(inset, transform, box), clipPath.Radii.Length == 0, "clip-path", box);
        }
        if (s.ClipRect is { } clipEdges && box.IsOutOfFlow)
        {
            var clipped = RectF.FromEdges(rect.X + (clipEdges[3] ?? 0), rect.Y + (clipEdges[0] ?? 0), rect.X + (clipEdges[1] ?? rect.Width), rect.Y + (clipEdges[2] ?? rect.Height));
            clip = new ClipNode(clip, transform, ScreenRect(clipped, transform, box), true, "css-clip", box);
        }
        var borderBox = new PropertyState(transform, clip, effect);
        var properties = new BoxProperties { BorderBox = borderBox, Contents = borderBox };
        if (box.IsScrollContainer)
        {
            var padding = new RectF(rect.X + box.Border.Left, rect.Y + box.Border.Top, box.PaddingBoxWidth, box.PaddingBoxHeight);
            static float Round(float v) => MathF.Round(v, MidpointRounding.AwayFromZero);
            var snapped = RectF.FromEdges(Round(padding.Left), Round(padding.Top), Round(padding.Right), Round(padding.Bottom));
            var rounded = UiPainter.HasInnerRadius(box);
            var overflowClip = new ClipNode(clip, transform, ScreenRect(snapped, transform, box), !rounded, "overflow", box);
            var contentsTransform = transform;
            var max = new Vector2(MathF.Max(0, box.ScrollWidth - box.PaddingBoxWidth), MathF.Max(0, box.ScrollHeight - box.PaddingBoxHeight));
            var current = box.Element?.LayoutScroll ?? Vector2.Zero;
            var scrollsOverflow = ScrollsOverflow(box);
            if (needsScroll)
            {
                contentsTransform = new TransformNode(transform, "scroll", box)
                {
                    Scroll = new ScrollNode
                    {
                        Box = box,
                        ContainerRect = overflowClip.Rect,
                        ContentsRect = new RectF(overflowClip.Rect.X - current.X, overflowClip.Rect.Y - current.Y, Snap(MathF.Max(box.ScrollWidth, padding.Width)), Snap(MathF.Max(box.ScrollHeight, padding.Height))),
                        MaxOffset = max,
                        CurrentOffset = current,
                        UserScrollable = s.OverflowX is Overflow.Auto or Overflow.Scroll || s.OverflowY is Overflow.Auto or Overflow.Scroll,
                        OverflowClip = overflowClip,
                        PrefersNonComposited = box.Element?.Tag == "select",
                    },
                };
                properties.ScrollTranslation = contentsTransform;
                if (scrollsOverflow)
                    properties.BackgroundLocation = BackgroundLocation(box);
            }
            properties.Contents = new PropertyState(contentsTransform, overflowClip, effect);
        }
        else if (svgRoot)
        {
            // An <svg>'s content: clipped to its content box unless its overflow is visible, then
            // in its replaced content transform (the viewBox's).
            var contentsClip = clip;
            if (s.OverflowX != Overflow.Visible || s.OverflowY != Overflow.Visible)
            {
                var content = RectF.FromEdges(Snap(rect.X + box.ContentLeft), Snap(rect.Y + box.ContentTop), Snap(rect.X + box.ContentLeft + box.ContentWidth), Snap(rect.Y + box.ContentTop + box.ContentHeight));
                contentsClip = new ClipNode(clip, transform, ScreenRect(content, transform, box), true, "overflow", box);
            }
            properties.Contents = new PropertyState(new TransformNode(transform, "replaced", box), contentsClip, effect);
        }
        _boxes[box] = properties;
        foreach (var child in box.Children)
            Visit(child, properties.Contents, effect);
        if (box.Marker is { } marker)
            Visit(marker, properties.Contents, effect);
    }

    /// <summary>
    /// NeedsScrollAndScrollTranslation: a scroll container whose content overflows an auto or
    /// scroll axis (PaintLayerScrollableArea::ScrollsOverflow), or that is scrolled, or that doesn't
    /// propagate scrolling (overscroll-behavior).
    /// </summary>
    static bool NeedsScrollTranslation(LayoutBox box)
    {
        if (!box.IsScrollContainer)
            return false;
        var s = box.Style;
        var propagates = s.Visibility != Visibility.Visible || (s.OverscrollBehaviorX == OverscrollBehavior.Auto && s.OverscrollBehaviorY == OverscrollBehavior.Auto);
        return ScrollsOverflow(box) || (box.Element?.LayoutScroll ?? Vector2.Zero) != Vector2.Zero || !propagates;
    }

    /// <summary>LayoutUnit::Round: half up.</summary>
    static float Snap(float v) => MathF.Round(v, MidpointRounding.AwayFromZero);

    /// <summary>PaintLayerScrollableArea::ScrollsOverflow: content overflows an axis that scrolls (auto or scroll), in whole pixels.</summary>
    internal static bool ScrollsOverflow(LayoutBox box)
    {
        static float Round(float v) => MathF.Round(v, MidpointRounding.AwayFromZero);
        var s = box.Style;
        return (s.OverflowX is Overflow.Auto or Overflow.Scroll && Round(box.ScrollWidth) > Round(box.PaddingBoxWidth))
            || (s.OverflowY is Overflow.Auto or Overflow.Scroll && Round(box.ScrollHeight) > Round(box.PaddingBoxHeight));
    }

    /// <summary>A rect in the box's border box space on screen (through its own transform when it has one).</summary>
    static RectF ScreenRect(RectF rect, TransformNode transform, LayoutBox box) =>
        transform.Owner == box && transform.Kind == "transform" && box.Style.HasTransform ? rect.Transform(UiPainter.TransformFor(box, box.ViewportRect)) : rect;

    /// <summary>Whether a sticky box can move with scrolling (its scroll container has overflow).</summary>
    static bool StickyScrolls(LayoutBox box)
    {
        for (var container = box.Container; container is not null; container = container.Container)
            if (container.IsScrollContainer || container.IsViewport)
                return container.ScrollWidth > container.PaddingBoxWidth + 0.01f || container.ScrollHeight > container.PaddingBoxHeight + 0.01f;
        return false;
    }

    /// <summary>How far blur and drop-shadow filters spread content (FilterOperations::MapRect).</summary>
    static Outset? PixelMovingOutset(FilterFunction[] filters)
    {
        float left = 0, top = 0, right = 0, bottom = 0;
        var any = false;
        foreach (var filter in filters)
        {
            if (filter.Kind == FilterFunctionKind.Blur)
            {
                var reach = filter.Amount * 3;
                (left, top, right, bottom) = (left + reach, top + reach, right + reach, bottom + reach);
                any = true;
            }
            else if (filter.Kind == FilterFunctionKind.DropShadow)
            {
                var reach = filter.Amount * 3;
                left += MathF.Max(0, reach - filter.OffsetX);
                right += MathF.Max(0, reach + filter.OffsetX);
                top += MathF.Max(0, reach - filter.OffsetY);
                bottom += MathF.Max(0, reach + filter.OffsetY);
                any = true;
            }
        }
        return any ? new Outset(left, top, right, bottom) : null;
    }

    /// <summary>LayoutBox::ComputeBackgroundPaintLocation for a scroller that scrolls (LCD text strongly preferred or not).</summary>
    static BackgroundPaintLocation BackgroundLocation(LayoutBox box)
    {
        var s = box.Style;
        if (s.BoxShadows.Any(shadow => shadow.Inset))
            return BackgroundPaintLocation.BorderBox;
        // Background images (only local ones could scroll with the contents).
        if (s.Backgrounds.Length > 0 || s.BackgroundImages.Any(image => image.Kind != ImageValue.None.Kind))
            return BackgroundPaintLocation.BorderBox;
        if (s.BackgroundColor.A <= 0 || s.ScrollbarGutterStable)
            return BackgroundPaintLocation.BorderBox;
        // background-clip is the border box: like the padding box when borders and scrollbars are opaque.
        if (BorderBoxIsLikePaddingBox(box))
            return BackgroundPaintLocation.Contents;
        return s.BackgroundColor.A >= 1 ? BackgroundPaintLocation.Both : BackgroundPaintLocation.BorderBox;
    }

    /// <summary>LayoutBox::BackgroundClipBorderBoxIsEquivalentToPaddingBox.</summary>
    static bool BorderBoxIsLikePaddingBox(LayoutBox box)
    {
        var s = box.Style;
        if ((box.VerticalScrollbar > 0 || box.HorizontalScrollbar > 0) && s.ScrollbarTrack is { A: < 1 })
            return false;
        bool Opaque(float width, Color? color, BorderLineStyle style) => width <= 0 || (style == BorderLineStyle.Solid && (color ?? s.Color).A >= 1);
        return Opaque(s.BorderTop, s.BorderTopColor, s.BorderTopStyle) && Opaque(s.BorderRight, s.BorderRightColor, s.BorderRightStyle)
            && Opaque(s.BorderBottom, s.BorderBottomColor, s.BorderBottomStyle) && Opaque(s.BorderLeft, s.BorderLeftColor, s.BorderLeftStyle);
    }
}

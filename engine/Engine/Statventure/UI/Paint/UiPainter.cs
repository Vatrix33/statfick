using System.Numerics;
using Statventure.Canvas;
using Statventure.Core;
using Statventure.Graphics;
using Statventure.UI.Dom;
using Statventure.UI.Layout;
using Statventure.UI.Paint.Compositing;
using Statventure.UI.Style;
using CanvasRenderer = Statventure.Canvas.Canvas;
using CanvasBorderStyle = Statventure.Canvas.BorderStyle;

namespace Statventure.UI.Paint;

/// <summary>
/// Paints a laid-out document on the canvas in CSS order (CSS 2 Appendix E): stacking contexts
/// with negative, zero and positive z-index, block backgrounds, inline content, positioned boxes;
/// overflow clips, transforms, opacity, filters, blend modes and backdrop filters; backgrounds
/// (colours, gradients, images), borders with per-side colours, box shadows, outlines, text with
/// decorations and shadows, images, form controls and scrollbars.
/// </summary>
public sealed partial class UiPainter : IDisposable
{
    readonly UiDocument _document;
    readonly PaintTarget _canvas = new();
    LayoutBox? _backgroundSource;
    readonly PropertyTreeBuilder _properties;
    /// <summary>The analysis pass's paint chunks, with their verdicts (LCD text or not).</summary>
    List<PaintChunk>? _analysis;
    /// <summary>The layers Chromium would make of the last paint (Blink's pending layers).</summary>
    internal List<PendingLayer>? Layers { get; private set; }
    /// <summary>Effects with LCD text in them (their canvas layers start from the backdrop).</summary>
    readonly HashSet<EffectNode> _lcdEffects = [];
    /// <summary>
    /// How far Blink's rounded paint offsets move what is painted now from where layout put it (in
    /// the space the canvas transforms; zero outside transformed boxes): see <see cref="PushTransformOf"/>.
    /// Every position read from layout goes through <see cref="Placed"/> or <see cref="PlacedAt"/>, so
    /// the snapping that follows is measured from the rounded origin.
    /// </summary>
    Vector2 _shift;

    /// <summary>A box's border box where it is painted (layout's place, moved by the rounded paint offsets around it).</summary>
    RectF Placed(LayoutBox box) => box.ViewportRect.Offset(_shift);

    /// <summary>A box's position where it is painted (see <see cref="Placed"/>).</summary>
    Vector2 PlacedAt(LayoutBox box) => box.ViewportPosition + _shift;

    /// <summary>The bindless texture of an image by url (null while it isn't loaded).</summary>
    public Func<string, int?>? ImageTexture { get; set; }

    public UiPainter(UiDocument document)
    {
        _document = document;
        _properties = new PropertyTreeBuilder(document);
    }

    public void Dispose()
    {
        foreach (var image in _filterImages.Values)
            image?.Dispose();
        _filterImages.Clear();
        foreach (var image in _controlImages.Values)
            image?.Dispose();
        _controlImages.Clear();
    }

    readonly Dictionary<bool, GpuImage?> _controlImages = [];

    /// <summary>The search cancel image in a rect (as a texture made on first use).</summary>
    void DrawSearchCancel(RectF rect, bool pressed)
    {
        if (!_canvas.HasCanvas)
        {
            _canvas.FillRect(rect, Color.Transparent);
            return;
        }
        if (!_controlImages.TryGetValue(pressed, out var image))
        {
            var rgba = ControlImages.SearchCancel(pressed);
            image = _canvas.CreateTexture(rgba.Width, rgba.Height, rgba.Pixels);
            _controlImages[pressed] = image;
        }
        if (image is not null)
            _canvas.DrawImage(image, rect);
    }

    sealed class Context
    {
        public required LayoutBox Root;
        public readonly List<LayoutBox> Negative = [];
        public readonly List<LayoutBox> Layered = [];
        public readonly List<LayoutBox> Positive = [];
    }

    /// <summary>Paints the whole document (call after <see cref="UiDocument.Update"/>). The document's hit testing uses what was painted.</summary>
    public void Paint(CanvasRenderer canvas)
    {
        // Glyphs as Chromium rasterizes them on this screen (unhinted at subpixel positions on Linux above 1×).
        canvas.FontSettings = Text.FontRenderSettings.System.AtScale(_document.DeviceScale);
        // First the paint chunks and Chromium's layerization of them, which decide where text keeps
        // LCD antialiasing; then the drawing, in the same chunks.
        _contentful = false;
        Traverse(canvas, analyze: true);
        Traverse(canvas, analyze: false);
        // PaintTiming's first contentful paint: a frame with text, an image, SVG or a surface.
        if (_contentful)
            _document.PaintedContent();
    }

    bool _contentful;

    /// <summary>Goes through the paint order without drawing, for the hit regions alone.</summary>
    internal void RecordHitRegions() => Traverse(null, analyze: false);

    /// <summary>Paints for the layerization alone (no canvas): fills <see cref="Layers"/>.</summary>
    internal void Analyze() => Traverse(null, analyze: true);

    /// <summary>The engine's layers as Chromium's cc layers would be (for comparing with traces).</summary>
    internal List<PendingLayer> AnalyzeLayers()
    {
        Analyze();
        return Layers!;
    }

    void Traverse(CanvasRenderer? canvas, bool analyze)
    {
        var version = _document.VisualVersion;
        var viewport = _document.Layout.Viewport;
        _canvas.Begin(canvas, new Vector2(viewport.Width, viewport.Height), recordOnly: analyze);
        _canvas.DevicePixelRatio = _document.Zoom;
        if (analyze)
        {
            _properties.Build(viewport);
            _canvas.BeginChunks();
        }
        else if (canvas is not null && _analysis is not null)
            _canvas.BeginChunks(_analysis);
        if (viewport.Children.FirstOrDefault() is { } root)
        {
            PaintCanvasBackground(viewport, root);
            // The root element makes the root stacking context.
            PaintNormalFlow(root);
            PaintViewportScrollbars(viewport, root);
        }
        if (analyze)
        {
            _analysis = _canvas.Chunks!;
            var preference = Text.FontRenderSettings.System.IsSubpixel && _document.DeviceScale < 1.5f ? LcdTextPreference.StronglyPreferred : LcdTextPreference.Ignored;
            Layers = new Layerizer(_analysis, preference, _document.Zoom, _properties.RootEffect).Layerize();
            _lcdEffects.Clear();
            foreach (var chunk in _analysis)
                if (chunk.LcdText)
                    for (var effect = chunk.State.Effect; effect is not null && _lcdEffects.Add(effect); effect = effect.Parent)
                    {
                    }
        }
        _document.SetHitRegions(_canvas.Regions, version);
    }

    BoxProperties? PropertiesOf(LayoutBox box) => _properties.For(box);

    /// <summary>The box takes pointer input where it is painted (pointer-events: none boxes don't, though Blink still records their hit test data).</summary>
    void Hit(LayoutBox box, RectF rect, CornerRadii radii, bool text = false, LineItem? item = null, RectF? borderBox = null) =>
        _canvas.Hit(box, rect, radii, text, item, borderBox, takesInput: box.Style.UsedPointerEvents != PointerEvents.None);

    // ---------- Stacking contexts ----------


    static bool CreatesStackingContext(LayoutBox box)
    {
        if (box.Type is BoxType.Text or BoxType.LineBreak or BoxType.Inline)
            return false;
        var s = box.Style;
        if (s.CreatesStackingContext)
            return true;
        // Flex and grid items with a z-index stack even when static.
        return s.ZIndex is not null && box.Parent is { Type: BoxType.Flex or BoxType.Grid } && !box.IsOutOfFlow;
    }

    static bool IsPositioned(LayoutBox box) => box.Style.Position != PositionKind.Static && box.Type is not (BoxType.Text or BoxType.LineBreak or BoxType.Inline);

    static int ZOf(LayoutBox box) => box.Style.ZIndex ?? 0;

    static IEnumerable<LayoutBox> ChildrenOf(LayoutBox box)
    {
        if (box.Marker is { } marker)
            yield return marker;
        foreach (var child in box.Children)
            yield return child;
    }

    /// <summary>Gathers a context's layers: child stacking contexts by z-index and positioned boxes in tree order.</summary>
    static void Collect(LayoutBox parent, Context context)
    {
        foreach (var child in ChildrenOf(parent))
        {
            if (child.Type is BoxType.Text or BoxType.LineBreak)
                continue;
            if (CreatesStackingContext(child))
            {
                var z = ZOf(child);
                (z < 0 ? context.Negative : z > 0 ? context.Positive : context.Layered).Add(child);
                continue;
            }
            if (IsPositioned(child))
                context.Layered.Add(child);
            Collect(child, context);
        }
    }

    void PaintContext(Context context)
    {
        var root = context.Root;
        // Stable sorts: by z-index, then tree order.
        var negative = context.Negative.Select((b, i) => (b, i)).OrderBy(t => ZOf(t.b)).ThenBy(t => t.i).Select(t => t.b).ToList();
        var positive = context.Positive.Select((b, i) => (b, i)).OrderBy(t => ZOf(t.b)).ThenBy(t => t.i).Select(t => t.b).ToList();
        PaintDecorations(root);
        // Scrollbars paint with the box's background (Blink's non-overlay overflow controls), outside
        // the padding box its content is clipped to.
        PaintScrollbars(root);
        var clips = PushOwnClip(root);
        foreach (var box in negative)
            PaintLayer(box, root);
        PaintBlockBackgrounds(root, includeSelf: false);
        PaintForeground(root);
        PaintOutlines(root, includeSelf: false);
        PopClips(clips);
        // Its own outline isn't clipped by its own overflow, and comes before its layered children.
        PaintOutline(root);
        clips = PushOwnClip(root);
        foreach (var box in context.Layered)
            PaintLayer(box, root);
        foreach (var box in positive)
            PaintLayer(box, root);
        PopClips(clips);
    }

    /// <summary>The root element's stacking context.</summary>
    void PaintNormalFlow(LayoutBox rootBox)
    {
        var context = new Context { Root = rootBox };
        Collect(rootBox, context);
        PaintContext(context);
    }

    /// <summary>A child stacking context or a positioned box painted in its parent context's layer order.</summary>
    void PaintLayer(LayoutBox box, LayoutBox contextRoot)
    {
        var clips = PushClipChain(box, contextRoot);
        // Blink paints a stacked layer as a subsequence, which may start and end its own chunks.
        var subsequence = _canvas.BeginLayer(PropertiesOf(box)?.BorderBox, InkOverflowRect(box.Style, Placed(box)),
            box.Style.UsedPointerEvents == PointerEvents.None || box.Style.Visibility != Visibility.Visible, HasTwoDescendantLayers(box));
        if (CreatesStackingContext(box))
            PaintStackingContext(box);
        else
            PaintPseudoContext(box);
        _canvas.EndLayer(subsequence);
        PopClips(clips);
    }

    /// <summary>
    /// Whether a box has a PaintLayer in Blink (LayoutBox::LayerTypeRequired): stacked, or a
    /// non-replaced box that clips its overflow.
    /// </summary>
    static bool HasPaintLayer(LayoutBox box) =>
        CreatesStackingContext(box) || IsPositioned(box)
        || (box.IsScrollContainer && box.Type != BoxType.Replaced && (box.Style.OverflowX != Overflow.Visible || box.Style.OverflowY != Overflow.Visible));

    /// <summary>Whether two or more PaintLayers are below a box's.</summary>
    static bool HasTwoDescendantLayers(LayoutBox box)
    {
        var count = 0;
        bool Visit(LayoutBox b)
        {
            foreach (var child in b.Children)
            {
                if (HasPaintLayer(child) && ++count >= 2)
                    return true;
                if (Visit(child))
                    return true;
            }
            return false;
        }
        return Visit(box);
    }

    void PaintStackingContext(LayoutBox box)
    {
        var s = box.Style;
        if (s.Visibility == Visibility.Collapse && box.Type != BoxType.Block)
            return;
        // Fully transparent: nothing is drawn, but it still takes input.
        var muted = s.Opacity <= 0;
        if (muted)
            _canvas.Mute();
        var shift = _shift;
        var transformed = s.HasTransform && PushTransformOf(box);
        var rect = Placed(box);
        var clipped = 0;
        if (s.ClipPath is { } clipPath)
        {
            var inset = RectF.FromEdges(
                rect.X + clipPath.Left.ResolveOr(rect.Width, 0), rect.Y + clipPath.Top.ResolveOr(rect.Height, 0),
                rect.Right - clipPath.Right.ResolveOr(rect.Width, 0), rect.Bottom - clipPath.Bottom.ResolveOr(rect.Height, 0));
            var radii = clipPath.Radii.Length == 4 ? ResolveRadii(clipPath.Radii[0], clipPath.Radii[1], clipPath.Radii[2], clipPath.Radii[3], inset.Size) : CornerRadii.Zero;
            _canvas.PushClip(inset, radii);
            clipped++;
        }
        if (s.ClipRect is { } clipRect && box.IsOutOfFlow)
        {
            _canvas.PushClip(ClipRectOf(clipRect, rect));
            clipped++;
        }
        // Backdrop filters see what's behind, before the box paints.
        foreach (var filter in muted ? [] : s.BackdropFilters)
            if (filter.Kind == FilterFunctionKind.Blur && filter.Amount > 0)
                _canvas.DrawBackdropBlur(rect, BorderRadii(box, rect.Size), filter.Amount);
        var layered = !muted && (s.Opacity < 1 || s.Filters.Length > 0 || s.MixBlendMode != CssBlendMode.Normal);
        if (layered)
        {
            _canvas.PushLayer(new LayerOptions
            {
                // LCD text in it (by the layerization's verdicts) needs the backdrop under it.
                StartsFromBackdrop = PropertiesOf(box)?.BorderBox.Effect is { } effect && OwnEffectHasLcdText(effect, box),
                Opacity = s.Opacity,
                Filters = s.Filters.Select(ToCanvasFilter).ToArray(),
                Blend = s.MixBlendMode switch
                {
                    CssBlendMode.Multiply => BlendMode.Multiply,
                    CssBlendMode.Screen => BlendMode.Screen,
                    CssBlendMode.PlusLighter => BlendMode.Additive,
                    _ => BlendMode.Premultiplied,
                },
            });
        }
        var context = new Context { Root = box };
        Collect(box, context);
        PaintContext(context);
        if (layered)
            _canvas.PopLayer();
        for (var i = 0; i < clipped; i++)
            _canvas.PopClip();
        if (transformed)
            _canvas.PopTransform();
        _shift = shift;
        if (muted)
            _canvas.Unmute();
    }

    /// <summary>
    /// A transformed box's paint offset as Blink makes it (PaintPropertyTreeBuilder::
    /// UpdateForPaintOffsetTranslation): its offset in the space around it is rounded to a whole
    /// device pixel (LayoutUnit::Round, half up) for the paint offset translation, and the fraction
    /// left over (the subpixel accumulation) is kept only along the axes it can propagate along
    /// (<see cref="SubpixelAxes"/>); along the others the box and everything in it move to the rounded
    /// place, and are snapped from there. The transform's origin is measured from that place. Pushes
    /// the box's transform and moves <see cref="_shift"/>; returns true (the caller pops it).
    /// </summary>
    bool PushTransformOf(LayoutBox box)
    {
        var (transform, move) = RoundedTransform(box);
        _shift += move;
        _canvas.PushTransform(transform);
        return true;
    }

    /// <summary>
    /// A transformed box's transform about its origin measured from its rounded paint offset, and
    /// how far that offset moves the box and its contents (see <see cref="PushTransformOf"/>).
    /// </summary>
    (Matrix3x2 Transform, Vector2 Move) RoundedTransform(LayoutBox box)
    {
        var rect = Placed(box);
        var scale = _canvas.Scale;
        var (keepX, keepY) = SubpixelAxes(box, rect);
        static float Round(float v, float scale) => MathF.Floor(v * scale + 0.5f) / scale;
        var move = new Vector2(keepX ? 0 : Round(rect.X, scale) - rect.X, keepY ? 0 : Round(rect.Y, scale) - rect.Y);
        return (TransformFor(box, rect.Offset(move)), move);
    }

    /// <summary>
    /// Along which axes a transformed box keeps the fraction of its paint offset (Blink's
    /// CanPropagateSubpixelAccumulation): none under will-change of transform, scale or rotate; under
    /// a current animation of transform, rotate or scale all of it if every keyframe is an identity or
    /// a translation, else none; otherwise all of it for no transform or a translation, the axes the
    /// transform doesn't scale for a scale and translation, and none for anything else (rotations,
    /// skews).
    /// </summary>
    (bool X, bool Y) SubpixelAxes(LayoutBox box, RectF rect)
    {
        var s = box.Style;
        if (s.WillChangeProperty("transform") || s.WillChangeProperty("scale") || s.WillChangeProperty("rotate"))
            return (false, false);
        if (box.Element is { } element)
        {
            var pseudo = PseudoName(box);
            if (_document.Animations.HasCurrentAnimation(element, pseudo, "transform") || _document.Animations.HasCurrentAnimation(element, pseudo, "rotate")
                || _document.Animations.HasCurrentAnimation(element, pseudo, "scale"))
            {
                var translations = _document.Animations.TransformAnimationsAre(element, pseudo, style => IsTranslation(CssTransform(style, rect)));
                return (translations, translations);
            }
        }
        var m = TransformFor(box, rect);
        if (IsTranslation(m))
            return (true, true);
        if (m.M12 == 0 && m.M21 == 0)
            return (m.M11 == 1, m.M22 == 1);
        return (false, false);
    }

    static bool IsTranslation(Matrix3x2 m) => m.M11 == 1 && m.M22 == 1 && m.M12 == 0 && m.M21 == 0;

    static string PseudoName(LayoutBox box) => box.Pseudo switch
    {
        PseudoKind.Before => "before",
        PseudoKind.After => "after",
        PseudoKind.Marker => "marker",
        _ => "",
    };

    /// <summary>Whether a box's own effects (filter, then opacity) have LCD text in them.</summary>
    bool OwnEffectHasLcdText(EffectNode effect, LayoutBox box)
    {
        for (var e = effect; e is not null && e.Owner == box; e = e.Parent)
            if (_lcdEffects.Contains(e))
                return true;
        return false;
    }

    /// <summary>A positioned box with z-index auto, or an atomic inline: painted as if it made a stacking context, except that its positioned descendants belong to the real one.</summary>
    void PaintPseudoContext(LayoutBox box)
    {
        if (box.Style.ClipRect is { } clipRect && box.IsOutOfFlow)
            _canvas.PushClip(ClipRectOf(clipRect, Placed(box)));
        PaintDecorations(box);
        PaintScrollbars(box);
        var clips = PushOwnClip(box);
        PaintBlockBackgrounds(box, includeSelf: false);
        PaintForeground(box);
        PaintOutlines(box, includeSelf: false);
        PopClips(clips);
        PaintOutline(box);
        if (box.Style.ClipRect is not null && box.IsOutOfFlow)
            _canvas.PopClip();
    }

    static RectF ClipRectOf(float?[] edges, RectF rect) => RectF.FromEdges(
        rect.X + (edges[3] ?? 0), rect.Y + (edges[0] ?? 0),
        rect.X + (edges[1] ?? rect.Width), rect.Y + (edges[2] ?? rect.Height));

    /// <summary>Is this box painted with the normal flow of its parent (not a layer of its own)?</summary>
    static bool InNormalFlow(LayoutBox box) => !CreatesStackingContext(box) && !IsPositioned(box);

    /// <summary>
    /// Flex and grid items paint like inline blocks (CSS Flexbox §5.4, Grid §9; Blink's
    /// IsPaintedAtomically): all their phases at once, in their container's foreground phase.
    /// </summary>
    static bool PaintsAtomically(LayoutBox box) =>
        box.Parent is { Type: BoxType.Flex or BoxType.Grid } && !box.IsOutOfFlow && box.Type is not (BoxType.Text or BoxType.Inline or BoxType.LineBreak);

    /// <summary>A box's children in paint order: a flex or grid container's in order-modified document order.</summary>
    static IEnumerable<LayoutBox> PaintOrderChildren(LayoutBox box) =>
        box.Type is BoxType.Flex or BoxType.Grid && box.Children.Any(c => c.Style.Order != 0) ? box.Children.OrderBy(c => c.Style.Order) : box.Children;

    // ---------- Clips ----------

    /// <summary>Clips to a scroll container's padding box (with its inner radii) for its descendants.</summary>
    int PushOwnClip(LayoutBox box)
    {
        if (!box.IsScrollContainer || box.Style.OverflowX == Overflow.Visible && box.Style.OverflowY == Overflow.Visible)
            return 0;
        var exact = Placed(box);
        var rect = Snap(exact);
        var exactPadding = new RectF(exact.X + box.Border.Left, exact.Y + box.Border.Top, box.PaddingBoxWidth, box.PaddingBoxHeight);
        // Blink clips to the overflow clip rect pixel-snapped (its edges rounded).
        static float Round(float v) => MathF.Round(v, MidpointRounding.AwayFromZero);
        var padding = RectF.FromEdges(Round(exactPadding.Left), Round(exactPadding.Top), Round(exactPadding.Right), Round(exactPadding.Bottom));
        var radii = BorderRadii(box, rect.Size).Shrink(box.Border.Top, box.Border.Right, box.Border.Bottom, box.Border.Left);
        // overflow-x: visible with overflow-y: hidden still clips both (they compute to auto); clip clips one axis only.
        if (box.Style.OverflowX == Overflow.Clip && box.Style.OverflowY == Overflow.Visible)
        {
            padding = new RectF(padding.X, -1e6f, padding.Width, 2e6f);
            exactPadding = new RectF(exactPadding.X, -1e6f, exactPadding.Width, 2e6f);
        }
        else if (box.Style.OverflowY == Overflow.Clip && box.Style.OverflowX == Overflow.Visible)
        {
            padding = new RectF(-1e6f, padding.Y, 2e6f, padding.Height);
            exactPadding = new RectF(-1e6f, exactPadding.Y, 2e6f, exactPadding.Height);
        }
        _canvas.PushClip(padding, radii, exactPadding);
        return 1;
    }

    /// <summary>The clips of the scroll containers between a layer and its stacking context (inclusive), outermost first.</summary>
    int PushClipChain(LayoutBox box, LayoutBox contextRoot)
    {
        var chain = new List<LayoutBox>();
        for (var container = box.Container; container is not null; container = container.Container)
        {
            if (container.IsScrollContainer)
                chain.Add(container);
            if (container == contextRoot)
                break;
        }
        // The context root's own clip is already pushed by its context.
        chain.Remove(contextRoot);
        var count = 0;
        for (var i = chain.Count - 1; i >= 0; i--)
            count += PushOwnClip(chain[i]);
        return count;
    }

    void PopClips(int count)
    {
        for (var i = 0; i < count; i++)
            _canvas.PopClip();
    }

    // ---------- Phases ----------

    /// <summary>Backgrounds and borders of in-flow block-level descendants, in tree order (Appendix E step 4).</summary>
    void PaintBlockBackgrounds(LayoutBox box, bool includeSelf)
    {
        if (includeSelf)
            PaintDecorations(box);
        if (box.ChildrenInline)
            return;
        foreach (var child in PaintOrderChildren(box))
        {
            if (!InNormalFlow(child) || child.Type is BoxType.Text or BoxType.Inline or BoxType.LineBreak || PaintsAtomically(child))
                continue;
            PaintDecorations(child);
            var clips = PushOwnClip(child);
            PaintBlockBackgrounds(child, includeSelf: false);
            PopClips(clips);
        }
    }

    /// <summary>Inline content, replaced content and atomic inlines of in-flow descendants (Appendix E step 7).</summary>
    void PaintForeground(LayoutBox box)
    {
        if (box.Type == BoxType.Replaced)
            PaintReplaced(box);
        if (box.Pseudo == PseudoKind.Marker && box.Inline is null && box.Style.ListStyleType is "disclosure-open" or "disclosure-closed" && box.Style.Visibility == Visibility.Visible)
            PaintDisclosure(box, box.Style.ListStyleType == "disclosure-open");
        if (box.Marker is { } marker)
            PaintMarker(marker);
        if (box.Inline is { } inline)
        {
            PaintLines(box, inline);
            return;
        }
        foreach (var child in PaintOrderChildren(box))
        {
            if (!InNormalFlow(child) || child.Type is BoxType.Text or BoxType.Inline or BoxType.LineBreak)
                continue;
            if (PaintsAtomically(child))
            {
                PaintPseudoContext(child);
                continue;
            }
            var clips = PushOwnClip(child);
            PaintForeground(child);
            PopClips(clips);
            PaintScrollbars(child);
        }
    }

    void PaintOutlines(LayoutBox box, bool includeSelf)
    {
        if (includeSelf)
            PaintOutline(box);
        if (box.Inline is { } inline)
        {
            foreach (var line in inline.Lines)
                foreach (var item in line.Items)
                    if (item.Kind == LineItemKind.InlineBox && item.Box is { } inlineBox && inlineBox.Fragments?.FirstOrDefault() == item.Fragment)
                        PaintInlineOutline(box, inlineBox);
            return;
        }
        foreach (var child in PaintOrderChildren(box))
        {
            if (!InNormalFlow(child) || child.Type is BoxType.Text or BoxType.Inline or BoxType.LineBreak || PaintsAtomically(child))
                continue;
            // A box's own outline isn't clipped by its own overflow; its descendants' are.
            PaintOutline(child);
            var clips = PushOwnClip(child);
            PaintOutlines(child, includeSelf: false);
            PopClips(clips);
        }
    }

    // ---------- Backgrounds and borders ----------

    /// <summary>Whether a box's padding box (its overflow clip) has rounded corners.</summary>
    internal static bool HasInnerRadius(LayoutBox box)
    {
        var s = box.Style;
        var size = new Vector2(box.Width, box.Height);
        var radii = ResolveRadii(s.RadiusTopLeft, s.RadiusTopRight, s.RadiusBottomRight, s.RadiusBottomLeft, size).Shrink(box.Border.Top, box.Border.Right, box.Border.Bottom, box.Border.Left);
        return !radii.IsZero;
    }

    CornerRadii BorderRadii(LayoutBox box, Vector2 size)
    {
        var s = box.Style;
        return ResolveRadii(s.RadiusTopLeft, s.RadiusTopRight, s.RadiusBottomRight, s.RadiusBottomLeft, size);
    }

    static CornerRadii ResolveRadii(Corner tl, Corner tr, Corner br, Corner bl, Vector2 size)
    {
        Vector2 R(Corner c) => new(MathF.Max(0, c.X.ResolveOr(size.X, 0)), MathF.Max(0, c.Y.ResolveOr(size.Y, 0)));
        return new CornerRadii(R(tl), R(tr), R(br), R(bl)).Fit(size);
    }

    static CanvasBorderStyle ToCanvas(BorderLineStyle style) => style switch
    {
        BorderLineStyle.None or BorderLineStyle.Hidden => CanvasBorderStyle.None,
        BorderLineStyle.Dashed or BorderLineStyle.Dotted => CanvasBorderStyle.Dashed,
        _ => CanvasBorderStyle.Solid,
    };

    static BorderSides SidesOf(ComputedStyle s, bool left = true, bool right = true) => new(
        new BorderSide(s.BorderTop, s.BorderTopColor ?? s.Color, ToCanvas(s.BorderTopStyle)),
        new BorderSide(right ? s.BorderRight : 0, s.BorderRightColor ?? s.Color, ToCanvas(s.BorderRightStyle)),
        new BorderSide(s.BorderBottom, s.BorderBottomColor ?? s.Color, ToCanvas(s.BorderBottomStyle)),
        new BorderSide(left ? s.BorderLeft : 0, s.BorderLeftColor ?? s.Color, ToCanvas(s.BorderLeftStyle)));

    /// <summary>A box's own decorations: outer shadows, background, inset shadows, border.</summary>
    void PaintDecorations(LayoutBox box)
    {
        if (box.Type is BoxType.Text or BoxType.LineBreak or BoxType.Inline)
            return;
        var s = box.Style;
        if (s.Visibility != Visibility.Visible)
            return;
        var rect = Placed(box);
        if (rect.Width <= 0 && rect.Height <= 0)
            return;
        var properties = PropertiesOf(box);
        if (properties is not null)
            _canvas.UseState(properties.BorderBox, box);
        var radii = BorderRadii(box, rect.Size);
        // The root's (or the propagating body's) background is the canvas's.
        var isRoot = box.Element is { Parent: null } && box.Pseudo == PseudoKind.None;
        // A scroller whose background goes into its scrolling contents doesn't paint it opaque here.
        _canvas.NotOpaque = properties is { ScrollTranslation: not null } && (properties.BackgroundLocation & BackgroundPaintLocation.BorderBox) == 0;
        _canvas.BeginItem(boxDecoration: true, InkOverflowRect(s, rect));
        PaintBoxDecorations(box, s, rect, radii, SidesOf(s), box == _backgroundSource || isRoot);
        _canvas.EndItem();
        _canvas.NotOpaque = false;
        // An inline SVG's shapes decide where it takes input (HitSvg), which measures from layout's
        // place: its region is recorded there, the shift in its transform (Blink hit tests through the
        // paint property trees, rounded paint offsets and all). One whose shapes overflow takes input
        // where they are.
        if (box.Type == BoxType.Replaced && box.Element?.Tag == "svg")
        {
            _canvas.PushTransform(Matrix3x2.CreateTranslation(_shift));
            var layoutRect = rect.Offset(-_shift);
            if (s.OverflowX == Overflow.Visible)
                Hit(box, SvgOverflow(box), CornerRadii.Zero, borderBox: layoutRect);
            else
                Hit(box, layoutRect, radii);
            _canvas.PopTransform();
        }
        else
            Hit(box, rect, radii);
        if (properties is not null && box.IsScrollContainer)
            RecordScrolling(box, properties);
    }

    /// <summary>
    /// A box's self ink overflow (PhysicalBoxFragment::ComputeSelfInkOverflow), its display items'
    /// visual rect: the border box with its outer box shadows' reach (3 standard deviations of the
    /// blur, which is half the blur radius, plus the spread) and its outline, enclosed in whole pixels.
    /// </summary>
    internal static RectF InkOverflowRect(ComputedStyle s, RectF borderBox)
    {
        float left = 0, top = 0, right = 0, bottom = 0;
        foreach (var shadow in s.BoxShadows)
        {
            if (shadow.Inset)
                continue;
            var reach = shadow.Blur * 0.5f * 3 + shadow.Spread;
            left = MathF.Max(left, reach - shadow.OffsetX);
            top = MathF.Max(top, reach - shadow.OffsetY);
            right = MathF.Max(right, reach + shadow.OffsetX);
            bottom = MathF.Max(bottom, reach + shadow.OffsetY);
        }
        // PhysicalBoxStrut::Enclosing: whole layout units outward.
        var rect = RectF.FromEdges(borderBox.Left - MathF.Ceiling(left), borderBox.Top - MathF.Ceiling(top), borderBox.Right + MathF.Ceiling(right), borderBox.Bottom + MathF.Ceiling(bottom));
        if (s.OutlineStyle != BorderLineStyle.None && s.OutlineWidth > 0)
        {
            var extent = MathF.Max(0, s.OutlineWidth + s.OutlineOffset);
            rect = Gfx.Union(rect, borderBox.Inflate(extent));
        }
        return rect;
    }

    /// <summary>
    /// What Blink paints for a scroller in its background phase after its decorations: the scroll
    /// hit test, the scrollbars (where the engine draws them later), and the background again in the
    /// scrolling contents when it goes there.
    /// </summary>
    void RecordScrolling(LayoutBox box, BoxProperties properties)
    {
        var s = box.Style;
        var scrollTranslation = properties.ScrollTranslation;
        if (scrollTranslation is not null)
            _canvas.RecordScrollHitTest(properties.BorderBox, scrollTranslation, box, scrollTranslation.Scroll!.ContainerRect);
        // Classic (non-overlay) scrollbars paint in the background phase, before the scrolling background.
        if (box.Element is { } element && (box.VerticalScrollbar > 0 || box.HorizontalScrollbar > 0))
        {
            var (vertical, horizontal) = ScrollbarGeometry.For(box, Snap(Placed(box)), element.LayoutScroll.X, element.LayoutScroll.Y, _document.DeviceScale);
            if (vertical is { } v)
                _canvas.RecordScrollbar(properties.BorderBox, scrollTranslation, box, v.Bar, false);
            if (horizontal is { } h)
                _canvas.RecordScrollbar(properties.BorderBox, scrollTranslation, box, h.Bar, false);
        }
        // The scrolling contents' hit test data (an auto or scroll box), with the background when it goes there.
        var inContents = (properties.BackgroundLocation & BackgroundPaintLocation.Contents) != 0;
        if (s.OverflowX is Overflow.Auto or Overflow.Scroll || s.OverflowY is Overflow.Auto or Overflow.Scroll || inContents)
        {
            var border = box.Border;
            var rect = Placed(box);
            var contents = scrollTranslation?.Scroll!.ContentsRect.Offset(_shift) ?? new RectF(rect.X + border.Left, rect.Y + border.Top, box.PaddingBoxWidth, box.PaddingBoxHeight);
            var fill = inContents && s.BackgroundColor.A > 0 ? new Brush { Color = s.BackgroundColor } : null;
            _canvas.RecordScrollingBackground(properties.Contents, box, Snap(contents.Inflate(border.Left, border.Top, border.Right, border.Bottom)), fill);
        }
    }

    /// <summary>
    /// Chromium paints box decorations on whole device pixels: each edge rounds to the nearest pixel
    /// (its pixel-snapped rect), so 1 px borders stay crisp at fractional layout positions.
    /// </summary>
    RectF Snap(RectF rect) => SnapRect(rect, _canvas.Scale);

    /// <summary>A rect with each edge on the nearest device pixel.</summary>
    internal static RectF SnapRect(RectF rect, float scale)
    {
        float S(float v) => MathF.Round(v * scale, MidpointRounding.AwayFromZero) / scale;
        return RectF.FromEdges(S(rect.Left), S(rect.Top), S(rect.Right), S(rect.Bottom));
    }

    void PaintBoxDecorations(LayoutBox box, ComputedStyle s, RectF rect, CornerRadii radii, BorderSides sides, bool skipBackground)
    {
        rect = Snap(rect);
        // Outer shadows: the first one listed is on top.
        for (var i = s.BoxShadows.Length - 1; i >= 0; i--)
        {
            var shadow = s.BoxShadows[i];
            if (!shadow.Inset)
                _canvas.DrawBoxShadow(rect, radii, new BoxShadow(new Vector2(shadow.OffsetX, shadow.OffsetY), shadow.Blur, shadow.Spread, shadow.Color));
        }
        // A control with its native look: the theme paints instead of the CSS background and border.
        var look = box.Pseudo == PseudoKind.None && box.Element is { } control ? ControlAppearance.Effective(control, s) : ControlLook.None;
        if (look is not (ControlLook.None or ControlLook.MenulistButton or ControlLook.Listbox or ControlLook.Meter))
        {
            PaintTheme(box, box.Element!, look, rect);
            PaintInsetShadows(s, rect, radii);
            return;
        }
        PaintStyledFill(s, rect, radii, sides, skipBackground);
        // A styled select keeps the theme's arrow.
        if (look == ControlLook.MenulistButton)
            PaintMenuListArrow(box, s, rect);
    }

    /// <summary>A box with a style that belongs to no layout box (a control's part): shadows, background and border.</summary>
    void PaintStyledBox(ComputedStyle s, RectF rect, CornerRadii radii, BorderSides sides)
    {
        rect = Snap(rect);
        for (var i = s.BoxShadows.Length - 1; i >= 0; i--)
        {
            var shadow = s.BoxShadows[i];
            if (!shadow.Inset)
                _canvas.DrawBoxShadow(rect, radii, new BoxShadow(new Vector2(shadow.OffsetX, shadow.OffsetY), shadow.Blur, shadow.Spread, shadow.Color));
        }
        PaintStyledFill(s, rect, radii, sides, false);
    }

    /// <summary>The background (colour and layers), inset shadows and border.</summary>
    void PaintStyledFill(ComputedStyle s, RectF rect, CornerRadii radii, BorderSides sides, bool skipBackground)
    {
        var hasInset = s.BoxShadows.Any(sh => sh.Inset);
        var background = skipBackground ? Color.Transparent : s.BackgroundColor;
        var layers = skipBackground ? [] : s.Backgrounds;
        if (layers.Length == 0 && !hasInset)
        {
            // The common case: one primitive for background and border.
            if (background.A > 0 || sides.IsVisible)
                _canvas.DrawBox(rect, background.A > 0 ? Brush.Solid(background) : null, radii, sides);
            return;
        }
        if (background.A > 0)
            _canvas.DrawBox(rect, Brush.Solid(background), radii, Border.None);
        if (layers.Length > 0)
        {
            var padding = new RectF(rect.X + s.BorderLeft, rect.Y + s.BorderTop, rect.Width - s.BorderLeft - s.BorderRight, rect.Height - s.BorderTop - s.BorderBottom);
            PaintBackgroundLayers(layers, rect, padding, radii);
        }
        if (hasInset)
            PaintInsetShadows(s, rect, radii);
        if (sides.IsVisible)
            _canvas.DrawBox(rect, null, radii, sides);
    }

    void PaintInsetShadows(ComputedStyle s, RectF rect, CornerRadii radii)
    {
        if (!s.BoxShadows.Any(sh => sh.Inset))
            return;
        var padding = new RectF(rect.X + s.BorderLeft, rect.Y + s.BorderTop, rect.Width - s.BorderLeft - s.BorderRight, rect.Height - s.BorderTop - s.BorderBottom);
        var innerRadii = radii.Shrink(s.BorderTop, s.BorderRight, s.BorderBottom, s.BorderLeft);
        for (var i = s.BoxShadows.Length - 1; i >= 0; i--)
        {
            var shadow = s.BoxShadows[i];
            if (shadow.Inset)
                _canvas.DrawBoxShadow(padding, innerRadii, new BoxShadow(new Vector2(shadow.OffsetX, shadow.OffsetY), shadow.Blur, shadow.Spread, shadow.Color, Inset: true));
        }
    }

    /// <summary>Background images, bottom layer first, positioned in the padding box and painted within the border box.</summary>
    void PaintBackgroundLayers(BackgroundLayer[] layers, RectF borderBox, RectF positioning, CornerRadii radii)
    {
        if (!_canvas.Drawing)
            return;
        _canvas.PushClip(borderBox, radii);
        var viewportArea = new RectF(0, 0, _document.Layout.Viewport.PaddingBoxWidth, _document.Layout.Viewport.PaddingBoxHeight);
        for (var i = layers.Length - 1; i >= 0; i--)
        {
            var layer = layers[i];
            // background-attachment: fixed positions the layer against the viewport.
            var area = layer.Fixed ? viewportArea : positioning;
            var svgImage = layer.Image.Kind == ImageKind.Url ? SvgImage(layer.Image.Url) : null;
            var natural = svgImage is { } svgSource ? svgSource.Size : layer.Image.Kind == ImageKind.Url ? _document.Layout.Context.ImageSize?.Invoke(layer.Image.Url) : null;
            var ratio = svgImage is { } withRatio ? withRatio.Ratio : natural is { X: > 0, Y: > 0 } n ? n.X / n.Y : 0;
            if (layer.Image.Kind == ImageKind.Url && svgImage is null && (natural is null || ImageTexture?.Invoke(layer.Image.Url) is null))
                continue;
            var tile = TileSize(layer, area.Size, natural, ratio);
            if (tile.X <= 0 || tile.Y <= 0)
                continue;
            var position = new Vector2(
                area.X + layer.PositionX.ResolveOr(area.Width - tile.X, 0),
                area.Y + layer.PositionY.ResolveOr(area.Height - tile.Y, 0));
            // Tiles cover the painting area (the border box) when they repeat.
            float startX = position.X, endX = position.X + tile.X, startY = position.Y, endY = position.Y + tile.Y;
            if (layer.RepeatX != BackgroundRepeat.NoRepeat)
            {
                startX = position.X - MathF.Ceiling((position.X - borderBox.X) / tile.X) * tile.X;
                endX = borderBox.Right;
            }
            if (layer.RepeatY != BackgroundRepeat.NoRepeat)
            {
                startY = position.Y - MathF.Ceiling((position.Y - borderBox.Y) / tile.Y) * tile.Y;
                endY = borderBox.Bottom;
            }
            var count = 0;
            for (var y = startY; y < endY - 0.001f && count < 4096; y += tile.Y)
                for (var x = startX; x < endX - 0.001f && count < 4096; x += tile.X, count++)
                {
                    var tileRect = new RectF(x, y, tile.X, tile.Y);
                    if (svgImage is { } svg)
                    {
                        if (FilterImage(layer.Image.Url, svg.Svg, tileRect) is { } filtered)
                            _canvas.DrawImage(filtered, tileRect);
                        else
                            PaintSvgTree(svg.Svg, tileRect, Color.Black, clip: true);
                    }
                    else if (layer.Image.Kind == ImageKind.Url)
                        _canvas.DrawImage(ImageTexture!(layer.Image.Url)!.Value, tileRect);
                    else if (GradientBrush(layer.Image, tile) is { } brush)
                        _canvas.DrawBox(tileRect, brush, CornerRadii.Zero, Border.None);
                }
        }
        _canvas.PopClip();
    }

    /// <summary>A background image's tile size (CSS Backgrounds 3 §3.9), from its natural size and/or ratio.</summary>
    static Vector2 TileSize(BackgroundLayer layer, Vector2 area, Vector2? natural, float ratio)
    {
        Vector2 Contain(bool cover)
        {
            var areaRatio = area.Y > 0 ? area.X / area.Y : 1;
            var widthLimited = cover ? ratio < areaRatio : ratio > areaRatio;
            return widthLimited ? new Vector2(area.X, area.X / ratio) : new Vector2(area.Y * ratio, area.Y);
        }
        switch (layer.SizeKeyword)
        {
            case "cover" when ratio > 0:
                return Contain(cover: true);
            case "contain" when ratio > 0:
                return Contain(cover: false);
            case "cover":
            case "contain":
                return area;
        }
        var width = layer.SizeX.IsAuto ? float.NaN : layer.SizeX.ResolveOr(area.X, float.NaN);
        var height = layer.SizeY.IsAuto ? float.NaN : layer.SizeY.ResolveOr(area.Y, float.NaN);
        // No natural size but a ratio: sized as for contain.
        if (float.IsNaN(width) && float.IsNaN(height))
            return natural ?? (ratio > 0 ? Contain(cover: false) : area);
        if (float.IsNaN(width))
            width = ratio > 0 ? height * ratio : natural?.X ?? area.X;
        if (float.IsNaN(height))
            height = ratio > 0 ? width / ratio : natural?.Y ?? area.Y;
        return new Vector2(width, height);
    }

    /// <summary>A CSS gradient for a tile of this size, as a canvas brush (geometry in the tile's own box).</summary>
    static Brush? GradientBrush(ImageValue image, Vector2 size)
    {
        if (image.Stops.Length == 0)
            return null;
        float length;
        Brush brush;
        if (image.Kind == ImageKind.LinearGradient)
        {
            var angle = image.UsesCorner ? Brush.CssCornerAngle(size, image.CornerRight, image.CornerBottom) : image.Angle;
            var radians = angle * MathF.PI / 180f;
            length = MathF.Abs(size.X * MathF.Sin(radians)) + MathF.Abs(size.Y * MathF.Cos(radians));
            brush = Brush.CssLinear(size, angle, ResolveStops(image.Stops, length));
        }
        else
        {
            var center = new Vector2(image.CenterX.ResolveOr(size.X, size.X / 2), image.CenterY.ResolveOr(size.Y, size.Y / 2));
            var radii = RadialRadii(image, size, center);
            length = radii.X;
            brush = Brush.Radial(center, radii, ResolveStops(image.Stops, MathF.Max(radii.X, 1e-3f)));
        }
        return brush with { Repeat = image.Repeating };
    }

    /// <summary>The ending shape's radii (CSS Images 3 §3.2.2).</summary>
    static Vector2 RadialRadii(ImageValue image, Vector2 size, Vector2 c)
    {
        if (image.SizeKeyword.Length == 0)
        {
            var rx = image.RadiusX.ResolveOr(size.X, 0);
            var ry = image.Circle ? rx : image.RadiusY.ResolveOr(size.Y, 0);
            return new Vector2(MathF.Max(rx, 0), MathF.Max(ry, 0));
        }
        var left = c.X;
        var right = size.X - c.X;
        var top = c.Y;
        var bottom = size.Y - c.Y;
        var closestX = MathF.Min(MathF.Abs(left), MathF.Abs(right));
        var closestY = MathF.Min(MathF.Abs(top), MathF.Abs(bottom));
        var farthestX = MathF.Max(MathF.Abs(left), MathF.Abs(right));
        var farthestY = MathF.Max(MathF.Abs(top), MathF.Abs(bottom));
        switch (image.SizeKeyword)
        {
            case "closest-side":
                return image.Circle ? new Vector2(MathF.Min(closestX, closestY)) : new Vector2(closestX, closestY);
            case "farthest-side":
                return image.Circle ? new Vector2(MathF.Max(farthestX, farthestY)) : new Vector2(farthestX, farthestY);
            case "closest-corner":
            {
                if (image.Circle)
                    return new Vector2(MathF.Sqrt(closestX * closestX + closestY * closestY));
                // An ellipse through the corner with the closest sides' ratio.
                var r = closestY > 0 ? closestX / closestY : 1;
                var ry = MathF.Sqrt(closestY * closestY + closestX * closestX / (r * r));
                return new Vector2(ry * r, ry);
            }
            default:
            {
                if (image.Circle)
                    return new Vector2(MathF.Sqrt(farthestX * farthestX + farthestY * farthestY));
                var r = farthestY > 0 ? farthestX / farthestY : 1;
                // The farthest corner, with the farthest sides' aspect ratio (√2 for the default ellipse).
                var ry = MathF.Sqrt(farthestY * farthestY + farthestX * farthestX / (r * r));
                return new Vector2(ry * r, ry);
            }
        }
    }

    /// <summary>Colour stops with positions along a gradient line of this length (CSS Images 3 §3.4.3: auto positions spread evenly, never going backwards).</summary>
    static GradientStop[] ResolveStops(ColorStop[] stops, float length)
    {
        var count = stops.Length;
        var offsets = new float[count];
        var known = new bool[count];
        for (var i = 0; i < count; i++)
        {
            if (stops[i].Position is { } p)
            {
                offsets[i] = length > 0 ? p.ResolveOr(length, 0) / length : 0;
                known[i] = true;
            }
        }
        if (!known[0])
        {
            offsets[0] = 0;
            known[0] = true;
        }
        if (!known[count - 1])
        {
            offsets[count - 1] = MathF.Max(1, offsets[..(count - 1)].Where((_, i) => known[i]).DefaultIfEmpty(0).Max());
            known[count - 1] = true;
        }
        for (var i = 1; i < count; i++)
            if (known[i] && offsets[i] < offsets[i - 1])
                offsets[i] = offsets[i - 1];
        for (var i = 1; i < count; i++)
        {
            if (known[i])
                continue;
            var j = i;
            while (!known[j])
                j++;
            var start = offsets[i - 1];
            var step = (offsets[j] - start) / (j - i + 1);
            for (var k = i; k < j; k++)
            {
                offsets[k] = start + step * (k - i + 1);
                known[k] = true;
            }
        }
        return stops.Select((s, i) => new GradientStop(offsets[i], s.Color)).ToArray();
    }

    // ---------- Root background ----------

    /// <summary>The root's background paints the whole canvas; the body's does when the root has none (CSS Backgrounds 3 §2.11.2).</summary>
    void PaintCanvasBackground(LayoutBox viewport, LayoutBox root)
    {
        // The root scroller's hit test, then the document background in its scrolling contents.
        if (PropertiesOf(viewport) is { ScrollTranslation: { } rootScroll } view)
        {
            _canvas.UseState(view.BorderBox, viewport);
            _canvas.RecordScrollHitTest(view.BorderBox, rootScroll, viewport, rootScroll.Scroll!.ContainerRect);
            _canvas.UseState(view.Contents, viewport);
        }
        _canvas.BeginItem(boxDecoration: true);
        PaintCanvasBackgroundItem(viewport, root);
        _canvas.EndItem();
    }

    void PaintCanvasBackgroundItem(LayoutBox viewport, LayoutBox root)
    {
        _backgroundSource = null;
        var source = root;
        if (root.Style.BackgroundColor.A <= 0 && root.Style.Backgrounds.Length == 0
            && root.Children.FirstOrDefault(c => c.Element?.Tag == "body") is { } body)
            source = body;
        var s = source.Style;
        // The document's background rect: its scrolling contents, the viewport at least.
        var scroll = _document.Layout.ViewportScroll;
        var canvasRect = Snap(new RectF(0, 0, MathF.Max(viewport.Width, viewport.ScrollWidth), MathF.Max(viewport.Height, viewport.ScrollHeight)).Offset(-scroll));
        // Under everything, the page's base colour (a browser's white), unless an opaque background covers it (ViewPainter).
        if (_document.BaseBackground.A > 0 && s.BackgroundColor.A < 1)
            _canvas.FillRect(canvasRect, _document.BaseBackground);
        if (s.BackgroundColor.A <= 0 && s.Backgrounds.Length == 0)
            return;
        _backgroundSource = source;
        if (s.BackgroundColor.A > 0)
            _canvas.FillRect(canvasRect, s.BackgroundColor);
        if (s.Backgrounds.Length > 0)
        {
            // Positioned against the root element's box.
            var rootRect = Placed(root);
            PaintBackgroundLayers(s.Backgrounds, canvasRect, new RectF(rootRect.X, rootRect.Y, rootRect.Width, MathF.Max(rootRect.Height, viewport.Height)), CornerRadii.Zero);
        }
    }

    // ---------- Transforms and filters ----------

    /// <summary>
    /// A box's transform about its transform-origin, in the space <paramref name="rect"/> (its border
    /// box) is in. The reference box is the border box, or the content box for transform-box:
    /// content-box and fill-box (Blink's UsedTransformBox for boxes with a CSS layout box).
    /// </summary>
    internal static Matrix3x2 TransformFor(LayoutBox box, RectF rect)
    {
        var s = box.Style;
        if (s.TransformBox is TransformBox.ContentBox or TransformBox.FillBox)
            rect = new RectF(rect.X + box.ContentLeft, rect.Y + box.ContentTop, box.ContentWidth, box.ContentHeight);
        return CssTransform(s, rect);
    }

    /// <summary>The CSS transform of a style with the reference box <paramref name="rect"/> (ComputedStyle::ApplyTransform).</summary>
    internal static Matrix3x2 CssTransform(ComputedStyle s, RectF rect)
    {
        var origin = new Vector2(rect.X + s.TransformOriginX.ResolveOr(rect.Width, rect.Width / 2), rect.Y + s.TransformOriginY.ResolveOr(rect.Height, rect.Height / 2));
        // translate, rotate and scale apply before transform (CSS Transforms 2 §7).
        var m = Matrix3x2.Identity;
        foreach (var function in s.Transform.Reverse())
            m *= function.ToMatrix(rect.Size);
        if (s.ScaleX != 1 || s.ScaleY != 1)
            m *= Matrix3x2.CreateScale(s.ScaleX, s.ScaleY);
        if (s.Rotate != 0)
            m *= Matrix3x2.CreateRotation(s.Rotate * MathF.PI / 180f);
        if (!s.TranslateX.IsZero || !s.TranslateY.IsZero)
            m *= Matrix3x2.CreateTranslation(s.TranslateX.ResolveOr(rect.Width, 0), s.TranslateY.ResolveOr(rect.Height, 0));
        return Matrix3x2.CreateTranslation(-origin) * m * Matrix3x2.CreateTranslation(origin);
    }

    static Filter ToCanvasFilter(FilterFunction f) => f.Kind switch
    {
        FilterFunctionKind.Blur => Filter.Blur(f.Amount),
        FilterFunctionKind.DropShadow => Filter.DropShadow(new Vector2(f.OffsetX, f.OffsetY), f.Amount, f.Color),
        FilterFunctionKind.Brightness => Filter.Brightness(f.Amount),
        FilterFunctionKind.Contrast => Filter.Contrast(f.Amount),
        FilterFunctionKind.Saturate => Filter.Saturate(f.Amount),
        FilterFunctionKind.Grayscale => Filter.Grayscale(f.Amount),
        FilterFunctionKind.Sepia => Filter.Sepia(f.Amount),
        FilterFunctionKind.HueRotate => Filter.HueRotate(f.Amount),
        FilterFunctionKind.Invert => Filter.Invert(f.Amount),
        _ => Filter.Opacity(f.Amount),
    };
}

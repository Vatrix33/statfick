using System.Numerics;
using Statventure.Text;
using Statventure.UI.Style;

namespace Statventure.UI.Layout;

/// <summary>What a box is laid out in: the space its containing block offers.</summary>
public struct Space : IEquatable<Space>
{
    /// <summary>The width auto widths fill (the containing block's content width).</summary>
    public float AvailableWidth;
    /// <summary>What percentages of width, margins and padding refer to.</summary>
    public float PercentWidth;
    /// <summary>What percentage heights refer to; NaN when that height is indefinite.</summary>
    public float PercentHeight;
    /// <summary>A border-box width the parent imposes (flexed or stretched); NaN when none.</summary>
    public float FixedWidth;
    public float FixedHeight;
    /// <summary>Auto width shrinks to fit the content (inline-blocks, absolutely positioned boxes, flex items).</summary>
    public bool ShrinkToFit;
    /// <summary>Measure the content's height: the box's height and min-height properties are ignored.</summary>
    public bool ContentHeightOnly;

    public static Space For(float width, float percentHeight = float.NaN) => new()
    {
        AvailableWidth = width,
        PercentWidth = width,
        PercentHeight = percentHeight,
        FixedWidth = float.NaN,
        FixedHeight = float.NaN,
    };

    // NaN equals NaN here (float.Equals): an indefinite height is the same space as another indefinite height.
    public readonly bool Equals(Space other) =>
        AvailableWidth.Equals(other.AvailableWidth) && PercentWidth.Equals(other.PercentWidth) && PercentHeight.Equals(other.PercentHeight)
        && FixedWidth.Equals(other.FixedWidth) && FixedHeight.Equals(other.FixedHeight) && ShrinkToFit == other.ShrinkToFit && ContentHeightOnly == other.ContentHeightOnly;

    public override readonly bool Equals(object? obj) => obj is Space other && Equals(other);

    public override readonly int GetHashCode() => HashCode.Combine(AvailableWidth, PercentWidth, PercentHeight, FixedWidth, FixedHeight, ShrinkToFit, ContentHeightOnly);
}

/// <summary>The environment of a layout: fonts, the viewport, images, scrollbar sizes.</summary>
public sealed class LayoutContext
{
    public required FontLibrary Fonts { get; init; }
    public float ViewportWidth { get; set; }
    public float ViewportHeight { get; set; }
    /// <summary>The natural size of an image by url (null while unknown).</summary>
    public Func<string, Vector2?>? ImageSize { get; set; }
    /// <summary>
    /// Device pixels per window unit. Scrollbars scale with it alone (Blink's ScaleFromDIP), not
    /// with the page zoom.
    /// </summary>
    public float DeviceScale { get; set; } = 1;
    /// <summary>Scrollbar thickness for scrollbar-width: auto and thin (Chromium's Fluent 15 px, two thirds thin), in device pixels.</summary>
    public float Scrollbar => ScrollbarGeometry.Thickness(false, DeviceScale);
    public float ThinScrollbar => ScrollbarGeometry.Thickness(true, DeviceScale);
    /// <summary>
    /// The scrollbars (vertical, horizontal thickness) an auto scroller starts with when it has no
    /// layout yet (otherwise it starts with those of its last layout). Tests use it to compare a
    /// fresh layout with an updated one.
    /// </summary>
    internal Func<LayoutBox, Vector2?>? ScrollbarHint { get; set; }

    public float ScrollbarThickness(ComputedStyle style) => style.ScrollbarWidth switch
    {
        "none" => 0,
        "thin" => ThinScrollbar,
        _ => Scrollbar,
    };
}

/// <summary>
/// Lays out a box tree (CSS 2 visual formatting with CSS Box Sizing, Flexbox and Grid): block flow
/// with margin collapsing, inline formatting, flex and grid containers, absolutely and fixed
/// positioned boxes, relative and sticky offsets, scroll containers and their scrollbars, and the
/// intrinsic (min-content / max-content) sizes all of these need.
/// </summary>
public sealed partial class LayoutEngine
{
    public LayoutContext Context { get; }
    /// <summary>How many box layouts have run (not kept from before): the work incremental layout does.</summary>
    public long BoxesLaidOut { get; private set; }
    /// <summary>The initial containing block: the viewport (it scrolls the document).</summary>
    public LayoutBox Viewport { get; }
    /// <summary>How far the document is scrolled (the viewport's scroll offset).</summary>
    public Vector2 ViewportScroll
    {
        get => Viewport.DocumentScroll;
        set => Viewport.DocumentScroll = value;
    }

    /// <summary>The overflow the viewport scrolls with (CSS Overflow 3 §3.3).</summary>
    public (Overflow X, Overflow Y) ViewportOverflowValues => ViewportOverflow(Viewport.Children.FirstOrDefault());

    readonly InlineLayout _inline;
    readonly FlexLayout _flex;
    readonly GridLayout _grid;

    public LayoutEngine(LayoutContext context)
    {
        Context = context;
        Viewport = new LayoutBox(BoxType.Block, new ComputedStyle { Display = Display.Block }) { IsAnonymous = true, IsViewport = true };
        _inline = new InlineLayout(this);
        _flex = new FlexLayout(this);
        _grid = new GridLayout(this);
    }

    internal InlineLayout Inline => _inline;

    /// <summary>Lays out the tree under the viewport (the root box is its only child).</summary>
    public void Layout()
    {
        var viewport = Viewport;
        viewport.Width = Context.ViewportWidth;
        viewport.Height = Context.ViewportHeight;
        viewport.X = viewport.Y = 0;
        var root = viewport.Children.FirstOrDefault();
        // The viewport scrolls the document with the root's overflow (or the body's, when the root's is visible).
        var (overflowX, overflowY) = ViewportOverflow(root);
        var thickness = root is null ? 0 : Context.ScrollbarThickness(root.Style);
        var scrollbars = new Scrollbars(viewport, overflowX, overflowY, thickness, Context);
        for (var pass = 0; pass < 4; pass++)
        {
            LayoutRoot(root);
            var overflow = ScrollableOverflow(viewport);
            viewport.ScrollWidth = MathF.Max(viewport.PaddingBoxWidth, overflow.X);
            viewport.ScrollHeight = MathF.Max(viewport.PaddingBoxHeight, overflow.Y);
            if (!scrollbars.Update(viewport, overflow))
                break;
        }
        viewport.HasLayout = true;
    }

    /// <summary>
    /// Scrollbars of an overflow: auto scroller (and overflow: scroll). An auto scroller keeps the
    /// scrollbars of its last layout and drops one only when the content fits without it, as Blink
    /// does (so an unchanged scroller lays out once, with its children in the same space as before);
    /// a scrollbar added in this layout stays (no flip-flopping).
    /// </summary>
    struct Scrollbars
    {
        readonly Overflow _x, _y;
        readonly float _thickness;
        bool _addedVertical, _addedHorizontal;

        public Scrollbars(LayoutBox box, Overflow x, Overflow y, float thickness, LayoutContext context, bool stableGutter = false)
        {
            _x = x;
            _y = y;
            _thickness = thickness;
            var previous = box.HasLayout ? new Vector2(box.VerticalScrollbar, box.HorizontalScrollbar) : context.ScrollbarHint?.Invoke(box) ?? Vector2.Zero;
            box.VerticalScrollbar = y == Overflow.Scroll || (stableGutter && y is Overflow.Auto or Overflow.Hidden) || (y == Overflow.Auto && previous.X > 0) ? thickness : 0;
            box.HorizontalScrollbar = x == Overflow.Scroll || (x == Overflow.Auto && previous.Y > 0) ? thickness : 0;
            if (stableGutter)
                _y = Overflow.Scroll;
        }

        /// <summary>Adds or drops auto scrollbars after a layout; true when they changed (lay out again).</summary>
        public bool Update(LayoutBox box, Vector2 overflow)
        {
            if (_thickness == 0)
                return false;
            // Both checks against the layout just done (a scrollbar changes the room for the other).
            var overflowsY = overflow.Y > box.PaddingBoxHeight + 0.01f;
            var overflowsX = overflow.X > box.PaddingBoxWidth + 0.01f;
            var changed = false;
            if (_y == Overflow.Auto)
            {
                if (box.VerticalScrollbar == 0 && overflowsY)
                {
                    box.VerticalScrollbar = _thickness;
                    _addedVertical = changed = true;
                }
                else if (box.VerticalScrollbar > 0 && !overflowsY && !_addedVertical)
                {
                    box.VerticalScrollbar = 0;
                    changed = true;
                }
            }
            if (_x == Overflow.Auto)
            {
                if (box.HorizontalScrollbar == 0 && overflowsX)
                {
                    box.HorizontalScrollbar = _thickness;
                    _addedHorizontal = changed = true;
                }
                else if (box.HorizontalScrollbar > 0 && !overflowsX && !_addedHorizontal)
                {
                    box.HorizontalScrollbar = 0;
                    changed = true;
                }
            }
            return changed;
        }
    }

    void LayoutRoot(LayoutBox? root)
    {
        var viewport = Viewport;
        if (root is not null)
        {
            var space = Space.For(viewport.PaddingBoxWidth, viewport.PaddingBoxHeight);
            LayoutBlockLevel(root, space);
            // The root's margins don't collapse with anything.
            root.X = root.Margin.Left;
            root.Y = root.Margin.Top;
            root.Container = viewport;
            root.RelativeOffset = RelativeOffset(root.Style, viewport.PaddingBoxWidth, viewport.PaddingBoxHeight);
        }
        LayoutPositioned(viewport);
    }

    /// <summary>The overflow the viewport uses (CSS Overflow 3 §3.3): the root's, else the body's; visible means auto.</summary>
    static (Overflow X, Overflow Y) ViewportOverflow(LayoutBox? root)
    {
        if (root is null)
            return (Overflow.Auto, Overflow.Auto);
        var source = root.Style;
        if (source.OverflowX == Overflow.Visible && source.OverflowY == Overflow.Visible
            && root.Children.FirstOrDefault(c => c.Element?.Tag == "body") is { } body)
            source = body.Style;
        static Overflow Map(Overflow o) => o switch
        {
            Overflow.Visible => Overflow.Auto,
            Overflow.Clip => Overflow.Hidden,
            _ => o,
        };
        return (Map(source.OverflowX), Map(source.OverflowY));
    }

    /// <summary>Is this box's overflow the viewport's (the root, or the body whose overflow propagated)? Such boxes don't scroll themselves.</summary>
    internal static bool PropagatesOverflowToViewport(LayoutBox box)
    {
        if (box.Element is not { } element || box.Pseudo != PseudoKind.None)
            return false;
        if (element.Parent is null)
            return true;
        if (element.Tag == "body" && element.Parent is { Parent: null } html && html.Style is { OverflowX: Overflow.Visible, OverflowY: Overflow.Visible })
            return true;
        return false;
    }

    // ---------- Resolving lengths ----------

    /// <summary>A length in pixels as Blink's MinimumValueForLength gives it: truncated to a LayoutUnit.</summary>
    internal static float Resolve(Length length, float basis) => LayoutUnit.Truncate(length.ResolveOr(basis, 0));

    internal static bool IsDefinite(Length length, float basis) =>
        length.IsDefinite && (!length.HasPercent || !float.IsNaN(basis));

    /// <summary>Border, padding and (non-auto) margins; percentages refer to the containing block's width.</summary>
    internal static void ResolveEdges(LayoutBox box, float percentBasis)
    {
        var s = box.Style;
        var basis = float.IsNaN(percentBasis) ? 0 : percentBasis;
        box.Border = new Edges(s.BorderTop, s.BorderRight, s.BorderBottom, s.BorderLeft);
        box.Padding = new Edges(
            MathF.Max(0, Resolve(s.PaddingTop, basis)), MathF.Max(0, Resolve(s.PaddingRight, basis)),
            MathF.Max(0, Resolve(s.PaddingBottom, basis)), MathF.Max(0, Resolve(s.PaddingLeft, basis)));
        box.Margin = new Edges(
            s.MarginTop.IsAuto ? 0 : Resolve(s.MarginTop, basis), s.MarginRight.IsAuto ? 0 : Resolve(s.MarginRight, basis),
            s.MarginBottom.IsAuto ? 0 : Resolve(s.MarginBottom, basis), s.MarginLeft.IsAuto ? 0 : Resolve(s.MarginLeft, basis));
    }

    static float HorizontalEdges(LayoutBox box) => box.Border.Horizontal + box.Padding.Horizontal;
    static float VerticalEdges(LayoutBox box) => box.Border.Vertical + box.Padding.Vertical;

    /// <summary>A specified width as a border-box width; NaN when auto or not resolvable.</summary>
    internal float SpecifiedWidth(LayoutBox box, Length width, float basis, float available)
    {
        // A scrollbar takes its room from the content box: it isn't added to a specified width.
        var edges = HorizontalEdges(box);
        switch (width.Kind)
        {
            case LengthKind.Auto:
            case LengthKind.None:
                return float.NaN;
            case LengthKind.MinContent:
                return IntrinsicWidth(box, min: true);
            case LengthKind.MaxContent:
                return IntrinsicWidth(box, min: false);
            case LengthKind.FitContent:
                return MathF.Min(MathF.Max(IntrinsicWidth(box, true), available), IntrinsicWidth(box, false));
        }
        if (width.HasPercent && float.IsNaN(basis))
            return float.NaN;
        var value = LayoutUnit.Truncate(width.Resolve(basis));
        return box.Style.BoxSizing == BoxSizing.BorderBox ? MathF.Max(value, edges + box.VerticalScrollbar) : MathF.Max(value + edges, edges + box.VerticalScrollbar);
    }

    /// <summary>A specified height as a border-box height; NaN when auto or the percentage basis is indefinite.</summary>
    internal float SpecifiedHeight(LayoutBox box, Length height, float basis)
    {
        if (!height.IsDefinite || (height.HasPercent && float.IsNaN(basis)))
            return float.NaN;
        var edges = VerticalEdges(box);
        var value = LayoutUnit.Truncate(height.Resolve(basis));
        return box.Style.BoxSizing == BoxSizing.BorderBox ? MathF.Max(value, edges + box.HorizontalScrollbar) : MathF.Max(value + edges, edges + box.HorizontalScrollbar);
    }

    internal float ClampWidth(LayoutBox box, float width, float basis, float available)
    {
        var s = box.Style;
        var max = s.MaxWidth.IsNone ? float.NaN : SpecifiedWidth(box, s.MaxWidth, basis, available);
        if (!float.IsNaN(max))
            width = MathF.Min(width, max);
        var min = s.MinWidth.IsAuto ? float.NaN : SpecifiedWidth(box, s.MinWidth, basis, available);
        if (!float.IsNaN(min))
            width = MathF.Max(width, min);
        return MathF.Max(width, HorizontalEdges(box) + box.VerticalScrollbar);
    }

    internal float ClampHeight(LayoutBox box, float height, float basis)
    {
        var s = box.Style;
        var max = s.MaxHeight.IsNone ? float.NaN : SpecifiedHeight(box, s.MaxHeight, basis);
        if (!float.IsNaN(max))
            height = MathF.Min(height, max);
        var min = s.MinHeight.IsAuto ? float.NaN : SpecifiedHeight(box, s.MinHeight, basis);
        if (!float.IsNaN(min))
            height = MathF.Max(height, min);
        return MathF.Max(height, VerticalEdges(box) + box.HorizontalScrollbar);
    }

    internal static Vector2 RelativeOffset(ComputedStyle style, float cbWidth, float cbHeight)
    {
        if (style.Position != PositionKind.Relative)
            return Vector2.Zero;
        float x = 0, y = 0;
        if (!style.Left.IsAuto)
            x = LayoutUnit.Truncate(style.Left.ResolveOr(cbWidth, 0));
        else if (!style.Right.IsAuto)
            x = -LayoutUnit.Truncate(style.Right.ResolveOr(cbWidth, 0));
        if (!style.Top.IsAuto)
            y = LayoutUnit.Truncate(style.Top.ResolveOr(cbHeight, 0));
        else if (!style.Bottom.IsAuto)
            y = -LayoutUnit.Truncate(style.Bottom.ResolveOr(cbHeight, 0));
        if (float.IsNaN(y))
            y = 0;
        return new Vector2(x, y);
    }

    // ---------- Dispatch ----------

    /// <summary>A box's size and baselines from a layout.</summary>
    internal readonly record struct Measured(float Width, float Height, float? FirstBaseline, float? LastBaseline);

    static Measured MeasuredOf(LayoutBox box) => new(box.Width, box.Height, box.FirstBaseline, box.LastBaseline);

    /// <summary>
    /// The size a box would have in <paramref name="space"/>, for a container that lays it out again
    /// afterwards: kept from earlier measurements while the box is unchanged. The box itself may be
    /// left laid out in another space, so read the result, not the box.
    /// </summary>
    internal Measured Measure(LayoutBox box, in Space space)
    {
        if (box.IsClean && box.HasLayout)
        {
            if (box.LastSpace.Equals(space))
                return MeasuredOf(box);
            if (box.Measures is { } measures)
                foreach (var (measuredSpace, result) in measures)
                    if (measuredSpace.Equals(space))
                        return result;
        }
        LayoutBlockLevel(box, space);
        var measured = MeasuredOf(box);
        var list = box.Measures ??= [];
        if (list.Count >= 4)
            list.RemoveAt(0);
        list.Add((space, measured));
        return measured;
    }

    /// <summary>Lays out a block-level box (or any box placed by its parent as a unit): its size and its contents.</summary>
    internal void LayoutBlockLevel(LayoutBox box, in Space space)
    {
        // Unchanged and in the same space: the last layout stands (its parent places it).
        if (box.IsClean && box.HasLayout && box.LastSpace.Equals(space))
            return;
        BoxesLaidOut++;
        switch (box.Type)
        {
            case BoxType.Flex:
                _flex.Layout(box, space);
                break;
            case BoxType.Grid:
                _grid.Layout(box, space);
                break;
            case BoxType.Replaced:
                LayoutReplaced(box, space);
                break;
            default:
                LayoutBlock(box, space);
                break;
        }
        if (box.Type != BoxType.Block)
        {
            box.TopStrut = default;
            box.TopStrut.Append(box.Margin.Top);
            box.BottomStrut = default;
            box.BottomStrut.Append(box.Margin.Bottom);
            box.CollapsesThrough = false;
        }
        box.LastSpace = space;
        box.HasLayout = true;
        box.NeedsLayout = box.ChildNeedsLayout = false;
    }

    /// <summary>Lays out a box placed as a unit (inline-block, flex or grid item, positioned box) with a given space.</summary>
    internal void LayoutAtomic(LayoutBox box, in Space space) => LayoutBlockLevel(box, space);

    /// <summary>Does this box start a new block formatting context (its margins don't collapse with its children's)?</summary>
    internal static bool EstablishesFormattingContext(LayoutBox box)
    {
        var s = box.Style;
        var isRoot = box.Element is { Parent: null } && box.Pseudo == PseudoKind.None && !box.IsAnonymous;
        return isRoot || s.IsOutOfFlow || s.IsInlineLevel || box.IsScrollContainer || box.Type != BoxType.Block
            || s.Display == Display.WebkitBox
            || box.Parent is { Type: BoxType.Flex or BoxType.Grid }
            || box.Pseudo == PseudoKind.Marker
            || (box.Element?.Tag == "button" && box.Pseudo == PseudoKind.None);
    }

    // ---------- Block layout ----------

    void LayoutBlock(LayoutBox box, in Space space)
    {
        LayoutContainer(box, space, (b, sp, _) => LayoutBlockContents(b, sp), (b, contentHeight) =>
        {
            // A button centres its content in its height (Chromium lays buttons out as centring flex boxes).
            if (IsButton(b) && b.ContentHeight > contentHeight + 0.001f)
                ShiftContents(b, (b.ContentHeight - contentHeight) / 2);
        });
        PlaceMarker(box);
    }

    static bool IsButton(LayoutBox box) =>
        box.Pseudo == PseudoKind.None && !box.IsAnonymous && box.Element is { } e
        && (e.Tag == "button" || (e.Tag == "input" && (e.GetAttribute("type") ?? "").ToLowerInvariant() is "button" or "submit" or "reset"));

    /// <summary>Moves a block's in-flow content (children and lines) down by <paramref name="dy"/>.</summary>
    static void ShiftContents(LayoutBox box, float dy)
    {
        foreach (var child in box.Children)
            if (child.Container == box && !child.IsOutOfFlow && child.Type is not (BoxType.Text or BoxType.Inline or BoxType.LineBreak))
                child.Y += dy;
        if (box.Inline is { } inline)
        {
            foreach (var line in inline.Lines)
            {
                line.Top += dy;
                line.Baseline += dy;
                foreach (var item in line.Items)
                {
                    item.Baseline += dy;
                    if (item.Kind == LineItemKind.Atomic)
                        item.Box!.Y += dy;
                    if (item.Kind == LineItemKind.InlineBox)
                        item.Fragment = item.Fragment with { Rect = item.Fragment.Rect.Offset(0, dy) };
                }
            }
            foreach (var descendant in box.Descendants())
                if (descendant.InlineRoot == box && descendant.Fragments is { } fragments)
                    for (var i = 0; i < fragments.Count; i++)
                        fragments[i] = fragments[i] with { Rect = fragments[i].Rect.Offset(0, dy) };
        }
        if (box.FirstBaseline is { } first)
            box.FirstBaseline = first + dy;
        if (box.LastBaseline is { } last)
            box.LastBaseline = last + dy;
    }

    /// <summary>
    /// The frame every container shares: edges, width, the contents (laid out by
    /// <paramref name="contents"/>, which gets the definite content height or NaN and returns the
    /// content height), the height, the absolutely positioned boxes it contains, and scrollbars for
    /// overflowing scroll containers. <paramref name="sized"/> runs once the height is known (with the content height).
    /// </summary>
    internal void LayoutContainer(LayoutBox box, in Space space, Func<LayoutBox, Space, float, float> contents, Action<LayoutBox, float>? sized = null)
    {
        var s = box.Style;
        ResolveEdges(box, space.PercentWidth);
        var scrolls = box.IsScrollContainer;
        var scrollbars = scrolls
            ? new Scrollbars(box, s.OverflowX, s.OverflowY, Context.ScrollbarThickness(s), Context, s.ScrollbarGutterStable)
            : default;
        if (!scrolls)
            box.VerticalScrollbar = box.HorizontalScrollbar = 0;

        ComputeBlockWidth(box, space);
        for (var pass = 0; pass < 4; pass++)
        {
            var definite = DefiniteBlockHeight(box, space);
            var contentHeightBasis = float.IsNaN(definite) ? float.NaN : MathF.Max(0, definite - VerticalEdges(box) - box.HorizontalScrollbar);
            var contentHeight = contents(box, space, contentHeightBasis);
            FinishBlockHeight(box, space, contentHeight);
            sized?.Invoke(box, contentHeight);
            // Positioned boxes are laid out with their containing block (they count in its scrollable overflow).
            LayoutPositionedIn(box);
            if (!box.IsScrollContainer)
                break;
            // Overflowing content of an auto scroller gets scrollbars, which take room from the content.
            var overflow = ScrollableOverflow(box);
            box.ScrollWidth = overflow.X;
            box.ScrollHeight = overflow.Y;
            if (!scrollbars.Update(box, overflow))
                break;
            ComputeBlockWidth(box, space);
        }
    }

    /// <summary>The border-box width of a block (CSS 2 §10.3.3, §10.3.5 shrink-to-fit), with auto margins resolved.</summary>
    internal void ComputeBlockWidth(LayoutBox box, in Space space)
    {
        var s = box.Style;
        float width;
        var marginLeftAuto = s.MarginLeft.IsAuto;
        var marginRightAuto = s.MarginRight.IsAuto;
        if (!float.IsNaN(space.FixedWidth))
        {
            box.Width = space.FixedWidth;
            return;
        }
        var available = space.AvailableWidth - box.Margin.Left - box.Margin.Right;
        width = SpecifiedWidth(box, s.Width, space.PercentWidth, available);
        var widthAuto = float.IsNaN(width);
        if (widthAuto && s.AspectRatio > 0)
        {
            var height = SpecifiedHeight(box, s.Height, space.PercentHeight);
            if (!float.IsNaN(height))
            {
                width = RatioWidth(box, height);
                widthAuto = false;
            }
        }
        if (widthAuto)
        {
            width = space.ShrinkToFit
                ? MathF.Min(MathF.Max(IntrinsicWidth(box, true), available), IntrinsicWidth(box, false))
                : available;
        }
        width = ClampWidth(box, width, space.PercentWidth, available);
        box.Width = width;
        if (!space.ShrinkToFit && (marginLeftAuto || marginRightAuto) && !box.Style.IsInlineLevel)
        {
            var remaining = space.AvailableWidth - width - box.Margin.Left - box.Margin.Right;
            if (remaining < 0)
                remaining = 0;
            if (marginLeftAuto && marginRightAuto)
                box.Margin.Left = box.Margin.Right = remaining / 2;
            else if (marginLeftAuto)
                box.Margin.Left = remaining;
            else
                box.Margin.Right += remaining;
        }
    }

    internal static float RatioWidth(LayoutBox box, float borderBoxHeight)
    {
        var ratio = box.Style.AspectRatio;
        if (box.Style.BoxSizing == BoxSizing.BorderBox)
            return borderBoxHeight * ratio;
        var edgesV = box.Border.Vertical + box.Padding.Vertical;
        return (borderBoxHeight - edgesV) * ratio + box.Border.Horizontal + box.Padding.Horizontal;
    }

    internal static float RatioHeight(LayoutBox box, float borderBoxWidth)
    {
        var ratio = box.Style.AspectRatio;
        if (box.Style.BoxSizing == BoxSizing.BorderBox)
            return borderBoxWidth / ratio;
        var edgesH = box.Border.Horizontal + box.Padding.Horizontal;
        return (borderBoxWidth - edgesH) / ratio + box.Border.Vertical + box.Padding.Vertical;
    }

    /// <summary>The block's own definite height (from height, a fixed size or aspect-ratio), as a border-box height; NaN if auto.</summary>
    internal float DefiniteBlockHeight(LayoutBox box, in Space space)
    {
        if (!float.IsNaN(space.FixedHeight))
            return space.FixedHeight;
        if (space.ContentHeightOnly)
            return float.NaN;
        var height = SpecifiedHeight(box, box.Style.Height, space.PercentHeight);
        if (float.IsNaN(height) && box.Style.AspectRatio > 0 && box.Style.Height.IsAuto)
            return float.NaN; // from the ratio, but content can still make it taller (min-height: auto)
        return float.IsNaN(height) ? float.NaN : ClampHeight(box, height, space.PercentHeight);
    }

    void FinishBlockHeight(LayoutBox box, in Space space, float contentHeight)
    {
        var definite = DefiniteBlockHeight(box, space);
        var contentBased = contentHeight + VerticalEdges(box) + box.HorizontalScrollbar;
        if (space.ContentHeightOnly)
        {
            var max = box.Style.MaxHeight.IsNone ? float.NaN : SpecifiedHeight(box, box.Style.MaxHeight, space.PercentHeight);
            box.Height = float.IsNaN(max) ? contentBased : MathF.Min(contentBased, MathF.Max(max, VerticalEdges(box)));
            return;
        }
        if (!float.IsNaN(definite))
            box.Height = definite;
        else if (box.Style.AspectRatio > 0 && box.Style.Height.IsAuto)
        {
            // aspect-ratio with min-height: auto: the ratio's height, or the content's if that is taller
            // (unless the box scrolls its overflow).
            var ratio = RatioHeight(box, box.Width);
            box.Height = ClampHeight(box, box.Style.IsScrollContainer || !box.Style.MinHeight.IsAuto ? ratio : MathF.Max(ratio, contentBased), space.PercentHeight);
        }
        else
            box.Height = ClampHeight(box, contentBased, space.PercentHeight);
    }

    /// <summary>Lays out the block's children; returns the content height.</summary>
    float LayoutBlockContents(LayoutBox box, in Space space)
    {
        var contentWidth = box.ContentWidth;
        var definite = DefiniteBlockHeight(box, space);
        var percentHeight = float.IsNaN(definite) ? float.NaN : MathF.Max(0, definite - VerticalEdges(box) - box.HorizontalScrollbar);
        var formattingContext = EstablishesFormattingContext(box);
        box.TopStrut = default;
        box.TopStrut.Append(box.Margin.Top);
        box.BottomStrut = default;
        box.BottomStrut.Append(box.Margin.Bottom);
        box.CollapsesThrough = false;
        box.Inline = null;
        box.FirstBaseline = box.LastBaseline = null;

        if (box.Children.Any(c => c.IsInlineLevel))
        {
            var inline = _inline.Layout(box, contentWidth, percentHeight);
            box.Inline = inline;
            if (inline.Lines.Count > 0)
            {
                box.FirstBaseline = inline.Lines[0].Baseline;
                box.LastBaseline = inline.Lines[^1].Baseline;
            }
            if (inline.Height == 0 && inline.Lines.Count == 0)
                return EmptyBlock(box, formattingContext);
            return inline.Height;
        }

        var collapseTop = !formattingContext && box.Border.Top == 0 && box.Padding.Top == 0;
        var heightAuto = box.Style.Height.IsAuto || (box.Style.Height.HasPercent && float.IsNaN(space.PercentHeight));
        var collapseBottom = !formattingContext && box.Border.Bottom == 0 && box.Padding.Bottom == 0 && heightAuto && float.IsNaN(space.FixedHeight);
        var y = box.ContentTop;
        var strut = new MarginStrut();
        var atTop = true;
        var childSpace = Space.For(contentWidth, percentHeight);
        foreach (var child in box.Children)
        {
            if (child.IsOutOfFlow)
            {
                child.StaticContainer = box;
                child.StaticPosition = new Vector2(box.ContentLeft, atTop && collapseTop ? y : y + strut.Sum);
                continue;
            }
            child.Container = box;
            LayoutBlockLevel(child, childSpace);
            child.X = box.ContentLeft + child.Margin.Left;
            child.RelativeOffset = RelativeOffset(child.Style, contentWidth, percentHeight);
            if (child.CollapsesThrough)
            {
                strut.Append(child.TopStrut);
                child.Y = atTop && collapseTop ? y : y + strut.Sum;
                if (atTop && collapseTop)
                    box.TopStrut.Append(child.TopStrut);
                continue;
            }
            strut.Append(child.TopStrut);
            if (atTop && collapseTop)
            {
                box.TopStrut.Append(strut);
                child.Y = y;
            }
            else
                child.Y = y + strut.Sum;
            atTop = false;
            y = child.Y + child.Height;
            strut = child.BottomStrut;
            if (child.FirstBaseline is { } first && box.FirstBaseline is null)
                box.FirstBaseline = child.Y + first;
            if (child.LastBaseline is { } last)
                box.LastBaseline = child.Y + last;
        }
        if (atTop)
        {
            // Nothing in flow: the box may be empty with collapsing margins.
            var empty = EmptyBlock(box, formattingContext);
            if (box.CollapsesThrough)
                box.TopStrut.Append(strut);
            return empty;
        }
        if (collapseBottom)
            box.BottomStrut.Append(strut);
        else
            y += strut.Sum;
        return y - box.ContentTop;
    }

    /// <summary>A block without in-flow content: its margins collapse through it unless something separates them.</summary>
    float EmptyBlock(LayoutBox box, bool formattingContext)
    {
        var s = box.Style;
        var minHeight = s.MinHeight.IsAuto ? 0 : Resolve(s.MinHeight, 0);
        if (!formattingContext && VerticalEdges(box) == 0 && (s.Height.IsAuto || Resolve(s.Height, 0) == 0) && minHeight <= 0 && s.AspectRatio <= 0)
        {
            box.CollapsesThrough = true;
            box.TopStrut.Append(box.BottomStrut);
            box.BottomStrut = box.TopStrut;
        }
        return 0;
    }

    /// <summary>Puts an outside list marker before the item's first line.</summary>
    void PlaceMarker(LayoutBox box)
    {
        if (box.Marker is not { } marker)
            return;
        var markerLayout = _inline.LayoutMarker(marker);
        marker.Inline = markerLayout.Content;
        // The first line inside the item (in its own space).
        float baseline;
        if (FirstLine(box, Vector2.Zero) is { } found)
            baseline = found.Y;
        else
            baseline = box.ContentTop + markerLayout.Baseline;
        marker.Container = box;
        marker.X = box.ContentLeft - markerLayout.Width;
        marker.Y = baseline - markerLayout.Baseline;
        marker.Width = markerLayout.Width;
        marker.Height = markerLayout.Height;
    }

    /// <summary>The position of the first line's baseline below a box (in that box's space).</summary>
    static Vector2? FirstLine(LayoutBox box, Vector2 offset)
    {
        if (box.Inline is { Lines.Count: > 0 } inline)
            return offset + new Vector2(inline.Lines[0].Left, inline.Lines[0].Baseline);
        foreach (var child in box.Children)
        {
            if (child.IsOutOfFlow || child.Type is BoxType.Text or BoxType.Inline || child.Style.IsScrollContainer)
                continue;
            if (FirstLine(child, offset + new Vector2(child.X, child.Y)) is { } found)
                return found;
            if (child.Height > 0)
                return null;
        }
        return null;
    }

    // ---------- Replaced elements ----------

    void LayoutReplaced(LayoutBox box, in Space space)
    {
        ResolveEdges(box, space.PercentWidth);
        var natural = ReplacedContent.NaturalSize(box, Context);
        box.NaturalSize = natural;
        var s = box.Style;
        var edgesH = HorizontalEdges(box);
        var edgesV = VerticalEdges(box);
        var ratio = s.AspectRatio > 0 ? s.AspectRatio : ReplacedContent.HasNaturalRatio(box) ? ReplacedContent.NaturalRatio(box, natural) : 0;
        var width = !float.IsNaN(space.FixedWidth) ? space.FixedWidth : SpecifiedWidth(box, s.Width, space.PercentWidth, space.AvailableWidth);
        var height = !float.IsNaN(space.FixedHeight) ? space.FixedHeight : SpecifiedHeight(box, s.Height, space.PercentHeight);
        if (float.IsNaN(width) && float.IsNaN(height))
        {
            width = (natural?.X ?? 0) + edgesH;
            height = (natural?.Y ?? 0) + edgesV;
            // A ratio without a natural size: fill the containing block's width (CSS 2 §10.3.2), height from the ratio.
            if (ReplacedContent.FillsWidth(box))
            {
                width = MathF.Max(edgesH, space.AvailableWidth - box.Margin.Horizontal);
                height = ratio > 0 ? (width - edgesH) / ratio + edgesV : 150 + edgesV;
            }
        }
        else if (float.IsNaN(width))
            width = ratio > 0 ? (height - edgesV) * ratio + edgesH : (natural?.X ?? 0) + edgesH;
        else if (float.IsNaN(height))
            height = ratio > 0 ? (width - edgesH) / ratio + edgesV : (natural?.Y ?? 0) + edgesV;
        box.Width = float.IsNaN(space.FixedWidth) ? ClampWidth(box, width, space.PercentWidth, space.AvailableWidth) : width;
        box.Height = float.IsNaN(space.FixedHeight) ? ClampHeight(box, height, space.PercentHeight) : height;
        box.FirstBaseline = box.LastBaseline = ReplacedContent.Baseline(box, Context);
        box.Inline = null;
        if (box.IsScrollContainer && box.Element?.Tag == "textarea")
            ScrollTextArea(box);
        else
            box.VerticalScrollbar = box.HorizontalScrollbar = 0;
        if (s.Display is not (Display.Inline or Display.InlineBlock) && (s.MarginLeft.IsAuto || s.MarginRight.IsAuto) && !space.ShrinkToFit)
        {
            var remaining = MathF.Max(0, space.AvailableWidth - box.Width - box.Margin.Horizontal);
            if (s.MarginLeft.IsAuto && s.MarginRight.IsAuto)
                box.Margin.Left = box.Margin.Right = remaining / 2;
            else if (s.MarginLeft.IsAuto)
                box.Margin.Left = remaining;
        }
    }

    /// <summary>
    /// A textarea scrolls its text (Blink's LayoutTextControlMultiLine is a block with overflow:
    /// auto around the inner editor): its scrollable overflow is the wrapped text with the padding
    /// around it, and auto scrollbars come and go as a block's do (the text rewraps beside one).
    /// </summary>
    void ScrollTextArea(LayoutBox box)
    {
        var s = box.Style;
        var scrollbars = new Scrollbars(box, s.OverflowX, s.OverflowY, Context.ScrollbarThickness(s), Context, s.ScrollbarGutterStable);
        for (var pass = 0; pass < 4; pass++)
        {
            var text = ReplacedContent.TextAreaTextSize(box, Context, box.ContentWidth);
            var overflow = new Vector2(MathF.Max(box.PaddingBoxWidth, box.Padding.Left + text.X + box.Padding.Right),
                MathF.Max(box.PaddingBoxHeight, box.Padding.Top + text.Y + box.Padding.Bottom));
            box.ScrollWidth = overflow.X;
            box.ScrollHeight = overflow.Y;
            if (!scrollbars.Update(box, overflow))
                break;
        }
    }

    // ---------- Positioned boxes ----------

    /// <summary>The offset of <paramref name="box"/>'s border box from <paramref name="ancestor"/>'s, following containers.</summary>
    static Vector2 OffsetIn(LayoutBox box, LayoutBox ancestor)
    {
        var offset = Vector2.Zero;
        for (var b = box; b is not null && b != ancestor; b = b.Container)
            offset += new Vector2(b.X, b.Y) + b.RelativeOffset;
        return offset;
    }

    /// <summary>
    /// Lays out the absolutely positioned boxes whose containing block is <paramref name="box"/>, or
    /// an inline box on its lines (once the lines exist).
    /// </summary>
    void LayoutPositionedIn(LayoutBox box)
    {
        if (box.PositionedDescendants is not null)
            LayoutPositioned(box);
        if (box.Inline is null)
            return;
        void Inlines(LayoutBox parent)
        {
            foreach (var child in parent.Children)
                if (child.Type == BoxType.Inline)
                {
                    if (child.PositionedDescendants is not null)
                        LayoutPositioned(child);
                    Inlines(child);
                }
        }
        Inlines(box);
    }

    /// <summary>Lays out the absolutely positioned boxes whose containing block is <paramref name="container"/> (CSS 2 §10.3.7, §10.6.4).</summary>
    internal void LayoutPositioned(LayoutBox container)
    {
        if (container.PositionedDescendants is not { } list)
            return;
        var isViewport = container == Viewport;
        foreach (var box in list)
        {
            var fixedToViewport = box.Container is null;
            float cbLeft, cbTop, cbWidth, cbHeight;
            if (fixedToViewport || isViewport)
            {
                cbLeft = cbTop = 0;
                cbWidth = Viewport.PaddingBoxWidth;
                cbHeight = Viewport.PaddingBoxHeight;
            }
            else if (container.Type == BoxType.Inline && container.Fragments is { Count: > 0 } fragments)
            {
                // An inline containing block: from the start of its first fragment to the end of its last.
                var first = fragments[0].Rect;
                var last = fragments[^1].Rect;
                cbLeft = first.X + container.Border.Left;
                cbTop = first.Y + container.Border.Top;
                cbWidth = MathF.Max(0, last.Right - container.Border.Right - cbLeft);
                cbHeight = MathF.Max(0, last.Bottom - container.Border.Bottom - cbTop);
            }
            else
            {
                cbLeft = container.Border.Left;
                cbTop = container.Border.Top;
                cbWidth = container.PaddingBoxWidth;
                cbHeight = container.PaddingBoxHeight;
            }

            // The static position, in the containing block's space.
            var staticPosition = box.StaticPosition;
            if (box.StaticContainer is { } staticContainer)
            {
                staticPosition += fixedToViewport ? staticContainer.ViewportPosition : OffsetIn(staticContainer, container);
            }
            staticPosition -= new Vector2(cbLeft, cbTop);

            var s = box.Style;
            ResolveEdges(box, cbWidth);
            var left = s.Left.IsAuto ? float.NaN : Resolve(s.Left, cbWidth);
            var right = s.Right.IsAuto ? float.NaN : Resolve(s.Right, cbWidth);
            var top = s.Top.IsAuto ? float.NaN : LayoutUnit.Truncate(s.Top.ResolveOr(cbHeight, float.NaN));
            var bottom = s.Bottom.IsAuto ? float.NaN : LayoutUnit.Truncate(s.Bottom.ResolveOr(cbHeight, float.NaN));

            // Horizontal.
            var space = Space.For(cbWidth, cbHeight);
            space.ShrinkToFit = true;
            var marginH = box.Margin.Horizontal;
            // The size comes from the insets and the content, as if the box had no scrollbars (its
            // layout keeps or adds them; a kept layout keeps its own).
            var (keptVertical, keptHorizontal) = (box.VerticalScrollbar, box.HorizontalScrollbar);
            box.VerticalScrollbar = 0;
            box.HorizontalScrollbar = 0;
            var width = box.Type == BoxType.Replaced ? float.NaN : SpecifiedWidth(box, s.Width, cbWidth, cbWidth);
            if (float.IsNaN(width) && s.AspectRatio > 0)
            {
                var h = SpecifiedHeight(box, s.Height, cbHeight);
                if (float.IsNaN(h) && !float.IsNaN(top) && !float.IsNaN(bottom) && box.Type != BoxType.Replaced)
                    h = cbHeight - top - bottom - box.Margin.Vertical;
                if (!float.IsNaN(h))
                    width = RatioWidth(box, h);
            }
            if (box.Type != BoxType.Replaced)
            {
                if (float.IsNaN(width))
                {
                    if (!float.IsNaN(left) && !float.IsNaN(right))
                        width = MathF.Max(0, cbWidth - left - right - marginH);
                    else
                    {
                        var available = cbWidth - (float.IsNaN(left) ? staticPosition.X : left) - (float.IsNaN(right) ? 0 : right) - marginH;
                        if (float.IsNaN(left) && float.IsNaN(right))
                            available = cbWidth - staticPosition.X - marginH;
                        width = MathF.Min(MathF.Max(IntrinsicWidth(box, true), available), IntrinsicWidth(box, false));
                    }
                }
                width = ClampWidth(box, width, cbWidth, cbWidth);
                space.FixedWidth = width;
            }

            // Lay out with the width fixed; the height is fixed when both insets (or height) determine it.
            var height = SpecifiedHeight(box, s.Height, cbHeight);
            if (float.IsNaN(height) && s.AspectRatio > 0 && !float.IsNaN(width) && box.Type != BoxType.Replaced)
                height = RatioHeight(box, width);
            if (float.IsNaN(height) && !float.IsNaN(top) && !float.IsNaN(bottom) && box.Type != BoxType.Replaced && s.AspectRatio <= 0)
                height = MathF.Max(0, cbHeight - top - bottom - box.Margin.Vertical);
            if (!float.IsNaN(height) && box.Type != BoxType.Replaced)
                space.FixedHeight = ClampHeight(box, height, cbHeight);
            space.PercentHeight = cbHeight;
            (box.VerticalScrollbar, box.HorizontalScrollbar) = (keptVertical, keptHorizontal);
            LayoutBlockLevel(box, space);
            // Layout resolved the edges again; auto margins are zero there.
            ResolveEdges(box, cbWidth);
            width = box.Width;
            height = box.Height;

            // The static position is where the margin box would be (aligned in a flex or grid container's content box).
            if (box.StaticArea is { } area && box.StaticContainer is { } areaContainer)
            {
                var areaOrigin = (fixedToViewport ? areaContainer.ViewportPosition : OffsetIn(areaContainer, container)) - new Vector2(cbLeft, cbTop);
                staticPosition = areaOrigin + new Vector2(
                    area.X + (area.Width - box.MarginBoxWidth) * box.StaticAlign.X,
                    area.Y + (area.Height - box.MarginBoxHeight) * box.StaticAlign.Y);
            }

            // Horizontal position and auto margins.
            if (float.IsNaN(left) && float.IsNaN(right))
                left = staticPosition.X;
            if (!float.IsNaN(left) && !float.IsNaN(right) && (s.MarginLeft.IsAuto || s.MarginRight.IsAuto))
            {
                var free = cbWidth - left - right - width - box.Margin.Horizontal;
                if (s.MarginLeft.IsAuto && s.MarginRight.IsAuto)
                {
                    if (free < 0)
                        box.Margin.Right += free;
                    else
                        box.Margin.Left = box.Margin.Right = free / 2;
                }
                else if (s.MarginLeft.IsAuto)
                    box.Margin.Left = free;
                else
                    box.Margin.Right = free;
            }
            if (float.IsNaN(left))
                left = cbWidth - right - width - box.Margin.Horizontal;

            // Vertical.
            if (float.IsNaN(top) && float.IsNaN(bottom))
                top = staticPosition.Y;
            if (!float.IsNaN(top) && !float.IsNaN(bottom) && (s.MarginTop.IsAuto || s.MarginBottom.IsAuto))
            {
                var free = cbHeight - top - bottom - height - box.Margin.Vertical;
                if (s.MarginTop.IsAuto && s.MarginBottom.IsAuto)
                {
                    if (free < 0)
                        box.Margin.Bottom += free;
                    else
                        box.Margin.Top = box.Margin.Bottom = free / 2;
                }
                else if (s.MarginTop.IsAuto)
                    box.Margin.Top = free;
                else
                    box.Margin.Bottom = free;
            }
            if (float.IsNaN(top))
                top = cbHeight - bottom - height - box.Margin.Vertical;

            box.X = cbLeft + left + box.Margin.Left;
            box.Y = cbTop + top + box.Margin.Top;
            box.RelativeOffset = Vector2.Zero;
        }
    }

    // ---------- Overflow ----------

    /// <summary>
    /// The scrollable overflow of a box, from its padding box's top left (CSS Overflow 3 §2.2), as
    /// Blink's ScrollableOverflowCalculator finds it: the padding box, the border boxes of its
    /// children and of their unclipped descendants, and the in-flow children's margin boxes (lines
    /// included) grown by the end padding. In those margin boxes start margins count from zero
    /// and end margins may be negative (down to the box's size).
    /// </summary>
    internal Vector2 ScrollableOverflow(LayoutBox box)
    {
        var origin = new Vector2(box.Border.Left, box.Border.Top);
        float right = box.PaddingBoxWidth, bottom = box.PaddingBoxHeight;
        float? inflowRight = null, inflowBottom = null;
        void Overflow(LayoutBox child, Vector2 offset)
        {
            var position = offset + new Vector2(child.X, child.Y) + child.RelativeOffset - origin;
            right = MathF.Max(right, position.X + child.Width);
            bottom = MathF.Max(bottom, position.Y + child.Height);
            // Descendants that overflow a box without clipping overflow it further.
            if (!child.Style.IsScrollContainer && child.Type != BoxType.Text)
                foreach (var grandchild in child.Children)
                    if (grandchild.Container == child && grandchild.Type is not (BoxType.Text or BoxType.Inline or BoxType.LineBreak))
                        Overflow(grandchild, position + origin);
        }
        foreach (var child in box.Children)
        {
            if (child.Type is BoxType.Text or BoxType.Inline or BoxType.LineBreak || child.Container != box)
                continue;
            Overflow(child, Vector2.Zero);
            if (child.IsOutOfFlow)
                continue;
            // The in-flow bounds use the position before relative offsets.
            var position = new Vector2(child.X, child.Y) - origin;
            var endRight = position.X + child.Width + MathF.Max(child.Margin.Right, -child.Width);
            var endBottom = position.Y + child.Height + MathF.Max(child.Margin.Bottom, -child.Height);
            inflowRight = MathF.Max(inflowRight ?? float.MinValue, endRight);
            inflowBottom = MathF.Max(inflowBottom ?? float.MinValue, endBottom);
        }
        if (box.PositionedDescendants is not null)
            foreach (var positioned in box.PositionedDescendants)
                // (Its own positioned children were counted with its children.)
                if (positioned.Container == box && positioned.Parent != box)
                    Overflow(positioned, Vector2.Zero);
        if (box.Inline is { } inline)
        {
            // Text overflows as far as it reaches; the line boxes (as wide as the content box) are in
            // flow, lines a line clamp hides included (as in Chromium).
            right = MathF.Max(right, inline.Right - origin.X);
            var linesBottom = box.ContentTop + inline.OverflowHeight - origin.Y;
            bottom = MathF.Max(bottom, linesBottom);
            inflowBottom = MathF.Max(inflowBottom ?? float.MinValue, linesBottom);
            inflowRight = MathF.Max(inflowRight ?? float.MinValue, box.ContentLeft + box.ContentWidth - origin.X);
        }
        if (inflowRight is { } r)
            right = MathF.Max(right, r + box.Padding.Right);
        if (inflowBottom is { } b)
            bottom = MathF.Max(bottom, b + box.Padding.Bottom);
        return new Vector2(right, bottom);
    }
}

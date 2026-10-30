using System.Numerics;
using Statventure.Core;
using Statventure.Text;
using Statventure.UI.Dom;
using Statventure.UI.Style;

namespace Statventure.UI.Layout;

public enum BoxType
{
    /// <summary>A block container (block, inline-block, list-item, flow-root, -webkit-box).</summary>
    Block,
    /// <summary>A non-atomic inline box (display: inline).</summary>
    Inline,
    Flex,
    Grid,
    /// <summary>A run of text (a text node, or generated content).</summary>
    Text,
    /// <summary>A forced line break (&lt;br&gt;).</summary>
    LineBreak,
    /// <summary>A replaced element: its content is an image, a canvas or a form control.</summary>
    Replaced,
}

public enum PseudoKind
{
    None,
    Before,
    After,
    Marker,
}

/// <summary>Widths of a box's four sides.</summary>
public struct Edges
{
    public float Top, Right, Bottom, Left;

    public Edges(float top, float right, float bottom, float left)
    {
        Top = top;
        Right = right;
        Bottom = bottom;
        Left = left;
    }

    public readonly float Horizontal => Left + Right;
    public readonly float Vertical => Top + Bottom;

    public static Edges operator +(Edges a, Edges b) => new(a.Top + b.Top, a.Right + b.Right, a.Bottom + b.Bottom, a.Left + b.Left);

    public override readonly string ToString() => $"{Top} {Right} {Bottom} {Left}";
}

/// <summary>Collapsing margins (CSS 2 §8.3.1): the largest positive and the most negative margin.</summary>
public struct MarginStrut
{
    public float Positive, Negative;

    public void Append(float margin)
    {
        if (margin > Positive)
            Positive = margin;
        else if (margin < Negative)
            Negative = margin;
    }

    public void Append(MarginStrut other)
    {
        Positive = MathF.Max(Positive, other.Positive);
        Negative = MathF.Min(Negative, other.Negative);
    }

    public readonly float Sum => Positive + Negative;
}

/// <summary>Where a box's style comes from: boxes read their element's current style, so style changes need no box work.</summary>
internal enum StyleSource : byte
{
    /// <summary>A style of the box's own (the viewport, probes).</summary>
    Own,
    /// <summary>The element's style.</summary>
    Element,
    /// <summary>The element's ::before or ::after style.</summary>
    Before,
    After,
    /// <summary>A text box: the style of the text node's parent element.</summary>
    TextNode,
    /// <summary>The parent box's style (generated text inside pseudo-elements and markers).</summary>
    Parent,
    /// <summary>Derived from another box's style (anonymous boxes, markers), again whenever that style changes.</summary>
    Derived,
}

/// <summary>How a derived style is made from its source.</summary>
internal enum DerivedStyle : byte { AnonymousBlock, Marker, DisclosureMarker }

/// <summary>
/// A box of the layout tree. Geometry is the border box, with <see cref="X"/>/<see cref="Y"/>
/// relative to the border box of <see cref="Container"/> (the box that laid it out: its block,
/// flex or grid container, or for absolutely positioned boxes their containing block; null for the
/// root and fixed boxes, which are relative to the viewport).
/// </summary>
public sealed class LayoutBox
{
    public BoxType Type;
    /// <summary>The element the box belongs to (for pseudo-element boxes, their element); null for anonymous and text boxes.</summary>
    public UiElement? Element;
    public PseudoKind Pseudo;
    public bool IsAnonymous;
    public LayoutBox? Parent;
    /// <summary>The viewport (the initial containing block): it scrolls the document by <see cref="DocumentScroll"/>.</summary>
    public bool IsViewport { get; internal init; }
    internal Vector2 DocumentScroll;
    /// <summary>The viewport: its sticky positioned boxes, in tree order.</summary>
    internal List<LayoutBox>? StickyBoxes;

    ComputedStyle _style;
    StyleSource _source;
    DerivedStyle _derived;
    LayoutBox? _deriveFrom;
    ComputedStyle? _derivedOf;

    /// <summary>The box's computed style (for element boxes, the element's current one).</summary>
    public ComputedStyle Style
    {
        get => _source switch
        {
            StyleSource.Own => _style,
            StyleSource.Element => Element!.Style ?? _style,
            StyleSource.Before => Element!.BeforeStyle ?? _style,
            StyleSource.After => Element!.AfterStyle ?? _style,
            StyleSource.TextNode => TextNode!.Parent?.Style ?? _style,
            StyleSource.Parent => Parent?.Style ?? _style,
            _ => DerivedNow(),
        };
        set
        {
            _style = value;
            _source = StyleSource.Own;
        }
    }

    internal void ReadStyleFrom(StyleSource source) => _source = source;

    internal void DeriveStyle(DerivedStyle kind, LayoutBox from)
    {
        _source = StyleSource.Derived;
        _derived = kind;
        _deriveFrom = from;
        _derivedOf = null;
    }

    ComputedStyle DerivedNow()
    {
        var source = _deriveFrom!.Style;
        if (!ReferenceEquals(source, _derivedOf))
        {
            _style = Derive(_derived, source);
            _derivedOf = source;
        }
        return _style;
    }

    internal static ComputedStyle Derive(DerivedStyle kind, ComputedStyle source)
    {
        switch (kind)
        {
            case DerivedStyle.Marker:
            {
                // The marker inherits from the list item; markers use tabular numbers and keep their spaces.
                var marker = StyleEngine.Anonymous(source, Display.Inline);
                // Outside markers are inline-blocks in Blink (AdjustStyleForMarker), so decorations don't reach them.
                if (!source.ListStyleInside)
                    marker.BaseDecorations = [];
                marker.WhiteSpace = Statventure.Text.WhiteSpace.Pre;
                marker.TabularNumbers = true;
                marker.TextTransform = Statventure.Text.TextTransform.None;
                marker.LetterSpacing = 0;
                marker.Finish();
                return marker;
            }
            case DerivedStyle.DisclosureMarker:
            {
                // Chromium draws disclosure triangles as shapes (the painter draws it). Inside, the
                // marker is the symbol, 0.66 em zoomed (DisclosureSymbolSize), and a 0.4 em margin
                // after it that Blink takes from the specified, unzoomed size (InlineMarginsForInside).
                var triangle = StyleEngine.Anonymous(source, Display.InlineBlock);
                var specified = source.FontSize / source.Zoom;
                triangle.Width = Length.Px(LayoutUnit.Truncate(specified * source.Zoom * 0.66f) + LayoutUnit.Truncate(0.4f * specified));
                return triangle;
            }
            default:
                return StyleEngine.Anonymous(source, Display.Block);
        }
    }
    public readonly List<LayoutBox> Children = [];

    /// <summary>Text boxes: the text, and the text node it comes from (null for generated content).</summary>
    public string Text = "";
    public UiText? TextNode;

    public LayoutBox? Container;
    public float X, Y, Width, Height;
    public Edges Margin, Border, Padding;

    /// <summary>The block's lines, when its children are inline-level.</summary>
    public InlineContent? Inline;
    /// <summary>Non-atomic inline boxes: the rectangles they cover on each line (in their IFC root's space).</summary>
    public List<InlineFragment>? Fragments;
    /// <summary>The IFC root for boxes laid out in lines (inline boxes and atomic inlines).</summary>
    public LayoutBox? InlineRoot;

    /// <summary>A list item's outside marker (not one of its children: it hangs off the first line).</summary>
    public LayoutBox? Marker;

    /// <summary>Baselines from the border box's top; null if the box has none.</summary>
    public float? FirstBaseline, LastBaseline;

    /// <summary>Absolutely positioned boxes whose containing block is this box.</summary>
    public List<LayoutBox>? PositionedDescendants;
    /// <summary>Where an absolutely positioned box would be if it were static, in its parent's content space.</summary>
    public Vector2 StaticPosition;
    /// <summary>The box that the static position is relative to (the parent that laid out the placeholder).</summary>
    public LayoutBox? StaticContainer;
    /// <summary>
    /// Children of flex and grid containers: the static position aligns the box in this area (the
    /// container's content box, in the static container's space) by these factors (0 start, 0.5 centre, 1 end).
    /// </summary>
    public RectF? StaticArea;
    public Vector2 StaticAlign;
    /// <summary>position: relative offsets (and sticky ones, which depend on scrolling).</summary>
    public Vector2 RelativeOffset;
    public Vector2 StickyOffset;

    /// <summary>Scroll containers: the size of the scrollable overflow area (from the padding box's top left).</summary>
    public float ScrollWidth, ScrollHeight;
    public float VerticalScrollbar, HorizontalScrollbar;

    /// <summary>The ink overflow of the box and its descendants, relative to its border box (for culling).</summary>
    public RectF InkOverflow;
    /// <summary>An inline box that has had fragments of its own keeps them (Blink's latched ShouldCreateBoxFragment).</summary>
    internal bool KeepsBoxFragments;

    // Incremental layout: a box laid out again in the space of its last layout, with nothing
    // changed in it, keeps that layout. Changes mark the box and its ancestors.
    /// <summary>Something about this box itself changed (style, children, text).</summary>
    internal bool NeedsLayout = true;
    /// <summary>Something below this box changed.</summary>
    internal bool ChildNeedsLayout;
    /// <summary>The geometry is that of a layout in <see cref="LastSpace"/>.</summary>
    internal bool HasLayout;
    internal Space LastSpace;
    /// <summary>Results of measuring layouts in other spaces (flex and grid measure items before placing them).</summary>
    internal List<(Space Space, LayoutEngine.Measured Result)>? Measures;
    // Intrinsic sizes and the collected inline content (kept until something in the box changes).
    internal float? CachedMinContent, CachedMaxContent, CachedContentMin;
    internal object? Paragraph;

    internal bool IsClean => !NeedsLayout && !ChildNeedsLayout;

    /// <summary>The box needs a new layout, and so do its ancestors (their sizes may depend on it).</summary>
    internal void MarkNeedsLayout()
    {
        NeedsLayout = true;
        ForgetSizes();
        for (var ancestor = Parent; ancestor is not null; ancestor = ancestor.Parent)
        {
            ancestor.ChildNeedsLayout = true;
            ancestor.ForgetSizes();
        }
    }

    /// <summary>The box needs a new layout (its ancestors are marked by whoever changed them).</summary>
    internal void Invalidate()
    {
        NeedsLayout = true;
        ForgetSizes();
    }

    void ForgetSizes()
    {
        CachedMinContent = CachedMaxContent = CachedContentMin = null;
        Measures?.Clear();
        Paragraph = null;
    }
    /// <summary>For an empty block whose margins collapse through it (CSS 2 §8.3.1).</summary>
    internal bool CollapsesThrough;
    internal MarginStrut TopStrut, BottomStrut;
    /// <summary>A replaced box's natural size and ratio.</summary>
    public Vector2? NaturalSize;

    public LayoutBox(BoxType type, ComputedStyle style)
    {
        Type = type;
        _style = style;
    }

    public bool IsInlineLevel => Type is BoxType.Inline or BoxType.Text or BoxType.LineBreak || (Style.IsInlineLevel && !IsOutOfFlow);
    public bool IsAtomicInline => Style.IsInlineLevel && Type is BoxType.Block or BoxType.Flex or BoxType.Grid or BoxType.Replaced && !IsOutOfFlow;
    public bool IsOutOfFlow => Style.IsOutOfFlow && Type is not (BoxType.Text or BoxType.LineBreak);
    public bool IsBlockContainer => Type == BoxType.Block;
    /// <summary>
    /// Clips (and maybe scrolls) its overflow. The root and a propagating body give theirs to the
    /// viewport instead. An inline svg only clips (Blink's replaced elements never scroll); its clip
    /// is the svg root's own (see PropertyTreeBuilder).
    /// </summary>
    public bool IsScrollContainer => Style.IsScrollContainer && Type is BoxType.Block or BoxType.Flex or BoxType.Grid or BoxType.Replaced
        && !(Type == BoxType.Replaced && Element?.Tag == "svg") && !LayoutEngine.PropagatesOverflowToViewport(this);
    public bool ChildrenInline => Inline is not null;

    public float ContentLeft => Border.Left + Padding.Left;
    public float ContentTop => Border.Top + Padding.Top;
    public float ContentWidth => MathF.Max(0, Width - Border.Horizontal - Padding.Horizontal - VerticalScrollbar);
    public float ContentHeight => MathF.Max(0, Height - Border.Vertical - Padding.Vertical - HorizontalScrollbar);
    public float PaddingBoxWidth => MathF.Max(0, Width - Border.Horizontal - VerticalScrollbar);
    public float PaddingBoxHeight => MathF.Max(0, Height - Border.Vertical - HorizontalScrollbar);
    public float MarginBoxWidth => Width + Margin.Horizontal;
    public float MarginBoxHeight => Height + Margin.Vertical;

    public RectF BorderRect => new(0, 0, Width, Height);
    public RectF PaddingRect => new(Border.Left, Border.Top, PaddingBoxWidth, PaddingBoxHeight);
    public RectF ContentRect => new(ContentLeft, ContentTop, ContentWidth, ContentHeight);

    /// <summary>The border box's top-left in viewport space, before transforms (scroll offsets applied).</summary>
    public Vector2 ViewportPosition
    {
        get
        {
            var position = new Vector2(X, Y) + RelativeOffset + StickyOffset;
            for (var container = Container; container is not null; container = container.Container)
            {
                position += new Vector2(container.X, container.Y) + container.RelativeOffset + container.StickyOffset;
                if (container.IsScrollContainer && container.Element is { } scroller)
                    position -= scroller.LayoutScroll;
                else if (container.IsViewport)
                    position -= container.DocumentScroll;
            }
            return position;
        }
    }

    public RectF ViewportRect
    {
        get
        {
            var p = ViewportPosition;
            return new RectF(p.X, p.Y, Width, Height);
        }
    }

    public IEnumerable<LayoutBox> Descendants()
    {
        foreach (var child in Children)
        {
            yield return child;
            foreach (var descendant in child.Descendants())
                yield return descendant;
        }
    }

    public override string ToString()
    {
        var name = Element?.ToString() ?? (Type == BoxType.Text ? $"\"{(Text.Length > 20 ? Text[..20] + "…" : Text)}\"" : "anonymous");
        if (Pseudo != PseudoKind.None)
            name += "::" + Pseudo.ToString().ToLowerInvariant();
        return $"{Type} {name} ({X}, {Y}, {Width}×{Height})";
    }
}

/// <summary>Where a non-atomic inline box lies on one line (its border box), in the IFC root's space.</summary>
public readonly record struct InlineFragment(RectF Rect, bool IsFirst, bool IsLast, int Line);

/// <summary>The line boxes of an inline formatting context.</summary>
public sealed class InlineContent
{
    public readonly List<LineBox> Lines = [];
    /// <summary>The height of the visible lines (the block's content height).</summary>
    public float Height;
    /// <summary>The height of all lines, including those a line clamp hides.</summary>
    public float OverflowHeight;
    /// <summary>The widest line's right edge (scrollable overflow).</summary>
    public float Right;
}

/// <summary>One line: its position (in the IFC root's border-box space) and what's on it, in paint order.</summary>
public sealed class LineBox
{
    public float Top, Height, Baseline;
    public float Left, Width;
    /// <summary>Hidden by -webkit-line-clamp (laid out, not painted).</summary>
    public bool Hidden;
    public readonly List<LineItem> Items = [];
}

public enum LineItemKind
{
    /// <summary>Glyphs of a text box.</summary>
    Text,
    /// <summary>The start of an inline box's fragment on this line (its background and border paint here).</summary>
    InlineBox,
    /// <summary>An atomic inline (inline-block, replaced element): the box itself is positioned.</summary>
    Atomic,
    /// <summary>A list marker or ellipsis glyphs that belong to no text box.</summary>
    Generated,
}

public sealed class LineItem
{
    public LineItemKind Kind;
    public LayoutBox? Box;
    /// <summary>Generated items: the box whose style they use (the IFC root).</summary>
    public LayoutBox? Owner;
    public GlyphRun? Run;
    public ComputedStyle Style => (Box ?? Owner)!.Style;
    /// <summary>Text items: the glyph origin (x of the run start, the baseline).</summary>
    public float X, Baseline;
    public float Width;
    /// <summary>Text items: the range of the box's text they show.</summary>
    public int TextStart, TextEnd;
    /// <summary>Text items: the font's ascent and descent at this size (for decorations, selection, hit testing).</summary>
    public float Ascent, Descent;
    /// <summary>InlineBox items: the fragment.</summary>
    public InlineFragment Fragment;
    /// <summary>Text items: preserved spaces hanging at the end of the line.</summary>
    public bool Hanging;
    /// <summary>Text items: the shaped clusters on the line (processed text offsets, x in the line's space, width), for carets and selections.</summary>
    public List<ItemCluster>? Clusters;
}

/// <summary>A shaped cluster of a text item: its range of the box's processed text, where it is on the line, and its glyphs in the item's run.</summary>
public readonly record struct ItemCluster(int Start, int End, float X, float Width, int GlyphStart, int GlyphCount);

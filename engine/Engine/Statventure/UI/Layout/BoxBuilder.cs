using Statventure.UI.Dom;
using Statventure.UI.Style;

namespace Statventure.UI.Layout;

/// <summary>
/// Builds the box tree from the element tree and computed styles (CSS Display 3): display: none
/// and contents, pseudo-element and list-marker boxes, anonymous block boxes around inline runs
/// that sit next to blocks, anonymous flex and grid items around loose text, and the containing
/// blocks of absolutely positioned boxes. Updates are incremental: elements keep their boxes
/// unless marked (<see cref="UiElement.BoxNeedsRebuild"/>), and only elements whose children
/// changed collect their child boxes again.
/// </summary>
public sealed class BoxBuilder
{
    /// <summary>Elements whose content is not laid out as children (images, canvases, form fields).</summary>
    static readonly HashSet<string> ReplacedTags = ["img", "canvas", "video", "iframe", "select", "textarea", "progress", "meter", "surface", "svg"];

    readonly LayoutBox _viewport;
    readonly List<string> _diagnostics = [];

    /// <summary>Box-tree constructs the engine handles only approximately (should stay empty for the game).</summary>
    public IReadOnlyList<string> Diagnostics => _diagnostics;

    public BoxBuilder(LayoutBox viewport)
    {
        _viewport = viewport;
    }

    public static bool IsReplaced(UiElement element)
    {
        if (ReplacedTags.Contains(element.Tag))
            return true;
        // Inputs show their value or label themselves (Blink's shadow content); image inputs aren't drawn by the engine.
        return element.Tag == "input" && element.InputType is not ("hidden" or "image");
    }

    /// <summary>Brings the boxes up to date with the element tree; returns the root element's box (null if it has display: none).</summary>
    public LayoutBox? Build(UiElement root)
    {
        var current = _viewport.Children.FirstOrDefault();
        if (root.BoxNeedsRebuild || current is null || current.Element != root)
        {
            _diagnostics.Clear();
            _viewport.Children.Clear();
            var boxes = new List<LayoutBox>();
            BuildElement(root, boxes);
            if (boxes.FirstOrDefault() is { } box)
                Adopt(_viewport, box);
        }
        else if (root.ChildBoxesNeedRebuild || root.DescendantBoxesNeedRebuild)
            Revisit(root);
        AssignContainingBlocks();
        _viewport.MarkNeedsLayout();
        return _viewport.Children.FirstOrDefault();
    }

    static void ClearFlags(UiElement element) => element.BoxNeedsRebuild = element.ChildBoxesNeedRebuild = element.DescendantBoxesNeedRebuild = false;

    void BuildElement(UiElement element, List<LayoutBox> output)
    {
        var style = element.Style;
        if (style is null || style.Display == Display.None || (element.Tag == "input" && string.Equals(element.GetAttribute("type"), "hidden", StringComparison.OrdinalIgnoreCase)))
        {
            ClearBoxes(element);
            return;
        }
        if (style.Display == Display.Contents && !IsReplaced(element))
        {
            element.Box = null;
            ClearFlags(element);
            BuildChildren(element, style, output);
            return;
        }
        if (!element.BoxNeedsRebuild && element.Box is { } kept)
        {
            // Unchanged: the box stays, with whatever changed inside it rebuilt.
            Revisit(element);
            output.Add(kept);
            return;
        }
        ClearFlags(element);

        var type = TypeOf(element, style);
        var box = new LayoutBox(type, style) { Element = element };
        box.ReadStyleFrom(StyleSource.Element);
        element.Box = box;
        if (type is not (BoxType.Replaced or BoxType.LineBreak))
            Attach(box, CollectChildren(element, box, style));
        output.Add(box);
    }

    /// <summary>The type of an element's principal box.</summary>
    static BoxType TypeOf(UiElement element, ComputedStyle style)
    {
        var type = style.Display switch
        {
            Display.Flex or Display.InlineFlex => BoxType.Flex,
            Display.Grid or Display.InlineGrid => BoxType.Grid,
            Display.Inline => BoxType.Inline,
            _ => BoxType.Block,
        };
        if (IsReplaced(element))
            return BoxType.Replaced;
        if (element.Tag == "br" && type == BoxType.Inline)
            return BoxType.LineBreak;
        // Buttons are always atomic (they can't be plain inline boxes): an inline button is laid out as an inline-block.
        if (type == BoxType.Inline && IsButton(element))
            return BoxType.Block;
        return type;
    }

    /// <summary>The boxes inside an element's box: ::before, the marker, the children's boxes, ::after.</summary>
    List<LayoutBox> CollectChildren(UiElement element, LayoutBox box, ComputedStyle style)
    {
        var children = new List<LayoutBox>();
        if (element.BeforeStyle is { } before)
            BuildPseudo(element, before, PseudoKind.Before, children);
        box.Marker = null;
        if (style.Display == Display.ListItem && style.ListStyleType != "none")
        {
            // An inside marker is ordinary inline content; an outside one hangs off the first line.
            var markers = new List<LayoutBox>();
            BuildMarker(element, box, style, markers);
            if (style.ListStyleInside)
                children.AddRange(markers);
            else if (markers.Count > 0)
            {
                // Laid out on its own like an inline-block, then hung off the first line.
                box.Marker = markers[0];
                markers[0].Parent = box;
                markers[0].Type = BoxType.Block;
            }
        }
        BuildChildren(element, style, children);
        if (element.AfterStyle is { } after)
            BuildPseudo(element, after, PseudoKind.After, children);
        return children;
    }

    /// <summary>An element that keeps its box: what changed inside it is rebuilt.</summary>
    void Revisit(UiElement element)
    {
        if (element.ChildBoxesNeedRebuild)
        {
            Recollect(element);
            return;
        }
        if (element.DescendantBoxesNeedRebuild)
            foreach (var child in element.Children)
            {
                if (child is not UiElement childElement || !(childElement.BoxNeedsRebuild || childElement.ChildBoxesNeedRebuild || childElement.DescendantBoxesNeedRebuild))
                    continue;
                // A child with new boxes, or one whose boxes sit among ours (display: contents), changes our children.
                if (childElement.BoxNeedsRebuild || childElement.Box is null)
                {
                    Recollect(element);
                    return;
                }
                Revisit(childElement);
            }
        ClearFlags(element);
    }

    /// <summary>An element keeps its box but collects its child boxes again.</summary>
    void Recollect(UiElement element)
    {
        ClearFlags(element);
        if (element.Box is not { } box || element.Style is not { } style)
            return;
        if (box.Type is BoxType.Replaced or BoxType.LineBreak)
        {
            // Replaced content (select options, ...) may size the box.
            foreach (var child in element.ElementChildren)
                ClearBoxes(child);
            box.MarkNeedsLayout();
            return;
        }
        box.Children.Clear();
        // An inline box made a block for a block child it no longer has is inline again.
        box.Type = TypeOf(element, style);
        box.Fragments = null;
        Attach(box, CollectChildren(element, box, style));
        box.MarkNeedsLayout();
    }

    static bool IsButton(UiElement element) =>
        element.Tag == "button" || (element.Tag == "input" && (element.GetAttribute("type") ?? "").ToLowerInvariant() is "button" or "submit" or "reset");

    /// <summary>The element and its descendants have no boxes (display: none, removed, not rendered).</summary>
    public static void ClearBoxes(UiElement element)
    {
        element.Box = null;
        ClearFlags(element);
        foreach (var child in element.Children)
            if (child is UiElement childElement)
                ClearBoxes(childElement);
    }

    /// <summary>
    /// Which box is the containing block of each absolutely and fixed positioned box (CSS 2 §10.1):
    /// the nearest positioned (or transformed, filtered) ancestor, else the viewport. A containing
    /// block whose positioned boxes changed is laid out again.
    /// </summary>
    void AssignContainingBlocks()
    {
        var lists = new Dictionary<LayoutBox, List<LayoutBox>>();
        void Add(LayoutBox container, LayoutBox box, bool fixedToViewport)
        {
            if (!lists.TryGetValue(container, out var list))
                lists[container] = list = [];
            list.Add(box);
            var cb = fixedToViewport ? null : container;
            if (box.Container != cb)
            {
                box.Container = cb;
                box.MarkNeedsLayout();
            }
        }
        var containers = new List<LayoutBox>();
        void Walk(LayoutBox box, LayoutBox absolute, LayoutBox? fixedContainer)
        {
            if (box.PositionedDescendants is not null)
                containers.Add(box);
            foreach (var child in box.Children)
            {
                if (child.Element is null || child.Pseudo == PseudoKind.Marker || child.Type is BoxType.Text or BoxType.LineBreak)
                {
                    Walk(child, absolute, fixedContainer);
                    continue;
                }
                var s = child.Style;
                if (s.Position == PositionKind.Absolute)
                    Add(absolute, child, false);
                else if (s.Position == PositionKind.Fixed)
                    Add(fixedContainer ?? _viewport, child, fixedContainer is null);
                Walk(child, StyleDifference.ContainsPositioned(s) ? child : absolute, StyleDifference.ContainsFixed(s) ? child : fixedContainer);
            }
        }
        Walk(_viewport, _viewport, null);
        foreach (var container in containers)
            if (!lists.ContainsKey(container))
            {
                container.PositionedDescendants = null;
                container.MarkNeedsLayout();
            }
        foreach (var (container, list) in lists)
            if (container.PositionedDescendants is not { } old || !old.SequenceEqual(list))
            {
                container.PositionedDescendants = list;
                container.MarkNeedsLayout();
            }
    }

    void BuildChildren(UiElement element, ComputedStyle style, List<LayoutBox> output)
    {
        // A closed <details> shows only its summary.
        var closedDetails = element.Tag == "details" && !element.HasAttribute("open");
        var summarySeen = false;
        foreach (var child in element.Children)
        {
            switch (child)
            {
                case UiText text:
                    if (closedDetails || text.Text.Length == 0)
                        break;
                    var textBox = new LayoutBox(BoxType.Text, style) { Text = text.Text, TextNode = text, Element = null };
                    textBox.ReadStyleFrom(StyleSource.TextNode);
                    output.Add(textBox);
                    break;
                case UiElement childElement:
                    if (closedDetails)
                    {
                        if (childElement.Tag != "summary" || summarySeen)
                        {
                            ClearBoxes(childElement);
                            break;
                        }
                        summarySeen = true;
                    }
                    if (childElement.Tag is "option" or "optgroup" or "script" or "style" or "template" or "head" or "title" or "meta" or "link")
                    {
                        ClearBoxes(childElement);
                        break;
                    }
                    // List items are numbered among their siblings: their markers are made again.
                    if (childElement.Style?.Display == Display.ListItem)
                        childElement.BoxNeedsRebuild = true;
                    BuildElement(childElement, output);
                    break;
            }
        }
    }

    void BuildPseudo(UiElement element, ComputedStyle style, PseudoKind kind, List<LayoutBox> output)
    {
        if (style.Display == Display.None || style.Content is null)
            return;
        var text = string.Concat(style.Content.Select(item => item.Attribute is { } name ? element.GetAttribute(name) ?? "" : item.Text));
        var type = style.Display switch
        {
            Display.Flex or Display.InlineFlex => BoxType.Flex,
            Display.Grid or Display.InlineGrid => BoxType.Grid,
            Display.Inline or Display.Contents => BoxType.Inline,
            _ => BoxType.Block,
        };
        var box = new LayoutBox(type, style) { Element = element, Pseudo = kind };
        box.ReadStyleFrom(kind == PseudoKind.Before ? StyleSource.Before : StyleSource.After);
        var children = new List<LayoutBox>();
        if (text.Length > 0)
        {
            var textBox = new LayoutBox(BoxType.Text, style) { Text = text };
            textBox.ReadStyleFrom(StyleSource.Parent);
            children.Add(textBox);
        }
        Attach(box, children);
        output.Add(box);
    }

    void BuildMarker(UiElement element, LayoutBox item, ComputedStyle style, List<LayoutBox> output)
    {
        var text = MarkerText(element, style);
        if (text.Length == 0)
            return;
        if (style.ListStyleType is "disclosure-open" or "disclosure-closed" && style.ListStyleInside)
        {
            var triangle = new LayoutBox(BoxType.Block, LayoutBox.Derive(DerivedStyle.DisclosureMarker, style)) { Element = element, Pseudo = PseudoKind.Marker };
            triangle.DeriveStyle(DerivedStyle.DisclosureMarker, item);
            output.Add(triangle);
            return;
        }
        var box = new LayoutBox(BoxType.Inline, LayoutBox.Derive(DerivedStyle.Marker, style)) { Element = element, Pseudo = PseudoKind.Marker };
        box.DeriveStyle(DerivedStyle.Marker, item);
        var textBox = new LayoutBox(BoxType.Text, box.Style) { Text = text, Parent = box };
        textBox.ReadStyleFrom(StyleSource.Parent);
        box.Children.Add(textBox);
        output.Add(box);
    }

    static string MarkerText(UiElement element, ComputedStyle style)
    {
        if (style.ListStyleType.StartsWith("string:", StringComparison.Ordinal))
            return style.ListStyleType["string:".Length..];
        // The item's number: its position among list-item siblings (ol start / li value aren't used by the game).
        var number = 1;
        if (element.Parent is { } parent)
        {
            number = 0;
            foreach (var sibling in parent.ElementChildren)
            {
                if (sibling.Style?.Display == Display.ListItem)
                    number++;
                if (sibling == element)
                    break;
            }
            if (parent.Tag == "ol" && int.TryParse(parent.GetAttribute("start"), out var start))
                number += start - 1;
        }
        return style.ListStyleType switch
        {
            "disc" => "• ",
            "circle" => "◦ ",
            "square" => "▪ ",
            "disclosure-open" => "▾ ",
            "disclosure-closed" => "▸ ",
            "decimal" => number + ". ",
            "decimal-leading-zero" => number.ToString("00") + ". ",
            "lower-alpha" or "lower-latin" => Alphabetic(number, 'a') + ". ",
            "upper-alpha" or "upper-latin" => Alphabetic(number, 'A') + ". ",
            "lower-roman" => Roman(number).ToLowerInvariant() + ". ",
            "upper-roman" => Roman(number) + ". ",
            _ => "",
        };
    }

    static string Alphabetic(int n, char first)
    {
        var text = "";
        while (n > 0)
        {
            n--;
            text = (char)(first + n % 26) + text;
            n /= 26;
        }
        return text;
    }

    static string Roman(int n)
    {
        var values = new[] { 1000, 900, 500, 400, 100, 90, 50, 40, 10, 9, 5, 4, 1 };
        var symbols = new[] { "M", "CM", "D", "CD", "C", "XC", "L", "XL", "X", "IX", "V", "IV", "I" };
        var text = new System.Text.StringBuilder();
        for (var i = 0; i < values.Length; i++)
            while (n >= values[i])
            {
                text.Append(symbols[i]);
                n -= values[i];
            }
        return text.ToString();
    }

    /// <summary>Gives a box its children, adding the anonymous boxes its type needs.</summary>
    void Attach(LayoutBox box, List<LayoutBox> children)
    {
        switch (box.Type)
        {
            case BoxType.Flex:
            case BoxType.Grid:
                AttachItems(box, children);
                break;
            case BoxType.Inline:
                foreach (var child in children)
                {
                    if (!child.IsInlineLevel && !child.IsOutOfFlow)
                    {
                        // A block inside an inline box: CSS splits the inline around it. The engine makes
                        // the inline box a block instead (the game's markup never does this).
                        var diagnostic = $"block-level {child} inside inline {box}";
                        if (!_diagnostics.Contains(diagnostic))
                            _diagnostics.Add(diagnostic);
                        box.Type = BoxType.Block;
                        box.Fragments = null;
                        AttachBlock(box, children);
                        return;
                    }
                }
                foreach (var child in children)
                    Adopt(box, child);
                break;
            default:
                AttachBlock(box, children);
                break;
        }
    }

    static LayoutBox Anonymous(LayoutBox parent)
    {
        var anonymous = new LayoutBox(BoxType.Block, StyleEngine.Anonymous(parent.Style, Display.Block)) { IsAnonymous = true };
        anonymous.DeriveStyle(DerivedStyle.AnonymousBlock, parent);
        return anonymous;
    }

    static void Adopt(LayoutBox parent, LayoutBox child)
    {
        // A kept box under a new parent (or anonymous wrapper) is laid out again: its layout can depend on the parent.
        if (child.Parent != parent)
        {
            child.Parent = parent;
            child.Invalidate();
        }
        parent.Children.Add(child);
    }

    static bool IsCollapsibleWhitespace(LayoutBox box) =>
        box.Type == BoxType.Text && box.Style.WhiteSpace is Text.WhiteSpace.Normal or Text.WhiteSpace.NoWrap
        && box.Text.All(c => c is ' ' or '\t' or '\n' or '\r');

    /// <summary>A block container: all children inline-level, or all block-level with inline runs wrapped in anonymous blocks.</summary>
    void AttachBlock(LayoutBox box, List<LayoutBox> children)
    {
        var hasBlock = children.Any(c => !c.IsInlineLevel && !c.IsOutOfFlow);
        if (!hasBlock)
        {
            foreach (var child in children)
                Adopt(box, child);
            return;
        }
        List<LayoutBox>? run = null;
        void Flush()
        {
            if (run is null)
                return;
            // Runs of collapsible white space between blocks generate nothing.
            if (run.Any(c => !IsCollapsibleWhitespace(c) && !c.IsOutOfFlow))
            {
                var anonymous = Anonymous(box);
                foreach (var child in run)
                    Adopt(anonymous, child);
                Adopt(box, anonymous);
            }
            else
            {
                foreach (var child in run.Where(c => c.IsOutOfFlow))
                    Adopt(box, child);
            }
            run = null;
        }
        foreach (var child in children)
        {
            if (child.IsInlineLevel || (child.IsOutOfFlow && run is not null))
                (run ??= []).Add(child);
            else
            {
                Flush();
                Adopt(box, child);
            }
        }
        Flush();
    }

    /// <summary>Flex and grid containers: each in-flow child is an item; loose text gets an anonymous block item.</summary>
    void AttachItems(LayoutBox box, List<LayoutBox> children)
    {
        List<LayoutBox>? run = null;
        void Flush()
        {
            if (run is null)
                return;
            if (run.Any(c => !IsCollapsibleWhitespace(c)))
            {
                var anonymous = Anonymous(box);
                foreach (var child in run)
                    Adopt(anonymous, child);
                Adopt(box, anonymous);
            }
            run = null;
        }
        foreach (var child in children)
        {
            if (child.Type is BoxType.Text or BoxType.Inline or BoxType.LineBreak)
                (run ??= []).Add(child);
            else
            {
                Flush();
                Adopt(box, child);
            }
        }
        Flush();
    }
}

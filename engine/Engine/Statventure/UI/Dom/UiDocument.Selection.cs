using System.Numerics;
using System.Text;
using Statventure.UI.Layout;

namespace Statventure.UI.Dom;

/// <summary>A place in the DOM: an offset in a text node's text, or a child index in an element.</summary>
public readonly record struct DomPosition(UiNode Node, int Offset);

/// <summary>
/// The page's selection outside text fields (Blink's FrameSelection when it isn't in a text
/// control): an anchor and a focus. A press on content that can start a selection puts a caret
/// there (at the nearest text position, or in an empty block), a double press selects the word,
/// a triple press the paragraph, a drag extends; form controls other than text fields and
/// user-select: none start nothing. Every change fires selectionchange at the document. When a
/// text field takes the selection (focus), the page's goes away.
/// </summary>
public sealed partial class UiDocument
{
    DomPosition? _selectionAnchor, _selectionFocus;
    bool _documentSelectionChangePending;

    /// <summary>The text field the page's selection is in (the last one focus brought it to; null once the page has it).</summary>
    internal UiElement? SelectionField { get; private set; }

    /// <summary>
    /// The text field holding the page's selection while it isn't focused (getSelection() shows a
    /// caret at its place). An inert field's text has no caret position (Blink's VisibleSelection
    /// finds no candidate: inert content isn't selectable), so there's none then.
    /// </summary>
    public UiElement? SelectionHolder => SelectionField is { } holder && holder.Document == this && FocusedElement != holder && holder.Style is not { IsInert: true } ? holder : null;

    /// <summary>The page's selection (null when there is none, or the selection is in a text field).</summary>
    public (DomPosition Anchor, DomPosition Focus)? PageSelection =>
        _selectionAnchor is { } anchor && _selectionFocus is { } focus ? (anchor, focus) : null;

    /// <summary>Sets the page's selection; a change fires selectionchange at the document (once per frame).</summary>
    internal void SetPageSelection(DomPosition? anchor, DomPosition? focus)
    {
        if (anchor is null || focus is null)
            anchor = focus = null;
        if (anchor == _selectionAnchor && focus == _selectionFocus)
            return;
        _selectionAnchor = anchor;
        _selectionFocus = focus;
        SelectionField = null;
        Repaint();
        if (_documentSelectionChangePending)
            return;
        _documentSelectionChangePending = true;
        PostLateTask(() =>
        {
            _documentSelectionChangePending = false;
            DispatchEvent(new UiEvent("selectionchange", bubbles: false, cancelable: false) { IsTrusted = true });
        });
    }

    /// <summary>A text field took the selection: the page's goes (its field announces the change).</summary>
    internal void SelectionMovedIntoField(UiElement field)
    {
        SelectionField = field;
        if (_selectionAnchor is null)
            return;
        _selectionAnchor = _selectionFocus = null;
        Repaint();
    }

    /// <summary>The page's selection in document order: (start, end), or null.</summary>
    public (DomPosition Start, DomPosition End)? PageSelectionRange
    {
        get
        {
            if (PageSelection is not { } selection)
                return null;
            return Compare(selection.Anchor, selection.Focus) <= 0 ? (selection.Anchor, selection.Focus) : (selection.Focus, selection.Anchor);
        }
    }

    /// <summary>Whether a press on the element may start a selection (Blink's CanStartSelection, and form controls other than text fields don't).</summary>
    internal static bool CanStartSelection(UiElement element)
    {
        for (var e = element; e is not null; e = e.Parent)
            if (e.IsFormControl && !e.IsTextField)
                return false;
        return element.Style is not { UsedUserSelect: Style.UserSelect.None };
    }

    // ---------- Positions ----------

    /// <summary>Compares two positions in document order (negative: a first), as DOM boundary points.</summary>
    public static int Compare(DomPosition a, DomPosition b)
    {
        if (a.Node == b.Node)
            return a.Offset.CompareTo(b.Offset);
        var pathA = PathOf(a.Node);
        var pathB = PathOf(b.Node);
        var i = 0;
        while (i < pathA.Count && i < pathB.Count && pathA[i] == pathB[i])
            i++;
        if (i == pathA.Count)
        {
            // a's node contains b's: compare a's offset with the child holding b.
            var index = IndexOf(pathB[i]);
            return a.Offset <= index ? -1 : 1;
        }
        if (i == pathB.Count)
        {
            var index = IndexOf(pathA[i]);
            return b.Offset <= index ? 1 : -1;
        }
        return IndexOf(pathA[i]).CompareTo(IndexOf(pathB[i]));
    }

    internal static int IndexOf(UiNode node)
    {
        if (node.Parent is not { } parent)
            return 0;
        var children = parent.Children;
        for (var i = 0; i < children.Count; i++)
            if (children[i] == node)
                return i;
        return 0;
    }

    static List<UiNode> PathOf(UiNode node)
    {
        var path = new List<UiNode>();
        for (UiNode? n = node; n is not null; n = n.Parent)
            path.Add(n);
        path.Reverse();
        return path;
    }

    /// <summary>The position under a point (Blink's PositionForPoint): in the text there, or the nearest text of the block, or an empty block itself.</summary>
    internal DomPosition? PositionAt(HitResult hit, Vector2 point)
    {
        var local = Matrix3x2.Invert(hit.Transform, out var inverse) ? Vector2.Transform(point, inverse) : point;
        if (hit.TextItem is { } item && hit.Box.Type == BoxType.Text)
            return TextPosition(hit.Box, item, local.X - hit.TextRect.X + item.X);
        return PositionInBox(hit.Box, local) is { } position ? Canonical(position) : null;
    }

    /// <summary>The nearest caret stop in a text item (x in its line's space).</summary>
    static DomPosition? TextPosition(LayoutBox textBox, LineItem item, float x)
    {
        if (textBox.TextNode is not { } node || item.Clusters is not { Count: > 0 } clusters)
            return null;
        var text = InlineLayout.TextOf.TryGetValue(textBox, out var processed) ? processed : textBox.Text;
        var best = clusters[0].Start;
        var bestDistance = float.MaxValue;
        foreach (var cluster in clusters)
        {
            // Graphemes in a cluster share its width.
            var stops = new List<int> { cluster.Start };
            for (var b = Statventure.Text.TextBoundaries.NextGrapheme(text, cluster.Start); b < cluster.End; b = Statventure.Text.TextBoundaries.NextGrapheme(text, b))
                stops.Add(b);
            for (var k = 0; k <= stops.Count; k++)
            {
                var offset = k < stops.Count ? stops[k] : cluster.End;
                var at = cluster.X + cluster.Width * k / stops.Count;
                var distance = MathF.Abs(at - x);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = offset;
                }
            }
        }
        return new DomPosition(node, SourceOffset(textBox, best));
    }

    /// <summary>A processed text offset of a text box as an offset in its DOM text.</summary>
    internal static int SourceOffset(LayoutBox textBox, int processed)
    {
        if (!InlineLayout.SourceOffsetsOf.TryGetValue(textBox, out var origins) || origins.Length == 0)
            return processed;
        return origins[Math.Clamp(processed, 0, origins.Length - 1)];
    }

    /// <summary>A DOM text offset as the processed offset at or after it.</summary>
    internal static int ProcessedOffset(LayoutBox textBox, int source)
    {
        if (!InlineLayout.SourceOffsetsOf.TryGetValue(textBox, out var origins) || origins.Length == 0)
            return source;
        for (var i = 0; i < origins.Length; i++)
            if (origins[i] >= source)
                return i;
        return origins.Length - 1;
    }

    /// <summary>A position in a box for a point (untransformed viewport units).</summary>
    DomPosition? PositionInBox(LayoutBox box, Vector2 local)
    {
        var rect = box.ViewportRect;
        if (box.Inline is { Lines.Count: > 0 } inline)
        {
            var y = local.Y - rect.Y;
            var x = local.X - rect.X;
            var line = inline.Lines.FirstOrDefault(l => !l.Hidden && y < l.Top + l.Height) ?? inline.Lines.LastOrDefault(l => !l.Hidden);
            if (line is not null)
            {
                LineItem? nearest = null;
                var distance = float.MaxValue;
                foreach (var item in line.Items)
                {
                    if (item.Kind != LineItemKind.Text || item.Box?.TextNode is null)
                        continue;
                    var d = x < item.X ? item.X - x : x > item.X + item.Width ? x - item.X - item.Width : 0;
                    if (d < distance)
                    {
                        distance = d;
                        nearest = item;
                    }
                }
                if (nearest is not null)
                    return TextPosition(nearest.Box!, nearest, x);
            }
            return box.Element is { } element ? new DomPosition(element, 0) : null;
        }
        var children = box.Children.Where(c => !c.IsOutOfFlow && c.Type is not (BoxType.Text or BoxType.LineBreak)).ToList();
        if (children.Count > 0)
        {
            // LayoutBlock::PositionForPoint: the last child whose top is above the point (the first otherwise).
            var chosen = children[0];
            foreach (var child in children)
                if (child.ViewportRect.Y <= local.Y)
                    chosen = child;
            return PositionInBox(chosen, local);
        }
        return box.Element is { } owner ? new DomPosition(owner, 0) : null;
    }

    /// <summary>The text box showing a text node (null when it isn't rendered).</summary>
    static LayoutBox? TextBoxOf(UiText node)
    {
        static LayoutBox? Find(LayoutBox box, UiText node)
        {
            foreach (var child in box.Children)
            {
                if (child.Type == BoxType.Text && child.TextNode == node)
                    return child;
                if (child.Element is null || child.Element == node.Parent)
                    if (Find(child, node) is { } found)
                        return found;
            }
            return null;
        }
        return node.Parent?.Box is { } parent ? Find(parent, node) : null;
    }

    /// <summary>The word around a position in a text node (as in text fields, over the node's rendered text).</summary>
    internal static (DomPosition Start, DomPosition End)? WordAround(DomPosition position)
    {
        if (position.Node is not UiText node || TextBoxOf(node) is not { } box)
            return null;
        var text = InlineLayout.TextOf.TryGetValue(box, out var processed) ? processed : box.Text;
        var offset = ProcessedOffset(box, position.Offset);
        var atEnd = offset >= text.Length;
        var start = Statventure.Text.TextEditing.StartOfWord(text, offset, nextWordIfOnBoundary: !atEnd);
        var end = atEnd || text[offset] == '\n' ? offset : Statventure.Text.TextEditing.EndOfWord(text, offset, previousWordIfOnBoundary: atEnd);
        return (new DomPosition(node, SourceOffset(box, Math.Min(start, end))), new DomPosition(node, SourceOffset(box, Math.Max(start, end))));
    }

    /// <summary>The paragraph around a position: from the first to the last text of its block.</summary>
    internal (DomPosition Start, DomPosition End)? ParagraphAround(DomPosition position)
    {
        var block = position.Node switch
        {
            UiText text => TextBoxOf(text),
            UiElement element => element.Box,
            _ => null,
        };
        // The block whose lines hold the position.
        while (block is not null && block.Inline is null)
            block = block.Parent;
        if (block?.Inline is not { } inline)
            return null;
        DomPosition? first = null, last = null;
        UiText? lastNode = null;
        foreach (var line in inline.Lines)
            foreach (var item in line.Items)
                if (item.Kind == LineItemKind.Text && item.Box?.TextNode is { } node)
                {
                    first ??= new DomPosition(node, SourceOffset(item.Box, item.TextStart));
                    last = new DomPosition(node, SourceOffset(item.Box, item.TextEnd));
                    lastNode = node;
                }
        if (first is not { } s || last is not { } e)
            return null;
        // The end goes on to the next position: the start of the next rendered text (before its node).
        var passed = false;
        foreach (var node in DescendantNodes(Root))
        {
            if (node == lastNode)
            {
                passed = true;
                continue;
            }
            if (passed && node is UiText next && RenderedText(next) is { Length: > 0 } && next.Parent is { } parent)
                return (s, new DomPosition(parent, IndexOf(next)));
        }
        return (s, e);
    }

    /// <summary>Ctrl+A outside a text field: everything in the body (its first and last text positions).</summary>
    internal void SelectAllPage() =>
        SetPageSelection(Canonical(new DomPosition(Body, 0)), Canonical(new DomPosition(Body, Body.Children.Count)));

    /// <summary>A text node's rendered text (null when it has no text box).</summary>
    static string? RenderedText(UiText node) =>
        TextBoxOf(node) is { } box ? InlineLayout.TextOf.TryGetValue(box, out var processed) ? processed : box.Text : null;

    /// <summary>
    /// Blink's CanonicalPosition for a position between nodes: an empty block keeps it; otherwise the
    /// first text position after it if that is in the same block, else the last one before it, else
    /// the next one.
    /// </summary>
    internal DomPosition Canonical(DomPosition position)
    {
        if (position.Node is not UiElement element)
            return position;
        if (element.Box is { } own && own.Type is not (BoxType.Inline or BoxType.Text) && own.Height > 0 && !HasRenderedText(element))
            return position;
        DomPosition? next = null, previous = null;
        foreach (var node in DescendantNodes(Root))
        {
            if (node is not UiText text || RenderedText(text) is not { Length: > 0 } rendered || TextBoxOf(text) is not { } box)
                continue;
            var start = new DomPosition(text, SourceOffset(box, 0));
            if (Compare(start, position) >= 0)
            {
                next = start;
                break;
            }
            previous = new DomPosition(text, SourceOffset(box, rendered.Length));
        }
        static UiElement? BlockOf(UiNode node)
        {
            for (var e = node as UiElement ?? node.Parent; e is not null; e = e.Parent)
                if (e.Box is { } box && box.Type is not (BoxType.Inline or BoxType.Text))
                    return e;
            return null;
        }
        if (next is { } n && BlockOf(n.Node) == BlockOf(element))
            return n;
        return previous ?? next ?? position;
    }

    bool HasRenderedText(UiElement element)
    {
        foreach (var node in DescendantNodes(element))
            if (node is UiText text && RenderedText(text) is { Length: > 0 })
                return true;
        return false;
    }

    /// <summary>
    /// The selected text, as copied and as getSelection().toString() (Blink's TextIterator): the
    /// rendered text of the text in the range; a line break before and after in-flow blocks (an
    /// extra one after paragraphs) and for &lt;br&gt;.
    /// </summary>
    public string PageSelectionText()
    {
        if (PageSelectionRange is not { } range)
            return "";
        var result = new StringBuilder();
        var emitted = false;
        var needsAnotherNewline = false;
        UiText? lastText = null;
        void Emit(string text)
        {
            if (text.Length == 0)
                return;
            if (needsAnotherNewline)
            {
                result.Append('\n');
                needsAnotherNewline = false;
            }
            result.Append(text);
            emitted = true;
        }
        static bool BlockBreaks(UiElement element) =>
            element.Box is { } box && box.Type is not (BoxType.Inline or BoxType.Text or BoxType.LineBreak) && !box.IsOutOfFlow
            && element.Tag != "body" && !box.IsAtomicInline;
        bool HasRenderedAfter(UiElement element)
        {
            for (UiNode? n = element; n is not null; n = n.Parent)
                for (var sibling = n.NextSibling; sibling is not null; sibling = sibling.NextSibling)
                    if (sibling is UiElement { Box: not null } || (sibling is UiText text && TextBoxOf(text) is not null))
                        return true;
            return false;
        }
        bool InRange(UiNode node, int length) =>
            Compare(new DomPosition(node, length), range.Start) > 0 && Compare(new DomPosition(node, 0), range.End) < 0;
        void Walk(UiElement element)
        {
            foreach (var child in element.Children)
            {
                if (child is UiText text)
                {
                    if (TextBoxOf(text) is not { } box || !InRange(text, text.Text.Length))
                        continue;
                    var processed = InlineLayout.TextOf.TryGetValue(box, out var p) ? p : box.Text;
                    var from = range.Start.Node == text ? ProcessedOffset(box, range.Start.Offset) : 0;
                    var to = range.End.Node == text ? ProcessedOffset(box, range.End.Offset) : processed.Length;
                    if (to > from)
                        Emit(processed[from..to]);
                    lastText = text;
                    continue;
                }
                if (child is not UiElement inner)
                    continue;
                // Elements wholly outside the range are skipped.
                if (Compare(new DomPosition(inner, inner.Children.Count), range.Start) <= 0 && range.Start.Node != inner && !range.Start.Node.IsDescendantOf(inner))
                    continue;
                if (Compare(new DomPosition(element, IndexOf(inner)), range.End) >= 0)
                    break;
                if (inner.Box is null)
                    continue;
                if (inner.Tag == "br")
                {
                    Emit("\n");
                    continue;
                }
                var breaks = BlockBreaks(inner);
                if (breaks && emitted && result.Length > 0 && result[^1] != '\n')
                    Emit("\n");
                Walk(inner);
                if (breaks && lastText is not null && emitted && HasRenderedAfter(inner))
                {
                    if (result.Length > 0 && result[^1] != '\n')
                    {
                        result.Append('\n');
                        needsAnotherNewline = inner.Tag == "p";
                    }
                    else if (inner.Tag == "p")
                        result.Append('\n');
                }
            }
        }
        Walk(Root);
        return result.ToString();
    }

    static IEnumerable<UiNode> DescendantNodes(UiElement element)
    {
        foreach (var child in element.Children)
        {
            yield return child;
            if (child is UiElement inner)
                foreach (var descendant in DescendantNodes(inner))
                    yield return descendant;
        }
    }
}

using System.Numerics;
using Statventure.Canvas;
using Statventure.Core;
using Statventure.Text;
using Statventure.UI.Dom;
using Statventure.UI.Layout;
using Statventure.UI.Style;

namespace Statventure.UI.Paint;

public sealed partial class UiPainter
{
    /// <summary>The texture an engine surface element shows (a 3D view), by element.</summary>
    public Func<UiElement, int?>? SurfaceTexture { get; set; }

    // ---------- Lines ----------

    /// <summary>The offset of relatively and sticky positioned inline boxes around a box inside an IFC.</summary>
    static Vector2 InlineOffset(LayoutBox box, LayoutBox root)
    {
        var offset = Vector2.Zero;
        for (var b = box; b is not null && b != root; b = b.Parent)
            if (b.Type == BoxType.Inline)
                offset += b.RelativeOffset + b.StickyOffset;
        return offset;
    }

    void PaintLines(LayoutBox root, InlineContent inline)
    {
        var origin = root.ViewportPosition;
        var breaks = _document.PageSelectionRange is not null ? LineBreaksOf(root) : null;
        for (var lineIndex = 0; lineIndex < inline.Lines.Count; lineIndex++)
        {
            var line = inline.Lines[lineIndex];
            if (line.Hidden)
                continue;
            if (breaks?.TryGetValue(lineIndex, out var lineBreak) == true)
                PaintBreakSelection(root, origin, lineBreak, line);
            foreach (var item in line.Items)
            {
                switch (item.Kind)
                {
                    case LineItemKind.InlineBox:
                        PaintInlineFragment(root, origin, item);
                        break;
                    case LineItemKind.Text:
                    case LineItemKind.Generated:
                        // A text fragment: one display item, in its inline parent's (or block's) contents.
                        _canvas.UseState(_properties.ContentsOf(item.Box?.Parent ?? root), item.Box?.Parent ?? root);
                        _canvas.BeginItem();
                        PaintText(root, origin, item, line, lineIndex);
                        _canvas.EndItem();
                        break;
                    case LineItemKind.Atomic:
                        if (item.Box is { } atomic && InNormalFlow(atomic))
                            PaintPseudoContext(atomic);
                        break;
                }
            }
        }
    }

    void PaintInlineFragment(LayoutBox root, Vector2 origin, LineItem item)
    {
        var box = item.Box!;
        var s = box.Style;
        if (s.Visibility != Visibility.Visible)
            return;
        var fragment = item.Fragment;
        var rect = fragment.Rect.Offset(origin + InlineOffset(box, root));
        if (rect.Width <= 0 && rect.Height <= 0)
            return;
        // box-decoration-break: slice — the start edge only on the first fragment, the end edge only on the last.
        var radii = BorderRadii(box, rect.Size);
        if (!fragment.IsFirst)
            radii = radii with { TopLeft = Vector2.Zero, BottomLeft = Vector2.Zero };
        if (!fragment.IsLast)
            radii = radii with { TopRight = Vector2.Zero, BottomRight = Vector2.Zero };
        if (PropertiesOf(box) is { } properties)
            _canvas.UseState(properties.BorderBox, box);
        _canvas.BeginItem(boxDecoration: true, InkOverflowRect(s, rect));
        PaintBoxDecorations(box, s, rect, radii, SidesOf(s, left: fragment.IsFirst, right: fragment.IsLast), skipBackground: false);
        _canvas.EndItem();
        Hit(box, rect, radii);
    }

    void PaintText(LayoutBox root, Vector2 origin, LineItem item, LineBox line, int lineIndex)
    {
        var style = item.Style;
        if (style.Visibility != Visibility.Visible || item.Run is not { } run)
            return;
        var offset = item.Box is { } textBox ? InlineOffset(textBox, root) : Vector2.Zero;
        var unrounded = origin + offset + new Vector2(item.X, item.Baseline);
        // ComputePhysicalBoxGeometry: the text's box moves with its line's top rounded to a pixel, so
        // glyphs, shadows and decorations start from there; selection and hit testing don't.
        var lineTop = origin.Y + line.Top;
        var baseline = unrounded with { Y = unrounded.Y + MathF.Floor(lineTop + 0.5f) - lineTop };
        // The page's selection over this text, with its highlight (a preserved newline has no glyphs, only that).
        var selection = PageSelectionOf(root, item, line);
        if (run.Glyphs.Count > 0)
            _contentful = true;
        if (run.Glyphs.Count == 0)
        {
            if (_canvas.Drawing && selection is { } newline && item.Box?.TextNode?.Parent is { } newlineOwner)
            {
                var background = SelectionColors(newlineOwner).Background;
                var highlight = SelectionHighlight(root, item, line, unrounded, offset, newline.From, newline.To, newline.LineBreak);
                if (background.A > 0 && highlight.Width > 0)
                    _canvas.FillRect(highlight, background);
            }
            return;
        }
        // Text takes input in its font's box (ascent and descent of the primary font, as Chromium's text fragments).
        Hit(item.Box ?? root, new RectF(unrounded.X, unrounded.Y - item.Ascent, item.Width, item.Ascent + item.Descent), CornerRadii.Zero, text: true, item: item);
        if (!_canvas.Drawing)
            return;
        DrawTextShadows(run, baseline, style, item.Width, item.Ascent, item.Descent);
        var decorations = DecorationsOf(root, item, line, lineIndex, origin, baseline);
        if (selection is { } range && item.Box?.TextNode?.Parent is { } owner)
        {
            var (background, text) = SelectionColors(owner);
            var highlight = SelectionHighlight(root, item, line, unrounded, offset, range.From, range.To, range.LineBreak);
            GlyphRun Slice(int from, int to, bool overlapping) => SliceItemRun(item, run, from, to, overlapping);
            // Decorated text takes Blink's overlay path, other text the fast one.
            if (decorations is not null)
                PaintOverlaySelection(decorations, Slice, baseline, style.Color, item.TextStart, item.TextEnd, range.From, range.To, highlight, background, text,
                    at => baseline.X + ItemX(item, at) - item.X);
            else
                PaintSelectedGlyphs(Slice, baseline, style.Color, item.TextStart, item.TextEnd, range.From, range.To, highlight, background, text);
            return;
        }
        // Underlines and overlines under the text, line-throughs over it.
        if (decorations is not null)
            PaintDecorationLines(decorations, lineThrough: false);
        _canvas.DrawGlyphs(run, baseline, style.Color);
        if (decorations is not null)
            PaintDecorationLines(decorations, lineThrough: true);
    }

    /// <summary>Text shadows under glyphs: the first listed is on top (ascent and descent bound a blurred one's layer).</summary>
    void DrawTextShadows(GlyphRun run, Vector2 baseline, ComputedStyle style, float width, float? ascent = null, float? descent = null)
    {
        if (style.TextShadows.Length == 0)
            return;
        var up = ascent ?? style.FontSize * 1.25f;
        var down = descent ?? style.FontSize * 0.5f;
        for (var i = style.TextShadows.Length - 1; i >= 0; i--)
        {
            var shadow = style.TextShadows[i];
            var at = baseline + new Vector2(shadow.OffsetX, shadow.OffsetY);
            if (shadow.Blur > 0)
            {
                var bounds = new RectF(at.X - shadow.Blur * 2, at.Y - up - shadow.Blur * 2, width + shadow.Blur * 4, up + down + shadow.Blur * 4);
                _canvas.PushLayer(new LayerOptions { Filters = [Filter.Blur(shadow.Blur / 2)], Bounds = bounds });
                _canvas.DrawGlyphs(run, at, shadow.Color);
                _canvas.PopLayer();
            }
            else
                _canvas.DrawGlyphs(run, at, shadow.Color);
        }
    }

    /// <summary>
    /// A text item's selection highlight (InlineCursor::CurrentLocalSelectionRectForText): the selected
    /// range's advance, a space wider when the line's end is selected (a preserved newline is a space
    /// wide itself), as tall as the line box and the text together, on whole pixels.
    /// </summary>
    RectF SelectionHighlight(LayoutBox root, LineItem item, LineBox line, Vector2 baseline, Vector2 offset, int from, int to, bool lineBreak)
    {
        var style = item.Style;
        var left = baseline.X + ItemX(item, from) - item.X;
        var right = baseline.X + ItemX(item, to) - item.X;
        if (lineBreak)
            right += SpaceWidth(style);
        var lineTop = root.ViewportPosition.Y + offset.Y + line.Top;
        var top = MathF.Min(lineTop, baseline.Y - item.Ascent);
        var bottom = MathF.Max(lineTop + line.Height, baseline.Y + item.Descent);
        return Snap(RectF.FromEdges(left, top, right, bottom));
    }

    /// <summary>
    /// Text with part of it selected, as Blink's fast selection painting: the glyphs before and after
    /// the selected range in the text's colour, the highlight over them, then the selected glyphs in
    /// the selection's colour. Those are clipped to the highlight (and drawn outside it in the text's
    /// colour) unless the highlight, a pixel larger, holds all the text's ink or all the text is selected.
    /// </summary>
    void PaintSelectedGlyphs(Func<int, int, bool, GlyphRun> slice, Vector2 origin, Color color, int textStart, int textEnd,
        int from, int to, RectF highlight, Color background, Color selectedColor)
    {
        if (textStart < from)
            _canvas.DrawGlyphs(slice(textStart, from, false), origin, color);
        if (to < textEnd)
            _canvas.DrawGlyphs(slice(to, textEnd, false), origin, color);
        if (background.A > 0 && highlight.Width > 0 && highlight.Height > 0)
            _canvas.FillRect(highlight, background);
        if (from >= to)
            return;
        var whole = slice(textStart, textEnd, false);
        if ((from <= textStart && to >= textEnd) || highlight.Inflate(1).ContainsRect(InkBounds(whole, origin)))
        {
            _canvas.DrawGlyphs(slice(from, to, false), origin, selectedColor);
            return;
        }
        var selected = slice(from, to, true);
        foreach (var outside in Outside(highlight))
        {
            _canvas.PushClip(outside);
            _canvas.DrawGlyphs(selected, origin, color);
            _canvas.PopClip();
        }
        _canvas.PushClip(highlight);
        _canvas.DrawGlyphs(selected, origin, selectedColor);
        _canvas.PopClip();
    }

    /// <summary>Four rectangles that together cover everything outside <paramref name="hole"/> (a clip-out).</summary>
    static RectF[] Outside(RectF hole)
    {
        const float Far = 1e6f;
        return
        [
            RectF.FromEdges(-Far, -Far, hole.Left, Far),
            RectF.FromEdges(hole.Right, -Far, Far, Far),
            RectF.FromEdges(hole.Left, -Far, hole.Right, hole.Top),
            RectF.FromEdges(hole.Left, hole.Bottom, hole.Right, Far),
        ];
    }

    /// <summary>The whole-pixel bounds of glyphs' ink (their outlines' extents) drawn at <paramref name="origin"/>.</summary>
    static RectF InkBounds(GlyphRun run, Vector2 origin)
    {
        var face = run.Font.Face;
        var scale = run.Style.Size / face.UnitsPerEm;
        float left = float.MaxValue, top = float.MaxValue, right = float.MinValue, bottom = float.MinValue;
        foreach (var glyph in run.Glyphs)
        {
            if (!face.HbFont.TryGetGlyphExtents(glyph.Glyph, out var extents) || extents.Width == 0 || extents.Height == 0)
                continue;
            var x = origin.X + glyph.X + extents.XBearing * scale;
            var y = origin.Y + glyph.Y - extents.YBearing * scale;
            left = MathF.Min(left, x);
            right = MathF.Max(right, x + extents.Width * scale);
            top = MathF.Min(top, y);
            bottom = MathF.Max(bottom, y - extents.Height * scale);
        }
        return left > right ? RectF.Empty : RectF.FromEdges(MathF.Floor(left), MathF.Floor(top), MathF.Ceiling(right), MathF.Ceiling(bottom));
    }

    /// <summary>
    /// The glyphs of a text item's clusters that start in [from, to) (Blink slicing a shape result by
    /// offsets); overlapping, those of clusters with any part in it (a ligature split by the range).
    /// </summary>
    static GlyphRun SliceItemRun(LineItem item, GlyphRun run, int from, int to, bool overlapping)
    {
        var slice = new GlyphRun { Font = run.Font, Style = run.Style, X = run.X, Width = run.Width };
        if (item.Clusters is not { } clusters)
        {
            if (from <= item.TextStart && to >= item.TextEnd)
                slice.Glyphs.AddRange(run.Glyphs);
            return slice;
        }
        foreach (var cluster in clusters)
            if (overlapping ? cluster.Start < to && cluster.End > from : cluster.Start >= from && cluster.Start < to)
                for (var g = 0; g < cluster.GlyphCount; g++)
                    slice.Glyphs.Add(run.Glyphs[cluster.GlyphStart + g]);
        return slice;
    }

    /// <summary>The x of a processed text offset in a text item (in its line's space; graphemes in a shaped cluster share it).</summary>
    static float ItemX(LineItem item, int offset)
    {
        if (item.Clusters is not { } clusters || offset >= item.TextEnd)
            return item.X + item.Width;
        foreach (var cluster in clusters)
        {
            if (offset < cluster.Start || offset >= cluster.End)
                continue;
            if (offset == cluster.Start)
                return cluster.X;
            var text = Layout.InlineLayout.TextOf.TryGetValue(item.Box!, out var processed) ? processed : item.Box!.Text;
            var stops = 1;
            var before = 0;
            for (var b = Text.TextBoundaries.NextGrapheme(text, cluster.Start); b < cluster.End; b = Text.TextBoundaries.NextGrapheme(text, b))
            {
                stops++;
                if (b <= offset)
                    before++;
            }
            return cluster.X + cluster.Width * before / stops;
        }
        return item.X;
    }

    /// <summary>A space's advance in the style's primary font plus the letter spacing (Blink's Font::SpaceWidth), in layout units.</summary>
    float SpaceWidth(ComputedStyle style)
    {
        var font = _document.Fonts.Resolve(style.FontFamilies, style.FontWeight, style.FontItalic) ?? _document.Fonts.ResolveDefault(style.FontWeight, style.FontItalic);
        if (font is not { } resolved || resolved.Face.GlyphFor(' ') is var space && space == 0)
            return style.LetterSpacing;
        var width = resolved.Face.HbFont.GetHorizontalGlyphAdvance(space) * style.FontSize / resolved.Face.UnitsPerEm + style.LetterSpacing;
        return MathF.Truncate(width * 64) / 64;
    }

    /// <summary>The forced line breaks (br) of an IFC, by line: they end their lines, after every item.</summary>
    static Dictionary<int, LayoutBox> LineBreaksOf(LayoutBox root)
    {
        var breaks = new Dictionary<int, LayoutBox>();
        void Walk(LayoutBox box)
        {
            foreach (var child in box.Children)
            {
                if (child.Type == BoxType.LineBreak && child.InlineRoot == root && child.Fragments is [var fragment, ..])
                    breaks[fragment.Line] = child;
                else if (child.Type == BoxType.Inline)
                    Walk(child);
            }
        }
        Walk(root);
        return breaks;
    }

    /// <summary>
    /// A selected br (its "\n" inside the page's selection) highlights a space's width where it is,
    /// as tall as the line and its font (InlineCursor::CurrentLocalSelectionRectForText), except the
    /// page's very last box.
    /// </summary>
    void PaintBreakSelection(LayoutBox root, Vector2 origin, LayoutBox lineBreak, LineBox line)
    {
        if (!_canvas.Drawing || _document.PageSelectionRange is not { } range || lineBreak.Element is not { Parent: { } parent } element
            || lineBreak.Fragments is not [var fragment, ..] || lineBreak.Style.Visibility != Visibility.Visible)
            return;
        var index = UiDocument.IndexOf(element);
        if (UiDocument.Compare(range.Start, new DomPosition(parent, index)) > 0 || UiDocument.Compare(range.End, new DomPosition(parent, index + 1)) < 0)
            return;
        // IsLastBRInPage: nothing follows it in the box tree.
        var last = true;
        for (var b = lineBreak; b.Parent is { } boxParent && last; b = boxParent)
            last = boxParent.Children[^1] == b;
        if (last)
            return;
        var offset = InlineOffset(lineBreak, root);
        var rect = fragment.Rect.Offset(origin + offset);
        var lineTop = origin.Y + offset.Y + line.Top;
        var highlight = Snap(RectF.FromEdges(rect.X, MathF.Min(lineTop, rect.Top), rect.X + SpaceWidth(lineBreak.Style), MathF.Max(lineTop + line.Height, rect.Bottom)));
        var background = SelectionColors(element).Background;
        if (background.A > 0 && highlight.Width > 0)
            _canvas.FillRect(highlight, background);
    }

    /// <summary>
    /// The part of a text item the page's selection covers (processed offsets), and whether the
    /// line's end is selected too (LayoutSelection::ComputeSelectionStatus: the selection goes on
    /// past this text, the line's last leaf; Blink then highlights a space's width more). A
    /// preserved newline's own character is that line end.
    /// </summary>
    (int From, int To, bool LineBreak)? PageSelectionOf(LayoutBox root, LineItem item, LineBox line)
    {
        if (_document.PageSelectionRange is not { } range || item.Box is not { TextNode: { } node } box)
            return null;
        if (UiDocument.Compare(range.Start, range.End) == 0)
            return null;
        int start, end;
        if (range.Start.Node == node)
            start = UiDocument.ProcessedOffset(box, range.Start.Offset);
        else
            start = UiDocument.Compare(range.Start, new DomPosition(node, 0)) <= 0 ? int.MinValue : int.MaxValue;
        if (range.End.Node == node)
            end = UiDocument.ProcessedOffset(box, range.End.Offset);
        else
            end = UiDocument.Compare(range.End, new DomPosition(node, node.Text.Length)) >= 0 ? int.MaxValue : int.MinValue;
        if (IsNewline(item))
            return start <= item.TextStart && end > item.TextStart ? (item.TextStart, item.TextStart, true) : null;
        var from = Math.Max(item.TextStart, start);
        var to = Math.Min(item.TextEnd, end);
        var lineBreak = start <= item.TextEnd && end > item.TextEnd && IsBeforeSoftLineBreak(root, item, line) && !TruncatesOverflowingText(root);
        if (to < from || (to == from && !lineBreak))
            return null;
        return (from, Math.Max(from, to), lineBreak);
    }

    /// <summary>A preserved newline's item: no glyphs, at a "\n" of the processed text.</summary>
    static bool IsNewline(LineItem item) =>
        item.Kind == LineItemKind.Text && item.TextStart == item.TextEnd && item.Box is { } box
        && InlineLayout.TextOf.TryGetValue(box, out var text) && item.TextStart < text.Length && text[item.TextStart] == '\n';

    /// <summary>
    /// InlineCursor::IsBeforeSoftLineBreak: the item is its line's last leaf (no br after it), and
    /// the line isn't an inline-block's last.
    /// </summary>
    static bool IsBeforeSoftLineBreak(LayoutBox root, LineItem item, LineBox line)
    {
        if (line.Items.Count == 0 || line.Items[^1] != item || root.Inline is not { } inline)
            return false;
        var lineIndex = inline.Lines.IndexOf(line);
        if (root.IsAtomicInline && lineIndex == inline.Lines.Count - 1)
            return false;
        return !LineBreaksOf(root).ContainsKey(lineIndex);
    }

    /// <summary>LayoutBlockFlow::ShouldTruncateOverflowingText: the block (an anonymous one's parent) clips its overflow with an ellipsis.</summary>
    static bool TruncatesOverflowingText(LayoutBox root)
    {
        var box = root.IsAnonymous && root.Parent is { } parent ? parent : root;
        var s = box.Style;
        return s.TextOverflow != TextOverflow.Clip && (s.OverflowX != Overflow.Visible || s.OverflowY != Overflow.Visible);
    }

    /// <summary>
    /// Selection colours (HighlightStyleUtils): the author's ::selection colours if it has any;
    /// else Chromium's, Highlight and HighlightText while the window is active, a translucent grey
    /// and dark grey text when it isn't.
    /// </summary>
    internal (Color Background, Color Text) SelectionColors(UiElement owner)
    {
        if (owner.Styles?.Selection is { } pseudo)
            return (pseudo.BackgroundColor, pseudo.Color);
        return _document.WindowFocused
            ? (Color.FromBytes(0, 65, 198, 204), Color.FromBytes(255, 255, 255))
            : (Color.FromBytes(162, 162, 162, 153), Color.FromBytes(50, 50, 50));
    }

    // ---------- Markers ----------

    void PaintMarker(LayoutBox marker)
    {
        if (marker.Style.Visibility != Visibility.Visible)
            return;
        // Outside markers take no input in Chromium (the point hits what is behind them).
        _canvas.IgnoreHits();
        if (marker.Inline is { } inline)
            PaintLines(marker, inline);
        _canvas.TakeHits();
    }

    /// <summary>An inside disclosure marker (a summary's triangle), drawn as Chromium draws it.</summary>
    /// <summary>
    /// A details/summary disclosure triangle (Blink's TextFragmentPainter::PaintSymbol): a square of
    /// 0.66em standing on the baseline at the marker's start, filled with Blink's canonical points —
    /// pointing to the inline end when closed, down when open.
    /// </summary>
    void PaintDisclosure(LayoutBox marker, bool open)
    {
        var rect = marker.ViewportRect;
        var s = marker.Style;
        var size = MathF.Floor(s.FontSize * 0.66f * 64) / 64;
        // The marker box has no content, so the line's baseline is its bottom edge. Blink paints a
        // line's text fragments with the line's top rounded to a whole pixel (ComputePhysicalBoxGeometry).
        var shift = 0f;
        if (marker.Parent is { Inline: { } inline } root)
            foreach (var line in inline.Lines)
                if (line.Items.Any(i => i.Box == marker))
                {
                    var lineTop = root.ViewportPosition.Y + line.Top;
                    shift = MathF.Floor(lineTop + 0.5f) - lineTop;
                    break;
                }
        var box = new RectF(rect.X, rect.Bottom - size + shift, size, size);
        ReadOnlySpan<Vector2> points = open
            ? [new(0f, 0.07f), new(0.5f, 0.93f), new(1f, 0.07f)]
            : [new(0f, 0f), new(0.86f, 0.5f), new(0f, 1f)];
        var path = new Canvas.Path().MoveTo(box.Position + points[0] * size);
        for (var i = 1; i < points.Length; i++)
            path.LineTo(box.Position + points[i] * size);
        path.Close();
        _canvas.FillPath(path, Brush.Solid(s.Color));
    }

    // ---------- Replaced content ----------

    /// <summary>A replaced element's content (one display item, in its contents state; an inline svg's shapes are items of their own).</summary>
    void PaintReplaced(LayoutBox box)
    {
        if (box.Style.Visibility != Visibility.Visible || box.Element is null)
            return;
        if (PropertiesOf(box) is { } properties)
            _canvas.UseState(properties.Contents, box);
        var oneItem = box.Element.Tag != "svg";
        if (oneItem)
            _canvas.BeginItem();
        PaintReplacedContent(box);
        if (oneItem)
            _canvas.EndItem();
    }

    void PaintReplacedContent(LayoutBox box)
    {
        if (box.Element is not { } element)
            return;
        var rect = Snap(box.ViewportRect);
        var content = Snap(new RectF(box.ViewportRect.X + box.ContentLeft, box.ViewportRect.Y + box.ContentTop, box.ContentWidth, box.ContentHeight));
        switch (element.Tag)
        {
            case "img":
                if (element.GetAttribute("src") is not { } src)
                    break;
                if (SvgImage(src) is { } svgImage)
                {
                    _contentful = true;
                    // An SVG image: drawn as vectors in its object-fit box.
                    var natural = svgImage.Size ?? (svgImage.Ratio > 0 ? new Vector2(content.Width, content.Width / svgImage.Ratio) : content.Size);
                    PaintFittedSvg(svgImage.Svg, natural, content, box);
                }
                else if (ImageTexture?.Invoke(src) is { } texture && _document.Layout.Context.ImageSize?.Invoke(src) is { } natural)
                {
                    _contentful = true;
                    DrawFitted(texture, natural, content, box);
                }
                break;
            case "canvas":
            case "surface":
                if (SurfaceTexture?.Invoke(element) is { } surface)
                {
                    _contentful = true;
                    // A drawn canvas is a layer of its own in Chromium (a foreign layer, its compositing reason Canvas).
                    if (PropertiesOf(box) is { } properties)
                        _canvas.RecordForeign(properties.Contents, box, content);
                    _canvas.SuppressChunks();
                    _canvas.DrawImage(surface, content);
                    _canvas.ResumeChunks();
                }
                break;
            case "svg":
                _contentful = true;
                PaintSvg(box, element, content);
                break;
            default:
                PaintControl(box, element, rect, content);
                break;
        }
    }

    /// <summary>An image in its content box by object-fit (centred, as object-position's default).</summary>
    void DrawFitted(int texture, Vector2 natural, RectF content, LayoutBox box)
    {
        if (natural.X <= 0 || natural.Y <= 0 || content.Width <= 0 || content.Height <= 0)
            return;
        var size = box.Style.ObjectFit switch
        {
            ObjectFit.Contain => natural * MathF.Min(content.Width / natural.X, content.Height / natural.Y),
            ObjectFit.Cover => natural * MathF.Max(content.Width / natural.X, content.Height / natural.Y),
            ObjectFit.None => natural,
            ObjectFit.ScaleDown => natural * MathF.Min(1, MathF.Min(content.Width / natural.X, content.Height / natural.Y)),
            _ => content.Size,
        };
        var placed = new RectF(content.X + (content.Width - size.X) / 2, content.Y + (content.Height - size.Y) / 2, size.X, size.Y);
        _canvas.PushClip(content);
        _canvas.DrawImage(texture, placed);
        _canvas.PopClip();
    }

    void PaintFittedSvg(UiElement svg, Vector2 natural, RectF content, LayoutBox box)
    {
        if (natural.X <= 0 || natural.Y <= 0)
            natural = content.Size;
        var size = box.Style.ObjectFit switch
        {
            ObjectFit.Contain => natural * MathF.Min(content.Width / natural.X, content.Height / natural.Y),
            ObjectFit.Cover => natural * MathF.Max(content.Width / natural.X, content.Height / natural.Y),
            ObjectFit.None => natural,
            ObjectFit.ScaleDown => natural * MathF.Min(1, MathF.Min(content.Width / natural.X, content.Height / natural.Y)),
            _ => content.Size,
        };
        var placed = new RectF(content.X + (content.Width - size.X) / 2, content.Y + (content.Height - size.Y) / 2, size.X, size.Y);
        _canvas.PushClip(content);
        PaintSvgTree(svg, placed, box.Style.Color, clip: false);
        _canvas.PopClip();
    }
}

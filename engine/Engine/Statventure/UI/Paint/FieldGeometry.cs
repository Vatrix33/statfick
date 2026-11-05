using System.Numerics;
using Statventure.Core;
using Statventure.Text;
using Statventure.UI.Dom;
using Statventure.UI.Layout;
using Statventure.UI.Style;

namespace Statventure.UI.Paint;

/// <summary>
/// A text field's editor text as it is laid out and placed, shared by painting, the caret and
/// hitting text with the pointer: the editor area (the content box less a spin or cancel button),
/// the text's layout (bullets for passwords), and where the layout's origin goes (scrolled
/// sideways by the editor's scroll; vertically centred in a single-line field, its baseline on a
/// whole pixel). Viewport units, untransformed.
/// </summary>
internal sealed class FieldGeometry
{
    public required UiElement Element { get; init; }
    public required TextLayout Layout { get; init; }
    public required RectF Editor { get; init; }
    /// <summary>What the text shows through: a one-line field's editor, a textarea's padding box (less its scrollbars).</summary>
    public required RectF Clip { get; init; }
    public required Vector2 Origin { get; init; }
    public required bool Multiline { get; init; }
    /// <summary>The height of a line (for the caret in an empty field).</summary>
    public required float LineHeight { get; init; }
    /// <summary>The layout's top before its baseline is put on a whole pixel (where Blink's inner editor is).</summary>
    public required float LayoutTop { get; init; }
    /// <summary>The left of the editor's content as scrolled (the inner editor's origin).</summary>
    public required float InnerLeft { get; init; }
    /// <summary>The primary font's rounded ascent and descent: a text fragment's height, and the caret's.</summary>
    public required float Ascent { get; init; }
    public required float Descent { get; init; }
    public required TextAlign Align { get; init; }

    /// <summary>
    /// The caret's width: Blink's BarCaretWidth, one window unit in device pixels (at least one
    /// pixel), as a LayoutUnit.
    /// </summary>
    public float CaretWidth { get; init; } = 1;

    /// <summary>
    /// The caret before <paramref name="offset"/> (Blink's ComputeLocalCaretRectAtTextOffset):
    /// <see cref="CaretWidth"/> wide and as tall as the text's font, half its width left of the
    /// position, rounded to a pixel inside the line box and the editor. An empty editor's
    /// (LocalCaretRectForEmptyElement) is at its start, middle or end, centred on the line.
    /// Viewport units, not yet on whole pixels.
    /// </summary>
    public RectF CaretRect(int offset)
    {
        var (x, lineIndex) = Layout.CaretPosition(offset);
        var fontHeight = Ascent + Descent;
        var width = CaretWidth;
        if (lineIndex < 0)
        {
            var at = Align switch { TextAlign.Center => Editor.Width / 2, TextAlign.Right => Editor.Width - width, _ => 0 };
            at = MathF.Min(at, MathF.Max(0, Editor.Width - width));
            return new RectF(InnerLeft + at, LayoutTop + (LineHeight - fontHeight) / 2, width, fontHeight);
        }
        var line = Layout.Lines[lineIndex];
        var lineLeft = Origin.X - InnerLeft + line.Left;
        var min = MathF.Ceiling(lineLeft);
        var max = MathF.Floor(MathF.Max(Editor.Width, lineLeft + line.Width) - width);
        // Half the width as LayoutUnit halves it (raw / 2).
        var half = MathF.Floor(width * 64 / 2) / 64;
        var left = min >= max ? max : MathF.Floor(Math.Clamp(Origin.X - InnerLeft + x - half, min, max) + 0.5f);
        return new RectF(InnerLeft + left, LayoutTop + line.Baseline - Ascent, width, fontHeight);
    }

    /// <summary>The caret stop nearest to a point (viewport units).</summary>
    public int OffsetAt(Vector2 point) => Layout.Lines.Count == 0 ? 0 : Layout.OffsetAt(point.X - Origin.X, point.Y - Origin.Y);

    /// <summary>The rectangles a selection covers, line by line.</summary>
    public IEnumerable<RectF> SelectionRects(int start, int end)
    {
        for (var i = 0; i < Layout.Lines.Count; i++)
        {
            var line = Layout.Lines[i];
            if (end <= line.TextStart && !(start == end))
                continue;
            var (offsets, xs) = Layout.CaretStops(i);
            if (offsets.Length == 0 || start > offsets[^1] || end < offsets[0])
                continue;
            float X(int offset)
            {
                for (var k = 0; k < offsets.Length; k++)
                    if (offsets[k] >= offset)
                        return xs[k];
                return xs[^1];
            }
            var from = Math.Max(start, offsets[0]);
            var to = Math.Min(end, offsets[^1]);
            if (to <= from)
                continue;
            yield return new RectF(Origin.X + X(from), Origin.Y + line.Top, X(to) - X(from), line.Height);
        }
    }

    /// <summary>
    /// A field's content box and its editor (the content box less the number field's spin button or
    /// the search field's cancel button), where layout put them: Blink paints the inner editor's
    /// text at its fractional position (glyphs land on quarter pixels).
    /// </summary>
    public static (RectF Content, RectF Editor) EditorRects(LayoutBox box, UiElement element, Vector2 shift = default)
    {
        var rect = box.ViewportRect.Offset(shift);
        var content = new RectF(rect.X + box.ContentLeft, rect.Y + box.ContentTop, box.ContentWidth, box.ContentHeight);
        var editor = content;
        if (element.InputType == "number" && UiPainter.SpinButtonOf(box, element, shift) is { } spin)
            editor = editor with { Width = MathF.Max(0, editor.Width - spin.Width) };
        else if (element.InputType == "search" && UiPainter.CancelButtonOf(box, element, shift) is { } cancel)
            editor = editor with { Width = MathF.Max(0, cancel.Button.X - cancel.MarginLeft - editor.X) };
        return (content, editor);
    }

    /// <summary>The geometry of a field's editor text (null for elements that aren't text fields).</summary>
    public static FieldGeometry? Of(UiDocument document, LayoutBox box, UiElement element, Vector2 shift = default)
    {
        if (!element.IsTextField)
            return null;
        var style = box.Style;
        var rect = box.ViewportRect.Offset(shift);
        var (content, editor) = EditorRects(box, element, shift);
        var multiline = element.Tag == "textarea";
        var text = element.EditorText;
        if (element.InputType == "password")
            text = new string('•', text.Length);
        var paragraph = new ParagraphStyle
        {
            WhiteSpace = multiline ? WhiteSpace.PreWrap : WhiteSpace.Pre,
            MaxWidth = multiline ? editor.Width : float.PositiveInfinity,
            Align = style.TextAlign switch
            {
                TextAlignment.Center => TextAlign.Center,
                TextAlignment.Right or TextAlignment.End => TextAlign.Right,
                _ => TextAlign.Left,
            },
        };
        var layout = TextLayout.Create(document.Fonts, text, InlineLayout.TextStyleFor(style), paragraph);
        var lineHeight = ReplacedContent.FieldLineHeight(style, document.Fonts);
        var height = layout.Lines.Count > 0 ? layout.Height : lineHeight;
        // A textarea scrolls its text (the box is the scroller); a one-line field's inner editor scrolls sideways.
        var scroll = multiline ? element.LayoutScroll : new Vector2(element.EditorScrollLeft, 0);
        var top = multiline ? content.Y - scroll.Y : content.Y + (content.Height - height) / 2;
        var y = layout.Lines.Count > 0 ? UiPainter.SnapBaseline(top, layout) : MathF.Round(top);
        var x = editor.X - scroll.X;
        // Text too wide for the field starts at the start edge.
        if (!multiline && paragraph.Align != TextAlign.Left && layout.Width <= editor.Width)
            x += paragraph.Align == TextAlign.Center ? (editor.Width - layout.Width) / 2 : editor.Width - layout.Width;
        var font = document.Fonts.Resolve(style.FontFamilies, style.FontWeight, style.FontItalic) ?? document.Fonts.ResolveDefault(style.FontWeight, style.FontItalic);
        var (ascent, descent, _) = font is { } resolved ? TextLayout.Metrics(resolved.Face, style.FontSize, style.SubpixelPositioning) : (style.FontSize, 0f, 0f);
        return new FieldGeometry
        {
            Element = element, Layout = layout, Editor = editor, Origin = new Vector2(x, y),
            CaretWidth = MathF.Floor(MathF.Max(1, document.DeviceScale) * 64) / 64, Multiline = multiline, LineHeight = lineHeight, LayoutTop = top,
            InnerLeft = editor.X - scroll.X, Ascent = ascent, Descent = descent, Align = paragraph.Align,
            Clip = multiline ? new RectF(rect.X + box.Border.Left, rect.Y + box.Border.Top, box.PaddingBoxWidth, box.PaddingBoxHeight) : editor,
        };
    }
}

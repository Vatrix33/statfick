using System.Numerics;
using Statventure.Canvas;
using Statventure.Core;
using Statventure.Text;
using Statventure.UI.Css;
using Statventure.UI.Dom;
using Statventure.UI.Layout;
using Statventure.UI.Style;

namespace Statventure.UI.Paint;

public sealed partial class UiPainter
{
    /// <summary>Chromium's inner spin button (number fields): 15 px wide, the content box's height.</summary>
    internal const float SpinButtonWidth = 15;

    /// <summary>Blink's GetWebThemeState: disabled (also read-only), pressed (:active), hovered, normal.</summary>
    internal static ThemeState StateOf(UiElement element) =>
        element.IsDisabled || (element.IsFormControl && element.ReadOnly) ? ThemeState.Disabled
        : element.IsActive ? ThemeState.Pressed
        : element.IsHovered ? ThemeState.Hovered
        : ThemeState.Normal;

    /// <summary>The accent colour (accent-color, made opaque over the canvas colour), or null for the theme's.</summary>
    static Color? AccentOf(ComputedStyle s)
    {
        if (s.AccentColor is not { } accent)
            return null;
        if (accent.A >= 1)
            return accent;
        var a = accent.A;
        return new Color(accent.R * a + (1 - a), accent.G * a + (1 - a), accent.B * a + (1 - a), 1);
    }

    // ---------- The native look (the box decoration phase) ----------

    void PaintTheme(LayoutBox box, UiElement element, ControlLook look, RectF rect)
    {
        if (!_canvas.Drawing)
            return;
        var s = box.Style;
        var state = StateOf(element);
        switch (look)
        {
            case ControlLook.Checkbox:
            case ControlLook.Radio:
            {
                // ThemePainterDefault::ApplyZoomToRect: drawn unzoomed (the size over the zoom, in whole
                // pixels) on a canvas scaled by the zoom around the rect's corner.
                var zoom = s.Zoom;
                var unzoomed = zoom == 1 ? rect : new RectF(rect.X, rect.Y, (int)(rect.Width / zoom), (int)(rect.Height / zoom));
                if (zoom != 1)
                    _canvas.PushTransform(Matrix3x2.CreateTranslation(-rect.X, -rect.Y) * Matrix3x2.CreateScale(zoom) * Matrix3x2.CreateTranslation(rect.X, rect.Y));
                if (look == ControlLook.Checkbox)
                    ControlTheme.PaintCheckbox(_canvas, unzoomed, state, element.Checked, element.Indeterminate, AccentOf(s));
                else
                    ControlTheme.PaintRadio(_canvas, unzoomed, state, element.Checked, AccentOf(s));
                if (zoom != 1)
                    _canvas.PopTransform();
                break;
            }
            case ControlLook.PushButton:
            case ControlLook.Button:
                ControlTheme.PaintButton(_canvas, rect, state, s.Zoom);
                break;
            case ControlLook.TextField:
            case ControlLook.SearchField:
            case ControlLook.TextArea:
                ControlTheme.PaintTextField(_canvas, rect, state, s.BackgroundColor, hasBorder: true, s.Zoom);
                break;
            case ControlLook.Menulist:
                if (!s.HasBorderRadius)
                    ControlTheme.PaintTextField(_canvas, rect, state, s.BackgroundColor, hasBorder: s.BorderTop > 0 || s.BorderRight > 0 || s.BorderBottom > 0 || s.BorderLeft > 0, s.Zoom);
                PaintMenuListArrow(box, s, rect);
                break;
            case ControlLook.ProgressBar:
            {
                var position = ProgressPosition(element);
                ControlTheme.PaintProgressBar(_canvas, rect, state, MathF.Max(0, position), position >= 0, AccentOf(s), s.Zoom);
                break;
            }
            case ControlLook.SliderHorizontal:
            {
                var thumb = RangeThumb(box, element, _shift);
                ControlTheme.PaintSliderTrack(_canvas, rect, state, MathF.Round(thumb.X - rect.X, MidpointRounding.AwayFromZero), AccentOf(s), s.Zoom);
                break;
            }
        }
    }

    /// <summary>
    /// ThemePainterDefault::SetupMenuListArrow: an 8 px arrow (scaled as the arrow box is) centred
    /// in the arrow box (15 px at the device scale or zoomed, ClampedMenuListArrowPaddingSize) at the inline end.
    /// </summary>
    void PaintMenuListArrow(LayoutBox box, ComputedStyle s, RectF rect)
    {
        if (!_canvas.Drawing)
            return;
        var right = (int)rect.X + (int)rect.Width - (int)MathF.Floor(s.BorderRight);
        var middle = (int)rect.Y + (int)rect.Height / 2;
        var arrowBox = ReplacedContent.ArrowBoxWidth(s, _document.DeviceScale);
        var arrowSize = 8 * arrowBox / SpinButtonWidth;
        var arrowX = (int)(right - (arrowBox + arrowSize) / 2);
        ControlTheme.PaintMenuListArrow(_canvas, rect, arrowX, middle, arrowSize, s.Color);
    }

    /// <summary>A progress element's position: value / max in [0, 1], or -1 when indeterminate (no valid value).</summary>
    internal static float ProgressPosition(UiElement element)
    {
        if (UiElement.ParseNumber(element.GetAttribute("value")) is not { } value)
            return -1;
        var max = UiElement.ParseNumber(element.GetAttribute("max")) is { } m && m > 0 ? m : 1;
        return (float)Math.Clamp(value / max, 0, 1);
    }

    /// <summary>
    /// A range input's thumb (Blink's LayoutSliderTrack): 16×16 in the track, which fills the
    /// content box's width and sits in its middle; at the value's share of the track less the thumb.
    /// </summary>
    internal static RectF RangeThumb(LayoutBox box, UiElement element, Vector2 shift = default)
    {
        var rect = box.ViewportRect.Offset(shift);
        var content = new RectF(rect.X + box.ContentLeft, rect.Y + box.ContentTop, box.ContentWidth, box.ContentHeight);
        var thumbStyle = element.Styles?.Part(PseudoElement.SliderThumb);
        // LayoutTheme::AdjustSliderThumbSize: the theme's 16 px, zoomed.
        var size = Layout.LayoutUnit.Truncate(16 * box.Style.Zoom);
        if (thumbStyle is not null && thumbStyle.Appearance == "none")
        {
            size = thumbStyle.Width.IsAuto ? 0 : thumbStyle.Width.ResolveOr(content.Width, 0);
        }
        var range = element.StepRangeOf(rejectAny: true);
        var value = UiElement.ParseNumber(element.Value) ?? range.DefaultValue;
        var proportion = range.Maximum == range.Minimum ? 0 : (float)((value - range.Minimum) / (range.Maximum - range.Minimum));
        proportion = Math.Clamp(proportion, 0, 1);
        // Positions are in layout units (1/64 px).
        var offset = MathF.Floor(proportion * (content.Width - size) * 64) / 64;
        var y = content.Y + (content.Height - size) / 2;
        return new RectF(content.X + offset, y, size, size);
    }

    // ---------- Content: text, values and parts ----------

    /// <summary>Form controls' content: field text, the select's value, the slider thumb, spin and cancel buttons, styled progress parts, input buttons' labels.</summary>
    void PaintControl(LayoutBox box, UiElement element, RectF rect, RectF content)
    {
        var s = box.Style;
        switch (element.Tag)
        {
            case "input":
                switch (element.InputType)
                {
                    case "checkbox":
                    case "radio":
                    case "color":
                    case "file":
                    case "hidden":
                        return;
                    case "range":
                        PaintRangeThumb(box, element);
                        return;
                    case "submit":
                    case "reset":
                    case "button":
                        PaintCenteredLabel(box, ReplacedContent.InputButtonLabel(element), content);
                        return;
                    default:
                        PaintTextField(box, element, content);
                        return;
                }
            case "textarea":
                PaintFieldText(box, element, content, content, element.EditorText, multiline: true);
                PaintFieldSelection(box, element);
                return;
            case "select":
                PaintSelect(box, element, content);
                return;
            case "progress":
                if (ControlAppearance.Effective(element, s) == ControlLook.None)
                    PaintProgressParts(element, content);
                return;
            case "meter":
                PaintMeter(box, element, content);
                return;
        }
    }

    void PaintRangeThumb(LayoutBox box, UiElement element)
    {
        if (!_canvas.Drawing || ControlAppearance.Effective(element, box.Style) == ControlLook.None)
            return;
        var thumb = Snap(RangeThumb(box, element, _shift));
        var state = element.IsDisabled ? ThemeState.Disabled
            : _document.RangeThumbActive == element ? ThemeState.Pressed
            : _document.RangeThumbHovered == element ? ThemeState.Hovered
            : ThemeState.Normal;
        ControlTheme.PaintSliderThumb(_canvas, thumb, state, AccentOf(box.Style));
    }

    /// <summary>A box's content box where layout put it (not snapped to pixels): text inside controls is painted from there.</summary>
    static RectF ExactContent(LayoutBox box, Vector2 shift)
    {
        var rect = box.ViewportRect.Offset(shift);
        return new RectF(rect.X + box.ContentLeft, rect.Y + box.ContentTop, box.ContentWidth, box.ContentHeight);
    }

    /// <summary>The top of a text layout placed at <paramref name="top"/>, moved so its baseline is on a whole pixel (Skia rounds glyphs' y).</summary>
    internal static float SnapBaseline(float top, TextLayout layout)
    {
        var baseline = MathF.Floor(top + layout.FirstBaseline + 0.5f);
        return baseline - layout.FirstBaseline;
    }

    /// <summary>A label centred in the content box (input buttons), clipped to it.</summary>
    void PaintCenteredLabel(LayoutBox box, string label, RectF content)
    {
        if (label.Length == 0)
            return;
        var style = box.Style;
        var layout = TextLayout.Create(_document.Fonts, label, InlineLayout.TextStyleFor(style), new ParagraphStyle { WhiteSpace = Text.WhiteSpace.Pre });
        var exact = ExactContent(box, _shift);
        var x = exact.X + (exact.Width - layout.Width) / 2;
        var y = SnapBaseline(exact.Y + (exact.Height - layout.Height) / 2, layout);
        _canvas.PushClip(content);
        _canvas.DrawText(layout, new Vector2(x, y), style.Color);
        _canvas.PopClip();
    }

    /// <summary>
    /// A single-line text field: its text (or placeholder) in the editor, which is the content box
    /// less the number field's spin button or the search field's cancel button; those are drawn
    /// when their part style shows them (on hover and focus).
    /// </summary>
    void PaintTextField(LayoutBox box, UiElement element, RectF content)
    {
        var editor = content;
        var type = element.InputType;
        if (type == "number" && SpinButtonOf(box, element, _shift) is { } spin)
        {
            editor = editor with { Width = MathF.Max(0, editor.Width - spin.Width) };
            PaintSpinButton(element, spin);
        }
        else if (type == "search" && CancelButtonOf(box, element, _shift) is { } cancel)
        {
            editor = editor with { Width = MathF.Max(0, cancel.Button.X - cancel.MarginLeft - editor.X) };
            PaintCancelButton(element, cancel.Button);
        }
        PaintFieldText(box, element, content, editor, element.EditorText);
        PaintFieldSelection(box, element);
    }

    /// <summary>The number field's spin button (null when its part has appearance or display none, or the field's look is textfield).</summary>
    internal static RectF? SpinButtonOf(LayoutBox box, UiElement element, Vector2 shift = default)
    {
        var s = box.Style;
        if (element.InputType != "number" || s.Appearance is "none" or "textfield")
            return null;
        var part = element.Styles?.Part(PseudoElement.InnerSpinButton);
        if (part is not null && (part.Display == Display.None || part.Appearance == "none"))
            return null;
        var rect = box.ViewportRect.Offset(shift);
        var content = new RectF(rect.X + box.ContentLeft, rect.Y + box.ContentTop, box.ContentWidth, box.ContentHeight);
        // The button stretches over the field's line (the decoration container), which is centred
        // in the content box (-internal-align-content-block: center), in layout units.
        var height = element.Document is { } document ? ReplacedContent.FieldLineHeight(s, document.Fonts) : content.Height;
        var top = MathF.Floor((content.Height - height) / 2 * 64) / 64;
        // LayoutTheme::AdjustInnerSpinButtonStyle: the theme's 15 px, zoomed.
        var width = Layout.LayoutUnit.Truncate(SpinButtonWidth * s.Zoom);
        return new RectF(content.Right - width, content.Y + top, width, height);
    }

    void PaintSpinButton(UiElement element, RectF spin)
    {
        var part = element.Styles?.Part(PseudoElement.InnerSpinButton);
        if (!_canvas.Drawing || part is null || part.Opacity <= 0 || part.Visibility != Visibility.Visible)
            return;
        var state = element.IsDisabled || element.ReadOnly ? ThemeState.Disabled
            : _document.SpinButtonPressed == element ? ThemeState.Pressed
            : _document.SpinButtonHovered == element ? ThemeState.Hovered
            : ThemeState.Normal;
        var layered = part.Opacity < 1;
        if (layered)
            _canvas.PushLayer(new LayerOptions { Opacity = part.Opacity });
        ControlTheme.PaintInnerSpinButton(_canvas, Snap(spin), state, _document.SpinButtonUpHalf);
        if (layered)
            _canvas.PopLayer();
    }

    /// <summary>
    /// The search field's cancel button (LayoutThemeDefault): 9 px at 13 px text, scaled with the
    /// font (5 to 21 px), centred vertically after the editor, with 1 px and 3 px margins.
    /// </summary>
    internal static (RectF Button, float MarginLeft)? CancelButtonOf(LayoutBox box, UiElement element, Vector2 shift = default)
    {
        var part = element.Styles?.Part(PseudoElement.SearchCancelButton);
        if (part is null || part.Display == Display.None)
            return null;
        var s = box.Style;
        var size = (float)Math.Round(Math.Min(Math.Max(5, 9 * part.FontSize / 13f), 21), MidpointRounding.AwayFromZero);
        if (part.Appearance == "none")
            size = 0;
        var rect = box.ViewportRect.Offset(shift);
        var content = new RectF(rect.X + box.ContentLeft, rect.Y + box.ContentTop, box.ContentWidth, box.ContentHeight);
        float marginLeft = 1, marginRight = 3;
        var x = content.Right - marginRight - size;
        var y = content.Y + (content.Height - size) / 2;
        _ = s;
        return (new RectF(x, y, size, size), marginLeft);
    }

    void PaintCancelButton(UiElement element, RectF button)
    {
        var part = element.Styles?.Part(PseudoElement.SearchCancelButton);
        if (!_canvas.Drawing || part is null || part.Opacity <= 0 || element.Value.Length == 0 || button.Width <= 0)
            return;
        // ThemePainterDefault::PaintSearchFieldCancelButton: the cancel image in the button's content box.
        var size = MathF.Min(button.Width, button.Height);
        var painting = new RectF(MathF.Round(button.X), MathF.Round(button.Y + (button.Height - size + 1) / 2), size, size);
        var pressed = _document.CancelButtonPressed == element;
        var layered = part.Opacity < 1;
        if (layered)
            _canvas.PushLayer(new LayerOptions { Opacity = part.Opacity });
        DrawSearchCancel(painting, pressed);
        if (layered)
            _canvas.PopLayer();
    }

    /// <summary>A text field's text (or placeholder): one line centred in the content box (text areas: from its top), clipped to the editor.</summary>
    void PaintFieldText(LayoutBox box, UiElement element, RectF content, RectF editor, string value, bool multiline = false)
    {
        var style = box.Style;
        var text = value;
        var textStyle = style;
        if (text.Length == 0 && element.GetAttribute("placeholder") is { Length: > 0 } placeholder)
        {
            text = multiline ? placeholder : placeholder.Replace("\r", "").Replace("\n", "");
            textStyle = element.Styles?.Placeholder ?? style;
        }
        if (text.Length == 0 || !_canvas.Drawing)
            return;
        if (element.InputType == "password" && text == value)
            text = new string('•', text.Length);
        var paragraph = new ParagraphStyle
        {
            WhiteSpace = multiline ? Text.WhiteSpace.PreWrap : Text.WhiteSpace.Pre,
            MaxWidth = multiline ? editor.Width : float.PositiveInfinity,
            Align = style.TextAlign switch
            {
                TextAlignment.Center => TextAlign.Center,
                TextAlignment.Right or TextAlignment.End => TextAlign.Right,
                _ => TextAlign.Left,
            },
        };
        var color = textStyle.Color;
        // A textarea's text scrolls inside its padding box; a one-line field's inside its editor.
        var rect = Placed(box);
        var clip = multiline ? new RectF(rect.X + box.Border.Left, rect.Y + box.Border.Top, box.PaddingBoxWidth, box.PaddingBoxHeight) : editor;
        // The focused field holds the page's selection: its selected range paints as Blink paints selected text.
        var (start, end, _) = element.EditorSelection;
        if (text == value && start != end && _document.FocusedElement == element && FieldGeometry.Of(_document, box, element, _shift) is { } geometry)
        {
            _canvas.PushClip(clip);
            PaintFieldTextSelected(geometry, style, color, start, end);
            _canvas.PopClip();
            return;
        }
        var layout = TextLayout.Create(_document.Fonts, text, InlineLayout.TextStyleFor(textStyle) with { Color = color, Underline = false }, paragraph);
        var lineHeight = layout.Height;
        // The text sits where layout put the editor, not on the snapped content box.
        var (exactContent, exactEditor) = FieldGeometry.EditorRects(box, element, _shift);
        if (multiline)
            exactEditor = exactContent;
        var y = SnapBaseline(multiline ? exactContent.Y - element.LayoutScroll.Y : exactContent.Y + (exactContent.Height - lineHeight) / 2, layout);
        var x = exactEditor.X - (multiline ? element.LayoutScroll.X : element.EditorScrollLeft);
        // Text too wide for the field starts at the start edge.
        if (!multiline && paragraph.Align != TextAlign.Left && layout.Width <= exactEditor.Width)
            x += paragraph.Align == TextAlign.Center ? (exactEditor.Width - layout.Width) / 2 : exactEditor.Width - layout.Width;
        _canvas.PushClip(clip);
        PaintFieldLayout(layout, new Vector2(x, y), textStyle, color);
        _canvas.PopClip();
    }

    /// <summary>
    /// A focused field's text with a selected range, run by run as Blink paints text fragments: the
    /// unselected glyphs, the run's highlight (to the line's end and a space more when the selection
    /// goes on to the next line), then the selected glyphs in the selection colour. A line with
    /// only a newline highlights a space's width.
    /// </summary>
    void PaintFieldTextSelected(FieldGeometry geometry, ComputedStyle style, Color color, int start, int end)
    {
        var (background, selectedColor) = SelectionColors(geometry.Element);
        var layout = geometry.Layout;
        var text = layout.Text;
        var space = SpaceWidth(style);
        for (var i = 0; i < layout.Lines.Count; i++)
        {
            var line = layout.Lines[i];
            var lineEnd = line.TextEnd > line.TextStart && line.TextEnd <= text.Length && text[line.TextEnd - 1] == '\n' ? line.TextEnd - 1 : line.TextEnd;
            var (offsets, xs) = layout.CaretStops(i);
            float X(int offset)
            {
                for (var k = 0; k < offsets.Length; k++)
                    if (offsets[k] >= offset)
                        return xs[k];
                return xs[^1];
            }
            var lineTop = geometry.Origin.Y + line.Top;
            var top = MathF.Min(lineTop, geometry.LayoutTop + line.Baseline - geometry.Ascent);
            var bottom = MathF.Max(lineTop + line.Height, geometry.LayoutTop + line.Baseline + geometry.Descent);
            var continues = start <= lineEnd && end > lineEnd;
            if (line.Runs.Count == 0)
            {
                if (continues && background.A > 0)
                {
                    var x = geometry.Origin.X + line.Left;
                    _canvas.FillRect(Snap(RectF.FromEdges(x, top, x + space, bottom)), background);
                }
                continue;
            }
            for (var r = 0; r < line.Runs.Count; r++)
            {
                var run = line.Runs[r];
                if (run.Glyphs.Count == 0)
                    continue;
                var runStart = run.Glyphs.Min(g => g.Cluster);
                var runEnd = r + 1 < line.Runs.Count && line.Runs[r + 1].Glyphs.Count > 0 ? line.Runs[r + 1].Glyphs.Min(g => g.Cluster) : lineEnd;
                if (runEnd < runStart)
                    (runStart, runEnd) = (runEnd, runStart);
                var origin = geometry.Origin + new Vector2(line.Left + run.X, line.Baseline);
                var from = Math.Max(start, runStart);
                var to = Math.Min(end, runEnd);
                var lineBreak = r == line.Runs.Count - 1 && continues;
                if (to < from || (to == from && !lineBreak))
                {
                    PaintFieldRun(layout, run, origin, style, color);
                    continue;
                }
                to = Math.Max(from, to);
                var left = geometry.Origin.X + X(from);
                var right = geometry.Origin.X + X(to) + (lineBreak ? space : 0);
                var highlight = Snap(RectF.FromEdges(left, top, right, bottom));
                GlyphRun Slice(int a, int b, bool overlapping) => SliceLayoutRun(run, runEnd, a, b, overlapping);
                DrawTextShadows(run, origin, style, run.Width);
                if (FieldRunDecorations(style, layout, run, origin) is { } decorations)
                    PaintOverlaySelection(decorations, Slice, origin, color, runStart, runEnd, from, to, highlight, background, selectedColor, at => geometry.Origin.X + X(at));
                else
                    PaintSelectedGlyphs(Slice, origin, color, runStart, runEnd, from, to, highlight, background, selectedColor);
            }
        }
    }

    /// <summary>A field's laid-out text, run by run, with its shadows and decorations.</summary>
    void PaintFieldLayout(TextLayout layout, Vector2 position, ComputedStyle style, Color color)
    {
        foreach (var line in layout.Lines)
            foreach (var run in line.Runs)
                PaintFieldRun(layout, run, position + new Vector2(line.Left + run.X, line.Baseline), style, color);
    }

    void PaintFieldRun(TextLayout layout, GlyphRun run, Vector2 origin, ComputedStyle style, Color color)
    {
        DrawTextShadows(run, origin, style, run.Width);
        var decorations = FieldRunDecorations(style, layout, run, origin);
        if (decorations is not null)
            PaintDecorationLines(decorations, lineThrough: false);
        _canvas.DrawGlyphs(run, origin, color);
        if (decorations is not null)
            PaintDecorationLines(decorations, lineThrough: true);
    }

    /// <summary>The glyphs of a laid-out run whose clusters start in [from, to); overlapping, those of clusters with any part in it.</summary>
    static GlyphRun SliceLayoutRun(GlyphRun run, int runEnd, int from, int to, bool overlapping)
    {
        var slice = new GlyphRun { Font = run.Font, Style = run.Style, X = run.X, Width = run.Width };
        var starts = run.Glyphs.Select(g => g.Cluster).Distinct().Order().ToArray();
        foreach (var glyph in run.Glyphs)
        {
            var index = Array.BinarySearch(starts, glyph.Cluster);
            var clusterEnd = index + 1 < starts.Length ? starts[index + 1] : runEnd;
            if (overlapping ? glyph.Cluster < to && clusterEnd > from : glyph.Cluster >= from && glyph.Cluster < to)
                slice.Glyphs.Add(glyph);
        }
        return slice;
    }

    /// <summary>A focused field's caret: 1 px wide and as tall as its font, in caret-color, while its blink is on.</summary>
    void PaintFieldSelection(LayoutBox box, UiElement element)
    {
        if (_document.FocusedElement != element || !_canvas.Drawing || FieldGeometry.Of(_document, box, element, _shift) is not { } geometry)
            return;
        var shown = _document.CaretShown(element);
        _document.CaretPainted(shown);
        if (!shown)
            return;
        var (_, focus) = element.EditorAnchorAndFocus;
        // Painted on whole pixels (ToPixelSnappedRect), clipped to the editor.
        var caret = geometry.CaretRect(focus);
        static float Round(float v) => MathF.Floor(v + 0.5f);
        var color = box.Style.CaretColor ?? box.Style.Color;
        _canvas.PushClip(geometry.Clip);
        _canvas.FillRect(RectF.FromEdges(Round(caret.X), Round(caret.Y), Round(caret.Right), Round(caret.Bottom)), color);
        _canvas.PopClip();
    }

    /// <summary>A menu list's text: the selected option's, after the 4 px inner start padding, before the arrow box.</summary>
    void PaintSelect(LayoutBox box, UiElement element, RectF content)
    {
        var style = box.Style;
        if (element.SelectedOption is not { } selected || !_canvas.Drawing)
            return;
        var text = selected.OptionLabel;
        if (text.Length == 0)
            return;
        var themed = ControlAppearance.Effective(element, style) != ControlLook.None;
        var arrow = themed ? ReplacedContent.ArrowWidth(style, _document.DeviceScale) - (int)(4 * style.Zoom) : 0;
        var start = themed ? (int)(4 * style.Zoom) : 0;
        var layout = TextLayout.Create(_document.Fonts, text, InlineLayout.TextStyleFor(style), new ParagraphStyle { WhiteSpace = Text.WhiteSpace.Pre });
        var textArea = new RectF(content.X + start, content.Y, MathF.Max(0, content.Width - arrow - start), content.Height);
        // The text sits where layout put the inner block; the clip is on whole pixels.
        var exact = ExactContent(box, _shift);
        _canvas.PushClip(textArea);
        _canvas.DrawText(layout, new Vector2(exact.X + start, SnapBaseline(exact.Y + (exact.Height - layout.Height) / 2, layout)), style.Color);
        _canvas.PopClip();
    }

    /// <summary>
    /// A progress element without its native look: its parts as boxes styled by
    /// ::-webkit-progress-bar (the content box) and ::-webkit-progress-value (the value's share of
    /// the bar's content box; a width transition on it animates).
    /// </summary>
    void PaintProgressParts(UiElement element, RectF content)
    {
        if (element.Styles is not { } styles)
            return;
        var inner = styles.Part(PseudoElement.ProgressInnerElement);
        var bar = styles.Part(PseudoElement.ProgressBar);
        var value = styles.Part(PseudoElement.ProgressValue);
        if (inner.Display == Display.None || bar.Display == Display.None)
            return;
        var barRect = PartRect(bar, content);
        PaintPartBox(bar, barRect);
        if (value.Display == Display.None)
            return;
        var barContent = new RectF(barRect.X + bar.BorderLeft + bar.PaddingLeft.ResolveOr(content.Width, 0), barRect.Y + bar.BorderTop + bar.PaddingTop.ResolveOr(content.Width, 0),
            MathF.Max(0, barRect.Width - bar.BorderLeft - bar.BorderRight - bar.PaddingLeft.ResolveOr(content.Width, 0) - bar.PaddingRight.ResolveOr(content.Width, 0)),
            MathF.Max(0, barRect.Height - bar.BorderTop - bar.BorderBottom - bar.PaddingTop.ResolveOr(content.Width, 0) - bar.PaddingBottom.ResolveOr(content.Width, 0)));
        var position = _document.DisplayedProgress(element, value);
        if (position < 0)
            return;
        var valueHeight = value.Height.IsAuto ? barContent.Height : value.Height.ResolveOr(barContent.Height, barContent.Height);
        var valueRect = new RectF(barContent.X, barContent.Y, barContent.Width * position, valueHeight);
        PaintPartBox(value, valueRect);
    }

    /// <summary>A part's border box in its container's content box (width and height 100% by default, as the UA styles them).</summary>
    static RectF PartRect(ComputedStyle part, RectF container)
    {
        var width = part.Width.IsAuto ? container.Width : part.Width.ResolveOr(container.Width, container.Width);
        var height = part.Height.IsAuto ? container.Height : part.Height.ResolveOr(container.Height, container.Height);
        if (part.BoxSizing != BoxSizing.BorderBox)
        {
            width += part.BorderLeft + part.BorderRight + part.PaddingLeft.ResolveOr(container.Width, 0) + part.PaddingRight.ResolveOr(container.Width, 0);
            height += part.BorderTop + part.BorderBottom + part.PaddingTop.ResolveOr(container.Width, 0) + part.PaddingBottom.ResolveOr(container.Width, 0);
        }
        return new RectF(container.X, container.Y, width, height);
    }

    /// <summary>A part's background, border and shadows (as a box with the part's style).</summary>
    void PaintPartBox(ComputedStyle part, RectF rect)
    {
        if (part.Visibility != Visibility.Visible || rect.Width <= 0 && rect.Height <= 0)
            return;
        var radii = ResolveRadii(part.RadiusTopLeft, part.RadiusTopRight, part.RadiusBottomRight, part.RadiusBottomLeft, rect.Size);
        var layered = part.Opacity < 1;
        if (layered)
            _canvas.PushLayer(new LayerOptions { Opacity = part.Opacity });
        PaintStyledBox(part, rect, radii, SidesOf(part));
        if (layered)
            _canvas.PopLayer();
    }

    void PaintMeter(LayoutBox box, UiElement element, RectF content)
    {
        // Chromium paints meters with CSS (its theme has no meter); the engine shows the value share in green.
        if (!_canvas.Drawing)
            return;
        float Number(string name, float fallback) => UiElement.ParseNumber(element.GetAttribute(name)) is { } v ? (float)v : fallback;
        var min = Number("min", 0);
        var max = MathF.Max(min, Number("max", 1));
        var value = Math.Clamp(Number("value", 0), min, max);
        var share = max > min ? (value - min) / (max - min) : 0;
        _canvas.FillRect(new RectF(content.X, content.Y, content.Width * share, content.Height), Color.FromHex("#107c10"));
    }
}

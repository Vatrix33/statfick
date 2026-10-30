using System.Numerics;
using Statventure.Text;
using Statventure.UI.Style;

namespace Statventure.UI.Layout;

/// <summary>
/// The natural sizes and baselines of replaced elements: images, canvases and engine surfaces, and
/// form controls sized as Chromium sizes them (text fields from the font's average character width,
/// checkboxes, sliders, selects, text areas).
/// </summary>
public static class ReplacedContent
{
    public static string InputType(LayoutBox box) => box.Element?.InputType ?? "";

    /// <summary>The label of a submit, reset or button input: its value, else Chromium's default ("Submit", "Reset").</summary>
    public static string InputButtonLabel(Dom.UiElement element) =>
        element.GetAttribute("value") ?? element.InputType switch { "submit" => "Submit", "reset" => "Reset", _ => "" };

    static bool IsInputButton(LayoutBox box) => box.Element?.Tag == "input" && InputType(box) is "submit" or "reset" or "button";

    static bool IsTextField(LayoutBox box) =>
        box.Element?.Tag == "input" && InputType(box) is "text" or "number" or "search" or "email" or "password" or "url" or "tel";

    /// <summary>The content-box size the element has by itself (null when it has none).</summary>
    public static Vector2? NaturalSize(LayoutBox box, LayoutContext context)
    {
        var element = box.Element;
        if (element is null)
            return null;
        var zoom = box.Style.Zoom;
        switch (element.Tag)
        {
            // Sizes in CSS px are zoomed (Blink's ThemePartIntrinsicSize, LayoutImage, LayoutReplaced's default 300×150).
            case "img":
                return element.GetAttribute("src") is { } src && context.ImageSize?.Invoke(src) is { } size ? size * zoom : Vector2.Zero;
            case "svg":
                return SvgSize(element) * zoom;
            case "canvas":
            case "surface":
            case "video":
            case "iframe":
                return Units(new Vector2(Attribute(element, "width", 300), Attribute(element, "height", 150)) * zoom);
            case "progress":
                return Units(new Vector2(160, 16) * zoom);
            case "meter":
                return Units(new Vector2(80, 16) * zoom);
            case "select":
                return SelectSize(box, context);
            case "textarea":
            {
                var (average, lineHeight) = CharacterMetrics(box, context);
                var cols = Attribute(element, "cols", 20);
                var rows = Attribute(element, "rows", 2);
                return new Vector2(MathF.Ceiling(average * cols) + context.ScrollbarThickness(box.Style), lineHeight * rows);
            }
            case "input":
                switch (InputType(box))
                {
                    case "checkbox":
                    case "radio":
                        return Units(new Vector2(13, 13) * zoom);
                    case "range":
                        return Units(new Vector2(129, 16) * zoom);
                    case "color":
                        return Units(new Vector2(50, 27) * zoom);
                    case "file":
                    {
                        var (average, lineHeight) = CharacterMetrics(box, context);
                        return new Vector2(MathF.Ceiling(average * 26), lineHeight);
                    }
                    case "submit":
                    case "reset":
                    case "button":
                    {
                        // As a button with its label: the text's width, a line of the font.
                        var (_, lineHeight) = CharacterMetrics(box, context);
                        var label = InputButtonLabel(element);
                        var style = box.Style;
                        var width = label.Length == 0 ? 0 : TextLayout.Create(context.Fonts, label, InlineLayout.TextStyleFor(style), new ParagraphStyle { WhiteSpace = WhiteSpace.Pre }).Width;
                        return new Vector2(width, lineHeight);
                    }
                    default:
                        return TextFieldSize(box, context);
                }
        }
        return null;
    }

    /// <summary>
    /// An inline svg's natural size: its width and height attributes when they are absolute; null
    /// otherwise (it then fills its container and takes its height from the viewBox ratio).
    /// </summary>
    static Vector2? SvgSize(Dom.UiElement element)
    {
        static float? Absolute(string? value)
        {
            if (value is null)
                return null;
            var text = value.Trim();
            if (text.EndsWith("px"))
                text = text[..^2];
            return float.TryParse(text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var v) && v >= 0 ? v : null;
        }
        var width = Absolute(element.GetAttribute("width"));
        var height = Absolute(element.GetAttribute("height"));
        var ratio = SvgRatio(element);
        if (width is { } w && height is { } h)
            return new Vector2(w, h);
        if (width is { } w2)
            return ratio > 0 ? new Vector2(w2, w2 / ratio) : new Vector2(w2, 150);
        if (height is { } h2)
            return ratio > 0 ? new Vector2(h2 * ratio, h2) : new Vector2(300, h2);
        return ratio > 0 ? null : new Vector2(300, 150);
    }

    /// <summary>The viewBox's aspect ratio (0 without one).</summary>
    public static float SvgRatio(Dom.UiElement element)
    {
        var viewBox = element.GetAttribute("viewbox");
        if (viewBox is null)
            return 0;
        var parts = viewBox.Split((char[])[' ', ',', '\t', '\n'], StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 4 || !float.TryParse(parts[2], System.Globalization.CultureInfo.InvariantCulture, out var w) || !float.TryParse(parts[3], System.Globalization.CultureInfo.InvariantCulture, out var h) || w <= 0 || h <= 0)
            return 0;
        return w / h;
    }

    static float Attribute(Dom.UiElement element, string name, float fallback) =>
        float.TryParse(element.GetAttribute(name), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var value) && value >= 0 ? value : fallback;

    /// <summary>The primary font's average character width and the line height, for the box's font.</summary>
    static (float Average, float LineHeight) CharacterMetrics(LayoutBox box, LayoutContext context) => CharacterMetrics(box.Style, context.Fonts);

    /// <summary>
    /// The size of a textarea's text wrapped to a width (its inner editor's lines): at least one line
    /// (the placeholder break Blink keeps in an empty one).
    /// </summary>
    internal static Vector2 TextAreaTextSize(LayoutBox box, LayoutContext context, float width)
    {
        var style = box.Style;
        var lineHeight = FieldLineHeight(style, context.Fonts);
        if (box.Element is not { } element || element.EditorText.Length == 0)
            return new Vector2(0, lineHeight);
        var layout = TextLayout.Create(context.Fonts, element.EditorText, InlineLayout.TextStyleFor(style),
            new ParagraphStyle { WhiteSpace = WhiteSpace.PreWrap, MaxWidth = width });
        return new Vector2(layout.Width, MathF.Max(layout.Height, lineHeight));
    }

    /// <summary>A text control's line: the height of its inner editor (and of the spin button beside it).</summary>
    internal static float FieldLineHeight(ComputedStyle style, FontLibrary fonts) => CharacterMetrics(style, fonts).LineHeight;

    static (float Average, float LineHeight) CharacterMetrics(ComputedStyle style, FontLibrary fonts)
    {
        var font = fonts.Resolve(style.FontFamilies, style.FontWeight, style.FontItalic) ?? fonts.ResolveDefault(style.FontWeight, style.FontItalic);
        if (font is not { } resolved)
            return (style.FontSize / 2, style.FontSize * 1.2f);
        var face = resolved.Face;
        var scale = style.FontSize / face.UnitsPerEm;
        var average = face.AverageCharWidth > 0 ? face.AverageCharWidth * scale : face.GlyphFor('0') is var zero && zero != 0 ? face.HbFont.GetHorizontalGlyphAdvance(zero) * scale : style.FontSize / 2;
        var (ascent, descent, gap) = TextLayout.Metrics(face, style.FontSize, style.SubpixelPositioning);
        var lineHeight = style.LineHeight.Kind switch
        {
            LineHeightKind.Number => style.LineHeight.Value * style.FontSize,
            LineHeightKind.Length => style.LineHeight.Value,
            _ => ascent + descent + gap,
        };
        return (average, lineHeight);
    }

    /// <summary>
    /// A text field: size (20) average characters plus the font's widest character beyond one
    /// average (Chromium's LayoutTextControlSingleLine). The average is the OS/2 one, rounded when
    /// it rounds up; the widest is the rounded bounding box width; the sum is rounded (fitted to
    /// what the game's Chromium measures for Lato and STIX Two at 10–30px).
    /// </summary>
    static Vector2 TextFieldSize(LayoutBox box, LayoutContext context)
    {
        var element = box.Element!;
        var (average, lineHeight) = CharacterMetrics(box, context);
        var size = Attribute(element, "size", 20);
        if (size < 1)
            size = 20;
        var style = box.Style;
        var font = context.Fonts.Resolve(style.FontFamilies, style.FontWeight, style.FontItalic) ?? context.Fonts.ResolveDefault(style.FontWeight, style.FontItalic);
        float width;
        if (font is { } f && f.Face.AverageCharWidth > 0)
        {
            var rounded = MathF.Round(average, MidpointRounding.AwayFromZero);
            var c = rounded >= average ? rounded : average;
            var widest = MathF.Round(f.Face.BoundsWidth * style.FontSize / f.Face.UnitsPerEm, MidpointRounding.AwayFromZero);
            width = MathF.Round(c * size + widest - c, MidpointRounding.AwayFromZero);
        }
        else
            width = MathF.Ceiling(average * size);
        return new Vector2(width, lineHeight);
    }

    static Vector2 SelectSize(LayoutBox box, LayoutContext context)
    {
        var element = box.Element!;
        var style = box.Style;
        // The menu list's text box ignores line-height: its height is the font's own line spacing.
        var font = context.Fonts.Resolve(style.FontFamilies, style.FontWeight, style.FontItalic) ?? context.Fonts.ResolveDefault(style.FontWeight, style.FontItalic);
        var lineHeight = style.FontSize * 1.2f;
        if (font is { } f)
        {
            var (ascent, descent, gap) = TextLayout.Metrics(f.Face, style.FontSize, style.SubpixelPositioning);
            lineHeight = ascent + descent + gap;
        }
        var widest = 0f;
        // Chromium measures option text word by word (its word shaper), with the select's own font.
        foreach (var option in element.Descendants().Where(e => e.Tag == "option"))
            widest = MathF.Max(widest, MeasureWords(option.TextContent.Trim(), style, context));
        // The text, the inner paddings (4 px at the start, 1 px above and below, zoomed and truncated) and the drop-down arrow.
        return new Vector2(MathF.Ceiling(widest) + ArrowWidth(style, context.DeviceScale), lineHeight + 2 * InternalPadding(style, 1));
    }

    /// <summary>LayoutThemeDefault::MenuListInternalPadding: a padding times the zoom, truncated (none without appearance).</summary>
    static int InternalPadding(ComputedStyle style, int padding) => style.AppearanceNone ? 0 : (int)(padding * style.Zoom);

    /// <summary>
    /// The menu list's start and end paddings (LayoutThemeDefault::PopupInternalPaddingStart/End):
    /// 4 px at the start; at the end 1 px and the arrow box, 15 px (the scrollbar's arrow) at the
    /// device scale truncated, or zoomed when the zoom is larger (ClampedMenuListArrowPaddingSize).
    /// </summary>
    public static float ArrowWidth(ComputedStyle style, float deviceScale)
    {
        if (style.AppearanceNone)
            return 0;
        return InternalPadding(style, 4) + (int)(1 * style.Zoom + ArrowBoxWidth(style, deviceScale));
    }

    /// <summary>ClampedMenuListArrowPaddingSize: the arrow box's width.</summary>
    public static float ArrowBoxWidth(ComputedStyle style, float deviceScale)
    {
        const int original = 15;
        var scaled = (int)(original * deviceScale);
        var scaleOfDevice = scaled / (float)original;
        return style.Zoom < scaleOfDevice ? scaled : original * style.Zoom;
    }

    /// <summary>Text width with each word and each space shaped on its own (no kerning across spaces).</summary>
    public static float MeasureWords(string text, ComputedStyle style, LayoutContext context)
    {
        var width = 0f;
        var start = 0;
        while (start < text.Length)
        {
            var end = start + 1;
            if (text[start] != ' ')
                while (end < text.Length && text[end] != ' ')
                    end++;
            width += MeasureText(text[start..end], style, context);
            start = end;
        }
        return width;
    }

    public static float MeasureText(string text, ComputedStyle style, LayoutContext context)
    {
        if (text.Length == 0)
            return 0;
        var layout = TextLayout.Create(context.Fonts, text, InlineLayout.TextStyleFor(style), new ParagraphStyle { WhiteSpace = Text.WhiteSpace.Pre });
        // The runs' advance, spaces included.
        return layout.Lines.Count == 0 ? 0 : layout.Lines.Max(l => l.Runs.Sum(r => r.Width));
    }

    /// <summary>A zoomed size as LayoutUnits (PhysicalSize::Scale truncates to 1/64 px).</summary>
    static Vector2 Units(Vector2 size) => new(LayoutUnit.Truncate(size.X), LayoutUnit.Truncate(size.Y));

    /// <summary>Images, canvases and surfaces keep their natural aspect ratio; form controls don't have one.</summary>
    public static bool HasNaturalRatio(LayoutBox box) => box.Element?.Tag is "img" or "canvas" or "surface" or "video" or "iframe" or "svg";

    /// <summary>The natural aspect ratio, also for replaced content that has a ratio but no natural size (an svg with only a viewBox).</summary>
    public static float NaturalRatio(LayoutBox box, Vector2? natural) =>
        natural is { X: > 0, Y: > 0 } n ? n.X / n.Y : box.Element?.Tag == "svg" ? SvgRatio(box.Element) : 0;

    /// <summary>Elements that fill their line like blocks when their width is auto.</summary>
    public static bool FillsWidth(LayoutBox box) => box.Element?.Tag == "svg" && box.NaturalSize is null;

    /// <summary>The baseline from the border box's top (null: the bottom margin edge).</summary>
    public static float? Baseline(LayoutBox box, LayoutContext context)
    {
        var element = box.Element;
        if (element is null)
            return null;
        // Checkboxes, radio buttons and sliders sit on their border box's bottom edge.
        if (element.Tag == "input" && InputType(box) is "checkbox" or "radio" or "range")
            return box.Height;
        var textLike = IsTextField(box) || IsInputButton(box) || element.Tag is "select" or "textarea" || (element.Tag == "input" && InputType(box) == "file");
        if (!textLike)
            return null;
        var style = box.Style;
        var font = context.Fonts.Resolve(style.FontFamilies, style.FontWeight, style.FontItalic) ?? context.Fonts.ResolveDefault(style.FontWeight, style.FontItalic);
        if (font is not { } f)
            return null;
        var (ascent, descent, gap) = TextLayout.Metrics(f.Face, style.FontSize, style.SubpixelPositioning);
        var (_, lineHeight) = CharacterMetrics(box, context);
        var above = ascent + MathF.Floor((lineHeight - ascent - descent) / 2);
        // The text sits centred in the content box (single-line fields) or at its top (text areas).
        var contentHeight = box.ContentHeight;
        var offset = element.Tag == "textarea" ? 0 : MathF.Max(0, (contentHeight - lineHeight) / 2);
        return box.ContentTop + offset + above;
    }
}

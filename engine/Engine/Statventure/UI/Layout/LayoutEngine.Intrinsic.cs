using Statventure.UI.Style;

namespace Statventure.UI.Layout;

public sealed partial class LayoutEngine
{
    // A fixed length as MinimumValueForLength gives it: truncated to a LayoutUnit (percentages count as zero).
    static float FixedPixels(Length length) => length.IsValue && !length.HasPercent ? LayoutUnit.Truncate(length.Pixels) : 0;

    /// <summary>Border and padding that don't depend on the containing block (percentages count as zero).</summary>
    static float FixedHorizontalEdges(LayoutBox box) =>
        box.Style.BorderLeft + box.Style.BorderRight + MathF.Max(0, FixedPixels(box.Style.PaddingLeft)) + MathF.Max(0, FixedPixels(box.Style.PaddingRight));

    /// <summary>
    /// A box's min-content or max-content width (its border box; CSS Sizing 3 §5): a fixed width
    /// wins, otherwise the content decides, clamped by fixed min/max widths.
    /// </summary>
    internal float IntrinsicWidth(LayoutBox box, bool min) => IntrinsicWidth(box, min, ignoreSpecified: false);

    /// <param name="ignoreSpecified">Size the content only, as if width were auto (a flex item's content size suggestion).</param>
    internal float IntrinsicWidth(LayoutBox box, bool min, bool ignoreSpecified)
    {
        if (ignoreSpecified && min && box.CachedContentMin is { } contentCached)
            return contentCached;
        if (!ignoreSpecified && (min ? box.CachedMinContent : box.CachedMaxContent) is { } cached)
            return cached;

        var s = box.Style;
        var edges = FixedHorizontalEdges(box);
        var scrollbar = s.OverflowY == Overflow.Scroll || (s.ScrollbarGutterStable && s.OverflowY is Overflow.Auto or Overflow.Hidden) ? Context.ScrollbarThickness(s) : 0;
        edges += scrollbar;
        float result;
        if (ignoreSpecified)
        {
            var content = box.Type switch
            {
                BoxType.Flex => _flex.IntrinsicWidth(box, min),
                BoxType.Grid => _grid.IntrinsicWidth(box, min),
                BoxType.Replaced => ReplacedIntrinsicWidth(box),
                _ => BlockIntrinsicWidth(box, min),
            };
            result = content + edges;
            if (s.MaxWidth.IsValue && !s.MaxWidth.HasPercent)
                result = MathF.Min(result, s.BoxSizing == BoxSizing.BorderBox ? s.MaxWidth.Pixels : s.MaxWidth.Pixels + edges);
            result = MathF.Max(result, edges);
            if (min)
                box.CachedContentMin = result;
            return result;
        }
        if (s.Width.IsValue && !s.Width.HasPercent)
            result = s.BoxSizing == BoxSizing.BorderBox ? MathF.Max(s.Width.Pixels, edges) : s.Width.Pixels + edges;
        else if (s.Width.Kind is LengthKind.Min or LengthKind.Max or LengthKind.Clamp && !s.Width.HasPercent)
        {
            var value = s.Width.Resolve(0);
            result = s.BoxSizing == BoxSizing.BorderBox ? MathF.Max(value, edges) : value + edges;
        }
        else if (s.AspectRatio > 0 && s.Height.IsValue && !s.Height.HasPercent)
        {
            var height = s.BoxSizing == BoxSizing.BorderBox ? s.Height.Pixels : s.Height.Pixels + s.BorderTop + s.BorderBottom + FixedPixels(s.PaddingTop) + FixedPixels(s.PaddingBottom);
            var probe = new LayoutBox(box.Type, s);
            probe.Border = new Edges(s.BorderTop, s.BorderRight, s.BorderBottom, s.BorderLeft);
            probe.Padding = new Edges(FixedPixels(s.PaddingTop), FixedPixels(s.PaddingRight), FixedPixels(s.PaddingBottom), FixedPixels(s.PaddingLeft));
            result = RatioWidth(probe, height);
        }
        else
        {
            var content = box.Type switch
            {
                BoxType.Flex => _flex.IntrinsicWidth(box, min),
                BoxType.Grid => _grid.IntrinsicWidth(box, min),
                BoxType.Replaced => ReplacedIntrinsicWidth(box),
                _ => BlockIntrinsicWidth(box, min),
            };
            // A box that clips or scrolls its content can still be squeezed to nothing... but only
            // from its minimum size, which intrinsic sizing doesn't use; the content counts.
            result = content + edges;
        }

        // Fixed min-width / max-width.
        if (s.MaxWidth.IsValue && !s.MaxWidth.HasPercent)
            result = MathF.Min(result, s.BoxSizing == BoxSizing.BorderBox ? s.MaxWidth.Pixels : s.MaxWidth.Pixels + edges);
        if (s.MinWidth.IsValue && !s.MinWidth.HasPercent)
            result = MathF.Max(result, s.BoxSizing == BoxSizing.BorderBox ? s.MinWidth.Pixels : s.MinWidth.Pixels + edges);
        result = MathF.Max(result, edges);

        if (min)
            box.CachedMinContent = result;
        else
            box.CachedMaxContent = result;
        return result;
    }

    /// <summary>A box's outer min-content or max-content contribution: its intrinsic width plus its fixed margins.</summary>
    internal float Contribution(LayoutBox box, bool min)
    {
        var s = box.Style;
        var margins = FixedPixels(s.MarginLeft) + FixedPixels(s.MarginRight);
        // Replaced elements with percentage sizes are compressible: they contribute nothing to min-content.
        if (min && box.Type == BoxType.Replaced && (s.Width.HasPercent || s.MaxWidth.HasPercent))
            return margins;
        return IntrinsicWidth(box, min) + margins;
    }

    float BlockIntrinsicWidth(LayoutBox box, bool min)
    {
        if (box.Children.Any(c => c.IsInlineLevel))
            return _inline.IntrinsicWidth(box, min);
        var width = 0f;
        foreach (var child in box.Children)
        {
            if (child.IsOutOfFlow)
                continue;
            width = MathF.Max(width, Contribution(child, min));
        }
        return width;
    }

    float ReplacedIntrinsicWidth(LayoutBox box)
    {
        var natural = ReplacedContent.NaturalSize(box, Context);
        var s = box.Style;
        if (s.Height.IsValue && !s.Height.HasPercent && natural is { X: > 0, Y: > 0 } n)
        {
            var height = s.BoxSizing == BoxSizing.BorderBox ? s.Height.Pixels - s.BorderTop - s.BorderBottom - FixedPixels(s.PaddingTop) - FixedPixels(s.PaddingBottom) : s.Height.Pixels;
            var ratio = s.AspectRatio > 0 ? s.AspectRatio : n.X / n.Y;
            return MathF.Max(0, height) * ratio;
        }
        return natural?.X ?? 0;
    }
}

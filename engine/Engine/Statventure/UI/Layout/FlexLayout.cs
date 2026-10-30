using System.Numerics;
using Statventure.UI.Style;

namespace Statventure.UI.Layout;

/// <summary>
/// Flex layout (CSS Flexbox 1 §9): flex base sizes and automatic minimum sizes, flex lines,
/// resolving flexible lengths with freezing, cross sizes with stretch and baseline alignment, auto
/// margins, justify-content, align-items/align-self and align-content, gaps, order and the reverse
/// directions, and the container's intrinsic widths.
/// </summary>
internal sealed class FlexLayout
{
    readonly LayoutEngine _engine;

    public FlexLayout(LayoutEngine engine)
    {
        _engine = engine;
    }

    sealed class Item
    {
        public required LayoutBox Box;
        public float Base, BaseContent, Hypothetical;
        public float MinMain, MaxMain;
        public float Target, Violation;
        public bool Frozen;
        public float MainMargins;
        public float Grow, Shrink;
        public float Main, Cross;
        public float CrossMargins;
        public float Ascent;
        public Alignment Align;
        public float MainOffset, CrossOffset;
        public bool Stretched;
    }

    sealed class Line
    {
        public readonly List<Item> Items = [];
        public float Cross, Offset, MaxAscent, MaxDescent;
    }

    static bool IsRow(ComputedStyle s) => s.FlexDirection is FlexDirection.Row or FlexDirection.RowReverse;
    static bool IsReverse(ComputedStyle s) => s.FlexDirection is FlexDirection.RowReverse or FlexDirection.ColumnReverse;

    static float Gap(Length gap, float basis) => gap.IsNone ? 0 : MathF.Max(0, LayoutUnit.Truncate(gap.ResolveOr(basis, 0)));

    public void Layout(LayoutBox box, in Space space) => _engine.LayoutContainer(box, space, LayoutContents);

    /// <summary>Lays out the flex items; returns the content height.</summary>
    float LayoutContents(LayoutBox box, Space space, float definiteContentHeight)
    {
        var s = box.Style;
        var row = IsRow(s);
        var contentWidth = box.ContentWidth;
        var contentHeight = definiteContentHeight; // NaN when indefinite
        var mainGap = Gap(row ? s.ColumnGap : s.RowGap, row ? contentWidth : contentHeight);
        var crossGap = Gap(row ? s.RowGap : s.ColumnGap, row ? contentHeight : contentWidth);
        box.Inline = null;
        box.FirstBaseline = box.LastBaseline = null;

        // Items in order-modified document order.
        var items = new List<Item>();
        foreach (var child in box.Children)
        {
            if (child.IsOutOfFlow)
            {
                // As if it were the only flex item (Flexbox §4.1): aligned by justify-content and align-self.
                child.StaticContainer = box;
                child.StaticPosition = new Vector2(box.ContentLeft, box.ContentTop);
                child.StaticArea = new Core.RectF(box.ContentLeft, box.ContentTop, box.ContentWidth, float.IsNaN(definiteContentHeight) ? 0 : definiteContentHeight);
                var main = s.JustifyContent switch
                {
                    Alignment.Center or Alignment.SpaceAround or Alignment.SpaceEvenly => 0.5f,
                    Alignment.FlexEnd => 1f,
                    Alignment.End or Alignment.Right => IsReverse(s) ? 0f : 1f,
                    Alignment.Start or Alignment.Left => IsReverse(s) ? 1f : 0f,
                    _ => 0f,
                };
                if (IsReverse(s) && s.JustifyContent is Alignment.Normal or Alignment.FlexStart or Alignment.SpaceBetween or Alignment.Stretch)
                    main = 1f;
                if (IsReverse(s) && s.JustifyContent == Alignment.FlexEnd)
                    main = 0f;
                var crossAlign = child.Style.AlignSelf is Alignment.Auto ? s.AlignItems : child.Style.AlignSelf;
                var cross = crossAlign switch
                {
                    Alignment.Center => 0.5f,
                    Alignment.FlexEnd or Alignment.End or Alignment.SelfEnd => s.FlexWrap == FlexWrap.WrapReverse && crossAlign == Alignment.FlexEnd ? 0f : 1f,
                    _ => s.FlexWrap == FlexWrap.WrapReverse ? 1f : 0f,
                };
                child.StaticAlign = row ? new Vector2(main, cross) : new Vector2(cross, main);
                continue;
            }
            child.Container = box;
            items.Add(new Item { Box = child });
        }
        items = items.OrderBy(i => i.Box.Style.Order).ToList();

        var availableMain = row ? contentWidth : contentHeight;
        var minHeight = s.MinHeight.IsAuto ? float.NaN : _engine.SpecifiedHeight(box, s.MinHeight, space.PercentHeight);
        var maxHeight = s.MaxHeight.IsNone ? float.NaN : _engine.SpecifiedHeight(box, s.MaxHeight, space.PercentHeight);
        var edgesV = box.Border.Vertical + box.Padding.Vertical + box.HorizontalScrollbar;

        // Flex base sizes and hypothetical main sizes (§9.2).
        foreach (var item in items)
        {
            var child = item.Box;
            var cs = child.Style;
            LayoutEngine.ResolveEdges(child, contentWidth);
            item.Grow = cs.FlexGrow;
            item.Shrink = cs.FlexShrink;
            item.Align = cs.AlignSelf is Alignment.Auto ? s.AlignItems : cs.AlignSelf;
            if (item.Align is Alignment.Normal)
                item.Align = Alignment.Stretch;
            item.MainMargins = row ? child.Margin.Horizontal : child.Margin.Vertical;
            item.CrossMargins = row ? child.Margin.Vertical : child.Margin.Horizontal;
            var edges = row ? child.Border.Horizontal + child.Padding.Horizontal : child.Border.Vertical + child.Padding.Vertical;

            var basis = cs.FlexBasis;
            var size = float.NaN;
            if (basis.IsDefinite && (!basis.HasPercent || !float.IsNaN(availableMain)))
                size = row ? _engine.SpecifiedWidth(child, basis, availableMain, contentWidth) : _engine.SpecifiedHeight(child, basis, availableMain);
            else if (basis.IsAuto)
            {
                var mainSize = row ? cs.Width : cs.Height;
                size = row ? _engine.SpecifiedWidth(child, mainSize, contentWidth, contentWidth) : _engine.SpecifiedHeight(child, mainSize, contentHeight);
                if (row && mainSize.Kind is LengthKind.MinContent or LengthKind.MaxContent or LengthKind.FitContent)
                    size = _engine.SpecifiedWidth(child, mainSize, contentWidth, contentWidth);
            }
            if (float.IsNaN(size) && cs.AspectRatio > 0)
            {
                // A definite cross size and an aspect ratio give the main size.
                var crossSize = row ? _engine.SpecifiedHeight(child, cs.Height, contentHeight) : _engine.SpecifiedWidth(child, cs.Width, contentWidth, contentWidth);
                if (!float.IsNaN(crossSize))
                    size = row ? LayoutEngine.RatioWidth(child, crossSize) : LayoutEngine.RatioHeight(child, crossSize);
            }
            if (float.IsNaN(size))
                size = row ? _engine.IntrinsicWidth(child, min: false) : ContentHeight(child, box, item, contentWidth, contentHeight);
            item.Base = MathF.Max(size, edges);
            item.BaseContent = MathF.Max(0, item.Base - edges);

            // Min and max main sizes, with the automatic minimum (§4.5).
            var minMain = row
                ? (cs.MinWidth.IsAuto ? float.NaN : _engine.SpecifiedWidth(child, cs.MinWidth, contentWidth, contentWidth))
                : (cs.MinHeight.IsAuto ? float.NaN : _engine.SpecifiedHeight(child, cs.MinHeight, contentHeight));
            var maxMain = row
                ? (cs.MaxWidth.IsNone ? float.NaN : _engine.SpecifiedWidth(child, cs.MaxWidth, contentWidth, contentWidth))
                : (cs.MaxHeight.IsNone ? float.NaN : _engine.SpecifiedHeight(child, cs.MaxHeight, contentHeight));
            if (float.IsNaN(minMain))
            {
                // (FlexLayoutAlgorithm::ShouldApplyAutoMinSize looks at the main axis's computed overflow, not at scrolling.)
                if ((row ? cs.MinWidth.IsAuto : cs.MinHeight.IsAuto) && !(row ? cs.IsOverflowValueScrollableX : cs.IsOverflowValueScrollableY))
                {
                    var contentSuggestion = row ? _engine.IntrinsicWidth(child, min: true, ignoreSpecified: true) : ContentHeight(child, box, item, contentWidth, contentHeight);
                    if (!float.IsNaN(maxMain))
                        contentSuggestion = MathF.Min(contentSuggestion, maxMain);
                    var specified = row ? _engine.SpecifiedWidth(child, cs.Width, contentWidth, contentWidth) : _engine.SpecifiedHeight(child, cs.Height, contentHeight);
                    if (!float.IsNaN(specified))
                        contentSuggestion = MathF.Min(contentSuggestion, specified);
                    minMain = contentSuggestion;
                }
                else
                    minMain = 0;
            }
            item.MinMain = MathF.Max(minMain, edges);
            item.MaxMain = float.IsNaN(maxMain) ? float.PositiveInfinity : MathF.Max(maxMain, item.MinMain);
            item.Hypothetical = Math.Clamp(item.Base, item.MinMain, item.MaxMain);
        }

        // An indefinite column container gets its main size from its items, clamped (as Chromium does).
        if (!row && float.IsNaN(availableMain))
        {
            var sum = items.Sum(i => i.Hypothetical + i.MainMargins) + mainGap * Math.Max(0, items.Count - 1);
            if (s.FlexWrap != FlexWrap.NoWrap)
                sum = items.Count == 0 ? 0 : items.Max(i => i.Hypothetical + i.MainMargins);
            var border = sum + edgesV;
            if (!float.IsNaN(maxHeight))
                border = MathF.Min(border, maxHeight);
            if (!float.IsNaN(minHeight))
                border = MathF.Max(border, minHeight);
            availableMain = MathF.Max(0, border - edgesV);
            if (s.FlexWrap != FlexWrap.NoWrap && float.IsNaN(maxHeight))
                availableMain = float.PositiveInfinity;
        }

        // Flex lines (§9.3).
        var lines = new List<Line>();
        var current = new Line();
        lines.Add(current);
        var used = 0f;
        foreach (var item in items)
        {
            var outer = item.Hypothetical + item.MainMargins;
            if (s.FlexWrap != FlexWrap.NoWrap && current.Items.Count > 0 && used + mainGap + outer > availableMain + 0.01f)
            {
                current = new Line();
                lines.Add(current);
                used = 0;
            }
            used += (current.Items.Count > 0 ? mainGap : 0) + outer;
            current.Items.Add(item);
        }

        // Resolving flexible lengths (§9.7).
        var mainForFlex = float.IsInfinity(availableMain) ? float.NaN : availableMain;
        foreach (var line in lines)
            ResolveFlexibleLengths(line, mainForFlex, mainGap);

        // Hypothetical cross sizes (§9.4). Items of rows that stretch are laid out again at their
        // stretched size: this first layout only measures them.
        foreach (var line in lines)
            foreach (var item in line.Items)
                MeasureCross(item, box, row, contentWidth, contentHeight, fixedCross: float.NaN, final: !(row && Stretches(item, row)));

        // Line cross sizes.
        var singleLine = s.FlexWrap == FlexWrap.NoWrap;
        var definiteCross = row ? contentHeight : contentWidth;
        foreach (var line in lines)
        {
            float maxAscent = 0, maxDescent = 0, largest = 0;
            foreach (var item in line.Items)
            {
                var outer = item.Cross + item.CrossMargins;
                if (item.Align is Alignment.Baseline or Alignment.LastBaseline && row && !AutoCrossMargins(item, row))
                {
                    var ascent = item.Ascent + (row ? item.Box.Margin.Top : item.Box.Margin.Left);
                    maxAscent = MathF.Max(maxAscent, ascent);
                    maxDescent = MathF.Max(maxDescent, outer - ascent);
                }
                else
                    largest = MathF.Max(largest, outer);
            }
            line.MaxAscent = maxAscent;
            line.MaxDescent = maxDescent;
            line.Cross = MathF.Max(largest, maxAscent + maxDescent);
        }
        if (singleLine && !float.IsNaN(definiteCross) && lines.Count == 1)
            lines[0].Cross = definiteCross;
        else if (singleLine && lines.Count == 1 && !row)
            lines[0].Cross = contentWidth;

        // The container's cross size (for rows with an auto height).
        float containerCross;
        var linesCross = lines.Sum(l => l.Cross) + crossGap * Math.Max(0, lines.Count - 1);
        if (!float.IsNaN(definiteCross))
            containerCross = definiteCross;
        else
        {
            // Rows: the content height, clamped by min/max height.
            var border = linesCross + edgesV;
            if (!float.IsNaN(maxHeight))
                border = MathF.Min(border, maxHeight);
            if (!float.IsNaN(minHeight))
                border = MathF.Max(border, minHeight);
            containerCross = MathF.Max(0, border - edgesV);
            if (singleLine && lines.Count == 1)
                lines[0].Cross = containerCross;
        }

        // align-content (§9.4 step 15, §9.6).
        var freeCross = containerCross - linesCross;
        var alignContent = s.AlignContent;
        if (!singleLine && lines.Count > 0)
        {
            if (alignContent is Alignment.Normal or Alignment.Stretch && freeCross > 0)
            {
                var extra = freeCross / lines.Count;
                foreach (var line in lines)
                    line.Cross += extra;
                freeCross = 0;
            }
            var (start, between) = Distribute(alignContent, freeCross, lines.Count);
            var offset = start;
            foreach (var line in lines)
            {
                line.Offset = offset;
                offset += line.Cross + crossGap + between;
            }
        }
        else if (lines.Count > 0)
            lines[0].Offset = 0;
        if (s.FlexWrap == FlexWrap.WrapReverse)
            foreach (var line in lines)
                line.Offset = containerCross - line.Offset - line.Cross;

        // Stretch (§9.4 step 11), then cross-axis alignment.
        foreach (var line in lines)
            foreach (var item in line.Items)
            {
                if (Stretches(item, row))
                {
                    var stretched = MathF.Max(0, line.Cross - item.CrossMargins);
                    var cs = item.Box.Style;
                    var min = row ? (cs.MinHeight.IsAuto ? 0 : _engine.SpecifiedHeight(item.Box, cs.MinHeight, contentHeight)) : (cs.MinWidth.IsAuto ? 0 : _engine.SpecifiedWidth(item.Box, cs.MinWidth, contentWidth, contentWidth));
                    var max = row ? (cs.MaxHeight.IsNone ? float.NaN : _engine.SpecifiedHeight(item.Box, cs.MaxHeight, contentHeight)) : (cs.MaxWidth.IsNone ? float.NaN : _engine.SpecifiedWidth(item.Box, cs.MaxWidth, contentWidth, contentWidth));
                    if (!float.IsNaN(max))
                        stretched = MathF.Min(stretched, max);
                    if (!float.IsNaN(min))
                        stretched = MathF.Max(stretched, min);
                    if (MathF.Abs(stretched - item.Cross) > 0.001f || row)
                        MeasureCross(item, box, row, contentWidth, contentHeight, fixedCross: stretched, final: true);
                    item.Stretched = true;
                }
                var free = line.Cross - item.Cross - item.CrossMargins;
                var marginStart = row ? item.Box.Style.MarginTop : item.Box.Style.MarginLeft;
                var marginEnd = row ? item.Box.Style.MarginBottom : item.Box.Style.MarginRight;
                if (marginStart.IsAuto || marginEnd.IsAuto)
                {
                    var share = marginStart.IsAuto && marginEnd.IsAuto ? MathF.Max(0, free) / 2 : MathF.Max(0, free);
                    item.CrossOffset = marginStart.IsAuto ? share : 0;
                    continue;
                }
                // wrap-reverse swaps cross-start and cross-end; start/end stay physical.
                var wrapReverse = s.FlexWrap == FlexWrap.WrapReverse;
                item.CrossOffset = item.Align switch
                {
                    Alignment.FlexEnd => wrapReverse ? 0 : free,
                    Alignment.End or Alignment.SelfEnd => free,
                    Alignment.Start or Alignment.SelfStart => 0,
                    Alignment.Center => free / 2,
                    Alignment.Baseline or Alignment.LastBaseline when row && !wrapReverse => line.MaxAscent - (item.Ascent + item.Box.Margin.Top),
                    _ => wrapReverse ? free : 0,
                };
            }

        // Main-axis alignment (§9.5) and final positions.
        var reverse = IsReverse(s);
        foreach (var line in lines)
        {
            var mainSize = float.IsNaN(mainForFlex) ? line.Items.Sum(i => i.Main + i.MainMargins) + mainGap * Math.Max(0, line.Items.Count - 1) : mainForFlex;
            var usedMain = line.Items.Sum(i => i.Main + i.MainMargins) + mainGap * Math.Max(0, line.Items.Count - 1);
            var free = mainSize - usedMain;
            var autoMargins = 0;
            foreach (var item in line.Items)
            {
                var cs = item.Box.Style;
                if (row ? cs.MarginLeft.IsAuto : cs.MarginTop.IsAuto)
                    autoMargins++;
                if (row ? cs.MarginRight.IsAuto : cs.MarginBottom.IsAuto)
                    autoMargins++;
            }
            // Offsets from main-start (flex-relative); reverse directions are mirrored afterwards.
            float start = 0, between = 0;
            var perAuto = 0f;
            if (autoMargins > 0 && free > 0)
                perAuto = free / autoMargins;
            else
            {
                (start, between) = s.JustifyContent switch
                {
                    Alignment.FlexEnd => (free, 0f),
                    Alignment.Start or Alignment.Left => (reverse ? free : 0f, 0f),
                    Alignment.End or Alignment.Right => (reverse ? 0f : free, 0f),
                    Alignment.Center or Alignment.SpaceBetween or Alignment.SpaceAround or Alignment.SpaceEvenly => Distribute(s.JustifyContent, free, line.Items.Count),
                    _ => (0f, 0f),
                };
            }
            var position = start;
            foreach (var item in line.Items)
            {
                var cs = item.Box.Style;
                var autoStart = row ? cs.MarginLeft.IsAuto : cs.MarginTop.IsAuto;
                var autoEnd = row ? cs.MarginRight.IsAuto : cs.MarginBottom.IsAuto;
                if (reverse)
                    (autoStart, autoEnd) = (autoEnd, autoStart);
                if (autoStart)
                    position += perAuto;
                item.MainOffset = position;
                position += item.Main + item.MainMargins;
                if (autoEnd)
                    position += perAuto;
                position += mainGap + between;
                if (reverse)
                    item.MainOffset = mainSize - item.MainOffset - item.Main - item.MainMargins;
            }
            foreach (var item in line.Items)
            {
                var child = item.Box;
                if (row)
                {
                    child.X = box.ContentLeft + item.MainOffset + child.Margin.Left;
                    child.Y = box.ContentTop + line.Offset + item.CrossOffset + child.Margin.Top;
                }
                else
                {
                    child.X = box.ContentLeft + line.Offset + item.CrossOffset + child.Margin.Left;
                    child.Y = box.ContentTop + item.MainOffset + child.Margin.Top;
                }
                child.RelativeOffset = LayoutEngine.RelativeOffset(child.Style, contentWidth, contentHeight);
            }
        }

        // Baselines: the first line's baseline-aligned item, else its first item.
        if (lines.Count > 0 && lines[0].Items.Count > 0)
        {
            var first = lines[0].Items.FirstOrDefault(i => i.Align is Alignment.Baseline && row) ?? lines[0].Items.MinBy(i => row ? i.MainOffset : i.MainOffset)!;
            var child = first.Box;
            box.FirstBaseline = child.Y + (child.FirstBaseline ?? child.Height);
            var lastLine = lines[^1];
            var last = lastLine.Items.LastOrDefault(i => i.Align is Alignment.Baseline && row) ?? lastLine.Items[^1];
            box.LastBaseline = last.Box.Y + (last.Box.LastBaseline ?? last.Box.Height);
        }

        // The static areas of absolutely positioned children get the content height once it is known.
        var finalHeight = row ? containerCross : float.IsNaN(contentHeight) ? (float.IsInfinity(availableMain) ? lines.Max(l => l.Items.Sum(i => i.Main + i.MainMargins) + mainGap * Math.Max(0, l.Items.Count - 1)) : availableMain) : contentHeight;
        foreach (var child in box.Children)
            if (child.IsOutOfFlow && child.StaticArea is { } area)
                child.StaticArea = area with { Height = finalHeight };
        if (row)
            return containerCross;
        return float.IsNaN(contentHeight) ? (float.IsInfinity(availableMain) ? lines.Max(l => l.Items.Sum(i => i.Main + i.MainMargins) + mainGap * Math.Max(0, l.Items.Count - 1)) : availableMain) : contentHeight;
    }

    /// <summary>align-self: stretch with an auto cross size and no auto margins in the cross axis.</summary>
    static bool Stretches(Item item, bool row)
    {
        var crossSizeAuto = row ? item.Box.Style.Height.IsAuto : item.Box.Style.Width.IsAuto;
        return item.Align == Alignment.Stretch && crossSizeAuto && !AutoCrossMargins(item, row);
    }

    static bool AutoCrossMargins(Item item, bool row)
    {
        var cs = item.Box.Style;
        return row ? cs.MarginTop.IsAuto || cs.MarginBottom.IsAuto : cs.MarginLeft.IsAuto || cs.MarginRight.IsAuto;
    }

    /// <summary>Start offset and extra space between for the distributed alignment values.</summary>
    static (float Start, float Between) Distribute(Alignment alignment, float free, int count)
    {
        switch (alignment)
        {
            case Alignment.Center:
                return (free / 2, 0);
            case Alignment.FlexEnd or Alignment.End:
                return (free, 0);
            case Alignment.SpaceBetween:
                return free > 0 && count > 1 ? (0, free / (count - 1)) : (0, 0);
            case Alignment.SpaceAround:
                return free > 0 ? (free / count / 2, free / count) : (free / 2, 0);
            case Alignment.SpaceEvenly:
                return free > 0 ? (free / (count + 1), free / (count + 1)) : (free / 2, 0);
            default:
                return (0, 0);
        }
    }

    /// <summary>The item's height when laid out at its cross size (column containers: its content-based main size).</summary>
    float ContentHeight(LayoutBox child, LayoutBox container, Item item, float contentWidth, float contentHeight)
    {
        var space = ItemSpace(container, contentWidth, contentHeight);
        // The content's height: the item's own height property doesn't count.
        space.ContentHeightOnly = true;
        var align = item.Box.Style.AlignSelf is Alignment.Auto ? container.Style.AlignItems : item.Box.Style.AlignSelf;
        if (child.Style.Width.IsAuto && align is Alignment.Normal or Alignment.Stretch && !(child.Style.MarginLeft.IsAuto || child.Style.MarginRight.IsAuto))
            space.FixedWidth = MathF.Max(0, contentWidth - child.Margin.Horizontal);
        else
            space.ShrinkToFit = true;
        return _engine.Measure(child, space).Height;
    }

    static Space ItemSpace(LayoutBox container, float contentWidth, float contentHeight)
    {
        var space = Space.For(contentWidth, contentHeight);
        return space;
    }

    /// <summary>
    /// Lays out an item with its main size fixed; the cross size is its own unless <paramref name="fixedCross"/>
    /// is given. Not <paramref name="final"/>: the item is laid out again afterwards (this only measures).
    /// </summary>
    void MeasureCross(Item item, LayoutBox container, bool row, float contentWidth, float contentHeight, float fixedCross, bool final)
    {
        var child = item.Box;
        var space = ItemSpace(container, contentWidth, contentHeight);
        if (row)
        {
            space.FixedWidth = item.Main;
            if (!float.IsNaN(fixedCross))
            {
                space.FixedHeight = fixedCross;
                // A stretched item's height is definite for its children's percentages.
            }
        }
        else
        {
            space.FixedHeight = item.Main;
            if (!float.IsNaN(fixedCross))
                space.FixedWidth = fixedCross;
            else if (child.Style.Width.IsAuto && item.Align == Alignment.Stretch && !AutoCrossMargins(item, row))
                space.FixedWidth = MathF.Max(0, contentWidth - child.Margin.Horizontal);
            else
                space.ShrinkToFit = true;
        }
        LayoutEngine.Measured measured;
        if (final)
        {
            _engine.LayoutAtomic(child, space);
            measured = new LayoutEngine.Measured(child.Width, child.Height, child.FirstBaseline, child.LastBaseline);
        }
        else
            measured = _engine.Measure(child, space);
        LayoutEngine.ResolveEdges(child, contentWidth);
        item.Cross = row ? measured.Height : measured.Width;
        item.Ascent = measured.FirstBaseline ?? measured.Height;
    }

    static void ResolveFlexibleLengths(Line line, float available, float gap)
    {
        var items = line.Items;
        var gaps = gap * Math.Max(0, items.Count - 1);
        if (float.IsNaN(available))
        {
            foreach (var item in items)
                item.Main = item.Hypothetical;
            return;
        }
        var sumHypothetical = items.Sum(i => i.Hypothetical + i.MainMargins) + gaps;
        var growing = sumHypothetical < available;
        foreach (var item in items)
        {
            item.Frozen = false;
            item.Target = item.Base;
            var factor = growing ? item.Grow : item.Shrink;
            if (factor == 0 || (growing && item.Base > item.Hypothetical) || (!growing && item.Base < item.Hypothetical))
            {
                item.Frozen = true;
                item.Target = item.Hypothetical;
            }
        }
        float FreeSpace() => available - gaps - items.Sum(i => (i.Frozen ? i.Target : i.Base) + i.MainMargins);
        var initialFree = FreeSpace();
        for (var iteration = 0; iteration < items.Count + 1; iteration++)
        {
            var unfrozen = items.Where(i => !i.Frozen).ToList();
            if (unfrozen.Count == 0)
                break;
            var free = FreeSpace();
            var factorSum = unfrozen.Sum(i => growing ? i.Grow : i.Shrink);
            if (factorSum < 1)
            {
                var scaled = initialFree * factorSum;
                if (MathF.Abs(scaled) < MathF.Abs(free))
                    free = scaled;
            }
            if (growing)
            {
                foreach (var item in unfrozen)
                    item.Target = item.Base + (factorSum > 0 ? free * item.Grow / factorSum : 0);
            }
            else
            {
                var scaledSum = unfrozen.Sum(i => i.Shrink * i.BaseContent);
                foreach (var item in unfrozen)
                    item.Target = item.Base + (scaledSum > 0 ? free * (item.Shrink * item.BaseContent) / scaledSum : 0);
            }
            // Fix min/max violations.
            var totalViolation = 0f;
            foreach (var item in unfrozen)
            {
                var clamped = Math.Clamp(item.Target, item.MinMain, item.MaxMain);
                item.Violation = clamped - item.Target;
                totalViolation += item.Violation;
                item.Target = clamped;
            }
            if (MathF.Abs(totalViolation) < 0.001f)
            {
                foreach (var item in unfrozen)
                    item.Frozen = true;
                break;
            }
            foreach (var item in unfrozen)
                if ((totalViolation > 0 && item.Violation > 0) || (totalViolation < 0 && item.Violation < 0))
                    item.Frozen = true;
        }
        // Blink stores flexed sizes as LayoutUnits.
        foreach (var item in items)
            item.Main = LayoutUnit.Truncate(item.Target);
    }

    /// <summary>The container's min-content or max-content content width.</summary>
    public float IntrinsicWidth(LayoutBox box, bool min)
    {
        var s = box.Style;
        var items = box.Children.Where(c => !c.IsOutOfFlow).ToList();
        if (items.Count == 0)
            return 0;
        var gap = s.ColumnGap.IsNone ? 0 : s.ColumnGap.IsValue && !s.ColumnGap.HasPercent ? s.ColumnGap.Pixels : 0;
        if (IsRow(s))
        {
            if (min && s.FlexWrap != FlexWrap.NoWrap)
                return items.Max(c => _engine.Contribution(c, true));
            var sum = 0f;
            foreach (var item in items)
            {
                var contribution = _engine.Contribution(item, min);
                // An item that can't grow doesn't contribute more than its flex basis to max-content.
                var basis = item.Style.FlexBasis;
                if (!min && basis.IsValue && !basis.HasPercent && item.Style.FlexGrow == 0)
                    contribution = MathF.Max(MathF.Min(contribution, BasisOuter(item)), _engine.Contribution(item, true));
                if (!min && basis.IsValue && !basis.HasPercent && item.Style.FlexShrink == 0)
                    contribution = MathF.Max(contribution, BasisOuter(item));
                sum += contribution;
            }
            return sum + gap * (items.Count - 1);
        }
        return items.Max(c => _engine.Contribution(c, min));
    }

    static float BasisOuter(LayoutBox item)
    {
        var s = item.Style;
        var basis = s.FlexBasis.Pixels;
        var edges = s.BorderLeft + s.BorderRight + (s.PaddingLeft.IsValue && !s.PaddingLeft.HasPercent ? LayoutUnit.Truncate(s.PaddingLeft.Pixels) : 0) + (s.PaddingRight.IsValue && !s.PaddingRight.HasPercent ? LayoutUnit.Truncate(s.PaddingRight.Pixels) : 0);
        var margins = (s.MarginLeft.IsValue && !s.MarginLeft.HasPercent ? s.MarginLeft.Pixels : 0) + (s.MarginRight.IsValue && !s.MarginRight.HasPercent ? s.MarginRight.Pixels : 0);
        return (s.BoxSizing == BoxSizing.BorderBox ? MathF.Max(basis, edges) : basis + edges) + margins;
    }
}

using System.Numerics;
using Statventure.UI.Style;

namespace Statventure.UI.Layout;

/// <summary>
/// Grid layout (CSS Grid 1): the explicit grid with repeat(auto-fill/auto-fit), named lines and
/// areas, item placement (definite, row-locked, auto with sparse or dense packing, in either flow
/// direction), implicit tracks, the track sizing algorithm (§11: intrinsic contributions, spanning
/// items, maximizing, flexible tracks, stretching auto tracks), gaps, item alignment and
/// justify/align-content, and the container's intrinsic widths.
/// </summary>
internal sealed class GridLayout
{
    readonly LayoutEngine _engine;

    public GridLayout(LayoutEngine engine)
    {
        _engine = engine;
    }

    enum SizingKind { Fixed, Auto, MinContent, MaxContent, Flex, FitContent }

    /// <summary>How an axis is sized: into a definite size, an indefinite one (free space unlimited), or under a min-/max-content constraint.</summary>
    enum Sizing { Definite, Indefinite, MinContent, MaxContent }

    sealed class Track
    {
        public SizingKind MinKind, MaxKind;
        public float MinFixed, MaxFixed, Flex, FitLimit;
        public float Base, Limit;
        public bool FromAutoFit;
        public bool Collapsed;
        public float Offset;
        public bool IsFlexible => MaxKind == SizingKind.Flex;
        public bool HasIntrinsicMin => MinKind is SizingKind.Auto or SizingKind.MinContent or SizingKind.MaxContent;
        public bool HasIntrinsicMax => MaxKind is SizingKind.Auto or SizingKind.MinContent or SizingKind.MaxContent or SizingKind.FitContent;
    }

    sealed class Item
    {
        public required LayoutBox Box;
        public int RowStart, RowEnd, ColumnStart, ColumnEnd;
        public float Width, Height;
    }

    sealed class Grid
    {
        public readonly List<Track> Columns = [];
        public readonly List<Track> Rows = [];
        public readonly List<Item> Items = [];
        public float ColumnGap, RowGap;
        public int ExplicitColumns, ExplicitRows;
    }

    static Track TrackFrom(TrackSize size, float basis)
    {
        var track = new Track();
        void Set(TrackSize part, bool isMin)
        {
            SizingKind kind;
            float value = 0;
            switch (part.Kind)
            {
                case TrackKind.Length:
                    if (part.Length.HasPercent && float.IsNaN(basis))
                        kind = SizingKind.Auto;
                    else
                    {
                        kind = SizingKind.Fixed;
                        value = MathF.Max(0, LayoutUnit.Truncate(part.Length.ResolveOr(basis, 0)));
                    }
                    break;
                case TrackKind.Fraction:
                    kind = isMin ? SizingKind.Auto : SizingKind.Flex;
                    value = part.Fraction;
                    break;
                case TrackKind.MinContent:
                    kind = SizingKind.MinContent;
                    break;
                case TrackKind.MaxContent:
                    kind = SizingKind.MaxContent;
                    break;
                case TrackKind.FitContent:
                    kind = isMin ? SizingKind.Auto : SizingKind.FitContent;
                    value = MathF.Max(0, LayoutUnit.Truncate(part.Length.ResolveOr(float.IsNaN(basis) ? 0 : basis, 0)));
                    break;
                default:
                    kind = SizingKind.Auto;
                    break;
            }
            if (isMin)
            {
                track.MinKind = kind;
                track.MinFixed = value;
            }
            else
            {
                track.MaxKind = kind;
                if (kind == SizingKind.Flex)
                    track.Flex = value;
                else if (kind == SizingKind.FitContent)
                    track.FitLimit = value;
                else
                    track.MaxFixed = value;
            }
        }
        if (size.Kind == TrackKind.MinMax)
        {
            Set(size.Min!, true);
            Set(size.Max!, false);
        }
        else
        {
            Set(size, true);
            Set(size, false);
        }
        return track;
    }

    // ---------- Explicit grid ----------

    /// <summary>The explicit tracks of one axis, with auto-fill/auto-fit repetitions for the available size.</summary>
    static (List<Track> Tracks, List<List<string>> Lines) ExplicitTracks(GridTemplate template, float available, float gap)
    {
        var tracks = new List<Track>();
        var lines = new List<List<string>>();
        if (template.IsNone)
        {
            lines.Add([]);
            return (tracks, lines);
        }
        var repeatCount = 0;
        if (template.AutoRepeat is { Length: > 0 } repeat)
        {
            // How many repetitions fit (§7.2.3.2): fixed sizes, the max when it's fixed, else the min.
            float Fixed(TrackSize t)
            {
                var track = TrackFrom(t, available);
                if (track.MaxKind == SizingKind.Fixed)
                    return track.MinKind == SizingKind.Fixed ? MathF.Max(track.MinFixed, track.MaxFixed) : track.MaxFixed;
                return track.MinKind == SizingKind.Fixed ? track.MinFixed : 0;
            }
            var others = template.Tracks.Sum(Fixed) + gap * template.Tracks.Length;
            var repeatSize = repeat.Sum(Fixed) + gap * repeat.Length;
            if (float.IsNaN(available) || float.IsInfinity(available) || repeatSize <= 0)
                repeatCount = 1;
            else
                repeatCount = Math.Max(1, (int)MathF.Floor((available - others + gap) / repeatSize));
        }
        var names = template.LineNames;
        for (var i = 0; i <= template.Tracks.Length; i++)
        {
            if (template.AutoRepeat is { } autoRepeat && i == template.RepeatIndex)
            {
                var repeatNames = i < names.Length ? names[i] : [];
                for (var r = 0; r < repeatCount; r++)
                    foreach (var size in autoRepeat)
                    {
                        lines.Add([.. repeatNames]);
                        var track = TrackFrom(size, available);
                        track.FromAutoFit = template.AutoFit;
                        tracks.Add(track);
                        repeatNames = [];
                    }
                if (i < template.Tracks.Length)
                {
                    lines.Add([]);
                    tracks.Add(TrackFrom(template.Tracks[i], available));
                }
                continue;
            }
            var lineNames = i < names.Length ? names[i].ToList() : [];
            if (i == template.Tracks.Length)
            {
                lines.Add(lineNames);
                break;
            }
            lines.Add(lineNames);
            tracks.Add(TrackFrom(template.Tracks[i], available));
        }
        while (lines.Count < tracks.Count + 1)
            lines.Add([]);
        return (tracks, lines);
    }

    /// <summary>grid-template-areas: each named area's rows and columns (0-based, end exclusive).</summary>
    static Dictionary<string, (int RowStart, int RowEnd, int ColumnStart, int ColumnEnd)> Areas(string[]? areas)
    {
        var result = new Dictionary<string, (int, int, int, int)>(StringComparer.Ordinal);
        if (areas is null)
            return result;
        for (var r = 0; r < areas.Length; r++)
        {
            var cells = areas[r].Split((char[])[' ', '\t', '\n'], StringSplitOptions.RemoveEmptyEntries);
            for (var c = 0; c < cells.Length; c++)
            {
                var name = cells[c];
                if (name.All(ch => ch == '.'))
                    continue;
                if (result.TryGetValue(name, out var area))
                    result[name] = (Math.Min(area.Item1, r), Math.Max(area.Item2, r + 1), Math.Min(area.Item3, c), Math.Max(area.Item4, c + 1));
                else
                    result[name] = (r, r + 1, c, c + 1);
            }
        }
        return result;
    }

    // ---------- Placement ----------

    /// <summary>A line index (0-based line number) from a grid-line value; null when auto or not found.</summary>
    static int? ResolveLine(GridLine line, List<List<string>> names, int explicitTracks, bool isEnd, Dictionary<string, (int RowStart, int RowEnd, int ColumnStart, int ColumnEnd)> areas, bool rows)
    {
        if (line.IsAuto || line.Span > 0)
            return null;
        if (line.Name is { } name)
        {
            if (areas.TryGetValue(name, out var area))
                return rows ? (isEnd ? area.RowEnd : area.RowStart) : (isEnd ? area.ColumnEnd : area.ColumnStart);
            var suffixed = name + (isEnd ? "-end" : "-start");
            foreach (var (n, index) in new[] { (name, 0), (suffixed, 0) })
            {
                var count = line.Line == 0 ? 1 : line.Line;
                if (count > 0)
                {
                    for (var i = 0; i < names.Count; i++)
                        if (names[i].Contains(n) && --count == 0)
                            return i;
                }
                else
                {
                    for (var i = names.Count - 1; i >= 0; i--)
                        if (names[i].Contains(n) && ++count == 0)
                            return i;
                }
            }
            // Implicit area lines: "name-start" / "name-end" of a named area.
            foreach (var (areaName, a) in areas)
            {
                if (name == areaName + "-start")
                    return rows ? a.RowStart : a.ColumnStart;
                if (name == areaName + "-end")
                    return rows ? a.RowEnd : a.ColumnEnd;
            }
            return explicitTracks + 1; // an unknown name: the first implicit line after the grid
        }
        if (line.Line > 0)
            return line.Line - 1;
        if (line.Line < 0)
            return Math.Max(0, explicitTracks + 1 + line.Line);
        return null;
    }

    /// <summary>Start and span of an item along one axis; start null when it must be auto-placed.</summary>
    static (int? Start, int Span) Placement(GridLine start, GridLine end, List<List<string>> names, int explicitTracks, Dictionary<string, (int RowStart, int RowEnd, int ColumnStart, int ColumnEnd)> areas, bool rows)
    {
        var s = ResolveLine(start, names, explicitTracks, false, areas, rows);
        var e = ResolveLine(end, names, explicitTracks, true, areas, rows);
        if (s is { } a && e is { } b)
        {
            if (b < a)
                (a, b) = (b, a);
            if (b == a)
                b = a + 1;
            return (a, b - a);
        }
        if (s is { } only)
            return (only, Math.Max(1, end.Span > 0 ? end.Span : 1));
        if (e is { } endOnly)
        {
            var span = Math.Max(1, start.Span > 0 ? start.Span : 1);
            return (Math.Max(0, endOnly - span), span);
        }
        var autoSpan = start.Span > 0 ? start.Span : end.Span > 0 ? end.Span : 1;
        return (null, autoSpan);
    }

    Grid Place(LayoutBox box, float contentWidth, float contentHeight)
    {
        var s = box.Style;
        var grid = new Grid
        {
            ColumnGap = s.ColumnGap.IsNone ? 0 : MathF.Max(0, LayoutUnit.Truncate(s.ColumnGap.ResolveOr(contentWidth, 0))),
            RowGap = s.RowGap.IsNone ? 0 : MathF.Max(0, LayoutUnit.Truncate(s.RowGap.ResolveOr(float.IsNaN(contentHeight) ? 0 : contentHeight, 0))),
        };
        var (columns, columnLines) = ExplicitTracks(s.GridTemplateColumns, contentWidth, grid.ColumnGap);
        var (rows, rowLines) = ExplicitTracks(s.GridTemplateRows, contentHeight, grid.RowGap);
        var areas = Areas(s.GridTemplateAreas);
        // Areas extend the explicit grid.
        var areaRows = areas.Count > 0 ? areas.Values.Max(a => a.RowEnd) : 0;
        var areaColumns = areas.Count > 0 ? areas.Values.Max(a => a.ColumnEnd) : 0;
        grid.ExplicitColumns = Math.Max(columns.Count, areaColumns);
        grid.ExplicitRows = Math.Max(rows.Count, areaRows);
        grid.Columns.AddRange(columns);
        grid.Rows.AddRange(rows);

        var columnFlow = s.GridAutoFlow is GridAutoFlow.Column or GridAutoFlow.ColumnDense;
        var dense = s.GridAutoFlow is GridAutoFlow.RowDense or GridAutoFlow.ColumnDense;
        var occupied = new HashSet<(int Row, int Column)>();
        bool Free(int row, int column, int rowSpan, int columnSpan)
        {
            for (var r = row; r < row + rowSpan; r++)
                for (var c = column; c < column + columnSpan; c++)
                    if (occupied.Contains((r, c)))
                        return false;
            return true;
        }
        void Occupy(Item item)
        {
            for (var r = item.RowStart; r < item.RowEnd; r++)
                for (var c = item.ColumnStart; c < item.ColumnEnd; c++)
                    occupied.Add((r, c));
        }

        var pending = new List<(Item Item, int? Row, int RowSpan, int? Column, int ColumnSpan)>();
        foreach (var child in box.Children)
        {
            if (child.IsOutOfFlow)
            {
                // As if it were the only item in an area covering the content box, aligned by justify-self and align-self.
                child.StaticContainer = box;
                child.StaticPosition = new Vector2(box.ContentLeft, box.ContentTop);
                child.StaticArea = new Core.RectF(box.ContentLeft, box.ContentTop, box.ContentWidth, float.IsNaN(contentHeight) ? 0 : contentHeight);
                static float Factor(Alignment a) => a switch
                {
                    Alignment.Center => 0.5f,
                    Alignment.End or Alignment.FlexEnd or Alignment.SelfEnd or Alignment.Right => 1f,
                    _ => 0f,
                };
                var justify = child.Style.JustifySelf is Alignment.Auto ? s.JustifyItems : child.Style.JustifySelf;
                var align = child.Style.AlignSelf is Alignment.Auto ? s.AlignItems : child.Style.AlignSelf;
                child.StaticAlign = new Vector2(Factor(justify), Factor(align));
                continue;
            }
            child.Container = box;
            var cs = child.Style;
            var (row, rowSpan) = Placement(cs.GridRowStart, cs.GridRowEnd, rowLines, grid.ExplicitRows, areas, rows: true);
            var (column, columnSpan) = Placement(cs.GridColumnStart, cs.GridColumnEnd, columnLines, grid.ExplicitColumns, areas, rows: false);
            var item = new Item { Box = child };
            pending.Add((item, row, rowSpan, column, columnSpan));
        }
        // Order-modified document order.
        pending = pending.OrderBy(p => p.Item.Box.Style.Order).ToList();
        grid.Items.AddRange(pending.Select(p => p.Item));

        // 1. Items with both positions definite.
        foreach (var (item, row, rowSpan, column, columnSpan) in pending)
            if (row is { } r && column is { } c)
            {
                (item.RowStart, item.RowEnd, item.ColumnStart, item.ColumnEnd) = (r, r + rowSpan, c, c + columnSpan);
                Occupy(item);
            }
        // 2. Items locked to a row (or column, in column flow).
        var lockedCursor = new Dictionary<int, int>();
        foreach (var (item, row, rowSpan, column, columnSpan) in pending)
        {
            if (row is not null && column is not null)
                continue;
            var majorLocked = columnFlow ? column : row;
            if (majorLocked is not { } major)
                continue;
            var majorSpan = columnFlow ? columnSpan : rowSpan;
            var minorSpan = columnFlow ? rowSpan : columnSpan;
            var minor = dense ? 0 : lockedCursor.GetValueOrDefault(major);
            while (!(columnFlow ? Free(minor, major, minorSpan, majorSpan) : Free(major, minor, majorSpan, minorSpan)))
                minor++;
            if (columnFlow)
                (item.ColumnStart, item.ColumnEnd, item.RowStart, item.RowEnd) = (major, major + majorSpan, minor, minor + minorSpan);
            else
                (item.RowStart, item.RowEnd, item.ColumnStart, item.ColumnEnd) = (major, major + majorSpan, minor, minor + minorSpan);
            if (!dense)
                lockedCursor[major] = minor + minorSpan;
            Occupy(item);
        }
        // 3. Auto-placed items: the minor axis count is the explicit grid's (or the widest item's).
        var minorCount = columnFlow ? grid.ExplicitRows : grid.ExplicitColumns;
        foreach (var (item, row, rowSpan, column, columnSpan) in pending)
        {
            var minorDefinite = columnFlow ? row : column;
            var minorSpan = columnFlow ? rowSpan : columnSpan;
            if (minorDefinite is { } m)
                minorCount = Math.Max(minorCount, m + minorSpan);
            else
                minorCount = Math.Max(minorCount, minorSpan);
        }
        minorCount = Math.Max(minorCount, 1);
        int cursorMajor = 0, cursorMinor = 0;
        foreach (var (item, row, rowSpan, column, columnSpan) in pending)
        {
            var majorDefinite = columnFlow ? column : row;
            if (majorDefinite is not null)
                continue;
            var minorDefinite = columnFlow ? row : column;
            var majorSpan = columnFlow ? columnSpan : rowSpan;
            var minorSpan = columnFlow ? rowSpan : columnSpan;
            if (dense)
            {
                cursorMajor = 0;
                cursorMinor = 0;
            }
            if (minorDefinite is { } fixedMinor)
            {
                if (!dense && fixedMinor < cursorMinor)
                    cursorMajor++;
                cursorMinor = fixedMinor;
                while (!(columnFlow ? Free(cursorMinor, cursorMajor, minorSpan, majorSpan) : Free(cursorMajor, cursorMinor, majorSpan, minorSpan)))
                    cursorMajor++;
            }
            else
            {
                while (true)
                {
                    if (cursorMinor + minorSpan > minorCount)
                    {
                        cursorMinor = 0;
                        cursorMajor++;
                        continue;
                    }
                    if (columnFlow ? Free(cursorMinor, cursorMajor, minorSpan, majorSpan) : Free(cursorMajor, cursorMinor, majorSpan, minorSpan))
                        break;
                    cursorMinor++;
                }
            }
            if (columnFlow)
                (item.ColumnStart, item.ColumnEnd, item.RowStart, item.RowEnd) = (cursorMajor, cursorMajor + majorSpan, cursorMinor, cursorMinor + minorSpan);
            else
                (item.RowStart, item.RowEnd, item.ColumnStart, item.ColumnEnd) = (cursorMajor, cursorMajor + majorSpan, cursorMinor, cursorMinor + minorSpan);
            Occupy(item);
            cursorMinor += minorSpan;
        }

        // Implicit tracks.
        var columnCount = Math.Max(grid.ExplicitColumns, grid.Items.Count > 0 ? grid.Items.Max(i => i.ColumnEnd) : 0);
        var rowCount = Math.Max(grid.ExplicitRows, grid.Items.Count > 0 ? grid.Items.Max(i => i.RowEnd) : 0);
        AddImplicit(grid.Columns, columnCount, s.GridAutoColumns, contentWidth, grid.ExplicitColumns);
        AddImplicit(grid.Rows, rowCount, s.GridAutoRows, contentHeight, grid.ExplicitRows);

        // auto-fit: empty repeated tracks collapse.
        foreach (var (tracks, rowsAxis) in new[] { (grid.Columns, false), (grid.Rows, true) })
            for (var t = 0; t < tracks.Count; t++)
                if (tracks[t].FromAutoFit && !grid.Items.Any(i => rowsAxis ? i.RowStart <= t && t < i.RowEnd : i.ColumnStart <= t && t < i.ColumnEnd))
                    tracks[t].Collapsed = true;
        return grid;
    }

    static void AddImplicit(List<Track> tracks, int count, TrackSize[] pattern, float basis, int explicitCount)
    {
        var index = 0;
        while (tracks.Count < count)
        {
            // Area-defined explicit tracks beyond the template also use grid-auto-*.
            tracks.Add(TrackFrom(pattern[index % pattern.Length], basis));
            index++;
        }
    }

    // ---------- Track sizing (§11) ----------

    float Gaps(List<Track> tracks, float gap, int start, int end)
    {
        var count = 0;
        for (var t = start; t < end - 1; t++)
            if (!tracks[t].Collapsed)
                count++;
        return gap * count;
    }

    static float TotalGaps(List<Track> tracks, float gap) => gap * Math.Max(0, tracks.Count(t => !t.Collapsed) - 1);

    /// <summary>Sizes one axis' tracks. <paramref name="contribution"/> gives an item's (min, max, minimum) contributions in this axis.</summary>
    void SizeTracks(List<Track> tracks, List<Item> items, bool rows, float available, float gap, Func<Item, (float Min, float Max, float Minimum)> contribution, Alignment distribution, Sizing sizing)
    {
        if (sizing == Sizing.Definite && (float.IsNaN(available) || float.IsInfinity(available)))
            sizing = Sizing.Indefinite;
        var maxContentConstraint = sizing == Sizing.MaxContent;
        var minContentConstraint = sizing == Sizing.MinContent;
        // Initialize (§11.4).
        foreach (var track in tracks)
        {
            if (track.Collapsed)
            {
                track.Base = track.Limit = 0;
                continue;
            }
            track.Base = track.MinKind == SizingKind.Fixed ? track.MinFixed : 0;
            track.Limit = track.MaxKind switch
            {
                SizingKind.Fixed => track.MaxFixed,
                SizingKind.Flex => track.Base,
                _ => float.PositiveInfinity,
            };
            if (track.Limit < track.Base)
                track.Limit = track.Base;
        }

        int Start(Item i) => rows ? i.RowStart : i.ColumnStart;
        int End(Item i) => rows ? i.RowEnd : i.ColumnEnd;
        bool SpansFlexible(Item i)
        {
            for (var t = Start(i); t < End(i); t++)
                if (tracks[t].IsFlexible)
                    return true;
            return false;
        }

        // Resolve intrinsic track sizes (§11.5): single-span items first, then by span.
        var contributions = new Dictionary<Item, (float Min, float Max, float Minimum)>();
        foreach (var item in items)
            contributions[item] = contribution(item);
        foreach (var item in items.Where(i => End(i) - Start(i) == 1))
        {
            var track = tracks[Start(item)];
            if (track.Collapsed)
                continue;
            var (min, max, minimum) = contributions[item];
            switch (track.MinKind)
            {
                case SizingKind.Auto:
                    // The minimum contribution (under a content constraint, that content size), limited by a fixed max track size.
                    var automatic = maxContentConstraint ? max : minContentConstraint ? min : minimum;
                    if (track.MaxKind == SizingKind.Fixed)
                        automatic = MathF.Min(automatic, MathF.Max(track.MaxFixed, track.MinFixed));
                    track.Base = MathF.Max(track.Base, automatic);
                    break;
                case SizingKind.MinContent:
                    track.Base = MathF.Max(track.Base, min);
                    break;
                case SizingKind.MaxContent:
                    track.Base = MathF.Max(track.Base, max);
                    break;
            }
            switch (track.MaxKind)
            {
                case SizingKind.MinContent:
                    track.Limit = Grow(track.Limit, min);
                    break;
                case SizingKind.MaxContent:
                case SizingKind.Auto:
                    track.Limit = Grow(track.Limit, max);
                    break;
                case SizingKind.FitContent:
                    track.Limit = Grow(track.Limit, MathF.Max(min, MathF.Min(max, track.FitLimit)));
                    break;
            }
            if (track.Limit < track.Base)
                track.Limit = track.Base;
        }
        foreach (var group in items.Where(i => End(i) - Start(i) > 1 && !SpansFlexible(i)).GroupBy(i => End(i) - Start(i)).OrderBy(g => g.Key))
        {
            foreach (var item in group)
            {
                var spanned = Enumerable.Range(Start(item), End(item) - Start(item)).Select(t => tracks[t]).Where(t => !t.Collapsed).ToList();
                if (spanned.Count == 0)
                    continue;
                var (min, max, minimum) = contributions[item];
                var gaps = Gaps(tracks, gap, Start(item), End(item));
                // Base sizes of tracks with intrinsic minimums.
                var intrinsic = spanned.Where(t => t.HasIntrinsicMin).ToList();
                var needed = (maxContentConstraint ? max : minContentConstraint ? min : minimum) - spanned.Sum(t => t.Base) - gaps;
                if (needed > 0 && intrinsic.Count > 0)
                    foreach (var t in intrinsic)
                        t.Base += needed / intrinsic.Count;
                foreach (var t in spanned)
                    if (t.Limit < t.Base)
                        t.Limit = t.Base;
                // Growth limits of tracks with intrinsic maximums.
                var growable = spanned.Where(t => t.HasIntrinsicMax).ToList();
                var limitSum = spanned.Sum(t => float.IsInfinity(t.Limit) ? t.Base : t.Limit);
                var neededLimit = max - limitSum - gaps;
                if (growable.Count > 0)
                    foreach (var t in growable)
                    {
                        var current = float.IsInfinity(t.Limit) ? t.Base : t.Limit;
                        t.Limit = neededLimit > 0 ? current + neededLimit / growable.Count : (float.IsInfinity(t.Limit) ? t.Base : t.Limit);
                    }
            }
        }
        foreach (var track in tracks)
            if (float.IsInfinity(track.Limit))
                track.Limit = track.Base;

        // Maximize tracks (§11.6): unlimited free space (max-content, or an indefinite size) grows
        // every track to its growth limit; a min-content constraint grows nothing.
        var definite = sizing == Sizing.Definite;
        if (sizing is Sizing.MaxContent or Sizing.Indefinite)
        {
            foreach (var track in tracks)
                if (!track.IsFlexible)
                    track.Base = MathF.Max(track.Base, track.Limit);
        }
        else if (definite)
        {
            var free = available - tracks.Sum(t => t.Base) - TotalGaps(tracks, gap);
            for (var guard = 0; free > 0.001f && guard < tracks.Count + 2; guard++)
            {
                var growable = tracks.Where(t => !t.Collapsed && t.Base < t.Limit - 0.001f && !t.IsFlexible).ToList();
                if (growable.Count == 0)
                    break;
                var share = MathF.Max(LayoutUnit.Truncate(free / growable.Count), LayoutUnit.Epsilon);
                foreach (var t in growable)
                {
                    var grow = MathF.Min(MathF.Min(share, t.Limit - t.Base), free);
                    t.Base += grow;
                    free -= grow;
                }
            }
        }

        // Expand flexible tracks (§11.7).
        var flexible = tracks.Where(t => t.IsFlexible && !t.Collapsed).ToList();
        if (flexible.Count > 0)
        {
            float fr;
            if (definite)
                fr = FindFrSize(tracks, Enumerable.Range(0, tracks.Count).ToList(), available - TotalGaps(tracks, gap));
            else if (minContentConstraint)
                fr = 0;
            else
            {
                // Indefinite: the largest of each flexible track's base / flex and what items need.
                fr = 0;
                foreach (var t in flexible)
                    fr = MathF.Max(fr, t.Flex > 1 ? t.Base / t.Flex : t.Base);
                foreach (var item in items.Where(SpansFlexible))
                {
                    var spannedIndexes = Enumerable.Range(Start(item), End(item) - Start(item)).ToList();
                    var space = contributions[item].Max - Gaps(tracks, gap, Start(item), End(item));
                    fr = MathF.Max(fr, FindFrSize(tracks, spannedIndexes, space));
                }
            }
            // A flexible track's size is LayoutUnit(fr × flex) (truncated) in Blink.
            foreach (var t in flexible)
                t.Base = MathF.Max(t.Base, LayoutUnit.Truncate(fr * t.Flex));
        }

        // Stretch auto tracks (§11.8).
        if (definite && distribution is Alignment.Normal or Alignment.Stretch)
        {
            var free = available - tracks.Sum(t => t.Base) - TotalGaps(tracks, gap);
            var autos = tracks.Where(t => t.MaxKind == SizingKind.Auto && !t.Collapsed).ToList();
            if (free > 0 && autos.Count > 0 && flexible.Count == 0)
                foreach (var t in autos)
                    t.Base += LayoutUnit.Truncate(free / autos.Count);
        }
    }

    static float Grow(float limit, float value) => float.IsInfinity(limit) ? value : MathF.Max(limit, value);

    /// <summary>Finds the size of an fr (§11.7.1) for the given tracks filling <paramref name="space"/>.</summary>
    static float FindFrSize(List<Track> all, List<int> indexes, float space)
    {
        var inflexible = new HashSet<int>();
        for (var guard = 0; guard < indexes.Count + 1; guard++)
        {
            var leftover = space;
            var flexSum = 0f;
            foreach (var i in indexes)
            {
                var t = all[i];
                if (t.Collapsed)
                    continue;
                if (t.IsFlexible && !inflexible.Contains(i))
                    flexSum += t.Flex;
                else
                    leftover -= t.Base;
            }
            var fr = MathF.Max(0, leftover) / MathF.Max(flexSum, 1);
            var restart = false;
            foreach (var i in indexes)
            {
                var t = all[i];
                if (t.IsFlexible && !inflexible.Contains(i) && t.Base > fr * t.Flex)
                {
                    inflexible.Add(i);
                    restart = true;
                }
            }
            if (!restart)
                return fr;
        }
        return 0;
    }

    // ---------- Layout ----------

    public void Layout(LayoutBox box, in Space space) => _engine.LayoutContainer(box, space, LayoutContents);

    float LayoutContents(LayoutBox box, Space space, float definiteContentHeight)
    {
        var s = box.Style;
        var contentWidth = box.ContentWidth;
        box.Inline = null;
        box.FirstBaseline = box.LastBaseline = null;
        var grid = Place(box, contentWidth, definiteContentHeight);

        // Columns: item widths.
        SizeTracks(grid.Columns, grid.Items, rows: false, contentWidth, grid.ColumnGap, item =>
        {
            var min = _engine.Contribution(item.Box, true);
            var max = _engine.Contribution(item.Box, false);
            return (min, max, Minimum(item.Box, min, horizontal: true, contentWidth));
        }, s.JustifyContent, Sizing.Definite);
        PositionTracks(grid.Columns, grid.ColumnGap, contentWidth, s.JustifyContent);

        // Rows: item heights at their column widths.
        foreach (var item in grid.Items)
        {
            var areaWidth = AreaSize(grid.Columns, grid.ColumnGap, item.ColumnStart, item.ColumnEnd);
            LayoutItem(item, box, areaWidth, float.NaN, contentWidth, definiteContentHeight, finalPass: false);
        }
        SizeTracks(grid.Rows, grid.Items, rows: true, definiteContentHeight, grid.RowGap, item =>
        {
            var h = item.Height + item.Box.Margin.Vertical;
            return (h, h, MinimumHeight(item.Box, h));
        }, s.AlignContent, Sizing.Definite);

        // The content height: the rows (when the container's height is auto).
        var rowsHeight = grid.Rows.Sum(t => t.Base) + TotalGaps(grid.Rows, grid.RowGap);
        var contentHeight = definiteContentHeight;
        if (float.IsNaN(contentHeight))
        {
            var edgesV = box.Border.Vertical + box.Padding.Vertical + box.HorizontalScrollbar;
            var border = rowsHeight + edgesV;
            var minHeight = s.MinHeight.IsAuto ? float.NaN : _engine.SpecifiedHeight(box, s.MinHeight, space.PercentHeight);
            var maxHeight = s.MaxHeight.IsNone ? float.NaN : _engine.SpecifiedHeight(box, s.MaxHeight, space.PercentHeight);
            if (!float.IsNaN(maxHeight))
                border = MathF.Min(border, maxHeight);
            if (!float.IsNaN(minHeight))
                border = MathF.Max(border, minHeight);
            contentHeight = MathF.Max(0, border - edgesV);
            // With a min-height, flexible and stretched rows fill the now definite height.
            if (contentHeight > rowsHeight + 0.01f)
                SizeTracks(grid.Rows, grid.Items, rows: true, contentHeight, grid.RowGap, item =>
                {
                    var h = item.Height + item.Box.Margin.Vertical;
                    return (h, h, MinimumHeight(item.Box, h));
                }, s.AlignContent, Sizing.Definite);
        }
        PositionTracks(grid.Rows, grid.RowGap, contentHeight, s.AlignContent);

        // Final item layout and alignment in their areas.
        foreach (var item in grid.Items)
        {
            var areaWidth = AreaSize(grid.Columns, grid.ColumnGap, item.ColumnStart, item.ColumnEnd);
            var areaHeight = AreaSize(grid.Rows, grid.RowGap, item.RowStart, item.RowEnd);
            LayoutItem(item, box, areaWidth, areaHeight, contentWidth, contentHeight, finalPass: true);
            var child = item.Box;
            var cs = child.Style;
            var x = grid.Columns[item.ColumnStart].Offset;
            var y = grid.Rows[item.RowStart].Offset;
            var justify = cs.JustifySelf is Alignment.Auto ? s.JustifyItems : cs.JustifySelf;
            var align = cs.AlignSelf is Alignment.Auto ? s.AlignItems : cs.AlignSelf;
            var freeX = areaWidth - child.MarginBoxWidth;
            var freeY = areaHeight - child.MarginBoxHeight;
            if (cs.MarginLeft.IsAuto || cs.MarginRight.IsAuto)
                x += cs.MarginLeft.IsAuto && cs.MarginRight.IsAuto ? MathF.Max(0, freeX) / 2 : cs.MarginLeft.IsAuto ? MathF.Max(0, freeX) : 0;
            else
                x += AlignOffset(justify, freeX);
            if (cs.MarginTop.IsAuto || cs.MarginBottom.IsAuto)
                y += cs.MarginTop.IsAuto && cs.MarginBottom.IsAuto ? MathF.Max(0, freeY) / 2 : cs.MarginTop.IsAuto ? MathF.Max(0, freeY) : 0;
            else
                y += AlignOffset(align, freeY);
            child.X = box.ContentLeft + x + child.Margin.Left;
            child.Y = box.ContentTop + y + child.Margin.Top;
            child.RelativeOffset = LayoutEngine.RelativeOffset(cs, areaWidth, areaHeight);
        }

        foreach (var child in box.Children)
            if (child.IsOutOfFlow && child.StaticArea is { } area)
                child.StaticArea = area with { Height = contentHeight };

        // Baselines: the first row's first item.
        var firstRow = grid.Items.Where(i => i.RowStart == 0).OrderBy(i => i.ColumnStart).FirstOrDefault() ?? grid.Items.OrderBy(i => i.RowStart).ThenBy(i => i.ColumnStart).FirstOrDefault();
        if (firstRow is not null)
            box.FirstBaseline = firstRow.Box.Y + (firstRow.Box.FirstBaseline ?? firstRow.Box.Height);
        var lastRow = grid.Items.OrderBy(i => i.RowStart).ThenBy(i => i.ColumnStart).LastOrDefault();
        if (lastRow is not null)
            box.LastBaseline = lastRow.Box.Y + (lastRow.Box.LastBaseline ?? lastRow.Box.Height);
        return contentHeight;
    }

    static float AlignOffset(Alignment alignment, float free) => alignment switch
    {
        Alignment.End or Alignment.FlexEnd or Alignment.SelfEnd or Alignment.Right => free,
        Alignment.Center => free / 2,
        _ => 0,
    };

    /// <summary>The minimum contribution: the automatic minimum (min-content, unless the item scrolls) or an explicit min-width.</summary>
    float Minimum(LayoutBox child, float minContent, bool horizontal, float basis)
    {
        var cs = child.Style;
        var margins = (cs.MarginLeft.IsValue && !cs.MarginLeft.HasPercent ? cs.MarginLeft.Pixels : 0) + (cs.MarginRight.IsValue && !cs.MarginRight.HasPercent ? cs.MarginRight.Pixels : 0);
        if (cs.Width.IsDefinite && !cs.Width.HasPercent)
            return minContent;
        if (!cs.MinWidth.IsAuto)
        {
            var min = _engine.SpecifiedWidth(child, cs.MinWidth, float.NaN, 0);
            return (float.IsNaN(min) ? 0 : min) + margins;
        }
        // (CalculateIntrinsicMinimumContribution: the axis's computed overflow, not whether the item scrolls.)
        if (cs.IsOverflowValueScrollableX)
            return margins + cs.BorderLeft + cs.BorderRight + Fixed(cs.PaddingLeft) + Fixed(cs.PaddingRight);
        return minContent;
    }

    static float Fixed(Length length) => length.IsValue && !length.HasPercent ? MathF.Max(0, LayoutUnit.Truncate(length.Pixels)) : 0;

    /// <summary>
    /// An item's minimum contribution to its rows: an explicit min-height, nothing for a scroll
    /// container (its automatic minimum is zero), else its content height.
    /// </summary>
    float MinimumHeight(LayoutBox child, float outerHeight)
    {
        var cs = child.Style;
        // An item with its own definite height contributes that (its min-content contribution).
        if (cs.Height.IsDefinite && !cs.Height.HasPercent)
            return outerHeight;
        var margins = child.Margin.Vertical;
        var edges = child.Border.Vertical + child.Padding.Vertical;
        if (!cs.MinHeight.IsAuto)
        {
            var min = cs.MinHeight.HasPercent ? float.NaN : _engine.SpecifiedHeight(child, cs.MinHeight, float.NaN);
            return (float.IsNaN(min) ? edges : min) + margins;
        }
        if (cs.IsOverflowValueScrollableY)
            return edges + margins;
        return outerHeight;
    }

    static float AreaSize(List<Track> tracks, float gap, int start, int end)
    {
        var size = 0f;
        var counted = 0;
        for (var t = start; t < end && t < tracks.Count; t++)
        {
            if (tracks[t].Collapsed)
                continue;
            size += tracks[t].Base;
            counted++;
        }
        return size + gap * Math.Max(0, counted - 1);
    }

    /// <summary>Track offsets with justify-content / align-content applied to any free space.</summary>
    static void PositionTracks(List<Track> tracks, float gap, float available, Alignment alignment)
    {
        var used = tracks.Sum(t => t.Base) + TotalGaps(tracks, gap);
        var free = float.IsNaN(available) ? 0 : available - used;
        var count = tracks.Count(t => !t.Collapsed);
        float start = 0, between = 0;
        switch (alignment)
        {
            case Alignment.Center:
                start = free / 2;
                break;
            case Alignment.End or Alignment.FlexEnd or Alignment.Right:
                start = free;
                break;
            case Alignment.SpaceBetween when free > 0 && count > 1:
                between = free / (count - 1);
                break;
            case Alignment.SpaceAround when free > 0 && count > 0:
                between = free / count;
                start = between / 2;
                break;
            case Alignment.SpaceEvenly when free > 0:
                between = free / (count + 1);
                start = between;
                break;
        }
        var position = start;
        var first = true;
        foreach (var track in tracks)
        {
            if (track.Collapsed)
            {
                track.Offset = position;
                continue;
            }
            if (!first)
                position += gap + between;
            track.Offset = position;
            position += track.Base;
            first = false;
        }
    }

    /// <summary>Lays out an item in its area: stretched (auto size, no auto margins) or fit-content.</summary>
    void LayoutItem(Item item, LayoutBox container, float areaWidth, float areaHeight, float contentWidth, float contentHeight, bool finalPass)
    {
        var child = item.Box;
        var cs = child.Style;
        var s = container.Style;
        var space = Space.For(areaWidth, finalPass ? areaHeight : float.NaN);
        LayoutEngine.ResolveEdges(child, areaWidth);
        var justify = cs.JustifySelf is Alignment.Auto ? s.JustifyItems : cs.JustifySelf;
        var align = cs.AlignSelf is Alignment.Auto ? s.AlignItems : cs.AlignSelf;
        var stretchX = justify is Alignment.Normal or Alignment.Stretch && cs.Width.IsAuto && !cs.MarginLeft.IsAuto && !cs.MarginRight.IsAuto
            && !(child.Type == BoxType.Replaced && justify == Alignment.Normal) && !(cs.AspectRatio > 0 && justify == Alignment.Normal && !cs.Height.IsAuto);
        if (stretchX)
            space.FixedWidth = MathF.Max(0, areaWidth - child.Margin.Horizontal);
        else
            space.ShrinkToFit = true;
        var stretchY = finalPass && align is Alignment.Normal or Alignment.Stretch && cs.Height.IsAuto && !cs.MarginTop.IsAuto && !cs.MarginBottom.IsAuto
            && !(child.Type == BoxType.Replaced && align == Alignment.Normal) && !(cs.AspectRatio > 0 && align == Alignment.Normal);
        if (stretchY)
        {
            var target = MathF.Max(0, areaHeight - child.Margin.Vertical);
            var max = cs.MaxHeight.IsNone ? float.NaN : _engine.SpecifiedHeight(child, cs.MaxHeight, areaHeight);
            if (!float.IsNaN(max))
                target = MathF.Min(target, max);
            space.FixedHeight = target;
        }
        if (finalPass)
        {
            _engine.LayoutAtomic(child, space);
            item.Width = child.Width;
            item.Height = child.Height;
        }
        else
        {
            // Measuring for the row sizes: the item is laid out in its area afterwards.
            var measured = _engine.Measure(child, space);
            item.Width = measured.Width;
            item.Height = measured.Height;
        }
        LayoutEngine.ResolveEdges(child, areaWidth);
    }

    /// <summary>The container's min-content or max-content content width (§11.3 with an intrinsic constraint).</summary>
    public float IntrinsicWidth(LayoutBox box, bool min)
    {
        var grid = Place(box, float.NaN, float.NaN);
        SizeTracks(grid.Columns, grid.Items, rows: false, float.NaN, grid.ColumnGap, item =>
        {
            var minContent = _engine.Contribution(item.Box, true);
            var maxContent = _engine.Contribution(item.Box, false);
            return (minContent, maxContent, Minimum(item.Box, minContent, true, 0));
        }, Alignment.Start, min ? Sizing.MinContent : Sizing.MaxContent);
        return grid.Columns.Sum(t => t.Base) + TotalGaps(grid.Columns, grid.ColumnGap);
    }
}

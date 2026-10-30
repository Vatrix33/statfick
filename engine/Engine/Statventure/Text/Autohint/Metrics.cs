namespace Statventure.Text.Autohint;

/// <summary>FreeType's fixed-point helpers as skrifa computes them (read-fonts' Fixed: FT_MulFix, FT_DivFix, FT_MulDiv).</summary>
internal static class Fixed
{
    public static int Mul(int a, int b)
    {
        var ab = (long)a * b;
        return (int)((ab + 0x8000 - (ab < 0 ? 1 : 0)) >> 16);
    }

    public static int Div(int a, int b)
    {
        var negative = (a < 0) ^ (b < 0);
        var au = (ulong)Math.Abs((long)a);
        var bu = (ulong)Math.Abs((long)b);
        var q = bu == 0 ? 0x7FFFFFFFu : (uint)(((au << 16) + (bu >> 1)) / bu);
        return negative ? unchecked(-(int)q) : (int)q;
    }

    public static int MulDiv(int a, int b, int c)
    {
        var sign = 1;
        var su = (ulong)(long)a;
        var au = (ulong)(long)b;
        var bu = (ulong)(long)c;
        if (a < 0)
        {
            su = unchecked(0UL - su);
            sign = -1;
        }
        if (b < 0)
        {
            au = unchecked(0UL - au);
            sign = -sign;
        }
        if (c < 0)
        {
            bu = unchecked(0UL - bu);
            sign = -sign;
        }
        var result = bu > 0 ? unchecked(su * au + (bu >> 1)) / bu : 0x7FFFFFFFUL;
        return sign < 0 ? unchecked(-(int)result) : unchecked((int)result);
    }

    public static int PixRound(int a) => (a + 32) & ~63;

    public static int PixFloor(int a) => a & ~63;
}

/// <summary>How a size is rendered (skrifa's hinting Target).</summary>
internal enum SmoothMode { Normal, Light, Lcd, VerticalLcd }

internal readonly record struct HintTarget(bool Mono, SmoothMode Mode, bool PreserveLinearMetrics)
{
    public bool IsLight => !Mono && Mode == SmoothMode.Light;
    public bool IsLcd => !Mono && Mode == SmoothMode.Lcd;
    public bool IsVerticalLcd => !Mono && Mode == SmoothMode.VerticalLcd;
    public bool KeepsLinearMetrics => !Mono && PreserveLinearMetrics;
}

[Flags]
internal enum ScaleFlags
{
    None = 0,
    HorizontalSnap = 1 << 0,
    VerticalSnap = 1 << 1,
    StemAdjust = 1 << 2,
    Mono = 1 << 3,
    NoHorizontal = 1 << 4,
    NoVertical = 1 << 5,
    NoAdvance = 1 << 6,
}

/// <summary>Scaling parameters, which metrics may adjust (skrifa's Scale).</summary>
internal struct Scale
{
    public ScaleFlags Flags;
    /// <summary>Font units to 26.6.</summary>
    public int XScale, YScale;
    /// <summary>In 1/64 pixels.</summary>
    public int XDelta, YDelta;
    public float Size;
    public int UnitsPerEm;

    public Scale(float size, int unitsPerEm, bool isItalic, HintTarget target, ScriptGroup group)
    {
        var scale = Fixed.Div((int)(size * 64f), unitsPerEm);
        var flags = ScaleFlags.None;
        var isMono = target.Mono;
        var isLight = target.IsLight || target.KeepsLinearMetrics;
        // Snap vertical stems for monochrome and horizontal LCD rendering, horizontal stems for monochrome and vertical LCD.
        if (isMono || target.IsLcd)
            flags |= ScaleFlags.HorizontalSnap;
        if (isMono || target.IsVerticalLcd)
            flags |= ScaleFlags.VerticalSnap;
        // Stems go to full pixels unless in LCD or light modes.
        if (!(target.IsLcd || isLight))
            flags |= ScaleFlags.StemAdjust;
        if (isMono)
            flags |= ScaleFlags.Mono;
        if (group == ScriptGroup.Default)
        {
            // No horizontal hinting for LCD, light hinting and italic fonts.
            if (target.IsLcd || isLight || isItalic)
                flags |= ScaleFlags.NoHorizontal;
        }
        else
            flags |= ScaleFlags.NoAdvance;
        XScale = YScale = scale;
        XDelta = YDelta = 0;
        Size = size;
        UnitsPerEm = unitsPerEm;
        Flags = flags;
    }
}

internal struct WidthMetrics
{
    public int EdgeDistanceThreshold;
    public int StandardWidth;
    public bool IsExtraLight;
}

internal record struct ScaledWidth(int Scaled, int Fitted);

internal record struct UnscaledBlue(int Position, int Overshoot, int Ascender, int Descender, BlueZones Zones);

internal record struct ScaledBlue(ScaledWidth Position, ScaledWidth Overshoot, BlueZones Zones, bool IsActive);

internal sealed class UnscaledAxisMetrics
{
    public Dimension Dim;
    public List<int> Widths = [];
    public WidthMetrics WidthMetrics;
    public List<UnscaledBlue> Blues = [];
}

internal sealed class ScaledAxisMetrics
{
    public Dimension Dim;
    public int Scale;
    public int Delta;
    public readonly List<ScaledWidth> Widths = [];
    public WidthMetrics WidthMetrics;
    public readonly List<ScaledBlue> Blues = [];
}

internal sealed class UnscaledStyleMetrics
{
    public int ClassIx;
    public bool DigitsHaveSameWidth;
    public UnscaledAxisMetrics[] Axes = [new() { Dim = Dimension.Horizontal }, new() { Dim = Dimension.Vertical }];

    public StyleClass StyleClass => StyleClass.All[ClassIx];
}

internal sealed class ScaledStyleMetrics
{
    public Scale Scale;
    public ScaledAxisMetrics[] Axes = [];
}

/// <summary>Style metrics: stem widths, blue zones and their scaling (skrifa's autohint metrics; FreeType's aflatin.c and afcjk.c).</summary>
internal static class MetricsComputer
{
    public const int MaxWidths = 16;
    const int MaxBlues = 8;

    static int DerivedConstant(int unitsPerEm, int value) => value * unitsPerEm / 2048;

    /// <summary>af_sort_and_quantize_widths: sorts, averages clusters within the threshold, drops the zeros.</summary>
    public static void SortAndQuantizeWidths(List<int> widths, int threshold)
    {
        if (widths.Count <= 1)
            return;
        widths.Sort();
        var table = widths;
        var curIx = 0;
        var curVal = table[curIx];
        var lastIx = table.Count - 1;
        var ix = 1;
        while (ix < table.Count)
        {
            if (table[ix] - curVal > threshold || ix == lastIx)
            {
                var sum = 0;
                if (table[ix] - curVal <= threshold && ix == lastIx)
                    ix++;
                for (var i = curIx; i < ix; i++)
                {
                    sum += table[i];
                    table[i] = 0;
                }
                table[curIx] = sum / ix;
                if (ix < lastIx)
                {
                    curIx = ix + 1;
                    curVal = table[curIx];
                }
            }
            ix++;
        }
        curIx = 1;
        for (var i = 1; i < table.Count; i++)
            if (table[i] != 0)
                table[curIx++] = table[i];
        widths.RemoveRange(curIx, widths.Count - curIx);
    }

    public static UnscaledStyleMetrics ComputeUnscaledStyleMetrics(Shaper shaper, StyleClass style, QuirksMode quirks)
    {
        var font = shaper.Font;
        // No metrics without a Unicode character map.
        if (font.IsSymbol)
            return new UnscaledStyleMetrics { ClassIx = style.Index };
        var widths = ComputeWidths(shaper, style, quirks);
        var blues = ComputeUnscaledBlues(shaper, style);
        int? digitAdvance = null;
        var same = true;
        for (var digit = '0'; digit <= '9'; digit++)
            if (font.Map(digit) is { } glyph && font.AdvanceWidth(glyph) is { } advance)
            {
                if (digitAdvance is not null && digitAdvance != advance)
                {
                    same = false;
                    break;
                }
                digitAdvance = advance;
            }
        return new UnscaledStyleMetrics
        {
            ClassIx = style.Index,
            DigitsHaveSameWidth = same,
            Axes =
            [
                new() { Dim = Dimension.Horizontal, Blues = blues[0], WidthMetrics = widths[0].Metrics, Widths = widths[0].Widths },
                new() { Dim = Dimension.Vertical, Blues = blues[1], WidthMetrics = widths[1].Metrics, Widths = widths[1].Widths },
            ],
        };
    }

    /// <summary>The standard stem widths of each dimension, from the first standard character that shapes to one glyph (aflatin.c af_latin_metrics_init_widths).</summary>
    static (WidthMetrics Metrics, List<int> Widths)[] ComputeWidths(Shaper shaper, StyleClass style, QuirksMode quirks)
    {
        var result = new (WidthMetrics Metrics, List<int> Widths)[] { (default, []), (default, []) };
        var font = shaper.Font;
        var unitsPerEm = font.UnitsPerEm;
        var outline = new Outline();
        var axis = new Axis();
        var clusterShaper = shaper.ForStyle(style);
        var shaped = new List<ShapedGlyph>();
        uint? glyph = null;
        foreach (var cluster in style.Script.StdChars.Split(' '))
        {
            clusterShaper.Shape(cluster, shaped);
            // Only clusters of a single (real) glyph.
            if (shaped.Count == 1 && shaped[0].Id != 0 && shaped[0].Id < (uint)font.GlyphCount)
            {
                glyph = shaped[0].Id;
                break;
            }
        }
        if (glyph is { } g && outline.Fill(font, g, quirks) && outline.PointCount > 0)
            for (var d = 0; d < 2; d++)
            {
                var widths = result[d].Widths;
                axis.Reset((Dimension)d, outline.Orientation);
                // Widths always use the default script group's segments.
                Topology.ComputeSegments(outline, axis);
                Topology.LinkSegments(outline, axis, 0, ScriptGroup.Default, null);
                var segments = axis.Segments;
                for (var segmentIx = 0; segmentIx < segments.Count; segmentIx++)
                {
                    var segment = segments[segmentIx];
                    if (segment.LinkIx is not { } linkIx)
                        continue;
                    var link = segments[linkIx];
                    if (linkIx > segmentIx && link.LinkIx == segmentIx)
                    {
                        var dist = Math.Abs(segment.Pos - link.Pos);
                        if (widths.Count < MaxWidths)
                            widths.Add(dist);
                        else
                            break;
                    }
                }
                // FreeType's quantizing always leaves one width.
                if (widths.Count == 0)
                    widths.Add(0);
                SortAndQuantizeWidths(widths, unitsPerEm / 100);
            }
        for (var d = 0; d < 2; d++)
        {
            var stdw = result[d].Widths.Count > 0 ? result[d].Widths[0] : DerivedConstant(unitsPerEm, 50);
            result[d].Metrics = new WidthMetrics { EdgeDistanceThreshold = stdw / 5, StandardWidth = stdw, IsExtraLight = false };
        }
        return result;
    }

    static List<UnscaledBlue>[] ComputeUnscaledBlues(Shaper shaper, StyleClass style) => style.Script.Group switch
    {
        ScriptGroup.Default => [[], ComputeDefaultBlues(shaper, style)],
        ScriptGroup.Cjk => ComputeCjkBlues(shaper, style),
        _ => [[], []],
    };

    /// <summary>Contour range and point within it of the last contour holding the best point (skrifa's find_last_contour).</summary>
    static (int Start, int End, int Point)? FindLastContour(List<UnscaledPoint> points, Func<UnscaledPoint, bool> f)
    {
        if (points.Count == 0)
            return null;
        (int Start, int End) best = (0, 0), cur = (0, 0);
        var bestPoint = 0;
        var foundInCur = false;
        for (var ix = 0; ix < points.Count; ix++)
        {
            var point = points[ix];
            if (point.IsContourStart)
            {
                if (foundInCur)
                    best = cur;
                cur = (ix, ix);
                foundInCur = false;
                if (ix + 1 >= points.Count || points[ix + 1].IsContourStart)
                    continue;
            }
            cur.End++;
            if (f(point))
            {
                bestPoint = ix - cur.Start;
                foundInCur = true;
            }
        }
        if (foundInCur)
            best = cur;
        return best.End > best.Start ? (best.Start, best.End, bestPoint) : null;
    }

    static bool SatisfiesMinLongSegmentLen(int firstIx, int lastIx, int contourLast)
    {
        var inclusiveDiff = firstIx <= lastIx ? lastIx - firstIx : contourLast - firstIx + 1 + lastIx;
        return inclusiveDiff + 2 <= contourLast;
    }

    /// <summary>The default script group's blue zones (aflatin.c af_latin_metrics_init_blues).</summary>
    static List<UnscaledBlue> ComputeDefaultBlues(Shaper shaper, StyleClass style)
    {
        var blues = new List<UnscaledBlue>();
        var font = shaper.Font;
        var unitsPerEm = font.UnitsPerEm;
        var flatThreshold = unitsPerEm / 14;
        var clusterShaper = shaper.ForStyle(style);
        var shaped = new List<ShapedGlyph>();
        var points = new List<UnscaledPoint>();
        var flats = new List<int>();
        var rounds = new List<int>();
        foreach (var (blueStr, blueZones) in style.Script.Blues)
        {
            var ascender = int.MinValue;
            var descender = int.MaxValue;
            flats.Clear();
            rounds.Clear();
            foreach (var cluster in blueStr.Split(' '))
            {
                var bestYExtremum = blueZones.IsTop() ? int.MinValue : int.MaxValue;
                var bestIsRound = false;
                clusterShaper.Shape(cluster, shaped);
                foreach (var glyph in shaped)
                {
                    if (glyph.Id == 0)
                        continue;
                    var yOffset = glyph.YOffset;
                    points.Clear();
                    if (!font.DrawUnscaled(glyph.Id, points, out _))
                        continue;
                    // Glyphs that can't render anything.
                    if (points.Count <= 2)
                        continue;
                    int? bestYOrNull = null;
                    (int Start, int End, int Point)? found;
                    if (blueZones.IsTopLike())
                        found = FindLastContour(points, point =>
                        {
                            if (bestYOrNull is null || point.Y > bestYOrNull)
                            {
                                bestYOrNull = point.Y;
                                ascender = Math.Max(ascender, point.Y + yOffset);
                                return true;
                            }
                            descender = Math.Min(descender, point.Y + yOffset);
                            return false;
                        });
                    else
                        found = FindLastContour(points, point =>
                        {
                            if (bestYOrNull is null || point.Y < bestYOrNull)
                            {
                                bestYOrNull = point.Y;
                                descender = Math.Min(descender, point.Y + yOffset);
                                return true;
                            }
                            ascender = Math.Max(ascender, point.Y + yOffset);
                            return false;
                        });
                    if (found is not { } contourAndPoint)
                        continue;
                    var contour = points.GetRange(contourAndPoint.Start, contourAndPoint.End - contourAndPoint.Start);
                    var len = contour.Count;
                    var bestPointIx = contourAndPoint.Point;
                    var bestY = bestYOrNull!.Value;
                    var bestX = contour[bestPointIx].X;
                    // Straight or round: look at the previous and next points off the extremum's y.
                    int? onPointFirst = contour[bestPointIx].IsOnCurve ? bestPointIx : null;
                    var onPointLast = onPointFirst;
                    var segmentFirst = bestPointIx;
                    var segmentLast = bestPointIx;
                    for (var k = len - 1; k >= 0; k--)
                    {
                        var ix = (k + bestPointIx) % len;
                        var prev = contour[ix];
                        var dist = Math.Abs(prev.Y - bestY);
                        // A small distance or angle (20 is about 2.9 degrees).
                        if (dist > 5 && Math.Abs(prev.X - bestX) <= 20 * dist)
                            break;
                        segmentFirst = ix;
                        if (prev.IsOnCurve)
                        {
                            onPointFirst = ix;
                            onPointLast ??= ix;
                        }
                    }
                    var nextIx = 0;
                    for (var k = 0; k < len; k++)
                    {
                        var ix = (k + bestPointIx + 1) % len;
                        var next = contour[ix];
                        nextIx = ix;
                        var dist = Math.Abs(next.Y - bestY);
                        if (dist > 5 && Math.Abs(next.X - bestX) <= 20 * dist)
                            break;
                        segmentLast = ix;
                        if (next.IsOnCurve)
                        {
                            onPointLast = ix;
                            onPointFirst ??= ix;
                        }
                    }
                    if (blueZones.IsLong())
                    {
                        // Long blues need a long enough segment near the extremum (Hebrew's vertical serifs).
                        var lengthThreshold = unitsPerEm / 25;
                        var dist = Math.Abs(contour[segmentLast].X - contour[segmentFirst].X);
                        if (dist < lengthThreshold && SatisfiesMinLongSegmentLen(segmentFirst, segmentLast, len - 1))
                        {
                            var heightThreshold = unitsPerEm / 4;
                            // The previous point with a different x.
                            var prevIx = bestPointIx;
                            for (var k = len - 1; k >= 0; k--)
                            {
                                var ix = (k + bestPointIx) % len;
                                if (contour[ix].X != bestX)
                                {
                                    prevIx = ix;
                                    break;
                                }
                            }
                            // The degenerate case.
                            if (prevIx == bestPointIx)
                                continue;
                            var isLtr = contour[prevIx].X < bestX;
                            var first = segmentLast;
                            var last = first;
                            int? pFirst = null, pLast = null;
                            var hit = false;
                            while (true)
                            {
                                if (!hit)
                                {
                                    first = last;
                                    if (contour[first].IsOnCurve)
                                    {
                                        pFirst = first;
                                        pLast = first;
                                    }
                                    else
                                    {
                                        pFirst = null;
                                        pLast = null;
                                    }
                                    hit = true;
                                }
                                last = last < len - 1 ? last + 1 : 0;
                                if (Math.Abs(bestY - contour[first].Y) > heightThreshold)
                                {
                                    hit = false;
                                    continue;
                                }
                                var d2 = Math.Abs(contour[last].Y - contour[first].Y);
                                if (d2 > 5 && Math.Abs(contour[last].X - contour[first].X) <= 20 * d2)
                                {
                                    hit = false;
                                    if (last == segmentFirst)
                                        break;
                                    continue;
                                }
                                if (contour[last].IsOnCurve)
                                {
                                    pLast = last;
                                    pFirst ??= last;
                                }
                                var firstX = contour[first].X;
                                var lastX = contour[last].X;
                                var isCurLtr = firstX < lastX;
                                var dx = Math.Abs(lastX - firstX);
                                if (isCurLtr == isLtr && dx >= lengthThreshold)
                                {
                                    while (true)
                                    {
                                        last = last < len - 1 ? last + 1 : 0;
                                        var dy = Math.Abs(contour[last].Y - contour[first].Y);
                                        if (dy > 5 && Math.Abs(contour[nextIx].X - contour[first].X) <= 20 * d2)
                                        {
                                            last = last > 0 ? last - 1 : len - 1;
                                            break;
                                        }
                                        pLast = last;
                                        if (contour[last].IsOnCurve)
                                        {
                                            pLast = last;
                                            pFirst ??= last;
                                        }
                                        if (last == segmentFirst)
                                            break;
                                    }
                                    bestY = contour[first].Y;
                                    segmentFirst = first;
                                    segmentLast = last;
                                    onPointFirst = pFirst;
                                    onPointLast = pLast;
                                    break;
                                }
                                if (last == segmentFirst)
                                    break;
                            }
                        }
                    }
                    bestY += yOffset;
                    // Flat when the on-curve points span more than the flat threshold, else round when an end is a control point.
                    bool isRound;
                    if (onPointFirst is { } opf && onPointLast is { } opl && Math.Abs(contour[opl].X - contour[opf].X) > flatThreshold)
                        isRound = false;
                    else
                        isRound = !contour[segmentFirst].IsOnCurve || !contour[segmentLast].IsOnCurve;
                    // Neutral zones ignore round segments.
                    if (isRound && blueZones.IsNeutral())
                        continue;
                    if (blueZones.IsTop())
                    {
                        if (bestY > bestYExtremum)
                        {
                            bestYExtremum = bestY;
                            bestIsRound = isRound;
                        }
                    }
                    else if (bestY < bestYExtremum)
                    {
                        bestYExtremum = bestY;
                        bestIsRound = isRound;
                    }
                }
                if (bestYExtremum != int.MinValue && bestYExtremum != int.MaxValue)
                    (bestIsRound ? rounds : flats).Add(bestYExtremum);
            }
            if (flats.Count == 0 && rounds.Count == 0)
                continue;
            rounds.Sort();
            flats.Sort();
            int blueRef, blueShoot;
            if (flats.Count == 0)
                blueRef = blueShoot = rounds[rounds.Count / 2];
            else if (rounds.Count == 0)
                blueRef = blueShoot = flats[flats.Count / 2];
            else
            {
                blueRef = flats[flats.Count / 2];
                blueShoot = rounds[rounds.Count / 2];
            }
            if (blueShoot != blueRef)
            {
                var overRef = blueShoot > blueRef;
                if (blueZones.IsTopLike() ^ overRef)
                    blueRef = blueShoot = (blueShoot + blueRef) / 2;
            }
            var blue = new UnscaledBlue(blueRef, blueShoot, ascender, descender, blueZones.RetainTopLikeOrNeutral());
            if (blueZones.IsXHeight())
                blue.Zones |= BlueZones.Adjustment;
            blues.Add(blue);
        }
        if (blues.Count == 0)
            return blues;
        // Sort from bottom to top.
        var sorted = new int[Math.Max(MaxBlues, blues.Count)];
        for (var i = 0; i < sorted.Length; i++)
            sorted[i] = i;
        var count = blues.Count;
        for (var i = 1; i < count; i++)
            for (var j = i; j >= 1; j--)
            {
                var first = blues[sorted[j - 1]];
                var second = blues[sorted[j]];
                var a = first.Zones.IsTopLike() ? first.Position : first.Overshoot;
                var b = second.Zones.IsTopLike() ? second.Position : second.Overshoot;
                if (b >= a)
                    break;
                (sorted[j], sorted[j - 1]) = (sorted[j - 1], sorted[j]);
            }
        // And adjust the tops.
        for (var i = 0; i < count - 1; i++)
        {
            var index1 = sorted[i];
            var index2 = sorted[i + 1];
            var first = blues[index1];
            var second = blues[index2];
            var a = first.Zones.IsTopLike() ? first.Overshoot : first.Position;
            var b = second.Zones.IsTopLike() ? second.Overshoot : second.Position;
            if (a > b)
            {
                if (first.Zones.IsTopLike())
                    first.Overshoot = b;
                else
                    first.Position = b;
                blues[index1] = first;
            }
        }
        return blues;
    }

    /// <summary>The CJK group's blue zones (afcjk.c af_cjk_metrics_init_blues; horizontal ones stay off, as in FreeType).</summary>
    static List<UnscaledBlue>[] ComputeCjkBlues(Shaper shaper, StyleClass style)
    {
        List<UnscaledBlue>[] result = [[], []];
        var font = shaper.Font;
        var clusterShaper = shaper.ForStyle(style);
        var shaped = new List<ShapedGlyph>();
        var points = new List<UnscaledPoint>();
        var flats = new List<int>();
        var fills = new List<int>();
        foreach (var (blueStr, blueZones) in style.Script.Blues)
        {
            var isHorizontal = blueZones.IsHorizontal();
            if (isHorizontal)
                continue;
            var isRight = blueZones.IsRight();
            var isTop = blueZones.IsTop();
            var blues = result[isHorizontal ? 0 : 1];
            if (blues.Count >= MaxBlues)
                continue;
            flats.Clear();
            fills.Clear();
            var isFill = true;
            foreach (var cluster in blueStr.Split(' '))
            {
                // '|' switches to the characters that give flat values.
                if (cluster == "|")
                {
                    isFill = false;
                    continue;
                }
                clusterShaper.Shape(cluster, shaped);
                foreach (var glyph in shaped)
                {
                    if (glyph.Id == 0)
                        continue;
                    points.Clear();
                    if (!font.DrawUnscaled(glyph.Id, points, out _) || points.Count <= 2)
                        continue;
                    var best = isHorizontal ? points[0].X : points[0].Y;
                    var takeMax = (isHorizontal && isRight) || (!isHorizontal && isTop);
                    foreach (var p in points)
                    {
                        var v = isHorizontal ? p.X : p.Y;
                        best = takeMax ? Math.Max(best, v) : Math.Min(best, v);
                    }
                    (isFill ? fills : flats).Add(best);
                }
            }
            if (flats.Count == 0 && fills.Count == 0)
                continue;
            fills.Sort();
            flats.Sort();
            int blueRef, blueShoot;
            if (flats.Count == 0)
                blueRef = blueShoot = fills[fills.Count / 2];
            else if (fills.Count == 0)
                blueRef = blueShoot = flats[flats.Count / 2];
            else
            {
                blueRef = fills[fills.Count / 2];
                blueShoot = flats[flats.Count / 2];
            }
            if (blueShoot != blueRef)
            {
                var underRef = blueShoot < blueRef;
                if (blueZones.IsTop() ^ underRef)
                    blueRef = blueShoot = (blueShoot + blueRef) / 2;
            }
            blues.Add(new UnscaledBlue(blueRef, blueShoot, 0, 0, blueZones & BlueZones.Top));
        }
        return result;
    }

    /// <summary>Scales a style's metrics, possibly adjusting the vertical scale (aflatin.c af_latin_metrics_scale).</summary>
    public static ScaledStyleMetrics ScaleStyleMetrics(UnscaledStyleMetrics unscaled, Scale scale, QuirksMode quirks)
    {
        var isDefault = unscaled.StyleClass.Script.Group == ScriptGroup.Default;
        ScaledAxisMetrics Axis(UnscaledAxisMetrics a) => isDefault
            ? ScaleDefaultAxisMetrics(a.Dim, a.Widths, a.WidthMetrics, a.Blues, ref scale, quirks)
            : ScaleCjkAxisMetrics(a.Dim, a.Widths, a.WidthMetrics, a.Blues, scale);
        var horizontal = Axis(unscaled.Axes[0]);
        var vertical = Axis(unscaled.Axes[1]);
        return new ScaledStyleMetrics { Scale = scale, Axes = [horizontal, vertical] };
    }

    static ScaledAxisMetrics ScaleDefaultAxisMetrics(Dimension dim, List<int> widths, WidthMetrics widthMetrics, List<UnscaledBlue> blues, ref Scale scale, QuirksMode quirks)
    {
        var axis = new ScaledAxisMetrics { Dim = dim };
        if (dim == Dimension.Horizontal)
        {
            axis.Scale = scale.XScale;
            axis.Delta = scale.XDelta;
        }
        else
        {
            axis.Scale = scale.YScale;
            axis.Delta = scale.YDelta;
        }
        // Correct the y scale to align the x-height.
        var adjustment = blues.FindIndex(b => (b.Zones & BlueZones.Adjustment) == BlueZones.Adjustment);
        if (adjustment >= 0)
        {
            var unscaledBlue = blues[adjustment];
            var scaled = Fixed.Mul(axis.Scale, unscaledBlue.Overshoot);
            var fitted = (scaled + 40) & ~63;
            if (scaled != fitted && dim == Dimension.Vertical)
            {
                var newScale = Fixed.MulDiv(axis.Scale, fitted, scaled);
                // At most 2 pixels of change.
                var maxHeight = scale.UnitsPerEm;
                foreach (var blue in blues)
                    maxHeight = Math.Max(Math.Max(maxHeight, blue.Ascender), -blue.Descender);
                var dist = Math.Abs(Fixed.Mul(maxHeight, newScale - axis.Scale));
                dist &= ~127;
                if (dist == 0)
                {
                    axis.Scale = newScale;
                    scale.YScale = newScale;
                }
            }
        }
        // The widths (FreeType always has at least the standard width).
        axis.WidthMetrics = widthMetrics;
        if (widths.Count == 0 && quirks == QuirksMode.Aot)
        {
            var scaled = Fixed.Mul(axis.Scale, axis.WidthMetrics.StandardWidth);
            axis.Widths.Add(new ScaledWidth(scaled, scaled));
        }
        else
            foreach (var unscaledWidth in widths)
            {
                var scaled = Fixed.Mul(axis.Scale, unscaledWidth);
                axis.Widths.Add(new ScaledWidth(scaled, scaled));
            }
        // Extra light: a standard width under 5/8 pixel.
        axis.WidthMetrics.IsExtraLight = Fixed.Mul(axis.WidthMetrics.StandardWidth, axis.Scale) < 32 + 8;
        if (dim == Dimension.Vertical)
        {
            foreach (var unscaledBlue in blues)
            {
                var scaledPosition = Fixed.Mul(axis.Scale, unscaledBlue.Position) + axis.Delta;
                var scaledOvershoot = Fixed.Mul(axis.Scale, unscaledBlue.Overshoot) + axis.Delta;
                var blue = new ScaledBlue(new ScaledWidth(scaledPosition, scaledPosition), new ScaledWidth(scaledOvershoot, scaledOvershoot), unscaledBlue.Zones, false);
                // Only zones under 3/4 pixel tall are active.
                var dist = Fixed.Mul(unscaledBlue.Position - unscaledBlue.Overshoot, axis.Scale);
                if (dist >= -48 && dist <= 48)
                {
                    var delta = Math.Abs(dist);
                    delta = delta < 32 ? 0 : delta < 48 ? 32 : 64;
                    if (dist < 0)
                        delta = -delta;
                    blue.Position = blue.Position with { Fitted = Fixed.PixRound(blue.Position.Scaled) };
                    blue.Overshoot = blue.Overshoot with { Fitted = blue.Position.Fitted - delta };
                    blue.IsActive = true;
                }
                axis.Blues.Add(blue);
            }
            // A sub-top zone stays only when it doesn't overlap another active zone.
            for (var blueIx = 0; blueIx < axis.Blues.Count; blueIx++)
            {
                var blue = axis.Blues[blueIx];
                if (!blue.Zones.IsSubTop() || !blue.IsActive)
                    continue;
                foreach (var blue2 in axis.Blues)
                {
                    if (blue2.Zones.IsSubTop() || !blue2.IsActive)
                        continue;
                    if (blue2.Position.Fitted <= blue.Overshoot.Fitted && blue2.Overshoot.Fitted >= blue.Position.Fitted)
                    {
                        axis.Blues[blueIx] = blue with { IsActive = false };
                        break;
                    }
                }
            }
        }
        return axis;
    }

    static ScaledAxisMetrics ScaleCjkAxisMetrics(Dimension dim, List<int> widths, WidthMetrics widthMetrics, List<UnscaledBlue> blues, Scale scale)
    {
        var axis = new ScaledAxisMetrics { Dim = dim };
        if (dim == Dimension.Horizontal)
        {
            axis.Scale = scale.XScale;
            axis.Delta = scale.XDelta;
        }
        else
        {
            axis.Scale = scale.YScale;
            axis.Delta = scale.YDelta;
        }
        var s = axis.Scale;
        foreach (var unscaledBlue in blues)
        {
            var position = Fixed.Mul(unscaledBlue.Position, s) + axis.Delta;
            var overshoot = Fixed.Mul(unscaledBlue.Overshoot, s) + axis.Delta;
            var blue = new ScaledBlue(new ScaledWidth(position, position), new ScaledWidth(overshoot, overshoot), unscaledBlue.Zones, false);
            var dist = Fixed.Mul(unscaledBlue.Position - unscaledBlue.Overshoot, s);
            if (dist >= -48 && dist <= 48)
            {
                blue.Position = blue.Position with { Fitted = Fixed.PixRound(blue.Position.Scaled) };
                // For CJK the overshoot is an undershoot.
                var delta1 = Fixed.Div(blue.Position.Fitted, s) - unscaledBlue.Overshoot;
                var delta2 = Fixed.Mul(Math.Abs(delta1), s);
                delta2 = delta2 < 32 ? 0 : Fixed.PixRound(delta2);
                if (delta1 < 0)
                    delta2 = -delta2;
                blue.Overshoot = blue.Overshoot with { Fitted = blue.Position.Fitted - delta2 };
                blue.IsActive = true;
            }
            axis.Blues.Add(blue);
        }
        // FreeType never scales CJK widths.
        foreach (var _ in widths)
            axis.Widths.Add(default);
        axis.WidthMetrics = widthMetrics;
        return axis;
    }
}

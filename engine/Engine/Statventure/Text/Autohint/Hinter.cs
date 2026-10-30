namespace Statventure.Text.Autohint;

/// <summary>Where the horizontal hinting moved the outermost edges, for hinted advances.</summary>
internal readonly record struct EdgeMetrics(int LeftOpos, int LeftPos, int RightOpos, int RightPos);

internal readonly record struct HintedMetrics(int XScale, EdgeMetrics? EdgeMetrics);

/// <summary>
/// Hints an outline (skrifa's autohint hint module: FreeType's af_latin_hints_apply and
/// af_cjk_hints_apply): segments and edges per dimension, edges fitted to blue zones and stem
/// widths, then the points moved with their edges and interpolated between them.
/// </summary>
internal static class Hinter
{
    public static HintedMetrics HintOutline(Outline outline, UnscaledStyleMetrics metrics, Scale initialScale, GlyphStyle? glyphStyle)
    {
        const QuirksMode quirks = QuirksMode.Jit;
        var scaledMetrics = MetricsComputer.ScaleStyleMetrics(metrics, initialScale, quirks);
        var scale = scaledMetrics.Scale;
        var axis = new Axis();
        var hintTopToBottom = metrics.StyleClass.Script.HintTopToBottom;
        outline.Scale(scale);
        var hinted = new HintedMetrics(scale.XScale, null);
        var group = metrics.StyleClass.Script.Group;
        // FreeType swaps in a dummy hinter for the default group without blue zones.
        if (group == ScriptGroup.Default && scaledMetrics.Axes[1].Blues.Count == 0)
            return hinted;
        for (var d = 0; d < 2; d++)
        {
            var dim = (Dimension)d;
            if ((dim == Dimension.Horizontal && (scale.Flags & ScaleFlags.NoHorizontal) != 0)
                || (dim == Dimension.Vertical && (scale.Flags & ScaleFlags.NoVertical) != 0))
                continue;
            axis.Reset(dim, outline.Orientation);
            Topology.ComputeSegments(outline, axis);
            var unscaledAxis = metrics.Axes[d];
            Topology.LinkSegments(outline, axis, scaledMetrics.Axes[d].Scale, group, unscaledAxis.Widths.Count > 0 ? unscaledAxis.Widths[^1] : null);
            Topology.ComputeEdges(axis, scaledMetrics.Axes[d], hintTopToBottom, scale.YScale, group);
            if (dim == Dimension.Vertical)
            {
                if (group != ScriptGroup.Default || glyphStyle is not { IsNonBase: true })
                    Topology.ComputeBlueEdges(axis, scale, unscaledAxis.Blues, scaledMetrics.Axes[d].Blues, group);
            }
            else
                hinted = hinted with { XScale = scaledMetrics.Axes[0].Scale };
            HintEdges(axis, scaledMetrics.Axes[d], group, scale, hintTopToBottom);
            AlignEdgePoints(outline, axis, group, scale);
            AlignStrongPoints(outline, axis);
            AlignWeakPoints(outline, dim);
            if (dim == Dimension.Horizontal && axis.Edges.Count > 1)
            {
                var left = axis.Edges[0];
                var right = axis.Edges[^1];
                hinted = hinted with { EdgeMetrics = new EdgeMetrics(left.Opos, left.Pos, right.Opos, right.Pos) };
            }
        }
        return hinted;
    }

    // ---------- Edges ----------

    static void SetPos(Axis axis, int ix, int pos)
    {
        var e = axis.Edges[ix];
        e.Pos = pos;
        axis.Edges[ix] = e;
    }

    static void AddFlags(Axis axis, int ix, TopoFlags flags)
    {
        var e = axis.Edges[ix];
        e.Flags |= flags;
        axis.Edges[ix] = e;
    }

    static bool IsDone(Edge e) => (e.Flags & TopoFlags.Done) != 0;

    static void HintEdges(Axis axis, ScaledAxisMetrics metrics, ScriptGroup group, in Scale scale, bool topToBottomHinting)
    {
        if (axis.Dim != Dimension.Vertical)
            topToBottomHinting = false;
        var anchorIx = AlignEdgesToBlues(axis, metrics, group, scale);
        (var serifCount, anchorIx) = AlignStemEdges(axis, metrics, group, scale, topToBottomHinting, anchorIx);
        if (axis.Dim == Dimension.Horizontal && axis.Edges.Count is 6 or 12)
            HintLowercaseM(axis.Edges, group);
        if (serifCount > 0 || anchorIx is null)
            AlignRemainingEdges(axis, group, topToBottomHinting, serifCount, anchorIx);
    }

    static int? AlignEdgesToBlues(Axis axis, ScaledAxisMetrics metrics, ScriptGroup group, in Scale scale)
    {
        int? anchorIx = null;
        // The default group aligns only vertical blues.
        if (group == ScriptGroup.Default && axis.Dim != Dimension.Vertical)
            return anchorIx;
        var edges = axis.Edges;
        for (var edgeIx = 0; edgeIx < edges.Count; edgeIx++)
        {
            var edge = edges[edgeIx];
            if (IsDone(edge))
                continue;
            var edge2Ix = edge.LinkIx;
            // Two neutral zones: skip one.
            if (edge.BlueEdge is not null && edge2Ix is { } linked && edges[linked].BlueEdge is not null)
            {
                int? skipIx = (edges[linked].Flags & TopoFlags.Neutral) != 0 ? linked : (edge.Flags & TopoFlags.Neutral) != 0 ? edgeIx : null;
                if (skipIx is { } skip)
                {
                    var s = edges[skip];
                    s.BlueEdge = null;
                    s.Flags &= ~TopoFlags.Neutral;
                    edges[skip] = s;
                }
            }
            // Flip the edges when the linked one is the one aligned to a blue zone.
            ScaledWidth blue;
            int edge1Ix;
            int? otherIx;
            if (edges[edgeIx].BlueEdge is { } own)
                (blue, edge1Ix, otherIx) = (own, edgeIx, edge2Ix);
            else if (edge2Ix is { } e2 && edges[e2].BlueEdge is { } theirs)
                (blue, edge1Ix, otherIx) = (theirs, e2, edgeIx);
            else
                continue;
            // Already placed through an earlier edge.
            if (IsDone(edges[edge1Ix]))
                continue;
            var edge1 = edges[edge1Ix];
            edge1.Pos = blue.Fitted;
            edge1.Flags |= TopoFlags.Done;
            edges[edge1Ix] = edge1;
            if (otherIx is { } other && edges[other].BlueEdge is null)
            {
                AddFlags(axis, other, TopoFlags.Done);
                AlignLinkedEdge(axis, metrics, group, scale, edge1Ix, other);
            }
            anchorIx ??= edgeIx;
        }
        return anchorIx;
    }

    static (int SerifCount, int? AnchorIx) AlignStemEdges(Axis axis, ScaledAxisMetrics metrics, ScriptGroup group, in Scale scale, bool topToBottomHinting, int? anchorIx)
    {
        var serifCount = 0;
        int? lastStemPos = null;
        var delta = 0;
        var edges = axis.Edges;
        for (var edgeIx = 0; edgeIx < edges.Count; edgeIx++)
        {
            var edgeSnapshot = edges[edgeIx];
            if (IsDone(edgeSnapshot))
                continue;
            // Edges without a link aren't stems.
            if (edgeSnapshot.LinkIx is not { } edge2Ix)
            {
                serifCount++;
                continue;
            }
            var edge2Pos = edges[edge2Ix].Pos;
            var edge2HasBlue = edges[edge2Ix].BlueEdge is not null;
            // CJK: stems too close are dealt with later.
            if (group != ScriptGroup.Default && lastStemPos is { } lastPos && (edgeSnapshot.Pos < lastPos + 64 || edge2Pos < lastPos + 64))
            {
                serifCount++;
                continue;
            }
            // (Shouldn't happen; FreeType's fallback.)
            if (edge2HasBlue)
            {
                AlignLinkedEdge(axis, metrics, group, scale, edge2Ix, edgeIx);
                AddFlags(axis, edgeIx, TopoFlags.Done);
                continue;
            }
            if (group == ScriptGroup.Default)
            {
                if (anchorIx is { } anchorAt)
                {
                    var anchor = edges[anchorAt];
                    var edge = edges[edgeIx];
                    var edge2 = edges[edge2Ix];
                    var originalPos = anchor.Pos + (edge.Opos - anchor.Opos);
                    var originalLen = edge2.Opos - edge.Opos;
                    var originalCenter = originalPos + (originalLen >> 1);
                    var curLen = StemWidth(metrics, group, scale, originalLen, 0, edge.Flags, edge2.Flags);
                    if (IsDone(edge2))
                        SetPos(axis, edgeIx, edge2.Pos - curLen);
                    else if (curLen < 96)
                    {
                        var curPos1 = Fixed.PixRound(originalCenter);
                        var (uOff, dOff) = curLen <= 64 ? (32, 32) : (38, 26);
                        var delta1 = Math.Abs(originalCenter - (curPos1 - uOff));
                        var delta2 = Math.Abs(originalCenter - (curPos1 + dOff));
                        curPos1 = delta1 < delta2 ? curPos1 - uOff : curPos1 + dOff;
                        SetPos(axis, edgeIx, curPos1 - curLen / 2);
                        SetPos(axis, edge2Ix, curPos1 + curLen / 2);
                    }
                    else
                    {
                        var curPos1 = Fixed.PixRound(originalPos);
                        var delta1 = Math.Abs(curPos1 + (curLen >> 1) - originalCenter);
                        var curPos2 = Fixed.PixRound(originalPos + originalLen) - curLen;
                        var delta2 = Math.Abs(curPos2 + (curLen >> 1) - originalCenter);
                        var newPos = delta1 < delta2 ? curPos1 : curPos2;
                        SetPos(axis, edgeIx, newPos);
                        SetPos(axis, edge2Ix, newPos + curLen);
                    }
                    AddFlags(axis, edgeIx, TopoFlags.Done);
                    AddFlags(axis, edge2Ix, TopoFlags.Done);
                    if (edgeIx > 0)
                        AdjustLink(edges, edgeIx, prev: true, topToBottomHinting);
                }
                else
                {
                    // No stem aligned yet.
                    var edge = edges[edgeIx];
                    var edge2 = edges[edge2Ix];
                    var originalLen = edge2.Opos - edge.Opos;
                    var curLen = StemWidth(metrics, group, scale, originalLen, 0, edge.Flags, edge2.Flags);
                    // Rounding for small stems: up to 1 pixel, and up to 1.5 pixels.
                    var (uOff, dOff) = curLen <= 64 ? (32, 32) : (38, 26);
                    if (curLen < 96)
                    {
                        var originalCenter = edge.Opos + (originalLen >> 1);
                        var curPos1 = Fixed.PixRound(originalCenter);
                        var error1 = Math.Abs(originalCenter - (curPos1 - uOff));
                        var error2 = Math.Abs(originalCenter - (curPos1 + dOff));
                        curPos1 = error1 < error2 ? curPos1 - uOff : curPos1 + dOff;
                        var edgePos = curPos1 - curLen / 2;
                        SetPos(axis, edgeIx, edgePos);
                        SetPos(axis, edge2Ix, edgePos + curLen);
                    }
                    else
                        SetPos(axis, edgeIx, Fixed.PixRound(edge.Opos));
                    AddFlags(axis, edgeIx, TopoFlags.Done);
                    AlignLinkedEdge(axis, metrics, group, scale, edgeIx, edge2Ix);
                    anchorIx = edgeIx;
                }
            }
            else
            {
                // CJK.
                if (edge2Ix < edgeIx)
                {
                    lastStemPos = edgeSnapshot.Pos;
                    AddFlags(axis, edgeIx, TopoFlags.Done);
                    AlignLinkedEdge(axis, metrics, group, scale, edge2Ix, edgeIx);
                    continue;
                }
                if (axis.Dim != Dimension.Vertical && anchorIx is null)
                    delta = HintNormalStemCjk(axis, metrics, group, scale, edgeIx, edge2Ix, delta);
                else
                    HintNormalStemCjk(axis, metrics, group, scale, edgeIx, edge2Ix, delta);
                anchorIx = edgeIx;
                AddFlags(axis, edgeIx, TopoFlags.Done);
                AddFlags(axis, edge2Ix, TopoFlags.Done);
                lastStemPos = edges[edge2Ix].Pos;
            }
        }
        return (serifCount, anchorIx);
    }

    static void HintLowercaseM(List<Edge> edges, ScriptGroup group)
    {
        var (edge1Ix, edge2Ix, edge3Ix) = edges.Count == 6 ? (0, 2, 4) : (1, 5, 9);
        var edge1 = edges[edge1Ix];
        var edge2 = edges[edge2Ix];
        var edge3 = edges[edge3Ix];
        var dist1 = edge2.Opos - edge1.Opos;
        var dist2 = edge3.Opos - edge2.Opos;
        var span = Math.Abs(dist1 - dist2);
        if (group != ScriptGroup.Default)
            foreach (var (e, ix) in new[] { (edge1, edge1Ix), (edge2, edge2Ix), (edge3, edge3Ix) })
                if (e.LinkIx != ix + 1)
                    return;
        if (span >= 8)
            return;
        var delta = edge3.Pos - (2 * edge2.Pos - edge1.Pos);
        var linkIx = edge3.LinkIx;
        edge3.Pos -= delta;
        edge3.Flags |= TopoFlags.Done;
        edges[edge3Ix] = edge3;
        if (linkIx is { } l)
        {
            var link = edges[l];
            link.Pos -= delta;
            link.Flags |= TopoFlags.Done;
            edges[l] = link;
        }
        // Serifs move with the stem.
        if (edges.Count == 12)
        {
            var e8 = edges[8];
            e8.Pos -= delta;
            edges[8] = e8;
            var e11 = edges[11];
            e11.Pos -= delta;
            edges[11] = e11;
        }
    }

    static void AlignRemainingEdges(Axis axis, ScriptGroup group, bool topToBottomHinting, int serifCount, int? anchorIx)
    {
        var edges = axis.Edges;
        if (group == ScriptGroup.Default)
        {
            for (var edgeIx = 0; edgeIx < edges.Count; edgeIx++)
            {
                var edge = edges[edgeIx];
                if (IsDone(edge))
                    continue;
                var delta = edge.SerifIx is { } s && s < edges.Count ? Math.Abs(edges[s].Opos - edge.Opos) : 1000;
                if (delta < 64 + 16)
                    AlignSerifEdge(axis, edge.SerifIx!.Value, edgeIx);
                else if (anchorIx is { } anchorAt)
                {
                    var (beforeIx, afterIx) = FindBoundingCompletedEdges(edges, edgeIx);
                    if (beforeIx is { } b && afterIx is { } a)
                    {
                        var before = edges[b];
                        var after = edges[a];
                        SetPos(axis, edgeIx, after.Opos == before.Opos
                            ? before.Pos
                            : before.Pos + Fixed.MulDiv(edge.Opos - before.Opos, after.Pos - before.Pos, after.Opos - before.Opos));
                    }
                    else
                    {
                        var anchor = edges[anchorAt];
                        SetPos(axis, edgeIx, anchor.Pos + ((edge.Opos - anchor.Opos + 16) & ~31));
                    }
                }
                else
                {
                    anchorIx = edgeIx;
                    SetPos(axis, edgeIx, Fixed.PixRound(edge.Opos));
                }
                AddFlags(axis, edgeIx, TopoFlags.Done);
                AdjustLink(edges, edgeIx, prev: true, topToBottomHinting);
                AdjustLink(edges, edgeIx, prev: false, topToBottomHinting);
            }
            return;
        }
        for (var edgeIx = 0; edgeIx < edges.Count; edgeIx++)
        {
            var edge = edges[edgeIx];
            if (IsDone(edge))
                continue;
            if (edge.SerifIx is { } serifIx)
            {
                AddFlags(axis, edgeIx, TopoFlags.Done);
                AlignSerifEdge(axis, serifIx, edgeIx);
                serifCount = Math.Max(0, serifCount - 1);
            }
        }
        if (serifCount == 0)
            return;
        for (var edgeIx = 0; edgeIx < edges.Count; edgeIx++)
        {
            var edge = edges[edgeIx];
            if (IsDone(edge))
                continue;
            switch (FindBoundingCompletedEdges(edges, edgeIx))
            {
                case ({ } beforeIx, null):
                    AlignSerifEdge(axis, beforeIx, edgeIx);
                    break;
                case (null, { } afterIx):
                    AlignSerifEdge(axis, afterIx, edgeIx);
                    break;
                case ({ } beforeIx, { } afterIx):
                {
                    var before = edges[beforeIx];
                    var after = edges[afterIx];
                    SetPos(axis, edgeIx, after.Fpos == before.Fpos
                        ? before.Pos
                        : before.Pos + Fixed.MulDiv(edge.Fpos - before.Fpos, after.Pos - before.Pos, after.Fpos - before.Fpos));
                    break;
                }
            }
        }
    }

    /// <summary>An edge that overlaps its neighbour (before it, or after it once that's done) moves onto it when its link is far from the edge before.</summary>
    static void AdjustLink(List<Edge> edges, int edgeIx, bool prev, bool topToBottomHinting)
    {
        var edge = edges[edgeIx];
        Edge edge2, prevEdge;
        if (!prev)
        {
            if (edgeIx + 1 >= edges.Count)
                return;
            edge2 = edges[edgeIx + 1];
            // Not before the next edge is done.
            if (!IsDone(edge2) || edgeIx == 0)
                return;
            prevEdge = edges[edgeIx - 1];
        }
        else
        {
            if (edgeIx == 0)
                return;
            edge2 = prevEdge = edges[edgeIx - 1];
        }
        var pos1 = edge.Pos;
        var pos2 = edge2.Pos;
        var orderCheck = prev == topToBottomHinting ? pos1 > pos2 : pos1 < pos2;
        if (!orderCheck || edge.LinkIx is not { } linkIx || linkIx >= edges.Count)
            return;
        if (Math.Abs(edges[linkIx].Pos - prevEdge.Pos) > 16)
        {
            edge.Pos = edge2.Pos;
            edges[edgeIx] = edge;
        }
    }

    static (int? Before, int? After) FindBoundingCompletedEdges(List<Edge> edges, int ix)
    {
        int? before = null, after = null;
        for (var i = Math.Min(ix, edges.Count) - 1; i >= 0; i--)
            if (IsDone(edges[i]))
            {
                before = i;
                break;
            }
        for (var i = ix + 1; i < edges.Count; i++)
            if (IsDone(edges[i]))
            {
                after = i;
                break;
            }
        return (before, after);
    }

    static int SnapWidth(List<ScaledWidth> widths, int width)
    {
        var bestDist = 64 + 32 + 2;
        var refWidth = width;
        foreach (var candidate in widths)
        {
            var dist = Math.Abs(width - candidate.Scaled);
            if (dist < bestDist)
            {
                bestDist = dist;
                refWidth = candidate.Scaled;
            }
        }
        var scaled = Fixed.PixRound(refWidth);
        if (width >= refWidth)
            return width < scaled + 48 ? refWidth : width;
        return width > scaled - 48 ? refWidth : width;
    }

    /// <summary>A stem's fitted width (af_latin_compute_stem_width, af_cjk_compute_stem_width).</summary>
    static int StemWidth(ScaledAxisMetrics metrics, ScriptGroup group, in Scale scale, int width, int baseDelta, TopoFlags baseFlags, TopoFlags stemFlags)
    {
        if ((scale.Flags & ScaleFlags.StemAdjust) == 0 || (group == ScriptGroup.Default && metrics.WidthMetrics.IsExtraLight))
            return width;
        var isVertical = metrics.Dim == Dimension.Vertical;
        var sign = width < 0 ? -1 : 1;
        var dist = Math.Abs(width);
        if ((isVertical && (scale.Flags & ScaleFlags.VerticalSnap) == 0) || (!isVertical && (scale.Flags & ScaleFlags.HorizontalSnap) == 0))
        {
            // Smooth hinting.
            if (group == ScriptGroup.Default)
            {
                // Serif widths stay.
                if ((stemFlags & TopoFlags.Serif) != 0 && isVertical && dist < 3 * 64)
                    return dist * sign;
                if ((baseFlags & TopoFlags.Round) != 0)
                {
                    if (dist < 80)
                        dist = 64;
                }
                else if (dist < 56)
                    dist = 56;
            }
            if (metrics.Widths.Count > 0)
            {
                // Compare to the standard width.
                var minWidth = metrics.Widths[0].Scaled;
                if (Math.Abs(dist - minWidth) < 40)
                    return Math.Max(minWidth, 48) * sign;
                if (group == ScriptGroup.Default)
                {
                    if (dist < 3 * 64)
                    {
                        var delta = dist & 63;
                        dist &= -64;
                        if (delta < 10)
                            dist += delta;
                        else if (delta < 32)
                            dist += 10;
                        else if (delta < 54)
                            dist += 54;
                        else
                            dist += delta;
                    }
                    else
                    {
                        var newBaseDelta = 0;
                        if ((width > 0 && baseDelta > 0) || (width < 0 && baseDelta < 0))
                        {
                            if (scale.Size < 10f)
                                newBaseDelta = baseDelta;
                            else if (scale.Size < 30f)
                                newBaseDelta = baseDelta * (int)(30f - scale.Size) / 20;
                        }
                        dist = (dist - Math.Abs(newBaseDelta) + 32) & ~63;
                    }
                }
            }
            if (group != ScriptGroup.Default)
            {
                if (dist < 54)
                    dist += (54 - dist) / 2;
                else if (dist < 3 * 64)
                {
                    var delta = dist & 63;
                    dist &= -64;
                    if (delta < 10)
                        dist += delta;
                    else if (delta < 22)
                        dist += 10;
                    else if (delta < 42)
                        dist += delta;
                    else if (delta < 54)
                        dist += 54;
                    else
                        dist += delta;
                }
            }
        }
        else
        {
            // Strong hinting: whole pixels.
            var originalDist = dist;
            dist = SnapWidth(metrics.Widths, dist);
            if (isVertical)
                dist = dist >= 64 ? (dist + 16) & ~63 : 64;
            else if ((scale.Flags & ScaleFlags.Mono) != 0)
                dist = dist < 64 ? 64 : (dist + 32) & ~63;
            else if (dist < 48)
                dist = (dist + 64) >> 1;
            else if (dist < 128)
            {
                // Integers only when that distorts by under a quarter pixel.
                dist = (dist + 22) & ~63;
                if (group == ScriptGroup.Default && Math.Abs(dist - originalDist) >= 16)
                {
                    dist = originalDist;
                    if (dist < 48)
                        dist = (dist + 64) >> 1;
                }
            }
            else
                dist = (dist + 32) & ~63;
        }
        return dist * sign;
    }

    static void AlignLinkedEdge(Axis axis, ScaledAxisMetrics metrics, ScriptGroup group, in Scale scale, int baseEdgeIx, int stemEdgeIx)
    {
        var baseEdge = axis.Edges[baseEdgeIx];
        var stemEdge = axis.Edges[stemEdgeIx];
        var width = stemEdge.Opos - baseEdge.Opos;
        var baseDelta = baseEdge.Pos - baseEdge.Opos;
        var fitted = StemWidth(metrics, group, scale, width, baseDelta, baseEdge.Flags, stemEdge.Flags);
        SetPos(axis, stemEdgeIx, baseEdge.Pos + fitted);
    }

    static void AlignSerifEdge(Axis axis, int baseEdgeIx, int serifEdgeIx)
    {
        var baseEdge = axis.Edges[baseEdgeIx];
        SetPos(axis, serifEdgeIx, baseEdge.Pos + (axis.Edges[serifEdgeIx].Opos - baseEdge.Opos));
    }

    static int HintNormalStemCjk(Axis axis, ScaledAxisMetrics metrics, ScriptGroup group, in Scale scale, int edgeIx, int edge2Ix, int anchor)
    {
        const int maxHorizontalGap = 9;
        const int maxVerticalGap = 15;
        const int maxDeltaAbs = 14;
        var edge = axis.Edges[edgeIx];
        var edge2 = axis.Edges[edge2Ix];
        var doStemAdjust = (scale.Flags & ScaleFlags.StemAdjust) != 0;
        int thresholdDelta;
        if (doStemAdjust)
            thresholdDelta = 0;
        else
        {
            var gap = axis.Dim == Dimension.Vertical ? maxHorizontalGap : maxVerticalGap;
            thresholdDelta = (edge.Flags & TopoFlags.Round) != 0 && (edge2.Flags & TopoFlags.Round) != 0 ? gap : gap / 3;
        }
        var threshold = 64 - thresholdDelta;
        var originalLen = edge2.Opos - edge.Opos;
        var curLen = StemWidth(metrics, group, scale, originalLen, 0, edge.Flags, edge2.Flags);
        var originalCenter = (edge.Opos + edge2.Opos) / 2 + anchor;
        var curPos1 = originalCenter - curLen / 2;
        var curPos2 = curPos1 + curLen;
        int Finish(int delta)
        {
            if (!doStemAdjust)
                delta = Math.Clamp(delta, -maxDeltaAbs, maxDeltaAbs);
            var adjustment = curPos1 + delta;
            if (edge.Opos < edge2.Opos)
            {
                SetPos(axis, edgeIx, adjustment);
                SetPos(axis, edge2Ix, adjustment + curLen);
            }
            else
            {
                SetPos(axis, edge2Ix, adjustment);
                SetPos(axis, edgeIx, adjustment + curLen);
            }
            return delta;
        }
        var dOff1 = curPos1 - Fixed.PixFloor(curPos1);
        var dOff2 = curPos2 - Fixed.PixFloor(curPos2);
        var result = 0;
        if (dOff1 == 0 || dOff2 == 0)
            return Finish(result);
        var uOff1 = 64 - dOff1;
        var uOff2 = 64 - dOff2;
        if (curLen <= threshold)
        {
            if (dOff2 < curLen)
                result = uOff1 <= dOff2 ? uOff1 : -dOff2;
            return Finish(result);
        }
        if (threshold < 64 && (dOff1 >= threshold || uOff1 >= threshold || dOff2 >= threshold || uOff2 >= threshold))
            return Finish(result);
        var offset = curLen & 63;
        if (offset < 32)
        {
            if (uOff1 <= offset || dOff2 <= offset)
                return Finish(result);
        }
        else
            offset = 64 - threshold;
        dOff1 = threshold - uOff1;
        uOff1 -= offset;
        uOff2 = threshold - dOff2;
        dOff2 -= offset;
        if (dOff1 <= uOff1)
            uOff1 = -dOff1;
        if (dOff2 <= uOff2)
            uOff2 = -dOff2;
        result = Math.Abs(uOff1) <= Math.Abs(uOff2) ? uOff1 : uOff2;
        return Finish(result);
    }

    // ---------- Points ----------

    /// <summary>Moves each segment's points with its edge (af_glyph_hints_align_edge_points).</summary>
    static void AlignEdgePoints(Outline outline, Axis axis, ScriptGroup group, in Scale scale)
    {
        var points = outline.Points;
        var snap = group == ScriptGroup.Default
            || (axis.Dim == Dimension.Horizontal && (scale.Flags & ScaleFlags.HorizontalSnap) != 0)
            || (axis.Dim == Dimension.Vertical && (scale.Flags & ScaleFlags.VerticalSnap) != 0);
        foreach (var segment in axis.Segments)
        {
            if (segment.EdgeIx is not { } edgeIx || edgeIx >= axis.Edges.Count)
                continue;
            var edge = axis.Edges[edgeIx];
            var delta = edge.Pos - edge.Opos;
            var pointIx = segment.FirstIx;
            var lastIx = segment.LastIx;
            while (true)
            {
                if (pointIx >= outline.PointCount)
                    return;
                ref var point = ref points[pointIx];
                if (axis.Dim == Dimension.Horizontal)
                {
                    point.X = snap ? edge.Pos : point.X + delta;
                    point.Flags |= PointFlags.TouchedX;
                }
                else
                {
                    point.Y = snap ? edge.Pos : point.Y + delta;
                    point.Flags |= PointFlags.TouchedY;
                }
                if (pointIx == lastIx)
                    break;
                pointIx = point.NextIx;
            }
        }
    }

    static void StorePoint(ref Point point, Dimension dim, int u)
    {
        if (dim == Dimension.Horizontal)
        {
            point.X = u;
            point.Flags |= PointFlags.TouchedX;
        }
        else
        {
            point.Y = u;
            point.Flags |= PointFlags.TouchedY;
        }
    }

    /// <summary>Moves the strong points that aren't on edges by the edges around them (af_glyph_hints_align_strong_points).</summary>
    static void AlignStrongPoints(Outline outline, Axis axis)
    {
        var edges = axis.Edges;
        if (edges.Count == 0)
            return;
        var dim = axis.Dim;
        var touchFlag = dim == Dimension.Horizontal ? PointFlags.TouchedX : PointFlags.TouchedY;
        var points = outline.Points;
        for (var pointIx = 0; pointIx < outline.PointCount; pointIx++)
        {
            ref var point = ref points[pointIx];
            // Touched points and weak ones (interpolated in the next pass) stay.
            if ((point.Flags & (touchFlag | PointFlags.WeakInterpolation)) != 0)
                continue;
            var (u, ou) = dim == Dimension.Vertical ? (point.Fy, point.Oy) : (point.Fx, point.Ox);
            // Before the first edge?
            var first = edges[0];
            if (first.Fpos - u >= 0)
            {
                StorePoint(ref point, dim, first.Pos - (first.Opos - ou));
                continue;
            }
            // After the last?
            var last = edges[^1];
            if (u - last.Fpos >= 0)
            {
                StorePoint(ref point, dim, last.Pos + (ou - last.Opos));
                continue;
            }
            // The enclosing edges: a linear search for few edges (it matters where edges share a position).
            int minIx;
            var onEdge = false;
            if (edges.Count <= 8)
            {
                minIx = 0;
                for (var i = 0; i < edges.Count; i++)
                    if (edges[i].Fpos >= u)
                    {
                        if (edges[i].Fpos == u)
                        {
                            StorePoint(ref point, dim, edges[i].Pos);
                            onEdge = true;
                        }
                        minIx = i;
                        break;
                    }
            }
            else
            {
                minIx = 0;
                var maxIx = edges.Count;
                while (minIx < maxIx)
                {
                    var midIx = (minIx + maxIx) >> 1;
                    int fpos = edges[midIx].Fpos;
                    if (u < fpos)
                        maxIx = midIx;
                    else if (u > fpos)
                        minIx = midIx + 1;
                    else
                    {
                        StorePoint(ref point, dim, edges[midIx].Pos);
                        onEdge = true;
                        break;
                    }
                }
            }
            if (onEdge || minIx == 0)
                continue;
            var beforeIx = minIx - 1;
            var before = edges[beforeIx];
            int edgeScale;
            if (before.Scale == 0)
            {
                if (minIx >= edges.Count)
                    return;
                var after = edges[minIx];
                edgeScale = Fixed.Div(after.Pos - before.Pos, after.Fpos - before.Fpos);
                before.Scale = edgeScale;
                edges[beforeIx] = before;
            }
            else
                edgeScale = before.Scale;
            StorePoint(ref point, dim, before.Pos + Fixed.Mul(u - before.Fpos, edgeScale));
        }
    }

    /// <summary>Interpolates the untouched points between the touched ones on each contour (af_glyph_hints_align_weak_points).</summary>
    static void AlignWeakPoints(Outline outline, Dimension dim)
    {
        var all = outline.Span;
        PointFlags touch;
        if (dim == Dimension.Horizontal)
        {
            for (var i = 0; i < all.Length; i++)
            {
                all[i].U = all[i].X;
                all[i].V = all[i].Ox;
            }
            touch = PointFlags.TouchedX;
        }
        else
        {
            for (var i = 0; i < all.Length; i++)
            {
                all[i].U = all[i].Y;
                all[i].V = all[i].Oy;
            }
            touch = PointFlags.TouchedY;
        }
        foreach (var contour in outline.Contours)
        {
            var points = all.Slice(contour.First, contour.Last - contour.First + 1);
            var firstTouchedIx = -1;
            for (var i = 0; i < points.Length; i++)
                if ((points[i].Flags & touch) != 0)
                {
                    firstTouchedIx = i;
                    break;
                }
            if (firstTouchedIx < 0)
                continue;
            var lastIx = points.Length - 1;
            var pointIx = firstTouchedIx;
            int lastTouchedIx;
            while (true)
            {
                // Skip touched neighbours.
                while (pointIx < lastIx && (points[pointIx + 1].Flags & touch) != 0)
                    pointIx++;
                lastTouchedIx = pointIx;
                // The next touched point.
                pointIx++;
                var found = false;
                while (pointIx <= lastIx)
                {
                    if ((points[pointIx].Flags & touch) != 0)
                    {
                        found = true;
                        break;
                    }
                    pointIx++;
                }
                if (!found)
                    break;
                IupInterpolate(points, lastTouchedIx + 1, pointIx - 1, lastTouchedIx, pointIx);
            }
            if (lastTouchedIx == firstTouchedIx)
                // Only one touched point.
                IupShift(points, 0, lastIx, firstTouchedIx);
            else
            {
                if (lastTouchedIx < lastIx)
                    IupInterpolate(points, lastTouchedIx + 1, lastIx, lastTouchedIx, firstTouchedIx);
                if (firstTouchedIx > 0)
                    IupInterpolate(points, 0, firstTouchedIx - 1, lastTouchedIx, firstTouchedIx);
            }
        }
        for (var i = 0; i < all.Length; i++)
            if (dim == Dimension.Horizontal)
                all[i].X = all[i].U;
            else
                all[i].Y = all[i].U;
    }

    static void IupShift(Span<Point> points, int p1Ix, int p2Ix, int refIx)
    {
        var reference = points[refIx];
        var delta = reference.U - reference.V;
        if (delta == 0)
            return;
        for (var i = p1Ix; i < refIx; i++)
            points[i].U = points[i].V + delta;
        for (var i = refIx + 1; i <= p2Ix; i++)
            points[i].U = points[i].V + delta;
    }

    static void IupInterpolate(Span<Point> points, int p1Ix, int p2Ix, int ref1Ix, int ref2Ix)
    {
        if (p1Ix > p2Ix)
            return;
        var ref1 = points[ref1Ix];
        var ref2 = points[ref2Ix];
        if (ref1.V > ref2.V)
            (ref1, ref2) = (ref2, ref1);
        int u1 = ref1.U, v1 = ref1.V, u2 = ref2.U, v2 = ref2.V;
        var d1 = u1 - v1;
        var d2 = u2 - v2;
        if (u1 == u2 || v1 == v2)
        {
            for (var i = p1Ix; i <= p2Ix; i++)
                points[i].U = points[i].V <= v1 ? points[i].V + d1 : points[i].V >= v2 ? points[i].V + d2 : u1;
        }
        else
        {
            var scale = Fixed.Div(u2 - u1, v2 - v1);
            for (var i = p1Ix; i <= p2Ix; i++)
                points[i].U = points[i].V <= v1 ? points[i].V + d1 : points[i].V >= v2 ? points[i].V + d2 : u1 + Fixed.Mul(points[i].V - v1, scale);
        }
    }
}

namespace Statventure.Text.Autohint;

internal enum Dimension { Horizontal = 0, Vertical = 1 }

[Flags]
internal enum TopoFlags : byte
{
    Normal = 0,
    Round = 1,
    Serif = 2,
    Done = 4,
    Neutral = 8,
}

/// <summary>A run of points along one direction (FreeType's AF_SegmentRec; positions are 16-bit as there).</summary>
internal struct Segment
{
    public TopoFlags Flags;
    public Direction Dir;
    public short Pos, Delta, MinCoord, MaxCoord, Height;
    public int Score, Len;
    public int? LinkIx, SerifIx;
    public int FirstIx, LastIx;
    public int? EdgeIx, EdgeNextIx;
}

/// <summary>Segments that line up, hinted together (FreeType's AF_EdgeRec).</summary>
internal struct Edge
{
    public short Fpos;
    public int Opos, Pos;
    public TopoFlags Flags;
    public Direction Dir;
    public ScaledWidth? BlueEdge;
    public int? LinkIx, SerifIx;
    public int Scale;
    public int FirstIx, LastIx;
}

/// <summary>One dimension's segments and edges (FreeType's AF_AxisHintsRec).</summary>
internal sealed class Axis
{
    public Dimension Dim;
    public Direction MajorDir;
    public readonly List<Segment> Segments = [];
    public readonly List<Edge> Edges = [];

    public void Reset(Dimension dim, Orientation? orientation)
    {
        Dim = dim;
        MajorDir = (dim, orientation) switch
        {
            (Dimension.Horizontal, Orientation.Clockwise) => Direction.Down,
            (Dimension.Vertical, Orientation.Clockwise) => Direction.Right,
            (Dimension.Horizontal, _) => Direction.Up,
            _ => Direction.Left,
        };
        Segments.Clear();
        Edges.Clear();
    }

    /// <summary>Inserts an edge in position order (minor direction first at equal positions).</summary>
    public void InsertEdge(Edge edge, bool topToBottomHinting)
    {
        Edges.Add(edge);
        if (Edges.Count == 1)
            return;
        var ix = Edges.Count - 1;
        while (ix > 0)
        {
            var prevFpos = Edges[ix - 1].Fpos;
            if ((topToBottomHinting && prevFpos > edge.Fpos) || (!topToBottomHinting && prevFpos < edge.Fpos))
                break;
            // Edges at the same position with the minor direction come before those with the major one.
            if (prevFpos == edge.Fpos && edge.Dir == MajorDir)
                break;
            Edges[ix] = Edges[ix - 1];
            ix--;
        }
        Edges[ix] = edge;
    }

    public void AppendSegmentToEdge(int segmentIx, int edgeIx)
    {
        var edge = Edges[edgeIx];
        var firstIx = edge.FirstIx;
        var lastIx = edge.LastIx;
        edge.LastIx = segmentIx;
        Edges[edgeIx] = edge;
        var segment = Segments[segmentIx];
        segment.EdgeNextIx = firstIx;
        Segments[segmentIx] = segment;
        var last = Segments[lastIx];
        last.EdgeNextIx = segmentIx;
        Segments[lastIx] = last;
    }
}

/// <summary>Segments and edges (skrifa's autohint topo: FreeType's aflatin.c / afcjk.c segment and edge code).</summary>
internal static class Topology
{
    const int MaxScore = 32000;
    const int MinScore = -32000;

    static int DerivedConstant(int unitsPerEm, int value) => value * unitsPerEm / 2048;

    public static bool ComputeSegments(Outline outline, Axis axis)
    {
        AssignPointUvs(outline, axis.Dim);
        if (!BuildSegments(outline, axis))
            return false;
        AdjustSegmentHeights(outline, axis);
        // (FreeType never runs its CJK round segment detection here.)
        return true;
    }

    public static void LinkSegments(Outline outline, Axis axis, int scale, ScriptGroup group, int? maxWidth)
    {
        if (group == ScriptGroup.Default)
            LinkSegmentsDefault(outline, axis, maxWidth);
        else
            LinkSegmentsCjk(outline, axis, scale);
    }

    static void LinkSegmentsDefault(Outline outline, Axis axis, int? maxWidthOrNull)
    {
        var maxWidth = maxWidthOrNull ?? 0;
        var lenThreshold = Math.Max(DerivedConstant(outline.UnitsPerEm, 8), 1);
        var lenScore = DerivedConstant(outline.UnitsPerEm, 6000);
        const int distScore = 3000;
        var segments = axis.Segments;
        for (var ix1 = 0; ix1 < segments.Count; ix1++)
        {
            if (segments[ix1].Dir != axis.MajorDir)
                continue;
            int pos1 = segments[ix1].Pos;
            for (var ix2 = 0; ix2 < segments.Count; ix2++)
            {
                var seg1 = segments[ix1];
                var seg2 = segments[ix2];
                int pos2 = seg2.Pos;
                if (!seg1.Dir.IsOpposite(seg2.Dir) || pos2 <= pos1)
                    continue;
                int min = Math.Max(seg1.MinCoord, seg2.MinCoord);
                int max = Math.Min(seg1.MaxCoord, seg2.MaxCoord);
                var len = max - min;
                if (len < lenThreshold)
                    continue;
                // Demerits for little overlap and for distances beyond the largest stem width.
                var dist = pos2 - pos1;
                int distDemerit;
                if (maxWidth != 0)
                {
                    var delta = (dist << 10) / maxWidth - (1 << 10);
                    distDemerit = delta > 10_000 ? MaxScore : delta > 0 ? delta * delta / distScore : 0;
                }
                else
                    distDemerit = dist;
                var score = distDemerit + lenScore / len;
                if (score < seg1.Score)
                {
                    seg1.Score = score;
                    seg1.LinkIx = ix2;
                    segments[ix1] = seg1;
                }
                if (score < seg2.Score)
                {
                    seg2 = segments[ix2];
                    seg2.Score = score;
                    seg2.LinkIx = ix1;
                    segments[ix2] = seg2;
                }
            }
        }
        // Serifs: segments whose link doesn't link back.
        for (var ix1 = 0; ix1 < segments.Count; ix1++)
        {
            if (segments[ix1].LinkIx is not { } ix2)
                continue;
            var seg2Link = segments[ix2].LinkIx;
            if (seg2Link != ix1)
            {
                var seg1 = segments[ix1];
                seg1.LinkIx = null;
                seg1.SerifIx = seg2Link;
                segments[ix1] = seg1;
            }
        }
    }

    static void LinkSegmentsCjk(Outline outline, Axis axis, int scale)
    {
        var lenThreshold = DerivedConstant(outline.UnitsPerEm, 8);
        var distThreshold = Fixed.Div(64 * 3, scale);
        var segments = axis.Segments;
        for (var ix1 = 0; ix1 < segments.Count; ix1++)
        {
            if (segments[ix1].Dir != axis.MajorDir)
                continue;
            int pos1 = segments[ix1].Pos;
            for (var ix2 = 0; ix2 < segments.Count; ix2++)
            {
                var seg1 = segments[ix1];
                var seg2 = segments[ix2];
                if (ix1 == ix2 || !seg1.Dir.IsOpposite(seg2.Dir))
                    continue;
                int pos2 = seg2.Pos;
                var dist = pos2 - pos1;
                if (dist < 0)
                    continue;
                int min = Math.Max(seg1.MinCoord, seg2.MinCoord);
                int max = Math.Min(seg1.MaxCoord, seg2.MaxCoord);
                var len = max - min;
                if (len < lenThreshold)
                    continue;
                bool Check(Segment seg) => dist * 8 < seg.Score * 9 && (dist * 8 < seg.Score * 7 || seg.Len < len);
                if (Check(seg1))
                {
                    var s = segments[ix1];
                    s.Score = dist;
                    s.Len = len;
                    s.LinkIx = ix2;
                    segments[ix1] = s;
                }
                if (Check(seg2))
                {
                    var s = segments[ix2];
                    s.Score = dist;
                    s.Len = len;
                    s.LinkIx = ix1;
                    segments[ix2] = s;
                }
            }
        }
        // Serifs.
        for (var ix1 = 0; ix1 < segments.Count; ix1++)
        {
            var seg1 = segments[ix1];
            if (seg1.Score >= distThreshold || seg1.LinkIx is not { } link1Ix)
                continue;
            var link1 = segments[link1Ix];
            if (link1.LinkIx != ix1 || link1.Pos <= seg1.Pos)
                continue;
            for (var ix2 = 0; ix2 < segments.Count; ix2++)
            {
                var seg2 = segments[ix2];
                if (seg2.Pos > seg1.Pos || ix1 == ix2)
                    continue;
                if (seg2.LinkIx is not { } link2Ix)
                    continue;
                var link2 = segments[link2Ix];
                if (link2.LinkIx != ix2 || link2.Pos < link1.Pos)
                    continue;
                if (seg1.Pos == seg2.Pos && link1.Pos == link2.Pos)
                    continue;
                if (seg2.Score <= seg1.Score || seg1.Score * 4 <= seg2.Score)
                    continue;
                if (seg1.Len >= seg2.Len * 3)
                {
                    for (var i = 0; i < segments.Count; i++)
                    {
                        var seg = segments[i];
                        if (seg.LinkIx == ix2)
                        {
                            seg.LinkIx = null;
                            seg.SerifIx = link1Ix;
                        }
                        else if (seg.LinkIx == link2Ix)
                        {
                            seg.LinkIx = null;
                            seg.SerifIx = ix1;
                        }
                        segments[i] = seg;
                    }
                }
                else
                {
                    var a = segments[ix1];
                    a.LinkIx = null;
                    segments[ix1] = a;
                    var b = segments[link1Ix];
                    b.LinkIx = null;
                    segments[link1Ix] = b;
                    break;
                }
            }
        }
        for (var ix1 = 0; ix1 < segments.Count; ix1++)
        {
            var seg1 = segments[ix1];
            if (seg1.LinkIx is not { } link)
                continue;
            var seg2 = segments[link];
            if (seg2.LinkIx != ix1)
            {
                seg1.LinkIx = null;
                if (seg2.Score < distThreshold || seg1.Score < seg2.Score * 4)
                    seg1.SerifIx = seg2.LinkIx;
                segments[ix1] = seg1;
            }
        }
    }

    static void AssignPointUvs(Outline outline, Dimension dim)
    {
        var points = outline.Span;
        for (var i = 0; i < points.Length; i++)
            if (dim == Dimension.Horizontal)
            {
                points[i].U = points[i].Fx;
                points[i].V = points[i].Fy;
            }
            else
            {
                points[i].U = points[i].Fy;
                points[i].V = points[i].Fx;
            }
    }

    struct State
    {
        public int MinPos, MaxPos, MinCoord, MaxCoord;
        public PointFlags MinFlags, MaxFlags;
        public int MinOnCoord, MaxOnCoord;

        public static State Initial => new()
        {
            MinPos = MaxScore, MaxPos = MinScore, MinCoord = MaxScore, MaxCoord = MinScore,
            MinOnCoord = MaxScore, MaxOnCoord = MinScore,
        };

        public readonly void ApplyTo(ref Segment segment, int flatThreshold)
        {
            segment.Pos = (short)((MinPos + MaxPos) >> 1);
            segment.Delta = (short)((MaxPos - MinPos) >> 1);
            // Round when an end is a control point and the on-curve points between stay within a limit.
            if (((MinFlags & PointFlags.OnCurve) == 0 || (MaxFlags & PointFlags.OnCurve) == 0) && MaxOnCoord - MinOnCoord < flatThreshold)
                segment.Flags |= TopoFlags.Round;
            segment.MinCoord = (short)MinCoord;
            segment.MaxCoord = (short)MaxCoord;
            segment.Height = (short)(MaxCoord - MinCoord);
        }
    }

    static bool BuildSegments(Outline outline, Axis axis)
    {
        var flatThreshold = outline.UnitsPerEm / 14;
        axis.Segments.Clear();
        var majorDir = axis.MajorDir.Normalize();
        var segmentDir = majorDir;
        var points = outline.Points;
        var segments = axis.Segments;
        foreach (var contour in outline.Contours)
        {
            var isSinglePointContour = contour.First == contour.Last;
            var pointIx = contour.First;
            var lastIx = contour.Prev(pointIx);
            var state = State.Initial;
            var prevState = state;
            int? prevSegmentIx = null;
            var segmentIx = 0;
            // Starting on an edge: back up to its start.
            if (points[pointIx].OutDir.IsSameAxis(majorDir) && points[lastIx].OutDir.IsSameAxis(majorDir))
            {
                lastIx = pointIx;
                while (true)
                {
                    pointIx = contour.Prev(pointIx);
                    if (!points[pointIx].OutDir.IsSameAxis(majorDir))
                    {
                        pointIx = contour.Next(pointIx);
                        break;
                    }
                    if (pointIx == lastIx)
                        break;
                }
            }
            lastIx = pointIx;
            var onEdge = false;
            var passed = false;
            while (true)
            {
                if (onEdge)
                {
                    var point = points[pointIx];
                    state.MinPos = Math.Min(state.MinPos, point.U);
                    state.MaxPos = Math.Max(state.MaxPos, point.U);
                    var v = point.V;
                    if (v < state.MinCoord)
                    {
                        state.MinCoord = v;
                        state.MinFlags = point.Flags;
                    }
                    if (v > state.MaxCoord)
                    {
                        state.MaxCoord = v;
                        state.MaxFlags = point.Flags;
                    }
                    if (point.IsOnCurve)
                    {
                        state.MinOnCoord = Math.Min(state.MinOnCoord, point.V);
                        state.MaxOnCoord = Math.Max(state.MaxOnCoord, point.V);
                    }
                    if (point.OutDir != segmentDir || pointIx == lastIx)
                    {
                        // Merge with the previous segment only when it ends where this one starts.
                        if (prevSegmentIx is { } candidate && segments[segmentIx].FirstIx != segments[candidate].LastIx)
                            prevSegmentIx = null;
                        if (prevSegmentIx is { } prevIx)
                        {
                            var prevSegment = segments[prevIx];
                            if (points[prevSegment.LastIx].InDir == point.InDir)
                            {
                                // Same directions: unify the segments.
                                state.MinPos = Math.Min(prevState.MinPos, state.MinPos);
                                state.MaxPos = Math.Max(prevState.MaxPos, state.MaxPos);
                                if (prevState.MinCoord < state.MinCoord)
                                {
                                    state.MinCoord = prevState.MinCoord;
                                    state.MinFlags = prevState.MinFlags;
                                }
                                if (prevState.MaxCoord > state.MaxCoord)
                                {
                                    state.MaxCoord = prevState.MaxCoord;
                                    state.MaxFlags = prevState.MaxFlags;
                                }
                                state.MinOnCoord = Math.Min(prevState.MinOnCoord, state.MinOnCoord);
                                state.MaxOnCoord = Math.Max(prevState.MaxOnCoord, state.MaxOnCoord);
                                prevSegment.LastIx = pointIx;
                                state.ApplyTo(ref prevSegment, flatThreshold);
                                segments[prevIx] = prevSegment;
                            }
                            else if (Math.Abs(prevState.MaxCoord - prevState.MinCoord) > Math.Abs(state.MaxCoord - state.MinCoord))
                            {
                                // Different directions, the previous segment longer: drop this one.
                                prevState.MinPos = Math.Min(prevState.MinPos, state.MinPos);
                                prevState.MaxPos = Math.Max(prevState.MaxPos, state.MaxPos);
                                prevSegment.LastIx = pointIx;
                                prevSegment.Pos = (short)((prevState.MinPos + prevState.MaxPos) >> 1);
                                prevSegment.Delta = (short)((prevState.MaxPos - prevState.MinPos) >> 1);
                                segments[prevIx] = prevSegment;
                            }
                            else
                            {
                                // Else drop the previous one.
                                state.MinPos = Math.Min(state.MinPos, prevState.MinPos);
                                state.MaxPos = Math.Max(state.MaxPos, prevState.MaxPos);
                                var segment = segments[segmentIx];
                                segment.LastIx = pointIx;
                                state.ApplyTo(ref segment, flatThreshold);
                                segments[prevIx] = segment;
                                prevState = state;
                            }
                            segments.RemoveAt(segments.Count - 1);
                        }
                        else
                        {
                            // Leaving an edge: the segment is done.
                            var segment = segments[segmentIx];
                            segment.LastIx = pointIx;
                            state.ApplyTo(ref segment, flatThreshold);
                            segments[segmentIx] = segment;
                            prevSegmentIx = segmentIx;
                            prevState = state;
                        }
                        onEdge = false;
                    }
                }
                if (pointIx == lastIx)
                {
                    if (passed)
                        break;
                    passed = true;
                }
                var p = points[pointIx];
                if (!onEdge && (p.OutDir.IsSameAxis(majorDir) || isSinglePointContour))
                {
                    if (segments.Count > 1000)
                    {
                        segments.Clear();
                        return false;
                    }
                    segmentIx = segments.Count;
                    segmentDir = p.OutDir;
                    var segment = new Segment { Dir = segmentDir, FirstIx = pointIx, LastIx = pointIx, Score = MaxScore };
                    state.MinPos = p.U;
                    state.MaxPos = p.U;
                    state.MinCoord = p.V;
                    state.MaxCoord = p.V;
                    state.MinFlags = p.Flags;
                    state.MaxFlags = p.Flags;
                    if (!p.IsOnCurve)
                    {
                        state.MinOnCoord = MaxScore;
                        state.MaxOnCoord = MinScore;
                    }
                    else
                    {
                        state.MinOnCoord = p.V;
                        state.MaxOnCoord = p.V;
                    }
                    onEdge = true;
                    if (isSinglePointContour)
                    {
                        segment.Pos = (short)state.MinPos;
                        if (!p.IsOnCurve)
                            segment.Flags |= TopoFlags.Round;
                        segment.MinCoord = (short)p.V;
                        segment.MaxCoord = (short)p.V;
                        segment.Height = 0;
                        onEdge = false;
                    }
                    segments.Add(segment);
                }
                pointIx = contour.Next(pointIx);
            }
        }
        return true;
    }

    static void AdjustSegmentHeights(Outline outline, Axis axis)
    {
        var points = outline.Points;
        var segments = axis.Segments;
        for (var i = 0; i < segments.Count; i++)
        {
            var segment = segments[i];
            var first = points[segment.FirstIx];
            var last = points[segment.LastIx];
            var prev = points[first.PrevIx];
            var next = points[last.NextIx];
            void Adjust(int v1, int v2) => segment.Height = (short)(segment.Height + ((v1 - v2) >> 1));
            if (first.V < last.V)
            {
                if (prev.V < first.V)
                    Adjust(first.V, prev.V);
                if (next.V > last.V)
                    Adjust(next.V, last.V);
            }
            else
            {
                if (prev.V > first.V)
                    Adjust(prev.V, first.V);
                if (next.V < last.V)
                    Adjust(last.V, next.V);
            }
            segments[i] = segment;
        }
    }

    public static void ComputeEdges(Axis axis, ScaledAxisMetrics metrics, bool topToBottomHinting, int yScale, ScriptGroup group)
    {
        axis.Edges.Clear();
        var scale = metrics.Scale;
        // CJK always passes false.
        if (axis.Dim == Dimension.Horizontal || group != ScriptGroup.Default)
            topToBottomHinting = false;
        // Ignore horizontal segments less than a pixel long…
        var segmentLengthThreshold = axis.Dim == Dimension.Horizontal ? Fixed.Div(64, yScale) : 0;
        // …and segments more than half a pixel wide.
        var segmentWidthThreshold = Fixed.Div(32, scale);
        // The edge distance threshold is at most a quarter pixel.
        var initialThreshold = metrics.WidthMetrics.EdgeDistanceThreshold;
        const int edgeDistanceThresholdMax = 64 / 4;
        int edgeDistanceThreshold;
        if (group == ScriptGroup.Default)
            edgeDistanceThreshold = Fixed.Div(Math.Min(Fixed.Mul(initialThreshold, scale), edgeDistanceThresholdMax), scale);
        else
        {
            var threshold = Fixed.Mul(initialThreshold, scale);
            edgeDistanceThreshold = threshold > edgeDistanceThresholdMax ? Fixed.Div(edgeDistanceThresholdMax, scale) : initialThreshold;
        }
        var segments = axis.Segments;
        for (var segmentIx = 0; segmentIx < segments.Count; segmentIx++)
        {
            var segment = segments[segmentIx];
            if (group == ScriptGroup.Default)
            {
                if (segment.Height < segmentLengthThreshold || segment.Delta > segmentWidthThreshold || segment.Dir == Direction.None)
                    continue;
                // Serif edges under 1.5 pixels.
                if (segment.SerifIx is not null && 2 * segment.Height < 3 * segmentLengthThreshold)
                    continue;
            }
            var bestDist = int.MaxValue;
            int? bestEdgeIx = null;
            for (var edgeIx = 0; edgeIx < axis.Edges.Count; edgeIx++)
            {
                var edge = axis.Edges[edgeIx];
                var dist = Math.Abs(segment.Pos - edge.Fpos);
                if (dist < edgeDistanceThreshold && edge.Dir == segment.Dir && dist < bestDist)
                {
                    if (group == ScriptGroup.Default)
                    {
                        bestEdgeIx = edgeIx;
                        break;
                    }
                    // CJK: all linked segments of the candidate edge must make one edge.
                    if (segment.LinkIx is { } linkIx)
                    {
                        var link = segments[linkIx];
                        var firstIx = edge.FirstIx;
                        var seg1 = segments[firstIx];
                        var dist2 = 0;
                        while (true)
                        {
                            if (seg1.LinkIx is { } link1Ix)
                            {
                                dist2 = Math.Abs(link.Pos - segments[link1Ix].Pos);
                                if (dist2 >= edgeDistanceThreshold)
                                    break;
                            }
                            if (seg1.EdgeNextIx == firstIx)
                                break;
                            if (seg1.EdgeNextIx is { } next && next < segments.Count)
                                seg1 = segments[next];
                            else
                                break;
                        }
                        if (dist2 >= edgeDistanceThreshold)
                            continue;
                    }
                    bestDist = dist;
                    bestEdgeIx = edgeIx;
                }
            }
            if (bestEdgeIx is { } found)
                axis.AppendSegmentToEdge(segmentIx, found);
            else
            {
                var opos = Fixed.Mul(segment.Pos, scale);
                axis.InsertEdge(new Edge { Fpos = segment.Pos, Opos = opos, Pos = opos, Dir = segment.Dir, FirstIx = segmentIx, LastIx = segmentIx }, topToBottomHinting);
                var s = segments[segmentIx];
                s.EdgeNextIx = segmentIx;
                segments[segmentIx] = s;
            }
        }
        if (group == ScriptGroup.Default)
        {
            // Single point segments without a direction join an edge they coincide with.
            for (var segmentIx = 0; segmentIx < segments.Count; segmentIx++)
            {
                var segment = segments[segmentIx];
                if (segment.Dir != Direction.None)
                    continue;
                for (var edgeIx = 0; edgeIx < axis.Edges.Count; edgeIx++)
                    if (Math.Abs(segment.Pos - axis.Edges[edgeIx].Fpos) < edgeDistanceThreshold)
                    {
                        axis.AppendSegmentToEdge(segmentIx, edgeIx);
                        break;
                    }
            }
        }
        LinkSegmentsToEdges(axis);
        ComputeEdgeProperties(axis);
    }

    static void LinkSegmentsToEdges(Axis axis)
    {
        var segments = axis.Segments;
        for (var edgeIx = 0; edgeIx < axis.Edges.Count; edgeIx++)
        {
            var edge = axis.Edges[edgeIx];
            var ix = edge.FirstIx;
            var lastIx = edge.LastIx;
            while (true)
            {
                var segment = segments[ix];
                segment.EdgeIx = edgeIx;
                segments[ix] = segment;
                if (ix == lastIx)
                    break;
                ix = segment.EdgeNextIx ?? lastIx;
            }
        }
    }

    static void ComputeEdgeProperties(Axis axis)
    {
        var edges = axis.Edges;
        var segments = axis.Segments;
        for (var edgeIx = 0; edgeIx < edges.Count; edgeIx++)
        {
            int roundness = 0, straightness = 0;
            var segmentIx = edges[edgeIx].FirstIx;
            var lastSegmentIx = edges[edgeIx].LastIx;
            while (true)
            {
                // The loop can change the edge: read it again.
                var edge = edges[edgeIx];
                var segment = segments[segmentIx];
                var nextSegmentIx = segment.EdgeNextIx;
                if ((segment.Flags & TopoFlags.Round) != 0)
                    roundness++;
                else
                    straightness++;
                var isSerif = segment.SerifIx is { } serifIx && segments[serifIx].EdgeIx is { } serifEdge && serifEdge != edgeIx;
                if (isSerif || (segment.LinkIx is { } link && segments[link].EdgeIx is not null))
                {
                    var (edge2Ix, segment2Ix) = isSerif ? (edge.SerifIx, segment.SerifIx) : (edge.LinkIx, segment.LinkIx);
                    int? resolved;
                    if (edge2Ix is { } e2 && segment2Ix is { } s2)
                    {
                        var edgeDelta = Math.Abs(edge.Fpos - edges[e2].Fpos);
                        var segmentDelta = Math.Abs(segment.Pos - segments[s2].Pos);
                        resolved = segmentDelta < edgeDelta ? segments[s2].EdgeIx : e2;
                    }
                    else if (segment2Ix is { } s)
                        resolved = segments[s].EdgeIx;
                    else
                        resolved = edge2Ix;
                    var current = edges[edgeIx];
                    if (isSerif)
                    {
                        current.SerifIx = resolved;
                        edges[edgeIx] = current;
                        var target = edges[resolved!.Value];
                        target.Flags |= TopoFlags.Serif;
                        edges[resolved.Value] = target;
                    }
                    else
                    {
                        current.LinkIx = resolved;
                        edges[edgeIx] = current;
                    }
                }
                if (segmentIx == lastSegmentIx)
                    break;
                segmentIx = nextSegmentIx ?? lastSegmentIx;
            }
            var done = edges[edgeIx];
            done.Flags = TopoFlags.Normal;
            if (roundness > 0 && roundness >= straightness)
                done.Flags |= TopoFlags.Round;
            // Linked edges drop their serifs.
            if (done.SerifIx is not null && done.LinkIx is not null)
                done.SerifIx = null;
            edges[edgeIx] = done;
        }
    }

    public static void ComputeBlueEdges(Axis axis, in Scale scale, IReadOnlyList<UnscaledBlue> unscaledBlues, IReadOnlyList<ScaledBlue> blues, ScriptGroup group)
    {
        // The default group has no horizontal blues.
        if (axis.Dim != Dimension.Vertical && group == ScriptGroup.Default)
            return;
        var axisScale = axis.Dim == Dimension.Horizontal ? scale.XScale : scale.YScale;
        // At most half a pixel.
        var initialBestDist = Math.Min(Fixed.Mul(scale.UnitsPerEm / 40, axisScale), 64 / 2);
        for (var e = 0; e < axis.Edges.Count; e++)
        {
            var edge = axis.Edges[e];
            ScaledWidth? bestBlue = null;
            var bestIsNeutral = false;
            var bestDist = initialBestDist;
            var count = Math.Min(unscaledBlues.Count, blues.Count);
            for (var blueIx = 0; blueIx < count; blueIx++)
            {
                var unscaledBlue = unscaledBlues[blueIx];
                var blue = blues[blueIx];
                if (!blue.IsActive)
                    continue;
                var isTop = blue.Zones.IsTopLike();
                var isNeutral = blue.Zones.IsNeutral();
                var isMajorDir = edge.Dir == axis.MajorDir;
                // Neutral blues take both directions.
                if (!((isTop ^ isMajorDir) || isNeutral))
                    continue;
                int refPos;
                ScaledWidth matching;
                if (group == ScriptGroup.Default)
                    (refPos, matching) = (unscaledBlue.Position, blue.Position);
                else if (Math.Abs(edge.Fpos - unscaledBlue.Position) > Math.Abs(edge.Fpos - unscaledBlue.Overshoot))
                    (refPos, matching) = (unscaledBlue.Overshoot, blue.Overshoot);
                else
                    (refPos, matching) = (unscaledBlue.Position, blue.Position);
                var dist = Fixed.Mul(Math.Abs(edge.Fpos - refPos), axisScale);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestBlue = matching;
                    bestIsNeutral = isNeutral;
                }
                // The default group compares to the overshoot too.
                if (group == ScriptGroup.Default && (edge.Flags & TopoFlags.Round) != 0 && dist != 0 && !isNeutral)
                {
                    var isUnderRef = edge.Fpos < unscaledBlue.Position;
                    if (isTop ^ isUnderRef)
                    {
                        var shootDist = Fixed.Mul(Math.Abs(edge.Fpos - unscaledBlue.Overshoot), axisScale);
                        if (shootDist < bestDist)
                        {
                            bestDist = shootDist;
                            bestBlue = blue.Overshoot;
                            bestIsNeutral = isNeutral;
                        }
                    }
                }
            }
            if (bestBlue is { } best)
            {
                edge.BlueEdge = best;
                if (bestIsNeutral)
                    edge.Flags |= TopoFlags.Neutral;
                axis.Edges[e] = edge;
            }
        }
    }
}

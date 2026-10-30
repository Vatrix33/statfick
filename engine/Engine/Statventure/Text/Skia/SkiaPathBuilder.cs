using System.Numerics;

namespace Statventure.Text.Skia;

/// <summary>
/// SkPathBuilder's editing (as the stroker uses it: moves that replace a trailing move, contours
/// reopened after a close, conics by weight, reversed and appended paths, rects and ovals) and the
/// SkPath queries the stroker asks (rect contours with their direction, the first direction).
/// </summary>
sealed partial class SkiaPath
{
    int LastMoveIndex
    {
        get
        {
            var index = 0;
            var last = -1;
            foreach (var verb in Verbs)
            {
                if (verb == PathVerb.Move)
                    last = index;
                index += PointsIn(verb);
            }
            return last;
        }
    }

    public int CountPoints => Points.Count;

    public Vector2? LastPoint => Points.Count > 0 ? Points[^1] : null;

    public void SetLastPoint(Vector2 point) => Points[^1] = point;

    public void Reset()
    {
        Points.Clear();
        Verbs.Clear();
        ConicWeights.Clear();
    }

    public SkiaPath MoveTo(Vector2 p)
    {
        if (Verbs.Count > 0 && Verbs[^1] == PathVerb.Move)
            Points[^1] = p;
        else
        {
            Points.Add(p);
            Verbs.Add(PathVerb.Move);
        }
        return this;
    }

    void EnsureMove()
    {
        if (Verbs.Count == 0)
            MoveTo(Vector2.Zero);
        else if (Verbs[^1] == PathVerb.Close)
            MoveTo(Points[LastMoveIndex]);
    }

    public SkiaPath LineTo(Vector2 p)
    {
        EnsureMove();
        Points.Add(p);
        Verbs.Add(PathVerb.Line);
        return this;
    }

    public SkiaPath QuadTo(Vector2 p1, Vector2 p2)
    {
        EnsureMove();
        Points.Add(p1);
        Points.Add(p2);
        Verbs.Add(PathVerb.Quad);
        return this;
    }

    public SkiaPath ConicTo(Vector2 p1, Vector2 p2, float w)
    {
        EnsureMove();
        if (w <= 0)
            return LineTo(p2);
        Points.Add(p1);
        Points.Add(p2);
        if (w == 1)
            Verbs.Add(PathVerb.Quad);
        else if (float.IsFinite(w))
        {
            Verbs.Add(PathVerb.Conic);
            ConicWeights.Add(w);
        }
        else
        {
            Verbs.Add(PathVerb.Line);
            Verbs.Add(PathVerb.Line);
        }
        return this;
    }

    public SkiaPath CubicTo(Vector2 p1, Vector2 p2, Vector2 p3)
    {
        EnsureMove();
        Points.Add(p1);
        Points.Add(p2);
        Points.Add(p3);
        Verbs.Add(PathVerb.Cubic);
        return this;
    }

    public SkiaPath Close()
    {
        if (Verbs.Count > 0 && Verbs[^1] != PathVerb.Close)
        {
            EnsureMove();
            Verbs.Add(PathVerb.Close);
        }
        return this;
    }

    /// <summary>SkPathBuilder::addPath (append, no matrix): replaces an empty or move-only path, else drops a trailing move first.</summary>
    public SkiaPath AddPath(SkiaPath src)
    {
        if (src.IsEmpty)
            return this;
        if (Verbs.Count <= 1)
        {
            Reset();
            Points.AddRange(src.Points);
            Verbs.AddRange(src.Verbs);
            ConicWeights.AddRange(src.ConicWeights);
            return this;
        }
        if (Verbs[^1] == PathVerb.Move)
        {
            Verbs.RemoveAt(Verbs.Count - 1);
            Points.RemoveAt(Points.Count - 1);
        }
        Points.AddRange(src.Points);
        Verbs.AddRange(src.Verbs);
        ConicWeights.AddRange(src.ConicWeights);
        return this;
    }

    /// <summary>SkPathBuilder::privateReversePathTo: the last contour of <paramref name="src"/> backwards from its last point, which is skipped.</summary>
    public SkiaPath ReversePathTo(SkiaPath src)
    {
        if (src.Verbs.Count == 0)
            return this;
        var pts = src.Points.Count - 1;
        var conics = src.ConicWeights.Count;
        for (var v = src.Verbs.Count - 1; v >= 0; v--)
        {
            var verb = src.Verbs[v];
            pts -= PointsIn(verb);
            switch (verb)
            {
                case PathVerb.Move:
                    return this;
                case PathVerb.Line:
                    LineTo(src.Points[pts]);
                    break;
                case PathVerb.Quad:
                    QuadTo(src.Points[pts + 1], src.Points[pts]);
                    break;
                case PathVerb.Conic:
                    ConicTo(src.Points[pts + 1], src.Points[pts], src.ConicWeights[--conics]);
                    break;
                case PathVerb.Cubic:
                    CubicTo(src.Points[pts + 2], src.Points[pts + 1], src.Points[pts]);
                    break;
            }
        }
        return this;
    }

    /// <summary>SkPathBuilder::privateReverseAddPath: every contour of <paramref name="src"/> added backwards.</summary>
    public SkiaPath ReverseAddPath(SkiaPath src)
    {
        if (src.Verbs.Count == 0)
            return this;
        var pts = src.Points.Count;
        var conics = src.ConicWeights.Count;
        var needMove = true;
        var needClose = false;
        for (var v = src.Verbs.Count - 1; v >= 0; v--)
        {
            var verb = src.Verbs[v];
            var n = PointsIn(verb);
            if (needMove)
            {
                --pts;
                MoveTo(src.Points[pts]);
                needMove = false;
            }
            pts -= n;
            switch (verb)
            {
                case PathVerb.Move:
                    if (needClose)
                    {
                        Close();
                        needClose = false;
                    }
                    needMove = true;
                    pts += 1;
                    break;
                case PathVerb.Line:
                    LineTo(src.Points[pts]);
                    break;
                case PathVerb.Quad:
                    QuadTo(src.Points[pts + 1], src.Points[pts]);
                    break;
                case PathVerb.Conic:
                    ConicTo(src.Points[pts + 1], src.Points[pts], src.ConicWeights[--conics]);
                    break;
                case PathVerb.Cubic:
                    CubicTo(src.Points[pts + 2], src.Points[pts + 1], src.Points[pts]);
                    break;
                case PathVerb.Close:
                    needClose = true;
                    break;
            }
        }
        return this;
    }

    /// <summary>SkPath_RectPointIterator's corners: top left, top right, bottom right, bottom left.</summary>
    static Vector2[] RectCorners(SkRect r) => [new(r.Left, r.Top), new(r.Right, r.Top), new(r.Right, r.Bottom), new(r.Left, r.Bottom)];

    /// <summary>SkPathBuilder::addRect (SkPathRawShapes::Rect): four corners from <paramref name="index"/> in the direction, closed.</summary>
    public SkiaPath AddRect(SkRect rect, bool clockwise, int index = 0)
    {
        var corners = RectCorners(rect);
        var advance = clockwise ? 1 : 3;
        var i = index % 4;
        AddRawStart();
        Points.Add(corners[i]);
        Verbs.Add(PathVerb.Move);
        for (var k = 0; k < 3; k++)
        {
            i = (i + advance) % 4;
            Points.Add(corners[i]);
            Verbs.Add(PathVerb.Line);
        }
        Verbs.Add(PathVerb.Close);
        return this;
    }

    /// <summary>SkPathBuilder::addOval (SkPathRawShapes::Oval): four quarter conics, starting at <paramref name="index"/> (1, the right, by default).</summary>
    public SkiaPath AddOval(SkRect oval, bool clockwise, int index = 1)
    {
        var cx = (float)(((double)oval.Left + oval.Right) * 0.5);
        var cy = (float)(((double)oval.Top + oval.Bottom) * 0.5);
        Vector2[] ovalPts = [new(cx, oval.Top), new(oval.Right, cy), new(cx, oval.Bottom), new(oval.Left, cy)];
        var corners = RectCorners(oval);
        var advance = clockwise ? 1 : 3;
        var o = index % 4;
        var r = (index + (clockwise ? 0 : 1)) % 4;
        AddRawStart();
        Points.Add(ovalPts[o]);
        Verbs.Add(PathVerb.Move);
        for (var k = 0; k < 4; k++)
        {
            r = (r + advance) % 4;
            o = (o + advance) % 4;
            Points.Add(corners[r]);
            Points.Add(ovalPts[o]);
            Verbs.Add(PathVerb.Conic);
            ConicWeights.Add(SkiaGeometry.Root2Over2);
        }
        Verbs.Add(PathVerb.Close);
        return this;
    }

    public SkiaPath AddCircle(Vector2 center, float radius, bool clockwise = true) =>
        radius >= 0 ? AddOval(new SkRect(center.X - radius, center.Y - radius, center.X + radius, center.Y + radius), clockwise) : this;

    /// <summary>A raw shape starts a contour of its own: a trailing move is dropped, as when appending a path.</summary>
    void AddRawStart()
    {
        if (Verbs.Count > 0 && Verbs[^1] == PathVerb.Move)
        {
            Verbs.RemoveAt(Verbs.Count - 1);
            Points.RemoveAt(Points.Count - 1);
        }
    }

    public SkiaPath AddPolygon(ReadOnlySpan<Vector2> points, bool closed)
    {
        if (points.IsEmpty)
            return this;
        MoveTo(points[0]);
        if (points.Length > 1)
        {
            EnsureMove();
            foreach (var p in points[1..])
            {
                Points.Add(p);
                Verbs.Add(PathVerb.Line);
            }
        }
        if (closed)
            Close();
        return this;
    }

    public bool IsLastContourClosed => Verbs.Count > 0 && Verbs[^1] == PathVerb.Close;

    /// <summary>A rect contour (SkPathPriv::RectContour): its bounds, whether it's closed and its direction.</summary>
    public readonly record struct RectContour(SkRect Rect, bool IsClosed, bool Clockwise);

    /// <summary>SkPath::isRect: SkPathPriv::IsRectContour without partial rects.</summary>
    public RectContour? IsRect()
    {
        if (SegmentMask != 1 || Points.Count < 4 || Verbs.Count < 4)
            return null;
        if (TrivialRect() is { } trivial)
        {
            var v0 = Points[1] - Points[0];
            var v1 = Points[2] - Points[1];
            return new RectContour(trivial, true, v0.X * v1.Y - v0.Y * v1.X > 0);
        }
        static int MakeDir(float dx, float dy) => (dx != 0 ? 1 : 0) | (dx > 0 || dy > 0 ? 2 : 0);
        var currentVerb = 0;
        var corners = 0;
        Vector2 lineStart = Vector2.Zero, firstCorner = default, thirdCorner = default;
        var firstPt = -1;
        var lastPt = -1;
        var pts = 0;
        Span<int> directions = [-1, -1, -1, -1, -1];
        var closedOrMoved = false;
        var autoClose = false;
        while (currentVerb < Verbs.Count)
        {
            var verb = Verbs[currentVerb];
            switch (verb)
            {
                case PathVerb.Close:
                case PathVerb.Line:
                {
                    if (verb == PathVerb.Close)
                        autoClose = true;
                    else
                        lastPt = pts;
                    var lineEnd = verb == PathVerb.Close ? Points[firstPt] : Points[pts++];
                    var delta = lineEnd - lineStart;
                    if (delta.X != 0 && delta.Y != 0)
                        return null;
                    if (!float.IsFinite(delta.X) || !float.IsFinite(delta.Y))
                        return null;
                    if (lineStart == lineEnd)
                        break;
                    var nextDirection = MakeDir(delta.X, delta.Y);
                    if (corners == 0)
                    {
                        directions[0] = nextDirection;
                        corners = 1;
                        closedOrMoved = false;
                        lineStart = lineEnd;
                        break;
                    }
                    if (closedOrMoved)
                        return null;
                    if (autoClose && nextDirection == directions[0])
                        break;
                    closedOrMoved = autoClose;
                    if (directions[corners - 1] == nextDirection)
                    {
                        if (corners == 3 && verb == PathVerb.Line)
                            thirdCorner = lineEnd;
                        lineStart = lineEnd;
                        break;
                    }
                    directions[corners++] = nextDirection;
                    switch (corners)
                    {
                        case 2:
                            firstCorner = lineStart;
                            break;
                        case 3:
                            if ((directions[0] ^ directions[2]) != 2)
                                return null;
                            thirdCorner = lineEnd;
                            break;
                        case 4:
                            if ((directions[1] ^ directions[3]) != 2)
                                return null;
                            break;
                        default:
                            return null;
                    }
                    lineStart = lineEnd;
                    break;
                }
                case PathVerb.Quad:
                case PathVerb.Conic:
                case PathVerb.Cubic:
                    return null;
                case PathVerb.Move:
                    if (corners == 0)
                        firstPt = pts;
                    else
                    {
                        var close = Points[firstPt] - Points[lastPt];
                        if (close.X != 0 && close.Y != 0)
                            return null;
                    }
                    lineStart = Points[pts++];
                    closedOrMoved = true;
                    break;
            }
            currentVerb++;
        }
        if (corners < 3 || corners > 4)
            return null;
        var closeXY = Points[firstPt] - Points[lastPt];
        if (closeXY.X != 0 && closeXY.Y != 0)
            return null;
        var rect = new SkRect(MathF.Min(firstCorner.X, thirdCorner.X), MathF.Min(firstCorner.Y, thirdCorner.Y),
            MathF.Max(firstCorner.X, thirdCorner.X), MathF.Max(firstCorner.Y, thirdCorner.Y));
        return new RectContour(rect, autoClose, directions[0] == ((directions[1] + 1) & 3));
    }

    /// <summary>
    /// SkPathPriv::ComputeFirstDirection: the winding of the contour that reaches lowest (largest y),
    /// from the cross product at that point; null when it can't be told.
    /// </summary>
    public bool? FirstDirectionClockwise()
    {
        var ymax = Bounds.Top;
        var ymaxCross = 0f;
        var start = 0;
        var v = 0;
        while (v < Verbs.Count)
        {
            // ContourIter: a move and the points up to the next move.
            var count = 1;
            for (v++; v < Verbs.Count && Verbs[v] != PathVerb.Move; v++)
                count += PointsIn(Verbs[v]);
            var contour = start;
            start += count;
            if (count < 3)
                continue;
            var pts = System.Runtime.InteropServices.CollectionsMarshal.AsSpan(Points).Slice(contour, count);
            var n = count;
            var index = 0;
            var max = pts[0].Y;
            for (var i = 1; i < n; i++)
                if (pts[i].Y > max)
                {
                    max = pts[i].Y;
                    index = i;
                }
            if (pts[index].Y < ymax)
                continue;
            float cross = 0;
            var useCross = true;
            if (pts[(index + 1) % n].Y == pts[index].Y)
            {
                var y = pts[index].Y;
                float minX = pts[index].X, maxX = minX;
                int minIndex = index, maxIndex = index;
                for (var i = index + 1; i < n; i++)
                {
                    if (pts[i].Y != y)
                        break;
                    var x = pts[i].X;
                    if (x < minX)
                    {
                        minX = x;
                        minIndex = i;
                    }
                    else if (x > maxX)
                    {
                        maxX = x;
                        maxIndex = i;
                    }
                }
                if (minIndex != maxIndex)
                {
                    cross = minIndex - maxIndex;
                    useCross = false;
                }
            }
            if (useCross)
            {
                // find_diff_pt: the next point (by inc) that differs from pts[index], or index after a full turn.
                static int Different(ReadOnlySpan<Vector2> pts, int index, int n, int inc)
                {
                    var i = index;
                    for (;;)
                    {
                        i = (i + inc) % n;
                        if (i == index || pts[index] != pts[i])
                            break;
                    }
                    return i;
                }
                var prev = Different(pts, index, n, n - 1);
                if (prev == index)
                    continue;
                var next = Different(pts, index, n, 1);
                cross = CrossProduct(pts[prev], pts[index], pts[next]);
                if (cross == 0 && pts[prev].Y == pts[index].Y && pts[next].Y == pts[index].Y)
                    cross = pts[index].X - pts[next].X;
            }
            if (cross != 0)
            {
                ymax = pts[index].Y;
                ymaxCross = cross;
            }
        }
        return ymaxCross != 0 ? ymaxCross > 0 : null;
    }

    /// <summary>cross_prod: (p1 − p0) × (p2 − p0), again in double when the float result is zero.</summary>
    static float CrossProduct(Vector2 p0, Vector2 p1, Vector2 p2)
    {
        var cross = Cross(p1 - p0, p2 - p0);
        if (cross == 0)
            cross = (float)(((double)p1.X - p0.X) * ((double)p2.Y - p0.Y) - ((double)p1.Y - p0.Y) * ((double)p2.X - p0.X));
        return cross;
    }
}

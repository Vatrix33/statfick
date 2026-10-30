using System.Numerics;

namespace Statventure.Text.Skia;

/// <summary>SkPathVerb's values.</summary>
enum PathVerb : byte
{
    Move = 0,
    Line = 1,
    Quad = 2,
    Conic = 3,
    Cubic = 4,
    Close = 5,
}

/// <summary>SkRect: left, top, right, bottom in floats.</summary>
readonly record struct SkRect(float Left, float Top, float Right, float Bottom)
{
    public bool IsEmpty => !(Left < Right && Top < Bottom);

    /// <summary>SkRect::roundOut: floor the top left, ceil the bottom right.</summary>
    public (int Left, int Top, int Right, int Bottom) RoundOut() =>
        ((int)MathF.Floor(Left), (int)MathF.Floor(Top), (int)MathF.Ceiling(Right), (int)MathF.Ceiling(Bottom));
}

/// <summary>
/// A path as Skia keeps one (SkPath's points and verbs, its bounds, SkPathPriv's convexity and
/// rectangle tests), enough to rasterize glyph outlines exactly as Skia does.
/// </summary>
sealed partial class SkiaPath
{
    public readonly List<Vector2> Points = [];
    public readonly List<PathVerb> Verbs = [];
    /// <summary>The weight of each conic verb, in order.</summary>
    public readonly List<float> ConicWeights = [];

    public bool IsEmpty => Verbs.Count == 0;

    /// <summary>SkPath::SegmentMask: line 1, quad 2, conic 4, cubic 8.</summary>
    public int SegmentMask
    {
        get
        {
            var mask = 0;
            foreach (var verb in Verbs)
                mask |= verb switch { PathVerb.Line => 1, PathVerb.Quad => 2, PathVerb.Conic => 4, PathVerb.Cubic => 8, _ => 0 };
            return mask;
        }
    }

    /// <summary>The bounds of all points (control points included), as SkRect::Bounds.</summary>
    public SkRect Bounds
    {
        get
        {
            if (Points.Count == 0)
                return default;
            var min = Points[0];
            var max = Points[0];
            for (var i = 1; i < Points.Count; i++)
            {
                min = Vector2.Min(min, Points[i]);
                max = Vector2.Max(max, Points[i]);
            }
            return new SkRect(min.X, min.Y, max.X, max.Y);
        }
    }

    public SkiaPath Clone()
    {
        var path = new SkiaPath();
        path.Points.AddRange(Points);
        path.Verbs.AddRange(Verbs);
        path.ConicWeights.AddRange(ConicWeights);
        return path;
    }

    /// <summary>SkPath::makeOffset: every point moved by (dx, dy).</summary>
    public SkiaPath Offset(float dx, float dy)
    {
        var path = new SkiaPath();
        path.Verbs.AddRange(Verbs);
        path.ConicWeights.AddRange(ConicWeights);
        foreach (var p in Points)
            path.Points.Add(new Vector2(p.X + dx, p.Y + dy));
        return path;
    }

    /// <summary>SkMatrix::mapPoints for a scale and translate: x * sx + tx, y * sy + ty.</summary>
    public SkiaPath ScaleTranslate(float sx, float sy, float tx, float ty)
    {
        var path = new SkiaPath();
        path.Verbs.AddRange(Verbs);
        path.ConicWeights.AddRange(ConicWeights);
        foreach (var p in Points)
            path.Points.Add(new Vector2(p.X * sx + tx, p.Y * sy + ty));
        return path;
    }

    /// <summary>SkMatrix::mapPoints for an affine matrix [sx kx tx; ky sy ty] (Affine_vpts: src * scale + (swizzled src * skew + trans)).</summary>
    public SkiaPath Affine(float sx, float kx, float tx, float ky, float sy, float ty)
    {
        var path = new SkiaPath();
        path.Verbs.AddRange(Verbs);
        path.ConicWeights.AddRange(ConicWeights);
        foreach (var p in Points)
            path.Points.Add(new Vector2(p.X * sx + (p.Y * kx + tx), p.Y * sy + (p.X * ky + ty)));
        return path;
    }

    static float Cross(Vector2 a, Vector2 b) => a.X * b.Y - a.Y * b.X;
    static float Dot(Vector2 a, Vector2 b) => a.X * b.X + a.Y * b.Y;

    /// <summary>The number of points a verb adds (SkPathPriv::PtsInVerb).</summary>
    static int PointsIn(PathVerb verb) => verb switch
    {
        PathVerb.Move or PathVerb.Line => 1,
        PathVerb.Quad or PathVerb.Conic => 2,
        PathVerb.Cubic => 3,
        _ => 0,
    };

    /// <summary>SkPathPriv::ComputeConvexity: whether the path is known to be convex.</summary>
    public bool IsConvex
    {
        get
        {
            var verbCount = Verbs.Count;
            var pointCount = Points.Count;
            while (verbCount > 0 && Verbs[verbCount - 1] == PathVerb.Move)
            {
                verbCount--;
                pointCount--;
            }
            if (verbCount == 0)
                return true;
            if (IsConcaveBySign(pointCount))
                return false;
            var contourCount = 0;
            var needsClose = false;
            var state = new Convexicator();
            var pointIndex = 0;
            for (var v = 0; v < verbCount; v++)
            {
                var verb = Verbs[v];
                if (contourCount == 0)
                {
                    if (verb == PathVerb.Move)
                        state.SetMovePoint(Points[pointIndex]);
                    else
                    {
                        contourCount++;
                        needsClose = true;
                    }
                }
                if (contourCount == 1)
                {
                    if (verb is PathVerb.Close or PathVerb.Move)
                    {
                        if (!state.Close())
                            return false;
                        needsClose = false;
                        contourCount++;
                    }
                    else
                    {
                        var count = PointsIn(verb);
                        for (var i = 0; i < count; i++)
                            if (!state.AddPoint(Points[pointIndex + i]))
                                return false;
                    }
                }
                else if (verb != PathVerb.Move)
                    return false;
                pointIndex += PointsIn(verb);
            }
            if (needsClose && !state.Close())
                return false;
            return !(state.FirstDirectionUnknown && state.Reversals >= 3);
        }
    }

    bool IsConcaveBySign(int count)
    {
        if (count <= 3)
            return false;
        static int Sign(float x) => x < 0 ? 1 : 0;
        const int never = 2;
        var current = Points[0];
        int dxes = 0, dyes = 0, lastSx = never, lastSy = never;
        bool Step(Vector2 next)
        {
            var vec = next - current;
            if (vec != Vector2.Zero)
            {
                if (!float.IsFinite(vec.X) || !float.IsFinite(vec.Y))
                    return true;
                var sx = Sign(vec.X);
                var sy = Sign(vec.Y);
                dxes += sx != lastSx ? 1 : 0;
                dyes += sy != lastSy ? 1 : 0;
                if (dxes > 3 || dyes > 3)
                    return true;
                lastSx = sx;
                lastSy = sy;
            }
            current = next;
            return false;
        }
        // Every point in turn, then back to the first.
        for (var i = 1; i < count; i++)
            if (Step(Points[i]))
                return true;
        return Step(Points[0]);
    }

    /// <summary>SkPathPriv's Convexicator (one contour).</summary>
    struct Convexicator
    {
        enum DirChange { Unknown, Left, Right, Straight, Backwards, Invalid }

        Vector2 _firstPoint, _firstVec, _lastPoint, _lastVec;
        DirChange _expected;
        bool _directionKnown;
        public int Reversals;

        public readonly bool FirstDirectionUnknown => !_directionKnown;

        public Convexicator()
        {
            _expected = DirChange.Invalid;
        }

        public void SetMovePoint(Vector2 point)
        {
            _firstPoint = _lastPoint = point;
            _expected = DirChange.Invalid;
        }

        public bool AddPoint(Vector2 point)
        {
            if (_lastPoint == point)
                return true;
            if (_firstPoint == _lastPoint && _expected == DirChange.Invalid && _lastVec == Vector2.Zero)
            {
                _lastVec = point - _lastPoint;
                _firstVec = _lastVec;
            }
            else if (!AddVector(point - _lastPoint))
                return false;
            _lastPoint = point;
            return true;
        }

        public bool Close() => AddPoint(_firstPoint) && AddVector(_firstVec);

        DirChange DirectionChange(Vector2 current)
        {
            var cross = Cross(_lastVec, current);
            if (!float.IsFinite(cross))
                return DirChange.Unknown;
            if (cross == 0)
                return Dot(_lastVec, current) < 0 ? DirChange.Backwards : DirChange.Straight;
            return cross > 0 ? DirChange.Right : DirChange.Left;
        }

        bool AddVector(Vector2 current)
        {
            var dir = DirectionChange(current);
            switch (dir)
            {
                case DirChange.Left:
                case DirChange.Right:
                    if (_expected == DirChange.Invalid)
                    {
                        _expected = dir;
                        _directionKnown = true;
                    }
                    else if (dir != _expected)
                    {
                        _directionKnown = false;
                        return false;
                    }
                    _lastVec = current;
                    break;
                case DirChange.Straight:
                    break;
                case DirChange.Backwards:
                    _lastVec = current;
                    return ++Reversals < 3;
                case DirChange.Unknown:
                    return false;
            }
            return true;
        }
    }

    /// <summary>SkPathRaw::isRect (SkPathPriv::IsRectContour without partial rects): the rectangle a path of lines draws, if it is one.</summary>
    public SkRect? AsRect()
    {
        if (SegmentMask != 1 || Points.Count < 4 || Verbs.Count < 4)
            return null;
        if (TrivialRect() is { } trivial)
            return trivial;
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
        return new SkRect(MathF.Min(firstCorner.X, thirdCorner.X), MathF.Min(firstCorner.Y, thirdCorner.Y),
            MathF.Max(firstCorner.X, thirdCorner.X), MathF.Max(firstCorner.Y, thirdCorner.Y));
    }

    SkRect? TrivialRect()
    {
        if (Points.Count != 4 || Verbs.Count != 5 || Verbs[0] != PathVerb.Move || Verbs[1] != PathVerb.Line
            || Verbs[2] != PathVerb.Line || Verbs[3] != PathVerb.Line || Verbs[4] != PathVerb.Close)
            return null;
        var v0 = Points[1] - Points[0];
        var v1 = Points[2] - Points[1];
        var v2 = Points[3] - Points[2];
        var v3 = Points[0] - Points[3];
        static bool Orthogonal(Vector2 a, Vector2 b) => ((a.X == 0) ^ (b.X == 0)) & ((a.Y == 0) ^ (b.Y == 0));
        if (!(((v0.X == 0) ^ (v0.Y == 0)) & Orthogonal(v0, v1) & Orthogonal(v1, v2) & Orthogonal(v2, v3)))
            return null;
        var a = Points[0];
        var c = Points[2];
        return new SkRect(MathF.Min(a.X, c.X), MathF.Min(a.Y, c.Y), MathF.Max(a.X, c.X), MathF.Max(a.Y, c.Y));
    }
}

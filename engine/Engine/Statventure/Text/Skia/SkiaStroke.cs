using System.Numerics;
using static Statventure.Text.Skia.SkiaGeometry;

namespace Statventure.Text.Skia;

/// <summary>
/// Skia's path stroker (SkStroke.cpp, SkStrokerPriv.cpp at Chromium 152's revision): each contour
/// offset by half the width on both sides, curves approximated by quads that stay within a quarter
/// pixel of the true offset (cubics split at inflections, cusps given a circle), joins and caps as
/// the paint says; stroke-and-fill adds the source path in the stroke's winding.
/// Fake bold uses it (SkScalerContextRec::useStrokeForFakeBold).
/// </summary>
static class SkiaStroke
{
    public enum Cap { Butt, Round, Square }
    public enum Join { Miter, Round, Bevel }

    /// <summary>
    /// The outline of a synthetic bold glyph as Chromium 152 draws it for a web font (Fontations):
    /// stroke-and-fill with a width of the text size times 1/24 (9 px and less) to 1/32 (36 px and
    /// more), miter joins with the default limit 4, butt caps.
    /// </summary>
    public static SkiaPath FakeBold(SkiaPath path, float textSize)
    {
        // SkFloatInterpFunc over kStdFakeBoldInterpKeys {9, 36} and Values {1/24, 1/32}.
        const float key0 = 9, key1 = 36, value0 = 1f / 24, value1 = 1f / 32;
        float scale;
        if (textSize <= key0)
            scale = value0;
        else if (textSize >= key1)
            scale = value1;
        else
        {
            var t = (textSize - key0) / (key1 - key0);
            scale = value0 + (value1 - value0) * t;
        }
        return StrokePath(path, textSize * scale, 4, Cap.Butt, Join.Miter, doFill: true);
    }

    /// <summary>SkStroke::strokePath.</summary>
    public static SkiaPath StrokePath(SkiaPath src, float width, float miterLimit, Cap cap, Join join, bool doFill, float resScale = 1)
    {
        var dst = new SkiaPath();
        var radius = width / 2f;
        if (radius <= 0)
            return dst;
        if (src.IsRect() is { IsClosed: true } rect)
        {
            StrokeRect(rect.Rect, dst, rect.Clockwise, width, miterLimit, join, doFill);
            return dst;
        }
        var ignoreCenter = doFill && src.SegmentMask == 1 && src.IsLastContourClosed && src.IsConvex;
        var stroker = new PathStroker(src, radius, miterLimit, cap, join, resScale, ignoreCenter);
        var iter = new PathIter(src);
        var lastSegment = PathVerb.Move;
        Span<Vector2> pts = stackalloc Vector2[4];
        while (iter.Next(pts, out var weight) is { } verb)
        {
            switch (verb)
            {
                case PathVerb.Move:
                    stroker.MoveTo(pts[0]);
                    break;
                case PathVerb.Line:
                    stroker.LineTo(pts[1], iter);
                    lastSegment = PathVerb.Line;
                    break;
                case PathVerb.Quad:
                    stroker.QuadTo(pts[1], pts[2]);
                    lastSegment = PathVerb.Quad;
                    break;
                case PathVerb.Conic:
                    stroker.ConicTo(pts[1], pts[2], weight);
                    lastSegment = PathVerb.Conic;
                    break;
                case PathVerb.Cubic:
                    stroker.CubicTo(pts[1], pts[2], pts[3]);
                    lastSegment = PathVerb.Cubic;
                    break;
                case PathVerb.Close:
                    if (cap != Cap.Butt)
                    {
                        if (stroker.HasOnlyMoveTo)
                        {
                            stroker.LineTo(stroker.MoveToPt);
                            lastSegment = PathVerb.Line;
                            break;
                        }
                        if (stroker.IsCurrentContourEmpty)
                        {
                            lastSegment = PathVerb.Line;
                            break;
                        }
                    }
                    stroker.Close(lastSegment == PathVerb.Line);
                    break;
            }
        }
        stroker.Done(dst, lastSegment == PathVerb.Line);
        if (doFill && !ignoreCenter)
        {
            if (src.FirstDirectionClockwise() == false)
                dst.ReverseAddPath(src);
            else
                dst.AddPath(src);
        }
        return dst;
    }

    /// <summary>SkStroke::strokeRect.</summary>
    static void StrokeRect(SkRect rect, SkiaPath dst, bool clockwise, float width, float miterLimit, Join join, bool doFill)
    {
        dst.Reset();
        var radius = width / 2f;
        if (radius <= 0)
            return;
        var rw = rect.Right - rect.Left;
        var rh = rect.Bottom - rect.Top;
        if ((rw < 0) ^ (rh < 0))
            clockwise = !clockwise;
        rect = new SkRect(MathF.Min(rect.Left, rect.Right), MathF.Min(rect.Top, rect.Bottom), MathF.Max(rect.Left, rect.Right), MathF.Max(rect.Top, rect.Bottom));
        rw = rect.Right - rect.Left;
        rh = rect.Bottom - rect.Top;
        var r = new SkRect(rect.Left - radius, rect.Top - radius, rect.Right + radius, rect.Bottom + radius);
        if (join == Join.Miter && miterLimit < 1.41421356f)
            join = Join.Bevel;
        switch (join)
        {
            case Join.Miter:
                dst.AddRect(r, clockwise);
                break;
            case Join.Bevel:
            {
                Span<Vector2> pts = stackalloc Vector2[8];
                if (clockwise)
                {
                    pts[0] = new(rect.Left, r.Top);
                    pts[1] = new(rect.Right, r.Top);
                    pts[2] = new(r.Right, rect.Top);
                    pts[3] = new(r.Right, rect.Bottom);
                    pts[4] = new(rect.Right, r.Bottom);
                    pts[5] = new(rect.Left, r.Bottom);
                    pts[6] = new(r.Left, rect.Bottom);
                    pts[7] = new(r.Left, rect.Top);
                }
                else
                {
                    pts[7] = new(rect.Left, r.Top);
                    pts[6] = new(rect.Right, r.Top);
                    pts[5] = new(r.Right, rect.Top);
                    pts[4] = new(r.Right, rect.Bottom);
                    pts[3] = new(rect.Right, r.Bottom);
                    pts[2] = new(rect.Left, r.Bottom);
                    pts[1] = new(r.Left, rect.Bottom);
                    pts[0] = new(r.Left, rect.Top);
                }
                dst.AddPolygon(pts, true);
                break;
            }
            case Join.Round:
                // (A rounded rect; fake bold never asks for it.)
                throw new NotSupportedException("round joins of a stroked rect");
        }
        if (width < MathF.Min(rw, rh) && !doFill)
        {
            var inner = new SkRect(rect.Left + radius, rect.Top + radius, rect.Right - radius, rect.Bottom - radius);
            dst.AddRect(inner, !clockwise);
        }
    }

    /// <summary>SkPath::Iter without forced closing: a close first yields the line back to the contour's start, if any.</summary>
    sealed class PathIter
    {
        readonly SkiaPath _path;
        int _verb, _point, _conic;
        Vector2 _moveTo, _lastPt;
        bool _needClose;

        public PathIter(SkiaPath path) => _path = path;

        PathIter(PathIter other)
        {
            _path = other._path;
            _verb = other._verb;
            _point = other._point;
            _conic = other._conic;
            _moveTo = other._moveTo;
            _lastPt = other._lastPt;
            _needClose = other._needClose;
        }

        public PathIter Copy() => new(this);

        PathVerb AutoClose(Span<Vector2> pts)
        {
            if (_lastPt != _moveTo)
            {
                if (float.IsNaN(_lastPt.X) || float.IsNaN(_lastPt.Y) || float.IsNaN(_moveTo.X) || float.IsNaN(_moveTo.Y))
                    return PathVerb.Close;
                pts[0] = _lastPt;
                pts[1] = _moveTo;
                _lastPt = _moveTo;
                return PathVerb.Line;
            }
            pts[0] = _moveTo;
            return PathVerb.Close;
        }

        public PathVerb? Next(Span<Vector2> pts, out float weight)
        {
            weight = 1;
            var verbs = _path.Verbs;
            var points = _path.Points;
            if (_verb == verbs.Count)
            {
                if (_needClose)
                {
                    if (AutoClose(pts) == PathVerb.Line)
                        return PathVerb.Line;
                    _needClose = false;
                    return PathVerb.Close;
                }
                return null;
            }
            var verb = verbs[_verb++];
            switch (verb)
            {
                case PathVerb.Move:
                    if (_needClose)
                    {
                        _verb--;
                        verb = AutoClose(pts);
                        if (verb == PathVerb.Close)
                            _needClose = false;
                        return verb;
                    }
                    if (_verb == verbs.Count)
                        return null;
                    _moveTo = points[_point];
                    pts[0] = _moveTo;
                    _point++;
                    _lastPt = _moveTo;
                    _needClose = false;
                    break;
                case PathVerb.Line:
                    pts[0] = _lastPt;
                    pts[1] = points[_point];
                    _lastPt = points[_point];
                    _point++;
                    break;
                case PathVerb.Conic:
                case PathVerb.Quad:
                    if (verb == PathVerb.Conic)
                        weight = _path.ConicWeights[_conic++];
                    pts[0] = _lastPt;
                    pts[1] = points[_point];
                    pts[2] = points[_point + 1];
                    _lastPt = points[_point + 1];
                    _point += 2;
                    break;
                case PathVerb.Cubic:
                    pts[0] = _lastPt;
                    pts[1] = points[_point];
                    pts[2] = points[_point + 1];
                    pts[3] = points[_point + 2];
                    _lastPt = points[_point + 2];
                    _point += 3;
                    break;
                case PathVerb.Close:
                    verb = AutoClose(pts);
                    if (verb == PathVerb.Line)
                        _verb--;
                    else
                        _needClose = false;
                    _lastPt = _moveTo;
                    break;
            }
            return verb;
        }

        /// <summary>has_valid_tangent: whether the rest of the contour has a segment of non-zero length.</summary>
        public bool HasValidTangent()
        {
            var copy = Copy();
            Span<Vector2> pts = stackalloc Vector2[4];
            while (copy.Next(pts, out _) is { } verb)
            {
                switch (verb)
                {
                    case PathVerb.Move:
                        return false;
                    case PathVerb.Line:
                        if (pts[0] == pts[1])
                            continue;
                        return true;
                    case PathVerb.Quad:
                    case PathVerb.Conic:
                        if (pts[0] == pts[1] && pts[0] == pts[2])
                            continue;
                        return true;
                    case PathVerb.Cubic:
                        if (pts[0] == pts[1] && pts[0] == pts[2] && pts[0] == pts[3])
                            continue;
                        return true;
                    case PathVerb.Close:
                        return false;
                }
            }
            return false;
        }
    }

    /// <summary>SkQuadConstruct: the stroke quad being built for a t range of the curve.</summary>
    struct QuadConstruct
    {
        public Vector2 Q0, Q1, Q2;
        public Vector2 TangentStart, TangentEnd;
        public float StartT, MidT, EndT;
        public bool StartSet, EndSet, OppositeTangents;

        public bool Init(float start, float end)
        {
            StartT = start;
            MidT = (start + end) * 0.5f;
            EndT = end;
            StartSet = EndSet = false;
            return StartT < MidT && MidT < EndT;
        }

        public bool InitWithStart(in QuadConstruct parent)
        {
            if (!Init(parent.StartT, parent.MidT))
                return false;
            Q0 = parent.Q0;
            TangentStart = parent.TangentStart;
            StartSet = true;
            return true;
        }

        public bool InitWithEnd(in QuadConstruct parent)
        {
            if (!Init(parent.MidT, parent.EndT))
                return false;
            Q2 = parent.Q2;
            TangentEnd = parent.TangentEnd;
            EndSet = true;
            return true;
        }
    }

    enum ResultType { Split, Degenerate, Quad }

    enum ReductionType { Point, Line, Quad, Degenerate, Degenerate2, Degenerate3 }

    delegate void JoinProc(ref SkiaPath outer, ref SkiaPath inner, Vector2 beforeUnitNormal, Vector2 pivot, Vector2 afterUnitNormal,
        float radius, float invMiterLimit, bool prevIsLine, bool currIsLine);

    delegate void CapProc(SkiaPath sink, Vector2 pivot, Vector2 normal, Vector2 stop, bool extendLastPt);

    // ---------- Joiners and cappers (SkStrokerPriv.cpp) ----------

    static void ButtCapper(SkiaPath sink, Vector2 pivot, Vector2 normal, Vector2 stop, bool extendLastPt) => sink.LineTo(stop);

    static void RoundCapper(SkiaPath sink, Vector2 pivot, Vector2 normal, Vector2 stop, bool extendLastPt)
    {
        var parallel = RotateCW(normal);
        var projectedCenter = pivot + parallel;
        sink.ConicTo(projectedCenter + normal, projectedCenter, Root2Over2);
        sink.ConicTo(projectedCenter - normal, stop, Root2Over2);
    }

    static void SquareCapper(SkiaPath sink, Vector2 pivot, Vector2 normal, Vector2 stop, bool extendLastPt)
    {
        var parallel = RotateCW(normal);
        if (extendLastPt)
        {
            sink.SetLastPoint(pivot + normal + parallel);
            sink.LineTo(pivot - normal + parallel);
        }
        else
        {
            sink.LineTo(pivot + normal + parallel);
            sink.LineTo(pivot - normal + parallel);
            sink.LineTo(stop);
        }
    }

    static bool IsClockwise(Vector2 before, Vector2 after) => before.X * after.Y > before.Y * after.X;

    enum AngleType { Nearly180, Sharp, Shallow, NearlyLine }

    static AngleType Dot2AngleType(float dot)
    {
        if (dot >= 0)
            return MathF.Abs(1 - dot) <= NearlyZero ? AngleType.NearlyLine : AngleType.Shallow;
        return MathF.Abs(1 + dot) <= NearlyZero ? AngleType.Nearly180 : AngleType.Sharp;
    }

    static void HandleInnerJoin(SkiaPath inner, Vector2 pivot, Vector2 after)
    {
        inner.LineTo(pivot);
        inner.LineTo(pivot - after);
    }

    static void BluntJoiner(ref SkiaPath outer, ref SkiaPath inner, Vector2 beforeUnitNormal, Vector2 pivot, Vector2 afterUnitNormal,
        float radius, float invMiterLimit, bool prevIsLine, bool currIsLine)
    {
        var after = afterUnitNormal * radius;
        var o = outer;
        var i = inner;
        if (!IsClockwise(beforeUnitNormal, afterUnitNormal))
        {
            (o, i) = (i, o);
            after = -after;
        }
        o.LineTo(pivot + after);
        HandleInnerJoin(i, pivot, after);
    }

    static void RoundJoiner(ref SkiaPath outer, ref SkiaPath inner, Vector2 beforeUnitNormal, Vector2 pivot, Vector2 afterUnitNormal,
        float radius, float invMiterLimit, bool prevIsLine, bool currIsLine)
    {
        var dotProd = Dot(beforeUnitNormal, afterUnitNormal);
        if (Dot2AngleType(dotProd) == AngleType.NearlyLine)
            return;
        var before = beforeUnitNormal;
        var after = afterUnitNormal;
        var clockwise = true;
        var o = outer;
        var i = inner;
        if (!IsClockwise(before, after))
        {
            (o, i) = (i, o);
            before = -before;
            after = -after;
            clockwise = false;
        }
        Span<Conic> conics = stackalloc Conic[5];
        var count = BuildUnitArc(before, after, clockwise, radius, pivot, conics);
        if (count > 0)
        {
            for (var k = 0; k < count; k++)
                o.ConicTo(conics[k].P1, conics[k].P2, conics[k].W);
            after *= radius;
            HandleInnerJoin(i, pivot, after);
        }
    }

    static void MiterJoiner(ref SkiaPath outer, ref SkiaPath inner, Vector2 beforeUnitNormal, Vector2 pivot, Vector2 afterUnitNormal,
        float radius, float invMiterLimit, bool prevIsLine, bool currIsLine)
    {
        const float oneOverSqrt2 = 0.707106781f;
        var dotProd = Dot(beforeUnitNormal, afterUnitNormal);
        var angleType = Dot2AngleType(dotProd);
        var before = beforeUnitNormal;
        var after = afterUnitNormal;
        var o = outer;
        var i = inner;
        Vector2 mid;
        if (angleType == AngleType.NearlyLine)
            return;
        var blunt = false;
        if (angleType == AngleType.Nearly180)
        {
            currIsLine = false;
            blunt = true;
        }
        var ccw = false;
        if (!blunt)
        {
            ccw = !IsClockwise(before, after);
            if (ccw)
            {
                (o, i) = (i, o);
                before = -before;
                after = -after;
            }
        }
        if (!blunt)
        {
            if (dotProd == 0 && invMiterLimit <= oneOverSqrt2)
                mid = (before + after) * radius;
            else
            {
                var sinHalfAngle = MathF.Sqrt((1 + dotProd) / 2f);
                if (sinHalfAngle < invMiterLimit)
                {
                    currIsLine = false;
                    blunt = true;
                    mid = default;
                }
                else
                {
                    if (angleType == AngleType.Sharp)
                    {
                        mid = new Vector2(after.Y - before.Y, before.X - after.X);
                        if (ccw)
                            mid = -mid;
                    }
                    else
                        mid = before + after;
                    SetLength(ref mid, radius / sinHalfAngle);
                }
            }
            if (!blunt)
            {
                if (prevIsLine)
                    o.SetLastPoint(pivot + mid);
                else
                    o.LineTo(pivot + mid);
            }
        }
        after *= radius;
        if (!currIsLine)
            o.LineTo(pivot + after);
        HandleInnerJoin(i, pivot, after);
    }

    static CapProc CapFactory(Cap cap) => cap switch { Cap.Round => RoundCapper, Cap.Square => SquareCapper, _ => ButtCapper };

    static JoinProc JoinFactory(Join join) => join switch { Join.Round => RoundJoiner, Join.Bevel => BluntJoiner, _ => MiterJoiner };

    // ---------- SkPathStroker ----------

    sealed class PathStroker
    {
        static readonly int[] RecursiveLimits = [5 * 3, 24, 11 * 3, 11 * 3];
        const int TangentLimit = 0, CubicLimit = 1, ConicLimit = 2, QuadLimit = 3;

        readonly float _radius, _invMiterLimit, _resScale, _invResScale, _invResScaleSquared;
        Vector2 _firstNormal, _prevNormal, _firstUnitNormal, _prevUnitNormal;
        Vector2 _firstPt, _prevPt, _firstOuterPt;
        int _firstOuterPtIndexInContour;
        int _segmentCount;
        bool _prevIsLine;
        readonly bool _canIgnoreCenter;
        readonly CapProc _capper;
        readonly bool _buttCap;
        JoinProc _joiner;
        SkiaPath _inner = new(), _outer = new(), _cusper = new();
        int _strokeType;
        int _recursionDepth;
        bool _foundTangents, _joinCompleted;

        public PathStroker(SkiaPath src, float radius, float miterLimit, Cap cap, Join join, float resScale, bool canIgnoreCenter)
        {
            _radius = radius;
            _resScale = resScale;
            _canIgnoreCenter = canIgnoreCenter;
            _invMiterLimit = 0;
            if (join == Join.Miter)
            {
                if (miterLimit <= 1)
                    join = Join.Bevel;
                else
                    _invMiterLimit = 1 / miterLimit;
            }
            _capper = CapFactory(cap);
            _buttCap = cap == Cap.Butt;
            _joiner = JoinFactory(join);
            _segmentCount = -1;
            _firstOuterPtIndexInContour = 0;
            _prevIsLine = false;
            _invResScale = 1 / (resScale * 4);
            _invResScaleSquared = _invResScale * _invResScale;
            _recursionDepth = 0;
        }

        public bool HasOnlyMoveTo => _segmentCount == 0;
        public Vector2 MoveToPt => _firstPt;

        public void Close(bool isLine) => FinishContour(true, isLine);

        public void Done(SkiaPath dst, bool isLine)
        {
            FinishContour(false, isLine);
            dst.Reset();
            dst.AddPath(_outer);
            _outer = new SkiaPath();
        }

        static bool IsZeroLengthSincePoint(List<Vector2> points, int start)
        {
            var count = points.Count - start;
            if (count < 2)
                return true;
            var first = points[start];
            for (var i = 1; i < count; i++)
                if (first != points[start + i])
                    return false;
            return true;
        }

        public bool IsCurrentContourEmpty =>
            IsZeroLengthSincePoint(_inner.Points, 0) && IsZeroLengthSincePoint(_outer.Points, _firstOuterPtIndexInContour);

        static bool SetNormalUnitNormal(Vector2 before, Vector2 after, float scale, float radius, out Vector2 normal, out Vector2 unitNormal)
        {
            unitNormal = default;
            if (!SetNormalize(ref unitNormal, (after.X - before.X) * scale, (after.Y - before.Y) * scale))
            {
                normal = default;
                return false;
            }
            unitNormal = RotateCCW(unitNormal);
            normal = unitNormal * radius;
            return true;
        }

        static bool SetNormalUnitNormal(Vector2 vec, float radius, out Vector2 normal, out Vector2 unitNormal)
        {
            unitNormal = default;
            if (!SetNormalize(ref unitNormal, vec.X, vec.Y))
            {
                normal = default;
                return false;
            }
            unitNormal = RotateCCW(unitNormal);
            normal = unitNormal * radius;
            return true;
        }

        bool PreJoinTo(Vector2 currPt, out Vector2 normal, out Vector2 unitNormal, bool currIsLine)
        {
            if (!SetNormalUnitNormal(_prevPt, currPt, _resScale, _radius, out normal, out unitNormal))
            {
                if (_buttCap)
                    return false;
                normal = new Vector2(_radius, 0);
                unitNormal = new Vector2(1, 0);
            }
            if (_segmentCount == 0)
            {
                _firstNormal = normal;
                _firstUnitNormal = unitNormal;
                _firstOuterPt = _prevPt + normal;
                _outer.MoveTo(_firstOuterPt);
                _inner.MoveTo(_prevPt - normal);
            }
            else
                _joiner(ref _outer, ref _inner, _prevUnitNormal, _prevPt, unitNormal, _radius, _invMiterLimit, _prevIsLine, currIsLine);
            _prevIsLine = currIsLine;
            return true;
        }

        void PostJoinTo(Vector2 currPt, Vector2 normal, Vector2 unitNormal)
        {
            _joinCompleted = true;
            _prevPt = currPt;
            _prevUnitNormal = unitNormal;
            _prevNormal = normal;
            _segmentCount += 1;
        }

        void FinishContour(bool close, bool currIsLine)
        {
            if (_segmentCount > 0)
            {
                if (close)
                {
                    _joiner(ref _outer, ref _inner, _prevUnitNormal, _prevPt, _firstUnitNormal, _radius, _invMiterLimit, _prevIsLine, currIsLine);
                    _outer.Close();
                    if (_canIgnoreCenter)
                    {
                        if (Contains(Bounds(_inner), Bounds(_outer)))
                        {
                            _outer.Reset();
                            _outer.AddPath(_inner);
                        }
                    }
                    else if (_inner.LastPoint is { } pt)
                    {
                        _outer.MoveTo(pt);
                        _outer.ReversePathTo(_inner);
                        _outer.Close();
                    }
                }
                else if (_inner.LastPoint is { } pt)
                {
                    _capper(_outer, _prevPt, _prevNormal, pt, currIsLine);
                    _outer.ReversePathTo(_inner);
                    _capper(_outer, _firstPt, -_firstNormal, _firstOuterPt, _prevIsLine);
                    _outer.Close();
                }
                if (!_cusper.IsEmpty)
                {
                    _outer.AddPath(_cusper);
                    _cusper = new SkiaPath();
                }
            }
            _inner.Reset();
            _segmentCount = -1;
            _firstOuterPtIndexInContour = _outer.CountPoints;
        }

        /// <summary>SkPathBuilder::computeBounds: the points' bounds (empty for no points).</summary>
        static SkRect Bounds(SkiaPath path) => path.Points.Count == 0 ? new SkRect(0, 0, 0, 0) : path.Bounds;

        /// <summary>SkRect::contains(SkRect): non-empty and inside.</summary>
        static bool Contains(SkRect a, SkRect b) =>
            !a.IsEmpty && !b.IsEmpty && a.Left <= b.Left && a.Top <= b.Top && a.Right >= b.Right && a.Bottom >= b.Bottom;

        public void MoveTo(Vector2 pt)
        {
            if (_segmentCount > 0)
                FinishContour(false, false);
            _segmentCount = 0;
            _firstPt = _prevPt = pt;
            _joinCompleted = false;
        }

        void LineToPoint(Vector2 currPt, Vector2 normal)
        {
            _outer.LineTo(currPt + normal);
            _inner.LineTo(currPt - normal);
        }

        public void LineTo(Vector2 currPt, PathIter? iter = null)
        {
            var teenyLine = EqualsWithinTolerance(_prevPt, currPt, NearlyZero * _invResScale);
            if (_buttCap && teenyLine)
                return;
            if (teenyLine && (_joinCompleted || (iter is not null && iter.HasValidTangent())))
                return;
            if (!PreJoinTo(currPt, out var normal, out var unitNormal, true))
                return;
            LineToPoint(currPt, normal);
            PostJoinTo(currPt, normal, unitNormal);
        }

        void SetQuadEndNormal(ReadOnlySpan<Vector2> quad, Vector2 normalAB, Vector2 unitNormalAB, out Vector2 normalBC, out Vector2 unitNormalBC)
        {
            if (!SetNormalUnitNormal(quad[1], quad[2], _resScale, _radius, out normalBC, out unitNormalBC))
            {
                normalBC = normalAB;
                unitNormalBC = unitNormalAB;
            }
        }

        void SetCubicEndNormal(ReadOnlySpan<Vector2> cubic, Vector2 normalAB, Vector2 unitNormalAB, out Vector2 normalCD, out Vector2 unitNormalCD)
        {
            var ab = cubic[1] - cubic[0];
            var cd = cubic[3] - cubic[2];
            var degenerateAB = !CanNormalize(ab.X, ab.Y);
            var degenerateCD = !CanNormalize(cd.X, cd.Y);
            if (!(degenerateAB && degenerateCD))
            {
                if (degenerateAB)
                {
                    ab = cubic[2] - cubic[0];
                    degenerateAB = !CanNormalize(ab.X, ab.Y);
                }
                if (degenerateCD)
                {
                    cd = cubic[3] - cubic[1];
                    degenerateCD = !CanNormalize(cd.X, cd.Y);
                }
                if (!degenerateAB && !degenerateCD)
                {
                    SetNormalUnitNormal(cd, _radius, out normalCD, out unitNormalCD);
                    return;
                }
            }
            normalCD = normalAB;
            unitNormalCD = unitNormalAB;
        }

        void Init(int strokeType, ref QuadConstruct quadPts, float tStart, float tEnd)
        {
            _strokeType = strokeType;
            _foundTangents = false;
            quadPts.Init(tStart, tEnd);
        }

        static float PtToLine(Vector2 pt, Vector2 lineStart, Vector2 lineEnd)
        {
            var dxy = lineEnd - lineStart;
            var ab0 = pt - lineStart;
            var numer = Dot(dxy, ab0);
            var denom = Dot(dxy, dxy);
            var t = numer / denom;
            if (t >= 0 && t <= 1)
            {
                var hit = lineStart * (1 - t) + lineEnd * t;
                return DistanceToSqd(hit, pt);
            }
            return DistanceToSqd(pt, lineStart);
        }

        static float PtToTangentLine(Vector2 pt, Vector2 lineStart, Vector2 tangent)
        {
            var ab0 = pt - lineStart;
            var numer = Dot(tangent, ab0);
            var denom = Dot(tangent, tangent);
            var t = numer / denom;
            if (t >= 0 && t <= 1)
            {
                var hit = lineStart + tangent * t;
                return DistanceToSqd(hit, pt);
            }
            return DistanceToSqd(pt, lineStart);
        }

        static bool CubicInLine(ReadOnlySpan<Vector2> cubic)
        {
            var ptMax = -1f;
            int outer1 = 0, outer2 = 0;
            for (var index = 0; index < 3; ++index)
                for (var inner = index + 1; inner < 4; ++inner)
                {
                    var testDiff = cubic[inner] - cubic[index];
                    var testMax = MathF.Max(MathF.Abs(testDiff.X), MathF.Abs(testDiff.Y));
                    if (ptMax < testMax)
                    {
                        outer1 = index;
                        outer2 = inner;
                        ptMax = testMax;
                    }
                }
            var mid1 = (1 + (2 >> outer2)) >> outer1;
            var mid2 = outer1 ^ outer2 ^ mid1;
            var lineSlop = ptMax * ptMax * 0.00001f;
            return PtToLine(cubic[mid1], cubic[outer1], cubic[outer2]) <= lineSlop
                && PtToLine(cubic[mid2], cubic[outer1], cubic[outer2]) <= lineSlop;
        }

        static bool QuadInLine(ReadOnlySpan<Vector2> quad)
        {
            var ptMax = -1f;
            int outer1 = 0, outer2 = 0;
            for (var index = 0; index < 2; ++index)
                for (var inner = index + 1; inner < 3; ++inner)
                {
                    var testDiff = quad[inner] - quad[index];
                    var testMax = MathF.Max(MathF.Abs(testDiff.X), MathF.Abs(testDiff.Y));
                    if (ptMax < testMax)
                    {
                        outer1 = index;
                        outer2 = inner;
                        ptMax = testMax;
                    }
                }
            var mid = outer1 ^ outer2 ^ 3;
            const float curvatureSlop = 0.000005f;
            var lineSlop = ptMax * ptMax * curvatureSlop;
            return PtToLine(quad[mid], quad[outer1], quad[outer2]) <= lineSlop;
        }

        static ReductionType CheckCubicLinear(ReadOnlySpan<Vector2> cubic, Span<Vector2> reduction, out int tangentPt)
        {
            tangentPt = 1;
            var degenerateAB = !CanNormalize(cubic[1].X - cubic[0].X, cubic[1].Y - cubic[0].Y);
            var degenerateBC = !CanNormalize(cubic[2].X - cubic[1].X, cubic[2].Y - cubic[1].Y);
            var degenerateCD = !CanNormalize(cubic[3].X - cubic[2].X, cubic[3].Y - cubic[2].Y);
            if (degenerateAB & degenerateBC & degenerateCD)
                return ReductionType.Point;
            if ((degenerateAB ? 1 : 0) + (degenerateBC ? 1 : 0) + (degenerateCD ? 1 : 0) == 2)
                return ReductionType.Line;
            if (!CubicInLine(cubic))
            {
                tangentPt = degenerateAB ? 2 : 1;
                return ReductionType.Quad;
            }
            Span<float> tValues = stackalloc float[3];
            var count = FindCubicMaxCurvature(cubic, tValues);
            var rCount = 0;
            for (var index = 0; index < count; ++index)
            {
                var t = tValues[index];
                if (0 >= t || t >= 1)
                    continue;
                reduction[rCount] = EvalCubicAt(cubic, t);
                if (reduction[rCount] != cubic[0] && reduction[rCount] != cubic[3])
                    ++rCount;
            }
            if (rCount == 0)
                return ReductionType.Line;
            return ReductionType.Quad + rCount;
        }

        static ReductionType CheckConicLinear(Conic conic, out Vector2 reduction)
        {
            reduction = default;
            var degenerateAB = !CanNormalize(conic.P1.X - conic.P0.X, conic.P1.Y - conic.P0.Y);
            var degenerateBC = !CanNormalize(conic.P2.X - conic.P1.X, conic.P2.Y - conic.P1.Y);
            if (degenerateAB & degenerateBC)
                return ReductionType.Point;
            if (degenerateAB | degenerateBC)
                return ReductionType.Line;
            ReadOnlySpan<Vector2> pts = [conic.P0, conic.P1, conic.P2];
            if (!QuadInLine(pts))
                return ReductionType.Quad;
            var t = FindQuadMaxCurvature(pts);
            if (t == 0 || float.IsNaN(t))
                return ReductionType.Line;
            reduction = conic.EvalAt(t);
            return ReductionType.Degenerate;
        }

        static ReductionType CheckQuadLinear(ReadOnlySpan<Vector2> quad, out Vector2 reduction)
        {
            reduction = default;
            var degenerateAB = !CanNormalize(quad[1].X - quad[0].X, quad[1].Y - quad[0].Y);
            var degenerateBC = !CanNormalize(quad[2].X - quad[1].X, quad[2].Y - quad[1].Y);
            if (degenerateAB & degenerateBC)
                return ReductionType.Point;
            if (degenerateAB | degenerateBC)
                return ReductionType.Line;
            if (!QuadInLine(quad))
                return ReductionType.Quad;
            var t = FindQuadMaxCurvature(quad);
            if (t == 0 || t == 1)
                return ReductionType.Line;
            reduction = EvalQuadAt(quad, t);
            return ReductionType.Degenerate;
        }

        /// <summary>Two lines through a point between them, with round joins (degenerate curves).</summary>
        void LinesWithRoundJoin(ReadOnlySpan<Vector2> through, Vector2 end)
        {
            LineTo(through[0]);
            var saveJoiner = _joiner;
            _joiner = RoundJoiner;
            for (var i = 1; i < through.Length; i++)
                LineTo(through[i]);
            LineTo(end);
            _joiner = saveJoiner;
        }

        public void ConicTo(Vector2 pt1, Vector2 pt2, float weight)
        {
            var conic = new Conic(_prevPt, pt1, pt2, weight);
            var reductionType = CheckConicLinear(conic, out var reduction);
            if (reductionType is ReductionType.Point or ReductionType.Line)
            {
                LineTo(pt2);
                return;
            }
            if (reductionType == ReductionType.Degenerate)
            {
                LinesWithRoundJoin([reduction], pt2);
                return;
            }
            if (!PreJoinTo(pt1, out var normalAB, out var unitAB, false))
            {
                LineTo(pt2);
                return;
            }
            var quadPts = new QuadConstruct();
            Init(1, ref quadPts, 0, 1);
            ConicStroke(conic, ref quadPts);
            Init(-1, ref quadPts, 0, 1);
            ConicStroke(conic, ref quadPts);
            SetQuadEndNormal([conic.P0, conic.P1, conic.P2], normalAB, unitAB, out var normalBC, out var unitBC);
            PostJoinTo(pt2, normalBC, unitBC);
        }

        public void QuadTo(Vector2 pt1, Vector2 pt2)
        {
            ReadOnlySpan<Vector2> quad = [_prevPt, pt1, pt2];
            var reductionType = CheckQuadLinear(quad, out var reduction);
            if (reductionType is ReductionType.Point or ReductionType.Line)
            {
                LineTo(pt2);
                return;
            }
            if (reductionType == ReductionType.Degenerate)
            {
                LinesWithRoundJoin([reduction], pt2);
                return;
            }
            if (!PreJoinTo(pt1, out var normalAB, out var unitAB, false))
            {
                LineTo(pt2);
                return;
            }
            var quadPts = new QuadConstruct();
            Init(1, ref quadPts, 0, 1);
            QuadStroke(quad, ref quadPts);
            Init(-1, ref quadPts, 0, 1);
            QuadStroke(quad, ref quadPts);
            SetQuadEndNormal(quad, normalAB, unitAB, out var normalBC, out var unitBC);
            PostJoinTo(pt2, normalBC, unitBC);
        }

        void SetRayPts(Vector2 tPt, ref Vector2 dxy, out Vector2 onPt, out Vector2 tangent)
        {
            if (!SetLength(ref dxy, _radius))
                dxy = new Vector2(_radius, 0);
            float axisFlip = _strokeType;
            onPt = new Vector2(tPt.X + axisFlip * dxy.Y, tPt.Y - axisFlip * dxy.X);
            tangent = dxy;
        }

        void ConicPerpRay(Conic conic, float t, out Vector2 tPt, out Vector2 onPt, out Vector2 tangent)
        {
            tPt = conic.EvalAt(t);
            var dxy = conic.EvalTangentAt(t);
            if (dxy == Vector2.Zero)
                dxy = conic.P2 - conic.P0;
            SetRayPts(tPt, ref dxy, out onPt, out tangent);
        }

        void ConicQuadEnds(Conic conic, ref QuadConstruct quadPts)
        {
            if (!quadPts.StartSet)
            {
                ConicPerpRay(conic, quadPts.StartT, out _, out quadPts.Q0, out quadPts.TangentStart);
                quadPts.StartSet = true;
            }
            if (!quadPts.EndSet)
            {
                ConicPerpRay(conic, quadPts.EndT, out _, out quadPts.Q2, out quadPts.TangentEnd);
                quadPts.EndSet = true;
            }
        }

        void CubicPerpRay(ReadOnlySpan<Vector2> cubic, float t, out Vector2 tPt, out Vector2 onPt, out Vector2 tangent)
        {
            tPt = EvalCubicAt(cubic, t);
            var dxy = EvalCubicTangentAt(cubic, t);
            if (dxy == Vector2.Zero)
            {
                Span<Vector2> chopped = stackalloc Vector2[7];
                var usesChopped = false;
                if (MathF.Abs(t) <= NearlyZero)
                    dxy = cubic[2] - cubic[0];
                else if (MathF.Abs(1 - t) <= NearlyZero)
                    dxy = cubic[3] - cubic[1];
                else
                {
                    ChopCubicAt(cubic, chopped, t);
                    dxy = chopped[3] - chopped[2];
                    if (dxy == Vector2.Zero)
                    {
                        dxy = chopped[3] - chopped[1];
                        usesChopped = true;
                    }
                }
                if (dxy == Vector2.Zero)
                    dxy = usesChopped ? chopped[3] - chopped[0] : cubic[3] - cubic[0];
            }
            SetRayPts(tPt, ref dxy, out onPt, out tangent);
        }

        void CubicQuadEnds(ReadOnlySpan<Vector2> cubic, ref QuadConstruct quadPts)
        {
            if (!quadPts.StartSet)
            {
                CubicPerpRay(cubic, quadPts.StartT, out _, out quadPts.Q0, out quadPts.TangentStart);
                quadPts.StartSet = true;
            }
            if (!quadPts.EndSet)
            {
                CubicPerpRay(cubic, quadPts.EndT, out _, out quadPts.Q2, out quadPts.TangentEnd);
                quadPts.EndSet = true;
            }
        }

        void QuadPerpRay(ReadOnlySpan<Vector2> quad, float t, out Vector2 tPt, out Vector2 onPt, out Vector2 tangent)
        {
            tPt = EvalQuadAt(quad, t);
            var dxy = EvalQuadTangentAt(quad, t);
            if (dxy == Vector2.Zero)
                dxy = quad[2] - quad[0];
            SetRayPts(tPt, ref dxy, out onPt, out tangent);
        }

        /// <summary>The intersection of the stroke's end tangents: a quad, a line, or a split.</summary>
        ResultType IntersectRay(ref QuadConstruct quadPts, bool ctrlPt)
        {
            var start = quadPts.Q0;
            var end = quadPts.Q2;
            var aLen = quadPts.TangentStart;
            var bLen = quadPts.TangentEnd;
            var denom = Cross(aLen, bLen);
            if (denom == 0 || !float.IsFinite(denom))
            {
                quadPts.OppositeTangents = Dot(aLen, bLen) < 0;
                return ResultType.Degenerate;
            }
            quadPts.OppositeTangents = false;
            var ab0 = start - end;
            var numerA = Cross(bLen, ab0);
            var numerB = Cross(aLen, ab0);
            if ((numerA >= 0) == (numerB >= 0))
            {
                var dist1 = PtToTangentLine(start, end, quadPts.TangentEnd);
                var dist2 = PtToTangentLine(end, start, quadPts.TangentStart);
                if (MathF.Max(dist1, dist2) <= _invResScaleSquared)
                    return ResultType.Degenerate;
                return ResultType.Split;
            }
            numerA /= denom;
            var validDivide = numerA > numerA - 1;
            if (validDivide)
            {
                if (ctrlPt)
                    quadPts.Q1 = start + quadPts.TangentStart * numerA;
                return ResultType.Quad;
            }
            quadPts.OppositeTangents = Dot(aLen, bLen) < 0;
            return ResultType.Degenerate;
        }

        ResultType TangentsMeet(ReadOnlySpan<Vector2> cubic, ref QuadConstruct quadPts)
        {
            CubicQuadEnds(cubic, ref quadPts);
            return IntersectRay(ref quadPts, false);
        }

        static int IntersectQuadRay(Vector2 line0, Vector2 line1, ReadOnlySpan<Vector2> quad, Span<float> roots)
        {
            var vec = line1 - line0;
            Span<float> r = stackalloc float[3];
            for (var n = 0; n < 3; ++n)
                r[n] = Cross(vec, quad[n] - line0);
            var a = r[2];
            var b = r[1];
            var c = r[0];
            a += c - 2 * b;
            b -= c;
            return FindUnitQuadRoots(a, 2 * b, c, roots);
        }

        bool PtInQuadBounds(ReadOnlySpan<Vector2> quad, Vector2 pt)
        {
            var xMin = MathF.Min(MathF.Min(quad[0].X, quad[1].X), quad[2].X);
            if (pt.X + _invResScale < xMin)
                return false;
            var xMax = MathF.Max(MathF.Max(quad[0].X, quad[1].X), quad[2].X);
            if (pt.X - _invResScale > xMax)
                return false;
            var yMin = MathF.Min(MathF.Min(quad[0].Y, quad[1].Y), quad[2].Y);
            if (pt.Y + _invResScale < yMin)
                return false;
            var yMax = MathF.Max(MathF.Max(quad[0].Y, quad[1].Y), quad[2].Y);
            if (pt.Y - _invResScale > yMax)
                return false;
            return true;
        }

        static bool PointsWithinDist(Vector2 nearPt, Vector2 farPt, float limit) => DistanceToSqd(nearPt, farPt) <= limit * limit;

        static bool SharpAngle(in QuadConstruct quad)
        {
            var smaller = quad.Q1 - quad.Q0;
            var larger = quad.Q1 - quad.Q2;
            var smallerLen = LengthSqd(smaller);
            var largerLen = LengthSqd(larger);
            if (smallerLen > largerLen)
            {
                (smaller, larger) = (larger, smaller);
                largerLen = smallerLen;
            }
            if (!SetLength(ref smaller, largerLen))
                return false;
            return Dot(smaller, larger) > 0;
        }

        ResultType StrokeCloseEnough(in QuadConstruct quadPts, Vector2 ray0, Vector2 ray1)
        {
            ReadOnlySpan<Vector2> stroke = [quadPts.Q0, quadPts.Q1, quadPts.Q2];
            var strokeMid = EvalQuadAt(stroke, 0.5f);
            if (PointsWithinDist(ray0, strokeMid, _invResScale))
                return SharpAngle(quadPts) ? ResultType.Split : ResultType.Quad;
            if (!PtInQuadBounds(stroke, ray0))
                return ResultType.Split;
            Span<float> roots = stackalloc float[2];
            var rootCount = IntersectQuadRay(ray0, ray1, stroke, roots);
            if (rootCount != 1)
                return ResultType.Split;
            var quadPt = EvalQuadAt(stroke, roots[0]);
            var error = _invResScale * (1 - MathF.Abs(roots[0] - 0.5f) * 2);
            if (PointsWithinDist(ray0, quadPt, error))
                return SharpAngle(quadPts) ? ResultType.Split : ResultType.Quad;
            return ResultType.Split;
        }

        ResultType CompareQuadCubic(ReadOnlySpan<Vector2> cubic, ref QuadConstruct quadPts)
        {
            CubicQuadEnds(cubic, ref quadPts);
            var resultType = IntersectRay(ref quadPts, true);
            if (resultType != ResultType.Quad)
                return resultType;
            CubicPerpRay(cubic, quadPts.MidT, out var ray1, out var ray0, out _);
            return StrokeCloseEnough(quadPts, ray0, ray1);
        }

        ResultType CompareQuadConic(Conic conic, ref QuadConstruct quadPts)
        {
            ConicQuadEnds(conic, ref quadPts);
            var resultType = IntersectRay(ref quadPts, true);
            if (resultType != ResultType.Quad)
                return resultType;
            ConicPerpRay(conic, quadPts.MidT, out var ray1, out var ray0, out _);
            return StrokeCloseEnough(quadPts, ray0, ray1);
        }

        ResultType CompareQuadQuad(ReadOnlySpan<Vector2> quad, ref QuadConstruct quadPts)
        {
            if (!quadPts.StartSet)
            {
                QuadPerpRay(quad, quadPts.StartT, out _, out quadPts.Q0, out quadPts.TangentStart);
                quadPts.StartSet = true;
            }
            if (!quadPts.EndSet)
            {
                QuadPerpRay(quad, quadPts.EndT, out _, out quadPts.Q2, out quadPts.TangentEnd);
                quadPts.EndSet = true;
            }
            var resultType = IntersectRay(ref quadPts, true);
            if (resultType != ResultType.Quad)
                return resultType;
            QuadPerpRay(quad, quadPts.MidT, out var ray1, out var ray0, out _);
            return StrokeCloseEnough(quadPts, ray0, ray1);
        }

        SkiaPath Sink => _strokeType == 1 ? _outer : _inner;

        void AddDegenerateLine(in QuadConstruct quadPts) => Sink.LineTo(quadPts.Q2);

        bool CubicMidOnLine(ReadOnlySpan<Vector2> cubic, in QuadConstruct quadPts)
        {
            CubicPerpRay(cubic, quadPts.MidT, out _, out var strokeMid, out _);
            var dist = PtToLine(strokeMid, quadPts.Q0, quadPts.Q2);
            return dist < _invResScaleSquared;
        }

        bool CubicStroke(ReadOnlySpan<Vector2> cubic, ref QuadConstruct quadPts)
        {
            if (!_foundTangents)
            {
                var resultType = TangentsMeet(cubic, ref quadPts);
                if (resultType != ResultType.Quad)
                {
                    if ((resultType == ResultType.Degenerate || PointsWithinDist(quadPts.Q0, quadPts.Q2, _invResScale))
                        && CubicMidOnLine(cubic, quadPts))
                    {
                        AddDegenerateLine(quadPts);
                        return true;
                    }
                }
                else
                    _foundTangents = true;
            }
            if (_foundTangents)
            {
                var resultType = CompareQuadCubic(cubic, ref quadPts);
                if (resultType == ResultType.Quad)
                {
                    Sink.QuadTo(quadPts.Q1, quadPts.Q2);
                    return true;
                }
                if (resultType == ResultType.Degenerate && !quadPts.OppositeTangents)
                {
                    AddDegenerateLine(quadPts);
                    return true;
                }
            }
            if (!IsFinite(quadPts.Q2))
                return false;
            if (++_recursionDepth > RecursiveLimits[_foundTangents ? CubicLimit : TangentLimit])
            {
                AddDegenerateLine(quadPts);
                return true;
            }
            var half = new QuadConstruct();
            if (!half.InitWithStart(quadPts))
            {
                AddDegenerateLine(quadPts);
                --_recursionDepth;
                return true;
            }
            if (!CubicStroke(cubic, ref half))
                return false;
            if (!half.InitWithEnd(quadPts))
            {
                AddDegenerateLine(quadPts);
                --_recursionDepth;
                return true;
            }
            if (!CubicStroke(cubic, ref half))
                return false;
            --_recursionDepth;
            return true;
        }

        bool ConicStroke(Conic conic, ref QuadConstruct quadPts)
        {
            var resultType = CompareQuadConic(conic, ref quadPts);
            if (resultType == ResultType.Quad)
            {
                Sink.QuadTo(quadPts.Q1, quadPts.Q2);
                return true;
            }
            if (resultType == ResultType.Degenerate)
            {
                AddDegenerateLine(quadPts);
                return true;
            }
            if (++_recursionDepth > RecursiveLimits[ConicLimit])
            {
                AddDegenerateLine(quadPts);
                return true;
            }
            var half = new QuadConstruct();
            half.InitWithStart(quadPts);
            if (!ConicStroke(conic, ref half))
                return false;
            half.InitWithEnd(quadPts);
            if (!ConicStroke(conic, ref half))
                return false;
            --_recursionDepth;
            return true;
        }

        bool QuadStroke(ReadOnlySpan<Vector2> quad, ref QuadConstruct quadPts)
        {
            var resultType = CompareQuadQuad(quad, ref quadPts);
            if (resultType == ResultType.Quad)
            {
                Sink.QuadTo(quadPts.Q1, quadPts.Q2);
                return true;
            }
            if (resultType == ResultType.Degenerate)
            {
                AddDegenerateLine(quadPts);
                return true;
            }
            if (++_recursionDepth > RecursiveLimits[QuadLimit])
            {
                AddDegenerateLine(quadPts);
                return true;
            }
            var half = new QuadConstruct();
            half.InitWithStart(quadPts);
            if (!QuadStroke(quad, ref half))
                return false;
            half.InitWithEnd(quadPts);
            if (!QuadStroke(quad, ref half))
                return false;
            --_recursionDepth;
            return true;
        }

        public void CubicTo(Vector2 pt1, Vector2 pt2, Vector2 pt3)
        {
            ReadOnlySpan<Vector2> cubic = [_prevPt, pt1, pt2, pt3];
            Span<Vector2> reduction = stackalloc Vector2[3];
            var reductionType = CheckCubicLinear(cubic, reduction, out var tangentPt);
            if (reductionType is ReductionType.Point or ReductionType.Line)
            {
                LineTo(pt3);
                return;
            }
            if (reductionType >= ReductionType.Degenerate)
            {
                LinesWithRoundJoin(reduction[..(reductionType - ReductionType.Degenerate + 1)], pt3);
                return;
            }
            if (!PreJoinTo(cubic[tangentPt], out var normalAB, out var unitAB, false))
            {
                LineTo(pt3);
                return;
            }
            Span<float> tValues = stackalloc float[2];
            var count = FindCubicInflections(cubic, tValues);
            var lastT = 0f;
            for (var index = 0; index <= count; ++index)
            {
                var nextT = index < count ? tValues[index] : 1;
                var quadPts = new QuadConstruct();
                Init(1, ref quadPts, lastT, nextT);
                CubicStroke(cubic, ref quadPts);
                Init(-1, ref quadPts, lastT, nextT);
                CubicStroke(cubic, ref quadPts);
                lastT = nextT;
            }
            var cusp = FindCubicCusp(cubic);
            if (cusp > 0)
            {
                var cuspLoc = EvalCubicAt(cubic, cusp);
                _cusper.AddCircle(cuspLoc, _radius);
            }
            SetCubicEndNormal(cubic, normalAB, unitAB, out var normalCD, out var unitCD);
            PostJoinTo(pt3, normalCD, unitCD);
        }
    }
}

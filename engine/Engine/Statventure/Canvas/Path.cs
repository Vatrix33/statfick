using System.Globalization;
using System.Numerics;
using System.Text;
using Statventure.Core;

namespace Statventure.Canvas;

public enum FillRule
{
    NonZero,
    EvenOdd,
}

public enum LineJoin
{
    Miter,
    Round,
    Bevel,
}

public enum LineCap
{
    Butt,
    Round,
    Square,
}

public sealed record StrokeStyle(float Width, LineJoin Join = LineJoin.Miter, LineCap Cap = LineCap.Butt, float MiterLimit = 4f);

/// <summary>
/// A vector path (lines, quadratic and cubic curves) in local units, built in code or parsed from SVG
/// path data. Paths are immutable once built and remember a content hash, so the canvas can cache
/// their rasterized coverage.
/// </summary>
public sealed class Path
{
    internal enum Verb : byte
    {
        Move,
        Line,
        Quad,
        Cubic,
        Close,
    }

    readonly List<Verb> _verbs = [];
    readonly List<Vector2> _points = [];
    Vector2 _start, _current;
    int? _hash;

    internal IReadOnlyList<Verb> Verbs => _verbs;
    internal IReadOnlyList<Vector2> Points => _points;

    /// <summary>The bounds of the path's points (curves stay inside their control points).</summary>
    public Core.RectF Bounds
    {
        get
        {
            if (_points.Count == 0)
                return Core.RectF.Empty;
            Vector2 min = _points[0], max = _points[0];
            foreach (var p in _points)
            {
                min = Vector2.Min(min, p);
                max = Vector2.Max(max, p);
            }
            return Core.RectF.FromEdges(min.X, min.Y, max.X, max.Y);
        }
    }
    public bool IsEmpty => _verbs.Count == 0;

    /// <summary>
    /// The tight bounds of the path (SkPath::computeTightBounds): its on-curve points (move points
    /// included) and the extremes of its curves.
    /// </summary>
    public Core.RectF TightBounds
    {
        get
        {
            if (_points.Count == 0)
                return Core.RectF.Empty;
            Vector2 min = _points[0], max = _points[0];
            void Add(Vector2 p)
            {
                min = Vector2.Min(min, p);
                max = Vector2.Max(max, p);
            }
            var at = 0;
            var current = _points[0];
            foreach (var verb in _verbs)
            {
                switch (verb)
                {
                    case Verb.Move:
                    case Verb.Line:
                        current = _points[at++];
                        Add(current);
                        break;
                    case Verb.Quad:
                    {
                        Vector2 c = _points[at], p = _points[at + 1];
                        at += 2;
                        // Where the derivative of each axis is zero: t = (p0 − c) / (p0 − 2c + p2).
                        for (var axis = 0; axis < 2; axis++)
                        {
                            float a = current[axis], b = c[axis], d = p[axis];
                            var denominator = a - 2 * b + d;
                            if (denominator != 0 && (a - b) / denominator is var t and > 0 and < 1)
                                Add(Quad(current, c, p, t));
                        }
                        Add(p);
                        current = p;
                        break;
                    }
                    case Verb.Cubic:
                    {
                        Vector2 c1 = _points[at], c2 = _points[at + 1], p = _points[at + 2];
                        at += 3;
                        // The derivative's roots: a t² + b t + c with a = 3(−p0 + 3c1 − 3c2 + p3), b = 6(p0 − 2c1 + c2), c = 3(c1 − p0).
                        for (var axis = 0; axis < 2; axis++)
                        {
                            float p0 = current[axis], q1 = c1[axis], q2 = c2[axis], p3 = p[axis];
                            var a = 3 * (-p0 + 3 * q1 - 3 * q2 + p3);
                            var b = 6 * (p0 - 2 * q1 + q2);
                            var c = 3 * (q1 - p0);
                            if (MathF.Abs(a) < 1e-12f)
                            {
                                if (b != 0 && -c / b is var t and > 0 and < 1)
                                    Add(Cubic(current, c1, c2, p, t));
                                continue;
                            }
                            var discriminant = b * b - 4 * a * c;
                            if (discriminant < 0)
                                continue;
                            var root = MathF.Sqrt(discriminant);
                            foreach (var t in new[] { (-b + root) / (2 * a), (-b - root) / (2 * a) })
                                if (t is > 0 and < 1)
                                    Add(Cubic(current, c1, c2, p, t));
                        }
                        Add(p);
                        current = p;
                        break;
                    }
                }
            }
            return Core.RectF.FromEdges(min.X, min.Y, max.X, max.Y);
        }
    }

    static Vector2 Quad(Vector2 p0, Vector2 c, Vector2 p1, float t) => (1 - t) * (1 - t) * p0 + 2 * t * (1 - t) * c + t * t * p1;

    static Vector2 Cubic(Vector2 p0, Vector2 c1, Vector2 c2, Vector2 p1, float t)
    {
        var u = 1 - t;
        return u * u * u * p0 + 3 * u * u * t * c1 + 3 * u * t * t * c2 + t * t * t * p1;
    }

    /// <summary>Whether the path is one line segment (Skia's SkPath::isLine).</summary>
    public bool IsLine => _verbs.Count == 2 && _verbs[0] == Verb.Move && _verbs[1] == Verb.Line;

    public Path MoveTo(Vector2 p)
    {
        _verbs.Add(Verb.Move);
        _points.Add(p);
        _start = _current = p;
        _hash = null;
        return this;
    }

    public Path LineTo(Vector2 p)
    {
        EnsureStarted();
        _verbs.Add(Verb.Line);
        _points.Add(p);
        _current = p;
        _hash = null;
        return this;
    }

    public Path QuadTo(Vector2 control, Vector2 p)
    {
        EnsureStarted();
        _verbs.Add(Verb.Quad);
        _points.Add(control);
        _points.Add(p);
        _current = p;
        _hash = null;
        return this;
    }

    public Path CubicTo(Vector2 c1, Vector2 c2, Vector2 p)
    {
        EnsureStarted();
        _verbs.Add(Verb.Cubic);
        _points.Add(c1);
        _points.Add(c2);
        _points.Add(p);
        _current = p;
        _hash = null;
        return this;
    }

    public Path Close()
    {
        if (_verbs.Count > 0 && _verbs[^1] != Verb.Close)
        {
            _verbs.Add(Verb.Close);
            _current = _start;
            _hash = null;
        }
        return this;
    }

    void EnsureStarted()
    {
        if (_verbs.Count == 0 || _verbs[^1] == Verb.Close)
            MoveTo(_current);
    }

    /// <summary>An SVG elliptical arc from the current point (the "A" command).</summary>
    public Path ArcTo(Vector2 radii, float xAxisRotationDegrees, bool largeArc, bool sweep, Vector2 end)
    {
        var start = _current;
        if (start == end)
            return this;
        var rx = MathF.Abs(radii.X);
        var ry = MathF.Abs(radii.Y);
        if (rx < 1e-6f || ry < 1e-6f)
            return LineTo(end);
        // Endpoint to centre parameterization (SVG implementation notes, F.6.5).
        var phi = xAxisRotationDegrees * MathF.PI / 180f;
        var (sin, cos) = MathF.SinCos(phi);
        var d = (start - end) / 2f;
        var x1 = cos * d.X + sin * d.Y;
        var y1 = -sin * d.X + cos * d.Y;
        var lambda = x1 * x1 / (rx * rx) + y1 * y1 / (ry * ry);
        if (lambda > 1)
        {
            var s = MathF.Sqrt(lambda);
            rx *= s;
            ry *= s;
        }
        var numerator = rx * rx * ry * ry - rx * rx * y1 * y1 - ry * ry * x1 * x1;
        var denominator = rx * rx * y1 * y1 + ry * ry * x1 * x1;
        var factor = MathF.Sqrt(MathF.Max(0, numerator / denominator)) * (largeArc == sweep ? -1 : 1);
        var cx1 = factor * rx * y1 / ry;
        var cy1 = factor * -ry * x1 / rx;
        var center = new Vector2(cos * cx1 - sin * cy1, sin * cx1 + cos * cy1) + (start + end) / 2f;
        static float Angle(Vector2 u, Vector2 v)
        {
            var a = MathF.Atan2(u.X * v.Y - u.Y * v.X, Vector2.Dot(u, v));
            return a;
        }
        var u0 = new Vector2((x1 - cx1) / rx, (y1 - cy1) / ry);
        var u1 = new Vector2((-x1 - cx1) / rx, (-y1 - cy1) / ry);
        var theta = Angle(Vector2.UnitX, u0);
        var delta = Angle(u0, u1);
        if (!sweep && delta > 0)
            delta -= MathF.Tau;
        else if (sweep && delta < 0)
            delta += MathF.Tau;
        // Split into pieces of at most 90 degrees, each a cubic.
        var segments = (int)MathF.Ceiling(MathF.Abs(delta) / (MathF.PI / 2) - 1e-4f);
        segments = Math.Max(segments, 1);
        var step = delta / segments;
        var k = 4f / 3f * MathF.Tan(step / 4f);
        Vector2 Point(float angle) => center + new Vector2(cos * rx * MathF.Cos(angle) - sin * ry * MathF.Sin(angle), sin * rx * MathF.Cos(angle) + cos * ry * MathF.Sin(angle));
        Vector2 Derivative(float angle) => new(-cos * rx * MathF.Sin(angle) - sin * ry * MathF.Cos(angle), -sin * rx * MathF.Sin(angle) + cos * ry * MathF.Cos(angle));
        var a0 = theta;
        for (var i = 0; i < segments; i++)
        {
            var a1 = a0 + step;
            var p0 = Point(a0);
            var p1 = i == segments - 1 ? end : Point(a1);
            CubicTo(p0 + Derivative(a0) * k, p1 - Derivative(a1) * k, p1);
            a0 = a1;
        }
        return this;
    }

    // ---------- Shapes ----------

    public static Path Rectangle(RectF rect) =>
        new Path().MoveTo(rect.Position).LineTo(new Vector2(rect.Right, rect.Top)).LineTo(new Vector2(rect.Right, rect.Bottom)).LineTo(new Vector2(rect.Left, rect.Bottom)).Close();

    public static Path Ellipse(Vector2 center, Vector2 radii)
    {
        var path = new Path().MoveTo(center + new Vector2(radii.X, 0));
        path.ArcTo(radii, 0, false, true, center - new Vector2(radii.X, 0));
        path.ArcTo(radii, 0, false, true, center + new Vector2(radii.X, 0));
        return path.Close();
    }

    public static Path Circle(Vector2 center, float radius) => Ellipse(center, new Vector2(radius));

    // ---------- SVG path data ----------

    /// <summary>Parses SVG path data ("M10 10 L20 20 Z", all commands, relative and absolute).</summary>
    public static Path ParseSvg(string data)
    {
        var path = new Path();
        var reader = new SvgReader(data);
        var command = ' ';
        Vector2 current = Vector2.Zero, start = Vector2.Zero, lastControl = Vector2.Zero;
        var lastCommand = ' ';
        while (true)
        {
            reader.SkipSeparators();
            if (reader.AtEnd)
                break;
            if (char.IsLetter(reader.Peek))
                command = reader.Next();
            else if (command == ' ')
                throw new FormatException("SVG path data must start with a command: " + data);
            var relative = char.IsLower(command);
            var offset = relative ? current : Vector2.Zero;
            switch (char.ToUpperInvariant(command))
            {
                case 'M':
                    current = reader.Point() + offset;
                    start = current;
                    path.MoveTo(current);
                    // Coordinates after the first pair of a move are line-tos.
                    command = relative ? 'l' : 'L';
                    break;
                case 'L':
                    current = reader.Point() + offset;
                    path.LineTo(current);
                    break;
                case 'H':
                    current = new Vector2(reader.Number() + (relative ? current.X : 0), current.Y);
                    path.LineTo(current);
                    break;
                case 'V':
                    current = new Vector2(current.X, reader.Number() + (relative ? current.Y : 0));
                    path.LineTo(current);
                    break;
                case 'C':
                {
                    var c1 = reader.Point() + offset;
                    var c2 = reader.Point() + offset;
                    var end = reader.Point() + offset;
                    path.CubicTo(c1, c2, end);
                    lastControl = c2;
                    current = end;
                    break;
                }
                case 'S':
                {
                    var c1 = "CcSs".Contains(lastCommand) ? 2 * current - lastControl : current;
                    var c2 = reader.Point() + offset;
                    var end = reader.Point() + offset;
                    path.CubicTo(c1, c2, end);
                    lastControl = c2;
                    current = end;
                    break;
                }
                case 'Q':
                {
                    var c = reader.Point() + offset;
                    var end = reader.Point() + offset;
                    path.QuadTo(c, end);
                    lastControl = c;
                    current = end;
                    break;
                }
                case 'T':
                {
                    var c = "QqTt".Contains(lastCommand) ? 2 * current - lastControl : current;
                    var end = reader.Point() + offset;
                    path.QuadTo(c, end);
                    lastControl = c;
                    current = end;
                    break;
                }
                case 'A':
                {
                    var radii = new Vector2(reader.Number(), reader.Number());
                    var rotation = reader.Number();
                    var large = reader.Flag();
                    var sweep = reader.Flag();
                    var end = reader.Point() + offset;
                    path.ArcTo(radii, rotation, large, sweep, end);
                    current = end;
                    break;
                }
                case 'Z':
                    path.Close();
                    current = start;
                    break;
                default:
                    throw new FormatException($"unknown SVG path command '{command}'");
            }
            lastCommand = command;
        }
        return path;
    }

    ref struct SvgReader(string text)
    {
        int _at = 0;
        readonly string _text = text;

        public readonly bool AtEnd => _at >= _text.Length;
        public readonly char Peek => _text[_at];
        public char Next() => _text[_at++];

        public void SkipSeparators()
        {
            while (_at < _text.Length && (char.IsWhiteSpace(_text[_at]) || _text[_at] == ','))
                _at++;
        }

        public float Number()
        {
            SkipSeparators();
            var start = _at;
            if (_at < _text.Length && (_text[_at] == '-' || _text[_at] == '+'))
                _at++;
            var dot = false;
            while (_at < _text.Length)
            {
                var c = _text[_at];
                if (char.IsDigit(c))
                    _at++;
                else if (c == '.' && !dot)
                {
                    dot = true;
                    _at++;
                }
                else if ((c == 'e' || c == 'E') && _at + 1 < _text.Length)
                {
                    _at++;
                    if (_text[_at] == '-' || _text[_at] == '+')
                        _at++;
                }
                else
                    break;
            }
            if (start == _at)
                throw new FormatException($"expected a number at {start} in SVG path data");
            return float.Parse(_text.AsSpan(start, _at - start), NumberStyles.Float, CultureInfo.InvariantCulture);
        }

        public bool Flag()
        {
            SkipSeparators();
            var c = _text[_at++];
            return c == '1';
        }

        public Vector2 Point() => new(Number(), Number());
    }

    // ---------- Geometry ----------

    /// <summary>
    /// The path as polylines in the space of <paramref name="transform"/>, curves split so they stay
    /// within <paramref name="tolerance"/> of the true curve. Each contour knows whether it was closed.
    /// </summary>
    public List<(List<Vector2> Points, bool Closed)> Flatten(Matrix3x2 transform, float tolerance = 0.1f)
    {
        var contours = new List<(List<Vector2>, bool)>();
        List<Vector2>? contour = null;
        var p = 0;
        Vector2 Point(int i) => Vector2.Transform(_points[i], transform);
        foreach (var verb in _verbs)
        {
            switch (verb)
            {
                case Verb.Move:
                    if (contour is { Count: > 1 })
                        contours.Add((contour, false));
                    contour = [Point(p++)];
                    break;
                case Verb.Line:
                    contour!.Add(Point(p++));
                    break;
                case Verb.Quad:
                {
                    var p0 = contour![^1];
                    var c = Point(p++);
                    var p1 = Point(p++);
                    var n = (int)Math.Clamp(MathF.Ceiling(MathF.Sqrt((p0 - 2 * c + p1).Length() / (4f * tolerance))), 1, 256);
                    for (var i = 1; i <= n; i++)
                    {
                        var t = i / (float)n;
                        var u = 1 - t;
                        contour.Add(u * u * p0 + 2 * u * t * c + t * t * p1);
                    }
                    break;
                }
                case Verb.Cubic:
                {
                    var p0 = contour![^1];
                    var c1 = Point(p++);
                    var c2 = Point(p++);
                    var p1 = Point(p++);
                    var m = MathF.Max((p0 - 2 * c1 + c2).Length(), (c1 - 2 * c2 + p1).Length());
                    var n = (int)Math.Clamp(MathF.Ceiling(MathF.Sqrt(0.75f * m / tolerance)), 1, 256);
                    for (var i = 1; i <= n; i++)
                    {
                        var t = i / (float)n;
                        var u = 1 - t;
                        contour.Add(u * u * u * p0 + 3 * u * u * t * c1 + 3 * u * t * t * c2 + t * t * t * p1);
                    }
                    break;
                }
                case Verb.Close:
                    if (contour is { Count: > 1 })
                        contours.Add((contour, true));
                    contour = contour is { Count: > 0 } ? [contour[0]] : null;
                    break;
            }
        }
        if (contour is { Count: > 1 })
            contours.Add((contour, false));
        return contours;
    }

    public override int GetHashCode()
    {
        if (_hash is { } hash)
            return hash;
        var h = new HashCode();
        foreach (var verb in _verbs)
            h.Add(verb);
        foreach (var point in _points)
            h.Add(point);
        _hash = h.ToHashCode();
        return _hash.Value;
    }

    public override bool Equals(object? obj) => obj is Path other && (ReferenceEquals(this, other) || (GetHashCode() == other.GetHashCode() && _verbs.SequenceEqual(other._verbs) && _points.SequenceEqual(other._points)));

    public override string ToString()
    {
        var text = new StringBuilder();
        var p = 0;
        foreach (var verb in _verbs)
        {
            switch (verb)
            {
                case Verb.Move: text.Append($"M{_points[p].X} {_points[p++].Y} "); break;
                case Verb.Line: text.Append($"L{_points[p].X} {_points[p++].Y} "); break;
                case Verb.Quad: text.Append($"Q{_points[p].X} {_points[p++].Y} {_points[p].X} {_points[p++].Y} "); break;
                case Verb.Cubic: text.Append($"C{_points[p].X} {_points[p++].Y} {_points[p].X} {_points[p++].Y} {_points[p].X} {_points[p++].Y} "); break;
                case Verb.Close: text.Append("Z "); break;
            }
        }
        return text.ToString().TrimEnd();
    }
}

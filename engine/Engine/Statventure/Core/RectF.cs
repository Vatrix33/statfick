using System.Numerics;

namespace Statventure.Core;

/// <summary>An axis-aligned rectangle: position (top left) and size.</summary>
public readonly record struct RectF(float X, float Y, float Width, float Height)
{
    public static readonly RectF Empty = new(0, 0, 0, 0);

    public static RectF FromEdges(float left, float top, float right, float bottom) => new(left, top, right - left, bottom - top);

    public float Left => X;
    public float Top => Y;
    public float Right => X + Width;
    public float Bottom => Y + Height;
    public Vector2 Position => new(X, Y);
    public Vector2 Size => new(Width, Height);
    public Vector2 Center => new(X + Width / 2, Y + Height / 2);
    public bool IsEmpty => Width <= 0 || Height <= 0;

    public bool Contains(Vector2 point) => point.X >= X && point.Y >= Y && point.X < Right && point.Y < Bottom;

    /// <summary>Whether <paramref name="other"/> lies wholly inside this rectangle (an empty one always does).</summary>
    public bool ContainsRect(RectF other) => other.IsEmpty || (other.Left >= Left && other.Top >= Top && other.Right <= Right && other.Bottom <= Bottom);

    public RectF Intersect(RectF other)
    {
        var left = MathF.Max(Left, other.Left);
        var top = MathF.Max(Top, other.Top);
        var right = MathF.Min(Right, other.Right);
        var bottom = MathF.Min(Bottom, other.Bottom);
        return right <= left || bottom <= top ? new RectF(left, top, 0, 0) : FromEdges(left, top, right, bottom);
    }

    public RectF Union(RectF other) =>
        IsEmpty ? other : other.IsEmpty ? this : FromEdges(MathF.Min(Left, other.Left), MathF.Min(Top, other.Top), MathF.Max(Right, other.Right), MathF.Max(Bottom, other.Bottom));

    public RectF Inflate(float amount) => new(X - amount, Y - amount, Width + amount * 2, Height + amount * 2);

    public RectF Inflate(float left, float top, float right, float bottom) => FromEdges(Left - left, Top - top, Right + right, Bottom + bottom);

    public RectF Offset(Vector2 by) => new(X + by.X, Y + by.Y, Width, Height);
    public RectF Offset(float dx, float dy) => new(X + dx, Y + dy, Width, Height);

    /// <summary>The bounding box of this rectangle under a transform.</summary>
    public RectF Transform(Matrix3x2 matrix)
    {
        var a = Vector2.Transform(new Vector2(Left, Top), matrix);
        var b = Vector2.Transform(new Vector2(Right, Top), matrix);
        var c = Vector2.Transform(new Vector2(Right, Bottom), matrix);
        var d = Vector2.Transform(new Vector2(Left, Bottom), matrix);
        var min = Vector2.Min(Vector2.Min(a, b), Vector2.Min(c, d));
        var max = Vector2.Max(Vector2.Max(a, b), Vector2.Max(c, d));
        return FromEdges(min.X, min.Y, max.X, max.Y);
    }
}

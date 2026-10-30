using System.Numerics;
using Statventure.Core;
using Statventure.Graphics;

namespace Statventure.Canvas;

public readonly record struct GradientStop(float Offset, Color Color);

public enum GradientKind
{
    Linear,
    Radial,
}

/// <summary>
/// How an area is painted: one colour or a gradient. Gradient geometry is in the shape's own box
/// (0,0 at its top left, in logical units): a linear gradient runs from <see cref="Start"/> to
/// <see cref="End"/>; a radial gradient is centred on <see cref="Start"/> with radii <see cref="Radii"/>.
/// Stops are sorted, offsets 0..1 along the gradient; colours mix in premultiplied space as in CSS.
/// </summary>
public sealed record Brush
{
    public Color Color { get; init; } = Color.White;
    public GradientKind Kind { get; init; }
    public GradientStop[]? Stops { get; init; }
    public Vector2 Start { get; init; }
    public Vector2 End { get; init; }
    public Vector2 Radii { get; init; }
    /// <summary>Repeat the stops beyond 0..1 (repeating-linear-gradient).</summary>
    public bool Repeat { get; init; }

    public bool IsGradient => Stops is { Length: > 0 };

    public static Brush Solid(Color color) => new() { Color = color };

    public static Brush Linear(Vector2 start, Vector2 end, params GradientStop[] stops) =>
        new() { Kind = GradientKind.Linear, Start = start, End = end, Stops = stops };

    public static Brush Radial(Vector2 center, Vector2 radii, params GradientStop[] stops) =>
        new() { Kind = GradientKind.Radial, Start = center, Radii = radii, Stops = stops };

    /// <summary>
    /// A CSS linear-gradient for a box of this size: <paramref name="angleDegrees"/> 0 points up,
    /// 90 right; the gradient line passes through the centre and is long enough that the corners
    /// get the first and last colours.
    /// </summary>
    public static Brush CssLinear(Vector2 boxSize, float angleDegrees, params GradientStop[] stops)
    {
        var angle = angleDegrees * MathF.PI / 180f;
        var direction = new Vector2(MathF.Sin(angle), -MathF.Cos(angle));
        var length = MathF.Abs(boxSize.X * MathF.Sin(angle)) + MathF.Abs(boxSize.Y * MathF.Cos(angle));
        var center = boxSize / 2f;
        return Linear(center - direction * length / 2f, center + direction * length / 2f, stops);
    }

    /// <summary>The CSS angle of "to top right" and the other corner keywords for a box.</summary>
    public static float CssCornerAngle(Vector2 boxSize, bool toRight, bool toBottom)
    {
        // The gradient line is perpendicular to the diagonal joining the corner's two neighbours.
        var angle = MathF.Atan2(boxSize.Y, boxSize.X) * 180f / MathF.PI;
        return (toRight, toBottom) switch
        {
            (true, false) => angle,
            (true, true) => 180f - angle,
            (false, true) => 180f + angle,
            _ => 360f - angle,
        };
    }
}

/// <summary>Corner radii (each corner can be elliptical: X horizontal, Y vertical).</summary>
public readonly record struct CornerRadii(Vector2 TopLeft, Vector2 TopRight, Vector2 BottomRight, Vector2 BottomLeft)
{
    public static readonly CornerRadii Zero = default;

    public CornerRadii(float all) : this(new Vector2(all), new Vector2(all), new Vector2(all), new Vector2(all)) { }

    public CornerRadii(float topLeft, float topRight, float bottomRight, float bottomLeft)
        : this(new Vector2(topLeft), new Vector2(topRight), new Vector2(bottomRight), new Vector2(bottomLeft)) { }

    public bool IsZero => TopLeft == Vector2.Zero && TopRight == Vector2.Zero && BottomRight == Vector2.Zero && BottomLeft == Vector2.Zero;

    /// <summary>Scales radii down when they don't fit the box (as CSS does), keeping their proportions.</summary>
    public CornerRadii Fit(Vector2 size)
    {
        float Ratio(float length, float a, float b) => a + b > length && a + b > 0 ? length / (a + b) : 1f;
        var f = MathF.Min(
            MathF.Min(Ratio(size.X, TopLeft.X, TopRight.X), Ratio(size.X, BottomLeft.X, BottomRight.X)),
            MathF.Min(Ratio(size.Y, TopLeft.Y, BottomLeft.Y), Ratio(size.Y, TopRight.Y, BottomRight.Y)));
        return f >= 1f ? this : new CornerRadii(TopLeft * f, TopRight * f, BottomRight * f, BottomLeft * f);
    }

    /// <summary>The radii of the inner edge of a border (or of a box shrunk by an inset).</summary>
    public CornerRadii Shrink(float top, float right, float bottom, float left) => new(
        Vector2.Max(Vector2.Zero, TopLeft - new Vector2(left, top)),
        Vector2.Max(Vector2.Zero, TopRight - new Vector2(right, top)),
        Vector2.Max(Vector2.Zero, BottomRight - new Vector2(right, bottom)),
        Vector2.Max(Vector2.Zero, BottomLeft - new Vector2(left, bottom)));

    public CornerRadii Scale(float factor) => new(TopLeft * factor, TopRight * factor, BottomRight * factor, BottomLeft * factor);
}

public enum BorderStyle
{
    None,
    Solid,
    Dashed,
}

/// <summary>A border inside a box's edge: widths per side (top, right, bottom, left), one colour and style.</summary>
public readonly record struct Border(Vector4 Widths, Color Color, BorderStyle Style = BorderStyle.Solid)
{
    public static readonly Border None = default;

    public Border(float width, Color color, BorderStyle style = BorderStyle.Solid) : this(new Vector4(width), color, style) { }

    public float Top => Widths.X;
    public float Right => Widths.Y;
    public float Bottom => Widths.Z;
    public float Left => Widths.W;
    public bool IsVisible => Style != BorderStyle.None && Color.A > 0 && Widths != Vector4.Zero;
}

/// <summary>One side of a border.</summary>
public readonly record struct BorderSide(float Width, Color Color, BorderStyle Style = BorderStyle.Solid)
{
    public bool IsVisible => Width > 0 && Style != BorderStyle.None && Color.A > 0;
}

/// <summary>A border whose sides can differ in width, colour and style (they meet on the corners' diagonals, as in browsers).</summary>
public readonly record struct BorderSides(BorderSide Top, BorderSide Right, BorderSide Bottom, BorderSide Left)
{
    public bool IsVisible => Top.IsVisible || Right.IsVisible || Bottom.IsVisible || Left.IsVisible;

    /// <summary>The sides as one colour and style, when every visible side agrees.</summary>
    public Border? Uniform
    {
        get
        {
            BorderSide? first = null;
            foreach (var side in new[] { Top, Right, Bottom, Left })
            {
                if (!side.IsVisible)
                    continue;
                if (first is { } f && (f.Color != side.Color || f.Style != side.Style))
                    return null;
                first ??= side;
            }
            if (first is not { } uniform)
                return Border.None;
            float W(BorderSide s) => s.IsVisible ? s.Width : 0;
            return new Border(new Vector4(W(Top), W(Right), W(Bottom), W(Left)), uniform.Color, uniform.Style);
        }
    }
}

/// <summary>A CSS box-shadow: blur is the CSS blur radius (σ = blur / 2).</summary>
public readonly record struct BoxShadow(Vector2 Offset, float Blur, float Spread, Color Color, bool Inset = false);

public enum FilterKind
{
    Blur,
    DropShadow,
    Brightness,
    Contrast,
    Saturate,
    Grayscale,
    Sepia,
    HueRotate,
    Invert,
    Opacity,
}

/// <summary>One CSS filter function. Amount is the CSS argument (1 = 100%; degrees for hue-rotate; pixels for blur).</summary>
public readonly record struct Filter(FilterKind Kind, float Amount, Vector2 Offset = default, Color Color = default)
{
    public static Filter Blur(float radius) => new(FilterKind.Blur, radius);
    public static Filter DropShadow(Vector2 offset, float blur, Color color) => new(FilterKind.DropShadow, blur, offset, color);
    public static Filter Brightness(float amount) => new(FilterKind.Brightness, amount);
    public static Filter Contrast(float amount) => new(FilterKind.Contrast, amount);
    public static Filter Saturate(float amount) => new(FilterKind.Saturate, amount);
    public static Filter Grayscale(float amount) => new(FilterKind.Grayscale, amount);
    public static Filter Sepia(float amount) => new(FilterKind.Sepia, amount);
    public static Filter HueRotate(float degrees) => new(FilterKind.HueRotate, degrees);
    public static Filter Invert(float amount) => new(FilterKind.Invert, amount);
    public static Filter Opacity(float amount) => new(FilterKind.Opacity, amount);

    public bool IsColorMatrix => Kind is not (FilterKind.Blur or FilterKind.DropShadow);
}

/// <summary>A group drawn into its own image, then filtered and composited (CSS opacity, filter, mix-blend-mode).</summary>
public sealed record LayerOptions
{
    public float Opacity { get; init; } = 1f;
    public Filter[] Filters { get; init; } = [];
    public BlendMode Blend { get; init; } = BlendMode.Premultiplied;
    /// <summary>The area the layer's content can cover (logical units, canvas space); null for the whole canvas.</summary>
    public RectF? Bounds { get; init; }
    /// <summary>
    /// The layer starts as a copy of what is under it, so text in it keeps LCD antialiasing (cc's
    /// save layers for opacity with SkCanvas's kInitWithPrevious and kPreserveLCDText); other
    /// layers start transparent and draw their text in grayscale.
    /// </summary>
    public bool StartsFromBackdrop { get; init; }
}

/// <summary>
/// A 4×5 colour matrix in the CSS filter sense, applied to straight (unpremultiplied) colour:
/// out = M · (r, g, b, a, 1).
/// </summary>
public readonly struct ColorMatrix
{
    public readonly Matrix4x4 M;
    public readonly Vector4 Offset;

    public ColorMatrix(Matrix4x4 m, Vector4 offset)
    {
        M = m;
        Offset = offset;
    }

    public static readonly ColorMatrix Identity = new(Matrix4x4.Identity, Vector4.Zero);

    /// <summary>a then b (b applied to a's result).</summary>
    public static ColorMatrix Then(ColorMatrix a, ColorMatrix b) =>
        new(a.M * b.M, Vector4.Transform(a.Offset, b.M) + b.Offset);

    public Vector4 Apply(Vector4 color) => Vector4.Transform(color, M) + Offset;

    // Matrices from the Filter Effects specification. System.Numerics uses row vectors (v · M), so
    // these are the transposes of the specification's matrices.
    static Matrix4x4 Rows(float r0, float r1, float r2, float g0, float g1, float g2, float b0, float b1, float b2) => new(
        r0, g0, b0, 0,
        r1, g1, b1, 0,
        r2, g2, b2, 0,
        0, 0, 0, 1);

    public static ColorMatrix For(Filter filter)
    {
        var a = filter.Amount;
        switch (filter.Kind)
        {
            case FilterKind.Brightness:
                return new(Matrix4x4.CreateScale(a, a, a), Vector4.Zero);
            case FilterKind.Contrast:
                var shift = -0.5f * a + 0.5f;
                return new(Matrix4x4.CreateScale(a, a, a), new Vector4(shift, shift, shift, 0));
            case FilterKind.Opacity:
                return new(new Matrix4x4(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, a), Vector4.Zero);
            case FilterKind.Invert:
                return new(Matrix4x4.CreateScale(1 - 2 * a, 1 - 2 * a, 1 - 2 * a), new Vector4(a, a, a, 0));
            case FilterKind.Saturate:
                return new(Rows(
                    0.213f + 0.787f * a, 0.715f - 0.715f * a, 0.072f - 0.072f * a,
                    0.213f - 0.213f * a, 0.715f + 0.285f * a, 0.072f - 0.072f * a,
                    0.213f - 0.213f * a, 0.715f - 0.715f * a, 0.072f + 0.928f * a), Vector4.Zero);
            case FilterKind.Grayscale:
                var g = 1 - Math.Clamp(a, 0, 1);
                return new(Rows(
                    0.2126f + 0.7874f * g, 0.7152f - 0.7152f * g, 0.0722f - 0.0722f * g,
                    0.2126f - 0.2126f * g, 0.7152f + 0.2848f * g, 0.0722f - 0.0722f * g,
                    0.2126f - 0.2126f * g, 0.7152f - 0.7152f * g, 0.0722f + 0.9278f * g), Vector4.Zero);
            case FilterKind.Sepia:
                var s = 1 - Math.Clamp(a, 0, 1);
                return new(Rows(
                    0.393f + 0.607f * s, 0.769f - 0.769f * s, 0.189f - 0.189f * s,
                    0.349f - 0.349f * s, 0.686f + 0.314f * s, 0.168f - 0.168f * s,
                    0.272f - 0.272f * s, 0.534f - 0.534f * s, 0.131f + 0.869f * s), Vector4.Zero);
            case FilterKind.HueRotate:
                var r = a * MathF.PI / 180f;
                float c = MathF.Cos(r), n = MathF.Sin(r);
                return new(Rows(
                    0.213f + c * 0.787f - n * 0.213f, 0.715f - c * 0.715f - n * 0.715f, 0.072f - c * 0.072f + n * 0.928f,
                    0.213f - c * 0.213f + n * 0.143f, 0.715f + c * 0.285f + n * 0.140f, 0.072f - c * 0.072f - n * 0.283f,
                    0.213f - c * 0.213f - n * 0.787f, 0.715f - c * 0.715f + n * 0.715f, 0.072f + c * 0.928f + n * 0.072f), Vector4.Zero);
            default:
                return Identity;
        }
    }
}

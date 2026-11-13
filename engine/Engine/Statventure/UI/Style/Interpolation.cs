using System.Numerics;
using Statventure.Core;

namespace Statventure.UI.Style;

/// <summary>
/// How computed values blend for animations and transitions (CSS Values 4 §3, CSS Transforms 2 §15,
/// Filter Effects §11, CSS Backgrounds 3): lengths, numbers, colours (premultiplied), transform lists
/// (pairwise, else by decomposed matrices), filter and shadow lists, clip insets and visibility.
/// Properties without an interpolation are discrete: they flip half way.
/// </summary>
public static class Interpolation
{
    public delegate void Blend(ComputedStyle from, ComputedStyle to, float t, ComputedStyle target);

    static readonly Dictionary<string, Blend> Blends = new(StringComparer.Ordinal);
    static readonly Dictionary<string, Func<ComputedStyle, ComputedStyle, bool>> Equal = new(StringComparer.Ordinal);

    /// <summary>Properties whose changes only repaint (no relayout).</summary>
    public static readonly HashSet<string> PaintOnly =
    [
        "opacity", "transform", "translate", "rotate", "scale", "transform-origin", "filter", "backdrop-filter", "box-shadow", "text-shadow",
        "color", "background-color", "background-image", "background-position", "background-size", "background-repeat",
        "border-top-color", "border-right-color", "border-bottom-color", "border-left-color", "outline-color", "outline-width", "outline-offset", "outline-style",
        "visibility", "clip-path", "pointer-events", "cursor", "mix-blend-mode", "z-index", "accent-color", "caret-color", "text-decoration-color",
        "scrollbar-color", "text-decoration-thickness", "text-underline-offset", "-webkit-text-fill-color", "background-clip",
    ];

    static void Add<T>(string name, Func<ComputedStyle, T> get, Action<ComputedStyle, T> set, Func<T, T, float, T> lerp, Func<T, T, bool>? equals = null)
    {
        Blends[name] = (from, to, t, target) => set(target, lerp(get(from), get(to), t));
        Equal[name] = (a, b) => equals is not null ? equals(get(a), get(b)) : EqualityComparer<T>.Default.Equals(get(a), get(b));
    }

    static float Num(float a, float b, float t) => a + (b - a) * t;

    static Color Col(Color a, Color b, float t)
    {
        // Premultiplied interpolation (CSS Color 4 §12.3).
        var pa = a.Premultiplied;
        var pb = b.Premultiplied;
        var p = Vector4.Lerp(pa, pb, t);
        if (p.W <= 0)
            return Color.Transparent;
        return new Color(p.X / p.W, p.Y / p.W, p.Z / p.W, Math.Clamp(p.W, 0, 1));
    }

    static Color? OptionalColor(Color? a, Color? b, float t, Color currentA, Color currentB) =>
        a is null && b is null ? null : Col(a ?? currentA, b ?? currentB, t);

    static Length Len(Length a, Length b, float t)
    {
        if (a.IsValue && b.IsValue)
            return Length.Lerp(a, b, t);
        return t < 0.5f ? a : b;
    }

    static Interpolation()
    {
        Add("opacity", s => s.Opacity, (s, v) => s.Opacity = Math.Clamp(v, 0, 1), Num);
        Add("color", s => s.Color, (s, v) => s.Color = v, Col);
        Add("background-color", s => s.BackgroundColor, (s, v) => s.BackgroundColor = v, Col);
        foreach (var (side, get, set) in new (string, Func<ComputedStyle, Color?>, Action<ComputedStyle, Color?>)[]
        {
            ("top", s => s.BorderTopColor, (s, v) => s.BorderTopColor = v),
            ("right", s => s.BorderRightColor, (s, v) => s.BorderRightColor = v),
            ("bottom", s => s.BorderBottomColor, (s, v) => s.BorderBottomColor = v),
            ("left", s => s.BorderLeftColor, (s, v) => s.BorderLeftColor = v),
        })
        {
            Blends[$"border-{side}-color"] = (from, to, t, target) => set(target, OptionalColor(get(from), get(to), t, from.Color, to.Color));
            Equal[$"border-{side}-color"] = (a, b) => (get(a) ?? a.Color) == (get(b) ?? b.Color);
        }
        Blends["outline-color"] = (from, to, t, target) => target.OutlineColor = OptionalColor(from.OutlineColor, to.OutlineColor, t, from.Color, to.Color);
        Equal["outline-color"] = (a, b) => (a.OutlineColor ?? a.Color) == (b.OutlineColor ?? b.Color);
        // A decoration's currentcolor is the fill colour (CSSColorInterpolationType's ResolveCurrentColor for text-decoration-color).
        Blends["text-decoration-color"] = (from, to, t, target) => target.TextDecorationColor = OptionalColor(from.TextDecorationColor, to.TextDecorationColor, t, from.TextFillColor ?? from.Color, to.TextFillColor ?? to.Color);
        Equal["text-decoration-color"] = (a, b) => (a.TextDecorationColor ?? a.TextFillColor ?? a.Color) == (b.TextDecorationColor ?? b.TextFillColor ?? b.Color);
        Blends["-webkit-text-fill-color"] = (from, to, t, target) => target.TextFillColor = OptionalColor(from.TextFillColor, to.TextFillColor, t, from.Color, to.Color);
        Equal["-webkit-text-fill-color"] = (a, b) => (a.TextFillColor ?? a.Color) == (b.TextFillColor ?? b.Color);

        foreach (var (name, get, set) in new (string, Func<ComputedStyle, Length>, Action<ComputedStyle, Length>)[]
        {
            ("width", s => s.Width, (s, v) => s.Width = v), ("height", s => s.Height, (s, v) => s.Height = v),
            ("min-width", s => s.MinWidth, (s, v) => s.MinWidth = v), ("min-height", s => s.MinHeight, (s, v) => s.MinHeight = v),
            ("max-width", s => s.MaxWidth, (s, v) => s.MaxWidth = v), ("max-height", s => s.MaxHeight, (s, v) => s.MaxHeight = v),
            ("top", s => s.Top, (s, v) => s.Top = v), ("right", s => s.Right, (s, v) => s.Right = v),
            ("bottom", s => s.Bottom, (s, v) => s.Bottom = v), ("left", s => s.Left, (s, v) => s.Left = v),
            ("margin-top", s => s.MarginTop, (s, v) => s.MarginTop = v), ("margin-right", s => s.MarginRight, (s, v) => s.MarginRight = v),
            ("margin-bottom", s => s.MarginBottom, (s, v) => s.MarginBottom = v), ("margin-left", s => s.MarginLeft, (s, v) => s.MarginLeft = v),
            ("padding-top", s => s.PaddingTop, (s, v) => s.PaddingTop = v), ("padding-right", s => s.PaddingRight, (s, v) => s.PaddingRight = v),
            ("padding-bottom", s => s.PaddingBottom, (s, v) => s.PaddingBottom = v), ("padding-left", s => s.PaddingLeft, (s, v) => s.PaddingLeft = v),
            ("flex-basis", s => s.FlexBasis, (s, v) => s.FlexBasis = v),
            ("row-gap", s => s.RowGap, (s, v) => s.RowGap = v), ("column-gap", s => s.ColumnGap, (s, v) => s.ColumnGap = v),
        })
            Add(name, get, set, Len);
        // Thickness interpolates between lengths (auto and from-font flip at the midpoint); an underline offset likewise.
        Add("text-decoration-thickness", s => s.TextDecorationThickness, (s, v) => s.TextDecorationThickness = v,
            (a, b, t) => a.Kind == DecorationThicknessKind.Length && b.Kind == DecorationThicknessKind.Length ? a with { Length = Len(a.Length, b.Length, t) } : t < 0.5f ? a : b);
        Add("text-underline-offset", s => s.TextUnderlineOffset, (s, v) => s.TextUnderlineOffset = v, Len);
        Add("letter-spacing", s => s.LetterSpacing, (s, v) => s.LetterSpacing = v, Num);
        Add("word-spacing", s => s.WordSpacing, (s, v) => s.WordSpacing = v, Num);
        Add("font-size", s => s.FontSize, (s, v) =>
        {
            s.FontSize = MathF.Max(0, v);
            s.SpecifiedFontSize = s.FontSize / s.Zoom;
        }, Num);
        Add("flex-grow", s => s.FlexGrow, (s, v) => s.FlexGrow = MathF.Max(0, v), Num);
        Add("flex-shrink", s => s.FlexShrink, (s, v) => s.FlexShrink = MathF.Max(0, v), Num);
        Add("outline-width", s => s.OutlineWidth, (s, v) => s.OutlineWidth = MathF.Max(0, v), Num);
        Add("outline-offset", s => s.OutlineOffset, (s, v) => s.OutlineOffset = Properties.OutlineOffset(v), Num);
        foreach (var (name, get, set) in new (string, Func<ComputedStyle, float>, Action<ComputedStyle, float>)[]
        {
            ("border-top-width", s => s.BorderTopWidth, (s, v) => s.BorderTopWidth = v), ("border-right-width", s => s.BorderRightWidth, (s, v) => s.BorderRightWidth = v),
            ("border-bottom-width", s => s.BorderBottomWidth, (s, v) => s.BorderBottomWidth = v), ("border-left-width", s => s.BorderLeftWidth, (s, v) => s.BorderLeftWidth = v),
        })
            Add(name, get, set, (a, b, t) => MathF.Max(0, Num(a, b, t)));
        Add("translate", s => (s.TranslateX, s.TranslateY), (s, v) => (s.TranslateX, s.TranslateY) = v, (a, b, t) => (Len(a.Item1, b.Item1, t), Len(a.Item2, b.Item2, t)));
        Add("rotate", s => s.Rotate, (s, v) => s.Rotate = v, Num);
        Add("scale", s => (s.ScaleX, s.ScaleY), (s, v) => (s.ScaleX, s.ScaleY) = v, (a, b, t) => (Num(a.Item1, b.Item1, t), Num(a.Item2, b.Item2, t)));
        Add("transform-origin", s => (s.TransformOriginX, s.TransformOriginY), (s, v) => (s.TransformOriginX, s.TransformOriginY) = v, (a, b, t) => (Len(a.Item1, b.Item1, t), Len(a.Item2, b.Item2, t)));
        Add("z-index", s => s.ZIndex, (s, v) => s.ZIndex = v, (a, b, t) => a is { } x && b is { } y ? (int)MathF.Round(Num(x, y, t)) : t < 0.5f ? a : b);

        Blends["transform"] = (from, to, t, target) => target.Transform = TransformLists(from.Transform, to.Transform, t);
        Equal["transform"] = (a, b) => a.Transform.SequenceEqual(b.Transform, TransformComparer.Instance);
        Blends["filter"] = (from, to, t, target) => target.Filters = FilterLists(from.Filters, to.Filters, t);
        Equal["filter"] = (a, b) => a.Filters.SequenceEqual(b.Filters);
        Blends["backdrop-filter"] = (from, to, t, target) => target.BackdropFilters = FilterLists(from.BackdropFilters, to.BackdropFilters, t);
        Equal["backdrop-filter"] = (a, b) => a.BackdropFilters.SequenceEqual(b.BackdropFilters);
        Blends["box-shadow"] = (from, to, t, target) => target.BoxShadows = ShadowLists(from.BoxShadows, to.BoxShadows, t);
        Equal["box-shadow"] = (a, b) => a.BoxShadows.SequenceEqual(b.BoxShadows);
        Blends["text-shadow"] = (from, to, t, target) => target.TextShadows = ShadowLists(from.TextShadows, to.TextShadows, t);
        Equal["text-shadow"] = (a, b) => a.TextShadows.SequenceEqual(b.TextShadows);
        Blends["clip-path"] = (from, to, t, target) => target.ClipPath = from.ClipPath is { } a && to.ClipPath is { } b
            ? new ClipInset(Len(a.Top, b.Top, t), Len(a.Right, b.Right, t), Len(a.Bottom, b.Bottom, t), Len(a.Left, b.Left, t),
                a.Radii.Zip(b.Radii, (x, y) => new Corner(Len(x.X, y.X, t), Len(x.Y, y.Y, t))).ToArray())
            : t < 0.5f ? from.ClipPath : to.ClipPath;
        Equal["clip-path"] = (a, b) => Equals(a.ClipPath, b.ClipPath) || (a.ClipPath is { } x && b.ClipPath is { } y && x.Top.Equals(y.Top) && x.Right.Equals(y.Right) && x.Bottom.Equals(y.Bottom) && x.Left.Equals(y.Left) && x.Radii.SequenceEqual(y.Radii));
        Blends["background-position"] = (from, to, t, target) =>
        {
            var a = from.BackgroundPositions;
            var b = to.BackgroundPositions;
            var count = Math.Max(a.Length, b.Length);
            target.BackgroundPositions = Enumerable.Range(0, count).Select(i => new BackgroundPosition(Len(a[i % a.Length].X, b[i % b.Length].X, t), Len(a[i % a.Length].Y, b[i % b.Length].Y, t))).ToArray();
        };
        Equal["background-position"] = (a, b) => a.BackgroundPositions.SequenceEqual(b.BackgroundPositions);
        Blends["background-size"] = (from, to, t, target) =>
        {
            var a = from.BackgroundSizes;
            var b = to.BackgroundSizes;
            var count = Math.Max(a.Length, b.Length);
            target.BackgroundSizes = Enumerable.Range(0, count).Select(i =>
            {
                var x = a[i % a.Length];
                var y = b[i % b.Length];
                return x.Keyword == y.Keyword && x.Keyword.Length == 0 ? new BackgroundSize(Len(x.X, y.X, t), Len(x.Y, y.Y, t), "") : t < 0.5f ? x : y;
            }).ToArray();
        };
        Equal["background-size"] = (a, b) => a.BackgroundSizes.SequenceEqual(b.BackgroundSizes);
        // visibility: visible during the whole interpolation if either end is visible (CSS Values 4 §3.2.2).
        Blends["visibility"] = (from, to, t, target) =>
        {
            if (from.Visibility == to.Visibility)
                target.Visibility = from.Visibility;
            else if (from.Visibility == Visibility.Visible || to.Visibility == Visibility.Visible)
                target.Visibility = t <= 0 ? from.Visibility : t >= 1 ? to.Visibility : Visibility.Visible;
            else
                target.Visibility = t < 0.5f ? from.Visibility : to.Visibility;
        };
        Equal["visibility"] = (a, b) => a.Visibility == b.Visibility;
    }

    /// <summary>Do two styles agree on a property? (Unknown properties compare by their copied value being the same object or value.)</summary>
    public static bool ValuesEqual(string property, ComputedStyle a, ComputedStyle b)
    {
        if (Equal.TryGetValue(property, out var equals))
            return equals(a, b);
        if (Properties.Get(property) is not { } definition)
            return true;
        // Compare by copying each into a fresh style and checking the fields that changed.
        var probeA = new ComputedStyle();
        var probeB = new ComputedStyle();
        definition.Copy(probeA, a);
        definition.Copy(probeB, b);
        return StyleFieldsEqual(probeA, probeB);
    }

    static bool StyleFieldsEqual(ComputedStyle a, ComputedStyle b)
    {
        foreach (var field in typeof(ComputedStyle).GetFields())
        {
            var x = field.GetValue(a);
            var y = field.GetValue(b);
            if (x is System.Collections.IEnumerable ex && y is System.Collections.IEnumerable ey && x is not string)
            {
                if (!ex.Cast<object?>().SequenceEqual(ey.Cast<object?>()))
                    return false;
            }
            else if (!Equals(x, y))
                return false;
        }
        return true;
    }

    /// <summary>Sets the property in <paramref name="target"/> to the blend of the two styles' values at t (0..1, may overshoot).</summary>
    public static void Apply(string property, ComputedStyle from, ComputedStyle to, float t, ComputedStyle target)
    {
        if (Blends.TryGetValue(property, out var blend))
            blend(from, to, t, target);
        else if (Properties.Get(property) is { } definition)
            definition.Copy(target, t < 0.5f ? from : to);
    }

    public static bool IsInterpolable(string property) => Blends.ContainsKey(property);

    // ---------- Transforms ----------

    sealed class TransformComparer : IEqualityComparer<TransformFunction>
    {
        public static readonly TransformComparer Instance = new();
        public bool Equals(TransformFunction? a, TransformFunction? b) =>
            a is not null && b is not null && a.Kind == b.Kind && a.X.Equals(b.X) && a.Y.Equals(b.Y) && a.A == b.A && a.B == b.B
            && (a.Matrix is null ? b.Matrix is null : b.Matrix is not null && a.Matrix.SequenceEqual(b.Matrix));
        public int GetHashCode(TransformFunction f) => HashCode.Combine(f.Kind, f.A, f.B);
    }

    static TransformFunction Identity(TransformFunction like) => like.Kind switch
    {
        TransformKind.Translate => new TransformFunction(TransformKind.Translate, Length.Zero, Length.Zero),
        TransformKind.Scale => new TransformFunction(TransformKind.Scale, default, default, 1, 1),
        TransformKind.Rotate => new TransformFunction(TransformKind.Rotate, default, default, 0),
        TransformKind.Skew => new TransformFunction(TransformKind.Skew, default, default, 0, 0),
        _ => new TransformFunction(TransformKind.Matrix, default, default, Matrix: [1, 0, 0, 1, 0, 0]),
    };

    static TransformFunction[] TransformLists(TransformFunction[] a, TransformFunction[] b, float t)
    {
        if (a.Length == 0 && b.Length == 0)
            return [];
        if (a.Length == 0)
            a = b.Select(Identity).ToArray();
        else if (b.Length == 0)
            b = a.Select(Identity).ToArray();
        // Pairwise when the shorter list's functions match the longer's start (the rest from identity).
        var count = Math.Max(a.Length, b.Length);
        var pairwise = true;
        for (var i = 0; i < count && pairwise; i++)
        {
            var fa = i < a.Length ? a[i] : null;
            var fb = i < b.Length ? b[i] : null;
            if (fa is not null && fb is not null && fa.Kind != fb.Kind)
                pairwise = false;
        }
        if (pairwise)
        {
            var result = new TransformFunction[count];
            for (var i = 0; i < count; i++)
            {
                var fa = i < a.Length ? a[i] : Identity(b[i]);
                var fb = i < b.Length ? b[i] : Identity(a[i]);
                result[i] = fa.Kind switch
                {
                    TransformKind.Translate => new TransformFunction(TransformKind.Translate, Len(fa.X, fb.X, t), Len(fa.Y, fb.Y, t)),
                    TransformKind.Matrix => MatrixFunction(Decomposed.Lerp(Decomposed.Of(ToMatrix(fa)), Decomposed.Of(ToMatrix(fb)), t).Recompose()),
                    _ => new TransformFunction(fa.Kind, default, default, Num(fa.A, fb.A, t), Num(fa.B, fb.B, t)),
                };
            }
            return result;
        }
        // Otherwise through decomposed matrices (translate percentages resolve against no box here).
        var ma = Compose(a);
        var mb = Compose(b);
        return [MatrixFunction(Decomposed.Lerp(Decomposed.Of(ma), Decomposed.Of(mb), t).Recompose())];
    }

    static Matrix3x2 ToMatrix(TransformFunction f) => f.ToMatrix(Vector2.Zero);

    static Matrix3x2 Compose(TransformFunction[] list)
    {
        var m = Matrix3x2.Identity;
        foreach (var f in list.Reverse())
            m *= ToMatrix(f);
        return m;
    }

    static TransformFunction MatrixFunction(Matrix3x2 m) =>
        new(TransformKind.Matrix, default, default, Matrix: [m.M11, m.M12, m.M21, m.M22, m.M31, m.M32]);

    /// <summary>A 2D matrix decomposed into translation, rotation, scale and skew (CSS Transforms 1 §20.1, "unmatrix").</summary>
    readonly record struct Decomposed(Vector2 Translate, Vector2 Scale, float Angle, float Skew)
    {
        public static Decomposed Of(Matrix3x2 m)
        {
            float a = m.M11, b = m.M12, c = m.M21, d = m.M22;
            var scaleX = MathF.Sqrt(a * a + b * b);
            if (scaleX != 0)
            {
                a /= scaleX;
                b /= scaleX;
            }
            var skew = a * c + b * d;
            c -= a * skew;
            d -= b * skew;
            var scaleY = MathF.Sqrt(c * c + d * d);
            if (scaleY != 0)
            {
                c /= scaleY;
                d /= scaleY;
                skew /= scaleY;
            }
            // A negative determinant flips one axis.
            if (a * d - b * c < 0)
            {
                a = -a;
                b = -b;
                skew = -skew;
                scaleX = -scaleX;
            }
            var angle = MathF.Atan2(b, a);
            return new Decomposed(new Vector2(m.M31, m.M32), new Vector2(scaleX, scaleY), angle, skew);
        }

        public static Decomposed Lerp(Decomposed x, Decomposed y, float t)
        {
            var ax = x.Angle;
            var ay = y.Angle;
            // Rotate the short way round.
            if (MathF.Abs(ax - ay) > MathF.PI)
            {
                if (ax > ay)
                    ax -= 2 * MathF.PI;
                else
                    ay -= 2 * MathF.PI;
            }
            return new Decomposed(Vector2.Lerp(x.Translate, y.Translate, t), Vector2.Lerp(x.Scale, y.Scale, t), Num(ax, ay, t), Num(x.Skew, y.Skew, t));
        }

        /// <summary>scale, then skew, then rotate, then translate (row vectors: S·K·R·T).</summary>
        public Matrix3x2 Recompose()
        {
            var m = Matrix3x2.CreateScale(Scale) * new Matrix3x2(1, 0, Skew, 1, 0, 0) * Matrix3x2.CreateRotation(Angle);
            m.M31 = Translate.X;
            m.M32 = Translate.Y;
            return m;
        }
    }

    // ---------- Filters and shadows ----------

    static FilterFunction InitialFilter(FilterFunction like) => like.Kind switch
    {
        FilterFunctionKind.Blur => like with { Amount = 0 },
        FilterFunctionKind.DropShadow => like with { Amount = 0, OffsetX = 0, OffsetY = 0, Color = Color.Transparent },
        FilterFunctionKind.Grayscale or FilterFunctionKind.Sepia or FilterFunctionKind.Invert or FilterFunctionKind.HueRotate => like with { Amount = 0 },
        _ => like with { Amount = 1 },
    };

    static FilterFunction[] FilterLists(FilterFunction[] a, FilterFunction[] b, float t)
    {
        var count = Math.Max(a.Length, b.Length);
        var result = new FilterFunction[count];
        for (var i = 0; i < count; i++)
        {
            var fa = i < a.Length ? a[i] : InitialFilter(b[i]);
            var fb = i < b.Length ? b[i] : InitialFilter(a[i]);
            if (fa.Kind != fb.Kind)
                return t < 0.5f ? a : b;
            result[i] = new FilterFunction(fa.Kind, Num(fa.Amount, fb.Amount, t), Num(fa.OffsetX, fb.OffsetX, t), Num(fa.OffsetY, fb.OffsetY, t), Col(fa.Color, fb.Color, t));
        }
        return result;
    }

    static ShadowValue[] ShadowLists(ShadowValue[] a, ShadowValue[] b, float t)
    {
        var count = Math.Max(a.Length, b.Length);
        var result = new ShadowValue[count];
        for (var i = 0; i < count; i++)
        {
            var sa = i < a.Length ? a[i] : null;
            var sb = i < b.Length ? b[i] : null;
            sa ??= new ShadowValue(0, 0, 0, 0, Color.Transparent, sb!.Inset);
            sb ??= new ShadowValue(0, 0, 0, 0, Color.Transparent, sa.Inset);
            if (sa.Inset != sb.Inset)
                return t < 0.5f ? a : b;
            result[i] = new ShadowValue(Num(sa.OffsetX, sb.OffsetX, t), Num(sa.OffsetY, sb.OffsetY, t), MathF.Max(0, Num(sa.Blur, sb.Blur, t)), Num(sa.Spread, sb.Spread, t), Col(sa.Color, sb.Color, t), sa.Inset);
        }
        return result;
    }
}

using System.Linq.Expressions;
using System.Reflection;

namespace Statventure.UI.Style;

/// <summary>What a style change requires, from least to most work.</summary>
public enum StyleChange
{
    None,
    /// <summary>Only painting differs (colours, backgrounds, transforms, shadows ...).</summary>
    Paint,
    /// <summary>Sizes or positions may differ: the box needs a new layout.</summary>
    Layout,
    /// <summary>The boxes the element generates differ (display, position, generated content ...): they are rebuilt.</summary>
    Box,
}

/// <summary>
/// Compares two computed styles field by field (structurally, arrays and records included) and
/// says what the difference requires. Every field of <see cref="ComputedStyle"/> counts as
/// affecting layout unless it is listed here as paint-only, as not rendered, as changing boxes, or as
/// a border style or width (layout only when a used width changes).
/// </summary>
public static class StyleDifference
{
    /// <summary>Fields that change the box tree (CSS Display, positioning schemes, generated content, markers).</summary>
    static readonly string[] BoxFields =
    [
        nameof(ComputedStyle.Display), nameof(ComputedStyle.Position), nameof(ComputedStyle.Content),
        nameof(ComputedStyle.ListStyleType), nameof(ComputedStyle.ListStyleInside),
        // Whether white-space-only text generates boxes between blocks.
        nameof(ComputedStyle.WhiteSpace),
    ];

    /// <summary>Fields only painting (and hit testing) reads.</summary>
    static readonly string[] PaintFields =
    [
        nameof(ComputedStyle.BorderTopColor), nameof(ComputedStyle.BorderRightColor), nameof(ComputedStyle.BorderBottomColor), nameof(ComputedStyle.BorderLeftColor),
        nameof(ComputedStyle.RadiusTopLeft), nameof(ComputedStyle.RadiusTopRight), nameof(ComputedStyle.RadiusBottomRight), nameof(ComputedStyle.RadiusBottomLeft),
        nameof(ComputedStyle.ZIndex), nameof(ComputedStyle.Color), nameof(ComputedStyle.TextShadows), nameof(ComputedStyle.Visibility),
        nameof(ComputedStyle.PointerEvents), nameof(ComputedStyle.Interactivity), nameof(ComputedStyle.IsInert), nameof(ComputedStyle.Cursor), nameof(ComputedStyle.CursorImages), nameof(ComputedStyle.UserSelect), nameof(ComputedStyle.AccentColor),
        nameof(ComputedStyle.CaretColor), nameof(ComputedStyle.ScrollbarThumb), nameof(ComputedStyle.ScrollbarTrack),
        nameof(ComputedStyle.Underline), nameof(ComputedStyle.LineThrough), nameof(ComputedStyle.Overline), nameof(ComputedStyle.TextDecorationColor),
        nameof(ComputedStyle.TextDecorationStyle), nameof(ComputedStyle.Resize), nameof(ComputedStyle.TextDecorationThickness), nameof(ComputedStyle.TextUnderlineOffset),
        nameof(ComputedStyle.AppliedDecorations), nameof(ComputedStyle.BaseDecorations), nameof(ComputedStyle.DecorationBlink), nameof(ComputedStyle.TextUnderlinePosition), nameof(ComputedStyle.TextDecorationSkipInk),
        nameof(ComputedStyle.Opacity), nameof(ComputedStyle.BackgroundColor), nameof(ComputedStyle.BackgroundImages), nameof(ComputedStyle.BackgroundSizes),
        nameof(ComputedStyle.BackgroundPositions), nameof(ComputedStyle.BackgroundRepeats), nameof(ComputedStyle.BackgroundFixed), nameof(ComputedStyle.Backgrounds),
        nameof(ComputedStyle.BoxShadows), nameof(ComputedStyle.Transform), nameof(ComputedStyle.TransformOriginX), nameof(ComputedStyle.TransformOriginY), nameof(ComputedStyle.TransformBox),
        nameof(ComputedStyle.TranslateX), nameof(ComputedStyle.TranslateY), nameof(ComputedStyle.ScaleX), nameof(ComputedStyle.ScaleY), nameof(ComputedStyle.Rotate),
        nameof(ComputedStyle.Filters), nameof(ComputedStyle.BackdropFilters), nameof(ComputedStyle.MixBlendMode), nameof(ComputedStyle.Isolate), nameof(ComputedStyle.WillChange),
        nameof(ComputedStyle.ClipPath), nameof(ComputedStyle.OutlineWidth), nameof(ComputedStyle.OutlineStyle), nameof(ComputedStyle.OutlineColor),
        nameof(ComputedStyle.OutlineOffset), nameof(ComputedStyle.ObjectFit), nameof(ComputedStyle.ClipRect),
    ];

    /// <summary>
    /// Border styles and widths (Blink's kBorderWidth group): a change needs layout only when a used
    /// width differs (0 for none and hidden), otherwise only painting (ComputedStyle::DiffNeedsFullLayout).
    /// </summary>
    static readonly string[] BorderFields =
    [
        nameof(ComputedStyle.BorderTopWidth), nameof(ComputedStyle.BorderRightWidth), nameof(ComputedStyle.BorderBottomWidth), nameof(ComputedStyle.BorderLeftWidth),
        nameof(ComputedStyle.BorderTopStyle), nameof(ComputedStyle.BorderRightStyle), nameof(ComputedStyle.BorderBottomStyle), nameof(ComputedStyle.BorderLeftStyle),
    ];

    /// <summary>Fields that don't render: the animation engine reads transitions and animations as styles change; custom properties reach the properties that use them.</summary>
    static readonly string[] IgnoredFields =
    [
        nameof(ComputedStyle.TransitionProperty), nameof(ComputedStyle.TransitionDuration), nameof(ComputedStyle.TransitionTiming), nameof(ComputedStyle.TransitionDelay),
        nameof(ComputedStyle.AnimationName), nameof(ComputedStyle.AnimationDuration), nameof(ComputedStyle.AnimationTiming), nameof(ComputedStyle.AnimationDelay),
        nameof(ComputedStyle.AnimationIterationCount), nameof(ComputedStyle.AnimationDirectionList), nameof(ComputedStyle.AnimationFillModeList),
        nameof(ComputedStyle.AnimationPaused), nameof(ComputedStyle.Transitions), nameof(ComputedStyle.Animations), nameof(ComputedStyle.Custom),
        nameof(ComputedStyle.ScrollBehavior), nameof(ComputedStyle.ScrollMarginTop), nameof(ComputedStyle.ScrollMarginRight),
        nameof(ComputedStyle.ScrollMarginBottom), nameof(ComputedStyle.ScrollMarginLeft), nameof(ComputedStyle.ScrollPaddingTop),
        nameof(ComputedStyle.ScrollPaddingRight), nameof(ComputedStyle.ScrollPaddingBottom), nameof(ComputedStyle.ScrollPaddingLeft),
        nameof(ComputedStyle.OverscrollBehaviorX), nameof(ComputedStyle.OverscrollBehaviorY),
    ];

    static readonly Func<ComputedStyle, ComputedStyle, bool> BoxEqual, LayoutEqual, BorderEqual, PaintEqual, AllEqual;

    static StyleDifference()
    {
        var fields = typeof(ComputedStyle).GetFields(BindingFlags.Public | BindingFlags.Instance);
        var known = BoxFields.Concat(PaintFields).Concat(BorderFields).Concat(IgnoredFields).ToHashSet();
        foreach (var name in known)
            if (fields.All(f => f.Name != name))
                throw new InvalidOperationException("StyleDifference names a field ComputedStyle doesn't have: " + name);
        BoxEqual = Comparer(fields.Where(f => BoxFields.Contains(f.Name)));
        BorderEqual = Comparer(fields.Where(f => BorderFields.Contains(f.Name)));
        PaintEqual = Comparer(fields.Where(f => PaintFields.Contains(f.Name)));
        LayoutEqual = Comparer(fields.Where(f => !known.Contains(f.Name)));
        AllEqual = Comparer(fields.Where(f => f.Name != nameof(ComputedStyle.Custom)));
    }

    /// <summary>Are the two styles the same in every field (custom properties included)?</summary>
    public static bool Identical(ComputedStyle? a, ComputedStyle? b)
    {
        if (ReferenceEquals(a, b))
            return true;
        if (a is null || b is null || !AllEqual(a, b))
            return false;
        if (ReferenceEquals(a.Custom, b.Custom))
            return true;
        if (a.Custom.Count != b.Custom.Count)
            return false;
        foreach (var (name, value) in a.Custom)
            if (!b.Custom.TryGetValue(name, out var other) || !(ReferenceEquals(value, other) || value.SequenceEqual(other)))
                return false;
        return true;
    }

    public static StyleChange Compare(ComputedStyle? a, ComputedStyle? b)
    {
        if (ReferenceEquals(a, b))
            return StyleChange.None;
        if (a is null || b is null)
            return StyleChange.Box;
        // Transforms and filters make a box the containing block of positioned descendants.
        if (!BoxEqual(a, b) || ContainsPositioned(a) != ContainsPositioned(b) || ContainsFixed(a) != ContainsFixed(b))
            return StyleChange.Box;
        if (!LayoutEqual(a, b) || !UsedBorderWidthsEqual(a, b))
            return StyleChange.Layout;
        return PaintEqual(a, b) && BorderEqual(a, b) ? StyleChange.None : StyleChange.Paint;
    }

    static bool UsedBorderWidthsEqual(ComputedStyle a, ComputedStyle b) =>
        a.BorderTop == b.BorderTop && a.BorderRight == b.BorderRight && a.BorderBottom == b.BorderBottom && a.BorderLeft == b.BorderLeft;

    /// <summary>The box is the containing block of absolutely positioned descendants.</summary>
    public static bool ContainsPositioned(ComputedStyle s) => s.Position != PositionKind.Static || s.HasTransform || s.Filters.Length > 0 || s.WillChangeContainsFixed;

    /// <summary>The box is the containing block of fixed positioned descendants.</summary>
    public static bool ContainsFixed(ComputedStyle s) => s.HasTransform || s.Filters.Length > 0 || s.BackdropFilters.Length > 0 || s.WillChangeContainsFixed;

    static Func<ComputedStyle, ComputedStyle, bool> Comparer(IEnumerable<FieldInfo> fields)
    {
        var a = Expression.Parameter(typeof(ComputedStyle), "a");
        var b = Expression.Parameter(typeof(ComputedStyle), "b");
        Expression body = Expression.Constant(true);
        foreach (var field in fields)
            body = Expression.AndAlso(body, DeepEquality.Call(field.FieldType, Expression.Field(a, field), Expression.Field(b, field)));
        return Expression.Lambda<Func<ComputedStyle, ComputedStyle, bool>>(body, a, b).Compile();
    }
}

/// <summary>Structural equality: values by their fields (records included), arrays element by element.</summary>
internal static class DeepEquality
{
    public static Expression Call(Type type, Expression a, Expression b) =>
        Expression.Call(typeof(Deep<>).MakeGenericType(type).GetMethod(nameof(Deep<int>.Equal))!, a, b);

    internal static Func<T, T, bool> Build<T>()
    {
        var type = typeof(T);
        if (type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal))
            return EqualityComparer<T>.Default.Equals;
        if (Nullable.GetUnderlyingType(type) is { } underlying)
            return (Func<T, T, bool>)Delegate.CreateDelegate(typeof(Func<T, T, bool>), typeof(DeepEquality).GetMethod(nameof(NullableEqual), BindingFlags.NonPublic | BindingFlags.Static)!.MakeGenericMethod(underlying));
        if (type.IsArray)
            return (Func<T, T, bool>)Delegate.CreateDelegate(typeof(Func<T, T, bool>), typeof(DeepEquality).GetMethod(nameof(ArrayEqual), BindingFlags.NonPublic | BindingFlags.Static)!.MakeGenericMethod(type.GetElementType()!));
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            return (Func<T, T, bool>)Delegate.CreateDelegate(typeof(Func<T, T, bool>), typeof(DeepEquality).GetMethod(nameof(ListEqual), BindingFlags.NonPublic | BindingFlags.Static)!.MakeGenericMethod(type.GetGenericArguments()[0]));
        if (type.Namespace?.StartsWith("Statventure", StringComparison.Ordinal) != true || typeof(Delegate).IsAssignableFrom(type))
            return EqualityComparer<T>.Default.Equals;
        // Our own records, classes and structs: every instance field (records keep their values in backing fields).
        var a = Expression.Parameter(type, "a");
        var b = Expression.Parameter(type, "b");
        Expression fields = Expression.Constant(true);
        for (var t = type; t is not null && t != typeof(object) && t != typeof(ValueType); t = t.BaseType)
            foreach (var field in t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                fields = Expression.AndAlso(fields, Call(field.FieldType, Expression.Field(a, field), Expression.Field(b, field)));
        Expression body = fields;
        if (!type.IsValueType)
        {
            var nil = Expression.Constant(null, type);
            body = Expression.OrElse(Expression.ReferenceEqual(a, b),
                Expression.AndAlso(Expression.AndAlso(Expression.ReferenceNotEqual(a, nil), Expression.ReferenceNotEqual(b, nil)), fields));
        }
        return Expression.Lambda<Func<T, T, bool>>(body, a, b).Compile();
    }

    static bool NullableEqual<T>(T? a, T? b) where T : struct =>
        a.HasValue == b.HasValue && (!a.HasValue || Deep<T>.Equal(a.GetValueOrDefault(), b.GetValueOrDefault()));

    static bool ArrayEqual<T>(T[]? a, T[]? b)
    {
        if (ReferenceEquals(a, b))
            return true;
        if (a is null || b is null || a.Length != b.Length)
            return false;
        for (var i = 0; i < a.Length; i++)
            if (!Deep<T>.Equal(a[i], b[i]))
                return false;
        return true;
    }

    static bool ListEqual<T>(List<T>? a, List<T>? b)
    {
        if (ReferenceEquals(a, b))
            return true;
        if (a is null || b is null || a.Count != b.Count)
            return false;
        for (var i = 0; i < a.Count; i++)
            if (!Deep<T>.Equal(a[i], b[i]))
                return false;
        return true;
    }
}

internal static class Deep<T>
{
    static Func<T, T, bool>? _equal;

    public static bool Equal(T a, T b) => (_equal ??= DeepEquality.Build<T>())(a, b);
}

using Statventure.Core;
using Statventure.UI.Css;

namespace Statventure.UI.Style;

/// <summary>
/// Custom properties' values as animations and transitions take them (Blink's interpolation types of a custom
/// property, InterpolationTypesMap::CreateInterpolationTypesForCSSSyntax): a registered colour interpolates smoothly
/// (CSSColorInterpolationType: premultiplied, in sRGB's 0–255 parameters, currentcolor resolved against the element's
/// colour, the result a computed colour); anything else flips half way (CSSDefaultInterpolationType), a registered
/// property's invalid value then taking its unset value.
/// </summary>
static class CustomValues
{
    /// <summary>
    /// A keyframe's value of a custom property (CSSInterpolationType::MaybeConvertCustomPropertyDeclaration): var()
    /// substituted from the element's custom properties (unresolved: unset), a CSS-wide keyword as one, a registered
    /// value computed (null when invalid at computed-value time).
    /// </summary>
    public static List<CssToken>? OfKeyframe(PropertyRegistration? registration, PreparedDeclaration declaration, ComputedStyle element, ComputedStyle parent)
    {
        var value = declaration.HasVar ? StyleEngine.Substitute(declaration.Value, element.Custom, null) : declaration.Value;
        var keyword = value is null ? "unset" : StyleEngine.WideKeyword(value);
        if (keyword is not null)
            return keyword switch
            {
                "initial" => registration?.Initial,
                "inherit" => parent.Custom.GetValueOrDefault(declaration.Property) ?? registration?.Initial,
                _ => registration is { Inherits: false } ? registration.Initial : parent.Custom.GetValueOrDefault(declaration.Property) ?? registration?.Initial,
            };
        return registration is not null ? PropertyRegistration.ComputeColor(value!) : value;
    }

    /// <summary>Whether two values interpolate smoothly: a registered colour's, both colours.</summary>
    public static bool Smooth(PropertyRegistration? registration, List<CssToken>? from, List<CssToken>? to) =>
        registration is not null && Rgba(from, Color.Black) is not null && Rgba(to, Color.Black) is not null;

    /// <summary>The value at <paramref name="t"/> (eased progress) from one value to another.</summary>
    public static List<CssToken>? Blend(PropertyRegistration? registration, List<CssToken>? from, List<CssToken>? to, double t, ComputedStyle element, ComputedStyle parent)
    {
        if (registration is not null && Rgba(from, element.Color) is { } a && Rgba(to, element.Color) is { } b)
        {
            // InterpolableColor: parameters premultiplied by alpha, blended, then divided again (as floats).
            double Blend(double x, double y) => x + (y - x) * t;
            var alpha = Blend(a.A, b.A);
            if (alpha <= 0)
                return CssParser.Trim(CssTokenizer.Tokenize("rgba(0, 0, 0, 0)"));
            var text = PropertyRegistration.SerializeLegacy(
                (float)(Blend(a.R * a.A, b.R * b.A) / alpha), (float)(Blend(a.G * a.A, b.G * b.A) / alpha), (float)(Blend(a.B * a.A, b.B * b.A) / alpha),
                Math.Clamp((float)alpha, 0f, 1f));
            return CssParser.Trim(CssTokenizer.Tokenize(text));
        }
        var chosen = t < 0.5 ? from : to;
        // A registered property's value that isn't one is invalid at computed-value time: its unset value.
        if (registration is not null && chosen is null)
            return registration.Inherits ? parent.Custom.GetValueOrDefault(registration.Name) ?? registration.Initial : registration.Initial;
        return chosen;
    }

    /// <summary>
    /// A registered colour's value as sRGB parameters 0–255 and alpha: anything the engine parses as a colour
    /// (currentcolor as <paramref name="current"/>); null when it isn't one.
    /// </summary>
    static (double R, double G, double B, double A)? Rgba(List<CssToken>? value, Color current)
    {
        if (value is null)
            return null;
        var reader = new ValueReader(value);
        if (!reader.TryColor(current, out var color) || !reader.AtEnd)
            return null;
        return (color.R * 255.0, color.G * 255.0, color.B * 255.0, color.A);
    }
}

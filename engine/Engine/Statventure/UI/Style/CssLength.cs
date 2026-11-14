using Statventure.UI.Css;

namespace Statventure.UI.Css
{
    public static partial class CssLength
    {
        /// <summary>A length token in pixels using only absolute units (media queries).</summary>
        public static double ToPixelsAbsolute(CssToken token) => token.Type switch
        {
            CssTokenType.Number => token.Number,
            CssTokenType.Dimension => token.Unit switch
            {
                "px" => token.Number,
                "em" or "rem" => token.Number * 16,
                "pt" => token.Number * 96 / 72,
                "pc" => token.Number * 16,
                "in" => token.Number * 96,
                "cm" => token.Number * 96 / 2.54,
                "mm" => token.Number * 96 / 25.4,
                _ => token.Number,
            },
            _ => 0,
        };
    }
}

namespace Statventure.UI.Style
{
    /// <summary>
    /// What units resolve against when a value is computed, as Blink's CSSToLengthConversionData:
    /// the element's and the root's specified (unzoomed) font sizes, the viewport in CSS px, the
    /// computed (zoomed) line height, and the zoom (CSSLengthResolver::ZoomedComputedPixels: every
    /// length is its CSS px value times the zoom; em is value × (specified size × zoom); in double,
    /// as Blink computes them).
    /// </summary>
    public readonly record struct UnitContext(float FontSize, float RootFontSize, float ViewportWidth, float ViewportHeight, float LineHeight = 0, float Zoom = 1)
    {
        public float Resolve(double value, string unit) => (float)(unit switch
        {
            "px" => value * Zoom,
            "em" => value * (FontSize * Zoom),
            "rem" => value * (RootFontSize * Zoom),
            "vw" or "dvw" or "svw" or "lvw" => value * (ViewportWidth / 100.0) * Zoom,
            "vh" or "dvh" or "svh" or "lvh" => value * (ViewportHeight / 100.0) * Zoom,
            "vmin" or "dvmin" or "svmin" or "lvmin" => value * (Math.Min(ViewportWidth, ViewportHeight) / 100.0) * Zoom,
            "vmax" or "dvmax" or "svmax" or "lvmax" => value * (Math.Max(ViewportWidth, ViewportHeight) / 100.0) * Zoom,
            "ch" => value * (FontSize * Zoom * 0.5f),
            "ex" => value * (FontSize * Zoom * 0.5f),
            "lh" => value * (LineHeight > 0 ? LineHeight : FontSize * Zoom * 1.2f),
            "pt" => value * (96.0 / 72.0) * Zoom,
            "pc" => value * 16.0 * Zoom,
            "in" => value * 96.0 * Zoom,
            "cm" => value * (96.0 / 2.54) * Zoom,
            "mm" => value * (96.0 / 25.4) * Zoom,
            "q" => value * (96.0 / 101.6) * Zoom,
            _ => double.NaN,
        });
    }

    public enum LengthKind : byte
    {
        /// <summary>Pixels plus a percentage of the reference (calc results are linear in both).</summary>
        Value,
        Auto,
        None,
        MinContent,
        MaxContent,
        FitContent,
        Min,
        Max,
        Clamp,
    }

    /// <summary>
    /// A computed length: pixels + percent% of whatever percentages refer to (so calc(100% - 20px)
    /// stays exact until layout knows the reference), or a keyword, or a min/max/clamp of lengths
    /// that still need that reference. A percentage stays one at zero (Blink's Length of type percent): 0% isn't
    /// 0px, since against a reference that isn't known (an indefinite height) it resolves to nothing.
    /// </summary>
    public readonly struct Length : IEquatable<Length>
    {
        public readonly float Pixels;
        public readonly float Percent;
        public readonly LengthKind Kind;
        readonly Length[]? _arguments;
        /// <summary>Whether it has a percentage, of any amount, zero too.</summary>
        readonly bool _percentage;

        public Length(float pixels, float percent = 0, LengthKind kind = LengthKind.Value, Length[]? arguments = null, bool percentage = false)
        {
            Pixels = pixels;
            Percent = percent;
            Kind = kind;
            _arguments = arguments;
            _percentage = kind == LengthKind.Value && (percentage || percent != 0);
        }

        public static readonly Length Zero = new(0);
        public static readonly Length Auto = new(0, 0, LengthKind.Auto);
        public static readonly Length None = new(0, 0, LengthKind.None);
        public static readonly Length MinContent = new(0, 0, LengthKind.MinContent);
        public static readonly Length MaxContent = new(0, 0, LengthKind.MaxContent);
        public static readonly Length FitContent = new(0, 0, LengthKind.FitContent);

        public static Length Px(float pixels) => new(pixels);
        public static Length Pct(float percent) => new(0, percent, percentage: true);

        public bool IsAuto => Kind == LengthKind.Auto;
        public bool IsNone => Kind == LengthKind.None;
        public bool IsValue => Kind == LengthKind.Value;
        public bool HasPercent => Kind == LengthKind.Value ? _percentage : _arguments?.Any(a => a.HasPercent) ?? false;
        public bool IsDefinite => Kind is LengthKind.Value or LengthKind.Min or LengthKind.Max or LengthKind.Clamp;
        public bool IsZero => Kind == LengthKind.Value && Pixels == 0 && Percent == 0;
        public IReadOnlyList<Length> Arguments => _arguments ?? [];

        /// <summary>The length in pixels, given what 100% is (NaN when percentages can't be resolved).</summary>
        public float Resolve(float reference)
        {
            switch (Kind)
            {
                case LengthKind.Value:
                    if (!_percentage)
                        return Pixels;
                    return float.IsNaN(reference) ? float.NaN : Pixels + Percent * reference / 100f;
                case LengthKind.Min:
                    return _arguments!.Min(a => a.Resolve(reference));
                case LengthKind.Max:
                    return _arguments!.Max(a => a.Resolve(reference));
                case LengthKind.Clamp:
                    var min = _arguments![0].Resolve(reference);
                    var value = _arguments[1].Resolve(reference);
                    var max = _arguments[2].Resolve(reference);
                    return MathF.Max(min, MathF.Min(value, max));
                default:
                    return float.NaN;
            }
        }

        /// <summary>Resolve, with auto/none (or an unresolvable percentage) giving <paramref name="fallback"/>.</summary>
        public float ResolveOr(float reference, float fallback)
        {
            var value = Resolve(reference);
            return float.IsNaN(value) ? fallback : value;
        }

        public static Length Lerp(Length a, Length b, float t)
        {
            if (a.Kind == LengthKind.Value && b.Kind == LengthKind.Value)
                return new Length(a.Pixels + (b.Pixels - a.Pixels) * t, a.Percent + (b.Percent - a.Percent) * t, percentage: a._percentage || b._percentage);
            return t < 0.5f ? a : b;
        }

        public static Length operator +(Length a, Length b) => new(a.Pixels + b.Pixels, a.Percent + b.Percent, percentage: a._percentage || b._percentage);
        public static Length operator -(Length a, Length b) => new(a.Pixels - b.Pixels, a.Percent - b.Percent, percentage: a._percentage || b._percentage);
        public static Length operator *(Length a, float f) => new(a.Pixels * f, a.Percent * f, percentage: a._percentage);

        public bool Equals(Length other) =>
            Pixels == other.Pixels && Percent == other.Percent && Kind == other.Kind && _percentage == other._percentage && Arguments.SequenceEqual(other.Arguments);

        public override bool Equals(object? obj) => obj is Length other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Pixels, Percent, Kind, _percentage);

        public override string ToString() => Kind switch
        {
            LengthKind.Value when !_percentage => $"{Pixels}px",
            LengthKind.Value when Pixels == 0 => $"{Percent}%",
            LengthKind.Value => $"calc({Percent}% + {Pixels}px)",
            LengthKind.Min or LengthKind.Max or LengthKind.Clamp => $"{Kind.ToString().ToLowerInvariant()}({string.Join(", ", Arguments)})",
            _ => Kind.ToString().ToLowerInvariant(),
        };
    }
}

using Statventure.Core;
using Statventure.UI.Css;
using TextNs = Statventure.Text;

namespace Statventure.UI.Style;

/// <summary>What a property value is computed against.</summary>
public sealed class StyleContext
{
    public required UnitContext Units;
    public required ComputedStyle Parent;
    public required ComputedStyle Style;
}

/// <summary>
/// The supported CSS properties: for each, whether it inherits, how its value is parsed into the
/// computed style, and how it is copied (for inheritance and initial values). Shorthands expand into
/// longhand token lists.
/// </summary>
public static partial class Properties
{
    public delegate bool Parser(ValueReader reader, StyleContext context);

    public sealed record Property(string Name, bool Inherited, Parser Parse, Action<ComputedStyle, ComputedStyle> Copy);

    static readonly Dictionary<string, Property> Longhands = new(StringComparer.Ordinal);
    static readonly Dictionary<string, Func<List<CssToken>, List<(string, List<CssToken>)>?>> Shorthands = new(StringComparer.Ordinal);
    static readonly HashSet<string> Ignored =
    [
        "will-change", "contain", "font-display", "src", "-webkit-box-orient", "list-style-image",
        "counter-reset", "counter-increment", "-webkit-font-smoothing", "-moz-osx-font-smoothing", "text-rendering",
        "font-feature-settings", "font-kerning", "-webkit-tap-highlight-color", "touch-action",
        "content-visibility", "contain-intrinsic-size", "text-size-adjust", "-webkit-text-size-adjust", "speak", "forced-color-adjust",
        "color-scheme", "print-color-adjust", "-webkit-print-color-adjust", "tab-size", "hyphens", "orphans", "widows", "image-rendering",
        "font-optical-sizing", "font-synthesis", "-webkit-user-drag", "scroll-snap-type", "scroll-snap-align",
        "unicode-range", "background-origin",
        "table-layout", "border-collapse", "border-spacing", "quotes", "direction",
        "break-inside", "break-before", "break-after", "page-break-inside", "paint-order", "text-anchor", "dominant-baseline",
        // SVG presentation properties: the engine draws vector art itself instead of an SVG DOM.
        "fill", "fill-opacity", "fill-rule", "stroke", "stroke-width", "stroke-opacity", "stroke-dasharray", "stroke-dashoffset",
        "stroke-linecap", "stroke-linejoin", "stroke-miterlimit", "stop-color", "stop-opacity", "vector-effect", "shape-rendering",
    ];

    public static readonly ComputedStyle Initial = new();

    public static Property? Get(string name) => Longhands.GetValueOrDefault(Canonical(name));
    public static bool IsShorthand(string name) => Shorthands.ContainsKey(Canonical(name));
    public static bool IsIgnored(string name) => Ignored.Contains(name);
    public static bool IsKnown(string name) => Longhands.ContainsKey(Canonical(name)) || Shorthands.ContainsKey(Canonical(name)) || Ignored.Contains(name);
    public static IEnumerable<Property> All => Longhands.Values;

    /// <summary>Expands a shorthand into longhands; null if the value is invalid.</summary>
    public static List<(string Name, List<CssToken> Value)>? Expand(string name, List<CssToken> value) =>
        Shorthands.TryGetValue(Canonical(name), out var expand) ? expand(value)?.Select(l => (Canonical(l.Item1), l.Item2)).ToList() : null;

    static readonly List<Property> OrderedLonghands = [];
    static readonly List<Property> InheritedLonghands = [];

    /// <summary>Longhands in registration order (the order the cascade computes them in).</summary>
    public static IReadOnlyList<Property> Ordered => OrderedLonghands;
    public static IReadOnlyList<Property> Inherited => InheritedLonghands;

    static void Add(string name, bool inherited, Parser parse, Action<ComputedStyle, ComputedStyle> copy)
    {
        var property = new Property(name, inherited, parse, copy);
        Longhands[name] = property;
        OrderedLonghands.Add(property);
        if (inherited)
            InheritedLonghands.Add(property);
    }

    static readonly Dictionary<string, string> Aliases = new(StringComparer.Ordinal);

    /// <summary>Another name for a property (prefixed or logical forms): the cascade treats them as one.</summary>
    static void Alias(string alias, string target) => Aliases[alias] = target;

    /// <summary>The property a name stands for (itself unless it is an alias).</summary>
    public static string Canonical(string name) => Aliases.TryGetValue(name, out var target) ? target : name;

    static List<CssToken> Single(CssToken token) => [token];

    // ---------- Shared value parsers ----------

    static bool Keyword<T>(ValueReader r, Dictionary<string, T> map, out T value)
    {
        var token = r.Peek();
        if (token.Type == CssTokenType.Ident && map.TryGetValue(token.Value.ToLowerInvariant(), out value!))
        {
            r.Next();
            return r.AtEnd;
        }
        value = default!;
        return false;
    }

    static Parser Enum<T>(Dictionary<string, T> map, Action<ComputedStyle, T> set) =>
        (r, c) =>
        {
            if (!Keyword(r, map, out var value))
                return false;
            set(c.Style, value);
            return true;
        };

    static Parser LengthOf(Action<ComputedStyle, Length> set, bool keywords = true, bool allowNegative = true) =>
        (r, c) =>
        {
            if (!(keywords ? r.TryLengthOrKeyword(c.Units, out var length) : r.TryLength(c.Units, out length)) || !r.AtEnd)
                return false;
            // Negative values are invalid; calc() mixing pixels and percentages is clamped at layout instead.
            if (!allowNegative && length.IsValue && ((length.Percent == 0 && length.Pixels < 0) || (length.Pixels == 0 && length.Percent < 0)))
                return false;
            set(c.Style, length);
            return true;
        };

    static Parser NumberOf(Action<ComputedStyle, float> set, bool allowNegative = true) =>
        (r, c) =>
        {
            if (!r.TryNumber(out var value) || !r.AtEnd || (!allowNegative && value < 0))
                return false;
            set(c.Style, value);
            return true;
        };

    static Parser ColorOf(Action<ComputedStyle, Color?> set) =>
        (r, c) =>
        {
            if (!r.TryColor(c.Style.Color, out var color) || !r.AtEnd)
                return false;
            set(c.Style, color);
            return true;
        };

    static bool BorderWidth(ValueReader r, StyleContext c, out float width)
    {
        if (r.TryIdent("thin"))
            width = 1 * c.Units.Zoom;
        else if (r.TryIdent("medium"))
            width = 3 * c.Units.Zoom;
        else if (r.TryIdent("thick"))
            width = 5 * c.Units.Zoom;
        else if (r.TryLength(c.Units, out var length) && !length.HasPercent)
            width = MathF.Max(0, length.Pixels);
        else
        {
            width = 0;
            return false;
        }
        // Blink's ClampLineWidth (after zooming): under 1 px rounds up, otherwise down to whole device pixels.
        width = width > 0 && width < 1 ? 1 : MathF.Floor(width);
        return true;
    }

    /// <summary>Outline offsets are whole pixels (Blink's ConvertOutlineOffset): under 1 px away from zero, otherwise toward it.</summary>
    internal static float OutlineOffset(float pixels)
    {
        var magnitude = MathF.Abs(pixels);
        if (magnitude is > 0 and < 1)
            return MathF.Sign(pixels);
        return MathF.CopySign(MathF.Floor(magnitude), pixels);
    }

    static readonly Dictionary<string, BorderLineStyle> LineStyles = new()
    {
        ["none"] = BorderLineStyle.None, ["hidden"] = BorderLineStyle.Hidden, ["solid"] = BorderLineStyle.Solid, ["dashed"] = BorderLineStyle.Dashed,
        ["dotted"] = BorderLineStyle.Dotted, ["double"] = BorderLineStyle.Double, ["groove"] = BorderLineStyle.Groove, ["ridge"] = BorderLineStyle.Ridge,
        ["inset"] = BorderLineStyle.Inset, ["outset"] = BorderLineStyle.Outset,
    };

    static readonly Dictionary<string, Alignment> Alignments = new()
    {
        ["auto"] = Alignment.Auto, ["normal"] = Alignment.Normal, ["stretch"] = Alignment.Stretch, ["start"] = Alignment.Start, ["end"] = Alignment.End,
        ["flex-start"] = Alignment.FlexStart, ["flex-end"] = Alignment.FlexEnd, ["center"] = Alignment.Center, ["baseline"] = Alignment.Baseline,
        ["space-between"] = Alignment.SpaceBetween, ["space-around"] = Alignment.SpaceAround, ["space-evenly"] = Alignment.SpaceEvenly,
        ["left"] = Alignment.Left, ["right"] = Alignment.Right, ["self-start"] = Alignment.SelfStart, ["self-end"] = Alignment.SelfEnd,
    };

    static Parser AlignmentOf(Action<ComputedStyle, Alignment> set) =>
        (r, c) =>
        {
            // "safe"/"unsafe" and "first"/"last baseline" prefixes.
            r.TryIdent("safe");
            r.TryIdent("unsafe");
            if (r.TryIdent("first"))
            {
                if (!r.TryIdent("baseline") || !r.AtEnd)
                    return false;
                set(c.Style, Alignment.Baseline);
                return true;
            }
            if (r.TryIdent("last"))
            {
                if (!r.TryIdent("baseline") || !r.AtEnd)
                    return false;
                set(c.Style, Alignment.LastBaseline);
                return true;
            }
            if (!Keyword(r, Alignments, out var value))
                return false;
            set(c.Style, value);
            return true;
        };

    public static TimingFunction? ParseTiming(ValueReader r)
    {
        var token = r.Peek();
        if (token.Type == CssTokenType.Ident)
        {
            TimingFunction? keyword = token.Value.ToLowerInvariant() switch
            {
                "linear" => TimingFunction.Linear,
                "ease" => TimingFunction.Ease,
                "ease-in" => TimingFunction.EaseIn,
                "ease-out" => TimingFunction.EaseOut,
                "ease-in-out" => TimingFunction.EaseInOut,
                "step-start" => new TimingFunction(0, 0, 1, 1, 1, true),
                "step-end" => new TimingFunction(0, 0, 1, 1, 1, false),
                _ => null,
            };
            if (keyword is not null)
                r.Next();
            return keyword;
        }
        if (token.Type == CssTokenType.Function && token.Value == "cubic-bezier")
        {
            var values = r.FunctionArguments().Where(t => t.Type == CssTokenType.Number).Select(t => (float)t.Number).ToList();
            return values.Count == 4 ? new TimingFunction(values[0], values[1], values[2], values[3]) : null;
        }
        if (token.Type == CssTokenType.Function && token.Value == "steps")
        {
            var arguments = r.FunctionArguments();
            var count = arguments.FirstOrDefault(t => t.Type == CssTokenType.Number);
            var jump = arguments.FirstOrDefault(t => t.Type == CssTokenType.Ident).Value?.ToLowerInvariant() ?? "end";
            return count.Type == CssTokenType.Number ? new TimingFunction(0, 0, 1, 1, (int)count.Number, jump is "start" or "jump-start") : null;
        }
        return null;
    }

    static ShadowValue[]? ParseShadows(List<CssToken> tokens, StyleContext c)
    {
        if (tokens.Count == 1 && tokens[0].IsIdent("none"))
            return [];
        var shadows = new List<ShadowValue>();
        foreach (var part in CssParser.SplitOnCommas(tokens))
        {
            var r = new ValueReader(part);
            var lengths = new List<float>();
            Color? color = null;
            var inset = false;
            while (!r.AtEnd)
            {
                if (r.TryIdent("inset"))
                    inset = true;
                else if (r.TryLength(c.Units, out var length))
                    lengths.Add(length.Pixels);
                else if (r.TryColor(c.Style.Color, out var parsed))
                    color = parsed;
                else
                    return null;
            }
            if (lengths.Count is < 2 or > 4)
                return null;
            shadows.Add(new ShadowValue(lengths[0], lengths[1], lengths.Count > 2 ? MathF.Max(0, lengths[2]) : 0, lengths.Count > 3 ? lengths[3] : 0, color ?? c.Style.Color, inset));
        }
        return shadows.ToArray();
    }

    static FilterFunction[]? ParseFilters(List<CssToken> tokens, StyleContext c)
    {
        var r = new ValueReader(tokens);
        if (r.TryIdent("none") && r.AtEnd)
            return [];
        var filters = new List<FilterFunction>();
        while (!r.AtEnd)
        {
            var token = r.Peek();
            if (token.Type != CssTokenType.Function)
                return null;
            var arguments = r.FunctionArguments();
            var a = new ValueReader(arguments);
            float Amount(float fallback)
            {
                if (a.AtEnd)
                    return fallback;
                if (a.TryPercentage(out var p))
                    return p / 100f;
                return a.TryNumber(out var n) ? n : float.NaN;
            }
            FilterFunction? filter = token.Value switch
            {
                "blur" => a.AtEnd ? new FilterFunction(FilterFunctionKind.Blur, 0) : a.TryLength(c.Units, out var blur) ? new FilterFunction(FilterFunctionKind.Blur, blur.Pixels) : null,
                "brightness" => new FilterFunction(FilterFunctionKind.Brightness, Amount(1)),
                "contrast" => new FilterFunction(FilterFunctionKind.Contrast, Amount(1)),
                "saturate" => new FilterFunction(FilterFunctionKind.Saturate, Amount(1)),
                "grayscale" => new FilterFunction(FilterFunctionKind.Grayscale, Amount(1)),
                "sepia" => new FilterFunction(FilterFunctionKind.Sepia, Amount(1)),
                "invert" => new FilterFunction(FilterFunctionKind.Invert, Amount(1)),
                "opacity" => new FilterFunction(FilterFunctionKind.Opacity, Amount(1)),
                "hue-rotate" => a.AtEnd ? new FilterFunction(FilterFunctionKind.HueRotate, 0) : a.TryAngle(out var angle) ? new FilterFunction(FilterFunctionKind.HueRotate, angle) : null,
                "drop-shadow" => DropShadow(arguments, c),
                _ => null,
            };
            if (filter is null || float.IsNaN(filter.Amount))
                return null;
            filters.Add(filter);
        }
        return filters.ToArray();
    }

    static FilterFunction? DropShadow(List<CssToken> arguments, StyleContext c)
    {
        var r = new ValueReader(arguments);
        var lengths = new List<float>();
        Color? color = null;
        while (!r.AtEnd)
        {
            if (r.TryLength(c.Units, out var length))
                lengths.Add(length.Pixels);
            else if (r.TryColor(c.Style.Color, out var parsed))
                color = parsed;
            else
                return null;
        }
        if (lengths.Count is < 2 or > 3)
            return null;
        return new FilterFunction(FilterFunctionKind.DropShadow, lengths.Count > 2 ? lengths[2] : 0, lengths[0], lengths[1], color ?? c.Style.Color);
    }

    static TransformFunction[]? ParseTransform(List<CssToken> tokens, StyleContext c)
    {
        var r = new ValueReader(tokens);
        if (r.TryIdent("none") && r.AtEnd)
            return [];
        var functions = new List<TransformFunction>();
        while (!r.AtEnd)
        {
            var token = r.Peek();
            if (token.Type != CssTokenType.Function)
                return null;
            var parts = CssParser.SplitOnCommas(r.FunctionArguments());
            Length L(int i) => i < parts.Count && new ValueReader(parts[i]).TryLength(c.Units, out var l) ? l : Length.Zero;
            float N(int i, float fallback) => i < parts.Count && new ValueReader(parts[i]) is var reader && (reader.TryNumber(out var n) || (reader.TryPercentage(out n) && (n /= 100) == n)) ? n : fallback;
            float A(int i) => i < parts.Count && new ValueReader(parts[i]).TryAngle(out var a) ? a : 0;
            TransformFunction? f = token.Value switch
            {
                "translate" => new TransformFunction(TransformKind.Translate, L(0), parts.Count > 1 ? L(1) : Length.Zero),
                "translatex" => new TransformFunction(TransformKind.Translate, L(0), Length.Zero),
                "translatey" => new TransformFunction(TransformKind.Translate, Length.Zero, L(0)),
                "translate3d" => new TransformFunction(TransformKind.Translate, L(0), L(1)),
                "scale" => new TransformFunction(TransformKind.Scale, default, default, N(0, 1), parts.Count > 1 ? N(1, 1) : N(0, 1)),
                "scalex" => new TransformFunction(TransformKind.Scale, default, default, N(0, 1), 1),
                "scaley" => new TransformFunction(TransformKind.Scale, default, default, 1, N(0, 1)),
                "scale3d" => new TransformFunction(TransformKind.Scale, default, default, N(0, 1), N(1, 1)),
                "rotate" or "rotatez" => new TransformFunction(TransformKind.Rotate, default, default, A(0)),
                "skew" => new TransformFunction(TransformKind.Skew, default, default, A(0), parts.Count > 1 ? A(1) : 0),
                "skewx" => new TransformFunction(TransformKind.Skew, default, default, A(0), 0),
                "skewy" => new TransformFunction(TransformKind.Skew, default, default, 0, A(0)),
                // The translation is px (TransformBuilder zooms it).
                "matrix" when parts.Count == 6 => new TransformFunction(TransformKind.Matrix, default, default, Matrix: [N(0, 1), N(1, 0), N(2, 0), N(3, 1), N(4, 0) * c.Units.Zoom, N(5, 0) * c.Units.Zoom]),
                // 3D functions that are 2D no-ops for this engine (rotateX/Y, perspective).
                "perspective" or "rotatex" or "rotatey" or "translatez" => new TransformFunction(TransformKind.Translate, Length.Zero, Length.Zero),
                _ => null,
            };
            if (f is null)
                return null;
            functions.Add(f);
        }
        return functions.ToArray();
    }

    static bool ParsePosition(ValueReader r, StyleContext c, out Length x, out Length y)
    {
        // One or two values: keywords (left/center/right, top/center/bottom) or lengths.
        var values = new List<(Length Length, string? Keyword)>();
        while (!r.AtEnd && values.Count < 4)
        {
            var keyword = r.TryAnyIdent();
            if (keyword is not null)
                values.Add((default, keyword));
            else if (r.TryLength(c.Units, out var length))
                values.Add((length, null));
            else
                break;
        }
        x = Length.Pct(50);
        y = Length.Pct(50);
        if (values.Count == 0)
            return false;
        Length FromKeyword(string k) => k switch { "left" or "top" => Length.Pct(0), "right" or "bottom" => Length.Pct(100), _ => Length.Pct(50) };
        if (values.Count == 1)
        {
            var (l, k) = values[0];
            if (k is "top" or "bottom")
                y = FromKeyword(k);
            else
                x = k is null ? l : FromKeyword(k);
            return true;
        }
        if (values.Count >= 3)
        {
            // Keywords with offsets: "right 10px bottom 20px", "top 5px left 5px", "left 10px top", "center bottom 4px".
            Length? px = null, py = null;
            for (var i = 0; i < values.Count; i++)
            {
                var keyword = values[i].Keyword;
                if (keyword is null)
                    return false;
                Length? offset = i + 1 < values.Count && values[i + 1].Keyword is null ? values[i + 1].Length : null;
                if (offset is not null)
                    i++;
                var at = offset ?? Length.Zero;
                switch (keyword)
                {
                    case "left": px = at; break;
                    case "right": px = Length.Pct(100) - at; break;
                    case "top": py = at; break;
                    case "bottom": py = Length.Pct(100) - at; break;
                    case "center":
                        if (px is null && (i + 1 >= values.Count || values[i + 1].Keyword is "top" or "bottom"))
                            px = Length.Pct(50);
                        else
                            py = Length.Pct(50);
                        break;
                    default: return false;
                }
            }
            x = px ?? Length.Pct(50);
            y = py ?? Length.Pct(50);
            return true;
        }
        var (l0, k0) = values[0];
        var (l1, k1) = values[1];
        if (k0 is "top" or "bottom" || k1 is "left" or "right")
        {
            (l0, k0, l1, k1) = (l1, k1, l0, k0);
        }
        x = k0 is null ? l0 : FromKeyword(k0);
        y = k1 is null ? l1 : FromKeyword(k1);
        return true;
    }

    static readonly HashSet<string> CursorKeywords =
    [
        "auto", "default", "none", "context-menu", "help", "pointer", "progress", "wait", "cell", "crosshair", "text", "vertical-text",
        "alias", "copy", "move", "no-drop", "not-allowed", "e-resize", "n-resize", "ne-resize", "nw-resize", "s-resize", "se-resize",
        "sw-resize", "w-resize", "ew-resize", "ns-resize", "nesw-resize", "nwse-resize", "col-resize", "row-resize", "all-scroll",
        "zoom-in", "zoom-out", "grab", "grabbing",
    ];

    static ImageValue? ParseImage(ValueReader r, StyleContext c)
    {
        var token = r.Peek();
        if (token.IsIdent("none"))
        {
            r.Next();
            return ImageValue.None;
        }
        if (token.Type == CssTokenType.Url)
        {
            r.Next();
            return new ImageValue { Kind = ImageKind.Url, Url = token.Value };
        }
        if (token.Type != CssTokenType.Function)
            return null;
        if (token.Value == "url")
        {
            var url = r.FunctionArguments().FirstOrDefault(t => t.Type == CssTokenType.String).Value ?? "";
            return new ImageValue { Kind = ImageKind.Url, Url = url };
        }
        var repeating = token.Value.StartsWith("repeating-");
        var kind = token.Value.Replace("repeating-", "").Replace("-webkit-", "");
        if (kind is not ("linear-gradient" or "radial-gradient"))
            return null;
        var parts = CssParser.SplitOnCommas(r.FunctionArguments());
        if (parts.Count == 0)
            return null;
        var image = new ImageValue { Kind = kind == "linear-gradient" ? ImageKind.LinearGradient : ImageKind.RadialGradient, Repeating = repeating };
        var first = 0;
        if (image.Kind == ImageKind.LinearGradient)
        {
            var p = new ValueReader(parts[0]);
            if (p.TryAngle(out var angle) && p.AtEnd)
            {
                image = image with { Angle = angle };
                first = 1;
            }
            else if (p.TryIdent("to"))
            {
                string? h = null, v = null;
                while (p.TryAnyIdent() is { } side)
                {
                    if (side is "left" or "right")
                        h = side;
                    else if (side is "top" or "bottom")
                        v = side;
                }
                if (h is not null && v is not null)
                    image = image with { UsesCorner = true, CornerRight = h == "right", CornerBottom = v == "bottom" };
                else
                    image = image with { Angle = (h, v) switch { ("right", _) => 90, ("left", _) => 270, (_, "top") => 0, _ => 180 } };
                first = 1;
            }
        }
        else
        {
            var p = new ValueReader(parts[0]);
            var consumed = false;
            var lengths = new List<Length>();
            while (!p.AtEnd)
            {
                var save = p.Position;
                if (p.TryIdent("circle"))
                    image = image with { Circle = true };
                else if (p.TryIdent("ellipse"))
                    image = image with { Circle = false };
                else if (p.Peek().Type == CssTokenType.Ident && p.Peek().Value.ToLowerInvariant() is "closest-side" or "closest-corner" or "farthest-side" or "farthest-corner")
                    image = image with { SizeKeyword = p.Next().Value.ToLowerInvariant() };
                else if (p.TryIdent("at"))
                {
                    if (!ParsePosition(p, c, out var cx, out var cy))
                        return null;
                    image = image with { CenterX = cx, CenterY = cy };
                }
                else if (p.TryLength(c.Units, out var length))
                    lengths.Add(length);
                else
                {
                    p.Position = save;
                    break;
                }
                consumed = true;
            }
            if (consumed && p.AtEnd)
            {
                first = 1;
                if (lengths.Count == 1)
                    image = image with { Circle = true, SizeKeyword = "", RadiusX = lengths[0], RadiusY = lengths[0] };
                else if (lengths.Count == 2)
                    image = image with { SizeKeyword = "", RadiusX = lengths[0], RadiusY = lengths[1] };
            }
        }
        var stops = new List<ColorStop>();
        for (var i = first; i < parts.Count; i++)
        {
            var p = new ValueReader(parts[i]);
            Color? color = null;
            var positions = new List<Length>();
            while (!p.AtEnd)
            {
                if (p.TryColor(c.Style.Color, out var parsed))
                    color = parsed;
                else if (p.TryLength(c.Units, out var position))
                    positions.Add(position);
                else
                    return null;
            }
            if (color is null)
            {
                // A colour hint: approximated as a stop of the mid colour.
                if (positions.Count == 1 && stops.Count > 0)
                {
                    stops.Add(new ColorStop(stops[^1].Color, positions[0]) with { Color = stops[^1].Color });
                    continue;
                }
                return null;
            }
            if (positions.Count == 0)
                stops.Add(new ColorStop(color.Value, null));
            foreach (var position in positions)
                stops.Add(new ColorStop(color.Value, position));
        }
        return stops.Count == 0 ? null : image with { Stops = stops.ToArray() };
    }

    static TrackSize? ParseTrack(ValueReader r, StyleContext c)
    {
        var token = r.Peek();
        if (token.Type == CssTokenType.Dimension && token.Unit == "fr")
        {
            r.Next();
            return new TrackSize(TrackKind.Fraction, Fraction: (float)token.Number);
        }
        if (token.Type == CssTokenType.Ident)
        {
            TrackSize? keyword = token.Value.ToLowerInvariant() switch
            {
                "auto" => TrackSize.AutoTrack,
                "min-content" => new TrackSize(TrackKind.MinContent),
                "max-content" => new TrackSize(TrackKind.MaxContent),
                _ => null,
            };
            if (keyword is not null)
                r.Next();
            return keyword;
        }
        if (token.Type == CssTokenType.Function && token.Value == "minmax")
        {
            var parts = CssParser.SplitOnCommas(r.FunctionArguments());
            if (parts.Count != 2)
                return null;
            var min = ParseTrack(new ValueReader(parts[0]), c);
            var max = ParseTrack(new ValueReader(parts[1]), c);
            return min is null || max is null ? null : new TrackSize(TrackKind.MinMax, Min: min, Max: max);
        }
        if (token.Type == CssTokenType.Function && token.Value == "fit-content")
        {
            var arguments = r.FunctionArguments();
            return new ValueReader(arguments).TryLength(c.Units, out var limit) ? new TrackSize(TrackKind.FitContent, limit) : null;
        }
        return r.TryLength(c.Units, out var length) ? new TrackSize(TrackKind.Length, length) : null;
    }

    static GridTemplate? ParseGridTemplate(List<CssToken> tokens, StyleContext c)
    {
        var r = new ValueReader(tokens);
        if (r.TryIdent("none") && r.AtEnd)
            return GridTemplate.None;
        var tracks = new List<TrackSize>();
        var names = new List<string[]>();
        var pendingNames = new List<string>();
        TrackSize[]? autoRepeat = null;
        var autoFit = false;
        var repeatIndex = 0;
        while (!r.AtEnd)
        {
            var token = r.Peek();
            if (token.Type == CssTokenType.LeftBracket)
            {
                r.Next();
                while (r.Peek().Type == CssTokenType.Ident)
                    pendingNames.Add(r.Next().Value);
                if (r.Next().Type != CssTokenType.RightBracket)
                    return null;
                continue;
            }
            if (token.Type == CssTokenType.Function && token.Value == "repeat")
            {
                var parts = CssParser.SplitOnCommas(r.FunctionArguments());
                if (parts.Count != 2)
                    return null;
                var countReader = new ValueReader(parts[0]);
                var inner = new List<TrackSize>();
                var innerReader = new ValueReader(parts[1]);
                while (!innerReader.AtEnd)
                {
                    if (innerReader.Peek().Type == CssTokenType.LeftBracket)
                    {
                        while (!innerReader.AtEnd && innerReader.Next().Type != CssTokenType.RightBracket) { }
                        continue;
                    }
                    var track = ParseTrack(innerReader, c);
                    if (track is null)
                        return null;
                    inner.Add(track);
                }
                if (countReader.TryIdent("auto-fill") || (autoFit = countReader.TryIdent("auto-fit")))
                {
                    autoRepeat = inner.ToArray();
                    repeatIndex = tracks.Count;
                    names.Add(pendingNames.ToArray());
                    pendingNames.Clear();
                    continue;
                }
                if (!countReader.TryNumber(out var count))
                    return null;
                for (var i = 0; i < (int)count; i++)
                    foreach (var track in inner)
                    {
                        names.Add(pendingNames.ToArray());
                        pendingNames.Clear();
                        tracks.Add(track);
                    }
                continue;
            }
            var size = ParseTrack(r, c);
            if (size is null)
                return null;
            names.Add(pendingNames.ToArray());
            pendingNames.Clear();
            tracks.Add(size);
        }
        names.Add(pendingNames.ToArray());
        return new GridTemplate(tracks.ToArray(), names.ToArray(), autoRepeat, autoFit, repeatIndex);
    }

    static GridLine? ParseGridLine(List<CssToken> tokens)
    {
        var r = new ValueReader(tokens);
        if (r.TryIdent("auto") && r.AtEnd)
            return GridLine.Auto;
        var span = false;
        int? number = null;
        string? name = null;
        while (!r.AtEnd)
        {
            if (r.TryIdent("span"))
                span = true;
            else if (r.TryNumber(out var n))
                number = (int)n;
            else if (r.TryAnyIdent() is { } ident)
                name = ident;
            else
                return null;
        }
        if (span)
            return new GridLine(0, number ?? 1, name);
        return new GridLine(number ?? 0, 0, number is null ? name : null);
    }

    static Parser ShadowsOf(Action<ComputedStyle, ShadowValue[]> set) => (r, c) =>
    {
        var shadows = ParseShadows(r.Remaining(), c);
        if (shadows is null)
            return false;
        set(c.Style, shadows);
        return true;
    };

    // ---------- Registry ----------

    static Properties()
    {
        // Box and positioning.
        Add("display", false, Enum(new Dictionary<string, Display>
        {
            ["none"] = Display.None, ["block"] = Display.Block, ["inline"] = Display.Inline, ["inline-block"] = Display.InlineBlock,
            ["flex"] = Display.Flex, ["inline-flex"] = Display.InlineFlex, ["grid"] = Display.Grid, ["inline-grid"] = Display.InlineGrid,
            ["contents"] = Display.Contents, ["list-item"] = Display.ListItem, ["-webkit-box"] = Display.WebkitBox, ["flow-root"] = Display.Block,
            ["table"] = Display.Block, ["table-row"] = Display.Block, ["table-cell"] = Display.InlineBlock,
        }, (s, v) => s.Display = v), (t, s) => t.Display = s.Display);
        Add("position", false, Enum(new Dictionary<string, PositionKind>
        {
            ["static"] = PositionKind.Static, ["relative"] = PositionKind.Relative, ["absolute"] = PositionKind.Absolute,
            ["fixed"] = PositionKind.Fixed, ["sticky"] = PositionKind.Sticky, ["-webkit-sticky"] = PositionKind.Sticky,
        }, (s, v) => s.Position = v), (t, s) => t.Position = s.Position);
        Add("box-sizing", false, Enum(new Dictionary<string, BoxSizing> { ["content-box"] = BoxSizing.ContentBox, ["border-box"] = BoxSizing.BorderBox }, (s, v) => s.BoxSizing = v), (t, s) => t.BoxSizing = s.BoxSizing);
        Add("width", false, LengthOf((s, v) => s.Width = v, allowNegative: false), (t, s) => t.Width = s.Width);
        Add("height", false, LengthOf((s, v) => s.Height = v, allowNegative: false), (t, s) => t.Height = s.Height);
        Add("min-width", false, LengthOf((s, v) => s.MinWidth = v, allowNegative: false), (t, s) => t.MinWidth = s.MinWidth);
        Add("min-height", false, LengthOf((s, v) => s.MinHeight = v, allowNegative: false), (t, s) => t.MinHeight = s.MinHeight);
        Add("max-width", false, LengthOf((s, v) => s.MaxWidth = v, allowNegative: false), (t, s) => t.MaxWidth = s.MaxWidth);
        Add("max-height", false, LengthOf((s, v) => s.MaxHeight = v, allowNegative: false), (t, s) => t.MaxHeight = s.MaxHeight);
        Add("margin-top", false, LengthOf((s, v) => s.MarginTop = v), (t, s) => t.MarginTop = s.MarginTop);
        Add("margin-right", false, LengthOf((s, v) => s.MarginRight = v), (t, s) => t.MarginRight = s.MarginRight);
        Add("margin-bottom", false, LengthOf((s, v) => s.MarginBottom = v), (t, s) => t.MarginBottom = s.MarginBottom);
        Add("margin-left", false, LengthOf((s, v) => s.MarginLeft = v), (t, s) => t.MarginLeft = s.MarginLeft);
        Add("padding-top", false, LengthOf((s, v) => s.PaddingTop = v, keywords: false, allowNegative: false), (t, s) => t.PaddingTop = s.PaddingTop);
        Add("padding-right", false, LengthOf((s, v) => s.PaddingRight = v, keywords: false, allowNegative: false), (t, s) => t.PaddingRight = s.PaddingRight);
        Add("padding-bottom", false, LengthOf((s, v) => s.PaddingBottom = v, keywords: false, allowNegative: false), (t, s) => t.PaddingBottom = s.PaddingBottom);
        Add("padding-left", false, LengthOf((s, v) => s.PaddingLeft = v, keywords: false, allowNegative: false), (t, s) => t.PaddingLeft = s.PaddingLeft);
        Add("top", false, LengthOf((s, v) => s.Top = v), (t, s) => t.Top = s.Top);
        Add("right", false, LengthOf((s, v) => s.Right = v), (t, s) => t.Right = s.Right);
        Add("bottom", false, LengthOf((s, v) => s.Bottom = v), (t, s) => t.Bottom = s.Bottom);
        Add("left", false, LengthOf((s, v) => s.Left = v), (t, s) => t.Left = s.Left);
        Add("aspect-ratio", false, (r, c) =>
        {
            if (r.TryIdent("auto"))
            {
                c.Style.AspectRatio = 0;
                return r.AtEnd || (r.TryNumber(out _) && r.AtEnd);
            }
            if (!r.TryNumber(out var w))
                return false;
            var h = 1f;
            if (r.TryDelim('/') && !r.TryNumber(out h))
                return false;
            c.Style.AspectRatio = h > 0 ? w / h : 0;
            return r.AtEnd;
        }, (t, s) => t.AspectRatio = s.AspectRatio);
        Add("z-index", false, (r, c) =>
        {
            if (r.TryIdent("auto"))
            {
                c.Style.ZIndex = null;
                return r.AtEnd;
            }
            if (!r.TryNumber(out var z))
                return false;
            c.Style.ZIndex = (int)z;
            return r.AtEnd;
        }, (t, s) => t.ZIndex = s.ZIndex);
        var overflows = new Dictionary<string, Overflow> { ["visible"] = Overflow.Visible, ["hidden"] = Overflow.Hidden, ["clip"] = Overflow.Clip, ["scroll"] = Overflow.Scroll, ["auto"] = Overflow.Auto, ["overlay"] = Overflow.Auto };
        Add("overflow-x", false, Enum(overflows, (s, v) => s.OverflowX = v), (t, s) => t.OverflowX = s.OverflowX);
        Add("overflow-y", false, Enum(overflows, (s, v) => s.OverflowY = v), (t, s) => t.OverflowY = s.OverflowY);

        // Borders.
        foreach (var (side, setWidth, setStyle, setColor, copy) in new (string, Action<ComputedStyle, float>, Action<ComputedStyle, BorderLineStyle>, Action<ComputedStyle, Color?>, Action<ComputedStyle, ComputedStyle>)[]
        {
            ("top", (s, v) => s.BorderTopWidth = v, (s, v) => s.BorderTopStyle = v, (s, v) => s.BorderTopColor = v, (t, s) => { t.BorderTopWidth = s.BorderTopWidth; }),
            ("right", (s, v) => s.BorderRightWidth = v, (s, v) => s.BorderRightStyle = v, (s, v) => s.BorderRightColor = v, (t, s) => { t.BorderRightWidth = s.BorderRightWidth; }),
            ("bottom", (s, v) => s.BorderBottomWidth = v, (s, v) => s.BorderBottomStyle = v, (s, v) => s.BorderBottomColor = v, (t, s) => { t.BorderBottomWidth = s.BorderBottomWidth; }),
            ("left", (s, v) => s.BorderLeftWidth = v, (s, v) => s.BorderLeftStyle = v, (s, v) => s.BorderLeftColor = v, (t, s) => { t.BorderLeftWidth = s.BorderLeftWidth; }),
        })
        {
            Add($"border-{side}-width", false, (r, c) => BorderWidth(r, c, out var w) && r.AtEnd && Set(() => setWidth(c.Style, w)), copy);
            Add($"border-{side}-style", false, Enum(LineStyles, setStyle), side switch
            {
                "top" => (t, s) => t.BorderTopStyle = s.BorderTopStyle,
                "right" => (t, s) => t.BorderRightStyle = s.BorderRightStyle,
                "bottom" => (t, s) => t.BorderBottomStyle = s.BorderBottomStyle,
                _ => (t, s) => t.BorderLeftStyle = s.BorderLeftStyle,
            });
            Add($"border-{side}-color", false, ColorOf(setColor), side switch
            {
                "top" => (t, s) => t.BorderTopColor = s.BorderTopColor,
                "right" => (t, s) => t.BorderRightColor = s.BorderRightColor,
                "bottom" => (t, s) => t.BorderBottomColor = s.BorderBottomColor,
                _ => (t, s) => t.BorderLeftColor = s.BorderLeftColor,
            });
        }
        foreach (var (corner, set, copy) in new (string, Action<ComputedStyle, Corner>, Action<ComputedStyle, ComputedStyle>)[]
        {
            ("top-left", (s, v) => s.RadiusTopLeft = v, (t, s) => t.RadiusTopLeft = s.RadiusTopLeft),
            ("top-right", (s, v) => s.RadiusTopRight = v, (t, s) => t.RadiusTopRight = s.RadiusTopRight),
            ("bottom-right", (s, v) => s.RadiusBottomRight = v, (t, s) => t.RadiusBottomRight = s.RadiusBottomRight),
            ("bottom-left", (s, v) => s.RadiusBottomLeft = v, (t, s) => t.RadiusBottomLeft = s.RadiusBottomLeft),
        })
            Add($"border-{corner}-radius", false, (r, c) =>
            {
                if (!r.TryLength(c.Units, out var x))
                    return false;
                var y = x;
                if (!r.AtEnd && !r.TryLength(c.Units, out y))
                    return false;
                set(c.Style, new Corner(x, y));
                return r.AtEnd;
            }, copy);

        // Flex and grid.
        Add("flex-direction", false, Enum(new Dictionary<string, FlexDirection> { ["row"] = FlexDirection.Row, ["row-reverse"] = FlexDirection.RowReverse, ["column"] = FlexDirection.Column, ["column-reverse"] = FlexDirection.ColumnReverse }, (s, v) => s.FlexDirection = v), (t, s) => t.FlexDirection = s.FlexDirection);
        Add("flex-wrap", false, Enum(new Dictionary<string, FlexWrap> { ["nowrap"] = FlexWrap.NoWrap, ["wrap"] = FlexWrap.Wrap, ["wrap-reverse"] = FlexWrap.WrapReverse }, (s, v) => s.FlexWrap = v), (t, s) => t.FlexWrap = s.FlexWrap);
        Add("flex-grow", false, NumberOf((s, v) => s.FlexGrow = v, false), (t, s) => t.FlexGrow = s.FlexGrow);
        Add("flex-shrink", false, NumberOf((s, v) => s.FlexShrink = v, false), (t, s) => t.FlexShrink = s.FlexShrink);
        Add("flex-basis", false, (r, c) =>
        {
            if (r.TryIdent("content"))
            {
                c.Style.FlexBasis = Length.MaxContent;
                return r.AtEnd;
            }
            return LengthOf((s, v) => s.FlexBasis = v, allowNegative: false)(r, c);
        }, (t, s) => t.FlexBasis = s.FlexBasis);
        Add("justify-content", false, AlignmentOf((s, v) => s.JustifyContent = v), (t, s) => t.JustifyContent = s.JustifyContent);
        Add("align-items", false, AlignmentOf((s, v) => s.AlignItems = v), (t, s) => t.AlignItems = s.AlignItems);
        Add("align-self", false, AlignmentOf((s, v) => s.AlignSelf = v), (t, s) => t.AlignSelf = s.AlignSelf);
        Add("align-content", false, AlignmentOf((s, v) => s.AlignContent = v), (t, s) => t.AlignContent = s.AlignContent);
        Add("justify-items", false, AlignmentOf((s, v) => s.JustifyItems = v), (t, s) => t.JustifyItems = s.JustifyItems);
        Add("justify-self", false, AlignmentOf((s, v) => s.JustifySelf = v), (t, s) => t.JustifySelf = s.JustifySelf);
        Add("order", false, NumberOf((s, v) => s.Order = (int)v), (t, s) => t.Order = s.Order);
        Add("row-gap", false, LengthOf((s, v) => s.RowGap = v), (t, s) => t.RowGap = s.RowGap);
        Add("column-gap", false, LengthOf((s, v) => s.ColumnGap = v), (t, s) => t.ColumnGap = s.ColumnGap);
        Add("grid-template-columns", false, (r, c) => ParseGridTemplate(r.Remaining(), c) is { } g && Set(() => c.Style.GridTemplateColumns = g), (t, s) => t.GridTemplateColumns = s.GridTemplateColumns);
        Add("grid-template-rows", false, (r, c) => ParseGridTemplate(r.Remaining(), c) is { } g && Set(() => c.Style.GridTemplateRows = g), (t, s) => t.GridTemplateRows = s.GridTemplateRows);
        Add("grid-template-areas", false, (r, c) =>
        {
            if (r.TryIdent("none"))
            {
                c.Style.GridTemplateAreas = null;
                return r.AtEnd;
            }
            var rows = new List<string>();
            while (r.Peek().Type == CssTokenType.String)
                rows.Add(r.Next().Value);
            c.Style.GridTemplateAreas = rows.ToArray();
            return r.AtEnd && rows.Count > 0;
        }, (t, s) => t.GridTemplateAreas = s.GridTemplateAreas);
        Add("grid-auto-flow", false, (r, c) =>
        {
            var row = true;
            var dense = false;
            while (r.TryAnyIdent() is { } k)
            {
                if (k == "column")
                    row = false;
                else if (k == "dense")
                    dense = true;
                else if (k != "row")
                    return false;
            }
            c.Style.GridAutoFlow = row ? (dense ? GridAutoFlow.RowDense : GridAutoFlow.Row) : (dense ? GridAutoFlow.ColumnDense : GridAutoFlow.Column);
            return r.AtEnd;
        }, (t, s) => t.GridAutoFlow = s.GridAutoFlow);
        Add("grid-auto-columns", false, (r, c) => ParseGridTemplate(r.Remaining(), c) is { Tracks.Length: > 0 } g && Set(() => c.Style.GridAutoColumns = g.Tracks), (t, s) => t.GridAutoColumns = s.GridAutoColumns);
        Add("grid-auto-rows", false, (r, c) => ParseGridTemplate(r.Remaining(), c) is { Tracks.Length: > 0 } g && Set(() => c.Style.GridAutoRows = g.Tracks), (t, s) => t.GridAutoRows = s.GridAutoRows);
        Add("grid-column-start", false, (r, c) => ParseGridLine(r.Remaining()) is { } l && Set(() => c.Style.GridColumnStart = l), (t, s) => t.GridColumnStart = s.GridColumnStart);
        Add("grid-column-end", false, (r, c) => ParseGridLine(r.Remaining()) is { } l && Set(() => c.Style.GridColumnEnd = l), (t, s) => t.GridColumnEnd = s.GridColumnEnd);
        Add("grid-row-start", false, (r, c) => ParseGridLine(r.Remaining()) is { } l && Set(() => c.Style.GridRowStart = l), (t, s) => t.GridRowStart = s.GridRowStart);
        Add("grid-row-end", false, (r, c) => ParseGridLine(r.Remaining()) is { } l && Set(() => c.Style.GridRowEnd = l), (t, s) => t.GridRowEnd = s.GridRowEnd);

        // Text (inherited).
        Add("color", true, (r, c) => r.TryColor(c.Parent.Color, out var color) && r.AtEnd && Set(() => c.Style.Color = color), (t, s) => t.Color = s.Color);
        Add("font-family", true, (r, c) =>
        {
            var families = new List<string>();
            var current = new List<string>();
            while (!r.AtEnd)
            {
                var token = r.Next();
                if (token.Type == CssTokenType.Comma)
                {
                    if (current.Count > 0)
                        families.Add(string.Join(" ", current));
                    current.Clear();
                }
                else if (token.Type is CssTokenType.String or CssTokenType.Ident)
                    current.Add(token.Value);
                else
                    return false;
            }
            if (current.Count > 0)
                families.Add(string.Join(" ", current));
            if (families.Count == 0)
                return false;
            c.Style.FontFamilies = families.ToArray();
            return true;
        }, (t, s) => t.FontFamilies = s.FontFamilies);
        Add("font-size", true, (r, c) =>
        {
            // Blink resolves the specified size in CSS px (FontSizeConversionData: unzoomed, em and %
            // of the parent's specified size) and zooms it after (GetComputedSizeFromSpecifiedSize).
            var parentSize = c.Parent.SpecifiedFontSize;
            var keyword = r.TryAnyIdent();
            float? size = keyword switch
            {
                "xx-small" => 9, "x-small" => 10, "small" => 13, "medium" => 16,
                "large" => 18, "x-large" => 24, "xx-large" => 32, "xxx-large" => 48,
                "smaller" => parentSize / 1.2f, "larger" => parentSize * 1.2f,
                _ => null,
            };
            if (keyword is not null && size is null)
                return false;
            if (size is null)
            {
                var units = c.Units with { FontSize = parentSize, Zoom = 1 };
                if (!r.TryLength(units, out var length) || length.IsValue && length.Pixels < 0)
                    return false;
                size = length.Resolve(parentSize);
            }
            c.Style.SpecifiedFontSize = size.Value;
            c.Style.FontSize = size.Value * c.Units.Zoom;
            return r.AtEnd;
        }, (t, s) =>
        {
            t.FontSize = s.FontSize;
            t.SpecifiedFontSize = s.SpecifiedFontSize;
        });
        Add("font-weight", true, (r, c) =>
        {
            var parent = c.Parent.FontWeight;
            int? weight = null;
            if (r.TryIdent("normal"))
                weight = 400;
            else if (r.TryIdent("bold"))
                weight = 700;
            else if (r.TryIdent("bolder"))
                weight = parent < 350 ? 400 : parent < 550 ? 700 : 900;
            else if (r.TryIdent("lighter"))
                weight = parent < 550 ? 100 : parent < 750 ? 400 : 700;
            else if (r.TryNumber(out var n))
                weight = (int)n;
            if (weight is null)
                return false;
            c.Style.FontWeight = Math.Clamp(weight.Value, 1, 1000);
            return r.AtEnd;
        }, (t, s) => t.FontWeight = s.FontWeight);
        Add("font-style", true, (r, c) =>
        {
            if (r.TryIdent("normal"))
                c.Style.FontItalic = false;
            else if (r.TryIdent("italic") || r.TryIdent("oblique"))
            {
                c.Style.FontItalic = true;
                r.TryAngle(out _);
            }
            else
                return false;
            return r.AtEnd;
        }, (t, s) => t.FontItalic = s.FontItalic);
        Add("line-height", true, (r, c) =>
        {
            if (r.TryIdent("normal"))
                c.Style.LineHeight = LineHeight.Normal;
            else if (r.TryNumber(out var n))
                c.Style.LineHeight = new LineHeight(LineHeightKind.Number, n);
            else if (r.TryLength(c.Units, out var length))
                c.Style.LineHeight = new LineHeight(LineHeightKind.Length, length.Resolve(c.Style.FontSize));
            else
                return false;
            return r.AtEnd;
        }, (t, s) => t.LineHeight = s.LineHeight);
        Add("letter-spacing", true, (r, c) =>
        {
            if (r.TryIdent("normal"))
                c.Style.LetterSpacing = 0;
            else if (r.TryLength(c.Units, out var length) && !length.HasPercent)
                c.Style.LetterSpacing = length.Pixels;
            else
                return false;
            return r.AtEnd;
        }, (t, s) => t.LetterSpacing = s.LetterSpacing);
        Add("word-spacing", true, (r, c) =>
        {
            if (r.TryIdent("normal"))
                c.Style.WordSpacing = 0;
            else if (r.TryLength(c.Units, out var length))
                c.Style.WordSpacing = length.Pixels;
            else
                return false;
            return r.AtEnd;
        }, (t, s) => t.WordSpacing = s.WordSpacing);
        Add("text-align", true, Enum(new Dictionary<string, TextAlignment>
        {
            ["start"] = TextAlignment.Start, ["left"] = TextAlignment.Left, ["center"] = TextAlignment.Center, ["right"] = TextAlignment.Right,
            ["justify"] = TextAlignment.Justify, ["end"] = TextAlignment.End, ["-webkit-center"] = TextAlignment.Center,
        }, (s, v) => s.TextAlign = v), (t, s) => t.TextAlign = s.TextAlign);
        Add("text-transform", true, Enum(new Dictionary<string, TextNs.TextTransform>
        {
            ["none"] = TextNs.TextTransform.None, ["uppercase"] = TextNs.TextTransform.Uppercase, ["lowercase"] = TextNs.TextTransform.Lowercase, ["capitalize"] = TextNs.TextTransform.Capitalize,
        }, (s, v) => s.TextTransform = v), (t, s) => t.TextTransform = s.TextTransform);
        Add("white-space", true, Enum(new Dictionary<string, TextNs.WhiteSpace>
        {
            ["normal"] = TextNs.WhiteSpace.Normal, ["nowrap"] = TextNs.WhiteSpace.NoWrap, ["pre"] = TextNs.WhiteSpace.Pre, ["pre-wrap"] = TextNs.WhiteSpace.PreWrap,
            ["pre-line"] = TextNs.WhiteSpace.PreLine, ["break-spaces"] = TextNs.WhiteSpace.PreWrap,
        }, (s, v) => s.WhiteSpace = v), (t, s) => t.WhiteSpace = s.WhiteSpace);
        Add("overflow-wrap", true, Enum(new Dictionary<string, TextNs.OverflowWrap>
        {
            ["normal"] = TextNs.OverflowWrap.Normal, ["anywhere"] = TextNs.OverflowWrap.Anywhere, ["break-word"] = TextNs.OverflowWrap.Anywhere,
        }, (s, v) => s.OverflowWrap = v), (t, s) => t.OverflowWrap = s.OverflowWrap);
        Alias("word-wrap", "overflow-wrap");
        Add("word-break", true, (r, c) =>
        {
            if (r.TryIdent("break-all") || r.TryIdent("break-word"))
                c.Style.OverflowWrap = TextNs.OverflowWrap.Anywhere;
            else if (!r.TryIdent("normal") && !r.TryIdent("keep-all"))
                return false;
            return r.AtEnd;
        }, (t, s) => { });
        Add("text-shadow", true, ShadowsOf((s, v) => s.TextShadows = v), (t, s) => t.TextShadows = s.TextShadows);
        Add("visibility", true, Enum(new Dictionary<string, Visibility> { ["visible"] = Visibility.Visible, ["hidden"] = Visibility.Hidden, ["collapse"] = Visibility.Collapse }, (s, v) => s.Visibility = v), (t, s) => t.Visibility = s.Visibility);
        Add("pointer-events", true, (r, c) =>
        {
            if (r.TryIdent("none"))
                c.Style.PointerEvents = PointerEvents.None;
            else if (r.TryAnyIdent() is not null)
                c.Style.PointerEvents = PointerEvents.Auto;
            else
                return false;
            return r.AtEnd;
        }, (t, s) => t.PointerEvents = s.PointerEvents);
        // [ <url> [<x> <y>]? , ]* <keyword> (Blink's ConsumeCursor; hotspots are whole numbers).
        Add("cursor", true, (r, c) =>
        {
            var images = new List<CursorImage>();
            while (r.Peek() is { Type: CssTokenType.Url } or { Type: CssTokenType.Function, Value: "url" })
            {
                var url = r.Peek().Type == CssTokenType.Url ? r.Next().Value : r.FunctionArguments().FirstOrDefault(t => t.Type == CssTokenType.String).Value ?? "";
                System.Numerics.Vector2? hotspot = null;
                if (r.TryNumber(out var x))
                {
                    if (!r.TryNumber(out var y))
                        return false;
                    hotspot = new System.Numerics.Vector2((int)x, (int)y);
                }
                images.Add(new CursorImage(url, hotspot));
                if (!r.TryComma())
                    return false;
            }
            if (r.TryAnyIdent() is not { } keyword || !r.AtEnd)
                return false;
            // The -webkit- names are aliases.
            keyword = keyword switch
            {
                "-webkit-zoom-in" => "zoom-in",
                "-webkit-zoom-out" => "zoom-out",
                "-webkit-grab" => "grab",
                "-webkit-grabbing" => "grabbing",
                _ => keyword,
            };
            if (!CursorKeywords.Contains(keyword))
                return false;
            c.Style.Cursor = keyword;
            c.Style.CursorImages = images.ToArray();
            return true;
        }, (t, s) =>
        {
            t.Cursor = s.Cursor;
            t.CursorImages = s.CursorImages;
        });
        Add("user-select", true, Enum(new Dictionary<string, UserSelect> { ["auto"] = UserSelect.Auto, ["none"] = UserSelect.None, ["text"] = UserSelect.Text, ["all"] = UserSelect.All }, (s, v) => s.UserSelect = v), (t, s) => t.UserSelect = s.UserSelect);
        Alias("-webkit-user-select", "user-select");
        // Blink's ApplyInertness: inert sets IsCSSInert, which the subtree keeps (the copy only ever adds it).
        Add("interactivity", true, (r, c) =>
        {
            if (r.TryIdent("inert"))
            {
                c.Style.Interactivity = Interactivity.Inert;
                c.Style.IsInert = true;
            }
            else if (r.TryIdent("auto"))
                c.Style.Interactivity = Interactivity.Auto;
            else
                return false;
            return r.AtEnd;
        }, (t, s) =>
        {
            t.Interactivity = s.Interactivity;
            t.IsInert |= s.IsInert;
        });
        Add("accent-color", true, (r, c) => r.TryIdent("auto") ? Set(() => c.Style.AccentColor = null) : ColorOf((s, v) => s.AccentColor = v)(r, c), (t, s) => t.AccentColor = s.AccentColor);
        // auto, or from-font or under, and left or right (in either order).
        Add("text-underline-position", true, (r, c) =>
        {
            if (r.TryIdent("auto"))
                return r.AtEnd && Set(() => c.Style.TextUnderlinePosition = UnderlinePosition.Auto);
            var position = UnderlinePosition.Auto;
            while (r.TryAnyIdent() is { } k)
            {
                var flag = k switch { "from-font" => UnderlinePosition.FromFont, "under" => UnderlinePosition.Under, "left" => UnderlinePosition.Left, "right" => UnderlinePosition.Right, _ => (UnderlinePosition?)null };
                var vertical = UnderlinePosition.FromFont | UnderlinePosition.Under;
                var side = UnderlinePosition.Left | UnderlinePosition.Right;
                if (flag is not { } f || (position & ((f & vertical) != 0 ? vertical : side)) != 0)
                    return false;
                position |= f;
            }
            return position != UnderlinePosition.Auto && r.AtEnd && Set(() => c.Style.TextUnderlinePosition = position);
        }, (t, s) => t.TextUnderlinePosition = s.TextUnderlinePosition);
        Add("text-decoration-skip-ink", true, Enum(new Dictionary<string, TextDecorationSkipInk>
        {
            ["auto"] = TextDecorationSkipInk.Auto, ["none"] = TextDecorationSkipInk.None, ["all"] = TextDecorationSkipInk.All,
        }, (s, v) => s.TextDecorationSkipInk = v), (t, s) => t.TextDecorationSkipInk = s.TextDecorationSkipInk);
        Add("caret-color", true, (r, c) => r.TryIdent("auto") ? Set(() => c.Style.CaretColor = null) : ColorOf((s, v) => s.CaretColor = v)(r, c), (t, s) => t.CaretColor = s.CaretColor);

        // Text (not inherited).
        Add("text-overflow", false, Enum(new Dictionary<string, TextOverflow> { ["clip"] = TextOverflow.Clip, ["ellipsis"] = TextOverflow.Ellipsis }, (s, v) => s.TextOverflow = v), (t, s) => t.TextOverflow = s.TextOverflow);
        // none, or any of underline, overline, line-through (and blink, which draws nothing), each once.
        Add("text-decoration-line", false, (r, c) =>
        {
            if (r.TryIdent("none"))
                return r.AtEnd && Set(() => (c.Style.Underline, c.Style.LineThrough, c.Style.Overline, c.Style.DecorationBlink) = (false, false, false, false));
            var lines = new HashSet<string>();
            while (r.TryAnyIdent() is { } k)
                if (k is not ("underline" or "line-through" or "overline" or "blink") || !lines.Add(k))
                    return false;
            if (lines.Count == 0 || !r.AtEnd)
                return false;
            (c.Style.Underline, c.Style.LineThrough, c.Style.Overline, c.Style.DecorationBlink) = (lines.Contains("underline"), lines.Contains("line-through"), lines.Contains("overline"), lines.Contains("blink"));
            return true;
        }, (t, s) => (t.Underline, t.LineThrough, t.Overline, t.DecorationBlink) = (s.Underline, s.LineThrough, s.Overline, s.DecorationBlink));
        // currentcolor stays the keyword (null): a decoration's currentcolor is the text's fill colour (DecorationColorIncludingFallback).
        Add("text-decoration-color", false, (r, c) => r.TryIdent("currentcolor") ? r.AtEnd && Set(() => c.Style.TextDecorationColor = null) : ColorOf((s, v) => s.TextDecorationColor = v)(r, c),
            (t, s) => t.TextDecorationColor = s.TextDecorationColor);
        // -webkit-text-fill-color: inherited, initially currentcolor, kept as the keyword (null) so each element fills with its own colour.
        Add("-webkit-text-fill-color", true, (r, c) => r.TryIdent("currentcolor") ? r.AtEnd && Set(() => c.Style.TextFillColor = null) : ColorOf((s, v) => s.TextFillColor = v)(r, c),
            (t, s) => t.TextFillColor = s.TextFillColor);
        Add("resize", false, Enum(new Dictionary<string, ResizeKind>
        {
            ["none"] = ResizeKind.None, ["both"] = ResizeKind.Both, ["horizontal"] = ResizeKind.Horizontal, ["vertical"] = ResizeKind.Vertical,
            ["block"] = ResizeKind.Block, ["inline"] = ResizeKind.Inline, ["auto"] = ResizeKind.Both, ["-internal-textarea-auto"] = ResizeKind.Both,
        }, (s, v) => s.Resize = v), (t, s) => t.Resize = s.Resize);
        Add("text-decoration-style", false, Enum(new Dictionary<string, DecorationStyle>
        {
            ["solid"] = DecorationStyle.Solid, ["double"] = DecorationStyle.Double, ["dotted"] = DecorationStyle.Dotted,
            ["dashed"] = DecorationStyle.Dashed, ["wavy"] = DecorationStyle.Wavy,
        }, (s, v) => s.TextDecorationStyle = v), (t, s) => t.TextDecorationStyle = s.TextDecorationStyle);
        Add("text-decoration-thickness", false, (r, c) =>
        {
            if (r.TryIdent("auto"))
                return r.AtEnd && Set(() => c.Style.TextDecorationThickness = DecorationThickness.Auto);
            if (r.TryIdent("from-font"))
                return r.AtEnd && Set(() => c.Style.TextDecorationThickness = new DecorationThickness(DecorationThicknessKind.FromFont, Length.Px(0)));
            return r.TryLength(c.Units, out var l) && r.AtEnd && Set(() => c.Style.TextDecorationThickness = new DecorationThickness(DecorationThicknessKind.Length, l));
        }, (t, s) => t.TextDecorationThickness = s.TextDecorationThickness);
        Add("text-underline-offset", false, (r, c) =>
        {
            if (r.TryIdent("auto"))
                return r.AtEnd && Set(() => c.Style.TextUnderlineOffset = Length.Auto);
            return r.TryLength(c.Units, out var l) && r.AtEnd && Set(() => c.Style.TextUnderlineOffset = l);
        }, (t, s) => t.TextUnderlineOffset = s.TextUnderlineOffset);
        Add("-webkit-line-clamp", false, (r, c) =>
        {
            if (r.TryIdent("none"))
                c.Style.LineClamp = 0;
            else if (r.TryNumber(out var n))
                c.Style.LineClamp = (int)n;
            else
                return false;
            return r.AtEnd;
        }, (t, s) => t.LineClamp = s.LineClamp);
        Alias("line-clamp", "-webkit-line-clamp");
        Add("vertical-align", false, (r, c) =>
        {
            var keyword = r.TryAnyIdent();
            if (keyword is null)
            {
                if (!r.TryLength(c.Units, out var length))
                    return false;
                c.Style.VerticalAlign = VerticalAlignKind.Length;
                c.Style.VerticalAlignLength = length;
                return r.AtEnd;
            }
            VerticalAlignKind? kind = keyword switch
            {
                "baseline" => VerticalAlignKind.Baseline, "middle" => VerticalAlignKind.Middle, "top" => VerticalAlignKind.Top, "bottom" => VerticalAlignKind.Bottom,
                "text-top" => VerticalAlignKind.TextTop, "text-bottom" => VerticalAlignKind.TextBottom, "sub" => VerticalAlignKind.Sub, "super" => VerticalAlignKind.Super,
                _ => null,
            };
            if (kind is null)
                return false;
            c.Style.VerticalAlign = kind.Value;
            return r.AtEnd;
        }, (t, s) => (t.VerticalAlign, t.VerticalAlignLength) = (s.VerticalAlign, s.VerticalAlignLength));

        // Visual.
        Add("opacity", false, (r, c) =>
        {
            if (r.TryPercentage(out var p))
                c.Style.Opacity = Math.Clamp(p / 100f, 0, 1);
            else if (r.TryNumber(out var n))
                c.Style.Opacity = Math.Clamp(n, 0, 1);
            else
                return false;
            return r.AtEnd;
        }, (t, s) => t.Opacity = s.Opacity);
        Add("background-color", false, (r, c) => r.TryColor(c.Style.Color, out var color) && r.AtEnd && Set(() => c.Style.BackgroundColor = color), (t, s) => t.BackgroundColor = s.BackgroundColor);
        Add("background-image", false, (r, c) =>
        {
            var images = new List<ImageValue>();
            foreach (var part in CssParser.SplitOnCommas(r.Remaining()))
            {
                var p = new ValueReader(part);
                var image = ParseImage(p, c);
                if (image is null || !p.AtEnd)
                    return false;
                images.Add(image);
            }
            c.Style.BackgroundImages = images.ToArray();
            return images.Count > 0;
        }, (t, s) => t.BackgroundImages = s.BackgroundImages);
        Add("background-size", false, (r, c) =>
        {
            var sizes = new List<BackgroundSize>();
            foreach (var part in CssParser.SplitOnCommas(r.Remaining()))
            {
                var p = new ValueReader(part);
                if (ParseBackgroundSize(p, c) is not { } size || !p.AtEnd)
                    return false;
                sizes.Add(size);
            }
            c.Style.BackgroundSizes = sizes.ToArray();
            return sizes.Count > 0;
        }, (t, s) => t.BackgroundSizes = s.BackgroundSizes);
        Add("background-position", false, (r, c) =>
        {
            var positions = new List<BackgroundPosition>();
            foreach (var part in CssParser.SplitOnCommas(r.Remaining()))
            {
                var p = new ValueReader(part);
                if (!ParsePosition(p, c, out var x, out var y) || !p.AtEnd)
                    return false;
                positions.Add(new BackgroundPosition(x, y));
            }
            c.Style.BackgroundPositions = positions.ToArray();
            return positions.Count > 0;
        }, (t, s) => t.BackgroundPositions = s.BackgroundPositions);
        Add("background-position-x", false, (r, c) =>
        {
            if (!ParsePosition(r, c, out var x, out _) || !r.AtEnd)
                return false;
            c.Style.BackgroundPositions = c.Style.BackgroundPositions.Select(p => p with { X = x }).ToArray();
            return true;
        }, (t, s) => t.BackgroundPositions = s.BackgroundPositions);
        Add("background-repeat", false, (r, c) =>
        {
            var repeats = new List<(BackgroundRepeat, BackgroundRepeat)>();
            foreach (var part in CssParser.SplitOnCommas(r.Remaining()))
            {
                var p = new ValueReader(part);
                if (ParseRepeat(p) is not { } repeat || !p.AtEnd)
                    return false;
                repeats.Add(repeat);
            }
            c.Style.BackgroundRepeats = repeats.ToArray();
            return repeats.Count > 0;
        }, (t, s) => t.BackgroundRepeats = s.BackgroundRepeats);
        Add("background-attachment", false, (r, c) =>
        {
            var fixedLayers = new List<bool>();
            foreach (var part in CssParser.SplitOnCommas(r.Remaining()))
            {
                var p = new ValueReader(part);
                var keyword = p.TryAnyIdent();
                if (keyword is not ("scroll" or "fixed" or "local") || !p.AtEnd)
                    return false;
                fixedLayers.Add(keyword == "fixed");
            }
            c.Style.BackgroundFixed = fixedLayers.ToArray();
            return fixedLayers.Count > 0;
        }, (t, s) => t.BackgroundFixed = s.BackgroundFixed);
        // background-clip (Blink's ConsumeBackgroundClip: a visual box or text; border-area isn't the engine's).
        Add("background-clip", false, (r, c) =>
        {
            var clips = new List<BackgroundClip>();
            foreach (var part in CssParser.SplitOnCommas(r.Remaining()))
            {
                var p = new ValueReader(part);
                BackgroundClip? clip = p.TryAnyIdent() switch
                {
                    "border-box" => BackgroundClip.BorderBox,
                    "padding-box" => BackgroundClip.PaddingBox,
                    "content-box" => BackgroundClip.ContentBox,
                    "text" => BackgroundClip.Text,
                    _ => null,
                };
                if (clip is not { } value || !p.AtEnd)
                    return false;
                clips.Add(value);
            }
            c.Style.BackgroundClips = clips.ToArray();
            return clips.Count > 0;
        }, (t, s) => t.BackgroundClips = s.BackgroundClips);
        Alias("-webkit-background-clip", "background-clip");
        Add("box-shadow", false, ShadowsOf((s, v) => s.BoxShadows = v), (t, s) => t.BoxShadows = s.BoxShadows);
        Add("transform", false, (r, c) => ParseTransform(r.Remaining(), c) is { } f && Set(() => c.Style.Transform = f), (t, s) => t.Transform = s.Transform);
        Add("transform-origin", false, (r, c) => ParsePosition(r, c, out var x, out var y) && Set(() => (c.Style.TransformOriginX, c.Style.TransformOriginY) = (x, y)), (t, s) => (t.TransformOriginX, t.TransformOriginY) = (s.TransformOriginX, s.TransformOriginY));
        Add("transform-box", false, Enum(new Dictionary<string, TransformBox> { ["content-box"] = TransformBox.ContentBox, ["border-box"] = TransformBox.BorderBox, ["fill-box"] = TransformBox.FillBox, ["stroke-box"] = TransformBox.StrokeBox, ["view-box"] = TransformBox.ViewBox }, (s, v) => s.TransformBox = v), (t, s) => t.TransformBox = s.TransformBox);
        Add("translate", false, (r, c) =>
        {
            if (r.TryIdent("none"))
            {
                (c.Style.TranslateX, c.Style.TranslateY) = (Length.Zero, Length.Zero);
                return r.AtEnd;
            }
            if (!r.TryLength(c.Units, out var x))
                return false;
            var y = Length.Zero;
            if (!r.AtEnd && !r.TryLength(c.Units, out y))
                return false;
            (c.Style.TranslateX, c.Style.TranslateY) = (x, y);
            return true;
        }, (t, s) => (t.TranslateX, t.TranslateY) = (s.TranslateX, s.TranslateY));
        Add("scale", false, (r, c) =>
        {
            if (r.TryIdent("none"))
            {
                (c.Style.ScaleX, c.Style.ScaleY) = (1, 1);
                return r.AtEnd;
            }
            if (!r.TryNumber(out var x))
                return false;
            var y = x;
            if (!r.AtEnd && !r.TryNumber(out y))
                return false;
            (c.Style.ScaleX, c.Style.ScaleY) = (x, y);
            return true;
        }, (t, s) => (t.ScaleX, t.ScaleY) = (s.ScaleX, s.ScaleY));
        Add("rotate", false, (r, c) =>
        {
            if (r.TryIdent("none"))
                c.Style.Rotate = 0;
            else if (r.TryAngle(out var a))
                c.Style.Rotate = a;
            else
                return false;
            return r.AtEnd;
        }, (t, s) => t.Rotate = s.Rotate);
        Add("filter", false, (r, c) => ParseFilters(r.Remaining(), c) is { } f && Set(() => c.Style.Filters = f), (t, s) => t.Filters = s.Filters);
        Add("backdrop-filter", false, (r, c) => ParseFilters(r.Remaining(), c) is { } f && Set(() => c.Style.BackdropFilters = f), (t, s) => t.BackdropFilters = s.BackdropFilters);
        Alias("-webkit-backdrop-filter", "backdrop-filter");
        Add("mix-blend-mode", false, Enum(new Dictionary<string, CssBlendMode>
        {
            ["normal"] = CssBlendMode.Normal, ["multiply"] = CssBlendMode.Multiply, ["screen"] = CssBlendMode.Screen, ["overlay"] = CssBlendMode.Overlay,
            ["darken"] = CssBlendMode.Darken, ["lighten"] = CssBlendMode.Lighten, ["color-dodge"] = CssBlendMode.ColorDodge, ["color-burn"] = CssBlendMode.ColorBurn,
            ["hard-light"] = CssBlendMode.HardLight, ["soft-light"] = CssBlendMode.SoftLight, ["difference"] = CssBlendMode.Difference, ["exclusion"] = CssBlendMode.Exclusion,
            ["hue"] = CssBlendMode.Hue, ["saturation"] = CssBlendMode.Saturation, ["color"] = CssBlendMode.Color, ["luminosity"] = CssBlendMode.Luminosity, ["plus-lighter"] = CssBlendMode.PlusLighter,
        }, (s, v) => s.MixBlendMode = v), (t, s) => t.MixBlendMode = s.MixBlendMode);
        // will-change: the properties named (lowercase); Blink composites for some of them.
        Add("will-change", false, (r, c) =>
        {
            if (r.TryIdent("auto"))
            {
                c.Style.WillChange = [];
                return r.AtEnd;
            }
            var names = new List<string>();
            do
            {
                if (r.TryAnyIdent() is not { } name || name is "will-change" or "none" or "all" or "auto")
                    return false;
                names.Add(name.ToLowerInvariant());
            }
            while (r.TryComma());
            c.Style.WillChange = [.. names];
            return r.AtEnd;
        }, (t, s) => t.WillChange = s.WillChange);
        Add("isolation", false, (r, c) => (r.TryIdent("isolate") ? Set(() => c.Style.Isolate = true) : r.TryIdent("auto") && Set(() => c.Style.Isolate = false)) && r.AtEnd, (t, s) => t.Isolate = s.Isolate);
        Add("clip-path", false, (r, c) =>
        {
            if (r.TryIdent("none"))
            {
                c.Style.ClipPath = null;
                return r.AtEnd;
            }
            var token = r.Peek();
            if (token.Type != CssTokenType.Function || token.Value != "inset")
                return false;
            var a = new ValueReader(r.FunctionArguments());
            var lengths = new List<Length>();
            while (a.TryLength(c.Units, out var l))
                lengths.Add(l);
            var radii = new Corner[4];
            if (a.TryIdent("round"))
            {
                var rl = new List<Length>();
                while (a.TryLength(c.Units, out var l))
                    rl.Add(l);
                for (var i = 0; i < 4; i++)
                {
                    var v = rl.Count == 0 ? Length.Zero : rl[i % rl.Count];
                    radii[i] = new Corner(v, v);
                }
            }
            if (lengths.Count == 0)
                return false;
            Length E(int i) => lengths[lengths.Count switch { 1 => 0, 2 => i % 2, 3 => i == 3 ? 1 : i, _ => i }];
            c.Style.ClipPath = new ClipInset(E(0), E(1), E(2), E(3), radii);
            return r.AtEnd;
        }, (t, s) => t.ClipPath = s.ClipPath);
        Add("outline-width", false, (r, c) => BorderWidth(r, c, out var w) && r.AtEnd && Set(() => c.Style.OutlineWidth = w), (t, s) => t.OutlineWidth = s.OutlineWidth);
        // outline-style takes auto (the focus ring) and not hidden.
        Add("outline-style", false, (r, c) => (r.TryIdent("auto") ? Set(() => c.Style.OutlineStyle = BorderLineStyle.Auto) : !r.Peek().IsIdent("hidden") && Enum(LineStyles, (s, v) => s.OutlineStyle = v)(r, c)) && r.AtEnd, (t, s) => t.OutlineStyle = s.OutlineStyle);
        Add("outline-color", false, (r, c) => r.TryIdent("invert") ? Set(() => c.Style.OutlineColor = null) : ColorOf((s, v) => s.OutlineColor = v)(r, c), (t, s) => t.OutlineColor = s.OutlineColor);
        Add("outline-offset", false, (r, c) => r.TryLength(c.Units, out var l) && !l.HasPercent && r.AtEnd && Set(() => c.Style.OutlineOffset = OutlineOffset(l.Pixels)), (t, s) => t.OutlineOffset = s.OutlineOffset);
        Add("object-fit", false, Enum(new Dictionary<string, ObjectFit> { ["fill"] = ObjectFit.Fill, ["contain"] = ObjectFit.Contain, ["cover"] = ObjectFit.Cover, ["none"] = ObjectFit.None, ["scale-down"] = ObjectFit.ScaleDown }, (s, v) => s.ObjectFit = v), (t, s) => t.ObjectFit = s.ObjectFit);
        Add("appearance", false, (r, c) => r.TryAnyIdent() is { } k && Set(() => c.Style.Appearance = k) && r.AtEnd, (t, s) => t.Appearance = s.Appearance);
        Alias("-webkit-appearance", "appearance");
        Add("scroll-behavior", false, Enum(new Dictionary<string, ScrollBehavior> { ["auto"] = ScrollBehavior.Auto, ["smooth"] = ScrollBehavior.Smooth }, (s, v) => s.ScrollBehavior = v), (t, s) => t.ScrollBehavior = s.ScrollBehavior);
        Add("scrollbar-width", false, (r, c) => r.TryAnyIdent() is { } k && Set(() => c.Style.ScrollbarWidth = k) && r.AtEnd, (t, s) => t.ScrollbarWidth = s.ScrollbarWidth);
        Add("content", false, (r, c) =>
        {
            if (r.TryIdent("none") || r.TryIdent("normal"))
            {
                c.Style.Content = null;
                return r.AtEnd;
            }
            var items = new List<ContentItem>();
            while (!r.AtEnd)
            {
                var token = r.Peek();
                if (token.Type == CssTokenType.String)
                {
                    r.Next();
                    items.Add(new ContentItem(token.Value));
                }
                else if (token.Type == CssTokenType.Function && token.Value == "attr")
                {
                    var name = r.FunctionArguments().FirstOrDefault(t => t.Type == CssTokenType.Ident).Value ?? "";
                    items.Add(new ContentItem("", name.ToLowerInvariant()));
                }
                else if (token.Type == CssTokenType.Ident && token.Value is "open-quote" or "close-quote")
                {
                    r.Next();
                    items.Add(new ContentItem(token.Value == "open-quote" ? "“" : "”"));
                }
                else
                    return false;
            }
            c.Style.Content = items.ToArray();
            return true;
        }, (t, s) => t.Content = s.Content);

        Add("list-style-type", true, (r, c) =>
        {
            if (r.Peek().Type == CssTokenType.String)
            {
                c.Style.ListStyleType = "string:" + r.Next().Value;
                return r.AtEnd;
            }
            var k = r.TryAnyIdent();
            if (k is not ("none" or "disc" or "circle" or "square" or "decimal" or "decimal-leading-zero" or "lower-alpha" or "upper-alpha" or "lower-latin" or "upper-latin" or "lower-roman" or "upper-roman" or "disclosure-open" or "disclosure-closed"))
                return false;
            c.Style.ListStyleType = k;
            return r.AtEnd;
        }, (t, s) => t.ListStyleType = s.ListStyleType);
        Add("list-style-position", true, (r, c) =>
        {
            if (r.TryIdent("inside"))
                c.Style.ListStyleInside = true;
            else if (r.TryIdent("outside"))
                c.Style.ListStyleInside = false;
            else
                return false;
            return r.AtEnd;
        }, (t, s) => t.ListStyleInside = s.ListStyleInside);
        Add("scrollbar-color", true, (r, c) =>
        {
            if (r.TryIdent("auto"))
            {
                (c.Style.ScrollbarThumb, c.Style.ScrollbarTrack) = (null, null);
                return r.AtEnd;
            }
            if (!r.TryColor(c.Style.Color, out var thumb) || !r.TryColor(c.Style.Color, out var track))
                return false;
            (c.Style.ScrollbarThumb, c.Style.ScrollbarTrack) = (thumb, track);
            return r.AtEnd;
        }, (t, s) => (t.ScrollbarThumb, t.ScrollbarTrack) = (s.ScrollbarThumb, s.ScrollbarTrack));
        Add("font-variant-numeric", true, (r, c) =>
        {
            var tabular = false;
            while (r.TryAnyIdent() is { } k)
            {
                if (k == "tabular-nums")
                    tabular = true;
                else if (k is not ("normal" or "proportional-nums" or "lining-nums" or "oldstyle-nums" or "slashed-zero" or "ordinal" or "diagonal-fractions" or "stacked-fractions"))
                    return false;
            }
            c.Style.TabularNumbers = tabular;
            return r.AtEnd;
        }, (t, s) => t.TabularNumbers = s.TabularNumbers);
        Add("clip", false, (r, c) =>
        {
            if (r.TryIdent("auto"))
            {
                c.Style.ClipRect = null;
                return r.AtEnd;
            }
            var token = r.Peek();
            if (token.Type != CssTokenType.Function || token.Value != "rect")
                return false;
            var a = new ValueReader(r.FunctionArguments());
            var edges = new float?[4];
            for (var i = 0; i < 4; i++)
            {
                if (a.TryIdent("auto"))
                    edges[i] = null;
                else if (a.TryLength(c.Units, out var l))
                    edges[i] = l.Pixels;
                else
                    return false;
                a.TryComma();
            }
            c.Style.ClipRect = edges;
            return a.AtEnd && r.AtEnd;
        }, (t, s) => t.ClipRect = s.ClipRect);
        foreach (var (side, set, copy) in new (string, Action<ComputedStyle, float>, Action<ComputedStyle, ComputedStyle>)[]
        {
            ("top", (s, v) => s.ScrollMarginTop = v, (t, s) => t.ScrollMarginTop = s.ScrollMarginTop),
            ("right", (s, v) => s.ScrollMarginRight = v, (t, s) => t.ScrollMarginRight = s.ScrollMarginRight),
            ("bottom", (s, v) => s.ScrollMarginBottom = v, (t, s) => t.ScrollMarginBottom = s.ScrollMarginBottom),
            ("left", (s, v) => s.ScrollMarginLeft = v, (t, s) => t.ScrollMarginLeft = s.ScrollMarginLeft),
        })
            Add("scroll-margin-" + side, false, (r, c) => r.TryLength(c.Units, out var l) && !l.HasPercent && r.AtEnd && Set(() => set(c.Style, l.Pixels)), copy);
        foreach (var (side, set, copy) in new (string, Action<ComputedStyle, Length>, Action<ComputedStyle, ComputedStyle>)[]
        {
            ("top", (s, v) => s.ScrollPaddingTop = v, (t, s) => t.ScrollPaddingTop = s.ScrollPaddingTop),
            ("right", (s, v) => s.ScrollPaddingRight = v, (t, s) => t.ScrollPaddingRight = s.ScrollPaddingRight),
            ("bottom", (s, v) => s.ScrollPaddingBottom = v, (t, s) => t.ScrollPaddingBottom = s.ScrollPaddingBottom),
            ("left", (s, v) => s.ScrollPaddingLeft = v, (t, s) => t.ScrollPaddingLeft = s.ScrollPaddingLeft),
        })
            Add("scroll-padding-" + side, false, (r, c) => (r.TryIdent("auto") ? Set(() => set(c.Style, Length.Zero)) : r.TryLength(c.Units, out var l) && Set(() => set(c.Style, l))) && r.AtEnd, copy);
        var overscroll = new Dictionary<string, OverscrollBehavior> { ["auto"] = OverscrollBehavior.Auto, ["contain"] = OverscrollBehavior.Contain, ["none"] = OverscrollBehavior.None };
        Add("overscroll-behavior-x", false, Enum(overscroll, (s, v) => s.OverscrollBehaviorX = v), (t, s) => t.OverscrollBehaviorX = s.OverscrollBehaviorX);
        Add("overscroll-behavior-y", false, Enum(overscroll, (s, v) => s.OverscrollBehaviorY = v), (t, s) => t.OverscrollBehaviorY = s.OverscrollBehaviorY);
        Add("scrollbar-gutter", false, (r, c) =>
        {
            if (r.TryIdent("auto"))
                c.Style.ScrollbarGutterStable = false;
            else if (r.TryIdent("stable"))
            {
                c.Style.ScrollbarGutterStable = true;
                r.TryIdent("both-edges");
            }
            else
                return false;
            return r.AtEnd;
        }, (t, s) => t.ScrollbarGutterStable = s.ScrollbarGutterStable);

        // Transitions and animations: each longhand is its own list; ComputedStyle.Finish combines them.
        Add("transition-property", false, (r, c) => ListOf(r, p => p.TryAnyIdent(), out var v) && Set(() => c.Style.TransitionProperty = v.ToArray()), (t, s) => t.TransitionProperty = s.TransitionProperty);
        Add("transition-duration", false, (r, c) => ListOfValues(r, p => p.TryTime(out var v) ? (float?)v : null, out var v) && Set(() => c.Style.TransitionDuration = v.ToArray()), (t, s) => t.TransitionDuration = s.TransitionDuration);
        Add("transition-delay", false, (r, c) => ListOfValues(r, p => p.TryTime(out var v) ? (float?)v : null, out var v) && Set(() => c.Style.TransitionDelay = v.ToArray()), (t, s) => t.TransitionDelay = s.TransitionDelay);
        Add("transition-timing-function", false, (r, c) => ListOf(r, ParseTiming, out var v) && Set(() => c.Style.TransitionTiming = v.ToArray()), (t, s) => t.TransitionTiming = s.TransitionTiming);
        Add("animation-name", false, (r, c) => ListOf(r, p => p.Peek().Type is CssTokenType.Ident or CssTokenType.String ? p.Next().Value : null, out var v) && Set(() => c.Style.AnimationName = v.ToArray()), (t, s) => t.AnimationName = s.AnimationName);
        Add("animation-duration", false, (r, c) => ListOfValues(r, p => p.TryTime(out var v) ? (float?)v : null, out var v) && Set(() => c.Style.AnimationDuration = v.ToArray()), (t, s) => t.AnimationDuration = s.AnimationDuration);
        Add("animation-delay", false, (r, c) => ListOfValues(r, p => p.TryTime(out var v) ? (float?)v : null, out var v) && Set(() => c.Style.AnimationDelay = v.ToArray()), (t, s) => t.AnimationDelay = s.AnimationDelay);
        Add("animation-timing-function", false, (r, c) => ListOf(r, ParseTiming, out var v) && Set(() => c.Style.AnimationTiming = v.ToArray()), (t, s) => t.AnimationTiming = s.AnimationTiming);
        Add("animation-iteration-count", false, (r, c) => ListOfValues(r, p => p.TryIdent("infinite") ? float.PositiveInfinity : p.TryNumber(out var v) && v >= 0 ? (float?)v : null, out var v) && Set(() => c.Style.AnimationIterationCount = v.ToArray()), (t, s) => t.AnimationIterationCount = s.AnimationIterationCount);
        Add("animation-direction", false, (r, c) => ListOfValues(r, p => ParseDirection(p.TryAnyIdent()), out var v) && Set(() => c.Style.AnimationDirectionList = v.ToArray()), (t, s) => t.AnimationDirectionList = s.AnimationDirectionList);
        Add("animation-fill-mode", false, (r, c) => ListOfValues(r, p => ParseFillMode(p.TryAnyIdent()), out var v) && Set(() => c.Style.AnimationFillModeList = v.ToArray()), (t, s) => t.AnimationFillModeList = s.AnimationFillModeList);
        Add("animation-play-state", false, (r, c) => ListOfValues(r, p => p.TryAnyIdent() switch { "running" => false, "paused" => true, _ => (bool?)null }, out var v) && Set(() => c.Style.AnimationPaused = v.ToArray()), (t, s) => t.AnimationPaused = s.AnimationPaused);

        RegisterShorthands();
    }

    static bool Set(Action action)
    {
        action();
        return true;
    }

    internal static BackgroundSize? ParseBackgroundSize(ValueReader p, StyleContext c)
    {
        if (p.TryIdent("cover"))
            return new BackgroundSize(Length.Auto, Length.Auto, "cover");
        if (p.TryIdent("contain"))
            return new BackgroundSize(Length.Auto, Length.Auto, "contain");
        if (!p.TryLengthOrKeyword(c.Units, out var x) || !(x.IsAuto || x.IsDefinite))
            return null;
        var y = Length.Auto;
        var save = p.Position;
        if (!p.AtEnd && !(p.TryLengthOrKeyword(c.Units, out y) && (y.IsAuto || y.IsDefinite)))
        {
            p.Position = save;
            y = Length.Auto;
        }
        return new BackgroundSize(x, y, "");
    }

    internal static (BackgroundRepeat X, BackgroundRepeat Y)? ParseRepeat(ValueReader p)
    {
        static BackgroundRepeat? One(string? k) => k switch
        {
            "repeat" => BackgroundRepeat.Repeat,
            "no-repeat" => BackgroundRepeat.NoRepeat,
            "space" => BackgroundRepeat.Space,
            "round" => BackgroundRepeat.Round,
            _ => null,
        };
        var save = p.Position;
        var a = p.TryAnyIdent();
        if (a == "repeat-x")
            return (BackgroundRepeat.Repeat, BackgroundRepeat.NoRepeat);
        if (a == "repeat-y")
            return (BackgroundRepeat.NoRepeat, BackgroundRepeat.Repeat);
        if (One(a) is not { } x)
        {
            p.Position = save;
            return null;
        }
        var save2 = p.Position;
        if (One(p.TryAnyIdent()) is { } y)
            return (x, y);
        p.Position = save2;
        return (x, x);
    }

    internal static AnimationDirection? ParseDirection(string? keyword) => keyword switch
    {
        "normal" => AnimationDirection.Normal,
        "reverse" => AnimationDirection.Reverse,
        "alternate" => AnimationDirection.Alternate,
        "alternate-reverse" => AnimationDirection.AlternateReverse,
        _ => null,
    };

    internal static AnimationFillMode? ParseFillMode(string? keyword) => keyword switch
    {
        "none" => AnimationFillMode.None,
        "forwards" => AnimationFillMode.Forwards,
        "backwards" => AnimationFillMode.Backwards,
        "both" => AnimationFillMode.Both,
        _ => null,
    };

    /// <summary>A comma-separated list whose items are each parsed by <paramref name="item"/>.</summary>
    static bool ListOf<T>(ValueReader r, Func<ValueReader, T?> item, out List<T> values) where T : class
    {
        values = [];
        while (!r.AtEnd)
        {
            var value = item(r);
            if (value is null)
                return false;
            values.Add(value);
            if (!r.TryComma())
                break;
        }
        return r.AtEnd && values.Count > 0;
    }

    static bool ListOfValues<T>(ValueReader r, Func<ValueReader, T?> item, out List<T> values) where T : struct
    {
        values = [];
        while (!r.AtEnd)
        {
            var value = item(r);
            if (value is null)
                return false;
            values.Add(value.Value);
            if (!r.TryComma())
                break;
        }
        return r.AtEnd && values.Count > 0;
    }
}

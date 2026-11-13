using Statventure.UI.Css;

namespace Statventure.UI.Style;

/// <summary>Shorthand expansion: a shorthand's value becomes longhand values, omitted parts "initial".</summary>
public static partial class Properties
{
    static readonly Dictionary<string, string[]> ShorthandParts = new(StringComparer.Ordinal)
    {
        ["margin"] = ["margin-top", "margin-right", "margin-bottom", "margin-left"],
        ["padding"] = ["padding-top", "padding-right", "padding-bottom", "padding-left"],
        ["inset"] = ["top", "right", "bottom", "left"],
        ["border-width"] = ["border-top-width", "border-right-width", "border-bottom-width", "border-left-width"],
        ["border-style"] = ["border-top-style", "border-right-style", "border-bottom-style", "border-left-style"],
        ["border-color"] = ["border-top-color", "border-right-color", "border-bottom-color", "border-left-color"],
        ["margin-inline"] = ["margin-left", "margin-right"],
        ["margin-block"] = ["margin-top", "margin-bottom"],
        ["padding-inline"] = ["padding-left", "padding-right"],
        ["padding-block"] = ["padding-top", "padding-bottom"],
        ["inset-inline"] = ["left", "right"],
        ["inset-block"] = ["top", "bottom"],
        ["scroll-margin"] = ["scroll-margin-top", "scroll-margin-right", "scroll-margin-bottom", "scroll-margin-left"],
        ["scroll-padding"] = ["scroll-padding-top", "scroll-padding-right", "scroll-padding-bottom", "scroll-padding-left"],
        ["scroll-margin-inline"] = ["scroll-margin-left", "scroll-margin-right"],
        ["scroll-margin-block"] = ["scroll-margin-top", "scroll-margin-bottom"],
        ["scroll-padding-inline"] = ["scroll-padding-left", "scroll-padding-right"],
        ["scroll-padding-block"] = ["scroll-padding-top", "scroll-padding-bottom"],
        ["overscroll-behavior"] = ["overscroll-behavior-x", "overscroll-behavior-y"],
        ["border-top"] = ["border-top-width", "border-top-style", "border-top-color"],
        ["border-right"] = ["border-right-width", "border-right-style", "border-right-color"],
        ["border-bottom"] = ["border-bottom-width", "border-bottom-style", "border-bottom-color"],
        ["border-left"] = ["border-left-width", "border-left-style", "border-left-color"],
        ["border-inline-start"] = ["border-left-width", "border-left-style", "border-left-color"],
        ["border-inline-end"] = ["border-right-width", "border-right-style", "border-right-color"],
        ["border-block-start"] = ["border-top-width", "border-top-style", "border-top-color"],
        ["border-block-end"] = ["border-bottom-width", "border-bottom-style", "border-bottom-color"],
        ["border-inline"] = ["border-left-width", "border-left-style", "border-left-color", "border-right-width", "border-right-style", "border-right-color"],
        ["border-block"] = ["border-top-width", "border-top-style", "border-top-color", "border-bottom-width", "border-bottom-style", "border-bottom-color"],
        ["border"] = ["border-top-width", "border-top-style", "border-top-color", "border-right-width", "border-right-style", "border-right-color", "border-bottom-width", "border-bottom-style", "border-bottom-color", "border-left-width", "border-left-style", "border-left-color"],
        ["outline"] = ["outline-width", "outline-style", "outline-color"],
        ["border-radius"] = ["border-top-left-radius", "border-top-right-radius", "border-bottom-right-radius", "border-bottom-left-radius"],
        ["overflow"] = ["overflow-x", "overflow-y"],
        ["gap"] = ["row-gap", "column-gap"],
        ["place-items"] = ["align-items", "justify-items"],
        ["place-content"] = ["align-content", "justify-content"],
        ["place-self"] = ["align-self", "justify-self"],
        ["flex"] = ["flex-grow", "flex-shrink", "flex-basis"],
        ["flex-flow"] = ["flex-direction", "flex-wrap"],
        ["grid-row"] = ["grid-row-start", "grid-row-end"],
        ["grid-column"] = ["grid-column-start", "grid-column-end"],
        ["grid-area"] = ["grid-row-start", "grid-column-start", "grid-row-end", "grid-column-end"],
        ["grid-template"] = ["grid-template-rows", "grid-template-columns", "grid-template-areas"],
        ["text-decoration"] = ["text-decoration-line", "text-decoration-thickness", "text-decoration-style", "text-decoration-color"],
        ["background"] = ["background-color", "background-image", "background-position", "background-size", "background-repeat", "background-attachment", "background-clip"],
        ["list-style"] = ["list-style-type", "list-style-position"],
        ["font"] = ["font-style", "font-weight", "font-size", "line-height", "font-family", "font-variant-numeric"],
        ["transition"] = ["transition-property", "transition-duration", "transition-timing-function", "transition-delay"],
        ["animation"] = ["animation-name", "animation-duration", "animation-timing-function", "animation-delay", "animation-iteration-count", "animation-direction", "animation-fill-mode", "animation-play-state"],
    };

    /// <summary>The longhands a shorthand sets (for CSS-wide keywords and var() in shorthands).</summary>
    public static IReadOnlyList<string> LonghandsOf(string shorthand) => ShorthandParts.TryGetValue(Canonical(shorthand), out var parts) ? parts : [];

    static readonly CssToken InitialToken = new(CssTokenType.Ident, "initial");
    static List<CssToken> InitialValue => [InitialToken];

    /// <summary>The value's top-level components: single tokens, or a function with its arguments.</summary>
    internal static List<List<CssToken>> Components(List<CssToken> tokens)
    {
        var components = new List<List<CssToken>>();
        List<CssToken>? current = null;
        var depth = 0;
        foreach (var token in tokens)
        {
            if (depth == 0 && token.Type == CssTokenType.Whitespace)
            {
                current = null;
                continue;
            }
            if (depth == 0 && current is null)
            {
                current = [];
                components.Add(current);
            }
            current!.Add(token);
            if (token.Type is CssTokenType.Function or CssTokenType.LeftParen or CssTokenType.LeftBracket)
                depth++;
            else if (token.Type is CssTokenType.RightParen or CssTokenType.RightBracket && depth > 0)
            {
                depth--;
                if (depth == 0)
                    current = null;
            }
            else if (depth == 0)
                current = null;
        }
        return components;
    }

    /// <summary>Splits at a top-level "/" delimiter.</summary>
    static List<List<CssToken>> SplitOnSlash(List<CssToken> tokens)
    {
        var parts = new List<List<CssToken>> { new() };
        var depth = 0;
        foreach (var token in tokens)
        {
            if (token.Type is CssTokenType.Function or CssTokenType.LeftParen or CssTokenType.LeftBracket)
                depth++;
            else if (token.Type is CssTokenType.RightParen or CssTokenType.RightBracket)
                depth--;
            if (depth == 0 && token.IsDelim('/'))
            {
                parts.Add([]);
                continue;
            }
            parts[^1].Add(token);
        }
        return parts.Select(CssParser.Trim).ToList();
    }

    static List<CssToken> Join(IEnumerable<List<CssToken>> components)
    {
        var joined = new List<CssToken>();
        foreach (var component in components)
        {
            if (joined.Count > 0)
                joined.Add(CssToken.Space);
            joined.AddRange(component);
        }
        return joined;
    }

    static List<CssToken> JoinCommas(IEnumerable<List<CssToken>> items)
    {
        var joined = new List<CssToken>();
        foreach (var item in items)
        {
            if (joined.Count > 0)
                joined.Add(new CssToken(CssTokenType.Comma, ","));
            joined.AddRange(item);
        }
        return joined;
    }

    static bool IsLengthLike(List<CssToken> component)
    {
        var token = component[0];
        return token.Type is CssTokenType.Dimension or CssTokenType.Percentage
            || (token.Type == CssTokenType.Number && token.Number == 0)
            || (token.Type == CssTokenType.Function && token.Value is "calc" or "min" or "max" or "clamp");
    }

    static bool IsNumberLike(List<CssToken> component) =>
        component[0].Type == CssTokenType.Number || (component[0].Type == CssTokenType.Function && component[0].Value == "calc" && !component.Any(t => t.Type is CssTokenType.Dimension or CssTokenType.Percentage));

    static bool IsTimeLike(List<CssToken> component) =>
        (component[0].Type == CssTokenType.Dimension && component[0].Unit is "s" or "ms")
        || (component[0].Type == CssTokenType.Function && component[0].Value == "calc" && component.Any(t => t.Type == CssTokenType.Dimension && t.Unit is "s" or "ms"));

    static bool IsIdent(List<CssToken> component, params string[] names) =>
        component.Count == 1 && component[0].Type == CssTokenType.Ident && names.Contains(component[0].Value.ToLowerInvariant());

    static bool IsTimingLike(List<CssToken> component) =>
        IsIdent(component, "linear", "ease", "ease-in", "ease-out", "ease-in-out", "step-start", "step-end")
        || (component[0].Type == CssTokenType.Function && component[0].Value is "cubic-bezier" or "steps" or "linear");

    /// <summary>1–4 values for top, right, bottom, left.</summary>
    static Func<List<CssToken>, List<(string, List<CssToken>)>?> Box(string top, string right, string bottom, string left) => value =>
    {
        var parts = Components(value);
        if (parts.Count is < 1 or > 4)
            return null;
        var t = parts[0];
        var r = parts.Count > 1 ? parts[1] : t;
        var b = parts.Count > 2 ? parts[2] : t;
        var l = parts.Count > 3 ? parts[3] : r;
        return [(top, t), (right, r), (bottom, b), (left, l)];
    };

    /// <summary>1–2 values for a start/end (or row/column) pair.</summary>
    static Func<List<CssToken>, List<(string, List<CssToken>)>?> Pair(string first, string second) => value =>
    {
        var parts = Components(value);
        if (parts.Count is < 1 or > 2)
            return null;
        return [(first, parts[0]), (second, parts.Count > 1 ? parts[1] : parts[0])];
    };

    /// <summary>"width style color" in any order (border sides, outline).</summary>
    static List<(string, List<CssToken>)>? Border(List<CssToken> value, string width, string style, string color)
    {
        List<CssToken>? w = null, s = null, c = null;
        foreach (var part in Components(value))
        {
            if (w is null && (IsLengthLike(part) || IsIdent(part, "thin", "medium", "thick")))
                w = part;
            else if (s is null && part.Count == 1 && part[0].Type == CssTokenType.Ident && (style == "outline-style" ? part[0].IsIdent("auto") || (LineStyles.ContainsKey(part[0].Value.ToLowerInvariant()) && !part[0].IsIdent("hidden")) : LineStyles.ContainsKey(part[0].Value.ToLowerInvariant())))
                s = part;
            else if (c is null)
                c = part;
            else
                return null;
        }
        return [(width, w ?? InitialValue), (style, s ?? InitialValue), (color, c ?? InitialValue)];
    }

    static List<(string, List<CssToken>)> Sides(Func<string, List<(string, List<CssToken>)>?> side, out bool valid)
    {
        var all = new List<(string, List<CssToken>)>();
        valid = true;
        foreach (var name in new[] { "top", "right", "bottom", "left" })
        {
            var longhands = side(name);
            if (longhands is null)
            {
                valid = false;
                return all;
            }
            all.AddRange(longhands);
        }
        return all;
    }

    /// <summary>A value that is just a custom identifier (a grid line or area name).</summary>
    static bool IsCustomIdent(List<CssToken> value)
    {
        var parts = Components(value);
        return parts.Count == 1 && parts[0].Count == 1 && parts[0][0].Type == CssTokenType.Ident && !parts[0][0].IsIdent("auto") && !parts[0][0].IsIdent("span");
    }

    static void RegisterShorthands()
    {
        Shorthands["margin"] = Box("margin-top", "margin-right", "margin-bottom", "margin-left");
        Shorthands["padding"] = Box("padding-top", "padding-right", "padding-bottom", "padding-left");
        Shorthands["inset"] = Box("top", "right", "bottom", "left");
        Shorthands["border-width"] = Box("border-top-width", "border-right-width", "border-bottom-width", "border-left-width");
        Shorthands["border-style"] = Box("border-top-style", "border-right-style", "border-bottom-style", "border-left-style");
        Shorthands["border-color"] = Box("border-top-color", "border-right-color", "border-bottom-color", "border-left-color");
        // Logical properties, for a left-to-right horizontal writing mode.
        Shorthands["margin-inline"] = Pair("margin-left", "margin-right");
        Shorthands["margin-block"] = Pair("margin-top", "margin-bottom");
        Shorthands["padding-inline"] = Pair("padding-left", "padding-right");
        Shorthands["padding-block"] = Pair("padding-top", "padding-bottom");
        Shorthands["inset-inline"] = Pair("left", "right");
        Shorthands["inset-block"] = Pair("top", "bottom");
        Shorthands["scroll-margin"] = Box("scroll-margin-top", "scroll-margin-right", "scroll-margin-bottom", "scroll-margin-left");
        Shorthands["scroll-padding"] = Box("scroll-padding-top", "scroll-padding-right", "scroll-padding-bottom", "scroll-padding-left");
        Shorthands["scroll-margin-inline"] = Pair("scroll-margin-left", "scroll-margin-right");
        Shorthands["scroll-margin-block"] = Pair("scroll-margin-top", "scroll-margin-bottom");
        Shorthands["scroll-padding-inline"] = Pair("scroll-padding-left", "scroll-padding-right");
        Shorthands["scroll-padding-block"] = Pair("scroll-padding-top", "scroll-padding-bottom");
        Shorthands["overscroll-behavior"] = Pair("overscroll-behavior-x", "overscroll-behavior-y");
        foreach (var (logical, physical) in new[] { ("inline-start", "left"), ("inline-end", "right"), ("block-start", "top"), ("block-end", "bottom") })
        {
            Alias($"margin-{logical}", $"margin-{physical}");
            Alias($"padding-{logical}", $"padding-{physical}");
            Alias($"border-{logical}-width", $"border-{physical}-width");
            Alias($"border-{logical}-style", $"border-{physical}-style");
            Alias($"border-{logical}-color", $"border-{physical}-color");
            Alias($"inset-{logical}", physical);
        }
        Alias("inline-size", "width");
        Alias("block-size", "height");
        Alias("min-inline-size", "min-width");
        Alias("min-block-size", "min-height");
        Alias("max-inline-size", "max-width");
        Alias("max-block-size", "max-height");
        Alias("grid-row-gap", "row-gap");
        Alias("grid-column-gap", "column-gap");

        foreach (var side in new[] { "top", "right", "bottom", "left" })
        {
            var s = side;
            Shorthands[$"border-{s}"] = value => Border(value, $"border-{s}-width", $"border-{s}-style", $"border-{s}-color");
        }
        foreach (var (logical, physical) in new[] { ("inline-start", "left"), ("inline-end", "right"), ("block-start", "top"), ("block-end", "bottom") })
        {
            var p = physical;
            Shorthands[$"border-{logical}"] = value => Border(value, $"border-{p}-width", $"border-{p}-style", $"border-{p}-color");
        }
        Shorthands["border-inline"] = value => Border(value, "border-left-width", "border-left-style", "border-left-color") is { } left
            ? [.. left, .. Border(value, "border-right-width", "border-right-style", "border-right-color")!]
            : null;
        Shorthands["border-block"] = value => Border(value, "border-top-width", "border-top-style", "border-top-color") is { } top
            ? [.. top, .. Border(value, "border-bottom-width", "border-bottom-style", "border-bottom-color")!]
            : null;
        Shorthands["border"] = value =>
        {
            var all = Sides(side => Border(value, $"border-{side}-width", $"border-{side}-style", $"border-{side}-color"), out var valid);
            return valid ? all : null;
        };
        Shorthands["outline"] = value => Border(value, "outline-width", "outline-style", "outline-color");
        Shorthands["border-radius"] = value =>
        {
            var halves = SplitOnSlash(value);
            if (halves.Count > 2)
                return null;
            static List<CssToken>[]? Four(List<CssToken> tokens)
            {
                var parts = Components(tokens);
                if (parts.Count is < 1 or > 4)
                    return null;
                var a = parts[0];
                var b = parts.Count > 1 ? parts[1] : a;
                var c = parts.Count > 2 ? parts[2] : a;
                var d = parts.Count > 3 ? parts[3] : b;
                return [a, b, c, d];
            }
            var x = Four(halves[0]);
            var y = halves.Count > 1 ? Four(halves[1]) : x;
            if (x is null || y is null)
                return null;
            var corners = new[] { "top-left", "top-right", "bottom-right", "bottom-left" };
            return corners.Select((corner, i) => ($"border-{corner}-radius", Join([x[i], y[i]]))).ToList();
        };

        Shorthands["overflow"] = Pair("overflow-x", "overflow-y");
        Shorthands["gap"] = Pair("row-gap", "column-gap");
        Alias("grid-gap", "gap");
        Shorthands["place-items"] = Pair("align-items", "justify-items");
        Shorthands["place-content"] = Pair("align-content", "justify-content");
        Shorthands["place-self"] = Pair("align-self", "justify-self");

        Shorthands["flex"] = value =>
        {
            var parts = Components(value);
            static List<CssToken> Number(double n) => [new CssToken(CssTokenType.Number, Number: n, IsInteger: n == Math.Floor(n))];
            if (parts.Count == 1 && IsIdent(parts[0], "none"))
                return [("flex-grow", Number(0)), ("flex-shrink", Number(0)), ("flex-basis", [new CssToken(CssTokenType.Ident, "auto")])];
            if (parts.Count == 1 && IsIdent(parts[0], "auto"))
                return [("flex-grow", Number(1)), ("flex-shrink", Number(1)), ("flex-basis", [new CssToken(CssTokenType.Ident, "auto")])];
            if (parts.Count is < 1 or > 3)
                return null;
            List<CssToken>? grow = null, shrink = null, basis = null;
            foreach (var part in parts)
            {
                if (IsNumberLike(part) && !(part[0].Type == CssTokenType.Number && part[0].Number == 0 && grow is not null && shrink is not null))
                {
                    if (grow is null)
                        grow = part;
                    else if (shrink is null)
                        shrink = part;
                    else
                        return null;
                }
                else if (basis is null)
                    basis = part;
                else
                    return null;
            }
            // flex: <grow> [<shrink>] gives a basis of 0%.
            return [("flex-grow", grow ?? Number(1)), ("flex-shrink", shrink ?? Number(1)), ("flex-basis", basis ?? [new CssToken(CssTokenType.Percentage, Number: 0)])];
        };
        Shorthands["flex-flow"] = value =>
        {
            List<CssToken>? direction = null, wrap = null;
            foreach (var part in Components(value))
            {
                if (IsIdent(part, "row", "row-reverse", "column", "column-reverse") && direction is null)
                    direction = part;
                else if (IsIdent(part, "nowrap", "wrap", "wrap-reverse") && wrap is null)
                    wrap = part;
                else
                    return null;
            }
            return [("flex-direction", direction ?? InitialValue), ("flex-wrap", wrap ?? InitialValue)];
        };

        Shorthands["grid-row"] = value => GridLinePair(value, "grid-row-start", "grid-row-end");
        Shorthands["grid-column"] = value => GridLinePair(value, "grid-column-start", "grid-column-end");
        static List<(string, List<CssToken>)>? GridLinePair(List<CssToken> value, string start, string end)
        {
            var parts = SplitOnSlash(value);
            if (parts.Count > 2 || parts[0].Count == 0)
                return null;
            var second = parts.Count > 1 ? parts[1] : IsCustomIdent(parts[0]) ? parts[0] : [new CssToken(CssTokenType.Ident, "auto")];
            return [(start, parts[0]), (end, second)];
        }
        Shorthands["grid-area"] = value =>
        {
            var parts = SplitOnSlash(value);
            if (parts.Count > 4 || parts[0].Count == 0)
                return null;
            List<CssToken> Auto() => [new CssToken(CssTokenType.Ident, "auto")];
            var rowStart = parts[0];
            var columnStart = parts.Count > 1 ? parts[1] : IsCustomIdent(rowStart) ? rowStart : Auto();
            var rowEnd = parts.Count > 2 ? parts[2] : IsCustomIdent(rowStart) ? rowStart : Auto();
            var columnEnd = parts.Count > 3 ? parts[3] : IsCustomIdent(columnStart) ? columnStart : Auto();
            return [("grid-row-start", rowStart), ("grid-column-start", columnStart), ("grid-row-end", rowEnd), ("grid-column-end", columnEnd)];
        };
        Shorthands["grid-template"] = value =>
        {
            if (IsIdent(Components(value) is { Count: 1 } c ? c[0] : [], "none"))
                return [("grid-template-rows", value), ("grid-template-columns", value), ("grid-template-areas", value)];
            var parts = SplitOnSlash(value);
            if (parts.Count != 2)
                return null;
            // "<rows> / <columns>" (the areas form with strings is not used by the game's styles).
            if (parts[0].Any(t => t.Type == CssTokenType.String))
            {
                var areas = parts[0].Where(t => t.Type == CssTokenType.String).ToList();
                var rows = parts[0].Where(t => t.Type != CssTokenType.String).ToList();
                return [("grid-template-areas", Join(areas.Select(a => new List<CssToken> { a }))), ("grid-template-rows", CssParser.Trim(rows) is { Count: > 0 } r ? r : [new CssToken(CssTokenType.Ident, "auto")]), ("grid-template-columns", parts[1])];
            }
            return [("grid-template-rows", parts[0]), ("grid-template-columns", parts[1]), ("grid-template-areas", InitialValue)];
        };

        // text-decoration: <line> || <thickness> || <style> || <color>, the line keywords together.
        Shorthands["text-decoration"] = value =>
        {
            var lines = new List<List<CssToken>>();
            List<CssToken>? thickness = null, style = null, color = null;
            var parts = Components(value);
            for (var i = 0; i < parts.Count; i++)
            {
                var part = parts[i];
                if (IsIdent(part, "none", "underline", "overline", "line-through", "blink"))
                {
                    // The line keywords form one group.
                    if (lines.Count > 0 && !IsIdent(parts[i - 1], "none", "underline", "overline", "line-through", "blink"))
                        return null;
                    lines.Add(part);
                }
                else if (style is null && IsIdent(part, "solid", "double", "dotted", "dashed", "wavy"))
                    style = part;
                else if (thickness is null && (IsLengthLike(part) || IsIdent(part, "auto", "from-font")))
                    thickness = part;
                else if (color is null)
                    color = part;
                else
                    return null;
            }
            return
            [
                ("text-decoration-line", lines.Count > 0 ? Join(lines) : InitialValue), ("text-decoration-thickness", thickness ?? InitialValue),
                ("text-decoration-style", style ?? InitialValue), ("text-decoration-color", color ?? InitialValue),
            ];
        };

        Shorthands["list-style"] = value =>
        {
            List<CssToken>? type = null, position = null;
            var nones = 0;
            foreach (var part in Components(value))
            {
                if (IsIdent(part, "inside", "outside") && position is null)
                    position = part;
                else if (IsIdent(part, "none"))
                    nones++;
                else if (type is null && (part[0].Type is CssTokenType.Ident or CssTokenType.String))
                    type = part;
                else if (part[0].Type == CssTokenType.Url || part[0].Type == CssTokenType.Function)
                    continue;
                else
                    return null;
            }
            if (nones > 0 && type is null)
                type = [new CssToken(CssTokenType.Ident, "none")];
            return [("list-style-type", type ?? InitialValue), ("list-style-position", position ?? InitialValue)];
        };
        Shorthands["background"] = ExpandBackground;
        Shorthands["font"] = ExpandFont;
        Shorthands["transition"] = ExpandTransition;
        Shorthands["animation"] = ExpandAnimation;
    }

    static List<(string, List<CssToken>)>? ExpandBackground(List<CssToken> value)
    {
        var layers = CssParser.SplitOnCommas(value);
        var images = new List<List<CssToken>>();
        var positions = new List<List<CssToken>>();
        var sizes = new List<List<CssToken>>();
        var repeats = new List<List<CssToken>>();
        var attachments = new List<List<CssToken>>();
        var clips = new List<List<CssToken>>();
        List<CssToken>? color = null;
        for (var l = 0; l < layers.Count; l++)
        {
            List<CssToken>? image = null, repeat = null, attachment = null;
            // Boxes (ParseBackgroundOrMask): the first a visual box is background-origin (the engine's isn't a
            // property), the next one background-clip, which takes text too; one box alone clips as well.
            List<CssToken>? origin = null, clip = null;
            var position = new List<List<CssToken>>();
            var size = new List<List<CssToken>>();
            var parts = Components(layers[l]);
            for (var i = 0; i < parts.Count; i++)
            {
                var part = parts[i];
                if (part.Count == 1 && part[0].IsDelim('/'))
                {
                    // position / size
                    for (i++; i < parts.Count && (IsLengthLike(parts[i]) || IsIdent(parts[i], "auto", "cover", "contain")) && size.Count < 2; i++)
                        size.Add(parts[i]);
                    i--;
                    continue;
                }
                if (image is null && (IsIdent(part, "none") || part[0].Type == CssTokenType.Url || (part[0].Type == CssTokenType.Function && part[0].Value.Contains("gradient")) || (part[0].Type == CssTokenType.Function && part[0].Value == "url")))
                    image = part;
                else if (IsIdent(part, "repeat", "no-repeat", "repeat-x", "repeat-y", "space", "round"))
                    repeat = repeat is null ? part : Join([repeat, part]);
                else if (IsIdent(part, "left", "right", "top", "bottom", "center") || IsLengthLike(part))
                    position.Add(part);
                else if (IsIdent(part, "scroll", "fixed", "local"))
                    attachment = part;
                else if (origin is null && IsIdent(part, "border-box", "padding-box", "content-box"))
                    origin = part;
                else if (clip is null && IsIdent(part, "border-box", "padding-box", "content-box", "text"))
                    clip = part;
                else if (l == layers.Count - 1 && color is null)
                    color = part;
                else
                    return null;
            }
            images.Add(image ?? [new CssToken(CssTokenType.Ident, "none")]);
            positions.Add(position.Count > 0 ? Join(position) : [new CssToken(CssTokenType.Percentage, Number: 0), CssToken.Space, new CssToken(CssTokenType.Percentage, Number: 0)]);
            sizes.Add(size.Count > 0 ? Join(size) : [new CssToken(CssTokenType.Ident, "auto")]);
            repeats.Add(repeat ?? [new CssToken(CssTokenType.Ident, "repeat")]);
            attachments.Add(attachment ?? [new CssToken(CssTokenType.Ident, "scroll")]);
            clips.Add(clip ?? origin ?? [new CssToken(CssTokenType.Ident, "border-box")]);
        }
        return
        [
            ("background-color", color ?? InitialValue),
            ("background-image", JoinCommas(images)),
            ("background-position", JoinCommas(positions)),
            ("background-size", JoinCommas(sizes)),
            ("background-repeat", JoinCommas(repeats)),
            ("background-attachment", JoinCommas(attachments)),
            ("background-clip", JoinCommas(clips)),
        ];
    }

    static List<(string, List<CssToken>)>? ExpandFont(List<CssToken> value)
    {
        var parts = Components(value);
        List<CssToken>? style = null, weight = null, size = null, lineHeight = null;
        var i = 0;
        // Up to four of style / variant / weight / stretch, then the size.
        for (; i < parts.Count; i++)
        {
            var part = parts[i];
            if (IsIdent(part, "normal"))
                continue;
            if (style is null && IsIdent(part, "italic", "oblique"))
                style = part;
            else if (weight is null && (IsIdent(part, "bold", "bolder", "lighter") || (part[0].Type == CssTokenType.Number && part[0].Number >= 1 && part[0].Number <= 1000)))
                weight = part;
            else if (IsIdent(part, "small-caps", "condensed", "expanded", "semi-condensed", "semi-expanded", "ultra-condensed", "ultra-expanded", "extra-condensed", "extra-expanded"))
                continue;
            else
                break;
        }
        if (i >= parts.Count)
            return null;
        // The size, optionally followed by "/ line-height" (the slash may be its own component).
        var sizePart = parts[i++];
        size = sizePart;
        if (i < parts.Count && parts[i].Count == 1 && parts[i][0].IsDelim('/'))
        {
            if (i + 1 >= parts.Count)
                return null;
            lineHeight = parts[i + 1];
            i += 2;
        }
        if (i >= parts.Count)
            return null;
        var family = Join(parts.Skip(i));
        if (!(IsLengthLike(size) || IsIdent(size, "xx-small", "x-small", "small", "medium", "large", "x-large", "xx-large", "xxx-large", "smaller", "larger")))
            return null;
        return
        [
            ("font-style", style ?? InitialValue),
            ("font-weight", weight ?? InitialValue),
            ("font-size", size),
            ("line-height", lineHeight ?? InitialValue),
            ("font-family", family),
            ("font-variant-numeric", InitialValue),
        ];
    }

    static List<(string, List<CssToken>)>? ExpandTransition(List<CssToken> value)
    {
        var properties = new List<List<CssToken>>();
        var durations = new List<List<CssToken>>();
        var timings = new List<List<CssToken>>();
        var delays = new List<List<CssToken>>();
        foreach (var item in CssParser.SplitOnCommas(value))
        {
            List<CssToken>? property = null, duration = null, timing = null, delay = null;
            foreach (var part in Components(item))
            {
                if (IsTimeLike(part))
                {
                    if (duration is null)
                        duration = part;
                    else if (delay is null)
                        delay = part;
                    else
                        return null;
                }
                else if (timing is null && IsTimingLike(part))
                    timing = part;
                else if (property is null && part.Count == 1 && part[0].Type == CssTokenType.Ident)
                    property = part;
                else
                    return null;
            }
            properties.Add(property ?? [new CssToken(CssTokenType.Ident, "all")]);
            durations.Add(duration ?? [new CssToken(CssTokenType.Dimension, Number: 0, Unit: "s")]);
            timings.Add(timing ?? [new CssToken(CssTokenType.Ident, "ease")]);
            delays.Add(delay ?? [new CssToken(CssTokenType.Dimension, Number: 0, Unit: "s")]);
        }
        return
        [
            ("transition-property", JoinCommas(properties)),
            ("transition-duration", JoinCommas(durations)),
            ("transition-timing-function", JoinCommas(timings)),
            ("transition-delay", JoinCommas(delays)),
        ];
    }

    static List<(string, List<CssToken>)>? ExpandAnimation(List<CssToken> value)
    {
        var names = new List<List<CssToken>>();
        var durations = new List<List<CssToken>>();
        var timings = new List<List<CssToken>>();
        var delays = new List<List<CssToken>>();
        var counts = new List<List<CssToken>>();
        var directions = new List<List<CssToken>>();
        var fills = new List<List<CssToken>>();
        var states = new List<List<CssToken>>();
        static List<CssToken> Ident(string name) => [new CssToken(CssTokenType.Ident, name)];
        static List<CssToken> Seconds(double s) => [new CssToken(CssTokenType.Dimension, Number: s, Unit: "s")];
        foreach (var item in CssParser.SplitOnCommas(value))
        {
            List<CssToken>? name = null, duration = null, timing = null, delay = null, count = null, direction = null, fill = null, state = null;
            foreach (var part in Components(item))
            {
                // A keyword goes to the first property it can belong to that isn't set yet; whatever is
                // left is the name (CSS Animations §3.10).
                if (IsTimeLike(part))
                {
                    if (duration is null)
                        duration = part;
                    else if (delay is null)
                        delay = part;
                    else
                        return null;
                }
                else if (timing is null && IsTimingLike(part))
                    timing = part;
                else if (count is null && (IsNumberLike(part) || IsIdent(part, "infinite")))
                    count = part;
                else if (direction is null && IsIdent(part, "normal", "reverse", "alternate", "alternate-reverse"))
                    direction = part;
                else if (fill is null && IsIdent(part, "none", "forwards", "backwards", "both"))
                    fill = part;
                else if (state is null && IsIdent(part, "running", "paused"))
                    state = part;
                else if (name is null && part.Count == 1 && part[0].Type is CssTokenType.Ident or CssTokenType.String)
                    name = part;
                else
                    return null;
            }
            names.Add(name ?? Ident("none"));
            durations.Add(duration ?? Seconds(0));
            timings.Add(timing ?? Ident("ease"));
            delays.Add(delay ?? Seconds(0));
            counts.Add(count ?? [new CssToken(CssTokenType.Number, Number: 1, IsInteger: true)]);
            directions.Add(direction ?? Ident("normal"));
            fills.Add(fill ?? Ident("none"));
            states.Add(state ?? Ident("running"));
        }
        return
        [
            ("animation-name", JoinCommas(names)),
            ("animation-duration", JoinCommas(durations)),
            ("animation-timing-function", JoinCommas(timings)),
            ("animation-delay", JoinCommas(delays)),
            ("animation-iteration-count", JoinCommas(counts)),
            ("animation-direction", JoinCommas(directions)),
            ("animation-fill-mode", JoinCommas(fills)),
            ("animation-play-state", JoinCommas(states)),
        ];
    }
}

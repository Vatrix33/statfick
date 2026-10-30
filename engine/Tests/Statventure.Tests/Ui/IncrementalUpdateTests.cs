using System.Globalization;
using System.IO.Compression;
using System.Numerics;
using System.Text;
using System.Text.Json;
using Statventure.Tests.Infrastructure;
using Statventure.UI.Dom;
using Statventure.UI.Layout;
using Statventure.UI.Style;

namespace Statventure.Tests.Ui;

/// <summary>
/// Updating a document after changes must give exactly what building it from scratch gives: each
/// game screen is changed at random (classes, text, inline styles, attributes, removed and copied
/// elements) and updated, then compared with a fresh document of the same tree — every computed
/// style, and every box with its geometry, lines and line items.
/// </summary>
public class IncrementalUpdateTests
{
    /// <summary>Rounds of changes per screen (STATVENTURE_INCREMENTAL_ROUNDS raises it for a longer search).</summary>
    static int Rounds => int.TryParse(Environment.GetEnvironmentVariable("STATVENTURE_INCREMENTAL_ROUNDS"), out var rounds) ? rounds : 8;

    static readonly Lazy<JsonElement> Capture = new(() =>
    {
        var path = Path.Combine(Path.GetDirectoryName(Golden.GoldenPath("x"))!, "game-ui.json.gz");
        using var stream = new GZipStream(File.OpenRead(path), CompressionMode.Decompress);
        return JsonDocument.Parse(stream).RootElement;
    });

    public static TheoryData<string> States => GameLayoutTests.States;

    static readonly string[] InlineStyles =
    [
        "display: none", "display: block", "display: inline", "display: flex", "display: grid", "display: inline-block", "display: contents",
        "width: 57px", "width: 40%", "height: 31px", "min-width: 120px", "max-width: 90px", "padding: 3px 9px", "margin: 4px auto", "border: 2px solid red",
        "font-size: 19px", "line-height: 1.7", "letter-spacing: 1px", "white-space: nowrap", "text-transform: uppercase", "text-align: center",
        "position: absolute; left: 10px; top: 6px", "position: relative; top: 3px", "position: fixed; right: 0; bottom: 0", "flex: 1 1 0", "flex-direction: column",
        "overflow: auto", "overflow: hidden", "color: rgb(200, 30, 40)", "background: rgb(20, 90, 160)", "opacity: 0.5", "visibility: hidden",
        "box-shadow: 0 2px 6px black", "transform: rotate(4deg)", "filter: blur(2px)", "outline: 2px solid blue", "z-index: 5",
        "content: 'x'", "list-style: decimal inside", "grid-template-columns: 1fr 2fr", "gap: 7px", "align-items: center", "justify-content: space-between",
        "",
    ];

    [Theory]
    [MemberData(nameof(States))]
    public void UpdatesMatchAFreshDocument(string name)
    {
        var state = Capture.Value.GetProperty("states").EnumerateArray().Single(s => s.GetProperty("name").GetString() == name);
        var document = GameLayoutTests.Build(state);
        // Deterministic per state (string hashes are randomized per process): seeded from the name's characters.
        // STATVENTURE_INCREMENTAL_SEED explores other sequences.
        var seed = int.TryParse(Environment.GetEnvironmentVariable("STATVENTURE_INCREMENTAL_SEED"), out var extra) ? extra : 0;
        var random = new Random(name.Aggregate(17 + seed, (h, c) => h * 31 + c));
        var classes = document.AllElements().SelectMany(e => e.Classes).Distinct().OrderBy(c => c, StringComparer.Ordinal).ToArray();
        var log = new List<string>();
        for (var round = 0; round < Rounds; round++)
        {
            var changes = random.Next(1, 4);
            for (var c = 0; c < changes; c++)
                log.Add($"round {round}: " + Mutate(document, random, classes));
            Normalize(document.Body);
            document.Update();
            var fresh = Fresh(state, document);
            var problems = Compare(document, fresh);
            Assert.True(problems.Count == 0, $"{name}, after:\n  {string.Join("\n  ", log)}\n{problems.Count} differences:\n" + string.Join("\n", problems.Take(25)));
        }
    }

    // ---------- Changes ----------

    internal static string Mutate(UiDocument document, Random random, string[] classes)
    {
        var elements = document.Body.Descendants().Where(e => e.Tag is not ("style" or "script")).ToList();
        var element = elements[random.Next(elements.Count)];
        var path = PathOf(element);
        switch (random.Next(8))
        {
            case 7:
            {
                // Interaction states (focus-within follows the document's focus, which isn't changed here).
                switch (random.Next(3))
                {
                    case 0:
                        element.IsHovered = !element.IsHovered;
                        return $"hover {element.IsHovered} on {path}";
                    case 1:
                        element.IsActive = !element.IsActive;
                        return $"active {element.IsActive} on {path}";
                    default:
                        element.IsFocused = !element.IsFocused;
                        element.FocusVisible = element.IsFocused;
                        return $"focus {element.IsFocused} on {path}";
                }
            }
            case 0:
            {
                var cls = classes[random.Next(classes.Length)];
                if (element.HasClass(cls))
                    element.RemoveClass(cls);
                else
                    element.AddClass(cls);
                return $"toggle class {cls} on {path}";
            }
            case 1:
            {
                var texts = document.Body.Descendants().SelectMany(e => e.Children.OfType<UiText>()).Where(t => t.Text.Trim().Length > 0).ToList();
                if (texts.Count == 0)
                    return "no text";
                var text = texts[random.Next(texts.Count)];
                var other = texts[random.Next(texts.Count)].Text;
                text.Text = random.Next(3) == 0 ? "" : other + (random.Next(2) == 0 ? " and more words here" : "");
                return $"text of {PathOf(text.Parent!)}";
            }
            case 2:
            {
                var style = InlineStyles[random.Next(InlineStyles.Length)];
                element.SetAttribute("style", style);
                return $"style \"{style}\" on {path}";
            }
            case 3:
                if (element.Parent == document.Body)
                    return "kept a body child";
                element.Remove();
                return $"removed {path}";
            case 4:
            {
                var target = elements[random.Next(elements.Count)];
                if (target == element || target.IsDescendantOf(element) || target.Tag is "img" or "input" or "br" or "svg")
                    return "no copy";
                target.InsertBefore(Copy(element), target.Children.Count > 0 ? target.Children[random.Next(target.Children.Count)] : null);
                return $"copied {path} into {PathOf(target)}";
            }
            case 5:
            {
                var attribute = new[] { "hidden", "open", "data-state", "disabled" }[random.Next(4)];
                if (element.HasAttribute(attribute))
                    element.RemoveAttribute(attribute);
                else
                    element.SetAttribute(attribute, attribute == "data-state" ? "active" : "");
                return $"toggle {attribute} on {path}";
            }
            default:
            {
                var child = new UiElement(random.Next(2) == 0 ? "span" : "div");
                child.SetAttribute("class", classes[random.Next(classes.Length)]);
                child.AppendText("inserted text");
                element.InsertBefore(child, element.Children.Count > 0 ? element.Children[random.Next(element.Children.Count)] : null);
                return $"inserted a {child.Tag}.{child.ClassName} into {path}";
            }
        }
    }

    static string PathOf(UiElement element)
    {
        var parts = new List<string>();
        for (var e = element; e is not null; e = e.Parent)
            parts.Add(e.Tag + (e.Id is { } id ? "#" + id : "") + (e.Parent is { } p ? $"[{p.Children.ToList().IndexOf(e)}]" : ""));
        parts.Reverse();
        return string.Join(">", parts);
    }

    static UiNode Copy(UiNode node)
    {
        if (node is UiText text)
            return new UiText(text.Text);
        var element = (UiElement)node;
        var copy = new UiElement(element.Tag);
        foreach (var (name, value) in element.Attributes)
            copy.SetAttribute(name, value);
        foreach (var child in element.Children)
            copy.Append(Copy(child));
        copy.ScrollLeft = element.ScrollLeft;
        copy.ScrollTop = element.ScrollTop;
        copy.IsHovered = element.IsHovered;
        copy.IsActive = element.IsActive;
        copy.IsFocused = element.IsFocused;
        copy.FocusVisible = element.FocusVisible;
        return copy;
    }

    /// <summary>Adjacent text nodes become one, empty ones go (a fresh parse would do the same).</summary>
    internal static void Normalize(UiElement element)
    {
        UiText? previous = null;
        foreach (var child in element.Children.ToList())
        {
            if (child is UiText text)
            {
                if (text.Text.Length == 0)
                {
                    element.RemoveChild(text);
                    continue;
                }
                if (previous is not null)
                {
                    previous.Text += text.Text;
                    element.RemoveChild(text);
                    continue;
                }
                previous = text;
            }
            else
            {
                previous = null;
                Normalize((UiElement)child);
            }
        }
    }

    /// <summary>
    /// A new document with the same stylesheets and a copy of the tree. Auto scrollers keep their
    /// scrollbars from one layout to the next (as in Blink), so the fresh one starts with the
    /// scrollbars the updated one has.
    /// </summary>
    internal static UiDocument Fresh(JsonElement state, UiDocument source)
    {
        var document = GameLayoutTests.Build(state, buildTree: false);
        foreach (var (name, value) in source.Root.Attributes)
            document.Root.SetAttribute(name, value);
        foreach (var (name, value) in source.Body.Attributes)
            document.Body.SetAttribute(name, value);
        foreach (var child in source.Body.Children)
            document.Body.Append(Copy(child));
        var sourceElements = source.AllElements().ToList();
        var index = document.AllElements().Select((e, i) => (e, i)).ToDictionary(p => p.e, p => p.i);
        document.Layout.Context.ScrollbarHint = box =>
        {
            if (box == document.Layout.Viewport)
                return new Vector2(source.Layout.Viewport.VerticalScrollbar, source.Layout.Viewport.HorizontalScrollbar);
            if (box.Element is { } element && box.Pseudo == PseudoKind.None && index.TryGetValue(element, out var i) && i < sourceElements.Count && sourceElements[i].Box is { } kept)
                return new Vector2(kept.VerticalScrollbar, kept.HorizontalScrollbar);
            return null;
        };
        document.Root.ScrollLeft = source.Root.ScrollLeft;
        document.Root.ScrollTop = source.Root.ScrollTop;
        document.Body.ScrollLeft = source.Body.ScrollLeft;
        document.Body.ScrollTop = source.Body.ScrollTop;
        document.Update();
        return document;
    }

    // ---------- Comparing ----------

    internal static List<string> Compare(UiDocument incremental, UiDocument fresh)
    {
        var problems = new List<string>();
        var a = incremental.AllElements().ToList();
        var b = fresh.AllElements().ToList();
        if (a.Count != b.Count)
        {
            problems.Add($"{a.Count} elements, fresh {b.Count}");
            return problems;
        }
        for (var i = 0; i < a.Count; i++)
        {
            if (!StyleDifference.Identical(a[i].Style, b[i].Style))
                problems.Add($"style of {PathOf(a[i])}: {Differences(a[i].Style, b[i].Style)}");
            if (!StyleDifference.Identical(a[i].BeforeStyle, b[i].BeforeStyle) || !StyleDifference.Identical(a[i].AfterStyle, b[i].AfterStyle))
                problems.Add($"pseudo-element style of {PathOf(a[i])}");
        }
        var boxesA = Dump(incremental);
        var boxesB = Dump(fresh);
        for (var i = 0; i < Math.Max(boxesA.Count, boxesB.Count); i++)
        {
            var x = i < boxesA.Count ? boxesA[i] : "(none)";
            var y = i < boxesB.Count ? boxesB[i] : "(none)";
            if (x != y)
            {
                problems.Add($"box {i}:\n    updated {x}\n    fresh   {y}");
                if (problems.Count > 20)
                    break;
            }
        }
        return problems;
    }

    static string Differences(ComputedStyle? a, ComputedStyle? b)
    {
        if (a is null || b is null)
            return a is null ? "none, fresh has one" : "has one, fresh none";
        var fields = typeof(ComputedStyle).GetFields().Where(f => f.Name != nameof(ComputedStyle.Custom))
            .Where(f => !Equals(Format(f.GetValue(a)), Format(f.GetValue(b)))).Select(f => $"{f.Name} {Format(f.GetValue(a))} / {Format(f.GetValue(b))}");
        return string.Join("; ", fields.DefaultIfEmpty("custom properties"));
    }

    static string Format(object? value) => value switch
    {
        null => "null",
        string s => s,
        System.Collections.IEnumerable list => "[" + string.Join(", ", list.Cast<object?>().Select(Format)) + "]",
        _ => value.ToString() ?? "",
    };

    static string R(float v) => v.ToString("R", CultureInfo.InvariantCulture);

    static string R(float? v) => v is { } x ? R(x) : "-";

    static string NameOf(LayoutBox? box) => box is null ? "-" : box.Type + (box.Element is { } e ? " " + e.Tag + (e.Id is { } id ? "#" + id : "") : box.IsAnonymous ? " anonymous" : "") + (box.Pseudo != PseudoKind.None ? "::" + box.Pseudo : "");

    /// <summary>Every box with its geometry, in tree order.</summary>
    static List<string> Dump(UiDocument document)
    {
        var lines = new List<string>();
        void Walk(LayoutBox box, string path)
        {
            var text = new StringBuilder();
            text.Append(path).Append(' ').Append(NameOf(box));
            if (box.Type == BoxType.Text)
                text.Append(" \"").Append(box.Text.Length > 24 ? box.Text[..24] : box.Text).Append('"');
            text.Append($" at {R(box.X)},{R(box.Y)} size {R(box.Width)}x{R(box.Height)} margin {Edges(box.Margin)} border {Edges(box.Border)} padding {Edges(box.Padding)}");
            text.Append($" container {NameOf(box.Container)} relative {R(box.RelativeOffset.X)},{R(box.RelativeOffset.Y)} baselines {R(box.FirstBaseline)},{R(box.LastBaseline)}");
            text.Append($" scroll {R(box.ScrollWidth)}x{R(box.ScrollHeight)} bars {R(box.VerticalScrollbar)},{R(box.HorizontalScrollbar)}");
            text.Append($" style {box.Style.Display} {R(box.Style.FontSize)} {box.Style.Color}");
            if (box.PositionedDescendants is { } positioned)
                text.Append(" positioned [").Append(string.Join(", ", positioned.Select(NameOf))).Append(']');
            if (box.Fragments is { } fragments)
                text.Append(" fragments [").Append(string.Join(", ", fragments.Select(f => $"{R(f.Rect.X)},{R(f.Rect.Y)} {R(f.Rect.Width)}x{R(f.Rect.Height)} {f.IsFirst} {f.IsLast} {f.Line}"))).Append(']');
            lines.Add(text.ToString());
            if (box.Inline is { } inline)
            {
                lines.Add($"{path} lines {inline.Lines.Count} height {R(inline.Height)} overflow {R(inline.OverflowHeight)} right {R(inline.Right)}");
                for (var l = 0; l < inline.Lines.Count; l++)
                {
                    var line = inline.Lines[l];
                    lines.Add($"{path} line {l} top {R(line.Top)} height {R(line.Height)} baseline {R(line.Baseline)} left {R(line.Left)} width {R(line.Width)} hidden {line.Hidden}");
                    foreach (var item in line.Items)
                        lines.Add($"{path} line {l} {item.Kind} {NameOf(item.Box)} x {R(item.X)} width {R(item.Width)} baseline {R(item.Baseline)} text {item.TextStart}-{item.TextEnd} glyphs {item.Run?.Glyphs.Count ?? -1} {item.Style.Color}");
                }
            }
            if (box.Marker is { } marker)
                Walk(marker, path + "/marker");
            for (var i = 0; i < box.Children.Count; i++)
                Walk(box.Children[i], path + "/" + i);
        }
        Walk(document.Layout.Viewport, "");
        return lines;
    }

    static string Edges(Edges e) => $"{R(e.Top)},{R(e.Right)},{R(e.Bottom)},{R(e.Left)}";
}

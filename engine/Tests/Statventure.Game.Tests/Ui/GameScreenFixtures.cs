using System.Globalization;
using System.Text;
using System.Text.Json;
using Statventure.Core;
using Statventure.Game.Icons;
using Statventure.Game.Settings;
using Statventure.Tests.Infrastructure;
using Statventure.UI.Dom;
using Statventure.UI.Layout;

namespace Statventure.Game.Tests;

/// <summary>
/// A screen of the game compared with Chromium 152 (GM1-P17; Docs/Game/Foundation.md, "How it works", step 8): the game's
/// document (<see cref="GameUiHarness"/>: <c>GameUi.Load</c> on the committed packs) at <see cref="Width"/> by
/// <see cref="Height"/> CSS px in <see cref="Theme"/>, put into its state by <see cref="Setup"/>. The test run writes it out
/// as a paint fixture into <c>Reference/game-screen-fixtures.json</c> (<see cref="GameScreenFixtures.ListJson"/>: the
/// document's sheets by name, the engine's top-layer pieces' plain look, the markup with its state written as attributes,
/// the root's and the body's classes);
/// <c>Tools/Scripts/make-game-screen-references.sh</c> screenshots it in Chromium at 1 and 1.65 into
/// <c>Golden/game-screen-&lt;name&gt;@1.png</c> and <c>@1.65.png</c>, which the engine's paint of the live document must match
/// at the paint fixtures' limits (12 levels, 0.2 % of the pixels), the icons' boxes masked (step 7 tests the icons).
/// </summary>
sealed record GameScreenFixture(string Name, int Width, int Height, GameTheme Theme, Action<GameUiHarness> Setup);

/// <summary>A part's screen fixtures, in a file of its own beside its tests: every class of the test assembly implementing this is found (steps 9 and 10 add theirs).</summary>
interface IGameScreenFixtures
{
    IEnumerable<GameScreenFixture> Fixtures { get; }
}

static class GameScreenFixtures
{
    /// <summary>The device scales every fixture is compared at.</summary>
    public static readonly float[] Scales = [1, 1.65f];

    /// <summary>Every fixture of the test assembly, by name.</summary>
    public static IReadOnlyList<GameScreenFixture> All { get; } =
        [.. typeof(GameScreenFixtures).Assembly.GetTypes()
            .Where(t => typeof(IGameScreenFixtures).IsAssignableFrom(t) && t is { IsAbstract: false, IsInterface: false })
            .SelectMany(t => ((IGameScreenFixtures)Activator.CreateInstance(t)!).Fixtures)
            .OrderBy(f => f.Name, StringComparer.Ordinal)];

    /// <summary>A scale as the golden's name has it ("1", "1.65").</summary>
    public static string ScaleName(float scale) => scale.ToString(CultureInfo.InvariantCulture);

    /// <summary>The fixture list's file (beside the engine tests' goldens, as the editor's).</summary>
    public static string ListPath => Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Golden.GoldenPath("x"))!, "..", "Reference", "game-screen-fixtures.json"));

    /// <summary>The game's document in the fixture's state, at <paramref name="scale"/>.</summary>
    public static GameUiHarness Build(GameScreenFixture fixture, float scale)
    {
        var harness = new GameUiHarness(fixture.Width, fixture.Height, scale, fixture.Theme);
        fixture.Setup(harness);
        harness.Frame();
        return harness;
    }

    /// <summary>
    /// The engine's own plain look of the top layer's pieces (its user-agent sheet's rules for them, <c>UserAgentStyles</c>),
    /// which Chromium hasn't: here at no specificity (<c>:where</c>), so the game's sheets win over them as an author
    /// sheet wins over the user agent's.
    /// </summary>
    public const string TopLayerCss = """
        :where(sv-top-layer) { display: block; position: fixed; left: 0; top: 0; right: 0; bottom: 0; z-index: 2147483647; pointer-events: none; }
        :where(sv-top-layer > *) { position: absolute; }
        :where(sv-validation-bubble) {
            display: block; max-width: 300px; margin-top: 4px; padding: 6px 10px; border: 1px solid #767676; background-color: white;
            color: black; font: 13.333333px sans-serif; box-shadow: 0 2px 6px rgba(0, 0, 0, 0.3); white-space: pre-wrap; pointer-events: none;
        }
        :where(sv-validation-main, sv-validation-sub) { display: block; }
        :where(sv-validation-sub) { margin-top: 2px; }
        :where(sv-tooltip) {
            display: block; box-sizing: content-box; padding: 3px 7px 4px; border: 1px solid rgb(60, 60, 60); background-color: white;
            color: rgb(60, 60, 60); font: 12px sans-serif; white-space: pre-wrap; overflow-wrap: break-word; pointer-events: none;
        }
        :where(sv-select-popup) {
            display: block; box-sizing: border-box; overflow-y: auto; border: 1px solid #767676; background-color: Field; color: FieldText;
            pointer-events: auto; white-space: pre; cursor: default; user-select: none;
        }
        :where(sv-select-group, sv-select-group-label, sv-select-option) { display: block; }
        :where(sv-select-group-label) { padding: 4px 2px 0; font-weight: bolder; }
        :where(sv-select-option) { padding: 4px 2px; min-height: 24px; box-sizing: border-box; }
        :where(sv-select-group sv-select-option) { padding-inline-start: 20px; }
        :where(sv-select-option[highlighted]) { background-color: SelectedItem; color: SelectedItemText; }
        :where(sv-select-option[disabled], sv-select-group[disabled]) { color: GrayText; }
        """;

    /// <summary>
    /// The fixture list as PaintReference reads it once make-game-screen-references.sh has put the sheets' text in (of
    /// <paramref name="fixtures"/>, by default all): <c>css</c> the top layer's plain look, <c>sheets</c> the document's sheets
    /// in its order, which the script reads from their committed sources (<c>ui/styles/&lt;name&gt;</c> is compiled from
    /// <c>engine/Assets/UI/styles/&lt;name&gt;.css</c>) without their <c>@font-face</c> rules (PaintReference gives the game's
    /// five faces: <c>gameFonts</c>) and puts after it; <c>htmlByScale</c> the markup at a scale where it isn't 1's (the
    /// engine places the top layer's pieces in whole device pixels at each scale: a select's list, a tooltip, a bubble);
    /// the root's and the body's classes and the body's style; <c>mainFrame</c>, captured as a page's own document, as the
    /// game's is (in PaintReference's iframe the top layer and the fixed dialogs would merge into the page's layer and keep
    /// LCD text where the game's document gives them a layer of their own: Docs/Compositing-Port.md, "The iframe and the
    /// main frame"), so a fixture's size is whole device pixels at each scale (multiples of 20 px).
    /// </summary>
    public static string ListJson(IEnumerable<GameScreenFixture>? fixtures = null) => JsonSerializer.Serialize((fixtures ?? All).Select(f =>
    {
        using var harness = Build(f, 1);
        var document = harness.Document;
        var html = Html(document);
        var byScale = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var scale in Scales.Where(s => s != 1))
        {
            using var scaled = Build(f, scale);
            if (Html(scaled.Document) is var other && other != html)
                byScale[ScaleName(scale)] = other;
        }
        return new
        {
            name = f.Name, width = f.Width, height = f.Height, css = TopLayerCss, sheets = harness.Ui.Sheets, html, htmlByScale = byScale,
            htmlClass = document.Root.ClassName, bodyClass = document.Body.ClassName, bodyStyle = document.Body.GetAttribute("style") ?? "",
            gameFonts = true, mainFrame = true, scrollX = 0, scrollY = 0, windowFocused = true,
        };
    }).ToList(), new JsonSerializerOptions { WriteIndented = false });

    static readonly HashSet<string> VoidTags = ["area", "base", "br", "col", "embed", "hr", "img", "input", "link", "meta", "source", "track", "wbr"];

    /// <summary>
    /// The body's content and then the top layer's as markup Chromium shows the same way (the editor's fixtures' rule,
    /// P36): the state that isn't in the attributes written as attributes (a field's value, checked boxes and selected
    /// options, <c>data-focus</c> on the focused element and <c>data-select</c> for its selected text, scroll offsets).
    /// The top layer (the root's last child in the engine) goes at the body's end: it is fixed and above everything.
    /// </summary>
    public static string Html(UiDocument document)
    {
        var html = new StringBuilder();
        foreach (var child in document.Body.Children)
            Write(html, child, document);
        if (document.Root.ElementChildren.FirstOrDefault(e => e.Tag == "sv-top-layer") is { } top)
            Write(html, top, document);
        return html.ToString();
    }

    static void Write(StringBuilder html, UiNode node, UiDocument document)
    {
        if (node is UiText text)
        {
            html.Append(Escape(text.Text, attribute: false));
            return;
        }
        if (node is UiComment comment)
        {
            html.Append("<!--").Append(comment.Data).Append("-->");
            return;
        }
        var element = (UiElement)node;
        var attributes = new List<(string Name, string Value)>(element.Attributes.Select(a => (a.Key, a.Value)));
        void Set(string name, string? value)
        {
            attributes.RemoveAll(a => a.Name == name);
            if (value is not null)
                attributes.Add((name, value));
        }
        if (element.Tag == "input" && !element.IsCheckable)
            Set("value", element.Value);
        if (element.IsCheckable)
            Set("checked", element.Checked ? "" : null);
        if (element.Tag == "option")
            Set("selected", element.Selected ? "" : null);
        if (document.FocusedElement == element)
        {
            Set("data-focus", "");
            if (element.IsTextField && element.SelectionStart is { } start && element.SelectionEnd is { } end && end > start)
                Set("data-select", $"{start} {end}");
        }
        if (element.ScrollTop != 0)
            Set("data-scroll-top", Number(element.ScrollTop));
        if (element.ScrollLeft != 0)
            Set("data-scroll-left", Number(element.ScrollLeft));
        html.Append('<').Append(element.Tag);
        foreach (var (name, value) in attributes)
        {
            html.Append(' ').Append(name);
            if (value.Length > 0)
                html.Append("=\"").Append(Escape(value, attribute: true)).Append('"');
        }
        html.Append('>');
        if (VoidTags.Contains(element.Tag))
            return;
        if (element.Tag == "textarea")
            html.Append(Escape(element.Value, attribute: false));
        else
            foreach (var child in element.Children)
                Write(html, child, document);
        html.Append("</").Append(element.Tag).Append('>');
    }

    static string Number(float value) => value.ToString("0.###", CultureInfo.InvariantCulture);

    static string Escape(string text, bool attribute)
    {
        var escaped = text.Replace("&", "&amp;").Replace(" ", "&nbsp;");
        return attribute ? escaped.Replace("\"", "&quot;") : escaped.Replace("<", "&lt;").Replace(">", "&gt;");
    }

    /// <summary>
    /// The boxes (device pixels, a pixel's margin around) of every character of the game's icon table on the laid-out
    /// lines: its cluster's width by its line's height (GM1-7: the icons are compared on their own sheet, step 7).
    /// </summary>
    public static List<RectF> IconBoxes(UiDocument document)
    {
        if (GameIcons.Table.Count == 0)
            return [];
        return TextBoxes([document.Layout.Viewport], text =>
        {
            foreach (var rune in text.EnumerateRunes())
                if (GameIcons.Table.ContainsKey(rune.Value))
                    return true;
            return false;
        });
    }

    /// <summary>The cluster boxes on the lines of <paramref name="roots"/> and every box under them whose text <paramref name="which"/> takes.</summary>
    static List<RectF> TextBoxes(IEnumerable<LayoutBox> roots, SpanPredicate which)
    {
        var boxes = new List<RectF>();
        foreach (var root in roots.SelectMany(r => r.Descendants().Prepend(r)))
        {
            if (root.Inline is not { } inline)
                continue;
            var origin = root.ViewportRect;
            foreach (var line in inline.Lines)
                foreach (var item in line.Items)
                {
                    if (item.Kind != LineItemKind.Text || item.Box is not { } box || item.Clusters is not { } clusters)
                        continue;
                    // The clusters index the box's processed text (white space collapsed, transformed), as the painter reads it.
                    var processed = InlineLayout.TextOf.TryGetValue(box, out var p) ? p : box.Text;
                    foreach (var cluster in clusters)
                        if (which(processed.AsSpan(cluster.Start, cluster.End - cluster.Start)))
                            boxes.Add(new RectF(origin.X + cluster.X - 1, origin.Y + line.Top - 1, cluster.Width + 2, line.Height + 2));
                }
        }
        return boxes;
    }

    delegate bool SpanPredicate(ReadOnlySpan<char> text);
}

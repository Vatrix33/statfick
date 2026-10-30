using System.Numerics;
using System.Text;
using System.Text.Json;
using Statventure.Core;
using Statventure.Tests.Canvas;
using Statventure.Tests.Infrastructure;
using Statventure.UI.Dom;
using Statventure.UI.Layout;

namespace Statventure.Tests.Ui;

/// <summary>
/// The UI must lay out like the current game's browser: every fixture is laid out by the engine and
/// each element's border box and each text node's line fragments are compared with what the browser
/// measured (Golden/layout-reference.json, made by Tools/Scripts/make-layout-references.sh).
/// </summary>
public class LayoutReferenceTests
{
    public static TheoryData<string> Fixtures => new(LayoutFixtures.All.Select(f => f.Name));

    static string ReferenceDirectory => Path.Combine(Path.GetDirectoryName(Golden.GoldenPath("x"))!, "..", "Reference");

    [Fact]
    public void TheBrowserScriptIsUpToDate()
    {
        var path = Path.Combine(ReferenceDirectory, "layout-fixtures.js");
        var script = ExportScript();
        if (!File.Exists(path) || File.ReadAllText(path) != script)
            File.WriteAllText(path, script);
        Assert.Contains("statventureLayout", script);
    }

    static readonly Lazy<JsonElement> Reference = new(() =>
    {
        var path = Path.Combine(Path.GetDirectoryName(Golden.GoldenPath("x"))!, "layout-reference.json");
        if (!File.Exists(path))
            throw new FileNotFoundException("run Tools/Scripts/make-layout-references.sh to measure the fixtures in the browser", path);
        return JsonDocument.Parse(File.ReadAllText(path)).RootElement;
    });

    public static UiDocument Build(LayoutFixture fixture)
    {
        var document = new UiDocument(TestFonts.Library);
        document.SetViewport(fixture.Width, fixture.Height);
        document.AddStylesheet(LayoutFixtures.BaseCss + fixture.Css, fixture.Name);
        Markup.SetInnerHtml(document.Body, fixture.Html);
        document.Update();
        return document;
    }

    [Theory]
    [MemberData(nameof(Fixtures))]
    public void TheEngineLaysOutLikeTheBrowser(string name)
    {
        var fixture = LayoutFixtures.All.Single(f => f.Name == name);
        var reference = Reference.Value.GetProperty("fixtures").GetProperty(name);
        var document = Build(fixture);
        var problems = Compare(document, reference);
        Assert.True(problems.Count == 0, $"{name}: {problems.Count} differences\n" + string.Join("\n", problems.Take(25)));
    }

    /// <summary>How far a box may be from the browser's (STATVENTURE_LAYOUT_TOLERANCE overrides it for exploring).</summary>
    static readonly float Tolerance = float.TryParse(Environment.GetEnvironmentVariable("STATVENTURE_LAYOUT_TOLERANCE"), System.Globalization.CultureInfo.InvariantCulture, out var t) ? t : 0.5f;

    /// <summary>
    /// The differences between the engine's layout and the browser's measurements (CSS px; the
    /// engine's layout units are divided by the document's zoom).
    /// </summary>
    public static List<string> Compare(UiDocument document, JsonElement reference, Func<UiText, bool>? skipText = null)
    {
        var zoom = document.Zoom;
        static RectF Unzoom(RectF r, float zoom) => zoom == 1 ? r : new RectF(r.X / zoom, r.Y / zoom, r.Width / zoom, r.Height / zoom);
        var problems = new List<string>();
        var elements = new List<UiElement> { document.Root, document.Body };
        elements.AddRange(document.Body.Descendants());
        var expected = reference.GetProperty("elements");
        if (expected.GetArrayLength() != elements.Count)
            problems.Add($"element count {elements.Count}, browser {expected.GetArrayLength()}");
        for (var i = 0; i < Math.Min(elements.Count, expected.GetArrayLength()); i++)
        {
            var e = expected[i];
            var element = elements[i];
            // Rows the browser left out (inside svg): nothing to compare.
            if (e.ValueKind == JsonValueKind.Null)
                continue;
            var tag = e[0].GetString();
            if (tag != element.Tag)
            {
                problems.Add($"#{i}: tag {element.Tag}, browser {tag}");
                break;
            }
            var want = new RectF(e[1].GetSingle(), e[2].GetSingle(), e[3].GetSingle(), e[4].GetSingle());
            var got = Unzoom(ElementRect(element), zoom);
            // Chromium still lays out the hidden content of a closed <details> (content-visibility: hidden); the engine doesn't.
            if (InClosedDetails(element))
                continue;
            if (!Near(want, got))
                problems.Add($"#{i} {Describe(element)}: {Format(got)}, browser {Format(want)}");
            // Scroll containers: the client and scroll sizes.
            if (element.Box is { IsScrollContainer: true } box && e.GetArrayLength() > 8 && e[5].ValueKind == JsonValueKind.Number && e.GetArrayLength() == 9)
            {
                var clientWidth = e[5].GetInt32();
                var clientHeight = e[6].GetInt32();
                var scrollWidth = e[7].GetInt32();
                var scrollHeight = e[8].GetInt32();
                // (Zoomed: the DOM's whole CSS pixels.)
                var gotClient = zoom == 1 ? ((int)MathF.Round(box.PaddingBoxWidth), (int)MathF.Round(box.PaddingBoxHeight)) : (element.ClientWidth, element.ClientHeight);
                var gotScroll = zoom == 1 ? ((int)MathF.Round(box.ScrollWidth), (int)MathF.Round(box.ScrollHeight)) : (element.ScrollWidth, element.ScrollHeight);
                if (Math.Abs(gotClient.Item1 - clientWidth) > 1 || Math.Abs(gotClient.Item2 - clientHeight) > 1)
                    problems.Add($"#{i} {Describe(element)}: client {gotClient}, browser ({clientWidth}, {clientHeight})");
                if (Math.Abs(gotScroll.Item1 - scrollWidth) > 1 || Math.Abs(gotScroll.Item2 - scrollHeight) > 1)
                    problems.Add($"#{i} {Describe(element)}: scroll {gotScroll}, browser ({scrollWidth}, {scrollHeight})");
            }
        }

        // Text fragments.
        var texts = new List<UiText>();
        void Collect(UiElement element)
        {
            foreach (var child in element.Children)
            {
                if (child is UiText text)
                    texts.Add(text);
                else if (child is UiElement el)
                    Collect(el);
            }
        }
        Collect(document.Body);
        var fragments = TextFragments(document);
        var expectedTexts = reference.GetProperty("texts");
        if (expectedTexts.GetArrayLength() != texts.Count)
            problems.Add($"text node count {texts.Count}, browser {expectedTexts.GetArrayLength()}");
        for (var i = 0; i < Math.Min(texts.Count, expectedTexts.GetArrayLength()); i++)
        {
            if (expectedTexts[i].ValueKind == JsonValueKind.Null)
                continue;
            var entries = expectedTexts[i].EnumerateArray().ToList();
            // Game captures start each entry with the node's text, to keep both lists in step.
            if (entries.Count > 0 && entries[0].ValueKind == JsonValueKind.String)
            {
                var content = entries[0].GetString()!;
                entries.RemoveAt(0);
                var mine = texts[i].Text.Length > 24 ? texts[i].Text[..24] : texts[i].Text;
                if (mine != content)
                {
                    problems.Add($"text {i}: the engine has \"{Short(texts[i].Text)}\", the browser \"{Short(content)}\" (the text lists stop matching here)");
                    break;
                }
            }
            var want = entries.Select(r => new RectF(r[0].GetSingle(), r[1].GetSingle(), r[2].GetSingle(), r[3].GetSingle())).Where(r => r.Width > 0 || r.Height > 0).ToList();
            // An ellipsized line: Chromium reports the whole hidden fragment and then the truncated one.
            for (var j = want.Count - 2; j >= 0; j--)
                if (MathF.Abs(want[j].X - want[j + 1].X) < 0.01f && MathF.Abs(want[j].Y - want[j + 1].Y) < 0.01f && want[j].Width > want[j + 1].Width)
                    want.RemoveAt(j);
            if (InClosedDetails(texts[i].Parent) || skipText?.Invoke(texts[i]) == true)
                continue;
            var got = (fragments.GetValueOrDefault(texts[i]) ?? []).Select(r => Unzoom(r, zoom)).ToList();
            var label = $"text {i} \"{Short(texts[i].Text)}\"";
            if (want.Count != got.Count)
            {
                problems.Add($"{label}: {got.Count} fragments [{string.Join(", ", got.Select(Format))}], browser {want.Count} [{string.Join(", ", want.Select(Format))}]");
                continue;
            }
            for (var j = 0; j < want.Count; j++)
                if (!Near(want[j], got[j]))
                    problems.Add($"{label} line {j}: {Format(got[j])}, browser {Format(want[j])}");
        }
        return problems;
    }

    static bool InClosedDetails(UiElement? element)
    {
        for (var e = element; e?.Parent is { } parent; e = parent)
            if (parent.Tag == "details" && !parent.HasAttribute("open") && e.Tag != "summary")
                return true;
        return false;
    }

    static string Short(string text)
    {
        text = text.Replace("\n", "\\n");
        return text.Length > 24 ? text[..24] + "…" : text;
    }

    static bool Near(RectF a, RectF b) =>
        MathF.Abs(a.X - b.X) <= Tolerance && MathF.Abs(a.Y - b.Y) <= Tolerance && MathF.Abs(a.Width - b.Width) <= Tolerance && MathF.Abs(a.Height - b.Height) <= Tolerance;

    static string Format(RectF r) => $"({r.X:0.##}, {r.Y:0.##}, {r.Width:0.##}×{r.Height:0.##})";

    static string Describe(UiElement element)
    {
        var text = new StringBuilder(element.Tag);
        if (element.ClassName.Length > 0)
            text.Append('.').Append(element.ClassName.Replace(' ', '.'));
        var content = element.TextContent.Trim();
        if (content.Length > 0)
            text.Append(" \"").Append(Short(content)).Append('"');
        return text.ToString();
    }

    /// <summary>An element's border box in the viewport as getBoundingClientRect gives it (inline boxes: the union of their fragments).</summary>
    public static RectF ElementRect(UiElement element)
    {
        var box = element.Box;
        if (box is null)
            return new RectF(0, 0, 0, 0);
        if (box.Type is BoxType.Inline or BoxType.LineBreak)
        {
            if (box.Fragments is not { Count: > 0 } fragments || box.InlineRoot is not { } root)
                return new RectF(0, 0, 0, 0);
            var origin = root.ViewportPosition + box.RelativeOffset;
            var union = fragments[0].Rect;
            foreach (var fragment in fragments.Skip(1))
                union = RectF.FromEdges(MathF.Min(union.Left, fragment.Rect.Left), MathF.Min(union.Top, fragment.Rect.Top), MathF.Max(union.Right, fragment.Rect.Right), MathF.Max(union.Bottom, fragment.Rect.Bottom));
            return new RectF(origin.X + union.X, origin.Y + union.Y, union.Width, union.Height);
        }
        return box.ViewportRect;
    }

    /// <summary>Each text node's line fragments in the viewport (one rectangle per line, as a Range's client rects).</summary>
    public static Dictionary<UiText, List<RectF>> TextFragments(UiDocument document)
    {
        var result = new Dictionary<UiText, List<RectF>>();
        void Visit(LayoutBox box)
        {
            if (box.Inline is { } inline)
            {
                var origin = box.ViewportPosition;
                foreach (var line in inline.Lines)
                {
                    var lastSeparate = false;
                    foreach (var item in line.Items)
                    {
                        if (item.Kind != LineItemKind.Text || item.Box?.TextNode is not { } node)
                            continue;
                        var rect = new RectF(origin.X + item.X, origin.Y + item.Baseline - item.Ascent, item.Width, item.Ascent + item.Descent);
                        if (!result.TryGetValue(node, out var list))
                            result[node] = list = [];
                        // Pieces of one node on one line (font fallback runs) join into one rectangle.
                        // (Chromium keeps hanging spaces and preserved newlines as fragments of their own.)
                        if (list.Count > 0 && !item.Hanging && item.Width > 0 && !lastSeparate && MathF.Abs(list[^1].Y - rect.Y) < 0.01f && MathF.Abs(list[^1].Right - rect.X) < 0.01f && list[^1].Height == rect.Height)
                            list[^1] = RectF.FromEdges(list[^1].Left, list[^1].Top, rect.Right, list[^1].Bottom);
                        else
                            list.Add(rect);
                        lastSeparate = item.Hanging || item.Width == 0;
                    }
                }
            }
            foreach (var child in box.Children)
                Visit(child);
            if (box.Marker is { } marker)
                Visit(marker);
        }
        Visit(document.Layout.Viewport);
        return result;
    }

    // ---------- The browser script ----------

    public static string ExportScript()
    {
        var fixtures = LayoutFixtures.All.Select(f => new { name = f.Name, width = f.Width, height = f.Height, css = LayoutFixtures.BaseCss + f.Css, html = f.Html });
        var json = JsonSerializer.Serialize(fixtures);
        return "// Generated by Statventure.Tests (LayoutReferenceTests.ExportScript): measures the layout fixtures.\n"
            + "const STATVENTURE_LAYOUT_FIXTURES = " + json + ";\n" + """
            async function statventureLayout() {
              const wait = ms => new Promise(resolve => setTimeout(resolve, ms));
              const base = new URL(".", location.href).href;
              const fonts = `
                @font-face { font-family: Lato; src: url("${base}objects/fonts/Lato-Regular.ttf"); font-weight: 400; font-style: normal; }
                @font-face { font-family: Lato; src: url("${base}objects/fonts/Lato-Semibold.ttf"); font-weight: 500 600; font-style: normal; }
                @font-face { font-family: Lato; src: url("${base}objects/fonts/Lato-Bold.ttf"); font-weight: 700 900; font-style: normal; }
                @font-face { font-family: "STIX Two Text"; src: url("${base}objects/fonts/STIXTwoText-Regular.otf"); font-weight: 400; font-style: normal; }
                @font-face { font-family: "STIX Two Text"; src: url("${base}objects/fonts/STIXTwoText-Italic.otf"); font-weight: 400; font-style: italic; }`;
              const q = v => Math.round(v * 64) / 64;
              const out = { userAgent: navigator.userAgent, devicePixelRatio: window.devicePixelRatio, fixtures: {} };
              for (const fixture of STATVENTURE_LAYOUT_FIXTURES) {
                const frame = document.createElement("iframe");
                frame.style.cssText = `position:fixed;left:0;top:0;width:${fixture.width}px;height:${fixture.height}px;border:0;z-index:2147483647;background:#fff`;
                const loaded = new Promise(resolve => frame.onload = resolve);
                frame.srcdoc = `<!DOCTYPE html><html><head><style>${fonts}\n${fixture.css}</style></head><body>${fixture.html}</body></html>`;
                document.body.append(frame);
                await loaded;
                const doc = frame.contentDocument;
                await Promise.all([...doc.fonts].map(f => f.load().catch(() => null)));
                await doc.fonts.ready;
                await wait(30);
                const elements = [doc.documentElement, doc.body, ...doc.body.querySelectorAll("*")];
                const rects = elements.map(e => {
                  const r = e.getBoundingClientRect();
                  return [e.tagName.toLowerCase(), q(r.x), q(r.y), q(r.width), q(r.height), e.clientWidth, e.clientHeight, e.scrollWidth, e.scrollHeight];
                });
                const texts = [];
                const walker = doc.createTreeWalker(doc.body, NodeFilter.SHOW_TEXT);
                for (let node = walker.nextNode(); node; node = walker.nextNode()) {
                  const range = doc.createRange();
                  range.selectNodeContents(node);
                  texts.push([...range.getClientRects()].map(r => [q(r.x), q(r.y), q(r.width), q(r.height)]));
                }
                out.fixtures[fixture.name] = { elements: rects, texts };
                frame.remove();
              }
              throw new Error("OUT" + JSON.stringify(out));
            }
            """;
    }
}

using System.Globalization;
using System.IO.Compression;
using System.Numerics;
using System.Text.Json;
using Statventure.Tests.Canvas;
using Statventure.Tests.Infrastructure;
using Statventure.Tests.Text;
using Statventure.Text;
using Statventure.UI.Dom;
using Statventure.UI.Layout;

namespace Statventure.Tests.Ui;

/// <summary>
/// Lines with icons lay out as the old game's (GM1 step 7; Docs/Game/Foundation.md, "How it works", step 7): every
/// captured game state (<c>Golden/game-ui.json.gz</c> and <c>game-ui@1.65.json.gz</c>, the game-screen references on
/// <c>main</c>: the old captures until step 1b's recapture replaces them, then those) laid out with the game's icons
/// drawing the 28 symbols in place of the fallback fonts Chromium drew them with, compared box for box with Chromium's
/// boxes as the game-screen layout tests compare them (<see cref="LayoutReferenceTests.Compare"/>, 0.5 px). Each icon's
/// box is the glyph Chromium drew, so the screens lay out as captured; any capture there and its stylesheet copy are
/// taken as they are, the states holding icons found in their markup. The other characters keep Chromium's fallback
/// fonts (<see cref="TestFonts.NewLibraryWithSystemFallback"/>; ☾ and ＋ aren't icons).
/// </summary>
public class IconLayoutTests
{
    static readonly Lazy<FontLibrary> Fonts = new(() =>
    {
        var fonts = TestFonts.NewLibraryWithSystemFallback();
        fonts.AddIcons(IconFiles.All.Select(i => i.Glyph));
        return fonts;
    });

    static JsonElement Capture(float scale)
    {
        var name = scale == 1 ? "game-ui.json.gz" : $"game-ui@{scale.ToString(CultureInfo.InvariantCulture)}.json.gz";
        using var stream = new GZipStream(File.OpenRead(Path.Combine(Path.GetDirectoryName(Golden.GoldenPath("x"))!, name)), CompressionMode.Decompress);
        return JsonDocument.Parse(stream).RootElement.Clone();
    }

    static readonly Lazy<JsonElement> AtOne = new(() => Capture(1));
    static readonly Lazy<JsonElement> AtScale = new(() => Capture(1.65f));

    static IEnumerable<JsonElement> States(float scale) => (scale == 1 ? AtOne : AtScale).Value.GetProperty("states").EnumerateArray();

    static bool HoldsIcons(JsonElement state) => state.GetProperty("markup").GetString()!.EnumerateRunes().Any(r => Fonts.Value.IconCodePoints.Contains(r.Value));

    /// <summary>Every captured state whose markup holds an icon's character, at both scales.</summary>
    public static TheoryData<string, float> Cases
    {
        get
        {
            var data = new TheoryData<string, float>();
            foreach (var scale in new[] { 1f, 1.65f })
                foreach (var state in States(scale).Where(HoldsIcons))
                    data.Add(state.GetProperty("name").GetString()!, scale);
            return data;
        }
    }

    /// <summary>
    /// A captured state as <see cref="GameLayoutTests.Build"/> builds it (the game-screen tests' own, step 1b's), with the
    /// fonts given: the environment, the game's compiled stylesheet, the state's own styles, the freeze, the markup and
    /// the scroll positions the game had set.
    /// </summary>
    static UiDocument Build(JsonElement state, float scale, FontLibrary fonts)
    {
        var document = new UiDocument(fonts);
        document.SetZoom(scale);
        var size = state.TryGetProperty("viewport", out var viewport)
            ? new Vector2(viewport[0].GetSingle(), viewport[1].GetSingle())
            : new Vector2(state.GetProperty("width").GetSingle(), state.GetProperty("height").GetSingle());
        document.SetViewport(size.X, size.Y, state.GetProperty("reducedMotion").GetBoolean());
        var images = state.GetProperty("images").EnumerateObject().ToDictionary(p => p.Name, p => new Vector2(p.Value[0].GetSingle(), p.Value[1].GetSingle()));
        document.Layout.Context.ImageSize = src => images.TryGetValue(src, out var found) ? found : null;
        document.AddStylesheet(GameStylesheets.Compiled);
        foreach (var style in state.GetProperty("styles").EnumerateArray())
            document.AddStylesheet(style.GetString()!, "inline");
        document.AddStylesheet("*, *::before, *::after { animation: none !important; transition: none !important; transform: none !important; translate: none !important; rotate: none !important; scale: none !important; }", "freeze");
        foreach (var attribute in state.GetProperty("html").EnumerateObject())
            document.Root.SetAttribute(attribute.Name, attribute.Value.GetString());
        foreach (var attribute in state.GetProperty("body").EnumerateObject())
            document.Body.SetAttribute(attribute.Name, attribute.Value.GetString());
        Markup.SetInnerHtml(document.Body, state.GetProperty("markup").GetString()!);
        var elements = new List<UiElement> { document.Root, document.Body };
        elements.AddRange(document.Body.Descendants());
        var rows = state.GetProperty("elements");
        for (var i = 0; i < Math.Min(elements.Count, rows.GetArrayLength()); i++)
            if (rows[i].ValueKind == JsonValueKind.Array && rows[i].GetArrayLength() == 7)
            {
                elements[i].ScrollLeft = rows[i][5].GetSingle();
                elements[i].ScrollTop = rows[i][6].GetSingle();
            }
        document.Update();
        return document;
    }

    /// <summary>The icons laid out in the document's lines: (character, the run's face) for every icon glyph.</summary>
    static List<(string Character, FontFace Face)> LaidOutIcons(UiDocument document)
    {
        var icons = new List<(string, FontFace)>();
        void Visit(LayoutBox box)
        {
            if (box.Inline is { } inline)
                foreach (var line in inline.Lines)
                    foreach (var item in line.Items)
                        if (item.Run is { Font.Face: { IsIconFace: true } face } run)
                            foreach (var glyph in run.Glyphs)
                                if (face.IconOf(glyph.Glyph) is { } icon)
                                    icons.Add((char.ConvertFromUtf32(icon.CodePoint), face));
            foreach (var child in box.Children)
                Visit(child);
            if (box.Marker is { } marker)
                Visit(marker);
        }
        Visit(document.Layout.Viewport);
        return icons;
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void LinesWithIconsLayOutAsCaptured(string name, float scale)
    {
        var state = States(scale).Single(s => s.GetProperty("name").GetString() == name);
        var document = Build(state, scale, Fonts.Value);
        var icons = LaidOutIcons(document);
        Assert.True(icons.Count > 0, $"{name}: no icon laid out");
        var problems = LayoutReferenceTests.Compare(document, state);
        Assert.True(problems.Count == 0, $"{name} at {scale}: {problems.Count} differences, {icons.Count} icons laid out ({string.Concat(icons.Select(i => i.Character).Distinct())})\n" + string.Join("\n", problems.Take(40)));
    }

    /// <summary>The check reaches icons: every captured state holds some, and the sheet's characters are drawn by icon faces there, ☾ and ＋ by fonts.</summary>
    [Fact]
    public void TheCapturedStatesHoldIcons()
    {
        foreach (var scale in new[] { 1f, 1.65f })
        {
            Assert.All(States(scale), state => Assert.True(HoldsIcons(state), state.GetProperty("name").GetString()));
            var sheet = States(scale).First();
            var document = Build(sheet, scale, Fonts.Value);
            var laidOut = LaidOutIcons(document).Select(i => i.Character).ToHashSet();
            Assert.Contains("⚙", laidOut);
            Assert.Contains("◌", laidOut);
            Assert.DoesNotContain("☾", laidOut);
        }
    }
}

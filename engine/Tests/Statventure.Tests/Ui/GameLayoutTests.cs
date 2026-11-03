using System.IO.Compression;
using System.Numerics;
using System.Text.Json;
using Statventure.Tests.Canvas;
using Statventure.Tests.Infrastructure;
using Statventure.UI.Dom;
using Statventure.UI.Style;

namespace Statventure.Tests.Ui;

/// <summary>
/// The game's own screens must lay out as the game's browser lays them out: each captured state
/// (markup, environment and measured boxes, Golden/game-ui.json.gz from
/// Tools/Scripts/make-game-references.sh) is laid out with the game's stylesheet and compared box
/// for box. Transforms and animations are frozen in both, so this checks layout alone. Since M9 step 7
/// the game's stylesheet is the compiled one (<see cref="GameStylesheets.Compiled"/>), loaded from a pack
/// the UI importer made, so every test of the game's screens goes through it.
/// </summary>
public class GameLayoutTests
{
    static readonly Lazy<JsonElement> Capture = new(() =>
    {
        var path = Path.Combine(Path.GetDirectoryName(Golden.GoldenPath("x"))!, "game-ui.json.gz");
        using var stream = new GZipStream(File.OpenRead(path), CompressionMode.Decompress);
        return JsonDocument.Parse(stream).RootElement;
    });


    const string Freeze = "*, *::before, *::after { animation: none !important; transition: none !important; transform: none !important; translate: none !important; rotate: none !important; scale: none !important; }";

    public static TheoryData<string> States => new(Capture.Value.GetProperty("states").EnumerateArray().Select(s => s.GetProperty("name").GetString()!));

    [Fact]
    public void TheEngineKnowsEveryPropertyTheGameUses()
    {
        Assert.Empty(GameStylesheets.Parsed.UnknownProperties);
    }

    /// <param name="buildTree">False: only the environment and stylesheets (the caller adds the tree).</param>
    /// <param name="deviceScale">The screen's device scale the state was captured at.</param>
    /// <param name="styles">The game's stylesheet (default: the compiled one).</param>
    public static UiDocument Build(JsonElement state, bool buildTree = true, float deviceScale = 1, Stylesheet? styles = null)
    {
        var document = new UiDocument(TestFonts.LibraryWithSystemFallback);
        document.SetZoom(deviceScale);
        // The unrounded viewport when the capture has it (at a fractional scale the window's pixels aren't innerWidth × the scale).
        var size = state.TryGetProperty("viewport", out var viewport)
            ? new Vector2(viewport[0].GetSingle(), viewport[1].GetSingle())
            : new Vector2(state.GetProperty("width").GetSingle(), state.GetProperty("height").GetSingle());
        document.SetViewport(size.X, size.Y, state.GetProperty("reducedMotion").GetBoolean());
        var images = state.GetProperty("images").EnumerateObject().ToDictionary(p => p.Name, p => new Vector2(p.Value[0].GetSingle(), p.Value[1].GetSingle()));
        document.Layout.Context.ImageSize = src => images.TryGetValue(src, out var size) ? size : null;
        document.AddStylesheet(styles ?? GameStylesheets.Compiled);
        foreach (var style in state.GetProperty("styles").EnumerateArray())
            document.AddStylesheet(style.GetString()!, "inline");
        document.AddStylesheet(Freeze, "freeze");
        if (!buildTree)
            return document;
        foreach (var attribute in state.GetProperty("html").EnumerateObject())
            document.Root.SetAttribute(attribute.Name, attribute.Value.GetString());
        foreach (var attribute in state.GetProperty("body").EnumerateObject())
            document.Body.SetAttribute(attribute.Name, attribute.Value.GetString());
        Markup.SetInnerHtml(document.Body, state.GetProperty("markup").GetString()!);
        // Scroll positions the game had set.
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

    [Theory]
    [MemberData(nameof(States))]
    public void TheGameScreenLaysOutLikeTheBrowser(string name)
    {
        var state = Capture.Value.GetProperty("states").EnumerateArray().Single(s => s.GetProperty("name").GetString() == name);
        var document = Build(state);
        var problems = LayoutReferenceTests.Compare(document, state);
        var diagnostics = document.LayoutDiagnostics.Count > 0 ? "\nlayout diagnostics: " + string.Join("; ", document.LayoutDiagnostics.Take(5)) : "";
        Assert.True(problems.Count == 0, $"{name}: {problems.Count} differences{diagnostics}\n" + string.Join("\n", problems.Take(40)));
    }
}

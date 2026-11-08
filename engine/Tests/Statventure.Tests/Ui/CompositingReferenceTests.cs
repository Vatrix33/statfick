using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Statventure.Core;
using Statventure.Tests.Infrastructure;
using Statventure.UI.Layout;
using Statventure.UI.Paint;
using Statventure.UI.Paint.Compositing;

namespace Statventure.Tests.Ui;

/// <summary>
/// The game's screens must composite as Chromium 152 composites them, since that decides where text
/// keeps LCD antialiasing: the engine's layers (Blink's pending layers from the painter's paint
/// chunks) against the cc layers in Chromium's trace (Reference/scaled/game-layers@&lt;scale&gt;.json from
/// Tools/Scripts/make-layer-references.sh), layer for layer: owner, screen rect, whether its
/// contents are opaque and why it can't draw LCD text.
/// </summary>
public class CompositingReferenceTests
{
    static string GameFolder => Infrastructure.TestPaths.OldGame;

    static readonly Dictionary<float, Lazy<JsonElement>> Captures = new()
    {
        [1f] = new(() => Load("game-ui.json.gz")),
        [1.65f] = new(() => Load("game-ui@1.65.json.gz")),
    };

    static readonly Dictionary<float, Lazy<JsonElement>> References = new()
    {
        [1f] = new(() => LoadReference(1f)),
        [1.65f] = new(() => LoadReference(1.65f)),
    };

    static JsonElement Load(string file)
    {
        using var stream = new GZipStream(File.OpenRead(Path.Combine(Path.GetDirectoryName(Golden.GoldenPath("x"))!, file)), CompressionMode.Decompress);
        return JsonDocument.Parse(stream).RootElement;
    }

    static JsonElement LoadReference(float scale) =>
        JsonDocument.Parse(File.ReadAllText(Path.Combine(Path.GetDirectoryName(Golden.GoldenPath("x"))!, "..", "Reference", "scaled",
            $"game-layers@{scale.ToString(CultureInfo.InvariantCulture)}.json"))).RootElement;

    public static TheoryData<string, float> Cases
    {
        get
        {
            var data = new TheoryData<string, float>();
            foreach (var scale in new[] { 1f, 1.65f })
                foreach (var screen in References[scale].Value.EnumerateObject())
                    data.Add(screen.Name, scale);
            return data;
        }
    }

    /// <summary>A layer as the comparison sees it.</summary>
    /// <param name="AltNames">Other names Blink may give the layer (its debug name is the first chunk's id: the box whose clip scope the chunk starts in, or its first display item's).</param>
    internal sealed record Row(string Name, RectF Quad, bool Opaque, string? Lcd, string Detail, string[]? AltNames = null)
    {
        public override string ToString() =>
            $"{Name,-60} [{Quad.X:0.#},{Quad.Y:0.#} {Quad.Width:0.#}x{Quad.Height:0.#}] opaque={Opaque} lcd={Lcd ?? "-"} {Detail}";
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void TheGameScreenCompositesLikeChromium(string name, float scale)
    {
        var expected = References[scale].Value.GetProperty(name).EnumerateArray().Select(ReferenceRow).ToList();
        var actual = EngineRows(name, scale);
        var problems = Compare(expected, actual);
        Assert.True(problems.Count == 0, $"{name}@{scale}: {string.Join("\n", problems)}\n--- Chromium\n{string.Join("\n", expected)}\n--- engine\n{string.Join("\n", actual)}");
    }

    static Row ReferenceRow(JsonElement layer)
    {
        var q = layer.GetProperty("quad");
        float x0 = MathF.Min(q[0].GetSingle(), q[6].GetSingle()), y0 = MathF.Min(q[1].GetSingle(), q[3].GetSingle());
        float x1 = MathF.Max(q[2].GetSingle(), q[4].GetSingle()), y1 = MathF.Max(q[5].GetSingle(), q[7].GetSingle());
        var lcd = layer.GetProperty("lcd");
        var reasons = string.Join(",", layer.GetProperty("reasons").EnumerateArray().Select(r => r.GetString()));
        return new Row(ReferenceName(layer.GetProperty("name").GetString()!), RectF.FromEdges(x0, y0, x1, y1), layer.GetProperty("opaque").GetBoolean(),
            lcd.ValueKind == JsonValueKind.Null ? null : lcd.GetString(), reasons);
    }

    /// <summary>Blink's debug name without the layout object's class and flags: "ASIDE id='x' class='y'", "::after", "#document".</summary>
    static string ReferenceName(string name)
    {
        if (name.StartsWith("Scrolling background of ", StringComparison.Ordinal))
            name = name["Scrolling background of ".Length..];
        return Regex.Replace(name, @"^Layout\w+( \([^)]*\))? ", "");
    }

    internal static List<Row> EngineRows(string name, float scale)
    {
        var state = Captures[scale].Value.GetProperty("states").EnumerateArray().Single(s => s.GetProperty("name").GetString() == name);
        var document = GameLayoutTests.Build(state, deviceScale: scale);
        var images = state.GetProperty("images").EnumerateObject().Select(p => p.Name).ToHashSet();
        using var stand = new Statventure.Canvas.CanvasImage(new Statventure.Imaging.RgbaImage(1, 1));
        using var painter = new UiPainter(document)
        {
            // Drawn images count as drawings (their pixels don't matter here).
            ImageTexture = url => images.Contains(url) ? stand : null,
            TextResource = url => File.Exists(Path.Combine(GameFolder, url)) ? File.ReadAllText(Path.Combine(GameFolder, url)) : null,
            // The game draws its canvases (the map's terrain) before the captures.
            SurfaceTexture = element => element.Tag == "canvas" ? 0 : null,
        };
        return painter.AnalyzeLayers().Where(l => l.DrawsContent && l.Type != LayerType.ScrollHitTest).Select(EngineRow).ToList();
    }

    static Row EngineRow(PendingLayer layer)
    {
        var first = layer.Chunks[0];
        var b = layer.Bounds;
        var quad = RectF.FromEdges(MathF.Floor(b.X), MathF.Floor(b.Y), MathF.Ceiling(b.Right), MathF.Ceiling(b.Bottom));
        var name = first.Kind == ChunkKind.Scrollbar ? (first.Bounds.Height > first.Bounds.Width ? "VerticalScrollbar" : "HorizontalScrollbar") : OwnerName(first.Owner);
        var lcd = layer.Type is LayerType.Scrollbar or LayerType.Foreign ? null : layer.LcdReason;
        var detail = $"{layer.Type} chunks={layer.Chunks.Count} t={layer.State.Transform} e={layer.State.Effect} opaqueRect={R(layer.RectKnownToBeOpaque)} text={layer.HasText}/{layer.TextKnownToBeOnOpaqueBackground}";
        // COMPOSITING_CHUNKS=1 lists each layer's chunks (for working out a difference).
        if (Environment.GetEnvironmentVariable("COMPOSITING_CHUNKS") == "1")
            foreach (var chunk in layer.Chunks.Take(40))
                detail += $"\n      {chunk.Kind} {chunk.Owner} bounds={R(chunk.Bounds)} drawable={R(chunk.DrawableBounds)} opaque={R(chunk.RectKnownToBeOpaque)} bg={chunk.BackgroundColor}/{chunk.BackgroundColorArea} text={chunk.HasText}/{chunk.TextKnownToBeOnOpaqueBackground} c={chunk.State.Clip} e={chunk.State.Effect}";
        var alternatives = new[] { first.State.Clip.Owner, first.State.Transform.Owner, first.State.Effect.Owner }.Where(b => b is not null).Select(OwnerName).ToArray();
        return new Row(name, quad, layer.ContentsOpaque, lcd, detail, alternatives);
    }

    static string R(RectF r) => $"[{r.X:0.##},{r.Y:0.##} {r.Width:0.##}x{r.Height:0.##}]";

    static string OwnerName(LayoutBox? box)
    {
        if (box is null)
            return "?";
        if (box.IsViewport)
            return "#document";
        if (box.Pseudo != PseudoKind.None)
            return "::" + box.Pseudo.ToString().ToLowerInvariant();
        if (box.Element is not { } element)
            return box.Parent is { } parent ? OwnerName(parent) : "?";
        var svg = element.Tag is "svg" or "path" or "g" or "circle" or "rect" or "line" or "polygon" or "polyline" or "ellipse" or "text" or "use";
        var text = new StringBuilder(svg ? element.Tag : element.Tag.ToUpperInvariant());
        if (element.Id is { Length: > 0 } id)
            text.Append($" id='{id}'");
        if (element.GetAttribute("class") is { Length: > 0 } classes)
            text.Append($" class='{classes}'");
        return text.ToString();
    }

    static List<string> Compare(List<Row> expected, List<Row> actual)
    {
        var problems = new List<string>();
        if (expected.Count != actual.Count)
            problems.Add($"{actual.Count} layers, Chromium has {expected.Count}");
        for (var i = 0; i < Math.Min(expected.Count, actual.Count); i++)
        {
            var e = expected[i];
            var a = actual[i];
            var near = MathF.Abs(e.Quad.X - a.Quad.X) <= 1.5f && MathF.Abs(e.Quad.Y - a.Quad.Y) <= 1.5f
                && MathF.Abs(e.Quad.Right - a.Quad.Right) <= 1.5f && MathF.Abs(e.Quad.Bottom - a.Quad.Bottom) <= 1.5f;
            // Blink sometimes has no debug name for a layer's first chunk (its client recorded none, as
            // for the filtered forged weapon art's svg layer chunk in the shop): such a layer is unnamed.
            var named = e.Name == a.Name || (a.AltNames?.Contains(e.Name) ?? false) || e.Name.Length == 0;
            if (!named || !near || e.Opaque != a.Opaque || e.Lcd != a.Lcd)
                problems.Add($"layer {i}: engine {a}\n         Chromium {e}");
        }
        return problems;
    }
}

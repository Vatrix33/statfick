using System.IO.Compression;
using System.Numerics;
using System.Text.Json;
using Statventure.Graphics;
using Statventure.Imaging;
using Statventure.Tests.Infrastructure;
using Statventure.UI.Paint;
using CanvasRenderer = Statventure.Canvas.Canvas;

namespace Statventure.Tests.Ui;

/// <summary>
/// The game's screens must look as the game's browser draws them: each captured state is painted by
/// the engine and compared with Chromium's screenshot (Golden/game-*.png). Renders that differ are
/// written to Actual/ with a difference image for review.
/// </summary>
[Collection("GPU")]
public class GamePaintTests(GpuFixture gpu)
{
    static string GameFolder => Infrastructure.TestPaths.OldGame;

    static readonly Lazy<JsonElement> Capture = new(() =>
    {
        var path = Path.Combine(Path.GetDirectoryName(Golden.GoldenPath("x"))!, "game-ui.json.gz");
        using var stream = new GZipStream(File.OpenRead(path), CompressionMode.Decompress);
        return JsonDocument.Parse(stream).RootElement;
    });

    public static TheoryData<string> States => new(Capture.Value.GetProperty("states").EnumerateArray().Select(s => s.GetProperty("name").GetString()!));

    [Theory]
    [MemberData(nameof(States))]
    public void TheGameScreenPaintsLikeTheBrowser(string name)
    {
        var state = Capture.Value.GetProperty("states").EnumerateArray().Single(s => s.GetProperty("name").GetString() == name);
        var image = Render(gpu.Device, state);
        // Inline SVG art (scenery, sprites, item drawings) becomes Blender renders in the port: those
        // areas take the browser's pixels, so the comparison measures the UI around them.
        var golden = ImageFile.Load(Golden.GoldenPath("game-" + name));
        foreach (var row in state.GetProperty("elements").EnumerateArray())
        {
            if (row.ValueKind != JsonValueKind.Array || row[0].GetString() != "svg")
                continue;
            var x0 = Math.Max(0, (int)MathF.Floor(row[1].GetSingle()));
            var y0 = Math.Max(0, (int)MathF.Floor(row[2].GetSingle()));
            var x1 = Math.Min(image.Width, (int)MathF.Ceiling(row[1].GetSingle() + row[3].GetSingle()));
            var y1 = Math.Min(image.Height, (int)MathF.Ceiling(row[2].GetSingle() + row[4].GetSingle()));
            for (var y = y0; y < y1; y++)
                for (var x = x0; x < x1; x++)
                    for (var c = 0; c < 4; c++)
                        image.Pixels[(y * image.Width + x) * 4 + c] = golden.Pixels[(y * golden.Width + x) * 4 + c];
        }
        Golden.Match("game-" + name, image, tolerance: 24, maxDifferentRatio: 0.03);
    }

    public static RgbaImage Render(GpuDevice device, JsonElement state, float deviceScale = 1)
    {
        var document = GameLayoutTests.Build(state, deviceScale: deviceScale);
        var width = (int)MathF.Round(state.GetProperty("width").GetSingle() * deviceScale);
        var height = (int)MathF.Round(state.GetProperty("height").GetSingle() * deviceScale);
        var textures = new Dictionary<string, Statventure.Canvas.CanvasImage?>();
        var sizes = state.GetProperty("images").EnumerateObject().ToDictionary(p => p.Name, p => new Vector2(p.Value[0].GetSingle(), p.Value[1].GetSingle()));
        Statventure.Canvas.CanvasImage? Texture(string url)
        {
            if (textures.TryGetValue(url, out var cached))
                return cached;
            Statventure.Canvas.CanvasImage? texture = null;
            var file = Path.Combine(GameFolder, url);
            if (!url.StartsWith("data:") && !url.EndsWith(".svg") && File.Exists(file))
            {
                var pixels = ImageFile.Load(file);
                texture = new Statventure.Canvas.CanvasImage(pixels);
                sizes.TryAdd(url, new Vector2(pixels.Width, pixels.Height));
            }
            textures[url] = texture;
            return texture;
        }
        document.Layout.Context.ImageSize = url => sizes.TryGetValue(url, out var size) ? size : Texture(url) is not null ? sizes[url] : null;
        using var painter = new UiPainter(document)
        {
            ImageTexture = Texture,
            TextResource = url => File.Exists(Path.Combine(GameFolder, url)) ? File.ReadAllText(Path.Combine(GameFolder, url)) : null,
        };
        using var canvas = new CanvasRenderer(device);
        using var target = GpuImage.CreateRenderTarget(device, width, height);
        var frame = device.BeginFrame();
        canvas.Begin(width, height);
        painter.Paint(canvas);
        canvas.Render(frame, target);
        device.EndFrame(frame);
        device.WaitIdle();
        var image = new RgbaImage(width, height, target.ReadPixels());
        foreach (var texture in textures.Values)
            texture?.Dispose();
        return image;
    }
}

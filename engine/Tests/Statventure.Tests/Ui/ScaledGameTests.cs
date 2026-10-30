using System.Globalization;
using System.IO.Compression;
using System.Text.Json;
using Statventure.Imaging;
using Statventure.Tests.Infrastructure;

namespace Statventure.Tests.Ui;

/// <summary>
/// The game's screens on a screen of device scale 1.65 (this machine's), as Chromium 152 lays them
/// out and draws them there (Golden/game-ui@1.65.json.gz and game-*@1.65.png, made by
/// Tools/Scripts/make-scaled-game-references.sh): the game layout and paint checks with the zoom.
/// </summary>
[Collection("GPU")]
public class ScaledGameTests(GpuFixture gpu)
{
    const float Scale = 1.65f;

    static string ScaleName => Scale.ToString(CultureInfo.InvariantCulture);

    static readonly Lazy<JsonElement> Capture = new(() =>
    {
        var path = Path.Combine(Path.GetDirectoryName(Golden.GoldenPath("x"))!, $"game-ui@{Scale.ToString(CultureInfo.InvariantCulture)}.json.gz");
        using var stream = new GZipStream(File.OpenRead(path), CompressionMode.Decompress);
        return JsonDocument.Parse(stream).RootElement;
    });

    public static TheoryData<string> States => new(Capture.Value.GetProperty("states").EnumerateArray().Select(s => s.GetProperty("name").GetString()!));

    static JsonElement State(string name) => Capture.Value.GetProperty("states").EnumerateArray().Single(s => s.GetProperty("name").GetString() == name);

    [Theory]
    [MemberData(nameof(States))]
    public void TheGameScreenLaysOutLikeTheBrowser(string name)
    {
        var state = State(name);
        var document = GameLayoutTests.Build(state, deviceScale: Scale);
        var problems = LayoutReferenceTests.Compare(document, state);
        Assert.True(problems.Count == 0, $"{name}: {problems.Count} differences\n" + string.Join("\n", problems.Take(40)));
    }

    [Theory]
    [MemberData(nameof(States))]
    public void TheGameScreenPaintsLikeTheBrowser(string name)
    {
        var state = State(name);
        var image = GamePaintTests.Render(gpu.Device, state, Scale);
        // Inline SVG art becomes Blender renders in the port: those areas take the browser's pixels.
        var golden = ImageFile.Load(Golden.GoldenPath($"game-{name}@{ScaleName}"));
        foreach (var row in state.GetProperty("elements").EnumerateArray())
        {
            if (row.ValueKind != JsonValueKind.Array || row[0].GetString() != "svg")
                continue;
            var x0 = Math.Max(0, (int)MathF.Floor(row[1].GetSingle() * Scale));
            var y0 = Math.Max(0, (int)MathF.Floor(row[2].GetSingle() * Scale));
            var x1 = Math.Min(image.Width, (int)MathF.Ceiling((row[1].GetSingle() + row[3].GetSingle()) * Scale));
            var y1 = Math.Min(image.Height, (int)MathF.Ceiling((row[2].GetSingle() + row[4].GetSingle()) * Scale));
            for (var y = y0; y < y1; y++)
                for (var x = x0; x < x1; x++)
                    for (var c = 0; c < 4; c++)
                        image.Pixels[(y * image.Width + x) * 4 + c] = golden.Pixels[(y * golden.Width + x) * 4 + c];
        }
        Golden.Match($"game-{name}@{ScaleName}", image, tolerance: 24, maxDifferentRatio: 0.03);
    }
}

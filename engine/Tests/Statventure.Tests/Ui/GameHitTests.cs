using System.IO.Compression;
using System.Numerics;
using System.Text.Json;
using Statventure.Tests.Infrastructure;
using Statventure.UI.Dom;

namespace Statventure.Tests.Ui;

/// <summary>
/// The pointer must hit what it hits in the game's browser: on each captured screen, the element
/// at every point of a 10 px grid is compared with Chromium's document.elementFromPoint (captured
/// by Tools/Scripts/make-game-references.sh; inline SVG parts count as their svg).
/// </summary>
public class GameHitTests(Xunit.Abstractions.ITestOutputHelper output)
{
    static readonly Lazy<JsonElement> Capture = new(() =>
    {
        var path = Path.Combine(Path.GetDirectoryName(Golden.GoldenPath("x"))!, "game-ui.json.gz");
        using var stream = new GZipStream(File.OpenRead(path), CompressionMode.Decompress);
        return JsonDocument.Parse(stream).RootElement;
    });

    public static TheoryData<string> States => GameLayoutTests.States;

    [Theory]
    [MemberData(nameof(States))]
    public void ThePointerHitsWhatTheBrowserHits(string name)
    {
        var state = Capture.Value.GetProperty("states").EnumerateArray().Single(s => s.GetProperty("name").GetString() == name);
        var document = GameLayoutTests.Build(state);
        var elements = document.AllElements().ToList();
        var index = elements.Select((e, i) => (e, i)).ToDictionary(p => p.e, p => p.i);
        var width = (int)state.GetProperty("width").GetSingle();
        var hits = state.GetProperty("hits").EnumerateArray().Select(h => h.GetInt32()).ToArray();
        // SVG art whose parts have transform attributes is left out: the capture's freeze sheet
        // (transform: none !important) overrides those attributes in Chromium, and the art becomes
        // Blender renders in the port (as GamePaintTests masks it).
        var masked = elements.Where(e => e.Tag == "svg" && e.Descendants().Any(d => d.HasAttribute("transform"))).Select(e => index[e]).ToHashSet();
        var skipped = 0;
        var edges = 0;
        var problems = new List<string>();
        var k = 0;
        for (var y = 5; y < (int)state.GetProperty("height").GetSingle(); y += 10)
            for (var x = 5; x < width; x += 10, k++)
            {
                var expected = hits[k];
                var hit = document.HitTest(new Vector2(x, y));
                // Parts of an inline SVG count as their svg (as in the capture).
                var element = hit?.Element;
                while (element is not null && element.Tag != "svg" && element.Parent?.Tag is not null && element.Ancestors().Any(a => a.Tag == "svg"))
                    element = element.Parent;
                var actual = element is not null ? index.GetValueOrDefault(element, -2) : -1;
                if (masked.Contains(expected) || masked.Contains(actual))
                {
                    skipped++;
                    continue;
                }
                if (actual == expected)
                    continue;
                // Where an edge of either element passes within 1.5 px, a 1/64 px layout difference
                // can decide: those points are counted, not failed.
                if (NearEdge(elements, expected, x, y) || NearEdge(elements, actual, x, y))
                {
                    edges++;
                    continue;
                }
                problems.Add($"({x}, {y}): {Describe(elements, actual)}, browser {Describe(elements, expected)}");
            }
        output.WriteLine($"{name}: {problems.Count} of {hits.Length} points differ ({edges} more on element edges, {skipped} on transformed SVG art left out)");
        Assert.True(edges <= hits.Length / 500, $"{name}: {edges} points on edges differ");
        if (Environment.GetEnvironmentVariable("STATVENTURE_HIT_DEBUG") is { } debug)
        {
            var point = debug.Split(',').Select(float.Parse).ToArray();
            foreach (var region in document.HitRegionsAt(new Vector2(point[0], point[1])))
                output.WriteLine($"  region {region.Box} rect {region.Rect} clip {region.Clip} contains {region.Contains(new Vector2(point[0], point[1]))}");
        }
        foreach (var problem in problems.Take(40))
            output.WriteLine("  " + problem);
        Assert.True(problems.Count == 0, $"{name}: {problems.Count} of {hits.Length} points hit something else\n" + string.Join("\n", problems.Take(30)));
    }

    static bool NearEdge(List<UiElement> elements, int index, int x, int y)
    {
        if (index < 0 || elements[index].Box is not { } box)
            return false;
        var rect = box.ViewportRect;
        static bool Near(float edge, float from, float to) => edge > from - 1.5f && edge < to + 1.5f;
        var inX = x + 1 > rect.Left - 1.5f && x < rect.Right + 1.5f;
        var inY = y + 1 > rect.Top - 1.5f && y < rect.Bottom + 1.5f;
        return (inY && (Near(rect.Left, x, x + 1) || Near(rect.Right, x, x + 1))) || (inX && (Near(rect.Top, y, y + 1) || Near(rect.Bottom, y, y + 1)));
    }

    static string Describe(List<UiElement> elements, int i) => i switch
    {
        -1 => "nothing",
        -2 => "(not in the list)",
        _ => $"#{i} {elements[i]}",
    };
}

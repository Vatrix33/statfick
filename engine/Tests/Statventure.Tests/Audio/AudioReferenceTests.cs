using System.IO.Compression;
using System.Text.Json;
using Statventure.Audio;
using Statventure.Tests.Infrastructure;

namespace Statventure.Tests.Audio;

/// <summary>
/// The audio fixtures' C# twins render what their JavaScript rendered in Chromium 152's
/// OfflineAudioContext (Golden/audio/&lt;name&gt;.f32.gz, from Tools/Scripts/make-audio-references.sh):
/// every sample within the fixture's float-rounding bound (Docs/Audio.md, "Method").
/// </summary>
public class AudioReferenceTests
{
    static string TestsDirectory => Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Golden.GoldenPath("x"))!, ".."));
    static string ReferenceDirectory => Path.Combine(TestsDirectory, "Reference");
    internal static string RenderPath(string name) => Path.Combine(TestsDirectory, "Golden", "audio", name + ".f32.gz");
    static string ActualPath(string name) => Path.Combine(TestsDirectory, "Actual", "audio", name + ".f32.gz");

    public static TheoryData<string> Fixtures => new(AudioFixtures.All.Select(f => f.Name));

    static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    [Fact]
    public void TheFixtureFileIsUpToDate()
    {
        var path = Path.Combine(ReferenceDirectory, "audio-fixtures.json");
        var fixtures = AudioFixtures.All.Select(f => new { f.Name, f.Channels, f.Length, f.Rate, f.Script, f.Quanta });
        var text = JsonSerializer.Serialize(fixtures, Json) + "\n";
        if (!File.Exists(path) || File.ReadAllText(path) != text)
            File.WriteAllText(path, text);
        Assert.Equal(AudioFixtures.All.Length, AudioFixtures.All.Select(f => f.Name).Distinct().Count());
    }

    [Theory]
    [MemberData(nameof(Fixtures))]
    public void TheEngineRendersAsChromiumDid(string name)
    {
        var fixture = AudioFixtures.All.Single(f => f.Name == name);
        var path = RenderPath(name);
        Assert.True(File.Exists(path), $"no Chromium render of '{name}': run Tools/Scripts/make-audio-references.sh {name}");
        var expected = Load(path, fixture.Channels, fixture.Length);
        var actual = Render(fixture);
        string? report;
        if (fixture.Tolerance == AudioFixtures.ShuffledSums)
            report = CompareWithShuffledSums(expected, fixture);
        else
        {
            var tolerance = fixture.Tolerance switch
            {
                AudioFixtures.Exact => 0,
                0 => SmallestNormal,
                var t => t,
            };
            report = Compare(expected, actual, tolerance);
        }
        if (report != null)
        {
            Save(ActualPath(name), actual);
            Assert.Fail($"{name}: {report}; the engine's render is in {ActualPath(name)}");
        }
        if (File.Exists(ActualPath(name)))
            File.Delete(ActualPath(name));
    }

    /// <summary>The smallest normal float: Chromium renders with denormals flushed to zero.</summary>
    internal const double SmallestNormal = 1.1754943508222875e-38;

    /// <summary>How many shuffled summing orders the rule renders (user decision, step 6: 4 missed sums of 5–6 terms).</summary>
    const int ShuffledOrders = 16;

    /// <summary>
    /// Step 6's rule for fixtures whose nodes sum three or more inputs (Docs/Audio.md): the engine
    /// renders the fixture with its sums in 16 shuffled orders; Chromium's render must lie inside their
    /// per-sample range, widened by their largest spread (at least the smallest normal float).
    /// </summary>
    static string? CompareWithShuffledSums(float[][] expected, AudioFixture fixture)
    {
        var renders = Enumerable.Range(1, ShuffledOrders).AsParallel().AsOrdered().Select(seed => Render(fixture, new Random(seed))).ToArray();
        double spread = SmallestNormal;
        for (var c = 0; c < expected.Length; c++)
            for (var i = 0; i < expected[c].Length; i++)
            {
                double low = renders.Min(r => r[c][i]), high = renders.Max(r => r[c][i]);
                spread = Math.Max(spread, high - low);
            }
        var over = 0;
        var worst = 0.0;
        (int Channel, int Frame)? first = null;
        for (var c = 0; c < expected.Length; c++)
            for (var i = 0; i < expected[c].Length; i++)
            {
                double low = renders.Min(r => r[c][i]), high = renders.Max(r => r[c][i]), value = expected[c][i];
                var outside = Math.Max(low - spread - value, value - high - spread);
                if (!(outside <= 0))
                {
                    over++;
                    first ??= (c, i);
                    worst = Math.Max(worst, outside);
                }
            }
        if (over == 0)
            return null;
        var (fc, fi) = first!.Value;
        return $"{over} samples lie outside the engine's {ShuffledOrders} summing orders (spread {spread:G3}) by up to {worst:G3}; first at channel {fc} frame {fi} " +
            $"(Chromium {expected[fc][fi]:G9}, engine {string.Join(" / ", renders.Select(r => r[fc][fi].ToString("G9")))})";
    }

    /// <summary>Renders the fixture's twin as the harness renders its script: built at time 0, suspended where it asked, <see cref="AudioFixture.Length"/> frames.</summary>
    internal static float[][] Render(AudioFixture fixture, Random? summingOrder = null)
    {
        var graph = new AudioGraph(fixture.Rate, fixture.Channels) { SummingOrder = summingOrder };
        var run = new AudioFixtureRun(graph);
        fixture.Build(run);
        var result = new float[fixture.Channels][];
        for (var c = 0; c < fixture.Channels; c++)
            result[c] = new float[fixture.Length];
        var bus = new AudioBus(fixture.Channels, AudioGraph.RenderQuantumFrames);
        for (var done = 0; done < fixture.Length;)
        {
            if (run.Suspends.Remove(graph.CurrentSampleFrame, out var actions))
                foreach (var action in actions)
                    action();
            if (graph.CurrentSampleFrame > 0)
                foreach (var action in run.EveryQuantum)
                    action();
            graph.RenderQuantum(bus);
            var count = Math.Min(AudioGraph.RenderQuantumFrames, fixture.Length - done);
            for (var c = 0; c < fixture.Channels; c++)
                bus.Channel(c).Data.AsSpan(0, count).CopyTo(result[c].AsSpan(done));
            done += count;
        }
        Assert.Empty(run.Suspends);
        return result;
    }

    /// <summary>A render file: planar little-endian float32 (channel 0's frames, then channel 1's), gzipped.</summary>
    internal static float[][] Load(string path, int channels, int length)
    {
        using var file = File.OpenRead(path);
        using var gzip = new GZipStream(file, CompressionMode.Decompress);
        using var bytes = new MemoryStream();
        gzip.CopyTo(bytes);
        var data = bytes.ToArray();
        Assert.True(data.Length == channels * length * 4, $"{path}: {data.Length} bytes, expected {channels} × {length} floats");
        var result = new float[channels][];
        for (var c = 0; c < channels; c++)
        {
            result[c] = new float[length];
            Buffer.BlockCopy(data, c * length * 4, result[c], 0, length * 4);
        }
        return result;
    }

    internal static void Save(string path, float[][] channels)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using var file = File.Create(path);
        using var gzip = new GZipStream(file, CompressionLevel.Optimal);
        foreach (var channel in channels)
        {
            var bytes = new byte[channel.Length * 4];
            Buffer.BlockCopy(channel, 0, bytes, 0, bytes.Length);
            gzip.Write(bytes);
        }
    }

    /// <summary>Null when every sample is within <paramref name="tolerance"/>, else where and how far they differ.</summary>
    internal static string? Compare(float[][] expected, float[][] actual, double tolerance)
    {
        var over = 0;
        var worst = 0.0;
        (int Channel, int Frame)? first = null, worstAt = null;
        for (var c = 0; c < expected.Length; c++)
            for (var i = 0; i < expected[c].Length; i++)
            {
                var difference = Math.Abs((double)expected[c][i] - actual[c][i]);
                if (!(difference <= tolerance))
                {
                    over++;
                    first ??= (c, i);
                }
                if (!(difference <= worst))
                {
                    worst = difference;
                    worstAt = (c, i);
                }
            }
        if (over == 0)
            return null;
        var (fc, fi) = first!.Value;
        var (wc, wi) = worstAt!.Value;
        return $"{over} samples differ by more than {tolerance:G3}; first at channel {fc} frame {fi} (Chromium {expected[fc][fi]:G9}, engine {actual[fc][fi]:G9}), "
            + $"worst {worst:G3} at channel {wc} frame {wi} (Chromium {expected[wc][wi]:G9}, engine {actual[wc][wi]:G9})";
    }
}

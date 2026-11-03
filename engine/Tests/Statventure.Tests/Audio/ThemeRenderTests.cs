using System.IO.Compression;
using System.Text.Json;
using Statventure.Audio;

namespace Statventure.Tests.Audio;

/// <summary>
/// Opt-in (STATVENTURE_THEMES=1, Tools/Scripts/test-themes.sh): every theme's offline render
/// (music.js's renderOffline) against four Chromium renders kept in the build cache by
/// Tools/Scripts/make-theme-references.sh (too big for the repo, Docs/Audio.md step 6). Both sides
/// sum in changing orders, so the engine renders each theme in four shuffled summing orders too
/// (<see cref="AudioGraph.SummingOrder"/>): at every sample the engine's range and Chromium's range
/// must come within the larger of the two sides' largest spreads of each other (user decision, after
/// the first rule, the engine's one render inside Chromium's range, failed on 6 themes by summing
/// order alone). Without the variable the theory passes at once.
/// </summary>
public class ThemeRenderTests
{
    static string Folder => Path.Combine(Environment.GetEnvironmentVariable("STATVENTURE_CACHE")
        ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".cache", "stat-fick-build"), "audio-themes");

    public static TheoryData<string> Themes => new(TestMusic.Library.Themes);

    static float[] Load(string path)
    {
        using var file = File.OpenRead(path);
        using var gzip = new GZipStream(file, CompressionMode.Decompress);
        using var bytes = new MemoryStream();
        gzip.CopyTo(bytes);
        var data = bytes.ToArray();
        var result = new float[data.Length / 4];
        Buffer.BlockCopy(data, 0, result, 0, data.Length);
        return result;
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void TheEngineRendersTheThemeAsChromiumDoes(string id)
    {
        if (Environment.GetEnvironmentVariable("STATVENTURE_THEMES") != "1")
            return;
        var index = Path.Combine(Folder, "themes.json");
        Assert.True(File.Exists(index), $"no theme references in {Folder}: run Tools/Scripts/make-theme-references.sh");
        using var document = JsonDocument.Parse(File.ReadAllText(index));
        Assert.True(document.RootElement.TryGetProperty(id, out var entry), $"no reference for {id}: run Tools/Scripts/make-theme-references.sh {id}");
        var frames = entry.GetProperty("frames"u8).GetInt32();
        var spread = Math.Max(entry.GetProperty("spread"u8).GetDouble(), AudioReferenceTests.SmallestNormal);
        var min = Load(Path.Combine(Folder, id + ".min.f32.gz"));
        var max = Load(Path.Combine(Folder, id + ".max.f32.gz"));
        Assert.Equal(Soundtrack.OfflineLength(TestMusic.Library, id, 44100), frames);
        var renders = Enumerable.Range(1, 4).AsParallel().AsOrdered().Select(seed => Soundtrack.RenderOffline(TestMusic.Library, id, 44100, new Random(seed))).ToArray();
        var engineSpread = 0.0;
        for (var c = 0; c < 2; c++)
            for (var i = 0; i < frames; i++)
            {
                double low = renders.Min(r => r[c][i]), high = renders.Max(r => r[c][i]);
                engineSpread = Math.Max(engineSpread, high - low);
            }
        var allowed = Math.Max(spread, engineSpread);
        var over = 0;
        var worst = 0.0;
        (int Channel, int Frame)? first = null;
        for (var c = 0; c < 2; c++)
            for (var i = 0; i < frames; i++)
            {
                double low = renders.Min(r => r[c][i]), high = renders.Max(r => r[c][i]);
                var gap = Math.Max(low - max[c * frames + i], min[c * frames + i] - high);
                if (!(gap <= allowed))
                {
                    over++;
                    first ??= (c, i);
                    worst = Math.Max(worst, gap);
                }
            }
        if (over > 0)
        {
            var (fc, fi) = first!.Value;
            Assert.Fail($"{id}: at {over} samples the engine's and Chromium's ranges are more than {allowed:G3} apart (Chromium's spread {spread:G3}, " +
                $"the engine's {engineSpread:G3}), by up to {worst:G3}; first at channel {fc} frame {fi} (Chromium {min[fc * frames + fi]:G9}…" +
                $"{max[fc * frames + fi]:G9}, engine {renders.Min(r => r[fc][fi]):G9}…{renders.Max(r => r[fc][fi]):G9})");
        }
    }
}

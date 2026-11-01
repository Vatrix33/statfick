using System.Text;
using System.Text.Json;
using Statventure.Audio;
using Statventure.Tests.Infrastructure;

namespace Statventure.Tests.Audio;

/// <summary>
/// The composer port against the old composer in Chromium (Reference/music-scores.json and
/// music-logs.json, from Tools/Scripts/make-music-references.sh): every theme compiles to the same
/// score (a digest of its timeline, sections, chords, notes and layers), lint finds the same, and
/// performing it calls the same Web Audio operations with the same values in the same order, step
/// by step (Docs/Audio.md, step 6).
/// </summary>
public class MusicTests
{
    static JsonElement Reference(string file)
    {
        var path = Path.Combine(Path.GetDirectoryName(Golden.GoldenPath("x"))!, "..", "Reference", file);
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        return document.RootElement.Clone();
    }

    static readonly Lazy<JsonElement> Scores = new(() => Reference("music-scores.json"));
    static readonly Lazy<JsonElement> Logs = new(() => Reference("music-logs.json"));

    public static TheoryData<string> Themes => new(MusicData.Themes.Select(t => t.Id));
    public static TheoryData<string> Performances => new(Reference("music-logs.json").EnumerateObject().Select(p => p.Name));

    sealed class Fnv
    {
        ulong _hash = 0xcbf29ce484222325;

        public Fnv Text(string text)
        {
            foreach (var b in Encoding.UTF8.GetBytes(text))
            {
                _hash ^= b;
                _hash *= 0x100000001b3;
            }
            return this;
        }

        public string Hex => _hash.ToString("x16");
        public string Low => ((uint)_hash).ToString("x8");
    }

    static string T(double value) => BitConverter.DoubleToUInt64Bits(value).ToString("x16");

    /// <summary>The compiled score as text, line for line as the harness prints the old composer's.</summary>
    internal static List<string> Digest(CompiledTheme compiled)
    {
        var lines = new List<string>
        {
            $"theme {compiled.Id} steps={T(compiled.Steps)} sub={T(compiled.Sub)} beats={T(compiled.Beats)} tonic={T(compiled.Tonic)} " +
            $"stepSeconds={T(compiled.StepSeconds)} total={T(compiled.TotalSteps)} loop={T(compiled.LoopStep)} room={compiled.Room} gain={T(compiled.Gain)} swing={T(compiled.Swing)}",
            $"scale {string.Join(",", compiled.Scale.Select(T))}",
        };
        var printed = new HashSet<CompiledSection>(ReferenceEqualityComparer.Instance);
        string NoteLine(NoteEvent e) => $"note {T(e.At)} {T(e.Length)} {T(e.Pitch)} {(e.Accent ? 1 : 0)}";
        foreach (var (section, start) in compiled.Timeline)
        {
            lines.Add($"at {T(start)} {section.Name}");
            if (!printed.Add(section))
                continue;
            lines.Add($"section {section.Name} bars={T(section.Bars)} steps={T(section.Steps)} dyn={T(section.Dynamics)} dynTo={T(section.DynamicsTo)}");
            foreach (var entry in section.Chords)
                lines.Add(entry.Chord is { } chord
                    ? $"chord {T(entry.Start)} {T(entry.Length)} {chord.Name} {T(chord.Root)} {string.Join(",", chord.Tones.Select(T))}"
                    : $"chord {T(entry.Start)} {T(entry.Length)} -");
            var chordAt = new List<string>();
            for (var i = 0; i < section.Steps; i++)
                chordAt.Add(section.ChordAt[i] is { } entry ? section.Chords.IndexOf(entry).ToString() : "-");
            lines.Add($"chordAt {string.Join(",", chordAt)}");
            lines.AddRange(section.Melody.Select(NoteLine));
            for (var index = 0; index < section.Layers.Count; index++)
            {
                var layer = section.Layers[index];
                lines.Add($"layer {index} {layer.Spec.Type} events={(layer.Events != null ? layer.Events.Count.ToString() : "-")} span={(layer.Span is { } span ? T(span) : "-")}");
                lines.AddRange((layer.Events ?? []).Select(NoteLine));
                lines.Add($"bystep {string.Join(",", layer.ByStep.Keys.Select(T))}");
            }
        }
        return lines;
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void ThemesCompileAsTheOldComposerDid(string id)
    {
        var expected = Scores.Value.GetProperty(id);
        var compiled = Score.Compile(MusicData.Themes.Single(t => t.Id == id));
        var lines = Digest(compiled);
        Assert.Equal(expected.GetProperty("lines"u8).GetInt32(), lines.Count);
        Assert.Equal(expected.GetProperty("digest"u8).GetString(), new Fnv().Text(string.Join("\n", lines) + "\n").Hex);
        Assert.Equal(expected.GetProperty("lint"u8).EnumerateArray().Select(e => e.GetString()!), Score.Lint(compiled));
    }

    [Fact]
    public void EveryThemeIsReferenced() =>
        Assert.Equal(Scores.Value.EnumerateObject().Select(p => p.Name).Where(n => n != "lintSpecs").Order(), MusicData.Themes.Select(t => t.Id).Order());

    /// <summary>
    /// Small specs (Reference/lint-specs.json) with clashes for lint and the chord forms no theme uses
    /// (sevenths of every quality, augmented, suspended and power chords, repeated bars, a trailing
    /// bar line, ties and rests): the same compiled score and the same findings, in the same order.
    /// </summary>
    [Fact]
    public void SmallSpecsCompileAndLintAsTheOldComposerDid()
    {
        var specs = Reference("lint-specs.json");
        var expected = Scores.Value.GetProperty("lintSpecs"u8);
        var findings = 0;
        foreach (var spec in specs.EnumerateArray())
        {
            var theme = ThemeJson.Parse(spec.GetRawText());
            var compiled = Score.Compile(theme);
            var reference = expected.GetProperty(theme.Id);
            var lines = Digest(compiled);
            Assert.Equal(reference.GetProperty("lines"u8).GetInt32(), lines.Count);
            Assert.Equal(reference.GetProperty("digest"u8).GetString(), new Fnv().Text(string.Join("\n", lines) + "\n").Hex);
            var issues = Score.Lint(compiled);
            findings += issues.Count;
            Assert.Equal(reference.GetProperty("lint"u8).EnumerateArray().Select(e => e.GetString()!), issues);
        }
        Assert.True(findings > 0);
    }

    /// <summary>The theme performed as renderOffline does (steps at 0.05 + n · stepSeconds) and 64 steps past its end, logged.</summary>
    internal static (List<List<string>> Segments, GraphBuilder Builder) Perform(string id, int rate)
    {
        var compiled = Score.Compile(MusicData.Themes.Single(t => t.Id == id));
        var log = new List<string>();
        var builder = new GraphBuilder(new AudioGraph(rate), log);
        var segments = new List<List<string>>();
        void Close()
        {
            segments.Add([.. log]);
            log.Clear();
        }
        var performance = new Performance(builder, compiled, builder.Graph.Destination);
        Close();
        for (var index = 0; index < compiled.TotalSteps + 64; index++)
        {
            performance.Step(index, 0.05 + index * compiled.StepSeconds);
            Close();
        }
        return (segments, builder);
    }

    [Theory]
    [MemberData(nameof(Performances))]
    public void ThemesPerformAsTheOldComposerDid(string performance)
    {
        var expected = Logs.Value.GetProperty(performance);
        var at = performance.LastIndexOf('@');
        var (segments, _) = Perform(performance[..at], int.Parse(performance[(at + 1)..]));
        var expectedSegments = expected.GetProperty("segments"u8).GetString()!.Split(' ');
        var total = new Fnv();
        for (var i = 0; i < segments.Count; i++)
        {
            var segment = new Fnv();
            foreach (var line in segments[i])
            {
                segment.Text(line + "\n");
                total.Text(line + "\n");
            }
            if (i >= expectedSegments.Length || segment.Low != expectedSegments[i])
                Assert.Fail($"{performance}: {(i == 0 ? "the setup" : $"step {i - 1}")} differs from the old composer's; the engine's calls:\n" +
                    string.Join("\n", segments[i].Take(80)));
        }
        Assert.Equal(expectedSegments.Length, segments.Count);
        Assert.Equal(expected.GetProperty("lines"u8).GetInt32(), segments.Sum(s => s.Count));
        Assert.Equal(expected.GetProperty("hash"u8).GetString(), total.Hex);
    }
}

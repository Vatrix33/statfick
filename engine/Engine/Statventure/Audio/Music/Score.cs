using System.Globalization;
using System.Text.RegularExpressions;

namespace Statventure.Audio;

/// <summary>A chord: its numeral, root in semitones above the tonic and tones above the root.</summary>
internal sealed class Chord(string name, double root, double[] tones)
{
    public string Name { get; } = name;
    public double Root { get; } = root;
    public double[] Tones { get; } = tones;
}

/// <summary>A chord's span in a section, in grid steps (a held chord "." lengthens the entry before it).</summary>
internal sealed class ChordEntry(double start, double length, Chord? chord)
{
    public double Start { get; } = start;
    public double Length { get; set; } = length;
    public Chord? Chord { get; } = chord;
}

/// <summary>A note of a melody or line: its step, length in steps, semitones above the tonic, accent.</summary>
internal sealed class NoteEvent(double at, double length, double pitch, bool accent)
{
    public double At { get; } = at;
    public double Length { get; set; } = length;
    public double Pitch { get; } = pitch;
    public bool Accent { get; } = accent;
}

/// <summary>A layer as the compiler leaves it: the spec, its notes by step and a line's span.</summary>
internal sealed class CompiledLayer(LayerSpec spec)
{
    public LayerSpec Spec { get; } = spec;
    public List<NoteEvent>? Events { get; set; }
    public Dictionary<double, List<NoteEvent>> ByStep { get; } = [];
    public double? Span { get; set; }
}

internal sealed class CompiledSection
{
    public required string Name { get; init; }
    public required double Bars { get; init; }
    public required double Steps { get; init; }
    public required List<ChordEntry> Chords { get; init; }
    public required ChordEntry?[] ChordAt { get; init; }
    public required List<NoteEvent> Melody { get; init; }
    public required List<CompiledLayer> Layers { get; init; }
    public required double Dynamics { get; init; }
    public required double DynamicsTo { get; init; }
}

internal sealed record TimelineEntry(CompiledSection Section, double Start);

/// <summary>A theme compiled into timed steps (music-composer.js's compile result).</summary>
internal sealed class CompiledTheme
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public required ThemeSpec Spec { get; init; }
    public required double[] Scale { get; init; }
    public required double Tonic { get; init; }
    /// <summary>Grid steps per bar.</summary>
    public required double Steps { get; init; }
    public required double Sub { get; init; }
    public required double Beats { get; init; }
    public required double StepSeconds { get; init; }
    public required List<TimelineEntry> Timeline { get; init; }
    public required double TotalSteps { get; init; }
    public required double LoopStep { get; init; }
    public required string Room { get; init; }
    public required double Gain { get; init; }
    public required double Swing { get; init; }
}

/// <summary>
/// music-composer.js's score side ported (Docs/Audio.md, step 6): scale degrees, chord numerals,
/// bars, chord and melody lines, sections with "like", the form and loop point; and lint, the
/// strong-beat clashes the authors check for. Numbers are doubles, as JavaScript's.
/// </summary>
internal static partial class Score
{
    public static readonly IReadOnlyDictionary<string, double[]> Modes = new Dictionary<string, double[]>
    {
        ["ionian"] = [0, 2, 4, 5, 7, 9, 11], ["dorian"] = [0, 2, 3, 5, 7, 9, 10], ["phrygian"] = [0, 1, 3, 5, 7, 8, 10],
        ["lydian"] = [0, 2, 4, 6, 7, 9, 11], ["mixolydian"] = [0, 2, 4, 5, 7, 9, 10], ["aeolian"] = [0, 2, 3, 5, 7, 8, 10],
        ["locrian"] = [0, 1, 3, 5, 6, 8, 10], ["harmonic"] = [0, 2, 3, 5, 7, 8, 11], ["hijaz"] = [0, 1, 4, 5, 7, 8, 10],
        ["hungarian"] = [0, 2, 3, 6, 7, 8, 11],
    };

    static readonly IReadOnlyDictionary<string, double[]> Qualities = new Dictionary<string, double[]>
    {
        ["M"] = [0, 4, 7], ["m"] = [0, 3, 7], ["dim"] = [0, 3, 6], ["aug"] = [0, 4, 8], ["sus2"] = [0, 2, 7], ["sus4"] = [0, 5, 7], ["5"] = [0, 7, 12],
    };

    [GeneratedRegex(@"^([b#]?)([1-7])([',]*)\z")]
    private static partial Regex DegreePattern();

    [GeneratedRegex(@"^([b#]?)([1-7])(M|m|dim|aug|sus2|sus4|5)?(7)?\z")]
    private static partial Regex ChordPattern();

    [GeneratedRegex(@"^([^/]+)/(\d+(?:\.\d+)?)(!?)\z")]
    private static partial Regex NotePattern();

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();

    /// <summary>A scale degree such as "5", "b3'" or "#7," as semitones above the tonic.</summary>
    public static double Degree(string token, double[] scale)
    {
        var match = DegreePattern().Match(token);
        if (!match.Success)
            throw new FormatException("Bad degree: " + token);
        var accidental = match.Groups[1].Value == "b" ? -1 : match.Groups[1].Value == "#" ? 1 : 0;
        double octave = 0;
        foreach (var mark in match.Groups[3].Value)
            octave += mark == '\'' ? 12 : -12;
        return scale[match.Groups[2].Value[0] - '1'] + accidental + octave;
    }

    /// <summary>A chord numeral ("1", "b6", "5M", "4m7", "1sus4"); its quality from the mode unless a suffix names it.</summary>
    public static Chord ParseChord(string token, double[] scale)
    {
        var match = ChordPattern().Match(token);
        if (!match.Success)
            throw new FormatException("Bad chord: " + token);
        var index = match.Groups[2].Value[0] - '1';
        var accidental = match.Groups[1].Value == "b" ? -1 : match.Groups[1].Value == "#" ? 1 : 0;
        var root = scale[index] + accidental;
        double Stack(int steps) => scale[(index + steps) % 7] + (index + steps >= 7 ? 12 : 0) - scale[index];
        var quality = match.Groups[3].Success ? match.Groups[3].Value : null;
        double[] tones = quality != null ? [.. Qualities[quality]] : accidental != 0 ? [0, 4, 7] : [0, Stack(2), Stack(4)];
        // A seventh follows the mode; a named quality takes the usual one (dominant, minor or diminished).
        if (match.Groups[4].Success)
            tones = [.. tones.Take(3), quality == "dim" ? 9 : quality != null || accidental != 0 ? 10 : Stack(6)];
        return new Chord(token, root, tones);
    }

    /// <summary>Bars split at "|", trimmed; an empty last bar dropped.</summary>
    internal static List<string> Bars(string text)
    {
        var all = text.Split('|').Select(bar => bar.Trim()).ToList();
        return all.Where((bar, index) => bar.Length > 0 || index < all.Count - 1).ToList();
    }

    internal static List<ChordEntry> CompileChords(string text, double[] scale, double steps)
    {
        var timeline = new List<ChordEntry>();
        Chord? previous = null;
        string[]? previousBar = null;
        var bars = Bars(text);
        for (var barIndex = 0; barIndex < bars.Count; barIndex++)
        {
            var bar = bars[barIndex];
            var tokens = bar == "%" ? previousBar! : Whitespace().Split(bar);
            previousBar = tokens;
            for (var index = 0; index < tokens.Length; index++)
            {
                var token = tokens[index];
                var current = token == "." ? previous : ParseChord(token, scale);
                var start = barIndex * steps + JsMath.Round(index * steps / tokens.Length);
                var end = barIndex * steps + JsMath.Round((index + 1) * steps / tokens.Length);
                if (timeline.Count > 0 && ReferenceEquals(timeline[^1].Chord, current) && token == ".")
                    timeline[^1].Length += end - start;
                else
                    timeline.Add(new ChordEntry(start, end - start, current));
                previous = current;
            }
        }
        return timeline;
    }

    /// <summary>A melody: "5/2 6/1 b7/1 | 1'/4", durations in grid steps, "_" rests, "-" ties, "!" accents.</summary>
    internal static List<NoteEvent> CompileLine(string text, double[] scale, double steps, string label)
    {
        var events = new List<NoteEvent>();
        var bars = Bars(text);
        for (var barIndex = 0; barIndex < bars.Count; barIndex++)
        {
            double position = 0;
            foreach (var token in Whitespace().Split(bars[barIndex]).Where(t => t.Length > 0))
            {
                var match = NotePattern().Match(token);
                if (!match.Success)
                    throw new FormatException($"Bad note \"{token}\" in {label}");
                var length = double.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
                var at = barIndex * steps + position;
                if (match.Groups[1].Value == "-")
                {
                    if (events.Count > 0)
                        events[^1].Length += length;
                }
                else if (match.Groups[1].Value != "_")
                    events.Add(new NoteEvent(at, length, Degree(match.Groups[1].Value, scale), match.Groups[3].Value.Length > 0));
                position += length;
            }
            if (Math.Abs(position - steps) > 1e-6)
                throw new FormatException($"{label}: bar {barIndex + 1} has {JsString(position)} steps, expected {JsString(steps)}");
        }
        return events;
    }

    /// <summary>JavaScript's String(number) for the numbers scores use (integers and short decimals).</summary>
    internal static string JsString(double value) =>
        value == Math.Floor(value) && Math.Abs(value) < 1e21 ? ((decimal)value).ToString(CultureInfo.InvariantCulture) : value.ToString("R", CultureInfo.InvariantCulture);

    /// <summary>Resolves sections (with "like"), the form and the loop point.</summary>
    public static CompiledTheme Compile(ThemeSpec spec)
    {
        if (!Modes.TryGetValue(spec.Mode, out var scale))
            throw new FormatException("Unknown mode " + spec.Mode);
        double beats = spec.Meter[0], sub = spec.Meter[1];
        var steps = beats * sub;
        var sections = new Dictionary<string, CompiledSection>();

        CompiledSection Resolve(string name)
        {
            if (sections.TryGetValue(name, out var done))
                return done;
            if (!spec.Sections.TryGetValue(name, out var raw))
                throw new FormatException($"{spec.Id}: missing section {name}");
            var baseSection = raw.Like != null ? spec.Sections.GetValueOrDefault(raw.Like) : null;
            // { ...base, ...raw, layers: raw.layers || base?.layers || [] }
            var chordsText = raw.Chords ?? baseSection?.Chords;
            var melodyText = raw.Melody ?? baseSection?.Melody;
            var dynamics = raw.Dynamics ?? baseSection?.Dynamics;
            var dynamicsTo = raw.DynamicsTo ?? baseSection?.DynamicsTo;
            var layerSpecs = raw.Layers ?? baseSection?.Layers ?? [];
            if (chordsText == null)
                throw new FormatException($"{spec.Id}/{name}: no chords");
            var chords = CompileChords(chordsText, scale, steps);
            var barCount = chords.Count > 0 ? JsMath.Round((chords[^1].Start + chords[^1].Length) / steps) : 0;
            var total = barCount * steps;
            var chordAt = new ChordEntry?[(int)total];
            foreach (var entry in chords)
                for (var i = entry.Start; i < entry.Start + entry.Length; i++)
                    if (i >= 0 && i < chordAt.Length)
                        chordAt[(int)i] = entry;
            var melody = melodyText != null && melodyText.Length > 0 ? CompileLine(melodyText, scale, steps, $"{spec.Id}/{name}/mel") : [];
            var layers = new List<CompiledLayer>();
            for (var index = 0; index < layerSpecs.Length; index++)
            {
                var layer = layerSpecs[index];
                var compiled = new CompiledLayer(layer);
                if (layer.Notes != null && layer.Notes.Length > 0)
                    compiled.Events = CompileLine(layer.Notes, scale, steps, $"{spec.Id}/{name}/layer{index}");
                if (layer.Type == "mel")
                    compiled.Events = melody;
                foreach (var e in compiled.Events ?? [])
                {
                    if (!compiled.ByStep.TryGetValue(e.At, out var list))
                        compiled.ByStep[e.At] = list = [];
                    list.Add(e);
                }
                if (compiled.Events != null && layer.Type == "line" && compiled.Events.Count > 0)
                    compiled.Span = Math.Ceiling(compiled.Events.Max(e => e.At + e.Length) / steps) * steps;
                layers.Add(compiled);
            }
            var section = new CompiledSection
            {
                Name = name, Bars = barCount, Steps = total, Chords = chords, ChordAt = chordAt, Melody = melody, Layers = layers,
                Dynamics = dynamics ?? 1, DynamicsTo = dynamicsTo ?? dynamics ?? 1,
            };
            sections[name] = section;
            return section;
        }

        var timeline = new List<TimelineEntry>();
        double cursor = 0;
        foreach (var name in spec.Form)
        {
            var section = Resolve(name);
            timeline.Add(new TimelineEntry(section, cursor));
            cursor += section.Steps;
        }
        var loopIndex = spec.Loop ?? 0;
        return new CompiledTheme
        {
            Id = spec.Id, Title = spec.Title, Spec = spec, Scale = scale, Tonic = spec.Tonic, Steps = steps, Sub = sub, Beats = beats,
            StepSeconds = 60 / spec.Tempo / sub, Timeline = timeline, TotalSteps = cursor, LoopStep = timeline[loopIndex].Start,
            Room = spec.Room is { Length: > 0 } room ? room : "hall", Gain = spec.Gain ?? 1, Swing = spec.Swing ?? 0,
        };
    }

    /// <summary>Strong-beat melody notes that clash with the chord, for authoring.</summary>
    public static List<string> Lint(CompiledTheme compiled)
    {
        var issues = new List<string>();
        foreach (var (section, _) in compiled.Timeline)
            foreach (var e in section.Melody)
            {
                if (e.At % compiled.Sub != 0 || e.Length < compiled.Sub)
                    continue;
                var entry = e.At >= 0 && e.At < section.ChordAt.Length && e.At == Math.Floor(e.At) ? section.ChordAt[(int)e.At] : null;
                if (entry?.Chord == null)
                    continue;
                var pc = ((e.Pitch - entry.Chord.Root) % 12 + 12) % 12;
                var tones = entry.Chord.Tones.Select(t => (t % 12 + 12) % 12).ToArray();
                if (tones.Contains(pc))
                    continue;
                var clash = tones.Any(t => Math.Abs(((pc - t + 6) % 12 + 12) % 12 - 6) == 1);
                if (clash)
                    issues.Add($"{compiled.Id}/{section.Name} bar {JsString(Math.Floor(e.At / compiled.Steps) + 1)}: degree {JsString(pc)} over {entry.Chord.Name}");
            }
        return issues.Distinct().ToList();
    }
}

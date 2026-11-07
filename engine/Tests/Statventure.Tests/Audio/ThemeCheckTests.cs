using Statventure.Audio;
using Statventure.Packs;
using Statventure.Records;
using Statventure.Tests.Packs;

namespace Statventure.Tests.Audio;

/// <summary>
/// A theme checked against the composer without playing it (M11 step 5, G5; Docs/Audio.md, "Checks and
/// marks"): every rule broken once, at the path of what it names, in the score's and the composer's words,
/// and each verdict the composer's own: a theme the check refuses fails when it is compiled and played (every
/// step of its form and past its loop point), one it passes plays. Every theme of the game passes.
/// </summary>
public class ThemeCheckTests
{
    // ---------------- A small theme and its variants ----------------

    static readonly SectionSpec A = new() { Chords = "1 | 4", Melody = "1/4 3/4 | 5/8" };

    /// <summary>A theme of two sections, B taking A's melody and layers: in dorian, 4 beats of 2 steps, in the chamber.</summary>
    static ThemeSpec Theme(params LayerSpec[] layers) => new()
    {
        Id = "probe", Title = "Probe", Tempo = 120, Meter = [4, 2], Tonic = 60, Mode = "dorian", Room = "chamber",
        Sections = new Dictionary<string, SectionSpec> { ["A"] = A with { Layers = layers }, ["B"] = new() { Chords = "5 | 1", Like = "A" } },
        Form = ["A", "B"], Loop = 0,
    };

    static ThemeSpec With(ThemeSpec theme, string section, Func<SectionSpec, SectionSpec> change) =>
        theme with { Sections = theme.Sections.ToDictionary(s => s.Key, s => s.Key == section ? change(s.Value) : s.Value) };

    static LayerSpec Layer(string type, string? instrument = null) => new() { Type = type, Instrument = instrument };

    const string Layer0 = "$.sections[\"A\"].layers[0]";

    /// <summary>The game's instruments, percussion voices and rooms, with <paramref name="change"/> made to a copy of them.</summary>
    static MusicLibrary Music(Action<Dictionary<string, InstrumentSpec>, Dictionary<string, PercussionSpec>, Dictionary<string, RoomSpec>>? change = null)
    {
        var game = TestMusic.Library;
        var instruments = new Dictionary<string, InstrumentSpec>(game.Instruments, StringComparer.Ordinal);
        var percussion = new Dictionary<string, PercussionSpec>(game.Percussion, StringComparer.Ordinal);
        var rooms = new Dictionary<string, RoomSpec>(game.Rooms, StringComparer.Ordinal);
        change?.Invoke(instruments, percussion, rooms);
        return new MusicLibrary([], instruments, percussion, rooms, new Dictionary<string, EffectPatch>());
    }

    static readonly MusicLibrary Game = Music();

    /// <summary>The library's parts as the game's data: a custom mount holding them at the music's paths.</summary>
    static VirtualFileSystem Data(MusicLibrary music)
    {
        var entries = new MemoryEntries();
        foreach (var (name, instrument) in music.Instruments)
            entries.Record(MusicNames.PathOf(MusicNames.Instruments, name), instrument);
        foreach (var (name, voice) in music.Percussion)
            entries.Record(MusicNames.PathOf(MusicNames.Percussion, name), voice);
        foreach (var (name, room) in music.Rooms)
            entries.Record(MusicNames.PathOf(MusicNames.Rooms, name), room);
        return new VirtualFileSystem([Mount.Custom("music", entries)]);
    }

    static MusicParts Parts(MusicLibrary music) => MusicParts.Of(music.Instruments, music.Percussion, music.Rooms);

    static List<string> Problems(ThemeSpec theme, MusicParts? parts) => [.. ThemeCheck.Problems(theme, parts).Select(p => p.ToString())];

    /// <summary>
    /// What the composer does with the theme: null when it compiles and every step of its form plays (twice past
    /// the loop point's section), else the exception it fails with.
    /// </summary>
    internal static Exception? Composer(ThemeSpec theme, MusicLibrary music)
    {
        try
        {
            var compiled = Score.Compile(theme);
            var builder = new GraphBuilder(new AudioGraph(8000), music);
            var performance = new Performance(builder, compiled, builder.Graph.Destination);
            var steps = compiled.TotalSteps + 2 * (compiled.TotalSteps - compiled.LoopStep);
            for (var step = 0; step < steps; step++)
                performance.Step(step, 0.05 + step * compiled.StepSeconds);
            return null;
        }
        catch (Exception problem)
        {
            return problem;
        }
    }

    /// <summary>
    /// The check's problems, each as "message (at path)"; and the same from the record's own check given the data
    /// (<see cref="RecordChecks.Check"/>, before the marks' own problems), and the composer's verdict agreeing: a
    /// refused theme fails to compile or play, in the first problem's words where the score or the composer fails
    /// with words of its own; a passed one plays.
    /// </summary>
    static List<string> Check(ThemeSpec theme, MusicLibrary? music = null)
    {
        music ??= Game;
        var problems = Problems(theme, Parts(music));
        using (var data = Data(music))
            Assert.Equal(problems, RecordChecks.Check(theme, data).Take(problems.Count).Select(p => p.ToString()));
        var composer = Composer(theme, music);
        if (problems.Count == 0)
            Assert.True(composer == null, $"the check passes it, but the composer fails: {composer}");
        else
        {
            Assert.True(composer != null, $"the check refuses it ({problems[0]}), but the composer plays it");
            if (composer is FormatException or InvalidOperationException)
                Assert.Equal(composer.Message, ThemeCheck.Problems(theme, Parts(music))[0].Message);
        }
        return problems;
    }

    // ---------------- The game's themes ----------------

    public static TheoryData<string> Themes => new(TestMusic.Library.Themes);

    /// <summary>Every theme of the game passes, against the library and against the committed pack, and the theme the check compiles is the score's.</summary>
    [Theory]
    [MemberData(nameof(Themes))]
    public void EveryThemeOfTheGamePasses(string id)
    {
        var theme = TestMusic.Library.Theme(id)!;
        Assert.Empty(ThemeCheck.Problems(theme, Parts(TestMusic.Library), out var compiled));
        Assert.Equal(MusicTests.Digest(Score.Compile(theme)), MusicTests.Digest(compiled!));
        using var data = new VirtualFileSystem([Mount.Pack(TestMusic.Pack)]);
        Assert.Empty(RecordChecks.Check(theme, data));
        Assert.Empty(RecordChecks.Check(theme));
    }

    [Fact]
    public void ASmallThemePlays()
    {
        Assert.Empty(Check(Theme(Layer("pad", "strings"), new() { Type = "drums", Drums = [new("timp", "x.x."), new("hat", "xoXr")] },
            new() { Type = "mel", Instrument = "flute", Doublings = [new() { Instrument = "oboe" }], Harmony = new() { Instrument = "fiddle" } })));
    }

    // ---------------- Names its layers play ----------------

    public static TheoryData<string, LayerSpec> InstrumentLayers => new()
    {
        { "pad", Layer("pad", "theremin") with { Pan = 0 } },
        { "pad-without-pan", Layer("pad", "theremin") },
        { "drone", Layer("drone", "theremin") },
        { "arp", Layer("arp", "theremin") with { Pattern = "135." } },
        { "bass", Layer("bass", "theremin") with { Pattern = "1" } },
        { "pulse", Layer("pulse", "theremin") with { Pattern = "1." } },
        { "strum", Layer("strum", "theremin") with { Pattern = "d.u." } },
        { "mel", Layer("mel", "theremin") },
        { "line", Layer("line", "theremin") with { Notes = "1/8 | 5/8" } },
        { "drips", Layer("drips", "theremin") with { Chance = 1 } },
    };

    [Theory]
    [MemberData(nameof(InstrumentLayers))]
    public void AnInstrumentALayerPlaysIsThere(string type, LayerSpec layer)
    {
        _ = type;
        Assert.Equal([$"Unknown instrument theremin (at {Layer0}.instrument)"], Check(Theme(layer)));
        if (layer.Type != "drips")
            Assert.Equal([$"Unknown instrument undefined (at {Layer0}.instrument)"], Check(Theme(layer with { Instrument = null })));
        // Checked alone (no data), the names aren't.
        Assert.Empty(Problems(Theme(layer), null));
        Assert.DoesNotContain(RecordChecks.Check(Theme(layer)), p => p.Path.ToString().EndsWith(".instrument", StringComparison.Ordinal));
    }

    [Fact]
    public void DripsPlayTheKalimbaWhenTheyNameNone()
    {
        var drips = Layer("drips") with { Chance = 1 };
        var noKalimba = Music((instruments, _, _) => instruments.Remove("kalimba"));
        Assert.Empty(Check(Theme(drips)));
        Assert.Empty(Check(Theme(drips with { Instrument = "" })));
        Assert.Equal([$"Unknown instrument kalimba (at {Layer0}.instrument)"], Check(Theme(drips), noKalimba));
        Assert.Equal([$"Unknown instrument kalimba (at {Layer0}.instrument)"], Check(Theme(drips with { Instrument = "" }), noKalimba));
        Assert.Empty(Check(Theme(drips with { Instrument = "harp" }), noKalimba));
    }

    [Fact]
    public void AMelodysDoublingsAndHarmonyAreThere()
    {
        Assert.Equal([$"Unknown instrument theremin (at {Layer0}.doublings[1].instrument)"],
            Check(Theme(Layer("mel", "flute") with { Doublings = [new() { Instrument = "oboe" }, new() { Instrument = "theremin" }] })));
        Assert.Equal([$"Unknown instrument undefined (at {Layer0}.doublings[0].instrument)"],
            Check(Theme(Layer("line", "flute") with { Notes = "1/8 | 5/8", Doublings = [new()] })));
        Assert.Equal([$"Unknown instrument theremin (at {Layer0}.harmony.instrument)"],
            Check(Theme(Layer("mel", "flute") with { Harmony = new() { Instrument = "theremin" } })));
        Assert.Equal([$"Unknown instrument undefined (at {Layer0}.harmony.instrument)"],
            Check(Theme(Layer("line", "flute") with { Notes = "1/8 | 5/8", Harmony = new() })));
    }

    /// <summary>An arpeggio, bass or pulse with doublings fails after its first note: the old script reads layer.dbl.i of an array.</summary>
    [Theory]
    [InlineData("arp")]
    [InlineData("bass")]
    [InlineData("pulse")]
    public void APatternLayerWithDoublingsFails(string type) =>
        Assert.Equal([$"Unknown instrument undefined (at {Layer0}.doublings)"],
            Check(Theme(Layer(type, "harp") with { Pattern = "1", Doublings = [new() { Instrument = "oboe" }] })));

    [Fact]
    public void APercussionVoiceALayerPlaysIsThere()
    {
        Assert.Equal([$"Unknown percussion cowbell (at {Layer0}.instrument)"], Check(Theme(Layer("hit", "cowbell"))));
        Assert.Equal([$"Unknown percussion undefined (at {Layer0}.instrument)"], Check(Theme(Layer("hit"))));
        Assert.Equal([$"Unknown percussion cowbell (at {Layer0}.drums[\"cowbell\"])"],
            Check(Theme(new LayerSpec { Type = "drums", Drums = [new("hat", "x.x."), new("cowbell", "..x.")] })));
        Assert.Empty(Check(Theme(new LayerSpec { Type = "drums", Drums = [] }, new() { Type = "drums" })));
        Assert.Empty(Problems(Theme(Layer("hit", "cowbell")), null));
    }

    /// <summary>A tex or drums layer plays no instrument: the composer never reads the name.</summary>
    [Fact]
    public void LayersThatPlayNoInstrumentIgnoreTheName() =>
        Assert.Empty(Check(Theme(new LayerSpec { Type = "tex", Kind = "rain", Instrument = "theremin" }, new() { Type = "drums", Instrument = "theremin", Drums = [new("hat", "x")] })));

    /// <summary>
    /// A layer is checked by what its type reads, not by whether a note of it sounds (the user's decision of 2026-11-07,
    /// accepting it): a layer that never plays (no bar of its cycle, a pattern of rests) naming an instrument that isn't
    /// there plays today, and is refused.
    /// </summary>
    [Fact]
    public void ASilentLayerNamingAnUnknownInstrumentIsRefused()
    {
        foreach (var layer in new[] { Layer("pad", "theremin") with { Bars = [] }, Layer("arp", "theremin") with { Pattern = "...." } })
        {
            var theme = Theme(layer);
            Assert.Null(Composer(theme, Game));
            Assert.Equal([$"Unknown instrument theremin (at {Layer0}.instrument)"], Problems(theme, Parts(Game)));
        }
    }

    [Fact]
    public void AnInstrumentALayerPlaysMakesASound()
    {
        var music = Music((instruments, _, _) =>
        {
            instruments["mute"] = instruments["strings"] with { Voices = [] };
            instruments["hum"] = instruments["strings"] with { Kind = "hum" };
            instruments["hush"] = instruments["choir"] with { Voices = null };
        });
        Assert.Equal([$"Instrument mute of kind bowed makes no sound (at {Layer0}.instrument)"], Check(Theme(Layer("pad", "mute")), music));
        Assert.Equal([$"Instrument hum of kind hum makes no sound (at {Layer0}.instrument)"], Check(Theme(Layer("mel", "hum")), music));
        Assert.Equal([$"Instrument hush of kind choir makes no sound (at {Layer0}.harmony.instrument)"],
            Check(Theme(Layer("mel", "flute") with { Harmony = new() { Instrument = "hush" } }), music));
    }

    /// <summary>The rule for a sound is the composer's, kind by kind: a note on each kind with no voices, with none and with one, played.</summary>
    [Fact]
    public void WhichInstrumentsSoundIsTheComposers()
    {
        var game = TestMusic.Library.Instruments;
        foreach (var kind in ComposerNames.InstrumentKinds.Append("piano"))
        {
            var template = game.Values.FirstOrDefault(i => i.Kind == kind) ?? game["strings"] with { Kind = kind };
            foreach (double[]? voices in new[] { null, [], new double[] { 0 } })
            {
                var instrument = template with { Voices = voices };
                var music = Music((instruments, _, _) => instruments["probe"] = instrument);
                var composer = Composer(Theme(Layer("mel", "probe")), music);
                Assert.True(ComposerNames.MakesSound(instrument) == (composer == null), $"{kind} with {voices?.Length.ToString() ?? "no"} voices: {composer}");
                if (composer != null)
                    Assert.Equal($"Instrument probe of kind {kind} makes no sound", composer.Message);
            }
        }
    }

    [Fact]
    public void AThemesRoomOrTheHallIsThere()
    {
        var noHall = Music((_, _, rooms) => rooms.Remove("hall"));
        Assert.Empty(Check(Theme() with { Room = "chamber" }, noHall));
        Assert.Equal(["its room 'nowhere' isn't in the data, nor is the room 'hall' the composer falls back on (at $.room)"], Check(Theme() with { Room = "nowhere" }, noHall));
        Assert.Equal(["its room 'hall' isn't in the data, nor is the room 'hall' the composer falls back on (at $.room)"], Check(Theme() with { Room = null }, noHall));
        Assert.Equal(["its room 'hall' isn't in the data, nor is the room 'hall' the composer falls back on (at $.room)"], Check(Theme() with { Room = "" }, noHall));
        // With the hall there, an unknown room sounds as the hall (the composer's fallback, as at load today).
        Assert.Empty(Check(Theme() with { Room = "nowhere" }));
        Assert.Empty(Problems(Theme() with { Room = "nowhere" }, null));
    }

    // ---------------- The layers' own rules ----------------

    [Fact]
    public void ALayersTypeIsOneTheComposerPlays()
    {
        Assert.Equal([$"Unknown layer harp (at {Layer0}.type)"], Check(Theme(Layer("harp", "harp"))));
        Assert.Equal([$"Unknown layer Pad (at {Layer0}.type)"], Check(Theme(Layer("Pad", "strings"))));
        Assert.Equal([$"Unknown layer harp (at {Layer0}.type)"], Problems(Theme(Layer("harp", "theremin")), null));
    }

    /// <summary>Every type the check knows plays, and each is the composer's: a layer of each, played.</summary>
    [Fact]
    public void TheLayerTypesAreTheComposers()
    {
        foreach (var type in ComposerNames.LayerTypes)
        {
            var layer = type switch
            {
                "hit" => Layer("hit", "gong"),
                "drums" => new LayerSpec { Type = "drums", Drums = [new("hat", "x")] },
                "tex" => new LayerSpec { Type = "tex", Kind = "rain" },
                "line" => Layer("line", "flute") with { Notes = "1/8 | 5/8" },
                _ => Layer(type, "harp") with { Pattern = ComposerNames.PatternLayers.Contains(type) ? "1d" : null },
            };
            Assert.Null(Composer(Theme(layer), Game));
            Assert.Empty(Check(Theme(layer)));
        }
        Assert.Equal("Unknown layer drip", Composer(Theme(Layer("drip", "harp")), Game)!.Message);
    }

    [Fact]
    public void ATexturesKindIsOneTheComposerPlays()
    {
        foreach (var kind in ComposerNames.Textures)
            Assert.Empty(Check(Theme(new LayerSpec { Type = "tex", Kind = kind })));
        Assert.Equal(["wind", "rain", "surf", "embers", "leaves", "drips"], ComposerNames.Textures);
        Assert.Equal([$"Unknown texture snow (at {Layer0}.kind)"], Check(Theme(new LayerSpec { Type = "tex", Kind = "snow" })));
        Assert.Equal([$"Unknown texture undefined (at {Layer0}.kind)"], Check(Theme(new LayerSpec { Type = "tex" })));
    }

    [Theory]
    [InlineData("arp")]
    [InlineData("bass")]
    [InlineData("pulse")]
    [InlineData("strum")]
    public void APatternLayerHasAPattern(string type)
    {
        Assert.Equal([$"probe: a {type} layer without a pattern (at {Layer0}.pattern)"], Check(Theme(Layer(type, "harp"))));
        Assert.Equal([$"probe: a {type} layer without a pattern (at {Layer0}.pattern)"], Check(Theme(Layer(type, "harp") with { Pattern = "" })));
    }

    [Fact]
    public void TheDegreesALayerTunesToAreScaleDegrees()
    {
        Assert.Equal([$"Bad degree: 9 (at {Layer0}.degrees[1])"], Check(Theme(Layer("drone", "drone") with { Degrees = ["1", "9", "b3'"] })));
        Assert.Equal([$"Bad degree: 0 (at {Layer0}.degree)"], Check(Theme(Layer("hit", "gong") with { Degree = "0" })));
        Assert.Empty(Check(Theme(Layer("hit", "gong") with { Degree = "#4," }, Layer("drone", "drone") with { Degrees = [] })));
    }

    /// <summary>A chord line opening with "." holds no chord there; a layer reading the chords fails on it, one that doesn't plays.</summary>
    [Fact]
    public void AHeldChordNeedsAChordBeforeIt()
    {
        var held = (LayerSpec layer) => With(Theme(layer), "A", a => a with { Chords = ". 1 | 4" });
        const string Message = "probe: a step plays a held chord with no chord before it (at $.sections[\"A\"].chords)";
        foreach (var layer in new[]
                 {
                     Layer("pad", "strings"), Layer("arp", "harp") with { Pattern = "1" }, Layer("bass", "contrabass") with { Pattern = "1" },
                     Layer("pulse", "harp") with { Pattern = "5" }, Layer("strum", "guitar") with { Pattern = "d" },
                     new LayerSpec { Type = "drums", Drums = [new("timp", "x")] }, new LayerSpec { Type = "drums", Drums = [new("hat", "x"), new("log", "x")] },
                     Layer("hit", "timp"),
                 })
            Assert.Equal([Message], Check(held(layer)));
        foreach (var layer in new[]
                 {
                     Layer("mel", "flute"), Layer("drone", "drone"), Layer("hit", "timp") with { Degree = "1" }, Layer("hit", "gong"),
                     new LayerSpec { Type = "drums", Drums = [new("hat", "x")] }, new LayerSpec { Type = "tex", Kind = "rain" },
                 })
            Assert.Empty(Check(held(layer)));
    }

    // ---------------- The score ----------------

    [Fact]
    public void TheScoresRules()
    {
        var theme = Theme(Layer("pad", "strings"));
        Assert.Equal(["Unknown mode blues (at $.mode)"], Check(theme with { Mode = "blues" }));
        Assert.Equal(["Bad chord: 9 (at $.sections[\"A\"].chords)"], Check(With(theme, "A", a => a with { Chords = "1 | 9" })));
        Assert.Equal(["Bad chord:  (at $.sections[\"A\"].chords)"], Check(With(theme, "A", a => a with { Chords = "1 || 4" })));
        Assert.Equal(["Bad note \"x\" in probe/A/mel (at $.sections[\"A\"].melody)"], Check(With(theme, "A", a => a with { Melody = "1/4 x | 5/8" })));
        Assert.Equal(["Bad degree: 8 (at $.sections[\"A\"].melody)"], Check(With(theme, "A", a => a with { Melody = "8/4 3/4 | 5/8" })));
        Assert.Equal(["probe/A/mel: bar 1 has 6 steps, expected 8 (at $.sections[\"A\"].melody)"], Check(With(theme, "A", a => a with { Melody = "1/4 3/2 | 5/8" })));
        Assert.Equal(["probe: missing section C (at $.form[2])"], Check(theme with { Form = ["A", "B", "C"] }));
        Assert.Equal(["probe/C: no chords (at $.sections[\"C\"].chords)"],
            Check(theme with { Sections = new Dictionary<string, SectionSpec>(theme.Sections) { ["C"] = new() { Melody = "1/8 | 1/8" } }, Form = ["A", "C"] }));
        Assert.Equal([$"Bad note \"5\" in probe/A/layer0 (at {Layer0}.notes)"], Check(Theme(Layer("line", "flute") with { Notes = "1/8 | 5" })));
        Assert.Equal([$"probe/A/layer0: bar 2 has 4 steps, expected 8 (at {Layer0}.notes)"], Check(Theme(Layer("line", "flute") with { Notes = "1/8 | 5/4" })));
    }

    /// <summary>What the score fails on today without words of its own: a meter of one number, an empty form, a loop point outside it, a first bar repeating none.</summary>
    [Fact]
    public void TheScoresShape()
    {
        var theme = Theme(Layer("pad", "strings"));
        Assert.Equal(["probe: the meter needs two numbers, the beats in a bar and the steps in a beat (at $.meter)"], Check(theme with { Meter = [4] }));
        Assert.Empty(Check(theme with { Meter = [4, 2, 9] }));
        Assert.Equal(["probe: the form names no section (at $.form)"], Check(theme with { Form = [] }));
        Assert.Equal(["probe: its loop point 2 isn't a place in the form (0 to 1) (at $.loop)"], Check(theme with { Loop = 2 }));
        Assert.Equal(["probe: its loop point -1 isn't a place in the form (0 to 1) (at $.loop)"], Check(theme with { Loop = -1 }));
        Assert.Empty(Check(theme with { Loop = 1 }));
        Assert.Empty(Check(theme with { Loop = null }));
        Assert.Equal(["probe/A: its first bar is \"%\", which repeats the bar before it (at $.sections[\"A\"].chords)"], Check(With(theme, "A", a => a with { Chords = "% | 1" })));
        Assert.Empty(Check(With(theme, "A", a => a with { Chords = "1 | %" })));
        // What the walk doesn't know of still fails the compile: reported for the whole theme in the compile's words.
        Assert.Equal(["Arithmetic operation resulted in an overflow."], Check(With(theme with { Meter = [-1, 4] }, "A", a => a with { Melody = null })));
    }

    /// <summary>Each problem of a broken score is reported, in the order the score meets them, before the layers'.</summary>
    [Fact]
    public void EveryProblemIsReportedInTheScoresOrder()
    {
        var theme = Theme(Layer("pad", "theremin"), Layer("tex"));
        theme = theme with
        {
            Form = ["A", "X", "B"],
            Sections = new Dictionary<string, SectionSpec>
            {
                ["A"] = theme.Sections["A"] with { Chords = "1 | 9", Melody = "1/4 | 5/8" },
                ["B"] = new() { Like = "A", Melody = "1/8 | x" },
            },
        };
        Assert.Equal(
        [
            "Bad chord: 9 (at $.sections[\"A\"].chords)",
            "probe/A/mel: bar 1 has 4 steps, expected 8 (at $.sections[\"A\"].melody)",
            "probe: missing section X (at $.form[1])",
            "Bad note \"x\" in probe/B/mel (at $.sections[\"B\"].melody)",
            $"Unknown instrument theremin (at {Layer0}.instrument)",
            $"Unknown texture undefined (at {Layer1}.kind)",
        ], Check(theme));
        // An unknown mode leaves the scale unknown: no part needing it is compiled.
        Assert.Equal(["Unknown mode x (at $.mode)", "probe: missing section X (at $.form[1])", $"Unknown instrument theremin (at {Layer0}.instrument)", $"Unknown texture undefined (at {Layer1}.kind)"],
            Check(theme with { Mode = "x" }));
        Assert.Equal(["Unknown mode x (at $.mode)"], Check(Theme(Layer("drone", "drone") with { Degrees = ["9"] }, Layer("hit", "gong") with { Degree = "9" }) with { Mode = "x" }));
    }

    const string Layer1 = "$.sections[\"A\"].layers[1]";

    /// <summary>A part a section takes from its like is reported where it is written, in the words of the section that plays it.</summary>
    [Fact]
    public void ALikesPartsAreReportedWhereTheyAreWritten()
    {
        var theme = With(Theme(Layer("pad", "theremin")), "A", a => a with { Melody = "1/4 | 5/8" }) with { Form = ["B"] };
        Assert.Equal(
        [
            "probe/B/mel: bar 1 has 4 steps, expected 8 (at $.sections[\"A\"].melody)",
            $"Unknown instrument theremin (at {Layer0}.instrument)",
        ], Check(theme));
        // B's own chords, A's that it doesn't set: each where it is written.
        Assert.Equal(["Bad chord: 9 (at $.sections[\"A\"].chords)"], Check(With(Theme(), "A", a => a with { Chords = "1 | 9" }) with { Form = ["A"] }));
        Assert.Empty(Check(With(Theme(), "A", a => a with { Chords = "1 | 9" }) with { Form = ["B"], Loop = 0 }));
        Assert.Equal(["Bad chord: 9 (at $.sections[\"A\"].chords)"],
            Check(With(With(Theme(), "A", a => a with { Chords = "1 | 9" }), "B", b => b with { Chords = null }) with { Form = ["B"] }));
        // A like naming no section takes nothing, as in the score.
        Assert.Equal(["probe/B: no chords (at $.sections[\"B\"].chords)"], Check(With(Theme(), "B", b => b with { Like = "Z", Chords = null })));
        Assert.Empty(Check(With(Theme(), "B", b => b with { Like = "Z" })));
    }

    /// <summary>A section the form doesn't play is never compiled or played: nothing in it is checked.</summary>
    [Fact]
    public void ASectionTheFormDoesntPlayIsntChecked()
    {
        var theme = Theme(Layer("pad", "strings"));
        theme = theme with
        {
            Sections = new Dictionary<string, SectionSpec>(theme.Sections)
            {
                ["Z"] = new() { Chords = "9 | %", Melody = "x", Layers = [Layer("harp", "theremin"), Layer("hit", "cowbell")] },
            },
        };
        Assert.Empty(Check(theme));
    }

    /// <summary>Checked alone, the score and the layers' own rules are; the names its layers play and its room need the data.</summary>
    [Fact]
    public void WithoutTheDataOnlyTheScoreAndTheLayersRulesAreChecked()
    {
        var theme = With(Theme(Layer("pad", "theremin"), Layer("tex", null), Layer("hit", "cowbell")), "A", a => a with { Melody = "1/4 | 5/8" }) with { Room = "nowhere" };
        Assert.Equal(["probe/A/mel: bar 1 has 4 steps, expected 8 (at $.sections[\"A\"].melody)", $"Unknown texture undefined (at {Layer1}.kind)"], Problems(theme, null));
        Assert.Equal(["probe/A/mel: bar 1 has 4 steps, expected 8 (at $.sections[\"A\"].melody)", $"Unknown texture undefined (at {Layer1}.kind)"],
            RecordChecks.Check(theme).Take(2).Select(p => p.ToString()));
    }

    /// <summary>The data's parts are read by the music's rule, an instrument once however often it is named; one that isn't an instrument is the file system's error.</summary>
    [Fact]
    public void TheDatasPartsAreReadByTheMusicsRule()
    {
        var entries = new MemoryEntries()
            .Record(MusicNames.PathOf(MusicNames.Instruments, "lowSpiccato"), TestMusic.Library.Instruments["lowSpiccato"])
            .Record(MusicNames.PathOf(MusicNames.Instruments, "glassHarp"), new RoomSpec(1, 0.5, 0.2))
            .Record(MusicNames.PathOf(MusicNames.Percussion, "bigDrum"), TestMusic.Library.Percussion["taiko"])
            .Record(MusicNames.PathOf(MusicNames.Rooms, "hall"), TestMusic.Library.Rooms["hall"]);
        using var data = new VirtualFileSystem([Mount.Custom("music", entries)]);
        var theme = Theme(Layer("pad", "lowSpiccato"), Layer("mel", "lowSpiccato"), Layer("hit", "bigDrum"), Layer("pulse", "glassHarp") with { Pattern = "1" },
            Layer("drone", "low_spiccato"), Layer("hit", "big_drum"), Layer("bass", "a b") with { Pattern = "1" });
        Assert.Equal(
        [
            "music: entry 'audio/instruments/glass_harp': a record of type audio.room, not audio.instrument (at $.sections[\"A\"].layers[3].instrument)",
            "Unknown instrument low_spiccato (at $.sections[\"A\"].layers[4].instrument)",
            "Unknown percussion big_drum (at $.sections[\"A\"].layers[5].instrument)",
            "Unknown instrument a b (at $.sections[\"A\"].layers[6].instrument)",
        ], ThemeCheck.Problems(theme, MusicParts.Of(data)).Select(p => p.ToString()));
        var reads = entries.Reads;
        ThemeCheck.Problems(Theme(Layer("pad", "lowSpiccato"), Layer("mel", "lowSpiccato"), Layer("drone", "lowSpiccato")), MusicParts.Of(data));
        Assert.Equal(reads + 1, entries.Reads);
    }
}

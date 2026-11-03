using Statventure.Audio;
using Statventure.Records;
using Statventure.Tests.Audio;

namespace Statventure.Tests.Core;

/// <summary>
/// The sound when the data reloads (Docs/Assets.md, step 9; S9-D), on the audio system's mixer without a
/// device or thread: the theme playing restarts through the switch Play makes when its music changed (its theme,
/// or any instrument, percussion voice or room, by binary form) and plays on otherwise; later plays use the new
/// library; the data's effect patches that are new or changed are registered, the others left as they are.
/// </summary>
public class AudioReloadTests
{
    const float Rate = 22050;

    static MusicLibrary Game => TestMusic.Library;

    /// <summary>A small theme on the game's instruments: a pad on the violins and a bass on the sub, in the chamber.</summary>
    static ThemeSpec Small(string id, double tempo = 120) => new()
    {
        Id = id, Title = id, Tempo = tempo, Meter = [1, 1], Tonic = 60, Mode = "dorian", Room = "chamber",
        Sections = new Dictionary<string, SectionSpec>
        {
            ["A"] = new()
            {
                Chords = "1 | 5",
                Layers =
                [
                    new() { Type = "pad", Instrument = "violins", Octave = 0, Velocity = 0.7, Voices = 2 },
                    new() { Type = "bass", Instrument = "sub", Octave = -2, Velocity = 0.7, Pattern = "1" },
                ],
            },
        },
        Form = ["A"],
    };

    static MusicLibrary With(ThemeSpec[] themes, IReadOnlyDictionary<string, InstrumentSpec>? instruments = null,
        IReadOnlyDictionary<string, PercussionSpec>? percussion = null, IReadOnlyDictionary<string, RoomSpec>? rooms = null,
        IReadOnlyDictionary<string, EffectPatch>? effects = null) =>
        new(themes, instruments ?? Game.Instruments, percussion ?? Game.Percussion, rooms ?? Game.Rooms, effects ?? new Dictionary<string, EffectPatch>());

    static Dictionary<string, T> Changed<T>(IReadOnlyDictionary<string, T> all, string name, Func<T, T> change) =>
        all.ToDictionary(p => p.Key, p => p.Key == name ? change(p.Value) : p.Value);

    /// <summary>Equal records, but other instances (read back from their binary forms).</summary>
    static Dictionary<string, T> Copy<T>(IReadOnlyDictionary<string, T> all) where T : notnull =>
        all.ToDictionary(p => p.Key, p => RecordSerializer.FromBinary<T>(RecordSerializer.ToBinary(p.Value)));

    static AudioSystem System(MusicLibrary music) => new(new AudioGraph(Rate, 2), music);

    static float[] Render(AudioSystem system, int quanta)
    {
        var bus = new AudioBus(2, AudioGraph.RenderQuantumFrames);
        var result = new List<float>();
        for (var q = 0; q < quanta; q++)
        {
            system.BeforeQuantum();
            system.Graph.RenderQuantum(bus);
            result.AddRange(bus.Channel(0).Data);
            result.AddRange(bus.Channel(1).Data);
        }
        return [.. result];
    }

    /// <summary>What the theme x sounds like with no Reload: 40 quanta, then <paramref name="quanta"/> more.</summary>
    static float[] PlayedOn(MusicLibrary music, int quanta, bool stop = false)
    {
        var system = System(music);
        system.Music.Play("x");
        Render(system, 40);
        if (stop)
            system.Music.Stop();
        return Render(system, quanta);
    }

    /// <summary>The theme x played for 40 quanta, the data reloaded as <paramref name="fresh"/>, then <paramref name="quanta"/> more.</summary>
    static (AudioSystem System, float[] After) Reloaded(MusicLibrary old, MusicLibrary fresh, int quanta, bool stop = false)
    {
        var system = System(old);
        system.Music.Play("x");
        Render(system, 40);
        if (stop)
            system.Music.Stop();
        system.Reload(fresh);
        return (system, Render(system, quanta));
    }

    [Fact]
    public void ThePlayingThemeRestartsThroughTheSwitchWhenItChanged()
    {
        var old = With([Small("x"), Small("y", 100)]);
        var changed = Small("x", 150);
        var fresh = With([changed, Small("y", 100)]);
        var (reloaded, after) = Reloaded(old, fresh, 160);
        Assert.Same(fresh, reloaded.Music.Library);
        Assert.Equal("x", reloaded.Music.Theme);
        // As if the game had stopped the theme (its bus fading out over 0.55 s) and played the new one (fading in over 0.8 s) at once.
        var expected = System(old);
        expected.Music.Play("x");
        Render(expected, 40);
        expected.Music.Stop();
        var compiled = Score.Compile(changed);
        expected.Post(() => expected.Music.Play("x", compiled));
        Assert.Equal(Render(expected, 160), after);
        Assert.NotEqual(PlayedOn(old, 160), after);
    }

    public static TheoryData<string> Unchanged => ["another theme changed", "the same music read again", "the order and the effects changed", "the theme is gone", "stopped"];

    [Theory]
    [MemberData(nameof(Unchanged))]
    public void ThePlayingThemePlaysOnWhenItsMusicIsTheSame(string how)
    {
        var old = With([Small("x"), Small("y", 100)]);
        var fresh = how switch
        {
            "another theme changed" => With([Small("x"), Small("y", 101)]),
            "the same music read again" => With([Copy(new Dictionary<string, ThemeSpec> { ["x"] = Small("x") })["x"], Small("y", 100)],
                Copy(Game.Instruments), Copy(Game.Percussion), Copy(Game.Rooms)),
            "the order and the effects changed" => With([Small("y", 100), Small("x")],
                effects: new Dictionary<string, EffectPatch> { ["chime"] = new() { Layers = [new() { Source = EffectSource.Sine, Length = 0.05 }] } }),
            "the theme is gone" => With([Small("y", 100)]),
            // Stopped before the Reload, a changed theme stays stopped.
            _ => With([Small("x", 150), Small("y", 100)]),
        };
        var stop = how == "stopped";
        var (reloaded, after) = Reloaded(old, fresh, 100, stop);
        Assert.Same(fresh, reloaded.Music.Library);
        Assert.Equal(PlayedOn(old, 100, stop), after);
        Assert.Equal(stop ? null : "x", reloaded.Music.Theme);
    }

    public static TheoryData<string> Changes => ["its theme", "an instrument it doesn't play", "an instrument added", "an instrument taken away", "a percussion voice", "a room it isn't in"];

    [Theory]
    [MemberData(nameof(Changes))]
    public void AnyInstrumentPercussionVoiceOrRoomChangedRestartsIt(string what)
    {
        var old = With([Small("x")]);
        var fresh = what switch
        {
            "its theme" => With([Small("x") with { Title = "renamed" }]),
            "an instrument it doesn't play" => With([Small("x")], Changed(Game.Instruments, "harp", i => i with { Level = i.Level * 0.5 })),
            "an instrument added" => With([Small("x")], new Dictionary<string, InstrumentSpec>(Game.Instruments) { ["harp2"] = Game.Instruments["harp"] }),
            "an instrument taken away" => With([Small("x")], Game.Instruments.Where(p => p.Key != "harp").ToDictionary()),
            "a percussion voice" => With([Small("x")], percussion: Changed(Game.Percussion, Game.Percussion.Keys.First(), p => p with { Level = p.Level * 0.5 })),
            _ => With([Small("x")], rooms: Changed(Game.Rooms, "cathedral", r => r with { Seconds = r.Seconds + 1 })),
        };
        var (reloaded, after) = Reloaded(old, fresh, 100);
        Assert.Equal("x", reloaded.Music.Theme);
        Assert.NotEqual(PlayedOn(old, 100), after);
    }

    [Fact]
    public void LaterPlaysUseTheNewLibrary()
    {
        var old = With([Small("x"), Small("y", 100)]);
        var fresh = With([Small("x"), Small("z", 90)], Changed(Game.Instruments, "violins", i => i with { Level = i.Level * 0.5 }));
        var reloaded = System(old);
        reloaded.Reload(fresh);
        Assert.Equal(["x", "z"], reloaded.Music.Themes);
        Assert.Equal("No theme 'y' (Parameter 'themeId')", Assert.Throws<ArgumentException>(() => reloaded.Music.Play("y")).Message);
        reloaded.Music.Play("x");
        var heard = Render(reloaded, 60);
        // The new instruments, as a system started on the new library plays it.
        var direct = System(fresh);
        direct.Music.Play("x");
        Assert.Equal(Render(direct, 60), heard);
        var before = System(old);
        before.Music.Play("x");
        Assert.NotEqual(Render(before, 60), heard);
        reloaded.Music.Play("z");
        Render(reloaded, 1);
        Assert.Equal("z", reloaded.Music.Theme);
    }

    static EffectPatch Tone(double frequency, double level = 1) =>
        new() { Layers = [new() { Source = EffectSource.Sine, Frequency = frequency, Length = 0.05 }], Level = level };

    [Fact]
    public void TheDatasNewAndChangedEffectPatchesAreRegistered()
    {
        var old = With([], effects: new Dictionary<string, EffectPatch> { ["chime"] = Tone(880), ["thud"] = Tone(110), ["gone"] = Tone(660) });
        var fresh = With([], effects: new Dictionary<string, EffectPatch> { ["chime"] = Tone(880, 0.5), ["thud"] = Tone(110), ["fresh"] = Tone(330) });
        var reloaded = System(old);
        // Registered in code: over a patch of the data, and one of its own.
        reloaded.Effects.Register("thud", Tone(220));
        reloaded.Effects.Register("own", Tone(440));
        reloaded.Reload(fresh);
        // The changed patch replaces the old; the unchanged one leaves the code's; one gone from the data stays; the new one plays.
        var expected = System(MusicLibrary.Empty);
        foreach (var (id, patch) in new[] { ("chime", Tone(880, 0.5)), ("thud", Tone(220)), ("gone", Tone(660)), ("fresh", Tone(330)), ("own", Tone(440)) })
            expected.Effects.Register(id, patch);
        foreach (var id in new[] { "chime", "thud", "gone", "fresh", "own" })
        {
            reloaded.Effects.Play(id);
            expected.Effects.Play(id);
        }
        var heard = Render(reloaded, 40);
        Assert.Contains(heard, s => s != 0);
        Assert.Equal(Render(expected, 40), heard);
    }

    [Fact]
    public void TheMusicIsComparedByItsRecordsBinaryForms()
    {
        var library = With([Small("x"), Small("y")]);
        Assert.True(library.SameMusic(library, "x"));
        // Other instances of equal records (whose arrays a record's own equality would compare by reference).
        var copy = With([Copy(new Dictionary<string, ThemeSpec> { ["x"] = Small("x") })["x"]], Copy(Game.Instruments), Copy(Game.Percussion), Copy(Game.Rooms));
        Assert.NotEqual(Small("x"), Small("x"));
        Assert.True(library.SameMusic(copy, "x"));
        // Other themes, the order and the effects don't count.
        Assert.True(library.SameMusic(With([Small("y", 99), Small("x")], effects: new Dictionary<string, EffectPatch> { ["a"] = Tone(1) }), "x"));
        // The theme, missing on either side, and any instrument, percussion voice or room do.
        Assert.False(library.SameMusic(With([Small("x", 121)]), "x"));
        Assert.False(library.SameMusic(With([Small("y")]), "x"));
        Assert.False(With([Small("y")]).SameMusic(library, "x"));
        Assert.False(library.SameMusic(With([Small("x")], Changed(Game.Instruments, "harp", i => i with { Cutoff = 1 })), "x"));
        Assert.False(library.SameMusic(With([Small("x")], Game.Instruments.ToDictionary(p => p.Key == "harp" ? "harp2" : p.Key, p => p.Value)), "x"));
        Assert.False(library.SameMusic(With([Small("x")], Game.Instruments.Where(p => p.Key != "harp").ToDictionary()), "x"));
        Assert.False(With([Small("x")], Game.Instruments.Where(p => p.Key != "harp").ToDictionary()).SameMusic(library, "x"));
        Assert.False(library.SameMusic(With([Small("x")], percussion: Changed(Game.Percussion, Game.Percussion.Keys.Last(), p => p with { Click = 0.9 })), "x"));
        Assert.False(library.SameMusic(With([Small("x")], rooms: Changed(Game.Rooms, "hall", r => r with { Wet = 0.9 })), "x"));
        Assert.False(library.SameMusic(With([Small("x")], rooms: Game.Rooms.Where(p => p.Key != "cathedral").ToDictionary()), "x"));
    }
}

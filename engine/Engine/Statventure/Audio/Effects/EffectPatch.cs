using Statventure.Records;

namespace Statventure.Audio;

// Sound effects as data (Docs/Audio.md, step 8): one-shot patches, layers of tones and noise bursts
// built from the same nodes as the music, the way the composer's percussion voices are. Records (M9
// step 6): entries audio/effects/<name> of audio.pak, which the audio system registers under their
// names when it starts (S6-4), or made in code and registered; M11 edits them. Marks for tools (M11
// step 5; Docs/Audio.md, "Checks and marks"): help texts, units and the limits the patch's check already
// holds (G4); its check reports the path of what it names.

/// <summary>What a layer sounds: an oscillator's wave, harmonics (a periodic wave of sine terms) or looping noise.</summary>
public enum EffectSource { Sine, Square, Sawtooth, Triangle, Harmonics, Noise }

/// <summary>In the order of the private BiquadFilterType, which a patch's filter converts to.</summary>
public enum EffectFilterType { Lowpass, Highpass, Bandpass, Lowshelf, Highshelf, Peaking, Notch, Allpass }

/// <summary>A range a patch draws from on each play.</summary>
[Record("audio.effect-range", 1)]
[Kind("Range")]
[Help("A multiplier drawn on each play, evenly between the two numbers.")]
public sealed record EffectRange(
    [property: Field(1), Help("The smallest value drawn.")] double Min,
    [property: Field(2), Help("The largest value drawn.")] double Max);

/// <summary>A layer's filter; its frequency scales with the play's pitch, and may sweep exponentially.</summary>
[Record("audio.effect-filter", 1)]
[Kind("Filter")]
[Help("A filter the layer's sound passes through.")]
public sealed record EffectFilter
{
    [Field(1)]
    [Help("What it lets through: lowpass the lows, highpass the highs, bandpass a band around its frequency, lowshelf and highshelf raise or lower the lows or highs, peaking a band, notch takes a band away, allpass only shifts the phase.")]
    public required EffectFilterType Type { get; init; }
    /// <summary>Hz at the layer's start.</summary>
    [Field(2)] [Help("Its frequency at the layer's start (the play's pitch scales it).")] [Unit("Hz")] public required double Frequency { get; init; }
    /// <summary>Hz to sweep to, if it sweeps.</summary>
    [Field(3)] [Help("If set, the frequency slides to this over the sweep time.")] [Unit("Hz")] public double? FrequencyTo { get; init; }
    /// <summary>Seconds of the sweep (0.6 of the layer's length by default, as the composer's sweeps).</summary>
    [Field(4)] [Help("How long the slide takes (0.6 of the layer's length when not set).")] [Unit("s")] public double? SweepTime { get; init; }
    [Field(5)] [Help("How sharp the filter is around its frequency (1: gentle).")] public double Q { get; init; } = 1;
    /// <summary>dB, for shelves and peaks.</summary>
    [Field(6)] [Help("For the shelves and peaking: how much the band is raised, or lowered below 0.")] [Unit("dB")] public double? Gain { get; init; }
}

/// <summary>A tone or noise burst with its own envelope: from silence to <see cref="Level"/> over <see cref="Attack"/>, then decaying over about <see cref="Length"/>.</summary>
[Record("audio.effect-layer", 1)]
[Kind("Layer")]
[Help("A tone or a burst of noise that rises to its level and fades away.")]
public sealed record EffectLayer
{
    [Field(1)] [Help("What it sounds: a wave (sine, square, sawtooth, triangle), harmonics you list, or noise.")] public required EffectSource Source { get; init; }
    /// <summary>Hz at the layer's start, scaled by the play's pitch (tones only).</summary>
    [Field(2)] [Help("The tone's pitch at the start (the play's pitch scales it); noise doesn't use it.")] [Unit("Hz")] public double Frequency { get; init; } = 440;
    /// <summary>Hz to sweep to, if it sweeps (tones only).</summary>
    [Field(3)] [Help("If set, the tone's pitch slides to this over the sweep time.")] [Unit("Hz")] public double? FrequencyTo { get; init; }
    /// <summary>Seconds of the sweep (0.6 of the length by default).</summary>
    [Field(4)] [Help("How long the slide takes (0.6 of the length when not set).")] [Unit("s")] public double? SweepTime { get; init; }
    /// <summary>The sine terms' amplitudes for <see cref="EffectSource.Harmonics"/>, from the DC term up (as the composer's organ and winds).</summary>
    [Field(5)] [Help("For harmonics: how loud each harmonic is; the first number isn't used, then the fundamental and each overtone above it.")]
    public double[]? Harmonics { get; init; }
    [Field(6)] [Help("A filter the layer passes through, if any.")] public EffectFilter? Filter { get; init; }
    /// <summary>Seconds after the play's start.</summary>
    [Field(7)] [Help("How long after the effect starts the layer begins.")] [Unit("s")] [Limits(0, double.PositiveInfinity)] public double Delay { get; init; }
    [Field(8)] [Help("How long it takes to rise to its level.")] [Unit("s")] [Limits(0, double.PositiveInfinity)] public double Attack { get; init; } = 0.002;
    [Field(9)] [Help("How loud it is at its peak.")] public double Level { get; init; } = 1;
    /// <summary>Seconds of the decay (a time constant of a quarter of it); the layer stops 50 ms after.</summary>
    [Field(10)] [Help("How long it sounds: it fades away over about this time and stops 50 ms after.")] [Unit("s")] [Limits(0, double.PositiveInfinity, MinExclusive = true)]
    public required double Length { get; init; }
}

/// <summary>A one-shot sound: its layers, level, how many voices of it may sound at once, and what varies per play.</summary>
[Record("audio.effect", 1)]
[Kind("Sound effect", MusicNames.Effects, Rule = typeof(MusicNameRule))]
[Help("A one-shot sound the game and its cutscenes play by name: layers of tones and noise that sound together.")]
public sealed record EffectPatch
{
    [Field(1)] [Help("The tones and noise bursts that sound together.")] public required EffectLayer[] Layers { get; init; }
    [Field(2)] [Help("The whole effect's loudness (1: the layers as they are).")] public double Level { get; init; } = 1;
    /// <summary>Past this many voices of the patch, the oldest fades out over 5 ms.</summary>
    [Field(3)] [Help("How many plays of it may sound at once; past that, the oldest fades out quickly.")] [Limits(1, double.PositiveInfinity)]
    public int MaxVoices { get; init; } = 4;
    /// <summary>A pitch multiplier drawn per play, if given.</summary>
    [Field(4)] [Help("If set, each play's pitch is multiplied by a value drawn between these two (1: as made), so repeats vary.")] public EffectRange? Pitch { get; init; }
    /// <summary>A level multiplier drawn per play, if given.</summary>
    [Field(5)] [Help("If set, each play's loudness is multiplied by a value drawn between these two, so repeats vary.")] public EffectRange? Volume { get; init; }

    /// <summary>
    /// What makes the patch unplayable ("has no layers", …), or null: checked when it is registered and when it is
    /// loaded from the data. The first of its check's problems (<see cref="RecordChecks.Check"/>: its own rules,
    /// <see cref="Playable"/>, then the limits its fields declare, which those rules already hold, G4).
    /// </summary>
    internal string? Problem() => RecordChecks.Check(this).FirstOrDefault()?.Message;

    /// <summary>
    /// The patch's rules, in order, each at what it names: layers to play, a voice allowed, each layer with a
    /// length above 0 and no negative attack or delay (the message at each of those that breaks it), and a
    /// harmonics layer with harmonics.
    /// </summary>
    [Check]
    static void Playable(EffectPatch patch, CheckContext check)
    {
        var layers = RecordPath.Root.Field(nameof(Layers));
        if (patch.Layers.Length == 0)
            check.Report(layers, "has no layers");
        if (patch.MaxVoices < 1)
            check.Report(RecordPath.Root.Field(nameof(MaxVoices)), "allows no voices");
        for (var i = 0; i < patch.Layers.Length; i++)
        {
            var layer = patch.Layers[i];
            var at = layers.Index(i);
            var message = $"has a layer {i} that needs a length above 0, and no negative attack or delay";
            if (!(layer.Length > 0))
                check.Report(at.Field(nameof(EffectLayer.Length)), message);
            if (!(layer.Attack >= 0))
                check.Report(at.Field(nameof(EffectLayer.Attack)), message);
            if (!(layer.Delay >= 0))
                check.Report(at.Field(nameof(EffectLayer.Delay)), message);
            if (layer.Source == EffectSource.Harmonics && layer.Harmonics is not { Length: > 0 })
                check.Report(at.Field(nameof(EffectLayer.Harmonics)), $"has a layer {i} with no harmonics");
        }
    }
}

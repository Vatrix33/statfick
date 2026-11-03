using Statventure.Records;

namespace Statventure.Audio;

// Sound effects as data (Docs/Audio.md, step 8): one-shot patches, layers of tones and noise bursts
// built from the same nodes as the music, the way the composer's percussion voices are. Records (M9
// step 6): entries audio/effects/<name> of audio.pak, which the audio system registers under their
// names when it starts (S6-4), or made in code and registered; M11 edits them.

/// <summary>What a layer sounds: an oscillator's wave, harmonics (a periodic wave of sine terms) or looping noise.</summary>
public enum EffectSource { Sine, Square, Sawtooth, Triangle, Harmonics, Noise }

/// <summary>In the order of the private BiquadFilterType, which a patch's filter converts to.</summary>
public enum EffectFilterType { Lowpass, Highpass, Bandpass, Lowshelf, Highshelf, Peaking, Notch, Allpass }

/// <summary>A range a patch draws from on each play.</summary>
[Record("audio.effect-range", 1)]
public sealed record EffectRange([property: Field(1)] double Min, [property: Field(2)] double Max);

/// <summary>A layer's filter; its frequency scales with the play's pitch, and may sweep exponentially.</summary>
[Record("audio.effect-filter", 1)]
public sealed record EffectFilter
{
    [Field(1)] public required EffectFilterType Type { get; init; }
    /// <summary>Hz at the layer's start.</summary>
    [Field(2)] public required double Frequency { get; init; }
    /// <summary>Hz to sweep to, if it sweeps.</summary>
    [Field(3)] public double? FrequencyTo { get; init; }
    /// <summary>Seconds of the sweep (0.6 of the layer's length by default, as the composer's sweeps).</summary>
    [Field(4)] public double? SweepTime { get; init; }
    [Field(5)] public double Q { get; init; } = 1;
    /// <summary>dB, for shelves and peaks.</summary>
    [Field(6)] public double? Gain { get; init; }
}

/// <summary>A tone or noise burst with its own envelope: from silence to <see cref="Level"/> over <see cref="Attack"/>, then decaying over about <see cref="Length"/>.</summary>
[Record("audio.effect-layer", 1)]
public sealed record EffectLayer
{
    [Field(1)] public required EffectSource Source { get; init; }
    /// <summary>Hz at the layer's start, scaled by the play's pitch (tones only).</summary>
    [Field(2)] public double Frequency { get; init; } = 440;
    /// <summary>Hz to sweep to, if it sweeps (tones only).</summary>
    [Field(3)] public double? FrequencyTo { get; init; }
    /// <summary>Seconds of the sweep (0.6 of the length by default).</summary>
    [Field(4)] public double? SweepTime { get; init; }
    /// <summary>The sine terms' amplitudes for <see cref="EffectSource.Harmonics"/>, from the DC term up (as the composer's organ and winds).</summary>
    [Field(5)] public double[]? Harmonics { get; init; }
    [Field(6)] public EffectFilter? Filter { get; init; }
    /// <summary>Seconds after the play's start.</summary>
    [Field(7)] public double Delay { get; init; }
    [Field(8)] public double Attack { get; init; } = 0.002;
    [Field(9)] public double Level { get; init; } = 1;
    /// <summary>Seconds of the decay (a time constant of a quarter of it); the layer stops 50 ms after.</summary>
    [Field(10)] public required double Length { get; init; }
}

/// <summary>A one-shot sound: its layers, level, how many voices of it may sound at once, and what varies per play.</summary>
[Record("audio.effect", 1)]
public sealed record EffectPatch
{
    [Field(1)] public required EffectLayer[] Layers { get; init; }
    [Field(2)] public double Level { get; init; } = 1;
    /// <summary>Past this many voices of the patch, the oldest fades out over 5 ms.</summary>
    [Field(3)] public int MaxVoices { get; init; } = 4;
    /// <summary>A pitch multiplier drawn per play, if given.</summary>
    [Field(4)] public EffectRange? Pitch { get; init; }
    /// <summary>A level multiplier drawn per play, if given.</summary>
    [Field(5)] public EffectRange? Volume { get; init; }

    /// <summary>What makes the patch unplayable ("has no layers", …), or null: checked when it is registered and when it is loaded from the data.</summary>
    internal string? Problem()
    {
        if (Layers.Length == 0)
            return "has no layers";
        if (MaxVoices < 1)
            return "allows no voices";
        for (var i = 0; i < Layers.Length; i++)
        {
            var layer = Layers[i];
            if (!(layer.Length > 0) || !(layer.Attack >= 0) || !(layer.Delay >= 0))
                return $"has a layer {i} that needs a length above 0, and no negative attack or delay";
            if (layer.Source == EffectSource.Harmonics && layer.Harmonics is not { Length: > 0 })
                return $"has a layer {i} with no harmonics";
        }
        return null;
    }
}

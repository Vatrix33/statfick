namespace Statventure.Audio;

// Sound effects as data (Docs/Audio.md, step 8): one-shot patches, layers of tones and noise bursts
// built from the same nodes as the music, the way the composer's percussion voices are. M8 has them as
// records made in code; M9 adds the pack form, M11 the editor.

/// <summary>What a layer sounds: an oscillator's wave, harmonics (a periodic wave of sine terms) or looping noise.</summary>
public enum EffectSource { Sine, Square, Sawtooth, Triangle, Harmonics, Noise }

/// <summary>In the order of the private BiquadFilterType, which a patch's filter converts to.</summary>
public enum EffectFilterType { Lowpass, Highpass, Bandpass, Lowshelf, Highshelf, Peaking, Notch, Allpass }

/// <summary>A range a patch draws from on each play.</summary>
public sealed record EffectRange(double Min, double Max);

/// <summary>A layer's filter; its frequency scales with the play's pitch, and may sweep exponentially.</summary>
public sealed record EffectFilter
{
    public required EffectFilterType Type { get; init; }
    /// <summary>Hz at the layer's start.</summary>
    public required double Frequency { get; init; }
    /// <summary>Hz to sweep to, if it sweeps.</summary>
    public double? FrequencyTo { get; init; }
    /// <summary>Seconds of the sweep (0.6 of the layer's length by default, as the composer's sweeps).</summary>
    public double? SweepTime { get; init; }
    public double Q { get; init; } = 1;
    /// <summary>dB, for shelves and peaks.</summary>
    public double? Gain { get; init; }
}

/// <summary>A tone or noise burst with its own envelope: from silence to <see cref="Level"/> over <see cref="Attack"/>, then decaying over about <see cref="Length"/>.</summary>
public sealed record EffectLayer
{
    public required EffectSource Source { get; init; }
    /// <summary>Hz at the layer's start, scaled by the play's pitch (tones only).</summary>
    public double Frequency { get; init; } = 440;
    /// <summary>Hz to sweep to, if it sweeps (tones only).</summary>
    public double? FrequencyTo { get; init; }
    /// <summary>Seconds of the sweep (0.6 of the length by default).</summary>
    public double? SweepTime { get; init; }
    /// <summary>The sine terms' amplitudes for <see cref="EffectSource.Harmonics"/>, from the DC term up (as the composer's organ and winds).</summary>
    public double[]? Harmonics { get; init; }
    public EffectFilter? Filter { get; init; }
    /// <summary>Seconds after the play's start.</summary>
    public double Delay { get; init; }
    public double Attack { get; init; } = 0.002;
    public double Level { get; init; } = 1;
    /// <summary>Seconds of the decay (a time constant of a quarter of it); the layer stops 50 ms after.</summary>
    public required double Length { get; init; }
}

/// <summary>A one-shot sound: its layers, level, how many voices of it may sound at once, and what varies per play.</summary>
public sealed record EffectPatch
{
    public required EffectLayer[] Layers { get; init; }
    public double Level { get; init; } = 1;
    /// <summary>Past this many voices of the patch, the oldest fades out over 5 ms.</summary>
    public int MaxVoices { get; init; } = 4;
    /// <summary>A pitch multiplier drawn per play, if given.</summary>
    public EffectRange? Pitch { get; init; }
    /// <summary>A level multiplier drawn per play, if given.</summary>
    public EffectRange? Volume { get; init; }
}

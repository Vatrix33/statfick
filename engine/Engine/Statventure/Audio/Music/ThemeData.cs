using Statventure.Records;

namespace Statventure.Audio;

// The music data (Docs/Audio.md, step 6; Docs/Assets.md, step 6): the old game's theme specs
// (music-themes.js, as MusicThemes.all() returns them) and the composer's instrument, percussion and
// room tables (music-composer.js), as records. They live in audio.pak, one entry each (Q21), made by
// Tools/Scripts/make-music-data.sh, and are read through a MusicLibrary. Each property's summary names
// the JavaScript key it comes from; the JSON form uses the properties' own names (Q22). An absent key
// is null, and the composer applies the old defaults (`??` and `||`) where it reads them.

/// <summary>A theme: tempo, meter, key and mode, a room, its sections and the form that orders them.</summary>
[Record("audio.theme", 1)]
public sealed record ThemeSpec
{
    /// <summary>id: what the game plays it by ("alder-vale", "combat@alder-vale"); its entry's path is made from it (MusicNames).</summary>
    [Field(1)] public required string Id { get; init; }
    [Field(2)] public required string Title { get; init; }
    /// <summary>tempo: beats per minute.</summary>
    [Field(3)] public required double Tempo { get; init; }
    /// <summary>meter: [beats per bar, grid steps per beat].</summary>
    [Field(4)] public required int[] Meter { get; init; }
    /// <summary>tonic: the key's MIDI note.</summary>
    [Field(5)] public required double Tonic { get; init; }
    /// <summary>mode: a key of the composer's modes (ionian, dorian, … hungarian).</summary>
    [Field(6)] public required string Mode { get; init; }
    [Field(7)] public string? Room { get; init; }
    [Field(8)] public double? Gain { get; init; }
    /// <summary>swing: the delay of every second step, in steps.</summary>
    [Field(9)] public double? Swing { get; init; }
    [Field(10)] public ThemeEq? Eq { get; init; }
    [Field(11)] public required IReadOnlyDictionary<string, SectionSpec> Sections { get; init; }
    /// <summary>form: section names in playing order.</summary>
    [Field(12)] public required string[] Form { get; init; }
    /// <summary>loop: the index in the form where the theme loops back to.</summary>
    [Field(13)] public int? Loop { get; init; }
    /// <summary>motif: the theme's motif, a note for its authors (the composer doesn't read it).</summary>
    [Field(14)] public string? Motif { get; init; }
}

/// <summary>eq: the master EQ's shelf and presence gains in dB (defaults −3 and 2).</summary>
[Record("audio.eq", 1)]
public sealed record ThemeEq
{
    [Field(1)] public double? Low { get; init; }
    [Field(2)] public double? Presence { get; init; }
}

/// <summary>A section: chords per bar, a melody, dynamics and layers; "like" takes another section's keys it doesn't set.</summary>
[Record("audio.section", 1)]
public sealed record SectionSpec
{
    [Field(1)] public string? Chords { get; init; }
    /// <summary>mel: the melody, in scale degrees with durations.</summary>
    [Field(2)] public string? Melody { get; init; }
    /// <summary>dyn: the dynamics at the start (1 by default).</summary>
    [Field(3)] public double? Dynamics { get; init; }
    /// <summary>dynTo: the dynamics at the end (the start's by default).</summary>
    [Field(4)] public double? DynamicsTo { get; init; }
    [Field(5)] public string? Like { get; init; }
    [Field(6)] public LayerSpec[]? Layers { get; init; }
}

/// <summary>A layer of a section: what plays (t: pad, drone, arp, bass, pulse, strum, mel, line, drums, hit, tex, drips), on which instrument, how.</summary>
[Record("audio.layer", 1)]
public sealed record LayerSpec
{
    /// <summary>t.</summary>
    [Field(1)] public required string Type { get; init; }
    /// <summary>i: an instrument (or for hit layers a percussion voice).</summary>
    [Field(2)] public string? Instrument { get; init; }
    /// <summary>o: octaves up or down.</summary>
    [Field(3)] public double? Octave { get; init; }
    /// <summary>v: velocity (0.7 by default).</summary>
    [Field(4)] public double? Velocity { get; init; }
    /// <summary>p: a step pattern (arp, bass, pulse, strum).</summary>
    [Field(5)] public string? Pattern { get; init; }
    /// <summary>p of a drums layer: a pattern per percussion voice, in the spec's order.</summary>
    [Field(6)] public IReadOnlyList<KeyValuePair<string, string>>? Drums { get; init; }
    /// <summary>deg of a hit layer: the scale degree it is tuned to.</summary>
    [Field(7)] public string? Degree { get; init; }
    /// <summary>deg of a drone layer: the scale degrees it holds (1 and 5 by default).</summary>
    [Field(8)] public string[]? Degrees { get; init; }
    /// <summary>n: a pad's voice count (3 by default).</summary>
    [Field(9)] public int? Voices { get; init; }
    /// <summary>every: a drone's length in steps (the section by default).</summary>
    [Field(10)] public int? Every { get; init; }
    /// <summary>bars: the bars of each cycle the layer plays in.</summary>
    [Field(11)] public int[]? Bars { get; init; }
    /// <summary>cycle: the bar cycle "bars" counts in (the section's bars by default).</summary>
    [Field(12)] public int? Cycle { get; init; }
    /// <summary>chance: a drip's chance per step (0.08 by default).</summary>
    [Field(13)] public double? Chance { get; init; }
    /// <summary>len: a swell hit's length in steps (a bar by default).</summary>
    [Field(14)] public double? Length { get; init; }
    /// <summary>at: a hit's step in the bar.</summary>
    [Field(15)] public int? At { get; init; }
    /// <summary>kind: a texture (wind, rain, surf, embers, leaves, drips).</summary>
    [Field(16)] public string? Kind { get; init; }
    /// <summary>role: what the "only" option filters by instead of the type (quote).</summary>
    [Field(17)] public string? Role { get; init; }
    /// <summary>notes: a line's own melody.</summary>
    [Field(18)] public string? Notes { get; init; }
    /// <summary>pan.</summary>
    [Field(19)] public double? Pan { get; init; }
    /// <summary>spread: a pad's pan between voices (0.35 by default).</summary>
    [Field(20)] public double? Spread { get; init; }
    /// <summary>legato: note length against the written length.</summary>
    [Field(21)] public double? Legato { get; init; }
    /// <summary>timpOct and logOct: the drums' timpani and log tunings in octaves.</summary>
    [Field(22)] public double? TimpaniOctave { get; init; }
    [Field(23)] public double? LogOctave { get; init; }
    /// <summary>harm: a harmony line in scale steps under or over the melody.</summary>
    [Field(24)] public HarmonySpec? Harmony { get; init; }
    /// <summary>dbl: instruments doubling a melody.</summary>
    [Field(25)] public DoublingSpec[]? Doublings { get; init; }
}

/// <summary>harm: a harmony line (i, steps, v, o, pan).</summary>
[Record("audio.harmony", 1)]
public sealed record HarmonySpec
{
    [Field(1)] public string? Instrument { get; init; }
    /// <summary>steps: scale steps from the melody (−2 by default).</summary>
    [Field(2)] public double? Steps { get; init; }
    [Field(3)] public double? Velocity { get; init; }
    [Field(4)] public double? Octave { get; init; }
    [Field(5)] public double? Pan { get; init; }
}

/// <summary>An entry of dbl: an instrument doubling the melody (i, o, v, pan, shift).</summary>
[Record("audio.doubling", 1)]
public sealed record DoublingSpec
{
    [Field(1)] public string? Instrument { get; init; }
    [Field(2)] public double? Octave { get; init; }
    [Field(3)] public double? Velocity { get; init; }
    [Field(4)] public double? Pan { get; init; }
    /// <summary>shift: semitones added.</summary>
    [Field(5)] public double? Shift { get; init; }
}

/// <summary>An instrument of the composer: its kind (bowed, brass, choir, organ, wind, pluck, mallet, glass, sub) picks the per-note graph; the rest tunes it.</summary>
[Record("audio.instrument", 1)]
public sealed record InstrumentSpec
{
    [Field(1)] public required string Kind { get; init; }
    /// <summary>voices: detunes in cents, one oscillator each.</summary>
    [Field(2)] public double[]? Voices { get; init; }
    [Field(3)] public double? Cutoff { get; init; }
    [Field(4)] public double? Attack { get; init; }
    [Field(5)] public double? Release { get; init; }
    /// <summary>vib: vibrato [rate Hz, depth cents, delay s].</summary>
    [Field(6)] public double[]? Vibrato { get; init; }
    [Field(7)] public required double Level { get; init; }
    [Field(8)] public double? Pan { get; init; }
    [Field(9)] public double? Send { get; init; }
    [Field(10)] public double? Decay { get; init; }
    /// <summary>trem: tremolo rate in Hz.</summary>
    [Field(11)] public double? Tremolo { get; init; }
    /// <summary>peak: a resonance [Hz, dB].</summary>
    [Field(12)] public double[]? Peak { get; init; }
    /// <summary>low and high: a brass filter's sweep in Hz.</summary>
    [Field(13)] public double? Low { get; init; }
    [Field(14)] public double? High { get; init; }
    /// <summary>vowel: a choir's formants in Hz.</summary>
    [Field(15)] public double[]? Vowel { get; init; }
    /// <summary>harmonics: an organ's or wind's partial amplitudes (sine terms).</summary>
    [Field(16)] public double[]? Harmonics { get; init; }
    [Field(17)] public double? Breath { get; init; }
    /// <summary>bright, damp, length, body, highpass: a plucked string's Karplus-Strong tone and body.</summary>
    [Field(18)] public double? Bright { get; init; }
    [Field(19)] public double? Damp { get; init; }
    [Field(20)] public double? Length { get; init; }
    [Field(21)] public double? Body { get; init; }
    [Field(22)] public double? Highpass { get; init; }
    /// <summary>partials: a mallet's [ratio, amount, decay s] per partial.</summary>
    [Field(23)] public double[][]? Partials { get; init; }
}

/// <summary>A percussion voice: a pitch sweep (body, tone, log, timp), noise bursts (noise, click, high, swell, thunder) or partials (metal).</summary>
[Record("audio.percussion", 1)]
public sealed record PercussionSpec
{
    [Field(1)] public double[]? Body { get; init; }
    [Field(2)] public double[]? Noise { get; init; }
    [Field(3)] public double? Click { get; init; }
    [Field(4)] public required double Level { get; init; }
    [Field(5)] public double? Send { get; init; }
    [Field(6)] public double[]? High { get; init; }
    [Field(7)] public double[]? Tone { get; init; }
    [Field(8)] public double? Attack { get; init; }
    [Field(9)] public bool Log { get; init; }
    [Field(10)] public bool Swell { get; init; }
    [Field(11)] public double[][]? Metal { get; init; }
    [Field(12)] public double? Base { get; init; }
    [Field(13)] public bool Thunder { get; init; }
    /// <summary>timp.</summary>
    [Field(14)] public bool Timpani { get; init; }
}

/// <summary>A room: its reverb's length in seconds, darkness and wet level.</summary>
[Record("audio.room", 1)]
public sealed record RoomSpec([property: Field(1)] double Seconds, [property: Field(2)] double Dark, [property: Field(3)] double Wet);

/// <summary>
/// The themes in the old game's order (MusicThemes.all(): regions, the battle theme and its regional
/// arrangements, bosses), by id: the entry <c>audio/theme-order</c> (Q21, S6-2). Theme entries it
/// doesn't name follow it in path order.
/// </summary>
[Record("audio.theme-order", 1)]
public sealed record ThemeOrder
{
    [Field(1)] public required string[] Themes { get; init; }
}

using Statventure.Records;

namespace Statventure.Audio;

// The music data (Docs/Audio.md, step 6; Docs/Assets.md, step 6): the old game's theme specs
// (music-themes.js, as MusicThemes.all() returns them) and the composer's instrument, percussion and
// room tables (music-composer.js), as records. They live in audio.pak, one entry each (Q21), made by
// Tools/Scripts/make-music-data.sh, and are read through a MusicLibrary. Each property's summary names
// the JavaScript key it comes from; the JSON form uses the properties' own names (Q22). An absent key
// is null, and the composer applies the old defaults (`??` and `||`) where it reads them.
// Marks for tools (M11 step 5; Docs/Audio.md, "Checks and marks"): the kinds' names, folders and the
// music's name rule, help texts, units, the strings the composer knows (choices) and the names of other
// entries (references); a theme's and the theme order's own checks. They change no data and no layout.

/// <summary>A theme: tempo, meter, key and mode, a room, its sections and the form that orders them.</summary>
[Record("audio.theme", 1)]
[Kind("Theme", MusicNames.Themes, Rule = typeof(MusicNameRule))]
[Help("A piece of music the game plays: its tempo, key and room, its sections and the order they play in.")]
public sealed record ThemeSpec
{
    /// <summary>id: what the game plays it by ("alder-vale", "combat@alder-vale"); its entry's path is made from it (MusicNames).</summary>
    [Field(1)]
    [Help("What the game plays the theme by, such as \"alder-vale\" or \"combat@alder-vale\"; the entry's name is made from it.")]
    public required string Id { get; init; }
    [Field(2)] [Help("The theme's name for people.")] public required string Title { get; init; }
    /// <summary>tempo: beats per minute.</summary>
    [Field(3)] [Help("How fast it plays, in beats per minute.")] [Unit("bpm")] public required double Tempo { get; init; }
    /// <summary>meter: [beats per bar, grid steps per beat].</summary>
    [Field(4)] [Help("Two numbers: the beats in a bar, and the grid steps in a beat (patterns and note lengths count in steps).")]
    public required int[] Meter { get; init; }
    /// <summary>tonic: the key's MIDI note.</summary>
    [Field(5)] [Help("The key's root note, as a MIDI note number (60 is middle C).")] public required double Tonic { get; init; }
    /// <summary>mode: a key of the composer's modes (ionian, dorian, … hungarian).</summary>
    [Field(6)]
    [Help("The scale the theme's degrees and chords are taken from.")]
    [Choices("ionian", "dorian", "phrygian", "lydian", "mixolydian", "aeolian", "locrian", "harmonic", "hijaz", "hungarian")]
    public required string Mode { get; init; }
    [Field(7)]
    [Help("The room whose echo the theme plays in (the hall when not set).")]
    [References(typeof(RoomSpec))]
    public string? Room { get; init; }
    [Field(8)] [Help("The theme's loudness (1 when not set).")] public double? Gain { get; init; }
    /// <summary>swing: the delay of every second step, in steps.</summary>
    [Field(9)] [Help("How far every second grid step is played late (0 or not set: straight).")] [Unit("steps")] public double? Swing { get; init; }
    [Field(10)] [Help("The theme's tone: how much its lows and its presence are raised or lowered.")] public ThemeEq? Eq { get; init; }
    [Field(11)] [Help("The theme's sections by name: their chords, melody and the layers that play them.")]
    public required IReadOnlyDictionary<string, SectionSpec> Sections { get; init; }
    /// <summary>form: section names in playing order.</summary>
    [Field(12)] [Help("The sections' names in playing order; the theme then starts again from its loop point.")] public required string[] Form { get; init; }
    /// <summary>loop: the index in the form where the theme loops back to.</summary>
    [Field(13)] [Help("Where in the form the theme starts again after its end: 0 for the first section (0 when not set).")] public int? Loop { get; init; }
    /// <summary>motif: the theme's motif, a note for its authors (the composer doesn't read it).</summary>
    [Field(14)] [Help("A note for the theme's authors; it doesn't change the sound.")] public string? Motif { get; init; }

    /// <summary>
    /// What fails today only when the music is compiled or played (G5; <see cref="ThemeCheck"/>), with the data
    /// its room and the instruments and percussion voices its layers play; without it, the score and the layers'
    /// own rules. (A theme's id against its entry's path is checked where the path is known: when the library loads.)
    /// </summary>
    [Check]
    static void Playable(ThemeSpec theme, CheckContext check)
    {
        foreach (var problem in ThemeCheck.Problems(theme, check.Data is { } data ? MusicParts.Of(data) : null))
            check.Report(problem.Path, problem.Message);
    }
}

/// <summary>eq: the master EQ's shelf and presence gains in dB (defaults −3 and 2).</summary>
[Record("audio.eq", 1)]
[Kind("EQ")]
[Help("The theme's tone.")]
public sealed record ThemeEq
{
    [Field(1)] [Help("How much the lows below 140 Hz are raised, or lowered below 0 (−3 when not set).")] [Unit("dB")] public double? Low { get; init; }
    [Field(2)] [Help("How much the presence around 2.6 kHz is raised, or lowered below 0 (2 when not set).")] [Unit("dB")] public double? Presence { get; init; }
}

/// <summary>A section: chords per bar, a melody, dynamics and layers; "like" takes another section's keys it doesn't set.</summary>
[Record("audio.section", 1)]
[Kind("Section")]
[Help("A part of the theme: its chords and melody, bar by bar, and the layers that play them.")]
public sealed record SectionSpec
{
    [Field(1)]
    [Help("The chords, bar by bar, split by |: scale-degree numerals such as 1, 4, b6, 5M, 4m7 or 1sus4, several in a bar sharing it; \".\" holds the chord before, a bar of \"%\" repeats the bar before.")]
    public string? Chords { get; init; }
    /// <summary>mel: the melody, in scale degrees with durations.</summary>
    [Field(2)]
    [Help("The melody, bar by bar, split by |: notes as degree/steps (5/2, b3'/1; ' an octave up, a comma down), _/steps a rest, -/steps to hold the note before longer, a ! at the end for an accent; each bar's steps must add up to a bar.")]
    public string? Melody { get; init; }
    /// <summary>dyn: the dynamics at the start (1 by default).</summary>
    [Field(3)] [Help("How loud the section is at its start (1 when not set).")] public double? Dynamics { get; init; }
    /// <summary>dynTo: the dynamics at the end (the start's by default).</summary>
    [Field(4)] [Help("How loud it is at its end (the start's when not set).")] public double? DynamicsTo { get; init; }
    [Field(5)] [Help("Another section of the theme whose chords, melody, dynamics and layers this one takes where it sets none.")] public string? Like { get; init; }
    [Field(6)] [Help("What plays in the section.")] public LayerSpec[]? Layers { get; init; }
}

/// <summary>A layer of a section: what plays (t: pad, drone, arp, bass, pulse, strum, mel, line, drums, hit, tex, drips), on which instrument, how.</summary>
[Record("audio.layer", 1)]
[Kind("Layer")]
[Help("One part of a section: what it plays and on which instrument.")]
public sealed record LayerSpec
{
    /// <summary>t.</summary>
    [Field(1)]
    [Help("What the layer plays: pad (the chords, held), drone (held degrees), arp, bass or pulse (chord tones by a pattern), strum, mel (the section's melody), line (its own notes), drums, hit (a percussion hit in chosen bars), tex (a noise texture) or drips.")]
    [Choices("pad", "drone", "arp", "bass", "pulse", "strum", "mel", "line", "drums", "hit", "tex", "drips")]
    public required string Type { get; init; }
    /// <summary>i: an instrument (or for hit layers a percussion voice).</summary>
    [Field(2)]
    [Help("The instrument it plays on; for a hit, the percussion voice; drips play the kalimba when not set.")]
    [References(typeof(InstrumentSpec), When = nameof(Type), Is = ["pad", "drone", "arp", "bass", "pulse", "strum", "mel", "line", "drips"])]
    [References(typeof(PercussionSpec), When = nameof(Type), Is = ["hit"])]
    public string? Instrument { get; init; }
    /// <summary>o: octaves up or down.</summary>
    [Field(3)] [Help("Octaves up, or down below 0.")] public double? Octave { get; init; }
    /// <summary>v: velocity (0.7 by default).</summary>
    [Field(4)] [Help("How hard it plays (0.7 when not set).")] public double? Velocity { get; init; }
    /// <summary>p: a step pattern (arp, bass, pulse, strum).</summary>
    [Field(5)]
    [Help("For arp, bass, pulse and strum: a symbol per grid step, repeating: a chord tone to play (1, 3, 5, 7, 8; T and F an octave up, L and P down), for a strum d, u or D, \".\" a rest, \"-\" to hold the note before.")]
    public string? Pattern { get; init; }
    /// <summary>p of a drums layer: a pattern per percussion voice, in the spec's order.</summary>
    [Field(6)]
    [Help("For drums: a pattern per percussion voice, a symbol per grid step, repeating: x a hit, X a loud one, o a soft one, r a roll, f a flam, \".\" a rest.")]
    [References(typeof(PercussionSpec), Keys = true)]
    public IReadOnlyList<KeyValuePair<string, string>>? Drums { get; init; }
    /// <summary>deg of a hit layer: the scale degree it is tuned to.</summary>
    [Field(7)] [Help("For a hit: the scale degree it is tuned to.")] public string? Degree { get; init; }
    /// <summary>deg of a drone layer: the scale degrees it holds (1 and 5 by default).</summary>
    [Field(8)] [Help("For a drone: the scale degrees it holds (1 and 5 when not set).")] public string[]? Degrees { get; init; }
    /// <summary>n: a pad's voice count (3 by default).</summary>
    [Field(9)] [Help("For a pad: how many notes of each chord it plays (3 when not set).")] public int? Voices { get; init; }
    /// <summary>every: a drone's length in steps (the section by default).</summary>
    [Field(10)] [Help("For a drone: how many grid steps each held note lasts (the whole section when not set).")] [Unit("steps")] public int? Every { get; init; }
    /// <summary>bars: the bars of each cycle the layer plays in.</summary>
    [Field(11)] [Help("The bars of each cycle the layer plays in, counting from 0 (every bar when not set).")] public int[]? Bars { get; init; }
    /// <summary>cycle: the bar cycle "bars" counts in (the section's bars by default).</summary>
    [Field(12)] [Help("How many bars that cycle has (the section's bars when not set).")] public int? Cycle { get; init; }
    /// <summary>chance: a drip's chance per step (0.08 by default).</summary>
    [Field(13)] [Help("For drips: the chance of a drip on each grid step (0.08 when not set).")] public double? Chance { get; init; }
    /// <summary>len: a swell hit's length in steps (a bar by default).</summary>
    [Field(14)] [Help("For a swell hit: how many grid steps it swells over (a bar when not set).")] [Unit("steps")] public double? Length { get; init; }
    /// <summary>at: a hit's step in the bar.</summary>
    [Field(15)] [Help("For a hit: the grid step of the bar it falls on (0 when not set).")] public int? At { get; init; }
    /// <summary>kind: a texture (wind, rain, surf, embers, leaves, drips).</summary>
    [Field(16)] [Help("For tex: the texture it plays.")] [Choices("wind", "rain", "surf", "embers", "leaves", "drips")] public string? Kind { get; init; }
    /// <summary>role: what the "only" option filters by instead of the type (quote).</summary>
    [Field(17)] [Help("A name to pick the layer by when only some layers are rendered (its type when not set).")] public string? Role { get; init; }
    /// <summary>notes: a line's own melody.</summary>
    [Field(18)] [Help("For a line: its own notes, written as a section's melody is.")] public string? Notes { get; init; }
    /// <summary>pan.</summary>
    [Field(19)] [Help("Where it is heard, from −1 (left) to 1 (right).")] public double? Pan { get; init; }
    /// <summary>spread: a pad's pan between voices (0.35 by default).</summary>
    [Field(20)] [Help("For a pad: how far apart its notes are panned (0.35 when not set).")] public double? Spread { get; init; }
    /// <summary>legato: note length against the written length.</summary>
    [Field(21)] [Help("How long notes sound against their written length (1: their full length).")] public double? Legato { get; init; }
    /// <summary>timpOct and logOct: the drums' timpani and log tunings in octaves.</summary>
    [Field(22)] [Help("For drums: octaves the timpani is tuned up or down from two octaves below the chord's root.")] public double? TimpaniOctave { get; init; }
    [Field(23)] [Help("For drums: the log drum's octave from the chord (−1 when not set).")] public double? LogOctave { get; init; }
    /// <summary>harm: a harmony line in scale steps under or over the melody.</summary>
    [Field(24)] [Help("For mel and line: a second line in harmony with the notes.")] public HarmonySpec? Harmony { get; init; }
    /// <summary>dbl: instruments doubling a melody.</summary>
    [Field(25)] [Help("For mel and line: other instruments playing the same notes.")] public DoublingSpec[]? Doublings { get; init; }
}

/// <summary>harm: a harmony line (i, steps, v, o, pan).</summary>
[Record("audio.harmony", 1)]
[Kind("Harmony")]
[Help("A second line in harmony with a melody's notes, a few scale steps away.")]
public sealed record HarmonySpec
{
    [Field(1)] [Help("The instrument it plays on.")] [References(typeof(InstrumentSpec))] public string? Instrument { get; init; }
    /// <summary>steps: scale steps from the melody (−2 by default).</summary>
    [Field(2)] [Help("Scale steps from the melody's notes, below 0 under them (−2, a third below, when not set).")] public double? Steps { get; init; }
    [Field(3)] [Help("How hard it plays (half the layer's when not set).")] public double? Velocity { get; init; }
    [Field(4)] [Help("Octaves up or down (the layer's when not set).")] public double? Octave { get; init; }
    [Field(5)] [Help("Where it is heard, from −1 (left) to 1 (right).")] public double? Pan { get; init; }
}

/// <summary>An entry of dbl: an instrument doubling the melody (i, o, v, pan, shift).</summary>
[Record("audio.doubling", 1)]
[Kind("Doubling")]
[Help("Another instrument playing a melody's notes with it.")]
public sealed record DoublingSpec
{
    [Field(1)] [Help("The instrument it plays on.")] [References(typeof(InstrumentSpec))] public string? Instrument { get; init; }
    [Field(2)] [Help("Octaves up, or down below 0, from the melody.")] public double? Octave { get; init; }
    [Field(3)] [Help("How hard it plays (half the layer's when not set).")] public double? Velocity { get; init; }
    [Field(4)] [Help("Where it is heard, from −1 (left) to 1 (right).")] public double? Pan { get; init; }
    /// <summary>shift: semitones added.</summary>
    [Field(5)] [Help("Semitones added to the melody's notes.")] [Unit("semitones")] public double? Shift { get; init; }
}

/// <summary>An instrument of the composer: its kind (bowed, brass, choir, organ, wind, pluck, mallet, glass, sub) picks the per-note graph; the rest tunes it.</summary>
[Record("audio.instrument", 1)]
[Kind("Instrument", MusicNames.Instruments, Rule = typeof(MusicNameRule))]
[Help("An instrument the themes' layers play: its kind picks how a note is made, the rest tunes it (each field says which kinds use it).")]
public sealed record InstrumentSpec
{
    [Field(1)]
    [Help("How a note is made: bowed and brass (sawtooth voices through a filter), choir (voices through a vowel), organ and wind (a wave of harmonics), pluck (a plucked string), mallet (struck partials), glass (a glassy tone with tremolo) or sub (a deep sine).")]
    [Choices("pluck", "mallet", "glass", "sub", "bowed", "brass", "choir", "organ", "wind")]
    public required string Kind { get; init; }
    /// <summary>voices: detunes in cents, one oscillator each.</summary>
    [Field(2)] [Help("For bowed, brass and choir: one voice per number, detuned by that many cents; with none, the instrument makes no sound.")] [Unit("cents")]
    public double[]? Voices { get; init; }
    [Field(3)] [Help("For bowed: how bright its filter is around a middle note; for wind: its filter's frequency (5200 when not set).")] [Unit("Hz")] public double? Cutoff { get; init; }
    [Field(4)] [Help("For every kind but pluck and mallet: how long a note takes to rise (shorter for short notes).")] [Unit("s")] public double? Attack { get; init; }
    [Field(5)] [Help("For every kind but pluck and mallet: how long a note takes to fade after it ends.")] [Unit("s")] public double? Release { get; init; }
    /// <summary>vib: vibrato [rate Hz, depth cents, delay s].</summary>
    [Field(6)] [Help("For bowed, brass, choir, organ and wind: a wobble in pitch: its rate in Hz, its depth in cents, and how long into the note it starts in seconds.")] public double[]? Vibrato { get; init; }
    [Field(7)] [Help("How loud a note is at full velocity.")] public required double Level { get; init; }
    [Field(8)] [Help("Where it is heard, from −1 (left) to 1 (right), unless the layer says.")] public double? Pan { get; init; }
    [Field(9)] [Help("How much of it goes into the theme's room (0.5 when not set).")] public double? Send { get; init; }
    [Field(10)] [Help("For a pluck: how long the string rings; for every other kind but mallet: how quickly a held note sinks toward 30% of its peak (when not set, it eases to 80% over the note).")] [Unit("s")]
    public double? Decay { get; init; }
    /// <summary>trem: tremolo rate in Hz.</summary>
    [Field(11)] [Help("For bowed, brass and glass: a pulse in loudness, this many times a second.")] [Unit("Hz")] public double? Tremolo { get; init; }
    /// <summary>peak: a resonance [Hz, dB].</summary>
    [Field(12)] [Help("For bowed and brass: a resonance: its frequency in Hz and how much it is raised in dB.")] public double[]? Peak { get; init; }
    /// <summary>low and high: a brass filter's sweep in Hz.</summary>
    [Field(13)] [Help("For brass: the filter's frequency at a note's start.")] [Unit("Hz")] public double? Low { get; init; }
    [Field(14)] [Help("For brass: the frequency the filter opens to, around a middle note.")] [Unit("Hz")] public double? High { get; init; }
    /// <summary>vowel: a choir's formants in Hz.</summary>
    [Field(15)] [Help("For a choir: the vowel's formant frequencies, up to three.")] [Unit("Hz")] public double[]? Vowel { get; init; }
    /// <summary>harmonics: an organ's or wind's partial amplitudes (sine terms).</summary>
    [Field(16)] [Help("For organ and wind: how loud each harmonic is; the first number isn't used, then the fundamental and each overtone above it.")]
    public double[]? Harmonics { get; init; }
    [Field(17)] [Help("For wind: how much breath noise sounds with each note.")] public double? Breath { get; init; }
    /// <summary>bright, damp, length, body, highpass: a plucked string's Karplus-Strong tone and body.</summary>
    [Field(18)] [Help("For a pluck: how bright the string's pluck is, from 0 to 1.")] public double? Bright { get; init; }
    [Field(19)] [Help("For a pluck: how quickly the string's high tones die away, from 0 to 1.")] public double? Damp { get; init; }
    [Field(20)] [Help("For a pluck: the longest the string rings.")] [Unit("s")] public double? Length { get; init; }
    [Field(21)] [Help("For a pluck: the frequency the instrument's body resonates at (none: no body).")] [Unit("Hz")] public double? Body { get; init; }
    [Field(22)] [Help("For a pluck: lows below this frequency are taken away (none: they stay).")] [Unit("Hz")] public double? Highpass { get; init; }
    /// <summary>partials: a mallet's [ratio, amount, decay s] per partial.</summary>
    [Field(23)] [Help("For a mallet: its partials, each three numbers: the frequency as a multiple of the note's, its loudness, and how long it rings in seconds.")]
    public double[][]? Partials { get; init; }
}

/// <summary>A percussion voice: a pitch sweep (body, tone, log, timp), noise bursts (noise, click, high, swell, thunder) or partials (metal).</summary>
[Record("audio.percussion", 1)]
[Kind("Percussion voice", MusicNames.Percussion, Rule = typeof(MusicNameRule))]
[Help("A drum or other percussion sound the themes' drums and hit layers play, made of pitch sweeps, noise bursts and ringing partials.")]
public sealed record PercussionSpec
{
    [Field(1)] [Help("A drum's body: a pitch falling from its first frequency to its second (Hz) over its length in seconds.")] public double[]? Body { get; init; }
    [Field(2)] [Help("A noise burst: with a body, its lowpass frequency in Hz and its length in seconds; with metal, the band's frequency and length.")] public double[]? Noise { get; init; }
    [Field(3)] [Help("With a body: how loud a short click at the start is.")] public double? Click { get; init; }
    [Field(4)] [Help("How loud a hit is at full velocity.")] public required double Level { get; init; }
    [Field(5)] [Help("How much of it goes into the theme's room (0.5 when not set).")] public double? Send { get; init; }
    [Field(6)] [Help("A bright noise burst: its frequency in Hz, its sharpness (Q) and its length in seconds.")] public double[]? High { get; init; }
    [Field(7)] [Help("A tone falling a little in pitch: its frequency in Hz and its length in seconds.")] public double[]? Tone { get; init; }
    [Field(8)] [Help("How long the bright noise burst takes to rise (2 ms when not set).")] [Unit("s")] public double? Attack { get; init; }
    [Field(9)] [Help("A wooden log drum tuned to the chord.")] public bool Log { get; init; }
    [Field(10)] [Help("A swelling rush of noise, as long as the hit layer asks.")] public bool Swell { get; init; }
    [Field(11)] [Help("Ringing metal partials, each three numbers: the frequency as a multiple of the base, its loudness, and how long it rings in seconds.")] public double[][]? Metal { get; init; }
    [Field(12)] [Help("For metal: the base frequency, unless the hit is tuned to a degree.")] [Unit("Hz")] public double? Base { get; init; }
    [Field(13)] [Help("Rolling thunder: rumbles of low noise.")] public bool Thunder { get; init; }
    /// <summary>timp.</summary>
    [Field(14)] [Help("A timpani tuned to the chord.")] public bool Timpani { get; init; }
}

/// <summary>A room: its reverb's length in seconds, darkness and wet level.</summary>
[Record("audio.room", 1)]
[Kind("Room", MusicNames.Rooms, Rule = typeof(MusicNameRule))]
[Help("The echo a theme plays in.")]
public sealed record RoomSpec(
    [property: Field(1), Help("How long the echo lasts."), Unit("s")] double Seconds,
    [property: Field(2), Help("How quickly the echo loses its highs, from 0 to 1.")] double Dark,
    [property: Field(3), Help("How loud the echo is.")] double Wet);

/// <summary>
/// The themes in the old game's order (MusicThemes.all(): regions, the battle theme and its regional
/// arrangements, bosses), by id: the entry <c>audio/theme-order</c> (Q21, S6-2). Theme entries it
/// doesn't name follow it in path order.
/// </summary>
[Record("audio.theme-order", 1)]
[Kind("Theme order")]
[Help("The order the game lists its themes in; themes it doesn't name follow it.")]
public sealed record ThemeOrder
{
    [Field(1)] [Help("The themes' ids, in order, each once.")] [References(typeof(ThemeSpec))] public required string[] Themes { get; init; }

    /// <summary>The order's rules as the library checks them when it loads (S6-2), each at the id it names; with the data, each id's entry.</summary>
    [Check]
    static void Listed(ThemeOrder order, CheckContext check)
    {
        Func<string, bool>? hasTheme = check.Data is { } data ? id => data.Exists(MusicNames.PathOf(MusicNames.Themes, id)) : null;
        foreach (var (index, problem) in MusicLibrary.OrderProblems(order, hasTheme))
            check.Report(RecordPath.Root.Field(nameof(Themes)).Index(index), problem);
    }
}

using System.Text.Json;
using Statventure.Audio;

namespace Statventure.Tests.Audio;

/// <summary>A theme spec written with the old JavaScript keys (as music-themes.js writes them), read into the engine's records.</summary>
internal static class ThemeJson
{
    static double D(JsonElement e) => e.GetDouble();
    static double[] Ds(JsonElement e) => [.. e.EnumerateArray().Select(D)];

    public static ThemeSpec Parse(string json)
    {
        using var document = JsonDocument.Parse(json);
        var t = document.RootElement;
        double? Num(JsonElement o, string key) => o.TryGetProperty(key, out var v) ? v.GetDouble() : null;
        int? Int(JsonElement o, string key) => o.TryGetProperty(key, out var v) ? v.GetInt32() : null;
        string? Str(JsonElement o, string key) => o.TryGetProperty(key, out var v) ? v.GetString() : null;
        LayerSpec Layer(JsonElement l) => new()
        {
            Type = Str(l, "t")!, Instrument = Str(l, "i"), Octave = Num(l, "o"), Velocity = Num(l, "v"),
            Pattern = l.TryGetProperty("p", out var p) && p.ValueKind == JsonValueKind.String ? p.GetString() : null,
            Drums = l.TryGetProperty("p", out var d) && d.ValueKind == JsonValueKind.Object
                ? [.. d.EnumerateObject().Select(e => new KeyValuePair<string, string>(e.Name, e.Value.GetString()!))] : null,
            Degree = l.TryGetProperty("deg", out var g) && g.ValueKind == JsonValueKind.String ? g.GetString() : null,
            Degrees = l.TryGetProperty("deg", out var gs) && gs.ValueKind == JsonValueKind.Array ? [.. gs.EnumerateArray().Select(x => x.GetString()!)] : null,
            Voices = Int(l, "n"), Every = Int(l, "every"), Cycle = Int(l, "cycle"), At = Int(l, "at"),
            Bars = l.TryGetProperty("bars", out var b) ? [.. b.EnumerateArray().Select(x => x.GetInt32())] : null,
            Chance = Num(l, "chance"), Length = Num(l, "len"), Kind = Str(l, "kind"), Role = Str(l, "role"), Notes = Str(l, "notes"),
            Pan = Num(l, "pan"), Spread = Num(l, "spread"), Legato = Num(l, "legato"), TimpaniOctave = Num(l, "timpOct"), LogOctave = Num(l, "logOct"),
            Harmony = l.TryGetProperty("harm", out var h)
                ? new() { Instrument = Str(h, "i"), Steps = Num(h, "steps"), Velocity = Num(h, "v"), Octave = Num(h, "o"), Pan = Num(h, "pan") } : null,
            Doublings = l.TryGetProperty("dbl", out var db)
                ? [.. db.EnumerateArray().Select(x => new DoublingSpec { Instrument = Str(x, "i"), Octave = Num(x, "o"), Velocity = Num(x, "v"), Pan = Num(x, "pan"), Shift = Num(x, "shift") })] : null,
        };
        SectionSpec Section(JsonElement s) => new()
        {
            Chords = Str(s, "chords"), Melody = Str(s, "mel"), Dynamics = Num(s, "dyn"), DynamicsTo = Num(s, "dynTo"), Like = Str(s, "like"),
            Layers = s.TryGetProperty("layers", out var ls) ? [.. ls.EnumerateArray().Select(Layer)] : null,
        };
        return new ThemeSpec
        {
            Id = Str(t, "id")!, Title = Str(t, "title") ?? "", Tempo = Num(t, "tempo")!.Value,
            Meter = [.. t.GetProperty("meter").EnumerateArray().Select(x => x.GetInt32())], Tonic = Num(t, "tonic")!.Value, Mode = Str(t, "mode")!,
            Room = Str(t, "room"), Gain = Num(t, "gain"), Swing = Num(t, "swing"),
            Eq = t.TryGetProperty("eq", out var eq) ? new() { Low = Num(eq, "low"), Presence = Num(eq, "presence") } : null,
            Sections = t.GetProperty("sections").EnumerateObject().ToDictionary(e => e.Name, e => Section(e.Value)),
            Form = [.. t.GetProperty("form").EnumerateArray().Select(x => x.GetString()!)], Loop = Int(t, "loop"),
        };
    }
}

internal static partial class AudioFixtures
{
    /// <summary>
    /// A small theme performed by the old composer in Chromium (the harness loads music-composer.js)
    /// and by the engine's: <paramref name="steps"/> steps at 0.05 + n · stepSeconds, as renderOffline
    /// does, into the destination. A suspend each quantum makes Chromium run the notes' cleanups
    /// where the engine does.
    /// </summary>
    static AudioFixture S(string name, int channels, double seconds, float rate, string spec, int steps, double tolerance = 0)
    {
        var script = $$"""
            const spec = {{spec}};
            const compiled = MusicComposer.compile(spec);
            const performance = MusicComposer.perform(ctx, compiled, out);
            for (let i = 0; i < {{steps}}; i++) performance.step(i, 0.05 + i * compiled.stepSeconds);
            """;
        var theme = ThemeJson.Parse(spec);
        return F(name, channels, (int)Math.Ceiling(seconds * rate), rate, script, r =>
        {
            var compiled = Score.Compile(theme);
            var performance = new Performance(new GraphBuilder(r.G, TestMusic.Library), compiled, r.G.Destination);
            for (var i = 0; i < steps; i++)
                performance.Step(i, 0.05 + i * compiled.StepSeconds);
        }, tolerance) with { Quanta = true };
    }

    /// <summary>One section of one bar per step (meter 1×1), a chord per bar, the layers given.</summary>
    static string Theme(string id, double tempo, string chords, string layers, string room = "chamber", string extra = "") =>
        $$"""{ "id": "{{id}}", "title": "{{id}}", "tempo": {{tempo}}, "meter": [1, 1], "tonic": 60, "mode": "dorian", "room": "{{room}}"{{extra}}, "sections": { "A": { "chords": "{{chords}}", "layers": [{{layers}}] } }, "form": ["A"] }""";

    /// <summary>
    /// music.js's setupAudio as the composer's themes meet it (master gain → limiter → destination),
    /// with its old score engine's delay and hall too, which no theme feeds: the engine leaves them
    /// out, and these fixtures show they only add zeros.
    /// </summary>
    const string MasterChain = """
        const master = ctx.createGain();
        master.gain.value = 0.58 * volume;
        const limiter = ctx.createDynamicsCompressor();
        limiter.threshold.value = -18;
        limiter.knee.value = 18;
        limiter.ratio.value = 3;
        limiter.attack.value = 0.006;
        limiter.release.value = 0.22;
        master.connect(limiter);
        limiter.connect(ctx.destination);
        const delay = ctx.createDelay(0.6);
        delay.delayTime.value = 0.23;
        const feedback = ctx.createGain();
        feedback.gain.value = 0.12;
        const wet = ctx.createGain();
        wet.gain.value = 0.18;
        delay.connect(feedback);
        feedback.connect(delay);
        delay.connect(wet);
        wet.connect(master);
        const hall = ctx.createConvolver();
        const hallBuffer = ctx.createBuffer(2, Math.ceil(ctx.sampleRate * 1.35), ctx.sampleRate);
        for (let channel = 0; channel < 2; channel++) {
          const samples = hallBuffer.getChannelData(channel);
          for (let i = 0; i < samples.length; i++) samples[i] = (Math.random() * 2 - 1) * Math.exp(-(i / ctx.sampleRate) * 4.2);
        }
        hall.buffer = hallBuffer;
        const hallFilter = ctx.createBiquadFilter();
        hallFilter.type = "lowpass";
        hallFilter.frequency.value = 3400;
        const hallLevel = ctx.createGain();
        hallLevel.gain.value = 0.16;
        hall.connect(hallFilter);
        hallFilter.connect(hallLevel);
        hallLevel.connect(master);

        """;

    /// <summary>music.js's sync, retire, stop, schedule and setVolume for the composer's themes (they need `master` and `volume`).</summary>
    const string MusicFunctions = """
        // music.js's sync, retire, stop, schedule and setVolume, for themes of the composer.
        let activeBus = null, activePerformance = null, activeTheme = null, nextNoteTime = 0, step = 0;
        const retire = bus => {
          if (!bus) return;
          const now = ctx.currentTime;
          bus.gain.cancelAndHoldAtTime(now);
          bus.gain.linearRampToValueAtTime(0, now + 0.55);
          at(now + 1.8, () => bus.disconnect());
        };
        const schedule = () => {
          if (!activeBus) return;
          if (nextNoteTime < ctx.currentTime - 0.5) nextNoteTime = ctx.currentTime + 0.04;
          while (nextNoteTime < ctx.currentTime + 0.2) {
            activePerformance.step(step, nextNoteTime);
            nextNoteTime += activePerformance.stepSeconds;
            step++;
          }
        };
        const play = (id, spec) => {
          if (id === activeTheme) return;
          retire(activeBus);
          activePerformance = MusicComposer.perform(ctx, MusicComposer.compile(spec), master);
          activeBus = activePerformance.bus;
          activeBus.gain.setValueAtTime(0, ctx.currentTime);
          activeBus.gain.linearRampToValueAtTime(1, ctx.currentTime + 0.8);
          activeTheme = id;
          step = 0;
          nextNoteTime = ctx.currentTime + 0.06;
          schedule();
        };
        const stop = () => { retire(activeBus); activeBus = null; activeTheme = null; activePerformance = null; };
        const setVolume = value => {
          volume = Math.max(0, Math.min(1, value));
          const now = ctx.currentTime;
          master.gain.cancelScheduledValues(now);
          master.gain.setValueAtTime(master.gain.value, now);
          master.gain.linearRampToValueAtTime(0.58 * volume, now + 0.04);
        };

        """;

    /// <summary>A theme with its own sections, form and loop.</summary>
    static string Song(string id, double tempo, int beats, int sub, string sections, string form, int loop = 0, string room = "chamber", string extra = "") =>
        $$"""{ "id": "{{id}}", "title": "{{id}}", "tempo": {{tempo}}, "meter": [{{beats}}, {{sub}}], "tonic": 60, "mode": "dorian", "room": "{{room}}"{{extra}}, "sections": { {{sections}} }, "form": [{{form}}], "loop": {{loop}} }""";

    /// <summary>A small ensemble: voice-led pads, bass, arpeggio, a melody with a harmony and a doubling, drums, hits, drips, a texture, swing and a bar filter; two sections, looping to the second.</summary>
    static readonly string Ensemble = Song("synth-ensemble", 150, 2, 2, """
        "intro": { "chords": "1 | 4", "dyn": 0.8, "dynTo": 1, "layers": [
          { "t": "pad", "i": "strings", "o": 0, "v": 0.5, "n": 3 },
          { "t": "tex", "kind": "wind", "v": 0.8 },
          { "t": "hit", "i": "swell", "len": 2, "v": 0.6 } ] },
        "A": { "chords": "5 | 1 . | 6", "mel": "5/2 6/1 5/1 | 3/2 2/2 | 1/4", "layers": [
          { "t": "mel", "i": "flute", "v": 0.8, "harm": { "i": "fiddle", "steps": -2, "v": 0.4 }, "dbl": [{ "i": "whistle", "o": 1, "v": 0.3 }] },
          { "t": "pad", "i": "strings", "o": 0, "v": 0.4, "n": 3 },
          { "t": "bass", "i": "cellos", "o": -2, "v": 0.6, "p": "1-5-" },
          { "t": "arp", "i": "harp", "o": 0, "v": 0.5, "p": "15T8", "bars": [1, 2] },
          { "t": "drums", "v": 0.7, "p": { "frame": "x.o.", "shaker": ".x.x" } },
          { "t": "hit", "i": "toll", "deg": "5", "o": -1, "at": 0, "bars": [0] },
          { "t": "drips", "i": "kalimba", "chance": 0.4, "o": 0, "v": 0.6 } ] }
        """, "\"intro\", \"A\"", loop: 1, extra: ", \"swing\": 0.15");

    static IEnumerable<AudioFixture> Synthesizer()
    {
        // ---- Instruments: one note or a short phrase each, mono at 22.05 kHz. ----
        // Bowed: two voices with vibrato (exact); three voices (strings) and tremolo sum three.
        yield return S("synth-violins", 1, 2.5, 22050, Theme("synth-violins", 60, "1 | 1", """{ "t": "drone", "i": "violins", "deg": ["5"], "every": 2 }"""), 1, Exact);
        yield return S("synth-strings", 1, 2.2, 22050, Theme("synth-strings", 60, "1 | 1", """{ "t": "drone", "i": "strings", "deg": ["1", "5"], "every": 2, "v": 0.8 }"""), 1, ShuffledSums);
        yield return S("synth-tremolo", 1, 2, 22050, Theme("synth-tremolo", 60, "1 | 1", """{ "t": "drone", "i": "tremolo", "deg": ["3"], "every": 2 }"""), 1, ShuffledSums);
        yield return S("synth-fiddle", 1, 2.5, 22050, Song("synth-fiddle", 120, 2, 1, """
            "A": { "chords": "1 | 1", "mel": "1/1 3/1! | 5/2", "layers": [{ "t": "mel", "i": "fiddle", "v": 0.9 }] }
            """, "\"A\""), 4, ShuffledSums);
        // Spiccato: the decaying envelope (setTargetAtTime to 30 %).
        yield return S("synth-spiccato", 1, 2.2, 22050, Song("synth-spiccato", 120, 4, 2, """
            "A": { "chords": "1", "layers": [{ "t": "pulse", "i": "spiccato", "o": 0, "v": 0.8, "p": "1.5.8.5." }] }
            """, "\"A\""), 8, Exact);
        // Brass: the filter's exponential sweep and the voices' detune ramp, with vibrato.
        yield return S("synth-horns", 1, 2.6, 22050, Song("synth-horns", 90, 2, 1, """
            "A": { "chords": "1 | 4", "mel": "5/1 6/1 | 5/2", "layers": [{ "t": "mel", "i": "horns", "o": -1, "v": 0.9 }] }
            """, "\"A\""), 4, ShuffledSums);
        // Choir: voices into a formant bank.
        yield return S("synth-choir", 1, 3, 22050, Theme("synth-choir", 60, "1 | 1", """{ "t": "drone", "i": "choir", "deg": ["1"], "every": 2 }"""), 1, ShuffledSums);
        // Organ and winds: periodic waves from harmonics; the flute's breath noise and chiff; a doubling.
        yield return S("synth-organ", 1, 2.2, 22050, Theme("synth-organ", 60, "1 | 1", """{ "t": "drone", "i": "organ", "deg": ["5"], "every": 2 }"""), 1, Exact);
        yield return S("synth-flute", 1, 2.4, 22050, Song("synth-flute", 100, 2, 1, """
            "A": { "chords": "1 | 1", "mel": "1/1 2/1 | 3/2", "layers": [{ "t": "mel", "i": "flute", "o": 1, "v": 0.9 }] }
            """, "\"A\""), 4, Exact);
        yield return S("synth-oboe", 1, 2.4, 22050, Song("synth-oboe", 100, 2, 1, """
            "A": { "chords": "1 | 1", "mel": "5/1 4/1 | 3/2", "layers": [{ "t": "mel", "i": "oboe", "v": 0.8, "dbl": [{ "i": "duduk", "o": -1, "v": 0.5 }] }] }
            """, "\"A\""), 4, Exact);
        // Plucked strings: Karplus-Strong buffers at a playback rate; body and highpass filters.
        yield return S("synth-harp", 1, 2.6, 22050, Song("synth-harp", 72, 2, 3, """
            "A": { "chords": "1 | 4", "layers": [{ "t": "arp", "i": "harp", "o": 0, "v": 0.7, "p": "15T8T5" }] }
            """, "\"A\""), 12, ShuffledSums);
        yield return S("synth-lute", 1, 2.2, 22050, Song("synth-lute", 72, 2, 3, """
            "A": { "chords": "1", "layers": [{ "t": "arp", "i": "lute", "o": -1, "v": 0.8, "p": "1..5.." }] }
            """, "\"A\""), 6, Exact);
        yield return S("synth-harpsichord", 1, 2.2, 22050, Song("synth-harpsichord", 120, 4, 2, """
            "A": { "chords": "1", "layers": [{ "t": "arp", "i": "harpsichord", "o": 0, "v": 0.7, "p": "1.5.8.5." }, { "t": "bass", "i": "pizz", "o": -2, "v": 0.7, "p": "1...5..." }] }
            """, "\"A\""), 8, Exact);
        // Mallets and glass: partials summed; the kalimba's high partials past the clamped Nyquist are skipped.
        yield return S("synth-celesta", 1, 2.4, 22050, Song("synth-celesta", 100, 2, 1, """
            "A": { "chords": "1 | 1", "mel": "5/1 3/1 | 1/2", "layers": [{ "t": "mel", "i": "celesta", "o": 1, "v": 0.8, "dbl": [{ "i": "marimba", "o": -1, "v": 0.6 }] }] }
            """, "\"A\""), 4, ShuffledSums);
        yield return S("synth-bells", 1, 2.4, 22050, Song("synth-bells", 120, 2, 1, """
            "A": { "chords": "1 | 1", "mel": "5/2 | 1'/2", "layers": [{ "t": "mel", "i": "bell", "o": 1, "v": 0.8 }, { "t": "drips", "i": "kalimba", "chance": 1, "o": 1, "v": 0.9 }] }
            """, "\"A\""), 4, ShuffledSums);
        yield return S("synth-glass", 1, 2.4, 22050, Theme("synth-glass", 60, "1 | 1", """{ "t": "drone", "i": "glass", "deg": ["1"], "every": 2 }"""), 1, ShuffledSums);
        yield return S("synth-sub", 1, 2, 22050, Song("synth-sub", 100, 4, 2, """
            "A": { "chords": "1", "layers": [{ "t": "bass", "i": "sub", "o": -2, "v": 0.9, "p": "1-..5-.." }] }
            """, "\"A\""), 8, Exact);

        // ---- Percussion: every voice of the composer's kit, one hit each, at 22.05 kHz. ----
        yield return S("synth-skins", 1, 3, 22050, Song("synth-skins", 120, 4, 2, """
            "A": { "chords": "1", "layers": [{ "t": "drums", "v": 0.8, "p": { "frame": "x.......", "tom": "..x.....", "dum": "....x...", "boom": "......x." } }] }
            """, "\"A\""), 8, Exact);
        yield return S("synth-war-drums", 1, 2.4, 22050, Song("synth-war-drums", 120, 4, 2, """
            "A": { "chords": "1", "layers": [{ "t": "drums", "v": 0.8, "p": { "taiko": "X...o...", "war": "..x...f." } }] }
            """, "\"A\""), 8, ShuffledSums);
        yield return S("synth-noises", 1, 3.2, 22050, Song("synth-noises", 120, 4, 2, """
            "A": { "chords": "1", "layers": [{ "t": "drums", "v": 0.8, "p": { "tek": "x.......", "snare": ".x......", "muffled": "..x.....", "hat": "...x....", "shaker": "....x...", "clack": ".....x..", "crash": "......x." } }] }
            """, "\"A\""), 8, Exact);
        yield return S("synth-tuned-drums", 1, 2.8, 22050, Song("synth-tuned-drums", 120, 4, 2, """
            "A": { "chords": "1 | 5", "layers": [{ "t": "drums", "v": 0.8, "p": { "log": "x.5.....", "timp": "....x..." } }, { "t": "hit", "i": "timp", "at": 6, "v": 0.6, "bars": [1] }] }
            """, "\"A\""), 16, ShuffledSums);
        yield return S("synth-metals", 1, 2.8, 22050, Song("synth-metals", 120, 4, 2, """
            "A": { "chords": "1", "layers": [{ "t": "drums", "v": 0.8, "p": { "gong": "x.......", "anvil": "..x.....", "zill": "....x..." } }, { "t": "hit", "i": "toll", "deg": "5", "o": -1, "at": 6, "v": 0.7 }] }
            """, "\"A\""), 8, ShuffledSums);
        yield return S("synth-swell-thunder", 1, 3, 22050, Song("synth-swell-thunder", 120, 4, 2, """
            "A": { "chords": "1", "layers": [{ "t": "hit", "i": "swell", "len": 4, "v": 0.8 }, { "t": "hit", "i": "thunder", "at": 4, "v": 0.8 }] }
            """, "\"A\""), 8, ShuffledSums);
        yield return S("synth-rolls", 1, 2.2, 22050, Song("synth-rolls", 120, 4, 2, """
            "A": { "chords": "1", "layers": [{ "t": "drums", "v": 0.8, "p": { "snare": "r...f..X", "frame": "o.x.r..." } }] }
            """, "\"A\""), 8, ShuffledSums);

        // ---- Textures: the six beds in turn, half a second each (each lasts 0.6 s longer). ----
        yield return S("synth-textures", 1, 3.8, 22050, Song("synth-textures", 120, 1, 1, """
            "wind": { "chords": "1", "layers": [{ "t": "tex", "kind": "wind", "v": 1 }] },
            "rain": { "chords": "1", "layers": [{ "t": "tex", "kind": "rain", "v": 1 }] },
            "surf": { "chords": "1", "layers": [{ "t": "tex", "kind": "surf", "v": 1 }] },
            "embers": { "chords": "1", "layers": [{ "t": "tex", "kind": "embers", "v": 1 }] },
            "leaves": { "chords": "1", "layers": [{ "t": "tex", "kind": "leaves", "v": 1 }] },
            "drips": { "chords": "1", "layers": [{ "t": "tex", "kind": "drips", "v": 1 }] }
            """, "\"wind\", \"rain\", \"surf\", \"embers\", \"leaves\", \"drips\""), 6, ShuffledSums);

        // ---- The sequencer: an ensemble in stereo, played past its end so the form loops; strums and lines. ----
        yield return S("synth-ensemble", 2, 4.4, 22050, Ensemble, 24, ShuffledSums);
        yield return S("synth-strum-line", 2, 2.6, 22050, Song("synth-strum-line", 120, 4, 2, """
            "A": { "chords": "1 | 6", "layers": [
              { "t": "strum", "i": "guitar", "o": -1, "v": 0.7, "p": "d...u..D" },
              { "t": "line", "i": "whistle", "o": 1, "v": 0.6, "notes": "5/2 _/2 3/4 | 1/8 | 2/4 _/4", "role": "quote" },
              { "t": "drone", "i": "contrabass", "o": -2, "v": 0.6, "deg": ["1"], "every": 16 } ] }
            """, "\"A\""), 16, ShuffledSums);

        // ---- A long room at 44.1 kHz: the cathedral's 4.2 s response behind one plucked note. ----
        yield return S("synth-cathedral", 2, 1.6, 44100, Song("synth-cathedral", 60, 1, 1, """
            "A": { "chords": "1", "layers": [{ "t": "arp", "i": "harp", "o": 0, "v": 0.8, "p": "5" }] }
            """, "\"A\"", room: "cathedral"), 1, Exact);

        // ---- music.js: the master chain at export loudness; switching themes with fades and the volume. ----
        // The ensemble louder (gain 2.5), so the limiter works.
        var loud = Ensemble.Replace("synth-ensemble", "synth-master").Replace("\"room\": \"chamber\"", "\"room\": \"chamber\", \"gain\": 2.5");
        yield return F("synth-master", 2, (int)Math.Ceiling(4.4 * 22050), 22050, "const volume = 1;\n" + MasterChain + $$"""
            const compiled = MusicComposer.compile({{loud}});
            const performance = MusicComposer.perform(ctx, compiled, master);
            for (let i = 0; i < 24; i++) performance.step(i, 0.05 + i * compiled.stepSeconds);
            """, r =>
        {
            var soundtrack = new Soundtrack(r.G, r.G.Destination, TestMusic.Library, export: true);
            var compiled = Score.Compile(ThemeJson.Parse(loud));
            var performance = soundtrack.Perform(compiled);
            for (var i = 0; i < 24; i++)
                performance.Step(i, 0.05 + i * compiled.StepSeconds);
        }, ShuffledSums) with { Quanta = true };

        var first = Theme("synth-fade-a", 120, "1 | 4 | 5 | 1", """{ "t": "pad", "i": "violins", "o": 0, "v": 0.7, "n": 2 }, { "t": "bass", "i": "sub", "o": -2, "v": 0.7, "p": "1" }""");
        var second = Theme("synth-fade-b", 100, "6 | 4", """{ "t": "drone", "i": "organ", "o": 0, "v": 0.6, "deg": ["1"], "every": 2 }, { "t": "drums", "v": 0.6, "p": { "frame": "x." } }""");
        yield return F("synth-fades", 2, (int)Math.Ceiling(4.6 * 22050), 22050, "let volume = 1;\n" + MasterChain + MusicFunctions + $$"""
            play("a", {{first}});
            quantum(schedule);
            at(0.7, () => setVolume(0.5));
            at(1.2, () => play("a", {{first}}));
            at(1.5, () => play("b", {{second}}));
            at(2.9, () => stop());
            """, r =>
        {
            var soundtrack = new Soundtrack(r.G, r.G.Destination, TestMusic.Library);
            var a = Score.Compile(ThemeJson.Parse(first));
            var b = Score.Compile(ThemeJson.Parse(second));
            soundtrack.Play("a", a);
            r.EveryQuantum.Add(soundtrack.Schedule);
            r.Suspend(0.7, () => soundtrack.Volume = 0.5);
            r.Suspend(1.2, () => soundtrack.Play("a", a));
            r.Suspend(1.5, () => soundtrack.Play("b", b));
            r.Suspend(2.9, soundtrack.Stop);
        }, ShuffledSums) with { Quanta = true };

        // ---- The audio system's mixer (step 7): music.js's chain into the master bus, a tone on the effects bus, every volume ramped. ----
        var busTheme = Theme("synth-buses", 120, "1 | 5", """{ "t": "pad", "i": "violins", "o": 0, "v": 0.8, "n": 2 }, { "t": "bass", "i": "sub", "o": -2, "v": 0.7, "p": "1" }""");
        yield return F("output-buses", 2, (int)Math.Ceiling(1.2 * 22050), 22050, """
            let volume = 1;
            const bus = ctx.createGain();
            bus.connect(ctx.destination);
            const effects = ctx.createGain();
            effects.connect(bus);

            """ + MasterChain.Replace("limiter.connect(ctx.destination);", "limiter.connect(bus);") + MusicFunctions + $$"""
            const tone = ctx.createOscillator();
            tone.frequency.value = 660;
            const toneLevel = ctx.createGain();
            toneLevel.gain.value = 0.2;
            tone.connect(toneLevel);
            toneLevel.connect(effects);
            tone.start(0);
            const ramp = (param, value) => {
              const now = ctx.currentTime;
              param.cancelScheduledValues(now);
              param.setValueAtTime(param.value, now);
              param.linearRampToValueAtTime(value, now + 0.04);
            };
            play("a", {{busTheme}});
            quantum(schedule);
            at(0.3, () => ramp(bus.gain, 0.5));
            at(0.45, () => ramp(effects.gain, 0.2));
            at(0.6, () => setVolume(0.4));
            at(0.8, () => ramp(bus.gain, 1));
            """, r =>
        {
            var system = new AudioSystem(r.G, TestMusic.Library);
            var tone = new OscillatorNode(r.G);
            tone.Frequency.Value = 660;
            var toneLevel = r.Gain(0.2f);
            tone.Connect(toneLevel);
            toneLevel.Connect(system.EffectsBus);
            tone.Start(0);
            system.Music.Play("a", Score.Compile(ThemeJson.Parse(busTheme)));
            r.EveryQuantum.Add(system.BeforeQuantum);
            r.Suspend(0.3, () => system.MasterVolume = 0.5);
            r.Suspend(0.45, () => system.EffectsVolume = 0.2);
            r.Suspend(0.6, () => system.Music.Volume = 0.4);
            r.Suspend(0.8, () => system.MasterVolume = 1);
        }, ShuffledSums) with { Quanta = true };
    }
}

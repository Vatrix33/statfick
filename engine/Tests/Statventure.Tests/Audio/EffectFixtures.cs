using System.Globalization;
using System.Text.Json;
using Statventure.Audio;

namespace Statventure.Tests.Audio;

/// <summary>A patch written as JSON, read into the engine's records (the fixtures' JavaScript reads the same text).</summary>
internal static class EffectJson
{
    public static EffectPatch Parse(string json)
    {
        using var document = JsonDocument.Parse(json);
        var p = document.RootElement;
        double? Num(JsonElement o, string key) => o.TryGetProperty(key, out var v) ? v.GetDouble() : null;
        EffectRange? Range(string key) => p.TryGetProperty(key, out var r) ? new EffectRange(r.GetProperty("min"u8).GetDouble(), r.GetProperty("max"u8).GetDouble()) : null;
        EffectFilter? Filter(JsonElement l)
        {
            if (!l.TryGetProperty("filter"u8, out var f))
                return null;
            var filter = new EffectFilter
            {
                Type = Enum.Parse<EffectFilterType>(f.GetProperty("type"u8).GetString()!, ignoreCase: true),
                Frequency = f.GetProperty("frequency"u8).GetDouble(),
                FrequencyTo = Num(f, "frequencyTo"), SweepTime = Num(f, "sweepTime"), Gain = Num(f, "gain"),
            };
            return Num(f, "q") is { } q ? filter with { Q = q } : filter;
        }
        EffectLayer Layer(JsonElement l)
        {
            var layer = new EffectLayer
            {
                Source = Enum.Parse<EffectSource>(l.GetProperty("source"u8).GetString()!, ignoreCase: true),
                FrequencyTo = Num(l, "frequencyTo"), SweepTime = Num(l, "sweepTime"),
                Harmonics = l.TryGetProperty("harmonics"u8, out var h) ? [.. h.EnumerateArray().Select(x => x.GetDouble())] : null,
                Filter = Filter(l), Length = l.GetProperty("length"u8).GetDouble(),
            };
            // A key left out keeps the record's default, which the script's own default then checks.
            if (Num(l, "frequency") is { } frequency)
                layer = layer with { Frequency = frequency };
            if (Num(l, "delay") is { } delay)
                layer = layer with { Delay = delay };
            if (Num(l, "attack") is { } attack)
                layer = layer with { Attack = attack };
            if (Num(l, "level") is { } level)
                layer = layer with { Level = level };
            return layer;
        }
        var patch = new EffectPatch
        {
            Layers = [.. p.GetProperty("layers"u8).EnumerateArray().Select(Layer)],
            Pitch = Range("pitch"), Volume = Range("volume"),
        };
        if (Num(p, "level") is { } patchLevel)
            patch = patch with { Level = patchLevel };
        if (p.TryGetProperty("maxVoices"u8, out var m))
            patch = patch with { MaxVoices = m.GetInt32() };
        return patch;
    }
}

internal static partial class AudioFixtures
{
    /// <summary>
    /// SoundEffects' graph on Web Audio, with the same defaults, draws and voice limits (the old game has
    /// no effects to take this from, so this states the graph; what the fixtures check is that the
    /// engine's nodes render it as Chromium's do).
    /// </summary>
    const string EffectFunctions = """
        const fxNoise = (() => { const b = ctx.createBuffer(1, ctx.sampleRate * 2, ctx.sampleRate), d = b.getChannelData(0), r = mulberry(9001); for (let i = 0; i < d.length; i++) d[i] = r() * 2 - 1; return b; })();
        const fxRandom = mulberry(7001);
        const fxVoices = [];
        const fxHz = value => Math.max(30, Math.min(ctx.sampleRate / 2 - 200, value));
        const fxDraw = range => range ? range.min + fxRandom() * (range.max - range.min) : 1;
        const fxSteal = voice => {
          const now = ctx.currentTime;
          voice.gain.gain.cancelAndHoldAtTime(now);
          voice.gain.gain.linearRampToValueAtTime(0, now + 0.005);
          for (const source of voice.sources) source.stop(now + 0.005);
          fxVoices.splice(fxVoices.indexOf(voice), 1);
        };
        const fxPlay = (id, patch, volume = 1, pan = 0, pitch = 1) => {
          const maxVoices = patch.maxVoices ?? 4;
          while (fxVoices.filter(v => v.id === id).length >= maxVoices) fxSteal(fxVoices.find(v => v.id === id));
          while (fxVoices.length >= 32) fxSteal(fxVoices[0]);
          pitch *= fxDraw(patch.pitch);
          const level = (patch.level ?? 1) * volume * fxDraw(patch.volume);
          const when = ctx.currentTime;
          const voiceGain = ctx.createGain();
          voiceGain.gain.value = level;
          const panner = ctx.createStereoPanner();
          panner.pan.value = Math.max(-1, Math.min(1, pan));
          voiceGain.connect(panner);
          panner.connect(ctx.destination);
          const nodes = [voiceGain, panner], sources = [];
          let last = null, lastStop = -Infinity;
          for (const layer of patch.layers) {
            const attack = layer.attack ?? 0.002, start = when + (layer.delay ?? 0), stop = start + attack + layer.length + 0.05;
            let source;
            if (layer.source === "noise") {
              source = ctx.createBufferSource();
              source.buffer = fxNoise;
              source.loop = true;
              source.start(start, fxRandom() * 1.5);
            } else {
              source = ctx.createOscillator();
              if (layer.source === "harmonics") source.setPeriodicWave(ctx.createPeriodicWave(new Float32Array(layer.harmonics.length), Float32Array.from(layer.harmonics)));
              else source.type = layer.source;
              source.frequency.setValueAtTime(fxHz((layer.frequency ?? 440) * pitch), start);
              if (layer.frequencyTo !== undefined) source.frequency.exponentialRampToValueAtTime(fxHz(layer.frequencyTo * pitch), start + (layer.sweepTime ?? layer.length * 0.6));
              source.start(start);
            }
            source.stop(stop);
            let head = source;
            if (layer.filter) {
              const f = layer.filter, filter = ctx.createBiquadFilter();
              filter.type = f.type;
              filter.frequency.setValueAtTime(fxHz(f.frequency * pitch), start);
              if (f.frequencyTo !== undefined) filter.frequency.exponentialRampToValueAtTime(fxHz(f.frequencyTo * pitch), start + (f.sweepTime ?? layer.length * 0.6));
              filter.Q.value = f.q ?? 1;
              if (f.gain !== undefined) filter.gain.value = f.gain;
              head.connect(filter);
              head = filter;
              nodes.push(filter);
            }
            const envelope = ctx.createGain();
            envelope.gain.setValueAtTime(0.0001, start);
            envelope.gain.linearRampToValueAtTime(layer.level ?? 1, start + attack);
            envelope.gain.setTargetAtTime(0, start + attack, layer.length / 4);
            head.connect(envelope);
            envelope.connect(voiceGain);
            nodes.push(source, envelope);
            sources.push(source);
            if (stop > lastStop) { last = source; lastStop = stop; }
          }
          const voice = { id, gain: voiceGain, sources };
          fxVoices.push(voice);
          last.onended = () => {
            const index = fxVoices.indexOf(voice);
            if (index >= 0) fxVoices.splice(index, 1);
            for (const node of nodes) node.disconnect();
          };
        };

        """;

    readonly record struct EffectPlay(double Time, string Id, double Volume = 1, double Pan = 0, double Pitch = 1);

    /// <summary>Patches by id and plays at given times, in Chromium and on the audio system's effects.</summary>
    static AudioFixture Fx(string name, double seconds, (string Id, string Json)[] patches, EffectPlay[] plays, double tolerance)
    {
        static string N(double value) => value.ToString("R", CultureInfo.InvariantCulture);
        var script = EffectFunctions + string.Concat(patches.Select((p, i) => $"const patch{i} = {p.Json};\n")) + string.Concat(plays.Select(play =>
        {
            var index = Array.FindIndex(patches, p => p.Id == play.Id);
            var call = $"fxPlay(\"{play.Id}\", patch{index}, {N(play.Volume)}, {N(play.Pan)}, {N(play.Pitch)})";
            return play.Time == 0 ? call + ";\n" : $"at({N(play.Time)}, () => {call});\n";
        }));
        return F(name, 2, (int)Math.Ceiling(seconds * 22050), 22050, script, r =>
        {
            var system = new AudioSystem(r.G);
            foreach (var (id, json) in patches)
                system.Effects.Register(id, EffectJson.Parse(json));
            foreach (var play in plays)
                if (play.Time == 0)
                    system.Effects.Play(play.Id, play.Volume, play.Pan, play.Pitch);
                else
                    r.Suspend(play.Time, () => system.Effects.Play(play.Id, play.Volume, play.Pan, play.Pitch));
            system.BeforeQuantum();
            r.EveryQuantum.Add(system.BeforeQuantum);
        }, tolerance) with { Quanta = true };
    }

    static IEnumerable<AudioFixture> Effects()
    {
        // A layer and a filter on all their defaults.
        yield return Fx("fx-defaults", 0.4, [("a", """{ "layers": [{ "source": "triangle", "filter": { "type": "lowpass", "frequency": 2000 }, "length": 0.1 }] }""")], [new(0, "a")], Exact);
        // Tones: a sine sweeping down and a delayed triangle; square and sawtooth with an explicit sweep time.
        yield return Fx("fx-tones", 0.8, [("a", """{ "layers": [{ "source": "sine", "frequency": 880, "frequencyTo": 220, "attack": 0.004, "length": 0.4 }, { "source": "triangle", "frequency": 330, "delay": 0.1, "level": 0.5, "length": 0.3 }] }""")], [new(0, "a")], Exact);
        yield return Fx("fx-waves", 0.7, [("a", """{ "layers": [{ "source": "square", "frequency": 220, "frequencyTo": 440, "sweepTime": 0.05, "level": 0.4, "length": 0.2 }, { "source": "sawtooth", "frequency": 110, "delay": 0.15, "level": 0.5, "length": 0.3 }], "level": 0.6 }""")], [new(0, "a")], Exact);
        // Harmonics through a filter that sweeps.
        yield return Fx("fx-harmonics", 0.7, [("a", """{ "layers": [{ "source": "harmonics", "harmonics": [0, 1, 0.5, 0.25, 0.1], "frequency": 262, "filter": { "type": "lowpass", "frequency": 4000, "frequencyTo": 400, "sweepTime": 0.3, "q": 3 }, "length": 0.4 }] }""")], [new(0, "a")], Exact);
        // Noise: bandpass sweeping, highpass, a boosted peak, a shelf; two patches overlapping.
        yield return Fx("fx-noise", 0.9, [
            ("a", """{ "layers": [{ "source": "noise", "filter": { "type": "bandpass", "frequency": 2000, "frequencyTo": 500, "q": 4 }, "length": 0.3 }, { "source": "noise", "filter": { "type": "highpass", "frequency": 5000 }, "attack": 0.001, "level": 0.5, "length": 0.03 }] }"""),
            ("b", """{ "layers": [{ "source": "noise", "filter": { "type": "peaking", "frequency": 800, "q": 2, "gain": 9 }, "level": 0.3, "length": 0.2 }, { "source": "noise", "filter": { "type": "lowshelf", "frequency": 300, "gain": -6 }, "level": 0.2, "delay": 0.05, "length": 0.2 }] }"""),
        ], [new(0, "a"), new(0.25, "b")], Exact);
        // A ringing filter: the voice's cleanup at the end of its last quantum cuts what still rings.
        yield return Fx("fx-cleanup", 0.5, [("a", """{ "layers": [{ "source": "noise", "filter": { "type": "bandpass", "frequency": 700, "q": 40 }, "level": 3, "length": 0.02 }] }""")], [new(0, "a")], Exact);
        // The caller's volume, pan and pitch (a filter's frequency scales too, noise's doesn't).
        yield return Fx("fx-play-arguments", 0.9, [("a", """{ "layers": [{ "source": "sine", "frequency": 500, "frequencyTo": 250, "length": 0.15 }, { "source": "noise", "filter": { "type": "bandpass", "frequency": 1500, "q": 2 }, "level": 0.4, "length": 0.1 }] }""")], [new(0, "a", 0.5, -0.7, 1.5), new(0.3, "a", 1, 0.4, 0.5), new(0.6, "a", 0.8, 3, 1)], Exact);
        // Per-play ranges: the pitch's draw, the volume's, then each noise layer's offset.
        yield return Fx("fx-variation", 0.9, [("a", """{ "layers": [{ "source": "triangle", "frequency": 600, "length": 0.1 }, { "source": "noise", "filter": { "type": "highpass", "frequency": 3000 }, "level": 0.3, "length": 0.05 }], "pitch": { "min": 0.8, "max": 1.25 }, "volume": { "min": 0.4, "max": 1 } }""")], [new(0, "a"), new(0.25, "a"), new(0.5, "a")], Exact);
        // Noise looping past the end of its two seconds: a long layer from an offset near the end (the first draw is 1.45 s, so it wraps after 0.55 s).
        yield return Fx("fx-noise-long", 1.5, [("a", """{ "layers": [{ "source": "noise", "filter": { "type": "lowpass", "frequency": 1200 }, "level": 0.5, "length": 1.3 }, { "source": "noise", "filter": { "type": "highpass", "frequency": 4000 }, "level": 0.2, "length": 1.3 }] }""")], [new(0, "a")], Exact);
        // Frequencies past the rate's Nyquist less 200 Hz are clamped, as the composer clamps them, also after the pitch.
        yield return Fx("fx-clamped", 0.5, [("a", """{ "layers": [{ "source": "sine", "frequency": 9000, "frequencyTo": 20000, "sweepTime": 0.1, "level": 0.3, "length": 0.2 }, { "source": "noise", "filter": { "type": "lowpass", "frequency": 9000 }, "level": 0.2, "length": 0.2 }] }""")], [new(0, "a", 1, 0, 1.5)], Exact);
        // A patch's limit: the third play makes the first fade out over 5 ms.
        yield return Fx("fx-patch-limit", 0.8, [("a", """{ "layers": [{ "source": "sine", "frequency": 440, "length": 0.6 }], "maxVoices": 2, "level": 0.4 }""")], [new(0, "a"), new(0.05, "a", 1, 0, 1.25), new(0.1, "a", 1, 0, 1.5)], ShuffledSums);
        // The overall limit: 34 patches in turn, the oldest fading as the 33rd and 34th start.
        var many = Enumerable.Range(0, 34).Select(i => ($"p{i}", $$"""{ "layers": [{ "source": "sine", "frequency": {{200 + i * 37}}, "length": 0.5 }], "level": 0.05 }""")).ToArray();
        yield return Fx("fx-overall-limit", 0.8, many, [.. Enumerable.Range(0, 34).Select(i => new EffectPlay(i * 0.006, $"p{i}"))], ShuffledSums);
    }
}

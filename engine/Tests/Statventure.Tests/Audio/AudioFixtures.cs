using Statventure.Audio;

namespace Statventure.Tests.Audio;

/// <summary>
/// A Web Audio graph rendered offline: <see cref="Script"/> builds it on `ctx` in Chromium 152
/// (Tools/AudioReference), <see cref="Build"/> builds its twin on the engine's nodes.
/// <see cref="Tolerance"/> is the largest difference a sample may show, worked out from float32
/// precision for what Chromium leaves undetermined (the order its inputs sum in); 0 means the
/// smallest normal float (Chromium flushes denormals to zero, the engine mostly doesn't);
/// <see cref="AudioFixtures.Exact"/> means no difference at all.
/// </summary>
internal sealed record AudioFixture(string Name, int Channels, int Length, float Rate, string Script, Action<AudioFixtureRun> Build, double Tolerance = 0);

/// <summary>A fixture's twin being built: its graph, and code to run while the render is suspended (OfflineAudioContext.suspend).</summary>
internal sealed class AudioFixtureRun(AudioGraph graph)
{
    public AudioGraph Graph { get; } = graph;
    public AudioGraph G => Graph;
    internal SortedList<long, List<Action>> Suspends { get; } = [];

    /// <summary>ctx.suspend(when).then(...): <paramref name="when"/> · rate truncated to a frame, rounded up to a quantum.</summary>
    public void Suspend(double when, Action action)
    {
        var frame = (long)(when * Graph.SampleRateF);
        const int Q = AudioGraph.RenderQuantumFrames;
        frame = Q * ((frame + Q - 1) / Q);
        if (!Suspends.TryGetValue(frame, out var actions))
            Suspends[frame] = actions = [];
        actions.Add(action);
    }

    public AudioBufferSourceNode Source(AudioBuffer buffer)
    {
        var source = new AudioBufferSourceNode(Graph) { Buffer = buffer };
        return source;
    }

    public GainNode Gain(float value)
    {
        var gain = new GainNode(Graph);
        gain.Gain.Value = value;
        return gain;
    }

    public GainNode Gain() => new(Graph);

    /// <summary>The fixtures' room response: noise decaying by <paramref name="decay"/> a frame (in double, as the script does).</summary>
    public AudioBuffer Room(int channels, int length, double seed, double decay)
    {
        var buffer = new AudioBuffer(channels, length, Graph.SampleRateF);
        for (var c = 0; c < channels; c++)
        {
            var random = Mulberry32((uint)(seed + c * 7919));
            var data = buffer.GetChannelData(c);
            var amplitude = 1.0;
            for (var i = 0; i < length; i++)
            {
                data[i] = (float)((random() * 2 - 1) * amplitude);
                amplitude *= decay;
            }
        }
        return buffer;
    }

    public ConvolverNode Convolver(AudioBuffer buffer, bool normalize = true)
    {
        var convolver = new ConvolverNode(Graph) { Normalize = normalize };
        convolver.Buffer = buffer;
        return convolver;
    }

    public StereoPannerNode Panner(float pan)
    {
        var panner = new StereoPannerNode(Graph);
        panner.Pan.Value = pan;
        return panner;
    }

    public BiquadFilterNode Filter(BiquadFilterType type, float frequency)
    {
        var filter = new BiquadFilterNode(Graph, type);
        filter.Frequency.Value = frequency;
        return filter;
    }

    public OscillatorNode Osc(OscillatorType type, float frequency)
    {
        var osc = new OscillatorNode(Graph, type);
        osc.Frequency.Value = frequency;
        return osc;
    }

    /// <summary>The fixtures' DC source: a buffer of 128 ones looped from 0, so a gain after it outputs its own values.</summary>
    public AudioBufferSourceNode Dc()
    {
        var buffer = new AudioBuffer(1, 128, Graph.SampleRateF);
        buffer.GetChannelData(0).AsSpan().Fill(1);
        var source = new AudioBufferSourceNode(Graph) { Buffer = buffer, Loop = true };
        source.Start(0);
        return source;
    }

    /// <summary>The fixtures' noise: the composer's mulberry32, each channel seeded seed + c · 7919, samples random · 2 − 1.</summary>
    public AudioBuffer Noise(int channels, int length, double seed, float? rate = null)
    {
        var buffer = new AudioBuffer(channels, length, rate ?? Graph.SampleRateF);
        for (var c = 0; c < channels; c++)
        {
            var random = Mulberry32((uint)(seed + c * 7919));
            var data = buffer.GetChannelData(c);
            for (var i = 0; i < data.Length; i++)
                data[i] = (float)(random() * 2 - 1);
        }
        return buffer;
    }

    /// <summary>music-composer.js's mulberry(seed), in uint arithmetic (the same bits as its Math.imul and int32 steps).</summary>
    public static Func<double> Mulberry32(uint seed)
    {
        var state = seed;
        return () =>
        {
            unchecked
            {
                state += 0x6D2B79F5;
                var t = state;
                t = (t ^ (t >> 15)) * (t | 1);
                t ^= t + (t ^ (t >> 7)) * (t | 61);
                return (t ^ (t >> 14)) / 4294967296.0;
            }
        };
    }
}

internal static class AudioFixtures
{
    /// <summary>JavaScript helpers every fixture script starts with, the twins of <see cref="AudioFixtureRun"/>'s.</summary>
    const string Prelude = """
        const mulberry = seed => { let state = seed >>> 0; return () => { state = (state + 0x6D2B79F5) >>> 0; let t = state; t = Math.imul(t ^ (t >>> 15), t | 1); t ^= t + Math.imul(t ^ (t >>> 7), t | 61); return ((t ^ (t >>> 14)) >>> 0) / 4294967296; }; };
        const noise = (channels, length, seed, rate = ctx.sampleRate) => { const b = ctx.createBuffer(channels, length, rate); for (let c = 0; c < channels; c++) { const d = b.getChannelData(c), r = mulberry(seed + c * 7919); for (let i = 0; i < d.length; i++) d[i] = r() * 2 - 1; } return b; };
        const source = buffer => { const s = ctx.createBufferSource(); s.buffer = buffer; return s; };
        const gain = value => { const g = ctx.createGain(); if (value !== undefined) g.gain.value = value; return g; };
        const dc = () => { const b = ctx.createBuffer(1, 128, ctx.sampleRate); b.getChannelData(0).fill(1); const s = source(b); s.loop = true; s.start(0); return s; };
        const out = ctx.destination;

        """;

    /// <summary>
    /// How far two summing orders of <paramref name="terms"/> float32 values of total magnitude at most
    /// <paramref name="magnitude"/> can differ, with one rounding stage after (2n · u · M, u = 2⁻²⁴):
    /// each order's error is at most (n − 1) · u · M, and a following gain adds at most one rounding of each.
    /// </summary>
    static double SumOrder(int terms, double magnitude) => 2 * terms * Math.Pow(2, -24) * magnitude;

    /// <summary>A tolerance of none: for fixtures whose denormals the engine flushes as Chromium does.</summary>
    public const double Exact = -1;

    static AudioFixture F(string name, int channels, int length, float rate, string script, Action<AudioFixtureRun> build, double tolerance = 0) =>
        new(name, channels, length, rate, Prelude + script, build, tolerance);

    static AudioFixture[]? s_all;
    public static AudioFixture[] All => s_all ??= [.. Core(), .. Oscillators(), .. Biquads(), .. Rooms()];

    static IEnumerable<AudioFixture> Core()
    {
        // ---------- Buffer sources ----------

        yield return F("source-mono-to-stereo", 2, 2048, 48000, """
            const s = source(noise(1, 1000, 1)); s.connect(out); s.start(0);
            """, r =>
        {
            var s = r.Source(r.Noise(1, 1000, 1));
            s.Connect(r.G.Destination);
            s.Start(0);
        });

        yield return F("source-start-between-frames", 2, 2048, 48000, """
            const s = source(noise(1, 1500, 2)); s.connect(out); s.start(0.0031234); s.stop(0.0201);
            """, r =>
        {
            var s = r.Source(r.Noise(1, 1500, 2));
            s.Connect(r.G.Destination);
            s.Start(0.0031234);
            s.Stop(0.0201);
        });

        yield return F("source-offset-and-duration", 1, 4096, 44100, """
            const a = source(noise(1, 1200, 3)); a.connect(out); a.start(0.001, 0.0017);
            const b = source(noise(1, 1200, 4)); b.connect(out); b.start(0.03, 0.002, 0.0101);
            """, r =>
        {
            var a = r.Source(r.Noise(1, 1200, 3));
            a.Connect(r.G.Destination);
            a.Start(0.001, 0.0017);
            var b = r.Source(r.Noise(1, 1200, 4));
            b.Connect(r.G.Destination);
            b.Start(0.03, 0.002, 0.0101);
        });

        yield return F("source-loop-points", 2, 4096, 48000, """
            const s = source(noise(1, 1000, 5)); s.loop = true; s.loopStart = 0.00313; s.loopEnd = 0.01577;
            s.connect(out); s.start(0, 0.002); s.stop(0.07);
            """, r =>
        {
            var s = r.Source(r.Noise(1, 1000, 5));
            s.Loop = true;
            s.LoopStart = 0.00313;
            s.LoopEnd = 0.01577;
            s.Connect(r.G.Destination);
            s.Start(0, 0.002);
            s.Stop(0.07);
        });

        yield return F("source-loop-whole", 1, 4096, 48000, """
            const s = source(noise(1, 300, 6)); s.loop = true; s.connect(out); s.start(0.002); s.stop(0.06);
            """, r =>
        {
            var s = r.Source(r.Noise(1, 300, 6));
            s.Loop = true;
            s.Connect(r.G.Destination);
            s.Start(0.002);
            s.Stop(0.06);
        });

        yield return F("source-rate-detune", 2, 6000, 48000, """
            const s = source(noise(1, 2000, 7)); s.loop = true; s.playbackRate.value = 0.7731; s.detune.value = 37;
            s.connect(out); s.start(0.0004);
            """, r =>
        {
            var s = r.Source(r.Noise(1, 2000, 7));
            s.Loop = true;
            s.PlaybackRate.Value = 0.7731f;
            s.Detune.Value = 37;
            s.Connect(r.G.Destination);
            s.Start(0.0004);
        });

        yield return F("source-rate-automation", 1, 12000, 48000, """
            const s = source(noise(1, 3000, 8)); s.loop = true;
            s.playbackRate.setValueAtTime(1.3, 0); s.playbackRate.linearRampToValueAtTime(0.6, 0.1);
            s.detune.setTargetAtTime(-200, 0.02, 0.03); s.detune.setValueAtTime(150, 0.2);
            s.connect(out); s.start(0);
            """, r =>
        {
            var s = r.Source(r.Noise(1, 3000, 8));
            s.Loop = true;
            s.PlaybackRate.SetValueAtTime(1.3f, 0).LinearRampToValueAtTime(0.6f, 0.1);
            s.Detune.SetTargetAtTime(-200, 0.02, 0.03).SetValueAtTime(150, 0.2);
            s.Connect(r.G.Destination);
            s.Start(0);
        });

        yield return F("source-buffer-rate", 1, 4096, 48000, """
            const s = source(noise(1, 800, 9, 22050)); s.connect(out); s.start(0.001);
            """, r =>
        {
            var s = r.Source(r.Noise(1, 800, 9, 22050));
            s.Connect(r.G.Destination);
            s.Start(0.001);
        });

        yield return F("source-reverse", 1, 4096, 48000, """
            const s = source(noise(1, 1000, 10)); s.loop = true; s.playbackRate.value = -0.75;
            s.connect(out); s.start(0, 0.01); s.stop(0.07);
            """, r =>
        {
            var s = r.Source(r.Noise(1, 1000, 10));
            s.Loop = true;
            s.PlaybackRate.Value = -0.75f;
            s.Connect(r.G.Destination);
            s.Start(0, 0.01);
            s.Stop(0.07);
        });

        yield return F("source-stereo-to-mono", 1, 2048, 48000, """
            const s = source(noise(2, 1500, 11)); s.connect(out); s.start(0);
            """, r =>
        {
            var s = r.Source(r.Noise(2, 1500, 11));
            s.Connect(r.G.Destination);
            s.Start(0);
        });

        // ---------- Gains and the parameter timeline ----------

        yield return F("gain-constant", 2, 2048, 48000, """
            const a = source(noise(1, 1800, 12)); const ga = gain(0.3); a.connect(ga).connect(out); a.start(0);
            const b = source(noise(2, 1800, 13)); const gb = gain(0); b.connect(gb).connect(out); b.start(0);
            """, r =>
        {
            var a = r.Source(r.Noise(1, 1800, 12));
            a.Connect(r.Gain(0.3f)).Connect(r.G.Destination);
            a.Start(0);
            var b = r.Source(r.Noise(2, 1800, 13));
            b.Connect(r.Gain(0)).Connect(r.G.Destination);
            b.Start(0);
        });

        yield return F("param-linear", 1, 6000, 48000, """
            const g = gain(); dc().connect(g).connect(out);
            g.gain.setValueAtTime(0.2, 0.01); g.gain.linearRampToValueAtTime(0.9, 0.0503);
            g.gain.linearRampToValueAtTime(-0.4, 0.0731); g.gain.linearRampToValueAtTime(0.25, 0.11);
            const h = gain(); dc().connect(h).connect(out);
            h.gain.linearRampToValueAtTime(0.5, 0.02);
            """, r =>
        {
            var g = r.Gain();
            r.Dc().Connect(g).Connect(r.G.Destination);
            g.Gain.SetValueAtTime(0.2f, 0.01).LinearRampToValueAtTime(0.9f, 0.0503).LinearRampToValueAtTime(-0.4f, 0.0731).LinearRampToValueAtTime(0.25f, 0.11);
            var h = r.Gain();
            r.Dc().Connect(h).Connect(r.G.Destination);
            h.Gain.LinearRampToValueAtTime(0.5f, 0.02);
        });

        yield return F("param-exponential", 1, 6000, 48000, """
            const g = gain(); dc().connect(g).connect(out);
            g.gain.setValueAtTime(0.01, 0.005); g.gain.exponentialRampToValueAtTime(0.8, 0.04);
            g.gain.exponentialRampToValueAtTime(0.05, 0.0901); g.gain.exponentialRampToValueAtTime(-0.3, 0.1);
            g.gain.setValueAtTime(-0.3, 0.105); g.gain.exponentialRampToValueAtTime(-0.01, 0.12);
            """, r =>
        {
            var g = r.Gain();
            r.Dc().Connect(g).Connect(r.G.Destination);
            g.Gain.SetValueAtTime(0.01f, 0.005).ExponentialRampToValueAtTime(0.8f, 0.04).ExponentialRampToValueAtTime(0.05f, 0.0901)
                .ExponentialRampToValueAtTime(-0.3f, 0.1).SetValueAtTime(-0.3f, 0.105).ExponentialRampToValueAtTime(-0.01f, 0.12);
        });

        yield return F("param-target", 1, 12000, 48000, """
            const g = gain(); dc().connect(g).connect(out);
            g.gain.setValueAtTime(0.9, 0); g.gain.setTargetAtTime(0.1, 0.01, 0.015);
            g.gain.setTargetAtTime(0.6, 0.05, 0.005); g.gain.linearRampToValueAtTime(0.2, 0.15);
            g.gain.setTargetAtTime(0, 0.16, 0.002); g.gain.setTargetAtTime(0.4, 0.2, 0);
            """, r =>
        {
            var g = r.Gain();
            r.Dc().Connect(g).Connect(r.G.Destination);
            g.Gain.SetValueAtTime(0.9f, 0).SetTargetAtTime(0.1f, 0.01, 0.015).SetTargetAtTime(0.6f, 0.05, 0.005)
                .LinearRampToValueAtTime(0.2f, 0.15).SetTargetAtTime(0, 0.16, 0.002).SetTargetAtTime(0.4f, 0.2, 0);
        });

        yield return F("param-curve", 1, 6000, 48000, """
            const g = gain(); dc().connect(g).connect(out);
            g.gain.setValueCurveAtTime([0, 0.5, 0.2, 1, -0.3, 0.7], 0.01, 0.0603);
            g.gain.setValueCurveAtTime(new Float32Array([0.1, 0.9, 0.4, 0.45, 0.2, 0.8, 0.3]), 0.08, 0.031);
            g.gain.linearRampToValueAtTime(-0.5, 0.12);
            """, r =>
        {
            var g = r.Gain();
            r.Dc().Connect(g).Connect(r.G.Destination);
            g.Gain.SetValueCurveAtTime([0, 0.5f, 0.2f, 1, -0.3f, 0.7f], 0.01, 0.0603)
                .SetValueCurveAtTime([0.1f, 0.9f, 0.4f, 0.45f, 0.2f, 0.8f, 0.3f], 0.08, 0.031)
                .LinearRampToValueAtTime(-0.5f, 0.12);
        });

        yield return F("param-cancel-scheduled", 1, 6000, 48000, """
            const g = gain(); dc().connect(g).connect(out);
            g.gain.setValueAtTime(0.3, 0.01); g.gain.linearRampToValueAtTime(0.9, 0.04); g.gain.linearRampToValueAtTime(0.1, 0.07);
            g.gain.setValueCurveAtTime([0.2, 0.6, 0.3], 0.08, 0.03);
            g.gain.cancelScheduledValues(0.05);
            const h = gain(); dc().connect(h).connect(out);
            h.gain.setValueCurveAtTime([0.5, -0.5, 0.25], 0.02, 0.06); h.gain.cancelScheduledValues(0.05);
            """, r =>
        {
            var g = r.Gain();
            r.Dc().Connect(g).Connect(r.G.Destination);
            g.Gain.SetValueAtTime(0.3f, 0.01).LinearRampToValueAtTime(0.9f, 0.04).LinearRampToValueAtTime(0.1f, 0.07)
                .SetValueCurveAtTime([0.2f, 0.6f, 0.3f], 0.08, 0.03).CancelScheduledValues(0.05);
            var h = r.Gain();
            r.Dc().Connect(h).Connect(r.G.Destination);
            h.Gain.SetValueCurveAtTime([0.5f, -0.5f, 0.25f], 0.02, 0.06).CancelScheduledValues(0.05);
        });

        // cancelAndHoldAtTime ahead of time, during each kind of automation.
        foreach (var (kind, js, cs) in new (string, string, Action<AudioParam>)[]
        {
            ("linear", "p.setValueAtTime(0.1, 0.01); p.linearRampToValueAtTime(0.9, 0.1);",
                p => p.SetValueAtTime(0.1f, 0.01).LinearRampToValueAtTime(0.9f, 0.1)),
            ("exponential", "p.setValueAtTime(0.05, 0.01); p.exponentialRampToValueAtTime(0.9, 0.1);",
                p => p.SetValueAtTime(0.05f, 0.01).ExponentialRampToValueAtTime(0.9f, 0.1)),
            ("target", "p.setValueAtTime(0.9, 0.01); p.setTargetAtTime(0.1, 0.02, 0.02);",
                p => p.SetValueAtTime(0.9f, 0.01).SetTargetAtTime(0.1f, 0.02, 0.02)),
            ("curve", "p.setValueCurveAtTime([0.1, 0.8, 0.3, 0.6], 0.01, 0.09);",
                p => p.SetValueCurveAtTime([0.1f, 0.8f, 0.3f, 0.6f], 0.01, 0.09)),
        })
        {
            yield return F($"param-cancel-hold-{kind}", 1, 6000, 48000, $$"""
                const g = gain(); dc().connect(g).connect(out); const p = g.gain;
                {{js}}
                p.cancelAndHoldAtTime(0.0517); p.linearRampToValueAtTime(0, 0.1);
                """, r =>
            {
                var g = r.Gain();
                r.Dc().Connect(g).Connect(r.G.Destination);
                cs(g.Gain);
                g.Gain.CancelAndHoldAtTime(0.0517).LinearRampToValueAtTime(0, 0.1);
            });
        }

        // The theme fade (music.js): while rendering, hold the bus where it is now and ramp it out.
        foreach (var (kind, js, cs) in new (string, string, Action<AudioParam>)[]
        {
            ("linear", "p.setValueAtTime(0.1, 0.01); p.linearRampToValueAtTime(0.9, 0.1);",
                p => p.SetValueAtTime(0.1f, 0.01).LinearRampToValueAtTime(0.9f, 0.1)),
            ("target", "p.setValueAtTime(0.9, 0.01); p.setTargetAtTime(0.1, 0.02, 0.02);",
                p => p.SetValueAtTime(0.9f, 0.01).SetTargetAtTime(0.1f, 0.02, 0.02)),
        })
        {
            yield return F($"param-fade-during-{kind}", 1, 8192, 48000, $$"""
                const g = gain(); dc().connect(g).connect(out); const p = g.gain;
                {{js}}
                ctx.suspend(0.04).then(() => { const now = ctx.currentTime; p.cancelAndHoldAtTime(now); p.linearRampToValueAtTime(0, now + 0.055); ctx.resume(); });
                """, r =>
            {
                var g = r.Gain();
                r.Dc().Connect(g).Connect(r.G.Destination);
                cs(g.Gain);
                r.Suspend(0.04, () =>
                {
                    var now = r.G.CurrentTime;
                    g.Gain.CancelAndHoldAtTime(now).LinearRampToValueAtTime(0, now + 0.055);
                });
            });
        }

        yield return F("param-clamped-to-now", 1, 8192, 48000, """
            const g = gain(); dc().connect(g).connect(out);
            g.gain.setValueAtTime(0.3, 0); g.gain.linearRampToValueAtTime(0.8, 0.12);
            ctx.suspend(0.05).then(() => {
              g.gain.setValueAtTime(0.7, 0.01); g.gain.linearRampToValueAtTime(0.2, 0.02);
              ctx.resume();
            });
            ctx.suspend(0.1).then(() => { g.gain.value = 0.25; g.gain.setTargetAtTime(0.9, 0.12, 0.01); ctx.resume(); });
            """, r =>
        {
            var g = r.Gain();
            r.Dc().Connect(g).Connect(r.G.Destination);
            g.Gain.SetValueAtTime(0.3f, 0).LinearRampToValueAtTime(0.8f, 0.12);
            r.Suspend(0.05, () => g.Gain.SetValueAtTime(0.7f, 0.01).LinearRampToValueAtTime(0.2f, 0.02));
            r.Suspend(0.1, () =>
            {
                g.Gain.Value = 0.25f;
                g.Gain.SetTargetAtTime(0.9f, 0.12, 0.01);
            });
        });

        // The first events fall between frames: the frames before them keep the value.
        yield return F("param-first-event-between-frames", 1, 4096, 48000, """
            const g = gain(); dc().connect(g).connect(out);
            g.gain.setValueAtTime(0.3, 0.0100104); g.gain.linearRampToValueAtTime(0.9, 0.0300021);
            const h = gain(); dc().connect(h).connect(out);
            h.gain.setTargetAtTime(0.2, 0.0200104, 0.01);
            """, r =>
        {
            var g = r.Gain();
            r.Dc().Connect(g).Connect(r.G.Destination);
            g.Gain.SetValueAtTime(0.3f, 0.0100104).LinearRampToValueAtTime(0.9f, 0.0300021);
            var h = r.Gain();
            r.Dc().Connect(h).Connect(r.G.Destination);
            h.Gain.SetTargetAtTime(0.2f, 0.0200104, 0.01);
        });

        // A parameter whose only event lies ahead is k-rate until the quantum the event falls in.
        yield return F("param-single-event", 1, 4096, 48000, """
            const g = gain(); dc().connect(g).connect(out); g.gain.setValueAtTime(0.5, 0.0101);
            const h = gain(); dc().connect(h).connect(out); h.gain.setTargetAtTime(0.25, 0.0301, 0.005);
            """, r =>
        {
            var g = r.Gain();
            r.Dc().Connect(g).Connect(r.G.Destination);
            g.Gain.SetValueAtTime(0.5f, 0.0101);
            var h = r.Gain();
            r.Dc().Connect(h).Connect(r.G.Destination);
            h.Gain.SetTargetAtTime(0.25f, 0.0301, 0.005);
        });

        // Events for past times, the later one first: clamped to now as they come, they keep the order they came in.
        yield return F("param-past-events-reversed", 1, 6000, 48000, """
            const g = gain(); dc().connect(g).connect(out); g.gain.setValueAtTime(0.3, 0);
            ctx.suspend(0.05).then(() => {
              g.gain.setValueAtTime(0.7, 0.03); g.gain.linearRampToValueAtTime(0.2, 0.01); g.gain.linearRampToValueAtTime(0.9, 0.09);
              ctx.resume();
            });
            """, r =>
        {
            var g = r.Gain();
            r.Dc().Connect(g).Connect(r.G.Destination);
            g.Gain.SetValueAtTime(0.3f, 0);
            r.Suspend(0.05, () => g.Gain.SetValueAtTime(0.7f, 0.03).LinearRampToValueAtTime(0.2f, 0.01).LinearRampToValueAtTime(0.9f, 0.09));
        });

        // A curve whose end event is cancelled holds its last value until the next event (or for good).
        yield return F("param-curve-end-held", 1, 6000, 48000, """
            const g = gain(); dc().connect(g).connect(out);
            g.gain.setValueCurveAtTime([0.2, 0.9, 0.4], 0.01, 0.03); g.gain.cancelScheduledValues(0.04); g.gain.setValueAtTime(0.1, 0.07);
            const h = gain(); dc().connect(h).connect(out);
            h.gain.setValueCurveAtTime([0.6, -0.2, 0.3], 0.02, 0.0251); h.gain.cancelScheduledValues(0.0451);
            """, r =>
        {
            var g = r.Gain();
            r.Dc().Connect(g).Connect(r.G.Destination);
            g.Gain.SetValueCurveAtTime([0.2f, 0.9f, 0.4f], 0.01, 0.03).CancelScheduledValues(0.04).SetValueAtTime(0.1f, 0.07);
            var h = r.Gain();
            r.Dc().Connect(h).Connect(r.G.Destination);
            h.Gain.SetValueCurveAtTime([0.6f, -0.2f, 0.3f], 0.02, 0.0251).CancelScheduledValues(0.0451);
        });

        // Held during a setTarget with no ramp after: one more step of the target at the hold, then that value.
        // (With a ramp after it, Chromium starts the ramp from 0: param-cancel-hold-target.)
        yield return F("param-cancel-hold-target-alone", 1, 6000, 48000, """
            const g = gain(); dc().connect(g).connect(out); const p = g.gain;
            p.setValueAtTime(0.9, 0.01); p.setTargetAtTime(0.1, 0.02, 0.02); p.cancelAndHoldAtTime(0.0517); p.setValueAtTime(0.5, 0.09);
            """, r =>
        {
            var g = r.Gain();
            r.Dc().Connect(g).Connect(r.G.Destination);
            g.Gain.SetValueAtTime(0.9f, 0.01).SetTargetAtTime(0.1f, 0.02, 0.02).CancelAndHoldAtTime(0.0517).SetValueAtTime(0.5f, 0.09);
        });

        // ---------- Inputs to parameters ----------

        yield return F("param-audio-rate-input", 1, 4096, 48000, """
            const g = gain(0.5); dc().connect(g).connect(out);
            const n = source(noise(1, 4096, 14)); const depth = gain(0.3); n.connect(depth).connect(g.gain); n.start(0.003);
            g.gain.setValueAtTime(0.5, 0.04); g.gain.linearRampToValueAtTime(0.1, 0.07);
            """, r =>
        {
            var g = r.Gain(0.5f);
            r.Dc().Connect(g).Connect(r.G.Destination);
            var n = r.Source(r.Noise(1, 4096, 14));
            n.Connect(r.Gain(0.3f)).Connect(g.Gain);
            n.Start(0.003);
            g.Gain.SetValueAtTime(0.5f, 0.04).LinearRampToValueAtTime(0.1f, 0.07);
        });

        yield return F("param-stereo-input", 1, 4096, 48000, """
            const g = gain(0.2); dc().connect(g).connect(out);
            const n = source(noise(2, 4096, 15)); n.connect(g.gain); n.start(0);
            const m = source(noise(1, 2000, 16)); m.connect(g.gain); m.start(0.01);
            """, r =>
        {
            var g = r.Gain(0.2f);
            r.Dc().Connect(g).Connect(r.G.Destination);
            var n = r.Source(r.Noise(2, 4096, 15));
            n.Connect(g.Gain);
            n.Start(0);
            var m = r.Source(r.Noise(1, 2000, 16));
            m.Connect(g.Gain);
            m.Start(0.01);
        });

        yield return F("param-k-rate-input", 1, 6000, 48000, """
            const s = source(noise(1, 2500, 17)); s.loop = true; s.connect(out); s.start(0);
            const lfo = source(noise(1, 6000, 18)); const depth = gain(0.4); lfo.connect(depth).connect(s.playbackRate); lfo.start(0);
            """, r =>
        {
            var s = r.Source(r.Noise(1, 2500, 17));
            s.Loop = true;
            s.Connect(r.G.Destination);
            s.Start(0);
            var lfo = r.Source(r.Noise(1, 6000, 18));
            lfo.Connect(r.Gain(0.4f)).Connect(s.PlaybackRate);
            lfo.Start(0);
        });

        // An input's NaN becomes the parameter's default (not its value), then the range applies.
        yield return F("param-nan-input", 1, 1024, 48000, """
            const g = gain(0.5); dc().connect(g).connect(out);
            const b = noise(1, 1024, 31); const d = b.getChannelData(0);
            for (let i = 3; i < d.length; i += 7) d[i] = NaN;
            for (let i = 5; i < d.length; i += 11) d[i] = Infinity;
            const n = source(b); n.connect(g.gain); n.start(0);
            """, r =>
        {
            var g = r.Gain(0.5f);
            r.Dc().Connect(g).Connect(r.G.Destination);
            var b = r.Noise(1, 1024, 31);
            var d = b.GetChannelData(0);
            for (var i = 3; i < d.Length; i += 7)
                d[i] = float.NaN;
            for (var i = 5; i < d.Length; i += 11)
                d[i] = float.PositiveInfinity;
            var n = r.Source(b);
            n.Connect(g.Gain);
            n.Start(0);
        });

        // ---------- Mixing ----------

        yield return F("mix-many-inputs", 2, 4096, 48000, """
            const g = gain(0.5); g.connect(out);
            [[1, 19], [2, 20], [1, 21], [2, 22]].forEach(([channels, seed], i) => { const s = source(noise(channels, 3000, seed)); s.connect(g); s.start(i * 0.002); });
            """, r =>
        {
            var g = r.Gain(0.5f);
            g.Connect(r.G.Destination);
            var i = 0;
            foreach (var (channels, seed) in new[] { (1, 19), (2, 20), (1, 21), (2, 22) })
            {
                var s = r.Source(r.Noise(channels, 3000, seed));
                s.Connect(g);
                s.Start(i++ * 0.002);
            }
        }, SumOrder(4, 4)); // four sources of at most 1 per channel, in Chromium's hash-set order

        yield return F("mix-explicit-mono", 2, 2048, 48000, """
            const g = gain(0.8); g.channelCountMode = "explicit"; g.channelCount = 1;
            const s = source(noise(2, 2000, 23)); s.connect(g).connect(out); s.start(0);
            """, r =>
        {
            var g = r.Gain(0.8f);
            g.ChannelCountMode = ChannelCountMode.Explicit;
            g.ChannelCount = 1;
            var s = r.Source(r.Noise(2, 2000, 23));
            s.Connect(g).Connect(r.G.Destination);
            s.Start(0);
        });

        yield return F("mix-clamped-max", 2, 2048, 48000, """
            const g = gain(); g.channelCountMode = "clamped-max"; g.channelCount = 1;
            const s = source(noise(2, 2000, 24)); s.connect(g).connect(out); s.start(0);
            const t = source(noise(1, 2000, 25)); t.connect(g); t.start(0.005);
            """, r =>
        {
            var g = r.Gain();
            g.ChannelCountMode = ChannelCountMode.ClampedMax;
            g.ChannelCount = 1;
            var s = r.Source(r.Noise(2, 2000, 24));
            s.Connect(g).Connect(r.G.Destination);
            s.Start(0);
            var t = r.Source(r.Noise(1, 2000, 25));
            t.Connect(g);
            t.Start(0.005);
        }, SumOrder(3, 2)); // the mono source and the stereo one's halves, in either order

        yield return F("mix-discrete", 2, 2048, 48000, """
            const down = gain(); down.channelCountMode = "explicit"; down.channelCount = 1; down.channelInterpretation = "discrete";
            const s = source(noise(2, 2000, 26)); s.connect(down).connect(out); s.start(0);
            const up = gain(0.5); up.channelCountMode = "explicit"; up.channelCount = 2; up.channelInterpretation = "discrete";
            const t = source(noise(1, 2000, 27)); t.connect(up).connect(out); t.start(0.001);
            """, r =>
        {
            var down = r.Gain();
            down.ChannelCountMode = ChannelCountMode.Explicit;
            down.ChannelCount = 1;
            down.ChannelInterpretation = ChannelInterpretation.Discrete;
            var s = r.Source(r.Noise(2, 2000, 26));
            s.Connect(down).Connect(r.G.Destination);
            s.Start(0);
            var up = r.Gain(0.5f);
            up.ChannelCountMode = ChannelCountMode.Explicit;
            up.ChannelCount = 2;
            up.ChannelInterpretation = ChannelInterpretation.Discrete;
            var t = r.Source(r.Noise(1, 2000, 27));
            t.Connect(up).Connect(r.G.Destination);
            t.Start(0.001);
        });

        yield return F("fan-out", 2, 2048, 48000, """
            const s = source(noise(1, 2000, 28)); s.start(0.0007);
            const a = gain(0.5); const b = gain(-0.25); s.connect(a).connect(out); s.connect(b).connect(out);
            """, r =>
        {
            var s = r.Source(r.Noise(1, 2000, 28));
            s.Start(0.0007);
            var a = r.Gain(0.5f);
            var b = r.Gain(-0.25f);
            s.Connect(a).Connect(r.G.Destination);
            s.Connect(b).Connect(r.G.Destination);
        });

        // ---------- Silence ----------

        // The gain's inputs fall silent between the two sounds; its automation goes on meanwhile.
        yield return F("silent-inputs-automation", 1, 12000, 48000, """
            const g = gain(); g.connect(out);
            g.gain.setValueAtTime(1, 0); g.gain.setTargetAtTime(0.05, 0.02, 0.04);
            const a = source(noise(1, 600, 29)); a.connect(g); a.start(0);
            const b = source(noise(1, 3000, 30)); b.connect(g); b.start(0.15);
            """, r =>
        {
            var g = r.Gain();
            g.Connect(r.G.Destination);
            g.Gain.SetValueAtTime(1, 0).SetTargetAtTime(0.05f, 0.02, 0.04);
            var a = r.Source(r.Noise(1, 600, 29));
            a.Connect(g);
            a.Start(0);
            var b = r.Source(r.Noise(1, 3000, 30));
            b.Connect(g);
            b.Start(0.15);
        });
    }

    /// <summary>JavaScript helpers for the oscillator fixtures (after the prelude).</summary>
    const string OscPrelude = """
        const osc = (type, frequency) => { const o = ctx.createOscillator(); o.type = type; o.frequency.value = frequency; return o; };

        """;

    static AudioFixture O(string name, int channels, int length, float rate, string script, Action<AudioFixtureRun> build, double tolerance = 0) =>
        F(name, channels, length, rate, OscPrelude + script, build, tolerance);

    static readonly (string Js, OscillatorType Type)[] Shapes =
        [("sine", OscillatorType.Sine), ("square", OscillatorType.Square), ("sawtooth", OscillatorType.Sawtooth), ("triangle", OscillatorType.Triangle)];

    // A custom wave like the composer's (imaginary harmonics, real zeros), and one with both parts and no normalization.
    static readonly float[] Harmonics = [0, 1, 0.6f, 0.3f, 0.45f, 0.1f, 0.25f, 0.05f, 0.12f];
    static readonly float[] RealParts = [0, 0.2f, -0.4f, 0, 0.3f, 0, 0, 0.15f, 0];

    static string Js(float[] values) => string.Join(", ", values.Select(v => v.ToString("R", System.Globalization.CultureInfo.InvariantCulture)));

    static IEnumerable<AudioFixture> Oscillators()
    {
        // ---------- The wave tables, read out ----------

        // At these rates an 8 Hz oscillator steps exactly one sample of its second range's table per
        // frame (rate · 1/8 is the table size, 8 Hz the lowest fundamental: no interpolation), and
        // 32 Hz every fourth sample of the eighth range's. Each rate has its own table size: the
        // Rust FFT's 1024-, 2048- and 8192-point plans.
        foreach (var (rate, size, shapes) in new (float, int, int)[] { (16384, 2048, 5), (32768, 4096, 5), (131072, 16384, 2) })
        {
            var period = size;
            var count = shapes;
            yield return O($"osc-tables-{size}", 1, count * (period + period / 4), rate, $$"""
                const real = new Float32Array([{{Js(RealParts)}}]), imag = new Float32Array([{{Js(Harmonics)}}]);
                const custom = ctx.createPeriodicWave(real.map(() => 0), imag);
                const shapes = ["sawtooth", "custom", "square", "triangle", "sine"].slice(0, {{count}});
                let t = 0;
                shapes.forEach(shape => {
                  for (const [frequency, frames] of [[8, {{period}}], [32, {{period / 4}}]]) {
                    const o = shape === "custom" ? osc("sine", frequency) : osc(shape, frequency);
                    if (shape === "custom") o.setPeriodicWave(custom);
                    o.connect(out); o.start(t / ctx.sampleRate); t += frames; o.stop(t / ctx.sampleRate);
                  }
                });
                """, r =>
            {
                var custom = r.G.CreatePeriodicWave(new float[RealParts.Length], Harmonics);
                OscillatorType[] order = [OscillatorType.Sawtooth, OscillatorType.Custom, OscillatorType.Square, OscillatorType.Triangle, OscillatorType.Sine];
                var t = 0;
                foreach (var shape in order.Take(count))
                    foreach (var (frequency, frames) in new[] { (8f, period), (32f, period / 4) })
                    {
                        var o = r.Osc(shape == OscillatorType.Custom ? OscillatorType.Sine : shape, frequency);
                        if (shape == OscillatorType.Custom)
                            o.SetPeriodicWave(custom);
                        o.Connect(r.G.Destination);
                        o.Start(t / (double)r.G.SampleRateF);
                        t += frames;
                        o.Stop(t / (double)r.G.SampleRateF);
                    }
            });
        }

        // ---------- Playing ----------

        foreach (var (js, type) in Shapes)
            yield return O($"osc-{js}", 1, 4096, 48000, $$"""
                const o = osc("{{js}}", 431.7); o.connect(out); o.start(0.0010021); o.stop(0.0781);
                """, r =>
            {
                var o = r.Osc(type, 431.7f);
                o.Connect(r.G.Destination);
                o.Start(0.0010021);
                o.Stop(0.0781);
            });

        yield return O("osc-custom", 1, 4096, 44100, $$"""
            const a = osc("sine", 220); a.setPeriodicWave(ctx.createPeriodicWave(new Float32Array({{Harmonics.Length}}), new Float32Array([{{Js(Harmonics)}}])));
            a.connect(out); a.start(0); a.stop(0.04);
            const b = osc("sine", 330); b.setPeriodicWave(ctx.createPeriodicWave(new Float32Array([{{Js(RealParts)}}]), new Float32Array([{{Js(Harmonics)}}]), { disableNormalization: true }));
            b.connect(out); b.start(0.05);
            """, r =>
        {
            var a = r.Osc(OscillatorType.Sine, 220);
            a.SetPeriodicWave(r.G.CreatePeriodicWave(new float[Harmonics.Length], Harmonics));
            a.Connect(r.G.Destination);
            a.Start(0);
            a.Stop(0.04);
            var b = r.Osc(OscillatorType.Sine, 330);
            b.SetPeriodicWave(r.G.CreatePeriodicWave(RealParts, Harmonics, disableNormalization: true));
            b.Connect(r.G.Destination);
            b.Start(0.05);
        });

        // Frequency swept (a-rate: a phase increment and a pair of tables per sample).
        yield return O("osc-frequency-automation", 1, 12000, 48000, """
            const o = osc("sawtooth", 100); o.connect(out);
            o.frequency.setValueAtTime(100, 0.01); o.frequency.exponentialRampToValueAtTime(6000, 0.12); o.frequency.linearRampToValueAtTime(40, 0.24);
            o.start(0.0021);
            """, r =>
        {
            var o = r.Osc(OscillatorType.Sawtooth, 100);
            o.Connect(r.G.Destination);
            o.Frequency.SetValueAtTime(100, 0.01).ExponentialRampToValueAtTime(6000, 0.12).LinearRampToValueAtTime(40, 0.24);
            o.Start(0.0021);
        });

        // Detune automated alone (its multipliers become the increments), then with the frequency.
        yield return O("osc-detune-automation", 1, 12000, 48000, """
            const o = osc("square", 300); o.connect(out);
            o.detune.setValueAtTime(0, 0); o.detune.linearRampToValueAtTime(1200, 0.08); o.detune.setTargetAtTime(-700, 0.1, 0.03);
            o.start(0);
            const p = osc("triangle", 500); p.connect(out);
            p.frequency.setValueAtTime(500, 0.13); p.frequency.linearRampToValueAtTime(250, 0.2);
            p.detune.setValueAtTime(-300, 0.13); p.detune.linearRampToValueAtTime(400, 0.24);
            p.start(0.13);
            """, r =>
        {
            var o = r.Osc(OscillatorType.Square, 300);
            o.Connect(r.G.Destination);
            o.Detune.SetValueAtTime(0, 0).LinearRampToValueAtTime(1200, 0.08).SetTargetAtTime(-700, 0.1, 0.03);
            o.Start(0);
            var p = r.Osc(OscillatorType.Triangle, 500);
            p.Connect(r.G.Destination);
            p.Frequency.SetValueAtTime(500, 0.13).LinearRampToValueAtTime(250, 0.2);
            p.Detune.SetValueAtTime(-300, 0.13).LinearRampToValueAtTime(400, 0.24);
            p.Start(0.13);
        });

        // A constant detune (k-rate multiplier), and a note starting mid-quantum while its frequency
        // moves (Blink takes the quantum's increments from its first frame).
        yield return O("osc-detune-and-late-start", 1, 6000, 48000, """
            const o = osc("sawtooth", 261.6); o.detune.value = 37; o.connect(out); o.start(0); o.stop(0.05);
            const p = osc("sine", 700); p.connect(out);
            p.frequency.setValueAtTime(700, 0); p.frequency.linearRampToValueAtTime(1500, 0.12);
            p.start(0.0600417);
            """, r =>
        {
            var o = r.Osc(OscillatorType.Sawtooth, 261.6f);
            o.Detune.Value = 37;
            o.Connect(r.G.Destination);
            o.Start(0);
            o.Stop(0.05);
            var p = r.Osc(OscillatorType.Sine, 700);
            p.Connect(r.G.Destination);
            p.Frequency.SetValueAtTime(700, 0).LinearRampToValueAtTime(1500, 0.12);
            p.Start(0.0600417);
        });

        // Low frequencies (3- and 5-point interpolation), as the composer's LFOs, one modulating a gain.
        yield return O("osc-lfo", 1, 24000, 48000, """
            const a = osc("sine", 2.5); a.connect(out); a.start(0);
            const b = osc("triangle", 0.4); const depth = gain(0.3); b.connect(depth);
            const g = gain(0.5); dc().connect(g).connect(out); depth.connect(g.gain); b.start(0.001);
            """, r =>
        {
            var a = r.Osc(OscillatorType.Sine, 2.5f);
            a.Connect(r.G.Destination);
            a.Start(0);
            var b = r.Osc(OscillatorType.Triangle, 0.4f);
            var depth = r.Gain(0.3f);
            b.Connect(depth);
            var g = r.Gain(0.5f);
            r.Dc().Connect(g).Connect(r.G.Destination);
            depth.Connect(g.Gain);
            b.Start(0.001);
        });

        // An oscillator driving another's frequency (audio-rate input, frequency crossing zero), and a
        // negative frequency.
        yield return O("osc-modulated-frequency", 1, 8192, 48000, """
            const carrier = osc("sine", 300); carrier.connect(out); carrier.start(0);
            const modulator = osc("sine", 47); const depth = gain(450); modulator.connect(depth).connect(carrier.frequency); modulator.start(0);
            const n = osc("sawtooth", -330); n.connect(out); n.start(0.1);
            """, r =>
        {
            var carrier = r.Osc(OscillatorType.Sine, 300);
            carrier.Connect(r.G.Destination);
            carrier.Start(0);
            var modulator = r.Osc(OscillatorType.Sine, 47);
            modulator.Connect(r.Gain(450)).Connect(carrier.Frequency);
            modulator.Start(0);
            var n = r.Osc(OscillatorType.Sawtooth, -330);
            n.Connect(r.G.Destination);
            n.Start(0.1);
        });

        // Near Nyquist (the top ranges, few partials), detuned past it (clamped), and the shape changed while playing.
        yield return O("osc-high-and-type-change", 1, 8192, 48000, """
            const a = osc("square", 9000); a.connect(out); a.start(0); a.stop(0.05);
            const b = osc("sawtooth", 15000); b.detune.value = 1200; b.connect(out); b.start(0.05); b.stop(0.1);
            const c = osc("triangle", 880); c.connect(out); c.start(0.1);
            ctx.suspend(0.13).then(() => { c.type = "square"; ctx.resume(); });
            """, r =>
        {
            var a = r.Osc(OscillatorType.Square, 9000);
            a.Connect(r.G.Destination);
            a.Start(0);
            a.Stop(0.05);
            var b = r.Osc(OscillatorType.Sawtooth, 15000);
            b.Detune.Value = 1200;
            b.Connect(r.G.Destination);
            b.Start(0.05);
            b.Stop(0.1);
            var c = r.Osc(OscillatorType.Triangle, 880);
            c.Connect(r.G.Destination);
            c.Start(0.1);
            r.Suspend(0.13, () => c.Type = OscillatorType.Square);
        });

        // Detuned above Nyquist and back down: the increments are clamped meanwhile (the top tables are
        // silent), which sets the phase the sweep comes back with.
        yield return O("osc-above-nyquist", 1, 8192, 48000, """
            const o = osc("sawtooth", 15000); o.connect(out);
            o.detune.setValueAtTime(1200, 0); o.detune.linearRampToValueAtTime(-4000, 0.15);
            o.start(0);
            """, r =>
        {
            var o = r.Osc(OscillatorType.Sawtooth, 15000);
            o.Connect(r.G.Destination);
            o.Detune.SetValueAtTime(1200, 0).LinearRampToValueAtTime(-4000, 0.15);
            o.Start(0);
        });

        // A steady note starting between frames after the first quantum (the k-rate path takes the
        // start's fraction of a frame into its phase).
        yield return O("osc-steady-late-start", 1, 4096, 48000, """
            const o = osc("square", 377.3); o.connect(out); o.start(0.0123456); o.stop(0.07);
            """, r =>
        {
            var o = r.Osc(OscillatorType.Square, 377.3f);
            o.Connect(r.G.Destination);
            o.Start(0.0123456);
            o.Stop(0.07);
        });

        // Other sample rates: 22.05 kHz (2048-point tables) and 96 kHz (16384-point).
        foreach (var rate in new[] { 22050f, 96000f })
            yield return O($"osc-rate-{rate / 1000:0.##}k".Replace('.', '_'), 1, 6000, rate, """
                const o = osc("sawtooth", 523.25); o.connect(out); o.start(0.001);
                o.frequency.setValueAtTime(523.25, 0.02); o.frequency.exponentialRampToValueAtTime(90, 0.05);
                """, r =>
            {
                var o = r.Osc(OscillatorType.Sawtooth, 523.25f);
                o.Connect(r.G.Destination);
                o.Start(0.001);
                o.Frequency.SetValueAtTime(523.25f, 0.02).ExponentialRampToValueAtTime(90, 0.05);
            });
    }

    /// <summary>JavaScript helpers for the biquad fixtures (after the oscillator prelude).</summary>
    const string FilterPrelude = """
        const filter = (type, frequency) => { const f = ctx.createBiquadFilter(); f.type = type; f.frequency.value = frequency; return f; };

        """;

    static AudioFixture B(string name, int channels, int length, float rate, string script, Action<AudioFixtureRun> build, double tolerance = 0) =>
        O(name, channels, length, rate, FilterPrelude + script, build, tolerance);

    static IEnumerable<AudioFixture> Biquads()
    {
        // Each type on a noise burst, ringing out after it until its tail has passed (then the node stops).
        foreach (var (type, js, frequency, q, gain, rate) in new (BiquadFilterType, string, float, float, float, float)[]
        {
            (BiquadFilterType.Lowpass, "lowpass", 900, 12, 0, 48000),
            (BiquadFilterType.Highpass, "highpass", 2500, -3, 0, 48000),
            (BiquadFilterType.Bandpass, "bandpass", 1200, 4, 0, 44100),
            (BiquadFilterType.Lowshelf, "lowshelf", 300, 1, 9, 48000),
            (BiquadFilterType.Highshelf, "highshelf", 4000, 1, -7, 48000),
            (BiquadFilterType.Peaking, "peaking", 1800, 2, 8, 44100),
            (BiquadFilterType.Notch, "notch", 700, 5, 0, 48000),
            (BiquadFilterType.Allpass, "allpass", 1500, 1.5f, 0, 48000),
        })
            yield return B($"biquad-{js}", 1, 6000, rate, $$"""
                const f = filter("{{js}}", {{frequency}}); f.Q.value = {{q}}; f.gain.value = {{gain}};
                const s = source(noise(1, 1500, 40)); s.connect(f).connect(out); s.start(0.001);
                """, r =>
            {
                var f = r.Filter(type, frequency);
                f.Q.Value = q;
                f.Gain.Value = gain;
                var s = r.Source(r.Noise(1, 1500, 40));
                s.Connect(f).Connect(r.G.Destination);
                s.Start(0.001);
            });

        // Coefficients per frame while the cutoff sweeps, Q rises and detune moves.
        yield return B("biquad-frequency-automation", 1, 12000, 48000, """
            const o = osc("sawtooth", 110); const f = filter("lowpass", 200); o.connect(f).connect(out); o.start(0);
            f.frequency.setValueAtTime(200, 0.01); f.frequency.exponentialRampToValueAtTime(8000, 0.15);
            f.Q.setValueAtTime(1, 0); f.Q.setTargetAtTime(15, 0.05, 0.04);
            f.detune.setValueAtTime(0, 0.16); f.detune.linearRampToValueAtTime(-1200, 0.24);
            """, r =>
        {
            var o = r.Osc(OscillatorType.Sawtooth, 110);
            var f = r.Filter(BiquadFilterType.Lowpass, 200);
            o.Connect(f).Connect(r.G.Destination);
            o.Start(0);
            f.Frequency.SetValueAtTime(200, 0.01).ExponentialRampToValueAtTime(8000, 0.15);
            f.Q.SetValueAtTime(1, 0).SetTargetAtTime(15, 0.05, 0.04);
            f.Detune.SetValueAtTime(0, 0.16).LinearRampToValueAtTime(-1200, 0.24);
        });

        yield return B("biquad-gain-automation", 1, 8192, 48000, """
            const s = source(noise(1, 8192, 41)); const p = filter("peaking", 2000); p.Q.value = 1.5;
            const l = filter("lowshelf", 250); s.connect(p).connect(l).connect(out); s.start(0);
            p.gain.setValueAtTime(-12, 0); p.gain.linearRampToValueAtTime(15, 0.12);
            l.gain.setValueAtTime(6, 0.04); l.gain.setTargetAtTime(-9, 0.08, 0.02);
            """, r =>
        {
            var s = r.Source(r.Noise(1, 8192, 41));
            var p = r.Filter(BiquadFilterType.Peaking, 2000);
            p.Q.Value = 1.5f;
            var l = r.Filter(BiquadFilterType.Lowshelf, 250);
            s.Connect(p).Connect(l).Connect(r.G.Destination);
            s.Start(0);
            p.Gain.SetValueAtTime(-12, 0).LinearRampToValueAtTime(15, 0.12);
            l.Gain.SetValueAtTime(6, 0.04).SetTargetAtTime(-9, 0.08, 0.02);
        });

        // Events ahead: sample-accurate but constant within most quanta (coefficients once a quantum),
        // a constant detune (exp2f), and values changed while rendering (the k-rate path's change check),
        // including the edges: cutoff 0, Nyquist, Q below 0.
        yield return B("biquad-held-and-changed", 1, 12000, 48000, """
            const s = source(noise(1, 12000, 42)); const f = filter("bandpass", 600); f.detune.value = 700;
            s.connect(f).connect(out); s.start(0);
            f.frequency.setValueAtTime(900, 0.05); f.frequency.setValueAtTime(1500, 0.08);
            ctx.suspend(0.1).then(() => { f.Q.value = 6; ctx.resume(); });
            ctx.suspend(0.13).then(() => { f.frequency.value = 0; ctx.resume(); });
            ctx.suspend(0.16).then(() => { f.frequency.value = 24000; ctx.resume(); });
            ctx.suspend(0.19).then(() => { f.frequency.value = 3000; f.Q.value = -2; ctx.resume(); });
            ctx.suspend(0.22).then(() => { f.Q.value = 3; f.detune.value = 0; ctx.resume(); });
            """, r =>
        {
            var s = r.Source(r.Noise(1, 12000, 42));
            var f = r.Filter(BiquadFilterType.Bandpass, 600);
            f.Detune.Value = 700;
            s.Connect(f).Connect(r.G.Destination);
            s.Start(0);
            f.Frequency.SetValueAtTime(900, 0.05).SetValueAtTime(1500, 0.08);
            r.Suspend(0.1, () => f.Q.Value = 6);
            r.Suspend(0.13, () => f.Frequency.Value = 0);
            r.Suspend(0.16, () => f.Frequency.Value = 24000);
            r.Suspend(0.19, () =>
            {
                f.Frequency.Value = 3000;
                f.Q.Value = -2;
            });
            r.Suspend(0.22, () =>
            {
                f.Q.Value = 3;
                f.Detune.Value = 0;
            });
        });

        // The other edges: peaking, allpass and notch at Q 0, shelves at the ends.
        foreach (var (js, type, setup) in new (string, BiquadFilterType, (string Js, Action<BiquadFilterNode> Cs))[]
        {
            ("peaking", BiquadFilterType.Peaking, ("f.Q.value = 0; f.gain.value = 6;", f => { f.Q.Value = 0; f.Gain.Value = 6; })),
            ("allpass", BiquadFilterType.Allpass, ("f.Q.value = 0;", f => f.Q.Value = 0)),
            ("notch", BiquadFilterType.Notch, ("f.Q.value = 0;", f => f.Q.Value = 0)),
            ("lowshelf", BiquadFilterType.Lowshelf, ("f.frequency.value = 24000; f.gain.value = -6;", f => { f.Frequency.Value = 24000; f.Gain.Value = -6; })),
            ("highshelf", BiquadFilterType.Highshelf, ("f.frequency.value = 0; f.gain.value = 4;", f => { f.Frequency.Value = 0; f.Gain.Value = 4; })),
        })
            yield return B($"biquad-edge-{js}", 1, 2048, 48000, $$"""
                const f = filter("{{js}}", 1000); {{setup.Js}}
                const s = source(noise(1, 1500, 43)); s.connect(f).connect(out); s.start(0);
                """, r =>
            {
                var f = r.Filter(type, 1000);
                setup.Cs(f);
                var s = r.Source(r.Noise(1, 1500, 43));
                s.Connect(f).Connect(r.G.Destination);
                s.Start(0);
            });

        // Stereo (a filter per channel); a stereo source joining a mono one changes the channel count
        // (new, cleared filters); a type change resets the state.
        yield return B("biquad-channels-and-type", 2, 9600, 48000, """
            const f = filter("lowpass", 1500); f.Q.value = 8; f.connect(out);
            const m = source(noise(1, 9600, 44)); m.connect(f); m.start(0);
            const st = source(noise(2, 9600, 45));
            ctx.suspend(0.05).then(() => { st.connect(f); st.start(ctx.currentTime); ctx.resume(); });
            ctx.suspend(0.12).then(() => { f.type = "highpass"; ctx.resume(); });
            """, r =>
        {
            var f = r.Filter(BiquadFilterType.Lowpass, 1500);
            f.Q.Value = 8;
            f.Connect(r.G.Destination);
            var m = r.Source(r.Noise(1, 9600, 44));
            m.Connect(f);
            m.Start(0);
            var st = r.Source(r.Noise(2, 9600, 45));
            r.Suspend(0.05, () =>
            {
                st.Connect(f);
                st.Start(r.G.CurrentTime);
            });
            r.Suspend(0.12, () => f.Type = BiquadFilterType.Highpass);
        });

        // The input taken away while the filter rings: it rings on (zeros in) until its tail has passed.
        yield return B("biquad-input-removed", 1, 9600, 48000, """
            const f = filter("bandpass", 440); f.Q.value = 30; f.connect(out);
            const s = source(noise(1, 9600, 46)); s.connect(f); s.start(0);
            ctx.suspend(0.03).then(() => { s.disconnect(); ctx.resume(); });
            """, r =>
        {
            var f = r.Filter(BiquadFilterType.Bandpass, 440);
            f.Q.Value = 30;
            f.Connect(r.G.Destination);
            var s = r.Source(r.Noise(1, 9600, 46));
            s.Connect(f);
            s.Start(0);
            r.Suspend(0.03, () => s.Disconnect());
        });

        // The composer's master EQ (music-composer.js: a highpass at 34 Hz, a low shelf at 140 Hz, a
        // presence peak at 2600 Hz) on notes, and an LFO moving a cutoff (as its wind does). At most two
        // notes sound at once: Chromium sums three in an order that changes from run to run, and the
        // EQ's low poles carry that last-bit difference along (to about 2 · 10⁻⁵).
        yield return B("biquad-eq-and-lfo", 1, 12000, 48000, """
            const lowCut = filter("highpass", 34); lowCut.Q.value = 0.7;
            const lowShelf = filter("lowshelf", 140); lowShelf.gain.value = -3;
            const presence = filter("peaking", 2600); presence.Q.value = 0.8; presence.gain.value = 2;
            lowCut.connect(lowShelf).connect(presence).connect(out);
            [[110, 0], [164.81, 0.06], [220, 0.12]].forEach(([frequency, when]) => { const o = osc("sawtooth", frequency); o.connect(lowCut); o.start(when); o.stop(when + 0.1); });
            const wind = filter("bandpass", 900); wind.Q.value = 2; wind.connect(out);
            const n = source(noise(1, 12000, 47)); n.connect(wind); n.start(0);
            const lfo = osc("sine", 0.7); const sweep = gain(260); lfo.connect(sweep).connect(wind.frequency); lfo.start(0);
            """, r =>
        {
            var lowCut = r.Filter(BiquadFilterType.Highpass, 34);
            lowCut.Q.Value = 0.7f;
            var lowShelf = r.Filter(BiquadFilterType.Lowshelf, 140);
            lowShelf.Gain.Value = -3;
            var presence = r.Filter(BiquadFilterType.Peaking, 2600);
            presence.Q.Value = 0.8f;
            presence.Gain.Value = 2;
            lowCut.Connect(lowShelf).Connect(presence).Connect(r.G.Destination);
            foreach (var (frequency, when) in new[] { (110f, 0.0), (164.81f, 0.06), (220f, 0.12) })
            {
                var o = r.Osc(OscillatorType.Sawtooth, frequency);
                o.Connect(lowCut);
                o.Start(when);
                o.Stop(when + 0.1);
            }
            var wind = r.Filter(BiquadFilterType.Bandpass, 900);
            wind.Q.Value = 2;
            wind.Connect(r.G.Destination);
            var n = r.Source(r.Noise(1, 12000, 47));
            n.Connect(wind);
            n.Start(0);
            var lfo = r.Osc(OscillatorType.Sine, 0.7f);
            lfo.Connect(r.Gain(260)).Connect(wind.Frequency);
            lfo.Start(0);
        });

        // A denormal input reads as zero (DAZ): the engine's gain makes 1e-39 where Chromium's flushes
        // it, and a wide 120 dB peak (b0 near 48 000) would lift that into the normal floats.
        yield return B("biquad-denormal-input", 1, 1024, 48000, """
            const g = gain(1e-39); dc().connect(g); const f = filter("peaking", 12000); f.Q.value = 0.01; f.gain.value = 120;
            g.connect(f).connect(out);
            """, r =>
        {
            var g = r.Gain(1e-39f);
            r.Dc().Connect(g);
            var f = r.Filter(BiquadFilterType.Peaking, 12000);
            f.Q.Value = 0.01f;
            f.Gain.Value = 120;
            g.Connect(f).Connect(r.G.Destination);
        }, Exact);

        // The filter falls silent (its tail passed) while its cutoff glides on; the next sound meets the
        // cutoff where the glide got to.
        yield return B("biquad-silent-automation", 1, 12000, 48000, """
            const f = filter("lowpass", 4000); f.Q.value = 6; f.connect(out);
            f.frequency.setValueAtTime(4000, 0); f.frequency.setTargetAtTime(300, 0.02, 0.04);
            const a = source(noise(1, 1000, 49)); a.connect(f); a.start(0);
            const b = source(noise(1, 2000, 50)); b.connect(f); b.start(0.16);
            """, r =>
        {
            var f = r.Filter(BiquadFilterType.Lowpass, 4000);
            f.Q.Value = 6;
            f.Connect(r.G.Destination);
            f.Frequency.SetValueAtTime(4000, 0).SetTargetAtTime(300, 0.02, 0.04);
            var a = r.Source(r.Noise(1, 1000, 49));
            a.Connect(f);
            a.Start(0);
            var b = r.Source(r.Noise(1, 2000, 50));
            b.Connect(f);
            b.Start(0.16);
        });

        // All four parameters k-rate while automated: one set of coefficients a quantum.
        yield return B("biquad-k-rate", 1, 8192, 48000, """
            const o = osc("square", 150); const f = filter("lowpass", 300); o.connect(f).connect(out); o.start(0);
            for (const p of [f.frequency, f.Q, f.gain, f.detune]) p.automationRate = "k-rate";
            f.frequency.setValueAtTime(300, 0); f.frequency.linearRampToValueAtTime(5000, 0.15); f.Q.setTargetAtTime(10, 0.02, 0.05);
            """, r =>
        {
            var o = r.Osc(OscillatorType.Square, 150);
            var f = r.Filter(BiquadFilterType.Lowpass, 300);
            o.Connect(f).Connect(r.G.Destination);
            o.Start(0);
            foreach (var p in new[] { f.Frequency, f.Q, f.Gain, f.Detune })
                p.IsAudioRate = false;
            f.Frequency.SetValueAtTime(300, 0).LinearRampToValueAtTime(5000, 0.15);
            f.Q.SetTargetAtTime(10, 0.02, 0.05);
        });

        // A one-sample pulse through a damped filter whose input stays unsilent zeros: the response
        // decays through the denormals, which Chromium flushes to zero.
        yield return B("biquad-denormal-decay", 1, 3072, 48000, """
            const g = gain(0); dc().connect(g); const idle = source(noise(1, 128, 48)); idle.connect(g.gain);
            g.gain.setValueAtTime(1, 0.001); g.gain.setValueAtTime(0, 0.001 + 1 / 48000);
            const f = filter("lowpass", 3000); f.Q.value = -20; g.connect(f).connect(out);
            """, r =>
        {
            var g = r.Gain(0);
            r.Dc().Connect(g);
            var idle = r.Source(r.Noise(1, 128, 48));
            idle.Connect(g.Gain);
            g.Gain.SetValueAtTime(1, 0.001).SetValueAtTime(0, 0.001 + 1 / 48000.0);
            var f = r.Filter(BiquadFilterType.Lowpass, 3000);
            f.Q.Value = -20;
            g.Connect(f).Connect(r.G.Destination);
        }, Exact);
    }

    /// <summary>JavaScript helpers for the convolver and panner fixtures (after the filter prelude).</summary>
    const string RoomPrelude = """
        const room = (channels, length, seed, decay) => { const b = ctx.createBuffer(channels, length, ctx.sampleRate); for (let c = 0; c < channels; c++) { const d = b.getChannelData(c), r = mulberry(seed + c * 7919); let amplitude = 1; for (let i = 0; i < length; i++) { d[i] = (r() * 2 - 1) * amplitude; amplitude *= decay; } } return b; };
        const convolver = (buffer, normalize = true) => { const c = ctx.createConvolver(); c.normalize = normalize; c.buffer = buffer; return c; };
        const panner = pan => { const p = ctx.createStereoPanner(); p.pan.value = pan; return p; };

        """;

    static AudioFixture R(string name, int channels, int length, float rate, string script, Action<AudioFixtureRun> build, double tolerance = 0) =>
        B(name, channels, length, rate, RoomPrelude + script, build, tolerance);

    static IEnumerable<AudioFixture> Rooms()
    {
        // ---------- The convolver ----------

        // A short mono room on a mono burst: the direct stage and FFT stages of 128 to 4096 points; the
        // node stops once the response's length has passed after the input fell silent.
        yield return R("conv-mono", 1, 8192, 48000, """
            const c = convolver(room(1, 3000, 60, 0.9985)); c.connect(out);
            const s = source(noise(1, 2000, 61)); s.connect(c); s.start(0.001);
            """, r =>
        {
            var c = r.Convolver(r.Room(1, 3000, 60, 0.9985));
            c.Connect(r.G.Destination);
            var s = r.Source(r.Noise(1, 2000, 61));
            s.Connect(c);
            s.Start(0.001);
        });

        // A long stereo room (stages up to 32768 points) on a mono input: stereo out.
        yield return R("conv-stereo-room", 2, 28672, 48000, """
            const c = convolver(room(2, 24000, 62, 0.99985)); c.connect(out);
            const s = source(noise(1, 2000, 63)); s.connect(c); s.start(0);
            """, r =>
        {
            var c = r.Convolver(r.Room(2, 24000, 62, 0.99985));
            c.Connect(r.G.Destination);
            var s = r.Source(r.Noise(1, 2000, 63));
            s.Connect(c);
            s.Start(0);
        });

        // A room longer than 32768 frames (as the composer's are): the stages past it stay at 32768
        // points and are staggered by their render phase (a pre-delay before their FFT blocks).
        yield return R("conv-long-room", 1, 45056, 48000, """
            const c = convolver(room(1, 40000, 76, 0.99992)); c.connect(out);
            const s = source(noise(1, 1500, 77)); s.connect(c); s.start(0);
            """, r =>
        {
            var c = r.Convolver(r.Room(1, 40000, 76, 0.99992));
            c.Connect(r.G.Destination);
            var s = r.Source(r.Noise(1, 1500, 77));
            s.Connect(c);
            s.Start(0);
        });

        // Stereo in through a stereo room, not normalized; stereo in through a mono room.
        yield return R("conv-stereo-input", 2, 12000, 44100, """
            const a = convolver(room(2, 5000, 64, 0.999), false); a.connect(out);
            const s = source(noise(2, 3000, 65)); s.connect(a); s.start(0);
            const b = convolver(room(1, 4000, 66, 0.9992)); b.connect(out);
            const t = source(noise(2, 2000, 67)); t.connect(b); t.start(0.12);
            """, r =>
        {
            var a = r.Convolver(r.Room(2, 5000, 64, 0.999), normalize: false);
            a.Connect(r.G.Destination);
            var s = r.Source(r.Noise(2, 3000, 65));
            s.Connect(a);
            s.Start(0);
            var b = r.Convolver(r.Room(1, 4000, 66, 0.9992));
            b.Connect(r.G.Destination);
            var t = r.Source(r.Noise(2, 2000, 67));
            t.Connect(b);
            t.Start(0.12);
        });

        // Silent past its tail, the node stops (its stages' block positions freeze); the next sound resumes them.
        yield return R("conv-silence-between", 1, 16384, 48000, """
            const c = convolver(room(1, 2500, 68, 0.998)); c.connect(out);
            const a = source(noise(1, 700, 69)); a.connect(c); a.start(0);
            const b = source(noise(1, 900, 70)); b.connect(c); b.start(0.2001);
            """, r =>
        {
            var c = r.Convolver(r.Room(1, 2500, 68, 0.998));
            c.Connect(r.G.Destination);
            var a = r.Source(r.Noise(1, 700, 69));
            a.Connect(c);
            a.Start(0);
            var b = r.Source(r.Noise(1, 900, 70));
            b.Connect(c);
            b.Start(0.2001);
        });

        // ---------- The stereo panner ----------

        // Mono: a constant pan (one value a quantum), then swept across (per sample); stereo: swept
        // across zero (the near side gets the far one), and held at 0 and at the ends.
        yield return R("panner-mono", 2, 8192, 48000, """
            const p = panner(-0.4); p.connect(out); const s = source(noise(1, 8192, 71)); s.connect(p); s.start(0);
            p.pan.setValueAtTime(-0.4, 0.05); p.pan.linearRampToValueAtTime(1, 0.15);
            """, r =>
        {
            var p = r.Panner(-0.4f);
            p.Connect(r.G.Destination);
            var s = r.Source(r.Noise(1, 8192, 71));
            s.Connect(p);
            s.Start(0);
            p.Pan.SetValueAtTime(-0.4f, 0.05).LinearRampToValueAtTime(1, 0.15);
        });

        yield return R("panner-stereo", 2, 12000, 48000, """
            const p = panner(0.3); p.connect(out); const s = source(noise(2, 12000, 72)); s.connect(p); s.start(0);
            p.pan.setValueAtTime(-1, 0.05); p.pan.linearRampToValueAtTime(1, 0.15);
            ctx.suspend(0.17).then(() => { p.pan.value = 0; ctx.resume(); });
            ctx.suspend(0.2).then(() => { p.pan.value = -1; ctx.resume(); });
            ctx.suspend(0.22).then(() => { p.pan.value = 1; ctx.resume(); });
            """, r =>
        {
            var p = r.Panner(0.3f);
            p.Connect(r.G.Destination);
            var s = r.Source(r.Noise(2, 12000, 72));
            s.Connect(p);
            s.Start(0);
            p.Pan.SetValueAtTime(-1, 0.05).LinearRampToValueAtTime(1, 0.15);
            r.Suspend(0.17, () => p.Pan.Value = 0);
            r.Suspend(0.2, () => p.Pan.Value = -1);
            r.Suspend(0.22, () => p.Pan.Value = 1);
        });

        // A finished stereo source disables a gain it alone fed (the gain is left mono, unheard); a new
        // mono source connected to the gain enables it again.
        yield return R("graph-disabled-and-reenabled", 2, 8192, 48000, """
            const g = gain(0.7); g.connect(out); const a = source(noise(2, 1500, 74)); a.connect(g); a.start(0);
            const b = source(noise(1, 2000, 75));
            ctx.suspend(0.08).then(() => { b.connect(g); b.start(ctx.currentTime + 0.001); ctx.resume(); });
            """, r =>
        {
            var g = r.Gain(0.7f);
            g.Connect(r.G.Destination);
            var a = r.Source(r.Noise(2, 1500, 74));
            a.Connect(g);
            a.Start(0);
            var b = r.Source(r.Noise(1, 2000, 75));
            r.Suspend(0.08, () =>
            {
                b.Connect(g);
                b.Start(r.G.CurrentTime + 0.001);
            });
        });

        // The composer's send: notes panned into a room (a convolver after panners, as its mix does).
        yield return R("panner-into-room", 2, 16384, 48000, """
            const c = convolver(room(2, 6000, 73, 0.9993)); c.connect(out);
            [[-0.6, 0, 196], [0.5, 0.04, 293.66]].forEach(([pan, when, frequency]) => {
              const o = osc("triangle", frequency); const p = panner(pan); o.connect(p).connect(c); o.start(when); o.stop(when + 0.08);
            });
            """, r =>
        {
            var c = r.Convolver(r.Room(2, 6000, 73, 0.9993));
            c.Connect(r.G.Destination);
            foreach (var (pan, when, frequency) in new[] { (-0.6f, 0.0, 196f), (0.5f, 0.04, 293.66f) })
            {
                var o = r.Osc(OscillatorType.Triangle, frequency);
                var p = r.Panner(pan);
                o.Connect(p).Connect(c);
                o.Start(when);
                o.Stop(when + 0.08);
            }
        });
    }
}

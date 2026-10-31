using Statventure.Audio;

namespace Statventure.Tests.Audio;

/// <summary>
/// A Web Audio graph rendered offline: <see cref="Script"/> builds it on `ctx` in Chromium 152
/// (Tools/AudioReference), <see cref="Build"/> builds its twin on the engine's nodes.
/// <see cref="Tolerance"/> is the largest difference a sample may show, worked out from float32
/// precision for what Chromium leaves undetermined (the order its inputs sum in); 0 means the
/// smallest normal float (Chromium flushes denormals to zero, the engine doesn't).
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

    static AudioFixture F(string name, int channels, int length, float rate, string script, Action<AudioFixtureRun> build, double tolerance = 0) =>
        new(name, channels, length, rate, Prelude + script, build, tolerance);

    static AudioFixture[]? s_all;
    public static AudioFixture[] All => s_all ??= [.. Core()];

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
}

using System.Diagnostics;
using Statventure.Audio;

namespace Statventure.Tests.Audio;

/// <summary>
/// The audio clock and the calls at a time on it (Docs/Audio.md, "The clock and scheduled calls"; Docs/Scene.md,
/// S6-B, S6-C): an effect or a music switch at a time renders sample for sample as the same graph built by
/// hand at that time (the voice's graph and music.js's switch written out again here), a time at a
/// quantum's start as the call made for that quantum, a past time at once; the voice is made in the quantum
/// its time falls in; a cancelled call leaves the rest as it was and adds nothing.
/// </summary>
public class ScheduledSoundTests
{
    const float Rate = 22050;
    const int Q = AudioGraph.RenderQuantumFrames;

    /// <summary>A tone sweeping down and a delayed noise burst through a bandpass sweeping too, with pitch and volume ranges (so the draws show).</summary>
    static readonly EffectPatch Ping = new()
    {
        Layers =
        [
            new EffectLayer { Source = EffectSource.Triangle, Frequency = 880, FrequencyTo = 330, Attack = 0.003, Length = 0.12 },
            new EffectLayer { Source = EffectSource.Noise, Filter = new EffectFilter { Type = EffectFilterType.Bandpass, Frequency = 2400, FrequencyTo = 600, Q = 3 }, Delay = 0.02, Level = 0.4, Length = 0.08 },
        ],
        Level = 0.6,
        Pitch = new EffectRange(0.8, 1.25),
        Volume = new EffectRange(0.5, 1),
    };

    /// <summary>A long tone, two voices at most.</summary>
    static readonly EffectPatch Hum = new() { Layers = [new EffectLayer { Source = EffectSource.Sine, Frequency = 300, Length = 0.6 }], MaxVoices = 2, Level = 0.3 };

    static AudioSystem System(MusicLibrary? music = null)
    {
        var system = new AudioSystem(new AudioGraph(Rate, 2), music ?? MusicLibrary.Empty);
        system.Effects.Register("ping", Ping);
        system.Effects.Register("hum", Hum);
        return system;
    }

    /// <summary>Renders <paramref name="quanta"/> quanta: before each, <paramref name="before"/> with the quantum's first frame, then the system's calls.</summary>
    static float[] Render(AudioSystem system, int quanta, Action<long>? before = null)
    {
        var bus = new AudioBus(2, Q);
        var result = new List<float>();
        for (var q = 0; q < quanta; q++)
        {
            before?.Invoke(system.Graph.CurrentSampleFrame);
            system.BeforeQuantum();
            system.Graph.RenderQuantum(bus);
            result.AddRange(bus.Channel(0).Data);
            result.AddRange(bus.Channel(1).Data);
        }
        return [.. result];
    }

    static bool InQuantum(double time, long frame) => time >= frame / (double)Rate && time < (frame + Q) / (double)Rate;

    static void Equal(float[] expected, float[] actual)
    {
        Assert.Equal(expected.Length, actual.Length);
        for (var i = 0; i < expected.Length; i++)
            if (expected[i] != actual[i])
                Assert.Fail($"frame {i / (2 * Q) * Q + i % Q} ({(i % (2 * Q) < Q ? "left" : "right")}) differs: {expected[i]:R} by hand, {actual[i]:R} scheduled; by hand first sound at {Array.FindIndex(expected, x => x != 0)}, scheduled at {Array.FindIndex(actual, x => x != 0)}; energy per quantum by hand {string.Join(" ", Enumerable.Range(Math.Max(0, i / (2 * Q) - 3), 6).Select(q => expected.Skip(q * 2 * Q).Take(2 * Q).Sum(Math.Abs).ToString("G3")))}, scheduled {string.Join(" ", Enumerable.Range(Math.Max(0, i / (2 * Q) - 3), 6).Select(q => actual.Skip(q * 2 * Q).Take(2 * Q).Sum(Math.Abs).ToString("G3")))}");
        Assert.Contains(actual, s => s != 0);
    }

    /// <summary>
    /// SoundEffects' voice stated again by hand (the twin of the effect fixtures' JavaScript, Docs/Audio.md,
    /// step 8), started at a given time: the pitch's draw, the level's, the noise offsets', the limits.
    /// </summary>
    sealed class EffectTwin(AudioSystem system)
    {
        readonly AudioGraph _g = system.Graph;
        readonly Func<double> _random = AudioFixtureRun.Mulberry32(7001);
        readonly List<(string Id, GainNode Gain, List<AudioScheduledSourceNode> Sources)> _voices = [];

        double Hz(double value) => Math.Max(30, Math.Min(_g.SampleRate / 2 - 200, value));

        double Draw(EffectRange? range) => range == null ? 1 : range.Min + _random() * (range.Max - range.Min);

        void Steal((string Id, GainNode Gain, List<AudioScheduledSourceNode> Sources) voice, double now)
        {
            voice.Gain.Gain.CancelAndHoldAtTime(now);
            voice.Gain.Gain.LinearRampToValueAtTime(0, now + 0.005);
            foreach (var source in voice.Sources)
                source.Stop(now + 0.005);
            _voices.Remove(voice);
        }

        public void Play(string id, EffectPatch patch, double when, double volume = 1, double pan = 0, double pitch = 1)
        {
            while (_voices.Count(v => v.Id == id) >= patch.MaxVoices)
                Steal(_voices.First(v => v.Id == id), when);
            while (_voices.Count >= 32)
                Steal(_voices[0], when);
            pitch *= Draw(patch.Pitch);
            var level = patch.Level * volume * Draw(patch.Volume);
            var voiceGain = new GainNode(_g);
            voiceGain.Gain.Value = (float)level;
            var panner = new StereoPannerNode(_g);
            panner.Pan.Value = (float)Math.Max(-1, Math.Min(1, pan));
            voiceGain.Connect(panner);
            panner.Connect(system.EffectsBus);
            var nodes = new List<AudioNode> { voiceGain, panner };
            var sources = new List<AudioScheduledSourceNode>();
            AudioScheduledSourceNode? last = null;
            var lastStop = double.NegativeInfinity;
            foreach (var layer in patch.Layers)
            {
                var start = when + layer.Delay;
                var stop = start + layer.Attack + layer.Length + 0.05;
                AudioScheduledSourceNode source;
                if (layer.Source == EffectSource.Noise)
                {
                    var noise = new AudioBufferSourceNode(_g) { Buffer = system.Music.Noise, Loop = true };
                    noise.Start(start, _random() * 1.5);
                    source = noise;
                }
                else
                {
                    var oscillator = new OscillatorNode(_g) { Type = (OscillatorType)(int)layer.Source };
                    oscillator.Frequency.SetValueAtTime((float)Hz(layer.Frequency * pitch), start);
                    if (layer.FrequencyTo is { } to)
                        oscillator.Frequency.ExponentialRampToValueAtTime((float)Hz(to * pitch), start + (layer.SweepTime ?? layer.Length * 0.6));
                    oscillator.Start(start);
                    source = oscillator;
                }
                source.Stop(stop);
                AudioNode head = source;
                if (layer.Filter is { } f)
                {
                    var filter = new BiquadFilterNode(_g) { Type = (BiquadFilterType)(int)f.Type };
                    filter.Frequency.SetValueAtTime((float)Hz(f.Frequency * pitch), start);
                    if (f.FrequencyTo is { } to)
                        filter.Frequency.ExponentialRampToValueAtTime((float)Hz(to * pitch), start + (f.SweepTime ?? layer.Length * 0.6));
                    filter.Q.Value = (float)f.Q;
                    head.Connect(filter);
                    head = filter;
                    nodes.Add(filter);
                }
                var envelope = new GainNode(_g);
                envelope.Gain.SetValueAtTime(0.0001f, start);
                envelope.Gain.LinearRampToValueAtTime((float)layer.Level, start + layer.Attack);
                envelope.Gain.SetTargetAtTime(0, start + layer.Attack, layer.Length / 4);
                head.Connect(envelope);
                envelope.Connect(voiceGain);
                nodes.Add(source);
                nodes.Add(envelope);
                sources.Add(source);
                if (stop > lastStop)
                {
                    last = source;
                    lastStop = stop;
                }
            }
            var voice = (id, voiceGain, sources);
            _voices.Add(voice);
            last!.Ended += _ =>
            {
                _voices.Remove(voice);
                foreach (var node in nodes)
                    node.Disconnect();
            };
        }
    }

    // ---------- The clock ----------

    [Fact]
    public void TheClockReadsTheNextQuantumsStart()
    {
        var system = System();
        Assert.Equal(0, system.Time);
        Render(system, 1);
        Assert.Equal(Q / (double)Rate, system.Time);
        Render(system, 9);
        Assert.Equal(10 * Q / (double)Rate, system.Time);
        Assert.Equal(system.Graph.CurrentTime, system.Time);
        // A call at the clock's time is applied at the start of that quantum: an effect at it plays as one played now.
        var at = system.PlayAtNow();
        var now = System();
        Render(now, 10);
        now.Effects.Play("ping");
        Equal(Render(now, 20), Render(system, 20));
        Assert.Equal(ScheduledSoundState.Done, at.State);
    }

    /// <summary>
    /// An output that takes a buffer at most every millisecond, paused or not, and counts them. It never fills up, so
    /// how many the audio thread writes before the test pauses it doesn't matter. (The sink this replaced stopped taking
    /// buffers after 201 and refused them while paused: under load the thread could spend all 201 before the pause was
    /// taken, and then nothing rendered after the resume; and a thread rendering on while paused went unnoticed.)
    /// </summary>
    sealed class Sink(int sampleRate) : IAudioOutput
    {
        // Read and restarted on the audio thread only.
        readonly Stopwatch _since = Stopwatch.StartNew();
        public int SampleRate { get; } = sampleRate;
        public int BufferFrames { get; } = OpenAlOutput.FramesPerBuffer(sampleRate);
        public volatile bool Paused;
        public volatile int Written;
        public int Writable => _since.ElapsedTicks >= Stopwatch.Frequency / 1000 ? 1 : 0;
        public void Write(ReadOnlySpan<float> interleaved)
        {
            Written++;
            _since.Restart();
        }
        public void Maintain() { }
        public void Pause() => Paused = true;
        public void Resume() => Paused = false;
        public void Wait(int milliseconds) => Thread.Sleep(1);
        public bool HasDevice => true;
        public void Dispose() { }
    }

    /// <summary>
    /// The clock stands still while the sound is paused: the audio thread renders nothing, though the output would take
    /// buffers, and goes on from there once resumed.
    /// </summary>
    [Fact]
    public void TheClockStandsStillWhileTheSoundIsPaused()
    {
        var output = new Sink(48000);
        using var system = new AudioSystem(output, MusicLibrary.Empty);
        Assert.True(SpinWait.SpinUntil(() => output.Written > 10, TimeSpan.FromSeconds(10)));
        system.Pause();
        // The output is paused after the buffer the thread was writing: nothing is written after it.
        Assert.True(SpinWait.SpinUntil(() => output.Paused, TimeSpan.FromSeconds(10)));
        var time = system.Time;
        var written = output.Written;
        Assert.True(time > 0);
        Thread.Sleep(50);
        Assert.Equal(time, system.Time);
        Assert.Equal(written, output.Written);
        Assert.Equal(system.Graph.CurrentTime, time);
        system.Resume();
        Assert.True(SpinWait.SpinUntil(() => system.Time > time, TimeSpan.FromSeconds(10)));
    }

    // ---------- Effects at a time ----------

    /// <summary>Times inside quanta, on a quantum's start, and two plays overlapping with the caller's volume, pan and pitch.</summary>
    [Theory]
    [InlineData(0.1234)]
    [InlineData(40 * Q / 22050.0)]
    [InlineData(0.30001)]
    public void AnEffectAtATimeIsTheGraphBuiltByHandAtThatTime(double time)
    {
        var scheduled = System();
        scheduled.Effects.PlayAt(time, "ping", 0.7, -0.4, 1.1);
        scheduled.Effects.PlayAt(time + 0.0517, "ping");
        // Two at one time: in the order they were asked for.
        scheduled.Effects.PlayAt(time, "ping", 1, 0.9);
        var byHand = System();
        var twin = new EffectTwin(byHand);
        var hand = Render(byHand, 60, frame =>
        {
            if (InQuantum(time, frame))
            {
                twin.Play("ping", Ping, time, 0.7, -0.4, 1.1);
                twin.Play("ping", Ping, time, 1, 0.9);
            }
            if (InQuantum(time + 0.0517, frame))
                twin.Play("ping", Ping, time + 0.0517);
        });
        Equal(hand, Render(scheduled, 60));
    }

    [Fact]
    public void AnEffectAtAQuantumsStartIsThePlayMadeForThatQuantum()
    {
        var scheduled = System();
        scheduled.Effects.PlayAt(12 * Q / (double)Rate, "ping", 0.9, 0.3, 0.8);
        var played = System();
        var hand = Render(played, 40, frame =>
        {
            if (frame == 12 * Q)
                played.Effects.Play("ping", 0.9, 0.3, 0.8);
        });
        Equal(hand, Render(scheduled, 40));
    }

    [Fact]
    public void ATimeAlreadyPastPlaysAtOnce()
    {
        var late = System();
        Render(late, 20);
        var sound = late.Effects.PlayAt(0.01, "ping");
        Assert.Equal(ScheduledSoundState.Pending, sound.State);
        var now = System();
        Render(now, 20);
        now.Effects.Play("ping");
        Equal(Render(now, 30), Render(late, 30));
        Assert.Equal(ScheduledSoundState.Done, sound.State);
        Assert.Throws<ArgumentOutOfRangeException>(() => late.Effects.PlayAt(double.NaN, "ping"));
        Assert.Throws<ArgumentOutOfRangeException>(() => late.Effects.PlayAt(double.PositiveInfinity, "ping"));
        Assert.Throws<ArgumentException>(() => late.Effects.PlayAt(1, "nothing"));
        Assert.Throws<ArgumentOutOfRangeException>(() => late.Effects.PlayAt(1, "ping", pitch: 0));
    }

    /// <summary>
    /// The voice is made in the quantum its time falls in (S6-C): plays waiting count toward no limit, the
    /// third of a two-voice patch steals the first when it starts, and the draws follow the starts, not
    /// the calls (the same plays asked for in another order sound the same).
    /// </summary>
    [Fact]
    public void TheVoiceIsMadeInTheQuantumItsTimeFallsIn()
    {
        var times = new[] { 0.1, 0.2, 0.3 };
        var scheduled = System();
        foreach (var time in times)
            scheduled.Effects.PlayAt(time, "hum");
        var counts = new List<(int Pending, int Voices)>();
        var actual = Render(scheduled, 80, frame =>
        {
            if (frame == Q || frame == 26 * Q || frame == 60 * Q)
                counts.Add((scheduled.Effects.Pending, scheduled.Effects.ActiveVoices));
        });
        // Before 0.1 s none sounds; at 0.15 s one; past 0.3 s two (the third stole the first).
        Assert.Equal([(3, 0), (2, 1), (0, 2)], counts);
        var byHand = System();
        var twin = new EffectTwin(byHand);
        var hand = Render(byHand, 80, frame =>
        {
            foreach (var time in times)
                if (InQuantum(time, frame))
                    twin.Play("hum", Hum, time);
        });
        Equal(hand, actual);

        // Draws in the order the voices start.
        var forwards = System();
        forwards.Effects.PlayAt(0.1, "ping");
        forwards.Effects.PlayAt(0.15, "ping", 1, 0.5);
        var backwards = System();
        backwards.Effects.PlayAt(0.15, "ping", 1, 0.5);
        backwards.Effects.PlayAt(0.1, "ping");
        Equal(Render(forwards, 40), Render(backwards, 40));
    }

    /// <summary>Past the 32 voices overall, the oldest fades out from the time the new voice starts.</summary>
    [Fact]
    public void TheOverallLimitStealsWhenTheVoiceStarts()
    {
        var scheduled = System();
        var byHand = System();
        var twin = new EffectTwin(byHand);
        var times = Enumerable.Range(0, 34).Select(i => 0.05 + i * 0.00317).ToArray();
        for (var i = 0; i < times.Length; i++)
        {
            scheduled.Effects.Register($"p{i}", Hum with { Layers = [Hum.Layers[0] with { Frequency = 200 + 23 * i }], Level = 0.02 });
            scheduled.Effects.PlayAt(times[i], $"p{i}");
        }
        var hand = Render(byHand, 40, frame =>
        {
            for (var i = 0; i < times.Length; i++)
                if (InQuantum(times[i], frame))
                    twin.Play($"p{i}", Hum with { Layers = [Hum.Layers[0] with { Frequency = 200 + 23 * i }], Level = 0.02 }, times[i]);
        });
        Equal(hand, Render(scheduled, 40));
        Assert.Equal(SoundEffects.MaxVoicesOverall, scheduled.Effects.ActiveVoices);
    }

    [Fact]
    public void ACancelledPlayLeavesTheRestAsItWasAndAddsNothing()
    {
        var cancelled = System();
        cancelled.Effects.PlayAt(0.05, "ping");
        var gone = cancelled.Effects.PlayAt(0.1, "ping", 1, -1);
        var kept = cancelled.Effects.PlayAt(0.2, "ping", 1, 0.5);
        var states = new List<ScheduledSoundState>();
        var actual = Render(cancelled, 70, frame =>
        {
            if (frame == 3 * Q)
            {
                gone.Cancel();
                // Queued like every call: still pending until the audio thread takes it.
                states.Add(gone.State);
            }
            if (frame == 4 * Q)
                states.Add(gone.State);
            if (frame == 45 * Q)
            {
                // Cancelling what happened (or was cancelled) already does nothing.
                states.Add(kept.State);
                kept.Cancel();
                gone.Cancel();
            }
        });
        Assert.Equal([ScheduledSoundState.Pending, ScheduledSoundState.Cancelled, ScheduledSoundState.Done], states);
        Assert.Equal((ScheduledSoundState.Done, ScheduledSoundState.Cancelled), (kept.State, gone.State));
        // The draws the cancelled play would have made aren't made: what follows sounds as if it never was.
        var without = System();
        without.Effects.PlayAt(0.05, "ping");
        without.Effects.PlayAt(0.2, "ping", 1, 0.5);
        Equal(Render(without, 70), actual);
    }

    // ---------- The music at a time ----------

    /// <summary>Two small themes (one bar per step, as the music fixtures' own): pads and a bass in the chamber; a drone and drums.</summary>
    static readonly CompiledTheme ThemeA = Score.Compile(ThemeJson.Parse("""{ "id": "a", "title": "a", "tempo": 120, "meter": [1, 1], "tonic": 60, "mode": "dorian", "room": "chamber", "sections": { "A": { "chords": "1 | 4 | 5 | 1", "layers": [{ "t": "pad", "i": "violins", "o": 0, "v": 0.7, "n": 2 }, { "t": "bass", "i": "sub", "o": -2, "v": 0.7, "p": "1" }] } }, "form": ["A"] }"""));
    static readonly CompiledTheme ThemeB = Score.Compile(ThemeJson.Parse("""{ "id": "b", "title": "b", "tempo": 100, "meter": [1, 1], "tonic": 60, "mode": "dorian", "room": "chamber", "sections": { "A": { "chords": "6 | 4", "layers": [{ "t": "drone", "i": "organ", "o": 0, "v": 0.6, "deg": ["1"], "every": 2 }, { "t": "drums", "v": 0.6, "p": { "frame": "x." } }] } }, "form": ["A"] }"""));

    /// <summary>A soundtrack on its own graph (no audio thread: calls apply at once), its scheduling before every quantum after the first.</summary>
    static float[] RenderMusic(Soundtrack music, AudioGraph graph, int quanta, Action<long>? before = null)
    {
        var bus = new AudioBus(2, Q);
        var result = new List<float>();
        for (var q = 0; q < quanta; q++)
        {
            before?.Invoke(graph.CurrentSampleFrame);
            if (q > 0)
                music.Schedule();
            graph.RenderQuantum(bus);
            result.AddRange(bus.Channel(0).Data);
            result.AddRange(bus.Channel(1).Data);
        }
        return [.. result];
    }

    /// <summary>
    /// music.js's sync stated again by hand on a soundtrack's master chain (Docs/Audio.md, step 6): the old
    /// bus held at the switch's time and ramped to 0 over 0.55 s, let go at the first quantum 1.8 s later;
    /// the new performance's bus from 0 to 1 over 0.8 s, its steps from 60 ms after the switch, each
    /// scheduled once it starts within 0.2 s.
    /// </summary>
    sealed class MusicTwin(Soundtrack chain, AudioGraph graph)
    {
        Performance? _performance;
        GainNode? _bus;
        double _next;
        double _step;
        readonly List<(double Time, GainNode Bus)> _retired = [];

        public void Switch(CompiledTheme? theme, double at)
        {
            if (_bus is { } old)
            {
                old.Gain.CancelAndHoldAtTime(at);
                old.Gain.LinearRampToValueAtTime(0, at + 0.55);
                _retired.Add((at + 1.8, old));
            }
            _performance = null;
            _bus = null;
            if (theme is null)
                return;
            _performance = chain.Perform(theme);
            _bus = _performance.Output;
            _bus.Gain.SetValueAtTime(0, at);
            _bus.Gain.LinearRampToValueAtTime(1, at + 0.8);
            _next = at + 0.06;
            _step = 0;
            Steps();
        }

        public void Steps()
        {
            for (var i = 0; i < _retired.Count;)
                if (Math.Ceiling(Math.Floor(_retired[i].Time * graph.SampleRateF) / Q) * Q <= graph.CurrentSampleFrame)
                {
                    _retired[i].Bus.Disconnect();
                    _retired.RemoveAt(i);
                }
                else
                    i++;
            if (_performance is null)
                return;
            while (_next < graph.CurrentTime + 0.2)
            {
                _performance.Step(_step, _next);
                _next += _performance.StepSeconds;
                _step++;
            }
        }
    }

    /// <summary>
    /// A switch and a stop at times inside quanta, while a theme plays: music.js's fades from those times,
    /// sample for sample as the switch made by hand there.
    /// </summary>
    [Fact]
    public void AMusicSwitchAndStopAtATimeAreMusicJsSwitchByHandAtThoseTimes()
    {
        const double SwitchAt = 0.5123, StopAt = 1.3077;
        var graph = new AudioGraph(Rate, 2);
        var music = new Soundtrack(graph, graph.Destination, TestMusic.Library);
        music.Play("a", ThemeA);
        var switched = music.NewSound(SwitchAt);
        music.Schedule(switched, "b", ThemeB);
        var stopped = music.NewSound(StopAt);
        music.ScheduleStop(stopped);
        var themes = new List<string?>();
        var actual = RenderMusic(music, graph, 280, frame =>
        {
            if (frame == 100 * Q || frame == 240 * Q)
                themes.Add(music.Theme);
        });
        Assert.Equal(["b", null], themes);
        Assert.Equal((ScheduledSoundState.Done, ScheduledSoundState.Done), (switched.State, stopped.State));

        var handGraph = new AudioGraph(Rate, 2);
        var twin = new MusicTwin(new Soundtrack(handGraph, handGraph.Destination, TestMusic.Library), handGraph);
        twin.Switch(ThemeA, 0);
        var hand = RenderHand(handGraph, 280, frame =>
        {
            if (frame == 0)
                return;
            if (InQuantum(SwitchAt, frame))
                twin.Switch(ThemeB, SwitchAt);
            else if (InQuantum(StopAt, frame))
                twin.Switch(null, StopAt);
            twin.Steps();
        });
        Equal(hand, actual);
    }

    static float[] RenderHand(AudioGraph graph, int quanta, Action<long> before)
    {
        var bus = new AudioBus(2, Q);
        var result = new List<float>();
        for (var q = 0; q < quanta; q++)
        {
            before(graph.CurrentSampleFrame);
            graph.RenderQuantum(bus);
            result.AddRange(bus.Channel(0).Data);
            result.AddRange(bus.Channel(1).Data);
        }
        return [.. result];
    }

    /// <summary>A switch at a quantum's start is the switch made for that quantum; one to the theme playing then does nothing; a cancelled one leaves it playing.</summary>
    [Fact]
    public void AMusicSwitchAtAQuantumsStartIsTheSwitchMadeForThatQuantum()
    {
        var graph = new AudioGraph(Rate, 2);
        var music = new Soundtrack(graph, graph.Destination, TestMusic.Library);
        music.Play("a", ThemeA);
        music.Schedule(music.NewSound(30 * Q / (double)Rate), "b", ThemeB);
        music.Schedule(music.NewSound(60 * Q / (double)Rate), "b", ThemeB);
        var cancelled = music.NewSound(70 * Q / (double)Rate);
        music.Schedule(cancelled, "a", ThemeA);
        var actual = RenderMusic(music, graph, 120, frame =>
        {
            if (frame == 65 * Q)
                cancelled.CancelNow();
        });
        Assert.Equal(ScheduledSoundState.Cancelled, cancelled.State);

        var handGraph = new AudioGraph(Rate, 2);
        var played = new Soundtrack(handGraph, handGraph.Destination, TestMusic.Library);
        played.Play("a", ThemeA);
        var hand = RenderMusic(played, handGraph, 120, frame =>
        {
            if (frame == 30 * Q)
                played.Play("b", ThemeB);
        });
        Equal(hand, actual);
    }

    /// <summary>A small theme on the game's instruments (as the reload tests' own).</summary>
    static ThemeSpec Small(string id, double tempo = 120) => new()
    {
        Id = id, Title = id, Tempo = tempo, Meter = [1, 1], Tonic = 60, Mode = "dorian", Room = "chamber",
        Sections = new Dictionary<string, SectionSpec>
        {
            ["A"] = new() { Chords = "1 | 5", Layers = [new() { Type = "pad", Instrument = "violins", Octave = 0, Velocity = 0.7, Voices = 2 }] },
        },
        Form = ["A"],
    };

    static MusicLibrary Library(params ThemeSpec[] themes) =>
        new(themes, TestMusic.Library.Instruments, TestMusic.Library.Percussion, TestMusic.Library.Rooms, new Dictionary<string, EffectPatch>());

    /// <summary>
    /// What a call at a time left playing is what the music plays as far as a reload is concerned (S9-D): a
    /// theme switched to at a time (or at once, a sequence's skip) restarts when its music changed; one
    /// stopped at a time stays stopped.
    /// </summary>
    [Theory]
    [InlineData("played at a time")]
    [InlineData("switched now")]
    [InlineData("stopped at a time")]
    public void AReloadSeesWhatTheCallsAtATimeLeftPlaying(string how)
    {
        var old = Library(Small("x"), Small("y", 100));
        float[] Run(bool reload)
        {
            var system = new AudioSystem(new AudioGraph(Rate, 2), old);
            if (how == "stopped at a time")
            {
                system.Music.Play("x");
                system.Music.StopAt(0.1);
            }
            else
            {
                system.Music.Play("y");
                if (how == "played at a time")
                    system.Music.PlayAt(0.1, "x");
                else
                    system.Post(() => system.Music.ApplyNow("x", old.Compiled("x")));
            }
            Render(system, 40);
            if (reload)
                system.Reload(Library(Small("x", 150), Small("y", 100)));
            var after = Render(system, 100);
            Assert.Equal(how == "stopped at a time" ? null : "x", system.Music.Theme);
            return after;
        }
        if (how == "stopped at a time")
            Assert.Equal(Run(false), Run(true));
        else
            Assert.NotEqual(Run(false), Run(true));
    }

    /// <summary>Through the audio system: a theme of its music at a time, and the stop; the calls queued as the others are.</summary>
    [Fact]
    public void TheAudioSystemsMusicPlaysAndStopsAtATime()
    {
        var system = System(TestMusic.Library);
        var play = system.Music.PlayAt(0.2, "alder-vale");
        var stop = system.Music.StopAt(0.6);
        var themes = new List<string?>();
        Render(system, 140, frame =>
        {
            if (frame == 45 * Q || frame == 120 * Q)
                themes.Add(system.Music.Theme);
        });
        Assert.Equal(["alder-vale", null], themes);
        Assert.Equal((ScheduledSoundState.Done, ScheduledSoundState.Done), (play.State, stop.State));
        Assert.Throws<ArgumentException>(() => system.Music.PlayAt(1, "nothing"));
        Assert.Throws<ArgumentOutOfRangeException>(() => system.Music.PlayAt(double.NaN, "alder-vale"));
        Assert.Throws<ArgumentOutOfRangeException>(() => system.Music.StopAt(double.NegativeInfinity));
    }
}

static class ScheduledSoundTestExtensions
{
    /// <summary>An effect at the clock's time now.</summary>
    public static ScheduledSound PlayAtNow(this AudioSystem system) => system.Effects.PlayAt(system.Time, "ping");
}

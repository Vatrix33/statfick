using System.Collections.Concurrent;
using System.Diagnostics;
using Statventure.Core;

namespace Statventure.Audio;

/// <summary>
/// The game's sound (Docs/Audio.md, step 7): an audio graph at the output device's rate, rendered
/// by an audio thread about 20 ms ahead in 128-frame quanta through OpenAL Soft; the music
/// (<see cref="Music"/>, music.js's master chain into the master bus) and the effects bus mixed on
/// the master bus. The game's calls are queued and applied before the next quantum; nothing locks
/// while rendering. With no device the graph keeps rendering in real time, silently.
/// </summary>
public sealed class AudioSystem : IDisposable
{
    readonly IAudioOutput? _output;
    readonly AudioGraph _graph;
    readonly GainNode _master, _effects;
    readonly AudioBus _bus = new(2, AudioGraph.RenderQuantumFrames);
    readonly ConcurrentQueue<Action> _commands = new();
    readonly Thread? _thread;
    readonly AutoResetEvent _wake = new(false);
    readonly long[] _quantumTicks = new long[4096];
    volatile bool _stop, _pauseRequested;
    bool _paused, _failed;
    double _masterVolume = 1, _effectsVolume = 1;
    long _quanta;

    /// <summary>Opens the default output device (or runs silently without one) and starts the audio thread.</summary>
    public AudioSystem() : this(new OpenAlOutput()) { }

    internal AudioSystem(IAudioOutput output, bool startThread = true) : this(new AudioGraph(output.SampleRate, 2))
    {
        _output = output;
        // The basic waves' tables now, so a first sawtooth doesn't make them on the audio thread.
        foreach (var shape in new[] { OscillatorType.Sine, OscillatorType.Square, OscillatorType.Sawtooth, OscillatorType.Triangle })
            _graph.BasicWave(shape);
        if (startThread)
        {
            _thread = new Thread(Run) { Name = "Statventure audio", IsBackground = true, Priority = ThreadPriority.Highest };
            _thread.Start();
            new Thread(WarmUp) { Name = "Statventure audio warm-up", IsBackground = true, Priority = ThreadPriority.BelowNormal }.Start();
        }
    }

    /// <summary>
    /// Once the rooms are ready, renders a second of a region, battle and boss theme into a throwaway
    /// graph, so the JIT has compiled the composer's and the nodes' code before the first theme
    /// change on the audio thread (a cold first start cost about 60 ms there).
    /// </summary>
    internal void WarmUp()
    {
        RoomsReady.Wait();
        var graph = new AudioGraph(_graph.SampleRateF, 2);
        var master = new GainNode(graph);
        master.Connect(graph.Destination);
        var rate = graph.SampleRateF;
        var music = new Soundtrack(graph, master, preparedRooms: name => PreparedRoom.Get(name, rate));
        var bus = new AudioBus(2, AudioGraph.RenderQuantumFrames);
        foreach (var id in new[] { "alder-vale", "combat", "great-imp-jiruan" })
        {
            music.Play(id);
            for (var q = 0; q < graph.SampleRateF / AudioGraph.RenderQuantumFrames; q++)
            {
                music.Schedule();
                graph.RenderQuantum(bus);
            }
        }
    }

    /// <summary>The mixer on a given graph, without output or thread (the fixtures render it and call <see cref="BeforeQuantum"/>).</summary>
    internal AudioSystem(AudioGraph graph)
    {
        _graph = graph;
        _master = new GainNode(_graph);
        _master.Connect(_graph.Destination);
        _effects = new GainNode(_graph);
        _effects.Connect(_master);
        // A worker makes all the rooms' responses and convolver kernels now, so starting a theme on the
        // audio thread only connects nodes (step 7's decision: a room's setup took 10–13 ms there). One
        // worker, the rooms in turn, leaving the other cores to the game and the audio thread; they are
        // kept for the process, per rate, as they never change.
        var rate = _graph.SampleRateF;
        var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        RoomsReady = ready.Task;
        new Thread(() =>
        {
            foreach (var name in MusicData.Rooms.Keys)
                PreparedRoom.Get(name, rate);
            ready.SetResult();
        }) { Name = "Statventure audio rooms", IsBackground = true, Priority = ThreadPriority.BelowNormal }.Start();
        Music = new Soundtrack(_graph, _master, dispatch: Post, preparedRooms: name => PreparedRoom.Get(name, rate));
        Effects = new SoundEffects(_graph, _effects, Music.Noise, Post);
    }

    /// <summary>Done when every room is prepared.</summary>
    internal Task RoomsReady { get; }

    /// <summary>The music: themes, their fades, the music volume.</summary>
    public Soundtrack Music { get; }

    /// <summary>The sound effects: patches played one-shot on the effects bus.</summary>
    public SoundEffects Effects { get; }

    /// <summary>The rate the graph renders at.</summary>
    public int SampleRate => (int)_graph.SampleRateF;

    /// <summary>Whether sound reaches a device now.</summary>
    public bool HasDevice => _output?.HasDevice ?? false;

    /// <summary>The master volume, 0 to 1: a last gain on the mix, ramped over 40 ms.</summary>
    public double MasterVolume
    {
        get => _masterVolume;
        set
        {
            if (!double.IsFinite(value))
                return;
            var volume = _masterVolume = Math.Clamp(value, 0, 1);
            Post(() => Ramp(_master.Gain, volume));
        }
    }

    /// <summary>The effects volume, 0 to 1: a gain on the effects bus, ramped over 40 ms.</summary>
    public double EffectsVolume
    {
        get => _effectsVolume;
        set
        {
            if (!double.IsFinite(value))
                return;
            var volume = _effectsVolume = Math.Clamp(value, 0, 1);
            Post(() => Ramp(_effects.Gain, volume));
        }
    }

    /// <summary>How often the device ran out of rendered audio (OpenAL only).</summary>
    internal int Underruns => (_output as OpenAlOutput)?.Underruns ?? 0;

    /// <summary>The OpenAL output, for the tests (null on other outputs).</summary>
    internal OpenAlOutput? OpenAl => _output as OpenAlOutput;

    /// <summary>The master bus's gain, for the measuring tools (silent runs).</summary>
    internal AudioParam MasterGain => _master.Gain;

    /// <summary>The effects bus, which <see cref="Effects"/> play into.</summary>
    internal GainNode EffectsBus => _effects;

    internal AudioGraph Graph => _graph;

    /// <summary>music.js's volume ramp: hold the current value, then 40 ms to the new one.</summary>
    void Ramp(AudioParam gain, double volume)
    {
        var now = _graph.CurrentTime;
        gain.CancelScheduledValues(now);
        gain.SetValueAtTime(gain.Value, now);
        gain.LinearRampToValueAtTime((float)volume, now + 0.04);
    }

    /// <summary>Pauses all audio where it is (the engine does when the window is minimized).</summary>
    public void Pause()
    {
        _pauseRequested = true;
        _wake.Set();
    }

    /// <summary>Goes on from where <see cref="Pause"/> stopped.</summary>
    public void Resume()
    {
        _pauseRequested = false;
        _wake.Set();
    }

    public bool Paused => _pauseRequested;

    /// <summary>Queues <paramref name="action"/> for the audio thread, before its next quantum.</summary>
    internal void Post(Action action) => _commands.Enqueue(action);

    /// <summary>
    /// Renders one output buffer: per quantum, the queued calls, then the soundtrack's scheduling, then
    /// the quantum, interleaved into <paramref name="interleaved"/>.
    /// </summary>
    internal void RenderBuffer(Span<float> interleaved)
    {
        const int Q = AudioGraph.RenderQuantumFrames;
        var frames = interleaved.Length / 2;
        for (var start = 0; start < frames; start += Q)
        {
            var begin = Stopwatch.GetTimestamp();
            BeforeQuantum();
            _graph.RenderQuantum(_bus);
            var left = _bus.Channel(0).Data;
            var right = _bus.Channel(1).Data;
            for (var i = 0; i < Q; i++)
            {
                interleaved[2 * (start + i)] = left[i];
                interleaved[2 * (start + i) + 1] = right[i];
            }
            _quantumTicks[_quanta++ % _quantumTicks.Length] = Stopwatch.GetTimestamp() - begin;
        }
    }

    /// <summary>Before each quantum: the queued calls in order, then the soundtrack's scheduling.</summary>
    internal void BeforeQuantum()
    {
        while (_commands.TryDequeue(out var command))
            command();
        Music.Schedule();
    }

    /// <summary>The last quanta's render times in milliseconds (up to 4096), for measuring.</summary>
    internal double[] RecentQuantumMilliseconds() =>
        [.. _quantumTicks.Take((int)Math.Min(_quanta, _quantumTicks.Length)).Select(t => t * 1000.0 / Stopwatch.Frequency)];

    void Run()
    {
        var output = _output!;
        var buffer = new float[output.BufferFrames * 2];
        var waitMilliseconds = Math.Max(1, output.BufferFrames * 1000 / output.SampleRate / 2);
        while (!_stop)
        {
            if (_pauseRequested != _paused)
            {
                _paused = _pauseRequested;
                if (_paused)
                    output.Pause();
                else
                    output.Resume();
            }
            if (_paused)
            {
                _wake.WaitOne(100);
                continue;
            }
            try
            {
                output.Maintain();
                var writable = output.Writable;
                for (var i = 0; i < writable; i++)
                {
                    if (_failed)
                        buffer.AsSpan().Clear();
                    else
                        RenderBuffer(buffer);
                    output.Write(buffer);
                }
                output.Maintain();
                if (writable == 0)
                    output.Wait(waitMilliseconds);
            }
            catch (Exception exception)
            {
                // A bug while rendering: log it once and go on with silence rather than end the thread.
                if (!_failed)
                    Log.Error("Audio", exception);
                _failed = true;
            }
        }
    }

    public void Dispose()
    {
        _stop = true;
        _wake.Set();
        _thread?.Join();
        _output?.Dispose();
        _wake.Dispose();
    }
}

namespace Statventure.Audio;

/// <summary>
/// The game's music (Docs/Audio.md, step 6): music.js's master chain (a gain of 0.58 × the volume
/// into a limiter), its themes performed by the composer port, and its switching: <see cref="Play"/>
/// fades the theme playing out over 0.55 s (and lets its bus go 1.8 s later) while the new one fades
/// in over 0.8 s, scheduling steps 0.2 s ahead. Themes are named by the old game's ids
/// ("alder-vale", "combat@alder-vale", a boss's id); which one plays is the game's choice. The
/// themes and the composer's instruments come from a <see cref="MusicLibrary"/>. The
/// renderer calls <see cref="Schedule"/> before each render quantum (music.js runs it every 45 ms;
/// the notes come out the same, all of them being scheduled ahead of their time). Under an
/// <see cref="AudioSystem"/> the game's calls are queued and applied by the audio thread before its
/// next quantum (step 7's decision), "now" being the graph's time then.
/// </summary>
public sealed class Soundtrack
{
    const double MasterLevel = 0.58;

    readonly GraphBuilder _builder;
    readonly GainNode _master;
    readonly List<(double Time, Action Action)> _timers = [];
    readonly Action<Action>? _dispatch;
    readonly Func<string, PreparedRoom?>? _preparedRooms;
    Performance? _performance;
    GainNode? _bus;
    double _nextNoteTime;
    double _step;
    double _volume;

    /// <summary>Every theme's id, in the library's order (the old game's: regions, the battle theme and its regional arrangements, bosses).</summary>
    public IReadOnlyList<string> Themes => Library.Themes;

    /// <summary>The themes, instruments, percussion voices and rooms this plays.</summary>
    public MusicLibrary Library { get; }

    /// <summary>
    /// music.js's setupAudio for the composer's themes: master gain → DynamicsCompressor (−18 dB,
    /// knee 18, ratio 3, attack 6 ms, release 220 ms) → <paramref name="destination"/>. Exports
    /// (renderOffline) keep the original loudness whatever the volume. The old score engine's delay and
    /// hall are left out: no theme feeds them, so they only ever add zeros.
    /// </summary>
    internal Soundtrack(AudioGraph graph, AudioNode destination, MusicLibrary library, double volume = 1, bool export = false, Action<Action>? dispatch = null,
        Func<string, PreparedRoom?>? preparedRooms = null)
    {
        Library = library;
        _builder = new GraphBuilder(graph, library) { PreparedRooms = preparedRooms };
        _preparedRooms = preparedRooms;
        _dispatch = dispatch;
        _volume = Math.Clamp(volume, 0, 1);
        _master = new GainNode(graph);
        _master.Gain.Value = (float)(MasterLevel * (export ? 1 : _volume));
        var limiter = new DynamicsCompressorNode(graph);
        limiter.Threshold.Value = -18;
        limiter.Knee.Value = 18;
        limiter.Ratio.Value = 3;
        limiter.Attack.Value = 0.006f;
        limiter.Release.Value = 0.22f;
        _master.Connect(limiter);
        limiter.Connect(destination);
    }

    AudioGraph Graph => _builder.Graph;

    /// <summary>The composer's two seconds of noise on this graph (the sound effects use it too).</summary>
    internal AudioBuffer Noise => ComposerKit.For(_builder).Noise;

    /// <summary>The theme playing (as the audio thread last applied the calls), or null.</summary>
    public string? Theme { get; private set; }

    /// <summary>Runs <paramref name="action"/> on the audio thread before its next quantum, or now without one.</summary>
    void Dispatch(Action action)
    {
        if (_dispatch != null)
            _dispatch(action);
        else
            action();
    }

    /// <summary>The volume, 0 to 1: the master gain ramps to 0.58 × it over 40 ms (music.js's setVolume).</summary>
    public double Volume
    {
        get => _volume;
        set
        {
            if (!double.IsFinite(value))
                return;
            var volume = _volume = Math.Clamp(value, 0, 1);
            Dispatch(() => ApplyVolume(volume));
        }
    }

    void ApplyVolume(double volume)
    {
        var now = Graph.CurrentTime;
        _master.Gain.CancelScheduledValues(now);
        _master.Gain.SetValueAtTime(_master.Gain.Value, now);
        _master.Gain.LinearRampToValueAtTime((float)(MasterLevel * volume), now + 0.04);
    }

    /// <summary>A performance of <paramref name="compiled"/> into the master gain, as renderOffline makes it.</summary>
    internal Performance Perform(CompiledTheme compiled) => new(_builder, compiled, _master);

    /// <summary>
    /// Plays <paramref name="themeId"/> from its start (music.js's sync when the desired theme changes):
    /// the old bus fades out, the new performance's bus fades in from 0 over 0.8 s, its first step 60 ms
    /// from now. Playing the theme that is playing does nothing. A theme the library doesn't have is an
    /// <see cref="ArgumentException"/>.
    /// </summary>
    public void Play(string themeId)
    {
        if (Library.Compiled(themeId) is not { } compiled)
            throw new ArgumentException($"No theme '{themeId}'", nameof(themeId));
        // If the worker is still preparing this room (just after start), the caller waits for it, not the audio thread.
        _preparedRooms?.Invoke(compiled.Room);
        Dispatch(() => Play(themeId, compiled));
    }

    /// <summary><see cref="Play(string)"/> of a compiled theme (the tests' small themes).</summary>
    internal void Play(string themeId, CompiledTheme compiled)
    {
        if (themeId == Theme)
            return;
        Retire(_bus);
        var now = Graph.CurrentTime;
        _performance = new Performance(_builder, compiled, _master);
        _bus = _performance.Output;
        _bus.Gain.SetValueAtTime(0, now);
        _bus.Gain.LinearRampToValueAtTime(1, now + 0.8);
        Theme = themeId;
        _step = 0;
        _nextNoteTime = now + 0.06;
        Schedule();
    }

    /// <summary>Fades the theme playing out (music.js's stop).</summary>
    public void Stop() => Dispatch(StopNow);

    void StopNow()
    {
        Retire(_bus);
        _bus = null;
        _performance = null;
        Theme = null;
    }

    /// <summary>music.js's retire: hold the bus where it is, ramp it to 0 over 0.55 s, disconnect it 1.8 s later.</summary>
    void Retire(GainNode? bus)
    {
        if (bus == null)
            return;
        var now = Graph.CurrentTime;
        bus.Gain.CancelAndHoldAtTime(now);
        bus.Gain.LinearRampToValueAtTime(0, now + 0.55);
        _timers.Add((now + 1.8, bus.Disconnect));
    }

    /// <summary>
    /// Before each render quantum, on the thread that renders: runs the timers that are due (at the
    /// first quantum boundary at or after their time) and schedules every step that starts within
    /// 0.2 s (music.js's schedule).
    /// </summary>
    internal void Schedule()
    {
        var now = Graph.CurrentTime;
        for (var i = 0; i < _timers.Count;)
            if (Math.Ceiling(Math.Floor(_timers[i].Time * Graph.SampleRateF) / AudioGraph.RenderQuantumFrames) * AudioGraph.RenderQuantumFrames <= Graph.CurrentSampleFrame)
            {
                var action = _timers[i].Action;
                _timers.RemoveAt(i);
                action();
            }
            else
                i++;
        if (_performance == null)
            return;
        if (_nextNoteTime < now - 0.5)
            _nextNoteTime = now + 0.04;
        while (_nextNoteTime < now + 0.2)
        {
            _performance.Step(_step, _nextNoteTime);
            _nextNoteTime += _performance.StepSeconds;
            _step++;
        }
    }

    /// <summary>The length of music.js's renderOffline of a theme: one pass of the form plus 4.5 s of tail.</summary>
    internal static int OfflineLength(MusicLibrary library, string themeId, float sampleRate)
    {
        var compiled = library.Compiled(themeId) ?? throw new ArgumentException($"No theme '{themeId}'", nameof(themeId));
        return (int)Math.Ceiling((compiled.TotalSteps * compiled.StepSeconds + 4.5) * sampleRate);
    }

    /// <summary>
    /// music.js's renderOffline: a stereo offline graph at <paramref name="sampleRate"/>, the master
    /// chain at export loudness, every step of one pass of the form scheduled at 0.05 + n · stepSeconds
    /// up front, rendered for <see cref="OfflineLength"/> frames.
    /// </summary>
    internal static float[][] RenderOffline(MusicLibrary library, string themeId, float sampleRate = 44100, Random? summingOrder = null)
    {
        var compiled = library.Compiled(themeId) ?? throw new ArgumentException($"No theme '{themeId}'", nameof(themeId));
        var graph = new AudioGraph(sampleRate, 2) { SummingOrder = summingOrder };
        var soundtrack = new Soundtrack(graph, graph.Destination, library, export: true);
        var performance = soundtrack.Perform(compiled);
        for (var index = 0; index < compiled.TotalSteps; index++)
            performance.Step(index, 0.05 + index * compiled.StepSeconds);
        return graph.RenderOffline(OfflineLength(library, themeId, sampleRate));
    }
}

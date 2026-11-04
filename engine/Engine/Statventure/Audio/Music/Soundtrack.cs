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
/// next quantum (step 7's decision), "now" being the graph's time then. <see cref="PlayAt"/> and
/// <see cref="StopAt"/> make the same switch at a time on the audio clock, in the quantum it falls in,
/// "now" being that time (Docs/Scene.md, S6-B).
/// </summary>
public sealed class Soundtrack
{
    const double MasterLevel = 0.58;

    readonly GainNode _master;
    readonly List<(double Time, Action Action)> _timers = [];
    readonly Action<Action>? _dispatch;
    readonly AudioSchedule _schedule;
    /// <summary>The composer's way to the graph for the library the audio thread plays (a new one after <see cref="Use"/>).</summary>
    GraphBuilder _builder;
    Func<string, PreparedRoom?>? _preparedRooms;
    /// <summary>The theme the game's last <see cref="Play(string)"/> asked for (null after <see cref="Stop"/>): what plays once its calls are applied.</summary>
    string? _requested;
    Performance? _performance;
    GainNode? _bus;
    double _nextNoteTime;
    double _step;
    double _volume;

    /// <summary>Every theme's id, in the library's order (the old game's: regions, the battle theme and its regional arrangements, bosses).</summary>
    public IReadOnlyList<string> Themes => Library.Themes;

    /// <summary>The themes, instruments, percussion voices and rooms this plays.</summary>
    public MusicLibrary Library { get; private set; }

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
        _schedule = new AudioSchedule(graph);
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
        _requested = themeId;
        Dispatch(() => Play(themeId, compiled));
    }

    /// <summary>
    /// <see cref="Play(string)"/> at <paramref name="time"/> on the audio clock (<see cref="AudioSystem.Time"/>; a
    /// time past when the audio thread takes the call: at once): in the quantum it falls in, music.js's fades
    /// from that time (the old bus held there and ramped to 0 over 0.55 s, the new one from 0 to 1 over 0.8 s,
    /// its first step 60 ms after it); the theme playing then does nothing. The handle cancels it until then.
    /// </summary>
    public ScheduledSound PlayAt(double time, string themeId)
    {
        if (!double.IsFinite(time))
            throw new ArgumentOutOfRangeException(nameof(time), time, "a time on the audio clock is a finite number");
        var compiled = Prepared(themeId);
        var sound = NewSound(time);
        Dispatch(() => Schedule(sound, themeId, compiled));
        return sound;
    }

    /// <summary><see cref="Stop"/> at <paramref name="time"/> on the audio clock: the theme playing then fades out from that time.</summary>
    public ScheduledSound StopAt(double time)
    {
        if (!double.IsFinite(time))
            throw new ArgumentOutOfRangeException(nameof(time), time, "a time on the audio clock is a finite number");
        var sound = NewSound(time);
        Dispatch(() => ScheduleStop(sound));
        return sound;
    }

    /// <summary>The theme compiled, its room ready (an <see cref="ArgumentException"/> for a theme the library hasn't).</summary>
    internal CompiledTheme Prepared(string themeId)
    {
        if (Library.Compiled(themeId) is not { } compiled)
            throw new ArgumentException($"No theme '{themeId}'", nameof(themeId));
        _preparedRooms?.Invoke(compiled.Room);
        return compiled;
    }

    internal ScheduledSound NewSound(double time) => new(time, _schedule, Dispatch);

    /// <summary>
    /// On the audio thread: <paramref name="sound"/> switches to the theme at its time. Its compiled theme is the
    /// library's when it was made (a reload before its time doesn't change it); once applied it is what the
    /// game's last call asked for, as for <see cref="Play(string)"/>.
    /// </summary>
    internal void Schedule(ScheduledSound sound, string themeId, CompiledTheme compiled) => _schedule.Add(sound, when =>
    {
        _requested = themeId;
        Play(themeId, compiled, when);
    });

    /// <summary>On the audio thread, now (a sequence's music cue at a skip, Q28): the switch to the theme (null: the stop) with the usual fades, as the game's call.</summary>
    internal void ApplyNow(string? themeId, CompiledTheme? compiled)
    {
        _requested = themeId;
        if (themeId is null)
            StopNow();
        else
            Play(themeId, compiled!);
    }

    /// <summary>On the audio thread: <paramref name="sound"/> stops the music at its time.</summary>
    internal void ScheduleStop(ScheduledSound sound) => _schedule.Add(sound, when =>
    {
        _requested = null;
        StopNow(when);
    });

    /// <summary>
    /// Plays <paramref name="library"/> from now on (the data reloaded; Docs/Assets.md, S9-D), on the game's
    /// thread: later calls use it at once; the theme playing restarts from its start with the new data,
    /// through the switch <see cref="Play(string)"/> makes, when its music changed (its theme, or any
    /// instrument, percussion voice or room: <see cref="MusicLibrary.SameMusic"/>); else, or when the new
    /// library hasn't that theme, it plays on as it was.
    /// </summary>
    internal void Use(MusicLibrary library, Func<string, PreparedRoom?>? preparedRooms)
    {
        var builder = new GraphBuilder(Graph, library) { PreparedRooms = preparedRooms };
        // The composer's noise now, not on the audio thread (as for the first library).
        ComposerKit.For(builder);
        var themeId = _requested;
        var restart = themeId is not null && !Library.SameMusic(library, themeId) ? library.Compiled(themeId) : null;
        if (restart is not null)
            preparedRooms?.Invoke(restart.Room);
        Library = library;
        _preparedRooms = preparedRooms;
        Dispatch(() =>
        {
            _builder = builder;
            if (restart is null)
                return;
            // Played again from its start, as a switch from the theme playing.
            Theme = null;
            Play(themeId!, restart);
        });
    }

    /// <summary><see cref="Play(string)"/> of a compiled theme (the tests' small themes), at <paramref name="at"/> (by default the quantum's start).</summary>
    internal void Play(string themeId, CompiledTheme compiled, double? at = null)
    {
        if (themeId == Theme)
            return;
        var now = at ?? Graph.CurrentTime;
        Retire(_bus, now);
        _performance = new Performance(_builder, compiled, _master);
        _bus = _performance.Output;
        _bus.Gain.SetValueAtTime(0, now);
        _bus.Gain.LinearRampToValueAtTime(1, now + 0.8);
        Theme = themeId;
        _step = 0;
        _nextNoteTime = now + 0.06;
        Advance();
    }

    /// <summary>Fades the theme playing out (music.js's stop).</summary>
    public void Stop()
    {
        _requested = null;
        Dispatch(() => StopNow());
    }

    /// <summary>The music's stop at <paramref name="at"/> (by default the quantum's start).</summary>
    internal void StopNow(double? at = null)
    {
        Retire(_bus, at ?? Graph.CurrentTime);
        _bus = null;
        _performance = null;
        Theme = null;
    }

    /// <summary>music.js's retire at <paramref name="now"/>: hold the bus where it is, ramp it to 0 over 0.55 s, disconnect it 1.8 s later.</summary>
    void Retire(GainNode? bus, double now)
    {
        if (bus == null)
            return;
        bus.Gain.CancelAndHoldAtTime(now);
        bus.Gain.LinearRampToValueAtTime(0, now + 0.55);
        _timers.Add((now + 1.8, bus.Disconnect));
    }

    /// <summary>
    /// Before each render quantum, on the thread that renders: applies the switches and stops at a time
    /// that fall in this quantum (each at its time), runs the timers that are due (at the first quantum
    /// boundary at or after their time) and schedules every step that starts within 0.2 s (music.js's
    /// schedule).
    /// </summary>
    internal void Schedule()
    {
        _schedule.ApplyDue();
        Advance();
    }

    /// <summary>Switches and stops at a time waiting for it (as the audio thread last applied the calls).</summary>
    internal int Pending => _schedule.Count;

    /// <summary>The due timers, then every step that starts within 0.2 s.</summary>
    void Advance()
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

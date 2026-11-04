using Statventure.Animation;
using Statventure.Render3D;
using Statventure.UI.Dom;

namespace Statventure.Scene;

/// <summary>An event cue delivered to game code: its name, its time on the timeline, and whether it came at a skip or was cut out of the reduced variant (Q18, Q19).</summary>
public readonly record struct SequenceCue(string Name, double Time, bool Skipped);

/// <summary>
/// A sequence playing on a scene (<see cref="SceneView.Play"/>; Docs/Scene.md, "The player", "How it
/// works", step 4). Its clock runs in real time on the document's frame clock times its
/// <see cref="Rate"/>, independent of the scene's battle pace (M10-7); not while paused, and not while
/// the document is hidden (the time it spent hidden is dropped). The timeline time τ is the clock, or
/// with the reduced variant (played when the document's media environment asks for reduced motion,
/// M10-4) the clock mapped through its cut list (Q19). The scene drives it in the frame's animation
/// timing update (Q17): the clock moves; the tracks' state at τ is applied in track order; the moments
/// passed go to their tracks (triggers set; event cues queued) in time order, within
/// <see cref="AnimationStrip.Epsilon"/> in record order; the figures it holds are moved by the change
/// in τ (their clip events fire; across a cut their clocks jump, firing none). Event cues are delivered
/// in the frame's animation events step after every figure's clip events, each as game code. At the end
/// (τ = the duration) the end state stays (Q21), <see cref="Finished"/> completes and the figures go
/// back to the scene's clock. <see cref="Skip"/> and <see cref="Finish"/> go to the end state at once
/// (every track at its end, Q3), delivering the event cues not yet delivered marked as skipped (Q18).
/// </summary>
public sealed class SequencePlayer
{
    readonly SceneView _scene;
    readonly UiDocument? _document;
    readonly SequenceSpan[] _spans;
    readonly double[] _starts;
    TrackPlayback[] _playbacks = [];
    PlaybackContext _context = null!;
    readonly List<(double Time, int Track, int Cue, TrackPlayback Playback)> _moments = [];
    readonly List<(Actor Actor, double Origin)> _held = [];
    readonly Dictionary<Actor, List<AnimationLayer>> _layers = [];
    readonly Queue<SequenceCue> _queue = [];
    readonly List<(string Name, TaskCompletionSource<SequenceCue> Source)> _waiting = [];
    readonly TaskCompletionSource<bool> _finished = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly Action<UiEvent>? _visibility;
    readonly bool _hasUi;
    int _next;
    bool _started, _force, _endPending, _delivering, _skipped;
    double _gap, _hiddenAt = double.NaN, _figures, _rate = 1;

    SequencePlayer(SceneView scene, Sequence sequence, UiDocument? document, IReadOnlyList<ModelInstance> stages)
    {
        _scene = scene;
        _document = document;
        Sequence = sequence;
        Stages = stages;
        Reduced = sequence.Reduced != null && document != null && document.Styles.Environment.ReducedMotion;
        _spans = Reduced ? [.. sequence.Reduced!] : [new SequenceSpan(0, sequence.Duration)];
        _starts = new double[_spans.Length];
        for (var i = 1; i < _spans.Length; i++)
            _starts[i] = _starts[i - 1] + _spans[i - 1].Length;
        Duration = _starts[^1] + _spans[^1].Length;
        _hasUi = sequence.Tracks.Any(t => t is UiTrack);
        if (document != null)
            _visibility = _ => VisibilityChanged();
    }

    public Sequence Sequence { get; }
    /// <summary>Whether it plays the reduced variant (chosen when it started).</summary>
    public bool Reduced { get; }
    /// <summary>Seconds the played variant takes: the sequence's duration, or the cut list's length.</summary>
    public double Duration { get; }
    /// <summary>Seconds on the played variant's clock, from 0 to <see cref="Duration"/>.</summary>
    public double Time { get; private set; }
    /// <summary>The timeline time τ: <see cref="Time"/> mapped through the cut list (the duration at the end).</summary>
    public double TimelineTime { get; private set; }
    /// <summary>Paused by the game (<see cref="Pause"/>).</summary>
    public bool Paused { get; private set; }
    /// <summary>Whether the game holds its AI while it plays (Q20).</summary>
    public bool Blocking => Sequence.Blocking;
    public bool Skippable => Sequence.Skippable;
    /// <summary>It reached its end, or was skipped or finished.</summary>
    public bool Ended { get; private set; }
    /// <summary>Completes at the end: true when it was skipped or finished, false when it played to its end.</summary>
    public Task<bool> Finished => _finished.Task;
    /// <summary>Its stages' instances, in the sequence's order (they stay after the end, Q21).</summary>
    public IReadOnlyList<ModelInstance> Stages { get; }

    /// <summary>An event cue, delivered as game code (in time order; at a skip, marked as skipped).</summary>
    public event Action<SequenceCue>? Cue;

    /// <summary>
    /// How fast its clock runs against the frame clock (1: as authored; 0 holds it), independent of the
    /// scene's battle pace (M10-7). A sequence with UI tracks plays at rate 1 only (Q29).
    /// </summary>
    public double Rate
    {
        get => _rate;
        set
        {
            if (!double.IsFinite(value) || value < 0)
                throw new ArgumentOutOfRangeException(nameof(value), value, "a sequence's rate is a finite number, 0 or more");
            if (value != 1 && _hasUi)
                throw new InvalidOperationException($"the sequence {Sequence.Name} has UI tracks, so it plays at rate 1 only");
            if (value == _rate)
                return;
            _rate = value;
            foreach (var playback in _playbacks)
                playback.RateChanged(value);
        }
    }

    /// <summary>A task that completes with the next event cue of this name delivered.</summary>
    public Task<SequenceCue> Next(string name)
    {
        var source = new TaskCompletionSource<SequenceCue>(TaskCreationOptions.RunContinuationsAsynchronously);
        _waiting.Add((name, source));
        return source.Task;
    }

    /// <summary>The layers the sequence put on top of a figure's stack (they stay after the end, Q21: the game takes them off).</summary>
    public IReadOnlyList<AnimationLayer> LayersOf(Actor actor) => _layers.TryGetValue(actor, out var layers) ? layers : [];

    /// <summary>Stops its clock (and the clocks of the figures it holds) until <see cref="Resume"/>.</summary>
    public void Pause() => SetPaused(true);

    public void Resume() => SetPaused(false);

    void SetPaused(bool paused)
    {
        if (Ended || paused == Paused)
            return;
        Paused = paused;
        foreach (var playback in _playbacks)
            playback.Paused(paused);
    }

    /// <summary>
    /// Jumps the clock to <paramref name="time"/> (seconds on the played variant's clock, kept within it)
    /// without firing anything (S4-5): the moments up to it count as passed, those after it come when
    /// the clock passes them (seeking back re-arms them); the figures' clocks jump; the state there is
    /// applied in the next frame.
    /// </summary>
    public void Seek(double time)
    {
        if (Ended)
            throw new InvalidOperationException($"the sequence {Sequence.Name} has ended");
        if (double.IsNaN(time))
            throw new ArgumentOutOfRangeException(nameof(time), time, "a seek needs a time");
        _endPending = false;
        Time = Math.Clamp(time, 0, Duration);
        TimelineTime = TimeAt(Time);
        _next = 0;
        while (_next < _moments.Count && _moments[_next].Time <= TimelineTime + AnimationStrip.Epsilon)
            _next++;
        foreach (var (actor, origin) in _held)
            actor.Animator.Seek(origin + TimelineTime);
        _figures = TimelineTime;
        _force = true;
        foreach (var playback in _playbacks)
            playback.Jumped(TimelineTime);
    }

    /// <summary>
    /// Ends the sequence in its end state now (Q3: every track at its end), delivering the event cues
    /// not yet delivered, marked as skipped (Q18); false (and nothing done) when it is unskippable or
    /// has ended (Q20).
    /// </summary>
    public bool Skip()
    {
        if (Ended || _endPending || !Sequence.Skippable)
            return false;
        EndNow();
        return true;
    }

    /// <summary>Ends any sequence, an unskippable one too, as a skip does (a battle that ends, Q20).</summary>
    public void Finish()
    {
        if (!Ended && !_endPending)
            EndNow();
    }

    // ---------- Starting ----------

    /// <summary>
    /// Checks the sequence and the bindings (S4-6: an <see cref="ArgumentException"/> naming the problem,
    /// nothing changed), then starts: the stages instanced under nodes named after them under the scene's
    /// root, the figures held, every track's playback begun. Its first frame is at its start.
    /// </summary>
    internal static SequencePlayer Start(SceneView scene, Sequence sequence, SequenceBindings bindings, UiDocument? document)
    {
        ArgumentNullException.ThrowIfNull(sequence);
        ArgumentNullException.ThrowIfNull(bindings);
        ArgumentException Problem(string message) => new($"the sequence {sequence.Name}: {message}");

        var stages = new Dictionary<string, StageBinding>();
        foreach (var (path, data, model) in bindings.Stages)
        {
            if (!sequence.Stages.Contains(path))
                throw Problem($"a stage {path} is given, which it doesn't bring");
            if (!stages.TryAdd(Sequence.StageName(path), new StageBinding(path, data, model)))
                throw Problem($"its stage {path} is given twice");
        }
        // A stage not given is Validate's problem (with the same words).
        var stageData = stages.Values.ToDictionary(s => s.Path, s => s.Data);
        sequence.Validate(stageData);

        var roles = new Dictionary<string, object>();
        var clips = new Dictionary<Actor, Func<string, ClipBinding?>>();
        var actorRoles = new Dictionary<Actor, string>();
        foreach (var (role, value, resolver) in bindings.Roles)
        {
            var definition = sequence.Roles.FirstOrDefault(r => r.Name == role) ?? throw Problem($"{role} is bound, but it has no such role");
            if (roles.ContainsKey(role))
                throw Problem($"the role {role} is bound twice");
            if (!Fits(definition.Kind, value))
                throw Problem($"the role {role} is a{(definition.Kind == RoleKind.Actor || definition.Kind == RoleKind.Element ? "n" : "")} {Kind(definition.Kind)} role, bound to a{(value is Actor or UiElement ? "n" : "")} {Describe(value)}");
            roles[role] = value;
            if (value is Actor actor)
            {
                if (!actorRoles.TryAdd(actor, role))
                    throw Problem($"the actor bound to {role} is also bound to {actorRoles[actor]}");
                clips[actor] = resolver ?? OwnClips(actor);
            }
        }
        foreach (var role in sequence.Roles)
            if (!roles.ContainsKey(role.Name))
                throw Problem($"the role {role.Name} ({Kind(role.Kind)}) isn't bound");
        foreach (var (actor, role) in actorRoles)
        {
            if (scene.HolderOf(actor) is { } other)
                throw Problem($"the actor bound to {role} is held by the sequence {other.Sequence.Name}, which is playing");
            var marked = sequence.Tracks.OfType<ActorTrack>().Any(t => t.Actor == role && t.Mark != null);
            if (!marked && actor.Node?.Scene != scene)
                throw Problem($"the actor bound to {role} isn't in the scene and stands on no mark of the sequence");
        }

        var instances = new ModelInstance[sequence.Stages.Count];
        var player = new SequencePlayer(scene, sequence, document, instances);
        var context = new PlaybackContext(player, sequence, scene, new SequenceTimes(sequence, stageData), roles, stages, clips);
        var playbacks = new TrackPlayback[sequence.Tracks.Count];
        for (var t = 0; t < playbacks.Length; t++)
        {
            context.Track = t;
            playbacks[t] = sequence.Tracks[t].CreatePlayback(context);
        }

        // Every check passed: the changes.
        for (var i = 0; i < sequence.Stages.Count; i++)
        {
            var stage = stages[Sequence.StageName(sequence.Stages[i])];
            var node = scene.Root.Add(stage.Name);
            stage.Instance = instances[i] = stage.Model is { } model ? node.Instance(model, scenery: true) : node.Instance(stage.Data);
        }
        player._context = context;
        player.Begin(actorRoles.Keys, playbacks);
        return player;
    }

    static bool Fits(RoleKind kind, object value) => kind switch
    {
        RoleKind.Actor => value is Actor,
        RoleKind.Node => value is SceneNode,
        RoleKind.Model => value is PlacedModel or ModelInstance,
        RoleKind.Element => value is UiElement,
        _ => value is Palette,
    };

    static string Kind(RoleKind kind) => kind.ToString().ToLowerInvariant();

    static string Describe(object value) => value switch
    {
        Actor => "actor",
        SceneNode => "node",
        PlacedModel or ModelInstance => "model",
        UiElement => "element",
        Palette => "palette",
        _ => value.GetType().Name,
    };

    /// <summary>A figure's own clips (its model's), bound to its stack's skeleton.</summary>
    static Func<string, ClipBinding?> OwnClips(Actor actor)
    {
        AnimationClip[]? all = null;
        return name =>
        {
            all ??= AnimationClip.AllOf(actor.Model.Data);
            return all.FirstOrDefault(c => c.Name == name) is { } clip ? new ClipBinding(clip, actor.Animator.Stack.Skeleton) : null;
        };
    }

    void Begin(IEnumerable<Actor> actors, TrackPlayback[] playbacks)
    {
        foreach (var actor in actors)
        {
            _held.Add((actor, actor.Animator.Time));
            _scene.Hold(actor, this);
        }
        _playbacks = playbacks;
        foreach (var playback in playbacks)
            playback.Begin();
        // Moments in time order; those within the epsilon of a run's first in record order (track, then cue).
        var all = new List<(double Time, int Track, int Cue, TrackPlayback Playback)>();
        for (var t = 0; t < playbacks.Length; t++)
            foreach (var (time, cue) in playbacks[t].Moments)
                all.Add((time, t, cue, playbacks[t]));
        var byTime = all.OrderBy(m => m.Time).ToList();
        for (var run = 0; run < byTime.Count;)
        {
            var end = run + 1;
            while (end < byTime.Count && byTime[end].Time - byTime[run].Time <= AnimationStrip.Epsilon)
                end++;
            _moments.AddRange(byTime.GetRange(run, end - run).OrderBy(m => m.Track).ThenBy(m => m.Cue));
            run = end;
        }
        _scene.AddPlayer(this);
        // Played while the document is hidden, its first frame (after the document is shown) is at its start anyway.
        _document?.AddEventListener("visibilitychange", _visibility!);
    }

    // ---------- Frames ----------

    internal double OriginOf(Actor actor) => _held.First(h => h.Actor == actor).Origin;

    internal void AddLayer(Actor actor, AnimationLayer layer)
    {
        if (!_layers.TryGetValue(actor, out var layers))
            _layers[actor] = layers = [];
        layers.Add(layer);
    }

    /// <summary>An event cue for the frame's events step (or a skip's delivery).</summary>
    internal void Queue(SequenceCue cue) => _queue.Enqueue(cue);

    void VisibilityChanged()
    {
        var document = _document!;
        if (Ended)
            return;
        if (document.Hidden)
            _hiddenAt = document.EventLoop.Time;
        else if (!double.IsNaN(_hiddenAt))
        {
            _gap += Math.Max(0, document.EventLoop.Time - _hiddenAt);
            _hiddenAt = double.NaN;
        }
        foreach (var playback in _playbacks)
            playback.VisibilityChanged(document.Hidden);
    }

    /// <summary>
    /// The frame's animation timing update (the scene calls it before it advances the other figures):
    /// <paramref name="seconds"/> since the scene's last frame, less the time the document was hidden,
    /// times the rate (nothing on the first frame, which is at the start).
    /// </summary>
    internal void AdvanceFrame(double seconds)
    {
        if (Ended || _endPending)
            return;
        var first = !_started;
        _started = true;
        var real = Math.Max(0, seconds - _gap);
        _gap = 0;
        var from = Time;
        var to = first || Paused ? from : Math.Min(Duration, from + real * _rate);
        if (!first && to == from && !_force)
            return;
        var tau = TimeAt(to);
        var force = first || _force;
        _force = false;
        _context.BeginFrame();
        foreach (var playback in _playbacks)
            playback.Apply(tau, force);
        while (_next < _moments.Count && _moments[_next].Time <= tau + AnimationStrip.Epsilon)
        {
            var moment = _moments[_next++];
            moment.Playback.Pass(moment.Cue, !Played(moment.Time));
        }
        MoveFigures(from, to);
        Time = to;
        TimelineTime = tau;
        if (to >= Duration)
            _endPending = true;
    }

    /// <summary>The frame's animation events step (after every figure's clip events): the event cues queued, then the end if it was reached.</summary>
    internal void DispatchFrame(UiEventLoop loop)
    {
        Deliver(loop);
        if (_endPending)
        {
            _endPending = false;
            Close(skipped: false, loop);
        }
    }

    /// <summary>τ at the played variant's clock time: within a span its start plus the time into it (held at its end), the duration at the end.</summary>
    double TimeAt(double clock)
    {
        if (clock >= Duration)
            return Sequence.Duration;
        var i = SpanOf(clock);
        var span = _spans[i];
        return span.From + Math.Clamp(clock - _starts[i], 0, span.To - span.From);
    }

    /// <summary>The span a clock time is in (at a boundary, the span it ends).</summary>
    int SpanOf(double clock)
    {
        for (var i = 0; i < _spans.Length; i++)
            if (clock <= _starts[i] + _spans[i].Length)
                return i;
        return _spans.Length - 1;
    }

    /// <summary>Whether a moment at <paramref name="time"/> lies in a span played (not cut out of the reduced variant).</summary>
    bool Played(double time) => _spans.Any(s => time >= s.From - AnimationStrip.Epsilon && time <= s.To + AnimationStrip.Epsilon);

    /// <summary>
    /// Moves the held figures' clocks with τ from the clock time <paramref name="from"/> to
    /// <paramref name="to"/>: through played time they advance (their own rate aside: their time is
    /// their start's plus τ), so their clip events fire; across a cut (and to the duration after the
    /// last span) they jump, firing none (S4-8).
    /// </summary>
    void MoveFigures(double from, double to)
    {
        var steps = new List<(bool Jump, double Value)>();
        var position = _figures;
        for (int i = SpanOf(from), last = SpanOf(to); i <= last; i++)
        {
            var span = _spans[i];
            var start = span.From + (i == SpanOf(from) ? Math.Clamp(from - _starts[i], 0, span.To - span.From) : 0);
            var end = i == last ? span.From + Math.Clamp(to - _starts[i], 0, span.To - span.From) : span.To;
            if (Math.Abs(start - position) > AnimationStrip.Epsilon)
                steps.Add((true, position = start));
            if (end > position)
            {
                steps.Add((false, end - position));
                position = end;
            }
        }
        if (to >= Duration && Sequence.Duration - position > AnimationStrip.Epsilon)
            steps.Add((true, position = Sequence.Duration));
        _figures = position;
        foreach (var (actor, origin) in _held)
            foreach (var (jump, value) in steps)
            {
                if (jump)
                    actor.Animator.Seek(origin + value);
                else
                {
                    var rate = actor.Animator.Rate;
                    actor.Animator.Rate = 1;
                    actor.Animator.Advance(value);
                    actor.Animator.Rate = rate;
                }
            }
    }

    /// <summary>A skip or a finish: the end state now, the moments left passed as skipped, then the end.</summary>
    void EndNow()
    {
        Time = Duration;
        TimelineTime = Sequence.Duration;
        _context.BeginFrame();
        foreach (var playback in _playbacks)
            playback.Apply(Sequence.Duration, force: true);
        while (_next < _moments.Count)
        {
            var moment = _moments[_next++];
            moment.Playback.Pass(moment.Cue, skipped: true);
        }
        foreach (var (actor, origin) in _held)
            actor.Animator.Seek(origin + Sequence.Duration);
        _figures = Sequence.Duration;
        Close(skipped: true, _document?.EventLoop);
    }

    /// <summary>The end: its state stays (Q21); the figures go back to the scene's clock; the cues still queued are delivered, then <see cref="Finished"/>.</summary>
    void Close(bool skipped, UiEventLoop? loop)
    {
        Ended = true;
        _skipped = skipped;
        foreach (var playback in _playbacks)
            playback.Ended(skipped);
        foreach (var (actor, _) in _held)
            _scene.Release(actor);
        _scene.RemovePlayer(this);
        if (_document is { } document)
            document.RemoveEventListener("visibilitychange", _visibility!);
        Deliver(loop);
    }

    /// <summary>Delivers the queued event cues in order, each as game code (a skip from a cue's handler adds to the queue being delivered).</summary>
    void Deliver(UiEventLoop? loop)
    {
        if (_delivering)
            return;
        _delivering = true;
        try
        {
            while (_queue.TryDequeue(out var cue))
            {
                loop?.EnterScript();
                try
                {
                    // Each handler apart: one that throws is logged, and the others and the waiting tasks still get the cue.
                    foreach (var handler in Cue?.GetInvocationList() ?? [])
                        try
                        {
                            ((Action<SequenceCue>)handler)(cue);
                        }
                        catch (Exception exception)
                        {
                            Core.Log.Error("Sequence", $"A cue handler of the sequence {Sequence.Name} failed: {exception}");
                        }
                    for (var i = 0; i < _waiting.Count; i++)
                        if (_waiting[i].Name == cue.Name)
                        {
                            _waiting[i].Source.TrySetResult(cue);
                            _waiting.RemoveAt(i--);
                        }
                }
                finally
                {
                    loop?.ExitScript();
                }
            }
        }
        finally
        {
            _delivering = false;
        }
        if (Ended)
            _finished.TrySetResult(_skipped);
    }
}

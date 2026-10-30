using Statventure.UI.Css;
using Statventure.UI.Style;

namespace Statventure.UI.Dom;

/// <summary>An animation's playback direction (Web Animations' PlaybackDirection).</summary>
public enum PlaybackDirection
{
    Normal,
    Reverse,
    Alternate,
    AlternateReverse,
}

/// <summary>Whether an animation's effect applies before and after its active interval (auto is none for element.animate).</summary>
public enum FillMode
{
    Auto,
    None,
    Forwards,
    Backwards,
    Both,
}

/// <summary>Web Animations' AnimationPlayState.</summary>
public enum UiAnimationPlayState
{
    Idle,
    Running,
    Paused,
    Finished,
}

/// <summary>
/// A keyframe for <see cref="UiElement.Animate(IReadOnlyList{Keyframe}, AnimateOptions)"/>: CSS
/// property values as text (CSS names, or camelCase as JavaScript writes them), an optional offset
/// (0..1; missing ones are spaced evenly) and the easing to the next keyframe.
/// </summary>
public sealed class Keyframe
{
    readonly List<KeyValuePair<string, string>> _values = [];

    /// <summary>A property's value (setting it again replaces it).</summary>
    public string? this[string property]
    {
        get => _values.FindLast(v => v.Key == Name(property)) is { Key: not null } found ? found.Value : null;
        set
        {
            var name = Name(property);
            _values.RemoveAll(v => v.Key == name);
            if (value is not null)
                _values.Add(new KeyValuePair<string, string>(name, value));
        }
    }

    public double? Offset { get; set; }

    /// <summary>The easing from this keyframe to the next (a CSS easing function).</summary>
    public string Easing { get; set; } = "linear";

    public IReadOnlyList<KeyValuePair<string, string>> Values => _values;

    /// <summary>A CSS property name: camelCase (backgroundColor, cssFloat) as JavaScript's keyframe keys are turned into CSS names.</summary>
    static string Name(string property)
    {
        if (property.StartsWith("--", StringComparison.Ordinal) || !property.Any(char.IsUpper))
            return property;
        if (property == "cssFloat")
            return "float";
        var name = new System.Text.StringBuilder();
        foreach (var c in property)
            if (char.IsUpper(c))
                name.Append('-').Append(char.ToLowerInvariant(c));
            else
                name.Append(c);
        return name.ToString();
    }
}

/// <summary>The timing options of element.animate (KeyframeAnimationOptions): times in milliseconds.</summary>
public sealed class AnimateOptions
{
    public double Duration { get; set; }
    public double Delay { get; set; }
    public double EndDelay { get; set; }
    public double Iterations { get; set; } = 1;
    public PlaybackDirection Direction { get; set; }
    public FillMode Fill { get; set; }
    /// <summary>The effect's easing (over each iteration; keyframes have their own).</summary>
    public string Easing { get; set; } = "linear";
}

/// <summary>An animation's finish or cancel event (AnimationPlaybackEvent): its current time and the timeline's, in ms.</summary>
public sealed class UiAnimationPlaybackEvent(string type, double? currentTime, double? timelineTime) : UiEvent(type, bubbles: false, cancelable: false)
{
    public double? CurrentTime { get; } = currentTime;
    public double? TimelineTime { get; } = timelineTime;
}

/// <summary>
/// An animation made by element.animate, as Blink 152's Animation on the document timeline: a
/// hold time or a start time (ms on the document's clock), play and pause pending until the frame
/// after which they're committed (the start time is that frame's time), the finished state updated
/// each frame (the finish notification comes in a microtask: Finished completes, then the finish
/// event is dispatched with the frame's animation events), Ready and Finished tasks that are
/// replaced when they're reset (a cancelled animation's are cancelled, as the promises are rejected
/// with an AbortError). The playback rate is always 1.
/// </summary>
public sealed class UiAnimation
{
    readonly UiDocument _document;

    internal UiAnimation(UiDocument document, UiElement target, ScriptEffect effect, long sequence)
    {
        _document = document;
        Target = target;
        Effect = effect;
        Sequence = sequence;
    }

    /// <summary>The element it animates.</summary>
    public UiElement Target { get; }

    internal ScriptEffect Effect { get; }

    /// <summary>Creation order (the composite order of script animations).</summary>
    internal long Sequence { get; }

    double? _startTime;
    double? _holdTime;
    double? _previousCurrentTime;
    bool _pendingPlay;
    bool _pendingPause;
    bool _pendingFinishNotification;
    bool _hasQueuedMicrotask;
    bool _committedFinishNotification;
    AnimationPromise? _ready;
    AnimationPromise? _finished;
    readonly EventListeners _listeners = new();

    /// <summary>Blink's time tolerance (1 µs, here in ms).</summary>
    const double TimeTolerance = 0.001;

    double TimelineTime => _document.TimelineTime;

    double EffectEnd => Effect.EndTime;

    // ---------- State ----------

    /// <summary>The start time (ms on the document's clock), null while pending or paused.</summary>
    public double? StartTime
    {
        get => _startTime;
        set => SetStartTime(value);
    }

    /// <summary>The current time (ms): the hold time, else the timeline's time since the start; null when idle.</summary>
    public double? CurrentTime
    {
        get => CurrentTimeInternal();
        set
        {
            if (value is not { } time)
            {
                if (CurrentTimeInternal() is not null)
                    throw new InvalidOperationException("currentTime may not be changed from resolved to unresolved");
                return;
            }
            SetCurrentTime(time);
        }
    }

    public UiAnimationPlayState PlayState => CalculatePlayState();

    /// <summary>A play or pause waits for the next frame.</summary>
    public bool Pending => _pendingPlay || _pendingPause;

    /// <summary>Completes (with the animation) when a pending play or pause took effect; cancelled when that was aborted.</summary>
    public Task<UiAnimation> Ready
    {
        get
        {
            var pending = Pending;
            if (_ready is null)
            {
                _ready = new AnimationPromise(this);
                if (!pending)
                    _ready.Resolve();
            }
            return _ready.Task;
        }
    }

    /// <summary>Completes (with the animation) when it finishes; cancelled when it's cancelled first.</summary>
    public Task<UiAnimation> Finished
    {
        get
        {
            if (_finished is null)
            {
                _finished = new AnimationPromise(this);
                if (CalculatePlayState() == UiAnimationPlayState.Finished && !_pendingFinishNotification)
                    _finished.Resolve();
            }
            return _finished.Task;
        }
    }

    internal double? CurrentTimeInternal() => _holdTime ?? CalculateCurrentTime();

    double? CalculateCurrentTime() => _startTime is { } start ? TimelineTime - start : null;

    bool Limited(double? currentTime) => currentTime is { } time && time >= EffectEnd - TimeTolerance;

    UiAnimationPlayState CalculatePlayState()
    {
        if (CurrentTimeInternal() is null && _startTime is null && !Pending)
            return UiAnimationPlayState.Idle;
        if (_pendingPause || (_startTime is null && !_pendingPlay))
            return UiAnimationPlayState.Paused;
        if (Limited(CurrentTimeInternal()))
            return UiAnimationPlayState.Finished;
        return UiAnimationPlayState.Running;
    }

    /// <summary>Not idle, not paused and not finished, or waiting for a frame: its values change with time.</summary>
    internal bool IsMoving => Pending || CalculatePlayState() == UiAnimationPlayState.Running;

    // ---------- Playback ----------

    /// <summary>play(): from the start when it's finished, idle or before its start.</summary>
    public void Play()
    {
        var abortedPause = _pendingPause;
        var hasPendingReadyPromise = false;
        var currentTime = CurrentTimeInternal();
        if (currentTime is not { } time || time < 0 || time >= EffectEnd)
            _holdTime = 0;
        if (_holdTime is not null)
            _startTime = null;
        if (_pendingPlay || _pendingPause)
        {
            _pendingPlay = false;
            _pendingPause = false;
            hasPendingReadyPromise = true;
        }
        if (_holdTime is null && !abortedPause)
            return;
        if (_ready is not null && !hasPendingReadyPromise)
            _ready.Reset();
        _pendingPlay = true;
        _committedFinishNotification = false;
        Changed(pending: true);
        UpdateFinishedState(seek: false, sync: false);
    }

    /// <summary>pause(): holds the current time once the next frame comes.</summary>
    public void Pause()
    {
        if (_pendingPause || CalculatePlayState() == UiAnimationPlayState.Paused)
            return;
        if (CurrentTimeInternal() is null)
            _holdTime = 0;
        if (_pendingPlay)
            _pendingPlay = false;
        else
            _ready?.Reset();
        _pendingPause = true;
        Changed(pending: true);
        UpdateFinishedState(seek: false, sync: false);
    }

    /// <summary>finish(): jumps to the end (the finish notification comes at once).</summary>
    public void Finish()
    {
        if (double.IsPositiveInfinity(EffectEnd))
            throw new InvalidOperationException("Cannot finish Animation with an infinite target effect end.");
        var end = EffectEnd;
        SetCurrentTimeInternal(end);
        _startTime ??= TimelineTime - end;
        if (_pendingPause && _startTime is not null)
        {
            _holdTime = null;
            _pendingPause = false;
            if (_ready is not null)
                ResolveMaybeAsync(_ready);
        }
        if (_pendingPlay && _startTime is not null)
        {
            _pendingPlay = false;
            if (_ready is not null)
                ResolveMaybeAsync(_ready);
        }
        Changed(pending: false);
        UpdateFinishedState(seek: true, sync: true);
    }

    /// <summary>cancel(): removes its effect; Finished and a pending Ready are cancelled, a cancel event comes with the next frame.</summary>
    public void Cancel()
    {
        if (CalculatePlayState() != UiAnimationPlayState.Idle)
        {
            if (Pending)
            {
                _pendingPlay = false;
                _pendingPause = false;
                if (_ready is not null)
                    RejectAndResetMaybeAsync(_ready);
            }
            if (_finished is not null)
            {
                if (_finished.IsPending)
                    RejectAndResetMaybeAsync(_finished);
                else
                    _finished.Reset();
            }
            if (HasListeners("cancel"))
                _document.Animations.QueueEvent(this, new UiAnimationPlaybackEvent("cancel", null, TimelineTime));
        }
        else
        {
            _pendingPlay = false;
            _pendingPause = false;
        }
        _holdTime = null;
        _startTime = null;
        Changed(pending: false);
    }

    void SetCurrentTime(double time)
    {
        SetCurrentTimeInternal(time);
        if (_pendingPause)
        {
            _holdTime = time;
            _startTime = null;
            _pendingPause = false;
            if (_ready is not null)
                ResolveMaybeAsync(_ready);
        }
        UpdateFinishedState(seek: true, sync: false);
        Changed(pending: false);
    }

    /// <summary>Silently sets the current time: the hold time while there is one (or no start time), else the start time.</summary>
    void SetCurrentTimeInternal(double time)
    {
        if (_holdTime is not null || _startTime is null)
            _holdTime = time;
        else
            _startTime = TimelineTime - time;
        _previousCurrentTime = null;
    }

    void SetStartTime(double? startTime)
    {
        var previousCurrentTime = CurrentTimeInternal();
        if (startTime is { } start && ScriptEffect.WithinEpsilon(TimelineTime / 1000, start / 1000))
            startTime = TimelineTime;
        _startTime = startTime;
        if (_startTime is not null)
            _holdTime = null;
        else
            _holdTime = previousCurrentTime;
        if (Pending)
        {
            _pendingPause = false;
            _pendingPlay = false;
            if (_ready is not null && _ready.IsPending)
                ResolveMaybeAsync(_ready);
        }
        UpdateFinishedState(seek: true, sync: false);
        Changed(pending: false);
    }

    // ---------- The frame ----------

    /// <summary>The frame's timing update (Animation::Update for an animation frame): the finished state, unless idle.</summary>
    internal void UpdateForAnimationFrame()
    {
        if (CalculatePlayState() != UiAnimationPlayState.Idle)
            UpdateFinishedState(seek: false, sync: false);
    }

    /// <summary>PendingAnimations::Update after the frame: a pending play or pause takes effect at <paramref name="readyTime"/> (ms).</summary>
    internal void NotifyReady(double readyTime)
    {
        if (_pendingPlay)
        {
            _pendingPlay = false;
            if (_holdTime is { } hold)
            {
                _startTime = readyTime - hold;
                _holdTime = null;
            }
            if (_ready is not null && _ready.IsPending)
                ResolveMaybeAsync(_ready);
            UpdateFinishedState(seek: false, sync: false);
            Changed(pending: false);
        }
        else if (_pendingPause)
        {
            _pendingPause = false;
            if (_startTime is { } start && _holdTime is null)
                _holdTime = readyTime - start;
            _startTime = null;
            if (_ready is not null && _ready.IsPending)
                ResolveMaybeAsync(_ready);
            UpdateFinishedState(seek: false, sync: false);
            Changed(pending: false);
        }
    }

    /// <summary>Web Animations' "update an animation's finished state" (Blink's UpdateFinishedState).</summary>
    void UpdateFinishedState(bool seek, bool sync)
    {
        var unconstrained = seek ? CurrentTimeInternal() : CalculateCurrentTime();
        if (unconstrained is { } current && _startTime is not null && !_pendingPlay && !_pendingPause)
        {
            if (current >= EffectEnd - TimeTolerance)
                _holdTime = seek ? current : _previousCurrentTime > EffectEnd ? _previousCurrentTime : EffectEnd;
            else
            {
                if (seek && _holdTime is { } hold)
                    _startTime = TimelineTime - hold;
                _holdTime = null;
            }
        }
        _previousCurrentTime = CurrentTimeInternal();
        if (CalculatePlayState() == UiAnimationPlayState.Finished)
        {
            if (!_committedFinishNotification)
            {
                if (sync)
                    CommitFinishNotification();
                else
                    ScheduleAsyncFinish();
            }
        }
        else
        {
            _pendingFinishNotification = false;
            _committedFinishNotification = false;
            if (_finished is { IsResolved: true })
                _finished.Reset();
        }
    }

    void ScheduleAsyncFinish()
    {
        _pendingFinishNotification = true;
        if (_hasQueuedMicrotask)
            return;
        _hasQueuedMicrotask = true;
        _document.EventLoop.QueueMicrotask(() =>
        {
            if (_pendingFinishNotification)
            {
                if (Pending)
                    NotifyReady(TimelineTime);
                CommitFinishNotification();
            }
            _hasQueuedMicrotask = false;
        });
    }

    void CommitFinishNotification()
    {
        if (_committedFinishNotification)
            return;
        _pendingFinishNotification = false;
        if (CalculatePlayState() != UiAnimationPlayState.Finished)
            return;
        if (_finished is not null && _finished.IsPending)
            ResolveMaybeAsync(_finished);
        if (HasListeners("finish"))
            _document.Animations.QueueEvent(this, new UiAnimationPlaybackEvent("finish", CurrentTimeInternal(), TimelineTime));
        _committedFinishNotification = true;
    }

    /// <summary>Its values may differ: the target is evaluated again; a pending play or pause waits for the frame's commit.</summary>
    void Changed(bool pending) => _document.Animations.ScriptAnimationChanged(this, pending);

    // ---------- Promises ----------

    /// <summary>During the frame's commit no game code may run: the promise is settled by a task.</summary>
    void ResolveMaybeAsync(AnimationPromise promise)
    {
        if (_document.Animations.Committing)
            _document.EventLoop.PostTask(promise.Resolve);
        else
            promise.Resolve();
    }

    void RejectAndResetMaybeAsync(AnimationPromise promise)
    {
        if (_document.Animations.Committing)
            _document.EventLoop.PostTask(promise.RejectAndReset);
        else
            promise.RejectAndReset();
    }

    /// <summary>
    /// A ScriptPromiseProperty: a task that completes once; resetting starts a new one for later
    /// readers (one handed out before stays as it was).
    /// </summary>
    sealed class AnimationPromise(UiAnimation owner)
    {
        TaskCompletionSource<UiAnimation> _source = New();

        static TaskCompletionSource<UiAnimation> New() => new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<UiAnimation> Task => _source.Task;
        public bool IsPending => !_source.Task.IsCompleted;
        public bool IsResolved => _source.Task.IsCompletedSuccessfully;
        public void Resolve() => _source.TrySetResult(owner);
        public void Reset() => _source = New();

        /// <summary>Rejected with an AbortError (a cancelled task), then reset.</summary>
        public void RejectAndReset()
        {
            _source.TrySetCanceled();
            Reset();
        }
    }

    // ---------- Events ----------

    /// <summary>onfinish (a listener at the place it was first set, as an event handler attribute).</summary>
    public Action<UiAnimationPlaybackEvent>? OnFinish
    {
        get => _onFinish;
        set => SetHandler("finish", ref _onFinish, value);
    }

    /// <summary>oncancel.</summary>
    public Action<UiAnimationPlaybackEvent>? OnCancel
    {
        get => _onCancel;
        set => SetHandler("cancel", ref _onCancel, value);
    }

    Action<UiAnimationPlaybackEvent>? _onFinish, _onCancel;
    readonly Dictionary<string, Action<UiEvent>> _handlerSlots = [];

    void SetHandler(string type, ref Action<UiAnimationPlaybackEvent>? field, Action<UiAnimationPlaybackEvent>? value)
    {
        field = value;
        if (value is null)
        {
            if (_handlerSlots.Remove(type, out var slot))
                _listeners.Remove(type, slot, capture: false);
            return;
        }
        if (_handlerSlots.ContainsKey(type))
            return;
        Action<UiEvent> invoke = type == "finish"
            ? e => _onFinish?.Invoke((UiAnimationPlaybackEvent)e)
            : e => _onCancel?.Invoke((UiAnimationPlaybackEvent)e);
        _handlerSlots[type] = invoke;
        _listeners.Add(type, invoke, invoke, capture: false);
    }

    /// <summary>addEventListener("finish" or "cancel", …).</summary>
    public void AddEventListener(string type, Action<UiAnimationPlaybackEvent> listener) =>
        _listeners.Add(type, listener, e => listener((UiAnimationPlaybackEvent)e), capture: false);

    public void RemoveEventListener(string type, Action<UiAnimationPlaybackEvent> listener) => _listeners.Remove(type, listener, capture: false);

    bool HasListeners(string type) => _listeners.Has(type);

    /// <summary>Dispatches one of its events (from the frame's animation event step).</summary>
    internal void Dispatch(UiEvent e)
    {
        e.CurrentTarget = this;
        e.Phase = EventPhase.AtTarget;
        e.IsTrusted = true;
        _listeners.Invoke(e, _document.EventLoop);
        e.CurrentTarget = null;
        e.Phase = EventPhase.None;
    }
}

/// <summary>
/// The keyframe effect of a script animation: its timing (Blink's Timing, normalized for the
/// document timeline) and its keyframes with their computed offsets and easings, sampled per
/// property as Blink's KeyframeEffectModel does.
/// </summary>
internal sealed class ScriptEffect
{
    public required double Delay;
    public required double EndDelay;
    public required double IterationDuration;
    public required double Iterations;
    public required PlaybackDirection Direction;
    /// <summary>Resolved (auto is none for a keyframe effect).</summary>
    public required FillMode Fill;
    public required TimingFunction Easing;
    public required List<EffectKeyframe> Keyframes;
    /// <summary>Every longhand a keyframe sets.</summary>
    public required HashSet<string> Properties;

    public double ActiveDuration => Iterations == 0 || IterationDuration == 0 ? 0 : IterationDuration * Iterations;
    public double EndTime => Math.Max(Delay + ActiveDuration + EndDelay, 0);

    internal sealed record EffectKeyframe(double Offset, TimingFunction Easing, List<PreparedDeclaration> Declarations);

    /// <summary>Parses element.animate's keyframes and options (TypeErrors as ArgumentExceptions).</summary>
    public static ScriptEffect Create(IReadOnlyList<Keyframe> keyframes, AnimateOptions options)
    {
        if (double.IsNaN(options.Duration) || options.Duration < 0)
            throw new ArgumentException("duration must be non-negative or auto");
        if (double.IsNaN(options.Iterations) || options.Iterations < 0)
            throw new ArgumentException("iterationCount must be non-negative");
        if (!double.IsFinite(options.Delay) || !double.IsFinite(options.EndDelay))
            throw new ArgumentException("delay and endDelay must be finite");
        var easing = ParseEasing(options.Easing);
        // Offsets given must be in 0..1 and in order (effect_input.cc).
        double? last = null;
        foreach (var keyframe in keyframes)
            if (keyframe.Offset is { } offset)
            {
                if (double.IsNaN(offset) || offset < 0 || offset > 1)
                    throw new ArgumentException("Offsets must be null or in the range [0,1].");
                if (last is { } previous && offset < previous)
                    throw new ArgumentException("Offsets must be monotonically non-decreasing.");
                last = offset;
            }
        var offsets = ComputedOffsets(keyframes);
        var frames = new List<EffectKeyframe>();
        var properties = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < keyframes.Count; i++)
        {
            var keyframe = keyframes[i];
            var text = new System.Text.StringBuilder();
            foreach (var (property, value) in keyframe.Values)
                text.Append(property).Append(':').Append(value).Append(';');
            // Values that don't parse are left out (Blink ignores them); animation and transition properties aren't animatable.
            var declarations = Stylesheet.Prepare(CssParser.ParseInlineStyle(text.ToString()).Where(d => !d.Important).ToList());
            declarations.RemoveAll(d => d.Property.StartsWith("animation", StringComparison.Ordinal) || d.Property.StartsWith("transition", StringComparison.Ordinal) || d.Property == "display");
            foreach (var declaration in declarations)
                properties.Add(declaration.Property);
            frames.Add(new EffectKeyframe(offsets[i], ParseEasing(keyframe.Easing), declarations));
        }
        return new ScriptEffect
        {
            Delay = options.Delay,
            EndDelay = options.EndDelay,
            IterationDuration = options.Duration,
            Iterations = options.Iterations,
            Direction = options.Direction,
            Fill = options.Fill == FillMode.Auto ? FillMode.None : options.Fill,
            Easing = easing,
            Keyframes = frames,
            Properties = properties,
        };
    }

    static TimingFunction ParseEasing(string text)
    {
        var reader = new ValueReader(CssParser.Trim(CssTokenizer.Tokenize(text)));
        if (Style.Properties.ParseTiming(reader) is { } timing && reader.AtEnd)
            return timing;
        throw new ArgumentException($"'{text}' is not a valid value for easing");
    }

    /// <summary>KeyframeEffectModelBase::GetComputedOffsets: a missing last offset is 1, a missing first 0, the others between spaced evenly.</summary>
    static double[] ComputedOffsets(IReadOnlyList<Keyframe> keyframes)
    {
        var result = new double[keyframes.Count];
        if (result.Length == 0)
            return result;
        for (var i = 0; i < result.Length; i++)
            result[i] = keyframes[i].Offset ?? double.NaN;
        if (keyframes[^1].Offset is null)
            result[^1] = 1;
        var lastIndex = 0;
        double lastOffset;
        if (keyframes[0].Offset is { } first)
            lastOffset = first;
        else if (result[0] != 1)
        {
            lastOffset = 0;
            result[0] = 0;
        }
        else
            return result;
        for (var i = lastIndex + 1; i < result.Length; i++)
        {
            var offset = result[i];
            if (double.IsNaN(offset))
                continue;
            for (var j = 1; j < i - lastIndex; j++)
                result[lastIndex + j] = lastOffset + (offset - lastOffset) * j / (i - lastIndex);
            lastIndex = i;
            lastOffset = offset;
        }
        return result;
    }

    // ---------- Timing (Timing::CalculateTimings) ----------

    enum Phase { None, Before, Active, After }

    const double TimeTolerance = 0.001;

    static bool WithinTolerance(double a, double b) => double.IsInfinity(a) || double.IsInfinity(b) ? a == b : Math.Abs(a - b) <= TimeTolerance;

    /// <summary>TimingCalculations::IsWithinAnimationTimeEpsilon (twice the double's machine epsilon).</summary>
    internal static bool WithinEpsilon(double a, double b) => Math.Abs(a - b) <= 2 * 2.220446049250313e-16;

    /// <summary>
    /// The transformed progress and the current iteration at <paramref name="localTime"/> (ms, null
    /// when idle), and whether the easing takes its left limit (before the active interval going
    /// forwards); null progress when the effect isn't in effect.
    /// </summary>
    public (double? Progress, double? Iteration, bool Before, bool InPlay, bool Current) Sample(double? localTime)
    {
        var phase = CalculatePhase(ref localTime);
        var active = ActiveDuration;
        double? activeTime = phase switch
        {
            Phase.Before when Fill is FillMode.Backwards or FillMode.Both => Math.Max(localTime!.Value - Delay, 0),
            Phase.Active => localTime!.Value - Delay,
            Phase.After when Fill is FillMode.Forwards or FillMode.Both => Math.Max(0, Math.Min(active, localTime!.Value - Delay)),
            _ => null,
        };
        var inPlay = phase == Phase.Active;
        var current = inPlay || phase == Phase.Before;
        if (activeTime is not { } time)
            return (null, null, false, inPlay, current);
        // Overall progress, simple iteration progress, current iteration.
        double overall = 0;
        if (WithinTolerance(IterationDuration, 0))
        {
            if (phase != Phase.Before)
                overall = Iterations;
        }
        else
            overall = time / IterationDuration;
        var simple = double.IsInfinity(overall) ? 0 : overall % 1.0;
        if (WithinEpsilon(simple, 0) && phase is Phase.Active or Phase.After && WithinTolerance(time, active) && !WithinEpsilon(Iterations, 0))
            simple = 1;
        double iteration;
        if (phase == Phase.After && double.IsInfinity(Iterations))
            iteration = double.PositiveInfinity;
        else if (simple == 1)
            iteration = Math.Max(0, Math.Floor(overall) - 1);
        else
            iteration = Math.Floor(overall);
        var even = double.IsInfinity(iteration) || WithinEpsilon(iteration % 2, 0);
        var forwards = Direction switch
        {
            PlaybackDirection.Normal => true,
            PlaybackDirection.Reverse => false,
            PlaybackDirection.Alternate => even,
            _ => !even,
        };
        var directed = forwards ? simple : 1 - simple;
        var before = forwards ? phase == Phase.Before : phase == Phase.After;
        if (phase == Phase.After)
        {
            if (forwards && WithinEpsilon(directed, 1))
                directed = 1;
            else if (!forwards && WithinEpsilon(directed, 0))
                directed = 0;
        }
        return (Easing.Evaluate(directed, before), iteration, before, inPlay, current);
    }

    /// <summary>CalculatePhase going forwards (playback rate 1): before the delay, active, after.</summary>
    Phase CalculatePhase(ref double? localTime)
    {
        if (localTime is not { } local)
            return Phase.None;
        var end = EndTime;
        var beforeBoundary = Math.Max(Math.Min(Delay, end), 0);
        if (WithinTolerance(local, beforeBoundary))
            localTime = local = beforeBoundary;
        if (local < beforeBoundary)
            return Phase.Before;
        var afterBoundary = Math.Max(Math.Min(Delay + ActiveDuration, end), 0);
        if (WithinTolerance(local, afterBoundary))
            localTime = local = afterBoundary;
        if (local > afterBoundary)
            return Phase.After;
        // Going forwards, the end of the active interval is already after it.
        return local == afterBoundary ? Phase.After : Phase.Active;
    }
}

public partial class UiElement
{
    /// <summary>
    /// element.animate: a new script animation of this element with <paramref name="keyframes"/>
    /// and <paramref name="options"/>, playing (its start waits for the next frame). Invalid
    /// options, easings or offsets throw an ArgumentException (JavaScript's TypeError).
    /// </summary>
    public UiAnimation Animate(IReadOnlyList<Keyframe> keyframes, AnimateOptions? options = null)
    {
        var document = Document ?? throw new InvalidOperationException("element.animate needs an element in a document.");
        var effect = ScriptEffect.Create(keyframes, options ?? new AnimateOptions());
        var animation = document.Animations.CreateScriptAnimation(document, this, effect);
        // Its effect applies from the element's next style update.
        document.InvalidateStyle(this, subtree: false);
        animation.Play();
        return animation;
    }

    /// <summary>getComputedStyle: the element's style with styles (and animated values) brought up to date.</summary>
    public ComputedStyle? GetComputedStyle()
    {
        Document?.UpdateLayout();
        return Style;
    }

    /// <summary>element.animate(keyframes, duration).</summary>
    public UiAnimation Animate(IReadOnlyList<Keyframe> keyframes, double duration) => Animate(keyframes, new AnimateOptions { Duration = duration });
}

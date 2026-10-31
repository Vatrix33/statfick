using Statventure.Render3D;
using Statventure.UI.Dom;

namespace Statventure.Animation;

/// <summary>
/// Plays an animation stack (and the state machine on it, if any) on a clock: <see cref="Advance"/>
/// moves it on (times its <see cref="Rate"/>), takes the machine's transitions due and queues the
/// clip events its strips pass and the machine's state notices; <see cref="DispatchEvents"/>
/// delivers them in time order (at one moment: states left, states entered, then clip events) to
/// <see cref="StateChanged"/> and <see cref="Event"/> and to the tasks from <see cref="Entered"/>,
/// <see cref="Left"/> and <see cref="Next"/>. Added to a document
/// (<see cref="UiDocument.AddFrameClient"/>), it's advanced in the frame's animation timing update
/// and its events are dispatched right after the frame's CSS animation events, each delivery as game
/// code whose microtasks (awaits) run after it (Docs/Animation.md, "Clip events").
/// <see cref="Seek"/> jumps without firing anything.
/// </summary>
public sealed class Animator(AnimationStack stack, StateMachine? machine = null) : IUiFrameClient
{
    readonly List<(double Time, int Order, ClipEvent? Clip, StateNotice? Notice)> _queued = [];
    readonly List<(string Name, TaskCompletionSource<ClipEvent> Source)> _waiting = [];
    readonly List<(string Layer, string State, bool Entered, TaskCompletionSource<StateNotice> Source)> _waitingStates = [];
    readonly List<ClipEvent> _clipScratch = [];
    readonly List<StateNotice> _noticeScratch = [];
    readonly PoseBuffer _buffer = new(stack.Skeleton);

    public AnimationStack Stack { get; } = stack;
    public StateMachine? Machine { get; } = machine;
    /// <summary>Seconds on the stack's clock.</summary>
    public double Time { get; private set; }
    /// <summary>How fast the clock runs against real time (1: as authored); 0 holds it.</summary>
    public double Rate { get; set; } = 1;

    /// <summary>A clip event, delivered in time order by <see cref="DispatchEvents"/>.</summary>
    public event Action<ClipEvent>? Event;

    /// <summary>A state machine layer's state entered or left, delivered in time order with the clip events.</summary>
    public event Action<StateNotice>? StateChanged;

    /// <summary>
    /// Moves the clock on by <paramref name="seconds"/> of real time (times the rate): the machine's
    /// transitions due, then the clip events passed, then the states its fades left behind.
    /// </summary>
    public void Advance(double seconds)
    {
        var from = Time;
        var to = Time + seconds * Rate;
        Time = to;
        if (!(to > from))
            return;
        _clipScratch.Clear();
        _noticeScratch.Clear();
        Machine?.Update(from, to, _noticeScratch);
        Stack.CollectEvents(from, to, _clipScratch);
        Machine?.Prune(to, _noticeScratch);
        var start = _queued.Count;
        foreach (var n in _noticeScratch)
            _queued.Add((n.Time, n.Entered ? 1 : 0, null, n));
        foreach (var e in _clipScratch)
            _queued.Add((e.Time, 2, e, null));
        // In time order; at one moment (within the epsilon of a run's first) states left, states
        // entered, then clip events, each in the order found.
        var added = _queued.GetRange(start, _queued.Count - start);
        _queued.RemoveRange(start, added.Count);
        var byTime = added.Select((item, index) => (item, index)).OrderBy(x => x.item.Time).ThenBy(x => x.index).Select(x => x.item).ToList();
        for (var run = 0; run < byTime.Count;)
        {
            var end = run + 1;
            while (end < byTime.Count && byTime[end].Time - byTime[run].Time <= AnimationStrip.Epsilon)
                end++;
            _queued.AddRange(byTime.GetRange(run, end - run).Select((item, index) => (item, index)).OrderBy(x => x.item.Order).ThenBy(x => x.index).Select(x => x.item));
            run = end;
        }
    }

    /// <summary>Sets the clock (a jump: seeking, restarting) without firing the events between.</summary>
    public void Seek(double time) => Time = time;

    /// <summary>A task that completes with the next dispatched event of this name.</summary>
    public Task<ClipEvent> Next(string name)
    {
        var source = new TaskCompletionSource<ClipEvent>(TaskCreationOptions.RunContinuationsAsynchronously);
        _waiting.Add((name, source));
        return source.Task;
    }

    /// <summary>A task that completes when the layer next enters this state.</summary>
    public Task<StateNotice> Entered(string layer, string state) => WaitForState(layer, state, true);

    /// <summary>A task that completes when the layer next leaves this state (its clip faded out).</summary>
    public Task<StateNotice> Left(string layer, string state) => WaitForState(layer, state, false);

    Task<StateNotice> WaitForState(string layer, string state, bool entered)
    {
        var source = new TaskCompletionSource<StateNotice>(TaskCreationOptions.RunContinuationsAsynchronously);
        _waitingStates.Add((layer, state, entered, source));
        return source.Task;
    }

    /// <summary>Delivers the queued events (outside a document: each handler and task in turn).</summary>
    public void DispatchEvents() => Dispatch(null);

    /// <summary>The model's pose at the clock's time.</summary>
    public ModelPose Pose(ModelPose? into = null)
    {
        Stack.Evaluate(Time, _buffer);
        return _buffer.ToPose(into);
    }

    void IUiFrameClient.AdvanceFrame(double seconds) => Advance(seconds);

    void IUiFrameClient.DispatchFrameEvents(UiEventLoop loop) => Dispatch(loop);

    void Dispatch(UiEventLoop? loop)
    {
        if (_queued.Count == 0)
            return;
        var due = _queued.ToArray();
        _queued.Clear();
        foreach (var item in due)
        {
            loop?.EnterScript();
            try
            {
                if (item.Clip is { } e)
                {
                    Event?.Invoke(e);
                    for (var i = 0; i < _waiting.Count; i++)
                        if (_waiting[i].Name == e.Name)
                        {
                            _waiting[i].Source.TrySetResult(e);
                            _waiting.RemoveAt(i--);
                        }
                }
                else if (item.Notice is { } n)
                {
                    StateChanged?.Invoke(n);
                    for (var i = 0; i < _waitingStates.Count; i++)
                        if (_waitingStates[i].Layer == n.Layer && _waitingStates[i].State == n.State && _waitingStates[i].Entered == n.Entered)
                        {
                            _waitingStates[i].Source.TrySetResult(n);
                            _waitingStates.RemoveAt(i--);
                        }
                }
            }
            catch (Exception exception)
            {
                Core.Log.Error("Animation", $"A clip event handler failed: {exception}");
            }
            finally
            {
                loop?.ExitScript();
            }
        }
    }
}

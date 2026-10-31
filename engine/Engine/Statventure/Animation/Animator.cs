using Statventure.Render3D;
using Statventure.UI.Dom;

namespace Statventure.Animation;

/// <summary>
/// Plays an animation stack on a clock: <see cref="Advance"/> moves it on (times its
/// <see cref="Rate"/>) and queues the clip events its strips pass, <see cref="DispatchEvents"/>
/// delivers them in time order to <see cref="Event"/> and to tasks from <see cref="Next"/>.
/// Added to a document (<see cref="UiDocument.AddFrameClient"/>), it's advanced in the frame's
/// animation timing update and its events are dispatched right after the frame's CSS animation
/// events, each delivery as game code whose microtasks (awaits) run after it (Docs/Animation.md,
/// "Clip events"). <see cref="Seek"/> jumps without firing anything.
/// </summary>
public sealed class Animator(AnimationStack stack) : IUiFrameClient
{
    readonly List<ClipEvent> _queued = [];
    readonly List<(string Name, TaskCompletionSource<ClipEvent> Source)> _waiting = [];
    readonly PoseBuffer _buffer = new(stack.Skeleton);

    public AnimationStack Stack { get; } = stack;
    /// <summary>Seconds on the stack's clock.</summary>
    public double Time { get; private set; }
    /// <summary>How fast the clock runs against real time (1: as authored); 0 holds it.</summary>
    public double Rate { get; set; } = 1;

    /// <summary>A clip event, delivered in time order by <see cref="DispatchEvents"/>.</summary>
    public event Action<ClipEvent>? Event;

    /// <summary>Moves the clock on by <paramref name="seconds"/> of real time (times the rate), queuing the events passed.</summary>
    public void Advance(double seconds)
    {
        var to = Time + seconds * Rate;
        if (to > Time)
            Stack.CollectEvents(Time, to, _queued);
        Time = to;
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
        foreach (var e in due)
        {
            loop?.EnterScript();
            try
            {
                Event?.Invoke(e);
                for (var i = 0; i < _waiting.Count; i++)
                    if (_waiting[i].Name == e.Name)
                    {
                        _waiting[i].Source.TrySetResult(e);
                        _waiting.RemoveAt(i--);
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

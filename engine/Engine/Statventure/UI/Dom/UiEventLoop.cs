using System.Diagnostics;
using Statventure.Core;

namespace Statventure.UI.Dom;

/// <summary>
/// A document's script event loop, as Chromium 152 runs HTML's: timer tasks (setTimeout and
/// setInterval with Blink's DOMTimer nesting clamp), a microtask queue that runs at every
/// checkpoint (after each task, after each callback the engine calls when no other callback is
/// running, after the animation timing update), and C# async/await on top: while game code runs
/// the loop is the thread's SynchronizationContext, so continuations are microtasks, as a
/// promise's reactions are. The document drives it from its frames: timers due before a frame
/// run before the frame's steps, each at its own time. While the document is hidden its timers are
/// throttled as Blink's scheduler throttles a hidden page's (<see cref="UiTimerThrottling"/>).
/// </summary>
public sealed class UiEventLoop : SynchronizationContext
{
    readonly int _thread = Environment.CurrentManagedThreadId;

    internal UiEventLoop()
    {
    }

    /// <summary>Seconds on the documents' clock: the time of the task or frame that runs now.</summary>
    public double Time { get; private set; }

    /// <summary>performance.now(): milliseconds since the clock's origin.</summary>
    public double Now => Time * 1000;

    // ---------- Timers ----------

    sealed class Timer
    {
        public required int Id;
        public required Action Callback;
        public required double Due;
        public double? Interval;
        public int Nesting;
        public long Sequence;
        public TimerKind Kind;
    }

    // Blink's DOMTimer: counted from 1, clamped above 6.
    const int MaxNestingLevel = 6;
    const double MinimumIntervalMs = 4;

    readonly Dictionary<int, Timer> _timers = [];
    readonly PriorityQueue<Timer, (double, long)> _due = new();
    int _lastTimerId;
    long _sequence;
    int _nestingLevel;

    /// <summary>setTimeout: <paramref name="callback"/> runs as a task once <paramref name="milliseconds"/> have passed; returns its id.</summary>
    public int SetTimeout(Action callback, double milliseconds = 0) => Install(callback, milliseconds, repeat: false);

    /// <summary>setInterval: <paramref name="callback"/> runs every <paramref name="milliseconds"/> until cleared; returns its id.</summary>
    public int SetInterval(Action callback, double milliseconds = 0) => Install(callback, milliseconds, repeat: true);

    /// <summary>clearTimeout / clearInterval (they share their ids).</summary>
    public void ClearTimeout(int id) => _timers.Remove(id);

    public void ClearInterval(int id) => _timers.Remove(id);

    /// <summary>Stops every timer (DOMTimer::ContextDestroyed: the document unloaded).</summary>
    internal void ClearTimers()
    {
        _timers.Clear();
        _due.Clear();
    }

    /// <summary>A task that completes after <paramref name="milliseconds"/> (a timer resolving a promise).</summary>
    public Task Delay(double milliseconds)
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        SetTimeout(() => completion.SetResult(), milliseconds);
        return completion.Task;
    }

    int Install(Action callback, double milliseconds, bool repeat)
    {
        if (double.IsNaN(milliseconds) || milliseconds < 0)
            milliseconds = 0;
        // DOMTimer's constructor: the nesting level is the running timer's plus one; deeper than 6, at least 4 ms.
        var nesting = _nestingLevel + 1;
        if (nesting > MaxNestingLevel && milliseconds < MinimumIntervalMs)
            milliseconds = MinimumIntervalMs;
        var id = _lastTimerId = _lastTimerId == int.MaxValue ? 1 : _lastTimerId + 1;
        var timer = new Timer
        {
            Id = id,
            Callback = callback,
            Due = Time + milliseconds / 1000,
            Interval = repeat ? milliseconds : null,
            Nesting = nesting,
            Sequence = _sequence++,
            // The scheduler's task type (DOMTimer): deeper than 6 high nesting, else a 0 ms timer immediate.
            Kind = nesting > MaxNestingLevel ? TimerKind.HighNesting : milliseconds == 0 ? TimerKind.Immediate : TimerKind.LowNesting,
        };
        _timers[id] = timer;
        _due.Enqueue(timer, (timer.Due, timer.Sequence));
        return id;
    }

    /// <summary>The time the next timer is due (seconds), if any.</summary>
    public double? NextTimerDue
    {
        get
        {
            // Entries of cleared timers, and old entries of intervals that ran while throttled, are stale.
            while (_due.TryPeek(out var timer, out var priority))
            {
                if (_timers.TryGetValue(timer.Id, out var live) && live == timer && priority == (timer.Due, timer.Sequence))
                    return timer.Due;
                _due.Dequeue();
            }
            return null;
        }
    }

    /// <summary>
    /// Runs the tasks due by <paramref name="time"/> in order (timers in due order, each at its own
    /// time, then posts from other threads), each followed by a microtask checkpoint, and moves the
    /// clock to <paramref name="time"/>. While the document is hidden, timers run when Blink's
    /// throttling lets them (each at that time), and the throttling's own changes happen at theirs.
    /// </summary>
    public void RunUntil(double time)
    {
        if (!Throttling.PageHidden)
        {
            while (NextTimerDue is { } due && due <= time)
            {
                var timer = _due.Dequeue();
                Time = Math.Max(Time, timer.Due);
                RunTimer(timer);
            }
        }
        else
            RunThrottledUntil(time);
        Time = Math.Max(Time, time);
        Throttling.Update(UiTimerThrottling.Ticks(Time));
        RunPosted();
    }

    void RunThrottledUntil(double time)
    {
        var until = UiTimerThrottling.Ticks(time);
        while (Throttling.PageHidden)
        {
            var now = UiTimerThrottling.Ticks(Time);
            Throttling.Update(now);
            var next = NextThrottledTimer(now, out var runAt);
            // The scheduler's control tasks (CPU throttling, intensive throttling, the end of recent audio) run at their times first.
            if (Throttling.NextStateChange(now) is { } change && change <= until && (next is null || change <= UiTimerThrottling.Ticks(runAt)))
            {
                Time = Math.Max(Time, UiTimerThrottling.Seconds(change));
                continue;
            }
            if (next is null || runAt > time)
                break;
            Time = Math.Max(Time, runAt);
            RunTimer(next);
        }
        // Shown again within this call: what's left runs unthrottled.
        if (!Throttling.PageHidden)
            while (NextTimerDue is { } due && due <= time)
            {
                var timer = _due.Dequeue();
                Time = Math.Max(Time, timer.Due);
                RunTimer(timer);
            }
    }

    /// <summary>The live timer that may run first from <paramref name="now"/> (by its allowed run time, due time and order), and that time.</summary>
    Timer? NextThrottledTimer(long now, out double runAt)
    {
        Timer? best = null;
        runAt = double.PositiveInfinity;
        foreach (var timer in _timers.Values)
        {
            var at = Throttling.Throttles(timer.Kind, now)
                ? UiTimerThrottling.Seconds(Throttling.AllowedRunTime(timer.Kind, UiTimerThrottling.Ticks(timer.Due), now))
                : timer.Due;
            if (best is null || at < runAt || (at == runAt && (timer.Due < best.Due || (timer.Due == best.Due && timer.Sequence < best.Sequence))))
            {
                best = timer;
                runAt = at;
            }
        }
        return best;
    }

    /// <summary>
    /// When the loop next has work while the document is hidden (seconds): a timer allowed to run,
    /// or a change of the throttling; null when nothing is pending (a host sleeps until then).
    /// </summary>
    public double? NextWakeUp
    {
        get
        {
            if (!Throttling.PageHidden)
                return NextTimerDue;
            var now = UiTimerThrottling.Ticks(Time);
            Throttling.Update(now);
            NextThrottledTimer(now, out var runAt);
            double? next = double.IsPositiveInfinity(runAt) ? null : runAt;
            if (Throttling.NextStateChange(now) is { } change && (next is null || UiTimerThrottling.Seconds(change) < next))
                next = UiTimerThrottling.Seconds(change);
            return next;
        }
    }

    void RunTimer(Timer timer)
    {
        // (A throttled timer runs without leaving the due queue: its entry there is stale from now on.)
        var kind = timer.Kind;
        if (timer.Interval is { } interval)
        {
            if (Time <= timer.Due)
                timer.Due += interval / 1000;
            else
            {
                // TimerBase::RunInternal after a late wake-up: the beat after now + interval / 20, no catching up.
                var period = UiTimerThrottling.Ticks(interval / 1000);
                timer.Due = period == 0 ? Time : UiTimerThrottling.Seconds(UiTimerThrottling.SnappedToNextTick(
                    UiTimerThrottling.Ticks(Time) + period / 20, UiTimerThrottling.Ticks(timer.Due), period));
            }
            // DOMTimer::Fired for an interval: one level deeper each time; at level 7 at least 4 ms and high nesting.
            timer.Nesting++;
            if (timer.Nesting == MaxNestingLevel + 1)
            {
                if (interval < MinimumIntervalMs)
                {
                    timer.Due += (MinimumIntervalMs - interval) / 1000;
                    timer.Interval = MinimumIntervalMs;
                }
                timer.Kind = TimerKind.HighNesting;
            }
            timer.Sequence = _sequence++;
            _due.Enqueue(timer, (timer.Due, timer.Sequence));
        }
        else
            _timers.Remove(timer.Id);
        var start = UiTimerThrottling.Ticks(Time);
        Throttling.OnRun(kind, start);
        _nestingLevel = timer.Nesting;
        var began = Stopwatch.GetTimestamp();
        try
        {
            RunTask(timer.Callback);
        }
        finally
        {
            _nestingLevel = 0;
        }
        var cost = TaskCost?.Invoke() ?? Stopwatch.GetElapsedTime(began).TotalSeconds;
        Throttling.Ran(kind, start, start + UiTimerThrottling.Ticks(cost));
    }

    // ---------- Hidden documents ----------

    /// <summary>Blink's throttling of a hidden page's timers (the document sets its state).</summary>
    internal UiTimerThrottling Throttling { get; } = new();

    /// <summary>
    /// What a timer task cost (seconds), for the CPU-time budget; by default its measured run time.
    /// Tests give fixed costs.
    /// </summary>
    internal Func<double>? TaskCost { get; set; }

    /// <summary>Called when another thread posts a task (a host sleeping while hidden wakes up for it).</summary>
    internal Action? Wake { get; set; }

    /// <summary>
    /// Where throttled wake-ups are aligned, in seconds on the loop's clock (Blink aligns them to the
    /// system's monotonic clock, so the phase is arbitrary to a page; tests take a reference's).
    /// </summary>
    public double WakeUpAlignment
    {
        get => UiTimerThrottling.Seconds(Throttling.Origin);
        set => Throttling.Origin = UiTimerThrottling.Ticks(value);
    }

    // ---------- Tasks and microtasks ----------

    readonly Queue<Action> _microtasks = new();
    readonly Queue<Action> _posted = new();
    int _depth;
    bool _checkingPoint;

    /// <summary>Runs <paramref name="task"/> as a task: game code, then a microtask checkpoint.</summary>
    public void RunTask(Action task)
    {
        EnterScript();
        try
        {
            task();
        }
        catch (Exception exception)
        {
            Log.Error("UI", $"A script task failed: {exception}");
        }
        finally
        {
            ExitScript();
        }
    }

    /// <summary>queueMicrotask.</summary>
    public void QueueMicrotask(Action microtask) => _microtasks.Enqueue(microtask);

    /// <summary>A task for the loop's next turn (before the next frame).</summary>
    internal void PostTask(Action task)
    {
        lock (_posted)
            _posted.Enqueue(task);
        if (Environment.CurrentManagedThreadId != _thread)
            Wake?.Invoke();
    }

    SynchronizationContext? _outerContext;

    /// <summary>
    /// The engine is about to call game code. When no other game code runs, the loop becomes the
    /// thread's SynchronizationContext for it.
    /// </summary>
    internal void EnterScript()
    {
        if (_depth++ == 0)
        {
            _outerContext = SynchronizationContext.Current;
            SetSynchronizationContext(this);
        }
    }

    /// <summary>Game code the engine called returned: when it was the outermost, the microtasks run.</summary>
    internal void ExitScript()
    {
        if (_depth == 1)
            PerformMicrotaskCheckpoint();
        if (--_depth == 0)
            SetSynchronizationContext(_outerContext);
    }

    /// <summary>Whether game code is running (a callback the engine called, or a task).</summary>
    public bool InScript => _depth > 0;

    /// <summary>HTML's microtask checkpoint: runs microtasks until none are left (those they queue too).</summary>
    public void PerformMicrotaskCheckpoint()
    {
        if (_checkingPoint)
            return;
        _checkingPoint = true;
        var outermost = _depth == 0;
        if (outermost)
        {
            _outerContext = SynchronizationContext.Current;
            SetSynchronizationContext(this);
        }
        _depth++;
        try
        {
            while (_microtasks.TryDequeue(out var microtask))
            {
                try
                {
                    microtask();
                }
                catch (Exception exception)
                {
                    Log.Error("UI", $"A microtask failed: {exception}");
                }
            }
        }
        finally
        {
            _depth--;
            if (outermost)
                SetSynchronizationContext(_outerContext);
            _checkingPoint = false;
        }
    }

    void RunPosted()
    {
        while (true)
        {
            Action? task;
            lock (_posted)
                if (!_posted.TryDequeue(out task))
                    return;
            RunTask(task);
        }
    }

    // ---------- SynchronizationContext ----------

    /// <summary>A continuation: on the loop's thread a microtask; from another thread a task for the loop's next turn.</summary>
    public override void Post(SendOrPostCallback d, object? state)
    {
        if (Environment.CurrentManagedThreadId == _thread)
            QueueMicrotask(() => d(state));
        else
        {
            lock (_posted)
                _posted.Enqueue(() => d(state));
            Wake?.Invoke();
        }
    }

    public override void Send(SendOrPostCallback d, object? state)
    {
        if (Environment.CurrentManagedThreadId != _thread)
            throw new InvalidOperationException("The UI event loop can't run synchronous work for another thread.");
        d(state);
    }

    public override SynchronizationContext CreateCopy() => this;
}

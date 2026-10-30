using Statventure.Core;

namespace Statventure.UI.Dom;

/// <summary>
/// A document's script event loop, as Chromium 152 runs HTML's: timer tasks (setTimeout and
/// setInterval with Blink's DOMTimer nesting clamp), a microtask queue that runs at every
/// checkpoint (after each task, after each callback the engine calls when no other callback is
/// running, after the animation timing update), and C# async/await on top: while game code runs
/// the loop is the thread's SynchronizationContext, so continuations are microtasks, as a
/// promise's reactions are. The document drives it from its frames: timers due before a frame
/// run before the frame's steps, each at its own time.
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
            while (_due.TryPeek(out var timer, out _))
            {
                if (_timers.TryGetValue(timer.Id, out var live) && live == timer)
                    return timer.Due;
                _due.Dequeue();
            }
            return null;
        }
    }

    /// <summary>
    /// Runs the tasks due by <paramref name="time"/> in order (timers in due order, each at its own
    /// time, then posts from other threads), each followed by a microtask checkpoint, and moves the
    /// clock to <paramref name="time"/>.
    /// </summary>
    public void RunUntil(double time)
    {
        while (NextTimerDue is { } due && due <= time)
        {
            var timer = _due.Dequeue();
            Time = Math.Max(Time, timer.Due);
            RunTimer(timer);
        }
        Time = Math.Max(Time, time);
        RunPosted();
    }

    void RunTimer(Timer timer)
    {
        if (timer.Interval is { } interval)
        {
            // DOMTimer::Fired for an interval: one level deeper each time; at level 7 at least 4 ms.
            timer.Nesting++;
            if (timer.Nesting == MaxNestingLevel + 1 && interval < MinimumIntervalMs)
                timer.Interval = interval = MinimumIntervalMs;
            timer.Due += interval / 1000;
            timer.Sequence = _sequence++;
            _due.Enqueue(timer, (timer.Due, timer.Sequence));
        }
        else
            _timers.Remove(timer.Id);
        _nestingLevel = timer.Nesting;
        try
        {
            RunTask(timer.Callback);
        }
        finally
        {
            _nestingLevel = 0;
        }
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
            lock (_posted)
                _posted.Enqueue(() => d(state));
    }

    public override void Send(SendOrPostCallback d, object? state)
    {
        if (Environment.CurrentManagedThreadId != _thread)
            throw new InvalidOperationException("The UI event loop can't run synchronous work for another thread.");
        d(state);
    }

    public override SynchronizationContext CreateCopy() => this;
}

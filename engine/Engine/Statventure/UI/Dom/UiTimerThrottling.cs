namespace Statventure.UI.Dom;

/// <summary>
/// How Chromium 152 runs a hidden page's timers (Blink's scheduler: page_scheduler_impl.cc,
/// frame_scheduler_impl.cc, task_queue_throttler.cc, wake_up_budget_pool.cc,
/// cpu_time_budget_pool.cc; Docs/Scripting.md, "Hidden documents"). Times are microseconds on the
/// event loop's clock (Blink's TimeTicks), aligned to <see cref="Origin"/>.
/// </summary>
/// <remarks>
/// A timer's task type follows its nesting level (DOMTimer): deeper than 6, "high nesting"
/// (intensively throttleable); else a 0 ms timeout is "immediate" (never throttled); else "low
/// nesting". While the page is hidden and silent (not audible, and not audible in the last 30 s),
/// low-nesting timers wake at most once a second (the hidden wake-up pool), high-nesting ones once
/// a second too until the grace period after hiding is over (60 s once the page had its first
/// contentful paint, 5 minutes before), then once a minute (the intensive pool, which still aligns
/// to a second when it hasn't woken for a minute). A wake-up lets the tasks due within 3 ms run.
/// From 10 s after hiding, throttled timer tasks also spend a CPU-time budget (1 s to start with,
/// refilled at 1% of the time, at most 3 s) and wait while it is below zero.
/// </remarks>
internal sealed class UiTimerThrottling
{
    internal const long Second = 1_000_000;
    // PageSchedulerImpl's constants.
    internal const long DefaultWakeUpInterval = Second;
    internal const long IntensiveWakeUpInterval = 60 * Second;
    internal const long WakeUpDuration = 3_000;
    internal const long CpuThrottlingDelay = 10 * Second;
    internal const long RecentAudioDelay = 30 * Second;
    // GetIntensiveWakeUpThrottlingGracePeriod: 5 minutes while loading, 1 minute once loaded.
    internal const long GracePeriodLoading = 300 * Second;
    internal const long GracePeriodLoaded = 60 * Second;

    /// <summary>Where wake-ups are aligned (Blink aligns to the monotonic clock's zero, arbitrary to a page).</summary>
    public long Origin;

    public readonly WakeUpBudgetPool Hidden;
    public readonly WakeUpBudgetPool Intensive;
    public readonly CpuTimeBudgetPool Cpu;

    bool _hidden;
    long _hiddenSince;
    bool _loadingWhenHidden;
    bool _audible;
    long? _silentSince;

    public UiTimerThrottling()
    {
        Hidden = new WakeUpBudgetPool(this);
        Intensive = new WakeUpBudgetPool(this) { LowerAlignmentIfNoRecentWakeUp = DefaultWakeUpInterval };
        Cpu = new CpuTimeBudgetPool(0);
    }

    public bool PageHidden => _hidden;

    /// <summary>The page became hidden or visible at <paramref name="now"/> (PageSchedulerImpl::SetPageVisible).</summary>
    public void SetHidden(bool hidden, long now, bool loading)
    {
        if (hidden == _hidden)
            return;
        _hidden = hidden;
        _hiddenSince = now;
        _loadingWhenHidden = loading;
    }

    /// <summary>The page's audio state (PageSchedulerImpl::AudioStateChanged): silent pages count as audible for 30 s more.</summary>
    public void SetAudible(bool audible, long now)
    {
        if (audible)
        {
            _audible = true;
            _silentSince = null;
        }
        else if (_audible)
        {
            _audible = false;
            _silentSince = now;
        }
    }

    bool AudioPlaying(long now) => _audible || (_silentSince is { } silent && now < silent + RecentAudioDelay);

    /// <summary>Whether throttleable timer queues are throttled now (ComputeThrottlingType: hidden and not playing audio).</summary>
    public bool Throttled(long now) => _hidden && !AudioPlaying(now);

    /// <summary>Whether a timer of <paramref name="kind"/> is throttled now (immediate timers never are: their queue isn't throttleable).</summary>
    public bool Throttles(TimerKind kind, long now) => kind != TimerKind.Immediate && Throttled(now);

    /// <summary>The intensive pool's interval now (GetIntensiveWakeUpThrottlingInterval).</summary>
    long IntensiveInterval(long now) =>
        _hidden && now >= _hiddenSince + (_loadingWhenHidden ? GracePeriodLoading : GracePeriodLoaded) ? IntensiveWakeUpInterval : DefaultWakeUpInterval;

    internal long IntervalOf(WakeUpBudgetPool pool, long now) => pool == Intensive ? IntensiveInterval(now) : DefaultWakeUpInterval;

    /// <summary>The CPU-time budget pool is enabled 10 s after the page was hidden (DoThrottleCPUTime).</summary>
    bool CpuThrottled(long now) => _hidden && now >= _hiddenSince + CpuThrottlingDelay;

    /// <summary>The next time after <paramref name="now"/> at which the throttling's state changes by itself (a scheduler control task), if any.</summary>
    public long? NextStateChange(long now)
    {
        long? next = null;
        void Consider(long at)
        {
            if (at > now && (next is null || at < next))
                next = at;
        }
        if (_hidden)
        {
            Consider(_hiddenSince + CpuThrottlingDelay);
            Consider(_hiddenSince + (_loadingWhenHidden ? GracePeriodLoading : GracePeriodLoaded));
        }
        if (_silentSince is { } silent)
            Consider(silent + RecentAudioDelay);
        return next;
    }

    /// <summary>Brings the pools' enabled states up to <paramref name="now"/> (the control tasks that run at their times).</summary>
    public void Update(long now) => Cpu.SetEnabled(CpuThrottled(now));

    /// <summary>
    /// When a throttled timer (<see cref="Throttles"/>) of <paramref name="kind"/> due at
    /// <paramref name="due"/> may run, seen at <paramref name="now"/>
    /// (TaskQueueThrottler::GetNextAllowedWakeUp): the latest of the next allowed times of the pools
    /// that can't run it then, never before now.
    /// </summary>
    public long AllowedRunTime(TimerKind kind, long due, long now)
    {
        var desired = Math.Max(due, now);
        var result = long.MinValue;
        var wakeUps = kind == TimerKind.HighNesting ? Intensive : Hidden;
        if (!wakeUps.CanRunTasksAt(desired))
            result = Math.Max(result, wakeUps.NextAllowedRunTime(desired, now));
        if (!Cpu.CanRunTasksAt(desired))
            result = Math.Max(result, Cpu.NextAllowedRunTime(desired));
        return result == long.MinValue ? desired : result;
    }

    /// <summary>A timer of <paramref name="kind"/> runs at <paramref name="now"/>: its pools wake up (TaskQueueThrottler::OnWakeUp).</summary>
    public void OnRun(TimerKind kind, long now)
    {
        if (!Throttles(kind, now))
            return;
        (kind == TimerKind.HighNesting ? Intensive : Hidden).OnWakeUp(now);
    }

    /// <summary>A throttled timer task ran from <paramref name="start"/> to <paramref name="end"/> (TaskQueueThrottler::OnTaskRunTimeReported).</summary>
    public void Ran(TimerKind kind, long start, long end)
    {
        if (!Throttles(kind, start))
            return;
        Cpu.RecordTaskRunTime(start, end);
    }

    /// <summary>TimeTicks::SnappedToNextTick: the first tick at or after <paramref name="time"/> of the ticks <paramref name="interval"/> apart through <paramref name="phase"/>.</summary>
    internal static long SnappedToNextTick(long time, long phase, long interval)
    {
        var offset = (phase - time) % interval;
        if (offset != 0 && phase < time)
            offset += interval;
        return time + offset;
    }

    internal static long Ticks(double seconds) => (long)Math.Round(seconds * 1e6);

    internal static double Seconds(long ticks) => ticks / 1e6;
}

/// <summary>The scheduler's task types for timers (DOMTimer's constructor and Fired).</summary>
internal enum TimerKind
{
    /// <summary>A 0 ms timer at nesting level 6 or less: never throttled.</summary>
    Immediate,
    /// <summary>A timer with a delay at nesting level 6 or less: throttled to a second when hidden.</summary>
    LowNesting,
    /// <summary>Deeper than nesting level 6: intensively throttled after the grace period.</summary>
    HighNesting,
}

/// <summary>Blink's WakeUpBudgetPool: wake-ups at most once per interval, aligned to it, each open for 3 ms.</summary>
internal sealed class WakeUpBudgetPool(UiTimerThrottling owner)
{
    public long? LastWakeUp;
    /// <summary>When set, a pool that hasn't woken for an interval may wake at this finer alignment.</summary>
    public long LowerAlignmentIfNoRecentWakeUp;

    public bool CanRunTasksAt(long moment)
    {
        if (LastWakeUp is not { } last)
            return false;
        if (last == moment)
            return true;
        return moment < last + UiTimerThrottling.WakeUpDuration;
    }

    public long NextAllowedRunTime(long desired, long now)
    {
        var interval = owner.IntervalOf(this, now);
        var origin = owner.Origin;
        // Not throttled while the desired time is still within the last wake-up.
        if (LastWakeUp is { } last && desired < last + UiTimerThrottling.WakeUpDuration)
            return desired;
        if (LowerAlignmentIfNoRecentWakeUp != 0)
        {
            if (LastWakeUp is not { } previous)
                return UiTimerThrottling.SnappedToNextTick(desired, origin, LowerAlignmentIfNoRecentWakeUp);
            // At least an interval after the last wake-up, at the finer alignment; or at the interval's own ticks.
            var aligned = UiTimerThrottling.SnappedToNextTick(Math.Max(desired, previous + interval), origin, LowerAlignmentIfNoRecentWakeUp);
            var atInterval = UiTimerThrottling.SnappedToNextTick(desired, origin, interval);
            return Math.Min(aligned, atInterval);
        }
        return UiTimerThrottling.SnappedToNextTick(desired, origin, interval);
    }

    public void OnWakeUp(long now)
    {
        // A wake-up within the last one's duration isn't a new one.
        if (LastWakeUp is { } last && now < last + UiTimerThrottling.WakeUpDuration)
            return;
        LastWakeUp = now;
    }
}

/// <summary>
/// Blink's CPUTimeBudgetPool with the background settings (page_scheduler_impl.cc): the budget
/// refills at 1% of the time while enabled, starts at 1 s and holds at most 3 s; tasks may run
/// while it isn't negative, else once it would have refilled.
/// </summary>
internal sealed class CpuTimeBudgetPool
{
    const double Rate = 0.01;
    const long MaxBudget = 3 * UiTimerThrottling.Second;
    const long InitialBudget = UiTimerThrottling.Second;

    public long Budget;
    public long LastCheckpoint;
    public bool Enabled;

    public CpuTimeBudgetPool(long now)
    {
        LastCheckpoint = now;
        // GrantAdditionalBudget at creation (the pool starts disabled, so no refill before it).
        Budget = Math.Min(InitialBudget, MaxBudget);
    }

    public void SetEnabled(bool enabled) => Enabled = enabled;

    public bool CanRunTasksAt(long moment)
    {
        if (!Enabled || Budget >= 0)
            return true;
        return moment - LastCheckpoint >= TimeToRecover;
    }

    long TimeToRecover => (long)(-Budget / Rate);

    public long NextAllowedRunTime(long desired)
    {
        if (!Enabled || Budget >= 0)
            return LastCheckpoint;
        return Math.Max(desired, LastCheckpoint + TimeToRecover);
    }

    public void RecordTaskRunTime(long start, long end)
    {
        Advance(end);
        if (Enabled)
            Budget = Math.Min(Budget - (end - start), MaxBudget);
    }

    void Advance(long now)
    {
        if (now <= LastCheckpoint)
            return;
        if (Enabled)
            Budget = Math.Min(Budget + (long)(Rate * (now - LastCheckpoint)), MaxBudget);
        LastCheckpoint = now;
    }
}

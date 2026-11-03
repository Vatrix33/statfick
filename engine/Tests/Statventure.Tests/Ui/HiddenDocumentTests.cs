using Statventure.Tests.Canvas;
using Statventure.Tests.Infrastructure;
using Statventure.UI.Dom;
using Statventure.UI.Paint;
using CanvasRenderer = Statventure.Canvas.Canvas;

namespace Statventure.Tests.Ui;

/// <summary>
/// The rules of hidden and unloaded documents the Chromium references don't reach on their own
/// (Docs/Scripting.md, "Hidden documents"; VisibilityReferenceTests compares the rest with Chromium):
/// Blink's wake-up and CPU-time budget pools as their sources define them, no frames while hidden,
/// the grace period's two lengths, the first contentful paint, unloading, and waking the host.
/// </summary>
public class HiddenDocumentTests
{
    const long S = UiTimerThrottling.Second;

    static UiDocument NewDocument(bool painted = true)
    {
        var document = new UiDocument(TestFonts.Library);
        document.SetViewport(400, 300);
        document.Update(0);
        if (painted)
            document.PaintedContent();
        return document;
    }

    // ---------- Blink's pools ----------

    [Fact]
    public void TicksSnapUpToTheIntervalThroughThePhase()
    {
        Assert.Equal(5 * S, UiTimerThrottling.SnappedToNextTick(5 * S, 0, S));
        Assert.Equal(6 * S, UiTimerThrottling.SnappedToNextTick(5 * S + 1, 0, S));
        Assert.Equal(5 * S + 300_000, UiTimerThrottling.SnappedToNextTick(4 * S + 400_000, 300_000, S));
        // A phase in the future works the same.
        Assert.Equal(2 * S + 300_000, UiTimerThrottling.SnappedToNextTick(2 * S, 9 * S + 300_000, S));
    }

    [Fact]
    public void AWakeUpPoolWakesOnItsTicksAndStaysOpenFor3Milliseconds()
    {
        var throttling = new UiTimerThrottling();
        var pool = throttling.Hidden;
        // Never woken: nothing may run, the first wake-up is the next tick.
        Assert.False(pool.CanRunTasksAt(0));
        Assert.Equal(2 * S, pool.NextAllowedRunTime(S + 1, S));
        pool.OnWakeUp(2 * S);
        Assert.True(pool.CanRunTasksAt(2 * S));
        Assert.True(pool.CanRunTasksAt(2 * S + 2_999));
        Assert.False(pool.CanRunTasksAt(2 * S + 3_000));
        // Within the wake-up a task runs when it's due; after it, at the next tick.
        Assert.Equal(2 * S + 2_000, pool.NextAllowedRunTime(2 * S + 2_000, 2 * S));
        Assert.Equal(3 * S, pool.NextAllowedRunTime(2 * S + 3_000, 2 * S));
        // A wake-up within the last one's duration isn't a new one.
        pool.OnWakeUp(2 * S + 1_000);
        Assert.Equal(2 * S, pool.LastWakeUp);
    }

    [Fact]
    public void TheIntensivePoolWakesOnTheMinuteOrOnTheSecondAfterAQuietMinute()
    {
        var throttling = new UiTimerThrottling();
        throttling.SetHidden(true, 0, loading: false);
        var pool = throttling.Intensive;
        // Before the grace period: a second, and the first wake-up on the second.
        Assert.Equal(S, pool.NextAllowedRunTime(S / 2, S / 2));
        pool.OnWakeUp(70 * S);
        // After it (60 s once painted): at least a minute after the last wake-up on the second, or on the minute's tick.
        Assert.Equal(120 * S, pool.NextAllowedRunTime(70 * S + 5_000, 70 * S + 5_000));
        Assert.Equal(131 * S, pool.NextAllowedRunTime(130 * S + 5_000, 130 * S + 5_000));
        // The hidden pool stays at a second.
        throttling.Hidden.OnWakeUp(70 * S);
        Assert.Equal(71 * S, throttling.Hidden.NextAllowedRunTime(70 * S + 5_000, 70 * S + 5_000));
    }

    [Fact]
    public void AnIntensivePoolNeverWokenWakesOnTheSecondEvenAfterTheGracePeriod()
    {
        var throttling = new UiTimerThrottling();
        throttling.SetHidden(true, 0, loading: false);
        Assert.Null(throttling.Intensive.LastWakeUp);
        Assert.Equal(71 * S, throttling.Intensive.NextAllowedRunTime(70 * S + 500_000, 70 * S + 500_000));
    }

    [Fact]
    public void AnOverdueTimerIsAllowedNoEarlierThanNow()
    {
        // Chromium never schedules a wake-up in the past: the next tick after now, not after the due time.
        var throttling = new UiTimerThrottling();
        throttling.SetHidden(true, 0, loading: false);
        throttling.Hidden.OnWakeUp(10 * S);
        Assert.True(throttling.Throttles(TimerKind.LowNesting, 11 * S));
        Assert.False(throttling.Throttles(TimerKind.Immediate, 11 * S));
        Assert.Equal(12 * S, throttling.AllowedRunTime(TimerKind.LowNesting, 10 * S + 200_000, 11 * S + 200_000));
    }

    [Fact]
    public void TheGracePeriodIsAMinuteOncePaintedElseFive()
    {
        foreach (var (loading, grace) in new[] { (false, 60 * S), (true, 300 * S) })
        {
            var throttling = new UiTimerThrottling();
            throttling.SetHidden(true, 10 * S, loading);
            Assert.Equal(S, throttling.IntervalOf(throttling.Intensive, 10 * S + grace - 1));
            Assert.Equal(60 * S, throttling.IntervalOf(throttling.Intensive, 10 * S + grace));
            Assert.Equal(S, throttling.IntervalOf(throttling.Hidden, 10 * S + grace));
            // The CPU budget's start, then the grace period's end.
            Assert.Equal(10 * S + UiTimerThrottling.CpuThrottlingDelay, throttling.NextStateChange(10 * S));
            Assert.Equal(10 * S + grace, throttling.NextStateChange(10 * S + UiTimerThrottling.CpuThrottlingDelay));
            // Shown again: back to a second.
            throttling.SetHidden(false, 10 * S + grace + S, loading);
            Assert.Equal(S, throttling.IntervalOf(throttling.Intensive, 10 * S + grace + S));
        }
    }

    [Fact]
    public void TheCpuBudgetRefillsOnlyWhileEnabledAndHoldsAtMostThreeSeconds()
    {
        var pool = new CpuTimeBudgetPool(0);
        Assert.Equal(S, pool.Budget);
        // Disabled: runs move the checkpoint but cost nothing.
        pool.RecordTaskRunTime(5 * S, 5 * S + 500_000);
        Assert.Equal(S, pool.Budget);
        Assert.Equal(5 * S + 500_000, pool.LastCheckpoint);
        pool.SetEnabled(true);
        // Enabled: 1% of the time since the checkpoint comes back, then the run's time goes.
        pool.RecordTaskRunTime(15 * S, 15 * S + 1_200_000);
        Assert.Equal(S + 107_000 - 1_200_000, pool.Budget);
        Assert.False(pool.CanRunTasksAt(16 * S));
        // 93 ms of debt waits 9.3 s from the checkpoint.
        Assert.Equal(16 * S + 200_000 + 9_300_000, pool.NextAllowedRunTime(16 * S));
        Assert.True(pool.CanRunTasksAt(16 * S + 200_000 + 9_300_000));
        // At most 3 s.
        pool.RecordTaskRunTime(1000 * S, 1000 * S);
        Assert.Equal(3 * S, pool.Budget);
    }

    [Fact]
    public void AudioKeepsAHiddenPageUnthrottledForThirtySecondsAfterItStops()
    {
        var throttling = new UiTimerThrottling();
        throttling.SetAudible(true, 0);
        throttling.SetHidden(true, S, loading: false);
        Assert.False(throttling.Throttled(20 * S));
        throttling.SetAudible(false, 20 * S);
        Assert.False(throttling.Throttled(50 * S - 1));
        Assert.True(throttling.Throttled(50 * S));
        Assert.Equal(50 * S, throttling.NextStateChange(41 * S));
        // Sound again: unthrottled at once.
        throttling.SetAudible(true, 60 * S);
        Assert.False(throttling.Throttled(60 * S));
    }

    // ---------- Documents ----------

    [Fact]
    public void AHiddenDocumentGetsNoFramesButItsTasksRun()
    {
        var document = NewDocument();
        var log = new List<string>();
        document.RunScript(() => document.RequestAnimationFrame(_ => log.Add("frame")));
        document.SetHidden(true);
        document.RunScript(() => document.SetTimeout(() => log.Add("immediate"), 0));
        document.Update(0.5);
        document.Update(5);
        Assert.Equal(["immediate"], log);
        document.SetHidden(false);
        document.Update(5.02);
        Assert.Equal(["immediate", "frame"], log);
    }

    [Fact]
    public void ALowNestingTimerWakesOnTheSecondAndADeepOneOnTheMinute()
    {
        var document = NewDocument();
        var log = new List<double>();
        var deep = new List<double>();
        // A chain eight links deep, then each link logged.
        var depth = 0;
        void Link() => document.SetTimeout(() => { if (++depth > 7) deep.Add(document.PerformanceNow); Link(); }, 10);
        document.RunScript(Link);
        document.Update(0.2);
        document.SetHidden(true);
        document.RunScript(() => document.SetTimeout(() => log.Add(document.PerformanceNow), 10));
        for (var t = 0.25; t < 200; t += 0.25)
            document.Update(t);
        // The one-shot ran on the next second.
        Assert.Equal([1000.0], log);
        var hidden = deep.Where(t => t > 200).ToList();
        // Once a second until 60 s after hiding (at 0.2 s), then on the minute.
        Assert.Contains(60_000.0, hidden);
        Assert.DoesNotContain(61_000.0, hidden);
        Assert.Contains(120_000.0, hidden);
        Assert.Contains(180_000.0, hidden);
        Assert.Equal(hidden.Count(t => t <= 60_000) + 2, hidden.Count);
    }

    [Fact]
    public void OneLongUpdateChangesTheThrottlingAtItsTime()
    {
        // The host may wake late (or a test may jump): the grace period's end still applies at its
        // time within one update, as Blink's control task runs at its time.
        List<double> Run(double step)
        {
            var document = NewDocument();
            var log = new List<double>();
            document.RunScript(() => document.SetInterval(() => log.Add(document.PerformanceNow), 100));
            document.Update(1.5);
            document.SetHidden(true);
            for (var t = 1.5 + step; t < 200; t += step)
                document.Update(t);
            document.Update(200);
            return log;
        }
        var small = Run(0.25);
        Assert.Contains(61_000.0, small);
        Assert.DoesNotContain(62_000.0, small);
        Assert.Equal(small, Run(1000));
    }

    [Fact]
    public void TimersShownAfterThrottlingRunInDueOrder()
    {
        var document = NewDocument();
        var log = new List<(double, string)>();
        document.RunScript(() =>
        {
            document.SetInterval(() => log.Add((Math.Round(document.PerformanceNow), "a")), 300);
            document.SetInterval(() => log.Add((Math.Round(document.PerformanceNow), "b")), 70);
        });
        document.Update(0.05);
        document.SetHidden(true);
        document.Update(5);
        log.Clear();
        document.SetHidden(false, 5);
        document.Update(5.3);
        // Both ran at the 5 s wake-up; then each on its own beat: b every 70 ms from 5.04 s, a at 5.1 s.
        Assert.Equal([(5040.0, "b"), (5100.0, "a"), (5110.0, "b"), (5180.0, "b"), (5250.0, "b")], log);
    }

    [Fact]
    public void AHiddenDocumentGetsItsSelectionChanges()
    {
        // selectionchange is a task (kMiscPlatformAPI), not a frame step.
        var document = NewDocument();
        Markup.SetInnerHtml(document.Body, """<input id="field" value="hello">""");
        document.Update(0.1);
        var log = new List<string>();
        document.AddEventListener("selectionchange", e => log.Add(e.Type + " " + document.VisibilityState), capture: true);
        document.SetHidden(true);
        document.RunScript(() => document.SetTimeout(() => document.QuerySelector("#field")!.Select(), 0));
        document.Update(0.2);
        Assert.Equal(["selectionchange hidden"], log);
    }

    [Fact]
    public void AnUnpaintedDocumentWaitsFiveMinutesForIntensiveThrottling()
    {
        var document = NewDocument(painted: false);
        var log = new List<double>();
        document.RunScript(() => document.SetInterval(() => log.Add(document.PerformanceNow), 100));
        document.Update(1.5);
        document.SetHidden(true);
        for (var t = 1.75; t < 400; t += 0.25)
            document.Update(t);
        Assert.Contains(299_000.0, log);
        Assert.Contains(300_000.0, log);
        Assert.Contains(301_000.0, log);
        Assert.DoesNotContain(302_000.0, log);
        Assert.Contains(360_000.0, log);
    }

    [Fact]
    public void UnloadingSendsPagehideVisibilityChangeAndUnloadThenNothingRuns()
    {
        var document = NewDocument();
        var log = new List<string>();
        foreach (var type in new[] { "pagehide", "visibilitychange", "webkitvisibilitychange", "unload" })
            document.AddEventListener(type, e => log.Add(e.Type + " " + document.VisibilityState));
        document.RunScript(() => document.SetTimeout(() => log.Add("timer"), 100));
        document.Unload();
        Assert.Equal(["pagehide visible", "visibilitychange hidden", "webkitvisibilitychange hidden", "unload hidden"], log);
        Assert.True(document.Hidden && document.Unloaded);
        // Nothing runs any more: not its timers, not a task another thread posts.
        var other = new Thread(() => document.EventLoop.Post(_ => log.Add("posted"), null));
        other.Start();
        other.Join();
        document.Update(1);
        document.SetHidden(false);
        document.Unload();
        Assert.Equal(4, log.Count);
        Assert.Null(document.NextWakeUp);
    }

    [Fact]
    public void UnloadingAHiddenDocumentDoesntChangeItsVisibilityAgain()
    {
        var document = NewDocument();
        document.SetHidden(true);
        var log = new List<string>();
        foreach (var type in new[] { "pagehide", "visibilitychange", "unload" })
            document.AddEventListener(type, e => log.Add(e.Type));
        document.Unload();
        Assert.Equal(["pagehide", "unload"], log);
    }

    [Fact]
    public void AHiddenDocumentTellsWhenItNextHasWork()
    {
        var document = NewDocument();
        document.EventLoop.WakeUpAlignment = 0;
        document.Update(0.5);
        document.SetHidden(true);
        // Nothing pending but the throttling's own changes (the CPU budget's start).
        Assert.Equal(10.5, document.NextWakeUp!.Value, 6);
        document.RunScript(() => document.SetTimeout(() => { }, 100));
        Assert.Equal(1, document.NextWakeUp!.Value, 6);
        // A task posted from another thread wakes the host.
        var woken = 0;
        document.EventLoop.Wake = () => woken++;
        var other = new Thread(() => document.EventLoop.Post(_ => { }, null));
        other.Start();
        other.Join();
        Assert.Equal(1, woken);
    }

    [Fact]
    public void AnIntervalLateFromThrottlingSkipsTheBeatsItMissed()
    {
        var document = NewDocument();
        var log = new List<double>();
        document.RunScript(() => document.SetInterval(() => log.Add(document.PerformanceNow), 300));
        document.Update(0.05);
        document.SetHidden(true);
        document.Update(1.5);
        document.SetHidden(false);
        document.Update(1.6);
        document.Update(2.0);
        // Hidden it ran once at 1 s (not catching up 300, 600, 900); shown again at 1.5 s its
        // missed 1.2 s beat runs at once, then it's on its own beat again.
        Assert.Equal([1000.0, 1500.0, 1800.0], log);
    }

    [Fact]
    public void ShownLaterThanItsLastWakeUpALateIntervalRunsOnce()
    {
        var document = NewDocument();
        var log = new List<double>();
        document.RunScript(() => document.SetInterval(() => log.Add(document.PerformanceNow), 250));
        document.Update(0.9);
        document.SetHidden(true);
        // The host sleeps until the wake-up at 1 s and the one at 2 s, and the window comes back at 2.94 s.
        document.Update(1);
        document.Update(2);
        document.SetHidden(false, 2.94);
        Assert.Equal(2.94, document.EventLoop.Time, 6);
        document.Update(2.94);
        document.Update(3.1);
        // Not the beats it missed between 2 s and 2.94 s: once on showing, then from 3 s on its beat.
        Assert.Equal([250.0, 500.0, 750.0, 1000.0, 2000.0, 2940.0, 3000.0], log);
    }

    [Collection("GPU")]
    public class Painting(GpuFixture gpu)
    {
        [Theory]
        [InlineData("""<img src="picture" style="width: 10px; height: 10px">""")]
        [InlineData("""<svg width="10" height="10"><rect width="10" height="10" fill="red"/></svg>""")]
        [InlineData("""<surface style="display: block; width: 10px; height: 10px"></surface>""")]
        public void AnImageSvgOrSurfaceIsContentfulToo(string html)
        {
            var document = new UiDocument(TestFonts.Library);
            document.SetViewport(200, 100);
            document.Layout.Context.ImageSize = _ => new System.Numerics.Vector2(10, 10);
            Markup.SetInnerHtml(document.Body, html);
            // Recorded only (never rendered), so any texture index will do.
            using var painter = new UiPainter(document) { ImageTexture = _ => 0, SurfaceTexture = _ => 0 };
            using var canvas = new CanvasRenderer(gpu.Device);
            document.Update(0.5);
            canvas.Begin(200, 100);
            painter.Paint(canvas);
            Assert.Equal((double?)0.5, document.FirstContentfulPaint);
        }

        [Fact]
        public void TheFirstContentfulPaintIsTheFirstFrameWithText()
        {
            var document = new UiDocument(TestFonts.Library);
            document.SetViewport(200, 100);
            Markup.SetInnerHtml(document.Body, """<div style="width: 50px; height: 20px; background: red"></div><img src="missing" style="width: 10px; height: 10px">""");
            using var painter = new UiPainter(document);
            using var canvas = new CanvasRenderer(gpu.Device);
            void Paint(double time)
            {
                document.Update(time);
                canvas.Begin(200, 100);
                painter.Paint(canvas);
            }
            Paint(0.5);
            Assert.Null(document.FirstContentfulPaint);
            Markup.SetInnerHtml(document.Body, "<p>Text</p>");
            Paint(1);
            Assert.Equal((double?)1, document.FirstContentfulPaint);
            Paint(2);
            Assert.Equal((double?)1, document.FirstContentfulPaint);
        }
    }
}

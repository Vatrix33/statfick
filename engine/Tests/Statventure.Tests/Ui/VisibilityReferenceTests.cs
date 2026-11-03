using System.Globalization;
using System.Text.Json;
using Statventure.Platform;
using Statventure.Tests.Canvas;
using Statventure.Tests.Infrastructure;
using Statventure.UI;
using Statventure.UI.Dom;

namespace Statventure.Tests.Ui;

/// <summary>
/// The engine hides, shows and unloads a document, and runs a hidden document's timers, as Chromium
/// 152 did with the old game's window (Reference/visibility-results.json, from
/// Tools/Scripts/make-visibility-references.sh; Docs/Scripting.md, "Hidden documents"). Each
/// scenario of Tools/VisibilityReference has its C# twin here, on the engine's clock in the page's
/// milliseconds; where Chromium's throttled wake-ups are aligned (to its monotonic clock, arbitrary
/// to the page) is taken from its log.
/// </summary>
public class VisibilityReferenceTests
{
    static string ReferenceDirectory => Path.Combine(Path.GetDirectoryName(Golden.GoldenPath("x"))!, "..", "Reference");

    /// <summary>A scenario's log: (page time in ms, text); the harness's own lines ("main …") have no time.</summary>
    static List<(double Time, string Text)> Chromium(string scenario)
    {
        var path = Path.Combine(ReferenceDirectory, "visibility-results.json");
        Assert.True(File.Exists(path), "no Chromium results: run Tools/Scripts/make-visibility-references.sh");
        using var results = JsonDocument.Parse(File.ReadAllText(path));
        Assert.True(results.RootElement.TryGetProperty(scenario, out var entry), $"no Chromium result for '{scenario}'");
        var lines = new List<(double, string)>();
        foreach (var item in entry.GetProperty("log"u8).EnumerateArray())
        {
            var line = item.GetString()!;
            var space = line.IndexOf(' ');
            lines.Add(line.StartsWith("main ", StringComparison.Ordinal)
                ? (double.NaN, line)
                : (double.Parse(line[..space], CultureInfo.InvariantCulture), line[(space + 1)..]));
        }
        return lines;
    }

    static double TimeOf(List<(double Time, string Text)> log, string text) => log.First(l => l.Text == text).Time;

    /// <summary>
    /// How late Chromium may run a task the engine runs on time (ms): its throttled wake-ups are
    /// "flexible, no sooner" with the message pump's leeway (base's kDefaultLeeway, 8 ms on Linux),
    /// so they run up to 8 ms after their time; plus 1 ms for the page's rounded log. Never earlier
    /// than the engine, beyond that rounding.
    /// </summary>
    const double Leeway = 8 + 1;

    static bool OnTime(double chromium, double engine) => chromium - engine >= -1 && chromium - engine <= Leeway;

    static UiDocument NewDocument(string html = "")
    {
        var document = new UiDocument(TestFonts.Library);
        document.SetViewport(400, 300);
        Markup.SetInnerHtml(document.Body, """<p style="font: 20px sans-serif">Visibility reference</p>""" + html);
        document.Update(0);
        // The page has had its first contentful paint (the harness's pages show their text first).
        document.PaintedContent();
        return document;
    }

    /// <summary>Runs the document's frames at 60 Hz from its clock up to <paramref name="until"/> (seconds).</summary>
    static void Frames(UiDocument document, double until)
    {
        var start = document.EventLoop.Time;
        for (var frame = 1; start + frame / 60.0 < until; frame++)
            document.Update(start + frame / 60.0);
        document.Update(until);
    }

    /// <summary>A hidden document as the window host runs it: an update at each wake-up it asks for, up to <paramref name="until"/>.</summary>
    static void Sleep(UiDocument document, double until)
    {
        for (var i = 0; i < 100_000 && document.NextWakeUp is { } next && next < until; i++)
            document.Update(next);
        document.Update(until);
    }

    // ---------- Events ----------

    /// <summary>
    /// Hiding (the window blurs, then the document turns hidden), showing (visible, then the window's
    /// focus back) and closing (pagehide, hidden, unload): the same events with the same properties
    /// and states, in the same order. Chromium's window and document targets are one in the engine
    /// (the document's listeners are the window's), and beforeunload isn't sent (the user's decision
    /// of 2026-11-03).
    /// </summary>
    [Fact]
    public void HidingShowingAndClosingSendChromiumsEvents()
    {
        var want = new List<string>();
        var started = false;
        foreach (var (_, text) in Chromium("events"))
        {
            started |= text == "main hide";
            if (!started || text.StartsWith("event beforeunload", StringComparison.Ordinal))
                continue;
            var parts = text.Split(' ').Where(p => !p.StartsWith("phase=", StringComparison.Ordinal))
                .Select(p => p is "target=window" ? "target=document" : p);
            want.Add(string.Join(' ', parts));
        }

        var document = NewDocument("""<input id="field">""");
        var got = new List<string>();
        foreach (var type in new[] { "visibilitychange", "webkitvisibilitychange", "pagehide", "pageshow", "beforeunload", "unload", "freeze", "resume", "blur", "focus", "focusin", "focusout" })
            document.AddEventListener(type, e =>
            {
                var target = e.Target is { } element ? "#" + element.Id : "document";
                var kind = e switch { UiPageTransitionEvent => "PageTransitionEvent", UiFocusEvent => "FocusEvent", _ => "Event" };
                var line = $"event {e.Type} target={target} bubbles={Js(e.Bubbles)} cancelable={Js(e.Cancelable)} trusted={Js(e.IsTrusted)} class={kind}";
                if (e is UiPageTransitionEvent transition)
                    line += $" persisted={Js(transition.Persisted)}";
                got.Add(line + $" hidden={Js(document.Hidden)} state={document.VisibilityState} hasFocus={Js(document.WindowFocused)}");
            }, capture: true);
        document.AddEventListener("visibilitychange", _ => got.Add("visibility " + document.VisibilityState));
        document.QuerySelector("#field")!.Focus();
        document.Update(0.1);
        got.Clear();
        // The window host's path: GLFW's events, in the order the window system sends them.
        var host = new UiHostInput(document.Input, (_, _) => null) { Visibility = document.SetHidden };
        got.Add("main hide");
        host.Dispatch([new InputEvent(InputEventKind.FocusLost), new InputEvent(InputEventKind.Minimized)]);
        document.Update(1.6);
        got.Add("main show");
        host.Dispatch([new InputEvent(InputEventKind.Restored), new InputEvent(InputEventKind.FocusGained)]);
        document.Update(3.1);
        got.Add("main close");
        document.Unload();
        Assert.Equal(want, got);
    }

    static string Js(bool value) => value ? "true" : "false";

    // ---------- Timers ----------

    /// <summary>
    /// A hidden document's timers: an interval and chains woken once a second, then (60 s after
    /// hiding, the page having painted) once a minute; one-shots made while hidden by their kinds
    /// (a 10 ms one on the second, a 0 ms one at once); after every timer stopped, a new chain's
    /// first deep link on the second (the intensive pool hadn't woken for a minute), the next on the
    /// minute; shown again, the rest at once. Every firing while hidden and after at Chromium's time
    /// (<see cref="Leeway"/>: Chromium's wake-ups may come up to 8 ms late, and its first tasks after
    /// showing a few ms after the visibility change).
    /// </summary>
    [Fact]
    public void HiddenTimersRunWhenChromiumRanThem()
    {
        var chromium = Chromium("timers");
        var hide = TimeOf(chromium, "visibility hidden");
        var show = TimeOf(chromium, "visibility visible");
        // The first wake-up on the minute: the first interval firing more than 2 s after the one before.
        var interval = chromium.Where(l => l.Text == "interval100").Select(l => l.Time).ToList();
        var minuteTick = interval.Zip(interval.Skip(1)).First(p => p.Second - p.First > 2000).Second;

        var document = NewDocument();
        document.EventLoop.WakeUpAlignment = minuteTick / 1000;
        var got = new List<(double Time, string Text)>();
        void Log(string text) => got.Add((Math.Round(document.PerformanceNow), text));
        var running = true;
        int intervalId = 0;
        document.RunScript(() =>
        {
            intervalId = document.SetInterval(() => Log("interval100"), 100);
            void Chain0() => document.SetTimeout(() => { if (!running) return; Log("chain0"); Chain0(); }, 0);
            void Chain250() => document.SetTimeout(() => { if (!running) return; Log("chain250"); Chain250(); }, 250);
            Chain0();
            Chain250();
        });
        Frames(document, hide / 1000);
        document.SetHidden(true);
        // What the harness ran in the page while it was hidden, at the times the page logged it.
        var injections = new List<(double Time, Action Run)>();
        foreach (var (time, text) in chromium.Where(l => l.Time > hide && l.Time < show))
        {
            if (text.StartsWith("install oneshot", StringComparison.Ordinal))
            {
                var delay = int.Parse(text["install oneshot".Length..], CultureInfo.InvariantCulture);
                injections.Add((time, () => { Log(text); document.SetTimeout(() => Log("oneshot" + delay), delay); }));
            }
            else if (text == "stop")
                injections.Add((time, () => { Log("stop"); running = false; document.ClearInterval(intervalId); }));
            else if (text == "new chain")
                injections.Add((time, () =>
                {
                    Log("new chain");
                    var depth = 0;
                    void Link() => document.SetTimeout(() => { depth++; Log("link" + depth); if (depth < 12) Link(); }, 0);
                    Link();
                }));
        }
        foreach (var (time, run) in injections)
        {
            Sleep(document, time / 1000);
            document.RunScript(run);
        }
        Sleep(document, show / 1000);
        document.SetHidden(false);
        Frames(document, (show + 1000) / 1000);

        CompareFirings(chromium.Where(l => l.Time > hide && !l.Text.StartsWith("visibility", StringComparison.Ordinal)),
            got.Where(l => l.Time > hide));
    }

    /// <summary>Per log text, the same number of firings, each <see cref="OnTime"/>.</summary>
    static void CompareFirings(IEnumerable<(double Time, string Text)> want, IEnumerable<(double Time, string Text)> got)
    {
        var expected = want.GroupBy(l => l.Text).ToDictionary(g => g.Key, g => g.Select(l => l.Time).ToList());
        var actual = got.GroupBy(l => l.Text).ToDictionary(g => g.Key, g => g.Select(l => l.Time).ToList());
        var problems = new List<string>();
        foreach (var key in expected.Keys.Union(actual.Keys).Order(StringComparer.Ordinal))
        {
            var w = expected.GetValueOrDefault(key) ?? [];
            var g = actual.GetValueOrDefault(key) ?? [];
            if (w.Count != g.Count)
                problems.Add($"{key}: Chromium {w.Count} times, engine {g.Count} (Chromium {string.Join(",", w.Take(8))}…, engine {string.Join(",", g.Take(8))}…)");
            else
                for (var i = 0; i < w.Count; i++)
                    if (!OnTime(w[i], g[i]))
                    {
                        problems.Add($"{key} #{i}: Chromium {w[i]}, engine {g[i]}");
                        break;
                    }
        }
        Assert.True(problems.Count == 0, string.Join("\n", problems));
    }

    // ---------- The CPU-time budget ----------

    /// <summary>
    /// An interval that is busy for 150 ms each time, hidden: woken once a second; from 10 s after
    /// hiding its runs spend the CPU-time budget (1 s, refilled at 1%), and once it's below zero the
    /// next run waits until it would have refilled; shown again it runs at once and then on its
    /// beat. The engine is given the costs the page measured. Every run starts at Chromium's time
    /// (<see cref="Leeway"/>), except the one after the wait: Chromium also charges what each task spends
    /// around its busy loop (entering it, the log line), which the page can't measure, and every
    /// millisecond of debt waits 100 ms; up to 0.25 ms per charged run is allowed (the wait may end
    /// that much later, never earlier).
    /// </summary>
    [Fact]
    public void TheCpuTimeBudgetHoldsBackBusyTimersAsChromiums()
    {
        var chromium = Chromium("cpu");
        var hide = TimeOf(chromium, "visibility hidden");
        var show = TimeOf(chromium, "visibility visible");
        // Runs as (start, cost): the page logs when the busy loop ended and how long it took.
        var runs = chromium.Where(l => l.Text.StartsWith("busy ", StringComparison.Ordinal))
            .Select(l => (Start: l.Time - double.Parse(l.Text[5..], CultureInfo.InvariantCulture), Cost: double.Parse(l.Text[5..], CultureInfo.InvariantCulture)))
            .ToList();
        var firstHidden = runs.First(r => r.Start > hide).Start;

        var document = NewDocument();
        document.EventLoop.WakeUpAlignment = firstHidden / 1000;
        var costs = new Queue<double>(runs.Select(r => r.Cost / 1000));
        document.EventLoop.TaskCost = () => costs.TryDequeue(out var cost) ? cost : 0.150;
        var got = new List<double>();
        Frames(document, (runs[0].Start - 200) / 1000);
        document.RunScript(() => document.SetInterval(() => got.Add(document.PerformanceNow), 200));
        Frames(document, hide / 1000);
        document.SetHidden(true);
        Sleep(document, show / 1000);
        document.SetHidden(false);
        Frames(document, (runs[^1].Start + 1) / 1000);

        Assert.Equal(runs.Count, got.Count);
        // The run that waited for the budget: the first hidden run more than 2 s after the one before.
        var waited = Enumerable.Range(1, runs.Count - 1).First(i => runs[i].Start > hide && runs[i].Start < show && runs[i].Start - runs[i - 1].Start > 2000);
        var charged = runs.Count(r => r.Start > hide + 10_000 && r.Start < runs[waited].Start);
        for (var i = 0; i < runs.Count; i++)
        {
            var late = runs[i].Start - got[i];
            if (i == waited)
                Assert.True(late >= -1 && late <= charged * 0.25 * 100 + Leeway, $"run {i} after the budget's wait: Chromium {runs[i].Start}, engine {got[i]} ({charged} charged runs)");
            else
                Assert.True(OnTime(runs[i].Start, got[i]), $"run {i}: Chromium {runs[i].Start}, engine {got[i]}");
        }
    }

    // ---------- Audible pages ----------

    /// <summary>
    /// A page whose sound stops when it's hidden: its timers aren't throttled until 30 s after it
    /// went silent, then they wake on the second. Chromium hears the silence a little after the
    /// oscillator stops (the output's power fades, a poll 15 times a second, 100 ms of glitch
    /// tolerance, the IPC to the page): its throttling starts within 0.6 s after the engine's
    /// delay; the engine, whose sound pauses when its window is minimized, is silent at once.
    /// </summary>
    [Fact]
    public void AnAudiblePageIsNotThrottledForThirtySecondsAfterItsSoundStopped()
    {
        var chromium = Chromium("audible");
        var hide = TimeOf(chromium, "visibility hidden");
        var stopped = TimeOf(chromium, "audio stopped");
        var chain = chromium.Where(l => l.Text == "chain250").Select(l => l.Time).ToList();
        var (lastFree, firstThrottled) = FirstGap(chain, hide);
        var delay = UiTimerThrottling.RecentAudioDelay / 1000.0;
        // Chromium's throttling began after the last free run and before the next run was due.
        Assert.True(lastFree + 250 > stopped + delay && lastFree < stopped + delay + 600,
            $"Chromium's timers ran freely until {lastFree - stopped} ms after the sound stopped; the engine's delay is {delay} ms");

        var document = NewDocument();
        document.EventLoop.WakeUpAlignment = firstThrottled / 1000;
        var got = new List<double>();
        document.Audible = true;
        document.RunScript(() =>
        {
            void Chain() => document.SetTimeout(() => { got.Add(document.PerformanceNow); Chain(); }, 250);
            Chain();
        });
        Frames(document, hide / 1000);
        document.SetHidden(true);
        document.Audible = false;
        var show = TimeOf(chromium, "visibility visible");
        Sleep(document, show / 1000);
        var (engineLastFree, engineFirstThrottled) = FirstGap(got, hide);
        Assert.True(engineLastFree <= hide + delay && engineLastFree + 250 > hide + delay, $"the engine's timers ran freely until {engineLastFree - hide} ms after hiding");
        // Then both wake on the same seconds: the engine's wake-ups are Chromium's, except perhaps one
        // more before Chromium's first (it heard the silence later).
        var chromiumTicks = chain.Where(t => t >= firstThrottled && t < show).ToList();
        var engineTicks = got.Where(t => t >= engineFirstThrottled && t < show).ToList();
        var early = engineTicks.Count(t => t < firstThrottled - 1);
        Assert.True(early <= 1, $"{early} engine wake-ups before Chromium's first");
        Assert.Equal(chromiumTicks.Count, engineTicks.Count - early);
        for (var i = 0; i < chromiumTicks.Count; i++)
            Assert.True(OnTime(chromiumTicks[i], engineTicks[early + i]), $"wake-up {i}: Chromium {chromiumTicks[i]}, engine {engineTicks[early + i]}");
        if (early == 1)
            Assert.True(OnTime(firstThrottled - 1000, engineTicks[0]), $"the engine's extra wake-up at {engineTicks[0]}");
    }

    /// <summary>The last firing before the first gap of more than 300 ms after <paramref name="after"/>, and the firing after the gap.</summary>
    static (double LastFree, double FirstThrottled) FirstGap(List<double> times, double after)
    {
        for (var i = 1; i < times.Count; i++)
            if (times[i - 1] > after && times[i] - times[i - 1] > 300)
                return (times[i - 1], times[i]);
        throw new InvalidOperationException("the timers were never throttled");
    }
}

using Statventure.Tests.Canvas;
using Statventure.UI.Dom;

namespace Statventure.Tests.Ui;

/// <summary>
/// What the script APIs do for C# beyond the Chromium references (<see cref="ScriptReferenceTests"/>):
/// invalid keyframes and options throw as JavaScript's TypeErrors, await continues in microtasks,
/// work from other threads comes back as tasks of the loop.
/// </summary>
public class ScriptApiTests
{
    static UiDocument Document(string html)
    {
        var document = new UiDocument(TestFonts.Library);
        document.SetViewport(400, 300);
        Markup.SetInnerHtml(document.Body, html);
        document.Update(0);
        return document;
    }

    static Keyframe[] Fade => [new Keyframe { ["opacity"] = "0" }, new Keyframe { ["opacity"] = "1" }];

    [Fact]
    public void InvalidKeyframesAndOptionsThrow()
    {
        var document = Document("""<div id="a"></div>""");
        var a = document.GetElementById("a")!;
        Assert.Throws<ArgumentException>(() => a.Animate(Fade, new AnimateOptions { Duration = -1 }));
        Assert.Throws<ArgumentException>(() => a.Animate(Fade, new AnimateOptions { Duration = 100, Iterations = double.NaN }));
        Assert.Throws<ArgumentException>(() => a.Animate(Fade, new AnimateOptions { Duration = 100, Easing = "bounce" }));
        Assert.Throws<ArgumentException>(() => a.Animate([new Keyframe { ["opacity"] = "0", Easing = "nope" }, new Keyframe { ["opacity"] = "1" }], 100));
        Assert.Throws<ArgumentException>(() => a.Animate([new Keyframe { ["opacity"] = "0", Offset = 0.6 }, new Keyframe { ["opacity"] = "1", Offset = 0.4 }], 100));
        Assert.Throws<ArgumentException>(() => a.Animate([new Keyframe { ["opacity"] = "0", Offset = 1.5 }], 100));
        var endless = a.Animate(Fade, new AnimateOptions { Duration = 100, Iterations = double.PositiveInfinity });
        Assert.Throws<InvalidOperationException>(endless.Finish);
        Assert.Throws<InvalidOperationException>(() => new UiElement("div").Animate(Fade, 100));
    }

    [Fact]
    public void KeyframesTakeCamelCaseNames()
    {
        var keyframe = new Keyframe { ["backgroundColor"] = "red", ["cssFloat"] = "left", ["--my-var"] = "1" };
        Assert.Equal(["background-color", "float", "--my-var"], keyframe.Values.Select(v => v.Key));
        Assert.Equal("red", keyframe["background-color"]);
    }

    [Fact]
    public void AwaitingFinishedContinuesInTheMicrotasksBeforeTheFinishEvent()
    {
        var document = Document("""<div id="a"></div>""");
        var a = document.GetElementById("a")!;
        var log = new List<string>();
        document.RunScript(() =>
        {
            var animation = a.Animate(Fade, 40);
            animation.OnFinish = _ => log.Add("finish event");
            Wait(animation);
        });
        for (var frame = 1; frame <= 6; frame++)
            document.Update(frame / 60.0);
        Assert.Equal(["awaited finished", "after await", "finish event"], log);

        async void Wait(UiAnimation animation)
        {
            var finished = await animation.Finished;
            log.Add(finished == animation ? "awaited finished" : "wrong animation");
            await Task.Yield();
            log.Add("after await");
        }
    }

    [Fact]
    public void ACancelledAnimationsFinishedThrowsWhenAwaited()
    {
        var document = Document("""<div id="a"></div>""");
        var a = document.GetElementById("a")!;
        var log = new List<string>();
        document.RunScript(() =>
        {
            var animation = a.Animate(Fade, 400);
            Wait(animation);
            animation.Cancel();
        });
        Assert.Equal(["cancelled"], log);

        async void Wait(UiAnimation animation)
        {
            try
            {
                await animation.Finished;
                log.Add("finished");
            }
            catch (OperationCanceledException)
            {
                log.Add("cancelled");
            }
        }
    }

    [Fact]
    public void WorkFromAnotherThreadComesBackAsATaskOfTheLoop()
    {
        var document = Document("");
        var loopThread = Environment.CurrentManagedThreadId;
        var log = new List<string>();
        var release = new ManualResetEventSlim();
        var posted = new ManualResetEventSlim();
        var continued = new ManualResetEventSlim();
        // The signal an application's loop sleeps on (UiHost: the window's empty event): another thread posted a task.
        document.EventLoop.Wake = posted.Set;
        try
        {
            document.RunScript(() => Work());
            // The work waits to be let go, so the await has handed its continuation to the loop.
            Assert.Equal(["before"], log);
            release.Set();
            Assert.True(WaitHandle.WaitAny([posted.WaitHandle, continued.WaitHandle], TimeSpan.FromSeconds(30)) != WaitHandle.WaitTimeout,
                "the work's continuation was neither posted to the loop nor run within 30 s");
            // Posted from the other thread, it waits for the loop's next turn.
            Assert.Equal(["before"], log);
            document.Update(1 / 60.0);
            Assert.Equal(["before", $"after on loop {true}"], log);
        }
        finally
        {
            release.Set();
        }

        async void Work()
        {
            log.Add("before");
            // A thread of its own, not the pool's: this test runs on one of xunit's pool threads and blocks it, so work
            // it queued to the pool would wait in that thread's local queue until another worker stole it, which a busy
            // test run can delay by seconds (Docs/Engine-Plan.md, Progress, "The flaky ScriptApiTests: work stuck in a
            // blocked pool thread's queue").
            await Task.Factory.StartNew(() => release.Wait(), CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            log.Add($"after on loop {Environment.CurrentManagedThreadId == loopThread}");
            continued.Set();
        }
    }

    [Fact]
    public void ListenersKeepWhatTheyListenToAlive()
    {
        // As in Blink: a media query list with a listener and an observer with targets live on
        // when game code drops them (a garbage collection must not lose their events).
        var document = Document("""<div id="a" style="width: 50%; height: 10px"></div>""");
        var log = new List<string>();
        document.RunScript(() =>
        {
            document.MatchMedia("(max-width: 350px)").AddChangeListener(e => log.Add("change " + e.Matches));
            new UiResizeObserver(document, entries => log.Add("resize " + entries[0].ContentRect.Width)).Observe(document.GetElementById("a")!);
        });
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        document.Update(1 / 60.0);
        document.SetViewport(300, 300);
        GC.Collect();
        GC.WaitForPendingFinalizers();
        document.Update(2 / 60.0);
        Assert.Equal(["resize 192", "change True", "resize 142"], log);
    }

    [Fact]
    public void DelayCompletesAtItsTimer()
    {
        var document = Document("");
        var log = new List<string>();
        document.RunScript(() => Wait());
        for (var frame = 1; frame <= 10; frame++)
            document.Update(frame / 60.0);
        Assert.Equal(["waited 100"], log);

        async void Wait()
        {
            var start = document.PerformanceNow;
            await document.Delay(100);
            log.Add($"waited {Math.Round(document.PerformanceNow - start)}");
        }
    }
}

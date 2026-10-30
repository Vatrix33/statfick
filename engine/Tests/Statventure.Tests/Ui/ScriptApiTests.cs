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
        var started = new ManualResetEventSlim();
        document.RunScript(() => Work());
        started.Wait(TimeSpan.FromSeconds(10));
        // The continuation was posted from the pool: it waits for the loop's next turn.
        for (var i = 0; i < 200 && log.Count < 2; i++)
        {
            Thread.Sleep(5);
            document.Update(document.Animations.Time + 1 / 60.0);
        }
        Assert.Equal(["before", $"after on loop {true}"], log);

        async void Work()
        {
            log.Add("before");
            await Task.Run(() => started.Set());
            log.Add($"after on loop {Environment.CurrentManagedThreadId == loopThread}");
        }
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

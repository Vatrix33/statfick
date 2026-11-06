using Statventure.Core;
using Statventure.Graphics;
using Statventure.Platform;
using Statventure.Tests.Storage;

namespace Statventure.Tests.Core;

/// <summary>
/// A close the application can refuse (Docs/Editor.md, step 3): when the window's close is requested the loop asks
/// <see cref="Application.ConfirmClose"/>, before the next frame and, while the window is minimized, after the
/// update; true (the default) ends the loop where it always ended, false keeps it running with the request
/// cleared, so the next close asks again; <see cref="Application.Quit"/> doesn't ask. A real application in a hidden
/// window (nothing is shown), counted as minimized so the loop presents nothing (a hidden window can't be minimized
/// on Wayland, and presenting to one isn't the same on every window system); a close is requested as the player's
/// is, by GLFW's close flag. The request before the next frame is made in Load, which the same check follows. In
/// the "Window" collection: GLFW is one for the process.
/// </summary>
[Collection("Window")]
public class ApplicationCloseTests
{
    /// <summary>More updates than any test needs: past it the application quits, so a loop that wouldn't end fails the test instead of hanging.</summary>
    const int Enough = 40;

    /// <summary>
    /// An application in a hidden, minimized window that writes down what happens: "load", "update n" (n counting
    /// the updates from 0), "close?" each time it is asked and "unload". Updates ask to be woken at once, so the
    /// minimized loop never sleeps.
    /// </summary>
    sealed class Recorder(StorageFolder folder) : Application(new ApplicationOptions
    {
        Window = new WindowOptions { Title = "Statventure close test", Width = 160, Height = 100, Visible = false },
        Audio = false,
        GameFolder = folder["player"],
    })
    {
        public readonly List<string> Seen = [];
        int _updates;

        /// <summary>The answers <see cref="ConfirmClose"/> gives, in turn (none: the default's).</summary>
        public Queue<bool>? Answers { get; init; }

        public bool CloseInLoad { get; init; }

        /// <summary>Each update with its number; may request a close or quit.</summary>
        public Action<Recorder, int>? OnUpdate { get; init; }

        public bool CloseRequestedAtUpdate { get; private set; }

        protected override void Load()
        {
            Window.MinimizedForTests = true;
            Seen.Add("load");
            if (CloseInLoad)
                Window.CloseRequested = true;
        }

        /// <summary>The wake-up each update asks for, in seconds (by default at once).</summary>
        public double WakeUp { get; set; }

        protected override void Update(in GameTime time)
        {
            var n = _updates++;
            Seen.Add($"update {n}");
            if (n == 1)
                CloseRequestedAtUpdate = Window.CloseRequested;
            OnUpdate?.Invoke(this, n);
            Window.RequestWakeUp(WakeUp);
            if (n >= Enough)
                Quit();
        }

        protected override void Render(Frame frame, GpuImage target, in GameTime time) =>
            throw new InvalidOperationException("a minimized window has no frames");

        protected override bool ConfirmClose()
        {
            Seen.Add("close?");
            return Answers is null ? base.ConfirmClose() : Answers.Dequeue();
        }

        protected override void Unload() => Seen.Add("unload");

        /// <summary>A close requested at the given updates, as the player's (GLFW's close flag).</summary>
        public static Action<Recorder, int> CloseAt(params int[] updates) => (a, n) =>
        {
            if (updates.Contains(n))
                a.Window.CloseRequested = true;
        };
    }

    static string[] Updates(int from, int to) => [.. Enumerable.Range(from, to - from + 1).Select(n => $"update {n}")];

    [Fact]
    public void ByDefaultAClosedWindowEndsTheLoopWhereItAlwaysDid()
    {
        if (!TestApplication.HasDisplay)
            return;
        using var folder = new StorageFolder();
        // While minimized: after the update in which it came.
        var minimized = new Recorder(folder) { OnUpdate = Recorder.CloseAt(2) };
        minimized.Run();
        Assert.Equal(["load", .. Updates(0, 2), "close?", "unload"], minimized.Seen);
        // Before the next frame: requested before the first, no frame at all.
        var first = new Recorder(folder) { CloseInLoad = true };
        first.Run();
        Assert.Equal(["load", "close?", "unload"], first.Seen);
    }

    [Fact]
    public void WhileMinimizedACloseEndsTheLoopBeforeItSleeps()
    {
        if (!TestApplication.HasDisplay)
            return;
        using var folder = new StorageFolder();
        // The update asks to sleep a minute: the close that came with it ends the loop at once, as it always did, not
        // at the next wake-up. A watchdog quits and wakes the loop after 20 s, so a loop that slept (or that a refused
        // close keeps going) fails the test, not hangs.
        var application = new Recorder(folder)
        {
            OnUpdate = (a, n) =>
            {
                if (n != 1)
                    return;
                a.Window.CloseRequested = true;
                a.WakeUp = 60;
            },
        };
        var woken = false;
        using var watchdog = new CancellationTokenSource();
        _ = Task.Delay(TimeSpan.FromSeconds(20), watchdog.Token).ContinueWith(_ =>
        {
            woken = true;
            application.Quit();
            Window.PostEmptyEvent();
        }, TaskContinuationOptions.OnlyOnRanToCompletion);
        application.Run();
        watchdog.Cancel();
        Assert.Equal(["load", .. Updates(0, 1), "close?", "unload"], application.Seen);
        Assert.False(woken, "the loop slept before it ended");
    }

    [Fact]
    public void ARefusedCloseKeepsTheLoopRunningAndTheNextCloseEndsIt()
    {
        if (!TestApplication.HasDisplay)
            return;
        using var folder = new StorageFolder();
        // While minimized: refused after update 2, the updates go on, the request cleared; accepted after update 5.
        var minimized = new Recorder(folder) { Answers = new([false, true]), OnUpdate = Recorder.CloseAt(2, 5) };
        minimized.Run();
        Assert.Equal(["load", .. Updates(0, 2), "close?", .. Updates(3, 5), "close?", "unload"], minimized.Seen);
        // Before the next frame: refused before the first, the request cleared by update 1; accepted after update 3.
        var first = new Recorder(folder) { Answers = new([false, true]), CloseInLoad = true, OnUpdate = Recorder.CloseAt(3) };
        first.Run();
        Assert.Equal(["load", "close?", .. Updates(0, 3), "close?", "unload"], first.Seen);
        Assert.False(first.CloseRequestedAtUpdate);
    }

    [Fact]
    public void EachRequestIsAskedOnce()
    {
        if (!TestApplication.HasDisplay)
            return;
        using var folder = new StorageFolder();
        // Three requests refused, each asked once, the loop running on between them until it quits by itself.
        var application = new Recorder(folder)
        {
            Answers = new([false, false, false]),
            CloseInLoad = true,
            OnUpdate = (a, n) =>
            {
                Recorder.CloseAt(1, 4)(a, n);
                if (n == 6)
                    a.Quit();
            },
        };
        application.Run();
        Assert.Equal(["load", "close?", .. Updates(0, 1), "close?", .. Updates(2, 4), "close?", .. Updates(5, 6), "unload"], application.Seen);
    }

    [Fact]
    public void QuitEndsTheLoopWithoutAsking()
    {
        if (!TestApplication.HasDisplay)
            return;
        using var folder = new StorageFolder();
        // The editor's way after a refusal: the player chose to close, so it quits.
        var application = new Recorder(folder)
        {
            Answers = new([false]),
            OnUpdate = (a, n) =>
            {
                Recorder.CloseAt(1)(a, n);
                if (n == 3)
                    a.Quit();
            },
        };
        application.Run();
        Assert.Equal(["load", .. Updates(0, 1), "close?", .. Updates(2, 3), "unload"], application.Seen);
        // Quit with a close requested too: no question.
        var both = new Recorder(folder)
        {
            Answers = new([]),
            OnUpdate = (a, n) =>
            {
                if (n != 1)
                    return;
                a.Window.CloseRequested = true;
                a.Quit();
            },
        };
        both.Run();
        Assert.Equal(["load", .. Updates(0, 1), "unload"], both.Seen);
    }

    [Fact]
    public void QuitWithACloseRequestedEndsTheLoopBeforeTheNextFrameWithoutAsking()
    {
        if (!TestApplication.HasDisplay)
            return;
        using var folder = new StorageFolder();
        // The loop sleeps a minute after update 1 (its check after the update done); meanwhile another thread requests
        // the close, quits and wakes it, so the two meet at the check before the next frame, which ends it unasked.
        Task? meanwhile = null;
        var application = new Recorder(folder)
        {
            Answers = new([]),
            OnUpdate = (a, n) =>
            {
                if (n != 1)
                    return;
                a.WakeUp = 60;
                meanwhile = Task.Run(async () =>
                {
                    await Task.Delay(300);
                    a.Window.CloseRequested = true;
                    a.Quit();
                    Window.PostEmptyEvent();
                });
            },
        };
        application.Run();
        meanwhile!.Wait();
        Assert.Equal(["load", .. Updates(0, 1), "unload"], application.Seen);
    }
}

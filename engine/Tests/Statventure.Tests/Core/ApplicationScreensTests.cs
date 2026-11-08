using System.Diagnostics;
using Statventure.Core;
using Statventure.Editor;
using Statventure.Editor.Session;
using Statventure.Graphics;
using Statventure.Imaging;
using Statventure.Platform;
using Statventure.Storage;
using Statventure.Tests.Editor;
using Statventure.Tests.Records;
using Statventure.Tests.Storage;
using Statventure.UI;
using Statventure.UI.Dom;

namespace Statventure.Tests.Core;

/// <summary>
/// The application's own screens (Docs/Packaging.md, "The BRUTAL logo and the credits" and "Crashes"; Q22–Q24, Q29; M12
/// step 5). The BRUTAL splash: on a program's run (a log name) with the option on, once per start, once the window and
/// device are made and before the sound and <c>Load</c>, drawn in the application's window for its 2 s (or until a key or
/// a click after its first second); a close during it ends the run without loading; a crash during it (its own, or
/// another thread's, taken there) is the start's; never shown by tests' applications (no log name) or with the option off,
/// nor before the crash window. The crash window: after the teardown (the pending saves written), for a crash on the main
/// thread and on another thread, showing the error and the crash file, Copy filling the clipboard, "Open logs folder"
/// starting the file manager on <c>logs/</c>, Close ending the program with <see cref="Application.CrashExitCode"/>; a
/// window that fails gives the terminal's message (Q24); a scripted run's window saves its first frame and closes; no
/// window without a log name. Real applications in hidden windows counted as minimized (presents hang under wlroots after
/// the first), with immediate wake-ups where they run many updates; the program's end caught. In the non-parallel
/// "Storage log" collection (the log, and the running application another thread's crash goes to, are one per process).
/// </summary>
[Collection("Storage log")]
public class ApplicationScreensTests : IDisposable
{
    readonly LogLevel _level = Log.Level;

    public ApplicationScreensTests() => Log.Level = LogLevel.Info;

    public void Dispose() => Log.Level = _level;

    static readonly TimeSpan Patience = TimeSpan.FromSeconds(10);

    /// <summary>
    /// A program's run in a small hidden window (a log name, the splash and the crash window as the option and the
    /// test say), its Load, Update and Unload the test's (it quits after the update unless <see cref="OnUpdate"/> asks to
    /// go on), its splash and crash window frames handed to the test, writing down what happened in order
    /// (<see cref="Events"/>), what went to the terminal, how the program would have ended and the file manager started.
    /// </summary>
    sealed class Screens : Application
    {
        public Screens(StorageFolder folder, bool splash = true, string? logName = "test-program") : base(new ApplicationOptions
        {
            Window = new WindowOptions { Title = "Screens test", Width = 160, Height = 100, Visible = false },
            Audio = false,
            GameFolder = folder["player"],
            LogName = logName,
            Splash = splash,
        })
        {
            Logs = folder["player/logs"];
            Terminal = Told.Add;
            Exit = Exits.Add;
            Launcher = start =>
            {
                Started.Add(start);
                return true;
            };
            EachSplashFrame = splash =>
            {
                Events.Add("splash");
                Splashes.Add(splash);
                if (OnSplash is not null)
                    OnSplash(this, splash);
                else if (Splashes.Count > 1)
                    Window.MinimizedForTests = true; // (one frame presented: a test expecting none fails rather than hangs)
            };
            EachCrashWindowFrame = window =>
            {
                Events.Add("crash window");
                CrashWindows.Add(window);
                if (OnCrashWindow is not null)
                    OnCrashWindow(this, window);
                else if (CrashWindows.Count > 1)
                {
                    // A window the test doesn't drive presents its first frame only (a hidden window's second present
                    // hangs under wlroots), then closes: a test expecting none fails rather than hangs.
                    window.Window!.MinimizedForTests = true;
                    CrashWindowTests.EndAnyway(window.Window, window.Close);
                }
            };
        }

        public string Logs { get; }
        public List<string> Events { get; } = [];
        public List<string> Told { get; } = [];
        public List<int> Exits { get; } = [];
        public List<ProcessStartInfo> Started { get; } = [];
        public List<Splash> Splashes { get; } = [];
        public List<CrashWindow> CrashWindows { get; } = [];
        public Action<Screens, Splash>? OnSplash { get; init; }
        public Action<Screens, CrashWindow>? OnCrashWindow { get; init; }
        public Action<Screens>? OnLoad { get; init; }
        public Func<Screens, long, bool>? OnUpdate { get; init; }

        protected override void Load()
        {
            Events.Add("load");
            OnLoad?.Invoke(this);
        }

        int _updates;

        protected override void Update(in GameTime time)
        {
            Events.Add("update");
            if (OnUpdate?.Invoke(this, _updates++) != true)
                Quit();
        }

        protected override void Render(Frame frame, GpuImage target, in GameTime time) { }

        protected override void Unload() => Events.Add("unload");

        public override void Dispose()
        {
            if (Gpu is not null)
                Events.Add("teardown");
            base.Dispose();
        }

        public string[] CrashFiles() => Directory.Exists(Logs) ? Directory.GetFiles(Logs, "crash-*.txt") : [];

        public string RunLog() => File.ReadAllText(Assert.Single(Directory.GetFiles(Logs, "test-program-*.log")));
    }

    /// <summary>The events with repeats of one kind in a row counted as one ("splash" for a splash of several frames).</summary>
    static List<string> Order(List<string> events) => [.. events.Where((e, i) => i == 0 || events[i - 1] != e)];

    // ---------- The splash ----------

    [Fact]
    public void AProgramsRunShowsTheSplashOnceBeforeLoadForItsLength()
    {
        if (!TestApplication.HasDisplay)
            return;
        using var folder = new StorageFolder();
        string? atLoad = null;
        using var application = new Screens(folder)
        {
            // Its first frame drawn (and captured); then counted as minimized, so no other frame is presented.
            OnSplash = (a, _) =>
            {
                if (a.Splashes.Count == 2)
                    a.Window.MinimizedForTests = true;
            },
            OnLoad = a =>
            {
                var splash = a.Splashes[^1];
                atLoad = $"finished {splash.Finished}, skipped {splash.Skipped}, elapsed {splash.Elapsed >= Splash.Length}";
            },
        };
        Assert.True(application.ShowsSplash);
        var capture = application.CaptureNextFrame(folder["splash.png"]);
        var logged = TestApplication.CaptureLog("Application", application.Run);
        // Once, before Load: one splash, its frames, then the application's own.
        Assert.Equal(["splash", "load", "update", "unload", "teardown"], Order(application.Events));
        Assert.Single(application.Splashes.Distinct());
        Assert.True(application.Splashes.Count >= 2);
        Assert.Equal("finished True, skipped False, elapsed True", atLoad);
        Assert.Contains("Info: the splash shown (2 s)", logged);
        Assert.Null(application.Crash);
        Assert.Empty(application.Exits);
        // The first frame was the splash's: the logo centred on the dark ground.
        Assert.True(capture.IsCompleted);
        var image = ImageFile.Load(folder["splash.png"]);
        Assert.Equal((0x1a, 0x1c, 0x1a), Pixel(image, 2, 2));
        Assert.Equal((0x1a, 0x1c, 0x1a), Pixel(image, image.Width - 3, image.Height - 3));
        // The logo, 160 px wide at 1x in a 160 x 100 window, its yellow square at its left and its white letters.
        var middle = image.Height / 2;
        var row = Enumerable.Range(0, image.Width).Select(x => Pixel(image, x, middle)).ToList();
        Assert.Contains(row, p => p.R > 200 && p.G > 150 && p.B < 80);
        Assert.Contains(row, p => p.R > 230 && p.G > 230 && p.B > 230);
    }

    static (int R, int G, int B) Pixel(RgbaImage image, int x, int y)
    {
        var i = (y * image.Width + x) * 4;
        return (image.Pixels[i], image.Pixels[i + 1], image.Pixels[i + 2]);
    }

    [Fact]
    public void NoSplashWithTheOptionOffOrOnATestsApplication()
    {
        if (!TestApplication.HasDisplay)
            return;
        using var folder = new StorageFolder();
        using (var off = new Screens(folder, splash: false))
        {
            Assert.False(off.ShowsSplash);
            off.Run();
            Assert.Equal(["load", "update", "unload", "teardown"], off.Events);
        }
        using (var unnamed = new Screens(folder, logName: null))
        {
            Assert.False(unnamed.ShowsSplash);
            unnamed.Run();
            Assert.Equal(["load", "update", "unload", "teardown"], unnamed.Events);
        }
        Assert.True(new ApplicationOptions().Splash);
        // The editor: its program's run shows it (its log name), its tests' construction doesn't.
        var game = GameData.Repo(EditorHarness.EngineFolder);
        using (var program = new EditorApplication(game, folder.Root))
            Assert.True(program.ShowsSplash);
        using (var tests = new EditorApplication(game, folder.Root, visible: false))
            Assert.False(tests.ShowsSplash);
    }

    [Fact]
    public void AKeyOrAClickSkipsTheSplashOnlyAfterItsFirstSecond()
    {
        if (!TestApplication.HasDisplay)
            return;
        using var folder = new StorageFolder();
        double? keyAt = null, atLoad = null;
        var clickedEarly = false;
        using var application = new Screens(folder)
        {
            OnSplash = (a, splash) =>
            {
                a.Window.MinimizedForTests = true;
                a.Window.RequestWakeUp(0);
                if (!clickedEarly)
                {
                    // A click in its first second: nothing.
                    a.Window.Input.Push(new InputEvent(InputEventKind.MouseDown, Position: new(80, 50)));
                    a.Window.Input.Push(new InputEvent(InputEventKind.MouseUp, Position: new(80, 50)));
                    clickedEarly = true;
                }
                else if (keyAt is null && splash.Elapsed >= 1.05)
                {
                    a.Window.Input.Push(new InputEvent(InputEventKind.KeyDown, Key: Key.Escape));
                    keyAt = splash.Elapsed;
                }
            },
            OnLoad = a => atLoad = a.Splashes[^1].Elapsed,
        };
        var logged = TestApplication.CaptureLog("Application", application.Run);
        var last = application.Splashes[^1];
        Assert.True(last.Skipped, "the key didn't skip the splash");
        Assert.NotNull(keyAt);
        Assert.InRange(atLoad!.Value, keyAt.Value, keyAt.Value + 0.5);
        Assert.True(atLoad < Splash.Length, "the splash lasted its whole length");
        Assert.Contains(logged, line => line.StartsWith("Info: the splash skipped at 1."));
        Assert.Equal(["splash", "load", "update", "unload", "teardown"], Order(application.Events));
    }

    [Fact]
    public void ClosingTheWindowDuringTheSplashEndsTheRunWithoutLoading()
    {
        if (!TestApplication.HasDisplay)
            return;
        using var folder = new StorageFolder();
        using var application = new Screens(folder)
        {
            OnSplash = (a, _) =>
            {
                a.Window.MinimizedForTests = true;
                a.Window.CloseRequested = true;
            },
        };
        var logged = TestApplication.CaptureLog("Application", application.Run);
        Assert.Equal(["splash", "teardown"], application.Events);
        Assert.Single(logged, line => line.StartsWith("Info: the window closed during the splash, at 0.0 s: the run ends without loading"));
        Assert.Null(application.Crash);
        Assert.Empty(application.Exits);
        Assert.Empty(application.CrashFiles());
    }

    [Fact]
    public void ACrashDuringTheSplashIsTheStartsWithNoSplashBeforeTheCrashWindow()
    {
        if (!TestApplication.HasDisplay)
            return;
        using var folder = new StorageFolder();
        using var application = new Screens(folder)
        {
            OnSplash = (a, _) =>
            {
                a.Window.MinimizedForTests = true;
                throw new InvalidOperationException("a splash that fails");
            },
            OnCrashWindow = (a, window) =>
            {
                window.Window!.MinimizedForTests = true;
                CrashWindowTests.EndAnyway(window.Window, window.Close);
            },
        };
        var thrown = Assert.ThrowsAny<Exception>(application.Run);
        Assert.Equal("a splash that fails", thrown.Message);
        Assert.Equal("on the main thread", application.Crash!.Where);
        Assert.Single(application.CrashFiles());
        // Nothing loaded; the crash window after the teardown, and no splash again before it.
        Assert.Equal(["splash", "teardown", "crash window"], application.Events);
        Assert.Equal([Application.CrashExitCode], application.Exits);
        Assert.Empty(application.Told);
    }

    [Fact]
    public void AnotherThreadsCrashDuringTheSplashIsTakenThere()
    {
        if (!TestApplication.HasDisplay)
            return;
        using var folder = new StorageFolder();
        Thread? thread = null;
        using var application = new Screens(folder)
        {
            CrashAnswerTime = TimeSpan.FromSeconds(5),
            OnSplash = (a, _) =>
            {
                a.Window.MinimizedForTests = true;
                if (thread is null)
                {
                    thread = new Thread(() => throw new InvalidOperationException("a thread that fails during the splash")) { Name = "Splash crash test", IsBackground = true };
                    thread.Start();
                }
            },
            OnCrashWindow = (_, window) =>
            {
                window.Window!.MinimizedForTests = true;
                CrashWindowTests.EndAnyway(window.Window, window.Close);
            },
        };
        var thrown = Assert.ThrowsAny<Exception>(application.Run);
        Assert.Equal("a thread that fails during the splash", thrown.Message);
        Assert.Equal("on the thread \"Splash crash test\"", application.Crash!.Where);
        Assert.True(thread!.Join(Patience), "the crashed thread wasn't let go");
        // Taken by the splash (which sleeps until its end or an event: the crash's wakes it), long before its 2 s.
        Assert.True(application.Splashes[^1].Elapsed < 1.5, "the splash went on");
        Assert.Equal(["splash", "teardown", "crash window"], Order(application.Events));
        Assert.Equal([Application.CrashExitCode], application.Exits);
    }

    // ---------- The crash window ----------

    [Theory]
    [InlineData("main")]
    [InlineData("thread")]
    public void ACrashShowsTheCrashWindowAfterTheTeardownAndItsButtonsWork(string source)
    {
        if (!TestApplication.HasDisplay)
            return;
        using var folder = new StorageFolder();
        var deadline = DateTime.UtcNow + Patience;
        var clipboard = new MemoryClipboard();
        string? seen = null;
        var frames = 0;
        using var application = new Screens(folder, splash: false)
        {
            OnLoad = a =>
            {
                a.Window.MinimizedForTests = true;
                // A save the teardown writes (S9-B), held a while by the writer: written before the crash window.
                a.Storage.Writer.BeforeWork = _ => Thread.Sleep(300);
                a.Storage.Saves.Save("slot", new Part { Name = source });
            },
            OnUpdate = (a, n) =>
            {
                a.Window.RequestWakeUp(0);
                if (n == 0)
                {
                    if (source == "main")
                        throw new InvalidOperationException("an update that fails");
                    new Thread(() => throw new InvalidOperationException("a thread that fails")) { Name = "Crash window test", IsBackground = true }.Start();
                }
                return DateTime.UtcNow < deadline;
            },
            OnCrashWindow = (a, window) =>
            {
                window.Window!.MinimizedForTests = true;
                window.Window.RequestWakeUp(0);
                var document = window.Document;
                switch (++frames)
                {
                    case 1:
                        var field = window.PathField!;
                        seen = string.Join(" | ",
                            document.QuerySelector("h1")!.TextContent,
                            document.QuerySelector(".error")!.TextContent,
                            field.Value,
                            $"focused {document.FocusedElement == field}, selected {field.SelectionStart}-{field.SelectionEnd}",
                            $"title {window.Window.Title}, visible {window.Window.Native.Visible}",
                            $"torn down {a.Window is null}, saved {File.Exists(folder["player/saves/slot.dat"])}, log open {Log.FilePath is not null}");
                        // The host gave the document the system's clipboard; a stand-in here.
                        document.Clipboard = clipboard;
                        document.RunScript(() => document.QuerySelector(".copy")!.Click());
                        document.RunScript(() => document.QuerySelector(".open-logs")!.Click());
                        break;
                    case 2:
                        document.RunScript(() => document.QuerySelector(".close")!.Click());
                        break;
                    case > 10:
                        CrashWindowTests.EndAnyway(window.Window, window.Close);
                        break;
                }
            },
        };
        var thrown = Assert.ThrowsAny<Exception>(application.Run);
        var crash = application.Crash!;
        Assert.Same(thrown, crash.Exception);
        var file = Assert.Single(application.CrashFiles());
        Assert.Equal(file, crash.File);
        Assert.Equal(string.Join(" | ",
            "Screens test stopped because of an error",
            $"InvalidOperationException: {thrown.Message}",
            file,
            $"focused True, selected 0-{file.Length}",
            "title Screens test, visible False",
            "torn down True, saved True, log open True"), seen);
        // After the teardown; its buttons: the path copied, the file manager on logs/, Close ending the program.
        Assert.Equal(["load", "update", "teardown", "crash window"], Order(application.Events));
        Assert.Equal(2, frames);
        Assert.Equal(file, clipboard.ReadText());
        Assert.Equal(1, Application.CrashExitCode);
        var start = Assert.Single(application.Started);
        Assert.Equal(GameStorage.FileManagerStart(OperatingSystem.IsWindows() ? StorageSystem.Windows : StorageSystem.Linux, application.Logs).ArgumentList, start.ArgumentList);
        Assert.Contains(application.Logs, start.ArgumentList.Concat([start.FileName]));
        Assert.Equal([Application.CrashExitCode], application.Exits);
        // The window, not the terminal; the log kept open for the window's lines, closed at the end.
        Assert.Empty(application.Told);
        var log = application.RunLog();
        Assert.Contains("[Application] the crash window opens", log);
        Assert.Contains("[Application] the crash window closed", log);
        Assert.True(log.IndexOf("crash file ", StringComparison.Ordinal) < log.IndexOf("the crash window opens", StringComparison.Ordinal));
        Assert.Null(Log.FilePath);
    }

    [Fact]
    public void ACrashWindowThatFailsGivesTheTerminalsMessage()
    {
        if (!TestApplication.HasDisplay)
            return;
        using var folder = new StorageFolder();
        using var application = new Screens(folder, splash: false)
        {
            OnLoad = a => a.Window.MinimizedForTests = true,
            OnUpdate = (_, _) => throw new InvalidOperationException("an update that fails"),
            // As a window or device that can't be made throws.
            OnCrashWindow = (_, _) => throw new InvalidOperationException("no window here"),
        };
        Assert.ThrowsAny<Exception>(application.Run);
        var crash = application.Crash!;
        Assert.Equal([$"Screens test stopped because of an error.\n{crash.Message}\nThe details are in: {crash.File}"], application.Told);
        Assert.Equal([Application.CrashExitCode], application.Exits);
        Assert.Contains("ERROR [Application] the crash window couldn't be shown: System.InvalidOperationException: no window here", application.RunLog());
    }

    [Fact]
    public void NoCrashWindowOnATestsApplication()
    {
        if (!TestApplication.HasDisplay)
            return;
        using var folder = new StorageFolder();
        using var application = new Screens(folder, logName: null)
        {
            OnLoad = a => a.Window.MinimizedForTests = true,
            OnUpdate = (_, _) => throw new InvalidOperationException("an update that fails"),
        };
        Assert.Equal("an update that fails", Assert.ThrowsAny<Exception>(application.Run).Message);
        Assert.DoesNotContain("crash window", application.Events);
        Assert.Empty(application.Exits);
        Assert.Empty(application.Told);
    }

    [Fact]
    public void AScriptedRunsCrashWindowSavesItsFirstFrameAndCloses()
    {
        if (!TestApplication.HasDisplay)
            return;
        using var folder = new StorageFolder();
        using var application = new Screens(folder, splash: false)
        {
            OnLoad = a => a.Window.MinimizedForTests = true,
            OnUpdate = (_, _) => throw new InvalidOperationException("an update that fails"),
        };
        application.CrashCapture = folder["crash.png"];
        Assert.ThrowsAny<Exception>(application.Run);
        // One frame (presented once: a hidden window's second present would hang), saved, then closed by itself.
        Assert.Equal(["load", "update", "teardown", "crash window"], application.Events);
        Assert.Equal([Application.CrashExitCode], application.Exits);
        var image = ImageFile.Load(folder["crash.png"]);
        Assert.Equal(EngineWindows.Width * image.Height, EngineWindows.Height * image.Width);
        var scale = image.Width / (double)EngineWindows.Width;
        int At(double css) => (int)(css * scale);
        Assert.Equal((0x1a, 0x1c, 0x1a), Pixel(image, 2, 2));
        // Close, the main button, in the ember at the foot on the right (its padding, 8 px in from its right edge).
        Assert.Equal((0xd7, 0x7a, 0x50), Pixel(image, At(EngineWindows.Width - 28), At(EngineWindows.Height - 28)));
        // The heading's text on the first line.
        Assert.Contains(Enumerable.Range(At(20), At(200)).Select(x => Pixel(image, x, At(28))), p => p.R > 200);
    }
}

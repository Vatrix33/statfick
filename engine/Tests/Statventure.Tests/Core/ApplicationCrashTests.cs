using Statventure.Audio;
using Statventure.Core;
using Statventure.Editor;
using Statventure.Editor.Session;
using Statventure.Graphics;
using Statventure.Platform;
using Statventure.Storage;
using Statventure.Tests.Editor;
using Statventure.Tests.Records;
using Statventure.Tests.Storage;

namespace Statventure.Tests.Core;

/// <summary>
/// The run's log and crash files (Docs/Packaging.md, "Logs" and "Crashes"; M12-8, M12-10, Q3, Q21, Q24): with a log name,
/// <see cref="Application.Run"/> writes the run's log in the player's game folder's <c>logs/</c>; a crash on the main thread
/// (the start, <c>Load</c>, <c>Update</c>, <c>Render</c>, <c>Unload</c>, the teardown) or on any other thread (a thread of
/// its own, the thread pool, the audio's room thread) writes its crash file first, then the application is torn down in
/// order (the pending saves written, S9-B), its message goes to the terminal and the exception leaves <c>Run</c>. Real
/// applications in hidden windows, each with a game folder of the test's own. In the non-parallel "Storage log"
/// collection: the log, and the running application a crash on another thread is handed to, are one for the process.
/// </summary>
[Collection("Storage log")]
public class ApplicationCrashTests : IDisposable
{
    readonly LogLevel _level = Log.Level;

    /// <summary>The log at its programs' level, Info, while the test runs (the GPU tests' fixture leaves it at Warning).</summary>
    public ApplicationCrashTests() => Log.Level = LogLevel.Info;

    public void Dispose() => Log.Level = _level;

    /// <summary>Long enough for anything a test waits for; a test that waits that long fails.</summary>
    static readonly TimeSpan Patience = TimeSpan.FromSeconds(10);

    /// <summary>
    /// An application in a small hidden window whose Load, Update, Render and Unload are the test's (it quits after the
    /// update unless <see cref="OnUpdate"/> asks to go on), which writes down the logs folder's files when its teardown
    /// starts, then runs <see cref="OnTeardown"/> after it, and catches what goes to the terminal. A test that runs
    /// many updates has it <see cref="Minimized"/> with an immediate <see cref="WakeUp"/>: it then presents no frames
    /// (a hidden window's presents aren't paced by every compositor, and wlroots hangs after a few; engine/CLAUDE.md)
    /// and its loop never sleeps.
    /// </summary>
    sealed class Crasher : Application
    {
        public Crasher(StorageFolder folder, string? logName = "test-program", string? developmentFolder = null) : base(new ApplicationOptions
        {
            Window = new WindowOptions { Title = "Crash test", Width = 160, Height = 100, Visible = false },
            Audio = false,
            GameFolder = folder["player"],
            DevelopmentFolder = developmentFolder,
            LogName = logName,
        })
        {
            Logs = folder["player/logs"];
            Terminal = Told.Add;
        }

        public string Logs { get; }
        public List<string> Told { get; } = [];
        public Action<Crasher>? OnLoad { get; init; }
        public Func<Crasher, long, bool>? OnUpdate { get; init; }
        public Action<Crasher>? OnRender { get; init; }
        public Action<Crasher>? OnUnload { get; init; }
        public Action<Crasher>? OnTeardown { get; init; }

        /// <summary>Counted as minimized from <c>Load</c> on: updates, no frames.</summary>
        public bool Minimized { get; init; }

        /// <summary>The wake-up each update asks for, in seconds (null: none, so a minimized loop sleeps until an event).</summary>
        public double? WakeUp { get; init; }

        /// <summary>The logs folder's file names when the teardown started (null: it didn't).</summary>
        public string[]? AtTeardown { get; private set; }

        protected override void Load()
        {
            Window.MinimizedForTests = Minimized;
            OnLoad?.Invoke(this);
        }

        int _updates;

        /// <summary>Each update with its number (the frame number stays 0 while minimized).</summary>
        protected override void Update(in GameTime time)
        {
            if (WakeUp is { } wakeUp)
                Window.RequestWakeUp(wakeUp);
            if (OnUpdate?.Invoke(this, _updates++) != true)
                Quit();
        }

        protected override void Render(Frame frame, GpuImage target, in GameTime time) => OnRender?.Invoke(this);

        protected override void Unload() => OnUnload?.Invoke(this);

        public override void Dispose()
        {
            if (AtTeardown is not null)
            {
                base.Dispose();
                return;
            }
            AtTeardown = Directory.Exists(Logs) ? [.. Directory.GetFiles(Logs).Select(file => Path.GetFileName(file)!)] : [];
            base.Dispose();
            OnTeardown?.Invoke(this);
        }

        public string[] CrashFiles() => Directory.Exists(Logs) ? Directory.GetFiles(Logs, "crash-*.txt") : [];
    }

    static string Stamp(DateTime time) => time.ToString("yyyy-MM-dd_HH-mm-ss", System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>A line of the log without its time and level.</summary>
    static string Text(string line) => line.IndexOf('[') is >= 0 and var at ? line[at..] : line;

    /// <summary>The first line of the test program's logs and crash files: its name, then the version and commit of the tests' own program file (a dev build's).</summary>
    static string ProgramLine => LogFiles.ProgramLine("test-program", ProgramInfo.Of(typeof(ApplicationCrashTests).Assembly));

    [Fact]
    public void TheRunsLogIsInThePlayersFolderNamedForTheProgramAndItsStartWithItsFirstLines()
    {
        if (!TestApplication.HasDisplay)
            return;
        using var folder = new StorageFolder();
        using var application = new Crasher(folder);
        var before = DateTime.Now.AddSeconds(-1);
        application.Run();
        var after = DateTime.Now;
        var log = Assert.Single(Directory.GetFiles(folder["player/logs"]));
        var name = Path.GetFileName(log);
        Assert.Matches("^test-program-[0-9]{4}-[0-9]{2}-[0-9]{2}_[0-9]{2}-[0-9]{2}-[0-9]{2}\\.log$", name);
        var stamp = name["test-program-".Length..^".log".Length];
        Assert.True(string.CompareOrdinal(Stamp(before), stamp) <= 0 && string.CompareOrdinal(stamp, Stamp(after)) <= 0, $"{stamp} isn't the run's start");
        var lines = File.ReadAllLines(log);
        Assert.Equal("[Program] " + ProgramLine, Text(lines[0]));
        Assert.StartsWith("test-program dev, commit ", ProgramLine);
        Assert.StartsWith("[Program] started ", Text(lines[1]));
        var started = DateTimeOffset.ParseExact(Text(lines[1])["[Program] started ".Length..], "yyyy-MM-dd HH:mm:ss zzz", System.Globalization.CultureInfo.InvariantCulture);
        Assert.InRange(started.LocalDateTime, before, after);
        Assert.Equal(DateTimeOffset.Now.Offset, started.Offset);
        Assert.Equal("[Program] " + LogFiles.PlatformLine(), Text(lines[2]));
        // Then what the application logged before: the game folder, the window, the device.
        Assert.Equal("[Storage] game folder " + folder["player"], Text(lines[3]));
        Assert.Contains(lines, line => Text(line).StartsWith("[Platform] window "));
        Assert.Contains(lines, line => Text(line).StartsWith("[Graphics] GPU "));
        // Closed when the run ended; no crash, nothing on the terminal.
        Assert.Null(Log.FilePath);
        Assert.Null(application.Crash);
        Assert.Empty(application.Told);
        Assert.Equal(["logs"], Directory.GetFileSystemEntries(folder["player"]).Select(Path.GetFileName));
    }

    [Fact]
    public void TestsApplicationsWriteNoLogUnlessTheyAsk()
    {
        if (!TestApplication.HasDisplay)
            return;
        using var folder = new StorageFolder();
        var thisMachines = LogFiles.Folder(GameFolder.ForThisMachine());
        var thereBefore = Directory.Exists(thisMachines);
        using (var quiet = new Crasher(folder, logName: null))
            quiet.Run();
        Assert.False(Directory.Exists(folder["player/logs"]));
        // A crash: recorded, but no file and nothing on the terminal.
        using var crashing = new Crasher(folder, logName: null) { OnUpdate = (_, _) => throw new InvalidOperationException("a loop that fails") };
        var thrown = Assert.Throws<InvalidOperationException>(crashing.Run);
        Assert.Same(thrown, crashing.Crash!.Exception);
        Assert.Equal("on the main thread", crashing.Crash.Where);
        Assert.Null(crashing.Crash.File);
        Assert.Empty(crashing.Told);
        Assert.False(Directory.Exists(folder["player/logs"]));
        Assert.False(Path.Exists(folder["player"]), "the game folder was made");
        // Nor in this machine's game folder (the tests' redirect).
        Assert.Equal(thereBefore, Directory.Exists(thisMachines));
    }

    [Fact]
    public void ALogThatCantBeMadeDoesntStopTheRun()
    {
        if (!TestApplication.HasDisplay)
            return;
        using var folder = new StorageFolder();
        folder.Write("player/logs", [1]);
        var updated = false;
        using var application = new Crasher(folder) { OnUpdate = (_, _) => !(updated = true) };
        var lines = TestApplication.CaptureLog("Application", application.Run);
        Assert.True(updated);
        Assert.StartsWith($"Warning: no log file in {folder["player/logs"]}: ", Assert.Single(lines));
        Assert.Null(application.Crash);
    }

    [Fact]
    public void AnApplicationClosesOnlyTheLogItOpened()
    {
        using var folder = new StorageFolder();
        try
        {
            var log = LogFiles.Start(folder["logs"], "stat-fick", ProgramInfo.Of(typeof(ApplicationCrashTests).Assembly), DateTimeOffset.Now);
            // Another application, writing no log, ends (as a test's beside the program's would).
            new Crasher(folder, logName: null).Dispose();
            Assert.Equal(log, Log.FilePath);
        }
        finally
        {
            Log.CloseFile();
        }
    }

    [Fact]
    public void TheProgramsNameTheirLogsOnTheirOwnPathOnly()
    {
        using var folder = new StorageFolder();
        var game = GameData.Repo(EditorHarness.EngineFolder);
        using (var program = new EditorApplication(game, folder.Root))
            Assert.Equal("statventure-editor", program.Options.LogName);
        using (var tests = new EditorApplication(game, folder.Root, visible: false))
            Assert.Null(tests.Options.LogName);
        Assert.Null(new ApplicationOptions().LogName);
    }

    /// <summary>The crash file's lines, checked against the run's log: its parts in order, the log's end the lines before the crash.</summary>
    static void CheckCrashFile(Crasher application, Exception thrown, string where)
    {
        var crash = application.Crash!;
        Assert.Same(thrown, crash.Exception);
        Assert.Equal(where, crash.Where);
        var file = Assert.Single(application.CrashFiles());
        Assert.Equal(file, crash.File);
        Assert.Equal($"crash-{Stamp(crash.Time.LocalDateTime)}.txt", Path.GetFileName(file));
        // Written before the teardown.
        Assert.Contains(Path.GetFileName(file), application.AtTeardown!);
        var log = Assert.Single(Directory.GetFiles(application.Logs, "test-program-*.log"));
        var logLines = File.ReadAllLines(log);
        var lines = File.ReadAllText(file).Split('\n');
        Assert.Equal(ProgramLine, lines[0]);
        Assert.Equal($"crashed {crash.Time.ToString("yyyy-MM-dd HH:mm:ss zzz", System.Globalization.CultureInfo.InvariantCulture)} {where}", lines[1]);
        Assert.Equal(LogFiles.PlatformLine(), lines[2]);
        Assert.Equal("log " + log, lines[3]);
        Assert.Equal("", lines[4]);
        Assert.Equal($"{thrown.GetType().FullName}: {thrown.Message}", lines[5]);
        Assert.StartsWith("   at ", lines[6]);
        // The log's end: the run's log up to the crash (its first lines included), then nothing.
        var end = Array.IndexOf(lines, "The log's last lines:");
        Assert.True(end > 6, "no log's end");
        Assert.Equal("", lines[end - 1]);
        var before = logLines.TakeWhile(line => !Text(line).StartsWith("[Application] crashed ")).ToArray();
        Assert.Equal([.. before, ""], lines[(end + 1)..]);
        Assert.StartsWith("[Program] ", Text(before[0]));
        // The log names the crash and its file.
        Assert.Contains(logLines, line => Text(line).StartsWith($"[Application] crashed {where}: {thrown.GetType().FullName}: {thrown.Message}"));
        Assert.Contains(logLines, line => Text(line) == "[Application] crash file " + file);
        // The terminal (Q24), after the teardown.
        Assert.Equal([$"Crash test stopped because of an error.\n{crash.Message}\nThe details are in: {file}"], application.Told);
        Assert.Null(Log.FilePath);
    }

    [Theory]
    [InlineData("start")]
    [InlineData("load")]
    [InlineData("update")]
    [InlineData("render")]
    [InlineData("unload")]
    public void ACrashOnTheMainThreadWritesItsCrashFileBeforeTheTeardown(string when)
    {
        if (when != "start" && !TestApplication.HasDisplay)
            return;
        using var folder = new StorageFolder();
        void Fail(string what)
        {
            if (when == what)
                throw new InvalidOperationException($"a {what} that fails");
        }
        using var application = new Crasher(folder, developmentFolder: when == "start" ? folder["missing"] : null)
        {
            OnLoad = _ => Fail("load"),
            // On to a frame that renders (a frame without a swapchain image has none), within the few frames a hidden window presents.
            OnUpdate = (_, frame) =>
            {
                Fail("update");
                return when == "render" && frame < 3;
            },
            OnRender = _ => Fail("render"),
            OnUnload = _ => Fail("unload"),
        };
        var thrown = Assert.ThrowsAny<Exception>(application.Run);
        if (when != "start")
            Assert.Equal($"a {when} that fails", thrown.Message);
        CheckCrashFile(application, thrown, "on the main thread");
        if (when == "start")
            Assert.DoesNotContain(File.ReadAllLines(Assert.Single(Directory.GetFiles(application.Logs, "*.log"))), line => Text(line).StartsWith("[Platform] window "));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ATeardownThatFailsLosesNoCrashFile(bool afterACrash)
    {
        if (!TestApplication.HasDisplay)
            return;
        using var folder = new StorageFolder();
        using var application = new Crasher(folder)
        {
            OnUpdate = (_, _) => afterACrash ? throw new InvalidOperationException("an update that fails") : false,
            OnTeardown = _ => throw new IOException("a teardown that fails"),
        };
        Exception? thrown = null;
        var logged = TestApplication.CaptureLog("Application", () => thrown = Record.Exception(application.Run));
        Assert.NotNull(thrown);
        // C#'s rule: the teardown's exception leaves Run.
        Assert.Equal("a teardown that fails", thrown.Message);
        var crash = application.Crash!;
        Assert.Equal(afterACrash ? "an update that fails" : "a teardown that fails", crash.Exception.Message);
        var file = Assert.Single(application.CrashFiles());
        Assert.Equal(file, crash.File);
        Assert.Contains($"{crash.Exception.GetType().FullName}: {crash.Exception.Message}\n", File.ReadAllText(file));
        Assert.Equal(afterACrash, application.AtTeardown!.Contains(Path.GetFileName(file)));
        // After a crash, the teardown's failure is logged as another.
        Assert.Equal(afterACrash, logged.Any(line => line.StartsWith("Error: another crash, on the main thread: System.IO.IOException: a teardown that fails")));
        Assert.Equal([$"Crash test stopped because of an error.\n{crash.Message}\nThe details are in: {file}"], application.Told);
        Assert.Null(Log.FilePath);
    }

    /// <summary>A music library with one room that can't be prepared (a negative length), named anew each time (prepared rooms are kept for the process).</summary>
    static MusicLibrary BrokenRoom() => new([], new Dictionary<string, InstrumentSpec>(), new Dictionary<string, PercussionSpec>(),
        new Dictionary<string, RoomSpec> { ["broken " + Guid.NewGuid()] = new RoomSpec(-1, 0, 0) }, new Dictionary<string, EffectPatch>());

    [Theory]
    [InlineData("thread")]
    [InlineData("pool")]
    [InlineData("audio")]
    public void AnotherThreadsCrashWritesItsCrashFileAndEndsTheRunInOrder(string source)
    {
        if (!TestApplication.HasDisplay)
            return;
        using var folder = new StorageFolder();
        Thread? thread = null;
        AudioSystem? audio = null;
        var deadline = DateTime.UtcNow + Patience;
        var afterTheCrash = 0;
        using var application = new Crasher(folder)
        {
            // Shorter than the teardown (the save takes 1.5 s): the thread is let go once the loop takes the crash,
            // not after the teardown, or .NET would end the process in the middle of it.
            CrashAnswerTime = TimeSpan.FromSeconds(1),
            Minimized = true,
            WakeUp = 0,
            OnUpdate = (a, frame) =>
            {
                if (a.Crash is not null)
                    afterTheCrash++;
                if (frame == 0)
                {
                    // A save the teardown writes (S9-B), held a while by the writer.
                    a.Storage.Writer.BeforeWork = _ => Thread.Sleep(1500);
                    a.Storage.Saves.Save("slot", new Part { Name = source });
                    switch (source)
                    {
                        case "thread":
                            thread = new Thread(() => throw new InvalidOperationException("a thread that fails")) { Name = "Crash test thread", IsBackground = true };
                            thread.Start();
                            break;
                        case "pool":
                            ThreadPool.UnsafeQueueUserWorkItem(_ => throw new InvalidOperationException("a pool thread that fails"), null);
                            break;
                        default:
                            // The audio's room thread (AudioSystem.PrepareRooms), which nothing catches.
                            audio = new AudioSystem(new AudioGraph(48000), BrokenRoom());
                            break;
                    }
                }
                return DateTime.UtcNow < deadline;
            },
        };
        var thrown = Assert.ThrowsAny<Exception>(application.Run);
        audio?.Dispose();
        var where = source switch
        {
            "thread" => "on the thread \"Crash test thread\"",
            "pool" => "on a thread-pool thread",
            _ => "on the thread \"Statventure audio rooms\"",
        };
        Assert.Equal(where, application.Crash?.Where);
        Assert.Equal(source switch { "thread" => "a thread that fails", "pool" => "a pool thread that fails", _ => thrown.Message }, thrown.Message);
        if (source == "audio")
            Assert.IsType<ArgumentOutOfRangeException>(thrown);
        CheckCrashFile(application, thrown, where);
        // Taken by the next update's loop, not later.
        Assert.InRange(afterTheCrash, 0, 1);
        // Ended in order: the pending save written by the teardown, and the failing thread let go.
        Assert.True(File.Exists(folder["player/saves/slot.dat"]), "the pending save wasn't written");
        if (thread is not null)
            Assert.True(thread.Join(Patience), "the crashed thread wasn't let go");
    }

    [Fact]
    public void AMinimizedLoopIsWokenForAnotherThreadsCrash()
    {
        if (!TestApplication.HasDisplay)
            return;
        using var folder = new StorageFolder();
        var exception = new InvalidOperationException("a crash while minimized");
        bool? taken = null;
        Thread? helper = null;
        using var application = new Crasher(folder)
        {
            CrashAnswerTime = TimeSpan.FromSeconds(3),
            // Minimized and asking no wake-up: the loop sleeps until an event.
            Minimized = true,
            OnUpdate = (a, frame) =>
            {
                if (helper is null)
                {
                    helper = new Thread(() =>
                    {
                        Thread.Sleep(300);
                        taken = a.CrashedOnAnotherThread(exception, "on a test's thread");
                        // A loop that wasn't woken: woken now and ended, so the test ends.
                        a.Quit();
                        Window.PostEmptyEvent();
                    }) { IsBackground = true };
                    helper.Start();
                }
                return true;
            },
        };
        Assert.Same(exception, Assert.ThrowsAny<Exception>(application.Run));
        Assert.True(helper!.Join(Patience));
        Assert.True(taken, "the sleeping loop didn't take the crash in time");
    }

    [Fact]
    public void ACrashTheLoopDoesntTakeInTimeIsToldAtOnceAndEndsTheProcess()
    {
        if (!TestApplication.HasDisplay)
            return;
        using var folder = new StorageFolder();
        var exception = new InvalidOperationException("a crash the loop doesn't answer");
        using var gate = new ManualResetEventSlim();
        bool? taken = null;
        var deadline = DateTime.UtcNow + Patience;
        int toldThen = -1;
        string[] crashFilesThen = [];
        using var application = new Crasher(folder)
        {
            CrashAnswerTime = TimeSpan.FromMilliseconds(300),
            Minimized = true,
            WakeUp = 0,
            OnUpdate = (a, frame) =>
            {
                if (frame == 0)
                {
                    new Thread(() =>
                    {
                        taken = a.CrashedOnAnotherThread(exception, "on a test's thread");
                        (toldThen, crashFilesThen) = (a.Told.Count, a.CrashFiles());
                        gate.Set();
                    }) { IsBackground = true }.Start();
                    // The loop is busy: it doesn't answer.
                    Assert.True(gate.Wait(Patience));
                }
                return DateTime.UtcNow < deadline;
            },
        };
        // In a program the process ends when the handler gives false; here the loop takes the crash afterwards.
        Assert.Same(exception, Assert.ThrowsAny<Exception>(application.Run));
        Assert.False(taken);
        Assert.Equal(1, toldThen);
        Assert.Single(crashFilesThen);
        Assert.Single(application.Told);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AnotherThreadsCrashDuringTheTeardownEndsTheRunUnlessItCrashedAlready(bool crashedAlready)
    {
        if (!TestApplication.HasDisplay)
            return;
        using var folder = new StorageFolder();
        var late = new InvalidOperationException("a crash during the teardown");
        bool? taken = null;
        Thread? helper = null;
        using var application = new Crasher(folder)
        {
            OnUpdate = (_, _) => crashedAlready ? throw new InvalidOperationException("an update that fails") : false,
            OnTeardown = a =>
            {
                helper = new Thread(() => taken = a.CrashedOnAnotherThread(late, "on a test's thread")) { IsBackground = true };
                helper.Start();
                // The run ends once the crash is handed over: recorded, and the thread waiting for the main loop.
                var waited = System.Diagnostics.Stopwatch.StartNew();
                while (waited.Elapsed < Patience && !(a.Crash is not null && helper.ThreadState.HasFlag(System.Threading.ThreadState.WaitSleepJoin)))
                    Thread.Sleep(10);
            },
        };
        Exception? thrown = null;
        var logged = TestApplication.CaptureLog("Application", () => thrown = Record.Exception(application.Run));
        Assert.True(helper!.Join(Patience));
        Assert.True(taken, "the thread wasn't let go when the run ended");
        if (crashedAlready)
        {
            Assert.Equal("an update that fails", thrown!.Message);
            Assert.Equal("an update that fails", application.Crash!.Exception.Message);
            Assert.Contains(logged, line => line.StartsWith("Error: another crash, on a test's thread: System.InvalidOperationException: a crash during the teardown"));
        }
        else
        {
            Assert.Same(late, thrown);
            Assert.Same(late, application.Crash!.Exception);
        }
        Assert.Single(application.CrashFiles());
        Assert.Single(application.Told);
    }

    [Fact]
    public void ACrashElsewhereAfterTheRunIsntTheApplications()
    {
        if (!TestApplication.HasDisplay)
            return;
        using var folder = new StorageFolder();
        using var application = new Crasher(folder);
        application.Run();
        var exception = new InvalidOperationException("too late");
        Assert.False(application.CrashedOnAnotherThread(exception, "on a test's thread"));
        // No application running: .NET's own way.
        Assert.False(Application.CrashedElsewhere(exception));
        Assert.Null(application.Crash);
        Assert.Empty(application.CrashFiles());
        // The next application to run is the one a crash goes to: here, on a thread without a name.
        using var next = new Crasher(folder)
        {
            Minimized = true,
            WakeUp = 0,
            OnUpdate = (_, frame) =>
            {
                if (frame == 0)
                    new Thread(() => throw new InvalidOperationException("an unnamed thread that fails")) { IsBackground = true }.Start();
                return frame < 10000;
            },
        };
        Assert.Equal("an unnamed thread that fails", Assert.ThrowsAny<Exception>(next.Run).Message);
        Assert.Equal("on an unnamed thread", next.Crash!.Where);
    }

    [Fact]
    public void ACrashFileThatCantBeWrittenLeavesTheLogAndTheTerminal()
    {
        if (!TestApplication.HasDisplay || OperatingSystem.IsWindows())
            return;
        using var folder = new StorageFolder();
        using var application = new Crasher(folder)
        {
            OnUpdate = (a, _) =>
            {
                // The logs folder made read-only: the log goes on, no new file can be made.
                File.SetUnixFileMode(a.Logs, UnixFileMode.UserRead | UnixFileMode.UserExecute);
                throw new InvalidOperationException("an update that fails");
            },
        };
        try
        {
            var logged = TestApplication.CaptureLog("Application", () => Assert.ThrowsAny<Exception>(application.Run));
            Assert.StartsWith($"Error: the crash file couldn't be written in {application.Logs}: ", logged[0]);
            Assert.StartsWith("Error: crashed on the main thread: ", logged[1]);
            Assert.Equal(2, logged.Count);
        }
        finally
        {
            File.SetUnixFileMode(application.Logs, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }
        Assert.Null(application.Crash!.File);
        Assert.Empty(application.CrashFiles());
        Assert.Equal(["Crash test stopped because of an error.\nInvalidOperationException: an update that fails"], application.Told);
    }
}

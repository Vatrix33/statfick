using System.Text;
using Statventure.Core;
using Statventure.Tests.Storage;

namespace Statventure.Tests.Core;

/// <summary>
/// A program's logs and crash files (Docs/Packaging.md, "Logs" and "Crashes"; M12-8, Q3, Q21): the names, the run's log
/// with its first lines, which files are kept and removed when a program starts, a crash file's text, the log's end
/// and a log that can't be written. Every folder is the test's own. In the non-parallel "Storage log" collection: the
/// log (its file and its last lines) is one for the process.
/// </summary>
[Collection("Storage log")]
public class LogFilesTests : IDisposable
{
    readonly LogLevel _level = Log.Level;

    /// <summary>The log at its programs' level, Info, while the test runs (the GPU tests' fixture leaves it at Warning).</summary>
    public LogFilesTests() => Log.Level = LogLevel.Info;

    public void Dispose() => Log.Level = _level;

    static readonly DateTime Now = new(2026, 11, 8, 14, 3, 22, DateTimeKind.Local);

    /// <summary>A released program's version and commit, as its program file records them.</summary>
    static readonly ProgramInfo Released = new("Statventure.Sandbox", "2026.11.8.1", "5c1e2f3a9b0d4e6f7a8b9c0d1e2f3a4b5c6d7e8f");

    [Fact]
    public void NamesAreTheProgramsAndTheLocalTimeWithoutColons()
    {
        Assert.Equal("stat-fick-2026-11-08_14-03-22.log", LogFiles.LogName("stat-fick", Now));
        Assert.Equal("statventure-editor-2026-01-05_04-07-09.log", LogFiles.LogName("statventure-editor", new DateTime(2026, 1, 5, 4, 7, 9)));
        Assert.Equal("crash-2026-11-08_14-03-22.txt", LogFiles.CrashFileName(Now));
        Assert.Equal(Path.Combine(Path.GetFullPath("/players/one"), "logs"), LogFiles.Folder("/players/one"));
        Assert.Equal((10, 10), (LogFiles.KeptLogs, LogFiles.KeptCrashFiles));
    }

    [Fact]
    public void TheRunsLogHasItsFirstLinesAndTakesTheLinesThatFollow()
    {
        using var folder = new StorageFolder();
        var logs = folder["player/logs"];
        var started = new DateTimeOffset(Now);
        string path;
        try
        {
            path = LogFiles.Start(logs, "stat-fick", Released, started);
            Assert.Equal(path, Log.FilePath);
            Log.Info("Test", "a line after the first ones");
        }
        finally
        {
            Log.CloseFile();
        }
        Assert.Equal(Path.Combine(logs, "stat-fick-2026-11-08_14-03-22.log"), path);
        Assert.Null(Log.FilePath);
        var lines = File.ReadAllLines(path);
        Assert.Equal(
            [
                "[Program] stat-fick 2026.11.8.1, commit 5c1e2f3a9b0d4e6f7a8b9c0d1e2f3a4b5c6d7e8f",
                "[Program] started " + started.ToString("yyyy-MM-dd HH:mm:ss zzz", System.Globalization.CultureInfo.InvariantCulture),
                "[Program] " + LogFiles.PlatformLine(),
                "[Test] a line after the first ones",
            ],
            lines.Select(line => line[line.IndexOf('[')..]));
        Assert.All(lines, line => Assert.Contains(" INFO  [", line));
        // UTF-8 without a byte order mark.
        Assert.Equal((byte)' ', File.ReadAllBytes(path)[0]);
        // A dev build's, and one that recorded no commit (built without git).
        Assert.Equal("statventure-editor dev, commit 5c1e", LogFiles.ProgramLine("statventure-editor", new ProgramInfo("Statventure.Editor", ProgramInfo.Dev, "5c1e")));
        Assert.Equal("stat-fick dev, commit unknown", LogFiles.ProgramLine("stat-fick", new ProgramInfo("Statventure.Sandbox", ProgramInfo.Dev, null)));
        Assert.Contains(System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription, LogFiles.PlatformLine());
        Assert.Contains(System.Runtime.InteropServices.RuntimeInformation.OSDescription, LogFiles.PlatformLine());
        Assert.Contains($"({Environment.OSVersion.VersionString}) {System.Runtime.InteropServices.RuntimeInformation.OSArchitecture}, ", LogFiles.PlatformLine());
        if (!OperatingSystem.IsWindows())
        {
            // Private, as the game folder's other files: the folders made 0700, the file 0600.
            const UnixFileMode folderMode = UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute;
            Assert.Equal(folderMode, File.GetUnixFileMode(logs));
            Assert.Equal(folderMode, File.GetUnixFileMode(folder["player"]));
            Assert.Equal(UnixFileMode.UserRead | UnixFileMode.UserWrite, File.GetUnixFileMode(path));
        }
    }

    /// <summary>The names of a folder's files and folders, in ordinal order.</summary>
    static string[] Names(string folder) =>
        [.. Directory.EnumerateFileSystemEntries(folder).Select(Path.GetFileName).Order(StringComparer.Ordinal)!];

    /// <summary>
    /// A logs folder holding 12 runs' logs of each program and 12 crash files, made in a shuffled order with their
    /// modified times the reverse of their names' (so only the names can order them), and files and folders of other
    /// name forms.
    /// </summary>
    static (string[] Game, string[] Editor, string[] Crashes, string[] Others) Fill(string logs)
    {
        Directory.CreateDirectory(logs);
        // Newest first. The 9th and 10th game logs share a second: "-2" is the later one.
        string[] game =
        [
            "stat-fick-2026-11-08_09-00-00.log", "stat-fick-2026-11-07_23-59-59.log", "stat-fick-2026-11-07_12-00-00.log",
            "stat-fick-2026-11-06_12-00-00.log", "stat-fick-2026-11-05_12-00-00.log", "stat-fick-2026-10-31_12-00-00.log",
            "stat-fick-2026-10-30_12-00-00.log", "stat-fick-2026-10-02_12-00-00.log", "stat-fick-2026-10-01_10-00-00-2.log",
            "stat-fick-2026-10-01_10-00-00.log", "stat-fick-2026-09-01_12-00-00.log", "stat-fick-2025-12-31_12-00-00.log",
        ];
        var editor = Enumerable.Range(1, 12).Select(day => $"statventure-editor-2026-10-{day:00}_08-00-00.log").Reverse().ToArray();
        // Newest first. The 10th and 11th share a second: "-3" is later than "-2" (numbers, not text, order them).
        string[] crashes =
        [
            "crash-2026-11-08_08-00-00.txt", "crash-2026-11-07_08-00-00.txt", "crash-2026-11-06_08-00-00.txt",
            "crash-2026-11-05_08-00-00.txt", "crash-2026-11-04_08-00-00.txt", "crash-2026-11-03_08-00-00.txt",
            "crash-2026-11-02_08-00-00.txt", "crash-2026-11-01_08-00-00.txt", "crash-2026-10-31_08-00-00-12.txt",
            "crash-2026-10-31_08-00-00-3.txt", "crash-2026-10-31_08-00-00-2.txt", "crash-2026-01-01_08-00-00.txt",
        ];
        // Other name forms, kept whatever their age.
        string[] others =
        [
            "stat-fick.log", "stat-fick-2026-11-08.log", "stat-fick-notes-2020-01-01_00-00-00.log", "stat-fick-2020-01-01_00-00-00.txt",
            "stat-fick-2020-01-01_00-00-00.log.bak", "old-stat-fick-2020-01-01_00-00-00.log", "Stat-Fick-2020-01-01_00-00-00.log",
            "stat-fick-2020-1-1_00-00-00.log", "stat-fick-2020-01-01_00-00-00-.log", "stat-fick-2020-01-01_00-00-00-x.log",
            "crash.txt", "crash-old.txt", "crash-2020-01-01_00-00-00.log", "crash-stat-fick-2020-01-01_00-00-00.txt",
            "statventure-editor.log", "readme.txt",
        ];
        var all = game.Concat(editor).Concat(crashes).Concat(others).ToList();
        var shuffle = new Random(12);
        var time = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        foreach (var name in all.OrderBy(_ => shuffle.Next()))
            File.WriteAllText(Path.Combine(logs, name), name);
        // Modified times: the newest by name the oldest on the disk.
        for (var i = 0; i < 12; i++)
        {
            File.SetLastWriteTimeUtc(Path.Combine(logs, game[i]), time.AddDays(i));
            File.SetLastWriteTimeUtc(Path.Combine(logs, editor[i]), time.AddDays(i));
            File.SetLastWriteTimeUtc(Path.Combine(logs, crashes[i]), time.AddDays(i));
        }
        // A folder named as a log is no log.
        Directory.CreateDirectory(Path.Combine(logs, "stat-fick-2019-01-01_00-00-00.log"));
        return (game, editor, crashes, [.. others, "stat-fick-2019-01-01_00-00-00.log"]);
    }

    [Fact]
    public void AStartKeepsItsProgramsLastTenRunsAndTheLastTenCrashFilesAndNothingElse()
    {
        using var folder = new StorageFolder();
        var logs = folder["logs"];
        var (game, editor, crashes, others) = Fill(logs);
        string path;
        try
        {
            path = LogFiles.Start(logs, "stat-fick", Released, new DateTimeOffset(Now));
        }
        finally
        {
            Log.CloseFile();
        }
        Assert.Equal("stat-fick-2026-11-08_14-03-22.log", Path.GetFileName(path));
        // This run and the 9 runs before it of the game; the editor's runs all; the 10 newest crash files; the rest.
        string[] expected = [Path.GetFileName(path), .. game.Take(9), .. editor, .. crashes.Take(10), .. others];
        Assert.Equal(expected.Order(StringComparer.Ordinal), Names(logs));
        // The files kept are as they were.
        Assert.Equal("stat-fick-2026-10-01_10-00-00-2.log", File.ReadAllText(Path.Combine(logs, "stat-fick-2026-10-01_10-00-00-2.log")));
    }

    [Fact]
    public void TheEditorsStartCountsOnlyTheEditorsRuns()
    {
        using var folder = new StorageFolder();
        var logs = folder["logs"];
        var (game, editor, crashes, others) = Fill(logs);
        try
        {
            LogFiles.Start(logs, "statventure-editor", Released, new DateTimeOffset(Now));
        }
        finally
        {
            Log.CloseFile();
        }
        string[] expected = ["statventure-editor-2026-11-08_14-03-22.log", .. editor.Take(9), .. game, .. crashes.Take(10), .. others];
        Assert.Equal(expected.Order(StringComparer.Ordinal), Names(logs));
    }

    [Fact]
    public void TheRunsOwnLogIsKeptWhenOlderRunsAreDatedLater()
    {
        // The clock was set back: ten runs dated after this one. This run's log stays; the oldest of them goes.
        using var folder = new StorageFolder();
        var logs = folder["logs"];
        Directory.CreateDirectory(logs);
        var later = Enumerable.Range(1, 10).Select(n => $"stat-fick-2099-01-{n:00}_00-00-00.log").ToArray();
        foreach (var name in later)
            File.WriteAllText(Path.Combine(logs, name), "");
        try
        {
            LogFiles.Start(logs, "stat-fick", Released, new DateTimeOffset(Now));
        }
        finally
        {
            Log.CloseFile();
        }
        string[] expected = ["stat-fick-2026-11-08_14-03-22.log", .. later.Skip(1)];
        Assert.Equal(expected.Order(StringComparer.Ordinal), Names(logs));
    }

    [Fact]
    public void ANameTakenGetsANumberAndNothingIsOverwritten()
    {
        using var folder = new StorageFolder();
        var logs = folder["logs"];
        Directory.CreateDirectory(logs);
        File.WriteAllText(Path.Combine(logs, "stat-fick-2026-11-08_14-03-22.log"), "the first run");
        File.WriteAllText(Path.Combine(logs, "crash-2026-11-08_14-03-22.txt"), "the first crash");
        string second, third;
        try
        {
            second = LogFiles.Start(logs, "stat-fick", Released, new DateTimeOffset(Now));
            third = LogFiles.Start(logs, "stat-fick", Released, new DateTimeOffset(Now));
        }
        finally
        {
            Log.CloseFile();
        }
        Assert.Equal(("stat-fick-2026-11-08_14-03-22-2.log", "stat-fick-2026-11-08_14-03-22-3.log"), (Path.GetFileName(second), Path.GetFileName(third)));
        Assert.Equal("the first run", File.ReadAllText(Path.Combine(logs, "stat-fick-2026-11-08_14-03-22.log")));
        var crash = LogFiles.WriteCrashFile(logs, new DateTimeOffset(Now), "the second crash");
        Assert.Equal(Path.Combine(logs, "crash-2026-11-08_14-03-22-2.txt"), crash);
        Assert.Equal("the first crash", File.ReadAllText(Path.Combine(logs, "crash-2026-11-08_14-03-22.txt")));
        Assert.Equal("the second crash", File.ReadAllText(crash));
    }

    [Fact]
    public void ACrashFileIsWrittenWholeInAFolderMadeForIt()
    {
        using var folder = new StorageFolder();
        var logs = folder["player/logs"];
        var path = LogFiles.WriteCrashFile(logs, new DateTimeOffset(Now), "crashed — é\n");
        Assert.Equal(Path.Combine(logs, "crash-2026-11-08_14-03-22.txt"), path);
        Assert.Equal(Encoding.UTF8.GetBytes("crashed — é\n"), File.ReadAllBytes(path));
        if (!OperatingSystem.IsWindows())
        {
            Assert.Equal(UnixFileMode.UserRead | UnixFileMode.UserWrite, File.GetUnixFileMode(path));
            Assert.Equal(UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute, File.GetUnixFileMode(logs));
        }
    }

    [Fact]
    public void ALogFolderThatCantBeMadeStopsTheStartWithNothingOpen()
    {
        using var folder = new StorageFolder();
        folder.Write("player/logs", [1]);
        Assert.ThrowsAny<IOException>(() => LogFiles.Start(folder["player/logs"], "stat-fick", Released, new DateTimeOffset(Now)));
        Assert.Null(Log.FilePath);
        Assert.ThrowsAny<IOException>(() => LogFiles.WriteCrashFile(folder["player/logs"], new DateTimeOffset(Now), "x"));
        // A folder that can't be listed: nothing removed, a warning, no exception.
        var lines = TestApplication.CaptureLog("Program", () => LogFiles.Prune(folder["missing"], "stat-fick"));
        Assert.StartsWith("Warning: old logs not removed: ", Assert.Single(lines));
    }

    static Exception Thrown()
    {
        try
        {
            throw new InvalidOperationException("a test's crash\nover two lines");
        }
        catch (Exception exception)
        {
            return exception;
        }
    }

    [Fact]
    public void ACrashFileHoldsTheProgramTheTimeTheSystemTheExceptionAndTheLogsEnd()
    {
        var exception = Thrown();
        var time = new DateTimeOffset(2026, 11, 8, 14, 5, 10, TimeSpan.FromHours(1));
        var text = LogFiles.CrashText("stat-fick", Released, time, "on the thread \"Statventure audio rooms\"", exception, ["    1.000 INFO  [Test] one", "    2.000 WARN  [Test] two"], "/players/one/logs/stat-fick-2026-11-08_14-03-22.log");
        var expected = string.Join("\n",
            "stat-fick 2026.11.8.1, commit 5c1e2f3a9b0d4e6f7a8b9c0d1e2f3a4b5c6d7e8f",
            "crashed 2026-11-08 14:05:10 +01:00 on the thread \"Statventure audio rooms\"",
            LogFiles.PlatformLine(),
            "log /players/one/logs/stat-fick-2026-11-08_14-03-22.log",
            "",
            exception.ToString().ReplaceLineEndings("\n"),
            "",
            "The log's last lines:",
            "    1.000 INFO  [Test] one",
            "    2.000 WARN  [Test] two",
            "");
        Assert.Equal(expected, text);
        // The stack names the method that threw (with its file and line in a build with symbols).
        Assert.Contains("at Statventure.Tests.Core.LogFilesTests.Thrown()", text);
        Assert.Contains("log (none)\n", LogFiles.CrashText("stat-fick", Released, time, "on the main thread", exception, [], null));
    }

    [Fact]
    public void ACrashsMessageIsOneLineAndTheTerminalsWordsAreTheCrashWindows()
    {
        var crash = new CrashReport(Thrown(), "on the main thread", DateTimeOffset.Now, "/players/one/logs/crash-2026-11-08_14-05-10.txt");
        Assert.Equal("InvalidOperationException: a test's crash over two lines", crash.Message);
        Assert.Equal(
            "Stat Fick stopped because of an error.\nInvalidOperationException: a test's crash over two lines\nThe details are in: /players/one/logs/crash-2026-11-08_14-05-10.txt",
            crash.TerminalText("Stat Fick"));
        Assert.Equal("Statventure Editor stopped because of an error.\nInvalidOperationException: a test's crash over two lines",
            (crash with { File = null }).TerminalText("Statventure Editor"));
    }

    [Fact]
    public void TheLogKeepsItsLastLinesSinceItsFileOpened()
    {
        var (toConsole, level) = (Log.ToConsole, Log.Level);
        try
        {
            Log.ToConsole = false;
            Log.Level = LogLevel.Info;
            Log.Info("Test", "before the file");
            Log.OpenFile(new MemoryStream(), "/players/one/logs/stat-fick-2026-11-08_14-03-22.log");
            Assert.Empty(Log.Tail());
            for (var i = 0; i < Log.TailLength + 5; i++)
                Log.Info("Test", $"line {i}");
            Log.Debug("Test", "below the level");
            var tail = Log.Tail();
            Assert.Equal(100, Log.TailLength);
            Assert.Equal(Enumerable.Range(5, Log.TailLength).Select(i => $"[Test] line {i}"), tail.Select(line => line[line.IndexOf('[')..]));
            Assert.All(tail, line => Assert.Matches(@"^ *[0-9]+\.[0-9]{3} INFO  \[Test\] line [0-9]+$", line));
        }
        finally
        {
            Log.CloseFile();
            (Log.ToConsole, Log.Level) = (toConsole, level);
        }
    }

    /// <summary>A stream whose writes fail, as a full disk's.</summary>
    sealed class FullDisk : MemoryStream
    {
        public override void Write(byte[] buffer, int offset, int count) => throw new IOException("No space left on device");
        public override void Write(ReadOnlySpan<byte> buffer) => throw new IOException("No space left on device");
    }

    [Fact]
    public void ALogThatCantBeWrittenStopsNothing()
    {
        var (toConsole, error) = (Log.ToConsole, Console.Error);
        var console = new StringWriter();
        try
        {
            Log.ToConsole = true;
            Console.SetError(console);
            Log.OpenFile(new FullDisk(), "/players/one/logs/stat-fick-2026-11-08_14-03-22.log");
            Log.Warning("Test", "a line the disk can't take");
            Log.Warning("Test", "and the next one");
            Assert.Equal(["[Test] a line the disk can't take", "[Test] and the next one"], Log.Tail().Select(line => line[line.IndexOf('[')..]));
        }
        finally
        {
            Log.CloseFile();
            Log.ToConsole = toConsole;
            Console.SetError(error);
        }
        var said = console.ToString().ReplaceLineEndings("\n").Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(3, said.Length);
        Assert.Equal($"The log file {Path.GetFullPath("/players/one/logs/stat-fick-2026-11-08_14-03-22.log")} can't be written any more: No space left on device", said[1]);
    }
}

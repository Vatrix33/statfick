using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using Statventure.Storage;

namespace Statventure.Core;

/// <summary>
/// A program's logs and crash files in the player's game folder (Docs/Packaging.md, "Logs" and "Crashes"; M12-8, Q3,
/// Q21): <c>logs/</c> beside the settings, saves and mods. One log per run, named for the program and the run's start
/// in local time (<c>stat-fick-2026-11-08_14-03-22.log</c>; the editor's <c>statventure-editor-…</c>), and on a crash
/// <c>crash-2026-11-08_14-05-10.txt</c>. When a program starts, the last <see cref="KeptLogs"/> runs of that program are
/// kept (the programs counted apart) and the last <see cref="KeptCrashFiles"/> crash files; older files of those two
/// name forms are removed, and nothing else in the folder is touched. A name already taken (two starts in one second)
/// gets <c>-2</c>, <c>-3</c> … before its extension; files are never overwritten.
/// </summary>
public static class LogFiles
{
    /// <summary>The folder's name in the game folder.</summary>
    public const string FolderName = "logs";

    /// <summary>How many runs' logs are kept per program (Q21).</summary>
    public const int KeptLogs = 10;

    /// <summary>How many crash files are kept, the programs' together (Q21).</summary>
    public const int KeptCrashFiles = 10;

    const string CrashPrefix = "crash";
    const string StampFormat = "yyyy-MM-dd_HH-mm-ss";
    const string StampPattern = "([0-9]{4}-[0-9]{2}-[0-9]{2}_[0-9]{2}-[0-9]{2}-[0-9]{2})(?:-([0-9]+))?";

    /// <summary>The logs' folder in <paramref name="gameFolder"/>.</summary>
    public static string Folder(string gameFolder) => Path.Combine(Path.GetFullPath(gameFolder), FolderName);

    /// <summary>A run's log name: <c>&lt;program&gt;-yyyy-MM-dd_HH-mm-ss.log</c>, in the local time it gets (no colons, for Windows).</summary>
    public static string LogName(string program, DateTime started) => $"{program}-{Stamp(started)}.log";

    /// <summary>A crash file's name: <c>crash-yyyy-MM-dd_HH-mm-ss.txt</c>, in the local time it gets.</summary>
    public static string CrashFileName(DateTime crashed) => $"{CrashPrefix}-{Stamp(crashed)}.txt";

    static string Stamp(DateTime time) => time.ToString(StampFormat, CultureInfo.InvariantCulture);

    /// <summary>
    /// Starts the run's log in <paramref name="folder"/> (made if missing; on Linux mode 0700 for each folder made, the
    /// file 0600, as the game folder's other files): <see cref="Log"/> writes to it from now on, its first lines the
    /// program with its version and commit (<see cref="ProgramLine"/>, from <paramref name="info"/>), the start time and
    /// the system; then the older logs of <paramref name="program"/> and the older crash files are removed
    /// (<see cref="Prune"/>). Gives the log's path. Throws when the folder or the file can't be made (nothing is open then).
    /// </summary>
    public static string Start(string folder, string program, ProgramInfo info, DateTimeOffset started)
    {
        StorageFiles.CreateFolder(folder);
        var (stream, path) = CreateNew(folder, LogName(program, started.LocalDateTime));
        Log.OpenFile(stream, path);
        Log.Info("Program", ProgramLine(program, info));
        Log.Info("Program", "started " + Time(started));
        Log.Info("Program", PlatformLine());
        Prune(folder, program, keep: path);
        return path;
    }

    /// <summary>
    /// Writes a crash file in <paramref name="folder"/> (made if missing) holding <paramref name="text"/> (UTF-8, flushed
    /// to the disk before it returns) and gives its path. Throws when it can't be written.
    /// </summary>
    public static string WriteCrashFile(string folder, DateTimeOffset crashed, string text)
    {
        StorageFiles.CreateFolder(folder);
        var (stream, path) = CreateNew(folder, CrashFileName(crashed.LocalDateTime));
        using (stream)
        {
            stream.Write(new UTF8Encoding(encoderShouldEmitUTF8Identifier: false).GetBytes(text));
            stream.Flush(flushToDisk: true);
        }
        return path;
    }

    /// <summary>
    /// A crash file's text (Docs/Packaging.md, "Crashes"): the program with its version and commit
    /// (<see cref="ProgramLine"/>), the time and where it happened, the system, the run's log; the exception with its
    /// stack; then the log's last lines (<paramref name="logEnd"/>, the lines written before the crash).
    /// </summary>
    public static string CrashText(string program, ProgramInfo info, DateTimeOffset crashed, string where, Exception exception, IReadOnlyList<string> logEnd, string? log)
    {
        var text = new StringBuilder();
        text.Append(ProgramLine(program, info)).Append('\n');
        text.Append("crashed ").Append(Time(crashed)).Append(' ').Append(where).Append('\n');
        text.Append(PlatformLine()).Append('\n');
        text.Append("log ").Append(log ?? "(none)").Append('\n');
        text.Append('\n');
        text.Append(exception.ToString().ReplaceLineEndings("\n")).Append('\n');
        text.Append('\n');
        text.Append("The log's last lines:\n");
        foreach (var line in logEnd)
            text.Append(line.ReplaceLineEndings("\n")).Append('\n');
        return text.ToString();
    }

    /// <summary>
    /// The program, its version and its commit (Q3): the first line of every log and crash file, <paramref name="program"/>
    /// (its name for its logs) with <paramref name="info"/>'s version and commit, "stat-fick 2026.11.8.1, commit 5c1e…"
    /// ("dev" for a build that isn't a release; "commit unknown" for a build that recorded none).
    /// </summary>
    public static string ProgramLine(string program, ProgramInfo info) => $"{program} {info.Version}, commit {info.Commit ?? "unknown"}";

    /// <summary>
    /// The system the program runs on: the operating system (on Linux its name, then the kernel's version), the
    /// processor's architecture and .NET.
    /// </summary>
    internal static string PlatformLine() =>
        $"{RuntimeInformation.OSDescription} ({Environment.OSVersion.VersionString}) {RuntimeInformation.OSArchitecture}, {RuntimeInformation.FrameworkDescription}";

    static string Time(DateTimeOffset time) => time.ToString("yyyy-MM-dd HH:mm:ss zzz", CultureInfo.InvariantCulture);

    /// <summary>
    /// Removes from <paramref name="folder"/> the logs of <paramref name="program"/> but the newest <see cref="KeptLogs"/>
    /// and the crash files but the newest <see cref="KeptCrashFiles"/>, the newest by the time in their names (then the
    /// <c>-2</c> … that follows it); <paramref name="keep"/> (the run's own log) is always kept. Only files of those two
    /// name forms count; a file that can't be removed (another program's open log, on Windows) stays.
    /// </summary>
    public static void Prune(string folder, string program, string? keep = null)
    {
        string[] files;
        try
        {
            files = Directory.GetFiles(folder);
        }
        catch (Exception problem) when (problem is IOException or UnauthorizedAccessException)
        {
            Log.Warning("Program", $"old logs not removed: {problem.Message}");
            return;
        }
        var kept = keep is null ? null : Path.GetFullPath(keep);
        var logs = new Regex($"^{Regex.Escape(program)}-{StampPattern}\\.log$", RegexOptions.CultureInvariant);
        var crashes = new Regex($"^{CrashPrefix}-{StampPattern}\\.txt$", RegexOptions.CultureInvariant);
        Remove(files.Where(file => Path.GetFullPath(file) != kept), logs, kept is null ? KeptLogs : KeptLogs - 1);
        Remove(files, crashes, KeptCrashFiles);
    }

    static void Remove(IEnumerable<string> files, Regex form, int kept)
    {
        var matching = new List<(string File, string Stamp, int Number)>();
        foreach (var file in files)
            if (form.Match(Path.GetFileName(file)) is { Success: true } match)
                matching.Add((file, match.Groups[1].Value, match.Groups[2].Success ? int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture) : 1));
        foreach (var (file, _, _) in matching.OrderByDescending(m => m.Stamp, StringComparer.Ordinal).ThenByDescending(m => m.Number).Skip(kept))
        {
            try
            {
                File.Delete(file);
            }
            catch (Exception problem) when (problem is IOException or UnauthorizedAccessException)
            {
                Log.Warning("Program", $"an old log not removed: {problem.Message}");
            }
        }
    }

    /// <summary>A new file named <paramref name="name"/> in <paramref name="folder"/>, else <c>-2</c>, <c>-3</c> … before its extension: the first that nothing has.</summary>
    static (FileStream Stream, string Path) CreateNew(string folder, string name)
    {
        var options = new FileStreamOptions { Mode = FileMode.CreateNew, Access = FileAccess.Write, Share = FileShare.Read };
        if (!OperatingSystem.IsWindows())
            options.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
        var stem = Path.GetFileNameWithoutExtension(name);
        var extension = Path.GetExtension(name);
        for (var n = 1; ; n++)
        {
            var path = Path.Combine(folder, n == 1 ? name : $"{stem}-{n}{extension}");
            try
            {
                return (new FileStream(path, options), path);
            }
            catch (IOException) when (Path.Exists(path))
            {
                // Taken: the next name.
            }
        }
    }
}

/// <summary>
/// A crash (Docs/Packaging.md, "Crashes"; M12-10): the exception, where it happened (the main thread, or which other
/// thread), when, and the crash file's path (null when the program writes no logs, or the file couldn't be written).
/// What M12 step 5's crash window shows: <see cref="Message"/> and <see cref="File"/> (Q22); with no window,
/// <see cref="TerminalText"/> (Q24). <c>Application.Crash</c> holds the run's.
/// </summary>
public sealed record CrashReport(Exception Exception, string Where, DateTimeOffset Time, string? File)
{
    /// <summary>The one line naming the error: its kind and its message.</summary>
    public string Message => $"{Exception.GetType().Name}: {Exception.Message}".ReplaceLineEndings(" ");

    /// <summary>
    /// The crash's message for the terminal (Q24), worded as the crash window (Q22): "<paramref name="program"/> stopped
    /// because of an error.", the error's line, and where the details are (the crash file), one per line.
    /// </summary>
    public string TerminalText(string program) => File is null
        ? $"{program} stopped because of an error.\n{Message}"
        : $"{program} stopped because of an error.\n{Message}\nThe details are in: {File}";
}

using System.Diagnostics;
using System.Text;

namespace Statventure.Core;

public enum LogLevel
{
    Trace,
    Debug,
    Info,
    Warning,
    Error,
}

/// <summary>
/// The engine log: timestamped lines to the console and, once <see cref="OpenFile(string)"/> is called, to a
/// file (a program's run log, Docs/Packaging.md, "Logs": <see cref="LogFiles"/>). It keeps its last
/// <see cref="TailLength"/> lines since the file was opened (<see cref="Tail"/>: the log's end in a crash file).
/// Thread-safe. Tests can capture lines through <see cref="Sink"/>.
/// </summary>
public static class Log
{
    /// <summary>How many of the last lines <see cref="Tail"/> keeps (the log's end in a crash file).</summary>
    public const int TailLength = 100;

    static readonly object Gate = new();
    static readonly Stopwatch Clock = Stopwatch.StartNew();
    static readonly Queue<string> LastLines = new(TailLength);
    static StreamWriter? _file;
    static string? _filePath;

    public static LogLevel Level { get; set; } = LogLevel.Info;
    public static bool ToConsole { get; set; } = true;

    /// <summary>Receives every line that passes <see cref="Level"/>, after it is written.</summary>
    public static Action<LogLevel, string, string>? Sink { get; set; }

    /// <summary>The log file's full path while one is open, else null.</summary>
    public static string? FilePath
    {
        get
        {
            lock (Gate)
                return _filePath;
        }
    }

    /// <summary>Makes <paramref name="path"/> anew (and its folder) and writes the lines to it from now on (see <see cref="OpenFile(Stream, string)"/>).</summary>
    public static void OpenFile(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        OpenFile(new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read), path);
    }

    /// <summary>
    /// Writes the lines to <paramref name="stream"/> from now on (UTF-8 without a byte order mark, every line flushed
    /// as written), closing the file open before; <paramref name="path"/> is its name (<see cref="FilePath"/>). The
    /// <see cref="Tail"/> starts again, so it holds this file's last lines.
    /// </summary>
    public static void OpenFile(Stream stream, string path)
    {
        lock (Gate)
        {
            _file?.Dispose();
            _file = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)) { AutoFlush = true };
            _filePath = Path.GetFullPath(path);
            LastLines.Clear();
        }
    }

    public static void CloseFile()
    {
        lock (Gate)
        {
            _file?.Dispose();
            _file = null;
            _filePath = null;
        }
    }

    /// <summary>The last <see cref="TailLength"/> lines written (each as the file has it), oldest first.</summary>
    public static IReadOnlyList<string> Tail()
    {
        lock (Gate)
            return [.. LastLines];
    }

    public static void Trace(string category, string message) => Write(LogLevel.Trace, category, message);
    public static void Debug(string category, string message) => Write(LogLevel.Debug, category, message);
    public static void Info(string category, string message) => Write(LogLevel.Info, category, message);
    public static void Warning(string category, string message) => Write(LogLevel.Warning, category, message);
    public static void Error(string category, string message) => Write(LogLevel.Error, category, message);

    public static void Error(string category, Exception exception) => Write(LogLevel.Error, category, exception.ToString());

    public static void Write(LogLevel level, string category, string message)
    {
        if (level < Level)
            return;
        var line = $"{Clock.Elapsed.TotalSeconds,9:0.000} {Label(level)} [{category}] {message}";
        lock (Gate)
        {
            if (ToConsole)
            {
                var writer = level >= LogLevel.Warning ? Console.Error : Console.Out;
                writer.WriteLine(line);
            }
            if (_file is { } file)
            {
                try
                {
                    file.WriteLine(line);
                }
                catch (Exception problem) when (problem is IOException or ObjectDisposedException)
                {
                    // A log that can't be written (a full disk) doesn't stop the program: the console goes on.
                    _file = null;
                    if (ToConsole)
                        Console.Error.WriteLine($"The log file {_filePath} can't be written any more: {problem.Message}");
                }
            }
            if (LastLines.Count == TailLength)
                LastLines.Dequeue();
            LastLines.Enqueue(line);
        }
        Sink?.Invoke(level, category, message);
    }

    static string Label(LogLevel level) => level switch
    {
        LogLevel.Trace => "TRACE",
        LogLevel.Debug => "DEBUG",
        LogLevel.Info => "INFO ",
        LogLevel.Warning => "WARN ",
        _ => "ERROR",
    };
}

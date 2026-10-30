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
/// The engine log: timestamped lines to the console and, once <see cref="OpenFile"/> is called, to a
/// file. Thread-safe. Tests can capture lines through <see cref="Sink"/>.
/// </summary>
public static class Log
{
    static readonly object Gate = new();
    static readonly Stopwatch Clock = Stopwatch.StartNew();
    static StreamWriter? _file;

    public static LogLevel Level { get; set; } = LogLevel.Info;
    public static bool ToConsole { get; set; } = true;

    /// <summary>Receives every line that passes <see cref="Level"/>, after it is written.</summary>
    public static Action<LogLevel, string, string>? Sink { get; set; }

    public static void OpenFile(string path)
    {
        lock (Gate)
        {
            _file?.Dispose();
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
            _file = new StreamWriter(path, append: false, Encoding.UTF8) { AutoFlush = true };
        }
    }

    public static void CloseFile()
    {
        lock (Gate)
        {
            _file?.Dispose();
            _file = null;
        }
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
            _file?.WriteLine(line);
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

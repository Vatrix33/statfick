using Statventure.Core;
using Statventure.Graphics;
using Statventure.Platform;

namespace Statventure.Tests.Core;

/// <summary>
/// A real application with a small window whose Load, Update and Unload are the test's; it quits after the
/// update unless <see cref="OnUpdate"/> asks to go on. Needs a display, as the swapchain test.
/// </summary>
sealed class TestApplication(ApplicationOptions options)
    : Application(options with { Window = new WindowOptions { Title = "Statventure test", Width = 160, Height = 100 } })
{
    public static bool HasDisplay =>
        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WAYLAND_DISPLAY")) || !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DISPLAY"));

    public Action<TestApplication>? OnLoad { get; init; }

    /// <summary>Each update, with its frame number; the application quits after it unless it gives true.</summary>
    public Func<TestApplication, long, bool>? OnUpdate { get; init; }

    public Action<TestApplication>? OnUnload { get; init; }

    protected override void Load() => OnLoad?.Invoke(this);

    protected override void Update(in GameTime time)
    {
        if (OnUpdate?.Invoke(this, time.Frame) != true)
            Quit();
    }

    protected override void Render(Frame frame, GpuImage target, in GameTime time) { }

    protected override void Unload() => OnUnload?.Invoke(this);

    /// <summary>The log lines of <paramref name="category"/> written while <paramref name="action"/> runs (tests using it are in the non-parallel "Storage log" collection: the log is one for the process).</summary>
    public static List<string> CaptureLog(string category, Action action)
    {
        var lines = new List<string>();
        var (sink, level) = (Log.Sink, Log.Level);
        Log.Sink = (lineLevel, lineCategory, message) =>
        {
            sink?.Invoke(lineLevel, lineCategory, message);
            if (lineCategory == category)
                lock (lines)
                    lines.Add($"{lineLevel}: {message}");
        };
        Log.Level = LogLevel.Info;
        try
        {
            action();
        }
        finally
        {
            (Log.Sink, Log.Level) = (sink, level);
        }
        lock (lines)
            return [.. lines];
    }
}

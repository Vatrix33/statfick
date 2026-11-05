using Statventure.Core;

namespace Statventure.Tests.Core;

/// <summary>
/// In the non-parallel "Storage log" collection, as it reads the log: run beside the parallel tests, its sink
/// caught a settings test's Warning ("Storage: settings …") and failed (found 2026-11-05, 2 of 5 filtered runs on lavapipe).
/// </summary>
[Collection("Storage log")]
public class LogTests
{
    [Fact]
    public void LinesBelowTheLevelAreDropped()
    {
        var seen = new List<string>();
        var (level, toConsole, sink) = (Log.Level, Log.ToConsole, Log.Sink);
        try
        {
            Log.ToConsole = false;
            Log.Level = LogLevel.Warning;
            Log.Sink = (_, category, message) => seen.Add(category + ":" + message);
            Log.Info("Test", "quiet");
            Log.Warning("Test", "loud");
            Assert.Equal(["Test:loud"], seen);
        }
        finally
        {
            (Log.Level, Log.ToConsole, Log.Sink) = (level, toConsole, sink);
        }
    }
}

using Statventure.Core;

namespace Statventure.Tests.Core;

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

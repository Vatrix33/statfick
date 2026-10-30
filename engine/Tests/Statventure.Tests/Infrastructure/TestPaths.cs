using System.Runtime.CompilerServices;

namespace Statventure.Tests.Infrastructure;

/// <summary>Folders the tests read outside their project.</summary>
public static class TestPaths
{
    static string ProjectDirectory([CallerFilePath] string path = "") => Path.GetDirectoryName(Path.GetDirectoryName(path)!)!;

    /// <summary>
    /// The old JavaScript game (its stylesheets, scripts and images for the game screens): the repo's
    /// old-game/ next to engine/, or $STATVENTURE_OLD_GAME.
    /// </summary>
    public static string OldGame =>
        Environment.GetEnvironmentVariable("STATVENTURE_OLD_GAME") is { Length: > 0 } folder
            ? folder
            : Path.GetFullPath(Path.Combine(ProjectDirectory(), "..", "..", "..", "old-game"));
}

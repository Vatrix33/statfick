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

    /// <summary>
    /// The game's committed packs, the source of truth (Docs/Assets.md, "Packs are the source of
    /// truth"): the repo's engine/Assets/Packs/ (made by the first step that commits a pack).
    /// </summary>
    public static string Packs => Path.GetFullPath(Path.Combine(ProjectDirectory(), "..", "..", "Assets", "Packs"));

    /// <summary>
    /// The committed models packs in the order the game mounts them (the user's Q77 of GM2: <c>models.pak</c>, then a
    /// <c>models-&lt;race&gt;.pak</c> for each race with models of its own; <see cref="Statventure.Core.Application.GamePackFiles"/>).
    /// </summary>
    public static IReadOnlyList<string> ModelsPacks =>
        [.. Statventure.Core.Application.GamePackFiles(Packs).Where(f => Path.GetFileName(f) == "models.pak" || Statventure.Core.Application.IsModelsPackName(Path.GetFileName(f)))];
}

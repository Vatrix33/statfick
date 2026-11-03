namespace Statventure.Storage;

/// <summary>
/// Where the per-user game folder is (Docs/Assets.md, "The game folder"): the player's settings,
/// save slots and mods, side by side, and what "Open game folder" opens. Linux:
/// <c>$XDG_DATA_HOME/stat-fick</c>, else <c>~/.local/share/stat-fick</c>; Windows:
/// <c>%APPDATA%\Stat Fick</c>. Nothing here touches the folder; it is made on first use.
/// </summary>
public static class GameFolder
{
    /// <summary>The folder's name on Linux.</summary>
    public const string LinuxName = "stat-fick";

    /// <summary>The folder's name on Windows.</summary>
    public const string WindowsName = "Stat Fick";

    /// <summary>
    /// The game folder for a system, environment and home folder; a pure function, so the tests check
    /// every case without touching a real folder.
    /// Linux: <c>$XDG_DATA_HOME</c> when it is set to an absolute path (the XDG base directory
    /// specification ignores a relative one, as .NET's LocalApplicationData does), else
    /// <c>&lt;home&gt;/.local/share</c>; then <c>stat-fick</c>. Windows: <paramref name="roamingAppData"/>
    /// (the RoamingAppData known folder, which <c>%APPDATA%</c> names); then <c>Stat Fick</c>.
    /// </summary>
    /// <param name="environment">Gives an environment variable's value, or null when it isn't set.</param>
    /// <param name="home">The user's home folder (Linux).</param>
    /// <param name="roamingAppData">The RoamingAppData known folder (Windows).</param>
    public static string Locate(StorageSystem system, Func<string, string?> environment, string? home, string? roamingAppData)
    {
        if (system == StorageSystem.Windows)
        {
            if (string.IsNullOrEmpty(roamingAppData))
                throw new InvalidOperationException("the RoamingAppData folder isn't known, so there is no game folder");
            return Join(roamingAppData, WindowsName, '\\');
        }
        var data = environment("XDG_DATA_HOME");
        if (string.IsNullOrEmpty(data) || data[0] != '/')
        {
            if (string.IsNullOrEmpty(home))
                throw new InvalidOperationException("the home folder isn't known, so there is no game folder");
            data = Join(home, ".local/share", '/');
        }
        return Join(data, LinuxName, '/');
    }

    /// <summary>This machine's game folder (not made here).</summary>
    public static string ForThisMachine() => Locate(
        StorageFiles.ThisSystem,
        Environment.GetEnvironmentVariable,
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile, Environment.SpecialFolderOption.DoNotVerify),
        OperatingSystem.IsWindows() ? Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData, Environment.SpecialFolderOption.DoNotVerify) : null);

    static string Join(string folder, string name, char separator) =>
        folder.EndsWith(separator) || separator == '\\' && folder.EndsWith('/') ? folder + name : folder + separator + name;
}

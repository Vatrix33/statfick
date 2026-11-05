using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Statventure.Tests.Infrastructure;

/// <summary>
/// The tests never reach the player's real folders (user decision 2026-11-05, Engine-Plan.md, "Decision on test
/// runs and the player's folders"): before any test runs, the process's own view of the per-user folders
/// (.NET's environment: <c>XDG_DATA_HOME</c>, <c>XDG_CONFIG_HOME</c> and <c>HOME</c>; on Windows also
/// <c>APPDATA</c>, <c>LOCALAPPDATA</c> and <c>USERPROFILE</c>) points into a scratch folder of this run, removed
/// when the process ends. So this machine's game folder and Documents folder are in it, and a test that forgets
/// to pass its own folder, or a mutation that makes the engine ignore the one it was given, writes there (M9
/// step 9's mutation "storage folder: the option ignored" wrote a test save into the real game folder before).
/// Only .NET's view changes: on Linux <see cref="Environment.SetEnvironmentVariable(string, string)"/> doesn't
/// reach the C library's environment, so the Vulkan loader, Mesa and GLFW see the real one; child processes get
/// .NET's (<see cref="SystemFonts"/> gives fontconfig the real folders back with <see cref="GiveOriginals"/>).
/// </summary>
static class PerUserFolders
{
    /// <summary>This run's scratch folder, holding the redirected per-user folders.</summary>
    public static string Root { get; private set; } = "";

    /// <summary>Each redirected variable with its value before (null: it wasn't set).</summary>
    public static IReadOnlyDictionary<string, string?> Originals { get; private set; } = new Dictionary<string, string?>();

    [ModuleInitializer]
    internal static void Initialize()
    {
        Root = Directory.CreateTempSubdirectory("statventure-tests-").FullName;
        var redirects = new Dictionary<string, string>
        {
            ["XDG_DATA_HOME"] = Path.Combine(Root, "data"),
            ["XDG_CONFIG_HOME"] = Path.Combine(Root, "config"),
            ["HOME"] = Path.Combine(Root, "home"),
        };
        if (OperatingSystem.IsWindows())
        {
            redirects["APPDATA"] = Path.Combine(Root, "AppData", "Roaming");
            redirects["LOCALAPPDATA"] = Path.Combine(Root, "AppData", "Local");
            redirects["USERPROFILE"] = Path.Combine(Root, "home");
        }
        // The build cache stays where it was: ThemeRenderTests falls back to the home folder's
        // .cache/stat-fick-build when STATVENTURE_CACHE isn't set (env.sh and env.ps1 set it).
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("STATVENTURE_CACHE")))
            Environment.SetEnvironmentVariable("STATVENTURE_CACHE",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".cache", "stat-fick-build"));
        Originals = redirects.Keys.ToDictionary(name => name, Environment.GetEnvironmentVariable);
        foreach (var (name, value) in redirects)
            Environment.SetEnvironmentVariable(name, value);
        AppDomain.CurrentDomain.ProcessExit += (_, _) =>
        {
            try
            {
                Directory.Delete(Root, recursive: true);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                // Left in the temporary folder.
            }
        };
    }

    /// <summary>Gives a child process the per-user folders as they were before the redirect.</summary>
    public static void GiveOriginals(ProcessStartInfo start)
    {
        foreach (var (name, value) in Originals)
        {
            if (value is null)
                start.Environment.Remove(name);
            else
                start.Environment[name] = value;
        }
    }
}

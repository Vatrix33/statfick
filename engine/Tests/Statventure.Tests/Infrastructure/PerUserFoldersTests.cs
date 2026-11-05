using System.Diagnostics;
using System.Runtime.InteropServices;
using Statventure.Core;
using Statventure.Storage;

namespace Statventure.Tests.Infrastructure;

/// <summary>
/// The test run never reaches the player's real folders (Engine-Plan.md, "Decision on test runs and the player's
/// folders", 2026-11-05): <see cref="PerUserFolders"/> points the process's per-user folders into the run's scratch
/// folder, so this machine's game folder and Documents folder are there; fontconfig is still asked with the real
/// ones, and the build cache stays where it was.
/// </summary>
public partial class PerUserFoldersTests
{
    static string Inside => PerUserFolders.Root + Path.DirectorySeparatorChar;

    [Fact]
    public void ThisMachinesFoldersAreInTheRunsScratchFolder()
    {
        Assert.StartsWith(Path.GetTempPath(), Inside);
        string[] variables = OperatingSystem.IsWindows()
            ? ["XDG_DATA_HOME", "XDG_CONFIG_HOME", "HOME", "APPDATA", "LOCALAPPDATA", "USERPROFILE"]
            : ["XDG_DATA_HOME", "XDG_CONFIG_HOME", "HOME"];
        foreach (var name in variables)
            Assert.StartsWith(Inside, Environment.GetEnvironmentVariable(name));
        var cache = Environment.GetEnvironmentVariable("STATVENTURE_CACHE");
        Assert.False(string.IsNullOrEmpty(cache), "the build cache isn't named, so it would be looked for in the scratch home");
        Assert.DoesNotContain(PerUserFolders.Root, cache);
        // Windows asks the known-folder API, which may not follow the variables (Engine-Plan.md, "Windows check").
        if (OperatingSystem.IsWindows())
            return;
        Assert.StartsWith(Inside, GameFolder.ForThisMachine());
        Assert.StartsWith(Inside, Application.StorageFolder(new ApplicationOptions()));
        Assert.StartsWith(Inside, DocumentsFolder.ForThisMachine());
    }

    /// <summary>The C library's environment, which the redirect leaves alone ("unset" when a variable isn't set).</summary>
    static string Native(string name) => Marshal.PtrToStringUTF8(GetEnv(name)) ?? "unset";

    [LibraryImport("libc", EntryPoint = "getenv", StringMarshalling = StringMarshalling.Utf8)]
    private static partial IntPtr GetEnv(string name);

    [Fact]
    public void FontconfigIsAskedWithTheRealFolders()
    {
        if (OperatingSystem.IsWindows())
            return;
        string[] script = ["-c", "printf '%s|%s|%s' \"${XDG_DATA_HOME-unset}\" \"${XDG_CONFIG_HOME-unset}\" \"${HOME-unset}\""];
        Assert.Equal($"{Native("XDG_DATA_HOME")}|{Native("XDG_CONFIG_HOME")}|{Native("HOME")}", SystemFonts.Run("sh", script));
        // Any other child process gets the scratch folders: .NET gives children its own view.
        var start = new ProcessStartInfo("sh") { RedirectStandardOutput = true };
        start.ArgumentList.Add("-c");
        start.ArgumentList.Add("printf %s \"$HOME\"");
        using var child = Process.Start(start)!;
        var home = child.StandardOutput.ReadToEnd();
        child.WaitForExit();
        Assert.StartsWith(Inside, home);
    }
}

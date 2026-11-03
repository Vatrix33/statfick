using System.Diagnostics;
using Statventure.Storage;

namespace Statventure.Tests.Storage;

/// <summary>
/// The per-user game folder (Docs/Assets.md, "The game folder"): where it is on each system and in
/// each environment (a pure function, so no real folder is touched), its layout, and "Open game
/// folder".
/// </summary>
public class GameFolderTests
{
    static Func<string, string?> Env(params (string Name, string Value)[] variables) =>
        name => variables.FirstOrDefault(v => v.Name == name).Value;

    public static TheoryData<string?, string, string> LinuxCases => new()
    {
        // XDG_DATA_HOME, home → the game folder
        { null, "/home/player", "/home/player/.local/share/stat-fick" },
        { "", "/home/player", "/home/player/.local/share/stat-fick" },
        { "/data", "/home/player", "/data/stat-fick" },
        { "/data/", "/home/player", "/data/stat-fick" },
        { "/", "/home/player", "/stat-fick" },
        // A relative XDG_DATA_HOME is ignored (the XDG base directory specification).
        { "data", "/home/player", "/home/player/.local/share/stat-fick" },
        { "~/data", "/home/player", "/home/player/.local/share/stat-fick" },
        { null, "/home/player/", "/home/player/.local/share/stat-fick" },
        { null, "/", "/.local/share/stat-fick" },
    };

    [Theory]
    [MemberData(nameof(LinuxCases))]
    public void OnLinuxItIsInTheXdgDataFolder(string? dataHome, string home, string expected)
    {
        var environment = dataHome == null ? Env() : Env(("XDG_DATA_HOME", dataHome));
        Assert.Equal(expected, GameFolder.Locate(StorageSystem.Linux, environment, home, roamingAppData: null));
        // Windows' folder plays no part on Linux.
        Assert.Equal(expected, GameFolder.Locate(StorageSystem.Linux, environment, home, @"C:\Users\Player\AppData\Roaming"));
    }

    [Theory]
    [InlineData(@"C:\Users\Player\AppData\Roaming", @"C:\Users\Player\AppData\Roaming\Stat Fick")]
    [InlineData(@"C:\Users\Player\AppData\Roaming\", @"C:\Users\Player\AppData\Roaming\Stat Fick")]
    [InlineData(@"D:\Profiles\Ünïcode Name\AppData\Roaming", @"D:\Profiles\Ünïcode Name\AppData\Roaming\Stat Fick")]
    public void OnWindowsItIsInTheRoamingAppDataFolder(string appData, string expected)
    {
        // XDG_DATA_HOME and the home folder play no part on Windows.
        Assert.Equal(expected, GameFolder.Locate(StorageSystem.Windows, Env(("XDG_DATA_HOME", "/data")), "/home/player", appData));
    }

    [Fact]
    public void WithoutAHomeOrAppDataFolderThereIsNone()
    {
        Assert.Throws<InvalidOperationException>(() => GameFolder.Locate(StorageSystem.Linux, Env(), null, null));
        Assert.Throws<InvalidOperationException>(() => GameFolder.Locate(StorageSystem.Linux, Env(), "", null));
        Assert.Throws<InvalidOperationException>(() => GameFolder.Locate(StorageSystem.Windows, Env(), "/home/player", null));
        Assert.Throws<InvalidOperationException>(() => GameFolder.Locate(StorageSystem.Windows, Env(), "/home/player", ""));
        // XDG_DATA_HOME alone is enough on Linux.
        Assert.Equal("/data/stat-fick", GameFolder.Locate(StorageSystem.Linux, Env(("XDG_DATA_HOME", "/data")), null, null));
    }

    [Fact]
    public void ThisMachinesFolderIsTheSystemsDataFolderAndIsntMade()
    {
        var folder = GameFolder.ForThisMachine();
        if (OperatingSystem.IsWindows())
            Assert.Equal(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData, Environment.SpecialFolderOption.DoNotVerify), "Stat Fick"), folder);
        else
            // .NET's LocalApplicationData follows the same XDG rule.
            Assert.Equal(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.DoNotVerify), "stat-fick"), folder);
    }

    [Fact]
    public void TheFolderHoldsSettingsSavesAndMods()
    {
        using var temp = new StorageFolder();
        using var storage = new GameStorage(temp["game"]);
        Assert.Equal(temp["game"], storage.Folder);
        Assert.Equal(Path.Combine(temp["game"], "settings.json"), storage.SettingsFile);
        Assert.Equal(Path.Combine(temp["game"], "saves"), storage.Saves.Folder);
        Assert.Equal(Path.Combine(temp["game"], "mods"), storage.ModsFolder);
        // Nothing is made until something is written.
        Assert.False(Directory.Exists(temp["game"]));
        Assert.Empty(storage.Saves.List());
        Assert.False(Directory.Exists(temp["game"]));
    }

    [Fact]
    public void FoldersAreMadeForTheUserAlone()
    {
        if (OperatingSystem.IsWindows())
            return;
        using var temp = new StorageFolder();
        using var storage = new GameStorage(temp["a/b/game"]);
        storage.Saves.Save("one", StorageTestData.Sample());
        storage.Flush();
        const UnixFileMode Private = UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute;
        foreach (var folder in new[] { "a", "a/b", "a/b/game", "a/b/game/saves" })
            Assert.Equal(Private, File.GetUnixFileMode(temp[folder]) & ~UnixFileMode.SetGroup);
        Assert.Equal(UnixFileMode.UserRead | UnixFileMode.UserWrite, File.GetUnixFileMode(temp["a/b/game/saves/one.dat"]));
    }

    [Fact]
    public void TheFileManagerOpensTheFolderAsElectronsShellOpenPath()
    {
        var linux = GameStorage.FileManagerStart(StorageSystem.Linux, "/home/player/.local/share/stat-fick");
        Assert.Equal("xdg-open", linux.FileName);
        Assert.Equal(["/home/player/.local/share/stat-fick"], linux.ArgumentList);
        Assert.Equal("/home/player/.local/share", linux.WorkingDirectory);
        Assert.False(linux.UseShellExecute);
        Assert.Equal("1", linux.Environment["MM_NOTTTY"]);
        Assert.Equal("/", GameStorage.FileManagerStart(StorageSystem.Linux, "/stat-fick").WorkingDirectory);

        var windows = GameStorage.FileManagerStart(StorageSystem.Windows, @"C:\Users\Player\AppData\Roaming\Stat Fick");
        Assert.Equal(@"C:\Users\Player\AppData\Roaming\Stat Fick", windows.FileName);
        Assert.True(windows.UseShellExecute);
        Assert.Equal("open", windows.Verb);
        Assert.Empty(windows.ArgumentList);
    }

    [Fact]
    public void OpeningTheFolderMakesItFirst()
    {
        using var temp = new StorageFolder();
        using var storage = new GameStorage(temp["game"]);
        var launched = new List<ProcessStartInfo>();
        storage.Launcher = start =>
        {
            Assert.True(Directory.Exists(temp["game"]));
            launched.Add(start);
            return true;
        };
        Assert.True(storage.OpenInFileManager());
        var start = Assert.Single(launched);
        if (OperatingSystem.IsWindows())
            Assert.Equal(temp["game"], start.FileName);
        else
            Assert.Equal([temp["game"]], start.ArgumentList);
        storage.Launcher = _ => throw new System.ComponentModel.Win32Exception("no file manager");
        Assert.False(storage.OpenInFileManager());
    }
}

using System.Text.Json.Nodes;
using Statventure.Storage;
using Statventure.Tests.Records;

namespace Statventure.Tests.Storage;

/// <summary>
/// What the storage code logs (Q18: a settings file that can't be used is logged; Q43: a setting of
/// the wrong type; failed writes; a missing file manager). Alone in its collection: the log's sink is
/// one for the process.
/// </summary>
[Collection("Storage log")]
public class StorageLogTests
{
    [Fact]
    public void ABrokenSettingsFileIsLogged()
    {
        using var temp = new StorageFolder();
        temp.Write("settings.json", "{oops"u8.ToArray());
        using var storage = new GameStorage(temp.Root);
        var lines = StorageTestData.CaptureLog(() => storage.LoadSettings(SettingsTests.Version, SettingsTests.Upgrades));
        var line = Assert.Single(lines);
        Assert.StartsWith($"Warning: settings {temp["settings.json"]}: it isn't valid JSON", line);
        Assert.EndsWith($"the defaults are used, the file is kept as {temp["settings.broken.json"]}", line);
        // A missing file is no problem.
        Assert.Empty(StorageTestData.CaptureLog(() => storage.LoadSettings(SettingsTests.Version, SettingsTests.Upgrades)));
    }

    [Fact]
    public void ABrokenSettingsFileThatCantBeKeptIsLoggedToo()
    {
        if (OperatingSystem.IsWindows())
            return;
        using var temp = new StorageFolder();
        temp.Write("settings.json", "{oops"u8.ToArray());
        // A folder where settings.broken.json would go: the file can't be moved there.
        temp.Write("settings.broken.json/inside", [1]);
        using var storage = new GameStorage(temp.Root);
        var lines = StorageTestData.CaptureLog(() => Assert.NotNull(storage.LoadSettings(SettingsTests.Version, SettingsTests.Upgrades).Problem));
        Assert.Contains("couldn't be kept as", Assert.Single(lines));
    }

    [Fact]
    public void ASettingOfTheWrongTypeIsLoggedOnce()
    {
        using var temp = new StorageFolder();
        temp.Write("settings.json", """{"version": 3, "musicVolume": "loud"}"""u8.ToArray());
        using var storage = new GameStorage(temp.Root);
        var settings = storage.LoadSettings(SettingsTests.Version, SettingsTests.Upgrades);
        var lines = StorageTestData.CaptureLog(() =>
        {
            settings.Get("musicVolume", 0.7);
            settings.Get("musicVolume", 0.7);
            settings.Get("absent", 1);
        });
        Assert.Equal([$"Warning: settings {temp["settings.json"]}: \"musicVolume\" holds \"loud\", not a Double; its default is used"], lines);
        // Setting it clears the warning: read as another type, it is logged again.
        settings.Set("musicVolume", 0.5);
        Assert.Empty(StorageTestData.CaptureLog(() => settings.Get("musicVolume", 0.7)));
        Assert.Single(StorageTestData.CaptureLog(() => settings.Get("musicVolume", "text")));
        Assert.Empty(StorageTestData.CaptureLog(() => settings.Get<JsonNode?>("musicVolume", null)));
    }

    [Fact]
    public void AFailedWriteIsLogged()
    {
        using var temp = new StorageFolder();
        temp.Write("saves", [0]);
        using var storage = new GameStorage(temp.Root);
        var lines = StorageTestData.CaptureLog(() =>
        {
            storage.Saves.Save("hero", new Creature { Name = "x" });
            storage.Flush();
        });
        Assert.StartsWith($"Error: writing {temp["saves/hero.dat"]} failed: ", Assert.Single(lines));
    }

    [Fact]
    public void AFileManagerThatCantBeOpenedIsLogged()
    {
        using var temp = new StorageFolder();
        using var storage = new GameStorage(temp.Root);
        storage.Launcher = _ => throw new System.ComponentModel.Win32Exception("no xdg-open");
        var lines = StorageTestData.CaptureLog(() => Assert.False(storage.OpenInFileManager()));
        Assert.Equal([$"Warning: the file manager couldn't be opened on {temp.Root}: no xdg-open"], lines);
    }
}

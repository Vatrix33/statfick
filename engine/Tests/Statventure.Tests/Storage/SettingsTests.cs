using System.Text;
using System.Text.Json.Nodes;
using Statventure.Storage;

namespace Statventure.Tests.Storage;

public enum BattlePace { Slow, Normal, Fast, VeryFast }

public sealed record WindowPlace(int X, int Y, bool Maximized);

/// <summary>
/// The settings file (Docs/Assets.md, "Settings"; Q18, Q42, Q43): defaults for missing keys, unknown
/// keys kept, the committed files of past versions (Data/Storage/Settings) upgraded, typed values,
/// writing on change only, the exact text written, and every kind of file that can't be used kept as
/// settings.broken.json with the defaults used. The test settings are at version 3: version 2 turned
/// the volume in percent into "musicVolume" from 0 to 1, version 3 renamed "pace" to "battlePace".
/// </summary>
public class SettingsTests
{
    public const int Version = 3;

    public static readonly Dictionary<int, Action<JsonObject>> Upgrades = new()
    {
        [1] = settings =>
        {
            settings["musicVolume"] = settings["volume"]!.GetValue<int>() / 100.0;
            settings.Remove("volume");
        },
        [2] = settings =>
        {
            var pace = settings["pace"];
            settings.Remove("pace");
            settings["battlePace"] = pace;
        },
    };

    static Settings Load(GameStorage storage) => storage.LoadSettings(Version, Upgrades);

    static string Text(StorageFolder temp, string relative) => File.ReadAllText(temp[relative]);

    [Fact]
    public async Task LoadingWaitsForAPendingChange()
    {
        using var temp = new StorageFolder();
        using var storage = new GameStorage(temp.Root);
        using var held = new HeldWriter(storage);
        Load(storage).Set("musicVolume", 0.25);
        held.WaitUntilHeld();
        var again = Task.Run(() => Load(storage));
        Thread.Sleep(200);
        Assert.False(again.IsCompleted);
        held.Release();
        Assert.Equal(0.25, (await again).Get("musicVolume", 0.7));
    }

    [Fact]
    public void AMissingFileGivesTheDefaultsAndIsWrittenOnTheFirstChange()
    {
        using var temp = new StorageFolder();
        // The game folder itself is made by the first change.
        using var storage = new GameStorage(temp["game"]);
        var settings = Load(storage);
        Assert.Null(settings.Problem);
        Assert.Equal(3, settings.Version);
        Assert.Equal(storage.SettingsFile, settings.File);
        Assert.False(settings.Contains("musicVolume"));
        Assert.Equal(0.7, settings.Get("musicVolume", 0.7));
        Assert.Equal(BattlePace.Normal, settings.Get("battlePace", BattlePace.Normal));
        storage.Flush();
        Assert.Empty(temp.Files());
        settings.Set("musicVolume", 0.5);
        Assert.True(settings.Contains("musicVolume"));
        Assert.Equal(0.5, settings.Get("musicVolume", 0.7));
        storage.Flush();
        Assert.Equal(["game/settings.json"], temp.Files());
        Assert.Equal("{\n  \"version\": 3,\n  \"musicVolume\": 0.5\n}\n", Text(temp, "game/settings.json"));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void APastVersionIsUpgraded(int version)
    {
        using var temp = new StorageFolder();
        var original = StorageTestData.Read($"Settings/settings-{version}.json");
        temp.Write("settings.json", original);
        using var storage = new GameStorage(temp.Root);
        var settings = Load(storage);
        Assert.Null(settings.Problem);
        Assert.Equal(0.8, settings.Get("musicVolume", 0.0));
        Assert.Equal(BattlePace.Fast, settings.Get("battlePace", BattlePace.Slow));
        Assert.False(settings.Contains("volume"));
        Assert.False(settings.Contains("pace"));
        Assert.False(settings.Contains("version"));
        // A key this game doesn't know is kept.
        Assert.Equal("{\"kept\":[true,2.5]}", settings.Get<JsonNode?>("fromAMod", null)!.ToJsonString());
        // Reading writes nothing; the next change writes the upgraded settings.
        storage.Flush();
        Assert.Equal(original, File.ReadAllBytes(temp["settings.json"]));
        settings.Set("zoom", 1.25);
        storage.Flush();
        var written = JsonNode.Parse(Text(temp, "settings.json"))!.AsObject();
        Assert.Equal(["version", .. version == 1 ? new[] { "fromAMod", "musicVolume" } : ["musicVolume", "fromAMod"], "battlePace", "zoom"],
            written.Select(p => p.Key));
        Assert.Equal(3, written["version"]!.GetValue<int>());
        if (version == Version)
            Assert.Equal(Encoding.UTF8.GetString(original).Replace("\n}\n", ",\n  \"zoom\": 1.25\n}\n"), Text(temp, "settings.json"));
    }

    [Fact]
    public void ValuesOfEveryKindRoundTrip()
    {
        using var temp = new StorageFolder();
        using (var storage = new GameStorage(temp.Root))
        {
            var settings = Load(storage);
            settings.Set("flag", true);
            settings.Set("count", 42);
            settings.Set("ratio", 0.1);
            settings.Set("tiny", 5e-324);
            settings.Set("name", "Héros ✦ 😀");
            settings.Set("pace", BattlePace.VeryFast);
            settings.Set("window", new WindowPlace(-3, 40, true));
            settings.Set("list", new[] { 1, 2, 3 });
        }
        Assert.Equal("""
            {
              "version": 3,
              "flag": true,
              "count": 42,
              "ratio": 0.1,
              "tiny": 5E-324,
              "name": "Héros ✦ \uD83D\uDE00",
              "pace": "veryFast",
              "window": {
                "x": -3,
                "y": 40,
                "maximized": true
              },
              "list": [
                1,
                2,
                3
              ]
            }

            """.Replace("\r", ""), Text(temp, "settings.json"));
        using var again = new GameStorage(temp.Root);
        var read = Load(again);
        Assert.True(read.Get("flag", false));
        Assert.Equal(42, read.Get("count", 0));
        Assert.Equal(0.1, read.Get("ratio", 0.0));
        Assert.Equal(5e-324, read.Get("tiny", 0.0));
        Assert.Equal("Héros ✦ 😀", read.Get("name", ""));
        Assert.Equal(BattlePace.VeryFast, read.Get("pace", BattlePace.Slow));
        Assert.Equal(new WindowPlace(-3, 40, true), read.Get("window", new WindowPlace(0, 0, false)));
        Assert.Equal([1, 2, 3], read.Get("list", Array.Empty<int>()));
    }

    [Fact]
    public void AValueOfTheWrongTypeGivesItsDefaultAndTheRestAreUsed()
    {
        using var temp = new StorageFolder();
        temp.Write("settings.json", """{"version": 3, "musicVolume": "loud", "count": 1.5, "pace": "sideways", "number": 2, "none": null, "battlePace": "slow", "ratio": "0.5"}"""u8.ToArray());
        using var storage = new GameStorage(temp.Root);
        var settings = Load(storage);
        Assert.Null(settings.Problem);
        Assert.Equal(0.7, settings.Get("musicVolume", 0.7));
        Assert.Equal(7, settings.Get("count", 7));
        Assert.Equal(BattlePace.Normal, settings.Get("pace", BattlePace.Normal));
        // Enums only by name (Q32's rule for records), numbers not as strings.
        Assert.Equal(BattlePace.Normal, settings.Get("number", BattlePace.Normal));
        Assert.Equal("x", settings.Get("number", "x"));
        Assert.Equal("dflt", settings.Get<string>("none", "dflt"));
        // Numbers aren't read from strings.
        Assert.Equal(0.7, settings.Get("ratio", 0.7));
        Assert.Equal(BattlePace.Slow, settings.Get("battlePace", BattlePace.Fast));
        // The bad value stays in the file until the setting is set.
        settings.Set("other", 1);
        storage.Flush();
        Assert.Contains("\"musicVolume\": \"loud\"", Text(temp, "settings.json"));
        settings.Set("musicVolume", 0.25);
        Assert.Equal(0.25, settings.Get("musicVolume", 0.7));
    }

    [Fact]
    public void OnlyAChangeIsWritten()
    {
        using var temp = new StorageFolder();
        using var storage = new GameStorage(temp.Root);
        var writes = 0;
        storage.Writer.BeforeWork = _ => Interlocked.Increment(ref writes);
        var settings = Load(storage);
        settings.Set("volume", 0.5);
        storage.Flush();
        settings.Set("volume", 0.5);
        settings.Set("window", new WindowPlace(1, 2, false));
        storage.Flush();
        settings.Set("window", new WindowPlace(1, 2, false));
        storage.Flush();
        Assert.Equal(2, writes);
        settings.Set("window", new WindowPlace(1, 2, true));
        storage.Flush();
        Assert.Equal(3, writes);
    }

    [Fact]
    public void TheLatestSettingsWinAndNoTemporaryFileStays()
    {
        using var temp = new StorageFolder();
        using var storage = new GameStorage(temp.Root);
        var writes = 0;
        using var held = new HeldWriter(storage, seen: _ => Interlocked.Increment(ref writes));
        var settings = Load(storage);
        settings.Set("volume", 0.1);
        held.WaitUntilHeld();
        for (var i = 2; i <= 9; i++)
            settings.Set("volume", i / 10.0);
        held.Release();
        storage.Flush();
        Assert.Equal(2, writes);
        Assert.Equal(["settings.json"], temp.Files());
        Assert.Equal("{\n  \"version\": 3,\n  \"volume\": 0.9\n}\n", Text(temp, "settings.json"));
        if (!OperatingSystem.IsWindows())
            Assert.Equal(UnixFileMode.UserRead | UnixFileMode.UserWrite, File.GetUnixFileMode(temp["settings.json"]));
    }

    [Fact]
    public void TheFileIsReplacedRatherThanRewritten()
    {
        // On Linux a reader of the old file keeps reading it: the new one was renamed over it.
        if (OperatingSystem.IsWindows())
            return;
        using var temp = new StorageFolder();
        using var storage = new GameStorage(temp.Root);
        var settings = Load(storage);
        settings.Set("volume", 0.1);
        storage.Flush();
        var old = File.ReadAllBytes(storage.SettingsFile);
        using var reader = new FileStream(storage.SettingsFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        settings.Set("volume", 0.2);
        storage.Flush();
        var seen = new byte[old.Length + 100];
        Assert.Equal(old, seen[..reader.Read(seen)]);
        Assert.Contains("0.2", Text(temp, "settings.json"));
    }

    public static TheoryData<string, string, string> BrokenFiles => new()
    {
        { "not JSON", "volume = 3", "isn't valid JSON" },
        { "empty", "", "isn't valid JSON" },
        { "cut short", "{\"version\": 3, \"a\": ", "isn't valid JSON" },
        { "a trailing comma", "{\"version\": 3,}", "isn't valid JSON" },
        { "a comment", "{\"version\": 3 /* a comment */}", "isn't valid JSON" },
        { "a repeated key", "{\"version\": 3, \"a\": 1, \"a\": 2}", "isn't valid JSON" },
        { "two values", "{\"version\": 3} {}", "isn't valid JSON" },
        { "an array", "[{\"version\": 3}]", "isn't a JSON object" },
        { "a number", "3", "isn't a JSON object" },
        { "null", "null", "isn't a JSON object" },
        { "no version", "{\"volume\": 3}", "has no \"version\"" },
        { "a version as text", "{\"version\": \"3\"}", "isn't a whole number from 1" },
        { "a fractional version", "{\"version\": 2.5}", "isn't a whole number from 1" },
        { "version 0", "{\"version\": 0}", "isn't a whole number from 1" },
        { "a negative version", "{\"version\": -1}", "isn't a whole number from 1" },
        { "a huge version", "{\"version\": 99999999999}", "isn't a whole number from 1" },
        { "a null version", "{\"version\": null}", "isn't a whole number from 1" },
        // Q42: a newer game's settings are treated as not parsing.
        { "a newer version", "{\"version\": 4, \"musicVolume\": 0.3}", "its version 4 is newer than this game's (3)" },
        // An upgrade that fails on what it finds.
        { "an upgrade failing", "{\"version\": 1, \"volume\": \"loud\"}", "its upgrade from version 1 failed" },
    };

    [Theory]
    [MemberData(nameof(BrokenFiles))]
    public void AFileThatCantBeUsedGivesTheDefaultsAndIsKept(string kind, string content, string problem)
    {
        using var temp = new StorageFolder();
        var bytes = Encoding.UTF8.GetBytes(content);
        temp.Write("settings.json", bytes);
        // An older broken file is replaced by the newer one.
        temp.Write("settings.broken.json", "older"u8.ToArray());
        using var storage = new GameStorage(temp.Root);
        var settings = Load(storage);
        Assert.True(settings.Problem?.Contains(problem) == true, $"{kind}: {settings.Problem}");
        Assert.False(settings.Contains("musicVolume"));
        Assert.Equal(0.7, settings.Get("musicVolume", 0.7));
        Assert.Equal(["settings.broken.json"], temp.Files());
        Assert.Equal(bytes, File.ReadAllBytes(temp["settings.broken.json"]));
        settings.Set("musicVolume", 0.4);
        storage.Flush();
        Assert.Equal("{\n  \"version\": 3,\n  \"musicVolume\": 0.4\n}\n", Text(temp, "settings.json"));
        Assert.Equal(bytes, File.ReadAllBytes(temp["settings.broken.json"]));
    }

    [Fact]
    public void AByteOrderMarkIsAllowed()
    {
        using var temp = new StorageFolder();
        temp.Write("settings.json", [0xEF, 0xBB, 0xBF, .. """{"version": 3, "musicVolume": 0.3}"""u8]);
        using var storage = new GameStorage(temp.Root);
        var settings = Load(storage);
        Assert.Null(settings.Problem);
        Assert.Equal(0.3, settings.Get("musicVolume", 0.7));
    }

    [Fact]
    public void AFileThatCantBeReadIsBroken()
    {
        using var temp = new StorageFolder();
        Directory.CreateDirectory(temp["settings.json"]);
        using var storage = new GameStorage(temp.Root);
        var settings = Load(storage);
        Assert.Contains("can't be read", settings.Problem);
        Assert.Equal(0.7, settings.Get("musicVolume", 0.7));
    }

    [Fact]
    public void TheGameMustGiveEveryUpgradeAndAVersionFromOne()
    {
        using var temp = new StorageFolder();
        using var storage = new GameStorage(temp.Root);
        Assert.Throws<ArgumentException>(() => storage.LoadSettings(3, new Dictionary<int, Action<JsonObject>> { [1] = _ => { } }));
        Assert.Throws<ArgumentException>(() => storage.LoadSettings(2));
        Assert.Throws<ArgumentOutOfRangeException>(() => storage.LoadSettings(0));
        Assert.Null(storage.LoadSettings(1).Problem);
    }

    [Fact]
    public void TheVersionIsntASetting()
    {
        using var temp = new StorageFolder();
        using var storage = new GameStorage(temp.Root);
        var settings = Load(storage);
        Assert.Throws<ArgumentException>(() => settings.Set("version", 4));
        Assert.Throws<ArgumentException>(() => settings.Set("", 4));
        // A value JSON can't hold is refused on the caller's thread, and nothing changes.
        Assert.ThrowsAny<ArgumentException>(() => settings.Set("volume", double.NaN));
        Assert.False(settings.Contains("volume"));
        storage.Flush();
        Assert.Empty(temp.Files());
    }

    [Fact]
    public void TheBrokenFilesNameFollowsTheSettingsFile()
    {
        Assert.Equal(Path.Combine("a", "settings.broken.json"), Settings.BrokenPath(Path.Combine("a", "settings.json")));
        Assert.Equal(Path.Combine("a", "other.broken.txt"), Settings.BrokenPath(Path.Combine("a", "other.txt")));
    }
}

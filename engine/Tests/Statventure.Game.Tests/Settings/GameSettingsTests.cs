using Statventure.Game.Settings;
using Statventure.Storage;

namespace Statventure.Game.Tests;

/// <summary>
/// The game's settings (GM1-P6; Docs/Game/Foundation.md, "<c>settings.json</c>"; Q19, Q22, GM1-4, GM1-12, GM1-13): each
/// key's first-start default, read from the file, a wrong type and an out-of-range value giving the default with a log
/// line, unknown keys kept, a key written only when its value changes and <see cref="GameSettings.Changed"/> raised only
/// then, the setters' limits, the mods' switches and the recent places, and <see cref="GameSettings.Peek"/> reading the
/// file as <see cref="GameSettings.Load"/> does while writing nothing. All on scratch folders.
/// </summary>
[Collection("Game log")]
public class GameSettingsTests
{
    sealed class Folder : IDisposable
    {
        readonly ScratchFolder _scratch = new();

        public Folder() => Storage = new GameStorage(_scratch["p"]);

        public GameStorage Storage { get; }

        public string Path => Storage.Folder;

        public string File => Storage.SettingsFile;

        public void Write(string json)
        {
            Directory.CreateDirectory(Path);
            System.IO.File.WriteAllText(File, json);
        }

        public string Text()
        {
            Storage.Flush();
            return System.IO.File.ReadAllText(File);
        }

        public GameSettings Load() => GameSettings.Load(Storage);

        public void Dispose()
        {
            Storage.Dispose();
            _scratch.Dispose();
        }
    }

    /// <summary>Every value of the settings, to compare two at once.</summary>
    static object Values(GameSettings s) =>
        (s.MasterVolume, s.MusicVolume, s.EffectsVolume, s.Theme, s.ZoomLevel, s.Fullscreen, s.Window, s.DevMode, s.LastCharacter,
            string.Join(",", new[] { "a.pak", "b.pak", "c.pak", "d.pak" }.Where(m => !s.IsModOn(m))), string.Join(",", s.RecentPlaces));

    static readonly object FirstStart = (1.0, 1.0, 0.6, GameTheme.Dark, 0.0, false, (WindowState?)null, false, (string?)null, "", "");

    [Fact]
    public void AFirstStartHasTheDesignsDefaultsAndWritesNothing()
    {
        using var folder = new Folder();
        var settings = folder.Load();
        // Q22: effects at 60 %, the master and music volumes at 100 %; zoom 100 %, windowed, the window never kept;
        // dark (GM1-4); Dev Mode off (GM1-12); no last character; every mod on; no recent places.
        Assert.Equal(FirstStart, Values(settings));
        Assert.Equal((1.0, 1.0, 0.6), (GameSettings.FirstMasterVolume, GameSettings.FirstMusicVolume, GameSettings.FirstEffectsVolume));
        Assert.True(settings.IsModOn("anything.pak"));
        folder.Storage.Flush();
        Assert.False(File.Exists(folder.File));
        Assert.Equal(SettingsSnapshot.Defaults, GameSettings.Peek(folder.Path));
    }

    const string EveryKey = """
        {
          "version": 1,
          "masterVolume": 0.25,
          "musicVolume": 0,
          "effectsVolume": 1,
          "theme": "light",
          "zoomLevel": -1.5,
          "fullscreen": true,
          "window": { "width": 1000, "height": 700, "maximized": true },
          "devMode": true,
          "lastCharacter": "ser-alrik",
          "mods": { "a.pak": { "on": false }, "b.pak": { "on": true }, "c.pak": { }, "d.pak": { "on": false } },
          "recentPlaces": [ "/srv/one", "/srv/two" ]
        }
        """;

    [Fact]
    public void EachKeyIsReadFromTheFile()
    {
        using var folder = new Folder();
        folder.Write(EveryKey);
        var log = GameTesting.CaptureLog(() => Assert.Equal(
            (0.25, 0.0, 1.0, GameTheme.Light, -1.5, true, (WindowState?)new WindowState(1000, 700, true), true, (string?)"ser-alrik", "a.pak,d.pak", "/srv/one,/srv/two"),
            Values(folder.Load())));
        Assert.Empty(log);
        Assert.Equal(new SettingsSnapshot(new WindowState(1000, 700, true), true, new HashSet<string> { "a.pak", "d.pak" }), GameSettings.Peek(folder.Path));
        // A missing "maximized" is false; the mods' order and the "on": true entries make no difference.
        folder.Write("""{ "version": 1, "window": { "width": 900, "height": 600 } }""");
        Assert.Equal(new WindowState(900, 600, false), folder.Load().Window);
    }

    /// <summary>Each key with a value of the wrong type, and the setting it gives.</summary>
    public static TheoryData<string, string> WrongTypes => new()
    {
        { "masterVolume", "\"loud\"" },
        { "musicVolume", "true" },
        { "effectsVolume", "[]" },
        { "theme", "\"blue\"" },
        { "theme", "1" },
        { "zoomLevel", "\"big\"" },
        { "fullscreen", "\"yes\"" },
        { "window", "{ \"width\": \"wide\", \"height\": 900 }" },
        { "window", "{ \"width\": 1000.5, \"height\": 900 }" },
        { "window", "5" },
        { "devMode", "1" },
        { "lastCharacter", "7" },
        { "mods", "[]" },
        { "mods", "{ \"a.pak\": { \"on\": \"no\" } }" },
        { "mods", "{ \"a.pak\": false }" },
        { "recentPlaces", "\"/srv\"" },
        { "recentPlaces", "[ 1 ]" },
    };

    [Theory]
    [MemberData(nameof(WrongTypes))]
    public void AValueOfTheWrongTypeGivesItsDefaultLogged(string key, string json)
    {
        using var folder = new Folder();
        var text = EveryKey.Replace($"\"{key}\": ", $"\"{key}\": {json}, \"was-{key}\": ");
        folder.Write(text);
        GameSettings? settings = null;
        var log = GameTesting.CaptureLog(() => settings = folder.Load());
        Assert.Equal(Expected(key), Values(settings!));
        Assert.Contains(log, line => line.StartsWith($"Warning [Storage] settings {folder.File}: \"{key}\" holds ", StringComparison.Ordinal));
        // The value stays in the file until the setting changes (Q43 of M9); the other settings are used.
        folder.Storage.Flush();
        Assert.Equal(text, File.ReadAllText(folder.File));
    }

    /// <summary>The values <see cref="EveryKey"/> gives with <paramref name="key"/> at its default.</summary>
    static object Expected(string key) =>
        (key == "masterVolume" ? 1.0 : 0.25, key == "musicVolume" ? 1.0 : 0.0, key == "effectsVolume" ? 0.6 : 1.0,
            key == "theme" ? GameTheme.Dark : GameTheme.Light, key == "zoomLevel" ? 0.0 : -1.5, key != "fullscreen",
            key == "window" ? null : (WindowState?)new WindowState(1000, 700, true), key != "devMode", key == "lastCharacter" ? null : (string?)"ser-alrik",
            key == "mods" ? "" : "a.pak,d.pak", key == "recentPlaces" ? "" : "/srv/one,/srv/two");

    /// <summary>Each key with a value of the right type outside its range.</summary>
    public static TheoryData<string, string, string> OutOfRange => new()
    {
        { "masterVolume", "1.5", "from 0 to 1" },
        { "masterVolume", "-0.01", "from 0 to 1" },
        { "musicVolume", "-0.1", "from 0 to 1" },
        { "effectsVolume", "2", "from 0 to 1" },
        { "zoomLevel", "0.25", "a half step whose zoom is from 0.5 to 3 times" },
        { "zoomLevel", "6.5", "a half step whose zoom is from 0.5 to 3 times" },
        { "zoomLevel", "-4", "a half step whose zoom is from 0.5 to 3 times" },
        { "window", "{ \"width\": 819, \"height\": 700 }", "a size from 820 × 600 to 16384 × 16384" },
        { "window", "{ \"width\": 1000, \"height\": 599 }", "a size from 820 × 600 to 16384 × 16384" },
        { "window", "{ \"width\": 16385, \"height\": 700 }", "a size from 820 × 600 to 16384 × 16384" },
        { "window", "{ \"width\": 1000, \"height\": 16385 }", "a size from 820 × 600 to 16384 × 16384" },
        { "window", "{ \"maximized\": true }", "a size from 820 × 600 to 16384 × 16384" },
        { "lastCharacter", "\"Ser Alrik\"", "a slot's name" },
        { "lastCharacter", "\"\"", "a slot's name" },
        { "lastCharacter", "\"con\"", "a slot's name" },
    };

    [Theory]
    [MemberData(nameof(OutOfRange))]
    public void AValueOutOfItsRangeGivesItsDefaultLogged(string key, string json, string range)
    {
        using var folder = new Folder();
        var text = EveryKey.Replace($"\"{key}\": ", $"\"{key}\": {json}, \"was-{key}\": ");
        folder.Write(text);
        GameSettings? settings = null;
        var log = GameTesting.CaptureLog(() => settings = folder.Load());
        Assert.Equal(Expected(key), Values(settings!));
        Assert.Single(log);
        Assert.StartsWith($"Warning [Settings] settings {folder.File}: \"{key}\" holds ", log[0], StringComparison.Ordinal);
        Assert.EndsWith($", not {range}; its default is used", log[0], StringComparison.Ordinal);
        folder.Storage.Flush();
        Assert.Equal(text, File.ReadAllText(folder.File));
    }

    [Fact]
    public void AValueAtTheEndsOfItsRangeIsKept()
    {
        var cases = new (string Key, string Json, Func<GameSettings, object?> Read, object? Expected)[]
        {
            ("masterVolume", "0", s => s.MasterVolume, 0.0),
            ("musicVolume", "0", s => s.MusicVolume, 0.0),
            ("effectsVolume", "1", s => s.EffectsVolume, 1.0),
            ("effectsVolume", "0", s => s.EffectsVolume, 0.0),
            ("zoomLevel", "-3.5", s => s.ZoomLevel, -3.5),
            ("zoomLevel", "6", s => s.ZoomLevel, 6.0),
            ("zoomLevel", "0.5", s => s.ZoomLevel, 0.5),
            ("window", "{ \"width\": 820, \"height\": 600 }", s => s.Window, new WindowState(820, 600, false)),
            ("window", "{ \"width\": 16384, \"height\": 16384 }", s => s.Window, new WindowState(16384, 16384, false)),
            ("lastCharacter", "\"character-2\"", s => s.LastCharacter, "character-2"),
        };
        foreach (var (key, json, read, expected) in cases)
        {
            using var folder = new Folder();
            folder.Write($"{{ \"version\": 1, \"{key}\": {json} }}");
            GameSettings? settings = null;
            var log = GameTesting.CaptureLog(() => settings = folder.Load());
            Assert.Empty(log);
            Assert.Equal(expected, read(settings!));
        }
    }

    [Fact]
    public void UnknownKeysAreKeptAndAChangedValueWritesOnlyItsKey()
    {
        using var folder = new Folder();
        folder.Write("""{ "battlePace": "slow", "version": 1, "future": { "x": [1, 2] }, "theme": "dark" }""");
        var settings = folder.Load();
        settings.Theme = GameTheme.Light;
        settings.MasterVolume = 0.5;
        Assert.Equal("""
            {
              "version": 1,
              "battlePace": "slow",
              "future": {
                "x": [
                  1,
                  2
                ]
              },
              "theme": "light",
              "masterVolume": 0.5
            }

            """, folder.Text());
    }

    [Fact]
    public void ASetThatChangesAValueWritesItsKeyAndSaysSo()
    {
        using var folder = new Folder();
        var settings = folder.Load();
        var changes = new List<SettingKey>();
        settings.Changed += changes.Add;
        settings.MasterVolume = 0.5;
        settings.MusicVolume = 0.25;
        settings.EffectsVolume = 0.75;
        settings.Theme = GameTheme.Light;
        settings.ZoomLevel = 1.5;
        settings.Fullscreen = true;
        settings.Window = new WindowState(1200, 900, true);
        settings.DevMode = true;
        settings.LastCharacter = "ser-alrik";
        settings.SetModOn("ember-rite.pak", false);
        settings.AddRecentPlace("/srv/saves");
        Assert.Equal(Enum.GetValues<SettingKey>(), changes);
        Assert.Equal("""
            {
              "version": 1,
              "masterVolume": 0.5,
              "musicVolume": 0.25,
              "effectsVolume": 0.75,
              "theme": "light",
              "zoomLevel": 1.5,
              "fullscreen": true,
              "window": {
                "width": 1200,
                "height": 900,
                "maximized": true
              },
              "devMode": true,
              "lastCharacter": "ser-alrik",
              "mods": {
                "ember-rite.pak": {
                  "on": false
                }
              },
              "recentPlaces": [
                "/srv/saves"
              ]
            }

            """, folder.Text());
        // Read back: every value as set.
        Assert.Equal((0.5, 0.25, 0.75, GameTheme.Light, 1.5, true, (WindowState?)new WindowState(1200, 900, true), true, (string?)"ser-alrik", "", "/srv/saves"),
            Values(folder.Load()));
        Assert.False(folder.Load().IsModOn("ember-rite.pak"));
    }

    [Fact]
    public void ASetThatDoesntChangeAValueDoesNothing()
    {
        using var folder = new Folder();
        var settings = folder.Load();
        var changes = new List<SettingKey>();
        settings.Changed += changes.Add;
        settings.MasterVolume = 1;
        settings.MusicVolume = 1;
        settings.EffectsVolume = 0.6;
        settings.Theme = GameTheme.Dark;
        settings.ZoomLevel = 0;
        settings.Fullscreen = false;
        settings.DevMode = false;
        settings.LastCharacter = null;
        settings.SetModOn("ember-rite.pak", true);
        Assert.Empty(changes);
        folder.Storage.Flush();
        Assert.False(File.Exists(folder.File));
        // Once kept, the same value again: no event, no write.
        settings.Window = new WindowState(1000, 800, false);
        settings.AddRecentPlace("/srv/saves");
        var text = folder.Text();
        File.Delete(folder.File);
        changes.Clear();
        settings.Window = new WindowState(1000, 800, false);
        settings.AddRecentPlace("/srv/saves/");
        settings.SetModOn("x.pak", true);
        Assert.Empty(changes);
        folder.Storage.Flush();
        Assert.False(File.Exists(folder.File));
        Assert.Contains("\"/srv/saves\"", text);
    }

    [Fact]
    public void TheSettersRefuseValuesOutsideTheirRange()
    {
        using var folder = new Folder();
        var settings = folder.Load();
        var changes = new List<SettingKey>();
        settings.Changed += changes.Add;
        foreach (var volume in new[] { -0.01, 1.01, double.NaN, double.PositiveInfinity })
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => settings.MasterVolume = volume);
            Assert.Throws<ArgumentOutOfRangeException>(() => settings.MusicVolume = volume);
            Assert.Throws<ArgumentOutOfRangeException>(() => settings.EffectsVolume = volume);
        }
        foreach (var level in new[] { 0.25, 6.5, -4, double.NaN, double.PositiveInfinity })
            Assert.Throws<ArgumentOutOfRangeException>(() => settings.ZoomLevel = level);
        Assert.Throws<ArgumentOutOfRangeException>(() => settings.Theme = (GameTheme)7);
        Assert.Throws<ArgumentException>(() => settings.LastCharacter = "Ser Alrik");
        Assert.Throws<ArgumentException>(() => settings.LastCharacter = "");
        Assert.Throws<ArgumentNullException>(() => settings.Window = null);
        Assert.Throws<ArgumentException>(() => settings.SetModOn("", false));
        Assert.Empty(changes);
        Assert.Equal(FirstStart, Values(settings));
        folder.Storage.Flush();
        Assert.False(File.Exists(folder.File));
        // The ends of the ranges are allowed.
        settings.MasterVolume = 0;
        settings.ZoomLevel = -3.5;
        settings.ZoomLevel = 6;
        Assert.Equal((0.0, 6.0), (settings.MasterVolume, settings.ZoomLevel));
        Assert.Equal([false, true, true, true, false, false],
            new[] { -4, -3.5, 0, 6, 6.5, 0.1 }.Select(GameSettings.IsZoomLevel));
    }

    [Fact]
    public void AWindowSetIsHeldWithinItsLimits()
    {
        using var folder = new Folder();
        var settings = folder.Load();
        settings.Window = new WindowState(100, 50, true);
        Assert.Equal(new WindowState(StatFickGame.MinWidth, StatFickGame.MinHeight, true), settings.Window);
        settings.Window = new WindowState(20000, 30000, false);
        Assert.Equal(new WindowState(GameSettings.MaxWindowSide, GameSettings.MaxWindowSide, false), settings.Window);
        // What is kept opens the next start's window.
        Assert.Equal(settings.Window, folder.Load().Window);
        Assert.Equal(settings.Window, GameSettings.Peek(folder.Path).Window);
    }

    [Fact]
    public void ClearingTheLastCharacterChangesItInMemoryOnly()
    {
        using var folder = new Folder();
        folder.Write("""{ "version": 1, "lastCharacter": "ser-alrik" }""");
        var settings = folder.Load();
        var changes = new List<SettingKey>();
        settings.Changed += changes.Add;
        settings.LastCharacter = null;
        settings.LastCharacter = null;
        Assert.Null(settings.LastCharacter);
        Assert.Equal([SettingKey.LastCharacter], changes);
        // Nothing written for it (the engine's settings can't make a key absent; a written null would be logged at every
        // start): the file keeps the old name; a later change of another setting writes it with that name, harmless.
        settings.Theme = GameTheme.Light;
        Assert.Contains("\"lastCharacter\": \"ser-alrik\"", folder.Text());
        Assert.Equal("ser-alrik", folder.Load().LastCharacter);
        // Set again to a slot: written.
        settings.LastCharacter = "character-1";
        Assert.Contains("\"lastCharacter\": \"character-1\"", folder.Text());
    }

    [Fact]
    public void TheModsSwitchesAreKeptInTheirFilesOrder()
    {
        using var folder = new Folder();
        var settings = folder.Load();
        var changes = new List<SettingKey>();
        settings.Changed += changes.Add;
        settings.SetModOn("b.pak", false);
        settings.SetModOn("a.pak", false);
        settings.SetModOn("a.pak", false);
        Assert.Contains("\"mods\": {\n    \"a.pak\": {\n      \"on\": false\n    },\n    \"b.pak\": {\n      \"on\": false\n    }\n  }", folder.Text());
        settings.SetModOn("a.pak", true);
        Assert.Contains("\"mods\": {\n    \"b.pak\": {\n      \"on\": false\n    }\n  }", folder.Text());
        Assert.Equal([SettingKey.Mods, SettingKey.Mods, SettingKey.Mods], changes);
        Assert.Equal((true, false), (settings.IsModOn("a.pak"), settings.IsModOn("b.pak")));
        // By the file's exact name.
        Assert.True(settings.IsModOn("B.pak"));
        var again = folder.Load();
        Assert.Equal((true, false), (again.IsModOn("a.pak"), again.IsModOn("b.pak")));
        Assert.Equal(["b.pak"], GameSettings.Peek(folder.Path).ModsOff);
        settings.SetModOn("b.pak", true);
        Assert.Contains("\"mods\": {}", folder.Text());
    }

    [Fact]
    public void TheRecentPlacesAreTheLastTenFoldersTheLatestFirst()
    {
        using var folder = new Folder();
        var settings = folder.Load();
        var changes = new List<SettingKey>();
        settings.Changed += changes.Add;
        for (var i = 1; i <= 12; i++)
            settings.AddRecentPlace($"/srv/{i}");
        Assert.Equal(Enumerable.Range(3, 10).Reverse().Select(i => $"/srv/{i}"), settings.RecentPlaces);
        settings.AddRecentPlace("/srv/7/");
        Assert.Equal("/srv/7", settings.RecentPlaces[0]);
        Assert.Equal(10, settings.RecentPlaces.Count);
        settings.AddRecentPlace("/srv/7");
        Assert.Equal(13, changes.Count);
        Assert.Equal(settings.RecentPlaces, folder.Load().RecentPlaces);
        // A relative folder is made full (the engine's RecentPlaces).
        settings.AddRecentPlace("relative");
        Assert.Equal(System.IO.Path.GetFullPath("relative"), settings.RecentPlaces[0]);
        // More than ten in the file: the first ten.
        folder.Write("{ \"version\": 1, \"recentPlaces\": [" + string.Join(", ", Enumerable.Range(1, 12).Select(i => $"\"/srv/{i}\"")) + "] }");
        Assert.Equal(Enumerable.Range(1, 10).Select(i => $"/srv/{i}"), folder.Load().RecentPlaces);
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("{ \"version\": 2, \"theme\": \"light\", \"fullscreen\": true }")]
    [InlineData("{ \"theme\": \"light\", \"fullscreen\": true }")]
    [InlineData("{ \"version\": 0, \"theme\": \"light\", \"fullscreen\": true }")]
    [InlineData("{ \"version\": -1, \"theme\": \"light\", \"fullscreen\": true }")]
    [InlineData("{ \"version\": 1.5, \"theme\": \"light\", \"fullscreen\": true }")]
    [InlineData("{ \"version\": \"1\", \"theme\": \"light\", \"fullscreen\": true }")]
    [InlineData("[ 1 ]")]
    [InlineData("{ \"version\": 1, \"theme\": \"light\", \"fullscreen\": true, \"fullscreen\": true }")]
    [InlineData("{ \"version\": 1, \"fullscreen\": true ")]
    [InlineData("")]
    public void AFileThatCantBeUsedGivesTheDefaultsAndIsKeptAside(string text)
    {
        using var folder = new Folder();
        folder.Write(text);
        // Peek first: the defaults, the file left where it is.
        Assert.Equal(SettingsSnapshot.Defaults, GameSettings.Peek(folder.Path));
        Assert.Equal(text, File.ReadAllText(folder.File));
        GameSettings? settings = null;
        var log = GameTesting.CaptureLog(() => settings = folder.Load());
        Assert.Equal(FirstStart, Values(settings!));
        Assert.False(File.Exists(folder.File));
        Assert.Equal(text, File.ReadAllText(Statventure.Storage.Settings.BrokenPath(folder.File)));
        Assert.Contains(log, line => line.StartsWith($"Warning [Storage] settings {folder.File}: ", StringComparison.Ordinal));
    }

    /// <summary>Files Peek and Load must read alike.</summary>
    public static TheoryData<string> PeekedFiles => new()
    {
        EveryKey,
        "﻿" + EveryKey,
        """{ "version": 1, "window": { "width": 819, "height": 700 }, "fullscreen": true }""",
        """{ "version": 1, "window": { "width": 1000, "height": 16385, "maximized": true } }""",
        """{ "version": 1, "window": 7, "fullscreen": "yes", "mods": [] }""",
        """{ "version": 1, "window": null, "fullscreen": null, "mods": null }""",
        """{ "version": 1, "mods": { "a.pak": { "on": "no" } } }""",
        """{ "version": 1, "mods": { "z.pak": { "on": false, "later": 3 }, "a.pak": { "on": false } } }""",
        """{ "version": 1, "Fullscreen": true, "Window": { "width": 1000, "height": 700 } }""",
        """{ "version": 1, "window": { "Width": 1000, "Height": 700 } }""",
    };

    [Theory]
    [MemberData(nameof(PeekedFiles))]
    public void PeekReadsTheWindowFullScreenAndModsAsLoadDoes(string text)
    {
        using var folder = new Folder();
        folder.Write(text);
        SettingsSnapshot? peeked = null;
        var log = GameTesting.CaptureLog(() => peeked = GameSettings.Peek(folder.Path));
        Assert.Empty(log);
        var loaded = folder.Load();
        var off = new HashSet<string>(new[] { "a.pak", "b.pak", "c.pak", "d.pak", "z.pak" }.Where(m => !loaded.IsModOn(m)));
        Assert.Equal(new SettingsSnapshot(loaded.Window, loaded.Fullscreen, off), peeked);
        Assert.Equal(off.Order(), peeked!.ModsOff.Order());
    }

    [Fact]
    public void PeekWritesNothingAndMakesNoFolder()
    {
        using var scratch = new ScratchFolder();
        var missing = scratch["no/such/folder"];
        Assert.Equal(SettingsSnapshot.Defaults, GameSettings.Peek(missing));
        Assert.False(Directory.Exists(scratch["no"]));
        // A file it can't read (a folder in its place): the defaults.
        Directory.CreateDirectory(Path.Combine(scratch["p"], GameStorage.SettingsName));
        Assert.Equal(SettingsSnapshot.Defaults, GameSettings.Peek(scratch["p"]));
        Assert.Equal([Path.Combine(scratch["p"], GameStorage.SettingsName)], Directory.EnumerateFileSystemEntries(scratch["p"]));
    }

    [Fact]
    public void ASnapshotEqualsAnotherOfTheSameValues()
    {
        var a = new SettingsSnapshot(new WindowState(1000, 700, false), true, new HashSet<string> { "a.pak" });
        Assert.Equal(a, new SettingsSnapshot(new WindowState(1000, 700, false), true, new HashSet<string> { "a.pak" }));
        Assert.NotEqual(a, new SettingsSnapshot(new WindowState(1000, 700, false), true, new HashSet<string> { "b.pak" }));
        Assert.NotEqual(a, new SettingsSnapshot(new WindowState(1000, 700, false), false, new HashSet<string> { "a.pak" }));
        Assert.NotEqual(a, new SettingsSnapshot(null, true, new HashSet<string> { "a.pak" }));
        Assert.False(a.IsModOn("a.pak"));
        Assert.True(a.IsModOn("b.pak"));
    }
}

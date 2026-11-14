using Statventure.Game.Settings;
using Statventure.Packs;
using Statventure.Storage;

namespace Statventure.Game.Tests;

/// <summary>
/// The application class on the parts' stubs (Docs/Game/Foundation.md, "The seams", "Who calls whom"): its options before
/// <c>Run</c>, the start sequence at <c>Load</c> in memory (no window: a window and volumes of the test's own), the
/// settings followed, the window's state kept, a later start's request taken.
/// </summary>
[Collection("Game log")]
public class StartSequenceTests
{
    [Fact]
    public void TheOptionsBeforeRunAreTheGamesWindowAndLog()
    {
        using var scratch = new ScratchFolder();
        var options = StatFickGame.OptionsFor(SettingsSnapshot.Defaults, scratch["p"]);
        var window = options.Window;
        Assert.Equal(("Stat Fick", 1440, 950, 820, 600, true, true), (window.Title, window.Width, window.Height, window.MinWidth, window.MinHeight, window.Resizable, window.Visible));
        Assert.Equal(("stat-fick", scratch["p"], true, true, false, null), (options.LogName, options.GameFolder, options.Splash, options.Audio, options.GamePacksOnly, options.DevelopmentFolder));
        // The window as the player left it (Q19): its size; maximized and full screen are put in place at Load.
        var remembered = StatFickGame.OptionsFor(new SettingsSnapshot(new WindowState(1000, 700, true), true, new HashSet<string>()), scratch["p"]);
        Assert.Equal((1000, 700), (remembered.Window.Width, remembered.Window.Height));

        // The program's: the settings peeked from the folder, the options through the mods; a crash in a capture run
        // captures the crash window there.
        var capture = new GameOptions { Capture = scratch["shot.png"] };
        using var game = StatFickGame.Create(capture, scratch["p"]);
        Assert.NotNull(game.Options.ModFilter);
        Assert.Equal(StatFickGame.OptionsFor(GameSettings.Peek(scratch["p"]), scratch["p"]) with { ModFilter = game.Options.ModFilter }, game.Options);
        Assert.Equal(GameSettings.Peek(scratch["p"]), game.Snapshot);
        Assert.Equal(scratch["shot.png"], game.CrashCapture);
        Assert.Null(game.Parts);
    }

    [Fact]
    public void TheStartMakesThePartsAttachesEveryScreenThenRunsTheStartScreen()
    {
        using var scratch = new ScratchFolder();
        var record = new List<string>();
        RecordingScreen[] screens = [new("one", record), new("two", record, StatFickGame.StartCommand), new("three", record)];
        using var game = GameTesting.Game(scratch["p"], screens: screens);
        var volumes = new FakeVolumes();
        var window = new FakeWindow();
        game.Volumes = volumes;
        game.GameWindow = window;
        using var storage = new GameStorage(scratch["p"]);
        using var data = new VirtualFileSystem([]);
        GameParts? parts = null;
        var log = GameTesting.CaptureLog(() => parts = game.Compose(storage, data));

        Assert.Equal(["attach one", "attach two", "attach three", "run start-screen"], record);
        Assert.Same(parts, game.Parts);
        Assert.All(screens, s => Assert.Same(parts, s.Parts));
        Assert.Same(game, parts!.Game);
        // The parts as their stubs make them: the first start's settings, no character, the mods, the game's document.
        Assert.Equal((1.0, 1.0, 0.6, GameTheme.Dark, false, null), (parts.Settings.MasterVolume, parts.Settings.MusicVolume, parts.Settings.EffectsVolume, parts.Settings.Theme, parts.Settings.DevMode, parts.Settings.LastCharacter));
        Assert.Null(parts.Session.Current);
        Assert.Empty(parts.Mods.Entries);
        // The mods' switches kept in the run's settings (Q3).
        parts.Mods.Switch("ember-rite.pak", false);
        Assert.False(parts.Settings.IsModOn("ember-rite.pak"));
        Assert.Equal("Stat Fick", parts.Ui.Document.QuerySelector(".game-name")!.TextContent);
        Assert.Null(parts.Ui.Host);
        // The run's randomness, its seed logged.
        Assert.Contains($"Info [Game] the run's random seed {((Random.GameRandom)parts.Random).Seed}", log);
        // The volumes on the sound at the start (Q22); the window as the settings said (nothing to change on a first start).
        Assert.Equal([(1.0, 1.0, 0.6)], volumes.Applied);
        Assert.Empty(window.Done);
    }

    [Fact]
    public void ACommandNotRegisteredYetDoesNothingAndIsLogged()
    {
        using var scratch = new ScratchFolder();
        // The program's own screens, all stubs: no screen registers start-screen yet.
        using var game = GameTesting.Game(scratch["p"], new GameOptions { Open = "settings" });
        using var storage = new GameStorage(scratch["p"]);
        using var data = new VirtualFileSystem([]);
        var log = GameTesting.CaptureLog(() => game.Compose(storage, data));
        Assert.Contains("Info [Commands] start-screen: not registered yet, nothing done", log);
        Assert.Contains("Info [Commands] open-settings: not registered yet, nothing done", log);
        Assert.False(game.Parts!.Ui.Commands.Run("quit"));
    }

    [Fact]
    public void ACommandRunsWhenRegisteredAndPossible()
    {
        using var scratch = new ScratchFolder();
        using var game = GameTesting.Game(scratch["p"], screens: []);
        using var storage = new GameStorage(scratch["p"]);
        using var data = new VirtualFileSystem([]);
        var commands = game.Compose(storage, data).Ui.Commands;
        var ran = new List<string?>();
        var possible = false;
        commands.Register("open-slot", ran.Add);
        commands.Register("continue", _ => ran.Add("continued"), () => possible);
        Assert.True(commands.Run("open-slot", "ser-alrik"));
        Assert.True(commands.Run("open-slot"));
        var log = GameTesting.CaptureLog(() => Assert.False(commands.Run("continue")));
        Assert.Equal(["Info [Commands] continue: not possible now, nothing done"], log);
        Assert.Equal((true, false, false), (commands.IsRegistered("continue"), commands.IsEnabled("continue"), commands.IsRegistered("quit")));
        possible = true;
        Assert.True(commands.IsEnabled("continue"));
        Assert.True(commands.Run("continue"));
        Assert.Equal(["ser-alrik", null, "continued"], ran);
        Assert.Equal(["continue", "open-slot"], commands.Names.Order());
        // One part per command: a second registration is a mistake.
        Assert.Throws<InvalidOperationException>(() => commands.Register("continue", _ => { }));
    }

    [Fact]
    public void TheCommandLinesCommandsRunAfterTheStartScreen()
    {
        using var scratch = new ScratchFolder();
        var record = new List<string>();
        using var game = GameTesting.Game(scratch["p"], new GameOptions { Open = "slots", About = true },
            [new RecordingScreen("all", record, "about", "open-slots", StatFickGame.StartCommand)]);
        using var storage = new GameStorage(scratch["p"]);
        using var data = new VirtualFileSystem([]);
        game.Compose(storage, data);
        Assert.Equal(["attach all", "run start-screen", "run open-slots", "run about"], record);
    }

    [Fact]
    public void AboutShowsTheEnginesCreditsViewUntilStep9()
    {
        using var scratch = new ScratchFolder();
        using var game = GameTesting.Game(scratch["p"], new GameOptions { About = true });
        using var storage = new GameStorage(scratch["p"]);
        using var data = new VirtualFileSystem([]);
        var log = GameTesting.CaptureLog(() => game.Compose(storage, data));
        var document = game.Parts!.Ui.Document;
        var view = document.QuerySelector(".engine-credits");
        Assert.NotNull(view);
        Assert.Equal("Stat Fick", document.QuerySelector(".engine-credits .credits h1")!.TextContent);
        Assert.Equal("Version dev", document.QuerySelector(".engine-credits .credits .version")!.TextContent);
        Assert.Contains("Info [Game] the credits view opened", log);
        // Once at a time; Close takes it away.
        game.Parts.Ui.Commands.Run("about");
        Assert.Single(document.QuerySelectorAll(".engine-credits"));
        document.QuerySelector(".engine-credits .credits-close")!.Click();
        Assert.Null(document.QuerySelector(".engine-credits"));
    }

    [Fact]
    public void TheVolumesFullScreenAndTheWindowFollowTheSettings()
    {
        using var scratch = new ScratchFolder();
        using var game = GameTesting.Game(scratch["p"], screens: []);
        var volumes = new FakeVolumes();
        var window = new FakeWindow();
        game.Volumes = volumes;
        game.GameWindow = window;
        using var storage = new GameStorage(scratch["p"]);
        using var data = new VirtualFileSystem([]);
        var settings = game.Compose(storage, data).Settings;

        settings.MasterVolume = 0.5;
        settings.MusicVolume = 0.25;
        settings.EffectsVolume = 0.75;
        Assert.Equal([(1.0, 1.0, 0.6), (0.5, 1.0, 0.6), (0.5, 0.25, 0.6), (0.5, 0.25, 0.75)], volumes.Applied);
        settings.Fullscreen = true;
        Assert.True(window.Fullscreen);
        settings.Fullscreen = false;
        Assert.False(window.Fullscreen);
        settings.Window = new WindowState(900, 700, true);
        settings.Window = new WindowState(900, 700, false);
        Assert.Equal(["maximize", "restore"], window.Done);
        // Other settings change nothing of the sound or the window.
        settings.Theme = GameTheme.Light;
        settings.DevMode = true;
        Assert.Equal(4, volumes.Applied.Count);
        Assert.Equal(2, window.Done.Count);
    }

    [Fact]
    public void TheSettingsSaidAtTheStartArePutOnTheWindow()
    {
        using var scratch = new ScratchFolder();
        using var game = GameTesting.Game(scratch["p"], screens: [], settingsLoaded: s =>
        {
            s.Window = new WindowState(1000, 700, true);
            s.Fullscreen = true;
        });
        var window = new FakeWindow();
        game.GameWindow = window;
        using var storage = new GameStorage(scratch["p"]);
        using var data = new VirtualFileSystem([]);
        game.Compose(storage, data);
        Assert.Equal(["maximize"], window.Done);
        Assert.True(window.Fullscreen);
    }

    [Fact]
    public void TheWindowsStateIsKeptInTheSettings()
    {
        using var scratch = new ScratchFolder();
        using var game = GameTesting.Game(scratch["p"], screens: []);
        var window = new FakeWindow { Width = 1000, Height = 800 };
        game.GameWindow = window;
        using var storage = new GameStorage(scratch["p"]);
        using var data = new VirtualFileSystem([]);
        var settings = game.Compose(storage, data).Settings;
        var changes = new List<SettingKey>();
        settings.Changed += changes.Add;

        game.KeepWindowState();
        Assert.Equal(new WindowState(1000, 800, false), settings.Window);
        game.KeepWindowState();
        Assert.Equal([SettingKey.Window], changes);
        // Maximized: the size kept is the one it is restored to, and the game's own write isn't put back on the window.
        (window.Maximized, window.Width, window.Height) = (true, 1920, 1080);
        game.KeepWindowState();
        Assert.Equal(new WindowState(1000, 800, true), settings.Window);
        (window.Maximized, window.Width, window.Height) = (false, 1000, 800);
        game.KeepWindowState();
        Assert.Equal(new WindowState(1000, 800, false), settings.Window);
        Assert.Empty(window.Done);
        // Minimized or full screen: nothing kept.
        (window.Minimized, window.Width, window.Height) = (true, 0, 0);
        game.KeepWindowState();
        (window.Minimized, window.Fullscreen, window.Width, window.Height) = (false, true, 2560, 1440);
        game.KeepWindowState();
        Assert.Equal(new WindowState(1000, 800, false), settings.Window);
        // Resized.
        (window.Fullscreen, window.Width, window.Height) = (false, 1200, 900);
        game.KeepWindowState();
        Assert.Equal(new WindowState(1200, 900, false), settings.Window);
    }

    [Fact]
    public void ALaterStartsRequestBringsTheWindowForwardOnce()
    {
        using var scratch = new ScratchFolder();
        using var game = GameTesting.Game(scratch["p"], screens: []);
        var window = new FakeWindow();
        game.GameWindow = window;
        game.TakeForwardRequest();
        Assert.Empty(window.Done);
        game.AskForward();
        var log = GameTesting.CaptureLog(() =>
        {
            game.TakeForwardRequest();
            game.TakeForwardRequest();
        });
        Assert.Equal(["forward"], window.Done);
        Assert.Contains("Info [Game] another start asked for the window: brought forward (the test's window)", log);
    }
}

using Statventure.Game.Session;
using Statventure.Game.Sounds;
using Statventure.Game.Ui;
using Statventure.Platform;
using Statventure.Storage;
using Statventure.UI.Dom;

namespace Statventure.Game.Tests;

/// <summary>
/// The flows across steps 9 and 10 (the manager, 2026-11-14), the seven screens attached as the game attaches them on a
/// scratch per-user folder (<see cref="ScreensHarness"/> with step 10's screens; the picker's places the scratch folder's):
/// the start screen's Load and Settings opening the slot list and the settings screen; the page's ⚙ and the menu's
/// Settings opening it over the page; the menu's Save Character and Load Character and Ctrl+S and Ctrl+O opening the
/// picker; Continue on a damaged slot asking step 10's question, and greyed when its character is deleted in the slot
/// list; the status-line rule against the real slot list (a refusal while it is open on its line alone, one after it
/// closed on the start screen's); the menu's and About's log lines, once per opening; <c>game-opened</c> once per
/// character opened, whichever screen opened it.
/// </summary>
[Collection("Game log")]
public class ScreenFlowTests
{
    static ScreensHarness Seven() => new(1000, 860, stepTen: true);

    /// <summary>A click, then the frames the screens' tasks and the document's microtasks need.</summary>
    static void Click(ScreensHarness s, UiElement element)
    {
        s.H.Click(element);
        Settle(s);
    }

    static void Settle(ScreensHarness s)
    {
        s.H.Frame();
        s.H.Frame();
    }

    static void Escape(ScreensHarness s)
    {
        s.H.Key("Escape", "Escape");
        Settle(s);
    }

    static UiElement MenuItem(ScreensHarness s, string label) =>
        s.Document.QuerySelectorAll("#game-menu .game-menu-item").Single(i => i.ElementChildren.First().TextContent == label);

    /// <summary>A hero with a race and a look (Q26: its sheet opens) made and left in its slot, flushed, so it has a save and a backup on disk; its slot.</summary>
    static string Saved(ScreensHarness s, string name)
    {
        var slot = s.Session.New(name);
        s.Session.Change(ScreensHarness.Ready);
        s.H.Storage.Flush();
        s.Session.Leave();
        s.H.Storage.Flush();
        Settle(s);
        return slot;
    }

    static int Opened(ScreensHarness s) => s.H.Sounds.Count(n => n == SoundNames.Opened);

    [Fact]
    public void TheStartScreensLoadOpensTheSlotListAndItsSettingsTheSettingsScreen()
    {
        using var s = Seven();
        var log = GameTesting.CaptureLog(() => Click(s, s.Q("#start-load")));
        Assert.Same(s.Q("#slots-modal"), s.H.Ui.Windows.Top);
        Assert.Contains("Info [Slots] the slot list opened", log);
        Escape(s);
        Assert.Null(s.H.Ui.Windows.Top);
        log = GameTesting.CaptureLog(() => Click(s, s.Q("#start-settings")));
        Assert.Same(s.Q("#settings-modal"), s.H.Ui.Windows.Top);
        Assert.Contains("Info [Settings] the settings screen opened", log);
        // Over the start screen, which stays.
        Assert.False(s.Q("#start-screen").Hidden);
        Assert.True(s.Start.Showing);
    }

    [Fact]
    public void ThePagesGearAndTheMenusSettingsOpenTheSettingsScreenOverThePage()
    {
        using var s = Seven();
        s.NewHero("Ser Alrik");
        Settle(s);
        Assert.False(s.Q("main.game-page").Hidden);
        var log = GameTesting.CaptureLog(() => Click(s, s.Q("#settings-open-button")));
        Assert.Same(s.Q("#settings-modal"), s.H.Ui.Windows.Top);
        Assert.Contains("Info [Settings] the settings screen opened", log);
        Assert.False(s.Q("main.game-page").Hidden);
        Escape(s);
        Assert.Null(s.H.Ui.Windows.Top);
        Click(s, s.Q("#menu-button"));
        Assert.Same(s.Q("#game-menu"), s.H.Ui.Windows.Top);
        log = GameTesting.CaptureLog(() => Click(s, MenuItem(s, "Settings")));
        // The menu closed, the settings screen its own window over the page.
        Assert.Equal([s.Q("#settings-modal")], s.H.Ui.Windows.Layers);
        Assert.Contains("Info [Settings] the settings screen opened", log);
        Assert.False(s.Q("main.game-page").Hidden);
    }

    [Fact]
    public void TheMenusSaveAndLoadCharacterAndCtrlSAndCtrlOOpenThePicker()
    {
        using var s = Seven();
        s.NewHero("Ser Alrik");
        Settle(s);
        void Opens(string mode, Action open)
        {
            var log = GameTesting.CaptureLog(() =>
            {
                open();
                Settle(s);
            });
            Assert.NotNull(s.Picker!.Open);
            Assert.Equal([s.Document.QuerySelector(".picker-layer")], s.H.Ui.Windows.Layers);
            Assert.Contains($"Info [Picker] the file picker opened to {mode} in {s.Documents}", log);
            Escape(s);
            Assert.Null(s.Picker.Open);
            Assert.Null(s.H.Ui.Windows.Top);
        }
        Opens("save", () =>
        {
            Click(s, s.Q("#menu-button"));
            s.H.Click(MenuItem(s, "Save Character"));
        });
        Opens("load", () =>
        {
            Click(s, s.Q("#menu-button"));
            s.H.Click(MenuItem(s, "Load Character"));
        });
        Opens("save", () => s.H.Key("s", "KeyS", KeyModifiers.Control));
        Opens("load", () => s.H.Key("o", "KeyO", KeyModifiers.Control));
        // The character stayed open throughout.
        Assert.Equal("ser-alrik", s.Session.CurrentSlot);
    }

    [Fact]
    public void ContinueOnADamagedSlotAsksStepTensQuestionAndItsBackupOpensThePage()
    {
        using var s = Seven();
        var slot = Saved(s, "Ser Alrik");
        ScreenParts.Damage(s.H.Storage.Saves.SavePath(slot));
        Assert.True(s.Start.Showing);
        s.H.Sounds.Clear();
        var log = GameTesting.CaptureLog(() => Click(s, s.Q("#start-continue")));
        var question = s.Document.QuerySelector(".damaged-save-layer");
        Assert.NotNull(question);
        Assert.Same(question, s.H.Ui.Windows.Top);
        Assert.Contains("Info [Slots] the damaged-save question for ser-alrik (its backup readable)", log);
        Assert.Null(s.Session.Current);
        Assert.Equal(0, Opened(s));
        Click(s, s.Q(".damaged-save-backup"));
        Assert.Equal("ser-alrik", s.Session.CurrentSlot);
        Assert.False(s.Q("main.game-page").Hidden);
        Assert.False(s.Start.Showing);
        Assert.Equal(1, Opened(s));
    }

    [Fact]
    public void DeletingTheCharacterToContinueInTheSlotListGreysContinue()
    {
        using var s = Seven();
        Saved(s, "Ser Alrik");
        Assert.False(s.Q("#start-continue").IsDisabled);
        Click(s, s.Q("#start-load"));
        Click(s, s.Q(".slot-row[data-slot=\"ser-alrik\"] .slot-delete"));
        Click(s, s.Q(".game-dialog-accept"));
        Assert.Empty(s.Session.Slots());
        // Seen under the slot list, before it closes.
        Assert.Same(s.Q("#slots-modal"), s.H.Ui.Windows.Top);
        Assert.True(s.Q("#start-continue").IsDisabled);
        Assert.False(s.H.Ui.Commands.IsEnabled("continue"));
    }

    [Fact]
    public void ARefusalWhileTheSlotListIsOpenShowsOnItsLineAloneAndOneAfterItClosedOnTheStartScreens()
    {
        using var s = Seven();
        File.WriteAllText(Path.Combine(s.Documents!, "emberfall.json"), "{ \"format\": \"emberfall-stat-block\", \"version\": 22 }");
        void LoadTheWrongFile()
        {
            s.Picker!.Open!.NameField.Value = "emberfall.json";
            Click(s, s.Q("#picker-accept"));
        }
        // The slot list's Load Character…, a file that isn't a character: the list's line says so, the start screen's not.
        Click(s, s.Q("#start-load"));
        Click(s, s.Q("#slots-load-file"));
        LoadTheWrongFile();
        Assert.Same(s.Q("#slots-modal"), s.H.Ui.Windows.Top);
        Assert.Equal(GameSession.NotACharacterSave, s.Q("#slots-status").TextContent);
        Assert.Equal("", s.Q("#start-status").TextContent);
        // The list closed: Ctrl+O on the start screen, the same file: the start screen's line says it.
        Escape(s);
        Assert.Null(s.H.Ui.Windows.Top);
        s.H.Key("o", "KeyO", KeyModifiers.Control);
        Settle(s);
        LoadTheWrongFile();
        Assert.Null(s.H.Ui.Windows.Top);
        Assert.Equal((GameSession.NotACharacterSave, "backup-status start-status is-error"), (s.Q("#start-status").TextContent, s.Q("#start-status").ClassName));
        Assert.Null(s.Session.Current);
    }

    [Fact]
    public void TheMenuAndAboutSayWhenTheyOpenOnce()
    {
        using var s = Seven();
        var log = GameTesting.CaptureLog(() =>
        {
            s.H.Ui.Commands.Run(GameUi.MenuCommand);
            s.H.Ui.Commands.Run(GameUi.MenuCommand);
            Escape(s);
            s.Session.New("Ser Alrik");
            Settle(s);
            s.H.Ui.Commands.Run(GameUi.MenuCommand);
            Escape(s);
            s.H.Ui.Commands.Run("about");
            s.H.Ui.Commands.Run("about");
        });
        Assert.Equal(["Info [Game] the menu opened (the start screen)", "Info [Game] the menu opened (a character open)", "Info [Game] the credits view opened"],
            log.Where(line => line.Contains("the menu opened", StringComparison.Ordinal) || line.Contains("the credits view", StringComparison.Ordinal)));
    }

    [Fact]
    public void GameOpenedSoundsOncePerCharacterOpenedWhicheverScreenOpenedIt()
    {
        using var s = Seven();
        Saved(s, "Ser Alrik");
        Saved(s, "Wren");
        CharacterFile.Export(Path.Combine(s.Documents!, "mira.json"), ScreensHarness.Ready(new Data.Saves.SaveGame { Name = "Mira" }));
        s.H.Sounds.Clear();
        // Continue (the start screen's own open).
        Click(s, s.Q("#start-continue"));
        Assert.Equal(("wren", 1), (s.Session.CurrentSlot, Opened(s)));
        // The menu's Start screen, then the slot list's Open (step 10's open-slot).
        Click(s, s.Q("#menu-button"));
        Click(s, MenuItem(s, "Start screen"));
        Assert.True(s.Start.Showing);
        Click(s, s.Q("#start-load"));
        Click(s, s.Q(".slot-row[data-slot=\"ser-alrik\"] .slot-open"));
        Assert.Equal(("ser-alrik", 2), (s.Session.CurrentSlot, Opened(s)));
        Assert.Null(s.H.Ui.Windows.Top);
        // Ctrl+O over the page, a character file (step 10's picker and the session's import): a new slot, opened.
        s.H.Key("o", "KeyO", KeyModifiers.Control);
        Settle(s);
        s.Picker!.Open!.NameField.Value = "mira.json";
        Click(s, s.Q("#picker-accept"));
        Assert.Equal(("mira", 3), (s.Session.CurrentSlot, Opened(s)));
        Assert.Equal(("Loaded mira.json", "backup-status topbar-file-status is-success"), (s.Q("#backup-status").TextContent, s.Q("#backup-status").ClassName));
    }
}

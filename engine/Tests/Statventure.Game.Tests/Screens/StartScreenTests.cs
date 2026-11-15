using Statventure.Game.Screens.Start;
using Statventure.Game.Sounds;

namespace Statventure.Game.Tests;

/// <summary>
/// The start screen (GM1-2, Q2, Q10, Q16, Q16b; Docs/Game/Foundation.md, "The start screen", "New character"; step 9): the
/// game's name and Continue, New character, Load, Settings, Quit and About in Q16's order, Continue greyed while there is
/// nothing to continue, the focus on the first button that can act; New character's prompt and the words its bubble
/// says for a name the save refuses; Continue opening the last character or asking step 10's damaged-save question; the
/// line naming the mods left out; <c>start-screen</c> leaving the open character.
/// </summary>
public class StartScreenTests
{
    static string[] Buttons(ScreensHarness s) => [.. s.Document.QuerySelectorAll("#start-screen .start-actions button").Select(b => b.TextContent)];

    [Fact]
    public void AFirstStartShowsTheStartScreenWithContinueGreyedAndNewCharacterFocused()
    {
        using var s = new ScreensHarness();
        Assert.True(s.Start.Showing);
        Assert.False(s.Q("#start-screen").Hidden);
        Assert.True(s.Q("main.game-page").Hidden);
        Assert.True(s.Q("#settings-open-button").Hidden);
        Assert.Equal(["Continue", "New character", "Load", "Settings", "Quit", "About"], Buttons(s));
        Assert.Equal(("RPG / PERSONAL RECORD", "Stat Fick"), (s.Q("#start-screen .eyebrow").TextContent, s.Q("#start-title").TextContent));
        Assert.True(s.Q("#start-continue").IsDisabled);
        Assert.False(s.H.Ui.Commands.IsEnabled("continue"));
        Assert.Same(s.Q("#start-new"), s.Document.FocusedElement);
        Assert.True(s.Q("#start-left-out").Hidden);
        Assert.Equal("", s.Q("#start-status").TextContent);
        Assert.Contains(StartScreen.Stylesheet, s.H.Ui.Sheets);
        // A rule under the comparisons' limits, checked as computed: the content on the dialog's parchment.
        var content = s.Q("#start-screen .start-content").Style!;
        Assert.Equal(s.Q("#start-screen .start-dialog").Style!.BackgroundColor, content.BackgroundColor);
        Assert.True(content.BackgroundColor.A > 0 && content.BackgroundImages.Length == 2, "the content's parchment: its grain over its surface");
        // A click on the greyed Continue does nothing and sounds nothing.
        s.H.Sounds.Clear();
        s.H.Click(s.Q("#start-continue"));
        s.H.Frame();
        Assert.Null(s.Session.Current);
        Assert.Empty(s.H.Sounds);
    }

    [Fact]
    public void WithACharacterToContinueContinueIsEnabledFocusedAndOpensIt()
    {
        using var s = new ScreensHarness(before: ScreensHarness.WithCharacter("Ser Alrik"));
        Assert.False(s.Q("#start-continue").IsDisabled);
        Assert.Same(s.Q("#start-continue"), s.Document.FocusedElement);
        s.H.Sounds.Clear();
        s.H.Click(s.Q("#start-continue"));
        s.H.Frame();
        Assert.Equal("ser-alrik", s.Session.CurrentSlot);
        Assert.False(s.Start.Showing);
        Assert.True(s.Sheet.Showing);
        Assert.Equal([SoundNames.Opened], s.H.Sounds);
        Assert.Empty(s.Ran);
    }

    [Fact]
    public void ContinueOnADamagedSaveAsksStep10sQuestionThroughOpenSlot()
    {
        using var s = new ScreensHarness(before: ScreensHarness.WithCharacter("Ser Alrik"));
        s.H.Storage.Flush();
        File.WriteAllText(s.H.Storage.Saves.SavePath("ser-alrik"), "not a save");
        File.Delete(s.H.Storage.Saves.BackupPath("ser-alrik"));
        s.H.Click(s.Q("#start-continue"));
        s.H.Frame();
        Assert.Null(s.Session.Current);
        Assert.True(s.Start.Showing);
        Assert.Equal(["open-slot ser-alrik"], s.Ran);
    }

    [Fact]
    public void ContinueFollowsTheSlotsAndTheLastCharacter()
    {
        using var s = new ScreensHarness(before: ScreensHarness.WithCharacter("Ser Alrik"));
        Assert.False(s.Q("#start-continue").IsDisabled);
        s.Session.Delete("ser-alrik");
        Assert.True(s.Q("#start-continue").IsDisabled);
        // A slot made and left again (another screen's doing): Continue enabled again.
        s.Session.New("Mira");
        s.Session.Leave();
        s.H.Frame();
        Assert.False(s.Q("#start-continue").IsDisabled);
        s.Parts.Settings.LastCharacter = null;
        Assert.True(s.Q("#start-continue").IsDisabled);
    }

    [Fact]
    public void NewCharacterAsksTheNameWithTheNextDefaultAndMakesAndOpensTheCharacter()
    {
        using var s = new ScreensHarness(before: ScreensHarness.WithCharacter("Character 1"));
        s.H.Click(s.Q("#start-new"));
        s.H.Frame();
        var field = s.Q(".game-prompt-field");
        // The approved words (2026-11-14).
        Assert.Equal(("Name your character", "Character 2", "80"), (s.Q(".game-dialog-question").TextContent, field.Value, field.GetAttribute("maxlength")));
        Assert.Equal((0, 11), (field.SelectionStart, field.SelectionEnd));
        Assert.Equal(("OK", "Cancel"), (s.Q(".game-dialog-accept").TextContent, s.Q(".game-dialog-cancel").TextContent));
        s.H.Type("Ser Alrik");
        s.H.Sounds.Clear();
        s.H.Click(s.Q(".game-dialog-accept"));
        s.H.Frame();
        Assert.Equal("ser-alrik", s.Session.CurrentSlot);
        Assert.Equal("Ser Alrik", s.Session.Current!.Name);
        Assert.Equal("ser-alrik", s.Parts.Settings.LastCharacter);
        Assert.True(s.Sheet.Showing);
        Assert.False(s.Start.Showing);
        Assert.Equal([SoundNames.Opened], s.H.Sounds);
        Assert.Empty(s.H.Ui.Windows.Layers);
    }

    [Fact]
    public void NewCharacterRefusesANameTheSaveRefusesWithItsWordsAndCancelChangesNothing()
    {
        using var s = new ScreensHarness();
        s.H.Ui.Commands.Run("new-character");
        s.H.Frame();
        var field = s.Q(".game-prompt-field");
        foreach (var (name, words) in new[]
        {
            ("", "Your character needs a name."),
            ("   ", "Your character needs a name."),
            ("Ser\tAlrik", "A name can't hold a tab or another control character."),
            (new string('a', 81), "A name can be at most 80 characters long."),
        })
        {
            field.Value = name;
            s.H.Sounds.Clear();
            s.H.Click(s.Q(".game-dialog-accept"));
            s.H.Frame();
            Assert.Equal(words, s.Document.ValidationBubble!.TextContent);
            Assert.Equal([SoundNames.Refused], s.H.Sounds);
            Assert.NotNull(s.H.Ui.Dialogs.Open);
            Assert.Empty(s.Session.Slots());
        }
        s.H.Click(s.Q(".game-dialog-cancel"));
        s.H.Frame();
        Assert.Null(s.H.Ui.Dialogs.Open);
        Assert.Empty(s.Session.Slots());
        Assert.True(s.Start.Showing);
    }

    [Fact]
    public void TheNameRefusalsWordsFollowTheSavesCheck()
    {
        Assert.Null(StartScreen.NameRefusal("Ser Alrik"));
        Assert.Null(StartScreen.NameRefusal(new string('a', 80)));
        Assert.Null(StartScreen.NameRefusal("Élise ✦"));
        Assert.Equal("Your character needs a name.", StartScreen.NameRefusal(" 　 "));
        Assert.Equal("A name can be at most 80 characters long.", StartScreen.NameRefusal(new string('a', 81)));
        Assert.Equal("A name can't hold a tab or another control character.", StartScreen.NameRefusal("a\u0007b"));
        // The first problem the check finds decides (an empty name of control characters is empty).
        Assert.Equal("Your character needs a name.", StartScreen.NameRefusal("\t"));
    }

    [Fact]
    public void TheLeftOutLineNamesUpToThreeModsAndHowManyMore()
    {
        Assert.Null(StartScreen.LeftOutLine([]));
        Assert.Equal("1 mod was left out: Ember Rite. Settings › Mods tells why.", StartScreen.LeftOutLine(["Ember Rite"]));
        Assert.Equal("2 mods were left out: Ember Rite and Frost Pack. Settings › Mods tells why.", StartScreen.LeftOutLine(["Ember Rite", "Frost Pack"]));
        Assert.Equal("3 mods were left out: A, B and C. Settings › Mods tells why.", StartScreen.LeftOutLine(["A", "B", "C"]));
        Assert.Equal("4 mods were left out: A, B, C and 1 more. Settings › Mods tells why.", StartScreen.LeftOutLine(["A", "B", "C", "D"]));
        Assert.Equal("5 mods were left out: A, B, C and 2 more. Settings › Mods tells why.", StartScreen.LeftOutLine(["A", "B", "C", "D", "E"]));
    }

    [Theory]
    [InlineData(new[] { "Ember Rite" }, "1 mod was left out: Ember Rite. Settings › Mods tells why.")]
    [InlineData(new[] { "Ember Rite", "Frost Pack" }, "2 mods were left out: Ember Rite and Frost Pack. Settings › Mods tells why.")]
    [InlineData(new[] { "Ember Rite", "Frost Pack", "Old Maps", "Night Market", "Bone Choir" }, "5 mods were left out: Bone Choir, Ember Rite, Frost Pack and 2 more. Settings › Mods tells why.")]
    public void TheStartScreenNamesTheModsLeftOutAtThisStart(string[] names, string line)
    {
        using var s = new ScreensHarness(leftOut: names);
        var leftOut = s.Q("#start-left-out");
        Assert.False(leftOut.Hidden);
        Assert.Equal(line, leftOut.TextContent);
        // Where the list says why, kept whole on its line.
        Assert.Equal(StartScreen.LeftOutWhere, s.Q("#start-left-out .start-left-out-where").TextContent);
    }

    [Fact]
    public void TheButtonsRunTheirCommands()
    {
        using var s = new ScreensHarness();
        s.H.Click(s.Q("#start-load"));
        s.H.Click(s.Q("#start-settings"));
        Assert.Equal(["open-slots", "open-settings"], s.Ran);
        s.H.Click(s.Q("#start-about"));
        Assert.True(s.H.Ui.Windows.IsOpen(s.About.Layer!));
        s.H.Ui.Windows.CloseTop();
        s.H.Click(s.Q("#start-quit"));
        Assert.Equal(["quit"], s.H.Done);
    }

    [Fact]
    public void TheStartScreenCommandLeavesTheOpenCharacterSavedAndShowsTheStartScreen()
    {
        using var s = new ScreensHarness();
        s.Session.New("Ser Alrik");
        s.H.Frame();
        Assert.False(s.Start.Showing);
        s.H.Storage.Flush();
        File.Delete(s.H.Storage.Saves.SavePath("ser-alrik"));
        s.H.Ui.Commands.Run("start-screen");
        s.H.Frame();
        Assert.Null(s.Session.Current);
        Assert.True(s.Start.Showing);
        Assert.False(s.Sheet.Showing);
        Assert.False(s.Q("#start-continue").IsDisabled);
        Assert.Same(s.Q("#start-continue"), s.Document.FocusedElement);
        // Left, it was saved to its slot.
        s.H.Storage.Flush();
        Assert.True(File.Exists(s.H.Storage.Saves.SavePath("ser-alrik")));
    }

    [Fact]
    public void TheStartScreenCommandOnTheStartScreenChangesNothing()
    {
        using var s = new ScreensHarness();
        s.H.Ui.Status(Statventure.Game.Session.GameSession.NotACharacterSave, Statventure.Game.Ui.StatusKind.Problem);
        s.Q("#start-load").Focus();
        s.H.Ui.Commands.Run("start-screen");
        s.H.Frame();
        Assert.True(s.Start.Showing);
        Assert.Equal(Statventure.Game.Session.GameSession.NotACharacterSave, s.Q("#start-status").TextContent);
        Assert.Same(s.Q("#start-load"), s.Document.FocusedElement);
    }

    [Fact]
    public void ACharacterLeftForAnotherNeverShowsTheStartScreenBetween()
    {
        using var s = new ScreensHarness();
        s.Session.New("Ser Alrik");
        s.H.Frame();
        var shown = 0;
        s.Q("#start-screen").AddEventListener("focusin", _ => shown++);
        // Another character made over the open one (as Load Character opens a file's): left, then opened, in one call.
        s.H.Document.RunScript(() => s.Session.New("Mira"));
        s.H.Frame();
        Assert.False(s.Start.Showing);
        Assert.True(s.Sheet.Showing);
        Assert.Equal(0, shown);
    }

    [Fact]
    public void EscapeOnTheStartScreenOpensTheMenu()
    {
        using var s = new ScreensHarness();
        s.H.Key("Escape", "Escape");
        Assert.True(s.H.Ui.Windows.IsOpen(s.Menu.Layer));
    }
}

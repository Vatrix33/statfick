using Statventure.Game.Ui;

namespace Statventure.Game.Tests;

/// <summary>
/// The status lines (the old <c>#backup-status</c>; Docs/Game/Foundation.md, "Save Character and Load Character"; the
/// manager's rule, 2026-11-14): <see cref="GameUi.Status"/> is the one channel, and a line shows what it says only while
/// its owner is the topmost surface: the page's and the start screen's while no window is open and their screen shows (a
/// window's own line, step 10's, while that window is the topmost). News in the old "success" ink, problems in its
/// "error" ink; a line is emptied when its screen shows again.
/// </summary>
public class StatusLineTests
{
    [Fact]
    public void TheStartScreensLineSaysWhatIsSaidWhileNoWindowIsOpen()
    {
        using var s = new ScreensHarness();
        var line = s.Q("#start-status");
        s.H.Ui.Status(Session.GameSession.NotACharacterSave, StatusKind.Problem);
        Assert.Equal((Session.GameSession.NotACharacterSave, "backup-status start-status is-error"), (line.TextContent, line.ClassName));
        s.H.Ui.Status("Loaded ser-alrik.json");
        Assert.Equal(("Loaded ser-alrik.json", "backup-status start-status is-success"), (line.TextContent, line.ClassName));
        // A window over it (step 10's slot list, say) has its own line: this one keeps what it had.
        var window = s.H.Layer("stand-in");
        s.H.Ui.Windows.Open(window);
        s.H.Ui.Status("Character could not be saved", StatusKind.Problem);
        Assert.Equal("Loaded ser-alrik.json", line.TextContent);
        s.H.Ui.Windows.Close(window);
        s.H.Ui.Status("Character saved");
        Assert.Equal("Character saved", line.TextContent);
        // The page's line hears nothing while the start screen shows.
        Assert.Equal("", s.Q("#backup-status").TextContent);
    }

    [Fact]
    public void ThePagesLineSaysWhatIsSaidWhileNoWindowIsOpenAndIsEmptiedForANewCharacter()
    {
        using var s = new ScreensHarness(1200, 400);
        s.Session.New("Ser Alrik");
        s.H.Frame();
        var line = s.Q("#backup-status");
        s.H.Ui.Status("Character saved");
        Assert.Equal(("Character saved", "backup-status topbar-file-status is-success"), (line.TextContent, line.ClassName));
        s.H.Ui.Status("Character could not be saved", StatusKind.Problem);
        Assert.Equal(("Character could not be saved", "backup-status topbar-file-status is-error"), (line.TextContent, line.ClassName));
        var window = s.H.Layer("stand-in");
        s.H.Ui.Windows.Open(window);
        s.H.Ui.Status("Loaded ser-alrik.json");
        Assert.Equal("Character could not be saved", line.TextContent);
        s.H.Ui.Windows.Close(window);
        Assert.Equal("", s.Q("#start-status").TextContent);
        // Another character opened: the page's line starts empty.
        s.Session.New("Mira");
        s.H.Frame();
        Assert.Equal(("", "backup-status topbar-file-status"), (line.TextContent, line.ClassName));
    }

    [Fact]
    public void TheStartScreensLineIsEmptiedWhenItShowsAgain()
    {
        using var s = new ScreensHarness();
        s.H.Ui.Status(Session.GameSession.NotACharacterSave, StatusKind.Problem);
        s.Session.New("Ser Alrik");
        s.H.Frame();
        s.H.Ui.Commands.Run("start-screen");
        s.H.Frame();
        Assert.Equal(("", "backup-status start-status"), (s.Q("#start-status").TextContent, s.Q("#start-status").ClassName));
    }
}

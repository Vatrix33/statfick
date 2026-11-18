using Statventure.Game.Screens.Sheet;
using Statventure.Game.Sounds;

namespace Statventure.Game.Tests;

/// <summary>
/// The character's page in GM1 (Q5, Q6, Q7, Q11, Q32, Q33; Docs/Game/Foundation.md, "What the player sees in a character in
/// GM1", "The top bar"; step 9): the old page's frame without the theme toggle and Save, Load and New Character, with
/// "☰ Menu" at the top bar's right end and the ✦ a mark, not a button; shown while a character is open, whoever opened
/// it, with <c>game-opened</c>; the autosave dot lit 350 ms on every save; the ⚙ Settings button and the Menu button
/// running their commands; since GM2 (Q26, Q34) shown for a hero with a race and a look, with the Field tools rail's room.
/// </summary>
public class PageTests
{
    [Fact]
    public void TheTopBarIsTheOldOnesFrameWithTheMenuButtonAndTheMarkNotAButton()
    {
        using var s = new ScreensHarness(1200, 400);
        var topbar = s.Q("main.game-page header.topbar");
        var combat = s.Q("#combat-view-toggle");
        Assert.Equal(("Return to combat", true), (combat.TextContent, combat.IsDisabled));
        var mark = s.Q(".topbar .brand-mark");
        Assert.Equal(("span", "true", null), (mark.Tag, mark.GetAttribute("aria-hidden"), mark.GetAttribute("title")));
        Assert.Equal("✦", mark.TextContent);
        Assert.False(mark.HasClass("dev-mode-toggle"));
        Assert.Equal("Stat Fick", s.Q(".topbar .brand-name").TextContent);
        Assert.Equal(("Day 1 · 08:00", "World time"), (s.Q("#sheet-world-clock").TextContent, s.Q("#sheet-world-clock").GetAttribute("aria-label")));
        Assert.Equal("Autosaved locally", s.Q(".topbar .autosave").TextContent);
        Assert.NotNull(s.Find(".topbar .autosave .status-dot"));
        var actions = s.Q(".topbar-actions");
        var menu = actions.ElementChildren.Last();
        Assert.Equal(("menu-button", "☰ Menu", "dialog"), (menu.Id, menu.TextContent, menu.GetAttribute("aria-haspopup")));
        Assert.True(menu.HasClass("backup-button"));
        // Q6: the theme toggle and the character buttons left the top bar.
        foreach (var gone in new[] { "#theme-toggle", "#backup-export-button", "#backup-restore-button", "#reset-button", ".character-file-actions", "#editing-toggle" })
            Assert.Null(s.Find(gone));
        Assert.Equal("", s.Q("#backup-status").TextContent);
        Assert.True(s.Q("#day-status").Hidden);
        var settings = s.Q("#settings-open-button");
        Assert.Equal(("Open settings", "⚙Settings"), (settings.GetAttribute("title"), settings.TextContent.Trim()));
        Assert.Same(topbar, combat.Parent);
        Assert.Contains(SheetScreen.Stylesheet, s.H.Ui.Sheets);
    }

    [Fact]
    public void ThePageShowsWhileACharacterIsOpenWhoeverOpenedItWithTheOpenedSound()
    {
        using var s = new ScreensHarness(1200, 400, before: ScreensHarness.WithHero("Ser Alrik"));
        Assert.False(s.Sheet.Showing);
        s.H.Sounds.Clear();
        // Another part opens it (step 10's slot list, say).
        s.Session.Open("ser-alrik");
        s.H.Frame();
        Assert.True(s.Sheet.Showing);
        Assert.False(s.Q("main.game-page").Hidden);
        Assert.False(s.Q("#settings-open-button").Hidden);
        Assert.Equal([SoundNames.Opened], s.H.Sounds);
        s.Session.Leave();
        s.H.Frame();
        Assert.False(s.Sheet.Showing);
        Assert.True(s.Q("#settings-open-button").Hidden);
        Assert.True(s.Start.Showing);
    }

    [Fact]
    public void TheAutosaveDotLightsFor350MillisecondsOnEverySave()
    {
        using var s = new ScreensHarness(1200, 400);
        var dot = s.Q(".status-dot");
        s.Session.New("Ser Alrik");
        Assert.True(dot.HasClass(SheetScreen.SavingClass));
        s.H.Wait(0.3);
        Assert.True(dot.HasClass(SheetScreen.SavingClass));
        // A save meanwhile starts the 350 ms again.
        s.Session.Change(save => save with { Name = "Ser Alrik the Bold" });
        s.H.Wait(0.3);
        Assert.True(dot.HasClass(SheetScreen.SavingClass));
        s.H.Wait(0.1);
        Assert.False(dot.HasClass(SheetScreen.SavingClass));
        // Left while its dot is lit and opened again at once: the dot went out with the page.
        s.Session.Change(save => save with { Name = "Ser Alrik" });
        Assert.True(dot.HasClass(SheetScreen.SavingClass));
        s.Session.Leave();
        s.Session.Open("ser-alrik");
        Assert.False(dot.HasClass(SheetScreen.SavingClass));
        s.H.Wait(0.5);
        // Opening a slot writes nothing: the dot stays as it is.
        s.Session.Leave();
        s.Session.Open("ser-alrik");
        s.H.Frame();
        Assert.False(dot.HasClass(SheetScreen.SavingClass));
    }

    [Fact]
    public void TheMenuAndSettingsButtonsRunTheirCommands()
    {
        using var s = new ScreensHarness(1200, 400);
        s.NewHero("Ser Alrik");
        s.H.Click(s.Q("#settings-open-button"));
        Assert.Equal(["open-settings"], s.Ran);
        s.H.Click(s.Q("#menu-button"));
        Assert.True(s.H.Ui.Windows.IsOpen(s.Menu.Layer));
    }

    [Fact]
    public void ThePageKeepsTheFieldToolsRailsRoomWhileTheSheetShows()
    {
        // Q34 (GM2): the whole rail and its room (game.css: the body's padding-left, var(--field-menu-width), 260 px at
        // 1350 px or less); GM1's Q32 kept none. The start screen, with no character, stays in the middle of the window.
        using var s = new ScreensHarness(1200, 400);
        Assert.Equal(0, s.Document.Body.Style!.PaddingLeft.ResolveOr(1200, -1));
        s.NewHero("Ser Alrik");
        Assert.True(s.Document.Body.HasClass(SheetScreen.RailRoomClass));
        Assert.Equal(260, s.Document.Body.Style!.PaddingLeft.ResolveOr(1200, -1));
        var rail = s.Q("#field-tools-menu").GetBoundingClientRect();
        Assert.Equal((0, 260), (rail.Left, rail.Width));
        // The page's frame in the middle of the rest (beside the page's own scrollbar).
        var page = s.Q("main.game-page").GetBoundingClientRect();
        Assert.Equal(s.Document.Root.ClientWidth - page.Right, page.Left - 260, 0.5);
        // Under 840 px the rail is a drawer and takes no room.
        s.Document.SetViewport(820, 400);
        s.H.Frame();
        Assert.Equal(0, s.Document.Body.Style!.PaddingLeft.ResolveOr(820, -1));
        // The character left: the room goes with the sheet.
        s.Document.SetViewport(1200, 400);
        s.Session.Leave();
        s.H.Frame();
        Assert.False(s.Document.Body.HasClass(SheetScreen.RailRoomClass));
        Assert.Equal(0, s.Document.Body.Style!.PaddingLeft.ResolveOr(1200, -1));
    }
}

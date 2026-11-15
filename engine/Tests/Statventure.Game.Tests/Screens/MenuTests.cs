using Statventure.Game.Screens.Menu;
using Statventure.Game.Sounds;

namespace Statventure.Game.Tests;

/// <summary>
/// The in-game menu (Q7, Q8, Q18; Docs/Game/Foundation.md, "The in-game menu"; step 9): the old menu's items in its order
/// and words, Electron's role labels and keys, Reload left out, Settings and Start screen added, those needing a character
/// left out on the start screen; opened by the top bar's Menu button, Escape with no window open and <c>open-menu</c>,
/// the focus on its first item; an item closes it and runs its command; closed by Escape, ×, the ground around it; Up,
/// Down, Home and End among the items.
/// </summary>
public class MenuTests
{
    static readonly string[] OnThePage =
    [
        "Save Character | Ctrl+S", "Load Character | Ctrl+O", "Open game folder", "Settings", "Start screen",
        "Actual Size | Ctrl+0", "Zoom In | Ctrl+Plus", "Zoom Out | Ctrl+-", "Toggle Full Screen | F11",
        "About Stat Fick",
        "Quit | Ctrl+Q",
    ];

    [Fact]
    public void OnThePageTheMenuHasEveryItemInTheOldMenusOrderAndWords()
    {
        using var s = new ScreensHarness(800, 680);
        s.Session.New("Ser Alrik");
        s.H.Frame();
        s.H.Ui.Commands.Run(MenuScreen.OpenCommand);
        Assert.Equal(OnThePage, s.MenuItems());
        Assert.Equal([5, 4, 1, 1], s.Document.QuerySelectorAll("#game-menu .game-menu-group").Select(g => g.ElementChildren.Count()));
        Assert.Equal(("STAT FICK", "Game menu"), (s.Q("#game-menu .eyebrow").TextContent, s.Q("#game-menu-title").TextContent));
        Assert.Same(s.Q("#game-menu .game-menu-item"), s.Document.FocusedElement);
        Assert.DoesNotContain(s.MenuItems(), item => item.StartsWith("Reload", StringComparison.Ordinal));
        Assert.Contains(MenuScreen.Stylesheet, s.H.Ui.Sheets);
        // Rules under the comparisons' limits, checked as computed: the items' words at the left; the content on the
        // dialog's parchment.
        Assert.Equal(UI.Style.TextAlignment.Left, s.Q("#game-menu .game-menu-item").Style!.TextAlign);
        var content = s.Q("#game-menu .game-menu-content").Style!;
        Assert.Equal(s.Q("#game-menu .game-menu-dialog").Style!.BackgroundColor, content.BackgroundColor);
        Assert.True(content.BackgroundColor.A > 0 && content.BackgroundImages.Length == 2, "the content's parchment: its grain over its surface");
    }

    [Fact]
    public void OnTheStartScreenTheItemsThatNeedACharacterAreLeftOut()
    {
        using var s = new ScreensHarness();
        s.H.Ui.Commands.Run(MenuScreen.OpenCommand);
        Assert.Equal(OnThePage.Where(i => !i.StartsWith("Save Character", StringComparison.Ordinal) && i != "Start screen"), s.MenuItems());
        Assert.Equal("Load Character | Ctrl+O", s.Document.FocusedElement!.ElementChildren.First().TextContent + " | " + s.Document.FocusedElement.ElementChildren.Last().TextContent);
    }

    [Fact]
    public void OnWindowsQuitIsExitWithNoKeys()
    {
        var items = MenuScreen.Items(windows: true).SelectMany(g => g).ToList();
        Assert.Equal(("Exit", "quit"), (items[^1].Label, items[^1].Command));
        Assert.Equal(("Quit", "quit"), (MenuScreen.Items(windows: false).SelectMany(g => g).Last().Label, "quit"));
        Assert.Equal(["save-character", "load-character", "open-game-folder", "open-settings", "start-screen", "zoom-reset", "zoom-in", "zoom-out", "fullscreen", "about", "quit"],
            items.Select(i => i.Command));
        Assert.Equal(["save-character", "start-screen"], items.Where(i => i.NeedsCharacter).Select(i => i.Command));
    }

    [Fact]
    public void TheMenuOpensByTheButtonByEscapeAndOnceAtATime()
    {
        using var s = new ScreensHarness(1200, 680);
        s.Session.New("Ser Alrik");
        s.H.Frame();
        s.H.Sounds.Clear();
        s.H.Click(s.Q("#menu-button"));
        Assert.True(s.H.Ui.Windows.IsOpen(s.Menu.Layer));
        Assert.Equal([SoundNames.Open], s.H.Sounds);
        // Asked again while open: left as it is (its items not made again, the focus where it was).
        s.H.Key("ArrowDown", "ArrowDown");
        var focused = s.Document.FocusedElement!;
        s.H.Ui.Commands.Run(MenuScreen.OpenCommand);
        Assert.Single(s.H.Ui.Windows.Layers);
        Assert.Single(s.Document.QuerySelectorAll("#game-menu"));
        Assert.Same(focused, s.Document.FocusedElement);
        Assert.Same(focused, s.Document.QuerySelectorAll("#game-menu .game-menu-item").ElementAt(1));
        // Escape closes it, the focus back on the Menu button; Escape with no window opens it again.
        s.H.Key("Escape", "Escape");
        Assert.Empty(s.H.Ui.Windows.Layers);
        Assert.Same(s.Q("#menu-button"), s.Document.FocusedElement);
        s.H.Key("Escape", "Escape");
        Assert.True(s.H.Ui.Windows.IsOpen(s.Menu.Layer));
        s.H.Click(s.Q("#game-menu .modal-close"));
        Assert.Empty(s.H.Ui.Windows.Layers);
        s.H.Ui.Commands.Run(MenuScreen.OpenCommand);
        s.H.Click(new System.Numerics.Vector2(10, 10));
        Assert.Empty(s.H.Ui.Windows.Layers);
    }

    [Fact]
    public void AnItemClosesTheMenuThenRunsItsCommand()
    {
        using var s = new ScreensHarness(800, 680);
        s.Session.New("Ser Alrik");
        s.H.Frame();
        // The menu closes before the item's command runs.
        s.H.Ui.Windows.Closed += layer =>
        {
            if (layer == s.Menu.Layer)
                s.Ran.Add("menu closed");
        };
        void Choose(string label)
        {
            s.H.Ui.Commands.Run(MenuScreen.OpenCommand);
            s.H.Frame();
            var item = s.Document.QuerySelectorAll("#game-menu .game-menu-item").Single(i => i.ElementChildren.First().TextContent == label);
            s.H.Click(item);
            Assert.False(s.H.Ui.Windows.IsOpen(s.Menu.Layer));
        }
        Choose("Save Character");
        Choose("Load Character");
        Choose("Settings");
        Assert.Equal(["menu closed", "save-character", "menu closed", "load-character", "menu closed", "open-settings"], s.Ran);
        Choose("Open game folder");
        Assert.Equal(["open game folder"], s.H.Done);
        Choose("Zoom In");
        Assert.Equal(0.5, s.Parts.Settings.ZoomLevel);
        Choose("Zoom Out");
        Choose("Zoom Out");
        Assert.Equal(-0.5, s.Parts.Settings.ZoomLevel);
        Choose("Actual Size");
        Assert.Equal(0, s.Parts.Settings.ZoomLevel);
        Choose("Toggle Full Screen");
        Assert.True(s.Parts.Settings.Fullscreen);
        Choose("About Stat Fick");
        Assert.True(s.H.Ui.Windows.IsOpen(s.About.Layer!));
        Assert.Single(s.H.Ui.Windows.Layers);
        s.H.Ui.Windows.CloseTop();
        Choose("Quit");
        Assert.Equal(["open game folder", "quit"], s.H.Done);
        Choose("Start screen");
        Assert.Null(s.Session.Current);
        Assert.True(s.Start.Showing);
    }

    [Fact]
    public void ChoosingAnItemSoundsWhatItDoes()
    {
        using var s = new ScreensHarness(800, 680);
        s.Session.New("Ser Alrik");
        s.H.Frame();
        s.H.Ui.Commands.Run(MenuScreen.OpenCommand);
        s.H.Frame();
        s.H.Sounds.Clear();
        s.H.Click(s.Document.QuerySelectorAll("#game-menu .game-menu-item").Single(i => i.ElementChildren.First().TextContent == "About Stat Fick"));
        s.H.Frame();
        // The menu closing and About opening in one action: the opening says most.
        Assert.Equal([SoundNames.Open], s.H.Sounds);
    }

    [Fact]
    public void UpDownHomeAndEndMoveAmongTheItems()
    {
        using var s = new ScreensHarness(800, 680);
        s.H.Ui.Commands.Run(MenuScreen.OpenCommand);
        s.H.Frame();
        var items = s.Document.QuerySelectorAll("#game-menu .game-menu-item").ToList();
        Assert.Same(items[0], s.Document.FocusedElement);
        // The menu takes the keys it moves by (their keydown cancelled).
        Assert.False(s.H.Key("ArrowDown", "ArrowDown"));
        Assert.Same(items[1], s.Document.FocusedElement);
        s.H.Key("ArrowUp", "ArrowUp");
        s.H.Key("ArrowUp", "ArrowUp");
        Assert.Same(items[^1], s.Document.FocusedElement);
        s.H.Key("ArrowDown", "ArrowDown");
        Assert.Same(items[0], s.Document.FocusedElement);
        s.H.Key("End", "End");
        Assert.Same(items[^1], s.Document.FocusedElement);
        s.H.Key("Home", "Home");
        Assert.Same(items[0], s.Document.FocusedElement);
        // With a modifier the keys are not the menu's.
        Assert.True(s.H.Key("ArrowDown", "ArrowDown", Platform.KeyModifiers.Shift));
        Assert.Same(items[0], s.Document.FocusedElement);
        // Enter runs the focused item.
        s.H.Key("ArrowDown", "ArrowDown");
        s.H.Key("Enter", "Enter");
        Assert.Equal(["open game folder"], s.H.Done);
    }
}

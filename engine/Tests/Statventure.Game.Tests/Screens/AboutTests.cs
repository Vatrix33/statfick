using Statventure.Game.Screens.About;
using Statventure.UI;

namespace Statventure.Game.Tests;

/// <summary>
/// About (Q20; Docs/Game/Foundation.md, "About"; step 9): a window in the old dialogs' look holding the engine's credits
/// (<see cref="Credits.Markup"/> for "Stat Fick" at the program's version: the same text the credits tests check), opened
/// by <c>about</c> at its top, once at a time, closed by ×, Escape and the ground around it; its lettering the game's.
/// </summary>
[Collection("Game log")]
public class AboutTests
{
    [Fact]
    public void AboutHoldsTheEnginesCreditsTextUnchanged()
    {
        using var s = new ScreensHarness();
        var log = GameTesting.CaptureLog(() => s.H.Ui.Commands.Run("about"));
        Assert.Contains("Info [Game] the credits view opened", log);
        Assert.True(s.H.Ui.Windows.IsOpen(s.About.Layer!));
        Assert.Equal(("HELP", "About Stat Fick"), (s.Q("#about-modal .eyebrow").TextContent, s.Q("#about-title").TextContent));
        // The credits' markup as the engine makes it, whole.
        var made = new UI.Dom.UiElement("div");
        UI.Dom.Markup.SetInnerHtml(made, Credits.Markup(StatFickGame.Title, AboutScreen.Program));
        Assert.Equal(made.TextContent, s.Q(".about-content").TextContent);
        Assert.Equal("Stat Fick", s.Q(".about-content .credits h1").TextContent);
        Assert.Equal("Version " + AboutScreen.Program.Version, s.Q(".about-content .credits .version").TextContent);
        Assert.Equal(Credits.Notice, s.Q(".about-content .credits .notice").TextContent);
        Assert.StartsWith(Credits.BrutalCopyright, s.Q(".about-content .credits .notice").TextContent);
        Assert.Equal(Credits.Components.Count, s.Document.QuerySelectorAll(".about-content .credits .component").Count());
        Assert.Equal(Credits.LicencesLine, s.Q(".about-content .credits").ElementChildren.Last().TextContent);
        // The engine's dark view is gone with step 2's stub.
        Assert.Null(s.Find(".engine-credits"));
        Assert.Contains(AboutScreen.Stylesheet, s.H.Ui.Sheets);
    }

    [Fact]
    public void AboutOpensAtItsTopOnceAtATimeAndClosesByTheCloseEscapeAndTheGround()
    {
        using var s = new ScreensHarness();
        s.H.Ui.Commands.Run("about");
        s.H.Frame();
        Assert.Same(s.Q("#about-modal .modal-close"), s.Document.FocusedElement);
        s.H.Ui.Commands.Run("about");
        Assert.Single(s.H.Ui.Windows.Layers);
        var content = s.Q(".about-content");
        content.ScrollTo(0, 400, UI.Dom.ScrollMode.Instant);
        s.H.Frame();
        Assert.True(content.ScrollTop > 0);
        s.H.Click(s.Q("#about-modal .modal-close"));
        Assert.Empty(s.H.Ui.Windows.Layers);
        // Opened again: at its top.
        s.H.Ui.Commands.Run("about");
        s.H.Frame();
        Assert.Equal(0, s.Q(".about-content").ScrollTop);
        s.H.Key("Escape", "Escape");
        Assert.Empty(s.H.Ui.Windows.Layers);
        s.H.Ui.Commands.Run("about");
        s.H.Click(new System.Numerics.Vector2(5, 340));
        Assert.Empty(s.H.Ui.Windows.Layers);
    }

    [Fact]
    public void TheCreditsAreInTheGamesLettering()
    {
        using var s = new ScreensHarness();
        s.H.Ui.Commands.Run("about");
        s.H.Frame();
        var title = s.Q(".about-content .credits h1").Style!;
        var heading = s.Q(".about-content .credits h2").Style!;
        var component = s.Q(".about-content .credits h3").Style!;
        Assert.Equal("STIX Two Text", title.FontFamilies[0]);
        Assert.Equal(("STIX Two Text", Statventure.Text.TextTransform.Uppercase), (heading.FontFamilies[0], heading.TextTransform));
        Assert.Equal(("Lato", 700), (component.FontFamilies[0], component.FontWeight));
        // Rules under the comparisons' limits, checked as computed: the version in the muted ink of the licences, not the
        // credits' own; the content on the dialog's parchment (its grain, its surface).
        var credits = s.Q(".about-content .credits").Style!;
        var version = s.Q(".about-content .credits .version").Style!;
        Assert.Equal(s.Q(".about-content .credits .licence").Style!.Color, version.Color);
        Assert.NotEqual(credits.Color, version.Color);
        var content = s.Q(".about-content").Style!;
        Assert.Equal(s.Q(".about-dialog").Style!.BackgroundColor, content.BackgroundColor);
        Assert.True(content.BackgroundColor.A > 0 && content.BackgroundImages.Length == 2, "the content's parchment: its grain over its surface");
    }
}

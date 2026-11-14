using Statventure.Game.Settings;
using Statventure.Game.Ui;
using Statventure.Packs;
using Statventure.Records;
using Statventure.Tests.Infrastructure;
using Statventure.UI.Css;
using Statventure.UI.Style;

namespace Statventure.Game.Tests;

/// <summary>
/// The game's one document (GM1-P9; Docs/Game/Foundation.md, "How it works", step 8): the port's stylesheets from
/// <c>ui.pak</c> in their order, the screens' after them, the fonts of their <c>@font-face</c> rules with the generic
/// families mapped and no system font, <c>html.dark-mode</c> and the page zoom following the settings.
/// </summary>
[Collection("Game log")]
public class GameDocumentTests
{
    [Fact]
    public void TheDocumentHasThePortsSheetsInTheirOrder()
    {
        using var h = new GameUiHarness();
        Assert.Equal(["ui/styles/game", "ui/styles/document", "ui/styles/dialogs", "ui/styles/controls"], GameUi.Stylesheets);
        Assert.Equal(GameUi.Stylesheets, h.Ui.Sheets);
        // Each sheet is in the document: game.css's dialog layer, dialogs.css's layer over it, controls.css's tooltip.
        var layer = h.Document.Body.Append("div", "modal-layer map-encounter-layer game-dialog-layer");
        var tooltip = h.Document.TopLayer.Append("sv-tooltip", text: "A title");
        h.Frame();
        Assert.Equal("grid", layer.Style!.Display.ToString().ToLowerInvariant());
        Assert.Equal(70, layer.Style.ZIndex);
        Assert.Equal(11f, tooltip.Style!.PaddingTop.Pixels);
        // document.css: the page fills the window, its body empty or not (the body's parchment or stone shows).
        Assert.Equal(600, h.Document.Body.GetBoundingClientRect().Height, 1f);
        // The order counts: the dialogs' action button follows the Gothic theme's buttons, as specific as it, so it
        // takes the ember only when dialogs.css comes after game.css.
        _ = h.Ui.Dialogs.Confirm("Unlearn Fire Bolt?", "Unlearn");
        h.Frame();
        var ember = h.Q(".game-dialog-accept").Style!.BorderTopColor;
        var cancel = h.Q(".game-dialog-cancel").Style!.BorderTopColor;
        Assert.NotEqual(cancel, ember);
    }

    [Fact]
    public void AScreensSheetComesAfterTheDocumentsOnceAndAMissingOneIsLoggedAndLeftOut()
    {
        using var h = new GameUiHarness();
        var log = GameTesting.CaptureLog(() =>
        {
            h.Ui.UseStylesheet("ui/styles/controls");
            h.Ui.UseStylesheet("ui/styles/start");
        });
        Assert.Equal(GameUi.Stylesheets, h.Ui.Sheets);
        Assert.Equal(["Error [Ui] the stylesheet ui/styles/start isn't in the game's data: left out"], log.Where(l => l.Contains("[Ui]")));
        // A sheet with no data at all: none of the port's sheets, each logged, the document still made.
        List<string> empty = [];
        var bare = GameTesting.CaptureLog(() =>
        {
            using var none = new GameUiHarness(packs: false);
            empty.AddRange(none.Ui.Sheets);
        });
        Assert.Empty(empty);
        Assert.Equal(GameUi.Stylesheets.Select(s => $"Error [Ui] the stylesheet {s} isn't in the game's data: left out"), bare.Where(l => l.Contains("[Ui]")));
    }

    [Fact]
    public void AScreensSheetsAreAddedInTheOrderAsked()
    {
        using var scratch = new ScratchFolder();
        // Two sheets of a screen's in a pack of the test's own over the committed ones.
        var pack = ScreenPack(scratch, ("ui/styles/one", ".probe { color: rgb(1, 2, 3); }"), ("ui/styles/two", ".probe { color: rgb(4, 5, 6); }"));
        foreach (var (first, second, expected) in new[] { ("one", "two", 4), ("two", "one", 1) })
        {
            using var h = new GameUiHarness(extra: pack);
            h.Ui.UseStylesheet($"ui/styles/{first}");
            h.Ui.UseStylesheet($"ui/styles/{second}");
            Assert.Equal([.. GameUi.Stylesheets, $"ui/styles/{first}", $"ui/styles/{second}"], h.Ui.Sheets);
            var probe = h.Document.Body.Append("p", "probe", "Probe");
            h.Frame();
            Assert.Equal(expected / 255f, probe.Style!.Color.R, 3);
        }
    }

    [Fact]
    public void TheFontsAreTheSheetsGenericFamiliesMappedNoSystemFont()
    {
        using var h = new GameUiHarness();
        var fonts = h.Document.Fonts;
        // The sheets' faces: ui.pak's five (@font-face).
        Assert.Equal(["Lato", "Lato", "Lato", "STIX Two Text", "STIX Two Text"], fonts.SheetFaces.Select(f => f.Family).Order());
        Assert.Equal("STIX Two Text", fonts.Resolve("serif", 400, false)!.Value.Face.Family);
        Assert.Equal("Lato", fonts.Resolve("sans-serif", 400, false)!.Value.Face.Family);
        Assert.Equal("STIX Two Text", fonts.Resolve(["Georgia", "serif"], 400, false)!.Value.Face.Family);
        Assert.Equal("Lato", fonts.Resolve(["Segoe UI", "Arial", "sans-serif"], 700, false)!.Value.Face.Family);
        Assert.Contains(fonts.SheetFaces, f => f == fonts.Resolve("sans-serif", 400, false)!.Value.Face);
        // Each the other's fallback (↓ is in Lato only; a symbol in STIX only); nothing else is asked (no system font).
        var lato = fonts.Resolve("Lato", 400, false)!.Value;
        var stix = fonts.Resolve("STIX Two Text", 400, false)!.Value;
        Assert.False(stix.Face.Has('↓'));
        Assert.Equal("Lato", fonts.ResolveFor('↓', stix)!.Value.Face.Family);
        var stixOnly = Enumerable.Range(0x100, 0x2F00).First(c => stix.Face.Has(c) && !lato.Face.Has(c));
        Assert.Equal("STIX Two Text", fonts.ResolveFor(stixOnly, lato)!.Value.Face.Family);
        // The game's icons before any font (GameIcons.AddTo, step 7): ⚙ is drawn as its icon, in Lato's text as in STIX's.
        Assert.True(fonts.ResolveFor('⚙', lato)!.Value.Face.IsIconFace);
        Assert.True(fonts.ResolveFor('⚙', stix)!.Value.Face.IsIconFace);
        Assert.False(fonts.ResolveFor('A', lato)!.Value.Face.IsIconFace);
        Assert.Null(fonts.ResolveFor('☃', lato));
        Assert.Null(fonts.ResolveFor('☃', stix));
    }

    [Fact]
    public void TheThemesClassFollowsTheSetting()
    {
        using var h = new GameUiHarness();
        // Dark on a first start (GM1-4).
        Assert.True(h.Document.Root.HasClass(GameUi.DarkModeClass));
        h.Settings.Theme = GameTheme.Light;
        Assert.False(h.Document.Root.HasClass(GameUi.DarkModeClass));
        h.Settings.Theme = GameTheme.Dark;
        Assert.True(h.Document.Root.HasClass(GameUi.DarkModeClass));
        // The sheets' dark variables apply: the page's paper.
        h.Frame();
        Assert.Equal((0x11 / 255f, 0x0c / 255f, 0x09 / 255f), (h.Document.Root.Style!.BackgroundColor.R, h.Document.Root.Style.BackgroundColor.G, h.Document.Root.Style.BackgroundColor.B), new ColourComparer());
        using var light = new GameUiHarness(theme: GameTheme.Light);
        Assert.False(light.Document.Root.HasClass(GameUi.DarkModeClass));
    }

    [Fact]
    public void ThePageZoomFollowsTheZoomLevel()
    {
        using var h = new GameUiHarness();
        Assert.Equal(1f, h.Document.PageZoom);
        h.Settings.ZoomLevel = 1.5;
        Assert.Equal((float)Math.Pow(1.2, 1.5), h.Document.PageZoom, 5);
        h.Settings.ZoomLevel = GameSettings.MinZoomLevel;
        Assert.Equal(0.25f, h.Document.PageZoom, 5);
        using var kept = new GameUiHarness(before: s => s.ZoomLevel = -2);
        Assert.Equal((float)Math.Pow(1.2, -2), kept.Document.PageZoom, 5);
    }

    /// <summary>A pack of <paramref name="sheets"/> compiled as the packer's UI importer compiles them.</summary>
    static string ScreenPack(ScratchFolder scratch, params (string Path, string Css)[] sheets)
    {
        var writer = new PackWriter();
        foreach (var (path, css) in sheets)
            writer.AddRecord(path, Stylesheet.Compile(css).Record);
        var file = scratch["screens.pak"];
        writer.Write(file);
        return file;
    }

    sealed class ColourComparer : IEqualityComparer<(float, float, float)>
    {
        public bool Equals((float, float, float) a, (float, float, float) b) =>
            Math.Abs(a.Item1 - b.Item1) < 0.002 && Math.Abs(a.Item2 - b.Item2) < 0.002 && Math.Abs(a.Item3 - b.Item3) < 0.002;

        public int GetHashCode((float, float, float) obj) => 0;
    }
}

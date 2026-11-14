using System.Text.Json.Nodes;
using Statventure.Game.Mods;
using Statventure.Game.Screens.Settings;
using Statventure.Game.Settings;
using Statventure.Game.Ui;
using Statventure.Platform;
using Statventure.UI.Dom;

namespace Statventure.Game.Tests;

/// <summary>
/// The settings screen with the mod list (GM1-3, GM1-4, GM1-10, GM1-12, GM1-13, Q3, Q13, Q15, Q18, Q22; step 10): opened by
/// <c>open-settings</c> with the settings as they are; every change applied at once and written to <c>settings.json</c>
/// (read back from the file); a volume applied while its slider moves and pressed once when let go; the zoom's buttons
/// running the menu's zoom commands, each disabled where it would do nothing; a change made elsewhere shown; the mods in
/// mounting order with each state, their switches kept in the settings and the next-start line beside a switch changed
/// since the start.
/// </summary>
public class SettingsScreenTests
{
    static GameUiHarness Harness() => new(1000, 860);

    /// <summary>The per-user folder's <c>settings.json</c> as written (the writer flushed first).</summary>
    static JsonObject File(GameUiHarness h)
    {
        h.Storage.Flush();
        return JsonNode.Parse(System.IO.File.ReadAllText(Path.Combine(h.PlayerFolder, "settings.json")))!.AsObject();
    }

    /// <summary>A click on <paramref name="element"/> once it is scrolled into the window's view.</summary>
    static void ClickInView(GameUiHarness h, UiElement element)
    {
        element.ScrollIntoView(ScrollAlign.Center);
        h.Frame();
        h.Click(element);
    }

    [Fact]
    public void ItOpensWithTheSettingsAsTheyAreTheFirstSlidersFocused()
    {
        using var h = Harness();
        var s = ScreenParts.Attach(h);
        Assert.True(s.Run(SettingsScreen.OpenCommand));
        h.Frame();
        var layer = s.Settings.Layer;
        Assert.True(h.Ui.Windows.IsOpen(layer));
        Assert.Equal("master-volume", h.Document.FocusedElement?.Id);
        Assert.Equal(("100", "100%"), (s.Q("#master-volume").Value, s.Q("#master-volume-value").TextContent));
        Assert.Equal(("100", "100%"), (s.Q("#music-volume").Value, s.Q("#music-volume-value").TextContent));
        Assert.Equal(("60", "60%"), (s.Q("#effects-volume").Value, s.Q("#effects-volume-value").TextContent));
        Assert.Equal("60%", s.Q("#effects-volume").GetAttribute("aria-valuetext"));
        Assert.Equal("Current theme: none playing", s.Q("#settings-current-track").TextContent);
        Assert.Equal("dark", s.Q("#game-theme").Value);
        Assert.Equal("100%", s.Q("#zoom-value").TextContent);
        Assert.Equal((false, true, false), (s.Q("#zoom-out").IsDisabled, s.Q("#zoom-reset").IsDisabled, s.Q("#zoom-in").IsDisabled));
        Assert.False(s.Q("#game-fullscreen").Checked);
        Assert.False(s.Q("#dev-mode").Checked);
        Assert.Equal(SettingsScreen.DevModeHelp, s.Q("#dev-mode-help").TextContent);
        Assert.Equal(SettingsScreen.NoMods, s.Q(".settings-mods-empty").TextContent);
        Assert.Equal(["ui-open"], h.Sounds);
        // Asked again while open, it stays as it is.
        s.Run(SettingsScreen.OpenCommand);
        h.Frame();
        Assert.Single(h.Ui.Windows.Layers);
        Assert.Equal(["ui-open"], h.Sounds);
        // Closed by its ×, by the backdrop, by Escape.
        h.Click(s.Q("#settings-modal .modal-close"));
        Assert.False(h.Ui.Windows.IsOpen(layer));
        s.Run(SettingsScreen.OpenCommand);
        h.Key("Escape", "Escape");
        Assert.False(h.Ui.Windows.IsOpen(layer));
        s.Run(SettingsScreen.OpenCommand);
        h.Click(new System.Numerics.Vector2(4, 4));
        Assert.False(h.Ui.Windows.IsOpen(layer));
    }

    [Fact]
    public void AVolumeAppliesWhileItsSliderMovesAndPressesOnceWhenLetGo()
    {
        using var h = Harness();
        var s = ScreenParts.Attach(h);
        s.Run(SettingsScreen.OpenCommand);
        h.Frame();
        h.Sounds.Clear();
        // Dragged: the track pressed at its start, moved to its middle; the setting follows each move, the press waits for the release.
        var slider = s.Q("#music-volume");
        var rect = slider.GetBoundingClientRect();
        var y = rect.Y + rect.Height / 2;
        h.Input.PointerMove(new(rect.X + 2, y));
        h.Input.PointerDown(new(rect.X + 2, y), MouseButton.Left);
        h.Frame();
        Assert.Equal(0, h.Settings.MusicVolume);
        h.Input.PointerMove(new(rect.X + rect.Width / 2, y));
        h.Frame();
        var middle = h.Settings.MusicVolume;
        Assert.InRange(middle, 0.4, 0.6);
        Assert.Equal($"{(int)Math.Round(middle * 100)}%", s.Q("#music-volume-value").TextContent);
        Assert.DoesNotContain("ui-press", h.Sounds);
        h.Input.PointerUp(new(rect.X + rect.Width / 2, y), MouseButton.Left);
        h.Frame();
        Assert.Equal(["ui-press"], h.Sounds);
        Assert.Equal(middle, File(h)["musicVolume"]!.GetValue<double>());
        // By the keys: each step applied and pressed (a key's change is its own let-go).
        h.Sounds.Clear();
        s.Q("#effects-volume").Focus();
        h.Key("ArrowLeft", "ArrowLeft");
        Assert.Equal(0.59, h.Settings.EffectsVolume);
        Assert.Equal("59%", s.Q("#effects-volume-value").TextContent);
        Assert.Equal(["ui-press"], h.Sounds);
        h.Key("Home", "Home");
        Assert.Equal(0, h.Settings.EffectsVolume);
        s.Q("#master-volume").Focus();
        h.Key("ArrowLeft", "ArrowLeft");
        Assert.Equal(0.99, h.Settings.MasterVolume);
        var file = File(h);
        Assert.Equal((0.99, 0.0), (file["masterVolume"]!.GetValue<double>(), file["effectsVolume"]!.GetValue<double>()));
    }

    [Fact]
    public void AChangeMadeElsewhereShowsWhileTheWindowIsOpen()
    {
        using var h = Harness();
        var s = ScreenParts.Attach(h);
        s.Run(SettingsScreen.OpenCommand);
        h.Settings.MasterVolume = 0.25;
        h.Settings.Theme = GameTheme.Light;
        h.Settings.Fullscreen = true;
        h.Settings.DevMode = true;
        h.Settings.ZoomLevel = 1;
        h.Frame();
        Assert.Equal(("25", "25%"), (s.Q("#master-volume").Value, s.Q("#master-volume-value").TextContent));
        Assert.Equal("light", s.Q("#game-theme").Value);
        Assert.True(s.Q("#game-fullscreen").Checked);
        Assert.True(s.Q("#dev-mode").Checked);
        Assert.Equal("120%", s.Q("#zoom-value").TextContent);
        // Closed (taken out of the document), the window isn't touched; opened again, it shows what the settings say.
        h.Ui.Windows.Close(s.Settings.Layer);
        h.Settings.EffectsVolume = 0.3;
        Assert.Null(s.Settings.Layer.Document);
        Assert.Equal("60%", s.Settings.Layer.Descendants().Single(e => e.Id == "effects-volume-value").TextContent);
        s.Run(SettingsScreen.OpenCommand);
        Assert.Equal("30%", s.Q("#effects-volume-value").TextContent);
    }

    [Fact]
    public void TheThemeFullScreenAndDevModeApplyAtOnceAndAreKept()
    {
        using var h = Harness();
        var s = ScreenParts.Attach(h);
        s.Run(SettingsScreen.OpenCommand);
        h.Frame();
        h.Sounds.Clear();
        var theme = s.Q("#game-theme");
        theme.Value = "light";
        theme.DispatchEvent(new UiEvent("change", bubbles: true, cancelable: false));
        h.Frame();
        Assert.Equal(GameTheme.Light, h.Settings.Theme);
        Assert.False(h.Document.Root.HasClass(GameUi.DarkModeClass));
        Assert.Equal(["ui-toggle"], h.Sounds);
        theme.Value = "dark";
        theme.DispatchEvent(new UiEvent("change", bubbles: true, cancelable: false));
        h.Frame();
        Assert.True(h.Document.Root.HasClass(GameUi.DarkModeClass));
        ClickInView(h, s.Q("#game-fullscreen"));
        Assert.True(h.Settings.Fullscreen);
        ClickInView(h, s.Q("#dev-mode"));
        Assert.True(h.Settings.DevMode);
        Assert.Equal("ui-toggle", h.Sounds[^1]);
        var file = File(h);
        Assert.Equal(("dark", true, true), (file["theme"]!.GetValue<string>(), file["fullscreen"]!.GetValue<bool>(), file["devMode"]!.GetValue<bool>()));
        ClickInView(h, s.Q("#dev-mode"));
        Assert.False(h.Settings.DevMode);
        Assert.False(File(h)["devMode"]!.GetValue<bool>());
    }

    [Fact]
    public void TheZoomsButtonsRunTheMenusZoomCommandsAndAreDisabledWhereTheyWouldDoNothing()
    {
        using var h = Harness();
        var s = ScreenParts.Attach(h);
        s.Run(SettingsScreen.OpenCommand);
        ClickInView(h, s.Q("#zoom-in"));
        Assert.Equal(0.5, h.Settings.ZoomLevel);
        Assert.Equal("110%", s.Q("#zoom-value").TextContent);
        Assert.False(s.Q("#zoom-reset").IsDisabled);
        ClickInView(h, s.Q("#zoom-reset"));
        Assert.Equal(0, h.Settings.ZoomLevel);
        Assert.True(s.Q("#zoom-reset").IsDisabled);
        ClickInView(h, s.Q("#zoom-out"));
        Assert.Equal(-0.5, h.Settings.ZoomLevel);
        Assert.Equal("91%", s.Q("#zoom-value").TextContent);
        Assert.Equal(-0.5, File(h)["zoomLevel"]!.GetValue<double>());
        // As far as it goes each way (the keys' clamped ends).
        h.Settings.ZoomLevel = GameSettings.MaxZoomLevel;
        h.Frame();
        Assert.Equal(("500%", true, false), (s.Q("#zoom-value").TextContent, s.Q("#zoom-in").IsDisabled, s.Q("#zoom-out").IsDisabled));
        h.Settings.ZoomLevel = GameSettings.MinZoomLevel;
        h.Frame();
        Assert.Equal(("25%", false, true), (s.Q("#zoom-value").TextContent, s.Q("#zoom-in").IsDisabled, s.Q("#zoom-out").IsDisabled));
    }

    [Theory]
    [InlineData(0, "100%")]
    [InlineData(0.5, "110%")]
    [InlineData(-0.5, "91%")]
    [InlineData(3, "173%")]
    [InlineData(-7.5, "25%")]
    public void AZoomLevelIsSaidAsItsZoomInWholePercent(double level, string text) => Assert.Equal(text, SettingsScreen.ZoomText(level));

    [Fact]
    public void TheModsShowInMountingOrderWithTheirStates()
    {
        using var h = Harness();
        var s = ScreenParts.Attach(h);
        s.EveryState();
        s.Run(SettingsScreen.OpenCommand);
        var rows = h.Document.QuerySelectorAll(".settings-mod").ToList();
        Assert.Equal(["broken-vale.pak", "ember-rite.pak", "frost-hollow.pak", "lanterns.pak", "quiet-nights.pak"], rows.Select(r => r.GetAttribute("data-mod")));
        string Text(UiElement row, string selector) => row.Descendants().FirstOrDefault(e => e.Matches(selector))?.TextContent ?? "";
        Assert.Equal(["Broken Vale", "Ember Rite", "Frost Hollow", "lanterns", "Quiet Nights"], rows.Select(r => Text(r, "strong")));
        Assert.Equal(["0.1 · by Someone · broken-vale.pak", "1.0 · by Vatrix · ember-rite.pak", "0.4.2 · by Maren · frost-hollow.pak", "lanterns.pak", "2.1 · by Vatrix · quiet-nights.pak"],
            rows.Select(r => Text(r, ".settings-mod-meta")));
        Assert.Equal(["A test mod with values its checks refuse.", "A rite of embers: a chime for the shrines and a lantern sold in Alderkeep.", "Colder nights in the hollow.", "", ""],
            rows.Select(r => Text(r, ".settings-mod-description")));
        Assert.Equal(
        [
            "Left out: stand-in/items/lamp: \"Legendary\" isn't one of the choices: Common, Rare, Exotic (at $.rarity) (and 1 more)",
            "Loaded", "Loaded", "Loaded", "Off",
        ], rows.Select(r => Text(r, ".settings-mod-state")));
        Assert.Equal(["is-left-out", "is-loaded", "is-loaded", "is-loaded", "is-off"], rows.Select(r => new[] { "is-left-out", "is-loaded", "is-off" }.Single(r.HasClass)));
        Assert.Equal([true, true, true, true, false], rows.Select(r => r.Descendants().Single(e => e.Matches("input[type=checkbox]")).Checked));
        Assert.Empty(h.Document.QuerySelectorAll(".settings-mod-next-start"));
    }

    [Fact]
    public void AModsSwitchIsKeptAndSaysWhenItTakesEffect()
    {
        using var h = Harness();
        var s = ScreenParts.Attach(h);
        s.EveryState();
        // Switched before the window opens: it says so at once.
        s.Parts.Mods.Switch("frost-hollow.pak", false);
        s.Run(SettingsScreen.OpenCommand);
        UiElement Row(string file) => h.Q($".settings-mod[data-mod=\"{file}\"]");
        UiElement Box(string file) => Row(file).Descendants().Single(e => e.Matches("input[type=checkbox]"));
        string? Note(string file) => Row(file).Descendants().FirstOrDefault(e => e.HasClass("settings-mod-next-start"))?.TextContent;
        Assert.Equal("Takes effect when the game is next started.", Note("frost-hollow.pak"));
        Assert.False(Box("frost-hollow.pak").Checked);
        // Switched off in the list: kept, said, the focus still on the same box.
        var box = Box("ember-rite.pak");
        h.Sounds.Clear();
        ClickInView(h, box);
        Assert.False(h.Settings.IsModOn("ember-rite.pak"));
        Assert.Same(box, Box("ember-rite.pak"));
        Assert.Same(box, h.Document.FocusedElement);
        Assert.Equal(SettingsScreen.NextStart, Note("ember-rite.pak"));
        Assert.Equal(["ui-toggle"], h.Sounds);
        Assert.Equal("Loaded", Row("ember-rite.pak").Descendants().Single(e => e.HasClass("settings-mod-state")).TextContent);
        var mods = File(h)["mods"]!.AsObject();
        Assert.Equal(["ember-rite.pak", "frost-hollow.pak", "quiet-nights.pak"], mods.Select(m => m.Key));
        Assert.All(mods, m => Assert.False(m.Value!["on"]!.GetValue<bool>()));
        // Switched back: as at the start, nothing to say.
        ClickInView(h, box);
        Assert.True(h.Settings.IsModOn("ember-rite.pak"));
        Assert.Null(Note("ember-rite.pak"));
        // A mod off at the start switched on says so too.
        ClickInView(h, Box("quiet-nights.pak"));
        Assert.True(h.Settings.IsModOn("quiet-nights.pak"));
        Assert.Equal(SettingsScreen.NextStart, Note("quiet-nights.pak"));
        Assert.Equal(["frost-hollow.pak"], File(h)["mods"]!.AsObject().Select(m => m.Key));
    }

    [Theory]
    [InlineData(ModState.Loaded, null, 0, "Loaded")]
    [InlineData(ModState.Off, null, 0, "Off")]
    [InlineData(ModState.LeftOut, "not a pack (it doesn't start with SVPK)", 0, "Left out: not a pack (it doesn't start with SVPK)")]
    [InlineData(ModState.LeftOut, "a: b", 3, "Left out: a: b (and 3 more)")]
    public void AModsStateInTheListsWords(ModState state, string? problem, int more, string text) =>
        Assert.Equal(text, SettingsScreen.StateText(new ModEntry("m.pak", "M", null, null, null, state, problem, more)));

    [Fact]
    public void WithNoModsTheListSaysSo()
    {
        using var h = Harness();
        var s = ScreenParts.Attach(h);
        s.Start(off: []);
        s.Run(SettingsScreen.OpenCommand);
        Assert.Empty(h.Document.QuerySelectorAll(".settings-mod"));
        Assert.Equal("No mods. A mod is a .pak file in the mods folder of the game folder (Menu › Open game folder).", s.Q(".settings-mods-empty").TextContent);
    }
}

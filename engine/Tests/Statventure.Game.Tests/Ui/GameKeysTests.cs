using Statventure.Game.Settings;
using Statventure.Game.Ui;
using Statventure.Platform;
using Statventure.UI.Dom;

namespace Statventure.Game.Tests;

/// <summary>
/// The commands and keys (Docs/Game/Foundation.md, "Keys", "How it works", step 8): the game's keys as Electron 44's
/// menu gave them (Ctrl+S, Ctrl+O, the View roles' and Quit's, read from Electron's source), plain Ctrl+= too (Q30), no
/// Reload (Q18); the document part's own commands (full screen, the zoom in Electron's clamped half steps, the game
/// folder, Quit).
/// </summary>
[Collection("Game log")]
public class GameKeysTests
{
    [Fact]
    public void TheMenusRolesHaveElectronsLabelsAndKeysOnEachSystem()
    {
        Assert.Equal(
            [("zoom-reset", "Actual Size", "Ctrl+0"), ("zoom-in", "Zoom In", "Ctrl+Plus"), ("zoom-out", "Zoom Out", "Ctrl+-"), ("fullscreen", "Toggle Full Screen", "F11"), ("quit", "Quit", "Ctrl+Q")],
            MenuRoles.All(windows: false).Select(r => (r.Command, r.Label, r.Keys)));
        Assert.Equal(("quit", "Exit", (string?)null), (MenuRoles.Quit(true).Command, MenuRoles.Quit(true).Label, MenuRoles.Quit(true).Keys));
        Assert.Equal(
            [("Ctrl+S", "save-character"), ("Ctrl+O", "load-character"), ("Ctrl+0", "zoom-reset"), ("Ctrl+Plus", "zoom-in"), ("Ctrl+-", "zoom-out"), ("F11", "fullscreen"), ("Ctrl+Q", "quit"), ("Ctrl+=", "zoom-in")],
            GameUi.Keys(windows: false));
        // On Windows Quit has no keys (Electron's quit role there).
        Assert.DoesNotContain(GameUi.Keys(windows: true), k => k.Command == "quit");
        Assert.Equal(7, GameUi.Keys(windows: true).Count);
        // The document's: the system the game follows (the tests follow Chromium on Linux).
        using var h = new GameUiHarness();
        Assert.Equal(GameUi.Keys(windows: false), h.Ui.Commands.Bindings);
        Assert.Equal("Ctrl+Plus", h.Ui.Commands.KeysOf("zoom-in"));
        Assert.Null(h.Ui.Commands.KeysOf("reload"));
        // Keys are bound once (Ctrl+Shift+= is Ctrl+Plus).
        Assert.Throws<InvalidOperationException>(() => h.Ui.Commands.Bind("Ctrl+Shift+=", "other"));
    }

    [Theory]
    [InlineData("Ctrl+S", "s", KeyModifiers.Control)]
    [InlineData("CmdOrCtrl+O", "o", KeyModifiers.Control)]
    [InlineData("CommandOrControl+0", "0", KeyModifiers.Control)]
    [InlineData("Ctrl+Plus", "=", KeyModifiers.Control | KeyModifiers.Shift)]
    [InlineData("Ctrl+-", "-", KeyModifiers.Control)]
    [InlineData("Ctrl+=", "=", KeyModifiers.Control)]
    [InlineData("Shift+Ctrl+_", "-", KeyModifiers.Control | KeyModifiers.Shift)]
    [InlineData("F11", "F11", KeyModifiers.None)]
    [InlineData("Alt+Meta+Esc", "Escape", KeyModifiers.Alt | KeyModifiers.Super)]
    public void AnAcceleratorsTextIsItsKeyAndModifiers(string text, string key, KeyModifiers modifiers) =>
        Assert.Equal(new KeyChord(key, modifiers), KeyChord.Parse(text));

    [Theory]
    [InlineData("Ctrl")]
    [InlineData("Ctrl+Space bar")]
    [InlineData("F25")]
    [InlineData("Ctrl+Bogus+S")]
    public void TextNamingNoKeyIsRefused(string text) => Assert.Throws<FormatException>(() => KeyChord.Parse(text));

    [Fact]
    public void AKeyEventIsItsUnshiftedKeyAndExactlyItsModifiers()
    {
        static UiKeyboardEvent Event(string key, bool ctrl = false, bool shift = false, bool alt = false, bool meta = false) =>
            new("keydown") { Key = key, CtrlKey = ctrl, ShiftKey = shift, AltKey = alt, MetaKey = meta };
        Assert.Equal(new KeyChord("s", KeyModifiers.Control | KeyModifiers.Shift), KeyChord.Of(Event("S", ctrl: true, shift: true)));
        Assert.Equal(new KeyChord("=", KeyModifiers.Control | KeyModifiers.Shift), KeyChord.Of(Event("+", ctrl: true, shift: true)));
        Assert.Equal(new KeyChord("0", KeyModifiers.Control | KeyModifiers.Shift), KeyChord.Of(Event(")", ctrl: true, shift: true)));
        Assert.Equal(new KeyChord("F11", KeyModifiers.Alt | KeyModifiers.Super), KeyChord.Of(Event("F11", alt: true, meta: true)));
    }

    [Fact]
    public void EachKeyRunsItsCommand()
    {
        using var h = new GameUiHarness();
        var ran = new List<string>();
        h.Commands(ran, "save-character", "load-character");
        Assert.False(h.Key("s", "KeyS", KeyModifiers.Control));
        Assert.False(h.Key("o", "KeyO", KeyModifiers.Control));
        Assert.Equal(["save-character", "load-character"], ran);
        // Zoom In: Electron's Ctrl+Plus, the =/+ key with Shift (the engine names the key as typed without Shift, a
        // browser as "+"), and plain Ctrl+= (Q30).
        h.Key("=", "Equal", KeyModifiers.Control | KeyModifiers.Shift);
        Assert.Equal(0.5, h.Settings.ZoomLevel);
        h.Key("+", "Equal", KeyModifiers.Control | KeyModifiers.Shift);
        Assert.Equal(1.0, h.Settings.ZoomLevel);
        h.Key("=", "Equal", KeyModifiers.Control);
        Assert.Equal(1.5, h.Settings.ZoomLevel);
        h.Key("-", "Minus", KeyModifiers.Control);
        Assert.Equal(1.0, h.Settings.ZoomLevel);
        h.Key("0", "Digit0", KeyModifiers.Control);
        Assert.Equal(0.0, h.Settings.ZoomLevel);
        h.Key("F11", "F11");
        Assert.True(h.Settings.Fullscreen);
        h.Key("F11", "F11");
        Assert.False(h.Settings.Fullscreen);
        h.Key("q", "KeyQ", KeyModifiers.Control);
        Assert.Equal(["quit"], h.Done);
    }

    [Fact]
    public void KeysWithOtherModifiersAndReloadDoNothing()
    {
        using var h = new GameUiHarness();
        var ran = new List<string>();
        h.Commands(ran, "save-character", "load-character", "reload");
        Assert.True(h.Key("s", "KeyS", KeyModifiers.Control | KeyModifiers.Shift));
        Assert.True(h.Key("s", "KeyS", KeyModifiers.Control | KeyModifiers.Alt));
        Assert.True(h.Key("s", "KeyS"));
        Assert.True(h.Key("r", "KeyR", KeyModifiers.Control));
        Assert.True(h.Key("F11", "F11", KeyModifiers.Shift));
        Assert.True(h.Key("-", "Minus", KeyModifiers.Control | KeyModifiers.Shift));
        Assert.True(h.Key("0", "Numpad0", KeyModifiers.Control | KeyModifiers.Alt));
        Assert.Empty(ran);
        Assert.Equal((0.0, false), (h.Settings.ZoomLevel, h.Settings.Fullscreen));
        Assert.Empty(h.Done);
    }

    [Fact]
    public void AKeyThePageHandledIsntTheMenus()
    {
        using var h = new GameUiHarness();
        var ran = new List<string>();
        h.Commands(ran, "save-character");
        h.Document.AddEventListener<UiKeyboardEvent>("keydown", e => e.PreventDefault(), capture: true);
        h.Key("s", "KeyS", KeyModifiers.Control);
        Assert.Empty(ran);
    }

    [Fact]
    public void TheKeysWorkWithAWindowOpenAndFromAField()
    {
        using var h = new GameUiHarness();
        var ran = new List<string>();
        h.Commands(ran, "load-character");
        _ = h.Ui.Dialogs.Prompt("Name your character", "Character 1", "OK");
        h.Frame();
        Assert.True(h.Q(".game-prompt-field").IsFocused);
        h.Key("o", "KeyO", KeyModifiers.Control);
        Assert.Equal(["load-character"], ran);
    }

    [Fact]
    public void TheZoomStepsByHalfALevelClampedAtElectronsLimits()
    {
        using var h = new GameUiHarness(before: s => s.ZoomLevel = 8.5);
        var max = GameSettings.MaxZoomLevel;
        Assert.Equal(Math.Log(5) / Math.Log(1.2), max);
        Assert.Equal(Math.Log(0.25) / Math.Log(1.2), GameSettings.MinZoomLevel);
        h.Ui.Commands.Run("zoom-in");
        Assert.Equal(max, h.Settings.ZoomLevel);
        h.Ui.Commands.Run("zoom-in");
        Assert.Equal(max, h.Settings.ZoomLevel);
        Assert.Equal(5f, h.Document.PageZoom, 4);
        // A step back from the clamped end is off the half steps, as Electron's.
        h.Ui.Commands.Run("zoom-out");
        Assert.Equal(max - 0.5, h.Settings.ZoomLevel);
        h.Ui.Commands.Run("zoom-reset");
        Assert.Equal(0, h.Settings.ZoomLevel);
        h.Settings.ZoomLevel = -7.5;
        h.Ui.Commands.Run("zoom-out");
        Assert.Equal(GameSettings.MinZoomLevel, h.Settings.ZoomLevel);
        Assert.Equal(0.25f, h.Document.PageZoom, 4);
        h.Ui.Commands.Run("zoom-out");
        Assert.Equal(GameSettings.MinZoomLevel, h.Settings.ZoomLevel);
        h.Ui.Commands.Run("zoom-in");
        Assert.Equal(GameSettings.MinZoomLevel + 0.5, h.Settings.ZoomLevel);
    }

    [Fact]
    public void ThePartsCommandsDoTheirJobs()
    {
        using var h = new GameUiHarness();
        Assert.Equal(["fullscreen", "open-game-folder", "quit", "zoom-in", "zoom-out", "zoom-reset"], h.Ui.Commands.Names.Order(StringComparer.Ordinal));
        h.Ui.Commands.Run("fullscreen");
        Assert.True(h.Settings.Fullscreen);
        h.Ui.Commands.Run("fullscreen");
        Assert.False(h.Settings.Fullscreen);
        h.Ui.Commands.Run("open-game-folder");
        h.OpenFolderWorks = false;
        var log = GameTesting.CaptureLog(() => h.Ui.Commands.Run("open-game-folder"));
        Assert.Equal(["Warning [Ui] the game folder couldn't be opened in a file manager"], log.Where(l => l.Contains("[Ui]")));
        h.Ui.Commands.Run("quit");
        Assert.Equal(["open game folder", "open game folder", "quit"], h.Done);
    }
}

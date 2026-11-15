using Statventure.Game.Screens.Settings;
using Statventure.Game.Settings;
using Statventure.UI.Dom;

namespace Statventure.Game.Tests;

/// <summary>
/// The lines the end-to-end flow reads (GM1-P21; Docs/Game/Foundation.md, "How it works", step 11): what the start screen
/// and the settings screen show, logged as they show it. Each line is held against the element it speaks of in the
/// document (its text, its state), so the log can't say one thing and the screen another. Run alone: they read the log.
/// </summary>
[Collection("Game log")]
public class ScreenLinesTests
{
    /// <summary>The log's lines of <paramref name="category"/> from the first that is <paramref name="from"/> on, "Info [Game] …" cut to the message.</summary>
    static List<string> After(List<string> log, string category, string from)
    {
        var prefix = $"Info [{category}] ";
        var lines = log.Where(l => l.StartsWith(prefix, StringComparison.Ordinal)).Select(l => l[prefix.Length..]).ToList();
        var at = lines.IndexOf(from);
        Assert.True(at >= 0, $"no line '{from}' in: {string.Join(" | ", lines)}");
        return lines[at..];
    }

    [Fact]
    public void AFirstStartsStartScreenSaysContinueIsGreyed()
    {
        ScreensHarness? s = null;
        var log = GameTesting.CaptureLog(() => s = new ScreensHarness());
        using var _ = s!;
        Assert.Equal(["the start screen shown", "Continue greyed"], After(log, "Game", "the start screen shown"));
        Assert.True(s.Q("#start-continue").IsDisabled);
        Assert.True(s.Q("#start-left-out").Hidden);
    }

    [Fact]
    public void WithACharacterTheStartScreenSaysWhichSlotContinueOpens()
    {
        ScreensHarness? s = null;
        var log = GameTesting.CaptureLog(() => s = new ScreensHarness(before: ScreensHarness.WithCharacter("Ser Alrik")));
        using var _ = s!;
        Assert.Equal(["the start screen shown", "Continue opens ser-alrik"], After(log, "Game", "the start screen shown"));
        Assert.False(s.Q("#start-continue").IsDisabled);
        Assert.Equal("ser-alrik", s.Session.ContinueSlot());
        // Shown again (the character opened and left): said again, as it shows then.
        log = GameTesting.CaptureLog(() =>
        {
            s.Session.Delete("ser-alrik");
            s.Session.New("Mira");
            s.H.Frame();
            s.H.Ui.Commands.Run("start-screen");
            s.H.Frame();
        });
        Assert.Equal(["the start screen shown", "Continue opens mira"], After(log, "Game", "the start screen shown"));
        Assert.False(s.Q("#start-continue").IsDisabled);
    }

    [Theory]
    [InlineData(new[] { "Ember Rite" }, "1 mod was")]
    [InlineData(new[] { "Ember Rite", "Frost Pack", "Old Maps", "Night Market", "Bone Choir" }, "5 mods were")]
    public void TheStartScreenSaysItsLeftOutLineAsTheLineHoldsIt(string[] names, string count)
    {
        ScreensHarness? s = null;
        var log = GameTesting.CaptureLog(() => s = new ScreensHarness(leftOut: names));
        using var _ = s!;
        var line = s.Q("#start-left-out");
        Assert.False(line.Hidden);
        Assert.Equal(["the start screen shown", "Continue greyed", "the left-out line: " + line.TextContent], After(log, "Game", "the start screen shown"));
        Assert.StartsWith($"the left-out line: {count} left out: {names.Order(StringComparer.Ordinal).First()}", After(log, "Game", "the start screen shown")[2]);
        Assert.EndsWith(" Settings › Mods tells why.", line.TextContent);
    }

    /// <summary>The values line as the window shows it, read here from the elements themselves.</summary>
    static string ValuesShown(ScreenParts s)
    {
        string Text(string selector) => s.Q(selector).TextContent;
        static string OnOff(bool on) => on ? "on" : "off";
        return $"master {Text("#master-volume-value")}, music {Text("#music-volume-value")}, effects {Text("#effects-volume-value")}, " +
            $"theme {s.Q("#game-theme").SelectedOption!.TextContent}, zoom {Text("#zoom-value")}, full screen {OnOff(s.Q("#game-fullscreen").Checked)}, " +
            $"Dev Mode {OnOff(s.Q("#dev-mode").Checked)}";
    }

    [Fact]
    public void TheSettingsScreenSaysTheValuesItShowsAndAnEmptyModList()
    {
        using var h = new GameUiHarness(1000, 860);
        var s = ScreenParts.Attach(h);
        s.Start(off: []);
        var log = GameTesting.CaptureLog(() => s.Run(SettingsScreen.OpenCommand));
        var lines = After(log, "Settings", "the settings screen opened");
        Assert.Equal(["the settings screen opened", "master 100%, music 100%, effects 60%, theme Dark, zoom 100%, full screen off, Dev Mode off", "the mod list: none"], lines);
        Assert.Equal(ValuesShown(s), lines[1]);
        Assert.Equal(SettingsScreen.NoMods, s.Q(".settings-mods-empty").TextContent);
        Assert.Empty(h.Document.QuerySelectorAll(".settings-mod"));

        // Other values: as the window shows them when it opens again.
        foreach (var (master, effects, theme, zoom, fullscreen, devMode, line) in new[]
        {
            (0.25, 0.333, GameTheme.Light, 1.0, false, true, "master 25%, music 100%, effects 33%, theme Light, zoom 120%, full screen off, Dev Mode on"),
            (1.0, 0.0, GameTheme.Dark, -0.5, true, false, "master 100%, music 100%, effects 0%, theme Dark, zoom 91%, full screen on, Dev Mode off"),
        })
        {
            h.Ui.Windows.Close(s.Settings.Layer);
            (h.Settings.MasterVolume, h.Settings.EffectsVolume, h.Settings.Theme, h.Settings.ZoomLevel, h.Settings.Fullscreen, h.Settings.DevMode) =
                (master, effects, theme, zoom, fullscreen, devMode);
            log = GameTesting.CaptureLog(() => s.Run(SettingsScreen.OpenCommand));
            lines = After(log, "Settings", "the settings screen opened");
            Assert.Equal(line, lines[1]);
            Assert.Equal(ValuesShown(s), lines[1]);
        }
        // Opened while open: nothing said again (it stays as it is).
        log = GameTesting.CaptureLog(() => s.Run(SettingsScreen.OpenCommand));
        Assert.DoesNotContain(log, l => l.StartsWith("Info [Settings]", StringComparison.Ordinal));
    }

    [Fact]
    public void TheSettingsScreenSaysEachModsRowAsItShowsIt()
    {
        using var h = new GameUiHarness(1000, 860);
        var s = ScreenParts.Attach(h);
        s.EveryState();
        s.Parts.Mods.Switch("frost-hollow.pak", false);
        var log = GameTesting.CaptureLog(() => s.Run(SettingsScreen.OpenCommand));
        var lines = After(log, "Settings", "the settings screen opened");
        Assert.Equal(
        [
            "mod broken-vale.pak: Left out: stand-in/items/lamp: \"Legendary\" isn't one of the choices: Common, Rare, Exotic (at $.rarity) (and 1 more)",
            "mod ember-rite.pak: Loaded",
            "mod frost-hollow.pak: Loaded; Takes effect when the game is next started.",
            "mod lanterns.pak: Loaded",
            "mod quiet-nights.pak: Off",
        ], lines[2..]);
        // Each line the row's own: its file, its state's words, its next-start line.
        var rows = h.Document.QuerySelectorAll(".settings-mod").ToList();
        Assert.Equal(rows.Count, lines.Count - 2);
        foreach (var (row, line) in rows.Zip(lines[2..]))
        {
            string? Text(string name) => row.Descendants().FirstOrDefault(e => e.HasClass(name))?.TextContent;
            var next = Text("settings-mod-next-start");
            Assert.Equal($"mod {row.GetAttribute("data-mod")}: {Text("settings-mod-state")}{(next is null ? "" : "; " + next)}", line);
        }
        Assert.Empty(h.Document.QuerySelectorAll(".settings-mods-empty"));
    }
}

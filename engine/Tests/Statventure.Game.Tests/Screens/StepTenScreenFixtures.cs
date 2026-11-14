using Statventure.Game.Screens.Picker;
using Statventure.Game.Screens.Settings;
using Statventure.Game.Screens.Slots;
using Statventure.Game.Settings;
using Statventure.Platform;

namespace Statventure.Game.Tests;

/// <summary>
/// Step 10's screens compared with Chromium (GM1-P17), as the user approved them (2026-11-14), each in both themes at
/// 1000 × 860: the settings screen (a first start's values; scrolled to the mods with every state; with no mods; Dev Mode
/// on), the slot list (several characters with a damaged one of each kind; empty; Delete's question), the damaged-save
/// question (with a backup that reads, and without), the file picker (Save in Documents with the name filled in; the
/// replace question; Load in Documents; a long folder scrolled; the recent places). Every scene is the same on every
/// machine (<see cref="ScreenParts"/>: fixed names, contents and times shown in UTC; the picker's folders its own). The
/// pointer rests where no hover look shows.
/// </summary>
sealed class StepTenScreenFixtures : IGameScreenFixtures
{
    /// <summary>The fixtures' size (whole device pixels at 1.65).</summary>
    public const int Width = 1000, Height = 860;

    public IEnumerable<GameScreenFixture> Fixtures
    {
        get
        {
            foreach (var theme in new[] { GameTheme.Dark, GameTheme.Light })
            {
                var t = theme.ToString().ToLowerInvariant();
                foreach (var (name, setup) in Scenes)
                    yield return new($"{name}-{t}", Width, Height, theme, setup);
            }
        }
    }

    /// <summary>The scenes, by name (the approval's captures').</summary>
    public static readonly (string Name, Action<GameUiHarness> Setup)[] Scenes =
    [
        ("settings-first-start", h => ScreenParts.Attach(h).Run(SettingsScreen.OpenCommand)),
        ("settings-mods", h =>
        {
            var s = ScreenParts.Attach(h);
            s.EveryState();
            s.Parts.Mods.Switch("frost-hollow.pak", false);
            s.Run(SettingsScreen.OpenCommand);
            ScrollToEnd(s);
        }),
        ("settings-no-mods", h =>
        {
            var s = ScreenParts.Attach(h);
            s.Start(off: []);
            s.Run(SettingsScreen.OpenCommand);
            ScrollToEnd(s);
        }),
        ("settings-dev-mode", h =>
        {
            var s = ScreenParts.Attach(h);
            s.Run(SettingsScreen.OpenCommand);
            // Switched on as the box does it (the box's change sets the setting).
            h.Settings.DevMode = true;
            ScrollTo(s, "#dev-mode", 300);
        }),
        ("slots-several", h =>
        {
            var s = ScreenParts.Attach(h);
            s.SeveralCharacters();
            s.Run(SlotsScreen.OpenCommand);
        }),
        ("slots-empty", h => ScreenParts.Attach(h).Run(SlotsScreen.OpenCommand)),
        ("slots-delete", h =>
        {
            var s = ScreenParts.Attach(h);
            s.SeveralCharacters();
            s.Run(SlotsScreen.OpenCommand);
            // Pressed by the keyboard (after a mouse press the question's focused action wouldn't match :focus-visible,
            // which PaintReference's script focus has).
            s.Q(".slot-row[data-slot=\"ser-alrik\"] .slot-delete").Focus();
            h.Key("Enter", "Enter");
            h.Frame();
        }),
        // Asked from the list by its command (as Enter on a row's Open: a press of the mouse before the focus moves would
        // leave the answer focused without :focus-visible, which PaintReference's script focus has).
        ("damaged-backup", h =>
        {
            var s = ScreenParts.Attach(h);
            s.SeveralCharacters();
            s.Run(SlotsScreen.OpenCommand);
            s.Run(SlotsScreen.OpenSlotCommand, "brother-cadfael");
        }),
        ("damaged-no-backup", h =>
        {
            var s = ScreenParts.Attach(h);
            s.SeveralCharacters();
            s.Run(SlotsScreen.OpenCommand);
            s.Run(SlotsScreen.OpenSlotCommand, "mira-of-the-vale");
        }),
        ("picker-save", h =>
        {
            var s = ScreenParts.Attach(h);
            s.SceneFolders();
            s.Session.New("Ser Alrik");
            s.Run(PickerScreen.SaveCommand);
        }),
        ("picker-save-replace", h =>
        {
            var s = ScreenParts.Attach(h);
            s.SceneFolders();
            s.Session.New("Ser Alrik");
            s.Run(PickerScreen.SaveCommand);
            // The file that is there picked from the listing (Enter on its row).
            s.OpenPicker.Listing.Focus();
            s.OpenPicker.Select(4);
            h.Key("Enter", "Enter");
            s.PointerAway();
        }),
        ("picker-load", h =>
        {
            var s = ScreenParts.Attach(h);
            s.SceneFolders();
            s.Run(PickerScreen.LoadCommand);
        }),
        ("picker-load-long", h =>
        {
            var s = ScreenParts.Attach(h);
            s.SceneFolders();
            h.Settings.AddRecentPlace(ScreenParts.Documents + "/Stat Fick archive");
            s.Run(PickerScreen.LoadCommand);
            s.OpenPicker.Listing.ScrollTop = FilePicker.RowHeight * 600;
            h.Frame();
            h.Frame();
            s.OpenPicker.Select(604);
            h.Frame();
        }),
        ("picker-load-recent", h =>
        {
            var s = ScreenParts.Attach(h);
            s.SceneFolders();
            foreach (var folder in new[] { ScreenParts.Home + "/Downloads", ScreenParts.Documents + "/Stat Fick archive", ScreenParts.Documents + "/old saves" })
                h.Settings.AddRecentPlace(folder);
            s.Run(PickerScreen.LoadCommand);
            s.OpenPicker.Select(0);
            h.Frame();
        }),
    ];

    /// <summary>The settings window's content scrolled to its end, where the mods are.</summary>
    static void ScrollToEnd(ScreenParts s)
    {
        Unfocus(s);
        var content = s.Q(".settings-content");
        content.ScrollTop = content.ScrollHeight;
        s.Harness.Frame();
    }

    /// <summary>The settings window's content scrolled so <paramref name="selector"/> is <paramref name="below"/> px under its top.</summary>
    static void ScrollTo(ScreenParts s, string selector, float below)
    {
        Unfocus(s);
        var content = s.Q(".settings-content");
        var offset = s.Q(selector).GetBoundingClientRect().Y - content.GetBoundingClientRect().Y + content.ScrollTop;
        content.ScrollTop = MathF.Max(0, offset - below);
        s.Harness.Frame();
    }

    /// <summary>
    /// The focus taken off the first slider (where the window put it) before the content scrolls, as a press on the
    /// window's text does: PaintReference focuses the fixture's focused element after scrolling, and a focus scrolls its
    /// element into view, so a focused slider out of view would scroll Chromium's page back to it.
    /// </summary>
    static void Unfocus(ScreenParts s)
    {
        s.Harness.Document.FocusedElement?.Blur();
        s.Harness.Frame();
    }
}

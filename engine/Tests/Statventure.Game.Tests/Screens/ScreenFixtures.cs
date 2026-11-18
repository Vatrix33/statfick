using System.Numerics;
using Statventure.Game.Settings;
using Statventure.Game.Ui;

namespace Statventure.Game.Tests;

/// <summary>
/// Step 9's screens compared with Chromium (GM1-P17), as the user approved them (2026-11-14): the start screen (a first
/// start with Continue greyed, a character to continue, the line naming one, two and five mods left out, its status line),
/// New character's prompt (filled in, and refusing an empty name), the character's page with its top bar (since GM2 a
/// hero with a race and a look, Q26: the sheet's top and the Field tools rail beside it; the autosave dot
/// lit, and a file message on its status line), the menu (on the page and on the start screen) and About (at its top and
/// scrolled to its end). The main screens in both themes, their variants in one. The windows are opened by their commands
/// (as Escape, <c>--about</c> and <c>--open</c> open them), so their first control has the focus as a script gives it; the
/// pointer rests where no hover look shows. The page's fixtures are wide enough for its top bar on one line (over 1120 px).
/// </summary>
sealed class ScreenFixtures : IGameScreenFixtures
{
    const int Width = 800, Height = 680, PageWidth = 1200, PageHeight = 400;

    public IEnumerable<GameScreenFixture> Fixtures
    {
        get
        {
            foreach (var theme in new[] { GameTheme.Dark, GameTheme.Light })
            {
                var t = theme.ToString().ToLowerInvariant();
                yield return Start($"start-first-{t}", theme);
                yield return Start($"start-continue-{t}", theme, before: ScreensHarness.WithCharacter("Ser Alrik"));
                yield return Start($"start-left-out-one-{t}", theme, leftOut: ["Ember Rite"]);
                yield return Start($"start-new-character-refused-{t}", theme, setup: s =>
                {
                    s.H.Ui.Commands.Run("new-character");
                    s.H.Frame();
                    s.Q(".game-prompt-field").Value = "";
                    s.H.Click(s.Q(".game-dialog-accept"));
                    Away(s);
                });
                yield return Page($"page-{t}", theme, s => s.NewHero("Ser Alrik"));
                yield return Start($"menu-page-{t}", theme, before: ScreensHarness.WithHero("Ser Alrik"), masks: [SheetScreenFixtures.SealMask], setup: s =>
                {
                    s.H.Ui.Commands.Run("continue");
                    s.H.Frame();
                    s.H.Ui.Commands.Run(GameUi.MenuCommand);
                });
                yield return Start($"about-top-{t}", theme, setup: s => s.H.Ui.Commands.Run("about"));
            }
            yield return Start("start-left-out-two-dark", GameTheme.Dark, leftOut: ["Ember Rite", "Frost Pack"]);
            yield return Start("start-left-out-many-dark", GameTheme.Dark, leftOut: ["Ember Rite", "Frost Pack", "Old Maps", "Night Market", "Bone Choir"]);
            yield return Start("start-status-dark", GameTheme.Dark, setup: s => s.H.Ui.Status(Session.GameSession.NotACharacterSave, StatusKind.Problem));
            yield return Start("start-new-character-dark", GameTheme.Dark, setup: s => s.H.Ui.Commands.Run("new-character"));
            yield return Page("page-status-light", GameTheme.Light, s =>
            {
                s.NewHero("Ser Alrik");
                s.H.Wait(0.5);
                s.H.Ui.Status("Character saved");
            });
            yield return Start("menu-start-dark", GameTheme.Dark, setup: s => s.H.Ui.Commands.Run(GameUi.MenuCommand));
            yield return Start("about-end-dark", GameTheme.Dark, setup: s =>
            {
                s.H.Ui.Commands.Run("about");
                s.H.Frame();
                s.Q(".about-content").ScrollTo(0, 1_000_000, UI.Dom.ScrollMode.Instant);
            });
        }
    }

    /// <summary>A fixture of the screens at the start screen's size, put in its state by <paramref name="setup"/>.</summary>
    static GameScreenFixture Start(string name, GameTheme theme, IReadOnlyList<string>? leftOut = null, Action<Session.GameSession>? before = null, Action<ScreensHarness>? setup = null,
        IReadOnlyList<string>? masks = null) =>
        new(name, Width, Height, theme, h => Screens(h, leftOut, before, setup), masks);

    /// <summary>A fixture of the character's page at the page's size (the sheet's race seal masked, the user's Q50: compared on its own).</summary>
    static GameScreenFixture Page(string name, GameTheme theme, Action<ScreensHarness> setup) =>
        new(name, PageWidth, PageHeight, theme, h => Screens(h, null, null, setup), [SheetScreenFixtures.SealMask]);

    /// <summary>
    /// The screens attached to the fixture's own document (<see cref="GameScreenFixtures.Build"/> makes it) with the real
    /// parts, as <see cref="ScreensHarness"/> attaches them, then the state.
    /// </summary>
    static void Screens(GameUiHarness h, IReadOnlyList<string>? leftOut, Action<Session.GameSession>? before, Action<ScreensHarness>? setup)
    {
        var screens = new ScreensHarness(h, leftOut, before);
        h.Frame();
        setup?.Invoke(screens);
        h.Frame();
    }

    /// <summary>The pointer off every control (no hover look).</summary>
    static void Away(ScreensHarness s) => s.H.Move(new Vector2(2, 2));
}

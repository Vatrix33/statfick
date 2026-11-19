using Statventure.Game.Screens.Races;
using Statventure.Game.Settings;

namespace Statventure.Game.Tests;

/// <summary>
/// The race carousel and the character maker compared with Chromium (GM1-P17), as the user approved them (2026-11-16):
/// the carousel on a playable race for a new hero, on another race than a hero's own ("Race locked"), in Dev Mode on an
/// enemy-only race, on a playable race with no looks in the data (the user's answer of 2026-11-16); the maker on its
/// first preset, with a category open, after the random button, for a race with ears and a tail; and both at the game's
/// smallest window width (820 px, tall enough to hold them: the page scrolling, the root scrollbar's thumb is the
/// engine's own, step 8's "How it works"). The main states in both themes, their variants in one. The carousel at
/// 1240 × 900 (Dev Mode's at 1240 × 1040: its strip's two rows lie where the pointer below rests at 900), the maker at
/// 1480 × 900 (multiples of 20 px), so the headless compositor's pointer (its output's middle, 1200 device px down and
/// right: at 1.65 the page's 727 px) rests where nothing lights (<see cref="HeroScreenFixturesTests"/>). The
/// data is the screens' test data (<see cref="HeroTestData"/>). The figure panels and the emblem's view show nothing in
/// the program until GM2 step 11 (no view: the panel's colour, the ✦ placeholder), so nothing is masked; the mask of every
/// <c>surface</c> box is GM2 step 5's (GM2-P12) and must be in place before a view draws there.
/// </summary>
sealed class HeroScreenFixtures : IGameScreenFixtures
{
    const int Width = 1240, MakerWidth = 1480, Height = 900, DevModeHeight = 1040, SmallestWidth = 820, SmallestHeight = 1000;

    public IEnumerable<GameScreenFixture> Fixtures
    {
        get
        {
            foreach (var theme in new[] { GameTheme.Dark, GameTheme.Light })
            {
                var t = theme.ToString().ToLowerInvariant();
                yield return Fixture($"races-playable-{t}", theme, s =>
                {
                    s.NewHero();
                    s.Run(HeroFlow.RacesCommand);
                });
                yield return Fixture($"races-locked-{t}", theme, s =>
                {
                    s.NewHero();
                    s.Session.Change(save => save with { Hero = save.Hero with { Race = HeroTestData.Human } });
                    s.Run(HeroFlow.RacesCommand);
                    s.Key("ArrowRight");
                });
                yield return Fixture($"races-dev-mode-{t}", theme, s =>
                {
                    s.H.Settings.DevMode = true;
                    s.NewHero();
                    s.Run(HeroFlow.RacesCommand, "goblin");
                }, Width, DevModeHeight);
                yield return Fixture($"maker-presets-{t}", theme, s =>
                {
                    s.NewHero();
                    s.Run(HeroFlow.MakerCommand, HeroTestData.Human);
                }, MakerWidth);
                yield return Fixture($"maker-category-open-{t}", theme, s =>
                {
                    s.NewHero();
                    s.Run(HeroFlow.MakerCommand, HeroTestData.Human);
                    Press(s, s.Row("part:hair").ElementChildren.First());
                }, MakerWidth);
            }
            yield return Fixture("races-no-looks-light", GameTheme.Light, s =>
            {
                s.NewHero();
                s.Run(HeroFlow.RacesCommand, "elf");
            });
            yield return Fixture("maker-random-dark", GameTheme.Dark, s =>
            {
                s.NewHero();
                s.Run(HeroFlow.MakerCommand, HeroTestData.Human);
                Press(s, s.Q("#maker-random"));
            }, MakerWidth);
            yield return Fixture("maker-half-beast-light", GameTheme.Light, s =>
            {
                s.NewHero();
                s.Run(HeroFlow.MakerCommand, HeroTestData.HalfBeast);
                Press(s, s.Row("part:ears").ElementChildren.First());
            }, MakerWidth);
            yield return Fixture("races-smallest-window-dark", GameTheme.Dark, s =>
            {
                s.NewHero();
                s.Run(HeroFlow.RacesCommand);
            }, SmallestWidth, SmallestHeight);
            yield return Fixture("maker-smallest-window-light", GameTheme.Light, s =>
            {
                s.NewHero();
                s.Run(HeroFlow.MakerCommand, HeroTestData.Human);
            }, SmallestWidth, SmallestHeight);
        }
    }

    /// <summary>
    /// A button pressed by the keyboard (focused, then Enter), so its focus shows as Chromium shows a script's focus with no
    /// input before it (PaintReference focuses the fixture's focused element so): a focus moved after a mouse press doesn't
    /// match :focus-visible (Docs/Game/Foundation.md, "How it works", step 10, "The fixtures' own care").
    /// </summary>
    static void Press(HeroScreensHarness s, UI.Dom.UiElement button)
    {
        button.Focus();
        s.Key("Enter");
    }

    /// <summary>A fixture of the flow's screens on the fixture's own document (<see cref="GameScreenFixtures.Build"/> makes it) and the shared test data, put in its state by <paramref name="setup"/>.</summary>
    static GameScreenFixture Fixture(string name, GameTheme theme, Action<HeroScreensHarness> setup, int width = Width, int height = Height) =>
        new(name, width, height, theme, h => setup(new HeroScreensHarness(h)));
}

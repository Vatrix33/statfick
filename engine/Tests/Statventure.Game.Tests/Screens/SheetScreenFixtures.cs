using System.Numerics;
using Statventure.Game.Settings;
using Statventure.UI.Dom;

namespace Statventure.Game.Tests;

/// <summary>
/// The sheet compared with Chromium (GM2 step 7; GM1-P17), as the user approved its captures (2026-11-16): the played
/// hero's page in the game's first window (1440 × 960) at its top, at the paper doll and at its end (the attributes and the
/// footer), in both themes; Dev Mode's controls at the top and at the attributes; a new hero; a long name as the heading;
/// the window at its narrowest (820 px): the rail a drawer, closed and open, and the paper doll there. The portrait panel's
/// 3D view is empty until GM2 step 11 and draws nothing on either side, so it is compared as it is (the views' mask is GM2
/// step 5's, GM2-P12, and must be in place before a view draws there). No state flashes (the Level icon's alert needs the
/// EXP of a level up) and no field has the focus (a caret blinks); the pointer rests where no hover look shows.
/// </summary>
sealed class SheetScreenFixtures : IGameScreenFixtures
{
    const int Width = 1440, Height = 960, Narrow = 820, NarrowHeight = 700;

    /// <summary>
    /// The race seal's box, masked in every fixture showing the sheet (the user's Q50, 2026-11-16: "Leave the seal out"): its
    /// emblem is compared on its own against Blender (GM2 step 10), and the seal's tilt (11°) is the canvas's known
    /// difference on slanted box edges, measured by <c>box-slant</c>.
    /// </summary>
    public const string SealMask = "#race-select-button";

    public IEnumerable<GameScreenFixture> Fixtures
    {
        get
        {
            foreach (var theme in new[] { GameTheme.Dark, GameTheme.Light })
            {
                var t = theme.ToString().ToLowerInvariant();
                yield return Sheet($"sheet-played-{t}", Width, Height, theme, SheetHeroes.OpenPlayed);
                yield return Sheet($"sheet-doll-{t}", Width, Height, theme, SheetHeroes.OpenPlayed, scroll: 1000);
                yield return Sheet($"sheet-attributes-{t}", Width, Height, theme, SheetHeroes.OpenPlayed, scroll: 1600);
            }
            yield return Sheet("sheet-dev-light", Width, Height, GameTheme.Light, SheetHeroes.OpenPlayed, dev: true);
            yield return Sheet("sheet-dev-attributes-dark", Width, Height, GameTheme.Dark, SheetHeroes.OpenPlayed, dev: true, scroll: 1640);
            yield return Sheet("sheet-new-dark", Width, Height, GameTheme.Dark, SheetHeroes.OpenNew);
            yield return Sheet("sheet-long-name-light", Width, 420, GameTheme.Light,
                s => SheetHeroes.Open(s, "Brother Aldous Wynnefield of the Seventh Lantern, Warden of Greyhollow", SheetHeroes.Played(SheetHeroes.Context(s.H), "dwarf"), SheetHeroes.Look("dwarf")));
            yield return Sheet("sheet-drawer-closed-light", Narrow, NarrowHeight, GameTheme.Light, SheetHeroes.OpenPlayed);
            yield return Sheet("sheet-drawer-open-dark", Narrow, NarrowHeight, GameTheme.Dark, SheetHeroes.OpenPlayed, then: s =>
            {
                s.H.Click(s.Q("#field-tools-toggle"));
                s.Document.FocusedElement?.Blur();
                // The drawer slides in (game.css: transform 180 ms); Chromium draws the open drawer of the markup at rest.
                s.H.Wait(0.5);
            });
            yield return Sheet("sheet-narrow-doll-dark", Narrow, NarrowHeight, GameTheme.Dark, SheetHeroes.OpenPlayed, scroll: 1400);
        }
    }

    /// <summary>The screens on the fixture's document with the hero <paramref name="open"/> opens on its sheet, then the state and the page's scroll.</summary>
    static GameScreenFixture Sheet(string name, int width, int height, GameTheme theme, Func<ScreensHarness, string> open, bool dev = false,
        Action<ScreensHarness>? then = null, float scroll = 0) =>
        new(name, width, height, theme, Masks: [SealMask], Setup: h =>
        {
            h.Settings.DevMode = dev;
            var s = new ScreensHarness(h);
            h.Frame();
            open(s);
            h.Frame();
            then?.Invoke(s);
            h.Move(new Vector2(2, 2));
            if (scroll > 0)
                s.Document.Root.ScrollTo(0, scroll, ScrollMode.Instant);
            h.Frame();
        });
}

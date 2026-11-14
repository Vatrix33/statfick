using Statventure.Game.Settings;
using Statventure.UI.Dom;

namespace Statventure.Game.Tests;

/// <summary>
/// Step 8's screens compared with Chromium (GM1-P17), as the user approved them (2026-11-14), in both themes: the confirm
/// and prompt dialogs (the prompt with its text selected, refused with the validation bubble, and with line breaks), the
/// title tooltip and an open select list. The page under them is the game's own (its body filling the window,
/// document.css); the pointer rests where no hover look shows.
/// </summary>
sealed class DialogScreenFixtures : IGameScreenFixtures
{
    public IEnumerable<GameScreenFixture> Fixtures
    {
        get
        {
            foreach (var theme in new[] { GameTheme.Dark, GameTheme.Light })
            {
                var t = theme.ToString().ToLowerInvariant();
                yield return new($"dialog-confirm-{t}", 800, 600, theme, h =>
                {
                    Page(h);
                    _ = h.Ui.Dialogs.Confirm("Unlearn Fire Bolt? You will need to learn or purchase it again.", "Unlearn");
                });
                yield return new($"dialog-prompt-{t}", 800, 600, theme, h =>
                {
                    Page(h);
                    _ = h.Ui.Dialogs.Prompt("Name your character", "Character 1", "OK");
                });
                yield return new($"dialog-prompt-refused-{t}", 800, 600, theme, h =>
                {
                    Page(h);
                    _ = h.Ui.Dialogs.Prompt("Name your character", "", "OK", text => text.Trim().Length == 0 ? "Please fill out this field." : null);
                    h.Frame();
                    h.Click(h.Q(".game-dialog-accept"));
                    // The pointer off the button (no hover look), on the backdrop.
                    h.Move(new System.Numerics.Vector2(4, 4));
                });
                yield return new($"dialog-prompt-lines-{t}", 800, 600, theme, h =>
                {
                    Page(h);
                    _ = h.Ui.Dialogs.Prompt("Field Notes: choose one observed spell to archive (tier 3 limit):\n1. Fire Bolt · Tier 1\n2. Frost Lance · Tier 2\n3. Stone Skin · Tier 3", "1", "OK");
                });
                yield return new($"control-tooltip-{t}", 800, 600, theme, h =>
                {
                    Page(h);
                    // A title on text with no hover look of its own (the old map's Forage button's words).
                    var holder = h.Document.Body.Append("p", "hero-copy");
                    holder.SetAttribute("style", "position: absolute; left: 80px; top: 100px; margin: 0");
                    var span = holder.Append("span", text: "Forage · 1 hour");
                    span.SetAttribute("title", "Search the ground here for potion ingredients: one hour, and the area's encounter risk");
                    h.Frame();
                    h.Move(h.Middle(span));
                    h.Wait(0.7);
                });
                yield return new($"control-select-{t}", 1000, 860, theme, h =>
                {
                    Page(h);
                    h.Ui.Windows.Open(Markup.Element(SettingsMarkup));
                    h.Frame();
                    h.Click(h.Q("#battle-pace"));
                });
            }
            // A confirm asked from a window (as a screen's Delete or Load would): over it, its layer above every window's.
            yield return new("dialog-confirm-over-window-dark", 1000, 860, GameTheme.Dark, h =>
            {
                Page(h);
                h.Ui.Windows.Open(Markup.Element(SettingsMarkup));
                h.Frame();
                _ = h.Ui.Dialogs.Confirm("Sell all 3 non-favorited runes for 42 Silver? Favorites are kept.", "Sell");
            });
        }
    }

    /// <summary>The game's page as it is under a screen (document.css fills it to the window).</summary>
    static void Page(GameUiHarness h) => h.Frame();

    /// <summary>The old settings window (<c>old-game/index.html</c> 26–62, as it is), for the open list in its place.</summary>
    const string SettingsMarkup = """
        <div class="modal-layer settings-layer" id="settings-modal" hidden>
          <button class="modal-backdrop" type="button" data-settings-close aria-label="Close settings"></button>
          <section class="spell-picker-dialog settings-dialog" role="dialog" aria-modal="true" aria-labelledby="settings-title">
            <header class="dialog-header compact-dialog-header">
              <div><p class="eyebrow">PREFERENCES</p><h2 id="settings-title">Game <em>settings</em></h2></div>
              <button class="modal-close" type="button" data-settings-close aria-label="Close settings">×</button>
            </header>
            <div class="settings-content">
              <div class="settings-volume-heading">
                <label for="sound-volume">Sound volume</label>
                <output id="sound-volume-value" for="sound-volume">100%</output>
              </div>
              <input id="sound-volume" class="settings-volume-slider" type="range" min="0" max="100" step="1" value="100" aria-describedby="sound-volume-help" />
              <p id="sound-volume-help">Adjust regional and battle music. 0% is silent.</p>
              <div class="settings-section">
                <label for="battle-interface">Battle interface</label>
                <select id="battle-interface" aria-describedby="battle-interface-help">
                  <option value="stage">Animated battlefield</option>
                  <option value="classic">Classic log view</option>
                </select>
                <p id="battle-interface-help">The animated battlefield shows every fighter on the field. The classic view lists them beside the battle log.</p>
              </div>
              <div class="settings-section">
                <label for="battle-pace">Battle pace</label>
                <select id="battle-pace" aria-describedby="battle-pace-help">
                  <option value="fast">Fast-paced</option>
                  <option value="slow">Slower · time to read</option>
                </select>
                <p id="battle-pace-help">Slower battles pause longer between enemy actions and turns.</p>
              </div>
              <div class="settings-section">
                <label class="settings-check" for="combat-animations"><input id="combat-animations" type="checkbox" checked aria-describedby="combat-animations-help" />Combat animations</label>
                <p id="combat-animations-help">Turn off spell cinematics, boss transitions and battle UI animations. Combat effects still apply.</p>
              </div>
              <p class="settings-current-track" id="settings-current-track"></p>
            </div>
          </section>
        </div>
        """;
}

using Statventure.Game.Data.Effects;

namespace Statventure.Game.Screens.Sheet;

/// <summary>
/// The character's page as markup (GM1-P7: the old page's HTML, <c>old-game/index.html</c> 21–25 and 64–299, parsed by the
/// engine): GM1's frame (the top bar with "☰ Menu", the status lines, ⚙ Settings) and GM2's sheet (Docs/Game/Hero.md, "The
/// sheet with the live figure"). What changed from the old page: the hero's name is the heading with its race and level
/// under it (Q30), the seal button shows the race's emblem picture and is the race's only control (Q27, Q36); the
/// equipment is a paper doll round a framed portrait panel with a dark backdrop holding the hero's 3D view (Q29, Q32;
/// empty until GM2 step 11), its five slots inert until GM3 (Q33, the old page's "future" slot); the Field tools rail is
/// whole, each tool disabled until its milestone (Q34). The Limitless toggle (GM15) and the dialogs (later steps) aren't
/// here.
/// </summary>
static class SheetMarkup
{
    /// <summary>The ids the screen fills and the steps after it use.</summary>
    public const string ViewId = "hero-view";

    /// <summary>The body's pieces before the page: the drawer's toggle and shade (shown with the page) and ⚙ Settings.</summary>
    public const string Page = """
        <button id="field-tools-toggle" class="field-tools-toggle" type="button" aria-controls="field-tools-menu" aria-expanded="false" hidden><span aria-hidden="true">☰</span><span>Field tools</span></button>
        <button id="field-tools-shade" class="field-tools-shade" type="button" aria-label="Close field tools" hidden></button>
        <button id="settings-open-button" class="settings-button" type="button" aria-haspopup="dialog" title="Open settings" hidden>
          <span class="settings-button-icon" aria-hidden="true">⚙</span><span>Settings</span>
        </button>
        <main class="shell game-page" hidden>
          <header class="topbar">
            <button id="combat-view-toggle" class="spellbook-button combat-view-toggle" type="button" disabled>Return to combat</button>
            <div class="brand">
              <span class="brand-mark" aria-hidden="true"><span>✦</span></span>
              <span>
                <span class="brand-name">Stat Fick</span>
              </span>
            </div>
            <div class="topbar-actions">
              <span class="world-clock" id="sheet-world-clock" aria-label="World time">Day 1 · 08:00</span>
              <span class="autosave"><span class="status-dot" aria-hidden="true"></span>Autosaved locally</span>
              <button class="backup-button menu-button" id="menu-button" type="button" aria-haspopup="dialog"><span aria-hidden="true">☰</span> Menu</button>
            </div>
          </header>
          <p class="backup-status topbar-file-status" id="backup-status" role="status" aria-live="polite"></p>
          <p class="day-status" id="day-status" role="status" hidden></p>
              <section class="hero" aria-labelledby="page-title">
            <div class="hero-title">
              <p class="eyebrow">RPG / PERSONAL RECORD</p>
              <h1 id="page-title" class="hero-name"></h1>
              <p class="hero-standing" id="hero-standing"></p>
            </div>
            <div class="hero-race">
              <button class="hero-sigil" id="race-select-button" type="button">
                <span class="hero-sigil-fallback" aria-hidden="true">✦</span>
                <img id="race-icon" alt="" hidden>
              </button>
              <span class="hero-race-name" id="hero-race-name"></span>
            </div>
          </section>

          <section class="exp-card" aria-labelledby="exp-heading">
            <div class="exp-header">
              <div>
                <p class="eyebrow">PROGRESSION</p>
                <h2 id="exp-heading">Earn <em>experience</em></h2>
              </div>
              <div class="exp-summary" aria-label="Current experience">
                <strong id="exp-current">0</strong>
                <span id="exp-threshold"></span>
              </div>
            </div>
            <div class="exp-bar-row">
              <div class="exp-track" id="exp-track" role="progressbar" aria-label="Experience toward next level" aria-valuemin="0">
                <span id="exp-bar"></span>
              </div>
              <form class="exp-form" id="exp-form" data-dev-only>
                <label class="sr-only" for="exp-input">Experience to add</label>
                <input class="exp-input" id="exp-input" type="number" inputmode="decimal" min="0" step="any" placeholder="Amount">
                <button class="add-exp-button" id="add-exp-button" type="submit"><span aria-hidden="true">+</span> Add EXP</button>
              </form>
            </div>
            <div class="exp-meta">
              <span id="exp-helper"></span>
              <span>Leveling keeps your progress moving.</span>
            </div>
          </section>

          <section class="resources-section" aria-label="Character resources">
            <div class="resources-heading">
              <p class="eyebrow">VITAL RESOURCES</p>
              <button class="restore-button" id="restore-button" type="button" data-dev-only><span aria-hidden="true">↻</span> Restore both to full</button>
            </div>
            <div class="resources">
            <article class="resource-card level-card">
              <div class="resource-icon" id="level-icon" aria-hidden="true">♜</div>
              <div class="resource-content"><span class="resource-label">Level</span><strong id="level-value">1</strong></div>
              <span class="level-cap">CAP 100</span>
              <button class="level-up-button" id="level-up-button" type="button"><span aria-hidden="true">+</span> Level up</button>
            </article>
            <article class="resource-card hp-card">
              <div class="resource-icon" aria-hidden="true">♡</div>
              <div class="resource-content"><span class="resource-label">Hit points</span><strong class="resource-value"><span class="resource-current" id="hp-current"></span><span class="resource-separator"> / </span><span class="resource-max" id="hp-max"></span></strong></div>
              <div class="resource-actions">
                <div class="resource-controls">
                  <button class="resource-adjust-button" data-resource="hp" data-change="-1" type="button" aria-label="Decrease HP">−</button>
                  <label class="sr-only" for="hp-adjust">HP amount</label>
                  <input class="resource-adjust-input" id="hp-adjust" type="number" inputmode="numeric" min="0" step="1" placeholder="Amount">
                  <button class="resource-adjust-button" data-resource="hp" data-change="1" type="button" aria-label="Increase HP">+</button>
                </div>
                <button class="resource-restore-button" data-restore-resource="hp" type="button" data-dev-only>Full HP</button>
              </div>
              <div class="resource-meter hp-meter" aria-hidden="true"><span id="hp-meter"></span></div>
              <span class="resource-formula">(1½ VIT + ½ STR + ½ MND) × 1.25</span>
            </article>
            <article class="resource-card mana-card">
              <div class="resource-icon" aria-hidden="true">◈</div>
              <div class="resource-content"><span class="resource-label">Mana</span><strong class="resource-value"><span class="resource-current" id="mana-current"></span><span class="resource-separator"> / </span><span class="resource-max" id="mana-max"></span></strong></div>
              <div class="resource-actions">
                <div class="resource-controls">
                  <button class="resource-adjust-button" data-resource="mana" data-change="-1" type="button" aria-label="Decrease Mana">−</button>
                  <label class="sr-only" for="mana-adjust">Mana amount</label>
                  <input class="resource-adjust-input" id="mana-adjust" type="number" inputmode="numeric" min="0" step="1" placeholder="Amount">
                  <button class="resource-adjust-button" data-resource="mana" data-change="1" type="button" aria-label="Increase Mana">+</button>
                </div>
                <button class="resource-restore-button" data-restore-resource="mana" type="button" data-dev-only>Full Mana</button>
              </div>
              <div class="resource-meter mana-meter" aria-hidden="true"><span id="mana-meter"></span></div>
              <span class="resource-formula">MND + ½ INT + ½ WIS</span>
            </article>
            </div>
          </section>

          <section class="defense-section" aria-labelledby="defense-heading">
            <div class="defense-heading">
              <div>
                <p class="eyebrow">DEFENSIVE MEASURES</p>
                <h2 id="defense-heading">Guard and <em>grace</em></h2>
              </div>
              <p class="defense-helper">Passive values shaped by your<br>character’s core attributes.</p>
            </div>
            <div class="defense-grid">
              <article class="defense-card armor-card">
                <div class="defense-icon" aria-hidden="true">▣</div>
                <div class="defense-content">
                  <span class="defense-label">Armor</span>
                  <div class="defense-values"><span><small>Base</small><strong id="armor-base-value"></strong></span><span><small>Modified</small><strong id="armor-modified-value"></strong></span><span><small>Current</small><strong id="armor-value"></strong></span></div>
                  <div class="defense-bonuses stat-bonuses">
                    <div class="stat-modifier-wrap"><label for="armor-permanent-bonus">Permanent bonus</label><div class="defense-modifier-field"><input class="modifier-input stat-modifier-input defense-bonus-input" id="armor-permanent-bonus" data-bonus="permanent" data-defense="armor" type="number" inputmode="decimal" step="any" value="0" aria-label="Armor permanent percentage bonus"><span>%</span></div></div>
                  </div>
                </div>
                <span class="defense-formula">BASE 5 + EQUIPMENT</span>
              </article>
              <article class="defense-card evasion-card">
                <div class="defense-icon" aria-hidden="true">◌</div>
                <div class="defense-content">
                  <span class="defense-label">Evasion</span>
                  <div class="defense-values"><span><small>Base</small><strong id="evasion-base-value"></strong></span><span><small>Modified</small><strong id="evasion-modified-value"></strong></span><span><small>Current</small><strong id="evasion-value"></strong></span></div>
                  <div class="defense-bonuses stat-bonuses">
                    <div class="stat-modifier-wrap"><label for="evasion-permanent-bonus">Permanent bonus</label><div class="defense-modifier-field"><input class="modifier-input stat-modifier-input defense-bonus-input" id="evasion-permanent-bonus" data-bonus="permanent" data-defense="evasion" type="number" inputmode="decimal" step="any" value="0" aria-label="Evasion permanent percentage bonus"><span>%</span></div></div>
                  </div>
                </div>
                <span class="defense-formula">½ × (10 + 1½ DEX + ½ LCK + ½ WIS) × ARMOR TYPE</span>
              </article>
            </div>
          </section>

          <aside class="main-tools" id="field-tools-menu" aria-label="Character tools">
            <div class="main-tools-copy">
              <p class="eyebrow">FIELD TOOLS</p>
              <span>Your world, character &amp; party.</span>
            </div>
            <div class="sidebar-purse" aria-labelledby="sidebar-purse-heading">
              <div class="sidebar-purse-heading">
                <p class="eyebrow" id="sidebar-purse-heading">PURSE</p>
                <span>Carried coin</span>
              </div>
              <div class="sidebar-currencies">
                <label class="sidebar-currency silver-currency" for="currency-silver">
                  <span class="currency-name">Silver</span>
                  <input class="currency-input" id="currency-silver" data-currency="silver" type="number" inputmode="numeric" min="0" step="1" value="0">
                </label>
                <label class="sidebar-currency gold-currency" for="currency-gold">
                  <span class="currency-name">Gold</span>
                  <input class="currency-input" id="currency-gold" data-currency="gold" type="number" inputmode="numeric" min="0" step="1" value="0">
                </label>
                <label class="sidebar-currency platinum-currency" for="currency-platinum">
                  <span class="currency-name">Platinum</span>
                  <input class="currency-input" id="currency-platinum" data-currency="platinum" type="number" inputmode="numeric" min="0" step="1" value="0">
                </label>
              </div>
            </div>
            <div class="companion-sidebar"><p class="eyebrow">COMPANIONS · <span id="companion-count">0 / 3</span></p><div id="companion-sidebar-list"><p class="companion-empty">No companions recruited yet.</p></div></div>
            <nav class="main-tools-actions" aria-label="Field tools">
              <button class="spellbook-button" id="map-open-button" type="button" disabled><span class="tool-icon" aria-hidden="true">⌖</span><span class="tool-copy"><strong>Map</strong><small>Travel, trade &amp; explore</small></span><span class="tool-arrow" aria-hidden="true">›</span></button>
              <button class="spellbook-button" id="combat-open-button" type="button" data-dev-only disabled><span class="tool-icon" aria-hidden="true">⚔</span><span class="tool-copy"><strong>Combat</strong><small>Test a custom encounter</small></span><span class="tool-arrow" aria-hidden="true">›</span></button>
              <button class="spellbook-button" id="spellbook-open-button" type="button" title="Open Spell book" disabled><span class="tool-icon" aria-hidden="true">✧</span><span class="tool-copy"><strong>Spell book</strong><small>Known spells &amp; casting</small></span><span class="tool-arrow" aria-hidden="true">›</span></button>
              <button class="spellbook-button" id="inventory-open-button" type="button" title="Open Inventory" disabled><span class="tool-icon" aria-hidden="true">◫</span><span class="tool-copy"><strong>Inventory</strong><small>Equipment &amp; supplies</small></span><span class="tool-arrow" aria-hidden="true">›</span></button>
              <button class="spellbook-button" id="companions-open-button" type="button" title="Manage Companions" disabled><span class="tool-icon" aria-hidden="true">♟</span><span class="tool-copy"><strong>Companions</strong><small>Party sheets &amp; equipment</small></span><span class="tool-arrow" aria-hidden="true">›</span></button>
              <button class="spellbook-button" id="classes-open-button" type="button" title="Open Classes" disabled><span class="tool-icon" aria-hidden="true">♜</span><span class="tool-copy"><strong>Classes</strong><small>Training &amp; class features</small></span><span class="tool-arrow" aria-hidden="true">›</span></button>
              <button class="spellbook-button" id="skills-open-button" type="button" title="Open Skills" disabled><span class="tool-icon" aria-hidden="true">✦</span><span class="tool-copy"><strong>Skills</strong><small>Activated abilities</small></span><span class="tool-arrow" aria-hidden="true">›</span></button>
              <button class="spellbook-button" id="passives-open-button" type="button" title="Open Passives" disabled><span class="tool-icon" aria-hidden="true">◇</span><span class="tool-copy"><strong>Passives</strong><small>Always-on effects</small></span><span class="tool-arrow" aria-hidden="true">›</span></button>
            </nav>
          </aside>

          <section class="equipment-section" aria-labelledby="equipment-heading">
            <div class="equipment-heading">
              <div>
                <p class="eyebrow">EQUIPMENT</p>
                <h2 id="equipment-heading">Worn and <em>carried</em></h2>
              </div>
              <p class="equipment-helper">Place weapons, armor, and<br>artifacts here.</p>
            </div>
            <div class="paper-doll">
              <article class="equipment-slot equipment-slot-future paper-doll-hand" data-equipment-slot="primaryHand">
                <span class="equipment-slot-label">Primary hand</span>
                <div class="equipment-slot-content" id="equipment-primary-hand"></div>
              </article>
              <figure class="portrait-panel" id="portrait-panel">
                <surface class="portrait-view" id="hero-view"></surface>
              </figure>
              <article class="equipment-slot equipment-slot-future paper-doll-hand" data-equipment-slot="offHand">
                <span class="equipment-slot-label">Off-hand</span>
                <div class="equipment-slot-content" id="equipment-off-hand"></div>
              </article>
              <article class="equipment-slot equipment-slot-future" data-equipment-slot="armor">
                <span class="equipment-slot-label">Armor</span>
                <div class="equipment-slot-content" id="equipment-armor"></div>
              </article>
              <article class="equipment-slot equipment-slot-future" data-equipment-slot="artifact1">
                <span class="equipment-slot-label">Artifact I</span>
                <div class="equipment-slot-content" id="equipment-artifact-1"></div>
              </article>
              <article class="equipment-slot equipment-slot-future" data-equipment-slot="artifact2">
                <span class="equipment-slot-label">Artifact II</span>
                <div class="equipment-slot-content" id="equipment-artifact-2"></div>
              </article>
            </div>
          </section>

          <section class="stats-panel" aria-labelledby="stats-heading">
            <div class="section-heading">
              <div>
                <p class="eyebrow">CORE ATTRIBUTES</p>
                <h2 id="stats-heading">The eight <em>facets</em></h2>
              </div>
              <div class="section-tools">
                <p class="helper-text">Adjust base values and bonuses.<br><span>Permanent bonuses require Dev-mode.</span></p>
                <div class="points-badge" aria-live="polite" aria-label="Unspent stat points">
                  <strong id="points-value">0</strong>
                  <span><span class="points-label">Unspent</span><span>stat points</span></span>
                </div>
                <button class="respec-button" id="respec-button" type="button" disabled><span aria-hidden="true">↺</span> Respec</button>
              </div>
            </div>
            <div class="stats-grid" id="stats-grid">STAT-CARDS</div>
          </section>

          <footer class="footer">
            <span>STAT FICK / FIELD NOTES</span>
            <span>Every legend begins with a single point.</span>
          </footer>
        </main>
        """;

    /// <summary>The eight attributes as the old page lists them (<c>statDefinitions</c>, <c>app.js</c> 285–294): key, label, short name, colour.</summary>
    public static readonly IReadOnlyList<(string Key, string Label, string Short, string Colour)> Attributes =
    [
        (AttributeKeys.Strength, "Strength", "STR", "#c7683f"),
        (AttributeKeys.Vitality, "Vitality", "VIT", "#bb7b4b"),
        (AttributeKeys.Mind, "Mind", "MND", "#8c6f83"),
        (AttributeKeys.Dexterity, "Dexterity", "DEX", "#738f88"),
        (AttributeKeys.Intelligence, "Intelligence", "INT", "#68849a"),
        (AttributeKeys.Wisdom, "Wisdom", "WIS", "#858b6c"),
        (AttributeKeys.Speed, "Speed", "SPD", "#9c7c58"),
        (AttributeKeys.Luck, "Luck", "LCK", "#ae6f73"),
    ];

    /// <summary>An attribute's card (the old <c>createStatCards</c>, <c>app.js</c> 1640–1672), its numbers left for the screen to fill.</summary>
    public static string StatCard(int index, string key, string label, string @short, string colour) => $"""
        <article class="stat-card" style="--stat-color: {colour}">
          <div class="stat-topline">
            <label class="stat-name" for="stat-{key}">{label}</label>
            <span class="stat-index">0{index + 1}</span>
          </div>
          <div class="stat-value-labels"><span>Base</span><span>Current</span></div>
          <div class="stat-value-row">
            <div class="stat-value-column stat-base-column">
              <div class="stat-input-wrap">
                <button class="stat-stepper" data-action="decrease" data-stat="{key}" type="button" aria-label="Decrease base {label}">−</button>
                <input class="stat-input" id="stat-{key}" data-stat="{key}" type="number" inputmode="numeric" step="1" aria-label="Base {label} value">
                <button class="stat-stepper" data-action="increase" data-stat="{key}" type="button" aria-label="Increase base {label}">+</button>
                <span class="stat-unit">{@short}</span>
              </div>
            </div>
            <div class="stat-value-column stat-current-column">
              <strong class="stat-current-value" id="stat-current-{key}"></strong>
              <span class="stat-unit">{@short}</span>
            </div>
          </div>
          <div class="stat-bonuses">
            <div class="stat-modifier-wrap">
              <label for="stat-permanent-bonus-{key}">Permanent bonus</label>
              <div class="stat-modifier-field">
                <input class="modifier-input stat-modifier-input stat-bonus-input" id="stat-permanent-bonus-{key}" data-bonus="permanent" data-stat="{key}" type="number" inputmode="decimal" step="any" aria-label="{label} permanent percentage bonus">
                <span>%</span>
              </div>
            </div>
          </div>
          <div class="stat-bar" aria-hidden="true"><span></span></div>
        </article>
        """;

    /// <summary>The page's markup with its eight attribute cards.</summary>
    public static string Markup { get; } = Page.Replace("STAT-CARDS",
        string.Concat(Attributes.Select((a, i) => StatCard(i, a.Key, a.Label, a.Short, a.Colour))));
}

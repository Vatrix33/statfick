# Stat Fick (the old game), system by system

A map of `old-game/` for planning the port onto Statventure. Surveyed read-only on 2026-11-12 from `main` at
`29d110d`. Counts were taken from the files themselves (scripts and JSON); where a fact comes only from
`old-game/Docs/Notes.md` it says "(Notes)". Paths are relative to `old-game/` unless they start with `engine/`.
Line counts are `wc -l`; byte sizes are given too, because several files have very long lines.

---

## 1. Overview

### 1.1 At a glance

- A single-page JavaScript RPG: a character sheet that is also the hub, a continent map with real-time travel,
  turn-based battles on an animated SVG battlefield, towns with shops and a guild, up to three companions, twelve
  bosses with mechanics and intro cutscenes, live-synthesized music. No sound effects (music only).
- **Code:** 48 scripts in `scripts/`, **39,571 lines, 3.2 MB**. `combat.js` (7,041 lines, 549 KB) and `app.js`
  (5,956 lines, 355 KB) are a third of the bytes. `continent-shape.js` is 11 lines but 255 KB of baked data.
  `index.html` 1,004 lines (82 KB); `styles.css` 4,184 lines (423 KB; 245 `@keyframes`, 62 `@media`, one
  `@property`).
- **Data files:** `classes/` 130 JSON (624 KB) + `classes/newc/` 16 files of text drafts (not loaded); `enemies/` 174 JSON +
  `index.json` (720 KB); `objects/` `spells.json` (388 spells, 279 KB), `artifacts.json` (85), `consumables.json` (71),
  `key-items.json` (5), `races.json` (28 races), 9 race seal SVGs, `stat-fick-icon.svg`, `fonts/` (Lato Regular,
  Semibold, Bold; STIX Two Text Regular, Italic; two licences); `music/` 43 MP3s (61 MB, offline exports the game never
  loads). Much more data lives inside scripts (forge tables, encounter tables, region geometry, the continent's land,
  alchemy content, music scores, sprite looks, cutscene scenes, interior scenes).
- No framework, no build step, no ES modules, no npm packages in the page. Electron only for the desktop shell and the
  test client.

### 1.2 How it starts

- **Desktop shell** (`desktop/`, Electron 44.5.0, electron-builder 26.15.3): `main.cjs` (94 lines) registers a
  `statfick://game/` protocol that serves the files next to the executable live (`assets.cjs`, 85 lines; no copy of the
  game inside the executable), refuses to start when `index.html`, `styles.css`, `scripts/app.js`,
  `objects/artifacts.json`, `classes` or `enemies` is missing, blocks every non-local request, opens one window
  (1440×950, minimum 820×600, single instance). Its menu: File › Save Character (Ctrl+S, calls the page's
  `exportBackup()`), Load Character (Ctrl+O), Open game folder, Quit; View › reload, zoom, fullscreen; Help › About. On
  close it pauses map travel, calls `saveCharacter()` and flushes storage. `preload.cjs` (6 lines) exposes
  `window.statFickDesktop.saveCharacter(json)` / `loadCharacter()` (native dialogs, 10 MB limit). Builds
  `Stat Fick.AppImage` and `Stat Fick.exe` (portable) sit in `old-game/` but are not in git.
- **`index.html`:** an inline script adds `styles.css?v=<Date.now()>` (cache busting); the body holds all static
  markup (the sheet, every modal, the combat view, the map, shops, guild); a closing inline script loads the 48
  scripts **one after another, each awaited, in a fixed order**: settings → world-region-data → continent-shape →
  continent-engine → guild-information-engine → world-engine → encounter-engine → guild-quest-engine → shop-engine →
  alchemy-data → alchemy-engine → forge-engine → combat-engine → combat-ai → limitless-engine → **app** →
  tier10-cinematics → sprite-kit → sprite-equipment → sprite-humanoid → sprite-creatures → sprite-catalog → forge-art →
  boss-intros → battle-scenery → battle-effects → battle-stage → **combat** → map-ink → map-terrain → map-regions →
  map-continent → **map** → church-of-talos → hearth-home → scene-banners → shop → reforge → magic-tower → apothecary
  → guild → companion-sheet-details → companions → limitless → interface → music-composer → music-themes → music.

### 1.3 How the code is organised

- **One global scope.** `app.js` declares the player's whole state as top-level `let` variables (65 in all, about 45
  of them the character's fields:
  `level`, `exp`, `stats`, `availablePoints`, `perkPoints`, `inventory`, `equipment`, `favoriteItems`, `knownSpells`,
  `classLevels`, `activeSkills`, `currencies`, `currentHp`, `currentMana`, `companions`, `recruitOffers`,
  `worldState`, `shopState`, `limitless`, `alchemy`, many counters for class requirements…) and 265 top-level
  functions. Later scripts read and assign those globals directly.
- **Pure rules modules** ("engines"), written `(function (root) { … })(globalThis)` with `module.exports` so they run
  in Node too: `CombatRules` (combat-engine), `CombatAI`, `WorldRules`, `WorldRegionData`, `ContinentRules`,
  `ContinentShape`, `EncounterRules`, `GuildQuestRules`, `GuildInformationRules`, `ShopRules`, `ForgeRules`,
  `LimitlessRules`; `AlchemyRules` and `AlchemyData` are pure but window-only.
- **Page modules**, IIFEs publishing `window.X`: `Combat`, `BattleStage`, `BattleFx`, `BattleScenery`, `BossIntros`,
  `TierTenCinematics`, `Sprites`, `SpriteKit`, `ForgeArt`, `WorldMap`, `MapTerrain`, `MapRegions`, `MapContinent`,
  `MapInk`, `Shop`, `ShopLayout`, `Reforge`, `MagicTower`, `Apothecary`, `Alchemy`, `ChurchOfTalos`, `HearthHome`,
  `SceneBanners`, `Guild`, `Companions`, `CompanionSheetDetails`, `Limitless`, `GameSettings`, `Music`,
  `MusicComposer`, `MusicThemes`. They call each other through optional chaining (`window.Combat?.active()`), so a
  missing module degrades silently.
- **The central trick, `withActor`** (`combat.js` line 143): to compute anything for a companion or an enemy,
  combat temporarily overwrites about 30 of app.js's player globals with that actor's character record, runs the
  sheet's own functions (`calculateClassEffects`, `currentStats`, spell costs, race effects…), then restores them.
  Every combatant, enemies included, is a full character record evaluated by the character sheet's code.
  `withCalculationFrame` caches results inside one synchronous calculation so the AI's thousands of forecasts stay
  affordable.
- **Effects are data interpreted by code.** Class levels, spells, consumables, races, artifacts and enemy files carry
  effect objects (`{ type, amount, … }`; artifacts use `kind`). The data use about **320 distinct `type` strings**
  (classes 260, spells 38, consumables 37, races 22, enemies 16) plus about 40 artifact kinds. `app.js` checks 216
  distinct `type === "…"` values (`addClassEffect`, lines 686–937, folds class effects into one accumulator) and
  `combat.js` 176.

### 1.4 State and save format

- **One character, one save slot.** `saveCharacter()` (93 call sites) writes the whole state as one JSON string to
  `localStorage["emberfall-character-stats"]`: `worldState`, `shopState`, `combatState` (a deep copy of the running
  battle, actors' full records included), race, appearance, level, EXP, points, `limitless`, `alchemy`, about 15
  class-requirement counters (`trapsUsed`, `unarmedAttacks`, `itemsSold`, `classKills`, `raceKills`, `classProgress`,
  `flawlessVictories`…), `editingEnabled` (Dev Mode), `darkMode`, HP, Mana, allocations, currencies, bonuses,
  inventory, favourites, equipment, known spells, class levels, skill states, companions (each a full character
  record), recruit offers, pending class spell choice, stats.
- **Backups:** `{ format: "emberfall-stat-block", version: 22, exportedAt, character }` as
  `stat-fick-character.json`, through the desktop bridge's native dialogs or a browser download / file input. A saved
  character file can also be imported as a companion.
- **Loading = migration.** `normalizeCharacter` (app.js 1399–1564) and `WorldRules.normalize` (world-engine.js 64–351)
  accept any older save and repair it: `ForgeRules.migrateLegacyItems` (deleted catalogue items → forged pieces,
  deterministic per character), `LEGACY_ITEMS` (24 placeholder groups), `RETIRED_ARTIFACT_IDS` (32), `RENAMED_ENEMIES`,
  retired quest kinds, `mapScale` (×3 distances), `continentVersion` (task 8), Ornn's retired Great Hunt, a save
  standing in water moved to land, the Limitless and alchemy records.
- **Other storage:** settings `stat-fick-game-settings`, volume `stat-fick-sound-volume` (localStorage); cached copies
  of `spells.json`, every class file, `races.json`, `artifacts.json` in localStorage (fallback when a fetch fails);
  file handles chosen with the File System Access API kept in IndexedDB (`emberfall-spell-file`,
  `emberfall-item-json-files`) for the dialogs' "Refresh file" / "Choose …json" buttons.
- **A battle in progress is saved and resumes on load** (`Combat.restore(character.combatState)`); a boss intro never
  replays on restore.

### 1.5 Screens

One page. The character sheet is always mounted; everything else is a `.modal-layer` toggled with `hidden`, or the
full-screen combat view laid over the sheet (which stays mounted). Cutscenes are overlays above everything. Escape
closes the topmost layer. Four layers are created by scripts rather than `index.html`: the Church of Talos window,
Hearth Home, the Limitless notice and the companion item picker. Section 3 lists every screen.

### 1.6 Rendering

- **UI:** DOM + CSS, markup built as HTML strings (`innerHTML`, 117 uses). The look is the "Gothic ink theme" block
  at the end of `styles.css` (line 3507 on), overriding the older "Adventurer's ledger" tokens: parchment light mode,
  stone dark mode (`html.dark-mode`, the user plays dark; Notes), brass-edged buttons, rarity colours
  (`--rarity`, `--rarity-ink`), the animated Exotic wave (`@property --exotic-tone`). Fonts Lato and STIX Two Text,
  plus system fonts for symbols (⚙ ✦ ⌖ ⚔ ♟ ☰ …).
- **SVG generated in code** for battle sprites, item portraits and icons, interiors, battle backgrounds, effects,
  cutscene scenes, Tier 10 vignettes, the map's overlay (roads, towns, castles, labels, markers, lanterns, weather).
- **Canvas 2D only for the map** (terrain tiles, continent backdrop, region overviews, fog, parchment):
  `map-terrain.js`, `map-continent.js`, `map-ink.js`, `map.js`. No WebGL, no Web Workers; heavy painting is sliced over
  frames (`requestAnimationFrame`, "Charting the region…").
- **Animation:** CSS keyframes (idle life, attack poses, cutscene timelines, scenery motion) and the Web Animations API
  (`element.animate` in `battle-stage.js` and `battle-effects.js`). `prefers-reduced-motion` and the settings shorten
  or skip them. Browser features relied on: `filter: drop-shadow/blur`, `mix-blend-mode`, animated `clip-path`, SVG
  masks and clip paths, `inert`, `matchMedia("(max-width: 840px)")`, `structuredClone`, `crypto.randomUUID`.

### 1.7 Sprites, maps and music in brief

- **Sprites** are functions that return SVG strings: one parameterised humanoid rig and five creature rigs
  (quadruped, griffon, dragon, wisp, core), facing right with the feet at the origin, every limb in its own group so
  CSS can animate it; an "ink" style (muted palette via `K.tone`, solid shadows clipped per shape, hatching, a contour
  drop-shadow from CSS). A catalogue gives each of the 9 playable races 8 looks (72) and each of the 174 enemies its own
  entry; equipment is drawn on the rig from the item (forged weapons from their parts). Section 2.27.
- **The map** is one continent of 20 regions. Each region has a "field function" (`map-regions.js`) giving colour,
  height and cover anywhere; `map-terrain.js` turns it into a field grid, procedural features and canvas tiles at
  1/2/4/8 px per unit with hillshade, hatching and contours; `map-continent.js` paints the backdrop, overviews and fog
  from the baked land field (`continent-shape.js`); an SVG overlay whose `viewBox` is the camera carries towns, roads,
  markers and labels. Section 2.21.
- **Music** is synthesized live with Web Audio: `music-themes.js` holds 55 themes in a compact score notation (scale
  degrees, chord numerals, step patterns), `music-composer.js` performs them with modelled instruments (bowed strings,
  brass, formant choirs, Karplus-Strong plucks, mallets, bells, percussion) and a convolution room per theme; `music.js`
  picks the theme (region, battle arrangement, boss and phase), crossfades region themes and owns the volume.
  Section 2.26.

### 1.8 Time and randomness

- **The world clock runs in real time** while the page is open (`map.js` `advanceAmbientTime`, every 250 ms): idle
  0.5 game minutes per real second, travelling 36 per second at 1× (map speed buttons 0.5×, 1×, 1.5×, 2×). Battles
  stop it. `advanceWorldTime` (app.js 5541) regenerates 5% of maximum HP and Mana per game hour (player and
  companions), resets daily skills at a new day, and lets shops restock (6-hour windows). Day/dusk/night lighting
  (`WorldRules.lighting`) drives the map's darkness, lanterns and the interiors' windows.
- **Gameplay randomness is `Math.random`** (combat rolls, loot, encounters, stock, recruits, boards); the old checks
  control dice by stubbing it. Art randomness is seeded (interiors per settlement, map noise, `stableLookVariant`);
  music uses its own seeded mulberry32; legacy-item migration uses `seededRandom` per character.

### 1.9 Tests (the reference harness)

- `test-client/` (headless Electron, offscreen and muted): `headless-main.cjs` (209 lines), `smoke.cjs` (80), suites
  `smoke | combat | enemies | lifecycle | all`, and `--script=<file> --call=<function>` for one browser check; run with
  `engine/Tools/Scripts/chromium.sh old-game/test-client …`. `test-client/tests/`: 49 `*-browser.js` check files,
  `combat-performance.cjs` (the combat and all-enemies suites, 530 cases), `combat-lifecycle-performance.cjs` + fixture
  (10,553 lines in all). Port tasks 1–9 and 11 each have a check file with one entry per rule, shown able to fail by
  mutation. Battle checks load a second copy of `combat.js` with a hook (`window.__shopReworkChecks`, …).
  `statFickDesktop.captureTestScreen(name, w, h)` writes screenshots. Baseline: 11 known failing or stale entries
  (Notes, "Regression"), among them the combat suite stopping at the Barbarian legacy bonus-choice check.
- `old-game/tests/` holds the older packed test client (`Stat Fick Tests.AppImage`, 120 MB) and
  `Regional Map Layout.png`.

### 1.10 Already in the engine (context only)

Not part of the old game, but it changes what "porting" means for some systems (from `engine/Docs/`): the soundtrack
is already ported (M8: the composer, 55 themes, 34 instruments, 21 percussion voices, 7 rooms, read from
`audio.pak`; music.js's old score engine was dropped; `engine/Docs/Audio.md`); the engine's UI (M4–M5) is tested
against 22 captured screens of the old game (11 states at 1× and 1.65×) and a copy of its stylesheet from the repo's
first commit; Ji'ruan's intro exists as a 3D proof cutscene (M10, `Tests/Statventure.Tests/Scene/JiruanIntro.cs`);
the game data library `Statventure.Game.Data` exists as a sample with a stand-in item; the editor (M11) and packaging
(M12) are done.

---

## 2. Systems

Each section: what it does for the player; its files (lines / KB); its data; what it needs and what needs it; what
matters for a port. "Needs" lists the systems that must exist first; "used by" the ones that call it.

### 2.1 Character sheet core (attributes, levels, EXP, resources, defences, money, Dev Mode)

- **For the player:** the main screen. Eight attributes (Strength, Vitality, Mind, Dexterity, Intelligence, Wisdom,
  Speed, Luck; 10 each at the start) raised with 5 free points per level; a level-up button that becomes available
  once EXP reaches the target (each level also gives +1 to every attribute and 1 class point up to Level 100); HP,
  Mana, Armor and Evasion with their formulas shown; a purse in silver, gold and platinum (100 silver = 1 gold,
  10,000 = 1 platinum; a new hero has 5 gold); five equipment slots; a respec of free points; a world clock; a theme
  toggle (light/dark); Save/Load/New Character buttons.
- **Formulas** (app.js 428–447, combat-engine.js): max HP = ⌊(1.5 VIT + STR/2 + MND/2) × 1.25⌋; max Mana = MND + INT/2 +
  WIS/2; Evasion = (10 + 1.5 DEX + 0.5 LCK + 0.5 WIS) / 2; base Armor 5; EXP to level = an enemy of that level's EXP ×
  `sameLevelVictories` (3–6 to Level 12, then rising to 24 at 99; 1,265 same-level victories to reach 100); enemy EXP =
  ⌈(level + 1) × 100 / share⌉ (share 5, 6, then 8–12). Level cap 100 (200 with Limitless).
- **Dev Mode** (`editingEnabled`, the ✦ next to "Stat Fick"; **on by default for a new character**, off for
  recruits): unlocks editing the attributes, permanent bonuses, Armor/Evasion bonuses and the purse, the EXP form,
  "Restore to full", adding spells and items from the catalogues, the Combat test screen (pick any enemy), levelling
  without EXP and companion dev tools. Without it, money and stats change only through play.
- **Files:** `app.js` lines 1–427 (constants, the 130 class file paths, DOM references), 428–1326 (derived values:
  artifact, class and race effect accumulators, equipment defence), 1327–2217 (the record, `normalizeCharacter`, save,
  backup, apply, level-up, reset, sheet rendering). The rest of app.js belongs to 2.2–2.5. `interface.js` (66) is the
  narrow-window drawer for the Field tools rail.
- **Data:** none of its own (formulas are code).
- **Needs:** races, classes (their effects change every number), items (equipment, artifacts), Limitless rules
  (cap), world rules (the record holds `worldState`). **Used by:** everything; combat evaluates every actor through
  these functions (`withActor`).
- **Port notes:** the player's state is ~45 globals, not an object; `normalizeCharacter` is the one place every
  migration happens; companions and enemies use the same record shape (`format`, `version`, `stats`, `classLevels`,
  `equipment`, `knownSpells`, …), so the record is effectively the game's actor schema. Numbers are JS doubles with
  `Math.floor` at specific points.

### 2.2 Races and appearance

- **For the player:** a new hero picks a race (a seal button on the sheet; 9 playable: Human, Elf, Dark Elf, Dwarf,
  Hobbit, Lizardman, Half-Beast, Goliath, Merfolk), then a gender and one of four looks for it (8 looks per race,
  previewed as battle sprites). Each race has attribute modifiers (−3…+3) and a passive (Human +2 max HP every fifth
  level; Elf/Dark Elf Mana and −3% spell cost; Half-Beast's claws as unarmed attacks; Goliath +10% HP, +5% Armor, −10%
  Evasion…). Races also restrict equipment.
- **Files:** `app.js` 2926–3221 (`normalizeRaceDefinition`, `renderRaces`, `raceDisallowsItem`, the appearance step,
  `raceAccent`). Looks: `sprite-catalog.js` `RACES` (2.27).
- **Data:** `objects/races.json`, 28 races (9 playable, 19 enemy-only: goblin, impid, wolf, bear, griffon, orc, demon,
  devil, elemental, giant, dragon, troll, siren, harpy, satyr, lamia, undead, treant, dryad); each `{ id, name,
  playable, icon, statModifiers, passive }`; 22 effect types. `objects/race-*.svg`, 9 seals (Half-Beast, Goliath and
  Merfolk are placeholders, task 11). Race passives understand only the types `calculateRaceEffectsUncached` lists
  (app.js 1101).
- **Needs:** sprites (looks), items (restrictions). **Used by:** combat (race passives such as Undead's Unliving, death
  saves, Dragon +2 all stats per level), class requirements (race classes), recruits, enemy files (`raceId`).
- **Port notes:** enemy races carry rules too (e.g. Dragon's per-level stat growth made boss tuning hard; Notes).

### 2.3 Classes, skills and passives

- **For the player:** class points (1 per level) buy levels in classes whose requirements are met (character level,
  other classes' levels, and an "other" condition such as known spells, kills of a race, distance travelled, items
  sold, traps used, flawless victories, potions brewed…). Each class level grants permanent effects, a passive or an
  active skill. Skills are used from the Skills window or in battle, limited per combat or per day (cooldowns reset at
  a new game day). Some classes make the hero choose spells (the Mage's "choose 2 of 5 Tier 1 spells") or a bound
  familiar. The Classes window has a "class information" tab for classes whose requirements were bought from the
  guild's hooded man. Respecs at the Church of Talos.
- **Files:** `app.js` 2813–2925 (loading), 4738–5516 (requirements, `otherClassRequirementMet` with 41 kinds,
  descriptions, the class spell choice, `applyClassLevelRewards`, `changeClassLevel`), 5517–5750 (daily abilities,
  world time, skills and passives windows), 562–1057 (`addClassEffect`, `calculateClassEffects`). In battle:
  `combat.js` `useSkill` (4572–4805), passives and triggers throughout.
- **Data:** `classes/*.json`, **130 files** (`CLASS_FILES` lists them in app.js 17–148), each `{ id, name,
  requirements { level, classes, other }, maxLevel, levels[{ level, description, effects[] }] }`; **738 class levels**,
  **142 skills** (cooldown kind: combat 82, daily 54, none 4, item 2; up to 40 optional fields each), **127 passives**,
  **260 distinct effect types**; max levels 1–10 (5: 34 classes, 7: 31, 3: 18). No class rarity in the old game.
  Monster-only classes are marked (`unobtainable`, `race-playability`). The files are hand-formatted in several styles
  (Notes: edit with a span-preserving script, never re-dump). `classes/newc/` (14 `.txt` class drafts, an `IGNORE`
  file and a template) is not loaded.
- **Needs:** sheet core, spells (spell choices, spell requirements), items, races, world counters (distance, guild
  quests), alchemy counters. **Used by:** combat (most mechanics are class effects), map (travel speed, encounter
  chance, Stalker's Track Quarry), shops (Salesman prices), guild (Bounty Broker, Courier), enemies (class levels on
  every enemy file), Limitless (`BOSS_CLASSES`).
- **Port notes:** task 10 (with tasks 11–14's additions) designed a replacement set of 256 classes for the port to
  build (`engine/Docs/Port-Tasks/10_Class_Rework/`); the old classes stay unchanged in the old game, and whether the
  port first carries the old 130 over depends on the open question in 6.1. Every enemy file uses the old classes
  until the enemy revamp. The interpreter is spread over app.js and combat.js, not table-driven.

### 2.4 Spells and the spell book

- **For the player:** a spell book of known spells (filter Damage/Support), cast in battle (3 AP unless the spell or
  a class says otherwise) or, for healing and similar, from the sheet outside battle. Spells are bought at Magic
  Towers (castle cities), the Institute of Magic and the Storm Reliquary, offered by class level-ups, or taught by
  items (an Archivist's archived spells). A spell's tier needs character level (tier − 1) × 10, with a 10% chance of one
  spell up to 10 levels early in a tower's stock. Tier 10 spells play a short vignette (2.14).
- **Formulas:** power = base + primary stat × ratio + secondary stat × ratio, then flat and percent modifiers from
  classes, artifacts, the weapon (staffs and wands) and allies (`calculateSpellValue`, app.js 2323); Mana cost and AP
  from `calculateSpellManaCost` / `calculateSpellActionPoints`; spells ignore Armor.
- **Files:** `app.js` 2218–2812 (parsing, values, costs, labels, the spell book UI, loading); `combat.js` `cast`
  (4262–4413), `applyEffects` (4069–4256), `summonFriendly` (2896–2956), weather control.
- **Data:** `objects/spells.json`, **388 spells** `{ id, name, element, tier, type, manaCost, priceSilver, basePower,
  primary, secondary, description, effects[] }`; 12 elements (Basic 34; Fire, Water, Earth, Wind, Holy, Blood, Death,
  Lightning, Dark, Nature 31–33 each; Ice 31); tiers 1–10 (49/50/51/50/55/37/36/24/24/12); Damage 257, Support 131; 29
  summon spells (a summon is an enemy definition at its own level); 38 effect types. Some spells are restricted to a
  caster (`requiredCasterEnemyId`), some have `seasonVariants` (Juila's Wheel of the Year).
- **Needs:** sheet core, classes, items (caster weapons), enemies (summons). **Used by:** combat, shops (tower,
  Institute, Reliquary), companions, class requirements.
- **Port notes:** task 12 designed the replacement for the port to build (381 reworked + 168 new spells, Spell
  Augmentation, element statuses; `engine/Docs/Port-Tasks/12_Spell_Rework/`); when depends on 6.1. Instant death works on bosses today
  (`instant-death-per-thousand`; the rework makes bosses immune). Several "ignores n% of Armor" texts do nothing
  (Notes).

### 2.5 Items: catalogue, inventory and equipment

- **For the player:** an inventory window with tabs (all, weapons, armor, artifacts, consumables, ingredients, runes),
  favourites, item details (stats, parts, runes, effects, where an ingredient is found), equip/unequip into primary
  hand, off-hand, armor and two artifact slots (two-handed weapons fill both hands; race and Limitless restrictions),
  potion use outside battle. Rarity gem tags and colours everywhere (common, uncommon, rare, epic, legendary,
  masterwork, exotic).
- **Item kinds and where they come from:**
  - Weapons and armor: **all forged**, generated from their id (2.6); `objects/weapons.json` and `armor.json` were
    deleted (Notes), `itemCatalog.weapons/armor` stay empty arrays and their parsers are dead code.
  - Artifacts: `objects/artifacts.json`, 85: 32 uncommon, 24 rare, 16 epic, 8 legendary (one effect each, task 4),
    5 masterwork (Second Wind, Wraithguard, Arrowtip Rain, Titanfall, Treasure Seeker) on a level ramp: copies fixed at
    the level obtained (`<id>-lv<level>`, Levels 1–200, `masterworkArtifactAt`); each record has `look { shape,
    glyph }` and `placeholderArt`.
  - Consumables: `objects/consumables.json`, 71: 50 potions (10 Health/Mana kept from before + 40 alchemy potions),
    12 poisons, 8 traps (Spring, Spell Rune, Sentry, Tripwire, Smoke, Frost Glyph, Weighted Net, Gravity Well),
    Camping Supplies (map action "camp").
  - Key items: `objects/key-items.json`, 5 (Gem of False Dreams, Vial of Dragonblood, Blood Crystal, Shadow Crystal,
    Phylactery Core); used by class requirements and one shop offer (Bloody Mary for 5 Blood Crystals in Ravenwatch).
  - Runes: 24, items `rune-<id>` built by `ForgeRules.buildRuneItem`.
  - Ingredients: 56, from `alchemy-data.js` (2.17).
- **Files:** `app.js` 3222–4737 (parsing, masterwork artifact levels, item power, hit and crit chance, labels
  including `artifactEffectLabel` for ~40 artifact kinds, equip rules, inventory and catalogue UIs, item details,
  forged tooltips, loading with cache and file handles); consumables and key items are loaded by `combat.js`
  `loadConsumables`. `getItem(id)` resolves catalogue ids, forged ids, masterwork templates, levelled artifacts, runes
  and ingredients.
- **Needs:** forge rules, alchemy data, races, Limitless rules. **Used by:** sheet, combat, shops, loot, guild
  (artifact challenges, item information), companions.
- **Port notes:** item ids are strings that encode the item (forged recipes, `-lv<level>` artifacts); an inventory is
  a list of ids with repeats for quantity. Artifact effects are interpreted in two places (sheet: `calculateArtifactEffects`;
  battle: combat.js "The Artifact Rework" block, 553–680).

### 2.6 Forge: forged weapons and armor, casters, runes, masterworks, Exotics, natural weapons; item art

- **For the player:** every weapon and armor piece is made of parts (main + two secondary) by makers from materials,
  at a level; makers and materials change power, scaling, hit, crit, pierce, Mana cost and price; rarity follows from
  the parts; weapons roll up to 3 rune slots by level (0 below 40, 3 from 85; the Exotic Cherubim always 4), armor up
  to 2, filled with elemental or stat runes fitted at a Magic Tower. Staffs
  and wands are casters (a jewel or gem of an element and tier, a handle, an inscription) that change spell cost and
  power by category and element. Masterworks are named pieces with fixed main makers, Mythilium parts and unique
  effects; Exotics (Limitless) use Exium. A matching bonus applies when all parts come from one maker.
- **Files:** `forge-engine.js` (1,884 / 155 KB; pure, Node-loadable, `ForgeRules`): 8 weapon makers (Roundtable,
  Dwarven, Elven, Boreal Bolt, Abyss, Hellmade, Hearthforge, Outlaw), caster makers, about 39 weapon material rows,
  caster handle/stone/ink tables (27 rows), 21 weapon types + staff + wand (`WEAPONS`), crit table, 24 runes, 12 armor
  types (3 heavy, 3 light, 3 clothing, 3 shields), 11 armor makers (adding Institute of Magic, Church of Talos,
  Vampiric), armor materials, **37 `MASTERWORKS` entries (32 masterworks + 5 Exotics)**, 4 natural weapons (Beast
  Claws, Sunhorn Antlers, Vulcanic Ram Horns, Stardragon Claws), tier odds by level, `MAX_LEVEL` 200, price rules,
  names and descriptions, encode/decode for every id format, enemy placeholders (`forge-weapon-<1|2>-<type>…`,
  `forge-armor-<type>`, `~rarity`, `@element`), `armCharacter` (forges an enemy's real gear at spawn),
  `LEGACY_ITEMS`/`migrateLegacyItems`, the reforge rules (2.7). `forge-art.js` (1,046 / 114 KB, `ForgeArt`): every
  part drawn per maker style and material palette for portraits, shop cards and battle sprites; maker logos;
  `MASTERWORK_LAYOUT`; armor portraits; rune, potion, ingredient, trap, camp and trinket icons; artifact looks
  (`ARTIFACT_SHAPES` 49 objects, per-rarity styles, four masterwork artifact drawings).
- **Id formats:** `weapon-forged-<type>-<level>-<main>-<guard>-<third>-s<slots><runes>`,
  `weapon-forged-staff-<lvl>-<mk><stone><tier>-<mk><handle>-<mk><ink>`, `armor-forged-<type>-<lvl>-…-s<0-2><runes>`,
  `weapon-masterwork-<key>-…`, `armor-masterwork-<key>-…`; slotting a rune changes the id (code replaces the
  inventory entry).
- **Data:** all inside `forge-engine.js`; masterwork templates `masterwork-<key>` sit in drop tables.
- **Needs:** nothing (pure). Art needs `SpriteKit`. **Used by:** items, shops, tower, reforge, loot, enemies
  (`armEnemy`), recruits, Limitless, combat (masterwork effects: ~30 special effect kinds in `strike`, `hurt`,
  `applyCombatDamage`, `applyUniqueWeaponHitEffects` 2456–2533).
- **Port notes:** placeholder art for task 1's six elemental masterworks, Doctor's Orders, the Hexbane Coat, Winged
  Hussar's wings, artifact looks, potion and ingredient icons (Notes, `engine/Docs/Port-Tasks/README.md`). The engine's
  open question "the level of detail of the item models" applies to all forged parts.

### 2.7 Reforge

- **For the player:** at an anvil (Blacksmith: weapons, heavy and light armor, shields; Tailor: clothing; Institute of
  Magic: staffs and wands; Hearth Home: all of those and the only anvil for masterworks, 15% off) a piece's two
  secondary parts can be replaced by any shop-made part up to the main part's rarity, for 3 × the rarity price of each
  changed part; pieces from the pack, equipped pieces and companions' worn gear can be reforged.
- **Files:** `forge-engine.js` (`reforgeKind`, `reforgeReason`, `reforgeOptions`, `reforgeCost`, `reforgedSpec`,
  `reforge`, 1787–1873); `reforge.js` (153 / 13 KB, `Reforge.create/pieces/replacePiece`: the anvil UI in a shop's
  detail pane, placeholder anvil drawing).
- **Needs:** forge, shops, companions. **Used by:** shop.js (`anvilFor`), magic-tower.js (Institute).
- **Port notes:** task 14 adds smithing materials that cut reforging costs by 75% (DG26).

### 2.8 Combat rules (the battle engine)

- **For the player:** turn-based battles between the party (the hero, up to three companions, their summons) and up
  to 50 enemies (normally 1–4; the stage lays out up to 8 a side). Turn order comes from Speed on a turn meter; each
  turn has **5 AP** (+ class bonuses, + potion effects) to spend on: weapon attack (2 AP, unarmed 1; off-hand attacks
  with two one-handed weapons), spell (3 AP default), item (potion 1 AP, poison thrown, trap placed, ingredient eaten
  by an Assimilator), skill, Guard or Evade stance (2 AP, until the next turn: ×1.5 Armor or Evasion and ×0.5 the
  other), Meditate (4 AP: 20% of max Mana + 0.5 MND + 0.2 LCK), Run (1 AP: 50 + 0.5 SPD + 0.2 LCK − 0.4 × the fastest
  enemy's SPD, at least 5%), End turn. The player commands the hero and every companion; summons and enemies act by
  AI. A target is chosen by clicking a card or a sprite. Optional 0-AP reactions (e.g. Hellish Rebuke) open a prompt.
  Victory opens the rewards window; defeat carries the party to the nearest settlement (or a region's safe retreat).
- **Rules** (`combat-engine.js`, 114 lines, `CombatRules`, pure): `nextTurn` (each actor's counter fills by Speed to
  1,000), damage = power × crit × (1 − resistance%) × 100 / (100 + 2 × Armor), at least 1 (spells ignore Armor); hit
  chance = Accuracy / (Accuracy + Evasion), ranged attacks against half Evasion; crit chance from Crit Rating =
  100 × (1 − e^(−rating/100)), at most 99%, with "momentum" (a pseudo-random distribution, `actor.critMisses`); crit
  multiplier 1.25 + bonus, at most 1.75; trap damage, rune reduction, resource restore, trap limit, EXP curves,
  meditate and escape formulas.
- **The engine** (`combat.js`, 7,041 / 549 KB, `window.Combat`), by line ranges: 1–480 actor values (`withActor`,
  `values`, `defense`, natural armor for unarmored enemies, AP, action restrictions, Ki, loadouts, spell forecasts,
  phases' damage floor, `setHp`); 490–1073 healing (`healingFactor`: Doctor's Orders), Connected Balancer's shared
  burden, artifact effects in battle, `applyCombatDamage` (barriers, death saves, Second Chance, Tough-Out), equipment
  reactions, timed effects and `expire`; 1074–1230 phases and cutscene hooks; 1231–1700 the turn loop (`advance`, which
  runs every boss mechanic's turn-start hook), defeat, `finish`, loot and EXP (2.12), outcome, refresh, AI scheduling;
  1700–2956 actions and damage: resistances, `incomingDamageFactor`, `directDamage`, traps (`triggerControlTraps`), `hurt`
  (2110–2270), weapon attack values, on-hit effects of classes, armor, masterworks and artifacts, `strike` (2612–2743),
  `attack`, summons; 2957–3916 boss mechanics (2.11); 3917–4860 effects, `cast`, Lament's second target, `useItem`,
  poisons, eating ingredients, timed effects, skills, stance, meditate, run, `action`; 4859–6078 AI (2.10); 6079–6341
  rendering (cards, the stage snapshot and `stageLook`); 6342–6693 loading consumables and enemies, `validateEnemy`, the
  Dev Mode setup, `startRoster`, encounters, pre-fight escape; 6694–6759 `restore`; 6760–6949 the public API;
  6950–7041 event wiring.
- **Effects:** timed effects on actors (`actor.effects`, `expiresTurn`), stuns, roots and AP losses (`staggerAp`,
  `rootedAp`, `trapApLoss`, applied at turn start), debuffs and cleanses, immunities, barriers, stances, damage over
  time, marks, auras, weather (one spell sets the battle's weather), equipment charges and summons, Ki and sorcery
  points, phases (`phaseTransitions` at HP thresholds, `unskippable` phases), death saves by race, the trap limit.
- **Data:** consumables, spells, enemies, classes (all above); `SPELL_ELEMENTS` (12) in app.js.
- **Needs:** sheet core and its calculations, classes, races, spells, items/forge, artifacts, alchemy (potions,
  poisons, ingredients), enemies. **Used by:** map (encounters), guild (objectives, Great Hunts), Church and Hearth
  Home (boss fights), Limitless, music (`Combat.musicTheme`), the battle stage.
- **Port notes:**
  - Every rule reads the sheet's globals through `withActor`; companions' and enemies' numbers come from the same
    code as the player's.
  - A battle is a plain object saved whole with the character (`serialize` = `structuredClone(battle)`) and resumed on
    load; actors keep their scaled stats (Limitless, Empowered ×1.3, Sparkling ×1.4 and double HP).
  - The rules resolve immediately; the stage only replays them (`stage(type, detail)` reports events). Enemy turns
    wait for the stage's beats (`BattleStage.whenIdle`) and for boss intros (`bossIntroUntil`).
  - The checks run combat with the stage off (`BattleStage.enabled = () => false`) and load a second copy of
    combat.js with internals exposed.
  - Known old failure: the Barbarian level 7 legacy bonus-choice check (Notes).

### 2.9 Enemies

- **For the player:** 174 enemy kinds across the regions: animals, goblins, orcs, trolls, giants, undead, elementals,
  dragons, merfolk, demons, harpies, satyrs, lamias, treants, dryads, bandits and more (23 races), each with classes,
  spells, gear, resistances and drops; 12 bosses. "Empowered" enemies (guild Hunt targets, artifact guardians) are
  ×1.3 attributes; "Sparkling" enemies (0.1% each, 2% for a Cull target) ×1.4 attributes, double HP, 11× EXP and
  masterwork rolls.
- **Files:** `combat.js` `loadEnemies` (fetches `enemies/index.json` and every file), `validateEnemy` (6375–6496: checks
  and also injects shared drops, e.g. Noble Hero and the Hexbane Coat for non-boss demons/devils/undead, the Shadow
  Crystal for all, Vial of Dragonblood for dragons), `armEnemy` / `ForgeRules.armCharacter` (forges real gear from
  placeholders at spawn, one level below to three above), `startRoster` (startingAllies, Empowered, Sparkling,
  Limitless scaling).
- **Data:** `enemies/*.json`, **174 files** + `index.json`; each a character record `{ format, version, id, name,
  level, raceId, classLevels, stats, inventory, equipment, knownSpells, … }` with optional fields: `drops` (70),
  `naturalWeapon` (58), `element` and `resistances` (55), `activeSkills`, `permanentBonuses`, `maxHpPercent`, `baseArmor`,
  `aiProfile` (18), `unique` (12 bosses), `introCinematic` (12), `noDropItems`, `phaseTransitions`, `startingAllies`,
  `startingSummons`, `inert`, `attackElement`, `formerNames`, and one mechanic block per boss (2.11). Races: elemental 28,
  dragon 22, undead 20, human 11, demon 9, troll 8, orc/harpy/wolf/giant/goblin 7, merfolk 6, griffon/dryad 5, impid/devil
  4, others fewer. Every enemy also has its own sprite entry (2.27) and is listed in an encounter table, a summon spell
  or a boss roster.
- **Bosses** (all `unique`, with an AI profile and an intro): Great Imp Ji'ruan (18), Juila the Grand Forest Matriarch
  (22), Nygheir the Wounded (23), Zik Lun the armored Hulk (24), Deep Sea King (32), Tankina, Disciple of Talos (44,
  Church of Talos), Ornn, God of the Forge (48, Hearth Home), Gaian, Avatar of the Land (53), Archdevil Saffron (66),
  Ancient Lich Ool (78), Ur-Mat, the False Dreamer (90), Anatemis, the White Star (100). Ten are Great Hunts (2.18).
- **Needs:** classes, races, spells, forge, combat. **Used by:** encounters, guild, Church, Hearth Home, alchemy
  (ingredient drops by enemy id), guild information (drop sources), sprites, music (boss themes).
- **Port notes:** the user wants every enemy revamped during the port for the new classes and spells (Port-Tasks
  README); boss and enemy models are to be as detailed as the cutscene scenes ("At port time"). Ids are generated from
  names (`<slug>-level-<n>`, bosses `…-unique-level-<n>`) and some are hard-coded in scripts (Tankina, Ornn, Anatemis).
  Names are matched as strings ("Empowered …", "Sparkling …" are stripped for kill counts; `formerNames`).

### 2.10 Enemy AI and boss AI profiles

- **For the player:** enemies (and summons) pick attacks, spells, skills, stances, meditation and items by forecasting
  each option's outcome; bosses follow hand-made rules on top (one strike a turn, no repeated sweeps, summon timing).
- **Files:** `combat-ai.js` (130 / 9 KB, `CombatAI`, pure): bounded serialised memory (focus target, plan, the last 16
  actions), `intent` (survive, finish, rescue, recover, support, pressure, focus) and `rank` (repetition penalties,
  committed plans). `combat.js` 4859–6078: damage and outcome estimates on copies (`estimatedWeaponStrike`,
  `estimatedSpellDamage`, `forecastRedirections`), threat and target priority, candidates for attacks, spells per
  target, skills (`skillCandidate` alone is ~290 lines), defence, meditation, items, the tactical context, then
  **17 profiles in `AI_PROFILES`** (5850–6007: `jiruan`, `juila`, `juila-court`, `nygheir`, `tankina`,
  `talos-apostle`, `deep-sea-king`, `ornn`, `saffron`, `devil-servant`, `ool`, `grave-guard`, `anatemis`, `ur-mat`,
  `gaian`, `troll-guard`, `zik-lun`; helper `oneStrikeATurn`).
- **Needs:** the combat engine (forecasts reuse its formulas). **Used by:** combat (`enemyAction`, `previewAiDecision`).
- **Port notes:** forecasts must never mutate the battle or roll dice ("previews never spend resources, change state,
  or roll RNG"); performance is measured by the test client's combat and lifecycle suites (530 AI cases).

### 2.11 Boss mechanics and phases

- **For the player:** each boss fight has its own rule, shown as a badge over the field and an overlay on the stage:
  Juila's cycle of seasons (and seasonal spirits, the Wheel of the Year spell); Nygheir's old wounds torn open by
  crits; Zik Lun's warband orders (Charge, Hold the Line, Regroup), war drummer and war horn; the Deep Sea King's royal
  duel; Tankina's gathering storm fed by lightning spells; Ornn's reforging tempers spoiled by damage; Gaian's regrowing
  heart cores and the Severed state; Saffron's infernal sacrifice of his servants; Ool's phylactery (he turns to dust
  and re-forms until it is broken); Ur-Mat's seven dream laws; Anatemis's star trials (Mirror Scales, Sunfall,
  Supernova) across three forms. Phases at HP thresholds change names, stats, sprites, music and play a cutscene.
- **Files:** `combat.js` 2957–3916 (~960 lines): `seasonState/seasonTurn`, `warbandState/warbandTurn`, `stormState`,
  `reforgeState`, `heartCoreState`, `sacrificeState`, `phylacteryState`, `dreamState`, `starState`, `royalDuelState`,
  `setOpenWounds/tearOldWound`; hooks in `advance`, `endTurn`, `hurt`, `applyCombatDamage`, `logFall`; phases
  `startNextPhase` (1074–1111), `playPhaseIntro`. Badges and looks via the snapshot's `season`/`order` slots and
  `stageLook` (6130–6163), CSS per mechanic ("Battle seasons", "Warband orders", dream laws, storm).
- **Data:** the enemy files' blocks `seasonCycle`, `oldWounds`, `warband`, `royalDuel`, `storm`, `reforge`,
  `heartCores` (+ `livingAllyProtection`), `infernalSacrifice`, `phylactery`, `dreamLaws`, `starTrials`;
  `phaseTransitions` on Nygheir, Tankina, Ornn, Ur-Mat (1 each) and Anatemis (2).
- **Needs:** combat, AI, enemies, cutscenes, music, sprites (look variants). **Used by:** —.
- **Port notes:** each mechanic was balanced with a simulator in `claude-work/` (Notes; target win rates per boss,
  e.g. Anatemis about 5% for a prepared Level 100 party). Task 14 moves Saffron and Ool from Great Hunts to
  dungeon bosses (Port-Tasks README). The simulators (`ny-sim.js`, `ju-sim.js`, `jr-sim.js`, `rt-launch.sh`, with hundreds of variants) are only on the user's PC in `~/.cache/stat-fick-build/claude-work/`, not in the repo.

### 2.12 Loot, EXP and rewards

- **For the player:** after a victory, a rewards window lists EXP, silver and items; each enemy's equipment drops at
  40% (not uniques), drop tables roll (guaranteed-until-claimed drops, first-kill drops), 1% artifact rolls by enemy
  level, ingredients (25%; elites 50% twice; bosses one of two), masterworks (raised by Luck: 0.05 × Luck), Sparkling
  masterwork rolls (70% and 5%), Exotics (Limitless), an archived spell offer for an Archivist. At the level cap no EXP
  is paid. Defeat costs nothing but the trip back to town.
- **Files:** `combat.js` 1386–1600 (`armEnemy`, `rollEquipmentDrop`, `addEnemyLoot`, `renderLoot`, `resolveLoot`,
  `offerArchivedSpell`, `awardDefeatedEnemyExperience`, `rollEnemyArtifact`, Sparkling, `rollExoticExtras`), records
  for class requirements (solo survivor, flawless, familiars).
- **Needs:** items, forge, artifacts, alchemy, Limitless, guild (Treasure Seeker's money, Cull counts). **Used by:**
  combat.

### 2.13 Battle presentation: the animated stage and the classic view

- **For the player:** two battle views chosen in Settings: **"stage"** (default), an animated battlefield with the
  region's scenery, sprites in formation, idle animation, attack and cast poses, projectiles, spell effects per element,
  floating numbers, status icons, a turn-order column, toast log lines with a pinnable history, banners for turns and
  outcomes, boss badges, a Speed-up button; and **"classic"**, a framed journal of cards (friendlies, log, foes) with a
  "next 10 turns" timeline. Both share the action bar (AP, weapon, Spells, Use an Item, skills, Guard/Evade, Meditate,
  Run, End turn).
- **Files:** `battle-stage.js` (666 / 31 KB, `BattleStage`): sprites by look signature, layout (`FORMATIONS` for 1–8
  per side, depth via `--depth`), a queue of "beats" (turn, strike, spell, item, stance, meditate, run, skill, phase,
  finish, interrupt) each an animation then its impacts, `whenIdle` for the AI, the season/order badges, the log and
  turn list. `battle-effects.js` (563 / 45 KB, `BattleFx`): DOM/SVG effects with the Web Animations API: 13 element
  palettes and motifs, offensive/support/summon spells, Tier 10 spectacles, projectiles, hit sparks, items and potions,
  stances, meditate, skills, masterwork activations. `battle-scenery.js` (303 / 31 KB, `BattleScenery`): 1600×600
  layered SVG backgrounds, **33 biomes**, a biome per region (17) and per zone, seeded variation, time of day, ambient
  motion (clouds, rain, snow, embers, fireflies). The classic cards and action bar: `combat.js` `render`/`renderBattle`.
  CSS: "Animated battlefield" and "Combat cards" blocks in `styles.css` (3022–3506).
- **Needs:** combat (events and snapshots), sprites, forge-art (weapon sprites), world (region, zone, clock).
  **Used by:** combat.
- **Port notes:** presentation never changes rules; the enemy delay depends on settings (`battlePace`,
  `battleSpeed`, `combatAnimations`). The "Sparkling" glow and several looks are placeholder art.

### 2.14 Cutscenes: boss intros, phase scenes, Tier 10 vignettes

- **For the player:** a letterboxed scene with a title card when a boss battle starts (once, never on restore), and
  when a boss changes phase; Skip and Escape end it; the AI waits until it ends. Casting a Tier 10 spell shows a 1.9 s
  non-blocking vignette with the spell's sigil and a caption.
- **Files:** `boss-intros.js` (716 / 68 KB, `BossIntros`): **19 scenes**: intros `jiruan`, `juila`, `nygheir`,
  `zik-lun`, `deep-sea-king`, `tankina`, `ornn`, `gaian`, `saffron`, `ool`, `ur-mat`, `anatemis`, and phase/special scenes
  `nygheir-phase`, `tankina-phase`, `ornn-phase`, `ool-phylactery`, `ur-mat-lucid`, `anatemis-golden`, `anatemis-zenith`;
  each draws an SVG scene around the boss's real battle sprite (and its retinue), with shared scene helpers
  (`forgeScene`, `talosScene`, `starScene`, `dreamScene`, `graveScene`). Timelines are CSS (`styles.css` 1623–2310, one
  block per boss). `tier10-cinematics.js` (90 / 9 KB): 12 vignettes, one per element's Tier 10 spell, inline SVG plus
  CSS. Played from `combat.js` (`playBossIntro`, `playPhaseIntro`, `playTierTenCinematic`).
- **Legacy:** `#anatemis-cinematic` and `#boss-special-cinematic` markup and their functions are still in
  `index.html`/`combat.js` but no enemy file triggers them any more.
- **Needs:** sprites, combat, settings (off when combat animations are off). **Used by:** combat.
- **Port notes:** the engine already made Ji'ruan's intro as a 3D proof (M10); the Tier 10 vignettes are to be
  reworked with task 12's Tier 10 spells (SP47). To capture frames the old checks pause `getAnimations()` and set
  `currentTime` (Notes).

### 2.15 Shops and towns

- **For the player:** every settlement (11 castle cities, 16 towns) has a **Blacksmith** (forged weapons and
  non-clothing armor made at the hero's level: castle 9 pieces incl. 5 weapons, town 6 incl. 3; regional masterwork
  offers such as Bloody Mary in Ravenwatch; buys weapons and armor; a Reforge tab), a **Tailor** (clothing: castle 6,
  town 4; buys clothing; Reforge), a **General Store** (consumables, traps, Camping Supplies: castle 8 kinds, town 5,
  quantities 3–6 or 2–4; the horse carriage once, 50,000 silver) and an **Apothecary** (2.17); castle cities add a
  **Magic Tower** (6 forged staffs/wands, 12 spells with a 10% chance of one over-tier, all 24 runes, rune fitting at
  25% of the rune's price in a magic circle, removal of all runes at 3× their price with the runes returned, rune
  selling) and a Guild hall (2.18). Special stores: the **Institute of Magic** (an island site in Mirrormere's lake;
  the Tower window's other venue: Institute staffs/wands and clothing at 15% off, Doctor's Orders for 30 Platinum, 12
  spells at 20% off, fitting/removal at 20% off, a caster anvil, no runes), the **Storm Reliquary** (Church of Talos,
  2.23) and **Hearth Home's forge** (2.24). Selling pays half the price (`ShopRules.salePrice`), never more than the
  shop's price; "sell all copies" and "sell all non-favourites". Resting in a settlement (10 h, 25 silver in a castle
  city, 15 in a town) restores HP and Mana; camping (8 h) uses Camping Supplies.
- **Stock:** generated per settlement when first opened in a 6-game-hour window (`ShopRules.refreshWindow`), saved in
  `shopState.locations[<settlement id>]` (blacksmith, tailor, general, tower, spells; `apothecary:<id>`,
  `church-of-talos`, `hearth-home`, `institute-of-magic`); the whole cache resets each window or when
  `STOCK_VERSION` (26) changes. Rarity and tier weights by the hero's level band (`levelBand`: ≤15 common/tiers 1–2,
  ≤30 uncommon/3–4, ≤60 rare/5–6, ≤90 epic/7–8, else legendary/9–10; Tier 10 weight 0.1).
- **Files:** `shop.js` (562 / 37 KB, `Shop`, `ShopLayout`): the shop window for `blacksmith`, `general`, `tailor`,
  `hearth`; buy/sell/reforge modes; the list on the right with the hovered item's details on the left (reused by every
  store). `magic-tower.js` (473 / 34 KB, `MagicTower`): venues `tower` and `institute`, seven menus (casters, clothing,
  spells, runes, fitting, sell, reforge), the rune circle. `shop-engine.js` (98 / 4 KB, `ShopRules`, pure): wallet
  maths (silver/gold/platinum), sale price, price formatting, level bands, rarity and tier weights, weighted sampling,
  the refresh window.
- **Data:** none of their own; stock comes from `ForgeRules`, consumables, spells, runes, `AlchemyData`.
- **Needs:** items/forge, spells, world (which settlement, castle or town, region), clock, sheet (purse, level),
  interiors (2.16), reforge. **Used by:** companions (spell prices), Church and Hearth Home (store UI), guild
  (rewards use the same wallet).
- **Port notes:** stock and prices are recomputed from the hero's current level each window; buying a forged item
  stores its id only. The carriage's drawing in the shop is placeholder art.

### 2.16 Interior scenes ("banners")

- **For the player:** every shop, tower, church, guild and apothecary window is a full-window ink-drawn room
  (1280×900) lit by its own fires, lamps and windows, by day, dusk or night; castle cities get large crowded halls,
  towns small rooms; each settlement always shows the same room. The guild's room is the menu: hovering an object
  outlines it in gold and names it.
- **Files:** `scene-banners.js` (1,418 / 111 KB, `SceneBanners.show(host, kind, place, regionId)`): 13 scene kinds
  (`blacksmith-castle` arms hall, `blacksmith-town` country smithy, `general-castle` grand emporium, `general-town`
  country store, `tower` and `institute` tower sanctum, `church` storm chapel, `guild-hall`, `guild-town`, `tailor`,
  `hearth` (arms hall), `apothecary-castle`, `apothecary-town`), built from ~100 drawing helpers (masonry, people with
  poses, furniture, goods, fires, chandeliers, magic circles, cauldrons…); region materials (oak, granite, moss,
  sandstone, redrock, limestone) and skies; three stacked SVGs (static scene with masked darkness and hatching; a
  screen-blended live layer of flames and glows; clickable spots for the guild). **Placeholder art:** `guild-hall`,
  `guild-town`, `tailor`, `institute`, `hearth`, `apothecary-castle`, `apothecary-town` (`PLACEHOLDER_ART`).
- **Needs:** `SpriteKit` (colours, ink helpers), world (region, settlement), clock. **Used by:** shops, tower,
  apothecary, Church store, guild.
- **Port notes:** layout rule for the full-window scenes: faces and counters in y≈130–360 above the panels; emitted
  fire and light draw over everything (Notes).

### 2.17 Alchemy and the Apothecary (potions, poisons, ingredients, foraging, brewing)

- **For the player:** an Apothecary in every settlement with five menus: Potions (Health and Mana pairs of your
  band and below always, plus random potions and poisons: castle 8, town 5), Ingredients (every common one, and 5 or 3
  of the region's rarer ones), Brewing (a cauldron with three ingredient boxes; known recipes only; costs nothing but
  the ingredients; the result waits on the cauldron), Recipes (three unknown recipes at 10× the potion's price), Sell
  (half price). Foraging on the map takes an hour (45% to find something, the zone's table, rivers add two), then rolls
  the zone's encounter. Enemies drop ingredients. In battle a potion costs 1 AP on yourself or an ally; poisons are
  thrown at one or all enemies (bosses take half); Health, Mana and cure potions can be used outside battle. Two classes
  (Alchemist, Assimilator: eat ingredients in battle). Guild Collect quests ask for ingredients.
- **Files:** `alchemy-data.js` (429 / 39 KB, `AlchemyData`): **56 ingredients**, **62 recipes** (10 Health/Mana +
  40 potions + 12 poisons; three ingredients each), 2 starting recipes, forage tables per region and zone, river
  ingredients, each enemy's ingredients by enemy id, the bosses' pairs; generated once from
  `engine/Docs/Port-Tasks/9_Potion_Rework/draft.md`, now the source. `alchemy-engine.js` (240 / 17 KB, `AlchemyRules`,
  pure): `matchRecipe`, forage, enemy drops, region ingredients, Apothecary stock and recipe offers, Collect picks,
  half-strength poisons for bosses, assimilation buffs. `apothecary.js` (383 / 29 KB, `Apothecary`, `Alchemy`):
  the window and the cauldron. Battle side in `combat.js` (`throwPoison`, `eatIngredient`, potion timed effects such as
  `crit-chance-percent`, `control-immune`, `turn-start-ap`, `barrier-percent`, `rise-again-percent`, `undying`, the
  Ascendance toll); `map.js` `forage`; `app.js` potion use outside battle and ingredient details.
- **Data:** the 50 potions and 12 poisons in `objects/consumables.json`; `classes/Alchemist.json`,
  `classes/Assimilator.json`; the character's `alchemy` record `{ known, brewed, foraged, keenHarvestReady }`.
- **Needs:** items, shops (window layout), world (zones, rivers), encounters, combat, enemies, classes. **Used by:**
  guild (Collect), loot.
- **Port notes:** placeholder art: both Apothecary interiors, the cauldron and its animation, potion/poison/ingredient
  icons (`potionArt` by effect colour, `ingredientArt`).

### 2.18 Adventurers Guild (quests, Great Hunts, recruits, the hooded man, artifacts, trophies)

- **For the player:** 15 guild halls (the 11 castle cities and 4 towns whose `guildServices` list
  `quest, artifacts, great-hunts, recruit`). The room is the menu: the **bulletin board** (a daily board, castle 4 and
  town 3 standard quests of different types, +1 with the Bounty Broker class, plus a Great Hunt when eligible), the
  **counter** (hand in finished quests: silver and Reputation, a parchment note rises; Deliver letters handed over),
  the **tavern** (recruits: castle 5, town 2; cost 30 + 2 × level + 25 per companion already hired, in Reputation),
  the **hooded man** (castle guilds: buy information on a class's requirements or an item's sources, 65 Reputation
  each), **artifacts** (pay 70 Reputation to have an Empowered enemy guard a chosen artifact somewhere in the region;
  once per 7 days), **trophies** (completed Great Hunts; place a rematch marker). One standard quest and one Great Hunt
  at a time; a Quests card on the map (J) shows both and lets you give up.
- **Quest types** (`GuildQuestRules.TYPES`): Hunt (an Empowered enemy at a marked spot), Cull (N kills inside a 3 km
  circle; half the zone's encounters become the target), Deliver (letters to another guild and back), Search (a spot,
  then a fight that is harder to flee), Escort (a trip out with planned bandit ambushes, 80/10/10 for 0/1/2), Travel
  (to a place and back), Collect (ingredients to the issuing guild); reward factors Hunt 1.8, Cull 1.55, Deliver 1.35,
  Search 1.2, Escort 1.1, Travel 1.0 (Collect pays like one of the lower four by rarity). Quest level = your level held
  to the guild region's range. **Great Hunts:** ten bosses (Ji'ruan, Juila, Nygheir, Zik Lun, Deep Sea King, Gaian,
  Saffron, Ool, Ur-Mat, Anatemis), each offered in a region and fought at a zone marker; reward ×2.4 and Reputation ×3;
  rematches from the trophies.
- **Files:** `guild.js` (1,069 / 84 KB, `Guild`): zones per region (`regionalZones`), the Great Hunt list, boards,
  accept/give up/hand in, objectives on the map (`objectiveAt`, `markers`, `areas`), `adjustEncounter` (Cull share),
  `onBattleFinished`, `onTravel` (Escort), the room's spots, dock panels (board, counter, artifacts, tavern, hooded man,
  trophies), the quests card, `classInformation`. `guild-quest-engine.js` (194 / 13 KB, `GuildQuestRules`, pure):
  types, factors, reward tables, Great Hunt reward, quest level, adjacency over roads and boats, trip minutes, ambush
  plans and groups, Cull and Search numbers, titles, recruit level, cost and gear rarity.
  `guild-information-engine.js` (96 / 6 KB, `GuildInformationRules`, pure): which classes and items can be offered,
  enemy locations, drop chances shown (Limitless-aware). The two interiors: `scene-banners.js` `guildHall`,
  `guildTown` (placeholder art).
- **Data:** state in `worldState.guild` (`reputation`, `completedQuests`, `quest`, `greatHunt`, `boards`, `notice`,
  `artifactOffers`, `artifactChallenge`, `greatHuntsCompleted`, `greatHuntRematches`, `avoidedGreatHunts`, `information`).
- **Needs:** world (settlements, routes, adjacency, travel minutes), encounters, combat (objective fights, Great Hunts),
  enemies, companions (recruits), items/artifacts, classes (information, Courier, Bounty Broker), alchemy (Collect),
  map (markers, Search, ticks). **Used by:** map, combat (`finish`), classes (requirement counters).
- **Port notes:** task 14 adds the Delve quest and moves Saffron and Ool to dungeons (Port-Tasks README). Not covered by
  an old check: Run keeping its normal chance in Search/Escort fights, the reward note's fade (Notes).

### 2.19 World rules: regions, continent, zones, travel, time, routes

- **For the player:** one continent of 20 regions (17 charted, 3 uncharted and empty until the port); clicking the
  map or a numbered destination walks the hero there along the fastest route (roads are fastest; rivers, marsh,
  sand and volcanic slopes slow travel); the clock runs while travelling; zones decide encounters; regions stay under
  fog until first entered; the boat (Seabridge ↔ Emberhaven on Cinder Isle, 12.6 h), caravans between unlocked castle
  cities (2 silver per km, 21 km/h of road; a castle's caravan opens on the first visit), a horse carriage (twice as
  fast on roads, 1.5× on open ground, not on mountain roads, rivers, Cinder Isle or rough zones).
- **Regions and level ranges:** Alder Vale (start; zones 1–15), Verdant Reach 5–20, Stoneveil Foothills 10–20, Sunward
  Coast 15–30, Stormwake Hills 15–35 (rainstorm weather), Mirrormere Basin 20–40, Cinder Isle 25–40, Gloamfen Marsh
  35–55 (with the Abyssal Rift, task 6), Saffron Expanse 35–50, Crimson Divide 40–55, Deep Desert 45–55, Verdigris Jungle
  45–65, High Stoneveil 45–60, Bloodstone Coast 55–70, Duskpine Forest 60–75, Ancient Battlefield 65–75, Stoneveil Summit
  85–95; `uncharted-highlands`, `uncharted-steppe`, `uncharted-delta`. **Settlements:** 11 castle cities (Alderkeep,
  Canopyhold, Crownspire, Greenwatch, Lakespire, Miragehold, Mirewatch, Nocturne Keep (needs the Vampire class),
  Ravenwatch, Stormhold, Westport) and 16 towns; **sites:** Church of Talos (Stormwake Hills), Institute of Magic
  (Mirrormere's island, reached by a bridge), Hearth Home (Cinder Isle's volcano).
- **Units:** a region keeps local coordinates 1600×1000 (120 × 75 km); on the continent it is enlarged 1.4×
  (`ContinentRules.SCALE`), continent units C = origin + local × 1.4, `UNITS_PER_KM` 40/3 C; a region's ground reaches
  600 local units past its map (`REACH`); every point belongs to the nearest region. Encounters every 126 travel
  minutes (`ENCOUNTER_MINUTES` 90 × 1.4). Travel 36 game minutes per real second at 1×; idle 0.5.
- **Files:** `world-engine.js` (1,128 / 74 KB, `WorldRules`, pure): the Alder Vale's data inline, the region registry,
  `normalize` (the world state and its migrations), zones (`zoneAt`, 436–568), speeds, carriage ground, the road graph,
  routes within a region and across the continent (`continentRoute`: Dijkstra over every region's roads, off-road legs
  only through entered regions, `landPath` A* on a 32 C grid round water), caravan trips, stepping, lighting, exits,
  retired artifact ids. `world-region-data.js` (414 / 30 KB, `WorldRegionData`): the other 16 charted regions and the
  3 empty ones: settlements, roads and rivers as Bézier point lists, zone shapes (ellipses), exits, sites, boats,
  level ranges, weather, guild services. `continent-engine.js` (277 / 19 KB, `ContinentRules`): origins of draft 6,
  `regionAt`, building the continent (stamping roads and places onto the land), `landAt` (bilinear), coverage between
  neighbours (`paintedGrid` at 32 C, wandering borders), extents. `continent-shape.js` (11 lines / 255 KB,
  `ContinentShape`): the land field on a 1150 × 1069 grid of 16 C cells, run-length coded; generated by
  `engine/Docs/Port-Tasks/8_Map_Rework/bake.py`, not to be edited by hand.
- **Data:** inside those scripts; the approved layout and its scripts are in `engine/Docs/Port-Tasks/8_Map_Rework/`.
- **Needs:** nothing (pure). **Used by:** map, encounters, guild, shops, alchemy, combat (defeat retreat, scenery
  biome), music (region theme), saves.
- **Port notes:** the three uncharted regions' names, terrain, enemies, levels, towns and connections are to be made
  in the port (W27). The engine's plan is a 3D map (W1: every 2D look of the continent is placeholder).

### 2.20 Encounters

- **For the player:** while travelling (every 126 travel minutes, at the zone's chance), when foraging, at guild
  objectives and in Search/Escort events, a prompt shows the enemies (Sparkling ones marked) with Fight or Run (a
  pre-fight escape with +25%, harder for some quest fights). Dev Mode adds Force encounter.
- **Rules:** per region a set of `chances` (per zone, 0 in towns and on roads; e.g. 15–62%), `profiles` (each zone's
  level range; safe zones) and `tables` (weighted groups by level). A group's level is picked near the hero's level
  clamped to the zone's range (weights 72/14/2 by distance); the chance falls to 70% or 40% when the hero is 10 or 20
  levels above the zone; class effects change it. About **89 zone tables and 567 groups**; empty rule sets for the
  three uncharted regions.
- **Files:** `encounter-engine.js` (1,023 / 55 KB, `EncounterRules`, pure; lines 1–904 are data: `enemyLevels`,
  `dragonElements`, `animalFamilies`, and 17 regions' chances/profiles/tables; 905–1023 rules: `select`, `generate`,
  `generateIncluding` (a Cull target's group), `encounterChance`, `roll`, `enemiesForZone`). Prompts and flow in
  `map.js` (`presentTravelEncounter`, `renderEncounterPrompt`, `startPendingEncounter`, `runFromPendingEncounter`).
- **Needs:** world (zones), enemies (names, levels). **Used by:** map, guild, alchemy (forage), Sparkling rolls.

### 2.21 Map view and terrain painting

- **For the player:** a map window over the sheet: a painted ink-and-parchment continent with hillshade, hatching,
  contours, forests, fields, rivers, roads, towns and castles drawn as little buildings, area names; zoom (1–10× of
  the region's fit, wheel, +/−), pan (drag, arrow keys), Fit and Follow; a header with region, levels, day, clock,
  light and time-speed buttons; the zone readout; an actions bar (Pause, Clear marker, Camp, Rest, Forage, Blacksmith,
  General Store, Apothecary, Tailor, Magic Tower, Guild, Church, Institute, Hearth Home, respecs, Storm Reliquary,
  Caravan, Travel by boat, Search, Quests, Track Quarry); a numbered destinations list; **31 keyboard shortcuts**; night
  lanterns in windows; rain and lightning in Stormwake; fog and cloud banks as the Stoneveil rises; the boat and the
  caravan wagon animated along their way (skippable); quest markers and the Cull circle; fog over unentered regions;
  "Uncharted" parchment.
- **Files:** `map.js` (1,469 / 107 KB, `WorldMap`): the camera (SVG `viewBox` over the canvas), drawing the terrain
  per view (`drawTerrain`), overlays, altitude effects, lanterns, region art, markers, the clock, ticks and travel,
  encounters, rest/camp/forage, boat, caravan, carriage, keyboard, ambient time, route restore. `map-terrain.js` (893 /
  62 KB, `MapTerrain`): per region a field grid (2-unit cells, 14 channels: colour, height, water, forest, pine, rock,
  wet, sand, snow, farm, glow, mask) built in slices; deterministic features (trees, rocks, reeds, dunes, farms, ruins)
  with LOD; 256-px canvas tiles at 1/2/4/8 px per unit with an LRU; per-pixel hillshade, hatching, contours, water,
  micro relief, field patchwork, canopy, Worley cells; roads, rivers and settlements painted in. `map-regions.js`
  (1,062 / 76 KB, `MapRegions.spec(id)`): **17 region specs** (field, cover, tree kinds, decoration, labels; a shared
  `mountain()` so the three Stoneveil regions join). `map-continent.js` (170 / 12 KB, `MapContinent`): the backdrop,
  region overviews at far zoom, the coastline, the fog image, uncharted parchment. `map-ink.js` (350 / 31 KB, `MapInk`):
  the palette `tone`, paper, compass, scale, frame and symbol scattering (`finish` belongs to the pre-continent SVG maps
  and is mostly unused now; `tone` and `INK` are still used). CSS: the map blocks in `styles.css` (around lines 640–760).
- **Needs:** world rules, continent, encounters, guild (markers, areas), shops/services (buttons), combat, alchemy,
  sheet (clock, purse). **Used by:** the guild, shops and sites (opened from its actions bar).
- **Port notes:** everything painted is placeholder for the port's 3D map (W1), but the rules behind it are not.
  Painting is main-thread JavaScript per pixel; old checks read `#regional-map`'s `viewBox`, not its bounding box (Notes).
  `map-speed-browser.js` is a known flaky timing check.

### 2.22 Companions

- **For the player:** up to three companions, hired at a guild tavern for Reputation (they arrive at the region's
  level with one melee forged weapon and one body armor, every stat point and class point unspent and no class) or
  imported from a saved character file (≤10 MB, renamed on import). Each has a full sheet in the Companions window:
  attributes, resources, classes, passives, skills, spells (learned at a Magic Tower's stock for coin; class spell
  choices), equipment taken from and returned to the hero's pack. In battle the player commands them; they level with
  the battle's EXP, regenerate with the clock, rest with the hero, follow the hero's Limitless switch; dismissing one
  returns their gear to the pack. They appear in the Field tools rail.
- **Files:** `companions.js` (508 / 53 KB, `Companions`): the roster and sheet, import, `makeOffer(s)` (recruits:
  race, name lists per playable race, gender, a look, forged gear by `GuildQuestRules.gearRarity`), `allowedSlots`,
  `spellPriceFor`, `restoreAll`. `companion-sheet-details.js` (252 / 24 KB, `CompanionSheetDetails`): the detailed
  member browser (stats, classes, the equipment/spell picker modal), reusing the sheet's calculations.
- **Data:** the companions live inside the save (`companions[]`: `{ id, name, character }`, each `character` a full
  record normalised with `{ companion: true }`); `recruitOffers[]`.
- **Needs:** sheet core (the record and its calculations; combat's `previewValues`), classes, spells, items/forge,
  races, sprites (looks), guild (tavern), shops (spell stock). **Used by:** combat, guild, Limitless, rest/camp, reforge
  (companions' worn gear).
- **Port notes:** companions are evaluated through combat's `withActor`, not through their own code paths; the AI does
  not drive them.

### 2.23 Church of Talos

- **For the player:** a site in Stormwake Hills. Challenge Tankina, Disciple of Talos (Level 44, with two Apostles of
  Talos; she gains Gorthok the Thunder Boar in her second phase). Once she has fallen: Respec Classes and Respec Stats
  (12,000 silver each; fulfilled "other" class requirements stay fulfilled) and the **Storm Reliquary** store (6 Church
  of Talos lightning staffs/wands, 8 lightning spells with a 10% chance of one over-tier, the Stormhide Cuirass for 20
  Platinum always on offer). She can be fought again.
- **Files:** `church-of-talos.js` (294 / 23 KB, `ChurchOfTalos`): its window (created by the script), challenge,
  respecs, the store (`#church-store-modal` in `index.html`, two menus: arms, spells; scene `church` = storm chapel).
- **Data:** the site in `world-region-data.js`; `worldState.defeatedSiteBosses`; stock in
  `shopState.locations["church-of-talos"]`.
- **Needs:** world (site), combat, enemies (Tankina's mechanics), forge (`generate({ maker, element })`), spells, shops
  (layout, wallet, refresh window), interiors. **Used by:** map (buttons), combat (`onVictory`).

### 2.24 Hearth Home

- **For the player:** a forge site on Cinder Isle's volcano (the road beyond Emberhaven). Challenge Ornn, God of the
  Forge (Level 48, with his Vulcanic Ram guarding the anvil). Once he has fallen, the forge opens: Hearthforge weapons
  (5) and armor (4) at 10% off, buying nothing, and the only anvil that reforges masterworks (15% off). He can be fought
  again.
- **Files:** `hearth-home.js` (64 / 4 KB, `HearthHome`): its window (created by the script), challenge, enter; the
  store is the shop window's `hearth` kind (`shop.js` `HEARTH`); scene `hearth` reuses the arms hall (placeholder).
- **Needs:** world (site), combat, enemies, shops, reforge. **Used by:** map, combat (`onVictory`).
- **Port notes:** Ornn was a Great Hunt until task 3; saves with his hunt get a note (world-engine `normalize`).

### 2.25 Limitless (New Game Plus) and the Exotics

- **For the player:** after Anatemis's first defeat a pop-up announces Limitless and a button appears under the hero's
  name. On: Levels up to 200 (5 points and +1 to each attribute per level, no class points), enemies below your level
  rise to it and every enemy gets +25% attributes (bosses also gain the class levels of a Level-100 character),
  masterwork drop chances ×2, gear above Level 100 can be worn, the Exotic rarity drops (a green-purple wave): Fletcher,
  Tough-Out, Winged Hussar, Cherubim, Connected Balancer, made with Exium. Off: you and your companions are your Level-100
  selves (everything above is kept aside for later), gear above 100 comes off into the pack. The sheet's numbers glow
  in the Exotic wave while on.
- **Files:** `limitless-engine.js` (108 / 9 KB, `LimitlessRules`, pure): the record `{ unlocked, active, kept, spent }`,
  `cap`, `keepAside`/`restore`, `noteSpent`, `scaleEnemy` (×1.25, `BOSS_CLASSES` per boss id), `wearable`.
  `limitless.js` (87 / 5 KB, `Limitless`): the toggle, `setActive`, `onVictory`, the notice. Spread elsewhere:
  `forge-engine.js` (`MAX_LEVEL` 200, `levelCurve`, Exium, `EXOTIC_IDS`), `combat.js` (scaling at spawn, doubled drops,
  `rollExoticExtras`, the Exotics' effects, the bosses' one-strike rule for skills, 0 EXP at the cap), `app.js` (caps,
  level-up, `needs-limitless` borders), `guild-information-engine.js` (odds), CSS (`.item-rarity-exotic`,
  `body.is-limitless`, `exotic-wave`).
- **Needs:** sheet core, combat, enemies, forge, companions, Anatemis's battle. **Used by:** loot, guild information.
- **Port notes:** task 14's dungeon enemies are to scale through the same `scaleEnemy` (Port-Tasks README). Placeholder
  art: Winged Hussar's wings and the Exotic looks.

### 2.26 Music

- **For the player:** every region has its own theme; ordinary battles play the battle theme in the region's own
  arrangement (key, mode, tempo, instruments, groove and a quote of the region's melody); each boss has a theme, and
  phases or states switch it (e.g. Ool mortal, Gaian severed, Anatemis's three forms). Crossing into another region
  crossfades over 4 s; the uncharted regions keep the last charted region's music. A volume slider and the current
  track's name are in Settings. Audio starts on the first pointer press (browser autoplay rules).
- **Files:** `music-composer.js` (788 / 46 KB, `MusicComposer`): the score compiler (10 modes, chord qualities, scale
  degrees, patterns, `lint` for melody/chord clashes), deterministic mulberry32 randomness, rooms (generated impulse
  responses), the instruments (bowed and brass voices, formant choirs, organ and wind periodic waves, Karplus-Strong
  plucks, mallets, bells, glass, sub), percussion, textures, the step sequencer with swing and voice leading,
  `perform`. `music-themes.js` (2,521 / 124 KB, `MusicThemes`): **55 themes**: 17 region themes, the battle theme
  (`combat`) and its 17 regional arrangements (`variations`, expanded by `arrange`), 20 boss themes (Ji'ruan; Juila;
  Nygheir, Nygheir the Scarred; Zik Lun; Deep Sea King; Tankina, Tankina Chosen; Ornn, Forge Erupts; Gaian, Gaian
  Severed; Saffron; Ool, Ool Mortal; Ur-Mat, Lucid Change; Anatemis White, Golden, Zenith), a `LOUDNESS` trim per theme
  (about −17 LUFS). `music.js` (1,225 / 72 KB, `Music`): the original score engine with 24 scores (all now replaced at
  load by composer themes of the same id), the master chain (gain, limiter, delay, hall), theme choice
  (`desiredTheme`: region, `combat@<region>`, `Combat.musicTheme()`), the crossfade, volume, `renderOffline`,
  `scoreInfo`, `lint`.
- **Data:** the scores are in `music-themes.js`; `music/` holds 43 MP3 offline exports (17 region, 17 region battles,
  9 boss/battle) that the game never loads.
- **Needs:** world (region), combat (battle, boss, phase), settings. **Used by:** —.
- **Port notes:** already ported to the engine in M8 (bit-exact with Chromium; `engine/Docs/Audio.md`), with the
  crossfade listed as task 8 work for the engine's soundtrack. The game has no sound effects; the port is to add them
  (user decision 2026-11-03).

### 2.27 Sprites (battle figures, race looks, enemy looks)

- **For the player:** every hero, companion, summon and enemy appears as an ink-drawn figure in the gothic "Darkest
  Dungeon"-like style (Notes), wearing its actual weapon, shield and armor; bosses change look with phases, seasons,
  storms, tempers, wounds and states; the race picker previews the 8 looks; the classic view and the turn list use
  portraits.
- **Files:** `sprite-kit.js` (299 / 20 KB, `SpriteKit` as `K`): colour maths, the muted palette (`tone`, `"!"` to opt
  out for glows), ink shadows clipped per shape (`beginInk`/`endInk`/`clipped`), hatching, primitives, metal and cloth
  palettes (`MATERIALS`, forged materials registered as `forged-<mat>-<maker>`), `itemVisual` (an item → its visual
  kind, material, rarity). `sprite-equipment.js` (492 / 53 KB, `K.equipment`): weapons drawn in their own frame
  (sword, dagger, greatsword, axe, mace, club, hammer, spear, staff, wand, bow, crossbow, trident, rapier), masterwork
  weapon and shield designs, shields, claws, `armorLayers` for the humanoid rig (torso, sleeves, gauntlets, greaves,
  skirts, head pieces, capes). `sprite-humanoid.js` (1,128 / 136 KB, `K.humanoid`): one rig for every playable race and
  humanoid monster: proportions (catalogue values read as ratios of the old chibi rig), skins and elemental/construct
  materials, heads, ears (incl. beast ears), eyes, mouths, hair, beards, horns, crests, headwear (boss crowns),
  outfits (boss robes and armor), lower bodies (legs, hooves, talons, fish and snake tails, floating robes, roots),
  wing-arms, tails, wings, boss extras (war helm, warpaint, sparks, dream glyphs, embers, pauldrons, trophies,
  standard, drum, veins). `sprite-creatures.js` (648 / 78 KB): `K.quadruped` (wolves, bears, boars, stags, rams…),
  `K.griffon`, `K.dragon` (plates, horn crowns, relics, tattered wings, wounds, constellations, aurora…), `K.wisp`,
  `K.core` (seed, phylactery). `sprite-catalog.js` (629 / 69 KB, `Sprites`): `RACES` (9 playable races × 4 male and 4
  female looks = 72), `ENEMIES` (174 entries, one per enemy file; built from helper presets), `RACE_FALLBACKS`,
  natural weapon visuals, `equipmentVisuals`, `specFor` (merges phase/season/storm/temper variants from `stageLook`),
  a render cache, the API (`forActor`, `portrait`, `playablePreview`, `renderSpec`, `itemVisual`, `enemyNames`).
  `forge-art.js` draws forged weapons and armor for sprites (2.6). CSS: idle and action animations, contour shadows
  (`.sp-ink`, `.sp-inked`), glows (`.sp-glow`, `.sp-eyeglow`).
- **Needs:** items/forge (equipment visuals), `getItem`. **Used by:** battle stage, classic cards, race picker, boss
  intros (the boss's real sprite), recruits, interiors (shared kit).
- **Port notes:** placeholder looks: Half-Beast, Goliath and Merfolk (task 11), the six Abyssal Rift enemies (task 6),
  the Void Imp and other later additions built from existing kits. The engine's plan remakes figures as 3D models
  ("how the inline SVG art and its transforms map to the Blender remakes" is an open question; boss and enemy models
  as detailed as the cutscene scenes).

### 2.28 Settings

- **For the player:** a Settings dialog (⚙, top right): sound volume (0–100%) with the current track's name; battle
  interface (animated stage or classic); battle pace (fast or slow, ×1.5 delays); combat animations on/off (also turns
  off boss intros and Tier 10 vignettes). In battle a Speed-up button sets battle speed to instant. Elsewhere: the
  light/dark theme toggle in the top bar and the Dev Mode toggle (both saved with the character, not with the
  settings); map time speed (saved in the world state).
- **Files:** `settings.js` (41 / 2 KB, `GameSettings`: `{ battlePace, combatAnimations, battleUi, battleSpeed }` in
  `localStorage["stat-fick-game-settings"]`, `enemyDelay`); `music.js` (volume in `stat-fick-sound-volume`).
- **Needs:** —. **Used by:** battle stage, combat, cutscenes, music.

### 2.29 Interface shell, styles and input

- **For the player:** a parchment/stone "gothic ink" UI with a leather-bound navigation rail ("Field tools": purse,
  companions, Map, Combat (Dev), Spell book, Inventory, Companions, Classes, Skills, Passives), framed panels, brass
  buttons, inked meters, rarity colours and element stripes; on windows narrower than 840 px the rail becomes a drawer
  (☰). Every window closes with Escape and its × button; the map has 31 keyboard shortcuts; dialogs trap Tab focus;
  ARIA roles and live regions throughout. A "Return to combat" button appears in the top bar during a battle.
- **Files:** `styles.css` (4,184 lines / 423 KB): base and "Adventurer's ledger" styles, the map, combat and cutscene
  blocks (one per boss), shops, the shared menu chrome, companion sheets, the animated battlefield, combat cards, the
  "Gothic ink theme" (3507 on, `:root`-prefixed to win), item rarity (3709), shop halls and the Magic Tower, the guild
  interior, the reforge anvil, Limitless gear borders, the Potion Rework block (4150 on). `interface.js` (66 / 3 KB):
  the drawer. `index.html` (1,004 lines): all static markup. Window/modal state: `app.js` `updateModalState`, Escape
  handling in app.js, combat.js, map.js and each store module.
- **Port notes:** the engine's UI already reproduces this stylesheet's look for the 22 captured screens (from the repo's
  first commit, 3,915 lines); the stylesheet has grown since (`engine/Docs/Engine-Plan.md`, "At port time"). Symbols
  outside Lato/STIX (⚙ ✦ ☾ ↓ ＋ ⚔ ♟ …) are an open question there.

### 2.30 Saves and data loading

- See 1.4 for the save. Data loading (`app.js` `synchronizeSourceFiles`): `objects/spells.json`, the 130 class files,
  `objects/races.json`, `objects/artifacts.json` are fetched with a cache-busting query; on failure the copies cached
  in localStorage are used; "Refresh file(s)" buttons in the spell book, inventory, classes and race dialogs reload
  them (and on a browser, "Choose …json" asks for a file through the File System Access API and remembers its handle in
  IndexedDB). `combat.js` loads `consumables.json`, `key-items.json`, `enemies/index.json` and every enemy file.
  Nothing else is loaded at run time: everything else is in the scripts.
- **Port notes:** M9 gave the engine packs, a VFS, versioned binary data, the JSON class loader, saves and settings;
  M12-9 decided a `Statventure.Game.Data` library for the game's record kinds.

### 2.31 Desktop shell and test client

- Covered in 1.2 and 1.9. The shell's only page-facing API is `window.statFickDesktop` (save, load); the test client
  adds `captureTestScreen`. Both are Electron 44.5.0 serving the folder over a custom protocol.

---

## 3. Screens

Every view the player sees. "Layer" = a `.modal-layer` over the sheet unless noted.

| # | Screen | What is on it | System | Owner (script; markup) |
|---|---|---|---|---|
| 1 | **Character sheet** (main page) | top bar (✦ Dev Mode, name, world clock, autosave dot, theme toggle, Save/Load/New Character, "Return to combat"), hero name and race seal, Limitless button, EXP card, Level/HP/Mana cards, Guard and Evasion, equipment slots (2 hands, armor, 2 artifacts), the eight attributes with steppers and respec, the Field tools rail (purse, companions, navigation) | 2.1 | `app.js`, `interface.js`, `limitless.js`; `index.html` 64–300 |
| 2 | Field tools drawer | the rail as a drawer under 840 px | 2.29 | `interface.js` |
| 3 | Settings | volume and current track, battle interface, pace, combat animations | 2.28 | `settings.js`, `music.js`; `#settings-modal` |
| 4 | Race picker + appearance step | 9 playable races, then gender and 4 looks each | 2.2 | `app.js`; `#race-modal` |
| 5 | Spell book | known spells, filters, details, Use, Refresh file | 2.4 | `app.js`; `#spellbook-modal` |
| 6 | Spell catalogue (Dev) | add any spell | 2.4 | `app.js`; `#spell-picker` |
| 7 | Inventory | tabs (weapons, armor, artifacts, consumables, ingredients, runes), favourites, details, equip, potion use | 2.5 | `app.js`; `#inventory-modal` |
| 8 | Item catalogue (Dev) | add any item | 2.5 | `app.js`; `#item-picker` |
| 9 | Classes | available and current classes, class details, class information tab | 2.3, 2.18 | `app.js`; `#classes-modal` |
| 10 | Class spell choice | pick N of the offered spells | 2.3 | `app.js`; `#class-spell-choice-modal` |
| 11 | Skills | skill list, details, activate | 2.3 | `app.js`; `#skills-modal` |
| 12 | Passives | passive list and details | 2.3 | `app.js`; `#passives-modal` |
| 13 | Companions | roster, import panel, companion sheet (stats, resources, classes, skills, spells, gear), Dismiss | 2.22 | `companions.js`; `#companions-modal` |
| 14 | Companion picker | choose equipment or spells for a companion | 2.22 | `companion-sheet-details.js` (created by script) |
| 15 | Limitless unlocked notice | pop-up after Anatemis | 2.25 | `limitless.js` (created by script) |
| 16 | Combat setup (Dev) | pick an enemy and a count | 2.8 | `combat.js`; `#combat-setup` |
| 17 | **Combat view, stage** | scenery, sprites, effects, turn list, log toasts/history, banners, boss badge, Speed up, action bar | 2.8, 2.13 | `combat.js`, `battle-stage.js`, `battle-effects.js`, `battle-scenery.js`; `#combat-view` (full screen) |
| 18 | Combat view, classic | friendly cards, battle log, opponent cards, next-10-turns timeline, action bar | 2.8, 2.13 | `combat.js`; `#combat-view` |
| 19 | Action picker | spells or items for this turn (weather choice, Golden Apex choice, Lament second target) | 2.8 | `combat.js`; `#combat-ability-picker` |
| 20 | Optional reaction | 0-AP reaction prompt (e.g. Hellish Rebuke) | 2.8 | `combat.js`; `#combat-reaction-modal` |
| 21 | Battle rewards | EXP, silver, items; archived spell offer | 2.12 | `combat.js`; `#combat-loot-modal` |
| 22 | Boss intro / phase cutscene | letterboxed scene, title card, Skip | 2.14 | `boss-intros.js` via `combat.js`; `#boss-intro-cinematic` (overlay) |
| 23 | Tier 10 vignette | sigil, spell name, caption (non-blocking) | 2.14 | `tier10-cinematics.js`; `#tier10-cinematic` (overlay) |
| 24 | (legacy) Anatemis and "special" cinematics | markup kept, no longer triggered | 2.14 | `combat.js`; `#anatemis-cinematic`, `#boss-special-cinematic` |
| 25 | **Map** | header (region, levels, day, clock, light, time speeds), canvas terrain + SVG overlay + weather, zoom/fit/follow, carriage switch, scale bar, zone readout, actions bar, destinations list, status | 2.19–2.21 | `map.js` (+ map-terrain/regions/continent/ink); `#map-modal` |
| 26 | Caravan panel | other unlocked castle cities, fare, time, route preview | 2.19 | `map.js`; `#map-caravan-panel` (docked on the map) |
| 27 | Boat voyage / caravan trip | animated trip on the map, skippable | 2.19 | `map.js` |
| 28 | Encounter prompt | zone, enemy list (Sparkling marked), Fight, Run | 2.20 | `map.js`; `#map-encounter-modal` |
| 29 | (legacy) Region transition prompt | "Travel onward?"; never shown since task 8 | 2.19 | `map.js`; `#map-region-transition-modal` |
| 30 | Quests card | the standard quest and the Great Hunt, progress, Give up | 2.18 | `guild.js`; `#guild-quests-modal` |
| 31 | **Shop window** (Blacksmith, General Store, Tailor, Hearth Home forge) | interior scene, purse, Buy/Sell/Reforge, list + details, the anvil | 2.15, 2.7, 2.16 | `shop.js`, `reforge.js`, `scene-banners.js`; `#shop-modal` |
| 32 | **Magic Tower / Institute of Magic** | interior, menus (casters, clothing, spells, runes, fitting circle, sell, reforge), popup | 2.15 | `magic-tower.js`; `#magic-tower-modal` |
| 33 | **Apothecary** | interior, menus (potions, ingredients, brewing cauldron, recipes, sell) | 2.17 | `apothecary.js`; `#apothecary-modal` |
| 34 | Church of Talos | description, Challenge Tankina, respecs, store button | 2.23 | `church-of-talos.js` (created by script) |
| 35 | Storm Reliquary | storm chapel interior, arms and spells menus | 2.23 | `church-of-talos.js`; `#church-store-modal` |
| 36 | Hearth Home | description, Challenge Ornn, Enter the forge | 2.24 | `hearth-home.js` (created by script) |
| 37 | **Adventurers Guild hall** | clickable room (board, counter, tavern, hooded man, trophies), docked panel (board, counter, artifacts, tavern recruits, hooded man's information, trophies), reward note | 2.18 | `guild.js`, `scene-banners.js`; `#guild-modal` |
| 38 | Save / load dialogs | native file dialogs (desktop) or download / file input | 1.4 | `app.js`, `desktop/main.cjs` |
| 39 | Native window menu | File, View, Help › About | 1.2 | `desktop/main.cjs` |

---

## 4. Dependency graph (what must exist before what)

Built from the code's actual calls. Each line names what it needs; the order is one in which every line's needs come
before it (not every line needs everything above it).

1. **Pure rule tables with no game dependencies:** `ShopRules` (wallet, bands, weights), `CombatRules` (formulas),
   `CombatAI` (ranking policy), `ForgeRules` (forged items, runes, masterworks, Exotics, migrations), `WorldRegionData`
   + `ContinentShape` → `ContinentRules` → `WorldRules`, `EncounterRules`, `AlchemyData` → `AlchemyRules`,
   `LimitlessRules`, `GuildQuestRules` (needs `WorldRules`, `EncounterRules`), `GuildInformationRules`.
2. **Catalogue data:** races, classes (+ the class-effect interpreter), spells, artifacts, consumables, key items,
   ingredients; `getItem` (needs `ForgeRules`, `AlchemyData`).
3. **The character record and its calculations** (attributes, HP/Mana/Armor/Evasion, class/race/artifact/equipment
   effects, spell values and costs, `normalizeCharacter` with every migration). Needs 1–2. Everything below evaluates
   actors through it.
4. **Character sheet screens** (sheet, race picker, spell book, inventory and equipment, classes, skills, passives).
   Needs 3; the race picker also needs sprites (8) for its previews.
5. **Enemies** (files, `validateEnemy`, `armEnemy` forging their gear, Empowered/Sparkling/Limitless scaling). Needs
   2–3, `ForgeRules`, `LimitlessRules`.
6. **Combat engine** (turn loop, actions, damage, effects, summons, potions, poisons, traps, phases, saving a battle).
   Needs 3 and 5, `AlchemyRules` (potions/poisons/ingredients), consumables.
7. **Enemy AI and boss AI profiles** → **boss mechanics** (11 engines). Need 6.
8. **Sprites** (`SpriteKit` → `sprite-equipment` → `sprite-humanoid`/`sprite-creatures` → `sprite-catalog`) and
   **forge art** (needs `SpriteKit`, `ForgeRules`). Need item visuals (2); independent of combat.
9. **Battle presentation** (scenery needs region/zone and clock from `WorldRules`; stage and effects need 6 and 8) and
   **cutscenes** (need 8 and 6's hooks).
10. **Loot and rewards** (needs 6, `ForgeRules`, artifacts, alchemy drops, Limitless, Sparkling; Treasure Seeker reads
    guild pay and shop sales).
11. **World clock and travel, encounters, map view** (needs `WorldRules`, `EncounterRules`, terrain painting, and 6 for
    fights; foraging needs `AlchemyRules`; markers need the guild, 15).
12. **Interior scenes** (need `SpriteKit`, settlement and region from `WorldRules`, the clock).
13. **Shops** (Blacksmith, Tailor, General Store, Magic Tower/Institute) + **reforge** (need `ForgeRules`, spells,
    runes, consumables, settlements, the clock's 6-hour windows, 12) → **Apothecary** (needs `AlchemyRules` and the shop
    layout).
14. **Companions** (need 3, 4, `ForgeRules` for recruits' gear, sprites for looks, shops for spells; fight through 6).
15. **Adventurers Guild** (needs `WorldRules` routes and adjacency, encounters, 6 for objective fights and Great Hunts,
    enemies, companions for the tavern, artifacts, classes for information, `AlchemyRules` for Collect, the map for
    markers, Search and travel ticks, 12 for the room).
16. **Church of Talos** and **Hearth Home** (need a boss fight (6–7, 11's site), shops' layout and wallet, forge,
    spells, 12).
17. **Limitless** (needs Anatemis's battle (6–7), levels past 100 in 3, forge `MAX_LEVEL` 200, drops (10), companions).
18. **Music** (needs region and battle/boss state; the synth itself is independent and already in the engine).
19. **Settings** (independent; read by 9, 13's banners, and music).

Cross-links that make the order less than strict: the sheet's class requirements read counters from combat, the map,
shops, the guild and alchemy (kills, distance travelled, items sold, guild quests, potions brewed, ingredients
foraged); combat's `finish` calls the guild, Church, Hearth Home and Limitless; `advanceWorldTime` (sheet) restocks shops
and renders the map clock; `saveCharacter` asks combat to serialize itself.

---

## 5. Sizes

Lines and bytes of the scripts by system (every script counted once; app.js and combat.js are given whole to the
system that owns most of them; their internal ranges are in sections 2.1–2.12). Data files are the files outside
`scripts/`; data embedded in scripts is listed in the last column.

| System | Scripts | Lines | KB | Data files | Data inside scripts |
|---|---|---:|---:|---|---|
| Character sheet, races, classes, spells, items, skills (app.js) | 1 | 5,956 | 355 | 130 class JSON (624 KB); `spells.json` (388); `races.json` (28); `artifacts.json` (85); 9 seals | the 130 class paths |
| Combat rules + AI + boss mechanics (combat, combat-engine, combat-ai) | 3 | 7,285 | 564 | 174 enemy JSON + index (720 KB); `consumables.json` (71); `key-items.json` (5) | 17 AI profiles |
| Battle presentation (stage, effects, scenery) | 3 | 1,532 | 108 | — | 33 biomes, 13 element palettes |
| Cutscenes (boss intros, Tier 10) | 2 | 806 | 76 | — | 19 scenes, 12 vignettes |
| Sprites (kit, equipment, humanoid, creatures, catalog) | 5 | 3,196 | 353 | — | 72 playable looks, 174 enemy looks |
| Forge rules, item art, reforge | 3 | 3,083 | 281 | — | 8 makers, ~39 materials, 23 weapon types, 12 armor types, 24 runes, 37 masterworks/Exotics, 49 artifact shapes |
| Shops (shop, magic-tower, shop-engine) | 3 | 1,133 | 76 | — | stock sizes, venues |
| Interior scenes (scene-banners) | 1 | 1,418 | 111 | — | 13 scene kinds |
| Church of Talos, Hearth Home | 2 | 358 | 27 | — | rosters, store sizes |
| Alchemy (data, rules, Apothecary) | 3 | 1,052 | 84 | (potions and poisons are in `consumables.json`) | 56 ingredients, 62 recipes, forage and drop tables |
| Guild (guild, quest rules, information rules) | 3 | 1,359 | 103 | — | 10 Great Hunts, zones per region |
| World rules, continent, encounters | 5 | 2,853 | 433 | — | 20 regions, 27 settlements, 3 sites, ~89 zone tables / 567 groups, the 255 KB land field |
| Map view and terrain painting | 5 | 3,944 | 287 | — | 17 region specs |
| Companions | 2 | 760 | 76 | — | recruit name lists |
| Limitless | 2 | 195 | 14 | — | boss class levels |
| Music (music, composer, themes) | 3 | 4,534 | 241 | 43 MP3 exports (61 MB, unused) | 55 themes (+ 24 legacy scores) |
| Settings, drawer | 2 | 107 | 5 | — | — |
| **All 48 scripts** | **48** | **39,571** | **3,198** | | |
| `index.html` | | 1,004 | 82 | | |
| `styles.css` | | 4,184 | 423 | `objects/fonts/` (5 fonts) | 245 keyframes |
| Desktop shell (`desktop/*.cjs`) | 3 | 185 | | icons | |
| Test client (`test-client/*.cjs` + `tests/`) | 5 + 52 | 383 + 10,553 | | 1 fixture JSON | |

Per-script sizes, smallest first (lines / KB): settings 41/2, hearth-home 64/4, interface 66/3, limitless 87/5,
tier10-cinematics 90/9, guild-information-engine 96/6, shop-engine 98/4, limitless-engine 108/9, combat-engine 114/7,
combat-ai 130/9, reforge 153/13, map-continent 170/12, guild-quest-engine 194/13, alchemy-engine 240/17,
companion-sheet-details 252/24, continent-engine 277/19, church-of-talos 294/23, sprite-kit 299/20, battle-scenery
303/31, map-ink 350/31, apothecary 383/29, world-region-data 414/30, alchemy-data 429/39, magic-tower 473/34,
sprite-equipment 492/53, companions 508/53, shop 562/37, battle-effects 563/45, sprite-catalog 629/69,
sprite-creatures 648/78, battle-stage 666/31, boss-intros 716/68, music-composer 788/46, map-terrain 893/62,
encounter-engine 1,023/55, forge-art 1,046/114, map-regions 1,062/76, guild 1,069/84, world-engine 1,128/74,
sprite-humanoid 1,128/136, music 1,225/72, scene-banners 1,418/111, map 1,469/107, forge-engine 1,884/155, music-themes
2,521/124, app 5,956/355, combat 7,041/549, continent-shape 11/255.

Content counts in one place: 9 playable races (28 in all); 130 classes, 738 class levels, 142 skills, 127 passives;
388 spells (12 elements, tiers 1–10); 174 enemies, 12 bosses, 17 AI profiles, 11 boss mechanics, 6 phase
transitions; 85 artifacts; 71 consumables (50 potions, 12 poisons, 8 traps, Camping Supplies); 5 key items; 24 runes;
56 ingredients, 62 recipes; 37 masterwork/Exotic forge definitions + 5 masterwork artifacts; 20 regions (17 charted),
27 settlements, 3 sites, 15 guild halls; 55 music themes; 19 boss cutscenes, 12 Tier 10 vignettes; 13 interior
scenes; 33 battle biomes; 39 screens (section 3); about 320 effect types in the data.

---

## 6. Questions a port planner would ask that the code can't answer

Where `engine/Docs/Engine-Plan.md` already records the question as open or decided, that is said; the rest are new.

**Scope and order**
1. Is the faithful port done first and the approved reworks (task 10 classes, 12 spells, 13 new classes, 14 dungeons)
   after it, or are they built while porting? (Open in Engine-Plan since 2026-11-05.) It decides whether the 130 old
   classes, 388 old spells and the enemies' class levels and spell lists are ported at all, and when the enemy revamp
   happens.
2. What does "faithful" cover: the rules and numbers only, or also the screen structure (the character sheet as the
   hub with modal windows over it, the map as a window, the combat view over the sheet)? Does the port keep the
   one-page, many-modals structure or move to a different game flow?
3. Which presentation is remade in 3D and which stays 2D (drawn or as images): battle figures (72 playable looks, 174
   enemies; the plan names Blender remakes as an open question), battle backgrounds (33 biomes), the 19 boss
   cutscenes, the 13 interiors, item portraits and icons (forged parts, 49 artifact shapes, runes, potions,
   ingredients), the map (3D per W1), the Tier 10 vignettes (to be reworked, SP47)? Is there an interim stage where the
   old SVG looks are reproduced before the remakes, as the UI and music were reproduced exactly?
4. Is the battle itself 3D (camera, formations, up to 8 a side on the stage, up to 50 enemies in a roster)? Are both
   battle views kept (the animated stage and the classic card view)?
5. The three uncharted regions (W27): made during the faithful port, with the 3D map, or after?

**Player-facing behaviour**
6. Dev Mode: kept in the shipped game? (It is on by default for a new character in the old game and unlocks
   attribute, purse and EXP editing, catalogue adds, the Combat test screen and Force encounter.) Or does it move to
   the editor or a debug build?
7. Saves: must the port load old saves (the localStorage blob or exported `emberfall-stat-block` v22 files) and import
   old character files as companions? If yes, which of the old migrations must survive (`LEGACY_ITEMS`,
   `RETIRED_ARTIFACT_IDS`, renamed enemies, `mapScale`, `continentVersion`, retired quest kinds, Ornn's hunt)? One
   character and one slot as now, or several? Is a battle in progress still saved and resumed?
8. The real-time clock: keep time passing while the player sits on the sheet (0.5 game minutes per second) and the
   36 minutes per second of travel? Pause when the window loses focus (the old game stops on `document.hidden`)?
9. Which browser-era conveniences stay: the "Refresh file" / "Choose …json" buttons for live data reloading, the
   drawer below 840 px, Escape and Tab-trap behaviour in every window, the 31 map shortcuts, the native File menu
   (Ctrl+S / Ctrl+O)?
10. Are companions still commanded by the player in battle (no option for AI control)? Is the three-companion limit
    kept?
11. Which settings stay per character (theme and Dev Mode are saved with the character today) and which become game
    settings? Is the light (parchment) theme kept, or only dark (the user plays dark)?
12. Target window sizes and UI scale (the old window is 1440×950, minimum 820×600; the engine's references are at 1×
    and 1.65×).
13. Sound effects: which ones (open at port time in Engine-Plan; the old game has none)?
14. Localisation: all text is English, inline in scripts and markup; is a string table wanted?

**Data and rules**
15. Which data embedded in scripts becomes pack records editable by mods and the editor (M12-9's
    `Statventure.Game.Data`): encounter tables, forge tables (makers, materials, weapon and armor types, runes,
    masterworks), region geometry and zones, the continent's land field, alchemy content, the Great Hunt list, shop
    sizes, scene and sprite looks, AI profiles, boss mechanic parameters? Which stays code?
16. Randomness: the old game uses unseeded `Math.random`. Should the port seed gameplay randomness (for replays and
    tests), and must it reproduce the old game's sequence of draws anywhere (as the music port reproduces the
    composer's mulberry32)?
17. How are the old checks used: ported one by one as C# tests (49 check files; Notes' last regressions counted
    158–161 runnable entries, plus the smoke, combat, enemies and lifecycle suites), or is the old game run as an
    oracle the way the UI and audio references are?
18. The 11 known failing or stale old checks (the Barbarian legacy bonus choice that stops the combat suite,
    `bloody-mary`'s crit cap, `settings-companions`, `shop`, `music`, `newc`, `trap`, …): which behaviour is right for
    the port?
19. Dead or legacy parts: confirm they are left out: music.js's old score engine (the engine already dropped it),
    `#anatemis-cinematic` and `#boss-special-cinematic`, the region transition prompt, `MapInk.finish`, the
    weapon/armor record parsers and empty `itemCatalog.weapons/armor`, the 43 MP3 exports, `classes/newc/`,
    `RACE_FALLBACKS` in the sprite catalogue (every enemy has its own look now).
20. Balance: the boss simulators live only in `~/.cache/stat-fick-build/claude-work/` on the user's PC. Is balance
    re-checked in the port (for the faithful bosses, and again after the class, spell and enemy reworks), and with
    which tool?
21. Instant death on bosses, spells whose "ignores Armor" text does nothing, the Goliath/Human HP imbalance at low
    stats (Notes): fixed in the faithful port, or left to the reworks?
22. Performance targets: the AI forecasts thousands of outcomes per decision and the map paints per pixel on the main
    thread; are there frame-time or turn-time budgets the port must meet?

# Old game notes

These notes were kept in Claude's local memory while the old game was developed (2026-10-06 to
2026-10-16) and moved into the repo on 2026-10-30, when the project went to GitHub. They describe the
JavaScript game in this folder: design decisions, systems, tests and gotchas. Paths are the repo's
(`old-game/` was `/run/media/system/M.2_2TB/Stats`). Superseded since: the game is in git now (the
"no git" decision below is from 2026-10-06), and the headless test client's source is in
`test-client/` (run it with `engine/Tools/Scripts/chromium.sh old-game/test-client --suite=smoke`;
no need to unpack `app.asar` any more). `claude-work/` means `~/.cache/stat-fick-build/claude-work/`
on the original PC (backups and helper scripts, not in the repo).

The user's main project is **Stat Fick**, a vanilla-JS fantasy RPG (character sheet, 18-region map, turn-based combat, guild, companions) with an Electron desktop shell. It lives at `old-game`.

**User decisions (2026-10-06):**
- No git. The user declined it.
- The game is played only through `Stat Fick.AppImage` and `Stat Fick.exe`. The browser launchers and all README files were removed on request.

**Executables use the live files:** Since 2026-10-06, the game executables bundle no game data. They read the files in their own folder, and a missing file returns 404 with no fallback. `desktop/main.cjs` checks for the game files at startup.
- **`objects/weapons.json` was deleted** at the user's request on 2026-10-16, done the same way as `armor.json`.
  - `app.js` no longer loads, caches or offers a file chooser for weapons. `ITEM_FILES`, `ITEM_CACHE_KEYS` and `itemSourceStates` hold only artifacts, and the old `emberfall-weapons-cache` localStorage entry is removed at startup.
  - `itemCatalog.weapons` stays an empty array, and the weapon record parser is left in place unused.
  - `index.html` lost the weapons file button and input.
  - `desktop/main.cjs` `requiredGameFiles` now lists `objects/artifacts.json` instead of `objects/weapons.json`. The executables refuse to start if a listed file is missing, so both were rebuilt (`npm run build:linux` / `build:windows` in `desktop/`, offline from the cached Electron) and copied into the game folder.
  - The test-client chain patches `headless-main.cjs` the same way (`weapons_file` in `patch_an.py`). The smoke check was relaxed to `weapons >= 0`.
  - Backups, including the old executables, are in `claude-work/weaponsjson-backup/` (deleted 2026-11-05 in the user's cleanup). The regression afterwards stayed at the 28/38 baseline.

**Shortcuts and icons:**
- `Stat Fick.desktop` lives in `objects/` at the user's request. Its paths are absolute.
- Dolphin shows AppImage icons through a per-user thumbnailer Claude added on 2026-10-06: `~/.local/bin/appimage-thumbnailer` plus `~/.local/share/thumbnailers/appimage.thumbnailer`. It reads the embedded `.DirIcon` with unsquashfs and never runs the AppImage.
- The AppImage icon comes from `desktop/icons/icon.png`, the same design as the `.exe`'s `icon.ico`.

**Headless tests:** `tests/Stat Fick Tests.AppImage` is the test client for Claude to use.
- It always tests the live folder above `tests/`, or the folder given with `--game-folder`. Run it with `--suite=smoke|combat|enemies|lifecycle|all`.
- Its sources and checks live only inside its `app.asar`. To change them, extract with `asar` and rebuild with electron-builder `--projectDir`. Its package.json carries the build config.

**Building:** There's no Node on PATH. Use `~/.cache/stat-fick-build/node-v24.21.0-linux-x64/bin`. The Electron 44.5.0 zips are cached, so builds work offline. Windows portable builds work on Linux without Wine (via resedit). The `.exe` can't be run here.

**Animated battle UI (added 2026-10-06):**
- Sprites are code-drawn SVG: `sprite-kit`, `sprite-equipment`, `sprite-humanoid`, `sprite-creatures` and `sprite-catalog` (48 playable looks, all 159 enemies).
- The battlefield is split across `battle-scenery`, `battle-effects` and `battle-stage`.
- `combat.js` only *reports* events to `window.BattleStage` through `stage()`/`stageEquipment()`. Its rules are unchanged.
- Settings `battleUi` (`stage`|`classic`) and `battleSpeed` live in `settings.js`. The player's look is the character field `appearance {gender, variant}`.

**Ink sprite style (2026-10-07):** The user asked for sprites close to the Darkest Dungeon art style, so every rig now uses it.
- Palette: `K.tone` mutes all colours. A `"!"` prefix opts a colour out, which glows, eyes and fire need.
- Shading: solid INK shadows clipped to each shape via `K.clipped` (clip paths are collected between `K.beginInk()` and `K.endInk()`), plus `K.hatch` strokes trimmed to their box.
- `.sp-ink`/`.sp-inked` get a contour drop-shadow in `styles.css`.
- Humanoids use realistic proportions. Catalogue `build` values are read as ratios of the old chibi defaults.
- Name plates sit above every figure: actors set `--depth`, not a z-index.
- The battlefield backgrounds are still the bright original scenery. Restyling them was offered, not requested.

**Map rework (2026-10-10, user request "rework all maps, each region way bigger, same layout, more detailed and seamless; the Stoneveils are one massive mountain"):** supersedes the SVG landscapes below (old `map.js` backed up in `claude-work/maps-backup/`).
- Scale: regions are 3× larger (`UNITS_PER_KM` 40/3, `MAP_SCALE` 3 in `world-engine.js`; map stays 1600×1000 units). The travel clock runs 3× faster (36 game min per real second), encounters every `ENCOUNTER_MINUTES` 90 travel minutes. Save migration `mapScale` triples saved km. Boats 540 min; Traveler 600 km, Cartographer 1500 km, Pilgrim 60 km thresholds. User chose all four options (3×, faster clock, same encounters per crossing, all three Stoneveils as one mountain).
- `scripts/map-terrain.js` (`MapTerrain`): field grid (2-unit cells) built in slices, canvas tiles at 1/2/4/8 px per unit (LRU), per-pixel hillshade, hatching, contours, water, micro relief, field patchwork (`spec.fields`), canopy (`MapTerrain.canopy`, `spec.canopyFrom` hides glyph trees), `spec.paint(px)` hook, Worley `cells`, features with LOD (`KEEP`) and `REACH` for big ones.
- `scripts/map-regions.js` (`MapRegions.spec(id)`): one spec per region (17). Shared `mountain()` height in a `FRAME` so Foothills/High/Summit join seamlessly; `mountainRelief` shared hillshade.
- `map.js` view: camera = SVG viewBox over a canvas; zoom 1–10× of fit, Follow (Y), wheel zoom, drag pan. Tests must read `#regional-map.viewBox`, not its bounding rect.
- Region roads (exits) are numbered destinations like towns (after towns and sites, arrow + destination name) with a waymarker on the map (`exitArtFor`, `data-map-exit`); clicking travels to `exit.point`, arrival raises the crossing prompt. Their labels hide at overview zoom.
- Night lanterns fade per element via CSS `--lantern`; a translucent SVG group over the terrain canvas composites as a dark rectangle in this Chromium.
- Test client patches for this: `claude-work/patch_maps.py`. Screenshot tool: `claude-work/runmap.sh <ids> [spots-json]` (fit/mid/close per region).

**Ink region maps (2026-10-07):** `scripts/map-ink.js` (loaded before `map.js`) finishes every region landscape.
- `MapInk.finish(markup, region)` tones the colours, feathers zone ellipses into washes, scatters terrain symbols and lays a canvas-made parchment sheet (multiply) under the lettering. It also replaces the compass, scale and frame.
- Symbols are `<use href="#ink-*">` defs, placed per region in `DETAILS`. Clearance checks each symbol's whole footprint against land, roads, rivers, towns, labels and the compass.
- `data-vivid` on a tag keeps its colour (lava, windows).
- Towns and castles are drawn by `castleArt`/`villageArt` in `map.js`. Night lanterns light those buildings' windows.

**Gothic sheet theme (2026-10-08):** The block at the end of `styles.css` headed "Gothic ink theme" restyles the whole UI. It overrides the earlier "Adventurer's ledger" tokens.
- Light mode is parchment; dark mode is stone. The user plays in dark mode.
- Its rules are prefixed `:root` to win over earlier rules.
- Element and rarity accents must keep their `border-left` stripes.

**Remastered music (2026-10-08):** Region themes and battle themes are synthesised live by `scripts/music-composer.js`. The scores live in `scripts/music-themes.js`, which uses scale-degree notation (the header documents it). Both are loaded before `music.js`.
- Every normal battle plays `combat@<regionId>`, the region's arrangement of the battle theme.
- `Music.state().theme` stays "combat"; `.variation` holds the region.
- Boss themes still use the old engine in `music.js`. Boss music remasters were deferred by the user.
- The `LOUDNESS` table in `music-themes.js` holds per-theme trims measured with ffmpeg EBU R128: regions about −17.5 LUFS, battles about −17.
- `Music.lint()` lists melody notes that clash with their chord on a strong beat.
- The MP3s in `music/` are offline exports only; the game never loads them.

**Item rarity colours (2026-10-08):** `itemRarityTag()` in `app.js` renders a gem tag. It appears in inventory entries, equipment slots, the item picker, item details and shop card headings. The "Item rarity" block in `styles.css` sets `--rarity` (vivid) and `--rarity-ink` (readable text, per theme) for each `.item-rarity-*`/`.rarity-*`. The older `--spell-accent` values stay because tests check them.

**Shop and guild banners (2026-10-08):** `scripts/scene-banners.js` (loaded before `shop.js`) draws ink interiors with SpriteKit.
- `SceneBanners.show(host, kind, place, regionId)` fills `#shop-banner` and `#guild-banner`.
- Castle cities get a big variant: great forge, emporium, arcane athenaeum, and a perspective great hall for the guild. Towns get small rooms; a town's reduced guild is a tavern.
- Scenes are seeded by settlement. Region picks the materials; game time picks day/dusk/night windows.
- Each banner is a static SVG (scene, masked darkness with light holes, hatching) plus an `.sb-live` screen-blended SVG of animated flames and glows. Emitted things always draw above the scene, so keep fires clear of anything that should stand in front of them.

**Item rework, phase 1: forged weapons (2026-10-08).** The user plans further phases: spell staffs and wands, masterwork items and Mythilium, then armor.
- Rules live in `scripts/forge-engine.js` (`ForgeRules`): 8 makers, 38 materials, 21 weapon types and 24 runes. It is pure and runs in Node, which makes unit tests easy.
- A weapon's whole recipe is its id: `weapon-forged-<type>-<level>-<main>-<guard>-<third>-s<slots><runes>`. Each part is a 2-letter maker code plus a 3-letter material code.
- `getItem()` falls back to `ForgeRules.item(id)`. Slotting a rune changes the id, so code replaces the inventory entry.
- Standard catalogue weapons carry `forgedAs` (and `shopAvailable: false`) in `objects/weapons.json`. Enemies' copies become forged weapons at spawn (`armEnemy` in `combat.js`). Old saves keep working.
- Art and logos are in `scripts/forge-art.js` (`ForgeArt`). The Magic Tower (castle cities, map key W) is in `scripts/magic-tower.js`.
- Enemies drop each non-unique equipment piece with a 40% chance.

**Item rework, phase 2: spell staffs and wands (2026-10-08).** These are forged too: a jewel (staff) or gem (wand), a handle and an inscription.
- IDs look like `weapon-forged-staff-<lvl>-<mk><stone><tier>-<mk><handle>-<mk><ink>`.
- Effects become `spell-cost-percent` and `spell-power-percent` entries with `categories`, `element` and `excludeElement`, read by `weaponSpellEffectApplies` in `app.js`.
- The user decided:
  - Rarity comes from the handle.
  - Jewel tier odds pass through 85/13/2 at level 1, 47/45/8 at 35, 15/75/10 at 50, 8/47/45 at 70 and 3/17/80 at 100, changing gradually in between.
  - Vampiric changes attack-spell cost by 8% (more), and blood attack spells cost 8% less.
  - Offensive and Attack mean Damage spells; Support excludes summons.
  - Abyss's effect applies through its jewel or gem.
  - Vampiric parts follow the Outlaw rule: only on vampires and in Nocturne Keep, and Vampiric inscriptions only with a Vampiric jewel or gem.
  - About 25% of generated weapons are casters (`casterShare`).
- The user wants gaps in their specs reported, not improvised.

**Item rework, phase 3: forged armor (2026-10-08).** Armor and shields are forged too: 12 types (3 each of heavy, light, clothing and shield), each built from a main part plus 2 secondary parts.
- IDs look like `armor-forged-<type>-<lvl>-<mk><mat>×3-s<0-2><runes>`. The rules are the `ARMOR*` tables and `generateArmor` in `forge-engine.js`.
- Armor has at most 2 runes. Elemental runes give +25% resistance, and several may share one piece. Other runes have defensive versions (Heft, Precision, Vital Hit, Empower); scaling runes swap the armor's scaling stat.
- The user decided:
  - Keep the armor formula (power × 100/(100 + 2·armor)). Spells ignore armor.
  - Hit chance = Accuracy/(Accuracy + Evasion), with no floor. Ranged attacks face half Evasion.
  - Makers per category as proposed. No new makers.
  - Pacing target: armored fights end about 1.2× faster than before the weapon rework.
- Calibration model: `claude-work/pacecheck.cjs`. It compares legacy weapons and armor (backup `armor.before-phase3.json`) with forged ones by level.
  - This made armor high from level 1, with growth coming mainly from the scaling stat.
  - Natural armor for unarmored enemies is 20, plus up to 40 more around level 8. A level-growing curve would have made late beast fights 2–3× slower than before the rework.
- Art: `ForgeArt.drawArmor` draws the portraits. Battle sprites reuse `armorLayers` with `forged-<mat>-<maker>` palettes registered in `K.MATERIALS`.

**Item rework, phase 4: shops (2026-10-08).**
- The Spell Shop is gone. Spells are sold only by the Magic Tower in castle cities. `stockFor(settlement).spells` is that list, and companions learn from it.
- Blacksmith stock: castle 5 forged weapons + 5 armor, town 3 + 4. Staffs and wands are sold only at the tower (6 per castle), though the smith still buys them back.
- Runes are inventory items `rune-<id>` (category "rune", own inventory tab). The tower always stocks every rune. Fitting costs 25% of the rune's price. "Remove all runes" costs 3× each rune's price and returns the runes to the pack.
- Layout: `shop.js` exposes `ShopLayout`. Buying shows the list on the right and the hovered entry's details on the left; selling mirrors that. The tower reuses it for its five menus, and its fitting menu is a magic circle.
- `scene-banners.js` draws full-window interiors (1280×900, `xMidYMin slice`). Keep faces and counters in y≈130–360, above the panels. Emitted fire and light draw over everything, so keep people clear of them.
- `itemDetailMarkup` and `spellDetailMarkup` in `app.js` return markup that the shops reuse.

**Catalogue clean-up (2026-10-09):**
- The 160 old catalogue weapons and armor pieces (`forgedAs`) and 8 enemy-specific named pieces were deleted. `weapons.json` now holds 17 masterworks plus Beast Claws and Vulcanic Ram Horns; `armor.json` holds the 8 masterworks.
- Enemy files reference placeholders such as `forge-weapon-<1|2>-<type>+<type>` and `forge-armor-<type>`. `ForgeRules.item` resolves them, and `armCharacter` forges real pieces at spawn.
- `ForgeRules.LEGACY_ITEMS` maps every deleted id to its placeholder. `migrateLegacyItems` (called in `normalizeCharacter`) converts old saves deterministically to forged pieces at the character's level.
- `Masterwork Items.txt` in the game folder lists every masterwork plus the 8 retired named pieces. The user wants those recorded for later use.
- Backups of the old data, the enemy files and the original test client are in `claude-work/cleanup-backup/` (deleted 2026-11-05 in the user's cleanup).
- The test client was patched with `claude-work/patch-testclient.py` and repacked with the original runtime (first 188392 bytes) plus `mksquashfs -comp gzip -b 131072 -no-fragments -no-xattrs -root-owned`.

**Item rework, phase 5: masterworks and natural weapons (2026-10-09).**
- Unique items are gone; their drop rules now apply to masterwork weapons, armor and artifacts (`isMasterworkEquipment`). The class perk is now `masterwork-drop-chance-flat`. Enemies still have their own `unique` (boss) flag.
- Masterworks are forged recipes (`ForgeRules.MASTERWORKS`, ids `weapon-masterwork-<key>-…`):
  - Mythilium main part; each secondary part is Mythilium 25% of the time.
  - Masterwork-only prefixes: Flawless, Exalted, Rending, Unerring, Fateful.
  - Forged at the player's level, from drops or the shop.
  - Templates (`masterwork-<key>`) sit in drop tables and in `items`.
  - Done so far: Stunted Sword (goblins, 5%) and Bloody Mary (Ravenwatch, 5 Blood Crystals). The user is leaving the boss masterworks for later.
  - A masterwork may fix its main part's maker with `mainMaker`, even a maker that never forges that shape otherwise. The Stunted Sword's is always Outlaw and Bloody Mary's always Boreal Bolt (user decision, 2026-10-09). The other two parts vary but are never Outlaw; the user chose that over the usual Outlaw-main rule. `decodeMasterwork` enforces this. `fixedMasterworkId` repairs old saves inside `migrateLegacyItems`. `STOCK_VERSION` is 24.
- Natural weapons (`ForgeRules.NATURAL_WEAPONS`) replaced the Beast Claws, Ram Horns and Stardragon Claws items.
  - Enemy files carry `naturalWeapon`; the Beast class grants claws.
  - Attacks cost 2 AP and scale like an uncommon forged two-handed weapon of the attacker's level. Anatemis's claws scale like a Level 100 masterwork.
- The user deleted `Masterwork Items.txt`, so don't recreate it unasked.
- The test client was patched again via `claude-work/patch_phase5.py`, imported by `patch-testclient.py`. The baseline is now 28/38; the stale checks are bloody-mary, settings-companions and adventure-improvements.

**Class rework for forged items (2026-10-09):** the user asked to "slightly rework" class features built for the old item system. Done in 54 class files (84 effects); the user wasn't asked about the conversion rule, so report it if they question numbers.
- Rule: flat hit chance, Evasion and Armor became percentages at twice the number, capped at ±50% (+10 hit → Accuracy +20%; Evasion +30 → +50%). Flat weapon power +2 → +5% (Great Beast claws +5 → +10%). `weapon-scaling` 0.1 → 0.2, because forged ratios are about double the old ones. Crit numbers were kept (they are already Crit Rating); only the wording changed.
- Warrior's dead heavy-armor evasion debuff became `heavy-armor-evasion-penalty-reduction` 20: heavy armor's Evasion penalty is 20% smaller, for both forged `evasionPercent` and old flat `evasionDebuff`.
- New effect types in `app.js`/`combat.js`: `conditional-defense-percent`, `fencer-weapon-hit-percent`, `fencer-evasion-percent`, `unarmed-hit-percent`, `quest-`/`journey-party-`/`artifact-count-defense-percent`. Marks use `hitPercent`, with skill-upgrade `markHitPercent`. Double Down uses `hitPercent`, Noxious Coating `hitPercentPenalty`, Cursed Edge `armorPercent`. Hostile auras accept `defense-percent`. Old flat types still work, because spells (23 `defense-flat`) and artifacts (5 `weapon-hit-flat`) were left unchanged.
- Accuracy percentages now apply after the 70 floor, in `calculateWeaponHitChance` and for unarmed attacks.
- The class files are hand-formatted in several styles. Edit them with the span-preserving `claude-work/class-rework.py` approach, never `json.dump`. Backup: `claude-work/classrework-backup/`. Check script: `claude-work/class-check.js`.

**Boss rework 1: Great Imp Ji'ruan (2026-10-10).** The user is reworking the bosses one at a time, each together with its masterwork drop.
- **Ji'ruan's file:** Level 18, `maxHpPercent` 30 and `maxManaPercent` 50, with no Raid Boss class (user decision).
  - He keeps Summoner 7, whose Overprotective passive makes him untargetable while any of his summons lives; the user chose that as his protection.
  - Dark-only spell book. Stats raised to boss level (total 408), speed 46.
- **Weapon:** placeholders can fix a rarity, as in `forge-weapon-1-rapier~rare`.
- **AI:** enemy files can name an `aiProfile`, implemented in `AI_PROFILES` in `combat.js`. The `jiruan` profile:
  - Turn 1: Double Summon plus Imp Retinue twice, giving 4 Imps.
  - Later: rebuilds when no Imp is left, or when one is left and he is below 40% HP.
  - After a mid-fight Retinue he must wait a turn before the next; meanwhile he can only summon a Lesser Imp.
  - He skips Guard and Mana Overload.
- **Imp Retinue:** now summons the new Level 12 Void Imp (`enemies/Void Imp.json`) instead of Lesser Imps. Its other numbers are unchanged.
- **Balance:** simulated with `claude-work/jr-sim.js` via `run-sim.sh <runs> <standard|prepared> <tag>`, which plays Level 20 parties with the game's AI on both sides.
  - Final result: standard parties win about 88% and lose about 1.1 members on average; prepared parties win about 94% and lose about 0.25.
- **Look:** new rig options in `sprite-humanoid.js`: horns `jagged-crown`, wings `decaying`, `voidVeins`, `plates` and `goatBuild: "brute"`. The Void Imp has its own catalogue look.
- **Theme:** `great-imp-jiruan` in `music-themes.js` (the `bosses` list). It replaces his old score. Its loudness trim is 2.08, measured in-page with BS.1770 against `combat@alder-vale`.
- **Impshield:** now masterwork armor, `armor-masterwork-impshield-…`, a Kite Shield whose main maker is always Abyss.
  - Effects: −15% damage from Impids and Demons, and `absorb-element-spell-chance` (5% for dark spells; the spell has no effect and the wearer regains its Mana cost).
  - The old `armor-impshield-shield` is migrated through `LEGACY_ITEMS`.
- **Test client:** `jiruan-rework-browser.js` was rewritten; `patch_jiruan.py` is imported by `patch-testclient.py`.
- **Boss intro cutscenes (2026-10-10):** `scripts/boss-intros.js` (`BossIntros`), loaded before `battle-scenery.js`.
  - An enemy file's `introCinematic` names the intro (Ji'ruan: `"jiruan"`).
  - `playBossIntro` in `combat.js` plays it once when an encounter starts, never on restore. It is skipped when combat animations are off.
  - The AI waits through `bossIntroUntil`; Skip and Escape end it.
  - Markup: `#boss-intro-cinematic` in `index.html`. Styles: the "Boss intro cutscenes" block in `styles.css`.
  - Each intro draws its own SVG scene around the boss's real battle sprite, plus a title card.
  - To capture frames, pause `getAnimations({ subtree: true })` and set `currentTime`.

**Boss rework 2: Juila the Grand Forest Matriarch (2026-10-10).** Her name was corrected from "Matriach" on 2026-10-11 (user request); IDs keep the old spelling (`juila-grand-forest-matriach…`). The enemy file lists `formerNames` (encounter rosters resolve them), `WorldRules.RENAMED_ENEMIES` migrates saved world state on load, and `Combat.restore` renames resumed actors. User decisions: cycle of seasons; harder raid fight (Raid Boss class kept, aimed at prepared Level 25+ parties); Branch of the Forest deleted (she carries `forge-weapon-2-staff~rare@nature`; old copies migrate to an epic Nature staff via `LEGACY_ITEMS`); ethereal choir-and-harp theme; tree-mother look with seasonal foliage; seasons are pure threat; seasonal spirits; intro cutscene; then "Longer fight" pacing. The Great Forest Plate became **Rootbark** (user-approved design and name): masterwork Scale Armor, main part always Elven, `resist: { nature: 15 }` on the masterwork def (merged into the forged resistances), effect `turning-seasons` (per-wearer cycle every 2 of the wearer's turns: Spring 3% HP regen, Summer +10% damage, Autumn `melee-retaliation-percent` 15, Winter −15% damage taken; `wearerSeasonTurn` in combat.js), 8% from Juila; old plates migrate via `LEGACY_ITEMS`. Juila wears no armor (keeps her tree-mother sprite) and has `baseArmor` 37 instead.
- Seasons: enemy-file `seasonCycle` {turns: 2, seasons[{id, name, spirit, omen, description, allies[], foes[], roots?}]} driven by `seasonTurn` in `combat.js` (start of every real turn; timed effects flagged `season`, removed on change; spirit summoned per season and faded when it ends; foretold a turn ahead; Winter `rootedAp`; all end when she dies). Snapshot `season` drives the stage badge and drift overlay ("Battle seasons" in styles.css); `stageLook` adds `season` and the catalogue entry's `seasons` palette swaps her colours.
- Roster: Juila + two **Grove Matriarchs** (new court variant, `aiProfile` "juila-court"; regular Dryad Matriarchs untouched). Her AI (`juila`, via `oneStrikeATurn`): one damage spell a turn, skips Quickened Spell, Mana Overload and Call of the Ancient Pack. Her spell-power aura was removed.
- Spirits: Blossom Sprite, Sunhorn Stag (new quadruped `type: "stag"`, natural weapon `sunhorn-antlers`), Harvest Wisp, Rime Wolf. Rig additions: horns `great-antlers` (moss, blossoms, frost) and lower `trunk`.
- **Wheel of the Year** (2026-10-11, user asked for a unique spell that makes combat more interesting): `wheel-of-the-year-nature-unique`, Juila only. Spell records may carry `seasonVariants` {season: {name, effects}} (kept by the app.js spell loader; `seasonalSpell` in `cast()` picks the battle's season). Spring Bloom heals her side and cleanses; Summer Blaze hits every hero (counts as her one strike that turn); Autumn Harvest −10% all stats 2 turns; Winter Hoarfrost `root-ap` 1 on every hero (new effect type, reuses `rootedAp`). Her AI casts it once per season, first thing in the season (`battle.season.wheelIndex`).
- Forge templates accept `@element` for staffs/wands (jewel + ink of that element). Intros name their `retinue` and are themed by `data-boss`.
- Balance (simulator `claude-work/ju-sim.js` via `run-jusim.sh <runs> <standard|prepared> <tag> [level]`; batches of 16 are noisy, use 32+): final (with the Wheel, maxHpPercent 210) prepared Level 25 about 47% wins, 3 deaths, about 6 rounds (the user chose a 6–8 round war of attrition); standard parties lose. Final sheet: Wis 34, Mind 38, Dex 44, Speed 42, maxHpPercent 210, turnStartManaPercent 10, Autumn 2%. The sim's level-28 parties are unreliable (extra class levels exceed caps).
- Test check: `claude-work/juila-rework-browser.js` via `patch_juila.py`.

**Boss rework 3: Nygheir the Wounded (2026-10-11).** User decisions: Old Wounds mechanic; harder raid fight like Juila (prepared party a few levels above, Raid Boss class); keep phase 2 (25% → heal 40%, earth caster with dragonbreath); phase-2 cutscene "Old wounds remember" built around his real sprite; full sprite redesign; mournful-to-furious music; drops unchanged (vial, Second Wind 12%); both a battle-start intro and the phase cutscene.
- Old Wounds: enemy-file `oldWounds` {count 3, bleedPercent 2, damageTakenPercent 10, rageDamagePercent 12, burstAtPhase 1}. A critical hit tears one open (`tearOldWound`, from `strike`): +10% damage taken and 2%/turn bleed per wound, +12% claw damage (rage drives claws only, not spells). At phase 1 they burst (`burstOldWounds`): all 3 open as "Burning Scars", no bleed. Unique spell `stonecrust-earth-unique` (effect `seal-wound`) closes one and heals 5%.
- AI `nygheir`: at most 2 damaging actions a turn and an all-enemies spell counts as both (`oneStrikeATurn(..., limit, areaWeight)`); after the phase, one strike a turn and no party-wide spell two turns running; Stonecrust when ≥2 open (or 1 and <50% HP).
- Phases can carry `unskippable: true` (crossing the threshold starts the phase at once via `phaseDamageFloor`; Anatemis keeps her id check) and `cinematic: "<BossIntros key>"` (played by `playPhaseIntro` through `#boss-intro-cinematic`, with `render(..., { before })` showing the pre-change look). The old `#nygheir-cinematic` markup/CSS/JS was removed (Anatemis's progress bar now uses `bi-progress`).
- Balance (`claude-work/ny-sim.js` via `run-nysim.sh`, default level 26): his "dragon" race adds +2 to every stat per level (+44), so stats in his file are low: Str 30, Int 20, Speed 0 plus `permanentBonuses.speed` −25 (effective ~56), Guardian 5 replaced Rogue 5, maxHpPercent 75, phase-2 statBonuses int/wis +10, defenseBonuses armor +20 only. Result: Level 26 (boss +3, like Juila at 22→25) prepared ~36–47% wins, standard ~44%, ~6 rounds; Level 25 ~6%, Level 24 0% (steep cliff, also in the sim's party scaling).
- Look: dragon rig options `plates` (back slabs + scutes), `hornCrown`, `relics` (spears, banner, sword, arrows), `tattered` wings (SVG mask holes), `scarredEye`, `wounds`/`openWounds`, `burning`, `legW`, `headLift`, `headReach`, `neckW`. Catalogue entries may have `phases: {1: {...}}`; `stageLook` adds `phase` and `openWounds`.
- Music: composer themes `nygheir-the-wounded` (duduk lament, 64 bpm aeolian) and `nygheir-the-scarred` (same motive, 132 bpm harmonic minor); `musicTheme` switches on `phaseIndex`, `startNextPhase` calls `Music.sync()`; trims 2.92 / 1.31 (≈ −17.8 / −16.7 LUFS). `desiredTheme` falls back to "combat" if a theme is missing.
- Test check: `claude-work/nygheir-rework-browser.js` via `patch_nygheir.py`. Regression after the rework: unchanged 28/38 (same 10 failures), smoke/enemies/lifecycle pass. Checks that drive enemy turns must set `BattleStage.enabled = () => false`, or `enemyAction` waits on stage animations.

**Boss rework 4: Zik Lun, the armored Hulk (2026-10-11).** User decisions: Warband chief (orders + vengeance + war horn), regular Great Hunt (no Raid Boss), his own troll guard (Troll Shieldbearer + Troll War-drummer, new Level 20 files), war horn calls Hill Trolls on a cooldown, if he falls the rest lose heart, intro only (no phase), troll-warlord look, remastered siege-march; Puny Smasher and Great Steel-Plated Warsuit deleted and replaced by generic forged gear (old copies migrate via `LEGACY_ITEMS` to `~epic` Greathammer/Full Plate); Giantslayer kept at 5% and to be reworked last after asking the user.
- Enemy-file `warband` {guard, drummer, drumPercent, orders{charge, hold, regroup}, vengeance, loseHeart, hornEveryTurns}; engine `warbandTurn` in `combat.js` (start of every turn): an order at the start of each of his turns (Charge! marks the lowest-HP hero as prey: warband +damage, prey takes more; Hold the Line! armor + `guardian-ward` so the Shieldbearer takes every single-target blow meant for him; Regroup! cleanses and heals, exempt from the 80% enemy-healing cut); a living drummer scales orders ×1.5; fallen warband trolls add Vengeance stacks; his death applies "Lost Heart". Snapshot `order` drives the stage badge (`.battle-order`) and an "order" banner. Warband AI focus: candidates on the prey ×1.6 score.
- AI `zik-lun`: one hammer blow a turn, War Horn (`war-horn-basic-unique`, summons `hill-troll-level-14`, maxActive 2) on his turn 2 and every 3 turns; `troll-guard` (Shieldbearer, one strike a turn; Guardian 5 keeps Interpose without Aegis Bond).
- Balance (`run-nysim.sh` with `ROSTER='["Zik Lun, the armored Hulk","Troll Shieldbearer","Troll War-drummer"]'`): standard Level 26 ~92% wins, Level 25 ~69%, Level 24 low (sim cliff), ~4 rounds. Final: Str 58, Vit 40 (his hammer scales on Vitality), maxHpPercent 240, guards maxHpPercent 90, Shieldbearer Str 44, Charge +10%, Vengeance +10% dmg / +6% speed per stack (max 5).
- Forge fix: armor placeholders now honor `~rarity` (`generateArmor({ rarity })`, used by `migrateLegacyItems` and `armCharacter`); before, only weapons did.
- Sprites: humanoid options `helm` (horned war-helm), `warpaint`, `sash`, `trophies`, `standard`, `drum`. BossIntros `retinue` may be a list (different guards).
- Music: composer theme `zik-lun-armored-hulk` (old melody transcribed, D harmonic minor with the Neapolitan), trim 1.47 (−17.2 LUFS).
- **Giantslayer** (user decisions, 2026-10-11): forged masterwork `masterwork-giantslayer` (Greatsword, main part always Roundtable), effect `damage-and-pierce-vs-larger-max-hp` in its growing form {stepPercent 20, amount 10, maxAmount 60, armorPiercePercent 20}: +10% damage per 20% more max HP than the wielder, up to +60%, pierce once at least one step. Zik drops it at 8%. Old `weapon-giantslayer-meele` migrates via `LEGACY_ITEMS`. Forged masterworks get a prefix in their name (e.g. "Exalted Giantslayer"); item panels use ForgeArt, and forged weapons are drawn from their forged parts in battle too (`sprite-equipment` `weapon()` prefers `ForgeArt.sprite`); the masterwork key (passed as `unique` by `ForgeArt.visualFor`) still selects the hand-made hit effects and the hand-made armor designs (keep keys equal to the old design keys or alias them in `SPRITE_UNIQUE`).
- Test check: `claude-work/zik-rework-browser.js` via `patch_zik.py` (also masterwork count 32 → 30, the map check's entourage, and the smoke suite's catalogue floor `weapons > 10 && armor > 5` → `> 0`, since `armor.json` is down to 5 pieces).

**Boss rework 5: Deep Sea King (2026-10-11).** First boss under the user's "bosses a bit harder from now on" rule (see the boss difficulty rule). User decisions: King's duel; harder raid fight (prepared Level 35 ~30–35%); alone; others "free but slowed"; challenges whoever hurt him most; duel timed, a fallen champion feeds him, a survivor is honored; no phase; full redesign; regal sea shanty; intro; Arrowtip Rain moved to every ordinary merfolk enemy at 1% (Spearfisher, Reed, Reef Guard, Steamcaller, Tidecaller); Royal Trident reworked last.
- Enemy-file `royalDuel` {durationTurns 2, outsideDamagePercent −50, triumph {healPercent 10, damagePercent 10, maxStacks 5}, honor [+15% attack, +15% spell power, +20% armor], honorTurns 2}. Engine in `combat.js`: `royalDuelTurn` (turn start: init, king dead, fallen champion → Triumph) and `royalDuelTurnEnd` (called from `endTurn`: duel timeout → honor, then a new challenge, telegraphed for his next 2 turns). `damageBy` ledger via `creditRoyalDuelDamage` in `applyCombatDamage`/`directDamage`; `opponents()` locks him to the champion (`royalDuelPartner`); `incomingDamageFactor` adds `royalDuelDamagePercent`. Snapshot `order` shows "Royal Duel · Champion X · n turns". AI `deep-sea-king`: never the Gladiator Duel skill; 1 strike a turn in a duel, 2 otherwise.
- Bug fixed: `checkOutcome` declared victory when the remaining enemy was in a protected duel with a companion (it used `opponents(player())`); now `hostiles(player())`.
- Royal Trident (user design): forged masterwork `masterwork-royal-trident`, spear, main part always Hellmade, `all-stats-on-damage` {amount 5, maxStacks 3, durationTurns 2} (stacks; a two-handed weapon in both hands now triggers once); King drops it at 12% and wields a concrete forged copy `weapon-masterwork-royal-trident-20-hmmyt-elque-dwman-s0` (forged at level 20, because a level-32 masterwork made him unbeatable), listed in `noDropItems`. Old `weapon-royal-trident-meele` migrates via `LEGACY_ITEMS`.
- Balance: Str 93 (unused by the spear), Dex 58, Vit 40, maxHpPercent 260 → prepared Level 35 ~34% (33/96), standard ~47%, ~5.5 rounds.
- Look: humanoid options outfit top `shell`, headwear `coral-crown`, `capeTrim`, `kelp`, `tailStyle: "royal"` (+`tailBand`, `bellyColor`), `outfitIsArmor` (hides worn armor). Intro `deep-sea-king` (sunset beach, rises from the waves; the boss sprite is clipped at the waterline with an animated `clip-path`). Music composer theme `deep-sea-king` (6/8 D dorian shanty, fiddle + brass), trim 2.0 (−17.4 LUFS).
- Tests: `claude-work/dsk-rework-checks.js` appended to `coast-lich-bosses-browser.js` by `patch_dsk.py`, which also moves the trident references in the coast, masterwork-rarity and guild-information checks to `masterwork-royal-trident` (12%).

**Boss rework 6: Tankina, Disciple of Talos (2026-10-11).** User decisions: "a step harder than the King" (prepared Level 47 ~25%); the gathering storm (anyone's lightning spell feeds it; only builds, never calms); keep both Apostles and Gorthok, tied in; phase kept at 50% with a reworked cutscene; full redesign; storm-ritual music, wild in phase 2; intro; Claw of Beowulf moved to every wolf (Juvenile, Adult, Highland, Dire, Duskpine, Boreal Direwolf) at 2%; a Church of Talos store opening after her defeat.
- Enemy-file `storm.stages` [{id, name, at (lightning casts), allies: effects}]: drizzle 0 (nothing), rain 2, storm 4, thunderstorm 7, tempest 10 (+28% attack and spell power, +20 speed, +20 armor). Engine in `combat.js` (`stormState`, `startStorm`, `feedStorm`, `stormTurn`, `applyStormTo`): every lightning `cast()` feeds it (+1, plus `storm-surge` effects); stage effects apply to her whole team (Gorthok too) as unremovable effects flagged `storm`. Shown through the snapshot `season` slot (`storm: true`) and `.battle-stage[data-season=drizzle…tempest]` rain/flash CSS; `stageLook` sets `look.storm`, and `sprite-catalog` `storms` variants crackle more.
- Apostle of Talos: `call-the-storm-lightning-unique` (Support, feeds the storm twice), AI `talos-apostle` (calls it on turn 2 and every 3 turns, one strike a turn). AI `tankina`: lightning ×1.5, no sweeping spell two turns running. Balance ~23% prepared Level 47 (22/96).
- Look: humanoid outfit `storm-plate`, headwear `storm-crown`, `lightningScars`, `staticSparks`. Intro `tankina` and phase cutscene `tankina-phase` (phase `cinematic`, through `playPhaseIntro`; `introRetinue(key)` shared helper). Music: composer themes `tankina-disciple-of-talos` (D phrygian war-chant, trim 1.75, −17.3 LUFS) and `tankina-chosen-of-talos` (D hijaz, 138 bpm, trim 1.22, −16.7 LUFS); `musicTheme` switches on her `phaseIndex`.
- **Storm Reliquary** (`church-of-talos.js`, modal `#church-store-modal`, map button `#map-church-store`, scene `church` = `stormChapel` in `scene-banners.js`): 6 Church of Talos lightning staffs/wands (`ForgeRules.generate({ maker, element })` — new `maker` option for casters), 8 lightning spells (10% chance of one over-tier), and the Stormhide Cuirass for 20 Platinum, always on offer. Stock lives in `shopState.locations["church-of-talos"]` {relics, tower, spells}, turns over with the shops' 6-hour window; the save loader now keeps `tower` and `relics` lists (it used to drop the Magic Tower's casters).
- Items (user designs): `masterwork-claw-of-beowulf` (Zweihander, Boreal Bolt main, mark-on-hit 35%/+20%/2 turns, wolves 2%); `masterwork-stormhide-cuirass` (Leather Armor, Church of Talos main, +10 Evasion, −50% Lightning spell damage, `soldAt` "Storm Reliquary · Church of Talos", forged at the buyer's level); `masterwork-transformer` (Kite Shield, Church of Talos main, unchanged effect, Tankina 8%). Old ids migrate via `LEGACY_ITEMS`; catalogue entries removed. `ForgeArt` maps masterwork keys to the old hand-made battle designs through `SPRITE_UNIQUE` (`claw-of-beowulf` → `beowulf`, `stormhide-cuirass` → `stormhide`).
- Tests: `claude-work/tk-rework-checks.js` appended to `talos-boss-browser.js` by `patch_tk.py` (also the talos cinematic, edge and guild-information checks). `claude-work/rebuild-testclient.sh` rebuilds the patched test client from the pristine image and refreshes `client/tests`.

**Boss rework 7: Ornn, God of the Forge (2026-10-11).** User decisions: Reforging; difficulty like Tankina (prepared Level 51 ~25%); Ram at half HP, guarding the anvil; tempers Molten Edge, Hardened Plate, Spiked Plate (not Quenched Swing); spoiled by damage; a temper lasts until the next reforge; keep 50% with a new cutscene; full redesign; smith's work-song; intro; Anvil Hammer and Godforged Plate reworked last.
- Enemy-file `reforge` {firstTurn, everyTurns 3, spoilPercent 15, spoilApLoss 2, guard "vulcanic-ram-level-50", tempers [{id, name, favor wounded|melee|always, description, effects}]}. Engine in `combat.js` (`reforgeState`, `reforgeTurnEnd` from `endTurn`, `reforgeTurn` at turn start, `creditReforgeDamage` in both damage paths, `setTemper`, `chooseTemper`): he heats the metal at the end of his turn; at his next turn start the temper sets unless the heroes dealt `spoilPercent` of his max HP, then `staggerAp` costs him AP. While he works the guard gets a `guardian-ward` on him (`reforgeWard`). New timed effect `molten-strike` (extra Fire % of the hit + "Molten Burn" turn-start loss; `applyTimedEffects` keeps `burnPercent`/`burnTurns`). Snapshot `order` shows "Reforging · X" with "Cracked n%", or the held temper; `stageLook` sets `temper`/`reforging`. The "turn — N AP" log now shows the real AP (after roots or a stagger).
- AI `ornn`: two strikes a turn, skips all-or-nothing, perfect-form, duel, mana-overload. His old numbers made him unwinnable (1,461 armor, speed 213, one-shots). Final: Str 20, Vit 40, Speed 30 with `permanentBonuses.speed` −53, Luck 20, no shield, `permanentDefenseBonuses.armor` 0, `maxHpPercent` 95, wears Level 30 forged copies of his Hammer and Plate (`noDropItems`; Level 48 copies made him unbeatable) → prepared L51 25.5% (49/192; 90%: 29%, 100%: 18%), about 6 rounds, metal cracked ~40% of the time.
- Look: humanoid outfit `smith-apron`, `pauldrons` {color, trim, sheen, spikes}, `forgeCracks`, `embers`/`emberLevel`, `moltenWeapon`; catalog `tempers` and `reforging` variants (merged in `specFor`). Quadruped `molten` (magma cracks, flame fleece) for the Vulcanic Ram. Intro `ornn` and phase `ornn-phase` (`forgeScene`). Music: `ornn-god-of-the-forge` (C aeolian work-song, anvil, trim 2.06, −17.3 LUFS) and `ornn-forge-erupts` (C harmonic, 132 bpm, trim 1.08, −17.0 LUFS), switched on `phaseIndex`.
- Items (user designs): `masterwork-anvil-hammer` (Mace, Hearthforge main, 15% of Armor as damage, 12% with first-kill guarantee; drawn as a portable anvil on a short handle: `MASTERWORK_LAYOUT["anvil-hammer"]` style `anvil` in `forge-art.js` `bluntHead`, dark iron with a Mythilium inlay, used by both the portrait and the battle sprite) and `masterwork-godforged-plate` (Full Plate, Hearthforge main, evasion locked at 0, masterwork field `armorBonusPercent` 25, 8%). Old ids migrate; old `firstKillDrops` ids migrate to the template ids.
- Tests: `claude-work/or-rework-checks.js` appended to `cinder-bosses-browser.js` by `patch_or.py` (also the cinder cinematic preview). Simulator: `ny-sim.js` options `OVR` (incl. `equip`, `unequip`, `permanentBonuses`, `permanentDefenseBonuses`, `baseArmor`; applied after the battle starts, so speed changes there miss the opening turn order) and `DEFP` (definition fields such as `maxHpPercent` or `reforge`, applied before the battle); `rt-launch.sh` boss key `or`; `or-stats.sh` prints temper counts. Watch out: `pgrep`/`pkill -f` patterns match their own shell's command line.

**Boss rework 8: Gaian, Avatar of the Land (2026-10-11).** User decisions: Regrowing Cores; difficulty like the King (prepared Level 56, 30–35%); keep the two Jungle Cores; no phase; full redesign; "Heart of the jungle" music; intro; items last.
- Enemy-file `heartCores` {enemyId, regrowTurns 3, severed {turns 2, damageTakenPercent 30, apLoss 2}} next to the existing `livingAllyProtection` (25% per living Core). Engine `heartCoreTurn` (turn start) in `combat.js`: dead Cores are noted with the avatar's turn count, a lone one regrows (revived in place, full HP, `regrowCore`) 3 of his turns later; if all are down he is Severed (unremovable `severed` effects, AP loss via `staggerAp` + `staggerReason`) for 2 of his turns, then all regrow. Core EXP is awarded only once (`experienceAwarded`). Badge (`order` slot): "Severed · n turns" or "Core regrowing · n turns"; `stageLook.severed`. Music `gaian-avatar-of-the-land` (E dorian heartbeat, trim 2.47, −17.2 LUFS) and `gaian-severed` (faltering heart, trim 4.6, −19.6 LUFS) switched by the severed state.
- AI `gaian`: two strikes a turn, no burst skills. Balance: he no longer wears the Soulbound Barrier (its armor from 25% of max HP fed the Jackhammer's power from armor, making him unhittable and unstoppable as HP rose); wields a Level 30 forged Jackhammer (`noDropItems`); `maxHpPercent` 720 → prepared L56 33% (64/192), about 4 rounds, severed ~1.6 times a fight.
- Look: elemental stone with `lower: "roots"`, headwear `canopy-crown` {color, blossom}, `moss`, `veins` (+`veinsDim` when severed via catalog `severed` variant), `outfitIsArmor`. Jungle Cores: core rig `style: "seed"` (`heartSeed`). Intro `gaian` (Old Growth, two Cores as retinue).
- Items (user designs): `masterwork-jackhammer` (Greathammer, Elven main, +20% Armor as weapon power and 10% splash, 12%), `masterwork-soulbound-barrier` (Garb, Institute of Magic main, armor 25% of max HP and the missing-HP spell ward, 8%); Titanfall unchanged at 5%. Old ids migrate.
- Tests: `claude-work/ga-rework-checks.js` appended to `gaian-boss-browser.js` by `patch_ga.py` (forged items, relative armor/power checks: forged parts multiply the Barrier armor and Jackhammer power by their own percentages). `rt-launch.sh` key `ga`, `ga-stats.sh`.

**Boss rework 9: Archdevil Saffron (2026-10-12).** User decisions: Infernal Sacrifice; "a step harder" (prepared Level 69 about 20%); keep all three Servants; sacrifice a Servant below 35% on his turn; heal 10% + Infernal Power (+15% spell power, stacking); a rift returns it after 3 of his turns, Servants killed outright stay dead; no phase; full redesign; music "a theme fit for a royal with a hellish undertone"; intro; items last.
- Enemy-file `infernalSacrifice` {servants [ids], belowPercent 35, healPercent 10, powerPercent 15, returnTurns 3}. Engine `sacrificeTurn` (his turn start: rift returns first, then he drains the weakest Servant below the threshold, one a turn) in `combat.js`; a drained Servant is set to 0 HP with `sacrificed` and `experienceAwarded` true (no EXP or loot), reset when it returns. Badge "Infernal Power n · Rift returns X · n turns"; `stageLook.infernal` (1–3). AI `saffron` (one damaging spell a turn; skips mana-overload, quickened-spell, magnum-opus, wild-thesis) and `devil-servant` (one strike a turn; skips execute, all-or-nothing, perfect-form) — without them the court one-shot L69 heroes.
- Balance: Saffron `maxHpPercent` 590, Servants `maxHpPercent` 300, wears Level 40 forged copies (`noDropItems`) → prepared L69 22.4% (43/192), ~3.6 rounds, ~0.65 sacrifices a fight. Hellish Rebuke's retaliation and his self-heals/shields make long, high-HP fights lethal; the sim's per-actor damage split credits DoT ticks during his turn to him.
- Look: humanoid outfit `infernal-robe` {color, color2, trim, sigils} (+`sigilLevel`), headwear `hellfire-crown` {color, gem, flame, blaze 1–3}, bat wings `veins`, servant `brand` on the brow; catalog `infernal` variants. Intro `saffron` (Pyramid Sanctum, four braziers, three-Servant retinue list). Music `archdevil-saffron` (D hijaz royal processional, trim 2.03, −17.1 LUFS).
- Items: caster masterworks are new (`isCasterMasterwork`: def with a staff/wand type, no parts, `mainMaker` + `element`; Greater gem, legendary handle roll; own id format `weapon-masterwork-<key>-<lvl>-<gem>-<handle>-<ink>`; built from `buildCasterItem` with masterwork name/rarity/effects). `masterwork-hellish-rebuke` (Spell Wand, Hellmade Ruby, unchanged effects, 12%), `masterwork-seventh-cycle` (Chainmail, Hellmade main, unchanged effect, 6%). `objects/armor.json` was then deleted at the user's request (2026-10-12): every armor piece is forged (masterworks included). `app.js` no longer loads, caches or offers a file chooser for armor (`ITEM_FILES`/`ITEM_CACHE_KEYS`/`itemSourceStates` have no armor; the old `emberfall-armor-cache` localStorage entry is removed at startup); `itemCatalog.armor` stays an empty array and the armor record parser is left in place unused. The smoke check was relaxed to `armor >= 0` in `patch_sf.py`; backup in `claude-work/armorjson-backup/`.
- Tests: `claude-work/sf-rework-checks.js` appended to `saffron-boss-browser.js` by `patch_sf.py`; `rt-launch.sh` key `sf`, `sf-stats.sh`. Fixed a flaky Tankina store check (the random stock could offer a spell the test hero already knew).

**Boss rework 10: Ancient Lich Ool (2026-10-12).** User decisions: the phylactery; difficulty like Saffron (prepared Level 81 about 20%); keep the three Legionnaires, tied in (they guard it); re-form after 2 rounds at 50% HP; the Legionnaires guard it (single-target attacks redirected, area attacks reach it); broken = mortal and −20% spell power, and if he is dust at that moment he stays dust; a cutscene when it shatters; full redesign; requiem music with a frantic dirge once mortal; intro; items last, per item.
- Enemy-file `phylactery` {enemyId, guards [ids], guardAiProfile, reformRounds 2, reformHpPercent 50, broken {effects}, cinematic} plus `startingAllies` for the vessel `Phylactery of Ool` (new file, `inert: true`: inert actors never act; `advance()` skips them after the turn-start hooks, so their turns only count rounds; inert actors get no undead death save). Engine in `combat.js`: `phylacteryState`, `phylacteryTurn` (turn start; each vessel turn while he is dust is one round), `logFall` (replaces the two "falls" logs: a held lich `turnToDust`, the vessel's fall `shatterPhylactery`, a mortal lich ends the state), `reformLich`, `phylacteryHolds` (dust awards no EXP or loot), `phylacteryGuardFor` (in `redirectedGuardTarget` and `forecastRedirections`; the guard with the most HP; `redirectedGuardTarget` gained an `area` flag, passed by the two weapon-sweep sites). Badge (`order`): "Phylactery · Guarded by N Legionnaires", "Ool is dust · N rounds" (`unit: "round"`, new in `battle-stage.js`), "Mortal". `stageLook`: `soulDimmed` on him, `cracks` 0–3 on the vessel.
- Discovered: the Undead race's Unliving forbids spellcasting, so Ool (and every undead caster) never cast. New enemy flag `unlivingCaster` (Ool only; checked in `actionAllowed`). Summoner 7 → 2 removed Overprotective (he was untargetable while his summons lived), and Immortal Body (Lich 1) is suppressed for a boss with a phylactery (`lichManaBarrier`). `aiProfileOf` now also gives summoned guards the phylactery's `guardAiProfile`.
- AI `ool`: two damaging spells a turn (a sweeping one counts as both), never the same sweeping spell twice running, skips mana-overload/quickened/magnum-opus/wild-thesis. `grave-guard` (his summoned Legionnaires only; random-encounter Legionnaires are unchanged): one strike a turn, no all-or-nothing, rampage, execute, perfect-form, duel or champions-challenge.
- Look: humanoid outfit `grave-robe` (tattered hem, ragged mantle, ribcage opening with `soulFire`, dim with `soulDimmed`, a stole with his sigil), headwear `bone-crown`, `tabard` {color, trim, sigil} for grave-knights (sigil also on the helm brow; `oolSigil` helper); catalog `mortal` variant; core rig `style: "phylactery"` (crystal in a bone cage on a grave-stone, `cracks`). Intro `ool` and cutscene `ool-phylactery` (`graveScene` in `boss-intros.js`, `bi-oo-*` CSS). Music `ancient-lich-ool` (D aeolian requiem, Dies Irae in deep choir, organ, toll, muffled drum; trim 3.26) and `ancient-lich-ool-mortal` (D harmonic, 132 bpm; trim 1.41), switched on `battle.phylactery.broken`.
- Items (user designs): `masterwork-staff-of-the-guild` (Spell Staff, Institute of Magic main with a Greater Death jewel, Hematite, which the Institute never cuts otherwise: caster masterworks now force their main maker's stone of their element in `generateMasterwork`; "summons only": summon spells −10%, summons +40% HP and Mana, +30% damage; 8%) and `masterwork-heartless-enricher` (Dagger, Abyss main, off-hand 100% Armor Pierce, 5%). Both removed from `weapons.json`, old ids migrate via `LEGACY_ITEMS`. Ool wields a Level 50 copy (`noDropItems`). Phylactery Core still 20%.
- Balance (with the new staff): Ool `maxHpPercent` 900, phylactery 3000 → prepared L81 22.7% (29/128 after the corpse fix; 30/128 before), about 6.4 rounds, ~0.9 dust deaths a fight, the phylactery broken in every win. Trials: 700/3000 34%, 1000/2500 25%, 800/4500 12.5%. Legionnaires still deal ~2/3 of the damage, Ool ~1/3.
- Corpse fix: `defeatedEnemyFor` (Call of the Dead's `consumeDefeatedEnemy`) used to remove any dead foe from `battle.actors`, including the player, companions, and Ool's dust (a hero's Wild Thesis did it in the sim). It now skips the player, companions, inert actors and a lich held by his phylactery. Before Ool, only undead (who could not cast) knew the spell.
- Regression after the rework: unchanged 28/38, smoke/enemies/lifecycle pass.
- Tests: `claude-work/oo-rework-checks.js` appended to `coast-lich-bosses-browser.js` by `patch_ool.py` (also flips the old Overprotective assertion and moves the staff/dagger checks to the forged items). `rt-launch.sh` key `oo`, `oo-stats.sh`; `ny-sim.js` results carry `phyl` (deaths/reforms/broken).

**Boss rework 11: Ur-Mat, the False Dreamer (2026-10-13).** User: "he is supposed to be the most powerful mortal sorcerer". Decisions: Dream laws; Brutal (prepared Level 93 about 10%); alone (helpers only from his spells); Lucid Change kept with a new cutscene; laws every 3 of his turns, fixed cycle, no early breaking; seven laws (Blades of Glass, Rebounding Words, Inverted Mercy, Slowed Time, Echoing Spells, Sealed Names, Dreamer's Ward); cycle starts at a random law; after Lucid Change two laws hold at once; full redesign; dream waltz; intro. Items: Keening (Elven longsword, echo 50 → 40%, 10%), Sunder (Hearthforge greathammer, unchanged, 10%), Wraithguard unchanged (artifact, 10%), Gem 5%; he carries nothing.
- Enemy-file `dreamLaws` {everyTurns, randomStart, doubleAfterPhase, laws [{id, name, description, foes, allies, apLoss}]}. Engine in `combat.js`: `dreamState`, `dreamTurn` (turn start), `writeLaw`, `applyLawsTo` (effects flagged `dreamLaw`), `endDream`; shown through the snapshot `season` slot (`dream: true`, two names joined with " and "); stage overlay CSS per law id. New effect types: `spell-rebound-percent` (in `hurt`), `inverted-mercy` (amount = healing %, hp = wound %; `healingAmount` + a wound in `setHp`), `skills-sealed` (`actionAllowed`), `spell-echo-percent` (in `cast`); Slowed Time is `stat-percent` speed −25 plus `apLoss` via `staggerAp`.
- Found: his AI used Lucid Change as a heal by itself (it is also a Sharmat skill); the `ur-mat` profile skips it and arcane-echo/arcane-chorus/doubled-spell/arcane-compression/battle-cry/sorcery-blast/mana-overload/quickened/magnum-opus/wild-thesis; one damaging spell a turn, never sweeps two turns running, Tier 10 spells from his 2nd turn and then every 3rd, one heal a turn only below 60% HP, no new summon while one lives. The phase is now `unskippable` (otherwise the party skipped Lucid Change). Summoner 7 → 2 (no Overprotective) + Dreamweaver 5 keeps the class total at 90 (Ool's total dropped to 73 when his Summoner went 7 → 2; not fixed).
- Balance: his old sheet (Int 340, Wis 316, Speed 170, ~1,700 HP) either died before acting or one-shot the sim party with any Tier 9–10 sweep (his class stack doubles spell values; casters have 700–1,000 HP at Level 93). Final: Int 100, Wis 100, Speed 110 (Mind 352 kept for his Mana pool; enemy stats are not shown to players), `maxHpPercent` 500 → prepared L93 8.6% (11/128), about 3 rounds, Lucid Change in ~80% of fights and every win goes through it; 450% gave ~13.5%. Speed is the strongest lever: tune it with `DEFP` `{"character":{"stats":{…full stats…}}}` (shallow merge), never `OVR` (applied after the opening turn order). `ny-sim.js` OVR used to delete `resourceMax`, which disabled unskippable phase floors; fixed. His Black Dragon summon deals ~1/4 of the damage.
- Look: humanoid eyes `closed`, `thirdEye`, headwear `dream-crown` (`blaze`), outfit `dream-robe` (`stars`), `glyphs`, `lawGlyph` (from `stageLook.law`, seven signs in `LAW_SIGNS`); catalog `phases.1` opens his eyes. Intro `ur-mat` and `ur-mat-lucid` (`dreamScene`, `bi-um-*`). Music `ur-mat-false-dreamer` (composer waltz, replaces the old score, trim 3.14) and `ur-mat-lucid-change` (trim 1.2).
- Tests: `claude-work/um-rework-checks.js` appended to `cinder-bosses-browser.js` by `patch_um.py` (also the old Lucid checks moved to the boss-intro player, Keening 40%, forged Keening/Sunder, and the cinder cinematic preview). `rt-launch.sh` key `um`, `um-stats.sh`. A check that forces a law must push `battle.dream.startTurn` far ahead, or he rewrites it before the hero's turn (he is much faster than the test hero). Regression after the rework: unchanged 28/38.

**Boss rework 12 (the last): Anatemis, the White Star (2026-10-16).** User: "canonically the strongest being currently alive". Decisions: a trial per form, each form keeping the earlier ones; hardest of all (prepared Level 100 about 5%); alone; both phase changes kept (Golden Star 50%, Zenith 15%, both unskippable by her id) with new cutscenes; White Star = Mirror Scales, Golden = Sunfall, Zenith = Supernova; full redesign; celestial hymn per form; intro. Items (user designs): the Starscale Blade became **Quasar Jet** (Roundtable longsword with a pommel instead of a crossguard; never misses; each critical hit charges the next attack +30%, each further critical in a row +30% more, up to +300%; 10%, no guarantee) and the **Golden Apex** a caster masterwork (Institute of Magic Greater Diamond; prism: the wielder's Basic damage splits into four quarters, one Basic and three random other elements; 8%; the old discounts and Sweep/Zenith choice are gone). `objects/weapons.json` was then empty (every weapon is forged).
- Enemy-file `starTrials` {mirror {reflectPercent 25, breakPercent 20, reformTurns 3}, sunfall {fromPhase 1, damagePercent 50, deflectPercent 8}, supernova {fromPhase 2, chargeTurns 3, damagePercent 60, collapsePercent 15, staggerAp 2}}. Engine in `combat.js`: `starState`, `starTurn` (her turn start: mirror reform, Sunfall resolve then mark, supernova start/detonate), `mirrorScales` (in `hurt`: reflects hero spells, weapon damage wears the mirror), `creditStarDamage` (all damage paths: deflect/collapse ledgers), `starStrike` (irreducible share of a hero's max HP), `endStarTrials` (also from `logFall`). Badge (`order`): Supernova n turns · Sunfall on X, Sunfall · Marked, Mirror Scales / Mirror shattered. `stageLook.mirror`, `.nova`. `playPhaseIntro` now shows the previous form as "before" when phase > 1.
- New weapon effect kinds: `always-hit` (in `weaponAttackValues`), `critical-chain-damage-percent` {amount, maxAmount} (`actor.critChain`, in `strike`), `prism-basic-damage` (in `hurt`, logs the split); labels in `artifactEffectLabel`. `forge-art.js` blade layout option `pommel` (Quasar Jet: no crossguard, the second part caps the grip). Guaranteed-until-claimed drops are now template-aware (`lootMatchesDrop`; claimed ids may start with `masterwork-`).
- AI `anatemis`: two blows a turn (a sweep counts as both), no sweep two turns running, Tier 10 from her 2nd turn then every 3rd, one heal a turn only below 60%, skips overloads/repeats/gambles. Balance: her Dragon race adds +2 per level to every stat (+200 at Level 100), so raw stats alone cannot tame her (old effective Speed 744, Int 982): final raw Str/Dex/Int/Wis/Speed 0 with `permanentBonuses` Str/Dex −70, Int/Wis/Speed −85 (her phase bonuses +30 Int/Wis and +25 all still apply), `maxHpPercent` 110 → prepared L100 4.7% (6/128), about 2.7 rounds, Zenith reached in ~60% of fights. 120% gave ~3%, 100% ~17%.
- Look: dragon rig `constellations`, `aurora` (wing bands), `lightCrown`, `mirror` (gleams), `corona` (Golden), `heartStar` (+`nova`); catalog `phases` 1 gold / 2 white-gold. Intro `anatemis` (a star falls onto the Frozen Crown), `anatemis-golden` (sun rises), `anatemis-zenith` (bloom of rings) via `starScene`. The old `#anatemis-cinematic` markup and `playPhaseCinematic` are now unused (left in place). Music `anatemis-white-star` (D dorian hymn, replaces the old score, trim 3.45), `anatemis-golden-star` (march, 2.18), `anatemis-zenith` (choral finale, 1.33), switched on `phaseIndex`.
- Tests: `claude-work/an-rework-checks.js` appended to `anatemis-browser.js` and `an-drops-checks.js` replacing `anatemis-drops-browser.js`, via `patch_an.py` (also party-loot and guild-information fixtures, the lifecycle catalogue fixture and smoke `weapons >= 0`). `rt-launch.sh` key `an`, `an-stats.sh`. Regression after the rework: unchanged 28/38.

**Masterwork Elementals (port task 1, 2026-11-05, a cloud session; the task is
`engine/Docs/Port-Tasks/1_Masterwork_Elementals.txt`).** Done in the old game before the port (user decision); the six
masterworks' looks are placeholders the user wants fully remade in the port. The user's answers, asked before the code:
- **Drops:** each at 5% from every elemental-race enemy of its element (its file's `element`, else its spells'), bosses
  excluded: Plasma Blade from Fire Elemental, Fire Spirit, Magma Elemental, Vulcanic Ram; Tidepiercer from Water
  Elemental, Water Spirit, Greater and Raging Water Spirit, Oasis Guardian; First Comet from Earth Elemental, Earth
  Spirit, Greater Earth Spirit, Steelguard Golem, Pyramid Sentinel (not Gaian); Peace and Balance from Wind Elemental;
  Kugelblitz from Lightning and Storm Elemental; Avalanche from Ice and Frost Elemental. The elementals keep their
  placeholder weapons for good; old saves' retired uniques convert as before.
- **Plasma Blade:** a wand for spells (always a Greater Ruby by Hellmade, like Hellish Rebuke, with its handle and
  inscription) whose attacks use a longsword's numbers (power and crit by level, one-handed), scaling Intelligence then
  Dexterity at a longsword's ratios, all attack damage Fire; no rune slots. (Not a wand's weak attacks.)
- **Fixed runes:** Tidepiercer (Water), First Comet (Earth), Peace and Balance (Wind) and Kugelblitz (Lightning) always
  have their element's damage rune, Avalanche the Ice resistance rune: part of the masterwork, not removable, on top of
  the rune slots it rolls (a weapon still can't take a second damage-element rune). (Not in one of its slots, not
  removable runes.)
- **Tidepiercer:** +50 armor pierce on top of its parts' (capped at 100% as all pierce). Its lance has a crossguard
  instead of a pommel.
- **First Comet:** a 25% chance per hit to stun for 1 turn, by the game's stun rules (stun immunity holds).
- **Peace and Balance:** a two-part quarterstaff; no effect pushes its wielder's turn meter back or lowers their Speed,
  whether from enemies or their own skills and gear; increases still work.
- **Kugelblitz:** a 15% splash to every other enemy, reduced like Jackhammer's; every enemy it damaged is marked until
  the end of its next turn (refreshed, never stacked): its spells cost 50% more Mana.
- **Avalanche:** a brigandine. Each damage taken adds a counter; at 7 every enemy loses 30% Speed for 3 turns, the
  counters reset and can build again; a new trigger refreshes the slow. Counters start at 0 each battle.
- **Looks (placeholder):** each type's normal forged masterwork look with simple hints: a short red line from the Plasma
  Blade's gem, three prongs on Tidepiercer's head. To be fully remade in the port.
- **Stories:** drafted, one line each in the style of the other masterworks; the user can change them any time.
- **How it's built:**
  - `ForgeRules.MASTERWORKS` has `plasma-blade`, `tidepiercer`, `first-comet`, `peace-and-balance`, `kugelblitz` and
    `avalanche`, each with `placeholderArt: true`.
  - New definition fields:
    - `fixedRunes`, read by `fixedRunes(spec)` and `allRunes(spec)`. They count in `totals`, `armorTotals` and rune
      conflicts; they're never in the id or the slots, and the parts list shows them as "fixed".
    - `totalsBonus` (Tidepiercer's +50 pierce).
    - `attacksAs` (the Plasma Blade's longsword numbers, Intelligence then Dexterity, `element: "fire"`, in
      `buildItem`'s caster branch).
  - Two-part masterworks (`null` third part, "x" in the id) for the quarterstaff.
  - New effect kinds:
    - `stun-on-hit-chance` and `spell-cost-mark-on-damage`, in `strike`'s on-hit loop.
    - `turn-and-speed-lock`: `wieldsTurnAndSpeedLock()` in `app.js`, read by `currentStatsUncached` (which also covers
      `permanentStatPercent`), `statPercent`, hostile and party stat effects, and `shiftTurnMeter` for `shift-turn-meters`.
    - `avalanche-counters`: `addAvalancheCounter`, from `triggerDamageTakenEquipment`, with counters in
      `actor.avalancheCounters`.
    - `armor-pierce-bonus`, which only labels Tidepiercer's pierce.
  - Placeholder hints in `forge-art.js`'s `MASTERWORK_LAYOUT` (`beam`, `prongs`; marked PLACEHOLDER ART). Battle
    sprites of forged items are drawn there too.
  - Tidepiercer's +50 pierce usually makes it "Rending" (the pierce prefix) unless it is Flawless.
- **Tests:** `test-client/tests/masterwork-elementals-browser.js`, one entry per rule (`runMasterworkElementalItemChecks`,
  `…RuneChecks`, `runTidepiercerChecks`, `runFirstCometChecks`, `runPeaceAndBalanceChecks`, `runKugelblitzChecks`,
  `runAvalancheChecks`, `runMasterworkElementalDropChecks`; `runMasterworkElementalChecks` runs all). The combat checks
  load `combat.js` again with its internals exposed, as `anatemis-drops-browser.js` does. Each rule was broken once in a
  copy of the game (22 mutations, all caught; one found `permanentStatPercent`'s own Speed filter redundant, so it was
  removed). Not covered by a check: the lock on stat effects from higher-level enemies' passives and from party-wide
  effects (the same one-line rule as the checked paths). `masterwork-rarity-browser.js` now counts 31 masterworks (was
  25). The other browser checks and suites gave the same results as before (54 of 66 runs passing; the 12 failures are
  the known ones; `map-speed` failed once and passed twice on rerun).

**Guild Rework (port task 2, 2026-11-06, a cloud session, branch `claude/fervent-carson-f5ol9s`; the task is
`engine/Docs/Port-Tasks/2_Guild.txt`).** Built in the old game, as task 1 was (user decision G1); old game only, the engine
is untouched and its work goes on beside it. Done: the faithful port brings it over and doesn't build it again; its only
port work is remaking the two guild interiors, which are placeholder art (G2; `engine/Docs/Port-Tasks/README.md` says
what the remake keeps). The user's 34 answers, asked before the code, are in
`engine/Docs/Engine-Plan.md`, "Decisions for task 2, Guild Rework" (G1–G34). How it's built:
- **Rules:** `scripts/guild-quest-engine.js` (`GuildQuestRules`, loaded after `encounter-engine.js`; pure, runs in Node):
  the six types and their reward factors (Hunt 1.8, Cull 1.55, Deliver 1.35, Search 1.2, Escort 1.1, Travel 1.0 on
  today's silver and Reputation formulas), `rewardTable` (every type at least 1 silver and 1 Reputation above the one
  below it, so rounding never ties two ranks), the quest level (`questLevel`: the player's level held to the guild
  region's range; the Alder Vale, which has no `levelRange`, spans its zones' 1–15), `greatHuntReward` (×2.4, Reputation
  ×3 on top, at the boss's level or the quest level, whichever is higher), board and recruit counts, `cullRequired`,
  the Cull circle (radius 40 units = 3 km) and its 50% target share, Search clearances, the pre-fight escape factors
  (Search 0.5, Escort bandits 0.3), the Escort ambush roll (`ambushCount`: one roll, 80/10/10) and plan (25–75% of the
  outbound trip, a second at least 20% later; `ambushRetry` after a defeat), `AMBUSH_GROUPS` (the encounter tables'
  bandit-only groups by their enemies' level: 4, 10, 15, 36, 44), adjacency over roads and boats (`links`,
  `adjacentRegions`, `firstStep` for markers), `tripMinutes` (land travel along the fastest routes), notice titles,
  recruit level, cost and gear rarity (`gearRarity`: `ForgeRules.rarityOdds` at the region's midpoint, like the
  Blacksmith's stock).
- **State** (`world-engine.js` `normalize`): `guild.quest` (the one standard quest, `version: 2`), `guild.greatHunt` (the
  one Great Hunt; older saves kept it in `quest` and are moved), `guild.boards` (`{ guildId: { day, offers } }`, only
  today's survive), `guild.notice` (the note shown once when an old quest was dropped). New quest fields: `title`,
  `level`, `destination` (`regionId`, `settlementId`, `name`, `point`), `recommended`, `radius`, `ambushes`
  (`[{ at, done }]`), `travelled`, `tripMinutes`, `taken` (on board offers). Old Clear, Escort and Travel quests are
  dropped with the note (G33); old Hunt and Search quests are kept. `recruitOffers` keep a `guildId` (app.js) and hold
  up to 5.
- **Guild** (`scripts/guild.js`): `boardFor` draws a guild's board on the first visit of a day (castle 4, town 3,
  Bounty Broker +1, each a different type, plus a Great Hunt when eligible); `accept` (one standard quest, one Great
  Hunt; an Escort's trip and ambushes are rolled here); `useCounter` (hands a Deliver's letters over at the destination
  guild, then pays every finished quest at once with the rising note; journeys only at the guild that issued them,
  Hunt, Cull and Search at any guild as before, Great Hunts in their offering region); `giveUp` (anywhere, with a
  confirmation); `onTravel` (an Escort's outbound minutes, from `map.js`'s tick); `objectiveAt` (also the due Escort
  ambush, wherever the trip is, roads included); `onBattleFinished` (from `combat.js`'s `finish`: Search and Escort
  fights are settled by a win, and by fleeing through `avoidObjective`; a defeat leaves Search open and reschedules the
  ambush); `adjustEncounter` (the Cull share, for travel and Force encounters in `map.js`); `markers` (a journey's
  destination or the way back, on the road or harbour toward another region) and `areas` (the Cull circle, drawn by
  `map.js` in `#map-guild-areas`). `Guild.makeOffer(type)` is for the browser checks.
- **Interior:** the guild window is a full-window scene like the shops (`.shop-hall.guild-hall`): `scene-banners.js`
  `guildHall` (castle cities) and `guildTown` (the four town guilds), PLACEHOLDER ART (`PLACEHOLDER_ART`, G2). Clickable
  objects are recorded with `spot()` and drawn again in a third SVG layer (`.sb-spots`): a hit box, a copy of the object
  whose SVG filter leaves only a gold outline glow, and a name label, shown on hover or keyboard focus. The board,
  counter, tavern, hooded man (castle only) and trophies (once a Great Hunt of that guild is done) open the docked
  panel (`#guild-dock`, `#guild-panel`). Town guilds gained `recruit` in `guildServices` (two recruits) and keep no
  `information`. Flames draw over everything, so the hearths stand clear of the people.
- **Recruits** (`companions.js` `makeOffers({ count, level, regionId })`): +1 to every attribute per level, 5 stat
  points per level and 1 class point per level left unspent, one melee forged weapon (not ranged, not a staff or wand)
  and one body armor the race can wear, each rolling its rarity at the region's level. As before, the other offers go
  once someone is hired; the next visit draws new faces.
- **Map and class menu:** `#map-quests` (key J) opens `#guild-quests-modal` with both quests and Give up; Search starts a
  fight (`presentTravelEncounter(…, "search")`); the encounter prompt says when escaping is harder. The class menu's
  `#class-info-button` swaps the two class lists for `#class-info-panel`, the classes whose requirements were bought,
  each showing `Guild.classInformation(id)` (the bought page). Courier's texts and requirement label now name Deliver.
- **Tests:** `test-client/tests/guild-rework-browser.js`, one entry per rule (`runGuildBoardChecks`, `…QuestLimit…`,
  `…Reward…`, `…Hunt…`, `…Cull…`, `…Deliver…`, `…Search…`, `…Escort…`, `…Travel…`, `…Marker…`, `…Counter…`,
  `…Interior…`, `…Recruit…`, `…QuestCard…`, `…ClassInformation…`, `…Save…`; `runGuildReworkChecks` runs all). The checks
  that drove the old buttons now use the room: `guild-browser.js` (its Clear, Travel and Escort parts are in the new
  file), `regional-guild-browser.js`, `guild-information-browser.js` (the hooded man), `guild-rematches-browser.js` (the
  trophies), `companions-browser.js` (the tavern), `coast-island-browser.js`, `newc-browser.js`, and the boss checks
  that accept Great Hunts (`Guild.boardFor`, `Guild.accept`, `Guild.useCounter`, `worldState.guild.greatHunt`).
- **Not covered by a check:** that Run in battle keeps its normal chance in a Search or Escort fight (G14; nothing in the
  battle reads the objective's factor); the reward note's fade after three seconds.
- **The checks can fail:** each rule was broken once in a copy of the game (94 mutations, one per rule, by a small runner
  in the cloud session's `claude-work/guild/`, not kept: a copy of `old-game/` per worker, the covering entry run with
  `--game-folder` pointing at it). 93 were caught; the first pass let five through, and four of those were gaps the checks then closed
  (two Great Hunts with the same id, an encounter roll that already held the Cull target, a Search spot never near an
  exit in today's zones, a Search button checked only from another zone). The fifth, dropping the shield filter from a
  recruit's armor, cannot differ: `allowedSlots` puts a shield in the off-hand only, never in the armor slot.
- **Regression** (2026-11-06, the cloud, Xvfb): every zero-argument entry of the 42 check files (88 entries: the 71 before
  plus 17 new) and the smoke, enemies, lifecycle and combat suites, against a baseline run of the game as it was before
  the rework (61 of 71 passing). After: 77 of 88 pass; the 11 failures are the baseline's 10, unchanged
  (`adventure-improvements` ×2, `bloody-mary`, `combat-lifecycle` outside its suite, `music`, `newc`,
  `settings-companions`, `shop`, `talos-edge`, `trap`), plus `map-speed`, the known flaky travel-timing check, which
  passed twice when rerun alone. Smoke, enemies (511 cases) and lifecycle pass; the combat suite stops at the known
  Barbarian failure, as before. `cinder-bosses`' `runCinderBossChecks` failed once during the work (Ur-Mat's Slowed
  Time, a random battle case) and passed four times since, also on the old game. The failing `adventure-improvements`
  and `newc` entries stop before their guild parts, which were moved to the room all the same.

**Shop Rework (port task 3, 2026-11-06, a cloud session, branch `claude/fervent-carson-f5ol9s`; the task is
`engine/Docs/Port-Tasks/3_Shop_Rework.txt`).** Built in the old game, as tasks 1 and 2 were (the user: "same rules
apply"); old game only, the engine is untouched and its work goes on beside it. Done: the faithful port brings it over
and doesn't build it again; its only port work is remaking its placeholder art (R1; `engine/Docs/Port-Tasks/README.md`
lists it). The user's 29 answers are in `engine/Docs/Engine-Plan.md`, "Decisions for task 3, Shop Rework" (R1–R29).
How it's built:
- **Reforge rules** (`scripts/forge-engine.js`, the section before `const api`; pure, runs in Node): `reforgeKind`
  (weapon, armor, clothing, caster), `reforgeReason(spec, anvil)` (anvils `smith`, `tailor`, `institute`, `hearth`:
  weapons and heavy/light armor and shields at the Blacksmith and Hearth Home, clothing at the Tailor, staffs and wands
  at the Institute, masterworks only at Hearth Home; the Outlaw-only club nowhere), `reforgeOptions(spec, index)` (parts
  2 and 3 only; every shop-made part, `partOptions`/`armorPartOptions` with Outlaw and Vampiric off, up to the main
  part's rarity; masterworks add Mythilium by the masterwork makers; a staff's or wand's handle up to `TIER_HANDLE_CAP`
  by its stone's tier, the inscription any ink), `reforgeCost` (`REFORGE_COST_TIMES` 3 × `RARITY_PRICE` of each
  changed part's rarity; an inscription by the handle's rarity; unchanged parts free), `reforgedSpec`/`reforge` (main
  part, level, slots and runes kept; a part outside the options is refused). `generate({ maker })` and
  `generateArmor({ maker, types })` forge a fixed maker's pieces (the Institute's and Hearth Home's stock).
  `MASTERWORKS["doctors-orders"]`: a white Robe, Institute of Magic main part, `placeholderArt: true`.
- **The anvil** (`scripts/reforge.js`, `window.Reforge`): `create({ anvil, discount, list, stage, … })` draws the anvil
  in a shop's detail pane and its lists on the left: `pieces(anvil)` (the pack, equipped pieces marked, and each
  companion's worn gear: R4), the two changeable parts with hover cards, every allowed replacement with its price,
  `commit()` (pays, then `replacePiece` swaps the piece in the pack, its equipped slots following, or on the companion).
  Used by `shop.js` (`anvilFor(kind)`: Blacksmith, Tailor, Hearth Home with 15% off) and `magic-tower.js` (the
  Institute). CSS: `.reforge-*` at the end of `styles.css`.
- **Shops** (`scripts/shop.js`, `STOCK_VERSION` 25): the Blacksmith keeps castle 9 pieces (5 weapons), town 6 (3), no
  clothing (`smithArmorTypes`), and doesn't buy clothing; the Tailor (`#map-tailor`, key U; `SHOPS.tailor`) stands in
  every settlement with a Blacksmith, castle 6 clothing pieces, town 4, buys clothing only; both have a Reforge tab
  (`#shop-reforge-mode`, not in the General Store). Hearth Home's store is the shop window's `hearth` kind: `HEARTH`
  (5 Hearthforge weapons and 4 armor pieces in `shopState.locations["hearth-home"]`, 10% off, buys nothing),
  `hearthOpen()` only after Ornn's defeat there.
- **Institute of Magic** (`scripts/magic-tower.js`): the Tower window has two venues, `tower` (castle cities, as
  before) and `institute` (`VENUES`, `INSTITUTE`): 6 Institute-made staffs and wands and 6 clothing pieces at 15% off,
  Doctor's Orders always on its rail (forged at the buyer's level, 30 Platinum), 12 spells at 20% off, rune fitting and
  removal 20% off, no runes for sale, a Reforge tab for casters; its stock lives in
  `shopState.locations["institute-of-magic"]` and turns over with the shops. `#map-institute`, key I.
- **The map:** Mirrormere's lake has an island (`island`) and a bridge (`bridges`, a road over the water, about 37 km
  west to the ring road on Lakespire's side); `WorldRules.isLand` counts both as land (`onBridge`), and
  `map-regions.js` paints the island. Cinder Isle's road runs on to Hearth Home (`sites`, x 858, y 596) on the volcano's
  south-west edge. Both are safe zones in `encounter-engine.js`. `map.js` draws sites by kind (`SITE_ICONS`: the
  Church's, a domed tower, a forge; the last two placeholders).
- **Hearth Home and Ornn** (`scripts/hearth-home.js`, `window.HearthHome`, loaded after `church-of-talos.js`):
  `#map-hearth-home` (key H) opens its window; Challenge Ornn starts `Combat.startEncounter(["Ornn, God of the
  Forge"])` (his Ram, mechanics and drops unchanged); `onVictory` (from `combat.js`'s `finish`, beside the Church's)
  adds `ornn-god-of-the-forge-unique-level-48` to `worldState.defeatedSiteBosses`, which opens Enter the forge
  (`Shop.open("hearth")`); he can be fought again. Ornn left `guild.js`'s Great Hunts; the guild's information lists
  him as Hearth Home's site challenge (`siteRosters`). Saves (`world-engine.js` `normalize`, R15): an active Ornn Great
  Hunt is withdrawn with a note (beside an old contract's note when both apply), his rematch markers and today's board
  offers of him are dropped, a completed Ornn Hunt doesn't open the forge.
- **Doctor's Orders in battle** (`combat.js`): `healingFactor(recipient, healer)` inside `healingAmount`, which every
  battle heal passes through: +50% when the healer wears it, +25% when the recipient does; a self-heal takes the larger
  (×1.5), one wearer healing another both (×1.875). Each heal site names its healer: spells, skills and items (the
  user), heal-over-time ticks (`healerId`, kept by `applyTimedEffects`), team passives (each owner's share weighted, then
  `HEAL_SETTLED`), lifesteal and regeneration (the actor itself). The AI's heal estimates pass the healer too. Labels:
  `app.js` `artifactEffectLabel`.
- **Traps** (`combat.js` `controlTrapEffects`, `triggerControlTraps`; items in `objects/consumables.json`, General Store
  stock like the old traps): Tripwire (melee: 1 AP lost next turn), Smoke Trap (any attack: the owner's side +20%
  Evasion, `smokeOwnerId`, lifted in `expire` at the owner's next turn, or at each ally's own next turn if the owner
  fell), Frost Glyph (a spell hit, even absorbed: −25% Speed for 2 turns), Weighted Net (melee: stunned for 1 turn,
  stun immunity holds), Gravity Well (any attack: every enemy loses 2 AP next turn). Weapon attacks trigger them hit or
  miss (`triggerWeaponTraps`), spells on a hit (`hurt`). Trap AP losses go into `trapApLoss`, applied at turn start after
  roots and staggers, so they add up (R26). They count against the trap limit; the AI values them by the old traps'
  formula.
- **Scenes** (`scene-banners.js`): `tailorShop` (bolts of cloth, a dress form), the Institute reuses the Tower sanctum
  and Hearth Home the arms hall; `PLACEHOLDER_ART` lists `tailor`, `institute` and `hearth`.
- **Tests:** `test-client/tests/shop-rework-browser.js`, one entry per group of rules: `checkReforgeRules`,
  `checkShopsAndTailor`, `checkInstituteOfMagic`, `checkHearthHome`, `checkOrnnInOldSaves`,
  `checkOrnnInGuildInformation`, `checkDoctorsOrdersHealing`, `checkDoctorsOrdersSources`, `checkNewTraps`. The battle
  entries load a second copy of `combat.js` with a hook (`window.__shopReworkChecks`), as the Ornn checks do.
  `masterwork-rarity-browser.js` counts 32 masterworks now.
- **Not covered by a check:** the anvil's hover cards and list layout; the scenes, island painting and site icons
  (placeholders); companions choosing to place the new traps (the old formula).
- **The checks can fail:** each rule was broken once in a copy of the game (121 mutations, one per rule, by the same
  kind of runner as task 2's, in the cloud session's `claude-work/shop/`, not kept). All 121 were caught after a second
  pass; the first let nine through, each a gap the checks then closed: the armor-part check used full plate, which
  Outlaw and Vampiric never make (now light armor, a shield and a robe too); the fixed-maker forging was only seen
  through stock that filters by maker anyway (now checked on `generate` and `generateArmor` directly); the
  heal-over-time check's numbers vanished in an enemy's −80% (now on the wearer); Gravity Well was set off only by
  melee (now a spell too); a clothing-free Blacksmith shelf was drawn once (now six draws); the Tailor's sell check used
  an equipped sword, never sellable (now a spare mace); and the Ornn board check ran with Ornn's hunt marked completed.
- **Regression** (2026-11-06, the cloud, Xvfb): every zero-argument entry of the 42 check files (97 entries: the 88 of
  merged `main` plus 9 new) and the smoke, enemies, lifecycle and combat suites, against a baseline run of merged
  `main` (07857b1) with its own checks. Every entry gives the baseline's result except `cinder-bosses`'
  `runCinderBossChecks`, which expected Ornn among Cinder Isle's Great Hunts and was updated (it passes); the nine new
  entries pass. The baseline's 11 failures are unchanged (`adventure-improvements` ×3, `bloody-mary`,
  `combat-lifecycle` outside its suite, `music`, `newc`, `settings-companions`, `shop`, `talos-edge`, `trap`);
  `shop-browser.js` is stale since before task 2 (8-piece castle smiths, a `map-spell-shop` button, 219
  catalogue items) and stops at its first assertion as before. Smoke, enemies (511 cases) and lifecycle pass; the
  combat suite stops at the known Barbarian failure, as before.

**Small changes (2026-11-03, a cloud session; the user's list, every design choice asked first and recorded in
`engine/Docs/Engine-Plan.md`, "Decisions for the old game's small changes").** Checked first that none of them gets in
the engine's way. The list's rune slotting sound was left out: the port adds proper sound effects instead.
- **Money:** the purse (`.currency-input`) is disabled outside Dev Mode (`updateEditingControls`, with a "Enable Dev Mode
  to edit money" tooltip on each field and `.sidebar-purse.is-locked` for the cursor). Money still changes through play.
- **Dev Mode text:** "Dev-mode · On/Off" (`#editing-status`, `.brand-subtitle`) is gone; "Stat Fick" and the ✦ toggle
  stay, whose lit or faded look and tooltip show the state. The shorter brand lets the top bar fit on one row at 1440 px
  (as it already did in wider windows), so "Return to combat" sits beside the name there.
- **Temporary bonuses are gone:** no "Temporary bonus" fields on the sheet (the eight attributes, Armor, Evasion) or on
  companion sheets; `temporaryBonuses` and `temporaryDefenseBonuses` left the character record, saves, `withActor`'s
  swap and the enemy files (all were zero). Old saves load with their temporary values dropped. `combineBonuses` became
  `bonusValues` (a numeric copy of the permanent bonuses).
- **Equipment slots:** an item's (or the empty slot's) button fills its whole field in every slot, top-aligned, also when
  a taller slot sets the row's height (the shield next to a weapon filled half its field).
- **The selected target:** on the stage a larger, brighter marker (18 px, glowing), a pulsing ring at the target's feet
  (`.battle-actor-shadow::after`, `battle-target-ring`) and a gold-edged name tag (green for allies); in the classic
  view a 3 px border with a soft glow, also on bosses (`is-unique` used to override the target's look).
- **Levelling:** `sameLevelVictories` is a fast start (3, 3, 4, 4, 4, 5, 5, 5, 6, 6, 6 for levels 1–11; 51 to reach 12),
  then `round(7 + 17 × ((level − 12) / 87)^1.5)`, up to 24 at 99 (186 victories to reach 30, 388 to 50, 1265 to 100;
  before 83, 251, 475, 1075). Enemies' EXP is unchanged (`enemyExperience`: (level + 1) × 100 over the old 5, 6, then
  8–12 victories); `experienceRequired` = an enemy's EXP × the victories, so the targets grow (level 1 needs 120, 12
  needs 1141, 50 needs 5100, 99 needs 20016). Asked because keeping (level + 1) × 100 would have flattened enemy EXP
  from level 50 on.
- **Vial of Dragonblood:** every dragon-race enemy drops it at 0.5% (Mountain Wyrm's chance; 20 more, Anatemis
  included); Nygheir keeps 1%.
- **Matching weapons:** `ForgeRules.MATCHING_BONUS` adds a bonus per maker when every part comes from one maker
  (`matchingMaker`; masterworks call it Flawless): Roundtable +3% final power, Dwarven +5% armor pierce, Elven +5
  Accuracy, Boreal Bolt +5% base power, Abyss +5 Crit Rating, Hellmade +5% secondary scaling, Hearthforge +3 Accuracy and
  +3 Crit Rating, Outlaw nothing; not staffs, wands, armor or shields. `totals` adds it; the parts list shows it after
  the parts ("Matching bonus · Every part by …").
- **Tests:** `test-client/tests/small-changes-browser.js`, one entry per rule (`runPurseDevModeChecks`,
  `runDevModeTextChecks`, `runTemporaryBoostChecks`, `runEquipmentFillChecks`, `runTargetHighlightChecks`,
  `runLevellingCurveChecks`, `runDragonbloodDropChecks`, `runMatchingBonusChecks`; `runSmallChangeChecks` runs all);
  each fails on the game as it was before. `cinder-bosses` and `adventure-improvements` no longer set temporary bonuses.

**Simulator fix and re-tune (2026-10-11):** `claude-work/ny-sim.js` used to pile every level above 20 into one class past its cap, so parties above Level 20 were badly under-built (6 wasted levels at 26, 15 at 35, 27 at 47). Archetypes now have a `growth` list of fitting classes filled up to each class's cap and level requirement (`ju-sim.js` got the same fix). With fair parties the four earlier bosses won 90–100%, so the user had all four re-tuned (96-fight batches): Juila `maxHpPercent` 210 → 335 (prepared L25 43%); Nygheir 75 → 136 (prepared L26 45%; his check asserts 136); Zik Lun 240 → 455 (standard L26 90%); Deep Sea King 260 → 740 and Dex 58 → 70 (prepared L35 33%; Dex 72–80 drops him to ~17–21%, a cliff). Older balance lines above give pre-fix numbers. `ny-sim.js` takes `OVR='{"<enemy name>":{"maxHpPercent":…,"stats":{…}}}'` for trial numbers without editing enemy files; `claude-work/rt-launch.sh <tag> <ju|ny|zl|dk> <runs> '<override>'` runs one boss with its agreed party.

**Work files:** the session scratchpad was removed mid-session. Use `~/.cache/stat-fick-build/claude-work` for test scripts and the extracted test client (`client/regress.sh` runs the full regression).

**Crit system (2026-10-08, user request):** the 40% cap is gone.
- All crit sources add Crit Rating: the weapon type's base (`ForgeRules.CRIT`; non-forged weapons use 5), 0.15 × Luck, another 0.1 × Luck for weapons that scale with Luck, and class, artifact, part and rune bonuses.
- `CombatRules.critChanceFromRating` turns rating into a chance: 100 × (1 − e^(−rating/100)), at most 99%.
- Crit momentum: `critRoll` and `critChanceAfterMisses` use a pseudo-random distribution, tracked in `actor.critMisses`.
- Weapon types add crit damage (`item.critDamage`). The total crit bonus stays capped at +75%.
- The user tuned for roughly 10–20% crit on ordinary builds and 70–85% on dedicated crit builds.

**Visual testing technique:** Run `tests/Stat Fick Tests.AppImage --script=<file.js> --call=<fn>`. In the script, call `window.statFickDesktop.captureTestScreen(name, w, h)`; it writes `<profile>/<name>.png`, and the profile path is in the report. View the PNG with Read.

**Scratch files don't last:** /tmp, including the session scratchpad, is cleared on reboot. To run the bundled browser checks, extract `tests/*-browser.js` from the test client's `app.asar` (Python can parse the asar header). Pass the file to `--script` and its zero-argument `async function` to `--call`.

**Regression baseline:** There are 38 bundled browser check files inside the test client's `app.asar` under `tests/`. Call each file's zero-argument entry: `cinder-cinematic` has two (`previewDreamerCinematic`, `previewForgeCinematic`), and its first function is a helper that fails without arguments. 30 pass and 8 fail for reasons that predate the battle UI:
- `combat-lifecycle` and `critical` only work inside their own suite.
- The `masterwork-rarity`, `music`, `newc` and `shop` checks assert stale UI.
- `talos-edge` is the Barbarian failure.
- `trap` fails too.
- Since phase 5 (2026-10-09), 28 pass and 10 fail. Three checks are stale on purpose (the test client can be patched; see the phase 5 note):
  - `bloody-mary` asserts the removed 40% crit cap.
  - `settings-companions` looks for the "Hit chance" label, which is now "Accuracy".
  - `adventure-improvements` presses S for the removed Spell Shop.
A change is safe when these results stay identical.
- 2026-11-03, a cloud session (Xvfb, lavapipe): every zero-argument function of the 41 files in `test-client/tests/` run
  as its own entry (`chromium.sh old-game/test-client --script=<file> --call=<fn>`, three at a time), plus the smoke,
  enemies, lifecycle and combat suites: 57 runs, 44 pass and 13 fail. Failing: `adventure-improvements`
  (`runAdventureImprovementsChecks` and `runMapImprovementChecks`: a missing element), `bloody-mary`, `combat-lifecycle`
  (only inside its suite), `music`, `newc`, `settings-companions`, `shop`, `talos-edge` and the combat suite (both the
  Barbarian failure), `trap`, and two flaky ones that passed on reruns: `map-speed` (travel timing) and
  `runCompanionImportChecks`. The small changes of that day left these results the same (the flaky two passed), and
  added `small-changes-browser.js` (9 entries, all passing).

**Known failure:** The combat suite fails "Legacy bonus-choice cancellation makes no changes and does not spend money". The cause is the 2026-10-06 edit that made Barbarian level 7 grant both STR and VIT +5% instead of a choice. The Church of Talos prompt for older saves that never recorded the choice became unreachable. The failure stops the combat suite early, so run `enemies` and `lifecycle` separately.

**Why:** These facts decide how changes get built and verified.
**How to apply:**
- After changes, run the smoke suite (~1s) plus the relevant suites.
- Rebuild the executables only when `desktop/` changes. Game-file edits apply live.
- Expect the Barbarian failure until the user decides how to handle it.

## Boss difficulty (user rule, 2026-10-11)

From the Deep Sea King rework on, every Stat Fick boss rework should be tuned a bit harder than the earlier reworks (user, 2026-10-11: "From here on out, the difficulty for bosses should be raised a bit").

**Why:** The earlier reworks (Ji'ruan, Juila, Nygheir, Zik Lun; see the old game's notes (old-game/Docs/Notes.md)) set the reference points, and the user wants later bosses tougher than those.

**How to apply:** For each later boss, set the simulator win-rate target lower than the matching earlier reference:
- Raid fights: Juila and Nygheir were tuned to about 40–47% for prepared parties 3 levels above the boss.
- Regular Great Hunts: Ji'ruan reached about 88% and Zik Lun about 92% for standard parties 2 levels above.

Say in the report how much harder the new target is, and still ask the user about each boss's difficulty band.

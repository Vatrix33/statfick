# Stat Fick: content and asset inventory for the port

Survey of `old-game/` (the reference game) and the engine docs, read-only, on `main` at `29d110d` (2026-11-12).
Paths are relative to the repo root (`/run/media/system/M.2_2TB/Stat Fick Repo/`). Counts were taken from the files
themselves (JSON parsed, JS tables read as text); where a number comes from a doc instead, the doc is named.
No milestone recommendations here: this is the material the content milestones are sized from.

---

## 1. Content counts

"Old game" is what exists and runs today. "Port adds" is what the approved port-task drafts and decisions add on top
(`engine/Docs/Port-Tasks/README.md`, `engine/Docs/Engine-Plan.md`); nothing in that column exists as code yet.

| Content | Old game today | Port adds (approved, not built) | Source |
|---|---|---|---|
| **Classes** | 130 files: 104 no colour, 18 blue, 6 purple, 2 gold (Demon Lord, Hero). 4 not for players (Beast, Great Beast, Monster: `race-playability`; Raid Boss: `unobtainable`), so 126 for players. 2 came from port task 9 (Alchemist, Assimilator) | Drafts hold **256** (164 no colour, 66 blue, 18 purple, 6 gold, 2 red), 252 for players: 130 reworked + 40 new (task 10) + 3 racial (task 11) + 2 (task 12) + 79 (task 13) + 2 (task 14). Every class feature to be tested after the port (C32) | `old-game/classes/*.json`; `engine/Docs/Port-Tasks/10_Class_Rework/` |
| Class levels | 738 levels in all; max level per class: 1 (×1), 3 (×18), 4 (×14), 5 (×34), 6 (×16), 7 (×31), 8 (×2), 9 (×12), 10 (×2). 142 skills, 127 passives (nested counted), 131 distinct effect types, 41 kinds of "other" requirement | Features for all 256 level by level in `Class-Rework-Features.md` (277 KB) | same |
| Class drafts (user's own) | `classes/newc/`: 15 `.txt` drafts + a template + 6 under `IGNORE/`; 7 of the names went into task 10 | — | `old-game/classes/newc/` |
| **Enemies** | **174** files (+ `index.json`). By race: elemental 28, dragon 22, undead 20, human 11, demon 9, troll 8, wolf/orc/harpy/giant/goblin 7 each, merfolk 6, griffon/dryad 5, impid/devil 4, bear/lizardman/treant/lamia/satyr 3, siren/dark-elf 1. 136 types sit in the regional encounter tables (the 12 elemental dragons through a spread); the other 38 are the 12 bosses, their retinues, summons and Juila's 4 season spirits. 6 added by port task 6 (Abyssal Rift) | Every enemy revamped for the reworked classes and spells (general port task, C2, SP16); 2 new summon creatures (Mana Golem, Storm Roc, task 12); dungeon-only enemy types for 12 dungeons (task 14, DG9); the enemies of the 3 new regions (task 8, W27) | `old-game/enemies/`; `scripts/encounter-engine.js` |
| **Bosses** | **12**: 10 Great Hunts (Ji'ruan 18, Juila 22, Nygheir 23, Zik Lun 24, Deep Sea King 32, Gaian 53, Saffron 66, Ool 78, Ur-Mat 90, Anatemis 100) + 2 site bosses (Tankina 44 at the Church of Talos, Ornn 48 at Hearth Home). 17 scripted AI profiles (bosses and minions) | +11 dungeon bosses (8 named by the task, Nocturne Keep's vampire lord, a harpy and a dragon boss; DG27, DG28, DG58); Saffron and Ool move from Great Hunts to dungeon bosses | `scripts/guild.js` 148–225; `combat.js` 5850–6007 |
| **Regions** | 17 charted + 3 placeholders ("Uncharted land": highlands, steppe, delta; walkable, empty) on one continent; each region 1600 × 1000 local units ≈ 168 × 105 km after the 1.4× enlargement | The 3 new regions in full: names, terrain, enemies, levels, towns, connections (W27) | `scripts/world-region-data.js`, `world-engine.js`, `continent-engine.js` |
| Encounter zones | **89** zones with encounter tables (+ safe "Town"/"Road") | dungeon stages (pools of groups per stage, DG7) | `scripts/encounter-engine.js` |
| **Settlements** | **27**: 11 castle cities, 16 towns. Plus 3 sites (Church of Talos, Institute of Magic on Mirrormere's island, Hearth Home on Cinder Isle's volcano) | towns of the 3 new regions; 12 dungeon entrances | `world-region-data.js`, `world-engine.js` 3–8 |
| **Dungeons** | 0 | **12** (9 named + 3: harpies 50–56, Nocturne Keep 66–70, dragons 85–92), each with stages and a boss, a 3D overview scene each (DG69) | `Port-Tasks/14_Dungeons.txt`, DG1–DG78 |
| **Races** | 28: 9 playable (Human, Elf, Dark Elf, Dwarf, Hobbit, Lizardman, Half-Beast, Goliath, Merfolk), 19 enemy-only | — | `objects/races.json` |
| Playable looks | 72 (9 races × 2 sexes × 4 looks); 24 of them (Half-Beast, Merfolk, Goliath) placeholder | — | `scripts/sprite-catalog.js` 12–152 |
| Race seals | 9 SVG files (3 placeholder: half-beast, goliath, merfolk) | — | `objects/race-*.svg` |
| Companions | party of you + up to 3; recruits at taverns (castle 5 offers, town 2); recruit names: 9 races × 10 male + 10 female first names + family names | — | `scripts/companions.js` 74–121 |
| **Spells** | **388**: 12 elements (Basic 34, Lightning/Dark/Nature 33, Fire/Water/Earth/Wind/Holy/Blood/Death 32, Ice 31); tiers 1–10 (49/50/51/50/55/37/36/24/24/**12**); 257 Damage, 131 Support; 29 summon effects (28 creatures); 381 learnable + 7 boss/enemy-only | 381 reworked + **168 new** (14 per element) = 549 learnable + the 7 unchanged; Spell Augmentation (a magic circle per element); the Tier 10 animations reworked (SP47) | `objects/spells.json`; `Port-Tasks/12_Spell_Rework/` |
| Elements | 12: basic, fire, water, holy, lightning, blood, dark, ice, earth, wind, nature, death (`SPELL_ELEMENTS`, `app.js` 249) | — | |
| Status effects | No central table: timed records on `actor.effects` in `combat.js` (stun, mark, taunt, burn, bleed, poison, root, stat/defense buffs and debuffs, retaliation, healing-received, spell-miss, flee-disabled, stances) | **12 element statuses + 12 reactions** (Exposed, Burning, Soaked, Cracked, Off-Balance, Shocked, Chilled/Frozen, Blessed, Hexed, Rooted, Withered, Bleeding); 28 spells borrow another element's status; "how each looks and sounds in battle is made in the port" | `Port-Tasks/12_Spell_Rework/Element-Statuses.md` |
| **Artifacts** | **85**: 32 uncommon, 24 rare, 16 epic, 8 legendary, 5 masterwork (Second Wind, Wraithguard, Arrowtip Rain, Titanfall, Treasure Seeker). All 85 `placeholderArt: true`; looks from 50 shapes × 76 glyphs | — | `objects/artifacts.json` |
| **Masterwork weapons/armor** | **32** (22 weapons, 10 armor/shields), 11 with placeholder looks; **5 Exotics** (Fletcher, Tough-Out, Winged Hussar, Cherubim, Connected Balancer), all placeholder | +3 masterworks, +1 Exotic (task 14, DG37, DG38) | `scripts/forge-engine.js` 1101–1383 |
| Natural weapons | 4 (Beast Claws, Sunhorn Antlers, Vulcanic Ram Horns, Stardragon Claws) + Half-Beast Natural Claws (unarmed) | — | `forge-engine.js` 1524–1529 |
| Forging system (procedural items) | 8 makers; 23 weapon types (21 + staff + wand); 39 weapon materials (9 common, 7 uncommon, 8 rare, 7 epic, 6 legendary, Mythilium, Exium); 12 caster handles; 12 stones × 3 tiers; 12 inks; 12 armor types (3 heavy, 3 light, 3 clothing, 3 shields); 48 armor materials; 24 runes; 7 rarities (common … masterwork, exotic). Every weapon and armor piece is generated: the item id encodes its recipe | smithing materials (weapon, armor, staff, wand parts) as items with a 75% reforge discount (DG26, DG29–DG32); a red class rarity look (C16) | `forge-engine.js` |
| **Consumables** | **71**: 50 potions (10 Health/Mana + 40 new), 12 throwable poisons, 8 traps (5 from task 3), Camping Supplies | 11 throwing items + 4 extra (task 14, DG39, DG43) | `objects/consumables.json` |
| Ingredients | **56** (15 common, 18 uncommon, 13 rare, 6 epic, 4 legendary) | — | `scripts/alchemy-data.js` 8–65 |
| Recipes | **62** (3 ingredients each; 2 known at start) | — | `alchemy-data.js` 67–130 |
| Key items | 5 (Gem of False Dreams, Vial of Dragonblood, Blood Crystal, Shadow Crystal, Phylactery Core) | Moonfang (task 13) | `objects/key-items.json` |
| **Quests** | 7 standard types (Hunt, Cull, Deliver, Search, Escort, Travel, Collect) on daily boards (castle 4, town 3 a day) + Great Hunts (10). No story questline | Delve (task 14), Ravenwatch's Nocturne Keep quest (DG55) | `scripts/guild-quest-engine.js`, `guild.js` |
| Shops and services | Blacksmith (castle 9 / town 6 pieces), Tailor (6/4), General Store (8/5), Magic Tower (castle cities: 6 casters, 12 spells, every rune), Apothecary (every settlement), Guild (board, counter, artifacts, tavern, hooded man), Church Storm Reliquary (6 casters, 8 spells), Institute of Magic, Hearth Home (5 weapons + 4 armor), 4 reforge anvils, caravan, horse carriage | — | `scripts/shop.js` 11–30, `magic-tower.js`, `church-of-talos.js`, `apothecary.js`, `hearth-home.js` |
| **Cutscenes** | 19 boss intro scenes (12 intros + 7 phase scenes); 3 inline special phase cinematics in `index.html` (storm-talos, false-dreamer, forge-god); 12 Tier 10 spell vignettes; the boat trip on the map | the other 11 boss intros, phase/special cinematics, 12 vignettes and banners "made at port time" (M10-2); 11 new boss intros (task 14); reworked Tier 10 animations | `scripts/boss-intros.js`, `tier10-cinematics.js`, `index.html` 653–690 |
| Interior scenes ("banners") | 13 variants from 11 drawings, full window 1280 × 900: Blacksmith ×2, General Store ×2, Magic Tower, Church, Guild ×2, Tailor, Institute (= Tower), Hearth Home (= arms hall), Apothecary ×2. 7 are placeholder | remade in Blender (2026-10-16) | `scripts/scene-banners.js` 1360–1370 |
| Battle backgrounds | 33 biomes (zone → biome table), time of day, ambient motion | 3D stages built in Blender | `scripts/battle-scenery.js` 12–71 |
| **Music** | **55 themes**: 17 region themes, 1 battle theme + 17 regional arrangements, 20 boss themes (12 bosses, with phase themes). 34 instruments, 21 percussion voices, 7 rooms, 10 modes. All synthesized live. 43 MP3s in `music/` (63.2 MB) are offline exports the game never loads | themes for dungeons (DG34), 11 new bosses, 3 new regions | `scripts/music-themes.js`, `music-composer.js` |
| **Sound effects** | **0** (the old game has music only) | the port adds proper sound effects (user, 2026-11-03); the list is asked at port time | `engine/Docs/Audio.md` |
| UI | `index.html` 1004 lines (401 ids, 166 buttons, 24 modal layers + overlays); `styles.css` 4184 lines, 423,161 B, ~3,527 rule blocks, ~1,051 class names, 245 `@keyframes`, 101 section comments | the stylesheet copy in the engine's tests is older (3915 lines) | |
| Fonts | 5 faces: Lato Regular/Semibold/Bold (TTF, 657–669 KB each), STIX Two Text Regular/Italic (OTF, 292/308 KB), + 2 OFL licences | already in the engine's `ui.pak` (999 KB) | `objects/fonts/` |

Encounter content per region (zones with tables, enemy types met there, settlements):

| Region (levels) | Zones | Enemy types | Settlements and sites |
|---|---|---|---|
| Alder Vale (1–15) | 5 | 18 | Alderkeep (castle), Millhaven, Willowford, Briarfield |
| Verdant Reach (5–20) | 4 | 28 | Greenwatch (castle), Heartgrove |
| Stoneveil Foothills (10–20) | 8 (4 "near Town") | 20 | Crownspire (castle), Southwatch, Stonefoot |
| Sunward Coast (15–30) | 5 | 18 | Westport (castle), Dunehaven, Seabridge (boat to Cinder Isle) |
| Stormwake Hills (15–35) | 4 | 13 | Stormhold (castle), Rainford, Brackenrise; Church of Talos |
| Mirrormere Basin (20–40) | 5 | 16 | Lakespire (castle); Institute of Magic |
| Cinder Isle (25–40) | 5 | 11 | Emberhaven; Hearth Home |
| Saffron Expanse (35–50) | 5 | 10 | Miragehold (castle), Dunecross |
| Gloamfen Marsh (35–55) | 5 (incl. Abyssal Rift) | 12 | Mirewatch (castle), Reedcross |
| Crimson Divide (40–55) | 6 | 11 | Amberpost |
| High Stoneveil (45–60) | 5 | 21 (9 + 12 elemental dragons in Dragonspine) | Cairnrest |
| Deep Desert (45–55) | 5 | 8 | Lastlight |
| Verdigris Jungle (45–65) | 5 | 5 | Canopyhold (castle) |
| Bloodstone Coast (55–70) | 6 (incl. Vampire Citadel, 100% encounters) | 8 | Nocturne Keep (castle, vampires only) |
| Duskpine Forest (60–75) | 5 | 9 | Ravenwatch (castle) |
| Ancient Battlefield (65–75) | 6 | 8 | none |
| Stoneveil Summit (85–95) | 5 | 5 | none |
| 3 uncharted regions | 0 | 0 | none |

Enemies outside the encounter tables (38): the 12 bosses; retinues (Apostle of Talos, Thunder Boar Gorthok, 3 Great
Devil Servants, Grove Matriarch, Jungle Core, Phylactery of Ool, Troll Shieldbearer, Troll War-drummer, Void Imp,
Vulcanic Ram); Juila's season spirits (Rime Wolf, Sunhorn Stag, Blossom Sprite, Harvest Wisp); summons (Fire Spirit,
Undead Thrall, Undead Hulk, Hemoghost, Radiant Sentinel, Void Stalker, Pale Monarch, Ice Elemental, Steelguard Golem);
Earth Spirit (has a level, drops and a sprite but appears in no table and no summon effect).

---

## 2. Data schemas (fields, briefly)

**Class** (`classes/*.json`, one file each; the port keeps classes as `.json`, Engine-Plan 2026-10-16):
`id`, `name`, `color` (only on 26 files: blue, purple, gold), `maxLevel`, `requirements { level, classes {id: level},
anyClasses, excludes, other }` (`other` is one object or a list; 41 kinds such as `known-spells`, `race`,
`stat-threshold`, `distance-traveled`, `potions-brewed`, `unobtainable`), `levels [ { level, description, effects [] } ]`.
An effect is `{ type, … }` (131 types: `stat-permanent-percent`, `spell-power-percent`, …); `type: "passive"` and
`type: "skill"` nest their own `effects` and carry `id`, `name`, `description`, `condition` or `combatOnly`,
`cooldown`, `durationTurns`, `actionPoints`.

**Enemy** (`enemies/*.json`, listed in `enemies/index.json`): `format: "stat-fick-enemy"`, `version: 1`, `id`
(`adult-wolf-beast-level-5`), `name`, `level`, `raceId`, `classLevels {classId: level}`, `stats` (the 8: strength,
vitality, mind, dexterity, intelligence, wisdom, speed, luck), `inventory []`, `equipment { primaryHand, offHand, armor,
artifact1, artifact2 }` (items are templates such as `forge-weapon-1-longsword+rapier`, forged at spawn),
`knownSpells []`. Optional: `drops [{itemId, chance}]`, `naturalWeapon`, `element`, `resistances`, `perkPoints`,
`availablePoints`, `manualAllocations`, `permanentBonuses`, `permanentDefenseBonuses`, `activeSkills`, `currentHp`,
`currentMana`, `maxHpPercent`, `baseArmor`, `aiProfile`, `unique`, `introCinematic`, `noDropItems`,
`phaseTransitions`, `startingAllies`, `attackElement`, and one-off boss blocks (`heartCores`, `warband`, `dreamLaws`,
`oldWounds`, `storm`, `starTrials`, `royalDuel`, `reforge`, `infernalSacrifice`, `seasonCycle`, `phylactery`,
`startingSummons`, `unlivingCaster`). Bosses also have Limitless class sets (`limitless-engine.js` `BOSS_CLASSES`).

**Items.**
- Forged weapons/armor have **no records**: the id is the recipe, resolved by `ForgeRules.item(id)`:
  `weapon-forged-<type>-<lvl>-<mk><mat>-<mk><mat>-<mk><mat>-s<slots><runes>`;
  `weapon-forged-staff-<lvl>-<mk><stone><tier>-<mk><handle>-<mk><ink>`;
  `armor-forged-<type>-<lvl>-<mk><mat>×3-s<0-2><runes>`; masterworks `weapon-masterwork-…`/`armor-masterwork-…`;
  enemy templates `forge-weapon-<1|2>-<type>+<type>~<rarity>@…`. Masterwork/Exotic definitions are entries of
  `MASTERWORKS` (name, type, parts, fixed runes, effects, `exotic`, `placeholderArt`, `color`).
- Artifact: `id`, `name`, `rarity`, `priceSilver`, `description`, `effects [{kind, target, amount, …}]`,
  `look {shape, glyph}`, `placeholderArt`; masterwork ones add `shopAvailable`, `automaticDrop`, `priceRamp`.
- Consumable: `id`, `name`, `description`, `rarity`, `priceSilver`, `effects`, and `potion`/`poison`, `alchemy`,
  `outsideBattle`, `element`, `potionResource`, `mapAction`.
- Key item: `id`, `name`, `type: "key-item"`, `rarity`, `description`, `priceSilver`, `catalogueAvailable`,
  `shopAvailable`, `playerSellable`.
- Rune items are `rune-<id>` over the 24 `RUNES`.

**Spell** (`objects/spells.json`, one array): `id`, `name`, `element`, `tier`, `type` (Damage/Support), `manaCost`,
`priceSilver`, `basePower`, `primary {stat, multiplier}`, `secondary`, `description` (with `[X]`), `effects [{type,
amount, recipient, enemyId (summons), …}]`; optional `actionPoints`, `unique`, `requiredCasterEnemyId(s)`,
`seasonVariants`, `catalogueAvailable`, `shopAvailable`.

**Potion, poison, ingredient, recipe.** Potions/poisons are consumables (above). Ingredient: `{id, name, rarity,
priceSilver, description}`. Recipe: `{id (the potion), ingredients [3]}`. Forage: region → zone → `[{id}]`, plus the
rivers' list. Drops: enemy id → `[{id}]`; boss drops: boss id → `[ids]`.

**Race** (`objects/races.json`): `id`, `name`, `playable`, `icon` (a seal SVG path or null), `statModifiers` (8),
`passive {id, name, condition, description, effects}`.

**Region** (one per region, spread over several files):
- Geometry and places (`world-region-data.js`; the Alder Vale in `world-engine.js`): `id`, `name`, `eyebrow`,
  `title`, `description`, `levelRange {minimum, maximum}`, `start`, `settlements [{id, name, kind, x, y,
  guildServices, requiresClass, boat}]`, `sites [{id, name, kind}]`, `roads` (polylines), `river`, lake/island/bridges,
  zone areas (ellipses), `exits [{id, name, point, retreat, targetRegionId, targetPoint, prompt, destinationName}]`,
  `safeRetreatRegionId`, `uncharted`.
- Placement on the continent: `continent-engine.js` `ORIGINS` (line 21); land field `continent-shape.js` (baked).
- Encounters: `chances` (zone → %), `profiles` (zone → `{minimum, maximum, safe}`), `tables` (zone →
  `entry(level, weight, () => [enemy names])`).
- Look: `map-regions.js` `SPECS[id]` (a field function giving colour, height, water, forest, pine, rock, wet, sand,
  snow, farm, glow per point; `cover`, `treeKind`, `decorate`, `labels`); battle biome (`battle-scenery.js`); music
  theme; forage table; guild zone anchors (`guild.js` `regionalZones`).

**Quest** (saved in `worldState.guild.quest` / `.greatHunt`, `world-engine.js` 64–130): `type` (hunt, cull, deliver,
search, escort, travel, collect, great-hunt; old `clear` retired), `version` (2), `title`, `description`, `enemy`,
`zone`, `regionId`, `originRegionId`, `originCastleId`, `townId`, `uniqueId`, `offeredAtLevel`, `level`, `offeredDay`,
`progress`, `complete`, `required`, `radius`, `reward`, `point`, `destination {regionId, settlementId, name, point}`,
`recommended`, `ambushes [{at, done}]`, `travelled`, `tripMinutes`, `ingredientId`, `ingredientName`, `taken`.
Great Hunt definitions (`guild.js` 148–225): `id`, `enemy`, `level`, `regionId`, `offerRegionId`, `zone`, `zones`,
`roster`, `description`. Quest rules and numbers: `guild-quest-engine.js` (`TYPES`, `FACTORS`, `OFFERS`, `RECRUITS`,
`CULL`, `SEARCH`, `AMBUSH`, `AMBUSH_GROUPS`).

Per Engine-Plan (M9 decisions): classes stay `.json`; "the old game's tables (enemies, spells, artifacts, consumables,
key items, races) are converted at port time, when their C# records exist, checked against the old game in Chromium
as the music is"; the port's own record kinds live in a data library `Statventure.Game.Data` (M12-9).

---

## 3. Where each content table lives

All under `old-game/`. Line ranges are the table literal's first and last line.

| Table | File, lines | Count |
|---|---|---|
| Class files | `classes/*.json` | 130 (+ `classes/newc/` 22 text files) |
| Class file list and cache keys | `scripts/app.js` 17–149 (`CLASS_FILES`), 150–247 (`CLASS_CACHE_KEYS`) | 130 |
| Enemy files | `enemies/*.json`, `enemies/index.json` | 174 |
| Enemy levels | `scripts/encounter-engine.js` 38–82 (`enemyLevels`) | 126 names + the 12 elemental dragons |
| Elemental dragons | `encounter-engine.js` 24–37 (`dragonElements`) | 12 |
| Animal families | `encounter-engine.js` 83–105 | — |
| Encounter tables (Alder Vale) | `encounter-engine.js` 3–23 (chances, profiles), 106–206 (`tables`) | 5 zones |
| Encounter tables (16 other regions) | `encounter-engine.js` 208–881 (`<region>Chances/Profiles/Tables`) | 84 zones |
| Region → rule sets | `encounter-engine.js` 882–904 (`regionRuleSets`) | 20 |
| Boss Limitless classes | `scripts/limitless-engine.js` 77–89 (`BOSS_CLASSES`) | 11 bosses |
| Boss/minion AI | `scripts/combat.js` 5850–6007 (`AI_PROFILES`) | 17 |
| Great Hunts | `scripts/guild.js` 148–225 (`greatHunts`) | 10 |
| Guild zone anchors | `guild.js` 44–145 (`regionalZones`) | 17 regions |
| Guild quest rules | `scripts/guild-quest-engine.js` 9–33 | 7 types |
| Site bosses | `scripts/church-of-talos.js` 6–7, `scripts/hearth-home.js` 8 | 2 |
| Regions (16) and settlements | `scripts/world-region-data.js` 28–412 (`WorldRegionData`) | 16 + 3 uncharted |
| Alder Vale | `scripts/world-engine.js` 3–8 (settlements), 295 on (`alder`) | 1 |
| Continent placement | `scripts/continent-engine.js` 21 (`ORIGINS`); `scripts/continent-shape.js` (baked land, 261 KB) | 20 |
| Region map looks | `scripts/map-regions.js` 135–1062 (`SPECS["…"]` from line 145) | 17 |
| Terrain feature painters | `scripts/map-terrain.js` 625 on (`DRAW`) | 41 |
| Map ink symbols | `scripts/map-ink.js` (`#ink-*` defs) | 22 |
| Map site icons | `scripts/map.js` 377–381 (`SITE_ICONS`); castle/village art 55–66 | — |
| Races | `objects/races.json` | 28 |
| Playable looks | `scripts/sprite-catalog.js` 12–152 (`RACES`) | 72 |
| Enemy sprite specs | `sprite-catalog.js` 199–507 (`ENEMIES`) | 174 |
| Recruit names | `scripts/companions.js` 74–121 (`names`) | 9 races |
| Spells | `objects/spells.json` (279 KB) | 388 |
| Tier 10 vignettes | `scripts/tier10-cinematics.js` 10–59 (`scenes`) | 12 |
| Boss intros | `scripts/boss-intros.js` 8–493 (`INTROS`); timelines in `styles.css` 1623–2310 | 19 |
| Special phase cinematics | `index.html` 653–690, `combat.js` 1213 (`playSpecialCinematic`) | 3 |
| Artifacts | `objects/artifacts.json` (40 KB) | 85 |
| Consumables | `objects/consumables.json` (30 KB) | 71 |
| Key items | `objects/key-items.json` | 5 |
| Ingredients | `scripts/alchemy-data.js` 8–65 (`INGREDIENTS`) | 56 |
| Recipes | `alchemy-data.js` 67–130 (`RECIPES`), 132 (`STARTING_RECIPES`) | 62 |
| Forage tables | `alchemy-data.js` 134–247 (`FORAGE`), 249 (`RIVER`) | 17 regions |
| Ingredient drops | `alchemy-data.js` 251–413 (`DROPS`), 415–428 (`BOSS_DROPS`) | per enemy, per boss |
| Makers | `scripts/forge-engine.js` 19–36 (`MAKERS`) | 8 |
| Weapon materials | `forge-engine.js` 76–118 (`MATERIAL_ROWS`) | 39 |
| Caster handles, stones, inks | `forge-engine.js` 124–138, 142–155, 160–164 | 12, 12, 12 |
| Weapon types | `forge-engine.js` 196–221 (`WEAPONS`) | 23 |
| Runes | `forge-engine.js` 228–253 (`RUNES`) | 24 |
| Armor types | `forge-engine.js` 800–813 (`ARMORS`) | 12 |
| Armor materials | `forge-engine.js` 841–890 (`ARMOR_MATERIAL_LIST`) | 48 |
| Masterworks and Exotics | `forge-engine.js` 1101–1383 (`MASTERWORKS`) | 32 + 5 |
| Natural weapons | `forge-engine.js` 1524–1529 | 4 |
| Retired item ids (save migration) | `forge-engine.js` 1575–1619 (`LEGACY_ITEMS`) | — |
| Shop sizes | `scripts/shop.js` 11–30 (`SMITH`, `TAILOR`, `GENERAL`, `TOWER`, `SHOPS`, `HEARTH`) | — |
| Institute / Storm Reliquary | `scripts/magic-tower.js` 25–30; `church-of-talos.js` 109–112 | — |
| Interior scenes | `scripts/scene-banners.js` 1360–1370 (`SCENES`, `SIZES`, `PLACEHOLDER_ART`) | 13 variants |
| Battle biomes | `scripts/battle-scenery.js` 12–46 (`BIOMES`), 48–53 (`REGION_BASE`), 54–71 (`ZONES`) | 33 |
| Element effect colours | `scripts/battle-effects.js` 9–23 | 12 |
| Item art palettes, logos, artifact styles | `scripts/forge-art.js` 17–58 (`PALETTES`, 39), 554–563 (`LOGOS`, 8), 789–806 (`RUNE_GLYPHS`, 24), 921–926 (`ARTIFACT_STYLE`), 945–995 (`ARTIFACT_SHAPES`), 1004–1013 (`MASTERWORK_ARTIFACTS`, 5) | — |
| Sprite metal palettes | `scripts/sprite-kit.js` 139–172 (`MATERIALS`) | 32 |
| Music: instruments, percussion, rooms | `scripts/music-composer.js` 159–194 (`I`), 196–218 (`P`), 219–224 (`ROOMS`), 11–16 (`MODES`) | 34, 21, 7, 10 |
| Music: themes | `scripts/music-themes.js` 20–1015 (regions), 1016–1230 (battle), 1231–1259 (arrangements), 1260–2486 (bosses), 2487–2514 (`LOUDNESS`) | 55 |
| Original scores (dropped by the engine) | `scripts/music.js` 20 on (`scores`) | 24 |

---

## 4. Art

### 4.1 How it is made today

**Everything visual in the old game is code.** There are no raster images in the game: no PNG/JPG is referenced by
`index.html`, `styles.css` or any script. The only image files are the 9 race seal SVGs, the app icon
(`objects/stat-fick-icon.svg`, 643 B; `desktop/icons/icon.png` 251 KB, `icon.ico` 370 KB), a schematic of the old
regional connections (`tests/Regional Map Layout.png`, 2700 × 2595, pre-task-8, not used by the game) and the task 8
draft picture (`engine/Docs/Port-Tasks/8_Map_Rework/continent-draft-6.png`, made by `draft6.py`). All of it was
written as SVG/canvas code in the game's sessions; nothing is an artist's hand-drawn raster.

The style (Notes, 2026-10-07/08): the user asked for something close to Darkest Dungeon: a "gothic ink" look with
heavy near-black outlines (`#130e0b`, ink `#0d0907`), a muted earthy palette (`K.tone` mutes every colour; a `"!"`
prefix opts glows, eyes and fire out), solid ink shadows clipped to each shape and fine hatching; the UI is a
"Gothic ink theme" (parchment in light mode, stone in dark mode; the user plays dark).

**Characters and enemies** (`scripts/sprite-kit.js`, `sprite-humanoid.js`, `sprite-creatures.js`,
`sprite-equipment.js`, `sprite-catalog.js`; 20 + 139 + 78 + 54 + 71 KB): procedural SVG rigs built from a spec.
- *Humanoid rig*: one parameterised body for every playable race and the humanoid monster races: proportions (`build`),
  skins and 15 body materials (stone, bark, fire, ice, water, wind, lightning, holy, shadow, magma, metal, bone,
  sandstone, nature, blood), ~76 part styles (hair, beards, headwear, eyes, ears, horns), lower bodies (legs, goat,
  bird, fishtail, snaketail, float, roots, trunk), arms (normal, wings, branches), tails and wings. Every limb is a
  pivot group (`sp-upper`, `sp-head`, `sp-leg-front/back`, `sp-tail`, `sp-wings`) animated by CSS keyframes (idle
  life, swing, guard, cast, recoil).
- *Creature rigs*: quadruped (wolf, bear, boar, ram, stag), griffon, dragon (dragons, drakes, wyrms), wisp (spirits,
  ghosts), core (Jungle Core seed, Ool's phylactery). Boss extras: old wounds, lodged weapons, tattered wings, horn
  crowns.
- Of the 174 enemy sprites, 116 use the humanoid rig and 58 the creature rigs (dragon 22, wisp 13, wolf 8, griffon 6,
  bear 4, other quadrupeds 3, core 2). Bosses are custom specs on the kits; Juila has 4 season looks, Nygheir a phase
  look, Anatemis three forms.
- *Equipment layers* (`sprite-equipment.js`, `forge-art.js`): weapons drawn in their own frame (17 sprite shapes:
  sword, axe, bow, club, crossbow, dagger, greataxe, greatbow, greatsword, hammer, longbow, mace, rapier, spear, staff,
  trident, wand) in the maker's style and the material's palette; armor as layers on the humanoid (torso, sleeves,
  gauntlets, greaves, skirts, head pieces by maker, capes); unique sprites for 7 masterwork armors and the
  Stardragon Claws.

**Item art** (`scripts/forge-art.js`, 116 KB): composed, not individually drawn.
- Forged weapons and armor: every part drawn in its maker's style (Roundtable classic, Dwarven blocky, Elven
  leaf-like, Boreal Bolt bare, Abyss jagged, Hellmade ornate, Hearthforge folded, Outlaw lashed) and its material's
  palette (39 palettes); 8 maker logos; 24 rune glyphs; stones and inks in element colours.
- Artifacts: one shared material per rarity (bronze/plain glass, silver/blue-white gem, gold filigree/violet gem,
  white gold/glowing core and halo) × 50 shapes × 76 glyphs; 5 masterwork artifact drawings.
- Potions by tier shape and effect colour (skull on poisons); ingredients by name keywords (cap, mineral, fang,
  feather, scale, liquid, seed/berry, bark, clay, flower, leaf).
- General Store goods art; the carriage (`shop.js` `CARRIAGE_ART`); the reforge anvil (`reforge.js`); the brewing
  cauldron and its animation (`apothecary.js`, `styles.css`).

**The map** (`map.js` 109 KB, `map-terrain.js` 64 KB, `map-regions.js` 78 KB, `map-ink.js` 31 KB,
`map-continent.js` 12 KB, `continent-shape.js` 261 KB, `continent-engine.js` 20 KB): procedural ink-and-parchment.
- Each region's look is a field function (`map-regions.js`) giving colour, height, water, forest, pine, rock, wet,
  sand, snow, farm and glow at any point, warped by noise; the Stoneveils share one mountain height.
- `map-terrain.js` turns it into a field grid and canvas tiles at 1/2/4/8 px per unit (LRU): hillshade, engraved
  hatching, contours, water, micro relief, field patchwork, canopy, Worley cells, and 41 kinds of placed features
  with level of detail (rocks, tufts, flowers, reeds, pools, dunes, cliffs, bridges, houses, farm fields, hedges,
  ruins, towers, windmills, camps, bones, craters, spires, crystals, graves, banners, lava, snowdrifts, crevasses,
  boats, piers, lighthouses, obelisks, columns, statues, wrecks, smoke, tents, ribcages, carts, pyramids …).
- `map-ink.js` regrades colours, feathers zones into washes, scatters 22 ink symbols, lays a canvas parchment sheet
  with burned edges, and draws the compass, scale and frame.
- `map.js`: the SVG overlay: settlements (`castleArt`, `villageArt`), site icons, exits and waymarkers, routes and
  the travel line, night lanterns per window, rain, the boat trip and its waves, the caravan wagon and route, the
  carriage, the mountain's altitude fog and clouds, the fog over unentered regions, the Quests card.
- `map-continent.js`: the far-out overviews, backdrop, coastline and fog (task 8, placeholder).
- The continent's land is draft 6, made in Python (`Port-Tasks/8_Map_Rework/draft6.py`) and baked into
  `continent-shape.js` by `bake.py`.

**Battle scenery** (`battle-scenery.js`, 32 KB): 33 layered-SVG biomes (sky, far hills, middle, ground, foreground),
seeded per region and zone, with the world clock's time of day and ambient motion (clouds, rain, snow, embers,
fireflies). Notes (2026-10-07): "The battlefield backgrounds are still the bright original scenery. Restyling them was
offered, not requested", so they never got the ink restyle.

**Battle effects** (`battle-effects.js`, 46 KB; CSS in `styles.css` "Battlefield effects"): per element (12 colour
sets) offensive, support and summon magic, Tier X spectacles, projectiles and hit sparks, items and potions, stances,
meditate, skills, masterwork activations; floating numbers, target marker, turn and outcome banners, the Sparkling
glow, boss overlays (Juila's seasons, Zik Lun's orders, Ur-Mat's dream laws, Tankina's storm, the distorted dream and
molten forge).

**Interiors** (`scene-banners.js`, 114 KB): ink rooms drawn from ~70 SpriteKit helpers (masonry, boards, plaster,
timber, floors, pillars, windows, fires, candles, chandeliers, people, cats, dogs, barrels, shelves, counters, forges,
anvils, weapon racks, crystal balls, orreries, magic circles, cauldrons, notice boards, trophies …). Seeded per
settlement (a town always shows the same room), materials by region, day/dusk/night light; a static SVG, a screen-
blended live layer of flames and glows, and for the guild a third layer of hover glows and labels.

**Cutscenes**: `boss-intros.js` (69 KB): per intro a hand-composed SVG scene around the boss's real battle sprite, a
title card and a CSS timeline (7.0–8.4 s). `tier10-cinematics.js`: 12 hand-drawn SVG symbols with CSS motion and a
caption. The 3 special phase cinematics are inline SVG in `index.html`.

**UI chrome** (`styles.css`): the Gothic ink theme block (page, lettering, framed panels, wells, inked inputs, buttons,
inked meter troughs, leather-bound navigation rail, dialogs, leather map/battle header), rarity gem tags and the
animated Exotic wave, element and rarity accent stripes, SVG data-URI corner ornaments and `feTurbulence` parchment
noise; Unicode symbols outside the fonts (⚙ ✦ ☾ ↓ ＋ ⚔ ♟ …).

### 4.2 Authored by hand vs generated

| Authored one by one (fixed markup) | Generated from data, rules and seeds |
|---|---|
| 9 race seals (`objects/race-*.svg`; 6 detailed, 3 placeholder) | 72 playable looks and 174 enemy sprites (rig + spec) |
| App icon | Every forged weapon/armor portrait and sprite (maker × part × material) |
| 19 boss intro scenes, 3 special cinematics, 12 Tier 10 symbols | 85 artifact looks (rarity style × shape × glyph) |
| 8 maker logos, 24 rune glyphs, 5 masterwork artifact drawings | Potion, poison and ingredient icons (rules by tier and name) |
| Unique masterwork weapon/armor designs (`MASTERWORK_LAYOUT`, `SPRITE_UNIQUE`) | 13 interior scenes (seeded per settlement, region, time of day) |
| Map site icons, castle and village art, carriage, CSS ornaments | 33 battle biomes (seeded, time of day) |
| | Map terrain, features, ink finish (field functions + noise); continent land (Python, baked) |
| | Battle effects (element templates) |

### 4.3 What counts as placeholder art

From `engine/Docs/Port-Tasks/README.md` ("Placeholder art the port remakes fully") and `old-game/Docs/Notes.md`
(PLACEHOLDER ART markers; in code `placeholderArt: true` and `SceneBanners`' `PLACEHOLDER_ART`):
- **Task 1**: looks of Plasma Blade, Tidepiercer, First Comet, Peace and Balance, Kugelblitz, Avalanche (item art and
  battle sprites: normal forged looks with hints).
- **Task 2**: the two guild interiors (`guildHall`, `guildTown`), to be remade as detailed, clickable illustrations
  (layout rules G21–G23 kept).
- **Task 3**: Tailor's shop, Institute's inside (today the Tower sanctum), Hearth Home's inside (today the arms
  hall), their map icons, Hearth Home's window, the island and bridge painting, Doctor's Orders' look, the reforge
  anvil drawing, the five traps' looks.
- **Task 4**: every artifact's look (all 85 carry `placeholderArt: true`) and the 4 masterwork artifact drawings.
- **Task 5**: the Sparkling look, Treasure Seeker's drawing, the looks of Moneyshot, Lament, Noble Hero, Hexbane Coat.
- **Task 6**: the 6 Abyssal Rift enemies' sprites and the Rift on Gloamfen's map.
- **Task 7**: the 5 Exotics' looks (Winged Hussar's wings the defining feature), the Limitless button and pop-up
  (the Exotic colour wave itself is real, not placeholder).
- **Task 8**: the continent's whole 2D painting, fog, "Uncharted" parchment, boat and waves, caravan wagon and route,
  carriage drawing and switch, mountain fog and clouds (W1: all remade in the 3D map).
- **Task 9**: the two Apothecary banners, the cauldron and its animation, the icons of 40 potions, 12 poisons and
  56 ingredients.
- **Task 11**: the 3 new race seals and the 24 looks of Half-Beast, Merfolk and Goliath (with the `beast`/
  `beast-round` ear styles).

Placeholder in code: 16 masterwork/Exotic definitions, 85 artifacts, 7 interior scenes, 3 seals, 30 sprite specs
(24 looks + 6 Rift enemies). Under the engine's decisions, though, every old visual is replaced (3D models, Blender
banners and cutscenes), placeholder or not.

### 4.4 The new look (engine docs)

`engine/Docs/Engine-Plan.md` "User decisions (2026-10-16)" and "Decisions for M6/M7/M10"; `engine/Docs/Renderer-3D.md`;
`engine/Docs/Animation.md`; `engine/Docs/Scene.md`.
- **Characters, enemies, items**: real-time 3D models made in Blender (glTF 2.0 `.glb`), with full animation sets.
  **Battles**: the fixed orthographic side view on a 3D stage built in Blender. **Map**: 3D terrain seen straight down,
  orthographic, zoom 1–10× of fit. **Banners and cutscenes**: remade in Blender. **UI**: the engine's own CSS-like UI;
  inline SVG stays for UI (transforms) but "the art becomes Blender renders".
- **Shading**: cel shading in two tones: lit = the palette slot's colour; shadow = that colour mixed 42% toward ink
  `#130e0b` (unless the palette gives a shadow tone); band edge at N·L 0.314.
- **Ink lines**: screen-space, outline 3.5 px and inner lines 2 px at device scale 1 ("the old game's battle sprites
  in a 1080p window"), ink `#130e0b`; silhouettes, open edges, creases > 120°, mesh feature edges; `ink = 0` opts out.
- **Hatching**: in every shadow-tone area, lines 6 px apart, 1 px wide, at 72°, 35% toward the ink, screen-aligned and
  moving with the model; `hatch = 0` opts out.
- **Glow**: emission materials glow with the "lizardman halo" (8 px blur, alpha × 2.2, 2.5 px blur over it).
- **Shadows**: cast in real time; only scenery receives them, drawn as the darker tone with hatching; characters and
  items keep plain cel shading.
- **Colours: palette slots** (`metal.base`, `metal.trim`, `cloth`, `skin`, …) filled from palettes at draw time, "so
  one sword model shows in every metal and one robe in every cloth, as the old game's palettes (metals with
  base/light/dark/trim/grip, cloths, skin per species) do".
- **Terrain**: per region a 16-bit height image and a cover image (palette slots) made in Blender ("first versions
  can be generated from today's field functions"); trees, rocks, ruins as placed models; water shaded with two
  wave trains.
- **Particles**: disc, ring, spark, puff, shard in palette colours, optionally inked, glowing.
- **Rigs** (M7): one clip set per rig family (humanoid, quadruped, griffon, dragon, wisp, core), shared by bone names;
  gear as models on `socket.*` bones, armour skinned; clips at 60 fps, no root motion; the clip list per family is in
  `Animation.md` ("Clip set"); bosses add phase change, transform, intro entrance and specials, asked per boss.
  The user approves the first model of each rig before the rest is made.
- **The bar** (user, 2026-11-05): "the boss and enemy models are to be as detailed and high-quality as the cutscene
  scenes" (Ji'ruan's intro was remade as M10's proof with a stand-in figure).
- **Still open**: "the game's own palette, light setups, line widths and hatch pattern are asked of the user when they
  come up" (Renderer-3D, Method); "how the inline SVG art and its transforms map to the Blender remakes", "the level
  of detail of the item models", UI symbols outside Lato/STIX, the grabbing cursor (Engine-Plan, "At port time"); new
  program icons for the game and the editor "with the port's art pass" (M12-5).
- Target: Vulkan 1.3 GPUs from ~2016 (GTX 1050, RX 470) at 60 fps in 1080p, MSAA 4×.
- What exists in the engine today: test models only (`engine/Tests/Statventure.Tests/Data/Models/`: figure, stocky,
  sword, tabard, stage, jiruan, jiruan-acting …); the shipped packs are `audio.pak` (57 KB) and `ui.pak` (999 KB, the
  fonts). No game model, terrain or banner yet.

### 4.5 Every distinct kind of visual the port must produce

1. **Humanoid characters with equipment**: 9 playable races × 2 sexes × 4 looks (72), used by the hero, companions and
   recruits; Half-Beast beast kinds (wolf, fox, bear, lynx), Merfolk on legs, Goliath stone-grey with runes.
2. **Equipment models**: 23 weapon types in 8 maker styles (part shapes per maker) and 39 metal/wood palettes; staff
   and wand parts (12 stones × 3 tiers, 12 handles, 12 inks); 12 armor types (3 heavy, 3 light, 3 clothing, 3
   shields) with maker helmets, in 48 armor materials; rune glows; 32 masterwork + 5 Exotic unique designs (+3 and +1
   from task 14); natural weapons and the Half-Beast claws.
3. **Humanoid enemies** (~116): goblin, orc, troll, giant, devil, imp, undead (skeleton, armoured undead, wraith,
   vampire), satyr, dryad, lamia, merfolk, harpy, treant, elementals (body materials), humans (bandits, knights,
   cultists), lizardmen, plus the humanoid bosses (Tankina, Ornn, Saffron, Ool, Juila, Gaian, Ur-Mat).
4. **Creatures by family** (~58): quadrupeds (8 wolves, 4 bears, boar, ram, stag), 6 griffons, 22 dragons/wyrms/drakes
   (12 elemental dragons; Nygheir, Anatemis), 13 wisps/spirits, 2 cores; + Mana Golem and Storm Roc (task 12);
   + dungeon-only types, 11 new bosses and the 3 new regions' enemies (tasks 14, 8).
5. **Boss variants**: phases and forms (Juila ×4 seasons, Nygheir scarred, Anatemis ×3, Ur-Mat lucid, Ool mortal,
   Gaian severed, Ornn erupting, Tankina chosen), retinues, the Sparkling variant of every regular enemy.
6. **Item icons/portraits** for inventory, shops and details: forged pieces, runes, 85 artifacts (80 rarity-styled,
   5 masterwork), 50 potions, 12 poisons, 56 ingredients, 8 traps, Camping Supplies, 5 key items (+ Moonfang),
   15 throwing items and smithing materials (task 14), the carriage, maker logos.
7. **The map**: 3D continent terrain for 20 regions (height + cover, coast, two inland waters, lakes, rivers, roads,
   the Stoneveil mountain rising into fog and clouds), placed features (the 41 kinds above, trees by kind), castle
   cities and towns, 3 sites (Church, Institute island and bridge, Hearth Home volcano), exits/waymarkers, the
   Abyssal Rift, the Vampire Citadel, 12 dungeon entrances; the fog of unentered regions; boat and waves, caravan
   wagon and route, carriage; night lanterns, rain; lettering for 89 zones and places; 12 3D dungeon overviews
   (DG69).
8. **Interiors** (Blender banners): Blacksmith (castle/town), General Store (castle/town), Magic Tower sanctum,
   Church storm chapel, Guild hall and town guild (clickable objects with glow and labels), Tailor, Institute,
   Hearth Home, Apothecary (castle/town): 13 variants; day/dusk/night light; regional materials; animated fire.
9. **Battle stages**: 33 biomes (+ town and road variants) with time of day and ambient weather; dungeon stages.
10. **Effects**: spells for 12 elements × offensive/support/summon, Tier X spectacles, projectiles, hits and crits,
    item and potion use, stances, skills, masterwork activations, floating numbers, the target marker, the Sparkling
    shine, boss mechanic overlays, the 12 element statuses and 12 reactions (task 12), the 12 Augmentation magic
    circles, the brewing cauldron, the reforge anvil.
11. **Cutscenes**: 12 boss intros (Ji'ruan's exists as a proof) + 7 phase scenes + 3 special cinematics; 12 Tier 10
    vignettes (reworked to the new Tier 10 spells); 11 new boss intros; the boat trip; turn/outcome and region
    banners.
12. **UI chrome**: the Gothic ink theme in two modes, ~24 screens/modals (sheet, race picker with seals, spellbook,
    inventory, item picker, classes, skills, passives, combat setup/abilities/reactions/loot, map, encounter, region
    transition, shop, Magic Tower, Apothecary, Church store, guild, quests, companions, class spell choice, settings),
    rarity tags (7 rarities + the red class rarity), element accents, ornaments, the symbols outside the fonts, the
    grabbing cursor, 9 race seals (3 to remake), Limitless button and pop-up.
13. **Fonts**: Lato (Regular, Semibold, Bold) and STIX Two Text (Regular, Italic), already in `ui.pak`.
14. **Program icons**: the game's and the editor's (none since M12; the old `icon.png`/`icon.ico` exist).

---

## 5. Audio

**What the old game has.** Music only, no sound effects. All music is synthesized live with Web Audio:
- `scripts/music-themes.js` (127 KB): compact scores in scale-degree notation (melodies, chord numerals, step
  patterns, drum patterns, meters). 38 specs: 17 region themes (one per charted region), 1 battle theme arranged per
  region (17 `combat@<region>` arrangements: key, mode, tempo, instruments, groove and a quote of the region melody),
  20 boss themes (Ji'ruan; Juila; Tankina ×2; Saffron; Anatemis ×3; Ur-Mat ×2; Ool ×2; Gaian ×2; Ornn ×2; Deep Sea
  King; Zik Lun; Nygheir ×2). With the arrangements expanded: **55 themes**. `LOUDNESS` trims (EBU R128: regions
  ≈ −17.5 LUFS, battles ≈ −17).
- `scripts/music-composer.js` (47 KB): `compile`, `perform`, `lint`. Per-note graphs: oscillators (sine, triangle,
  sawtooth, square, custom periodic waves), Karplus-Strong plucked strings, formant choirs, noise buffers, biquads,
  LFOs, panners, a convolution room per theme (impulse from seeded noise), sends, EQ; its own seeded mulberry32.
  34 instruments (strings, violins, cellos, contrabass, spiccato, tremolo, fiddle, pad, drone, horns, trombones,
  trumpet, choirs, organ, flute, whistle, oboe, duduk, clarinet, harp, lute, oud, guitar, harpsichord, pizz, celesta,
  marimba, kalimba, bell, glass, sub …), 21 percussion voices (taiko, war drum, frame, tom, dum, tek, snare, hat,
  shaker, gong, anvil, thunder, timpani …), 7 rooms (chamber, hall, forest, open, cathedral, cavern, mountain), 10
  modes.
- `scripts/music.js` (73 KB): the master chain (gain 0.58 × volume → compressor −18 dB, knee 18, ratio 3), theme
  choice (`desiredTheme`: region theme, `combat@<region>` in battle, boss themes), fades (out 0.55 s, in 0.8 s), and
  since task 8 a 4 s crossfade at region borders (`CROSSFADE`); the empty regions keep the last theme. Its 24 original
  scores are replaced at load by the remastered themes. Volume slider saved in localStorage.
- `music/` (43 MP3s, 63.2 MB, in git): 17 region themes, 17 regional battle arrangements, the battle theme and 8 boss
  themes. "Offline exports only; the game never loads them" (`old-game/Docs/Notes.md`; `engine/Docs/Notes.md`).
- Stale comments: `music-composer.js`'s header and Notes (2026-10-08) still say boss themes use the old engine; the
  current `music-themes.js` holds 20 boss themes and `music.js` replaces every score with them.

**What the engine has ported** (`engine/Docs/Audio.md`, M8, done 2026-11-02, approved by the user's ear):
- Chromium 152's Web Audio processing ported sample for sample (wavetables, biquads, AudioParam timeline, 128-frame
  quanta, convolver with RustFFT's AVX path, compressor, panner, buffer sources; V8's `Math.exp` and glibc's `pow`).
- The composer, its themes and music.js's master chain as a game synthesizer (`Engine/Statventure/Audio/Music/`):
  all 55 themes compile to the old composer's scores and make the same 2.7 million Web Audio calls; all 55 full
  renders pass the opt-in check against Chromium. Data: 34 instruments, 21 percussion voices, 7 rooms, 55 themes in
  `audio.pak` (since M9 step 6). The old score engine was dropped.
- Output through OpenAL Soft (music, effects and master buses with volumes); scheduled cues for cutscenes (M10).
- A sound-effect system: one-shot patches (layers of tones and noise bursts, filter, envelope, seeded variation, voice
  limits) on an effects bus, played by game code or animation clip events.
- **Not ported yet**: the theme choice (`desiredTheme`, "comes with the game port") and the task 8 crossfade between
  region themes (Audio.md "Theme API": `Play` still switches as music.js did before task 8).
- **New audio work for the port**: every sound effect (the old game has none; first asked: a sound when a rune is
  slotted; Ji'ruan's proof cutscene's 3 effect patches are to be asked one by one); themes for 12 dungeons (DG34), 11
  new bosses, 3 new regions.

---

## 6. Saves

**The old game's save.**
- One character autosaved to `localStorage["emberfall-character-stats"]` as one JSON object (`saveCharacter`,
  `scripts/app.js` 1838–1850), written on changes and on window close. Fields: `worldState` (clock, time speed,
  region and position, distances, pending encounter, visited regions, defeated site bosses, guild {reputation,
  completed counts, standard quest, Great Hunt, artifact offers, hooded man's information}, carriage, …), `shopState`
  (`locations`: each visited settlement's and Hearth Home's stock), `combatState` (a deep copy of the whole battle
  when one is active), race, appearance, level, EXP, points, `limitless`, `alchemy` (known recipes, counts), a dozen
  requirement counters (traps used, unarmed attacks, summons used, class/race kills, flawless victories, …),
  HP/mana, allocations, currencies, bonuses, inventory, favourites, equipment, known spells, class levels, active
  skills, `companions` (up to 3 full character records), recruit offers, pending class choices, stats.
- Save/Load Character exports the same object as `stat-fick-character.json`, `format: "emberfall-stat-block"`,
  `version: 22` (`backupPayload`, `app.js` 1857). Companions can be imported from such files (10 MB limit, also in
  `desktop/main.cjs`).
- Other localStorage keys: settings (`stat-fick-game-settings`), music volume (`stat-fick-sound-volume`), and caches of
  the class, spell, race and item files (`stat-fick-<class>-cache`, `emberfall-*-cache`).
- The old game migrates its own old saves in its normalizers: map scale ×3 (`mapScale`), `continentVersion`, retired
  item ids → forged pieces (`LEGACY_ITEMS`), 32 retired artifacts, quest version 2 and retired quest kinds, Ornn's
  hunt, enemy renames (Lesser Dragon → Red Dragon), Limitless locked for old saves.
- Size: no measured save size is recorded in the docs. What grows it: `combatState` (every actor of a battle in
  progress), `shopState` (one stock list per visited settlement, forged item ids up to ~40 characters each),
  companions (each a full character with inventory and classes), the guild's information records.

**The port's saves** (`engine/Docs/Engine-Plan.md`, "User decisions (2026-10-16)" and "Decisions for M9";
`engine/Docs/Assets.md`):
- **Saves start fresh; old saves are not imported** (2026-10-16; repeated in M9's character-export decision, in C12
  "Saved characters are ignored", and in task 14's notes). No migration from the old game is planned.
- Format: binary versioned records (the packs' serializer), a 48-byte header (`SVSV`, format version, payload length,
  SHA-256), written atomically with a `.bak` of the previous save; **named save slots** (`saves/<name>.dat`), not
  compressed, written on every request by a background writer; a damaged save is set aside and the player asked.
  Save Character / Load Character kept as readable JSON of the new format (old format not read).
- Per-user game folder: Linux `~/.local/share/stat-fick/`, Windows `%APPDATA%\Stat Fick\` (`settings.json`,
  `saves/`, `mods/`).
- Planned content of the port's save beyond the old one: the dungeon state (dungeon, stage, stages cleared, bomb
  stuns used, Waylight used, bosses met; the battle and stage resume on load, DG notes), Spell Augmentation ranks,
  the new class requirement counters (tasks 10, 13, 14).

---

## 7. Open questions

Found while surveying; none of these is answered in the docs read.

1. **Item art form**: are inventory/shop icons and item portraits 3D renders of the item models, live 3D views, or 2D
   art? The engine decided items are 3D models; the docs leave "the level of detail of the item models" and "how the
   inline SVG art maps to the Blender remakes" to port time. This decides the size of the biggest asset class (forged
   parts × makers, 85 artifacts, 118 potion/poison/ingredient icons).
2. **Forged items in 3D**: one model per part shape per maker (weapon part kinds: head, handle, body, guard, string;
   armor main + 2 secondary parts) with materials as palette slots? The count of models depends on it; not decided.
3. **The game's palette, light setups, line widths and hatch pattern** for the real game are still to be asked
   (Renderer-3D, Method); the old palettes (`sprite-kit.js` `MATERIALS`, `forge-art.js` `PALETTES`, `battle-scenery.js`
   biomes, `map-regions.js` palettes) are the only source today.
4. **Battle backgrounds**: the old ones were never restyled to ink; the target look for the 33 biome stages in 3D is
   not described anywhere.
5. **Map lettering and the ink finish**: whether the 3D map keeps the parchment, compass, scale, frame and inked labels
   of `map-ink.js` is not stated (the camera keeps labels and markers mapping 1:1).
6. **The 3 special phase cinematics** in `index.html` (storm-talos, false-dreamer, forge-god) overlap with the
   phase intros in `boss-intros.js` (`tankina-phase`, `ur-mat-lucid`, `ornn-phase`); which ones the port remakes is
   not listed (M10-2 says "the phase and special cinematics").
7. **Earth Spirit** (`enemies/Earth Spirit.json`, level 14) appears in no encounter table and no summon effect; unused
   or reached some other way?
8. **The 43 MP3 exports** (63.2 MB in git) are used by neither the game nor the engine; whether they stay is not
   recorded.
9. **Stale docs**: `music-composer.js`'s header and old-game Notes say boss themes still use the old engine; the code
   says otherwise. The old-game Notes also still say "48 playable looks, all 159 enemies" (now 72 and 174).
10. **Save size**: no measurement of an old save exists; the port's slots are uncompressed and will hold battles and
    dungeon state; whether that matters is not discussed.
11. **New content volume left to drafts at port time**: dungeon enemy types and stage pools, 11 bosses (mechanics,
    intros, themes, models), 3 masterworks + 1 Exotic, 15 throwing items, the 3 new regions (enemies, towns, themes),
    the red rarity's look, the 12 statuses' looks and sounds; none has a count of models or scenes yet.
12. **Sound effects list**: the port must add them, but which sounds (per action, per spell, per element, per UI
    event) is asked at port time; no inventory exists.
13. **Re-capturing the game screens** (the engine's 22 reference captures and its stylesheet copy predate tasks 2–11):
    before the port or screen by screen (open since 2026-11-06).
14. **Converter for the old tables**: enemies, spells, artifacts, consumables, key items, races are "converted at port
    time ... checked against the old game in Chromium as the music is"; forged items have no tables (rules in
    `forge-engine.js`), and alchemy, encounters, regions, Great Hunts and shop sizes are JS literals, so they need the
    same Chromium-read path or a port of the rules.

---

### Appendix: other files surveyed

- `old-game/objects/` (17 entries): `fonts/` (5 faces + 2 licences, 2.59 MB), 9 race seal SVGs (0.7–3.6 KB),
  `races.json` (18 KB), `spells.json` (279 KB), `artifacts.json` (40 KB), `consumables.json` (30 KB),
  `key-items.json` (1.7 KB), `stat-fick-icon.svg`, `Stat Fick.desktop` (the user's launcher).
- `old-game/music/` (2 entries): `region-music/` (17 MP3s) and `battle-music/` (9 MP3s + `region-battles/` 17 MP3s).
- `old-game/index.html` (82,644 B): the character sheet (hero, EXP, resources, defense, equipment, stats) with a
  navigation rail, 24 `.modal-layer` dialogs, overlays (`tier10-cinematic`, `boss-intro-cinematic`,
  `boss-special-cinematic`, `battle-banner`, `shop-banner`, `guild-banner`); an inline loader that loads the 48 scripts
  in order.
- `old-game/desktop/` (the Electron 44.5.0 shell, electron-builder 26.15.3): a `statfick://game` protocol serving the
  game files live from the executable's folder with a CSP (`assets.cjs`), a startup check for required files, a
  1440 × 950 sandboxed window, native Save/Load Character dialogs over IPC (`preload.cjs`: `statFickDesktop`), the
  File/View/Help menu (Save, Load, Open game folder, Quit; reload, zoom, fullscreen; About), saving on close.
  Builds an AppImage and a portable `.exe`.
- `old-game/Stat Fick.AppImage` 125,219,529 B; `Stat Fick.exe` 99,841,038 B (not in git; same files in
  `desktop/release/`). `tests/Stat Fick Tests.AppImage` 125,231,080 B (old packaged test client, not in git).
- `old-game/test-client/` (the headless test runner, Electron): suites `smoke` (catalogues load, sandboxed window,
  a Goblin fight, the map and an encounter, persistence), `combat` and `enemies` (`combat-performance.cjs`: AI and
  every enemy), `lifecycle` (`combat-lifecycle-performance.cjs` against `combat-lifecycle-fixture.json`); 49
  `*-browser.js` check files (one per feature, boss or port task: masterwork-elementals, guild-rework, shop-rework,
  artifact-rework, new-areas, new-masterworks, limitless, map-rework, potion-rework, new-races, small-changes, each boss
  …). They check rules, not looks ("the checks run with the stage off"). The engine's reference harnesses separately
  capture 22 game screens (11 states at 1× and 1.65×) and render the music in Chromium.
- `old-game/tests/`: `Regional Map Layout.png` (a schematic of the 17 regions' connections, before task 8) and the old
  test AppImage.

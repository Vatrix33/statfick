# The fourteen port tasks: state and what the port must still do

Survey of 2026-11-12, read-only, from `main` at `29d110d`. Paths are relative to the repo
(`/run/media/system/M.2_2TB/Stat Fick Repo/`). Abbreviations: **PT** = `engine/Docs/Port-Tasks/`, **EP** =
`engine/Docs/Engine-Plan.md`, **ON** = `old-game/Docs/Notes.md`, **TC** = `old-game/test-client/tests/`.

Sources read: PT `README.md` in full; every `N_*.txt`; PT `8_Map_Rework/README.md` and `regions.json`;
`9_Potion_Rework/README.md` and `draft.md` (outline); `10_Class_Rework/` (README, both drafts' structure, task 13/14
parts); `12_Spell_Rework/` (README, `Spell-Augmentation.md`, `Element-Statuses.md`, `Classes.md`, spot checks of the
batches); `13_New_classes/README.md`; `14_Dungeons/README.md` and `Classes.md`; EP "How to continue" (lines 6–200),
"Open questions for later milestones" / "At port time" (735–834), "Decision on the user's tasks for the port"
(1409–1445) and every "Decisions for task N" section (tasks 2–14, lines 1446–2907; task 1 has none, see below); ON's
sections for tasks 1–9 and 11 (lines 281–1110), and its notes on tasks 10 and 12.

Warning on decision ids: three prefixes are reused elsewhere in EP. Task 2's **G1–G34** vs the editor's game questions
**G1–G6** (M11); task 9's **P1–P31** vs the editor's **P1–P38** (M11); task 5's **M1–M26** vs milestone names (M11,
M12, M11-19…). Always cite them with their task.

---

## 1. Table

| # | Task | State | Date | Decisions (EP) | Where its content lives |
|---|---|---|---|---|---|
| 1 | Masterwork Elementals | Built in old game | 2026-11-05 | none numbered; answers in ON "Masterwork Elementals" (user decision 2026-11-05, EP 1421) | old game code/data; ON 281–338; TC `masterwork-elementals-browser.js` |
| 2 | Guild Rework | Built in old game | 2026-11-06 | G1–G34 (EP 1446) | old game; ON "Guild Rework" 339–415; TC `guild-rework-browser.js` |
| 3 | Shop Rework | Built in old game | 2026-11-06 | R1–R29 (EP 1519) | old game; ON "Shop Rework" 416–504; TC `shop-rework-browser.js` |
| 4 | Artifact Rework | Built in old game (PR #10) | 2026-11-06 | A1–A32 (EP 1577) | old game (`objects/artifacts.json`); ON "Artifact Rework" 505–573; TC `artifact-rework-browser.js` |
| 5 | New Masterworks | Built in old game (PR #11) | 2026-11-06 | M1–M26 (EP 1669) | old game; ON "New Masterworks" 648–738; TC `new-masterworks-browser.js` |
| 6 | New Areas | Built in old game (PR #10) | 2026-11-06 | N1–N10 (EP 1644) | old game; ON "New Areas" 574–647; TC `new-areas-browser.js` |
| 7 | New Game Plus (Limitless, Exotic, Exium) | Built in old game (PR #12) | 2026-11-06 | L1–L33 (EP 1937) | old game; ON "New Game Plus: Limitless and the Exotics" 739–841; TC `limitless-browser.js` |
| 8 | Map Rework | Built in old game (PR #14), but its 3 new regions are empty placeholders | 2026-11-06 to 11-07 | W1–W35 (EP 1816) | old game; PT `8_Map_Rework/` (draft 6 picture, `draft6.py`, `draft3.py`, `regions.json`, `bake.py`, README); ON "Map Rework: one continent" 842–940; TC `map-rework-browser.js` |
| 9 | Potion Rework | Built in old game (PR #14) | 2026-11-07 | P1–P31 (EP 1739) | old game; PT `9_Potion_Rework/draft.md` (+ `draft.py`); ON "Potion Rework" 941–1044; TC `potion-rework-browser.js` |
| 10 | Class Rework | Designed for the port (not in old game, C23) | 2026-11-10 to 11-11 | C1–C32 (EP 2729) | PT `10_Class_Rework/Class-Rework-Draft.md`, `Class-Rework-Features.md`, README |
| 11 | New Races | Split (T1): races built in old game; 3 racial classes designed for the port | 2026-11-11 | T1–T17 (EP 2686) | old game (races); ON "New Races" 1045–1095; TC `new-races-browser.js`; classes at end of task 10's drafts |
| 12 | Spell Rework | Designed for the port (SP1) | 2026-11-11 to 11-12 | SP1–SP69 (EP 2466) | PT `12_Spell_Rework/` (29 files); its 2 classes at end of task 10's drafts |
| 13 | New Classes | Designed for the port (NC1) | 2026-11-12 | NC1–NC19 (EP 2369) | end of task 10's drafts ("Classes added by task 13", "Task 13's classes"); record in PT `13_New_classes/` |
| 14 | Dungeons | Built during the port itself (DG1); only its 2 classes designed now | 2026-11-12 (decisions) | DG1–DG78 (EP 2013) | PT `14_Dungeons.txt` + EP DG1–DG78; classes at end of task 10's drafts; record PT `14_Dungeons/Classes.md` |

Plus **the general task** (every enemy revamped, 2026-11-11, commit `a509e91`): recorded only, no decisions of its own
(section 3).

---

## 2. Per task

Common to tasks 1–9 and 11 (PT README "How to use them", "Done in the old game so far"): **"don't build it again"**;
the faithful port carries the rules over with the rest of the game, the old game's browser checks (one entry per rule,
each shown able to fail by mutation) are "the reference the ported rules are tested against", and only the
placeholder art listed under "Placeholder art the port remakes fully" is left. The port does not import the old game's
saves (2026-10-16, `engine/Docs/Assets.md` line 38; restated in EP task 14 "Given before"), so the old-save migration
rules each of these tasks built (G33, R15, A8–A11, L17–L18, W15, …) concern the old game only; DG33 applies R15's way
to "the port's own saves".

### Task 1, Masterwork Elementals (built 2026-11-05)

**Intent** (`1_Masterwork_Elementals.txt`): the six elementals' former unique items, now placeholder forged weapons
that "can become permanent", are remade as masterworks: Plasma Blade (Fire spell wand with longsword properties, a red
laser beam from its gem, Int/Dex, no rune slots, Hellmade), Tidepiercer (trident-shaped lance with a crossguard, 50%
armour ignore, fixed Water rune, Dwarven), First Comet (mace, 25% stun, Earth rune, Hearthforge), Peace and Balance
(quarterstaff, turn counter and Speed can't be reduced, Wind rune, Elven), Kugelblitz (shortbow, 15% splash and a
+50%-Mana-cost mark, Lightning rune, Church of Talos), Avalanche (light armour, 7 counters slow all enemies 30%, Ice
resistance rune, Roundtable); each 5% from elementals of its element.

**In the old game now** (ON 281–338): `ForgeRules.MASTERWORKS` entries `plasma-blade`, `tidepiercer`, `first-comet`,
`peace-and-balance`, `kugelblitz`, `avalanche` (each `placeholderArt: true`); new definition fields `fixedRunes`,
`totalsBonus`, `attacksAs`; two-part masterworks (quarterstaff); new effect kinds `stun-on-hit-chance`,
`spell-cost-mark-on-damage`, `turn-and-speed-lock`, `avalanche-counters`, `armor-pierce-bonus`; drop lists per
elemental (bosses excluded); placeholder hints `beam` and `prongs` in `forge-art.js` `MASTERWORK_LAYOUT`. Checks: TC
`masterwork-elementals-browser.js` (one entry per rule; 22 mutations, all caught).

**Port must:**
- (a) carry over the six masterworks, drops and effects.
- (b) remake their looks, item art and battle sprites (PT README "Placeholder art", Task 1): today "each is its type's
  normal forged masterwork look with simple hints": the Plasma Blade's short red line (design: a red laser beam from
  the gem, like a laser sword) and Tidepiercer's three prongs (design: a trident-shaped lance with a crossguard instead
  of a pommel). First Comet, Peace and Balance, Kugelblitz, Avalanche have no hint yet.
- (c) nothing new.

### Task 2, Guild Rework (built 2026-11-06)

**Intent** (`2_Guild.txt`, a 908-line spec in 4 parts, 24 sections): rework the guild's quests into six types (Hunt,
Cull, Deliver, Search, Escort, Travel; Great Hunts kept) with daily refresh and a reward hierarchy, and replace the guild
menu with an illustrated, clickable interior: bulletin board, counter (completion, artifact selection), tavern
recruitment, the hooded man's information menu, a class information button, hover effects, and two visibly different
interiors (castle town, smaller guild).

**In the old game now** (ON 339–415): rules in `scripts/guild-quest-engine.js` (`GuildQuestRules`: types, reward
factors Hunt 1.8 … Travel 1.0, quest level, Great Hunt ×2.4, Cull circle, Escort ambushes, bandit groups, adjacency);
state in `world-engine.js` (`guild.quest`, `guild.greatHunt`, `guild.boards`); `scripts/guild.js` (boards, accept,
counter, give up, markers, Cull areas); interiors `guildHall`/`guildTown` in `scene-banners.js` (PLACEHOLDER_ART) with
clickable `spot()`s and a docked panel; recruits in `companions.js` `makeOffers`; the map's Quests card (`#map-quests`,
key J); the class menu's `#class-info-button`. Checks: TC `guild-rework-browser.js` (17 run entries; 94 mutations, 93
caught, the 94th can't differ).

**Port must:**
- (a) carry over everything (six types, boards, rewards, interior rules, recruits, Quests card, class info button).
- (b) remake the two guild interiors (`guildHall` castle-city hall, `guildTown` town guild) "fully as the detailed
  illustrations the task asks for" (`2_Guild.txt` sections 10, 17, 18: a large, elaborate castle-town hall and a
  modest, compact town guild that visibly differ), keeping G21 (guild left: board beside the counter and clerk; tavern
  right: hearth, table of adventurers, hooded man alone at a corner table in castle cities only; trophies on the back
  wall), G23 (candle-gold outline glow and name on hover or keyboard focus; every object clickable), G22 (room above a
  panel docked along the bottom). Town guilds have no hooded man (G3).
- (c) nothing from task 2 itself. Later tasks extend the guild in the port: the hooded man's red-class rule (NC7,
  task 13), the Delve quest type, Ravenwatch's unique quest in the Great Hunt slot, Saffron and Ool leaving the Great
  Hunts (task 14).

### Task 3, Shop Rework (built 2026-11-06)

**Intent** (`3_Shop_Rework.txt`): a reforge menu at the Blacksmith (swap parts 2 and 3 up to the main part's rarity,
costly); a Tailor in every town taking over clothing and its reforging; the Institute of Magic on Mirrormere's lake
island (IoM gear 15% off, spells 20% off, staff/wand reforging, rune slotting 20% off, the Doctor's Orders masterwork);
Hearth Home at the volcano behind Ornn (no longer a Great Hunt), Hearthforge gear 10% off, reforging 15% off, the only
place to reforge masterworks; five new traps.

**In the old game now** (ON 416–504): reforge rules in `scripts/forge-engine.js` (`reforgeKind`, `reforgeReason`,
`reforgeOptions`, `reforgeCost` = 3 × `RARITY_PRICE`, `MASTERWORKS["doctors-orders"]`); the anvil `scripts/reforge.js`;
`scripts/shop.js` (`STOCK_VERSION` 25, Tailor key U, Hearth Home store); `scripts/magic-tower.js` (Institute venue, key
I); `scripts/hearth-home.js` (Ornn, `defeatedSiteBosses`, key H); `combat.js` `healingFactor` and the five traps
(Tripwire, Smoke Trap, Frost Glyph, Weighted Net, Gravity Well) in `objects/consumables.json`; the lake island and
bridge in the region data and `map-regions.js`. Checks: TC `shop-rework-browser.js` (9 entries; 121 mutations, all
caught after a second pass).

**Port must:**
- (a) carry over everything.
- (b) remake (R1; PT README): the Tailor's shop scene (`tailorShop`), the Institute's interior (today the Magic Tower's
  sanctum, `institute`), Hearth Home's interior (today the Blacksmith's arms hall, `hearth`); their map icons
  (`SITE_ICONS` in `map.js`: domed tower, forge with flame) and Hearth Home's window (a plain panel like the Church's);
  the island and bridge as painted on Mirrormere's map; Doctor's Orders' look (normal forged robe in white); the
  reforge anvil drawing (`reforge.js`); the five traps' looks (the General Store's generic goods art).
- (c) nothing from task 3 itself, but two later designs change its systems in the port: task 14's smithing materials
  become items that cut a reforge to them by 75%, used up per changed part, with a marker in the reforge menu (DG26,
  DG29–DG32); task 12's Augmentation menu goes into the Institute (25% off, SP12) and the Church of Talos's Storm
  Reliquary (10% / 40% on Lightning and Wind, SP24).

### Task 4, Artifact Rework (built 2026-11-06)

**Intent** (`4_Artifact_Rework.txt`): a unique look for every artifact (similar within a rarity, masterworks completely
unique), new effects for the non-masterwork ones (≥32 uncommon, 24 rare, 16 epic, 8 legendary), and a level ramp for
masterwork artifacts (uncommon strength at low level, legendary at 80, beyond after).

**In the old game now** (ON 505–573): `objects/artifacts.json` (the 32 old artifacts replaced by 80 new; 4 masterwork
artifacts on the ramp: Second Wind, Wraithguard, Arrowtip Rain, Titanfall); `app.js` `ARTIFACT_EFFECT_KINDS`,
`masterworkArtifactAt` (Levels 1–200, a straight line through 1 and 80); `combat.js` "The Artifact Rework" block; 1%
artifact drop from every non-`unique` enemy; old saves cleaned (`RETIRED_ARTIFACT_IDS`); looks in `forge-art.js`
`artifactArt` (`ARTIFACT_STYLE` per rarity, `ARTIFACT_SHAPES` 49 objects, `MASTERWORK_ARTIFACTS` 4 drawings). Checks:
TC `artifact-rework-browser.js` (12 entries; 85 mutations, all caught after a second pass).

**Port must:**
- (a) carry over the set, effects, ramp, drops.
- (b) remake (A1, A2): every artifact's look (today: per-rarity shared material: worn bronze and plain glass uncommon;
  polished silver, cut blue-white gem rare; gold filigree, faceted violet gem and two side stones epic; white gold,
  glowing core and halo legendary) "as proper item art: artifacts of a rarity similar with a few details off,
  masterworks each completely their own"; and the four masterwork artifacts' own drawings (Second Wind a glass lung
  with wind; Wraithguard a pale gauntlet trailing mist; Arrowtip Rain a quiver with falling arrowheads; Titanfall a
  cracked stone fist). Task 5's Treasure Seeker is a fifth masterwork artifact (task 5 below).
- (c) nothing.

### Task 5, New Masterworks (built 2026-11-06)

**Intent** (`5_New_Masterworks.txt`): Sparkling enemies (0.1% chance modified by Luck, 2% hidden in a Cull, highlighted,
+40% stats and +100% HP, ×11 EXP, 70% + 5% masterwork drops) with their two exclusive masterworks Treasure Seeker
(artifact) and Moneyshot (Outlaw longbow); Lament (Elven wand, 2% from sirens) and Noble Hero (Roundtable crossbow, 1%
from demons and devils); a new masterwork armour at the session's discretion; the Shadow Crystal at 0.05% from every
enemy.

**In the old game now** (ON 648–738): `combat.js` "Sparkling enemies" block (`SPARKLING`); Treasure Seeker in
`objects/artifacts.json` on task 4's ramp; Moneyshot (`fifth-attack-boost`), Lament (`extra-target-spell`), Noble Hero
(`second-strike`) and the Hexbane Coat (Boreal Bolt coat, −10% spell damage taken and 25% reflected; 1% from non-boss
undead, demons, devils; M26) in `forge-engine.js` `MASTERWORKS`; Shadow Crystal drop (M24); Sparkling look in
`styles.css`. Also made masterwork armour wear its recorded colour (Doctor's Orders white). Checks: TC
`new-masterworks-browser.js` (10 entries; 103 mutations, all caught after a second pass).

**Port must:**
- (a) carry over everything.
- (b) remake (M1; PT README): the Sparkling look (`styles.css`: shimmering gold "Sparkling" on the encounter screen, gold
  glow and twinkling motes over the sprite, gold name in battle, gold-edged card with ✦ in the classic view) as
  "Sparkling enemies that visibly shine"; Treasure Seeker's drawing (`MASTERWORK_ARTIFACTS["treasure-seeker"]`, a
  gilded compass over coins); the looks of Moneyshot, Lament, Noble Hero and the Hexbane Coat (today their types'
  normal forged looks; the coat silver-grey), "each masterwork its own".
- (c) nothing. Task 14 keeps Sparkling in regular dungeon stages, never boss stages (DG40); task 13's Treasure Hunter
  counts Sparkling enemies defeated.

### Task 6, New Areas (built 2026-11-06)

**Intent** (`6_New_Areas.txt`): one demon/abyss area in the marsh with corrupted enemies like Alder Vale's: at least 3
demon/impid and 3 corrupted enemy types.

**In the old game now** (ON 574–647): the Abyssal Rift, a Level 45–55 zone in Gloamfen Marsh's south-west
(`world-region-data.js` `abyssalRift`; `encounter-engine.js`: 50% encounter chance, nine groups); six enemy files in
`enemies/` (Corrupted Bog Troll 47, Corrupted Mire Stalker 50, Corrupted Mossback Lamia 53, all `demon` race; Abyssal
Imp 46 impid, Rift Fiend 50, Abyss Witch 54); the marsh's range raised to 35–55 (N10); placeholder sprites in
`sprite-catalog.js` and the Rift painted in `map-regions.js`. Checks: TC `new-areas-browser.js` (7 entries; 64
mutations, all caught after a second pass).

**Port must:**
- (a) carry over the zone, enemies, groups (and place the Rift on the continent, already done by task 8 in the old
  game).
- (b) remake (N4): the six enemies' battle sprites (today marsh bases darkened with red eyes, troll with violet veins;
  imp ember-red; Rift Fiend and Abyss Witch the devil kit in charcoal and violet robe) "the corrupted creatures
  recognisably their marsh kin, corrupted as Alder Vale's are, and the demons and the rift their own"; and the Rift on
  Gloamfen's map (dark violet scar with a red-glowing heart, and its label). The general "At port time" bar applies:
  boss and enemy models as detailed as the cutscene scenes (EP 757).
- (c) nothing. The six enemies fall under the general enemy revamp (section 3).

### Task 7, New Game Plus: Limitless, Exotic, Exium (built 2026-11-06)

**Intent** (`7_New_Game_Plus.txt`): Limitless, unlocked by Anatemis's first defeat (pop-up, sheet button, toggle): level
cap 200 without new class perks, all enemies scaled to the player and +25% stats, bosses with Level-100 class perks,
masterwork drops doubled, fully reversible; the Exotic rarity (green/purple wave) and its material Exium; five Exotics
(Fletcher, Tough-Out, Winged Hussar, Cherubim, Connected Balancer); an Exotic reward for beating Anatemis at Level 200.

**In the old game now** (ON 739–841): `scripts/limitless-engine.js` (`LimitlessRules`, `scaleEnemy`, the eleven
bosses' `BOSS_CLASSES` at exactly 100 class levels, L29); `scripts/limitless.js` (button, pop-up); `app.js` levels and
EXP; `combat.js` enemy scaling and drops; `forge-engine.js` (`MAX_LEVEL` 200, gear past 100, `RARITIES` ending in
"exotic", Exium, `EXOTIC_IDS`); the Exotic colour in `styles.css`. Checks: TC `limitless-browser.js` (14 entries; 100
mutations, all caught after a second pass).

**Port must:**
- (a) carry over everything; later enemies (task 14's dungeons and the like) scale through the same `scaleEnemy` when
  they spawn (PT README task 7; "The order": Limitless is future-proof, task 14 gives its dungeon enemies the Limitless
  scaling).
- (b) remake (L1): the five Exotics' looks (today their types' normal forged looks, Exium drawn as Mythilium, but for
  Winged Hussar's simple pair of wings, `wingedGuard` in `forge-art.js`), "each Exotic its own, Winged Hussar's wings
  its defining feature"; the Limitless button and pop-up (plain sheet styles). **Not placeholder:** the Exotic colour
  (dark neon green and purple moving wave) and the sheet's look while Limitless is on.
- (c) nothing new from task 7. Its `BOSS_CLASSES` name the old classes, so they fall under C2's rework (EP 2735: "the
  bosses' `BOSS_CLASSES` of task 7, L29"); task 14's new bosses get Level-100 class sets "drafted with them" (EP
  "Following from the answers"); task 13's Limitbreaker's passive is +25% to all stats while Limitless is on (NC18);
  task 14 adds a sixth Exotic (DG36, DG38).

### Task 8, Map Rework (built 2026-11-06 to 11-07; three regions left to the port)

**Intent** (`8_Map_Rework.txt`): one continuous continent about 40% larger instead of separate regional maps, no
visible borders, music crossing over at regions, regions repositioned for believable geography with progression
safeguards, roads' connections kept; new areas allowed only if "fully implemented rather than serving as empty filler"
(section 3: identity, terrain, enemies, level band, density, connections; new enemy types if needed); a boat-travel
animation; fog and clouds as the mountain rises; caravan fast travel between visited castle towns; a horse carriage to
buy.

**In the old game now** (ON 842–940): the land `scripts/continent-shape.js` (generated by PT `8_Map_Rework/bake.py`,
don't edit), places in `continent-engine.js` (`ORIGINS`), moved roads in `world-region-data.js`, routes in
`world-engine.js` (W30: off-road only through entered regions), fog of unentered regions (`visitedRegions`), the 2D map
in `map.js`, `map-terrain.js`, `map-continent.js`, `music.js` crossfade (4 s), the boat (756 min), mountain altitude
effects, caravan (`#map-caravan`, key K), carriage (50,000 silver). The three new regions exist only as ground:
`uncharted-highlands`, `uncharted-steppe`, `uncharted-delta` (`uncharted: true`, no settlements, roads, exits, sites,
encounters; header "Uncharted land"; parchment marked "Uncharted"; keep the last region's music; W28, W29). Checks: TC
`map-rework-browser.js` (9 entries; 44 mutations, all caught after a second pass).

**Port must:**
- (a) carry over the rules ("the land, the routes, the timings and amounts" are not placeholder); and the engine's
  soundtrack, which switches themes as `music.js` did before task 8, gains the region crossfade (`engine/Docs/Audio.md`,
  "Theme API"; PT README task 8; EP 131–133).
- (b) remake in the port's **3D map** (W1) with draft 6 (`8_Map_Rework/continent-draft-6.png`) as the reference for the
  continent's shape (W26, W27): the continent's painting (regions' pictures side by side, `map-terrain.js`,
  `ContinentRules.cover`; far-out overviews, backdrop, coast line in `map-continent.js`), the fog over unentered regions,
  the empty regions' "Uncharted" parchment (W29), the boat and its drifting waves (W31), the caravan's wagon and route
  line, the horse carriage's drawing in the General Store (`CARRIAGE_ART` in `shop.js`) and its map switch, the
  mountain's fog and cloud banks (W32).
- (c) **make the three new regions** (W27: "everything for these regions will be done during the port"; their "names,
  identity, terrain, enemies, level bands, towns and connections are made in the port"). Places (8_Map_Rework README,
  origins in continent units):
  - rocky highlands, `uncharted-highlands`, origin (2484, −56): east of the Alder Vale, south of the Sunward Coast; the
    continent's one rocky region (W20); draft label band 15–30 (proposal only).
  - steppe, `uncharted-steppe`, (672, 1644): south of the Alder Vale, east of Stormwake; draft 20–35 (proposal only).
  - west-coast delta, `uncharted-delta`, (−4296, 5422): below the Verdigris Jungle, above Duskpine, west of Saffron;
    draft 50–65 (proposal only).
  **Undecided:** all of names, identity, terrain, enemies, level bands, towns, connections ("The labels and level bands
  in the picture are only the draft's proposals", 8 README). **Already fixed by decisions:** task 14's harpy dungeon is
  in the rocky highlands at 50–56 "whatever band the region is given" (DG65); Forage is off in the uncharted regions
  "empty until the port" (P11), so they need forage tables; the carriage may cross "the uncharted land" (W33). In the
  old game they have empty encounter rule sets and no guild, Forage or quest content (ON 842–940, W28).
- Order: the README's "The order": "the new locations of tasks 3 and 6 before the map rework of task 8" (done).

### Task 9, Potion Rework (built 2026-11-07)

**Intent** (`9_Potion_Rework.txt`): an Apothecary in every town (exclusive potion seller, always stocking Health and
Mana), 40 new potions over five rarities and 12 throwable poisons, ≥50 ingredients from foraging and enemy drops,
Forage on the map, brewing and recipe discovery, the Alchemist and Assimilator classes, and the guild's Collect quest.

**In the old game now** (ON 941–1044): potions and poisons in `objects/consumables.json`; 56 ingredients, 62 recipes,
forage tables and enemy drops in `scripts/alchemy-data.js` (both generated once from `draft.md`); rules
`scripts/alchemy-engine.js`; `scripts/apothecary.js` (key O); Forage in `map.js` (key D); `classes/Alchemist.json` (5
levels) and `classes/Assimilator.json` (7 levels); Collect quests (`guild-quest-engine.js`, `guild.js`); banners
`apothecaryHall`/`apothecaryShop` in `scene-banners.js`. Approved content: PT `9_Potion_Rework/draft.md` (P31); "If the
game's data and this draft ever differ, the game's data is the newer" (9 README). Checks: TC `potion-rework-browser.js`
(12 entries; 97 mutations, all caught after a second pass).

**Port must:**
- (a) carry over everything.
- (b) remake (P2): the two Apothecary banners (castle-city `apothecaryHall`, town `apothecaryShop`) "sharing an
  apothecary look, grand and modest"; the brewing cauldron and its animation (`apothecary.js`, `styles.css`); the icons
  of the 40 new potions, the 12 poisons and the 56 ingredients (`potionArt`, `ingredientArt` in `forge-art.js`), "each
  potion, poison and ingredient its own".
- (c) nothing from task 9. Users in later designs: task 10's Poisoner remade around task 9's poisons (C16); the
  Alchemist and Assimilator are among task 10's 130 current classes ("unchanged; it was made with task 9");
  task 14's Throwing Specialist counts the 12 thrown poisons (DG4) and its materials are also foraged (DG32); Collect
  counts dungeon finds (DG71). README "The order": "the reforging of task 3 before task 9", "the guild quests of task 2
  before Collect and Delve", "poisons before … the Poisoner of task 10".

### Task 10, Class Rework (designed 2026-11-10 to 11-11, for the port)

**Intent** (`10_Class_Rework.txt`): review every class (identity, requirements, passive and skill), remake what is dated,
under- or over-powered or useless while keeping its identity; a capstone (passive or skill at the last level); colour
rarity (none, blue, purple, gold) sets power and can be changed on the user's word; then add 26 named classes and 15
of the session's own, asking each one's identity, levels, requirements and rarity in that order.

**State:** not built in the old game (C23, which replaced C1); the old game's classes are unchanged. Drafts in PT
`10_Class_Rework/` (C31): `Class-Rework-Draft.md` (1831 lines; identity, levels, requirements, rarity) and
`Class-Rework-Features.md` (4388 lines; level-by-level features, old beside remade, approved as a sample of four and
eleven batches). Task 10's own part: **170 classes** = 130 current (each reviewed, C3) + 40 new (the task's names minus
Poisoner, which exists and is reworked (C9): 25; seven names from `old-game/classes/newc/`: Chronicler, Shaman, Elder
Shaman, Trickster, Awakened, Warden, Scout (C4, C7); eight of the session's own: Field Medic, Sentinel, Beast Tamer,
Sunderer, Camp Cook, Leywalker, Nightwalker, Mentor (C6)). Rarity of the 170: 109 none, 42 blue, 13 purple, 5 gold, 1
red (Unreality, a new rarity above gold, C16). Changes to current classes: Scavenger 7 → 5 levels; 8 identities
sharpened; Element Siphon renamed Elemental Siphon (id `element-siphon` kept); Poisoner remade; 15 rarities raised.
Rules: one passive and one skill per class (C21; exceptions the twelve elemental mages, item classes, Vampire Lord C24,
Ki Warrior C26, Psykic C27; enemy-only Raid Boss, Beast, Great Beast, Monster outside the rules C25), capstone at last
level, requirements within a turn's 5 AP (C28), Hierophant's shape for all elemental mages.
The drafts now also hold tasks 11–14's 86 classes (256 in all, 252 for players).

**Port must:**
- (a) nothing to carry over: the old game's 130 class files stay as they are until the port replaces them.
- (b) no placeholder art. The red rarity's look is still to ask (C16, "Still to ask", EP 2886–2888).
- (c) **build all 256 classes from the drafts** with the user's three duties (C32): "(1) the classes are unchanged in the
  old game; during the port, the classes in the drafts are added; (2) during porting, every class is inspected for
  possible errors and inconsistencies (in the drafts and against the game's rules, before or while it is built); (3)
  after the port, every class feature is tested so that it works as intended (every level of all 256 classes, passives
  and skills included)" (EP 816–821). Also: every mechanic the features mark "(new)" (282 occurrences of "(new)" in
  `Class-Rework-Features.md`: about 133 in task 10's part, 4 in task 12's, 136 in task 13's, 9 in task 14's; raw text
  count); the new requirement counts task 10's new classes use (e.g. days in a row without entering a town (Hermit),
  caravan trips (Caravanist), regions entered (Explorer), items reforged (Smith, Mastersmith), runes fitted (Rune
  Knight), information bought from the hooded man (Informant), Search quests completed (Seeker), the Cycle's
  one-battle combination; the draft says "a few need a new count the game would keep"); the full rework of every
  enemy that uses classes (C2, section 3). "The numbers are proposals to tune when built" (10 README).
- **Order constraints:** C2/C11: "every enemy that uses the old classes is fully reworked during the port … once the
  port has built task 10's reworked classes"; no class is removed (C11), so the old game's enemies keep naming classes
  that exist; saved characters ignored (C12). README "The order": caravans (task 8) and poisons (task 9) before the
  Caravanist and the Poisoner. Within task 10's own part of the features file the only other task named is task 9
  (Poisoner, Alchemist, Assimilator, Camp Cook); tasks 12 and 14 are named only in their own appended sections.

### Task 11, New Races (races built 2026-11-11; classes for the port)

**Intent** (`11_New_races.txt`): three new playable races with icons, Half-Beast, Merfolk (made playable), Goliath,
each with a racial class like the existing ones.

**In the old game now** (ON 1045–1095): `objects/races.json` (`half-beast`: balanced feral stats, Natural Claws;
`goliath`: mighty and slow, Giant's Blood; `merfolk` made `playable`, its stats and Fishman Physiology kept); new race
effect types in `app.js` (`max-hp-percent`, `unarmed-power-per-levels`, `unarmed-name`); claws as unarmed attacks in
`combat.js` (T13); placeholder looks in `sprite-catalog.js` `RACES` (four male, four female each) with beast ear styles
`beast` and `beast-round` in `sprite-humanoid.js`; placeholder seals `objects/race-half-beast.svg`, `race-goliath.svg`,
`race-merfolk.svg`; recruit names in `companions.js`. Checks: TC `new-races-browser.js` (6 checks; 22 mutations, all
caught).

**Port must:**
- (a) carry over the races.
- (b) remake (T5): the three seals ("seals that sit with the six older ones") and the 24 looks: Half-Beasts that
  clearly show their beast (wolf, fox, bear, lynx; T14), Merfolk recognisably sea-folk on land (legs, fin ears, T3),
  Goliaths visibly giant-blooded ("the stone-grey skin and rune-like markings of T4, which the placeholders don't
  draw").
- (c) the three racial classes, **Wildheart** (Half-Beast), **Deepcaller** (Merfolk), **Peakwarden** (Goliath): Level
  30 and the race, 3 levels, no colour, passive at 1, skill at 3 (T17); in task 10's drafts, built with the other
  classes (T1).
- Open (ON 1086–1089): "at low stats a human's Rulers of the World … outweighs Giant's Blood's +10% … A balance point
  for the port's race review, not changed here."

### Task 12, Spell Rework (designed 2026-11-11 to 11-12, for the port)

**Intent** (`12_Spell_Rework.txt`): bring every spell up to date (power, scaling, effects, Mana cost; tier kept) so
spells are as fleshed out as weapons and armour; add two spells per element in Tiers 1–5 and one in Tiers 6–9, with at
least four new summons (one each for Nature and Death, two for the elements with the fewest); add the Spellweaver
(augments spells, changes elements, scalings, tiers) and the Ritual Caster (delays casts to cut Mana).

**State:** not built in the old game (SP1: "the old game stays unchanged and the port builds them, so the old game's
enemies and classes stay balanced until they are revamped together at the port"). The spec is PT `12_Spell_Rework/`
(29 files, all approved; its README lists them):
- `Spell-Augmentation.md` (SP27): the system. Per hero, per spell: attributes in ranks (up to 5 each, effect duration
  3, at most 10 per spell, SP10), one common attribute list (SP14), summons' own attributes Vitality, Might, Speed
  (SP26), costs (k-th rank = k × 10% of the spell's Magic Tower price), reset at half refund (SP13); the Spellweaver's
  weavings (element 100%, scaling stat 50%, tier 200% of the spell price; outside the 10 ranks, SP63); the power curve
  (SP17–SP22: Mana as scarce as now, all-enemies spells at 75%, higher tiers raised, spells never crit SP20).
  Where: every Magic Tower (full), the Institute of Magic (25% off), the Church of Talos's Storm Reliquary (10%, 40% on
  Lightning and Wind), anywhere from the spellbook for a Spellweaver (20% off, adds up to 50%).
- `Spell-Sample.md` (SP31) and `Element-Statuses.md` (SP32, SP60, SP61): 12 element statuses (Exposed, Burning, Soaked,
  Cracked, Off-Balance, Shocked, Chilled/Frozen, Blessed, Hexed, Rooted, Withered, Bleeding), 12 reactions (Resonance,
  Melt, Steam, Topple, Wildfire, Electrocute, Flash Freeze, Purify, Devour, Overgrowth, Exsanguinate, Blood Harvest),
  each spell's own chance and duration, statuses work both ways (enemies on heroes), 28 spells borrowing another
  element's status.
- `Batch-01-Basic.md` … `Batch-12-Blood.md` (SP35–SP47): all **381** learnable spells reworked (33, 31, 32, 31, 32, 31,
  31, 32, 32, 32, 32, 32 by element). The 7 boss/enemy-only spells stay exactly as they are (SP4: War Horn, Call the
  Storm, Thunderbolt of Talos, Stonecrust, Wheel of the Year, Vulcanic Charge, Summon Imp Retinue).
- `New-01-Basic.md` … `New-12-Blood.md` (SP49–SP59, SP62): **168** new spells, 14 per element (12 elements, Basic
  included, SP3), four new summons (Basic's Mana Golem, Wind's Storm Roc, Nature's Dryad, Death's Wraith; SP34, SP48);
  every element keeps ≥1 summon and ≥1 Dragonbreath spell (SP33).
- `Classes.md` (SP69): Spellweaver (7 levels, purple; Level 50, Mage 5, ≥30 augmentation ranks bought) and Ritual
  Caster (5 levels, blue; Level 35, Mage 5, 15 spells known); delays 1–3 turns at −30% Mana each, one ritual at a time,
  broken only by a stun (SP64–SP68). Copied to the end of task 10's drafts.

**Port must:**
- (a) nothing to carry over (the old game's `objects/spells.json`, 388 spells, stays until replaced).
- (b) no placeholder art, but art and sound made new: the Augmentation menu's look (SP25: an empty circle with one slot,
  two lists (player's, companions'), a lit-up magic circle per element with an animation and sound, a symbol per
  offensive/summon/defensive spell, a spot per attribute, reinforced spots); each status's look and sound in battle
  ("How each looks and sounds in battle is made in the port", `Element-Statuses.md`); the two new creatures' looks;
  **the Tier 10 spell animations reworked** (SP47; the old game's full-screen cinematics,
  `old-game/scripts/tier10-cinematics.js`; Tier 10 has one spell per element).
- (c) build: Spell Augmentation (menu, per-hero ranks and weavings in the character file and save, enemies' in their
  definitions; `Spell-Augmentation.md` §10); statuses, reactions, borrowed statuses; the 381 reworked and 168 new spells
  on the curve; the Mana Golem (Level 18, ~200 HP) and Storm Roc (Level 50, ~600 HP) creatures with looks and stats;
  every "(new)" mechanic (140 occurrences of "(new)" across the folder's files, raw count); bosses can't be defeated
  outright by instant death (SP46); the two classes with task 10 (incl. the augmentation-ranks count for the
  Spellweaver's requirement); enemies' spells, augmentations included, in the general enemy revamp (SP16).
- **Order constraints:** SP1 (old enemies and classes stay balanced until revamped together at the port); SP16 (enemy
  augmentations chosen per enemy in the revamp); task 13's classes build on task 12's statuses and reactions (section
  4). The Augmentation menu sits in task 3's Institute of Magic and the Church of Talos.

### Task 13, New Classes (designed 2026-11-12, for the port)

**Intent** (`13_New_classes.txt`): 4 melee, 4 ranged, 4 spell, 4 rogue-path classes; 2 for each of six pairs
(melee-mage, ranged-mage, rogue-mage, melee-rogue, melee-ranged, rogue-ranged); 9 without a focus; then more until
players can acquire 250 classes; each asked identity, levels, requirements, rarity in order.

**State:** not built in the old game (NC1: task 10's rules apply). **79 classes** (NC2: 171 of task 10's 175 were
acquirable, Raid Boss, Beast, Great Beast, Monster not): the task's 37 by focus and 42 more (8 next steps for new
classes, 7 melee, 3 fighters with an element, 5 ranged, 3 rogue, 7 spells, 9 without a focus). Rarity: 51 none, 22
blue, 4 purple, 1 gold (Paragon), 1 red (Limitbreaker, NC6). Each focus class builds on its path from any class on it
(NC3); each built in its own shape (NC11). Approved and copied to the end of task 10's two drafts (NC19); PT
`13_New_classes/New-Classes-Draft.md` and `New-Classes-Features.md` stay as the record. Names by batch (NC10–NC17):
sample Bladebound, Harrier, Catalyst, Mimic; batch 1 Twinblade, Avenger, Crusher, Arbalest, Fletcher, Deadeye,
Enchanter, Riftmage, Glyphmage, Cutthroat, Opportunist, Con Artist; batch 2 Warcaster, Cinderblade, Wandslinger, Prism
Archer, Spellthief, Illusionist, Corsair, Thug, Ranger, Skirmisher, Sniper, Poacher; batch 3 Prodigy, Steadfast,
Equilibrist, Fateweaver, Conqueror, Martyr, Financier, Collector, Chameleon; batch 4 Bladestorm, Transmuter,
Phantasmist, Voidwalker, Doppelganger, Kingpin, Artificer, Warlord, Frostwarden, Stormblade, Thornguard; batch 5 Lancer,
Scourge, Shieldmaster, Grappler, Drunken Master, Skald, Knight-Errant, Trickshot, Huntmaster, Bowmaster, Giantslayer,
Lightbearer; batch 6 Saboteur, Accomplice, Vigilante, Soulbinder, Beguiler, Astromancer, Golemancer, Hedge Witch,
Dirgesinger, Abjurer; batch 7 Guildmaster, Limitbreaker, Treasure Hunter, Daredevil, Surgeon, Ascetic, Lycanthrope,
Paragon, Schemer. (Task 13's class Fletcher shares its name with task 7's Exotic Fletcher.)

**Port must:**
- (a) nothing to carry over. (b) red rarity look shared with Unreality (C16, NC6).
- (c) build the 79 with task 10's three duties (C32; EP 822–824); the hooded man's rule (NC7): "the hooded man never
  offers information on a red class while the player is Level 99 or lower; from Level 100 the red classes (Unreality,
  Limitbreaker) join his class offers, the Limitbreaker despite needing Level 110" (his rule leaving out classes above
  Level 100, `playerClasses` in `guild-information-engine.js`, no longer applies to it); the new counts, requirement
  kinds and key item listed in `Class-Rework-Draft.md` "Task 13's new counts and requirement kinds" (22 new counts, e.g.
  weapon-in-each-hand attacks, element reactions set off, boat trips, Sparkling enemies defeated, Great Hunts completed,
  guild Escort quests; new kinds: stats within a quarter of the lowest, no artifacts equipped, the outlaws group; the
  **Moonfang**, a key item consumed on acquisition like the Blood Crystal, a 1% drop from a wolf defeated at night,
  NC17); the "(new)" mechanics (≈136 marks in task 13's part of the features file).
- **Order constraints / dependencies:** Catalyst ("Set off 50 element reactions", new count "from task 12's
  reactions"), Steadfast ("from task 12's statuses"), Enchanter ("Enchanted weapon hits set off reactions … (task
  12)"), and other features using task 12's statuses; Limitbreaker on Limitless (task 7); Treasure Hunter on Sparkling
  (task 5); Corsair on boats (task 8); Huntmaster on 5 completed Great Hunts (with Saffron, Ool and Ornn gone, eight
  remain; EP task 14 facts); Knight-Errant on Escort quests (task 2); Drunken Master on potions (task 9); Paragon on
  task 10's Jack of all Trades 4, Knight 5, Sharpshooter 5, Spellcaster 5, Assassin 5 (NC8). Task 13's classes were
  kept clear of task 14's Dungeon Delver and Throwing Specialist (NC2).

### Task 14, Dungeons (built during the port itself; decisions 2026-11-12)

**Intent** (`14_Dungeons.txt`, 966 lines, 18 sections): dungeons as fixed map locations with a dedicated overview,
multi-stage battles ending with a boss; Camping Supplies (20% HP and Mana) between stages; lock-in, defeat penalties
(without a Delve: −75% money, −50% eligible items, respawn at the dungeon at 1% HP 0% Mana; with a Delve: −35% money,
−10% items, nearest town) and a mandatory warning before every entry; the Delve guild quest; nine named dungeons and
three more of the session's design; bosses at Great Hunt difficulty; Saffron and Ool moved from Great Hunts to dungeon
bosses; three masterworks (Great Mountain Mole, Void Lord Firadialin, Goblin King) and one Exotic; the Dungeon Delver
(exactly 5 levels) and the Throwing Specialist; seven named General Store consumables (Emergency Rope, Waylight,
Sparkle Bombs, Flash Bomb, Marked Throwing Knife, Throwing Knife, Dust Bomb) and four more throwing consumables.

**State:** DG1: "nothing in the old game, nothing built or drafted now; every design question is asked now and its
answer recorded here". All design questions answered (DG1–DG74); the content (dungeon enemies and stages, bosses,
items, numbers) is "drafted at port time and approved by the user then" (DG2). The two classes were then designed and
approved (DG75–DG78) and sit at the end of task 10's drafts: Dungeon Delver (5 levels, blue; Level 15, finish 2
different dungeons) and Throwing Specialist (6 levels, no colour; Level 12, throw 30 throwing items in battle, the
thrown poisons counting). EP 2026–2066 lists what the old game has and lacks for it (no staged battles; a defeat costs
nothing today; Camping Supplies exist (map only, full restore); stun rules; General Store random stock; ten Great
Hunts; none of the eight named new bosses exist; goblins only up to Level 17, etc.).

**The twelve dungeons** (task text; DG5, DG51–DG57, DG61, DG65):

| Dungeon | Levels | Stages | Enemies | Boss | Place (DG5 etc.) |
|---|---|---|---|---|---|
| Goblin Cave | 4–8 | 4 | Goblins | Hobgoblin Leader | Alder Vale, Forest (5–11) |
| River Portal | 10–14 | 5 | Water Elementals | Deep Sea Apparition | Stoneveil Foothills, River (12–17) |
| Abandoned City | 20–24 | 6 | Demons and Impids | Imp Lord Ru'manon | Mirrormere Basin, Lake Shore (20–26) |
| Ruined Castle | 25–30 | 8 | Goblins | Goblin Paladin | Stormwake Hills, Thunder Hills (23–29) |
| Mountain Portal | 38–40 | 5 | Earth Elementals | Great Mountain Mole | Cinder Isle, Lava Fields (36–39) |
| Ancient Prison Ruin | 42–46 | 9 | Goblins | Goblin King + 2 Goblin Champions | Crimson Divide, Sunscorched Escarpment (44–47) |
| Great Void Rift | 62–68 | 6 | Demons | Void Lord Firadialin | Bloodstone Coast, Giant Rock Spires (62–67) |
| Ancient Pyramid | 65–70 | 10 | Devils | Saffron (moved, unchanged, DG33) | Deep Desert, Pyramid Sanctum |
| Shattered Castle | 74–78 | 8 | Undead | Ool (moved, unchanged, DG33) | Ancient Battlefield, Silent Westfield (72–75) |
| harpy dungeon (name at port time) | ~50–56 | port draft | Harpies (DG53) | new, port draft | rocky highlands, task 8's new region (DG65) |
| Nocturne Keep's dungeon | ~66–70 | port draft | Vampires (DG52) | a new vampire lord (DG58) | Nocturne Keep itself (Bloodstone Coast's castle city) |
| dragon lair (name at port time) | ~85–92 | port draft | Dragons (DG52) | new, port draft | Stoneveil Summit, Cloudbreak Ridge (88–92), below the Dragon Eyries (DG61) |

**Decided rules, by area** (EP 2073–2350): entry needs nothing; warning shows only the applying case, large text, with
recommended levels and the Waylight choice (DG6, DG70); stage battles from designed pools of groups, Empowered elites
in later stages (DG7); enemy levels rise by stage to the range top (DG8); dungeon-only enemy types plus existing ones
(DG9); between stages everything but travel, towns, shops (DG10); Camping Supplies at most one after each cleared stage,
20% to all incl. fallen, keep chances apply (DG11); time passes per stage and per camp (DG12, amounts at port time);
Run allowed even from the boss, the stage is refought with a new group (DG13); defeat losses: unequipped items only,
not key or quest items, per copy, rounded down, money by whole purse value, summary shown (DG14, DG15); with a Delve:
nearest settlement, Delve fails, no further cost (DG16); replays unlimited (DG17); Delves: a chance per daily board,
offered by the dungeon's region's guilds, paid like a Great Hunt (×2.4 silver, 3× Reputation), in the standard quest
slot, can reappear (DG18–DG21); Emergency Rope between stages only (DG22); Waylight on the entry warning, skips stage 1
without rewards, never the boss (DG23); regular stages give normal rewards plus materials (DG24); a finished dungeon
gives only the boss's drops (DG25); smithing materials as items (every natural material on every anvil; Mythilium and
Exium never drop), used up per changed part, 75% off, one or two dungeon-only per dungeon, the rest from fitting
enemies and Forage (DG26, DG29–DG32); bosses on the Great Hunts' win-rate curve by simulation, each with intro, own
music theme and own detailed model (DG27, DG28); a music theme per dungeon (DG34); only the task's three masterworks are
new, other bosses drop rare gear etc. (DG35); the new Exotic drops like the five (Limitless only, never doubled, 4–5%)
(DG36), a kind none of the five is (dagger, tower shield, longsword, lance, brigandine are taken) (DG38); masterwork
kinds chosen per boss in the draft (DG37); four extra throwing items cover damage over time, lowered defences,
disrupted abilities, a party buff (DG39); Sparkling in regular stages only (DG40); new items in the General Store's
random stock (DG41); to-hit, damage, AP, durations, hit-chance points, boss stun halving, stun limits, refresh rules,
Flash Bomb +50 points (DG42–DG50); Nocturne Keep: Ravenwatch's unique quest is its Delve, always offered from its
lowest level until done, in the Great Hunt slot; finishing it opens the keep (Vampiric gear included) to non-vampires;
replays from a button in the keep with the lighter penalties and boss drops only; non-vampires may enter before with
full penalties; vampire players only with the quest active; defeat with lighter penalties wakes in Ravenwatch (DG52,
DG55, DG56, DG59, DG60, DG62–DG64, DG66, DG73, DG74); the Delver's "dungeon enemies" are every enemy fought inside a
dungeon (DG67); the boss hidden as a silhouette and "???" until met (DG68); **the dungeon overview is a 3D scene** in
the port's renderer (DG69); guild Hunt/Cull count only their zones, Collect counts dungeon finds (DG71); a dungeon
entrance is a site with a safe radius and an Enter button (DG72). "Following from the answers": the game's save holds
the dungeon state (stage, cleared, bomb stuns used, Waylight, bosses met); new bosses gain Level-100 class levels under
Limitless "their class sets drafted with them".

**Port must:**
- (a) nothing to carry over (but Saffron and Ool move "unchanged" from the Great Hunts, as Ornn did, DG33; an active
  Saffron/Ool hunt in the port's own saves is withdrawn with a note; completed hunts still count for the Huntmaster).
- (b) nothing placeholder; new art and sound: twelve 3D overview scenes (DG69), twelve dungeon music themes (DG34),
  eleven boss intros, themes and detailed models (DG28; the "At port time" model bar), looks for the three masterworks,
  the Exotic, the eleven consumables and the material items, the reforge menu's material marker (DG26).
- (c) build the whole task from `14_Dungeons.txt` and DG1–DG78, after writing and getting approved the port-time drafts
  EP lists at the end of the section (EP 2351–2368, quoted in section 6). Plus the two classes with task 10 (and the
  two new counts: different dungeons finished; throwing items thrown in battle).
- **Order constraints:** DG1 ("built during the port itself"); README "The order": needs task 7's Exotic rarity, task 2's
  guild quests (Delve); Limitless scales its enemies (task 7). Needs the port's 3D renderer (overview, DG69) and 3D map
  (exact spots, DG5); the harpy dungeon needs task 8's rocky highlands made (DG65); its dungeon-only enemy types are
  "drafted at port time with the general enemy revamp" (DG9); the materials change task 3's reforging (DG26–DG32); the
  Throwing Specialist needs task 9's thrown poisons (DG4).

---

## 3. The general task: every enemy revamped (classes and spells)

**Source:** commit `a509e91` (2026-11-11, "Docs: a general port task, every enemy revamped (classes and spells)";
touched EP and PT README only). PT README lines 94–97; EP "At port time" (EP 799–804) and "Decision on the user's tasks
for the port" (EP 1416–1418).

**What it says:** "all enemies are revamped during the port, because their classes have changed (task 10) and task 12
will change the spell system as well. Every enemy, not only those with class levels; the revamp takes both the reworked
classes and the reworked spells into account (… task 10's narrower note on enemies with classes, C2, is part of it)".
The user's words (EP 801–802): "All enemies need to be revamped because their classes has been changed and an upcoming task
will change the spell system as well".

**What other decisions put into it:**
- C2 (EP 2735): "the current enemies that use the old classes (enemy files with class levels, the bosses'
  `BOSS_CLASSES` of task 7, L29) need a full rework, but that happens during the game port, not in this task"; EP
  813–815: "once the port has built task 10's reworked classes; their full rework is not part of task 10's design".
- C11: no class is removed, so enemies keep naming existing classes until the port reworks them (the old game refuses an
  enemy file naming a missing class, `validateEnemy`).
- SP16: "Enemies' spells may carry augmentations, chosen per enemy in the port's enemy revamp …, stronger and later
  enemies more"; `Spell-Augmentation.md` §9: the revamp "may give them weavings if it gives them the Spellweaver class".
  SP1: old enemies and classes "revamped together at the port". `Element-Statuses.md`: statuses work both ways
  ("enemies' spells are revamped at the port, SP16").
- DG9: task 14's dungeon-only enemy types are "drafted at port time with the general enemy revamp"; DG33: Saffron and
  Ool "revamped with every enemy in the port".
- "At port time" requirement (EP 757): boss and enemy models "as detailed and high-quality as the cutscene scenes".

**State:** a recorded requirement only: no draft, no decision series, no scope or method chosen (who drafts, review
form, balance target). Facts for sizing: 174 enemy files carry class levels (C facts, EP 2898; this survey counts 174
files in `old-game/enemies/` besides `index.json`, 12 of them `unique`); 12 `unique` bosses (A13); 11 bosses with
`BOSS_CLASSES` (L29); the old game's enemies suite runs 530 cases (ON, task 11 regression); 49 enemies carry the Beast
class (C19).

---

## 4. Cross-task dependencies and ordering constraints (facts)

**From the docs' own "order" (PT README "The order", checked 2026-11-05):** each task builds only on earlier ones: task
2's Cull before task 5; task 3's reforging before task 9; task 5's Sparkling before task 7; tasks 3 and 6's new
locations before task 8; task 2's guild quests before Collect (9) and Delve (14); caravans (8) and poisons (9) before
task 10's Caravanist and Poisoner; task 7's Exotic rarity before task 14. "They are added to the game during the port,
in that order" (EP 1418).

**Dependencies of the three designed tasks and task 14 (all facts from the drafts and decisions):**
1. Task 11's three racial classes, task 12's two, task 13's 79 and task 14's two are all in task 10's two drafts and are
   "built with the other classes" (T1, SP69, NC19, DG78); C32's three duties cover all 256.
2. Task 13's classes use task 12's mechanics: Catalyst's requirement counts element reactions ("a new count, from task
   12's reactions"); Steadfast's counts task 12's statuses; Enchanter's level 4 "Enchanted weapon hits set off
   reactions as a spell of their element would (task 12) (new)"; Fletcher (task 12's Bleeding and Off-Balance),
   Phantasmist (Hexed), Frostwarden (Chilled and Frozen), Lightbearer (Blessed and Purify)
   (`Class-Rework-Features.md` lines 2984–3993).
3. Task 12's Spellweaver and Ritual Caster need Spell Augmentation and rituals (SP63–SP69); the Spellweaver's
   requirement counts augmentation ranks bought.
4. Task 10's classes use systems of tasks 2–9 (already in the old game): Poisoner on task 9's poisons (C16), Caravanist
   (caravan, task 8), Explorer (regions entered, task 8), Smith/Mastersmith (reforging, task 3; C29), Seeker (Search
   quests, task 2; C30), Informant (hooded man, task 2), Camp Cook (Forage, task 9); the Dragonkin's capstone uses the
   Dragonbreath spells (task 12 keeps at least one per element, SP33).
5. Task 13 builds on tasks 5, 7, 8, 2, 9 (Treasure Hunter, Limitbreaker, Corsair, Huntmaster/Knight-Errant, Drunken
   Master) and on task 2's hooded man (NC7).
6. The general enemy revamp needs task 10's classes built first (C2) and uses task 12's spells, augmentations and
   statuses (SP16); task 14's dungeon enemies are drafted with it (DG9).
7. Task 14 needs: task 2 (guild, Great Hunts, quest slots), task 3 (reforging, for the material discount; the Ornn
   precedent R14/R15), task 5 (Sparkling in stages, DG40), task 7 (Limitless scaling; Exotic rarity; Level-100 boss
   classes), task 8 (continent places DG5, the rocky highlands region DG65, sites DG72), task 9 (thrown poisons DG4;
   Forage finds materials DG32; Collect DG71), task 10 (the classes and their counts; the new bosses' class sets),
   task 13 (the Huntmaster's Great Hunt count; the Moonfang's night time continuing in dungeons, DG12), the general
   enemy revamp (DG9, DG33), the port's 3D renderer (overview DG69) and 3D map (DG5), boss intros/themes/models (DG28).
8. Task 8's new regions are a precondition of one task 14 dungeon (DG65); their Forage (P11) and carriage rules (W33)
   connect to tasks 9 and 8.
9. Task 12's Augmentation menu lives in task 3's Institute of Magic and the Church of Talos (SP12, SP24).
10. Engine side: the engine's soundtrack must gain the region crossfade (task 8; `Audio.md` "Theme API"); the port's
    own record kinds (items, enemies, spells, shops) go into a data library `Statventure.Game.Data` (G3 of M11, closed
    by M12-9, EP 786–790).

**Ordering constraints stated in the decisions (quoted):**
- Tasks 1–9, 11 (races): carried over by the faithful port ("don't build it again"); only their art is remade.
- Task 10: "during the port, the classes in the drafts are added"; "during porting, every class is inspected"; "after
  the port, every class feature is tested" (C32).
- Task 12: the old game "stays unchanged and the port builds them, so the old game's enemies and classes stay balanced
  until they are revamped together at the port" (SP1).
- Task 13: built "from the end of task 10's drafts … with the three duties above" (EP 822–824).
- Task 14: "built during the port itself" (DG1); "Where an answer leaves a draft to port time, that draft is written and
  approved then" (EP 827–828).
- Enemies with classes: reworked "once the port has built task 10's reworked classes" (EP 813–815).
- Open: whether 10, 12, 13, 14 come while porting or after the faithful port (section 6, item 1).

---

## 5. Sizes

| What | Count | Source |
|---|---|---|
| Classes in the old game | 130 (104 none, 18 blue, 6 purple, 2 gold) | C facts, EP 2894 |
| Classes in task 10's drafts | 256 = 170 (task 10: 130 current + 40 new) + 3 (11) + 2 (12) + 79 (13) + 2 (14) | 10_Class_Rework README; EP |
| Classes players can acquire | 252 (Raid Boss, Beast, Great Beast, Monster can't be had) | NC2, DG78 |
| Rarity of the 256 | 164 none, 66 blue, 18 purple, 6 gold, 2 red (summed from: task 10 109/42/13/5/1; task 11 3 none; task 12 1 purple + 1 blue; task 13 51/22/4/1/1; task 14 1 blue + 1 none) | C16, T17, SP69, NC9, DG78 |
| Class drafts' length | `Class-Rework-Draft.md` 1831 lines; `Class-Rework-Features.md` 4388 lines | wc |
| "(new)" marks in class features | 282 (≈133 task 10, 0 task 11, 4 task 12, 136 task 13, 9 task 14) | grep count |
| New requirement counts/kinds | task 13: 22 new counts, 3 new kinds, 1 key item (Moonfang); task 14: 2 counts; task 10: several (not listed separately) | `Class-Rework-Draft.md` 1781–1831 |
| Spells in the old game | 388 (381 learnable + 7 boss/enemy-only); 29 summon spells; 23 Dragonbreath spells | SP preamble, SP33 |
| Spells after task 12 | 381 reworked + 168 new = 549 learnable; + 7 unchanged = 556 | SP47, SP62, SP61 |
| Spell system pieces | 12 statuses, 12 reactions, 28 borrowing spells, 4 new summons, 2 new creatures, 12 Tier 10 animations, 1 Augmentation menu with 12 element circles | Element-Statuses.md, SP47, SP25 |
| "(new)" marks in task 12 files | 140 | grep count |
| Potions / poisons / ingredients / recipes | 40 new potions (8/12/10/6/4) + 10 Health and Mana; 12 poisons (2/4/4/2); 56 ingredients (15/18/13/6/4); 62 recipes | 9 README, draft.md |
| Consumables in the old game | 71 (Camping Supplies, 8 traps, 10 Health/Mana, 40 potions, 12 poisons) | EP task 14 facts |
| Artifacts | 80 non-masterwork (32/24/16/8) + 4 masterwork (task 4) + Treasure Seeker (task 5) | A16, M15 |
| Masterworks | 6 (task 1), Doctor's Orders (3), Moneyshot, Lament, Noble Hero, Hexbane Coat, Treasure Seeker (5); M10's Sparkling pool: 35 masterwork weapons, armour and artifacts; task 14 adds 3 | ON, M10, DG35 |
| Exotics | 5 (task 7) + 1 (task 14) | L, DG36 |
| Traps | 8 (3 old + 5 from task 3) | R17–R21, EP task 14 facts |
| Regions | 17 existing + 3 new (empty) = 20; 27 settlements and 3 sites in `regions.json` (pre-move export; this survey's count) | 8 README, regions.json |
| Enemies | 174 enemy files; 12 `unique`; 11 bosses with `BOSS_CLASSES`; task 6 added 6 | C facts, A13, L29, N9 |
| Enemies the port adds | 11 new dungeon bosses (8 named incl. Goblin Champion, + vampire lord, harpy boss, dragon boss); dungeon-only types (count drafted at port time; stronger harpies above Level 46); 2 summon creatures (task 12); the new regions' enemies (undecided) | EP 2351–2362, DG57, SP |
| Dungeons | 12; the nine named have 61 stages (4+5+6+8+5+9+6+10+8); the three extra's stages at port time | task text, DG |
| Task 14 items | 3 masterworks, 1 Exotic, 11 General Store consumables (7 named + 4 extra; Emergency Rope and Waylight are not thrown), smithing materials as items (every natural anvil material; count not stated) | task text, DG29, 14_Dungeons/Classes.md |
| Task 14 audio/3D | 12 dungeon themes, 11 boss themes, 11 intros, 11 boss models, 12 3D overview scenes | DG28, DG34, DG69 |
| Placeholder art items to remake | task 1: 6 masterwork looks (item + battle sprite); task 2: 2 interiors; task 3: 3 scenes, 2 map icons, 1 window, island/bridge painting, 1 robe, anvil, 5 traps; task 4: 80 artifact looks + 4 masterwork drawings; task 5: Sparkling look + 5 masterwork looks; task 6: 6 sprites + map painting; task 7: 5 Exotic looks, button, pop-up; task 8: the whole 2D map → 3D map; task 9: 2 banners, cauldron + animation, 108 icons (40 + 12 + 56); task 11: 3 seals, 24 looks | PT README "Placeholder art" |
| Old game checks per task (mutations) | 1: 22; 2: 94; 3: 121; 4: 85; 5: 103; 6: 64; 7: 100; 8: 44; 9: 97; 11: 22 | ON |
| Decision counts | G34, R29, A32, M26, N10, L33, W35, P31, C32, T17, SP69, NC19, DG78 | EP |

---

## 6. Open questions the docs leave to the port or the user

1. **While porting or after the faithful port (tasks 10, 12, 13, 14).** EP 33–35: "**the open question for the user:**
   whether the approved port tasks 10, 12, 13 and 14 are built while porting or after the faithful port (left open since
   2026-11-05)". PT README "When": "(still open for them: before the port, after the faithful port, or while
   porting)"; EP 1418–1419: "Still open: whether they come after the faithful port or while porting (the user will
   answer later)". Note: DG1 already says task 14 is "built during the port itself"; EP still lists 14 under the open
   question, so whether DG1 means "while porting" is not stated.
2. **The red rarity's look.** EP 2886–2888 ("Still to ask (nothing assumed)"): "The red rarity's look (Unreality's colour
   in the class list and details, and the Limitbreaker's since task 13, NC6), when the port builds the classes (C16,
   C23)."
3. **The three new regions** (W27): "their names, identity, terrain, enemies, level bands, towns and connections are made
   in the port"; 8 README: "The three new regions' names, identities, level bands and contents are **not decided** … The
   labels and level bands in the picture are only the draft's proposals." Constraints already set: harpy dungeon in the
   highlands at 50–56 (DG65); Forage not there until the port (P11); carriage may cross them (W33); task 8's text section
   3 ("fully implemented rather than serving as empty filler space").
4. **Task 14's port-time drafts** (EP 2351–2368, "Left for the drafts the port writes and the user approves then"):
   "The twelve dungeons: their stages (counts given by the task for the nine; for the three extra ones, in their
   drafts), each stage's pool of groups (DG7) and the dungeon-only enemy types (DG9) with the general enemy revamp; the
   three extra dungeons' names and identities …; the exact spots on the 3D map (DG5); the music themes (DG34) and the 3D
   overview scenes (DG69)"; "The eleven new bosses …: mechanics, intros, themes, models (DG27, DG28, DG58), balanced by
   simulation; their drops (DG35)"; "The three masterworks (kinds per boss, DG37) and the Exotic (a kind none of the five
   is, DG38), with drop chances"; "The eleven throwing items' numbers (prices, the knives' damage, DG43) and the four
   extra ones (DG39)"; "The smithing materials as items: which drop where and in which dungeons, their Forage odds and
   prices (DG29–DG32); the reforge menu's mark for a carried material (DG26)"; "The numbers left open: the Delve chance
   per board (DG18), the time a stage and a camp take (DG12), Ravenwatch's quest text and reward (DG55)." Also "only
   the two new counts their requirements use are left" for the two classes.
   Wording point: DG41 says "the eleven (and the four of DG39) join the rarity-weighted pool", while the task text says
   "all eleven throwing consumables" (7 + 4); read together the total is eleven, but DG41's phrasing could be read as
   fifteen.
5. **The general enemy revamp** has no design yet (section 3): scope "every enemy", method and review form not decided;
   C2: "their full rework is not part of task 10's design".
6. **Class numbers to tune.** 10 README: "The numbers are proposals to tune when built"; features file: "Numbers are
   proposals to tune when built". C32 (2) asks every class to be inspected for errors and inconsistencies during porting.
7. **Task 11 balance note** (ON 1086–1089): the Goliath's Giant's Blood vs the human's Rulers of the World at low stats,
   "A balance point for the port's race review, not changed here."
8. **The game-screen references.** PT README "Found before the work" and EP 768–783: the 22 captured screens and the
   tests' stylesheet copy predate the tasks; "When (before the port starts, or screen by screen as the port reaches
   each) is a question to ask at port time".
9. **Other "At port time" questions** (EP 757–790), not tied to one task but touching their art: "which sounds the game
   gets (… the rune slotting sound among them); UI symbols outside Lato and STIX (⚙ ✦ ☾ ↓ ＋ ⚔ ♟ …); how the inline SVG
   art and its transforms map to the Blender remakes; the grabbing cursor's design; the level of detail of the item
   models"; M11's G2 (what the game does with a mod that has parts it can't read; "recommended then: as today"); M12's
   in-game update check once public (M12-7), new icons for the game and editor with the port's art pass (M12-5), the
   BRUTAL logo and copyright in the game's own screens (Q11). The requirement (not a question): boss and enemy models as
   detailed as the cutscene scenes.
10. **Task 12's art and animation** have no design beyond SP25 and "made in the port": the statuses' looks and sounds
    (`Element-Statuses.md`), the Mana Golem's and Storm Roc's looks ("made by the port with its look and stats",
    `New-01-Basic.md`), and how the Tier 10 animations are reworked (SP47).
11. **Before each task's code**, PT README "How to use them": "ask its design questions with the question tool and
    record the answers in `Engine-Plan.md`, as for every step." For tasks 1–9 and 11 "their design questions are answered
    and their rules are already in its code and data."

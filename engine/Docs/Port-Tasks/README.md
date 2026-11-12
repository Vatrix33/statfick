# Tasks for the port

The user's tasks to add to the game during the port (user decision 2026-11-05), done in their number order (a plain
file listing puts `10_` before `1_`; the list below is the order). The `.txt` files are the user's own text, unchanged
(only their line endings were made LF, as the repo stores text), except three names in task 7 the user confirmed on
2026-11-05: "Laila" is Juila, "Zik" is Zik Lun, "Mythium" is Mythilium; and one in task 10 the user confirmed on
2026-11-10: "Ascendend" is Ascended. Fourteen tasks.

1. `1_Masterwork_Elementals.txt`: six masterworks remade from the elementals' former unique items (Plasma Blade,
   Tidepiercer, First Comet, Peace and Balance, Kugelblitz, Avalanche); the elementals' placeholder weapons stay.
   **Already done in the old game (2026-11-05): don't build it again.** The port carries it over with the rest of the
   game; only its placeholder art is left to remake (below).
2. `2_Guild.txt`: the Adventurers Guild's quest system rework (Hunt, Cull, Deliver, Search, Escort, Travel; Great Hunts
   kept) and its illustrated, clickable interior (castle town and smaller guilds), tavern recruitment, the hooded man,
   and a class information button.
   **Already done in the old game (2026-11-06): don't build it again.** The port carries it over with the rest of the
   game; only its placeholder art, the two guild interiors, is left to remake (below).
3. `3_Shop_Rework.txt`: reforging at the Blacksmith, a Tailor in every town, the Institute of Magic on the lake, Hearth
   Home at the volcano (Ornn as its boss instead of a Great Hunt), the Doctor's Orders masterwork, five new traps.
   **Already done in the old game (2026-11-06): don't build it again.** The port carries it over with the rest of the
   game; only its placeholder art is left to remake (below).
4. `4_Artifact_Rework.txt`: a look for every artifact, new effects for the non-masterwork ones (at least 32 uncommon,
   24 rare, 16 epic, 8 legendary), level scaling for masterwork artifacts.
   **Already done in the old game (2026-11-06): don't build it again.** The port carries it over with the rest of the
   game; only its placeholder looks are left to remake (below).
5. `5_New_Masterworks.txt`: Sparkling enemies with Treasure Seeker and Moneyshot, the Lament and Noble Hero
   masterworks, a new masterwork armor, the Shadow Crystal as a rare drop from every enemy.
   **Already done in the old game (2026-11-06): don't build it again.** The port carries it over with the rest of the
   game; only its placeholder art is left to remake (below).
6. `6_New_Areas.txt`: a demon/abyss area in the marsh with corrupted enemies (at least three demon/impid and three
   corrupted enemy types).
   **Already done in the old game (2026-11-06): don't build it again.** The port carries it over with the rest of the
   game; only its placeholder looks are left to remake (below).
7. `7_New_Game_Plus.txt`: Limitless, unlocked by the first defeat of Anatemis and switchable on and off (level cap 200,
   enemies scaled to the player and 25% stronger, bosses with level-100 class perks, doubled masterwork drops), the
   Exotic rarity and its material Exium, five Exotic items, and an Exotic reward for beating Anatemis at level 200.
   **Already done in the old game (2026-11-06): don't build it again.** The port carries it over with the rest of the
   game; only its placeholder art is left to remake (below).
8. `8_Map_Rework.txt`: one continuous continent about 40% larger instead of separate regional maps, music that crosses
   over between regions, a boat-travel animation, fog and clouds as the mountain rises, caravan fast travel between
   visited castle towns, and a horse carriage to buy.
   **Already done in the old game (2026-11-07): don't build it again.** The port carries it over with the rest of the
   game; left for the port: its placeholder looks, remade in the port's 3D map (below), and the three new regions.
   Its layout (draft 6, 2026-11-07, `Engine-Plan.md`, "Decisions for task 8", W26): the picture, the scripts that make
   it and every region's place are in `8_Map_Rework/`, the reference for the port's visual redesign of the map. Its
   three new regions (rocky highlands east of the Alder Vale, a steppe south of it, a west-coast delta below the
   Verdigris Jungle) are left empty in the old game (walkable, no encounters, parchment marked "Uncharted"): their
   names, identity, terrain, enemies, level bands, towns and connections are all made in the port (the user, W27).
9. `9_Potion_Rework.txt`: an Apothecary in every town, 40 new potions and 12 poisons, at least 50 ingredients, foraging
   on the map, brewing and recipes, the Alchemist and Assimilator classes, and a Collect quest for the guild.
   **Already done in the old game (2026-11-07): don't build it again.** The port carries it over with the rest of the
   game; only its placeholder art is left to remake (below). The approved content (every potion, poison, ingredient
   and recipe) is `9_Potion_Rework/draft.md`.
10. `10_Class_Rework.txt`: every class reviewed and reworked where needed (identity, requirements, passive and skill, a
    capstone, its colour rarity), then 26 named new classes and 15 more; for the new classes the user is asked about
    each one's identity, levels, requirements and rarity, in that order.
    **Designed and approved (2026-11-10 to 2026-11-11), for the port:** not built in the old game; the port builds it
    from the approved drafts in `10_Class_Rework/` (C23, C31; its README says what each file holds): every class's
    identity, levels, requirements and rarity (130 current, 40 new), and every class's features level by level. See
    "Designed for the port" below.
11. `11_New_races.txt`: three new playable races with icons (Half-Beast, Merfolk made playable, Goliath), each with a
    racial class.
    **The races are done in the old game (2026-11-11): don't build them again;** the port carries them over with the
    rest of the game, and only their placeholder seals and looks are left to remake (below). Their three racial classes
    (Wildheart, Deepcaller, Peakwarden) are not in the old game: they are in task 10's drafts, built by the port (T1).
12. `12_Spell_Rework.txt`: every spell brought up to date (power, scaling, effects, mana cost; tiers kept), two new
    spells per element in tiers 1–5 and one in tiers 6–9 (at least four new summons), and the Spellweaver and Ritual
    Caster classes.
    **Designed and approved (2026-11-11 to 2026-11-12), for the port:** like task 10, not built in the old game (SP1);
    the port builds it from the approved drafts in `12_Spell_Rework/` (its README says what each file holds). See
    "Designed for the port" below. Decisions SP1–SP69 in `Engine-Plan.md`, "Decisions for task 12, Spell Rework".
13. `13_New_classes.txt`: new classes by focus (four each for melee, ranged, spells and the rogue path, two for each of
    six combinations, nine without a focus), then more until players can acquire 250 classes; the user is asked about
    each one's identity, levels, requirements and rarity, in that order.
    **Designed and approved (2026-11-12), for the port:** like task 10, not built in the old game (NC1); 79 new
    classes, the 37 by focus and 42 more (171 of task 10's 175 could be acquired; NC2), now at the end of task 10's
    drafts in `10_Class_Rework/` (NC19; drafted in `13_New_classes/`, which stays as the record). See "Designed for the
    port" below. Decisions NC1–NC19 in `Engine-Plan.md`, "Decisions for task 13, New Classes".
14. `14_Dungeons.txt`: dungeons (nine named, three more), each with stages and a boss; Camping Supplies between
    stages; defeat penalties and a warning before entry; the Delve guild quest; Saffron and Ool move from Great Hunts
    to dungeon bosses; three masterworks and an Exotic; the Dungeon Delver and Throwing Specialist classes; eleven
    throwing consumables for the General Store.
    **Built during the port itself (the user, 2026-11-12, DG1):** nothing in the old game, nothing built or drafted
    before the port; its design questions are asked and answered beforehand, in `Engine-Plan.md`, "Decisions for task
    14, Dungeons" (DG1 onward), and the port builds it from this file and those answers. See "Built by the port from
    the decisions" below.

**How to use them:** read them when the port starts (`Engine-Plan.md`, "Open questions for later milestones", "At port
time" points here). Before each task's code, ask its design questions with the question tool and record the answers
in `Engine-Plan.md`, as for every step. Skip the tasks under "Done in the old game so far": they are part of the game the
port starts from, so their design questions are answered and their rules are already in its code and data. For those,
only the art under "Placeholder art the port remakes fully" is left.

**A general port task beside the tasks (the user, 2026-11-11):** all enemies are revamped during the port, because their
classes have changed (task 10) and task 12 will change the spell system as well. Every enemy, not only those with class
levels; the revamp takes both the reworked classes and the reworked spells into account (`Engine-Plan.md`, "At port
time"; task 10's narrower note on enemies with classes, C2, is part of it).

**Also for the port, from task 12 (the user, 2026-11-11, SP47):** the Tier 10 spell animations (the old game's
full-screen cinematics, `old-game/scripts/tier10-cinematics.js`) are reworked to go with the reworked Tier 10 spells.

**The order (checked 2026-11-05):** each task builds only on earlier ones (the Cull quest of task 2 before task 5, the
reforging of task 3 before task 9, Sparkling enemies of task 5 before task 7, the new locations of tasks 3
and 6 before the map rework of task 8, the guild quests of task 2 before Collect and Delve, caravans and poisons
before the Caravanist and Poisoner of task 10, the Exotic rarity of task 7 before task 14), with these points to
remember (the user's answers, 2026-11-05):
- Task 7's "dungeon enemies" is meant to be future-proof: Limitless scales every kind of enemy, also those added later.
  Dungeons only come with task 14 (which needs task 7's Exotic rarity), so task 14 gives its dungeon enemies the
  Limitless scaling.
- Task 13 adds classes until there are 250; task 14's two classes come on top (252 in the end), as intended. (Done:
  task 13's 79 bring the classes players can acquire to 250, NC2.)

**When:** task 1 is done in the old game before the port (user decision 2026-11-05), its new art as placeholders; task 2
is built in the old game too (user decision 2026-11-06, G1), also with placeholder art, and so is task 3 (the user,
2026-11-06, "same rules apply"; R1); the other tasks are decided one at a time (still open for them: before the port,
after the faithful port, or while porting). Task 4 too was built in the old game (the user asked for it on 2026-11-06;
A1 kept its art placeholder), and so were task 6 (the user asked for it the same day; N4 kept its art placeholder) and
task 5 (asked for after tasks 4 and 6 were merged; M1 kept its art placeholder), and task 7 (asked for after task 5
was merged, while M11's engine work went on; L1 kept its art placeholder), and task 8 (asked for after task 7 was
merged; W1 kept its 2D looks placeholder for the port's 3D map), and task 9 (asked for while task 8's pull request
waited, into the same pull request, P1; P2 kept its art placeholder). Task 11 (asked for after task 10's design, on
2026-11-11) is split (T1): its races, seals and looks in the old game, with placeholder art (T5); its racial classes
in task 10's drafts for the port. Tasks 10, 12 and 13 are not built in the old game at all: designed in drafts and
built by the port (C23, 2026-11-10; SP1, 2026-11-11; NC1, 2026-11-12), with task 12's two classes and task 13's 79
added to task 10's drafts. Task 14 is built during the port itself (the user, 2026-11-12, DG1): not in the old game and
not drafted beforehand; only its design questions are answered before the port.

**Done in the old game so far:**
- Task 1, Masterwork Elementals (2026-11-05): the six masterworks, their drops and effects, all in the old game's code
  and data, so the faithful port brings them over like any other masterwork. The design answers and where each rule
  lives are in `../../../old-game/Docs/Notes.md`, "Masterwork Elementals"; the old game's checks for it,
  `old-game/test-client/tests/masterwork-elementals-browser.js` (one entry per rule), are the reference the ported
  rules are tested against. Still to do in the port: only the art (below). The task's `.txt` stays as the user wrote it
  (no "done" mark in it); this README and `Engine-Plan.md` record that it's done.
- Task 2, Guild Rework (2026-11-06, a cloud session, merged through a pull request): the six quest types, the daily
  boards, the rewards, the clickable guild interior, the recruits, the map's Quests card and the class information
  button, all in the old game's code and data, so the faithful port brings them over like the rest of the guild. The
  user's 34 design answers are in `Engine-Plan.md`, "Decisions for task 2, Guild Rework" (G1–G34); where each rule lives
  is in `../../../old-game/Docs/Notes.md`, "Guild Rework"; the old game's checks for it,
  `old-game/test-client/tests/guild-rework-browser.js` (one entry per rule), are the reference the ported rules are
  tested against. Still to do in the port: only the art (below). The task's `.txt` stays as the user wrote it (no "done"
  mark in it); this README and `Engine-Plan.md` record that it's done. It touched only `old-game/` and the docs, never
  the engine (the user, 2026-11-06).
- Task 3, Shop Rework (2026-11-06, a cloud session, merged through a pull request): reforging at four anvils (Blacksmith,
  Tailor, Institute of Magic, Hearth Home), the Tailor in every settlement with a Blacksmith, the Institute of Magic on
  its island in Mirrormere's lake, Hearth Home with Ornn as its site boss (no longer a Great Hunt), Doctor's Orders and
  the five traps, all in the old game's code and data. The user's 29 design answers are in `Engine-Plan.md`, "Decisions
  for task 3, Shop Rework" (R1–R29); where each rule lives is in `../../../old-game/Docs/Notes.md`, "Shop Rework"; the old
  game's checks for it, `old-game/test-client/tests/shop-rework-browser.js`, are the reference the ported rules are
  tested against. Still to do in the port: only the art (below). The task's `.txt` stays as the user wrote it. It
  touched only `old-game/` and the docs, never the engine (the user, "same rules apply" as task 2).
- Task 4, Artifact Rework (2026-11-06, a cloud session, merged through pull request #10 with task 6 on 2026-11-06): the old artifact set replaced by 80
  new ones (32 uncommon, 24 rare, 16 epic, 8 legendary, each with its own effect), the four masterwork artifacts on a
  level ramp (fixed at the level obtained; about uncommon strength at Level 1, legendary at 80, rising beyond), rare
  enemy drops, old saves cleaned, and placeholder looks for every artifact. The user's 32 design answers are in
  `Engine-Plan.md`, "Decisions for task 4, Artifact Rework" (A1–A32); the approved list and where each rule lives are in
  `../../../old-game/Docs/Notes.md`, "Artifact Rework"; the old game's checks,
  `old-game/test-client/tests/artifact-rework-browser.js`, are the reference the ported rules are tested against. Still
  to do in the port: only the looks (below). Old game only, as tasks 2 and 3.
- Task 6, New Areas (2026-11-06, a cloud session, merged in the same pull request as task 4, #10, on 2026-11-06): the Abyssal Rift, a new
  Level 45–55 zone in Gloamfen Marsh's south-west, with three corrupted marsh creatures (Corrupted Bog Troll, Mire
  Stalker with Dark strikes, Mossback Lamia with Dark spells; all demon race) and three demons (Abyssal Imp, Rift
  Fiend, Abyss Witch), nine encounter groups, guild Hunts there, and the marsh's range raised to 35–55. The user's
  design answers are in `Engine-Plan.md`, "Decisions for task 6, New Areas" (N1–N10); where each rule lives is in
  `../../../old-game/Docs/Notes.md`, "New Areas"; the old game's checks,
  `old-game/test-client/tests/new-areas-browser.js`, are the reference the ported rules are tested against. Still to do
  in the port: only the looks (below). Old game only. Task 8 (the map rework) places the Rift on the continent.
- Task 5, New Masterworks (2026-11-06, a cloud session, merged through pull request #11 on 2026-11-06): Sparkling enemies (a rare, stronger,
  richer version of any regular enemy, highlighted on the encounter screen, dropping masterworks), Treasure Seeker and
  Moneyshot (Sparkling only), Lament (2% from the Tide Siren), Noble Hero (1% from non-boss demons and devils), the
  Hexbane Coat (the new masterwork armor, drafted and reviewed; 1% from non-boss undead, demons and devils) and the
  Shadow Crystal at 0.05% from every enemy. The user's design answers are in `Engine-Plan.md`, "Decisions for task 5, New
  Masterworks" (M1–M26); where each rule lives is in `../../../old-game/Docs/Notes.md`, "New Masterworks"; the old game's
  checks, `old-game/test-client/tests/new-masterworks-browser.js`, are the reference the ported rules are tested against.
  Still to do in the port: only the art (below). Old game only.
- Task 7, New Game Plus (2026-11-06, a cloud session, merged as pull request #12, L22): Limitless (unlocked by Anatemis's first
  defeat with a pop-up; the sheet's Limitless button; Levels up to 200 without new perk points; off, you and your
  companions are your Level-100 selves and everything above is kept for when it's on again; enemies rise to your level
  with +25% attributes; bosses gain the class levels of a Level-100 character, reviewed by the user; masterwork drops
  doubled; gear past Level 100, which can't be worn while it's off), the Exotic rarity (a green and purple wave), Exium,
  and five Exotics: Fletcher, Tough-Out, Winged Hussar, Cherubim and Connected Balancer, with Anatemis's Level-200
  reward. The user's design answers are in `Engine-Plan.md`, "Decisions for task 7, New Game Plus" (L1–L33); where each
  rule lives is in `../../../old-game/Docs/Notes.md`, "New Game Plus: Limitless and the Exotics"; the old game's checks,
  `old-game/test-client/tests/limitless-browser.js`, are the reference the ported rules are tested against. Still to do
  in the port: only the art (below). Old game only. Later enemies (task 14's dungeons and the like) scale through the
  same `scaleEnemy` when they spawn.
- Task 8, Map Rework (2026-11-06 to 2026-11-07, a cloud session, one pull request): one continent instead of separate
  regional maps, laid out as the user's approved draft 6 (`8_Map_Rework/`): every region whole, 1.4 times larger, so
  every trip takes 1.4 times longer with as many encounters on it as before (W4); a natural coast and two inland waters; no
  barriers between neighbours (the map's header names the region you stand in and its levels); Cinder Isle 250 km out
  (the boat 12.6 hours); regions under fog until first entered; clicks that walk across regions along the fastest way
  (off the roads only through entered regions); the music crossfading at borders; the boat trip played on the map; fog
  and clouds as the Stoneveil rises; the caravan between visited castle cities; the horse carriage. The three new
  regions are empty until the port (W27–W29). The user's design answers are in `Engine-Plan.md`, "Decisions for task 8,
  Map Rework" (W1–W35); where each rule lives is in `../../../old-game/Docs/Notes.md`, "Map Rework: one continent"; the
  old game's checks, `old-game/test-client/tests/map-rework-browser.js`, are the reference the ported rules are tested
  against. Still to do in the port: the looks (below) and the three new regions; and the engine's soundtrack, ported
  before task 8, gains the crossfade between region themes (`Docs/Audio.md`, "Theme API"). Old game only.
- Task 9, Potion Rework (2026-11-07, a cloud session, in the same pull request as task 8, P1): the Apothecary in every
  settlement (potions, ingredients, brewing at a cauldron, recipes, selling), the ten Health and Mana Potions always on
  sale there by level band and gone from the General Store, 40 new potions and 12 throwable poisons (half strength on
  bosses), 56 ingredients foraged on the map (Forage, an hour) and dropped by every enemy, 62 three-ingredient recipes
  (the two Lesser ones known from the start, the rest bought at ten times the potion), the Alchemist (better foraging,
  Ingredient Lore) and the Assimilator (eats ingredients in battle for buffs), and the guild's Collect quest. The user's
  design answers are in `Engine-Plan.md`, "Decisions for task 9, Potion Rework" (P1–P31); the approved content is
  `9_Potion_Rework/draft.md` (its README says what the folder holds); where each rule lives is in
  `../../../old-game/Docs/Notes.md`, "Potion Rework"; the old
  game's checks, `old-game/test-client/tests/potion-rework-browser.js`, are the reference the ported rules are tested
  against. Still to do in the port: only the art (below). Old game only.
- Task 11, New Races (2026-11-11, a cloud session): Half-Beast (balanced feral stats; Natural Claws, its unarmed
  attacks as claws that grow with its level and keep every unarmed bonus) and Goliath (mighty and slow; Giant's Blood,
  +10% maximum HP and +5% Armor for −10% Evasion) are new playable races, and Merfolk are playable, keeping the enemies'
  stats and Fishman Physiology; each has a seal, four male and four female looks (Half-Beasts as wolf, fox, bear and
  lynx kin with beast ears, tails and claws; Merfolk on legs with fin ears; Goliaths a head taller) and recruit names,
  so they join the taverns. The user's design answers are in `Engine-Plan.md`, "Decisions for task 11, New Races"
  (T1–T17); where each rule lives is in `../../../old-game/Docs/Notes.md`, "New Races"; the old game's checks,
  `old-game/test-client/tests/new-races-browser.js`, are the reference the ported rules are tested against. Still to
  do in the port: the art (below) and the three racial classes, which are in task 10's drafts (`10_Class_Rework/`, at
  the end of both files). Old game only for the races; it touched no engine file.

**Designed for the port, not built in the old game:**
- Task 10, Class Rework (2026-11-10 to 2026-11-11, a cloud session, C1–C32 in `Engine-Plan.md`, "Decisions for task 10,
  Class Rework"): the user had the old game left as it is (C23) and the reworked classes built by the port from
  `10_Class_Rework/`: `Class-Rework-Draft.md` (identity, levels, requirements and rarity of all 170 classes: the 130
  current, every one reviewed, and 40 new: 25 of the task's names, Poisoner already existing and reworked, seven names
  from `old-game/classes/newc/` and eight of the session's own) and `Class-Rework-Features.md` (each class's levels,
  the old beside the remade for current classes, approved in a sample of four and eleven batches). Rules: one passive
  and one skill per class (exceptions: the elemental mages, the item classes, the Vampire Lord, the Ki Warrior, the
  Psykic; enemy-only classes outside the rules), the passive or skill at the last level, requirements and skills
  within a turn's 5 AP. A new rarity, red, above gold, for Unreality (and, since task 13, the Limitbreaker; its look
  decided when the port builds it).
  Also for the port: the full rework of every enemy that uses classes (C2), and the mechanics the features mark "(new)".
  **The port's three duties (the user, 2026-11-11, C32):** the classes are unchanged in the old game, and during the
  port the classes in the drafts are added; during porting, every class is inspected for possible errors and
  inconsistencies; after the port, every class feature is tested so that it works as intended.
- Task 12, Spell Rework (2026-11-11 to 2026-11-12, a cloud session, SP1–SP69 in `Engine-Plan.md`, "Decisions for task
  12, Spell Rework"): the old game stays as it is (SP1) and the port builds from `12_Spell_Rework/`: **Spell
  Augmentation** (`Spell-Augmentation.md`: the Augmentation menu with a magic circle per element, made only in the
  port, SP25; ranks, attributes, costs, discounts, resets; the Spellweaver's weavings; the power curve every spell
  follows), the **element statuses and reactions** (`Element-Statuses.md`, with 28 spells borrowing another element's
  status), **every spell players can learn reworked** (381, `Batch-01-Basic.md` to `Batch-12-Blood.md`; the seven boss
  and enemy-only spells left as they are, SP4) and **168 new spells** (`New-01-Basic.md` to `New-12-Blood.md`, fourteen
  per element, every element keeping at least one summon and one Dragonbreath spell). Its two classes, the Spellweaver
  and the Ritual Caster, are in task 10's drafts (`10_Class_Rework/`, 175 classes then, 254 with task 13's). Also for
  the port: the mechanics the drafts mark "(new)"; two new creatures for the new summons, the Mana Golem (Basic) and
  the Storm Roc (Wind), with their looks and stats (Nature's Dryad and Death's Wraith use creatures the game has); the
  Tier 10 spell animations reworked (SP47); bosses can't be defeated outright by instant death (SP46); and every
  enemy's spells, augmentations included, revamped with the general enemy revamp (SP16).
- Task 13, New Classes (2026-11-12, a cloud session, NC1–NC19 in `Engine-Plan.md`, "Decisions for task 13, New
  Classes"): designed like task 10's classes (NC1) and built by the port from task 10's drafts, at whose end they now
  stand (`10_Class_Rework/`: "Classes added by task 13" in `Class-Rework-Draft.md`, "Task 13's classes" in
  `Class-Rework-Features.md`; drafted and reviewed in `13_New_classes/`, which stays as the record). 79 classes: the
  task's 37 by focus (4 melee, 4 ranged, 4 spells, 4 from the rogue's path, 2 for each of the six pairs, 9 without a
  focus) and 42 more chosen to fill gaps, so that players can acquire 250 (NC2); each focus class builds on its path
  (NC3) and each is built in its own shape (NC11). Rarity: 51 without a colour, 22 blue, 4 purple, 1 gold (the
  Paragon), 1 red (the Limitbreaker, the second red class, NC6). Also for the port: **the hooded man offers no
  information on a red class while the player is Level 99 or lower** (from Level 100 he offers both, the Limitbreaker
  despite its Level 110; NC7); the new counts, requirement kinds and the Moonfang key item the requirements use
  (listed at the end of task 13's part of `Class-Rework-Draft.md`); and the mechanics the features mark "(new)". Task
  10's three duties (C32) cover these classes too.

**Built by the port from the decisions, not designed in drafts:**
- Task 14, Dungeons (2026-11-12, a cloud session, DG1 onward in `Engine-Plan.md`, "Decisions for task 14, Dungeons"):
  the user wants it done during the port itself. Nothing of it is in the old game or in drafts; the design questions
  are answered and recorded beforehand, with what the old game has for it and lacks (checked 2026-11-12), and the port
  builds the whole task from `14_Dungeons.txt` and those answers.

**Placeholder art the port remakes fully** (the user wants it fully visually remade during the port; in the old game's
code task 1's and task 3's masterwork definitions and task 4's artifacts carry `placeholderArt: true`, and task 2's and
task 3's scenes are listed in `PLACEHOLDER_ART`):
- Task 1: the looks of Plasma Blade, Tidepiercer, First Comet, Peace and Balance, Kugelblitz and Avalanche, in their
  item art and battle sprites. Today each is its type's normal forged masterwork look with simple hints: a short red
  line from the Plasma Blade's gem (the design: a red laser beam from the gem, like a laser sword) and three prongs on
  Tidepiercer's head (the design: a lance shaped like a trident, with a crossguard instead of a pommel).
- Task 2: the two guild interiors (the castle-city guild hall, `guildHall`, and the town guild, `guildTown`, in
  `old-game/scripts/scene-banners.js`). Today they are simple ink rooms made with little effort on purpose (user decision
  G2, "Placeholder, so don't put much effort into it"), marked PLACEHOLDER ART in the code and listed in its
  `PLACEHOLDER_ART` (`SceneBanners.placeholderArt("guild-hall")` and `("guild-town")` return true). The port remakes them
  fully as the detailed illustrations the task asks for (`2_Guild.txt`, sections 10, 17 and 18: a large, elaborate
  castle-town hall and a modest, compact town guild that visibly differ), keeping what the user decided: the guild on
  the left (bulletin board beside the counter and its clerk), the tavern on the right (a hearth, a table of
  adventurers and, in castle cities only, the hooded man alone at a corner table), hunting trophies on the back wall
  (G21); every one of those objects clickable, with a candle-gold outline glow and its name on hover or keyboard focus
  (G23); the room in the upper part, above the panel docked along the bottom (G22). The rules behind the objects are not
  placeholder and stay as they are.
- Task 3 (user decision R1, placeholder like task 2): the Tailor's shop scene (`tailorShop` in `scene-banners.js`:
  bolts of cloth and a dress form in the shop frame), the Institute of Magic's inside (today the Magic Tower's sanctum,
  `institute`) and Hearth Home's (today the Blacksmith's arms hall, `hearth`), all three in `PLACEHOLDER_ART`; their map
  icons (`SITE_ICONS` in `map.js`: a domed tower, a forge with a flame) and Hearth Home's window (a plain panel like the
  Church's); the island and its bridge as painted on Mirrormere's map (`map-regions.js`); Doctor's Orders' look (the
  normal forged robe in white, `placeholderArt: true`); the reforge anvil drawing (`reforge.js`); and the five traps'
  looks (the General Store's generic goods art). The rules behind them are not placeholder.
- Task 4 (user decisions A1, A2): every artifact's look (`artifactArt` in `old-game/scripts/forge-art.js`: an object per
  artifact with a small mark, in its rarity's shared material: worn bronze with plain glass for uncommon, polished
  silver with a cut blue-white gem for rare, gold filigree with a faceted violet gem and two side stones for epic, white
  gold with a glowing core and a halo for legendary) and the four masterwork artifacts' own drawings (Second Wind, a
  glass lung with wind; Wraithguard, a pale gauntlet trailing mist; Arrowtip Rain, a quiver with falling arrowheads;
  Titanfall, a cracked stone fist). The port remakes them as proper item art: artifacts of a rarity similar with a few
  details off, masterworks each completely their own (the task's own words). The rules behind them are not placeholder.
- Task 6 (user decision N4): the six Abyssal Rift enemies' battle sprites (`old-game/scripts/sprite-catalog.js`, after
  the Void Stalker: the Corrupted Bog Troll, Mire Stalker and Mossback Lamia are their marsh bases darkened with
  glowing red eyes, the troll with violet veins; the Abyssal Imp an ember-red imp; the Rift Fiend and Abyss Witch are
  the devil kit in charcoal and in a violet robe) and the Rift on Gloamfen's map (`map-regions.js`: a dark violet scar
  with a red-glowing heart, painted over the mire, and its label). The port remakes them as proper art: the corrupted
  creatures recognisably their marsh kin, corrupted as Alder Vale's are, and the demons and the rift their own. The
  rules behind them are not placeholder.
- Task 5 (user decision M1): the Sparkling look (`old-game/styles.css`: a shimmering gold "Sparkling" on the encounter
  screen, a gold glow and twinkling motes over the sprite and a gold name in battle, a gold-edged card with ✦ in the
  classic view); Treasure Seeker's drawing (`MASTERWORK_ARTIFACTS["treasure-seeker"]` in `forge-art.js`: a gilded compass
  over coins); and the looks of Moneyshot, Lament, Noble Hero and the Hexbane Coat, which are their types' normal forged
  looks (the coat in silver-grey through its `color`). The port remakes them as proper art: Sparkling enemies that
  visibly shine, and each masterwork its own. The rules behind them are not placeholder.
- Task 7 (user decision L1): the five Exotics' looks are their types' normal forged looks (Exium drawn as Mythilium),
  but for Winged Hussar's crossguard, a simple pair of wings (`wingedGuard` in `old-game/scripts/forge-art.js`); the
  Limitless button and pop-up are the sheet's plain styles. The port remakes them as proper art: each Exotic its own,
  Winged Hussar's wings its defining feature. The Exotic colour itself (dark neon green and purple in a moving wave,
  `old-game/styles.css`, the task 7 block) and the sheet's look while Limitless is on are real, not placeholder; nor
  are the rules behind them.
- Task 8 (user decision W1: its 2D looks are placeholders for the port's 3D map): the continent's painting (the
  regions' own pictures side by side over their territories, meeting along wandering lines, `map-terrain.js` and
  `ContinentRules.cover`; the far-out overviews and backdrop and the coast line, `map-continent.js`), the fog over
  unentered regions, the empty regions' parchment marked "Uncharted" (W29), the boat and its drifting waves (W31), the
  caravan's wagon and route line, the horse carriage's drawing in the General Store (`CARRIAGE_ART` in `shop.js`) and its switch on the map, and the
  mountain's fog and cloud banks (W32), all in `old-game/scripts/map.js` and the files named. The port remakes them in
  its 3D map, with draft 6 (`8_Map_Rework/continent-draft-6.png`) as the reference for the continent's shape. The rules
  behind them (the land, the routes, the timings and amounts) are not placeholder.
- Task 9 (user decision P2): the two Apothecary banners (`apothecaryHall` for castle cities, `apothecaryShop` for towns,
  in `old-game/scripts/scene-banners.js` and its `PLACEHOLDER_ART`: stone and plaster rooms of potion shelves, hanging
  herbs, a bubbling cauldron and the apothecary at the counter), the brewing cauldron and its animation
  (`old-game/scripts/apothecary.js`, `styles.css`), and the icons of the 40 new potions, the 12 poisons and the 56
  ingredients (`potionArt` coloured by effect, a skull on poisons, and `ingredientArt`'s simple herb, mushroom, mineral,
  animal-part and essence shapes, in `old-game/scripts/forge-art.js`). The port remakes them as proper art: the two
  banners sharing an apothecary look, grand and modest; each potion, poison and ingredient its own. The rules behind
  them are not placeholder.
- Task 11 (user decision T5: simple placeholders): the three new race seals (`old-game/objects/race-half-beast.svg`,
  `race-goliath.svg`, `race-merfolk.svg`: a paw, mountains, waves, each marked PLACEHOLDER ART) and the 24 looks of
  Half-Beast, Merfolk and Goliath (`RACES` in `old-game/scripts/sprite-catalog.js`, under a PLACEHOLDER ART comment,
  with the two beast ear styles, `beast` and `beast-round`, in `sprite-humanoid.js`). The port remakes them as proper
  art: seals that sit with the six older ones, Half-Beasts that clearly show their beast (wolf, fox, bear, lynx),
  Merfolk recognisably sea-folk on land, and Goliaths visibly giant-blooded (the stone-grey skin and rune-like markings
  of T4, which the placeholders don't draw). The rules behind them are not placeholder.

**Found before the work:**
- Tasks 1 and 2 were checked against the engine work on 2026-11-03 (tasks 3 and 4 on 2026-11-06): none touches it (the
  engine's tests use their own copy of the stylesheet and saved captures; the game's code and tables come over at port
  time). After tasks 2–6 were merged (pull requests #8–#11), the full suite ran on the PC on `a32c5cb` (2026-11-06):
  2547 tests passing on the GPU and on lavapipe, the same as before the merges. Task 7 (pull request #12) changed no
  file the engine's tests read either, nor did tasks 8 and 9 (pull request #14; the only class file the tests read,
  `Black Knight.json`, is untouched). Task 8 did change `music.js`'s theme switch (the region crossfade), which the
  engine's soundtrack restates by hand; its tests keep the old switch until the port adds the crossfade
  (`../Audio.md`, "Theme API").
- The game-screen references predate the tasks (checked 2026-11-06): the 22 captured screens and the tests' copy of the
  stylesheet (`engine/Tests/Statventure.Tests/Data/Game/styles.css`) came in with the repo's first commit (`f45ca7a`,
  2026-10-30), the copy being that commit's `old-game/styles.css`. The old game's stylesheet differs from it since the
  small changes of 2026-11-03 (`7cdd47e`) and grew with tasks 2, 3, 5, 7, 8 and 9 (3915 lines in the copy, 4184 in the old game);
  its markup and scripts changed too. So the engine's game-screen tests check the old looks, as intended for now; the
  screens are to be captured again from the current old game. When (before the port starts, or screen by screen as the
  port reaches each) is a question to ask at port time (`Engine-Plan.md`, "Open questions for later milestones", "At
  port time").

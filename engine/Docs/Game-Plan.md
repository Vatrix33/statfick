# Stat Fick on Statventure: the game's milestone plan

The engine is finished to M12 (`Docs/Engine-Plan.md`, "Milestones"; 3720 tests on `main` at `29d110d`). This file is
the plan for the port of the game itself (`../old-game/`) onto it: the game's milestones GM1–GM18, what each one gives
the player, what it starts from in the old game and the engine, what it adds from the approved port tasks, the art it
must close with, how it is checked, what it needs and what needs it, its weight, and the questions to ask the user
before it starts. Written on 2026-11-12 from a survey of the whole repo (four survey files, in the repo since the same
day in `Docs/Game/Survey/`, its README first: `old-game-systems.md`, `engine-for-the-port.md`, `port-tasks-state.md`,
`content-inventory.md`; a snapshot of `29d110d`, not kept current); every pointer below was checked against the repo the
same day, and where a survey and the repo disagree the repo's figure is given (listed in "Cross-cutting", "Corrections
to the survey").

Paths are relative to `engine/` unless they start with `../old-game/` or `old-game/`. "The old game" is `../old-game/`
as it is on `main`; "the drafts" are the approved port-task files in `Docs/Port-Tasks/`.

## How to use this plan (read this first)

**What it inherits.** Everything the engine's milestones run by stays in force, and this file points to it rather than
copying it:
- The asking rule: `Docs/Engine-Plan.md`, "Ask the user about design choices — at once, never improvise." (the M11
  exception ended with M11) and the root `../CLAUDE.md`. Every design choice is asked with the question tool (2–4
  options, the recommended one first with its reason) as soon as it comes up and recorded with its date; never carried
  on with an assumed answer.
- Stopping at the end of each milestone and waiting for the user's go: `Docs/Engine-Plan.md`, "Stop at the end of each
  milestone".
- Keeping the docs current after every step: `Docs/Engine-Plan.md`, "Keep the notes current".
- The machines, git and pushing: `../CLAUDE.md`, "Where this is worked on" and "Git"; workers in worktrees with a
  merge worker (`Docs/Engine-Plan.md`, "Merging (user decision 2026-11-02)"); at most four workers, one full run or
  mutation run at a time under the shared lock (`Docs/Editor.md`, "Steps"; `Docs/Packaging.md`, "Steps").
- Headless work: no window on the user's screen (full runs inside `cage` on wlroots' headless backend; the engine's test
  windows hidden), audio from a session only at volume 0, the player's real folders never written
  (`Docs/Engine-Plan.md`, "How each step is finished", items 1 and 2; "Decision on test runs and the player's folders").
  The game program, once it exists (GM1), is run from scripts only with a scratch per-user folder, as the sandbox and
  the editor are (`XDG_DATA_HOME`, `--player-folder`).
- The method (references first, the rule ported, a test against the reference): `Docs/Engine-Plan.md`, "Method", and
  for 3D `Docs/Renderer-3D.md`, "Method".

**How a game milestone runs** (the order M9–M12 followed, `Docs/Engine-Plan.md`, "Starting M9", applied to the game):
1. `git pull`; read this plan's section for the milestone, the port-task files and decisions it names, and the
   design docs of the engine areas it uses.
2. Check the section's facts against the code (the old game and the engine) and note any that moved since 2026-11-12.
3. Ask its questions (GMn-1, GMn-2 …) at its start (GP-6), all at once with the question tool, four per call; record
   the answers with their dates in this file under "Decisions for GMn" (opened in the milestone's own section), with a
   pointer line in `Docs/Engine-Plan.md`'s index of decisions ("Open questions for later milestones"; GP-7).
4. Where the milestone's content is not approved yet (the enemy revamp, task 14's drafts, the three new regions, new
   music themes, the sound list), write the drafts and have them approved first, as task 14's DG2 and "At port time" lay
   down ("Where an answer leaves a draft to port time, that draft is written and approved then"): a sample first, then
   batches, each approved, as tasks 10, 12 and 13 were.
5. Write the milestone's design doc, `Docs/Game/<Aspect>.md` (decisions; what exists, checked against the code of both
   the old game and the engine; method; design; steps with **Owns:**, **Must not touch:** and **Finished:**; how it
   works; status), split the milestone into steps run in waves as `Docs/Packaging.md` did, and run each step as below.
6. At the end: tick it under "Milestones" here, update the notes, report what was done and what wasn't checked, and
   stop until the user's go.

**How each step is finished** (the engine's rule, `Docs/Engine-Plan.md`, "How each step is finished", with what the
game adds):
1. `Tools/Scripts/test.sh` passes in full on the GPU and on lavapipe, under the shared lock and inside the headless
   compositor, nothing held unless recorded as held. A number the user chose, or a tolerance, isn't the step's to
   change; a flaky test gets its cause found.
2. New tests can fail: every rule they cover broken one at a time (`Tools/Scripts/mutate.py`), every survivor accounted
   for, the count reported, and no mutation writing to the player's real folders.
3. **Rules against the old game.** Where a rule is the old game's (not changed by a rework), the reference is the old
   game itself:
   - the pure rules modules, which load in Node (`module.exports`): `CombatRules` (`combat-engine.js`), `CombatAI`,
     `ForgeRules`, `WorldRules`, `WorldRegionData`, `ContinentRules`, `ContinentShape`, `EncounterRules`,
     `GuildQuestRules`, `GuildInformationRules`, `ShopRules`, `LimitlessRules`; their results for every input the test
     names are generated once and committed (as `Reference/*-results.json` are), so the tests run where there is no
     Node. (The PC's shell has no `node` on its PATH, checked 2026-11-12; how the generator runs Node, Electron's own
     or an install, is the first step's to settle.)
   - the rules inside `app.js`, `combat.js` and the window-only `AlchemyRules`/`AlchemyData`: through the old game's
     test client in Chromium (`Tools/Scripts/chromium.sh old-game/test-client --script=<file> --call=<function>`), with
     randomness stubbed as the old checks stub `Math.random`, and the measured values brought out through the test
     preload's save hook (`Docs/Notes.md`, 2026-11-11/12, "getting data out of the old game's headless test client").
   - the old game's own checks (`old-game/test-client/tests/`, 49 `*-browser.js` files and the suites): every rule one
     of them checks has a port test with the same expectation, named after it.
   Where a rework changes a rule (tasks 10, 12, 13, 14, the enemy revamp, the three new regions), the approved draft and
   its decision ids are the reference, and the test says so.
4. **Screens against Chromium.** A screen the old game still has unchanged is compared with Chromium captures of the
   old game (`Tools/Scripts/make-game-references.sh`, `make-scaled-game-references.sh`, `make-layer-references.sh`;
   `GamePaintTests`' 24 levels / 3 %, the layout box for box, compositing layer for layer, at 1 and 1.65, 3D views and
   inline SVG art masked). A screen that is new or changed is shown to the user as captures for approval first, then
   tested against Chromium rendering the port's own markup, as the editor's screens are (PaintReference, 12 levels /
   0.2 %).
5. **3D against Blender.** Every model, stage, clip and cutscene against Blender 5.2's renders and evaluations
   (`Tools/Scripts/make-blender-references.sh`; `ReferenceLimits.M6`, `ReferenceLimits.DetailedStage` for detailed
   stages; clips within Animation.md's limits), after the user approved them through Blender renders: the first model of
   each rig family (Decisions for M7, "Model approval"), every boss model and cutscene (M10's bar, `Docs/Scene.md`, "The
   bar for the port's cutscenes"). **Every model is regenerable from its script** (GP-4): the step that makes or changes
   a model rebuilds it from its Blender script on the PC and checks the result equals the committed `.glb` and renders;
   the cloud, without Blender, tests against what is committed.
6. **Sound by ear and offline renders.** Effect patches as fixtures against Chromium's OfflineAudioContext (the engine's
   nodes render each patch's graph as Chromium does), their offline renders committed, and each sound approved by the
   user's ear in a real terminal (a session plays only at volume 0); new music themes rendered by the old composer in
   Chromium (`make-theme-references.sh`, `test-themes.sh`) and approved by ear.
7. Docs: this file's Status and the milestone's section, its design doc, `Docs/Engine-Plan.md`'s "Current task",
   `Docs/Notes.md`, and on the PC Claude's memory file `stat-fick-port.md`.
8. Commit and push as `../CLAUDE.md` says (a cloud session pushes its branch and opens a pull request).
9. At the end of a milestone: tick it, report, stop.

**Where each kind of work runs.** Blender (models, stages, clips, cutscenes, regenerating a model) only on the user's
PC; the old game's test client and Chromium captures on the PC or under Xvfb in the cloud (Electron 44.5.0's download
gotcha: `Docs/Notes.md`, 2026-11-11); everything else anywhere; the Windows PC builds and tests only
(`Docs/Engine-Plan.md`, "Windows check").

## Decisions for the plan

**The user's decisions (2026-11-12, asked through the manager with the question tool).**
- GP-1 **Reworks built in while porting:** each system's milestone ports the rules once and fills them with the
  approved content directly (task 10's 256 classes, task 12's 381 reworked + 168 new spells with statuses and
  reactions, task 13's classes inside 10's drafts, task 14's 12 dungeons built during the port); the old game stays
  the reference for everything the reworks don't change. (Not "faithful port first"; not "faithful rules, new
  content".) This answers the question left open since 2026-11-05 ("whether the approved port tasks 10, 12, 13 and 14
  are built while porting or after the faithful port", `Docs/Engine-Plan.md`, "Current task" and "At port time").
- GP-2 **Milestones cut by aspect, ordered for implementation:** one milestone per game aspect. (Not "by playable
  slice"; not "by layer".) As asked, the option read "Milestones cut by aspect, playable early: … ordered so a playable
  loop (a character, one region, combat, a town) exists after the first few"; the "playable early" part was the
  manager's wording, and the user dropped it with GP-5 (2026-11-12): the order serves implementation only.
- GP-3 **Each milestone includes its art:** a milestone closes only with its final 3D art for its aspect (no stand-in
  visuals shipped, no separate art milestones). (Not "art milestones after gameplay"; not "all art last".)
- GP-4 **Workers make the models by Blender scripts:** Blender Python that builds the models procedurally (rigs from
  parts, palette slots, equipment layers), as the old SVG sprites were built in code; every model reproducible from the
  repo, checked against Blender renders; the user reviews renders and may hand-model any piece. (Not "the user by
  hand"; not "mixed".) The engine's test models are already made this way (`Tools/BlenderReference/make_models.py`,
  "the script is their source", `Docs/Renderer-3D.md`, "Reference harness").
- GP-5 **No early playable loop; the order made for implementation, not the recommendation** (the user, 2026-11-12, the
  question tool, none of the three options taken): "I dont need an early playable loop. Create a Milestone order that
  makes it best to implement". The question had been: GP-2 asked for a loop after the first few milestones, but under
  GP-1 a region's enemies are the revamped ones, which need the reworked classes and spells first (C2, SP16), so the
  first order reached enemies on the map only after its GM10. (Not the first order as planned, the walkable world and a
  town early, the recommendation; not classes, spells and the bestiary ahead of the continent and the towns; not the
  old game's enemies, classes and spells as interim content.) The milestones were reordered the same day for
  implementation ("Why this grouping and this order").
- GP-6 **Each milestone's questions are asked at its start** (the user, 2026-11-12): GM1's are asked now, before it
  starts; each later milestone's when it begins, as M11's and M12's were (`Docs/Engine-Plan.md`, "Starting M9").
- GP-7 **Where the answers are recorded** (the user, 2026-11-12, confirming the plan's proposal): in this file, under
  "Decisions for GMn" in the milestone's own section (dated, who decided, "**…, not the recommendation:**" where the
  user chose otherwise, the options not taken as "(Not …; not ….)", Engine-Plan's style), with a pointer line in
  `Docs/Engine-Plan.md`'s index of decisions ("Open questions for later milestones").

**Earlier decisions that bind the plan** (each where it is recorded):
- **Old saves are never imported.** "Saves start fresh" (`Docs/Engine-Plan.md`, "User decisions (2026-10-16)",
  Data); "old saves aren't imported, decided 2026-10-16" (Decisions for M9, "Character export and import";
  `Docs/Assets.md`, Decisions, "The old game's saves aren't imported (2026-10-16)"); C12 "Saved characters are ignored"
  (Decisions for task 10). The old-save rules the tasks built in the old game (G33, R15, A11, L17, W15 and their kin)
  concern the old game only; DG33 applies R15's way to "the port's own saves".
- **C32, the three duties for the classes** (the user, 2026-11-11; "At port time"): (1) the classes are unchanged in
  the old game and during the port the classes in the drafts are added; (2) during porting every class is inspected for
  errors and inconsistencies (in the drafts and against the game's rules, before or while it is built); (3) after the
  port every class feature is tested so that it works as intended (every level of all 256 classes, passives and skills
  included). Task 13's classes come under them too (NC1, NC19).
- **DG1 and DG2:** task 14 is built during the port itself; its rules and main choices are answered (DG1–DG74), its
  content (dungeon enemies and stages, bosses, items, numbers) is drafted at port time and approved by the user then
  (the list: `Docs/Engine-Plan.md`, "Where task 14 stands (2026-11-12)").
- **W27:** the three new regions (the rocky highlands, the steppe, the west-coast delta) are made in the port: "their
  names, identity, terrain, enemies, level bands, towns and connections are made in the port" (Decisions for task 8;
  `Docs/Port-Tasks/8_Map_Rework/README.md`), draft 6 the reference for the map (W26).
- **The general enemy revamp** (the user, 2026-11-11; "At port time"; `Docs/Port-Tasks/README.md`): "all enemies are
  revamped during the port, because their classes have changed (task 10) and an upcoming task will change the spell
  system as well"; every enemy, with C2 (the enemies using the old classes reworked once the port has built task 10's
  classes) and SP16 (enemies' spells may carry augmentations, chosen per enemy in the revamp) part of it.
- **The asking rule** (above) and the order of the tasks (`Docs/Port-Tasks/README.md`, "The order": each builds only on
  earlier ones; "They are added to the game during the port, in that order", Decision on the user's tasks for the port).
- **The look** (User decisions 2026-10-16; Decisions for M6, M7, M10): characters, enemies and items are real-time 3D
  Blender models with cel shading, ink lines and full animation sets; battles keep the fixed orthographic side view on a
  3D stage; the map is 3D terrain seen from above; banners and cutscenes are remade in Blender; the UI is the engine's
  own; classes stay `.json`, everything else goes into packs; Linux and Windows. The user's requirement of 2026-11-05:
  **the boss and enemy models are as detailed and high-quality as the cutscene scenes** ("At port time"). The first
  model of each rig family is approved before the rest of that rig is made (Decisions for M7, "Model approval").
- **Sound effects** (the user, 2026-11-03, Decisions for M8): the finished port has proper sound effects; the old game
  has none; which ones is asked at port time (the first asked for: slotting a rune).
- **The data library** (M12-9, closing G3): the port's record kinds live in `Statventure.Game.Data`, which the game
  links and the editor loads from the game's folder.

**Chosen by the plan itself** (internal, doc and tooling matters the manager left to this plan; none changes the game):
- The milestones are `GM1`…`GM18`, the plan's decisions `GP-n`, a milestone's questions `GMn-k`. Checked free on
  2026-11-12 in `engine/Docs/` (with `Port-Tasks/`) and `old-game/Docs/`: neither `GM` nor `GP` followed by a number
  appears. The identifiers already in use there: `M` (the engine's milestones, `M10-2`, and task 5's M1–M26), `Q`
  (detail rounds, `Q1`…, `Q-A`…), `S` (questions during steps: `S3-1`, `S5-Q1`, `S6-A`, `S7-14`, `S9-D`), `P` (task 9's
  P1–P31 and the editor's P1–P38), `G` (task 2's G1–G34 and the editor's G1–G6), `SP`, `DG`, `C`, `W`, `NC`, `L`, `A`,
  `T`, `R`, `N`.
- Each milestone's design doc is `Docs/Game/<Aspect>.md`; its worker branches are `gm<n>-design`, `gm<n>-step<k>`.
- The plan's reading of GP-3: a milestone closes with the final art of its own aspect; what belongs to a later
  milestone's aspect (an enemy's model during combat core, a summoned creature during the spells) shows as the engine's
  stand-in test figure in development builds until that milestone, and the release (GM18) checks that no stand-in is
  left. Each section names what it shows as a stand-in meanwhile.

## Milestones

Each milestone is finished only when its tests pass and its art is final for its aspect (GP-3); the next one starts on
the user's go, its questions asked first (GP-6).

- [ ] **GM1 Foundation: the game program, its data library, saves and settings:**
  - The game's own program replaces the sandbox (`stat-fick`, `Stat Fick.exe`); the real `Statventure.Game.Data` library
    replaces the stand-in, with the save and settings records.
    - Save slots, autosave, character files, the start screen, the in-game menu (Save/Load Character, Open game folder,
    Quit, zoom, fullscreen, About and credits), the settings screen with Dev Mode's switch and the mod list; the
    interface sounds.
  - The game's stylesheet in `ui.pak`; the game-screen references recaptured (GM1-1); the symbols outside the fonts.
- [ ] **GM2 The hero: the character sheet, races and looks:**
  - The sheet (attributes, levelling and EXP, HP, Mana, Armor, Evasion, the purse), the actor record every combatant
    shares, the race picker and the appearance step, the 28 races' records and the nine playable races' passives.
  - The humanoid rig family with its clip set, the 72 playable looks, the three new race seals.
- [ ] **GM3 Arms and armour: the forge, artifacts and the inventory:**
  - Every forged weapon, armour piece, caster and rune; the 37 masterworks and Exotics; natural weapons; the 85
    artifacts and their level ramp; the 5 key items; the inventory and equipment screens.
  - The item models (parts per maker and material, garments on the bodies, unique designs) and the item pictures.
- [ ] **GM4 The continent and travel:**
  - The 3D map of the continent (draft 6), travel along routes, the real-time clock, day and night, weather, fog over
    unentered regions, the mountain's fog and clouds, the boat, caravans and the carriage's rules.
  - The region music with the 4-second crossfade; settlements and sites as places on the map.
- [ ] **GM5 Combat core: the battle, its rules and its stage:**
  - The turn meter, 5 AP, every action, damage, hit and crit, reactions, the old timed effects and task 12's 12
    statuses and 12 reactions, items in battle, loot, EXP and defeat, the ordinary AI, a battle saved and resumed.
  - The battle screen (the 3D stage view and the classic view), formations, the humanoid clips at work, the battle's
    effects, status looks, the battle settings, Dev Mode's combat setup.
- [ ] **GM6 Towns: the shops, the Apothecary and resting:**
  - Blacksmith, Tailor, General Store and Apothecary in every settlement, stock windows, selling, the reforge anvil,
    brewing and recipes, resting and camping; the 71 consumables, 56 ingredients and 62 recipes.
  - Seven interiors (Blacksmith and General Store, castle and town; the Tailor; the Apothecary, castle and town), the
    anvil, the cauldron, the potion, poison, ingredient, trap and carriage models.
- [ ] **GM7 Spells and magic: the spell book, Spell Augmentation and the Magic Towers:**
  - Task 12's 549 learnable spells and the 7 enemy-only ones, casting, summons, the spell book; Spell Augmentation and
    its menu; the Magic Tower and the Institute of Magic with their interiors.
  - The spells' effects per element, the 12 Tier 10 animations reworked (SP47), the Augmentation menu's 12 circles.
- [ ] **GM8 Classes: the 256:**
  - Task 10's drafts (with tasks 11–14's classes at their end): every class's requirements, levels, passives and
    skills; the new counts, requirement kinds and the Moonfang; the class files in `classes/`.
  - The Classes, Skills, Passives and class spell choice windows; the red rarity; C32's duties (1) and (2).
- [ ] **GM9 The bestiary: every enemy revamped and modelled:**
  - The general enemy revamp drafted, approved and built for the 146 non-boss enemy files and the two new summoned
    creatures; Empowered and Sparkling enemies.
  - Every other regular enemy kind drafted with it: task 14's dungeon-only types (DG9) and, if GM4 designed the three
    new regions (GM4-4), their new types.
  - The creature rig families and every enemy's model, the Sparkling shine, task 6's Rift enemies remade.
- [ ] **GM10 Regions in battle: encounters, foraging and battle stages:**
  - The 89 zone tables and 567 groups, travel encounters and the encounter prompt, foraging, the regional battle themes.
  - The 33 battle biomes as 3D stages with time of day and weather.
- [ ] **GM11 Companions and the Adventurers Guild:**
  - Up to three companions with full sheets, recruits and imports; the 15 guild halls, the seven quest types, the
    board, counter, tavern, hooded man (NC7), artifacts and trophies, the Quests card.
  - The two guild interiors as the detailed, clickable rooms task 2 asks for.
- [ ] **GM12 The three new regions:**
  - W27's regions drafted and approved where GM4 left them: names, identity, level bands, towns, roads and connections.
  - Built whole: terrain (unless GM4 made it), encounters, forage, towns and guild services, music, battle stages; their
    enemy types from GM9 (or drafted here by the revamp's rules if GM4-4 left their design here).
- [ ] **GM13 The Church of Talos and Hearth Home, and the boss machinery:**
  - The boss machinery (phases, badges, boss music, intros held by the AI, the game's cutscene path, the boss
    simulator), built on the two site bosses: Tankina and Ornn with their mechanics, AI, retinues and four cutscenes.
  - Respecs, the Storm Reliquary (with Augmentation), Hearth Home's forge; the storm chapel and Hearth Home's own room.
- [ ] **GM14 Bosses and Great Hunts:**
  - The eight Great Hunt bosses on GM13's machinery, with their seven mechanics and ten AI profiles, revamped and
    balanced by simulation; offered on the guild's boards, with trophies and rematches.
  - Their models at the cutscene bar, their phase looks and retinues (the core rig family's first model), their 12
    cutscenes (8 intros, 4 phase scenes).
- [ ] **GM15 Limitless and the endgame:**
  - Task 7's Limitless: Levels to 200, enemy scaling, keep-aside, doubled masterwork drops, Exotic drops, the Level-200
    reward.
  - The bosses' Level-100 class sets (drafted with them in GM13 and GM14) applied; the Limitless button and notice.
- [ ] **GM16 Dungeons:**
  - Task 14 whole, from its port-time drafts: the twelve dungeons with their 3D overviews, stages and their pools of
    GM9's enemies, warning, penalties, Delve and Nocturne Keep's quest.
  - Eleven new bosses and Saffron and Ool moved in, with their intros and themes; the dungeon themes.
  - The three masterworks, the Exotic, the eleven consumables and the smithing materials.
- [ ] **GM17 Balance and the class tests:**
  - C32's duty (3): every level, passive and skill of the 256 classes exercised in play.
  - The balance pass by simulation (bosses, zones, races, classes, items); the user's playtest.
- [ ] **GM18 The release:**
  - The real game in both downloads (the data library with every kind, `packs/`, `classes/`), icons, the editor knowing
    the game's kinds.
  - The end-to-end check extended to play, the Windows check, the first release on the user's word.

### Why this grouping and this order

**The grouping** follows GP-2's aspects and the old game's own dependency graph (`Docs/Game/Survey/old-game-systems.md`,
§4: pure rules → catalogue data → the character record → the sheet → enemies → combat → AI and bosses → art →
presentation → loot → world → interiors → shops → companions → guild → sites → Limitless → music). Aspects too large
for one team were split where the code splits them: the world into the continent and travel (GM4: `world-engine.js`,
the continent, the map view) and the regions in battle (GM10: `encounter-engine.js`, `battle-scenery.js`, foraging);
combat into its core (GM5) and the bosses (GM13, GM14, GM16: `combat.js` 2957–3916 and the AI profiles 5850–6007); the
character into the hero (GM2) and the classes (GM8). Enemies are their own aspect (GM9) because the revamp is one body
of content and the creature rigs are one body of art. The bosses are grouped by where the player meets them: the two
site bosses with their sites (GM13), the eight that stay Great Hunts (GM14), Saffron and Ool with the dungeons they move
to (GM16, DG33).
The Institute of Magic sits with the Magic Towers in GM7, because in the code it is the tower window's second venue
(`magic-tower.js`, venues `tower` and `institute`). Companions go with the guild (GM11) because the tavern is where they
are hired (the only other way in is a character file) and recruits are one of the guild's own rules (`GuildQuestRules`'
`RECRUITS`). Music needs no milestone of its own: the soundtrack is already the engine's (M8), and each milestone wires
the themes its aspect plays (GM4 regions, GM5 battle, GM13 and GM14 bosses) or makes the new ones it needs (GM12,
GM16). Sound effects are made by each milestone for its own aspect (GM1-6).

**The order is made for implementation** (GP-5): each milestone needs only what the ones before it built; nothing is
built twice, or built on a stand-in rule a later milestone has to replace; the reworked content comes before what
consumes it (GP-1: spells and classes before the enemies that use them); and each shared art family comes before its
users (the humanoid rig before every humanoid model, the creature families before the bosses on them, the room kit
before every interior). What each milestone needs from the ones before it:
- **GM1** needs only the engine (M9–M12): every later kind lives in its data library, every later state in its save.
- **GM2** needs GM1. It makes the actor record every later combatant is (the old game evaluates every actor through the
  sheet's own rules, `withActor`, `combat.js` line 143) and the humanoid rig, the shared family of every humanoid model
  after it: companions, recruits, the people in the rooms, humanoid enemies and bosses.
- **GM3** needs GM2: garments are skinned per body type (Decisions for M7, "Gear in detail") and weapons hang on GM2's
  sockets, so the gear follows the bodies while the humanoid family is open.
- **GM4** needs GM1 and GM2 (the clock's regeneration acts on actors) and nothing from GM3 or the battle. It comes
  before combat because a defeat carries the party to the nearest settlement, so GM5 builds its defeat on real places,
  and before the towns because a shop belongs to a settlement (castle city or town, region materials, stock windows on
  the clock). Its feature models (trees, rocks, ruins) are there for the battle stages (GM10) to reuse.
- **GM5** needs GM2 (actors, clips), GM3 (weapons, armour, artifacts, `armCharacter`) and GM4 (settlements for a
  defeat). It ports the effect interpreter for every effect type the old battle reads, so GM7 and GM8 add only the
  types their drafts mark "(new)".
- **GM6** needs GM3 (stock, reforging), GM4 (settlements, the clock) and GM5 (the battle rules its consumables fill).
- **GM7** needs GM3 (casters, runes), GM5 (casting, the statuses) and GM6 (the shop window the tower and the Institute
  are venues of).
- **GM8** needs GM7 (class spell choices; Augmentation for the Spellweaver, rituals for the Ritual Caster), GM5 (skills
  and passives in battle), and GM4, GM6 and GM7 for the counts their requirements read (caravan trips, regions entered,
  items reforged, potions brewed, runes fitted), so only the counts of later systems (Sparkling, the guild, Great
  Hunts, Limitless, dungeons) wait for their milestones.
- **GM9** needs GM7 and GM8 (the revamp gives enemies the reworked spells and classes, C2, SP16), GM3 (their gear) and
  GM2 (the humanoid rig every humanoid enemy is built on). It drafts every regular enemy kind at once, the existing
  ones, task 14's dungeon-only types (DG9: "drafted at port time with the general enemy revamp") and the new regions'
  types when GM4 designed them, so each creature family is opened once.
- **GM10** needs GM9 (the tables name enemies), GM4 (zones, travel) and GM6 (foraging finds ingredients).
- **GM11** needs GM8 (companions' classes, the hooded man's class information), GM10 (the Hunt, Cull, Search and
  Escort fights) and GM6 (Collect). It comes before every boss because boss balance is simulated with parties (the old
  simulators play parties, `../old-game/Docs/Notes.md`).
- **GM12** needs GM4–GM11, everything a region holds (land, towns, enemies, encounters, forage, guild services); after
  it the world is whole, before the bosses and the dungeons placed in it (the harpy dungeon in the rocky highlands,
  DG65).
- **GM13** builds the boss machinery on the two site bosses, each with one mechanic and one phase scene and neither
  needing the guild; it needs GM6 (the shop window, reforging), GM7 (Augmentation at the Reliquary), GM8 (respecs), GM9
  (Gorthok and the Vulcanic Ram are quadrupeds, the Apostles humanoid) and GM11 (parties for the simulations).
- **GM14** needs GM13 (the machinery, the simulator, the cutscene path) and GM11 (the boards that offer Great Hunts,
  the trophies).
- **GM15** needs GM14 (Anatemis's first defeat unlocks it), the bosses' Level-100 class sets drafted with them in GM13
  and GM14, and the systems it scales (GM2, GM3, GM5, GM8, GM11).
- **GM16** needs everything before it: task 14 builds on tasks 2, 3, 5, 7, 8, 9, 10 and 13 and on the revamp; its
  enemies are GM9's and scale through Limitless (GM15), its Exotic drops only under Limitless (DG36), its harpy dungeon
  lies in GM12's highlands, its bosses run on GM13's machinery.
- **GM17** is C32's "after the port"; **GM18** ships what is done.

What this order changed from the first one (written the same day, before GP-5, for a loop after the first few
milestones): the continent moved ahead of combat (GM4, GM5); the three new regions moved up to follow the guild
(GM12); the Church of Talos and Hearth Home moved ahead of the Great Hunts and took the boss machinery with them (GM13,
GM14), with the two questions that apply to every boss (GM13-1, GM13-2); and the dungeon-only enemy types moved into
the bestiary, as DG9 has them drafted. The count stays 18: no merge or split was clearly better.

## GM1 Foundation: the game program, its data library, saves and settings

**What the player gets.** Starting `stat-fick` / `Stat Fick.exe` shows the BRUTAL splash (2 s, skippable after 1 s, M12)
and then a start screen in the game's look (GM1-2): Continue (the last character; greyed while there is none, Q16b), New
character (its name asked first, Q2), Load (the slots, with "Load Character…" for a file, Q24), Settings, Quit and About
(Q16), then the character's page: in
GM1 the top bar over an empty page (Q5). A character is saved to its named slot after every change, as the old game's
`saveCharacter` is called after every change (92 call sites), and on quit; Save Character and Load Character write and
read the save's readable JSON in the Documents folder through an in-game file picker, a loaded file becoming a new slot
(Q9). The old Electron menu (File › Save Character Ctrl+S, Load Character Ctrl+O, Open game folder, Quit; View › reload,
zoom, fullscreen; Help › About) becomes an in-game menu (Decisions for M5: no native menu bar, no OS dialogs), opened by a
Menu button in the top bar and by Escape (Q7), without Reload (Q18) and with Settings and "Start screen" added (Q8),
About showing the credits with BRUTAL's copyright (M12 Q11) in the game's look (Q20). The Settings screen holds the
volumes (music, and the master and effects volumes M8 added; effects at 60 % on a first start, Q22), zoom, fullscreen,
the theme (both kept, dark by default, GM1-4; set only there, Q6), Dev Mode's switch (off by default and turned on there
when wanted, GM1-12; kept in `settings.json`, the same for every character, as the theme is, GM1-13) and the mod list
(each mod named from its `mod/info`, every mod checked at start, a broken one named and left out, GM1-10; an on/off
switch each, Q3; changes take effect at the next start, Q18); the battle settings join it in GM5. Buttons and windows
opening and closing sound softly (GM1-11, the eight sounds of Q13). No music plays in GM1 (Q15). There is nothing to play
yet: the sheet comes with GM2. The detail round: "Decisions for GM1", "GM1's detail round"; the design:
`Docs/Game/Foundation.md`.

**What the old game has.**
- The desktop shell (`desktop/`, Electron 44.5.0): `main.cjs` (94 lines: the `statfick://game/` protocol, the check that
  `index.html`, `styles.css`, `scripts/app.js`, `objects/artifacts.json`, `classes` and `enemies` exist, one window
  1440×950 with a minimum of 820×600 and a single instance, the menu, saving on close), `assets.cjs` (85),
  `preload.cjs` (6: `window.statFickDesktop.saveCharacter(json)` and `loadCharacter()`, native dialogs, a 10 MB limit).
- The save: `saveCharacter()` (`app.js` 1839–1850) writes the whole state as one JSON object to
  `localStorage["emberfall-character-stats"]` (its fields: "Cross-cutting", "The save and what goes in it"); the backup
  file `stat-fick-character.json` is `{ format: "emberfall-stat-block", version: 22, exportedAt, character }`
  (`backupPayload`, `app.js` 1857). Loading is migration (`normalizeCharacter`, `app.js` 1399–1564;
  `WorldRules.normalize`, `world-engine.js` 64–351), which the port does not carry ("What the port never does").
- Settings: `settings.js` (41 lines; `{ battlePace: "fast", combatAnimations: true, battleUi: "stage", battleSpeed:
  "normal" }` in `localStorage["stat-fick-game-settings"]`), the music volume in `stat-fick-sound-volume` (`music.js`).
  The theme and Dev Mode are saved with the character: `editingEnabled: true` and `darkMode: false` for a new character
  (`app.js` 1354–1355); recruits come with Dev Mode off.
- Dev Mode unlocks: editing the attributes, permanent bonuses, Armor and Evasion bonuses and the purse; the EXP form;
  "Restore to full"; adding spells and items from the catalogues (screens 6 and 8 in
  `Docs/Game/Survey/old-game-systems.md`, §3); the Combat test screen (16); levelling without EXP; choosing another
  race once one is chosen and the enemy-only races; the appearance step skipped; companions taught any spell; companion
  dev tools. (Corrected 2026-11-13, `Docs/Game/Foundation.md`: Force encounter isn't Dev Mode's, `map.js` 582–584.)
- The markup: `index.html` (1004 lines, 24 `.modal-layer` dialogs; the sheet's top bar with ✦, the name, the world
  clock, the autosave dot, the theme toggle, Save/Load/New Character and "Return to combat" in lines 65–86, the sheet
  page `main.shell` 64–300, the ⚙ Settings button outside it at the window's bottom right, 23–25);
  `styles.css` 4184 lines (245 `@keyframes`).

**What the engine provides.**
- `Core/Application.cs`: `ApplicationOptions` (`GameFolder`, `DevelopmentFolder`, `LogName`, `Splash`, `GamePacksOnly`),
  `Application.Data` (the virtual file system: the area packs in `packs/`, then `classes/`, then the player's `mods/`,
  then the development folder), `Application.Storage`, `Application.Audio`, `ReloadData()`, `ConfirmClose()`;
  `Core/Log.cs`, `LogFiles.cs`, `CrashWindow.cs`, `ProgramInfo.cs`; `UI/Splash.cs` and `UI/Credits.cs`, which "the
  port's start sequence and credits screen take over" (M12 Q11).
- `Storage/`: `SaveSlots.cs` (named slots, `saves/<name>.dat` and `.dat.bak`, the 48-byte `SVSV` header, written by
  `BackgroundWriter.cs`, a damaged slot kept aside and reported; `Docs/Assets.md` Q15–Q17, Q38–Q43), `Settings.cs`
  (`settings.json` with a version, unknown keys kept, `settings.broken.json`), `CharacterFile.cs`, `DocumentsFolder.cs`,
  `FolderListing.cs`, `FileBrowser.cs` (the model for the in-game picker, M11-9), `GameStorage.cs`
  (`OpenInFileManager`).
- `Records/` (`[Record]`, `[Field]`, `[Subtype]`, `RecordSerializer`, frozen past versions with `Upgrade()`, committed
  layout signatures and fixtures) and the marks the editor reads (`[Kind]`, `[Help]`, `[Unit]`, `[Limits]`,
  `[Choices]`, `[Colour]`, `[Bulk]`, `[References]`, `[Check]`; the game's loaders run the same checks, G4 and G5);
  `Packs/ModInfo.cs`.
- The UI (M4, M5): every property of the old stylesheet, styleable tooltips, select lists and validation bubbles
  (Decisions for M5), page zoom (`UiHost.PageZoom`, `SetZoom`), the event loop and Web Animations that let the port
  mirror the old JavaScript (`Docs/Scripting.md`), `visibilitychange` and `pagehide` (the game side is the port's).
- What GM1 replaces: `Samples/Statventure.Sandbox` (published as the game: `Tools/Packaging/Publish.cs` lines 157–158),
  the stand-in data library `Samples/Statventure.Game.Data` (one kind, `StandInItem.cs`, `stand-in.item`), and
  `GameLibrary.RepoProject` (`Tools/Editor/Game/GameLibrary.cs` line 40, "one constant the port changes").

**What is new.**
- The game's program and the real data library (its name, its being one file beside the executable and its version
  matching the editor's are fixed by M12-9, Q18 and `Docs/Packaging.md`, step 6; where the two projects sit in the repo
  is the design doc's to settle, and asked if it changes what ships; settled: `Game/Statventure.Game/` and
  `Game/Statventure.Game.Data/`, GM1-P1 in `Docs/Game/Foundation.md`).
- The save record (version 1 holds what GM1 needs, the character's name; GM2's part comes as version 2, and each later
  milestone adds its part as a new version, M9's rule); character files as the save's JSON form (Decisions for M9);
  `settings.json`'s keys (not a record: M9's `Settings`). One record kind in GM1, `save.game`.
- The screens the old game hasn't: the start screen (GM1-2), the slot list with the player's own names (Q15 of M9; the
  name kept in the save and the file named from it, Q1 of GM1's detail round), the damaged-save question ("use the backup,
  start new", Q17 of M9, with "Back"), the file picker (Q19 of M9), the in-game menu, About with the credits, the mod list,
  the confirm and prompt dialogs in place of the old `window.confirm` and `window.prompt`: built from the old game's panels,
  buttons and dialogs and shown as captures before their tests (GM1-8). The top bar changes too: the theme toggle and
  Save/Load/New Character leave it, a Menu button joins it (Q6, Q7).
- Dev Mode moved into the settings screen, the sheet's ✦ button disabled (GM1-3): it stays as the brand mark, always lit,
  no tooltip (Q11); off by default (GM1-12) and kept in `settings.json`, the same for every character (GM1-13), so the
  save doesn't hold it.
- The mod list and the check of every mod at start (GM1-10, which answers M11's G2): a broken mod is named and left
  out instead of stopping the game when its part is used.
- The port's own stylesheet in `ui.pak` ("What M9 leaves for the port"), started from the old game's current
  `styles.css`; the game-screen references recaptured once at GM1's start, the tests' stylesheet copy brought up to
  the old game's (GM1-1). Engine work comes first: today's `styles.css` doesn't compile in the engine (`@property`,
  `-webkit-text-fill-color`; `background-clip: text` ignored), and the capture script needs changes for today's old game
  (`Shop.open` needs a settlement). The old game is frozen until the recapture is on `main` (the user, 2026-11-13).
- The symbols the fonts lack (⚙ ✦ ⚔ ♟ …; the tests emulate Chromium with "a table of the fonts Chromium picked for the
  game's 20 symbols", `Docs/Engine-Plan.md`, Progress, M4) become vector icons drawn by the engine in the game's ink
  (GM1-7). None exists yet: ♜ ♡ ◈ ▣ ◌ ◫ ◆ were decided as icons on 2026-10-16 (`Docs/Notes.md`, "User decisions") but never
  built. 28 icons: the 29 characters neither Lato nor STIX has and ◌, less ☾ and ＋, whose only uses left the top bar
  (Q6); ↓ is in Lato. (Corrected 2026-11-13, `Docs/Game/Foundation.md`.)
- The randomness service every rule draws from, fresh draws on every load as the old game's (GM1-5; "Cross-cutting",
  "Randomness and seeding").
- The interface sounds (GM1-11) and the game-event sounds of GM1's own screens (GM1-6), as effect patches approved by
  ear. Everything stays in English (GM1-9).

**The art.** No 3D. The new screens in the Gothic ink theme, the symbols as vector icons, the theme's two modes
(GM1-4). No Blender script family starts here. Its sounds: the interface's (GM1-11).

**How it is checked.** The save and settings records by round trips, damaged files and committed old-version fixtures,
as M9's are. The new screens, and the settings screen and the top bar, which both change (Q6, Q11; corrected 2026-11-13:
they are no longer compared with `game-settings` and `game-sheet`), through the user's approval of captures, then
Chromium renders of the port's markup. The program end to end (`Tools/Scripts/end-to-end.sh`, which relies on the
stand-in's `--capture`, `--about`, `--crash`, `--crash-thread`, its `[Data] mod …` log lines and its "parchment" page: the
game offers the same or the check changes with it; `Docs/Packaging.md`, "How it works", step 8). Nothing here can be
checked against the old game's behaviour: its save and dialogs are browser- and Electron-specific; its texts, keys and
dialog behaviour are ported from its code.

**Depends on / needed by.** Depends on M9–M12. Needed by every later milestone: each adds its kinds to the data library
and its part to the save.

**Weight: M.** About 1,100 old lines (the shell 185, `settings.js` 41, the save, backup and apply code of `app.js` about
900); six new screens (the mod list among them); no art. Its risk is the save's shape, which every later milestone
extends.

**Decisions for GM1 (the user, 2026-11-12, the question tool, asked by the manager before the milestone; GP-6):**
every answer was the recommended option except Dev Mode (GM1-3, the user's own answer), the mod list (GM1-10) and the
interface sounds (GM1-11). GM1-12 and GM1-13, which GM1-3's answer left open, were asked after the eleven the same day
and answered as recommended.
- GM1-1 **The game-screen references:** captured again once at GM1's start, all 11 states at 1 and 1.65 from today's
  old game, the tests' stylesheet copy brought up to it. (Not screen by screen as each milestone reaches its screen.)
- GM1-2 **After the splash:** a start screen in the game's look with Continue (the last character), New character, Load
  (the slots) and Settings, then the sheet. (Not straight onto the last character's sheet with the slots behind its
  Load button; not a title screen with a 3D scene.)
- GM1-3 **Dev Mode, the user's own answer, none of the options:** "Toggleable from the settings, old button disabled".
  Dev Mode is switched on and off in the settings screen; the old ✦ button beside the game's name on the sheet is
  disabled. Whether the disabled button stays (present but inert) or goes is left to the step that builds the top bar,
  and asked if it would show the player something different. Its default for a new character and where its state is
  kept were not stated; asked afterwards as GM1-12 and GM1-13. (Not the ✦ switch kept and off for a new character, the
  recommendation; not kept and on, as the old game; not development builds only; not moved into the editor.)
- GM1-4 **The theme:** a game setting in `settings.json`, both themes kept, dark by default. (Not per character and
  light by default, as the old game; not dark only.)
- GM1-5 **Randomness and reloading:** as the old game, every load draws fresh randomness; the random state is not saved.
  (Not the random state saved, so loading a save repeats the same outcomes.)
- GM1-6 **Sound effects:** each milestone makes its own aspect's sounds, their list asked with its other questions and
  approved by ear. The sound questions of the later milestones are therefore asked with them. (Not one list for the
  whole game decided now; not one sound milestone before the release.)
- GM1-7 **Symbols outside Lato and STIX:** vector icons drawn by the engine in the game's ink, as ♜ ♡ ◈ ▣ ◌ ◫ ◆ are.
  (Not a symbol font shipped in `ui.pak`; not the system's fonts.)
- GM1-8 **Screens the old game doesn't have:** built from the old game's panels, buttons and dialogs in its Gothic ink
  theme, each shown to the user as captures before its tests. (Not described or sketched with the user first.)
- GM1-9 **Languages:** English only, the text in the screens and the data as the old game. (Not a string table.)
- GM1-10 **Mods, not the recommendation:** a mod list in the settings, each mod named from its `mod/info`; every mod
  checked at start, a broken one named and left out. This answers G2, kept from M11 for the port ("to ask where mods and
  the game's mod list are asked"; `Docs/Editor.md`, "The game's questions"). (Not as today, no list and an error naming
  the mod and the part only when that part is used, the recommendation then and now; not as today plus a warning in the
  log at start.)
- GM1-11 **Interface sounds, not the recommendation:** soft sounds for buttons and for windows opening and closing,
  besides the game-event sounds. (Not game events only, the recommendation.)
- GM1-12 **Dev Mode's default** (the user, 2026-11-12, the question tool, asked by the manager after the eleven; GM1-3
  hadn't stated it): off by default for a new character; it is turned on in the settings when wanted. (Not on by
  default, as the old game for a new character.)
- GM1-13 **Where Dev Mode's state is kept** (the user, 2026-11-12, asked with GM1-12; "toggleable from the settings"
  hadn't said): in `settings.json`, the same for every character, switched in the settings screen like the theme
  (GM1-4). (Not with each character, as the old game did, the settings screen showing the current character's.)

**GM1's detail round (the user, 2026-11-13, the question tool, asked by the manager in six rounds of four after the
design doc's first version; in full with each question's facts and options in `Docs/Game/Foundation.md`, "The detail
round" and "The questions as asked"; Q24, raised by the answers, asked after the six rounds the same day):** every answer
the recommended option except Q6, Q11, Q15, Q16 (and Q16b), Q18, Q22 and Q23. Nothing of GM1 is open.
- Q1 A character's name: any name up to 80 characters, kept in the save, the slot's file named from it.
- Q2 The name asked before the sheet, "Character <n>" filled in.
- Q3 The mod list: an on/off switch per mod, kept in `settings.json`; the order the files' name order.
- Q4 A broken mod: what the editor refuses, checked at start alone over the game's packs, the whole mod left out; the
  start time measured, the user asked again if noticeable.
- Q5 In a character in GM1: the top bar over an empty parchment page, the ⚙ button.
- **Q6, not the recommendation: "Name and status only".** The theme toggle and Save, Load and New Character leave the top
  bar (the theme in Settings, Save and Load Character in the menu, New character on the start screen); the top bar holds
  "Return to combat", the ✦, "Stat Fick", the clock, the autosave status and the Menu button, a changed screen. (Not all
  kept and working; not the theme toggle alone dropped.)
- Q7 The menu: a Menu button at the top bar's right end, and Escape when no window is open.
- Q8 The menu adds Settings and "Start screen".
- Q9 Load Character makes a new slot.
- Q10 A line on the start screen names the mods left out.
- **Q11, not the recommendation: "Inert, always lit".** The ✦ is the brand mark only: always lit, not clickable, no
  tooltip, nothing to do with Dev Mode. (Not inert and showing Dev Mode; not removed.)
- Q12 The icons in the text's colour, inked shapes, approved as a sheet of captures.
- Q13 The eight interface sounds. Q14 Paper and ink.
- **Q15, not the recommendation: "No music until GM4".** Nothing plays in GM1; the music volume stays a setting, applied
  from GM4. (Not `alder-vale` throughout; not a silent start screen with `alder-vale` in a character.)
- **Q16, not the recommendation: "Add Quit and About".** The start screen: Continue, New character, Load, Settings, Quit,
  About. **Q16b (the manager's follow-up), not the recommendation:** Continue shown greyed while there is nothing to
  continue, never left out. (Not the four plus Quit with Continue left out; not the four with Continue greyed.)
- Q17 The slot list: Open and Delete.
- **Q18, not the recommendation: "No Reload".** Data and mods read at start only, no Reload item or Ctrl+R; a mod
  change takes effect at the next start, which the mod list says. (Not Reload with Dev Mode on; not Reload for everyone.)
- Q19 The window as the player left it. Q20 About as a parchment dialog with the engine's credits text. Q21 The
  damaged-save question: backup, new, or back.
- **Q22, not the recommendation: "Effects lower".** Effects 60 %, master and music 100 % on a first start. (Not all at
  100 %.)
- **Q23, not the recommendation: the general message** for an old game's character file, as for any wrong file. (Not a
  message of its own.)
- Also the user (2026-11-13, in the chat): **the old game is frozen** for the recapture, "Old game wont be touched until
  you give the ok, except if you need something"; the manager gives the ok once the recapture is on `main`.
- Q24 (raised by Q6 and Q16), Load Character from the start screen: "Load Character…" in the slot list, loading a
  character file into a new slot; the menu's Load Character and Ctrl+O stay as well. (Not a seventh start-screen button;
  not Escape and Ctrl+O only.)

## GM2 The hero: the character sheet, races and looks

**What the player gets.** The character sheet, the game's hub: the eight attributes (Strength, Vitality, Mind,
Dexterity, Intelligence, Wisdom, Speed, Luck; 10 each at the start) raised with 5 free points a level and +1 to each a
level; the level-up button once EXP reaches the target; HP, Mana, Armor and Evasion with their formulas shown; the purse
(silver, gold, platinum: 100 silver a gold, 10,000 silver a platinum; 5 gold for a new hero); the five equipment slots
(filled from GM3); the respec of free points; the Field tools rail and its drawer under 840 px. A new hero picks one of
nine races by its seal (Human, Elf, Dark Elf, Dwarf, Hobbit, Lizardman, Half-Beast, Goliath, Merfolk), then a gender and
one of four looks, each shown as a live 3D figure.

**What the old game has.**
- `app.js` lines 1–427 (constants, DOM references; the 130 class paths go with GM8), 428–1326 (the derived values: the
  formulas at 428–447, the race effect accumulator, equipment defence; the class effects at 562–1057 go with GM8, the
  artifacts with GM3), 1327–2217 (the record, saving, applying, level-up, reset, the sheet's rendering), 2926–3221
  (`normalizeRaceDefinition`, `renderRaces`, `raceDisallowsItem`, the appearance step, `raceAccent`); `interface.js`
  (66, the drawer).
- Formulas: max HP ⌊(1.5 VIT + STR/2 + MND/2) × 1.25⌋; max Mana MND + INT/2 + WIS/2; Evasion (10 + 1.5 DEX + 0.5 LCK +
  0.5 WIS)/2; base Armor 5; EXP to level = an enemy of that level's EXP × `sameLevelVictories` (3–6 to Level 12, rising
  to 24 at 99; 1,265 same-level victories to Level 100); enemy EXP ⌈(level + 1) × 100 / share⌉; cap 100 (200 with
  Limitless, GM15). Numbers are JavaScript doubles with `Math.floor` at set points.
- `objects/races.json`: 28 races (9 playable, 19 enemy-only), each `{ id, name, playable, icon, statModifiers, passive
  }` with 22 effect types (`calculateRaceEffectsUncached`, `app.js` 1101); nine seals `objects/race-*.svg` (Half-Beast,
  Goliath and Merfolk placeholders).
- The looks: `sprite-catalog.js` `RACES` (lines 12–152: 9 races × 4 male and 4 female = 72) on the humanoid rig of
  `sprite-humanoid.js` (1,128 lines: proportions read as ratios of the old chibi rig, skins and 15 body materials, about
  76 part styles, ears including `beast` and `beast-round`, lower bodies, wing-arms, tails) and `sprite-kit.js` (299:
  the muted palette `tone`, ink shadows, hatching, 32 metal palettes `MATERIALS` at 139–172).
- Screens 1 (the sheet), 2 (the drawer), 4 (the race picker and appearance step); captures `game-sheet` at 1 and 1.65.

**What the engine provides.** The UI for the screens; `Render3D` (`Palette` slots such as `skin`, `cloth`,
`metal.base`; `View3D` and `surface` views for the race picker's figures), `Animation` (`Skeleton`, clips shared by bone
names, `StateMachine`, `Gear`: `socket.<slot>` bones and skinned garments), `Scene` (`Actor`, `SceneView`); the clip set
per rig family (`Docs/Animation.md`, "Clip set"); the records.

**What is new.**
- The actor record (the old game's character record is "effectively the game's actor schema": the hero, companions and
  every enemy share it) and its calculations as an object instead of `app.js`'s 65 top-level variables and `withActor`'s
  swap (`combat.js` line 143): the design doc's main subject, with the effect interpreter's shape (the old one checks
  216 `type` values in `app.js` and 176 in `combat.js`).
- Task 11's three races are in the old game already (T1); their looks and seals are placeholders to remake (T5): 24
  looks (Half-Beasts that clearly show their beast, wolf, fox, bear or lynx, T14; Merfolk recognisably sea-folk on land,
  legs and fin ears, T3; Goliaths visibly giant-blooded, stone-grey skin and rune-like markings, T4) and three seals
  "that sit with the six older ones". Their racial classes come with GM8.
- The Goliath and Human at low levels (`../old-game/Docs/Notes.md`, "New Races": at Level 40 with the default allotted
  stats a human has 48 HP, a Goliath 40) is "a balance point for the port's race review" (GM2-4).

**The art.** The humanoid rig family: one skeleton with the `socket.<slot>` bones, bodies for the nine playable races
and both sexes (builds, heads, hair, beards, ears, eyes, skins in palette slots), and its clip set: about 28 clips
(`Docs/Animation.md`, "Clip set": idle and stunned; advance, return, flee; seven attacks, the one-handed and two-handed
swings, thrust, dagger stab, staff or wand strike, unarmed punch, off-hand swing; bow and crossbow; cast, skill, item,
drink, meditate; guard and evade, entered and held; hurt, critical hurt, stumble; death, victory). The 72 looks built
from it, the three seals as SVG. Per Decisions for M7 the first humanoid is approved "with gear and its full clip set,
shown as renders and a playing view" before the rest: GM2 makes a sample weapon and armour piece for that approval,
replaced by GM3's. Blender script family: `humanoid` (rig, body parts, looks as parameter sets, clips keyed by script);
the enemies' parts (goat legs, tails, wings, elemental bodies) are added to it in GM9. Checked against Blender: the
looks' renders (`ReferenceLimits.M6`), the clips against Blender's evaluation, the playing view by the user's eye.

**How it is checked.** The formulas and EXP curves against the old game (`CombatRules` in Node for the EXP and level
curves it holds; the sheet's formulas through the test client); the race passives against the old game's race checks
(`new-races-browser.js`, 22 rules each shown able to fail) where they act on the sheet (their battle side in GM5); the
sheet against `game-sheet` (its slots and buttons inert until their milestones), the race picker against a capture
added to `Reference/game-capture.js`, with the 3D figures masked. Not checkable against the old game: the figures
(Blender and the user only).

**Depends on / needed by.** Depends on GM1. Needed by everything: combat evaluates every actor through these rules;
companions (GM11) and enemies (GM9) are actor records; the humanoid rig carries every humanoid enemy and boss.

**Weight: XL.** About 1,700 old lines for the sheet and races, 1,560 for the humanoid and its kit; one rig family with
about 28 clips and 72 looks, the first model approved before the rest.

**Open questions for the user.**
- GM2-1 **The 72 looks in 3D:** (a) each old look remade as its 3D counterpart (its hair, beard, skin, ears and build
  as the catalogue gives them), more detailed; the user approves the first humanoid (M7), then each race's eight
  [recommended: faithful, and the catalogue already defines them]; (b) new looks designed per race, eight each,
  proposed as renders; (c) a character maker: the player builds a look from parts (hair, face, skin, build) instead
  of picking one of eight.
- GM2-2 **The hero on the sheet:** (a) as the old game: no figure on the sheet (the race seal by the name), the figure
  in the race picker and in battle [recommended: faithful]; (b) a live 3D figure on the sheet beside the equipment
  slots, wearing the gear.
- GM2-3 **The game's palettes, light, line widths and hatching** ("asked of the user when they come up",
  `Docs/Renderer-3D.md`, "Method"): (a) the engine's approved look (Decisions for M6; Ji'ruan's stage, M10) with the
  palettes taken from the old game's (`sprite-kit.js` `MATERIALS`, `forge-art.js` `PALETTES`, the skins per species),
  shown on the first humanoid for approval [recommended]; (b) a new palette proposed with the first humanoid.
- GM2-4 **The Goliath and the Human at low levels:** (a) kept as the old game; reviewed with every number in GM17
  [recommended: one balance pass with the reworked classes in]; (b) reviewed now, before the looks.
- GM2-5 **The three new race seals:** (a) SVG in the style of the six older seals, shown as captures [recommended];
  (b) all nine seals remade as renders of 3D emblems.

## GM3 Arms and armour: the forge, artifacts and the inventory

**What the player gets.** The inventory window (tabs for weapons, armour, artifacts, consumables, ingredients, runes;
favourites; item details with stats, parts, runes and effects), equipping into the primary hand, the off-hand, the
armour slot and two artifact slots (two-handed weapons take both hands; race restrictions), rarity tags and colours
(common, uncommon, rare, epic, legendary, masterwork, exotic, the Exotic's moving wave). Every weapon and armour piece
is forged: a main part and two secondary parts by makers from materials at a level, which set power, scaling, hit, crit,
pierce, Mana cost and price; up to 3 rune slots on weapons by level (0 below 40, 3 from 85; Cherubim always 4) and up to
2 on armour; staffs and wands with a stone or jewel, a handle and an inscription; named masterworks with fixed main
makers and Mythilium; Exotics in Exium (worn above Level 100 from GM15); a matching bonus when every part is one
maker's; the 85 artifacts with their effects, the masterwork artifacts fixed at the level they were found (Levels
1–200); the 5 key items. Every item a 3D model, worn on the figure as it is made.

**What the old game has.**
- `forge-engine.js` (1,884 lines, pure, `ForgeRules`): 8 weapon makers (Roundtable, Dwarven, Elven, Boreal Bolt, Abyss,
  Hellmade, Hearthforge, Outlaw; `MAKERS` 19–36), 39 weapon materials (9 common, 7 uncommon, 8 rare, 7 epic, 6
  legendary, Mythilium, Exium; `MATERIAL_ROWS` 76–118), 12 caster handles, 12 stones in 3 tiers and 12 inks (124–164),
  23 weapon types (21, staff, wand; `WEAPONS` 196–221), 24 runes (228–253), 12 armour types (3 heavy, 3 light, 3
  clothing, 3 shields; `ARMORS` 800–813), 11 armour makers (adding the Institute of Magic, the Church of Talos and
  Vampiric), 48 armour materials (841–890), `MASTERWORKS` 37 entries (32 masterworks, 22 weapons and 10 armour pieces,
  and 5 Exotics; 1101–1383), 4 natural weapons (Beast Claws, Sunhorn Antlers, Vulcanic Ram Horns, Stardragon Claws;
  1524–1529), tier odds by level, `MAX_LEVEL` 200, prices, names, every id format, `armCharacter` (an enemy's gear,
  used by GM5 and GM9); the reforge rules (1787–1873) go with GM6, `LEGACY_ITEMS` (1575–1619) is not ported.
- `forge-art.js` (1,046): every part per maker style and material palette (`PALETTES` 39 at 17–58), maker logos
  (`LOGOS` 8), `MASTERWORK_LAYOUT`, armour portraits, rune glyphs (`RUNE_GLYPHS` 24), artifact looks (`ARTIFACT_STYLE`
  per rarity; `ARTIFACT_SHAPES` draws 49 objects, while the 85 records name 50 shapes and 76 glyphs in their `look`;
  `MASTERWORK_ARTIFACTS` 5); `sprite-equipment.js` (492: weapons in 17 sprite shapes, masterwork weapon and shield
  designs, `armorLayers` for the rig).
- `app.js` 3222–4737: parsing, masterwork artifact levels (`masterworkArtifactAt`, a straight line through Levels 1 and
  80), item power, hit and crit chance, labels (`artifactEffectLabel` for about 40 artifact kinds), equip rules, the
  inventory and catalogue windows, item details, `getItem(id)` (catalogue ids, forged ids, masterwork templates,
  levelled artifacts, runes, ingredients).
- `objects/artifacts.json`: 85 (32 uncommon, 24 rare, 16 epic, 8 legendary, 5 masterwork: Second Wind, Wraithguard,
  Arrowtip Rain, Titanfall, Treasure Seeker; all `placeholderArt: true`); `objects/key-items.json`: 5 (Gem of False
  Dreams, Vial of Dragonblood, Blood Crystal, Shadow Crystal, Phylactery Core).
- Screens 7 (inventory), 8 (item catalogue, Dev); capture `game-inventory`. Checks: `masterwork-elementals-browser.js`
  (22 mutations), `artifact-rework-browser.js` (12 entries, 85 mutations), `new-masterworks-browser.js` (10 entries,
  103 mutations), the forge parts of `limitless-browser.js`.

**What the engine provides.** `Animation/Gear.cs` (`ActorItem` on `socket.<slot>` bones; `ActorGarment` skinned to the
wearer, garments "made per body type", Decisions for M7, "Gear in detail"), `Render3D/Palette.cs` (one sword model in
every metal, one robe in every cloth), glow from emission (runes, Exotic parts), `surface` views for item pictures, the
UI's long lists (with the painter's limit, "Engine items the port will meet").

**What is new.** The placeholder art of tasks 1, 3, 4, 5 and 7 remade as proper art (`Docs/Port-Tasks/README.md`,
"Placeholder art the port remakes fully"): task 1's six masterworks (the Plasma Blade's red laser beam from its gem,
Tidepiercer a trident-shaped lance with a crossguard; First Comet, Peace and Balance, Kugelblitz and Avalanche without a
design hint yet); task 3's Doctor's Orders (the forged robe in white); task 4's every artifact ("artifacts of a rarity
similar with a few details off, masterworks each completely their own") and the four masterwork artifacts (Second Wind a
glass lung with wind, Wraithguard a pale gauntlet trailing mist, Arrowtip Rain a quiver with falling arrowheads,
Titanfall a cracked stone fist); task 5's Treasure Seeker (a gilded compass over coins), Moneyshot, Lament, Noble Hero
and the Hexbane Coat ("each masterwork its own"); task 7's five Exotics ("each Exotic its own, Winged Hussar's wings its
defining feature"). The Exotic colour and wave are real and kept. Task 14's three masterworks, Exotic and smithing
materials come with GM16. The Transformer's two looks (GM3-4).

**The art.** Weapon parts for 23 types in 8 makers' styles (main, guard, third part), caster parts (12 stones in 3
tiers, 12 handles, 12 inscriptions), armour for 12 types in 11 makers' styles with the makers' helmets, made per body
type (GM3-3), shields; materials as palette slots (39 weapon and 48 armour materials); 37 unique masterwork and Exotic
designs; 4 natural weapons and the Half-Beast's claws; 24 rune glyphs glowing on the parts; 85 artifacts (80 from the
shapes in four rarity styles, 5 masterwork designs); 5 key items; the maker logos. The item pictures in the UI (GM3-2).
Blender script families: `weapon-parts`, `caster-parts`, `garments`, `shields`, `masterworks` (one script per design),
`artifacts`, `key-items`. The Blender reference checks each part and design rendered alone and worn by a GM2 figure
(`ReferenceLimits.M6`), and gear on sockets against Blender's evaluation (`Docs/Animation.md`, "Rendered gear").

**How it is checked.** `ForgeRules` in Node, exhaustively over what the tests name: every id format encoded and decoded,
generation by level, tier odds, prices, names, `armCharacter`; the item rules of `app.js` (power, hit and crit chance,
equip rules, the masterwork ramp, `getItem`) through the test client; the four task checks above rule by rule; the
inventory against `game-inventory`. The masterworks' and artifacts' battle effects are checked in GM5. Not checkable
against the old game: the models (Blender and the user's approval only).

**Depends on / needed by.** Depends on GM2 (the bodies the garments fit, the sockets). Needed by GM5 (weapon attacks,
armour, artifact effects), GM6 (every shop's stock, reforging), GM7 (casters, runes at the tower), GM9 (enemies' gear),
GM15 (Exotics, gear above Level 100), GM16 (task 14's items).

**Weight: XL.** About 4,900 old lines; the largest item art set: 23 × 8 weapon part sets, 12 × 11 armour sets times the
body types, 37 unique designs, 85 artifacts.

**Open questions for the user.**
- GM3-1 **How detailed the item models are** ("the level of detail of the item models", "At port time"): (a) made for
  close-up: an item's details and the shops show it large, so each part is modelled for that size, and the same model
  is small in a hand in battle [recommended]; (b) made for battle size, the item pictures kept small; (c) two models
  per part, a close-up one and a battle one.
- GM3-2 **Item pictures in the inventory, the shops and the details:** (a) live 3D views of the item's own model in its
  materials and runes [recommended: forged items are combinations of makers, materials and parts too many to draw or
  render one by one]; (b) 2D icons per item kind drawn in code, as the old game, with 3D only in the details pane; (c)
  pictures rendered from the models when the game is built, per part and material.
- GM3-3 **Armour on the different bodies** (garments are made per body type, Decisions for M7): (a) each piece fitted
  to each playable body (9 races × 2 sexes) and to each humanoid enemy body that wears forged armour [recommended: a
  close fit on every figure]; (b) a few shared builds (small, medium, large) that every body maps onto (fewer models,
  a looser fit).
- GM3-4 **The Transformer** (`Docs/Notes.md`, "Game inconsistency": the old battle sprite draws a round copper shield,
  the item and its icon a kite shield): (a) the kite shield everywhere [recommended: the item's own design]; (b) the
  round copper shield.
- GM3-5 **Masterworks and Exotics without a design hint** (First Comet, Peace and Balance, Kugelblitz, Avalanche,
  Moneyshot, Lament, Noble Hero, the Hexbane Coat, four of the five Exotics): (a) designed by the port from each one's
  name, effect and maker, shown as renders a few at a time for approval [recommended]; (b) each described by the user
  first.

## GM4 The continent and travel

**What the player gets.** The map window over the sheet: one continent of 20 regions in 3D seen straight down (zoom
1–10× of the fit, pan, Fit and Follow), regions under fog until first entered (W12), travel by clicking a place or a
numbered destination along the fastest route (roads fastest; rivers, marsh, sand and volcanic slopes slow; off the roads
only through entered regions, W30), the clock running while travelling (36 game minutes a real second at 1×, buttons
for 0.5×, 1.5× and 2×) and idling (0.5), day, dusk and night with lanterns in windows, rain and lightning over
Stormwake, fog and clouds as the Stoneveil rises (W32), the boat between Seabridge and Emberhaven (12.6 hours, W11; a
trip of about 5 seconds on the map itself, W14, W31), caravans between unlocked castle cities (2 silver a km, 21 km/h of
road; a castle's caravan opens on the first visit; W16), the carriage's rules (twice as fast on roads, 1.5× on open
ground, not on the mountain roads, rivers, Cinder Isle or rough zones; W10, W33; bought in GM6), the header (region,
levels, day, clock, light, time speed), the zone readout, the actions bar (each service's button arriving with its
milestone), 31 keyboard shortcuts. Each region plays its theme, crossfading over 4 seconds at a border (W14); the empty
regions keep the last theme (W28).

**What the old game has.**
- `world-engine.js` (1,128, pure, `WorldRules`: the Alder Vale's data, the region registry, zones (`zoneAt`, 436–568),
  speeds, the carriage's ground, the road graph, routes (`continentRoute`, Dijkstra over every region's roads;
  `landPath`, A* on a 32 C grid), caravan trips, stepping, lighting, exits); `world-region-data.js` (414: the other 16
  charted regions and the 3 empty ones: settlements, roads and rivers as Bézier point lists, zone ellipses, exits,
  sites, the boat, level ranges, weather, guild services); `continent-engine.js` (277, `ContinentRules`: draft 6's
  origins, `regionAt`, `landAt`, the borders); `continent-shape.js` (11 lines, 255 KB: the land on a 1150 × 1069 grid of
  16 C cells, baked by `Docs/Port-Tasks/8_Map_Rework/bake.py`). A region is 1600 × 1000 local units (120 × 75 km),
  enlarged 1.4× on the continent; `UNITS_PER_KM` 40/3; an encounter roll every 126 travel minutes (GM10).
- The view: `map.js` (1,469: the camera, overlays, altitude effects, lanterns, markers, the clock, travel, the boat, the
  caravan, the carriage, the keyboard, ambient time), `map-terrain.js` (893: a field grid of 14 channels per region, 41
  feature kinds, canvas tiles), `map-regions.js` (1,062: 17 region specs, field functions), `map-continent.js` (170),
  `map-ink.js` (350); the map blocks of `styles.css`.
- The clock: `advanceAmbientTime` (`map.js`, every 250 ms), `advanceWorldTime` (`app.js` 5541: 5 % of max HP and Mana a
  game hour, daily skills reset on a new day, shops restocking in 6-hour windows), `WorldRules.lighting`.
- `music.js`'s `desiredTheme` and the 4-second crossfade (`CROSSFADE`).
- The 20 regions: Alder Vale (the start; zones 1–15), Verdant Reach 5–20, Stoneveil Foothills 10–20, Sunward Coast
  15–30, Stormwake Hills 15–35, Mirrormere Basin 20–40, Cinder Isle 25–40, Gloamfen Marsh 35–55 (with the Abyssal Rift),
  Saffron Expanse 35–50, Crimson Divide 40–55, Deep Desert 45–55, Verdigris Jungle 45–65, High Stoneveil 45–60,
  Bloodstone Coast 55–70, Duskpine Forest 60–75, Ancient Battlefield 65–75, Stoneveil Summit 85–95, and the three
  uncharted ones; 27 settlements (11 castle cities, 16 towns) and 3 sites (the Church of Talos, the Institute of Magic,
  Hearth Home).
- Screens 25–27 (the map, the caravan panel, the boat and caravan trips; 29, the region transition prompt, is legacy);
  capture `game-map`. Checks: `map-rework-browser.js` (9 entries, 44 mutations), `map-speed` (a known flaky timing
  check).

**What the engine provides.** `Render3D/Terrain.cs`, `TerrainData`, `TerrainImporter` (`.terrain.json`), `PlacedTerrain`
("the map is a scene"), the orthographic camera, water with two wave trains, placed models, picking (`View3D.Pick`);
the soundtrack (`Soundtrack.Play`, which still switches themes as `music.js` did before task 8: `Docs/Audio.md`,
"Decisions", "Theme API"); image cursors with hotspots (Decisions for M5).

**What is new.** The 3D map (W1: every 2D look of the continent is placeholder), draft 6 its reference
(`Docs/Port-Tasks/8_Map_Rework/continent-draft-6.png`, W26); task 8's placeholders remade (the continent's painting,
the fog, the boat and its waves, the caravan's wagon and route line, the carriage's switch, the mountain's fog and
clouds); task 3's Mirrormere island and bridge and the site icons (`SITE_ICONS`: a domed tower, a forge with a flame);
task 6's Rift on Gloamfen's map ("a dark violet scar with a red-glowing heart"); the three new regions' land (GM4-4);
the region crossfade in the engine's soundtrack (an engine change, tested against `music.js` in Chromium; the small task
"Music switch during a fade-in" touches the same code).

**The art.** Terrain for 20 regions (a 16-bit height image and a cover image each; "first versions can be generated
from today's field functions", Decisions for M6) on the baked land; the coast, the two inland waters, lakes, rivers,
roads; the 41 feature kinds of `map-terrain.js` (trees by kind, rocks, ruins, farms, windmills, towers, wrecks, pyramids
…) as placed models; 27 settlements and 3 sites (GM4-2); exits and waymarkers; the Abyssal Rift and the Vampire
Citadel; the boat, the caravan wagon and the carriage; lanterns, rain, lightning, the mountain's fog and clouds, the fog
of unentered regions (GM4-6); the labels and the map's finish (GM4-1); the hero's place (GM4-3); the grabbing cursor
(GM4-7). Blender script families: `terrain` (field functions to heights and covers, as `make_terrain.py` makes the test
terrain), `map-features`, `settlements`, `sites`, `vehicles`. Checked against Blender's terrain renders (Render3D's
terrain scenes) and by the user against draft 6.

**How it is checked.** `WorldRules`, `WorldRegionData`, `ContinentRules` and `ContinentShape` in Node, exhaustively
over what the tests name (routes, `zoneAt`, speeds, the carriage's ground, caravan fares and times, lighting,
`regionAt`, `landAt`); `map-rework-browser.js` rule by rule; the crossfade against `music.js` rendered in Chromium (as
M8's fixtures are); the map's header, actions and destinations against `game-map` (the 3D view masked). Not checkable
against the old game: the 3D map's look.

**Depends on / needed by.** Depends on GM1 and GM2 (the clock's regeneration acts on actors); nothing from GM3 or the
battle. Needed by GM5 (a defeat carries the party to the nearest settlement), GM6 (settlements, the clock's stock
windows), GM8 (the counts of caravan trips, boat trips and regions entered), GM9 (the new regions' enemy types, if
GM4-4 designs them here), GM10 (encounters, foraging; the feature models reused on the battle stages), GM11 (markers,
quests), GM12, GM13 (sites), GM16 (dungeon entrances, DG5, DG72).

**Weight: XL.** About 5,800 old lines; terrain for 20 regions, 41 feature kinds, 30 places, the vehicles.

**Open questions for the user.**
- GM4-1 **The map's finish:** (a) the 3D terrain in the cel, ink and hatching look, with the old map's parchment frame,
  compass, scale bar and inked labels [recommended: keeps the old map's character]; (b) the 3D terrain with its labels
  only.
- GM4-2 **Settlements and sites on the map:** (a) each castle city and town its own small 3D model from a kit, seeded
  per settlement as the old interiors are, castles larger [recommended]; (b) one castle-city model and one town model
  shared by all.
- GM4-3 **The hero on the map:** (a) a marker, as the old game [recommended: faithful]; (b) a small 3D figure of the
  hero walking the route.
- GM4-4 **The three new regions' land** (W27): (a) designed here (names, identity, terrain, level bands, roads, the
  towns' places: drafted by the port, approved by the user) and built with the map; their enemy types drafted with the
  revamp in GM9, their encounters, services, forage and music in GM12 [recommended: the map closes whole, GP-3, and
  each creature family is opened once]; (b) all in GM12, the map showing them as plain uncharted land until then.
- GM4-5 **The clock:** (a) as the old game: time passes on the sheet and in windows (0.5 game minutes a second) and
  while travelling (36), battles stop it, a minimized window stops it [recommended: faithful]; (b) time passes only
  while travelling, resting and camping.
- GM4-6 **The fog over unentered regions** (W12; its look a placeholder): (a) drifting cloud over unentered land in the
  3D look, lifting when the region is entered [recommended]; (b) unentered regions drawn as blank parchment.
- GM4-7 **The grabbing cursor** (a game-styled image, Decisions for M5; its design open): (a) an inked gauntlet, open
  over the map and closed while dragging [recommended]; (b) an inked bare hand; (c) the system's cursor.
- GM4-8 **Map sounds** (GM1-6): (a) a small set: steps or hooves while travelling, waves on the boat,
  wagon wheels, rain and thunder over Stormwake [recommended]; (b) none, the music only.

## GM5 Combat core: the battle, its rules and its stage

**What the player gets.** Turn-based battles between the party (the hero, up to three companions from GM11, their
summons) and up to 50 enemies (normally 1–4; the stage lays out up to 8 a side). Turn order by Speed on a turn meter;
5 AP a turn (plus class and potion bonuses) for a weapon attack (2 AP, unarmed 1, off-hand attacks with two one-handed
weapons), a spell (GM7), an item (a potion 1 AP, a poison thrown, a trap placed, an ingredient eaten by an Assimilator),
a skill (GM8), Guard or Evade (2 AP: ×1.5 Armor or Evasion and ×0.5 the other until the next turn), Meditate (4 AP),
Run (1 AP) or End turn; optional 0-AP reactions. Task 12's twelve statuses and twelve reactions work both ways. Victory
opens the rewards window (EXP, silver, items); defeat carries the party to the nearest settlement (from GM4). A battle
in progress is saved and resumed (GM5-2). The battle screen: the animated 3D stage (figures in formation, the clips,
projectiles, effects, floating numbers, status icons, the turn-order column, log toasts with a pinnable history, turn
and outcome banners, a Speed-up button) and the classic card journal (GM5-1), the action bar, the battle settings (pace,
speed, animations, view), Dev Mode's combat setup.

**What the old game has.**
- `combat-engine.js` (114 lines, pure, `CombatRules`): `nextTurn` (counters fill by Speed to 1,000), damage = power ×
  crit × (1 − resistance %) × 100 / (100 + 2 × Armor), at least 1 (spells ignore Armor); hit chance Accuracy /
  (Accuracy + Evasion), ranged attacks against half Evasion; crit chance 100 × (1 − e^(−rating/100)) at most 99 %, with
  momentum (`critMisses`); the crit multiplier 1.25 + bonus, at most 1.75; trap damage, rune reduction, resources, the
  trap limit, EXP curves, Meditate (20 % of max Mana + 0.5 MND + 0.2 LCK) and escape (50 + 0.5 SPD + 0.2 LCK − 0.4 × the
  fastest enemy's SPD, at least 5 %).
- `combat.js` (7,041 lines) without the parts other milestones take: actor values (1–480: `withActor`, `values`,
  `defense`, natural armour, AP, restrictions, Ki, loadouts, `setHp`); healing, artifact effects in battle (553–680),
  `applyCombatDamage` (barriers, death saves, Second Chance, Tough-Out), equipment reactions, timed effects (490–1073);
  the turn loop, defeat, `finish`, loot and EXP (1231–1700; loot 1386–1600); actions and damage (1700–2956:
  resistances, traps, `hurt` 2110–2270, on-hit effects, `strike` 2612–2743, the unique weapons' hits 2456–2533);
  effects, items, poisons, ingredients, stances, Meditate, Run (3917–4860 less `cast` and `useSkill`); the AI's
  forecasts and candidates (4859–6078 less the profiles); rendering (6079–6341); loading, `validateEnemy` (6375–6496),
  the Dev setup, `startRoster` with Empowered, Sparkling and Limitless scaling (6342–6693); `restore` (6694–6759). GM7
  takes `cast` and the summons, GM8 `useSkill`, GM13 the phases and cutscene hooks (1074–1230), GM13, GM14 and GM16 the
  boss mechanics (2957–3916) and the AI profiles (5850–6007).
- `combat-ai.js` (130, pure, `CombatAI`: bounded memory, `intent`, `rank`); "previews never spend resources, change
  state, or roll RNG".
- `battle-stage.js` (666: `FORMATIONS` for 1–8 a side, depth, the queue of beats, `whenIdle`), the generic parts of
  `battle-effects.js` (563: projectiles, hit sparks, items and potions, stances, Meditate, skills, masterwork
  activations; the element palettes go with GM7), `settings.js`'s battle settings, the "Animated battlefield" and
  "Combat cards" blocks of `styles.css` (3022–3506).
- Screens 16–21 (combat setup, the stage view, the classic view, the action picker, the reaction prompt, the rewards);
  capture `game-combat`. Checks: the combat, enemies and lifecycle suites (`combat-performance.cjs`, 530 AI cases;
  `combat-lifecycle-performance.cjs` and its fixture) and the battle checks, run with the stage off
  (`BattleStage.enabled = () => false`) and a second copy of `combat.js` with its internals exposed.

**What the engine provides.** `Scene` (`SceneView` with `Rate` as the battle pace, `Actor`s on nodes), `Animation`
(state machines, clip events as awaitable markers: a hit landing, a bow's release), `Render3D` (`Particles`, particle
effect records at `models/effects/<name>`), `Audio` (`SoundEffects`, `Actor.Sounds`), the UI's Web Animations and event
loop for the banners, toasts and the AI's waits (`Docs/Scripting.md`).

**What is new.** Task 12's element statuses and reactions (`Docs/Port-Tasks/12_Spell_Rework/Element-Statuses.md`):
Exposed, Burning, Soaked, Cracked, Off-Balance, Shocked, Chilled/Frozen, Blessed, Hexed, Rooted, Withered, Bleeding;
Resonance, Melt, Steam, Topple, Wildfire, Electrocute, Flash Freeze, Purify, Devour, Overgrowth, Exsanguinate, Blood
Harvest; each spell's own chance and duration (from GM7), working both ways; "how each looks and sounds in battle is
made in the port". Spells never crit (SP20); bosses can't be defeated outright by instant death (SP46). The effect
interpreter is built here for every effect type the old game's battle reads, so GM7 and GM8 add only the types their
drafts mark "(new)".

**The art.** The battle camera (the fixed orthographic side view) and formations in 3D; the humanoid clips at work
through a battle state machine (the lunge without root motion, hurt, death); particle effects for hits, criticals,
misses, projectiles (arrows, bolts, thrown flasks), Guard and Evade, Meditate, fleeing, drinking, traps set and sprung,
masterwork activations; the twelve statuses on the figures and their icons, the twelve reaction bursts (GM5-3); the
floating numbers, target marker, banners and turn list in the UI. What isn't combat's aspect shows as stand-ins until
its milestone: the regional stages (GM10; a plain test stage meanwhile), enemies (GM9), the potion and trap models
(GM6). No new rig family; the effects are particle records, their positions checked against Blender (their motion "not
checked against Blender", `Docs/Scene.md`, Status, step 5) and their look by the user's eye.

**How it is checked.** `CombatRules` and `CombatAI` in Node; the rest of the rules through the test client with
`Math.random` stubbed, every rule of the old battle checks mirrored, on actors that exist in both games (without
classes, or with effect types both share), since the old classes and spells are not ported (GP-1). The AI: its forecasts
never change state or roll (a rule with its own test), its cost per decision measured against the old suites' 530 cases.
The battle screen's UI against `game-combat` (3D masked) and a capture of the classic view. Not checkable against the
old game: the statuses' looks (new) and the 3D stage (Blender and the user).

**Depends on / needed by.** Depends on GM2 (actors, clips), GM3 (weapons, armour, artifacts, `armCharacter`) and GM4
(the settlement a defeat carries the party to). Needed by GM6 (its consumables' battle rules) and every milestone with
a fight: GM7–GM16.

**Weight: XL.** About 5,000 of `combat.js`'s lines, `combat-engine.js` 114, `combat-ai.js` 130, `battle-stage.js` 666
and part of `battle-effects.js`; 24 new status and reaction rules; the battle's effects.

**Open questions for the user.**
- GM5-1 **The battle views:** (a) both, as the old game: the animated 3D stage (the default) and the classic card
  journal [recommended: faithful]; (b) the 3D stage only.
- GM5-2 **A battle in progress when the game is saved or closed:** (a) saved and resumed on load, as the old game (a
  boss intro never replays) [recommended: faithful]; (b) not kept: loading puts the party back before the battle.
- GM5-3 **How statuses and reactions show:** (a) each status on the figure in its element's colours and as an icon on
  the turn list and the cards, each reaction a burst of its own with a sound, proposed as a set of renders for approval
  [recommended]; (b) icons and log lines only, as the old game's timed effects.
- GM5-4 **Battle sounds** (GM1-6): (a) hits by weapon family, misses, criticals, Guard and Evade,
  deaths, potions, traps, the lunge's steps from clip events, the turn banner [recommended]; (b) only hits, criticals
  and deaths.

## GM6 Towns: the shops, the Apothecary and resting

**What the player gets.** In every settlement (11 castle cities, 16 towns): the **Blacksmith** (forged weapons and
non-clothing armour at the hero's level, castle 9 pieces with 5 weapons, town 6 with 3; regional masterwork offers such
as Bloody Mary in Ravenwatch for 5 Blood Crystals; buying weapons and armour; Reforge), the **Tailor** (clothing, castle
6, town 4; Reforge), the **General Store** (consumables, traps and Camping Supplies, castle 8 kinds, town 5, 3–6 or 2–4
of each; the horse carriage once, 50,000 silver) and the **Apothecary** (Potions: the Health and Mana pairs of the
hero's band and below always, plus random potions and poisons, castle 8, town 5; Ingredients: every common one and 5 or
3 of the region's rarer ones; Brewing in a cauldron with three ingredient boxes, known recipes only, free but for the
ingredients; Recipes: three unknown ones at 10× the potion's price; Sell). Selling pays half the price, never more than
the shop's own; "sell all copies" and "sell all non-favourites". Stock is made per settlement when first opened in a
6-game-hour window and saved. Reforging: a piece's two secondary parts replaced by any shop-made part up to the main
part's rarity, 3 × the rarity price per changed part, from the pack, equipped or on a companion. Resting in a
settlement (10 hours; 25 silver in a castle city, 15 in a town) and camping (8 hours, Camping Supplies). Each shop is a
full-window room lit by its fires, lamps and windows by day, dusk or night, the same room every visit to a settlement.

**What the old game has.** `shop.js` (562: `Shop`, `ShopLayout`; the shop sizes `SMITH`, `TAILOR`, `GENERAL`, `TOWER`,
`SHOPS`, `HEARTH` at 11–30; `STOCK_VERSION` 26; `CARRIAGE_ART`), `shop-engine.js` (98, pure, `ShopRules`: the wallet,
the sale price, level bands (≤15 common and tiers 1–2, ≤30 uncommon and 3–4, ≤60 rare and 5–6, ≤90 epic and 7–8, else
legendary and 9–10, Tier 10 weighted 0.1), rarity and tier weights, the refresh window), `reforge.js` (153: the anvil)
and `forge-engine.js`'s reforge rules (1787–1873), `apothecary.js` (383: `Apothecary`, `Alchemy`, the cauldron),
`alchemy-data.js` (429: 56 ingredients at 8–65, 15 common, 18 uncommon, 13 rare, 6 epic, 4 legendary; 62 recipes at
67–130, 10 Health and Mana, 40 potions, 12 poisons, three ingredients each; 2 starting recipes; the forage tables at
134–249 go with GM10; ingredient drops per enemy at 251–413 and the bosses' at 415–428), `alchemy-engine.js` (240,
`AlchemyRules`, pure but window-only), `objects/consumables.json` (71: 50 potions, 12 poisons, 8 traps, Camping
Supplies), and the seven interiors of `scene-banners.js` (`armsHall`, `countrySmithy`, `grandEmporium`,
`countryStore`, `tailorShop`, `apothecaryHall`, `apothecaryShop`; full window 1280 × 900, region materials, seeded per
settlement, three stacked layers). Screens 31 and 33; capture `game-shop` (its stock seeded with mulberry32 `0x5eed` in
`Reference/game-capture.js`). Checks: `shop-rework-browser.js` (9 entries, 121 mutations), `potion-rework-browser.js`
(12 entries, 97 mutations); `shop` and `trap` fail on the old code too ("Cross-cutting", "The old game's checks: the
known failures …"). The rooms' layout rule: faces and counters in y ≈ 130–360 above the panels, emitted fire and
light drawn over everything (`../old-game/Docs/Notes.md`).

**What the engine provides.** The UI; `surface` views and scenes for the rooms (one white sun per view; light sources
"as layered glowing shells with their halo as geometry", as M10's stage made them; glow from emission); the GM2 figures
for the people in the rooms; the long-list limit ("Engine items the port will meet").

**What is new.** Task 3's Tailor's shop, the reforge anvil and the five newer traps' looks (Tripwire, Smoke Trap, Frost
Glyph, Weighted Net, Gravity Well); task 9's two Apothecary rooms ("sharing an apothecary look, grand and modest"), the
cauldron and its animation, and the icons of the 40 potions, 12 poisons and 56 ingredients ("each potion, poison and
ingredient its own"); task 8's carriage drawing. All placeholder in the old game, all made new here.

**The art.** Seven interiors (Blacksmith castle and town, General Store castle and town, the Tailor, the Apothecary
castle and town) in region materials (oak, granite, moss, sandstone, redrock, limestone), seeded per settlement, lit by
the time of day, with their people; the anvil; the cauldron; 50 potions and 12 poisons; 56 ingredients; 8 traps;
Camping Supplies; the carriage; the General Store's goods. Blender script families: `interiors` (a room kit: masonry,
timber, floors, furniture, fires, lamps, the people's places), `flasks` (potions and poisons by tier shape and effect
colour, as the old `potionArt`), `ingredients` (by kind: cap, mineral, fang, feather, scale, liquid, seed or berry,
bark, clay, flower, leaf, as the old `ingredientArt`), `goods`, `traps`, and `vehicles` from GM4. Each room checked
against Blender's render of the same stage (`ReferenceLimits.DetailedStage`) and approved by the user.

**How it is checked.** `ShopRules` in Node; stock, recipes and brewing through the test client (`AlchemyRules` is
window-only) with seeded draws; the two task checks rule by rule; the shop window against `game-shop`, the Apothecary
against a capture added to `Reference/game-capture.js`, the rooms masked. Potions, poisons and traps in battle use
GM5's rules with this content. Not checkable against the old game: the rooms and items in 3D.

**Depends on / needed by.** Depends on GM3 (stock, reforging), GM4 (settlements, the clock's windows), GM5 (consumables
in battle). Needed by GM7 (the tower's window reuses the shop's layout), GM8 (the counts of items reforged and potions
brewed), GM10 (foraging's ingredients), GM11 (Collect quests), GM13 (the Storm Reliquary and Hearth Home's forge reuse
the shop window), GM16 (the materials' reforge discount).

**Weight: XL.** About 2,300 old lines and the shared room kit of `scene-banners.js` (1,418); seven interiors; about 130
item models from a few script families.

**Open questions for the user.**
- GM6-1 **How the rooms are shown:** (a) live 3D rooms in the game's look, lit by the time of day and their own fires,
  each settlement's own (seeded), the people moving [recommended: the old rooms change with the time of day and every
  settlement has its own]; (b) pictures rendered once per room and time of day, without motion.
- GM6-2 **Town sounds** (GM1-6): (a) coins on buying and selling, the anvil on reforging, the cauldron
  brewing, a door or bell on entering [recommended]; (b) only the purchase and the cauldron.

## GM7 Spells and magic: the spell book, Spell Augmentation and the Magic Towers

**What the player gets.** The spell book (known spells, filtered by Damage and Support, details, Use outside battle for
healing and the like); casting in battle (3 AP unless the spell or a class says otherwise; spells ignore Armor and never
crit, SP20); summoned creatures fighting beside the party; a spell's tier needing character level (tier − 1) × 10; the
Tier 10 spells' animations. In each castle city a **Magic Tower** (6 forged staffs and wands, 12 spells with a 10 %
chance of one up to 10 levels early, all 24 runes, rune fitting at 25 % of the rune's price in a magic circle, removal
of all runes at 3 × their price with the runes returned, selling), and on Mirrormere's island the **Institute of Magic**
(Institute staffs, wands and clothing at 15 % off, Doctor's Orders for 30 Platinum, 12 spells at 20 % off, fitting and
removal at 20 % off, a caster anvil, no runes). **Spell Augmentation** at both (full price at a tower, 25 % off at the
Institute; the Storm Reliquary's in GM13 and the Spellweaver's from the spell book in GM8): a hero raises a spell's
attributes in ranks on the element's magic circle.

**What the old game has.** `app.js` 2218–2812 (parsing, values: `calculateSpellValue` at 2323, base power plus a primary
and a secondary stat times their ratios, then modifiers from classes, artifacts, the weapon and allies; costs:
`calculateSpellManaCost`, `calculateSpellActionPoints`; labels; the spell book; loading); `combat.js` `cast`
(4262–4413), `applyEffects` (4069–4256), `summonFriendly` (2896–2956), weather control, Lament's second target;
`magic-tower.js` (473: venues `tower` and `institute`, seven menus between them, the rune circle);
`tier10-cinematics.js` (90: 12 vignettes, one per element's Tier 10 spell, 1.9 s, not blocking, no skip); the element
parts of `battle-effects.js` (its 13 palettes, element colours at 9–23); the tower's room (`towerSanctum`, which the
Institute borrows as a placeholder). `objects/spells.json`: 388 spells (Basic 34; Lightning, Dark, Nature 33; Fire,
Water, Earth, Wind, Holy, Blood, Death 32; Ice 31; tiers 1–10: 49/50/51/50/55/37/36/24/24/12; 257 Damage, 131 Support;
29 summon spells; 38 effect types; `requiredCasterEnemyId`, `seasonVariants`). Screens 5, 6, 23 and 32; capture
`game-spellbook`.

**What the engine provides.** `Scene` sequences (a vignette is a sequence marked non-blocking, M10-3), particles, glow,
`SoundEffects`, the UI; the game binds the vignette's play and its suppression under the combat-animations setting
(M10-3).

**What is new.** Task 12 whole but its classes (`Docs/Port-Tasks/12_Spell_Rework/`, 29 files, all approved, SP1–SP69):
the 381 reworked learnable spells (`Batch-01-Basic.md` to `Batch-12-Blood.md`: 33, 31, 32, 31, 32, 31, 31, 32, 32, 32,
32, 32 by element), the 168 new ones (`New-01-Basic.md` to `New-12-Blood.md`, 14 per element, Basic included), the 7
boss and enemy-only spells unchanged (SP4: War Horn, Call the Storm, Thunderbolt of Talos, Stonecrust, Wheel of the
Year, Vulcanic Charge, Summon Imp Retinue): 556 spell records; the four new summons (Basic's Mana Golem and Wind's Storm
Roc, new creatures; Nature's Dryad and Death's Wraith on existing ones; SP34, SP48); every element keeping a summon and
a Dragonbreath spell (SP33); the 28 spells borrowing another element's status (SP60, SP61); Spell Augmentation
(`Spell-Augmentation.md`: up to 5 ranks per attribute and 10 per spell, SP10; one common attribute list, SP14; summons'
own Vitality, Might and Speed, SP26; the k-th rank costs k × 10 % of the spell's tower price, a reset refunds half,
SP13; the ranks saved per hero and per companion, the enemies' in their definitions, §10); the power curve every spell
follows (SP17–SP22); the 140 "(new)" marks across the folder. The Augmentation menu's look as SP25 gives it. The Tier 10
animations reworked (SP47, the form GM7-2). The Institute's own room (task 3, R1).

**The art.** The spells' effects for 12 elements (GM7-1); the 12 Tier 10 animations; the Augmentation menu (SP25: an
empty circle with one open slot, two lists, the player's spells and the companions'; a lit-up magic circle per element
with a small animation and a sound, a symbol for offensive, summon and defensive spells, a spot per attribute, an
upgraded spot visibly reinforced): 12 circles; the rune fitting circle; the tower's sanctum and the Institute's room
(script family `interiors`). The summoned creatures' models are GM9's (the nine old summon creatures and the two new
ones show as stand-ins until then). Effects are particle records; the Tier 10 animations and rooms are checked against
Blender and approved by the user.

**How it is checked.** The 7 unchanged spells exactly against the old game; the value, cost and AP formulas against the
old game's on the old records, where the rework keeps the formula, with the drafts' numbers as the reference for each
reworked and new spell; casting and summoning through the test client for the rules the rework keeps; the spell book
against `game-spellbook` (its list differing by content), the tower against a capture added to
`Reference/game-capture.js`. Not checkable against the old game: Augmentation, the new spells and the 28 borrowings
(drafts only), the effects and the Tier 10 animations (Blender and the user).

**Depends on / needed by.** Depends on GM3 (casters, runes), GM5 (casting rules, statuses), GM6 (the shop window, the
towns). Needed by GM8 (class spell choices, the Spellweaver and the Ritual Caster), GM9 (enemies' spells and their
augmentations, SP16), GM11 (companions learn spells at a tower), GM13 (the Storm Reliquary).

**Weight: XL.** About 2,000 old lines; 556 spell records from 25 draft files; the Augmentation system; 140 "(new)"
marks; effects for 12 elements, 12 Tier 10 animations, 12 circles, 2 rooms.

**Open questions for the user.**
- GM7-1 **The spells' effects:** (a) one effect design per element and kind (offensive, support, summon), varied by
  tier and by target count, as the old game's element templates [recommended]; (b) each of the 549 spells an effect of
  its own.
- GM7-2 **The Tier 10 animations** (SP47, "the port reworks the Tier 10 spell animations"): (a) short 3D vignettes over
  the battle (about 2 s, not blocking, no skip), one per element's Tier 10 spell, as the old game's [recommended:
  faithful in timing, remade in 3D]; (b) full cutscenes, letterboxed and skippable, like the boss intros.
- GM7-3 **Spell sounds** (GM1-6): (a) a cast and an impact sound per element and kind, varied by tier
  [recommended]; (b) a sound per spell.

## GM8 Classes: the 256

**What the player gets.** A class point each level up to 100 (none under Limitless); the Classes window (the classes
whose requirements are met and the hero's own, their details, the class information tab for classes the hooded man told
of, GM11); buying a class level when its requirements are met (character level, other classes' levels and an "other"
condition: known spells, kills of a race, distance travelled, items sold, traps used, flawless victories, potions
brewed …); each level's permanent effects, passive or skill; skills used from the Skills window or in battle, limited
per combat or per day (reset at a new game day); class spell choices and bound familiars; the Passives window; rarity
colours, none, blue, purple, gold and the new red. Respecs at the Church of Talos come with GM13.

**What the old game has.** `app.js` 562–1057 (`addClassEffect` at 686–937 folding class effects,
`calculateClassEffects`), 2813–2925 (loading the 130 files listed in `CLASS_FILES`, 17–149), 4738–5516 (requirements,
`otherClassRequirementMet` with 41 kinds, descriptions, the class spell choice, `applyClassLevelRewards`,
`changeClassLevel`), 5517–5750 (daily abilities, the skills and passives windows); `combat.js` `useSkill` (4572–4805)
and the passives' triggers throughout. `classes/*.json`: 130 files, 738 class levels, 142 skills (cooldowns: combat 82,
daily 54, none 4, item 2), 127 passives, maximum levels 1–10, 260 distinct `type` strings (139 of them inside `effects`
lists); they stay as they are (C32 (1), C23) and are the reference for every feature task 10 keeps ("the old beside the
remade" in the features file). Screens 9–12; captures `game-classes`, `game-skills`, `game-passives`.

**What the engine provides.** The `classes/` mount (above the packs, below the mods; Q2); loose records (a `.json` whose
object starts with `"$types"`, `Packs/LooseRecord.cs`; the old files don't); M9's Q27 ("the class record and loader
come at port time"), Q31 (the files renamed under the path rule, `Black Knight.json` → `black-knight.json`) and Q29 (an
old name would fail the mount); `Publish.cs` to copy `classes/` beside the game with the listings and a `cover` line in
`licenses.txt` (`Docs/Packaging.md`, "No `classes/`"). The engine's own `LooseRecordTests` keep reading the old game's
`classes/Black Knight.json`.

**What is new.** Task 10's drafts in `Docs/Port-Tasks/10_Class_Rework/` (`Class-Rework-Draft.md`, 1,831 lines: identity,
levels, requirements, rarity; `Class-Rework-Features.md`, 4,388 lines: the features level by level), holding **256
classes**: task 10's 170 (the 130 current, each reviewed, and 40 new), task 11's 3 (Wildheart, Deepcaller, Peakwarden),
task 12's 2 (Spellweaver, Ritual Caster), task 13's 79, task 14's 2 (Dungeon Delver, Throwing Specialist); 252 for
players (Raid Boss, Beast, Great Beast and Monster are enemy-only); rarity 164 without a colour, 66 blue, 18 purple, 6
gold, 2 red (Unreality, Limitbreaker). Their rules: one passive and one skill per class with the listed exceptions (C21,
C24–C27), the passive or skill at the last level, requirements and skills within a turn's 5 AP (C28). The 282 "(new)"
marks (about 133 in task 10's part, 4 in task 12's, 136 in task 13's, 9 in task 14's). The new counts: task 10's (days
in a row without entering a town, caravan trips, regions entered, items reforged, runes fitted, information bought,
Search quests …), task 13's 22 counts, 3 requirement kinds (stats within a quarter of the lowest, no artifacts equipped,
the outlaws group) and the Moonfang key item (a 1 % drop from a wolf defeated at night, used up on acquisition, NC17),
task 14's 2 counts (different dungeons finished, throwing items thrown). C32's duties (1), every class added, and (2),
every class inspected (its findings asked as they come); duty (3) is GM17's. A requirement or feature that needs a later
system is built with that system and listed there: Sparkling defeats and the Moonfang (GM9), the guild's counts and
class information (GM11), Great Hunts completed (GM14), the Limitbreaker (GM15), the dungeons' classes (GM16).

**The art.** The red rarity's look (GM8-2); the skills' battle effects (the `skill` clip with an effect per kind, as
the old `battle-effects.js`'s skills); the creatures classes call up are GM9's models.

**How it is checked.** Requirement kinds and effect types the old classes share through the test client, the old class
files as fixtures (a level's effect on the sheet, a skill's in battle); every feature of the 256 against its draft's
text, a test per feature; the class windows against `game-classes`, `game-skills` and `game-passives` (the rows
differing by content). Not checkable against the old game: the 126 new classes and every changed feature (drafts only).

**Depends on / needed by.** Depends on GM2, GM5, GM7 (spell choices, Augmentation for the Spellweaver, rituals), GM4 and
GM6 (the counts of caravans, regions, reforging, potions). Needed by GM9 (enemies' class levels, C2), GM11 (companions'
classes, the hooded man), GM13 (respecs), GM15 (Limitless class rules), GM17.

**Weight: XL.** About 1,900 old lines of class code; 256 classes from 6,219 lines of drafts; 282 new mechanics; the
counts and kinds.

**Open questions for the user.**
- GM8-1 **The class files' format** (classes stay `.json`, 2026-10-16; the record comes at port time, Q27): (a) the
  engine's record JSON (`$types`, camelCase), one file per class in `classes/` named by the path rule (Q31), editable in
  the editor and by hand [recommended: one format for every record, checked at load as the editor checks it]; (b) the
  old game's file shape, read by a loader of its own.
- GM8-2 **The red rarity's look** (C16, "still to ask"; Unreality and the Limitbreaker): (a) a still red gem tag and red
  name, like the blue, purple and gold [recommended: consistent]; (b) red with a moving wave, like the Exotic rarity;
  (c) a few designs shown as captures to choose from.
- GM8-3 **The drafts' numbers** ("proposals to tune when built", `10_Class_Rework/README.md`): (a) built as drafted and
  tuned in GM17 by simulation, each change approved [recommended: one balance pass with every system in]; (b) tuned per
  class while it is built, each change approved.

## GM9 The bestiary: every enemy revamped and modelled

**What the player gets.** Every enemy kind met in the regions (136), the summoned creatures (9 old, 2 new) and the
Earth Spirit if kept (GM9-3), revamped for the reworked classes and spells, each with its own 3D model in its family,
with every enemy kind the later milestones place: task 14's dungeon-only types and, when GM4 designs the three new
regions (GM4-4), their new types;
Empowered enemies (guild targets, artifact guardians: ×1.3 attributes) and Sparkling ones (0.1 % each, 2 % for a Cull
target; ×1.4 attributes, double HP, 11× EXP, masterwork rolls of 70 % and 5 %) that visibly shine; enemies' gear forged
when they spawn (one level below to three above theirs), dropping at 40 % (not uniques); their ingredient drops.

**What the old game has.** `enemies/*.json` (174 files and `index.json`, each a character record `{ format:
"stat-fick-enemy", version: 1, id, name, level, raceId, classLevels, stats, inventory, equipment, knownSpells, … }`;
optional `drops` (70 files), `naturalWeapon` (58), `element` and `resistances` (55), `activeSkills`, `permanentBonuses`,
`maxHpPercent`, `baseArmor`, `aiProfile` (18) …; ids made from names, `<slug>-level-<n>`, with `formerNames`); by race:
elemental 28, dragon 22, undead 20, human 11, demon 9, troll 8, wolf, orc, harpy, giant and goblin 7 each, merfolk 6,
griffon and dryad 5, impid and devil 4, bear, lizardman, treant, lamia and satyr 3, siren and dark elf 1. `combat.js`
`loadEnemies`, `validateEnemy` (6375–6496: checks, and shared drops: Noble Hero and the Hexbane Coat for non-boss
demons, devils and undead, the Shadow Crystal for all, the Vial of Dragonblood for dragons), `armEnemy` with
`ForgeRules.armCharacter`, `startRoster`. `sprite-catalog.js` `ENEMIES` (199–507, one entry per file from helper
presets), `sprite-creatures.js` (648: `K.quadruped`, `K.griffon`, `K.dragon`, `K.wisp`, `K.core`) and the enemy parts of
`sprite-humanoid.js`: of the 174 sprites 116 are on the humanoid rig and 58 on creature rigs (dragon 22, wisp 13, wolf
8, griffon 6, bear 4, other quadrupeds 3, core 2). `alchemy-data.js`'s ingredient drops per enemy. Checks: the enemies
suite (every enemy spawned), `new-areas-browser.js` (7 entries, 64 mutations), the Sparkling rules of
`new-masterworks-browser.js`. GM9 takes 146 of the 174 files: the 28 that come with their bosses (12 bosses, 12
retinue files, Juila's 4 season spirits) are GM13's, GM14's and GM16's.

**What the engine provides.** The humanoid rig family (GM2); the clip sets per family (`Docs/Animation.md`, "Clip set":
quadrupeds idle, bite or pounce, horn charge, hurt, stumble, stunned, death, victory; griffon and dragon the same and a
wing attack; wisp and core idle, attack pulse, hurt, death); the rule that the first model of each rig family is
approved first (Decisions for M7).

**What is new.** The general enemy revamp, drafted and approved here (GM9-1, GM9-2): every enemy's classes (C2; the 73
classes the old enemies name all exist reworked, C11), spells and their augmentations (SP16: "stronger and later enemies
more"; weavings if the revamp gives an enemy the Spellweaver), stats and gear. Task 6's six Rift enemies remade
(Corrupted Bog Troll 47, Corrupted Mire Stalker 50, Corrupted Mossback Lamia 53, Abyssal Imp 46, Rift Fiend 50, Abyss
Witch 54: "the corrupted creatures recognisably their marsh kin, corrupted as Alder Vale's are, and the demons and the
rift their own", N4). Task 5's Sparkling look ("Sparkling enemies that visibly shine": the gold text on the encounter
screen, the glow and motes over the figure, the gold name and card). Task 12's Mana Golem (Level 18, about 200 HP) and
Storm Roc (Level 50, about 600 HP), "made by the port with its look and stats". Task 14's dungeon-only enemy types,
"drafted at port time with the general enemy revamp" (DG9): types of their own for each dungeon's theme (goblins up to
Level 46 where today's stop at 17, water and earth elementals, demons and impids, devils, undead, harpies above Level 46
(DG57), vampires, dragons), drafted here with the rest and placed in the stages' pools by GM16. The three new regions'
new types when GM4-4 is (a). The bar: as detailed as the cutscene scenes.

**The art.** The creature rig families quadruped, griffon, dragon and wisp (the core family with GM14), each's first
model approved by the user before the rest; the humanoid rig extended for the enemy races (goblin, orc, troll, giant,
devil, impid, undead, satyr, dryad, lamia, harpy, treant, the elementals in their body materials, humans, lizardmen,
merfolk, sirens): lower bodies (goat legs, bird legs, fish and snake tails, floating robes, roots), wing-arms, branches,
tails, wings; every enemy's model and variants (GM9-4), the dungeon-only and new regions' types included, so each
family is made in one pass; the Sparkling shine; their gear from GM3. Script families: `humanoid` (extended),
`quadruped`, `griffon`, `dragon`, `wisp`. About 146 enemies and 2 creatures, and the new types their drafts give,
checked against Blender and approved per family.

**How it is checked.** The fields the revamp keeps against the old files exactly; spawning (`startRoster`; `armEnemy`
through `ForgeRules.armCharacter` in Node); `validateEnemy`'s checks as the enemy kind's `[Check]` (G4, G5); the AI's
cost per decision with the real enemies against the old enemies suite; each enemy's difficulty by simulation (GM9-2).
Not checkable against the old game: the revamped content (drafts) and the models (Blender and the user).

**Depends on / needed by.** Depends on GM2 (the humanoid rig), GM3 (gear), GM4 (the new regions' identities, if
GM4-4 is (a)), GM5, GM7 and GM8 (C2, SP16). Needed by GM10 (the tables name them), GM11 (hunts, artifact guardians),
GM12 (the new regions' types), GM13 and GM14 (retinues on its families; the bosses revamped by its rules), GM16 (the
dungeon-only types in the stages' pools).

**Weight: XL.** About 1,300 old lines (loading and validation 350, `sprite-creatures.js` 648, the catalogue's 309) but
the largest content and art body: about 148 enemies drafted, approved, built and modelled, with the dungeon-only and
new regions' types (their count set by the drafts); four new rig families.

**Open questions for the user.**
- GM9-1 **How the revamp is drafted:** (a) by family (race), a sample of a few enemies first, then batches, each
  approved, every enemy keeping its name, level, place and identity [recommended: as tasks 10, 12 and 13 were drafted];
  (b) region by region; (c) all at once.
- GM9-2 **The revamp's difficulty target:** (a) each enemy about as hard against a hero of its level as in the old game,
  measured by simulation (as SP8 measured every enemy against plain heroes for the spells) [recommended]; (b) set freely
  by the drafts and checked in GM17.
- GM9-3 **The Earth Spirit** (Level 14; in `enemyLevels` but in no table and no summon, so never met): (a) left out
  [recommended: unused in the old game]; (b) placed in a zone's table by the revamp.
- GM9-4 **Enemy variants** (juvenile and adult, corrupted, the elemental dragons' colours): (a) shared models per kind
  with their own parts and palettes, as the old catalogue's presets [recommended]; (b) each enemy a model of its own.
- GM9-5 **Creature sounds** (GM1-6): (a) a voice per family (growl, screech, hiss, rattle) on attack,
  hurt and death [recommended]; (b) none.

## GM10 Regions in battle: encounters, foraging and battle stages

**What the player gets.** While travelling, an encounter roll every 126 travel minutes at the zone's chance (0 in towns
and on roads; 15–62 % elsewhere), less when the hero is 10 or 20 levels above the zone (70 % or 40 %); a prompt showing
the enemies (Sparkling ones marked) with Fight or Run (a pre-fight escape with +25 %); the group's level near the hero's
within the zone's range (weights 72/14/2 by distance); Force encounter (not Dev Mode's: shown in an encounter area or
for a rematch, `map.js` 582–584; corrected 2026-11-13). Foraging on the map (an hour, 45 % to
find something from the zone's table, rivers adding two, then the zone's encounter roll; not in towns, on roads, at
sites or in the uncharted regions, P11). Each battle on its region's and zone's stage, by day, dusk or night, with its
weather, and the region's battle theme (`combat@<region>`). From here a new hero meets revamped enemies on the map.

**What the old game has.** `encounter-engine.js` (1,023, pure, `EncounterRules`: 1–904 data, `enemyLevels`,
`dragonElements`, `animalFamilies` and the 17 regions' chances, profiles and tables; 905–1023 rules, `select`,
`generate`, `generateIncluding`, `encounterChance`, `roll`, `enemiesForZone`): 89 zone tables and 567 groups, empty rule
sets for the three uncharted regions; the flow in `map.js` (`presentTravelEncounter`, `renderEncounterPrompt`,
`startPendingEncounter`, `runFromPendingEncounter`, `forage`); `alchemy-data.js`'s forage tables (`FORAGE` 134–247,
`RIVER` 249); `battle-scenery.js` (303: 33 biomes `BIOMES` 12–46, a biome per region `REGION_BASE` 48–53 and per zone
`ZONES` 54–71, 1600 × 600 layered scenes, seeded, time of day, clouds, rain, snow, embers, fireflies; "still the bright
original scenery", never ink-restyled, `../old-game/Docs/Notes.md`). Screen 28 (the encounter prompt).

**What the engine provides.** Stages from glTF (`model` 2, cameras and one sun; `export_stage`), particles for weather,
the soundtrack's 17 regional battle arrangements in `audio.pak`.

**What is new.** The 33 biome stages in 3D (the 2026-10-16 decision: battles on a 3D stage built in Blender).

**The art.** 33 battle stages with their town and road variants, seeded per region and zone, by day, dusk and night,
with ambient weather as particles. Script family `battle-stages` (a kit per biome: ground, middle, far hills, sky,
foreground). Each stage against Blender's render (`ReferenceLimits.DetailedStage`) and approved by the user.

**How it is checked.** `EncounterRules` in Node, exhaustively over the tables with seeded draws; foraging through the
test client (`AlchemyRules` is window-only); the encounter prompt against a capture added to
`Reference/game-capture.js`. Not checkable against the old game: the stages.

**Depends on / needed by.** Depends on GM4, GM5, GM6 (ingredients), GM9. Needed by GM11 (Cull, Search, Escort fights
and Collect), GM12 (the new regions' tables).

**Weight: L.** About 1,650 old lines; 33 stages.

**Open questions for the user.**
- GM10-1 **The stages' colours:** (a) the ink look with the stages' colours muted as the figures' are (the old
  `K.tone`), since the old backgrounds were never restyled [recommended: one look for figures and stages]; (b) the old
  scenery's bright colours kept.
- GM10-2 **Ambient sound on the stages** (GM1-6): (a) a quiet ambience per biome (wind, rain, birds,
  surf, embers) [recommended]; (b) none, the battle theme only.

## GM11 Companions and the Adventurers Guild

**What the player gets.** Up to three companions, hired at a guild's tavern for Reputation (they arrive at the region's
level with one melee forged weapon and one body armour, every point unspent and no class) or made from a character file
(at most 10 MB, renamed on import); each with a full sheet in the Companions window (attributes, resources, classes,
passives, skills, spells learned at a Magic Tower for coin, class spell choices, equipment taken from and returned to
the hero's pack); commanded by the player in battle; levelling with the battle's EXP, regenerating with the clock,
resting with the hero, following the hero's Limitless switch; dismissing one returns its gear. The guild in 15 halls
(the 11 castle cities and Cairnrest, Lastlight, Amberpost and Emberhaven): the room is the menu. The **bulletin board**
(a daily board of castle 4 and town 3 standard quests, +1 with the Bounty Broker, and a Great Hunt when one is eligible,
GM14), the **counter** (hand in finished quests for silver and Reputation; Deliver letters), the **tavern** (recruits:
castle 5, town 2; 30 + 2 × level + 25 per companion already hired, in Reputation), the **hooded man** in castle guilds
(a class's requirements or an item's sources for 65 Reputation each; no red class while the hero is Level 99 or lower,
NC7), **artifacts** (70 Reputation to have an Empowered enemy guard a chosen artifact in the region, once per 7 days),
**trophies** (completed Great Hunts, rematch markers). One standard quest and one Great Hunt at a time; the Quests card
on the map (J) shows both and lets you give up. Quest types and their reward factors: Hunt 1.8, Cull 1.55, Deliver 1.35,
Search 1.2, Escort 1.1 (planned ambushes 80/10/10 for 0/1/2), Travel 1.0, Collect (paid like one of the lower four by
the ingredient's rarity); the quest's level is the hero's held to the guild region's range.

**What the old game has.** `companions.js` (508: the roster and sheet, import, `makeOffer(s)` with name lists for 9
races at 74–121, `allowedSlots`, `spellPriceFor`, `restoreAll`), `companion-sheet-details.js` (252); `guild.js` (1,069:
`regionalZones` 44–145, the Great Hunt list 148–225 for GM14, boards, accept, give up, hand in, objectives on the map,
Cull areas, `adjustEncounter`, `onBattleFinished`, `onTravel` for Escort, the room's spots, the docked panels, the
Quests card, `classInformation`), `guild-quest-engine.js` (194, pure: `TYPES`, `FACTORS`, `OFFERS`, `RECRUITS`, `CULL`,
`SEARCH`, `AMBUSH`, `AMBUSH_GROUPS`, adjacency over roads and boats, trip minutes), `guild-information-engine.js` (96,
pure: offered classes and items, enemy locations, drop chances); the rooms `guildHall` and `guildTown` in
`scene-banners.js` (placeholders); the state in `worldState.guild`. Screens 13, 14, 30, 37; capture `game-companions`.
Checks: `guild-rework-browser.js` (17 entries, 94 mutations, the 94th unable to differ); `settings-companions` (stale:
it looks for "Hit chance", now "Accuracy"), `runCompanionImportChecks` (flaky), `adventure-improvements` (two entries
fail, one intermittent: "Cross-cutting"). Not covered by an old check: Run keeping its normal chance in Search and
Escort fights, the reward note's fade (`../old-game/Docs/Notes.md`).

**What the engine provides.** The actor record and sheet (GM2, GM8) for the companion sheet; character files
(`CharacterFile.Import<T>`, the save's JSON form, M9); the map's overlay for markers and areas (GM4); picking on a
`surface` for the room's clickable objects (`View3D.Pick`, `SurfaceViews.Pick`).

**What is new.** Task 2's two rooms remade "fully as the detailed illustrations the task asks for" (`2_Guild.txt`
sections 10, 17, 18: a large, elaborate castle-city hall and a modest, compact town guild that visibly differ), keeping
G21 (the guild on the left, the bulletin board beside the counter and its clerk; the tavern on the right with a hearth,
a table of adventurers and, in castle cities only, the hooded man alone at a corner table; trophies on the back wall),
G22 (the room above a panel docked along the bottom) and G23 (every object clickable, a candle-gold outline glow and its
name on hover or keyboard focus). NC7. The hooded man's class information for 256 classes (the old rule leaving out
classes above Level 100, `playerClasses`, except the Limitbreaker from Level 100, NC7). The class requirements counted
here (information bought, Search quests, Escort quests and the like, GM8). The companions' Augmentation ranks (task 12).

**The art.** The two guild rooms (script family `interiors`, with clickable objects named as nodes), the recruits as
GM2's figures, the quest markers, Cull circle, Search spots and Escort ambushes on the map, the reward note.

**How it is checked.** `GuildQuestRules` and `GuildInformationRules` in Node; the guild's flows through the test client
and `guild-rework-browser.js` rule by rule; the Companions window against `game-companions`, the guild's panels against
captures added to `Reference/game-capture.js` (the room masked); the room against Blender and the user's approval.

**Depends on / needed by.** Depends on GM4 (routes, markers), GM6 (Collect), GM8 (classes, information), GM9 and GM10
(hunts, Cull, Search, Escort fights). Needed by GM12 (the new regions' guild services), GM13 and GM14 (parties for the
bosses' simulations; Great Hunts offered and their trophies), GM15 (companions under Limitless), GM16 (Delve,
Ravenwatch's quest, Collect counting dungeon finds, DG71).

**Weight: L.** About 2,100 old lines; two detailed interiors.

**Open questions for the user.**
- GM11-1 **Commanding companions:** (a) the player commands every companion, as the old game [recommended: faithful];
  (b) also an option to let the AI command a companion.
- GM11-2 **A companion from a character file:** (a) kept, with the port's own character files (the new format; old
  files are not read) [recommended: the feature stays as M9 kept character files]; (b) dropped.

## GM12 The three new regions

**What the player gets.** Three regions where the old game has empty, walkable "Uncharted land": the rocky highlands
(`uncharted-highlands`, origin (2484, −56) in continent units: east of the Alder Vale, south of the Sunward Coast; the
continent's one rocky region, W20; draft 6's label band 15–30), the steppe (`uncharted-steppe`, (672, 1644): south of
the Alder Vale, east of Stormwake; 20–35) and the west-coast delta (`uncharted-delta`, (−4296, 5422): below the
Verdigris Jungle, above Duskpine, west of the Saffron Expanse; 50–65); each with a name, an identity, terrain, a level
band, towns, roads and connections, enemies, encounters, forage, music and battle stages, "fully implemented rather than
serving as empty filler" (task 8, section 3). The bands are only the draft's proposals (`8_Map_Rework/README.md`).

**What the old game has.** The three as ground only (`uncharted: true`; no settlements, roads, exits, sites or
encounters; the header "Uncharted land"; the last region's music kept, W28; Forage off, P11); their places in
`continent-engine.js`'s `ORIGINS` and the baked land.

**What the engine provides.** Everything GM4, GM6, GM9, GM10 and GM11 built.

**What is new.** Everything (W27), within what decisions already fix: task 14's harpy dungeon is in the rocky highlands
at Levels 50–56 "whatever band the region is given" (DG65); the carriage may cross the uncharted land (W33); forage
tables needed (P11).

**The art.** Their terrain if GM4-4 left it here; their towns (map models and rooms from the kits); their new enemy
types' models (made in GM9 when GM4-4 is (a), here otherwise; GM12-2); their battle stages; two themes each (GM12-3).

**How it is checked.** Their rules are the ported ones (GM4, GM10, GM11), checked with this content; the content
against its approved drafts; models and stages against Blender; music against Chromium renders of the composer and by
ear.

**Depends on / needed by.** Depends on GM4, GM6, GM9, GM10, GM11. Needed by GM16 (the harpy dungeon).

**Weight: L.** No old code; three regions' drafts and their art (the size follows the drafts).

**Open questions for the user.**
- GM12-1 **How the regions are made:** (a) drafted by the port region by region (name, identity, terrain, band, towns,
  roads, enemies, encounters, forage, music) and approved, as task 14's content is (DG2) [recommended]; (b) the user
  gives the names and identities first, the port drafts the rest.
- GM12-2 **New enemy types** (asked with GM9's questions instead if GM4-4 is (a), since GM9 drafts them then): (a)
  where a region's identity needs them, drafted by the revamp's rules (task 8 section 3 allows "new enemy types if
  needed") [recommended]; (b) existing enemy types only.
- GM12-3 **Their music:** (a) a region theme and a battle arrangement each, written in the composer's notation and
  approved by ear [recommended: every region has its own]; (b) the neighbours' themes reused.

## GM13 The Church of Talos and Hearth Home, and the boss machinery

**What the player gets.** The **Church of Talos** in the Stormwake Hills: challenge Tankina, Disciple of Talos (Level
44, with two Apostles of Talos; Gorthok the Thunder Boar joins in her second phase; her gathering storm fed by lightning
spells); once she has fallen, Respec Classes and Respec Stats (12,000 silver each; fulfilled "other" requirements stay
fulfilled) and the **Storm Reliquary** (6 Church of Talos lightning staffs and wands, 8 lightning spells with a 10 %
chance of one over-tier, the Stormhide Cuirass always on offer for 20 Platinum; Spell Augmentation at 10 % off, 40 % on
Lightning and Wind, SP24). **Hearth Home** on Cinder Isle's volcano: challenge Ornn, God of the Forge (Level 48, with
his Vulcanic Ram at the anvil; his reforging tempers spoiled by damage); once he has fallen, the forge: Hearthforge
weapons (5) and armour (4) at 10 % off, buying nothing, and the only anvil that reforges masterworks (15 % off). Both
can be fought again. With them comes the frame of every boss fight after: a badge and an overlay over the field for the
boss's rule; phases at HP thresholds that change names, stats, looks and music and play a scene; an intro once when the
battle starts (never when a saved battle resumes; Skip and Escape end it; none with combat animations off; a still card
under reduced motion), the AI waiting until it ends; the boss's theme.

**What the old game has.** `church-of-talos.js` (294: its window, made by the script, the challenge, the respecs, the
store `#church-store-modal` with two menus), `hearth-home.js` (64), `shop.js`'s `HEARTH`; `combat.js`'s `stormState`
and `reforgeState`; the profiles `tankina`, `talos-apostle`, `ornn`; the scenes `tankina`, `tankina-phase`, `ornn`,
`ornn-phase`; the themes Tankina, Tankina Chosen, Ornn, Forge Erupts; the rooms `stormChapel` (`church`) and Hearth
Home's, which borrows the arms hall (`hearth`, a placeholder); `worldState.defeatedSiteBosses`. Screens 34–36. The
machinery every boss shares: `combat.js` 1074–1230 (phases, `startNextPhase` 1074–1111, `playPhaseIntro`, the cutscene
hooks), the mechanics' turn-start hooks in `advance`, `stageLook` (6130–6163) and the snapshot's badge slots;
`boss-intros.js`'s player and shared scene helpers (`talosScene`, `forgeScene` among them) with the timelines in
`styles.css` 1623–2310; `Combat.musicTheme` (the boss's theme and its phase switches). Balance: each old boss was tuned
with a simulator (`jr-sim.js`, `ju-sim.js`, `ny-sim.js` and others, run by `rt-launch.sh`) kept only on the user's PC in
`~/.cache/stat-fick-build/claude-work/`, with the targets in `../old-game/Docs/Notes.md` (for example Juila prepared at
Level 25 about 47 % wins).

**What the engine provides.** `Docs/Scene.md`'s sequencer (`Sequence` records at `world/cutscenes/<name>`,
`SequencePlayer` with `Skip`, `Blocking`, cues and the reduced variant), the bar for the port's cutscenes
(`Docs/Scene.md`, "How it works", step 7), M10-3 (the game binds skipping and the combat-animations setting), Q20
(`Blocking`, for the game's AI hold); GM6's shop window and anvil; GM7's Augmentation.

**What is new.** The boss machinery and the game's cutscene path, built here first on two bosses with one mechanic and
one phase scene each, so GM14 and GM16 only add bosses; the port's own boss simulator on the engine's rules, running
parties (GM11) against a boss with the old targets (GM13-1). Task 3's Hearth Home room and window (placeholders: "a
plain panel like the Church's"); the two bosses and their retinues revamped by GM9's rules, their Level-100 class sets
drafted with them (applied by GM15); bosses immune to instant death (SP46); Augmentation at the Reliquary (SP24); the
boss clips (their rig's set plus phase change, transform, an intro entrance and their specials, "asked per boss at port
time", Decisions for M7).

**The art.** Tankina (and her chosen look), the Apostles, Gorthok (quadruped), Ornn (and his erupting look), the
Vulcanic Ram (quadruped); the badges and overlays (the storm, the tempers); 4 cutscenes to Ji'ruan's bar (a detailed
stage composed like the old SVG scene, acting clips per cutscene, particles, the card, sound per GM13-2), each approved
through renders before its tests; the storm chapel and Hearth Home's own room; the two site windows (GM13-3). Script
families from GM9, `interiors`, and `cutscene-stages`, which starts here. Checked under `ReferenceLimits.DetailedStage`.

**How it is checked.** The mechanics' rules and the phases against the old game's boss checks (with the revamped numbers
as the reference where they differ); the profiles' rules through the test client on comparable states; the cards against
Chromium captures of the old cards (`make-cutscene-references.sh`, as `JiruanCardTests`); stages, clips and models
against Blender; win rates by simulation (GM13-1); the respecs, the Reliquary and the forge through the test client and
`shop-rework-browser.js`'s Hearth Home rules; the windows against captures added to `Reference/game-capture.js`. Not
checkable against the old game: the 3D scenes, and the balance after the revamp.

**Depends on / needed by.** Depends on GM5, GM6 (the shop window, reforging), GM7 (Augmentation at the Reliquary), GM8
(respecs), GM9 (the retinues' families, the revamp's rules) and GM11 (parties for the simulations). Needed by GM14
(the machinery, the cutscene path, the simulator), GM15 (the class sets) and GM16 (all three, and the reforge of
masterworks with the smithing materials, DG26).

**Weight: XL.** About 360 old lines of the sites, about 300 of their mechanics and profiles and about 250 of the shared
phases, intros and boss music; two bosses, three retinue models, four cutscenes, two rooms; the simulator.

**Open questions for the user.**
- GM13-1 **The bosses' difficulty after the revamp:** (a) the old targets per boss (`../old-game/Docs/Notes.md`),
  measured again by simulation with the revamped classes, spells and bosses [recommended]; (b) new targets set by the
  user per boss.
- GM13-2 **Sound in the cutscenes** (the old ones have none of their own; the proof has three first-guess sounds):
  (a) every cutscene with its own sounds on its events and a music cue, as the proof [recommended]; (b) silent as the
  old game's, the boss's theme only.
- GM13-3 **The site windows** (both plain panels in the old game; Hearth Home's a placeholder): (a) each with an
  illustrated scene of the site above its buttons, in the rooms' look [recommended]; (b) plain panels for both,
  restyled.

## GM14 Bosses and Great Hunts

**What the player gets.** The eight bosses that stay Great Hunts (with Saffron and Ool moving to dungeons, DG33, and
Ornn to Hearth Home since task 3): Great Imp Ji'ruan (Level 18), Juila the Grand Forest Matriarch (22), Nygheir the
Wounded (23), Zik Lun, the armored Hulk (24), the Deep Sea King (32), Gaian, Avatar of the Land (53), Ur-Mat, the False
Dreamer (90), Anatemis, the White Star (100); each offered on a guild's board in its region and fought at a zone marker,
paying 2.4 × and 3 × the Reputation, with rematches from the trophies. Each fight has its rule, shown as a badge and an
overlay: Juila's cycle of seasons and her seasonal spirits; Nygheir's old wounds torn open by criticals; Zik Lun's
warband orders (Charge, Hold the Line, Regroup), war drummer and war horn; the Deep Sea King's royal duel; Gaian's
regrowing heart cores and the Severed state; Ur-Mat's seven dream laws; Anatemis's star trials (Mirror Scales, Sunfall,
Supernova) over three forms. Phases, intros and themes run on GM13's machinery.

**What the old game has.** `combat.js` 2957–3916 (the mechanics' states and turns: `seasonState`, `warbandState`,
`royalDuelState`, `heartCoreState`, `dreamState`, `starState`, `setOpenWounds`/`tearOldWound`; hooks in `advance`,
`endTurn`, `hurt`, `applyCombatDamage`, `logFall`), `AI_PROFILES` (5850–6007; here `jiruan`, `juila`, `juila-court`,
`nygheir`, `deep-sea-king`, `gaian`, `ur-mat`, `anatemis`, `troll-guard`, `zik-lun`: 10 of the 17); the enemy files'
blocks `seasonCycle`, `oldWounds`, `warband`, `royalDuel`, `heartCores` (with Gaian's ally Jungle Core),
`dreamLaws`, `starTrials`; `guild.js` `greatHunts` (148–225: id, enemy, level, region, zone, roster, description);
`boss-intros.js` (716; 12 of its 19 scenes: the intros `jiruan`, `juila`, `nygheir`, `zik-lun`, `deep-sea-king`,
`gaian`, `ur-mat`, `anatemis` and the scenes `nygheir-phase`, `ur-mat-lucid`, `anatemis-golden`, `anatemis-zenith`;
timelines in `styles.css` 1623–2310); the boss themes in `audio.pak` (Ji'ruan; Juila; Nygheir and Nygheir the Scarred;
Zik Lun; Deep Sea King; Gaian and Gaian Severed; Ur-Mat and Lucid Change; Anatemis White, Golden and Zenith). Checks: a
check file per boss among the 49; their balance targets in `../old-game/Docs/Notes.md` (GM13).

**What the engine provides.** GM13's boss machinery, cutscene path and simulator on the engine's sequencer; the proof
(Ji'ruan's intro, test data only: its stage, its clips, the card against Chromium in `JiruanCardTests`).

**What is new.** The eight bosses and their retinues revamped with the general task by GM9's rules (DG33 says it of
Saffron and Ool: "revamped with every enemy in the port"), their Level-100 class sets drafted with them (applied by
GM15); bosses immune to instant death (SP46); the boss clips: "their rig's set plus phase change, transform, an intro
entrance and their specials (asked per boss at port time)" (Decisions for M7), asked as each boss's model is made; the
models at the user's bar (2026-11-05).

**The art.** Eight boss models and their looks (Juila's four seasons, Nygheir scarred, Gaian severed, Ur-Mat lucid,
Anatemis's three forms), their retinues and spirits from the rosters (Jungle Core, the first model of the core family;
Juila's Rime Wolf, Sunhorn Stag, Blossom Sprite and Harvest Wisp …), the mechanic overlays and badges (seasons, orders,
dream laws, star trials), 12 cutscenes to Ji'ruan's bar (a detailed stage composed like the old SVG scene, acting clips
per cutscene, particles, the card, sound per GM13-2), each stage and its clips approved through renders before its
tests. Script families: GM9's rig families, the core family (new here) and GM13's `cutscene-stages`. Checked under
`ReferenceLimits.DetailedStage`.

**How it is checked.** The mechanics' rules against the old game's boss checks (with the revamped numbers as the
reference where they differ); the profiles' rules through the test client on comparable states; the cards against
Chromium captures of the old cards (`make-cutscene-references.sh`, as `JiruanCardTests`); stages, clips and models
against Blender; win rates by GM13's simulator with the targets of GM13-1. Not checkable against the old game: the 3D
scenes, and the balance after the revamp.

**Depends on / needed by.** Depends on GM13 (the machinery, the cutscene path, the simulator), GM11 (the boards that
offer Great Hunts, the trophies, parties), GM9 (the retinues' families, the revamp's rules), GM5, GM7 and GM8. Needed by
GM15 (Anatemis unlocks Limitless) and GM16 (the core family for Ool's Phylactery).

**Weight: XL.** About 600 old lines of mechanics and profiles and 450 of intros; 8 bosses and about 10 retinue models
at the bar, 12 cutscenes.

**Open questions for the user.**
- GM14-1 **Ji'ruan's proof stage** (approved 2026-11-05 with the stand-in figure): (a) reused with the real models
  swapped in and approved again [recommended]; (b) remade from scratch with the others.

## GM15 Limitless and the endgame

**What the player gets.** After Anatemis's first defeat a notice announces Limitless and a button appears under the
hero's name. On: Levels to 200 (5 points and +1 to each attribute a level, no class points); enemies below the hero's
level rise to it and every enemy gets +25 % attributes; bosses also gain the class levels of a Level-100 character;
masterwork drops doubled; gear above Level 100 can be worn; the Exotic rarity drops (Fletcher, Tough-Out, Winged Hussar,
Cherubim, Connected Balancer, made with Exium). Off: the hero and the companions are their Level-100 selves (everything
above kept aside), gear above 100 goes into the pack. The sheet's numbers glow in the Exotic wave while it is on. An
Exotic reward for beating Anatemis at Level 200.

**What the old game has.** `limitless-engine.js` (108, pure, `LimitlessRules`: the record `{ unlocked, active, kept,
spent }`, `cap`, `keepAside`/`restore`, `noteSpent`, `scaleEnemy`, `BOSS_CLASSES` at 77–89 for 11 bosses at exactly 100
class levels, L29, `wearable`), `limitless.js` (87); spread: `forge-engine.js` (`MAX_LEVEL` 200, `levelCurve`, Exium,
`EXOTIC_IDS`), `combat.js` (scaling at spawn, doubled drops, `rollExoticExtras`, the Exotics' effects, no EXP at the
cap), `app.js` (caps, level-up, `needs-limitless` borders), `guild-information-engine.js` (odds), `styles.css`
(`.item-rarity-exotic`, `body.is-limitless`, the wave). Screen 15. Check: `limitless-browser.js` (14 entries, 100
mutations).

**What the engine provides.** Nothing special: the rules sit on GM2, GM3, GM5, GM8, GM9 and GM14.

**What is new.** `BOSS_CLASSES` reworked for the new classes (C2: "the bosses' `BOSS_CLASSES` of task 7, L29"): the
sets drafted with each boss's revamp in GM13 and GM14 applied here (Saffron's, Ool's and the new bosses' come with them
in GM16, "drafted with them"); task
13's Limitbreaker (+25 % to all stats while Limitless is on, NC18; red; offered by the hooded man from Level 100, NC7);
the Limitless button and notice, placeholders (L1). Task 14's dungeon enemies scale through the same `scaleEnemy`, and
its Exotic drops like the five (DG36): GM16.

**The art.** The button and the notice (GM15-2); the Exotics' models are GM3's; the wave and the sheet's look while on
are real and kept.

**How it is checked.** `LimitlessRules` in Node; `limitless-browser.js` rule by rule; the reworked `BOSS_CLASSES`
against the sets approved with their bosses.

**Depends on / needed by.** Depends on GM2, GM3, GM5, GM8, GM11, GM13 and GM14 (the class sets; Anatemis). Needed
by GM16.

**Weight: M.** About 200 old lines and their spread; the bosses' class sets redrafted; two UI pieces.

**Open questions for the user.**
- GM15-1 **The moment Limitless unlocks:** (a) the notice, as the old game [recommended: faithful]; (b) a short
  cutscene after Anatemis's first defeat, then the notice.
- GM15-2 **The Limitless button and notice** (placeholders, L1): (a) designed in the game's UI with the Exotic wave as
  their accent, shown as captures for approval [recommended]; (b) the user describes them first.

## GM16 Dungeons

**What the player gets.** Task 14 (`Docs/Port-Tasks/14_Dungeons.txt`, 966 lines; DG1–DG78): twelve dungeons at fixed
places, each an entrance site with a safe radius and an Enter button (DG72), a mandatory warning before entry naming the
case that applies (DG6, DG70), a 3D overview of the dungeon (DG69) with the party moving from stage to stage,
multi-stage battles from designed pools of groups with Empowered elites in later stages and levels rising to the range's
top (DG7, DG8), Camping Supplies between stages (20 % to all, the fallen included, at most one after each cleared stage,
DG11), Run even from the boss with the stage refought (DG13), defeat penalties (without a Delve −75 % money and −50 % of
eligible items, waking at the dungeon at 1 % HP and 0 % Mana; with a Delve −35 % and −10 %, the nearest town;
DG14–DG16), the boss hidden as a silhouette and "???" until met (DG68), unlimited replays (DG17), the Delve quest
(DG18–DG21), Waylight and the Emergency Rope (DG22, DG23), Nocturne Keep's quest from Ravenwatch opening the keep to
non-vampires (DG52, DG55, DG56, DG59, DG60, DG62–DG64, DG66, DG73, DG74), the smithing materials as items that cut a
reforge to them by 75 % (DG26, DG29–DG32), three masterworks and an Exotic, eleven throwing and other consumables in the
General Store (DG39, DG41), a music theme per dungeon (DG34). The dungeons:

| Dungeon | Levels | Stages | Enemies | Boss | Place |
| --- | --- | --- | --- | --- | --- |
| Goblin Cave | 4–8 | 4 | Goblins | Hobgoblin Leader | Alder Vale, Forest |
| River Portal | 10–14 | 5 | Water elementals | Deep Sea Apparition | Stoneveil Foothills, River |
| Abandoned City | 20–24 | 6 | Demons and impids | Imp Lord Ru'manon | Mirrormere Basin, Lake Shore |
| Ruined Castle | 25–30 | 8 | Goblins | Goblin Paladin | Stormwake Hills, Thunder Hills |
| Mountain Portal | 38–40 | 5 | Earth elementals | Great Mountain Mole | Cinder Isle, Lava Fields |
| Ancient Prison Ruin | 42–46 | 9 | Goblins | Goblin King and 2 Goblin Champions | Crimson Divide, Sunscorched Escarpment |
| Great Void Rift | 62–68 | 6 | Demons | Void Lord Firadialin | Bloodstone Coast, Giant Rock Spires |
| Ancient Pyramid | 65–70 | 10 | Devils | Saffron (moved, DG33) | Deep Desert, Pyramid Sanctum |
| Shattered Castle | 74–78 | 8 | Undead | Ool (moved, DG33) | Ancient Battlefield, Silent Westfield |
| the harpy dungeon (named in its draft) | 50–56 | in its draft | Harpies (DG53) | in its draft | the rocky highlands (DG65) |
| Nocturne Keep | 66–70 | in its draft | Vampires (DG52) | a vampire lord (DG58) | Nocturne Keep, Bloodstone Coast |
| the dragon lair (named in its draft) | 85–92 | in its draft | Dragons (DG52) | in its draft | Stoneveil Summit, Cloudbreak Ridge (DG61) |

**What the old game has.** Nothing of the task (DG1); for it: Saffron (Level 66; his infernal sacrifice of his three
Great Devil Servants, protector, reaver and archer; the profiles `saffron` and `devil-servant`; his intro `saffron`) and
Ool (Level 78; his phylactery, he turns to dust and re-forms until it is broken; the Phylactery of Ool and the
Gravebound Legionnaire with him; the profiles `ool` and `grave-guard`; the scenes `ool` and `ool-phylactery`; the themes
Saffron, Ool, Ool Mortal), both Great Hunts until now; what the old game has and lacks for the task (no staged battles,
a defeat costing nothing, Camping Supplies on the map only, the stun rules, the General Store's random stock, goblins
only up to Level 17): `Docs/Engine-Plan.md`, "Decisions for task 14, Dungeons", its opening facts.

**What the engine provides.** Everything built before: the battle (GM5) extended to stages, the 3D renderer for the
overview (DG69), the map's sites (GM4), the dungeon-only enemy types (GM9, DG9), the guild's quest slots (GM11), the
boss machinery, cutscene path and simulator (GM13), the core family (GM14), Limitless (GM15), the reforge (GM6,
GM13).

**What is new.** The whole task, after its port-time drafts are written and approved (DG2; the list in
`Docs/Engine-Plan.md`, "Where task 14 stands (2026-11-12)"): the stages of the nine (61: 4 + 5 + 6 + 8 + 5 + 9 + 6 + 10
+ 8) and of the three extra dungeons, each stage's pool of groups (DG7) from GM9's dungeon-only types and the existing
ones whose level fits (DG9); the three extra dungeons' names and identities; the exact spots on the 3D map (DG5); the
eleven new bosses (the eight the task names counting the Goblin Champion, the keep's vampire lord, the harpy and dragon
bosses: mechanics, intros, themes, models, DG27, DG28, DG58; balanced by simulation on the Great Hunts' curve, DG27;
their drops, DG35; their Level-100 class sets for Limitless); the three masterworks (Great Mountain Mole, Void Lord
Firadialin, Goblin King; their kinds per boss, DG37) and the Exotic (a kind none of the five is, DG38); the eleven
throwing items' numbers (DG43) and the four extra ones (DG39); the smithing materials (which drop where, their Forage
odds and prices, DG29–DG32; the reforge menu's mark for a carried material, DG26); the numbers left open (the Delve
chance per board, DG18; the time a stage and a camp take, DG12; Ravenwatch's quest text and reward, DG55). Saffron and
Ool moved "unchanged" (DG33) and revamped with every enemy; an active Saffron or Ool hunt in the port's own saves
withdrawn with a note. The Dungeon Delver's and Throwing Specialist's two counts (GM8's classes).

**The art.** Twelve 3D overviews (GM16-3); the dungeons' battle stages (GM16-2); eleven new boss models at the bar with
their intros; Saffron, his three servants, Ool, the Phylactery (the core family) and their three scenes; the three
masterworks, the Exotic, the eleven consumables and the material items; the entrances on the map; 23 new themes (12
dungeons, 11 bosses). The dungeon-only enemy types' models are GM9's. Script families from GM3, GM4, GM6, GM9, GM13
(`cutscene-stages`), GM14 (core) and `overviews`.

**How it is checked.** Every DG rule a test with its id; the drafts' content as approved; the new bosses' win rates by
GM13's simulator on the Great Hunts' curve (DG27); models, stages, overviews and cutscenes against Blender after the
user's approval; the themes against Chromium renders of the composer and by ear. Nothing here has an old-game reference
but Saffron's and Ool's rules.

**Depends on / needed by.** Depends on everything before it (GM1–GM15): the task builds on tasks 2, 3, 5, 7, 8, 9, 10
and 13 and on the enemy revamp (`Docs/Port-Tasks/README.md`, "The order"; DG4, DG9, DG26, DG36, DG40, DG65, DG69).
Needed by GM17.

**Weight: XL.** No old code but Saffron's and Ool's; the largest drafted content: 12 dungeons, at least 61 stages, 11
bosses with 14 cutscenes in all (11 intros and Saffron's and Ool's three), 23 themes, about 20 new items and the
materials.

**Open questions for the user.**
- GM16-1 **The order of the work:** (a) one dungeon at a time from draft to art, the Goblin Cave first [recommended: the
  first dungeon shows every system working before the next draft]; (b) every dungeon's draft first, then the building.
- GM16-2 **The dungeons' battle stages:** (a) each dungeon its own stage set, varied from stage to stage
  [recommended]; (b) shared sets by kind (cave, ruin, portal, keep, lair).
- GM16-3 **The overview** (DG69: a 3D scene, the party moving from stage to stage, the task's information over it): (a)
  a diorama of the dungeon's stages seen from above at an angle, the party's marker moving along, the boss's stage dark
  with its silhouette until met [recommended]; (b) a top view like the map's.

## GM17 Balance and the class tests

**What the player gets.** A game whose numbers hold together after the reworks: classes, spells, enemies, bosses, races
and items measured together, and every class feature shown to work.

**What the old game has.** The balance method: simulators of the bosses (on the user's PC, `claude-work/`), the
measurement behind task 12's curve (every enemy spawned against plain heroes of Levels 1–100, SP8), the old suites'
regressions (`../old-game/Docs/Notes.md`, the "Regression" entries).

**What the engine provides.** Headless battles on the port's own rules (GM5 on), which the simulations run on.

**What is new.** C32's duty (3): "after the port, every class feature is tested so that it works as intended (every
level of all 256 classes, passives and skills included)": each class level taken by a test hero and each passive and
skill exercised in a battle set up for it, a report per class. The balance pass: the class numbers ("proposals to tune
when built"), the race review (GM2-4), zone difficulty by level, the bosses' and dungeons' win rates, item and spell
power on task 12's curve. The user's playtest.

**The art.** None of its own.

**How it is checked.** The simulations and the class tests are themselves checks; each change they lead to goes to the
user (GM17-1) and its milestone's tests.

**Depends on / needed by.** Depends on everything before. Needed by GM18.

**Weight: XL.** 256 classes' levels (from 4,388 lines of features) exercised; the whole game simulated.

**Open questions for the user.**
- GM17-1 **How balance changes are approved:** (a) the simulations report each imbalance with a proposed change, the
  user approving them in batches [recommended]; (b) the user playtests first and names what to change.
- GM17-2 **The playtest build:** (a) the packaged game for Linux and Windows at the start of GM17 [recommended: the
  user plays what will ship]; (b) from the repo on the PC only.

## GM18 The release

**What the player gets.** Stat Fick on Statventure in its downloads: `stat-fick-<v>-linux.tar.gz` and
`stat-fick-<v>-windows.zip` with the game's program, `Statventure.Game.Data.dll` holding every kind, `packs/` (`audio`,
`ui`, `models`, `world`, `data`), `classes/`, `NOTICE.txt` and `licenses/`; the editor's download knowing the game's
kinds; icons for both programs.

**What the old game has.** Its executables (`Stat Fick.AppImage`, `Stat Fick.exe`, not in git); the icon
`objects/stat-fick-icon.svg` and `desktop/icons/`.

**What the engine provides.** M12's pipeline (`Tools/Scripts/publish.sh`, `check-packages.sh`, `end-to-end.sh`,
`release.sh`), the editor (M11) loading the game's data library (M12-9), the version scheme (Q1).

**What is new.** The stand-in removed everywhere (the sandbox as the published game, its `Data/` folder, the stand-in
library); `classes/` in `Publish.cs` and the two listings with its `cover` line (`Docs/Packaging.md`); `models.pak`,
`world.pak` and `data.pak` committed (`Docs/Assets.md`, "Content still to come"); the editor's previews for the game's
kinds; `end-to-end.sh` extended to play (a new hero, a battle, a save and a load, a mod of the game's kinds made in the
editor and loaded in the game); icons with a Windows build or resource step (M12-5); the check that no stand-in visual
is left (GP-3); the Windows check (`Docs/Engine-Plan.md`, "Windows check", items 1–9); the first release on the user's
word (`release.sh`).

**The art.** The two icons (GM18-1).

**How it is checked.** `check-packages.sh` and its self-test, `end-to-end.sh` as extended, the full suite on both
drivers and on Windows.

**Depends on / needed by.** Depends on everything.

**Weight: M.** Scripts and checks; two icons; the Windows run.

**Open questions for the user.**
- GM18-1 **The icons** (M12-5: none until the port's art pass): (a) new icons for the game and the editor made from the
  game's art, shown for approval [recommended]; (b) the old game's icon for the game and a new one for the editor.
- GM18-2 **The editor and the game's kinds:** (a) the generic form for every kind, as M11 built it [recommended: no
  editor code per kind, M11-2]; (b) special views for the largest kinds (classes, enemies, dungeons).

## Cross-cutting

### The data library and the editor

`Statventure.Game.Data` (M12-9, closing G3) holds the game's record kinds and their rules; the game links it, the editor
loads it from the game's folder into its own load context once per run, and the packer references its project by name,
so `pack`, `update`, `extract`, `dump` and git's diff driver know the game's kinds. It ships as one file beside the
game's executable in the game's download only (the editor's download has no copy; a folder counts as a game when it
holds `packs/` and the library), and its version must equal the editor's (`dev` matches `dev`; Q18). GM1 makes it from
the stand-in and changes `GameLibrary.RepoProject`.

Every kind follows "What M11 leaves for the port": a record (`[Record]`, `[Field]`, `[Subtype]`) named as the engine's
kinds are (dotted, lowercase, area first), marked for the editor (`[Kind(display name, folder)]`, `[Help]`, `[Unit]`,
`[Limits]` only for limits the game's own check enforces, a new limit being a question for the user (G4), `[Choices]`,
`[Colour]`, `[Bulk]`, `[References]`), its rules as `[Check]` methods that the game's loaders run too (G4, G5): the old
game's `validateEnemy` and the class checks become `[Check]`s. The kinds arrive with their milestones: GM1 the save
(`save.game`; the character file is its JSON form and `settings.json` isn't a record, corrected 2026-11-13); GM2 races
and the actor record; GM3 the forge's tables (makers, materials, weapon and armour types, caster parts, runes, masterworks, natural weapons), artifacts, key items; GM4 regions (places, roads,
zones, exits, sites), the continent's land and the terrain (the engine's `terrain` kind); GM5 the statuses and
reactions; GM6 shop sizes, consumables, ingredients, recipes; GM7 spells and the Augmentation attributes; GM8 the
classes (loose JSON in `classes/`, GM8-1); GM9 enemies; GM10 encounter and forage tables, battle biomes; GM11 quest
types, rewards, recruit names; GM13 the bosses' phases and mechanics' parameters; GM14 Great Hunts; GM16 dungeons,
stages, materials. Which of the old
game's in-script tables become records and which stay code (the AI profiles are code in the old game) is decided in
each milestone's design doc, and asked where it changes what a modder can edit (the asking rule's "data formats").

The packs (Decisions for M9): `data.pak` for the tables, `world.pak` for terrain, map data and cutscenes, `models.pak`
for models, clips, state machines, stages and particle effects, `audio.pak` and `ui.pak` as now, `classes/` loose.
Data from the old game is read from it as the music is (`make-music-data.sh` reads the old scripts in Chromium), or from
the pure modules in Node, by a script per kind whose output is committed; content from the drafts is written from the
drafts. The repo's script-made packs switch to `packer update` once an editor-made entry is committed into them (P30).
The editor's views for the game's kinds: GM18-2.

### The save and what goes in it

The old save (`app.js` line 1843) holds: `worldState` (the clock, time speed, region and position, distances, a pending
encounter, visited regions, defeated site bosses, the guild's reputation, completed counts, standard quest, Great Hunt,
boards, notice, artifact offers and challenge, completed and avoided hunts, rematches, information bought; the
carriage), `shopState` (each visited settlement's stock, `apothecary:<id>`, `church-of-talos`, `hearth-home`,
`institute-of-magic`), `combatState` (a deep copy of a running battle, every actor's full record), the race, appearance,
level, EXP, points, `limitless`, `alchemy` (`known`, `brewed`, `foraged`, `keenHarvestReady`), the requirement counters
(`trapsUsed`, `unarmedAttacks`, `summonSpellsUsed`, `summonSpellsUsedByElement`, `bloodSupplies`, `itemsSold`,
`soloSurvivorVictories`, `multiKillCombats`, `classKills`, `raceKills`, `classProgress`, `defensiveActionsUsed`,
`flawlessVictories`, `enemyCount`), `editingEnabled`, `darkMode`, HP, Mana, allocations, currencies, permanent bonuses,
the inventory (ids, repeated for quantity), favourites, equipment, known spells, class levels, skill states, up to three
companions (each a full record), recruit offers, pending class spell choices, the stats.

The port's save adds: Spell Augmentation's ranks and weavings per hero and companion (task 12, `Spell-Augmentation.md`
§10); tasks 10, 13 and 14's new counts and the Moonfang; the smithing materials; the dungeon state (the dungeon, its
stage, the stages cleared, bomb stuns used, Waylight, bosses met; the battle and stage resumed on load: "Decisions for
task 14", "Following from the answers"). It doesn't hold Dev Mode's state or the theme, which are game settings in
`settings.json` (GM1-13, GM1-4), nor the random state (GM1-5). Its form is M9's: a binary versioned record behind the
`SVSV` header, named slots with a `.bak`, a damaged slot set aside and the player asked, written on every request by
the background writer. Each milestone adds its part as a new version with its `Upgrade()` and committed fixtures, so a
save made in GM2 loads in GM18 (the port's own saves are upgraded; the old game's never). No old save's size was ever
measured; GM5 measures the save with a battle in it.

### Dev Mode

The old game turns it on for a new character and saves it with the character; it unlocks editing the attributes,
bonuses and the purse, the EXP form, "Restore to full", the spell and item catalogues, the Combat test screen, levelling
without EXP, another race once one is chosen and the enemy-only races, the appearance step skipped, companions taught any
spell and companion dev tools (Force encounter isn't Dev Mode's; corrected 2026-11-13, `Docs/Game/Foundation.md`). In the
port it is switched in the settings screen and the old ✦ button is disabled (GM1-3), staying as the brand mark, always
lit (Q11 of GM1's detail round); it is off by default and turned on in the settings when wanted (GM1-12), and its state is
kept in `settings.json`, the same for every character, as the theme's is (GM1-13, GM1-4). The tests reach the same
powers through the game's API, not its screens.

### Randomness and seeding

The old game draws gameplay randomness from unseeded `Math.random` (combat rolls, loot, encounters, stock, recruits,
boards); its checks stub `Math.random`. Its art randomness is seeded (interiors per settlement, map noise,
`stableLookVariant`), its music uses mulberry32 (ported in M8), the capture harness seeds the shop's stock with
mulberry32 `0x5eed`. The port draws every gameplay roll from one random service (GM1), seeded per run, its state not
saved (fresh draws on every load, GM1-5); a test seeds it or hands it the exact draws the old check's stub gives, so a
battle can be replayed against the old game's result draw for draw; art keeps its per-object seeds; sound effects vary
from `SoundEffects.Seed` (M8). Nothing reproduces `Math.random` itself. Its generator (the manager, 2026-11-13, GM1-P11):
`xoshiro256**` seeded through SplitMix64 from one 64-bit seed, a draw's top 53 bits as a double in [0, 1).

### The real-time clock

The old clock: `advanceAmbientTime` every 250 ms, 0.5 game minutes a real second idle, 36 travelling at 1× (0.5× to
2×), stopped by battles and by a hidden page (`map.js` pauses travel on `visibilitychange` and `pagehide`);
`advanceWorldTime` regenerates 5 % of HP and Mana a game hour, resets daily skills and restocks shops. GM4 ports it on
the engine's event loop (`SetInterval` with DOMTimer's clamp, `Docs/Scripting.md`), the game listening to the
`visibilitychange` the engine sends for a minimized window (a hidden document is throttled as Chromium throttles it);
GM4-5 asks whether time keeps passing off the map. Tests drive the clock by hand, never by real time (as the script
fixtures use 0 ms timers).

### Sound

The music is the engine's already (M8: 55 themes, 34 instruments, 21 percussion voices, 7 rooms in `audio.pak`, approved
by ear on 2026-11-02); the game ports `desiredTheme` (region theme, else `alder-vale`; in battle `combat@<region>` if it
exists, else `combat`; a boss's theme, else `combat`) in GM4 and GM5, and the crossfade in GM4. New themes: two per new
region (GM12-3) and 23 for the dungeons and their bosses (GM16), written in the composer's notation, checked against
Chromium renders of the old composer (which plays any score) and approved by ear. Sound effects: none in the old game;
the port adds them (2026-11-03); the engine's `SoundEffects` (patches as data at `audio/effects/<name>`, `Actor.Sounds`
for clip events, seeded variation, voice limits of 4 per patch and 32 in all; no stop yet: the small task "A stop for
sound effects"). When and which (GM1-6): each milestone makes its own aspect's sounds, their list asked with its
questions and approved by ear (GM4-8, GM5-4, GM6-2, GM7-3, GM9-5, GM10-2, GM13-2); GM1's own are the interface's,
buttons and windows opening and closing as well as game events (GM1-11): eight, in paper and ink (Q13, Q14 of GM1's
detail round), the effects volume at 60 % on a first start (Q22); no music plays in GM1 (Q15); the first sound the user
asked for, a rune
slotted, belongs to GM7's tower. The proof cutscene's three sounds still wait for the user's ear (small task), GM13-2.

### The old game's checks: the known failures and how the test client is reused

The latest regression (`../old-game/Docs/Notes.md`, 2026-11-11: 162 runs, 148 passing): 12 fail on the code before task
11 too, the known ones: `adventure-improvements`' `runMapImprovementChecks` and `runAdventureImprovementsChecks`,
`bloody-mary`, `combat-lifecycle`'s two entries outside its suite, `music`, `newc`, `settings-companions`, `shop`,
`talos-edge`, `trap`, and the combat suite, which stops at `talos-edge`'s legacy bonus-choice assertion; 2 are
intermittent: `adventure-improvements`' `runGreatHuntRewardChecks` (a "reading 'reputation'" error, 3 runs in 4 on the
old code too) and `map-speed` (timing). The causes recorded ("Regression baseline"): `bloody-mary` asserts the removed
40 % crit cap; `settings-companions` looks for "Hit chance", now "Accuracy"; `adventure-improvements` presses S for the
removed Spell Shop; `music`, `newc` and `shop` assert stale UI; `talos-edge` and the combat suite are the Barbarian
failure (level 7 now grants both Strength and Vitality +5 %, so the Church's prompt for older saves that never recorded
the choice is unreachable); `trap` fails too, its cause not recorded; `combat-lifecycle` works only inside its suite;
`coast-lich-bosses`' Ool checks fail about 1 run in 12.

How the port treats them: the old game's code is the reference, not a stale check, so a stale expectation is not
mirrored; the Barbarian's legacy choice concerns old saves, which the port never reads, and the Barbarian is reworked by
task 10; `trap` and the intermittent ones are looked into by the milestone that ports their rules (GM4 for travel
speed, GM5 and GM6 for traps, GM11 for the hunt reward), and where the code itself looks wrong the case is asked, not
fixed in silence. The old game is not changed for the port (`../old-game/CLAUDE.md`). The test client is reused as the
port's oracle ("How each step is finished", item 3): one entry at a time with `--script` and `--call`, the stage off,
a second copy of `combat.js` with its internals exposed, the results brought out through the save hook, on the PC or
under Xvfb.

### The game-screen references

The engine's game-screen tests compare with 22 captures (11 states at 1 and 1.65: the sheet, classes, combat,
companions, inventory, map, passives, settings, shop, skills, spell book: `Golden/game-*.png`, `game-ui.json.gz`,
`game-ui@1.65.json.gz`, `Reference/scaled/game-layers@1.json` and `@1.65.json`) and keep their own copy of the
stylesheet (`Tests/Statventure.Tests/Data/Game/styles.css`, 3915 lines), all from the repo's first commit `f45ca7a`. The
old game's `styles.css` is 4184 lines on `main` (`Docs/Engine-Plan.md`'s "At port time" gives 4080, measured on
`a32c5cb` on 2026-11-06, before tasks 7–9 added the rest), and its markup and scripts changed with tasks 2–11. They are
captured again once at GM1's start, the stylesheet copy with them (GM1-1; `make-game-references.sh`, which also copies
the stylesheet, `make-scaled-game-references.sh`, `make-layer-references.sh`; on the PC or under Xvfb). Found on
2026-11-13 (`Docs/Game/Foundation.md`): today's stylesheet doesn't compile in the engine (`@property` at `styles.css` 3726
and `-webkit-text-fill-color` refused, `background-clip: text` ignored), so GM1's step 1a ports those first, and the
capture script needs changes for today's old game (step 1b; `Shop.open` needs a settlement); the old game is frozen
until the recapture is on `main` (the user, 2026-11-13). Screens the 11
states don't cover (the race picker, the classic battle view, the tower, the Apothecary, the guild's panels, the
encounter prompt, the Church, the Companions picker …) are added to `Reference/game-capture.js` by the milestone that
ports them; `Docs/Compositing-Port.md`, "Next", asks to come back to it when new screens are captured.

### Engine items the port will meet

Each is recorded in `Docs/Engine-Plan.md`, "Small tasks left from earlier milestones", or `Docs/Packaging.md`, Status,
and waits for the user's go, asked when its milestone reaches it:
- "The painter paints every row of a scroll container" (2,021 rows took 109 ms a frame): the inventory (GM3), the shops
  (GM6), the spell book (GM7), the classes (GM8).
- "Music switch during a fade-in": the region crossfade (GM4).
- "A stop for sound effects": GM1-6's sounds (GM1's own don't need it: each ends by itself, `Docs/Game/Foundation.md`).
- The CSS today's old stylesheet uses and the engine lacks (`@property`, `-webkit-text-fill-color`, `background-clip:
  text`): GM1's step 1a, before the recapture (found 2026-11-13).
- The 1.65 findings in UI the game uses (control labels a device pixel low, text grayscale in a composited box, beside a
  tall scroller or under a stacking context, a select's padding, a textarea's `line-height`, a positioned box with a
  z-index inside a scroller): the first milestone whose screen meets one (GM1 on).
- `flex: 1` with a 0 % basis in a column flexbox of indefinite height (`Docs/Packaging.md`, Status, step 6).
- "publish fails when the repo is reached through a symlink": GM1's step 2, which owns the build and publish scripts
  (moved from GM18 by the manager, 2026-11-13). The "Windows check", items 1–9: GM18 (GM1's step 11 rewrites the items
  the game changes).
- The proof's sounds by ear: GM13 (GM13-2); the by-hand checks (a)–(e): GM18 at the latest.

### The three decision-id collisions

Three prefixes mean two things in `Docs/Engine-Plan.md`: `G` (task 2's G1–G34 and the editor's G1–G6 of M11), `P` (task
9's P1–P31 and the editor's P1–P38), `M` (task 5's M1–M26, the milestones M0–M12 and their questions such as M10-2). The
docs always cite them with their source ("task 9's P11", "G2 of M11"), and so does this plan. `GP-n` and `GMn-k` are
free (checked 2026-11-12; "Decisions for the plan").

### What the port never does

- Import the old game's saves or character files (2026-10-16; Decisions for M9; C12): the old normalizers and their
  migrations (`normalizeCharacter`, `WorldRules.normalize`, `ForgeRules.migrateLegacyItems`, `LEGACY_ITEMS`,
  `RETIRED_ARTIFACT_IDS`, `RENAMED_ENEMIES`, `mapScale`, `continentVersion`, retired quest kinds, Ornn's hunt note, a
  save standing in water moved to land) and the tasks' old-save rules (G33, R15, A11, L17, W15) stay in the old game.
- Carry the browser-only parts: localStorage and its caches of the data files; the "Refresh file" and "Choose …json"
  buttons and the File System Access API handles kept in IndexedDB; the cache-busting query on `styles.css`; the
  `statfick://` protocol, Electron's menu and native dialogs (in-game UI instead, Decisions for M5); audio waiting for
  the first pointer press (a browser autoplay rule); `withActor`'s swapping of global variables; the 43 MP3 exports in
  `music/`; `music.js`'s 24 original scores (dropped in M8).
- Carry the dead parts: `#anatemis-cinematic`, `#boss-special-cinematic` and its three themes (storm-talos,
  false-dreamer, forge-god), which no enemy file reaches (`Docs/Scene.md`, "The old game's cutscenes (facts)"); the
  region transition prompt (never shown since task 8); `MapInk.finish`; the weapon and armour record parsers and the
  empty `itemCatalog.weapons/armor`; `classes/newc/` (drafts, never loaded); `RACE_FALLBACKS`.
- Ship the old 130 classes and 388 spells as content (GP-1): they stay in the old game as the reference for the rules
  the reworks keep.
- Change the old game (`../old-game/CLAUDE.md`: "Change it only when the user asks").

### Corrections to the survey

Found against the repo on 2026-11-12; the repo's figures are the ones used above.
1. `old-game/classes/newc/` holds 15 `.txt` drafts, a template and an `IGNORE/` folder of 6 files (one survey said "14
   drafts, an IGNORE file and a template").
2. The old game's `styles.css` is 4184 lines (the brief and `Docs/Engine-Plan.md`'s "At port time" give 4080, a count
   from `a32c5cb`; `Docs/Port-Tasks/README.md` gives 4184).
3. The old checks failing on the old code too are 12 entries (11 check entries in 9 files and the combat suite) and 2
   intermittent ones, by the latest regression (2026-11-11), not 11.
4. The class files hold 260 distinct `type` strings, 139 of them inside `effects` lists (the surveys gave 260 and 131,
   counting differently).
5. `ARTIFACT_SHAPES` draws 49 objects while the artifact records name 50 shapes and 76 glyphs (the surveys gave one
   figure each).
6. `Docs/Assets.md` still counts 129 class files and 169 enemy files (2026-11-02); the repo has 130 and 174. (Settled
   2026-11-12: Assets.md, "What exists", now adds today's counts beside its dated ones.)
7. Nygheir's file has no `phaseTransitions` entry (one survey listed him among the bosses with a phase transition); how
   his scene `nygheir-phase` is reached is GM14's to read in `combat.js`.

## What this plan leaves open

Every question below is about the game. Each milestone's are asked with the question tool when it begins (GP-6) and
answered in its section (GP-7); GM1's thirteen were all answered on 2026-11-12 ("Decisions for GM1": the eleven, then
GM1-12 and GM1-13, which came out of GM1-3's answer), and GM1's detail round (Q1–Q24 and Q16b) on 2026-11-13 ("GM1's
detail round"), so none of GM1's is left. The sound questions are asked with their milestones (GM1-6).

- GM2-1 The 72 looks: faithful 3D remakes / new looks per race / a character maker.
- GM2-2 The hero on the sheet: no figure / a live 3D figure.
- GM2-3 Palettes, light, lines, hatching: the engine's look with the old palettes / a new palette.
- GM2-4 Goliath and Human at low levels: in GM17's review / now.
- GM2-5 The three new seals: SVG like the six / all nine as 3D renders.
- GM3-1 Item models' detail: close-up / battle size / two models.
- GM3-2 Item pictures: live 3D / 2D icons / pre-rendered pictures.
- GM3-3 Armour on bodies: fitted per body / a few shared builds.
- GM3-4 The Transformer: kite shield / round copper shield.
- GM3-5 Masterworks and Exotics without a hint: designed by the port and approved / described by the user.
- GM4-1 The map's finish: with parchment frame, compass, scale, inked labels / labels only.
- GM4-2 Settlements on the map: each its own from a kit / two shared models.
- GM4-3 The hero on the map: a marker / a 3D figure.
- GM4-4 The new regions' land: designed in GM4 / all in GM12.
- GM4-5 The clock: as the old game / only while travelling, resting, camping.
- GM4-6 Fog over unentered regions: drifting cloud / blank parchment.
- GM4-7 The grabbing cursor: an inked gauntlet / an inked hand / the system's.
- GM4-8 Map sounds: a small set / none.
- GM5-1 Battle views: both / the 3D stage only.
- GM5-2 A battle on save or quit: resumed / dropped.
- GM5-3 Statuses and reactions: on the figure, icons and bursts / icons and log only.
- GM5-4 Battle sounds: the full set / hits, criticals, deaths.
- GM6-1 The rooms: live 3D / pictures per room and time of day.
- GM6-2 Town sounds: coins, anvil, cauldron, door / purchase and cauldron only.
- GM7-1 Spell effects: per element and kind / per spell.
- GM7-2 Tier 10 animations: short 3D vignettes / full cutscenes.
- GM7-3 Spell sounds: per element and kind / per spell.
- GM8-1 Class files: the engine's record JSON / the old shape.
- GM8-2 The red rarity: still red like the others / a moving wave / designs to choose from.
- GM8-3 The drafts' numbers: tuned in GM17 / tuned while built.
- GM9-1 The revamp's drafts: by family, sample then batches / by region / all at once.
- GM9-2 The revamp's difficulty: as the old game by simulation / free, checked in GM17.
- GM9-3 The Earth Spirit: left out / placed in a table.
- GM9-4 Enemy variants: shared models with parts and palettes / a model each.
- GM9-5 Creature sounds: a voice per family / none.
- GM10-1 Stage colours: muted like the figures / the old bright colours.
- GM10-2 Stage ambience: per biome / none.
- GM11-1 Companions: commanded by the player / an AI option too.
- GM11-2 Companions from character files: kept / dropped.
- GM12-1 The new regions: drafted by the port and approved / the user names them first.
- GM12-2 New enemy types there: where needed / existing only (asked with GM9's questions if GM4-4 is (a)).
- GM12-3 Their music: two new themes each / neighbours' themes.
- GM13-1 Boss difficulty: the old targets re-measured / new targets from the user.
- GM13-2 Cutscene sound: their own sounds / silent with the theme.
- GM13-3 The site windows: illustrated / plain panels.
- GM14-1 Ji'ruan's proof stage: reused and re-approved / remade.
- GM15-1 Limitless unlocking: the notice / a cutscene and the notice.
- GM15-2 The Limitless button and notice: designed with the Exotic wave, approved / described by the user.
- GM16-1 The dungeons' order: one at a time from draft to art / all drafts first.
- GM16-2 Dungeon stages: one set per dungeon / shared by kind.
- GM16-3 The overview: a diorama at an angle / a top view.
- GM17-1 Balance changes: proposed by the simulations, approved in batches / from the user's playtest.
- GM17-2 The playtest build: packaged for Linux and Windows / from the repo.
- GM18-1 The icons: new ones from the game's art / the old icon for the game.
- GM18-2 The editor for the game's kinds: the generic form / special views.

Questions per milestone: GM1 0, GM2 5, GM3 5, GM4 8, GM5 4, GM6 2, GM7 3, GM8 3, GM9 5, GM10 2, GM11 2, GM12 3, GM13 3,
GM14 1, GM15 2, GM16 3, GM17 2, GM18 2: 55 in all.

## Status

- **2026-11-12:** the plan written after the survey of the repo (docs only; branch `game-plan` from `29d110d`), with the
  user's GP-1–GP-4. The same day the user answered GP-5 (no early playable loop: the order made for implementation, and
  the milestones reordered for it), GP-6 (each milestone's questions asked at its start) and GP-7 (the answers recorded
  here, a pointer in `Docs/Engine-Plan.md`), then GM1's eleven questions ("Decisions for GM1": all as recommended but
  Dev Mode in the settings with the old button disabled, the user's own answer; the mod list with a check at start,
  which answers M11's G2; soft interface sounds), and after them GM1-12 and GM1-13, both as recommended (Dev Mode off
  by default; its state in `settings.json`, the same for every character): GM1's questions are all answered. No
  milestone started; GM1 starts on the user's go.
- **2026-11-13:** GM1 started on the user's go. Its design doc, `Docs/Game/Foundation.md` (docs only, the branch
  `gm1-design` from `2bd4da9`): GM1's section checked against the code (what moved is listed there, "Where the plan was
  wrong or has moved", and corrected here), the design, then the same day the user's detail round, complete (Q1–Q24 and
  Q16b, "Decisions for GM1", "GM1's detail round"; seven answers not the recommendation: the top bar trimmed to name and
  status, the ✦ always lit, no music in GM1, Quit and About on the start screen with Continue greyed, no Reload, effects
  at 60 %, the general message for an old character file; Q24, raised by the answers, as recommended: "Load Character…"
  in the slot list) and the manager's 19 decisions; the old game frozen until the recapture is on `main`; the design and
  twelve steps (1a, 1b, 2–11) in four waves final, nothing open. Nothing ticked; no step started.
- **2026-11-13, GM1 step 3 (the icons' outlines):**
  - The 28 icons are in `Assets/UI/icons/`.
  - The style is the user's pick, "Engraved", not the recommendation (Woodcut).
  - ★ and ✸ have the heavier lit line, the user's answers at stop 2. The set was approved.
  - The icon face draws its code points before any font (the manager; GM1-P10 changed in that point).
  - The details are in `Docs/Game/Foundation.md`, "How it works", step 3.

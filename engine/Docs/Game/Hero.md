# The hero (GM2)

The game's second milestone (`Docs/Game-Plan.md`, GM2): the character sheet (the eight attributes, levelling and EXP,
HP, Mana, Armor and Evasion, the purse, the equipment slots, the Field tools rail), the actor record every combatant
shares and its calculations, the 28 races' records and the nine playable races' passives, the race carousel and a
character maker in place of the old appearance step (GM2-1, GM2-6, GM2-7), a live 3D figure of the hero on the sheet
(GM2-2), the game's new look (GM2-3, GM2-8), the humanoid rig family with its clip set, its parts and the first
humanoid's approval, and the nine race seals as 3D emblems (GM2-5). Written on 2026-11-15 (docs only, the branch
`gm2-design` from `8c7d07d`), checked against the code of the old game and the engine that day; the user's detail round
(Q1–Q42) and the manager's decisions on the proposals (GM2-P1–GM2-P16) were recorded the same day and the design and
the steps made final. **The design is decided; nothing is open; no step started.**

Paths are relative to `engine/` unless they start with `old-game/`. "The old game" is `old-game/` on `main`
(unchanged since GM1's recapture, `cde21a4`).

## Decisions

### The user's decisions for GM2 (2026-11-15)

Asked by the manager with the question tool on the user's go for GM2, the five of the plan and three follow-ups their
answers raised, at once; in full, with the options as asked and the ones not taken, in `Docs/Game-Plan.md`, GM2,
"Decisions for GM2". In short below; what an answer leaves open is a question of the detail round, not an answer of
this doc's.
- **GM2-1, not the recommendation: a character maker.** The player builds a look from parts (hair, face, skin, build)
  instead of picking one of eight. (Not faithful 3D remakes of the 72 looks, the recommendation; not new looks per race.)
- **GM2-2, not the recommendation: a live 3D figure on the sheet**, beside the equipment slots, wearing the equipped
  gear; the sheet's layout changes to make room and is shown as captures first. (Not no figure, as the old game.)
- **GM2-3, not the recommendation: a new palette**, proposed together with the first humanoid. (Not the engine's
  approved look with the old game's palettes.)
- **GM2-4: the Goliath and the Human at low levels** kept as the old game, reviewed in GM17's balance pass. (Not now.)
- **GM2-5, not the recommendation: all nine seals as 3D emblems**, the six older ones too, shown for approval. (Not the
  three new ones as SVG in the six's style.)
- **GM2-6, not the recommendation: presets and a random button**: each race's eight old looks as starting points the
  player can then change, plus a button that rolls a random look from the race's parts. (Not presets only; not none.)
- **GM2-7, when the look can be changed: only when the hero is made.** (Not any time from the sheet; not later in towns
  for a price.)
- **GM2-8, not the recommendation: everything**: colours, light, line widths and hatching are all open again; candidates
  are shown on the first humanoid; the engine's earlier models (Ji'ruan's stage) follow the new look later. (Not colours
  only; not colours and light.)

### The user's word on the 3D models (GP-8, 2026-11-15)

Said to the manager right after the eight answers, and recorded as GP-8 in `Docs/Game-Plan.md`, "Decisions for the
plan", because it holds for every milestone with 3D art: **"I want you to note that i want to assign a lot of effort
into the 3D Models. I really want them to look very good".** How this doc plans for it is the design's own reading,
marked as such ("Design", "Meeting GP-8"); what "very good" means was asked (Q1–Q6 of the detail round), not assumed.

### The detail round (Q1–Q42, the user, 2026-11-15)

Asked by the manager with the question tool in ten rounds of four, the options as this doc prepared them, then Q40, then Q41 and Q42, which the answers opened (in
full with each question's facts and options: "The questions as asked"; recorded in `Docs/Game-Plan.md`, "GM2's detail
round", too). Twelve answers are not the recommended option: **Q1, Q2, Q9, Q18, Q19, Q21, Q22, Q23, Q28, Q31, Q32,
Q37.**
- **Q1, not the recommendation: heroic, realistic** proportions, about 7.5 heads, smaller heads and longer limbs, the
  races differing by the same ratios. (Not the old six-head proportions; not big heads.)
- **Q2, not the recommendation: stylised** faces and hands: simple eye shapes, no face bones, mitten hands (a thumb and
  one block). (Not modelled faces with face bones and fingers; not modelled faces with mittens.)
- Q3 Sculpted masses: hair as modelled locks, clothes with modelled folds, seams, hems, belts, buckles.
- Q4 A mood board first (about six small renders on a rough figure; one or two picked; then full candidates).
- Q5 A full sheet per model (turnaround, close-ups, beside the old sprite, clips as strips and videos, the playing view).
- Q6 Three candidates for the look and for the first humanoid, two for later families, as many rounds as wanted.
- Q7 Today's renderer rules with any numbers; a new rule only when the user asks for one.
- Q8 A human man and woman together, first.
- **Q9, not the recommendation: a sword and shield with a chain shirt** as the first humanoid's sample gear.
- Q10 The clips in four batches. Q11 The earlier models take the new look when used (Ji'ruan's stage at GM14). Q12 The
  look in the game's data, a record a mod can restyle.
- Q13 The race fixes its body and defining traits; the player chooses the rest. Q14 Male and female, each its own body per
  race. Q15 A few modelled builds per race and sex (slender, average, strong). Q16 A list of modelled faces (asked with the
  note that Q2 left face bones out).
- Q17 Swatches per race. **Q18, not the recommendation: one plain outfit per race and sex, colours only.** **Q19, not
  the recommendation: a large set, about twice (a).** Q20 Each option named; the presets keep the old labels.
- **Q21, not the recommendation: presets as a list; random rolls everything; no locks.** **Q22, not the recommendation:
  the Half-Beast's ears, tail and fur chosen freely, no beast choice.** **Q23, not the recommendation: the Merfolk's and
  the Goliath's marks optional, "none" among the patterns.** Q24 Small head pieces as choices, hidden under a helmet.
- Q25 Name, race, maker, sheet. Q26 The sheet opens only with a race and a look. Q27 Dev Mode's old powers. **Q28, not
  the recommendation: a carousel, one race at a time with a large figure.**
- Q29 A paper doll (asked with a sketch: the figure in the middle of the equipment section, the hands' slots at its sides,
  armour and the two artifacts below). Q30 The name as the heading, race and level under it. **Q31, not the
  recommendation: the figure idles in a fixed ¾ view, nothing else.** **Q32, not the recommendation: a framed portrait
  panel with a dark backdrop.**
- Q33 The figure in its own clothes before GM3, the slots inert. Q34 The whole rail, each tool disabled until its
  milestone. Q35 A medallion in relief. Q36 Pictures where small, a live, slowly turning emblem only for the race being
  chosen.
- **Q37, not the recommendation: nine new motifs, each proposed as a sketch first.** Q38 A small new set of sounds,
  approved by ear. Q39 Parts and presets are data a mod can add to.
- Q40 (the manager's follow-up, as Q16's "about six faces" met Q19's "about twice"): about six faces per race and sex,
  108 in all, the large set doubling the other parts.
- Q41 (opened by Q28 and Q31): the carousel's and the maker's figures as the sheet's, each idling in a fixed
  three-quarter view. Q42 (opened by Q23 with T3 and T4): the Goliath's and the Merfolk's presets wear marks, a rune
  pattern on each Goliath preset and a scale pattern on each Merfolk preset; any hero can still choose "none".

### The manager's decisions on the proposals (GM2-P1–GM2-P16, 2026-11-15)

Every one (a) as proposed ("The proposals as made"), with three notes:
- **GM2-P7:** the swatches stay part of `race`; no `look.swatches` is built. Q39 named parts and presets; whether a mod
  can add swatches apart was not asked, and is a question a later need may open.
- **GM2-P10:** the manager checked: the Flatpak is Blender 5.3 (its commit of 2026-11-13). Blender 5.2 is pinned through
  `$STATVENTURE_BLENDER`; the first art step fetches the official 5.2 Linux build into the build cache and verifies that
  the engine's existing 56 render references still reproduce with it before any new art.
- **GM2-P16, amended for Q28 (c) with Q36 (a):** a screen keeps only the live views it shows (the sheet one figure; the
  maker one figure; the carousel one figure and the one live emblem), each released when its screen hides; the memory and
  the per-frame cost measured on the GPU and on lavapipe and reported by the step that builds them. With Q32's dark panel
  the sheet's figure stands on no ground that takes a shadow ("Design", "The sheet").

**GM2-P17, the seven sounds' wiring (the manager, 2026-11-15, on step 12's readings, which apply Q38 and GM1's one sound
per action):** a preset picked in the maker plays `maker-choice`; `point-spent` is a free point spent, so Dev Mode's
direct edits keep GM1's `ui-press` and a Dev Mode level up plays `level-up`; one `point-spent` per point however fast, the
patch's four voices taking over the oldest, no rate limit, nothing once the "+" is disabled; a race chosen or changed
(Dev Mode's too) plays `race-chosen`; nothing on hover. The seven weigh 5 in an action, above GM1's event sounds' 4
(`Ui/GameSounds.cs`, made by step 12 with the manager's leave). Steps 7 and 8 wire them as "How it works", step 12, lists.

### Questions from the steps (Q43–Q44, the user, 2026-11-15)

Raised by step 1 (the `race` kind's fields; the docs didn't answer them) and asked by the manager with the question tool
the same day; both the recommended option. Recorded in `Docs/Game-Plan.md`, "GM2's detail round", too.
- **Q43 The races' order.** One entry per race at `data/races/<id>` holds no order (a pack lists its entries by path:
  bear, dark-elf, demon …), while the carousel shows the races in `races.json`'s order and the old recruits' random picks
  (GM11) index the playable races in it. (a) An `order` number on each race, the old game's order written as 10, 20, …
  280, the same number by id, so a mod's race can sit anywhere among them and the order is data. *Recommended.* (b) A
  separate list entry naming the ids in order (a mod adding a race replaces the whole list; two such mods collide). (c)
  The order in the game's code, a mod's races after them by id. **Answer: (a).**
- **Q44 The emblem.** `normalizeRaceDefinition` keeps the seal's path (`icon`: `objects/race-human.svg` for the nine with a
  seal, null for the enemy-only, which show ✦); the emblems' pictures and models are step 10's and have no paths yet. (a)
  `emblem`, a name ("human", "dark-elf" …; none: no emblem, the ✦), step 10 placing its pictures and its model by that
  name. *Recommended.* (b) No field until step 10 adds it (race version 2). (c) A path to a picture in `ui.pak`, filled
  once step 10 makes them. **Answer: (a).**
- The manager's, on step 1's report (a test fixture's sample text, not a choice about the game): the `settings-mods`
  screens' broken mod holds two races its checks refuse, so the row shows the game's real kind and a real refusal in the
  editor's words, still wrapping onto two lines ("How it works", step 1).

### Earlier decisions that bind GM2

- **The look of the port** (User decisions 2026-10-16): characters, enemies and items are real-time 3D models made in
  Blender, drawn with cel shading and ink lines in the game's art style, with full animation sets; battles keep the
  fixed orthographic side view; screens are built in C# on the engine's UI; saves start fresh; classes stay `.json`,
  everything else goes into packs.
- **Decisions for M6** (2026-10-30), the renderer's rules: Blender 5.2 renders as the reference (cel shading, Freestyle
  ink lines), compared like the UI with Chromium; ink lines of a fixed width on screen times the device scale;
  characters, enemies and items receive no shadows (only scenery does); MSAA 4×; Vulkan 1.3 GPUs from about 2016 at
  60 fps in 1080p; `.glb` models; cel shading in two tones; hatching in shadow tones; glow from emission; colours as
  palette slots; every 3D view a `surface` element, rendered when something changes, with picking; linear blend
  skinning, 4 influences. **GM2-3 and GM2-8 reopen the numbers of the look** (the palette, the light, the line widths,
  the hatching); which of M6's rules the new look may change besides its numbers is the detail round's (Q7).
- **Decisions for M7** (2026-10-31): the humanoid clip set (idle and stunned loops; advance, return, flee; one-handed
  and two-handed swings, thrust, dagger stab, staff or wand strike, unarmed punch, off-hand swing; bow, crossbow; cast,
  skill, item, drink, meditate; guard and evade, entered then held; hurt, critical hurt, stumble; death, victory); no
  root motion; clip events as pose markers; state machines as data; NLA layering and fades; clips at 60 fps, sampled;
  **clips shared by bone names within a rig family whose models "share bone names and differ in proportions (races,
  sexes, builds)"**; gear: rigid items on `socket.<slot>` bones, bending clothing skinned to the wearer's bones, "made per
  body type"; secondary motion keyed, no simulation; **no shape keys: "Everything moves by bones"**; **model approval:
  the first humanoid "with gear and its full clip set, shown as renders and a playing view" before the rest of that
  rig**.
- **Decisions for M9 and M10:** records with frozen past versions, `Upgrade()` and committed fixtures; the packs
  (`models.pak` for models, clips, state machines; `data.pak` for tables); figures are `Actor`s on scene nodes in a
  `SceneView`; the user's requirement of 2026-11-05 that "the boss and enemy models are as detailed and high-quality as
  the cutscene scenes" (the stand-in figure is not the bar), and the bar's way of approval (Blender renders first, then
  the tests; `Docs/Scene.md`, "The bar for the port's cutscenes").
- **M11 and M12-9:** the game's record kinds live in `Statventure.Game.Data`, editable in the editor with no editor code
  when marked (`[Kind]`, `[Help]`, `[Unit]`, `[Limits]`, `[Choices]`, `[Colour]`, `[References]`, `[Check]`); the game
  refuses what the editor refuses (G4, G5); a new limit is a question for the user (G4).
- **The plan's** GP-1 (the reworks built in; the old game the reference for what they don't change), GP-3 (GM2 closes
  with its final art), GP-4 (every model regenerable from its Blender script), GP-6, GP-7, GP-8.
- **GM1's** (`Docs/Game/Foundation.md`): GM1-5 (fresh randomness on every load; the random state not saved), GM1-6 (GM2
  makes its own sounds, their list asked with its questions and approved by ear), GM1-8 (new and changed screens built
  from the old game's panels in its Gothic ink theme, shown as captures before their tests), GM1-9 (English), GM1-12 and
  GM1-13 (Dev Mode off by default, switched in the settings, the same for every character), Q2 (the name asked before
  the sheet; "GM2 puts the race picker after it"), Q5 (the character's page an empty parchment page until GM2), Q11 (the
  ✦ the brand mark only), Q15 (no music until GM4), **Q32 (the Field tools rail's room not kept in GM1: "GM2 brings the
  rail and its room together")**; the manager's GM1-P3 (the development-only `stand-in.item` goes when GM2's first real
  kind with `[Kind]` arrives), GM1-P5 (GM2's part of the save is `save.game` version 2 with its `Upgrade()` and
  fixtures), GM1-P7 and GM1-P8 (the markup in the program, a sheet per screen part in `ui.pak`), GM1-P17 (the screens'
  Chromium fixtures), GM1-P19 (generated files at merges).
- **Task 11** (Decisions for task 11, T1–T17, 2026-11-11): the races are built in the old game and carried over (T1);
  their racial classes come with GM8. For the looks: Half-Beasts "one race whose looks vary by beast, one shared racial
  trait" (T2), wolf, fox, bear and lynx (T14); Merfolk on legs on land, "with fin ears, scales and webbed hands, so
  armour and boots fit as everyone's" (T3); Goliaths "a head taller than humans and heavy-built, with giant-blooded
  marks (stone-grey skin, rune-like markings), using the same gear" (T4); the old seals and looks of the three are
  placeholders to remake (T5); the claws count as unarmed (T13). The rules T8–T15 are the old game's and stay.
- **The old saves are never imported** (2026-10-16; C12); the old appearance record `{ gender, variant }` is not read.

## What exists (checked against the code, 2026-11-15)

Nothing of GM2 yet: the game's character page is GM1's top bar over an empty parchment page
(`Game/Statventure.Game/Screens/Sheet/SheetScreen.cs`), the save holds a name only
(`Game/Statventure.Game.Data/Saves/SaveGame.cs`), and no humanoid model, look or race record exists.

### The old game: the sheet and its rules

- **Constants** (`old-game/scripts/app.js`): `LEVEL_CAP` 100 (2), `BASE_ARMOR` 5 (3), `BASE_EVASION` 10 (4); the eight
  attributes with their labels, short names and colours (`statDefinitions`, 285–294: Strength STR `#c7683f`, Vitality VIT
  `#bb7b4b`, Mind MND `#8c6f83`, Dexterity DEX `#738f88`, Intelligence INT `#68849a`, Wisdom WIS `#858b6c`, Speed SPD
  `#9c7c58`, Luck LCK `#ae6f73`); 10 each to start (297); the purse silver, gold, platinum (296), a new hero's 0, 5, 0
  (299); the five equipment slots `primaryHand`, `offHand`, `armor`, `artifact1`, `artifact2` with their labels (304–310).
- **The formulas** (428–445): max HP `max(0, floor((1.5 VIT + STR/2 + MND/2) × 1.25))`; max Mana `max(0, floor(MND +
  INT/2 + WIS/2))`; Evasion `(10 + 1.5 DEX + 0.5 LCK + 0.5 WIS) / 2`, floored where the sheet uses it (1771); a current
  attribute `floor(base × (1 + percent / 100))`. JavaScript doubles throughout, `Math.floor` and `Math.round` at set
  points.
- **The values' chain:** the base an attribute shows is the stored value plus the race's modifier, plus, for a race with
  `all-stats-flat-per-level-up`, that amount times (level − 1) (`raceStatModifier`, `displayedBaseStat`, 1084–1095);
  the current attributes (`currentStatsUncached`, 1196–1211) add the artifacts' flat amounts to the base and sum the
  percentages of the permanent bonuses, the artifacts (three kinds), the classes, the race and the battle, then the
  battle's `statValue`; a weapon with `turn-and-speed-lock` (Peace and Balance) counts only Speed's increases (1192–1201).
  Max HP (`calculateMaxHpWithArtifacts`, 1213–1222): the battle's maximum in a battle, else the formula plus the
  artifacts' and the race's flat HP, times one plus the artifacts', classes', race's and worn armour's HP percentages,
  floored. Max Mana (`calculateMaxManaWithEffects`, 1224–1230) the same with the race's and artifacts' flat Mana and the
  classes' and artifacts' percentages. Armor and Evasion (`updateDerivedValues`, 1762–1837): base Armor 5, base Evasion
  the floored formula; modified Armor 5 plus the equipment's (`calculateEquipmentDefense`, 1298–1325, GM3's items);
  modified Evasion `max(0, floor((base − the armour's debuff + flat Evasion) × (1 + Evasion % / 100)))`; then each times
  one plus its permanent percentage (the bonus, the classes', the race's, the battle's), floored, plus the classes' (and
  for Evasion the race's) flat amounts, then the lowest of the equipment's, classes' and race's caps and the battle's
  value. The sheet shows Base, Modified and Current for each (`index.html` 191, 202) and the formulas as text (158, 173,
  196, 207). HP and Mana are clamped to their maxima once the race catalogue is ready (1784–1788).
- **Levelling** (`CombatRules`, `old-game/scripts/combat-engine.js` 89–108, a pure module): the EXP a level needs is an
  enemy of that level's EXP times `sameLevelVictories` (3, 3, 4, 4, 4, 5, 5, 5, 6, 6, 6 to Level 11, then `round(7 + 17 ×
  t^1.5)` with `t = min(1, (level − 12) / 87)`); an enemy's EXP is `ceil((level + 1) × 100 / share)`, the share 5 at Level
  1, 6 at Level 2, then `min(12, 8 + floor((level − 3) / 10))`. The cap is `LimitlessRules.cap` (100, 200 with Limitless;
  `limitless-engine.js` 10, 29). **Level up** (`app.js` 2122–2139): refused at the cap and, outside Dev Mode, below the
  target; the level +1, the target's EXP taken off (not below 0), 5 free points, a class point while the level is 100 or
  less (`perkPoints`, GM8's to spend), every attribute +1, then half of max HP (unless the race disables healing) and half
  of max Mana restored, floored and capped (`healResourcesOnLevelUp`, 2029–2035). The sheet's texts: "CAP 100", "/ N EXP"
  or "MAX LEVEL", "N EXP to reach Level L", "Level cap reached · 100", "Dev-mode level up enabled · N EXP target"
  (1795–1819); the Level icon ♜ flashes (`has-level-up`) when the EXP is there (1824); the button is disabled at the cap
  or, outside Dev Mode, below the target (1823).
- **Free points and respec:** a "+" stepper spends a free point when one is left (the attribute +1, `manualAllocations`
  +1, `LimitlessRules.noteSpent`, which counts only above Level 100); otherwise the steppers and the attribute fields work
  only in Dev Mode (1696–1713, 1726–1734). Respec (2102–2114) gives back every free point spent and clears Limitless's
  count of points spent; disabled while none is spent (1725). The church's class respec is GM13's.
- **HP and Mana by hand:** the sheet's − and + buttons with an amount field change current HP and Mana for everyone
  (`index.html` 148–154 and 163–169 carry no `data-dev-only`; `adjustResource`, `app.js` 2008–2019, refuses healing for a
  race with `healing-disabled`); "Full HP", "Full Mana" and "Restore both to full" are Dev Mode's (`index.html` 136, 155,
  170; `app.js` 2021–2027, 2058–2064).
- **Dev Mode's powers on the sheet** (`updateEditingControls`, 1718–1748): the attribute fields and "−" steppers, the
  permanent bonuses of the attributes and of Armor and Evasion (1685–1694, 2081–2090), the purse ("Enable Dev Mode to edit
  money", 1738–1743; 2070–2078), the EXP form (2037–2046), levelling without EXP, the `[data-dev-only]` controls (the
  Combat test button is GM5's, the catalogues' "+" GM3's and GM7's). Since GM1-3 and GM1-13 Dev Mode is the setting, not
  the character's.
- **The record** (`defaultCharacter`, 1327–1374): the hero's fields the sheet uses are `raceId` (null), `appearance`
  (null), `level` 1, `exp` 0, `availablePoints` 0, `perkPoints` 1 (`startingPerkPointGranted`), `enemyCount` 0 (the
  battle's, read by Mud Runner), `currentHp` and `currentMana` (the maxima of the default attributes, 31 and 20:
  ⌊(15 + 5 + 5) × 1.25⌋ = 31; "30" here until step 2's oracle read the old game's own value, `new-hero.json`),
  `manualAllocations`, `currencies`, `permanentBonuses`, `permanentDefenseBonuses`, `equipment` (five nulls), `stats`;
  the rest belongs to later milestones (`Docs/Game-Plan.md`, "The save and what goes in it"). The page holds them as
  about 45 top-level variables (1574–1616); combat evaluates any actor by swapping them (`withActor`, `combat.js` 143),
  and companions and enemies are records of the same shape. The appearance record is `{ gender: "male" | "female",
  variant: 0–3 }` (`normalizeAppearanceRecord`, 1377–1379); `stableLookVariant(text)` (1381–1385) gives companions one of
  four looks from their name's hash, "without storing a random roll".
- **The page** (`index.html` 90–299, the sheet's part of `main.shell`): the hero section ("RPG / PERSONAL RECORD",
  "Character *sheet*", a line of copy, the hidden Limitless toggle, the race seal button `#race-select-button` with its ✦
  fallback and the race's name, 90–104); the EXP card (106–131); the vital resources, Level, HP and Mana (133–176); Armor
  and Evasion, "Guard and *grace*" (178–210); the Field tools rail `aside.main-tools` (212–248: the purse, the companions'
  list, the eight tool buttons Map, Combat, Spell book, Inventory, Companions, Classes, Skills, Passives with their icons
  ⌖ ⚔ ✧ ◫ ♟ ♜ ✦ ◇); the equipment, "Worn and *carried*", two rows of slots (250–282); "The eight *facets*" with the free
  points and Respec (284–299) and the attribute cards built by `createStatCards` (`app.js` 1640–1716). The rail's toggle
  "☰ Field tools" and its shade sit outside the page (21–22); `old-game/scripts/interface.js` (66 lines) makes the rail a
  drawer under 840 px (`matchMedia("(max-width: 840px)")`, 7): opened by the toggle, closed by its shade, Escape or a
  tool's click, Tab kept in it.
- **The old checks on the sheet's rules:** `small-changes-browser.js` `runLevellingCurveChecks` (102–118: the curve; the
  victories to reach 12, 30, 50 and 100, 51, 186, 388 and 1265; the EXP targets at 1, 12, 50, 99: 120, 1141, 5100,
  20016); `map-browser.js` 15–22 (the Level icon's flash, no false alert in Dev Mode); `limitless-browser.js` 425–486 (the
  level-up button above 100, GM15's); `new-races-browser.js` (below).

### The old game: the races

- **`old-game/objects/races.json`:** 28 races, an array, each `{ id, name, playable, icon, statModifiers, passive: { id,
  name, condition, description, effects } }`. Nine playable: Human, Elf, Dark Elf, Dwarf, Hobbit, Lizardman, Half-Beast,
  Goliath (in that order, then the 19 enemy-only ones) and Merfolk (the file's last); each playable one has its seal
  `objects/race-<id>.svg`, the enemy-only ones `icon: null`. The enemy-only: goblin, impid, wolf, bear, griffon, orc,
  demon, devil, elemental, giant, dragon, troll, siren, harpy, satyr, lamia, undead, treant, dryad.
- **Loading** (`app.js` 2926–2954, 3155–3194): the file must be an array; each race normalized (the id slugified from
  `id` or `name`; the name trimmed and not empty; `playable` true unless `false`; every attribute's modifier a finite
  number, 0 when missing; a passive with a name required, its condition "Always active" by default, its effects the
  objects in its list); duplicate ids refused. The localStorage cache and "↻ Refresh file" are the browser's (not
  carried, `Docs/Game-Plan.md`, "What the port never does").
- **The nine playable races' modifiers and passives:**

  | Race | STR | VIT | MND | DEX | INT | WIS | SPD | LCK | Passive (its effects) |
  | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
  | Human | +2 | +1 | −3 | −1 | +1 | 0 | −1 | +1 | Rulers of the World: +2 max HP every fifth level (`max-hp-per-levels` 2, interval 5) |
  | Elf | −2 | −2 | +3 | +1 | +1 | +1 | 0 | −2 | Sylvan Heritage: +1 max Mana every fifth level, spells 3 % cheaper (`max-mana-per-levels` 1, interval 5; `spell-cost-percent` 3) |
  | Dark Elf | −1 | −2 | +2 | +2 | +1 | +1 | +1 | −3 | Sylvan Heritage (the same) |
  | Dwarf | +2 | +3 | −2 | 0 | −2 | −1 | −1 | +1 | Mountainborn: Armor +8 % (`defense-permanent-percent` armor 8) |
  | Hobbit | −3 | −2 | 0 | +2 | 0 | 0 | +1 | +2 | Nimble Stature: Evasion +15 %, no Heavy Armor (`defense-permanent-percent` evasion 15; `equipment-restriction` armor, heavy) |
  | Lizardman | +1 | 0 | −1 | +2 | −2 | −1 | +3 | 0 | Mud Runner: +2 Evasion per enemy in combat (`defense-flat-per-enemy` evasion 2) |
  | Half-Beast | +1 | +1 | −1 | +1 | −2 | +1 | +1 | −2 | Natural Claws: unarmed attacks are "Claws", +1 base power every 10 levels (`unarmed-name`; `unarmed-power-per-levels` 1, interval 10) |
  | Goliath | +3 | +3 | −2 | −2 | −1 | 0 | −2 | +1 | Giant's Blood: max HP +10 %, Armor +5 %, Evasion −10 % (`max-hp-percent` 10; `defense-permanent-percent` armor 5, evasion −10) |
  | Merfolk | +1 | +3 | +1 | +2 | −2 | 0 | −1 | −2 | Fishman Physiology: 20 % less Water and 10 % more Fire spell damage (`incoming-spell-damage-percent` water −20, fire +10) |

- **The 22 effect types** the 28 passives use (counted from the file): `defense-permanent-percent` (18 uses),
  `incoming-spell-damage-percent` (11), `max-hp-per-levels` (4), `max-mana-per-levels` (4), `spell-cost-percent` (3),
  `all-stats-flat-per-level-up` (2), and once each `equipment-restriction`, `defense-flat-per-enemy`, `unarmed-name`,
  `unarmed-power-per-levels`, `max-hp-percent`, `stat-permanent-percent`, `defense-cap`, `turn-start-restore-percent`,
  `spell-damage-mana-burn-percent`, `incoming-spell-miss-chance`, `debuff-immunity`,
  `conditional-stats-percent-vs-higher-max-hp`, `healing-disabled`, `spellcasting-disabled`, `death-save-decay`,
  `spell-power-percent`. **The sheet's side** (`calculateRaceEffectsUncached`, `app.js` 1101–1164, into the totals of
  `emptyRaceEffects`, 1058–1078) reads 14 of them and `trap-limit-flat`, which no race uses: `max-hp-per-levels` and
  `max-mana-per-levels` (`floor(level / interval) × amount`), `max-hp-percent`, `unarmed-power-per-levels`,
  `unarmed-name`, `spell-cost-percent` (with or without a `namePrefix` or `element`), `trap-limit-flat`,
  `healing-disabled`, `spellcasting-disabled`, `debuff-immunity`, `stat-permanent-percent`, `defense-permanent-percent`,
  `defense-flat-per-enemy` (times `enemyCount`), `defense-cap` (the lowest wins), `equipment-restriction` (category and
  type); `all-stats-flat-per-level-up` is read by `raceStatModifier` (1088): 15 of the 22 on the sheet. **The battle's
  side** reads a race's effects through `racePassiveEffectsOf` (`combat.js` 174–180) and the passives' list
  (`passiveEffectsOf`, 1837): the other seven, `incoming-spell-damage-percent` (1818, 1823), `turn-start-restore-percent`
  (978), `spell-damage-mana-burn-percent` (4136), `incoming-spell-miss-chance` (2845),
  `conditional-stats-percent-vs-higher-max-hp` (239), `death-save-decay` (1923) and `spell-power-percent` (the dryad's;
  where the battle reads it from a race is GM5's to trace), and `healing-disabled` and `debuff-immunity` again (178–180); those are GM5's. A race's id counts in combat too
  (`actorRace`, 331: race-targeted effects of artifacts and classes, GM3's and GM8's).
- **Equipment restrictions** (3019–3036): an item a race's restriction names (category and type) can't be worn, and
  choosing a race takes such items off (`enforceRaceEquipmentRestrictions`; "· Restricted equipment removed").
- **Accents** (`raceAccent`, 2956–2958): Human `#c7683f`, Elf `#728577`, Dark Elf `#77607e`, Dwarf `#8b684e`, Hobbit
  `#858b6c`, Lizardman `#66806a`, Half-Beast `#9a6a3f`, Goliath `#7d7a72`, Merfolk `#4c9a8a` (and goblin, impid);
  `#c7683f` otherwise; the seal button's and the list's `--race-accent`.
- **The old checks:** `new-races-browser.js` (182 lines, six entries: `checkNewRaceData` the three races' playability,
  seals, stats, passives and accents; `checkGiantsBlood` its +10 % HP on the sheet and in a companion's battle values;
  `checkNaturalClaws` the claws as a 1-AP unarmed attack and their growth, the battle's side; `checkNewRaceLooks` four
  named looks per sex, the Goliath taller, the Merfolk on legs with fin ears, the Half-Beasts' tails, claws and beast
  ears; `checkNewRaceRecruits`; `checkNewRacePicker` the list's seals), 22 mutations in the old game, all caught
  (`old-game/Docs/Notes.md`, "New Races"). Found then (Notes 1086–1089): at Level 40 with the default allotted stats a
  human has 48 HP, a Goliath 40, "a balance point for the port's race review" (GM2-4: GM17's).

### The old game: the race picker and the appearance step

- **Opening it:** the seal button by the hero's heading (`index.html` 98–102; `updateRaceHero`, `app.js` 2965–2983):
  "Choose a race" while none is chosen, then "<race> · Choose your look" while the look is missing, "<race> · Race
  locked" (disabled) outside Dev Mode once chosen, "<race> · Change race" in Dev Mode; the race's seal, or ✦ when it has
  none. Clicking it opens the race dialog (5755–5763), going straight to the look when a race has none and Dev Mode is
  off.
- **The race dialog** (`index.html` 302–331): "Choose a *race*", the file's state and "↻ Refresh file" (browser-only),
  "Playable races are always available. Dev Mode also unlocks non-playable races.", the list (`renderRaces`,
  2985–3017): one button per race, playable ones always, enemy-only ones with Dev Mode ("Non-playable · Dev Mode"); each
  with its seal (or ✦), its name, "Selected" or its kind, its eight modifiers ("STR +2" …) and its passive's name and
  description. A playable race (a new one, or another in Dev Mode) opens the appearance step; an enemy-only race is
  chosen at once (`selectRace`, 3038–3068: "Enable Dev Mode to select a non-playable race", "A chosen race can only be
  changed in Dev Mode", "<race> selected", HP and Mana set to their maxima).
- **The appearance step** (3070–3152; `index.html` 318–329): "<RACE> · APPEARANCE", "Choose your *look*", "← All races";
  two radio groups, MALE and FEMALE, of four looks each (`Sprites.playablePreview`: the figure as an SVG sprite wearing
  the hero's equipment, and the look's label); the arrow keys move the choice; the summary "<race> · <Male|Female> ·
  <label>"; the button "Begin as <race>" (or "Use this look" for the race already chosen); confirming sets the race and
  the look ("<label> chosen"). A race may be chosen with no look (the dialog closed): the seal then asks for one.

### The old game: the looks, the raw material of the maker

The 72 looks are data in `old-game/scripts/sprite-catalog.js` (`RACES`, 12–152), drawn on one humanoid rig by
`sprite-humanoid.js` (1,128 lines) with `sprite-kit.js` (299). The full inventory (every look's every field, every
value the rig draws, with lines) was made for this doc on 2026-11-15 and is kept in
`~/.cache/stat-fick-build/claude-work/gm2/design/looks-inventory.md` (outside the repo); what the maker needs is below.
- **How a look is made** (`sprite-catalog.js` 10–11, 154–170): each race has a `base` and four looks per sex, merged
  (objects key by key); `tunic(colour, pants)` and `dress(colour)` make the outfit; `playableSpec` adds the rig, race,
  sex and the race's battle `size` (169: dwarf 0.86, hobbit 0.74, elf and dark elf 1.04, Goliath 1.18, the rest 1).
  A look has no id but its race, sex and index; its label (Wayfarer, Sellsword …) is its only name (`lookLabel`, 607).
- **Every colour is a hex value in the look** (skin, hair, beard, eyes, brows, outfit, trousers, ties, crests, head
  pieces); palette names exist only for gear (`MATERIALS`, `CLOTH`). Among the 72: 68 skin colours, 43 hair, 32 eye,
  49 outfit colours. Every colour drawn goes through `tone` (`sprite-kit.js` 38–59: saturation × 0.68, lightness to 0.04
  + 0.86 L, then 10 % toward `#4a3220`), the old game's muted look; a `"!"` prefix opts out (glows, glints).
- **What each race fixes (its `base`) and what its looks vary:**

  | Race | Fixed by the race | Varied by its 8 looks |
  | --- | --- | --- |
  | Human | nothing (the rig's defaults: round head and ears, round eyes, small nose, boots) | skin, hair, beard (2 male looks), eye colour, fierce brows, freckles, outfit (tunic or dress) |
  | Elf, Dark Elf | long ears, narrow eyes, a slimmer build (legs 26, torso 27, shoulders 10.5, hips 9.5), size 1.04 | skin (the Dark Elf's grey-violet), hair, eye colour, a circlet or a flower, a scar, outfit |
  | Dwarf | build (legs 17, torso 25, head 21, shoulders 13.5, hips 11.5, arm 19), big nose, size 0.86 | skin, hair (bald too), beard (braided, forked, long, full), brow colour, freckles, outfit |
  | Hobbit | build (legs 15, torso 21, head 19.5, shoulders 10, hips 9.5, arm 17), big bare feet with foot hair, small nose, size 0.74 | skin, hair (foot hair in its colour), eye colour, a flower, outfit (vest or dress) |
  | Lizardman | lizard head, no ears, no hair, no nose, no brows, slit eyes, a lizard tail in the skin's colour, claws for hands and feet, a loincloth, bare legs | skin (green, teal, sand, red, jade, blue, gold, violet), crest (spines, fin, frill, feathers) and its colour, eye colour, the loincloth's colours |
  | Half-Beast | beast ears in the hair's colour, claws for hands (placeholders, T5) | skin, ears pointed (`beast`, wolf, fox, lynx) or round (`beast-round`, bear), hair, a beard, a wolf or tufted tail in the hair's colour, eye colour and shape, outfit |
  | Merfolk | fin ears, round eyes `#1f4f6f` (placeholders, T5) | sea-tinted skin, hair, a fin crest (2 looks), stubble, outfit |
  | Goliath | build (legs 25, torso 30, head 19, shoulders 14, hips 11.5, arm 24), big nose, size 1.18 (placeholders, T5) | stone-grey skin, hair, beard, eye colour, outfit; no rune markings drawn (T4 asked for them) |

- **The fields and the values the rig draws** (`sprite-humanoid.js`; P: used by the 72 looks, E: only by humanoid
  enemies):

  | Field | Values the rig draws | Used by the 72 looks |
  | --- | --- | --- |
  | head (86–116) | round (default), lizard; E orc, troll, skull | round 64, lizard 8 |
  | ears (118–149) | round (default), long, fin, beast, beast-round, none; E pointy, goblin; unused animal | round 32, long 16, fin 8, beast 6, beast-round 2, none 8 |
  | eyes (152–195) | round (default), narrow, slit; E beady, glow, void, skull, closed; `fierce` brows; lashes (every female) | round 46, narrow 18, slit 8; fierce 6 |
  | brows (186–193) | drawn unless off; colour from the hair or `browColor` | off 8 (lizardmen), own colour 2 |
  | nose (213–220) | small (default), big, none; E long, snout | small 48, big 16, none 8 |
  | mouth (223–231) | line (default; a lip for women); E grin, fangs, tusks, teeth; unused smile, frown | line 72 |
  | marks (233–235) | scar, freckles (`blush` is set on 9 looks and never drawn) | scar 1, freckles 2 |
  | hair (240–317) | bald, crop, spiky, swept, curly, curls, topknot, ponytail, highponytail, long, wavy, bob, bun, braid, twinbraids, messy, shaggy, each with a colour and, for tied styles, a tie colour; E mohawk, leaves, flame; unused pixie | 16 styles and bald; long 11, crop 7, braid 7 … |
  | beard (319–334) | stubble, full, long, braided (with ties), forked; E goatee | 10 looks |
  | crest (388–404) | fin, spines, frill, feathers | 10 looks (lizardmen, two merfolk) |
  | headwear (406–486) | circlet (with a gem), flower; E 12 crowns, hoods, hats | 4 looks |
  | horns (336–386) | E only: 9 styles | none |
  | outfit (628–791) | tunic (default), vest (with a shirt), dress, loincloth, with colour, second colour, trim, trousers or bare legs, stripes; E 12 more (robes, plates, fur, leaves …) | tunic 41, dress 15, vest 8, loincloth 8 |
  | lower body (797–913) | legs (default); E goat, bird, roots, fishtail, snaketail, trunk, float | legs 72 |
  | feet (842–860) | boots (default), big-bare (with foot hair), claws; E bare; unused shoes, paws, hooves | boots 56, big-bare 8, claws 8 |
  | hands (957–958) | normal, claws | claws 16 |
  | arms (925–938) | normal; E wings, branches | normal 72 |
  | tail (964–970) | lizard, wolf, tuft; E devil and imp | 16 looks |
  | wings (976–999) | E only: bat, decaying; unused feather, insect | none |
  | body material (20–38) | 17 (stone, bark, fire, magma, ice, water, lightning, wind, bone, metal, sandstone, shadow, ghost, sand, blood, nature, holy) | none (enemies only; 5 unused) |

  The rig draws 154 styles in all (90 head pieces, 64 body pieces); the 72 looks use 55.
- **Proportions and builds** (`sprite-humanoid.js` 13–16, 1008–1020): the catalogue's numbers are ratios of an older
  chibi rig's defaults (legs 24, torso 27, head radius 20, shoulders 11.5, hips 10, arm 22) applied to the rig's own
  (legs 46, torso 33, head radius 8.4, shoulders 13.5, hips 9.6, arm 37); a woman's shoulders × 0.86, hips × 1.04, arm and
  leg widths × 0.86 and 0.9, head × 0.96 (1009). Measured from feet to crown, a human stands about 101 units, six heads
  tall; an elf 105 (6.3 heads), a dwarf 86.5 (4.9), a hobbit 76 (4.6), a Goliath 106 (6.6); in battle each is then scaled
  by its race's `size` (a Goliath 125 units against a human's 101). The race picker scales every figure to 118 px.
- **Ink, shadows and hatching** (`sprite-kit.js` 10–112): outline `#130e0b`, pens 1.25–1.8 units, an ink shadow over the
  side away from the light (a solid share and a hatched edge at 72°), hatch strokes 2.4 units apart at 60°; light from the
  facing side. The engine's look of M6 was measured from these at 1080p ("Decisions for M6").
- **Choosing and storing:** the save holds `appearance: { gender, variant }` or null, nothing else (no colours, no
  seed). With no look, the battle picks one from a hash of a seed (`normalizeAppearance`, 560–566: the hero's seed
  "player" gives male, look 0); an enemy-only race draws its enemy's figure (`RACE_FALLBACKS`, 509–510). Recruits get a
  random sex and a random look of the four (`companions.js` 155–189, `makeOffer`); an imported companion keeps its look,
  forced to its sex (`app.js` 1484–1490, `stableLookVariant`).
- **Where the hero's figure shows:** the race picker (118 px), the battle stage (scaled by the stage and `size`), the
  battle cards' and turn list's portraits (a head crop, 22–40 px); not on the sheet (the race seal), not on the
  companion sheet (the seal), not on the recruits' cards.

### The old game: the seals

Nine files `old-game/objects/race-<id>.svg`, each a 128 × 128 view box with a `<title>` "<Race> seal": a dark disc, a
rim and an inner ring. **The six finished seals** (Human, Elf, Dark Elf, Dwarf, Hobbit, Lizardman; 3.2–3.6 KB each): a
radial dark field, a gold rim with 24 tick marks, the motif in parchment `#e4d3a8` with ink strokes, hatching on its
shadow half, a glow in the race's colour behind it and a pale highlight arc. Their motifs: a red heater shield with a
parchment cross under a gold crown (Human); a parchment leaf before a pale crescent (Elf); a crescent crossed by a
dagger with a lilac star (Dark Elf); mountains over an anvil and a hammer striking sparks (Dwarf); an acorn between two
leaves (Hobbit); a scaled diamond with a reptile eye and two fangs (Lizardman). **The three placeholders** (Goliath,
Half-Beast, Merfolk; 0.7–0.9 KB, marked "PLACEHOLDER ART (port task 11, T5)"): a flat field and a plain rim, a snow-capped
mountain, a paw print, two waves under a fin. **Where they show:** the sheet's seal button, rotated 11° (the image about
124 px, 99 px under 840 px, 72 px under 650 px; `styles.css` 216–240, 2466, 2915, 2933; sepia-toned in dark mode, 3648);
the race list (56 px, 617–618); the companion sheet (68 px, 52 px under 560 px; `companions.js` 269). The engine's own
tests read the six older seals as text, as the captured old sheet names them (`Tests/Statventure.Tests/Ui/
GamePaintTests.cs` 62–80).

### The engine

Checked 2026-11-15 (the details, with lines, kept in `~/.cache/stat-fick-build/claude-work/gm2/design/
engine-3d-facts.md`, outside the repo). Paths under `Engine/Statventure/`.
- **The look as the renderer holds it:** `CelStyle` (`Render3D/Renderer3D.cs` 189–212), a plain C# record whose one
  instance is `CelStyle.Default`: ink `#130e0b`, shadow mix 0.42, band edge 0.3, outline 3.5 px, inner lines 2 px,
  crease 120°, hatching 6 px apart, 1 px, 72°, 35 % toward the ink, glow 8 / 2.5 / 2.2. It is set per view
  (`View3D.Style`, `Render3D/View3D.cs` 64; a `SceneView` hands it to its figures). The sun is one white directional
  light per view (`SunLight`, `Renderer3D.cs` 177–180: direction, strength, angle 3°); a coloured sun is refused
  (`Render3D/GltfImport.cs` 114–118). **Not in the engine:** ambient, fill, rim or coloured light; more than one light;
  textures or vertex colours (one colour per material: `GltfImport.cs` 355 reads only `TEXCOORD_0`, no shader samples a
  texture); shape keys (bones only, M7); a record, pack form or editor kind for a style or a palette. Fixed in the
  shaders: the band's antialiasing, the inner lines' depth and angle tolerances (`Shaders/Source/ink_edges.frag` 131–220),
  16 shadow maps of 1024² (`Renderer3D.cs` 30–31). Characters never receive shadows; scenery does (`Render3D/Model.cs`
  302).
- **Palettes:** `Render3D/Palette.cs` 14–60: slot names with sRGB colours and optional shadow tones; a material names
  its slot by its own name (Blender's `.001` stripped, 53–59); a missing slot takes the material's base colour. An
  `Actor` has one palette (`Scene/Actor.cs` 40) and draws its items and garments with it (93, 97): gear has no palette of
  its own, so two pieces that name the same slot show one colour. Two figures sharing a model can have different
  palettes (Ji'ruan's cutscene does, `Samples/Statventure.Sandbox/Cutscene.cs` 122–140).
- **Figures:** an `Actor` (`Scene/Actor.cs` 16–103) is one body model, its animator, its palette, `Items` (rigid
  `ActorItem`s on a named node, `Animation/Gear.cs` 14–36) and `Garments` (`ActorGarment`s skinned to the wearer's bones
  by name, 46–88, "made per body type": no refit to other proportions, 42–44); the body is fixed when the actor is made
  (a new body means a new actor); gear can be added and removed at any time (`Actor.cs` 53–59). Figure and gear are one
  model: one outline, one glow, one hatching anchor. There is no per-instance switch to hide a part of a model (only
  `ModelInstance` of stages has one). Skinning: 4 influences per vertex (M6); a clip is shared by bone names, relative
  to each bone's rest, translations not scaled with the body (`Animation/Skeleton.cs` 122–145).
- **Views:** a `SceneView` in a `surface` element (`UI/SurfaceViews.cs` 16–60); it renders only when something changed,
  so an idling figure renders every frame; picking gives the owner and an 8-bit part index (`Renderer3D.cs` 887); each
  view's render target allocates a 4096² shadow atlas (64 MB) when made (`Renderer3D.cs` 104). No limit on views.
- **Models, clips and machines as data:** `model` v2 (`Render3D/ModelData.cs` 225–241, `[Kind("Model", "models")]`),
  one entry per `.glb` at `models/<path>`; a rig family's clips as a `.glb` with the armature and its actions;
  `animation.state-machine` v1 (`Animation/StateMachineDefinition.cs` 25–93); stages at `models/stages/<name>`.
  **No `models.pak` and no `data.pak` exist** (`Assets/Packs/` holds `audio.pak` and `ui.pak`; the engine mounts the
  others when present).
- **The stand-in figure** (`Tools/BlenderReference/make_models.py` 534–638, `figure.glb`): 17 deforming bones (`hips`,
  `spine`, `chest`, `neck`, `head`; `upper_arm`, `forearm`, `hand`, `thigh`, `shin`, `foot` per side) and two sockets
  (`socket.hand.R`, `socket.back`), tubes and a sphere; its clips `idle`, `swing`, `twist`, `pulse`; `stocky.glb` the same
  bones in other proportions. "Not the bar" (Decisions for M10).
- **The Blender harness:** `make_models.py` (2,104 lines, 16 test models, exported reproducibly with a canonical triangle
  order, 146–182; `ANIMATED` 188–189; `export_markers` 506–515); `Tools/Scripts/make-blender-references.sh` (models,
  renders, glow, evaluations); EEVEE at 64 samples with Freestyle lines (`render_scenes.py` 61–93, 308–336). Committed: 16
  models, 56 render goldens (30 shading, 26 ink), 33 evaluations. `ReferenceLimits.M6` and `DetailedStage`
  (`Tests/Statventure.Tests/Render3D/ReferenceLimits.cs` 15–61; the latter the user's, for Ji'ruan, S7-13). Every test
  scene uses `CelStyle.Default`, written into the scene file the Blender script reads, so a new default regenerates every
  reference by running the tests and the script; **except Ji'ruan's stage**, whose colours `jr_lit` bakes from the old
  0.42 and ink (`make_models.py` 1142–1148). A change of the line widths touches the 26 ink goldens, of the band,
  hatching, shadow mix or ink the 26 shaded ones with models or terrain (Ji'ruan's six among them), and two tests hold the
  42 % numbers (`CelStyleTests.cs` 13–18, `Render3DReferenceTests.cs` 156–160). The 33 evaluations don't depend on the
  look.

### What GM1 left

From the code (`Game/`, `Tests/Statventure.Game.Tests/`) and `Docs/Game/Foundation.md`:
- **The program's seam** is `IGameScreen.Attach(GameParts)` (`Game/Statventure.Game/IGameScreen.cs` 8–12); `GameParts`
  holds Settings, Session, Mods, Ui, Random and the game (`GameParts.cs` 21); the screens are a fixed list
  (`StatFickGame.NewScreens()`, `StatFickGame.cs` 101–102, mirrored in the tests' `ScreensHarness.cs` 51–68); commands
  through `GameCommands.Register(name, run, enabled)`; markup as C# raw strings of the old HTML parsed by the engine;
  sounds by `parts.Ui.Sound(SoundNames.X)`, one per action (`Ui/GameSounds.cs` 28–66).
- **The character's page** (`Screens/Sheet/SheetScreen.cs` 34–56): the ⚙ button, the top bar (Return to combat
  disabled, the ✦ mark, "Stat Fick", the clock at "Day 1 · 08:00", the autosave dot, "☰ Menu"), the two status lines, and
  an empty `main.shell`. Its sheet `Assets/UI/styles/sheet.css` holds `body { padding-left: 0 }` (Q32);
  `Assets/UI/styles/game.css`, the old stylesheet, already holds every rule of the sheet, the rail (300 px, 260 px at
  1350 px or less) and the drawer (840 px or less). `Tests/Statventure.Game.Tests/Screens/PageTests.cs` 107–116 asserts the
  rail's room is not kept; GM2 changes that test with the rail.
- **The save** `save.game` v1 (`Game/Statventure.Game.Data/Saves/SaveGame.cs`: `[NotAPackKind]`, `Name`, its check);
  its layout `Tests/Statventure.Game.Tests/Data/Records/Layouts/save.game.txt` and fixtures
  `Fixtures/save.game/ser-alrik.{json,rec}`; `LibraryRecordLayoutTests.cs` 21–27 and 66–71 pin the library's kinds and
  version 1's layout (GM2 changes both). **The session** (`Session/GameSession.cs`): `New(name)` (139–149) after the
  start screen's name prompt (`StartScreen.cs` 206–214), `Change(update)` (226–236: checks, saves, raises `Saved`; nothing
  calls it yet), `Export` and `Import` (252–299, older versions upgraded).
- **Randomness:** `IGameRandom` with `Next()` and `Next(n)`, `GameRandom` (`xoshiro256**`), `ScriptedRandom`
  (`Random/GameRandom.cs`); nothing draws from it yet; Foundation.md's "few helpers the old code repeats" were not added.
- **The data library** holds two kinds, `save.game` and the development-only `stand-in.item` (`StandIn/StandInItem.cs`
  12–25). What still leans on the stand-in: the editor's tests (`GameTesting.cs`, `StandInKindTests.cs`,
  `GameLookupTests.cs` 114, `GameFolderModTests.cs`), `end_to_end.py`'s mod, the game's mod and settings tests, and the
  goldens `settings-mods-dark` and `settings-mods-light` (they show its error text).
- **Screens against Chromium:** `IGameScreenFixtures` (`Tests/Statventure.Game.Tests/Ui/GameScreenFixtures.cs` 24–42), 62
  fixtures, 12 levels / 0.2 % (`GameScreenTests.cs` 70); only the icons' boxes are masked: **no mask for a 3D view
  exists yet** (the old game's inline SVG mask, `GamePaintTests.cs` 37–51, is the pattern).
- **Dev Mode** is `parts.Settings.DevMode`, followed through `Settings.Changed`; the settings screen's line already says
  what it unlocks (`Screens/Settings/SettingsScreen.cs` 40–41).
- **Icons:** of the 28, the sheet, the rail, the drawer and the race picker use 16, 13 beyond GM1's ⚙ ✦ ☰: ↺ ↻ ⌖ ▣ ◇ ◈
  ◌ ◫ ♜ ♟ ♡ ⚔ ✧.
- **The packages** list `audio.pak` and `ui.pak` only (`Tools/Packaging/listings/stat-fick-linux.txt` 56–57).

### Where the plan was wrong or has moved

Found on 2026-11-15, against `Docs/Game-Plan.md`'s GM2 section as written on 2026-11-12 (corrected there with the
answers where a sentence was wrong):
1. The answers moved GM2's whole art: a character maker instead of 72 remade looks (GM2-1, GM2-6, GM2-7), a figure on
   the sheet (GM2-2), a new look with everything open (GM2-3, GM2-8), nine 3D emblems instead of three SVG seals
   (GM2-5). The section's "What the player gets", "What is new", "The art", "How it is checked" and "Weight" are
   rewritten for them.
2. "15 body materials": the rig has 17 (`sprite-humanoid.js` 20–38; ghost and sand were left out of the survey's count,
   `Docs/Game/Survey/content-inventory.md` 243). Enemy-only; GM9's.
3. "about 76 part styles": not itemized anywhere. The rig draws 154 styles (90 head pieces, 64 body pieces) and the 72
   looks use 55; the six fields the survey names come to 67, 76 with crests, noses and the war helm.
4. "the sheet against `game-sheet` … the race picker against a capture added to `Reference/game-capture.js`": both
   screens change (GM2-2, GM2-5, GM2-1), so neither is compared with the old game; captures for approval, then Chromium
   rendering the port's markup ("Method").
5. "3D views masked": the game's screen tests mask no 3D view yet; GM2 adds the mask (GM2-P12).
6. **Blender:** the PC's Flatpak Blender is **5.3** now (`flatpak info org.blender.Blender`, 2026-11-15); every reference
   and every ported rule is 5.2's, and `make-blender-references.sh` takes the Flatpak when `$STATVENTURE_BLENDER` is
   unset (17–25). GM2-P10.
7. Dev Mode "the appearance step skipped" (GM1's list): in the old game a playable race still opens the look step in Dev
   Mode (`app.js` 3013); Dev Mode only skips it for enemy-only races (which have no looks) and doesn't force it when the
   seal is clicked (5761).
8. The effect type counts ("216 `type` values in `app.js` and 176 in `combat.js`"): counted again by their `type === "…"`
   and `kind === "…"` tests, about 250 and 220; a matter of how they are counted, nothing depends on it.
9. For GM3 (not GM2's to change): the engine's gear has no palette of its own (`Scene/Actor.cs` 93, 97), so "one sword
   model in every metal" holds per figure, not per piece: a sword and armour of different metals on one figure need
   distinct slot names or an engine change. And `Foundation.md` (3521–3528) names GM2 among the milestones deciding the
   old `window.prompt` calls; none of the five is on a GM2 screen (the familiar's and a class level's choices are GM8's).

## Method

GM2 has five kinds of work, each with its reference. Every check runs headless and silent, on scratch per-user folders,
never the player's real ones; Blender work runs only on the user's PC ("How each step is finished", items 1 and 2;
`Docs/Game-Plan.md`, "Where each kind of work runs").

- **Rules against the old game** (`Docs/Game-Plan.md`, "How each step is finished", item 3). GM2 is the first milestone
  to use both oracles, so its first rules step builds them:
  - *Through Node:* `CombatRules` (`combat-engine.js`: `experienceRequired`, `sameLevelVictories`, `enemyExperience`) and
    `LimitlessRules.cap` (`limitless-engine.js`), for every level 1–200. Node 24 is in the build cache
    (`~/.cache/stat-fick-build/node-v24.21.0-linux-x64/`, not on the PATH; checked 2026-11-15): a generator script runs
    the module there and writes `Reference/*-results.json`, committed, so the tests run where there is no Node (the plan
    left "how the generator runs Node" to the first step that needs it).
  - *Through the test client:* the sheet's rules inside `app.js` (the formulas, `raceStatModifier`,
    `calculateRaceEffects`, `currentStats`, max HP and Mana, Armor and Evasion with nothing worn, level up and its
    healing, the steppers, respec, `adjustResource`, `normalizeRaceDefinition`'s refusals, `selectRace`'s texts and
    refusals, `raceDisallowsItem`), for each of the 28 races (Dev Mode reaching the enemy-only ones) over levels,
    attributes, bonuses, allocations and resources the tests name: `chromium.sh old-game/test-client --script=<file>
    --call=<function>`, the results brought out through the test preload's save hook (`Docs/Notes.md`, 2026-11-11/12),
    committed. None of these rules draws `Math.random`.
  - *The old checks:* each expectation of `runLevellingCurveChecks` (`small-changes-browser.js` 102–118), the Level
    icon's (`map-browser.js` 15–22), `checkNewRaceData`, the sheet's part of `checkGiantsBlood` and `checkNewRacePicker`
    (the list's content) has a port test with the same expectation, named after it. Their battle parts
    (`checkNaturalClaws`' attacks, `checkGiantsBlood`'s companion) are GM5's and GM11's; `checkNewRaceRecruits` GM11's;
    `checkNewRaceLooks` is about the old sprites, and what it holds that the decisions keep (T2–T4, T14: the Goliath
    taller, the Merfolk on legs with fin ears, every Half-Beast with a beast's ears, tail and claws) becomes rules of the
    maker's data, tested on the data.
  - *Numbers as the old game computes them:* doubles, the same `floor`, `ceil` and JavaScript `round` (halves toward +∞)
    at the same points, in the same order.
- **Screens against Chromium.** Every GM2 screen is new or changed: the sheet (GM2-2 changes its layout: the paper
  doll, Q29), the race carousel (Q28, in place of the old list), the character maker (new). None is compared with the old game's captures.
  Each is built from the old game's markup, classes and panels (GM1-8), shown to the user as captures (1×, both themes)
  first, then tested against Chromium rendering the port's own markup and stylesheet (`Tools/PaintReference` as the main
  frame at 1 and 1.65, 12 levels / 0.2 %; `Reference/game-screen-fixtures.json` and `make-game-screen-references.sh`,
  GM1-P17), every 3D view's box masked there and tested on its own. The engine's own tests of the old `game-sheet` state
  stay as they are (they test the engine's drawing of the old page).
- **3D against Blender** (`Docs/Renderer-3D.md`, "Method"; `Docs/Animation.md`, "Reference harness"): every body, part,
  emblem and sample item rendered by Blender and by the engine from the same `.glb` and compared (`ReferenceLimits.M6`,
  or another limit only with the user's word: a tolerance isn't a step's to choose); every clip against Blender's
  evaluation within Animation.md's limits; gear on sockets as "Rendered gear" is checked. **Every model regenerable**
  (GP-4): the step that makes or changes a model rebuilds it from its script on the PC and checks the result against the
  committed `.glb` and renders; the cloud tests against what is committed.
- **Looks built at run time against Blender.** The engine assembles a figure from a look (a parameter set) at run time;
  the `humanoid` script assembles the same parameter set in Blender and renders it, and the two are compared for the 72
  presets and a fixed list of seeded random looks. Besides, a structural test walks every combination the maker allows
  (every part binds to its body's bones, every palette slot is filled, nothing is missing or doubled), which no render
  list can cover.
- **What only the user's eye can judge**, each approved before its tests: the look's candidates and the chosen look;
  the first humanoid bare, with the sample weapon and armour piece, in every clip and in a playing view (M7's "Model
  approval"); each later body and part family; the presets as rebuilt; the random button's rolls; the nine emblems; the
  sheet's, the picker's and the maker's captures; the sounds, by ear (GM1-6). Steps with an approval stop twice: first
  what the user must see, then the tests ("Steps").

## Design

The design the steps work from, decided (GM2-1–GM2-8, Q1–Q42, GM2-P1–GM2-P16); each choice names its question ("Qn") or
proposal ("GM2-Pn"). Nothing is open.

### The actor record and its calculations (the hero now; companions and every enemy later)

The old game's character record "is effectively the game's actor schema" (`Docs/Game-Plan.md`, GM2): the hero,
companions and enemies share it, and combat evaluates any of them by swapping the sheet's globals (`withActor`). The port
makes it an object and its rules functions of it, with no globals and no swapping:
- **The sheet record** (`actor.sheet`, in the data library, GM2-P1): what an actor *has*: the race (a reference to a
  `race` entry; none for a new hero, as the old `needs-race`), level, EXP, free points (`availablePoints`), class points
  (`perkPoints`: GM2 grants them on level-up, as the old game does, and GM8 spends them; kept from GM2 so a GM2 save's
  hero has the right count in GM8), the eight stored attributes (`stats`), the free points spent per attribute
  (`manualAllocations`, which respec gives back), the permanent bonuses (eight percentages, Armor and Evasion),
  current HP and Mana, the purse (silver, gold, platinum). Field names in the port's words with the old names in each
  field's `[Help]`. Later milestones add their parts as fields of their own records (equipment GM3, class levels GM8,
  spells GM7, Limitless GM15), each a new version (M9's rule), so the record never holds a stand-in.
- **The rules** (`ActorRules`, plain functions of a sheet, the races and the effect totals, GM2-P2): the base and
  current attributes, max HP, max Mana, Armor and Evasion (base, modified, current), the EXP target and the cap, and the
  actions: spend a point, respec, level up, change HP or Mana by hand, the Dev Mode edits. Each reads its inputs from
  the record and the totals, never from screen state; each is the old function's arithmetic in its order. The battle's
  overrides the old code reaches through `window.Combat` (`resourceMaximum`, `statValue`, `defense`,
  `permanentStatPercent`, `permanentDefensePercent`) are one interface the rules take, empty outside a battle (GM5 fills
  it); the artifacts' and classes' totals likewise (GM3, GM8), zero in GM2. So the hero's sheet in GM2 computes exactly
  what the old game shows for a hero with no equipment, no class and no battle, which is what the oracle checks.
- **Companions and enemies** (GM11, GM9) are sheets too: the same record and rules, with their own parts (a companion's
  name and look, an enemy file's level, classes, gear). The rules never ask "is this the hero".

### The effect interpreter

A race's passive holds a list of effects; so do artifacts, items, class levels, skills, statuses and spells in later
milestones (the old code compares about 250 distinct type strings in `app.js` and about 220 in `combat.js`, counted
2026-11-15 by their `type === "…"` and `kind === "…"` tests; `Docs/Game-Plan.md` counted 216 and 176 another way). The
port's shape (GM2-P3):
- **Effects are records, one subtype per type** (`[Subtype]` of an abstract `Effect`; the JSON form names it in
  `"$type"`, `Records/RecordAttributes.cs` 31–37): `MaxHpPerLevels { Amount, Interval }`, `DefensePermanentPercent {
  Target, Amount }`, `EquipmentRestriction { Category, Type }` … with the old type strings as their names
  (`effect.max-hp-per-levels`), so the editor shows each kind's own fields and the checks are per kind (G4: what the old
  loader accepted, no more). Adding a subtype is a new version of the union (31–34), which every milestone that adds
  types does.
- **Two sides, as the old code:** the *sheet side* folds a source's effects into totals (`RaceTotals` now, with the
  fields of `emptyRaceEffects`, `app.js` 1058–1078), which the rules read; the *battle side* (GM5) reads the same records
  at the moments the battle asks (the turn's start, a spell's damage, a death save). GM2 builds the sheet side for the 15
  race types the sheet reads (and `trap-limit-flat`, which the old fold reads though no race uses it) and declares the
  other seven as records whose sheet side does nothing, so all 28 races load and the editor shows them, and GM5 adds their
  battle side only.
- **What later milestones add:** GM3 the artifacts' and items' kinds (about 40 artifact kinds, `artifactEffectLabel`)
  with their totals; GM5 the battle side of every type the old battle reads, and the statuses' and reactions' effects
  (task 12); GM7 the spells' effects; GM8 the class effects (`addClassEffect`, `app.js` 686–937) and the "(new)"
  mechanics of task 10's drafts. Each milestone adds subtypes and a totals record of its own; no milestone changes the
  meaning of an existing type.

### The races as records

The kind `race` (GM2-P4): one entry per race at `data/races/<id>` in a new `data.pak` (none is committed yet:
`Assets/Packs/` holds `audio.pak` and `ui.pak` only), written from `old-game/objects/races.json` by a script whose output
is committed (`Docs/Game-Plan.md`, "The data library and the editor"). Fields: the name, playable, the eight modifiers,
the passive (id, name, condition, description, effects), the accent colour (`raceAccent`, now data: `[Colour]`), and
GM2's own: the race's emblem (GM2-5) and its maker data (below). Its `[Check]`s are `normalizeRaceDefinition`'s rules.
It is GM2's first real kind with `[Kind]`, so `stand-in.item` goes (GM1-P3): the editor's tests and the end-to-end mod
switch to `race` entries.

### The save, version 2

`save.game` v2 (GM1-P5): version 1 (`name`) frozen as `SaveGame.V1` with its fixtures; version 2 adds the hero: its
sheet (`actor.sheet`) and its look (`look.hero`, below). `Upgrade()` from v1 gives a new hero's defaults
(`defaultCharacter`: level 1, 10 in each attribute, 5 gold, 1 class point, HP 31 and Mana 20, a new `ActorSheet`;
"HP 30" here until step 2 found the old game's 31) with no race and no look, so
a GM1 character opens on the new-hero flow (Q26). Fixtures of both versions in both forms, the layout signatures
(`Tests/Statventure.Game.Tests/Data/Records/`). A character file is the save's JSON form, so a GM2 hero exports and
imports with its look.

### The look as a parameter set

A hero's look is a record of choices, not a model: `look.hero` (GM2-P6), kept in the save (v2) and in the character
file. It names the race, the sex (Q14) and the build (Q15), so the body; a part for each category the race lets the
player choose (the face, Q16; hair, beard, crest, the Half-Beast's ears and tail, marks, head pieces; Q13, Q19, Q22–Q24)
and a colour for each colour choice, a swatch of the race's lists (Q17), the outfit's colours among them (Q18); and the
preset it started from (GM2-6), for the maker's display only. Its `[Check]`s, run with the data, refuse a part the race,
the sex or the build doesn't allow, so a character file can't make a look the maker couldn't (G4). The engine builds the
figure from it at run time ("The humanoid rig family"); nothing about the figure is stored but the choices. Recruits
(GM11) and the humanoid enemies (GM9) can be the same kind of record, made by their milestones' rules from the same parts
(theirs to ask), so the figure code never asks whose look it draws.

### The sheet with the live figure (GM2-2; Q29–Q34)

The old page (`index.html` 90–299) built by `Screens/Sheet/` from the old HTML (GM1-P7), with the old stylesheet's rules
and the port's own sheet for what changes:
- **The paper doll** (Q29, as the sketch the user saw): in the middle of the equipment section a framed portrait panel
  with a dark backdrop (Q32) holding the hero's figure, the Primary hand and Off-hand slots at its sides, the Armor and the
  two Artifact slots below it. The figure "wearing the equipped gear" (GM2-2): a `surface` element with a `SceneView` and
  the hero's `Actor` (the look's body, parts and palette; GM3's gear as `ActorItem`s and `ActorGarment`s); before GM3 in
  its own outfit, the five slots inert (Q33). It **idles in a fixed three-quarter view and does nothing else** (Q31): no
  turning, no reactions to the sheet. Its view's background is the panel's dark colour; nothing in the panel is scenery,
  and characters receive no shadows (M6), so no shadow is seen there and no ground is modelled. What that saves beyond the
  ground: a view with nothing to receive a shadow has no use for the shadow pass and its 4096² atlas (64 MB, allocated per
  render target, `Renderer3D.cs` 104); whether the engine still allocates and renders them for such a view is measured by
  step 5, and if it does, the change (the atlas and the pass only for a view with scenery) is an engine task for the
  manager. An idling figure renders every frame, so the view pauses while a window covers the sheet and while the game's
  window is hidden (the engine's `visibilitychange`). It is built again only when the look or the gear changes.
- **The heading** (Q30): the hero's name as the page's heading in the old heading's style, the race and the level under
  it (the old eyebrow, the copy line and the seal button's place as the captures show). The seal button shows the race's
  emblem picture (Q36); outside Dev Mode it is inert ("<race> · Race locked", the old title), in Dev Mode it opens the
  carousel (Q27).
- **The rest as the old sheet:** the EXP card; Level, HP and Mana with the old controls; Armor and Evasion with Base,
  Modified and Current; the eight attribute cards with steppers, free points and Respec; Dev Mode's controls shown when the
  setting is on (GM1-12, GM1-13), following `Settings.Changed`. Every change goes through `GameSession.Change`, which
  saves and lights the dot, as `saveCharacter` did.
- **The whole Field tools rail** (Q34; GM1's Q32: the rail and its room come with GM2): the purse, the companions' list
  (empty until GM11) and the eight tools, each shown and disabled until its milestone (Map GM4, Combat test GM5 and Dev
  Mode's, Spell book GM7, Inventory GM3, Companions GM11, Classes, Skills, Passives GM8); the "☰ Field tools" toggle, its
  shade and the drawer under 840 px ported from `interface.js`; the page's room for the rail back (`sheet.css`;
  `PageTests.cs` 107–116 changed).
- **Checked:** captures for approval (1× and both themes, the drawer, Dev Mode on and off), then the port's markup
  against Chromium with the panel's view masked (GM2-P12), the figure on its own against Blender.

### The new hero's flow: name, race, maker (Q25–Q28)

Name, race, maker, sheet (Q25): GM1's name prompt, then the race carousel, then the maker; the sheet opens only with a
race and a look (Q26): leaving goes back to the start screen, and a hero without both (a GM1 save, a flow left unfinished)
opens on the flow again.
- **The race carousel** (`Screens/Races/`; Q28): one race at a time with a large figure (the race's first preset, idling in a
  fixed three-quarter view as the sheet's, Q41), the race's emblem live and slowly turning (Q36), and the old list's content for that race: its name, its
  eight modifiers ("STR +2" …) and its passive's name and description; the old texts ("Choose a *race*", the intro line);
  the next and previous race in `races.json`'s order by buttons and the arrow keys; choosing it opens the maker. Dev Mode
  (Q27): the 19 enemy-only races in the carousel too ("Non-playable · Dev Mode"), chosen at once as the old game did
  (no look), shown as the engine's stand-in figure until GM9 makes their bodies; the race changeable later from the
  sheet's seal button, a playable race then opening the maker again.
- **The character maker** (`Screens/Maker/`; GM2-1, GM2-6): the hero's figure large, idling in a fixed
  three-quarter view as the sheet's (Q41), the race's eight presets as a list (Q21), labelled with the old labels (Q20), four per sex; the choices by category (sex, build, face, hair, colours …),
  each option named (Q20), colours as the race's swatches (Q17); the random button rolls every choice the race allows, no
  locks (Q21), "none" counting as an option where a category has it (Q23, Q24), each draw uniform over its list (no
  weights were asked; GM2-P15's helper), from the game's randomness service (fresh on every load, GM1-5). "Begin as
  <race>" (the old button's words) confirms: the race and the look saved, HP and Mana set to their maxima (as
  `selectRace`), the sheet opened; "← All races" goes back to the carousel. The look is final then (GM2-7), Dev Mode
  apart (Q27).
- **What the fixed view doesn't show** (a fact of Q41, not a choice): the maker never shows the back of the hair, a tail
  or the outfit, so the presets' and the parts' approval sheets (Q5's turnarounds, steps 9a–9e) are where the user sees
  them.
- **Checked:** both screens as new screens (captures, then Chromium of the port's markup with the views masked); the
  flow's rules (what each step allows, refusals, Dev Mode, what is saved when) as the old game's where it has them
  (`selectRace`'s texts and race lock), new rules by tests of their own.

### The maker's parts model (Q13–Q24, Q40)

- **What a race fixes** (Q13): its bodies (two sexes, Q14, three builds, Q15, at Q1's heroic ratios with the races'
  differences kept by the same ratios) and its defining traits: the elves' long ears and narrow eye shape, the lizardman's
  head (no hair, no ears, no brows), its tail and claws, the Merfolk's fin ears and webbed hands (T3), the Half-Beast's
  claws and its having beast ears and a tail (their shapes free, Q22), the hobbit's big bare feet, the dwarf's and the
  Goliath's build; a face list per race keeps the race's nose and features.
- **What the player chooses:** sex and build; one of about six faces per race and sex (Q16, Q40), stylised (Q2); eye
  colour; hair style (about 16–24 per sex, Q40) with its colour and tie; a beard where the race's lists offer them (about
  12, Q40); the crest (lizardmen; the Merfolk's fin crest); the Half-Beast's ears, tail and fur colours freely (Q22); marks
  (scars, freckles; the Merfolk's scale patterns and the Goliath's rune patterns), each list with "none" (Q23); small head
  pieces (circlets, flowers, ties, beads), hidden under a helmet (Q24); the one outfit's colours (Q18); the skin. The
  large set (Q19) is about twice the "every old part plus a few new" set, faces kept at six (Q40); each list is proposed
  for approval in its step.
- **Colours:** palette slots on the parts' materials (GM2-P7's list), filled from the look's swatches; the swatches part
  of `race` (GM2-P7 as decided); one actor palette for figure and gear (`Actor.cs` 93, 97), so the hero's slots and GM3's
  gear slots take distinct names.
- **The presets** (GM2-6), the old 72 rebuilt as far as Q18 and Q19 allow. Each keeps its race, sex, label, hair style,
  beard, crest, ears, tail, head piece and marks (freckles, the scar), their colours as the nearest swatches, its eye
  colour, its skin; it takes the average build and a face from the list. **It loses its old outfit's shape:** the 41
  tunics, 15 dresses, 8 vests and 8 loincloths become the race's one outfit for that sex (Q18) in the old colours (its
  colour, second colour and trousers where the outfit has those slots). **The Half-Beast's presets** (Q22: no beast
  choice): each old look is already one of T14's beasts by its name, colours and parts, as this doc reads them (Greyhowl
  and Moonfang wolf, Emberbrush and Russetail fox, Bramblehide and Honeypaw bear, Tuftear and Snowpelt lynx), so each
  preset sets that beast's ears, tail and fur from the free lists; the reading is shown with the presets for approval.
  **The Goliath's and the Merfolk's presets wear marks** (Q42): a rune pattern on each Goliath preset, a scale pattern on
  each Merfolk preset, each chosen to suit its old look; any hero can still choose "none". Checked against the catalogue's data by a test that
  reads `sprite-catalog.js`'s values through the oracle (the old sprite scripts load in Node).

### The humanoid rig family

M7's rules made concrete for the game's first family (GM2-P8, GM2-P9):
- **One skeleton** for every humanoid body: the stand-in's 17 deforming bones as the base (`hips`, `spine`, `chest`,
  `neck`, `head`; `upper_arm`, `forearm`, `hand`, `thigh`, `shin`, `foot` per side); **mitten hands** (Q2): per hand a
  thumb bone and one bone for the block, so a hand closes on a grip, no finger bones; **no face bones** (Q2): the head
  moves as one, no blinks, no expression clips; chains for keyed secondary motion (M7) where a part needs them (tails,
  long hair, braids); the sockets M7 names (`socket.hand.R`, `socket.hand.L`, `socket.back`, `socket.head`,
  `socket.hip.L`, `socket.hip.R`) and one for the shield where its approval puts it (Q9). Every body has every bone, so
  every clip plays on all of them.
- **Bodies:** 54 (9 races × 2 sexes × 3 builds), heroic (Q1), differing in proportions only (M7); each body's `.glb`
  carries the skeleton in that body's rest; parts made on it bind by bone names as garments, rigid ones on sockets. A clip's
  translations keep their distance on every body (`Skeleton.cs` 122–145), so the hips' bob and the lunge's steps are
  authored on the human average body and checked on the hobbit's slender and the Goliath's strong body in the clip
  approval.
- **The clip set** (M7's list, about 28): idle, stunned; advance, return, flee; one-handed swing, two-handed swing,
  thrust, dagger stab, staff or wand strike, unarmed punch, off-hand swing; bow, crossbow; cast, skill, item, drink,
  meditate; guard and evade (enter, hold); hurt, critical hurt, stumble; death, victory; with their markers. None needs
  face bones or fingers (Q2). The old sprites had no fingers either (hands drawn as one shape, or claws), so no old pose is
  lost; what the mitten shapes is each grip: the sword's and dagger's grip and the staff's and two-handed haft held by the
  closed block and thumb; the shield's strap; the bow's string hooked by the block and the crossbow's trigger pressed by
  it; a flask held between thumb and block for drink and item; the punch a closed mitten; the claws of lizardmen and
  Half-Beasts modelled on the block. One clip library for the family (`models/humanoid/clips`), shared by bone names; GM5
  builds the battle's state machine, GM2 a small one for its figures (idle).
- **The sample gear** (Q9): a one-handed sword on `socket.hand.R`, a shield on the left arm, a chain shirt fitted as a
  garment to the human man and woman (average build); shown in every clip batch; whether GM3 keeps them is GM3's.
- **What the engine limits today** (for GP-8's bar set on facts): one colour per material, no textures or vertex colours
  (eye shapes, brows, lips, freckles, scales, runes and cloth trims are geometry with their own materials); no shape
  keys; 4 bone influences per vertex; linear blend skinning; one white sun, no ambient or coloured light; ink lines from
  silhouettes, surface crossings and creases sharper than the crease angle (120° today), so a detail shows as a line only
  if it is modelled as a crease or an edge; hatching on whole shadow areas. No vertex, triangle or bone count limit was
  found; the target (Decisions for M6) is a 2016 GPU at 60 fps in 1080p with up to 8 figures a side, so the first
  humanoid's counts are measured on the GPU and on lavapipe and reported with its approval.

### The Blender script family `humanoid` (GP-4) and looks built at run time

- **Scripts** (GM2-P9): `Tools/Models/humanoid/` for Blender 5.2 (GM2-P10): the skeleton and sockets, each body, each part
  family, the clips keyed by script, the emblems, the sample gear, the look's numbers and slot names; outputs in
  `Assets/Models/humanoid/` (committed), exported as `make_models.py` exports; `models.pak` by a pack script (GM2-P4);
  `build.py --check` rebuilds into a scratch folder and compares with what is committed (GP-4). **Fitting by script:**
  what is designed once and worn on several bodies (the outfit on three builds, hair on each race's skull, beards on the
  faces that offer them, head pieces, marks) is fitted by the script (the same fit run again on a rebuild), each fitted
  result approved with its family.
- **Assembly in Blender:** the same package assembles a `look.hero` (its JSON form) in Blender (the body, its parts bound
  by Armature modifiers as the tabard is, `Docs/Animation.md`, "Rendered gear"; the palette, the sun, the style) and
  renders it with `render_scenes.py`'s rules; the engine's figure builder makes the same look; the two renders compared
  (`ReferenceLimits.M6`, or a limit the user approves) for the 72 presets and a fixed list of seeded random looks, and the
  bound parts' vertices at a clip's frame as `GearReferenceTests` does.
- **The structural walk:** every body with every part it allows and every slot filled: all bones matched, no material
  without a slot or a fixed colour, no preset or roll naming a missing part; in the game's tests.

### The new look (GM2-3, GM2-8; Q4, Q6, Q7, Q11, Q12)

- **Within today's rules** (Q7): any palette and per-slot shadow tones, the shadow mix and band edge, the sun's direction,
  strength and softness, the ink's colour and two line widths, the crease angle, the hatching's spacing, width, angle and
  strength, the glow's numbers. A new rule (more light, a third tone, other lines or hatching, textures) only when the user
  asks for one after seeing the candidates, as an engine task of its own.
- **How it is chosen** (Q4, Q6): a mood board of about six small renders of directions on a rough figure (the side view
  and a ¾ close-up), the user picks one or two; then three full candidates on the first humanoid's blockout; then as many
  rounds as the user wants. The candidates are also where the user first sees **a stylised face at heroic proportions**
  (Q1, Q2): which features are geometry (the eye shapes, the brows, the nose, the mouth's line, the ears) and how much of
  each the ink draws is what the candidates show and the user chooses; this doc doesn't fix the face's style.
- **Where it lives** (Q12, GM2-P11): one record in `data.pak` (the engine's `render.style`: the `CelStyle` numbers and
  the sun; the palette's swatches are in the races), used by every game view and restylable by a mod.
- **The engine's references keep their test values** (Renderer-3D.md: "test data, not the game's look"); **Ji'ruan's
  stage** takes the new look when GM14 reuses it (Q11; `jr_lit`, `make_models.py` 1142–1148, bakes today's).

### The nine seals as 3D emblems (GM2-5; Q35–Q37)

Each race's emblem a medallion in relief (Q35): a rim, a field and the race's motif raised, in the game's new look; **nine
new motifs** (Q37), each proposed as a sketch first, before any modelling: the six older seals' motifs are not kept (so
T5's "seals that sit with the six older ones" has no six to sit with: the nine are one new set). Shown as pictures where
small (Q36): the sheet's seal button (about 124, 99 or 72 px), a companion's sheet (68 or 52 px), PNGs in `ui.pak` rendered
by Blender from the emblem's script at each size and at 1 and 1.65, regenerable (GP-4) and compared with the engine's
render; and live, slowly turning, only for the race shown in the carousel (Q36, one live view, GM2-P16). Approved as
sketches, then as renders of all nine together.

### Meeting GP-8

The user's word (GP-8) is recorded as said; **what follows is this design's way of meeting it**, on the answers to
Q1–Q6, Q19 and Q40:
- **The bar the answers set:** heroic proportions (Q1); stylised faces with simple eye shapes and mitten hands, no face
  bones (Q2); hair as sculpted locks and clothes with modelled folds, seams, hems, belts and buckles (Q3); a large set of
  parts (Q19, Q40). Detail goes where these answers put it: in the silhouettes, the hair's masses, the clothes' folds and
  the faces' shapes, not in fingers or expressions.
- **Room to iterate:** a mood board, then three candidates for the look and for the first humanoid, two for each later
  family, as many rounds as the user wants (Q4, Q6); every model approved on a full sheet (Q5: turnaround, close-ups of
  face and hands, beside the old sprite, its clips as strips and videos, the playing view); the first humanoid bare, with
  its sword, shield and chain shirt, in four clip batches and in the playing view.
- **Every render looked at critically before it goes to the user:** against the old sprite of the same look, at battle
  size and in the sheet's panel, for silhouette, ink (doubled or missing lines, lines through a face), hatching (stripes on
  small parts), proportions across races and builds, and clipping through clothes in every clip; what is found is fixed
  first.
- **No step trades model quality for a quicker finish:** an art step ends only on the user's approval; a test a better
  model fails is investigated, not met by simplifying the model; the 3D steps are sized honestly ("What GM2 makes",
  "Steps").
- **Where it runs:** Blender only on the user's PC; the 3D steps are the manager's own, with priority over workers; the
  rules side runs meanwhile.

### The data library's kinds and the editor

GM2 adds the library's first real kinds (GM2-P4–GM2-P7): `race` (`data/races/<id>`, its swatches inside it), the effects
as one union (`effect.*`), `actor.sheet` and `look.hero` (in the save, `[NotAPackKind]` like it), the maker's
`look.body`, `look.part` and `look.preset`, and the engine's `render.style` (GM2-P11). Each with `[Kind]`, `[Help]`,
`[Choices]`, `[Colour]`, `[References]` and the `[Check]`s that the game's loader runs too (G4, G5); the editor shows them
in its generic form (M11; special views are GM18-2's question). **Mods** (Q39): a mod can add parts and presets (a part's
model and record; a preset), and they appear in the maker for the races and bodies they name. Whether a mod can add
swatches apart from a race is not asked (GM2-P7's note) and is left for a later question.

### Dev Mode (Q27)

The old powers on the sheet come back under the setting (GM1-3, GM1-12, GM1-13): the attribute fields and "−" steppers,
the permanent bonuses, the purse, the EXP form, levelling without EXP, "Full HP", "Full Mana", "Restore both to full". In
the carousel: the 19 enemy-only races, and another race once chosen, from the sheet's seal button, a playable race
opening the maker again (outside GM2-7, a development tool); an enemy-only hero shown as the engine's stand-in figure
until GM9. The sheet follows the setting live.

### Sounds (GM1-6; Q38)

A small new set, approved by ear as GM1's were: a race chosen, a maker choice changed, the random roll, a point spent, a
level up, a respec, the hero confirmed. Effect patches in `Assets/Audio/effects/`, packed into `audio.pak` by
`make-music-data.sh` (GM1-P16), named in `SoundNames` (its test, `SoundNamesTests.cs` 11–26, extended), one sound per action
(`GameSounds.cs` 28–66). The figure's own sounds (steps, swings) are GM5's. **Made by step 12 (2026-11-15):** the user's
picks by ear, `race-chosen` (the emblem stamped firmly), `maker-choice` (a pattern book's page flicked), `maker-random`
(the pounce pot shaken), `point-spent` (an ink drop), `level-up` (gold leaf), `respec` (the wax tablet smoothed),
`hero-confirmed` (the book clapped shut); which action plays which is GM2-P17 ("How it works", step 12).

### What GM2 makes, with counts

| What | How many | From |
| --- | --- | --- |
| The look | 1 record (palette, sun, lines, hatching), chosen from a mood board and three candidates | GM2-3, GM2-8, Q4–Q7, Q12 |
| The skeleton | 1, with mitten hands' bones, no face bones, the sockets | M7, Q2 |
| Bodies | 54: 9 races × 2 sexes × 3 builds, heroic proportions | Q1, Q14, Q15 |
| Faces | about 108: six per race and sex, stylised | Q2, Q16, Q40 |
| Hair styles | about 16–24 per sex, on the eight races with hair | Q19, Q40 |
| Beards | about 12, where a race's lists offer them | Q19, Q40 |
| Crests | about twice the old 4 (lizardmen; the Merfolk's fin) | Q13, Q19 |
| The Half-Beast's ears and tails | free lists, about twice the old 2 ear shapes and 2 tails, T14's wolf, fox, bear and lynx among them; the lizardman's tails likewise | Q19, Q22 |
| Marks | scars and freckles, the Merfolk's scale patterns, the Goliath's rune patterns, about twice the old 2 kinds, each list with "none" | Q19, Q23 |
| Head pieces | about twice the old circlet and flower, with ties and beads | Q19, Q24 |
| Outfits | 18 (one per race and sex), each fitted to the 3 builds: 54 | Q15, Q18 |
| Swatches | per race: skins, hair, eyes, cloth, marks; from the chosen look | Q17 |
| Presets | 72, the old looks rebuilt (their outfit shapes lost; the Goliath's and the Merfolk's with marks) | GM2-6, Q18–Q22, Q42 |
| Clips | about 28, with markers | M7, Q10 |
| Sample gear | a sword, a shield, a chain shirt | Q9 |
| Emblems | 9 medallions on nine new motifs; pictures at each size and scale; one live view | Q35–Q37 |

The "about" counts are the questions' own; each list is proposed for approval in its step. **The same as work:**

| Work | Made how | Models | Step |
| --- | --- | --- | --- |
| The look | mood board, three candidates, refinement | a blockout figure | 3 |
| The skeleton and sockets | once | 1 | 4 |
| Bodies | the average build modelled per race and sex (18), the slender and strong builds made from it by the script and refined (36) | 54 | 4 (the human average), 9 |
| Faces | per race and sex; one head size per race and sex, fitted by script to the three builds' necks | about 108 | 4 (the first), 9 |
| Hair | each style once per sex, fitted by script to each race's skull | about 40 designs, about 320 fitted | 9a (the set), 9b–9e (fitted) |
| Beards, head pieces, scars and freckles | each once, fitted by script to the faces and skulls that offer them | about 20 designs | 9a, 9b–9e |
| Crests, ears, tails, scale and rune patterns | per race, fitted by script to its builds | a few dozen | 9b–9e |
| Outfits | designed per race and sex, fitted by script to the three builds | 18 designs, 54 fitted | 4 (the human), 9 |
| Clips | once, on the human average body; checked on every body | about 28 | 4 |
| Sample gear | the sword and shield rigid, the chain shirt fitted to two bodies | 3 | 4 |
| Presets | parameter sets, no models; each rendered for approval | 72 | 9 |
| Emblems | sketches, then models and pictures | 9 | 10 |

**Approvals in all,** at the counts above: about 3 rounds for the look (step 3); about 8 for the first humanoid (three
candidates, the bare sheet, with gear, four clip batches, the playing view; step 4); about 2 per race and 1 for the
shared sets (steps 9a–9e, about 17); 2 for the emblems (step 10); the screens' captures (steps 7, 8) and the live figures
(step 11); the sounds (step 12): some 35 stops, each more than once if the user asks for another round.

### Where the answers pull against each other

Each resolved here without a choice the user didn't make; the two that needed one were asked as Q41 and Q42 and answered.
1. **Heroic proportions (Q1) and stylised faces and mitten hands (Q2).** Not a contradiction: a realistic body with a
   simplified face and hands. How the stylised face reads at heroic proportions is what the look's and the first
   humanoid's candidates show (Q6); the doc doesn't fix it.
2. **Faces (Q16) without face bones (Q2).** A face is a modelled head from the list; no sliders, no blinks, no expressions
   (the manager asked Q16 with that note). The head moves as one in the clips.
3. **Mitten hands (Q2) and the sword, shield, bow, crossbow, flask grips (Q9, the clip set).** Every grip is a closed
   block and a thumb; the old sprites had no fingers, so nothing the old game showed is lost ("The humanoid rig family").
4. **Six faces (Q16) and "about twice" (Q19):** settled by the manager's Q40: six, the doubling for the other parts.
5. **One plain outfit, colours only (Q18) and the presets (GM2-6) and the large set (Q19):** the presets lose their old
   outfit shapes (tunic, dress, vest, loincloth) and keep their colours; Q19's doubling doesn't reach the outfit, which
   Q18 fixed at one per race and sex.
6. **No beast choice (Q22) and T2, T14 (looks varying by beast; wolf, fox, bear, lynx, two looks each):** the free lists
   hold the four beasts' ears and tails, and the presets keep their beasts by their parts (the doc's reading of the old
   names, shown for approval); nothing in the maker names a beast.
7. **Optional marks (Q23) and T3, T4 (scales; "visibly giant-blooded … rune-like markings"):** the race still fixes the
   Merfolk's fin ears and webbed hands and the Goliath's stone-grey skin swatches and build (Q13); the scales and runes
   are optional, the later answer; the presets wear them (Q42).
8. **Random rolls everything, no locks (Q21), with optional marks and head pieces (Q23, Q24):** "none" is one option of
   such a list, so a roll can give no marks or no head piece; the draws are uniform, no weights having been asked.
9. **The carousel (Q28) and a live emblem only for the race being chosen (Q36):** the carousel holds two live views, its
   figure and the emblem (GM2-P16 as amended); every other emblem is a picture.
10. **The sheet's figure fixed and idle (Q31) and the carousel's and maker's figures:** Q31 asked about the sheet only;
    Q41 gave the carousel's and the maker's figures the same fixed idle, so the approvals' turnarounds show the backs.
11. **The paper doll (Q29) and the framed dark portrait panel (Q32):** the panel is the paper doll's middle, the slots
    around it as the sketch showed; no ground or plinth (the recommendation of Q32 not taken), so no shadow is seen.
12. **Nine new motifs (Q37) and T5's "seals that sit with the six older ones":** the six are replaced too (GM2-5), so the
    nine are drawn as one new set.
13. **Three builds (Q15) and GM3's armour:** each armour piece is fitted to 54 bodies (9 races, 2 sexes, 3 builds),
    noted for GM3-3, which is asked on that fact.

## The questions as asked (Q1–Q42, answered 2026-11-15)

Prepared by this doc (Q1–Q39) and asked by the manager with the question tool on 2026-11-15, four at a time, in this
order, as written here (the notes at Q16 and Q29 say what the manager added when asking); Q40 the manager's follow-up; Q41 and Q42, which the
answers opened, prepared by this doc and asked the same day.
The recommended option first, with its reason; each answer after its question and in "Decisions" above. Twelve answers
are not the recommendation: Q1, Q2, Q9, Q18, Q19, Q21, Q22, Q23, Q28, Q31, Q32, Q37.

**Round 1: what "very good" means (GP-8); every art step depends on these.**

**Q1. The figures' proportions.** The old figures stand about six heads tall (a human 101 units feet to crown, its head
17), dwarves about five and hobbits under five, broader; Goliaths a head taller than humans (T4) and elves slimmer. The
port's figures are new 3D models with the old races' differences kept or changed.
- (a) **The old proportions:** about six heads for a human, each race's old ratios (the dwarf's and hobbit's shorter
  bodies and larger heads, the elf's long legs, the Goliath's height and bulk) modelled in 3D. *Recommended:* the races
  keep the silhouettes the player knows, and faces stay readable at battle size.
- (b) **Heroic, realistic:** about 7.5 heads, smaller heads and longer limbs; the races' differences by the same ratios.
  Faces read smaller in battle and on the sheet.
- (c) **Stylised, big heads:** about four to five heads, large faces and hands; closer to a cartoon.
- **Answer (the user, 2026-11-15): (b) Heroic, realistic, not the recommendation.**

**Q2. How much the faces and hands carry.** The engine draws one colour per material and no textures, so eyes, brows,
lips and freckles are modelled pieces with their own colours; everything moves by bones (no shape keys, M7), so a
blink or an expression needs face bones; each vertex follows at most four bones.
- (a) **Modelled faces and hands:** eyes with lids, irises and pupils as geometry, shaped brows, nose, lips and ears; a
  few face bones (jaw, lids, brows) for blinks and expressions in the clips; hands with four fingers and a thumb on
  finger bones, posed for each grip. *Recommended:* the sheet and the maker show the face and hands up close, and GP-8
  asks for them to look very good. Costs the most bones and the most modelling per body.
- (b) **Stylised:** simple eye shapes, no face bones, mitten hands (a thumb and one block).
- (c) **Modelled faces, mitten hands:** faces as (a), hands as (b).
- **Answer (the user, 2026-11-15): (b) Stylised, not the recommendation.**

**Q3. Hair and clothing detail.** No textures and no see-through materials exist in the engine; ink lines appear at
silhouettes and sharp creases, so a lock of hair or a fold shows as a line only if it is modelled.
- (a) **Sculpted masses:** hair as modelled locks and clumps whose creases the ink draws; clothes with modelled folds,
  seams, hems, belts and buckles. *Recommended:* reads well in the cel and ink look with no engine work.
- (b) **Hair cards:** hair as strips with strand textures and cut-out edges, as many games do. Needs see-through textures
  in the renderer, ported against Blender first (engine work, weeks).
- (c) **Smooth and simple:** hair and clothes as plain shapes, few folds.
- **Answer (the user, 2026-11-15): (a).**

**Q4. References for the new look.** GM2-3 and GM2-8 opened the palette, the light, the line widths and the hatching;
candidates are shown on the first humanoid. The old game's look is muted umber colours, ink outlines and hatching on
the shadow side, a gothic ink style.
- (a) **A mood board first:** about six small renders of different directions on a rough figure (darker or brighter,
  warmer or colder, heavier or lighter lines and hatching); you pick one or two, then full candidates follow.
  *Recommended:* you steer with pictures before the costly candidates, and need no references of your own.
- (b) **Your references first:** you name games, artists or pictures; the candidates follow them.
- (c) **No references:** three candidates proposed straight away, starting from the old gothic ink style.
- **Answer (the user, 2026-11-15): (a).**

**Round 2: how the art is approved.**

**Q5. What you judge each model by.** M7's rule: the first humanoid is approved "with gear and its full clip set, shown
as renders and a playing view" before the rest.
- (a) **A full sheet per model:** a turnaround (front, ¾, side, back) in the game's look, close-ups of face and hands, the
  model beside the old game's sprite of the same look, each clip as a frame strip and a looping video, and the playing
  view in the engine. *Recommended:* GP-8: every angle and every motion seen before it's built on.
- (b) **Turnaround and playing view only.**
- (c) **The playing view only.**
- **Answer (the user, 2026-11-15): (a).**

**Q6. How many candidates and rounds.**
- (a) **Three candidates** for the look and for the first humanoid's face and body, refined over as many rounds as you
  want; two for each later family of parts. *Recommended:* a real choice without a flood of renders.
- (b) **One proposal at a time,** changed until you approve it.
- (c) **Four or more candidates** each time.
- **Answer (the user, 2026-11-15): (a).**

**Q7. What the new look may change in the renderer.** Today: two tones per colour (lit, and a shadow mixed toward the
ink), one white sun per view, ink lines of two widths, straight hatching in the shadow; every number set per view and
checked against Blender. "Everything open" (GM2-8) can mean the numbers or new rules too.
- (a) **Today's rules, any numbers:** any palette, the shadow's mix and edge, the sun's direction, strength and softness,
  the lines' colour and widths, the hatching's spacing, width, angle and strength; a new rule only when you ask for one
  after seeing the candidates (then an engine task of its own). *Recommended:* every candidate renders now and is
  checked against Blender as today; nothing is built that may not be chosen.
- (b) **New rules allowed from the start:** candidates may use a third tone, a rim light, coloured or ambient light,
  lines thinning with distance, other hatching; each is ported against Blender before it's shown. Engine work of weeks
  before the first candidate.
- (c) **The light only reopened:** today's rules, plus coloured light and an ambient term.
- **Answer (the user, 2026-11-15): (a).**

**Q8. Which body is approved first.** M7: the first humanoid is approved before any other body or part.
- (a) **A human man and a human woman together** (the base the other races are built from, both sexes since builds and
  clothes come from both). *Recommended:* the second body would otherwise reopen the first's rig and clips.
- (b) **A human man alone,** the woman after.
- (c) **A race you name.**
- **Answer (the user, 2026-11-15): (a).**

**Round 3: the first humanoid's gear and clips; the look's place.**

**Q9. The sample weapon and armour piece for the first humanoid** (M7: approved "with gear"; GM3 makes every item).
- (a) **A one-handed sword and a light (leather) armour**, kept as GM3's first designs if you like them. *Recommended:*
  exercises the hand socket, a fitted garment and the one-handed clips.
- (b) **A sword and shield with a chain shirt:** also the off-hand socket and a heavier garment.
- (c) **A staff and a robe:** the casting clips and a long skirt.
- **Answer (the user, 2026-11-15): (b) A sword and shield with a chain shirt, not the recommendation.**

**Q10. How the clips are shown.** About 28 clips (idle, stunned; advance, return, flee; seven attacks; bow, crossbow;
cast, skill, item, drink, meditate; guard, evade; hurt, critical hurt, stumble; death, victory).
- (a) **In four batches** (moving and reacting; melee attacks; ranged, casting and items; guard, evade, death,
  victory), each as frame strips and looping videos from the battle's side and from ¾, and in the engine's playing view,
  played on the human and on the smallest and largest bodies. *Recommended:* each batch fixed before the next builds on it.
- (b) **All at once.**
- (c) **One clip at a time.**
- **Answer (the user, 2026-11-15): (a).**

**Q11. When the engine's earlier models take the new look** (GM2-8: "later"). Ji'ruan's cutscene stage bakes today's
look into its colours; the engine's 56 Blender test references use test values, which the docs call test data.
- (a) **When they are used:** Ji'ruan's stage takes the new look when GM14 reuses it (GM14-1) and is approved again
  then; the test references keep their test values. *Recommended:* no rework before a use.
- (b) **Right after GM2,** as an engine task: the stage redone and approved, the references made again.
- (c) **Within GM2.**
- **Answer (the user, 2026-11-15): (a).**

**Q12. Where the game's look is kept.** The engine holds a look's numbers in a code object per view; nothing in the data
holds a look or a palette yet.
- (a) **In the game's data:** one record for the look (its numbers and the palette's colours), used by every 3D view,
  shown in the editor, so a mod can restyle the game. *Recommended:* the look is data like everything else the port
  makes (2026-10-16: "everything else is binary `.pak` files"); a small engine change.
- (b) **In the program:** constants, not moddable.
- **Answer (the user, 2026-11-15): (a).**

**Round 4: the maker's model.**

**Q13. What the race fixes and what you choose.** In the old game each race fixes its body and traits (the elf's long
ears and narrow eyes, the lizardman's head, crest place, tail and claws, the merfolk's fins, the half-beast's beast
ears and claws, the dwarf's and Goliath's big nose and build); its looks vary skin, hair, beard, eyes, crests, marks and
clothes.
- (a) **As the old game:** the race fixes its body and defining traits; you choose the rest from the race's own lists.
  *Recommended:* every race stays recognisable (T2–T4).
- (b) **Traits as lists within the race:** several ear shapes for elves, several crest places for lizardmen.
- (c) **Any part on any race.**
- **Answer (the user, 2026-11-15): (a).**

**Q14. Sex and body.** The old game has a male and a female body per race (the woman's shoulders and arms narrower,
hips wider).
- (a) **Male and female,** each its own body per race, as the old game. *Recommended:* faithful; armour is fitted per body.
- (b) **A slider** from masculine to feminine.
- (c) **Male and female, plus a body choice not tied to sex.**
- **Answer (the user, 2026-11-15): (a).**

**Q15. Build and height.** M7 ruled out shape keys; clothes and armour are fitted to the body they were made on and
don't follow other proportions.
- (a) **A few builds per race and sex** (slender, average, strong), each a modelled body; GM3 fits each piece of armour to
  each build. *Recommended:* every body modelled to the bar, every piece fitting. Each build adds a body and its fitted
  clothes and armour (GM3-3 is asked on that fact).
- (b) **Sliders** for height and build, done by bone scales; clothes follow the bones, fitting roughly; engine work (a
  build layer under the clips).
- (c) **One body per race and sex,** no build choice.
- **Answer (the user, 2026-11-15): (a).**

**Q16. Faces.**
- (a) **A list of modelled faces** per race and sex (about six), with eye colour, brows and marks (scars, freckles, a
  race's markings) as lists of their own. *Recommended:* each face modelled to the bar.
- (b) **Face sliders** (jaw, nose, eyes) through face bones; less control over how each looks.
- (c) **Both.**
- **Answer (the user, 2026-11-15): (a).** Asked with the note that Q2 left face bones out, so sliders would have brought them back.

**Round 5: the maker's choices.**

**Q17. Colours.** The 72 old looks use 68 skin, 43 hair and 32 eye colours, all fixed values; the new palette is GM2-3's.
- (a) **Swatches per race** (skins, hair, eyes, clothes) from the new palette, each known to look right in the cel
  bands. *Recommended:* every combination looks right in the game's light.
- (b) **A free colour picker** for every colour.
- (c) **Swatches and a free picker.**
- **Answer (the user, 2026-11-15): (a).**

**Q18. What the hero wears under no armour.** Every old look has an outfit (a tunic, a dress, a vest with a shirt, or the
lizardman's loincloth, with trousers and colours); GM3 makes the armour.
- (a) **Clothes as choices:** a few garments per race and sex (tunic, dress, vest, loincloth …) in chosen colours, worn
  when no armour covers them. *Recommended:* the presets need them, and the hero isn't bare before GM3.
- (b) **One plain outfit per race and sex,** colours only.
- (c) **None of their own:** GM3's armour only.
- **Answer (the user, 2026-11-15): (b) One plain outfit per race and sex, colours only, not the recommendation.**

**Q19. How many parts.** The old 72 looks use 16 hair styles, 5 beards, 4 crests, 3 tails, 3 eye shapes, 2 head pieces,
4 outfits.
- (a) **Every part the old looks use, remade, plus a few new per category** where a race has few (about 8–12 hair
  styles per sex, 6 beards, 6 faces per race and sex). *Recommended:* the presets rebuilt and real choice beyond them.
- (b) **Only what the old looks use.**
- (c) **A large set,** about twice (a); GM2's art grows with it.
- **Answer (the user, 2026-11-15): (c) A large set, about twice (a), not the recommendation.**

**Q20. Names in the maker.**
- (a) **Each option named** ("Braid", "Ash grey"), the presets keeping the old labels (Wayfarer, Sellsword …).
  *Recommended:* as the old looks are named.
- (b) **Numbered** ("Hair 3").
- **Answer (the user, 2026-11-15): (a).**

**Round 6: presets, random, the three remade races.**

**Q21. Presets and the random button** (GM2-6).
- (a) **A row of the race's eight presets** (the old labels, four per sex) above the choices: picking one sets every
  choice; the random button rolls every choice the race allows, with a lock per category to keep a choice while rolling
  the rest. *Recommended:* quick starts and controlled rolls.
- (b) **Presets as a list,** random rolls everything, no locks.
- (c) **(a) without locks.**
- **Answer (the user, 2026-11-15): (b) Presets as a list, random rolls everything, no locks, not the recommendation.**

**Q22. The Half-Beast's beast** (T2: "one race whose looks vary by beast"; T14: wolf, fox, bear, lynx).
- (a) **The beast as the first choice** (wolf, fox, bear, lynx), setting the ears, tail and fur colours it allows; the
  rest chosen as for any race. *Recommended:* T2's shared trait, each beast clearly shown (T5's remake).
- (b) **Ears, tail and fur chosen freely,** no beast choice.
- **Answer (the user, 2026-11-15): (b) Ears, tail and fur chosen freely, no beast choice, not the recommendation.**

**Q23. The Merfolk's and the Goliath's marks** (T3: fin ears, scales, webbed hands; T4: stone-grey skin, rune-like
markings; the old placeholders draw neither scales nor runes).
- (a) **Always present, chosen from patterns:** Merfolk fin-ear shapes and scale patterns, Goliath rune patterns and
  stone tones, as lists of the race. *Recommended:* T3 and T4 make them the races' marks.
- (b) **Optional:** "none" among the patterns.
- **Answer (the user, 2026-11-15): (b) Optional, "none" among the patterns, not the recommendation.**

**Q24. Small head pieces.** Two old looks wear a circlet, two a flower; ties and beads hold braids.
- (a) **Small head pieces as choices** (circlets, flowers, ties, beads), hidden under a helmet. *Recommended:* the presets
  need them.
- (b) **None.**
- **Answer (the user, 2026-11-15): (a).**

**Round 7: the new hero's flow.**

**Q25. The order of the steps.** GM1 asks the name first, then opens the character ("GM2 puts the race picker after it").
- (a) **Name, race, maker, sheet.** *Recommended:* as GM1 decided.
- (b) **Race, maker, then the name** on the maker's last page.
- (c) **The name field inside the maker.**
- **Answer (the user, 2026-11-15): (a).**

**Q26. Leaving the flow before it is finished.** The old game let you close the race dialog; the sheet then said
"Choose race" until you chose, and a race could be chosen without a look. GM2-7: the look is changed only when the hero is
made.
- (a) **The sheet opens only with a race and a look:** the flow can be left only for the start screen, and an unfinished
  hero opens on the flow again. *Recommended:* no hero without a look; GM2-7 stays simple.
- (b) **As the old game:** the sheet opens with "Choose race", the seal button reopens the flow, no figure until then.
- (c) **Leaving deletes the new character.**
- **Answer (the user, 2026-11-15): (a).**

**Q27. Dev Mode in the picker and the maker.** The old Dev Mode showed the 19 enemy-only races and let the race be changed
later; an enemy-only race had no look step.
- (a) **The old powers:** the enemy-only races listed, the race changeable any time, a playable race changed in Dev Mode
  opening the maker again (a development tool outside GM2-7); an enemy-only hero shown as the engine's stand-in figure
  until GM9 makes those bodies. *Recommended:* faithful to Dev Mode's purpose.
- (b) **Race only:** Dev Mode changes the race but never the look.
- (c) **No race powers** in Dev Mode.
- **Answer (the user, 2026-11-15): (a).**

**Q28. The race picker's look.**
- (a) **The old dialog with the new emblems, and a turning figure** of the race's first preset beside the list.
  *Recommended:* a race is chosen by how it looks too.
- (b) **The old dialog with the new emblems only.**
- (c) **A carousel,** one race at a time with a large figure.
- **Answer (the user, 2026-11-15): (c) A carousel, one race at a time with a large figure, not the recommendation.**

**Round 8: the sheet with the figure.**

**Q29. Where the figure stands** (GM2-2: beside the equipment slots; the layout changes to make room). Today the slots are
two rows (the two hands; armour and two artifacts) under the attribute area, the rail on the left; the window opens at
1440 × 950, at least 820 × 600.
- (a) **A paper doll:** the figure in the middle of the equipment section, the hands' slots at its sides, armour and the
  artifacts below it. *Recommended:* the slots read as worn.
- (b) **A column:** the figure beside the two rows, the rows as they are.
- (c) **At the top:** the figure by the hero's heading, the slots below as today.
- **Answer (the user, 2026-11-15): (a).** Asked with a small layout sketch: the figure in the middle of the equipment section, the two hands' slots at its sides, armour and the two artifacts below it.

**Q30. The hero's name on the sheet.** The old hero had no name; the sheet's heading is "Character *sheet*" under "RPG /
PERSONAL RECORD". GM1 gave every character a name, shown so far only in the slot list.
- (a) **The name as the heading,** in the old heading's style, with the race and level under it. *Recommended:* the
  character the page belongs to, at a glance.
- (b) **"Character sheet" kept,** the name in the small line above it.
- (c) **No name on the sheet.**
- **Answer (the user, 2026-11-15): (a).**

**Q31. What the figure does.**
- (a) **Idles,** the mouse turns it, it turns back slowly when let go; plays its victory clip once on a level up.
  *Recommended:* alive and responsive. Renders every frame while shown.
- (b) **Idles** in a fixed ¾ view, nothing else.
- (c) **A still pose,** rendered only when something changes.
- **Answer (the user, 2026-11-15): (b) Idles in a fixed ¾ view, nothing else, not the recommendation.**

**Q32. How the figure is set.** Characters receive no shadows (M6); scenery does.
- (a) **On a small stone plinth** that takes its shadow, over the page's parchment, lit by the game's sun. *Recommended:*
  grounded, and the shadow shows the look's light.
- (b) **In a framed portrait panel** with a dark backdrop.
- (c) **Floating** over the parchment.
- **Answer (the user, 2026-11-15): (b) A framed portrait panel with a dark backdrop, not the recommendation.**

**Round 9: the sheet before GM3; the emblems.**

**Q33. The figure before GM3 makes gear.**
- (a) **In its maker's clothes,** the five slots empty and inert until GM3. *Recommended:* nothing shown that isn't
  final (GP-3).
- (b) **Wearing the sample sword and armour** of the approval as stand-ins until GM3.
- **Answer (the user, 2026-11-15): (a).**

**Q34. The Field tools rail in GM2.** It holds the purse, the companions (GM11) and eight tools: Map (GM4), Combat test
(GM5, Dev Mode), Spell book (GM7), Inventory (GM3), Companions (GM11), Classes, Skills, Passives (GM8).
- (a) **The whole rail,** each tool shown and disabled until its milestone. *Recommended:* the layout final now; later
  parts inert until their milestones (GP-3), as the top bar's "Return to combat".
- (b) **The purse only;** tools added as their milestones come.
- (c) **No rail** until GM3.
- **Answer (the user, 2026-11-15): (a).**

**Q35. What a 3D emblem is** (GM2-5). The old seals are round medallions: a gold rim with tick marks, a dark field, the
race's motif in parchment with hatched shadows and a coloured glow (Human a shield with a cross under a crown; Elf a leaf
and a moon; Dark Elf a crescent, a dagger and a star; Dwarf mountains, an anvil and a hammer; Hobbit an acorn and leaves;
Lizardman a scaled diamond with a slit eye; the three placeholders a mountain, a paw, waves).
- (a) **A medallion in relief:** the rim, the field and the motif raised, in the game's look. *Recommended:* the seals'
  design carried into 3D.
- (b) **A free-standing object** per race (a carved leaf, an anvil and hammer …).
- (c) **A heraldic shield** per race.
- **Answer (the user, 2026-11-15): (a).**

**Q36. Where the emblems are 3D and where pictures.** Seals show at 56 px (the race list), about 124, 99 or 72 px (the
sheet's seal button) and 68 or 52 px (a companion's sheet). Each live 3D view uses 64 MB of video memory and draws every
frame it moves.
- (a) **Pictures** rendered from the emblem models wherever they are small; a live, slowly turning emblem only for the race
  being chosen. *Recommended:* crisp at every size, little cost.
- (b) **Live 3D everywhere.**
- (c) **Pictures everywhere.**
- **Answer (the user, 2026-11-15): (a).**

**Round 10: motifs, sounds, mods.**

**Q37. The emblems' motifs.**
- (a) **The six older motifs kept;** the Half-Beast, Goliath and Merfolk given new motifs in their spirit, proposed as
  sketches first. *Recommended:* "seals that sit with the six older ones" (T5's remake).
- (b) **Nine new motifs.**
- (c) **The placeholders' motifs refined** (mountain, paw, waves).
- **Answer (the user, 2026-11-15): (b) Nine new motifs, each proposed as a sketch first, not the recommendation.**

**Q38. Sounds for GM2's screens** (GM1-6; GM1's eight are paper and ink).
- (a) **A small set:** a race chosen, a maker choice changed, the random roll, a point spent, a level up, a respec, the
  hero confirmed; approved by ear. *Recommended:* the sheet's main actions heard, as GM1's were.
- (b) **GM1's eight only.**
- **Answer (the user, 2026-11-15): (a).**

**Q39. Parts and presets in mods.**
- (a) **Data a mod can add to:** a mod can add a hair style (its model and record) or a preset, and it appears in the
  maker for the races it names. *Recommended:* the maker grows with mods as every other kind does.
- (b) **Fixed in the game.**
- **Answer (the user, 2026-11-15): (a).**

**Q40 (the manager's follow-up, 2026-11-15, because Q16's "about six faces" and Q19's "about twice" met).** How many
faces in the large set.
- (a) **About six faces per race and sex, 108 in all,** the large set doubling the other parts (hair about 16–24 styles
  per sex, about 12 beards, more crests, tails, marks and head pieces). *Recommended.*
- (b) **About twelve,** 216 in all.
- (c) **About nine,** 162 in all.
- **Answer (the user, 2026-11-15): (a).**

### Questions the answers opened (Q41–Q42, answered 2026-11-15)

Prepared by this doc after the detail round and asked by the manager with the question tool the same day; both answers
the recommended option.

**Q41. The figures in the carousel and the maker.** For the sheet you chose a figure that idles in a fixed three-quarter
view and does nothing else (Q31); the race carousel shows one race at a time with a large figure (Q28) and the maker
shows the hero being built, from the front of the face to the back of the hair.
- (a) **As the sheet's:** each idles in a fixed three-quarter view. *Recommended:* one rule for every figure outside battle,
  as Q31 chose.
- (b) **The maker's turnable:** the mouse turns the maker's figure (to see the back of hair, tails, the outfit), turning
  back when let go; the carousel's fixed.
- (c) **Both turning slowly** on their own.
- **Answer (the user, 2026-11-15): (a).**

**Q42. The Goliath's and the Merfolk's presets and their marks.** You made the marks optional, "none" among the patterns
(Q23). Task 11 asked for Goliaths "visibly giant-blooded (stone-grey skin, rune-like markings)" (T4) and Merfolk with
"fin ears, scales and webbed hands" (T3); the old placeholder looks draw neither runes nor scales, and the presets are
those looks rebuilt.
- (a) **The presets wear marks:** each Goliath preset a rune pattern and each Merfolk preset a scale pattern, chosen to
  suit its old look. *Recommended:* T3's and T4's remake shows where most players start, while any hero may still choose
  none.
- (b) **The presets without marks,** as the old looks are.
- **Answer (the user, 2026-11-15): (a).**

## The proposals as made (GM2-P1–GM2-P16, decided 2026-11-15)

Internal and tooling choices that change nothing the player or a modder meets beyond what the questions ask; the
recommendation first, with its reason. The manager's decisions (2026-11-15) are under "Decisions": every one (a), with
notes on GM2-P7, GM2-P10 and GM2-P16.

- **GM2-P1 Where the actor record and its rules live.** (a) In the data library (`Game/Statventure.Game.Data/Actors/`):
  the record and its rules as plain code with no UI or engine graphics, so the game, its tests, the oracle comparisons
  and the editor's checks (GM9's enemy files) use one implementation. (b) The record in the library, the rules in the
  program.
- **GM2-P2 The rules' form.** (a) Functions of an immutable sheet that return a new sheet (spend, respec, level up, change
  a resource, Dev Mode's edits) and values computed on demand from the sheet, the races and the effect totals (no cache
  until a measurement asks for one); the session's `Change` saves each result; JavaScript's rounding kept in one small
  helper (`Math.round`'s halves toward +∞). (b) A mutable sheet object with methods, as the old globals were.
- **GM2-P3 Effects.** (a) One union `effect.*`, a subtype per old type string with its fields (the 22 race types now),
  sheet-side totals per source (`RaceTotals`), the battle side added by GM5; the old type strings as the subtypes'
  names. (b) One generic effect record (`type`, `amount`, `target`, … as optional fields), interpreted by a switch.
- **GM2-P4 Data and packs.** (a) A new `data.pak` (races, the maker's records) and `models.pak` (the humanoid family's
  models and clips, the emblems), each made by a script from committed sources (`Assets/Data/…` as JSON forms written
  from `races.json` by a converter; `Assets/Models/humanoid/*.glb` from Blender), reproducible byte for byte as `ui.pak`
  is (P30 of M11 holds until an editor-made entry is committed); both in the packages and their listings. (b) One pack
  for both.
- **GM2-P5 `save.game` v2.** (a) Field 2 `hero` (`actor.sheet` v1), field 3 `look` (`look.hero` v1), version 1 frozen as
  `SaveGame.V1`, `Upgrade()` giving no race and no look; the layout tests and fixtures of both versions (the guard tests
  `LibraryRecordLayoutTests.cs` 21–27 and 66–71 changed). (b) The hero's fields directly on `save.game`.
- **GM2-P6 The look record.** (a) `look.hero` v1: `race`, `sex`, `body` (a `look.body` reference), `parts` (a map from
  category to a `look.part` reference), `colours` (a map from palette slot to a swatch or a colour), `preset` (for
  display); its `[Check]`s with the data. (b) A look as a preset reference plus overrides.
- **GM2-P7 The maker's kinds.** (a) `look.body` (its model, race, sex, build), `look.part` (its model per body, category,
  races and sexes, palette slots), `look.preset` (per race and sex, the old label), the race's swatches as part of
  `race` (or `look.swatches` if Q39 lets mods add them apart); the palette slot names fixed in one list both the scripts
  and the game read (`skin`, `hair`, `hair.tie`, `eyes`, `brows`, `lips`, `cloth`, `cloth.trim`, `trousers`, `crest`,
  `marking`, `claws` …), distinct from GM3's gear slots. (b) The parts listed inside each race.
- **GM2-P8 Assembling a figure.** (a) One `Actor` per figure: the body model, every part made on that body as an
  `ActorGarment` (no refit needed) and rigid parts on sockets as `ActorItem`s; a new actor when the body changes; the
  palette built from the look's colours; no engine change. (b) One merged model per look, built at run time (engine
  work: merging meshes and skins).
- **GM2-P9 The Blender scripts.** (a) A package `Tools/Models/humanoid/` (`rig.py`, `bodies/`, `parts/`, `clips/`,
  `emblems/`, `gear.py`, `look.py` with the slot names and the chosen look's numbers, `assemble.py`, `build.py` with
  `--check`), outputs in `Assets/Models/humanoid/`, run by `Tools/Scripts/make-humanoid.sh` (on the PC, Blender 5.2,
  headless); the references (renders and evaluations of bodies, parts, clips, assembled looks and emblems) by
  `make-blender-references.sh`'s scene files as today, the game's scenes written by the game's tests. (b) The game's
  models added to `make_models.py`.
- **GM2-P10 Blender's version.** The PC's Flatpak is 5.3 since an update (checked 2026-11-15); the engine's rules and every
  reference are 5.2's. (a) Pin Blender 5.2: the official 5.2 Linux build in `~/.cache/stat-fick-build/blender-5.2/`, run
  through `$STATVENTURE_BLENDER` (which the scripts already honour), set in `Tools/Scripts/env.local.sh`; a move to 5.3 is
  an engine task of its own (every reference rendered again and compared). (b) Pin the Flatpak to its 5.2 commit
  (`flatpak update --commit=…`; the next update moves it again). (c) Move to 5.3 now, in GM2's first art step.
- **GM2-P11 The game's look in the engine** (if Q12 is (a)). (a) `CelStyle` and the sun's settings as a record in the
  engine (`render.style`, with `[Kind]`), the game's one entry in `data.pak`, every game view taking it; the engine's
  test scenes keep `CelStyle.Default`. (b) The game's own record converted to `CelStyle` in the game.
- **GM2-P12 3D views in the game's screen fixtures.** (a) The fixture harness masks every `surface` box (as it masks the
  icons' boxes), and each view is tested on its own against Blender. (b) Each fixture draws a flat stand-in in the view.
- **GM2-P13 The oracle tooling.** (a) `Tools/OldGameOracle/`: a Node runner for the pure modules (the cache's Node 24,
  found by `env.sh`) and test-client scripts with a case list each, results in
  `Tests/Statventure.Game.Tests/Reference/old-game/*.json`, `Tools/Scripts/make-old-game-references.sh [module …]`;
  later milestones add their modules and cases. (b) A script per milestone.
- **GM2-P14 The playing view for approvals.** (a) The sandbox gets a figure mode (`--figure <look.json or glb>
  [--clips <glb>]`: the figure in the game's look playing its clips in turn, keys to step and turn; `--dump <folder>` for
  frames), so the user sees the engine's own playing view before the game's screens exist. (b) The game's maker screen
  is built first and used for it.
- **GM2-P15 Randomness helpers.** (a) The first helper on `IGameRandom` with the random button: a uniform pick and a
  weighted pick, ported as `Math.floor(Math.random() * n)` reads (GM1-P11's form). (b) The maker draws with `Next(n)`
  only.
- **GM2-P16 Live views and memory.** (a) A screen keeps at most one live 3D view at a time (the sheet's figure, or the
  maker's, or the picker's preview), each released when its screen hides; the 64 MB shadow atlas and the per-frame cost
  measured on the GPU and lavapipe and reported. (b) Views kept for the session.

## Left to the steps

Small internal choices, each recorded by the step that makes it; a step that finds one players would see stops and asks.
- The rules' classes beyond the outline; the totals' fields per source; the names of the commands the screens add
  (`open-races`, `open-maker` …) and of the log lines the end-to-end flow reads.
- The exact case lists of the oracles (every race, levels 1–100 and a spread of attributes and bonuses, at least).
- The bone list beyond the minimum and the sockets GM3 asks for; bone roll and naming within M7's rules.
- The parts' palette slot names beyond GM2-P7's list; the swatches' values (from the approved look).
- The scenes and sample looks of the Blender comparisons; the seeds of the random looks compared.
- The sheet's, picker's and maker's markup and classes beyond the old ones, shown as captures (GM1-8).
- A step whose screen meets an engine item ("Engine items the port will meet" in `Docs/Game-Plan.md`) stops and reports.

## Steps

Final (2026-11-15). Each step ends as "How each step is finished" says (`Docs/Game-Plan.md`): its tests, the full suite
(both test projects) on the GPU and on lavapipe under the shared lock inside a headless compositor, a mutation list for
every rule it adds, its entry under "How it works" and Status here, its Progress line, `Docs/Notes.md`. At most four
workers at once, each in its own worktree; one full run or mutation run at a time. Workers don't touch Engine-Plan's
"Current task" (the merge worker does). Generated files at merges as GM1-P19 (`data.pak`, `models.pak`, `ui.pak`,
`audio.pak`, `Reference/game-screen-fixtures.json`: the step merged second reruns the script or the tests that make it).

**Steps with an approval stop twice or more:** first what the user must see (renders, turnarounds, close-ups, strips,
videos, captures, sounds) under `/var/home/Vatrix33/.cache/stat-fick-build/claude-work/gm2/step<k>/approval/` (`step3`,
`step9b` …), reported to the manager, who shows the user; then, with the verdict, the tests and the rest. An art step
stops as often as its rounds need (Q6): a change the user asks for is made and shown again before any test is written on
it. Every model is shown on Q5's full sheet: a turnaround (front, ¾, side, back), close-ups of face and hands, beside the
old game's sprite of the same look, its clips as frame strips and looping videos (the battle's side view and ¾), and the
playing view.

**The 3D steps (3, 4, 9a–9e, 10, and the Blender sides of 5 and 11) are the manager's own,** in Blender 5.2 on the user's
PC only (GM2-P10), with priority over workers; sized for iteration (GP-8): most of GM2's weight. **The rules side and the
screens (1, 2, 6, 7, 8, 12) run beside them,** on stand-ins where the art isn't there yet (the engine's stand-in figure,
an empty panel, placeholder emblem pictures), never shipped (GP-3).

1. **The races, the effect records and `data.pak`; the stand-in kind removed** (rules; anywhere).
   - Owns: `Game/Statventure.Game.Data/Races/` and `Effects/` (the `race` kind with its swatches' field, the `effect.*`
     union with the 22 race types, `RaceTotals` and the sheet-side fold), the converter from `old-game/objects/races.json`
     and its committed JSON forms, the data pack script and `Assets/Packs/data.pak` (GM2-P4), the packaging listings; the
     removal of `StandIn/` and the switch of every test and flow that used it (the editor's game tests,
     `GameFolderModTests`, `end_to_end.py`'s mod, the game's mod and settings tests, the `settings-mods-*` goldens
     captured again).
   - Must not touch: the save, the screens, the engine's renderer, art.
   - Finished: the 28 races load from `data.pak`, their checks refuse what `normalizeRaceDefinition` refuses; the editor
     shows `race` entries; the sheet-side fold of every race at levels 1–100 against the old game (step 2's oracle, or a
     case list of its own first). No approval.
   - **Done (2026-11-15/16, the branch `gm2-step1`; "How it works", step 1).** Its choices: `race` with `order` (Q43) and
     `emblem` (Q44) and an empty swatches list; the union's 23 subtypes (the 22 race types and `trap-limit-flat`, which
     the fold reads); no limits or choices the old loader didn't have, the new fields' forms checked (colours, the
     emblem's name); the converter in C# in the game's tests (the forms written by the serializer), its output
     `Assets/Data/data/races/`, the pack always made afresh; a case list of its own in step 2's case form, with crafted
     races and levels 0 and enemy count −1 for the guards; `RaceTotals` without the old `passives` list (the race's own);
     the broken mod of `settings-mods` holding two refused races (the manager's); `list-marks` made 700 px tall.
2. **The actor record, its rules and the oracles** (rules; anywhere for the tests; the oracles on the PC or under Xvfb).
   After 1 (its oracle tooling may start beside 1).
   - Owns: `Game/Statventure.Game.Data/Actors/` (`actor.sheet` and its rules, GM2-P1, GM2-P2), `Tools/OldGameOracle/`,
     `make-old-game-references.sh` and the committed results (GM2-P13), the port tests named after the old checks
     (`runLevellingCurveChecks`, the Level icon's, `checkNewRaceData`, the sheet's part of `checkGiantsBlood`).
   - Must not touch: the save, the screens, art.
   - Finished: every rule matches the old game on the named cases (Node for `CombatRules` and `LimitlessRules.cap`, the
     test client for `app.js`'s rules); each mutation killed. No approval.
   - **Done (2026-11-15/16, the branch `gm2-step2`; "How it works", step 2).** Its choices: the oracle's modules (a case
     list and a `run` each, Node or the test client, results committed one case a line, a test failing when the old game
     changed under them); step 1's race oracle folded into it at the rebase (one oracle, one way to make it); hand-made
     totals for the sources GM2 lacks and crafted races in the oracle's sheets, so every term of the formulas is checked;
     `actor.sheet` with whole attributes, points, HP, Mana and coins, EXP and bonuses doubles, no `[Check]` (step 6's
     question); the rules on step 1's keys, value types, fold and `JsValues` (one of each); Dev Mode's fields refusing
     outside it; `ChooseRace`'s refusals as reasons. Found: a new hero's HP is 31, not 30.
3. **Blender 5.2, the art's tooling and the look** (3D, the manager; the PC).
   - Owns: the official Blender 5.2 Linux build in the build cache and `$STATVENTURE_BLENDER` in `env.local.sh`; **first,
     before any new art, the proof that the engine's 56 render references and its evaluations reproduce with it**
     (GM2-P10 as decided); `Tools/Models/humanoid/`'s frame (GM2-P9: the package, `build.py --check`, the scene writer);
     the sandbox's figure mode (GM2-P14); a blockout figure (a heroic body, a stylised head, an outfit, a sword); the mood
     board, the three candidates and the chosen look's numbers in `look.py`.
   - Must not touch: the engine's test references and `CelStyle.Default` (Q11), the game's screens.
   - Approval stops: the mood board, about six directions (Q4); the three candidates (Q6), each in the side view and a ¾
     close-up with its stylised face; the refinement, as many rounds as the user wants. Files in `claude-work/gm2/step3/approval/`.
   - Finished: the look approved and written down (palette, sun, lines, hatching); the figure mode playing a test figure
     in it; the tooling's checks.
   - **Split in two (the manager's decision, 2026-11-15):** **3a**, everything of the step that is tooling and code
     (Blender 5.2 pinned and proven, `Tools/Models/humanoid/`'s frame, `look.py` with today's look as its default, the
     render entry point, `make-humanoid.sh`, the sandbox's figure mode with `play-figure.sh`), done by a worker; **3b**, the
     blockout figure, the mood board, the three candidates and the chosen look's numbers, made in Blender with 3a's
     tooling, the manager's own (the user chose the look "Ember, hearth" on 2026-11-15; the manager writes it into
     `look.py`).
   - **3a done (2026-11-15, a worker, the branch `gm2-step3a`):** the official Blender 5.2.0 (the references' own: the
     Flatpak's build from the day the references began until 2026-11-14) in `~/.cache/stat-fick-build/blender-5.2/`,
     its checksum checked, found by `env.sh`; all 109 reference files (16 models, 4 terrain files, 56 renders, 33
     evaluations) made again with it byte for byte; the frame, the render entry point and the figure mode with their
     tests and mutations. The choices made are under "How it works", step 3a.
4. **The first humanoid** (3D, the manager; the PC; M7's "Model approval"). After 3.
   - Owns: the skeleton (mitten hands, no face bones, the sockets); the human man and woman, average build (Q8), their
     faces, hair and outfit to Q2's and Q3's bar; the sword, shield and chain shirt (Q9); the full clip set with its
     markers and the figures' small state machine; their `.glb` in `Assets/Models/humanoid/`; their Blender references
     (turnarounds as render scenes, clips as evaluations, gear as "Rendered gear") and tests; `models.pak` and its script.
   - Must not touch: the other races and builds (9), the game's screens.
   - Approval stops (each a full sheet, Q5): three candidates of the two bodies and faces (Q6), then the chosen pair
     refined; bare; with the sword, shield and chain shirt; the four clip batches (Q10: moving and reacting; melee;
     ranged, casting and items; guard, evade, death, victory), each played on the human pair and on blockouts of the
     smallest and largest bodies (the hobbit's slender, the Goliath's strong); the playing view in the figure mode. Then
     the tests.
   - Finished: approved; every model, render and clip against Blender (`ReferenceLimits.M6`, or a limit the user
     approved); `build.py --check` equal; the counts (vertices, bones, materials) and a playing view's cost measured on the
     GPU and lavapipe and reported.
5. **The game's look in the engine and figures built from looks** (engine and game; the PC for the Blender side). After 3's
   look and 4's skeleton.
   - Owns: `render.style` in the engine's `Render3D/` with its tests and the game's entry in `data.pak` (Q12, GM2-P11);
     the game's figure builder (a `look.hero` to an `Actor`, GM2-P8); `assemble.py` and the comparisons; the structural
     walk; the 3D mask in the game's screen fixtures (GM2-P12); the measurement of a view with no scenery (the shadow pass
     and its atlas; an engine task for the manager if they are spent there).
   - Must not touch: the engine's test scenes' style (Q11), the screens' markup.
   - Finished: an assembled look renders as Blender's (sample looks now, the presets as 9 lands them); the walk over every
     combination; the mask in place. No approval (the look was approved in 3).
6. **The save v2 and the maker's records** (rules; anywhere). After 2.
   - Owns: `save.game` v2 (GM2-P5) with `Upgrade()` and fixtures, `look.hero`, `look.body`, `look.part`, `look.preset`
     with their checks (GM2-P6, GM2-P7; the swatches in `race`), the guard tests' change, the presets' data from the
     catalogue (as data; their parts arrive with 4 and 9).
   - Must not touch: the screens, art.
   - Finished: round trips, the v1 fixture upgrading, every check against a case. No approval.
7. **The sheet** (rules and screen; captures on the PC or under Xvfb). After 2 and 6.
   - Owns: `Screens/Sheet/` (the old page's markup; the paper doll with the dark panel, Q29, Q32, its view empty until 11;
     the name as the heading, Q30; the seal button's emblem picture, a placeholder until 10), `Assets/UI/styles/sheet.css`,
     the whole rail with its disabled tools and the drawer (Q34), `PageTests.cs`'s rail test, the sheet's fixtures.
   - Must not touch: the carousel, the maker, the views' code (11).
   - Approval stops: the sheet's captures (1×, both themes, the drawer under 840 px, Dev Mode on and off) in
     `claude-work/gm2/step7/approval/`; then the fixtures.
   - Finished: approved; the screen against Chromium of the port's markup (12 levels / 0.2 %, the view masked); every
     sheet action through the rules and saved.
8. **The race carousel and the maker's screens** (screen; captures on the PC or under Xvfb). After 1 and 6.
   - Owns: `Screens/Races/` (the carousel, Q28) and `Screens/Maker/` (presets as a list, the choices, the random button
     with no locks, Q21; GM2-P15's helper), their sheets, the flow from New character to the sheet (Q25, Q26), Dev Mode's
     powers (Q27), their fixtures; the views show the engine's stand-in figure and a placeholder emblem until 10 and 11.
   - Must not touch: the sheet, art.
   - Approval stops: the screens' captures (both themes; Dev Mode's carousel too) in `claude-work/gm2/step8/approval/`;
     then the fixtures.
   - Finished: approved; the screens against Chromium (views masked); the flow's rules tested.
9. **The builds, the races and the parts** (3D, the manager; the PC). After 4 is approved.
   - **9a The shared sets and the human:** the human's slender and strong builds, its other faces, the outfit fitted to
     the builds; the shared sets designed and fitted to the human: hair (about 16–24 styles per sex), beards (about 12),
     head pieces, scars and freckles; the human's eight presets. Approval stops: the builds and faces; the sets (as lists
     on the human, two candidates per family, Q6); the presets beside the old looks.
   - **9b Elf and Dark Elf; 9c Dwarf and Hobbit; 9d Lizardman and Merfolk; 9e Half-Beast and Goliath**, side by side
     after 9a's sets are approved: each race's six bodies (two sexes, three builds), twelve faces, its outfit per sex
     fitted to its builds, the sets fitted to its skulls and faces, its own parts (crests and the lizardman's tails; the
     Merfolk's fin crest and scale patterns; the Half-Beast's ears and tails, T14's beasts among them; the Goliath's rune
     patterns), its swatches, its eight presets (the Goliath's and Merfolk's with marks, Q42). Approval stops per race: its
     bodies and faces; its parts and presets beside the old looks; the clips played on its bodies.
   - Owns (each): its models in `Assets/Models/humanoid/`, its Blender references and the assembly comparisons of its
     presets, its race's records filled. Must not touch: the skeleton and the clips (4's: a change there goes back to 4
     and is approved again), another race's parts.
   - Finished (each): approved; every model and preset against Blender; `build.py --check` equal.
10. **The nine emblems** (3D, the manager; the PC). Its sketches any time; its models after 3's look.
    - Owns: the nine motifs' sketches (Q37), the emblems' scripts and models (medallions in relief, Q35), their pictures in
      `ui.pak` at each size and scale (Q36), their Blender references, the live emblem's scene for the carousel.
    - Approval stops: **the nine motifs as sketches, before any modelling** (`claude-work/gm2/step10/approval/`); the
      nine as renders together; then the tests.
    - Finished: approved; pictures and models against Blender; regenerable.
11. **The live figures and the live emblem** (game; anywhere for the tests). After 5, 7 and 8, with 9a's human; finished
    when 9e and 10 are in.
    - Owns: the views' code in the three screens (the hero's actor in the sheet's panel, idle in a fixed ¾ view, Q31;
      the carousel's and the maker's figures in the same fixed idle, Q41; the carousel's live emblem, Q36), pausing when covered or
      hidden, rebuilding on a change in the maker, at most the live views each screen shows (GM2-P16 as amended).
    - Must not touch: the screens' markup and sheets (7's and 8's), the models.
    - Approval stops: the three screens with their live views (videos); then the tests.
    - Finished: approved; each view against Blender's render of the same scene; the memory and the per-frame cost measured
      on the GPU and on lavapipe and reported.
12. **The sounds** (anywhere; the user's ear on the PC). Any time.
    - Owns: the patches (Q38: a race chosen, a maker choice, the random roll, a point spent, a level up, a respec, the
      hero confirmed) in `Assets/Audio/effects/`, `audio.pak`, `SoundNames` and its test; the wiring once 7 and 8 are
      merged (or by them, named here).
    - Approval stop: each sound by ear (`play-sounds.sh --candidates`), then the fixtures.
    - **Done (2026-11-15, the branch `gm2-step12`; "How it works", step 12):** three candidates a sound, each its own
      paper-and-ink image, levelled as GM1's, rendered in Chromium and measured offline; the user's picks by ear "b c b a c
      b a", no changes. The step's choices: the names (the manager's, (a) of the first report); `SoundNames.Hero` and `All`
      (15); `point-spent` the only patch with a per-play range (±3 % pitch, a tonal sound heard many times in a row); the
      quick runs (`@row`, `@held`) rendered as WAV files for the ear. With the manager's leave: the seven weigh 5 in
      `Ui/GameSounds.cs` (GM2-P17), the tests that pin `audio.pak`'s content brought to it, and an engine rule the
      editor's `preview-effect` met at 1.65 once its list overflowed: Chromium's `DrawingDisplayItem::TightenVisualRect`
      (`Docs/Compositing-Port.md`), ported with a fixture of its own. The wiring is steps 7's and 8's (GM2-P17).
13. **End to end, the packages and the Windows items** (the PC). After every other step.
    - Owns: `end-to-end.sh`'s new-hero flow (name, race, a preset, a random roll, confirm; a level up and a point spent,
      read from the log), the packages' listings with `data.pak` and `models.pak`, `check-packages.sh`'s rules for them,
      "Windows check" items for GM2, the checks by hand on Linux for GM2.
    - Finished: the flow passing on the GPU and lavapipe; the packages checked; GM2 ready to tick.

**Waves** (the first starts on the manager's go):
- **Wave 1:** 1, then 2 (its tooling beside 1); 3 (the manager); 12 and 10's sketches when free.
- **Wave 2:** 4 after 3 (the manager); 6 after 2; 10's models after 3's look.
- **Wave 3:** 5 after 3 and 4's skeleton; 7 and 8 after 6; 9a after 4's approval.
- **Wave 4:** 9b–9e after 9a's sets (side by side, the manager's, as many at once as the PC and the approvals carry); 11
  after 5, 7, 8 and 9a.
- **Wave 5:** 11 finished (after 9e and 10); 13.
- **What must be approved before what:** the look (3) before the first humanoid's finish (4) and every emblem model (10);
  the first humanoid with its gear and full clip set (4) before any other build, race or part (9); 9a's shared sets
  before 9b–9e; the emblems' sketches before their models; the screens' captures (7, 8) before their fixtures.
- **Side by side:** at most four workers. While the manager works 3, 4, 9 and 10, workers take 1, 2, 6, 7, 8, 12 and the
  engine side of 5; one full run at a time.

| Step | What | Kind | After | Stops for the user |
| --- | --- | --- | --- | --- |
| 1 | races, effects, `data.pak`, stand-in removed | rules | none | no |
| 2 | actor record, rules, oracles | rules | 1 | no |
| 3 | Blender 5.2 verified, tooling, figure mode, the look | 3D (manager) | none | mood board, candidates, rounds |
| 4 | first humanoid: human pair, sword, shield, chain shirt, clips | 3D (manager) | 3 | candidates, bare, gear, 4 clip batches, playing view |
| 5 | the look record, figures from looks, mask | engine, game | 3, 4's skeleton | no |
| 6 | save v2, maker's records | rules | 2 | no |
| 7 | the sheet: paper doll, panel, name, rail | screen | 2, 6 | captures |
| 8 | carousel and maker screens | screen | 1, 6 | captures |
| 9a | shared sets, the human's builds and faces, presets | 3D (manager) | 4 approved | builds and faces, sets, presets |
| 9b–9e | the other races, two by two | 3D (manager) | 9a's sets | per race: bodies and faces, parts and presets |
| 10 | nine emblems | 3D (manager) | sketches any time; models after 3 | sketches, renders |
| 11 | live figures and the live emblem | game | 5, 7, 8, 9a (ends after 9e, 10) | live views |
| 12 | sounds | audio | answers | by ear |
| 13 | end to end, packages, Windows | PC | all | no |

## How it works

Filled by the steps.

### Step 3a: Blender 5.2 pinned and proven, the art's tooling, the sandbox's figure mode (2026-11-15)

Step 3's tooling and code, split from the look itself (3b, the manager's; "Steps", step 3). On the PC only for what runs
Blender; the tests run anywhere (what Blender made is committed).

**Blender 5.2.0, pinned (GM2-P10).** The official build `blender-5.2.0-linux-x64.tar.xz` from
`download.blender.org/release/Blender5.2/`, its SHA-256 checked against the published `blender-5.2.0.sha256`, unpacked
at `~/.cache/stat-fick-build/blender-5.2/blender-5.2.0-linux-x64/blender` (the archive kept in `download/` beside it):
"Blender 5.2.0 LTS (hash fbe6228777e7 built 2026-07-14 01:32:04)". Why 5.2.0 and not the latest 5.2.4: `flatpak
history` shows the Flatpak installed on 2026-10-16 at commit `f97247d9e87d` and updated only on 2026-11-14 (to 5.3);
Flathub's log dates that commit 2026-07-14, 5.2.0's release day, with no commit between it and 5.3; and every committed
test model says "Khronos glTF Blender I/O v5.2.39", 5.2.0's own add-on. So every reference was made with 5.2.0.
- **Found by every script:** `Tools/Scripts/env.sh` sets `$STATVENTURE_BLENDER` to the pinned build when it is there
  and nothing set it before (as it finds Node in the cache): a worktree has no `env.local.sh` (git-ignored, one per
  checkout), so GM2-P10's `env.local.sh` line alone would leave every worker on the Flatpak's 5.3. `env.local.sh` may
  still name another (it is sourced after); the main checkout's holds only `STATVENTURE_GAME`.
- **Refused otherwise:** `Tools/Scripts/blender.sh` (sourced by `make-blender-references.sh` and `make-humanoid.sh`)
  takes `$STATVENTURE_BLENDER`, else `blender` on the PATH, else the Flatpak, and refuses any Blender whose version
  isn't 5.2.x (the Flatpak's 5.3 among them) unless `STATVENTURE_BLENDER_ANY_VERSION=1` (to measure a move only); it
  runs Blender headless only (`-b --factory-startup`, `DISPLAY` and `WAYLAND_DISPLAY` unset for it).
- **The proof:** `make-blender-references.sh --out <folder>` makes everything into a scratch copy of the test project's
  layout (the scene files copied into `<folder>/Reference` so the model and terrain folders resolve there; nothing
  committed is written), and `Tools/BlenderReference/compare_references.py <folder>` compares every file with what is
  committed: byte-equal, or measured (a render's pixels and levels, an evaluation's largest number difference, a model's
  JSON and float buffers). With the pinned build: **109 of 109 byte-equal** (16 models, 4 terrain files, the 56 renders
  with the glow composed, 33 evaluations: 21 animation, 9 F-curve, 1 node, 2 stage), 3 min 42 s on the GPU; again at
  the step's end, 117 of 117 with the figure mode's 8 references among them (below). A move to 5.3 is an engine task of its own:
  the same two commands with `STATVENTURE_BLENDER_ANY_VERSION=1` measure it.

**The `humanoid` package (GM2-P9), `Tools/Models/humanoid/`.** Its layout as the design gives it: `rig.py` (the socket
names M7 gives, the skeleton's bones for step 4), `bodies/`, `parts/`, `clips/`, `emblems/` (packages whose
docstrings say what goes there and how), `gear.py` and `assemble.py` (what steps 4 and 5 write there), and the working
parts:
- `manifest.json`: every model the family makes, `{name, builder: "module:function", out}` (`out` relative to
  `engine/`, the family's in `Assets/Models/humanoid/`). A builder is a function of the `.glb`'s path that builds the
  model in an empty Blender scene and exports it with `make_models.export` (the test models' exporter: the triangles
  in a canonical order), `make_models.ANIMATED` for clips; builders import `make_models`' helpers (`armature`,
  `material`, `keys`, `stack_actions`, `chain_tube`, `skinned_mesh` …) and their own modules from the package.
- `build.py`, run inside Blender by `make-humanoid.sh build [name …]`: builds every model the manifest names (or those
  given) into its place. `make-humanoid.sh check [name …]` builds them into a scratch folder and compares each with
  what is committed **byte for byte**: the export is deterministic (the sample built seven times, the same bytes; the 16
  test models the same bytes from the Flatpak's 5.2.0 and the official one); a difference fails the run and names what
  differs (`compare_references.py`'s model comparison: the JSON's values, the float buffers number by number). Shown
  able to fail: a colour of the sample changed (the JSON's base colour, 0.00142) and a key's angle changed (the buffer,
  0.00867), each exit 1. `check` also runs `look.py check`.
- `look.py`: a look's numbers, everything a candidate can set within today's rules (Q7), read from the engine's code:
  `background` (the view's, sRGB), `sun` (`direction` toward it in glTF's world, `strength`, `angle`: `SunLight`),
  `style` (every number of `CelStyle` by its property's name in camelCase: `ink`, `shadowMix`, `bandEdge`,
  `outlineWidth`, `innerWidth`, `creaseAngle`, `hatchSpacing`, `hatchWidth`, `hatchAngle`, `hatchInk`, `glowWide`,
  `glowCore`, `glowBoost`; widths at device scale 1), `palette` and `shadows` (slot → sRGB colour; a slot's own shadow
  tone). `TODAY` is the engine's look as it is, unchanged: `CelStyle.Default`, the sun of the lizardman test its numbers
  were measured under (`Scenes3D.Lizardman`: (1, 1.1, 0.7), strength 3, `SunLight`'s angle 3), the reference scenes'
  ground `#b8b0a0`, an empty palette (each material its own base colour). `LOOKS` names the looks (`today`; the chosen
  one joins it in 3b), `SLOTS` the palette slot names the parts use (GM2-P7's list; the parts' steps add theirs) and
  `GEAR_SLOTS` the gear's (GM3's, distinct; empty until step 4's sample gear); a look naming another slot is refused, so
  a typo can't pass. `python3 look.py write <name or file> [<file>]` writes a
  look file (`looks/<name>.json` by default: JSON, `format` "statventure.look", `version` 1, its keys in a fixed
  order, so a look written twice is the same bytes); `list`; `check` (every look valid, `looks/today.json` as written).
  The look file is the one both sides read: render.py and the figure mode (`--look <name or file>`). Shown able to fail:
  19 breaks of its rules on a copy, each killed (`claude-work/gm2/step3a/pybreaks/`).
- `render.py` with `compose.py`, run by `make-humanoid.sh render`: a figure (`--model <body.glb>`, `--garment <glb>`…,
  `--item <glb>@<socket>`…) or a scene file (`--scene`) in a look (`--look`, today's by default) at a size and in views:
  `front` (0°), `three-quarter` (45°: the figure's front, glTF's +Z, turned toward the screen's right), `side` (90°:
  facing the screen's right), `back` (180°), any number of degrees, and `close-up` (45°, framed on `--focus`, the
  `head` bone by default, else the figure's top fifth, `--extent` metres high, 0.3 of the figure by default). The figure
  turns under a fixed camera and sun, so each side is lit alike; each view frames the figure at rest (its width or height
  with 6 % room each side, `--margin`), so the frames of a clip share one camera; orthographic unless `--perspective
  <degrees>`, the camera 5° down (`--elevation`; a face exactly edge-on is where Freestyle and the engine part). `--clip
  <name> --time <s> [--clips <library.glb>]` poses the figure as Blender evaluates the clip (played by bone names on a
  library's), the garments by the same joints, the items on their sockets as `Animation/Gear.cs` places them. It writes
  the scene file (`<out>/layers/<stem>-scenes.json`, `render_scenes.py`'s format, the models by their paths from it)
  and renders it with `render_scenes.py` itself (imported: its `main()` now runs only as Blender's script, the one change
  there), so a mood-board render is drawn by exactly what the engine's references are; `compose_glow.py` composes the
  glow, and `compose.py` lays Freestyle's ink over the shading at its coverage in linear light, as the engine draws its
  lines over its view: `<out>/<stem>-<view>.png`, the layers kept in `<out>/layers/`. `--scenes-only` writes the scene
  file alone. Blender's glTF importer leaves an animated armature at its first clip a frame in, which clearing the
  animation data keeps, so render.py gives every joint explicitly (at rest when no clip is asked); and it makes a bone
  shape (an icosphere in an excluded collection), which framing leaves out.
- `sample.py`: the frame's sample, a cube figure (four boxes, one a nose, on the bones `lower` and `upper`, materials in
  the slots `trousers`, `cloth`, `skin`; facing glTF's +Z; clips `bow`, 0.75 s, with a marker, and `sway`, 1 s), test
  data at `Tests/Statventure.Tests/Data/Models/humanoid/sample.glb`, not art and not for a pack.

**The figure mode (GM2-P14), `Samples/Statventure.Sandbox/Figure/`** (compiled into the engine's tests too, which link
its files). `Statventure.Sandbox --figure <body.glb> [--clips <glb>] [--look <name or file>] [--garment <glb>]…
[--item <glb>@<socket>]… [--view …] [--size WxH] [--elevation] [--margin] [--extent] [--focus] [--perspective]
[--dump <folder> [--fps N] [--times t,…] [--scene-file <file>]]`, run by `Tools/Scripts/play-figure.sh <body.glb> …`
(it compiles the shaders and builds the sandbox in Release first).
- `FigureLook`: the look file read strictly, as `look.py` writes it (every key, nothing else, each value of its kind, an
  `InvalidDataException` naming the first problem), into `CelStyle`, `SunLight`, the background and a `Palette`; the
  slot names aren't checked here (look.py's list).
- `FigureView`: the figure as the game shows one: an `Actor` on a node of a `SceneView` (rate 0: the mode's own clock
  seeks it) in a `View3D` with the look's background, sun and style, the look's palette, the garments as
  `ActorGarment`s and the items as `ActorItem`s on their sockets; the views and their framing are render.py's, number for
  number (`FigureFraming`; held to render.py's cameras for the sample by the tests); the clips in turn (`FigureClips`:
  one NLA layer of strips end to end, played round; at a meeting the stack shows the earlier clip at its end; a still clip
  takes a frame). `SceneOf` writes the scene at the moment in `render_scenes.py`'s format (each joint's deformation, the
  garments posed by the same joints, the items at their sockets, the look, the camera), which is how the engine's picture
  is held to Blender's.
- `FigureSandbox` (an `Application`, no splash, no sound, the sandbox's logs): the view fills the window (a `surface`
  element), a caption names the clip, its time, the view and the turn. Keys (`FigureControls`): Space plays and pauses,
  Right and Left step a frame (1/60 s, on whole frames) and pause, Down and Up go to the next and the previous clip's
  start, A and D turn the figure 15° (the camera stays the view's), 1–5 show the five views, F11 full screen, Escape
  quits. `--dump` writes one round of every clip in turn at `--fps` (30) or the `--times` given, `frame-NNNN.png` and
  `frames.txt` (each frame's time, clip and time in it), with `--scene-file` every frame's scene for Blender
  (`make-humanoid.sh render --scene <file>` renders them); it draws without vsync (a hidden window's surface, which a
  compositor doesn't pace, would hold a waiting present for ever), quits with its last frame, and refuses to write frames
  when the window came out of another size than the picture (exit 1). A window under the engine's usual minimum (320 ×
  240) keeps the picture's size.
- `play-figure.sh` with `--dump`, on the PC: inside a headless cage of its own whose output is the picture's size, on the
  cage's Xwayland (a Wayland window there draws its own title bar inside its size, GLFW's libdecor, 37 pixels less
  picture: cage offers no server-side decorations), a scratch per-user folder, then `ffmpeg` (9.0.2 on the PC) makes
  `<folder>/playing.mp4` (H.264, yuv420p, CRF 16); without ffmpeg it says the frames are there.
- **Its tests** (`Tests/Statventure.Tests/Sandbox/FigureModeTests.cs`, 84): the look file (today's is the engine's
  unchanged; every number of `CelStyle` in it and read from its own key, by reflection over `CelStyle`; 39 broken files
  refused; a colour back to its text at every level), the framing against render.py's cameras for the sample (`Reference/figure-framing*-scenes.json`, written by
  `make-humanoid.sh framing`: the five views and 30°, a perspective close-up on a bone, a picture so narrow its width
  frames the figure, a camera far enough to move the far plane; within 10⁻⁴), the clips in turn against the stack's own
  evaluation, the keys, the command line (every option to its place; nine refused), **the figure mode's picture against
  Blender's**: four scenes (`Reference/figure-scenes.json`, written with `STATVENTURE_FIGURE_SCENES=write`, compared
  otherwise, so a change of the scenes or of `SceneOf` fails until the references are rendered again; rendered by
  `make-blender-references.sh` into `Golden/r3d-figure-*.png` and `-ink.png`): the sample in today's look at
  three-quarter swaying, in a look of numbers all its own (no two alike) from the side bowing and in a perspective
  close-up, and the engine's test figure with its tabard and sword mid-swing; the shading within `ReferenceLimits.M6`
  (`Compare`) and the lines (`CompareInk`); and **the window's own picture** (a hidden window dumping a frame) equal to the
  view's picture pixel for pixel, its index and scene file what the view holds, a dump's frame count, the size guard and
  a small picture. The gear scene is seen from the front: at three-quarter a sliver of the lit thigh under a pixel wide
  shows between the leg's shadow and the tabard, shaded once per pixel under MSAA where Blender shades each sample (one
  edge pixel 37 levels off; `Docs/Renderer-3D.md`, "Freestyle's limits seen": test cameras avoid such slivers).

**Shown able to fail:** the figure mode's rules, 125 mutations (`claude-work/gm2/step3a/mutations.json`, made by
`make-mutations.py` beside it; three gaps they showed closed and run again in `mutations-rerun.json`): 120 killed, 5
accounted for: three can't differ (the line setting a perspective camera, `Camera`'s default; `<` for `<=` at a clip's
end with the stack's epsilon kept; a colour's channel truncated for rounded, the same at every byte) and two can't show
in the tests (the placement of a skinned item's points, the engine's items being rigid; the window's minimum size, a
hidden window keeping its size either way: it matters where a window manager enforces it). The dump's vsync broken by
hand hangs the run (noticed). `look.py`'s rules: 19 breaks of a copy, all killed (`claude-work/gm2/step3a/pybreaks/`);
`build.py --check`: a colour and a key of the sample changed, each failing it.

**How the manager makes and shows a model** (on the PC; Blender through the pin, headless; from the repo's `engine/`):
1. A builder: a function in a module of `Tools/Models/humanoid/` (say `bodies/human.py`, `def male_average(path):`),
   building in an empty scene and ending with `make_models.export(path, **make_models.ANIMATED)` (or plain `export(path)`
   for a model without clips); a line in `manifest.json`: `{"name": "human-male-average", "builder":
   "bodies.human:male_average", "out": "Assets/Models/humanoid/human-male-average.glb"}`; then `Tools/Scripts/make-humanoid.sh
   build human-male-average` (and `make-humanoid.sh check` before a commit).
2. Its turnaround and a close-up in a look: `Tools/Scripts/make-humanoid.sh render --out <folder> --name <stem> --model
   Assets/Models/humanoid/<model>.glb --look <look file or name> --views front,three-quarter,side,back,close-up --size
   800x1000` (`--focus <bone> --extent <metres>` for another close-up, `--clip <name> --time <s>` for a pose, `--garment`
   and `--item <glb>@<socket>` for what it wears): `<folder>/<stem>-front.png` … `<stem>-close-up.png`.
3. Playing in the engine's view: in a window, `Tools/Scripts/play-figure.sh <model>.glb --look <look> [--clips
   <library.glb>]`; frames and a video, headless: `Tools/Scripts/play-figure.sh <model>.glb --look <look> --view
   three-quarter --size 800x1000 --dump <folder> [--fps 30] [--scene-file <folder>/scenes.json]`, giving
   `<folder>/frame-NNNN.png`, `frames.txt` and `playing.mp4`; the same frames in Blender: `make-humanoid.sh render
   --out <folder2> --name frames --scene <folder>/scenes.json`.
4. A look: in `look.py`, a dict like `TODAY` added to `LOOKS` under its name (or a JSON file of the same fields), then
   `python3 Tools/Models/humanoid/look.py write <name>` writes `looks/<name>.json`, which `--look <name>` finds on both
   sides (render.py by its name in `LOOKS` or in `looks/`, the figure mode in `looks/`, copied beside the sandbox when it
   builds); `--look <path>` takes any look file.

**Choices made (internal, recorded here):** the pin found by `env.sh` as well as named in `env.local.sh`; a Blender
other than 5.2.x refused by the scripts; `--out` and `compare_references.py` for proving a Blender build; the sample's
output in the tests' data, not in `Assets/Models/humanoid/` (not art, never packed); one look file format both sides
read, `look.py` writing it with today's look as its default; a look's slot names checked against `look.py`'s list on the
Python side; turnarounds turning the figure under a fixed camera and sun; views framed at rest; the close-up's and the
margin's numbers (0.3 of the figure, the top fifth, 6 %, 5° down); the mood board's picture the shading with the ink
laid over it as the engine lays it; the figure mode's clips in turn on one NLA layer; dumps without vsync and inside a
cage of their own on Xwayland; `--dump`'s index and scene file; the figure scene file written only on request; `--figure
<look.json>` (a `look.hero`) left to step 5's figure builder (the mode takes a body `.glb` with garments and items now).

### Step 12: the sounds (2026-11-15, the branch `gm2-step12`)

The seven of Q38 as effect patches of the engine's existing nodes and rules (nothing changed in
`Engine/Statventure/Audio/`), chosen by the user by ear as GM1's eight were (GM1 step 4 the model in every respect;
`Docs/Audio.md`, "The game's sound effects", has their layers).
- **Candidates first:** for each sound three (a, b, c), 21 in all, each its own image of the paper-and-ink world (a quill,
  ink, wax, parchment, a book), so any pick of one letter per sound stays apart from the other six and from GM1's eight:
  the emblem stamped crisply, firmly or embossed (`race-chosen`); a swatch card laid, a brush dab, a pattern book's page
  flicked (`maker-choice`); pages riffled, the pounce pot shaken, lots drawn (`maker-random`); an ink drop, the nib tapped
  on the inkwell's rim, a tally mark (`point-spent`); three ink drops rising, a ribbon tied, gold leaf burnished
  (`level-up`, the reward, a little richer); the pen knife scraping, the wax tablet smoothed, the tallies rubbed out
  (`respec`); the book clapped shut, the blotter rocked, the name signed (`hero-confirmed`). Every voice ends within 0.3 s
  (GM1's rule, 71–292 ms). Each rendered in Chromium's OfflineAudioContext from the effect fixtures' own script at 48 kHz
  and measured at the first start's effects 60 %: levelled as GM1's (the mean of the K- and A-weighted loudness over the
  loudest 100 ms at −31.5 dB), the two heard many times in a row (`maker-choice`, `point-spent`) 3 dB under, as
  `ui-press` is, so none is louder than GM1's at the same volume; peaks −14.5 to −24.4 dBFS (GM1's −14.2 to −20.5), RMS
  over the loudest 100 ms −31.7 to −38.9 dBFS (GM1's −25.7 to −38.4). Told apart offline by a distance of octave bands and
  envelope: every candidate at least 8.7 from any render of another sound, where GM1's own closest pair (`ui-open`,
  `ui-toggle`) is 6.9. The two heard often were also rendered as quick runs (8 plays 120 ms apart, 12 plays 40 ms apart:
  a key held) for the ear. The user listened with `play-sounds.sh --candidates` (the manager's wrapper
  `claude-work/gm2/listen-sounds.sh`) and the renders' WAV files; the scratch tools, the candidates and the measurements
  stay in `claude-work/gm2/step12/`.
- **The user's picks (2026-11-15, by ear at the first start's volume, "b c b a c b a", no changes, the levels as heard),
  each source a byte-identical copy of its candidate:**

  | Sound | Pick | What it is | Patch level | Voice ends | At 60 %: peak, RMS-100, loudness |
  | --- | --- | --- | --- | --- | --- |
  | `race-chosen` | b | the race's emblem stamped firmly: a fuller, lower press held a moment, a duller peel | 0.9943 | 218 ms | −14.6, −33.4, −31.5 dB |
  | `maker-choice` | c | a pattern book's page flicked by the thumb: a quick light flick and its snap | 0.4467 | 93 ms | −21.3, −38.9, −34.5 dB |
  | `maker-random` | b | the pounce pot shaken over the page: three shakes of fine sand | 0.3163 | 250 ms | −17.7, −35.2, −31.5 dB |
  | `point-spent` | a | an ink drop: one soft rising plip, its pitch ±3 % each play | 0.2065 | 96 ms | −21.3, −36.4, −34.5 dB |
  | `level-up` | c | gold leaf laid on the initial: a rising swish and a soft shimmer (C, E, G) | 0.3199 | 292 ms | −24.4, −35.9, −31.5 dB |
  | `respec` | b | the wax tablet smoothed: one long, soft, smooth stroke | 0.4651 | 250 ms | −19.7, −33.3, −31.5 dB |
  | `hero-confirmed` | a | the hero's book clapped shut: the cover swinging, the clap, the air it pushes out | 0.7413 | 180 ms | −15.3, −34.3, −31.5 dB |

- **Where they are:** `Assets/Audio/effects/<name>.json` (the record's exact JSON form); `audio.pak` made again by
  `make-music-data.sh` under the shared lock inside the headless compositor (133 entries; reusing the old pack, reusing
  again and fresh: the same bytes); `Game/Statventure.Game/Sounds/SoundNames` (`RaceChosen`, `MakerChoice`,
  `MakerRandom`, `PointSpent`, `LevelUp`, `Respec`, `HeroConfirmed`; `Hero`, the seven in Q38's order; `All`, GM1's eight
  then the seven). The audio system registers each under its name at the start; `play-sounds.sh` plays all fifteen with
  no names.
- **The wiring (GM2-P17, the manager's, 2026-11-15), for steps 7 and 8:** each action plays its one sound through
  `parts.Ui.Sound(SoundNames.X)` when it is done (not when it is refused or does nothing), once per action, nothing on
  hover. The seven weigh 5 in `GameSounds.Weight` (step 12), above GM1's event sounds' 4, so the action's sound is the one
  heard over the `ui-press` or `ui-toggle` GM1's listener asks for first, and over a window opening or closing or
  `game-opened` in the same action.

  | Action | Sound | When |
  | --- | --- | --- |
  | A race chosen in the carousel (its choose button) | `race-chosen` | once, as the race is taken (the maker opens in the same action, unheard) |
  | An enemy-only race chosen at once in Dev Mode; the race changed from the sheet's seal button in Dev Mode | `race-chosen` | once, as the race is set |
  | Browsing the carousel (next, previous, the arrow keys) | GM1's: `ui-press` for its buttons, nothing for keys | — |
  | One of the maker's choices changed (sex, build, face, hair, a colour …), by click or keys | `maker-choice` | once per change; nothing when the choice stays the same |
  | A preset picked from the maker's list | `maker-choice` | once |
  | The random button | `maker-random` | once per roll (not one per choice it changes) |
  | "+" spending a free point | `point-spent` | once per point, however fast (clicks, or Enter held on the button): the patch's 4 voices take over the oldest; no rate limit; once no point is left the "+" is disabled and nothing plays |
  | Dev Mode's direct edits ("+" with no free point, "−", the fields) | GM1's `ui-press` for the buttons | — |
  | Level up (and Dev Mode's level up without EXP) | `level-up` | once |
  | Respec | `respec` | once |
  | "Begin as <race>" (the hero confirmed) | `hero-confirmed` | once; over `game-opened`, `ui-open` or `ui-close` should the sheet's opening ask for them |

- **The engine rule the step met (with the manager's leave):** the new rows of the game's `audio/effects` folder made the
  editor's `preview-effect` entry list overflow, so at 1.65 (composited scrolling) its status line, whose top is at 1014.75
  device px where the list ends, became an overlap layer of its own in the engine with grayscale text, where Chromium keeps
  it in the page's layer with LCD text: 3489 pixels (0.20 %) over the limit's 3484. The cause is Chromium's
  `DrawingDisplayItem::TightenVisualRect`: a drawing item of one plain rect fill takes that rect as its visual rect (the
  line's border-top, one FillRect from 1015), which the engine didn't have (the box's ink overflow from 1014 overlapped the
  list's container rect to 1015). Ported in `UI/Paint/PaintTarget.cs` with a fixture of its own
  (`Ui/VisualRectTighteningReferenceTests`); the rule and what moved: `Docs/Compositing-Port.md`, "Visual rects tightened
  to one plain fill". `preview-effect` at 1.65 then differs from Chromium in 1576 pixels (the old screen 1568).
- **Checks:** a fixture per sound against Chromium (`AudioFixtures.GameSound`, M8's rule; the renders the ones the user
  heard: five sample for sample, `level-up` and `hero-confirmed` within Chromium's summing order, 5.6e-9 and 3.0e-8);
  the engine's `GameSoundsTests` (the fifteen sources, each checked, in its JSON form, within 0.3 s; the pack holding
  exactly them; each registered under its name and over within 0.3 s); the game's `SoundNamesTests` (the names, `Hero`,
  `All`, the pack's patch for each) and `GameSoundsTests` (each name's weight; `hero-confirmed` over `game-opened`,
  `ui-open` and `ui-close`; a click whose action asks for one of the seven sounding it alone, with a window opening or
  closing too; the seven playing from the committed pack). Brought to the pack's new content (the manager's leave):
  `MusicLibraryTests.TheCommittedPackHoldsTheGamesMusic` (8 → 15 effects), `AudioMarksTests.TheCommittedPackPasses` (126
  → 133 entries), the editor fixture `preview-effect` (its markup and its two goldens captured again headless: seven rows
  more and the list's scrollbar).
- **Breaks** (`mutate.py`, lists in `claude-work/gm2/step12/`): the sound side, 14, all killed: a name changed, the
  hero's list short of one, the seven weighing 4 (as GM1's events), one of them left out of the weight, a source and the
  pack apart, a source not in its JSON form, a source's layer past 0.3 s, and each of the seven fixtures with the engine
  playing that sound 1 % higher. The rule, 9 (`Docs/Compositing-Port.md`), 6 killed and 3 accounted for. The 0.3 s rule
  also by hand on the pack (`check-time-rule.sh`: a source lengthened past it and the pack made from it, the sources'
  check and the pack's play both fail; the pack then made again, its committed bytes). Nothing written to the player's
  real folders.

### Step 1: the races, the effect records and `data.pak`; the stand-in kind removed (2026-11-15, the branch `gm2-step1`)

**The kinds** (`Game/Statventure.Game.Data/Races/`, `Effects/`; the library's first real kinds, GM2-P3, GM2-P4, GM2-P7):
- **`race` version 1** (`Race`, `[Kind("Race", "data/races")]`), one entry per race at `data/races/<id>`, the id the
  entry's name (`RaceCatalogue.PathOf`; the path rule stands for the old `slugify`, and "Duplicate race id" can't happen
  with one entry per path). Its fields, `normalizeRaceDefinition`'s and the decided ones: `name` (required), `playable`
  (true when not given: the old `raw.playable !== false`), `statModifiers` (`race.modifiers`: the eight attributes, 0 when
  not given), `passive` (required; `race.passive`: `id`, `name` (required, may be empty as there), `condition` ("Always
  active" when not given; `ShownCondition` shows an empty one so, as the old `|| "Always active"`), `description`,
  `effects`), `accent` (`[Colour]`, the old code's `raceAccent`, "#c7683f" when not given), `emblem` (Q44: the seal's
  name, null for none), `order` (Q43: 10, 20, … 280 in `races.json`'s order) and `swatches` (`race.swatch`: `slot`, `name`,
  `[Colour]` `colour`): **an empty list in all 28 races**: the swatches' values come with the approved look (step 3) and
  the maker's records (step 6), and the slot names' list is step 6's (GM2-P7); the shape only is here. JSON names are the
  old file's (`statModifiers`, `passive`, `effects` …).
- **The checks** (`[Check]`, run by the editor and by the game's mod check alike, G4, G5), what the old loader refused
  in the editor's words: a name empty once JavaScript's `trim` takes its white space ("the name is empty", at `$.name`;
  the old "name must be a nonempty string"; JavaScript's set: U+FEFF, U+2028/9 and every space separator, not U+0085, which
  .NET's `Trim` takes), each modifier a finite number ("the strength modifier must be a number (it is NaN)", at
  `$.statModifiers.strength`; the old "<attribute> modifier must be a number"). What the old loader refused that a record
  can't hold at all, its record form refuses: not an object, no name or a name that isn't text, no passive or a passive
  without a name ("race needs the key \"name\"" and the like); not an array and a repeated id are the file's, which the
  port doesn't have. New with the new fields, not limits on the old data (G4): the accent and a swatch's colour "#rrggbb",
  the emblem one name of the data path rule (its pictures are found by it). No field has `[Limits]` or `[Choices]` the old
  loader didn't have: an effect's target or element the old code doesn't know is passed by, as there.
- **`effect` version 1** (`Effect`, a union, `[Kind("Effect")]`): 23 subtypes, tags 1–23, each `effect.<the old type
  string>` with the fields the old code reads for that type and their defaults the old `toNumber`/`toInteger` fallbacks
  (`amount` 0, `interval` 1; `death-save-decay`'s `initialChance` nullable, as the old code reads a missing one): the 22
  types `races.json` uses and `trap-limit-flat`, which the old race fold reads though no race uses it (GM2-P3 named 22;
  the design's "sheet side for … `trap-limit-flat`" needs it as a type). `equipment-restriction` keeps `typeName` and
  `target` (the old code reads either). Display names are the type strings in words ("Max HP per levels").
- **The sheet side** (`RaceTotals.Of(race, level, enemyCount)`, the old `calculateRaceEffectsUncached` with every field of
  `emptyRaceEffects`: `MaxHpFlat`, `MaxHpPercent`, `MaxManaFlat`, `UnarmedPowerFlat`, `UnarmedName`, `SpellCostPercent`,
  `SpellCostPercentModifiers`, `StatPermanentPercent` (`AttributeValues`), `DefensePermanentPercent`, `DefenseFlat`,
  `DefenseCap` (`DefenseValues`, infinite when uncapped), `EquipmentRestrictions`, `TrapLimitFlat`, `HealingDisabled`,
  `SpellcastingDisabled`, `DebuffImmune`; not its `passives`, which is the race's one passive) and the attributes' race
  modifiers (`Race.StatModifier(attribute, level)` and `RaceTotals.StatModifiers(race, level)`, the old `raceStatModifier`:
  the modifier plus `max(0, level − 1) × amount` of each `all-stats-flat-per-level-up`; a key that isn't an attribute's
  gets the growth too, as there). Each the old arithmetic in its order: `max(1, toInteger(interval, 1))`, `floor(level /
  interval) × amount`, `amount × max(0, enemyCount)`, a cap `min(cap, max(0, amount))`, names and elements lower-cased as
  JavaScript does (`JsValues.ToLowerCase`: İ and the final sigma included), the last unarmed name, a target the totals
  don't have passed by. `RaceTotals.SheetSide` (16 types) and `BattleSide` (7) say which is which; a test holds every
  subtype to exactly one.
- **What step 2 (and 6, 8) take:** `RaceCatalogue.Load(data)` (every race directly in `data/races`, by `order`, then id;
  an entry there of another kind is the data's refusal), `Race.Playable`, `RaceTotals.Of` (step 2's seam's (b): maximum HP
  flat and percent, maximum Mana flat, the stat and defence percentages, the defence flats and caps, `HealingDisabled`),
  `Race.StatModifier` / `RaceTotals.StatModifiers` (its (c)), the attribute keys (`AttributeKeys`, `DefenseKeys`, the old
  game's strings) and JavaScript's value rules (`JsValues`).

**Which side reads what; where the table in "What exists" was wrong** (checked in the old code, 2026-11-15). The split
holds: the fold's fifteen types and `all-stats-flat-per-level-up` on the sheet side, seven on the battle side. But:
1. `spell-power-percent` from a race (the dryad's) is read by **nothing** in the old game: the battle's spell power takes
   timed effects (`timedSpellPercent`, `combat.js` 2838) and the classes' totals; the passives' screen only shows its
   label (`app.js` 5100, 5710). GM5 decides what it does.
2. `passiveEffectsOf` (`combat.js` 1837) is the classes' passives only (`passivesOf` = `calculateClassEffects().passives`,
   318): the battle reads a race's effects through `racePassiveEffectsOf` (174–175) and `getSelectedRace()` (1816) alone.
3. `spellcasting-disabled` and `debuff-immunity` are folded, but nothing reads those totals: the battle reads the race's
   own effects (`raceHasEffect`, 177–180, 323). `healing-disabled`'s total is read (`adjustResource`, the level-up
   healing, `map.js` 987).
4. Of the fold's totals only maximum HP and Mana, the stat and defence percentages, Evasion's flat (the sheet adds the
   race's flat to Evasion only, 1781; Armor's in `calculateArmorForWeapon`, 3650), the caps and `HealingDisabled` act on
   the sheet; the unarmed ones act in battle (`combat.js` 398, 2303, GM5), the spell cost on spells (`app.js` 2376, GM7),
   the restrictions on items (3021, GM3), the trap limit on traps (1167), and the flat per enemy is 0 outside a fight.

**The converter and the data** (GM2-P4): `OldRaceFile` (in the game's tests, `Tests/Statventure.Game.Tests/Races/`):
`races.json` read as `applyRaceText` reads it and each race as `normalizeRaceDefinition` makes it, with JavaScript's own
rules (what is text, an object, truthy; `slugify`), refusing what they refuse with their messages; the accent from the old
`raceAccent` table, the order the file's, the seal `objects/race-<name>.svg` as the emblem. What a port record can't hold
as the old game held it is the converter's own refusal, never a guess (an id that isn't text, an effect without `type`
or of a type the port lacks, a field its type doesn't have or of another kind of value, a seal of another form). Its
output is `Assets/Data/data/races/<id>.json`, the races' JSON forms, `data.pak`'s tree (what `packer extract` gives);
`RaceDataTests` holds the committed files to the converter's output byte for byte (written with
`STATVENTURE_RACE_DATA=write`). `Tools/Scripts/make-data-pack.sh` packs `Assets/Data` into `Assets/Packs/data.pak` with the
packer (Release; the game's kinds known), always afresh (into a new file, then moved over the old: no stored bytes taken
over), so the same folder gives the same bytes anywhere; the tests hold the committed pack to a fresh pack of the folder.
The engine already mounts `packs/data.pak` (`Application.AreaPacks`): `Game/Statventure.Game` is unchanged.

**The oracle** (**folded into step 2's tooling at step 2's rebase**: the modules `race-fold` and `race-rules` of
`Tools/OldGameOracle/`, run by `Tools/Scripts/make-old-game-references.sh`, the case list moved to
`Tools/OldGameOracle/cases/race-cases.json`, the results `Reference/old-game/race-fold.json` and `race-rules.json`, the
same values; `make-race-references.sh` and `race-references.js` gone: "How it works", step 2. As this step made it:)
(the old game's test client in Chromium 152, headless, through `chromium.sh`, as GM2-P13 lays down; step
2's tooling came beside it, so this step's is its own): `Tests/Statventure.Game.Tests/Reference/race-cases.json` (the case
list: the fold's levels and enemy counts, five crafted races in `races.json`'s form whose values reach the fold's edges,
53 `normalizeRaceDefinition` cases, 4 whole files), `race-references.js` (the test-client script),
`Tools/Scripts/make-race-references.sh` (the run; inside a headless compositor on the PC) writing `race-fold.json` (3,696
cases, step 2's case form `{ module, sources, cases }`: the 28 races and the crafted ones at levels 0–101, 150 and 200
with no enemies, at 1, 50 and 100 with 1 and 3, at 1 and 50 with −1; `statModifier` and every total) and
`race-rules.json` (the normalized races and cases, the files' refusals, the accents); both name `objects/races.json` and
`scripts/app.js` by SHA-256, and a test fails when the old game changed under them. No rule here draws `Math.random`.

**Tests** (game: 34 new, 599 in all; engine: one more, `RaceKindTests` having one test more than `StandInKindTests`): `RaceTotalsTests` (every oracle case, field by
field; non-finite numbers as `toNumber`; the sides; the old checks that act on the sheet, named after them:
`CheckNewRaceData`, `CheckGiantsBlood`'s race totals, `CheckNaturalClaws`' power and name), `RaceRecordTests` (the
fixtures, the names, the kind, the checks against the oracle's cases), `RaceDataTests` (the committed forms, `data.pak`,
the 28 in the game as it mounts its packs, the catalogue's order, each race the old game's), `OldRaceFileTests`,
`JsValuesTests`, `AttributeValuesTests` (the keys in the old order, each its own value); the layout guard covers every record type of the library (`LibraryRecords`, `TheGuardCoversEveryRecordTypeOfTheLibrary`:
the save, `race` and its three parts, `effect` and its 23), with the fixtures `race/goliath` and `race/every-effect`.

**The stand-in kind removed** (GM1-P3): `StandIn/` gone; the editor's game tests (`GameTesting.RaceKind`, `RaceType()`,
`Race(name)` from its JSON form, `LibraryKinds` 29; `RaceKindTests` in place of `StandInKindTests`), `GameFolderModTests`
(New entry "Race (race)", the name, a modifier, the passive made, an effect added and chosen), the game's mod and screen
tests (`TestRaces`) now use races. The end-to-end flow's mod is `GameFolderModTests`' output, so `end_to_end.py` is
unchanged. **The `settings-mods` goldens** (approved by the user in GM1) changed only in their fixture text: the stand-in
kind is gone, so the broken mod holds two races its checks refuse (lamp, its strength not a number, first, so the row
still wraps onto two lines; torch, an empty name); the row reads "Left out: data/races/lamp: the strength modifier must
be a number (it is NaN) (at $.statModifiers.strength) (and 1 more)". Measured before and after (the manager accepted
them): only the pixels in that text's box differ, at 1× x 280–620, y 304–336 (5,012 pixels, dark and light alike), at
1.65 x 462–1085, y 510–562; no other of the 62 fixtures' markup changed.

**The editor's screens over the repo's packs** show `data.pak` now (its `data/races` folder in the entry list, the pack in
the browser): 12 editor fixtures' markup changed (`checks-problems`, `import-again`, `import-path`, `import-replace`,
`list-marks`, `list-unreadable`, `preview-cutscene`, `preview-effect`, `preview-model`, `references-list`,
`saving-mod-info`, `saving-save-as`), captured again (only the entry list's pane differs, at 1× within x 0–280); the
entry list's and the browser's tests name the new folder and pack. `list-marks` is 1000 × 700 now (640 before): at 640
its tree overflowed the pane, where **Chromium scrolls the focused tree into view and the engine's `Focus()` doesn't**
(an engine finding, reported; the fixture shows the whole tree again, as it did). Every later step that adds entries to
the repo's packs moves these screens the same way.

The engine's tests that read every committed pack (`CommittedPackTests.EveryCommittedPackIsSound`,
`RecordDraftTests.EveryCommittedPacksRecordsBuildBackToTheirStoredForm`) read them with the game's kinds now, loaded the
editor's way (the engine's test project never references the library).

**Packaging:** `file packs/data.pak` in both game listings and `cover packs/data.pak -` in `licenses.txt` (the game's own
data); `check_packages.py`'s rule ("the committed packs exactly") is unchanged.

**Shown able to fail** (`mutate.py` with the game's project, under the shared lock in a headless cage;
`claude-work/gm2/step1/mutations.json`, made by `make-mutations.py`): 113 mutations of every check, each effect subtype's
names, tags, fields and defaults (the layout guard), the fold per type (intervals, `toNumber`, the enemies, the caps, the
lower case, the fallbacks, the last name, the sides), the modifiers' growth, the catalogue, the converter's rules and
JavaScript's value rules. The first run: 101 killed, 9 survived, 3 didn't build (they removed fields the tests set:
written again as layout changes). Of the 9, six were gaps, closed (`AttributeValuesTests`; a second percentage on one
attribute and one defence among the crafted races; a seven-character colour without its "#"; an id, a condition and a
description of 0 in the case list) and killed in the rerun (`mutations-rerun.json`, 12: 9 killed). **The standing list:
113, 110 killed, 3 that can't differ:** an interval's fallback 0 for 1 (`max(1, …)` lifts both to 1); the catalogue's
tie by id (the data lists entries in ordinal path order, which is the ids' order); the converter reading an array's
modifiers (an array has no named members, as JavaScript's `[1, 2].strength` is undefined). **The pack script's rules** (a
runner of its own, `pack-script-rules.py`: a scratch engine with a stand-in `dotnet`; eleven rules: the Release packer
built first, `Assets/Data` packed whole into `Assets/Packs/data.pak`, always afresh, an old pack replaced, a failing
packer or build failing the script with the old pack kept and nothing left, a leftover of the run's own name removed, no
`Assets/Data` refused): 14 mutations, **14 killed**. Nothing written to the player's real folders.

**Runs** (headless, scratch per-user folders, nothing audible): the full suite on the GPU and on lavapipe, on the step's
commit rebased on step 12's `f071f09`: 4266 + 610 = 4876 tests (`main`: 4265 + 576 = 4841), none held; `publish.sh` and
`check-packages.sh` ok on the four archives of `2026.11.16.2` (the game's Linux archive 42.9 MB, 55 files; Windows
39.1 MB, 48), the self-test 119 breaks, 119 caught; `end-to-end.sh` from that fresh publish: 241 of 241 on the GPU
(118 s) and on lavapipe (222 s); the race oracle run again gives the same bytes. Rebased on step 12, `editor-fixtures.json`
was made again by the tests and `preview-effect` captured again on top of step 12's (its seven new sounds push the
`data/races` folder below the list's fold: only the scrollbar's thumb differs from step 12's picture).

### Step 2: the actor record, its rules and the oracles (2026-11-15 and 16, the branch `gm2-step2`)

**The oracle** (GM2-P13; `Tools/OldGameOracle/`, `Tools/Scripts/make-old-game-references.sh [module …]`). The old
game's own code run on a case list, its results committed in `Tests/Statventure.Game.Tests/Reference/old-game/
<module>.json`, so the port's tests compare where there is no Node or Chromium. The old game is only read.
- **A module** is `Tools/OldGameOracle/modules/<name>.cjs` (CommonJS): `where` ("node": the old game's pure modules,
  loaded with `require` by the cache's Node 24, which `env.sh` finds; "client": the old page in its headless test client,
  Chromium 152 through `chromium.sh old-game/test-client --script --call=oracleMain`), `sources` (the old-game files the
  results depend on; their SHA-256 go with the results), `cases(old)` (the case list, made in Node: `old.json(path)` reads
  an old-game data file, `old.cases(file)` a case file of `Tools/OldGameOracle/cases/`; random spreads only from
  mulberry32 with fixed seeds), `run(input, …)` (one case's output; in the page `run(input, setup)` with the old game's
  globals), optionally `setup(old)` (data made in Node once and given to every run in the page: crafted races) and `uses`
  (helper files of `shared/` put before the module in the page's scope: `sheet-page.js`). `oracle.cjs` runs a Node module
  in place; for a client module it writes the page's script (the shared helpers, the module, the case list and the setup
  as JSON, `oracleMain` running every case), the script runs the test client with `TMPDIR` a folder of its own (the
  client's fresh profile), the results leave through the test preload's save hook (`saveCharacter`, which needs a `stats`
  key and takes 10 MiB at most: a module with more splits its cases), and `oracle.cjs client-result` checks the page ran
  exactly the case list before writing. The script runs inside a headless cage of its own when cage exists (the PC), else
  `chromium.sh` takes Xvfb (the cloud). Results: `{ "module", "made", "sources": { path: sha256 }, "cases": [ { "input",
  "output" } ] }`, one case a line; numbers as V8 writes them (read back as the same doubles), the non-finite ones as
  "Infinity", "-Infinity", "NaN". The same old game and case lists give the same bytes (the script run twice on the final
  tree: no difference). `OldGameReferenceTests` holds every module to its results file and every source's hash to the old
  game as it is: **a change to the old game fails the suite until the script is run again** (the port is never checked
  against an old game that is gone; chosen over a silent staleness, as the game-screen captures have).
- **The modules now:** `combat-rules-levels` (Node: `experienceRequired`, `sameLevelVictories`, `enemyExperience` at
  levels 1–200 and seven whole levels outside, 207 cases); `limitless-cap` (Node: `cap` off and on, the constants);
  `new-hero` (the page's `defaultCharacter()` in the port's words); `race-fold` and `race-rules` (step 1's race oracle,
  folded in at this step's rebase so there is one oracle and one way to make it: its case list moved to
  `cases/race-cases.json`, its script's two parts as two modules, the same 3,696 fold cases and the same values, the
  rules' five parts one case each; step 1's `RaceOracle` reads them there); `sheet-values` (311 cases: what the sheet shows,
  through the old `displayedBaseStat`, `currentStats`, `effectivePermanentBonus`, `effectivePermanentDefenseBonus`,
  `calculateMaxHpWithArtifacts`, `calculateMaxManaWithEffects`, `updateDerivedValues`' Armor and Evasion and its hold of HP
  and Mana, `updateEditingControls`' controls, the EXP card's texts; the bar's width caught from the old code's own
  assignment, as the CSSOM shortens a number read back); `sheet-actions` (488 cases: each handler pressed with its button
  enabled first, so its own checks decide: the "+" and "−" steppers, respec, level up, `adjustResource`, Full HP, Full
  Mana, Restore both, Dev Mode's attribute, bonus, Armor/Evasion bonus, purse and EXP fields, `selectRace`; the sheet
  "settled" once shown, then after). Their sheets: the new hero, sheets with no race, every race of `races.json` (the new
  hero, Level 40 with nothing raised as the old checks make it, and spreads), Limitless's cap, the edges (bonuses below
  −100 %, attributes far below 0, Armor in the thousands, resources out of range), a crafted race (`stubs.race`, the old
  file's form, id `stub-race`, put in the page's catalogue through `normalizeRaceDefinition` and converted in the tests by
  step 1's `OldRaceFile`: the real fold reads it) and hand-made totals for the sources GM2 doesn't have (the artifacts and
  the worn equipment GM3's, the classes GM8's, the battle's overrides GM5's), which the page puts in place of the old
  functions (`calculateArtifactEffects`, `calculateClassEffects`, `calculateEquipmentDefense` and its kin,
  `window.Combat`'s five) and the tests' doubles repeat: every term of every formula is reached, not only those GM2
  fills.
- **Adding a module or a case** (a later milestone's): write `modules/<name>.cjs` (its `where`, `sources`, `cases`,
  `run`), run `Tools/Scripts/make-old-game-references.sh <name>`, commit the results, and test the port with
  `OldGameReference.Load("<name>")` (its cases' inputs and outputs, `OldGameReference.Number` for the non-finite ones):
  the tests read the cases from the results, so a case is added by changing the case list (or the shared generators of
  `shared/`) and running the script again. A large list kept as data goes in `cases/`.
- **Found:** a new hero's HP is **31**, not the design's 30 (`⌊(15 + 5 + 5) × 1.25⌋`; corrected in "What exists" and "The
  save, version 2"); the EXP bar's `style.width` read back is the CSSOM's shortened number (seven decimals, cut), so the
  oracle takes the old code's own string.

**The actor record** (`Game/Statventure.Game.Data/Actors/`, GM2-P1): `actor.sheet` v1 (`ActorSheet`, `[NotAPackKind]`, no
`[Kind]`, as `save.game`): `race` (the race's id, none for a new hero: written left out), `level` 1, `experience` (a double:
Dev Mode's EXP form takes fractions) 0, `freePoints` 0, `classPoints` 1, `attributes` (`actor.attributes`, eight whole
numbers) 10 each, `spentPoints` (`actor.attributes`) 0 each, `bonuses` (`actor.bonuses`: the eight attributes', Armor's and
Evasion's percentages, doubles) 0, `hp` 31, `mana` 20, `purse` (`actor.purse`: silver, gold, platinum) 0, 5, 0: a new
`ActorSheet` is the old `defaultCharacter`'s new hero (the oracle's `new-hero`). Each field's `[Help]` names the old field.
The parts are `[NotAPackKind]` (never an entry). No `[Check]`: the old `normalizeCharacter` clamped rather than refused
(a level to 1–cap, points, coins and HP to 0 or more); whether a character file outside those is refused or clamped is
step 6's (it reads the save). Its layout and fixtures (`new-hero`, `played`) are in the library's guard.

**The rules** (`ActorRules`, GM2-P2: functions of an immutable sheet and a `SheetContext`; values computed when asked):
- **Values:** `BaseAttributes`, `CurrentAttributes` (`AttributeValues`), `ShownBonus` / `ShownDefenseBonus` (the bonus
  fields), `MaxHp`, `MaxMana`, `Defenses` (Armor's and Evasion's base, modified and current), `ExperienceRequired`,
  `AtLevelCap`, `LevelCap`, `CanLevelUp(devMode)`, `LevelUpAlert`, `CanRaise(devMode)`, `CanRespec`, and `Evaluate(sheet,
  context, devMode)` with all of them for a screen (`SheetValues`, HP and Mana held, the EXP remaining and the bar's
  percentage). `MaxHpFormula`, `MaxManaFormula`, `EvasionFormula`, `BaseArmor`, `BaseEvasion` are the old constants and
  formulas. `RaceOf`, `RaceTotalsOf` (step 1's `RaceTotals.Of` at the sheet's level with the battle's enemies).
- **Actions** (each a new sheet; the same instance when the old game refuses): `Raise`, `Lower`, `Respec`, `LevelUp`,
  `ChangeResource(resource, amount)`, `RestoreResource`, `RestoreBoth`, `SetBaseAttribute`, `SetBonus`,
  `SetDefenseBonus`, `SetPurse`, `AddExperience` (Dev Mode's: refused outside it, where the old fields are disabled),
  `ChooseRace(race, devMode)` (`RaceChoice`: the sheet, or `NotPlayable` / `Locked` for the old status lines; the texts are
  the screens'), and `Settle` (HP and Mana held to their maxima, as `updateDerivedValues` holds them; every action ends
  with it, as the old handlers end with that call).
- **The context** (`SheetContext`): `Races` (by id: `WithRaces(RaceCatalogue.Load(data))`), `Artifacts`, `Classes`,
  `Equipment` (functions of the sheet giving `ArtifactSheetTotals`, `ClassSheetTotals`, `EquipmentSheetTotals`: zero
  until GM3 and GM8; the equipment's defence a function of the current attributes, as an item's power reads them),
  `Battle` (`IBattleSheet`: the battle's maximum of a resource, its stat and defence percentages, its value of an
  attribute and of Armor or Evasion, its enemies; `NoBattle` outside one: no maximum, nothing added, Armor and Evasion
  floored and held at 0 or more, as the old `Combat.defense` does with no battle), `Limitless` (GM15's cap of 200; level
  ups above 100 give no class point already). `SheetContext.Plain` has none of them.
- **Numbers:** step 1's `JsValues.ToNumber` / `ToInteger` (one helper for those; this step's own were dropped at the
  rebase), and `JsNumbers` for what it lacks: `Round` (V8's halves toward +∞; the curve's), `OrZero` (`x || 0`), `ToInt` (a
  whole double into an `int` field, saturated). Attributes and defences are named by step 1's keys (`AttributeKeys`,
  `DefenseKeys`) and their numbers held in its `AttributeValues` and `DefenseValues` (this step's own enum and number types
  were dropped at the rebase, so there is one of each). `Levelling` holds the curves and the cap (`Cap`, `BaseCap`,
  `LimitlessCap`, `PointsPerLevel`); the curve's `pow` is the C library's, and a test shows no level's value lies near a
  half, so any `pow` within an ulp rounds the same.

**Choices made** (internal; none changes what a player sees): attributes, points, HP, Mana and coins are whole numbers in
the record (the old save floors them on load), EXP and the bonuses doubles; Dev Mode's attribute field floors the stored
attribute at once (the old page keeps a fraction until the next load for a race with a fractional modifier, which no race
has); a coin count past `int` is held at its end; Dev Mode's fields refuse outside it (the old page disables them);
`ChooseRace`'s refusals as reasons, their words left to the screens; the race-oracle fold (above); the stale-oracle test.

**For later steps.** *Step 6 (the save v2):* the hero is `ActorSheet` (record `actor.sheet` v1), the save's field as
GM2-P5 has it; `new ActorSheet()` is v1's `Upgrade()` hero (HP 31); the session's `Change(save => save with { Hero =
ActorRules.LevelUp(save.Hero, context, settings.DevMode) })` saves each result; a character file holds it in its JSON
form; out-of-range values (the old game clamped) are its question. *Step 7 (the sheet):* `ActorRules.Evaluate(sheet,
context, devMode)` gives every number and control state the page shows (`SheetValues`), the texts' formats are in
`ActorRulesOracleTests` (the old `formatValue`, "CAP 100", "/ N EXP", "MAX LEVEL", the helper lines, the bar's width and
`aria` values, all checked against the old page) and in `sheet-values.json`'s `texts`; each control calls its action and
hands the result to `GameSession.Change`; the context is `SheetContext.Plain.WithRaces(RaceCatalogue.Load(<the game's data>))` (`Application.Data`).
*Step 8 (the carousel):* `ActorRules.ChooseRace` then HP and Mana are filled (`selectRace`'s), `NotPlayable`/`Locked` for
its status lines. *GM3:* the artifacts' totals, the worn equipment (its defence, caps, HP percentage, Peace and Balance)
and the race's equipment restrictions on choosing a race; *GM5:* `IBattleSheet` per actor (in place of `withActor`; a
battle's fractional maximum would need HP as a double: a new version) and the battle side of the race; *GM8:* the classes'
totals and spending class points; *GM9, GM11:* an enemy or a companion is an `ActorSheet` with its own parts beside it (a
name and look, an enemy file's classes and gear; `maxHpPercent` and `baseArmor` of an enemy file are GM9's); *GM15:*
`Limitless` in the context, its record (`noteSpent`, the respec clearing its count) beside the sheet.

**Tests** (game; the counts in "Runs"): `LevellingTests` (the curves at every level against Node, the cap, the curve
far from halves, `RunLevellingCurveChecks` with each expectation of `small-changes-browser.js` 102–118, JavaScript's
round), `ActorRulesOracleTests` (every case of `sheet-values` and `sheet-actions`, number for number, every race of
`data.pak`, the crafted races and every stub kind among them), `ActorRulesTests` (`RunMapChecksLevelIcon`:
`map-browser.js` 15–22; `CheckGiantsBlood`: its maximum HP on the sheet, step 1's test having its race totals, and the
human's 48 HP against the Goliath's 40 at Level 40 that task 11 found; Dev Mode's refusals; refused actions giving the same
sheet; a race the data doesn't have; the context's races), `ActorSheetTests` (the new hero is the old one, the marks, both
forms, the fixtures, the parts by key), `OldGameReferenceTests` (every module has its results and the results are of
today's old game). `checkNewRaceData` and `checkNaturalClaws` are step 1's tests (race data). The library's guard covers
`actor.attributes`, `actor.bonuses`, `actor.purse`, `actor.sheet`; the editor's `LibraryKinds` is 33.

**Shown able to fail** (`mutate.py` with the game's project, under the shared lock; `claude-work/gm2/step2/`: the list
made by `make-mutations.py` before the rebase, `make-mutations2.py` after it, `mutations2.json` the standing one): 220
mutations of every rule: the curves and the cap; the race's lookup and fold at the sheet's level with the battle's enemies;
the current attributes (each percentage, Peace and Balance's lock, the flat amounts, the floor, the battle's value); the
shown bonuses; the formulas, their floors and their holds at 0; max HP and Mana (each flat amount and percentage, the
battle's maximum); Armor and Evasion (each term, each cap, the battle's value); the controls; `Settle`; every condition and
term of each action; Dev Mode's refusals; choosing a race (each refusal, the fill); the record's defaults and marks; the
parts' keys; JavaScript's round, `|| 0` and the whole-number hold; the defence with no battle. The first runs (before and
after the rebase) found gaps, each closed by cases of the oracle's own (Peace and Balance against a lowered Speed, the
formulas below 0 under flat amounts, Armor in the thousands, armour that grows with Strength, caps below the values, every
action drawn from each pool of sheets, a race that can't heal, the bonus fields with every source in, HP and Mana below 0
when shown, a crafted race's modifier before it is chosen). **The standing list on the final references: 220, 212
killed, 8 that can't differ:** the level up's heal not floored (an `int` field takes the value by truncation, and it is
never below 0, so that floors it); the level up's heal and HP and Mana by hand past the maximum or below 0 (5: `Settle`
holds them again at the end, as the old `updateDerivedValues` does after `adjustResource` and the level up); no damage
to a race that can't heal (amount 0, the only one that differs, changes nothing on a shown sheet, and the old button
never passes 0); Dev Mode's attribute field not floored (with a whole race modifier `floor(typed − m) = floor(typed) −
m`, and every race's is whole). **The oracle tooling's own rules** (`script-rules.py`, a runner of its own: a rule broken
in `oracle.cjs`, a shared helper, a module or the script; the script run on the modules it reaches; killed when it fails or
its results differ by a byte; under the lock and alone): 20, **17 killed, 3 that can't differ:** the page's case-list
check removed (it guards against a broken page: "the page skips its last case" is killed by it), the results written
without the non-finite encoding (no Node module gives a non-finite number, and the page encodes the client modules'
itself), the crafted races numbered from 0 (`normalizeRaceDefinition`'s index only names a race in its messages). Nothing
written to the player's real folders.

**Runs** (headless, scratch per-user folders, nothing audible; on the step's tree rebased on `bf59d0d`, `main` with steps
1, 3a and 12): the full suite on the GPU (293 s with the build) and on lavapipe (309 s): **4266 + 630 = 4896 tests**
(`main`: 4266 + 610 = 4876; 20 new, all the game's), none held or skipped; `make-old-game-references.sh` with every module
run twice on the final tree: not a byte differs.

## Status

- **2026-11-15:** written on the user's go for GM2 (docs only, the branch `gm2-design` from `8c7d07d`), after the user's
  eight answers (GM2-1–GM2-8) and the word recorded as GP-8: the plan's GM2 section checked against the code of the old
  game and the engine (what moved: "Where the plan was wrong or has moved"), the method, the design, 39 questions for the
  user's detail round (Q1–Q39 in ten rounds) and 16 proposals for the manager (GM2-P1–GM2-P16), thirteen draft steps in
  five waves. **The detail round waits for the user**; nothing decided beyond the eight answers and GP-8; no step started.
- **2026-11-15, the design decided:** the user's detail round, Q1–Q39 and the manager's follow-up Q40, answered the same
  day (twelve not the recommendation: heroic proportions, stylised faces and mitten hands, a sword and shield with a chain
  shirt, one plain outfit per race and sex, a large set of parts, presets as a list with random and no locks, the
  Half-Beast's parts free, optional marks, a race carousel, the sheet's figure idle in a fixed view, a dark portrait panel,
  nine new emblem motifs); the manager's GM2-P1–GM2-P16 all (a), with the notes on P7, P10 and P16. The design, the
  counts ("What GM2 makes"), the answers' tensions and thirteen steps (9 in five parts) in five waves are final; two
  questions the answers opened (Q41, Q42) waited to be asked. No step started.
- **2026-11-15, Q41 and Q42 answered** (the user, the question tool, both the recommended option): the carousel's and the
  maker's figures idle in the sheet's fixed three-quarter view; the Goliath's and the Merfolk's presets wear marks.
  Nothing is open; no step started; the first wave starts on the manager's go.
- **2026-11-15, step 3a done** (a worker, the branch `gm2-step3a`; step 3 split into 3a and 3b by the manager): Blender
  5.2.0 pinned in the build cache and proven (109 of 109 reference files byte-equal with it), `Tools/Models/humanoid/`'s
  frame with `look.py` (today's look the default) and the render entry point, `make-humanoid.sh`, the sandbox's figure
  mode with `play-figure.sh`; 4214 + 565 = 4779 tests (84 new) passing on the GPU and on lavapipe; mutations 125, 120
  killed, 5 accounted for ("How it works", step 3a). 3b, the look, is the manager's
  (the user chose "Ember, hearth" on 2026-11-15).
- **2026-11-16, step 12 done (the branch `gm2-step12`; "How it works", step 12):** the seven sounds of Q38, approved by the
  user's ear on 2026-11-15 ("b c b a c b a", no changes), in `Assets/Audio/effects/` and `audio.pak` (133 entries),
  named in `SoundNames` (`Hero`, `All` 15), each a fixture against Chromium; their weight in an action (5) and the wiring
  for steps 7 and 8 (GM2-P17). With the manager's leave, the engine rule the editor's `preview-effect` met at 1.65 once
  its list overflowed (Chromium's `DrawingDisplayItem::TightenVisualRect`) ported with a fixture of its own; it moved
  seven comparisons nearer to Chromium and none farther, and closed the known item "A status line's text grayscale at
  1.65 beside a tall scroller". The full suite on the GPU and on lavapipe: 4265 + 576 = 4841 tests (`main`'s 4214 + 565
  and 62 new), none held; mutations: the sound side 14 of 14 killed, the rule 9, 6 killed and 3 accounted for; the
  0.3 s rule by hand on the source and the pack.
- **2026-11-15/16, step 1 done** (the branch `gm2-step1`; "How it works", step 1): the `race` kind and the `effect` union,
  the race fold matching the old game on all 3,696 cases of its test client, the 28 races in `data.pak` from a converter
  and a pack script, the stand-in kind gone. It raised two questions, answered by the user as recommended (Q43 the races'
  order as a number, Q44 the emblem as a name). The `settings-mods` screens the user approved in GM1 changed only in their
  fixture's refusal text (accepted by the manager); 12 editor goldens show the new `data/races` folder. 4266 + 610 = 4876
  tests on the GPU and on lavapipe; mutations 113 standing, 110 killed, 3 that can't differ; the pack script's 14 killed;
  the end-to-end flow 241 of 241 on both. Found for later steps: a race's `spell-power-percent` is read by nothing in the
  old game (GM5's to decide), and the engine's `Focus()` doesn't scroll into view as Chromium's `focus()` does.
- **2026-11-16, step 2 done** (the branch `gm2-step2`; "How it works", step 2): the old game's oracle
  (`Tools/OldGameOracle/`, `make-old-game-references.sh`: seven modules, Node and the test client, results committed and
  reproduced byte for byte; step 1's race oracle folded into it), `actor.sheet` and its rules in the data library on step
  1's races, matching the old game on every case (the curves at levels 1–200, 311 sheets' values, 488 actions, real and
  crafted races and hand-made totals for every source GM2 lacks). 4266 + 630 = 4896 tests on the GPU and on lavapipe;
  mutations 220 standing, 212 killed, 8 that can't differ; the tooling's 20, 17 killed, 3 that can't differ. Found: a new
  hero's HP is 31, not 30 (corrected above). Left for step 6: whether a character file with a sheet out of the old ranges
  is refused or clamped (the old game clamped).

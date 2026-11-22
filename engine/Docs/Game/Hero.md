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

**GM2-P18, a look's models are references, refused when missing; bodies, parts and presets enter `data.pak` with their
models (the manager, 2026-11-16, on step 6's question):** a model named by a `look.body` or a `look.part` is a
`[References]` to the engine's `model` kind, so a missing one is "no model …" at the field in the editor and in the mod
check, exactly as a sequence's stage; the checks know only "a model that is there" and the record's own rules (race, sex,
build, category), no path form (step 4a's `models/humanoid/bodies/<race>-<sex>-<build>` and
`models/humanoid/parts/<category>/<part>/<body>` are examples they accept). Since the mod check checks the game's records
under every mod, nothing in `data.pak` may name a model before its file is in `models.pak`: step 6 commits the kinds, their
checks and tests on the tests' own data, and the 72 presets' catalogue data waits outside `data.pak` (in the oracle's
results), moved in by one step when the models land ("How it works", step 6). (Not a model path checked for its form only,
a missing file drawn as nothing: a typo would show only as a missing piece of a figure; not the game's own data excused
from the model check until GM2's end: no temporary rule in the mod checker.)

**GM2-P19, a model's pieces joined per ink flag (the manager, 2026-11-16, on step 4a's cost study):** every model joins its
pieces at export into one object per ink flag (the pieces the ink draws, and those marked `ink = 0`), so it has one
primitive per material and ink flag (`kit.join_by_ink`). Blender's reference joins a model's meshes for Freestyle anyway,
so the look is the same; a geared figure's 101 primitives become 23.

**GM2-P20, two detail levels from one sculpt (the manager, 2026-11-16, with Q54):** `levels.py`'s "full" (the sheet, the
maker and the carousel: one figure seen large with no scenery, lightened only where it can't be seen) and "battle" (the
stage: up to sixteen small figures with shadows), built by the same scripts from the same sculpt; the screen that builds
an actor chooses the level, the engine gets no level-of-detail machinery. GM2 ships the full level; the battle level is
proven and measured on the first humanoid (the tests' data) and every body's battle models are built when the battle
comes (GM5).

**GM2-P21, a stance is a loop of the arms (the manager, 2026-11-17, with the clips; recorded by step 4b):** a stance is a
loop of the arms only (shoulders, upper arms, forearms, hands, fingers, thumbs), as long as the idle (120 frames), made on
the idle's body. The battle lays it over the arms of whatever else plays (idle, moving, reactions, guard, evade), its own
time running; a weapon's own attack is the whole body's and starts and ends in its stance's first frame. The one-handed
stance has no loop: its arms are the clips' own. GM2 only puts the loops in the library and proves they play; laying one
over another clip by a bone mask is the battle's (GM5), an engine item for it (`Docs/Game-Plan.md`, "Engine items the port
will meet"). (Over the idle alone the engine's NLA layering does it already: a Replace layer changes only the channels
its clip keys; step 4b's figure mode shows it, "How it works", step 4b.)

**GM2-P22, the hips' place scaled by the legs' length; the figure's ground (the manager, 2026-11-17, widened 2026-11-18;
recorded by step 4b):** a clip's hips' place is metres keyed on the man; on a body with shorter or longer legs it must be
scaled by the legs' length (the low stance, the crouches and the leaps would otherwise sink a small body's feet into the
ground or lift a large one's off it). An engine task of its own before step 11's live figures. Widened on 2026-11-18 with
what step 4b measured: the ground is where a body's soles are at rest, and the man's and the woman's boots' soles reach
24.0 mm under the plane of their origin (the bare feet stand on it; `outfit.boots` displaces the sole down and its shell is
10 mm thick), so a figure must be placed with its soles on the ground, not its origin; the task becomes "the figure's
ground" (`Docs/Game-Plan.md`, "Engine items the port will meet"). **Done 2026-11-20** (a worker, the branch
`gm2-figure-ground`; "How it works", "The figure's ground"): every clip of the family carries its measure, the left leg
(`thigh.L` → `shin.L` → `foot.L`), and a clip's bone places are scaled by its length on the playing body over the man's
(the woman 0.968; exactly 1 on the man, nothing of his changed); an actor stands by its soles (`Actor.Ground`, the lowest
point at rest of its body and garments: −23.997 mm on both human bodies), which the game's figures and the figure mode
set; Blender's side the same where it evaluates, renders or assembles a figure. Every later body, the smallest and the
largest included, plays the clips by this rule; the user sees them on the hobbit and the Goliath in step 9 (Q59).

**GM2-P23, the keying scripts into the repo later (the manager, 2026-11-18; recorded by step 4b):** the manager's keying
scripts (`claude-work/gm2/step4/handover-clips/kit/`) go into the repo in a task of their own after step 4b, so a clip can
be keyed again there; until then the data in `Tools/Models/humanoid/clips/` is the clips.

**Two things the clips' pictures did that the game must do (the manager, 2026-11-18; recorded by step 4b, with their
milestones in `Docs/Game-Plan.md`):** GM5: `evade` has no root motion; the battle moves the figure back between the markers
`leap` and `land` (0.85 m in the pictures the user approved: "i want the character to fully jump back") and brings it back
afterwards. GM3: a real bow's string runs from the limbs' tips to the drawing hand's socket while the hand holds it (in the
bow's stance and in `bow` up to the marker `shoot`), straight between the tips otherwise, and an arrow lies on it until
`shoot` (the user: "The bow string does not move").

**The manager's tooling choice on the clips' ease (2026-11-18, on step 4b's measurement):** every clip keyed at every frame
is written `"ease": "linear"` (23 clips; `flee` and `meditate` keyed at every frame for it), the sparse ones (idle,
stunned, stand, death, guard-hold, evade-hold, the five holds) stay `"bezier"`. With a key at every frame and Bézier keys,
Blender's auto-clamped handles bend each quaternion component inside a frame, where the engine draws the exported frame
samples straight (glTF's rule); the game only ever sees the frame samples, so nothing it shows changed, and Blender's
in-betweens now agree with the engine's under Animation.md's rule as it stands ("How it works", step 4b).

**GM2-P24, a posed mesh's normals the whole object's (the manager, 2026-11-18, on step 4b's posed samples):** two of step
4b's posed samples, the geared man and woman at the swing's hit, pass `ReferenceLimits.DetailedFigure` in every measure but
the edge pixels' (on the chain shirt's skirt in the lunge). No limit changes and no view is swapped: a task of its own
after step 4b's merge makes a posed mesh's normals the whole object's, across its primitives, as Blender's evaluated mesh
has them (the engine recomputes them per primitive); it proves that cause first (suspected, not proven), then adds the two
samples back under the user's limits, or comes back with numbers for the user. Step 4b leaves them out of its tests and
references and hands over everything they need (`claude-work/gm2/step4b/pending-samples/`; "How it works", step 4b).
**Owed beside it (GM2-P35, 2026-11-21):** two assembled close-ups of step 9a, the hair locks' seams; their files in
`claude-work/gm2/step9a-repo/pending-samples/` ("The manager's answers to step 9a" below).

**The manager's answers to step 4a (2026-11-16):** Blender runs with 8 threads for the family's models, always
(`make-humanoid.sh`; QuadriFlow and the order of the edges Blender makes follow the thread count); the clip frame's test
clip `rest-sway` stays out of the packs (the tests' data; the family's `clips.glb` comes with its first real clips); the
tunic as the outfit sheet's state; the slot table as built, held by a test that fails on a material that is neither a
look's slot nor a fixed colour; a figure's own hair and iris colours are step 5's; the cost of a frame's animation work a
measurement without a bound; a comparison over M6's limits stops for the manager (no limit or scene changed by the
worker: then Q55). The two addenda after the hand-over (the manager's, 2026-11-16): a garment cut from the body takes the
body's weights at the nearest point of its surface and every skirt the body's own heights; the hair cap's edge turned in
under the skin, every skirt's rings at the body's own heights and the mail's skirt flared clear ("How it works", step 4a).

**The manager's answer to step 5 (2026-11-18):** the human race's swatches are step 9a's, approved there with the human's
builds, faces and presets (step 5's "Finished": the sample looks now, the presets as 9 lands them); until then the eight
human presets draw their models' own placeholder colours, the expected state, and no comparison or golden holds those
colours as a look ("How it works", step 5).

**The manager's decisions with step 9a's art (GM2-P25–GM2-P30, 2026-11-18 to 20; recorded by step 9a from the manager's
hand-over, `claude-work/gm2/step9a/handover/HANDOVER.md`):**
- **GM2-P25 (2026-11-18):** a race's three builds share one skeleton per sex; so a face, a hair style, a beard, a head
  piece and a mark are made once per sex (on the average body) and serve all three builds: one `look.part.model` with
  three bodies. Checked: long hair made on the average body lies right on the slender and the strong one.
- **GM2-P26 (2026-11-19):** a beard is made to the widest jaw, fullest chin and highest cheekbones among its race's and
  sex's faces, so one model fits all of them; beards are offered to men only, as in the old game, and take the hair's
  colour (the slot `hair`).
- **GM2-P27 (2026-11-19):** one-sided parts (the flower, hair beads, the feather, one-sided marks, the side braid) are on
  the figure's right, the side the battle and the sheet's three-quarter view show. A band (circlet, headband, wreath) lies
  close to the skull (1.05 of its radius) so it never floats on a shaved head; full hair covers part of it.
- **GM2-P28 (2026-11-20):** head pieces take the outfit's slots (`cloth`, `cloth.trim`, `leather`), none of their own.
  `brows` (which the lashes and the mouth's line share) and `eye.white` have one swatch each: no choice.
- **GM2-P29 (2026-11-20):** the maker offers a colour only when the body or a chosen part uses its slot (a man has no lip
  colour; a tie colour only with a tied style; a mark colour only with a mark). A look still holds a swatch for every slot
  its race has swatches of, as step 6 checks.
- **GM2-P30 (2026-11-20):** against Blender, 9a compares the eight presets, the four new bodies and a handful of looks
  chosen for their demanding parts; every other model is proven by the structural walk and `make-humanoid.sh check`.
  (Comparing every one of some 150 models would add tens of megabytes of references.)

**The manager's answers to step 9a (2026-11-21, on the worker's report):**
- **GM2-P35, two close-ups owed to an engine task:** two of 9a's assembled close-ups pass `DetailedFigure` in every measure
  but the shading's inside pixels (step 5's woman close-up with the Ranger's long hair 31 / 39 of 24 allowed, the slender
  woman's twin braids close-up 26 / 22: faint seams 3 to 28 levels darker where hair locks meet on lit hair, Blender flat).
  No limit is touched: the twin braids close-up leaves the tests, step 5's red close-up goes back to the long tied hair it
  passed with (18 / 16), both failing samples' files wait in `claude-work/gm2/step9a-repo/pending-samples/` (README there),
  and an engine task of its own finds the cause and brings both back before GM2 is ticked (`Docs/Game-Plan.md`, "Engine
  items the port will meet"; owed beside GM2-P24's). The twin braids' look is compared at the sheet's three-quarter view
  instead (it passes honestly: 2 / 3), so none of the six demanding looks is missing.
- **GM2-P36, a part's place in its category's list:** `look.part` gains an `order` (version 2), as a preset has in its race's
  list; each category's options by it, the same place by name; the game's in the approved lists' order (`parts.json`) with
  "None" first (0, then 10, 20 …). The maker, the random button and a refit use it (a hero switched to a man takes "None"
  for the beard). The form: "How it works", step 6 ("The maker's records"), and step 9a.
- **The hair beads' slot** (step 9a's one word, `metal.trim` to `cloth.trim`): agreed, GM2-P28's; the manager's own copy
  follows. **`hair.tie` and `marking` in the game's look file:** left as they are (the figure mode shows its placeholders for
  them; the model test names them as the swatches' alone).

**GM2-P31, the emblems' colours (the manager, 2026-11-20; recorded by step 10):** an emblem's materials are six palette
slots of its own (`emblem.rim`, `emblem.field`, `emblem.high`, `emblem.metal`, `emblem.accent`, `emblem.cut`), filled from
the race's accent colour (`race.accent`) by one rule: rim and metal the look's gold `#bb8d3c`; high the bone `#e1d3ad`;
field the accent mixed 58 % toward the ink `#1a120d`; accent the accent mixed 18 % toward the bone; cut the accent mixed
90 % toward the ink. So a race record's accent colours its emblem, and the rule is one place in the game and one in the
scripts (`Figures/Emblems.cs`, `Tools/Models/humanoid/emblems/palette.py`; "How it works", step 10).

**GM2-P32, the medallion's make (the manager, 2026-11-20; recorded by step 10):** one unit is the field's radius, 0.08 m;
the body turned (128 sides) with a rim to 1.33 units carrying 40 beads, the field set 0.145 units under the rim's top, a
plain back; each raised outline a slab chamfered at its top (`hi` 0.070, `acc` 0.056, `mid` 0.050 units off the field; an
element lying over an earlier one at least 0.022 higher); a `lo` element a thin inlay with no ink; the front along the
figures' front (glTF's +Z), the medallion's middle at the origin.

**GM2-P34, how the emblems' small pictures are made (the manager, 2026-11-21, on step 10's question):** every picture is
the emblem's 640 px render (the approval renders' camera and size) scaled down, as the small copies the user approved were
(`emblem_render.py`'s sheet scaled the 640 px renders with Pillow's Lanczos); a render made at the small size itself has the
look's 3 px lines swallow the medallion (at 72 px its 40 beads become a black ring, the 36 px one mostly ink:
`claude-work/gm2/step10-repo/try1/compare.png`). The filter step 10 chose: Lanczos of three lobes stretched to the scale,
in sRGB with the cover multiplied in, the scaling part of the regenerable script ("How it works", step 10).

### Questions from the steps (Q43–Q67 and Q70–Q73, the user, 2026-11-15 to 2026-11-21)

Q43 and Q44 raised by step 1 (the `race` kind's fields; the docs didn't answer them) and asked by the manager with the
question tool the same day; both the recommended option. Q45 asked by the manager on 2026-11-16 for step 6 (step 2 had
left it open: whether a character file with a sheet out of the old ranges is refused or corrected). Q46–Q49 the user's
verdict on step 7's approval captures and Q50 raised by step 7's comparisons, asked by the manager with the question tool
on 2026-11-16, every one the recommended option. Q51–Q53 asked by the manager on 2026-11-16 on step 8's approval
stop, each answered as recommended. Recorded in `Docs/Game-Plan.md`, "GM2's detail round", too.
- **Q46 The sheet's captures: "Yes, approved"** (2026-11-16): the layout and look of step 7's captures stand
  (`claude-work/gm2/step7/approval/`, the old game's page beside each).
- **Q47 Under the hero's name: the race and the level** ("HUMAN · LEVEL 30", in the lettering of the race's name under
  the seal); the old copy line ("A quiet place to keep track of what makes your hero extraordinary.") dropped. (Not the
  old line kept; not both.)
- **Q48 The seal outside Dev Mode: in full colour.** Inert ("<race> · Race locked") but not dimmed. (Not dimmed to half
  as the old game's disabled buttons are.)
- **Q49 The paper doll as shown:** three equal columns; the portrait panel in the middle in its dark leather frame with
  brass corner brackets and a brass hairline, its window the Ember look's dark brown (`#2a1f1a`, the "hearth"
  candidate's background), 480 px tall, the same in both themes; the hands' slots at its sides at its middle; Armor and
  the two Artifacts in a row below. (Not a larger panel.)
- **Q50 The race seal in the sheet's picture comparisons: left out** ("Leave the seal out"): the seal's box is masked in
  every comparison of the sheet with Chromium; its emblem is compared on its own against Blender (step 10), and its 11°
  tilt is the canvas's known difference on slanted box edges (decided 2026-11-05; measured by `box-slant`). (Not those
  pictures at the rotated-box limit, 12 levels / 1 %; not an engine port of Skia's coverage for tilted boxes.)
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
- **Q45 A character's numbers out of range** (2026-11-16, the question tool, asked by the manager). A save or a character
  file can hold a sheet whose numbers the game never makes (a level of 0 or above the cap, points, coins or HP below 0: a
  file edited by hand); the old game's `normalizeCharacter` corrected them on every load. **Answer: corrected silently,
  as the old game** (its `normalizeCharacter`: the level into 1 to the cap, points, coins and HP to 0 or more), with no
  message and without refusing the file. (Not "correct and say so"; not "refuse the file".) The look is different and
  stays as designed: its checks refuse a part the race, the sex or the build doesn't allow (G4). Built by step 6 ("How it
  works", step 6), field for field as the old function, its other corrections named there (the race's id through
  `slugify`, the EXP 0 at the cap and 0 or more, a bonus that isn't a finite number 0).
- **Q51 The race carousel's captures** (2026-11-16, the question tool, asked by the manager on step 8's approval set,
  `claude-work/gm2/step8/approval/`): **"Yes, approved"** as captures 01–03 and 09 show it (a playable race, another
  race than the hero's own outside Dev Mode, Dev Mode on an enemy-only race; the smallest window), beside the old picker.
- **Q52 The character maker's captures** (the same day, the same set): **"Yes, approved"** as captures 04–07 and 10 show
  it (the presets' list, a category open, after the random button, a race with ears and a tail; the smallest window).
- **Q53 A playable race with no looks in the data** (the same day; raised by step 8: the shipped data has no body yet
  until steps 4a and 9, and a mod's playable race may have none; the old game took a race without looks at once, with no
  look step, `hasPlayableLooks`, where Q26 opens the sheet only with a race and a look). **Answer: "Shown, but not
  choosable"**, the recommended option: the race is in the carousel, its button disabled, the line "No looks for the
  <race> yet"; `open-maker` for it refuses the same way, with a log line. (Not the old game's "taken at once without a
  look"; not the maker opening with Begin disabled.) Dev Mode's enemy-only races stay as Q27 has them (taken at once, no
  look). Built by step 8 ("How it works", step 8).
- **The manager's, on step 7's reports (2026-11-16):** the 3D view's mask in the screen fixtures stays step 5's
  (GM2-P12); the sheet's panel draws nothing on either side until step 11, so its fixtures compare it unmasked, and the
  mask must be in place before a view draws there (steps 5, 11). The fixtures' mask hook (a fixture names boxes to mask
  by selector, nothing wider) added by step 7 for the seal (Q50), for step 5 to use for the `surface` boxes unchanged.
  `data.pak` mounted in the game's test harness (`GameUiHarness`), step 8's line the same. Step 7's leave to restate
  GM1's tests that expected the page after "New character" for Q26, and `end_to_end.py`'s Continue and load
  expectations, as small as the rule allows. The three engine differences step 7's comparisons found (`overflow-wrap`
  breaking a word under `white-space: nowrap`; a number field's spin button left out under `appearance: textfield`; a
  number `line-height` kept as a float) fixed once on a branch of their own, `gm2-engine-fixes`, merged before step 7,
  which rebases onto it. Accepted as step 7 did them: the page's end clearing the ⚙ button (`padding-bottom: 72px`), the
  fixture list carrying the page's scroll (step 8's fixtures get the same field), `SheetScreen.Ready` as Q26's rule
  until step 8's `HeroFlow.Needs` is merged (whoever merges second makes them one: step 8 did, `HeroFlow.Needs` with
  `Ready`'s words, the sheet asking it).
- The manager's, on step 1's report (a test fixture's sample text, not a choice about the game): the `settings-mods`
  screens' broken mod holds two races its checks refuse, so the row shows the game's real kind and a real refusal in the
  editor's words, still wrapping onto two lines ("How it works", step 1).

Q54 and Q55 raised by step 4a and asked by the manager with the question tool on 2026-11-16 (numbers fixed by the manager
so the steps' don't collide: steps 7 and 8 hold Q46–Q53).
- **Q54 The lighter figures** (2026-11-16, the question tool, asked by the manager on step 4a's cost study: about 152,000
  triangles and 100 primitives a geared figure). The first figures and the same figures rebuilt lighter on the same
  sculpted forms, side by side; named to the user as differing on purpose: the hairline (a clean arc; it was the jagged
  edge where the closed cap dived into the skull) and the collars and hems (smooth; they were crumpled). **Answer: "Yes,
  use the lighter ones."** Built by step 4a with GM2-P19 and GM2-P20 ("How it works", step 4a): a geared figure 57,546
  triangles in 23 primitives (the man; the woman 59,986 in 24).
- **Q55 The limits for detailed figures** (2026-11-16, the question tool, asked by the manager with step 4a's measurement
  and pictures). The 14 humanoid scenes pass none of M6's shading limits and seven fail its ink limits, on both drivers:
  the figures' many small forms on MSAA's edges, and the engine's ink drawing an edge that lies a little under what
  covers it where Freestyle hides it. **Answer: "Approve limits now, refine the lines after"** (chosen over "approve the
  limits, leave the lines" and "fix the lines first"): `ReferenceLimits.DetailedFigure` set from that day's measurement
  plus about a twentieth, as the detailed stages' were (S7-13), so the figures go into the repo; then an engine task makes
  the game's ink agree better with Blender's (a line hidden under another piece stays hidden), and the limits are
  measured again and tightened ("How it works", step 4a, "The detailed figures' limits"; Status).

Q56, Q57 and Q59, and the user's manner for every clip, came with the first humanoid's clips (step 4, the manager's; the
user's answers by the question tool and words to the manager, 2026-11-16 to 18; recorded by step 4b from the manager's
hand-over, `claude-work/gm2/step4/handover-clips/HANDOVER.md`; numbers fixed by the manager: Q58 is the ink fix's).
- **Q56 A ready stance per weapon kind** (2026-11-16: "A stance per weapon kind"); the six as approved on 2026-11-17 ("Yes,
  all six"): one-handed (sword, axe, mace, dagger; a shield or a second weapon in the left), two-handed (greatsword, great
  axe, spear), staff or wand, unarmed, bow, crossbow (the manager added the crossbow's: it can't be held as a bow). The
  one-handed stance is the clips' own arms; the other five are the stance loops `hold-*` (GM2-P21).
- **Q57 The sheet's, the maker's and the carousel's figure stands relaxed, at ease** (2026-11-16: "Relaxed, at ease"); the
  stand as approved on 2026-11-17 ("Yes, as shown"): the clip `stand`, which the figures' small state machine loops
  ("How it works", step 4b).
- **The user's manner for every clip** (2026-11-17, feedback to keep for every later clip of any rig): not stiff, not like a
  drill: ready to fight, the whole body moving, nothing held still. Said of the first stances ("The stances look very
  stiff. Make them seem more genuine, as if the character is ready to fight and not look like training"), the stand
  ("These are too stiff as well") and the attacks ("Make it also less stiff"); everything was rekeyed on a second stance
  (wide, low, turned side-on, leaning in, the rear heel up, the shield up, the weapon cocked) in that manner (the body
  loads, drives, goes through and settles; the trunk, the neck and the head follow the hips late; nothing is held still).
  The user's later words, each answered by a rework and approved after it (2026-11-17 and 18): the bow ("The hand movement
  of the bow is too weird", then "The bow string does not move"), the drink ("The drink animation does not put the drink to
  the mouth", then "During potion drink, the arm twists weird"), the skill ("the main hand does not move and gets
  twisted"), the evade ("Nothing much happens in the Evade, i want the character to fully jump back. The Evade hold seems
  very twitchy"), the guard ("During the guard hold, i want the shield to be fully in front of the character"), the
  victory ("In the victory pose, the arm twists weird").
- **The clips approved** (the question tool): 2026-11-17 the six stances, the relaxed stand and the seven melee attacks
  ("Yes, all seven"); 2026-11-18 moving and reacting on the second stance ("Yes, all eight"), guard, guard-hold, evade,
  evade-hold, victory and death ("Yes, all right now"), the bow ("Yes, the bow is right"), the drink with skill and
  victory ("Yes, the drink is right"); cast, item, meditate and crossbow shown in the third batch and not objected to;
  `strike` and `swing-off-hand` corrected by the manager after their approval (a wrist folded 135° for ten frames; an
  elbow buckled for a frame) and approved again ("Yes, both are right"); the woman playing the man's clips ("Yes, she
  moves right": one clip set for both). Then the ground (step 4b's measure, the manager's re-bake of 2026-11-18, "The ground
  and the in-betweens" in the hand-over): the ground is where the boots' soles are at rest; a foot is raised by what its
  boot would dip under that level past 2 mm (no clip more than 2.2 mm under it on any frame; the legs' angles moved by at
  most 2.5°, victory's push-off 10°: advance, critical-hurt, evade, flee, punch, swing, swing-two-handed, thrust, strike,
  victory); `meditate` a real kneel on the right knee (before, the knee hung 16 cm over the ground and the toe was 7 cm in
  it); `death`'s fallen body lying on the ground (heels, thighs, fist and head within a few mm of it, the back and seat 1.5
  cm over it under the mail; before, all of it 7 to 9 cm above; a key more at 54); every clip keyed at every frame written
  linear. The corrected `death` and `meditate`, shown on a floor, approved on 2026-11-18 ("Yes, both are right";
  `claude-work/gm2/step4/approval/clips-ground/`): the 34 files are final. **The playing view approved** (2026-11-19, the
  question tool, shown step 4b's twelve videos, `claude-work/gm2/step4b/playing/`): "Yes, it plays right". With it every
  approval stop of step 4 is passed.
- **Q59 When the clips are checked on the smallest and largest bodies** (2026-11-18; it changes Q10 (a), which said each clip
  batch is also played on the hobbit's slender and the Goliath's strong body): those bodies are made in step 9 and need the
  engine's scaling of the hips' place by the legs' length first (GM2-P22). Asked when to check the clips on them, the user
  chose **"In step 9, on the real bodies"** (not now on rough blockouts): when step 9 has made the two bodies and the engine
  scales the hips' place, the manager shows the user the clips on them and fixes what looks wrong then (an approval stop
  of step 9's, "Steps").

Q58 and Q60 raised by GM2's engine task on the ink ("How it works", "The ink's cover") and asked by the manager with the
question tool on 2026-11-18 (the numbers the manager's, so as not to collide with the steps' own).
- **Q58 The detailed figures' limits tightened** (2026-11-18, the question tool, asked by the manager with the ink task's
  measurement on the GPU and on lavapipe; Q55's "refine the lines after" done). The 14 scenes' worst, before the task →
  after: Freestyle's lines missed 2.50 → 0.09 %, extra lines 7.68 → 3.00 %, unlike ink 12.71 → 8.51 %, the mean coverage
  difference 0.134 → 0.103; the shading's stray pixels unchanged (23 inside, 88 on edges). **Answer: "Tighten as proposed
  (Recommended)":** `ReferenceLimits.DetailedFigure` is M6's with extra lines 3.3 % (was 8 %) and unlike ink 9.4 % (was
  13.5 %), each the worst plus about a tenth; missed lines and the coverage difference back to M6's 2 % and 0.12 (were
  2.7 % and 0.14); the stray pixels' one in 4,200 and one in 1,100 kept. `EachWidenedLimitBites` holds the two widened ink
  limits on their new worst scenes (extra just under 2.87–3.00 % on the woman's close-up, unlike under 8.50–8.51 % on her
  gear from the side) and the stray pixels' on the man's close-up as before. *Replaced by Q60 for the extra lines and the
  unlike ink.*
- **Q60 The detailed figures' limits on the ink task's final rule** (2026-11-18, the question tool, asked by the manager).
  After Q58 the task's mutation run found a flaw in its first rule (an edge's own faces' planes, carried a pixel off the
  edge, put a covering surface "behind": a rod's contour under a sheet was drawn); the final rule judges another piece's
  cover by its own plane within its normal's precision, and measured nearer Freestyle everywhere ("How it works", "The
  ink's cover"). Shown the worst of the 14 scenes and step 5's six assembled looks on both drivers: missed 0.09 %, extra
  1.82 % (Q58's numbers: 3.00 %), unlike 7.56 % (8.51 %), coverage 0.093, the stray pixels unchanged; and the rule put to
  the user: a limit stays widened only where a scene measures past M6's, at the worst plus about a tenth. **Answer:
  "Tighten as proposed":** `ReferenceLimits.DetailedFigure` is M6's with unlike ink 8.3 % (Q58: 9.4 %) and the stray
  pixels' one in 4,200 and one in 1,100; extra lines back to M6's 3 % (Q58: 3.3 %), missed lines and the coverage
  difference M6's as Q58 had them. `EachWidenedLimitBites`: the stray pixels on the man's close-up as before (4,500 and
  1,200), the unlike ink just under 7.56 % on the woman geared from the side (7.5 %); no extra row (not widened).

Q61–Q65 and the approvals of step 9a came with the shared sets and the human (the manager's art, asked with the question
tool, 2026-11-18 to 20; recorded by step 9a from the manager's hand-over, `claude-work/gm2/step9a/handover/HANDOVER.md`;
the sheets the user saw: `claude-work/gm2/step9a/approval/` `builds-faces/`, `sets/`, `colours/`, `presets/`).
- **Q61 (2026-11-18): the shared sets' lists**, each "Yes" as proposed: men's hair 20, women's hair 20, beards 12, head
  pieces 8, marks 6, each list with "None" (the lists with their names: "How it works", step 9a).
- **Q62 (2026-11-19): the woman's Side braid hangs before the right shoulder** ("Mirror it to the right": the battle and the
  sheet show the hero's right side).
- **Q63 (2026-11-20): the human's swatches**: body colours "Yes, all four lists" (skin 8, hair 10, eyes 8, lips 4), outfit
  colours "Yes, all of them" (cloth 10, trim 5, trousers 6, leather 3, buckle 2), ties and marks "Yes, both" (hair tie 6,
  marking 5).
- **Q64 (2026-11-20): every tied hair style carries a tie whose colour is chosen on its own** (the slot `hair.tie`; asked
  with Q63's ties: the other option was ties in the hair's colour).
- **Q65 (2026-11-20): a mark is made for each face** ("A model per face": a mark lies exactly on the skin; in the data a
  mark's model names the faces it fits; a mod that adds a face supplies marks for it, or that face offers none). Its form
  in the data: "How it works", step 9a ("Q65 in the data").
- **The approvals of step 9a:** the builds ("Yes, both builds are right") and the ten new faces ("Yes, all five" each sex),
  2026-11-18; the 40 hair styles ("Yes, all twenty" each sex) and the 12 beards ("Yes, all twelve"), 2026-11-19; the 8
  head pieces ("Yes, all eight") and the 6 marks ("Yes, all six"), 2026-11-19; the swatches, the ties and the eight
  presets ("Yes, all eight"), 2026-11-20. With them every approval stop of 9a is passed.
- **Q73 (2026-11-21, the question tool, asked by the manager on step 9a's report): a colour row with a single swatch is
  hidden** ("Hide the row"): a colour row is shown only when its slot has two or more swatches; the human's brows and eye
  whites disappear from the maker, and a race or a mod that gives them more swatches gets the row. The look still holds the
  swatch (step 6's check). Built by step 9a (`LookMaker.OfferedSlots`, "How it works", step 9a).

Step 10's, the nine emblems (the numbers the manager's): Q66 and Q67 and the medallions' approval came with the art
(the manager's hand-over, `claude-work/gm2/step10/handover/HANDOVER.md`); Q70 was raised by step 10's comparison with
Blender, Q71 and Q72 by its captures of the screens; each asked by the manager with the question tool and recorded by
step 10 ("How it works", step 10).
- **Q66 The nine motifs, chosen from two sketches each** (2026-11-20; Q37's "each proposed as a sketch first",
  `claude-work/gm2/step10/approval/emblem-sketches.png`): Human: the keep (a castle keep flying a banner); Elf: the great
  tree (an ancient oak, crown and roots; the other sketch was a stag's head, the manager's pick); Dark Elf: the black sun (an
  eclipsed sun ringed with rays; the other a spider in its web); Dwarf: the helm and beard; Hobbit: the pipe; Lizardman: the
  crested head (a lizardman's head in profile; the other a lizard from above, the manager's pick); Half-Beast: the claw
  marks; Goliath: the cairn (stacked stones with a rune); Merfolk: the shell (the other a tail fluke; the user was told the
  Merfolk walk on legs). None repeats an old seal's motif (Q37).
- **The nine medallions approved as renders** (2026-11-20, "Yes, all nine"; `claude-work/gm2/step10/approval/emblems.png`):
  step 10's second approval stop.
- **Q67 The live emblem rocks from side to side** (2026-11-20, "Rock from side to side"): it sways slowly about 35 degrees
  to each side and back and never shows its back; the back of the model stays plain. This changes Q36's "slowly turning"
  for the carousel's live emblem; its period is step 11's proposal (8 s).
- **Q70 The emblems' lines against Freestyle** (2026-11-21; raised by step 10: the engine's lines on the nine emblems
  passed neither the detailed figures' limits nor M6's: extra lines 5.5–13.0 % against 3 %, total ink up to 17.0 % over
  Freestyle's against 10 %, the coverage difference up to 0.140 against 0.12, every Freestyle line drawn; the engine draws a
  line along the top of every raised outline, seen nearly edge-on from 5° up, where its side rises from the field or the
  outline under it, where no mesh edge runs and Freestyle draws none; shown the Half-Beast's emblem both ways).
  **Answer: "Accept for now, fix the engine later":** the emblems go in as they are, held to limits of their own,
  `ReferenceLimits.Emblem`: the detailed figures' with extra lines 14.3 %, total ink 18.6 % and the coverage difference
  0.154 (each the worst measured plus about a tenth); an engine task later stops the engine drawing a line where one surface
  rises from another, then the limits are measured again and tightened (`Docs/Game-Plan.md`, "Engine items the port will
  meet"). (Not an engine task first; not the raised shapes joined into the field in the model, which changes the approved
  look: the manager tried an exact union of each raised shape with the body in Blender on 2026-11-21: two emblems came out
  the same to the eye and the elf's killed Blender, so it is not pursued.)
- **Q71 The carousel's emblems** (2026-11-21, shown step 10's captures, `claude-work/gm2/step10-repo/captures/`): **"Yes,
  as shown"**: each emblem's picture fills its medallion's inside where the ✦ stood, 94 px in the large one and 36 px in a
  medallion of the strip; the enemy-only races, which have no emblem, keep the ✦.
- **Q72 The sheet's seal in the dark theme** (2026-11-21, the same captures): **"Keep the sepia tone"**: the old
  stylesheet's filter on the seal's image in the dark theme (`game.css`, `html.dark-mode .hero-sigil img`, step 7's approved
  stylesheet) stays and tones the emblem; the light theme shows the emblem's own colours.

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
124 px, 99 px under 840 px, 72 px under 650 px, *measured by step 10: 113.75, 89.27 and 61.92 px wide, the button's
own padding taken off first*; `styles.css` 216–240, 2466, 2915, 2933; sepia-toned in dark mode, 3648);
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
  translations were kept at their distance on every body until GM2-P22's task (2026-11-20, "How it works", "The figure's
  ground"): since then a clip's hips' place follows the playing body's legs (the clips' measure, the left leg's thigh and
  shin; the woman's 0.968 of the man's), so the hips' bob and the lunge's steps are authored on the human average body
  and every other body, the smallest and largest included, plays them by that rule; the user sees them on the hobbit's
  slender and the Goliath's strong body in step 9 (Q59). A figure stands by its soles (its lowest point at rest), not by
  its model's origin.
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
small (Q36): the sheet's seal button (about 124, 99 or 72 px; *113.76, 89.28 and 61.92 px as laid out, step 10*), a companion's sheet (68 or 52 px), PNGs in `ui.pak` rendered
by Blender from the emblem's script at each size and at 1 and 1.65, regenerable (GP-4) and compared with the engine's
render; and live, slowly turning (*rocking about 35° to each side since Q67*), only for the race shown in the carousel (Q36, one live view, GM2-P16). Approved as
sketches, then as renders of all nine together (Q66; "How it works", step 10).

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
    noted for GM3-3, which is asked on that fact (answered 2026-11-20: each piece fitted to every body).

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
  clothes and armour (GM3-3 is asked on that fact; answered 2026-11-20: fitted to every body).
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
   - **Split (the manager, 2026-11-16):** the art, its approvals and the clips stay the manager's (in scratch scripts
     under `claude-work/gm2/step4/`); **4a**, the approved figures' scripts and models in the repo, done by a worker.
   - **4a done (2026-11-16/17, a worker, the branch `gm2-step4a`; "How it works", step 4a):** the manager's approved
     scripts ported into `Tools/Models/humanoid/` without changing the art (proven against his models within
     micrometres, with equal counts), split as GM2-P8 (the bodies with the rig and the plain outfit, faces and hair as
     garments on `head`, the chain shirt per body, the sword and the shield as items); the lighter figures (Q54) at two
     levels (GM2-P20) joined per ink flag (GM2-P19); the game's look `EMBER_HEARTH`; the clip frame with its test clip;
     `models.pak`; the figures against Blender under `ReferenceLimits.DetailedFigure` (Q55); the counts and the cost.
   - **4b done (2026-11-18, a worker, the branch `gm2-step4b`; "How it works", step 4b):** the 34 approved clips as data
     in `Tools/Models/humanoid/clips/` and their library `clips.glb` in `models.pak`, held to the hand-over's list and the
     keying's rules, to Blender on both bodies (on frames within 0.005 mm, between frames under Animation.md's rule once
     the clips keyed at every frame were linear) and as six posed pictures under `DetailedFigure` (two more owed,
     GM2-P24); the figures' small state machine (`models/humanoid/machines/figure`, `stand` looped, Q57); the figure
     mode's `--play` and `--label`; the playing view. With the user's Q56, Q57 and Q59 (the clips checked on the smallest
     and largest bodies in step 9), the manager's GM2-P21–GM2-P24, the ground (the soles' rest level, 24 mm under a body's
     origin; the clips re-keyed to it) and the clips' ease. Its choices: "How it works", step 4b ("Choices made").
   - **GM2-P22's engine task done (2026-11-20, a worker, the branch `gm2-figure-ground`; "How it works", "The figure's
     ground"):** the clips' hips' place scaled by the playing body's legs (the library's clips measured by the left leg),
     the man's poses and references unchanged to the bit, the woman's 35 evaluations made again by the rule (her lowest
     point now at most 2.43 mm under her ground, death's end 2.48 mm over it; before, 24.11 mm under), the ground test
     bounding both bodies; figures standing by their soles in the game's builder, the figure mode and Blender's renders.
5. **The game's look in the engine and figures built from looks** (engine and game; the PC for the Blender side). After 3's
   look and 4's skeleton.
   - Owns: `render.style` in the engine's `Render3D/` with its tests and the game's entry in `data.pak` (Q12, GM2-P11);
     the game's figure builder (a `look.hero` to an `Actor`, GM2-P8); `assemble.py` and the comparisons; the structural
     walk; the 3D mask in the game's screen fixtures (GM2-P12); the measurement of a view with no scenery (the shadow pass
     and its atlas; an engine task for the manager if they are spent there).
   - Must not touch: the engine's test scenes' style (Q11), the screens' markup.
   - Finished: an assembled look renders as Blender's (sample looks now, the presets as 9 lands them); the walk over every
     combination; the mask in place. No approval (the look was approved in 3).
   - **Done (2026-11-17/18, a worker, the branch `gm2-step5`; "How it works", step 5).** With the manager's answer of
     2026-11-18 (the human's swatches are 9a's: its presets draw their models' own colours until then). Its choices:
     `render.style` at `render/styles` under a look file's names, its rules the look file's, the game's entry
     `render/styles/game` written from `ember-hearth.json`; the builder's plan apart from its build, every missing thing a
     look names refused, a category it leaves out drawn as nothing, look parts all garments and the gear's items the rigid
     ones; the first humanoid's four parts and the writer's two bodies and eight presets in `data.pak`; `assemble.py`
     resolving looks on its own, six samples in the tests' colours against it under `DetailedFigure`; the walk by pairs;
     step 7's mask hook (on `main` first) naming every `surface` for every fixture. Measured: no shadow pass in a view with no
     scenery, its 64 MiB atlas allocated all the same (an engine task).
6. **The save v2 and the maker's records** (rules; anywhere). After 2.
   - Owns: `save.game` v2 (GM2-P5) with `Upgrade()` and fixtures, `look.hero`, `look.body`, `look.part`, `look.preset`
     with their checks (GM2-P6, GM2-P7; the swatches in `race`), the guard tests' change, the presets' data from the
     catalogue (as data; their parts arrive with 4 and 9).
   - Must not touch: the screens, art.
   - Finished: round trips, the v1 fixture upgrading, every check against a case. No approval.
   - **Done (2026-11-16, the branch `gm2-step6`; "How it works", step 6).** With the user's Q45 (a character's numbers
     out of range corrected silently, as the old `normalizeCharacter`, checked against it in the test client) and the
     manager's GM2-P18 (a look's models are references, refused when missing; nothing of the maker's enters `data.pak`
     before its models, so the presets' data waits in the oracle's catalogue with a one-step writer). Its choices: the
     look's colours by swatch name; "none" a part with no model; the categories (face, brows, hair, beard, crest, ears,
     tail, marks, head-piece) and step 4a's fifteen slots as `[Choices]`; a part offered by its races, sexes and the bodies
     its models are fitted to, one model for several bodies; the presets `<race>-<sex>-<n>`, ordered 10–80; the correction
     in the data library, applied by the session on opening, with a log line; the session given the game's data to check
     a loaded file; the race converter keeping the swatches.
7. **The sheet** (rules and screen; captures on the PC or under Xvfb). After 2 and 6.
   - Owns: `Screens/Sheet/` (the old page's markup; the paper doll with the dark panel, Q29, Q32, its view empty until 11;
     the name as the heading, Q30; the seal button's emblem picture, a placeholder until 10), `Assets/UI/styles/sheet.css`,
     the whole rail with its disabled tools and the drawer (Q34), `PageTests.cs`'s rail test, the sheet's fixtures.
   - Must not touch: the carousel, the maker, the views' code (11).
   - Approval stops: the sheet's captures (1×, both themes, the drawer under 840 px, Dev Mode on and off) in
     `claude-work/gm2/step7/approval/`; then the fixtures.
   - Finished: approved; the screen against Chromium of the port's markup (12 levels / 0.2 %, the view masked); every
     sheet action through the rules and saved.
   - **Done (2026-11-16/17, the branch `gm2-step7`; "How it works", step 7).** Approved by the user (Q46–Q49: the
     captures, the race and level under the name, the seal in full colour, the paper doll as shown), the seal left out of
     the picture comparisons (Q50) through the fixtures' new mask hook (by selector; step 5's for the views). Q26's rule
     was `SheetScreen.Ready` until step 8 made it one, `HeroFlow.Needs` (step 8 rebased onto step 7). Three engine
     differences its comparisons found fixed first on `gm2-engine-fixes` ("The engine fixes"). GM1's tests that expected
     the page after "New character" restated for Q26, and `end_to_end.py`'s Continue and load. Its choices are under "How
     it works", step 7 ("Choices made"). The view stays empty (11), the seal's emblem a placeholder (10).
8. **The race carousel and the maker's screens** (screen; captures on the PC or under Xvfb). After 1 and 6.
   - Owns: `Screens/Races/` (the carousel, Q28) and `Screens/Maker/` (presets as a list, the choices, the random button
     with no locks, Q21; GM2-P15's helper), their sheets, the flow from New character to the sheet (Q25, Q26), Dev Mode's
     powers (Q27), their fixtures; the views show the engine's stand-in figure and a placeholder emblem until 10 and 11.
   - Must not touch: the sheet, art.
   - Approval stops: the screens' captures (both themes; Dev Mode's carousel too) in `claude-work/gm2/step8/approval/`;
     then the fixtures.
   - Finished: approved; the screens against Chromium (views masked); the flow's rules tested.
   - **Done (2026-11-16 to 19, the branch `gm2-step8`; "How it works", step 8).** Approved by the user as captured (Q51, Q52), a
     playable race with no looks shown but not choosable (Q53). Its choices (the manager accepted them): both screens on
     the page itself, as the start screen; × to the start screen while the hero is unfinished, else back to the sheet;
     each screen hides itself before it runs the next one's command; the race and the look saved together only at the
     end; the preset kept as where the look started, a part the new body doesn't offer taking the category's first
     option; the random roll's fixed order (sex, build, each category, each colour; one draw a list); `open-races` and
     `open-maker` only for a hero who still needs the flow, or in Dev Mode. The views (the figures, the live emblem) are a
     seam for step 11 (`IHeroViews`), none in the program until then; the views' mask stays step 5's (the manager's).
   - **The engine fix it waits for is done** (2026-11-18, the branch `gm2-round-borders`; `Docs/Compositing-Port.md`, "Thin
     rounded borders"): the strip's approved 1 px round borders as Chromium paints them (Skia's hairline, the background
     shrunk under the border, the shown medallion's ring of shadow cut a pixel inside it). On the engine's page of the
     strip (`Ui/RoundBorderReferenceTests`, `rborder-strip`, its `races.css` rules as test content) the pixels past 12
     levels go from 2,715 to 11 (light at 1; the step's band had 2,746), 1,927 to 29 (dark at 1), 3,359 to 74 and 1,156 to
     65 (light and dark at 1.65). Step 8 rebases onto it, recaptures nothing (its goldens are Chromium's), and marks its
     engine item done.
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
   - **9a done (2026-11-20/21, a worker, the branch `gm2-step9a`; "How it works", step 9a):** the manager's approved art
     in the repo: the twelve script files (one word changed: the hair beads in the outfit's trim, GM2-P28), 152 models
     (the four builds, ten faces, 38 hair styles, 12 beards, 16 head pieces, 72 marks) in `models.pak`, the human's 69
     swatches, 82 parts with their approved names and places, six bodies and the eight presets in `data.pak`; Q65 as `look.part.model`
     version 2's `faces`, GM2-P29 in the maker; the presets held to the old looks by a table of names, the 18 samples of
     GM2-P30 against Blender. With the user's Q61–Q65 and the approvals, the manager's GM2-P25–GM2-P30, and on its report the
     manager's GM2-P35 (two close-ups owed to an engine task), GM2-P36 (`look.part`'s `order`) and the user's Q73 (a colour
     of one swatch shows no row). Its choices:
     "How it works", step 9a ("Choices made"). What a race of 9b–9e now needs: there, "For later steps".
   - **An approval stop added (2026-11-18, the user's Q59):** the clips checked on the smallest and largest bodies (the
     hobbit's slender, the Goliath's strong) happens here, on the real bodies, not on blockouts in step 4: once 9c and 9e
     have made those bodies and the engine scales the hips' place by the legs' length (GM2-P22), the manager shows the user
     the clips played on them and fixes what looks wrong then. **The engine's part done (2026-11-20, GM2-P22's task, the
     branch `gm2-figure-ground`; "How it works", "The figure's ground"):** every body plays the clips with their hips'
     place scaled by its left leg's length over the man's, and stands by its soles; a body built so needs nothing more
     (its skeleton's `thigh.L`, `shin.L`, `foot.L` are its measure). Made-up legs of 0.6 and 1.2 of the man's keep every
     leg joint at that ratio of his place to 0.3 µm; whether the clips *look* right on those proportions is the user's, here.
10. **The nine emblems** (3D, the manager; the PC). Its sketches any time; its models after 3's look.
    - Owns: the nine motifs' sketches (Q37), the emblems' scripts and models (medallions in relief, Q35), their pictures in
      `ui.pak` at each size and scale (Q36), their Blender references, the live emblem's scene for the carousel.
    - Approval stops: **the nine motifs as sketches, before any modelling** (`claude-work/gm2/step10/approval/`); the
      nine as renders together; then the tests.
    - Finished: approved; pictures and models against Blender; regenerable.
    - **The art done by the manager (2026-11-20):** two sketches a race, the user's Q66, the medallions modelled and
      approved as renders ("Yes, all nine"), Q67 (the live emblem rocks), GM2-P31 and GM2-P32; then **the repo's part by a
      worker (2026-11-20 to 22, the branch `gm2-step10`, rebased on step 9a; "How it works", step 10)**, with the user's
      Q70 (the emblems' own
      limits until an engine task), Q71 (the carousel's pictures as shown) and Q72 (the dark seal's sepia kept) and the
      manager's GM2-P34 (the pictures scaled down from the 640 px render). **Done (2026-11-22).** Its choices: the builder
      `emblems/medallion.py` (the manager's geometry unchanged: the nine models byte for byte his) with the outlines
      `chosen.json` beside it (one element a line, written and checked by `chosen.py`); the six slots `look.EMBLEM_SLOTS`; the
      rule in the game `Figures/Emblems.cs` and in the scripts `emblems/palette.py`, held alike by the tests; the pictures'
      scene a 640 × 640 render over black and over white (the cover from the two), made transparent, lined and scaled
      down by the game's tests from the committed renders (`make-emblem-pictures.sh` runs the chain); one picture per size
      a screen lays an emblem out at, 1 and 1.65 (ten sizes, measured), named by its size in device pixels and chosen per
      image by a resize observer on its device-pixel box; the medallion's and the strip's pictures absolutely placed in
      their insides; the harness's images as the program's host loads them; Chromium's captures given the pictures' files.
11. **The live figures and the live emblem** (game; anywhere for the tests). After 5, 7 and 8, with 9a's human; finished
    when 9e and 10 are in.
    - Owns: the views' code in the three screens (the hero's actor in the sheet's panel, idle in a fixed ¾ view, Q31;
      the carousel's and the maker's figures in the same fixed idle, Q41; the carousel's live emblem, Q36), pausing when covered or
      hidden, rebuilding on a change in the maker, at most the live views each screen shows (GM2-P16 as amended).
    - Must not touch: the screens' markup and sheets (7's and 8's), the models.
    - Approval stops: the three screens with their live views (videos); then the tests.
    - Finished: approved; each view against Blender's render of the same scene; the memory and the per-frame cost measured
      on the GPU and on lavapipe and reported.
    - **What GM2-P22's task gives it (2026-11-20, "How it works", "The figure's ground"):** a `HeroFigure`'s actor stands by
      its soles on the node it hangs on (`Actor.Ground`, set by the builder: drawn 23.997 mm up from the node for the
      human bodies), so its node goes where its soles should be; its clips, bound by `ClipBinding` as step 4b says, follow
      its body's legs with nothing to add. A view framed on the figure (`FigureFraming.CameraFor`) frames it as it stands:
      its rest points raised by `-Ground`, as the figure mode and `AssemblyTests` do.
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

### Step 6: the save v2 and the maker's records (2026-11-16, the branch `gm2-step6`)

**Decided on the way.** The user's Q45 (a character's numbers out of range corrected silently, as the old game) and the
manager's GM2-P18 (a look's models are references, refused when missing; bodies, parts and presets enter `data.pak` with
their models): "Decisions".

**The save, version 2** (`Game/Statventure.Game.Data/Saves/SaveGame.cs`, GM2-P5): `save.game` v2 holds `name` (1),
`hero` (2, `actor.sheet`, a new hero by default) and `look` (3, `look.hero`, null until the maker confirms one; a Dev Mode
hero of an enemy-only race has none, Q27). Version 1 is frozen as `SaveGame.V1` (its name only); its `Upgrade()` keeps the
name and gives the old game's new hero with no race and no look (HP 31, Mana 20, 5 gold, a class point), so a GM1
character opens on the new hero's flow (Q26). Its new check: a look is the hero's race's ("the look is a human's, but the
hero is a dwarf", or "… has no race", at `$.look.race`); the look's own checks run inside it, at `$.look…`. Its layout's
two versions are in `Data/Records/Layouts/save.game.txt`; fixtures: GM1's `ser-alrik` (version 1, both forms, untouched,
still GM1's character file byte for byte) and version 2's `ser-alrik-new-hero` (what version 1 upgrades to, equal to it)
and `ser-alrik-played` (a played hero with a look). A character file is the save's JSON form: GM2's carries the sheet and
the look (`{ "$types": { "save.game": 2, "actor.sheet": 1, …, "look.hero": 1 }, "name": …, "hero": { … }, "look": { … } }`;
no look: the key left out).

**The correction on opening (the user's Q45)** (`Actors/SheetCorrection.cs`, `SaveGame.Corrected()`): the old
`normalizeCharacter` (`app.js` 1399–1563) for the record's fields, in its order: the race's id through `slugify` (now
`JsValues.Slugify`, the one copy: the converter's moved there), the level into 1 to the cap (100; `limitless` gives 200,
GM15's), the EXP 0 at the cap (the old L18) else 0 or more and 0 when it isn't finite, the free points, class points,
points spent on each attribute, coins, HP and Mana 0 or more, a bonus that isn't finite 0; the attributes as they are.
**What the old function does beyond Q45's words, ported and named here:** the race's id slugified ("Dark Elf" is the
`dark-elf`; nothing kept is ""), the EXP dropped at the cap and held at 0 or more, the class points and the points spent
held at 0 or more, a bonus that isn't a finite number made 0. **What it does that the port doesn't do** (nothing a player
of the port meets): the old save's other keys (`stats` at the top, `modifiers`, `defenseModifiers`), the starting class
point of a save without `startingPerkPointGranted`, the Barbarian's old choice reward (GM8's), the fields of later
milestones. A value the record can't hold at all (a fraction in a whole-number field, text for a number) is the record's
form's refusal ("That file is not a valid character save"), as for any file the game can't read; a field a file leaves
out takes the record's default (HP 31, Mana 20, where the old game took the maxima of the file's attributes). HP and Mana
above their maxima are held by the sheet's rules when shown and after every action (step 2's `Settle`, as the old
`updateDerivedValues`), not here. **Where:** the session (`Session/GameSession.cs`) corrects a slot's save on `Open`, a
backup on `UseBackup` and a character file on `Import` before its checks; silently (no message, nothing refused), nothing
written until the next save, and one log line when anything changed (`[Session] <slot or file>: numbers out of range
corrected as the old game did`). **Checked against the old game:** the oracle module `save-correction` (the test client:
the old `normalizeCharacter` given each case in the old save's keys, its result read back; 102 cases: every edge by hand,
the race's slugs among them, and a seeded spread), `SheetCorrectionTests` case for case.

**The maker's records** (`Game/Statventure.Game.Data/Looks/`, GM2-P6, GM2-P7; each with `[Help]` on the kind and every
field):
- **`look.hero` v1** (`LookHero`, `[NotAPackKind]`, in the save and in each preset): `race` (`[References]` a race),
  `sex` (`[Choices]` male, female), `body` (a `look.body`), `parts` (a map from category to a `look.part`), `colours` (a map
  from palette slot to **the name of a swatch of the race's** for that slot: Q17 left no free colour, so P6's "a swatch or a
  colour" is the swatch), `preset` (a `look.preset`, for the maker's display, none: none). Two looks with the same choices
  are equal whatever their maps' order.
- **`look.body` v1** (`LookBody`, `[Kind("Look body", "data/looks/bodies")]`): `race`, `sex`, `build` (`[Choices]`
  slender, average, strong, Q15), `model` (a model, by its whole path under `models/`). One body per race, sex and build.
- **`look.part` v1** (`LookPart`, `[Kind("Look part", "data/looks/parts")]`): `category` (`[Choices]` face, brows, hair,
  beard, crest, ears, tail, marks, head-piece: the maker's categories as Q16, Q19 and Q22–Q24 name them, in the maker's
  order), `name` (shown, Q20), `races`, `sexes`, `models` (a list of `look.part.model`: a `model` and the `bodies` it is
  fitted to, **so several bodies can wear one model file**, a part fitted once per race and sex serving its three builds),
  `slots` (`[Choices]` the figure's palette slots its materials use). A part is **offered on a body** when the body's race
  and sex are among the part's and a model of it is fitted to that body; **a part with no model at all is an option drawn
  as nothing** ("None", "Bald", "Clean-shaven": Q23, Q24), offered on every body of its races and sexes. So a category has
  "none" where such a part is among its options and is a choice every look makes where it isn't (the Half-Beast's ears and
  tail, Q13): data, for steps 4 and 9 to make with their lists.
  **Since step 9a (2026-11-21):** **`look.part` v2** adds `order` (an int, field 7; the manager's GM2-P36): the part's place
  in its category's list, lower first, the same place by name, as a preset's in its race's (no check, as the presets' has
  none); version 1 frozen (`LookPart.V1`), upgrading to place 0, so version-1 parts keep their names' order. The game's:
  "None" 0, then 10, 20 … in the approved list's order. **`look.part.model` v2** adds `faces` (a list of `look.part`
  references, field 3; the user's Q65): the faces the model fits, none: any face; version 1 frozen (`LookPartModel.V1`,
  upgrading to no faces). A part is then offered on a body **with a face** when a model of it fits that body and that face
  (a model naming faces fits only them; with no face chosen only a model naming none); a look draws the first model that
  fits its body and its face. Their checks and words: "How it works", step 9a ("Q65 in the data").
- **`look.preset` v1** (`LookPreset`, `[Kind("Look preset", "data/looks/presets")]`): `label` (the old label, Q20),
  `order` (the race's list: lower first, the same number by name; the old looks the male 10–40 then the female 50–80,
  the old dialog's order, written as Q43 wrote the races'), `look` (every choice; its `preset` none). The game's are named
  `<race>-<sex>-<n>` (n from 1 in the old order).
- **The lists** (`LookNames`): each set of choices written once as one field's `[Choices]` and read from it (sexes, builds,
  categories, slots); the other fields with the same choices held to it by the tests. **The palette slots are step 4a's
  final fifteen** (`skin`, `hair`, `hair.tie`, `eyes`, `brows`, `lips`, `cloth`, `cloth.trim`, `trousers`, `crest`,
  `marking`, `claws`, `eye.white`, `leather`, `buckle`); the gear's (`metal.*`, `wood`, `grip`) are GM3's and not a look's,
  `pupil` and `glint` fixed. They are written twice for now, here and in `look.py`'s `SLOTS` (GM2-P7's "one list both
  sides read" isn't made: no test reads `look.py`).
- **The race's swatches** (step 1's `race.swatch`, the swatches staying in `race`, GM2-P7): the slot `[Choices]` the
  fifteen, and the race's new check: each swatch named, no slot with two of one name ("the race has another skin colour
  \"Fair\""). The converter from `races.json` now keeps each committed race's swatches (`RaceDataTests.Converted`), so the
  steps that fill them (9, with their approvals) write them into the races' JSON forms and run `make-data-pack.sh`.
- **`LookData`**: the bodies, parts and presets of the data by name (`Load`, the game's: an entry of another kind there is
  the data's error, as the races'; `Read`, the checks': such an entry passed over), `Offered(body)` (each category's options
  in the maker's order), `BodyOf(race, sex, build)`, `PresetsOf(race)` (the maker's list), and the checks' lookups.

**The checks** (`[Check]`s, the game's and the editor's alike, G4, G5; the words as the editor shows them):
- **A look's, with the data** (G4: what the maker couldn't make): a body of another race ("the body \"x\" is the dwarf's, not
  the human's") or sex; a part of another category ("\"x\" is a beard, not a hair"); a part not offered on the body: "isn't
  for the dwarf", "isn't for a female body", "has no model for the body \"human-male-strong\"" (the build); a category the
  body offers left out ("no beard chosen (the body \"x\" has 1 to choose from)", at `$.parts`); a colour for a slot the race
  has no swatches of ("the human has no cloth colours"), a swatch name the race hasn't, a slot the race has swatches of left
  out ("no skin colour chosen"). **Without the data** only its keys: a category that isn't one, a slot that isn't one.
  Whether the race, body, parts and preset are there at all is the references' check, so a name that points at nothing is
  reported once: "no look part \"hair-mohawk\" (data/looks/parts/hair-mohawk)".
- **A part's:** a name; one race and one sex at least, none twice, no slot twice; each model for one body at least, each
  body one model's ("the body \"x\" has a model already (models[0])"); with the data, each model's body of the part's races
  and sexes. **A body's:** one per race, sex and build in the data ("2 bodies are the human male average body (a, b): the
  maker takes one"). **A preset's:** a label; its look names no preset; its look checked as a hero's.
- **A missing model (GM2-P18):** the bodies' and parts' `model` fields are `[References(typeof(ModelData), Folder =
  "models", Rule = typeof(PathNames))]`, so a model that isn't in the data is "no model \"models/humanoid/bodies/x\"
  (models/humanoid/bodies/x)" at the field, in the editor and in the mod check (which checks the game's records under every
  mod: a shipped body without its model would leave out every mod), and a path that isn't one under `models/` is "\"x\"
  can't name a model in models". Nothing checks a path's form beyond that: 4a's names are examples.

**The session and the data** (`GameSession(storage, settings, data)`; the start gives it the game's data): a loaded
character file is corrected (Q45), then checked with the data and the references resolved over it (a lookup of the data's
record kinds, as the editor's and the mod check's), so a file's look the maker couldn't make is refused in the check's
words ("That file is not a valid character save: \"hair-braid\" has no model for the body \"human-male-strong\""). A slot's
save opens as it is (GM1's session checks a slot's save only when it changes, and `Change` checks without the data), so a
hero whose look names what the data no longer has (a mod switched off) still opens: what its figure shows then is the
figure's (step 5, 11).

**Where the presets wait, and the one step that moves them into `data.pak` (GM2-P18).** The 72 old looks are
`Tests/Statventure.Game.Tests/Reference/old-game/look-catalogue.json`: the oracle module `look-catalogue` (Node: the old
sprite scripts loaded with a window of their own, `Sprites.forActor`'s spec and `Sprites.lookLabel` for each race of the
catalogue, male then female, four each), every field the old rig drew (hair style and colour, beard, eyes, crest, ears,
tail, head piece, marks, skin, outfit), a test failing when `sprite-catalog.js` changes. A preset is made of a look by
`LookDataTests.Preset(look, body, kept)`: its name `<race>-<sex>-<n>`, label, order, race, sex and the race and sex's
average body, its parts and colours those of the committed preset (none before). **The step, for 4a, 9 or whoever lands
bodies' models:** run the game's tests with `STATVENTURE_LOOK_DATA=write` (`--filter LookDataTests` is enough): it writes
into `Assets/Data/data/looks/` (1) a `look.body` for each model `models/humanoid/bodies/<race>-<sex>-<build>` in
`Assets/Packs/models.pak` whose race is a race of `Assets/Data`, sex one of the two and build one of the three (4a's naming;
a body named otherwise is written by hand into `bodies/` and kept), and (2) a `look.preset` for each old look whose race and
sex have an average body, keeping the parts and colours of a preset file already there; then
`Tools/Scripts/make-data-pack.sh`. The parts (`data/looks/parts/<name>.json`, `look.part`) and the races' swatches are the
art steps' own records, written with their approved lists; once a race's parts are there its presets must name them (each
category its bodies offer) and its swatches (each slot): fill each preset's `parts` and `colours` in its file and run the
writer again (it keeps them). The guards: `TheCommittedLooksAreTheWritersOutput` (each committed body and preset is what
the writer makes of it: a preset's name, label, order, race, sex and body the catalogue's), `TheShippedLooksPassTheirChecks`
(every `data/looks` entry of the game's packs, mounted as the game mounts them, passes its checks with the references:
a missing model or an unchosen category fails here first), `AssetsDataHoldsTheRacesAndTheMakersRecords`. The editor's
screens over the repo's packs then show the new `data/looks` folder: their goldens are captured again as step 1's were
(12 fixtures; GM1-P19). Nothing of this is committed now: `models.pak` isn't on `main` (step 4a).

**The cost of the checks**, measured (a scratch test, Release, on the PC) at GM2's counts (54 bodies, 220 parts with a model
each for three bodies, 72 presets, nine races of 30 swatches, from a pack): checking all 346 records with the data and the
references takes about 190 ms, the presets' looks 135 ms of it (each reads every part to know what its body offers). Once
the data is in `data.pak` the mod check at start pays it once per start with mods on (GM1 step 6b's shortcuts check the
game's side once); a step that finds it too much at start (GM2 step 13's end-to-end, GM1-6b's measure) has this number.

**For later steps.** *Step 7 (the sheet):* the hero is `save.Hero`, its look `save.Look` (null for a hero without one);
`GameSession.Change(save => save with { Hero = … })` as step 2 said; a hero opened is already corrected. *Step 8 (the
carousel and the maker):* `LookData.Load(Application.Data)`: the race's presets `PresetsOf(race)` (each preset's
`Label`, its `Look` to copy, then `Look with { Preset = <its name> }`), the bodies `BodyOf(race, sex, build)`, the choices
`Offered(body name, body)` (categories in the maker's order, a part's shown name `LookPart.Name`, "none" being one of the
options where the data has one), the colours from the race's `Swatches` (a choice for every slot the race has swatches of,
**set even when the chosen parts don't show that slot**: the check asks for every one), the random roll uniform over each
offered list and each slot's swatches; "Begin as <race>": `Change(save => save with { Hero = <ActorRules.ChooseRace …>,
Look = <the look> })`, the look's race the hero's (the save's check); a look the maker makes passes every check by
construction, and `RecordChecks.Check(look, data)` says why one doesn't. *Step 5 (figures from looks):* a look's body
`LookData.Body(data, look.Body)` → its `Model`; each part `look.Parts[category]` → `LookPart.Models`, the entry whose
`Bodies` holds the look's body → its `Model` (none: the part draws nothing); the palette from `look.Colours` (slot → the
race's swatch of that name → its `Colour`) and the parts' `Slots`; a look naming something the data hasn't (a mod gone)
reaches the figure only from a slot's save (opened unchecked). *Steps 4 and 9:* the one step above; part names are theirs
(4a's `swept-back`, `long-tied`, `human-male-1` are fine names), and a hair style fitted per race and sex to its three
builds is one `look.part.model` with three bodies.

**Choices made** (internal; recorded here): the look's colours by swatch name; "none" as a part with no model rather than a
missing key; categories and slots as `[Choices]` (a mod can't add a category or slot; a new one is a change of the list);
a part offered by its races, sexes and fitted bodies (no `builds` field: the models say it); the presets' order 10–80 in
Q43's way and their names `<race>-<sex>-<n>`; the save's correction in the data library and applied by the session (Open,
UseBackup, Import before its checks) with a log line; `Slugify` moved into `JsValues`; the session given the game's data
for loading files (an optional third argument; the tests' sessions without data check what needs none); the look checks
reading the data afresh each time (no cache: the mod check's shortcuts follow every read); the converter keeping the
swatches; the writer in the game's tests as step 1's converter, writing on request only (the move into `data.pak` is a
deliberate step, not forced by a test when `models.pak` gains bodies).

**Tests** (game; the counts in "Runs"): `SheetCorrectionTests` (every oracle case; each rule alone; Limitless's cap;
the save's hero only), `SaveGameTests` (version 2's fields and help, version 1's upgrade from both forms, the look's race,
both forms with a hero and a look), `LibraryRecordLayoutTests` (the guard over the five new types; the save's two layouts;
version 1's fixture upgrading to version 2's), `LookRecordTests` (the kinds, folders, marks and references; one list of
each choice; the test data passing; every refusal of a look, a part, a body, a preset and the race's swatches; what a body
is offered; the checks without the data; equality; the fixtures `look.body/human-male-average`, `look.part/hair-braid`,
`look.preset/human-male-1`), `LookDataTests` (the catalogue's 72; a preset of its look; the writer on made-up models; the
committed data the writer's; the shipped looks passing their checks; `Assets/Data`'s folders), `GameSessionTests` (Q45 on
opening, a backup and a file, with its log line and nothing written early; a version-1 save and file opening as a new
hero; a file's look checked with the data, the race corrected before it; a newer version now 3), `StartSequenceTests` (the
start's session checking a file against the game's data); `RaceDataTests` with the swatches kept. The test data
(`Looks/TestLooks.cs`): two races with swatches, stand-in `model` records at the paths the bodies and parts name, four
bodies, five parts (a face for two bodies, a braid on two, three with no model), a preset. The engine's project:
`GameTesting.LibraryKinds` 38.

**Shown able to fail** (`mutate.py` with the game's project, under the shared lock in a headless cage;
`claude-work/gm2/step6/mutations.json`, made by `make-mutations.py`): 114 mutations of every rule the step adds: each
correction of Q45 and the cap's Limitless, `slugify`'s steps, the save's upgrade, correction and look-race check, the
session's corrections (opening, backup, file, before its checks), its data and references for a file and the start giving
them, its log line, every check of the look, the part, the body, the preset and the swatches, the look's equality, every
`[References]` and `[Choices]` mark (a missing model among them), `LookData`'s offers, bodies, presets' order and its strict
and lenient reading. The first run: 108 killed, 6 survived; one a gap, closed (two looks whose colours one holds and the
other holds with one more) and killed in the rerun (`mutations-rerun.json`). **The standing list: 114, 109 killed, 5 that
can't differ:** `slugify` without its trim, or with .NET's (white space at either end becomes one "-", which slugify takes
off anyway; the two trims differ only in U+0085, a dash either way); `slugify` keeping capitals (after JavaScript's lower
case none is left); the data's kinds without the "is a record" guard (a file there makes the load throw, caught as none:
the guard only saves the load); the presets' list without its tie by name (the presets are held in name order and the sort
is stable). **The two oracle modules' rules** (`module-rules.py`, a runner of its own as step 2's `script-rules.py`, under
the lock and alone: a module broken, run again by `make-old-game-references.sh`, then the port's tests that read its
results; killed when the script fails or a port test does): 18, the old function fed or read back wrongly (the starting
class point, the race, the EXP, the free points, the attributes, the allocations, the defence bonuses, Mana; the level,
EXP, class points, bonuses, HP and purse read back) and the catalogue's labels, sexes, count and specs: **18 killed**. Each
module run twice gives the same bytes. Nothing written to the player's real folders.

**Runs** (headless, scratch per-user folders, nothing audible; on `main`'s `1253451`, which hadn't moved): the full suite
on the GPU (263 s with the build) and on lavapipe (308 s): **4266 + 659 = 4925 tests** (`main`: 4266 + 630 = 4896; 29 new,
all the game's), none held or skipped; `make-old-game-references.sh save-correction look-catalogue` run again on the final
tree: not a byte differs. No pack, golden or editor fixture changed (nothing enters `data.pak`, GM2-P18).

### Step 4a: the first humanoid's scripts and models in the repo (2026-11-16/17, the branch `gm2-step4a`)

The manager's approved scratch scripts (`claude-work/gm2/step4/`) ported into `Tools/Models/humanoid/` without changing the
art, the models built from them into `Assets/Models/humanoid/` and `models.pak`, held against the manager's models and
against Blender. On the PC only for what runs Blender (the pinned 5.2.0, `-t 8`); the tests run anywhere.

**The scripts** (`Tools/Models/humanoid/`; each module's header says what it holds):
- `kit.py` (lofts, tubes, quad spheres, modifiers, the lit and unlit materials; `join` and `join_by_ink`, GM2-P19),
  `materials.py` (every material by its slot), `forms.py` (the body's and the head's forms fused by voxels, smoothed and
  given their masses: the sculpt; the head's flat pieces laid on by rays; the hair as an open cap and sculpted locks),
  `lighten.py` (the sculpt remade lighter: `light_body` remeshes the whole body and each hand apart and stitches them,
  `light_head` remeshes the head finer over the face; every new vertex put back onto the sculpt and the remesh tried again
  with more faces while the result strays too far), `levels.py` (GM2-P20's two levels' numbers), `rig.py` (the skeleton:
  23 deforming bones and 7 sockets, the roll rule; `bind`, `bind_rigid`, `bind_like`), `outfit.py` (the `Maker` that cuts
  garments from the dressed body: `cut`, `shell`, `paint`, `outline`, `skirt`, `buckle`; the tunic), `gear.py` (the sword,
  the shield and the chain shirt; the battle's lighter sword and shield), `bodies/` (`params(name, level)`, `build`,
  `body_model`; `human.py`'s `male_average` and `female_average`, candidate A as the user chose it), `parts/face.py` and
  `parts/hair.py` (the two faces and the two hair styles approved), `clips/library.py` (the clip frame, below), `look.py`
  with `EMBER_HEARTH` (the game's look as the user chose it; `looks/ember-hearth.json`) and the slots (`SLOTS` gains
  `eye.white`, `leather`, `buckle`; `GEAR_SLOTS` `metal.base`, `metal.trim`, `metal.shade`, `wood`, `grip`; `FIXED` the
  pupils `#060404` and the glints `#efe6d0`, unlit, their own colour in every look).
- **`manifest.json`** names 18 models, each with its builder, its arguments, its **level** (`"full"` or `"battle"`, passed
  as `level=`) and its file: the 10 game models in `Assets/Models/humanoid/` at the full level (`bodies/human-<sex>-average`,
  `parts/face/human-<sex>-1/human-<sex>-average`, `parts/hair/swept-back/human-male-average`,
  `parts/hair/long-tied/human-female-average`, `gear/chain-shirt/human-<sex>-average`, `gear/sword`, `gear/shield`), and as
  the tests' data, never packed: the frame's `sample`, `test-clips` (below) and `battle/…`, the geared man's six models at
  the battle level in `Tests/Statventure.Tests/Data/Models/humanoid/battle/`.
- **The split (GM2-P8):** a body's model is its skeleton, its surface and its plain outfit, joined; a face and a hair
  style are made on a body and bound wholly to `head`, exported with that body's skeleton (garments); the chain shirt is
  made on each body over its outfit and exported alone with the skeleton (a garment, bound by bone names); the sword and
  the shield are rigid items in their sockets' axes, the grip at the origin.
- **One model, one primitive per material and ink flag** (GM2-P19): each builder joins its pieces with
  `kit.join_by_ink` before the export, the inked ones as one object (named after the model: `<body>`, `<body>-<face>`,
  `<body>-<style>`, `<body>-chain-shirt`, `sword`, `shield`) and those marked `ink = 0` (the mail's rows of rings, the
  sword's fuller) as `<name>-unlined`.
- **The same bytes every build.** `make-humanoid.sh check` builds every model again into scratch and compares byte for
  byte (`build.py --check`): 18 of 18 equal, twice. Two things made that hold: Blender runs with 8 threads
  (`make-humanoid.sh` sets `BLENDER_THREADS=8`, `blender.sh` passes `-t`; QuadriFlow and the order of the edges Blender
  makes, which the weights' smoothing sums over, follow the thread count: at `-t 4` and `-t 1` the body differs), and the
  element lists given to a cut are in the mesh's own order (`lighten.geometry`: the indices brought up to date, then
  sorted; a Python set's order follows the elements' addresses, which differ run to run: the manager's hand-over scripts
  gave the wrists a different triangulation on every build, 33–92 triangles of the skin; shown by building them twice,
  and twice the same with the order fixed).
- `evaluate_animations.py` builds a model named `humanoid/<name>` through the manifest's builder (the clip scenes);
  `make-blender-references.sh` renders `Reference/humanoid-scenes.json` (with `--out`, its model folder rewritten to the
  committed `Assets/Models`).

**The lighter figures (Q54, GM2-P20) and the two addenda.** The sculpt is an intermediate inside the build; each model
is built at its level: `levels.LEVELS["full"]` (the body 6,500 quads and each hand 1,000, the head 4,500 with the face
enlarged ×1.9 for the remesh, the hair's cap cut from a quad sphere of 14 and its locks of 10 sides subdivided once, a
skirt at most 72 faces around and 9 rings, the mail's rows every 22 mm) and `"battle"` (1,900 and 150, the body within
25 mm of the sculpt, 900 at ×1.6 with the face within 12 mm, 8, 6 sides unsubdivided, 36 and 5, 44 mm; the sword without
its fuller and binding, the shield's face in two rings, its boss and rivets cut coarser). Then, from the manager on
2026-11-16 after the hand-over: **(1)** `rig.bind_like` gives a garment cut from the body the body's weights at the
nearest point of its surface, mixed from that face's corners (the four strongest, summing to one), not the nearest
vertex's; and `Maker.skirt_weights` binds every skirt by the body's own heights (`Maker.skirt_band`: from the belt's line
to the middle of the thigh), not its own: on the lighter meshes the tunic showed through the mail in a twist or a lean.
**(2)** `forms.open_cap`: the cap's edge stands 1.2 % outside the skin on the hairline and one more ring of faces turns
straight in to an open edge at 0.95 of the skull's radius (so no open edge lies just under the skin, where the engine's
ink drew it); `Maker.skirt`: every skirt's rings at the body's own heights, on one grid between the skirt's own top and
hem (two skirts worn over each other fold at the same heights), the hem's band from the first ring at or above its
height; `gear.chain_shirt`: the mail's skirt flared 32 mm (was 16; the tunic's skirt with its pleats reaches 38 mm off the
body at the mail's hem).

**The proof.** The repo's models against the manager's at each stage, assembled as the game assembles a figure and posed
as his were (`claude-work/gm2/step4a/proof/`: his scripts run again, the scenes rendered both ways, every vertex against
the nearest of the other's, the pictures compared): the first models within 2.4 µm; the lighter ones within 2.5 µm of
the hand-over's files (whose own scripts give other bytes on every run, as above); after the addenda within 1.1 µm at
rest and 18 µm posed (the weights mixed at sub-µm different points), the counts equal his (the man geared 57,546
triangles in 23 primitives, the woman 59,986 in 24, the battle man 16,686 in 22); the pictures differ in 0.04–0.7 % of
pixels, all in the posed folds' shading (his frozen normals against the skinned ones the engine recomputes) and a few ink
fragments.

**The models** (vertices / triangles / primitives / bytes; the full level, the battle level beside the man's):

| Model | Full | Battle |
|---|---|---|
| `bodies/human-male-average` | 12,066 / 23,716 / 6 / 689,420 | 3,971 / 7,692 / 6 / 237,048 |
| `bodies/human-female-average` | 11,764 / 23,148 / 6 / 672,516 | |
| `parts/face/human-male-1/…` | 4,367 / 8,554 / 6 / 259,576 | 1,030 / 1,880 / 6 / 72,672 |
| `parts/face/human-female-1/…` | 5,009 / 9,808 / 7 / 296,180 | |
| `parts/hair/swept-back/…` | 3,588 / 7,038 / 1 / 211,032 | 736 / 1,366 / 1 / 51,496 |
| `parts/hair/long-tied/…` | 4,586 / 9,014 / 1 / 266,596 | |
| `gear/chain-shirt/human-male-average` | 12,680 / 12,024 / 3 / 643,268 | 4,118 / 4,158 / 3 / 219,328 |
| `gear/chain-shirt/human-female-average` | 12,248 / 11,802 / 3 / 622,724 | |
| `gear/sword` | 1,303 / 1,734 / 4 / 45,136 | 315 / 358 / 3 / 12,268 |
| `gear/shield` | 2,254 / 4,480 / 3 / 83,548 | 630 / 1,232 / 3 / 25,080 |

`Assets/Models/humanoid/` 3,789,996 bytes (the first figures: 9,295,040); `models.pak` 1,740,366 (4,187,845), 0.46 of
the files. At the design's counts the 54 bodies come to about 37 MB of `.glb` (17 MB packed), with about 108 faces (324
if each is fitted to the three builds) and 320 fitted hair styles 143–203 MB (66–93 MB packed; the first figures'
estimate 430–550 and 240–300), chain shirts for every body 34 MB more.

**The cost** (`HumanoidCostTests`, a measurement without a bound, the manager's answer; and a scratch probe for the
study, 1080p, Release, medians of seven runs; the lighter figures as of the first addendum, which the second moved by
under 0.3 % of a figure's triangles): sixteen geared figures (eight men, eight women) **1.99 ms on the GPU** (the first
figures 3.03 ms, of which the main pass 1.06 and the feature edges 0.83, the frame alone 0.45–0.60), sixteen geared men
at the full level 1.94, at the battle level 1.25; with the stage in view (scenery, so the shadow pass draws every figure
into the sun's 16 maps) 5.60 and 2.59 (the stage alone 0.89; the first figures about 11.8); one figure at 600 × 800 0.24
ms; on lavapipe 330, 283 and 124 ms, with the stage 1,065 and 361 (the first figures 1,079 without it). The CPU about 21
µs a figure a frame at either level, about 0.35 ms for sixteen, nearly all of it building the draw items (each
primitive's posed bounds, its joint matrices and tones): it follows the primitives, not the triangles. The study's fit
over the first figures: 0.73 ms + 16 × (0.06–0.10 µs a primitive + 0.89 ns a triangle).

**The clip frame** (`clips/library.py`; its header gives the file's form and the keying): a clip is a JSON file (`name`,
`frames` at 60 fps, `loop`, `markers`, `keys` of XYZ Euler degrees and metres in each bone's own axes, `ease` bezier or
linear), checked before anything is keyed (the file's own keys only, the bones the skeleton's, frames rising, a loop's
last key repeating its first); keyed as the manager's previews key them (each rotation the quaternion of its Euler
angles with the sign nearer the bone's previous key, data frame n at Blender frame n + 1, the action's range the clip's
length, its markers the animation's events) on the human man's skeleton and exported as the engine's animated models
are. `make-humanoid.sh clips <folder> <library.glb>` makes a library of any folder (to try clips). The frame's test clip
`rest-sway` (120 frames, a loop, a slow sway and a breath, `inhale` at 0 and `exhale` at 60, the hips' location among its
keys) is the tests' data (`Tests/Statventure.Tests/Data/Models/humanoid/test-clips/`, built into `test-clips.glb` beside
it), never in a pack: the family's `clips.glb` comes with its first real clips (`HumanoidPackTests` has the line to flip).
A location is applied in metres as it is, from each body's own rest, in its hips' axes (3 mm comes out as 2.991 on the man
and 2.989 on the woman): a body of shorter legs sinks and a longer one floats by the clip's own amount, nothing scaled.
Three animation scenes (`humanoid-rest-sway`, on the man and on the woman) evaluated by Blender hold the engine's
sampling to it.

**`models.pak`** (`Tools/Scripts/make-models-pack.sh`): `Assets/Models` copied to a scratch `models/` folder and packed
afresh by the packer (its model importer: each `.glb` the record at its path without the ending), the same bytes every
run; listed in both games' listings (`file packs/models.pak`) with its licence cover. The game mounts it as its other
packs.

**The detailed figures' limits (Q55; `ReferenceLimits.DetailedFigure`).** The 14 scenes (`HumanoidReferenceTests`: each
body from the front, three-quarter, side and back at 360 × 480, a 320 × 320 close-up of the face, and geared at
three-quarter and from the side posed by `rest-sway`) measured on the PC's GPU and on lavapipe on 2026-11-16 with the
final models; each measure past M6's set to the worst plus about a twentieth, rounded as S7-13's were; the rest M6's.
Two new fields: the share of the picture that may be inside pixels off by more than 2 levels (`InsideEvery`) or edge
pixels beyond the edge rule (`EdgeEvery`), apart (M6: none); `Render3DReferenceTests.Compare(expected, actual, limits)`
reads them.

| Measure | Measured (both drivers) | Worst | Limit (M6's) |
|---|---|---|---|
| Inside pixels off by more than 2 levels | 1–23 a scene | 23 of 102,400 (the man's close-up), one in 4,452 | one in 4,200 (none) |
| Edge pixels beyond the edge rule | 12–88 | 88 of 102,400 (the man's close-up, lavapipe), one in 1,164 | one in 1,100 (none) |
| The edges' mean | 2.06–2.81 levels | | 4 (4) |
| Freestyle's line pixels missed | 0.13–2.50 % | the woman's close-up | 2.7 % (2 %) |
| Extra line pixels | 0.46–7.68 % | the man's close-up, lavapipe | 8 % (3 %) |
| Total ink off Freestyle's | 0.70–4.94 % | | 10 % (10 %) |
| Mean coverage difference | 0.075–0.134 | the woman's close-up | 0.14 (0.12) |
| Unlike ink about a line | 1.39–12.71 % | the woman geared from the side, lavapipe | 13.5 % (3 %) |

What parts the engine from Blender here: MSAA's edges on the figures' many small forms, and the engine's ink drawing a
feature edge that lies under what covers it, where Freestyle hides it (`ink_edges.frag`: an edge point is drawn when it
lies no more than half a pixel, two when the covering surface is seen nearly edge-on, plus 1e-4 of the depth behind that
surface's plane: about 1.5 mm in the close-ups and 2.8 mm in the full views, 4 to 9 mm edge-on). What shows because of
it, in the pictures the manager judged (`claude-work/gm2/step4a/look/`): the hairline's crease, the jaw's underside
against the neck, lines along the hair's locks, the mail sleeve's seam at the shoulder, more lines on the sword and the
shield's rim seen edge-on. An engine task makes the ink agree better (Status), then the limits are measured again and
tightened.

*Corrected by the engine task (2026-11-18, "The ink's cover" below):* the allowance was not what drew most of those lines.
With the feature edges switched off, the hairline, the jaw's underside and the lines along the locks were still drawn:
they were the per-pixel crease rule within a part, where two pieces met at a steep angle without a break in depth (the
hair's cap and the forehead, the jaw and the neck, a lock on the cap). A model joined per ink flag (GM2-P19) is one part,
and so are the face, the hair and the chain shirt over the body, garments exported with its skeleton, whose nodes share
the body's numbers. The allowance itself drew the doubled lines of the gear (the shield's rim over its field, the
sword's pieces). The limits were tightened by the user's Q58 and Q60 (2026-11-18): unlike ink 8.3 %; extra lines, missed
lines and the coverage difference M6's.

**Tests** (engine; the counts in "Runs"): `HumanoidModelTests` (the manifest names every committed model; every skinned
model carries the family's skeleton, the bones' parents as `rig.py` has them, at its body's rest; every vertex follows
at most four deforming bones, never a socket, its weights summing to one; the face and the hair follow the head alone;
every material a slot of the game's look or a fixed colour, the eyes unlit, the drawn lines unhatched; the strips of
drawn lines the model's one piece without ink; one primitive per material and ink flag; the battle level the full figure
lighter: the same skeleton at the same rest, the same materials but the fuller's, at most the triangles and primitives
measured plus a tenth; **the outfit stays under the mail in a deep lean with a twist, a lunge and the arms raised**: an
outfit vertex facing out and sideways is covered where the line along its normal meets the mail or the belt within 40
mm at rest, and posed it shows through where that line meets nothing within 50 mm and the mail lies behind it: none on
the full man, the full woman and the battle man (the models before the first addendum: 17 and 14); the items rigid with
the grip at the origin, the sword's blade 0.88 m along the socket's Y, the shield off the arm; the clip library holding
every clip of its folder, `rest-sway` as its data says, a loop), `HumanoidReferenceTests` (the 14 scenes' file what the
figure mode writes, `STATVENTURE_HUMANOID_SCENES=write`; each scene's shading and ink against Blender's under
`DetailedFigure`; the limit set pinned; each widened limit failing its worst scene when set just under what it measured),
`HumanoidPackTests` (the pack exactly what the packer makes of `Assets/Models`, one record a model, no clips and nothing of
the battle; the game finds every model in it), `HumanoidCostTests` (the counts and sizes of both levels, sixteen geared
figures' animation work, the GPU on request: `STATVENTURE_MEASURE=1`), `EmberHearthLookTests` (the game's look the one
chosen), and the three animation scenes.

**Shown able to fail:** the step adds no engine or game code; its rules are the scripts', the models' and the limits'. **The C# side**
(`mutate.py` under the shared lock in a headless cage; `claude-work/gm2/step4a/mutations.json`): 22 mutations, **22
killed**: the comparison's stray pixels (inside and on edges, loosened, not dropped, their shares read from each other's
field, the edges' mean not passed), each widened limit put back to M6's and each set just under what its worst scene
measured, the humanoid scenes held to M6's, `list-marks` back to 700 px. (M6's own `InsideEvery` and `EdgeEvery` can't
show: no M6 comparison reads them.) **The scripts** (each rule broken in a copy of the scripts and the committed models,
then `make-humanoid.sh check` of the models it touches, which must differ: `claude-work/gm2/step4a/pybreaks/models.py`):
26, **26 killed** (the joining per ink flag and its names; the battle's sword and shield; a builder, the manifest and
`build.py` losing the level; a body's parameters, the locks' fineness; the hair cap's edge outside the skin, its inner edge
and its turn; the head's enlarged face; the hands' faces; `bind_like`'s four bones, their sum and the surface's mix; the
skirt band, the skirts' rings on the body's grid, the mail's flare, its rows' spacing and joining, the collar's
relaxing, a cut's reach); with step 4a's earlier breaks of `look.py` and the clip library, 43 of 43. **The models**: the
per-ink-flag test fails on the first, unjoined models (`bodies/human-male-average`: leather 5 times, cloth 2, cloth.trim
5), the lean test on the models before the first addendum (17 vertices through on the man, 14 on the woman). Nothing
written to the player's real folders.

**Runs** (headless, scratch per-user folders, nothing audible; on `main`'s `3c22527`): the full suite on the GPU and on
lavapipe, 4364 + 659 = 5023 tests (`main`: 4266 + 659 = 4925; 98 new, all the engine's), none held; `make-humanoid.sh check` 18 of 18
the same bytes, twice; `models.pak` the same bytes twice; `publish.sh` and `check-packages.sh` ok on the four archives of
`2026.11.17.1` (the game's Linux archive 44.6 MB, 56 files; Windows 40.8 MB, 49), the self-test 119 breaks, 119 caught;
`end-to-end.sh` from that publish: 241 of 241 on the GPU (118 s) and on lavapipe (222 s). Editor goldens: the game's packs
now list `models.pak`, so 18 captured again (9 fixtures at both scales; `list-marks` made 760 px tall, as step 1 made it
700, so its tree still fits without scrolling) and six editor tests' expectations given the new pack and folder.

**Choices made (internal, recorded here):** the manifest's level as its own field, given to the builder as `level=`; the
battle models named `battle/` and the full model's name; the joined objects named after their model; `levels.py` holding
only what differs between the levels; the cut lists in the mesh's order after an index update; the test of a model's
draws over the game's and the battle's models alike; the lean test's pose and rule (a ray along the normal, the belt
counted as cover, rims and inner sides left out, 50 mm); the stray pixels' shares as "one in N" like `StrayEvery`; the
bite test setting each widened limit just under the measure on both drivers.

### The engine fixes: three of Chromium's rules (2026-11-17, the branch `gm2-engine-fixes`)

An engine task inside GM2, on its own branch so that steps 7 and 8 rebase onto it (the manager's, 2026-11-16). **Step 7
found the three** comparing the sheet's screen with Chromium (its worker measured candidate fixes and put them back): the
word PLATINUM (a currency's name in the sidebar: `.currency-name`'s `overflow-wrap: anywhere` under
`.sidebar-currency .currency-name`'s `white-space: nowrap`) drawn broken as PLATINU/M in a sheet up to
1350 px wide where Chromium keeps it whole and overflowing; the numbers in the sheet's stat fields (`appearance:
textfield`) 7.5 px (centred) or 15 px (at the end) off, the engine having given such a field no spin button; the 40 px
heading at `line-height: 1.05` 41.984375 px high where Chromium's is 42 (the number kept as a float). Step 8's carousel
and maker meet the same. Each rule is ported from Blink 152 with its neighbours (`Docs/Compositing-Port.md`, "Three text
rules", has the sources): words break inside only on a line that can wrap (`LineBreaker::SetCurrentStyleForce`), and
there `overflow-wrap: break-word` (not for the min-content size), `word-break` as a property of its own (break-word,
break-all with its class table, keep-all); a number field's spin button made whatever the field's appearance, its room its
margin box (the theme's 15 px, the author's paddings, borders and margins; none under its own appearance: none or display:
none); a number line-height kept as Blink's percentage from the parsed double, and every reader (line boxes, controls'
lines, a text field's inner editor rule, the lh unit, the text style). The fixtures: `Ui/TextRulesReferenceTests`, three
Chromium pages at six scales and zooms. **What moved in the suite** (the full suite with the rules and without, every
comparison's numbers logged; Compositing-Port's table): only the old game's screens (`game-*`), all nearer to Chromium,
through the sheet's defence bonus fields (`appearance: textfield`, now with the spin button's room: `game-sheet` 2,375 →
2,263 pixels past 12 levels at 1×, 5,120 → 4,874 at 1.65; the screens with the sheet under a dimmed overlay a few levels);
no layout, no editor screen (step 4a's 18 goldens among them) and none of the port's game screens moved. **Found beside,
left as they are** (the manager's word, 2026-11-17): five differences, in Compositing-Port's "Left as they were" (a
min-content word after an item's first 1/64 px wide, the kerning kept across a break inside a word, the focus ring at
1.65, percentage line-heights not cut to whole percents; a number field's own width from its min and max, an engine
task before the first screen with such a field: the combat quantity, the companions' resource fields).
- **What steps 7 and 8 do at their rebase:** take the branch as it is; recapture nothing of theirs for these rules (their
  goldens are Chromium's); run their screen fixtures again: the PLATINUM rows, the stat fields and the 1.05 headings
  should now match without their workarounds; drop any of their own changes to the engine's `InlineLayout`,
  `UiPainter.Controls`, `ComputedStyle`, `Properties` or `ReplacedContent` for these rules; then
  `Reference/game-screen-fixtures.json` is made again by the game's tests if their fixtures changed. Nothing of theirs
  was run here (their branches aren't merged): if a fixture of theirs still fails after the rebase, the five differences
  above are the first to look at (each with its numbers in Compositing-Port's "Left as they were"; none can reach the
  sheet's number fields, which have no max, or its line-heights, which are numbers).

### Step 7: the sheet (2026-11-16/17, the branch `gm2-step7`)

**Decided on the way.** The user (2026-11-16, the question tool, through the manager): Q46–Q49 (the captures approved;
the race and level under the name; the seal in full colour; the paper doll as shown) and Q50 (the seal left out of the
picture comparisons); the manager's decisions of the same day ("Decisions", "Questions from the steps").

**The screen** (`Game/Statventure.Game/Screens/Sheet/`), on GM1's character page (the same `SheetScreen`, its folder):
- `SheetMarkup`: the old page's markup (`old-game/index.html` 21–25 and 64–299, and its footer, 556–559, inside the old
  `main.shell`), with what the decisions change: the hero's name as the heading (`h1#page-title.hero-name`) and the race
  and level under it (`p#hero-standing`, Q30, Q47) in place of the old copy line; the seal button (`#race-select-button`)
  with the emblem's `<img id="race-icon">` and the old ✦ fallback; the equipment as a paper doll (`.paper-doll`, Q29): the
  Primary hand slot, `figure.portrait-panel` holding `<surface id="hero-view" class="portrait-view">` (step 11's view),
  the Off-hand slot, then Armor, Artifact I and II, each slot the old page's inert "future" slot
  (`equipment-slot-future`, "Empty slot", Q33, the old ids kept); the whole rail (`aside#field-tools-menu`, Q34): the
  purse, "COMPANIONS · 0 / 3" and "No companions recruited yet." (GM11's), the eight tools disabled (Combat
  `data-dev-only`, as the old), the "☰ Field tools" toggle and its shade before the page (the old body's); the eight
  attribute cards as the old `createStatCards` writes them (`SheetMarkup.Attributes`: the old `statDefinitions`). The
  Limitless toggle (GM15) and the old dialogs aren't there.
- `SheetScreen`: **Q26's rule** `Ready(save)` (since step 8 `HeroFlow.Needs(save, context)`, the one place both screens
  ask, with these words): the hero has a race, and a look unless its race is one the data has as enemy-only (Dev Mode's
  hero, Q27, has none); a race the data doesn't have (a mod's, switched off) can't be told apart and gets the sheet
  ("Unknown race"). On every open of a character (`GameSession.Opened`: `game-opened` played, the
  status line emptied, as GM1) and on **`open-sheet`** (registered here, possible while a character is open; the flow's
  end runs it): the sheet shown when ready, else the page hidden, `[Game] the hero has no race|look: the new hero's flow
  asked for` logged and **`open-races`** run (step 8's; not registered on this branch, so the commands log it as not
  registered). Shown: the page, ⚙, the drawer's toggle, `body.has-field-tools` (the rail's room); hidden on `Closed`.
  Every save of the open character (`Saved`, the dot as GM1) and every change of Dev Mode (`Settings.Changed`) shows it
  again. **What it shows** is `ActorRules.Evaluate(hero, context, devMode)`, the context `SheetContext.Plain.WithRaces(
  RaceCatalogue.Load(<the document's data>))` (the game's data: the same `VirtualFileSystem` as `Application.Data`), every
  number written as the old `updateDerivedValues` writes it (`formatValue` or `String(n)` where the old code has each), a
  field written only when its text differs. **Every control calls its rule** (the old handlers): "+" `Raise`, "−" `Lower`,
  the attribute, bonus, Armor/Evasion bonus and purse fields on `input` (`SetBaseAttribute`, `SetBonus`,
  `SetDefenseBonus`, `SetPurse`, the field's text read as JavaScript's `Number`), HP and Mana's − and + with the amount's
  whole value (`ChangeResource`; nothing below 1; the field emptied), Full HP, Full Mana, Restore both, the EXP form
  (`AddExperience`, the field emptied when added), Respec, Level up; **an action the rule refuses (the same sheet back)
  saves nothing and sounds nothing of its own**, else the result goes to `GameSession.Change` and GM2-P17's sound plays:
  `point-spent` when the "+" spends a free point (Dev Mode's "+" with none left and "−" keep GM1's press), `level-up`
  (Dev Mode's too), `respec`. The seal: in Dev Mode it runs `open-races` (Q27), outside it is disabled with "<race> · Race
  locked"; in full colour either way (Q48: `sheet.css`).
- `SheetTexts`: JavaScript's `String(n)` (ECMA-262's Number::toString: shortest round-trip digits, plain from 10⁻⁶ to
  below 10²¹), the old `formatValue`, the EXP card's lines (checked against every case of `sheet-values.json`'s `texts`),
  the meters' `scaleX`, the attribute bars, the race's name ("Unknown race"), the seal's title, the line under the name.
- `EmblemPictures`: the seal's picture, a placeholder until step 10 (the old seals' brass rim with 24 ticks and an inner
  ring round the game's four-pointed star, the same for every race with an emblem; an SVG data URL, so the engine, the
  program's window and Chromium draw it alike); a race with no emblem shows the ✦. **Step 10 replaces `For(race)`** (by
  `Race.Emblem`'s name).
- `FieldTools`: the old `interface.js` (its first part) line by line: under 840 px (`matchMedia`) the rail is a drawer,
  inert and `aria-hidden` while closed, opened by the toggle (the focus on its first enabled control: the purse in Dev
  Mode, else the toggle keeps it), closed by the toggle, the shade, Escape (the focus back on the toggle), a tool's click
  (not taking the focus back) and the page widening past 840 px; Tab and Shift+Tab kept among its controls and the
  toggle. A window over the page takes the keys (its Escape closes it, not the drawer).

**`Assets/UI/styles/sheet.css`:** the rail's room only while the sheet shows (`body.has-field-tools { padding-left:
var(--field-menu-width) }`, none under 840 px; the start screen and its windows stay centred, GM1's Q32); the drawer's
toggle and shade hidden with the page by their `hidden` (the old stylesheet's `[hidden] { display: none !important }`
outweighs game.css's display for them under 840 px); the page's end clearing
the ⚙ button (`main.game-page { padding-bottom: 72px }`: the button 16 px up and 38 px tall, so the footer's line reads
whole scrolled to the end, checked at 1440 and 820 px); the heading (`.hero-title { flex: 1 1 320px; min-width: 0 }`, so
a long name wraps beside the seal; `.hero-standing`); the seal undimmed (`:root .hero-sigil:disabled { opacity: 1 }`,
Q48); the paper doll (three equal columns, the hands centred on the panel, two columns under 760 px with the panel on
top, one under 470) and the portrait panel (480 px, a 1 px `#6e5538` border, the leather gradient with brass corner
brackets in both themes, the view's window 20 px in on `--portrait-ground: #2a1f1a` with a brass hairline and a soft
vignette drawn over it: `::after`). `ui.pak` made again.

**For later steps.** *Step 8:* the flow starts on `open-races` (asked by the sheet for a hero that isn't ready, and by the
seal in Dev Mode) and ends with `GameSession.Change(save => save with { Hero = …, Look = … })` then `open-sheet`;
`SheetScreen.Ready(save)` is Q26's rule here, `HeroFlow.Needs` step 8's: the step merged second makes them one (step 8
did). The tests' stand-in for the flow is `ScreensHarness.NewHero` (New, `Ready`, `open-sheet`). *Step 11:* the view is
`surface#hero-view` in `figure.portrait-panel`, positioned 20 px inside the panel's border (480 px tall at every width;
the panel's width the middle column's); its ground the surface's computed `background-color` (`--portrait-ground`,
`#2a1f1a`, the same in both themes); the view must pause while the page is hidden (`SheetScreen.Showing`) or covered
(GM2-P16); the screens' fixtures compare the panel unmasked until step 5's mask is in place, which must come before a
view draws there. *Step 10:* `EmblemPictures.For(race)`, by the race's `Emblem`; the seal is 172 px (138 at 840 px or
less, 100 at 650 px or less: game.css's later rules win; *corrected by step 10, 2026-11-21: this note said "123 under 760,
93 under 470"*), its picture 72 % of the button's content box (the button less its 1 px border and its own padding of 1 by
6 px, so 113.76 × 120.96 CSS px, 89.28 × 96.48, 61.92 × 69.12, the square picture drawn as wide as that: measured in the
engine and in Chromium alike; *the note said "72 % of it"*), turned 11° with the button and back. *Step 5:* the fixtures' mask hook
below. *Step 13:* `end_to_end.py`'s Continue and load now expect the new hero's flow for PlayerFolderTests' character
(no race): the log line and the page alone; GM2's whole flow (a race and a look, then the sheet) is step 13's to write.
*GM3:* the slots' `equipment-slot-future` and their ids are the old page's; *GM8:* class points (the Classes tool's
flash); *GM11:* the companions' list; *GM15:* the Limitless toggle and the cap's text.

**The fixtures' mask hook** (`Tests/Statventure.Game.Tests/Ui/`; the user's Q50, the manager's leave): a
`GameScreenFixture` names boxes to mask by selector (`Masks`); `GameScreenFixtures.MaskBoxes` gives each matching
element's border box as laid out, before its own transform (a box turned in place about its centre, as the round seal,
keeps its footprint), with the reach of its outer box shadows past their offsets, a pixel around, in device pixels;
`GameScreenTests` takes those boxes from the golden as it takes the icons'. The sheet's and the page's fixtures name
`#race-select-button` (`SheetScreenFixtures.SealMask`); step 5 names the `surface` boxes the same way (GM2-P12). The
fixture list also carries the page's own scroll now (`scrollX`, `scrollY`: the root's offsets), so a long page's lower
part is a fixture of the window's size.

**GM1's tests restated for Q26** (the manager's leave; each keeps its point; helpers in `ScreensHarness`: `open-races`
among the recording stand-ins, `WithHero`, `Ready`, `NewHero`): `StartScreenTests` (Continue opens a hero with a race and
a look onto its sheet; New character makes and opens the character and asks for the new hero's flow, the sheet not
shown; a character left for another never shows the start screen between, both new heroes asking for the flow);
`PageTests` (the page for a hero with a race and a look, whoever opens it; Menu and ⚙; the rail test now proves the room
kept while the sheet shows, 260 px at 1200, none under 840 px and none with no character); `MenuTests` (two, on a
ready hero), `StatusLineTests` (one), `ScreenFlowTests` (four: its `Saved` helper and the imported character file give a
race and a look); the fixtures `page-*` (1200 × 400: the rail and the sheet's top beside the top bar) and `menu-page-*`
open a ready hero, their goldens captured again. **`end_to_end.py`:** `--run continue` and `--run load-character` expect
the log line `the hero has no race: the new hero's flow asked for` (after `Continue opens`, before `opened`; after `loaded
into`) and the picture "the stone page alone" (`page_alone`: the page's colour most, no lettering anywhere, no start
dialog) where they expected the character's page; `character_page` stays for step 13.

**Tests** (game): `SheetScreenTests` (Q26 and Q27's rule with the flow asked for and logged; `open-sheet`; every number
shown against `Evaluate`; the races' bonuses in the fields; Dev Mode live; a save shown again; the seal and its emblem;
every control's rule and save and sound; refusals saving nothing; typed fields outside Dev Mode; a long word in the name;
the race and level line's colour; the paper doll, its slots' rows and its panel's frame, corners and ground; the rail; the
drawer; the EXP texts on every oracle case; JavaScript's number text, `formatValue`, the
meters, bars and race words; the fixture list's scroll; the seal's mask against the seal's box), `SheetHeroes`,
`SheetScreenFixtures` (13: the played page at its top, its doll and its end in both themes, Dev Mode at the top and the
attributes, a new hero, a long name, 820 px with the drawer closed and open and its doll; the seal masked; through the
real hidden window too, `ProgramPathScreenTests`), `GameScreenTests.AFixturesNamedBoxesAreTheirInkAsLaidOut`.

**Shown able to fail** (`mutate.py` with the game's project, under the shared lock in a headless cage;
`claude-work/gm2/step7/mutations.json`, made by `make-mutations.py`; the filter the sheet's, the page's, GM1's restated
classes, the mask hook's test, the fixture list and one fixture's comparison): 168 mutations of every rule the step adds:
Q26's rule and its flow (the race, the look, the enemy-only race, the log line, `open-races`, `open-sheet`), showing and
hiding (the page, ⚙, the toggle, the rail's room, the drawer closed on leaving), every value written (each field, meter,
bar, alert, Dev Mode's controls, the seal's state and title, the race's line), every action (its rule, its arguments, the
save, the sound, a refusal), the texts (`String(n)`'s ranges, `formatValue`, the EXP lines, the meters' clamp, the bars'
bounds), the emblem's seam, the drawer (`interface.js`'s every branch), the mask hook (the pixel around, the shadow's
reach and offset, inset shadows left out, only the named boxes, the boxes masked at all) and the fixture list's scroll. The first full run: 159 killed, 9
survived; six were gaps, closed and killed in the rerun (`mutations-rerun.json`): a field's text as JavaScript's `Number`
reads it (white space alone is 0, a no-break space is white space, an infinity is no number), HP and Mana's amount
counted by its size (`Math.abs` of the floor: "-5.5" takes 6), the focus back on the drawer's toggle from inside it, and
a disabled control not the drawer's to focus (outside Dev Mode the toggle is its only stop); one showed a redundant line
(`Show` updating the drawer, which every change already updates), taken out with its mutation. **The standing list: 167,
165 killed, 2 that can't differ:** the level's cap not written (it is 100 for every hero until GM15's Limitless, and the
markup says "CAP 100"); Dev Mode's level-up rule always called as Dev Mode's (the button is disabled exactly when the
rule outside Dev Mode refuses, the old `app.js` 1823, so the rule is never reached with too little EXP). Nothing written
to the player's real folders. **The stylesheet** (`css-mutate.py`, a runner of its own as GM1 step 10's: each mutation of
`sheet.css` packed into `ui.pak` before its tests, the source and the pack put back after; on `main` with the engine
fixes): 26 mutations, one per rule (the rail's room, the page's end, the heading, the race and level line, the seal
undimmed, the paper doll's columns, hands and slots, the panel's height, frame, corners, border, shadow, ground and
window, the narrow layouts). The first run: 20 killed, 6 survived; five were gaps, closed with computed-style and layout
checks (a single long word in the name kept inside the heading; the race and level line in the race name's colour, not
the ink; the slots below stretched to one height when one holds more; the panel's four corner brackets; its border's
own brown in both themes: each too small for the pictures' 0.2 %) and killed in the rerun (`css-mutations-rerun.json`);
one showed a redundant rule (the drawer's toggle and shade hidden by their `hidden`, which the old stylesheet's
`[hidden] { display: none !important }` already does), taken out with its mutation. **The standing list: 25, 25
killed.** **`end_to_end.py`** (2026-11-17, on `6a33ced`, before the rebase): 243 of 243 on the GPU
(122 s, `--publish`) and on lavapipe (266 s); its Q26 checks broken one at a time on the same archives (the log line's
words; the two pictures judged as the character's page, what they expected before): each break fails its two checks
and only those (241 of 243).

**The comparisons** (pixels past 12 levels against the limit, 0.2 %; the seal masked, Q50, which took 1,150–1,360 pixels
off each fixture showing it). Before `gm2-engine-fixes` (2026-11-16) eight failed, each for the engine difference named
(the fixtures neither loosened nor resized): *the spin button's room* (a number field under `appearance: textfield`
keeps Blink's 15 px spin button, transparent; the engine left it out): `sheet-attributes-dark` 3,924 at 1× (limit 2,764)
and 8,955 at 1.65 (7,527), `sheet-attributes-light` 3,879 and 9,610, `sheet-dev-attributes-dark` 4,096 and 10,027;
*`overflow-wrap` under `nowrap`* (the purse's "PLATINUM" broken in the engine at a 260 px rail, whole in Chromium):
`page-dark` 5,558 at 1× (limit 960) and 12,533 at 1.65 (2,613), `page-light` 5,678 and 12,849, `page-status-light` 5,666
and 12,809; *a number line-height kept as a float* (a 40 px heading at 1.05 laid 41.984375 high, Blink's 42):
`sheet-drawer-closed-light` 1,321 at 1× (limit 1,148). **On `main` with the fixes** (2026-11-17, rebased on `55ed6b0`,
nothing of the step's changed for them): those eight 555 and 1,079, 621 and 1,892, 592 and 2,026; 80 and 177, 90 and 275,
78 and 235; 155 (and 504 at 1.65); every sheet and page fixture at 31 % of its limit or less at 1× and 38 % or less at
1.65 (`sheet-dev-light` 840 and 2,888 the most; the `menu-page` pair 21–48); the real hidden window's frames the
harness's (`ProgramPathScreenTests`).

**Runs** (headless, scratch per-user folders, nothing audible; on `main`'s `55ed6b0`, 2026-11-18): the full suite on the
GPU (323 s with the build) and on lavapipe (371 s): **4392 + 748 = 5140 tests** (`main`: 4392 + 659 = 5051; 89 new, all
the game's), none held or skipped; `end_to_end.py` on the final tree 243 of 243 on the GPU (118 s, `--publish`). Goldens: the 13 sheet fixtures at 1 and
1.65 (26, Chromium's), GM1's `page-*` and `menu-page-*` captured again for a ready hero (10); `ui.pak` with `sheet.css`.

**Choices made** (internal; recorded here): the Q26 rule's unknown race getting the sheet; the page hidden whole for a hero
that isn't ready (step 8's screens show what the flow shows); `game-opened` on every open as GM1; an action saved only
when its rule didn't refuse it (the old handlers' early returns), the purse's field shown again after its input as the
old; the emblem's placeholder an SVG data URL in the program; the races read from the document's data; `open-sheet`
showing nothing new when the sheet already shows but writing it again; the fixtures at the game's first window (1440 ×
960) scrolled to the sheet's parts, and 820 × 700 for the drawer; the drawer's open fixture waiting for its slide.
### Step 5: the game's look in the engine and figures built from looks (2026-11-17/18, the branch `gm2-step5`)

**Decided on the way.** The manager (2026-11-18, on this step's question): the human race's swatches are **step 9a's**,
approved there with the human's builds, faces and presets; until then the eight human presets draw their models' own
placeholder colours, the expected state (below, "The look data"). Nothing else was asked: the record's path and fields,
the builder's refusals and the samples are this step's internal choices (below).

**`render.style`** (`Engine/Statventure/Render3D/RenderStyle.cs`; the user's Q12, GM2-P11): the engine's record of a look
of the 3D views, `[Kind("Render style", "render/styles")]`, version 1: every number of `CelStyle` under its property's name
in camelCase (`ink` "#rrggbb" `[Colour]`, `shadowMix`, `bandEdge`, `outlineWidth`, `innerWidth`, `creaseAngle`,
`hatchSpacing`, `hatchWidth`, `hatchAngle`, `hatchInk`, `glowWide`, `glowCore`, `glowBoost`, with their units) and `sun`
(`render.style.sun`: `direction` (x, y, z) toward it, `strength`, `angle`), exactly a look file's `style` and `sun`
(`look.py`), so one look reads the same on both sides. `Cel` and `SunLight` give a view its style and sun; `Of(CelStyle,
SunLight)` the record of them (the ink to its nearest level). A new style is the engine's own look (`CelStyle.Default`
under `look.py`'s `today` sun). Its rules are the look file's (the figure mode's `FigureLook`, step 3a), no more: the ink a
colour, every number finite, the sun's direction not 0, its strength and angle 0 or more (`[Check]`s, no `[Limits]`
marks: G4); `Load(data, path)` refuses a broken one naming the entry. The background and the figures' colours are not a
style's (the view's own ground, the palettes). The engine's test scenes keep `CelStyle.Default` (Q11).
**The game's entry** is `render/styles/game` in `data.pak` (`Assets/Data/render/styles/game.json`), the look the user chose,
"Ember, hearth", written from `Tools/Models/humanoid/looks/ember-hearth.json` by the game's tests
(`STATVENTURE_STYLE_DATA=write`, `GameStyleTests`, then `make-data-pack.sh`); `GameStyle.Load(data)`
(`Game/Statventure.Game/Figures/`) is how a game view takes it (step 11's views). A mod restyles the game by an entry of
its own at that path.

**The figure builder** (`Game/Statventure.Game/Figures/HeroFigure.cs`; GM2-P8). `HeroFigure.Resolve(data, look, gear?)`
reads the records only and gives the plan: the body's model; each part the look chooses with its model for the look's body
(a part with no model drawn as nothing), in the maker's order of categories (a key that isn't one after them by name); the
palette's colours, each the race's swatch of the name the look chose for its slot; the gear's garments, items (model and
socket) and colours. A look naming anything the data hasn't is refused with all of it named
(`LookRefusedException`, GM2-P18): no body, the body's model, no part, a part with models but none for the body (moot
without the body), a part's model, the race (when it chooses colours), a swatch; the gear's models likewise. What a look
leaves out it may leave out: a category the body offers that the look doesn't choose draws nothing (a mod that adds a
category mustn't refuse older heroes; the maker's check still asks for every one). `HeroFigure.Build(device, data, look,
gear?)` makes one `Actor` of the body's model (its skeleton and plain outfit) with an empty `AnimationStack` (the screens
add their clips, step 11), every part's model and the gear's garments as `ActorGarment`s bound to the body's bones by name
(no refit: a part is fitted to the bodies its models name), the gear's items as `ActorItem`s on their sockets, and the
palette of the plan's colours; it owns its models (disposing it frees them) and leaves nothing made when it fails (a
refusal, a model that doesn't read, an item on a socket the body hasn't). `FigureGear` is GM3's seam: its colours may
name only the gear's own slots (never a look's, GM2-P7: one actor palette colours figure and gear). Look parts are all
garments: the look records name no socket, and step 4a made the faces and hair bound wholly to `head`; rigid items on
sockets are the gear's (GM2-P8's "rigid parts" there).

**The look data** (`Assets/Data/data/looks/`, the one-step writer of step 6, run for what the first humanoid has): the four
parts written by hand (`face-human-male-1` and `face-human-female-1`, shown as "Face 1"; `hair-swept-back` "Swept back"
and `hair-long-tied` "Long, tied"; each for its sex, its model fitted to its sex's average body, its `slots` its
materials'); then `STATVENTURE_LOOK_DATA=write` wrote the two bodies (`human-male-average`, `human-female-average`) and the
eight human presets, each preset's parts the face and hair of its sex (filled in its file, then the writer again, which
keeps them); `make-data-pack.sh`: `data.pak` 43 entries (28 races, 2 bodies, 4 parts, 8 presets, the style). **The
presets have no colours: the human race has no swatches yet**, so every human figure draws its models' own placeholder
colours (`materials.py`'s, not the approved palette); that is the expected state until **step 9a, which adds the human
race's swatches** (in `Assets/Data/data/races/human.json`, kept by the race converter) **and each preset's colours** (its
`colours` filled in its file, the writer run again), then `make-data-pack.sh`; the shared parts' names and lists (the
provisional "Face 1", "Swept back", "Long, tied" among them) are 9a's too. No comparison or golden of this step holds the
placeholder colours as a look (below: every sample is coloured by swatches of the tests').

**`assemble.py` and the comparisons.** `Tools/Models/humanoid/assemble.py` reads a `look.hero` against the game's data on its
own (the records' JSON forms in data folders, a later folder's entry over an earlier's as a mod's over the game's; a model
`models/<path>` as `<models>/<path>.glb`; the style and sun the data's `render/styles/game`) and refuses what the game
refuses, then poses, frames and writes the scene as render.py frames a figure (render_scenes.py's format). Two ways in:
`make-blender-references.sh` (its step 6: `Reference/assembly-looks.json` into `Reference/assembly-scenes.json`, then
rendered into `Golden/r3d-assembly-*.png` and `-ink.png`; `--out` too) and `make-humanoid.sh assemble --out <folder>
--name <stem> --look-hero <file> --data <folder>… [render.py's view options, --garment, --item, --gear-colour]` (a look's
pictures for approvals, the ink laid over the shading as `render` does). **The game's tests' side**
(`Tests/Statventure.Game.Tests/Figures/AssemblyTests.cs`): six samples, each the parts of the game's first presets in
colours of the tests' own swatches (a human race of the tests' over the game's data, `Reference/assembly-data/`, as a mod's
data would be; the game's look's colours among them, so `assembly-human-male-ember` is the approved man): the man and the
woman in the game's look's colours at three-quarter and from the front, others in every slot from the back and the side
and the woman's face in a close-up, and the man in the sample gear (the chain shirt a garment, the sword and the shield on
their sockets, the look file's metals) posed by `rest-sway` at 0.5 s. `STATVENTURE_ASSEMBLY_LOOKS=write` writes the list and
the tests' race; a change of a sample, a preset or a swatch fails `TheListIsUpToDate` until they are written and assembled
again. Then each figure built by `HeroFigure.Build` in the game's style (data.pak's) on the portrait panel's ground, framed
by the figure mode's `FigureFraming` (render.py's numbers), is held to Blender's: **the same models in the same order, the
same palette, the camera within 10⁻⁴, each item at its socket within 10⁻⁵** (`TheFiguresHoldWhatBlendersAssemblyHolds`),
then the shading and the lines under `ReferenceLimits.DetailedFigure` (unchanged). Measured on both drivers (PC GPU /
lavapipe; the limits for 172,800 px: 41 inside, 157 edge pixels; for the close-up's 102,400: 24 and 93):

| Sample | Inside | Edge | Edges' mean | Missed | Extra | Ink off | Coverage | Unlike |
|---|---|---|---|---|---|---|---|---|
| man, game's colours, ¾ | 6 / 6 | 35 / 30 | 2.1 / 2.2 | 0.41 % | 1.11 / 1.20 % | −1.4 % | 0.082 | 1.93 % |
| woman, game's colours, front | 5 / 5 | 33 / 30 | 2.3 / 2.4 | 0.45 % | 0.57 / 0.61 % | −1.5 % | 0.075 | 1.60 % |
| man, other colours, back | 17 / 17 | 31 / 35 | 2.8 | 0.13 % | 1.19 / 1.26 % | −0.7 / −0.8 % | 0.086 | 2.07 % |
| woman, other colours, side | 6 / 8 | 17 / 19 | 2.7 | 0.66 / 0.71 % | 1.35 / 1.42 % | −2.9 / −3.0 % | 0.093 | 2.97 % |
| woman's face, close-up | 18 / 16 | 60 / 61 | 3.4 / 3.5 | **2.50 %** | 5.95 / 6.23 % | +1.1 % | **0.134** | 6.78 / 6.81 % |
| man geared, posed, ¾ | 8 / 11 | 67 / 56 | 2.6 | 0.16 % | 0.78 / 0.83 % | −2.5 % | 0.085 | 3.01 / 3.03 % |
| limit (`DetailedFigure`) | 1 in 4,200 | 1 in 1,100 | 4 | 2.7 % | 8 % | 10 % | 0.14 | 13.5 % |

The close-up is the nearest to its limits, as step 4a's woman's close-up was (the same face from the same view: missed 2.50
%, coverage 0.134). `TheAssemblysCategoriesAreTheMakers` holds `assemble.py`'s order of categories to `LookNames`.

*Since the ink's engine task ("The ink's cover", 2026-11-18, rebased onto this step):* the six measured again on both
drivers with its final rule, the shading unchanged (the ink task draws nothing there), the ink under the user's Q60
limits (missed 2 %, extra 3 %, coverage 0.12, unlike 8.3 %): missed 0–0.04 %, extra 0.09–1.82 % (the face's close-up,
1.74 % on the GPU), coverage 0.067–0.086, unlike 0.17–2.55 % (the man geared, from 3.01 / 3.03 %); the face's close-up
from 2.50 % missed, 5.95 / 6.23 % extra, 0.134 coverage and 6.78 / 6.81 % unlike to 0 %, 1.74 / 1.82 %, 0.086 and
0.86 %.

**The structural walk** (`Figures/FigureWalkTests.cs`): over the game's packs, every body with every option offered on it
(each pair) and every slot of its race's swatches: each option resolves, each model reads, each part's model binds to the
body's bones (every joint a bone of the body's, at the body's rest within 10⁻⁵: made on that body), no model is drawn by two
categories, every material is a look's slot or a fixed colour (the pupils, the glints) and a part's materials' slots are
its `slots`, every slot the body and its options use is filled by the race's swatches, and every preset builds. Whether a
figure builds whole is decided by its body and each option and slot apart (no option reaches into another), so the pairs
walk every combination (reported as their count: the product of each category's options and each slot's swatches). Now:
two bodies, four pairs, two combinations; **the human is listed as drawn in its models' own colours** (no swatches; the
test pins that list, so step 9a empties it). `EachBreakIsFound` shows each rule finding its break with a record of the
test's over the game's data (another body's model, a model without bones, a slot left out or one too many, a model drawn
twice, an unfilled slot, a part drawn as nothing with slots, a preset naming nothing, a missing model).

**The 3D views' mask in the game's screen fixtures (GM2-P12).** The same mechanism as step 7's mask hook (built from its
branch line for line while it wasn't merged; step 7 merged first, so the rebase met one conflicting line):
`GameScreenFixture.Masks` (selectors), `GameScreenFixtures.MaskBoxes` (each match's border box as laid out, before its own
transform, with its outer shadows' reach, a pixel around, device pixels) and its test, step 7's; this step names the views
for every fixture, `GameScreenFixtures.Views` (`surface`), masked besides what a fixture names (`[.. Views, ..
fixture.Masks ?? []]` in `TheScreenPaintsLikeChromium`; the sheet's `surface#hero-view` among them, empty until step 11), the test
`EveryViewsBoxIsMasked`, and the fixture `view-masked-dark` (`Ui/ViewScreenFixtures.cs`, captured in Chromium at 1 and
1.65): a framed panel holding a `surface` that the engine draws empty and Chromium fills with its red child, so the
comparison passes only with the mask (and `ProgramPathScreenTests` draws it in a window as the harness does). **Engine
and Chromium differ on `surface`** (met here, not changed): the engine lays it out as a replaced element (a 300 × 150
natural size, as a canvas; its children never drawn), Chromium as an unknown element (inline, no natural size, its children
drawn). So a view's box must be sized fully (`display: block`, a width and a height), as step 7's `surface#hero-view` is.

**The measurement of a view with no scenery** (one figure as the sheet shows it: the man in the game's style at 240 × 440,
the portrait view's window, at 396 × 726, its size at 1.65, and at 600 × 800; a scratch probe, medians of 200 frames, the
machine loaded by other workers; `claude-work/gm2/step5/measure-view-*.txt`): **the shadow pass doesn't run** (the figure's
13 draw items cast and none receives: `FitShadow` gives nothing to fit), so no frame time goes there: `View3D.Update` 0.22
/ 0.26–0.30 / 0.45–0.49 ms on the GPU, `Renderer3D.Render` alone 0.17 / 0.19–0.24 / 0.28 ms (the lines about 0.06–0.15 of
it); on lavapipe 19.4 / 20.9 / 38.2 ms (the lines about 4.7). Were the figure to receive (scenery in the view), the pass
into the sun's 16 maps would add about 0.45 ms on the GPU and 77 ms on lavapipe. **But the 4096² shadow atlas (64 MiB) is
allocated for every view's render target all the same** (`RenderTarget3D`), unused: 64 of the view's 76 MiB at 240 × 440,
of 97 at 396 × 726, of 119 at 600 × 800 (the rest the MSAA colour, depth and surface records, the edges and features, the
picture and the records' readback). An engine task for the manager: the atlas made only for a view with something that
receives shadows (GM2-P16's memory).

**What moved beside.** `data.pak` gains the `data/looks` and `render/styles` folders, so the editor's screens over the
repo's packs show them: 18 goldens captured again (9 fixtures at both scales; every changed pixel in the entry list's
pane, at 1× within x 0–280: `claude-work/gm2/step5/editor-goldens/diff.txt`), `list-marks` made 840 px tall (760 since
step 4a) so its tree still fits without scrolling (Chromium scrolls a focused tree into view, the engine's `Focus()`
doesn't); `EntryListTests` name the new folders (321 rows); `EngineKindsFormTests` enters 4 where a number field holds 3
already (the sun's strength), so an edit changes the value; `EngineKindMarksTests.Kinds` has `render.style` (its marks in
`Data/Records/Marks/render.style.txt`); `RaceDataTests` and `LookDataTests` allow the game's style beside the races and the
maker's records; the comparisons of a render with Blender's moved into `Render3DReferenceTests.Comparison.cs` (a part of
the class), linked into the game's tests with `ReferenceLimits.cs` and the figure mode's `FigureLook.cs` and
`FigureView.cs`.

**Tests** (engine, 33: `RenderStyleTests` 28 and the kinds' theories' cases for the new kind; game, 48: `GameStyleTests` 3,
`HeroFigureTests` 16, `FigureWalkTests` 10, `AssemblyTests` 15, `GameScreenTests`' `EveryViewsBoxIsMasked` and the
fixture's 2, `ProgramPathScreenTests` 1 more).

**Shown able to fail.** *The C# rules* (`mutate.py` under the shared lock in a headless cage;
`claude-work/gm2/step5/mutations.json`, made by `make-mutations.py`): 78 mutations of `render.style` (each number's field
both ways, the sun's, the ink's rounding, every rule and default, the kind's folder and colour mark), the game's style (its
path, its check), the builder (every refusal, the fit by body, swatches by slot and name, the race read only for colours,
the gear's models, slots and colours, the parts' order, the gear's garments after the parts', the palette, the garments
bound to the body's skeleton, the items on their sockets), the walk's own rules, and the mask (no view, another tag, the
named boxes, a pixel around, inset shadows, an offset, a spread, only the first match). The first run: 74 killed, 4
survived. Three were gaps, closed and killed in the reruns (`mutations-rerun.json`, `mutations-rerun2.json`; the
garments' test now poses each bone its own way): the ink's levels truncated (every ink read from "#rrggbb" sits on a level;
now an ink between levels made in code), the game's style loaded unchecked (now a broken style in its place, a mod's,
refused), the garments bound to their own skeleton (the family's models list their bones in the body's order, so the
indices agree; now a part whose model has a node of its own first still follows the body's bones by name). **The standing
list: 78, 77 killed, one that can't show here:** the fixture's named boxes ignored (no fixture on this branch names a mask; step 7's sheet fixtures do, and its
own mutations hold them). *`assemble.py`'s rules* (a runner of its own, `assemble-rules.py`: each break applied to a copy
of the scripts, nine cases run through the pinned Blender, headless: the committed list assembled as committed, six
refusals with their exact words, a part with two models the first another body's under a later data folder's style, a
look listing hair before its face): **16 of 16 killed** (a swatch by name whatever its slot, the first model whatever its
bodies, each refusal unreported, a fit reported without a body, the gear's models, colours and items, the data folders'
order, the style not the data's, the parts' order, framing posed, the background). Nothing written to the player's real
folders.

**Runs** (headless, scratch per-user folders, nothing audible): the full suite on the GPU and on lavapipe on `main`'s
`feb869f` (step 7 merged; the rebase met step 7's mask hook on one line, its fixture list made again by the tests):
**4425 + 796 = 5221 tests** (`main`: 4392 + 748 = 5140; 81 new, 33 the engine's and 48 the game's), none held; before
the rebase the same on `55ed6b0` (4425 + 708); `view-masked-dark` captured again after the rebase, the same bytes.
`make-blender-references.sh --out` with `compare_references.py`: **160 of 160 byte-equal**, the 12 assembly renders among
them. The player's real folders unchanged through every run.

**Choices made (internal, recorded here):** the style's path `render/styles/game` in a kind folder `render/styles`, its
fields named as a look file's, the sun a nested record, its rules the look file's with no `[Limits]`, its defaults the
engine's look; the builder in the program (`Figures/`), its plan apart from the build (the walk and the comparisons read
it); refusing every missing thing a look names, never a category it leaves out; look parts all garments, the gear's
items the only rigid pieces; gear colours only in the gear's slots; the four parts' entry names category-prefixed (as step
6's test data) and their provisional names; samples only in the tests' colours, from the presets' parts; the comparison
code made a partial class to be linked; Blender's side resolving the look on its own and its scene file committed and
compared with the engine's figure; the walk by pairs; step 7's mask hook with `surface` named for every fixture; the view fixture's red child
as what Chromium draws in a view's box.

**For later steps.** *Step 11:* `HeroFigure.Build(device, Application.Data, save.Look)` (catch `LookRefusedException`: a
mod gone under a slot's save), its `Skeleton` for the idle clip's layer, `GameStyle.Load(data)` for the view's `Style` and
`Sun`, the view's `Background` the panel's ground (`--portrait-ground`); the view's `surface` sized fully (above); the
fixtures need nothing (their `surface` boxes are masked). *Step 9a:* the human race's swatches and the human presets'
colours (then the walk's list of uncoloured races empties and every slot is held to the swatches), the shared parts' lists
and names; a preset whose parts change changes `AssemblyTests`' list: write it (`STATVENTURE_ASSEMBLY_LOOKS=write`) and
run `make-blender-references.sh` (or its step 6 alone) for the `assembly-*` scenes. *Step 8 (and any screen with a view):*
nothing to name: every `surface` box is masked already. *Not done here (not in the step's brief):* the sandbox's figure mode
taking a `look.hero` (`--figure <look.json>`, which step 3a left to this builder) still takes a body `.glb` with its
garments and items; `make-humanoid.sh assemble` draws a look on Blender's side.

### The ink's cover: what hides a line, as Freestyle hides it (2026-11-17/18, the branch `gm2-ink-cover`)

The engine task step 4a recorded ("The ink's depth allowance", `Docs/Game-Plan.md`, "Engine items the port will meet";
Q55's "refine the lines after"), done in the engine's ink (`Docs/Renderer-3D.md`, "Ink lines, how they're found",
"Feature edges, how they're drawn", "Detailed figures"). **What it found first:** the lines step 4a put down to the
feature edges' allowance were mostly another rule's (step 4a's note, corrected where it stands): the per-pixel crease
within a part drew where two pieces of one part meet at a steep angle without a break in depth (the hair's cap and the
forehead, the jaw and the neck, a lock on the cap), an intersection Freestyle never draws; a model joined per ink flag
is one part, and the face, the hair and the chain shirt are garments exported with the body's skeleton, their mesh nodes
numbered as the body's. The allowance drew the gear's doubled lines (the shield's rim over its field, the sword's
pieces). **What it does** (the manager accepted the design on 2026-11-18): the main pass writes each sample's sheet, a
16-bit hash of the mesh drawn (by model, mesh and node) and the triangle's island (`Model`: a mesh's primitives welded
by position and joined by their triangles); a feature edge knows its two faces (their opposite vertices at load) and its
island; a point of it is hidden when another sheet, the pixel's sample nearest the point, lies in front of it by more
than that surface's coarse normal leaves unsure when its plane is carried to the point (Freestyle's rule: any other
surface in front, however close, hides an edge), and keeps the old allowance against its own sheet; the per-pixel
crease is drawn only within one sheet; and the meshes' silhouette edges (their two faces, from the posed vertices each
frame, one toward the viewer and one away: Freestyle's contour with face smoothness off, as the reference renders) draw
a piece's contour lying on another piece of its part, which no break in depth shows, but not beside another part of the
model, where the per-pixel tests decide as M6's scenes were measured (`pose-side`: Freestyle judges the tube's long
contour along the fin by its middle and hides it whole). The choices made inside the task (internal, the worker's): a
sheet attachment of its own (R16_UINT, 16-bit hash: two pieces share a number one time in 65,536) rather than the
surface record's bits, so the per-pixel rules keep their part; the sheet's mesh number per model, mesh and node; an
island per vertex (a flat attribute) rather than per triangle; the cover judged by the pixel's sample nearest the
point, against another sheet only (against its own, sample 0 as before); the cover's plane carried within its normal's
precision (3.8° at worst, measured over the octahedral code), never more than the old half pixel, and a cover seen
nearly edge-on (|N·V| ≤ 0.25) still allowed the old two pixels; silhouettes kept only where no other part shows about
the point. **Tried and left out:** the edge's own two faces' exact planes from the posed vertices, the cover compared
with them on the pixel's ray (the first design, accepted by the manager and measured: Q58 was set on it); the faces'
planes carried a pixel off the edge put a cover "behind" them, so a rod's contour under a sheet was drawn
(`AContourUnderAnotherSurfaceIsHidden`, which the mutation run asked for, found it), and without them every humanoid
scene came out nearer Freestyle (Q60). **What it can't hide:** another surface seen nearly edge-on lying within about a
pixel's depth of the edge (its plane, carried even half a pixel, may be off by more than the gap, and the surface record
holds each sample's depth at the pixel's centre only), where the old two pixels stand; and an edge under a fold of its
own piece (the same island) within half a pixel, where its own sheet's old allowance stands. Neither shows in the 14
scenes or the six assembled looks.

**The measurement** (every 3D comparison of the suite logged before and with the final rule on both drivers;
`claude-work/gm2/ink-cover/before|final`, the per-scene table in `final/humanoid-table.md` and in `Docs/Renderer-3D.md`,
"Detailed figures"): the 14 humanoid scenes' worst, before → after: Freestyle's lines missed 2.50 → 0.09 %, extra lines
7.68 → 1.82 %, unlike ink 12.71 → 7.56 %, the mean coverage difference 0.134 → 0.093, the total ink 4.94 → 6.56 % off
(the woman geared from the side: the engine now draws less than Freestyle on the sword and the shield, where Freestyle
judges a long edge by its middle), the shading's stray pixels unchanged (23 inside, 88 on edges: the ink task drew
nothing in them). Missed lines, extra lines, unlike ink and the coverage difference fell in all fourteen scenes on both
drivers. Then the user's Q60 tightened the limits (Q58 had, on the first rule's numbers). **Every other comparison**
passes under its own unchanged limits; no shading comparison moved, and of the ink: nearer to Freestyle on both drivers
Ji'ruan's stage at its six times (extra lines 9.5–11.9 → 8.2–10.7 %, unlike ink 5.9–8.0 → 4.1–5.4 %, missed halved),
`cel-closeup`, `cel-shapes`, `gear-swing`, `figure-gear` (unlike 2.84 → 1.99 %), `ink-models`, `scene-objects`,
`shadow-map`, `terrain-map`, `terrain-oblique`; a little farther, each far under its limit: `model-blocks` (missed 0.02
→ 0.10 %, four pixels where an outline's gap at a corner widens; unlike 1.04 → 1.10 %), `figure-today` (missed 0.04 →
0.12 %, while its extra lines went 0.46 → 0), `cel-side` (unlike 0.04 → 0.08 %), `terrain-oblique`'s total ink (0.11 →
0.35 % off); `pose-side` unchanged (2.85 % extra on the GPU, 1.72 → 1.73 % on lavapipe). No golden changed.

**Tests** (engine): `InkCoverTests`, small scenes built in code with the lines drawn alone, each checked where its rule
decides and with a control that the line is drawn without what covers it: an edge under another piece of its mesh,
another mesh, another model or its mesh on another node, 2 mm over it (a quarter of a pixel's depth), hidden; a drum's
rim crease against its own faces at 6° drawn unbroken; a post through the plate draws no line where they meet; a flat
lock's contour lying on the plate drawn, in one primitive or across two; a round rod's sides drawn over its own curve;
a rod's contour 7 mm under a sheet hidden. `HumanoidReferenceTests` and the game's `AssemblyTests` under Q60's limits;
`EachWidenedLimitBites` with Q60's rows. Mutations 25, 20 killed, 5 that no test tells apart (accounted for in
`Docs/Renderer-3D.md`, Status).

### Step 8: the race carousel and the character maker (2026-11-16/17, the branch `gm2-step8`)

**Decided on the way.** The user (2026-11-16, the question tool, through the manager, on the approval set in
`claude-work/gm2/step8/approval/`: the captures at 1440 × 950 in both themes, the carousel beside the old picker in the
same state from its test client): the carousel and the maker **approved as captured** (Q51, Q52), and **a playable race
with no looks in the data shown but not choosable** (Q53; "Decisions", "Questions from the steps"). The manager
(2026-11-16): the step's choices accepted as listed under "Steps", step 8; the views' mask left to step 5 (GM2-P12, as
step 7 does); the "← All races" button given the room of the game's other small buttons (it hugged its text in the
approved capture 05: now 5 × 11 px of padding, the arrow 6 px from its words).

**The flow** (`Screens/Races/HeroFlow.cs`; Q25–Q28, GM2-7): name, race, maker, sheet. GM1's New character makes and opens
a hero with no race and no look; step 7's sheet asks for `open-races` on every open of a hero that **`HeroFlow.Needs`,
Q26's one rule** (made one with step 7's `SheetScreen.Ready` on the rebase, 2026-11-18, in `Ready`'s words; the sheet,
the carousel's and the maker's openings and their × all ask it): no race, or no look for a race the data has as playable;
a Dev Mode hero of an enemy-only race has none and needs none (Q27); a race the data hasn't (a mod's, switched off) can't
be told from an enemy-only one and gets the sheet. **A playable race the data has no looks for needs the flow too**
(Q26's words, "the sheet opens only with a race and a look"; the carousel shows that race not choosable, Q53, so such a
hero, who comes only from a file or a mod switched off, can leave only for the start screen until the looks are there):
before the rebase this step followed the old `needsAppearanceChoice` there (none needed), which Q26 and step 7's tests
don't. The commands, fixed by the manager: **`open-races [race]`** and **`open-maker <race>`** (this step's),
**`open-sheet`** (step 7's, asked at the end; `HeroFlow.SheetCommand` is `SheetScreen.OpenCommand` and the sheet's
`RacesCommand` the flow's). Both are possible only for a hero who still needs the flow, or in Dev Mode
(`HeroFlow.CanOpen`; outside it the look is final, GM2-7). Each screen hides itself before it runs the next one's
command (screens reach each other only through commands), and both hide when the character is left (`Session.Closed`;
a character opened while one shows was opened after the shown one was left). `HeroFlow.Finish` is the only save: the
race chosen as the old `selectRace` (`ActorRules.ChooseRace`: refused outside Dev Mode for an enemy-only race or a hero
who has another race; HP and Mana filled; a look for the hero's own race, the old "Use this look", chooses nothing again
and keeps them, as the old appearance step), then `GameSession.Change` with the sheet and the look together. Leaving (each
screen's ×, `HeroFlow.LeaveCommand`): `start-screen` while the hero needs the flow (Q26: it opens on the flow again, the
hero saved as it was), else `open-sheet` (Dev Mode's change given up); its label and tooltip say which ("Back to the start
screen", "Back to the character sheet"). `HeroData` is the game's data as the flow reads it (the races in their order,
`LookData`, `HasLooks` = a body of the race), read at each opening from the document's data (`GameUi.Document.Files`, the
application's; the screens' internal `Data` gives the tests theirs).

**The race carousel** (`Screens/Races/RacesScreen.cs`, `Assets/UI/styles/races.css`; Q28, Q36, Q41): a section on the page
itself as the start screen (no sheet under it, Q26), the old race dialog's look (`.spell-picker-dialog.race-dialog`, the
leather header "ANCESTRY", "Choose a *race*", the old intro line; the browser's "↻ Refresh file" not carried). One race at
a time: the dark portrait panel with the figure's `surface` (`#races-figure`), the emblem's medallion with its own
`surface` (`#races-emblem-view`) and the ✦ in the race's colour, the name, the kind ("Playable", "Selected" for the hero's
own, "Non-playable · Dev Mode"), the old list's eight modifiers ("STR +2" …, the old `formatValue`: `HeroTexts`) and the
passive's name and description in a well, "Choose <race>". ‹ and › (CSS chevrons: no font has to carry them), Left and
Right anywhere on the page while it shows and no window is over it (taken from the page; a key with a modifier, or one
the page took, does nothing), round from one end to the other; the strip of small medallions under the stage (the list
in order, a rule wherever it goes from playable to enemy-only and back, the shown one ringed, `aria-current`, a click
shows it; each names its race's emblem, `data-emblem`, for step 10's pictures). The list: the playable races, every race
in Dev Mode (Q27) in the races' order (Q43: the Merfolk last, after the enemy-only ones, as `races.json` has it),
following the setting while it shows. Its button: a playable race opens the maker (`race-chosen`; nothing saved yet); an
enemy-only race (Dev Mode) is taken at once with no look, saved, and `open-sheet` asked for (`race-chosen`); for another
race than the hero's own outside Dev Mode it is "Race locked", disabled, the old refusal on the status line ("A chosen
race can only be changed in Dev Mode", the error's look); for a playable race with no looks it is disabled with "No looks
for the <race> yet" (Q53). The focus: on the button at opening (on × when the button is disabled); lost when the button is
disabled under it, it comes back at the next race that can be taken. No race in the data: the old empty list ("No races
loaded.").

**The character maker** (`Screens/Maker/MakerScreen.cs`, `LookMaker.cs`, `Assets/UI/styles/maker.css`; GM2-1, GM2-6,
Q13–Q24, Q21, Q41): a section on the page itself, the old appearance step's words ("<RACE> · APPEARANCE", "Choose your
*look*", "← All races", the summary, "Begin as <race>" or "Use this look") in the dialogs' look, three columns: the presets
(men's then women's under "MALE" and "FEMALE", by their labels, the one the look started from marked; "No presets" for a
race without) and "Random look" under them; the dark portrait panel with the figure's `surface` (`#maker-figure`); the
choices: Sex and Build each in a line, then a row per category the body offers (Face, Brows, Hair, Beard, Crest, Ears,
Tail, Marks, Head piece, in `LookNames.Categories`' order) and, under "COLOURS", per slot the race has swatches for (in
`LookNames.Slots`' order, each with its swatch's disc), each row showing its choice and opening to its named options, one
row open at a time (the list scrolls within the panel's height when long). Under 1080 px the presets go above the figure
and the choices. A choice by click, or by the arrow keys among a group's options (the old step's: Right and Down the
next, Left and Up the previous, round, the focus following; the presets are groups too), `maker-choice` when it changes
the look and nothing else when it doesn't (GM1's press); the random button `maker-random` once a roll; Begin saves and
asks for the sheet (`hero-confirmed`), a refusal (Dev Mode turned off meanwhile, for another race) on its status line in
the old words with `ui-refused`; "← All races" runs `open-races <race>`; ×. The rows are made again on every change, the
focus kept on the same control (a `data-key` each). `open-maker` refuses with a log line and shows nothing for no race, a
race the data hasn't, an enemy-only race, a race with no looks (Q53) and a race `selectRace` refuses ("[Maker] open-maker:
<why>, nothing done").

**The maker's rules** (`LookMaker`, pure, on step 6's `LookData` and the race): the sexes and builds the race has bodies
for; the start: the hero's own look when its body is the race's (Dev Mode), else the race's first preset (`Preset` its
name), else a plain look (the first sex, its average build or else its first, each category's first option, each slot's
first swatch); none for a race with no body. A preset sets every choice; a changed sex keeps the build when that sex has
it (else its first), a changed build keeps the sex; either way each part the new body offers is kept and a category's
first option taken otherwise, the categories it doesn't offer dropped (a woman has no beard), the colours unchanged. A
part or swatch not offered is refused. The look's `Preset` is where it started, kept through later changes (the maker's
display: the list's mark, the summary's label). **The random roll** (Q21): one uniform draw a list in a fixed order: the
sex (over the sexes with a body), the build (over that sex's), each category the body then offers (in the maker's order,
"none" an option like the others: Q23, Q24), each slot (in the slots' order); no preset. Every look the maker makes
passes every check of `look.hero` with the data (G4), tested over the presets, each sex and build, and rolled looks.

**GM2-P15's helpers** (`Game/Statventure.Game/Random/RandomPicks.cs`): `Pick` (`items[Math.floor(Math.random() *
items.length)]`, one draw; an empty list refused before drawing), `WeightedIndex` and `PickWeighted` (the old engines'
form, `forge-engine.js` 305–310 with `encounter-engine.js` 929–938's weight rule: the roll a draw times the total, a weight
below 0 or not a number counted as 0, each taken off in turn, the first taking the roll below 0 picked, else the last).
The maker uses `Pick` only (no weights were asked).

**The views: stand-ins, and what replaces them** (GP-3: nothing of this ships). `IHeroViews` (`Screens/Races/HeroViews.cs`)
is the seam step 11 fills: `ShowFigure(surface, race, look)` (the carousel: the hero's own look for its own race in Dev
Mode, else the race's first preset, else none; the maker: the look on every change), `ShowEmblem(surface, race, emblem)`
(true when a live view shows: the medallion's ✦ then hides, `is-live`), `Hide(surface)` (each screen lets its views go
when it hides, and the carousel's when the list is empty). **In the program the screens have `NoHeroViews`: the figure
panels are empty (their dark colour) and the emblem shows the ✦ in the race's colour.** The approval captures drew the
engine's stand-in test figure (`figure.glb`, `stocky.glb` for a strong body, the look's skin and cloth on its two
materials) through a capture program's own `IHeroViews` (`claude-work/gm2/step8/capture/`), never in the program. **Step
11** replaces the views (the look's figure from step 5's builder on step 9's bodies and parts, idling in the fixed
three-quarter view, Q41; the live emblem, Q36; pausing when hidden; the memory and the cost measured, GM2-P16); **step 10**
the ✦ by the emblems' pictures (the strip's and, until the live one shows, the medallion's), found by `data-emblem`;
**step 9** the test data by the real bodies, parts, presets and swatches (no code change: the screens read the data).
**The shipped data today has no body**: every playable race shows "No looks for the <race> yet" and none can be taken, so
the program can't make a hero until step 4a lands the human's bodies (and step 9 the rest); Dev Mode's enemy-only races
can be taken.

**Sounds** (GM2-P17, step 12's names): `race-chosen` (a race taken, playable or enemy-only), `maker-choice` (a choice that
changes the look, a preset), `maker-random` (once a roll), `hero-confirmed` (Begin); browsing the carousel GM1's press for
its buttons, nothing for keys; a refused Begin `ui-refused`.

**Log lines** (for step 13): `[Races] the race carousel opened on <race> (<n> races[, Dev Mode])`, `[Races] <race> chosen:
its look next`, `[Maker] the maker opened for the <race> (<body>[, preset <preset>])`, `[Maker] a random look: <body>,
<parts>; <slot> <swatch>, …`, `[Maker] open-maker: <why>, nothing done` (a warning), `[Hero] <slot>: the hero is a <race>,
<sex>, body <body>` or `… a <race> with no look`.

**The screens' test data** (`Tests/Statventure.Game.Tests/Screens/HeroTestData.cs`; GM2-P18: nothing of the maker's is in
`data.pak` before its models): the committed `data.pak` **without its own looks** (since step 5 it holds the human's two
bodies, four parts and eight presets: `data/looks/` left out by a custom mount over the pack, so the tests and the
fixtures don't move as steps 9 and 9a fill the real data; one test of the carousel and one of the flow's data look at
the shipped data itself: the human the one race with looks, choosable, its maker on its eight presets) with a scratch
folder on top holding swatches for the human, the
Half-Beast and the lizardman (the crest's listed first: the maker shows the slots' order, not the race's) and the maker's
records, every name "Test …": the human's six bodies, eight presets (the old labels after "Test"), six categories (a
required face, brows and hair; a beard for men with "none"; marks and a head piece with "none"); the Half-Beast's ears
and tail (required) and a man's slender body before the average; the lizardman's crest; the dwarf's one body with a beard
and no swatches; the elf none (Q53's case). Models: empty `model` records at `models/test/figure` and `models/test/stocky`
(the checks ask only that one is there).

**Fixtures** (`Screens/HeroScreenFixtures.cs`, 15; GM1-P17, 12 levels / 0.2 %, as the main frame; on
`HeroScreensHarness`, the game's sheets asked in the game's order before the flow's, the other screens not attached):
the carousel (`races-playable`,
`races-locked`, `races-dev-mode` in both themes, `races-no-looks-light`), the maker (`maker-presets`,
`maker-category-open` in both themes, `maker-random-dark`, `maker-half-beast-light`) at 1240 × 900 (Dev Mode's
1240 × 1040) and 1480 × 900, and both at the smallest width (`races-smallest-window-dark`, `maker-smallest-window-light`, 820 × 1000). Every one passes at
1 and 1.65. Since step 5 every fixture masks the `surface` boxes (`GameScreenFixtures.Views`, GM2-P12): here the
carousel's figure (`#races-figure`) and its emblem's view (`#races-emblem-view`, which covers the medallion's inside and
its ✦ placeholder, held by `HeroViewsTests` instead) and the maker's figure (`#maker-figure`); the frames, the panels'
padding and borders round them and everything else still compared, and every fixture still passes as it did unmasked.
Each also through the real hidden window (`ProgramPathScreenTests`
picks them up). Found on the way, each kept to the fixture's side: (1) the headless compositor's pointer rests at its
output's middle (1200 device px), at 1.65 the page's 727 px: over the maker at 1240 px it lit a row's hover (1.67 % at
1.65, dark): the maker's fixtures are 1480 px wide, so it rests on the portrait; and over Dev Mode's carousel at 900 px
high it rested on a medallion of the strip, whose hover ring was in Chromium's references at 1.65 in both themes and
not in the engine's picture, under the limit (seen on the final recapture, 2026-11-17): Dev Mode's carousel is 1040 px
high, so it rests in the stage beside "Choose", and `HeroScreenFixturesTests` holds the premise for every fixture (each
drawn at a scale where the pointer falls on its page, with the pointer there and without it, pixel for pixel; at 900
px the two Dev Mode fixtures lit 27,412 and 37,832 pixels); (2) a focus a script moves after a mouse
press shows no ring in the engine (as Chromium), while PaintReference focuses with no input before (a ring): the
fixtures open rows and roll by the keyboard (focus, Enter); (3) the strip's 28 small rims in Dev Mode, the approved
1 px round borders, come out past the limit at 1, all along the small circles' outlines: the canvas antialiases a thin
round border on a small circle otherwise than Chromium (an engine finding: `Docs/Game-Plan.md`, "Engine items the port
will meet"). For a while they were drawn as inset rings (`box-shadow: inset 0 0 0 1px`), which passed, with the
enemy-only medallions' dimming (opacity .8) taken out in the same guess; beside the approved borders, both drawn by the
engine (`claude-work/gm2/step8/approval/medallions-compare-*.png`: the playable strip and Dev Mode's, both themes, at 1
and 1.65, enlarged 4×), the rings were the same at 1 (no pixel past 12 levels, the worst 8–12) but not at 1.65, where a
border's width is snapped to whole device pixels (1) and a shadow's spread isn't (1.65), in Chromium as in the engine:
the rims heavier and a dark seam between the shown medallion's two rings (2,238–5,908 pixels past 12 levels, the worst
66–125). **A look the user approved is not changed to make a comparison pass (the manager, 2026-11-17 and 18):** the
borders and the dimming are back exactly as approved (`eaea66b`'s strip rules); the comparison that failed was neither
masked nor loosened: an engine worker ported Chromium's painting of thin rounded borders on its own branch
(`gm2-round-borders`, "The engine fix it waits for is done" under "Steps", step 8), and this step merged after it. Before
the fix (on `c043ad3`, with step 5's view masks; `claude-work/gm2/step8/borders-numbers-c043ad3.txt`):
`races-dev-mode-light` at 1 2,949 pixels past 12 levels (0.229 %, the worst 81, the limit 2,579 of 1240 × 1040; 2,746
of them in the strip's band), dark at 1 2,125 (0.165 %), at 1.65 dark 1,402 and light 5,152 (the limit 7,021). **With
the fix** (`0eca9e5`, `numbers-0eca9e5.txt`): light at 1 **160** (0.012 %, 85 in the band), dark at 1 136 (0.011 %, 91),
at 1.65 light 1,985 (0.057 %, 160 in the band) and dark 301 (0.009 %, 170); the goldens (Chromium's) the same bytes;
every one of the step's 30 comparisons under its limit, the most 0.124 % (`races-locked-light` at 1.65); (4) with the page scrolling (820 × 600) the only differing pixels were the root scrollbar's thumb, about 15 px
shorter in the engine than in Chromium for a page 974 px high (both lay it out 974 px high; `maker-smallest-window-light`
3,854 pixels past 12 levels at 1, 0.78 %, and 12,784 at 1.65, 0.95 %, all in the scrollbar's column): an engine finding,
listed and not fixed (the same list), the smallest-window fixtures made 1000 px high instead.

**Tests** (the game's; the counts in "Runs"): `RandomPicksTests` (the picks draw for draw on a scripted source, their
edges and refusals, a uniform spread), `LookMakerTests` (what a race offers, each start, each choice and refit, the roll
draw for draw, every look made passing its checks), `HeroTextsTests` (`formatValue` and the modifiers, the rows' values
from the old function in Node; the refusals' words against the oracle's `sheet-actions` chooseRace cases; the screens'
words), `RacesScreenTests` (`CheckNewRacePicker`: the three new races offered with their emblems, as
`new-races-browser.js` checks the seals; the list and its content, browsing, Dev Mode and the setting followed, the lock,
Q53, taking a playable and an enemy-only race, ×, hiding, who may open it, no races), `MakerScreenTests` (the opening,
presets, rows, sex and build, the arrow keys, the focus kept, the roll, Begin and what it saves, "Use this look", a
refusal, "← All races" and ×, who may open it for which race, no presets, no swatches), `HeroFlowTests` (name, race, maker,
sheet from GM1's New character on its real start screen; an unfinished hero and a GM1 character opening on the flow
again; `Needs`; the flow's data; the commands; `Finish`'s refusals and its choosing again), `HeroFlowLogTests` (the log
lines, and an arrow key off a group's options failing nothing; in the "Game log" collection), `HeroViewsTests` (what each
surface is asked to show and when it is let go), `HeroSheetsTests` (the sheets' rules under the comparisons' limits, as
computed, Dev Mode's dimming among them), `HeroScreenFixturesTests` (the compositor's pointer lights nothing in a
fixture), and the 15 fixtures in `GameScreenTests` and `ProgramPathScreenTests`. The whole flow (`HeroFlowTests`' first
two) runs on the game's own start screen and step 7's sheet (`HeroScreensHarness(sheet: true)`): New character, the
sheet asking for the carousel and staying hidden, the maker, Begin, the sheet shown with the hero; Continue on an
unfinished hero opening the carousel again; the other tests keep the sheet's stand-in, and step 7's tests keep theirs
for the flow (`ScreensHarness.NewHero`, beside the real one). **No GM1 test changed** by this step. Of step 7's, one
line: its test of Q26's rule asks `HeroFlow.Needs` (the same cases). The engine's `UiResourcesTests.Stylesheets` names the two new sheets; `ui.pak` 20
entries.

**Shown able to fail** (`claude-work/gm2/step8/`: `make-mutations.py` writes `mutations.json`, run in two halves,
`mutate.sh`; the sheets' `make-css-mutations.py`, `css-mutate.sh`, `ui.pak` made again for each). **The code: 174, all
killed** (the step's functional tests only, so none rests on a golden): the picks (their arithmetic, edges and refusals),
the maker's rules (what is offered, each start, each choice and refit, the roll's draws and order), the words, the flow
(`Needs`, `Refusal`, `Finish`, leaving, who may open), the game's two screens attached, the carousel (the list, Dev Mode,
the rule, the strip, the kind, the lock, Q53, the focus, the keys and their refusals, taking a race, hiding, the views) and
the maker (its refusals at opening, the rows, the presets, the choices, the keys, Begin and its refusal, "← All races",
×, the views). The first run: 87 of the first half, 79 killed, 6 survived, 2 rewritten (they didn't build); the second:
those 8 again and the other 87, 92 killed, 3 survived; each survivor a gap, closed and killed in the last run
(`mutations-rerun.json`, 11 of 11): the weighted pick's NaN and below-0 weights in the roll and the total; a start look
whose sex disagrees with its body (a slot's save opens unchecked); an enemy-only race with a body still needing no look
(an orc's body in the test data); a race with an emblem followed by one without; the focus fixup's blur a task later
before the focus comes back; a taken key on a build that would change; a focus off a group's options (the handler now
passes it by, `at < 0`, where it had divided by an empty group's count: a logged listener failure the tests now look for);
the maker's × for a finished hero. **The sheets: 14, all killed**: 9 by the screen comparisons; 5 under their limits (the
stage's chevron, the strip's ring, a swatch's colour, the chosen preset's ring, an open row's chevron) killed by their
computed values (`HeroSheetsTests`). With the dimming back (2026-11-17) the list was 16 (`css-dimming.json`: the
strip's ring again and the dimming's two rules, the enemy-only medallions and the shown one, killed by their computed
values), with the borders back (2026-11-18) **17, all killed** on step 5's `main` (`c043ad3`, `css-mutations.json`):
the shown medallion's border in its race's colour added and the ring's strings the approved rule's, both held by their
computed values, and the stage's chevron, the swatch's colour, the chosen preset's ring and an open row's chevron on
theirs too (under the limits, as before: on step 7's `main` the chevron's had been "killed" by `races-dev-mode-dark` at
1 standing at 0.198 % before step 5's masks); while the border fix wasn't on `main` the screens' filter left
`races-dev-mode-light` out (it failed at 1 unmutated, so it would have "killed" every mutation); **on the fix
(`0eca9e5`) with it back in: 17 of 17 killed** (7 by the screens, 9 by `HeroSheetsTests`' computed values, the live emblem's placeholder by `HeroViewsTests`; the
race's kind in capitals moved to a computed check there: with every comparison far under its limit after the fix, its
few hundred pixels of small text no longer reach the screens' 0.2 %). The pointer's premise broken (Dev Mode's carousel back at 900 px) is killed by
`HeroScreenFixturesTests` (`mutations-pointer.json`). **Q26's one rule** (on step 7's `main` and again on step 5's,
`mutations-q26.json`, the step's tests and step 7's `SheetScreenTests`): **10 of 10 killed**: an empty race taken for a race, an enemy-only race or
a race the data hasn't needing a look, a hero with a look needing the flow, the flow's data without the races, the sheet
asking the rule the other way or without the races, leaving the other way, Dev Mode or the rule not opening the flow
(the code's list is 177 now, `make-mutations.py`, every old text found on the rebased tree). Nothing written to the player's real folders.

**Runs** (headless, scratch per-user folders, nothing audible; on `main` `01d256d`, with steps 7 and 5, the round
borders and the ink's cover merged): the full suite on the GPU (388 s with the build) and on lavapipe (457 s): **4472 +
948 = 5420 tests** (`main`: 4472 + 796 = 5268; 152 new, all the game's), none held or skipped. The step's 30 comparisons
recaptured on the round borders' `main` (`0eca9e5`): the same bytes, every one under its limit. The player's real
folders unchanged by every run (`claude-work/gm2/step8/real-folders.txt`).

**For later steps.** *Step 7 (the sheet), done on the rebase:* the sheet asks `HeroFlow.Needs(save, context)` (Q26's
one rule) on every open and on `open-sheet`, runs `open-races` for a hero who needs the flow, and the seal's Dev Mode
change runs it too. *Step 11:* implement `IHeroViews` and give it to both screens (their `Views`); the surfaces are `#races-figure`,
`#races-emblem-view`, `#maker-figure`; the carousel asks for the race's first preset (none: no preset, an enemy-only race),
the maker for every change; `Hide` comes when each screen hides; add the views' mask (step 5) before a view draws in a
fixture. *Step 10:* the pictures by `data-emblem` (the strip's buttons, the medallion `#races-emblem`); the medallion's ✦
hides once `ShowEmblem` returns true. *Step 9:* real records replace `HeroTestData` with no code change; the category and
slot names the maker shows are `HeroTexts.Category` and `Slot`. *Step 13:* the commands and log lines above; the flow
needs bodies in the data to finish a playable hero.

**Choices made** (internal; recorded here, the player-facing ones accepted by the manager): the shared flow as `HeroFlow`
and `HeroData` in `Screens/Races/`; the data read from the document's data at each opening (no cache), a seam for the
tests; Q26's rule one with step 7's in `HeroFlow.Needs` (its words: a playable race with no looks needs the flow; **the
manager's reading of Q26, 2026-11-19:** a hero with a race but no look needs the flow even when that race has no looks
in the data, as follows from the user's Q26, "the sheet opens only with a race and a look", and Q53; the old game's
`needsAppearanceChoice` opened the sheet there, a difference recorded as such); the views as a
seam (`IHeroViews`), not a stand-in model in the game's packs; the old success line ("<race> selected") not shown (the
screen hides at once, as the old dialog closed); the rows made again on each change with the focus kept by a key; the
fixtures' widths for the compositor's pointer, their rows opened by the keyboard; the weighted pick refusing an empty list.

### Step 4b: the first humanoid's clip library in the repo (2026-11-18, the branch `gm2-step4b`)

The 34 clips the manager keyed on the human man and the user approved (the hand-over, `claude-work/gm2/step4/
handover-clips/`: `HANDOVER.md`, `list.txt`, `clips/`) in the family's library and `models.pak`, held to Blender on both
bodies and to the rules they were keyed by; the figures' small state machine; the figure mode playing the clips in the
game's look. On the PC only for what runs Blender (the pinned 5.2.0); the tests run anywhere. Decided on the way: the
user's Q56, Q57 and Q59, the manager's GM2-P21–GM2-P24, the ground and the clips' ease ("Decisions").

**The data.** `Tools/Models/humanoid/clips/<name>.json`, the 34 files copied from the hand-over byte for byte (their sha256
the manager's `checksums.txt`; `claude-work/gm2/step4b/checksums-*.txt`), library.py's form (step 4a), unchanged by this
step. They came twice: the first set measured here (the ground, the in-betweens, below), the manager's re-bake of the same
day the final one (24 files changed; `death` and `meditate` approved again on a floor). **GM2-P23:** the keying scripts
(`handover-clips/kit/`) come into the repo in a task of their own; until then these files are the clips.

**The library.** The manifest's `clips` entry: `clips.library:library` with no folder (its own, `clips/`) into
`Assets/Models/humanoid/clips.glb` (757,796 bytes: 31 nodes, the man's skeleton and its sockets, no mesh; 34 animations,
each its markers as events), keyed in about 3 s by library.py as step 4a made it; `make-humanoid.sh check clips` the same
bytes, twice. `models.pak` (`make-models-pack.sh`) holds it at `models/humanoid/clips` beside the ten figure models and the
state machine: 12 entries, 2,171,138 bytes. The frame's test clip `rest-sway` stays the tests' data, never packed.

**The clip set** (the hand-over's list; frames at 60 fps; "loop": its last key its first; the markers are the clip's events
at those frames; what plays it from the hand-over and M7's list):

| Clip | Frames | Seconds | Loop | Markers (frame) | Played for |
|---|---|---|---|---|---|
| `idle` | 120 | 2.00 | yes | | standing in battle, every weapon kind (a stance loop laid over its arms, GM2-P21) |
| `stunned` | 90 | 1.50 | yes | | stunned |
| `advance` | 28 | 0.47 | | step 11, step2 18 | the lunge to a target (the battle moves the figure, no root motion) |
| `return` | 28 | 0.47 | | land 19 | back to its place |
| `flee` | 32 | 0.53 | yes | step 0, step2 16 | fleeing |
| `stand` | 240 | 4.00 | yes | | the sheet's, the maker's and the carousel's figure, relaxed (Q57; not a battle clip) |
| `hurt` | 24 | 0.40 | | | hit |
| `critical-hurt` | 44 | 0.73 | | | hit critically |
| `stumble` | 46 | 0.77 | | step 8 | stumbling |
| `death` | 60 | 1.00 | | fall 26, down 34 | dying; its last frame held |
| `swing` | 46 | 0.77 | | hit 17, step 15 | one-handed (sword, axe, mace, club, hammer; the stance is the clips' own) |
| `thrust` | 40 | 0.67 | | hit 14, step 13 | one-handed thrust (spear, trident, rapier) |
| `stab` | 30 | 0.50 | | hit 9, step 8 | dagger |
| `strike` | 48 | 0.80 | | hit 18, step 16 | staff or wand, from `hold-staff` |
| `punch` | 32 | 0.53 | | hit 9, step 8 | unarmed (the Half-Beast's claws too), from `hold-unarmed` |
| `swing-off-hand` | 40 | 0.67 | | hit 13, step 12 | a second weapon in the left hand |
| `swing-two-handed` | 48 | 0.80 | | hit 17, step 15 | two-handed (greatsword, great axe, spear), from `hold-two-handed` |
| `bow` | 70 | 1.17 | | shoot 36 | bow, from `hold-bow` (the string held to frame 34) |
| `crossbow` | 58 | 0.97 | | shoot 26 | crossbow, from `hold-crossbow` |
| `cast` | 54 | 0.90 | | cast 24 | casting a spell |
| `skill` | 50 | 0.83 | | skill 17 | a class skill |
| `item` | 46 | 0.77 | | use 23 | using an item |
| `drink` | 76 | 1.27 | | drink 32 | drinking a potion |
| `meditate` | 86 | 1.43 | | restore 42 | meditating (a kneel on the right knee) |
| `guard` | 14 | 0.23 | | guard 9 | entering guard, then `guard-hold` |
| `guard-hold` | 120 | 2.00 | yes | | guard held (the shield fully before the body) |
| `evade` | 30 | 0.50 | | leap 6, land 15, evade 19 | entering evade (the battle moves the figure 0.85 m back between `leap` and `land`), then `evade-hold` |
| `evade-hold` | 96 | 1.60 | yes | | evade held |
| `victory` | 68 | 1.13 | | cheer 14, land 22 | victory |
| `hold-two-handed` | 120 | 2.00 | yes | | the two-handed stance, the arms alone (GM2-P21) |
| `hold-staff` | 120 | 2.00 | yes | | the staff's or wand's stance, the arms alone |
| `hold-unarmed` | 120 | 2.00 | yes | | the unarmed stance, the arms alone |
| `hold-bow` | 120 | 2.00 | yes | | the bow's stance, the arms alone |
| `hold-crossbow` | 120 | 2.00 | yes | | the crossbow's stance, the arms alone |

Every whole-body clip turns the trunk, the head, both legs and both arms and moves the hips (19 to 23 bones); the five
stance loops turn the 12 bones of the arms (shoulders, upper arms, forearms, hands, fingers, thumbs) and move nothing. 23
clips have a key at every frame and are linear (the manager's choice, "Decisions"), the 11 sparse ones Bézier.

**Held to the hand-over's list and the keying's rules** (`Tests/Statventure.Tests/Humanoid/HumanoidClipTests.cs`; the list
as a table, `HumanoidClipList`, so a clip added, dropped or renamed, or one whose length, loop, bones or markers move,
fails until the list changes with it): the library holds the list's clips and no others, as the folder does, each its
length, its markers as events at their frames, its bones turned, no scale, a location on the hips alone (none in a stance
loop), each file's own length, loop and markers the list's; a loop's last key is its first, in its file (as library.py
checks) and in the library (every channel's sample at its end its start's: the angle computed in double with an atan2, as
an arccosine of float32 keys reads 0.02° where nothing differs); the stance loops turn the 12 arm bones alone and every
other clip the whole body; `guard` ends where `guard-hold` begins and `evade` where `evade-hold` does (measured 0.097° and
0.369°, the left upper arm, the hips to the micrometre; **bounds 0.4° and 0.1 mm**); a stance laid over the idle turns the
arms as the stance does and leaves the rest to the idle, on both bodies (below, "The figure mode").

**The ground** (`TheManStaysOnTheGround`, since GM2-P22's task `BothBodiesStayOnTheGround`; the manager, 2026-11-18): the ground is where a body's soles are at rest, its
lowest point there, **24.0 mm under the plane of its origin on both bodies**: the bare feet stand on the origin's plane
(the skin's lowest +0.5 mm) and the boots' soles reach 24 mm under it (`outfit.boots` displaces the sole to −14 mm, then
its 7 + 3 mm shell). So a figure must be placed with its soles on the ground, not its origin (GM2-P22, "the figure's
ground"). On the man, skinned on the CPU as `skin.comp` skins, at every frame of every whole-body clip: **at most 2.21 mm
under the ground** (meditate's kneeling knee at frame 22), 2.00–2.01 mm in the clips whose feet the re-bake raised, the
rest under 1.6 mm; death's last frame 1.92 mm under it (heels, thighs, fist and head on it). **Bounds: 3 mm under; death's
end from 3 mm under to 5 mm over** (the manager's). Measured first on the hand-over's first set (its numbers sent to the
manager, who re-keyed): meditate's boot toe 68.8 mm under (the knee hanging 16 cm over the ground), victory 25.3, the
attacks' and advance's front toe 9 to 11, death's fallen body 66 mm over the ground. **The woman** (no bound, her legs
shorter and a clip's hips' place not scaled yet, GM2-P22): her standing frames about 4.0 mm under her soles' rest level
(idle 4.02), the attacks 6 to 8 mm (swing-two-handed 7.81, thrust 7.71, swing-off-hand 7.59, swing 7.18), stumble 8.14,
meditate 18.14 and **death's fallen body 24.11 mm under at its end** (the thigh): what GM2-P22 must scale.

**Held to Blender on both bodies** (`AnimationScenes`: `humanoid-<clip>-male` and `-female`, 68 scenes, the library played
by bone names on each body as `rest-sway` is; `make-blender-references.sh` evaluates them, about 5 minutes, 26 MB of
`Golden/anim-humanoid-*.json`): at times lighter than `TimesFor`'s (every other frame and the last, each marker's frame,
the middle of every other frame, one before and one after: 4,962 in all; `EveryClipIsHeldToBlenderOnBothBodiesAtItsMarkers`
holds that every marker's frame is among them and that Blender's reference is at those very times). **On frames within
0.0048 mm** (limit 0.02), **between frames at most 0.98 of Animation.md's allowance** (death's left upper arm at frame 3.5,
Bézier; the 23 linear clips at most 0.08, 0.002 mm). The markers are Blender's pose markers (`ClipEventsAreTheActionsMarkers`). **The
in-betweens** (found here, the manager's choice): on the first set, keyed at every frame with Bézier keys, 44 of the 68
scenes were over the allowance by 1.01 to 2.42 times (strike 2.42, crossbow 2.26, bow 1.86, swing-two-handed 1.84 …; the
largest error 42 mm at swing's right elbow mid-frame within an allowance of 77): Blender's auto-clamped handles flatten
each quaternion component at its turning points, so its curve bends inside a frame, where the engine draws the exported
frame samples straight. The game only sees the frame samples; written linear, Blender's in-betweens are the engine's and
the rule holds unchanged.

**Posed figures against Blender** (`HumanoidReferenceTests`, six scenes beside step 4a's 14, written into
`Reference/humanoid-scenes.json` by the figure mode, the clip played twice in turn so its last frame is a meeting, not the
cycle's start; rendered by `make-blender-references.sh`): the man geared (chain shirt, sword, shield) in guard-hold (frame
60, three-quarter), at death's last frame (from the side, 720 × 400) and in evade's leap (frame 11, the side), in the chain
shirt with empty hands (their weapons are GM3's) in swing-two-handed with the arms overhead (frame 12, three-quarter) and
in bow at full draw (frame 34, the side, 480 × 480); the woman geared in `stand` (1 s, three-quarter). All under
`ReferenceLimits.DetailedFigure` as main has it after the ink's engine task (the user's Q58 and Q60: M6's with inside one
in 4,200, edge one in 1,100 and unlike 8.3 %), on both drivers (GPU / lavapipe; measured 2026-11-19 on `01d256d`):

| Sample | Inside | Edge | Edges' mean | Missed | Extra | Unlike | Coverage |
|---|---|---|---|---|---|---|---|
| man, swing-two-handed overhead | 4 / 10 | 118 / 111 | 2.8 | 0.08 % | 0.91 / 0.96 % | 1.30 / 1.32 % | 0.080 |
| man, bow at full draw | 9 / 8 | 93 / 87 | 2.5 / 2.6 | 0.05 % | 0.83 / 0.87 % | 1.68 % | 0.081 |
| man, guard-hold | 20 / 20 | 137 / 136 | 2.7 | 0.02 % | 0.44 / 0.46 % | 1.44 % | 0.080 |
| man, death's last frame | 13 / 14 | 108 / 97 | 2.9 | 0.14 % | 0.35 / 0.36 % | 7.06 / 7.08 % | 0.096 |
| man, evade's leap | 3 / 2 | 76 / 80 | 2.6 | 0.08 % | 0.33 / 0.34 % | 2.45 % | 0.081 |
| woman, stand | 18 / 24 | 77 / 75 | 2.5 / 2.6 | 0.21 % | 0.20 / 0.21 % | 4.44 / 4.49 % | 0.084 |
| limit (`DetailedFigure`; at 172,800 px) | 1 in 4,200 (41) | 1 in 1,100 (157) | 4 | 2 % | 3 % | 8.3 % | 0.12 |

(Before the ink task, on `c043ad3` under Q55's limits, the same shading and inside numbers, the ink farther: death's last
frame's unlike ink 11.1 % and coverage 0.120, the others' unlike at most 6.4 %.) The nearest to its limit is death's
unlike ink (7.06 % of 8.3 %).

**Owed (GM2-P24):** the geared man and woman at the swing's hit (frame 17, three-quarter, 360 × 480) pass every measure
but the edge pixels' (the man 178 on the GPU and 179 on lavapipe, the woman 160 and 155; the limit 157; measured again
after the ink task, the same: the shading's, its ink now well inside, unlike 2.1–2.3 %), about 80 of them in one place,
the chain shirt's skirt at its front in the lunge, where the edge between its lit and shadowed band runs a few pixels
apart; suspected, not proven: `normals.comp` recomputes a posed mesh's normals per primitive where Blender's evaluated
mesh averages across the object, and the fold lies at the mail's seams. Left out of the tests and references; their
scenes, Blender's references, the engine's renders on both drivers (before and after the ink task), the difference
pictures, the measures and the steps to add them back are in `claude-work/gm2/step4b/pending-samples/` for GM2-P24's task.

**The figures' small state machine** ("GM5 builds the battle's state machine, GM2 a small one for its figures"; Q57):
`models/humanoid/machines/figure` in `models.pak`, from its JSON form `Assets/Models/humanoid/machines/figure.json`
(Docs/Assets.md's `animation.state-machine`, the serializer's own form): named `figure`, no parameters, one layer `base`
(Replace) entering `stand`, its one state `stand` looping the clip `stand` at speed 1, no transitions. Step 11's live
figures (the sheet, the maker, the carousel, Q31, Q41) play it: `StateMachineDefinition.Load(data,
"models/humanoid/machines/figure")`, an `AnimationStack` on the body's skeleton, `new StateMachine(definition, stack, name
=> new ClipBinding(clip of models/humanoid/clips by name, skeleton))`, an `Animator(stack, machine)`. `HumanoidMachineTests`:
it loads from `models.pak` mounted as the game mounts its packs (M7's rules), its content, its file the record's own JSON
form, every clip it names in the library, and on both bodies an animator with it poses the body as `stand` at the time
round its 4 s, past the first round, the clock in uneven steps. The family's machines live in `machines/` beside the
models (`HumanoidModelTests` names every file of the folder: a manifest model or a machine).

**The figure mode** (GM2-P14; `Samples/Statventure.Sandbox/Figure/`): **`--play <clip>[+<clip>][:times],…`** (`FigurePlay`,
`FigureSpec.Play`): the clips to play in turn instead of the library's every clip once, each turn its clip `times` in a
row (one strip of that many repeats, its time round the clip as Blender's strip counts it, its very end the clip's end), a
`+<clip>` laid over it on a layer above for the turn's length (Replace, `Extrapolation.Nothing` outside it), named
`<clip>+<clip>` in the caption and the dump's index; a name the library hasn't is refused. **`--label`**: a dump's frames
name the clip, its time and the view in the caption (`FigureSandbox.Label`; without it a dump stays the view's picture
alone). **A stance over the idle needs no engine code:** a stance loop keys the arms alone, so the layer above changes the
arms and leaves the rest to the idle (Blender's blend domain); held bone by bone to the two clips on both bodies
(`AStanceLaidOverTheIdleTurnsTheArmsAlone`); laying one over clips by a bone mask is GM5's (GM2-P21). Tests in
`FigureModeTests`: the play list's turns, repeats and laid-over clip on the sample against the stack, the view playing
it, the command line, the label's dump (the caption's box alone differs from the plain dump, its words there) and a play
list's dump index.

**The playing view** (the user's, GM2-P14; `claude-work/gm2/step4b/playing/`, its `LIST.md` says what is where): the man
and the woman in the chain shirt with the sword and the shield playing the one-handed set in turn (31 s), in the chain
shirt with empty hands each stance over the idle then its attack, with stab, swing-off-hand, item and drink (26.6 s), and
`stand` in the plain outfit (8 s); each from the battle's side and from three-quarter, 1200 × 800, 30 fps, labelled:
`playing.mp4` (`play-figure.sh --dump`, headless, a cage of its own). **Approved by the user on 2026-11-19 ("Yes, it plays
right"), the last approval stop of step 4.** In a window: `Tools/Scripts/play-figure.sh
Assets/Models/humanoid/bodies/human-male-average.glb --clips Assets/Models/humanoid/clips.glb --look ember-hearth --garment
<face> --garment <hair> --garment <chain shirt> --item Assets/Models/humanoid/gear/sword.glb@socket.hand.R --item
Assets/Models/humanoid/gear/shield.glb@socket.shield.L --play '<list>' --view side --size 1200x800` (the full command in
`LIST.md`). **Its cost** (a scratch probe, the figure mode's view of the geared man playing `swing` from the side, medians
of 200 frames, the machine shared with other workers): 36,258 vertices, 57,546 triangles, 23 primitives (draw items), 17
material slots, 30 joints (23 deforming, 7 sockets) in 6 models; posing 0.10–0.35 ms of CPU a frame; `View3D.Update`
(render, submit, wait) **0.40 ms at 600 × 800 and 0.48 ms at 1200 × 800 on the GPU**, **29.7 and 45.3 ms on lavapipe**.

**Shown able to fail** (`mutate.py` under the shared lock in a headless cage; `claude-work/gm2/step4b/mutations.json`, made by
`make-mutations.py` beside it): **54 mutations, 54 killed.** The library swapped for a broken one (each keyed by
library.py from the clip files with one rule broken, `make-breaks.py`: a loop's end not its start, a marker a frame late,
a clip a frame longer, one dropped, one added, a stance loop moving the hips or turning the chest, a whole-body clip
leaving a foot alone, guard's end 1° and evade's 1 mm from their holds, advance's feet 8 mm into the ground, death's body
8 mm over it): 12; the list (a clip renamed, dropped, a frame longer, a marker late, a loop not looping, its bones): 6;
the clip files (a loop's last key, a marker, the length, the loop flag): 4; the scenes Blender evaluates (no time at the
markers' frames, the man's body twice): 2; the state machine (`models.pak` built with it holding, playing the idle, at
1.5 times; its file not looping): 4; the manifest without the library: 1; the posed samples with the clip once: 1; the
figure mode's play list, layers, names, command line, caption and index: 24. Nothing written to the player's real folders.

**What moved beside.** `HumanoidFiles.Names()` holds `clips` (`Figures()` leaves it out); `HumanoidPackTests` expects the
library and the machine in `models.pak` (the test clip still out); the editor's cutscene preview offers the game's clip
library in every clips role, so `CutscenePreviewTests` names it (`GameLibraries`), as step 4a's figures are named in the
model roles; the editor's fixture `preview-cutscene` lists it among its role's options (`Reference/editor-fixtures.json`),
its Chromium goldens captured again the same bytes (a closed select shows its chosen option only); the editor's entry
list is unchanged (`models/humanoid` stays folded).

**Runs** (headless, scratch per-user folders, nothing audible): the full suite on the GPU and on lavapipe on `main`'s
`1768286` (the round borders, the ink's task, step 8 and a docs-only plan merge), **4645 + 948 = 5593 tests** (`main`:
4472 + 948 = 5420; 173 new, all the engine's), none held; before, the same on `c043ad3` (4598 + 796, both drivers), on
`01d256d` (4645 + 796, the GPU) and on `5857ff2` (4645 + 948, the GPU); `make-humanoid.sh check` 19 of 19 the same bytes,
twice; `models.pak` the same bytes three times (before and after the rebases); `make-blender-references.sh --out` with
`compare_references.py`: **240 of 240 byte-equal** (the 68 clip evaluations and the six posed scenes among them); the six
posed samples measured again on both drivers on `01d256d` (the table above; nothing of the engine, the models or the
references moved after it).

**Choices made (internal, recorded here):** the library's manifest name `clips` and the machine's place
`models/humanoid/machines/figure`, named `figure`, its layer `base`; the clip list as a C# table in the tests; "the whole
body" as the trunk, the head, both legs and both arms' upper arms and forearms with the hips moved (every clip keys
more); the ground the soles' rest level (the manager's); the lighter time set for 68 scenes; the posed samples' views,
sizes and frames, the empty hands for the clips whose weapons are GM3's, the clip played twice in a sample so its last
frame is a meeting; `--play`'s syntax (`:` for the times, `+` for the clip laid over) and `--label`; the playing view at
1200 × 800 so a lying body stays in the frame, each weapon's stance shown over the idle before its attack.

**For later steps.** *Step 11:* the machine above with the library `models/humanoid/clips`; a figure placed by its soles
(GM2-P22: until that task, the soles are 24 mm under a figure's origin). *GM2-P22's task:* the woman's numbers above (her
fallen body 24 mm under); then Q59's check on the hobbit's and the Goliath's bodies in step 9. *GM2-P24's task:* the two
owed samples. *GM5:* the stance loops' mask (GM2-P21), `evade`'s move between `leap` and `land`, the battle's machine
beside `figure` in `machines/`. *GM3:* the bow's string and arrow; the weapons the clips were shown with.

### The figure's ground: a clip's hips by the body's legs, a figure by its soles (2026-11-20, the branch `gm2-figure-ground`)

The engine task of GM2-P22 as the manager widened it on 2026-11-18 (`Docs/Game-Plan.md`, "Engine items the port will
meet"), before step 11's live figures. Two rules, each the same on the engine's and on Blender's side; the rule and its
code in `Docs/Animation.md`, "How it's done" ("A clip's translations follow the body's size", "A figure stands by its
soles"). Every choice here is internal (none changes what the player sees beyond putting the feet on the ground); none
was asked.

**A clip's hips by the body's legs.** A clip's only translation is the hips' place, metres keyed on the man; its legs are
rotations. Played on a body with other legs, the hips moved by his centimetres over legs that fold by its own, so the
feet sank or floated. Now a clip carries its **measure**, the chain of joints whose rest length measures a body for its
translations, and a `ClipBinding` multiplies every bone's sampled place by that chain's length on the playing body over
its length on the body the clip was keyed on. *The measure:* the left leg, `thigh.L` → `shin.L` → `foot.L`, its length
the hip joint to the knee plus the knee to the ankle at rest (the bones that fold under the hips; the bodies are
symmetric, the right leg the same to the micrometre). Taken over the hip joint's height (it carries the ankle's height,
which doesn't fold) and over the hips' own height (it carries the pelvis): on a body built as the man scaled they all
give the same ratio, and on the woman they differ by under 0.2 % (under 2 mm on death's fall), the chain naming the two
bones that fold. The man 0.81515 m, the woman 0.78918: **0.968**. *Where the measure is:* the keyed body's length needs
no new data (the library's `.glb` carries the man's skeleton, so `AnimationClip.From` measures the chain on it), but
which joints measure a family does: `model.animation` version 2's `measure`, from the animation's extras (library.py sets
each action's custom property `measure` to its `MEASURE`), because the engine is the family's and not the humanoid's: a
rule by bone names would also have scaled M7's test figures' clips on the stocky figure (its legs 0.64 of the figure's)
and so changed references that are not this task's; with the measure in the data, a clip without one (M7's, Ji'ruan's
acting, an item's own clips) plays as before, and a family whose rig has no legs names another chain or none (then it
plays unscaled). *Exact and free on the man:* the same skeleton numbers give the same length, the ratio is exactly 1 and
the product is skipped, so his poses are his clips' samples to the bit (`TheManPlaysTheClipsExactlyAsKeyed`) and his 34
evaluations against Blender and `rest-sway`'s on him are the same files. *Everywhere a clip is bound:* layers, fades,
the state machine, sequences, the editor's preview, and the gear (garments and items follow the posed bones). Blender's
side (`Tools/BlenderReference/clip_measure.py`): the reference evaluation (`evaluate_animations.py`, the 68 scenes and
`rest-sway`'s) and `render.py --clip` (so `assemble.py`) multiply the action's location keys and their handles by the same
ratio, measured on the armatures or the `.glb`s' rest; 1 on the man, nothing touched.

**A figure by its soles.** A body's ground is its lowest point at rest, measured when it is loaded: `ModelData.LowestAtRest`
(the meshes skinned at rest as the renderer skins them) of the body and the garments it wears, its rigid items left out,
to the micrometre (`Actor.GroundOf`): −23.997 mm on both human bodies (their booted soles). *Measured, not stored:* no new
data and no format change, it always agrees with the model's shape, and a bare sole, a claw or a hoof is whatever reaches
lowest (made-up feet in `BareSolesClawsAndHoovesStandOnWhatReachesLowest`); a garment reaching lower (boots made apart, a
long robe) sets it. *Placed by it:* `Actor.Ground` (default 0: nothing changes for any other actor) raises the figure's
model space by its negation (`Actor.Placement`), so the node an actor hangs on is where its soles stand; its gear and its
hatching's anchor go with it. The game's `HeroFigure` sets it (every preset and the geared man: −23.997 mm); the figure mode
sets it and frames the figure as it stands (its camera raised with it: the same picture), writing the scene for Blender
raised alike; Blender's side, `render.py`'s `figure_ground` (so `assemble.py`'s), raises its figure by the same rounded
number and anchors its hatching there. GM2's screens have no ground and frame their figures, so no figure moves in its
view; no game screen's golden changed (the sheet's, the maker's and the carousel's views draw nothing until step 11).

**The woman's ground** (`BothBodiesStayOnTheGround`, the step 4b test now bounding both bodies; her lowest point, skin and
plain outfit skinned on the CPU at every frame of every whole-body clip, under her soles' rest level):

| Clip | The woman before (mm under, frame) | After | The man |
|---|---|---|---|
| `idle` | 4.02 (14) | 0.72 (71, foot.L) | 0.28 |
| `stunned` | 5.80 (37) | 1.45 (38, foot.L) | 1.53 |
| `advance` | 4.93 (8) | 0.96 (8, foot.L) | 2.00 |
| `return` | 5.42 (19) | 0.56 (24, foot.L) | 0.23 |
| `flee` | 2.43 (11) | 1.24 (11, foot.L) | 2.00 |
| `stand` | 2.32 (123) | 1.70 (123, foot.L) | 0.34 |
| `hurt` | 4.54 (10) | 0.44 (0, foot.L) | 0.15 |
| `critical-hurt` | 5.64 (17) | 0.51 (38, foot.L) | 2.00 |
| `stumble` | 8.14 (18) | 0.91 (7, foot.L) | 0.21 |
| `death` | 24.11 (60) | 1.60 (2, foot.L) | 1.98 |
| `swing` | 7.18 (20) | 2.23 (3, foot.L) | 2.00 |
| `thrust` | 7.71 (19) | 2.32 (2, foot.L) | 2.00 |
| `stab` | 6.77 (13) | 0.51 (25, foot.L) | 1.15 |
| `strike` | 6.92 (21) | 2.12 (7, foot.L) | 2.00 |
| `punch` | 6.00 (9) | 0.55 (4, foot.L) | 2.00 |
| `swing-off-hand` | 7.59 (16) | 1.27 (16, foot.L) | 0.23 |
| `swing-two-handed` | 7.81 (21) | 2.18 (7, foot.L) | 2.00 |
| `bow` | 4.22 (36) | 2.43 (34, foot.L) | 0.14 |
| `crossbow` | 4.10 (52) | 1.67 (28, foot.L) | 0.14 |
| `cast` | 5.87 (32) | 0.62 (18, foot.L) | 0.17 |
| `skill` | 5.62 (8) | 0.44 (0, foot.L) | 0.21 |
| `item` | 5.56 (23) | 0.89 (23, foot.L) | 0.21 |
| `drink` | 4.17 (64) | 1.08 (32, foot.L) | 0.14 |
| `meditate` | 18.14 (22) | 1.58 (22, the kneeling shin) | 2.21 |
| `guard` | 5.63 (9) | 0.44 (0, foot.L) | 0.14 |
| `guard-hold` | 5.18 (7) | 0.21 (62, foot.L) | 0.08 |
| `evade` | 6.08 (5) | 0.91 (5, foot.L) | 2.00 |
| `evade-hold` | 0.34 (76) | 2.48 *over* (77: both feet off the ground throughout) | 1.07 over |
| `death`'s last frame, over the ground | 24.11 *under* | 2.48 over | 1.92 under |

Scaled, she is within the man's bounds everywhere: at most **2.43 mm** under (bow's front foot at full draw), death's
fallen body 2.48 mm over the ground. **Her bounds are the man's** (3 mm under; death's end from 3 mm under to 5 mm over:
the manager's); the scaling alone brought her within a few millimetres, so nothing else was needed. Her lighter standing
dip (0.4–0.7 mm against his 0.1–0.3) is her other proportions (her feet and ankles smaller than her legs' ratio).

**Held** (`Tests/Statventure.Tests/Humanoid/FigureGroundTests.cs` unless named): every clip of both libraries measured by
the man's left leg, its length his (0.81515 m), the right leg the same on both bodies, the woman's ratio 0.968; the man's
scale exactly 1 and his hips' place the clip's sample to the bit, at and between frames; **made-up legs of 0.6 and 1.2 of
the man's** (the hips' height, the hip joints' offsets, thighs, shins and feet that many times, the trunk as it is): every
leg joint and the hips at that many times the man's place at every frame of every whole-body clip, **within 0.01 mm**
(measured 0.12 and 0.24 µm); a clip without a measure (M7's idle on the stocky figure) and a body without the chain (the
frame's sample) play as keyed, a chain of one joint or with a joint the skeleton hasn't measures nothing; only a bone's
place is scaled (a hand-made clip: its rotation and scale, and a plain node's place, as keyed); the scale in a stance laid
over the idle, a cross-fade, the figures' machine and for the gear (the woman's chain shirt and sword at death's end where
an unscaled pose with the hips' place scaled by hand puts them, and not where the unscaled clip does: 24 mm apart); the
bodies' ground −24.0 mm (to 0.01 mm), their worn parts reaching no lower; made-up bare soles, claws, hooves, a garment
lower than the soles, a model without a mesh, the micrometre's rounding (`BareSolesClawsAndHoovesStandOnWhatReachesLowest`);
an actor drawn raised by its ground, gear and hatching anchor alike, and exactly as before at 0 (`ActorGroundTests`); the
figure mode standing the geared man by his soles and framing him so (`FigureModeTests.TheFigureStandsByItsSoles`); every
game preset standing by its soles, and a figure on a garment sunk under them (`HeroFigureTests.AGameFigureStandsByItsSoles`);
a clip's measure read from its extras, a broken one refused, the record's version 2 in both forms
(`ModelPackTests.AClipsMeasureIsReadFromItsExtras`, `model/clips`), the model's check (`ModelValidationTests`, two rules);
both bodies on the ground (`HumanoidClipTests.BothBodiesStayOnTheGround`); the woman's evaluations against Blender's
(`AnimationReferenceTests`, the 34 + `rest-sway`: on frames within 0.0049 mm, between frames at most 0.98 of
Animation.md's allowance, death's left upper arm at frame 3.5 as before); a new assembled sample, the woman kneeling in
`meditate` at its marker (`assembly-human-female-meditate`, the family's library), which holds `render.py`'s scaling and
its ground against the game's builder.

**What moved in the references.** The man's: his 34 clip evaluations and `rest-sway`'s on him and on its own skeleton
**the same files** (not a byte). The woman's: 30 evaluations made again by the rule (`rest-sway` and her 29 whole-body
clips; her five stance loops move nothing and are the same files); her `stand` sample and her two geared samples posed by
`rest-sway` drawn again. Standing by the soles moved every figure the figure mode or Blender's assembly writes 23.997 mm
up with its camera (M7's figure in `figure-gear` 10 mm down: its feet 10 mm over its origin; the frame's sample not at
all): the same picture, made again by Blender because its numbers moved. Those that only moved with their camera (the
man's 12 figure scenes and 3 assembled looks, the woman's 5 unposed scenes and 3 assembled looks, M7's `figure-gear`)
differ from before in at most 25 pixels a picture, by at most 8 levels, but one ink pixel of the man's
`swing-two-handed-overhead` (Freestyle's rounding); 3 of the man's 30 pictures came out the same bytes. `clips.glb` and `test-clips.glb` (the measure in each
animation's extras; their keys the same bytes), `models.pak` (every model record at `model.animation` version 2), the
scene files (`figure-scenes.json`, `humanoid-scenes.json`, `assembly-looks.json` and `assembly-scenes.json`); the
editor's `preview-model` shows the animation's new row "Measure: 0 joints" (`Reference/editor-fixtures.json`; its
Chromium goldens captured again at 1 and 1.65: only the scroller's thumb moved, 24 and 40 pixels, the form a row longer
with the row itself below the view).

**Shown able to fail** (`claude-work/gm2/figure-ground/mutations.json`, made by `make-mutations.py` beside it; `mutate.py`
under the shared lock in a headless cage): **32 mutations, 32 killed**: the scaling (never, upside down, a plain node's
place, rotations or scales scaled too, 0 without a measure, the keyed length 1 % off, no clip measured), the chain (its
first segment alone, without Z, one joint a chain, a missing joint skipped), the measure's data (not read, a broken one
taken as none, the check's two rules), the ground (a skinned mesh by its node, the highest point, the garments left out,
not rounded, to the millimetre), the placement (lowered, by the node alone, by the ground alone), the figure mode (no
ground, the body's alone, framed by its origin, the close-up's focus not raised, the scene file by its node) and the
game's builder (no ground, the garments left out, the items taken in). The first run left one survivor, the figure mode's
ground from the body alone (no figure of the tests wore anything lower than its body): closed by a figure that does (M7's
figure in the human man's booted body as a garment, `TheFigureStandsByItsSoles`) and killed on the second run, with the
one mutation the first run couldn't build (written again). Blender's side by a runner of its own (`blender-breaks.sh`: each
break made the references it feeds again, ran the tests on them, put everything back): **7 breaks, 7 killed**: the
evaluation not scaling (the woman's death and meditate off Blender's), the ratio upside down, the keys scaled without
their handles (the Bézier `stand` and `death` bend between keys by them), `render.py`'s `pose_at` not scaling (the
kneeling woman's assembly), the figure not raised, the hatching's anchor not raised, the ground taken from the garments
without the body.

**Runs** (headless, scratch per-user folders, nothing audible): the full suite on the GPU and on lavapipe on `main`'s
`d2752e5`, **4660 + 951 = 5611 tests** (`main`: 4645 + 948 = 5593; 18 new: the engine's 15, the game's 3), none held;
`make-humanoid.sh check` 19 of 19 the same bytes (`clips` and `test-clips` with their measure); `make-blender-references.sh
--out` with `compare_references.py`: **242 of 242 byte-equal** (step 4b's 240 and the new sample's two); the player's
real folders the same before and after.

**Choices made (internal, recorded here):** the measure the left leg's thigh and shin (above); the measure in the clip's
data, per animation, and no number stored; a clip without a measure or a body without its chain unscaled; only a bone's
place scaled; the ground measured when loaded from the body and its garments, items left out, to the micrometre; the
ground on the `Actor` (any node it hangs on is where it stands) rather than a node the figure builder adds; the figure
mode and Blender's renders standing their figures too and framing them as they stand; a seventh assembled sample to hold
`render.py`'s scaling; the woman's bounds the man's.

**For later steps.** *Step 11:* hang a `HeroFigure`'s actor on the node where its soles go; nothing else (its ground is
set, its clips scale themselves). *Step 9 (Q59):* a new body needs its `thigh.L`, `shin.L` and `foot.L` (every body has
every bone) and nothing more; its ground is its lowest point (boots, bare soles, claws or hooves). *GM5:* a stage's marks
are where figures' soles go. *Any later family:* its clip library names its own measure (or none). *A branch made
before this one and rebased onto it:* `models.pak` made again with `make-models-pack.sh` (every model record is written
at `model.animation` version 2; a pack made before fails `EveryCommittedPacksRecordsBuildBackToTheirStoredForm`); a clip
library keyed again by `library.py` carries the measure by itself; every figure scene the figure mode writes
(`humanoid-scenes.json`, `figure-scenes.json`) or `assemble.py` assembles (`assembly-looks.json` → `assembly-scenes.json`)
written again and its Blender references made again (the figures now stand by their soles, their cameras with them);
a scene posing the woman (or any other body) by a clip made again too (her clips' hips scaled).
### Step 9a: the shared sets and the human in the repo (2026-11-20/21, the branch `gm2-step9a`)

The manager's art of step 9a (made in a scratch mirror of the family's scripts, every stop approved by the user; the
hand-over `claude-work/gm2/step9a/handover/`: `HANDOVER.md`, the twelve script files, `parts.json`, `swatches.py`,
`presets.py`) brought into the repo: the scripts, the models, the data, the user's Q65 in the data forms, the manager's
GM2-P29 in the maker, the tests. On the PC only for what runs Blender (the pinned 5.2.0, 8 threads); the tests run
anywhere. Decided on the way: the user's Q61–Q65 and the approvals, the manager's GM2-P25–GM2-P30 ("Decisions"); then, on
the step's report, the manager's GM2-P35 (two close-ups owed to an engine task), GM2-P36 (a part's place in its list) and
the user's Q73 (a colour of one swatch shows no row). Rebased on the figure's ground (`a6eb02a`): `models.pak` made again
(every model record at `model.animation` version 2), the scene files written again, the assembled figures standing by
their soles.

**The scripts** (`Tools/Models/humanoid/`): the twelve files as handed over (`bodies/__init__.py` with `as_build`,
`bodies/human.py` with the four builds, `forms.py`, `rig.py`, `outfit.py`, `gear.py`, `materials.py` with the slots
`hair.tie` and `marking`, `parts/face.py` with the ten new faces, `parts/hair.py` with the 38 new styles, and the new
`parts/beard.py`, `parts/headpiece.py`, `parts/mark.py`; what each change does: the hand-over's "What the tools' changes
do"), **one word changed**: the hair beads' two beads took the gear's slot `metal.trim` where GM2-P28 and `parts.json`
name the outfit's `cloth.trim` (the structural walk found it: a look's part in a gear's slot, which a hero's palette never
fills, so the beads would have shown the model's placeholder colour). In the sheets the user approved, drawn in the game's
look file, `metal.trim` and `cloth.trim` are the same gold (`#bb8d3c`): nothing the user saw changed. With the new files
the 19 committed models build the same bytes (`make-humanoid.sh check`, 28 s).

**The models** (`manifest.json`: 152 entries added, 171 in all, each at the full level; `make-humanoid.sh build`: 7 min 24
s for the 152; `make-humanoid.sh check`: 171 of 171 the same bytes, twice before the rebase and again after it, about 9
minutes a check). **One flake found:** with the machine loaded (three heavy jobs at once, or four builds side by side) the
woman's `twin-braids` comes out with one normal of one vertex of its hair cap (vertex 291) 5.35e-05 off the committed one,
2 builds in 24 (`claude-work/gm2/step9a-repo/flaky/`, `tools/flaky-build.sh`, `tools/glb-diff.py`): Blender's normals
summed in another order under thread load, a picture no one can tell apart; the check fails on it now and then. For the
manager (a determinism fix in the family's scripts, or a check that measures normals, is a tooling choice beyond this
step). Paths as step 4a's, a part made on its sex's average body (GM2-P25), the marks with their face in the path:
- `bodies/human-<sex>-slender` and `-strong` (4): the average body's skeleton at its rest, every deforming bone where the
  average has it, the hip and back sockets moved with the build (`HumanoidModelTests.ABuildHasItsAverageBodysBones`);
- `parts/face/human-<sex>-<2..6>/human-<sex>-average` (10), on the first face's skull;
- `parts/hair/<style>/human-<sex>-average` (38: 19 a sex; `long`, `curly`, `shaggy` and `buzz-cut` on each sex's head),
  made with the sex's first face (as step 4a's two);
- `parts/beard/<style>/human-male-average` (12), made to the man's widest jaw, fullest chin and highest cheekbones
  (GM2-P26, `beard.made_to`);
- `parts/head-piece/<piece>/human-<sex>-average` (16);
- `parts/marks/<mark>/<face>/human-<sex>-average` (72: six marks on each of the twelve faces, Q65), each one piece in
  `marking` without ink.

| Kind | Models | Bytes | Triangles (most) |
|---|---|---|---|
| bodies (6, 4 new) | 6 | 4,692,728 | 33,848 (the woman strong) |
| faces | 12 | 3,744,828 | 14,732 |
| hair | 40 | 10,219,128 | 16,304 (the man's `curly`) |
| beards | 12 | 1,196,564 | 5,928 |
| head pieces | 16 | 694,076 | 4,416 |
| marks | 72 | 984,976 | 164 |
| gear, clips | 5 | 2,152,472 | 12,024 |

`Assets/Models/humanoid/` 23,684,772 bytes in 163 models (was 4,547,792 in 11; the figure's ground then made `clips.glb`
1,492 bytes longer); `models.pak` (`make-models-pack.sh`, 164 entries) 2,170,661 (`main` `a6eb02a`) → 10,425,256 bytes. The largest models: the strong bodies (976,084 and 936,464 bytes, 33,848 and
32,442 triangles), then the slender (712,308; 705,936), the average ones, the chain shirts and the man's curly hair
(472,808, 16,304 triangles).

**The lists with their names** (Q61; `parts.json`; each list with "None"): *the man's hair:* Crop, Swept back, Spiky,
Curly, Messy, Shaggy, Long, Ponytail (tied), Topknot, Buzz cut, Side part, Fringe, Slicked back, Undercut, Mohawk, Warrior
braids, Half-tied (tied), Mane, Receding, Curtains. *The woman's hair:* Long, Long tied, Braid (tied), Curls, Bun, High
ponytail (tied), Twin braids (tied), Wavy, Bob, Curly, Pixie crop, Side braid (tied), Crown braid, Half-up (tied), Long with
fringe, Top bun, Shaggy, Undercut (`side-undercut`), Twin buns, Buzz cut. *Beards* (men): Stubble, Short trimmed, Full,
Goatee, Moustache, Moustache with chin tuft, Mutton chops, Chin strap, Long, Braided, Forked, Twin braids. *Head pieces*
(both): Circlet, Flower, Headband, Hair beads, Feather, Leaf wreath, Hairpin, Earrings. *Marks* (both, a model per face):
Freckles, Beauty mark, Scar over the brow, Scar on the cheek, Scar across the nose, Scar on the lip. *Faces:* Face 1–6 a
sex. The provisional names of step 5 replaced: "Long, tied" is "Long tied" ("Face 1" and "Swept back" stay).

**The data** (`Assets/Data/`, written from the hand-over's three files by a script of the step's,
`claude-work/gm2/step9a-repo/tools/write_data.py`, then the race converter and the look writer, `STATVENTURE_RACE_DATA` and
`STATVENTURE_LOOK_DATA=write`, which keep what it wrote and write it as the serializer does; `make-data-pack.sh`: 125
entries, `data.pak` 10,653 → 29,642 bytes):
- `data/races/human.json`: the 69 swatches of `swatches.py` in its order (skin 8, hair 10, eyes 8, lips 4, cloth 10, trim
  5, trousers 6, leather 3, buckle 2, hair tie 6, marking 5, brows 1, eye whites 1; Q63).
- `data/looks/bodies/`: the six (the writer's, step 6).
- `data/looks/parts/`: 82 parts named `<category>-<key>` (the hand-over's keys), each at `look.part` version 2 with its
  place: 12 faces, 41 hair styles, 13 beards (men only), 9 head pieces, 7 marks, each list's "None" `<category>-none`
  (the step 6 way, a part with no model). A style both sexes' lists hold (`long`, `curly`, `shaggy`, `buzz-cut`) comes at
  another place in each, so it is a part a sex, `hair-<style>-male` and `-female`. Each model is fitted to its sex's three
  bodies (GM2-P25); a part's `slots` are its models' materials' (the walk holds them to it), written in the slots' order.
  A mark's twelve models each name their face (Q65).
- `data/looks/presets/`: the eight of `presets.py`: its face, hair, beard (the men: its style or `beard-none`), mark (or
  `marks-none`), `head-piece-none`, and a colour of every slot the human has swatches of, the slots it names its own and
  the others `swatches.DEFAULT`'s (the approved look's own colours).

**GM2-P36: a part's place in its category's list** (`look.part` version 2, its form in step 6's "The maker's records";
`LookData.Offered`): each category's options by `order`, the same place by name. The game's in the approved lists' order
(`parts.json`) with "None" first: 0, then 10, 20 … (a face Face 1–6 at 10–60; the man's hair None, Crop, Swept back …
Curtains; the woman's None, Long, Long tied … Buzz cut; the beards None, Stubble … Twin braids; the head pieces and marks
likewise). The maker lists them so, the random button draws over the same lists, and a refit takes the category's first
option, "None" where a category has it: the Ranger made a man gets no beard, no hair and the first face (`LookMakerTests.
TheOptionsComeInTheApprovedOrderNoneFirst`). A version-1 part reads as place 0, so a mod's older parts keep their names'
order (`LookRecordTests.ACategorysOptionsComeByTheirPlaces`).

**Q65 in the data: a part's model by face** (`look.part.model` version 2, `Game/Statventure.Game.Data/Looks/LookPart.cs`;
version 1 frozen as `LookPartModel.V1`, upgrading to a model that names no faces; fixtures `look.part/hair-braid` version 1
and `look.part/marks-scar` version 2; the layout recorded):
- **The field:** `faces`, a list of `look.part` references (`[References(typeof(LookPart))]`): the faces the model fits;
  none: it fits any face. `LookPartModel.Fits(body, face)`: fitted to the body, and naming the face or no face.
  `LookPart.ModelFor(body, face)`: the first model that fits. A look's face is its part of the category `face`
  (`LookData.FaceOf`, `LookNames.Face`).
- **Offered:** `LookPart.IsFor`/`Refusal(name, body, face)` and `LookData.Offered(name, body, face)`: a part whose models
  name faces is offered on a body only with a face it has a model for there; with no face chosen only a model naming none
  fits. The refusals' words: "has no model for the face \"face-human-male-2\" on the body \"human-male-strong\"", "has no
  model for the body \"human-male-average\" without a face" (after step 6's race, sex and body).
- **The checks** (a part's, the game's and the editor's alike): each face once a model ("\"x\" is there twice"); each body
  and face one model's, a model naming no faces every face's on its bodies ("the body \"x\" has a model already
  (models[0])", "the face \"f\" on the body \"x\" has a model already (models[0])"); a face's own models name no faces ("a
  face's model names no faces (it is the face)"); with the data, a face that isn't one ("\"x\" is a hair, not a face") or
  isn't offered on a body of the model ("the face \"f\" has no model for the body \"x\""); a face that isn't there is the
  reference's ("no look part …"). A look's check asks what its body offers with its face, and refuses a part with no model
  for its face in those words.
- **The figure builder** (`HeroFigure.Resolve`) and **`assemble.py`** take each part's model for the look's body and face,
  and refuse in the same words; the maker (`LookMaker`) offers the categories with the look's face, chooses (a preset
  fitted, a sex or build changed, the roll) category by category in the maker's order with the face chosen first, and fits
  the look again when a part changes (a face without a part's model takes the category's first option, as a new body does).
  A mod that adds a face supplies its marks or that face offers none (Q65's words): the walk and the maker's tests show both.

**GM2-P29 in the maker** (`LookMaker.OfferedSlots`, `MakerScreen`'s colours): of the slots the race has swatches of, those
the look's body or one of its chosen parts uses, in the slots' order; the look still holds every slot (kept while hidden,
the roll still draws every one, every look passing its checks). A part's slots are its record's `slots`; the body's are its
model's materials' (`HeroData.SlotsOfBody`, read from the model in the data once a body: the record has no slots field,
and the model is where they are). **And the user's Q73:** of those, only the slots the race has two swatches of or more
(a colour with one swatch is no choice: the human's brows and eye whites, GM2-P28, show no row; a race or a mod that gives
them more gets it). On the human: a man shows eight colours (no lips, tie or marking), the Duelist ten (her lips, her
ponytail's tie), the Scholar's freckles their marking. No markup or stylesheet changed. The screens' test data's stand-in
body models now have the materials `skin` and `cloth` (`HeroTestData.BodySlots`), so GM2-P29 changes no fixture; Q73 hides
the Half-Beast's one skin colour in `maker-half-beast-light`, whose fixture and Chromium goldens were made again
(`Golden/game-screen-maker-half-beast-light@1.png`, `@1.65.png`: its Skin row gone, the Hair row under the title).

**The presets against the old looks** (`LookDataTests.EachPresetKeepsItsOldLooksHairBeardMarksAndTie`, over the oracle's
`look-catalogue`, the old `sprite-catalog.js` run in Node): each committed preset's hair, beard and marks the old look's by
this table, a tie exactly when the old style had one (the new style's slots hold `hair.tie`), and an old look's own tie
colour as the race's tie swatch nearest it (the Duelist's `#c43a4a`: Red):

| Old (`sprite-humanoid.js`) | New part |
|---|---|
| hair `bald`, `crop`, `spiky`, `swept`, `curly`, `curls`, `topknot` | `hair-none`, `hair-crop`, `hair-spiky`, `hair-swept-back`, `hair-curly-<sex>`, `hair-curls`, `hair-topknot` |
| hair `ponytail`, `highponytail`, `long`, `wavy`, `bob`, `bun` | `hair-ponytail`, `hair-high-ponytail`, `hair-long-<sex>`, `hair-wavy`, `hair-bob`, `hair-bun` |
| hair `braid`, `twinbraids`, `messy`, `shaggy` | `hair-braid`, `hair-twin-braids`, `hair-messy`, `hair-shaggy-<sex>` |
| beard none, `stubble`, `full`, `long`, `braided`, `forked` | `beard-none`, `beard-stubble`, `beard-full`, `beard-long`, `beard-braided`, `beard-forked` |
| marks none, `freckles`, `scar` | `marks-none`, `marks-freckles`, `marks-scar-brow` (the old scar ran from over the eye down across it, `sprite-humanoid.js` 234) |
| tied: `ponytail`, `highponytail`, `braid`, `twinbraids` | a style with `hair.tie` |

The human's eight: Wayfarer crop and stubble, Sellsword spiky, Smith curly and full, Courtier swept back; Ranger long,
Duelist high ponytail with the red tie, Healer curls, Scholar braid and freckles. (The old scar and the styles no human
wears are the other races' presets', 9b–9e.)

**Against Blender** (GM2-P30; `AssemblyTests`, `Reference/assembly-looks.json` written with `STATVENTURE_ASSEMBLY_LOOKS=write`,
assembled and rendered by `make-blender-references.sh`): 18 samples of the game's own data and swatches (a sample may name
its own data folders: `"data"` in the list, which `assemble.py` and `make-blender-references.sh --out` read): the eight
presets at three-quarter (360 × 480); the four new bodies plain from the front (the first face and the first humanoid's
hair, the approved look's own colours); six looks: the man strong with `mane` and the `long` beard (three-quarter), the man
slender with `mohawk`, the circlet and the brow scar (a close-up, 320 × 320), the man average with `warrior-braids` and the
`braided` beard (side), the woman strong with `curls` and the leaf wreath (three-quarter), the woman slender with
`twin-braids`, the flower and freckles (three-quarter), the woman average with `side-braid`, the feather and the cheek
scar (three-quarter). Step 5's samples (the tests' race over the game's data) take the presets' new parts (the man crop and
stubble, the woman the Ranger's long hair, the figure's ground's kneeling woman too), but for the woman's close-up, which
keeps the first humanoid's long tied hair (GM2-P35). `TheGamesOwnSamplesAreTheHumansPresetsBodiesAndLooks` holds each a look
the maker could make and every part named above among them. Measured under `ReferenceLimits.DetailedFigure` on both drivers
after the rebase (GPU / lavapipe; `claude-work/gm2/step9a-repo/measure-*.txt`, `measure-table.md`; the limits for 172,800
pixels 41 inside and 157 edge pixels, for a close-up's 102,400 24 and 93; edges' mean 4, missed 2 %, extra 3 %, unlike
8.3 %, coverage 0.12):

| Sample | Inside | Edge | Edges' mean | Missed | Extra | Unlike | Coverage |
|---|---|---|---|---|---|---|---|
| human-male-ember | 8 / 12 | 74 / 78 | 2.2 / 2.3 | 0.01 % | 0.12 / 0.13 % | 0.45 / 0.44 % | 0.074 |
| human-female-ember | 8 / 7 | 35 / 38 | 2.3 | 0.02 % | 0.04 % | 0.38 / 0.39 % | 0.069 |
| human-male-red-back | 15 / 17 | 23 / 22 | 2.8 | 0.04 % | 0.48 / 0.51 % | 0.84 % | 0.076 |
| human-female-red | 10 / 9 | 9 | 2.8 / 2.9 | 0.02 % | 0.16 / 0.17 % | 0.28 % | 0.072 |
| human-female-red-close-up (long tied) | 18 / 16 | 60 / 61 | 3.4 / 3.5 | 0.00 % | 1.74 / 1.82 % | 0.86 % | 0.086 |
| human-male-geared | 9 / 5 | 146 / 144 | 2.7 | 0.00 % | 0.05 % | 2.13 / 2.14 % | 0.080 |
| human-female-meditate | 26 | 73 / 69 | 2.5 / 2.6 | 0.05 % | 0.39 / 0.41 % | 0.88 % | 0.077 |
| preset-human-male-1 … 4 | 5–10 / 4–9 | 39–73 / 36–77 | 1.9–2.2 | 0–0.01 % | 0.12–0.27 / 0.13–0.29 % | 0.18–0.45 % | 0.069–0.074 |
| preset-human-female-1 … 4 | 0–3 / 0–4 | 24–36 / 27–38 | 2.0–2.4 / 2.0–2.5 | 0–0.01 % | 0.14–0.37 / 0.15–0.40 % | 0.19–0.84 % | 0.068–0.073 |
| human-male-slender, -strong | 9, 7 | 49 / 50, 39 / 38 | 2.3–2.5 | 0.00 % | 0.12–0.16 % | 0.06, 0.13 % | 0.067, 0.070 |
| human-female-slender, -strong | 2 / 3, 5 | 33 / 32, 16 / 18 | 2.3–2.4 | 0–0.01 % | 0.09–0.22 % | 0.21 / 0.20, 0.32 % | 0.068 |
| human-male-strong-mane | 5 | 21 / 23 | 2.0 / 2.1 | 0.01 % | 0.26 / 0.28 % | 0.32 % | 0.071 |
| human-male-slender-mohawk (close-up) | 0 / 1 | 26 / 24 | 2.1 / 2.0 | 0.00 % | 0.74 / 0.77 % | 1.09 / 1.07 % | 0.081 |
| human-male-warrior-braids | 6 / 8 | 25 / 31 | 2.5 | 0.00 % | 0.13 / 0.14 % | 0.24 / 0.22 % | 0.070 |
| human-female-strong-curls | 2 | 25 / 26 | 2.0 | 0.00 % | 0.22 / 0.24 % | 0.44 / 0.43 % | 0.074 / 0.073 |
| human-female-slender-twin-braids | 2 / 3 | 33 / 30 | 2.4 / 2.5 | 0.00 % | 0.04 % | 0.32 % | 0.069 |
| human-female-side-braid | 1 / 0 | 19 / 18 | 2.0 | 0.00 % | 0.30 / 0.32 % | 0.35 % | 0.071 |

The nearest to its limit is the geared man's edge pixels (146 of 157), as before 9a. **Owed (GM2-P35):** two close-ups
passed every measure but the inside pixels: the woman's close-up with the Ranger's long hair 31 / 39 and the twin braids'
close-up 26 / 22 (24 allowed): faint seams 3 to 28 levels darker along where two hair locks meet on lit hair, which Blender
draws flat (suspected: the shading normals where the locks of one joined hair model cross; not proven). Both out of the
tests, no limit touched; their files (list and scene entries, Blender's and the engine's pictures on both drivers, the
numbers, the pixels marked) in `claude-work/gm2/step9a-repo/pending-samples/` (its README says how to bring them back); an
engine task of its own before GM2 is ticked (`Docs/Game-Plan.md`, "Engine items the port will meet"). The twin braids' look
is compared at three-quarter instead, the sheet's own view, where it passes (2 / 3), so none of the six looks is missing.

**The builds on the ground** (`HumanoidClipTests.BothBodiesStayOnTheGround`, the figure's ground's test, over the six bodies;
every body's numbers in `claude-work/gm2/step9a-repo/ground-builds.txt`): the four builds' soles rest at −24.0 mm as the
average bodies' (held), their legs the average's, so the clips' hips scale by the same ratios; their lowest points are
measured with no bound, since the thicker builds' thighs and shins reach under the man's bounds (3 mm under; death's end 3
mm under to 5 over) where the body lies or kneels: the strong man's death 14.77 mm under at its end (the right thigh),
meditate 7.43 (the kneeling shin), flee 3.74, advance 3.59, victory 3.26; the strong woman's death 9.04, meditate 4.40; the
slender man's death 4.38 (his right foot), meditate 3.42; the slender woman within them everywhere (at most 2.49, bow). The
two average bodies keep their bounds. Reported to the manager with Q59's check of the clips on the builds (not widened).

**Tests** (5266 + 1000 = 6266; on `main` `a6eb02a` 4660 + 951 = 5611): `LookRecordTests` (both new versions' fields and the faces' reference;
`APartMadeForEachFaceIsOfferedWithItsFaces`, `APartsFacesChecks`, `ACategorysOptionsComeByTheirPlaces`; the fixtures of
every version), `LibraryRecordLayoutTests` (the layouts), `LookDataTests` (the presets against the old looks;
`TheMakersRecordsAreTheSerializersForms`), `LookMakerTests` (`TheMakerOffersAColourOnlyWhereTheBodyOrAChosenPartUsesIt`,
`TheOptionsComeInTheApprovedOrderNoneFirst`, `AMarkFollowsTheChosenFace`, on the game's own data), `MakerScreenTests`
(`AColourRowShowsOnlyWhereTheBodyOrAChosenPartUsesItsSlot`, `AColourWithOneSwatchShowsNoRow`, on the shipped data, whose
harness now mounts `models.pak` too, and on the test data with a race's own swatches, `HeroScreensHarness`'s `races`),
`HeroFigureTests` (`APartMadeForEachFaceDrawsTheModelOfTheLooksFace`; the game's preset tests on the Scholar, the
Courtier and the new Wayfarer; the figure's ground's soles test over the game's own presets), `FigureWalkTests` (below),
`AssemblyTests` (25 samples), the screens' `maker-half-beast-light` (Q73), `HumanoidClipTests.BothBodiesStayOnTheGround`
over the six bodies (above), and the engine's
`HumanoidModelTests` over the 152 new models (each body and part's skeleton and rest, weights, materials, inkless pieces,
one primitive per material and ink flag; `ABuildHasItsAverageBodysBones`; every part follows the head alone; the marks
one inkless piece in `marking`, unhatched; `hair.tie` and `marking` the two slots the game's look file doesn't colour).
**The structural walk** (`FigureWalkTests`): every body with every option offered on it, a part made for each face walked
with each face it is offered with (the pairs: 86 on each of the man's bodies, 73 on each of the woman's, 477 in all);
a part's model bound by the joints its vertices follow (a part made on the average body is worn on the three builds,
whose hip and back sockets lie elsewhere, GM2-P25); the races drawn in their models' own colours none now (the human's
swatches fill every slot its figures use). A break of its own: a mark's model missing for its last face.

**Shown able to fail.** *The C# rules and the data* (`mutate.py` under the shared lock in a headless cage;
`claude-work/gm2/step9a-repo/mutations.json`, made by `tools/make-mutations.py`): 47 mutations of Q65's field and its
rules (the fit by body and face, the refusals and their words, the first model that fits, each check of a part's faces,
version 1's upgrade, the faces' reference), the data's offers and a look's check with the face, the figure builder, the
maker (its categories and choices with the face, the refit on a part's change), GM2-P29 (the body's and the parts'
slots, the order, no look, the screen's rows and title, the bodies' slots given and read), the walk (the face pairing, the
joints followed) and the data (the presets' hair, beard, marks and tie, the braid's tie, a part's form). The first run: 43
killed, 4 survived; two were gaps, closed and killed in the rerun (`mutations-rerun.json`: a maker with no look offering no
colours, a race whose swatches nothing uses showing no colours' title). After the manager's answers and the rebase the list
grew by GM2-P36 (`mutations-2.json`: the order by name alone, the same place in reverse name, the highest first, version
1's place not 0, version 1's slots lost in the upgrade) and Q73 (a colour of one swatch offered, one of two hidden too, the
rule only with or only without the bodies' slots; and "no bodies' slots" offering none, which replaced "every one").
**The standing list: 56, 54 killed, 2 that can't differ:** the first model that fits against the last (a part's checks
leave one model a body and face, so they are the same), and the walk following a joint of weight 0 (every unused
influence of the family's models names the skin's first joint, the hips, which every build has at its average's rest).
*`assemble.py`'s rules* (`tools/assemble-rules.py`, step 5's
runner with 9a's cases: a mark by face on two faces, a face with no model of it, a look with no face, a scene's own data):
**25 of 25 killed** (step 5's 16 and 9 new: the first model whatever its faces, a model naming faces fitting no face or any
face, the face not the look's, each refusal unreported or put the other way, a scene's data ignored or put under the
list's; the same 25 after the rebase, `assemble-rules-2.log`). *The engine's model tests* (`tools/model-breaks.py`: a
committed model changed, the tests run, the file put back): 7 of 8 killed (a slender body that is the average's, a
build's spine moved, a mark inked, a mark hatched, a beard and a head piece following the neck, and for the ground test
over the builds a build standing 1 cm lower, its `hips` joint moved, since a skinned mesh ignores its node's place); the
eighth, a tie in a gear's slot, is the walk's rule, not the engine's (the engine's tests take the gear's slots, which the
gear's models use; the walk refuses a part in one, and found the hair beads so). Nothing written to the player's real
folders (the game's folder and `~/Documents` the same before and after every run).

**Runs** (each full run under the shared lock in a headless cage, a scratch per-user folder, sound to the null device;
logs in `claude-work/gm2/step9a-repo/`): the first ones, before the manager's answers, found the two close-ups (GM2-P35).
The final ones on the rebased branch (`602dde1` on `a6eb02a`; only the docs' last words after them): the GPU 5266 + 1000
= 6266 passed (`full-final-gpu.log`), lavapipe 6266 passed (`full-final-lavapipe.log`). The editor's `preview-cutscene`
fixture lists `models.pak`'s models, so the new ones changed `Reference/editor-fixtures.json`; captured again in Chromium
(`make-editor-references.sh preview-cutscene`), its goldens came out the same bytes (a closed list). `make-humanoid.sh
check` after the rebase: 171 of 171 equal twice on a quiet machine (`check-final-1.log`, `-3.log`; the flake above in
`-2.log`). `make-blender-references.sh --out` (20 min) and `compare_references.py`: 278 files byte-equal, none different
or missing. The player's real folders the same before and after every run.

**Choices made (internal, recorded here):** the hair beads' slot (above, the manager agreed); the data written by a script
from the hand-over's files, then the converter and the writer; part names `<category>-<key>`, "None" `<category>-none`, a
style of both lists a part a sex (`hair-<style>-<sex>`: GM2-P36's places differ in each list); `order` a field of
`look.part` version 2 appended as field 7, no check (as the presets' has none); a part's slots in the slots' order;
Q73 in `LookMaker.OfferedSlots` (the maker's colours), the look and the roll untouched; the marks' paths with the face before the body; Q65's field on `look.part.model`
(version 2) with its checks and words as above, the face a look's `face` part; a model naming faces fitting no look without
a face; the maker fitting the look again on any part's change; the body's slots read from its model's materials
(`HeroData.SlotsOfBody`), the screens' stand-in body models given `skin` and `cloth`; the shipped harness mounting
`models.pak`; the walk's binding by the joints followed and its face pairing; the table of old names; the six looks' views
and colours, the bodies plain from the front, the twin braids at three-quarter (GM2-P35); a sample's own data folders in
the assembly list; the measurement probe outside the repo (`claude-work/gm2/step9a-repo/tools/ZzScratchMeasureAssembly.cs`);
the ground test's builds measured with no bound and its problems all reported at once.

**For later steps.** *9b–9e (each race):* its three builds by `bodies.as_build` from its average body (the same skeleton,
the girths and the masses' and sockets' ratios); six faces a sex on one skull (`parts/face.py`, `flat` and the head's
frame shared), so the shared hair styles and head pieces fit all of them; the shared sets fitted to its heads (hair made
with the sex's first face, `hair_model`; beards made to the race's widest jaw, fullest chin and highest cheekbones,
`beard.made_to`; head pieces; marks one model per face); its swatches in its race file (every slot its figures use) and
its presets in the old looks' names, held to the old looks by the table above (the scar is `marks-scar-brow`; an old style
a sex's list lacks is the step's to place); parts with their slots in the slots' order, their places in their approved
lists ("None" 0, then 10, 20 …; GM2-P36), its models fitted to its three bodies; the walk, the maker and `AssemblyTests` take
them with no code change. Its builds' ground: measured by `BothBodiesStayOnTheGround`'s way (the human's thicker builds
lie and kneel under the man's bounds: the manager's to judge with Q59). *Step 11:* the figures' parts by body and
face (`HeroFigure` does it). *GM2-P22's task and step 11:* the four new bodies are in `models.pak` (Q59's check is on the
hobbit's and the Goliath's).

### Step 10: the nine emblems in the repo (2026-11-20 to 22, the branch `gm2-step10`)

The manager's art (the motifs' sketches, the user's Q66, the medallions modelled and approved as renders, Q67; GM2-P31,
GM2-P32: `claude-work/gm2/step10/`, its hand-over `handover/HANDOVER.md`) brought into the repo: the scripts, the models,
the pictures in `ui.pak`, the screens showing them, the rule of their colours in the game for the live emblem (step 11's),
the tests. Decided on the way: the user's Q70 (the emblems' own limits against Blender until an engine task), Q71 (the
carousel's pictures as shown) and Q72 (the dark seal's sepia kept); the manager's GM2-P34 (the pictures scaled down from
the 640 px render). On the PC only for what runs Blender; the tests run anywhere (what Blender made is committed).

**The scripts** (`Tools/Models/humanoid/emblems/`; `__init__.py` says what each file is):
- `medallion.py`, the builder (the manifest's `emblems.medallion:emblem_model <name>`): the manager's `emblem_build.py`
  with its paths made the repo's and nothing of its geometry, heights or materials changed (GM2-P32): the body turned in
  128 sides (a plain back 0.10 units behind the field, the rim to 1.33 units, the field 0.145 under the rim's top), 40
  beads, each outline of the motif a slab chamfered at its top (`hi` 0.070, `acc` 0.056, `mid` 0.050 units off the
  field, an element over an earlier one at least 0.022 higher; the chamfer at most 0.020 units, 0.24 of the outline's
  thickness, 0.45 of its height), a `lo` element a thin inlay 0.006 units over what it lies on, drawn without ink; smooth
  shading split at 32°; the pieces joined per ink flag (GM2-P19: `emblem-<name>` and `emblem-<name>-unlined`); the front
  along glTF's +Z, the middle at the origin; one unit 0.08 m.
- `chosen.json`: the outlines of the nine chosen motifs (Q66), each element `points` in the unit field, a `tone` and
  `outline`; written by `chosen.py` from `motifs.py` (one element a line; its values the hand-over's to the last digit,
  `chosen.py check` holds the file to it). `motifs.py` and `sketchkit.py` are the manager's sketch scripts, kept for the
  record and for redrawing a motif (Pillow draws the sketches).
- `palette.py`, the rule of the colours (GM2-P31) in plain Python: `GOLD`, `BONE`, `INK` and `RULE` (each slot: from the
  accent or a colour, toward what, how far), a mix per sRGB channel `a + (b − a)·t` rounded half to even (the approval
  renders' Python `round`); `palette.py <#accent>` prints the six colours, `palette.py check` holds every scene of
  `Reference/emblem-scenes.json` (written by the game's tests with the game's rule) to it for its race's accent.
- `look.py`'s `EMBLEM_SLOTS` (the six, after `SLOTS` and `GEAR_SLOTS`; a look may name them); `manifest.json`: nine entries
  `emblems/<name>` (the races' `emblem` values), no detail level, out `Assets/Models/humanoid/emblems/<name>.glb`.
- `make-humanoid.sh check` also runs `chosen.py check` and `palette.py check`.

**The models** (`Assets/Models/humanoid/emblems/`, in `models.pak` as `models/humanoid/emblems/<name>`): built by
`make-humanoid.sh build emblems/<name>`, **each byte for byte the manager's** `claude-work/gm2/step10/out/models/<name>.glb`
(compared file against file; the export is deterministic). Vertices / triangles / primitives / bytes: human 5,020 / 6,228
/ 6 / 203,880; elf 8,898 / 10,404 / 4 / 351,100; dark elf 4,802 / 6,052 / 6 / 195,864; dwarf 10,118 / 11,540 / 5 /
398,008; hobbit 7,286 / 8,880 / 5 / 291,428; lizardman 6,398 / 7,848 / 5 / 256,824; half-beast 6,582 / 8,164 / 5 / 264,600;
goliath 11,388 / 12,900 / 6 / 447,724; merfolk 6,364 / 7,912 / 5 / 256,112 (2,665,540 bytes in all). `make-humanoid.sh
check`: every model of the manifest the same bytes, twice (28 of 28 on the figure's ground; 180 of 180 after the rebase on
step 9a; step 9a's flake on the twin braids' hair cap not met). `models.pak` 10,860,567
bytes, 173 entries (`main` before, step 9a: 10,425,256 bytes, 164 entries; the nine emblems 435,311 bytes of it).

**The rule in the game** (`Game/Statventure.Game/Figures/Emblems.cs`): `ModelPath(emblem)` = `models/humanoid/emblems/
<emblem>`; `Gold`, `Bone`, `Ink`, `Rule` and `Colours(accent)` (the six slots, the same arithmetic as `palette.py`:
doubles, `MidpointRounding.ToEven`); `PaletteOf(race)` and **`PaletteOf(VirtualFileSystem data, string race)`**, the live
emblem's seam (a race's id in the game's data to a `Palette` with the six slots filled; a race the data hasn't refused);
`Unit`, `Radius`; `PictureCamera()` (the approval renders' camera: orthographic, 3 units high, at the middle from (0,
0.35, 4) m, about 5° up). **For step 11** (its branch's `LiveHeroViews`): the emblem's model is `Emblems.ModelPath(race.Emblem)`
(`models/humanoid/emblems/<emblem>` in models.pak; a race with no `emblem` has none: the ✦), its colours
`LiveHeroViews.EmblemPalette = Emblems.PaletteOf` (the `(data, race id) → Palette` seam), its style the game's
(`GameStyle.Load`); its framing step 11's own (the medallion's middle at the origin, its front +Z, 2.66 units = 0.2128 m
across); `LiveViewReferenceTests.EmblemSamples` takes the real emblems from models.pak in place of its stand-ins.
Whoever merges second wires the seam (the manager's word).

**The pictures** (GM2-P34; `engine/Assets/UI/emblems/<emblem>-<size>.png`, ui.pak's `ui/emblems/`, made by
`Tools/Scripts/make-emblem-pictures.sh`):
1. The game's tests write the scenes (`EmblemReferenceTests.SceneFile`, `Reference/emblem-scenes.json`,
   `STATVENTURE_EMBLEM_SCENES=write`): each race's emblem from models.pak's file in `Assets/Models`, its race's colours by
   the game's rule, the game's style and sun (data.pak's `render/styles/game`), `Emblems.PictureCamera()`, 640 × 640 (the
   approval renders' size), over black and over white (`emblem-<name>`, `emblem-<name>-white`).
2. `make-blender-references.sh` renders them (its seventh part; `--out` rewrites the scene file's model folder as the
   humanoid scenes'): `Golden/r3d-emblem-<name>[-white].png` and `-ink.png` (36 files, 3.0 MB; the two inks the same
   bytes).
3. The game's tests make the pictures from those renders (`EmblemPicturesTests.Master` and `Picture`,
   `STATVENTURE_EMBLEM_PICTURES=write`; otherwise every committed picture must be what the renders make, pixel for pixel):
   the ground made transparent (a pixel's cover is one less the mean, over its channels, of the white render less the
   black in linear light; its colour over black is already that cover times the emblem's), the lines laid over (the ink
   render's coverage in the look's ink `#080605`, as `compose.py` and the engine lay lines over a view; the cover grows
   by the ink's past the edge), then scaled down to each size with a Lanczos filter of three lobes stretched to the scale
   (the approval copies' filter), in sRGB with the cover multiplied in (so a picture laid over the page is the render laid
   over it made smaller, as the approved copies over the dark ground were), across then down, each pixel's weights summing
   to one, the cover and the colour clamped to what a pixel holds, then the colour divided by the cover (PNG's straight
   alpha), each rounded to the nearest level; written by the engine's PNG writer (stb's).
4. `make-ui-pack.sh` packs `Assets/UI/emblems/*.png` at `ui/emblems/` (files, as they are).
`make-emblem-pictures.sh` runs the four (step 4 uses the shared build cache: under the shared lock when workers run
beside). Made again twice on the PC: the renders, the 90 pictures and the pack the same bytes each time.

**Their sizes** (`EmblemPictures.Places`; measured in the engine and in Chromium alike on the screens' fixtures): the
sheet's seal draws its picture 72 % of the seal button's content box, the button 172 px (138 at 840 px or less, 100 at 650
px or less: the later rules of game.css win) less its 1 px border and the button's own padding of 1 by 6 px: 113.76 ×
120.96 CSS px (89.28 × 96.48, 61.92 × 69.12), the square picture as wide as the box (`object-fit: contain`); Chromium lays
out 113.75 × 120.953 and 89.266 × 96.469 at 1, 114.328 and 89.848 CSS px wide at 1.65 (188.6 and 148.3 device pixels).
(Step 7's note and the design's "about 124, 99 or 72 px" counted 72 % of the whole button; corrected.) The carousel's
medallion holds its picture in its inside, 94 px (156.4 device pixels at 1.65, its border a whole device pixel); a medallion
of the strip in its inside, 36 px (60.7). So ten sizes, device pixels: **36, 61, 62, 89, 94, 103, 114, 148, 156, 189**
(each place at 1 and 1.65, rounded): 90 pictures, 1,693,509 bytes. ui.pak 2,769,289 bytes (110 entries; was 1,098,522,
20).

**The screens** (Q36, Q71, Q72): `Screens/Sheet/EmblemPictures.cs`, one per screen: `Show(img, emblem)` shows the emblem's
picture in an `img` (true) or hides it with no `src` (false: no emblem, or an emblem without a picture at every size in
the document's data, a mod's), the picture the one whose size is nearest the image's box in device pixels by their ratio
(the smaller side: `object-fit: contain`), chosen again whenever that box changes (a `ResizeObserver` on the
device-pixel content box: the window's width past a breakpoint, its scale, the page's zoom; set at once on a new emblem
from the size last seen), `Forget(img)` for an image taken out of the page. The sheet (`SheetScreen`) shows the hero's
race's picture on the seal's `#race-icon`, the ✦ (`.hero-sigil-fallback`) only without one; the old stylesheet's dark
theme sepia on it stays (Q72). The carousel (`RacesScreen`): a strip medallion holds `img.races-strip-picture` where its ✦
stood (class `has-picture` on the medallion; the ✦ for a race without), the stage's medallion `img#races-emblem-picture`
before its live view's `surface`, its ✦ hidden while the picture shows, and both gone under a live emblem (`.is-live`);
`races.css`: the pictures absolutely placed in their medallion's inside (`.races-strip-item.has-picture { position:
relative }`), the strip's letting the pointer through (`pointer-events: none`: the square picture's corners lie outside its
round medallion, which the engine and Chromium hit-test by its border's curve; taking the pointer there gave the headless
compositor's resting pointer a hover and the title's tooltip over the half-beast's medallion at 1.65, which
`HeroScreenFixturesTests` holds the fixtures free of), nothing of the approved look else changed (Q71). `IHeroViews` is unchanged (its comment's "✦ in the
race's colour" until a live view is now the emblem's picture: step 11's file to word).

**The tests' harness:** `GameUiHarness` gives its document's images as the program's window host does (`UiHost`: the
layout's image sizes and the painter's images from the document's data, `UiResources`), and `GameScreenTests.Render`
paints them, so the fixtures, the screens' tests and `ProgramPathScreenTests`' real window agree on the pictures.
`make-game-screen-references.sh` gives Chromium an image the document reads from `ui.pak` (`src="ui/....png"`) as its
committed file (`engine/Assets/UI/...`, which ui.pak holds as it is), by its file url.

**Against Blender** (`EmblemReferenceTests`, the game's tests: each emblem built as the live view will build it, the model
from models.pak, the rule's palette, the game's style, the pictures' camera, 640 × 640): the shading over both grounds and
the lines under **`ReferenceLimits.Emblem`** (the user's Q70: the detailed figures' limits with extra lines 14.3 %, total
ink 18.6 %, the coverage difference 0.154; pinned by a test; each widened limit bites on the Half-Beast's emblem set just
under what it measured on both drivers). Measured (the PC's GPU / lavapipe): the shading's inside pixels off by more than 2
levels 20 / 34 of 409,600 in every scene (the limit 97), edge pixels 20–28 / 22–33 (372), the edges' mean 3.07–3.87 levels
(4); the lines: missed 0–1.54 % (2 %), extra 5.50–12.77 / 5.63–13.02 % (DetailedFigure's and M6's 3 %), total ink
+6.0–17.0 % (10 %), coverage 0.097–0.140 (0.12), unlike 0.09–4.03 % (DetailedFigure's 8.3 %; M6's 3 % fails on the dark
elf and the half-beast). Under M6 every scene fails its stray pixels (none allowed) and the lines. What parts them: the
engine draws a line along the top of every raised outline where its side, seen nearly edge-on from 5° up, rises from the
field or from the outline under it; no mesh edge runs there and Freestyle draws none (`Docs/Renderer-3D.md`, "Ink
comparison": the engine "can't tell two surfaces crossing from a silhouette"); every line Freestyle draws, the engine
draws. An engine task is to stop that line, then these limits are measured again and tightened (`Docs/Game-Plan.md`,
"Engine items the port will meet"). Not to try again: the manager joined each raised shape with the body in Blender by an
exact union (2026-11-21): two emblems came out the same to the eye, the elf's killed Blender; not pursued.

**Tests** (engine: `EmblemModelTests` 19 (the manifest makes the races' emblems by the builder at no level; each medallion
rigid, its materials the emblems' slots with rim and field, the inlays unhatched and the one piece without ink, one
primitive per material and ink flag; its middle at the origin, its field flat at z = 0 facing +Z, its rim 1.33 units,
its back 0.10 behind); `HumanoidFiles.Figures()` leaves the emblems out (they are no figures), `Emblems()` lists them;
`UiResourcesTests` with the pictures in ui.pak; `EntryListTests` with the editor tree's `ui/emblems`. Game:
`EmblemPaletteTests` 15 (the nine races' approved colours, half to even, the slots of `EMBLEM_SLOTS`, palette.py's
constants and rule read from the file, Blender's scenes coloured by the rule, the seam for every race of the data),
`EmblemReferenceTests` 23, `EmblemPicturesTests` 20 (the pictures what the renders make, ui.pak holding them, the two
renders one scene, the making on small made-up renders), `EmblemScreensTests` 26 (the sizes, the nearest picture, the
seal at each width and scale as Chromium lays it out, the picture following the zoom and the race, an emblem with no or
some pictures, the carousel's strip and medallion at 1 and 1.65, the ✦ for an enemy-only race, the live emblem taking the
picture's place, the images let go when the strip is made again); `SheetScreenTests` two lines.)

**What moved beside:** `models.pak` and `ui.pak` made again; the game-screen goldens of the 26 fixtures whose page now
shows a picture captured again in Chromium at 1 and 1.65 (the carousel's 7, where the strip shows the pictures; the
sheet's 14 and GM1's `page-*` and `menu-page-*` 5, where the seal is masked, Q50, so the comparison doesn't see it but the
golden is the markup's): every fixture passes as before. 20 of them came out changed; the sheet's six scrolled down past
the seal (`sheet-attributes-dark`, `-light`, `sheet-dev-attributes-dark`, `sheet-doll-dark`, `-light`,
`sheet-narrow-doll-dark`) the same bytes. Five of the carousel's at 1.65 captured once more after the
pictures were given `pointer-events: none` (their first capture had the compositor's pointer hovering the half-beast's
medallion through its picture's corner: the hover ring, whose timing made a recapture differ by up to 5 levels). The maker's 7 fixtures' markup changed only in the hidden
carousel's section (no picture shows): not captured again. The editor's 12 fixtures that list the game's packs (`ui.pak`
gains the folder `ui/emblems`) captured again at both scales; `EntryListTests` 322 rows. After the rebase on step 9a:
`models.pak` made again (`make-models-pack.sh`; `ui.pak` and `data.pak` made again by their scripts came out the same
bytes, `data.pak` step 9a's), both fixture lists written again by the tests; the two fixtures both steps had changed
(`maker-half-beast-light`, 9a's colour rows beside this step's hidden carousel, and the editor's `preview-cutscene`, whose
model list holds 9a's models and the emblems) captured again at 1 and 1.65: each the same bytes as step 9a's golden (the
carousel hidden in the maker; the preview's list a closed one).

**Shown able to fail** (`claude-work/gm2/step10-repo/`). *The C# rules and the manifest's* (`mutate.py` under the shared
lock in a headless cage; `mutations.json`, made by `make-mutations.py`): 70 mutations: the rule (each colour, amount,
target and source, the slots' order, the rounding, a colour's checks, the seam's race and refusal), the model's folder,
the pictures' camera, the pictures' sizes and their choice (the nearest by ratio, a tie, the device-pixel box, the smaller
side, a box of no size, every size or none), showing, hiding and forgetting an image, the sheet's and the carousel's use
of them, the making of the pictures (the cover, the lines, the ink over, sRGB with the cover, the filter's lobes, stretch
and weights, the clamp and the division), the emblems' limits and their bite, the harness's images, the manifest's
emblems. The first run: 66 killed, 4 survived; three were gaps, closed and killed in the rerun (`mutations-rerun.json`): a
colour's `#` unchecked, the CSS box observed instead of the device pixels' (every case had changed both: now a page zoom
that keeps the CSS box), a box of no size taken (chosen again before a frame paints: now an image shown again at once);
one can't differ: a tie of two sizes going to the smaller (the boxes are whole device pixels and no whole number lies on
the geometric mean of two neighbouring sizes: no product of neighbours is a square). **The standing list: 70, 69 killed,
1 that can't differ.** *The carousel's sheet* (`css-mutate.py`: `ui.pak` made again for each; the screens' comparisons, the
emblem screens' tests and, for the pointer's rule, `HeroScreenFixturesTests`): 7, 5 killed (the strip's picture taking
the pointer among them; the same rule on the medallion's picture showed itself redundant, the medallion clipping its
inside to its circle and having no hover or title, and was taken out with its mutation) (one after a gap closed: the medallion's picture now held to its inside exactly),
2 that can't differ: the medallion's and a strip medallion's picture in the flow instead of placed absolutely (each is its
medallion's only shown item, so the grid gives it the same box; the rule keeps that box free of the picture's own size,
156 and 61 px at 1.65, larger than the medallion). *The scripts* (`script-breaks.py`, each break through the check that
must notice): the builder and the outlines 12 of 12 (`make-humanoid.sh check` of one emblem, `chosen.py check`), the rule
7 of 7 (`palette.py check`, the game's tests reading `palette.py` and `look.py`); *the two tools* by hand
(`tool-breaks.sh`): `make-ui-pack.sh` without the pictures (the engine's `UiResourcesTests` and the game's emblem tests
fail), `make-game-screen-references.sh` without the pictures' files (the carousel's comparisons fail at 1 and 1.65), each put back
and made again (the pack the same bytes; a Chromium recapture of `races-playable-dark` at 1.65 came out 316 pixels at most 5
levels off the committed golden, all in one strip medallion: the hover the compositor's pointer gave it through its
picture's corner, since prevented, above). After the rebase on step 9a (which changed the manifest, the screens' test
harness and the family's scripts beside the builder) the C# list, the carousel's sheet and the scripts' breaks were run
again (`claude-work/gm2/step10-repo/rebase/`): the same, 69 of 70 (the tie), 5 of 7 (the two in the flow), 12 of 12 and 7
of 7; the two tools' breaks not again (neither tool nor what it reads changed). Nothing written to the player's real folders.

**Runs** (each full run under the shared lock in a headless cage, a scratch per-user folder, sound to the null device;
logs in `claude-work/gm2/step10-repo/`): the first on the figure's ground, on the GPU, found the strip's pictures taking
the compositor's pointer (five of `HeroScreenFixturesTests`' fixtures at 1.65; fixed above); then 4679 + 1035 = 5714
passed on the GPU and on lavapipe (`318af40` on `a6eb02a`: 4660 + 951 = 5611; `full-step10-gpu-2.log`,
`full-step10-lavapipe.log`). The final ones on the branch rebased on step 9a (`8c17686` and `a580250` on `d1da4b9`; only
the docs' words after them): the GPU 5285 + 1084 = 6369 passed (`full-final-gpu.log`), lavapipe 6369 passed
(`full-final-lavapipe.log`); `main` `d1da4b9` 5266 + 1000 = 6266, 103 new. After the rebase `make-blender-references.sh
--out` (10 min) and `compare_references.py`: 314 files byte-equal (step 9a's 278 and the emblems' 36), none different or
missing. The player's real folders the same before and after every run.

**Choices made (internal, recorded here):** the builder a module of its own beside the outlines (the package's `__init__`
kept free of Blender, so `palette.py` runs as plain Python); the outlines one element a line; `EMBLEM_SLOTS` a third list
in `look.py`; the rule's place in the game `Figures/Emblems.cs` with the pictures' camera beside it; the pictures made in the
game's tests from committed renders (the repo's way for generated data: the C# composer runs everywhere, the renders only
on the PC), over two grounds for the cover; the pictures named by their size in device pixels, one per place and scale as
laid out, chosen per image by a resize observer (the engine has no `srcset`, `image-set()` or resolution media query);
the nearest by ratio, the smaller side; "every size or none" for a mod's emblem; the strip's picture absolutely placed (a
class on its medallion); the harness's images; Chromium given the committed files; the maker's fixtures not recaptured.

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
- **2026-11-16, step 6 done** (the branch `gm2-step6`; "How it works", step 6): `save.game` v2 (the hero's sheet and look;
  version 1 frozen, upgrading to a new hero with no race and no look), the correction of a loaded character as the old
  game's `normalizeCharacter` (the user's Q45: silently; matching the old game on 102 cases of its test client), the
  maker's records `look.hero`, `look.body`, `look.part` (with `look.part.model`) and `look.preset` with their checks (what
  the maker couldn't make refused, a missing model refused: the manager's GM2-P18), the race's swatches checked and kept
  by the converter, the 72 old looks as the oracle's catalogue with the one-step writer that moves bodies and presets into
  `data.pak` when their models land. 4266 + 659 = 4925 tests on the GPU and on lavapipe, none held; mutations 114
  standing (109 killed, 5 that can't differ) and the oracle modules' 18 (killed). Found: the old function corrects more
  than Q45 named (the race's id, the EXP, a bonus that isn't finite: ported, "How it works", step 6), and nothing of the
  maker's could enter `data.pak` before its models (the mod check checks the game's records under every mod: GM2-P18).
- **2026-11-17, step 4a done** (a worker, the branch `gm2-step4a`; "How it works", step 4a): the manager's approved first
  humanoid in the repo: `Tools/Models/humanoid/`'s scripts (proven against his models within micrometres, equal counts),
  the human man and woman with their faces, hair and plain outfit, the chain shirt, the sword and the shield as ten models
  in `Assets/Models/humanoid/` and `models.pak`, the battle level's geared man in the tests' data, the game's look
  `EMBER_HEARTH`, the clip frame with its test clip `rest-sway`. The user approved the lighter figures (Q54; the manager's
  GM2-P19 and GM2-P20) and the detailed figures' limits (Q55, `ReferenceLimits.DetailedFigure`), under which the 14 scenes
  pass on both drivers. A geared figure 57,546 triangles in 23 primitives (the first figures about 152,000 in 100); sixteen
  of them 1.99 ms a 1080p frame on the GPU. 4364 + 659 = 5023 tests on the GPU and on lavapipe; mutations 22 of 22 killed, the scripts' breaks 26 of 26 (and the look's and the clip library's 43 of 43). 18 editor
  goldens captured again (the game's packs list `models.pak`; `list-marks` 760 px tall). **Engine tasks for later, not
  GM2's** (`Docs/Game-Plan.md`, "Engine items the port will meet"): the ink's depth allowance (`ink_edges.frag` draws a
  feature edge lying up to half a pixel, two edge-on, plus 1e-4 of the depth under what covers it, about 1.5–9 mm in the
  humanoid scenes; what shows because of it: the hairline's crease, the jaw's underside, lines along the locks, the mail
  sleeve's seam, more lines on the sword, the shield's rim edge-on; then the humanoid scenes measured again and the
  limits tightened); figures in the shadow pass (each drawn into the sun's 16 maps: sixteen figures 5.60 ms with a stage,
  1.99 without); skinning once per mesh instead of per primitive; a measurement on a real 2016-class card.
- **2026-11-17, the engine fixes done** (a worker, the branch `gm2-engine-fixes`; "How it works", "The engine fixes"):
  three of Blink's rules step 7 found on the sheet ported with their neighbours (words broken only where lines wrap; the
  spin button's room under any appearance of the field; a number line-height kept from the double), with
  `Ui/TextRulesReferenceTests` against Chromium; the old game's screens moved nearer, nothing farther; five differences
  found beside recorded, not fixed (the manager's word), one of them an engine task before the first screen with a
  number field that has a min and a max. Steps 7 and 8 rebase onto it. 4392 + 659 = 5051 tests on the GPU and on
  lavapipe (`main`: 4364 + 659 = 5023; 28 new); breaks 79, 78 killed, one that can't differ.
- **2026-11-18, step 7 done** (a worker, the branch `gm2-step7`, rebased on the engine fixes; "How it works", step 7): the
  character sheet on the engine, on GM1's page: the old page's markup with the hero's name as its heading and the race
  and level under it, the race seal (a placeholder emblem until step 10) in full colour, the paper doll round the dark
  portrait panel whose view stays empty until step 11, the whole Field tools rail with its tools disabled and its drawer
  under 840 px (`interface.js` line by line); every number the sheet's rules' (`ActorRules.Evaluate`, written as the old
  page writes it) and every control through its rule, saved with its sound; the sheet only for a hero with a race and a
  look (Q26; `open-races` asked otherwise), `open-sheet`. Approved by the user (Q46–Q49), the seal masked in the
  comparisons (Q50) through the fixtures' new mask hook; 13 fixtures in both themes and at 820 px, all passing with the
  engine fixes (the eight that waited at most 27 % of their limit). GM1's tests and `end_to_end.py` restated for Q26.
  4392 + 748 = 5140 tests on the GPU and on lavapipe (`main`: 4392 + 659 = 5051; 89 new); mutations 167 standing (165
  killed, 2 that can't differ) and the stylesheet's 25 (all killed); `end_to_end.py` 243 of 243 on both drivers, its two
  breaks caught. Found: the three engine differences fixed on `gm2-engine-fixes`.
- **2026-11-18, step 5 done** (a worker, the branch `gm2-step5`; "How it works", step 5): the game's look as the engine's
  `render.style` record (CelStyle's numbers and the sun under a look file's names) with the game's entry
  `render/styles/game` in `data.pak` ("Ember, hearth"); the figure builder (`HeroFigure`: a `look.hero` to one `Actor`,
  every missing thing it names refused); the first humanoid's bodies, four parts and eight human presets in `data.pak`;
  `assemble.py` reading looks against the data in Blender, six sample looks held to it under
  `ReferenceLimits.DetailedFigure` on both drivers (the woman's close-up nearest its limits, as in step 4a); the structural
  walk; every `surface` box masked in the game's screen fixtures. **The eight human presets draw their models' own
  placeholder colours until step 9a adds the human race's swatches and the presets' colours** (the manager's answer,
  2026-11-18); no comparison or golden holds them as a look. Found: a view with no scenery runs no shadow pass but
  allocates its 64 MiB shadow atlas (an engine task, GM2-P16's memory); the engine lays a `surface` out as a replaced
  element (300 × 150, children not drawn) where Chromium's is an unknown inline element. 4425 + 796 = 5221 tests on the
  GPU and on lavapipe (`main`: 4392 + 748 = 5140; 81 new); mutations 78 standing, 77 killed, one that can't show on this
  branch; `assemble.py`'s breaks 16 of 16; 18 editor goldens captured again (`data.pak`'s new folders).
- **2026-11-18, the engine fix for step 8's race strip done** (a worker, the branch `gm2-round-borders`;
  `Docs/Compositing-Port.md`, "Thin rounded borders"; "Steps", step 8): thin rounded borders, outlines and the theme's
  frames as Chromium paints them, with the background shrunk under a rounded border, an opaque box's shadow cut a pixel
  inside it and radii constrained to the snapped box; the strip's page 2,715 → 11 pixels past 12 levels (light at 1). The
  sheet's screens moved nearer (`sheet-played-light` 848 → 214 pixels past 12 levels at 1, `sheet-dev-light` 2,888 → 1,676
  at 1.65; the port's 150 screens that moved, 149 nearer and one farther by 14 pixels, a dark settings screen over the
  theme's light checkboxes). Step 8 rebases onto it.
- **2026-11-18, the ink's engine task done** (a worker, the branch `gm2-ink-cover`, rebased on steps 7 and 5 and the
  round borders; "How it works", "The ink's cover"): what step 4a put down to the feature edges' allowance was mostly the per-pixel crease
  meeting two pieces of one part (step 4a's note corrected); each sample now knows its piece (sheet), an edge under
  another piece is hidden as Freestyle hides it, the crease is drawn only within a piece and the meshes' silhouettes draw
  a piece's contour lying on another. The 14 humanoid scenes and step 5's six looks nearer Freestyle in every scene on
  both drivers (worst missed 2.50 → 0.09 %, extra 7.68 → 1.82 %, unlike 12.71 → 7.56 %), every other comparison under its
  own limits, no golden changed; the detailed figures' limits tightened by the user's Q58 and then Q60 (unlike ink 8.3 %;
  extra, missed and coverage M6's). The frame's cost on the RX 9070 (under the lock): 16 geared figures at 1080p 1.61 →
  1.72 ms (+7 %), with the stage 5.02 → 5.13 ms. 4472 + 796 = 5268 tests on the GPU and on lavapipe (`main` `0eca9e5`:
  4465 + 796 = 5261; 10 new, 3 bite rows fewer). Mutations 25, 20 killed, 5 that no test tells apart (accounted for in
  `Docs/Renderer-3D.md`, Status).
- **2026-11-19, step 8 done** (the branch `gm2-step8`; "How it works", step 8): the race carousel and the character
  maker, approved by the user as captured (Q51, Q52), a playable race with no looks shown but not choosable (Q53): the new
  hero's flow from GM1's name to `open-sheet` (`open-races`, `open-maker`), the race and the look saved together, Dev
  Mode's enemy-only races taken at once, the random roll on GM2-P15's `RandomPicks`, the sounds of GM2-P17; the views a
  seam for step 11 (none in the program yet: empty panels, the ✦ for the emblem); 15 fixtures against Chromium (the views'
  boxes masked since step 5), on test data without the shipped looks,
  each through the real window; Q26's rule one with step 7's (`HeroFlow.Needs`, which the sheet asks). 4472 + 948 = 5420 tests on the GPU and on lavapipe
  (`main`: 4472 + 796 = 5268; 152 new). Found: the
  headless compositor's pointer and a script's focus after a mouse press
  bear on a fixture's picture; the strip's approved 1 px round borders differed past the limit at 1 in Dev Mode's light
  fixture (2,949 pixels, the engine's antialiasing of thin round borders; the inset rings tried instead weren't the
  approved picture at 1.65): fixed first on `gm2-round-borders`, the manager's word of 2026-11-18, now 160; the root
  scrollbar's thumb is about 15 px shorter in the engine than in Chromium (both engine findings in `Docs/Game-Plan.md`,
  "Engine items the port will meet"; the thumb listed, not fixed).
- **2026-11-19, step 4b done** (a worker, the branch `gm2-step4b`; "How it works", step 4b): the first humanoid's 34
  approved clips in the repo as data and as the family's library in `models.pak`, held to the hand-over's list, to the
  rules they were keyed by, to the ground and to Blender on both bodies; six posed figures under `DetailedFigure` on both
  drivers, two more owed to GM2-P24's task; the figures' small state machine (`stand` looped); the figure mode's play list
  and label; the playing view for the user. The step's measuring sent the first set back once (the boots' toes up to 69 mm
  under the ground, the fallen body 66 mm over it; the clips keyed at every frame bending between frames in Blender), the
  manager re-keyed, and the user approved `death` and `meditate` again, then the playing view ("Yes, it plays right",
  2026-11-19): every approval stop of step 4 passed. Recorded: the user's Q56, Q57, Q59, the manager's GM2-P21–GM2-P24.
  Found: a body's soles are 24 mm under its origin (GM2-P22's "figure's ground"); the woman's fallen body 24 mm under her
  soles' level until the hips' place is scaled. The six posed samples measured again after the ink's task, inside Q60's
  limits (the nearest death's unlike ink, 7.06 % of 8.3 %). 4645 + 948 = 5593 tests on the GPU and on lavapipe (`main`
  `1768286`: 4472 + 948 = 5420; 173 new); mutations 54 of 54 killed.
- **2026-11-20, GM2-P22's engine task done** (a worker, the branch `gm2-figure-ground`; "How it works", "The figure's
  ground"): a clip's hips' place follows the playing body's legs (the clips measured by the left leg; the woman 0.968 of
  the man; exactly 1 on him, nothing of his clips' references changed) and a figure stands by its soles (the lowest point
  at rest of its body and garments, −23.997 mm on both human bodies), in the engine, the game's figure builder, the
  figure mode and Blender's side. The woman's lowest point at most 2.43 mm under her ground in any clip (was 24.11 at
  death's end), within the man's bounds, which the ground test now holds her to; her 30 evaluations made again by the
  rule, the figure scenes made again standing by the soles (the same pictures), a seventh assembled sample (the woman
  kneeling). Every later body, the smallest and largest included, plays the clips by this rule (Q59: the user sees it in
  step 9). 4660 + 951 = 5611 tests on the GPU and on lavapipe (`main` `d2752e5`: 4645 + 948 = 5593; 18 new); mutations
  32 of 32 killed (one survivor of the first run closed by a test), Blender's side 7 of 7.
- **2026-11-21, step 9a done** (a worker, the branch `gm2-step9a`, rebased on the figure's ground; "How it works", step 9a):
  the manager's approved art of the shared sets and the human in the repo: the twelve script files (the hair beads moved
  into the outfit's trim, one word, GM2-P28, agreed), 152 new models (171 in the manifest, the same bytes on every build,
  twice) in `models.pak` (2,170,661 → 10,425,256 bytes), the human's 69 swatches, six bodies, 82 parts under the approved
  names in the approved order and the eight presets in `data.pak` (10,653 → 29,642 bytes); the user's Q65 as
  `look.part.model` version 2's `faces` (a mark's model by face, in the checks, the offers, the figure builder,
  `assemble.py` and the maker), the manager's GM2-P29 in the maker (a colour offered only where the body or a chosen part
  uses it) and GM2-P36 (`look.part` version 2's `order`: each list in the approved order, "None" first), the user's Q73 (a
  colour of one swatch shows no row); the presets held to the old looks by a table of names; the walk over every body,
  option and face, the human coloured; 18 samples against Blender (GM2-P30), two close-ups owed to an engine task
  (GM2-P35). Recorded: the user's Q61–Q65 and Q73 and the approvals, the manager's GM2-P25–GM2-P30, GM2-P35, GM2-P36.
  4660 + 951 → 5266 + 1000 = 6266 tests, on the GPU and lavapipe. Found: the hair beads in a gear's slot (the walk); the hair locks' seams up close (GM2-P35); the thicker builds
  lying and kneeling under the man's ground bounds (measured, for the manager with Q59); the woman's twin braids' hair cap
  one normal off now and then when Blender builds it under load (`make-humanoid.sh check` flaky on it).
- **2026-11-22, step 10 done** (the manager's art, 2026-11-20: the motifs chosen, Q66, the medallions approved as renders,
  Q67; the repo's part by a worker on the branch `gm2-step10`, rebased on step 9a; "How it works", step 10): the nine emblems' builder and
  outlines in `Tools/Models/humanoid/emblems/` (the models byte for byte the manager's), in `models.pak`; their colours'
  rule in the game (`Figures/Emblems.cs`, with `Emblems.PaletteOf(data, race)` for step 11's live emblem) and in the
  scripts (`palette.py`), held alike; their pictures (ten sizes as the screens lay them out, from the 640 px renders scaled
  down, GM2-P34) in `ui.pak`, shown by the sheet's seal and the carousel's medallion and strip where the ✦ stood (Q71;
  the dark seal's sepia kept, Q72); held to Blender under `ReferenceLimits.Emblem` (Q70: the engine draws a line where a
  raised outline rises from the field, Freestyle doesn't; an engine task). Found: the seal's picture is 113.76 px, not the
  design's 124 (step 7's note corrected); a square picture in a round medallion takes the pointer at its corners. 5266 +
  1000 → 5285 + 1084 = 6369 tests on the GPU and on lavapipe (`main` `d1da4b9`; 103 new); mutations 69 of 70 killed (a tie
  of two sizes can't differ), the carousel's sheet 5 of 7 (two can't differ), the scripts 19 of 19, the tools 2 of 2.

# Statventure engine plan

Statventure is the engine for the BRUTAL port of Stat Fick. It's built on BRUTAL (C#, Vulkan 1.3).
The engine is finished, fully tested, before the game is ported onto it.

## How to continue (read this first)

**Current task (2026-11-08): M12 (packaging) started on the user's go ("Then start with M12"); its twelve questions
(M12-1–M12-12) and the detail round its design doc raised (Q1–Q28, and the M12-9 clarification) are answered ("Decisions
for M12"), and so are the two questions Q11's answer (the BRUTAL logo splash and credits now) raised, Q29 (the
splash) and Q30 (the credits view); nothing is open. The design doc is `Docs/Packaging.md`, and its eight steps, in
three waves, are the plan, none started. The rule to ask stands in full again in M12, the editor's packaging included (M11's exception
was for M11's editor only).** No code of M12 yet. M11 (editor foundation) is done and ticked: `main` is the merge of `m11-step14` (step 14, on `00e5bd4`, "M11
fixes after step 14").
**All fourteen steps of M11 are on `main`** (Progress, "M11 step 1" to "M11 step 14", "M11 fixes after step 14" and "M11
done"; `Docs/Editor.md`, Status): record shapes, drafts and marks (step 1), packs for the editor (2), the window, sound
and file-browser hooks (3), the shell (4), the audio kinds' checks and marks (5) and the other kinds' (6), the mod's info
entry (7), opening packs, the browser and the entry list (8), the form, undo and the JSON view (9), checks and references
(10), saving and mods (11), import (12), the preview (13), and the whole editor end to end (14), with the fixes its flow
called for (New entry in the top bar and Ctrl+N; Enter in the import dialog; the empty Stand-ins heading). **The full
suite on `main` is 3496 tests, passing on the GPU and on lavapipe, nothing held** (2026-11-08, inside a headless
Wayland compositor under the shared lock; "How each step is finished", item 1). M11 ran from 2026-11-06 (the user's go,
after the full run on `a32c5cb`) to 2026-11-08 under **the user's rule for M11** (questions about the game to the user,
the editor's own design the manager's, big decisions told; "Decisions for M11"): the editor's choices P1–P38 and each
step's questions were the manager's, the game's questions G4 and G5 the user's (the game enforces declared limits; the
play-time checks also run at load, their cost measured and accepted for the music and the cutscenes on 2026-11-07), G1
and G6 the manager's, G2 and G3 kept for the port ("Open questions for later milestones", "At port time"). What M11 leaves
for the port is below ("What M11 leaves for the port"). **Open, none of it blocking M12, each waiting for the user's go:**
the by-hand checks for the user on Linux, (a)–(e) in "Small tasks left from earlier milestones" (the editor in a real
window, real drops and closes, the preview's sound by ear, saving, and step 14's mod by hand); the small tasks with no go
yet in the same list (the music switch during a fade-in; the proof cutscene's sounds by ear; the engine findings of M11:
control labels a device pixel low at 1.65, text grayscale at 1.65 in a composited box, under an editor dialog over a
composited scroller and in a status line beside a tall scroller, a select with its own padding wider than Chromium's, the
painter painting every row of a scroll container, a textarea's `line-height`, a positioned box with a z-index inside a
scroller, a stacking context on the form's rows, `ValueShape.TextOf`, a stop for sound effects); the Windows checklist
("Windows check" below, items 1–8, item 8 M11's, completed at M11's end); and the housekeeping list (finished workers'
worktrees and branches). **Next: M12's steps** (`Docs/Packaging.md`, "Steps": wave 1,
side by side: 1 the version and invariant globalization, 2 logs and crash files, 3 publishing, the archives and their
check with `editor.ps1`, 4 the engine's embedded Lato and BRUTAL logo with the splash and the credits as engine pieces;
wave 2: 5 the splash before `Load` and the crash window (after 2 and 4), 6 the editor as a download
of its own (after 1–4, rebased on 5 for one call), 7 the release script and its text (after 1 and 3); wave 3: 8 end to end with the Windows items).
Before M11: M10 (scene and sequencer) done 2026-11-05 (the proof cutscene approved by the user), M9 (assets and data)
2026-11-03, M8's music listened to and approved 2026-11-02 (Progress). Nothing since 2026-10-30 has run on Windows
("Windows check"). The project is one git repo (`../`, see `../CLAUDE.md`), on the private GitHub repo
`github.com/Vatrix33/statfick`; the user's tasks for the port are in `Docs/Port-Tasks/`, read when the port starts
("Decision on the user's tasks for the port"; "Open questions for later milestones", "At port time"); tasks 1–9 are done
in the old game (the paragraphs below).
**Beside the engine, old game only (2026-11-06): port task 2, Guild Rework, is done in the old game**, as task 1 was
(the user's answer G1, "Decisions for task 2, Guild Rework" below; built on the cloud branch `claude/fervent-carson-f5ol9s`
and merged through a pull request). The port doesn't build it again; only its two guild interiors, placeholder art, are
remade (`Docs/Port-Tasks/README.md`). It touches only `../old-game/` (its scripts, markup, stylesheet, Courier's class file and
test client) and these docs, never the engine: the engine's tests read from the old game only its fonts,
`classes/Black Knight.json` and the image and SVG files their saved captures name, none of which changed, and use their
own copy of the stylesheet, so `Tools/Scripts/test.sh` was not run for it (the cloud session had no .NET). **Engine work
goes on uninterrupted** (M11 on the user's go, the background-layer branch); don't wait for task 2, and don't count it
as the engine's current task. How it's built, its checks and their results: `../old-game/Docs/Notes.md`, "Guild
Rework"; `Docs/Port-Tasks/README.md` lists its placeholder art.
**Beside the engine, old game only (2026-11-06): port task 3, Shop Rework, is done in the old game**, as tasks 1 and 2
were (the user: "same rules apply"; decisions R1–R29 in "Decisions for task 3, Shop Rework" below; built on the cloud
branch `claude/fervent-carson-f5ol9s`, merged through a pull request). The port doesn't build it again; only its
placeholder art is remade (`Docs/Port-Tasks/README.md` lists it). It touches only `../old-game/` (its
scripts, markup, stylesheet, consumables and test client) and these docs, never the engine (the engine's tests read
none of what changed), so `Tools/Scripts/test.sh` was not run for it. **Engine work goes on uninterrupted**; don't count
task 3 as the engine's current task. How it's built, its checks and their results: `../old-game/Docs/Notes.md`, "Shop Rework".
**Beside the engine, old game only (2026-11-06): port task 4, Artifact Rework, is done in the old game**, as tasks 1–3
were (decisions A1–A32 in "Decisions for task 4, Artifact Rework" below; built on the cloud branch
`claude/fervent-carson-f5ol9s` and merged through pull request #10, with task 6, on 2026-11-06). The port doesn't build it again; only
its placeholder looks are remade (`Docs/Port-Tasks/README.md`). It touches only `../old-game/` (its scripts, artifact
data and test client) and these docs, never the engine (the engine's tests read none of what changed), so
`Tools/Scripts/test.sh` was not run for it. **Engine work goes on uninterrupted**; don't count task 4 as the engine's
current task. How it's built, the approved list, its checks and their results: `../old-game/Docs/Notes.md`, "Artifact
Rework".
**Beside the engine, old game only (2026-11-06): port task 6, New Areas, is done in the old game**, as tasks 1–4 were
(decisions N1–N10 in "Decisions for task 6, New Areas" below; the Abyssal Rift in Gloamfen Marsh and its six enemies;
built on the cloud branch `claude/fervent-carson-f5ol9s` and merged in the same pull request as task 4, #10, on 2026-11-06, N8). The port
doesn't build it again; only its placeholder looks are remade (`Docs/Port-Tasks/README.md`). It touches only
`../old-game/` (its scripts, enemy files and test client) and these docs, never the engine (the engine's tests read
none of what changed), so `Tools/Scripts/test.sh` was not run for it. **Engine work goes on uninterrupted**; don't count
task 6 as the engine's current task. How it's built, its checks and their results: `../old-game/Docs/Notes.md`, "New
Areas".
**Beside the engine, old game only (2026-11-06): port task 5, New Masterworks, is done in the old game**, as tasks 1–4
and 6 were (decisions M1–M26 in "Decisions for task 5, New Masterworks" below: Sparkling enemies, Treasure Seeker,
Moneyshot, Lament, Noble Hero, the Hexbane Coat and the Shadow Crystal drop; built on the cloud branch
`claude/fervent-carson-f5ol9s` and merged through pull request #11, one for the whole task, on 2026-11-06, M25). The port doesn't build it
again; only its placeholder art is remade (`Docs/Port-Tasks/README.md`). It touches only `../old-game/` (its scripts,
stylesheet, artifact data, one enemy file and test client) and these docs, never the engine (the engine's tests read
none of what changed), so `Tools/Scripts/test.sh` was not run for it. **Engine work goes on uninterrupted**; don't count
task 5 as the engine's current task. How it's built, its checks and their results: `../old-game/Docs/Notes.md`, "New
Masterworks". It also makes Doctor's Orders (task 3) the white robe R11 asked for: masterwork armor now wears its
recorded colour.
**Beside the engine, old game only (2026-11-06): port task 7, New Game Plus, is done in the old game**, as tasks 1–6
were (decisions L1–L33 in "Decisions for task 7, New Game Plus" below: Limitless, the Exotic rarity, Exium and the five
Exotics; built on the cloud branch `claude/fervent-carson-f5ol9s`, one pull request, L22, merged as #12 on 2026-11-06,
`0d8e175`). The port doesn't
build it again; only its placeholder art is remade (`Docs/Port-Tasks/README.md`). It touches only `../old-game/` (its
scripts, markup, stylesheet, eleven boss files and test client) and these docs, never the engine (the engine's tests
read none of what changed), so `Tools/Scripts/test.sh` was not run for it. **Engine work goes on uninterrupted**; don't
count task 7 as the engine's current task. How it's built, its checks and their results: `../old-game/Docs/Notes.md`,
"New Game Plus: Limitless and the Exotics".
**Beside the engine, old game only (2026-11-07): port task 8, Map Rework, is done in the old game**, as tasks 1–7 were
(W1; decisions W1–W35 in "Decisions for task 8, Map Rework" below; built on the cloud branch
`claude/fervent-carson-f5ol9s`, one pull request). One continent laid out as the user's approved draft 6 (W26, kept with
its scripts and every region's place in `Docs/Port-Tasks/8_Map_Rework/`), every region whole and 1.4 times larger, a
natural coast with two inland waters and no barriers between neighbours, Cinder Isle 250 km out with the 12.6-hour boat,
fog over regions not yet entered, walks across regions (off the roads only through entered ones, W30), the music's
crossfade, the boat trip on the map, the mountain's fog and clouds, the caravan and the horse carriage. Its three new
regions are empty in the old game (walkable, no encounters, parchment marked "Uncharted") and are made in the port
(W27–W29). The port doesn't build it again; left for it: the map's placeholder looks, remade in its 3D map, and the three
new regions (`Docs/Port-Tasks/README.md`), and the region crossfade for the engine's soundtrack, which switches themes
as music.js did before task 8 (`Docs/Audio.md`, "Theme API"). It touches only `../old-game/` (its scripts, markup, stylesheet and test
client) and these docs, never the engine (the engine's tests read none of what changed: the fonts, `classes/` and the
images their captures name are untouched), so `Tools/Scripts/test.sh` was not run for it. **Engine work goes on
uninterrupted**; don't count task 8 as the engine's current task. How it's built, its checks and their results:
`../old-game/Docs/Notes.md`, "Map Rework: one continent".
**Beside the engine, old game only (2026-11-07): port task 9, Potion Rework, is done in the old game**, as tasks 1–8
were (decisions P1–P31 in "Decisions for task 9, Potion Rework" below; on the cloud branch `claude/fervent-carson-f5ol9s`,
in the same pull request as task 8, P1). The Apothecary in every settlement (potions, ingredients, brewing, recipes,
selling), 40 new potions and 12 poisons, 56 ingredients (foraged and dropped), 62 recipes, the Alchemist and the
Assimilator, and the guild's Collect quest; the approved content is `Docs/Port-Tasks/9_Potion_Rework/draft.md` (P31).
The port doesn't build it again; only its placeholder art is remade (`Docs/Port-Tasks/README.md`). It touches only
`../old-game/` (its scripts, data, two new class files, markup, stylesheet and test client) and these docs, never the
engine (the engine's tests read none of what changed: the one class file they read is untouched), so
`Tools/Scripts/test.sh` was not run for it. **Engine work goes on uninterrupted**; don't count task 9 as the engine's
current task. How it's built, its checks and their results: `../old-game/Docs/Notes.md`, "Potion Rework".
**Beside the engine, old game only (2026-11-11): port task 11, New Races, is done in the old game** for its races, and
in task 10's drafts for its classes (T1). Half-Beast (balanced feral stats, Natural Claws) and Goliath (mighty and slow,
Giant's Blood) are playable, and so are the Merfolk (the enemies' stats and passive kept); each has a placeholder seal,
four male and four female placeholder looks and recruit names (T5, T7). Their racial classes, Wildheart, Deepcaller and
Peakwarden, are at the end of both drafts in `Docs/Port-Tasks/10_Class_Rework/` (173 classes there now). Decisions
T1–T17 in "Decisions for task 11, New Races" below; where each rule lives, the checks (`new-races-browser.js`, 22 rules
each shown able to fail) and the regression (148 of 162 pass; the 14 others fail on the code before it too or are the
known intermittent ones): `../old-game/Docs/Notes.md`, "New Races". The engine's tests read only the six older race
seals, which didn't change, so `Tools/Scripts/test.sh` was not run for it. **Engine work goes on uninterrupted**; don't
count task 11 as the engine's current task.
**Port task 10, Class Rework, is designed and approved, for the port (2026-11-10 to 2026-11-11, a cloud session).**
Unlike tasks 1–9 it is **not built in the old game: the port builds it** from the approved drafts (C23, which replaced
C1), now in `Docs/Port-Tasks/10_Class_Rework/` (C31; its README says what each file holds); during porting every
class is inspected for errors and inconsistencies, and after the port every class feature is tested (C32): `Class-Rework-Draft.md`,
every class's identity, levels, requirements and rarity (130 current, 40 new; 109 without a colour, 42 blue, 13
purple, 5 gold, 1 red), and `Class-Rework-Features.md`, every class's level-by-level features (a sample of four, then
eleven batches, each approved by the user, with their notes applied). The rules they follow: every class reviewed
(C3), one passive and one skill per class but the elemental mages, the item classes, the Vampire Lord, the Ki Warrior
and the Psykic (C21, C24, C26, C27), the enemy-only classes outside the rules (C25), the passive or skill at the last
level (the task's capstone rule), requirements and skills within a turn's 5 AP (C28). All answers: C1–C32 in
"Decisions for task 10, Class Rework" below. Left for the port: building the classes, the red rarity's look (C16), the
full rework of every enemy that uses classes (C2), and the "(new)" mechanics the features file marks. Nothing changes
in the old game. **Engine work goes on uninterrupted**; don't count task 10 as the engine's current task.
**Port task 12, Spell Rework, is being designed for the port (from 2026-11-11, a cloud session),** like task 10: not
built in the old game (SP1); drafts in `Docs/Port-Tasks/12_Spell_Rework/` (SP21). The spells get a system of their
own, Spell Augmentation (SP9–SP16): heroes pay at the Magic Towers (less at the Institute of Magic and, for Lightning,
the Church of Talos) to raise a spell's attributes, up to 5 ranks each and 10 per spell; only the Spellweaver changes
elements, scalings and tiers. Measured first (SP8), the curve keeps Mana as scarce as now, puts all-enemies spells at
75% and raises the high tiers (SP17–SP22). Where it stands: the system draft, `Spell-Augmentation.md`, waits for the
user's review; then a sample of four spells, one batch per element, the 168 new spells and the two classes (SP2,
SP3, SP15); the menu's look is asked last (SP23). Decisions SP1 onward in "Decisions for task 12, Spell Rework" below.
**Engine work goes on uninterrupted**; don't count task 12 as the engine's current task.
**The full run after the merges (2026-11-06, on the PC):** the paragraphs above are the cloud session's, which ran no
engine tests. With pull requests #8–#11 merged (`main` `a32c5cb`), `Tools/Scripts/test.sh` gives 2547 tests, all
passing, none skipped, on the GPU (1 min 10 s) and on lavapipe (1 min 25 s), as on `7fd3aac`; the player's real game
folder stayed missing and the Documents folder's top level unchanged. Checked in the code first: the tests read from
`../old-game/` only the five fonts in `objects/fonts/` (`UiResourcesTests`), `classes/Black Knight.json`
(`LooseRecordTests`) and the six race SVGs in `objects/` that the game-screen captures name (`GamePaintTests`,
`CompositingReferenceTests`), none of which changed in `1c71213..a32c5cb`; `test.sh` and `build.sh` read nothing there.
The game screens are tested against the old game's looks as captured (the old game has changed since; "At port time"
below).
Before each later milestone (M11 on) starts, first ask its questions under "Open questions for later milestones"
below (all at once, with the question tool), record the answers, and only then plan and write code, in the order
"Starting M9" below gives (M10 followed it too).
Merging (user decision 2026-11-02): workers in git worktrees (`.claude/worktrees/`) commit on their own branch, and a
separate, non-isolated merge worker merges into `main` and pushes, because the harness refuses git commands aimed at the
main checkout from a worktree.

**Starting M9 (done; M9 finished 2026-11-03):** it started on 2026-11-02 with the user's go, in the order M10 followed
too (2026-11-04), M11 (2026-11-06; its design doc `Docs/Editor.md`, under the user's rule for M11) and M12 (2026-11-08;
its design doc `Docs/Packaging.md`, its twelve questions asked first, the details its survey raised after): `git pull` and read; check the code against the facts and work out what each question needs deciding; ask
the questions all at once (the question tool takes 4 per call, 2–4 options each, the recommended one first); record the
answers ("Decisions for M9"); write the design doc (`Docs/Assets.md`: decisions, what exists checked against the code,
method, design, steps, how it works, status) and split the milestone into steps; do each step as "How each step is
finished" says. Its survey of what existed, the questions the survey added and its method are kept in `Docs/Assets.md`
("Decisions", "What exists", "Method").

**Small tasks left from earlier milestones (recorded 2026-11-02; done 2026-11-03 on the user's go, a cloud session, except
what needs the user's machines):**
- Window host: send `visibilitychange` and `pagehide`. **Done (2026-11-03):** it grew past a contained change once
  Chromium's behaviour was captured: the user decided (see "Decisions for the window's visibility") that a minimized
  window's document runs as Chromium runs a hidden page, so the engine now has hidden documents (no frames, Blink's timer
  throttling with its wake-up pools, the intensive throttling, the CPU-time budget and the audible exemption), the unload
  sequence on close, minimizing through GLFW's iconify and the application loop, and the sound's audibility
  (Docs/Scripting.md, "Hidden documents"; Docs/Audio.md, "Audibility"). Checked against a new Chromium harness
  (`Tools/VisibilityReference`) and in a real window under a window manager. Not checked: Wayland (GLFW never reports
  minimizing there; the user accepted that) and Windows.
- Repo housekeeping: **done (2026-11-03)**: the root `.gitignore` ignores `/.claude/worktrees/` and
  `/.claude/settings.local.json` (the user chose these over the whole `.claude/` folder).
- Windows: open; it is its own checklist now, "Windows check" below.
- Stale paths: `Docs/Notes.md` said `Stat Fick Source/Docs/Engine-Plan.md` in two places (text copied from the old memory;
  the folder is `engine/` now); fixed on 2026-11-02, and the last two (the "Asking about design choices" section and the
  2026-10-25 entry's `Compositing-Port.md`) on 2026-11-03.
- Not checked here, needs hardware or a window manager: audio on a really different device, a device lost or appearing, a
  real window minimize (`Docs/Audio.md`, Status).
- Music switch during a fade-in (found 2026-11-04 in M10 step 6; open): music.js's switch made exactly at a quantum's
  start while the old theme is still in its 0.8 s fade-in cuts the old theme within a quantum instead of fading it
  (`cancelAndHoldAtTime` at the current time prunes the ramp's first event, Blink's rule ported line for line in M8;
  the game's own `Music.Play` and a scheduled switch on a quantum's start both do it, one inside a quantum fades). Not
  checked against Chromium (the M8 fixtures switch only after a fade-in): check against a Chromium render whether
  music.js does the same; if not, find the rule (`Docs/Audio.md`, "The clock and scheduled calls", "Found"). No go yet.
- The proof cutscene's three sounds by ear (open, recorded 2026-11-05): the rift (glassy crack and sub drop), the flash
  (thunderclap) and the title (deep toll), the kinds the user chose on 2026-11-05 (S7-9–S7-11), have not been judged by
  the user's ear for level, length and the thunder's delay to the flash's peak (their levels and lengths are first
  guesses). `Tools/Scripts/play-cutscene.sh jiruan [--volume 0-100]` plays it. Standing rule: when a script or a session
  plays audio, it sends only volume 0 (it is audible on the user's headset); listening is the user's, in a real terminal.
- By-hand checks for the user on Linux (open, recorded 2026-11-07, no go yet: the user decides when; no window may be
  shown on the PC from a session, so these are the user's, in a real terminal): (a) `engine/Tools/Scripts/editor.sh`:
  the editor's shell in a real window (M11 step 4): the window and its title, the zoom keys (Ctrl+= and Ctrl+-) through
  the host (`UiHost.PageZoom`), a splitter dragged with the mouse, and its cursor (`Docs/Editor.md`, "How it works",
  step 4, "Not checked"); (b) `~/.cache/stat-fick-build/claude-work/m11/drop-check.sh` (the session's window system,
  Wayland on the PC) and `drop-check.sh x11` (X11 through XWayland), a window that logs drops and closes (M11 step 3): a
  real drop of files from a file manager (the paths printed), closing by the title bar (the first close refused, the
  next ends it), and closing from the taskbar while minimized (`Docs/Editor.md`, "How it works", step 3, "Not checked
  here (by hand)"). The same on Windows: "Windows check", item 8. (c) The editor's preview (M11 step 13; `editor.sh`, in
  a real terminal, at a volume of the user's choosing: the editor's volume starts at 50 %): a sound effect played with
  its play's volume, pan and pitch, a theme played, edited while it plays (it restarts through its fades) and stopped,
  Space from the entry list; a model turned with the mouse and zoomed with the wheel, its clips played, a stage's camera
  looked through; a particle effect looping; a cutscene with stand-ins played, paused and sought (`Docs/Editor.md`, "How
  it works", step 13, "Not checked here"). (d) Saving (M11 step 11; `editor.sh --player-folder <a scratch folder>`, so the
  mods made stay out of the real mods folder): New mod (its name and file name, the mod's info in its own form), an
  entry edited and Ctrl+S (the title's "•" and the list's marks go), Save As with some entries unticked (the browser,
  the new file open after), and closing with unsaved work (Save, Discard, Cancel) by the title bar (`Docs/Editor.md`,
  "How it works", step 11). (e) Step 14's mod by hand (`editor.sh --player-folder <a scratch folder>`, the copy of
  `~/.cache/stat-fick-build/claude-work/m11/step14/ember-rite.pak` in its `mods/`): New entry by the top bar's button and
  by Ctrl+N, a reference picked from its list with the mouse, the JSON tab, the cutscene `world/cutscenes/rite` previewed
  and its chime heard at a volume of the user's choosing (`Docs/Editor.md`, "How it works", step 14). The same on
  Windows: "Windows check", item 8 (gathered in `Docs/Editor.md`, Status).
- A stop for sound effects (open, recorded 2026-11-07 by M11 step 13; no go yet): the engine's `SoundEffects` has
  `Register`, `Play` and `PlayAt` only; a voice once started plays to its end (`ScheduledSound.Cancel` works only before
  its time), so the editor's preview can't cut a sounding effect with Stop (P31; the manager's decision Q2 of 2026-11-07:
  in M11 Stop stops a theme playing and a sounding effect ends by itself). To add, by the audio's owner: a stop for the
  effects (e.g. `SoundEffects.StopAll()`, every voice faded out over the steal's 5 ms), which the preview's Stop then
  calls (`Docs/Editor.md`, "How it works", step 13).
- Housekeeping (open, needs the user's go; recorded 2026-11-05; checked against `git worktree list` and `git branch` on
  2026-11-08 at M11's end, listing only what is there): every worktree under `.claude/worktrees/` is a finished worker's
  whose work is on `main` (the docs-only ones superseded) and can go, after `git worktree unlock` for the locked ones.
  From before M11: `agent-a100365f9dd9f6455`, `agent-a854dc507294edc31` (`shadow-inset-caster`), `agent-a90b2ab8c1e10ed5b`,
  `agent-ab2955e985eacee1d`, `stray-saves` (locked), `agent-a2e8de9d65afa90cf` (`docs-2026-11-05-evening`) and
  `agent-a93b8e77b1ce8d3b9` (`docs-m11-answers`). From M11 and the work beside it: `agent-a7af00a2756e64974`
  (`m11-step2`), `agent-a426936d050e88bec` (`m11-step4`), `agent-a1ec0e500ee3cb5ea` (`m11-step1`),
  `agent-a6537232e954bf00b` (`m11-step7`), `agent-aec1ea5dc55ab019d` (`m11-step3`, locked), `agent-a4031ddbb1001df19`
  (`m11-step5`, locked), `agent-a5af0bef78dd7cefd` (`m11-step6`), `agent-adb75d946c33c35be` (`m11-step9`), `m11-step8`
  (locked), `m11-step12` (locked), `agent-a5fd483069282c022` (`m11-step13`), `agent-a02497cf032af0aed` (`m11-step10`),
  `agent-acf904db51cc4ffe5` (`m11-fixes-1`, locked; it held step 11's branch `m11-step11` before), `agent-ac865c716354051df`
  (`m11-step14`, once merged), `agent-abcc33021821690f3` (`lcd-first-text`), `agent-a55b0fabf44a865d5`
  (`background-layers-aa`, locked), `agent-a8c8ea516c01d3cb0` (`flaky-script-api-task`, locked), `agent-a6d967f5c503ec975`
  (`m11-design`), `agent-a4bb2c94143be5ce9` (`docs-after-port-tasks`) and `agent-a36386867198a2222` (`docs-m11-midway`).
  With each `agent-<id>` worktree goes its own branch `worktree-agent-<id>`; and the branches with no worktree:
  `m11-step11`, `worktree-agent-a7ff929d77cdfabea`, `worktree-agent-aff23c463ed886502`, and the backups git doesn't count
  as merged (work in progress kept before a squash): `m11-step6-wip-backup`, `m11-step10-wip`, `m11-step10-wip2`,
  `m11-step13-wip-backup`, `step7-unsquashed-backup` and `paint-offset-rounding`.
- Control labels a device pixel low at 1.65 (found 2026-11-06 by the text-verdict fix, Progress, "Text first in a paint
  chunk"; open, no go yet: the user decides when): a select's or an input button's label sits one device pixel below
  Chromium's at a device scale of 1.65 (at 1× they match). In a 20 px tall `appearance: none` select or button with Lato
  13 px, 897 (button) and 946 (select) pixels of the control's 170 × 20 px row are past 12 levels, worst 198; the text
  is one device row off (moved up a row, the select's row differs in 157). Suspected cause: `UiPainter.SnapBaseline`
  (`UI/Paint/UiPainter.Controls.cs`; also used by the text fields and `FieldGeometry`) puts the baseline on a whole CSS
  pixel, `MathF.Floor(top + FirstBaseline + 0.5)`, and the canvas then rounds it to a device pixel, where Blink lays the
  control's inner text out in device-scaled layout units with the font's metrics rounded at the zoomed size. Where to
  start: a Chromium fixture at 1 and 1.65 with such a control (the text-verdict fixture had one while it was made, not
  committed: a `<select>` or `<input type="button">` with `appearance: none; margin: 0; border: 0; padding: 0 6px; height:
  20px; font: inherit; color: inherit; background: #e8e0d0` in a `13px/20px Lato` page), then compare the baseline Blink
  gives (`LayoutMenuList`, `LayoutButton`, `SimpleFontData`'s rounded ascent and descent) with `SnapBaseline`'s. Because
  of it, `ChunkTextLcdReferenceTests.AControlsLabelTakesItsChunksVerdict` checks a control label's verdict in the engine
  alone, not against Chromium.
- A composited box's text grayscale at 1.65 where Chromium's is LCD (found 2026-11-06 by the text-verdict fix; open, no go
  yet: the user decides when): a composited box (`will-change: opacity`) 56 × 16 px with an opaque background, its
  bottom on a fractional device pixel (a top at 220 px, 363 device pixels, and a bottom at 389.4), holds text the
  layerization calls grayscale where Chromium draws it with LCD (colour spread 16 against 126 in that box; the layer's
  own verdict, not the draw pass's). Suspected cause, not checked: the text's visual rect in the opaque analysis is the
  font's ascent to descent (`PaintTarget.GlyphBounds`, "a text fragment's ink rect, roughly"), 0.2 to 15.8 px in that
  line, so enclosed in device pixels it reaches row 390 while the opaque background's enclosed rect ends at 389, and the
  text isn't known to be on an opaque background; Blink takes the text fragment's ink overflow (the glyphs' ink), which
  stays inside. Where to start: the fixture's first version, not committed (`ChunkTextLcdReferenceTests` with `Pitch =
  20`, the boxes `.k`, `.w` and `.sl` 16 px tall and `line-height: 16px`: its `after-lcd-layer` case, the `.sl` box, in
  both columns), `COMPOSITING_CHUNKS=1` for the chunk's text flags, then Blink's text fragment visual rect (its ink
  overflow) against `GlyphBounds`. The text-verdict fixture was moved onto whole device pixels at 1.65 (rows 40 px apart,
  boxes 20 px tall) to stay clear of it.
- An editor dialog over a composited scroller: its text grayscale at 1.65 where Chromium's is LCD (found 2026-11-07 by M11
  step 12, Progress, "M11 step 12"; open, no go yet: the user decides when): the keep-or-take list (`div.dialog` 640 px
  wide with an opaque background, on the shell's translucent `div.dialog-layer`, `position: absolute` over the whole
  window) shown while the form pane behind it holds a model's long form (the pane a scroller with more rows than it
  shows): at 1.65 every glyph of the dialog is grayscale in the engine (colour spread 9–13) and LCD in Chromium (93–130),
  23,374 pixels past 12 levels (1.34 %); at 1× both are LCD, and with the form pane empty both are LCD at 1.65. At 1.65
  the scroller is composited and the dialog over it with it, and the engine's verdict for that layer's text differs from
  cc's. Suspected, not checked: the same rule as the item above (the text's visual rect in the opaque analysis), or the
  layer's translucent background (the dialog layer's) hiding the dialog box's opaque one from the analysis. Where to
  start: `Tests/.../Editor/Import/ImportScreens.cs`, the `import-again` fixture with `editor.Session.Select(entry)` put
  back before the import (the form shows the model), captured as the fixture is (`claude-work/m11/step12-capture.sh 1.65
  2400x2400 import-again`), then `COMPOSITING_CHUNKS=1` for the layers' text flags. The fixture shows the dialog over an
  empty form pane to stay clear of it.
- A select with its own padding 6–7 px wider than Chromium's (found 2026-11-07 by M11 step 9, Progress, "M11 step 9";
  open, no go yet: the user decides when): the editor's form selects (`appearance` left auto, `height: 22px; padding: 0
  4px; border: 1px solid; border-radius: 3px`, an author background and colour, Lato 13 px; `Tools/Editor/Form/Form.css`)
  are 6–7 px wider in the engine than in Chromium at 1×: 79 against 72 px (options Pale, Dark, Golden), 67 against 61
  (Small, Big) and 80 against 73 (a, (empty), β), measured from the borders in the `form-controls` render and its golden
  (`Golden/editor-form-controls@1.png`, the Shade, Huge and Words selects); what follows a select on its line moves with
  it. Most of the form fixtures' differences (0.11–0.16 % of their pixels past 12 levels, under the 0.2 % limit). Suspected
  cause, not checked: `ReplacedContent.SelectSize` (`UI/Layout/ReplacedContent.cs`) adds the menu list's internal
  paddings (4 px at the start, 1 px and the 15 px arrow box at the end, `ArrowWidth`) on top of the author's padding,
  while an author background and border make the select a `menulist-button` in Chromium, whose inner element takes its
  paddings by other rules (`LayoutThemeDefault::PopupInternalPaddingStart`/`End` and the menu list's inner element's
  style; read before guessing). Where to start: a Chromium fixture at 1 and 1.65 with such selects (with and without the
  author padding, background and border) and PaintReference's element rects, against the engine's widths.
- The painter paints every row of a scroll container, seen or not (found 2026-11-07 by M11 step 8, Progress, "M11 step
  8"; open, no go yet: the user decides when): an editor entry list of 2,021 rows, every one an element, took 109 ms a
  frame to paint on the PC's GPU (Release; 3.5 ms with 29 rows), about 0.05 ms a row, though only some 24 are in the
  pane's view; Blink paints a scroller's content within its cull rect (the visible rect and an interest margin). The
  editor's list makes only the rows near its view now (P37), so nothing waits on it; a long list or table in the game (the
  port's shops, inventories) would. Where to start: `UI/Paint/UiPainter` with a scroll container of many rows, against
  Blink's cull rect (`CullRect`, `PaintLayerPainter`'s interest rect); `claude-work/m11/step8/ListBenchmark` times it.
- A status line's text grayscale at 1.65 beside a tall scroller (found 2026-11-07 by M11 step 10, Progress, "M11 step
  10"; open, no go yet: the user decides when): the editor's status line (`footer.status` in `Tools/Editor/Shell/Shell.css`:
  25 px, `overflow: hidden`, a border on top, no background of its own; Lato 13 px in `--muted`) is drawn with grayscale
  text in the engine where Chromium draws it with LCD, at a device scale of 1.65 only and only while the form pane above
  it is a scroller with content to scroll: 1,823 pixels past 12 levels in `checks-problems@1.65` (while its pack was named
  `broken-vale.pak`; the status "Opened broken-vale.pak: 1 entry"), 1,440 in `references-list@1.65`, 815 in step 9's
  `form-controls@1.65`; none at 1×, none in the fixtures whose form pane doesn't scroll (`shell`, `list-marks`,
  `form-json`). Not its top on a fractional device pixel: with the fixture 665 px tall, (665 − 25) × 1.65 = 1056, the
  difference stayed. Not looked into. Where to start: `checks-problems@1.65` (`Tests/.../Editor/Checks/ChecksScreens.cs`)
  with `COMPOSITING_CHUNKS=1` for the status line's chunk and its layer, against Chromium's layer tree for the same markup
  (the status line is LCD there, so it is in a layer known opaque under it); the 1.65 LCD preference is "Ignored"
  (`UiPainter.LcdPreference`), so the verdict is the layer's own.
- A textarea's `line-height` (found 2026-11-08 by M11 step 11, Progress, "M11 step 11"; open, no go yet: the user decides
  when): the engine lays a textarea's lines out at the font's normal line height whatever the author's `line-height`.
  The mod info's description (`Tools/Editor/Saving/Saving.css`, Lato 13 px, `font: inherit` from a body of 24 px lines,
  then `line-height: 20px`): Chromium 152 put its lines 20 px apart, the engine 16 px (Lato's normal height) with its first
  line 2 px higher, 0.53 % of `saving-mod-info@1` and 0.43 % at 1.65 past 12 levels. The editor uses `line-height: normal`
  there now (both alike). The input fixtures (`Tests/.../Ui/InputFixtures.cs`) have textareas only at the normal height.
  Where to start: a paint or input fixture with a textarea at `line-height: 20px`, then the textarea's inner editor in
  `UI/Layout/` (`ReplacedContent`, `LayoutEngine`) against Blink's `LayoutTextControl`.
- A positioned box with a z-index inside a scroller, composited apart in Chromium (found 2026-11-07 by M11 step 10; open,
  no go yet): a box with `position: absolute; z-index: 2` inside the editor's form pane (a scroller, `overflow: auto`,
  not a stacking context) paints in the root's stacking context, after the scroller, yet must scroll with it; Chromium
  composites it into a layer of its own (a scroll-dependent box painted out of its scroller's order), its text grayscale
  there (the box had rounded corners, so no opaque layer for LCD), while the engine paints it with the scroller's
  content, its text LCD. The reference field's list was first such an overlay: `references-list` 0.21 % at 1× and 0.26 %
  at 1.65, the list's text (over the 0.2 % limit); it is now in the rows' flow instead, which both draw alike (the
  manager's decision, 2026-11-07: the overlay comes back only if the engine composites such boxes as Chromium does).
  Where to start: Blink's `CompositingReasonFinder` and the paint-order/scroll-container checks of
  `PaintArtifactCompositor` (a chunk whose scroll translation is the scroller's but whose paint order is after a
  composited layer that doesn't contain it), and the engine's layerizer (`UI/Paint/Layerizer`) with the list as an
  overlay again (`ReferenceListPart` inserting into the field's holder, `position: absolute; top: 100%; z-index: 2`).
- A stacking context on the form's rows turning the form's text grayscale at 1.65 (found 2026-11-07 by M11 step 10; open,
  no go yet; Chromium's look not captured): `.form-rows { isolation: isolate }` or `.form-rows { position: relative;
  z-index: 0 }` (a stacking context inside the form pane's scroller, tried to keep the overlay above inside it) turned the
  form's text grayscale in the engine at 1.65: `form-controls@1.65` and `form-controls-end@1.65` went from 0.14–0.16 % to
  1.12 % of their pixels past 12 levels against the goldens captured without it. Not known whether Chromium draws it
  grayscale too (no capture with the rule was made): first capture `form-controls` with the rule added at 1.65, then, if
  Chromium keeps LCD, compare the layers (`COMPOSITING_CHUNKS=1`).
- A small API item for step 1's author (found 2026-11-07 by M11 step 10): `ValueShape.TextOf` (a map key as its path
  segment writes it: a string as it is, an enum member by its JSON name, a number in decimal) is internal, so the editor's
  `EntryReferences` (`Tools/Editor/References/EntryReferences.cs`, `KeyText`) copies it to write a map key's path the way
  `RecordChecks` and the form's rows do. Making it public (or a `RecordPath.Key(ValueShape, object)`) would let callers
  use the one rule.
- The "Known differences" lists (`Docs/Compositing-Port.md`, "Known differences left (SVG)"; `Docs/Scripting.md`, "Known
  differences left"; `Docs/Animation.md`, "Known differences from Blender's NLA"; M5's Progress, "Known differences left (not
  yet ported)") are accepted differences, not tasks, unless the port finds the game needs one.

**Windows check (open; needs the user's Windows PC; recorded 2026-11-03).** Nothing since the move to GitHub (2026-10-30)
has run on Windows; M6, M7, M8 and M9 never did. Only the compile-time part is checked: on 2026-11-03 every project of the
solution built for `win-x64` from Linux with 0 errors and the Windows natives copied (Progress, "Small tasks"). Run from
`engine\` in PowerShell, in this order; for each item: what to do, what passes, where it came from.
1. **Set up, build, test.** Do: `Tools\Scripts\setup.ps1` once, then `Tools\Scripts\build.ps1`, then
   `Tools\Scripts\test.ps1`. Passes: `build.ps1` ends without errors and leaves in `$STATVENTURE_GAME` the published
   sandbox with `packs\` (`audio.pak`, `ui.pak`) beside it, stale files removed and changed packs put in place with
   `File.Replace` (run it twice: the second run copies nothing); `test.ps1` gives the same results as Linux (3496 tests
   on `main` at M11's end, 2026-11-08), all passing (the goldens are platform-pinned: the tests act as Chromium on Linux with the
   reference PC's font settings, `ReferencePlatform.cs`, `SystemFonts.cs`, so Windows must not need its own). From:
   `build.ps1` never run (M9 step 9, `Docs/Assets.md`, "How it works", step 9, and Status, step 9); the setup scripts
   from the move to GitHub (Progress, "Moved to one git repo for GitHub").
2. **M9's per-step items.** (a) *Packs* (step 1): a running game with a pack open; replace the pack (`build.ps1` again
   with a changed pack, or a rename over it) and Reload. Passes: the rename over the open pack succeeds (the pack is
   opened with delete sharing for this), and the tests on read sharing pass. From: `Docs/Assets.md`, "How it works",
   step 1, "Not checked". (b) *Storage* (step 4): save a slot twice, export and import a character, use "Open game
   folder". Passes: the second save keeps the first as the backup (`File.Replace`, one call on Windows); the game folder
   is the RoamingAppData known folder's `Stat Fick\`; export and import use the Documents known folder; "Open game
   folder" opens Explorer there. From: `Docs/Assets.md`, "How it works", step 4, and Status, step 4. (c) *`textconv.sh`*
   (step 3): `git diff` or `git show` on a commit that changes a pack, in Git for Windows. Passes: the pack shows as its
   dump (git runs the script with its own bash; the script needs `env.sh` to find dotnet). From: `Docs/Assets.md`,
   "How it works", step 3. (d) *The build's game folder* (step 9): start the sandbox from `$STATVENTURE_GAME`. Passes:
   it mounts `packs\` and `classes\` beside the executable and the player's `mods\` from the game folder. From:
   `Docs/Assets.md`, "How it works", step 9.
3. **FreeType's Windows DLL.** Do: check whether `FT_Library_SetLcdFilter` works there (whether the DLL the package ships
   is built with subpixel rendering; the call's error code says it), and run `Text/FreeTypeThreadingTests` and the text
   goldens (part of item 1). Passes: the tests pass whatever the answer; record the answer. The engine's text doesn't
   depend on the filter: the FreeType it loads on Linux is built without subpixel rendering, so the call changes nothing
   there, and the text goldens pass against Chromium's captures without it (the test checks only the filter each
   library was given). From: Progress, "FreeType thread safety (2026-11-03)"; "What M9 leaves for the port".
4. **M8 audio on Windows.** Do: listen to themes through OpenAL Soft (the package ships its Windows natives). No
   PowerShell equivalent of `play-theme.sh` exists yet; by hand it is what the script does: `dotnet build
   Tools\PlayTheme -c Release`, then `dotnet Tools\PlayTheme\bin\Release\net10.0\PlayTheme.dll <the script's
   arguments>`. Passes: it sounds as it did on Linux on 2026-11-02 (the math is ported, so results should match) and the
   audio tests pass in item 1. From: `Docs/Audio.md`, Status, "Heard, but not run on Windows".
5. **Hidden documents and minimizing.** Do: run the sandbox in a window, minimize it and restore it, with sound playing
   and with none. Passes: GLFW's iconify reports the minimize; the document goes hidden as under X11 (the window's blur,
   then the visibility change; on restore the visibility change, then the focus), its timers are throttled, and the sound
   pauses unless the page is audible. From: Small tasks above, "Window host" ("Not checked: ... Windows");
   `Docs/Scripting.md`, "Hidden documents"; `Docs/Audio.md`, Status, "Not checked automatically".
6. **GPU renders on a Windows GPU.** Do: item 1's `test.ps1` on the Windows PC's GPU (the M6 and M7 goldens are in it).
   Passes: every render golden passes as on this PC's AMD GPU and on lavapipe; a failure is reported as it is, its rule
   found, not given a wider margin. From: the Small tasks' Windows bullet ("M6, M7 and M8 never did").
7. **Do the tests' redirected folders hold on Windows?** The test process sets `APPDATA`, `LOCALAPPDATA` and
   `USERPROFILE` to a scratch folder ("Decision on test runs and the player's folders"), but this machine's game folder
   and Documents folder come from the known-folder API there (`Environment.GetFolderPath(ApplicationData)` and
   `MyDocuments`), which may not follow the process's variables. Do: in PowerShell 7 (`pwsh`, .NET as the tests),
   `[Environment]::SetEnvironmentVariable("APPDATA", "C:\x\Roaming"); [Environment]::SetEnvironmentVariable("USERPROFILE",
   "C:\x"); [Environment]::GetFolderPath("ApplicationData"); [Environment]::GetFolderPath("MyDocuments")`. Answers:
   `C:\x\…` for both: the redirect covers Windows, so drop the Windows exception from
   `Infrastructure/PerUserFoldersTests.ThisMachinesFoldersAreInTheRunsScratchFolder` and run it; the real folders: the
   Windows suite is not protected from a test that forgets its own folder (mutations never run there), so ask the user
   how to close it (for example a test-only folder override in `GameFolder` and `DocumentsFolder`). From: the 2026-11-05
   decision (Progress, "Test runs kept out of the player's folders").
8. **M11's items (recorded 2026-11-07, completed 2026-11-08 at M11's end; `Docs/Editor.md`, Status, gathers them).**
   (a) *Writing over a pack held open* (steps 2 and 11): `PackRewriteTests.ARewrittenPackKeepsEveryOtherEntryAsItWasStored` in item 1's `test.ps1` writes a pack
   over itself while it is open, as the editor saves (a save in the editor is (g)). Passes: the
   test passes: the writer's new file is moved over the open pack (`PackFile` opens it sharing read, write and delete;
   item 2a is the game's side). From: `Docs/Editor.md`, Status, step 2; `Docs/Assets.md`, "Packs for the editor", "Not
   checked". (b) *Import's default lookup on a file system that ignores case* (step 2): import one terrain description
   whose image is named in another case than the file beside it (it names `vale-height.png`, the file is
   `Vale-Height.png`): in the editor (Import, or a drop; step 12), or through `PackWriter.ImportFile` with the default
   `ImportSources`. Passes: record the answer, whether the import finds the file there; packing the folder refuses that
   name on every system, so if the import finds it, one file imported differs from the folder packed on Windows: report
   it. From: the same places. (c) *A real file drop and a real close* (steps 3, 8 and 11): in the editor's window (drops
   wired by step 8, the refused close by step 11), drop files from Explorer, close it by the title bar, and
   minimize it and close it from the taskbar. Passes: the drop reaches the window with Explorer's paths in its order (a
   dropped `.pak` opens, P27); each close is asked once (`Application.ConfirmClose`), also while minimized; a refused
   one keeps the window running and the next ends it. On Linux the check program is
   `~/.cache/stat-fick-build/claude-work/m11/drop-check.sh`, which isn't on the Windows PC. From: `Docs/Editor.md`,
   "How it works", step 3, "Not checked here (by hand)", and Status, step 3. (d) *The editor in a real window* (step
   4): `Tools\Scripts\editor.ps1` (M12 step 3, never run yet: no PowerShell on the PC or in the cloud) does what
   `editor.sh` does: `dotnet build Tools\Editor -c Release`, then `dotnet
   Tools\Editor\bin\Release\net10.0\Statventure.Editor.dll --repo <the engine folder>`, its arguments passed on
   (`--player-folder <folder>`). Passes: the script builds and starts the editor, its exit code the editor's; the shell shows as
   `editor.sh` shows it on Linux (the by-hand check in "Small tasks"): the window and its title, the zoom keys (Ctrl+=
   and Ctrl+-) through the host, a splitter dragged with the mouse and its cursor. From: `Docs/Editor.md`, "How it
   works", step 4, "Not checked", and Status, step 4. Also New entry by its button and Ctrl+N, a reference picked from
   its list, and Open, Save, Save As and New mod through the file browser (steps 8–12, 14). (e) *The file browser's places on
   Windows* (steps 3 and 8): the drive roots, Documents (the known folder) and the mods folder (`%APPDATA%\Stat Fick\mods`)
   listed, names compared ignoring case (`FileBrowser.NameComparison`), a path typed with `\` and `~\` read. Passes: as
   on Linux, with Windows' own roots and folders. From: `Docs/Editor.md`, "How it works", steps 3 and 8. (f) *The preview's
   sound* (step 13): a sound effect and a theme played in the editor through OpenAL Soft (at a volume of the user's
   choosing). Passes: as on Linux (item 4 for the music). From: `Docs/Editor.md`, "How it works", step 13, "Not checked
   here". (g) *A mod made and loaded* (steps 11 and 14): a mod made in the editor (New mod, an entry, Save) into
   `%APPDATA%\Stat Fick\mods\`, then the sandbox started from `$STATVENTURE_GAME`. Passes: the editor's save swaps the file
   in while it is open (8a), the sandbox mounts the mod (its log names it); the end-to-end tests pass in item 1's
   `test.ps1`. From: `Docs/Editor.md`, "How it works", steps 11 and 14.

When done, record: a Progress entry "Windows check" (date, what ran, results, anything that failed and why), a dated line
in `Docs/Notes.md`, and the "Not checked: Windows" lines in `Docs/Assets.md`, Status (and its "How it works" notes),
and in `Docs/Editor.md`, Status and "How it works" (item 8), turned into "checked on Windows <date>"; then this
subsection marked done.

**How each step is finished** (the user's standing rules, for every step of every milestone):
1. Tests: `Tools/Scripts/test.sh` passes in full on the GPU and on lavapipe (the variables are in `CLAUDE.md`). Don't
   tune numbers until a test passes: find the rule. A number the user chose, or a tolerance, isn't yours to change to
   make a test pass; ask. Report failures as they are; a flaky test gets its cause found, not a wider margin. Since M11,
   with workers side by side (the manager's practice, 2026-11-06 and 2026-11-07): every full run, on either driver, and
   every `mutate.py` run takes the shared lock (`flock -o ~/.cache/stat-fick-build/claude-work/full-run.lock …`;
   `Docs/Editor.md`, "Steps"); full runs, and mutation runs whose tests open windows, run inside a headless compositor
   (`cage` on wlroots' headless backend, the user's displays unset), so no window reaches the user's screen; the
   engine's test windows are hidden since M11 step 3, all but `SwapchainTests`' (`engine/CLAUDE.md`). The wrappers use
   `flock -o` and set `MSBUILDDISABLENODEREUSE=1 DOTNET_CLI_USE_MSBUILD_SERVER=0`, because MSBuild's reusable nodes
   otherwise outlive the run and keep the lock, and end their `cage` when its command ends
   (`claude-work/m11/step5-full-run.sh` and `step5-headless.sh` are the model).
2. New tests can fail: break each rule they cover, one at a time (`Tools/Scripts/mutate.py`), and account for every
   survivor: nothing can differ (say why) or it's a gap to close. Report the count. Mutation runs are also checked for
   writes to the player's real folders (decided 2026-11-05, "Decision on test runs and the player's folders"): `mutate.py`
   prints `!!! WROTE TO THE PLAYER'S REAL FOLDERS` (and records it in the results file) when a mutation changed this
   machine's game folder or the Documents folder's top level; any such line is a bug to find before the step is done.
3. Docs: this file's Progress section (and "Current task"), the area's design doc, `Docs/Notes.md`, and on the user's
   PC Claude's memory file `stat-fick-port.md`.
4. Commit each finished step to `main` and push, with the commands in `../CLAUDE.md` (a cloud session pushes a branch
   instead). Everything on GitHub stays private.
5. At the end of a milestone: tick it under Milestones, update the notes, report what was done and what wasn't checked,
   and stop until the user says to go on.

**What M11 leaves for the port:** the **Statventure Editor** (`Tools/Editor`, the program `Statventure.Editor`;
`Docs/Editor.md`), a separate program on the engine's UI that ships with the game for modding (M11-4; M12 packages it
as a download of its own, `statventure-editor` and `Statventure Editor.exe`: "Decisions for M12", M12-1, M12-2), in English, in the old game's dark colours and Lato. It opens one
pack at a time over the game's packs (P6): a mod in the player's `mods/` folder, or, in the dev setup only, one of the
game's packs in `engine/Assets/Packs`, saved in place. **How a modder uses it:** New mod (a pack in the mods folder
holding `mod/info`, the mod's name, author, version and description, edited in a form of its own; the game doesn't read
it yet, M11-15); then entries: New entry (the top bar or Ctrl+N: a kind and a name in its folder), Duplicate, a game entry
edited (the mod's own copy, P7; "Revert to the game's" drops it), Import (the button or a drop: `.glb` models and stages,
`.terrain.json` terrain, `.css` stylesheets, a record's `.json` form, fonts, images and texts; importing again keeps or
takes each value that differs); every record kind in one generic form made from its definition (a row per field, undo
and redo per entry, a JSON tab with copy and export); the engine's own checks as you edit, the problems at their fields,
at the form's top and in the list; references picked from lists, kept up to date on rename and remove; a live preview
(sound effects and themes, particle effects, models and stages, cutscenes with stand-ins); Save (Ctrl+S; refused while
anything has a problem, so a saved mod loads in the game, as step 14's flow shows from New mod to the game's loaders) and
Save As (a subset into a new file). **How it is started:** `Tools/Scripts/editor.sh [--player-folder <folder>]` from the
repo (Release, the dev setup); the program's options: `--repo <engine folder>` (the dev setup: the game's packs are
`<engine>/Assets/Packs` and can be saved in place), `--game <game folder>` (an installed game's `packs/`, read-only; by
default the `packs/` beside the program), `--player-folder <folder>` (the player's game folder: its mods, and the
editor's own settings in `editor/`: `shell.json`, `recent-places.json`, `volume.json`, `stand-ins.json`; by default this
machine's, so a script passes a scratch one); on Windows `Tools\Scripts\editor.ps1` (M12 step 3). **What the port's own record kinds
(items, enemies, spells, shops) must do to be editable with no editor code (M11-2):** be records as M9's are (`[Record]`,
`[Field]`, `[Subtype]`); mark the kind with `[Kind(display name, folder) { Rule }]` (the folder and name rule New entry and
references use; without a folder New takes a whole path) and their fields with `[Help]`, `[Unit]`, `[Limits]` (only
limits the game's own check already enforces: a new limit is a question for the user, G4), `[Choices]`, `[Colour]`,
`[Bulk]` and `[References(typeof(kind)) { Folder, Rule, Keys, When, Is }]`; and give their rules as a `[Check] static void
Name(T value, CheckContext check)` reporting each problem at its path (with the data, for checks across entries), which
their own loaders run too, so the game refuses what the editor refuses (G4, G5). **How those kinds reach the editor and
the packer was G3, closed by M12-9 (2026-11-08):** the port puts them in a data library of their own,
`Statventure.Game.Data`, which the game links and the editor loads from the game's folder (until then the editor and the
packer take record kinds from any assembly they are given), and G2 (a mod with parts the game can't read) is asked with the game's mod list. Left for later: special views
(a cutscene timeline, a map view, sound or particle editors; M11-2), previews of state machines, UI files and terrain
(M11-13), a stop for sound effects (Small tasks), a mod depending on another mod (P18's known limit), the repo's
script-made packs switching to `packer update` once an editor-made entry is committed into them (P30). Not checked: the
editor in a real window and its sound by ear (Small tasks, "By-hand checks", (a)–(e)) and Windows ("Windows check", items
1 and 8).

**What M10 leaves for the port:** the scene: a tree of `SceneNode`s (local translation, rotation and scale; the world by
Blender's parenting rule as glTF carries it) shown by a `SceneView` (a frame client, its `View3D` in a `surface` element;
it replaced M7's `AnimatedView`), with attachments on the nodes: `Actor`s (figures, placed only by their node), models
(`PlacedModel`), terrain (`PlacedTerrain`: the map is a scene), cameras (`SceneCamera`), a sun (`SceneSun`) and particle
emitters (`SceneEmitter`); instancing a model (`ModelInstance`) makes a scene node per glTF node, each top-level object a
model of its own (outline, glow mask, picking), and hangs its cameras and sun on their nodes (`Cameras`, `Sun`); a hidden
node hides its subtree, and models stay opaque (things appear by visibility, scale, particles and glow; fades are UI
overlays; Q23). F-curves (`FCurve`: Blender's keys, handles, interpolations and easing, in seconds; checked against
Blender 5.2). Cameras and a sun come from glTF stages exported by `make_models.py`'s `export_stage`: the exporter's SCENE
animation mode with "Split Animation by Object" off (a stage's whole timeline one clip), cutscene cameras with sensor fit
Vertical, one white sun with its angle copied into its custom property `angle` (other lights refused), raw lighting units,
and the timeline markers as the scene's custom property `events` (`scene["events"]`, written by `export_scene_markers`);
a stage lives at `models/stages/<name>`. Sequences: the record (`Sequence`, the `scene.*` records, at
`world/cutscenes/<name>` in `world.pak`, `Sequence.Load`) or the C# `SequenceBuilder`, with every track kind (clip,
transform, visibility, animation, palette, event, camera, light, particles, audio, UI); roles bound at play time by
`SequenceBindings` (actors, each with the cutscene's own clip library resolved by name and played by bone names, S7-5;
nodes, models, UI hosts, palettes; stages and effects by path; the audio system); `SceneView.Play` gives a
`SequencePlayer`: `Skip()` to the end state (false when unskippable), `Finish()`, `Seek`, `Rate`, `Pause`/`Resume`, the
reduced variant under the system's reduced motion, cues as events (`Cue`, `Next(name)`; those a skip passes marked
skipped) and `Finished`; a stage's clips outside a sequence through `SceneView.Animate`, on the battle pace. Particle
effects are records (`ParticleEffect`, `models/effects/<name>`), their emitters on nodes by Blender's birth rule. The
audio clock and calls at a time: `AudioSystem.Time`, `Effects.PlayAt`, `Music.PlayAt`, `Music.StopAt` (each a
`ScheduledSound` with `Cancel()`). A sequence's UI tracks set text, classes and attributes in its hosts, whose CSS
animations run on the sequence's clock (`AnimationTimeline`; `Docs/Scripting.md`, "Sequences' CSS animations"). The
sandbox's cutscene mode: `Tools/Scripts/play-cutscene.sh jiruan` (`--volume`, `--dump`). The proof, Ji'ruan's intro, is
test data only (`Tests/.../Scene/JiruanIntro.cs`, `Data/Models/jiruan.glb` and `jiruan-acting.glb`, `Data/Cutscenes/`;
nothing in the packs, Q30), its figures the engine's stand-in test figure. The bar for the port's cutscenes
(`Docs/Scene.md`, "How it works", step 7, "The bar for the port's cutscenes"; S7-1, S7-2, S7-4, S7-5): the 11 other boss
intros, the 7 phase cinematics, the 12 spell vignettes and the banners are made at port time to Ji'ruan's bar: a detailed
Blender stage in the cel, ink and hatch style composed like its old SVG (light sources as layered glowing shells with
their halo as geometry, atmosphere as particles), acting clips authored in Blender on the figures' rigs (a clip library per
cutscene, shared by bone names), particles, the card, sound; the stage and the clips approved by the user through Blender
renders first, before their tests; and the user's requirement (2026-11-05): **the boss and enemy models are as detailed
and high-quality as the cutscene scenes** (the stand-in figure is not the bar; "Open questions for later milestones", "At
port time"). The checks and their limits: a stage's nodes, cameras and sun against Blender's evaluation; its renders
against Blender's, a detailed stage's under `ReferenceLimits.DetailedStage` (S7-12, approved as set on 2026-11-05, S7-13;
M6's limits for every other scene); the acting clips against Blender as M7's clips are; the card against Chromium
captures of the old game (`make-cutscene-references.sh`; the paint fixtures' 12 levels / 0.2 %). Not checked: Windows
("Windows check" above); cc's raster of composited layers (the card's animated bar's two ends, 4 pixels at 1.4 s; a
known difference); the known differences recorded on 2026-11-05 (M5's "Known differences left": slanted box edges, the
shadow's clip inset, bleed avoidance under opaque rounded borders, the square corner's coverage, Blink's effect node for a
2D-scaled box, Skia's rectangle blur on whole pixels); the particles' motion and an animated lens against Blender
(`Docs/Scene.md`, Status, step 5); the proof's sound by ear (its levels and lengths are first guesses).

**What M9 leaves for the port:** `Application.Data` is the game's data: the area packs in `packs/` beside the
executable (the committed `Assets/Packs/audio.pak` and `ui.pak`, copied there by `build.sh`/`build.ps1`), `classes/`
beside them, the player's `mods/` in the per-user game folder, and the development folder on top
(`ApplicationOptions.DevelopmentFolder` or `$STATVENTURE_DEV_FOLDER`); records are read with `Data.Load<T>(path)`.
`Application.Storage` is the player's game folder: save slots, settings, character files (export and import of the JSON
form, from and to the Documents folder) and "Open game folder". `Application.ReloadData()` remounts the data and
reloads the music, then the game's listeners of `Data.Reloaded` run. The importers (models `.glb`, state machines'
JSON form, terrain `.terrain.json`, stylesheets `.css`; the music through `make-music-data.sh`, the UI pack through
`make-ui-pack.sh`) and `Tools/Packer` (`pack`, `list`, `verify`, `extract`, `dump`; `textconv.sh`, git's diff driver
for `*.pak`). Left to the port: the old game's tables and classes (renamed under the path rule, Q31), the port's own
stylesheet compiled into `ui.pak`, `models.pak` and `world.pak` once there is content to pack (none committed, Q1),
the packer's "update" command once a pack holds imported and editor-made entries, the save record (what a slot holds),
the slot screen and the file picker as in-game UI, and the settings list (the volumes, the old game's display
preferences and zoom). Not checked: Windows (the checklist is "Windows check" above, under "How to continue"); and
the compiled stylesheet's load time (95–180 ms from a pack, Debug, on a loaded machine)
and its JSON form's size (12.5 MB) are recorded as facts, not judged (`Docs/Assets.md`, step 7, "Sizes and load times").

**What M8 leaves for the port:** `Application.Audio` is the sound (paused while the window is minimized):
`Audio.Music.Play("combat@alder-vale")` with music.js's fades, `Stop`, `Music.Volume` (music.js's, before its
limiter), `MasterVolume`, `EffectsVolume`; `Audio.Music.Themes` lists the 55 ids (since M9 step 6 the music is
`audio.pak`'s, read through a `MusicLibrary`; `Docs/Assets.md`, step 6). The game ports `desiredTheme`
(region theme, else `alder-vale`; in battle `combat@<region>` if it exists, else `combat`; a boss's theme, else
`combat`) and saves the volumes (M9). Effects: `Audio.Effects.Register(id, patch)`, `Play(id, volume, pan, pitch)` (since
M9 step 6 every patch in the data, `audio/effects/<name>`, is registered under its name when the sound starts),
`Actor.Sounds` (event name → effect, pan, volume); the port must give the game proper sound effects (user decision
2026-11-03, "Decisions for M8"; the first asked for: slotting a rune), which ones is asked at port time. The old game's one
slider is the music volume. `test-themes.sh` (with `make-theme-references.sh` once on a machine, 2.6 GB) whenever
the synthesizer changes; `play-theme.sh <theme>` plays a theme to listen to it.

**What M7 leaves for the port:** figures are `Actor`s on the nodes of a scene (since M10 step 1 the scene,
`SceneView`, replaces M7's `AnimatedView`: added to the document as a frame client, its `View3D` shown in a
`surface` element; `Docs/Scene.md`); their clips come from the .glb files (exported with
`make_models.py`'s `ANIMATED` settings, markers by `export_markers`), shared by bone names within a rig
family; a rig's state machine is built with `StateMachineBuilder` (pack form in M9, editor in M11); gear
is `ActorItem`s on `socket.<slot>` bones and `ActorGarment`s; the battle pace is the scene's `Rate` (since M10 step 5 also
for the particle emitters no sequence holds and the water's waves, S5-Q1).

**Design docs by area** (read the one for the area you work on): `Docs/Compositing-Port.md` (Blink
compositing, LCD vs grayscale text, inline SVG transforms), `Docs/Scripting.md` (event loop, frame
callbacks, matchMedia, ResizeObserver, Web Animations), `Docs/Renderer-3D.md` (the 3D renderer, M6:
its decisions, how each part works, the Blender reference harness and comparisons, each step's status),
`Docs/Animation.md` (animation, M7: decisions, the Blender and glTF rules it ports, steps, status),
`Docs/Audio.md` (audio, M8: decisions, the old game's audio, method, steps, status), `Docs/Assets.md` (assets and data,
M9: decisions, what exists, method, the on-disk design, steps with their dependencies, format details, status),
`Docs/Scene.md` (scene and sequencer, M10: decisions, what exists, the old game's cutscenes, method, the design, steps
with their dependencies, step details, status), `Docs/Editor.md` (the editor, M11: decisions and the user's rule for
M11, what exists, method, the design with the editor choices proposed for the manager and the questions for the user
about the game, steps with their dependencies, status), `Docs/Packaging.md` (packaging, M12: decisions, what exists,
method, the design, the questions for the user, steps with their dependencies, status). The rest of M0–M5 is described
in Progress below.
A new area (M6 on) gets its own design doc in `Docs/`.

**Method.** For the 3D areas (M6 on) the reference is Blender instead: the same scenes rendered by
Blender 5.2, its rule ported and the engine tested against its renders (`Docs/Renderer-3D.md`, "Method").
Everything the old game shows behaves as Chromium 152 (the game's Electron) behaves:
1. Get ground truth first: capture references with the harnesses (`Tools/Scripts/make-*-references.sh`;
   Electron apps that `Tools/Scripts/chromium.sh` runs in Chromium 152 (Electron 44.5.0 from npm, the
   old game's): `Tools/InputReference` (real input), `Tools/PaintReference` (screenshots, any scale),
   `Tools/GameReference` (the old game's screens), `Tools/ScriptReference` (script scenarios), the old
   game's test client `../old-game/test-client`, cc traces) and read the Chromium source
   (`~/.cache/stat-fick-build/reference-src/`, README there; fetch missing files into that folder; in
   a cloud session the folder is empty: fetch what you need from googlesource, which the environment
   must allow). Captures run on Linux (the PC, or the cloud under Xvfb); the 1.65 ones need X11.
   To capture one new input fixture without replaying all of them: filter
   `Tests/Statventure.Tests/Reference/input-fixtures.json` down to it in a scratch file, run
   `Tools/Scripts/chromium.sh Tools/InputReference --shots=<dir> --fixtures=<file> --out=<file> --fonts=../old-game/objects/fonts`
   (for 1.65 add `--ozone-platform=x11 --scale=1.65` instead of `--shots`), then merge its key into
   `input-results.json` (or `scaled/input-results@1.65.json`). Back up a results file before merging.
2. Port the rule from the source, don't fit numbers until a test passes. When a result differs, find the
   rule that explains it (dump the engine's intermediate data and compare). Check the capture setup
   before blaming the engine (see the capture gotchas under Zoom in Progress).
3. Write a test against the reference; `Tools/Scripts/test.sh` must pass in full before a step is done.
   Report failures as they are.

**Keep the notes current.** After each finished step: this file's Progress section, the design doc of
the area (e.g. `Docs/Compositing-Port.md`), and `Docs/Notes.md` for history and gotchas (on the user's PC
also Claude's memory file `stat-fick-port.md`; cloud sessions only see the repo). Write the state
down before a long session's context runs out, so the next chat can pick up from the notes alone.

**Stop at the end of each milestone.** When a milestone's work is done and its tests pass: tick it in
Milestones, update the notes, report to the user what was done, and stop. Don't start the next
milestone until the user says to.

**Ask the user about design choices — at once, never improvise.** The user's standing rule: "If any
questions arise, ask them instantly and do not improvise anything."
- Ask about: anything the player would see, hear or do differently; looks and styles; behaviour the
  old game or Chromium doesn't define; trade-offs between matching Chromium and something else; data
  formats, file and folder layout; scope (cutting, adding or deferring a feature); anything in the
  open questions below.
- Don't ask about what has a clear answer in the code, in Chromium's behaviour or in this file.
- How: the question tool with 2–4 concrete options, the recommended one first with its reason, as soon
  as the question comes up — don't pick a default and carry on, don't save questions for the end.
  Record each answer with its date under "User decisions" (or the milestone's decisions).
- Examples of questions asked so far: LCD vs grayscale text, how tooltips, select lists and validation
  bubbles look, spellchecking, the grabbing cursor, menu bar and OS dialogs, 3D models vs sprites.
- **The exception for M11, the editor only (the user, 2026-11-06):** "For M11, only concern me with questions that
  are meant for the game itself. The editor is at your full disgression, but do inform me of big decisions". The
  manager's reading of it (the M11 manager, 2026-11-06): design choices about the editor itself (its looks, layout,
  behaviour, its own file formats, its tests) are decided by the manager of the session and recorded with their
  reason (`Docs/Editor.md`); workers still stop and report such a choice to the manager instead of assuming; choices
  that change the game (what the game shows, does or loads, data formats the game reads, the shipped game folder,
  anything a player meets outside the editor) still go to the user with the question tool; big editor decisions are
  told to the user. Everywhere else this rule stands as written above. **The exception ended with M11:** from M12
  (2026-11-08) every design choice is asked again, the editor's packaging included ("Decisions for M12").

**Open questions for later milestones** (ask each before starting that milestone; the answers decide
the design and the tests):
- **M6 3D renderer:** answered 2026-10-30 (see "Decisions for M6" below).
- **M7 Animation:** answered 2026-10-31 (see "Decisions for M7" below).
- **M8 Audio:** answered 2026-10-31 (see "Decisions for M8" below).
- **M9 Assets and data:** answered 2026-11-02 (see "Decisions for M9" below), and its 27 format details
  the same day (`Docs/Assets.md`, "Format details"); the questions asked during the steps are in "Decisions for M9"
  and `Docs/Assets.md` (Q28–Q43, S3-1–S3-5, S5-1, S6-1–S6-5, S7-1–S7-6, S8-1, S8-2, S9-A–S9-D). M9 is done.
- **M10 Scene and sequencer:** answered 2026-11-04 (see "Decisions for M10" below), and its 33 step details the
  same day (`Docs/Scene.md`, "Step details"); the questions asked during the steps are in "Decisions for M10" and
  `Docs/Scene.md` (S2-1–S2-3, S3-1–S3-5, S4-1–S4-9, S5-Q1, S5-1–S5-5, S6-A–S6-F, S7-1–S7-14), those of the work beside
  it in the "Decision on …" entries below. M10 is done (2026-11-05).
- **M11 Editor:** answered: the four main questions and four of the detail questions on 2026-11-05, the rest of the
  detail round (M11-9–M11-21) on 2026-11-06 (see "Decisions for M11" below); M11 started on 2026-11-06. Under the
  user's rule for M11 the editor's own design is the manager's (P1–P38, decided 2026-11-06); of the questions about
  the game it raised (`Docs/Editor.md`, "The game's questions (G1–G6)"), G4 and G5 are answered by the user and G1 and
  G6 decided by the manager (2026-11-06, "Decisions for M11"); G2 and G3 are kept for the port ("At port time" below).
  M11 is done (2026-11-08).
- **M12 Packaging:** answered 2026-11-08 (see "Decisions for M12" below; M12-9 closes G3); M12 started on 2026-11-08.
  The detail round (`Docs/Packaging.md`, Q1–Q28), the M12-9 clarification and Q29–Q30 (the BRUTAL splash and the
  credits view, raised by Q11's answer) were answered the same day; nothing is open. How the editor is
  started on Windows (`editor.ps1`, "Windows check", item 8d) is in that doc's step 3.
- **At port time:** a requirement, not a question (the user, 2026-11-05, approving Ji'ruan's cutscene stage): the boss and
  enemy models are to be as detailed and high-quality as the cutscene scenes; the stand-in test figure is not the bar
  (`Docs/Scene.md`, "The bar for the port's cutscenes"). The questions: which sounds the game gets (sound effect patches; the port must add proper sound effects, user
  decision 2026-11-03 in "Decisions for M8", the rune slotting sound among them); UI symbols outside Lato and STIX (⚙ ✦ ☾ ↓ ＋ ⚔ ♟ …); how the inline SVG art and its
  transforms map to the Blender remakes; the grabbing cursor's design; the level of detail of the item
  models. **First read `Docs/Port-Tasks/`:** the user's tasks to add during the port, in their number order
  (decided 2026-11-05); ask whether they come after the faithful port or while porting (left open by the user). Tasks 1
  (Masterwork Elementals), 2 (Guild Rework), 3 (Shop Rework), 4 (Artifact Rework), 5 (New Masterworks) and 6 (New Areas)
  are already done in the old game, so the faithful port brings them over: don't build them again, only remake their
  placeholder art (the README's list: task 1's six masterwork looks, task 2's two guild interiors, task 3's scenes, icons
  and looks, task 4's artifact looks, task 5's Sparkling look and new masterwork looks, task 6's Rift enemies and map
  painting). Also to ask (recorded 2026-11-06, not decided): when to capture the game screens again and bring the tests'
  copy of the stylesheet up to the old game's: before the port starts, or screen by screen as the port reaches each. The
  facts (checked 2026-11-06): the game-screen references (the 22 captured screens, 11 states at 1× and at 1.65:
  `Golden/game-ui.json.gz`, `game-ui@1.65.json.gz`, the `game-*.png` screenshots, `Reference/scaled/game-layers@1.json`
  and `@1.65.json`) and the tests' copy of the stylesheet (`Tests/Statventure.Tests/Data/Game/styles.css`, 3915 lines)
  were made from the old game as it was when they were captured: all came in with the repo's first commit (`f45ca7a`,
  2026-10-30), and the copy is that commit's `old-game/styles.css` byte for byte. The old game's `styles.css` differs from
  it since the user's small changes (`7cdd47e`, 2026-11-03) and grew with port tasks 2, 3 and 5 (4080 lines on
  `a32c5cb`; `diff` against the copy: 3 lines removed, 168 added); its `index.html` and scripts changed too (the class
  information button, the guild room, the shop's reforging, the new map sites). So the engine's game-screen tests check
  the old looks, which is intended for now; the references are to be captured again from the current old game at port
  time, when being the question above (`Tools/Scripts/make-game-references.sh`, which also copies the stylesheet,
  `make-scaled-game-references.sh` and `make-layer-references.sh`; on the user's PC or under Xvfb in the cloud,
  `../CLAUDE.md`).
  Kept from M11 for the user (recorded 2026-11-06, not asked; `Docs/Editor.md`, "The game's questions"), to ask where
  mods and the game's mod list are asked: **G2**, what the game does with a mod that has parts it can't read (a kind it
  doesn't know, or a newer version of one, from a newer editor or made by hand): today it loads the mod and stops with
  an error naming the mod and the part only when that part is used; the alternatives are a check of every mod at start
  that stops naming the mod, or a warning in the log at start, going on as today (recommended then: as today). **G3**,
  how the port's own record kinds (items, enemies, spells, shops) reach the editor and the packer, is **closed by M12-9
  (the user, 2026-11-08):** a data library of their own, `Statventure.Game.Data`, with their rules, which the game links
  and the editor loads from the game's folder (not the editor loading the game's program file; not the kinds in the
  engine). Until the port the editor and the packer take record kinds from any assembly they are given.
  From M12 (the user, 2026-11-08; "Decisions for M12"): **an in-game update check** once the game is public somewhere
  (M12-7: a check against that place; until then no check, and releases are drafts on the private repo that the user
  publishes); **new icons for both programs**, the game and the editor, with the port's art pass (M12-5: none in M12, the
  system's generic icon meanwhile; a Windows `.exe` built on Linux carries no icon or version resources, NETSDK1074, so
  the icons need a Windows build or a resource step then: `Docs/Packaging.md`, "The executables' names"); **the
  BRUTAL logo before the game starts and BRUTAL's copyright with the other credits in the game's own screens** (Q11:
  M12 gives both programs a splash and a credits view now, which the port's start sequence and credits screen take
  over).
  A general port task (the user, 2026-11-11): **all enemies are revamped during the port, because their classes have
  changed (task 10) and an upcoming task will change the spell system as well (task 12, Spell Rework)** ("All enemies
  need to be revamped because their classes has been changed and an upcoming task will change the spell system as
  well"). Every enemy, not only those with class levels: the revamp takes both into account, the reworked classes and
  the reworked spells. It includes the narrower point below.
  From task 10 (the user, 2026-11-10, C2, C11): **every enemy that uses the old classes is fully reworked during the port**
  (the enemy files' class levels and the bosses' `BOSS_CLASSES`), once the port has built task 10's reworked classes;
  their full rework is not part of task 10's design.
  From task 10 (the user, 2026-11-11, C32), three requirements for the port: **(1) the classes are unchanged in the old
  game; during the port, the classes in the drafts are added** (`Docs/Port-Tasks/10_Class_Rework/`:
  `Class-Rework-Draft.md` and `Class-Rework-Features.md`, all 170); **(2) during porting, every class is inspected for
  possible errors and inconsistencies** (in the drafts and against the game's rules, before or while it is built);
  **(3) after the port, every class feature is tested so that it works as intended** (every level of all 170 classes,
  passives and skills included).

## Folders

| Path | Contents |
| --- | --- |
| The repo (on the PC: `/run/media/system/M.2_2TB/Stat Fick Repo/`; private on GitHub) | `engine/` (this tree: engine, port, tests, tools, asset sources) and `old-game/` (the JavaScript game, the port's reference). `../CLAUDE.md` covers the machines it's worked on. |
| `$STATVENTURE_GAME` (on the PC `/run/media/system/M.2_2TB/Stat Fick/`, set in the git-ignored `Tools/Scripts/env.local.sh`; elsewhere `runtime/` in the repo) | Only what the game needs to run (the build output): since M9 step 9 `build.sh`/`build.ps1` publish the sandbox there (the game's stand-in until the port; framework-dependent) with `packs/` beside it, a copy of exactly `Assets/Packs/*.pak` (Docs/Assets.md, S9-A). The game reads `packs/` and `classes/` beside its executable (Q2; `classes/` comes with the port). |
| The player's game folder (Linux `$XDG_DATA_HOME/stat-fick/`, else `~/.local/share/stat-fick/`; Windows `%APPDATA%\Stat Fick\`) | `settings.json`, `saves/` and `mods/` (every `.pak` directly in it, mounted above the game's packs and classes, below the development folder: Q8, S3-4); `Application.Storage` (S9-B), `ApplicationOptions.GameFolder` points it elsewhere. The development folder (`ApplicationOptions.DevelopmentFolder` or `$STATVENTURE_DEV_FOLDER`) is wherever it's named. |
| `~/.cache/stat-fick-build/` (Windows: `%LOCALAPPDATA%\stat-fick-build`) | The .NET SDK (unless the system has one), the NuGet cache, Electron 44.5.0 for the harnesses (`electron-44.5.0/`), and on the PC the BRUTAL source for reference. All re-downloadable. |
| `~/.cache/stat-fick-build/reference-src/` | Upstream sources the engine ports from (Chromium 152, Skia, skrifa, read-fonts, Blender's Freestyle) and the game's cc traces; README inside. Only on the PC, not in the repo (user decision 2026-10-30). Re-downloadable. |

Source tree:

| Folder | Contents |
| --- | --- |
| `Engine/Statventure/` | The engine library (one assembly; namespaces `Statventure.*`). |
| `Tests/Statventure.Tests/` | Unit tests and headless GPU tests with golden images. |
| `Samples/Statventure.Sandbox/` | A visual test bed. |
| `Tools/` | Build scripts, the shader compiler, the packer and later the data editor. |
| `Assets/` | Asset sources: Blender files, fonts, shaders, stylesheets. `Assets/Packs/`: the committed packs, the game's data's source of truth (M9; `audio.pak` since step 6), copied into `$STATVENTURE_GAME/packs/` by the build. |
| `Docs/` | Plans and notes. `Docs/Port-Tasks/`: the user's tasks to add during the port, in number order (README inside). |

## User decisions (2026-10-16)

- **Characters, enemies, items:** real-time 3D models made in Blender, drawn with cel shading and ink lines in the game's art style. They get full animation sets.
- **Battles:** the current fixed side view (orthographic) on a real-time 3D stage built in Blender.
- **Map:** real-time 3D terrain seen from above. Banners and cutscenes are remade in Blender as well.
- **UI:** a custom, responsive game UI.
  - CSS-like stylesheets compiled into the packs.
  - Screens are built in C#.
  - Text uses the game's fonts (Lato, STIX Two Text): FreeType draws the glyphs and HarfBuzzSharp shapes them.
- **Music:** the live synthesizer is ported to C#. Audio goes through OpenAL Soft.
- **Data:**
  - Classes stay `.json`.
  - Everything else is binary `.pak` files, created and edited with an editor tool.
  - Saves start fresh.
- **Platforms:** Linux and Windows.

**Decisions for M5 (2026-10-17):**
- `title` tooltips and the open list of a `<select>` are game-styled (like the leather combat tooltips; a parchment list in the game font). The engine draws them as styleable elements with a plain default style; the game's stylesheet styles them at port time.
- No native menu bar and no OS dialogs: Save/Load Character, Open game folder, Quit, Reload, Zoom, Fullscreen, About and file picking all become in-game UI.
- Form validation (2026-10-18): the engine checks fields as Chromium does (required, min/max/step, maxlength), blocks the submit, focuses the field, and shows Chromium's message in a game-styled bubble (a styleable element, like the tooltips).
- No spellchecking in text fields (2026-10-18).
- The `grabbing` cursor (map panning) is a game-styled cursor image (2026-10-18); the engine supports image cursors with hotspots.
- Inline SVG (2026-10-25): port CSS transforms for SVG children: the `transform` attribute as a presentation attribute of CSS `transform`, `transform-origin`/`transform-box` and transform animations on SVG elements, and Blink's SVG visual rects (layer bounds). Fill, stroke, opacity and filters stay attribute-only (the art becomes Blender renders); full Blink SVG styling was not chosen.
- Game code scheduling (2026-10-25): the engine gets Chromium's event loop — SetTimeout/SetInterval, QueueMicrotask, RequestAnimationFrame, events, ResizeObserver and matchMedia listeners in Chromium's task, microtask and frame order — and C# async/await runs on it (continuations as microtasks, as JS promises; an element animation's Finished is a Task), so the port can mirror the old JavaScript closely. (Chosen over only the four frame APIs, or frame-counted awaits.)
- Web Animations (2026-10-30): port the core of `element.animate`: keyframes with offsets and per-keyframe easing; duration, delay, endDelay, iterations, direction, fill, easing; the animation object with Finished/Ready tasks, Cancel/Finish/Pause/Play, PlayState, CurrentTime, StartTime, finish and cancel events; composited after CSS animations as in Blink; each part tested against Chromium. (Not the game's subset only, not the full API: no KeyframeEffect objects, playbackRate, commitStyles, replace, composite modes or getAnimations.)
- Keyframes in C# (2026-10-30): dictionaries of CSS text with typed options, e.g. `el.Animate([new Keyframe { ["transform"] = "translateY(0)", ["opacity"] = "1" }, new Keyframe { Offset = 0.3, ["transform"] = "translateY(-6%)" }], new AnimateOptions { Duration = 620, Easing = "ease-in", Fill = FillMode.None })`; times in ms as SetTimeout. (Not @keyframes text, not anonymous objects.)
- Path antialiasing (2026-10-25): keep the canvas's own path rasterizer (quarter-pixel cached positions, polygon-flattened curves, its own stroker). Path edges may differ from Skia's by up to ~60 levels; it's a known difference, and SVG paint fixtures compare at a tolerance that covers edge antialiasing. Porting Skia's analytic AA and stroker for paths was not chosen.

**Decisions for M6 (2026-10-30):**
- Reference for the look: Blender renders of the same test models (cel shading in the game's palette, Freestyle ink lines, as in the lizardman test); the engine's renders are compared with them like the UI with Chromium: shading bands and colours closely, lines with a tolerance.
- Ink lines (outlines and inner edges) have a fixed width on screen, scaled with the display's pixel density, whatever the model's size or zoom.
- Shadows: real-time cast shadows on the battle stage and the map, drawn in the art style (shadowed areas take the darker palette tone and hatching).
- Map camera: orthographic, straight down, as the current map (labels, markers and travel lines map over 1:1; zoom 1–10× of fit); the 3D terrain gives relief, light and shadows.
- Anti-aliasing: MSAA 4×.
- Target: Vulkan 1.3 GPUs from about 2016 on (GTX 1050, RX 470, recent Intel integrated) at 60 fps in 1080p.
- Models come out of Blender as glTF 2.0 binary (.glb); M9 converts them into the packs.
- glTF reader (step 2, corrected the same day): BRUTAL's own glTF package (`Ahwoo.BRUTAL.Gltf`, referenced since M0) parses the .glb container and its JSON; the engine decodes accessors (component types, strides, sparse), builds the node hierarchy, materials and skins itself. (First answered "own reader" from options that missed the BRUTAL package; asked again.) M9's pack converter reuses it.
- Cel shading (step 3): the lizardman test's rule. Two tones: lit is the slot's colour; shadow is that colour mixed 42% toward the ink `#130e0b` (mixed in sRGB, as the lizardman script did), unless the palette gives the slot its own shadow tone. The band edge is where a white diffuse surface lit by a sun of strength 3 falls below 0.3 (EEVEE: strength/π · N·L, so N·L 0.314, 71.7° from the light). These are the engine's defaults, settable per scene. Band edges are antialiased like Blender's render (over about a pixel, from how fast the light changes on screen), not per MSAA sample.
- Terrain (step 8): per region a 16-bit height image and a cover image on one grid (each point's cover: a palette slot), made in Blender (first versions can be generated from today's field functions); the engine builds the ground in chunks with level of detail; trees, rocks and ruins are placed Blender models. Covers blend softly over three cells around a boundary, the boundary itself warped so it doesn't follow the grid. Water is shaded: a surface at the water level in its palette tones with the light band, broken by two sine wave trains (about 1.5 m and 0.6 m long, crossing, drifting) that tilt its normal by up to about 8°.
- Particles (step 8): flat camera-facing shapes drawn in the shader (disc, ring, spark stretched along its motion, puff with a soft edge, shard), in palette colours, optionally ink-outlined, glowing through the glow pass; size and opacity follow each particle's age (grow in, shrink and fade out), blended over the scene. Asked on 2026-10-31: motion computed from age (start, velocity, gravity, drag, spin; deterministic, rewindable, no collisions); sizes in world units; each effect sets its shapes' proportions (ring thickness, spark width and stretch, shard length and width, puff softness) and its ink line's width (pixels times the device scale, centred on the shape's edge); growing in and shrinking/fading out eased (smoothstep); colour from a start to an end palette slot, mixed in sRGB evenly over the life; depth-tested, no shadows, not hatched; emitters: bursts, steady streams and trails (following a moving object), from a point, sphere or cone, with ranges drawn from a fixed seed.
- 3D views in the UI (step 9, asked 2026-10-31): every 3D view is a `surface` element, the battle and the map included (one path); a background colour or transparent per view; rendered only when its scene, camera or size changes or the game asks; picking: a view says which model and part (and how deep) is under a point.
- Ink lines, mesh feature edges (decided during step 7): where a part lies on another (an armour plate on a body, a strap, a belt) the per-pixel tests see one surface, but Freestyle inks the plate's own creases and open edges; so each mesh's feature edges are found at load and inked where they lie on the visible surface (intersections still get none).
- GPU skinning (step 7): linear blend skinning (glTF's and Blender's default), 4 influences per vertex (JOINTS_0/WEIGHTS_0, Blender's export default). A posed mesh's normals are recomputed from its bent surface as Blender does (measured: Blender's posed normals are the deformed mesh's own smooth normals, each vertex averaging its faces weighted by their corner angles, sharp edges kept), not turned with the bone matrices (up to about 4° off at a bend).
- Hatching (step 6): in every shadow-tone area (a model's shadow side and cast shadows on scenery), as the lizardman hatched: lines 6 px apart, 1 px wide, at 72°, 35% toward the ink (the old game's battle sprites in a 1080p window), at device scale 1 times the device scale. The pattern is screen-aligned and moves with its model (anchored to the model's origin on screen), so a moving character carries its hatching. A material with the custom property `hatch = 0` (glTF material extras) has none (the lizardman turned it off for its crest and blade).
- Glow (step 6): marked by the Blender shader, through glTF's standard fields: a Principled BSDF with emission is cel-shaded and glows in its emission colour (the lizardman's blade); an Emission shader alone is drawn flat in that colour and glows (its badge and emblem); a Background shader is drawn flat without glow (its pupils; KHR_materials_unlit). The glow looks like the lizardman's halo (its compose.py): the glowing parts in their glow colours blurred by 8 px (alpha boosted 2.2×) with a 2.5 px core over it, behind the whole model (it shows around the model's outline, never over its own parts), sizes at device scale 1 times the device scale.
- Shadows (step 5): everything casts shadows; only scenery (the battle stage, the map terrain and its features; the game marks scenery models) receives them. Characters, enemies and items keep the lizardman's shading (no shadows on them, not even their own). Shadow edges are softened by the sun's size (Blender's sun angle, 3° by default, settable) as Blender renders it: across a shadow's fringe the shadow tone and the lit tone blend by how much of the sun is visible (EEVEE thresholds each sample's hard shadow, then averages), sharp near the caster, softer further away; everything else stays two-tone. (First answered after my wrong description, "crisp edge moved inward"; asked again on 2026-10-30.) No ink outline on cast shadows. Shadow maps fitted to the visible area (orthographic views of bounded areas need no cascades): first decided as one 2048² map; after the soft-shadow correction (a single map can't tell "under" from "behind" for floating parts), 16 maps of 1024², one per direction over the sun's disc as Blender renders a sized sun (it uses 64), in one 4096² atlas (64 MB).
- Ink lines (step 4): screen-space. After drawing, a pass finds edges per pixel from model and object IDs, depth and face normals, then draws each line centred on its edge at the set width, with soft edges and round ends (only visible lines, as Freestyle's visible-only linesets). Outline per model: each model (a character with its gear, an item, a piece of scenery) gets the thick outline wherever it meets anything else (the lizardman's external contour, which only met empty background because it was rendered alone). Inner lines, thin: the lizardman's set: silhouettes (one part over another), open mesh edges, creases where faces meet sharper than 120° (face normals more than 60° apart); no lines at colour boundaries. Widths at device scale 1: outline 3.5 px, inner 2 px (the old game's battle sprites in a 1080p window), times the device scale. Ink colour `#130e0b`. Parts without lines: a custom property on the Blender object (`ink = 0`), exported as glTF node extras (the lizardman's "nolines" collection).
- Model colours (step 2): palette slots. Each Blender material names a slot (e.g. `metal.base`, `metal.trim`, `cloth`, `skin`); the engine fills slots from a palette when it draws, so one sword model shows in every metal and one robe in every cloth, as the old game's palettes do. Materials that name no slot in the palette (glows, fixed details) keep their own colour.

**Decisions for M7 (2026-10-31, asked before the milestone started; all the recommended options):**
- Clip set: every pose the old battle stage plays, per rig, with the melee swing split into weapon families. Humanoids (players, companions, humanoid enemies): idle and stunned loops; advance and return (the lunge), flee; attacks: one-handed swing (sword, axe, mace, club, hammer), two-handed swing (greatsword, greataxe, two-handed hammer), thrust (spear, trident, rapier), dagger stab, staff/wand strike, unarmed punch, off-hand swing; shooting: bow (bow, longbow, greatbow), crossbow; cast, skill, item, drink, meditate; guard and evade (enter, then hold); hurt, critical hurt, stumble; death, victory. Quadrupeds (wolf, bear, boar, stag, ram): idle, bite/pounce, horn charge, hurt, stumble, stunned, death, victory. Griffon and dragon: the quadruped set and a wing attack. Wisp and core: idle (float), attack pulse, hurt, death. Bosses: their rig's set plus phase change, transform, an intro entrance and their specials (asked per boss at port time). (Not one attack per weapon kind, not the old game's set only.)
- Root motion: none. Clips animate in place (the model's origin stays; hips may bob, hop or fall); the game moves the whole model for lunges, knock-backs and fleeing, as the old game moves the sprite, so a lunge reaches every target whatever the distance.
- Clip events (a hit landing, a bow's release, a footstep): authored in Blender as pose markers in each action; the export script copies them into an action custom property, which Blender's glTF exporter writes as the animation's extras (it exports no markers itself: checked in its 5.2 source); the engine reads them.
- State machines: data. Per rig a graph of states (clip, speed, loop), transitions (conditions on parameters, triggers, cross-fade time) and any-state entries (hurt, death); M7 builds the format, the runtime and a C# builder; the packs carry it (M9) and the editor edits it (M11); the game sets parameters and triggers.
- Layers and cross-fades: Blender's NLA rules. Layers stack like NLA tracks: each replaces only the channels its clip keys, by its influence; additive layers combine on top as NLA's Combine mode does; a cross-fade is a linear influence ramp (an NLA strip's blend in/out); tested against Blender's NLA evaluation. Clips are exported with the exporter's "Force keeping channels for bones" off, so a clip's glTF channels are the ones its action keys (checked in the exporter's source: by default every bone is written into every clip). (Not explicit bone masks, not one clip at a time.)
- Clip data: glTF's sampled animation. Clips are animated at 60 fps in Blender (the game's frame rate) and exported sampled every frame (the exporter's default, which bakes constraints, IK and drivers into keys); the engine interpolates as glTF specifies. Checked against Blender's own evaluation of the actions: exactly at frames, within a measured tolerance between them. (Not 30 fps, not Blender's F-curves ported.)
- Clip sharing: by bone names, as Blender applies an action to another armature. One clip set per rig family (humanoid, quadruped, griffon, dragon, wisp, core); models of a family share bone names and differ in proportions (races, sexes, builds); a clip plays on any of them with each channel taken relative to that bone's rest pose. Bosses and odd enemies can add clips of their own.
- Gear: rigid items (weapons, shields, helmets, quivers) are their own models attached to socket bones in the rig (hand.R, hand.L, back, head …); armour and clothing that bends are meshes skinned to the wearer's skeleton by bone names.
- Secondary motion (capes, tails, hair, banners, wings): keyed by the animator in each clip, as the old game's loops were authored; no simulation (no spring bones).
- Shape keys: none. Everything moves by bones (a jaw or eyelids are bones); no glTF morph targets.
- Clip events (asked 2026-10-31, step 4): every acting strip fires its markers (influence above 0, also while fading in or out; held strips fire nothing); events are queued while the frame's animations update (in time order, every marker a long frame passes, each loop included) and dispatched in the same frame step as CSS animation events, also awaitable as Tasks; jumping a strip's time (seeking, restarting) fires nothing; a marker's name is the event, nothing more (the game's data decides what each does). (Not a 0.5 or full-influence threshold, not synchronous callbacks, not names with arguments.)
- Fades of rotations (asked 2026-10-31 during step 3): the engine blends the clips' unit quaternions by Blender's component rule. Blender's NLA mixes its quaternion F-curves' raw values, which aren't unit length between an action's keys (the curves interpolate each component), so mid-fade, where both clips turn the same bone, Blender's NLA preview can differ from the game by a few degrees (4° measured on the test swing's 0.15 s fade); the layer tests bake the reference actions' rotations to a unit key per frame first and check the NLA rule exactly. (Not exporting the raw lengths as extras, not slerp fades.)
- State machines in detail (asked 2026-10-31, step 5): a machine has layers, each with its own states and transitions on its own NLA tracks (Replace or Combine per layer; a layer can be empty and play nothing); changing state cross-fades within the layer and the outgoing clip fades out fully, so channels only it keyed go back to what the layers below give (Blender's NLA transition rule: both sides over the lower layers, mixed over every channel). Conditions: bool, int, float and trigger parameters (a trigger is used up by the transition that uses it, and otherwise kept until used or reset); a transition's conditions compare them (==, !=, <, >, ≤, ≥) and must all hold, optionally only after the state's clip has played to a point (its end or a share of it); any-state transitions are checked before the current state's; the first that holds wins; a transition may restart the state its layer is already in only if its "may restart" flag is set (off by default). A transition starting during another cross-fade on the same layer fades over the current blend (the unfinished fade runs on underneath until covered). A state's speed is a number or a float parameter's value, times the animator's rate. Cross-fade times are seconds of the animator's clock. The game also hears each layer's state entered (when its transition starts) and left (when the outgoing clip has faded out), delivered and awaitable like clip events. M7 defines the graph as data records built with the C# builder; M9 adds the pack form and loader, M11 the editor. (Not one-layer machines, triggers-and-clip-ends only, waiting for or cutting a running fade, fixed speeds, clip events only, a JSON form now, fades as shares of clips, restarts only with triggers or never, triggers dropped after an update.)
- Clips during state machine fades (asked 2026-10-31, step 5): both play through: the outgoing state's clip keeps playing while it fades out, the incoming starts playing as it fades in, mixed by Blender's transition rule (which Blender's transition strips apply to held poses; the rule is tested against them with held clips). (Not frozen as Blender's transitions, which would start every action later by its fade time.)
- Gear in detail (asked 2026-10-31, step 6): sockets are dedicated non-deforming bones named `socket.<slot>` (socket.hand.R, socket.hand.L, socket.back, socket.head, socket.hip.L …) parented to the bone that carries them; an item's origin is its grip and its axes line up with the socket's; clips may key a socket. Clothing on a body of other proportions deforms as Blender's Armature modifier deforms it (by the wearer's bones relative to the wearer's rest), so it fits only the body it was made on and garments are made per body type. Items may have their own bones and clips: when the wearer's state machine enters a state, the item plays its clip of the same name (if it has one) on the same clock. (Not attaching to deforming bones with per-item offsets, not glTF's bind-pose refit, not rigid items only.)
- Model approval (port time): the user approves the first model of each rig before the rest of that rig is made: first the first humanoid (with gear and its full clip set, shown as renders and a playing view), then the first quadruped, griffon, dragon, wisp and core. M7 itself animates test models only.

**Decisions for M8 (2026-10-31, asked before the milestone started):**
- Fidelity: the music matches the old game's sample for sample, within float rounding: Chromium 152's Web Audio processing is ported (the oscillators' band-limited wavetables, the biquad formulas, the AudioParam automation timeline, 128-frame render quanta, delay, convolver, compressor, stereo panner, buffer sources) and offline renders of every theme are compared with Chromium's (its OfflineAudioContext, randomness seeded), as M3–M5 did for text and layout. (Not the same graph with own DSP, not by ear.)
- API: a game synthesizer (instruments, voices, a sequencer, effects and buses shaped for the game); the old music scripts (music.js's score engine, music-composer.js, the themes) are rewritten onto it, producing the same sound. (The recommended Web Audio-like API was not chosen.) Inside, ported Blink processing nodes stay private: each instrument builds the same small graph per note as the old scripts did, so renders match Chromium exactly; the game sees only themes, cross-fades, effects and bus volumes. (Not fused per-instrument routines.)
- Music data in M8: generated C# data records (a script reads the old JavaScript data in the harness's Chromium and writes them into the engine's source); M9 adds the pack form, M11 the editor. (Not generated JSON.)
- Sound effects: synthesized from the same nodes (one-shot patches as data), on an effects bus, triggered by the game or by clip events (footsteps, hits); no audio files. Which sounds the game gets is decided at port time. (Not effects from audio files, not none.)
  - **The port adds them (user decision 2026-11-03):** the finished port has proper sound effects. The old game has
    none (music only), so they are new work in the port, not something carried over: the port plans them as part of its
    work, asks which sounds the game gets and makes them as effect patches (above). The first one asked for: a small
    sound when a rune is slotted (from the user's list of small changes to the old game, 2026-11-03).
- The old score engine (asked 2026-10-31): dropped. Every one of music.js's 24 original scores is replaced at load by a remastered theme from music-composer.js (38 themes and the regional battle arrangements); only the composer, its themes and music.js's master chain are ported. (Not ported as a fallback.)
- The FFT (asked 2026-10-31, during step 2): Chromium 152's Web Audio FFT is the RustFFT 6.4.1 crate (`WebAudioRustFft` is stable), which on this PC's CPU (AVX2 + FMA) runs its AVX/FMA algorithms. Port exactly those (AVX butterflies, mixed-radix stages, FMA where it fuses), bit for bit with this PC's Chromium; CPUs without AVX/FMA get the same bits emulated. (Not RustFFT's scalar path or pffft compared within a rounding bound.)
- The convolver's partitioning (asked 2026-11-01, during step 4): Chromium splits an impulse response differently offline (stage FFTs doubling from 128 to 32768 points, all in the render loop) and live (stages of 2048 points up to 12288 frames, the larger later ones on a background thread). The engine always runs the offline structure on the audio thread, bit for bit with the references; the cost of the big stages is measured in step 7. (Not the live structure, with or without a background thread.)
- Output: at the audio device's own rate, as Chromium's AudioContext runs (OpenAL Soft reports it), in 128-frame quanta, about 20 ms queued ahead. (Not a fixed 48 kHz, not ~60 ms.)
- Step 6's questions (asked 2026-11-01, before its code):
  - Theme data: records of the authored specs exactly as `MusicThemes.all()` returns them (55 themes: the regional battle arrangements already expanded, the loudness trims applied), read in the harness's Chromium; the composer's `compile()` and `lint()` are ported and run when the themes load, lint as a test. (Not the base specs with `arrange()` ported, not compiled themes.)
  - Instruments, percussion and rooms: generated records too (`MusicComposer.instruments`, `percussion`, `rooms`), each instrument's kind choosing the C# code that builds its per-note graph; the modes and chord qualities stay constants beside the compiler (they are notation). (Not hand-written tables.)
  - Theme checks: committed fixtures for every instrument kind, percussion voice, texture, room, the master chain and fades (exact or within the sum-order bound); the 55 full themes in an opt-in test (`STATVENTURE_THEMES=1`) against references in the build cache: four Chromium renders per theme, every engine sample inside their per-sample min/max widened by their largest spread (both rules changed later in step 6: see "Fixture sums" and "The full themes' rule" below); run at the end of step 6 and whenever the synthesizer changes. Measured first: 67 minutes of audio, 1.5 GB of float32 per render (gzip: 93 %), Chromium's own renders differing on almost every sample by up to 2e-4 (its pointer-hash summing order). (Not in every suite run, not committed excerpts; the plan's "renders committed" was dropped for the themes.)
  - exp and pow: V8 15.2's `Math.exp` is LLVM libc's correctly rounded exp (at the revision Chromium 152 pins, 3ea89f43); the engine ports it line for line, checked against V8's own results. (Not an own correctly rounded implementation.) Re-asked after checking V8's source and 2 million of its results: `Math.pow` and `**` go through V8's `math::pow`, which calls `std::pow` (flag `use_std_math_pow`, on by default), i.e. the system glibc 2.43 pow in Electron on this PC (all 2 million matched); not correctly rounded, and on this CPU its FMA variant `__pow_fma`. The engine ports glibc 2.43's `e_pow.c` as `__pow_fma` runs it (which multiply-adds GCC fuses pinned down by compiling glibc's source as glibc does and checking against the system pow), FMAs exact in software on CPUs without them; checked against V8 on millions of random arguments and every argument the composer can produce. (Not .NET's `Math.Pow`, which on Windows is UCRT's.)
  - Theme API: string ids as the old game's (`"alder-vale"`, `"combat@alder-vale"`, a boss's id): `Play(id)` with music.js's fades, `Stop()`, the list of ids; `desiredTheme`'s choice and fallbacks come with the game port. (Not typed constants, not a selection API now.)
  - Voice cleanup: a finished voice's nodes are disconnected at the end of the render quantum in which its first source ended (the earliest Chromium's main-thread `onended` can); fixtures get the same from Chromium by suspending the offline render every quantum. (Not dropping voices only once silent.)
  - Fixture sums (asked during step 6): per-note graphs that sum three or more inputs at one node (three-voice strings, the choir, mallets, glass, the metal and drum voices, rolls) differ between Chromium's own renders (its summing order), and IIR filters spread that past the float-rounding bound (a choir note: 4 Chromium renders up to 6.4e-7 apart, the engine 5.75e-7 from one; a violin note, without such sums, bit-identical). Such a fixture passes if Chromium's one committed render lies inside the per-sample min/max of 4 engine renders whose node inputs sum in shuffled orders (a seeded test hook, like Chromium's pointer hash sets), widened by their largest spread. (Not Chromium's spread committed, not exact-only fixtures.) Re-asked with data: against each of 4 Chromium runs of the 30 synthesizer fixtures, 4 orders passed 28–30 (thunder's 5 summed bursts and the gong's 6 partials undersampled, Chromium up to 35 % of the spread outside); 8 and 16 orders passed all; the user chose 16 orders (renders in parallel, about 42 s). (Not 8, not 4 with those voices left to other checks.)
  - The full themes' rule (asked during step 6, after the first run): the engine's one render inside Chromium's 4-run range widened by Chromium's spread passed 49 of 55 themes; the 6 others missed by summing order alone (on them the engine's own 4 shuffled orders spread 0.8e-4 to 2.9e-4, Chromium's 4 runs happened to agree more closely, and the two ranges came within 5–8e-6 of each other). Now both sides are sampled: the engine renders each theme in 4 shuffled orders, and at every sample its range and Chromium's must come within the larger of the two spreads. (Not the fixtures' 16-order rule, not 16 Chromium renders.)
- Step 7's questions (asked 2026-11-01, before its code):
  - Streaming: an audio thread keeps an OpenAL streaming source fed with queued buffers (4 × 5 ms of rendered quanta), refilling the played ones. (Not OpenAL Soft's callback buffer on its mixer thread.)
  - Output path untouched: direct channels (left to left, right to right), HRTF off, OpenAL's output limiter off, the device opened at its own rate, so the samples reach the system mixer as rendered. (Not OpenAL Soft's defaults: ±30° stereo panning, HRTF on headphones, its limiter.)
  - Volumes: the music volume is music.js's (`Soundtrack.Volume`, the master gain 0.58 × volume before the limiter); the effects volume a gain on the effects bus; the master volume a last gain on the mix; all ramp over 40 ms. (Not all as plain gains after their buses' processing.)
  - Headroom: nothing protects the mix; past ±1 the system clips, as Chromium's AudioContext does; effects leave room and step 8's tests catch patches that clip. (Not a master limiter.)
  - Device changes: follow the system's default device without interruption; the graph keeps its rate and OpenAL resamples if the new device's differs (as Chromium does). (Not rebuilding the graph at the new rate, not staying on the first device.)
  - No device: the graph keeps rendering in real time without output and sounds as soon as a device appears. (Not pausing.)
  - Minimized window: all audio pauses and resumes where it was when the window is restored. (Not the old game's stop-and-restart of the theme, which was recommended; not playing on.)
  - Threading: the game's calls (Play, Stop, volumes) are queued and applied by the audio thread before its next quantum, their "now" the graph's time then; no locks while rendering. (Not a graph lock.)
  - Theme starts (asked during step 7, after measuring: a theme start cost 12–23 ms in one quantum on the audio thread, 10–13 ms of it the room convolver's setup, about the whole 21 ms buffer): a worker prepares all 7 rooms' impulse responses and convolver kernels once when the audio system starts, shared by every theme in the room (each convolver keeps only its running state); a theme start then costs about 1–3 ms. (Not preparing on each Play with the switch delayed, not a 60 ms buffer.)
- Step 8's questions (asked 2026-11-02, before its code):
  - Patch form: a patch is a list of layers, each a tone (a wave or harmonics, with a frequency sweep) or a noise burst, through an optional filter, with its own delay, attack, level and decay, as the composer's percussion voices are built. (Not any node graph as data, not composer voices only.)
  - Room: effects are dry, straight to the effects bus. (Not a shared effects room, not the theme's room.)
  - Clip events: an actor has a table from event name to patch, and the engine plays the patch when the event is delivered; game code can also call `Effects.Play(id, volume, pan, pitch)`. (Not game code subscribing every time, not markers naming patches.)
  - Variation: a patch may give pitch and level ranges, drawn per play from a seeded mulberry32, so tests and replays repeat; no range means no variation. (Not none.)
  - Position: pan and volume come from the caller, through the stereo panner; an actor's table uses a pan and volume the game sets on the actor. (Not world positions with a listener.)
  - Voice limits: per patch (4 by default) and 32 overall; past a limit the oldest voice of that patch (or overall) fades out over 5 ms and the new one plays. (Not dropping new ones, not no limits.)

**Decisions for M9 (2026-11-02, asked before the milestone started):** every answer was the recommended option.
- Pack layout: one pack per area: `models.pak` (meshes, clips with their markers, state machines), `audio.pak` (themes, instruments, percussion, rooms, effect patches), `ui.pak` (compiled stylesheets, fonts, images), `world.pak` (terrain, map data), `data.pak` (the game's tables, once the port defines their records). Classes stay loose `.json` (decided 2026-10-16). (Not one game.pak, not a core pack plus packs per region.)
- Compression: Brotli per entry, from .NET's `System.IO.Compression`; an entry that doesn't shrink is stored raw (flagged). No new package. (Not Deflate, not Zstandard (a new package), not none.)
- Hashes: SHA-256 from the framework, per entry plus one over the index; used to detect damaged files and for the editor's change detection. (Not an own CRC32C or xxHash, not the System.IO.Hashing package.)
- Versioning: a version number per record type, fields by position, upgrade code on read for every past layout; fixture files of the old versions are committed and tested, so a layout change can't break old files unnoticed. The same for pack records, saves and settings. (Not tagged fields, not no versioning.)
- Overrides: the virtual file system mounts the game's packs in order, then extra packs from a mods folder, then the loose development folder on top; later mounts win per path. A mod is just another pack; the development folder gives the live-editing and Reload workflow. (Not the development folder only, not no overrides.)
- Where saves and settings live: one per-user game folder on each system, Linux `~/.local/share/stat-fick/` (honouring `$XDG_DATA_HOME`), Windows `%APPDATA%\Stat Fick\`; saves and settings side by side; it is what the in-game "Open game folder" opens. (Not XDG's data/config split, not next to the executable.)
- Save format: binary versioned records with the same serializer and versioning as the packs' data, hashed, written atomically (a temporary file, then a rename), keeping a backup of the previous save. (Not JSON text, not binary plus a JSON mirror.)
- Character export and import: kept. Save Character writes the current save as readable JSON of the new save format (versioned; not the old game's format: old saves aren't imported, decided 2026-10-16), Load Character reads one back; the default folder is Documents, as in the old game; through the in-game file picker (M5: no OS dialogs). So every record has a JSON form as well as its binary one. (Not a copy of the binary save, not dropping the feature.)
- Settings: `settings.json` in the game folder, System.Text.Json, with a version field; unknown keys are kept, missing ones take their defaults. Which settings exist is decided at port time (the old game has battlePace, combatAnimations, battleUi and battleSpeed in `scripts/settings.js` and the music volume in `music.js`; the port adds the master and effects volumes (M8), zoom, window state, display preferences). (Not a binary record, not INI.)
- Scope: M9 packs the engine's own data (models with clips and state machines; themes, instruments, percussion, rooms; effect patches; terrain; compiled stylesheets; fonts) and builds the format, the virtual file system, the serializer, the packer and importer tools, saves and settings. The old game's tables (enemies, spells, artifacts, consumables, key items, races) are converted at port time, when their C# records exist, checked against the old game in Chromium as the music is. (Not converting the tables now, as records or as JSON entries.)
- Shaders and the UI's PNGs: the SPIR-V shaders and the UI's PNGs stay embedded resources of the engine assembly: they're the engine's, not game data, and the engine and its tests work without a pack. (Not an engine.pak.)
- Music data: `MusicData.g.cs` goes. `make-music-data.sh` becomes the music importer: it reads the old scripts in Chromium as now and writes the records into `audio.pak`; the engine loads the themes from the pack; the existing checks (the exact call logs of all 55 themes against `music-logs.json`, the opt-in theme renders of `test-themes.sh`) run on the pack-loaded data. (Not keeping the .g.cs as the source or as a test reference.)
- The packs are the source of truth: committed in the repo in `engine/Assets/Packs/` (the Folders table's `Assets/`, the asset sources; the folder is made by the first step that commits a pack) and copied into the game folder (`$STATVENTURE_GAME`) by the build; importers fill them, the editor (M11) edits them. Binary diffs in git are the accepted cost, so the engine offers a text (JSON) dump of a pack for review. (Not packs built at each build from text sources, not a committed text dump beside each pack.)
- Format details, asked 2026-11-02 after the design doc (in full with reasons in `Docs/Assets.md`, "Format details"; every answer the recommended option except Q15 and Q17):
  - Packs: M9 commits only what exists, `audio.pak` (the music) and `ui.pak` (the five font faces), importers tested on the test assets in the test run (Q1); `packs/` and `classes/` beside the executable, mods in `mods/` in the per-user game folder (Q2); one file system per application, `Application.Data`, passed to loaders, tests mounting their own (Q3); paths lowercase ASCII `a–z 0–9 - _ . /` only (Q4); entry SHA-256 over the uncompressed content (Q5); index hash at mount, entry hash on every read, a mismatch an error naming pack and entry that stops the game (Q6); the development folder holds entries in their own forms (a record as `<path>.json`), mounted only with a development option, re-read on Reload (Q7); every `.pak` in the mods folder in file-name order, adding or replacing, no deletes, no in-game list (Q8); the byte layout as proposed (64-byte `SVPK` header, index at the end, Brotli quality 11 window 22, deterministic) (Q9); a `dump` command plus a git text-conversion driver in `.gitattributes`, one documented `git config` line per clone (Q14).
  - Records: `[Field(n)]` attributes read by reflection into compiled readers and writers, past versions as frozen copies plus hand-written upgrades (Q10); JSON in camelCase with a `$types` table, absent = default, unknown key = error, shortest round-trip doubles, NaN and infinities as strings, bytes base64 (Q11); unions through tagged subtypes (Q12); committed layout signatures per version plus fixtures (Q13).
  - **Saves, not the recommendation (Q15): named save slots.** Free names given by the player, each slot its own file and backup (`saves/<name>.dat` and `<name>.dat.bak`, names under the Q4 rule); the engine lists the folder, no fixed count; the slot screen is the port's; not compressed. (Not one save as the old game, not rolling backups.)
  - Saves are written on every request: serialized on the caller's thread, written by a background writer (the latest state wins), flushed on quit (Q16).
  - **A damaged or unreadable save, not the recommendation (Q17): load nothing and ask.** The engine reports the slot as damaged with its backup's state, the game asks the player (use the backup, start new), and the damaged file is kept aside, not deleted. (Not loading the backup automatically, with or without a notice.)
  - A `settings.json` that doesn't parse: defaults, kept as `settings.broken.json`, logged (Q18). The picker: M9 gives export and import functions, the Documents folder and folder listings; the picker screen is made at port time (Q19).
  - Content: one entry per `.glb` holding its `ModelData`, state machines separate (Q20); one entry per theme, instrument, percussion voice, room and effect patch plus a theme-order list (Q21), with the records' own JSON names (Q22); a compiled stylesheet is the parsed and prepared rules as tokens, checked at import, tested by comparing computed styles with the text-parsed sheet (Q23); fonts registered from `@font-face` rules as Chromium does (Q24); images stored as PNG or SVG bytes, decoded at load (Q25); terrain as 16-bit samples plus the range, converted at load as today (Q26); the class loader at port time, M9 only mounting the shipped `classes/` folder so it can be overridden (Q27).
  - Asked during step 1 (2026-11-03), every answer the recommended option (`Docs/Assets.md`, Q28–Q31): paths Windows can't hold are refused too, wherever paths are checked (a segment ending in a dot; the reserved device names `con`, `prn`, `aux`, `nul`, `com0`–`com9`, `lpt0`–`lpt9`, alone or with an extension; a segment over 255 characters) (Q28); a loose folder's file whose name breaks the rule is an error naming it, and the mount fails, as packing does (Q29); mods are the files directly in the mods folder ending in `.pak` in any case, in ordinal name order (Q30); the old game's class files are renamed under the rule (`black-knight.json`) when the port makes the shipped `classes/` folder, the old game's own files untouched: one path rule for everything (Q31).
  - Asked during step 2 (2026-11-03, `Docs/Assets.md` Q32–Q37, every answer the recommended option): enum values in JSON as the member's camelCase name, an unknown name an error (Q32); the JSON writer leaves out only a null whose default is null, and a field's default is part of its layout signature (Q33); an ordered list of pairs has the dictionary form, a JSON object in order, a repeated key an error (Q34); `$types` an object of name → version, the root's type first (Q35); vectors, quaternions and matrices as arrays of numbers in System.Numerics' order (Q36); adding a subtype to a union is a new version of the union (Q37).
  - Asked during step 3 (2026-11-03, `Docs/Assets.md` S3-1–S3-5, numbered apart since step 4 asked Q38 on; every answer the recommended option): a loose `.json` file is a record when its top-level object starts with `"$types"`, the record at its path without `.json`, by one rule for the packer's input, mounted folders and extract's output, its start read at mount, a start that isn't valid JSON failing the mount, a file entry holding a record's form refused by the writer (S3-1); the dump is one JSON document with each entry's path, kind, lengths, stored form and SHA-256, a record's JSON form, a UTF-8 text file's lines, nothing more for other files and no offsets (S3-2); importers chosen by the source file's name ending, `[Importer("models", ".glb")]` on an `IImporter`, the longest ending winning, two with one ending an error, files they read not packed themselves, found in the engine's assembly (S3-3); `Application.Data` mounts the area packs by name in the order models, audio, ui, world, data, each if present, other files ignored, no `packs/` meaning none, then `classes/`, mods (step 9) and the development folder (S3-4); the development folder is `ApplicationOptions.DevelopmentFolder`, else `$STATVENTURE_DEV_FOLDER`, a missing one an error (S3-5).
  - Asked during step 4 (2026-11-03, `Docs/Assets.md` Q38–Q43, every answer the recommended option): the save file's header is 48 bytes (`SVSV`, a u32 format version 1, the payload's u64 length and SHA-256), then exactly the payload (Q38); a damaged or unreadable save is kept aside when the slot is next written, never by loading, as `saves/<name>.dat.damaged` (`-2`, `-3` … when taken, never overwritten), the backup left as it is, and a damaged backup a write would replace likewise as `.bak.damaged` (Q39); slot names have at most 100 characters (Q40); `Delete(name)` removes a slot's save and backup, files kept aside stay (Q41); a `settings.json` of a newer version is treated as one that doesn't parse (Q42); a setting of the wrong type gives its default, logged, the others are used (Q43).
  - Asked during step 5 (2026-11-03, `Docs/Assets.md` S5-1; the recommended option): a model or state machine from the game's data that reads but breaks its rules is checked at load: `ModelData.Load(files, path)` checks what `GltfImport` checks plus the indices it leaves to BRUTAL (node, mesh, skin, material and root indices; joint indices against the skin; attribute lengths and indices against the vertex count; channel nodes, key counts, increasing key times), `StateMachineDefinition.Load(files, path)` runs `Validate()`, and a failure is a `DataFileException` naming the pack and entry or the file (not state machines only, not nothing at load). The step's own choices were accepted with it: record types `model.*` and `animation.*`, field numbers in declaration order, state machines anywhere by path.
  - Asked during step 6 (2026-11-03, `Docs/Assets.md` S6-1–S6-5, every answer the recommended option): music names outside the path rule are stored by one reversible rule, a capital as `_` and the small letter, `@` as `.` (`audio/instruments/low_spiccato`, `audio/themes/combat.alder-vale`), the library mapping paths back and a theme whose id isn't its path's an error (S6-1); `audio/theme-order` sets the order, theme entries it doesn't name following in path order, an id named twice or without an entry an error (S6-2); no music entries make a library without music (`Play` refuses every id as unknown), missing pieces when some are there are load errors (S6-3); the audio system registers every effect patch of the data under its name when it starts, `Register` still adding or replacing (S6-4); the importer's JSON sources go to a scratch folder in the build cache (`~/.cache/stat-fick-build/music-data/`), not the repo (S6-5).
  - Asked during step 8 (2026-11-03, `Docs/Assets.md` S8-1, S8-2; both the recommended option): a terrain description's name ends in `.terrain.json`, the terrain importer's ending, the committed test terrain keeping its name and the tests copying it under the ending (S8-1); the terrain record is `TerrainData` itself, `terrain` version 1: width, depth, cellSize, heightMin, heightMax, heightSamples (a uint16 list), covers (bytes), coverSlots (strings), waterLevel (float or null), the C# names unchanged and the float heights not stored (S8-2). Accepted with them: the check at load is what the description's loader checks, and a description's unknown keys are ignored as before.
  - Asked during step 9 (2026-11-03, `Docs/Assets.md` S9-A–S9-D; every answer the recommended option): the build also publishes the sandbox, the game's stand-in until the port, into `$STATVENTURE_GAME` (framework-dependent, `dotnet publish --no-build`) with `packs/` beside it holding exactly the committed packs, stale ones removed (S9-A); a public `Application.Storage`, made at the start of `Run()` for `ApplicationOptions.GameFolder` (null: this machine's folder), flushed and disposed after the game's unload on quit and after a failed start or loop (S9-B); a public `Application.ReloadData()` the game calls, no key and no file watcher; a failed Reload is logged and gives false, the old data staying (or only the old music library, when only the music fails) (S9-C); on Reload the theme playing restarts through the usual switch fades only when its music changed (its theme, or any instrument, percussion voice or room, by binary form), else plays on; later `Play` calls use the new library; the data's new and changed effect patches are registered, ones gone from the data stay until quit, patches registered in code stay (S9-D).
  - Asked during step 7 (2026-11-03, `Docs/Assets.md` S7-1–S7-6, every answer the recommended option): every url, in stylesheets and the UI's markup, is a full data path from the root under the path rule, `data:` urls as they are, a relative, `http:`, out-of-rule or missing target refused at import (S7-1); compiling refuses a selector the engine can't parse except one naming another browser's prefixed pseudo-class or pseudo-element (dropped and logged, as Chromium drops it), an at-rule it doesn't handle, an unknown property and a value it can't parse, properties it deliberately ignores staying allowed and dropped (S7-2); the compiled form is the rule structure as records with selectors, media queries and values as CSS tokens, selectors and media queries parsed at load (S7-3); `UiDocument.Files` gives a document the game's data, a @font-face font that is missing or doesn't load is an error naming the sheet that stops the game, a missing or broken image or SVG shows nothing and is logged once (S7-4); `Tools/Scripts/make-ui-pack.sh` copies the five fonts from `old-game/objects/fonts/` under names that follow the path rule into a build-cache scratch folder and packs `engine/Assets/Packs/ui.pak` (S7-5), with the two OFL licence texts as `ui/fonts/lato-ofl.txt` and `ui/fonts/stix-ofl.txt` (S7-6). Accepted with them: the importer takes `.css` only, sheets at `ui/styles/<name>`, fonts at `ui/fonts/`, images at `ui/images/`; the `ui.*` record types; fonts loaded when a sheet is added, @font-face families before the library's own, faces shared per library and given back when the sheet goes; the descriptors honoured (font-family, src with one url() and an optional truetype or opentype format, font-style normal or italic, font-weight, font-display); the tests' plan.

**Decisions for M10 (2026-11-04, asked before the milestone started):** every answer was the recommended option except
the scope (M10-2). The design doc is `Docs/Scene.md`.
- Authoring (M10-1): a cutscene is a sequence record with tracks. The 3D motion (camera moves, the motion of objects and figures) is made in Blender and exported as glTF clips (an animated camera node too), which the sequence references; timing, title cards, music cues, effects and the skip rules are sequencer tracks, authored in the editor (M11) or built in C# as the state machines are (`StateMachineBuilder`). Why: Blender already makes every model and clip and is the reference the 3D is tested against (M6, M7), while cues, the UI parts and skip rules have nothing in Blender to come from. (Not everything in Blender, with timeline markers driving the cues; not code only.)
- **Scope (M10-2), not the recommendation:** M10 builds the engine (nodes, the sequencer, every track kind, tweening, the Blender checks, a test sequence that uses every track) and also remakes one boss intro now as a proof, Ji'ruan's (M10-11). The other game cutscenes (the other 11 boss intros, the phase and special cinematics, the 12 spell vignettes, the banners) are made at port time. (Chosen over the recommended option, which left every game cutscene to port time.)
- Skipping (M10-3): sequences are skippable by default, and a sequence can be marked unskippable. A skip jumps to the end state: every track evaluated at its end (final transforms, the title card hidden, the music cue applied). Vignettes are sequences marked non-blocking. The game binds the skip button and key and decides when its combat-animations setting suppresses a sequence (the old game's `combatAnimations` off plays none and stops one playing). Why: a skipped and a watched cutscene then leave the same scene behind. (Not cutting at once where it was; not "never skippable in the engine", every skip left to the game.)
- Reduced motion (M10-4): a sequence may carry a reduced variant (a shorter duration or a cut list), which the engine plays when the system's reduced-motion setting asks for it; a sequence without one plays as it is. Why: the old game plays every cutscene shorter under reduced motion (a boss intro 2.4 s with its motion off), so the data has to say how. (Not left to the game; not ignored.)
- Nodes (M10-5): a node tree: parents and children, each node with a local translation, rotation and scale and a world matrix, by Blender's object parenting rule as the glTF importer reads it; actors, cameras, lights, particle emitters and plain models attach to nodes; the animated view (M7's `AnimatedView`) becomes a scene with a root node; bone sockets stay for gear (M7). Why: a stage exported from Blender is such a hierarchy already, and one tree places everything one way. (Not flat lists with explicit parent links.)
- Tweening (M10-6): the sequencer's own curves are Blender's F-curves: keys with Bezier interpolation (auto and vector handles), linear and constant, and Blender's easing set (sine, quad, cubic, quart, quint, expo, circ, back, bounce, elastic; ease in, out and in-out), the model the imported clips are made with in Blender; checked against Blender's evaluation with the existing harness. Why: one model for every motion, with a reference to test it against. (Not CSS easing; not both.)
- Clock (M10-7): a sequence runs in real time on the document's frame clock at its own rate (1 by default), independent of the battle pace; it pauses while the document is hidden; the figures it animates follow its rate. (Not tied to the animated view's rate, the battle pace.)
- Audio cues (M10-8): a sequence's sounds and music cues are scheduled on the audio clock from the sequence's start, sample-accurate; a skip or a pause cancels the cues still pending. Why: on the frame, a sound lands only to the frame (16.7 ms) and then to the audio thread's next quantum. (Not triggered on the frame.) Corrected 2026-11-04: the decision said "through M8's scheduling", but M8 has no public scheduling: the game's calls (`Music.Play`, `Effects.Play`) start at the audio thread's next quantum and the audio clock (`AudioGraph.CurrentTime`, `Soundtrack.Schedule`) is internal. The graph inside is sample-accurate, so M10's step 6 adds a play at a time, a clock the game can read and cancelling (`Docs/Scene.md`, "What exists", "Audio").
- Text and screen tracks (M10-9): title cards, captions, the letterbox, flashes and vignettes are UI: a track names UI elements and sets their text, classes and attributes at cue times; their look and motion stay in the game's stylesheet, animated by the UI as the old game's CSS keyframes animated them. Why: the UI already plays CSS animations as Chromium does (M4, M5), so the old title cards carry over rule by rule. (Not text drawn by the engine in the 3D view.)
- Data form (M10-10): a versioned record through the M9 serializer (binary and JSON forms), in `world.pak` under `world/cutscenes/<name>`, plus a C# builder like the state machines'; the editor (M11) edits it. (Not `data.pak`; not a builder only.)
- The proof (M10-11): Ji'ruan's intro (the rift in the Great Alderwood, his four Void Imps, the title card): the engine's test figure stands in for the boss and the imps (a placeholder until the real models), the stage and its camera are built in Blender, the rest is in the sequence. (Not the sequencer only, without a Blender scene; not another boss's intro.)
- Step details, asked 2026-11-04 after the design doc (in full with reasons in `Docs/Scene.md`, "Step details", Q1–Q33; every answer the recommended option except Q31):
  - Step 1, the node tree and the scene: the scene replaces `AnimatedView` and an actor is placed only by its node, `Actor.Transform` goes (Q1; not a wrapper with `Transform` as an alias, not both kept); instancing makes a scene node per glTF node, each top-level stage object a model of its own for the outline, glow mask and picking, as Blender's render draws a contour per object (Q2; not the whole `.glb` one model, not a `group` property); terrain attaches to a node, so the map is a scene (Q3; not left out).
  - Step 2, F-curves: times in seconds (Q4; not frames); double precision, checked against Blender within float32 rounding (Q5; not float32 bit for bit); handle types always stored, positions only for free and aligned handles, auto, auto-clamped and vector handles computed at load by Blender's rule (Q6; not every position stored); no modifiers (Q7; not Cycles, not Cycles and Noise); rotations as Euler XYZ (Q8; not quaternion components, not either by a mode). Asked during step 2 (2026-11-04, `Docs/Scene.md` S2-1–S2-3, all as recommended): the layout `scene.key` (time, value, interpolation, easing, both handle types, absolute handle positions for free and aligned sides only, back, amplitude, period in seconds; Blender's insert defaults) and `scene.curve` (keys, extrapolation, smoothing defaulting to continuous acceleration as Blender 5.2's new F-curves) (S2-1; not handle offsets from the key, not smoothing none by default); the record is the curve, immutable, `Evaluate(seconds)`, handles resolved once and cached, key helpers (S2-2; not data records with a separate evaluator); a curve with no keys, keys not increasing at least 0.01 frame apart or handle positions that don't fit their types is refused when made or loaded, naming the problem, while what Blender's update fixes in handles is fixed the same way (S2-3; not Blender's update in full with an empty curve evaluating to 0, not refusing those handles too).
  - Step 3, cameras and lights from glTF: in the model record as version 2 of `model` and `model.node`, with frozen version-1 copies and upgrades (Q9; not a separate stage record); the exporter's vertical field of view as it is, cutscene cameras with sensor fit Vertical (Q10; not lens and sensor in extras with the fov per view); field-of-view changes as a curve on the sequence's camera track, cuts as camera-track cues (Q11; not `KHR_animation_pointer` and markers, not one camera per shot); one sun per view, raw lighting units, its angle in extras, other light types refused at import (Q12; not point and spot lights, not no lights from glTF); the scene export mode, a stage's whole timeline one clip (Q13; not actions, not NLA tracks).
  - Asked during step 3 (2026-11-04), all as recommended (`Docs/Scene.md`, "Step details", S3-1–S3-5): orthographic cameras read as glTF defines them (the height twice ymag), the stage export putting the exporter's xmag and ymag right from the camera's scale and the render size's aspect, since the exporter computes them as for sensor fit Auto (S3-1; not orthographic stage cameras with sensor fit Auto, not refused); the stage export copies the sun's own angle into the light's custom property `angle` in degrees, a sun without it refused at import naming the file and the light (S3-2; not a default of 3°, not a property set by hand on the sun object); a sun that isn't white refused at import (S3-3; not ignored, not stored unused); the record in the engine's terms: `model` 2, `model.node` 2, `model.camera` 1, `model.light` 1, a perspective camera without a far plane refused (S3-4; not glTF's own fields); instancing hangs `SceneCamera`s and the `SceneSun` on their nodes, listed as `ModelInstance.Cameras` and `.Sun`, none made the scene's by itself (S3-5; not without the lists, not made the scene's when it has none).
  - Step 4, the sequence record, builder and player core: the record's layout as proposed, `scene.*`, every track kind declared in step 4, stages at `models/stages/<name>` (Q14); roles bound at play time, stage nodes by their Blender names, UI elements by id in a bound host (Q15; not global names, not roles for actors only); figures on mark nodes, their clips as NLA strips on a top layer, parameter and trigger cues, their clock the sequence's while bound (Q16; not strips only, not cues only); state tracks applied in the frame's animation timing update in track order, moments in time order, within 10⁻⁶ s in record order, after the figures' clip events (Q17; not before the clip events, not UI cues as a task); event cues delivered at a skip, marked as skipped (Q18; not undelivered, not per cue); the reduced variant a cut list of spans, equal ends holding a moment, moments cut out treated as at a skip (Q19; not a second set of tracks, not a duration only); any number play at once, `blocking` a flag for the game, unskippable meaning `Skip()` returns false while `Finish()` still ends it (Q20; not one blocking sequence at a time, not inert); the end state stays until the game changes it (Q21; not restored); timeline markers exported into the stage clip's extras, cues may sit at a marker (Q22; not times only); models stay opaque, things appear by visibility, scale, particles and glow, fades as UI overlays (Q23; not per-model opacity); Ji'ruan's reveal a palette cue from all-ink to his own (Q24; not a renderer tint, not left out).
  - Asked during step 4 (2026-11-04, `Docs/Scene.md` S4-1–S4-9, every answer the recommended option): the layout as typed track records in a union (`scene.track`, eleven kinds, each with its own cue record), `scene.sequence`, `scene.role`, `scene.span`, `scene.strip`, node references as a node role or `<stage>/<Blender name>`, `Validate()` plus the check against the stages (S4-1; not one generic cue record); markers live in the stage (the Blender scene's custom property `events`, written by the exporter's scene mode as the stage clip's extras), a cue names `<stage>/<marker>` with its time an offset, resolved at build, load and play, no marker list in the record (S4-2; not the record's own copy, not both); a palette cue is a bound palette role, a `fill` colour on every slot or the target's own (S4-3; not palettes in the record); a transform track and a clip on one node: track order decides channel by channel (S4-4; not an offset, not refused); a seek is a jump that fires nothing, seeking back re-arms (S4-5; not cues delivered as skipped); `Play` checks everything first and throws naming the problem, changing nothing (S4-6; not lenient, not optional roles); a stage's clips outside a sequence run on the scene's battle pace, `SceneView.Animate(instance, stack)` (S4-7; not real time, not left to the game); state cues apply when the cue in effect changes, curves and clips every frame, triggers not yet set are set at a skip, figures' clip events don't fire across a jump (S4-8; not every frame); moments after the reduced variant's last span arrive marked skipped, the literal reading (S4-9; not unmarked).
  - Step 5, camera, light and particle tracks: particle effects as records of their own in `models.pak` at `models/effects/<name>` (Q25; not `world.pak`, not inline); particles evaluated at the end time at a skip, running emitters playing on until their particles die (Q26; not removed, not left where they were).
  - Asked during step 5 (2026-11-04, `Docs/Scene.md` S5-Q1, S5-1–S5-5): **not the recommendation (S5-Q1):** the scene's free emitters (the game's, and a sequence's after its end) and the water's waves both run at the scene's `Rate`, the battle pace, one clock for everything the scene animates (the recommendation was the water in real time, as the old game's ambient loops weren't paced; not both in real time); the rest as recommended: the effect records `particle.effect`, `particle.range` and the unions `particle.shape` and `particle.spawn`, float32, M6's fields and defaults, refused when loaded and when an emitter starts, naming the rule (S5-1; not ranges as a vector2, not double); a sequence's effects bound as its stages are, `SequenceBindings.Effect(path, effect)`, `Play` refusing one missing, unnamed, twice or broken (S5-2; not a data source in the bindings, not one on the scene); emitters on nodes by Blender's birth rule (each particle born where the emitter is at its birth, its spawn in the emitter's space, then in world space), the node's world recorded per frame and interpolated between frames, a stretch jumped over from the world after the jump, an emitter not on a node keeping M6's world spawn, `node.Attach(new SceneEmitter(effect) { … })` for the game's, taken off once its last particle has died (S5-3; not the spawn in world space, not births at the frame's world); a cue or emitter without a palette coloured by the nearest figure or model at or above its node, its palette then its materials then M6's grey (S5-4; not grey, not refused); the field-of-view, strength and angle curves set the camera or sun cut to from the first cut on, every frame, one cut away from given its own back, an orthographic camera left alone, the end values kept (S5-5; not the last value kept after a cut away, not the view's camera whatever it is).
  - Step 6, audio and UI tracks: the audio anchor is the quantum in which the sequence's first call is applied, cues sent ahead with their times (Q27; not latency-corrected, not re-anchored each frame); a music cue at a skip applied with music.js's usual fades (Q28; not at full level at once, not dropped); the player pauses and resumes the CSS animations it started, also when the document is hidden, sequences with UI tracks play at rate 1 only, no CSS seeking (Q29; not a playback rate and seeking for CSS animations, not the UI on the document's clock).
  - Asked during step 6 (2026-11-04, `Docs/Scene.md` S6-A–S6-F, every answer the recommended option): the audio track plays on `SequenceBindings.Audio(audio)`, none (or null) playing nothing, with one `Play` checking that its effects are registered and its themes exist (S6-A; not `Play` throwing without one, not a `SceneView.Audio`); the public clock and calls at a time: `AudioSystem.Time` (the start of the next quantum the audio thread renders, standing still while paused), `Effects.PlayAt(time, id, volume, pan, pitch)`, `Music.PlayAt(time, theme)`, `Music.StopAt(time)`, each giving a handle with `Cancel()`, `State` and `Time`, a past time at once, Q27's anchoring inside the sequence's playback as one batch per quantum (S6-B; not public anchors, not `AudioTime` overloads); a scheduled effect's voice made in the quantum its time falls in, the limits and draws then (S6-C; not when the call is applied); a sequence pauses everything inside its UI tracks' hosts (hosts, descendants, pseudo-elements, running and newly started) on its own clock, transitions on the document's, nested hosts of two playing sequences refused (S6-D; not only the animations started while it plays, not transitions too); `SequencePlayer.AdvanceFrame` sets `Time` and `TimelineTime` before the playbacks apply (S6-E; not the clock from τ); step 4's test bench binds "card" and its rate test drops the UI track (S6-F; not the audio track only). The step's reading of `Docs/Scene.md` was accepted with them (UI state per element and thing set, ids found at `Play`, the cue sending, resending and cancelling rules, the skip's and the reduced variant's audio, a music switch at a time, the effect cue's defaults).
  - Step 7, the proof: its files as test data only, nothing in the packs (Q30; not the first game content, not the `.blend` too); **not the recommendation (Q31):** every element of the old scene remade simply, shown to the user as Blender renders for approval before its tests are written (the recommendation had no approval; not a minimal stage); Blender renders at sampled times for the 3D, Chromium captures of the old title card (its CSS animations seeked) for the UI (Q32; not engine goldens, not without Chromium); a sandbox mode that plays the proof (Q33; not none).
  - Asked at the approval round (2026-11-04, 22:50, after the user saw phase 1's renders: the stage too simple, the figures not moving; `Docs/Scene.md`, "Step details", S7-1–S7-4): **Q31 revised (S7-1):** the stage gets real detail while still a proof (trunks with roots, branches and foliage silhouettes; ground relief and undergrowth; the rift as layered shells with its halo as geometry; mist and embers as particles; cracks with their glow; a night sky with depth; all in the game's cel, ink and hatch style, composed like the old SVG), iterated with the user through renders until approved (not "remade simply"; not final-quality content: it stays a proof); the figures act (S7-2): cutscene clips authored in Blender on the stand-in rig (the king's rise from the light, a spread and a settle into a breathing idle; the imps' flight out of the rift, flapping, hover and landing; breathing idles), played by the sequence as NLA strips, retargetable to the real rigs by bone names (M7); content stays test data (S7-3, Q30 unchanged); no phase cutscene now, Ji'ruan's is the one proof, and the 11 other intros and the 7 phase cinematics follow at port time to the same bar (S7-4; the bar in `Docs/Scene.md`, "How it works", step 7).
  - Asked at the second approval round (2026-11-05, after the user saw phase 1b's renders; `Docs/Scene.md`, "Step details", S7-5): the stage and the acting **approved** ("The scene is perfect. I can't fully judge it because of the placeholder model, but it's very good. Note down somewhere that I want the boss model to be as detailed and high-quality as the scene."); **S7-5:** a separate clip library per cutscene (Ji'ruan's `jiruan-acting.glb`, the rig's armature alone with the cutscene's clips), bound to the figures by bone names through the bindings' clip source (not the clips added to the figure's model); accepted with it: the stage's object names beyond the kept layout are its own, and the halo is geometry with no UI halo layer; the user's requirement for the port: the boss and enemy models as detailed and high-quality as the cutscene scenes ("Open questions for later milestones", "At port time").
  - Asked after phase 2 was built (2026-11-05; `Docs/Scene.md`, "Step details", S7-6–S7-8), all three as recommended: the proof's sound, S7-6: the theme `great-imp-jiruan` as a music cue at 0 and effect cues at the markers `rift-opens`, `flash` and `title-card`, the three new patches asked of the user one by one before they are made (not the music alone, not no sound); the canvas's box antialiasing, S7-7: fixed by its own worker (a box's distance in pixels by its edge normal's own derivative), the card's three held times compared again after it (not left held); the Blender harness, S7-8: it follows the engine's halo rule per glowing model (M6's decision), the M6 glow scenes unchanged, then the stage's ink lines measured and brought to the user as a question, the six Blender comparisons held until then (not the engine's rule changed, not left held). Phase 2's stage changes and its small additions outside its list accepted by the M10 manager.
  - Asked with the sound's options and the ink study (2026-11-05; `Docs/Scene.md`, "Step details", S7-9–S7-12): **not the recommendation (S7-9):** the rift's sound the "glassy crack and sub drop" (a high-passed noise burst, a sine falling 116 → 58 Hz, a quiet triangle at F5; the recommendation was the "void tear"; not the "pressure hum"); **not the recommendation (S7-10):** the flash's sound the "thunderclap" (noise low-passed 1,200 → 200 Hz, a 58 Hz sine, a short square at A3; the recommendation was the air rush into a chord and a bell; not the reversed swell); as recommended: the title card's sound the "deep toll" (S7-11; a triangle at B♭2, sines at 233 and 291 Hz, a short noise strike; not the brass stab, not the soft shimmer), levels and lengths tuned by ear in the sandbox; the stage compared with Blender under a separate, documented limit for detailed-stage scenes set from the ink study's measurement and naming its two causes, M6's 2 % limit on missed lines kept and M6's own numbers unchanged, about 12 % for extra lines (S7-12; not the far trees without ink, not the far geometry simplified).
  - Asked with the limits set (2026-11-05; `Docs/Scene.md`, "Step details", S7-13): the detailed stages' limits (`ReferenceLimits.DetailedStage`) approved exactly as set from the measurement, each the worst measured plus about a twentieth: extra lines 12.5 %, unlike ink 8.5 %, total ink 10.5 %, the mean coverage difference 0.175, stray edge pixels one in 800; M6's for the rest (`Docs/Renderer-3D.md`, "Detailed stages").
  - Decided after the canvas fix (2026-11-05; `Docs/Scene.md`, "Step details", S7-14): the card at 1.4 s is held until the canvas blurs a shadow as Chromium does under a non-uniform scale (found 2026-11-05: the progress bar's `box-shadow` under `scaleX(0.2)`, blurred across the bar with σ ≈ 2.7 px = 6 × √0.2 in Chromium and σ 6 in the engine; numbers in `Docs/Scene.md`, step 7, "The card at 1.4 s"); the rule is ported now by a worker of its own in the canvas code, which un-holds `JiruanCardTests.TheCardPaintsAsTheOldGameDrewItAtTheTime` at 1.4 s on main; step 7 merges with it held, and M10 is ticked once it matches.

**Decisions for M11 (the main questions and four of the detail questions answered 2026-11-05, the rest of the detail
round on 2026-11-06; M11 started on 2026-11-06):** the four main questions (M11-1–M11-4) and four of the detail
questions (M11-5–M11-8) were asked late in the evening of 2026-11-05 with the question tool, ahead of time; the rest of
the detail round (M11-9–M11-21) on 2026-11-06 with the question tool, after the manager's reading of the code. Every
answer was the recommended option except the audience (M11-4), undo and redo (M11-7) and the name (M11-19); M11-21 is
in the user's words. The design doc is `Docs/Editor.md`.
- **The user's go (2026-11-06):** "Once that worker finishes you can start with M11", given after the full run on
  `a32c5cb` ("Current task", "The full run after the merges"); M11 started on 2026-11-06.
- **The user's rule for M11 (2026-11-06, verbatim):** "For M11, only concern me with questions that are meant for the
  game itself. The editor is at your full disgression, but do inform me of big decisions". The manager's reading of it
  (the M11 manager, 2026-11-06): design choices about the editor itself (its looks, layout, behaviour, its own file
  formats, its tests) are decided by the manager of the session and recorded with their reason; workers still stop and
  report such a choice to the manager instead of assuming; choices that change the game (what the game shows, does or
  loads, data formats the game reads, the shipped game folder, anything a player meets outside the editor) still go to
  the user with the question tool; big editor decisions are told to the user. Everywhere else the rule to ask about
  every design choice stands ("Ask the user about design choices" above). `Docs/Editor.md` keeps the editor's own
  choices (P1–P38) apart from the questions that touch the game (G1–G6).
- Form (M11-1): a separate editor app: its own program in `Tools/`, built on the engine's UI, opening the pack files directly; the game stays free of editor code. (Not an in-game dev mode; not a separate app that the game can also open with a dev key. The old game's Dev Mode is a switch inside the game that unlocks things like editing the purse, not a data editor.)
- What it edits at the end of M11 (M11-2): every record kind through one generic form: a list of a pack's entries plus a property grid generated from the record definitions, with undo and redo. Every record kind that exists is editable (today: music and sound effects, particle effects, cutscene sequences, animation state machines, models, UI files), and the port's items, enemies, spells and shops become editable when the port adds their records, with no new editor code. Special views (a cutscene timeline, a map view, hand-built sound or particle editors) come later, when wanted. (Not the generic form plus a cutscene timeline in M11; not the generic form plus sound and particle editors in M11.)
- Preview (M11-3): a live preview pane: select an entry and see or hear it (a sound effect or a theme, a particle effect, a model, a cutscene), updating as a value changes; it reuses the engine's scene view, sound system and sequence player. Its tests play sound only at volume 0, as everything that plays sound from a script or a session does. (Not play/show on a button only; not no preview in M11.)
- **Audience (M11-4), not the recommendation:** the editor is shipped with the game for modding: part of the shipped folder (M12 packages it), able to create a mod instead of changing the game's packs, and with player-ready looks and error handling. This adds work to M11 (mods, the looks, the error handling) and to M12 (the editor in the shipped folder). (The recommendation was a dev tool that isn't shipped; not a dev tool that can save as a mod.)
- Where the editor saves (M11-5): mods for players, the game's packs only from the repo. The shipped editor shows the game's data read-only and saves every change into a mod pack the player names (a mod is a `.pak` in the player's mods folder that adds or replaces entries and can't delete one, M9's Q8/Q30). Only when the editor is started from the repo (the dev setup) can it also save into the game's own packs in `engine/Assets/Packs`. A player can't break the installed game. (Not always asking "game packs or a mod"; not mods only for the user too, with a build step folding a work pack into the game's packs.)
- The look (M11-6): its own plain tool style: a compact dark tool layout (lists left, the form in the middle, the preview right) in the game's font, with its own small stylesheet; it doesn't depend on the game's screens. (Not the game's style; not the plain style with a light and a dark theme.)
- **Undo and redo (M11-7), not the recommendation:** per entry: each entry keeps its own history, so undo only touches the entry being looked at; adding or removing entries is not undoable, the editor asks first instead. (The recommendation was one history per open pack for the whole session, across saves; not a history kept across restarts.)
- Importing (M11-8): in the editor: an Import button and drag-and-drop that run the same importers the packer has, so a modder can add a model or a UI file without a command line; the packer stays for builds and scripts. (Not the packer only in M11; not import with re-import when the source file changes.)
- Picking files (M11-9): the editor's own file browser, built with the engine's UI (folders, files, a path field, recent places), plus files dropped from the desktop onto the window, which the window learns (the engine has no OS file drop yet). The same rule as the game's "no OS dialogs"; the game can reuse the browser later for Load Character. (Not the system's file dialog; not a path field and drop only.)
- The form's rows (M11-10): a row's name is made from the field's code name ("Max voices" from `MaxVoices`); fields can also declare a short help text, a unit and allowed limits in the record's definition, shown as a tooltip, a suffix and slider ends; added to the existing kinds bit by bit, where players need them. (Not names only; not a separate description file per kind.)
- References (M11-11): fields that name another entry by its path are marked in the record's definition with the kind they point at; the form offers a list of matching entries instead of free text and flags a path that doesn't exist; renaming or removing an entry lists who refers to it and offers to update them. (Not a picker and a warning only; not plain text.)
- Invalid values (M11-12): the engine's own check runs as you edit; the problem is shown at the field and in the entry list, the preview keeps the last valid state, and saving refuses and names the invalid entries, so a mod made by the editor always loads in the game. (Not saving allowed with a warning; not refusing the edit at once.)
- What gets a live preview in M11 (M11-13): sound effects and music themes (play, stop, volume), particle effects (looping), models (their clips, a camera you can turn), cutscenes (play, pause, a time slider). State machines, UI files and terrain show their form without a preview until later. (Not also UI files and state machines; not every kind.)
- Imported entries (M11-14; models, terrain, stylesheets): the form shows them; single values (names, colours) are editable, bulk data shows as a one-line summary ("2,431 vertices") and changes only by importing the file again; import replaces the bulk data and asks before overwriting edits made in the form. (Not view only; not everything editable.)
- Mod info (M11-15): a mod made by the editor carries an info entry inside its pack: name, author, version, description, edited in the editor; the game ignores it for now (a mod list in the game can use it later); mods without it still load. (Not "the file name is the mod"; not also a mod list in the game now.)
- A pack it can't fully read (M11-16; damaged, made for a newer version, unknown kinds): open what is readable, read-only, with each unreadable entry listed with its reason; the pack can't be saved over; Save As to a new file keeps only what the user chooses. (Not refusing the whole pack; not opening for editing and dropping unreadable entries on save.)
- Saving (M11-17): on command (Ctrl+S or a button); changed entries are marked in the list, the title shows unsaved work, closing asks first; a save writes the whole pack safely (a new file swapped in), as the packer does. (Not saving on every change; not also a recovery copy.)
- JSON (M11-18): a read-only JSON view of the entry beside the form, with copy and export to a file; a JSON file comes back in through Import. (Not no JSON; not an editable JSON view.)
- **The name (M11-19), not the recommendation:** "Statventure Editor", named after the engine; in the repo `Tools/Editor`. Where it sits in the shipped folder and its executable's file name are asked with M12's packaging questions ("Open questions for later milestones"). (The recommendation was "Stat Fick Editor"; not a working name until M12.)
- How the editor's screens are tested (M11-20): a few editor screens (list, form, file browser, dialogs) are rendered in Chromium from the same markup and compared, at both scales; everything the editor does (editing, undo, saving, import, references) is tested through its data and UI events; the preview pane's content is left out of the picture comparison. (Not behaviour tests only; not every screen against Chromium.)
- Language (M11-21): the editor's texts are English, like the game (the user: "English is fine").
- The "still to ask" list of 2026-11-05 is answered by these: what a mod holds and how it is named, opened and listed (M11-15, M11-9, M11-5), what the preview shows (M11-13), references to a renamed or removed entry (M11-11), what import takes (M11-14, M11-18), damaged or foreign packs (M11-16, M11-12), the language (M11-21); whether undo reaches a rename is now an editor choice under the user's rule (`Docs/Editor.md`, P14), and the M12 side (the editor's place in the shipped folder and its executable's name, M11-19) moved to M12's open questions.
- **The editor's design, P1–P38 (decided by the manager, 2026-11-06, under the user's rule for M11):** every choice as
  recommended, each with its options and reason in `Docs/Editor.md`, "Decided by the manager (P1–P38)" (among them: one
  pack open at a time over the game's packs only, P6; drafts as typed trees of field values, P10; a reference that
  doesn't resolve refuses saving, P18, with its known limit: a mod that refers to another mod's entries can't be saved by
  the editor in M11, which opening over the installed mods would lift later; the old game's dark-mode colours, P4).
- **The mod's info entry (G1, decided by the manager as following from M11-15, 2026-11-06):** the user chose an info
  entry inside the mod's pack that a mod list in the game can use later, so it is a `mod/info` entry of a record kind
  the engine knows (`mod.info`: name, author, version, description, as text); the game never reads it for now. (Not a
  kind only the editor knows; not the pack's header, a pack format change.) Not a user answer.
- **Declared limits (G4, the user's answer, 2026-11-06, the question tool): the game enforces them too.** One truth for
  the editor and the game: a mod made by hand with a value outside a limit is refused by the game with a message naming
  the entry, as the editor refuses it; in M11 only limits that the game's checks already enforce today are declared, so
  nothing that loads today stops loading. (Not the editor only; not slider ends only.) The recommended option.
- **Checks that today run only when something plays (G5, the user's answer, 2026-11-06): they also run when the data
  loads, with the cost measured first.** A broken theme or cutscene is reported when the game starts or reloads its
  data, naming the entry; the step measures the added loading time first, and if it is noticeable the manager goes back
  to the user before it goes in (`Docs/Editor.md`, steps 5 and 6: measure, report the numbers, stop for the manager's
  word). (Not the editor only, with the game as today.) The recommended option.
  **The music's check wired (the user's decision, 2026-11-07, the question tool):** after step 5 measured it and
  stopped, the user chose to wire the theme check into `MusicLibrary.Load`, with the numbers shown (a cold start's
  loading 48.9 → 69.6 ms on the game's thread, the compile of all 55 themes moving there from the warm-up thread, 3.5
  ms more work in all; warm about 3 ms more; a Reload about 5 ms more) and accepting both consequences: a layer that
  never sounds naming an instrument that isn't there is refused too, and the tests' sample libraries whose theme
  doesn't compile are refused (the tests given a playable theme). Progress, "M11 step 5"; `Docs/Editor.md`, step 5.
- **Cutscenes' checks in loading (the user, 2026-11-07, the question tool, M11 step 6's measurement shown: the proof's
  `Sequence.Load` cold 137 → 159 → 192 ms, +12 ms once the music is loaded, warm +0.2 ms):** (1) the declared limits
  and the sequence's effects-and-sounds check against the data go into loading (`Sequence.Load`); (2) the loaders that
  built the proof's data without `audio.pak` get the sound pack, as the game mounts it: the sandbox's cutscene mode
  and `JiruanIntroTests` (and any test loading `Data/Cutscenes`), so the proof still loads and the check holds
  everywhere. (Not checking a theme only when the data holds music; not the editor only.) `Docs/Editor.md`, "How it
  works", step 6.
- **The editor's own settings (G6, decided by the manager, 2026-11-06):** an `editor/` folder in the player's per-user
  game folder, beside `settings.json`, `saves/` and `mods/`; the game ignores it. Tests and tools redirect it like the
  other per-user folders (`Tests/Statventure.Tests/Infrastructure/PerUserFolders.cs`) and never write the player's real
  one. (Not a folder of the editor's own; not nothing remembered.)
- **G2 and G3, not asked now** (no M11 step waits for them): kept as open questions for the user under "Open questions
  for later milestones", "At port time". The game treats a mod with parts it can't read as today (G2); the editor and
  the packer take record kinds from any assembly they are given, so every option of G3 stays open. (G3 was closed by
  M12-9 on 2026-11-08: "Decisions for M12".)

**Decisions for M12 (answered 2026-11-08, asked before the milestone's design):** the twelve questions M12-1–M12-12
were asked on 2026-11-08 with the question tool by the manager, after reading the plan and the code, on the user's go.
Every answer was the recommended option except the shipped folders (M12-1), icons (M12-5) and versioning (M12-6); M12-7
is an in-game update check later with the first option's way meanwhile. The design doc is `Docs/Packaging.md`; the
details these answers left open, its Q1–Q28, were answered the same day (the detail round below), and so were Q29 and
Q30, which Q11's answer raised.
- **The user's go (2026-11-08):** "Then start with M12"; M12 started on 2026-11-08.
- **The rule to ask stands in full again (2026-11-08):** every design choice of M12 is asked, the editor's packaging
  included; the M11 discretion rule ("Decisions for M11", the user's rule for M11) was for M11's editor only.
- **The shipped folders (M12-1), not the recommendation: two separate downloads,** the game and the editor packaged
  apart; a modder downloads both, and the editor must be told where the game is. (The recommendation was one flat
  folder with the editor beside the game; not the editor in an `editor/` subfolder.)
- Executable names (M12-2): the game `stat-fick` on Linux and `Stat Fick.exe` on Windows (the old game's names); the
  editor `statventure-editor` and `Statventure Editor.exe`. (Not the same lower-case names on both; not display names on
  both.)
- The runtime (M12-3): self-contained .NET per platform, no trimming (the record system uses reflection). (Not
  framework-dependent; not trimmed.)
- The package form (M12-4): an archive per platform, `.tar.gz` for Linux and `.zip` for Windows; no installer, no
  AppImage (either can come later as its own step). (Not an AppImage and a portable .exe like the old game; not archives
  plus a Windows installer.)
- **Icons (M12-5), not the recommendation: none in M12;** new icons for both programs come with the port's art pass
  ("At port time"); the executables show the system's generic icon meanwhile. (The recommendation was the old game's
  icon for both for now; not a new editor icon now.)
- **Versioning (M12-6), not the recommendation: date-based versions** (in the style of 2026.11.8). The exact format
  (same-day builds, how it shows in the editor's title and the log, the git commit) is still to ask (`Docs/Packaging.md`,
  Q1–Q3). (The recommendation was 0.x.y semantic versions until the port is done; not continuing from the old game's
  1.0.0.)
- Updates (M12-7): an in-game update check, later, a port-time item (once the game is public somewhere, a check against
  that place; "At port time"); meanwhile M12 does what the first option said: no update check, and a release script
  builds both platforms' archives and uploads them as a draft GitHub Release on the private repo with the version's tag,
  which the user publishes. (Not "no check, packages only built locally".)
- Logs (M12-8): `logs/` in the player's per-user folder (`~/.local/share/stat-fick/logs`, `%APPDATA%\Stat Fick\logs`),
  one file per run, `logs/<program>-<date-time>.log`, the last 10 kept; on a crash the exception and the log's end also
  go to `logs/crash-<date-time>.txt`; the editor logs the same way. (Not beside the executable; not only on a crash.)
- The data library (M12-9; closes G3, "At port time"): a shared data library used by the game and the editor: the port
  puts its record kinds in a library, `Statventure.Game.Data`, that the game links and the editor loads from the game's
  folder; one file in each download. (Not the editor loading the game's program; not the kinds in the engine.)
- On a crash (M12-10): a plain window drawn by the engine (no OS dialog, per the game's rule) naming what went wrong and
  the crash file's path, shown for copying; then the program ends. (Not exiting quietly with the crash file only; not
  trying to go on.)
- No portable mode (M12-11): the player's folder is always the per-user one. (Not a marker file switching to a `data/`
  folder beside the program; not deciding at port time.)
- The first packages (M12-12): the sandbox as the game's stand-in, plus the editor: the whole pipeline (publish,
  archives, versions, logs, crash handling, releases) is built and proven now with the sandbox where the game will be;
  the port later replaces the stand-in. (Not the editor only; not building the pipeline without publishing.)
- **M12-9 clarified (the user, 2026-11-08, with the detail round):** the editor's download carries no copy of the data
  library; the editor loads the game's (the "one file" is the game's download's).
- **The detail round (Q1–Q28 of `Docs/Packaging.md`, answered 2026-11-08 with the question tool, after the design
  doc):** every answer the recommended option except Q1, Q11 and Q12; in full, with each option, in
  `Docs/Packaging.md`, "Decided (Q1–Q28)".
  - **Q1, the version's form, not the recommendation: always four numbers,** `2026.11.8.1`, a second release the same
    day `2026.11.8.2`; a build from the repo that isn't a release says `dev` (its commit in the log). (The
    recommendation was `2026.11.8` with `.2` only for a same-day second; not `2026.11.08` as text.) Q2: the date is the
    released commit's UTC date, the fourth number counting that date's releases (not the day the script runs). Q3: the
    version in the editor's title; the version and commit in the first lines of every log and crash file; the game's
    title stays its name (not both titles; not logs only; not the commit in the title).
  - The downloads: Q4 `stat-fick-2026.11.8.1-linux.tar.gz`, `-windows.zip`, `statventure-editor-…` likewise, each
    holding a folder named with the version, so a new version unpacks beside the old (not `Stat Fick/` unpacked over
    the old; not no folder). Q5 single-file executables with the native libraries beside them, the data library a
    file of its own (not the usual folder of about 200 files; not the natives inside). Q6 no console window on
    Windows for either (not console programs; not the editor with one). Q7 symbols embedded, stacks with file and
    line (not `.pdb` files; not none). Q8 .NET's invariant globalization for the programs and the tests (not the
    system's ICU; not ICU shipped). Q9 the Visual C++ runtime's three DLLs beside the Windows programs, local
    deployment, kept in the build cache (not required of the player; not Microsoft's installer in the zip). Q10
    `NOTICE.txt` and a `licenses/` folder (not one joined file).
  - **Q11, BRUTAL's logo and credits, not the recommendation: a BRUTAL logo splash and an About/credits view in both
    programs now** (the sandbox stand-in and the editor; the ported game gets them again in its own screens, "At port
    time"). (The recommendation was `NOTICE.txt` now and the logo and credits at port time, the packages not shared
    meanwhile; not asking Ahwoo for written permission.) Its details are Q29 and Q30 below.
  - **Q12, a readme, not the recommendation: none in the downloads;** the release's text on GitHub carries what it
    would have said. (The recommendation was a short `README.txt` in each; not nothing at all.) Q13: the stand-in keeps
    its title "Statventure Sandbox" and the release's text says it stands in for the game (not a line on its page;
    not nothing). Q14: unsigned for now (not a certificate now). Q15: the Windows archives built on Linux, so their
    `.exe` files carry no version resources for now (NETSDK1074; not on the Windows PC; not GitHub's runners).
  - The editor: Q16 a "Where is Stat Fick?" screen with its file browser on its first start (and when the remembered
    folder no longer holds the game), remembered in `editor/`; `--game` and `--repo` stay (not the usual places
    first; not the command line only). Q17 Lato regular and bold embedded (not an `editor.pak`; not only for the first
    screen), with Q23 one engine resource every single-file program carries. Q18 a game of another version refused,
    naming both (not a warning; not no check). Q19 the data library's loading now, through a stand-in
    `Statventure.Game.Data` library with a test kind in `Samples/`, carried by the packaged sandbox (not at port
    time). Q20 `--capture <png> --frames N` for the editor (not an environment variable; not no option).
  - Logs and crashes: Q21 `stat-fick-2026-11-08_14-03-22.log` in local time, the last 10 runs per program and the last
    10 crash files (not 10 in all; not crash files kept for ever). Q22 the crash window: a heading, one line naming the
    error, the crash file's path with Copy, "Open logs folder" (the system's file manager, not a dialog) and Close
    (not without the folder button; not with the stack). Q23 the editor's dark look for both, Lato embedded in the
    engine (not the game's parchment; not `ui.pak`'s Lato). Q24 with no window possible, the crash file and a
    terminal message (not a Windows message box). Q25 a crash .NET can't catch: the log only (not a crash file at the
    next start; not the crash window then).
  - Releases and checks: Q26 tag `v2026.11.8.1`, title "Stat Fick 2026.11.8.1", a fixed text the user can edit in the
    draft (not `stat-fick-2026.11.8.1`; not the commits listed). Q27 the release script refuses unless the tree is
    clean and its commit is on GitHub's `main`, and runs no tests (not the full suite; not no checks). Q28 the package
    checks in `Tools/Scripts/check-packages.sh`, run by the packaging steps and before a release, not in `test.sh`
    (not all in `test.sh`; not the Linux packages in it).
- **Q29 and Q30, raised by Q11's answer (the user, 2026-11-08, the question tool; both the recommended option;
  `Docs/Packaging.md`, "Decided (Q29–Q30)"):** Q29, the splash: the white logo (`LOGO_BRUTAL_WHT.png`) centred on the
  dark ground (Q23's look) at its own size, for 2 seconds at every start of both programs, skippable by a key or a
  click after the first second (not never skippable; not the black logo on the game's parchment). Q30, the credits:
  one engine credits view for both programs, opened by an "About" button in the editor's top bar (its 19 screens
  captured again) and one on the sandbox's page; it lists the program's name and version, "Built with BRUTAL" with
  BRUTAL's copyright line and `NOTICE.txt` verbatim, every component with its licence and copyright line (FreeType's
  credit line, OpenAL Soft under the LGPL with where its source is, the OFL fonts, the rest) and a pointer to
  `licenses/` (not F1 instead of buttons; not the sandbox's credits as a section of its page).
- **The logo's file (the user, 2026-11-08, the question tool, asked by M12 step 4 when its check found the files
  differ; `Docs/Packaging.md`, "Decided (the logo's file)"):** ahwoo.com/brutal-branding (a 301 to
  github.com/ahwootech/brutal-branding) has offered 4096 × 896 logos since 2026-10-21 (commit `5d38373a24`), while
  BRUTAL's source and README keep the page's first 712 × 168 version (`69ef8a855e`). **(a), the recommendation:** the
  page's current white logo embedded byte for byte, drawn at the size Q29 was decided with (the earlier file's
  artwork, 696 × 152 CSS px) and scaled down further only to fit the window, Chromium's mipmapped downscaling of images
  ported into the engine first (step 4's prerequisite). (Not the 712 × 168 file at its own size, soft at 1.65; not the
  4096 file at its own size, filling the window; not asking Ahwoo.)
- **Two points from step 3 (the user, 2026-11-08, the question tool; `Docs/Packaging.md`, "How it works", step 3):**
  the Visual C++ runtime's three DLLs **ship** in the Windows archives (Q9): the redistributable's own licence doesn't
  allow shipping it with a program, Visual Studio's does for its licensed users, and the user states they are (or will
  be) a licensed Visual Studio user and ships them under Visual Studio's Distributable Code terms (recorded in
  `Tools/Packaging/vc-runtime.txt`); and **the repo's address stays out of the shipped programs**: the SDK's Source
  Link wrote the private repo's address into the embedded symbols, now left out (`-p:EnableSourceLink=false`) with a
  check rule (the recommended option; not keeping the SDK's default).

**Decisions for the window's visibility (2026-11-03, the small task "send `visibilitychange` and `pagehide`"; asked after
probing Chromium 152 under Xvfb with and without a window manager; every answer the recommended option):**
- While the window is minimized, the document runs as Chromium 152 runs a hidden page: timers and tasks keep running, but
  timers wake at most once a second (Blink's aligned wake-ups, and its longer limit after five minutes hidden, as
  captured), with no frames, painting or frame callbacks. A battle's timers keep coming slowly, as in the old game. The
  sound still pauses (M8's decision). (Not everything paused while minimized.)
- When the window closes, the document gets Chromium's unload sequence without `beforeunload`: `pagehide` (persisted
  false), `visibilitychange` to hidden (with `webkitvisibilitychange`), `unload`, with Chromium's event properties; nothing
  in the document can keep the window open. (Not also a cancellable `beforeunload`, not `pagehide` only.)
- Wayland: GLFW 3.5.1 (BRUTAL's) can't tell that a window is minimized on Wayland (its `_glfwWindowIconifiedWayland`
  always answers no), so the engine takes GLFW's report as it is: on Wayland a minimized window stays visible to the
  document (and the sound doesn't pause), a known difference to check on the PC. X11 and Windows report it. (Not forcing
  XWayland, not a patched GLFW now.)
- Asked the same day after reading Blink's scheduler (`page_scheduler_impl.cc`, `frame_scheduler_impl.cc`,
  `wake_up_budget_pool.cc`, `cpu_time_budget_pool.cc`, `dom_timer.cc`, `audio_stream_monitor.cc`):
  - Audible pages: ported, by the output's level. Blink doesn't throttle a hidden page that is audible or was in the last
    30 s; the engine's `AudioSystem` measures its output as Chromium's audio service does (a power monitor, audible
    above about −72 dBFS, polled 15 times a second), and the document counts as audible while it is and for 30 s after.
    Minimizing with music on gives full-speed timers for 30 s, then throttling, as in the old game. (Not "a theme or
    effect is playing" as the proxy, not always silent.)
  - The CPU-time budget: ported, charging the engine's own measured task times (tests inject the costs): 10 s after the
    page is hidden, throttled timer tasks spend a budget (1 s at the start, refilling at 1% of the time, at most 3 s)
    and wait while it is below zero, as Blink's background CPU-time budget pool. (Not left out.)

**Decision on the speed tests (2026-11-03):** the tests that measure wall-clock speed against a bound
(`AnimatedViewTests.Speed.TheCostOfAFigureIsMeasured`, since M10 step 1 `SceneViewTests.Speed.TheCostOfAFigureIsMeasured`; `CanvasScaleTests.Speed.TwentyThousandPrimitivesRecordAndRenderQuickly`)
run alone, in the "Speed" collection (`Infrastructure/GpuFixture.cs`: never in parallel with other tests, its own GPU
device); their bounds stay. Asked because both failed now and then in the cloud's full run, also on the commit before that
day's work (the animation cost 0.89–1.06 ms per figure against 0.5 ms in the parallel run, 0.12–0.26 ms alone): the other
tests, lavapipe's rendering among them, took the 4 cores. (Not looser bounds on a CPU Vulkan, not leaving them flaky.)

**Decision on FreeType's library-level gaps (2026-11-03):** while fixing the flaky map compositing test (FreeType faces
used by two threads at once; Progress, "FreeType thread safety"), two library-level gaps were found, and the user decided
to fix them too, each with a test that fails without the fix: `FontLibrary.Add` made FreeType faces outside any lock
(FreeType wants a library's faces made and destroyed one at a time), and the LCD filter was kept in a process-wide static
though FreeType keeps one per library. (Not leaving them as noted gaps.)

**Decision on the audio output's follow-ups (2026-11-05):** while finding the flaky `OutputTests.TheDefaultDevicePacesTheGraph`
(OpenAL's process-wide current context; Progress, "OpenAL outputs in one process"), two more faults of the output were found,
and the user decided both as recommended, each fixed with a test that fails without the fix: (1) after an underrun the output
goes on with new audio: when the source is first seen stopped, before anything new is written, the buffers it already played
are taken back (they were played again from its queue's head, the last 20 ms over; not left as it was); (2) OpenAL's
default-device events get one registration shared by every live output, turned off when the last one goes (each output had
registered its own callback, so only the last one made followed the default device and the first disposed turned the events
off for all; not left as a noted gap).

**Decision on test runs and the player's folders (2026-11-05):** the test process points its own view of the per-user
folders at a scratch folder of the run, so neither a test that forgets to pass its own folder nor a mutated engine can
write into the player's real ones. Asked because the real game folder on the PC (`~/.local/share/stat-fick/saves/`)
held a test save and its backup, written on 2026-11-03 at 22:15 by M9 step 9's mutation "storage folder: the option
ignored" (`Application` ignoring `ApplicationOptions.GameFolder`) while `mutate.py` ran
`ApplicationStorageTests.WhatTheGameSavedIsWrittenBeforeRunEnds` (Progress, "Test runs kept out of the player's
folders"). What was chosen: a `[ModuleInitializer]` of the test assembly (`Infrastructure/PerUserFolders.cs`) sets
.NET's `XDG_DATA_HOME`, `XDG_CONFIG_HOME` and `HOME` (on Windows also `APPDATA`, `LOCALAPPDATA`, `USERPROFILE`) to
folders inside a per-run scratch folder in the temporary folder, removed when the process ends; fontconfig's children
(`SystemFonts`, when recording) get the real values back; a test checks that this machine's game folder and Documents
folder resolve inside the scratch folder; `mutate.py` compares the real game folder and the Documents folder's top level
before and after each mutation and flags any change loudly. Only .NET's view changes: on Linux the C library's
environment stays as it was (the Vulkan loader, Mesa and GLFW see the real one). Not known yet: whether Windows' known-folder
lookup follows the redirected variables ("Windows check", item 7). (Not making the engine's `GameFolder.ForThisMachine()`
refuse inside the test host, which would put test-only logic in shipped code; not only rewording the one mutation.)

**Decision on box antialiasing under non-uniform transforms (2026-11-05):** the cutscene proof (M10 step 7 phase 2) found that
the canvas antialiased a box's edges by one isotropic scale (`canvas_fragment.glsl`, `boxCoverage`: `sqrt(|det|)` of the local
coordinates' derivatives), so under `scaleX(0.2)` the title card's 2 px progress bar spread over about four rows where Chromium
keeps it crisp; the user decided it is fixed now, against Chromium 152 references (Progress, "Canvas: box antialiasing under
non-uniform transforms"). (Not left as a known difference.) The fix's two questions, answered the same day, both as
recommended: (1) slanted box edges (rotations, skews; Chromium fills such a box as a path, by area with its points snapped to a
quarter pixel down the screen) keep the canvas's one-pixel ramp, recorded as a known difference (M5's "Known differences left"),
the `box-slant` fixture held at the looser rotated-box tolerance (12 levels, 1 %) as a guard and the rule by `BoxTransformTests`
(not porting Skia's path coverage for boxes; not dropping the fixture); (2) of the painter differences found on the way, two are
ported, each its own small step with a Chromium fixture after this fix is merged: a transformed box's paint offset rounded with
its fraction kept only along the axes the transform doesn't scale (Blink's `CanPropagateSubpixelAccumulation`), and
background-image layers antialiased once at a fractional edge (the layer clip and each tile antialias it now); the shadow's
clip-out inset, the bleed avoidance under opaque rounded borders and the square corner's product coverage are recorded as known
differences, with the unchecked blurred shadow under a non-uniform scale (not porting all five; not recording all five).

**Decision on background layers (2026-11-05 and 2026-11-06; decided; ported, on `main` since its merge):**
for the background-image layers' double edge above, the user chose to port **the whole Blink background-layer rule,
seamless tiling included** (not only the single fractional edge), approved the small addition to the canvas's API for
tiles (`Canvas.PushTile`/`PopTile`, the `premultiplied` flag on `DrawImage`; no shader or batching change), and decided
that `background-clip` and `background-origin` **stay unsupported** for now (parsed and ignored; the game uses neither;
M5's known differences). The port: Progress, "Background layers as Blink paints them" (2026-11-06). **Clip-layer bleed
avoidance: recorded as a known difference (the user, 2026-11-06, the recommended option).** Blink's clip-layer bleed
avoidance (`kBackgroundBleedClipLayer`: a rounded box without an opaque border whose background has an image over a
colour, or two layers) paints the whole background into a layer and clips it once by the rounded rect; the painter clips
each draw by the same rounded rect, so a corner's edge pixels show the colour under the image's edge where Chromium
doesn't. No game screen has such a box (none of the 22 screens at 1× and 1.65, nor any paint fixture, paints one); in the
background fixture's `two` box the corner pixels are up to 59 levels off at 1× and 85 at 1.65, and painting the layer
exactly (measured with the tile API) brings that to 37 and 56 and 2 pixels fewer past 12 levels. Not chosen: painting it
exactly with the tile API (the background into a texture of the box's pixels, drawn once through the rounded rect: no
canvas change, one texture per such box and frame, exact for untransformed boxes), and giving the canvas a layer whose
composite alone is clipped (most general; a canvas change). The surviving mutation "the clip-layer bleed avoidance not
clipping (as no bleed avoidance)" rests on this decision (the approximation draws exactly that). M5's known differences
list it.

**Decision on the paint offset of transformed boxes (2026-11-05):** porting the first of those two rules showed it wider than the
box: Blink rounds a transformed box's paint offset (its paint offset translation), keeps the fraction only along the axes it
can propagate along (`CanPropagateSubpixelAccumulation`), measures the transform's origin from the rounded place, and snaps
the box and everything inside it from there, so children, text and the rest move too (at 1× a chip inside a `scale(0.8)` card
at 20.5 px sat 0.9 px off), and the boxes Chromium composites (`will-change: transform`, transform animations) differ besides
by cc rastering their layer by itself. The user chose option (a): the full rule for the transforms Chromium paints with the page
(every snapped position under a transformed box measured from the rounded origin, through the painter's shared snapping: the
box, its children, text, decorations, clips, scrollbars, outlines, controls, SVG roots; the transform's origin from the rounded
place) and the which-fraction-is-kept rules for `will-change` and transform animations; glyph placement stays in device space
as Skia's does; cc's composited-layer raster is not ported, recorded as a known difference (M5's "Known differences left"),
those cards compared by measured geometry rather than pixels (Progress, "Paint offset of transformed boxes"). (Not the box's own
geometry only; not cc's layer raster too.) The user approved the same day the measured edges' tolerance as set: within 0.1 px of
Chromium's.

**Decision on blurred shadows under non-uniform transforms (2026-11-05):** the cutscene proof's title card (S7-14) found the
progress bar's glow (`box-shadow: 0 0 12px`) under `scaleX(0.2)` blurred across the bar with σ 6 where Chromium's is about
2.7 (6 × √0.2); the user decided the rule is ported now (not left as M5's unchecked known difference). Ported (Progress,
"Canvas: blurred shadows under non-uniform transforms"): Blink's shadows respect the transform, so Skia's blur mask filter
takes the sigma to the device with one factor, `SkMatrix::mapRadius`, at most 128, and blurs the shape as placed there,
the same in every direction; the canvas computes that blur in the box's units as a correlated Gaussian. Three more rules
found on the way and measured, asked the same day through the managing session and answered by the user, all as
recommended: (1) Blink gives a box whose transform is a 2D scale an effect node of its own (`NeedsEffectFor2DScaleTransform`,
crbug.com/40084005), drawn as a layer bounded by what the box and its descendants paint in their own units, which cuts a
shadow blurred wider than that on the device: **recorded as a known difference** (M5's "Known differences left"; not ported
in the painter); (2) Blink casts an inset shadow from a frame the blur radius wide around the padding box
(`AreaCastingShadowInHole`), the canvas from everything outside the hole: **ported**, as its own step after this one is
merged, on a fresh branch (the canvas shader and the unit test's inset expectation; every existing reference re-checked: the
user was told at most 4 levels, `css-shadows` nearer Chromium, the game screens 1–3 levels, and anything moving more is
reported first); (3) Skia blurs a rectangle with its edges rounded to whole pixels (`SkBlurMask::BlurRect`, the nine-patch):
**recorded as a known difference**. And `shadow-rotate`'s limit: **the same as `box-slant`'s, 12 levels / 1 %** (slanted box
edges). The fixtures `shadow-scale-x`, `shadow-scale-y` and `shadow-rotate` stay held until that step: it un-holds
`shadow-rotate` at 12 / 1 % and reports `shadow-scale-x` and `shadow-scale-y` after the inset port (if they still exceed the
paint fixtures' 0.2 %, by how much and from which of the two known differences, as a tolerance question for the user). (Not
porting the 2D-scale effect node or Skia's rect rounding; not leaving the inset caster as a difference.) Done (Progress,
"Canvas: inset shadows cast from Blink's frame"): `shadow-rotate` passes at 12 / 1 %, `shadow-scale-x` and `shadow-scale-y`
at 1.65 at the paint fixtures' 12 / 0.2 %; `shadow-scale-y` at 1× keeps 0.214 % (1,059 pixels against 992). The user's
answers on it (2026-11-05), told plainly that every game screen moves slightly away from Chromium: **the inset port is kept
as it is** (the frame in eight rows; not four rows with the unit test's limit raised, not Skia's kernel ported now), and the
blur kernel's shape is **a known difference and a candidate for a later step**: the canvas blurs with a true Gaussian,
Skia's mask blur (`SkMaskBlurFilter`) with three box passes of about 1.88σ, whose support ends near 2.8σ, so the frame
takes away the Gaussian's tail beyond 2σ (up to 2.3 % of the colour) where Chromium's barely has one; the 22 game screens'
blurred inset shadows move by 1 level, `game-sheet` by up to 3 (1×) and 4 (1.65, 2 pixels), each a little further from
Chromium (pixels beyond 12 levels: `game-sheet@1.65` 174,193 → 175,652, `game-settings@1.65` 51,628 → 51,670, the rest by
0–18), all within their tolerances. **`shadow-scale-y` at 1× is held to 12 levels / 0.25 %** (at 1× only; 1.65 keeps the
paint fixtures' 0.2 %) for three causes: the 2D-scale effect node's cut, Skia's rect-blur rounding and the rounded-corner
blur (Skia blurs a round rect's mask, the canvas Wallace's closed form). (Not a wider limit for every shadow fixture.)

**Text first in a paint chunk (2026-11-06; a bug fixed as Chromium behaves, nothing to decide):** M11 step 4 found the
editor's status line (`overflow: hidden`) drawn grayscale where Chromium draws it with LCD text; the manager gave it a
worker of its own on the painter (branch `lcd-first-text`, made on `8c06f86` and rebased on `42fdd90`; Progress, "Text first in a paint chunk"). The
cause: in the draw pass `PaintTarget` set `Canvas.TextLcd` only when it made a chunk, and a display item joins (and
makes) its chunk only when it ends, after its text was drawn, so text that is the first drawing of a chunk took the
previous chunk's verdict. Chromium rasters every item with the verdict of the cc layer its chunk went to (cc's tiles,
`use_lcd_text`), and an item joins the chunk current when it ends (`PaintChunker::IncrementDisplayItemIndex`); the port
gives each text drawing the verdict of that chunk, made or not yet (`PaintChunker::WillCreateNewChunk`). It went both
ways (grayscale where Chromium has LCD after a chunk without text, LCD where it has grayscale after a layer with LCD text)
and reached no game screen, paint fixture, card or compositing reference: of the existing references only the editor's
two screens moved, nearer Chromium. No limit or tolerance changed; no golden but the new fixture's. Found on the way and
left as they are (reported, not this fix's; both open in "Small tasks left from earlier milestones", no go yet): a select's or input button's label sits a device pixel below Chromium's at
1.65 (`UiPainter.SnapBaseline` rounds the baseline to a whole CSS pixel, Blink in device pixels); and at 1.65 the text of
a composited 56 × 16 px box with an opaque background (its bottom on a fractional device pixel) is grayscale in the engine,
LCD in Chromium, the layerization's verdict itself (seen with the new fixture's first version, not looked into further):
likely the text's visual rect in the opaque analysis, the font's ascent to descent (`PaintTarget.GlyphBounds`) where
Blink takes the glyphs' ink, reaching one device pixel past the enclosed opaque rect.

**Decision for the repo housekeeping task (2026-11-03):** the root `.gitignore` ignores `/.claude/worktrees/` (the worker
worktrees) and `/.claude/settings.local.json` (one machine's settings), not the whole `.claude/` folder, so shared Claude
Code files (`.claude/settings.json`, hooks, skills) can still be committed. (Not ignoring all of `.claude/`.)

**Decisions for the old game's small changes (2026-11-03, the user's list; asked before the code):** the details and
what was done are in `../old-game/Docs/Notes.md`, "Small changes (2026-11-03)". Checked first against the engine work:
none of them gets in its way (the tests use a copy of the stylesheet and frozen captures; the game's tables are converted
at port time). The rune slotting sound was left out of the old game; the port adds proper sound effects instead
("Decisions for M8").
- Levelling: a fast start, then a steeper curve: 3–6 same-level kills per level up to 12 (51 to reach 12), rising to 24
  per level at 99 (186 to reach 30, 388 to 50, 1265 to 100; today 83, 251, 475, 1075). EXP targets stay (level+1)×100.
  (Not ending at 18 or 14 per level.)
- Vial of Dragonblood: every dragon-race enemy drops it at 0.5% (Mountain Wyrm's chance), Anatemis included; Nygheir
  keeps 1%. (Not 1% for all, not ordinary dragons only.)
- Matching weapons (all parts by one maker; masterworks' Flawless included, not armour, shields, staffs or wands) get a
  bonus of their own per maker, different from its usual one: Roundtable +3% final power, Dwarven +5% armor pierce,
  Elven +5 Accuracy, Boreal Bolt +5% base power, Abyss +5 Crit Rating, Hellmade +5% secondary scaling, Hearthforge +3
  Accuracy and +3 Crit Rating, Outlaw nothing. (Not half or all of its usual bonus again, not doubled.)
- The selected target: stage, a larger, brighter marker, a pulsing gold ring at its feet and a gold edge on its name
  tag; classic, a thicker border with a soft glow. (Not a "Target" label, not dimming the others.)
- Temporary boost fields: removed from the player's sheet and the companions'; saved temporary values stop counting.
- Dev Mode: the "Dev-mode · On/Off" text is hidden; "Stat Fick" and the ✦ button stay. (Not hiding the name too.)
- Equipment slots: the item or empty-slot button fills its whole field in every slot. (Not the off-hand only.)
- Money (the purse) can be edited only in Dev Mode.

**Decision on the user's tasks for the port (2026-11-05):** the task files the user wrote are kept unchanged in
`Docs/Port-Tasks/` (named by their order: 1 Masterwork Elementals, 2 Guild, 3 Shop Rework, 4 Artifact Rework, 5 New
Masterworks, 6 New Areas, 7 New Game Plus, 8 Map Rework, 9 Potion Rework, 10 Class Rework, 11 New Races, 12 Spell
Rework, 13 New Classes, 14 Dungeons; its README sums them up and notes what the order means for them). The text is
kept as written except three names in task 7 the user confirmed the same day (Laila → Juila, Zik → Zik Lun, Mythium →
Mythilium); task 7's Limitless scaling is meant to cover every later kind of enemy too (task 14's dungeon enemies), and
the class count going past 250 with task 14's two classes is intended. A general port task added on 2026-11-11 (the
user): every enemy is revamped during the port, since task 10 changed the classes and task 12 will change the spells
("Open questions for later milestones", "At port time"). They are added to the game during the port, in that order, and read when the port
starts. (Not a top-level folder, not in `../old-game/Docs/`.) Still open: whether they come after the faithful port or
while porting (the user will answer later).
- **Task 1 is done in the old game now (user decision 2026-11-05):** before the port, with placeholder looks for the six
  masterworks (each type's normal forged look plus simple hints); the user wants them fully remade visually in the port.
  The other tasks are decided one at a time (not yet the same approach for all 14). Its design answers are in
  `../old-game/Docs/Notes.md`, "Masterwork Elementals"; `Docs/Port-Tasks/README.md` lists the placeholder art. The port
  brings task 1 over with the rest of the game and doesn't redo it from the task file; its only port work is that art.
- **Task 2 is done in the old game too (user decision 2026-11-06, G1):** before the port, old game only (the engine
  untouched), with placeholder art for the two guild interiors (G2) that the user wants fully remade in the port. Its
  design answers are G1–G34 just below; where each rule lives is in `../old-game/Docs/Notes.md`, "Guild Rework";
  `Docs/Port-Tasks/README.md` lists the placeholder art and what the remake keeps. The port brings task 2 over with the
  rest of the game and doesn't redo it from the task file; its only port work is that art.
- **Task 3 is done in the old game too (2026-11-06, the user: "same rules apply"; R1):** old game only, placeholder art
  (the Tailor's, Institute's and Hearth Home's scenes, their map icons, Doctor's Orders' look, the reforge anvil, the
  traps). Decisions R1–R29 below; `../old-game/Docs/Notes.md`, "Shop Rework". Merged through a pull request.
- **Task 4 is done in the old game too (2026-11-06; A1):** old game only, placeholder looks for every artifact and the
  four masterwork artifacts. Decisions A1–A32 below; `../old-game/Docs/Notes.md`, "Artifact Rework". Merged through pull
  request #10 with task 6 on 2026-11-06.
- **Task 6 is done in the old game too (2026-11-06; N4):** old game only, placeholder looks for the six Abyssal Rift
  enemies and the Rift on Gloamfen's map. Decisions N1–N10 below; `../old-game/Docs/Notes.md`, "New Areas". Merged through pull
  request #10 with task 4 on 2026-11-06.
- **Task 5 is done in the old game too (2026-11-06; M1):** old game only, placeholder art (the Sparkling look,
  Treasure Seeker's drawing, the new weapons' and the Hexbane Coat's forged looks). Decisions M1–M26 below;
  `../old-game/Docs/Notes.md`, "New Masterworks". Merged through pull request #11 on 2026-11-06.
  For all six done tasks the port brings them over with the rest of the game, doesn't redo them from the task files,
  and only remakes the art `Docs/Port-Tasks/README.md` lists.

**Decisions for task 2, Guild Rework (2026-11-06, a cloud session; the user asked for task 2 that day; the task is
`Docs/Port-Tasks/2_Guild.txt`).** Old game only: it doesn't touch the engine, and engine work continues beside it (the
user, 2026-11-06; "How to continue"). Asked before any code, four at a time, after reading the old game's guild, quests,
recruiting, information, encounters and class menu:
- G1 **Where:** in the old game now, as task 1 was (checked with its test client, one check per rule); the faithful port
  brings it over. (Not only recorded for the port.)
- G2 **Art:** the two interiors (castle-city guild, town guild) are placeholder art for the port's full remake
  ("Placeholder, so don't put much effort into it"): simple ink scenes in the game's style, marked PLACEHOLDER ART in
  `scene-banners.js` and listed in its `PLACEHOLDER_ART`, and in `Docs/Port-Tasks/README.md` with what the remake keeps.
- G3 **Town guilds** (Cairnrest, Emberhaven, Lastlight, Amberpost): no hooded man (no information there); only a few
  adventurers, so their recruit selection is much smaller than a castle city's (G5). They keep quests, artifacts and
  Great Hunts.
- G4 **Active quests:** one standard quest at a time, as today; Great Hunts are the exception and can always be accepted
  next to it, but only one Great Hunt at a time (G6). The artifact challenge stays separate.
- G5 **Recruits offered:** castle-city guild 5, town guild 2 (today 3 everywhere).
- G6 **Great Hunts:** one active Great Hunt at a time (not any number).
- G7 **Daily board:** per guild, per in-game day: drawn on the first visit of each day (the day turns at midnight) and kept
  all day, also when the player's level changes; accepted quests are never touched by it.
- G8 **Offers per day:** castle-city guild 4 standard quests, town guild 3, each a different type; Bounty Broker adds one in
  both; a Great Hunt is added when eligible, as today.
- G9 **Reach** (Deliver, Escort, Travel): any adjacent region (one road or boat away), whatever its level; the quest's
  description names the destination region's recommended level. Settlements the player can't enter are skipped.
- G10 **Destinations:** Escort and Travel both pick any settlement other than the guild's own, preferring other regions
  (no town/castle-city split). Deliver goes to an adjacent region's guild.
- G11 **Cull count:** 5, plus 1 per 20 levels of the quest, at most 10.
- G12 **Cull area:** a marked circle about 6 km across inside a zone where the target lives; only kills inside it count.
- G13 **Cull spawns:** inside the circle half of all encounters (travel and Force encounter) are the target in one of its
  usual groups; the encounter chance itself is unchanged.
- G14 **Escape reductions** (Search −50%, Escort bandits −70%): only the pre-fight escape (its final chance, after the
  +25 bonus); Run in battle stays normal.
- G15 **Dealt with:** winning or fleeing counts for both Search and Escort encounters; a defeat leaves it open (search
  again; the bandits strike again later on the way) and applies the usual defeat rules.
- G16 **Ambush timing:** the 80/10/10 outcome is rolled once on accepting; each ambush gets a random point between 25% and
  75% of the outbound trip's travel time (a second at least 20% later) and strikes there, on roads too; arriving first
  makes it strike on arrival.
- G17 **Bandits:** the nearest existing bandit group, unchanged (bandits exist at levels 4, 10, 15, 36, 44). (Not raised
  to the level, not new bandit enemies.)
- G18 **Rewards:** quest level = the player's level clamped to the guild region's level range (the destination doesn't
  count); today's formulas (silver 100 + 35 × level, Reputation (8 + 1.5 × level) / 2) times Hunt 1.8, Cull 1.55,
  Deliver 1.35, Search 1.2, Escort 1.1, Travel 1.0; Great Hunts keep ×2.4 (Reputation ×3 on top) at the boss's level or
  the quest level, whichever is higher.
- G19 **Map marker in another region:** until the player is in the destination's region, the road exit (or harbour)
  leading there carries the quest marker; in its region the settlement itself is marked.
- G20 **Search spot:** in the guild's region, in a zone suited to the player's level (as today), away from settlements,
  sites, region exits and other markers.
- G21 **Layout:** the guild on the left (the bulletin board on the wall beside the counter and its clerk), the tavern on the
  right (a hearth, a table of adventurers, and in castle cities the hooded man alone at a corner table), hunting trophies
  on the back wall. (Not mirrored.)
- G22 **Menus:** a panel docked along the bottom of the window, the scene above it. (Not a card beside the object, not a
  centered card.)
- G23 **Hover:** the object's outline lights up in a soft candle-gold glow with a small name label (Bulletin board,
  Counter, Adventurers, Hooded man, Trophies), fading in and out quickly.
- G24 **Completion feedback:** a small parchment note rises from the counter with a coin and the gold, a seal and the
  Reputation (and any items), stays about 3 seconds and fades; clicks pass through it.
- G25 **Great Hunts completed** (rematch markers): the trophy wall opens it, clickable once a Great Hunt of that guild is
  done.
- G26 **Viewing and giving up quests:** the active quests are viewable and can be given up at all times, not only in
  the guild (the user's answer to "on the board or at the counter?"); G29 says where.
- G27 **Board look:** the day's quests as pinned parchment notices with short generated titles; clicking one shows its
  type, objective, place, enemy or kill count, gold, Reputation and special rules beside it, with Accept.
- G28 **Recruit level:** the middle of the guild region's level range, never above the player's level.
- G29 **Quest view:** a Quests button among the map's buttons (with a key) opens a small card with the standard quest and
  the Great Hunt: objective, place, progress, reward and Give up (with a confirmation). (Not in the sidebar.)
- G30 **Recruit cost:** 30 + 2 × the recruit's level, plus 25 per companion already in the party (level 10: 50, level 50:
  130, level 90: 210 with an empty party).
- G31 **Recruit gear:** one melee weapon and one body armor their race can wear, both rolling rarity like the
  Blacksmith's stock at the region's level (not common-only, not shields).
- G32 **Class information button:** shows what was bought at the guild, the revealed requirements page, nothing more.
- G33 **Old saves:** an active quest of an old kind (Clear, Escort to a map point, Travel within the region) is dropped
  with a note in the guild. (Not kept.)
- G34 **Courier:** Deliver counts as a travel quest for Courier's requirement and its Dispatch Guard, like Travel and
  Escort.

**Decisions for task 3, Shop Rework (2026-11-06, a cloud session; the user asked for task 3 that day, "same rules
apply" as task 2: built in the old game, old game only, engine work continues beside it; the task is
`Docs/Port-Tasks/3_Shop_Rework.txt`).** Asked before any code, four at a time (R24–R29 while building, as they came
up):
- R1 **Art:** placeholder, as task 2 (the Tailor's shop, the Institute of Magic and Hearth Home on the map and inside, the
  Doctor's Orders look, the reforge anvil, the five traps): simple ink art marked placeholder in the code and listed in
  `Docs/Port-Tasks/README.md` for the port's full remake.
- R2 **Reforge part pool:** every shop-made part (every maker and material that can make that part, as the shops forge
  them), never above the main part's rarity; not Outlaw parts (loot only) and not Vampiric parts.
- R3 **Reforge cost:** 3× the base part price per changed part, by the new part's rarity (common 360, uncommon 1,800, rare
  7,200, epic 18,000, legendary 36,000, masterwork 72,000 silver); unchanged parts cost nothing.
- R4 **Items on the anvil:** forged items in the player's pack, equipped ones included, and the gear the companions wear.
- R5 **Masterworks** (Hearth Home only): the main part and a fixed main maker never change; the two other parts can be any
  shop-made part up to masterwork rarity, Mythilium included; fixed runes stay; never Outlaw.
- R6 **Staffs and wands:** the handle's rarity is capped by the jewel's or gem's tier: Lesser up to uncommon, Regular up
  to epic, Greater up to legendary; the inscription can be any ink. (Not "handle up to its current rarity", not no limit.)
- R7 **Tailor:** beside every Blacksmith (castle cities and towns), castle cities stocking 6 clothing pieces, towns 4,
  turning over with the other shops.
- R8 **Blacksmith after the move:** no clothing bought or sold there; its armor shelves shrink to castle 4, town 3 (heavy,
  light armor and shields), so the offer across both shops stays similar. (Not the same count.)
- R9 **Institute of Magic, runes:** fitting and removing runes both 20% off; runes themselves are still bought at a Magic
  Tower. (Not fitting only, not selling runes.)
- R10 **Institute of Magic, place:** an island in the middle of Mirrormere's lake, a bridge of about 37 km running west
  from the ring road on Lakespire's side. (Not nearer the east shore.)
- R11 **Doctor's Orders:** a white Robe with an Institute of Magic main part, forged at the buyer's level when bought (like
  the Stormhide Cuirass), always on offer at the Institute, 30 Platinum.
- R12 **Doctor's Orders, healing given:** every heal the wearer gives to anyone on their side, themselves included, is
  50% stronger. (Not others only.)
- R13 **Doctor's Orders, healing received:** +25% on every HP restored to the wearer in battle (spells, skills, potions,
  regeneration, lifesteal, item effects), whoever gives it; resting and camping are not raised.
- R14 **Hearth Home:** on the volcano's south-west edge where Cinder Isle's road ends (the road extended a little). Ornn
  (Level 48, his Vulcanic Ram, mechanics and drops unchanged) is fought there; his defeat opens Hearth Home, and he can be
  challenged again, like Tankina.
- R15 **Ornn in old saves:** Hearth Home opens only by beating Ornn there (a completed Ornn Great Hunt does not count); an
  active Ornn Great Hunt is withdrawn with a note (no reward, no cost); his rematch markers are removed.
- R16 **Staff and wand reforge cost:** a changed handle costs 3× its rarity's part price; a changed inscription 3× the
  part price of the handle's rarity.
- R17–R21 **Five new traps,** one per rarity: Tripwire (common, about 250 silver: when an enemy uses a melee attack on you,
  it loses 1 AP on its next turn), Smoke Trap (uncommon, about 900: when an enemy attacks you, you and your allies gain
  +20% Evasion until your next turn), Frost Glyph (rare, about 2,800: when an enemy damages you with a spell, its Speed
  drops 25% for 2 turns), Weighted Net (epic, about 6,500: when an enemy uses a melee attack on you, it is stunned for 1
  turn), Gravity Well (legendary, about 16,000: when an enemy attacks you, every enemy loses 2 AP on its next turn).
- R22 **Trap sales:** in the General Store's stock like the existing traps, rarer ones more often at higher levels.
- R23 **Map keys:** Tailor U, Institute of Magic I, Hearth Home H.
- R24 **Doctor's Orders, which heals are given (2026-11-06):** everything the wearer causes: their spells, skills and
  items (potions too), each tick of a heal-over-time they cast, team-healing passives they own, and their own lifesteal
  and regeneration. (Not actions only, not spells and skills only.)
- R25 **Doctor's Orders, self-heals:** a heal the wearer gives themselves gets only the larger bonus, ×1.5 (given), not
  given and received together. (Not ×1.875, not ×1.75.)
- R26 **AP losses add up:** Tripwire's and Gravity Well's AP losses add to each other and to any other AP loss the enemy
  carries into its next turn (a boss stagger, roots). (Not the largest only.)
- R27 **Doctor's Orders, one wearer healing another:** both bonuses apply, multiplied: ×1.5 × 1.25 = ×1.875. (Not the
  larger only, not added.)
- R28 **Smoke Trap, a fallen owner:** if the trap's owner falls before their next turn, each ally keeps the smoke until
  that ally's own next turn begins. (Not lifted at once, not kept for the battle.)
- R29 **Frost Glyph, an absorbed spell:** any spell hit triggers it, even one a barrier soaks entirely (0 damage). (Not
  only spells that take HP.)

**Decisions for task 4, Artifact Rework (2026-11-06, a cloud session; the user asked for task 4 that day after task 3
was merged; built in the old game like tasks 1–3, old game only; the task is `Docs/Port-Tasks/4_Artifact_Rework.txt`).**
Asked before any code, four at a time (A16 is the user's approval of the drafted list; A17–A32 came up while building):
- A1 **Art:** placeholder, like tasks 2 and 3: simple distinct looks, marked placeholder in the code and listed in
  `Docs/Port-Tasks/README.md` for the port's full remake. (Not proper art.)
- A2 **A rarity's shared look:** each rarity has its own metal, gem cut and setting style; each artifact keeps its own
  object (ring, amulet, charm…) with a few details of its own. Masterwork artifacts each look completely their own.
  (Not one object per rarity.)
- A3 **The old artifacts:** the whole non-masterwork set is replaced by new artifacts with new names (at least 32
  uncommon, 24 rare, 16 epic, 8 legendary). (Not keeping the old ones with new effects.)
- A4 **Designing the effects:** I draft the full list (names, effects, numbers, prices, the masterworks' numbers along
  the ramp) for the user to review; nothing is built before they approve or adjust it.
- A5 **A masterwork artifact's level:** fixed when obtained, like masterwork weapons and armor (it drops at your level and
  keeps it, shown on its card). (Not following the wearer.)
- A6 **The ramp:** a steady line from Level 1 (about uncommon strength) to Level 80 (about legendary), rising at the same
  rate after, through 100 and on into task 7's Limitless levels. (Not slow-then-fast, not flattening past 80.)
- A7 **The masterwork artifacts' effects:** Second Wind, Wraithguard, Arrowtip Rain and Titanfall keep their mechanics;
  their numbers follow the ramp (the draft lists them at Levels 1, 40, 80 and 100). (Not redesigned.)
- A8 **Old artifacts in saves:** removed from the inventory (the user's own answer), everywhere they are (pack, worn
  slots, companions' gear). (Not converted to new ones.)
- A9 **Telling the player:** silently, no note. (Not a note on load.)
- A10 **An active Guild artifact challenge whose prize was removed:** withdrawn with a note, its 70 Reputation refunded.
  (Not a new prize, not withdrawn without a refund.)
- A11 **Old saves' masterwork artifacts** (no level yet): Level 80. (Not the owner's level, not Level 1.)
- A12 **Sources:** the Guild's artifact challenges as today, and also rare enemy drops (A13, A14). Masterwork artifacts
  keep dropping where they do. (Not shops.)
- A13 **Drop chance:** 1% for every defeated enemy; bosses drop none (the user's own answer). In the code: every enemy
  the battle's loot roll covers (not summons), except the 12 `unique` enemies. (Not 1% with bosses at 10%, not 0.5% or
  2%.)
- A14 **Drop rarity:** any rarity from uncommon up, weighted by the enemy's level the way shop stock is
  (`ShopRules.rarityWeight`). (Not strictly the enemy's or the player's level band.)
- A15 **Reviewing the draft:** in the chat, rarity by rarity. (Not a page, not a repo file.)
- A16 **The draft, approved as drafted** (2026-11-06, "no changes needed"): 32 uncommon, 24 rare, 16 epic and 8 legendary
  artifacts with their effects, prices and looks, and the four masterwork artifacts' numbers at Levels 1, 40, 80 and 100
  (Second Wind 3/6/9/10.5% heal; Wraithguard hit +10/30/50/60 and guard 2/6/10/12%; Arrowtip Rain 6/15/25/30% splash;
  Titanfall's golem at 40/84/130/153% strength; prices 2,000/18,800/36,000/44,600 silver). The list itself lives in
  `../old-game/objects/artifacts.json` and `../old-game/Docs/Notes.md`, "Artifact Rework".
- A17 **Triage Pouch and Mender's Thimble with Doctor's Orders:** each item's bonus multiplies separately (received
  ×1.25 × 1.10, given ×1.5 × 1.08). (Not added into one bonus.)
- A18 **Phoenix Plume and Warden's Bell:** heroes only (you and your companions), not summoned creatures.
- A19 **Starfall Orb's star:** like a Holy spell hit: the target's Holy resistance applies and it counts as spell damage
  (spell traps and wards react), but spell power doesn't raise it. (Not fixed true damage, not a full spell.)
- A20 **The "+X% damage" artifacts** (Greenskin Tooth, Silver Nail, Dragonbane Scale, Spirit Lantern, Executioner's Mark,
  Giantslayer Torc, First Light Pin, Tide-turner Compass, Banner of Kings): everything the wearer deals: weapons, spells,
  traps, thorns and item damage. (Not weapons and spells only, not weapons only.)
- A21 **Self-heals with several healing items:** every bonus multiplies (all given × all received); only Doctor's Orders
  with itself keeps R25 (its larger bonus on a self-heal), times the other items' bonuses. (Not the larger total only.)
- A22 **Battle Hymn Chime and Soulharvest Sickle:** they trigger whenever an enemy falls to your side, whoever defeated
  it. (Not only the wearer's killing blow.)
- A23 **Rallying Banner Pin and Banner of Kings:** summoned creatures count as allies too. (Not heroes only; Phoenix
  Plume and Warden's Bell stay heroes only, A18.)
- A24 **Forked Lightning Rod:** the strike is 25% of the damage the first target took, dealt as the same element to the
  second enemy as a spell hit; its resistance and wards apply. (Not an exact 25%.)
- A25 **Bandit's Coin and Studious Quill on a companion:** the Coin raises the party's silver whoever wears it (two add
  up); the Quill raises only its wearer's EXP. (Not both party-wide, not only on you.)
- A26 **First Light Pin:** the bonus waits for your first attack or spell that deals damage. (Not spent on a miss.)
- A27 **What counts as a hit** (Warding Cord, Second Chance Locket): any damage an enemy causes: attacks, spells, traps,
  thorns and damage over time. (Not attacks and spells only.)
- A28 **Phantom Step Pin:** the +25% goes to your next melee or ranged weapon attack. (Not a spell.)
- A29 **Phoenix Plume, the wearer falling first:** they rise at 30% HP when their next turn would have started in the
  turn order, and take that turn. (Not others only.)
- A30 **Phantom Step Pin on a miss:** the bonus is spent on the next weapon attack, hit or miss. (Not kept until it
  lands.)
- A31 **Forked Lightning Rod and spells that hit several enemies:** one extra strike per cast, on a random enemy the
  spell didn't hit, for 25% of the largest damage it dealt; none if it hit every enemy. (Not one per enemy hit.)
- A32 **Banner of Kings while its wearer is down:** the bonus lasts the whole battle once it starts. (Not paused.)

**Decisions for task 6, New Areas (2026-11-06, a cloud session; the user asked for task 6 that day while task 4's last
checks ran, "That task is very small"; built in the old game like tasks 1–4, old game only; the task is
`Docs/Port-Tasks/6_New_Areas.txt`).** Asked before any code, four at a time:
- N1 **Kind of area:** a new zone on Gloamfen Marsh's own map, with its own encounter table and level range. (Not a new
  region.)
- N2 **Place:** the marsh's empty south-west, between the Rocky Rise and Reedcross; nothing existing moves. (Not inside
  the Deep Mire, not the north-east.)
- N3 **Levels:** 45–55, a little above the marsh's hardest zone. (Not 40–50, not 50–60.)
- N4 **Art:** placeholder, like tasks 2–4: the enemies' looks from the existing sprite kits (darkened, corrupted,
  glowing eyes) and a simple abyss painting on the map, marked placeholder for the port's full remake.
- N5 **Designing the enemies:** I draft each enemy (name, race, level, classes and stats in short, spells or trait,
  look), the zone's name and its encounter groups in the chat for the user's review; nothing is built before approval.
- N6 **The corrupted ones:** corrupted versions of Gloamfen's own creatures (e.g. Bog Troll, Mire Stalker, Mossback
  Lamia). (Not Alder Vale's at a higher level, not a mix.)
- N7 **Loot:** like other enemies: their gear, the usual chances and task 4's 1% artifact drop; no special item.
- N8 **Delivery:** one pull request for tasks 4 and 6, each described separately. (Not task 4 first.)
- N9 **The draft, approved as drafted** (2026-11-06, "Enemies look good"): the zone is the Abyssal Rift in the marsh's
  south-west (Levels 45–55, a 50% encounter chance, a dark violet-black scar with a red-glowing heart on the map); the
  corrupted creatures are Corrupted Bog Troll (47), Corrupted Mire Stalker (50, its strikes Dark) and Corrupted Mossback
  Lamia (53, Dark spells instead of her nature ones), all `demon` race (Holy hurts more, Dark not at all) with darkened
  looks and red eyes; the demons are Abyssal Imp (impid, 46, a rogue-warlock in pairs), Rift Fiend (demon, 50, a clawed
  brute) and Abyss Witch (demon, 54, a caster); nine encounter groups from Level 45 to 55.
- N10 **The marsh's level range** (2026-11-06, asked while writing the checks): raised from 35–50 to 35–55 to take in
  the Rift: the map header reads "LEVEL 35–55" and guild quests in the marsh go up to Level 55. (Not kept at 35–50.)

**Decisions for task 5, New Masterworks (2026-11-06, a cloud session; the user asked for task 5 that day after tasks 4
and 6 were merged; the task is `Docs/Port-Tasks/5_New_Masterworks.txt`).** Asked before any code, four at a time:
- M1 **Where and art:** in the old game like tasks 1–4 and 6, with placeholder art listed for the port's full remake.
  (Not finished art now, not only noted for the port.)
- M2 **Who can be Sparkling:** any regular enemy at the start of any battle (travel, forced, guild Hunt, Cull, Search and
  Escort fights); never a summon, and nobody in a battle that has a boss (a boss's aids: Saffron's Servants, Zik Lun's
  warband, Tankina's Apostles and the like). (Not travel and forced only; the Empowered Hunt target can be Sparkling.)
- M3 **Luck:** the 0.1% chance is multiplied by (1 + the player's Luck / 500), at most 0.2% (Luck 50: 0.11%, 100: 0.12%,
  300: 0.16%, 500 or more: 0.2%). (Not ×(1 + Luck/1000) uncapped, not ×(1 + Luck/200) up to 0.3%.)
- M4 **The Cull's hidden 2%:** only for the Cull's target enemy met inside the Cull circle, where its kills count. (Not
  anywhere while the Cull is active.)
- M5 **Highlight:** on the encounter screen "Sparkling" before its name in gold with a shimmer; in battle it is named
  "Sparkling X", with a placeholder sparkle around its sprite and on its card. (Not the encounter screen only, no map
  status line.)
- M6 **Strength:** all eight attributes ×1.4 (HP already rises with Vitality), then its maximum HP doubled on top. (Not
  HP exactly twice a normal one's, not Vitality left out.)
- M7 **Rewards:** "1000% more EXP" is 11 times the EXP, on top of Empowered and other bonuses; silver as a normal enemy.
  (Not ×10, not ×11 silver.)
- M8 **Drop rolls:** two separate rolls, 70% and 5%; each that hits drops one masterwork (one 68%, two 3.5%, none
  28.5%). (Not the 5% only after the 70%, not exactly 5% for two.)
- M9 **The Cull's 2% and Luck:** Luck raises it like the 0.1%: 2% × (1 + Luck/500), at most 4% (Luck 100: 2.4%). (Not
  a flat 2%.)
- M10 **The drop pool:** every masterwork weapon, armor and artifact (35, counting Lament, Noble Hero and the new armor),
  boss-only and bought ones included; not the masterwork-rarity key items. (Not leaving out the boss-only ones, not
  adding the key items.)
- M11 **Treasure Seeker and Moneyshot's share:** each 5 times as likely as any other masterwork in the pool (about 11%
  of Sparkling masterwork drops each). (Not 3×, not 10×.)
- M12 **Luck on the drop rolls:** the game's masterwork Luck bonus (+0.05 × Luck percentage points) applies to both
  rolls (Luck 100: 75% and 10%). (Not fixed 70% and 5%.)
- M13 **Treasure Seeker's money:** battle silver, guild quest rewards and sales all doubled, but a sale never pays more
  than the item's shop price (buying and reselling can't make money); two worn in the party still double once. (Not
  battles and quests only, not uncapped sales.)
- M14 **Treasure Seeker's Luck:** +1 Luck per Platinum (10,000 silver) held, no cap. (Not at most +100, not +5 per
  Platinum up to +200.)
- M15 **Treasure Seeker on the level ramp:** like task 4's masterwork artifacts, the user's numbers at Level 80, weaker
  when it drops lower and stronger above. (Not fixed at every level.)
- M16 **Moneyshot's count:** every attack with it counts, hits and misses; the 5th, 10th, 15th… of each battle are
  boosted, and the count starts over each battle. (Not hits only, not kept between battles.)
- M17 **Treasure Seeker at Level 1:** uncommon strength: +15% money and +0.15 Luck per Platinum, rising on task 4's
  straight line to +100% and +1 at Level 80 (about +122% and +1.2 at Level 100). (Not half at Level 1, not the money
  alone on the ramp.)
- M18 **Moneyshot's boosted attack:** a boosted critical deals +200% instead of +50% (on top of the normal critical
  damage); the echo is 5% of what the hit dealt, and each other enemy's defenses reduce it (like Kugelblitz's cleave).
  (Not an echo that ignores defenses.)
- M19 **Lament's choice:** in the spell list each single-target spell gets a "Lament: +1 target (+50% Mana)" choice;
  then you click the second target. (Not picked automatically, not always on.)
- M20 **Lament's spells:** any spell aimed at one combatant: damage and curses on enemies, heals and buffs on allies.
  (Not damage spells only.)
- M21 **Noble Hero's effect (reworded at the user's request):** "Deals 25% less damage, but each hit strikes the target
  again for the same amount. The second strike counts as a weapon attack." The second strike makes no new hit or
  critical roll and triggers what a weapon hit triggers (on-hit effects, lifesteal, burns, the target's thorns). (Not
  two attacks at 75% with their own rolls.)
- M22 **Noble Hero's drop:** 1% from every demon and devil enemy except the two bosses (Great Imp Ji'ruan, Archdevil
  Saffron); Saffron's Great Devil Servants can drop it. (Not the bosses too.)
- M23 **The new masterwork armor:** I draft it (type, maker, effect, story, source) in the chat for the user's review and
  build it after approval. (Not built at my discretion unseen.)
- M24 **The Shadow Crystal:** 0.05% from every enemy that gives loot, bosses included; not summons or objects. (Not
  regular enemies only.)
- M25 **Delivery:** all three parts (Sparkling with Treasure Seeker and Moneyshot; Lament and Noble Hero; the new armor
  and the Shadow Crystal) in one pull request at the end; the only stop on the way is the new armor's draft (M23).
  (Not a stop after each part, not a pull request per part.)
- M26 **The new armor, the Hexbane Coat** (the draft reviewed 2026-11-06; the user changed only its source, "It only
  drops from one enemy? That seems a bit too rare"): a masterwork coat (clothing), Boreal Bolt its main maker; spell damage
  you take −10%, and when an enemy's spell damages you, 25% of that damage strikes the caster back in the spell's element,
  its defenses applying. Story: "A Boreal Bolt hunter's long coat, lined with silvered thread. Witches who curse its
  wearer feel their own spells come home." It drops at 1% from every non-boss undead, demon and devil (not objects),
  forged at your level, and is in the Sparkling pool; placeholder look: the normal forged coat in silver-grey. (Not 5%
  from the Abyss Witch only, as drafted; not 3% from undead and demon casters of Level 45 and up, not 2% from every
  caster from Level 40, not sold in Ravenwatch.)

**Decisions for task 9, Potion Rework (2026-11-07, a cloud session; the user asked for task 9 while task 8's pull request,
#14, waited to be merged; the task is `Docs/Port-Tasks/9_Potion_Rework.txt`).**
- P1 **Where:** "Since i cant merge right now, you can start the next task and put it in the same push request": built
  in the old game like tasks 1–8, on the same branch, in the same pull request as task 8 (#14).
- P2 **Art:** placeholder like tasks 1–8: the two Apothecary banners, the cauldron and its brewing animation, and the
  potions', poisons' and ingredients' icons are simple versions from the existing drawing kits, marked PLACEHOLDER ART
  and listed for the port's remake. Every potion, poison and ingredient is real and complete. (Not finished art now.)
- P3 **The ten Health and Mana Potions at the Apothecary** (Lesser, plain, Greater, Superior, Ultimate of each): always on
  sale in unlimited quantity, outside the random stock: the pair of your level band and every weaker pair (the General
  Store's bands today: up to Level 15 common, 30 uncommon, 60 rare, 90 epic, then legendary). (Not all ten always, the
  recommendation; not your band's pair only.)
- P4 **Recipes known from the start:** the Potion of Lesser Health's and Lesser Mana's, from easy-to-find ingredients; the
  other eight Health and Mana recipes are bought like the new potions'. (Not the four common and uncommon ones, not all ten.)
- P5 **The content** (40 potions, 12 poisons, 50+ ingredients with their sources and drop tables, every recipe): drafted
  as a full list and shown for review before anything is built. (Not built directly.)
- P6 **Potions outside battle:** Health, Mana and cure potions can be used anytime from the inventory, on yourself or a
  companion (a cure only when there is something to cure); buff potions only in battle, where their durations count the
  drinker's turns. (Not buffs drunk on the map carried into the next battle; not battle only, as today.)
- P7 **Stacking:** each potion's buff is held once; the same potion again only refreshes its duration; different potions
  add up, even on the same stat (the game's timed effects today). (Not "the strongest wins", not one potion buff at a time.)
- P8 **Poisons and bosses:** every poison works on the 12 bosses, at half strength: its damage, reductions and durations
  halved. (Not "no stuns or AP loss on bosses", the recommendation; not a 50% resist chance; not fully.)
- P9 **Who a buff potion reaches:** any ally, like Health and Mana Potions today: the user drinks it or hands it to a
  companion or summon, 1 AP. (Not the drinker only.)
- P10 **What a forage costs:** one hour of game time, with the zone's encounter chance rolled as for an hour of travel
  there. (Not 30 minutes without encounters, not 2 hours with a full roll.)
- P11 **Where Forage works:** every wild zone of every region, rivers and lake shores included; not in towns, on roads, at
  sites, or in the three uncharted regions (empty until the port, W27). (Not also on roads and near towns.)
- P12 **Ingredient drops:** each enemy has its own fitting ingredients, about a 25% chance a kill drops one (common ones
  more often, rare ones less); bosses and Empowered enemies drop more and rarer ones. (Not 1 in 2, not 1 in 10.)
- P13 **The Apothecary's ingredient menu:** every common ingredient always, plus a random selection of the region's own
  uncommon and rarer ingredients, a few of each, re-rolled with the other shops' stock every 6 game hours. (Not a random
  selection from all, not every ingredient everywhere.)
- P14 **A recipe's price:** ten times its potion's (Lesser Health's 4,500 silver). (Not 5×, not 20×.)
- P15 **When an Apothecary's three recipes change:** every 6 game hours, with the rest of the shops' stock; each
  Apothecary has its own three, the same however often the menu is opened. (Not once a game day, the recommendation; not
  every 3 game days.)
- P16 **Which recipes are offered:** any recipe not yet known, weighted toward your level band's rarity as the General
  Store weights its goods. (Not only up to your band's rarity, not evenly.)
- P17 **Poisons are brewed too:** each has a three-ingredient recipe bought like the potions' (62 recipes: the ten Health
  and Mana, the 40 new potions, the 12 poisons); poisons are also sold ready-made. (Not bought only.)
- P18 **The Alchemist's way in:** character Level 20 and 10 potions brewed. (Not Level 10 and 25 ingredients foraged, the
  recommendation; not open from Level 1.)
- P19 **The Assimilator's way in:** character Level 10 and 25 ingredients found by foraging. (Not Level 15 and 20 potions
  drunk, the recommendation; not Level 15 and three Alchemist levels.)
- P20 **A raw ingredient's strength:** at the first Assimilator level its combined buff is about a third of its potions'
  effects, rising to about two-thirds at the last; rarer ingredients last longer; potions stay the stronger choice. (Not
  half rising to full, not a quarter rising to half.)
- P21 **Ingredient buffs stack like potions:** the same ingredient refreshes its buff, different ingredients add up.
  (Not one at a time, the recommendation; not more slots as the class levels.)
- P22 **What a Collect quest pays:** by its ingredient's rarity: common like Travel (×1.0), uncommon like Escort (×1.1), rare
  like Search (×1.2), epic or legendary like Deliver (×1.35); the quantity asked is set so each rarity takes similar effort;
  never as much as Cull or Hunt. (Not one rate of ×1.15, not always like Deliver.)
- P23 **Where a Collect quest is handed in:** the guild that offered it. (Not any guild.)
- P24 **Which copies count:** any copies in the pack when handed in at the counter: foraged, dropped or bought. (Not only
  those found after taking the quest.)
- P25 **Which ingredient a Collect quest asks for:** one found in the guild's own region or a neighbouring one, of a
  rarity fitting your level band. (Not any from a region already entered.)
- P26 **Keys:** the Apothecary button O (among every settlement's services, beside the General Store), Forage D (with
  Camp and Rest). (Not M and S, not O and S.)
- P27 **Ingredients in the inventory:** their own Ingredients tab; potions and poisons stay in Consumables. (Not with the
  consumables.)
- P28 **Selling back:** the Apothecary buys potions, poisons and ingredients at half price; the General Store keeps buying
  its other goods but no longer potions. (Not both, not nobody.)
- P29 **When an Assimilator eats:** in battle, 1 AP, from the battle's item menu like a potion. (Not also on the map.)
- P30 **The two classes in the usual shape** (the user's review of the content draft: "My only complaint are the classes.
  Compared to other classes, they seem very different. Bring them more in line"): one gain per level, small fixed
  bonuses on most levels, a skill as the capstone. The Assimilator (the user's own answer): "Give the Assimilator 7
  levels. Its captstone will be a skill, since at Level 1 it should gain the Passive that allows ingrediant-eating":
  Raw Assimilation at Level 1 (a third of its potions' strength), half at Level 4, two-thirds at Level 6 (P20's
  "two-thirds at the last level" now at Level 6, before the capstone), the skill Devour at Level 7. The Alchemist likewise
  gains its identity, Ingredient Lore, at Level 1, in 5 levels with the skill Keen Harvest as its capstone.
  (Not a skill at Level 5 with eating capped at half; not two-thirds for all eating at Level 5 without a skill.)
- P31 **The content approved** (2026-11-07, "Now i am satisfied"): `Docs/Port-Tasks/9_Potion_Rework/draft.md` as it
  stands after P30 (made by `draft.py` there): the 40 potions, 12 poisons, 56 ingredients, 62 recipes, forage and drop
  rules, the Apothecary, the two classes and Collect quests.

**Decisions for task 8, Map Rework (2026-11-06, a cloud session; the user asked for task 8 after task 7 was merged, while
M11's engine work goes on on the PC; the task is `Docs/Port-Tasks/8_Map_Rework.txt`).** Asked before any code:
- W1 **Where:** in the old game, like tasks 1–7: the continent, its rules and every system built and playable there; its
  2D looks (terrain, fog, clouds, the boat and caravan animations) are placeholder art the port remakes in its 3D map.
  (Not in the port only, not split between the two.)
- W2 **The layout:** drafted first (a layout map and a table: regions, levels, roads, barriers, proposed new areas) and
  shown for review before it is built. (Not built directly.)
- W3 **"About 40% larger":** distances: travel between places takes about 40% longer; the land area roughly doubles.
  (Not the area +40%.)
- W4 **Encounters on the longer roads:** the same number per trip as today: their spacing stretches with the world.
  (Not the same per distance.)
- W5 **The continent's climate** (asked after the first layout draft, whose "southern-hemisphere" reading didn't hold: a
  tropical Jungle in the temperate north and a cold fog forest at the hot southern tip): a cold north and a hot south.
  The Verdigris Jungle moves south of Gloamfen, onto the wet west coast beside the deserts behind a rain-shadow ridge,
  its one road still only to Gloamfen (leaving it southward now); Duskpine becomes a cold highland plateau that the
  Ancient Battlefield climbs to from the Bloodstone coast. The road network and level order are unchanged. (Not the
  first draft kept as it was.)
- W6 **The layout:** draft 2 approved as drawn (2026-11-06): the regions whole at 1.4× on their roads' compass (the
  Verdigris Jungle turned round and placed south of Gloamfen, whose road to it now leaves southward from Reedcross),
  joined by border strips about 22 km wide; the Western Stoneveil spurs, the Rainwall, the Verdigris Range, the Glass
  Escarpment and the Stoneveil's cliffs seal the neighbours no road joins; the Western and Eastern Gulfs; no new areas.
  The picture and table are made by `claude-work/continent/draft.py` in the cloud session.
- W7 **The caravan's fare:** 2 silver per km of road between the two castle cities (neighbours, 120–230 km apart, cost
  240–460 silver; Alderkeep–Lakespire 870; Alderkeep–Ravenwatch about 2,450; the longest, Westport–Ravenwatch, about
  3,070). (Not 1 or 3 silver per km.)
- W8 **The caravan's time:** three times walking pace on roads, 21 km/h of road (neighbours 6–11 hours,
  Alderkeep–Ravenwatch about 2.4 days). (Not twice walking pace, the recommendation, nor walking pace.)
- W9 **The horse carriage's price:** 50,000 silver (5 platinum) at the General Store, bought once. (Not 25,000 or
  100,000.)
- W10 **Where the carriage can't go:** every road but the steep mountain roads of High Stoneveil and the Summit; off-road
  only open land (plains, hills, downs, steppe, flats, beaches, fields), never forest, jungle, marsh or bog, dunes, rock or
  cliffs, lava or rivers; it stays on the mainland when you sail to Cinder Isle. (Not roads only, not nearly anywhere.)
- W11 **The boat's time:** 12.6 hours (today's 9 ×1.4, like every trip, W3). (Not today's 9 hours.)
- W12 **Regions never entered:** under fog until first entered: the coastline and the regions you have entered show; an
  unentered region shows no towns, roads or names. (Not the whole continent visible from the start.)
- W13 **How far a click takes you:** to any town or site you know, walking along the roads across regions without
  stopping (encounters on the way as usual); the numbered destinations list the current region's places and its roads
  out. (Not the current region only.)
- W14 **Lengths:** the boat animation about 5 seconds; a caravan trip's 4 seconds for neighbours up to 8 for the longest;
  the music crossfades over 4 seconds at a region border. (Not all about 3 seconds, not all about 10.)
- W15 **Saves from before task 8:** everything open: the whole continent counts as entered (no fog) and every castle
  city's caravan as unlocked. (Not only where the player stands, the recommendation; not guessed from their level.)
- W16 **Where the caravan is offered:** a Caravan button among a castle city's service buttons under the map (beside the
  General Store and Travel by boat); it opens the list of the player's other unlocked castle cities with each fare and
  time, and a preview of the route on the map. (Not a tab of the General Store.)
- W17 **A natural continent** (asked after the user saw the first renders, 2026-11-06: "the continent seems very blocky
  … i want a natural looking continent"): one irregular, noise-shaped coastline (capes, bays) and irregular mountain
  ranges between the neighbours no road joins, in place of the regions' rectangles; the walkable land follows that
  outline both ways: in places it reaches past a region's old edge (wild ground with that edge's terrain, danger and
  encounters), elsewhere the sea or the mountains bite in. Roads, settlements, sites and the ground near them are never
  cut. (Not "bite in only", the recommendation; not the looks only with the old rectangles walkable.)
- W18 **How far the natural edge moves:** up to about 30 km (400 continent units) from a region's old straight edge, in
  or out. (Not about 15 km, the recommendation, nor about 7 km.)
- W19 **The arms and bays** (asked after the user reviewed the natural shape, 2026-11-06: "the deep desert and the
  Duskpine Forest with the Ancient Battlefield stick out way too much … you can fill out some ocean area with land to
  connect the old areas better … Or you could restructure the continent better"): restructure first (Duskpine and the
  Battlefield fold up into the bay below the Jungle, the Deep Desert tucks in; roads re-routed, every connection kept),
  and a new, fully built region (its own terrain, enemies, level band, connections) wherever a big gap still remains;
  drawn as a layout draft for review before it is built. (Not new regions with today's layout, not restructuring only.)
- W20 **Impassable rock** (the user's own answer): "Dont force-seperate regions. Just place one rocky terrain as a new
  passable region and thats it": no impassable rock parts any two regions any more (this replaces W17's mountain core
  between neighbours no road joins); the continent's only rocky terrain is one new region, which can be walked. (Not
  thin ranges only where needed, the recommendation; not water between them; not today's ranges without their bites.)
- W21 **Cinder Isle:** about 250 km offshore; the boat keeps its 12.6 hours (W11). (Not about 150 km, the
  recommendation, nor about 100 km.)
- W22 **Draft 3's review** (2026-11-07; draft 3 placed Duskpine north of the Battlefield, the Deep Desert up beside
  Saffron, three new regions: rocky highlands south of the Alder Vale, an east-coast steppe, a west-coast delta): not
  approved as drawn. The user's changes: the Stoneveil Summit must not look cut off from the High Stoneveil (one
  mountain); Cinder Isle further south, about level with the Verdant Reach; the Sunward Coast joined to the mainland; the
  water north-west of Mirrormere (it read as a second, bigger lake beside the basin's own) filled with land to the Marsh
  by enlarging Gloamfen, the Jungle and Stormwake; the Crimson Divide joined to the Saffron Expanse by land by enlarging
  the Expanse. So: regions grow into the ground between them (no sea or rock between neighbours; capes and bays only on
  the outer coast). Drawn as draft 4.
- W23 **Keeping low levels out where regions touch:** no barrier and no prompt; "the current recommended level should be
  displayed in the map view, that is warning enough": the map's header names the region you stand in and its levels
  ("STORMWAKE HILLS · LEVEL 15–35") and changes as you cross. (Not a warning the first time you step into a region 10+
  levels above you, the recommendation; not a warning every time.)
- W24 **Draft 4's review** (2026-11-07; draft 4: draft 3 with W22's changes, the rocky highlands moved east of the Alder
  Vale to join the Sunward Coast to the mainland, the steppe south of the Alder Vale, the Deep Desert a little further
  up, Cinder Isle level with the Verdant Reach): too smooth; the coasts of draft 3 were better. Kept from draft 3: an
  inland water between the Alder Vale and the highlands, one between Duskpine and the Crimson Divide, and the Deep
  Desert's little island. Drawn as draft 5.
- W25 **Draft 5's review** (2026-11-07): the Deep Desert's island must look as it did in draft 3, and the water between
  Duskpine and the Crimson Divide be as large as it was there. Drawn as draft 6 (draft 3's coast taken over at both
  places).
- W26 **The layout:** draft 6 approved (2026-11-07, after its legend was made smaller and moved to the bottom right):
  `Docs/Port-Tasks/8_Map_Rework/` (the picture, the scripts that make it, every region's place in continent units).
- W27 **The three new regions** (the rocky highlands, the steppe, the west-coast delta): "they will be empty for now,
  because everything for these regions will be done during the port": in the old game they are only ground on the
  continent; their names, identity, terrain, enemies, level bands, towns and connections are made in the port. The user
  asked to document this and to keep draft 6 as the reference for the port's visual redesign of the map (kept in
  `Docs/Port-Tasks/8_Map_Rework/`, pointed to from `Docs/Port-Tasks/README.md`).
- W28 **Walking the empty regions in the old game:** their ground can be walked like any other, with no encounters,
  towns or quests; the map's header reads "Uncharted land" with no level range; the music of the region you came from
  keeps playing; they are fogged until first entered like every region (W12). (Not closed until the port; not the
  nearest old region's wild ground with its enemies.)
- W29 **How the empty regions look on the old game's map:** blank parchment marked "Uncharted", so they read as
  unfinished. (Not plain placeholder ground in fitting colours, the recommendation.)
- W30 **Long walks across open land** (asked when W20 let the planner cut across country between regions: Alderkeep to
  Lakespire became a straight 284 km through the empty steppe instead of 432 km of road): the fastest way, but off the
  roads only through regions already entered; through any other region a click follows the roads (leaving them only in
  the regions where the trip starts and ends). Refines W13. (Not always along the roads, the recommendation; not the
  fastest way across any land.)
- W31 **The boat trip** (about 5 seconds, W14): on the map itself: the camera pulls out to show both coasts, a small boat
  leaves Seabridge and sails across the real sea to Emberhaven while the waves drift and the clock runs on 12.6 hours,
  then the camera settles on the island; Esc or a click skips to the end. (Not a separate scene card.)
- W32 **The mountain's height on the map:** the height rises smoothly as you go north through the Foothills, the High
  Stoneveil and the Summit; from the High Stoneveil on, everything lower than you (the Foothills, the Alder Vale and
  beyond) fades under a soft fog that thickens as you climb; on the Summit slow-drifting cloud banks lie below and around
  you; a wide clear circle round you, and markers and towns always stay readable. (Not a ring of fog only near you; not
  just dimming the lowlands.)
- W33 **The carriage's ground where W10 left it open:** open sand that is not dunes (the Deep Desert's Outer Wastes, the
  Crimson Divide's Open Desert, the Bloodstone Coast's Sanguine Wastes and Blood Coast) and the uncharted land may be
  crossed; the Mountainfoot, Stormcrest, Floodplain and Desert Fringe, and all of the Ancient Battlefield off its roads,
  may not. (Not hills and edges, the recommendation; not the Battlefield's open ground.)
- W34 **Planning with the carriage on:** a click plans a way the carriage can drive (roads and its open land) and says
  so if there is none; the carriage switches itself off only when you steer it into rough ground yourself. (Not
  planning as on foot and switching off on the way.)
- W35 **Where the carriage is after a journey:** it comes along with the caravan; by boat it stays at Seabridge (W10)
  and is there again when you sail back. (Not left where you boarded the caravan.)

**Decisions for task 7, New Game Plus: Limitless, Exotic and Exium (2026-11-06, a cloud session; the user asked for task 7
after task 5 was merged, while M11's engine work goes on on the PC; the task is `Docs/Port-Tasks/7_New_Game_Plus.txt`).**
Asked before any code, four at a time:
- L1 **Where and art:** in the old game like tasks 1–6, with placeholder looks from the existing kits (Winged Hussar with a
  simple winged crossguard, as the task asks); the green and purple wave is real CSS; all listed for the port's remake.
  (Not finished art, not only noted for the port.)
- L2 **Turning Limitless off** (section 3.1 says both "set back to 100" and "do not reset to 100"): while it's off you are
  Level 100 with your Level-100 stats; your Limitless level, the stat points gained above 100 and your EXP toward the
  next level are kept aside and all come back when it's turned on again. (Not keeping the level and stats, frozen.)
- L3 **Enemy scaling:** per level of difference from yours, +1 to each of the enemy's attributes; the 5 free points a
  hero gets per level are not applied. (The user's own answer; not hero-like growth with the points, not proportional,
  not only upward.)
- L4 **Companions:** like you: up to Level 200 while Limitless is on, back to their Level-100 values while it's off.
- L5 **Scaling direction:** only up: enemies below your level rise to it (+1 per attribute per level); enemies above it
  keep their own level and stats. (Not both ways.)
- L6 **Bosses' Level-100 classes:** I draft, for each boss whose class levels add up to less than 100, the classes that
  fit its role and mechanics up to 100 levels, show the list, and build after the user's approval. (Not an automatic
  rule.)
- L7 **The +25%:** the eight attributes ×1.25, once at battle start like Empowered and Sparkling; HP, Armor, Evasion and
  damage follow from them. (Not Armor and Evasion ×1.25 on top.)
- L8 **Masterwork drops ×2:** every masterwork in an enemy's drop table ×2 before the Luck bonus, and Sparkling's two rolls
  too (70% → 100%, 5% → 10%). (Not drop tables only; not the Luck bonus too.)
- L9 **Gear above Level 100:** past Level 100 forged weapons and armor keep rising at the average pace of Levels 1–100
  (a lance, 12 power at Level 1 and 140 at 100, reaches 204 at 150 and 268 at 200; their scaling ratios likewise). (Not
  half or a quarter of that pace.)
- L10 **That gear while Limitless is off:** turning Limitless off takes anything above Level 100 off (into the pack), and
  it can't be worn until Limitless is on again. (Not left on, greyed and inactive.)
- L11 **Exium:** Mythilium's material values +3 each: +8% final power, +8 Accuracy, +8 Crit Rating and +8% armor pierce
  per part (16 on the main part). (Not ×1.03.)
- L12 **Exotic level:** forged at your level when they drop (up to 200), like masterwork drops. (Not always Level 200.)
- L13 **Exotics while Limitless is off:** taken off like other gear above Level 100 (L10); one at Level 100 or below stays
  on (L25: active, not greyed). (Not every Exotic kept on and greyed.)
- L14 **Winged Hussar's damage:** +1% damage per 4 Evasion (with its own +15% Evasion counted), a configurable number.
  (Not 25% or 40% of Evasion added to base power.)
- L15 **Anatemis's Level-200 reward:** from all five Exotics (section 8), armor and shield included. (Not weapons only,
  as section 10 says.)
- L16 **Item types:** Tough-Out is a Hearthforge tower shield, Connected Balancer a Boreal Bolt brigandine. (Not a kite
  shield and scale armor, not a buckler and leather armor.)
- L17 **Old saves:** every old save starts locked, also one that beat Anatemis before; beating her again (a rematch)
  unlocks it. (Not unlocked on load.)
- L18 **EXP at Level 100:** none is gained at Level 100 without Limitless on (also while it's still locked); EXP an old
  save banked at Level 100 is cleared when it loads. (Not keeping the old pile.)
- L19 **Connected Balancer's allies:** every living ally on the wearer's side, summons included. (Not heroes only.)
- L20 **In battle:** Limitless cannot be turned off during a battle (the user's own answer), nor on (L21): the button is
  disabled in battle, so every battle runs with one Limitless state throughout.
- L21 **Turning it on in battle:** no, only between battles. (Not from the next battle, not at once.)
- L22 **Delivery:** one pull request at the end; the only stop on the way is the bosses' class draft (L6). (Not two pull
  requests.)
Asked while building the Exotics (2026-11-06):
- L23 **Cherubim's +12% per rune:** +12% final power and +12 points of armor pierce per attached rune (four runes: +48
  and +48), part of the weapon's own totals like any rune's. (Not ×1.12 of the current values.)
- L24 **Elemental runes on Cherubim:** Holy is its base element; an elemental rune mixes in its own element at 50%, as on
  every other weapon. (Not Holy only.)
- L25 **Exotics at Level 100 or below while Limitless is off** (the user's own answer; replaces L13's "greyed and
  inactive"): they stay active, worn or put on, like any other gear; the rule that a piece can't be worn while Limitless
  is off applies only to pieces of Level 101 and above.
- L26 **Fletcher's armor break** (the user's own answer): it stacks: every hit adds its own −20% Armor (−40% on a
  critical hit) for 1 turn. (Not one renewed effect per wielder, not the latest replacing it.)
- L27 **Gear above Level 100 while Limitless is off** (the user's own answer): any such piece, Exotic or not, has a red
  border and the note "Level N required" (N its level) in the pack and item panels. (Not greyed.)
- L28 **Exotic weapons' other parts:** Exium a quarter of the time, otherwise any fitting material; the main part is
  always Exium. (Not Mythilium as on masterworks, not all Exium.)
The bosses' class draft (L6), reviewed 2026-11-06:
- L29 **The draft:** approved as drafted: the eleven bosses' `BOSS_CLASSES` in `../old-game/scripts/limitless-engine.js`,
  each at exactly 100 class levels, none over its class's maximum, no hero, raid-boss or item-locked class.
- L30 **One strike a turn:** a boss's damaging skill (Final Blow, Worldshaker, Chain Overload, Pestilent Cloud, Psychic
  Lance, Rampage and any other) uses up its one strike that turn, like an attack or a spell. (Not on top of it.)
- L31 **Burst and gamble skills** (Arcane Chorus, Arcane Echo, Doubled Spell, Wild Thesis, Magnum Opus, All or Nothing,
  Champion's Challenge): bosses may use them; they are not added to the bosses' don't-use lists. (Not kept from them.)
- L32 **Guard-only levels** (Bulwark 2/4/6, General 2, Strategist 4) on bosses that never Guard: kept; they count toward
  the 100. (Not swapped for other classes.)
Asked after the regression run (2026-11-06):
- L33 **The victory screen's EXP at your level cap** (Level 100 without Limitless, 200 with it), where you gain none
  (L18): it reads 0 EXP; companions below their cap still gain the battle's EXP. (Not the number with a note, not left
  showing the battle's EXP.)

**Decisions for task 12, Spell Rework (2026-11-11, a cloud session; the user: "Go ahead with Task 12"; the task is
`Docs/Port-Tasks/12_Spell_Rework.txt`).** Asked before any work, after reading the old game's spells
(`objects/spells.json`: 388 spells in 12 elements, Basic among them; Tiers 1–10, 4–6 per element in Tiers 1–5, 3 in
Tiers 6–7, 2 in Tiers 8–9, 1 in Tier 10; power = base power + a primary and a secondary stat × their multipliers; a
Mana cost; 3 AP unless the spell says otherwise; 29 summon spells) and how they are cast (`calculateSpellValue`,
`calculateSpellManaCost`, `calculateSpellActionPoints` in `scripts/app.js`, `cast` in `scripts/combat.js`):
- SP1 **Where:** drafts for the port, as task 10: the spells are designed in draft files the user reviews and
  approves; the old game stays unchanged and the port builds them, so the old game's enemies and classes stay
  balanced until they are revamped together at the port. (Not built in the old game, not split with the classes in
  drafts.)
- SP2 **The review:** a sample of four reworked spells first, to settle the style; then one batch per element (about
  32 spells each); then the new spells, element by element; then the two classes. (Not by tier, not one full draft.)
- SP3 **Basic counts as an element** for the new spells: 12 elements, so 168 new spells (two per element in each of
  Tiers 1–5, 120; one per element in each of Tiers 6–9, 48). (Not the other 11 only.)
- SP4 **The boss and enemy-only spells are left out:** War Horn, Call the Storm, Thunderbolt of Talos, Stonecrust,
  Wheel of the Year, Vulcanic Charge and Summon Imp Retinue stay exactly as they are; the rework covers the spells
  players can learn. (Not reviewed with their mechanics kept, not reworked like the others.)
- SP5 **Not just an effect on every spell** (the user: "I dont mean small effects just to flesh spells out. The
  weapon/armor system is very detailed and complex, and i want spells to have a similar detailed and fitting unique
  system. Just adding effects to every spell wont bring them in line with weapons/armors"): spells get a system of
  their own, as detailed as forging is for weapons and armor; its design is asked next.
- SP6 **New mechanics are allowed**, each marked "(new)" in the draft so the port knows to build it, as task 10's
  features file does. (Not only mechanics the game already has, not only the spells' own effect types.)
- SP7 **AP cost** (the user: "Most spells need 3 AP with other specialized faster spells using 2 AP, but 1 AP, 4 AP and
  5 AP spells may be added (or reworked into) if a spells powerlevel needs it"): 3 AP is the norm, 2 AP for
  specialised faster spells, and 1, 4 or 5 AP where a spell's power calls for it.
- SP8 **Power level:** measured first (spells against weapon attacks and the enemies' HP at several levels), then a
  power and Mana curve per tier is proposed with the sample, for the user to approve. (Not today's curve kept, not
  spells made generally stronger.)
- SP9 **The spells' system is Spell Augmentation** (the user, answering SP5's question, which offered scribed spells
  made like forged gear, spell mastery by casting, glyph slots, or scribed spells with mastery: "Something like the
  mastery system, but its called Spell Augmentation. Add a new menu to each Magic Tower that allows selecting known
  spells to further boost certain attributes of them (Enhanced power, resistance penetration and effect duration
  increase may be some examples, but further can be added). That costs money, but augmenting in the Institute of Magic
  or the Church of Talos has a discount. Also change the Spellweaver class to be able to augment anywhere at reduced
  cost and make that class generally focus on this augmentation. If any further design or system questions appear,
  ask me about it"): a new Augmentation menu in every Magic Tower; you pick a spell you know and pay to boost its
  attributes (enhanced power, resistance penetration, longer effects and others); the Institute of Magic and the
  Church of Talos augment for less; the Spellweaver augments anywhere, for less, and the class is built around
  augmentation. Its details are asked one by one (SP10 on).
- SP10 **Augmentation's limits:** each attribute is raised in ranks, up to 5, but a spell holds at most 10 ranks in
  all, so each spell gets a build of its own; a rank costs more than the one before it and more for a higher tier.
  (Not 5 ranks of everything with only money as the limit, not a total that grows with the caster's level.)
- SP11 **Element, scaling and tier changes are the Spellweaver's alone:** everyone can buy the plain boosts (power,
  resistance penetration, effect duration, Mana cost and the others); changing a spell's element, its scaling stats or
  raising its tier only a Spellweaver can do. (Not open to everyone, not element and scaling for everyone with only the
  tier the Spellweaver's.)
- SP12 **The discounts:** the Institute of Magic takes 25% off every spell's augmentation; the Church of Talos takes 40%
  off, on Lightning spells only. (Not 25% at both with the Church on Lightning only, not 25% on every spell at both.)
- SP13 **Undoing it:** at any Magic Tower a spell's augmentation can be reset, for half of what was paid back;
  unlearning a spell resets it the same way. Augmentation belongs to each hero (the player and every companion). (Not
  permanent, not a free reset.)
- SP14 **One common list of attributes:** every spell uses the same list, showing only the attributes that apply to
  it (no effect duration on a spell without a lasting effect, summon attributes only on summons). (Not the common list
  plus a signature augmentation per spell, not a list of its own for each spell.)
- SP15 **A system draft comes first:** before the sample of four spells (SP2), a short draft of the system for the
  user's approval: the Augmentation menu, the attributes and what a rank does, the costs, and the measured power and
  Mana curve per tier (SP8). Then the sample using it, then the elements. (Not shown together with the sample.)
- SP16 **Enemies' spells** may carry augmentations, chosen per enemy in the port's enemy revamp (the general port
  task), stronger and later enemies more. (Not bosses only, not never.)
Asked after the measurement (SP8; a hero of the old game without classes or gear, levels 1–100, against the 174 enemy
definitions; the script and its data are in `~/.cache/stat-fick-build/claude-work/t12/`): a pure caster's Mana covers
2–3 casts of the best spell they can learn and heroes regain none in battle; all-enemies spells deal 90–97% of a
same-tier single-target spell's power for about the same Mana; at Level 100 a Tier 1 spell gives about 11 power per
Mana and a Tier 9 spell about 2 (3.6× as strong per cast); a spell's damage per turn is about a fighter's two weapon
attacks against same-level enemies (spells ignore armor and Evasion, so they pull ahead against armored ones).
- SP17 **Mana stays as scarce as now:** no regeneration in battle and no flatter Mana curve; gear, classes, potions and
  Mana-cost augmentation are what stretch it. (Not a flatter curve, not regeneration, not both milder.)
- SP18 **All-enemies spells deal about 75%** of a same-tier single-target spell's power for the same Mana (the user's
  own number; offered: about 60%, the same power for about 50% more Mana, or kept as now).
- SP19 **A narrower gap between tiers:** higher tiers stay stronger per cast and less efficient per Mana, but the gap
  shrinks, to about 2.5–3× power per Mana from Tier 1 to Tier 9 instead of about 5×; low tiers stay the cheap filler.
  (Not equal per Mana, not today's gap.)
- SP20 **Spells never critically hit:** critical chance stays a weapon attribute and is not in the augmentation list.
  (Not spell crits.)
- SP21 **Where the drafts live:** `Docs/Port-Tasks/12_Spell_Rework/` from the start, since task 12 is for the port only
  and nothing moves later. (Not `../old-game/Docs/` during the review, as task 10's were.)
- SP22 **The gap closes by raising the high tiers:** with Mana kept (SP17), Tiers 1–3 keep about today's power, so the
  early game is unchanged, and higher tiers gain power step by step for the same Mana: for a raw caster at the level
  each tier unlocks, Tier 5 about 235 (174 now), Tier 7 about 500 (281), Tier 9 about 940 (430), Tier 10 about 1,600
  (740); all-enemies spells 75% of that (SP18). Tier 1 to Tier 9 power per Mana: 2.75× instead of 5.4×. (Not split
  with dearer Tiers 1–3, not the low tiers lowered.)
- SP23 **The Augmentation menu's look is asked later** (the user, while the system draft was being written: "When it
  comes to how the augmentation menu should look, ask me later. Prepare everything else first"): the system draft says
  only what the menu does; its look is asked once everything else is prepared.

**Decisions for task 11, New Races (2026-11-11, a cloud session; the user: "You can now start Task 11"; the task is
`Docs/Port-Tasks/11_New_races.txt`).** Asked before any work, after reading the old game's races (`objects/races.json`:
stat modifiers adding up to about 0, one passive, a seal icon; four male and four female looks each in
`scripts/sprite-catalog.js`; recruit names in `scripts/companions.js`; an accent colour) and its racial classes:
- T1 **Where:** the races, their icons and looks go into the old game now; the three racial classes are added to task
  10's drafts for the port (`Docs/Port-Tasks/10_Class_Rework/`), since they follow task 10's reworked race classes,
  which exist only there. (Not all in drafts for the port, not all in the old game.)
- T2 **Half-Beast's looks:** one race whose looks vary by beast, one shared racial trait. (Not wolf-kin only, not a
  beast chosen at creation with its own trait.)
- T3 **Merfolk's look:** legs on land, with fin ears, scales and webbed hands, so armour and boots fit as everyone's.
  (Not the enemies' fishtail.)
- T4 **Goliath's size:** a head taller than humans and heavy-built, with giant-blooded marks (stone-grey skin,
  rune-like markings), using the same gear. (Not giant-size.)
- T5 **Art:** simple placeholders, the icons (seals) and the looks alike, to be remade in the port, as the guild
  interiors were. (Not in the six seals' finished style.)
- T6 **Review:** question by question, not through a draft file.
- T7 **Recruits:** the new races appear among the tavern recruits too, with their own names.
- T8 **Merfolk's passive:** kept (Fishman Physiology: 20% less Water spell damage, 10% more Fire), shared with the
  merfolk enemies. (Not a new trait for both.)
- T9 **Half-Beast's stats:** "balanced feral": Strength, Vitality, Dexterity, Wisdom and Speed +1; Mind −1,
  Intelligence −2, Luck −2. (Not the agile hunter, not the brute.)
- T10 **Half-Beast's passive: Natural Claws:** its unarmed attacks are claws that grow with its level. (Not Feral
  Instinct, not Keen Senses.)
- T11 **Goliath's stats:** "mighty and slow": Strength and Vitality +3, Luck +1; Mind, Dexterity and Speed −2,
  Intelligence −1. (Not the steadier-minded spread.)
- T12 **Goliath's passive: Giant's Blood:** maximum HP +10%, Armor +5%, Evasion −10%, a lighter Large Form. (Not
  Mighty Frame, not Stone Hide.)
- T13 **The claws count as unarmed:** 1 AP, every unarmed bonus applies (Monk, Brawler, Martial Artist), a little
  stronger than fists and growing with level. (Not a 2-AP natural weapon like the Beast class's, without unarmed
  bonuses.)
- T14 **Half-Beast's beasts:** wolf, fox, bear and lynx, two looks each. (Not wolf, bear and a griffon-feathered kin;
  not wolf, fox, lynx and boar.)
- T15 **Merfolk's stats:** kept as the enemies' (Strength +1, Vitality +3, Mind +1, Dexterity +2; Intelligence −2,
  Speed −1, Luck −2). (Not brought to 0.)
- T16 **Recruit names:** written by me in each race's style. (Not given by the user.)
- T17 **The racial classes** (for task 10's drafts; Level 30 and the race, 3 levels, no colour, a passive at level 1
  and a skill at level 3, like the five existing ones): Half-Beast **Wildheart** (Wild Blood: claws +10% damage, Speed
  +5%; Strength and Dexterity +5%; Unleash the Beast: 3 turns of +20% claw damage and +15% Speed, once per battle);
  Merfolk **Deepcaller** (Call of the Deep: Water spell power +15% and no more Fire weakness; Vitality and Mind +5%;
  Maelstrom: Water damage to every enemy and 15% less Speed for 2 turns, once per battle); Goliath **Peakwarden**
  (Unmoving: 10% less damage while Guarding, can't be slowed; Armor +10%, Vitality +5%; Mountain's Shelter: every other
  ally takes 15% less damage for 2 turns, once per battle). (Not Packblood, Tideborn or Giantkin.)

**Decisions for task 10, Class Rework (2026-11-10, a cloud session; the task is `Docs/Port-Tasks/10_Class_Rework.txt`).**
The user had the task read but not started ("Inform yourself of this task, don't start it yet"), then gave C1–C5 as
notes to keep; C6–C8 were asked the same day to settle them, and C9–C12 answer what was still open. Nothing is built
yet.
- C1 (replaced by C23: the port builds them, not the old game) **Where:** the classes are built in the old game, as
  tasks 1–9 were; the faithful port brings them over.
- C2 **Enemies with classes:** the current enemies that use the old classes (enemy files with class levels, the
  bosses' `BOSS_CLASSES` of task 7, L29) need a full rework, but that happens during the game port, not in this task
  ("Open questions for later milestones", "At port time").
- C3 **Every class is reviewed:** all 130 current classes and every new one are gone through and listed, even where
  nothing changes.
- C4 **`../old-game/classes/newc/`:** its files are ignored, except seven, which are added as new classes too:
  Chronicler, Shaman, Elder Shaman, Trickster, Awakened, Warden and Scout. They count toward the task's 15 classes of
  my own. Ignored: the drafts there for classes on the task's list (Acrobat, Blade Dancer, Bruiser, Hotshot,
  Infiltrator, Mastersmith, Smith; the empty Juggernaut, Tinkerer and Vanguard; Explorer and Spell Sparkler in
  `newc/IGNORE/`), which still come from the list, and Runesmith, Temperer and the template.
- C5 (replaced by C14: one draft) **The review is done in draft files, in two rounds, both with every class, the current ones and the new ones of
  this task:** first draft, every class with its identity and its number of levels; when that is done, a second draft,
  every class with its requirements and its rarity (colour). For the new classes this is how the task's four
  questions (identity, levels, then requirements, rarity) are asked.
- C6 **Classes of my own:** 8 more besides the seven of C4, so 15 in all, as the task says. (The user's note said 9;
  asked: 8.) The new classes are then 40 (41 before C9): the task's 25 other names, the seven of C4 and 8 of my own.
- C7 **The seven of C4 are names only:** everything about them is proposed fresh. Awakened's and Warden's filled-in
  drafts are not used. (Not their files' identity, levels, requirements and colour.)
- C8 **Where the drafts go:** `../old-game/Docs/`, beside the old game's notes, while under review (moved to
  `Docs/Port-Tasks/10_Class_Rework/` once approved, C23, C31). (Not `Docs/Port-Tasks/10_Class_Rework/`
  as task 9's draft was.)
Asked the same day about what reading the old game turned up:
- C9 **Poisoner** already exists (`classes/Poisoner.json`, from the earlier batch of 32 new classes that
  `newc-browser.js` checks): it is dropped from the task's list of new classes and reworked like every other current
  class.
- C10 **"Ascendend"** in the task's list is a misspelling: the class is **Ascended** (like Enlightened and Awakened; not
  Ascendant). The task's text is corrected to it, as task 7's names were (`Docs/Port-Tasks/README.md`).
- C11 **No class is removed** in this task, so every enemy keeps naming classes that exist until the port reworks them
  (C2; the old game refuses an enemy file naming a class it doesn't have, `validateEnemy` in
  `../old-game/scripts/combat.js`).
- C12 **Saved characters are ignored:** no one will play the pre-port version of the game, so nothing is done for saves
  with more levels in a class than its new maximum.
Said while draft 1 was written (the user's go for it, 2026-11-10):
- C13 **The draft's form:** each current class's identity is drawn from all its features, its passives and skills
  included, but the draft doesn't list the passives and skills.
- C14 **One draft, not two** (the user, after seeing draft 1: "With the draft being structured like this, I see no
  reason to do a second draft"): identity, levels, requirements and rarity of every class in one file, which the user
  then reviews properly. (Replaces C5's two rounds; draft 1 was folded into it.) It is
  `Class-Rework-Draft.md` (now in `Docs/Port-Tasks/10_Class_Rework/`, C31; made by `~/.cache/stat-fick-build/claude-work/class-rework/make_draft.py`,
  which checks that every class file appears once and that the new classes' required classes exist; the file is the
  source now, edit it directly). Proposed there, for the user to approve: Scavenger 7 → 5 levels (the only change of
  levels); eight identities sharpened where two classes do the same thing (Champion, Bulwark, Cleric, Paladin,
  Oathsworn, Templar, Assassin, Archivist); every current requirement kept (none needs changing); nine rarities raised
  (Avatar, Cardinal Priest, Great Mage, Chaos Cultist, Joker, Dragonkin and Vampire to blue; Madman and Sharmat to
  purple), as the task lets me ask; 40 new classes with identity, levels, requirements and rarity, among them my eight
  (Field Medic, Sentinel, Beast Tamer, Sunderer, Camp Cook, Leywalker, Nightwalker, Mentor). Rarity across the 170:
  116 without a colour, 38 blue, 13 purple, 3 gold.
- C15 **Identities: a short playstyle** (the user, the same day, first "a proper small explanation of what the class
  does", then, after seeing it: "I don't need a description that detailed ... a short description of this class's
  playstyle without mentioning its skills/passives and not mentioning specific stat gains"): one or two sentences on
  how the class plays; never its skills or passives (they can change), never specific stat gains. They live in
  `~/.cache/stat-fick-build/claude-work/class-rework/identities.py` beside `make_draft.py`; the draft is the source now.
The user's review of the draft (2026-11-10; their notes, "E-Notes", on 17 classes, everything else accepted as drafted):
- C16 **The notes, applied:** rarity raised: Death Paladin, Nullblade, Plaguebinder, Jester and Jack of all Trades to
  blue; Vampire Lord and Joker to purple; Avatar to purple (not blue as I proposed); Madman and Sharmat to gold (not
  purple). Element Siphon renamed **Elemental Siphon** (its id `element-siphon` stays, as Juila's did when her name was
  corrected). Poisoner remade to use the game's poisons (task 9). Bruiser's identity: hits harder the higher its
  maximum HP. Spell Sparkler blue, its requirement a little easier (3 spells in one turn 5 times, not 10). Juggernaut's
  identity also: its turns come sooner. Beast Tamer's requirement counts every Beast-class enemy (in case more are
  added). **Unreality: the only red class, a new rarity above gold**, needs Sharmat and Madman at their maximum level,
  rewrites the rules of reality, 3 levels. Rarity across the 170 now: 109 without a colour, 42 blue, 13 purple, 5 gold,
  1 red. Each note stands word for word under its class in the draft.
Asked about the notes the same day:
- C17 **Unreality's requirement:** Sharmat 3 and Madman 3 only, no level or other class of its own (Ur-Mat's gem and the
  0.05% Shadow Crystal make it late anyway). (Not also Level 60, not also Dreamweaver 5.)
- C18 **Bruiser's requirement:** Level 15, Warrior 5, Vitality is your highest stat (the old "win 10 battles in which you
  fell below half HP" fitted the old identity). (Not kept, not the most maximum HP in the party.)
- C19 **Beast Tamer tames animals only** (wolves, bears, griffons and animal kinds added later), though its requirement
  counts every Beast-class enemy: 49 enemies carry the Beast class, every dragon (Anatemis too), the treants and others
  among them. (Not every non-boss Beast-class enemy.)
The features round (2026-11-10):
- C20 **It starts with a sample of four** (the user: remake one random starting class, one random item-acquired class,
  one random elemental mage and one random class overall, with the old class as the reference and the capstone rule in
  mind). Drawn at random: Warrior, Vampire, Hierophant, Reaver. Proposed in `Class-Rework-Features.md` (now in `Docs/Port-Tasks/10_Class_Rework/`, C31)
  (old and remade level by level, with why); not built until approved. It also tries out rules for the rest, for the
  user to judge: no filler levels, once-a-day skills once per battle unless they are big bursts, no self-defeating
  penalties, power by rarity, and Hierophant's shape as the template for all twelve elemental mages.
- C21 **The sample is approved** ("Otherwise, this draft is good"), its rules with it, and one more the user forgot:
  **a class has exactly one passive and one skill.** Exceptions: the twelve elemental mages and the classes acquired
  by an item (Dragonkin, Lich, Madman, Sharmat, Vampire), and the Vampire Lord (C24). Today's classes with more
  (Druid, Ki Warrior, Paladin, Psykic, Sharpshooter, Summoner) or with none of one (Jack of all Trades, Trap Layer,
  Trap Master) are brought to one of each in their remakes.
- C22 **The rest, group by group:** a few of the draft's groups at a time, added to the features file for the user's
  review, the new classes last; the user's notes on one batch shape the next. (Not all current then all new, not all at
  once.) Batch 1 (the four starting classes but Warrior, the melee fighters but Reaver) approved as written
  (2026-11-10, "Approved. Continue with the next"). Batch 2 (the knights and defenders, the holy warriors, the unarmed
  fighters) approved the same day but the Ki Warrior (C26); the revised Ki Warrior taken as approved when the user went
  on without notes ("Go ahead"). Batch 3 (the archer's path, the rogue's path, weapons and spells together) approved the
  same day, but the Thief's Grand Theft still triggers on a critical hit, as before (the user's note; the draft had let
  any hit steal); its new share of the enemy's money stays. Batch 4 (the mage's path) approved the same day. Batch 5
  (the eleven other elemental mages on the Hierophant's template, the masters of elements) approved the same day, but
  the Stormcaller peaks at +100% spell power as before (the user's note): Storm Rhythm up to 25 charges of 4% (the
  draft had 10 of 3%). Batch 6 (priests and healers, summoners and dark pacts, leaders and minds) approved the same day,
  but the Summoner's Overprotective becomes a chance (35%) that a random summon of its intercepts a single-target attack
  aimed at it, "that way enemies can use it too" (the user's note; the draft had made enemies far less likely to attack
  it). Batch 7 (luck, the road, trade and the guild) approved the same day, but the Jester's 6-sided Luck rolls once a
  battle and pays only on a six (the user's note; the old game, and the draft, let it reroll for free until a six).
  Batch 8 (the race classes, the classes from rare items, the legends and the rest), the last of the current classes,
  approved the same day, but the Dragonkin's Dragon's Breath makes better use of the 23 Dragonbreath spells (the user's
  note): it makes the next one free, 40% stronger and its weakening a turn longer (the draft had a fire attack of its
  own). Every current class is through. Batch 9, the new classes part 1 (the first twelve of the task's list), written
  for review.
- C28 **Requirements that a turn's Action Points must allow** (the user asked whether Hotshot's and Spell Sparkler's
  requirements are achievable; checked in the code: a turn has 5 AP, a weapon attack costs 2 (unarmed 1), most spells 3
  and eight Tier 1–2 spells 2; only the Draught of Haste, +1 AP a turn for 3 turns at 9,000 silver, adds AP).
  Hotshot: "make 3 ranged attacks in one turn" needed 6 AP; it now requires hitting 2 different enemies with ranged
  attacks in one turn, 25 times (not defeating 3 with ranged attacks in a battle, not 2 ranged attacks in a turn).
  Spell Sparkler: three spells in a turn are possible only through Spellcaster 5's Quickened Spell (2 + 2 + 1 AP) or a
  Haste draught; Spellcaster 5 joins its requirement so the way is clear (not kept as it was, not 2 spells in a turn).
  The same check found Hotshot's Quick Draw too strong (every shot at 1 AP and 75%, five a turn); revised before review
  to one full shot and then 1-AP half shots. The other 38 new classes' requirements checked the same way: all within
  reach. Batch 9 approved after it (2026-11-10). Batch 10, the other thirteen of the task's list, written for review.
- C29 **The Mastersmith's level 1** (the user asked what "reforging gives you two results to choose from" means; it
  meant nothing: reforging isn't random, the player picks the second and third parts, up to the main part's rarity, and
  the main part, level, slots and runes stay; `reforgeOptions` in `../old-game/scripts/forge-engine.js`): when the
  Mastersmith reforges, the new parts can be one rarity above the main part's. (Not changing the main part too, not
  half-price reforging.) The Smith's Field Forge follows the rules of the anvil the item would need.
- C30 **The Seeker's level 4** (the user asked whether it was redundant; it was: the hit chance is Accuracy ÷ (Accuracy +
  Evasion), `weaponHitChance` in `../old-game/scripts/combat-engine.js`, so 15% less enemy Evasion is just more Accuracy
  in a class that already has it and can't-miss features, and a Search quest's fight starts at its marked spot with no
  search time, G20): the guild's Search quests give the Seeker 25% more money and Reputation. (Not ignoring evasion
  buffs, not more damage after a miss.) Batch 10 approved after it (2026-11-10). Batch 11, the last (the seven newc
  classes and my eight), approved as written (2026-11-11); with it every class has its approved features.
- C31 **The drafts moved, task 10's design done** (2026-11-11, after the last batch's approval, as C23 set): both drafts
  went from `../old-game/Docs/` to `Docs/Port-Tasks/10_Class_Rework/`, with a README there as task 9's folder has;
  `Docs/Port-Tasks/README.md` lists task 10 as designed for the port. The scripts that first made the draft
  (`~/.cache/stat-fick-build/claude-work/class-rework/`) are not kept: the two files are the source.
- C32 **What the port must do with them** (the user, 2026-11-11: "I want the port to note the following: Classes are
  unchanged in the old game. During the port, the classes in the draft are added. During porting, every class needs to
  be inspected for possible errors and inconsistencies. After the port, every class feature needs to be tested so it
  works as intended."): recorded under "Open questions for later milestones", "At port time", in
  `Docs/Port-Tasks/README.md` and in `Docs/Port-Tasks/10_Class_Rework/README.md`.
- C27 **The Psykic is an exception to the one-skill rule too**, like the Ki Warrior (asked before batch 4): its four
  skills stand in for the spells it doesn't know, with one passive. (Not folded into one skill.)
- C26 **The Ki Warrior is an exception to the one-skill rule** (the user: "I wanted it to have many skills to simulate a
  spell replacement. Overflowing Ki can stay, but add 2 more skills to that class that use Ki points"): Ki Focus, plus
  Ki Blast (2 Ki, one enemy) and Ki Wave (3 Ki, every enemy) at levels 5 and 8, its own attacks paid with Ki and scaling
  on Strength and Intelligence; Overflowing Ki stays its one passive. The automatic Ki-to-Mana of the first version
  gives level 5 to Ki Blast. Revised for the user's review.
- C23 **Not built in the old game** (the user: "The newly approved classes should not be implemented into the old game.
  They are added only on game port, so after I approved all classes, keep the drafts in a good location and note this
  down somewhere"): the old game's classes stay as they are; the port builds the reworked set from the approved
  drafts. Replaces C1. When every class is approved, `Class-Rework-Draft.md` and `Class-Rework-Features.md` move from
  `../old-game/Docs/` to `Docs/Port-Tasks/10_Class_Rework/`, beside the task, as task 9's approved draft sits in
  `Docs/Port-Tasks/9_Potion_Rework/` (and `Docs/Port-Tasks/README.md` lists task 10 as left for the port). (Not after
  each batch, not at the end in the old game.)
- C24 **The Vampire Lord is an exception** to the one-passive-one-skill rule, like the Vampire it grows from (it needs
  Vampire 4, not an item). (Not following the rule.)
- C25 **The enemy-only classes stay outside the rule:** Raid Boss (unobtainable), and Beast, Great Beast and Monster
  (for races players can't pick); the enemies' classes are reworked in the port anyway (C2). (Not given a passive and a
  skill each.)
Still to ask (nothing assumed):
- The red rarity's look (Unreality's colour in the class list and details), when the port builds the classes (C16, C23).
Kept in mind:
- (Moot since C23: the old game's class files don't change.) `classes/Black Knight.json` is read by the engine's tests
  (`LooseRecordTests`); if it changes, `Tools/Scripts/test.sh`
  must run. The class files are hand-formatted: edit them keeping their layout, never through `json.dump`
  (`../old-game/Docs/Notes.md`).
Facts found (2026-11-10): 130 class files (104 without a colour, 18 blue, 6 purple, 2 gold: Hero and Demon Lord; Raid
Boss unobtainable; Beast, Monster and Great Beast need a race players can't pick). Ten break the task's capstone rule:
Assassin, Barbarian, Cleric, Cultist, Jack of all Trades and Vampire have their passive or skill before the last level;
Beast, Monster, Trap Layer and Trap Master have neither. 18 have more than one (the twelve element mages two skills
each, Druid, Ki Warrior, Paladin, Psykic, Sharpshooter, Summoner). 174 enemy files carry class levels.

**Decisions for the move to GitHub (2026-10-30):**
- One private GitHub repo with two folders: `engine/` (the engine and port, formerly `Stat Fick Source/`) and `old-game/` (the JavaScript game, formerly `Stats/`).
- On this PC both move into one repo folder, `/run/media/system/M.2_2TB/Stat Fick Repo/`, which becomes the only working copy (scripts, docs and notes follow the new paths).
- Remote work: Claude Code cloud sessions (also from the phone app) and a Windows PC. The cloud gets a setup script (no GPU: software Vulkan). Windows must build, run and test (PowerShell scripts, test fonts from the repo); Chromium reference captures stay Linux/cloud only.
- The project notes kept in Claude's local memory move into the repo docs (cloud sessions don't see that memory). The reference source cache stays out (re-downloadable).
- Git workflow: cloud sessions push their own branch and open a pull request that the user merges on GitHub; sessions on the PC commit to `main` and push. The repo goes into the user's existing, empty private GitHub repo `github.com/Vatrix33/statfick`; commits carry `meistervatrix@gmail.com`; this PC logs in with the GitHub CLI (`gh`).
- Downloads (2026-10-30): everything on GitHub stays private. The old game goes up as a release in the private repo (Linux and Windows zips: the executable with the game files it reads); for others, the user shares the same zips through another site with a secret link.

## Progress

- **M0 is done** (2026-10-16):
  - `Statventure.slnx`, `Directory.Build.props`.
  - `Tools/Scripts/env.sh`, `build.sh` (shaders, then the solution), `test.sh` and `accept-golden.sh`.
  - `Tools/ShaderCompiler`: `Engine/Statventure/Shaders/Source/*.vert|frag|comp` become `Shaders/Bin/*.spv`, embedded. `*.glsl` files are includes.
  - `Core/Log`, `Core/NativeLibraries`, xUnit tests.
- **M1 is done** (2026-10-16):
  - **Platform:** `Platform/Window` (GLFW; logical units versus pixels; cursors, clipboard, fullscreen) and `Platform/Input` (an event queue plus state).
  - **Device:** `Graphics/GpuDevice`.
    - Vulkan 1.3: dynamic rendering, sync2, descriptor indexing, buffer device address, a timeline semaphore.
    - VMA memory.
    - Deferred release scopes: anything released while recording waits for that recording's submission.
    - `Immediate`, and frames in flight with a per-frame upload buffer.
  - **Resources:**
    - `GpuBuffer`: Device, Upload or Readback memory, all with device addresses.
    - `GpuImage`: tracks its layout; upload, mipmaps by blitting, readback.
  - **Pipelines and textures:** `Bindless` (16384 textures, 6 samplers, white texture at index 0), `ShaderLibrary` and `PipelineCache` (one shared layout: bindless set 0 plus 128 bytes of push constants; blend modes; vertices pulled from buffers).
  - **Drawing and presenting:** `CommandList` (barriers, dynamic rendering, copies) and `Swapchain` (UNORM, rebuilds itself).
  - **Loop:** `Core/Application` (main loop, minimize handling, frame capture to PNG).
  - **Images:** `Imaging/ImageFile` (stb load, PNG save).
  - **Sandbox:** `Samples/Statventure.Sandbox` (`--capture file.png`).
  - **Tests (13):** a headless GPU fixture; golden images in `Tests/Statventure.Tests/Golden` (renders that don't match go to `Actual/` for review); a real window and swapchain test.
  - **Display scale:** the engine follows the scale GLFW reports. It reported 1 for this display at first; since 2026-10-23 it reports 1.65 (KDE Wayland, fractional scaling).
- **M2 is done** (2026-10-16), in `Canvas/`. 36 tests.
  - **Canvas:** records in logical units at any scale and renders through one instanced pipeline (`canvas.vert`/`.frag`).
    - Rounded boxes: per-corner elliptical radii, per-side border widths, dashes as Chromium draws them (2w dash, w gap, spread evenly).
    - Linear, radial and repeating gradients.
    - Outer and inset box shadows (σ = blur / 2).
    - Images, coverage (glyphs, paths), rounded clips, 2D transforms, per-draw colour filters, multiply and screen blending.
    - Layers: opacity, the CSS filter chain (blur, drop-shadow with σ = value, colour matrices) and backdrop blur (mirrored edges).
  - **Paths:** `Path` (SVG path-data parser, arcs, flattening to 0.1 px), `PathRasterizer` (exact area coverage, nonzero/even-odd, stroker with joins and caps), and a path cache in `TextureAtlas` (R8 shelf-packed pages, which the glyphs will share).
  - **Browser references:** `Tests/.../Canvas/ReferenceScenes.cs` defines scenes in CSS/SVG plus canvas calls. `Tools/Scripts/make-css-references.sh` renders them in the old game's test client into `Golden/css-*.png`. All five scenes match within tolerance 12 / 1%.
  - **Review tools:** `Tools/Scripts/diff-images.py` shows reference, render and difference; `STATVENTURE_KEEP_RENDERS=1` keeps renders.
  - **Performance:** 20,000 primitives record in 4 ms; the whole frame takes 6 ms.
- **M3 is done** (2026-10-16), in `Text/` plus `Canvas/CanvasText.cs`. 42 tests.
  - **FreeType:** own bindings (`Text/FreeType.cs`, FreeType 2.14.3 layouts; `CLong` for C long; the glyph slot has `glyph_index` after `next`).
  - **Fonts:** `FontFace` (FreeType plus HarfBuzzSharp) and `FontLibrary` (CSS font matching, synthetic bold when 600+ is asked of a ≤500 face, synthetic oblique, per-character fallback families).
  - **Rasterizing:** `GlyphRasterizer` follows the system's hinting; fake bold is ppem/24, fake italic a 1/4 shear; quarter-pixel offsets.
  - **System settings:** `FontRenderSettings.System` reads fontconfig through `FcFontMatch` (here: RGB subpixel, slight hinting, default LCD filter) or Windows font smoothing.
  - **Layout:** `TextLayout` handles white-space modes, text-transform, HarfBuzz shaping with fractional advances (as Blink), and wrapping at spaces and dashes with hanging spaces. It also does `overflow-wrap: anywhere`, Blink line boxes (rounded ascent, descent and gap; top half-leading floored), alignment, ellipsis that keeps the preceding space, line clamp, and min/max-content widths.
  - **Canvas text:** glyphs are cached in the atlas. LCD subpixel text uses dual-source blending (`canvas_lcd.frag`, `BlendMode.DualSourceLcd`), only straight on the target with no layer, filter, blend or rotation; otherwise grayscale.
  - **Mask gamma:** Skia's mask-gamma formula with fitted gamma 1.4 / contrast 0.64. Glyphs are placed at quarter pixels (as the game's Chromium does).
  - **Browser reference:** the css-text scene matches Chromium (0.28% of pixels beyond 40 levels).
  - **Not covered yet:** rotated text draws axis-aligned glyph quads; carets and hit testing come with M5.
- **M4 is done** (2026-10-17). Styles, the box tree, layout, painting, transitions and animations, and incremental updates. 129 tests.
  - **CSS:** `UI/Css` (tokenizer, parser, selectors and matching) and `UI/Style`:
    - `Properties` (+ `.Shorthands`): every property the game's stylesheet uses, with parsing, shorthands, aliases and logical properties. The game's sheet has no unknown properties (a test checks this).
    - `Stylesheet` prepares declarations once: shorthands expanded, invalid values dropped, var() kept for later.
    - `StyleEngine`: a rule index per media environment; the cascade (UA, author, style attribute, !important, presentational hints); custom properties with cycles; var() substitution, also in shorthands; CSS-wide keywords; blockification; pseudo-elements (::before, ::after, ::placeholder, ::selection); keyframes.
    - `UserAgentStyles`: the parts of Chromium's html.css the UI uses.
  - **DOM:** `UI/Dom/UiDocument` (invalidation and the update: styles, boxes, layout, paint) and `Markup` (an HTML fragment parser and serializer, as innerHTML). Elements have sibling links and version counters (attributes; anything in the subtree) for caches.
  - **Layout:** `UI/Layout`:
    - `BoxBuilder` builds the box tree: anonymous blocks and items, ::before/::after, list markers (outside and inside), closed details, containing blocks.
    - `LayoutEngine`: block flow with margin collapsing, widths and heights (box-sizing, min/max, aspect-ratio), scroll containers and scrollbars (15 px, thin 10 px, gutter), viewport scrollbars with overflow propagation, positioned boxes (static positions aligned in flex and grid), relative offsets, intrinsic sizes, centred button content.
    - `InlineLayout`: white space, shaping (ligatures off under letter-spacing, tabular-nums), break opportunities, inline box fragments, atomic inlines, vertical-align (sub/super in LayoutUnits), text-align (overflowing lines start-align), justify, ellipsis (keeps the space before it), line clamp (hidden lines still laid out), hanging preserved spaces, br and newline fragments.
    - `FlexLayout` (CSS Flexbox §9, automatic minimum sizes from content, wrap-reverse) and `GridLayout` (placement, auto-fill/fit, named lines and areas, the track sizing algorithm with definite, indefinite and min/max-content modes).
    - `ReplacedContent`: images, canvases, inline svg (viewBox ratio), and form controls sized as Chromium does (text fields fitted for Lato and STIX at 10–30 px, selects measured word by word, checkbox/radio/range baselines).
  - **Font rounding:** ascent, descent and line gap round halves up, as Skia does (it was banker's rounding before).
  - **Browser comparisons:**
    - `Tests/.../Ui/LayoutFixtures.cs` has 36 fixture pages. `Tools/Scripts/make-layout-references.sh` measures them in the old game's Chromium (152) into `Golden/layout-reference.json`. Every element box and text fragment matches within 0.5 px.
    - `Tools/Scripts/make-game-references.sh` drives the old game through 11 screens (sheet, seven modals, shop, map, combat; transforms and animations frozen). It stores the markup and the measured boxes in `Golden/game-ui.json.gz`, and the stylesheet in `Data/Game/styles.css`. All 11 screens match box for box.
    - Symbols missing from Lato and STIX come from system fonts in Chromium. The test emulates that: fontconfig, plus a table of the fonts Chromium picked for the game's 20 symbols.
  - **LayoutUnits:** Blink lays out in 1/64 px fixed point. The engine truncates as Blink does: resolved lengths, number line-heights, flexed sizes, grid fr and distributions, and text item widths rounded up. Pixel snapping then rounds box edges the same way. At 0.02 px tolerance, 37 of 49 comparisons match exactly.
  - **Painting:** `UI/Paint/UiPainter` (+ `.Content`, `.Controls`, `.Svg`):
    - Appendix E paint order with stacking contexts (z-index, flex/grid item z-index), positioned layers and atomic inlines as pseudo-contexts.
    - Scroll-container clips (also for out-of-order layers), transforms (transform, translate, rotate, scale), opacity/filter/blend layers, backdrop blur, clip-path inset and clip rect.
    - Backgrounds: colours, gradients (stop resolution, radial ending shapes), url and SVG images, size/position (3/4-value syntax)/repeat, background-attachment fixed, root/body propagation to the canvas.
    - Borders with per-side colours and styles (new canvas `DrawBox(BorderSides)`; sides meet on the corner diagonals), box shadows, outlines that follow radii.
    - Text: glyph runs (new `Canvas.DrawGlyphs`), text-shadow (blurred via layers), decorations propagated from ancestors, hidden clamp lines.
    - Images with object-fit, inline SVG and SVG images: a vector subset with gradients, transforms, use, viewBox and preserveAspectRatio. SVG images whose rect uses feTurbulence (+ feColorMatrix) render through `Imaging/Turbulence` (the spec's reference algorithm, linearRGB to sRGB).
    - Form controls (checkbox, radio, range, progress, text fields with value/placeholder and the 15 px number spinner, select with chevron), outside/inside list markers and disclosure triangles, classic scrollbars with scrollbar-color.
  - **Screenshot comparisons:** `make-game-references.sh` also stores Chromium's screenshot of every state (`Golden/game-*.png`). `GamePaintTests` paints each state and compares (tolerance 24, at most 3% of pixels; inline SVG art is masked because Blender renders replace it). All 11 pass. Beyond 12 levels: 0.1–1.3% of pixels, mostly glyph edges.
  - **Known approximations:**
    - At fractional font sizes (e.g. 18.72 px), Chromium's text is up to 0.2% narrower. The cause isn't found yet: integer and quarter sizes match exactly, and FreeType's linear and hinted advances both disagree. The game uses mostly integer sizes.
    - Variable fonts' weight axes aren't applied (only system symbol fallbacks are variable).
    - Inside disc/circle/square markers are text-based (the game has none).
    - The inside disclosure marker's width is fitted (1.0593em).
    - Content of closed details isn't laid out. Chromium lays it out invisibly, so the tests skip it.
    - A block inside an inline box makes the inline a block; the game has none, and `UiDocument.LayoutDiagnostics` reports any.
  - **Animations:** `Style/AnimationEngine` and `Style/Interpolation`.
    - @keyframes with per-keyframe easing; iteration count, direction, fill mode, delay (also negative), play state.
    - Transitions start from the value shown and stop when no longer listed.
    - Interpolation: premultiplied colours; transform lists pairwise or through 2D matrix decomposition; filter and shadow lists; clip-path inset; visibility.
    - Paint-only values are written into the shown style in place; layout values relayout the element; inherited values restyle the children.
    - `AnimationReferenceTests`: 6 fixtures sampled at 10 times match Chromium's getComputedStyle (`make-animation-references.sh`).
  - **Incremental updates:** an update redoes only what a change needs.
    - **Invalidation sets** (`Style/RuleFeatures`): every class, id, attribute and state the selectors name, with how far a change reaches (the element, its descendants, its following siblings). Changes to what :has() arguments look at restyle the ancestors. Attributes decide :checked, :disabled, :placeholder-shown and :any-link. A class, attribute or state no selector names restyles nothing.
    - **Matching:** rules are bucketed by id, class, tag, attribute, root, and the alternatives of :is()/:where(). A counting Bloom filter of the ancestors (as WebKit's) skips selectors whose ancestors can't be there. Structural pseudo-classes and attribute words don't allocate.
    - **Shared styles:** elements with the same matched rules, style attribute and parent style share one computed style. A recomputed style identical to the old one keeps the old instance (so the children's shared styles still apply). Computed styles are never changed after computing; animated elements show a private copy.
    - **Pseudo-elements:** ::before/::after are computed only when a `content` value wins; ::placeholder and ::selection when first asked for.
    - **Style differences** (`Style/StyleDifference`): compiled structural comparisons classify a new style as no change, paint only, layout, or box tree (display, position, content, markers, white-space, becoming a containing block).
    - **Boxes read their element's current style** (anonymous boxes and markers derive theirs again when it changes), so paint-only changes need no box work. Elements keep their boxes: a box-tree change rebuilds that element's boxes and has its parent collect its child boxes again; containing blocks are reassigned in one pass.
    - **Layout cache:** a box laid out again in the space of its last layout, with nothing changed in it, keeps that layout. Changes mark the box and its ancestors; intrinsic sizes and collected inline content stay until then. Flex and grid measure items through a separate cache of measurements, so measuring doesn't undo the final layout.
    - **Scrollbars** of overflow: auto scrollers stay from one layout to the next until the content fits without them, as in Blink (they no longer flip each layout). Positioned boxes are laid out inside their containing block's frame, before its overflow and scrollbars are measured; positioned boxes inside inline containing blocks are laid out too.
    - **Fonts:** glyph coverage per face and character fallbacks are cached; font lookups don't lock on hits.
    - **Painting:** SVG shapes, transforms, id maps and inherited paints are kept until the SVG changes. Layer images of a recording that is never rendered go back to the pool.
    - **Tests:** `IncrementalUpdateTests` changes every game screen at random (classes, text, inline styles, attributes, states, removed, copied and inserted elements) and compares each update with a fresh document of the same tree: every computed style and every box with its lines and items, exactly. `STATVENTURE_INCREMENTAL_ROUNDS` and `STATVENTURE_INCREMENTAL_SEED` widen the search (seeds 0–15 pass). `IncrementalWorkTests` checks the work done through `StyleEngine.StylesComputed` and `LayoutEngine.BoxesLaidOut`.
    - **Benchmark:** `Tools/Scripts/benchmark.sh [state ...]` (`Tools/UiBenchmark`) times the captured screens. Release, this machine:

      | | sheet | combat | shop |
      |---|---|---|---|
      | Cold first update (styles, boxes, layout) | 37 ms | 38 ms | 21 ms |
      | Restyle everything | 4.8 ms | 0.9 ms | 1.4 ms |
      | Relayout everything | 6.8 ms | 0.6 ms | 2.5 ms |
      | Class toggle, paint-only style, unused attribute | < 0.03 ms | < 0.01 ms | < 0.01 ms |
      | Text edit, padding change | 0.15 ms | 0.03 ms | 0.03 ms |
      | Paint and render a frame | 3.3 ms | 2.5 ms | 6.6 ms |

      Before this work, one attribute change took 20 ms on the sheet and a full restyle and layout 130–150 ms.
    - **Known cost:** a box that is measured and then laid out at another size (stretched flex and grid items) is laid out twice when something inside it changes.
  - **Open question for the port:** symbols such as ⚙ ✦ ☾ ↓ ＋ ⚔ ♟ in UI text aren't in Lato or STIX. The decision so far covers ♜ ♡ ◈ ▣ ◌ ◫ ◆ (vector icons). Ask about the rest when porting.
- **M5 is done** (2026-10-17 to 10-30). 496 tests.
  - **Hit testing** (`UiDocument.HitTest`): the painter's traversal records hit regions in paint order (Blink's 1×1 square test for boxes, pixel-snapped rects for text, rounded and overflow clips, opacity 0 still hit, pointer-events, visibility, SVG shapes). `GameHitTests` checks a 10 px grid of `elementFromPoint` on all 11 captured screens.
  - **Events** (`UiEvents`, `UiInput`): DOM dispatch (capture, target, bubble), Chromium's pointer and mouse order, boundary events, :hover and :active chains, clicks, double clicks, auxclick, contextmenu, pointer capture, focus and blur events, :focus-visible, Tab order, Enter and Space activation, fake mouse moves after visual changes.
  - **Scrolling** (`UiDocument.Scrolling`, `UiInput.Scrolling`), from Chromium 152's source (`cc/`, `ui/native_theme`, Blink `scroll/`):
    - Scrollbars are Fluent scrollbars: 15 px (thin 10 px), 18 px arrow buttons, a pill thumb at least 17 px long, `#FCFCFC`/`#8B8B8B`, hovered and pressed `#636363`, scrollbar-color with Chromium's contrast blending. Arrows are triangles filled without antialiasing. `ui-scrollbars` matches Chromium to the pixel except two thumb-corner pixels.
    - Scroll API: offsets clamp (also after layout), the root's are the document's, `ScrollTo`/`ScrollBy`/`ScrollIntoView` (block/inline alignment, scroll-margin, scroll-padding, every scroller out to the document), `ScrollMode` (auto follows scroll-behavior).
    - Animations as cc's: programmatic √distance frames on cubic-bezier(0.4, 0, 0, 1) up to 1.5 s, keyboard 0.15 s, wheel 6–12 frames ease-in-out, retargeted with the current speed, linear autoscroll.
    - Wheel: 500 ms latching, chaining only when a turn starts, overscroll-behavior, Shift turns it horizontal, Ctrl zooms (`ZoomWheel`), only the first event of a turn cancelable. The host converts notches: Chromium 152 scrolls 120 px per notch on Linux and 100 px (3 lines of 33⅓) on Windows.
    - Keyboard: arrows 40 px, Page Up/Down and Space 87.5% of the scrollport, Home/End, from the focused element (else the last pressed one) outwards; text fields keep their keys.
    - Scrollbars: thumb drags, Shift+click jumps, buttons and track scroll and repeat after 250 ms at 20 steps a second, snap-back on Windows only; the page sees mousedown/mouseup but no click; focus only moves to a focusable element.
    - `scroll` events once per frame, `scrollend` when input and animations stop.
    - position: sticky as Blink's StickyPositionScrollingConstraints (against the scroller's content box; the containing block less the sticky box's margins). `ui-sticky` and `ui-sticky-viewport` match Chromium.
    - Scrollable overflow follows Blink's ScrollableOverflowCalculator (end padding after in-flow margin boxes, not after negative-margin children or out-of-flow boxes).
    - CSSOM geometry: clientWidth/Height/Left/Top, scrollWidth/Height, offsetWidth/Height/Left/Top, offsetParent, getBoundingClientRect, getClientRects.
  - **Paint references** (`PaintFixtures`, `make-paint-references.sh`): fixtures screenshotted in the game's Chromium.
  - **Canvas:** box coverage now converts distances to pixels through the affine local coordinates (the distance's own derivatives were wrong on 1 px boxes). Pages get a white base background, as in a browser (`UiDocument.BaseBackground`).
  - **Forms and text editing (#27, done 2026-10-23):** activation, spin buttons, range, search cancel, selects with the game-styled popup, Blink's editing commands, undo, ICU word breaks, mouse and page selection. Painting is matched to Chromium screenshots (`InputPaintReferenceTests`, captured by the input harness):
    - Caret: Blink's caret rect (x = round(x − 0.5) inside the line and editor, as tall as the font; an empty editor's at its start, centred on the line).
    - Selections: Blink's fast selection painting (unselected glyphs, highlight, selected glyphs clipped to it), a space's width more when the line's end is selected (also for `<br>` and preserved newlines), inactive-window colours; decorated text takes Blink's highlight overlay path.
    - Focus rings (`outline: auto`, `UiPainter.Outlines.cs`): OutlinePainter's two strokes (2 px white outside, 2 px outline colour inside, 1 px over a border), corner radii (a themed control's own, author radii, else 3 px), around the union of the outline rects including block ink overflow (children, text, anchors' out-of-flow boxes), right-angle region paths rounded like SkCornerPathEffect.
    - Text decorations (`UiPainter.Decorations.cs`): applied decorations propagated in style (stopped by atomic inlines and out-of-flow boxes), decorating boxes (the line's or an inline box's font), thickness, `text-underline-offset`, `text-underline-position`, solid/double/dotted/dashed/wavy lines as DecorationLinePainter draws them, `text-decoration-skip-ink` from unhinted glyph outlines (`Text/GlyphOutlines.cs`) with Blink's CJK exceptions (a generated table), decoration shadows; fields and placeholders use the same painter. `ui-decorations` matches Chromium.
    - Text fields' theme border and background are drawn without antialiasing (as with Skia's default paint flags).
  - **Text fields scroll (2026-10-23):** a textarea is a scroll container (wrapped text sets its overflow, auto scrollbars); the caret is revealed only for user moves, typing and mouse selection, through every scroller out to the document (Blink's RevealSelection: ToEdgeIfNeeded, TopAlways after a page move); Page Up/Down follow ModifyWithPageGranularity (a one-line field leaves them to the page); up/down keep their x; inner-editor scrolls fire scroll/scrollend at the input; scroll and scrollend share Blink's per-frame queue; selectionchange runs after the frame's scroll events. `field-scroll` events, states and screenshots match Chromium.
  - **Resizers (2026-10-23):** `resize` (a textarea's `-internal-textarea-auto` is both ways) on scroll containers; Blink's corner rect and scroll corner (bars shortened by it), the diagonal grip lines drawn without antialiasing; dragging sets the inline width and height (at least the corner's size) with the resize cursors. Scrollbars paint with the box's background and its own outline after its content (Blink's order). The `resize` fixture matches Chromium.
  - **Animation and transition events (2026-10-23):** Blink's event delegates: each animation and transition keeps the phase its events last saw; the frame's timing update queues animationstart/iteration/end and transitionrun/start/end (transitions first, then each in composite order), a cancel queues its event at once (removed animation, transition replaced or no longer listed, display: none, removal, a pseudo-element going away); the queue is dispatched after the frame's scroll events. With it the transition update follows Blink: a running transition to another value is cancelled first, the before-change style samples the running transitions now, a reversal is shortened (CSS Transitions' reversing shortening factor), `transition: none` cancels all; finished animations stay matched by name (a restyle no longer restarts them) and an element shown again starts its animations. The `animation-events` fixture (events and elapsed times, to a tenth) matches Chromium; `UiAnimationTests` covers restyles, display: none and reversal.
  - **Title tooltips (#28, 2026-10-23, `UiInput.Tooltips.cs`):** Chromium's two halves ported from source (the browser draws them in a window of its own, which the harness can't capture): Blink's text under the pointer (nearest title, an SVG element's first `<title>`, else an invalid control's validation message unless its form is `novalidate`; passed on when the point or text changes, cleared first when the same text comes from another node, nothing over scrollbars) and views' TooltipController (shown 500 ms after the pointer rests, no hiding by time; a press hides it until the text under the pointer changes, a key press until the next text change; the wheel and leaving the window hide it; text cut at 2048 characters, trimmed, tabs as 8 spaces). `<sv-tooltip>` in the top layer, placed as TooltipAura does with the window as its screen (10 px right and 15 px below the pointer, max width min(800, (width + 1) / 2), shifted left, above the pointer when it doesn't fit). Select lists carry their options' and groups' titles (a page of their own). `UiTooltipTests`.
  - **Window host (#28, 2026-10-23, `UI/UiHost.cs`, `UI/UiHostInput.cs`, `Platform/SystemSettings.cs`):** runs a document in a GLFW window: key values from GLFW's char events (else named keys, the keypad's navigation keys without Num Lock, the layout's character with Shift/Caps Lock for letters), codes by US position, a modifier key held while it goes down, wheel notches as 120 px (Linux) or 100 px (Windows), focus and leave events; the cursor as a system shape or a cursor image (`cursor: url() x y, keyword` parsed as Blink does; the first loaded image ≤ 128 px, one over 32 px only where it fits in the viewport; scaled to the pixel density); the system clipboard; the caret blink interval and reduced motion as Chromium reads them (GTK's gtk-cursor-blink/-time and gtk-enable-animations via GNOME's settings or settings.ini; GetCaretBlinkTime and client-area animations on Windows); painting at the window's density. The sandbox is now a UI demo (`--capture` works through the swapchain). `UiHostInputTests`, `UiCursorTests`.
  - **Selector fix:** the ancestor filter required compounds before a sibling combinator to be ancestors (`#a:focus ~ #b > #c` never matched `#c` in styles); now only compounds followed by a descendant or child combinator count, as in Blink's SelectorFilter.
  - **Known differences left (not yet ported):** a non-auto outline on an inline box that wraps lines is drawn per line box (Blink unites them with ComplexOutlinePainter); `::selection`'s own text decorations aren't drawn; a list box's focused option ring; Blink drops the ellipsis of editable text holding the selection focus (TextOverflowClipWithSelection); rounded-corner antialiasing differs from Skia by a few pixels at small radii; box edges that slant under a transform (rotations, skews) are antialiased by the canvas's ramp one pixel wide across the edge, where Chromium's CPU raster fills the box as a path (Skia's analytic scan converter: area coverage, its points snapped to a quarter pixel down the screen), up to about 18 levels apart at 1× and 27 at 1.65 even under a uniform rotation (user decision 2026-11-05, "Decision on box antialiasing under non-uniform transforms"; the `box-slant` fixture is held at 12 levels / 1 % as a guard); an opaque box's outer shadow is cut at the box's edge, where Blink insets the clip-out by one pixel (`ClipToBorderEdge`), so a fractional edge shows the shadow at half (same decision); Blink's background bleed avoidance under an opaque rounded border is done for a box with background images (since 2026-11-06, Progress, "Background layers as Blink paints them") but not for one with a colour alone (its fill and border are one primitive), so that fill shows in the border's outer edge pixels (same decision); background layers ("Decision on background layers", 2026-11-05/06): `background-clip` and `background-origin` are parsed and ignored (the border box and the padding box, their initial values; the game uses neither), Blink's clip-layer bleed avoidance is painted as a rounded clip on each draw, where Blink clips the whole background once as a layer (the user's decision of 2026-11-06 in that paragraph; no game screen or paint fixture has such a box; the fixture's `two` box corners up to 59 levels off at 1× and 85 at 1.65), a raster image repeating along one axis only is sampled with repeat along both (Skia clamps the other; the game has no raster backgrounds), a composited fixed background (`CanCompositeBackgroundAttachmentFixed`) is painted as Blink paints it but not given its own cc layer (nothing on screen differs; LCD text is off at a device scale of 1.5 and up, where it applies), and SVG shapes in a background tile are drawn with the canvas's path antialiasing (the fixture's ratio-only SVG circle, up to 61 levels at its edge); a dashed border's dash ends at a device scale of 1.65 sit on whole pixels where Chromium's are fractional (found on 2026-11-06 by the background fixture's `d3` box, a 3 px dashed border: 128 pixels past 12 levels, worst 51, none at 1×; the background through the gaps matches within a level; the border painter's, not the background's, and not looked into; for whoever picks it up: the canvas draws the dashes in `Shaders/Source/canvas_fragment.glsl`'s `dashMask` (dashes twice the width, gaps the width, spread so each side starts and ends with a dash), Blink's are `BoxBorderPainter` with `StrokeData`'s dash path effect, and `ui-bg-layers@1.65.png` with `BackgroundLayerReferenceTests`' element list shows the case); two found on 2026-11-06 by the text-verdict fix, open, not accepted (the user decides when; "Small tasks left from earlier milestones" has the numbers and where to start): a select's or input button's label sits one device pixel below Chromium's at 1.65 (`UiPainter.SnapBaseline` rounds the baseline to a whole CSS pixel), and at 1.65 a composited box's text on its own opaque background is grayscale where Chromium's is LCD when the box's bottom falls on a fractional device pixel (likely `PaintTarget.GlyphBounds`, ascent to descent, against Blink's glyph ink); one found on 2026-11-07 by M11 step 9, open in the same list, no go yet: a select with an author background, border and padding is 6–7 px wider than Chromium's (`ReplacedContent.SelectSize`'s internal paddings on top of the author's, suspected); three found on 2026-11-07 by M11 step 10, open in the same list, no go yet: at 1.65 a status line's text (no background of its own) is grayscale beside a tall scroller where Chromium's is LCD (up to 1,823 pixels of an editor screen), a positioned box with a z-index inside a scroller (painted from an outer stacking context) is composited apart by Chromium, its text grayscale, and not by the engine, its text LCD (the editor's reference list was moved into the flow for it), and a stacking context on the editor's form rows turned their text grayscale in the engine at 1.65 (Chromium's look with it not captured); a square corner's pixel takes the distance to the corner point where Skia's area coverage is the product of the two edges' (same decision); a blurred box shadow under a transform has Skia's one device sigma since 2026-11-05, but Blink gives a box whose transform is a 2D scale an effect node of its own (`NeedsEffectFor2DScaleTransform`), a layer bounded by what the box paints in its own units, which cuts a shadow blurred wider on the device at that rect rounded out to whole pixels (up to about 12 levels for a 12 px blur under `scaleX(0.2)`, 1–3 at the cutscene card's bar ends); the painter makes no such node (user decision 2026-11-05, "Decision on blurred shadows under non-uniform transforms"); Skia blurs a rectangle with its edges rounded to whole pixels (`SkBlurMask::BlurRect` and its nine-patch), the canvas at the exact fractional edges, so a glow around a fractional edge sits up to half a pixel apart (same decision); the canvas blurs shadows with a true Gaussian where Skia's mask blur (`SkMaskBlurFilter`) takes three box passes of about 1.88σ (support near 2.8σ), so since the inset shadow is cast from Blink's frame the game screens' blurred inset shadows sit a level lighter than Chromium's in places (`game-sheet` up to 4 levels in 2 pixels at 1.65; a candidate for a later step; user decision 2026-11-05, same decision); a transformed box Chromium composites into a layer of its own (`will-change: transform`, a transform animation) is rastered there by itself and drawn through its transform by cc, where the canvas draws through the transform directly: the same place (the `offset-measured` fixture's composited cards' left and top edges within 0.08 px of Chromium's; tested within 0.1 px, the tolerance the user approved on 2026-11-05), but under `will-change: transform` with a scale the far edges land on whole pixels in Chromium (0.2 px off at 1×, 0.4 px at 1.65) and the pixels differ (93 past 12 levels at 1×, worst 42; 196 at 1.65, worst 85), a little at the edges of the others (a translation animation and `will-change` on a translation 8 pixels, a scale animation 1), and with text more (a card with a border and text under a scale animation, 76 pixels at 1×, worst 75: its text grayscale where Chromium keeps LCD) (user decision 2026-11-05, "Decision on the paint offset of transformed boxes"); text drag and drop isn't implemented; animation events of different elements in the same frame come in tree order (Blink's timeline sorts them by the elements' addresses, which follow creation); `transition-behavior: allow-discrete` isn't parsed. Host: GLFW doesn't say whether scrolling comes from a wheel or a touchpad (all are notches); the wait, help, cell, alias, copy, zoom and grab cursors show the arrow unless the page gives an image (the game uses none of them; its grabbing cursor will be an image); a non-letter key with Ctrl and Shift reports its unshifted character; AltGr is reported as Alt; SVG cursor images and .cur hotspots aren't supported.
  - **Zoom (#29 + #30), done 2026-10-24 (418 tests).** Measured 2026-10-23: painting the paint fixtures with the canvas scaled by 1.65 differs from Chromium at a 1.65 device scale in 0.1–3% of pixels (1× matches within 0.2%): Chromium lays the page out in device pixels (zoom-for-DSF), so 1 px borders stay one device pixel and text baselines and edges snap to device pixels. Plan (Blink's effective zoom; device scale and page zoom are one zoom):
    - Document: `DeviceScale` (the window's), `PageZoom` (Electron's View menu: level ±0.5, factor 1.2^level), `Zoom` = both. The viewport is given in window units (DIPs); layout works in device pixels (DIP × DeviceScale); the CSS viewport (media queries, vw/vh) is device px / Zoom.
    - Style: every length is its CSS px value × Zoom (CSSLengthResolver::ZoomedComputedPixels: px, absolute units, em/rem from specified sizes, viewport units); font sizes zoomed; border and outline widths floored after zooming (ClampLineWidth: (0, 1) → 1); images' natural sizes × Zoom; SVG content scaled.
    - Hard-coded sizes in layout and painting follow Blink: theme parts with the element's zoom, scrollbars (15 px, arrows, thumb) and scroll steps (40 px lines) with the device scale only (Chromium's scale_from_dip, WindowToViewportScalar), focus rings, caret.
    - DOM in CSS px: event coordinates, geometry (getBoundingClientRect, offset*, client*, scroll*) and scroll setters converted as Blink's AdjustForAbsoluteZoom does; input from the host in DIPs.
    - Painting at canvas scale 1 (device pixels).
    - References: `Tools/PaintReference` (DevTools device metrics override, `--scale=1.65`; `--mode=zoom` for page zoom) for paint fixtures; layout fixtures and the game screens at 1.65 too. Everything at zoom 1 must stay as it is.
    - **Done (2026-10-24):** document zoom API, zoomed style lengths, theme parts, scrollbars, DOM geometry in CSS px, whole-pixel scroll offsets, text metrics at zoom; the paint fixtures at a 1.65 device scale (references from X11 Chromium with `--force-device-scale-factor`, CPU raster) match in paint and layout. Text: Chromium 152 on Linux loads every web font with Fontations and Skia draws its glyphs from the outline, not with FreeType's renderer. The engine now does the same (`Text/Skia`: Skia's analytic anti-aliasing scan converter, its 4× LCD FIR filter and RGB565 masks, SkMaskGamma with Linux's contrast 0.2 / gamma 1.2, the CPU blitter's 5-bit LCD blend, SkGlyphPositionRoundingSpec placement); the fitted shader gamma is only left for FreeType's fake bold. Field, select and button text are painted from the unsnapped content box (Blink paints them at fractional positions).
    - **Hinting (2026-10-24):** at a device scale of 1 Chromium hints with Fontations: skrifa 0.44's autohinter in light mode for TrueType fonts (Skia forces it for glyf fonts at slight hinting). Ported as `Text/Autohint` (font access through FreeType's unscaled outlines, the GSUB coverage walk of `autohint_shaping`, style classes generated from skrifa's tables by `Tools/Autohint/generate.py`, outline topology, widths and blue zones, segments and edges, edge and point hinting). `TextHintingReferenceTests` compares glyph sheets (every letter, digit and sign the game writes, 9–28 px, Lato regular and bold) with Chromium at scale 1 (`make-scaled-paint-references.sh 1 1 text-fixtures.json`): every hinted outline matches. Advances now follow skrifa too (the hmtx advance through a 16.16 FreeType scale and FT_MulDiv, kerning linear), which moved glyphs that sat at quarter-pixel boundaries onto Chromium's quarter.
    - **Text differences left:** LCD blending: Skia's CPU blitter (the references) floors `(src − dst)·m >> 5`, the GPU rounds (one level); Blink sums glyph positions in another float order, so a glyph within a float ulp of a quarter pixel can land on the next (about one in 2,500); CFF (STIX) outlines and their light hinting come from FreeType rather than skrifa (the STIX glyph sheets match Chromium exactly at 1× and 1.65 all the same); text-decoration ink skipping of synthetic bold text takes FreeType's emboldened outline (Skia's path strike strokes it; no game text does this); system fallback fonts are drawn like web fonts (Chromium draws them with its FreeType backend, whose fake bold emboldens and widens advances); medium and full hinting (not this system's setting) use FreeType's autohinter where Chromium runs skrifa's TrueType interpreter for fonts with instructions.
    - **Done (2026-10-24, later):** input in window units (DIPs) scaled to device pixels for hit testing and scrolling, DOM event coordinates in CSS px (MouseEvent floored, PointerEvent fractional except click/auxclick/contextmenu, movement from truncated screen positions), wheel deltas over devicePixelRatio, 40 px × device scale line steps; caret width max(1, device scale); focus ring widths and offsets by Blink's zoom rules; tooltip, select popup and validation bubble placed in DIPs; the host's `SetZoom(window content scale, page zoom)` with the canvas in device pixels. Font sizes resolve from the unzoomed specified size (em/rem lengths are specified × zoom), number line heights round as Blink, fixed lengths truncate to LayoutUnits, line tops round before the baseline, decoration lines snap before their shift, inline max-content sums text items without float error (a 1/64 px short sum wrapped a `<summary>` at zoom 1.98). References at 1.65 and at 1.65 with page zoom 1.2: paint fixtures (`make-scaled-paint-references.sh SCALE [PAGE_ZOOM]`), layout fixtures (`make-scaled-layout-references.sh`), the 11 game screens (`Tools/GameReference`, `make-scaled-game-references.sh`), the input fixtures (`make-input-references.sh 1.65`: an X11 window at the scale instead of offscreen rendering, which ignores it). Input fixes the 1.65 input references showed: the document's programmatic and scroll-into-view scrolls compare unrounded offsets (Blink's RootFrameViewport never rounds: one that rounds back to where it is fires scrollend without scroll), while elements round first; a press inside a field's selection is decided by the position under it (FrameSelection::Contains, both ends included), not the painted highlight; a field's inner editor scrolls to rounded offsets within its pixel-snapped content (text plus caret width) and reports CSS px; resizing works in CSS px (box size, pointer and resizer over the zoom, LayoutUnit truncation) from floored pointer positions. Capture rules learnt: the window must be whole device pixels (capturePage scales the surface to ceil(DIPs × scale): 884 DIPs at 1.65 blurred every page-zoom golden), the page zoom is set before the fixture loads (a later zoom scales scroll offsets already set), frames are sized to whole device pixels; an X11 window out of sight gets a frame only every 2 s, so the scaled input harness runs with `--disable-frame-rate-limit`, and the window manager adds a few pixels to X11 windows, so each scaled input result records the page's viewport and the replay uses it.
  - **LCD text and compositing (2026-10-24 to 10-25, done: all 22 game screens exact):** opacity layers start from their backdrop and keep LCD text unless the frame has a filter or blend layer; isolation groups around blending elements turn their text grayscale (`TextHintingReferenceTests` text-opacity fixtures). cc traces of the game (`lcd_text_disallowed_reason` per layer, from `contentTracing` with disabled-by-default-cc.debug) show the rest: Chromium composites the fixed side menu, modal layers, their scrolling panels and what overlaps them into separate cc layers, and text in a layer that isn't opaque (contents_opaque, or text known to be on an opaque background) is grayscale. **User decision (2026-10-24): match Chromium exactly** — port Blink's compositing reasons, layerization (overlap, merging) and opaque-area tracking, tested against per-screen layer lists from traces. Done: property trees, paint chunks, layerizer and per-chunk LCD verdicts drive the canvas (two paint passes); `CompositingReferenceTests` compares every game screen's layers with Chromium's. **Details, rules, file map, debugging and the SVG differences left: `Docs/Compositing-Port.md`.**
  - **Shop compositing and inline SVG (2026-10-25, 467 tests):** all 22 game screens now composite like Chromium. The shop's references were inconsistent (random stock; `game-capture.js` now seeds it). Ported Blink's svg.css (inline svg clips its overflow; SVG elements transform about 0 0), replaced elements never being scroll containers, flex/grid automatic minimums by the axis's overflow value, and (user decision) CSS transforms for SVG children: the `transform` attribute as a presentation attribute, `transform-box`, TransformHelper's reference boxes and zoom, CSS `matrix()` translations zoomed, Blink's SVG bounding boxes, one display item per shape with Blink's visual rect, SVG elements' client rects. New `svg-transforms` paint fixture (1×, 1.65, 1.65×1.2; geometry exact through element boxes, edges at tolerance 60 by the path antialiasing decision) and `SvgTransformTests`. Details: `Docs/Compositing-Port.md`.
  - **Synthetic bold and font sizes (2026-10-25, 472 tests):** fake bold is Skia's: the glyph outline stroked and filled (SkScalerContextRec::useStrokeForFakeBold: width text size × 1/24 at 9 px to 1/32 at 36 px, miter joins, limit 4) by a port of SkStroke/SkPathStroker with its joiners, cappers, quad approximation of curves, rect special case and stroke-and-fill direction (`Text/Skia/SkiaStroke.cs`, `SkiaGeometry.cs`, `SkiaPathBuilder.cs`); conics reach the scan converter as quads (SkAutoConicToQuads, tolerance 0.25). FreeType no longer draws any text (the fitted shader text gamma is gone). Glyph images are now made at Blink's font cache size, truncated to 0.01 px (FontDescription::EffectiveFontSize: 12 px at 1.65 is 19.79), as advances already were. New glyph sheets: STIX regular and synthetic bold at 1× and 1.65, Lato at 1.65 (`TextHintingReferenceTests`, tolerance 1); all match Chromium. The PaintReference harness now loads the game's STIX faces in paint mode too (the system's STIX Two Text has a real bold face and stood in before).
  - **Script APIs, part 1 (2026-10-30, 481 tests, `Docs/Scripting.md`):** the document's event loop (`UiEventLoop`: timers with DOMTimer's nesting clamp, tasks, microtask checkpoints after tasks and after every callback the engine calls, C# async/await continuations as microtasks), frame callbacks, matchMedia lists with change events, the window's resize event, ResizeObserver (Blink's depth rounds, loop-limit error event, entries with LayoutUnit sizes and the snapped device pixel box), in Blink's frame order. `UiDocument.Update` now runs the frame's steps once per frame (a nested Update only does style and layout; before, a hover restyle could run the frame steps twice). New harness `Tools/ScriptReference` (`make-script-references.sh`): scenarios in JavaScript and their C# twins (`ScriptFixtures`) log callbacks tagged with frames; all 8 match Chromium.
  - **Web Animations (2026-10-30, 491 tests, `Docs/Scripting.md`):** `element.Animate` with Blink's Animation procedures (hold/start time, pending play and pause committed after the frame, finish/cancel, seeking, the finished state per frame with its microtask, Ready/Finished tasks reset as promise properties, finish and cancel events), Timing::CalculateTimings, KeyframeEffectModel's offsets, neutral keyframes and interpolation records, composited after CSS animations in creation order; Blink's animation clock between frames; gfx's CubicBezier solver (unclamped, extrapolating by the end gradients) and steps with the before flag. `GetComputedStyle()` samples animations at the frame's time in frame callbacks. Fixtures `animate-playback`, `animate-sampling` (including the game's battle keyframes), `animate-control`, `animate-composite` match Chromium; `ScriptApiTests` covers C#-only behaviour. **#31 is done.**
  - **inert, hover and focus fixup (2026-10-30, 493 tests):** the game's compact menu is `inert`. Ported Chromium 152's rule: the UA sheet's `[inert] { interactivity: inert !important }`, `interactivity` with Blink's IsCSSInert (inherited, never undone below), used pointer-events and user-select none, not focusable, an inert field's selection has no caret position (getSelection is empty); the attribute only restyles. Two general rules came with the reference: Blink recomputes :hover under a still pointer only after layout or a scroll (a fake mouse move), not after paint-only changes (the cursor still follows any change, as its update timer does); and the focus fixup blurs a focused element that can no longer take focus in a task, with blur and focusout (it was silent). New input fixture `inert` (1× and 1.65) matches Chromium; all earlier fixtures still match.
  - **M5 checklist (2026-10-30):** hit testing, event routing, hover/active/focus, keyboard navigation, text input, scroll views, tooltips, modals (the game's are `aria-modal` divs and `inert`) and drag and drop (the game drags with pointer capture only; no HTML drag and drop) are done. **M5 is finished.**
  - **Moved to one git repo for GitHub (2026-10-30, 494 tests):** `engine/` + `old-game/` in `/run/media/system/M.2_2TB/Stat Fick Repo/` (user decisions above). Paths come from the repo (`Tools/Scripts/env.sh`, `env.ps1`; a git-ignored `env.local.sh` keeps this PC's runtime folder); the harnesses and the old game's test client (its source unpacked into `old-game/test-client`) run on Electron 44.5.0 from npm (`Tools/Scripts/chromium.sh`, Xvfb without a display); the tests act as Chromium on Linux with this PC's font settings (`Infrastructure/ReferencePlatform.cs`, `Platform/PlatformBehaviour.cs`) and take system fonts from `Data/SystemFonts` (fontconfig's answers recorded here, `Infrastructure/SystemFonts.cs`); native libraries load from the portable runtime folder (Ubuntu's .NET reports `ubuntu.24.04-x64`); the frame speed check and two opacity-text tolerances allow for a CPU Vulkan. Checked: 494 pass on the GPU and on Mesa's lavapipe; a fresh clone in a clean Ubuntu 24.04 container prepared only by `Tools/Scripts/cloud-setup.sh` builds, passes all tests, runs the old game's smoke suite and reproduces the script references byte for byte; a win-x64 build carries every native library; the PowerShell scripts parse (Windows itself untested here). Found on the way: media query lists with listeners were only weakly held, so a garbage collection could lose their change events (a flaky `resize-observer-window`); they're now kept alive as in Blink (`ScriptApiTests.ListenersKeepWhatTheyListenToAlive`).
  - **Border-style changes repaint (2026-10-30, 496 tests):** a change to border styles or widths now lays out only when a used width (0 for none and hidden) changes, otherwise it only repaints, as Blink's ComputedStyle::DiffNeedsFullLayout treats its kBorderWidth group (`StyleDifference.BorderFields`). The UA style's button :active (outset to inset) no longer lays out, so :hover isn't recomputed after it either. `IncrementalWorkTests`: outset to inset, none to hidden (also with another width) don't lay out; a width change and none to solid do.
  - **M6 step 1, 3D core (2026-10-30, 501 tests, `Docs/Renderer-3D.md`):** cameras (orthographic, perspective; reversed depth, Vulkan's Y flip), GPU meshes pulled by address, MSAA 4× sRGB targets with D32 depth and a resolve, a flat-colour pipeline. First Blender reference harness: the tests write `Reference/r3d-scenes.json`, `Tools/Scripts/make-blender-references.sh` renders it with EEVEE (`Tools/BlenderReference/render_scenes.py`) into `Golden/r3d-*.png`, and `Render3DReferenceTests` compares (interiors within 2 levels, edges by the MSAA tolerance). Three scenes match Blender, interiors exactly. The reference found a culling bug: with the projection's Y flip glTF's front faces stay counter-clockwise on screen, and culling clockwise had drawn the back faces.
  - **M6 step 2, glTF import (2026-10-30, 515 tests, `Docs/Renderer-3D.md`):** user decisions: BRUTAL's glTF package parses (asked twice: the first question missed that BRUTAL has one), and materials name palette slots. `GltfImport` decodes accessors per the specification into `ModelData` (hierarchy, meshes, materials, skins); `Palette` fills slots; `Model` draws the hierarchy on the GPU, skinned meshes posed by their joints at rest; the renderer draws double-sided materials unculled and mirrored nodes with the other winding. Blender builds the test models (`Tools/BlenderReference/make_models.py` → `Data/Models/*.glb`) and renders them through its own importer; `model-blocks` and `model-rigged` match, and `GltfImportTests` covers the models and a hand-written file with what Blender doesn't export. Blender's renders are now byte-reproducible (PNG stamps off).
  - **M6 step 3, cel shading (2026-10-30, 521 tests, `Docs/Renderer-3D.md`):** user decisions: the lizardman test's rule (two tones, shadow mixed 42% toward the ink in sRGB unless the palette gives one, band edge at 0.3 of a strength-3 sun's diffuse = N·L 0.314) and band edges antialiased like Blender's render. Recovered the lizardman script from an old session transcript (its files were deleted) to port its exact node chain. `mesh_cel.frag` (Lambert as EEVEE computes it, measured: strength/π · N·L; back faces flipped; fwidth band antialiasing), `SunLight`, `CelStyle`, palette shadow tones. The Blender reference uses the same toon chain, a camera-only world and no ray tracing. Five cel scenes match Blender, interiors exact; breaking the back-face flip, the normal matrix or the band edge by 2% fails them.
  - **M6 step 4, ink lines (2026-10-30, 530 tests, `Docs/Renderer-3D.md`):** user decisions: outline per model, 3.5/2 px times the device scale, the lizardman's inner set, `ink = 0` object property, screen space. The 3D pass writes per-sample surface records (model, part, ink, face normal, depth); `ink_edges.frag` finds outline and inner-line boundaries (ownership by depth, surface breaks by depth continuation, creases by face normals) and places them to a quarter pixel from all four samples; `ink_lines.frag` draws them centred with round ends. Freestyle references per model scene (models baked to one object each so "Contour" outlines a model; line coverage from a holdout render); `CompareInk` checks presence, extras, total ink, coverage and local width; missing creases, thin lines, lines on inkless parts and per-scene outlines each fail. Blender's model exports are made byte-reproducible (triangle order).
  - **M6 step 5, shadows (2026-10-30, 538 tests, `Docs/Renderer-3D.md`):** user decisions: scenery receives (everything casts), soft edges from a 3° sun as Blender renders them (a gradient across the fringe), no ink on shadows, maps fitted to the view, 16 maps of 1024² (two answers were asked again after I found my first description and design wrong). 16 sheared maps over the sun's disc in a 4096² atlas, bilinear lookups, the tone blended toward the shadow tone by the hidden share of the sun. Blender references with jittered sun shadows; `CompareShadowed` (geometry edges as before, fringe rules inside surfaces, normalised by the shadows' own difference); the `shadow-fringe` scene catches a sun 30% off; hard shadows, shadows on characters and none also fail.
  - **M6 step 6, hatching and glow (2026-10-30, 543 tests, `Docs/Renderer-3D.md`):** user decisions: hatching 6 px apart, 1 px, 72°, 35% ink, in all shadow areas, moving with each model, `hatch = 0` per material; glow marked by the Blender shader (Principled with emission glows, Emission alone is flat and glows, Background is flat), the lizardman's halo behind the model. Hatching in `mesh_cel.frag` (exact box-filtered line share, mixed by the shadow share); glow in four passes (sources, Pillow's extended box blur, owner, compose.py's sRGB composite behind the model). Blender hatching nodes and glow renders, `compose_glow.py` (Pillow) for the halo. Mutation checks catch every hatching and glow mistake tried.
  - **M6 step 7, GPU skinning (2026-10-30, 547 tests, `Docs/Renderer-3D.md`):** user decisions: normals recomputed like Blender, linear blend, 4 influences, and mesh feature edges for the ink lines (a gap found by the posed tests: plate edges lying on a body). `ModelPose`/`SkinJob`, skin.comp + normals.comp into a per-frame device arena, flat faces split apart (Blender keeps all-flat faces flat), feature edges drawn per frame and kept where visible. Posed Blender references from skin-independent joint deformations (Blender's posed vertices match to 3e-7). Mutation checks catch wrong normals, averaged flat faces, swapped matrices, one influence and missing feature edges.
  - **M6 step 8, terrain (2026-10-31, 555 tests, `Docs/Renderer-3D.md`):** user decisions: height and cover maps, covers blended softly over three cells (a warped boundary), shaded water with two wave trains; particles decided too (shapes in the shader, shrinking and fading), still to build. `TerrainData`/`Terrain` (32-cell chunks at strides 1, 2, 4, 8 with full-resolution borders, so neighbours share vertices), terrain.frag (warped tent blend of the covers' palette tones), water.frag (two sine trains at the scene's clock), the toon chain shared in mesh_lighting.glsl. Blender rebuilds the grid, the blend and the waves in nodes (`make_terrain.py`, `add_terrain`); scenes `terrain-map`, `terrain-oblique`, `terrain-water` and a chunk-crack test. Found: Freestyle's outline-or-not vote misfires over big triangles (the reference water is now the terrain's cells, inset 1 mm); the ink's edge-on exception swallowed terrain folds (now bounded by twice a pixel's run of depth); views past the map's open edge show the water under the land (cameras stay inside); T-junctions at level-of-detail seams left pixel gaps; the waves needed a low sun to be tested at all; centroid world positions matched Blender worse than the pixel centre.
  - **M6 step 8, particles (2026-10-31, 566 tests, `Docs/Renderer-3D.md`):** user decisions (asked in three rounds): motion computed from age, sizes in metres, per-effect proportions and ink width (centred on the edge), smoothstep growth and fading, start/end palette colours mixed in sRGB evenly, depth-tested without shadows or hatching, emitters with bursts, streams and trails from a point, ball or cone with seeded ranges. `Particles.cs`, particle shaders, a premultiplied 16-bit particle layer drawn after the ink lines against the scene's depth and laid over the view; glowing particles feed the glow. Blender lays a node-built quad per particle, rendered alone with the scene held out and laid on in linear light after the halo. Mutations of every rule fail the `particles` scene; frame-border pixels are no longer compared (EEVEE's samples reach past the frame).
  - **M6 step 9, 3D views in the UI (2026-10-31, 573 tests, `Docs/Renderer-3D.md`):** user decisions: every 3D view is a `surface` element (battle and map too), background colour or transparent per view, redraw only on change, picking now. `View3D` (renders on a set property, a moved camera, a new size or scale, or `Invalidate`; a straight-alpha sRGB picture for the canvas; picking from kept surface records), `SurfaceViews` in `UiHost.Views`, the glow composite over transparent views. Tested against the direct render, an `<img>` of the same picture inside a turned, translucent, rounded frame at scales 1 and 2, and picking through CSS transforms; mutations of each rule fail.
  - **M6 done (2026-10-31, 573 tests):** ticked. Not measured on a 2016-class GPU (this PC's RX 9070 takes 0.8-1.4 ms a 1080p frame for the test scenes).
  - **M7 started (2026-10-31):** the user's go; the eleven open questions asked and answered ("Decisions for M7"); design doc, the Blender and glTF rules to port (NLA blending, strip time, pose channels, Appendix C) and the seven steps in `Docs/Animation.md`. Blender 5.2.0's animation sources are in `~/.cache/stat-fick-build/reference-src/blender-anim/`, the glTF specification in `reference-src/gltf/`.
  - **M7 step 1, clips from glTF (2026-10-31, 584 tests, `Docs/Animation.md`):** `GltfImport` reads animations (`ModelAnimation`); `Animation/AnimationClip.cs` samples them by glTF's Appendix C (STEP, slerp the short way, CUBICSPLINE with tangents times the segment's duration, clamping), bone channels turned rest-relative at load (Blender's pose channels); `Animation/Skeleton.cs` binds clips by node name and builds `ModelPose`s. Test models `figure.glb` (a humanoid rig with idle, swing and twist actions) and `reach.glb` (an IK arm), Blender's evaluation of the actions (`Tools/BlenderReference/evaluate_animations.py` → `Golden/anim-*.json`): on frames within 0.0006 mm, between frames within the interpolation bound (h²/8 · f'' per bone in its parent's frame). Mutations of every rule fail a test.
  - **M7 step 2, clips shared by bone names (2026-10-31, 586 tests):** a stockier test model of the same rig family (`stocky.glb`: other proportions, rolled upper arms, no neck, an extra beard bone); Blender plays the figure's actions on its armature, and the engine's clips (bound by name, applied on each bone's own rest as Blender applies an action to another armature) match on frames within 0.0005 mm. Naive absolute playback by names fails the scenes.
  - **M7 step 3, layers and cross-fades (2026-10-31, 592 tests):** `Animation/AnimationStack.cs` ports Blender's NLA (strip lookup with extrapolation, linear blend ramps with Blender's limits, strip time with parts, scale, repeats and reversal, Replace and Combine on the keyed channels only); a cross-fade is a strip blending in on a new top layer. User decision: fades blend unit samples (Blender mixes raw quaternion curve values, a few degrees apart mid-fade). Four layered Blender scenes match (Replace within 0.001 mm, Combine within Blender's float32 quantum); 14 mutations of the rules each fail a scene.
  - **M7 step 4, events (2026-10-31, 609 tests):** user decisions: every acting strip fires its markers, queued like CSS animation events (awaitable), names only. Pose markers exported as animation extras (`events`), read into clips; `AnimationStack.CollectEvents`, `Animator` (clock, rate, seek, `Event`, `Next(name)`), `IUiFrameClient` run by `UiDocument` (advanced after the CSS timing update, dispatched after the CSS animation events, before frame callbacks). Blender's markers checked against the clips; every firing rule mutation-checked.
  - **M7 step 5, state machines (2026-10-31, 622 tests):** user decisions recorded above ("State machines in detail", "Clips during state machine fades"). `FadeLayer` (Blender's NLA transition rule applied to fading entries), `StateMachineDefinition` + `StateMachineBuilder` (data records), `StateMachine` (parameters, transitions by exit time or conditions, triggers, any-state priority, restarts, speed parameters, interrupted fades, entered/left notices) and the `Animator` delivering notices with clip events. The mixing rule matches Blender's transition strips (made headless through the NLA editor's operator); 15 mutations each fail a test.
  - **M7 step 6, gear (2026-10-31, 631 tests):** user decisions ("Gear in detail"): `socket.<slot>` bones, clothing as Blender's Armature modifier deforms it, items with their own clips played with the wearer's states. `Socket` (C⁻¹ then the socket's world: the exporter converts objects' axes, not bones'), `Garment`, `ItemAnimation`, `Model.Draw` world and anchor parameters; gear is one model with its wearer (owner, hatch anchor). Blender references: item placement and garment vertices on the figure and the stocky figure, and a rendered geared figure; mutations of each rule fail a test.
  - **M7 step 7, animated views (2026-10-31, 635 tests):** `Actor` (a model, its animator, transform, palette, items and garments; refreshed and drawn as one model) and `AnimatedView` (a frame client advancing its actors on the document's clock at its rate, giving its `View3D` new items only when something changed, dispatching their events). The picture matches a direct render of the pose at the clock's time; the clock matches an element animation's. Cost on this PC: 45-75 µs of animation work per geared figure per frame, a 1080p frame of 24 figures 1.5 ms on the GPU.
  - **M7 done (2026-10-31, 635 tests):** ticked. Fifteen user decisions in all (the eleven up front, then fades of rotations, clip events, state machine details and fades, gear), each rule checked against Blender 5.2 where Blender defines it (clips, retargeting, NLA layers and transitions, markers, sockets, garments, renders) and mutation-checked.
  - **M8 started (2026-10-31):** the user's go; the questions asked and answered in two rounds plus one found on the way (the old score engine, dropped) ("Decisions for M8"); design doc `Docs/Audio.md`. Chromium 152's Web Audio sources (Blink's `modules/webaudio`, `platform/audio`, the x86 vector math, pffft) are in `~/.cache/stat-fick-build/reference-src/chromium-152-source/webaudio/`.
  - **M8 step 1, harness and render core (2026-10-31, 681 tests):** `Tools/AudioReference` renders fixtures in Chromium 152's OfflineAudioContext (planar float32, gzipped, in `Golden/audio`); `Engine/Statventure/Audio/` ports Blink's graph (128-frame quanta, pulling, silence, channel mixing), AudioParam's automation timeline with its x86 SSE paths, gain and buffer sources, offline rendering, and fdlibm's exp and pow (checked bit for bit against Blink's own fdlibm built natively, `Tools/NativeReference`, so named since step 2). 39 fixtures match Chromium bit for bit, except two multi-input mixes within a summing-order bound: Chromium's summing order changes from run to run. 51 of 62 mutations fail a test; the rest change nothing a render shows (Docs/Audio.md, Status).
  - **M8 step 2, oscillators and periodic waves (2026-10-31, 707 tests):** user decision on the way: Chromium 152's Web Audio FFT is RustFFT 6.4.1 (the `WebAudioRustFft` feature is stable), ported as its AVX/FMA path runs on this PC (`Audio/RustFft.cs`: an emulated AVX register, Butterfly128/256/512Avx, MixedRadix4/8xn, Blink's real-FFT packing); glibc 2.43's exp2f and log2f (`Audio/Libm.cs`, checked on all 2³² floats; `Tools/NativeReference`, formerly FdlibmReference, also hashes them); `PeriodicWave` and `OscillatorNode` with Blink's SSE2 kernels. 18 fixtures match Chromium bit for bit, among them the wave tables read out directly at three rates (each FFT plan). 31 of 36 mutations fail a test; the rest are exact identities.
  - **M8 step 3, biquad filters (2026-11-01, 733 tests):** `Audio/Biquad.cs` ports Blink's Biquad (all eight types, fdlibm sin/cos/expf coefficients, per-frame coefficients, the tail analysis deciding when a silent filter stops) and BiquadFilterHandler (in-place pull, change detection, resets on type and channel changes), with Chromium's denormal flush (FTZ/DAZ) mirrored in the recursion; fdlibm sin, cos, log, expf added (checked natively). 23 fixtures match Chromium bit for bit; 24 of 31 mutations fail a test, the rest are below float precision or optimizations. Found: Chromium's run-to-run summing order, carried through low filters, reaches 2·10⁻⁵ (the theme comparisons need a bound from Chromium's own spread).
  - **M8 step 4, convolver and stereo panner (2026-11-01, 742 tests):** user decision: the offline partitioning of the convolver always. `Audio/Convolver.cs` (Reverb with its stages, FFT and direct convolvers, normalization with Vsvesq's AVX summing order), `Audio/StereoPanner.cs`, Butterfly64Avx; and Blink's connection counting with disabled outputs and the tail-processing list in `AudioGraph.cs` (it changes the sound: a stereo room rings on in mono once its stereo source has finished). 12 fixtures match Chromium bit for bit; 19 of 23 mutations fail a test, the rest change no sample.
  - **M8 step 5, dynamics compressor (2026-11-01, 750 tests):** `Audio/DynamicsCompressor.cs` ports Blink's compressor (look-ahead, adaptive release, soft knee, makeup gain, metering); glibc 2.43's powf and log10f added to `Audio/Libm.cs` (checked on all 2³² floats), fdlibm's asin; the C library's exp and sin there are replaced by fdlibm's, which give the same floats on every float input (checked natively). 5 fixtures match Chromium (one within the summing-order bound); 15 of 18 mutations fail a test.
  - **M8 step 6, the game synthesizer (2026-11-01, 974 tests):** user decisions recorded above ("Step 6's questions"; pow, fixture sums, the order count and the full themes' rule re-asked with data). `Audio/Music/`: `ThemeData.cs` (records), `MusicData.g.cs` (generated by `make-music-data.sh` from the old JavaScript in Chromium: 55 themes, 34 instruments, 21 percussion voices, 7 rooms), `Score.cs` (compile, lint), `GraphBuilder.cs` (every Web Audio call of the composer, optionally logged; the per-context kit), `Composer.cs` (`Performance`: rooms, channels, every instrument kind, percussion, textures, the sequencer), `Soundtrack.cs` (public: music.js's master chain, `Play(id)` with its fades, `Stop`, `Volume`, `Schedule` per quantum, `RenderOffline`); `Audio/JsMath.cs`, `Audio/LlvmLibc.cs` (V8's `Math.exp`: LLVM libc's correctly rounded exp with its double-double and 128-bit passes), `Audio/Libm.Pow.cs` (glibc 2.43's pow as its FMA variant runs, the fused steps read from libm.so.6's machine code). Checked: math hashes against the system pow and Electron's V8 (`make-v8-math-reference.sh`); every theme's compiled score, lint and whole performance (2.7 million calls) equal to the old composer's on a recording stand-in context (`make-music-references.sh`); 30 rendered fixtures (12 exact, 18 by the shuffled-sums rule: a seeded test hook sums node inputs in shuffled orders, 16 orders); all 55 full themes opt-in (`make-theme-references.sh` → 2.6 GB in the build cache, `test-themes.sh`) with both sides' summing orders sampled. 39 of 47 mutations fail a test, the rest are equivalent, speed-only or below observable precision.
  - **M8 step 7, the output (2026-11-02, 990 tests):** user decisions recorded above ("Step 7's questions"; theme starts asked after measuring). `Audio/AudioSystem.cs` (public, `Application.Audio`: the graph at the device's rate, master and effects buses, the soundtrack, the audio thread, game calls queued and applied before the next quantum, paused while the window is minimized), `Audio/Output/AudioOutput.cs` (OpenAL Soft: 4 queued buffers of about 5 ms, direct channels, HRTF and limiter off; follows the default device, renders silently at the real-time pace without one); rooms' responses and convolver kernels prepared at start by a worker and shared (`Reverb.Prepared`, `PreparedRoom`), a warm-up render for the JIT, the FFT's scratch arrays from a pool. Measured by `Tools/AudioBenchmark` (`audio-benchmark.sh`): 71 minutes of music live at 48 kHz, mean 0.23 ms of the 2.67 ms budget, 99.9th percentile 2.08 ms, worst 12.9 ms; no underruns in repeated runs on this PC's headset with theme switches. Checked: a loopback device gives samples back bit for bit; the buses' fixture matches Chromium; the opt-in themes still pass (55 of 55); 17 of 19 mutations fail a test, the other 2 change no defined behaviour. Not checked automatically: moving to a really different device, a device lost or appearing, a real window minimize.
  - **M8 step 8, sound effects (2026-11-02, 1013 tests):** user decisions recorded above ("Step 8's questions"). `Audio/Effects/`: `EffectPatch` records (layers of tones and noise, filters, envelopes as the composer's percussion, optional ranges, voice limits), `SoundEffects` (`Register`, `Play`, `Seed`; voices dry on the effects bus, the oldest stolen with a 5 ms fade past a patch's limit or 32 overall, cleaned up at the end of their last quantum), `ActorSounds` (`Actor.Sounds`: clip event name → effect). 12 fixtures against Chromium, unit tests, and 32 mutations that all fail a test; 32 voices add 0.13 ms to a quantum's mean. **M8 done (all 8 steps).**
  - **M9 started (2026-11-02):** the user's go; the thirteen questions asked and answered, all with the recommended options ("Decisions for M9"); design doc `Docs/Assets.md`: the present loaders and data checked against the code (three details of the plan's survey corrected), the method, a proposed on-disk design (packs, records in binary and JSON, the virtual file system, the game folder with saves, settings and character files, the importers), nine proposed steps with the files each touches and which can run side by side, and 27 open questions with recommendations. The 27 were asked the same evening and answered, all as recommended except named save slots (Q15) and asking the player about a damaged save (Q17) ("Decisions for M9", "Format details"; `Docs/Assets.md` updated to match). No code yet.
  - **Left for the port, not M5:** the game listens for `visibilitychange` and `pagehide` (map.js pauses travel); the window host doesn't send them yet. (Done 2026-11-03, see "Small tasks" below.)
  - **Small tasks (2026-11-03, 1046 tests, a cloud session):** the user's go on the small tasks left from earlier milestones.
    *Hidden documents* (Docs/Scripting.md, "Hidden documents"; user decisions "Decisions for the window's visibility"):
    Chromium 152 probed first (minimize under openbox, hide, close, 7-minute hidden captures), then Blink's scheduler read
    (fetched from GitHub's Chromium mirror: googlesource isn't allowed in the cloud). `UiDocument.Visibility.cs`
    (`Hidden`, `VisibilityState`, `SetHidden` with visibilitychange and webkitvisibilitychange, `Audible`, `Unload` with
    pagehide, visibilitychange and unload, `NextWakeUp`, `FirstContentfulPaint`; no frames while hidden),
    `UiTimerThrottling.cs` (timer kinds by nesting, the hidden and intensive wake-up pools, the grace period of 60 s after a
    contentful paint else 5 minutes, the CPU-time budget, 30 s of recent audio), `UiEventLoop` (throttled timers, a late
    interval skipping missed beats as TimerBase does), `Window` (GLFW's iconify as `Minimized`/`Restored` input,
    `IsMinimized` from it, `RequestWakeUp`, `WaitEvents(seconds)`, `PostEmptyEvent`), `Application` (keeps updating while
    minimized, without drawing, sleeping until the next wake-up), `UiHost` (visibility from input at the update's time,
    `Audio` → `Document.Audible`, wake-up requests, `Unload`, unloading on `Dispose`), the painter reporting contentful
    paints, `Audio/Output/Audibility.cs` (Chromium's power monitor and audibility polling) with `AudioSystem.Audible`. Found
    on the way: the window's own focus and blur were FocusEvents; Chromium sends plain Events (fixed). New harness
    `Tools/VisibilityReference` (`make-visibility-references.sh`, about six minutes): `events`, `timers`, `cpu`, `audible`
    → `Reference/visibility-results.json`; `VisibilityReferenceTests` (4), `HiddenDocumentTests` (23),
    `AudibilityTests` (6). Checked in a real window too (Xvfb with openbox, the sandbox's new `--log-visibility`), which
    found that showing must happen at the window system's moment, not the hidden document's last wake-up (a late interval
    replayed its missed beats; fixed and tested). Mutations: see Docs/Notes.md, 2026-11-03. Not checked: Wayland (GLFW
    can't tell), Windows, the GPU (this was a cloud session: lavapipe only).
    *Housekeeping:* `.gitignore` (worker worktrees, local settings), the last stale `Stat Fick Source` paths in Notes.md.
    *Windows:* every project of the solution (engine, sandbox, tests, shader compiler, UI and audio benchmarks, PlayTheme)
    builds for `win-x64` from Linux with 0 errors (per project: a solution can't take a RuntimeIdentifier), the Windows
    natives copied; running there still needs the user's Windows PC.
  - **FreeType thread safety (2026-11-03, 1668 tests, +10, a worker worktree):** the cause of the flaky
    `CompositingReferenceTests(map, 1.65)` (seen once in step 3's full GPU run). The layer analysis loads glyph outlines for
    underlines that skip ink (`GlyphOutlines`; of the captured screens only the map's), and the tests' shared fonts were used
    by several threads at once while FreeType allows one thread at a time per face: a `NullReferenceException` reading an
    outline another thread's glyph load was replacing. Reproduced without forcing on the unfixed build (5 of 48 runs of the
    four screen test classes on a busy machine: four times that test, once `ScaledGameTests(passives)` from the other side
    of the race), the fixed build 40 of 40 alongside; forced, 25 of 60 against 0 of 200. Fix: `FontFace.FreeTypeLock`,
    held by every use of a face's FreeType state (`GlyphOutlines`, `SkiaGlyphs`, `GlyphRasterizer`, `AutohintFont`). The
    user's decision (2026-11-03, "Decision on FreeType's library-level gaps" above): the two library-level gaps found on the
    way were fixed too: `FontLibrary.FreeTypeLock` serializes making and destroying a library's faces and setting and using
    its LCD filter, and each library records its own filter (it was a process-wide static). Found: the FreeType the engine
    loads is built without subpixel rendering, so `FT_Library_SetLcdFilter` changes nothing (its test checks the filter each
    library was given). `Text/FreeTypeThreadingTests` (10). The full suite (1668) passes on the GPU and on lavapipe.
    Mutations: 10 (`mutate.py`, `~/.cache/stat-fick-build/claude-work/flaky-compositing/mutations.json`: each of the nine
    locks removed, and the filter made process-wide again), 10 killed.
    Details: Docs/Notes.md, 2026-11-03; Docs/Compositing-Port.md, "Threads".
  - **OpenAL outputs in one process (2026-11-05, 2317 tests, +4, a worker worktree):** the cause of the flaky
    `OutputTests.TheDefaultDevicePacesTheGraph` (seen once in M10 step 6's full GPU run, a Blender render beside it).
    `OpenAlOutput` made its context OpenAL's process-wide current one, and its AL calls went to whichever context was
    current (a source is a name within its context, a buffer within its device, the first ones the same numbers in
    each): a second output in the process took over the first's calls, and once closed left it none, so the first
    graph stopped for good with no underrun or error logged. In the full suite's parallel phase the only other output
    is `ApplicationMusicTests`' application, so the pacing test failed only when the two overlapped, at its 30 s wait
    (`OutputTests.cs:321`). Reproduced with the unchanged tests started together: 10 of 10 failed there; load alone
    didn't (100 of 100 probe runs under 16 busy loops on the 8 cores: a device-paced graph only falls behind; load only
    made the overlap likelier). Fix: `OpenAlOutput.Use()`, the output's context set on the calling thread for each
    method's AL calls and the thread's own put back (`ALC_EXT_thread_local_context`); test
    `EachOutputPlaysItsOwnStream` (two loopback outputs; fails on the old code). The user's two follow-ups, the same day
    ("Decision on the audio output's follow-ups"): after an underrun the output goes on with new audio
    (`TakeBackAfterUnderrun`: the played buffers taken back when the source is first seen stopped, before each write,
    the restart and resuming; they were played again from the queue's head; test `AfterAnUnderrunTheOutputGoesOnWithNewAudio`),
    and the default-device events shared (`DefaultDeviceEvents`: one registration for every live output, off with the
    last; each output had registered its own, so only the last made heard a change and the first disposed turned them
    off; tests `EveryOutputHearsTheDefaultDeviceChange`, `AnOutputFollowsTheDefaultDeviceUntilItIsDisposed`; the pacing
    test reopens through the callback OpenAL holds). Each new test fails with the old behaviour put back. After the fix:
    the two tests started together 20 of 20 unloaded and 30 of 30 under 16 busy loops; the full suite (2317) passes on
    the GPU and on lavapipe. Mutations: 30 (`mutate.py`, `~/.cache/stat-fick-build/claude-work/flaky-pacing/mutations.json`),
    28 killed; the 2 survivors can't differ (the context scope in `Dispose`, whose calls the device's closing right
    after makes moot; the guard on a refused thread context, unreachable). The opt-in theme check wasn't run: it renders
    offline through the synthesizer, and only the output changed. Details: Docs/Notes.md, 2026-11-05; Docs/Audio.md,
    "The output" and Status.
  - **Canvas: box antialiasing under non-uniform transforms (2026-11-05, 2380 tests, +61, a worker worktree):** the user's decision
    ("Decision on box antialiasing under non-uniform transforms"). The bug: `boxCoverage` turned a box's distance into pixels
    with one scale for the whole box, `sqrt(|det|)` of `dFdx(p)`/`dFdy(p)`, and chose the nearest edge in local units, so
    under `scaleX(s)` the edges across the unscaled axis ramped over `1/sqrt(s)` px (a 2 px bar under `scaleX(0.1)`: six
    rows, its core 66 % covered, where Chromium paints two full rows). Ground truth: two new paint fixtures in Chromium 152
    (`Tests/.../Ui/TransformPaintReferenceTests.cs`, list `Reference/transform-fixtures.json`, `Golden/ui-box-{scale,slant}@{1,1.65}.png`,
    made by `make-scaled-paint-references.sh 1 1 transform-fixtures.json` and `1.65 1 ...`; two captures identical):
    `box-scale` (2 px and 1 px bars under scaleX/scaleY 0.1–3 from the end and about the centre, mirrored; circular and
    elliptical radii, borders, a radius with a border, crisp shadows and the game's meter look under one-axis scales; the same
    boxes untransformed and uniformly scaled) and `box-slant` (a rotation after a one-axis scale, a scale after a rotation,
    skews, a rounded box and bordered boxes so, beside a uniformly scaled rotation). The rule (ported from the geometry: Skia
    fills an axis-aligned rect by exact area, which a one-pixel ramp across each edge reproduces): every distance is taken to
    device pixels before distances are compared: across an edge x = c local x changes by the length of its gradient in
    pixels (|(dFdx(p).x, dFdy(p).x)|), so the edge is (x − c)/|∇x| px away, likewise y, the nearest edge the largest in
    pixels; beyond a square corner the local way to the corner point carried to pixels whole (the derivatives' inverse); in
    an elliptical corner its distance over its gradient's length in pixels (the gradient m(e/r)/k, along the ellipse's
    normal). Only `canvas_fragment.glsl` changed (`cornerRadii` shared with the clips' `boxDistance`, which is unchanged); no
    vertex data. Against Chromium, pixels beyond 12 levels, before → after (GPU; lavapipe within 1 level of it):
    `box-scale@1` 4183 (1.62 %, worst 77) → 315 (0.12 %, worst 56); `@1.65` 6708 (0.95 %) → 790 (0.11 %, worst 56); its bars
    1336 + 540 → 0 at 1× and 2235 + 882 → 0 at 1.65 (worst 1–2 levels); `box-slant@1` 1281 (0.63 %, worst 91) → 823 (0.40 %);
    `@1.65` 2049 (0.37 %) → 1176 (0.21 %). What `box-scale` keeps is not this rule: the rounded corners' known difference (the
    untransformed control box alike), Blink's shadow clip inset and the background layers' double edge below. On slanted
    edges Chromium's CPU raster fills the box as a path (Skia's analytic scan converter: area coverage, the points snapped to a
    quarter pixel down the screen), the canvas by the ramp: the uniformly scaled rotation, which this rule doesn't touch,
    keeps 49 pixels beyond 12 at 1× (worst 18) and 124 at 1.65 (worst 27). So `box-scale` is held at the paint fixtures'
    12 levels / 0.2 % (fails before at both scales), `box-slant` at `CanvasReferenceTests`' rotated-box 12 / 1 % (passes
    before too: a guard against a misplaced or misshapen box), and the rule itself on every transform by
    `Tests/.../Canvas/BoxTransformTests.cs`: each pixel against the box's distance on the target (48 cases: 12 transforms ×
    1, 1.65 × square and elliptical corners; every pixel, a rounded corner's by its ellipse's first-order distance with the
    gradient carried to pixels by the transform itself; within 2 levels) and a scale before a rotation drawn like the
    box scaled in size, radii and border instead and only rotated (8 cases, within 2 levels). On the old shader 44 + 8 of
    them fail (the 4 uniform cases pass), and `box-scale` at both scales; all pass after, on the GPU and lavapipe. Existing
    references: the filtered suite (`Canvas|Paint|GamePaint|Scaled|Compositing|TextHinting|View3D|HiddenDocument|UiResources`,
    364 tests with the new ones) passes with the tolerances unchanged; its 98 kept renders compared with the old shader's: GPU 79 identical,
    19 differ by one level in 1–69 pixels each (largest 0.0018 %, `game-settings@1.65`); lavapipe 93 identical, 5 by one level
    in 1–3 pixels. Not identical to the bit because the old rule averaged both axes' derivative noise (`sqrt(dx.x·dy.y)`) where
    the new one takes each edge's own (`|dx.x|`, `|dy.y|`); the derivatives of interpolated coordinates carry float noise far
    from the origin. Found on the way, and the user's answers the same day ("Decision on box antialiasing under non-uniform
    transforms"): to be ported after this fix is merged, each its own step with a Chromium fixture (the first since ported,
    wider than first thought: Progress, "Paint offset of transformed boxes"): Blink rounds a
    transformed box's paint offset and keeps its fraction only along the axes the transform doesn't scale
    (`PaintPropertyTreeBuilder::UpdateForPaintOffsetTranslation`, `CanPropagateSubpixelAccumulation`), the painter
    transforms about the unrounded origin (at 1.65 a box at 250 CSS px under `scale(0.8)` ends a device pixel short; the
    fixtures put transformed boxes on multiples of 20 CSS px to keep it out); a background image layer is clipped with
    antialiasing and its tiles drawn with it too, so a fractional edge gets coverage squared (0.4 → 0.16; the meter's
    gradient under `scaleX`). Recorded as known differences (M5's "Known differences left"): slanted box edges (Skia's path
    coverage against the ramp); Blink insets an opaque box's shadow clip by one pixel (`ClipToBorderEdge`), the canvas cuts
    the shadow at the box's edge (a fractional edge shows the shadow at half); Blink's background bleed avoidance under an
    opaque rounded border isn't done (the fill shows in the border's outer edge pixels); Skia's area coverage at a square
    corner is the product of the two edges' (the canvas: the distance to the corner point); and, not checked, a blurred
    shadow under a non-uniform scale (Skia blurs with one sigma, `mapRadius`; the canvas blurs in the box's units).
    `Tools/Scripts/mutate.py` compiles the shaders (build.sh's step, forced) before building when a mutation's file is a
    shader source, and again once it is put back: it built only the test project, which leaves the embedded SPIR-V as it was,
    so a shader mutation ran the unmutated shader and read as a survivor (checked: a syntax error is "BUILD FAILED", the
    isotropic rule changes `canvas.frag.spv`, the restored source gives the same bytes back). The full suite (2380) passes on the GPU
    (1 min 16 s) and on lavapipe (1 min 26 s), run again with the final tests. Mutations: 10 (`mutate.py`,
    `~/.cache/stat-fick-build/claude-work/canvas-aa/mutations.json`: the isotropic scale back, the nearer edge chosen in local
    units, an edge's scale from the columns or from the axis' own derivative, three for the square corner, three for the
    elliptical corner's gradient), 10 killed. The first run left one survivor, the elliptical corner's gradient without its
    quadrant's signs: a scale before a rotation leaves the box's units a diagonal metric, where the signs can't matter, and the
    distance test then left rounded corners out; it now checks them (elliptical radii, so a circle's radius and an ellipse's
    normal part ways) and the shears kill it. Details: Docs/Notes.md, 2026-11-05 (canvas).
  - **Paint offset of transformed boxes (2026-11-05, 2435 tests, +12, a worker worktree):** the user's decision ("Decision on
    the paint offset of transformed boxes", option (a)). Blink's rule (`paint_property_tree_builder.cc`,
    `UpdateForPaintOffsetTranslation`, `CanPropagateSubpixelAccumulation`; `ElementAnimations::IsIdentityOrTranslation`,
    `KeyframeEffect::IsIdentityOrTranslation`; in `reference-src/chromium-152-source/offset/`): a box with a transform
    (`transform`, `translate`/`rotate`/`scale`, or a current animation of them; `will-change` alone makes none) gets a paint offset
    translation, its offset in the space around it rounded half up (`LayoutUnit::Round`: -40.5 to -40), and keeps the fraction
    left (the subpixel accumulation) along both axes for no transform or a translation, along the axes with scale 1 for a scale
    and translation, along none for anything else; along none under `will-change: transform`, `scale` or `rotate`; under a
    current animation or transition of `transform`, `rotate` or `scale`, along both if every keyframe of its transform animations
    is an identity or a translation, else none. The transform's origin is measured from the rounded place, the box and all
    inside it are snapped from there, and hit testing follows (Blink hit tests through the paint property trees:
    `PaintLayer::CreateLocalTransformState`); DOM geometry (getBoundingClientRect) stays layout's. Ported in the painter as a
    shift (`UiPainter._shift`): `PushTransformOf`/`RoundedTransform` move a transformed box and its contents by the dropped
    fraction (to the rounded place) and push its transform about the origin from there, `SubpixelAxes` is the which-fraction
    rule, and every position the painter reads from layout goes through `Placed`/`PlacedAt` or a shift argument of the static
    helpers it shares with input (`FieldGeometry.Of`/`EditorRects`, `RangeThumb`, `SpinButtonOf`, `CancelButtonOf`; input passes
    none), so the existing snapping measures from the rounded origin; an inline SVG's hit region is recorded at layout's place
    with the shift in its transform, since `HitSvg` (called by the document later) measures from layout's place (UiPainter.cs,
    .Content, .Controls, .Outlines, .Scrollbars, .Svg, FieldGeometry.cs); `AnimationEngine.TransformAnimationsAre` (accepted
    outside the painter) answers the keyframe question (CSS animations' tracks, transitions, script animations). Glyph
    placement stays in device space. Not touched: the compositing property trees (their geometry stays layout's; a fraction of
    a pixel). Tests (`Tests/.../Ui/TransformOffsetReferenceTests.cs`, list `Reference/offset-fixtures.json`,
    `Golden/ui-offset-{boxes,measured}@{1,1.65}.png`, made by `make-scaled-paint-references.sh 1 1 offset-fixtures.json` and
    `1.65 1 ...`, two captures identical): `offset-boxes` against Chromium's pixels at the paint fixtures' 12 levels / 0.2 %
    (cards with a border, a positioned chip and text under `scale(0.8)`, `scaleX`, `scaleY`, a translation, a scaled card in a
    scaled box, the game's meters and the boss intro's bar inside boxes at fractional places, an untransformed card, a scaled
    card at -40.5 px); `offset-measured` by geometry, the left and top edges of each card and its chip measured as summed
    coverage between two colours, within 0.1 px of Chromium's (the user approved 0.1 px on 2026-11-05) (the composited cards,
    `will-change` on a scale and on a translation, a scale animation, a translation animation and one from a translation to a
    scale paused on the translation, whose pixels differ by cc's raster; a rotated card, whose slanted edges differ by Skia's
    area coverage; the card at -40.5 px): measured, everything within 0.01 px but the `will-change` scale's chip, 0.073–0.076 px
    (cc rasters that layer itself); a rule broken puts an edge 0.3–1 px off. Engine only:
    `AScaledBoxPaintsTheSameFromPlacesThatRoundAlike`, a card with every kind of content (decorations and shadow, text and its
    decorations, a disclosure marker, a checkbox, a range, a select, number, search and text fields with the number field's spin
    button or the search field's cancel button and the caret shown, one focused then the other, an inline SVG, a clipped box, a
    scroller's scrollbars, an outline) under `scale(0.8)` at 19.72, 19.9, 20.1 and 20.28 px, which round alike at 1 and 1.65 and
    span most of the pixel (so a position snapped from layout's place crosses a half pixel between two of them whatever its
    fraction), every pixel equal; `AScaledBoxHitTestsTheSameFromPlacesThatRoundAlike`, the same card hit-tested on a half-pixel
    grid from three of those places, every point the same element (its SVG's shapes included); and
    `ABoxInATransitionToAScaleDropsTheFraction` (Chromium's harness can't hold a transition), a box at the start of a transform
    transition from a translation to a scale painting the same from places that round alike. Before the port (the painter on
    `main`): `offset-boxes` 1725 pixels past 12 levels at 1× (1.04 %, worst 189) and 2509 at 1.65 (0.55 %); 10 measured edges
    past 0.1 px at 1× (the cards' own edges 0.1 px, their chips 0.9–1 px) and 1 at 1.65; the rich card thousands of pixels apart
    (5510 and 14100 between 20.1 and 20.25 px); after: 47 and 43 pixels (square-corner coverage, a known difference), every
    measured edge within 0.08 px, the rich card equal and hit alike. Existing references: the filtered suite
    (`Canvas|Paint|GamePaint|Scaled|Compositing|TextHinting|View3D|HiddenDocument|UiResources|JiruanCard|GameHit|Transform`) passes
    with the tolerances unchanged and its 107 kept renders (the cutscene card's five passing moments among them) are identical to
    the painter's before the port, on the GPU and on lavapipe. The full suite (2435 passed, 1 held: the cutscene card at 1.4 s)
    passes on the GPU (1 min 16 s) and on lavapipe (1 min 39 s). Mutations: 29 (`mutate.py`, `~/.cache/stat-fick-build/claude-work/canvas-aa/offset-mutations.json`: the rule kept or dropped everywhere, its rounding floored or away from zero, the origin from the unrounded place, the contents not moved, the move not undone, the `will-change` rule, the animation rule by the value shown, always, never, without transitions or by the first keyframe, the scale's axes swapped or both kept, and every routed read at layout's place: decorations, overflow clips, text lines, the disclosure marker, replaced content, scrollbars, outlines, the range thumb, a select's text, the spin and cancel buttons, field text, the caret, the SVG's hit region), 29 killed. The first run, with the rich card at two places 0.15 px apart, left three survivors (scrollbars, spin button, an SVG content read used only for DOM geometry): the places now span most of the pixel, and the DOM-geometry read takes layout's place outright.
    Details: Docs/Notes.md, 2026-11-05 (paint offset).
  - **Canvas: blurred shadows under non-uniform transforms (2026-11-05, 2516 tests and 6 held, +81, a worker worktree, rebased on `0731233`):** the user's
    decision ("Decision on blurred shadows under non-uniform transforms"). The rule, from the source: Blink paints a box
    shadow (`BoxPainterBase::PaintNormalBoxShadow`, `PaintInsetBoxShadow`) with a draw looper whose blur respects the
    transform (`DrawLooperBuilder::kShadowRespectsTransforms`, sigma = blur / 2, `ShadowData::BlurAsSigma`), which cc applies
    as `SkMaskFilter::MakeBlur(kNormal, sigma, respectCTM = true)` (`cc::DrawLooper::Layer::Apply`); Chromium's CPU raster
    (SkBitmapDevice, the mask filter not turned into an image filter there) blurs the shape's device mask with
    `SkBlurMaskFilterImpl::computeXformedSigma` = min(`SkMatrix::mapRadius`(sigma), 128), the geometric mean of the lengths the
    matrix gives a step along x and along y: one sigma on the device, the same in every direction (`SkDraw::drawPath`,
    `SkMaskFilterBase::filterPath`, `SkBlurMask::BlurRect`/`BoxBlur`). The canvas blurred in the box's units instead (σ 6 across
    the card's bar under `scaleX(0.2)`, Chromium's 2.68). Now `canvas_common.glsl`'s `shadowBlur` gives that device blur in the
    box's units as a Gaussian of covariance device² (M⁻¹)(M⁻¹)ᵀ (M the columns x0, x1): sigma x = device |x1| / |det|, sigma y =
    device |x0| / |det|, correlation −(x0 · x1) / (|x0| |x1|) (zero unless the transform shears the box's axes), and
    `blurredBox` (Wallace's closed form) integrates each sampled row across x with the x blur given that row (mean moved by
    correlation × σx/σy × the row's offset, sigma by √(1 − correlation²)); `canvas.vert` reaches 3 sigma along each axis. Ground
    truth: `Tests/.../Ui/ShadowTransformPaintReferenceTests.cs`, list `Reference/shadow-fixtures.json`, `Golden/ui-shadow-*@{1,1.65}.png`
    (`make-scaled-paint-references.sh 1 1 shadow-fixtures.json` and `1.65 1 ...`; only this list captured): `shadow-scale-x` and
    `shadow-scale-y` (outer, offset and spread, inset, inset offset and spread, rounded, around a box with no background, blur
    12, under none, `scale(0.6)`, scaleX/scaleY 0.2, 0.6 and 2), `shadow-blurs` (blur 4 and 24, outer with and without spread
    and offset, inset), `shadow-rotate` (a uniform rotation, `rotate(30deg) scaleX(0.4)`, `scale(2, 0.5) rotate(30deg)`,
    `skewX(25deg)`), `shadow-card` (the card's track and bar, `scaleX(0.2)` and 0.6) and `shadow-card-animated` (the same under
    the card's CSS animation, paused). Static and animated: the glow identical; at 1× the animated bar's two ends softer (the
    composited layer resampled: x = 83, 84 give 65, 161 where the static bar gives 29, 197, as the card at 1.4 s shows), at 1.65
    no pixel differs. Pixels beyond 12 levels, before → after (GPU; lavapipe within a few pixels): `shadow-blurs` 4.53 % /
    4.59 % (1× / 1.65) → 0.09 % / 0.15 %; `shadow-card` 0.86 % / 0.83 % → 0 / 0 (worst 7 / 6 levels; before the rebase on the
    paint offset's rounding, 12 pixels at the bar's left end at 1.65); `shadow-card-animated` 0.86 % / 0.83 % → 0.003 % / 0; `shadow-scale-x` 5.9 % / 6.1 % →
    0.33 % / 0.26 %; `shadow-scale-y` 5.8 % / 6.0 % → 0.33 % / 0.26 %; `shadow-rotate` 3.4 % / 3.3 % → 0.59 % / 0.47 % (1.48 % /
    1.35 % with the axes' blurs taken as independent: the shear's corners). The cutscene card (`JiruanCardTests`, Chromium's
    `cutscene-card-*.png`): 1.4 s 2,133 → 60 (0.007 %; 56 of them the overture's italic glyph, 4 the bar's composited left end,
    worst 46), so it is un-held and passes at its 12 levels / 0.2 %; 2.5 s 1,226 → 61; 3.4 s 6 → 4; 4.6 s 25 → 25. Held (the
    user's answers the same day, "Decision on blurred shadows under non-uniform transforms": (2) is ported as the next step,
    which un-holds them, (1) and (3) are known differences): `shadow-scale-x`, `shadow-scale-y` and `shadow-rotate`, for three
    more rules: (1) Blink's
    `PaintPropertyTreeBuilder` gives a box whose transform is a 2D scale (`NeedsEffectFor2DScaleTransform`: not
    will-change, not a running compositor animation, no backdrop filter around, not preserve-3d, not replaced) an effect node of
    opacity 1, which `PaintChunksToCcLayer` emits as a `SaveLayerAlphaOp` bounded by its chunks' drawable bounds in the box's
    units, so a shadow blurred past its own ink overflow on the device (σ√s against 3σ·s along a shrunk axis) is cut at that
    rect rounded out to whole pixels (seen in cc's display item trace; a rotation after the scale makes no node and no cut; the
    card's bar ends cut 3.6 px out, by 1–3 levels); the painter makes no such node; (2) `AreaCastingShadowInHole`: the inset
    shadow's caster is the padding box outset by the blur radius (and the spread's size if negative) with its offset copy,
    minus the hole, so along an axis the transform shrinks the frame is thinner than the device blur (Chromium lighter by up
    to 15 levels inside a `scaleX(2)` box's long edges); measured as an experiment (box × (blur(frame rect) − blur(hole))):
    `shadow-scale-x` 0.33 → 0.17 %, `shadow-scale-y` 0.33 → 0.21 %, `shadow-rotate` 0.59 → 0.35 %, `shadow-blurs` 0.09 →
    0.02 %, and existing references move by at most 4 levels (`css-shadows`, `scale2-shadows`; the game screens 1–3);
    (3) `SkBlurMask::BlurRect` rounds the blurred rectangle's edges to whole pixels and the nine-patch stretches its middle,
    so a fractional edge's glow sits up to half a pixel off (an asymmetric glow around a box whose spread or offset scales to a
    fraction). Tests: `Tests/.../Canvas/ShadowTransformTests.cs`: each pixel (every other one each way) within the shadow's reach
    against the shape blurred on the target in the target's own axes (its rows' extents integrated across x in closed form,
    weighted down y piece by piece between the corners' heights), 72 cases (12 transforms, shears included, × outer, outer
    with spread and offset, inset with spread and offset × 1, 1.65) within 4 levels (the canvas's four rows and error
    function: 3.2 at most, the untransformed box alike), and a 300 px blur past the cap of 128; on `main`'s shader every
    non-uniform case and the cap fail (12 uniform ones pass, as they must). Existing references: the filtered suite
    (`Canvas|Paint|GamePaint|Scaled|Compositing|TextHinting|View3D|HiddenDocument|UiResources|JiruanCard`, 465 with the new
    tests: 459 pass, the 6 held cases skipped) passes on the GPU and on lavapipe; its 104 kept renders other than the card's
    and the new fixtures' are identical to `main`'s to the bit (checked before and after the rebase on `0731233`) (untransformed and uniformly scaled shadows unchanged); only the card at
    2.5, 3.4 and 4.6 s changed, nearer Chromium. The full suite passes: 2516 tests and 6 held (the three
    fixtures at both scales) on the GPU (1 min 13 s) and on lavapipe (1 min 32 s). Mutations: 15 (`mutate.py`,
    `~/.cache/stat-fick-build/claude-work/shadow-blur/mutations.json`, made by `make-mutations.py` beside it: the old rule back,
    √|det| for mapRadius, no cap, the axes swapped, the correlation dropped or its sign flipped, the row's shift dropped or
    reversed, its sigma not narrowed, the rows weighted or sampled with the x sigma, the quad's reach in the box's units or the
    smaller axis's, the outer shadow and the inset hole with the old sigma), 15 killed; nothing written to the player's real
    folders.
    The source files are saved in `reference-src/chromium-152-source/shadow/` and `reference-src/skia/`. Details: Docs/Notes.md,
    2026-11-05 (shadow blur).
  - **Canvas: inset shadows cast from Blink's frame (2026-11-05, 2547 tests, +31, none held, a worker worktree, branch `shadow-inset-caster` on `5c11c06`):**
    the user's answer (2) in "Decision on blurred shadows under non-uniform transforms". Blink paints an inset shadow
    (`BoxPainterBase::PaintInsetBoxShadow`) by filling, with the blurring draw looper, a rect with the hole cut out
    (`FillRectWithContouredHole`, clipped to the padding box): the rect is `AreaCastingShadowInHole` (the padding box outset
    by the blur radius, and by the spread's size when it is negative, joined with that moved back by the offset), the hole the
    padding box shrunk by the spread, and the looper moves both by the offset. The canvas cast it from everything outside the
    hole. Now `canvas.frag` takes the frame's blur less the hole's (blur is linear; the frame blurred whole in eight rows,
    `blurredBoxRows`, the hole in the usual four, since the errors of the two add), unblurred inset shadows unchanged, and an
    empty hole fills the box as Blink does. Along an axis a transform shrinks the frame is thinner than the device blur (under
    `scaleX(0.2)` 2.4 px against σ 2.68), and Chromium's shadow is lighter there by up to 15 levels. Pixels beyond 12 levels
    against Chromium, before → after (GPU, 1× / 1.65; lavapipe within 0.03 %): `shadow-scale-x` 0.33 % / 0.26 % → 0.17 % /
    0.16 % (un-held); `shadow-scale-y` 0.33 % / 0.26 % → 0.214 % / 0.17 % (1.65 un-held, 0.19 % on lavapipe); `shadow-rotate`
    0.59 % / 0.47 % → 0.35 % / 0.21 % (un-held at `box-slant`'s 12 / 1 %, as the user decided); `shadow-blurs` 0.09 % / 0.15 % →
    0.02 % / 0.05 %. `shadow-scale-y` at 1×, held to the user's 0.25 % (2026-11-05): 1,059 pixels against the 0.2 % limit's 992,
    598 of them at its two `scaleY(0.2)` boxes with a spread (Blink's 2D-scale effect cutting the glow at their own ink
    overflow, 3.6 px out, and Skia's rect blur rounding the shape's fractional edges: 29 levels on the offset box's lower
    edge), 276 at the offset and spread box under `scale(0.6)` (the rounding) and 178 at the rounded box's corners (untransformed
    alike: Skia blurs a round rect's mask, the canvas Wallace's closed form). Tests: `ShadowTransformTests`' inset cases expect
    the frame's blur less the hole's (a new inset case with a negative spread and offset; 97 cases, worst 3.8 levels against
    4, 4.3 with the frame in four rows); on the previous shader 42 of the 48 inset cases fail (the 6 that pass: the negative
    spread untransformed and uniformly scaled at both scales, and under `scaleX(-0.4)` and `scaleX(0.6)` at 1×); and
    `AnInsetShadowWithNoHoleLeftFillsTheBox` (a 25 px spread leaves no hole: Blink fills the padding box unblurred).
    Existing references (GPU and lavapipe alike): 88 renders identical; `css-shadows` and `scale2-shadows` by up to 4 levels
    (`css-shadows` nearer Chromium: its moved pixels' summed difference 3,545 → 2,170); the 22 game screens mostly by 1 level,
    `game-sheet` by up to 3 (1×) and 4 (1.65, 2 pixels), every one a little further from Chromium (e.g. `game-sheet@1.65`'s
    pixels beyond 12 levels 174,193 → 175,652, `game-settings@1.65` 51,628 → 51,670, the rest by 0–18): their blurred inset
    shadows (the cards' `inset 0 0 42px var(--inner-shade)`, `inset 0 0 22px #000c`) lose the frame's Gaussian tail beyond 2σ,
    up to 2.3 % of the colour, where Skia's triple box blur (`SkMaskBlurFilter`) has almost no tail there, so the frame barely
    changes Chromium's. All within their tolerances; the user kept the port so and recorded the kernel's shape as a known
    difference (2026-11-05). The full suite passes, 2547 tests and none held, on the GPU (1 min 12 s)
    and on lavapipe (1 min 38 s). Mutations: 8 (`mutate.py`, `~/.cache/stat-fick-build/claude-work/shadow-blur/inset-mutations.json`,
    made by `make-inset-mutations.py` beside it: the old caster, the frame without the blur radius or by σ, a negative spread
    not widening it, without its offset copy, not moved by the offset, in four rows, an empty hole left empty), 8 killed;
    nothing written to the player's real folders.
  - **Background layers as Blink paints them (2026-11-06, 2597 tests, +17, none held, a worker worktree, branch `background-layers-aa` on `5af923e`):**
    the user's decision of 2026-11-05 ("Decision on background layers"): the whole layer rule, seamless tiling included.
    New `UI/Paint/UiPainter.Backgrounds.cs` ports `BoxPainterBase::PaintFillLayer` and `BackgroundImageGeometry` in device
    pixels with LayoutUnit arithmetic: the dest snapped, edges a border hides ending at the snapped inner border
    (`ObscuredBorderOutsets`), the positioning area snapped to the inner border, tile sizes (lengths, percentages, auto from
    the natural size or ratio, contain and cover with their rounding, generated images against the snapped area), repeat,
    space, round and no-repeat (a no-repeat tile's place rounded to a whole pixel), the phase (`ComputeTilePhase`,
    `IntMod` truncating), the dest cut to the box and snapped again; `background-attachment: fixed` against the viewport,
    composited when Blink composites it (`CanCompositeBackgroundAttachmentFixed`: not with LCD text strongly preferred, a
    single layer, no shadow, appearance or radius, at most 8 such boxes) and painted over the viewport clipped to the box,
    and scrolling under a transformed box (`HasBackgroundFixedToViewport`); bleed avoidance (`ComputeBleedAvoidance`:
    shrinking under an opaque rounded border; the clip layer approximated by a rounded clip on each draw, a known
    difference by the user's decision of 2026-11-06, "Decision on background layers"). Drawing:
    the bottom layer as the box's (rounded) rect when one tile covers it (`PaintFastBottomLayer`, square boxes too,
    `ContouredRect::HasRoundCurvature` holding without radii), else one draw per layer over the snapped dest, square boxes
    unclipped and rounded ones under the antialiased rounded clip: a single tile drawn from its source rect
    (`OptimizeToSingleTileDraw`; a gradient's rect cut to the tile, `GradientGeneratedImage::Draw`), repeated tiles as a
    pattern: a raster image through the repeat sampler, a gradient, an SVG or spaced tiles drawn once into a tile texture of
    Skia's picture-shader size (`PaintRecord::GetFixedScaleBounds`: the step through the transform's column lengths, rounded
    up) and repeated from it, so tiles have no seams (a spaced SVG tile clipped to whole texture pixels,
    `SVGImage::DrawPatternForContainer`). SVG images are drawn at the box's zoom, and one with a natural size is scaled like
    any image. The canvas's tile API (approved the same day): `Canvas.PushTile`/`PopTile` (draws in the tile's pixels, no
    colour filter or blend, grayscale text; an exact-size target from `CanvasRender.AcquireExactTarget`'s pool, rendered as a
    child command before the pass that samples it) and `DrawImage`'s `premultiplied` flag (`KIND_COMPOSITE`); no shader or
    batching change; `PaintTarget.PushTile`/`PopTile`/`DrawTile` keep a tile's content out of display items and hit regions.
    Found against Chromium on the way (probes in `claude-work/canvas-aa/probe/`: game screens with the header's background
    varied, traced with `make-layer-references.sh`'s tool, and a hidden Electron app reading `LayerTree.snapshotCommandLog`):
    the fast path for square boxes (`CompositingReferenceTests` `settings` and `companions` at 1× failed without it: the
    dialog header's gradient, 101.296875 px tall with a 1 px border, is one tile only by the snapped size the fast path
    tests, and only one tile is opaque); and at 1.65 the composited fixed background and the spaced SVG's pixel-snapped clip
    (the dashed border's 128 pixels there are the border painter's dash ends, a known difference). Tests:
    `Canvas/CanvasTileTests` (9: a tile repeated equals its content drawn at each place, at 1× and 2×; no seams at fractional
    places, at 1.65 and under `scaleX(0.4)`; a half-transparent tile premultiplied once; filtered and blended once where it is
    drawn, with and without multiply; exact and recycled textures) and `Ui/BackgroundLayerReferenceTests` (8: the fixture
    `bg-layers`, 37 boxes, Chromium 152 at 1× and 1.65 captured twice identically into `Golden/ui-bg-layers@*.png` from
    `Reference/background-fixtures.json`, its own list, so no other reference was recaptured; the image within 12 levels /
    0.2 % (0.157 % at both scales); each box outside its rounded corners at most 2 pixels past 12; no colour bleeding through
    an opaque rounded border; a pattern its box's drawing). Main fails it: 0.36 % at 1× and 3.1 % at 1.65 on the first
    version. Every UI, canvas and card reference before → after (`claude-work/canvas-aa/moved.py`, summed difference against Chromium): 99 renders unchanged on the GPU, 97 on lavapipe; the 22 game screens nearer on 16 / 19 (GPU / lavapipe) and farther on 6 / 3 (`game-combat`, `game-companions@1.65`, `game-map` and `@1.65`, `game-sheet`, `game-shop@1.65`; by 0.06–2.8 %), all well inside their 24 levels / 3 %; past 24 levels `game-sheet@1.65` 104,723 → 100,554, `game-settings@1.65` 33,276 → 33,242, `game-shop@1.65` 5,105 → 4,875 (past 12 levels 12,156 → 13,918, in its blurred backdrop), the most added 19 (`game-inventory@1.65`); `box-scale` nearer at both scales. The user was shown these numbers (2026-11-06) and chose to merge (not to merge with
    the six farther screens looked into first, nor to wait). Mutations: 38 (`claude-work/canvas-aa/background-mutations.json`,
    made by `make-background-mutations.py`): 35 killed (9 of them after the tests the first run asked for: the per-element check,
    the filter and blend test, the pattern's drawing, the no-repeat, round, inside and coloured-border cases), 3 survivors
    accounted for: a tile's content recorded as display items (the analysis pass draws no tile content, and in the draw
    pass its items join the box's chunk), `IntMod` floored (the first tile one whole step later: a repeated tile draws the
    same pixels, only the single draw becomes a pattern), the clip layer painted as no bleed avoidance (the approximation
    draws exactly that; kept by the user's decision of 2026-11-06). The full suite passes, 2597 tests and none held, on the GPU (1 min 15 s) and on
    lavapipe (1 min 30 s); nothing written to the player's real folders.
  - **Text first in a paint chunk (2026-11-06, 2803 tests, +7, none held, a worker worktree, branch `lcd-first-text`, rebased on `42fdd90`):**
    found by M11 step 4 (the editor's status line, in a box with `overflow: hidden`, grayscale where Chromium's is LCD);
    "Text first in a paint chunk" above. The cause, in `UI/Paint/PaintTarget.cs`: the draw pass set `Canvas.TextLcd` from a
    chunk's verdict only in `CurrentChunk`, when it made a chunk, and a display item (`BeginItem` … `EndItem`) joins, and
    makes, its chunk in `AddItem` when it ends, after its text was drawn; so text drawn in an item whose chunk didn't exist
    yet took the verdict of the chunk made last. That happens when the text is the first thing recorded in a new chunk: in
    the contents of an `overflow: hidden` or `clip` box (also a scrolled `overflow: hidden` one, after its scroll hit test),
    and the parent's text after a stacked layer's subsequence (or after a drawn canvas's foreign chunk). Only text reads the
    verdict (`Canvas.UseLcd`); an opacity layer's start from the backdrop comes from the analysis by effect, not by order;
    a drawing outside an item makes its chunk before it draws. Reached: text fragments (glyph runs, their shadows, selected
    slices) and a replaced control's label or value (`DrawText` and `DrawGlyphs` in the control's item). Both ways: a chunk
    without text has a grayscale verdict, so text first after one went grayscale where Chromium has LCD (the common case);
    after a layer with LCD text, text first in a grayscale layer went LCD. Not reached: `auto` and `scroll` scrollers (the
    scrolling contents' hit test makes their chunk first), clip-path and CSS clip (in the box's own state: its hit test
    makes the chunk), text after a drawing or a hit test in its chunk. A probe (each text drawing's verdict against its
    chunk's; `claude-work/lcd-first-text/with-probe.diff`) over every UI, canvas, card and editor reference found only the
    editor's status line (both screens, both scales) and a space after the rich card's inline SVG
    (`TransformOffsetReferenceTests`; a space draws nothing): no game screen. Chromium (Blink's `drawing_recorder.cc/.h`
    and `paint_controller.h` fetched into `reference-src/chromium-152-source/lcd/`; `paint_chunker.cc`, `tile_manager.cc`,
    `raster_buffer_provider.cc` and `picture_layer_impl.cc` already there): cc rasters a layer's tiles with or without LCD text as
    a whole (`TileManager`: `use_lcd_text = tile->can_use_lcd_text()`; `RasterBufferProvider`), so every display item takes
    its layer's verdict, and an item joins the chunk current when it ends (`DrawingRecorder`'s destructor →
    `PaintController::CreateAndAppend` → `PaintChunker::IncrementDisplayItemIndex` → `EnsureCurrentChunk`, a new chunk when
    `WillCreateNewChunk`). The fix: `PaintTarget.UseChunkTextVerdict` before each text drawing (`DrawGlyphs`, `DrawText`)
    takes the verdict of the last chunk or, when one will start, of the next (`WillCreateNewChunk`, shared with
    `CurrentChunk`); `CurrentChunk` no longer sets it. The analysis pass is untouched (chunks, layers and verdicts as
    before). Test `Ui/ChunkTextLcdReferenceTests` (7): the fixture `chunk-text-lcd`, 13 cases each on the page's opaque
    background and in a transparent composited layer of its own (text first in an `overflow: hidden`, `clip` and hidden
    box with its own background, in a clip-path box, under a CSS clip, in a scroller, in a `will-change: transform` box,
    grayscale by rule, in an opacity box, after a drawing in its chunk, after a stacked layer's subsequence in other
    properties and in the same ones (a subsequence made for its hit testing), after a layer with LCD text in a layer
    without, the editor's status line), Chromium 152 at 1 and 1.65 captured twice identically into
    `Golden/ui-chunk-text-lcd@*.png` from its own list `Reference/chunk-text-fixtures.json` (no other reference
    recaptured): the image within 12 levels / 0.2 %, and every case's box with no pixel past 12 levels (all within 2
    levels; the scroller's bar left to the scrollbar fixtures: two arrow pixels at 14 levels at 1.65); and a control's
    label taking its own chunk's verdict after text of the other one, both ways (the engine alone: a select's label sits
    a device pixel below Chromium's at 1.65, "Text first in a paint chunk" above). Main fails the fixture: 6,170 pixels
    past 12 levels at 1× (2.6 %), 11,769 at 1.65 (1.8 %); after the fix 0 and 4. Every other UI, canvas, card and editor
    reference before → after (`claude-work/lcd-first-text/moved.py`, against Chromium): 126 renders identical on the GPU
    and on lavapipe (all 22 game screens at both scales, the paint, text, transform, offset, shadow, background and canvas
    fixtures, the card; the compositing references compare layers, which the fix doesn't touch), the editor's four nearer,
    none farther: pixels past 12 levels `shell` 435 → 0 (1×) and 843 → 28 (1.65), `dialog` 389 → 57 and 598 → 110 (GPU;
    lavapipe 436 → 0, 843 → 28, 388 → 57, 601 → 109); what is left there is rounded corners' antialiasing. Mutations: 13
    (`claude-work/lcd-first-text/mutations.json`, made by `make-mutations.py` beside it; run headless under the lock): 8
    killed (no verdict for glyph runs, none for a control's laid-out label, main's rule of the last chunk made, always the
    next chunk, a forced new chunk or other properties not seen, LCD or grayscale everywhere), 5 survivors that can't
    differ: a chunk of another kind not seen (scroll hit test, scrollbar and foreign chunks always force the next chunk
    anyway), no chunk yet not seen (the root's background or hit test data always makes the first chunk before any text),
    a chunk past the analysis's taken as LCD (the draw pass makes the analysis's chunks exactly), suppressed drawings
    given a verdict (no text is drawn while chunks are suppressed: scrollbars, a canvas's image, a background tile, where
    SVG text isn't drawn), and a glyph run's verdict taken before it is recorded (outside an item the record makes the
    chunk the verdict named; inside one it makes none); nothing written to the player's real folders. The full suite
    passes on `8c06f86` (2683 tests: 2676 and 7 new; the GPU 1 min 9 s, lavapipe 1 min 34 s) and, rebased, on `42fdd90`:
    2803 tests (`main`'s 2796 and 7 new), none held or skipped, on the GPU (1 min 12 s) and on lavapipe (1 min 34 s), run
    inside a headless Wayland compositor (`claude-work/lcd-first-text/full-run.sh`: cage on wlroots' headless backend, so
    no test window reaches the screen); the player's real game folder stayed missing and the Documents folder's top level
    unchanged. On `42fdd90` the 22 game screens' renders with the fix equal `main`'s pixel for pixel, on the GPU and on
    lavapipe. Found and left (not this fix's; open in "Small tasks left from earlier milestones", no go yet): a control
    label's baseline at 1.65, and a composited box's text judged off its opaque background at 1.65.
  - **M9 step 1, pack container and virtual file system (2026-11-03, 1184 tests):** four questions asked on the way, all answered as recommended (Q28–Q31 in `Docs/Assets.md`: names Windows can't hold refused too; a misnamed loose file stops the mount; mods are `.pak` in any case in ordinal name order; the old game's class files renamed at port time). `Engine/Statventure/Packs/`: `PackPath` (the rule), `PackFile` (the 64-byte `SVPK` header and the index checked at open, every entry's SHA-256 on every read, positional reads from several threads, `Verify`, `ExtractTo`), `PackWriter` (Brotli quality 11, window 22, entries in parallel, raw unless strictly shorter, re-pack reuse of unchanged entries by content hash after checking them, written to a temporary file then renamed), `VirtualFileSystem` with `Mount.Pack`, `Mount.Folder` (optionally below a place, e.g. `classes`) and `Mount.Mods` (topmost wins, union listing, `Origin`, an atomic `Reload` with `Reloaded`), `DataFileException` naming the file and entry. `Tools/Packer` (`pack`, `list`, `verify`, `extract`), in the solution. `../.gitattributes`: `*.pak` and step 2's `*.rec` binary. 138 tests in `Tests/Statventure.Tests/Packs/`; the full suite passes on the GPU and on lavapipe. 95 mutations, 93 fail a test after four gaps were closed; the 2 survivors can't differ here (read sharing only: Linux's `FileShare` is advisory; no flush to disk: a crash only). Not checked: Windows (sharing, renaming over an open pack).
  - **M9 step 2, the record serializer (2026-11-03, 1229 tests):** six format details asked on the way, all as recommended (Q32–Q37 in `Docs/Assets.md`; "Decisions for M9"). `Engine/Statventure/Records/`: `[Record(name, version)]`, `[Field(n)]`, `[Subtype(tag, type)]`; `RecordFamily` (a type's versions, frozen copies nested in it, upgrades), `RecordLayout` (compiled getters and factories, unions), the codecs of every field kind the later steps' records use (numbers, strings, bytes, enums, vectors and matrices, nullables, lists, dictionaries, ordered pair lists, records, unions), the binary form (type table, LEB128 counts, little-endian numbers, floats bit for bit, presence bytes, blocks) and the JSON form (`$types`, camelCase, shortest numbers, NaN as strings, base64, `$type`), `RecordSerializer` (the API), `RecordFormatException` (reason and path). Tests `Tests/Statventure.Tests/Records/` (45): both forms of every kind, exact bytes and text, damage of every kind, versions and upgrades from committed fixtures of past versions (`Tests/Statventure.Tests/Data/Records/Fixtures`, binary ones `.rec`), and the layout guard (`Data/Records/Layouts/<type>.txt`, recorded with `STATVENTURE_RECORD_FIXTURES=1`). The full suite passes on the GPU and on lavapipe; mutations: 103, all fail a test after one gap was closed (a nullable positional parameter without a default must be given, as a state's clip). The engine's own records come with steps 5–8.
  - **M9 step 3, records in packs, the packer's JSON input and dump, `Application.Data` (2026-11-03, 1358 tests):** five questions asked on the way, all as recommended (S3-1–S3-5 in `Docs/Assets.md`; "Decisions for M9"). `Engine/Statventure/Packs/`: record entries (flag bit 1; `PackWriter.AddRecord`, `PackEntry.IsRecord`; a record beside its loose file refused), `VirtualFileSystem.Load<T>` and `IsRecord` (a pack's binary form or a loose folder's `<path>.json`, every misuse and damage a `DataFileException` naming the pack and entry or the file), the loose rule (`LooseRecord`, `LooseFolder`: a `.json` starting with `"$types"` is a record, a start that isn't JSON fails the mount), `PackWriter.AddFolder` with records' JSON forms and importers (`Importers.cs`: `[Importer(name, endings)]`, `IImporter`, `ImportContext`), `PackFile.ExtractTo` in the loose form and `RecordJson`, `PackDump`, `RecordTypes`. `Core/Application.cs`: `Data` (the area packs by name, `classes/`, the development folder from `ApplicationOptions.DevelopmentFolder` or `$STATVENTURE_DEV_FOLDER`), mounted before the window. `Tools/Packer`: `dump`, records in `pack`, `list` and `extract`; `textconv.sh`, git's driver (`../.gitattributes` `*.pak binary diff=pak`; one `git config` line per clone, in `engine/CLAUDE.md`). `TestPaths.Packs`. 129 new tests in `Tests/Statventure.Tests/Packs/` (267 there). The full suite passes on the GPU and on lavapipe; mutations: 101, all fail a test after three gaps were closed (an escape lost from a test case, a folder listing that happened to be sorted on tmpfs, a cut byte order mark only short reads show); one failed its tests only as seen by hand (`mutate.py`'s `grep` took its output for binary). Later need noted in `Docs/Assets.md`: an "update" command once a pack holds imported and editor-made entries.
  - **M9 step 4, the game folder, saves, settings and character files (2026-11-03, 1535 tests):** six format details asked on the way, all as recommended (Q38–Q43 in `Docs/Assets.md`; "Decisions for M9"). `Engine/Statventure/Storage/`: `GameFolder` (the per-user folder by system and environment, a pure function), `GameStorage` (settings, `saves/`, `mods/`; one background writer, `Flush`, `WriteFailed`, "Open game folder" as Electron's `shell.openPath`), `SaveFile` (the 48-byte `SVSV` header, `SaveState`: missing, readable, damaged, unreadable), `SaveSlots` (named slots, the backup through `File.Replace`, damaged and unreadable files kept aside on the next write, `Delete`), `Settings` (a JSON object keeping every key, typed get and set, upgrades, unusable files kept as `settings.broken.json`), `CharacterFile` (export and import of the JSON form, the old game's 10 MiB limit), `DocumentsFolder` (Chromium 152's XDG lookup and its making of the folder), `FolderListing`. `Tools/DocumentsReference` and `Tools/Scripts/make-documents-reference.sh` capture Electron's `app.getPath("documents")` in 46 environments (and .NET's `MyDocuments`, which differs in 31) into `Tests/Statventure.Tests/Data/Storage/documents-reference.json`. 177 tests in `Tests/Statventure.Tests/Storage/`, with old-version saves, character files and settings in `Data/Storage/`. Calling `GameStorage.Flush` when the application quits (`Application.cs`) is left to step 9 or the port; until then the writer's foreground thread keeps the process alive until pending files are written. The full suite passes on the GPU and on lavapipe; mutations: 137, 135 fail a test after one gap was closed (a 46th Documents case: a backslash just before the end of an unclosed value) and a redundant line was removed; the 2 survivors can't differ (no flush to disk: a crash only; settings' `AllowDuplicateProperties = false`, which `JsonObject` enforces anyway). Not checked: Windows.
  - **M9 step 5, models and state machines in packs (2026-11-03, 1600 tests):** one question asked on the way, answered as recommended (S5-1 in `Docs/Assets.md`: a model or state machine that reads but breaks its rules is checked at load; "Decisions for M9"); the step's record type names, field numbers and machines anywhere by path accepted with it. `Render3D/ModelData.cs`: the model's record types (`model`, `model.node`, `.mesh`, `.primitive`, `.joints`, `.material`, `.skin`, `.animation`, `.channel`, `.event`, all version 1: `ModelData` as `GltfImport` gives it), `ModelData.Load(files, path)` and `Validate()` (what `GltfImport` checks plus the node, mesh, skin, material, root and joint indices it leaves to BRUTAL); `Render3D/ModelImporter.cs` (`[Importer("models", ".glb")]`: one checked record per `.glb` at its path without the ending); `Model.Load(device, files, path)`; `Animation/StateMachineDefinition.cs`: the state machine's record types (`animation.state-machine`, `.parameter`, `.condition`, `.state`, `.transition`, `.layer`), packed from their JSON form, `StateMachineDefinition.Load(files, path)` validating; `Packs/VirtualFileSystem.Problem(path, problem, inner)` (a path's place named as the file system's own errors name it, for every loader that checks what it read). 65 new tests: `Render3D/ModelPackTests.cs` (the 12 test models from a pack and from their JSON form equal `GltfImport`'s bit for bit; four Blender scenes drawn from pack-loaded models match their goldens), `Render3D/ModelValidationTests.cs` (each rule broken once, 39 cases at their limits; the problems' naming from packs and loose folders; the importer's check; `Problem`), `Animation/StateMachineRecordTests.cs` (the M7 machines through both forms, one written by hand, machines packed beside a model, a figure and its machine from a pack playing as the built ones, frame by frame; a machine that doesn't validate); layouts and fixtures of the 16 new types in `Tests/Statventure.Tests/Data/Records/`. No pack committed (Q1). `StateMachineTests.Humanoid` made internal for the new tests. The full suite passes on the GPU and on lavapipe; mutations: 96, all fail a test. Checked by hand: the packer's commands on `.glb` files and a machine's JSON form. Not checked: Windows.
  - **M9 step 6, the music and effects in `audio.pak`, `MusicData.g.cs` removed (2026-11-03, 1658 tests):** five questions asked on the way, all as recommended (S6-1–S6-5 in `Docs/Assets.md`; "Decisions for M9"). Records declared (`audio.theme`, `audio.section`, `audio.layer` … `audio.room`, `audio.theme-order`, `audio.effect` and its parts; layouts and fixtures in `Tests/Statventure.Tests/Data/Records/`). `Audio/Music/MusicLibrary.cs`: `MusicNames` (the folders and the reversible name rule) and `MusicLibrary` (`Load(data)` with the order, id, room and patch checks, `Empty`); `AudioSystem(MusicLibrary)`, `Soundtrack`, the composer, `GraphBuilder`, the prepared rooms (keyed by name, values and rate), `PlayTheme` and `AudioBenchmark` instance-based on it; effect patches of the data registered by name; `Core/Application.cs`: `Audio = new AudioSystem(MusicLibrary.Load(Data))`. `make-music-data.sh` is the importer: `music.cjs` writes the records' JSON forms (as the serializer writes them) into `~/.cache/stat-fick-build/music-data/`, the packer packs them into the committed `engine/Assets/Packs/audio.pak` (118 entries, 57 KB). Checked once before deleting `MusicData.g.cs`: every pack-loaded record equal to it bit for bit. Every music test passes unchanged on the pack (61 exact call logs, scores, fixtures); 58 new tests (`MusicLibraryTests`, `ApplicationMusicTests`). The full suite passes on the GPU and on lavapipe; `test-themes.sh`: all 55 themes pass on the pack-loaded data (21 minutes); mutations: 62, 61 fail a test; the one survivor, the rooms not prepared ahead, changes no sample (`Play` prepares a missing room on the caller's thread, so only when the work is done changes: step 7's speed decision, measured by `audio-benchmark.sh --start`).
  - **M9 step 8, terrain into `world.pak` (2026-11-03, 1686 tests):** two questions asked on the way, both as recommended (S8-1, S8-2 in `Docs/Assets.md`; "Decisions for M9"). `Render3D/Terrain.cs`: `TerrainData` is the record `terrain` (width, depth, cell size, the height range, the height image's 16-bit samples, the covers, the slots, the water level), its float heights worked out from the samples when first read by M6's formula, so the test terrain's heights are bit for bit what M6's loader gave (their SHA-256 taken before the change is pinned by a test) and the Blender comparisons hold; one description parser for `TerrainData.Load(path)` and the importer (the images decoded from memory by the same Stb decoder, problems named: a missing or mistyped key, a size that isn't whole, an image of another size or none); `TerrainData.Load(files, path)` and `Validate()` (a sample and a cover for each of the grid's points, as the loader checks its images; a `DataFileException` naming the pack and entry or the loose file), `Terrain.Load(device, files, path)`. `Render3D/TerrainImporter.cs`: `[Importer("world", ".terrain.json")]`, the record at the description's path without the ending, its images taken (not packed). Tests: 18 in `Tests/Statventure.Tests/Render3D/TerrainPackTests.cs` and `TerrainPackRenderTests.cs` (the description-loaded terrain against M6's hashes, the pack-loaded and JSON-form-loaded terrain equal to it, the importer's rules with 23 unusable descriptions, broken records refused at load, the three terrain scenes from the pack against Blender's renders and pixel for pixel against the description-loaded terrain's), the `terrain` layout and fixture. No pack committed (Q1). `Docs/Assets.md`'s "16-bit height PNG" corrected: Stb's `Load16` takes 8-bit (v · 257) and colour PNGs too. The full suite passes on the GPU and on lavapipe (1686 tests); mutations: 52, all fail a test. Not checked: Windows.
  - **M9 step 9, the game's data at run time (2026-11-03, 1716 tests):** four questions asked on the way, all as recommended (S9-A–S9-D in `Docs/Assets.md`; "Decisions for M9"). `Tools/Scripts/build.sh` and `build.ps1`: after the solution, the sandbox (the game's stand-in until the port) published into `$STATVENTURE_GAME` (`dotnet publish --no-build`, framework-dependent) and `packs/` beside it made exactly `Assets/Packs/*.pak` (stale packs removed, unchanged ones left, changed ones copied beside and renamed over, so a running game keeps its open pack). `Core/Application.cs`: `ApplicationOptions.GameFolder`; `Application.Storage` (the player's `GameStorage`, made first in `Run()`, disposed (flushed) in `Dispose()` after the window: after the game's `Unload()` and after a failed start or loop); `DataMounts` with the player's `mods/` between `classes/` and the development folder (mounted even when missing, so Reload sees mods added later); `Application.ReloadData()` (the data remounted, the music reloaded by the application's own first listener of `Data.Reloaded`, then the game's listeners; a failed mount or music logged, giving false). `Audio/AudioSystem.cs` `Reload` (rooms prepared on a worker, the music switched, new and changed effect patches registered), `Audio/Music/Soundtrack.cs` `Use` (a new builder for the library; the theme the game last asked for restarted through the switch when its music changed), `Audio/Music/MusicLibrary.cs` `SameMusic` (binary forms). 30 new tests: `Packs/ApplicationModsTests.cs`, `Core/ApplicationStorageTests.cs`, `Core/ApplicationReloadTests.cs`, `Core/AudioReloadTests.cs` (with `Core/TestApplication.cs`); `Audio/ApplicationMusicTests` points its application at a game folder of its own. Checked by hand: the build script's copy rules; the published sandbox run from the laid-out folder with a mod in a per-user folder of its own and a development folder (mounts in order, the mod's music loaded), with a damaged mod (stops before the window, naming it) and with a mod's theme of the wrong id (stops when the sound starts, naming mod and entry). Known limit (noted in `Docs/Assets.md`): Reload re-reads the mounts found at the start. The full suite passes on the GPU and on lavapipe (1716 tests); mutations: 54, 49 fail a test (one malformed mutation replaced); the 5 survivors change no sample, only when and on which thread work is done (as step 6's survivor): the restart's room not prepared on the game's thread ahead of it, `Play` preparing rooms through the old library's preparer, the new builder's composer kit made on the audio thread at its first performance (the same fixed-seed noise), the new builder without the prepared rooms (the composer makes the room's response and kernels itself, sample for sample the same, as the exact restart test shows), and the new library's rooms not prepared on a worker (the process-wide cache is filled on demand instead); the build script's 7 rules broken by hand each fail its check script. Not checked: Windows (`build.ps1` not run).
  - **M9 step 7, stylesheets, fonts and images into `ui.pak` (2026-11-03, 1907 tests):** six questions asked on the way, all as recommended (S7-1–S7-6 in `Docs/Assets.md`; "Decisions for M9"). `UI/Css/StylesheetRecords.cs`: the compiled stylesheet's records (`ui.stylesheet`, `ui.style-rule`, `ui.declaration`, `ui.keyframes`, `ui.keyframe`, `ui.font-face`, `ui.token`: the rule structure, everything inside as CSS tokens, a token's kind folding in its integer and id flags). `UI/Style/Stylesheet.cs`: `Compile(text, fileExists)` (parsed and prepared as `Parse`, then checked: selectors that don't parse except other browsers', unhandled at-rules, declarations and keyframe selectors that don't parse, unknown properties, bad values, @font-face rules the engine can't honour, urls that aren't `data:` or data paths of files there), `ToRecord`, `FromRecord` and `Load(files, path)` (selectors and media queries parsed from their tokens, everything checked again, problems named through `VirtualFileSystem.Problem`); `UI/Css/CssParser.cs`: `CssParseDiagnostics` (what parsing leaves out), the rules' selector and media tokens kept. `UI/Css/FontFaceDescriptors.cs`: a @font-face as the engine honours it. `UI/StylesheetImporter.cs`: `[Importer("ui", ".css")]`. `Text/FontLibrary.cs`: faces from @font-face rules (`AcquireSheetFace`, `ReleaseSheetFace`, `SheetFaces`: shared by data and descriptors, matched before the library's own faces of the family, the later first). `UI/Dom/UiDocument.cs`: `Files`, the author sheets' @font-face fonts read from it and given back, a font that can't be had an error naming the sheet. `UI/UiHost.cs`: `UiResources` (images and SVG text from the document's data after the callbacks, what can't be shown logged once). `Tools/Scripts/make-ui-pack.sh` and the committed `engine/Assets/Packs/ui.pak` (the five faces and two licence texts, 999 KB). Tests (`Tests/Statventure.Tests/Ui/`): `CompiledGameStylesTests` (the old stylesheet compiled by the importer gives the parsed one's computed styles on the 22 captured screens; the same rules part for part), `StylesheetCompileTests` (every token kind and both record forms, the importer, every refusal of compiling and of loading), `FontFaceRuleTests` (the descriptors, the committed pack's faces equal `TestFonts`' with the same matching and layout, sharing, hiding, errors, `Files`), `UiResourcesTests` and `UiHostResourceTests` (a real window), the committed pack's contents and repacking; layouts and the fixture in `Tests/Statventure.Tests/Data/Records/`. `GameLayoutTests.Build` takes the compiled sheet, so every test of the game's screens runs on it. The full suite passes on the GPU and on lavapipe (1907 tests); mutations: 175 (`~/.cache/stat-fick-build/claude-work/m9/step7-mutations.json`, made by `make-step7-mutations.py` beside it): 162 fail a test; one was malformed (renaming a token kind's member breaks the code naming it; the layout guard is covered by two kinds' numbers swapped); 12 survived, all accounted for. Seven were gaps, closed and then killed (`step7-rerun.json`): keyframes under @media and an !important keyframe declaration were missing from the sample sheet (three mutations; added, its fixture recorded again), a malformed declaration inside an otherwise valid @font-face, family names joined by a comma without a space, a face of another weight with the same range end, and setting `Files` failing at a second sheet after the first's faces were taken. Three were redundant code, removed: the `::` branch of the other-browsers check (the loop meets a pseudo-element's second colon, whose next token is the name), the problems' de-duplication (nothing makes one twice; each is now listed as found) and the `IsShorthand` test of a pending shorthand (a name that isn't one has no longhands). Two can't differ in results, only in work: acquiring a sheet face looks for an existing one before making one (without it the face is made and retired as a duplicate, kept until the library goes: memory and time only), and setting `Files` to the file system it already has returns at once (otherwise its faces are taken again, the old ones given back and the layout redone). Not checked: Windows.
  - **M9 done (2026-11-03, 1907 tests):** all nine steps merged on `main` (with the FreeType thread-safety fix on the way); the full suite passes on the GPU and on lavapipe. Mutations over the nine steps: 875, 852 fail a test; 22 survivors, all accounted for (gaps closed and killed, redundant code removed, or no difference in results, only in work or on Windows), and 1 malformed (step 7). Ticked under Milestones; M10 waits for the user's go.
  - **M10 started (2026-11-04):** the user's go; the eleven questions asked and answered, all with the recommended options except the scope (M10-2: Ji'ruan's boss intro remade now as a proof, M10-11) ("Decisions for M10"); design doc `Docs/Scene.md`: what exists checked against the code (the flat `AnimatedView` with one matrix per actor; glTF cameras and lights not read, though BRUTAL's schema has them; one sun and no model opacity in the renderer; particle effects not records; frame clients advanced by the whole hidden time after a minimize; the audio API playing only "now", with no readable clock), the old game's four kinds of cutscene and Ji'ruan's CSS timeline element by element, the method, a proposed design (the node tree and scene, F-curves, the sequence record with its track kinds, the builder, the player, audio and UI cues, the Blender export of a cutscene stage, the proof), seven proposed steps with the files each touches and which can run side by side, and 33 open questions with recommendations. Blender 5.2's `fcurve.cc` is in `reference-src/blender-anim/`; `curve.cc` (auto handles) and `easing.cc` are still to fetch. No code yet.
  - **M10 step details (2026-11-04):** the 33 questions asked and answered the same day, all as recommended except Q31 (the proof's stage shown to the user as Blender renders for approval before its tests) ("Decisions for M10", "Step details"; `Docs/Scene.md` updated: "Open questions" became "Step details", the design states the answers, the step list follows them). M10-8's wording corrected: M8 has no public scheduling, step 6 adds it. No code yet.
  - **M10 step 1, the node tree and the scene (2026-11-04, 1930 tests, on its worker branch):** Q1–Q3 answered as recommended (the scene replaces `AnimatedView`, `Actor.Transform` goes; each top-level object of an instanced model is its own model; terrain on a node). `Engine/Statventure/Scene/`: `SceneNode` (local translation, rotation and scale; world = local × parent's world, worked out again only after a change above; paths), attachments `Actor` (moved from `Animation/AnimatedView.cs`, which is gone), `PlacedModel`, `ModelInstance` (a model's hierarchy as scene nodes, per-object outline, glow mask, hatch anchor and picking; its clips and NLA stacks applied to its nodes by name), `PlacedTerrain`, `SceneCamera`, `SceneSun` (glTF's conventions); `SceneView` (the frame client: figures at the scene's rate, new draw items only on change, the view's camera and sun from nodes). Tests: `Tests/Scene/` (`SceneNodeTests`, `SceneReferenceTests`, `SceneViewTests` with M7's view tests moved in, `SceneRenderTests`); Blender: the hierarchy test model (`make_models.py`: parent inverses, a bone parent, non-uniform scale above a turned child, a mirrored branch, an animated parent) evaluated by `evaluate_animations.py` (node scenes, `Reference/node-scenes.json`, `Golden/nodes-hierarchy.json`): within 0.0007 mm on frames, at most 0.68 of M7's allowance between; the render scene `scene-objects` (instanced, per-object ink; `render_scenes.py`'s objects mode) and `gear-swing` and `terrain-map` drawn through the scene match their goldens. Also hiding (Q23: a hidden node hides its subtree). The full suite passes on the GPU and on lavapipe (1930). Mutations: 65 (`~/.cache/stat-fick-build/claude-work/m10/step1-mutations.json`, made by `make-step1-mutations.py` beside it): 62 fail a test (two of them after gaps were closed: a node re-added after its world was read with no parent, and something new attached to a node already shown); one, a node allowed under itself, hangs the test (walking up to the root never ends; `mutate.py` reads the timeout's missing result line as a survivor); two survive and can't differ in results, only in work: marking a subtree stale without stopping at nodes already stale, and marking an already stale node again. Details in `Docs/Scene.md`, "How it works", step 1.
  - **M10 step 2, F-curves and easing (2026-11-04, 1975 tests, on its worker branch):** three details asked on the way, all as recommended (S2-1–S2-3 in `Docs/Scene.md`; "Decisions for M10", "Step details"). `Engine/Statventure/Scene/FCurve.cs` (the records `scene.curve` and `scene.key`, version 1, which are the curve: immutable, `Evaluate(seconds)`, `Handles`, key helpers, `Load`; the rules a curve keeps, refused when made, copied or read), `FCurveHandles.cs` (Blender's update: crossed and stray handles fixed, the 0.001-frame clearance, `calchandleNurb_intern` for every handle type, held ends, continuous acceleration's tridiagonal solve with its limits), `FCurveEvaluation.cs` (extrapolation by the end keys' interpolation, the key search with its 0.0001-frame threshold, constant, linear, Bezier with `correct_bezpart`), `FCurveEasing.cs` (`easing.cc`); in double and in frames inside, seconds outside. One departure from Blender's code, not its results: a Bezier segment's parameter by Newton's method in a bracket, as `solve_cubic` from double coefficients loses the root where the time runs linearly. Checked against Blender 5.2 (`Tools/BlenderReference/evaluate_curves.py`, one line in `make-blender-references.sh`): 134 curves, 595 keys, 21,068 times, values within an allowance of 16 float32 roundings of the sizes involved (at most 0.66 of it), every key's handles (at most 0.28). Layouts and fixtures for both types. Blender facts: its new F-curves smooth with continuous acceleration; the end keys' interpolation decides the extrapolation; thresholds are frames and aligned handles depend on the time unit; the Python API's `keyframe_points.add` zeroes back, amplitude and period. The full suite passes on the GPU and on lavapipe (1975, step 1's 1930 and 45 new). Mutations: 157, 145 killed; 12 survive, none able to differ beyond float32 rounding: equivalent, the clearance's one-ulp step (one double ulp of a clamped handle's time), the both-free early return (nothing below it changes a free side), the second 5× limit (it can only act when the first didn't), an interior auto-clamped peak's lock and runs ending at locked keys (its clamps hold its offset at 0 either way; only a zero's sign changes), a two-key run's early return (solving gives the chord the handle rule gave), the output's guard for computed sides (without cycles every caller passes one), elastic's amplitude-0 branch (f·0 is 0 either way) and its end test (a segment is never evaluated at its own end: that frame is the next key's); below float32's resolution, the flat-segment shortcut (values within FLT_EPSILON); and two of the solver's heuristics no curve among 40,000 random auto-clamped ones tells apart: locking the overshooting handles that go the right way first (only a zero's sign changes) and releasing a handle at most twice (none is released twice) (`Docs/Scene.md`, Status).
  - **M10 step 3, cameras and lights from glTF (2026-11-04, 2027 tests, on its worker branch):** Q9–Q13 as answered; S3-1–S3-5 asked on the way, all as recommended ("Decisions for M10", "Step details"). `Render3D/ModelData.cs`: `model` and `model.node` version 2 (cameras and lights; a node's camera and light) with frozen version-1 copies and upgrades, `model.camera` and `model.light` (layouts and fixtures; M9's version-1 model fixture kept, its upgrade tested). `Render3D/GltfImport.cs`: `KHR_lights_punctual` taken as a required extension (Blender lists it so); perspective cameras by yfov, orthographic by twice ymag; one white directional light with its angle from its extras; refused, naming the file and the camera or light: point and spot lights, a coloured sun, a sun without its angle, more than one sun, an infinite perspective, values that make no projection or sun, indices that don't exist (through the models importer a `DataFileException`); `ModelData.Validate` checks the same. `Scene/ModelInstance.cs`: instancing hangs `SceneCamera`s and the `SceneSun` on their nodes (`Cameras`, `Sun`). Blender 5.2's exporter read and probed: no timeline-marker support (step 4's Q22 goes through a custom property of the scene, which the scene mode's one clip takes as its extras), "Split Animation by Object" must be off for one clip, cameras and lights turned Rx(−90°) after their world, the raw lighting mode labelled deprecated, hemi and area lights not exported, orthographic magnifications computed as for sensor fit Auto. `make_models.py`: the stage export (scene mode, raw units, the sun's angle copied, orthographic magnifications put right after export) and the stage `dolly`; `evaluate_animations.py` (stage scenes, `Golden/stage-dolly.json`): every node within 0.00053 mm of Blender on frames, at most 0.67 of M7's allowance between, the cameras' settings and projections at two aspects and the sun's direction, strength and angle within 1.2·10⁻⁷; the render scene `scene-dolly` through the imported camera and sun matches Blender's render (`render_scenes.py`'s objects mode drops the file's cameras and lights). Tests: `Scene/SceneStageTests.cs`, `Render3D/ModelCameraLightTests.cs`, `GltfImportTests`; `ModelPackTests`' test-model count 14; one `Scenes3D` entry. The full suite passes on the GPU and on lavapipe (2027). Mutations: 68 (`~/.cache/stat-fick-build/claude-work/m10/step3-mutations.json`, made by `make-step3-mutations.py` beside it), all killed; one gap was closed while writing them (the test sun's strength and angle had been `SceneSun`'s defaults, so instancing's copy went untested). Details in `Docs/Scene.md`, "How it works", step 3.
  - **M10 step 4, the sequence record, builder and player core (2026-11-04, 2190 tests, on its worker branch, rebased on step 3):** nine details asked on the way, all as recommended (S4-1–S4-9 in `Docs/Scene.md`; "Decisions for M10", "Step details"). `Engine/Statventure/Scene/Sequence.cs` (the `scene.*` records, version 1: the sequence, roles, spans, strips, the union of the eleven track kinds and their cues; `Validate()` alone and with the stages; `Load(files, path)` with the stages from the same data, errors through `VirtualFileSystem.Problem`; marker times), `SequenceBuilder.cs`, `SequenceBindings.cs`, `SequencePlayer.cs` (the clock on the frame clock at its own rate, pause, the hidden time dropped, seek, skip, finish, the end state, the reduced variant's cut list, cue delivery as game code), `SequencePlayback.cs` and a `SequencePlayback.<Kind>.cs` per kind (clip, transform, visibility, animation, palette and event played; camera, light, particles, audio and UI placeholders for steps 5 and 6); `SceneView` gains `Play`, `Playing` and `Animate` and drives its players in its frame step (players, then the other figures, then the animated models, one refresh; in the events step the figures' events, then the cues). The marker convention for step 7's export: the Blender scene's custom property `events` = `[{name, time}]`, seconds from its first frame (Blender 5.2's scene mode writes the scene's custom properties as the animation's extras; `GltfImport` already reads `events`). Euler XYZ checked against Blender 5.2's mathutils. Tests: `Tests/Scene/Sequence*Tests.cs`, `TestSequence.Core.cs`, `SequenceBench.cs`; layouts and the fixture for the 25 new types. Three behaviours settled while writing it, accepted by the M10 manager (a clip's channel back to rest unless an earlier track set it in that frame; state cues within 10⁻⁶ s one moment, the later in record order in effect; game code in the end's frame before its events step finds the end pending). Also fixed: `Animator` calls each event and state handler apart, so a throwing one no longer leaves the others and the `Next`, `Entered` and `Left` tasks without the event (a test that fails without the fix). The full suite passes on the GPU and on lavapipe (2190: step 3's 2027 and 163 new). Mutations: 259 (`~/.cache/stat-fick-build/claude-work/m10/step4-mutations.json`, made by `make-step4-mutations.py` beside it; the four for the `Animator` fix among them), all killed: 257 in the run, two after closing the gaps they showed: a stage path with two slashes outside `models/stages/` (only the depth check had been tried; a case added) and the scene's player step removed, which made the tests' frame loops spin until `mutate.py`'s timeout (read as a survivor; the loops are bounded now, `SequenceBench.FrameUntil`, so a player that stops fails the test). Four candidates were left out of the list: two lines of code removed while writing it (a second check of the stages given, which `Validate(stages)` makes with the same words, and a hidden-at-play branch no frame could reach), and two that can't change a result (a frame's early return when nothing changed, and forcing the first frame, which state cues get anyway as they start with no cue in effect).
  - **M10 step 5, camera, light and particle tracks (2026-11-04, 2267 tests, on its worker branch):** six details asked on the way (S5-Q1 not the recommendation: the free emitters and the water both on the scene's rate; S5-1–S5-5 as recommended; "Decisions for M10", "Step details"). `Render3D/Particles.cs`: `ParticleEffect`, `ParticleRange`, `ParticleShape` and `ParticleSpawn` as the records `particle.effect`, `particle.range` and the unions `particle.shape`, `particle.spawn` (version 1, float32, M6's defaults; layouts and five fixtures), `ParticleEffect.Problem` and `Load` (`models/effects/<name>`, Q25), emitters on a frame (`ParticleEmitter.Frame`: the spawn in the emitter's space at each birth, Blender's `psys_get_birth_coords`), `Count`, `End`. `Scene/SceneEmitter.cs`: emitters on nodes (the node's world recorded per frame, births between frames interpolated, jumps from the world after; its own clock; colours from its palette or what its node belongs to). The camera, light and particle playback (`SequencePlayback.Camera.cs`, `.Light.cs`, `.Particles.cs`): cuts and sun cues by the cue in effect, the curves on the camera or sun cut to, the particle track a state (each emitter at τ less its cue's time; seeks, skips and frames across a cut jumps; released to the scene's clock at the end). Bounded changes approved by the M10 manager: `SceneView` (emitters advanced at `Rate`, recorded and drawn, finished ones taken off; the water's clock at `Rate` while it holds water), `SequenceBindings.Effect`, `SequencePlayer.Start` (the effects' checks), `PlaybackContext.Effect`; the test bench's pivot carries a camera and a sun. Blender 5.2's `particle_system.cc` read (fetched into `reference-src/blender-anim/`): `reset_particle` evaluates the emitter's animation at each particle's birth and `psys_get_birth_coords` places it through the emitter's world. Tests: `ParticleTests` (records, rules, loading, frames), `SequenceCameraTests` (cuts against Blender's evaluation of the dolly stage through the player, the curve, refusals), `SequenceLightTests`, `SequenceParticleTests` (scene emitters, the track, the particle reference scene replayed through a sequence against `Golden/r3d-particles`, the water's clock), `TestSequence.Camera.cs`. The design text listed particle starts among the moments; corrected (a particle track is a state). The full suite passes on the GPU and on lavapipe (2267: step 4's 2190 and 77 new). Mutations: 151 (`~/.cache/stat-fick-build/claude-work/m10/step5-mutations.json`, made by `make-step5-mutations.py` beside it), all killed: 149 in the run, two after fixing them: one a gap (a drag of +∞ was never tried: the mutation dropping the drag's finiteness check still refuses NaN, as NaN ≥ 0 is false; a case added) and one malformed (removing a check left a `foreach` with a declaration as its body; rewritten as `if (false && …)`); seven candidates left out that can't change a result (`Docs/Scene.md`, Status).
  - **M10 step 6, audio and UI tracks (2026-11-04, 2312 tests, on its worker branch, rebased on step 5):** six details asked on the way, all as recommended (S6-A–S6-F in `Docs/Scene.md`; "Decisions for M10", "Step details"). `Audio/AudioSystem.cs` (`Time`, the audio clock; `ScheduledSound`; `AudioSchedule`, each player's calls at a time applied in the quantum they fall in, at their time), `Audio/Effects/SoundEffects.cs` (`PlayAt`, the voice made in its quantum), `Audio/Music/Soundtrack.cs` (`PlayAt`, `StopAt`, music.js's fades from their time), `Scene/SequencePlayback.Audio.cs` (cues sent in the first frame anchored to the quantum the call is applied in, cancelled by a pause, the hidden document, a seek or a rate change and sent again in the next frame the clock moves in; at a skip the pending cancelled and the end's music cue applied if it hadn't happened; the reduced variant's cut parts as at a skip; the decisions on the audio thread), `Scene/SequencePlayback.Ui.cs` (text, classes and attributes per element and thing set, restores with the text's nodes, ids found at `Play`, nested hosts refused; `UiClock`), `UI/Style/AnimationEngine.cs` (`AnimationTimeline`: the CSS animations inside a sequence's hosts on its clock, held from the last frame of a pause and exactly through the hidden time), `Scene/SequenceBindings.cs` (`Audio`), `Scene/SequencePlayback.cs` (`PlaybackContext.Bindings`), `Scene/SequencePlayer.cs` (the clock set before the playbacks apply, S6-E). Tests: `Audio/ScheduledSoundTests.cs`, `Scene/SequenceAudioTests.cs`, `Scene/SequenceUiTests.cs`, `Ui/AnimationTimelineTests.cs`, `Scene/TestSequence.Sound.cs`; step 4's bench and rate test (S6-F). Found: music.js's switch at a quantum's start while the old theme is still fading in cuts it (Blink's `cancelAndHoldAtTime` at the current time, as ported in M8; not checked against Chromium, not changed). The full suite passes on the GPU and on lavapipe (2312: step 5's 2267 and 45 new; `OutputTests.TheDefaultDevicePacesTheGraph`, the real device's pacing test, failed once in one GPU run and passed alone and in the next full run). Mutations: 101 (`~/.cache/stat-fick-build/claude-work/m10/step6-mutations.json`, made by `make-step6-mutations.py` beside it): 99 killed, 93 in the run and 6 after closing the gaps they showed (five survivors and one that didn't build: the mutation itself, rewritten): only the last music cue of a cut part (two at one cut only differ when the later names the theme playing; a test added), the hold released at the end at the last frame (a skip from a timer before a frame's steps; the end test skips so now), a play-state pause on a timeline whose time is behind the document's (the test holds and releases first), the last frame's time (a pause from a timer resumed between frames), an animation's pause time moved at a detach (the test detaches one paused by its play-state). Two survive and can't differ: sending at rate 0 (no frame moves the clock then; a forced one would schedule cues at infinite times, cancelled at the next rate change before any could be due) and the end's frame sending in its `Apply` (the guard keeps the audio thread from starting cues a skip cancels right after; offline both calls are taken in the same quantum before any is applied, and the end as played sends the same call in the same frame). Six candidates left out of the list: only work or memory (a held animation counted settled, the detach's move back, the restore's guard for an unchanged text, the rooms prepared on the game's thread) or unreachable (the clock's guards against a repeated pause or visibility change, the nesting check's own-player exception). Details in `Docs/Scene.md`, "How it works", step 6; `Docs/Audio.md`, "The clock and scheduled calls"; `Docs/Scripting.md`, "Sequences' CSS animations".
  - **Test runs kept out of the player's folders (2026-11-05, 2319 tests, +2, a worker worktree):** the PC's real game folder
    `~/.local/share/stat-fick/saves/` held `slot.dat` (74 bytes) and `slot.dat.bak` (76), a `test.part` record named
    "load" and its backup, both folders made on 2026-11-03 at 22:15:35 (mode 0700, as the engine makes them), between
    the end of M9 step 9's lavapipe run (22:14:37) and the end of its mutation run (22:20:00). The writer: the step's
    mutation "storage folder: the option ignored" (`Application.StorageFolder` without `options.GameFolder`), the 7th of
    54, under which `ApplicationStorageTests.WhatTheGameSavedIsWrittenBeforeRunEnds` (cases update, unload, load) saved
    its slot into `GameFolder.ForThisMachine()`; the mutation was reported killed, as it should be, and nothing looked
    at the folder. Proof: the mutation applied by hand with `XDG_DATA_HOME` and `HOME` pointed at a scratch folder gave
    the same result line (5 failed of 5) and left there a `slot.dat` identical byte for byte to the one found (its
    backup "unload"). No test or tool reaches the real folders unmutated: the 215 storage, core and application tests
    left the scratch data folder empty; the other callers of `ForThisMachine()` only compare paths; no tool makes an
    `Application` or `GameStorage`; the sandbox saves nothing. The user's decision ("Decision on test runs and the
    player's folders"): `Tests/Statventure.Tests/Infrastructure/PerUserFolders.cs` (the module initializer: .NET's
    `XDG_DATA_HOME`, `XDG_CONFIG_HOME`, `HOME`, on Windows also `APPDATA`, `LOCALAPPDATA`, `USERPROFILE`, inside a
    per-run `statventure-tests-*` folder in the temporary folder, removed at exit; `STATVENTURE_CACHE`, when unset,
    named as `ThemeRenderTests` found it before, so the build cache isn't looked for in the scratch home;
    `GiveOriginals` for child processes), `SystemFonts.Run` giving fontconfig the real folders,
    `Infrastructure/PerUserFoldersTests.cs` (2: this machine's game folder, the application's default storage folder and
    the Documents folder inside the scratch folder, the variables too; fontconfig's child sees the C library's
    environment while any other child sees the scratch folders), `Tools/Scripts/mutate.py` (watches the real game folder
    whole and the Documents folder's top level, missing counting as a state; a change prints `!!! WROTE TO THE PLAYER'S
    REAL FOLDERS` and goes into the results file and a count at the end). Checked: step 9's mutation run through the new
    `mutate.py` with the redirect writes its saves into the run's scratch folder and is still killed (5 of 5), and with
    the redirect taken out by hand it writes into the watched folder (a stand-in `XDG_DATA_HOME`) and `mutate.py`
    flags it (`made +saves +saves/slot.dat +saves/slot.dat.bak`). Mutations: 11 (`~/.cache/stat-fick-build/claude-work/
    stray-saves/perfolders-mutations.json`, made by `make-mutations.py` beside it), 10 killed; the survivor, the build
    cache not kept, can't differ under `mutate.py` (it sources `env.sh`, which sets `STATVENTURE_CACHE`) and was killed
    by hand without the variable; the removal at exit, which no test sees, was checked by hand (without it the run left
    its folder in `/tmp`); the Windows variables can't differ on Linux. Also fixed: `Core/LogTests` reads the log but ran
    in parallel, and on lavapipe its sink caught a settings test's Warning in 2 of 5 filtered runs; it is in the "Storage
    log" collection now (6 of 6 then). Filtered runs (Storage, Core, Application*, Infrastructure, `UiHostResourceTests`:
    218) pass on the GPU and on lavapipe. The full suite (rebased on the audio output fixes, bfa17e8): 2319 on the GPU
    (72 s) and on lavapipe (82 s), all passing; after each run the real game folder was still missing and no scratch
    folder was left in `/tmp`. Not checked: Windows ("Windows check", item 7).
    Details: Docs/Notes.md, 2026-11-05.
  - **M10 step 7, Ji'ruan's intro (2026-11-05, on its worker branch):** phase 1 (the stage, approval renders) and phase 1b (the detailed stage and the acting clips in `jiruan-acting.glb`) approved by the user on 2026-11-05 (S7-1–S7-5, "Decisions for M10"); phase 2 built: `Tests/Statventure.Tests/Scene/JiruanIntro.cs` (the sequence over the stage: its clip, camera and sun, the five stand-ins on their marks playing the clip library on the approved schedule, the reveal as a palette cue at the stage's marker, mist and ember particle cues, event cues, the old card's UI track, the still card as the reduced variant; its JSON fixture and effects under `Data/Cutscenes`), `JiruanIntroTests`, `JiruanCardTests` (the card against the live old game in Chromium, `Tools/GameReference/cutscene-card.js`, `make-cutscene-references.sh`), `JiruanStage` and `JiruanRenderTests` (render scenes and Blender's renders; `Scenes3D` gains a clip time and per-model palettes, `render_scenes.py` both), the sandbox's `--cutscene jiruan` (`play-cutscene.sh`; `--dump`), engine approval media. Found and reported, not changed: the canvas's isotropic box antialiasing spreads a `scaleX`-ed bar (three card times held; S7-7, fixed by its own worker); the engine's glow and the Blender harness part ways on overlapping glowing models, and Freestyle on fine scenery. Then, with the user's answers: the harness follows the engine's halo rule (S7-8; `render_scenes.py`, `hold_by_owner`; M6's goldens unchanged), the ink study, the audio track with the user's three sounds (S7-6, S7-9–S7-11; `Data/Cutscenes/audio/effects`, played by the sandbox from `audio.pak`, Release, `--volume`), and the detailed stages' limits (S7-12; `ReferenceLimits.DetailedStage`), under which the six Blender comparisons run and pass. The limits approved as set (S7-13). Rebased on main (aac5523, the audio output fixes and the test-folder redirect): the full suite passes, 2360 tests and the card's three times held, on the GPU (1 min 32 s) and on lavapipe (1 min 30 s); mutations: 53 (`~/.cache/stat-fick-build/claude-work/m10/step7-mutations.json`, made by `make-step7-mutations.py` beside it), 52 killed; the survivor, the reference scenes' hold after a one-shot strip taken out, can't differ at the six sampled times (none falls after a one-shot clip's end before the next strip: the king's gaps are 3.5–3.6 s and 4.8–5.0 s, an imp's fly-out ends as its hover starts); nothing written to the player's real folders. Rebased on main again (e9568b8, the canvas fix, S7-7): the card matches Chromium at 0, 2.5 (1,226 pixels off, 0.15 %), 3.4 (6), 4.6 (25) and 7 s; at 1.4 s 2,133 pixels (0.26 %, over the 0.2 %) differ, nearly all the progress bar's glow under `scaleX(0.2)` (Chromium's blur across the bar about σ 2.7 px, 6 × √0.2, the engine's σ 6) and its two ends (Chromium's spread over two pixels): reported, nothing adjusted, 1.4 s held (S7-14: until the canvas blurs a shadow as Chromium does under a non-uniform scale, ported by its own worker, who un-holds it; M10 ticked once it matches). The full suite passes, 2423 tests and 1 held, on the GPU (1 min 13 s) and on lavapipe (1 min 30 s); the card's mutations re-run, 7 of 7 killed (two new), so 55 mutations, 54 killed (`Docs/Scene.md`, step 7, "Phase 2", "The card at 1.4 s", and Status).
  - **M10 done (2026-11-05, 2516 tests):** all seven steps merged on `main`; since the shadow blur port (`515c128`) the proof's title card matches Chromium at all six moments, so nothing of M10 is held; the proof (Ji'ruan's intro) approved by the user on 2026-11-05. The full suite passes on the GPU and on lavapipe: 2516 tests and 6 held, none of them M10's (the paint fixtures `shadow-scale-x`, `shadow-scale-y` and `shadow-rotate` at both scales, waiting for the inset shadow's port). Mutations over the seven steps: 856, 838 killed (10 of them after closing the gaps their first runs showed, 2 after rewriting a malformed mutation: one in step 5, one in step 6); 1 hangs its test (step 1: a node allowed under itself; `mutate.py` reads the timeout as a survivor); 17 survivors, all accounted for, none able to change a result the checks see (step 1 2 and step 6 2: only work or calls nothing can tell apart; step 2 12: within float32 rounding; step 7 1: at no sampled time). The work beside it has its own entries above: "OpenAL outputs in one process", "Test runs kept out of the player's folders", "Canvas: box antialiasing under non-uniform transforms", "Paint offset of transformed boxes", "Canvas: blurred shadows under non-uniform transforms". Ticked under Milestones; M11 waits for the user's go.
  - **M11 started (2026-11-06):** the user's go ("Once that worker finishes you can start with M11", after the full run on `a32c5cb`); the rest of the detail round asked and answered the same day, M11-9–M11-21, all with the recommended options except the name (M11-19: "Statventure Editor", not "Stat Fick Editor"; its place in the shipped folder and its executable's name left to M12), and the user's rule for M11 (questions about the game to the user, the editor the manager's, big decisions told) ("Decisions for M11"); design doc `Docs/Editor.md`: what exists checked against the code (a record's fields, getters and factories exist but are internal, and `FCurve` can't hold a broken value, so the form edits drafts; the checks are four styles of message without field paths, and a theme's score and the instruments it names are checked only when it plays; references are plain strings; `PackWriter` can't add a record from its binary form, so no pack can yet be rewritten keeping its other entries byte for byte, and no mount holds unsaved data; `ImportContext` works only inside `AddFolder`; no file drop (BRUTAL's `OnDrop` unused) and no close the application can refuse; the audio system's library swap is internal; the listbox and colour input don't work, there is no dialog element or HTML drag and drop), the method, a proposed design with 38 editor choices for the manager (P1–P38) and six questions for the user about the game (G1–G6: the mod info entry's place and kind, mods the game can't fully read, how the port's kinds reach the editor, whether declared limits and the checks run today only at play bind the game, where the editor keeps its settings), and fourteen proposed steps (engine: record shapes, drafts and marks; packs for the editor; window and sound hooks; audio kinds' checks and marks; the other kinds' checks and marks; the mod info entry; editor: the shell; opening packs; the form; checks and references; saving and mods; import; the preview; end to end) with the files each touches and which can run side by side (four waves). Also corrected the same day: `Docs/Assets.md`'s "byte-identical copy" of the old stylesheet (the tests' copy is the stylesheet as captured, `f45ca7a`) and `../old-game/CLAUDE.md`'s list of what the engine's tests read; port tasks 4–6's pull requests (#10, #11) worded as merged here and in `Docs/Port-Tasks/README.md`. No code yet.
  - **M11's design decided (2026-11-06, docs only):** the editor's own choices P1–P38 decided by the manager, all as recommended (`Docs/Editor.md`, "Decided by the manager (P1–P38)"; P18 gains its known limit: a mod that refers to another mod's entries can't be saved by the editor in M11); of the game's questions, G4 and G5 answered by the user with the question tool (the game enforces declared limits too, and only limits it already enforces are declared in M11; the checks that today run only when something plays also run when the data loads, steps 5 and 6 measuring the added loading time first and stopping for the manager's word), G1 and G6 decided by the manager (`mod/info` of the engine's kind `mod.info`, following from M11-15; the editor's settings in an `editor/` folder of the player's game folder, redirected in tests like the other per-user folders), G2 and G3 kept for the port ("Open questions for later milestones", "At port time") ("Decisions for M11"). Full runs across workers are serialized by `flock` on `~/.cache/stat-fick-build/claude-work/full-run.lock` (`Docs/Editor.md`, "Method" and "Steps"). The fourteen steps are the plan; none started. No code yet.
  - **M11 step 1, record shapes, drafts and marks (2026-11-06, 2782 tests, on its worker branch `m11-step1`, rebased on `0d8e175`):** no question came up that the design doesn't answer; the step's own choices (the names, drafts immutable, `Missing` in a field that isn't required meaning its default as an absent JSON key does, limits with exclusive ends, a reference naming its kind by type with its folder and rule from the kind's mark and a condition on one string or enum field, name rules as types, checks on any record type run wherever its values are held, the generic checks on built values) are listed in `Docs/Editor.md`, "How it works", step 1. `Engine/Statventure/Records/`: `RecordShape.cs` (`RecordShape`, `FieldShape`, `FieldLimits`, `ReferenceShape`, display names from code names), `ValueShape.cs` (`ValueShape`, `ValueKind`, `EnumMember`), `RecordMarks.cs` (`Kind`, `Help`, `Unit`, `Limits`, `Choices`, `Colour`, `Bulk`, `References`, `Check`), `NameRule.cs` (`NameRule`, `PlainNames`, `PathNames`), `RecordDraft.cs` (`DraftValue` with `Missing` and `Null`, `DraftLeaf`, `DraftList`, `DraftMap`, `RecordDraft`, `DraftBuild`), `RecordPath.cs` (`RecordPath`, `RecordProblem`), `RecordChecks.cs` (`RecordChecks.Check`, `CheckContext`, `IReferenceLookup`); `RecordLayout.cs` and `RecordCodecs.cs` only expose what they hold (fields, subtypes, the compiled factory and getters, the codecs' parts; `MapCodec.Make` public); no form, layout signature or declaration outside the tests changed. Tests: `Tests/Records/RecordShapeTests.cs`, `RecordDraftTests.cs`, `RecordCheckTests.cs`, marks and checks on the `test.*` kinds in `TestRecords.cs`, misdeclared ones and shapes the guarded kinds lack in `MarkVariants.cs`: all 50 fixtures and audio.pak's 118 records built back to the same binary form, a blank draft of each of the 78 engine record types, every build failure at its path (an `FCurve` refusing its keys at the curve). The full suite passes on the GPU and on lavapipe (2782 on `0d8e175`: main's 2676 and 106 new; 2686 earlier on step 2's `5af923e`), run inside a headless Wayland compositor (`cage` on wlroots' headless backend) so no test window showed on the user's screen. Mutations: 277 (`~/.cache/stat-fick-build/claude-work/m11/step1-mutations.json`, made by `make-step1-mutations.py` beside it, run in four chunks), 266 killed; of the 11 survivors 7 were gaps, closed, 3 redundant code, removed, 1 a gap in code then rewritten; the rerun (`step1-rerun.json`, 16) killed all, so the list as it stands (276) is all killed (`Docs/Editor.md`, Status); nothing written to the player's real folders. For steps 5, 6 and 10: `Docs/Editor.md`, "How it works", step 1, "For later steps".
  - **M11 step 2, packs for the editor (2026-11-06, 2580 tests, on its worker branch `m11-step2`):** no question came up that the design doesn't answer; the step's own choices (names, defaults, refusals) are listed in `Docs/Editor.md`, "How it works", step 2. `Engine/Statventure/Packs/`: `PackWriter.AddFrom(pack, entry, path?)` and `AddFrom(pack, include?)` (an open pack's entry added as it is, read and checked first: content, record flag, a record's binary form in its own version, and its stored bytes and compression carried into whatever pack is written, so Save As keeps them too; `PackWriteResult.Reused` counts them), `Remove`, `Contains`, `AddRecordBinary`; `PackFile.IndexHash`/`IndexHashHex` (P23); the custom mount (P38: `IMountEntries`, `Mount.Custom(name, entries)`, `MountKind.Custom`: paths and kinds taken at mount and Reload, bytes at each read, checked as a pack's index with its words, errors naming the mount's name and the entry); one file imported (P28: `PackWriter.ImportFile(file, sourcePath, ImportSources?, types?, importers?)` giving `ImportedEntry`s, `ImportSources.FileOf` for the files an importer reads besides, by default beside the file on disk, and `Exists` for the data paths, by default none; `ImportContext.Exists`, which `UI/StylesheetImporter.cs` now checks its urls through); `PackWriter.Update` and `packer update <folder> <file.pak>` (the folder's entries added or replacing, the rest kept as stored, urls checked against the folder and the pack, a missing pack, a damaged kept entry or a clash refused with the pack untouched; the other commands' output unchanged). Tests: `Tests/Statventure.Tests/Packs/` `PackRewriteTests` (9), `CustomMountTests` (13), `ImportFileTests` (7), `PackUpdateTests` (4). Found against `Docs/Editor.md`'s design: the writer's existing reuse keeps stored bytes only of the file being replaced, not on Save As to a new file (so kept entries carry theirs). Checked by hand: the packer's commands in a scratch folder (`claude-work/m11/step2-packer-smoke.sh`). The full suite passes on the GPU (1 min 13 s) and on lavapipe (1 min 39 s): 2580 tests (`main`'s 2547 and 33 new), none skipped; the player's real game folder stayed missing and the Documents folder's top level unchanged. Mutations: 68 (`~/.cache/stat-fick-build/claude-work/m11/step2-mutations.json`, made by `make-step2-mutations.py` beside it), all killed, nothing written to the player's real folders; two lines were taken out while writing the list, as nothing could tell them apart (`AddRecordBinary`'s own path check before the writer's, and the default lookup's separator replacement: `Path.Combine` takes `/` on Linux and Windows), and one candidate left out as unable to differ (an importer's own source file looked up like the others: the lookup gives the source itself). Not checked: Windows (writing over a pack held open; a case-insensitive file system under the import's default lookup). Details: `Docs/Editor.md`, "How it works", step 2; `Docs/Assets.md`, "Packs for the editor".
  - **M11 step 4, the editor's shell (2026-11-06, 2676 tests, on its worker branch `m11-step4`):** the Statventure Editor's program and shell in `Tools/Editor` (`Editor.csproj`, assembly `Statventure.Editor`, in `Statventure.slnx`, referenced by the test project; P1): the command line (`EditorArguments`: `--repo <engine folder>` the dev setup on the repo's packs, `--game <folder>` or `packs/` beside the program read-only, `--player-folder`; P5; no `ui.pak`, no start), the window (`EditorApplication`: 1280 × 800, at least 800 × 500, no sound until step 13, the view's title and page zoom through `UiHost`), the session's skeleton (`EditorSession`, a partial class later steps add files to: the game's data, the open pack's name, sources of unsaved work, change notices, what it owns), the view (`EditorView`: the top bar with New mod, Open, Save, Save As, Import, Undo, Redo, the three panes with splitters dragged by the pointer under its capture in whole CSS px, the status line; the old game's dark-mode colours with the ember accent, Lato 13 px from `ui.pak` through `@font-face`, rows 24 px; P4), parts found in the editor's assembly with no list (`EditorPart`: a pane or none, an order, an element of its own; P2), commands (`EditorCommands`: handlers in order until one acts, the top bar's buttons enabled while one can run) and the shell's keys (Ctrl+S, Ctrl+Shift+S, Ctrl+O, Ctrl+Z, Ctrl+Y and Ctrl+Shift+Z, Ctrl+F, F2, Delete, Ctrl+= and Ctrl+-; Delete in a text field is the field's, Undo from a text field says so for P13), the zoom (half a zoom level a step as the old game's View menu, 53–299 %), dialogs (`EditorDialogs`: over the shell, the rest `inert`, the focus as Chromium's modal dialog gives it and kept inside, Escape cancels, the focus given back), the title ("Statventure Editor — <pack>", "• " first while there is unsaved work). Chromium fixtures per part (`IEditorFixtures`, found in the test assembly; the run writes `Reference/editor-fixtures.json`; `Tools/Scripts/make-editor-references.sh [names]` captures with PaintReference at 1 and 1.65 into `Golden/editor-<name>@<scale>.png`; the writer puts the document's state into attributes and a surface as an empty box, P36); `Tools/Scripts/editor.sh` runs it. The empty shell and a dialog match Chromium at both scales at the paint fixtures' limits (shell 0.068 % at 1×, 0.048 % at 1.65; dialog 0.061 %, 0.034 %; the limit 0.2 %). The conventions steps 8–13 follow, and the choices the design left to the step, are in `Docs/Editor.md`, "How it works", step 4. Found, reported to the manager, the engine not changed: text that is the first drawing of a paint chunk is drawn with the previous chunk's LCD verdict (`PaintTarget` makes the chunk and sets `Canvas.TextLcd` only when the item ends, after its text is drawn), so the status line's text (in a box with `overflow: hidden`) is grayscale where Chromium's is LCD (within the limits); and Chromium's captures are made in an iframe, whose root isn't composited as the editor's main frame is, so a fixed box over the root is a layer of its own only in the engine (the shell's first, fixed, version failed at 1× on its top bar's text): the editor's stylesheet keeps out of layers of their own (convention 2). Also found: `Application.Run` mounts the player's mods for every application, so a damaged mod stops the editor's start. Both engine points are handled outside this step (the manager, 2026-11-06): the LCD bug by a worker of its own on the painter (repro `claude-work/m11/step4/ScratchLcdRepro.cs.txt`), a start without the player's mods by step 3 (`Core/Application.cs`), which `EditorApplication` takes up in a later step. Tests: 79 in `Tests/Statventure.Tests/Editor/`. The full suite passes on the GPU (1 min 14 s) and on lavapipe (1 min 27 s): 2676 tests (`main`'s 2597 at `2323779` and 79 new; the four screens compare with Chromium as they did on `5af923e`, the background-layer port changing none of their pixels), none skipped; the player's real game folder stayed missing and the Documents folder's top level unchanged. Mutations: 189 (`~/.cache/stat-fick-build/claude-work/m11/step4-mutations.json`, made by `step4/make-mutations.py` beside it), 183 killed (3 of them after closing the gaps the first run showed: a test reading a dialog's value that a broken rule never gave hung the run, the splitter's own width at the limit, a relative player folder; `step4-mutations-rerun.json`), nothing written to the player's real folders; 6 survivors, none able to change what the checks see: the other stylesheets' order and the editor's own parts (none exist before step 8), and four looks under the screens' limits (the focus ring 1 px narrower, about 0.02 % of the pixels; the dialog layer 10 % lighter, 3–4 levels; the dialog title 2 px smaller on its fixed 24 px line, under 0.2 %; the dialog layer fixed, whose text lies on the box's own opaque background and keeps LCD). Remembering the zoom and the window's size (P4, G6): step 8, with the other `editor/` settings (the manager's decision (a), 2026-11-06; `Docs/Editor.md`, "Steps", step 8). Not checked: the program in a real window (no window may be shown on the PC: to check by hand), Windows.
  - **M11 step 7, the mod's info entry (2026-11-06, 2796 tests, on its worker branch `m11-step7`, rebased on `65ee082`):** no question came up that the design doesn't answer; the step's own choices (names, each field's default) are listed in `Docs/Editor.md`, "How it works", step 7. `Engine/Statventure/Packs/ModInfo.cs`: the record `ModInfo`, the engine's kind `mod.info` version 1 (G1, M11-15), `Name`, `Author`, `Version`, `Description` (fields 1–4), each a `string` defaulting to empty text (any may be empty, a key left out of a hand-written JSON form is empty text), no check and no loader; `ModInfo.Path` = `mod/info`. Registered as every kind is, by its `[Record]` attribute (`RecordTypes.Engine` finds the engine's kinds by reflection; there is no list of kinds), so the dump, `extract`, `AddFolder` and the packer read it as any record. Its layout `Data/Records/Layouts/mod.info.txt` and fixtures `Data/Records/Fixtures/mod.info/` (`every-field`, `empty`) under the layout guard. **A mod carrying it changes nothing the game does:** mounting reads a pack's index, not its records; the game reads data by its own paths and lists only the music's folders under `audio/`; the log names a mod by its file; the committed packs hold nothing in `mod/`. Tests: `Tests/Statventure.Tests/Packs/ModInfoTests.cs`, `ModInfoTests` (6: the kind and path, both forms, the exact JSON, keys left out, the dump, extract and pack again, loading from a pack and a loose folder) and `ModInfoModsTests` (7, run alone as they read the log: the game's mounts from `Application.DataMounts` over copies of the committed `audio.pak` and `ui.pak`, `classes/` and two mods give the same logged "Data" lines, paths, kinds, origins, contents and music with the entry in both mods, in one, in neither; an entry the engine can't read, five ways, changes nothing until read and is then an error naming the mod and the entry, G2 as today; nothing in `mod/` in the committed packs). The full suite passes on the GPU (1 min 12 s) and on lavapipe (1 min 32 s), both run inside a headless Wayland compositor (`cage`, `WLR_BACKENDS=headless`, the user's displays unset; `claude-work/m11/step7-headless.sh`, `step7-full-run.sh`) so no window reaches the user's screen: 2796 tests (`main`'s 2782, 13 new, and one more case of step 1's blank-draft test over every engine kind, `mod.info`'s), none skipped; step 1's tests over every engine kind and fixture pass on `mod.info` (no marks, no `[Kind]`: its display name "Mod info" comes from the code name, and it has no folder, being one entry at a fixed path); the manager confirmed the fields' empty-text defaults, the names and "no helper" (2026-11-06); the player's real game folder stayed missing and the Documents folder's top level unchanged. Mutations: 54 run (`~/.cache/stat-fick-build/claude-work/m11/step7-mutations.json` and `step7-mutations-2.json`, made by `make-step7-mutations.py` and `make-step7-mutations-2.py` beside them), 43 killed, none survived, nothing written to the player's real folders: the kind's name, version, attribute and visibility, five paths, each field's number, attribute, name, nullability, required flag and default, and four ways of mounting reading or checking a mod's info; 11 of the first list didn't build (they broke the tests' own code) and the 8 renames and `required` were rewritten to build, all killed; the three type changes (`string[]`, `int`) have no form the record's users still compile against. Not checked: Windows (nothing of the step depends on the system). Details: `Docs/Editor.md`, "How it works", step 7; `Docs/Assets.md`, "The virtual file system".
  - **M11 step 9, the form, undo and the JSON view (2026-11-07, 3089 tests, on its worker branch `m11-step9`, rebased on `6a8cd78`, its full runs on `3e6df0f`):** the session's entries and the form over them (`Docs/Editor.md`, "How it works", step 9, with the API step 8 calls): `Tools/Editor/Session/EditorEntry.cs` (a working entry: path and saved path, source Pack, Game or New, a record, file or unreadable entry, its draft with its own history of drafts, the current build, the last value that built, its problems, changed meaning a different binary form) and `EditorSession.Drafts.cs` (the set shown with the pack's entries over the game's, `Writable`, the selection, commits, undo and redo per entry (M11-7), a hook for step 10's checks, New, Duplicate, Rename, Remove and "Revert to the game's" (P7, the manager's decision of 2026-11-06), each reported); `Tools/Editor/Form/` (`FormPart`: the head with the entry's path, kind and actions, the Form and JSON tabs, Undo and Redo with a field's own typing undone and redone first, P13; `FormView`, `ValueEditors`, `Slot`, `ValueText`: a row per field of every value shape (P11), rebuilt in place, numbers committed on Enter, leaving the field, a spin or an arrow key, a slider's drag one step, text that doesn't read kept, marked and not committed (P12), problems under their rows (P16); `EntryActions`: the dialogs asking first, F2 and Delete, a hook for step 10's referrers; the JSON tab with copy and export through an `IExportFilePicker` step 8's file browser implements (M11-18, P22)). The manager's decisions of 2026-11-06 recorded in `Docs/Editor.md` (the revert; New offering every plain kind until kinds have folder marks; Export disabled until the browser exists; the JSON tab at most 2,000 lines; Delete only from outside the form's rows; list elements from 0; a red of the editor's own for problems). Screens against Chromium (P36, the paint fixtures' limits, 12 levels and 0.2 %): `form-controls` and `form-controls-end` (the form in two halves, the second scrolled) 0.108 % and 0.139 %, 0.115 % and 0.155 % at 1 and 1.65, `form-json` 0 and 0.001 %, captured inside a headless `cage` (one fixture per run at 1.65: a run stalls after its first there; `Docs/Editor.md`, "Method"). Found, reported, the engine not changed: a select with its own padding is 6–7 px wider in the engine than in Chromium (most of the form screens' differences; open in "Small tasks left from earlier milestones", no go yet). Two of step 4's tests that assumed the editor had no parts of its own now look at their own. Tests: 149 in `Tests/Statventure.Tests/Editor/Form/`. The full suite passes on the GPU (1 min 16 s) and on lavapipe (1 min 37 s), inside a headless Wayland compositor: 3089 tests (`main`'s 2940 on `3e6df0f` and 149 new), none skipped; the player's real game folder stayed missing and the Documents folder's top level unchanged. Mutations: 231 run (`~/.cache/stat-fick-build/claude-work/m11/step9-mutations.json`, made by `make-step9-mutations.py` beside it; four chunks, then two reruns, `step9-rerun.json` and `step9-mutations-rerun2.json`): 207 killed at first; of the 22 that survived, 16 were gaps closed by new tests or assertions (a new entry shown, the removed list on a new set, a removal and a rename alone as unsaved work, an untouched entry whose record can't be written, an alpha typed into a Vector3, a new integer key above the largest, a colour text that isn't one in the swatch (two), a member's display name of two words, a union's switch between fields of one name and two kinds, a map entry removing itself, Undo and Revert once the pack takes no changes (two), a rename to the same name, a cancelled dialog's additions: with it, a same-name rename no longer runs them), 3 were redundant code, removed (a game entry's history cleared on a revert, though it was never shown; a field's mark cleared twice; the read-only guards no event reaches), and 3 are looks under the screens' limits (the problems' red, the selected tab's underline, the head's bold path: a few hundred pixels each where the form screens are at 0.11–0.16 % of the 0.2 %); 2 didn't build and were rewritten; the reruns (24) killed all, so the list as it stands (228) is all killed but those 3 looks; nothing written to the player's real folders. Renders for the user: `claude-work/m11/step9/render-*.png`. Not checked: the editor in a real window (to check by hand), Windows.
  - **M11 step 6, the scene, model, particle, terrain, state machine and stylesheet kinds' checks and marks (2026-11-07, 3156 tests, on its worker branch `m11-step6`, rebased on `3ee8d7b`):** each kind's existing rules are its `[Check]` (P20) with the path of what they name and their words unchanged (`Scene/Sequence.cs`, `Scene/FCurve.cs`, `Render3D/Particles.cs`, `Render3D/ModelData.cs`, `Render3D/Terrain.cs`, `Animation/StateMachineDefinition.cs`, `UI/Css/StylesheetRecords.cs`, `UI/Style/Stylesheet.cs`): `Validate()`/`Problem()` give the first problem of `RecordChecks.Check` (the kind's own rules, then the declared limits), so every logged message is as it was; a check goes on past a problem (a sequence's role, stage, track field, strip, cue and span each a part; a model's, a machine's and a terrain's rules skipping what an earlier one found unreadable); an `FCurve`'s rules for edited keys are `FCurve.ProblemsOf` and a compiled stylesheet's are `Stylesheet.FromRecord` read in a reporting mode. Marks: kind names and folders (the folder the data is read from today: `world/cutscenes`, `models/effects`, `models`, `world/terrain`, `ui/styles`; none for state machines and curves; the manager's rule, 2026-11-06), help texts and units on every particle effect and cutscene field, 34 limits each held by the kind's own rule already (G4; a test puts a value just outside each into a valid sample and sees the kind's own words first), bulk (a model's meshes, skins, roots, channels and hierarchy, a terrain's samples and covers, a sheet's rules), colours (a material's base colour and emission, a palette cue's fill), references (a sequence's stages, a particle cue's effect, and, once rebased on step 5, an audio cue's name: an effect patch or a theme by its kind, by step 5's music rule); the marks recorded per kind in `Tests/.../Data/Records/Marks/` and guarded as layouts are. The sequence against the data (G5): its stages read from it, the names in them, its particle effects, effect patches and themes (by the music's names) as `Play` checks them. **Measured, then decided by the user (2026-11-07, the question tool):** the proof's `Sequence.Load` (its stage's pack, `Data/Cutscenes`, `audio.pak`; Release; the branch's base, the limits only, the limits and the data check):

    | | base `42fdd90` | limits | limits and data check |
    | --- | --- | --- | --- |
    | cold, fresh process (30 runs, median) | 137.1 ms | 159.0 ms (+21.9) | 191.6 ms (+54.5) |
    | cold after the music library and effects | 110.3 ms | 122.0 ms (+11.7) | 122.7 ms (+12.4) |
    | warm (500 loads, median) | 12.06 ms | 12.15 ms (+0.08) | 12.24 ms (+0.17) |

    The user wired both into loading and gave the loaders that built the proof's data without `audio.pak` the sound pack (the sandbox's cutscene mode, `JiruanIntroTests`; the test sequence's load test gets its effect, sounds and the sound pack): `Sequence.Load` refuses a sequence naming an effect, patch or theme the data hasn't, in the sequence's words, and a broken one is its own error naming its entry. The sandbox's cutscene mode, run headless at volume 0 with a scratch player folder, loads the proof with its sound bound ("Decisions for M11", "Cutscenes' checks in loading"). Decided by the manager on the report (2026-11-06): a broken value reported twice (a limit and the kind's own rule; a reference and a data check) stays so, the editor showing the first problem per field (step 10); `AudioCue.Name` marked with step 5's rule once both are merged (done here after the rebase, being trivial); `RecordChecks` walking every nested record (61 ms on the compiled game stylesheet, Debug) left as an API gap for step 1's author, so `Stylesheet.Load` runs none. Tests: `Scene/EngineKindMarksTests.cs`, `Scene/SequenceDataCheckTests.cs`, every existing rule's test with its path, robustness tests per kind; step 1's `RecordShapeTests` (the F-curve's name now "F-curve") and `RecordDraftTests` and step 9's `EngineKindsFormTests` (their fillers give a bulk field its object; a kind of bulk fields only is not edited) adjusted. The full suite passes on `3ee8d7b` on the GPU (1 min 17 s) and on lavapipe (1 min 38 s), headless (`cage`; `claude-work/m11/step6-full-run.sh`, which ends its own cage if it lingers after `test.sh`): 3156 tests (`main`'s 3089 and 67 new), none skipped; the player's real game folder stayed missing and the Documents folder's top level unchanged. The first GPU run had one failure outside this step, `Ui/ScriptApiTests.WorkFromAnotherThreadComesBackAsATaskOfTheLoop` (a continuation from a pool thread must reach the document's loop within its 200 frames of 5 ms; other workers' builds and tests ran beside it), passing in the second GPU run and on lavapipe: reported, its cause not found. Mutations: 347 (`~/.cache/stat-fick-build/claude-work/m11/step6-mutations.json`, made by `make-step6-mutations.py` beside it; five chunks and a rerun): 344 in the first run, 333 killed; of the 11 survivors 7 were gaps, closed by new tests (a visibility track's node at its path, a curve made with several problems refused with the first, a terrain without a grid not compared with its counts, a stylesheet's value token, unquoted url, keyframe url and a keyframe's refused second declaration at their paths), 1 was code rewritten (the stylesheet's url check scanned @font-faces too, reporting a face refused for its src url twice: it now leaves them to `FontFaceDescriptors.Of`), and 3 can't change what any check sees (`Sequence.Validate()` and `Validate(stages)` without the declared limits: every declared limit is one the kind's own rule holds first, as the G4 test shows; each data entry read once per check, a cache); the rerun (11: the 7, the url check's new mutation and the audio cue's three reference marks) all killed. Nothing written to the player's real folders. Not checked: Windows. Details: `Docs/Editor.md`, "How it works", step 6; `Docs/Scene.md`, "M11 step 6"; `Docs/Assets.md`, "Checks with paths and marks".
  - **M11 step 3, the window, the sound and the file browser's model (2026-11-06, 2825 tests, on its worker branch `m11-step3`, rebased on `d5b8b39`):** no question came up that the design doesn't answer; the step's own choices (names, the drop as a window event, the browser's rules, `classes/` left out by `GamePacksOnly`) are listed in `Docs/Editor.md`, "How it works", step 3, all confirmed by the manager the same day. `Platform/Window.cs`: `Window.PathsDropped` (`event Action<IReadOnlyList<string>>`, raised from `PollEvents` in GLFW's drop callback, the paths as GLFW gives them, in its order; not an input event, so `Input` and the UI never see a drop; nothing in the game or the sandbox listens) and the test hook `MinimizedForTests` (internal). `Core/Application.cs`: `protected virtual bool ConfirmClose()` (true by default, ending the loop exactly where it always ended), asked once per close request before the next frame and, while minimized, after the update; false clears GLFW's flag and keeps the loop running; `Quit()` doesn't ask; and, added on the manager's word the same day, `ApplicationOptions.GamePacksOnly` (`Application.Data` the area packs only: no `classes/`, mods or development folder, so a damaged mod can't stop the editor's start; Reload the same; off by default). `Audio/AudioSystem.cs`: `Reload(MusicLibrary)`, S9-D's swap, public (for the preview's edited themes). `Storage/FileBrowser.cs`, `RecentPlaces.cs` (P26): `FileBrowser.Places` (mods folder, Documents, home, roots, recent places that are there), `List` (folders first, names ignoring case with an ordinal tie-break, files by endings in any case, hidden when asked), `ReadPath` (`TypedPath`: folder, file, new, missing, invalid; relative to the folder shown, `~` the home folder), `Complete` (`PathCompletion`: the matches' shared start, a separator after a single folder), `RecentPlaces` (ten, most recent first, kept by the caller). Tests: `Platform/WindowDropTests` (3: GLFW's own drop callback taken from GLFW and called as on a drop, on a hidden window), `Core/ApplicationCloseTests` (6) and `ApplicationGamePacksTests` (2) on real applications in hidden windows counted as minimized (nothing shown, nothing presented), `Core/AudioReloadTests.TheSwapIsPublic`, `Storage/FileBrowserTests` (9) and `FileBrowserDocumentsTests` (1) on scratch folders and the run's redirected per-user folders. Found and handled on the manager's word: the tests that made windows before opened visible ones for a moment (`WindowOptions.Visible` is true by default, the game's window, unchanged); **tests make their windows hidden now** (`engine/CLAUDE.md`): `TestApplication` (`ApplicationReloadTests`, `ApplicationStorageTests`), `ApplicationMusicTests` and `UiResourcesTests`, besides this step's; checked on the GPU and on lavapipe under Wayland and X11 with a read-only KWin script logging no window added on the real desktop, and in a headless wlroots compositor (`cage`). **The swapchain test stays visible**: its five vsync frames on a hidden surface pass on KWin but hang in wlroots, which never lets the second through (hiding it would test a compositor's leniency; no vsync would test less); and a hidden window presenting beside it stalled it too (under wlroots, with `ApplicationMusicTests`' window hidden and the two in parallel: 5 hangs in 10 runs, none with that window visible; GLFW is one for the process and not thread-safe), so `SwapchainTests` and `ApplicationMusicTests` joined the non-parallel "Window" collection and every test that presents to a window runs alone (10 of 10 runs of the window tests passed in cage). `FolderListing` lists a folder it can't read as empty (`IgnoreInaccessible`): its comment, which named an `UnauthorizedAccessException`, is corrected. GLFW 3.4, the system library the engine loads on Linux, implements drops on Wayland too (BRUTAL's comment is GLFW 3.3's). The full suite passes on the GPU (1 min 20 s) and on lavapipe (1 min 34 s), each inside a headless Wayland compositor (`cage`, as step 1's worker found: nothing reaches the screen): 2825 tests (`main`'s 2803 at `d5b8b39` and 22 new), none skipped; the player's real game folder stayed missing and the Documents folder's top level unchanged. Mutations: 84 (`~/.cache/stat-fick-build/claude-work/m11/step3-mutations.json` and `step3-mutations-2.json`, made by `make-step3-mutations.py` and `-2.py` beside them), 83 killed, nothing written to the player's real folders; in the first run "the default refuses" only timed out (its test's watchdog then woke the loop but didn't end it: mended, killed in the second run) and "Quit asked before the next frame" survived until a test met a close request and `Quit` at that check (another thread making both while the loop sleeps); one accounted for: a folder removed between a completion's reading and its listing (only a race reaches that catch). One equivalent candidate was taken out while writing the list (`Path.Join` adds no separator before one). Not checked: a real drop and a real close (title bar, taskbar while minimized) by hand under X11, Wayland and Windows (`claude-work/m11/drop-check.sh` runs a window that logs both); Windows. Details: `Docs/Editor.md`, "How it works", step 3.
  - **M11 step 5, audio kinds: checks and marks (2026-11-07, 2940 tests, on its worker branch `m11-step5`, rebased on `1f598ec`):** one question came up, the step's condition: wiring the theme check into loading was measured first and the step stopped; **the user decided on 2026-11-07 (the question tool) to wire it** ("Decisions for M11", G5), with the numbers shown and both consequences accepted (below). `Engine/Statventure/Audio/`: `Music/ThemeCheck.cs` (new: `ThemeCheck`, a theme against the composer without playing it, each problem at the path of what it names in the score's and composer's words: the room, the score compiled once and walked part by part only where it fails, each section the form plays and its layers by the composer's rules for their type; `MusicParts`, a library's parts or the data's by the music's rule), `Music/ThemeData.cs` (kinds with folders and `MusicNameRule`, help on every field, units, choices, references, `[Check]`s on `ThemeSpec` and `ThemeOrder`), `Effects/EffectPatch.cs` (kinds, help, units, the four limits its check held, G4; its `[Check]` with the old words at paths; `Problem()` the first of `RecordChecks.Check`), `Music/MusicLibrary.cs` (`MusicNameRule`; the room's and the order's rules shared with loading, `RoomProblem`, `OrderProblems`; **`Load` checks each theme after its older checks, the first problem naming the pack, entry and field, and keeps the themes it compiled**; `ThemesCompiled`), `Score.cs` and `Composer.cs` only exposing (three of the score's functions internal; `ComposerNames`). The measurement the user saw (Release, the committed packs mounted as the game mounts them, `MusicLibrary.Load` alone, no sound device, this PC):

    | | Before | With the check |
    | --- | --- | --- |
    | Cold start (15 fresh processes, medians) | 48.9 ms, and 17.2 ms of compiling all 55 themes a moment later (the warm-up thread, or the first theme played) | 69.6 ms, the compile included, none later |
    | Warm (200 loads in one process, each from a collected heap, medians) | 1.4 ms (3.4 ms of compiling later) | 4.5 ms |
    | Reload (remount and load, medians) | 3.8 ms (3.4 ms of compiling again when the theme playing changed) | 8.6 ms |

    So 20.7 ms more on the game's thread at the start and 3.5 ms more work in all, about 3 ms more warm, about 5 ms more on a Reload. The consequences the user accepted: a layer that never sounds (no bar of its cycle, a pattern of rests) naming an instrument that isn't there is refused; and four of `MusicLibraryTests`' libraries, whose sample theme doesn't compile (its melody's bars hold 3 of the meter's 12 steps), are given a playable theme with every field still set (the record fixtures keep the sample; a test shows its refusal at load). Before, a theme that didn't compile failed the audio system's warm-up thread at the start (an exception on a background thread ends the process); now loading stops naming its entry. Everything that loaded before still loads: the committed `audio.pak` (55 themes, 118 records), the tests' other data, the proof cutscene's three patches; every music test is unchanged (no line of the score's or the composer's logic changed). Tests: `Tests/Statventure.Tests/Audio/ThemeCheckTests.cs` (93), `AudioMarksTests.cs` (19), `MusicLibraryTests` (3 new). The full suite passes on the GPU (1 min 13 s) and on lavapipe (1 min 36 s), both inside a headless compositor whose wrapper ends its own `cage` when its command does (`claude-work/m11/step5-headless.sh`, `step5-full-run.sh`; a `cage` that outlived its command once held the shared lock): 2940 tests (`main`'s 2825 and 115 new), none skipped; the player's real game folder stayed missing and the Documents folder's top level unchanged. Mutations: 124 (`~/.cache/stat-fick-build/claude-work/m11/step5-mutations.json`, made by `make-step5-mutations.py` beside it), all killed, nothing written to the player's real folders. Found: step 1's `CheckContext` gives a check no entry path, so the theme's id rule stays in `Load`; the editor will see one mistake reported twice where a declared limit, a choice or a reference covers a kind's own rule (step 10). The step's own choices are listed in `Docs/Editor.md`, "How it works", step 5. Not checked: Windows.
  - **M11 step 8, opening packs: the browser, drops and the entry list (2026-11-07, 3250 tests, on its worker branch `m11-step8`, rebased on `22ee667`):** the manager's decisions of 2026-11-07 at the step's questions recorded in `Docs/Editor.md`, "How it works", step 8 (the `editor/` settings as one small JSON file per concern, `shell.json` and `recent-places.json`, read tolerantly and written whole; the file browser a dialog over the shell; the window's last normal size remembered, at least 800 × 500); the step's other choices listed there. `Tools/Editor/Session/EditorSession.Opening.cs`: one pack opened over the game's packs (P6; `Session.Open`, `OpenPack` with the index hash as opened, `PackFile` kept for saving, `Base` of the game's other packs, `GamePacks` in the game's order, `PlayerFolder`, `ModsFolder`, `SettingsFolder`), every entry read and classified by `EditorSession.ReadEntry` into step 9's `EditorEntry` (a record, a file, or unreadable with its reason in the pack dump's words: damaged, not a record, a kind not known, a newer version, values its type refuses), read-only when any can't be read (M11-16) or when it is one of the game's packs outside the dev setup (M11-5), a game pack that can't be read left out and named, and the import queue (`QueueImports`, `TakeImports`, `ImportsQueued`) step 12 takes. `Tools/Editor/Settings/EditorSettings.cs` (`EditorSettings`, `ShellSettings`, `RecentPlacesFile`). `Tools/Editor/Browser/`: the file browser (`FileBrowserPart`, `FileBrowserDialog`: places with the game's packs, a folder's listing with sizes and dates, Up, "Hidden", the path field with Tab completion, the endings of what is picked, recent places written at each pick, an export's replace question; it is step 9's `IExportFilePicker`) and `Opener` (Ctrl+O, a refused pack said with why, the unsaved-work question when opening and closing with `SaveFirst` for step 11, drops: the first `.pak` opens, other files queued for import, P27). `Tools/Editor/Entries/`: the entry list (P8: a tree by path, the kinds' names, the marks ↑ + • ! ×, the game's dimmed, the filter, the keys and type-ahead, an active item apart from the selection), its rows made only where seen (P37: measured first on 2,000 entries, Release; painting a frame of an open tree took 109 ms with every row an element, 1.4 ms now; the filter and an added entry 99–106 ms, now 3–4 ms). Step 4's two hooks: `EditorView.ZoomLevel` settable at start, and `EditorApplication` (the window's size from `shell.json` before `Run`, `GamePacksOnly = true`, `ConfirmClose`, drops, `shell.json` written at close). The screens against Chromium (P36) at 1 and 1.65 (pixels past 12 levels, the limit 0.2 %): `list-marks` 0 % and 0.002 %, `list-unreadable` 0 % and 0.001 %, `browser` 0.004 % and 0.008 %; every editor fixture shows the entry pane, so the shell's, the dialog's and step 9's screens were captured again too, as close as before (`shell` 0 and 0.002 %, `dialog` 0.009 % and 0.006 %, `form-json` 0 and 0.001 %, `form-controls` 0.108 % and 0.139 %, `form-controls-end` 0.115 % and 0.155 %), inside a headless cage with its own Xwayland (`claude-work/m11/step8-capture.sh`; no window on the user's screen). Found and fixed: `Tools/PaintReference` put its window at -20000 DIPs, out of X11's 16-bit positions at a scale of 1.65 (-33000 px), so under a cage's Xwayland the window never got the focus and the 1.65 captures had no focus rings; -10000 now (the dialog's box at 1.65 then pixel for pixel step 4's capture). Found, the engine not changed: the painter paints every row of a scroll container, seen or not (what made P37's list slow). Step 4's and step 9's tests that assumed no Open handler, no file browser, or the dialog first in the fixture list now see this step's parts. Tests: 94 in `Tests/Statventure.Tests/Editor/Opening/`. The full suite passes on the GPU (1 min 15 s) and on lavapipe (1 min 41 s), inside a headless Wayland compositor: 3250 tests, none skipped; the player's real game folder stayed missing and the Documents folder's top level unchanged. Mutations: 251 in the first run (`~/.cache/stat-fick-build/claude-work/m11/step8-mutations.json`, its chunks `-1` to `-4` and `-window`, the editor application's wiring run inside a headless cage; made by `step8/make-mutations.py`): 229 killed, 18 survived, 4 didn't build; 3 survivors were redundant code, removed (the tree's press handler: a press focuses the tree anyway; Right's check that an open folder's next item is inside it, which it always is; a `[hidden]` rule the browser's own stylesheet has), 12 were gaps closed by tests (a game pack's problem named without its folder, a compressed file's size, a settings file that isn't an object, the "Hidden" box kept when going to a folder, the selection kept when listing again, a size between 1,000 and 1,024 of a unit, a dialog's task read only once done (one survivor was a test waiting on a question a broken rule left open), a refused drop's own task, a relative name dropped, the status line's capital, the overscan's number, and the window's options in the settings tests' filter), and the 4 that didn't build were rewritten to build; the rerun (`step8-mutations-rerun.json`, 16) killed all 16. The list as it stands, 248, has 245 killed and 3 accounted for: looks under the screens' limits (the marks not ember, the active row's 1 px outline, an open folder's triangle closed: a few hundred pixels of 640,000). Nothing written to the player's real folders. Renders for the user: `claude-work/m11/step8/render-{list,browser}-audio@{1,1.65}.png` (the committed `audio.pak` open). Not checked: a real drop from a file manager and a real close by the title bar (by hand), the editor in a real window, Windows. Details: `Docs/Editor.md`, "How it works", step 8.
  - **The flaky ScriptApiTests: work stuck in a blocked pool thread's queue (2026-11-07, 3250 tests, none new, a worker
    worktree, branch `flaky-script-api-task` on `ac2306e`):** the cause of
    `Ui/ScriptApiTests.WorkFromAnotherThreadComesBackAsATaskOfTheLoop` failing once in M11 step 6's first full GPU run
    (other workers' builds and tests beside it) was the test's, not the engine's. xunit 2.9.3 (its default "conservative"
    parallel algorithm) runs test methods on .NET's thread pool, so the test's thread, the document's loop thread, is a
    pool worker; `Task.Run` called on a pool worker puts the work into that worker's own local queue
    (`ThreadPoolWorkQueue.Enqueue`), and the test then blocked that worker (`started.Wait(10 s)`, then 200 frames, each
    after a `Thread.Sleep(5)`). A worker's local items run only when another worker steals them, which a worker does only
    after finding its own queue and the global queue empty. In a busy full run every worker is inside a test that blocks
    (the pool's own counters, read every 100 ms while the work waited: as many threads processing as the goal, 5 to 101
    more items queued), so the work waited until the pool added a thread for starvation, one at a time and only after
    500 ms without any dequeue (goal × 1 s, 11–21 s, while the test process uses 80 % of the CPU or more, as lavapipe's runs
    did, 71–90 %; `PortableThreadPool.GateThread.cs`; only `Task.Wait` tells the pool a worker is blocked, not
    `ManualResetEventSlim.Wait` or `Thread.Sleep`), and every item queued before it had been taken. The test ignored the
    10 s wait's result and then gave the continuation the 200 frames (at least 1 s), so work started about 11 s late or
    more failed it ("Collections differ": only "before"). The engine marshals it correctly and was not changed:
    `UiEventLoop.Post` from another thread queues a task under a lock and wakes the host, and each turn of the loop runs
    every task posted before it. Measured with the test's steps instrumented (550 runs spread through three full runs):
    from `Task.Run` to the pool starting the work median 0.03 ms, over 1 s in 12 runs, at most 1.36 s on the GPU, 4.8 s
    on lavapipe and 5.3 s on the GPU with 16 busy loops; from the work's end to the post at most 0.13 ms; the
    continuation always ran in the first frame after the post. Natural runs, the test as it was and as it is in turn
    through a full run on lavapipe with 16 busy loops (150 each): the old one took up to 6.2 s, the new one at most
    0.38 s, none failed. Forced, the state the counters showed made certain (12 blocking pool items queued ahead of each
    run, in one test host): the old test failed 20 of 20, each after 11.0 s; the new one passed 20 of 20 in 0–2 ms, and 5
    of 5 with 40 queued ahead. Fix, in the test only: the work runs on a thread of its own
    (`TaskCreationOptions.LongRunning`), held until the script's await has handed its continuation to the loop; the test
    waits for the post itself through `UiEventLoop.Wake` (the signal an application's loop sleeps on), with a 30 s
    timeout and a message, then checks that the continuation hasn't run before the loop's next turn and runs in that turn
    on the loop's thread (stricter than before: the next frame, not within 200). Mutations: 6 (`mutate.py`,
    `~/.cache/stat-fick-build/claude-work/flaky-script-api/mutations.json`, made by `make-mutations.py` beside it), 6
    killed, each by this test alone: a post from another thread run there at once (91 ms), made a microtask, not waking
    the host, the thread check inverted (each the 30 s timeout), the loop's turn not running posted tasks (92 ms), and the
    fix undone (the work back on the pool: under 40 blocking items queued ahead, "the work's continuation was neither
    posted to the loop nor run within 30 s", 2 of 2; the reproduction's own test class, kept out of the repo as
    `TempScriptApiRepro.cs.txt` there). Nothing written to the player's real folders. The full suite passes on the GPU
    (1 min 25 s) and on lavapipe (1 min 54 s), headless (`claude-work/flaky-script-api/full-run.sh`): 3250 tests, none
    skipped; the real game folder stayed missing and the Documents folder's top level unchanged. The same pattern elsewhere (listed, not changed): `Core/ApplicationCloseTests.
    QuitWithACloseRequestedEndsTheLoopBeforeTheNextFrameWithoutAsking` queues `Task.Run` from the application's loop (the
    test's pool thread), which then sleeps up to 60 s; it runs in the non-parallel "Window" collection, with the pool
    idle. `Storage/SaveSlotTests` (`ListingAndLoadingWaitForPendingSaves`, `DisposingFlushesAndThenRefusesSaves`,
    `DeletingDropsAPendingSave`) and `Storage/SettingsTests.LoadingWaitsForAPendingChange` queue `Task.Run`, block their
    pool thread 100–200 ms and assert the work hasn't finished, then await it: they can't fail from this, but in a busy
    run that check can pass with the work not yet started. Details: Docs/Notes.md, 2026-11-07.
  - **M11 step 12, import (2026-11-07, 3311 tests, on its worker branch `m11-step12`, rebased on `c26a6ed`, its full runs on `ac2306e`):** four questions came up and the step stopped before writing code; **the manager's decisions of 2026-11-07** (each the recommended option) are recorded in `Docs/Editor.md`, "How it works", step 12: P29's values matched by path along the kind's shape (a list holding bulk data by index with the file's length, other structure differences as one value; the known limit: a node inserted in the file shifts the names after it, Take all covers it), the folder suggested by ending for files (`ui/fonts`, `ui/images`, texts and folderless kinds at the top, `mod.info` at `mod/info`), a re-import of the same kind one undoable step and anything else a new entry or a replacement asked first and not undoable, and one file at a time (the import dialog, then the replace question or the keep-or-take list only when the path is taken, Cancel skipping that file); the step's own choices confirmed. `Tools/Editor/Session/EditorSession.Import.cs` (what a file imports as, by its ending in any case; the path suggested from what one import at its fitted name makes; the path's problems; the import planned at the chosen path with `PackWriter.ImportFile`, a stylesheet's urls checked against the entries shown, P28; the entries joining the session as unsaved work, the pack file untouched: new entries, a re-import as a commit, a replacement listing the pack's entry as removed or keeping a game entry under the new one; `EditorEntry.ImportedFile`, an imported file's bytes, for step 11's save and the custom mount) and `Tools/Editor/Import/` (`ImportNames`, `ImportComparison` and `ImportDifference`, `ImportPart` with the Import button through the file browser, the drop queue, the import dialog, the replace question and the keep-or-take list, `Import.css`). Every kind imported by the button and by a drop is the packer's entry of the same file at the same path (`.glb`, `.terrain.json` with its images, `.css` with urls in the game's packs, the mod and the session, a record's `.json`, `.ttf`, `.otf`, `.png`, `.svg`, `.txt`); a record's JSON form exported from the JSON tab comes back in (M11-18). The screens against Chromium at 1 and 1.65 (pixels past 12 levels, the limit 0.2 %): `import-path` 0.009 % and 0.010 %, `import-replace` 0.009 % and 0.006 %, `import-again` 0.055 % and 0.094 %. Found, reported, the engine not changed: an editor dialog over a composited scroller has grayscale text at 1.65 where Chromium's is LCD (an open item in "Small tasks left from earlier milestones"; the fixture shows its dialog over an empty form pane). Step 8's drop test and its application test (with a display) now see step 12's part take the queue. Tests: 61 in `Tests/Statventure.Tests/Editor/Import/` (55, and the three screens at both scales). The full suite passes on the GPU (1 min 24 s) and on lavapipe (1 min 49 s), inside a headless Wayland compositor: 3311 tests (`main`'s 3250 on `ac2306e` and 61 new), none skipped; the player's real game folder stayed missing and the Documents folder's top level unchanged. Mutations: 221 (`~/.cache/stat-fick-build/claude-work/m11/step12-mutations.json`, made by `step12/make-mutations.py`): 195 killed; of the 26 survivors 17 were gaps closed by new tests, 3 redundant lines removed, 2 killed in fact (the test run crashed on a stack overflow, which `mutate.py` reads as passing: checked by hand), 1 a cache, 3 looks under the screens' limits; the reruns (17, then 1) killed all; nothing written to the player's real folders. Renders for the user: `claude-work/m11/step12/render-import-{path,replace,again}@{1,1.65}.png`. Not checked: the editor in a real window (a real drop, by hand), Windows (`ImportSources`' default lookup on a file system that ignores case, "Windows check", item 8b). Details: `Docs/Editor.md`, "How it works", step 12.
  - **M11 step 13, the preview (2026-11-07, 3377 tests, on its worker branch `m11-step13`, rebased on `fa56257`):** six questions came up and went to the manager, all answered (a) the same day and recorded in `Docs/Editor.md`, "How it works", step 13: the program's wiring (two lines in `EditorApplication.Load` besides the sound switched on), Stop and a sounding effect (the engine's effects have no stop: a small task above, "A stop for sound effects"), the editor's volume at 50 % when none is remembered, the data as edited owned by this step and used by step 10, element roles' hosts without a stylesheet, a preview palette per palette role. `Tools/Editor/Session/EditorSession.EditedData.cs`: `Session.EditedData`, the game's packs the open pack is seen over with a custom mount of the session's entries as edited on top (P38; an entry that doesn't build given as its last built value), taken again on every read. `Tools/Editor/Preview/`: the preview pane (`PreviewPart`, connected by the program to the window's GPU, the host's surface views and the application's sound): sound effects (Play, Stop, Space, the editor's volume remembered in `editor/volume.json`, the play's volume, pan and pitch; each valid edit registered at once; renders equal to registering and playing by hand), themes (the library from the data as edited swapped in, `AudioSystem.Reload`; the theme following valid edits of itself and of its instruments, percussion and rooms while it plays, the last library that loaded kept; renders equal to the swap and play by hand), particle effects (a looping emitter at the origin, 20 at once and 20 a second for two longest lives, started again once done; a seed and Restart; the preview's distinct colour per slot, editable), models (their clips played, paused, looped and moved on a time slider as applying them by hand; the turnable camera; a stage's cameras and sun), cutscenes (`Sequence.Load` on the data as edited, a preview copy with stand-ins chosen per cutscene and remembered in `editor/stand-ins.json`, the tracks that can't be bound left out and listed; Play, Pause, a time slider; its picture equal to playing the copy by hand); an invalid edit keeps the last valid state, saying why (M11-12, P35). The screens against Chromium (the 3D view left out): `preview-effect` 448 and 1,556 pixels (0.070 % at 1×, 0.089 % at 1.65), `preview-model` 27 and 2,190 (0.004 %, 0.126 %), `preview-cutscene` 81 and 361 (0.010 %, 0.017 %) (GPU; the limit 0.2 %). 60 new tests in `Tests/Statventure.Tests/Editor/Preview/` and the screens' six cases; step 8's options test expects the sound on. Silence: every sound test renders an offline audio system (no device, no thread); the two application tests open the default device and play nothing. The full suite passes on the GPU and on lavapipe, inside a headless Wayland compositor (`claude-work/m11/step13-full-run.sh`): 3377 tests (`main`'s 3311 at `fa56257` and 66 new), none skipped (the GPU 1 min 48 s, lavapipe 1 min 31 s; the screens on lavapipe within a few pixels of the GPU's: `preview-effect` 0.071 % and 0.093 %, `preview-model` 0.004 % and 0.126 %, `preview-cutscene` 0.010 % and 0.017 %); the player's real game folder stayed missing and the Documents folder's top level unchanged (also on `ac2306e` before the rebase, 3310 tests). Mutations: 183 in the first run (`~/.cache/stat-fick-build/claude-work/m11/step13-mutations-{1,2,3,4,window}.json`, made by `make-step13-mutations.py` beside them; the program's wiring inside a headless cage): 154 killed, 24 survived, 5 didn't build. Of the survivors, 17 were gaps closed by new tests (an old emitter left on its node at Restart; a clip chosen keeping the time, played again from its end, looping from 0 instead of on, no rest pose without one; the note never hidden again; the drag without the pointer's capture and never ending; a slider's value not written; a library that doesn't load dropping the last; the framing on a skinned mesh's node and on the box's corner; a cutscene's view not ended when it goes, a refused play keeping its stand-ins; an unchanged record of an older version written again in the current one; Space with a dialog open, and Space the list typed ahead), 4 were redundant code, removed (the loop's own end check: the scene takes a done emitter off its node in the frame its clock got there; the sliders' change listeners: a range's change always follows its input; the cutscene registering the data's patches again: the swap registers those new or changed, and the effect preview only what the data holds; a `[hidden]` rule the UA's already holds), and 3 can't change what the checks see (two assignments swapped with nothing between them; the note's colour, in no screen: the fixtures show valid entries; the host's sound, which only gives the document its audibility, seen only with sound on a device). The rerun (`step13-mutations-rerun.json`, made by `make-step13-rerun.py`: the 17, the 5 rewritten to build, and 5 for the imported files the data as edited serves and the loop's restart as it is now) killed all 27: the list as it stands, 183, has 180 killed and 3 accounted for. Nothing written to the player's real folders. Renders for the user: `claude-work/m11/step13/render-{model,particles,cutscene}@{1,1.65}.png`. Not checked: the preview in a real window and its sound by ear ("By-hand checks", (c)); Windows.
  - **M11 step 10, checks and references in the editor (2026-11-07 and 08, 3423 tests, on its worker branch `m11-step10`, rebased on step 13's merge `034b83c`):** two questions came up and the manager decided them (2026-11-07; `Docs/Editor.md`, "How it works", step 10): Q-A, a removal's referrers are offered "Point them at:" another entry of the kind, "Leave them" by default, a rename's "Update them to the new name" ticked by default; Q-B, after a commit the entry's own problems are fresh at once and the cross-entry problems of the last run stay until the next replaces them; and confirmed the step's other choices (every record entry checked across entries, the game's too; the first problem per path; the theme's id against its path as the editor's own rule, `MusicLibrary.Load` on the data as edited as the last check; the reference list attached to the form's markup, open while the field has the focus). The data as edited is step 13's (the manager's decision of 2026-11-07); this step reads it. `Tools/Editor/Checks/` (`EntryChecks`: each entry's own check at every change of its draft and at a rename, the checks across entries over every record entry with `Session.EditedData` and the session's references, then the game's music loading while no music entry has a problem, a check that throws shown as the entry's problem, only the first problem per path, `Pending`, `Due`, `RunPending`, `DataProblem`; `ChecksPart`: the 200 ms pause on the document's clock, P17), `Tools/Editor/Session/EditorSession.Checks.cs` (`Session.Checks`, `Session.EntriesWithProblems`: what step 11's save reads, after `Checks.RunPending()`), `Tools/Editor/References/` (`SessionReferences`, the lookup from the session's own entries; `EntryReferences`: the references in a draft, at a form row's path, the entries naming one, pointing names at another entry, the entries a mark can name; `ReferenceListPart`: the reference field's list in the row under the field, filtered as typed, the flag, keys and clicks, M11-11; `ReferrersPart`: the referrers in the rename and remove dialogs, updated in the same step, P19). Measured (Debug): the checks across entries over a mod opened over the repo's packs (126 entries) 53 ms the first time, 23–28 ms after; a theme's commit with its own check 0.3 ms. The screens against Chromium (P36; pixels past 12 levels, the limit 0.2 %, the GPU): `checks-problems` 0.050 % at 1× and 0.187 % at 1.65, `references-list` 0.046 % and 0.152 % (captured again after the rebase: the preview pane shows content), captured in a headless cage with the large output mode at 1.65 too (a fixture wider than the output is otherwise cut). Found, the engine not changed (`Docs/Editor.md`, step 10, "Found"): at 1.65 the status line's text is grayscale in the engine while the form pane is a tall scroller (Chromium LCD; already in step 9's `form-controls@1.65`); a positioned box inside the form pane's scroller is composited apart by Chromium (grayscale text) and not by the engine (LCD), so the list is in the rows' flow; a stacking context on the form's rows (isolation, or a z-index) turned the form's text grayscale in the engine at 1.65 (Chromium's look not captured). Tests: 46 (42 in `Tests/Statventure.Tests/Editor/Checks/`, the two screens at two scales). The full suite passes on the GPU (1 min 28 s) and on lavapipe (1 min 55 s), inside a headless Wayland compositor, rebased on step 13's merge (`034b83c`): 3423 tests (`main`'s 3377 and 46 new), none skipped (before the rebase, on `ac2306e` with step 13's `Session.EditedData`: 3296, 1 min 24 s and 1 min 52 s). With step 13's preview, an invalid entry's note is now its first problem, so six of step 13's test expectations take the problems' words (`Docs/Editor.md`, step 10, "With step 13"); nothing of the preview's code changed; the player's real game folder stayed missing and the Documents folder's top level unchanged. Mutations (`~/.cache/stat-fick-build/claude-work/m11/step10-mutations.json` and `step10-mutations-rerun.json`, made by `make-step10-mutations.py` and `make-step10-rerun.py` beside them): 176 in the first run (146 killed, 4 didn't build and were rewritten, 26 survived: 11 gaps closed by tests, 7 redundant code removed, 2 equivalent, 6 looks: 3 under the screens' limits and 3 of the dialogs no fixture shows), then a rerun of 20 (the gaps, the rewritten, the code changed since): 19 killed, 1 equivalent (an entry that doesn't build has no value to check); nothing written to the player's real folders. Renders for the user: `claude-work/m11/step10/render-{checks-problems,references-list}@{1,1.65}.png`. Not checked: the editor in a real window (the list and the dialogs by hand), Windows. Details: `Docs/Editor.md`, "How it works", step 10.
  - **M11 step 11, saving and mods (2026-11-08, 3485 tests, on its worker branch `m11-step11`, rebased on step 10's merge `7e3ff8d`):** six questions came up and the step stopped before writing code; **the manager's decisions of 2026-11-08** (each (a)) are recorded in `Docs/Editor.md`, "How it works", step 11: Q1, the mod's info in a form of its own in the form pane through a small documented hook in step 9's `FormPart` (`IEntryForm`; no existing screen changed by it); Q2, New mod writes the mod at once with `mod/info` holding its name; Q3, Save As's ticks first, then the file browser, then the new file open in place of the old, unticked entries with unsaved changes named as let go; Q4, a pack it can't fully read offers its readable entries ticked, the intact entries it can't read unticked (kept byte for byte when ticked), the damaged ones not offered; Q5, Save As checks the entries ticked as the game would see them (step 10's checks against the game's packs with only them on top, then the music's loading), a problem refusing it; Q6, histories kept across a save; the step's smaller choices confirmed. `Tools/Editor/Session/EditorSession.Saving.cs` (`CanSave`, `CanSaveAs`, `SaveEntries`, `SaveProblems` after step 10's `RunPending`, `ChangedOnDisk` by the index hash, P23; `Save`: unchanged entries as stored (older versions, renamed ones at their paths), edited, new and copied records in the current versions, imported files with their bytes, removed and reverted ones left out, the file swapped in, then the pack read again and the entries saved in place; `SaveAsChoices`, `SaveAsFileProblem`, `SaveAsData`, `SaveAsProblems`, `SaveAs`; `NewModFileName`, `NewModProblem`, `NewMod`; `EditorEntry.MarkSaved`) and `Tools/Editor/Saving/` (`SavingPart`: Save, Save As and New mod from the keys and buttons, the refusal naming the entries, the changed-file question, `Opener.SaveFirst`; `SaveAsDialog`; `NewModDialog`; `ModInfoForm`; `Saving.css`). A new mod saved is mounted by `Application.DataMounts` as the game mounts mods and read back equal; the dev setup's game pack saves in place (tested on a scratch copy of the repo's packs). The screens against Chromium (pixels past 12 levels, the limit 0.2 %, the GPU): `saving-save-as` 0.010 % at 1× and 0.021 % at 1.65, `saving-new-mod` 0.009 % and 0.061 %, `saving-mod-info` 0 and 0.002 %; **all 16 earlier editor screens captured again** (their top bar changed: New mod always enabled, Save and Save As with a pack open; before, `checks-problems`, `references-list` and `preview-model` at 1.65 were past the limit), each giving the counts it gave before. Found, the engine not changed: the engine's textarea ignores the author's `line-height` (an open item above, "A textarea's `line-height`"); a dialog over a composited scroller at 1.65 met again (the fixture closes the game's audio, as `list-unreadable` does); a key whose listener closes a dialog sends its keypress to what has the focus after (Chromium too: the New mod dialog's Enter cancels its default action). Changed in earlier steps' tests: step 4's `ShellTests` (New mod always enabled) and step 8's `OpenerTests` (its Discard-and-Cancel part sets `SaveFirst` to null). Tests: 62 (56 in `Tests/Statventure.Tests/Editor/Saving/`, the three screens at two scales). The full suite passes on the GPU (1 min 33 s) and on lavapipe (2 min 2 s), inside a headless Wayland compositor: 3485 tests (`main`'s 3423 and 62 new), none skipped; the player's real game folder stayed missing, the Documents folder's top level and the repo's packs unchanged. Mutations (`~/.cache/stat-fick-build/claude-work/m11/step11-mutations.json`, `-rerun.json` and `-standing.json`, made by `step11/make-mutations.py` and `make-rerun.py`): 232 in the first run, 205 killed, 27 survived (14 gaps closed by tests, 6 redundant lines removed, 7 accounted for: 1 equivalent, 1 the engine's textarea line height, 5 looks under the screens' limits or in no fixture); the rerun of 20 killed all; the list as it stands, 228, has 221 killed and 7 accounted for; nothing written to the player's real folders. Renders for the user: `claude-work/m11/step11/render-saving-{save-as,new-mod,mod-info}@{1,1.65}.png`. Not checked: the editor in a real window ("By-hand checks", (d)), Windows (writing over the open pack, "Windows check", item 8a). Details: `Docs/Editor.md`, "How it works", step 11.
  - **M11 fixes after step 14 (2026-11-08, 3493 tests, on the branch `m11-fixes-1` from `4f6120e`):** step 14's end-to-end flow found two faults in the editor and one look; **the manager's decisions of 2026-11-08** are recorded in `Docs/Editor.md`, "How it works", "Fixes after step 14". (1) Nothing in the UI started New entry (P15: `EntryActions.New` had no caller): decision (a), a "New entry" button in the top bar between Save As and Import, a command of its own (`EditorCommand.NewEntry`) with Ctrl+N, enabled while `Session.CanAdd`, opening step 9's dialog, also from a field of the mod info's own form. (2) Enter in the import dialog's path field closed it without cancelling the key's default action, so its keypress pressed the top bar's Import button and opened the file browser again: the listener now cancels it, as the New mod and entry dialogs do; the other dialogs' Enter handlers checked again (none else closes a dialog from a text field without cancelling it). (3) The cutscene preview's "Stand-ins" heading showed with nothing under it for a cutscene without roles: hidden until a cutscene with roles is taken. The top bar changed, so **all 19 editor screens were captured again** at 1 and 1.65 (`claude-work/m11/step11-capture.sh`, a headless cage, one fixture per run): each gives step 11's counts again (within 4 pixels at 1.65), from 0.16–0.70 % with the new button against the old goldens (`claude-work/m11/fixes-1/table.md`); one 1× capture (`list-marks`) lacked the focused tree's outline (Chromium's window unfocused in that run) and was captured again. Tests: 8 new (`Editor/Form/NewEntryCommandTests.cs` 2, `Editor/Import/ImportEnterTests.cs` 2, `Editor/Preview/StandInsHeadingTests.cs` 2, step 4's `CommandTests` Ctrl+N and Ctrl+Shift+N), step 4's `ShellTests` with the new button. The full suite passes on the GPU (1 min 34 s) and on lavapipe (2 min 1 s), inside a headless Wayland compositor: 3493 tests (`main`'s 3485 and 8 new), none skipped; the player's real game folder stayed missing, the Documents folder's top level and the repo's packs unchanged. Mutations (`~/.cache/stat-fick-build/claude-work/m11/fixes-1-mutations.json`, made by `fixes-1/make-mutations.py`): 13, all killed (the command's handler, its availability, the button, its tooltip and place, the key, the import dialog's Enter, the heading's four rules); nothing written to the player's real folders. Not checked: the editor in a real window ("By-hand checks", (d)), Windows.
  - **M11 step 14, the whole editor, end to end (2026-11-08, 3496 tests, on its worker branch `m11-step14`, rebased on `00e5bd4`):** tests only (`Tests/Statventure.Tests/Editor/EndToEnd/`: `EndToEndTesting.cs`, `ModFromScratchTests.cs`, `DevSetupTests.cs`, `FoundTests.cs`); details in `Docs/Editor.md`, "How it works", step 14. **A mod made from scratch through the UI**, driven as a modder drives it (the top bar, New entry by its button and by Ctrl+N, the form's fields, selects, Add, Make and set boxes, the reference lists, the list's filter, Import and Open through the file browser's path field, the JSON tab, the mod info's own form): New mod "Ember Rite" and its info; the game's hall changed in the mod (its copy, P7); a sound effect, a particle effect, a stage imported from the tests' `dolly.glb` (the packer's entry of the same file), a readme imported as a file, and a cutscene using the three, its stage, effect and sound picked from the reference lists; Save refused for a camera node typed wrong ("… cue 0's camera dolly/Lens: the stage models/stages/dolly has no node Lens", under the field and in the refusal), fixed, saved; then mounted as the game mounts mods over its packs (`Application.DataMounts`) and read by `Sequence.Load`, `ParticleEffect.Load`, `MusicLibrary.Load` and `ModelData.Load`, each the value the editor showed (binary form and JSON tab), the mod's hall over the game's, the game's 55 themes all there; opened again (Open, the browser), the same seven entries. **The dev setup:** `--repo` (the program's own `EditorArguments.Parse`) on a scratch copy of `engine/Assets/Packs`: the game's `audio.pak` opened writable, the harp's level typed and Ctrl+S: saved in place, every other entry's stored bytes kept, the dump changed only at the harp; the manager's decision of 2026-11-08: the test reads the dump in-process, and `textconv.sh` itself was run by hand once on the test's saved pack (the diff: the harp's `level` 0.22 → 0.375, its `storedLength` and `sha256`; `claude-work/m11/step14/textconv.diff`). **Found, and fixed apart before the step went on** ("M11 fixes after step 14" above): nothing in the UI started New entry (the flow first started it as step 9's tests do, then stopped and reported it); Enter in the import dialog opened the file browser again (a probe of every key and click event showed the keypress pressing the Import button the focus went back to; `FoundTests` keeps it as the end-to-end witness); the empty "Stand-ins" heading. **Every editor screen against Chromium** at 1 and 1.65 on the GPU and on lavapipe with `m11-fixes-1`'s goldens: all 19 within the limit (pixels past 12 levels, 0.2 %; the worst `checks-problems@1.65`, 0.187 % on the GPU and 0.188 % on lavapipe; the table in `Docs/Editor.md`, step 14), nothing captured again; P36's six screens are all there, so no fixture remained to add. Tests: 3 new. The full suite passes on the GPU (1 min 33 s) and on lavapipe (1 min 57 s), inside a headless Wayland compositor: 3496 tests (`main`'s 3493 and 3 new), none skipped; the player's real game folder stayed missing, the Documents folder's top level and the repo's packs (this worktree's and the main checkout's) unchanged. Mutations (`~/.cache/stat-fick-build/claude-work/m11/step14-mutations.json` and `-rerun.json`, made by `step14/make-mutations.py` and `make-rerun.py`; each a rule of the editor or the engine the flow goes through, only this step's tests run against it): 30 in the first run, 25 killed, 5 survived: 1 gap closed (no loose file was imported, so imported files left out of a save went unseen: the readme now), 1 malformed mutation rewritten (the mods also still mounted above the packs), 3 that can't differ on what the flow saves (a kept entry re-added from its record: the writer keeps a content's stored bytes by its hash; `Sequence.Load`'s and `ParticleEffect.Load`'s own checks: a mod the editor saved passes them by construction); the rerun of 3 (the gap, the rewritten one, a kept entry left out) killed all; the list as it stands, 31, has 28 killed and 3 accounted for; nothing written to the player's real folders. Renders for the user: `claude-work/m11/step14/render-cutscene@{1,1.65}.png` (the whole editor with the finished cutscene selected, its preview paused at 0.8 s) and the saved mod, `ember-rite.pak`. Not checked: the editor in a real window ("By-hand checks", (e)), Windows ("Windows check", item 8).
  - **M11 done (2026-11-08, 3496 tests):** all fourteen steps merged on `main`, with the fixes step 14's flow called for (`00e5bd4`); M11 ran from 2026-11-06 to 2026-11-08 under the user's rule for M11 (the editor's design the manager's: P1–P38 and every step's questions; the game's questions G4 and G5 the user's, G1 and G6 the manager's, G2 and G3 kept for the port). The Statventure Editor (`Tools/Editor`) opens one pack over the game's packs, edits every record kind through one form made from the kinds' definitions with undo per entry, checks as you edit with the engine's own checks (the game runs them at load too, G4, G5), references, import, a live preview, and saves mods (and, in the dev setup, the game's packs); a mod made from nothing through its UI loads in the game as the editor showed it (step 14). The full suite passes on the GPU and on lavapipe, inside a headless Wayland compositor under the shared lock: 3496 tests, none held (M11 started at 2547 on `a32c5cb`; the work beside it, the background-layer port and the first-text LCD fix among it, is in its own entries). Every one of the editor's 19 screens matches Chromium 152 at 1 and 1.65 within the paint fixtures' limits on both devices. Mutations: every step's list killed or accounted for (each step's entry; about 2,460 in the lists as they stand, 42 accounted for, none writing to the player's real folders). Found on the way and recorded as small tasks (open, no go yet): control labels a device pixel low at 1.65; text grayscale at 1.65 in a composited box, under a dialog over a composited scroller and in a status line beside a tall scroller; a select with its own padding wider than Chromium's; the painter painting every row of a scroll container; a textarea's `line-height`; a positioned box with a z-index inside a scroller; a stacking context on the form's rows; `ValueShape.TextOf`; a stop for sound effects. Fixed on the way: text first in a paint chunk (`lcd-first-text`), the capture harness's window out of X11's range at 1.65 (step 8), the flaky `ScriptApiTests` (a test's own fault). Not checked: the editor in a real window and by ear (the by-hand checks (a)–(e) in "Small tasks left from earlier milestones"), Windows ("Windows check", item 8). What it leaves for the port: "What M11 leaves for the port" under "How to continue". Ticked under Milestones; M12 waits for the user's go, its questions first.
  - **M12 started (2026-11-08, docs only, on the branch `m12-design` from `bb2e443`):** the user's go ("Then start with M12"); the twelve questions asked and answered the same day, all with the recommended options except the shipped folders (M12-1: two separate downloads, the editor told where the game is), icons (M12-5: none in M12) and versioning (M12-6: date-based); M12-7 an in-game update check later, draft GitHub releases meanwhile; M12-9 closes G3 (the `Statventure.Game.Data` library); the rule to ask stands in full again ("Decisions for M12"). Design doc `Docs/Packaging.md`: what exists checked against the code (the build publishes only the sandbox, framework-dependent, with every runtime's natives, 159 MB; nothing publishes the editor; the executable takes the assembly's name, so M12-2's names come from renaming it at publish or naming the assembly per platform; no program version anywhere, only the engine's own `EngineInfo.Version` 0.1.0; no program sets `ApplicationOptions.LogFile`; a crash is logged and rethrown after the window, device and sound are disposed, so a crash window needs a window, device and loop of its own, and the engine embeds no font; the editor's fonts are the game's `ui.pak`'s, so an editor downloaded apart can't draw before it knows the game; BRUTAL's Windows libraries need the Visual C++ runtime, `VCRUNTIME140.dll` and for some `MSVCP140.dll`, which nothing ships; a Windows `.exe` built on Linux gets no version resources, NETSDK1074; the Linux libraries need glibc 2.34 and carry `RUNPATH $ORIGIN`, each up to three times as copies; the BRUTAL licence asks for `NOTICE.txt`, the copyright in the credits and the logo before the program's primary function, the README waiving the logo for non-profit projects while the licence text names written permission), the method, a proposed design, 28 questions for the user (Q1–Q28, not asked yet) and seven proposed steps (the version; logs and crash files; publishing and the archives with their check and `editor.ps1`; the crash window; the editor as a download of its own; the release script; end to end with the Windows items) in three waves. No code yet.
  - **M12's detail round answered (2026-11-08, docs only, the branch `m12-design`):** the user answered Q1–Q28 of `Docs/Packaging.md` and clarified M12-9 with the question tool, all as recommended except Q1 (versions always of four numbers, `2026.11.8.1`), Q11 (a BRUTAL logo splash and a credits view in both programs now) and Q12 (no readme in the downloads, the release's text instead); M12-9: no copy of the data library in the editor's download ("Decisions for M12", "The detail round"). `Docs/Packaging.md`'s questions became "Decided (Q1–Q28)", its design and steps follow them: an eighth step (4, the engine's embedded Lato, one resource the crash window, the splash, the credits and the editor share, and the BRUTAL logo, with the splash and the credits as engine pieces), the splash and the crash window together in step 5 (both `Application.Run`'s own screens), the release's text carrying what a readme would have said (step 7). The logo: BRUTAL's own `docs/branding/LOGO_BRUTAL_WHT.png` and `LOGO_BRUTAL_BLK.png` (712 × 168) in the BRUTAL source in the build cache, not in any package; step 4 checks them against ahwoo.com/brutal-branding. Two new questions from Q11's answer, Q29 (the splash's look, length and skipping) and Q30 (where each program opens the credits, what they list), still to ask; step 4 waits for them. The eight steps are the plan; none started. No code yet.
  - **M12's Q29 and Q30 answered (2026-11-08, docs only, the branch `m12-design`):** both as recommended ("Decisions for M12"; `Docs/Packaging.md`, "Decided (Q29–Q30)"): the BRUTAL splash is the white logo centred on the dark ground at its own size, 2 s at every start of both programs, skippable by a key or click after the first second; one engine credits view for both programs, opened by an "About" button in the editor's top bar (its 19 screens captured again) and on the sandbox's page, listing the program and version, "Built with BRUTAL" with the copyright line and `NOTICE.txt` verbatim, every component with its licence and copyright line, and a pointer to `licenses/`. Nothing of M12's design is open; step 4 no longer waits; the eight steps are the plan, none started. No code yet.
  - **M12 step 1, the version and invariant globalization (2026-11-08, 3511 tests, on its worker branch `m12-step1` from `a895e5f`):** details in `Docs/Packaging.md`, "How it works", step 1. **The version:** `Directory.Build.props` gives a build with no `Version` the informational version `dev` (the dev default); a release is built with `-p:Version=2026.11.8.1` (step 7's script), its assemblies' versions too. **The commit (left to the step):** from git at build time by the .NET SDK's own Source Link (`SourceRevisionId`, "+<full hash>"; worktrees work; a script may pass `-p:SourceRevisionId`); a build from a source archive without git (`git archive` of `engine/`, its own `build.sh`) builds with 0 errors and the same 141 warnings and says `dev` with no commit. **`Core/ProgramInfo.cs`:** `record ProgramInfo(Name, Version, Commit)`, `Of(Assembly)` (the program's own assembly, not the entry assembly: the test host in tests), `Parse`, `Dev`, `IsDev`; for step 2's first lines, the credits and step 6's version check (`Of` works on a loaded assembly; the single-file bundle would need parsing). `EngineInfo.Version` stays 0.1.0 (confirmed by the manager: Vulkan's version field holds at most 1023 as its major number). **The editor's title (Q3):** `EditorView.TitleOf(version, pack, unsaved)` and `EditorView.Version`: "Statventure Editor 2026.11.8.1 — ember-rite.pak", the dev build's "Statventure Editor dev …"; no screen captured again; for step 5/6: the window's title before `Load` (`EditorApplication.OptionsFor`'s `EditorView.Name`) should become `TitleOf(Version, null, false)` once the splash comes before `Load`. **Invariant globalization (Q8)** for every project: the full suite in invariant mode found one rule needing ICU, `ImportNames.Fit`'s accents (`string.Normalize(FormD)` does nothing without ICU; three tests failed: "Ünïcode Tales!", "Icône.SVG", "Ärger über Öl"); the step stopped and reported it; **the manager's decision (2026-11-08, (a), Q8 and the editor's rule unchanged):** `Fit` takes accents off through HarfBuzz's canonical decompositions (`UnicodeFunctions.Default.TryDecompose`, repeated, non-spacing marks left out), the three tests as they were; same results as ICU's FormD on every sample checked (ǖ two levels down to u; ß, æ, ﬁ, Ǆ kept as FormD keeps them). Tests: 15 new (`Core/ProgramInfoTests.cs` 13, `Core/ProgramBuildTests.cs` 1: a scratch program built with the repo's props as a release, a dev build and without git; `ShellTests` 1 more), the title's text in `ShellTests` and five other editor tests. The full suite passes on the GPU (1 min 31 s) and on lavapipe (2 min 8 s), in invariant mode, inside a headless Wayland compositor under the shared lock: 3511 tests (`main`'s 3496 and 15 new), none skipped; the player's real game folder stayed missing, the Documents folder's top level unchanged. Mutations (`~/.cache/stat-fick-build/claude-work/m12/step1-mutations.json`, made by `step1-make-mutations.py`): 36, 34 killed, 2 accounted for (in the tests' dev build the editor's version is `dev`, so the title given the literal `dev` can't differ; every assembly of one build carries the same version, so the editor's version read from the engine's assembly can't differ); nothing written to the player's real folders. Not checked: Windows (the tests' scratch build there, "Windows check").
  - **M12 step 2, logs and crash files (2026-11-08, 3544 tests, on its worker branch `m12-step2`, rebased on step 1's `345388f`):** details in `Docs/Packaging.md`, "How it works", step 2. **The option:** `ApplicationOptions.LogName` (in place of the unused `LogFile`): `stat-fick` for the sandbox and its cutscene player, `statventure-editor` from the editor's public constructor only; null, the default: no files, so tests' applications write none unless they ask (**the manager's decisions, 2026-11-08**, recorded in `Docs/Packaging.md`, Steps, step 2: (1) the name only on the program's own path, the tests' construction leaving it unset; (2) the cutscene player logs as the game, its one line step 2's). **The run's log** (`Core/LogFiles.cs`): `<game folder>/logs/<name>-yyyy-MM-dd_HH-mm-ss.log`, local time, made new (a taken name `-2` …), folders 0700 and files 0600 on Linux, UTF-8, every line flushed; its first lines the program with its version and commit (`ProgramInfo.Of` of the application class's assembly), the start time with its offset, the system (`Bazzite (Unix 7.2.15.1) X64, .NET 10.0.13`); a log that can't be made or written never stops the program. **Kept:** at each start the program's own run and its 9 newest others, the 10 newest crash files, newest by the names' stamps, only those exact name forms. **Crashes:** an exception out of the start, `Load`, `Update`, `Render`, `Unload` or the teardown, or unhandled on any other thread; the crash file `logs/crash-<stamp>.txt` first (the program line, the time and where, the system, the log's path, the exception with its stack, the log's last 100 lines), then the teardown as before (the pending saves, S9-B), then the terminal message (Q24, the crash window's words), then the exception leaves `Run()`. **Other threads:** .NET 10's `ExceptionHandling.SetUnhandledExceptionHandler` (true lets the thread end, the process go on; checked in a scratch program first): the crash file at once, then the crash handed to the main loop, which throws it at its next iteration and so ends the run in order (a minimized loop woken with `Window.PostEmptyEvent`); a loop that doesn't answer within 10 s: the terminal message, and .NET ends the process with the crash file written. This hand-over is listed under step 5; it is done here because the ordered end needs it (step 5 adds the window after the teardown). **For step 5:** `Application.Crash` (`CrashReport`: `Exception`, `Where`, `Time`, `File`, `Message`, `TerminalText`), printed once in `Ended`; `Terminal` and `CrashAnswerTime` injectable. Tests: 33 new (`Core/LogFilesTests.cs` 12, `Core/ApplicationCrashTests.cs` 21, hidden windows; those running many updates minimized with immediate wake-ups, as wlroots hangs a hidden window's presents after a few frames). The full suite passes on the GPU (1 min 39 s) and on lavapipe (1 min 55 s) inside a headless Wayland compositor under the shared lock: 3544 tests (`main`'s 3511 and 33 new), none skipped; the player's real game folder stayed missing, the Documents folder's top level unchanged. Mutations (`~/.cache/stat-fick-build/claude-work/m12/step2-mutations.json`, made by `make-step2-mutations.py`): 100, 93 killed by mutate.py's reading, 6 killed by crashing the test host (a real thread's exception with no handler taking it ends the host; mutate.py reads only the summary line, which an aborted run prints as "Passed!" with fewer tests; each run again by hand and shown aborted, `step2-abort-check.py`), 1 accounted for (the crash file's flush to the disk can't be seen by a test); nothing written to the player's real folders. Not checked: Windows.
  - **M12 step 4, the engine's embedded Lato and BRUTAL logo, the splash and the credits as engine pieces, with Chromium's shrunk images first (2026-11-08, 3625 tests, on its worker branch `m12-step4`, rebased on `2911b84`; two commits, the prerequisite's and the step's):** details in `Docs/Packaging.md`, "How it works", step 4, and "Decided (the logo's file)". **The logo check stopped the step:** ahwoo.com/brutal-branding (a 301 to github.com/ahwootech/brutal-branding) has offered 4096 × 896 logos since 2026-10-21 (`5d38373a24`), BRUTAL's source keeps the page's first 712 × 168 version; **the user's decision (a), 2026-11-08** ("Decisions for M12"): the page's white logo byte for byte, drawn 696 CSS px wide, after porting Chromium's mipmapped downscaling. **The prerequisite (its own commit):** a raster image drawn as Chromium's CPU raster draws it: Blink's medium quality for decoded images; cc's software decode cache (the target size, its mip level, each half the last rounded up, a copy of the source part when the draw shrinks to half or less, nothing for an empty target; `Canvas/ImageDecodeScale.cs`); Skia's `scalePixels` with the HQ downsampler's levels (rounded down, 2 × 2 or 1 2 1, truncated) and its accessor's level (the geometric mean, less a half, rounded; `Canvas/SkiaMipmap.cs`); premultiplied decode (`Canvas/CanvasImage.cs`, the UI's images now); the draw through Skia's legacy shader context, weights in sixteenths, truncated (`canvas.frag`, `skiaBilerp`); an `<img>` drawn into its pixel-snapped fitted rect, a cover image cut to its box (ImagePainter::PaintIntoRect). Found: on RADV a `texelFetch` through a `sampler2D` built from the bindless arrays lost its non-uniform index (lanes drawing different images read one's texels); samplerless fetches are right. Two image fixtures (`image-shrink`, `image-shrink-backgrounds`, at 1 and 1.65) within the paint limits (0.006–0.06 % beyond 12 levels); every existing render pixel for pixel the same (210 on the GPU: no existing reference draws a raster image; the game screens' images are SVGs), one test changed (`View3DTests`' surface against image, now opaque and unturned). **The pieces:** `UI/EngineFonts.cs` (Lato regular and bold and their OFL text from the old game's files, equal to ui.pak's; `AddTo(FontLibrary)` as `Lato` 400/700, `Mount()` at ui.pak's paths); `UI/Splash.cs` (the logo centred on `#1a1c1a`, 696 px, `max-width`/`max-height` 100 %, `object-fit: contain`; `Advance(time)`, 2 s, a `keydown` or `mousedown` from 1 s skips; its own data, so a `UiHost` shows it unwired); `UI/Credits.cs` (title, "Version …", "Built with BRUTAL" with `NOTICE.txt` verbatim, 36 components with licences, copyright lines, FreeType's credit and OpenAL Soft's source, the pointer to `licenses/`; `Markup`/`Css` for a dialog, an instance a scrolling document). Five fixtures (`splash`, `splash-narrow`, `splash-short`, `credits`, `credits-end`) at 1 and 1.65 within the limits (the splashes 0 pixels beyond 12 levels, the credits at most 0.035 %). Tests: 81 new (`Canvas/ImageScalingTests` 42, `Ui/ImageScalingPaintReferenceTests` 5, `Ui/EngineScreensTests` 23, `Ui/EngineScreensPaintReferenceTests` 11, one `View3DTests` changed). The full suite passes on the GPU (1 min 55 s) and on lavapipe (2 min 27 s), inside a headless Wayland compositor under the shared lock: 3625 tests (`main`'s 3544 and 81 new), none skipped; the player's real game folder stayed missing, the Documents folder's top level unchanged. Mutations (`~/.cache/stat-fick-build/claude-work/m12/step4-mutations.json`, made by `step4-make-mutations-pre.py` and `step4-make-mutations.py`): 96, 94 killed; the 2 survivors were gaps (the canvas's own scale in a draw's matrix, which the UI's scale-1 canvas never shows; the cover cut, which an even start and a small box hid), closed and killed when run again; nothing written to the player's real folders. Not checked: Windows; the splash and the credits in a real window (steps 5 and 6).
  - **M12 step 3, publishing, the archives and their check; `editor.ps1` (2026-11-08, on its worker branch `m12-step3`, rebased on `7f09e9c`, step 4's merge; its full runs at 3511 tests on `4565aae`, before steps 2 and 4):** details in `Docs/Packaging.md`, "How it works", step 3. **The publish:** `Tools/Scripts/publish.sh [--version <v>] [--no-check] [<package> ...]` (Windows: `publish.ps1`) compiles the shaders, takes the Visual C++ runtime's DLLs into the build cache when needed, and runs `Tools/Packaging/Publish.cs` (a .NET 10 file-based program both scripts share): per package a self-contained single-file `dotnet publish` for `linux-x64` or `win-x64` (the programs' projects set `SelfContained`, `PublishSingleFile` with the natives beside, `PublishTrimmed` false and `WinExe` for Windows, only for a runtime publish, so the dev layout and the tests are unchanged), `DebugType=embedded`, `ContinuousIntegrationBuild=true` and `EnableSourceLink=false` for every assembly, `-p:Version` for a release, the work folder `release/.publish` inside the repo (**found:** outside it the build's generated files kept machine paths in the symbols), the executable renamed (`stat-fick`, `Stat Fick.exe`, `statventure-editor`, `Statventure Editor.exe`), `.pdb` files left out, `packs/` the committed packs (the game's), `NOTICE.txt` and 28 texts in `licenses/` from `Tools/Packaging/` (checked against the packages they came from at the versions restored), the three DLLs on Windows, then the archive (a `.tar.gz` with numeric owner 0 and mode 755 for ELF files, a `.zip`; both dated at the commit's time; one folder `<program>-<version>/`); about 20 s for the four. **Sizes (2026.11.8.1, rebased on step 4):** the game's Linux archive 45.1 MB (71 files), Windows 41.3 MB (64 files), the editor's Linux 41.8 MB (51), Windows 38.0 MB (44); the single-file program about 80 MB unpacked, 210 files inside it, .NET's own natives linked into its host. **Choices left to the step:** renaming at publish; the soname copies kept (8.1 MiB unpacked, 1.8 MB packed); the archives made by .NET (no `zip` or Python needed to make them); one licence set for all four packages; no `classes/` until the port (the design's; the brief named it). **The licences:** BRUTAL's `NOTICE.txt` and licence, each BRUTAL package's `LICENSES/` (FreeType's FTL and notices with libpng, GLFW, OpenAL Soft's LGPL 2.0 text, its four parts and where its source is, gli, KTX, stb, VMA, zlib, Vulkan, Vulkan Abstractions, glTF), HarfBuzzSharp's licence and notices, .NET's licence and notices (also the `Microsoft.Extensions.*` 10.0 packages'), `Microsoft.Extensions.ObjectPool`'s (11.0 preview, which BRUTAL's logging pulls), CommunityToolkit's, Lato's and STIX's OFL, Chromium 152's BSD licence (**found** on the rebase on step 4: the engine has embedded Chromium's two search-field cancel images since before M12; step 4's credits view doesn't list Chromium yet); kept byte for byte (`Tools/Packaging/.gitattributes`); `licenses.txt` names each text's source and what covers each file. **The Visual C++ runtime (Q9):** `Tools/Packaging/vc-runtime.txt` pins `VC_redist.x64.exe` 14.44.35211 and the three DLLs' SHA-256; `publish.sh` takes them out with `curl` and `cabextract` into `$STATVENTURE_CACHE/vc-runtime/14.44.35211/`, `publish.ps1` with the redistributable's `/layout` and `expand.exe` (not run); never in git. **Its licence:** the redistributable's own terms forbid shipping it "combined with any of your applications for others to use"; Microsoft's Visual Studio 2022 redistribution list lets licensed Visual Studio users ship the runtime's DLLs with their program under Visual Studio's licence; **the user's answer (2026-11-08): they ship**, the user a licensed Visual Studio user under its Distributable Code terms ("Decisions for M12"). **The check:** `Tools/Scripts/check-packages.sh` (`Tools/Packaging/check_packages.py`): the name and folder (Q1, Q4), every path and the executable bit against `Tools/Packaging/listings/`, modes, owner 0, no links, the packs byte for byte, the texts, the runtime DLLs' SHA-256, ELF x86-64 or PE32+ x86-64 with the GUI subsystem (Q6), single-file with exactly the files inside the listing names (Q5, M12-3: a trimmed or framework-dependent publish fails), embedded symbols under `/_/` (Q7), the program's version the archive's, every native library's needs met (in the folder through `$ORIGIN`, or the system's; at most `GLIBC_2.34` and `GLIBCXX_3.4.30`; the DLLs' imports), every file's licence covered; the Linux programs run in a headless cage (`xvfb-run` without it) on a scratch `XDG_DATA_HOME` with OpenAL Soft's null device: the stand-in draws (`--capture`, its page in about 1.3 s) and ends with 0, the editor given a wrong argument prints its usage and ends with 2; the real folders compared. **Found:** libKtx and libopenal need `GLIBCXX_3.4.30` (GCC 12's libstdc++), so the release's text should say Ubuntu 22.04, Debian 12 or Fedora 36 and later (not Fedora 35, not RHEL 9); `libHarfBuzzSharp.so` has no RUNPATH (it needs only the system's); Source Link wrote the git remote into the embedded symbols, from a clone of the GitHub repo the private repo's address (`raw.githubusercontent.com/Vatrix33/statfick/<commit>/*`): **the user's answer (2026-11-08): left out**, `-p:EnableSourceLink=false` (the commit stays in the version, the paths `/_/`), and the check fails on the repo's own assemblies or their symbols naming `github` or holding a Source Link entry. **From a clean tree:** a fresh clone's `publish.sh` compiles the shaders and makes and checks the four (25 s), byte for byte the worktree's; two publishes of one commit give the same archives. **`editor.ps1`** (the Windows counterpart of `editor.sh`; "Windows check", item 8d updated). `cloud-setup.sh` installs `binutils` and `cabextract`. **Tests:** the check's self-test (`check-packages.sh --self-test`) breaks each archive one rule at a time: on the four 2026.11.8.1 archives, rebased on step 2, 118 breaks, all caught (about 2 minutes); the archives packed again unbroken pass. No C# test (Q28: the package checks are a script of their own; a publish inside the suite would build for two platforms). The full suite passes on the GPU (1 min 40 s) and on lavapipe (1 min 53 s), inside a headless Wayland compositor under the shared lock, on `4565aae` (rebased on step 1): 3511 tests (`main`'s then, none new), none skipped (not run again after the rebases on steps 2 and 4: what the step changes in the solution, the two program projects' runtime-publish settings, is as it was in those runs, and steps 2 and 4 were tested by their own runs); the player's real game folder stayed missing, the Documents folder's top level and the repo's packs unchanged. **Mutations** (the publish's rules, `~/.cache/stat-fick-build/claude-work/m12/step3-mutations.json`, made by `step3-make-mutations.py`, run by `step3-mutate.py`: each change applied, `publish.sh` and the check run, the change undone; run again after the rebase with Source Link left on added): 45, 44 killed (2 of them by another rule than expected: with the project's runtime settings off the publish has no runtime pack, and the publish's licence check refuses it), 1 accounted for (the publish's own refusal of a version not of Q1's form, off: the check still refuses the archive's name); 5 tests of the publish's guards pass (a licence text not its package's, a text `licenses.txt` doesn't name, a text from a package the program hasn't, a version not of Q1's form, a tree without compiled shaders publishing); nothing written to the player's real folders. Not checked: Windows (`publish.ps1`, `editor.ps1`, the zips: step 8's items), the cloud (whether Ubuntu's own .NET SDK publishes for `win-x64`).
  - **M12 step 4 follow-up, Chromium credited and the credits read the shipped set (2026-11-08, 3626 tests, on its worker branch `m12-step4-credits` from `77854d4`):** details in `Docs/Packaging.md`, "How it works", step 4 ("The follow-up"). Step 3 found the engine has embedded Chromium 152's two search field cancel images (`UI/Resources/search_cancel*.png`) since before M12 and ships `licenses/Chromium-LICENSE.txt` (BSD-3-Clause); `UI/Credits.cs` now lists "Chromium 152 (the search field's cancel button images, in the engine)", BSD 3-Clause License, "Copyright 2015 The Chromium Authors" (the shipped text's line), last, with the fonts: 37 components. The credits' tests (`Ui/EngineScreensTests`) read the shipped set from `Tools/Packaging/licenses.txt` (its `text` lines) and the texts kept beside it, no longer from NuGet and the old game's fonts: the list and the folder agree (every text listed kept, every text kept listed), every shipped text is named by the credits, every text the credits name is shipped, `NOTICE.txt` and the Lato OFL text equal the engine's own copies; one test new. `credits` and `credits-end` captured again at 1 and 1.65 (the new lines; the top's scrollbar thumb shorter): pixels beyond 12 levels, credits 2 (1×) and 324 (0.021 %, 1.65), credits-end 199 (0.035 %) and 151 (0.010 %), as before. Mutations (`~/.cache/stat-fick-build/claude-work/m12/step4-mutations-followup.json`, made by `step4-make-mutations-followup.py`): 8 (Chromium left out, its copyright line and its text wrong, a component's text and a note's text not shipped, the closing line's text wrong, Chromium's `text` line taken out of `licenses.txt`, a text listed that isn't kept), all killed; nothing written to the player's real folders. The full suite passes on the GPU (1 min 39 s) and on lavapipe (2 min 15 s), inside a headless Wayland compositor under the shared lock: 3626 tests (`main`'s 3625 and 1 new), none skipped; the player's real game folder stayed missing, the Documents folder's top level unchanged. Not checked: Windows.
  - **M12 step 7, the release script and its text (2026-11-08, on its worker branch `m12-step7` from `77854d4`; scripts and data only, nothing the solution builds):** details in `Docs/Packaging.md`, "How it works", step 7. **The script:** `Tools/Scripts/release.sh [--dry-run]` (on the PC, `gh` logged in) refuses a work tree that isn't clean (`git status --porcelain`: changes staged or not and untracked files; git-ignored ones don't count) or a commit not on GitHub's `main` (fetched now, forced; any commit of its history), and runs no tests (Q27); the version is the commit's UTC committer date and one more than the highest `v<date>.<n>` among GitHub's tags (`git ls-remote`) and its releases' tags, drafts included (`gh release list`, read-only: a draft holds its number until it is published) (Q1, Q2); then `publish.sh --version <v> --no-check` and `check-packages.sh --version <v>` for the four; the text from `Tools/Packaging/release-text.md` (placeholders `{{version}}` and the four archives' names; one nothing fills refused) into `release/release-text-<v>.md`; and `gh release create v<v> --draft --target <commit> --title "Stat Fick <v>" --notes-file … <the four>` from the repo's root (Q26), `gh` printing the draft's address; `--dry-run` does all but the upload and prints the command and the text. git reaches GitHub with no prompt and no askpass (`GIT_TERMINAL_PROMPT=0 GIT_ASKPASS=/bin/false SSH_ASKPASS=`). **The text** (Q12–Q15): what each download is, "Statventure Sandbox stands in for the game until the port" (Q13), the versioned folders, starting each on Linux and Windows, unsigned with SmartScreen's More info and Run anyway (Q14), no version details in the programs' Properties (Q15), the editor's "Where is Stat Fick?" and its version rule, the player's folders and what they hold, the system needs (step 3's: Vulkan 1.3; x86-64 with glibc 2.34 and GCC 12's libstdc++, Ubuntu 22.04, Debian 12, Fedora 36 or later, not RHEL 9; Windows 10 or 11, 64-bit, the Visual C++ runtime shipped), the licences. **Choices left to the step:** the committer date (the archives' time; a rebased commit's author date is older); drafts counted with the tags; one more than the highest number, not the count; any commit of `main`'s history; untracked files make the tree dirty; the text's wording; the dry run's output; and, the manager's decision (a), a commit released twice gets the next number (Q2's count; the same commit gives the same date). **Tests:** `release.sh --self-test` (`Tools/Packaging/release_test.py`; not in `test.sh`, like Q28's package checks): 31 tests in about 3 s, each in a scratch repository with a bare one as its GitHub, stand-ins for `gh` (first on `PATH`), `publish.sh`, `check-packages.sh` and `test.sh` recording their calls: the version for a first and a same-day second release (`.1`, `.2`), the highest by number, drafts, the UTC committer date, no padding; each refusal (a changed, staged or untracked file; a commit not pushed, on another branch or off a moved-back `main`; GitHub, its tags or its releases unreadable; no `gh`); a failed publish, check or upload and a missing archive stopping it; the `gh` call with the tag, `--draft`, the commit, the title, the filled text and the four archives; the text's placeholders and required parts; no prompt and no askpass (a 401 server and a terminal of its own; an ssh stand-in); no tests run. **Mutations** (`~/.cache/stat-fick-build/claude-work/m12/step7-mutations.json`, made by `step7-make-mutations.py`, run by `step7-mutate.py` under the shared lock in a headless cage; `mutate.py` runs only `dotnet test`): 100, 97 killed at first, the 3 survivors gaps closed (ssh's askpass, a failed tag read after a good fetch, the Windows start line's program name) and all 100 killed when run again; nothing written to the player's real folders. **Against the real repo (read-only):** `2026.11.8.1` for `77854d4`, the four published and checked, the upload printed, GitHub's tags and releases unchanged; the publish fails when the repo is reached through a symbolic link (`/home` → `/var/home`), reported, the repo's own path holding none (`Docs/Packaging.md`, step 7). Nothing the solution builds changed, so the full suite wasn't run: the editor's and core tests filtered (652) pass on the GPU inside a headless cage; the player's real game folder stayed missing. Not checked: a real draft (the first is made only on the user's word); Windows (nothing of it runs there).
  - **M12 step 5, the application's own screens: the splash before `Load`, the crash window after a crash; the sandbox's credits (2026-11-08, 3658 tests, on its worker branch `m12-step5`, rebased on `b08013b`):** details in `Docs/Packaging.md`, "How it works", step 5. **The splash** (Q29): `Application.Run` shows step 4's `Splash` in the application's window once the window, device and swapchain are made, before the sound and `Load`, on a program's own run (a `LogName`; `ApplicationOptions.Splash`, on by default, so the editor's program, the sandbox and the cutscene player show it with no change; tests' applications, the editor's tests included, show none); a close during it ends the run unloaded, a crash in it is the start's, another thread's crash is taken there; the application's own loop and frame numbers start after it. **The crash window** (Q22–Q24; `Core/CrashWindow.cs`): after the teardown, for a crash on the main thread or another: a window, device, swapchain and loop of its own (640 × 240, fixed; the editor's dark look; the engine's Lato): "Statventure Sandbox stopped because of an error", the error's line, the crash file's path in a read-only field focused and selected, Copy (the clipboard), "Open logs folder" (the file manager on `logs/`, caught), Close; when it can't be shown, step 2's terminal message; then exit 1 (`Application.CrashExitCode`). The crash is now kept and thrown only after the run's end: rethrown at once, .NET reported it unhandled (its print, on Windows WER) before the teardown and the window ran. `Application.CrashCapture` makes a scripted run's window save its first frame and close. `MessageWindow` (Q6), the same look, for step 6's command-line problems. **The sandbox:** an About button opening the engine's credits (a Close and Escape back), `--capture` counting the page's frames after the splash, `--capture-splash`, `--about`, `--crash` and `--crash-thread` for step 8. Fixtures `crash-window`, `crash-window-long`, `crash-window-no-file`, `message-window` at 1 and 1.65 within the limits (at most 0.143 % beyond 12 levels; the same on lavapipe). Tests: 32 new in `Tests/Statventure.Tests/Core/` (`ApplicationScreensTests` 11, `CrashWindowTests` 12, `CrashWindowPaintReferenceTests` 9), `ApplicationCrashTests` adjusted (no splash, no crash window, its exit caught and checked). The full suite passes on the GPU and on lavapipe inside a headless Wayland compositor under the shared lock: 3658 tests, none skipped; the player's real game folder stayed missing, the Documents folder's top level unchanged. Mutations (`~/.cache/stat-fick-build/claude-work/m12/step5-mutations.json`, made by `step5-make-mutations.py`): 67, 66 killed, the other killed by hanging the next test (shown by hand: a window closed while minimized sleeping in `WaitEvents()`); the rules only a real process shows (the crash not rethrown at once, the application's capture, the sandbox's options) by `step5-sandbox-mutate.py` against the sandbox's own check: 9, all killed; nothing written to the player's real folders. Found (for the UI's owner): a focused text field reveals its caret end where Blink reveals a range selection's bounds (an overflowing path shows its end, Chromium its start); "Copy" grayscale beside the long error's scroller where Chromium draws LCD. Not checked: Windows; the splash, the crash window, Copy and "Open logs folder" in a real window on a desktop.

## Milestones

Each milestone is finished only when its tests pass.

- [x] **M0 Setup:**
  - Solution, shared build settings, environment and build scripts.
  - Native library resolution, logging.
  - Test project, sandbox.
- [x] **M1 Platform and GPU:**
  - Window and input (GLFW).
  - Vulkan 1.3 device: dynamic rendering, synchronization2, descriptor indexing.
  - VMA memory, buffers, images, uploads, bindless textures.
  - Samplers, SPIR-V shaders compiled at build time, pipelines.
  - Swapchain with resize, offscreen targets, readback, frames in flight with deferred deletion.
  - A headless GPU test fixture with golden-image comparison.
- [x] **M2 2D canvas:**
  - Batched rectangles with per-corner radii, borders and box shadows.
  - Linear and radial gradients, images.
  - Vector paths (fill and stroke).
  - Clip stack (rectangles and rounded rectangles), 2D transforms.
  - Opacity layers and filters (blur, drop-shadow, brightness, contrast, saturate, grayscale, sepia, hue-rotate).
  - Blend modes.
- [x] **M3 Text:**
  - FreeType bindings, HarfBuzz shaping, a dynamic glyph atlas.
  - Font families and styles, fallback.
  - Layout: line breaking, alignment, ellipsis, letter-spacing, text-transform, rich spans.
  - Measurement for the layout engine.
- [x] **M4 UI:**
  - Element tree, a stylesheet language (selectors, pseudo-classes, variables, calc, media queries, keyframes, transitions).
  - Box, flex, grid and absolute layout, overflow and scrolling, stacking.
  - Painting through the canvas.
- [x] **M5 UI interaction:**
  - Hit testing, event routing, hover, active and focus states, keyboard navigation.
  - Text input, scroll views, tooltips, modals, drag and drop.
- [x] **M6 3D renderer:**
  - glTF import, meshes and materials, GPU skinning.
  - Cel shading in palette colours, ink lines (silhouette and inner edges), hatching, glow.
  - Orthographic and perspective cameras, terrain, particles.
  - 3D views inside the UI (portraits, previews).
- [x] **M7 Animation:**
  - Skeletons and clips from glTF, sampling, cross-fades, layers.
  - Events, state machines.
- [x] **M8 Audio:**
  - OpenAL Soft streaming, a mixer with buses and fades.
  - Synthesizer: oscillators, noise, envelopes, biquad filters, convolution reverb, panning, sample-accurate scheduling.
  - Offline rendering for tests.
- [x] **M9 Assets and data:**
  - The `.pak` format: writer, reader, compression, hashes.
  - A virtual file system (packs plus loose development folders).
  - Asset loaders, binary data serialization with versioning, the JSON class loader.
  - Saves and settings.
- [x] **M10 Scene and sequencer:**
  - Nodes and transforms.
  - A timeline sequencer for cutscenes: tracks for transforms, animations, camera, text, audio and effects. Tweening.
- [x] **M11 Editor foundation** (done 2026-11-08):
  - An editor app shell on the engine UI: property grid, lists, undo and redo.
  - Pack editing.
- [ ] **M12 Packaging:**
  - Linux and Windows publish into the shipped folder layout, crash logs.

# engine/ (Statventure engine + the BRUTAL port of Stat Fick)

Read `Docs/Engine-Plan.md`, section "How to continue", before doing anything here. It has the current
task, the method (Chromium 152 references and source first, tests against them), the rule to keep the
docs and notes current, and the open design questions.

**Ask the user about design choices as soon as they come up — never improvise.** Use the question tool
with 2–4 concrete options (recommended first, with the reason), then record the answer with its date in
`Docs/Engine-Plan.md`. This covers anything the player would see, hear or do differently, looks,
behaviour the old game doesn't define, data formats, folder layout and scope. Don't ask what the code,
Chromium or the docs already answer. The one exception, M11's editor (the user, 2026-11-06): its own design is
the manager's, only choices that change the game go to the user (Engine-Plan.md, "Decisions for M11"; `Docs/Editor.md`).

**Stop when a milestone is finished** (tests pass, ticked in Engine-Plan.md, notes updated): report to
the user and wait for their go-ahead before starting the next one.

- The repo root is one folder up (`../CLAUDE.md`: the machines, git, the old game). Don't delete or
  overwrite without looking first.
- Build and test: `Tools/Scripts/test.sh` (Windows: `Tools\Scripts\test.ps1`); all tests must pass
  before a step counts as done. They pass the same on the PC's GPU, on a CPU Vulkan (the cloud) and
  on Windows: they act as Chromium on Linux with the reference PC's fonts and font settings.
  Since GM1 step 2 two test projects: `Tests/Statventure.Tests` (the engine's and the editor's) and
  `Tests/Statventure.Game.Tests` (the game's: its program `Game/Statventure.Game` and its data library
  `Game/Statventure.Game.Data` referenced; the shared infrastructure linked from the engine's tests); `test.sh` runs
  both, and the suite's count is their sum. `mutate.py` takes a mutation's `"project"` (the engine's tests without one).
  `build.sh` lays out the game (`stat-fick` once published; `Statventure.Game` in `$STATVENTURE_GAME`), not the sandbox.
  Tests make their windows hidden (`WindowOptions.Visible = false`: a surface and a swapchain all the same, nothing
  shown; the default stays visible for the game). The one exception is `SwapchainTests`: a hidden surface isn't paced
  by every compositor, and wlroots hangs its five vsync frames (`Docs/Editor.md`, "How it works", step 3).
- Packs (`.pak`, `Docs/Assets.md`) in `git diff` as their dumps: once per clone, from the repo's root,
  `git config diff.pak.textconv engine/Tools/Packer/textconv.sh`.
  `Tools/Scripts/make-ui-pack.sh` makes `Assets/Packs/ui.pak` (the old game's five fonts and their licences, and the
  port's stylesheets from `Assets/UI/styles/*.css`, compiled as `ui/styles/<name>`; GM1 step 1a; since GM2 step 10 the race
  emblems' pictures `Assets/UI/emblems/<emblem>-<size>.png` at `ui/emblems/`, made from Blender's renders of the emblems
  by `Tools/Scripts/make-emblem-pictures.sh`: `Docs/Game/Hero.md`, "How it works", step 10).
  `Tools/Scripts/make-data-pack.sh` makes `Assets/Packs/data.pak` afresh from `Assets/Data/` (the game's records as JSON
  forms at their paths: the 28 races, `data/races/<id>.json`, written from the old `races.json` by the game's tests'
  converter with `STATVENTURE_RACE_DATA=write`, each race's swatches kept from its file; GM2 step 1, `Docs/Game/Hero.md`,
  "How it works", step 1). The maker's records (`data/looks/bodies|parts|presets`, GM2 step 6) enter it only with their
  models (GM2-P18): `STATVENTURE_LOOK_DATA=write` (the game's tests, `LookDataTests`) writes a body for each
  `models/humanoid/bodies/<race>-<sex>-<build>` in `models.pak` and the old game's preset of each race and sex with its
  average body (`Docs/Game/Hero.md`, "How it works", step 6), then `make-data-pack.sh`. Since GM2 step 5 `data.pak` also
  holds the first humanoid's bodies, parts and human presets and the game's look, `render/styles/game` (the engine's
  `render.style`, written from `Tools/Models/humanoid/looks/ember-hearth.json` with `STATVENTURE_STYLE_DATA=write`,
  `GameStyleTests`).
- The old game's oracle (GM2-P13, GM2 step 2; `Docs/Game/Hero.md`, "How it works", step 2):
  `Tools/Scripts/make-old-game-references.sh [module ...]` runs the old game's own code on each module's case list
  (`Tools/OldGameOracle/modules/<module>.cjs`: its pure modules in Node, the cache's; the rules inside `app.js` in its
  headless test client, inside a headless cage of its own on the PC) into `Tests/Statventure.Game.Tests/Reference/
  old-game/<module>.json`, committed, the same bytes on every run (a test fails when the old game changed under them).
  The modules now: the levelling curves and the cap, the races' fold and loading rules (step 1's oracle, folded in), the
  new hero, the sheet's values and actions, a loaded character's correction (`save-correction`, Q45) and the old game's
  72 looks (`look-catalogue`, Node over `sprite-catalog.js`; GM2 step 6). A later milestone adds a module (a case list
  and a small `run`) and its test.
- Chromium 152 for references: `Tools/Scripts/chromium.sh` (Electron 44.5.0 from npm). `Tools/PaintReference` renders a
  fixture in an iframe; a fixture with `mainFrame: true` is rendered as a page's own document, as the game's and the
  editor's documents are. Use it where a fixed box or a box composited apart sits over the page (the top layer's
  tooltips, lists and bubbles; fixed dialogs): an iframe merges it into the page's layer and keeps LCD text where the main
  frame doesn't (`Docs/Compositing-Port.md`, "The iframe and the main frame"). Its size must be whole device pixels at
  1.65 (multiples of 20 px).
- A minimized window's document (hidden documents: visibility, unloading, Blink's timer throttling; `Docs/Scripting.md`,
  "Hidden documents"): references from `Tools/Scripts/make-visibility-references.sh` (about six minutes);
  `Samples/Statventure.Sandbox` with `--log-visibility` logs what a real minimize, restore and close do. (The sandbox is
  the engine's test bed since GM1 step 2: not published, logging as `statventure-sandbox`; from a script give it a
  scratch `XDG_DATA_HOME`.)
- The Documents folder (M9, character files; `Docs/Assets.md`): `Tools/Scripts/make-documents-reference.sh` captures
  Electron's `app.getPath("documents")` (and .NET's `MyDocuments` beside it) in 46 environments (about a minute).
- Blender 5.2 for the 3D references (M6 on): `Tools/Scripts/make-blender-references.sh [--out <folder>] [scene ...]`
  (`Docs/Renderer-3D.md`, "Reference harness"). On the PC the pinned official Blender 5.2.0 in
  `~/.cache/stat-fick-build/blender-5.2/` (GM2-P10, GM2 step 3a: the references' own build; `env.sh` sets
  `$STATVENTURE_BLENDER` to it; `Tools/Scripts/blender.sh` refuses any Blender but 5.2.x, the Flatpak's 5.3 among them,
  and runs it headless only). It also evaluates
  the animation scenes (`Docs/Animation.md`, "Reference harness": poses, NLA stacks, gear) and the F-curve scenes
  (`Tools/BlenderReference/evaluate_curves.py`, `Docs/Scene.md`, "How it works", step 2), and renders the sandbox's
  figure scenes. Its renders, evaluations and test models are committed, so the tests run where there's no Blender (the
  cloud). `--out <folder>` makes everything into a scratch folder and `Tools/BlenderReference/compare_references.py
  <folder>` compares it with what is committed (how a Blender build is proven: 5.2.0's, byte for byte).
- The game's humanoid models (GM2, `Docs/Game/Hero.md`, "How it works", step 3a): the Blender script family
  `Tools/Models/humanoid/` (`manifest.json`, `look.py` and its look files in `looks/`, `render.py`) through
  `Tools/Scripts/make-humanoid.sh build|check [name ...]` (builds the manifest's models; `check` rebuilds them into
  scratch and compares byte for byte), `render --out <folder> --name <stem> --model <glb> --look <look> --views
  front,three-quarter,side,back,close-up [...]` (a figure or a scene file in a look, drawn by the references' own
  Blender side, the ink laid over the shading as the engine lays it), `look list|write|check`, `framing`. The engine's
  playing view of a figure in a look: the sandbox's figure mode, `Tools/Scripts/play-figure.sh <body.glb> [--clips
  <glb>] [--play <clip>[+<clip>][:times],...] [--look <look>] [--view ...] [--size WxH] [--dump <folder> [--label]]`
  (keys: Space, Right/Left, Down/Up, A/D, 1-5, Escape; `--dump` writes frames and, with ffmpeg, `playing.mp4`, headless
  in a cage of its own; `--play` plays the named clips in turn, `idle+hold-bow` one laid over another, `--label` names
  the clip on a dump's frames: GM2 step 4b).
  Since step 4a (`Docs/Game/Hero.md`, "How it works", step 4a) the first humanoid's models are built by the family's
  scripts (`manifest.json`: each model's builder, arguments and detail level, `levels.py`'s "full", which the game
  ships, or "battle", the geared man's in the tests' data) into `Assets/Models/humanoid/`, always with 8 Blender threads
  (QuadriFlow follows the count), every model's pieces joined per ink flag; `make-humanoid.sh clips <folder>
  <library.glb>` keys a folder of clip files (`clips/library.py` gives their form; the frame's test clip `rest-sway` is the
  tests' data); `Tools/Scripts/make-models-pack.sh` makes `Assets/Packs/models.pak` afresh from `Assets/Models/`. Since step 4b
  (`Docs/Game/Hero.md`, "How it works", step 4b) the family's 34 approved clips are data in `Tools/Models/humanoid/clips/`
  (the manager's, as keyed; the keying scripts come later, GM2-P23), built by the manifest's `clips` into
  `Assets/Models/humanoid/clips.glb` (`models.pak`'s `models/humanoid/clips`), held to Blender on both bodies
  (`AnimationScenes`' `humanoid-<clip>-male|female`); the figures' small state machine is
  `Assets/Models/humanoid/machines/figure.json` (`models/humanoid/machines/figure`: `stand` looped, Q57). The
  figures are held to Blender's under `ReferenceLimits.DetailedFigure` (the user's Q55, tightened by Q58 and Q60 after
  GM2's ink task: each sample's sheet, a covered edge hidden as Freestyle hides it and the meshes' silhouettes,
  `Docs/Renderer-3D.md`, "Feature edges, how they're drawn"; a limit stays widened only where a scene measures past M6's,
  at the worst plus about a tenth).
  Since step 5 (`Docs/Game/Hero.md`, "How it works", step 5) the game builds a hero's figure from its look
  (`Game/Statventure.Game/Figures/HeroFigure.cs`), and `Tools/Models/humanoid/assemble.py` assembles the same look in
  Blender from the data: `make-humanoid.sh assemble --out <folder> --name <stem> --look-hero <look.json> --data <folder>
  [...]` for pictures; `make-blender-references.sh` assembles and renders the game's tests' samples
  (`Reference/assembly-looks.json`, written with `STATVENTURE_ASSEMBLY_LOOKS=write`, `AssemblyTests`).
  Since step 10 (`Docs/Game/Hero.md`, "How it works", step 10) the nine race emblems: medallions in relief built by
  `Tools/Models/humanoid/emblems/medallion.py` from the chosen outlines `emblems/chosen.json` (the manifest's
  `emblems/<name>`, the races' `emblem` values; `models.pak`'s `models/humanoid/emblems/<name>`), coloured from the race's
  accent by one rule (`Game/Statventure.Game/Figures/Emblems.cs`, `emblems/palette.py`), held to Blender under
  `ReferenceLimits.Emblem` (the user's Q70); their pictures in `ui.pak` made by `Tools/Scripts/make-emblem-pictures.sh`
  (the game's tests write the scenes, Blender renders them, the tests make the pictures, `make-ui-pack.sh` packs them).
- Cutscenes (M10, `Docs/Scene.md`, step 7): `Tools/Scripts/play-cutscene.sh jiruan [--volume 0-100]` plays Ji'ruan's intro, the proof, with its sound in
  the sandbox's window (Escape or its Skip button skips, R replays; `--dump <folder>` writes its frames instead);
  `Tools/Scripts/make-cutscene-references.sh` captures the old game's card in Chromium at the sampled times.
- The editor (M11, `Docs/Editor.md`): `Tools/Scripts/editor.sh [--player-folder <folder>] [--capture <file.png>
  [--frames N]]` runs the Statventure Editor from the repo (the dev setup, `--repo`, with the game's data library
  `Game/Statventure.Game.Data` built beside it; Release; from a script, pass a scratch player folder; `--capture` draws N frames, 30 by default, after the
  splash, saves the last and quits). The program's own command line (M12 step 6, `Docs/Packaging.md`): `statventure-editor
  [--repo <engine folder> | --game <game folder>] [--player-folder <folder>] [--capture <file.png> [--frames N]]`;
  without `--repo` or `--game` it takes the game's folder it remembers (`editor/game.json`), else asks "Where is Stat
  Fick?". Its screens against Chromium: run the tests once (they write `Reference/editor-fixtures.json`), then
  `Tools/Scripts/make-editor-references.sh [fixture ...]` (PaintReference at 1 and 1.65, an invisible window; only the
  named fixtures when given; from a headless compositor, make its output large, 2400 × 2400, at both scales).
- Packaging (M12, `Docs/Packaging.md`): the decisions (two downloads, the game and the editor; the detail round Q1–Q28
  and Q29–Q30, the BRUTAL splash and credits, answered 2026-11-08), what exists, the method, the design and
  the eight steps. `Tools/Scripts/publish.sh [--version <v>]` (Windows: `publish.ps1`) makes the four archives
  (`stat-fick-<v>-linux.tar.gz`, `-windows.zip`, `statventure-editor-…`) into `release/` and checks them with
  `Tools/Scripts/check-packages.sh` (`--self-test` breaks each one rule at a time); `Tools/Scripts/end-to-end.sh
  [--publish] --version <v>` runs the two Linux packages as a player and a modder would, headless (since GM1 step 11 the
  game's own flow: its first start, settings, About, crashes, a second start, the editor's mod and two broken ones, a
  character made in-process by the game's `PlayerFolderTests` continued and loaded with `--run`, a mod switched off;
  the start screen and the windows judged by the approved look's colours read from their Chromium goldens; about 115 s
  on the GPU, 215 s on lavapipe; not in
  `test.sh`); the listings, licence texts and the Visual C++ runtime's pin are in `Tools/Packaging/`;
  `Tools/Scripts/editor.ps1` is `editor.sh` for Windows. Every M12 choice is asked, the editor's packaging too (M11's
  exception ended with M11).
  `Tools/Scripts/release.sh [--dry-run | --self-test]` makes a release, a draft on GitHub (a real one only on the user's word).
- Audio (M8, `Docs/Audio.md`): `Tools/Scripts/play-theme.sh [theme] [--volume 0-100]` plays the music on the real
  device, to listen to it (in a real terminal; testing it from a script, send only volume 0).
  `Tools/Scripts/play-sounds.sh [name ...] [--volume 0-100] [--candidates <folder>]` plays the game's sound effects the
  same way (GM1 step 4, `Docs/Audio.md`, "The game's sound effects": every effect in `audio.pak` with no names; the
  effects volume, 60 by default as the game's first start; `--candidates` a folder of patches to have approved by ear).
  `Tools/Scripts/audio-benchmark.sh [--split | --start [--warm] | --live | --effects] [theme ...]` times it (Release).
  References: `make-audio-references.sh [names]` (fixtures), `make-music-data.sh` (the music importer: the old
  JavaScript's themes, instruments, percussion and rooms, and the game's sound effects from `Assets/Audio/effects/`,
  into `Assets/Packs/audio.pak`; on the PC from a script inside a headless compositor), `make-music-references.sh`,
  `make-native-references.sh`, `make-v8-math-reference.sh`. After changing the synthesizer also run `test-themes.sh`
  (about 20 minutes; `make-theme-references.sh` once per machine first, 2.6 GB in the build cache, half an hour or
  more); the normal suite doesn't need those.
- Tests must be able to fail: `Tools/Scripts/mutate.py <list.json>` breaks one rule at a time and reports what no test
  noticed (`Docs/Engine-Plan.md`, "How each step is finished"). Keep working files in
  `~/.cache/stat-fick-build/claude-work/`: a session's scratch folder is lost when the session restarts.
- On the PC a step is also checked on the CPU Vulkan the cloud uses (Mesa's lavapipe):
  `VK_DRIVER_FILES=/usr/share/vulkan/icd.d/lvp_icd.x86_64.json VK_ICD_FILENAMES=$VK_DRIVER_FILES Tools/Scripts/test.sh`.
- Reference sources: `~/.cache/stat-fick-build/reference-src/` (save new downloads there).
- The port of the game (after M12): `Docs/Game-Plan.md`, the game's milestones GM1–GM18, GM12 dissolved into GM4, GM6, GM9, GM10 and GM11 by the user's GP-9 (what each builds, its art,
  its checks against the old game, Blender and Chromium, its questions for the user); read its "How to use this plan"
  first.
- GM1 (Foundation), done 2026-11-15 (started 2026-11-13; 4130 + 565 = 4695 tests): its design doc
  `Docs/Game/Foundation.md` (the user's and the manager's decisions, what exists, the design with the program's seams,
  the steps, how each works, the user's "Checks by hand on Linux"). The next, GM2, started on the user's go (below).
  The old game was frozen for the recapture from 2026-11-13 until the manager's ok of 2026-11-15; a later change to it
  makes the game-screen references stale again (`Docs/Game/Foundation.md`, "Decisions").
- GM2 (The hero: the sheet, races and looks), started 2026-11-15 on the user's go: its design doc `Docs/Game/Hero.md`
  (the user's GM2-1–GM2-8 and detail round Q1–Q42, GP-8 the user's word on the 3D models, the manager's GM2-P1–P16; what
  exists, the method, the design, what GM2 makes with its counts, the steps), decided; no step started. Its 3D work runs
  in Blender 5.2 through `$STATVENTURE_BLENDER` (the PC's Flatpak is 5.3; GM2-P10): the pinned 5.2.0 since step 3a.
- The symbols' icons (GM1-7, `Docs/Game/Foundation.md`, "How it works", step 3): the outlines and their box table in
  `Assets/UI/icons/`, written by `Tools/IconSheet/gen/build.py` (Python with shapely and fonttools; its README; `--check`
  after any change). `Tools/Scripts/make-icon-sheet.sh [--out <folder>] [--icons label=folder ...] [--symbols …]` draws
  every icon in Chromium beside the old glyph at each of the old game's uses (`Tools/IconSheet/uses.json`), both themes,
  the small ones enlarged, and at 96 px over its box (PaintReference; inside a headless cage of its own when cage
  exists); `--uses` measures the old game's uses again. Since step 7 the engine draws them: `FontLibrary.AddIcons`
  (`Text/IconFace.cs`) takes the icons' characters before any font, the canvas fills their outlines; the game's table
  `Game/Statventure.Game/Icons/GameIconTable.cs` is written from `Assets/UI/icons/` by the game's tests
  (`STATVENTURE_ICON_TABLE=write`, then rebuild) and given to a library by `GameIcons.AddTo`;
  `Tools/Scripts/make-icon-references.sh [scale …]` captures the icon fixtures (`Reference/icon-fixtures.json`, written
  by the tests) in Chromium (on the PC inside a headless cage with a 2400 × 2400 output).
- The game's program (GM1 step 2, `Docs/Game/Foundation.md`, "How it works", step 2): `Game/Statventure.Game`
  (`StatFickGame`, its parts in folders of their own, each a later step's; one instance per per-user folder), and its
  data library `Game/Statventure.Game.Data`; its command line for checks only (`--capture <png> [--frames N]`,
  `--capture-splash <png>`, `--about`, `--open <screen>`, `--run <command>[=<argument>]` (GM1-P20: any command as a
  click runs it, e.g. `--run continue`, `--run load-character=<file>`; never `open-game-folder` from a script, it opens
  a file manager), `--crash`, `--crash-thread`); run it from a script as
  `build.sh` lays it out (`$STATVENTURE_GAME/Statventure.Game`) with a scratch `XDG_DATA_HOME` and `ALSOFT_DRIVERS=null`,
  inside a headless cage.
- The game's screens against Chromium (GM1-P17, `Docs/Game/Foundation.md`, "How it works", step 8): the game's tests
  write `Reference/game-screen-fixtures.json` (`Tests/Statventure.Game.Tests/Ui/GameScreenFixtures.cs`; a part's fixtures
  in a class of its own implementing `IGameScreenFixtures`); `Tools/Scripts/make-game-screen-references.sh [fixture ...]`
  captures them as main frames (`mainFrame`, sizes in multiples of 20 px) at 1 and 1.65 into `Golden/game-screen-*.png`
  (on the PC inside a headless cage with a 2400 × 2400 output).
- The survey the game plan was written from (2026-11-12, `29d110d`; a snapshot): `Docs/Game/Survey/`, its README first.
- Notes kept from earlier sessions (history, gotchas, recipes): `Docs/Notes.md`.

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
  Tests make their windows hidden (`WindowOptions.Visible = false`: a surface and a swapchain all the same, nothing
  shown; the default stays visible for the game). The one exception is `SwapchainTests`: a hidden surface isn't paced
  by every compositor, and wlroots hangs its five vsync frames (`Docs/Editor.md`, "How it works", step 3).
- Packs (`.pak`, `Docs/Assets.md`) in `git diff` as their dumps: once per clone, from the repo's root,
  `git config diff.pak.textconv engine/Tools/Packer/textconv.sh`.
  `Tools/Scripts/make-ui-pack.sh` makes `Assets/Packs/ui.pak` (the old game's five fonts and their licences).
- Chromium 152 for references: `Tools/Scripts/chromium.sh` (Electron 44.5.0 from npm).
- A minimized window's document (hidden documents: visibility, unloading, Blink's timer throttling; `Docs/Scripting.md`,
  "Hidden documents"): references from `Tools/Scripts/make-visibility-references.sh` (about six minutes);
  `Samples/Statventure.Sandbox` with `--log-visibility` logs what a real minimize, restore and close do.
- The Documents folder (M9, character files; `Docs/Assets.md`): `Tools/Scripts/make-documents-reference.sh` captures
  Electron's `app.getPath("documents")` (and .NET's `MyDocuments` beside it) in 46 environments (about a minute).
- Blender 5.2 for the 3D references (M6 on): `Tools/Scripts/make-blender-references.sh [scene ...]`
  (`Docs/Renderer-3D.md`, "Reference harness"; on the PC the Flatpak, run headless). It also evaluates
  the animation scenes (`Docs/Animation.md`, "Reference harness": poses, NLA stacks, gear) and the F-curve scenes
  (`Tools/BlenderReference/evaluate_curves.py`, `Docs/Scene.md`, "How it works", step 2). Its renders,
  evaluations and test models are committed, so the tests run where there's no Blender (the cloud).
- Cutscenes (M10, `Docs/Scene.md`, step 7): `Tools/Scripts/play-cutscene.sh jiruan [--volume 0-100]` plays Ji'ruan's intro, the proof, with its sound in
  the sandbox's window (Escape or its Skip button skips, R replays; `--dump <folder>` writes its frames instead);
  `Tools/Scripts/make-cutscene-references.sh` captures the old game's card in Chromium at the sampled times.
- The editor (M11, `Docs/Editor.md`): `Tools/Scripts/editor.sh [--player-folder <folder>] [--capture <file.png>
  [--frames N]]` runs the Statventure Editor from the repo (the dev setup, `--repo`, with the game data library built
  beside it; Release; from a script, pass a scratch player folder; `--capture` draws N frames, 30 by default, after the
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
  [--publish] --version <v>` runs the two Linux packages as a player and a modder would, headless (about 90 s; not in
  `test.sh`); the listings, licence texts and the Visual C++ runtime's pin are in `Tools/Packaging/`;
  `Tools/Scripts/editor.ps1` is `editor.sh` for Windows. Every M12 choice is asked, the editor's packaging too (M11's
  exception ended with M11).
  `Tools/Scripts/release.sh [--dry-run | --self-test]` makes a release, a draft on GitHub (a real one only on the user's word).
- Audio (M8, `Docs/Audio.md`): `Tools/Scripts/play-theme.sh [theme] [--volume 0-100]` plays the music on the real
  device, to listen to it (in a real terminal; testing it from a script, send only volume 0).
  `Tools/Scripts/audio-benchmark.sh [--split | --start [--warm] | --live | --effects] [theme ...]` times it (Release).
  References: `make-audio-references.sh [names]` (fixtures), `make-music-data.sh` (the music importer: the old
  JavaScript's themes, instruments, percussion and rooms into `Assets/Packs/audio.pak`), `make-music-references.sh`,
  `make-native-references.sh`, `make-v8-math-reference.sh`. After changing the synthesizer also run `test-themes.sh`
  (about 20 minutes; `make-theme-references.sh` once per machine first, 2.6 GB in the build cache, half an hour or
  more); the normal suite doesn't need those.
- Tests must be able to fail: `Tools/Scripts/mutate.py <list.json>` breaks one rule at a time and reports what no test
  noticed (`Docs/Engine-Plan.md`, "How each step is finished"). Keep working files in
  `~/.cache/stat-fick-build/claude-work/`: a session's scratch folder is lost when the session restarts.
- On the PC a step is also checked on the CPU Vulkan the cloud uses (Mesa's lavapipe):
  `VK_DRIVER_FILES=/usr/share/vulkan/icd.d/lvp_icd.x86_64.json VK_ICD_FILENAMES=$VK_DRIVER_FILES Tools/Scripts/test.sh`.
- Reference sources: `~/.cache/stat-fick-build/reference-src/` (save new downloads there).
- Notes kept from earlier sessions (history, gotchas, recipes): `Docs/Notes.md`.

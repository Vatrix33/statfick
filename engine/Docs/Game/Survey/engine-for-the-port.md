# The engine for the port: what Statventure gives, what it leaves, how milestones run

Survey for planning the port of `old-game/` onto `engine/` (Statventure) as milestones. Written 2026-11-12
on `main` at `29d110d` (M12 done, 3720 tests). It is read-only work: nothing in the repo was changed.

**Sources:** `engine/CLAUDE.md`, the repo's `CLAUDE.md`, and every `engine/Docs/*.md` except `Docs/Port-Tasks/`,
which another worker covers. Engine-Plan's port-task decision sections (tasks 1–14, lines about 1446–2900)
were only skimmed for structure. `Engine-Plan.md`, `Compositing-Port.md`, `Scripting.md`, `Renderer-3D.md`,
`Animation.md`, `Audio.md` and `Notes.md` were read directly; `Notes.md`'s dated history only in part. The
four largest design docs (`Editor.md` 3444 lines, `Scene.md` 2276, `Packaging.md` 2223, `Assets.md` 1838)
were each read in full by a reader of its own, and the quotes below were spot-checked against the files.
The engine's source tree was looked at only to confirm names.

**Pointers** read "Doc, section, subsection" and use the docs' own headings in quotes, for example
Engine-Plan.md, "How to continue", "What M9 leaves for the port".

---

## 1. How this project runs a milestone

### 1.1 The milestone list

Engine-Plan.md ends with `## Milestones`, in this exact form:

```
## Milestones

Each milestone is finished only when its tests pass.

- [x] **M0 Setup:**
  - Solution, shared build settings, environment and build scripts.
  ...
- [x] **M11 Editor foundation** (done 2026-11-08):
  - An editor app shell on the engine UI: property grid, lists, undo and redo.
  - Pack editing.
- [x] **M12 Packaging** (done 2026-11-12):
  - Linux and Windows publish into the shipped folder layout, crash logs.
```

Each entry is a checkbox, the milestone's number and name in bold, and its scope as plain sub-bullets. The
list is M0 Setup, M1 Platform and GPU, M2 2D canvas, M3 Text, M4 UI, M5 UI interaction, M6 3D renderer,
M7 Animation, M8 Audio, M9 Assets and data, M10 Scene and sequencer, M11 Editor foundation and M12
Packaging. All are ticked. A milestone is ticked here only when it is finished, and since M11 the entry
carries "(done <date>)".

Engine-Plan.md has three other parts the plan keeps up to date:
- **`## How to continue (read this first)`** opens with a bold **"Current task (<date>): …"** paragraph. It
  also holds "Small tasks left from earlier milestones", "Windows check", "How each step is finished", the
  "What Mn leaves for the port" paragraphs, "Design docs by area", "Method", "Keep the notes current",
  "Stop at the end of each milestone", "Ask the user about design choices — at once, never improvise." and
  "Open questions for later milestones", which ends in its "At port time" item.
- **`## User decisions (2026-10-16)`** is followed by "Decisions for M5" through "Decisions for M12". Each
  holds the milestone-level questions (`M10-1`, `M11-4`, `M12-9` …) with their dated answers, then the
  "Decision on …" entries for work done beside the milestones, then the port tasks' decisions.
- **`## Progress`** has one bullet per milestone for M0–M5 ("**M5 is done** (2026-10-17 to 10-30). 496
  tests."). From M6 on, the entries are sub-bullets in the forms:
  - "**M10 started (date):** the user's go …"
  - "**M11 step 9, the form, undo and the JSON view (2026-11-07, 3089 tests, on its worker branch
    `m11-step9`, rebased on `6a8cd78` …):**"
  - "**Mn done (date, N tests):** … Ticked under Milestones; M(n+1) waits for the user's go."

### 1.2 How a milestone starts

Engine-Plan.md, "How to continue", "Starting M9". This is the order M9 to M12 followed:

> it started on 2026-11-02 with the user's go, in the order M10 followed too (2026-11-04), M11 (2026-11-06; its
> design doc `Docs/Editor.md` …) and M12 (2026-11-08; its design doc `Docs/Packaging.md`, its twelve questions
> asked first, the details its survey raised after): `git pull` and read; check the code against the facts and
> work out what each question needs deciding; ask the questions all at once (the question tool takes 4 per call,
> 2–4 options each, the recommended one first); record the answers ("Decisions for M9"); write the design doc
> (`Docs/Assets.md`: decisions, what exists checked against the code, method, design, steps, how it works,
> status) and split the milestone into steps; do each step as "How each step is finished" says.

Also from "How to continue":

> Before each later milestone (M11 on) starts, first ask its questions under "Open questions for later
> milestones" below (all at once, with the question tool), record the answers, and only then plan and write
> code, in the order "Starting M9" below gives

The user's go is recorded word for word in the milestone's decisions:
- M11: "Once that worker finishes you can start with M11".
- M12: "Then start with M12".

### 1.3 The design doc per milestone

"A new area (M6 on) gets its own design doc in `Docs/`" (Engine-Plan.md, "Design docs by area"). M0–M5 have
none: they are described in Progress. Two M5 subjects have docs of their own: `Compositing-Port.md` and
`Scripting.md`.

| Doc | Milestone | Top-level sections, in order |
| --- | --- | --- |
| `Renderer-3D.md` | M6 | Decisions (user, 2026-10-30) · Method · Conventions (how each part works) · Reference harness · Steps · Status |
| `Animation.md` | M7 | Decisions (user, 2026-10-31) · Method · Rules from Blender and glTF (to port) · How it's done · Reference harness · Steps · Status |
| `Audio.md` | M8 | Decisions (user, 2026-10-31) · What the old game uses (surveyed 2026-10-31) · Method · Steps · How it works (one subsection per step) · Status |
| `Assets.md` | M9 | Decisions (user, 2026-11-02) · What exists (checked against the code, 2026-11-02) · Method · Design · Steps · Format details (user, 2026-11-02, asked after the design doc) · How it works · Status |
| `Scene.md` | M10 | Decisions (user, 2026-11-04) · What exists (checked against the code, 2026-11-04) · The old game's cutscenes (facts) · Method · Design · Steps · Step details (user, 2026-11-04, asked after the design doc) · How it works · Status |
| `Editor.md` | M11 | Decisions (user, 2026-11-05 and 2026-11-06; the manager's for the editor, 2026-11-06) · What exists (checked against the code, 2026-11-06) · Method · Design (ending in "Decided by the manager (P1–P38)" and "The game's questions (G1–G6)") · Steps · How it works · Status |
| `Packaging.md` | M12 | Decisions (user, 2026-11-08) · What exists (checked against the code, 2026-11-08) · Method · Design · Decided (Q1–Q28, the user, 2026-11-08) · Decided (Q29–Q30, the user, 2026-11-08), from Q11's answer · Decided (the logo's file, the user, 2026-11-08), from step 4's check · Left to the steps · Steps · How it works · Status |

The pattern, as M9–M12 settled it:

**Questions are numbered by round.**
- Milestone-level questions are `M12-1`…`M12-12`, recorded in Engine-Plan.md "Decisions for M12" and
  summarised at the top of the design doc.
- The detail round, asked after the design doc, is `Q1`…`Qn` (in "Format details", "Step details" or
  "Decided (Q1–Q28 …)").
- Questions asked during a step are numbered by that step: `S3-1`, `S5-Q1`, `S6-A`…`S6-F`, `S7-1`…`S7-14`,
  `S9-A`…`S9-D`. Assets.md explains why: questions asked during steps 3 and later are numbered S3-n and so
  on "because step 4, running beside it, asked Q38 on".
- M11 adds `P1`–`P38` (the manager's editor choices) and `G1`–`G6` (the editor's questions about the game).
- Each answer is dated and says who decided it. When the user did not take the recommended option, the answer
  is marked "**…, not the recommendation:**", and the options not taken are listed as "(Not …; not ….)".
- A milestone's summary line reads like "every answer was the recommended option except the scope (M10-2)".

**"What exists (checked against the code, <date>)"** surveys the code before the design. Later corrections are
noted inline, for example "(Corrected 2026-11-03, step N…)".

**"Method"** states which reference each kind of rule is checked against. Scene.md, for example: "Three kinds
of rule, three kinds of check".

**"Design"** gives one subsection per area, tagged with the step that builds it ("The form (step 9)").

**"Left to the steps"** (Packaging.md only):

> Small internal choices, each recorded by the step that makes it (a step that finds one visible to players
> stops and asks)

**Steps** are numbered.
- In Editor.md and Packaging.md each step lists **Owns:**, **Must not touch:** and **Finished:** (with the
  mutations it needs). Later a **Done (date, "How it works", step N)** line is added, which also records the
  manager's decisions.
- Steps run in **waves**. Assets: "**1 and 2** side by side; then **3** (after 1 and 2) beside **4** …; **9**
  last". Scene: seven steps; "Steps 1, 2, 3 and 7 make Blender references, so they run on the user's PC".
  Editor: 14 steps in four waves. Packaging: "**wave 1:** 1, 2, 3 and 4 side by side; **wave 2:** 5 …, 6 … and 7
  … side by side; **wave 3:** 8 (after all). Then the manager ticks M12 and stops."
- Steps that run side by side "touch no file in common besides those docs (each its own entry)".
- Each step "ends as every step does (`Docs/Engine-Plan.md`, "How each step is finished")".

**"How it works"** is "Filled in as the steps are done". It has one subsection per step: the files, the API,
the manager's decisions, "Found" (bugs or rules found on the way), "Not checked" (by hand, Windows), the
mutations, and "Choices the design left to the step".

**"Status"** reads "**M10 done (2026-11-05): all seven steps …**", then one dated entry per step with its test
count on the GPU and on lavapipe, the mutations killed and accounted for, and what was not checked.

**A proof or approval round where the work is judged by the user (M10).** The proof cutscene's stage was "shown
to the user as Blender renders for approval before its tests are written" (Q31). The first round found it "too
simple" and gave S7-1–S7-4. The second was approved: "The scene is perfect. I can't fully judge it because of
the placeholder model, but it's very good. Note down somewhere that I want the boss model to be as detailed
and high-quality as the scene." (Scene.md, "Step details", S7-5.) Limits measured on the way went to the user as
questions: the ink study (S7-12), then the limits approved exactly as set (S7-13).

**Measure, then ask (M11 G5).** "the step measures the added loading time first, and if it is noticeable the
manager goes back to the user before it goes in". The music's check was then wired on the user's decision, with
the numbers shown (Engine-Plan.md, "Decisions for M11").

**Held tests.** A test that waits on a fix being done elsewhere is "held", and the count is recorded:
- "2516 tests and 6 held" at M10's end.
- `JiruanCardTests` at 1.4 s was held until the shadow-blur port.

**What each milestone leaves.** After each milestone, Engine-Plan.md "How to continue" gets a "**What Mn leaves
for the port:**" paragraph (M7–M11). M12's is spread over "Current task" and Packaging.md (§3 below).

### 1.4 "How each step is finished" (verbatim)

Engine-Plan.md, "How to continue":

> **How each step is finished** (the user's standing rules, for every step of every milestone):
> 1. Tests: `Tools/Scripts/test.sh` passes in full on the GPU and on lavapipe (the variables are in `CLAUDE.md`). Don't
>    tune numbers until a test passes: find the rule. A number the user chose, or a tolerance, isn't yours to change to
>    make a test pass; ask. Report failures as they are; a flaky test gets its cause found, not a wider margin. Since M11,
>    with workers side by side (the manager's practice, 2026-11-06 and 2026-11-07): every full run, on either driver, and
>    every `mutate.py` run takes the shared lock (`flock -o ~/.cache/stat-fick-build/claude-work/full-run.lock …`;
>    `Docs/Editor.md`, "Steps"); full runs, and mutation runs whose tests open windows, run inside a headless compositor
>    (`cage` on wlroots' headless backend, the user's displays unset), so no window reaches the user's screen; the
>    engine's test windows are hidden since M11 step 3, all but `SwapchainTests`' (`engine/CLAUDE.md`). The wrappers use
>    `flock -o` and set `MSBUILDDISABLENODEREUSE=1 DOTNET_CLI_USE_MSBUILD_SERVER=0`, because MSBuild's reusable nodes
>    otherwise outlive the run and keep the lock, and end their `cage` when its command ends
>    (`claude-work/m11/step5-full-run.sh` and `step5-headless.sh` are the model).
> 2. New tests can fail: break each rule they cover, one at a time (`Tools/Scripts/mutate.py`), and account for every
>    survivor: nothing can differ (say why) or it's a gap to close. Report the count. Mutation runs are also checked for
>    writes to the player's real folders (decided 2026-11-05, "Decision on test runs and the player's folders"): `mutate.py`
>    prints `!!! WROTE TO THE PLAYER'S REAL FOLDERS` (and records it in the results file) when a mutation changed this
>    machine's game folder or the Documents folder's top level; any such line is a bug to find before the step is done.
> 3. Docs: this file's Progress section (and "Current task"), the area's design doc, `Docs/Notes.md`, and on the user's
>    PC Claude's memory file `stat-fick-port.md`.
> 4. Commit each finished step to `main` and push, with the commands in `../CLAUDE.md` (a cloud session pushes a branch
>    instead). Everything on GitHub stays private.
> 5. At the end of a milestone: tick it under Milestones, update the notes, report what was done and what wasn't checked,
>    and stop until the user says to go on.

On the PC the lavapipe run is
`VK_DRIVER_FILES=/usr/share/vulkan/icd.d/lvp_icd.x86_64.json VK_ICD_FILENAMES=$VK_DRIVER_FILES Tools/Scripts/test.sh`
(engine/CLAUDE.md).

**Related standing rules** (engine/CLAUDE.md; Engine-Plan.md, "How to continue"):
- **Hidden windows.** Tests make their windows hidden with `WindowOptions.Visible = false`. The one exception
  is `SwapchainTests`.
- **Volume 0.** "when a script or a session plays audio, it sends only volume 0 (it is audible on the user's
  headset); listening is the user's, in a real terminal".
- **No windows on the PC.** "no window may be shown on the PC from a session".
- **Working files** go in `~/.cache/stat-fick-build/claude-work/`, because "a session's scratch folder is lost
  when the session restarts".
- **Packaging steps** also run `check-packages.sh` where they make packages (Packaging.md, "Steps").

**"Keep the notes current"**:

> After each finished step: this file's Progress section, the design doc of the area …, and `Docs/Notes.md` for
> history and gotchas (on the user's PC also Claude's memory file `stat-fick-port.md`; cloud sessions only see the
> repo). Write the state down before a long session's context runs out, so the next chat can pick up from the notes
> alone.

**"Stop at the end of each milestone"**:

> When a milestone's work is done and its tests pass: tick it in Milestones, update the notes, report to the user
> what was done, and stop. Don't start the next milestone until the user says to.

### 1.5 The asking rule

Engine-Plan.md, "Ask the user about design choices — at once, never improvise.":

> The user's standing rule: "If any questions arise, ask them instantly and do not improvise anything."
> - Ask about: anything the player would see, hear or do differently; looks and styles; behaviour the
>   old game or Chromium doesn't define; trade-offs between matching Chromium and something else; data
>   formats, file and folder layout; scope (cutting, adding or deferring a feature); anything in the
>   open questions below.
> - Don't ask about what has a clear answer in the code, in Chromium's behaviour or in this file.
> - How: the question tool with 2–4 concrete options, the recommended one first with its reason, as soon
>   as the question comes up — don't pick a default and carry on, don't save questions for the end.
>   Record each answer with its date under "User decisions" (or the milestone's decisions).

**The M11 exception.** The user (2026-11-06): "For M11, only concern me with questions that are meant for the
game itself. The editor is at your full disgression, but do inform me of big decisions". Under it:
- the manager decided the editor's own design (P1–P38);
- "workers still stop and report such a choice to the manager instead of assuming";
- choices that change the game still went to the user.

**"The exception ended with M11: from M12 (2026-11-08) every design choice is asked again."** The root
CLAUDE.md states the rule as "Ask about every design choice as soon as it comes up … Never improvise or carry on
with an assumed answer". `Notes.md`, "Asking about design choices (user rule)", adds:

> Ask before starting each later milestone (M6–M12) using the open-question list in Engine-Plan.md's "How to
> continue" section.

### 1.6 The manager and the workers

The docs describe the arrangement in pieces:
- **Merging** (user decision 2026-11-02; Engine-Plan.md, "How to continue"): "workers in git worktrees
  (`.claude/worktrees/`) commit on their own branch, and a separate, non-isolated merge worker merges into
  `main` and pushes, because the harness refuses git commands aimed at the main checkout from a worktree".
  Notes.md (2026-11-02) says the same.
- **Concurrency** (Packaging.md, "Steps"; Editor.md, "Steps" alike):

  > At most four workers at once, each in its own worktree; **only one at a time runs the full
  > `Tools/Scripts/test.sh` or `mutate.py`**, serialised by the shared lock … filtered runs in a worker's own
  > worktree need no lock. Everything headless (no window on the user's screen), nothing audible, nothing
  > written into the player's real folders … Workers don't touch Engine-Plan's "Current task" (the merge
  > worker does). Steps side by side touch no file in common besides those docs (each its own entry).
- **Who decides.**
  - Under the M11 rule, "workers still stop and report such a choice to the manager instead of assuming".
  - In M10 and M12, the step-internal choices were "accepted by the M10 manager", or recorded as "the manager's
    decision (a), 2026-11-12" in the step's "How it works".
  - Loading-time costs were measured and the step stopped "for the manager's word" (Editor.md, steps 5 and 6).
  - Side fixes found during a step got "a worker of its own", for example `lcd-first-text` and the canvas's
    shadow fixes.
- **Branches.** Worker branches are named `m11-step1`…`m11-step14`, `m11-fixes-1`, `m12-design`,
  `m12-step1`…`m12-step8` and `m12-step4-credits`. Topical fixes have their own names: `lcd-first-text`,
  `shadow-inset-caster`, `background-layers-aa`. A Progress entry names its branch and the commit it was
  rebased on.
- **Cleanup.** "worker worktrees are removed after each merge from now on". The housekeeping of 2026-11-12 removed
  36 worktrees and 75 branches; "only `main` and its checkout remain" (Engine-Plan.md, "Small tasks left from
  earlier milestones", "Housekeeping").
- **Shared lock and headless compositor.** The lock is `flock -o ~/.cache/stat-fick-build/claude-work/full-run.lock`.
  `cage` runs on wlroots' headless backend (`WLR_BACKENDS=headless`). Editor screen captures run at 2400 × 2400
  (`wlr-randr --output HEADLESS-1 --custom-mode 2400x2400`), one fixture per run at 1.65. "End a wrapper's own
  cage when its command ends (a cage outliving its child held the shared lock once)." (Editor.md, "Method".)

### 1.7 Where the work happens

The repo's CLAUDE.md, "Where this is worked on" (decided 2026-10-30), and Engine-Plan.md, "Decisions for the move
to GitHub":

| Machine | What it is | What runs there | Git |
| --- | --- | --- | --- |
| **The user's PC** | Fedora, AMD GPU (Radeon RX 9070), the repo at `/run/media/system/M.2_2TB/Stat Fick Repo/` | The full suite on the GPU and on lavapipe. Chromium reference captures. Blender 5.2 (Flatpak, headless) references. The reference sources cache `~/.cache/stat-fick-build/reference-src/` exists only here. `$STATVENTURE_GAME` is `/run/media/system/M.2_2TB/Stat Fick/`. No window may be shown from a session. | Commits finished steps to `main` and pushes |
| **Claude Code cloud sessions** (also from the phone app) | Ubuntu 24.04, no GPU. Setup script `engine/Tools/Scripts/cloud-setup.sh` (.NET 10, Mesa's lavapipe, Xvfb, Electron 44.5.0) | Tests on lavapipe. Captures under Xvfb. Chromium and Skia source come from googlesource if the domains are allowed, else GitHub's mirror (`raw.githubusercontent.com/chromium/chromium/152.0.7977.89/<path>`). No Blender: renders, evaluations and test models are committed. | Pushes its own branch and opens a pull request; the user merges it |
| **Windows PC** | `engine\Tools\Scripts\setup.ps1` (once), `build.ps1`, `test.ps1` | Build, run and test only; no reference captures. "Nothing since 2026-10-30 has run on Windows" ("Windows check", §5.3). | — |

Commits use `git -c user.email=meistervatrix@gmail.com commit …` and end with the environment's attribution line.
Pushes use `GIT_TERMINAL_PROMPT=0 GIT_ASKPASS=/bin/false SSH_ASKPASS= git push -q origin main`. "Everything on
GitHub stays private." Pull before starting work on either side.

The port-task work of 2026-11-06 to 2026-11-12 ran in cloud sessions, "touches only `../old-game/` … never the
engine", and "`Tools/Scripts/test.sh` was not run for it". The full runs on the PC after such merges are recorded
in "The full run after the merges".

---

## 2. What the engine provides

One assembly, `Engine/Statventure/` (namespaces `Statventure.*`). Its folders are:
- `Animation`, `Audio`, `Canvas`, `Core`, `Graphics`, `Imaging`, `Packs`, `Platform`, `Records`, `Render3D`,
  `Scene`, `Shaders`, `Storage`, `Text` and `UI`.

The rest of the repo around it:
- **Samples:** `Samples/Statventure.Sandbox`, the game's stand-in, and `Samples/Statventure.Game.Data`, the
  stand-in data library.
- **Editor:** `Tools/Editor` (`Statventure.Editor`).
- **Tests:** `Tests/Statventure.Tests/`, 3720 tests on `main`, organised as `Animation`, `Audio`, `Canvas`,
  `Core`, `Editor`, `Graphics`, `Packs`, `Platform`, `Records`, `Render3D`, `Scene`, `Storage`, `Text`, `Ui`,
  plus `Golden/`, `Reference/`, `Data/`, `Actual/` and `Infrastructure/`.
- **Committed packs:** `Assets/Packs/audio.pak` and `ui.pak` only.

The standing look-and-feel decisions every area serves are Engine-Plan.md, "User decisions (2026-10-16)":
- **Characters, enemies, items:** real-time 3D Blender models with cel shading and ink lines, with full
  animation sets.
- **Battles:** the fixed orthographic side view, on a 3D stage built in Blender.
- **Map:** 3D terrain seen from above.
- **Banners and cutscenes:** remade in Blender.
- **UI:** a custom responsive UI with CSS-like stylesheets compiled into packs; screens are built in C#.
- **Text:** the game's fonts (Lato, STIX Two Text), with FreeType and HarfBuzzSharp.
- **Music:** the live synthesizer ported, through OpenAL Soft.
- **Data:** classes stay `.json`; everything else goes into `.pak` files edited with an editor tool.
- **Saves:** start fresh.
- **Platforms:** Linux and Windows.

### 2.1 Platform, GPU, application (M0, M1; M12 additions)

What exists (Engine-Plan.md, Progress, "M1 is done"):
- **`Platform/Window`** (GLFW): logical units versus pixels, cursors, clipboard, fullscreen.
  - `WindowOptions.Visible`.
  - Since M11 step 3: `Window.PathsDropped` (OS file drops), `RequestWakeUp`, `PostEmptyEvent`.
- **`Platform/Input`:** an event queue plus state.
- **`Platform/PlatformBehaviour`** and **`Platform/SystemSettings`** (caret blink and reduced motion, as Chromium
  reads them).
- **`Graphics/GpuDevice`:** Vulkan 1.3 with dynamic rendering, sync2, descriptor indexing, buffer device address,
  a timeline semaphore and VMA.
- **Other GPU parts:** `GpuBuffer`, `GpuImage`, `Bindless` (16384 textures, 6 samplers), `ShaderLibrary`,
  `PipelineCache`, `CommandList`, `Swapchain` and `Frame`.
- **Shaders:** compiled to SPIR-V at build time by `Tools/ShaderCompiler` and embedded.
- **`Core/Application`:** the main loop, minimize handling and frame capture to PNG. Its parts:
  - `ApplicationOptions`: `GameFolder`, `DevelopmentFolder`, `LogName`, `Splash`, `GamePacksOnly`.
  - `Application.Data`, `Storage` and `Audio`.
  - `ReloadData()`, `ConfirmClose()` and `CrashExitCode`.
- **Logs and crashes (M12):** `Core/Log`, `LogFiles`, `CrashWindow`, `ProgramInfo` (name, version, commit) and
  `EngineInfo`.
- **Other:** `Core/NativeLibraries`; `Imaging/ImageFile` (stb load, PNG save); `Imaging/Turbulence`.

Display scale follows GLFW; the PC reports 1.65 (KDE Wayland, fractional).

Tests: a headless GPU fixture with golden images (`Golden/`, mismatches written to `Actual/`), plus a real window
and swapchain test.

### 2.2 2D canvas (M2, with later ports)

`Canvas/` (Engine-Plan.md, Progress, "M2 is done" and later entries). The canvas records in logical units at any
scale and draws through one instanced pipeline. It covers:
- **Boxes and fills:** rounded boxes with per-corner elliptical radii and per-side borders and dashes
  (`DrawBox(BorderSides)`); linear, radial and repeating gradients; outer and inset box shadows; images and
  coverage (glyphs and paths).
- **Clips, transforms, layers:** rounded clips, 2D transforms, colour filters, multiply and screen blending;
  opacity layers, the CSS filter chain, backdrop blur; `PushTile`/`PopTile`.
- **Paths:** `Path` (SVG path data) and `PathRasterizer`, its own rasterizer, which is a known difference from
  Skia's.
- **Glyphs and images:** `TextureAtlas`; `DrawGlyphs` with `TextLcd`; Chromium's mipmapped downscaling of images
  (`SkiaMipmap`, `ImageDecodeScale`, M12 step 4).

Later ports matched Blink and Skia rules exactly:
- box antialiasing under non-uniform transforms;
- paint offsets of transformed boxes;
- blurred and inset shadows;
- background layers.

Tests: the `css-*` scenes against Chromium (12 levels / 1 %), the paint fixtures, and a speed test (20,000
primitives record in 4 ms).

### 2.3 Text (M3, and M5's zoom work)

`Text/`:
- **Fonts:** `FreeType.cs`, the engine's own bindings to FreeType 2.14.3; `FontFace` (FreeType plus HarfBuzzSharp);
  `FontLibrary` (CSS font matching, synthetic bold and oblique, per-character fallback);
  `FontRenderSettings.System` (fontconfig, or Windows smoothing).
- **Layout:** `TextLayout` handles white-space modes, shaping with fractional advances, wrapping, ellipsis, line
  clamp and min/max-content.
- **Drawing as Chromium 152 does:** `Text/Skia` (Skia's analytic AA scan converter, its 4× LCD FIR filter, mask
  gamma and SkStroke fake bold) and `Text/Autohint` (skrifa 0.44's autohinter). Glyphs are rasterized at Blink's
  EffectiveFontSize. FreeType no longer draws any text.
- **Editing:** `WordBreaker` (ICU word breaks from Unicode 17 tables), `TextEditing`, `GlyphOutlines`.

The game's five font faces come from `old-game/objects/fonts/`, packed into `ui.pak`. M12 adds the engine's own
embedded Lato (`UI/EngineFonts.cs`).

Tests:
- `TextHintingReferenceTests`: glyph sheets for Lato and STIX at 1× and 1.65, exact against Chromium.
- `FreeTypeThreadingTests`.

### 2.4 UI: document, stylesheet, layout, painting (M4) and interaction (M5)

**Stylesheet and DOM** (`UI/Css`, `UI/Style`, `UI/Dom`):
- **CSS:** tokenizer, parser, selectors.
- **`Properties`:** every property the game's stylesheet uses. "The game's sheet has no unknown properties (a
  test checks this)."
- **`StyleEngine`:** the cascade, custom properties and var(), media queries, pseudo-elements, keyframes.
- **`UserAgentStyles`.**
- **`UiDocument`:** invalidation and the update.
- **`Markup`:** an HTML fragment parser, like innerHTML.
- **Incremental updates:** invalidation sets, shared styles, persistent boxes, a layout cache. Tested by
  `IncrementalUpdateTests` and `IncrementalWorkTests`.

**Layout** (`UI/Layout`):
- `BoxBuilder` and `LayoutEngine`: block flow, scroll containers, positioned boxes.
- `InlineLayout`, `FlexLayout` and `GridLayout`.
- `ReplacedContent`: images, canvases, inline SVG and form controls sized as Chromium sizes them.
- Layout works in Blink's 1/64 px LayoutUnits.

**Painting** (`UI/Paint/UiPainter*`):
- Appendix E paint order and stacking contexts.
- Backgrounds, borders, shadows and outlines.
- Text with decorations.
- Inline SVG (a vector subset; CSS transforms on SVG children; no full SVG styling, by decision).
- Form controls and Fluent scrollbars.

**Compositing like Chromium** (`UI/Paint/Compositing/`, `Compositing-Port.md`):
- property trees, paint chunks and the layerizer;
- per-chunk LCD or grayscale text;
- two paint passes;
- `<canvas>` and `<surface>` drawn as foreign layers.

**Interaction (M5):**
- **Input:** hit testing (`UiDocument.HitTest`); DOM events (`UiEvents`, `UiInput`) with Chromium's pointer and
  mouse order; focus, `:focus-visible` and Tab order; `inert`.
- **Scrolling:** Chromium's scrolling (`UiDocument.Scrolling`) with wheel, keyboard and scrollbars,
  `position: sticky`, and CSSOM geometry.
- **Text fields and forms:** caret, selection and undo; form validation with a game-styleable bubble.
- **Tooltips and pop-ups:** title tooltips as Chromium times them (`<sv-tooltip>`, game-styleable); the select
  popup `sv-select-popup`.
- **The window host:** `UI/UiHost`, `UiHostInput`. It handles keys, wheel notches (120 px Linux, 100 px Windows),
  cursor images with hotspots, the clipboard, reduced motion and the caret blink.
- **Zoom:** device scale plus Electron-style page zoom (`UiHost.PageZoom`, `SetZoom`; level ±0.5, factor 1.2^level).

**Script APIs (M5 #31, `Scripting.md`)**, ported so "the port can mirror the old JavaScript":
- `UiEventLoop`: timers with DOMTimer's clamp, tasks, microtasks, and C# `async`/`await` running as microtasks on
  the loop's SynchronizationContext.
- `SetTimeout`/`SetInterval`, frame callbacks, matchMedia, the resize event and `UiResizeObserver`, in Blink's
  frame order.
- Web Animations: `el.Animate([new Keyframe {…}], new AnimateOptions {…})`, giving a `UiAnimation` with
  `Finished`/`Ready` as `Task`s, plus `GetComputedStyle()`.
- `IUiFrameClient` for frame-driven clients such as the 3D scene.

**Hidden documents** (`Scripting.md`, "Hidden documents"; `UiDocument.Visibility.cs`, `UiTimerThrottling.cs`):
- A minimized window runs as Chromium runs a hidden page: no frames, timers throttled through Blink's wake-up and
  CPU-time pools, the audible exemption.
- `visibilitychange`, `pagehide` and `unload` on close; no `beforeunload`.

**Sequences' CSS animations** run on a sequence's own clock (`AnimationTimeline`, M10).

**Tests against Chromium** (more in §4):
- layout fixtures within 0.5 px;
- the 11 game screens box for box;
- `GamePaintTests` (24 levels, 3 %, inline SVG masked);
- `GameHitTests`;
- paint fixtures (12 levels, 0.2 %);
- input fixtures (`InputReferenceTests`, `InputPaintReferenceTests`);
- 13 script fixtures (`ScriptReferenceTests`) and the visibility scenarios (`VisibilityReferenceTests`);
- `CompositingReferenceTests` on all 22 captures, layer for layer;
- `AnimationReferenceTests`.

Benchmark: `Tools/Scripts/benchmark.sh` (`Tools/UiBenchmark`).

### 2.5 3D renderer and its look (M6; `Renderer-3D.md`)

`Render3D/`:
- **Models and cameras:** `Camera` (orthographic and perspective, reversed Z), `Mesh`, `GltfImport` (BRUTAL's
  glTF package parses; the engine decodes accessors, nodes, materials and skins), `ModelData`, `Model`, and
  `Palette` (material slots such as `metal.base` and `cloth`, filled from a palette at draw time).
- **Drawing:** `Renderer3D`, `RenderTarget3D` (MSAA 4× sRGB, D32), `Terrain`/`TerrainData`, `Particles`
  (`ParticleEffect`), `View3D`, and `UI.SurfaceViews` (`UiHost.Views`).
- **Importers:** `ModelImporter`, `TerrainImporter`.

The look, as decided by the user (Engine-Plan.md, "Decisions for M6"):
- **Cel shading:** two tones. The shadow tone mixes 42 % toward the ink `#130e0b` in sRGB, unless the palette
  gives the slot one. The band edge is at N·L 0.314 for a sun of strength 3, antialiased like Blender's
  (`SunLight`, `CelStyle`).
- **Ink lines:** screen space, an outline per model of 3.5 px and inner lines of 2 px, times the device scale.
  - Silhouettes, open edges, creases sharper than 120°, and mesh feature edges are inked.
  - No lines at colour boundaries; `ink = 0` per object turns a part's lines off.
- **Hatching:** in shadow, 6 px apart, 1 px wide, at 72°, 35 % toward the ink, anchored to the model's origin on
  screen; `hatch = 0` per material turns it off.
- **Glow:** from emission, the lizardman's halo (8 px blur, alpha × 2.2, a 2.5 px core), drawn behind the model.
- **Shadows:** everything casts; only scenery receives. 16 maps of 1024² in a 4096² atlas, softened by a 3° sun,
  no ink on shadows.
- **Terrain:** 16-bit heights and a cover image, covers blended softly over three cells, shaded water with two
  wave trains, chunks with level of detail.
- **Particles:** disc, ring, spark, puff and shard shapes in palette colours, optionally inked, glowing, with
  motion as a closed form of age.
- **GPU skinning:** linear blend, 4 influences, normals recomputed from the bent surface as Blender does.
- **Every 3D view is a `surface` element**, rendered only on change, with picking through `View3D.Pick` and
  `SurfaceViews.Pick`.

Tests (`Render3DReferenceTests` and others) compare against Blender 5.2 renders (EEVEE, Freestyle) in
`Golden/r3d-*.png`, under `ReferenceLimits.M6` or `ReferenceLimits.DetailedStage`. Renderer-3D.md, "Reference
harness", gives the limits.

Not measured: a 2016-class GPU, the target. The PC's RX 9070 takes 0.8–1.4 ms for a 1080p frame.

### 2.6 Animation (M7; `Animation.md`)

`Animation/`:
- **Clips:** `AnimationClip`, `AnimationCurve` and `AnimationTrack`, from glTF's sampled clips at 60 fps.
- **Rigs and poses:** `Skeleton`, `ClipBinding` (clips shared by bone names, rest-relative) and `PoseBuffer`,
  feeding M6's `ModelPose`.
- **Layers:** `AnimationStack`, `AnimationLayer` and `AnimationStrip` (Blender's NLA: Replace, Combine,
  cross-fades), plus `FadeLayer`.
- **`Animator`:** `Time`, `Rate`, `Advance`, `Seek`, the `Event` event and `Next(name)`. Clip events come from
  Blender pose markers and are awaitable.
- **State machines:** `StateMachineDefinition` with `StateMachineBuilder` (layers, bool/int/float/trigger
  parameters, exit times, any-state transitions, cross-fades in seconds) and `StateMachine` (`Entered`, `Left`,
  `StateChanged`).
- **Gear** (`Gear.cs`): items on `socket.<slot>` bones (`ActorItem`, `ItemAnimation`); garments skinned to the
  wearer (`ActorGarment`, `Garment`).
- **Not used:** no root motion, no shape keys, no simulation for secondary motion.

Since M10, figures are `Actor`s on scene nodes, shown by `SceneView`.

Tests compare against Blender's own evaluation (`evaluate_animations.py`): on frames within 0.0006 mm, plus NLA
stacks, transitions and gear.

The clip set per rig family (humanoid, quadruped, griffon, dragon, wisp, core) is decided in "Decisions for M7".
M7 animates test models only.

### 2.7 Audio and the synthesizer (M8; `Audio.md`)

**The API** is `Application.Audio`, an `AudioSystem`:
- **Music** is a `Soundtrack`:
  - `Play(id)` with music.js's fades, `Stop()`, and `PlayAt`/`StopAt`;
  - `Volume` (music.js's, before its limiter);
  - `Themes` (55 ids such as `"alder-vale"` and `"combat@alder-vale"`).
- **Effects** is a `SoundEffects`: `Register(id, patch)`, `Play(id, volume, pan, pitch)`, `PlayAt` and `Seed`.
  Its patches are `EffectPatch` records (tone and noise layers, filters, envelopes, seeded ranges, voice limits).
- **The rest of `AudioSystem`:**
  - `MasterVolume` and `EffectsVolume`;
  - `Time`, the audio clock;
  - `ScheduledSound` (`Cancel`, `State`);
  - `Audible`;
  - `Reload(MusicLibrary)`.
- **`Actor.Sounds`** (`ActorSounds`) maps a clip event name to an effect.

**Inside:**
- Chromium 152's Web Audio processing ported privately: AudioParam automation, oscillators and periodic waves,
  biquads, the convolver (RustFFT's AVX path), the compressor and the stereo panner.
- V8's `Math.exp` and glibc's `pow` ported.
- The composer and the theme data, from `audio.pak` (`MusicLibrary`).
- OpenAL Soft streaming at the device's rate, in 128-frame quanta about 20 ms ahead.
- It pauses while minimized and follows the default device.

**Tests:** every fixture against Chromium's OfflineAudioContext within float rounding; the 55 full themes in the
opt-in `test-themes.sh`; 12 effect fixtures. The music was "listened to and approved 2026-11-02".

### 2.8 Assets, packs, VFS, records, saves, settings (M9; `Assets.md`)

- **Packs** (`Packs/`; `PackFile`, `PackWriter`, `PackPath`, `PackFormat`, `DataFileException`):
  - a 64-byte `SVPK` header with the index at the end;
  - Brotli per entry (quality 11, window 22), deterministic;
  - SHA-256 per entry and over the index;
  - paths `a–z 0–9 - _ . /` only, with Windows' reserved names refused.
- **`Tools/Packer`:** `pack`, `list`, `verify`, `extract`, `dump` and `update`. `textconv.sh` is git's diff
  driver for `*.pak`.
- **Records** (`Records/`, `RecordSerializer`):
  - declared with `[Record("audio.theme", 2)]`, `[Field(n)]` and `[Subtype(tag, typeof(X))]`;
  - past versions as frozen nested copies with `Upgrade()`;
  - a binary form and a strict JSON form (`"$types"` first, camelCase names);
  - layout signatures and old-version fixtures committed (`Data/Records/Layouts`, `Fixtures`).
  - "that attribute is the registration, there is no list of kinds".
- **Loose records:** a `.json` whose top-level object starts with `"$types"` is the record at its path
  (`LooseRecord`, `LooseFolder`).
- **The VFS** (`VirtualFileSystem`, `Application.Data`):
  - Mounts, bottom to top: the area packs from `packs/` beside the executable (models, audio, ui, world, data,
    each if present), then `classes/`, then every `.pak` in the player's `mods/` in ordinal name order, then the
    development folder (`ApplicationOptions.DevelopmentFolder` or `$STATVENTURE_DEV_FOLDER`).
  - "A path resolves to the topmost mount that has it". Mods add or replace; they never delete.
  - Reading: `Data.Load<T>(path)`, `Exists`, `ReadAllBytes`, `List`.
  - `Application.ReloadData()` reloads; the `Data.Reloaded` listeners run after it.
- **The game folder** (`Storage/`, `Application.Storage`, a `GameStorage`):
  - **Where:** Linux `$XDG_DATA_HOME/stat-fick/` or `~/.local/share/stat-fick/`; Windows `%APPDATA%\Stat Fick\`.
  - **Saves:** named save slots (`SaveSlots`, `saves/<name>.dat` plus `.dat.bak`), written atomically by a
    background writer. A damaged save loads nothing and is kept aside (Q17).
  - **Settings:** `settings.json` with a version (`Settings.Get<T>`/`Set<T>`, `settings.broken.json`).
  - **Character files:** `CharacterFile.Export`/`Import<T>`, the save's JSON form in the Documents folder
    (`DocumentsFolder`, Chromium's XDG lookup).
  - **Browsing:** `FolderListing` and `FileBrowser`.
  - **"Open game folder":** `GameStorage.OpenInFileManager()`.
- **Importers** (`[Importer(name, endings)]` on an `IImporter`):
  - `.glb` → `models/…`
  - `.terrain.json` → `world/terrain/…`
  - `.css` → `ui/styles/…`, compiled to `ui.stylesheet` tokens
  - the music via `make-music-data.sh`, which reads the old JavaScript in Chromium and writes `audio.pak`
  - fonts via `make-ui-pack.sh`, which writes `ui.pak`

Tests: round trips, damaged files, old-version fixtures, `CommittedPackTests`, and Storage's 177 tests. The real
folders are protected by `Infrastructure/PerUserFolders.cs` (§4.6).

### 2.9 Scene and sequencer (M10; `Scene.md`)

**Nodes and attachments** (`Scene/`):
- `SceneNode`: parents and children with local TRS and a world matrix by Blender's parenting rule; `Find(path)`;
  `Hidden`.
- Attachments: `Actor`, `PlacedModel`, `ModelInstance` (one node per glTF node, each top-level object its own
  model; `Cameras`, `Sun`), `PlacedTerrain` ("the map is a scene"), `SceneCamera`, `SceneSun` and `SceneEmitter`.

**`SceneView`** (an `IUiFrameClient` holding a `View3D` in a `surface` element):
- `Rate`, the battle pace, which the water and free emitters follow too;
- `Play(sequence, bindings, document)`;
- `Animate(instance, stack, time)` for a stage's clips outside a sequence.

**Curves:** `FCurve`/`FCurveKey` are Blender's F-curves and easing (`scene.curve`, `scene.key`), in seconds.

**Stages from glTF:** cameras and one white sun (`ModelCamera`, `ModelLight`; `model` 2) via `make_models.py`'s
`export_stage` and `export_scene_markers`.

**Sequences:**
- **The record:** `Sequence` (`scene.sequence` at `world/cutscenes/<name>`, `Sequence.Load`). It holds roles,
  stages and eleven track kinds:
  - clip, transform, visibility, animation, palette, event
  - camera, light, particles
  - audio, UI
- **Built in C#:** `SequenceBuilder`.
- **Bindings:** `SequenceBindings` binds actors, nodes, models, UI hosts and palettes, plus `Effect(path,
  effect)` and `Audio(audio)`.
- **Playing** gives a `SequencePlayer`:
  - `Skip()`, which returns false when the sequence is unskippable, and `Finish()`;
  - `Seek`, `Rate`, `Pause`/`Resume`;
  - the reduced variant under the system's reduced motion;
  - cues as events (`Cue`, `Next(name)`, marked `Skipped`), `Blocking`, `Finished`.
- **Audio cues** are scheduled on the audio clock. **UI tracks** set text, classes and attributes, and their CSS
  animations run on the sequence's clock.
- **Particle effects** are records (`particle.effect` at `models/effects/<name>`).

**The proof**, Ji'ruan's intro, is test data only (`Tests/.../Scene/JiruanIntro.cs`, `Data/Models/jiruan.glb`,
`jiruan-acting.glb`, `Data/Cutscenes/`). Its figures are the engine's stand-in test figure.
`Tools/Scripts/play-cutscene.sh jiruan` plays it.

**Tests:**
- curves, nodes and stages against Blender evaluations;
- the proof's renders under `ReferenceLimits.DetailedStage`;
- the card against Chromium captures of the old game's card (12 levels, 0.2 %) at six times.

### 2.10 Editor (M11; `Editor.md`)

**The program:** `Tools/Editor` (`Statventure.Editor`), shipped as its own download `statventure-editor` /
`Statventure Editor.exe`, in the old game's dark colours and Lato.
- It opens one pack at a time over the game's packs: a mod in `mods/`, or, in the dev setup (`--repo`), the
  game's packs in `engine/Assets/Packs`.
- Panes: the entry list, a generic form made from each kind's definition (undo and redo per entry; a JSON tab)
  and a live preview (effects, themes, particles, models and stages, cutscenes with stand-ins).
- It checks as you edit and refuses to save while any entry has a problem; it handles references, Import (and
  drops) and New mod.

**Engine API added for it:**
- `RecordShape` and `ValueShape`.
- Marks: `[Kind]`, `[Help]`, `[Unit]`, `[Limits]`, `[Choices]`, `[Colour]`, `[Bulk]`, `[References]` and
  `[Check]`.
- `RecordDraft`; `RecordChecks.Check`, `CheckContext` and `RecordProblem`/`RecordPath`.
- `PackWriter.AddFrom`/`ImportFile`, `Mount.Custom`, `packer update`.
- `ModInfo` (`mod/info`).

**Tests:**
- Behaviour through `EditorHarness` and the session's data.
- 24 editor screens (19 from M11, 5 from M12) against Chromium at 1 and 1.65, within 12 levels and 0.2 %.
- `ModFromScratchTests`: a mod made from nothing loads in the game's loaders.

### 2.11 Packaging (M12; `Packaging.md`)

**The downloads.** Four archives per release, `stat-fick-<v>-linux.tar.gz`, `-windows.zip` and
`statventure-editor-<v>-…`, each holding one folder named with the version. The executables:
- are self-contained, single-file and not trimmed, with symbols embedded and invariant globalization;
- are GUI programs on Windows, which also gets the three Visual C++ runtime DLLs;
- have their native libraries beside them, and in the game's download `Statventure.Game.Data.dll`, `packs/`,
  `NOTICE.txt` and `licenses/`.

**The version** has four numbers, `2026.11.12.1`; a build that isn't a release says `dev`.

**Logs** are `logs/<program>-<date>_<time>.log` in the per-user folder, the last 10 kept. A crash writes a crash
file and shows a crash window drawn by the engine, then exits with code 1.

**Both programs** start with the BRUTAL splash (2 s, skippable after 1 s) and have an About and credits view
(`UI/Splash.cs`, `UI/Credits.cs`).

**The editor and the game.** The editor finds the game ("Where is Stat Fick?", or `--game`, remembered in
`editor/game.json`) and loads the game's data library from it. The library's version must equal the editor's.

**Scripts** in `Tools/Scripts/`:
- `publish.sh` / `publish.ps1`
- `check-packages.sh`, with `--self-test`
- `end-to-end.sh`: 157 expectations; not in `test.sh`
- `release.sh`: a draft GitHub release, made only on the user's word

### 2.12 The sandbox (the game's stand-in)

`Samples/Statventure.Sandbox`:
- **What it is:** a UI demo page, titled "Statventure Sandbox", with an About button.
- **Its modes and options:** `--capture`, `--log-visibility` and the cutscene mode (`Cutscene.cs`); M12 added
  `--about`, `--crash` and `--crash-thread`.
- **How it ships:** `build.sh`/`build.ps1` publish it into `$STATVENTURE_GAME` with `packs/` beside it, "the
  game's stand-in until the port" (S9-A). It is packaged as `stat-fick` and `Stat Fick.exe`.

---

## 3. What is explicitly left for the port

Grouped by source, as the docs say it. Quotes are verbatim, trimmed to the clause that matters.

### 3.0 Standing rules the port inherits

- **Order of work** (`Notes.md`, head):
  1. Build the engine first, fully tested and functional so it "handles the game perfectly".
  2. Then port the game.
  3. Check over every system and function of the game.
- **Working rules** (`Notes.md`): "Remake every character, enemy and item in Blender, more detailed and more
  consistent, in the same art style. Scenery is remade in Blender too. Give the shipped game a proper game-like
  folder structure."
- **Engine-Plan.md opening:** "The engine is finished, fully tested, before the game is ported onto it."
- **"Next, on the user's go: the port of the game"** (Engine-Plan.md, "Current task"). It points to
  `Docs/Port-Tasks/` ("read its README first") and to "Open questions for later milestones", "At port time".
- **The open question for the user** ("Current task"): "whether the approved port tasks 10, 12, 13 and 14 are
  built while porting or after the faithful port (left open since 2026-11-05)".
- **Task 14 is different.** Dungeons is built "during the port itself (DG1)".

### 3.1 Engine-Plan.md, "Open questions for later milestones", "At port time"

**The requirement:**

> a requirement, not a question (the user, 2026-11-05, approving Ji'ruan's cutscene stage): the boss and enemy
> models are to be as detailed and high-quality as the cutscene scenes; the stand-in test figure is not the bar

**The questions to ask:**
- which sounds the game gets ("the port must add proper sound effects …, the rune slotting sound among them");
- UI symbols outside Lato and STIX (⚙ ✦ ☾ ↓ ＋ ⚔ ♟ …);
- "how the inline SVG art and its transforms map to the Blender remakes";
- "the grabbing cursor's design";
- "the level of detail of the item models".

**"First read `Docs/Port-Tasks/`":**
- Tasks 1–6 are done in the old game. The faithful port "brings them over: don't build them again, only remake
  their placeholder art".
- Tasks 7, 8, 9 and 11 are also done in the old game ("How to continue" paragraphs).
- Tasks 10, 12 and 13 are designed for the port to build; task 14 is built during the port.

**Recapturing the game screens** ("Also to ask (recorded 2026-11-06, not decided)"):

> when to capture the game screens again and bring the tests' copy of the stylesheet up to the old game's:
> before the port starts, or screen by screen as the port reaches each.

The facts behind it:
- The 22 captured screens, `Golden/game-ui.json.gz`, `game-ui@1.65.json.gz`, `game-*.png` and
  `Reference/scaled/game-layers@1.json`/`@1.65.json` all came from the first commit `f45ca7a`.
- So did the tests' copy `Tests/Statventure.Tests/Data/Game/styles.css` (3915 lines).
- The old game's `styles.css` has since grown to 4080 lines (3 removed, 168 added), and its `index.html` and
  scripts changed too.
- So "the engine's game-screen tests check the old looks, which is intended for now".
- The scripts to recapture: `make-game-references.sh`, `make-scaled-game-references.sh` and
  `make-layer-references.sh`.

**G2, kept from M11** "to ask where mods and the game's mod list are asked": what the game does with a mod that
has parts it can't read. Today it "loads the mod and stops with an error naming the mod and the part only when
that part is used". The alternatives are a check at start, or a warning in the log ("recommended then: as
today").

**G3, closed by M12-9:** "a data library of their own, `Statventure.Game.Data`, with their rules, which the game
links and the editor loads from the game's folder (not the editor loading the game's program file; not the kinds
in the engine)."

**From M12:**
- "**an in-game update check** once the game is public somewhere";
- "**new icons for both programs** … with the port's art pass … the icons need a Windows build or a resource step
  then";
- "**the BRUTAL logo before the game starts and BRUTAL's copyright with the other credits in the game's own
  screens** (Q11: M12 gives both programs a splash and a credits view now, which the port's start sequence and
  credits screen take over)".

**The general enemy rework** (the user, 2026-11-11): "all enemies are revamped during the port, because their
classes have changed (task 10) and an upcoming task will change the spell system as well (task 12, Spell Rework)".
This covers every enemy, with both reworks taken into account.

**From the port tasks.** The detail belongs to the Port-Tasks survey; these are listed in "At port time":
- the Spell Rework built from its drafts (SP1, SP69);
- the Tier 10 spell animations reworked (SP47; `../old-game/scripts/tier10-cinematics.js`);
- every enemy using the old classes fully reworked (C2, C11);
- C32's three duties: the draft classes added; every class inspected while porting; every class feature tested
  after the port;
- task 13's 79 classes;
- task 14 built during the port (DG1);
- the hooded man offers no red class at Level 99 or below (NC7).

### 3.2 "What M7 leaves for the port" (Engine-Plan.md, "How to continue")

- "figures are `Actor`s on the nodes of a scene … their clips come from the .glb files (exported with
  `make_models.py`'s `ANIMATED` settings, markers by `export_markers`), shared by bone names within a rig family".
- "a rig's state machine is built with `StateMachineBuilder` (pack form in M9, editor in M11)".
- "gear is `ActorItem`s on `socket.<slot>` bones and `ActorGarment`s; the battle pace is the scene's `Rate`".

From "Decisions for M7":
- **Boss clips:** bosses get "their rig's set plus phase change, transform, an intro entrance and their specials
  (asked per boss at port time)".
- **Model approval (port time):** "the user approves the first model of each rig before the rest of that rig is
  made: first the first humanoid (with gear and its full clip set, shown as renders and a playing view), then
  the first quadruped, griffon, dragon, wisp and core. M7 itself animates test models only."

From `Renderer-3D.md`, "Method": "Test scene values (palette entries, light, line widths) are test data, not the
game's look; the game's own palette, light setups, line widths and hatch pattern are asked of the user when they
come up."

### 3.3 "What M8 leaves for the port" (and `Audio.md`)

- "The game ports `desiredTheme` (region theme, else `alder-vale`; in battle `combat@<region>` if it exists, else
  `combat`; a boss's theme, else `combat`) and saves the volumes (M9)."
- "the port must give the game proper sound effects (user decision 2026-11-03 …; the first asked for: slotting a
  rune), which ones is asked at port time. The old game's one slider is the music volume."
- `Audio.md`, "Decisions", "Theme API": "Since port task 8 … music.js crossfades between two region themes … (4
  seconds; W14), and in the empty regions the last charted region's theme plays on (W28). `Play` here switches as
  music.js did before task 8; the game port adds the crossfade."
- `Audio.md`, "Sound effects (step 8)": "Which sounds the game gets is decided at port time." The old game has no
  effects at all ("music only"), so they are new work.
- `test-themes.sh` runs whenever the synthesizer changes; `play-theme.sh` is for listening.

### 3.4 "What M9 leaves for the port" (and `Assets.md`)

Engine-Plan.md:

> Left to the port: the old game's tables and classes (renamed under the path rule, Q31), the port's own
> stylesheet compiled into `ui.pak`, `models.pak` and `world.pak` once there is content to pack (none committed,
> Q1), the packer's "update" command once a pack holds imported and editor-made entries, the save record (what a
> slot holds), the slot screen and the file picker as in-game UI, and the settings list (the volumes, the old
> game's display preferences and zoom).

`Assets.md` adds:

**Game tables and classes:**
- "Scope": "The old game's tables (enemies, spells, artifacts, consumables, key items, races) are converted at port
  time, when their C# records exist, checked against the old game in Chromium as the music is."
- Pack layout: "`data.pak` (the game's tables, once the port defines their records)".
- **Q27:** "the class record and loader come at port time; M9 only mounts the shipped `classes/` folder, so a mod
  or the development folder can override a class. … Its files get names under the path rule when the port makes it
  (Q31)."
  - Q31: the 129 class files (`Black Knight.json`) are renamed (`black-knight.json`); the old game's own files stay.
  - Mounted under their old names, "they would fail the mount (Q29)".
- What exists, the old data measured: `classes/` 129 files (263 KB), `enemies/` 169 files, and in `objects/`:
  `spells.json` (279 KB), `artifacts.json`, `races.json`, `consumables.json` and `key-items.json`.
- The old game "also caches edited class, spell, item and race files in localStorage (its in-game editing …)".

**Saves, settings and character files:**
- "Which settings exist is decided at port time." The old game has `battlePace`, `combatAnimations`, `battleUi`
  and `battleSpeed` in `settings.js`, and the music volume. "its dark mode is part of the character save".
- "Which record the save is belongs to the port; M9 tests with test records." The slot screen is the port's.
- A damaged save: the game asks "use the backup, start new" (Q17).
- Saves are written "whenever the game asks, as the old game's `saveCharacter` after every change" (Q16).
- "the picker screen is made at port time with the other screens (Q19)". `FileBrowser` is the model it builds on.
- Old saves are not imported (2026-10-16).

**Content still to come:**
- `models.pak`, `world.pak` and `data.pak` "appear when the port makes their content".
- "Map data comes with the port."
- "The game's own sheet and images join that folder at port time".

**What the game binds:**
- `UiDocument.Files` ("the game sets it to `Application.Data`").
- `UiHost.LoadImage` and `LoadText`.
- `ApplicationOptions.DevelopmentFolder`, "which the game's own command line fills at port time".
- `Application.ReloadData()`: "the port binds it; the engine binds no key and watches no file".
- `make-music-data.sh` and `make-ui-pack.sh` switch to `packer update` "once an editor-made entry is committed"
  (P30).

**Possibly inconsistent: the class loader.** The Milestones entry for M9 lists "the JSON class loader". `Assets.md`
Q27 and "What M9 leaves for the port" say the class record and loader come at port time. What exists is the
generic loose-record rule, which needs `"$types"`; the old class files don't have it.

### 3.5 "What M10 leaves for the port" (and `Scene.md`)

Engine-Plan.md: the scene API (§2.9), then:

> The bar for the port's cutscenes …: the 11 other boss intros, the 7 phase cinematics, the 12 spell vignettes and
> the banners are made at port time to Ji'ruan's bar: a detailed Blender stage in the cel, ink and hatch style
> composed like its old SVG …, acting clips authored in Blender on the figures' rigs (a clip library per cutscene,
> shared by bone names), particles, the card, sound; the stage and the clips approved by the user through Blender
> renders first, before their tests; and the user's requirement (2026-11-05): **the boss and enemy models are as
> detailed and high-quality as the cutscene scenes**

`Scene.md`, "How it works", step 7, "The bar for the port's cutscenes" gives the same bar in detail:
- *Figures:* as detailed as the scenes.
- *Stage:* every element of the old scene remade; light sources as layered shells with "halo as geometry";
  atmosphere as particles; "models opaque (Q23)".
- *Figures act:* clips per rig played as NLA strips over the state machine, each cutscene with its own clip library.
- *Approval:* Blender renders at sampled times, "iterated until approved, before the tests are written".
- *Files:* "at port time the game's cutscenes are game content in the packs (M10-10), where their Blender sources
  live is the port's question."

**The old game's cutscenes to remake** (`Scene.md`, "The old game's cutscenes (facts)"; sources
`boss-intros.js`, `combat.js`, `tier10-cinematics.js`, `index.html`, `styles.css`):
- **12 boss intros**, 7.0–8.4 s: Ji'ruan (done as the proof), Juila, Tankina, Saffron, Gaian, Anatemis, Ur-Mat,
  Ool, Ornn, the Deep Sea King, Zik Lun and Nygheir.
- **7 phase cinematics**, 7.2–7.8 s: `tankina-phase`, `anatemis-golden`, `anatemis-zenith`, `ur-mat-lucid`,
  `ool-phylactery`, `ornn-phase` and `nygheir-phase`.
- **Special cinematics:** `boss-special-cinematic` (storm-talos, false-dreamer, forge-god; 3.4 s) and Anatemis's
  star cinematic (4.8 s). Today neither is reached: "in today's enemy files every phase with a cutscene names a
  phase cinematic".
- **12 spell vignettes**, 1.9 s: non-blocking, `role=status`, no skip. Task 12 also reworks the Tier 10 animations.
- **Banners** (`scene-banners.js`): "animated interior scenes, not cutscenes; remade in Blender at port time with
  the rest."
- **How the old game plays them:**
  - once at the encounter's start, "never when a saved battle resumes";
  - the AI held until 100 ms after `bossIntroUntil`;
  - skipped with `#boss-intro-skip` or Escape;
  - none with combat animations off;
  - a still card for 2.4 s under reduced motion;
  - "cutscenes have no sound of their own".

**What the game does, not the engine:**
- **M10-3:** "The game binds the skip button and key and decides when its combat-animations setting suppresses a
  sequence".
- **M10-9:** the card's "look and motion stay in the game's stylesheet".
- **Q15, Q20, Q21:** roles are bound at play time, one sequence for any boss of a kind; `Blocking` is a flag "for
  the game, which holds its AI"; the end state stays "until the game changes it".
- **S4-3:** "the game's palettes come from the game".
- **S5-2, S5-3:** the game loads effects with `ParticleEffect.Load` and starts its own emitters with
  `node.Attach(new SceneEmitter(effect) { … })`.
- **Phase 2:** the proof's event cues `intro-started`, `title-card` and `intro-ended` are for the game (the AI
  hold's end).

**M10-11 and Q30:** the proof's figure "stands in for the boss and the imps (a placeholder until the real models)".
Its files are test data that "never ships".

### 3.6 "What M11 leaves for the port" (and `Editor.md`)

Engine-Plan.md: the editor exists (§2.10). Then:

> **What the port's own record kinds (items, enemies, spells, shops) must do to be editable with no editor code
> (M11-2):** be records as M9's are (`[Record]`, `[Field]`, `[Subtype]`); mark the kind with `[Kind(display name,
> folder) { Rule }]` (the folder and name rule New entry and references use; without a folder New takes a whole
> path) and their fields with `[Help]`, `[Unit]`, `[Limits]` (only limits the game's own check already enforces:
> a new limit is a question for the user, G4), `[Choices]`, `[Colour]`, `[Bulk]` and `[References(typeof(kind)) {
> Folder, Rule, Keys, When, Is }]`; and give their rules as a `[Check] static void Name(T value, CheckContext
> check)` reporting each problem at its path (with the data, for checks across entries), which their own loaders
> run too, so the game refuses what the editor refuses (G4, G5).

> Left for later: special views (a cutscene timeline, a map view, sound or particle editors; M11-2), previews of
> state machines, UI files and terrain (M11-13), a stop for sound effects (Small tasks), a mod depending on
> another mod (P18's known limit), the repo's script-made packs switching to `packer update` once an editor-made
> entry is committed into them (P30).

`Editor.md` adds:
- **M11-9:** "the game can reuse the browser later for Load Character". `FileBrowser` is in `Statventure.Storage`.
- **M11-15 and G1:** `mod/info` (kind `mod.info`: name, author, version, description) — "the game ignores it for
  now (a mod list can use it later); mods without it still load".
- **"What exists", "The preview's parts":** "The record names roles, not what plays them: the actors' figures and
  clip libraries (S7-5), the UI hosts' markup, palettes and node roles are the game's. A preview must bind
  stand-ins or leave tracks out."
- **G4 (the user):** "the game enforces declared limits too". **G5 (the user):** play-time checks also run at load.
  Both are wired for music (`MusicLibrary.Load`) and cutscenes (`Sequence.Load`); the port's kinds bring their
  own `[Check]`.
- **"Not questions, stated":** "the engine's window learns file drops and a refusable close (step 3), but the game
  doesn't listen".
- **P6:** "not `classes/`, which aren't records until the port".
- **P34 (c), not chosen:** "Default stand-ins named in the sequence record (a change to the game's format: would be
  a question for the user)".

### 3.7 M12 (Packaging.md) and the stand-in data library

**The pipeline is proven with a stand-in.** Packaging.md's intro: "M12 builds and proves all of it with the sandbox
standing in for the game (M12-12); the port replaces the stand-in."

**The stand-in data library**, `Samples/Statventure.Game.Data` (M12 step 6, Q19), has one file of code,
`StandInItem.cs`:

```csharp
/// The stand-in data library's one test kind (M12 step 6, Q19): an entry of a kind that isn't the engine's, so the
/// editor and the packer are shown taking a game's kinds from its data library. Not a game rule: the port replaces
/// the library's contents with the game's own kinds (items, enemies, spells, shops).
[Kind("Stand-in item", "stand-in/items")]
[Help("A test kind of the stand-in data library: the port replaces it with the game's own kinds.")]
[Record("stand-in.item", 1)]
public sealed record StandInItem
{
    [Field(1)] [Help("The name players see.")] public string Title { get; init; } = "";
    [Field(2)] [Unit("gold")] [Limits(0, double.PositiveInfinity)] public int Price { get; init; }
    [Field(3)] [Choices("Common", "Rare", "Exotic")] public string Rarity { get; init; } = "Common";
}
```

**What the real `Statventure.Game.Data` has to do and keep,** gathered from the docs (no doc lists it in one place):
- **Shipped as a file of its own.** It is `Statventure.Game.Data.dll` beside the game's executable, not inside the
  single file. The sandbox's target `KeepTheDataLibraryBeside` sets `ExcludeFromSingleFile`. It is "one file in
  each download" (M12-9), clarified as the game's download only: "the editor's download carries no copy".
- **The editor recognises a game folder by it.** A folder counts as a game when it holds `packs/` and
  `Statventure.Game.Data.dll`; "its program isn't looked at" (Packaging.md, step 6).
- **Its version must match the editor's.** The library's informational version, read from its metadata, must
  equal the editor's exactly; `dev` matches `dev` (Q18). Another release's library "can't even load against this
  engine".
- **How it is loaded.** It goes into the editor's own `AssemblyLoadContext`, once per run. Its kinds (found by
  `[Record]`) and its importers join the engine's (`GameData.Types`, `Importers`).
- **The packer references its project** by name, "which the port's library keeps". So `pack`, `update`, `extract`,
  `dump` and git's diff driver know the game's kinds with no option.
- **One constant to change.** `GameLibrary.RepoProject` (the dev setup's path to the library's project) is "one
  constant the port changes when it makes its own project".
- **Contents.** "The port replaces its contents" with the game's kinds (items, enemies, spells, shops). They carry
  their own marks and `[Check]` methods, which the game's loaders also run (§3.6).
- **No importer yet.** "the stand-in has no importer, so nothing can differ until the port's library has one"
  (accounted mutations).

**What else in the packages is the stand-in's:**
- **The game's program.** `Statventure.Sandbox` is renamed at publish to `stat-fick` / `Stat Fick.exe`. "The
  stand-in keeps its title 'Statventure Sandbox' and the release's text says it stands in for the game" (Q13).
- **`Data/`** in the game's archive is "the sandbox's own files (fonts, the proof cutscene); stand-in only".
- **`classes/`** "comes with the port (M9 Q27); the stand-in has none". From step 3: "When the port adds it,
  `Publish.cs` copies it beside the game's program and the game's two listings gain its paths (with a `cover` line
  in `licenses.txt` for the class files)."
- **Options the end-to-end check relies on.** `end-to-end.sh` uses the stand-in's `--capture`, `--capture-splash`,
  `--about`, `--crash` and `--crash-thread` ("a crash trigger of the stand-in's own"), its `[Data] mod …` log
  lines, and its "parchment" page. The docs don't say whether the ported game must offer these.
- **Logs.** "the stand-in logs as the game", under `LogName` `stat-fick`.
- **Splash and credits.** "the ported game shows them again in its own screens" (Q11).
- **Later items:** icons "at port time" with a Windows build or resource step (M12-5, Q15); signing "looked at
  again when the game goes public" (Q14); an installer or AppImage "can come later as a step of its own" (M12-4).

### 3.8 M5 decisions that give the port UI work (Engine-Plan.md, "Decisions for M5"; Progress)

- **Tooltips and select lists:** "`title` tooltips and the open list of a `<select>` are game-styled …; the
  game's stylesheet styles them at port time."
- **No menu bar and no OS dialogs:** "Save/Load Character, Open game folder, Quit, Reload, Zoom, Fullscreen, About
  and file picking all become in-game UI."
- **Form validation:** a game-styled bubble (a styleable element).
- **The grabbing cursor:** "a game-styled cursor image". Its design is asked at port time.
- **Symbols:** the M4 Progress entry, "**Open question for the port:** symbols such as ⚙ ✦ ☾ ↓ ＋ ⚔ ♟ in UI text
  aren't in Lato or STIX. The decision so far covers ♜ ♡ ◈ ▣ ◌ ◫ ◆ (vector icons). Ask about the rest when
  porting." `Notes.md` (2026-10-16): those seven, set in "Georgia, serif" by the old game, "become vector icons
  drawn by the engine".
- **Inline SVG art:** masked in `GamePaintTests` "because Blender renders replace it".
- **Visibility events:** "**Left for the port, not M5:** the game listens for `visibilitychange` and `pagehide`
  (map.js pauses travel)". The engine side was done 2026-11-03; the game side is the port's.
- **The event loop and Web Animations** were chosen "so the port can mirror the old JavaScript closely".

### 3.9 Smaller port-time notes found elsewhere

- **`Notes.md`, "Game inconsistency":** "the battle sprite (`sprite-equipment.js` `MASTERWORK_SHIELDS.transformer`)
  still draws the Transformer as the old round copper shield, while the forged item and its icon are a kite
  shield. The user was told; nothing has been changed."
- **A painter limit the port's long lists will meet** ("Small tasks"): "The editor's list makes only the rows near
  its view now (P37), so nothing waits on it; a long list or table in the game (the port's shops, inventories)
  would." See §5.1.
- **Placeholder art the port remakes** is listed in `Docs/Port-Tasks/README.md`, outside this survey's scope.

---

## 4. The reference method the port inherits

### 4.1 The rule

Engine-Plan.md, "How to continue", "Method":

> Everything the old game shows behaves as Chromium 152 (the game's Electron) behaves:
> 1. Get ground truth first: capture references with the harnesses (`Tools/Scripts/make-*-references.sh`; Electron
>    apps that `Tools/Scripts/chromium.sh` runs in Chromium 152 (Electron 44.5.0 from npm, the old game's):
>    `Tools/InputReference` (real input), `Tools/PaintReference` (screenshots, any scale), `Tools/GameReference` (the
>    old game's screens), `Tools/ScriptReference` (script scenarios), the old game's test client
>    `../old-game/test-client`, cc traces) and read the Chromium source (`~/.cache/stat-fick-build/reference-src/` …).
>    Captures run on Linux (the PC, or the cloud under Xvfb); the 1.65 ones need X11.
> 2. Port the rule from the source, don't fit numbers until a test passes. When a result differs, find the rule that
>    explains it (dump the engine's intermediate data and compare). Check the capture setup before blaming the engine
> 3. Write a test against the reference; `Tools/Scripts/test.sh` must pass in full before a step is done.

"For the 3D areas (M6 on) the reference is Blender instead: the same scenes rendered by Blender 5.2, its rule
ported and the engine tested against its renders."

**The workflow for a fixture** (`Scripting.md`, "Tests"; Editor.md, "Method"):
1. The C# side declares the fixture.
2. One `test.sh` run writes the fixture list into `Tests/Statventure.Tests/Reference/*-fixtures.json`.
3. A `make-*-references.sh` script captures Chromium's result into `Reference/*-results.json` or `Golden/*.png`.
4. The tests compare.

"Capture at least twice and compare." To capture one new fixture alone, filter the fixture list into a scratch
file.

### 4.2 Harnesses and capture scripts (`engine/Tools/`, `Tools/Scripts/`)

| Harness | What it captures | Script |
| --- | --- | --- |
| `Tools/PaintReference` | Screenshots of fixtures (paint, the editor's screens) at 1, 1.65 and page zoom | `make-paint-references.sh`, `make-scaled-paint-references.sh SCALE [ZOOM] [list]`, `make-editor-references.sh` |
| `Tools/GameReference` (`main.cjs`, `layers.cjs`, `cutscene-card.js`) | The live old game in a real X11 window at a forced device scale, with cc layer trees | `make-scaled-game-references.sh`, `make-layer-references.sh`, `make-cutscene-references.sh` |
| `old-game/test-client` (`headless-main.cjs`, `smoke.cjs`, 52 test files in `tests/`) | The old game's own headless test runner (`chromium.sh test-client --suite=smoke\|combat\|enemies\|lifecycle\|all`). `make-game-references.sh` drives the game through its screens with it (`--script=Reference/game-capture.js --call=uiCapture`) and copies `styles.css`. `make-css-references.sh` renders the canvas scenes in it. | as named |
| `Tools/InputReference` | Real trusted input (`sendInputEvent`), event logs, states, snapshots | `make-input-references.sh [1.65]` |
| `Tools/ScriptReference` | Event loop and Web Animations scenarios | `make-script-references.sh` |
| `Tools/VisibilityReference` | Hidden and closed pages over minutes (about six minutes) | `make-visibility-references.sh` |
| `Tools/DocumentsReference` | Electron's `app.getPath("documents")` in 46 environments | `make-documents-reference.sh` |
| `Tools/AudioReference`, `Tools/NativeReference` | OfflineAudioContext renders; native math | `make-audio-references.sh`, `make-music-references.sh`, `make-theme-references.sh`/`test-themes.sh`, `make-native-references.sh`, `make-v8-math-reference.sh`, `make-music-data.sh` |
| `Tools/BlenderReference` (`make_models.py`, `render_scenes.py`, `evaluate_animations.py`, `evaluate_curves.py`, `compose_glow.py`, `make_terrain.py`) | Blender 5.2 EEVEE and Freestyle renders, pose, NLA and curve evaluations | `make-blender-references.sh [scene …]` |
| Layout fixtures | Element boxes measured in Chromium | `make-layout-references.sh`, `make-scaled-layout-references.sh` |
| Animation fixtures | CSS animations sampled by `getComputedStyle` | `make-animation-references.sh` |

Capture gotchas, from Engine-Plan.md's Progress under Zoom and `Notes.md`:
- Electron's `capturePage` resamples unless the window is whole device pixels.
- Offscreen X11 windows need `--disable-frame-rate-limit`.
- The shop's random stock is seeded (mulberry32, `0x5eed`) around `Shop.open` in `game-capture.js`.
- Timers with a delay are real time in Chromium, so script fixtures use 0 ms timers only.
- Check which font Chromium actually used before comparing.

### 4.3 What is compared, and to what limit

| Check | Reference | Limit |
| --- | --- | --- |
| Layout fixtures (`LayoutFixtures.cs`, 36 pages) | Chromium element boxes and text fragments | within 0.5 px |
| Game screens, layout (`make-game-references.sh`) | 11 states: the sheet, seven modals, the shop, the map, combat | box for box |
| Game screens, pixels (`GamePaintTests`) | `Golden/game-*.png` at 1× and 1.65 (22) | 24 levels, at most 3 % of pixels. Inline SVG art masked, because "Blender renders replace it". |
| Hit testing (`GameHitTests`) | `elementFromPoint` on a 10 px grid of the 11 screens | exact |
| Compositing (`CompositingReferenceTests`) | cc layer lists from traces, 22 screens | layer for layer: owner, rect ±1.5 px, contents opaque, LCD reason |
| Paint fixtures (`PaintReferenceTests`, `ScaledPaintReferenceTests` …) | PaintReference screenshots | **12 levels, 0.2 % of pixels** (`maxDifferentRatio: 0.002`). Exceptions: slanted boxes and `shadow-rotate` 12 levels / 1 %; `shadow-scale-y`@1 0.25 %; SVG path edges at tolerance 60. |
| Canvas scenes (`css-*`) | test-client renders | 12 levels, 1 % |
| Text (`TextHintingReferenceTests`) | glyph sheets at 1 and 1.65 | tolerance 1, exact outlines |
| Input, scripts, visibility | event and callback logs | exact by frame. Throttled wake-ups are compared within 9 ms of Chromium's (its 8 ms leeway). |
| Editor screens (24) | PaintReference at 1 and 1.65 | 12 levels, 0.2 %, the preview's `surface` left out |
| Cutscene card (`JiruanCardTests`) | `make-cutscene-references.sh`, the old game's card at six times | 12 levels, 0.2 % |
| 3D (`Render3DReferenceTests`) | Blender EEVEE and Freestyle | `ReferenceLimits.M6`: interiors within 2 levels, edges within 0.7 of the local contrast, ink measures (missed lines ≤ 2 %, extra ≤ 3 % …). `ReferenceLimits.DetailedStage` for detailed stages: extra lines 12.5 %, unlike ink 8.5 %, total ink 10.5 %, coverage 0.175, stray pixels 1 in 800. |
| Animation | Blender's evaluation | on frames within about 0.0006 mm; between frames within a measured interpolation allowance |
| F-curves | `evaluate_curves.py` | within 16 float32 roundings |
| Audio fixtures and themes | Chromium OfflineAudioContext | within float rounding; shuffled-sum rules (16 orders) for summed voices; the 55 themes in the opt-in `test-themes.sh` |
| Records, packs, saves | committed layouts and old-version fixtures | exact |

"A number the user chose, or a tolerance, isn't yours to change to make a test pass; ask." Every widened limit
in the docs was a user decision: S7-13, `shadow-scale-y`, the 0.1 px edge tolerance for transformed boxes, and
SVG paths at 60.

### 4.4 Fonts and the platform the tests pretend to be

- **`Tests/Statventure.Tests/Infrastructure/ReferencePlatform.cs`** is a `[ModuleInitializer]` that sets
  `PlatformBehaviour.Windows = false` and `FontRenderSettings.System = { Antialias = SubpixelRgb, Hinting =
  Slight, LcdFilter = 1 }`. In its own words: "The tests run as on the PC the Chromium references were captured
  on, whatever machine runs them".
- **`Infrastructure/SystemFonts.cs`** takes system fonts from `Tests/Statventure.Tests/Data/SystemFonts`, where
  fontconfig's answers are recorded. Symbols missing from Lato and STIX come from system fonts in Chromium, and
  the tests emulate that "with a table of the fonts Chromium picked for the game's 20 symbols".
- **The game's fonts** come from `old-game/objects/fonts/` (five faces), read by `UiResourcesTests` and packed into
  `ui.pak` by `make-ui-pack.sh`.
- **What the engine's tests read from `old-game/`** (Engine-Plan.md, "The full run after the merges"):
  - "only the five fonts in `objects/fonts/` (`UiResourcesTests`)";
  - "`classes/Black Knight.json` (`LooseRecordTests`)";
  - "the six race SVGs in `objects/` that the game-screen captures name (`GamePaintTests`,
    `CompositingReferenceTests`)".

  The stylesheet is the tests' own copy.
- **Chromium 152 on Linux** "loads every web font with Fontations and Skia draws its glyphs from the outline", so
  the engine ports Skia's glyph path and skrifa's autohinter. The LCD preference is ignored at a device scale of
  1.5 and up, so at 1.65 text is LCD or grayscale per cc layer.

### 4.5 What `Compositing-Port.md` says must be reproduced

- **The user's decision (2026-10-24):** "match Chromium exactly — text is grayscale where Chromium 152 puts it in a
  cc layer that can't use LCD text, LCD elsewhere. This needs Blink's layerization."
- **What the old game's screens show:**
  - the fixed side menu `.main-tools` is its own layer by **Overlap** and not opaque, so its text is grayscale;
  - modal layers are FixedPosition plus BackdropFilter;
  - their scrolling panels and the siblings overlapping them become Overlap layers;
  - scrollbars are their own layers.
- **The rules ported:**
  - direct compositing reasons and composited scrolling;
  - `LayerizeGroup` with merging, overlap and sparsity (10000·DPR²);
  - the opaque analysis (≤ 8 ops);
  - cc's LCD verdict;
  - paint order (flex and grid items atomic, layer subsequences);
  - paint offset translations, SVG roots, effects;
  - "Which verdict a drawing gets": the chunk its display item will join.
- **Status:** "**All 22 game screens composite exactly like Chromium**". Its "Next" says: "Come back to this doc
  when a change moves layers or LCD verdicts (`CompositingReferenceTests` fails) or when new screens are
  captured."
- **Debugging:** `COMPOSITING_CHUNKS=1 dotnet test … --filter "FullyQualifiedName~CompositingReference&DisplayName~shop"`.
- **"Known differences left (SVG)":**
  - SVG children's transform animations aren't composited apart;
  - there are no transform, opacity or filter nodes for SVG children (filtered shapes aren't drawn);
  - fill and stroke come from attributes only;
  - the stroke box is approximate;
  - path antialiasing (tolerance 60).

### 4.6 Protecting the player's folders

"Decision on test runs and the player's folders" (2026-11-05):
- `Infrastructure/PerUserFolders.cs` points .NET's `XDG_DATA_HOME`, `XDG_CONFIG_HOME` and `HOME` (on Windows also
  `APPDATA`, `LOCALAPPDATA`, `USERPROFILE`) at a per-run scratch folder.
- `PerUserFoldersTests` checks that this machine's folders resolve there.
- `mutate.py` flags any change to the real game folder or the Documents folder's top level.
- Scripts run every program with a scratch per-user folder: the editor's `--player-folder`, the sandbox through
  `XDG_DATA_HOME`.
- Not known: whether Windows' known-folder lookup follows the redirect ("Windows check", item 7).

### 4.7 What cannot be checked against Chromium (or is checked another way)

- **3D:** models, stages, terrain and particles are checked against Blender, not the old game. The old game draws
  SVG, so its art has no 3D reference, and inline SVG art is masked in the screen tests. The user approves art
  through Blender renders (M7 model approval, M10's bar). Particle motion and an animated lens are "Not checked
  against Blender" (Scene.md, step 5). Level of detail isn't checked against Blender, and nothing is measured on a
  2016-class GPU.
- **Sound effects:** the old game has none, so they have no game reference. Their fixtures show only "that the
  engine's nodes render the graph as Chromium's do".
- **Audio by ear:**
  - The themes were heard and approved by the user on 2026-11-02.
  - The proof cutscene's three sounds are "not judged by the user's ear" (levels and lengths are first guesses).
  - The editor's preview has not been heard (by-hand check (c)).
- **cc's raster of composited layers** is not ported, and those cases are compared by measured geometry.
- **Real windows and devices** are not checked automatically: a minimize under Wayland (GLFW never reports one), a
  real device change, OS drops and closes. These are by-hand items.
- **The engine's own screens** (splash, crash window, credits, editor) are compared with Chromium rendering the
  same markup, not with the old game.

---

## 5. Open engine items that bear on the port

### 5.1 "Small tasks left from earlier milestones" (Engine-Plan.md, "How to continue")

| # | Item | State |
| --- | --- | --- |
| 1 | Window host: send `visibilitychange` and `pagehide`. It became hidden documents, the unload sequence and audibility. | **Done** 2026-11-03. Not checked: Wayland (accepted), Windows. |
| 2 | Repo housekeeping (`.gitignore` of `/.claude/worktrees/`, `/.claude/settings.local.json`) | **Done** 2026-11-03 |
| 3 | Windows | Its own checklist, "Windows check" (§5.3) |
| 4 | Stale paths in `Notes.md` | **Done** 2026-11-02/03 |
| 5 | "Not checked here, needs hardware or a window manager: audio on a really different device, a device lost or appearing, a real window minimize" | Open |
| 6 | **Music switch during a fade-in** (found 2026-11-04, M10 step 6). A switch made exactly at a quantum's start, while the old theme is in its 0.8 s fade-in, cuts the old theme within a quantum instead of fading it (`cancelAndHoldAtTime`). Not checked against Chromium. | Open, no go yet |
| 7 | **The proof cutscene's three sounds by ear** (the rift, the flash, the title). Levels, lengths and the thunder's delay are first guesses; `play-cutscene.sh jiruan`. | Open (the user's ear) |
| 8 | **By-hand checks on Linux**, (a)–(e), for the editor: (a) the shell in a real window, zoom keys, splitter and cursor; (b) real drops and closes (`claude-work/m11/drop-check.sh`, also `x11`); (c) the preview by ear and eye; (d) saving, New mod, Save As, closing with unsaved work; (e) step 14's `ember-rite.pak` mod. M12 adds by hand: the splash, About, the crash window's Copy and "Open logs folder", "Where is Stat Fick?", Game folder…, and About scrolled ("Current task"). | Open, the user decides when |
| 9 | **A stop for sound effects.** `SoundEffects` has `Register`, `Play` and `PlayAt` only; a voice plays to its end. Proposed: `SoundEffects.StopAll()` with a 5 ms fade. | Open, no go yet |
| 10 | Housekeeping: worktrees and branches | **Done** 2026-11-12 |
| 11 | **Control labels a device pixel low at 1.65.** A select's or input button's label sits one device row below Chromium's. Suspect: `UiPainter.SnapBaseline` (whole CSS px, then device). | Open (found 2026-11-06), no go yet |
| 12 | **A composited box's text grayscale at 1.65 where Chromium's is LCD.** Suspect: `PaintTarget.GlyphBounds` (ascent to descent) against Blink's glyph ink in the opaque analysis. | Open (2026-11-06), no go yet |
| 13 | **An editor dialog over a composited scroller: text grayscale at 1.65** (1.34 % past 12 levels). Suspect: the same rule as 12, or the dialog layer's translucent background. | Open (M11 step 12), no go yet |
| 14 | **A select with its own padding 6–7 px wider than Chromium's.** Suspect: `ReplacedContent.SelectSize` adding the menu list's internal paddings on top of the author's. | Open (M11 step 9), no go yet |
| 15 | **The painter paints every row of a scroll container**, seen or not: 2,021 rows took 109 ms a frame. Blink paints within the cull rect. "a long list or table in the game (the port's shops, inventories) would" wait on it. | Open (M11 step 8), no go yet |
| 16 | **A status line's text grayscale at 1.65 beside a tall scroller** (up to 1,823 pixels) | Open (M11 step 10), no go yet |
| 17 | **A textarea's `line-height` ignored** (the lines take the font's normal height). Where to start: Blink's `LayoutTextControl`. | Open (M11 step 11), no go yet |
| 18 | **A positioned box with a z-index inside a scroller**, which Chromium composites apart (text grayscale). The editor's reference list was moved into the flow instead. | Open (M11 step 10), no go yet |
| 19 | **A stacking context on the form's rows turning text grayscale at 1.65** (Chromium's look not captured) | Open (M11 step 10), no go yet |
| 20 | **`ValueShape.TextOf` is internal.** Making it public, or adding `RecordPath.Key(ValueShape, object)`, would give callers one rule. | Open (M11 step 10) |
| 21 | **"Known differences" lists**: "accepted differences, not tasks, unless the port finds the game needs one" | Standing |

Items 11–20 are "M11's engine findings" in "Current task". Most are 1.65-only LCD or grayscale verdicts or control
geometry in UI the game will also use: selects, buttons, textareas, scrollers, long lists.

### 5.2 Found by M12 and left for the UI's owner (`Packaging.md`, "Status")

- **Step 5:**
  - the caret-end reveal against Blink's range reveal;
  - the crash window's "Copy" drawn grayscale instead of LCD;
  - Wayland protocol errors in `cage` at scale 1.65.
- **Step 6:** "`flex: 1` with a 0 % basis gives no height in a column flexbox of indefinite height" ("an item for
  the engine's UI").
- **Step 4, not ported:** `image-rendering`, the strict source-rect constraint, copy-from-copy scaling.
- **Open:** "publish fails when the repo is reached through a symlink" (`/home` → `/var/home`); "`pwd -P` in
  `env.sh` likely fixes it; not tried".
- **Also noted:** Lato's shipped OFL text mis-encodes "Łukasz"; `Microsoft.Extensions.ObjectPool` is an 11.0
  preview; there is no command form for `mutate.py` to mutate a script's rule.

### 5.3 "Windows check" (Engine-Plan.md, "How to continue"): open, nothing run

"Nothing since the move to GitHub (2026-10-30) has run on Windows; M6, M7, M8 and M9 never did." Only the compile
was checked: on 2026-11-03 every project built for `win-x64` from Linux. The items, each with what to do and what
passes:
1. **Set up, build, test** with `setup.ps1`, `build.ps1` and `test.ps1`. The same results as Linux, the goldens
   platform-pinned, and `build.ps1` run twice.
2. **M9's items:**
   - (a) replacing an open pack and Reload;
   - (b) saves with `File.Replace`, export and import through the Documents known folder, "Open game folder" in
     Explorer;
   - (c) `textconv.sh` in Git for Windows;
   - (d) the sandbox mounting `packs\`, `classes\` and `mods\`.
3. **FreeType's Windows DLL** and `FT_Library_SetLcdFilter`.
4. **M8 audio through OpenAL Soft** by ear. There is no `play-theme.ps1`; the commands are given.
5. **Hidden documents and minimizing** (blur, then visibility; throttling; sound pausing).
6. **GPU renders on a Windows GPU.** "a failure is reported as it is, its rule found, not given a wider margin".
7. **Do the tests' redirected folders hold on Windows?** (`GetFolderPath(ApplicationData)`, `MyDocuments`). If
   not, ask the user how to close the gap.
8. **M11's items (a)–(g):**
   - (a) writing over an open pack;
   - (b) import on a case-insensitive file system;
   - (c) real drops and closes;
   - (d) `editor.ps1` and the editor in a real window;
   - (e) the file browser's places;
   - (f) the preview's sound;
   - (g) a mod made and loaded.
9. **M12's items (a)–(i):**
   - (a) started from Explorer: no console, the splash, the names;
   - (b) logs, 10 kept;
   - (c) the crash window, exit 1, no Windows Error Reporting;
   - (d) the editor finding the game, and another version refused;
   - (e) a wrong command line;
   - (f) SmartScreen;
   - (g) the Visual C++ runtime without the redistributable;
   - (h) `publish.ps1` and `editor.ps1`, with `check-packages.sh` on the result;
   - (i) a mod made in the packaged editor, loaded by the packaged sandbox.

When it is done: a Progress entry "Windows check", a dated line in `Notes.md`, and the "Not checked: Windows" lines
in `Assets.md`, `Editor.md` and `Packaging.md` turned into "checked on Windows <date>".

### 5.4 Known differences accepted (not tasks unless the game needs one)

- **M5's "Known differences left (not yet ported)"** (Engine-Plan.md, Progress, M5):
  - **Outlines and selection:** an outline on a wrapped inline drawn per line box; `::selection`'s own
    decorations; a list box's focused option ring; the ellipsis of editable text holding the focus.
  - **Box edges and shadows:**
    - rounded corners at small radii;
    - slanted box edges (the canvas ramp against Skia's path fill; `box-slant` 12 / 1 %);
    - an opaque box's outer shadow cut at the box's edge;
    - bleed avoidance for colour-only rounded borders;
    - the square corner's coverage;
    - the 2D-scale effect node (`NeedsEffectFor2DScaleTransform`);
    - Skia's whole-pixel rect blur;
    - a true Gaussian against Skia's three box passes (inset shadows up to 4 levels lighter in `game-sheet`@1.65).
  - **Backgrounds:** `background-clip` and `background-origin` parsed and ignored ("the game uses neither");
    clip-layer bleed avoidance per draw; one-axis raster repeat; composited fixed backgrounds; SVG shapes in a
    background tile; dashed border ends at 1.65.
  - **Composited transforms:** cc's raster of composited transformed layers (measured geometry within 0.1 px).
  - **Not implemented:** text drag and drop; `transition-behavior: allow-discrete`.
  - **Ordering:** animation events of different elements come in tree order.
  - **The host:** wheel and touchpad not told apart; the wait, help, cell, alias, copy, zoom and grab cursors
    show the arrow; Ctrl+Shift with a non-letter key; AltGr reported as Alt; no SVG or `.cur` cursors.
- **M4's "Known approximations":**
  - fractional font sizes up to 0.2 % narrower;
  - variable font axes not applied;
  - inside markers text-based;
  - closed `<details>` not laid out;
  - a block inside an inline box turns the inline into a block (the game has none; `LayoutDiagnostics` reports
    any).
- **Text differences left** (Progress, Zoom):
  - the LCD blend's rounding;
  - a glyph within a float ulp of a quarter pixel;
  - STIX (CFF) hinting from FreeType;
  - fake bold's skip-ink outline;
  - system fallback fonts drawn like web fonts;
  - medium and full hinting.
- **`Compositing-Port.md`, "Known differences left (SVG)"** (§4.5).
- **`Scripting.md`, "Known differences left":**
  - composited animations' start time;
  - `Animate` needs an element in a document, with no pseudo-element targets and no `playbackRate`;
  - the CSS animation clock between frames.

  Its hidden-document list adds: Wayland; Chromium's 8 ms leeway; one start time per wake-up; no `beforeunload`,
  freeze or resume.
- **`Animation.md`, "Known differences from Blender's NLA":**
  - fades of hand-keyed rotations;
  - rotations past a half turn;
  - Blender's float32 quaternion power;
  - IK baked before blending;
  - an object channel unset falling to rest.
- **`Audio.md`:** the noise layers' wave-like sound, which the user chose to keep.
- **`Scene.md`:** the card at 1.4 s (cc's layer raster, a few pixels); a sheared parent inverse avoided in stages.
- **`Editor.md`, "Status", known limits:**
  - one pack at a time (P18: a mod can't refer to another mod's entries);
  - add, remove and rename not undoable;
  - only the first problem per field shown;
  - the JSON tab capped at 2,000 lines;
  - Stop doesn't cut a sounding effect;
  - no previews of state machines, UI files or terrain;
  - P30.
  - **Stale line:** the Status still says "the editor and the packer know the engine's kinds only until the port
    answers G3" (`Editor.md` line ~3132), although M12-9 closed G3.

### 5.5 Other open points named in "Current task"

- **The first real draft release on GitHub** is made only on the user's word (`release.sh`; its dry run works).
- **The open port question** (§3.0): tasks 10, 12, 13 and 14 while porting or after.
- **"Engine work goes on uninterrupted"** beside the port tasks. Each port task paragraph says not to count it as
  the engine's current task.

---

## 6. Conventions

### 6.1 Docs

- **`engine/Docs/Engine-Plan.md`** is the living plan: "How to continue", the decisions, Progress and Milestones.
- **One design doc per area since M6, `Docs/<Area>.md`:** `Renderer-3D.md`, `Animation.md`, `Audio.md`,
  `Assets.md`, `Scene.md`, `Editor.md` and `Packaging.md`. M5's parts are `Compositing-Port.md` and
  `Scripting.md`. "Design docs by area" in Engine-Plan.md lists them with one line each, and a new area adds its
  line there.
- **`Docs/Notes.md`** holds dated history entries ("- 2026-11-08 (3719 tests, +61, on the GPU and lavapipe,
  headless; a worker worktree, branch …)"), gotchas, recipes, the asking rule, the Blender notes and the BRUTAL
  test history. On the PC, Claude's memory file `stat-fick-port.md` is kept too.
- **`Docs/Port-Tasks/`** holds the user's tasks for the port, in number order; its README comes first.
- **The old game's own notes** are `old-game/Docs/Notes.md` (each port task done there has a section).
- **The rule:** docs are kept current after every step ("they are the only notes a cloud session has", the root
  CLAUDE.md).

### 6.2 Decision and question identifiers

| Scheme | Where | Example |
| --- | --- | --- |
| Milestone-level questions | Engine-Plan.md "Decisions for Mn", summarised in the design doc | `M10-2`, `M11-19`, `M12-9`. M5–M9's main decisions are unnumbered bullets ("Pack layout", "Compression" …). |
| Detail round after the design doc | the design doc ("Format details", "Step details", "Decided (Q1–Q28 …)") | `Q1`…`Q43` (Assets), `Q1`…`Q33` (Scene), `Q1`…`Q30` (Packaging). Q-numbers restart in each doc. |
| Questions during a step | the design doc, by step | `S3-1`, `S5-Q1`, `S6-A`, `S7-14`, `S9-D` |
| Step-internal manager questions | the step's "How it works" | `Q-A`, `Q-B`, `Q-C` (Packaging step 6); Editor steps' "Q2 of 2026-11-07" |
| Manager's editor choices | `Editor.md`, "Decided by the manager (P1–P38)" | `P18`, `P30` |
| The editor's questions about the game | `Editor.md`, "The game's questions (G1–G6)" | `G2`, `G3` |
| Port tasks' decisions | Engine-Plan.md, "Decisions for task N …" | task 2 `G1`–`G34`, task 3 `R`, task 4 `A`, task 5 `M1`–`M26`, task 6 `N`, task 7 `L`, task 8 `W`, task 9 `P1`–`P31`, task 10 `C`, task 11 `T`, task 12 `SP`, task 13 `NC`, task 14 `DG1`–`DG78` |

Some identifiers collide: `G1` (M11 and task 2), `P1` (M11 and task 9), and `M1` (milestone and task 5). The docs
always name the source with the identifier ("Decisions for M11", G4).

Recording format:
- each answer dated, with who decided;
- "every answer the recommended option except …";
- **"…, not the recommendation:**" in bold where the user chose otherwise;
- the rejected options as "(Not …; not ….)".

### 6.3 Branches, commits, test counts

- **Branches:** worker branches are `m11-stepN`, `m11-fixes-1`, `m12-design` and `m12-stepN` (`m12-step4-credits`
  for a follow-up). Topical fixes have their own names (`lcd-first-text`, `shadow-inset-caster`,
  `background-layers-aa`). Cloud port-task work ran on `claude/fervent-carson-f5ol9s` and similar, merged by pull
  request (#8–#12, #20).
- **Merging:** done by the merge worker; worktrees are removed after each merge.
- **Progress lines** record the suite count and the change ("3720 tests, +1"), "none held", the GPU and lavapipe
  times, and the mutation counts.

| Milestone done | Tests on `main` |
| --- | --- |
| M1 (2026-10-16) | 13 |
| M2 (2026-10-16) | 36 |
| M3 (2026-10-16) | 42 |
| M4 (2026-10-17) | 129 |
| M5 (2026-10-30) | 493 at its finish, 496 after the border-style fix |
| M6 (2026-10-31) | 573 |
| M7 (2026-10-31) | 635 |
| M8 (2026-11-02) | 1013 |
| M9 (2026-11-03) | 1907 |
| M10 (2026-11-05) | 2516 and 6 held, then 2547 with none held |
| M11 (2026-11-08) | 3496 |
| M12 (2026-11-12) | 3720. Full runs 1 min 57 s on the GPU and 2 min 7 s on lavapipe. |

### 6.4 Test infrastructure names

- **Folders:** `Tests/Statventure.Tests/` with `Golden/` (references and goldens), `Actual/` (mismatching renders),
  `Reference/` (fixture lists and Chromium results), `Data/` (test models, cutscenes, the stylesheet copy,
  `SystemFonts`, record layouts and fixtures, storage data) and `Infrastructure/` (`ReferencePlatform.cs`,
  `SystemFonts.cs`, `PerUserFolders.cs`, `GpuFixture.cs`).
- **Speed tests** run alone in the "Speed" collection.
- **Tools:** `accept-golden.sh`, `diff-images.py`, `mutate.py <list.json>` (lists kept in
  `claude-work/<milestone>/`).
- **Switches:**
  - `STATVENTURE_KEEP_RENDERS=1`
  - `STATVENTURE_RECORD_FIXTURES=1` (writes record layout and mark fixtures)
  - `STATVENTURE_THEMES=1` (the 55 full themes)
  - `STATVENTURE_EXHAUSTIVE=1`
  - `STATVENTURE_INCREMENTAL_ROUNDS` / `_SEED`
  - `COMPOSITING_CHUNKS=1`
  - `SCRIPT_LOG_DIR=/abs/dir`
- **Environment** (`Tools/Scripts/env.sh`): `STATVENTURE_SOURCE`, `STATVENTURE_REPO`, `STATVENTURE_OLD_GAME`,
  `STATVENTURE_SCRIPTS`, `STATVENTURE_CACHE` and `STATVENTURE_GAME` (`env.local.sh` is git-ignored).
  `STATVENTURE_DEV_FOLDER` is the game's development folder.

### 6.5 Folders

| Path | What |
| --- | --- |
| `engine/Assets/Packs/` | The committed packs, the source of truth for the game's data: only `audio.pak` (118 entries) and `ui.pak` (5 font faces and 2 OFL texts) so far. `git config diff.pak.textconv engine/Tools/Packer/textconv.sh` once per clone. |
| `$STATVENTURE_GAME` (PC: `/run/media/system/M.2_2TB/Stat Fick/`, else `runtime/` in the repo) | The dev build: the sandbox with `packs/` beside it. "`classes/` comes with the port". |
| The player's game folder | Linux `$XDG_DATA_HOME/stat-fick/` or `~/.local/share/stat-fick/`; Windows `%APPDATA%\Stat Fick\` (`Application.Storage`; `ApplicationOptions.GameFolder` points it elsewhere). It holds `settings.json`, `saves/` (`<name>.dat`, `.dat.bak`, `.dat.damaged`), `mods/`, `logs/` (M12) and `editor/` (`shell.json`, `recent-places.json`, `volume.json`, `stand-ins.json`, `game.json`; the game ignores it). Folders are made 0700 and files 0600. There is no portable mode (M12-11). |
| The Documents folder | Character files (`stat-fick-character.json` by default; Chromium's XDG lookup on Linux, the known folder on Windows) |
| A shipped game | `stat-fick-<v>/`: the executable, the natives, `Statventure.Game.Data.dll`, `packs/`, later `classes/`, `NOTICE.txt`, `licenses/` (the stand-in also has `Data/`) |
| `~/.cache/stat-fick-build/` | The .NET SDK, NuGet, Electron 44.5.0, `reference-src/` (PC only), `music-data/`, `vc-runtime/`, `end-to-end/`, and `claude-work/` (session working files, kept across sessions) |
| `release/`, `release/.publish` | The published archives (git-ignored) |

### 6.6 The mod format

- **A mod is a `.pak`** placed directly in the player's `mods/` folder. Mods mount above the game's packs and
  `classes/` and below the development folder, in ordinal file-name order (`.pak` in any case). A mod adds or
  replaces entries; it can't delete one (M9 Q8, Q30). "no in-game mod list" yet.
- **`mod/info`** is an entry of the engine's kind `mod.info` (`Packs/ModInfo.cs`, `ModInfo`), with `Name`,
  `Author`, `Version` and `Description`, all strings defaulting to empty, at `ModInfo.Path` = `mod/info`. The
  editor writes it on New mod. "The game never reads it"; "mods without it still load"; "A mod list in the game can
  read each mod file's own entry later" (G1, M11-15).
- **The editor saves** only when no entry has a problem, so "a saved mod loads in the game" (G4, G5).
- **Game entries edited in a mod** are the mod's own copies (P7).
- **A mod can't refer to another mod's entries** through the editor (P18).
- **Still open (G2):** what the game does with parts it can't read.

### 6.7 Pack paths and record kinds

- **Paths:** lowercase ASCII `a–z 0–9 - _ . /`, under the area's folder:
  - `audio/themes/<name>` (`combat@alder-vale` → `combat.alder-vale`), `audio/instruments/…`,
    `audio/percussion/…`, `audio/rooms/…`, `audio/effects/<name>`, `audio/theme-order`
  - `ui/styles/<name>`, `ui/fonts/…`, `ui/images/…`
  - `models/<name>`, `models/machines/…`, `models/stages/<name>`, `models/effects/<name>`
  - `world/terrain/<name>`, `world/cutscenes/<name>`
  - `mod/info`
  - Urls in stylesheets are full data paths.
- **Pack per area:** `models.pak`, `audio.pak`, `ui.pak`, `world.pak` and `data.pak` (data: "the game's tables,
  once the port defines their records"). Classes stay loose `.json` in `classes/`.
- **Kind names:** dotted, lowercase, area first: `audio.*`, `model.*`, `animation.*`, `ui.*`, `scene.*`,
  `particle.*`, `terrain`, `mod.info`, the tests' `test.*`, and the stand-in's `stand-in.item`.
- **Kinds are declared, not registered:** `[Record("name", version)]` on the type, `[Field(n)]` in declaration
  order, `[Subtype]` for unions. Past versions are frozen nested types with `Upgrade()`. A changed default or a new
  union subtype is a new version (Q33, Q37).
- **Editor marks:** `[Kind("Display name", "folder")]` sets the entry list's name and folder (e.g. "Cutscene" in
  `world/cutscenes`). Checks are `[Check]` methods.

### 6.8 Programs, versions, logs

- **Executables:** the game `stat-fick` / `Stat Fick.exe` (the old game's names); the editor `statventure-editor`
  / `Statventure Editor.exe`.
- **Version:** `2026.11.12.1`, from the released commit's UTC date plus a count; `dev` otherwise.
- **Tags and titles:** tag `v<v>`, release title "Stat Fick <v>".
- **Logs:** `<program>-yyyy-MM-dd_HH-mm-ss.log`, with the program, version and commit in the first lines.
- **Crash files:** `crash-<stamp>.txt`. Exit code 1 after a crash; 2 for the editor's command-line problems.
- **No icons** yet (M12-5).


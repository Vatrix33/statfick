# engine/ (Statventure engine + the BRUTAL port of Stat Fick)

Read `Docs/Engine-Plan.md`, section "How to continue", before doing anything here. It has the current
task, the method (Chromium 152 references and source first, tests against them), the rule to keep the
docs and notes current, and the open design questions.

**Ask the user about design choices as soon as they come up — never improvise.** Use the question tool
with 2–4 concrete options (recommended first, with the reason), then record the answer with its date in
`Docs/Engine-Plan.md`. This covers anything the player would see, hear or do differently, looks,
behaviour the old game doesn't define, data formats, folder layout and scope. Don't ask what the code,
Chromium or the docs already answer.

**Stop when a milestone is finished** (tests pass, ticked in Engine-Plan.md, notes updated): report to
the user and wait for their go-ahead before starting the next one.

- The repo root is one folder up (`../CLAUDE.md`: the machines, git, the old game). Don't delete or
  overwrite without looking first.
- Build and test: `Tools/Scripts/test.sh` (Windows: `Tools\Scripts\test.ps1`); all tests must pass
  before a step counts as done. They pass the same on the PC's GPU, on a CPU Vulkan (the cloud) and
  on Windows: they act as Chromium on Linux with the reference PC's fonts and font settings.
- Chromium 152 for references: `Tools/Scripts/chromium.sh` (Electron 44.5.0 from npm).
- Blender 5.2 for the 3D references (M6 on): `Tools/Scripts/make-blender-references.sh [scene ...]`
  (`Docs/Renderer-3D.md`, "Reference harness"; on the PC the Flatpak, run headless). Its renders and
  test models are committed, so the tests run where there's no Blender (the cloud).
- On the PC a step is also checked on the CPU Vulkan the cloud uses (Mesa's lavapipe):
  `VK_DRIVER_FILES=/usr/share/vulkan/icd.d/lvp_icd.x86_64.json VK_ICD_FILENAMES=$VK_DRIVER_FILES Tools/Scripts/test.sh`.
- Reference sources: `~/.cache/stat-fick-build/reference-src/` (save new downloads there).
- Notes kept from earlier sessions (history, gotchas, recipes): `Docs/Notes.md`.

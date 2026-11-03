# Stat Fick (the repo)

Two projects:
- `engine/`: Statventure (C#, Vulkan, on BRUTAL) and the port of the game onto it. This is the active
  work. Read `engine/CLAUDE.md`, then `engine/Docs/Engine-Plan.md` ("How to continue").
- `old-game/`: the original JavaScript game in its Electron shell. It's the reference the port is
  checked against (the engine's tests read its stylesheets and scripts; the reference harnesses drive
  it). Read `old-game/CLAUDE.md` before changing it.

The user's standing rules:
- Ask about every design choice as soon as it comes up (the question tool, 2–4 options, the
  recommended one first) and record the answer with its date in `engine/Docs/Engine-Plan.md`. Never
  improvise or carry on with an assumed answer.
- Stop when a milestone is finished; wait for the user's go before the next.
- `engine/Tools/Scripts/test.sh` must pass in full before a step counts as done (on the PC on the GPU and on lavapipe),
  and new tests must be shown able to fail by breaking each rule they cover.
- Keep the docs current after every step (they are the only notes a cloud session has).

Where this is worked on (decided 2026-10-30):
- **The user's PC** (Fedora, AMD GPU, `/run/media/system/M.2_2TB/Stat Fick Repo/`). The Chromium
  references were captured there; the reference sources cache (`~/.cache/stat-fick-build/reference-src/`)
  is only there.
- **Claude Code cloud sessions** (claude.ai/code, also from the phone app): Ubuntu 24.04, no GPU.
  The environment's setup script is `engine/Tools/Scripts/cloud-setup.sh` (.NET 10, Vulkan on the CPU
  via Mesa's lavapipe, Xvfb, Electron 44.5.0). The tests pass on the CPU Vulkan as on the GPU.
  Reference captures run under Xvfb. Fetching Chromium or Skia source from googlesource needs
  `chromium.googlesource.com` and `skia.googlesource.com` added to the environment's allowed domains; without
  them, GitHub's mirror works: `https://raw.githubusercontent.com/chromium/chromium/152.0.7977.89/<path>`.
- **A Windows PC**: build, run and test with `engine\Tools\Scripts\setup.ps1` (once), `build.ps1`,
  `test.ps1`. No reference captures there.

Git (decided 2026-10-30): the repo is private on GitHub (the user's existing repo). Cloud sessions
push their own branch and open a pull request, which the user merges on GitHub; sessions on the
user's PC commit finished steps to `main` and push. Pull before starting work on either side.
Commits carry the user's email and end with the attribution line your environment gives:
`git -c user.email=meistervatrix@gmail.com commit ...`. Push with
`GIT_TERMINAL_PROMPT=0 GIT_ASKPASS=/bin/false SSH_ASKPASS= git push -q origin main`. Everything on GitHub stays private.

The tests give the same results on all three: they act as Chromium on Linux with the reference PC's
font settings, and take system fonts from the repo (`engine/Tests/Statventure.Tests/Infrastructure/`
`ReferencePlatform.cs`, `SystemFonts.cs`).

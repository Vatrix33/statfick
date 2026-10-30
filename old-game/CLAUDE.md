# old-game/ (the original Stat Fick)

The JavaScript RPG (character sheet, regional map, turn-based combat, guild, companions) in an
Electron shell. The port in `../engine/` is the active work; this game is its reference: the engine's
tests read its stylesheets, scripts and images, and the reference harnesses drive it in Chromium 152.
Change it only when the user asks. Design history, systems and gotchas: `Docs/Notes.md`.

- **Run the headless tests** (the test client, `test-client/`):
  `../engine/Tools/Scripts/chromium.sh test-client --suite=smoke|combat|enemies|lifecycle|all`
  (Electron 44.5.0 from npm; it tests this folder). A report path is printed at the end.
- **Build the executables** (not in git): in `desktop/`, `npm ci`, then `npm run build:linux` or
  `npm run build:windows`, and copy the result from `desktop/release/` here. The executables read the
  game files next to them (`desktop/main.cjs` refuses to start when one is missing).
- `objects/Stat Fick.desktop` is the user's launcher on their PC (absolute paths).

# Foundation (GM1)

The game's first milestone (`Docs/Game-Plan.md`, GM1): the game's own program and its real data library in place of the
sandbox and the stand-in; save slots, character files and `settings.json`; the start screen, the in-game menu, the
settings screen with Dev Mode's switch and the mod list, the slot list, the file picker, About; the check of every mod
at start; the port's stylesheet in `ui.pak` and the game-screen references captured again from today's old game; the
symbols the fonts lack as vector icons; the randomness service; the interface sounds. Nothing to play yet: the sheet
comes with GM2, and no music plays until GM4. Written on 2026-11-13 (docs only, the branch `gm1-design` from `2bd4da9`),
checked against the code of both the old game and the engine that day; the user's detail round and the manager's
decisions on the proposals were recorded the same day, and the design and the steps made final.

Paths are relative to `engine/` unless they start with `old-game/`. "The old game" is `old-game/` on `main`.

## Decisions

### The user's decisions for GM1 (2026-11-12)

In full, with the options not taken, in `Docs/Game-Plan.md`, GM1, "Decisions for GM1". In short:
- **GM1-1** The game-screen references captured again once at GM1's start, all 11 states at 1 and 1.65 from today's
  old game, the tests' stylesheet copy brought up to it.
- **GM1-2** After the splash a start screen in the game's look: Continue (the last character), New character, Load
  (the slots), Settings; then the sheet. (Q16 and Q16b added Quit and About and keep Continue greyed when unusable.)
- **GM1-3** Dev Mode switched in the settings screen; the old ✦ button disabled (whether it stays: Q11, it stays as the
  brand mark, always lit).
- **GM1-4** The theme a game setting in `settings.json`, both themes kept, dark by default.
- **GM1-5** Every load draws fresh randomness; the random state isn't saved.
- **GM1-6** Each milestone makes its own aspect's sounds, their list asked with its questions and approved by ear.
- **GM1-7** Symbols outside Lato and STIX become vector icons drawn by the engine in the game's ink.
- **GM1-8** Screens the old game doesn't have are built from its panels, buttons and dialogs in its Gothic ink theme and
  shown to the user as captures before their tests.
- **GM1-9** English only.
- **GM1-10** A mod list in the settings, each mod named from its `mod/info`; every mod checked at start, a broken one
  named and left out (answers M11's G2).
- **GM1-11** Soft sounds for buttons and for windows opening and closing, besides the game-event sounds.
- **GM1-12** Dev Mode off by default; turned on in the settings when wanted.
- **GM1-13** Dev Mode's state in `settings.json`, the same for every character.

### Earlier decisions that bind GM1

- **Saves start fresh; old saves and character files are never imported** (User decisions 2026-10-16; Decisions for
  M9, "Character export and import"; `Docs/Assets.md`, Decisions). `normalizeCharacter` and every old migration stay in
  the old game (`Docs/Game-Plan.md`, "What the port never does").
- **No native menu bar and no OS dialogs** (Decisions for M5, 2026-10-17): Save/Load Character, Open game folder, Quit,
  Reload, Zoom, Fullscreen, About and file picking become in-game UI (Reload then left out by Q18). **Tooltips, select
  lists and validation bubbles are game-styled** (the same decisions; the game's stylesheet styles them at port time,
  "like the leather combat tooltips; a parchment list in the game font").
- **Screens are built in C#; CSS-like stylesheets compiled into the packs** (User decisions 2026-10-16, UI).
- **Sound effects are effect patches, no audio files; the port adds proper sound effects** (Decisions for M8, "Sound
  effects", and the user's decision of 2026-11-03). Dry on the effects bus, seeded variation, 4 voices a patch and 32 in
  all (step 8's questions).
- **M9's storage** (Decisions for M9 and `Docs/Assets.md`, "Format details"): named slots `saves/<name>.dat` with
  `.dat.bak`, names under the path rule, at most 100 characters (Q15, Q4, Q40); written on every request by a background
  writer, flushed on quit (Q16); a damaged or unreadable save loads nothing and the game asks "use the backup, start
  new", the damaged file kept aside (Q17, Q39); a slot can be deleted (Q41); `settings.json` with a version, unknown keys
  kept, missing ones default, a broken one kept as `settings.broken.json`, a newer one treated as broken, a wrong type
  defaulted (Q18, Q42, Q43); character files are the save's JSON form, `stat-fick-character.json` in Documents by
  default, through the in-game picker (Q19; "Character files"); one per-user game folder that "Open game folder" opens
  (Decisions for M9, "Where saves and settings live"); a version per record type with frozen past versions, `Upgrade()`
  and committed fixtures (Q10, Q13).
- **M9's mods** (Q8, Q30; S3-4): every `.pak` directly in `mods/`, in ordinal file-name order, adding or replacing,
  never deleting; above the game's packs and `classes/`, below the development folder. GM1-10 adds the list and the
  check.
- **M11's G4 and G5** (`Docs/Editor.md`, "The game's questions"): the game refuses what the editor refuses (declared
  limits, the kinds' checks), and the checks that once ran only when something played run when the data loads.
- **M12** (Decisions for M12; `Docs/Packaging.md`): the executables `stat-fick` and `Stat Fick.exe` (M12-2); the data
  library `Statventure.Game.Data`, linked by the game, loaded by the editor from the game's folder, one file beside the
  game's executable, of the editor's version (M12-9, Q18, Q19); the game's window title stays its name (Q3); the BRUTAL
  splash at every start, 2 s, skippable after 1 s (Q29); the credits with BRUTAL's notice (Q11, Q30: the port shows them
  again "in its own screens"); logs and crash files in the per-user `logs/` (M12-8); the crash window (M12-10);
  invariant globalization (Q8); package checks in `check-packages.sh`, not in `test.sh` (Q28).
- **GP-1** (the old 130 classes and 388 spells aren't shipped: no `classes/` in GM1; GM8 makes the class files), **GP-3**
  (a milestone closes with its aspect's final art; what belongs to a later one shows inert or as a stand-in until then),
  **GP-6** and **GP-7** (`Docs/Game-Plan.md`, "Decisions for the plan").
- **Test runs never touch the player's folders** ("Decision on test runs and the player's folders", 2026-11-05); no
  window on the user's screen, nothing audible from a session ("How each step is finished", item 1; the standing rule
  in "Small tasks", "The proof cutscene's three sounds by ear").

### The detail round (Q1–Q28 and Q16b, the user, 2026-11-13 and 2026-11-14)

Asked by the manager with the question tool in six rounds of four after this doc's first version, then Q24, which the
answers raised, the same day, and Q25 and Q26, which step 5 raised, the same day, then Q27 and Q28, which step 6 raised,
on 2026-11-14 (the questions as asked, with their facts and options: "The questions as asked" below). Every answer the
recommended option (a) except **Q6, Q11, Q15, Q16 (with its follow-up Q16b), Q18, Q22, Q23 and Q25**. Recorded in
`Docs/Game-Plan.md`, "Decisions for GM1", too (GP-7). Nothing is open.
- **Q1, a character's name and its file:** any name up to 80 characters, kept in the save; the slot's file is named from
  it by a rule ("Ser Alrik" → `saves/ser-alrik.dat`, a second one `ser-alrik-2.dat`). (Not only the path rule's
  characters accepted; not the name encoded reversibly in the file's name.)
- **Q2, when a new character is named:** asked before the sheet, the next free "Character <n>" filled in. (Not named
  automatically and renamed later.)
- **Q3, the mod list:** an on/off switch per mod, kept in `settings.json`; the order stays the files' name order. (Not a
  list to read only; not switches and an order of the player's.)
- **Q4, a broken mod:** what the editor refuses to save, each mod checked at start as the editor checks it, alone over
  the game's packs; the whole mod left out; the added start time measured, and the user asked again if it is noticeable.
  (Not only unreadable files and unknown kinds, values and references checked when used.)
- **Q5, what GM1 shows in a character:** the top bar over an empty parchment page, the ⚙ Settings button. (Not the whole
  old sheet inert; not staying on the start screen until GM2.)
- **Q6, the top bar's buttons, not the recommendation: "Name and status only".** The theme toggle and Save, Load and New
  Character leave the top bar; their jobs live in Settings (the theme), the menu (Save Character, Load Character) and the
  start screen (New character). The top bar holds "Return to combat" (disabled), the ✦, "Stat Fick", the world clock,
  the autosave status and Q7's Menu button; it is a changed screen. The status line under it stays for the file
  messages. (Not all kept and working, the recommendation; not only the theme toggle dropped.)
- **Q7, how the in-game menu opens:** a "Menu" button at the top bar's right end, and Escape when no window is open.
  (Not the ⚙ button turned into the menu; not Escape only.)
- **Q8, the menu's items:** the old menu's, with Settings and "Start screen" added. (Not "Characters…" over the sheet;
  not the old items only.)
- **Q9, Load Character:** the file's character becomes a new slot under the name it holds (a number added if taken) and
  opens. (Not over the current character after a confirmation; not asked each time.)
- **Q10, a mod left out:** a line on the start screen names it; the list says why, the log has the details. (Not the
  list and the log only; not a window at start.)
- **Q11, the ✦, not the recommendation: "Inert, always lit".** The brand mark only: always lit, not clickable, no
  tooltip, nothing to do with Dev Mode. (Not inert and showing Dev Mode's state, the recommendation; not removed.)
- **Q12, the icons:** each in the colour the stylesheet gives the text, the symbol's figure as an inked shape, approved
  as a sheet of captures. (Not always the ink colour; not plain solid glyphs.)
- **Q13, GM1's sounds:** the eight interface sounds (a button pressed, a switch or box changed, a window opening, a window
  closing, a character file saved, a character opened, an action refused, a character deleted; the press also when a
  volume slider is let go, at the new level). (Not four basics.)
- **Q14, their character:** paper and ink. (Not brass and leather; not soft tones.)
- **Q15, music in GM1, not the recommendation: "No music until GM4".** Nothing plays in GM1; no theme is started by
  GM1's code. The music volume stays a setting, kept and applied from GM4; what its row shows with no theme playing is
  part of the settings screen's captures. (Not `alder-vale` from the start screen on, the recommendation; not a silent
  start screen and `alder-vale` in a character.)
- **Q16, the start screen's buttons, not the recommendation: "Add Quit and About".** Continue, New character, Load,
  Settings, Quit, About. (Not the four plus Quit, Continue left out when unusable, the recommendation; not the four alone,
  Continue greyed.) **Q16b, asked by the manager as a follow-up the same day since (c) left it unsaid, not the
  recommendation: Continue is shown greyed** (disabled) while there is nothing to continue, never left out. (Not left
  out, the recommendation.)
- **Q17, the slot list:** Open and Delete, Delete asking once. (Not also Rename; not also Rename and Copy.)
- **Q18, Reload, not the recommendation: "No Reload".** Data and mods are read at start only: no Reload item and no
  Ctrl+R, with or without Dev Mode. A mod added, removed or switched takes effect at the next start, and the mod list
  says so where a switch was changed (its words part of the captures). The engine keeps `ReloadData()`; the game doesn't
  offer it. (Not Reload while Dev Mode is on, the recommendation; not Reload for everyone.)
- **Q19, the window between runs:** as the player left it (size, maximized, full screen) in `settings.json`, 1440 × 950
  the first time. (Not always 1440 × 950.)
- **Q20, About:** a parchment dialog in the game's look holding the engine's credits text and the version. (Not the
  engine's dark credits view.)
- **Q21, the damaged-save question:** "Use the backup (saved <time>)", "Start new" and "Back". (Not the two of M9's Q17
  with no way back.)
- **Q22, settings on a first start, not the recommendation: "Effects lower".** The effects volume at 60 %, the master and
  music volumes at 100 %; zoom 100 %, windowed; dark and Dev Mode off as GM1-4 and GM1-12 decided. (Not everything at
  100 %, the recommendation.)
- **Q23, an old game's character file, not the recommendation: the general message.** "That file is not a valid
  character save", as for any other wrong file; nothing recognises it. (Not a message of its own, the recommendation.)
- **Q24, Load Character from the start screen** (raised by Q6 and Q16: Load Character lived only in the menu, and the
  start screen has no top bar), asked after the six rounds: **in the slot list**: the Load window gets a "Load
  Character…" button that loads a character file into a new slot (Q9); the menu's Load Character and Ctrl+O stay as
  well. (Not a seventh start-screen button; not Escape's menu and Ctrl+O only.)
- **Q25, the name "Start new" keeps when neither the save nor its backup can be read** (raised by step 5: Q21's "the
  name kept" had no name to keep then), **not the recommendation: "The file's name".** The new character is called by
  the slot's file name ("ser-alrik"), with no prompt; when the backup reads, its name is kept as before. (Not the "Name
  your character" prompt with the file's name filled in, the recommendation; not the next free "Character <n>".)
- **Q26, the editor's New entry and the save** (raised by step 5: the editor offered New entry for every plain record
  kind, `save.game` too): **"Not offered"**: the editor leaves `save.game` out of New entry; the record is marked as not
  a pack kind (`[NotAPackKind]`, an engine mark). (Not offered like any kind.)
- **Q27, the mod check's start time** (the user, 2026-11-14, asked by the manager with step 6's numbers: +73 ms with one
  small mod, +232 ms with five, +111 ms with one of 2,056 entries, all before the window opens): **"Keep the full
  check"**: the start checks every mod as the editor would (Q4), its cost accepted. (Not a lighter check at the start, of
  the files and the kinds only.) After the answer, step 1a's stylesheet in `ui.pak` made each mod's check costlier (+494
  ms with one small mod, +1,440 ms with five, +515 ms with one of 2,056 entries): measured by step 6 on 2026-11-14 and
  gone to the manager ("How it works", step 6).
- **Q28, the editor's Import and a character file** (the user, 2026-11-14, asked by the manager; raised by step 6, after
  Q26 left Import open): **"Import refuses it"**: importing a record's JSON form of a kind marked as not a pack kind (the
  save) is refused, in the editor's words ("a save game (save.game) isn't something a mod can hold"), so nothing in the
  editor makes a save entry (New entry leaves it out, Q26; Duplicate has none to copy). The shared mod check stays as it
  is: a hand-made mod holding a save entry isn't broken for that, and the game ignores the entry. (Not Import left as it
  was.)

**The old game is frozen for the recapture** (the user, 2026-11-13, in the chat with the manager): "Old game wont be
touched until you give the ok, except if you need something". Nothing in `old-game/` changes while the references are
captured again (step 1b); the manager gives the user the ok once step 1b is on `main`.

### The manager's decisions on the proposals (GM1-P1–GM1-P19, 2026-11-13)

Every one as proposed ((a) where there were options) except GM1-P11's generator and GM1-P19, which the manager changed:
- **GM1-P1** `Game/Statventure.Game/` (assembly `Statventure.Game`, renamed at publish) and `Game/Statventure.Game.Data/`,
  a `/Game/` solution folder.
- **GM1-P2** the sandbox stays the engine's test bed (not published, no data library, logging as `statventure-sandbox`,
  its cutscene mode, `--log-visibility`, capture, About and crash options kept); the stand-in library project removed.
- **GM1-P3** `stand-in.item` moves into the real library as a development-only kind and goes when GM2's first real kind
  with `[Kind]` arrives; the editor's tests and the end-to-end mod switch to that kind then.
- **GM1-P4** a second test project, `Tests/Statventure.Game.Tests/`, linking the shared test infrastructure where it is;
  `test.sh` and `test.ps1` run both; `mutate.py` takes the project. The suite's count is both projects' sum: a report
  gives each and the sum.
- **GM1-P5** the save record `save.game`, version 1 holding `name`; GM2's part as version 2 with its `Upgrade()` and
  fixtures. **GM1-P6** `settings.json`'s keys as in the design, `lastCharacter` kept there. **GM1-P7** the markup in the
  program, the old pieces as the old HTML text. **GM1-P8** a committed `Assets/UI/styles/game.css` the port owns and a
  sheet per new screen part, packed by `make-ui-pack.sh`. **GM1-P9** one game document.
- **GM1-P10** the icons as outlines in `Assets/UI/icons/` compiled into a table in the game, an engine icon face as the
  last character fallback, each advance the glyph Chromium picked on the reference PC. **Changed by the manager on
  2026-11-13 (step 3):** the icon face draws its code points before any font, so ◌, which Lato and STIX have, is an icon
  too ("The symbols as vector icons").
- **GM1-P11, changed: the randomness service** in the game program; its generator `xoshiro256**` **seeded through
  SplitMix64 from one 64-bit seed**, `Next()` the top 53 bits as a double in [0, 1) (one draw per number, a long
  period). (Not `xoshiro128**`; not the engine's `Mulberry32`.)
- **GM1-P12** the game's command line: `--capture <png> [--frames N]`, `--capture-splash <png>`, `--about`,
  `--open <screen>`, `--crash`, `--crash-thread`, not in a usage text. **GM1-P13** the engine's mod hook in
  `ApplicationOptions` and a shared mod check in the engine's `Packs/` used by the editor's save and the game's start.
  **GM1-P14** single instance by a lock file and a local socket per per-user folder.
- **GM1-P15** the file picker the game's own view on `FileBrowser`. **GM1-P16** sounds' sources in
  `Assets/Audio/effects/*.json`; step 4 picks the packing script by this rule: `audio.pak` stays reproducible from the
  repo by its scripts, byte for byte; if the music import can't be rerun quickly and headless, a new `make-sound-data.sh`
  replaces only `audio/effects/*` in the committed pack; `play-sounds.sh` for the user's ear. **GM1-P17** the game's
  screen fixtures in `Reference/game-screen-fixtures.json` and `make-game-screen-references.sh`. **GM1-P18** the symlink
  fix and the shader repro in step 2.
- **GM1-P19, changed: generated files at merges.** The merge worker never generates, builds or commits anything. When
  two steps of one wave change one generated file's sources (a pack, `Reference/game-screen-fixtures.json`), the one
  merged second rebases on `main`, reruns the script or the tests that make it, reruns its tests and the full suite, and
  only then is merged ("Steps").

## What exists (checked against the code, 2026-11-13)

Nothing of GM1 yet: no game program, no game record kind, no game screen.

### The old game

- **The shell** (`old-game/desktop/`, Electron 44.5.0): `main.cjs` (94 lines) serves the folder beside the executable
  over `statfick://game/` (`assets.cjs`, 85) and refuses to start when `index.html`, `styles.css`, `scripts/app.js`,
  `objects/artifacts.json`, `classes` or `enemies` is missing (15, 28–40). One instance: a second start brings the
  first window back and focuses it, then quits (17–19). One window, title "Stat Fick" whatever the page sets, 1440 × 950,
  minimum 820 × 600, background `#f4efe7` (63–67, 71). The page must load within 45 s (90–93). `preload.cjs` (6) gives the
  page `window.statFickDesktop.saveCharacter(json)` and `loadCharacter()`.
- **The menu** (`main.cjs` 80–89): File › Save Character (Ctrl+S, calls `exportBackup()`), Load Character (Ctrl+O,
  clicks `#backup-restore-button`), Open game folder (`shell.openPath` on the game's own install folder, not a per-user
  one), Quit (Electron's role `quit`); View › Electron's roles `reload`, `resetZoom`, `zoomIn`, `zoomOut` and
  `togglefullscreen` (their labels and keys Electron 44's own, not read here: no Electron source is in the reference
  cache); Help › About Stat Fick (a native message box: "Game files are read live from <folder>. Choose View → Reload
  after editing them. This app stores its own autosave.").
- **On close** (`main.cjs` 73–79): the close is held, the page's `WorldMap.pause()` and `saveCharacter()` run, raced
  against 2.5 s, the storage is flushed and the window destroyed. No question is asked.
- **The save** (`scripts/app.js`): one character, one slot. `saveCharacter()` (1839–1850) writes the whole state as one
  JSON object to `localStorage["emberfall-character-stats"]` (`STORAGE_KEY`, line 1) and lights the autosave dot ember
  for 350 ms (1844–1846); 92 call sites (91 in the scripts, `app.js` 34 of them, `map.js` 22, `guild.js` 12; one in
  `main.cjs` 76). The fields: `Docs/Game-Plan.md`, "Cross-cutting", "The save and what goes in it". A new character's
  defaults (`defaultCharacter`, 1327–1374) include `editingEnabled: true` and `darkMode: false` (1354–1355). The hero
  has no name: only companions are named (the import field's `maxlength="80"`, `index.html` 968).
- **The load path:** at load `readSavedCharacter()` parses the stored JSON through `normalizeCharacter` (1399–1563; the
  migrations the port doesn't carry), the defaults on any failure (1565–1571), into the page's top-level variables
  (1573–1616). `applyCharacter` (1901–1971) puts a loaded character in place and saves it.
- **The backup file:** Save Character (`exportBackup`, 1866–1899) writes `backupPayload()` (1857–1864), `{ format:
  "emberfall-stat-block", version: 22, exportedAt, character }`, indented, through the desktop bridge: a native save
  dialog at `app.getPath("documents")/stat-fick-character.json`, filter "Stat Fick character" (`.json`), refused without
  `stats` or over 10 MB (`main.cjs` 46–54). Load Character (1975–1981) asks the bridge for a file (`main.cjs` 55–62:
  native open dialog, 10 MB) and `importCharacterText` (1983–1990) takes `payload.character ?? payload` if it has
  stats, then applies it over the one character. The status line `#backup-status` (`index.html` 87; `setBackupStatus`,
  1852–1855) says "Character saved", "Character saved to file", "Character could not be saved", "Loaded <file>" or
  "That file is not a valid character save".
- **New Character** (2141–2209): two native confirmations ("Create a new character and clear the current character
  sheet?", then "Are you sure? All current character progress will be cleared. Save the character first if you want to
  keep it."), then every field reset but `editingEnabled` and `darkMode`, saved, focus on the race button.
- **Native dialogs in the scripts:** 12 `window.confirm` (app.js 3, magic-tower.js 3, companions.js 2, shop.js 2,
  guild.js 1, church-of-talos.js 1) and 5 `window.prompt` (app.js 5235, 5454; companions.js 461; church-of-talos.js 69;
  combat.js 1459). In the port they become in-game dialogs (M5); GM1 makes the two kinds of dialog.
- **Settings** (`scripts/settings.js`, 41 lines): `{ battlePace: "fast", combatAnimations: true, battleUi: "stage",
  battleSpeed: "normal" }` in `localStorage["stat-fick-game-settings"]`, safe without storage, a `statfick:settingschange`
  event. **The music volume** (`scripts/music.js`): `localStorage["stat-fick-sound-volume"]` (5), 0–1, default 1, an older
  `stat-fick-music-enabled` false meaning 0 (311–317); `setVolume` ramps the master gain to 0.58 × it over 40 ms
  (1149–1162). Audio waits for the first key or pointer press (a browser rule the port drops).
- **The settings dialog** (`index.html` 23–63): the ⚙ Settings button sits outside the top bar, fixed at the window's
  bottom right (`styles.css` 69–85: `right: 16px; bottom: 16px`; hidden while a window is open, 2601); the dialog is a
  `.modal-layer.settings-layer` with a backdrop button, `section.spell-picker-dialog.settings-dialog` (440 px), the
  header "PREFERENCES / Game *settings*" with a "×" close; then "Sound volume" (a 0–100 % slider and its value, "Adjust
  regional and battle music. 0% is silent."), "Battle interface" (Animated battlefield, Classic log view), "Battle pace"
  (Fast-paced, Slower · time to read), "Combat animations" (a checkbox) and "Current theme: <title>" (`music.js` 346–357).
  `music.js` opens and closes it (1164–1182), focuses the slider, closes it on Escape before any window under it and traps
  Tab (1186–1196).
- **The theme:** `updateTheme()` (`app.js` 1632–1638) toggles `html.dark-mode` and the toggle's ☾/☀ icon, its title
  "Switch to dark mode"/"Switch to light mode" and `aria-pressed`; the toggle (2116–2120) flips `darkMode` and saves it
  with the character. The dark theme is the variables of `html.dark-mode` (`styles.css` 19–35) and about 110 lines of
  `.dark-mode` rules.
- **Dev Mode** (`editingEnabled`): the ✦ switch (`index.html` 68–70; handler `app.js` 2092–2100) flips it and saves it with
  the character. `updateEditingControls` (1718–1748) gives the switch its look (`is-on`) and tooltip ("Enable
  Dev-mode"/"Disable Dev-mode"), enables the attribute fields, steppers below the free points and bonus fields, the
  Armor and Evasion bonuses and the purse (else "Enable Dev Mode to edit money"), and shows the `[data-dev-only]`
  controls: the EXP form, "Restore both to full", "Full HP", "Full Mana", the Combat test button, the spell and item
  catalogues' "+" (`index.html` 121, 136, 155, 170, 240, 358, 417). It also gates levelling without EXP (1823, 2125),
  choosing another race once one is chosen and the enemy-only races (2969–3044), the appearance step being skipped (5761),
  the Combat test screen (`combat.js` 6523–6524), companions taught any spell (`companions.js` 266;
  `companion-sheet-details.js` 107) and the companions' dev tools (`companions.js` 287, 397, 484). Recruits come with it
  off (`companions.js` 163). **Force encounter isn't Dev Mode's** (`map.js` 582–584: shown in an encounter area or for a
  rematch, the map not locked, HP above 0).
- **The top bar** (`index.html` 65–86): "Return to combat" (disabled outside a battle, `styles.css` 1448), the brand (✦
  switch, "Stat Fick"), then the world clock ("Day 1 · 08:00", set by `map.js` 511), "Autosaved locally" with its dot
  (`styles.css` 155–157), the theme toggle (☾), and "↓ Save Character", "↑ Load Character", "＋ New Character" with the
  hidden file input; below it the status line (87) and `#day-status` (88). The rest of `main.shell` (90–300) is the sheet.
- **The dialogs the new screens start from:** 24 `.modal-layer`s in `index.html`, each a backdrop and a
  `section.spell-picker-dialog` (`role="dialog"`, `aria-modal`) with a `header.dialog-header` (eyebrow, a title with an
  `<em>`, the "×" `.modal-close`); `.modal-layer` (`styles.css` 1139; the Gothic ink theme's 3681) centres it on a
  darkened, blurred ground; `updateModalState` (`app.js` 5751) sets `body.modal-open`; Escape closes the topmost
  (`app.js` 5922–5942 and each module's own). Buttons: `.backup-button` (248–257, restyled at 3615–3621), the ⚙ button,
  `.spell-use-button`, `.available-spell-add`. The Gothic ink theme starts at `styles.css` 3507.
- **The start sequence:** `index.html` adds `styles.css?v=<now>` (10–17), then loads 48 scripts one after another
  (988–1001); `app.js` reads the saved character at load (1573), draws (2211–2216) and fetches its data files (5944–5956);
  `music.js` starts on the first key or click. No start screen: the page opens on the one character.
- **The symbols:** a scan of `index.html`, `styles.css` and the 48 scripts on 2026-11-13 (the cmap tables of the five font
  files; `~/.cache/stat-fick-build/claude-work/gm1/design/symbols.py`) finds 53 characters outside ASCII. 29 are in
  neither Lato nor STIX Two Text: ↶ ↺ ↻ ⇧ ⌄ ⌖ ▣ ▲ ▼ ◆ ◇ ◈ ◎ ◫ ★ ☆ ☰ ☾ ♛ ♜ ♟ ♡ ⚔ ⚙ ✦ ✧ ✸ ✺ ＋. Seven are in Lato only
  (← ↑ → ↓ ↗ ≈ ☀), so fine in Lato text; the rest are in both (◌ among them). Eleven rules set their text in
  "Georgia, serif" (`styles.css` 227, 289, 326, 617, 829, 834, 856, 887, 948, 1193, 1217), all on elements holding symbols
  (♜ ♡ ◈ ▣ ◌ ◫ ◆ ✦ ✧ ◇ ♟ and "…") or a race's seal image with ✦ or ♟ behind it (`app.js` 2995–2996, `companions.js` 269).
  The data files (`objects/`, `classes/`, `enemies/`) hold only × – −.

### The engine

- **The application** (`Engine/Statventure/Core/Application.cs`): `ApplicationOptions` (14–58: `Window`, `VSync`,
  `LogName`, `Splash`, `Audio`, `DevelopmentFolder`, `GameFolder`, `GamePacksOnly`). `Run()` (318–356) and its start
  (359–387): the run's log when there is a log name (M12-8), `Storage` for the per-user folder, the data mounted **before
  the window** (368–372: a damaged pack stops the start there), the window, the device, the BRUTAL splash (378, only on a
  run with a log name, 145), the sound, `Load`, the loop, `Unload`; a crash gives the crash file and the crash window
  (`Core/CrashWindow.cs`: "<program> stopped because of an error", 152). `DataMounts` (186–204): the area packs of `packs/`
  (models, audio, ui, world, data, 170), `classes/`, `Mount.Mods` of the per-user `mods/`, the development folder.
  `ReloadData()` (266–282) remounts and reloads the music, then the game's `Data.Reloaded` listeners. `ConfirmClose()`
  (243) is asked on a close; `Quit()` (233). `Core/ProgramInfo.cs` (name, version, commit), `Core/LogFiles.cs`
  (`<program>-<yyyy-MM-dd_HH-mm-ss>.log`, 10 kept per program, 10 crash files).
- **The window** (`Platform/Window.cs`): `WindowOptions` (9–19: title, width, height, resizable, visible, `MinWidth`,
  `MinHeight`), `Fullscreen` (182), the clipboard. No single-instance lock anywhere in the engine. **The page zoom:**
  `UiHost.PageZoom` (`UI/UiHost.cs` 77–85), 1.2 to the power of a zoom level; the editor moves the level by half steps
  between 0.5 and 3, Electron's default limits, and keeps it in `editor/shell.json` with its window size
  (`Tools/Editor/Shell/EditorView.Keys.cs` 7–30; `Tools/Editor/Settings/EditorSettings.cs` 105–140).
- **Storage** (`Engine/Statventure/Storage/`, `Docs/Assets.md`, "How it works", step 4): `GameStorage` (settings file,
  `saves/`, `mods/`, `OpenInFileManager` on the per-user folder, 72–89; one background writer), `SaveSlots` (named slots,
  `NameProblem` 65–72: one path-rule name, lowercase a–z, 0–9, `-`, `_`, `.`, no `/`, at most 100 characters; `List`,
  `Load` giving the save's reading and, when that isn't readable, the backup's; `Save`; `Delete`), `SaveFile` (the 48-byte
  `SVSV` header; `Missing`, `Readable`, `Damaged`, `Unreadable`), `Settings` (`Get<T>(key, default)`, `Set<T>`, 160–200;
  camelCase, written only on change), `CharacterFile` (`Export`, `Import` with upgrades, 10 MiB both ways,
  `DefaultName` "stat-fick-character.json", 16–19), `DocumentsFolder` (Chromium's lookup), `FolderListing`, `FileBrowser`
  (places: the mods folder, Documents, home, the roots, recent places; listing, typed paths, completion) and
  `RecentPlaces` (the editor's browser is built on them; "later the game's Load Character").
- **Records** (`Engine/Statventure/Records/`): `[Record(name, version)]`, `[Field(n)]`, `[Subtype]`, frozen past versions
  with `Upgrade()`, binary and JSON forms (`RecordSerializer`), committed layout signatures and fixtures; the marks
  `[Kind]`, `[Help]`, `[Unit]`, `[Limits]`, `[Choices]`, `[Colour]`, `[Bulk]`, `[References]`; `[Check]` methods run by
  `RecordChecks.Check(record, data, references)` (`RecordChecks.cs` 59–120). **The engine keeps one record kind per name
  for the process** (`Docs/Packaging.md`, step 6, the manager's Q-C).
- **Mods** (`Packs/VirtualFileSystem.cs`): `Mount.Mods(folder)` (74) opens every mod at each mount and Reload, logging
  `mod <file>` ("Data", 400–405), in `ModFiles`' order (420–429); a mod whose pack can't be opened fails the whole mount.
  `Packs/ModInfo.cs`: the record `mod.info` v1 at `mod/info` (name, author, version, description), "the game never reads
  it for now". The editor refuses to save a mod with any problem: every record entry checked with the data and its
  references (`Tools/Editor/Checks/EntryChecks.cs` 92–120), then the game's music loading on it.
- **The splash and the credits:** `UI/Splash.cs` (2 s, skippable after 1 s, the logo 696 CSS px on `#1a1c1a`),
  `UI/Credits.cs` (`Credits(title, program)`, `Markup`, `Css`, `ViewCss`, `Components`: the dark credits view the
  sandbox and the editor show), `UI/EngineFonts.cs` (Lato regular and bold embedded).
- **UI pieces the game styles:** the tooltip `sv-tooltip`, the select list `sv-select-popup` with `sv-select-option`
  rows and the validation bubble `sv-validation-bubble` (`UI/Dom/UiDocument.TopLayer.cs` 9–10, `UiInput.Tooltips.cs`),
  plain by default.
- **Fonts:** `Text/FontLibrary.cs`: families by name, `SetGeneric` (a generic family mapped to one), fallback families
  (`AddFallback`, 180) and a last-resort `CharacterFallback` (261–289). Nothing draws a symbol as a vector icon: no icon
  code exists in `Engine/Statventure/`.
- **The effect patches** (`Audio/Effects/EffectPatch.cs`, `SoundEffects.cs`; `Docs/Audio.md`, "Sound effects (step 8)"):
  the record `audio.effect` (layers of tones or noise through filters, a level, a voice limit, ranges), entries at
  `audio/effects/<name>` registered when the sound starts (S6-4); `Play(id, volume, pan, pitch)`, `Seed`, no stop (the
  small task "A stop for sound effects"). Twelve fixtures against Chromium's OfflineAudioContext, the same graph stated in
  JavaScript from the same patch JSON (`Reference/audio-fixtures.json`, written by the tests; renders in
  `Golden/audio/<name>.f32.gz` by `Tools/Scripts/make-audio-references.sh`). `AudioSystem.MasterVolume` and
  `EffectsVolume` (157–181) and `Soundtrack.Volume` (music.js's, 87–100), each 0–1. A theme's record has its `Title`
  (`Audio/Music/ThemeData.cs` 25), the old "Current theme" line's text.
- **The stand-in program** (`Samples/Statventure.Sandbox/Program.cs`): the UI demo page "Statventure Sandbox" with an
  About button (the engine's credits view), logging as the game (`LogName = "stat-fick"`, 61), F11 for full screen
  (185–186); options `--capture <png> [--frames N]`, `--capture-splash <png>`, `--about`, `--crash`, `--crash-thread`,
  `--log-visibility` and the cutscene mode `--cutscene jiruan [--dump <folder>] [--volume 0-100]` (`Cutscene.cs`, also
  logging as `stat-fick`). Its project (`Statventure.Sandbox.csproj`) references the stand-in library (23), keeps it out of
  the single-file bundle (`KeepTheDataLibraryBeside`, 27–33) and copies its `Data/` (the tests' fonts, the proof cutscene
  and its models, `audio.pak`; 36–41).
- **The stand-in library** (`Samples/Statventure.Game.Data/`): `StandInItem.cs`, the one kind `stand-in.item` v1 with
  `[Kind("Stand-in item", "stand-in/items")]`.
- **What reads them:** `Tools/Scripts/build.sh` 16 and `build.ps1` 19 (publish the sandbox into `$STATVENTURE_GAME`),
  `play-cutscene.sh` 14–18 (builds and runs the sandbox), `editor.sh` 10 and `editor.ps1` 11 (build the stand-in library
  beside the editor), `Tools/Editor/Game/GameLibrary.cs` 40 (`RepoProject = "Samples/Statventure.Game.Data"`),
  `Tools/Packer/Packer.csproj` 11 and `Program.cs` 25 (the library's kinds),
  `Tests/Statventure.Tests/Statventure.Tests.csproj` 21 (builds the library, not referenced), `Tools/Packaging/Publish.cs`
  158 (the `stat-fick` package is the sandbox's project, assembly `Statventure.Sandbox`, renamed `stat-fick` /
  `Stat Fick.exe`), `listings/stat-fick-*.txt` (the sandbox's `Data/` paths and `bundle Statventure.Sandbox.*` lines),
  `licenses.txt` 58–64 (the stand-in's `cover` lines),
  `release-text.md` 1 and 5 ("Statventure Sandbox stands in for the game"), `end_to_end.py` 284 and 409–415 (the library
  beside the game, built again with another version for the version check), `check_packages_self_test.py` 188 and 276.
  The tests: the editor's game lookup and kind tests (`Tests/Statventure.Tests/Editor/Game/`: `GameTesting.cs` copies the
  library's dev build into scratch game folders; `StandInKindTests.cs`, `GamePartTests.cs`, `GameScreenTests.cs`,
  `GameLookupTests.cs`, `EditorProgramTests.cs`) and `Editor/EndToEnd/GameFolderModTests.cs` (a mod with a `stand-in.item`
  entry); the engine screen and crash fixtures use "Statventure Sandbox" as a program's title (`Core/CrashWindowTests.cs`,
  `CrashWindowPaintReferenceTests.cs`, `Ui/EngineScreensTests.cs`, `EngineScreensPaintReferenceTests.cs`,
  `Core/LogFilesTests.cs`), strings only.
- **The end-to-end flow** (`Tools/Scripts/end-to-end.sh`, `Tools/Packaging/end_to_end.py`; `Docs/Packaging.md`, step 8): 157
  expectations on the two Linux archives, headless, one scratch per-user folder, OpenAL's null device. Of the game it
  uses the stand-in's `--capture-splash`, `--capture`, `--about`, `--crash`, `--crash-thread`, its page (its parchment
  most), its `[Data] mod <player>/mods/ember-rite.pak` line and its loading the mod's sounds.
  `check-packages.sh` runs the packaged game with `--capture <png> --frames 30` and wants a picture of more than one
  colour.
- **The game-screen references** (`Tests/Statventure.Tests/`): 22 captures, 11 states at 1 and 1.65 (sheet, inventory,
  spellbook, classes, skills, passives, settings, companions, shop, map, combat; `Golden/game-*.png`, `game-ui.json.gz`,
  `game-ui@1.65.json.gz`; layers `Reference/scaled/game-layers@1.json`, `@1.65.json`), all from `f45ca7a` (2026-10-30),
  driven by the committed `Reference/game-capture.js` (94 lines) through `Tools/Scripts/make-game-references.sh` (the test
  client, which also copies `styles.css` into `Data/Game/styles.css`), `make-scaled-game-references.sh` and
  `make-layer-references.sh` (`Tools/GameReference`, X11). The tests rebuild each state's DOM from its captured markup
  with the tests' stylesheet copy compiled into a pack (`Ui/GameStylesheets.cs`), compare the layout box for box
  (`Ui/GameLayoutTests.cs`), paint at 24 levels / 3 % with inline SVG masked (`Ui/GamePaintTests.cs` 52), the 1.65 ones
  (`ScaledGameTests.cs` 63), hit tests (`GameHitTests.cs`), layers (`CompositingReferenceTests.cs`) and computed styles of
  the compiled sheet (`CompiledGameStylesTests.cs`). **The stylesheet:** the copy is 3915 lines, the old game's 4184; the
  copy has no `@property`, the old game's has one (`styles.css` 3726, task 7's `--exotic-tone`), and it adds
  `-webkit-text-fill-color` and `background-clip: text` (1114, 3738), `border-right-width` and `:nth-of-type` (a scan of the
  names, 2026-11-13). The engine's compile refuses an at-rule other than @media, @keyframes, @-webkit-keyframes and
  @font-face and an unknown property (`Docs/Assets.md`, step 7, S7-2); `-webkit-text-fill-color` is unknown;
  `background-clip` and `-webkit-background-clip` are ignored on purpose (`UI/Style/Properties.cs` 36).
- **The symbols in the tests:** `Tests/Statventure.Tests/Canvas/TestFonts.cs` 96–102 gives the fonts Chromium picked on the
  reference PC for 20 symbols (↶ ▣ ◈ ◫ ☾ from Noto Sans Math, ↺ ↻ ◇ ☆ ♡ Adwaita Sans, ⌖ ☰ ⚙ ✦ ✧ ✺ Adwaita Mono, ♜ Noto Sans
  Symbols 2, ♟ ⚔ Noto Emoji, ＋ Noto Sans CJK); any other character goes through fontconfig, as Chromium does there.
- **Engine items the screens may meet** ("Small tasks left from earlier milestones"): at 1.65 a select's or button's label
  a device pixel low; a select with its own padding, border and background 6–7 px wider than Chromium's (the old settings
  dialog's selects are such); a dialog over a composited scroller, a status line beside a tall scroller and a stacking
  context on rows turning text grayscale where Chromium's is LCD; the painter painting every row of a scroll container.

### Where the plan was wrong or has moved

Found on 2026-11-13; `Docs/Game-Plan.md`'s GM1 section and "Cross-cutting" corrected the same day, with the answers.

1. GM1, "What is new", and GM1-7 say the symbols become icons "as ♜ ♡ ◈ ▣ ◌ ◫ ◆ already are". The engine draws no
   symbol as an icon: the decision of 2026-10-16 (`Docs/Notes.md`, "User decisions") was never built, and the
   game-screen tests draw those seven with the fonts Chromium picked (`TestFonts.SymbolFonts`), whose table has neither ◌
   (in Lato and STIX) nor ◆ (left to fontconfig).
2. The plan's examples "⚙ ✦ ☾ ↓ ＋ ⚔ ♟": ↓ is in all three Lato faces (only STIX lacks it), so it isn't one. The set is 29
   characters neither font has, plus ◌ by the 2026-10-16 decision: 30 symbols; ⇧ ⌄ ▲ ▼ ◆ ◎ ★ ♛ ✸ are used but on none of
   the 11 captured screens, so not in the tests' table. With Q6 (the theme toggle and New Character left the top bar)
   ☾ and ＋ have no use left: 28 icons.
3. GM1, "How it is checked": "The settings screen ... against the old game's captures (`game-settings`)". The settings
   screen changes (three volumes, the theme, zoom, full screen, Dev Mode, the mods; the battle rows wait for GM5), so it is
   a changed screen: captures for approval, then Chromium rendering the port's markup. So is the top bar since Q6 and Q11:
   neither is compared with the old captures ("Method").
4. "Cross-cutting", "The data library and the editor": "GM1 the save, the settings, the character file" as kinds.
   `settings.json` isn't a record (M9's `Settings`, a JSON object with a version: Q18, Q42, Q43), and the character file is
   the save record's JSON form. GM1 adds one record kind, the save.
5. GM1, "What the old game has": the top bar is `index.html` 65–86 (64–300 is the whole sheet page, `main.shell`), and the
   Settings button isn't in it (23–25, fixed at the bottom right). The survey (`old-game-systems.md` §2.28) puts the ⚙
   "top right": it is bottom right.
6. GM1 and "Cross-cutting", "Dev Mode": "Force encounter" isn't Dev Mode's (`map.js` 582–584); missing from the list:
   another race once one is chosen and the enemy-only races, the appearance step skipped, companions taught any spell,
   the companions' dev tools (above).
7. "Cross-cutting", "The save": the slot list "with the player's own names (Q15)" meets the path rule on slot names (Q4,
   `SaveSlots.NameProblem`): "Ser Alrik" can't be a slot's name as it is (Q1).
8. "Engine items the port will meet": "publish fails when the repo is reached through a symlink" moves from GM18 to GM1's
   step that owns the build and publish scripts (the manager, 2026-11-13; step 2 below).
9. GM1-1's recapture isn't only a run of the three scripts: `Shop.open` now returns unless the hero stands in a
   settlement (`shop.js` 502–506), `Combat.startEncounter`'s third parameter is now `guildObjective` (`combat.js` 6675,
   the capture passes `"normal"`), and today's stylesheet doesn't compile in the engine (`@property`,
   `-webkit-text-fill-color`; `background-clip: text` ignored). "Engine items the port will meet" doesn't list these.
10. `make-scaled-game-references.sh` (6–7) and `make-layer-references.sh` (5) say the tests write
    `Reference/game-capture.js`: no test does; the file is committed.
11. `Docs/Assets.md`, "What exists" (dated 2026-11-02), gives `saveCharacter()` at `app.js` 1753, `exportBackup` 1780 and
    `importCharacterText` 1896 with "about 80 calls": today 1839, 1866, 1983 and 92 call sites. GM1's "93 call sites"
    counts the desktop bridge's `statFickDesktop.saveCharacter(json)` (`app.js` 1871), which writes the backup file.

## Method

GM1 has two references: the old game for what it keeps (its stylesheet, its dialogs and panels, its keys, its texts),
and the engine's own rules for what is new (saves, settings, mods). Every check runs headless and silent, on scratch
per-user folders, never the player's real ones ("How each step is finished", items 1 and 2).

- **Saves and settings: round trips, damaged files and committed fixtures**, as M9's are (`Docs/Assets.md`, "Method"):
  the save record and the character file written and read back in both forms; every state of `SaveFile` and every case
  of Q17 and Q39 through the game's own flow (a damaged save with a good, damaged or missing backup; an unreadable one;
  a stale `.tmp`; a save missing beside its backup); the save's layout signature and a fixture of each version in both
  forms (`Data/Records/`); `settings.json` read with each key missing, of the wrong type, out of range, of a newer version,
  not parsing; the slot naming rule on every case it has (Q1). Nothing is compared with the old game, whose save was
  localStorage.
- **The new and changed screens:** the start screen, the character's page with its top bar (a changed screen since Q6
  and Q11), the in-game menu, the settings screen with the mod list, the slot list, the damaged-save question, the file
  picker, the confirm and prompt dialogs, About, the tooltip, select list and validation bubble styles. Each is built from
  the old game's markup, classes, panels, buttons and dialogs with the port's stylesheet, shown to the user as captures
  first (1×, both themes; GM1-8), and once approved tested against Chromium rendering the port's own markup and stylesheet
  (Tools/PaintReference at 1 and 1.65, 12 levels / 0.2 %, as the editor's screens), the icons' boxes masked (the icons are
  tested on their own).
- **No comparison with the old game's `game-sheet` or `game-settings`.** Both screens changed (Q6, Q11; the settings'
  rows). The top bar's left part (Return to combat, the ✦'s box, the name) would still lay out as `game-sheet`'s, but the
  ✦'s paint differs (always lit; the capture has Dev Mode off, dimmed) and the right part is new; the Chromium fixture of
  the port's markup, on the old stylesheet and the old classes, already checks what the engine draws, and a partial
  comparison would tie the screens' steps to the recapture (1b) for three boxes. Dropped.
- **The game-screen references (GM1-1):** the 22 captures and the layer traces taken again from today's old game, the
  tests' stylesheet copy made today's; every game-screen test must pass at its present limits. A rule the new captures
  meet that the engine lacks is ported from Chromium's source and tested; nothing is held unless recorded as held with the
  user's word; no tolerance is widened.
- **The icons:** each icon's outline against Chromium drawing the same outline as inline SVG at the sizes the game uses
  (M5's path tolerance), its box against the glyph Chromium picked on the reference PC (GM1-P10); the set approved by the
  user as captures first.
- **The mods:** the check at start on good mods and on one mod per kind of break (a damaged header, index or entry; an
  unknown kind; a newer version of a kind; an entry that doesn't read; a value its checks refuse; a reference to nothing;
  a theme that doesn't compile; an unreadable `mod/info`), the log and the list's states, a switched-off mod; the start
  time measured with no mods, one and several.
- **The sounds:** each patch a fixture against Chromium's OfflineAudioContext (the M8 effect fixtures' rule), its render
  committed; each sound approved by the user's ear in a real terminal (a session plays only at volume 0); the wiring by
  tests that count what plays.
- **The randomness service:** seeded draws repeated, `xoshiro256**` and SplitMix64 against their published reference
  outputs; a scripted source handing given draws.
- **The program end to end:** `end-to-end.sh` and `check-packages.sh` on the real program (step 11).
- **Behaviour the old game defines** (its texts, Escape closing the topmost window, focus kept in a dialog, Ctrl+S and
  Ctrl+O, the menu's keys) is ported from its code and tested in the engine; it isn't captured in Chromium (Electron's
  menu and native dialogs have no DOM).
- Every new test shown able to fail (`Tools/Scripts/mutate.py`); every step ends as "How each step is finished" says.

## Design

The design the steps work from, decided (Q1–Q28, Q16b; GM1-P1–GM1-P19).

### The program, the library and the repo

- **The game's program:** `Game/Statventure.Game/` (an `Exe`, assembly `Statventure.Game`; GM1-P1), published as
  `stat-fick` and `Stat Fick.exe` by renaming the executable as M12 does (`Publish.cs`), logging as `stat-fick` (M12-8),
  its window titled "Stat Fick" (Q3 of M12). It takes over the stand-in's place everywhere: `build.sh` and `build.ps1`
  publish it into `$STATVENTURE_GAME` (framework-dependent, S9-A), `Publish.cs`'s `stat-fick` package is its project, and
  its project carries the runtime-publish settings and `KeepTheDataLibraryBeside` the sandbox's has.
- **The data library:** `Game/Statventure.Game.Data/` (assembly `Statventure.Game.Data`, fixed by M12-9 and
  `GameLibrary.AssemblyName`). The game references it; the editor loads it from the game's folder; the packer references
  its project. `GameLibrary.RepoProject` becomes `Game/Statventure.Game.Data`; `editor.sh`, `editor.ps1`, the test project
  and `end_to_end.py` name the new path. In GM1 it holds the save record and the development-only `stand-in.item` (GM1-P3),
  which goes when GM2's first real kind with `[Kind]` arrives.
- **`Statventure.slnx`:** a `/Game/` folder with both; `Samples/` keeps the sandbox; the stand-in project goes.
- **The sandbox stays the engine's test bed** (GM1-P2): not published any more, no reference to the data library, logging
  as `statventure-sandbox`; its cutscene mode (`play-cutscene.sh`), `--log-visibility`, capture, About and crash options
  stay; the engine screen and crash fixtures keep "Statventure Sandbox" as a test string.
- **The packages:** the game's listings lose the sandbox's `Data/` and change their `bundle Statventure.Sandbox.*` lines to
  the game's assembly; `licenses.txt` loses the stand-in's `cover` lines; `release-text.md` stops saying the sandbox stands
  in (its new wording the step's, shown in the draft the user reads). No `classes/` in GM1 (GP-1).
- **Tests:** the game's tests in `Tests/Statventure.Game.Tests/` (GM1-P4), linking the shared infrastructure
  (`PerUserFolders`, `GpuFixture`, `Golden`, `TestFonts`, `ReferencePlatform`, `SystemFonts`) where it is, because the
  editor's tests load the data library through the editor's loader and a process holds one record kind per name;
  `test.sh` and `test.ps1` run both projects; `mutate.py` takes the project. The suite's count is the two projects' sum.

### The seams: the program's parts

Steps 5, 6, 7 and 8 run side by side on step 2's program, and steps 9 and 10 after them. So step 2 writes the program's
application class and, for each part below, a stub in that part's folder with the members outlined here, working in
memory where a stub must work; each later step fills its parts' folders (their files are that step's from then on),
compiles and tests against the other parts' stubs, and edits no other step's folder and not the application class.

| Part (folder) | Filled by | What it is, its members in outline | Its stub (step 2) |
| --- | --- | --- | --- |
| `StatFickGame`, `Program`, `GameParts` (the program's root) | 2 | The `Application` subclass: options, the single instance, the start sequence and the wiring below; `GameParts`, a record of the parts (`Settings`, `Session`, `Mods`, `Ui`, `Random`, the application) handed to each screen | — |
| `Settings/GameSettings` | 5 | `Load(GameStorage)`; `Peek(gameFolder)` → a read-only `SettingsSnapshot` (the window's state, full screen, the mods' switches) read before `Run`, when the engine's storage isn't made yet; typed values `MasterVolume`, `MusicVolume`, `EffectsVolume` (0–1), `Theme` (`GameTheme.Dark`/`Light`), `ZoomLevel`, `Fullscreen`, `Window` (`WindowState(Width, Height, Maximized)`), `DevMode`, `LastCharacter`, `IsModOn(file)`/`SetModOn(file, on)`, `RecentPlaces`/`AddRecentPlace(folder)`; `event Changed(SettingKey)` after a set changes a value | in memory, the defaults, events raised |
| `Session/GameSession` | 5 | `Slots()` (file name, the character's name, last played, readable or damaged), `NextDefaultName()`, `Current` and `CurrentSlot`, `New(name)`, `Open(slot)` → opened or damaged (with the backup's state and time), `UseBackup(slot)`, `StartNew(slot)`, `Delete(slot)`, `Change(update)` (saves), `Leave()`, `Export(file)`, `Import(file)` → a new slot or the problem's text; events `Opened`, `Closed`, `Saved` (the autosave dot), `SlotsChanged` | in memory |
| `Random/GameRandom` | 5 | `IGameRandom` (`Next()` in [0, 1), `Next(n)`), `GameRandom(seed)` (`xoshiro256**` through SplitMix64), `FromSystem()`, `Seed`, `ScriptedRandom(draws)` | a fixed seed |
| `Mods/GameMods` | 6 | `ApplyTo(ApplicationOptions, SettingsSnapshot)` → the options with the engine's mod check set (GM1-P13), the switches taken from the snapshot; `Entries` (file, name, author, version, description, state loaded/off/left out, the first problem and how many more, `SwitchedSinceStart`); `LeftOut`; `Switch(file, on)` | options unchanged (every mod mounts as today), no entries |
| `Icons/GameIcons` | 7 | `Table` (code point → outline, advance, ascent, descent in em), `AddTo(FontLibrary)` (the engine's icon face) | empty, `AddTo` does nothing |
| `Ui/GameUi` | 8 | `Document`, `Host`; `Load(application, settings, data)` (the stylesheets from `ui.pak` in order, the fonts, `GameIcons.AddTo`, the theme class, the page zoom, both following `Settings.Changed`); `Windows` (`Open(layer)`, `Close(layer)`, `CloseTop()`, `Top`); `Commands` (`Register(name, run, enabled)`, `Run(name, argument)`, the keys bound to names); `Dialogs` (`Confirm(question, yes)`, `Prompt(question, value, ok)`, each a task); `Status(text, kind)` with `event StatusShown`; `Sound(name)`; `Update`, `Render` | a bare document with the game's name, commands that log, no windows |
| `Sounds/SoundNames` | 2 | The eight names (`ui-press`, `ui-toggle`, `ui-open`, `ui-close`, `game-saved`, `game-opened`, `ui-refused`, `game-deleted`); their patches are step 4's, in `audio.pak` | final |
| `Screens/Start`, `Sheet`, `Menu`, `About` | 9 | Each `Attach(GameParts)`, registering its commands and building its markup on `GameUi` | does nothing |
| `Screens/Settings`, `Slots`, `Picker` | 10 | The same | does nothing |
| Library `Saves/SaveGame` | 5 | The record `save.game` v1 | a plain record with `Name`, no attributes |

**Who calls whom.** Before `Run` the application class takes `GameSettings.Peek` of this machine's per-user folder (the
window's size for its options; the switches) and passes its options through `GameMods.ApplyTo`; then in `Load` it makes
`GameSettings` on the engine's storage, `GameSession`, `GameRandom.FromSystem()` (its seed logged), `GameUi.Load`, attaches
each screen and runs the command `start-screen`; it applies the three volumes to the sound and full screen and the
window's state to the window, at load and on `Settings.Changed`, writes the window's state back when it changes, and
calls `Session.Leave()` on Quit and on the window's close. `GameUi` follows the theme and the
zoom from `GameSettings`, adds `GameIcons` to its fonts and plays sounds by name (a name with no patch in the data is
logged once and skipped). `GameSession` uses the engine's storage and `GameSettings.LastCharacter`; `GameMods` the
engine's check and `GameSettings`' switches. Screens use the parts and reach each other only through commands; no part
calls a screen. **The commands** and who registers them: `fullscreen`, `zoom-in`, `zoom-out`, `zoom-reset`,
`open-game-folder`, `quit` (8); `open-menu`, `start-screen`, `new-character`, `continue`, `about` (9); `save-character`,
`load-character`, `open-settings`, `open-slots`, `open-slot` (its argument a slot: opens it, or asks the damaged-save
question) (10). A command not registered yet does nothing and is logged, so every step runs with the others missing.
**The keys** (bound by step 8): Ctrl+S `save-character`, Ctrl+O `load-character`, Escape (closes the topmost window,
else `open-menu`), F11 `fullscreen`, the three zoom keys, Quit's key `quit`.

### The records and files GM1 adds

**The save record** (in the data library, GM1-P5): `save.game`, version 1. GM1 holds no character data: the sheet is
GM2's. Version 1 holds:
- `name` (field 1), the character's name as the player gave it (Q1): `[Help("The character's name, as the slot list
  shows it.")]`; its `[Check]`: not empty once trimmed, at most 80 characters, no control characters. Not marked `[Kind]`:
  a save has no place in a mod; marked `[NotAPackKind]` so the editor offers no New entry of it (Q26: step 5 found the
  editor offered one for every plain kind).
- Nothing else: the theme and Dev Mode are settings (GM1-4, GM1-13), the random state isn't saved (GM1-5), the slot's
  last write time is its file's.

Its layout signature and a fixture in both forms are committed (`Tests/Statventure.Game.Tests/Data/Records/`), as every
record's are (Q13 of M9). **How later milestones extend it:** each adds its part as a new field, a new version of
`save.game` with the past version frozen (`SaveGame.V1` …), a hand-written `Upgrade()` and a committed fixture of each past
version, so a save made at any milestone loads at the next. GM2's part (the hero) is version 2, its `Upgrade()` from
version 1 giving a new hero's defaults (no race yet: the race picker opens, as the old game's `needs-race`). The parts are
records of their own kinds, each versioned on its own (the `$types` table).

**The slot's file** (Q1): named from the character's name by one rule: lowercased, letters and digits kept, every run of
other characters one `-`, accents taken off as `ImportNames.Fit` does, cut to the path rule's limits, `character` when
nothing is left, `-2`, `-3` … added when taken; the name itself kept whole in the save. Two characters may share a name;
their files differ. Its edge cases as step 5 decided them: "How it works", step 5 (`_` and `.` are other characters too,
letters outside a–z after the accents' removal too; a Windows device name starts at `-2`).

**The character file** is `save.game`'s JSON form (indented, its `$types` table first), written by `CharacterFile.Export`
and read by `CharacterFile.Import`, which upgrades an older version and refuses a newer one or another kind; the game then
runs the record's checks (G4) and refuses a file they refuse. Any file that isn't one, an old game's file among them,
gets "That file is not a valid character save" (Q23). GM1's file: `{ "$types": { "save.game": 1 }, "name": "Ser Alrik" }`.

**`settings.json`**, version 1 (camelCase, as `Settings` writes; GM1-P6). Each key read with its default; a value of the
wrong type gives the default (Q43 of M9), one out of range likewise (logged); later milestones add keys without a new
version (unknown keys are kept and missing ones default), a new version only when a key's meaning changes, with its
upgrade.
- `masterVolume` 1, `musicVolume` 1, `effectsVolume` 0.6 (Q22), each 0–1; applied to `AudioSystem.MasterVolume`,
  `Soundtrack.Volume` and `AudioSystem.EffectsVolume`. The music volume is kept and applied though no theme plays in GM1
  (Q15).
- `theme`: `"dark"` or `"light"`, default `"dark"` (GM1-4).
- `zoomLevel`: a half step between Electron's limits (0.5 to 3 times), default 0 (100 %), as the editor's.
- `fullscreen`: default false; `window`: `{ "width", "height", "maximized" }`, absent at first (1440 × 950) (Q19).
- `devMode`: default false (GM1-12, GM1-13).
- `lastCharacter`: the slot Continue opens, or absent.
- `mods`: `{ "<file name>": { "on": false } }` for each mod switched off (Q3).
- `recentPlaces`: the file picker's last ten folders (GM1-P15).
- GM5 adds the battle settings (`battlePace`, `combatAnimations`, `battleUi`, `battleSpeed`, the old names).

### The program's flow

**Starting.** `stat-fick` checks that no other instance runs on this per-user folder; if one does, it asks that one to
bring its window forward and ends (the old shell's single instance, `main.cjs` 17–19; GM1-P14: on Wayland the compositor
may only flash it). Then `Application.Run`: the run's log, the per-user folder, the window's state read from
`settings.json` (Q19), the mods checked and the good ones mounted ("Mods" below), the window (1440 × 950 the first time,
minimum 820 × 600, "Stat Fick"), the device, the BRUTAL splash, the sound with the three volumes and no theme started
(Q15), then the game's `Load`: one game document (GM1-P9) with the port's stylesheets from `ui.pak`, `html.dark-mode` set
when the theme is dark, the page zoom, and the start screen.

**The start screen** (GM1-2, Q16, Q16b, Q10): the game's name and six buttons in the old game's look (captures first):
Continue, New character, Load, Settings, Quit, About. Continue is shown greyed (disabled) while `lastCharacter` names no
slot that is there. A mod left out at this start is named on it ("1 mod was left out: Ember Rite. Settings › Mods tells
why."). Settings opens the settings screen over it, About the About dialog, Quit ends; the ⚙ button isn't shown on it.
Escape opens the in-game menu here too, without the items that need an open character (Save Character, Start screen).

**New character** (Q1, Q2): a prompt dialog "Name your character" with the next free "Character <n>" filled in; its OK
makes the slot (the name in the save, the file named from it), saves it at once, sets `lastCharacter` and opens the
character's page. An empty name is refused (`ui-refused`). GM2 puts the race picker after it.

**Opening a character** (Continue, the slot list's Open, New character, Load Character): its slot is read; a readable one
opens; a damaged or unreadable one gives the damaged-save question; `lastCharacter` names it.

**What the player sees in a character in GM1** (Q5): the old page's frame: the top bar (below) over an empty parchment
page where GM2's sheet goes, the status line under the top bar for the file messages, the ⚙ Settings button at the bottom
right. Its later parts are inert until their milestones (GP-3): the world clock shows "Day 1 · 08:00" (GM4 runs it),
"Return to combat" stays disabled (GM5).

**Saving.** The game keeps the open character's `save.game` value; every change saves it to its slot (`SaveSlots.Save`:
serialized at once, written by the background writer, the latest state winning) and lights the autosave dot ember for
350 ms, as `saveCharacter` did after every change; leaving a character (the start screen, another character, Quit, the
window's close) saves it; the engine's teardown writes what is pending (S9-B). In GM1 the only changes are making and
loading a character; GM2 on, every change of the sheet.

**The slot list, Load** (Q17): a window listing every slot, newest first, its name and when it was last played
("13 November 2026, 14:03", fixed English, invariant globalization), with Open and Delete; Delete asks once ("Delete
Ser Alrik? Its save and its backup are removed.") and removes the save and backup (Q41 of M9; files kept aside stay). A
slot whose files can't be read shows its file's name and "damaged"; one whose save can't be read but whose backup can
shows the backup's name and "damaged" (the manager, confirming step 5's reading, 2026-11-13; the session's `SlotInfo`
says where the name came from). A "Load Character…" button in it loads a character
file into a new slot, the same as the menu's Load Character and Ctrl+O (Q24), so the start screen's Load reaches it too.

**The damaged-save question** (Q17 of M9, Q21): "Ser Alrik's save is damaged." with "Use the backup (saved <time>)" (only
when the backup reads), "Start new" (a new character in this slot, the name kept: the backup's when it reads, else the
slot's file name, "ser-alrik", with no prompt, Q25) and "Back" (nothing changes). Either of the first two writes the
slot, which keeps the damaged file aside (Q39 of M9). The question's name is the same one (the backup's, else the file's
name: the session's `OpenResult.Name`).

**Save Character and Load Character** (the menu, Ctrl+S, Ctrl+O, Q6; Load Character also the slot list's button, Q24):
the in-game file picker (GM1-P15). Save: in
Documents (`DocumentsFolder`) with `stat-fick-character.json` (M9), a name field, folders and `.json` files, an existing
file replaced after a confirmation; written by `CharacterFile.Export`; the status line says "Character saved" or
"Character could not be saved" (the old texts). Load: `.json` files; the file read, upgraded and checked; its character
opened in a new slot under the name it holds, a number added if that is taken (Q9), the one open before left saved in its
slot; "Loaded <file>", or "That file is not a valid character save" for any file that isn't one (Q23). A refusal shows
where the action was taken: on the page's status line, on the slot list's own, or, from the start screen (Ctrl+O,
Escape's menu), on a status line of the start screen's own under its buttons (in their captures). The picker's places:
Documents, home, the drives or `/`, recent places.

**The in-game menu** (Q7, Q8, Q18): opened by the top bar's Menu button and by Escape when no window is open (Escape still
closes the topmost window first, as the old game). Its items, the old menu's in its order and words (the View items and
Quit with Electron 44's role labels and keys, read by step 8), Reload left out, Settings and Start screen added: Save
Character (Ctrl+S), Load Character (Ctrl+O), Open game folder (the per-user folder, M9), Settings, Start screen (saves and
goes back), the zoom's reset, in and out, full screen, About, Quit.

**The settings screen**: the old settings dialog grown, its header and the "Sound volume" row's look kept: the master,
music and effects volumes (what the music row says with no theme playing, Q15, is part of its captures), the theme (here
only, Q6), the zoom, full screen, Dev Mode (off; what it unlocks said in a line), and the mods with their switches ("Mods"
below; where a switch was changed this run the list says it takes effect at the next start, Q18). Every change applies at
once, the mods' at the next start, and is written to `settings.json`. Its layout is shown as captures (GM1-8). The battle
rows join it in GM5.

**About** (Q20, from the start screen and the menu): a dialog in the game's parchment look holding the engine's credits
(`Credits.Components`, BRUTAL's copyright line and `NOTICE.txt`, the pointer to `licenses/`, the program's version), the
same text the credits tests check.

**The window:** 1440 × 950 the first time, minimum 820 × 600; then as the player left it (Q19); full screen by F11, the
menu or the setting; the page zoom by Ctrl+=, Ctrl+-, Ctrl+0, the menu or the setting, in Electron's half levels, applied to
every game document (`UiHost.PageZoom`).

**Keys:** Ctrl+S and Ctrl+O (`main.cjs` 82–83); the zoom items', full screen's and Quit's keys as Electron 44's roles give
them on each system (expected Ctrl+0, Ctrl+= or Ctrl+Plus, Ctrl+-, F11, Ctrl+Q on Linux; step 8 reads them from
Electron's source and records them); no Reload and no Ctrl+R (Q18); Escape; Tab kept inside an open dialog and Enter and
Space on buttons (the engine's).

**Quit and close:** both save the open character and end at once; no question (as the old close, `main.cjs` 73–79).
`ConfirmClose` agrees at once.

### The top bar

A changed screen (Q6, Q7, Q11): the old markup (`index.html` 65–88) built by the game, in the old game's words and
classes, without the theme toggle and the three character buttons, with the Menu button:
- "Return to combat": disabled (GM5).
- The ✦ (Q11): the brand mark only, always lit (the old mark's full look, never the dimmed one), not a button any more:
  no pointer, no hover tilt, not focusable, no tooltip, nothing to do with Dev Mode.
- "Stat Fick"; the world clock; "Autosaved locally" with its dot.
- "Menu" (Q7), styled as the top bar's old buttons.
Below it the status line (`#backup-status`) for the file messages and `#day-status` (GM4). Shown as captures, then a
Chromium fixture of the port's markup.

### Dialogs, tooltips and the window manager

One manager for every window of the game, as the old `.modal-layer`s behave: open and close a layer, `body.modal-open`
while one is open, Escape closing the topmost, focus moved into it and kept there, the focus given back on close, the
sounds of opening and closing. On it, the two dialogs every later milestone needs in place of `window.confirm` (12 uses)
and `window.prompt` (5 uses): a confirm dialog (a question, a "yes" button named for the action, Cancel) and a prompt
dialog (a question, a text field, OK, Cancel), in the old dialogs' look (captures first). The port's stylesheet also gives
`sv-tooltip` the leather combat tooltip's look, `sv-select-popup` a parchment list in the game font and
`sv-validation-bubble` the game's look (Decisions for M5), shown as captures.

### Mods: the list and the check at start

- **When:** at the start only (Q18: no Reload), before the data mounts: each `.pak` directly in `mods/`, in the file-name
  order mods mount in (M9 Q30). A mod switched off in the list (Q3) isn't checked or mounted. A mod added, removed or
  switched takes effect at the next start. The engine calls the game's check at every mount it makes; the engine's
  `ReloadData()` stays, but the game never calls it.
- **The check** (Q4: what the editor would refuse to save, checked as the editor checks a mod: alone over the game's
  packs; one shared check in the engine's `Packs/` that the editor's save uses too, GM1-P13): the pack opens (its header and
  index hash); every entry reads (each entry's hash, Q6 of M9); every record entry reads as a kind the game knows (the
  engine's and the data library's) at a version it knows; every record passes `RecordChecks.Check` with the game's packs
  and the mod as its data and the references resolved over them; the loaders' own checks the editor runs pass (the music
  loading with the mod, G5); `mod/info`, when there, reads. Any problem leaves the whole mod out. A mod that needs another
  mod's entries fails as the editor refuses it (P18's known limit of M11). The development folder isn't a mod.
- **The cost:** reading every entry of every mod at start; measured with no mods, one and several; if noticeable, the
  manager asks the user again (Q4). **Measured by step 6 and accepted by the user (Q27, 2026-11-14, "Keep the full
  check"):** +8 ms with no mods, +73 ms with one small mod, +232 ms with five, +111 ms with one of 2,056 entries (the
  wall time, Release, medians), all before the window opens ("How it works", step 6). **Measured again on step 1a's
  packs (2026-11-14; the game's stylesheet in `ui.pak` is checked with every mod):** −6 ms with none, +494 ms with one
  small mod, +1,440 ms with five, +515 ms with one of 2,056 entries; gone to the manager after Q27's answer.
- **The editor's Import** (Q28, 2026-11-14): a record's JSON form of a kind marked as not a pack kind (the save) is refused,
  so nothing in the editor makes a save entry; the check itself treats such a kind as any kind the game knows (a
  hand-made mod holding a save isn't broken for it; the game ignores the entry).
- **The list** (the settings screen): each mod in mounting order: its name (`mod/info`'s, else the file name without
  `.pak`), its version and author, its description, its file name, its switch, and its state: loaded, off, or left out
  with its first problem in the editor's words and how many more; "takes effect at the next start" beside a switch changed
  this run.
- **The log:** `[Mods] <file>: loaded (<name> <version> by <author>)`, `[Mods] <file>: off`, `[Mods] <file>: left out:
  <problem> (and n more)`; the file system's `[Data] mod <file>` line stays for each mod mounted.
- **The start screen** names the mods left out at this start (Q10).

### The port's stylesheet in `ui.pak` and the screens' markup

- **The stylesheet** (GM1-P8): `Assets/UI/styles/game.css`, today's `old-game/styles.css` with its five `@font-face` urls
  as `ui.pak`'s data paths (`ui/fonts/lato-regular.ttf` …, the rewrite `GameStylesheets.CompilableText` makes), owned by
  the port from then on; packed as `ui/styles/game`. Each new screen's rules in a sheet of its own beside it
  (`start.css`, `menu.css`, `about.css`, `settings.css`, `slots.css`, `picker.css`, `dialogs.css`, `controls.css`), packed
  at `ui/styles/<name>` and added after `game` in a fixed order. `Tools/Scripts/make-ui-pack.sh` packs
  `Assets/UI/styles/*.css` with the fonts. It compiles only once the engine handles today's sheet (step 1a).
- **The tests' stylesheet copy** (`Tests/Statventure.Tests/Data/Game/styles.css`) stays the reference the old game's
  captures were made with; it isn't the port's stylesheet.
- **The markup** (GM1-P7): built in C# (2026-10-16), the old pieces (the top bar, the settings dialog's frame) kept as the
  old HTML text so they can be compared with `index.html`; the new screens' markup the old dialogs' structure and classes.
- **Fonts:** the document's fonts are `ui.pak`'s (its `@font-face` rules); `serif` maps to STIX Two Text and `sans-serif`
  to Lato (no system fonts, GM1-7), Lato and STIX each the other's fallback; the icons' code points drawn by the icon face
  before any font (the manager, 2026-11-13; "The symbols as vector icons").

### The recapture (GM1-1)

- **The old game is frozen for it** (the user, 2026-11-13): nothing in `old-game/` changes until the manager gives the
  user the ok, once step 1b is on `main`; a change the recapture itself needs goes to the manager first.
- **What is captured:** the same 11 states at 1 and 1.65 with `game-capture.js`, adapted to today's old game where it no
  longer drives it (the hero placed in a settlement before `Shop.open("blacksmith")`; `startEncounter`'s arguments; the
  map's waits for task 8's charting); the layer traces at 1 and 1.65; the tests' stylesheet copy byte for byte today's.
  On the PC under a headless X server (the 1.65 captures need X11) or in the cloud under Xvfb; never on the user's screen.
  The capture's character stays the same (a level-30 human with a longsword and full plate, Dev Mode off, light theme).
- **What it can break:** the old game's markup, scripts and stylesheet changed with tasks 2–11, so the tests meet markup
  and rules they never drew. Known before running, and made first (step 1a): the compile refuses `@property` and
  `-webkit-text-fill-color`, so every game-screen test would fail; `background-clip: text` with a transparent fill (the
  Exotic wave, the Sparkling tag) is ignored, so such text would show as its gradient box.
- **How a failure is handled:** the rule found in Chromium's source and ported, tested at its own fixture and shown able
  to fail; a rule no captured screen shows is ported as far as the computed-style test needs and its paint left to the
  milestone whose screen shows it, recorded; a difference not ported is held only with the user's word, recorded with its
  cause; no limit is widened. The tests read from `old-game/` the fonts, `classes/Black Knight.json` and the images the new
  captures name.

### The symbols as vector icons (GM1-7)

**The set: 28 icons**, the 29 characters neither font has and ◌ (decided 2026-10-16), less ☾ and ＋, whose only uses were
the theme toggle (`index.html` 78, `app.js` 1634) and New Character's button (`index.html` 82), which left the top bar
(Q6). Every other one is shown somewhere in the game, if not in GM1 (GM1 itself shows ⚙ and ✦). Where each is used and
what Chromium drew it with on the reference PC (the fonts of `TestFonts.SymbolFonts`; the rest found by step 3 in the old
game's page: "How it works", step 3). The old game shows them in 83 places (`Tools/IconSheet/uses.json`):

| Icon | Name | Used in | Chromium's font |
| --- | --- | --- | --- |
| ↶ | anticlockwise top semicircle arrow | combat.js | Noto Sans Math |
| ↺ ↻ | open circle arrows | index.html (↻ 5) | Adwaita Sans |
| ⇧ | upwards white arrow | map.js (6) | Adwaita Sans (fallback) |
| ⌄ | down arrowhead | companions.js (3) | Adwaita Sans (fallback) |
| ⌖ | position indicator | index.html | Adwaita Mono |
| ▣ ◫ ◈ | squares and diamond | index.html, app.js, companions.js | Noto Sans Math |
| ▲ ▼ | triangles | styles.css | Liberation Sans Bold: not a fallback, `--font-ui`'s Arial |
| ◎ | bullseye | styles.css | Noto Sans Math (fallback) |
| ♛ | queen | styles.css | Noto Sans Symbols 2 (fallback) |
| ✸ | eight-pointed star | styles.css | Adwaita Mono (fallback) |
| ◆ | black diamond | styles.css, app.js, companions.js, map.js | Adwaita Sans (fallback) |
| ◇ ☆ ♡ | white diamond, star, heart | index.html, app.js, companions.js | Adwaita Sans |
| ◌ | dotted circle | index.html, companions.js | Liberation Serif: not a fallback, the serif of "Georgia, serif" |
| ★ | black star | app.js, map.js, shop.js | Adwaita Sans (fallback) |
| ☰ ⚙ ✦ ✧ ✺ | trigram, gear, stars, asterisk | index.html, app.js, styles.css, combat.js | Adwaita Mono |
| ♜ | rook | app.js, index.html | Noto Sans Symbols 2 |
| ♟ ⚔ | pawn, crossed swords | companions.js, index.html, combat.js | Noto Emoji |

- **None is drawn by the engine before step 7**; all 28 are new. Their outlines and boxes are in `Assets/UI/icons/`
  (step 3, approved by the user on 2026-11-13; "How it works", step 3).
- **How an icon is drawn** (Q12, GM1-P10): an outline (filled shapes with holes, SVG path data in `Assets/UI/icons/`,
  compiled into the game's table), drawn by the engine wherever the character appears in text, at the text's size, in the
  colour the stylesheet gives the text, as a glyph is (selection, opacity, transforms alike), through an icon face; its
  shape the symbol's figure as an inked shape in the game's style (the "Engraved" style, the user's pick). Its box
  (advance, ascent, descent) is the glyph Chromium picked on the reference PC, so every line lays out as the old game's did.
- **Every code point of the icon table is drawn by the icon face before any font** (the manager, 2026-11-13; GM1-P10
  changed in that one point, which said "the last character fallback"): ◌ stays an icon (the user's decision of
  2026-10-16 names it) and is in Lato and STIX Two Text, which a last fallback would never get past. Step 7 builds it so.
- **The game-screen tests and the real game:** the old game's captures keep being tested with Chromium's fallback fonts
  (`TestFonts.LibraryWithSystemFallback`), the engine against Chromium on the old markup; the real game has no system
  fonts and draws the icons. The port's screens' Chromium fixtures mask the icons' boxes; the icons themselves are checked
  on a sheet where Chromium draws each outline as inline SVG and the engine draws the icon.
- **Approval:** a sheet of captures, every icon at the sizes and colours the game uses, in both themes, beside Chromium's
  glyph from the old game, shown to the user before the engine work (step 3; made by `Tools/Scripts/make-icon-sheet.sh`).

### The randomness service (GM1-5, GM1-P11)

One service per run that every gameplay roll draws from, in the game program: `xoshiro256**` seeded through SplitMix64 from
one 64-bit seed, `Next()` the top 53 bits of a draw as a double in [0, 1), as `Math.random`, so a port reads like the old
line (`Math.floor(Math.random() * n)` becomes `(int)Math.Floor(random.Next() * n)`), with the few helpers the old code
repeats. Seeded once at start from the system, the seed logged; nothing saved, so every load draws afresh (GM1-5). Tests
seed it, or give it a scripted source that hands the exact draws an old check's stub gives and fails when it runs out, so a
rule can be replayed against the old game draw for draw ("Cross-cutting", "Randomness and seeding"). Art keeps its
per-object seeds; sound effects keep `SoundEffects.Seed`. GM1 has no roll; the service is for GM2 on.

### The sounds (GM1-6, GM1-11, Q13, Q14)

- **The list:** `ui-press` (a button pressed), `ui-toggle` (a switch or box), `ui-open` (a window opening), `ui-close` (a
  window closing), `game-saved` (a character file written), `game-opened` (a character opened: Continue, Open, New, Load
  Character), `ui-refused` (an action refused: a file that isn't a character, an empty name), `game-deleted` (a character
  deleted). `ui-press` also plays once when a volume slider is let go, at the new level. Nothing for hovering or for the
  autosave.
- **Their character:** paper and ink: a dry quill tick, a short paper rustle, a page turn, a low seal thump, a muted
  double knock; soft (GM1-11), each under 0.3 s, made for the effects volume's first-start 60 % (Q22).
- **As patches:** `audio.effect` records at `audio/effects/<name>`, their sources as JSON forms in
  `Assets/Audio/effects/`, packed into `audio.pak` by the rule of GM1-P16, registered when the sound starts (S6-4).
- **Wiring:** the window manager plays `ui-open` and `ui-close`; one listener on the game's documents plays `ui-press` for
  a click on an enabled button that opens no window and `ui-toggle` for a changed switch, box or select; the game's actions
  play the event sounds. One sound per action. Through the effects bus (`EffectsVolume`), dry.
- **A stop for sound effects** (the small task) isn't needed: each sound ends by itself, and a fifth quick press takes over
  the oldest voice of its patch (4 a patch).
- **Approval:** the user listens with `Tools/Scripts/play-sounds.sh [name …] [--volume 0-100]` in a real terminal;
  sessions and scripts play only at volume 0.
- **Approved (the user, 2026-11-13, by ear through `play-sounds.sh --candidates`, step 4's three candidates a, b, c
  per sound, at the first start's effects 60 %; "b c c c a c c c"; no changes asked, the levels as heard):**
  `ui-press` b (a bright, short scritch), `ui-toggle` c (one soft, low stroke), `ui-open` c (a crisp, short crinkle),
  `ui-close` c (a crisp, short fold), `game-saved` a (the low seal thump), `game-opened` c (the heavy page),
  `ui-refused` c (the tight, muted double knock), `game-deleted` c (a sheet crumpled). Their patches: "How it works",
  step 4; `Docs/Audio.md`, "The game's sound effects".

### End to end, the package check and Windows

- **`check-packages.sh`:** its rules unchanged; the game's listings made again (step 2); the packaged game's `--capture`
  draws its start screen (more than one colour; before step 9, the game document's parchment page).
- **`end-to-end.sh`** (step 11): the stand-in's expectations become the game's: the splash; the start screen on a first
  start (Continue greyed); the settings screen and the mod list (`--open settings`, GM1-P12); About (`--about`); the crash
  window (`--crash`, `--crash-thread`); the editor's mod (with its `stand-in.item` entry, GM1-P3) checked and mounted
  (`[Mods] ember-rite.pak: loaded`, `[Data] mod …`); a broken mod (a byte of its index flipped; one with an unknown kind)
  named and left out in the log, the list and the start screen's line; a switched-off mod not mounted; a slot, a
  `settings.json` and a character file made by an in-process test of the game's test project built with the packages'
  version, then the packaged game started on them: Continue enabled, the slot opened and written back on quit; no theme
  started (the log); the player's real folders unchanged.
- **"Windows check"** (`Docs/Engine-Plan.md`): item 1 (`build.ps1` publishes the game; `test.ps1` runs both test
  projects), 2b (saves, character files and "Open game folder" through the game's screens), 2d (the game, not the sandbox,
  in `$STATVENTURE_GAME`), 5 (minimizing with the game, or the sandbox's `--log-visibility`), 8g and 9i (the packaged game
  mounting a checked mod, the list showing it), 9a (the start screen, titled "Stat Fick", after the splash), 9c (the game's
  crash triggers) change; step 11 rewrites them.

## The questions as asked (Q1–Q28, answered 2026-11-13 and 2026-11-14)

Each as the manager asked it with the question tool (2026-11-13), in asking order, the recommended option first; its
answer after it and in "The detail round" above.

**Q1. A character's name and its save file.** M9 gave named slots with "free names the player gives" (Q15), but the
engine's slot names follow the pack path rule: lowercase a–z, 0–9, `-`, `_`, `.`, at most 100 characters
(`Storage/SaveSlots.cs` 65–72). The old hero had no name (one character, one save); companions' names take up to 80
characters of any kind.
- (a) **Any name, kept in save:** the player types any name up to 80 characters; the save holds it and the list shows it;
  the file is named from it by a rule ("Ser Alrik" → `saves/ser-alrik.dat`, a second one `ser-alrik-2.dat`). *Recommended:*
  the player's names exactly as typed, the folder still readable.
- (b) **Names are file names:** only lowercase letters, digits and `- _ .` are accepted; the field refuses the rest.
- (c) **Names encoded in files:** any name, written into the file's name by a reversible rule (capitals and spaces as
  escapes, as the music's names are); nothing stored in the save.
- **Answer (the user, 2026-11-13): (a).**

**Q2. When a new character gets its name.** The old New Character asked twice and cleared the one character, then put
the focus on the race button (`app.js` 2141–2208); with slots nothing is cleared. GM2 puts the race picker after this.
- (a) **Asked before the sheet:** a small dialog asks for the name, the next free "Character 1" filled in, then the
  character opens. *Recommended:* every slot has the player's name from the start.
- (b) **Named automatically:** "Character 1", "Character 2" …; renamed later in the slot list (which then needs Rename).
- **Answer (the user, 2026-11-13): (a).**

**Q3. What the player can do in the mod list.** GM1-10 gave a mod list in the settings and a check at start. Today every
`.pak` in `mods/` loads, in the files' name order; the only way to leave one out is to move its file.
- (a) **Switches to turn mods off:** each mod has an on/off switch, kept in `settings.json`; the order stays the files'
  name order. *Recommended:* a mod can be left out without touching the folder, and the order rule modders rely on stays.
- (b) **A list to read only:** each mod and its state; leaving one out means moving its file.
- (c) **Switches and an order:** switches, and the order set in the list (kept in `settings.json`), the name order only
  the default.
- **Answer (the user, 2026-11-13): (a).**

**Q4. What makes a mod "broken".** Today a mod whose file can't be opened stops the game at start, and a part the game
can't read stops it only when that part is used (M9; G2). The editor refuses to save a mod with any problem: an entry
that doesn't read, a kind the game doesn't know or a newer version of one, a value its kind's checks refuse, a reference
to an entry that isn't there, a theme that doesn't compile (checked over the game's own packs).
- (a) **What the editor refuses:** at every start and Reload each mod is checked as the editor checks it, alone over the
  game's packs, every entry read and checked; any problem leaves it out. *Recommended:* one rule for modders in the editor
  and the game, and a broken mod never reaches play. (The added start time is measured; if it is noticeable you are asked
  again.)
- (b) **Only unreadable files:** a mod is left out only when its file is damaged or holds kinds the game doesn't know;
  values and references are checked when used, as today.
- **Answer (the user, 2026-11-13): (a).**

**Q5. What GM1 shows after Continue or New character.** The character sheet is GM2's; GM1 has nothing to play. The plan's
rule (GP-3) is that a later milestone's parts show inert until then. The old page is the top bar over the sheet, with the
⚙ Settings button at the bottom right.
- (a) **Top bar, empty page:** the old top bar (its later parts inert: the clock at "Day 1 · 08:00", "Return to combat"
  disabled) over an empty parchment page, and the ⚙ button; GM2 fills the page. *Recommended:* nothing built twice, and
  the top bar is checked against the old game from GM1 on.
- (b) **The whole old sheet, inert:** the old sheet's whole page with its defaults, every control inert until GM2.
- (c) **No sheet yet:** Continue and New character make and save the character but stay on the start screen until GM2.
- **Answer (the user, 2026-11-13): (a).**

**Q6. The top bar's buttons.** The old top bar has the theme toggle ☾ and "Save Character", "Load Character", "New
Character". The theme is now a setting (GM1-4), there is a start screen (GM1-2) and an in-game menu. The old New Character
asked twice because it cleared the only character; with slots it clears nothing.
- (a) **Keep them all, working:** the toggle flips the theme setting (the same as Settings'); Save and Load Character open
  the file picker; New Character saves this one and starts a new one without asking. *Recommended:* the top bar stays the
  old game's and is checked against it.
- (b) **Drop the theme toggle:** the theme only in Settings; the three character buttons stay.
- (c) **Name and status only:** the toggle and the three buttons go (they are in Settings, the menu and the start screen).
- **Answer (the user, 2026-11-13): (c), not the recommendation (a): name and status only; the top bar a changed screen.**

**Q7. How the in-game menu opens.** The old Electron menu bar (File, View, Help) was always above the page; it becomes an
in-game menu (Decisions for M5). In the old game Escape closes the topmost window and does nothing on the bare page; the
⚙ Settings button is at the bottom right.
- (a) **Menu button and Escape:** a "Menu" button at the top bar's right end, and Escape when no window is open.
  *Recommended:* it stands where the old menu bar was, and Escape is what players expect.
- (b) **The ⚙ becomes Menu:** the bottom-right button opens the menu (Settings its first item), plus Escape; the top bar
  unchanged.
- (c) **Escape only:** no button.
- **Answer (the user, 2026-11-13): (a).**

**Q8. What the menu holds besides the old menu's items.** The old items: Save Character, Load Character, Open game folder,
Quit, Reload, the zoom's reset, in and out, full screen, About. With several characters the player also needs a way to
switch, which the old game didn't have.
- (a) **Add Settings, Start screen:** also Settings, and "Start screen" (saves this character and goes back, where Load and
  New character are). *Recommended:* one place for choosing characters, a short menu.
- (b) **Add Settings, Characters:** also Settings, and "Characters…", the slot list itself (open, new, delete) over the
  sheet.
- (c) **The old items only:** switching characters needs a restart.
- **Answer (the user, 2026-11-13): (a).**

**Q9. Which slot Load Character fills.** The old Load Character replaced the one character with the file's. With slots it
needs a slot; M9 left it to the port's screen.
- (a) **A new slot:** the file's character becomes a new slot under the name it holds (a number added if that is taken)
  and opens; the one open before stays in its slot. *Recommended:* nothing is ever overwritten.
- (b) **Over the current one:** it replaces the character open now, after a confirmation, as the old game did.
- (c) **Asked each time:** new slot or replace.
- **Answer (the user, 2026-11-13): (a).**

**Q10. Where a mod left out is told.** GM1-10: "a broken one named and left out". The old game had no mods.
- (a) **Line on start screen:** "1 mod was left out: Ember Rite. Settings › Mods tells why."; the list says why, the log
  has the details. *Recommended:* the player sees at once that a mod didn't load.
- (b) **Only in the mod list:** the list and the log.
- (c) **A window at start:** a window after the splash lists them, to be closed.
- **Answer (the user, 2026-11-13): (a).**

**Q11. The old ✦ button.** GM1-3 disabled the ✦ Dev Mode switch beside the game's name and left whether it stays to be
asked if it shows something different. In the old game its look shows Dev Mode: lit when on, dimmed and grey when off,
with the tooltip "Enable Dev-mode" or "Disable Dev-mode".
- (a) **Inert, shows Dev Mode:** it stays as the brand mark, not clickable, lit or dimmed as Dev Mode is on or off, its
  tooltip "Dev Mode is on (switched in Settings)" or "off". *Recommended:* the top bar keeps the old look in both states,
  and Dev Mode shows at a glance.
- (b) **Inert, always lit:** the brand mark only, always lit, no tooltip.
- (c) **Removed:** the name stands alone.
- **Answer (the user, 2026-11-13): (b), not the recommendation (a): the brand mark only, always lit, no tooltip.**

**Q12. The icons' colour and drawing.** GM1-7: symbols outside Lato and STIX become vector icons "in the game's ink". 30
symbols are concerned (⚙ ✦ ☾ ♜ ♡ ⚔ ＋ …). In the old game each takes the text's colour from the stylesheet (the sage ⚙
of the Settings button, the ✦ in the leather's accent colour, an ember ♜) and its shape from whatever font the system
had.
- (a) **Text's colour, inked shapes:** each icon the same figure as its symbol, drawn in the game's ink style, in the
  colour the stylesheet gives the text; approved as a sheet of captures. *Recommended:* every colour the old game gives a
  symbol stays, and the shapes join the game's look.
- (b) **Always the ink colour:** drawn in the ink colour (#130e0b) whatever the text's colour.
- (c) **Plain solid glyphs:** solid silhouettes like a font's, in the text's colour.
- **Answer (the user, 2026-11-13): (a).**

**Q13. GM1's sounds.** GM1-6: each milestone's sounds are asked with its questions; GM1-11: soft sounds for buttons and
windows besides game events. The old game has no sound effects; each sound is made as a synthesized patch and approved by
your ear.
- (a) **Eight interface sounds:** a button pressed; a switch or box changed; a window opening; a window closing; a
  character file saved; a character opened; an action refused; a character deleted; the press also played when a volume
  slider is let go, at the new level. *Recommended:* every action GM1's screens have, each soft and short.
- (b) **Four basics:** a button pressed, a window opening, a window closing, an action refused.
- **Answer (the user, 2026-11-13): (a).**

**Q14. What GM1's sounds sound like.** The game's look is parchment, ink and brass-edged buttons; the sounds are
synthesized from tones and noise bursts through filters (no recordings).
- (a) **Paper and ink:** a dry quill tick for buttons, a short paper rustle for windows, a page turn when a character
  opens, a low seal thump for saving, a muted double knock when refused. *Recommended:* they match the parchment look.
- (b) **Brass and leather:** soft metallic clicks and leather thumps, matching the buttons' brass edges.
- (c) **Soft tones:** short, quiet tones, neutral.
- **Answer (the user, 2026-11-13): (a).**

**Q15. Music in GM1.** The old game plays the region's theme, `alder-vale` when no region applies, from the first click,
and has no start screen. GM1 has no regions (GM4) or battles (GM5).
- (a) **Alder Vale throughout:** `alder-vale` plays from the start screen on and goes on into the character. *Recommended:*
  the old game is never silent while open, and no new music is needed.
- (b) **Silent start screen:** the start screen is silent; a character plays `alder-vale`.
- (c) **No music until GM4.**
- **Answer (the user, 2026-11-13): (c), not the recommendation (a): no music in GM1.**

**Q16. The start screen's buttons.** GM1-2 gave Continue (the last character), New character, Load and Settings. The old
game had no start screen; it was left by closing the window or File › Quit.
- (a) **Add Quit, hide Continue:** the four plus Quit; Continue left out while there is no character to continue.
  *Recommended:* a screen the player can leave, and no button that can't work.
- (b) **The four, Continue greyed:** only the four; Continue shown disabled when there is nothing to continue.
- (c) **Add Quit and About:** the four plus Quit and About.
- **Answer (the user, 2026-11-13): (c), not the recommendation (a): Quit and About added; and the follow-up Q16b, not
  the recommendation (left out): Continue shown greyed while there is nothing to continue.**

**Q17. The slot list.** The engine lists slots with their last write time and can delete one (save and backup; files kept
aside stay).
- (a) **Open and Delete:** each slot's name and when it was last played ("13 November 2026, 14:03"), newest first, Open and
  Delete (Delete asks once). *Recommended:* what the engine offers, and a slip loses nothing.
- (b) **Also Rename:** also Rename (the name in the save and the file's name).
- (c) **Also Rename and Copy:** also Rename, and Copy (a copy as a new slot).
- **Answer (the user, 2026-11-13): (a).**

**Q18. Reload.** The old View › Reload reloaded the page, reading the game's files again ("Choose View → Reload after
editing them"). The engine can read the packs, mods and development folder again while the game runs, made for live
editing.
- (a) **Only with Dev Mode on:** Reload (Ctrl+R) in the menu while Dev Mode is on: the data and mods read again, each mod
  checked again, the character kept. *Recommended:* a development tool, behind the game's development switch.
- (b) **Always in the menu:** for everyone, as the old menu had it.
- (c) **No Reload:** data and mods read at start only.
- **Answer (the user, 2026-11-13): (c), not the recommendation (a): no Reload; data and mods read at start only.**

**Q19. The window between runs.** The old window opened at 1440 × 950 every time. M9 listed "window state" among the
settings the port may add; the editor remembers its size.
- (a) **As the player left it:** size, maximized and full screen kept in `settings.json` and restored (1440 × 950 the first
  time). *Recommended:* what players expect, as the editor does.
- (b) **Always 1440 × 950:** as the old game; only full screen kept as a setting.
- **Answer (the user, 2026-11-13): (a).**

**Q20. About.** The engine's credits view is a dark page (the sandbox's and the editor's About). M12's answer had the
ported game show the credits again "in its own screens". The old About was a native message box.
- (a) **The game's look, same text:** a dialog in the game's parchment look holding the engine's credits text (the same
  lines, checked by the same tests) and the version. *Recommended:* every screen of the game in its look, the licences'
  text unchanged.
- (b) **The engine's credits view:** the dark view as the sandbox and the editor show it.
- **Answer (the user, 2026-11-13): (a).**

**Q21. The damaged-save question.** M9 (Q17): a damaged save loads nothing and the game asks: use the backup, or start
new.
- (a) **Backup, new, or back:** "Use the backup (saved <time>)" (when the backup reads), "Start new", and "Back" (nothing
  changes). *Recommended:* the player can leave it and decide later.
- (b) **Backup or new only:** the two, no way back.
- **Answer (the user, 2026-11-13): (a).**

**Q22. Settings on a first start.** The old music volume started at 100 %; the master and effects volumes are new. Dark
(GM1-4) and Dev Mode off (GM1-12) are decided.
- (a) **Everything at 100 %:** master, music and effects at 100 %, zoom 100 %, windowed. *Recommended:* as the old music,
  the sounds made soft for that level.
- (b) **Effects lower:** effects at 60 %, the rest at 100 %.
- **Answer (the user, 2026-11-13): (b), not the recommendation (a): the effects volume at 60 % on a first start.**

**Q23. An old game's character file.** Old character files aren't loaded (2026-10-16). One is recognisable
(`"format": "emberfall-stat-block"`); the old message for a wrong file was "That file is not a valid character save".
- (a) **Its own message:** "This character is from the JavaScript Stat Fick; it can't be loaded here." *Recommended:* the
  player learns why.
- (b) **The general message:** "That file is not a valid character save".
- **Answer (the user, 2026-11-13): (b), not the recommendation (a): the general message.**

**Q24. Load Character from the start screen** (asked after the six rounds, raised by the answers to Q6 and Q16). With Q6
(c) Load Character lives in the menu, opened by the top bar's Menu button (Q7), Escape and Ctrl+O; the start screen (Q16)
has no top bar, so a player who wants to bring a character file in at the start finds it only by Escape or Ctrl+O there.
- (a) **In the slot list:** the Load window gets a "Load Character…" button that loads a character file into a new slot
  (Q9). *Recommended:* one place for every character, nothing new on the start screen.
- (b) **A seventh start-screen button:** "Load Character" beside the other six.
- (c) **Escape and Ctrl+O only:** the menu on the start screen and the key, as first designed.
- **Answer (the user, 2026-11-13): (a)**, the menu's Load Character and Ctrl+O staying as well.

**Q25. The name on "Start new" when neither the save nor its backup can be read** (raised by step 5, asked by the
manager the same day; the options as step 5 put them). Q21's "Start new" keeps the character's name. When the save is
damaged but its backup reads, the backup holds the name; when neither reads, no name is known anywhere: the slot list
shows the slot's file name ("ser-alrik" for "Ser Alrik").
- (a) **A prompt, the file's name filled in:** "Start new" opens the "Name your character" prompt (New character's) with
  the slot's file name filled in; the new character goes into this slot under the name typed. *Recommended:* Q1 keeps
  names exactly as typed, and the file's name is a lossy form of the name the player can type back.
- (b) **The file's name:** the slot's file name becomes the character's name ("ser-alrik"), no prompt.
- (c) **The next free "Character <n>"**, no prompt.
- **Answer (the user, 2026-11-13): (b), not the recommendation (a).** When the backup reads, its name is kept as before.

**Q26. The editor's New entry and the save** (raised by step 5, asked by the manager the same day). The editor's New
entry offers every plain record kind of the game's data library, with or without `[Kind]`
(`EditorSession.NewKinds()`); once `save.game` is a record kind it would offer "Save game (save.game)", though a save
has no place in a mod.
- (a) **Not offered:** the editor leaves `save.game` out of New entry; the record is marked as not a pack kind.
  *Recommended.*
- (b) **Offered like any kind.**
- **Answer (the user, 2026-11-13): (a).** The manager gave step 5 the mark (`Engine/Statventure/Records/`), the
  editor's `NewKinds()` and its kinds-list tests for it; Import and Duplicate weren't asked about (step 5 looked:
  "How it works", step 5).

**Q27. The mod check's start time** (raised by step 6's measurement, asked by the manager on 2026-11-14; Q4 had the added
time measured and asked again if noticeable). The check reads every entry of each mod and checks every record the data
shows with it, the game's too (the 55 themes compiled against the composer), before the window opens. Measured (Release,
the built game, against the build without it, medians): +8 ms with no mods, +73 ms with one small mod, +232 ms with five
small mods, +111 ms with one mod of 2,056 entries.
- (a) **Keep the full check:** as built; one rule for the editor and the game. *Recommended:* a broken mod never reaches
  play, at a cost under a quarter of a second for several mods.
- (b) **A lighter check at the start:** the files and the kinds only, values and references checked when used.
- **Answer (the user, 2026-11-14): (a).**

**Q28. The editor's Import and a character file** (raised by step 6, asked by the manager on 2026-11-14; Q26 left Import
open). The editor's Import takes any record's JSON form of a kind the game knows; a character file (the save's JSON form)
was taken as a new `save.game` entry in the mod ("Ser Alrik.json imports as a save game (its JSON form). Its entry's
path:", then "Imported Ser Alrik.json as ser-alrik."), though no mod holds a save and the game ignores one.
- (a) **Import refuses it:** a record of a kind marked as not a pack kind is refused through Import's refusal for a file
  it can't take, in the editor's words. *Recommended:* nothing in the editor makes a save entry, as Q26 meant.
- (b) **Import left as it is.**
- **Answer (the user, 2026-11-14): (a).** Built by step 6 ("How it works", step 6); the shared mod check and the packer
  unchanged.

## The proposals as made (GM1-P1–GM1-P19)

As proposed to the manager, the recommendation first; the manager's decisions (2026-11-13) are under "Decisions" above:
all as recommended but GM1-P11's generator and GM1-P19.

- **GM1-P1 Where the game sits.** (a) `engine/Game/Statventure.Game/` (assembly `Statventure.Game`) and
  `engine/Game/Statventure.Game.Data/`, a `/Game/` folder in the solution: the engine's tree keeps `Engine/`, `Samples/`,
  `Tests/`, `Tools/`, and the game is one folder. (b) A repo-level `game/` beside `engine/` and `old-game/` (the solution,
  scripts and tests all reach across). (c) The assembly named `StatFick` (the game's name inside the bundle and in
  `ProgramInfo`).
- **GM1-P2 The sandbox after GM1.** (a) It stays the engine's test bed: not published, no data library, logging as
  `statventure-sandbox`, its cutscene mode and `--log-visibility` kept (and its capture, About and crash options, cheap);
  the stand-in project removed. (b) Deleted, the cutscene player and `--log-visibility` moved into the game as development
  options.
- **GM1-P3 The editor's tests and the end-to-end mod need a game kind with `[Kind]`; the real library has none until GM2.**
  (a) `stand-in.item` moves into the real library as a development-only kind (its help says so), removed when the first
  real kind arrives, the tests and the flow switched then; nothing ships before GM18. (b) A second, test-only library
  (another assembly name, so the editor's lookup and the packer change). (c) The tests build a kind with
  `PersistedAssemblyBuilder` (as `GameTesting.FakeLibrary` does for versions).
- **GM1-P4 Tests.** (a) A second project, `Tests/Statventure.Game.Tests/` (the game and its library referenced), sharing
  the infrastructure (`PerUserFolders`, `GpuFixture`, `Golden`, `TestFonts`, `ReferencePlatform`, `SystemFonts`) by linking
  those files where they are (not moving them, so steps on the engine's tests aren't disturbed); `test.sh` and `test.ps1`
  run both; `mutate.py` takes the project. Reason: the editor's
  tests load the library through the editor's loader, and a process holds one record kind per name (Q-C). (b) One project,
  the game's tests reaching the library only through the editor's loader (no typed access).
- **GM1-P5 The save record.** (a) `save.game` (area first), version 1 holding `name` only; GM2's part as version 2 with an
  `Upgrade()` from 1 and fixtures. (b) Version 1 left open until GM2 (no fixture until then), so GM1's test saves don't
  load afterwards. (c) `game.save` as the name.
- **GM1-P6 `settings.json`.** (a) The keys above, camelCase, version 1; `lastCharacter` kept there. (b) Continue opens the
  slot written last (its file's time), nothing kept.
- **GM1-P7 Markup.** (a) In the program: the old pieces as embedded `.html` resources or C# raw strings, textually as in
  `index.html`. (b) In `ui.pak` as files a mod can replace (a format modders meet: the user's question then).
- **GM1-P8 The port's stylesheet.** (a) A committed copy `Assets/UI/styles/game.css` owned by the port from GM1 on, and a
  sheet per new screen part beside it; `ui.pak` regenerated by `make-ui-pack.sh`, a merge conflict in it settled by running
  the script on the merged sources. (b) The old sheet copied from `old-game/` at every pack, the port's changes only in its
  own sheets layered after.
- **GM1-P9 One game document** as the old single page, the start screen a full-window layer in it, dialogs as
  `.modal-layer`s. (b) A document per screen.
- **GM1-P10 The icons' mechanism.** (a) Outlines as SVG files in `Assets/UI/icons/`, compiled into a table in the game; the
  engine gets an icon face (registered as the last character fallback of a `FontLibrary`: an advance and bounds per icon,
  shaped as single glyphs, drawn through the canvas's path fill in the text's colour); each advance the glyph Chromium
  picked on the reference PC, so lines lay out as the old game's. (b) Icons in `ui.pak` (moddable: the user's question,
  since GM1-7 declined a symbol font in `ui.pak`). (c) The icons' own advances (the old layout then differs where they
  sit).
- **GM1-P11 The randomness service** in the game program, one a run, `Next()` in [0, 1), seeded from the system and the
  seed logged, a scripted source for tests; its generator `xoshiro128**` (or the engine's `Mulberry32`). (b) In the data
  library, if rules move there.
- **GM1-P12 The game's command line:** (a) `--capture <png> [--frames N]`, `--capture-splash <png>`, `--about`,
  `--open start|settings|slots|menu`, `--crash`, `--crash-thread` (the stand-in's, kept for the flow, not in a usage text).
  (b) The crash triggers only with an environment variable.
- **GM1-P13 The mods' engine hook.** (a) `ApplicationOptions` gets the game's mod check (called for each mod file at every
  mount and Reload; the mods it accepts mount as `Mount.Mods` mounts them now, its log line kept); a shared `ModCheck` in
  the engine (`Packs/`) used by the editor's save check and the game's start check, so both refuse the same. (b) The game
  checks before `Run` and passes a list of packs to mount (Reload then can't add mods).
- **GM1-P14 Single instance.** (a) An exclusive lock file in the per-user folder and a local socket (a Unix socket, a
  named pipe) through which a second start asks the first to restore and focus its window; per per-user folder, so tests
  and scratch runs never block each other; on Wayland focus may be refused (GLFW's attention request then). (b) A second
  start shows "Stat Fick is already running" and ends.
- **GM1-P15 The file picker** is the game's own view on the engine's `FileBrowser` model, not the editor's code; places
  Documents, home, the roots, recent places (ten, in `settings.json`); only the rows near the view drawn in a long folder.
- **GM1-P16 The sounds' tooling.** Sources as JSON forms in `Assets/Audio/effects/`, packed into `audio.pak` by
  `make-music-data.sh` (or a new `make-sound-data.sh`); fixtures added to the M8 effect fixtures; `PlayTheme --effect`
  and `Tools/Scripts/play-sounds.sh [name …] [--volume 0-100]` for the user's ear.
- **GM1-P17 The new screens' Chromium fixtures:** the game's tests write `Reference/game-screen-fixtures.json`; a new
  `Tools/Scripts/make-game-screen-references.sh` captures them with PaintReference at 1 and 1.65 into
  `Golden/game-screen-*.png`, as `make-editor-references.sh` does.
- **GM1-P18 The two tooling items, in step 2** (it owns the build and publish scripts):
  - *Publish through a symlink:* `env.sh` sets `STATVENTURE_SOURCE` from the real path (`pwd -P`) and `Publish.cs` resolves
    `--engine` to it. The interrupted probe (`~/.cache/stat-fick-build/claude-work/prep/probe/`) shows why: a restore of the
    sandbox named by its `/home/…` path gave a root with 0 packages while its references were built under `/var/home/…`;
    the physical path, or a relative one from the logical folder, gave 37.
  - *`build.sh` after a failed shader:* not confirmed by the probe. `shader-repro.log` ends at "shaders: 0 built, 30 up to
    date, 1 failed" with no MSBuild output after it, while the baseline build's log carries MSBuild's 148 warnings and
    summary; the compiler returns 1 on a failure (`Tools/ShaderCompiler/Program.cs` 55) and `build.sh` runs under `set
    -euo pipefail` (`build.ps1` checks `$LASTEXITCODE`, 12–13). The repro printed its exit code to its terminal, not the
    log. The step runs the repro again recording the exit code, and changes the script only if the build goes on.
- **GM1-P19 Order of the stylesheet and packs among steps:** `ui.pak` and `audio.pak` are regenerated by their scripts;
  the merge worker reruns the script after merging steps that both changed a pack's sources.

## Left to the steps

Small internal choices, each recorded by the step that makes it; a step that finds one players would see stops and asks.
- The program's classes beyond the seams' outline; the window manager's, the dispatcher's and the icon face's API in
  detail.
- The slot file naming rule's edge cases beyond "The slot's file" (step 5).
- Out-of-range settings values (clamped or defaulted, logged); how the window's maximized state is read and kept, and
  whether full screen and maximized can be put in place before the splash (else at `Load`, a jump after the splash), if
  the engine's `Window` and `WindowOptions` don't offer them (step 2 asks the manager before an engine change).
- The log lines' exact words beyond those the end-to-end flow reads.
- The Electron 44 role labels and keys of the old View menu and Quit (step 8 reads them from Electron's source and
  records them).
- The sounds' parameters (approved by ear), the fixtures' and expectations' names.
- Removing the sandbox's files from `$STATVENTURE_GAME` when the game is first published there (step 2).
- A step whose screen meets one of the 1.65 findings or the row painting cost ("What exists", "Engine items") stops and
  reports; the manager asks the user for that engine task.
- Whether a rule only the recaptured stylesheet holds and no captured screen shows is ported now or recorded for the
  milestone whose screen shows it (step 1b).

## Steps

Each step ends as "How each step is finished" says: its tests, the full suite (both test projects since step 2; a report
gives each project's count and their sum, GM1-P4) on the GPU and on lavapipe under the shared lock inside a headless
compositor, a mutation list for every rule it adds, `check-packages.sh` where it makes packages, its entry under "How it
works" and Status here, its Progress line, `Docs/Notes.md`. At most four workers at once, each in its own worktree;
steps side by side touch no file in common besides the docs (each its own entry) and the generated files below. Workers
don't touch Engine-Plan's "Current task" (the merge worker does).

**The program's own files are step 2's** ("The seams"): its application class runs the start sequence through the parts
each later step fills in a folder of its own; the program's and the game's test project's files are taken by wildcards,
so later steps add and change only their own folders; screens reach each other through the dispatcher's commands, never
through each other's code.

**Steps with a user approval stop twice** (3 the icon sheet; 4 the sounds; 8, 9 and 10 their screens). First the step
makes what the user must see or hear and stops, reporting it: captures as PNG files at 1× in both themes under
`/var/home/Vatrix33/.cache/stat-fick-build/claude-work/gm1/step<k>/approval/` (`step3`, `step8` …), sounds through
`Tools/Scripts/play-sounds.sh` (the user in a real terminal; the step itself only at volume 0). The manager shows the user
and sends the verdict; only then does the step make the Chromium fixtures and tests of those looks (or the sounds'
fixtures) and finish. A change the user asks for is made and shown again before the fixtures.

**Generated files at merges (GM1-P19):** the merge worker never generates, builds or commits anything. When two steps of a
wave change one generated file's sources (`Assets/Packs/ui.pak`: 8, 9 and 10 each add sheets; `audio.pak`;
`Reference/game-screen-fixtures.json`, which the tests write), the one merged second rebases on `main`, reruns the pack's
script or the tests that write the file, reruns its tests and the full suite, and only then is merged.

**For the workers:** a mutation run rewrites `Reference/game-screen-fixtures.json` as it rewrites `editor-fixtures.json`:
restore it with `git checkout` after one. Every scripted start of the game (`--capture`, the end-to-end flow, a check)
uses a scratch per-user folder (`XDG_DATA_HOME`) and OpenAL Soft's null device (`ALSOFT_DRIVERS=null`); no window on the
user's screen (a headless compositor), never the player's real folders, never the real Documents folder. The old game
stays as it is until the manager's ok (the recapture's freeze).

**Order:**
- **Wave 1:** 1a (then 1b, the same worker, merged separately), 2, 3, 4.
- **Wave 2:** 5 and 6 after 2; 8 after 1a and 2; 7 after 1a and 3 (approved), joining when a worker is free (1b's
  worker counts while it runs: at most four at once).
- **Wave 3:** 9 and 10 after 5, 6, 7 and 8.
- **Wave 4:** 11 after 2–10.
- GM1 ends when 1b and 11 are merged; then the manager ticks GM1 in `Docs/Game-Plan.md` and stops for the user.
- What needs the recapture: nothing but GM1's end. 1a is needed by 7 (it changes the text painting 7 builds on) and by 8
  (its document loads `ui/styles/game`), so by 9, 10 and 11 through 8. Step 7's layout check uses whatever captures are on
  `main` when it runs (`f45ca7a`'s, or 1b's once merged): the states that hold icon characters.

1a. **Today's stylesheet in the engine and in `ui.pak`.** Independent; waits for nothing.
   - Owns: the engine's `UI/Css/`, `UI/Style/` and `UI/Paint/` (and `Canvas/` or `Text/` only where `background-clip:
     text` needs the glyphs as a mask) for `@property` (registered custom properties: `<color>` syntax, inheritance,
     initial value, interpolation in keyframes), `-webkit-text-fill-color` and `background-clip: text`, each with its
     Chromium fixture at 1 and 1.65 (`Reference/paint-fixtures.json` or a list of its own, their goldens); new
     `Assets/UI/styles/game.css`; `Tools/Scripts/make-ui-pack.sh`; `Assets/Packs/ui.pak`; `Docs/Assets.md`'s step 7 notes
     and `Docs/Compositing-Port.md` if they record the new rules.
   - Must not touch: the game-screen references and the tests' stylesheet copy (1b's), `old-game/`, the game program, the
     limits.
   - Finished: each rule matching its fixture within the paint limits and shown able to fail; `ui/styles/game` in
     `ui.pak`, compiled from today's sheet; the full suite passing with the references as they are. User approval: none.
   - **Done (2026-11-13, the branch `gm1-step1a`; "How it works", step 1a):** the three rules are ported from Blink.
     - *The fixtures:* five, in their own list `Reference/stylesheet-rule-fixtures.json` (`property-rule`,
       `text-fill-color`, `background-clip-text`, `background-clip-text-view`, `text-parent-rounding`). Each matches
       Chromium at 1 and 1.65, and each rule is shown able to fail.
     - *The pack:* `ui/styles/game` is in `ui.pak`, compiled from `Assets/UI/styles/game.css` (today's sheet), and the
       pack is reproducible.
     - *Found on the way:* a text's y is rounded against its parent inline box (ported). A field's text sits 1 px low
       when its line-height is under the font's (left to 1b).
     - *Outside the step's folders, with the manager's leave:* `BlendMode.DestinationIn` in `Graphics/PipelineCache.cs`;
       the editor's entry list tests and twelve editor screens, for `ui.pak`'s new `ui/styles` folder.
     - *The choices:*
       - only `<color>` is registered, and compiling refuses any other `@property`;
       - registered values are kept as their computed text;
       - animated custom properties are applied by computing the style again;
       - the text-rounding rule applies to all text;
       - the harness gained `trigger`, `computed` and the selections;
       - the background-clip fixture keeps out what the engine draws differently outside these rules (outside
         markers, a button's outset border, inline opacity, a field's line-height).
1b. **The recapture (GM1-1).** After 1a, the same worker, merged separately; the old game frozen meanwhile.
   - Owns: `Tests/Statventure.Tests/Reference/game-capture.js`, `Golden/game-*.png` (22), `Golden/game-ui.json.gz`,
     `game-ui@1.65.json.gz`, `Reference/scaled/game-layers@1.json` and `@1.65.json`, `Data/Game/styles.css`, the game-screen
     tests (`Ui/GameLayoutTests.cs`, `GamePaintTests.cs`, `ScaledGameTests.cs`, `GameHitTests.cs`,
     `CompositingReferenceTests.cs`, `CompiledGameStylesTests.cs`, `GameStylesheets.cs`, `IncrementalWorkTests.cs`,
     `IncrementalUpdateTests.cs`) where the new captures need it, the engine's `UI/` for further rules the captures meet,
     the three capture scripts' stale comments.
   - Must not touch: `old-game/` (a change it needs goes to the manager), `Text/` and `Canvas/` once step 7 runs (stop and
     report), `ui.pak`, the game program, the limits.
   - Finished: the 22 captures and the layer traces from today's old game, headless; the copy today's `styles.css` byte for
     byte; every game-screen test passing at its limits; each rule met ported and shown able to fail, or recorded as held
     with the user's word. User approval: none, unless something is to be held. Once merged, the manager gives the user
     the ok on the old game.
2. **The game program and the real data library in place of the stand-ins; the seams.** Independent; GM1-P1–P4, P12,
   P14, P18.
   - Owns: new `Game/Statventure.Game/` (the application class, `Program`, `GameParts`, `Sounds/SoundNames`, and every
     part's stub in its folder as "The seams" outlines: the start sequence and the wiring, options per GM1-P12, the window
     1440 × 950 minimum 820 × 600 titled "Stat Fick", single instance per GM1-P14, logging as `stat-fick`, the stand-in's
     `--about` with the engine's credits view until step 9; its project with wildcards) and new
     `Game/Statventure.Game.Data/` (`StandIn/StandInItem.cs` per GM1-P3, the `Saves/SaveGame` stub), `Statventure.slnx`,
     `Samples/Statventure.Sandbox/**` (GM1-P2), the removal of `Samples/Statventure.Game.Data/`, `Tools/Scripts/build.sh`,
     `build.ps1`, `test.sh`, `test.ps1`, `editor.sh`, `editor.ps1`, `env.sh`, `play-cutscene.sh` if it changes, `mutate.py`
     (GM1-P4), `cloud-setup.sh` if needed, `Tools/Packaging/Publish.cs`, `listings/stat-fick-*.txt`, `licenses.txt`,
     `release-text.md`, `end_to_end.py`'s library path (409–415) and the stand-in page's picture check only,
     `check_packages_self_test.py` if a line names the sandbox, `Tools/Editor/Game/GameLibrary.cs` (`RepoProject`),
     `Tools/Packer/Packer.csproj`, `Tests/Statventure.Tests/Statventure.Tests.csproj`, new `Tests/Statventure.Game.Tests/`
     (its project, the linked infrastructure, a test of the start sequence on the stubs), `engine/CLAUDE.md`'s lines on the
     sandbox and the stand-in, `Docs/Packaging.md` and `Docs/Editor.md` (their notes on the stand-in).
   - Must not touch: `Engine/Statventure/`, the editor's code beyond `RepoProject`, `old-game/`, the per-user folders.
   - Finished: the program runs its start sequence on the stubs; `build.sh` publishes it into `$STATVENTURE_GAME`;
     `publish.sh` makes the four archives with it and `check-packages.sh` and its self-test pass (listings made again); the
     packaged game starts headless, shows the splash and its page and ends (`--capture`); a second start on the same
     per-user folder ends and the first's window is asked forward; the editor finds the real library (`--repo`, `--game`);
     the packer and `editor.sh` use it; the sandbox's `--cutscene jiruan --dump` and `--log-visibility` work as before
     (volume 0); `end-to-end.sh` passes with the game where the stand-in stood; a publish from a clone reached through
     `/home/…` works; the shader repro run again and its exit code recorded (the script changed only if the build goes
     on). Mutations: the program's options, the single-instance rule, the window's options, the build's layout. User
     approval: none.
   - **Done (2026-11-13, "How it works", step 2).** The probe confirmed GM1-P4's reason. The seams are built with the
     nearest shape where the outline couldn't be written as it stands, each deviation listed there for steps 5–10 (static
     `Load` factories; `WindowState?` for a window never kept; `GameMods.Attach(GameSettings)`; the session's result
     records; `GameCommands`' shape, keys left to step 8; `IGameScreen`; the About stub registering `about` with the
     engine's credits view until step 9; `Session.Leave()` in `Unload`; `--open <screen>` running `open-<screen>`).
     The step's own choices: maximized and full screen put in place at `Load` (the engine's `WindowOptions` lack them;
     no engine change); the single instance's channel in Linux's abstract namespace named by a hash of the folder's real
     path, the lock file `stat-fick.lock` left in the per-user folder; a later start ends with 0; the stub page the
     Gothic ink theme's parchment; the sandbox's leftovers removed by name; `mutate.py`'s project per mutation;
     `env.sh`'s `pwd -P` on the scripts' folder only. `build.sh` unchanged for the shader item (the repro: exit code 1).
3. **The icons' outlines and their approval (GM1-7).** Independent; Q12 and GM1-P10 decided.
   - Owns: new `Assets/UI/icons/` (28 outlines), the approval sheet's fixture and a script making it in Chromium (new,
     under `Tools/Scripts/`), the measured boxes (each icon's advance and height from the glyph Chromium picked).
   - Must not touch: engine code, the tests' references, `old-game/`.
   - Finished: stops with the sheet (each icon at the sizes and colours the game uses, both themes, beside Chromium's
     glyph) in `claude-work/gm1/step3/approval/`; after the verdict the outlines and boxes committed. User approval: the
     icons' looks.
   - **Done (2026-11-13, the branch `gm1-step3`; "How it works", step 3):**
     - The 28 outlines and `icons.json` are in `Assets/UI/icons/`, written by the committed generator
       `Tools/IconSheet/gen/`.
     - The style is "Engraved", the user's pick over the recommended Woodcut. ★ and ✸ have the heavier lit line, the
       user's answers at stop 2; the set was approved the same day.
     - The tooling is `Tools/Scripts/make-icon-sheet.sh` with `Tools/IconSheet/`; its fixture is `uses.json`, the 83
       uses.
     - Choices: each icon's box is the glyph Chromium drew, its ascent and descent read as Chromium reads them (OS/2's
       typo metrics when USE_TYPO_METRICS is set), plus the line gap. The sheet script runs inside a cage of its own when
       cage exists.
     - Nothing the solution builds or the tests read changed, so no full runs were due.
4. **The sounds and their approval (GM1-6, GM1-11).** Independent; Q13, Q14, Q22, GM1-P16 decided.
   - Owns: new `Assets/Audio/effects/*.json`, the packing script by GM1-P16's rule (`make-music-data.sh` if it reruns
     quickly and headless and gives `audio.pak` byte for byte, else a new `make-sound-data.sh` replacing only
     `audio/effects/*`), `Assets/Packs/audio.pak`, the effect fixtures' list and their renders
     (`Tests/Statventure.Tests/Audio/` fixture sources, `Reference/audio-fixtures.json`, `Golden/audio/<sound>.f32.gz`),
     `Tools/AudioReference` if needed, `Tools/PlayTheme` (`--effect`), new `Tools/Scripts/play-sounds.sh`.
   - Must not touch: `Engine/Statventure/Audio/` (a patch needing a new rule stops and reports), the game program.
   - Finished: stops with the eight patches playable by `play-sounds.sh`; after the verdict each patch's fixture matching
     Chromium by M8's rule, `audio.pak` made again and reproducible. User approval: each sound by ear.
   - **Done (2026-11-13, the branch `gm1-step4`; "How it works", step 4):** three candidates a sound, rendered in
     Chromium and measured offline, then the user's picks by ear (b c c c a c c c, levels as heard). The step's choices:
     `make-music-data.sh` by GM1-P16's rule (it reruns in about a second and gives the pack back byte for byte), now
     copying `Assets/Audio/effects/*.json` in; the sources in the record's exact JSON form; the fixtures at 48 kHz, named
     as the sounds, the engine side playing the patch registered from the pack by name; the loudness set by the mean of
     a K- and an A-weighted measure over the loudest 100 ms; no per-play ranges; `play-sounds.sh` playing every effect
     in the pack with no names, `--candidates` kept for later milestones; outside the audio fixtures, the tests that pin
     `audio.pak`'s content brought to it (the manager's leave): `MusicLibraryTests`, `AudioMarksTests`, four
     `EntryListTests`, the ten editor screens' markup and 20 goldens.
5. **Saves, settings and the randomness service.** After 2; Q1, Q2, Q3, Q9, Q19, Q21, Q22, Q23, GM1-P5, P6, P11 decided.
   - Owns: `Game/Statventure.Game.Data/Saves/` (the record, its marks and checks), `Game/Statventure.Game/Settings/`,
     `Session/`, `Random/` (their parts as "The seams" outline them), their tests and fixtures in
     `Tests/Statventure.Game.Tests/` (`Saves/`, `Settings/`, `Session/`, `Random/`, `Data/Records/`).
   - Must not touch: `Engine/Statventure/Storage/` (an engine change found is reported), the other parts' folders, the
     application class.
   - Finished: round trips of the save, the character file and `settings.json`; every damaged and unreadable case through
     the session; the committed version-1 fixture and layout signature; the naming rule's cases; the settings' defaults
     (effects 0.6) and change events; seeded and scripted draws, `xoshiro256**` and SplitMix64 against their reference
     outputs; nothing outside scratch folders. Mutations: each rule. User approval: none.
   - **Done (2026-11-13, the branch `gm1-step5`; "How it works", step 5).** Raised and answered on the way: Q25 (Start
     new's name when nothing reads: the slot's file name) and Q26 (`save.game` marked `[NotAPackKind]`, left out of the
     editor's New entry; the mark, `NewKinds()` and its tests given to this step), and from the manager the slot list's
     backup names, the import refusal's words, `GameLookupTests`' kinds count and `lastCharacter` cleared in memory only
     (the engine's `Settings` has no `Remove`: an engine item). The step's choices: the naming rule's edge cases (`_` and
     `.` and letters outside a–z are other characters, `character` for nothing left, a device name starting at `-2`, the
     cut keeping the number's room, taken by the files); the settings' ranges (the window 820 × 600 to 16384 × 16384; a
     set window held within them; the other setters refusing); `Saved` on making and loading a character too; the
     session's `ContinueSlot()`; the library's own layout guard in the game's tests; the generators' composite checked
     by an own transcription of the reference code.
6. **The mod check at start.** After 2; Q3, Q4, Q10, Q18, GM1-P13 decided.
   - Owns: `Engine/Statventure/Core/Application.cs` (the option), `Engine/Statventure/Packs/VirtualFileSystem.cs` and the
     new shared mod check in `Packs/`, the editor's `Tools/Editor/Checks/EntryChecks.cs` and
     `Session/EditorSession.Saving.cs` as they move onto it, `Game/Statventure.Game/Mods/`, tests in
     `Tests/Statventure.Tests/Packs/` and `Editor/` (the shared check) and `Tests/Statventure.Game.Tests/Mods/`.
   - Must not touch: the other parts' folders, the application class (it calls `GameMods.ApplyTo`), the screens.
   - Finished: a good mod mounts; each break leaves its mod out with its problem in the log and the model; a switched-off
     mod isn't checked or mounted; a switch changed this run marked; the editor refuses exactly what the game leaves out;
     the start time measured (none, one, several mods) and reported. Mutations: each rule. User approval: none (the cost, if
     noticeable, goes to the user).
   - **Done (2026-11-13, the branch `gm1-step6`; "How it works", step 6):** the engine's shared check `Packs/ModCheck.cs`
     (the pack opens, every entry reads, every record the data shows checked as the editor's form shows it, the music's
     loading, `mod/info` a `mod.info` that reads), the editor's checks moved onto it (one rule new to the editor: a
     `mod/info` that isn't a `mod.info` refuses Save), `ApplicationOptions.ModFilter` given to the mods folder's mount,
     `GameMods` filled. The step's choices: the check's stages and order (the editor's Save's: an entry's own problems
     first, entries in path order, the data's last), the game's data under a mod the mounts below the mods folder, the
     filter asked once per mount with the whole list, a refused index's entry named, a switched-off mod's info read for
     its name, `SwitchedSinceStart` as a difference from the start, blank info text as not given, the log lines' parts
     only when given. The start time: +73 ms (one small mod), +232 ms (five), +111 ms (one of 2,056 entries) on the
     wall time, all before the window, kept by the user (Q27); on step 1a's packs (the game's stylesheet checked with
     every mod) +494 ms, +1,440 ms and +515 ms, gone to the manager. With it the user's Q28: the editor's Import refuses a
     record of a kind marked as not a pack kind (the save).
7. **The icons in the engine.** After 1a and 3 (approved); GM1-P10 decided.
   - Owns: `Engine/Statventure/Text/` (the icon face, its fallback place), `Engine/Statventure/Canvas/` where icon glyphs
     are drawn, `Game/Statventure.Game/Icons/` (the table made from `Assets/UI/icons/`), its tests and fixtures in a list of
     its own (`Reference/icon-fixtures.json`: the sheet against Chromium's inline-SVG outlines at 1 and 1.65; a captured
     state's markup laid out with the icons in place of the fallback fonts, its boxes the capture's).
   - Must not touch: `UI/Style/`, `UI/Css/`, `UI/Paint/` (1a's and 1b's), the other parts' folders, the screens.
   - Finished: every icon drawn at its box in the text's colour at any size and both scales, matching Chromium's drawing
     of its outline within the path tolerance; lines with icons laid out as the captures. Mutations: each rule. User
     approval: none beyond step 3's.
   - **Done (2026-11-14, the branch `gm1-step7`; "How it works", step 7):** the engine's icon face (`IconGlyph`,
     `FontLibrary.AddIcons`, taken before any font; one face per box and parent font, its space the parent's; the box's
     advance and metrics; drawn by the canvas's path fill where the glyph lands, grayscale, as a glyph in every rule of
     the painter), the game's table compiled from `Assets/UI/icons/` (`GameIconTable.cs`, written and checked by the
     game's tests) and `GameIcons.AddTo`; the seam's `GameIcon` gained `LineGap`. Checked: Chromium filling the same
     outlines where the engine's glyphs land (the 83 uses, the 28 large, the rules; 1 and 1.65; no pixel past 60
     levels), every captured state laid out box for box with the icons. Nothing in `UI/` changed. The step's choices:
     the face's shape (HarfBuzz font functions; per box and parent), the space from the parent, no synthesis, the
     advance through the font path's truncations, the outline in em for the path cache, glyph snapping but none under a
     rotation, no LCD for icons, the generated table, `AddTo` once per library, the fixtures placed from the engine's
     layout per scale. Found for the manager: `InlineLayout` takes only a fallback run's ascent into a `line-height:
     normal` line (Blink its descent and gap too; no captured line meets it); a transformed box's overflow scrolls in
     Chromium, not in the engine; ◆ ★ as the map's SVG text left to GM4.
8. **The game's document, windows, dialogs and commands.** After 1a and 2; Q7, Q13, Q15, GM1-P7, P9 decided.
   - Owns: `Game/Statventure.Game/Ui/` (`GameUi` as "The seams" outline it: the one document with the port's stylesheets
     from `ui.pak`, the theme class, the page zoom, the window manager, the dispatcher with its keys and its own commands,
     the confirm and prompt dialogs, the status event, sounds by name), new `Assets/UI/styles/dialogs.css` and
     `controls.css` (tooltip, select list, validation bubble), `ui.pak` made again, their fixtures, tests in
     `Tests/Statventure.Game.Tests/Ui/`.
   - Must not touch: the other parts' folders, the application class, the screens, `game.css` (1a's).
   - Finished: stops with captures of the two dialogs and the three control styles; after the verdict: windows open and
     close as the old `.modal-layer`s (Escape, focus kept and given back, `modal-open`); the dialogs and controls matching
     Chromium; the keys and commands; each sound name played at its action (counted, silent); no theme started.
     Mutations: each rule. User approval: the dialogs' and controls' looks.
9. **The start screen, the character's page and its top bar, the in-game menu, About.** After 5, 6, 7 and 8; Q2, Q5–Q8,
   Q10, Q11, Q16, Q16b, Q20 decided.
   - Owns: `Game/Statventure.Game/Screens/Start/`, `Screens/Sheet/` (the GM1 page and the top bar), `Screens/Menu/`,
     `Screens/About/`, their sheets `Assets/UI/styles/start.css`, `menu.css`, `about.css`, `ui.pak` made again, their
     commands, fixtures and tests.
   - Must not touch: step 8's and 10's folders, `game.css`, the application class.
   - Finished: stops with captures of the start screen (first start with Continue greyed, and with a character), the page
     with its top bar, the menu (on the page and on the start screen) and About, both themes; after the verdict: the flows
     (first start, Continue, New character, Start screen, Quit and the window's close saving) on scratch folders, the
     screens matching Chromium (12 levels / 0.2 %), the left-out mods' line. Mutations: each rule. User approval: those
     screens.
10. **The settings screen with the mod list, the slot list, the damaged-save question, the file picker.** After 5, 6, 7
    and 8; Q1, Q3, Q9, Q15, Q17, Q18, Q19, Q21, Q22, Q23, Q24, GM1-P15 decided.
    - Owns: `Game/Statventure.Game/Screens/Settings/`, `Screens/Slots/`, `Screens/Picker/`, their sheets
      `Assets/UI/styles/settings.css`, `slots.css`, `picker.css`, `ui.pak` made again, their commands (Save Character and
      Load Character among them), fixtures and tests.
    - Must not touch: step 8's and 9's folders, `game.css`, the application class.
    - Finished: stops with captures of the settings screen (the mod list with each state, a switch changed, the music row
      with nothing playing), the slot list with its "Load Character…" button, the damaged-save question and the picker
      (save and open), both themes; after
      the verdict: every setting applied and kept; Open, Delete and the damaged-save choices on scratch slots; Save and
      Load Character through the picker on scratch folders (never the real Documents); the screens matching Chromium.
      Mutations: each rule. User approval: those screens.
11. **End to end, the package check and the Windows items.** After 2–10; GM1-P12 decided.
    - Owns: `Tools/Packaging/end_to_end.py`, `Tools/Scripts/end-to-end.sh`, the game's in-process flow test in
      `Tests/Statventure.Game.Tests/EndToEnd/`, the listings if the game's files changed since step 2,
      `Docs/Engine-Plan.md`'s "Windows check" items, this doc's Status.
    - Finished: the flow passes on the PC on the GPU and on lavapipe (Xvfb in the cloud if it can run there), the real
      folders unchanged; its expectations shown able to fail by breaks; the Windows items rewritten.

| Step | Part | Depends on | Owns (main) | User approves |
| --- | --- | --- | --- | --- |
| 1a | Today's stylesheet in the engine and `ui.pak` | — | engine UI/Css, Style, Paint (rules for `@property`, `-webkit-text-fill-color`, `background-clip: text`) and fixtures; `game.css`, `make-ui-pack.sh`, `ui.pak` | — |
| 1b | The recapture | 1a (same worker) | `game-capture.js`, the 22 goldens, `game-ui` captures, layer traces, the tests' `styles.css`, game-screen tests, further UI rules | only a hold |
| 2 | Program, library, seams | — | `Game/**` (application class, stubs), sandbox, scripts, packaging, `GameLibrary.RepoProject`, packer, both test projects | — |
| 3 | Icon outlines | — | `Assets/UI/icons/` (28), the sheet | the icons |
| 4 | Sounds | — | `Assets/Audio/effects/`, `audio.pak` and its script, effect fixtures, `play-sounds.sh` | each sound |
| 5 | Saves, settings, randomness | 2 | library `Saves/`; `Settings/`, `Session/`, `Random/` | — |
| 6 | Mod check | 2 | engine `Application.cs`, `Packs/` and the shared check; editor's checks; `Mods/` | (the cost, if noticeable) |
| 7 | Icons in the engine | 1a, 3 | engine `Text/`, `Canvas/`; `Icons/` | — |
| 8 | Document, windows, dialogs, commands | 1a, 2 | `Ui/`; `dialogs.css`, `controls.css` | dialogs, controls |
| 9 | Start screen, page and top bar, menu, About | 5, 6, 7, 8 | `Screens/Start`, `Sheet`, `Menu`, `About`; their sheets | those screens |
| 10 | Settings and mods, slots, damaged save, picker | 5, 6, 7, 8 | `Screens/Settings`, `Slots`, `Picker`; their sheets | those screens |
| 11 | End to end, packages, Windows items | 2–10 | `end_to_end.py`, `end-to-end.sh`, the in-process flow test, "Windows check" | — |

## How it works

Filled in as the steps are done.

### Step 3: the icons' outlines and their approval (2026-11-13)

**Decided.**
- **The user (2026-11-13, the question tool, asked by the manager after each stop):**
  1. **The style is "Engraved", not the recommendation.** It was one of three styles shown on six icons (⚙ ✦ ⚔ ♜ ♡ ◈).
     The other two were "Woodcut" (solid ink with a highlight cut along the lit edge, recommended for its weight at small
     sizes) and "Hatched" (the map's hatched shadow).
  2. **★ and ✸ get the heavier lit line**, option (b) both times, as recommended. As first drawn, ★ read too close to ☆
     on the same favourite button at 11–16 px, and ✸ was a faint dot at its only size, 9 px. (Not solid ink; not as
     drawn.)
  3. **The set of 28 is approved** as shown, with those two answers. ✦ stays as drawn; the user was offered the heavier
     line for it too and didn't take it.
- **The manager (2026-11-13):**
  1. **The icon face draws its code points before any font.** ◌ stays an icon, and it is in Lato and STIX ("The symbols
     as vector icons"; GM1-P10 changed in that one point).
  2. **The generator is committed** in `Tools/IconSheet/gen/`, with a README. Nothing of the build or the tests needs
     it; later milestones add their icons with it.
  3. **Symbols with two meanings carry over unchanged**, as the old game has them. Guard is ◈ on the Guard action but ◆
     on the stage's guard status. ▣ means Armor on the cards and Equipment on a companion's tab. ⚙ is the Settings button
     and the trap status. ★ is a favourite and the great-hunt map marker.
  4. **The smallest uses keep the approved style.** They are the battle statuses ▲ ▼ ◆ ◎ ⚙ ✸ at 9 px bold and the map's
     ⇧ shortcut hints at 10 px. No icon was made simpler on the step's own judgement; the sheet shows those uses
     enlarged, and each icon that might not read there went to the user (★ and ✸ above).

**The style ("Engraved").** It follows the map's compass rose in `old-game/scripts/map-ink.js` ("each point is split
into a lit and a shadowed half, as on an engraved rose") and an engraver's shade lines. The light comes from the upper
right, as on the map's marks.
- A filled figure (★ ♜ ◆ ▲ ✦ …) is an ink contour 0.056 em wide. Its shadow half is solid ink, and its lit half is open
  inside the contour. The split follows the figure:
  - stars and ✦ split per point, clockwise, as the compass rose;
  - tall figures (♜ ♛'s foot ♟ ◆ ▲ ▼) split on the west half;
  - squares and the gear split on the lower-left diagonal;
  - an arrowhead splits along its axis.
- An open figure (☆ ◇ ♡ ⇧ ◎ …) is a line 0.056 em wide on its lit side and 0.116 em on its shadow side. The inside
  shifted towards the light is the hole, so the weight changes smoothly round the figure.
- Thin parts are solid ink: ☰'s bars, ◫'s line, ⌖'s cross, ♛'s spikes and balls, ⌄'s strokes, ✺'s petals. Where a row
  of marks faces the light, those on the shadow side are heavier: ◌'s dots and ✺'s petals grow towards the lower left,
  and ⌄'s west stroke has a shade line under it.
- ★ and ✸ have a 0.110 em line on their lit halves, so their open parts are slivers (decision 2).
- The front sword of ⚔ cuts a 0.044 em gap round itself.
- All weights are the same in em for every icon (`W` in `gen/iconlib.py`).

**The files.**
- **`Assets/UI/icons/<name>.svg`:** one per icon, the name being the character's Unicode name in lower case with hyphens
  (`black-four-pointed-star.svg`).
  - Each file has one `<path fill-rule="nonzero">`: filled shapes with holes, outer rings and holes wound in opposite
    directions, coordinates to 1/10 unit.
  - There are no strokes, styles, transforms, text or fill colour; the engine fills the path in the text's colour.
  - Units are 1/1000 em: x from the pen position, y down from the baseline (y = 0).
  - The `viewBox` is the icon's box (0, −ascent, advance, ascent + descent), and a comment states it.
- **`Assets/UI/icons/icons.json`:** the box table, one icon a line in code-point order. Each entry has `codePoint`,
  `character`, `name`, `file`, `advance`, `ascent`, `descent` and `lineGap` (in em), and `measured`: the font's name,
  file and version string and how it was found.
- **`Tools/IconSheet/gen/`:** `iconlib.py` (the geometry and the style), `figures.py` (the 28 figures), `build.py` and a
  README.
  - `build.py` writes the SVGs and the table. Each figure's ink box is centred on the old glyph's ink box, at about its
    size: the figures are drawn to the old ink sizes, and `--fit` lists the differences.
  - `--check` rebuilds into a temporary folder and compares byte for byte; it found 29 files and 0 differences on the
    committed set. The outlines were written with shapely 2.2.0 and fonttools 4.68.0 on Python 3.15.
- **`Tools/IconSheet/old-game-uses.js`, `uses.py`, `uses.json`:** the probe of the old game and the sheet's fixture.
  - The probe runs in the old game's headless test client. It puts each script-built use's markup, taken from the
    lines it names, into its real container. It reads the computed font, colour, opacity and ground in the light theme
    and under `html.dark-mode`; the ground is composited up the tree, a gradient taken at its first stop. The Field tools
    button's ☰ is read with the window at 800 px.
  - With the canvas it measures each symbol's advance and ink box at 1000 px in each use's font, and compares pixels
    with every installed family that has the symbol.
  - `uses.json` holds the 83 uses of the 28 symbols, one use a line, sizes 9–63 px. The map markers' grounds are the
    markers' circle colours, which the probe can't read.
- **`Tools/IconSheet/sheet.py` and `Tools/Scripts/make-icon-sheet.sh`:** the sheet in Chromium. Each use is shown at its
  real size, colour, opacity, ground and turn, with the words beside it, in both themes. Every use's measured advance
  must be its icon's advance, or the sheet isn't made.
  - Its pages are `-uses-` (every use), `-small-` (the uses at 13 px and under, with a 3× copy below) and `-large-`
    (96 px over the box and the baseline).
  - It renders with Tools/PaintReference at 1×, and crops each page to its content with PaintReference's element boxes.
  - On a machine with cage it runs inside a headless cage of its own; without cage, `chromium.sh` uses Xvfb.
  - `--icons label=folder` shows several sets side by side; it was used for the style sample and the questions.
  - `--uses` measures the old game again (on the PC, with fontconfig's candidate families).

**The boxes and the fonts.** Each icon keeps the box of the glyph Chromium drew for its symbol on the reference PC, so
lines lay out as the old game's did. The values are read from the repo's copies of those fonts
(`Tests/Statventure.Tests/Data/SystemFonts`).
- **Advance:** the font's horizontal advance (`hmtx`) over its em. Chromium's measured advance equals it in every one of
  the 83 uses, at every weight; Adwaita Sans's weight axis isn't set on a fallback.
- **Ascent, descent and line gap:** as Chromium reads them. Skia, through its Fontations backend, takes OS/2's typo
  metrics when USE_TYPO_METRICS is set, otherwise hhea's. Chromium's canvas `fontBoundingBox` agreed for every font it
  could name.
- **Which font drew each symbol:**
  - **The 20 in `TestFonts.SymbolFonts`** use that table's fonts (Chromium's fallback in 2026-10).
  - **⇧ ⌄ ◆ ★** are Adwaita Sans. For ⇧, fontconfig's recorded answer (`sans-serif:charset=21e7`) agrees.
  - **◎** is Noto Sans Math, **✸** Adwaita Mono, and **♛** Noto Sans Symbols 2. In each case Chromium's advance and ink
    box are that font's; for ♛ the ink box rules out Symbola, which has the same advance.
  - **▲ ▼** are Liberation Sans Bold: `--font-ui` names Arial and the battle status is 700, so this is not a fallback.
  - **◌** is Liberation Serif, the serif of "Georgia, serif".
- **Adwaita Mono on the PC has been updated since the capture** (34.01 to 34.08), but its advances and metrics are
  unchanged.
- **Examples:**
  - ✦: advance 0.6, ascent 0.965, descent 0.285 (Adwaita Mono's typo metrics; hhea says 0.215 plus a 0.07 gap).
  - ⚔: advance 1.2695 (Noto Emoji).
  - ◌: advance 0.5942 (Liberation Serif).

**For step 7.**
- The icon face draws the table's code points before any font (decision 1).
- `icons.json` carries Chromium's metrics. The engine's `FontFace` reads FreeType's hhea values, so for Adwaita Mono it
  would say descent 0.215 where Chromium says 0.285. This is an engine finding, left as it is (the game-screen tests
  pass), but it matters for any line where an Adwaita Mono fallback glyph sits with `line-height: normal`.
- ◆ and ★ also appear in the map's inline SVG as `<text fill="#fffaf5" font-size="1.25rem">` on the guild markers
  (`map.js` 545), scaled with the map. The engine's SVG text needs the icons too.
- The sheet's race sigil ✦ is turned −11° (`styles.css` 239); the sheet shows it so, and the turn comes from the
  element's transform.
- ▲ ▼ came from Liberation Sans Bold, through Arial, which the port doesn't have; their boxes are that font's.
- The sheet's fixture `Tools/IconSheet/uses.json` lists every use with its font, colours and Chromium's advance, so step
  7's icon fixtures can be built from it.

**At the smallest sizes** (the sheet's `-small-` pages):
- ↻ at 12 px bold, ⇧ at 10 px, and ▲ ▼ ◆ ◎ ⚙ at 9 px read as much as the old glyphs did, lighter.
- ✸ at 9 px is a brighter dot with the heavier line than it was without it, though smaller than the old glyph's;
  neither the old glyph nor the icon shows a star at that size.
- ✦ (Skills) and ✧ (Spell book) sit side by side in the field tools at 18 px. ✧ is plainly open; it lost the inner
  lines it had in a draft, which made it look like ✦. ✦ is partly filled.
- ✺ was redrawn as solid pointed petals; rays round an empty middle had read as a ring at 16 px.

### Step 4: the sounds (2026-11-13, the branch `gm1-step4`)

The eight interface sounds (GM1-6, GM1-11, Q13, Q14, Q22) as effect patches, made from the engine's existing nodes and
rules (nothing changed in `Engine/Statventure/Audio/`), chosen by the user by ear; `Docs/Audio.md`, "The game's sound
effects", has their layers, the measures and the checks.
- **Candidates first:** for each sound three candidates (a, b, c) that differ in what a listener notices (brighter or
  duller, shorter or longer, more noise or more tone; for `game-deleted`, which Q14 gave no image of its own, three
  images: a page torn, a name struck through, a sheet crumpled), each voice under 0.3 s. Each was rendered in Chromium's
  OfflineAudioContext from the effect fixtures' own script at 48 kHz, measured at the first start's level (effects 60 %,
  master 100 %: the envelope, the octave bands and centroid, the peak, the loudest 100 ms K-weighted and A-weighted), and
  its patch level set so the mean of the two loudness measures is −31.5 dB, `ui-press` −34.5 (the most heard, the
  quietest). The user listened with `play-sounds.sh --candidates` and to the WAV files of the renders.
- **The user's picks (2026-11-13), one line each, the candidate in the scratch folder `claude-work/gm1/step4/candidates/`
  each source is a byte-identical copy of:**
  - `ui-press` = `ui-press-b`: the brighter, shorter scritch (noise at 6 kHz and a falling 3.8 kHz sine, 12 ms).
  - `ui-toggle` = `ui-toggle-c`: one softer, lower stroke (noise swept 1.2 → 2.2 kHz, 70 ms).
  - `ui-open` = `ui-open-c`: the crisp, short crinkle (noise at 3.5 kHz with three crackles, 80 ms).
  - `ui-close` = `ui-close-c`: the crisp, short fold (noise swept 4.5 → 2.2 kHz, a crackle, a soft pat, 70 ms).
  - `game-saved` = `game-saved-a`: the seal thump (a sine 150 → 62 Hz, a low press and a contact, 160 ms).
  - `game-opened` = `game-opened-c`: the heavy page (a slow swish 0.7 → 2.5 kHz and a deep flap, 283 ms in all).
  - `ui-refused` = `ui-refused-c`: the tighter, harder knocks (two at 300 Hz, 80 ms apart).
  - `game-deleted` = `game-deleted-c`: a sheet crumpled (five irregular crackles over a soft bed).
  The rejected candidates stay in the scratch folder, out of the repo. The levels are as heard: each pick already met
  the set's target, so none changed.
- **Where they are:** `Assets/Audio/effects/<name>.json` (each the record's JSON form exactly as `RecordSerializer`
  writes it); `audio.pak` made again by `make-music-data.sh` (GM1-P16: its importer reruns in about a second, headless,
  and gives the committed pack back byte for byte, fresh or reusing its entries, so no `make-sound-data.sh`; the script
  now copies the sources in as `audio/effects/<name>` and refuses an importer that writes that folder itself): 126
  entries. The audio system registers them under their names at the start (S6-4), for step 8's `GameUi.Sound(name)`.
- **`play-sounds.sh [name …] [--volume 0-100] [--candidates <folder>]`** (`Tools/PlayTheme --effect`, the public API):
  every effect in `audio.pak` by name when no names are given; each name printed, played on the effects bus, 1.2 s
  after its end the next; the effects volume, 60 by default, the master at 100; `--candidates` kept for the later
  milestones' approvals.
- **Checks:** a fixture per sound against Chromium (`AudioFixtures.GameSound`, M8's rule: exact up to two layers, the
  shuffled-sums rule with three or more; renders `Golden/audio/<sound>.f32.gz`, the same as the candidates' renders the
  user heard, three of them within Chromium's summing order, 3e-8); `GameSoundsTests` (the sources are the eight, each
  checked, in its JSON form, under 0.3 s; the pack holds exactly them; each registers under its name and, played by it,
  sounds and is over within 0.3 s); the two scripts' rules by runners of the step's own.
- **Tests outside the audio fixtures brought to the pack's new content** (the manager's leave, 2026-11-13, for that
  only): `Audio/MusicLibraryTests.TheCommittedPackHoldsTheGamesMusic` (the effects are the eight, 118 + 8 entries),
  `Audio/AudioMarksTests.TheCommittedPackPasses` (126 entries); `Editor/Opening/EntryListTests` (the game's `audio`
  folder now lists `effects` first: `TheTreeListsThePacksEntriesOverTheGamesByPath`, `TheKeysMoveThroughTheTreeAndEnterOpens`,
  `TheFilterKeepsWhatMatchesByPathOrKind`, `OnlyTheRowsNearThePanesViewAreMade` 312 → 313 rows); the ten editor screens
  whose entry list shows the game's `audio` folder (checks-problems, import-again, import-path, import-replace,
  list-marks, preview-cutscene, preview-effect, preview-model, references-list, saving-mod-info): their markup in
  `Reference/editor-fixtures.json` as the tests write it and their 20 Chromium goldens `Golden/editor-<name>@1.png` and
  `@1.65.png` captured again headless (only the `effects` row and what it moves differ).

### Step 2: the game program and the real data library in place of the stand-ins; the seams (2026-11-13)

**GM1-P4's reason, probed first** (`claude-work/gm1/step2/probe-p4/`: a program referencing both the editor and the
library's project, run both ways round): a direct reference and the editor's loader in one process each list the 81
record kinds, but whichever then uses its `stand-in.item` type second (written to its binary form and read back,
`RecordSerializer`) is refused: "Statventure.Game.Data.StandIn.StandInItem and Statventure.Game.Data.StandIn.StandInItem
are both named stand-in.item" (`RecordFamily`'s one kind per name). So the game's tests, which use the library's types
directly, can't share a process with the editor's tests: a second test project, as decided.

**The projects (GM1-P1).** `Game/Statventure.Game/` (an `Exe`, assembly and root namespace `Statventure.Game`; the runtime
publish settings and `KeepTheDataLibraryBeside` the sandbox's project had; `InternalsVisibleTo` the game's tests) and
`Game/Statventure.Game.Data/` (moved with `git mv` from `Samples/`; `Samples/Statventure.Game.Data/` is gone), in the
solution's `/Game/` folder; both take their files by the SDK's wildcards (nothing listed). The library holds
`StandIn/StandInItem.cs` (namespace `Statventure.Game.Data.StandIn`; record, fields and `[Kind]` unchanged; its help now
"A development-only test kind, not part of the game: it goes when the game's first real kind arrives (GM2).", GM1-P3) and
step 5's stub `Saves/SaveGame.cs` (`Statventure.Game.Data.Saves.SaveGame`, a plain record with `Name`, no attributes:
not a record kind yet, so the editor and the packer still count 81 kinds). `GameLibrary.RepoProject` is
`Game/Statventure.Game.Data`; the packer, `editor.sh`, `editor.ps1` and the engine's test project name the new path.

**The program's own files** (the root, step 2's): `Program.cs` (`Main`: the options, the single instance, the game run),
`StatFickGame.cs` (the application class), `GameParts.cs`, `IGameScreen.cs`, `GameOptions.cs`, `SingleInstance.cs`,
`GameWindow.cs` (the window and the sound as the class uses them: `IGameWindow` with the engine's `EngineGameWindow`,
`IGameVolumes` with `AudioVolumes`; internal, so the tests give their own) and `Sounds/SoundNames.cs` (final). **The
stubs**, each in its part's folder, each the step's that fills it from now on: `Settings/GameSettings.cs`,
`Session/GameSession.cs`, `Random/GameRandom.cs`, `Mods/GameMods.cs`, `Icons/GameIcons.cs`, `Ui/GameUi.cs`,
`Screens/{Start,Sheet,Menu,About,Settings,Slots,Picker}/<Name>Screen.cs`.

**The seams as built** ("The seams" is the contract; where the outline couldn't be written as it stands, the nearest
shape, each listed here for the steps that fill them):
1. `GameParts` is a positional record `GameParts(Settings, Session, Mods, Ui, Random, Game)`; "the application" is
   `Game`, typed `StatFickGame` (its window, storage, data, sound, `Quit()`).
2. `GameSettings.Load(GameStorage)` and `GameUi.Load(Application, GameSettings, VirtualFileSystem)` are static factories
   giving the part (the outline's `Load(…)` with no owner). `GameSettings.Peek(gameFolder)` gives a
   `SettingsSnapshot(WindowState? Window, bool Fullscreen, IReadOnlySet<string> ModsOff)` with `IsModOn(file)` and
   `SettingsSnapshot.Defaults`. `GameSettings.Window` and the snapshot's `Window` are `WindowState?`: null while the
   window was never kept (the design's "absent at first"), the application's own 1440 × 950 then
   (`StatFickGame.FirstWidth`, `FirstHeight`). The typed values have setters raising `Changed(SettingKey)` when the value
   changes; `SettingKey` has eleven keys (the three volumes, `Theme`, `ZoomLevel`, `Fullscreen`, `Window`, `DevMode`,
   `LastCharacter`, `Mods`, `RecentPlaces`). The stub: the first start's values in memory (Q22, GM1-4, GM1-12), nothing
   read or written.
3. `GameMods.ApplyTo(ApplicationOptions, SettingsSnapshot)` is an instance method (the check's results stay with the
   object for `Entries`), and the object has one more member than the outline: `Attach(GameSettings)`, called at `Load`
   once the settings are made, so `Switch(file, on)` keeps the switch in them (the outline's "`GameMods` … `GameSettings`'
   switches"). `ModEntry(File, Name, Author, Version, Description, State, Problem, MoreProblems, SwitchedSinceStart)`,
   `ModState { Loaded, Off, LeftOut }`. The stub: the options unchanged, no entries.
4. `GameSession(GameStorage, GameSettings)`: `Slots()` gives `SlotInfo(Slot, Name, LastPlayed, Damaged)`;
   `NextDefaultName()`; `Current` (`SaveGame?`), `CurrentSlot`; `New(name)` gives the slot; `Open(slot)` gives
   `OpenResult(Slot, Opened, Backup, BackupTime)` (`BackupState { Missing, Readable, Damaged }`); `UseBackup`,
   `StartNew`, `Delete`; `Change(Func<SaveGame, SaveGame>)`; `Leave()`; `Export(file)` gives whether it was written;
   `Import(file)` gives `ImportResult(Slot, Problem)`; events `Opened`, `Closed`, `Saved`, `SlotsChanged` (plain
   `Action`). The stub works in memory (a placeholder slot naming; files refused with "That file is not a valid
   character save").
5. `IGameRandom` (`double Next()`, `int Next(int n)`), `GameRandom(ulong seed)` with `Seed`, static `FromSystem()` (the
   stub: `FixedSeed`, 0x5EED, .NET's generator), `ScriptedRandom(IEnumerable<double>)` (fails when its draws run out).
6. `GameIcons` is a static class: `Table` (code point → `GameIcon(Outline, Advance, Ascent, Descent)`), `AddTo(FontLibrary)`.
7. `GameUi`: `Document`, `Host` (null without a window); `Windows` (`GameWindows`: `Open(UiElement)`, `Close(UiElement)`,
   `CloseTop()` → whether one was open, `Top`); `Commands` (`GameCommands`: `Register(name, Action<string?> run,
   Func<bool>? enabled)`, a second registration of a name throwing; `Run(name, argument)` → whether it ran, a name not
   registered or not possible now logged and nothing done; `IsRegistered`, `IsEnabled`, `Names`); `Dialogs`
   (`GameDialogs`: `Task<bool> Confirm(question, yes)`, `Task<string?> Prompt(question, value, ok)`); `Status(text,
   StatusKind)` with `event Action<string, StatusKind> StatusShown` (`StatusKind { Info, Problem }`); `Sound(name)`;
   `Update(double)`, `Render(Frame, GpuImage)`, `Dispose()`. The keys are step 8's: the stub has no binding API, and
   step 8 adds it (the application class doesn't call it). The stub: a bare document, "Stat Fick" on the Gothic ink
   theme's parchment (`#d5c39b`, ink `#22160d`) in the engine's Lato; windows only kept in a list; commands as above;
   dialogs answered Cancel; no keys, theme, zoom or sound.
8. A screen is an `IGameScreen` (`Attach(GameParts)`); the application makes one of each, `StatFickGame.NewScreens()`,
   attached in this order: Start, Sheet, Menu, About, Settings, Slots, Picker.
9. **The About stub isn't empty:** it registers `about` with the engine's dark credits view for "Stat Fick" (a
   full-window layer in the game's document with a Close of its own; logged "[Game] the credits view opened"), so
   `--about` is "run `about`" (GM1-P12's "the engine's credits view until step 9") and step 9 replaces only its folder.
10. Quit and the window's close both leave the character through one `Session.Leave()` in `Unload` (which every end of
    the loop reaches, `--capture`'s quit too): `Application.Quit()` isn't virtual, so step 8's `quit` command calls
    `parts.Game.Quit()`, and `ConfirmClose` keeps the engine's default (agrees at once).
11. The window's maximized and full screen states are put in place at `Load` (below); the class reaches the window and
    the sound through `IGameWindow` and `IGameVolumes`, and the tests through `Compose` and `SettingsLoaded` (internal).
12. `--open <screen>` runs `open-<screen>`; `--open start` runs `start-screen` (`GameOptions.OpenCommand`).
13. Namespaces: the folders' (`Statventure.Game.Settings`, `.Session`, `.Random`, …). Inside the game's namespaces
    `Random` names the folder's namespace (write `System.Random`) and `Settings` the settings' (the engine's
    `Statventure.Storage.Settings` by its full name).

**The start sequence** (`StatFickGame`; "Who calls whom"): **before `Run`** `StatFickGame.Create(options, gameFolder)` peeks
the per-user folder's settings, makes the engine's options (`OptionsFor`: titled "Stat Fick", the remembered size or
1440 × 950, at least 820 × 600, resizable, logging as `stat-fick`, the per-user folder given; splash and sound on) and
passes them through `GameMods.ApplyTo`; `CrashCapture` is `--capture`'s file, and `--capture-splash` asks the next frame.
**At `Load`** the command line's problems and the single instance's are logged, the engine's window and sound adapters
made, then `Compose(Storage, Data)`: `GameSettings.Load`, `GameMods.Attach`, `new GameSession`, `GameRandom.FromSystem()`
(logged "[Game] the run's random seed <n>"), `GameUi.Load(this, settings, data)`, the parts (kept at once, so a start
command can reach them), every screen attached, the volumes put on the sound, maximized and full screen on the window
when the settings say so, `Settings.Changed` followed (the volumes, full screen, and a maximized state written by
anyone else), then `start-screen`, then `--open`'s command, then `about` for `--about`. **Each update:** `--crash` or
`--crash-thread` at frame 0 (thrown on the main thread, or on the thread "Game crash"); a later start's request taken
(below); the window's state kept; the document updated; `--capture` at frame N (the loop's, after the splash), then
quit. **`Unload`:** the character left, the document disposed. No theme is started (Q15) and nothing reloads (Q18).
With the stubs a start logs "[Commands] start-screen: not registered yet, nothing done" and draws the parchment page.

**The window (Q19).** The engine's `WindowOptions` have a size, a minimum and visibility, but no maximized or full screen
state, so those are put in place at `Load`, after the splash (a jump after it, as "Left to the steps" foresaw; the
engine unchanged). `EngineGameWindow` uses the engine's `Window.Fullscreen` and GLFW's own window (`Window.Native`:
`Maximize`, `Restore`, the `Maximized` and `Iconified` attributes, `Focus`, `RequestAttention`). Each update writes the
window's state to the settings when neither minimized nor full screen: its size while not maximized, and the size it is
restored to with `Maximized` when it is (the settings say a change only when the value changes). A `Window` change
is put on the window only when its maximized state differs from the window's, so the class's own writes (taken from the
window that moment) never move it.

**One instance per per-user folder (GM1-P14; `SingleInstance`).** `Program.Start` claims the folder before anything else:
the folder made if missing (each level 0700 on Linux, as the engine's storage makes them), an exclusive lock on
`stat-fick.lock` there (`FileShare.None`, which .NET takes as `flock` on Linux; the file 0600, left in place, the lock
released with the program, a crash too); the first then listens on a channel named `stat-fick-<16 hex digits of the
SHA-256 of the folder's real path>` (links resolved, so `/home/…` and `/var/home/…` are one; capitals on Windows): a Unix
socket in Linux's abstract namespace (no file, nothing left by a crash), a named pipe on Windows (current user only). A
later start that finds the lock held (Linux: `EWOULDBLOCK`, 11, which .NET gives as the exception's HResult, checked on
the PC; Windows: a sharing or lock violation) connects, says `forward`, waits for `ok` (trying for 3 s, the first may be
just starting), prints "Stat Fick is already running for this player: its window was asked to come forward." and ends
with 0 (unanswered: "… but it didn't answer.", 0). The first's listener thread raises `ForwardAsked`; the game notes it
and wakes a sleeping loop (`Window.PostEmptyEvent`), and its next update restores the window if minimized, asks the
focus and, on Wayland, GLFW's attention request, logged "[Game] another start asked for the window: focused, attention
requested (Wayland)". Anything else on the channel gets nothing. A folder that can't be made or locked for another
reason runs alone, the problem logged at `Load`. **Checked** with the dev layout and the packaged game, headless (two
starts in one cage): the later start ended with 0 in 0.05–0.14 s with its line, the first logged the request, and a start
on another per-user folder ran beside.

**The command line (GM1-P12; `GameOptions`):** `--capture <png> [--frames N]` (30 by default; N a whole number from 1),
`--capture-splash <png>`, `--about`, `--open <screen>`, `--crash`, `--crash-thread`; paths made full; no usage text; a
wrong argument (unknown, a missing value, a bad `--frames`) is ignored and logged as a warning once the log is open.

**The sandbox (GM1-P2):** its project has no reference to the data library and no publish settings; it logs as
`statventure-sandbox` (its cutscene player too; the engine's log rotation takes any program's name, so no engine change);
its title, `--cutscene`, `--log-visibility`, capture, About and crash options are as they were; `play-cutscene.sh` is
unchanged. Checked headless on scratch per-user folders, nothing audible: `play-cutscene.sh jiruan --volume 0 --dump`
wrote its 85 frames and 6 stills ("sound … at volume 0"), and `--log-visibility --capture` logged its timer, `pagehide`,
`visibilitychange` and `unload` and captured its page, each run's log `statventure-sandbox-<date>_<time>.log`.

**The editor and the packer on the real library:** `editor.sh --player-folder <scratch> --capture` loaded
`Game/Statventure.Game.Data/bin/Release/net10.0/Statventure.Game.Data.dll` (81 record kinds) and drew its shell; the
editor with `--game` on the dev layout (`$STATVENTURE_GAME`) loaded the library there; the packer packed and dumped a
`stand-in.item` entry.

**Build, test, publish.** `build.sh` and `build.ps1` publish `Game/Statventure.Game` into `$STATVENTURE_GAME`
(framework-dependent, S9-A) and then remove the sandbox's leftovers there: its program files (`Statventure.Sandbox`,
`.exe` on Windows, `.dll`, `.pdb`, `.deps.json`, `.runtimeconfig.json`) and its `Data/Fonts`, `Data/Cutscenes`,
`Data/Models`, and `Data/` when nothing else is in it; nothing else of the folder (chosen: by name, as the sandbox laid
them, rather than `Data/` whole). `test.sh` and `test.ps1` run `Tests/Statventure.Tests` then
`Tests/Statventure.Game.Tests` with the same arguments, the second also after the first failed, and fail if either did.
`mutate.py` takes a mutation's `"project"` (default the engine's tests), builds and tests it, and rebuilds every
project it used at the end. **Packaging:** `Publish.cs`'s `stat-fick` package is `Game/Statventure.Game` (assembly
`Statventure.Game`, renamed `stat-fick` / `Stat Fick.exe`); `check_packages.py` reads the game's version from
`Statventure.Game` inside the bundle and now fails when a program's own assembly isn't in its bundle (before, a wrong
name there skipped the version rule unseen: found by this step's breaks); the game's two listings made again (the sandbox's `Data/` gone, the `bundle` lines
`Statventure.Game.*`; the editor's unchanged); `licenses.txt` without the stand-in's `Data/` lines;
`release-text.md`'s first line and the game's bullet reworded (below) and `release_test.py`'s required phrases with them.
**`end_to_end.py`:** the other version's library built from `Game/Statventure.Game.Data`, the page's colour the game's
parchment (`#d5c39b`), and the game's own words where the flow read the sandbox's: `[Game]` for `[Sandbox]` (the
splash-first check and the credits view's line) and the crash messages "the game crashed on purpose (--crash)" / "… on
a thread of its own (--crash-thread)" on the thread "Game crash" (the flow's structure unchanged; step 11 extends it).

**The release's text, reworded:** the first line "Stat Fick {{version}} for Linux and Windows: the game and its editor,
each a download of its own. **The game is being ported to the Statventure engine:** its download holds Stat Fick's own
program as far as the port has come, and there is nothing to play yet."; the game's bullet "`{{stat-fick-linux}}` for
Linux, `{{stat-fick-windows}}` for Windows: Stat Fick on the Statventure engine, as far as the port has come." (true for
every step of GM1; the user sees it in the next draft).

**The packages (`2026.11.13.1`, from this step's tree rebased on step 4's `8acdc5a`):** the game's Linux archive 42.6 MB
(44,717,413 bytes, 54 files; was 45.1 MB, 72), Windows 38.8 MB (40,696,827 bytes, 47; was 41.3 MB, 65), the editor's 41.8 MB (51) and 38.0 MB (44) as
before; `check-packages.sh` ok on all four (the Linux two run headless); its self-test: 119 breaks, 119 caught.

**GM1-P18.** *The publish through a symbolic link:* `env.sh` takes the scripts' folder by its real path (`pwd -P`; the
engine's and the repo's folders follow from it), and `Publish.cs` resolves `--engine` to its real path (every link in
it, a link to a link too). Shown on a clone of this step's tree under `claude-work/gm1/step2/clone`, reached through
`/home/…` (`/home` is a link to `var/home`): `publish.sh` worked and its archive passed the check; with both fixes taken
out it failed as M12 step 7 found (the program's restore without its project references: "error CS0246: The type or
namespace name 'Brutal' could not be found"); either fix alone was enough for `publish.sh`; `Publish.cs` called directly
with the `/home/…` path (as `publish.ps1` calls it, without `env.sh`) failed without its fix and worked with it.
*`build.sh` after a failed shader:* the repro run again (`claude-work/gm1/step2/shader-repro.sh`: `test_color.frag`
broken, `build.sh` run, the shader put back): **`build.sh` ended with exit code 1** right after "shaders: 0 built, 30 up to
date, 1 failed", with no MSBuild output after it: the build stops (`set -euo pipefail`), so `build.sh` is unchanged.

**Tests** (`Tests/Statventure.Game.Tests/`, a project of its own, GM1-P4; it references the engine, the game and the
library, links `PerUserFolders`, `ReferencePlatform`, `SystemFonts`, `GpuFixture`, `Golden`, `TestPaths` and
`Canvas/TestFonts` from the engine's tests (TestPaths beside the six named: the committed packs and the old game's
folder for later steps), copies the engine tests' `Data/Fonts` beside it for `TestFonts`, and takes its own `Data/**`):
29 tests. `GameOptionsTests` (7: a player's start, each option, `--open`'s commands, a wrong command line said and
ignored); `SingleInstanceTests` (9: a later start forwarded and the first told, folders of their own, a start after the
first ended, one folder through a link, a first that doesn't answer, only the game's question and answer counting,
the folder and lock file private and a folder that can't be made, a later start ending with 0 without a game, the first
running the game and reached by a later start); `StartSequenceTests` (10, in memory on the stubs: the options before
`Run`, the parts made and every screen attached before `start-screen`, a command not registered logged, a command run
when registered and possible, the command line's commands after the start screen, About's credits view until step 9,
the volumes, full screen and the window following the settings, the settings at the start put on the window, the
window's state kept, a later start's request taken once); `GameRunTests` (1: a real run in a hidden window on the stubs,
no log or sound, on a thread of its own with a 60 s limit, so a run that doesn't quit fails rather than hangs: the window
titled "Stat Fick", the engine's window and host, the first frame captured, the parchment page with the name, the
request taken in the update, the window's state kept, the character left at the end);
`Sounds/SoundNamesTests` (2: the eight names, and the committed `audio.pak` holding a patch at each name's path and no
other effect, which step 4 left for the program). The engine's project gains or loses nothing (3720 on the step's base,
3738 on `main` after step 4; nothing moved; `EditorArgumentsTests`' library path and a few comments changed).

**Shown able to fail.** *The program's rules* (`mutate.py` with the game's project, under the shared lock in a headless
cage; `claude-work/gm1/step2/mutations.json`, made by `make-mutations.py`): 100 mutations of the options, the single
instance (`SingleInstance`, `Program.Start`), the window's options, the start sequence's wiring and the settings
followed, the window's state kept, a later start's request, the run, and the stubs the program runs on (the page, the
commands, About until step 9, the sounds' names). The first run (paused after 21 and finished from the 22nd,
`mutations.json.log`, `mutations-rest.json.log`): 95 killed, 4 survived (two of them the stubs', below), 1 didn't
build. Of those: "the screens not
attached" hung the real-run test (its quit is a screen's; a hidden window's presents hang under wlroots), which
`mutate.py` reads as a survivor: the run test now runs the game on a thread of its own with a 60 s limit; "the class's
own window write put back" survived because the guard was redundant (a `Window` change reaches the window only when its
maximized state differs from the window's, and the class's own write takes the window's at that moment): the guard
removed, and a mutation of that condition listed instead; "`--about` not run" didn't build (rewritten). The rerun
(`mutations-rerun.json`: those three and the 11 mutations the run test covers, the test having changed): 14 of 14 killed.
The standing list: 100, **98 killed, 2 accounted for**: the options not passed through `GameMods.ApplyTo` and the settings
not peeked (`GameSettings.Peek`) can't differ while those parts are stubs (the options come back unchanged and the
snapshot is the defaults; steps 6 and 5 make them differ, and their tests reach the wiring). *The scripts' rules* (a
runner of the step's own, `script-rules.py`: the worktree's `build.sh`, `test.sh`, `editor.sh`, `env.sh` and `mutate.py`
copied into a scratch engine with a stand-in `dotnet`, each checked in a second): the game published into the game
folder and the sandbox's leftovers removed and nothing else, both test projects run with the arguments and either's
failure kept, `editor.sh`'s library, `env.sh` through a link, `mutate.py`'s project; 25 mutations, **25 killed**. *The
sound pack test* (added after the rebase on step 4; `mutations-sounds.json`): a name not in `audio.pak`, the patches
looked for elsewhere, a name left out of the list: **3 killed**. *The packaging, for real* (`packaging-breaks.py`,
`check-break.py`): the stat-fick package's project back to the sandbox (its publish refused: no runtime pack, the
licence check), the release text's old wording (`release.sh --self-test`: 30 of 31, its text test failing), both
**killed**; the check's game assembly back to the sandbox's **survived** at first (the check skipped the version rule
when the named assembly wasn't in the bundle: a gap, closed by the new rule above) and was **killed** after it. *The
flow* (`e2e-breaks.py`: each of `end_to_end.py`'s changed expectations put back to the stand-in's, the flow run on the
GPU): the page's colour, the other version's library's project, the credits view's line, the crash's words, the crash
thread's name: **5 killed**, each on its own line. Nothing written to the player's real folders.

**Runs** (all inside headless cages, scratch per-user folders, nothing audible; on the step's commit rebased on step 4's
`8acdc5a`): `check-packages.sh` ok on the four archives (the Linux two run), its self-test 119 breaks, 119 caught;
`end-to-end.sh` 157 of 157 on the GPU (91 s) and on lavapipe (96 s); the full suite on the GPU (the engine's tests
1 min 59 s) and on lavapipe (2 min 18 s): 3738 + 29 = 3767 tests, none skipped.

**Not checked:** Windows (the named pipe, `build.ps1`'s leftover removal, `test.ps1`, `editor.ps1`; "Windows check");
the single instance on a desktop with a real window (raising and focusing it, Wayland's attention request seen); the
splash then maximize/full screen jump in a real window.

### Step 5: saves, settings and the randomness service (2026-11-13)

**Decided on the way.**
- **The user (2026-11-13, the question tool, asked by the manager; "The detail round"):** Q25, "Start new" on a slot
  whose save and backup both can't be read names the new character by the slot's file name (not the recommendation, a
  prompt); Q26, the editor's New entry leaves `save.game` out, the record marked not a pack kind.
- **The manager (2026-11-13):** a slot whose save can't be read but whose backup can carries the backup's name and is
  still damaged (step 5's reading, confirmed; the model says where the name came from); a refused character file's words
  are "That file is not a valid character save" for every wrong file and "That file is not a valid character save: <the
  check's message>" for a `save.game` its check refuses; no engine change for `Settings`' missing `Remove` (below);
  `GameLookupTests`' count of the library's kinds (the engine's + 2) and, for Q26, the mark, `NewKinds()` and its tests
  given to this step.

**The save record** (`Game/Statventure.Game.Data/Saves/SaveGame.cs`): `[NotAPackKind] [Record("save.game", 1)]`, the
field `name` (1, `""` by default, its help "The character's name, as the slot list shows it."), no `[Kind]`.
`SaveGame.NameProblems(name)` (and `NameProblem`, the first) are its `[Check]`'s rules, each reported at `$.name` as a
phrase naming the name: "the name is empty" (empty once trimmed, .NET's `Trim`: Unicode white space), "the name has 81
characters, more than 80" (counted in UTF-16 units, as the old name field's `maxlength`; `MaxNameLength`), "the name
holds a control character (U+0007)" (`char.IsControl`: C0, DEL and C1; the first one only). Its layout signature is
`save.game 1` / `1 name: string = ""`; the version-1 fixture `ser-alrik` (both forms) is the design's character file byte
for byte; the JSON form keeps non-ASCII but escapes characters outside the BMP (the records' writer).

**The library's layout guard** (`Tests/Statventure.Game.Tests/Saves/LibraryRecords.cs`, `LibraryRecordLayoutTests`;
data in `Tests/Statventure.Game.Tests/Data/Records/`): the engine's guard (`RecordLayoutTests`) for the data library's
record types, by the serializer's public API only (the game's tests don't see the engine's internals: a fixture's type
table is read from the file itself). The development-only `stand-in.item` is left out (it never had a layout or fixture
and goes in GM2). `STATVENTURE_RECORD_FIXTURES=1` writes what is missing, never an existing file; so GM2's version 2
keeps `ser-alrik` as version 1's fixture and adds its own.

**The mark (Q26):** `NotAPackKindAttribute` (`Engine/Statventure/Records/RecordMarks.cs`) on a record type,
`RecordShape.IsPackKind` (false when marked), and `EditorSession.NewKinds()` leaving such kinds out
(`Tools/Editor/Session/EditorSession.Drafts.cs`). Nothing else reads it: layouts, forms, the packer and the checks are
unchanged, and no editor screen changed (the stand-in kind is still offered; the editor's fixtures came out the same).
**Looked at, changed nothing (the manager's word):** the editor's Import takes a record's `.json` form of any kind the
session knows, so a character file (`stat-fick-character.json`) imported into a mod becomes a `save.game` entry at
`stat-fick-character` (a kind without a folder: its fitted name at the top) and passes its check; Duplicate copies any
entry with a kind, such an entry too; `packer pack` packs a `save.game` JSON form like any record; and step 6's check
will see `save.game` as a kind the game knows.

**The settings** (`Game/Statventure.Game/Settings/GameSettings.cs`), `settings.json` version 1 through the engine's
`Settings` (`GameStorage.LoadSettings(1, no upgrades)`):
- **Reading:** each key with its first-start default (`masterVolume` 1, `musicVolume` 1, `effectsVolume` 0.6, `theme`
  dark, `zoomLevel` 0, `fullscreen` false, `window` none, `devMode` false, `lastCharacter` none, `mods` all on,
  `recentPlaces` none). A missing key, a null or a wrong type gives the default (the engine's `Get`, which logs
  `[Storage] settings <file>: "<key>" holds <value>, not a <type>; its default is used`); a value of the right type out
  of its range gives the default too, logged here: `[Settings] settings <file>: "<key>" holds <value>, not <range>; its
  default is used`. The ranges: the volumes 0 to 1; the zoom level a half step whose zoom (1.2 to its power) is from 0.5
  to 3 times (−3.5 to 6), as the editor's; the window whole numbers, its width from 820 and its height from 600 (the
  window's smallest) to 16384 each (chosen: a swapchain's common limit; no screen shows a larger window, and a size the
  device can't make would stop every start); `lastCharacter` a slot's name under the engine's rule. The theme is
  `"dark"` or `"light"` (the engine's enum names; a number refused); `mods` an object of `{ "on": false }` objects (a
  missing `"on"` is on, an entry of another shape makes the whole value the default); `recentPlaces` a list of strings
  read through the engine's `RecentPlaces` (ten, full paths, repeats and non-paths dropped without a line). Unknown keys
  are the engine's to keep.
- **Setting:** a set that changes a value writes its key (the engine's `Set`, by the background writer) and raises
  `Changed` with its key; a set to the value it has does nothing (so a wrong-typed value stays in the file until its
  setting changes, Q43 of M9). The setters refuse what can't be kept: a volume outside 0–1 or NaN, a zoom level the game
  hasn't, an undefined theme (`ArgumentOutOfRangeException`), a `LastCharacter` that isn't a slot's name
  (`ArgumentException`), a null `Window` (`ArgumentNullException`: once kept it isn't cleared). A `Window` set is held
  within its limits (chosen over a refusal: the application writes the real window's state each update, and a kiosk
  compositor may make a window smaller than its minimum; what is kept always opens at the next start). The mods'
  switches are written as `{ "<file>": { "on": false } }` for each one off, in the files' ordinal order, `{}` when none;
  `IsModOn` compares file names exactly.
- **`LastCharacter` cleared (the engine's `Settings` can't make a key absent):** the engine has no `Remove`, and its `Get`
  logs a stored null as a wrong type at every start. So clearing `LastCharacter` (a deleted slot's) changes it in memory
  and raises `Changed`, and writes nothing: the file keeps the deleted slot's name, which names no slot, and a later
  write of another setting keeps it; the session takes a `lastCharacter` that names no slot as none when it starts
  (in memory). A new character in a slot of that name sets it anew. The engine item is open in `Docs/Engine-Plan.md`,
  "Small tasks left from earlier milestones" (the manager, 2026-11-13).
- **`Peek(gameFolder)`** reads `settings.json` before the engine's storage exists by the engine's own rules (a byte order
  mark allowed, repeated keys refused, an object with a whole-number `version` from 1 to the game's, the same upgrade
  table; the engine's JSON options for the values, copied: camelCase, enums by camelCase name, strict numbers) and the
  same typed readers as `Load` for the window, full screen and the mods; it writes nothing, makes no folder and logs
  nothing (a broken file is kept aside and logged by `Load`, once the log is open). The tests check that it reads every
  kind of file as `Load` does. `SettingsSnapshot` compares the mods' set by its contents (a record compares it by
  reference).

**The session** (`Game/Statventure.Game/Session/GameSession.cs`) on the engine's `SaveSlots`:
- **Events:** every write of a slot raises `SlotsChanged`; a write that makes, loads or changes the open character also
  raises `Saved` (the autosave dot: making and loading a character are changes, as the old game's `saveCharacter` after
  them), after `Opened` when it opens one. In order: `New`, `Import`, `UseBackup`, `StartNew`: (the open one left:
  `SlotsChanged`, `Closed`), `SlotsChanged`, `Opened`, `Saved`; `Open` of a readable slot: (the open one left), `Opened`
  (nothing written: a slot's last-played time moves when it is left); `Change`: `SlotsChanged`, `Saved`; `Leave`:
  `SlotsChanged`, `Closed`; `Delete`: (`Closed` if it was open, not saved), `SlotsChanged`.
- **`Slots()`:** every slot `SaveSlots.List` finds (its save or backup there), the last played first (ties by name),
  `LastPlayed` the save's write time (else the backup's) in UTC; `Name` the save's, else the backup's when it reads,
  else the slot's file name, with `NameFrom` (`Save`, `Backup`, `File`); `Damaged` when the save doesn't read (damaged,
  unreadable here, or missing beside its backup). It reads every slot's save on each call (small in GM1; GM5 measures a
  save with a battle in it).
- **`NextDefaultName()` (Q2):** "Character <n>" for the smallest n from 1 that no slot's character has (a damaged slot's
  by its backup's name) and whose slot (`character-n`) isn't taken, so the offered name gets the plain slot.
- **`New(name)`:** refused (`ArgumentException` with the check's words, nothing changed) when `NameProblem(name)` says
  so; else the slot from the name (below), the open character left (saved), the save written at once, `lastCharacter`
  set, opened.
- **`Open(slot)`:** a readable save opens (the open one left first); otherwise nothing changes and `OpenResult` gives the
  damaged-save question's facts: the backup's state (`Readable`; `Damaged` for a damaged or unreadable one; `Missing`),
  its file's time when it is there, and the name the question shows and Start new keeps (the backup's, else the file's
  name, Q25). "Back" is not calling anything.
- **`UseBackup(slot)`** saves the backup's value into the slot (the engine keeps the damaged save aside as
  `.dat.damaged`, `-2` … and leaves the backup as it is, Q39) and opens it; **`StartNew(slot)`** saves a new save under
  the kept name (cut to 80 characters if a slot's file name is longer, a `-` left at the cut taken off) and opens it. Both
  refuse a slot whose save reads (`InvalidOperationException`, nothing changed); `UseBackup` refuses a backup that
  doesn't read before leaving the open character.
- **`Delete(slot)`** closes it if open (not saved), removes its save, backup and a stale `.tmp` (the engine; files kept
  aside stay) and clears `lastCharacter` if it named it. **`Change(update)`** refuses a save its check refuses
  (`ArgumentException`, nothing changed) and saves; **`Leave()`** saves and closes.
- **`ContinueSlot()`** (an addition for step 9): `lastCharacter` while its slot is there (save or backup), else null
  (Continue greyed); the constructor drops a `lastCharacter` that names no slot.
- **`Export(file)`** writes the open character's JSON form (`CharacterFile.Export`, replacing a file there); false with
  no character open or when it can't be written (logged). **`Import(file)`** reads the file (`CharacterFile.Import`);
  anything that isn't a `save.game` this game reads (not JSON, empty, an old game's file, another kind, a newer version,
  an unknown key, a slot's binary file, over 10 MiB, missing, a folder) gives `NotACharacterSave`, "That file is not a
  valid character save" (Q23); one its check refuses gives "That file is not a valid character save: the name is empty"
  (the check's message); otherwise a new slot from its name (a number added when taken, Q9), the open one left (saved),
  opened. Nothing changes on a refusal.

**The slot's file from the name (Q1; `Session/SlotNames.cs`), the edge cases as decided:**
- Each character taken apart by its canonical decompositions and its accents (non-spacing marks) dropped, with HarfBuzz's
  Unicode data as `ImportNames.Fit` does (the program runs in invariant globalization), then lowercased: "Élise" and
  "Elise" are both `elise`, "Ångström" `angstrom`, the Kelvin sign K `k`.
- a–z and 0–9 kept; every run of anything else one `-`, `-` taken off both ends: `_` and `.` too (unlike `Fit`, so a slot
  never ends in a dot nor looks like an extension), and letters outside a–z after the accents are gone: "Weiß" `wei`,
  "Bjørn" `bj-rn`, Cyrillic or Chinese letters dashes.
- **A name with no letter or digit the rule keeps** ("日本", "!!!", "🜂"): `character`, then `character-2` ….
- **Names differing only in case or accents, or that meet in the rule** ("ser alrik", "Ser Alrik", "Ser Alrik!") share the
  stem: the later one gets the next number. The number only follows the stem: "Ser Alrik 2" made after a second
  "Ser Alrik" (`ser-alrik-2`) is `ser-alrik-2-2`.
- **Taken** means the slot's save or backup is there (`SaveSlots.Exists`: what the files say, so a case-insensitive file
  system counts as well); files kept aside and a stale `.tmp` don't take a name.
- **A name the rule would make a Windows device name** (`con`, `prn`, `aux`, `nul`, `com0`–`com9`, `lpt0`–`lpt9`, which
  the slot rule refuses): the numbering starts at `-2`, as if the plain name were taken: "Con" is `con-2`, "COM1"
  `com1-2`; "Con Man", "Com 1" and "Aux.txt" (`aux-txt`) aren't device names.
- **A very long name:** the stem is cut so that the slot with its number has at most 100 characters (the slot rule's
  limit), a `-` left at the cut taken off. A character's name has at most 80 characters and its stem never more than
  the name, so for the game's names the cut never happens; it keeps the rule total for any name.
- The first free of `stem`, `stem-2`, `stem-3` … is taken (a gap is filled).

**The randomness service** (`Game/Statventure.Game/Random/GameRandom.cs`): `xoshiro256**` 1.0, its state SplitMix64's
first four outputs from the seed (as the generators' author suggests; also rand_xoshiro's `seed_from_u64`), `Next()` an
output's top 53 bits times 2⁻⁵³, `Next(n)` `floor(Next() · n)` refusing n below 1 before drawing (an empty list has no
member to pick), `FromSystem()` seeded from the system's cryptographic source (any 64-bit seed), `Seed` kept (the
application logs it). `ScriptedRandom(draws)` hands its draws exactly as given (not checked to be in [0, 1), so an old
check's stub replays as it was), fails with "the scripted draws ran out", and says `Remaining`. **The reference:** Vigna's
`splitmix64.c` and `xoshiro256starstar.c` (prng.di.unimi.it) and rand_xoshiro's published reference outputs ("produced
with the reference implementation": SplitMix64's 50 outputs from 1477776061723855037, `xoshiro256**`'s 10 from the state
1, 2, 3, 4), saved in `~/.cache/stat-fick-build/reference-src/xoshiro/`; the tests check both lists, and the seeded
composite, whose state from 1477776061723855037 is the published SplitMix64 list's first four, against outputs of an
independent transcription of the reference code checked against both lists
(`claude-work/gm1/step5/xoshiro/reference.py`; compiling the downloaded C wasn't allowed in the session).

**The seams as built (where step 2's stubs changed; additions otherwise):**
1. `SlotInfo.Name` is a `string`, never null (the save's, the backup's, or the slot's file name), and `SlotInfo` has a
   fifth member, `NameFrom` (`SlotNameSource { Save, Backup, File }`).
2. `OpenResult` has two more members at its end: `Name` (the save's when opened; the question's and Start new's name when
   damaged) and `NameFrom`.
3. `GameSession` adds `ContinueSlot()`, the static `NameProblem(name)` and the constant `NotACharacterSave`; `New` and
   `Change` throw `ArgumentException` for a name the check refuses, `UseBackup` and `StartNew` throw
   `InvalidOperationException` on a slot whose save reads (and `UseBackup` when its backup doesn't). `StartNew(slot)` keeps
   its shape (Q25 needs no name from the screen).
4. `GameSettings` adds its keys, `Version`, the first volumes, the zoom's limits, `MaxWindowSide` and `IsZoomLevel(level)`;
   its setters throw for what can't be kept (above) and `Window` refuses null and holds sizes within limits;
   `SettingsSnapshot` equality by value.
5. `GameRandom`'s `Next(n)` throws for n below 1; `ScriptedRandom.Remaining`; internal `FromState`, `SplitMix64` and
   `NextBits` for the tests.
6. `SaveGame` adds `MaxNameLength`, `NameProblems(name)` and `NameProblem(name)`.
The application class, `GameParts` and the other parts compile unchanged; step 2's tests pass unchanged.

**For steps 9 and 10:** New character: the prompt filled with `NextDefaultName()`, its OK refused (`ui-refused`) while
`GameSession.NameProblem(text)` gives a problem, else `New(text)`. Continue: enabled while `ContinueSlot()` isn't null,
then `Open(it)`. The slot list: `Slots()` (newest first; `Name`, `NameFrom`, `Damaged`, `LastPlayed` in UTC for the
screen to show in local time); `SlotsChanged` to refresh it. The damaged-save question: `Open`'s `OpenResult` (`Name` for
"<Name>'s save is damaged.", `Backup` and `BackupTime` for "Use the backup (saved <time>)" only when `Readable`), then
`UseBackup`, `StartNew` or nothing ("Back"). Save Character: `Export(file)` → "Character saved" / "Character could not be
saved"; Load Character: `Import(file)` → `Slot` ("Loaded <file>") or `Problem` (the status line's words as given); the
picker's recent places: `Settings.AddRecentPlace(folder)` and `Settings.RecentPlaces`. Delete: `Delete(slot)`. The
settings screen's values and limits: the setters above (`IsZoomLevel` for the zoom's steps); the mods' switches through
step 6's `GameMods.Switch`. The autosave dot: `Saved`.

**Tests** (`Tests/Statventure.Game.Tests`, in the tests' root namespace: a test namespace named like a part's,
`…Tests.Random`, hides the game's `Random` namespace from step 2's tests): 171 new, the project 29 → 200.
`GameRandomTests` (8: both published lists, the seeding, a draw's bits, whole numbers and n refused before a draw, a
seed repeated, the system's seed, the scripted source); `SaveGameTests` (9: the record and its marks, good names, every
rule of the check with its words and path, both forms, a save file and a character file); `LibraryRecordLayoutTests` (7:
the guard over the library, the save's layout, its fixture in both forms, written back byte for byte, the design's
character file); `GameSettingsTests` (66: the first start writing nothing, every key read, 17 wrong types and 15
out-of-range values each defaulted with its line and left in the file, the ends of the ranges kept, unknown keys kept,
each set written with `Changed` and read back, a set changing nothing doing nothing, the setters' refusals, the window
held, `lastCharacter` cleared in memory only, the mods' switches, the recent places, 11 files that can't be used kept
aside (and peeked as the defaults, not moved), 10 files peeked as `Load` reads them, `Peek` writing nothing, the
snapshot's equality); `SlotNamesTests` (41: the rule and each edge case above); `GameSessionTests` (40: a new character
and its events, refused names, the next default name, the slot list with each kind of damage, opening, every damaged
case of Q17 and Q39 (a damaged save with a readable, damaged or missing backup; an unreadable save; an unreadable backup;
a save missing beside its backup; a stale `.tmp`; a readable save with a damaged backup; a save the system won't let be
read; damaged twice: `-2`), the answers refused on a readable slot, Start new's name cut, Delete, a stale
`lastCharacter` after a delete and a later write, Continue's slot, Change, Leave, Save Character, the Documents folder the
run's own, Load Character into a new slot, 10 wrong files and 3 unreadable ones with the general words, files the check
refuses (an empty name, none, a control character, too long) with their problem). The engine's project gains one (`SessionEntriesTests.NewLeavesOutAKindMarkedNotAPackKind`,
two variant kinds in `Editor/Form/KindMarkVariants.cs`) and `GameLookupTests`' count changes (the engine's + 2): 3739.

**Shown able to fail** (`mutate.py`, under the shared lock in a headless cage; `claude-work/gm1/step5/mutations.json`,
made by `make-mutations.py`): 169 mutations, the randomness service 32, the save 13, the mark 3 (two in the engine's
tests), the settings 58 (`Peek` 13 of them), the session 46 and the naming rule 17: **166 killed, 3 survived, each
because nothing can differ**: Peek's "a `version` that isn't a JSON number" check (kept as the engine's `Settings` has
it: `JsonValue.TryGetValue<int>` is already false for one), Peek's "repeated keys refused" option (`JsonObject` refuses a
repeated key itself when its properties are first read, as the engine's comment says, so such a file gives the defaults
either way) and the naming rule's taking of a decomposition's second part (a canonical decomposition's second part is a
combining mark, dropped anyway, or a Hangul jamo or an Indic vowel sign, a dash the first part already gives; no a–z
or 0–9 comes from a second part). Nothing written to the player's real folders.

**Runs** (inside headless cages under the shared lock, scratch folders, on the step's tree on `a7390ad`): the full suite
on the GPU (the engine's tests 1 min 57 s) and on lavapipe (2 min 12 s): 3739 + 200 = 3939 tests (`main`'s 3738 + 29 =
3767, and 172 new), none skipped; the editor's screen fixtures came out unchanged; the player's real folders unchanged.

**Not checked:** Windows (slot names on a case-insensitive file system, the device names in practice, `RecentPlaces` with
drive letters); the real Documents folder (the tests' resolver points into the run's scratch folder; step 10's picker
uses it).

### Step 1a: today's stylesheet in the engine and in `ui.pak` (2026-11-13, the branch `gm1-step1a`)

Today's `old-game/styles.css` brought three rules the engine refused or ignored (`@property`, `-webkit-text-fill-color`,
`background-clip: text`). They are ported from Chromium 152's source, each with a Chromium fixture at 1 and 1.65 of its
own; the port's stylesheet is then compiled into `ui.pak`.

- **Registered custom properties (`@property`).**
  - *Parsing:* `CssParser` keeps a `@property` rule as `CssPropertyRule` (its prelude and descriptors as written).
    `UI/Style/PropertyRegistration.cs` validates it as Blink does (`CSSParserImpl::ConsumePropertyRule`,
    `AtRuleDescriptorParser`, `PropertyRegistration::ConvertSyntax`/`ConvertInherits`/`ConvertInitial`):
    - the prelude is one custom property name;
    - per descriptor the last valid declaration counts; an `!important` one is invalid; unknown descriptors are ignored;
    - `syntax` is a string that `SyntaxDefinition` parses (a port of `CSSSyntaxStringParser`: `*`, or components between
      `|`; one of the fifteen type names, case-sensitive, or an identifier that isn't a CSS-wide keyword or `default`;
      `+` or `#`, but not after `<transform-list>`);
    - `inherits` is `true` or `false`;
    - `initial-value` is needed unless the syntax is `*`, and must parse for the syntax and be computationally
      independent.
    An invalid rule registers nothing, as in Chromium.
  - *The registry:* the style engine registers the valid rules of all sheets in order, a later one of a name winning
    (`StyleEngine::AddPropertyRules`). The registry and the initial style are made again when the sheets change.
  - *Computed values:* a registered `<color>` value is computed (`ComputeRegisteredPropertyValue`): `currentcolor` stays
    the keyword, and a function using `currentcolor` stays as written. Otherwise the value is replaced by the tokens of
    its serialized colour, so `var()` substitutes Chromium's text: `rgb(r, g, b)` or `rgba(r, g, b, a)`
    (`Color::SerializeLegacyColorAsCSSColor`, with alpha in two decimals when that is exact in floats, else three), or
    `color(srgb r g b[ / a])` for `color-mix()` and `color()` (six significant digits). `ValueReader` now reads
    `color(srgb …)` and `color(srgb-linear …)`.
  - *The cascade:* `CustomProperty::ApplyValue`, `ApplyInherit` and `ApplyInitial`:
    - an element without a value has the initial one;
    - a property that doesn't inherit starts from its initial value;
    - `inherit`, `initial`, `unset` and `revert` act as for any property;
    - a value invalid at computed-value time (not a colour, a `var()` cycle, a reference to nothing) takes the unset
      value;
    - a value that comes to one CSS-wide keyword after substitution is taken as that keyword.
  - *Animations and transitions:* a registered colour interpolates as Blink's `CSSColorInterpolationType`:
    premultiplied, in legacy sRGB's 0–255 parameters, `currentcolor` as the element's colour, the result computed. Any
    other custom property, and a registered one whose values aren't both colours, flips half way
    (`CSSDefaultInterpolationType`). Keyframe values are taken as `MaybeConvertCustomPropertyDeclaration` takes them:
    - `var()` is substituted from the element;
    - `initial` gives the registered initial value;
    - `inherit`, and `unset` of an inheriting property, give the parent's value;
    - a missing 0 % or 100 % keyframe is the underlying value.
    `transition: all` leaves custom properties out; a named one transitions only when registered as a colour with
    colours at both ends.

    The animated values are applied in the cascade (the animation origin, under `!important`):
    - `AnimationEngine.Restyled` computes the element's style again with them (`StyleEngine.Compute(…, overrides)`,
      not shared), so they reach the properties that use them, the pseudo-elements and the children;
    - `Tick` queues a restyle of each element whose animated custom values changed (`TakeCustomRestyles`, run by
      `UiDocument.UpdateStyleAndLayout`);
    - as in Blink, animation-only restyles start no transitions.
  - *Compiling* refuses three kinds of rule, so a compiled sheet registers exactly what Chromium registers:
    - a rule Chromium ignores ("a @property rule Chromium ignores: …");
    - a valid rule of another syntax ("… the engine can't take …");
    - a rule inside `@media`.
  - *The record:* `ui.stylesheet` is now version 2 with field 4 `properties` (`ui.property-rule` 1: the name and the
    descriptors as written). Version 1, frozen as `StylesheetRecord.V1`, upgrades with no rules. Loading refuses a rule
    that registers nothing, at its path. Its layouts, marks and the fixture `ui.stylesheet/with-properties` are in
    `Data/Records/`.
- **`-webkit-text-fill-color`** is inherited. Its initial `currentcolor` is kept as the keyword (null), so each element
  fills with its own colour.
  - *Painting* (`TextPainter::TextPaintingStyle`): glyphs fill with it — page text, the unselected glyphs beside a
    selection (on both of Blink's selection paths), a field's value, an input button's label, a select's label. Shadows
    keep `currentcolor`, the colour.
  - *Decorations:* a decoration's `currentcolor` is the fill colour (`DecorationColorIncludingFallback`), and
    `text-decoration-color: currentcolor` is now kept as the keyword too.
  - *Animation:* it interpolates as a colour and only repaints.
- **`background-clip`** is per layer (`border-box`, `padding-box`, `content-box`, `text`). Its `-webkit-` alias is the same
  property, and the `background` shorthand sets it: the first box is the origin and the next the clip; one box, or
  `text`, is the clip.
  - *The layers:* the colour is painted with the bottom layer's clip (`BackgroundColorClip`). When that differs from
    the bottom image layer's, the colour is painted first on its own.
  - *Painting a layer clipped to text* (`BoxPainterBase::PaintFillLayer` and `PaintFillLayerTextFillBox`):
    - there is no fast path; a rounded box clips to its snapped rounded border box first;
    - then `PaintTarget.PushTextClipLayer` clips to the snapped border box and opens a layer (in the current display
      item a save, a clip and a save layer, which end its opaqueness analysis);
    - the colour and the image are drawn in it, with no destination adjustment for obscured borders
      (`ComputeDestRectAdjustments`: none for `kText`);
    - then `PushTextMask` opens a layer composited with **destination-in**, a new `BlendMode.DestinationIn` in
      `Graphics/PipelineCache.cs` (Skia's `kDstIn`: the destination times the source's alpha; outside the step's
      folders, accepted by the manager on 2026-11-13);
    - the box's text is drawn there in black (`UiPainter.PaintTextClip`, Blink's `kTextClip` phase): the inline
      content, block children, atomic inlines, list markers and a control's text (a field's value in its editor, an
      input button's, a select's and a text area's text); no shadows, no selection; decorations in black
      (`BackgroundClipTextDecoration`, stable in 152); the descendants' opacity and transforms aren't applied; no hit
      regions;
    - an inline box's mask is the text of its own fragment on the line being painted.
  - *Mask text is grayscale:* text in the mask layer gets no LCD antialiasing (a transparent save layer).
  - *The view:* the root's background, which the view paints, always clips to the border box
    (`BoxBackgroundPaintContext`).
- **A rule found on the way:** a text's y is rounded against its parent's offset (`TextFragmentPainter`'s
  `ComputeBoxGeometry`, `UiPainter.ParentTop`). The parent is the innermost inline box around the text that has a box
  fragment on the line (`LayoutInline::ComputeInitialShouldCreateBoxFragment`: positioned, translucent, filtered,
  blended, clipped, a background, a border, a shadow, padding, margin or an outline), else the line. The engine had
  always rounded against the line's top, so the relatively placed span in the fixture was one device pixel off at 1.65.
  This applies to all text, not only masks.
- **Text/ and Canvas/:** unchanged. The mask needed no change there; the blend mode is in `Graphics/`.
- **What isn't ported, and what the engine does on meeting it:**
  - **`@property` syntaxes other than `<color>`:** compiling refuses the rule. In a sheet parsed from text the property
    stays unregistered (its value as written, inherited, `var()` substituting its tokens), where Chromium would compute
    and type it.
  - **`@property` inside `@media`:** compiling refuses it. Parsed, it registers whatever the media; Chromium registers it
    only while the query matches.
  - **Colour interpolation in other spaces:** a registered colour always interpolates in legacy sRGB parameters. Chromium
    interpolates `color()`/`color-mix()` results in their own space and mixed spaces in Oklab.
  - **The precision of an animated value:** an animated registered value is kept as its serialized text, so a transition
    that starts part way starts from the rounded value. Blink keeps the float colour: at most one level per channel.
  - **Custom properties animated on pseudo-elements,** and by script animations, aren't applied.
  - **`::selection`'s own `-webkit-text-fill-color`** isn't read (selected text keeps `::selection`'s colour), and
    `-webkit-text-stroke` (the decoration's fallback to the stroke colour) isn't the engine's.
  - **`background-clip: padding-box` and `content-box`** are kept as computed values and painted as `border-box` (the
    decision on background layers, 2026-11-05). `border-area`, which Chromium 152 parses, is invalid in the engine.
  - **An inline box's background image over several lines** is painted per fragment (Blink's image strip,
    `box-decoration-break: slice`, isn't the engine's).
  - **cc's rule that a `SaveLayerOp`** in a cc layer's record keeps the layer's opacity save layers from preserving LCD
    text (`PaintOpBuffer`) isn't modelled. Text in a translucent element sharing a cc layer with a clip-text box would
    be grayscale in Chromium.
- **The fixtures** (`Ui/StylesheetRuleFixtures.cs`; their own list `Reference/stylesheet-rule-fixtures.json`; Chromium
  through `Tools/PaintReference` at 1 and 1.65, headless):
  - *`property-rule`* (640 × 300): the descriptors, the rules Chromium ignores, every kind of initial value,
    inheritance, invalid values, keywords, `var()`, keyframes (linear, ease-in-out, partial, invalid, unregistered) and
    transitions.
  - *`text-fill-color`* (520 × 404): the fill, the decorations, controls' text, and a page selection from a filled
    paragraph into a clip-text one.
  - *`background-clip-text`* (560 × 372): the game's Exotic wave and Sparkling tag, and the mask's every kind of
    content (blocks, inline blocks, an inside marker, shadows, controls, a field's selection, an inline box's own text,
    the colour's own clip, an opaque border, a composited layer at whole pixels).
  - *`background-clip-text-view`* (200 × 60): the root's gradient over the whole canvas.
  - *`text-parent-rounding`* (420 × 152): spans with a background colour, a background image or none, raised or lowered
    by fractions of a pixel on lines of fractional heights.

  Each paints within the paint fixtures' limits (12 levels, 0.2 % of the pixels; differing pixels at 1 and 1.65:
  property-rule 0 and 0; text-fill-color 49 and 165; background-clip-text 229 and 606; the view 0 and 0; text-parent-rounding 0 and 0, against limits of 384–1144). Each also gives Chromium's computed values (`getComputedStyle`) for every element with an id
  (`StylesheetRuleReferenceTests`, `ComputedValues`).

  The harness gained four fixture features:
  - `trigger`: a class added to the body after the first style pass, to start transitions;
  - `computed`: writes `ui-<name>@<scale>.styles.json`;
  - `data-select="start end"` on a focused field: its selection (a number field takes none from a script);
  - `data-select-from` and `data-select-to`: the page's range in two elements' first text nodes.

  `StylesheetRuleReferenceTests.Build` makes the same in the engine.

  `StylesheetRuleReferenceTests` also check two things in the engine alone:
  - *an outside marker's text is in the mask:* Chromium's first capture showed it, but the engine places an outside
    marker apart from Chromium;
  - *a text clip makes nothing known opaque:* `AnalyzeLayers` on the composited layer.

  `PropertyRuleTests` cover the syntax strings, validation, computed text, compiling, the record (version 1 read with
  its parts), the registry (a later sheet, the initial style made again), the cascade, keyframes (`var()`, keywords,
  neutral keyframes, `currentcolor`, premultiplying, a keyframe's own timing, the discrete flip), transitions (from
  where the value is; none for an unregistered colour), a fill change, a decoration's `currentcolor`, the paint-only
  classification, and the shorthand (with `var()` too) and alias.
- **Choices (the step's own, none visible to players):**
  - Only `<color>` is registered, the syntax today's sheet uses.
  - Compiling refuses what it can't register rather than half-registering it.
  - Registered values are stored as the tokens of their computed text.
  - Animated custom properties are applied by computing the style again rather than patching it.
  - A control's mask text goes through the same functions as its painted text (`EditorOf` is shared with the field).
  - Animated colours are read back through the colour parser. An exact `rgb()` reader was dropped: it couldn't change a
    result.
  - The background-clip fixture holds no case the engine draws differently outside these rules:
    - *the list marker is inside:* the engine places an outside marker closer to its text than Chromium (`1.Marker` for
      `1. Marker` at 26 px Lato bold), mask or not;
    - *the input button has a plain border:* Chromium's outset border of a button with an author background differs;
    - *the controls are sized;*
    - *the shadow case has its own element:* the engine doesn't apply opacity to an inline box, which never makes a
      stacking context in `UiPainter`, so a translucent span's shadow differs;
    - *the stat field has `line-height: normal`:* see below.
- **The port's stylesheet:** `Assets/UI/styles/game.css` is today's `old-game/styles.css` with its five `@font-face`
  urls as `ui/fonts/…` data paths. It compiles with no problems; 2 `-moz-` rules are dropped as Chromium drops them.
  - *Packing:* `Tools/Scripts/make-ui-pack.sh` copies `Assets/UI/styles/*.css` to `ui/styles/` beside the fonts, and the
    packer's UI importer compiles each into `ui/styles/<name>`.
  - *Size:* `ui.pak` goes from 998,882 to 1,082,802 bytes (8 entries) and is the same bytes when made again. The record
    is 1,980,754 bytes, stored as 83,844: 3159 rules, 22,679 declarations, 245 `@keyframes`, 1 `@property`.
  - *Load time* (Debug, the PC busy with other workers' runs): loading `ui/styles/game` from the pack took 209 ms the
    first time (reading 40, building 169), then 121–198 ms. Parsing the text took 64–124 ms.
  - *Tests:* `UiResourcesTests` check that `ui/styles/game` loads from the committed pack and is the compile of the
    committed source, byte for byte and part for part, and that the source is today's sheet with the font paths.
- **Brought to `ui.pak`'s new entry:** the editor lists the game's packs, so its entry tree now shows `ui/styles`
  (under `ui/fonts`).
  - `Editor/Opening/EntryListTests` follow it (four tests: the trees, the last item, 313 → 314 rows).
  - The twelve editor screens whose markup the tests wrote differently are captured again headless at 1 and 1.65
    (`Golden/editor-<name>@1.png` and `@1.65.png`): checks-problems, import-again, import-path, import-replace,
    list-marks, list-unreadable, preview-cutscene, preview-effect, preview-model, references-list, saving-mod-info,
    saving-save-as.
  - Their markup in `Reference/editor-fixtures.json` is as the tests write it.
- **Found, for 1b:** a text field's text sits 1 px low at 1 in the engine when its line-height is under the font's
  ascent and descent (the stat inputs' `font: 700 1.625rem/1.2`; 26 px Lato bold: ascent 26, descent 6, line 31.2).
  - *In Blink,* the inner editor's line box has the field's line-height, and the top half-leading is floored
    (`floor(-0.4) = -1`, so the baseline is 25 px below the content top).
  - *In the engine,* the field's text is laid out with a normal line (32 px, baseline 26) centred in the 31.2 px content
    box.
  - *Measured* with the same field plain and clipped to text: both sit 1 px low at 1 (`UiPainter.PaintFieldText`,
    `FieldGeometry.Of`).
- **Found, for later:** two more engine differences outside these rules.
  - *Outside list markers:* the engine places one closer to its text than Chromium (`1.Marker` for `1. Marker`, disc
    markers too, at 26 px Lato bold), mask or not.
  - *Opacity on an inline box:* the engine doesn't apply it. An inline box never makes a stacking context in
    `UiPainter.CreatesStackingContext`, so its translucent text and shadows are drawn opaque. Today's sheet may meet it at
    `.boss-intro-top > span { opacity: .78 }` (the boss intro, not a captured screen).
- **Breaks:** `mutate.py`, 146 of the rules on the final tree (`claude-work/gm1/step1a/make-mutations.py`, `final-1..3.json` and `final-sr.json`, the last every fixture-checked one again on the final fixtures): 144 killed. Two can't differ:
  - *a colour with no alpha left not special-cased:* the general path divides 0 by 0, and .NET's saturating conversion makes each NaN channel 0, the same `rgba(0, 0, 0, 0)`;
  - *a text layer's dest moved off an obscuring border:* that border is opaque and painted over the background in exactly the part the adjustment takes off.

  The first runs' survivors were closed: tests and fixture cases added (`var()` in the shorthand, the version 1 upgrade, selections, an outside marker, the opaque analysis, the rounding spans, the shadows inside the box), an exact `rgb()` reader dropped and a redundant check removed. `make-ui-pack.sh`'s 3 rules ran through a runner of the step's own (`script-mutate.sh`), 3 killed; the unmutated script gave the same bytes. Nothing was written to the player's real folders.

### Step 6: the mod check at start (2026-11-13, the branch `gm1-step6`)

GM1-10, the user's Q3, Q4, Q10, Q18; GM1-P13. Every mod is checked at the start, alone over the game's packs, by one check
in the engine that the editor's save uses too; a broken one is left out whole, named in the log and the list's model; a
mod switched off is neither checked nor mounted; the switches are kept in the settings and take effect at the next start.

**The shared check** (`Engine/Statventure/Packs/ModCheck.cs`): `ModCheck.Check(file, game, kinds)` gives a
`ModReport(File, Info, Problems)` (`Passed` when there is none), each problem a `ModProblem(Entry, Problem)` whose text is
"entry: problem" (the editor's refusal list's form) or the problem alone. `game` is the mounts the mods go over (the
application gives the mounts below the mods folder: the area packs and `classes/`); `kinds` the engine's and the data
library's (`GameMods.GameKinds`). In order, each stage ending the check when it finds something:
1. **The pack opens** (`PackFile.Open`: the header, the index and its hash): one problem in the words the editor's Open
   says it with (the pack's problem; "the file isn't there"; the system's message), naming the entry when the index
   refuses one.
2. **Every entry reads** (`ModCheck.Read`, the editor's opening reads through it: the content's hash; a record entry a
   known kind at a version it reads): each entry that doesn't, with the editor's reason, in path order. (The editor opens
   such a pack read-only, so Save is refused.)
3. **The records** of the data the game would see with the mod (the game's mounts and the mod's pack, a
   `VirtualFileSystem`): every record it shows, the mod's and the game's below them (a game record the data can't give is
   skipped, as the editor shows it unreadable and doesn't check it), each checked as the editor's form shows it: its own
   check first (`ModCheck.Problems(path, value)`: the kind's checks and the marks', then `PathRules`: a theme's id against
   its path, and a record of another kind at `mod/info`), then what the data adds (`ModCheck.CheckRecords`: the same with
   the data and the references resolved over the records' kinds, the theme against the composer with the mod's
   instruments, G5), then, while no record under `audio/` has a problem, the game's music loading on the data
   (`ModCheck.Music`), its problem on the record it names or the data's. Each record's first problem
   (`ModCheck.FirstPerPath`), in path order; a file at `mod/info` among them at its place; the data's problem last.
   Data that can't be mounted with the mod is the data's problem; anything that throws otherwise is "its check failed:
   …", never an exception.

The mod's info (`ModReport.Info`) is its `mod/info` when that is a `mod.info` record that reads, whatever else breaks.

**The problems as the list will show them** (`ModEntry.Problem`, the first; the log the same, "(and n more)" after it):

| The break | The first problem |
| --- | --- |
| A damaged header (not a pack) | `not a pack (it doesn't start with SVPK)` (`29 bytes is too short for a pack` for a short file) |
| A damaged index | `the index doesn't match its hash (damaged)`; an index naming a bad path: `Audio/notes.txt: a path outside the rule: 'A' isn't allowed (only a–z, 0–9, '-', '_', '.' and '/')` |
| A damaged entry | `stand-in/items/lamp: the content doesn't match its hash (damaged)` |
| An unknown kind | `parts/arm: a record of type test.part, which isn't a record type known here` |
| A newer version of a kind | `stand-in/items/lamp: not a readable stand-in.item record: stand-in.item version 2 is newer than this engine reads (version 1)` |
| An entry that doesn't read | `stand-in/items/lamp: not a readable record: the type table is empty`; values its type refuses: `e/refused-values: not a readable scene.curve record: …` |
| A value its checks refuse | `stand-in/items/lamp: "Legendary" isn't one of the choices: Common, Rare, Exotic (at $.rarity)`; `stand-in/items/a: must be 0 or more (it is -1) (at $.price)`; `audio/effects/chime: allows no voices (at $.maxVoices)` |
| A reference to nothing | `audio/theme-order: it names the theme 'nowhere', which has no entry 'audio/themes/nowhere' (at $.themes[55])` |
| A theme that doesn't compile | `audio/themes/alder-vale: alder-vale/intro/mel: bar 1 has 1 steps, expected 6 (at $.sections["intro"].melody)` |
| A game theme the mod breaks (an instrument made silent) | `audio/themes/<theme>: Instrument <name> of kind <kind> makes no sound (at $.sections[…].layers[…].instrument)` |
| What only the music's loading refuses | on a record: `audio/themes/vault: a record of type audio.room, not audio.theme`; else the data's, naming the pack and entry: `<mod file>: entry 'audio/themes/notes': …` |
| A theme at another path | `audio/themes/alder-dale: the theme's id is 'alder-vale', but its path holds 'alder-dale' (a theme's path is made from its id) (at $.id)` |
| An unreadable `mod/info` | a file: `mod/info: a mod's info must be a mod.info record (this is a file)`; another kind: `mod/info: a mod's info must be a mod.info record (this is a audio.room record)`; a newer version: `mod/info: not a readable mod.info record: mod.info version 2 is newer than this engine reads (version 1)`; damaged: `mod/info: the content doesn't match its hash (damaged)` |

**The editor on it** (the brief: its behaviour, tests and screens unchanged): `EditorSession.ReadEntry` reads through
`ModCheck.Read`; `EntryChecks`' own check is `ModCheck.Problems`, its checks across entries `ModCheck.CheckRecords`
(`EntryChecks.PathRules` gone into `ModCheck.PathRules`, `EntryChecks.FirstPerPath` forwarding); Save As's check uses
`ModCheck.Problems` and `ModCheck.Music` (its own order kept: the first of one check with the data). **The one rule new to
the editor is the shared check's `mod/info` rule:** a record of another kind there is its problem (a path rule, shown on
the entry), and a file there is a problem Save and Save As refuse with, among the entries' in path order
(`EditorSession.SaveProblems`, `SaveAsProblems`). No editor test or screen changed. `Docs/Editor.md`, "GM1 step 6".

**Import refuses the save (the user's Q28, 2026-11-14):** the editor's import (`Tools/Editor/Session/EditorSession.Import.cs`,
`ReadRecord`, where every import reads a record: the path's suggestion and the plan, so the Import button and a drop
alike) refuses a record of a kind marked `[NotAPackKind]` (today `save.game`) through Import's refusal for a file it can't
take: "Can't import" — "Ser Alrik.json can't be imported: a save game (save.game) isn't something a mod can hold."
(`EditorSession.NotAPackKindProblem`). Before, it offered "Ser Alrik.json imports as a save game (its JSON form). Its
entry's path:" (`ser-alrik`) and made a `save.game` entry. The shared mod check and the packer are unchanged (a hand-made
mod holding a save isn't broken; the game ignores the entry: `GameModsTests.AModHoldingASaveIsCheckedAsAnyKind`). No screen
changed. **The start time (Q27, 2026-11-14):** the user kept the full check with the numbers below.

**The engine's hook** (`Core/Application.cs`, `Packs/VirtualFileSystem.cs`): `ApplicationOptions.ModFilter` (a
`ModFilter`: the mods found, in the order they mount, and the mounts below them → the mods to mount), given to the
player's mods folder's mount (`Mount.Mods(folder, filter)`); asked at every mount (the start, a Reload); the mods it
accepts mount in their files' name order, each logged `[Data] mod <file>` as before, the others not at all. Null (the
editor, the sandbox, the tests' applications): every mod mounts as before. The check runs where the data mounts, before
the window. Also `VirtualFileSystem.Load(RecordTypes, path)` (a record of whichever known kind it holds).

**`GameMods`** (`Game/Statventure.Game/Mods/GameMods.cs`; the seam as step 2 built it):
- `ApplyTo(options, snapshot)` keeps the snapshot's switches (this start's) and gives the options with its filter. At
  the mount, for each mod in order: switched off (`SettingsSnapshot.IsModOn`, by its file's name): not checked, not
  mounted, `[Mods] <file>: off`; else `ModCheck.Check` over the mounts below with `GameKinds` (the engine's and the data
  library's: `RecordTypes.Of(engine, typeof(SaveGame).Assembly)`): passed, mounted, `[Mods] <file>: loaded (<name>
  <version> by <author>)` (each part only when given: "loaded (Ember Rite by Vatrix)", "loaded (ember-rite)"); else left
  out, `[Mods] <file>: left out: <first problem>` and ` (and n more)` when there are more, a warning.
- `Entries`: each mod in mounting order, `ModEntry(File, Name, Author, Version, Description, State, Problem,
  MoreProblems, SwitchedSinceStart)`; the name `mod/info`'s when given, else the file's name without its ending
  (`ember-rite.pak` → `ember-rite`); author, version and description null when not given (empty or blank text is not
  given); `Problem` and `MoreProblems` only when left out (the problems count one per entry, as the editor's refusal lists
  them). A switched-off mod is still named from its `mod/info` (read alone, not checked; its file's name when it doesn't
  read). `LeftOut`: those left out.
- `Switch(file, on)`: `GameSettings.SetModOn` (the seam's member; before `Attach` it throws: there is nowhere to keep
  it); nothing mounts or leaves now. `SwitchedSinceStart` is true while the settings' switch differs from this start's
  (switched back, it is false again).
- Each mount makes the entries anew (the game never reloads, Q18).

**Choices** (internal, the step's): the check's stages and its order of problems as above (the editor's Save's order: an
entry's own problems first, entries in path order, the data's last; Open's refusal, then the read-only entries); the
game's data under a mod is whatever mounts lie below the mods folder (the editor's base is the area packs only: the same in
GM1, which has no `classes/`); `ModFilter` asked once per mount with the whole list (so the base and the entries are one
mount's); a refused index's entry named in the problem; a switched-off mod's info read for its name; `SwitchedSinceStart`
as a difference from the start; empty or blank info text as not given; the log lines' parts left out when not given; the
`[Mods]` lines before the file system's `[Data] mod` lines; `InternalsVisibleTo("Statventure.Game.Tests")` in
`Engine/Statventure/Statventure.csproj` (the game's tests use `Application.DataMounts`).

**The start time** (measured 2026-11-13 on this PC, under the shared lock, inside the headless cage on the GPU, OpenAL's
null output; the built game in Release laid out by `build.sh Release`, framework-dependent; main's `a7390ad` built the same
way from an export as the base without the check; each started `--capture <png> --frames 1` on a scratch per-user folder,
9 starts of each, interleaved; medians; `claude-work/gm1/step6/timing/`): the mods' mount is the time from the log's
`[Data] Mods …` line to the window's line (the check, the mount and the window's making); Load is the game's `Load` from
the log's start (the splash's 2 s included); the wall time from the launch to the end.

| Mods | Mount: base | with the check | added | Load added | Wall: base | with the check | added |
| --- | --- | --- | --- | --- | --- | --- | --- |
| none | 106 ms | 107 ms | +1 ms | −2 ms | 2617 ms | 2625 ms | +8 ms |
| one small (info, an effect, an item, a file) | 106 ms | 237 ms | +131 ms | +63 ms | 2616 ms | 2689 ms | +73 ms |
| five small | 107 ms | 403 ms | +296 ms | +226 ms | 2621 ms | 2853 ms | +232 ms |
| one large (2,056 entries: the 55 themes at another tempo, 1,000 effects, 500 items, 500 files) | 110 ms | 288 ms | +178 ms | +98 ms | 2651 ms | 2762 ms | +111 ms |

What it adds: the first mod's check about 130 ms (its own reading and the game's 126 audio records checked with it, the
55 themes compiled against the composer, the music loaded, most of it the code's first run; the game's later music
loading is then faster, so Load moves by about half), each further mod about 40 ms (the game's records checked again with
it), the large mod's 2,056 entries about 50 ms more than a small one. All of it before the window opens (the splash shows
that much later). In a trial round before the measurement (other workers' builds running, the files not in the cache) the
first small mod's check took 334 ms. Not measured: Windows, a cold disk. **The user kept the full check on these numbers
(Q27, 2026-11-14).**

**Measured again on step 1a's packs (2026-11-14, the step rebased on `eec6576`, against `eec6576` built the same way;
`timing/summary.txt`; the first measurement in `timing/summary-a7390ad.txt`).** Step 1a put the game's stylesheet in
`ui.pak` (`ui/styles/game`, one record of about 84 KB stored, a compiled sheet of thousands of nested records), and every
mod's check reads and checks it with the mod as it checks every record the data shows (alone, then with the data), so
each mod costs about 200 ms more:

| Mods | Mount: base | with the check | added | Load added | Wall: base | with the check | added |
| --- | --- | --- | --- | --- | --- | --- | --- |
| none | 125 ms | 124 ms | −1 ms | −17 ms | 2710 ms | 2704 ms | −6 ms |
| one small | 124 ms | 723 ms | +599 ms | +467 ms | 2697 ms | 3191 ms | +494 ms |
| five small | 125 ms | 1692 ms | +1567 ms | +1420 ms | 2712 ms | 4152 ms | +1440 ms |
| one large (2,056 entries) | 128 ms | 741 ms | +613 ms | +484 ms | 2727 ms | 3242 ms | +515 ms |

The first mod's check about 600 ms, each further one about 240 ms, all before the window opens. The packaged game in the
end-to-end flow (`end-to-end.sh`, the editor's `ember-rite.pak` with its stage model and cutscene) logs 669 ms from
`[Data] Mods …` to its `[Mods] ember-rite.pak: loaded …` line. **These are the numbers after Q27's answer; they went to the
manager with the step's report.**

**With step 5's settings** (rebased on `41df6e1`): `GameMods.Switch` keeps the switch in the real `settings.json`
(`GameSettings.SetModOn`, its `mods` key), `GameSettings.Peek` reads it back before the next start, which then neither
checks nor mounts the mod (a test through the real part). The save (`save.game`, `[NotAPackKind]`, Q26) is a kind the
game knows like any other: a mod holding one is checked by the save's own check and loads when it passes (the mark is
New entry's only; the check never reads it).

**Tests** (50 in the engine's project, 17 in the game's):
- `Tests/Statventure.Tests/Packs/ModCheckTests.cs` (16): a good mod and its info; a pack that doesn't open (too short, not
  a pack, a damaged index, a bad path in the index, not there); each way an entry can't be read (the check stops there);
  a record its checks refuse (its first problem, several in path order); a reference to nothing; a theme that doesn't
  compile; a game theme the mod breaks; the music's problem on the data and on a record; the music held back by a music
  record's problem; a record's own problems before the data's; a `mod/info` that isn't one (a file, another kind, a newer
  version, damaged; elsewhere it isn't the mod's); the order and the data's problem last; what the game's data can't give
  (a game record of an unknown kind skipped, a damaged game pack the data's problem, a missing one "its check failed");
  the game's data what the mounts give; `Load(RecordTypes, path)`.
- `Packs/ModFilterTests.cs` (4, alone: they read the log): without a filter every mod mounts; a filter given the mods in
  order and the mounts below, only those it accepts mounting in order and logged; asked at every Reload, none accepted
  none mounted; the application gives the mods folder its options' filter (none without it, no mods folder with the
  game's packs only).
- `Editor/Checks/ModCheckAgreementTests.cs` (28): the same 25 mods (good ones and each break) in the editor over the repo's
  packs and through the game's check: Open's refusal, the entries it can't read (read-only), Save's problems, the game's
  check's, one for one; Save As refusing a file at `mod/info`; another kind there shown on the entry; a music entry that
  doesn't build holding the music back.
- `Editor/Import/ImportNotAPackKindTests.cs` (2, Q28): the button's file and a drop refused with the words, nothing made,
  a kind alike not marked imported; the session's suggestion and plan refusing it; the words' article (a variant
  `variant.unpacked` added to `ImportVariants.cs`).
- `Tests/Statventure.Game.Tests/Mods/GameModsTests.cs` (17, in the root namespace, step 5's rule): each break left out
  with its exact problem and count in the model and the log while two good mods load (10 cases); a switched-off mod
  neither checked nor mounted, named from its info; the switch kept and marked (and switched back); the switch through
  `settings.json` to the next start; the save as any kind; the names and the log's parts; several problems counted, the
  game's kinds; the game run for real (a hidden window) checking the mods at its start.
- Changed: step 2's `StartSequenceTests` compares the options with the filter in them (step 2 left that to this step).

**Shown able to fail** (`mutate.py` under the shared lock in a headless cage; `claude-work/gm1/step6/make-mutations.py`
writes the lists): the first run, 91 (`mutations.json`): 88 killed, 2 survived, 1 didn't build. The survivors can't
differ: the mod's entries read from the data rather than from the check's own reading (the data's topmost is the mod's
own entry, the same value read again: the reading is a cache), and a game file read as a record (the catch gives null for
it as for the test it skips: an optimization). The one that didn't build (an editor entry that doesn't build left out of
the list) was rewritten as the same rule in the shared check, survived (`mutations-rerun.json`: a gap), and is killed by
the new `AMusicEntryThatDoesntBuildHoldsTheMusicBack`. After the rebase on step 5 (the game's tests renamed into the root
namespace, the settings real), the game's 35 and that one again (`mutations-rebased.json`): 36 of 36 killed. The standing list
`mutations-standing.json` (91): 89 killed, 2 accounted for. Q28's Import refusal (`mutations-q28.json`: the refusal left
out, every other kind refused, the article, the capital, the kind's name): 5 of 5 killed. Nothing written to the player's
real folders.

**Runs** (inside headless cages under the shared lock, scratch per-user folders, nothing audible; the step's commit rebased
on `eec6576`): the full suite on the GPU (the engine's tests 2 min 28 s) and on lavapipe (2 min 51 s): 3920 + 217 = 4137
tests (`main`'s 3870 + 200 and 50 + 17 new), none skipped, `Reference/editor-fixtures.json` unchanged (also passing on
`41df6e1` before the last rebase, 3789 + 217 on both drivers); `end-to-end.sh --publish --version 2026.11.13.1` on the GPU
(the four archives published and checked, then the flow: 157 of 157 in 97 s) and the flow on lavapipe (157 of 157, 99 s),
the editor's `ember-rite.pak` checked and mounted (`[Mods] ember-rite.pak: loaded (Ember Rite 1.0 by Vatrix)` before its
`[Data] mod …` line): no expectation changed; the player's real folders unchanged (this machine's game folder, which the
user's own runs of 2026-11-12 made, and the Documents folder's top level).

### Step 7: the icons in the engine (2026-11-14, the branch `gm1-step7`)

GM1-7, Q12, GM1-P10 (as changed by the manager on 2026-11-13: the icons before any font). The engine draws the 28 symbols
as step 3's outlines wherever their characters appear in a font library's text that has the icons; the game compiles the
outlines into its table and gives them to its fonts. Nothing in `UI/` changed: the layout and the painter reach icons
through the font library and the canvas as they reach glyphs.

**The icon face** (`Engine/Statventure/Text/IconFace.cs`, `FontFace.cs`, `FontLibrary.cs`, `TextLayout.cs`,
`GlyphOutlines.cs`):
- *The table:* `IconGlyph(CodePoint, Outline, Advance, Ascent, Descent, LineGap)`: the outline SVG path data in units
  of 1/1000 em (`IconGlyph.UnitsPerEm`), x from the pen, y down from the baseline, filled nonzero (step 3's format); the
  box in em. `FontLibrary.AddIcons(icons)` adds them (`IconCodePoints` lists them); a code point already an icon, or an
  outline that isn't path data, refuses the whole call (nothing of it added).
- *Before any font:* `FontLibrary.ResolveFor(codepoint, preferred)` asks the icons first, then the preferred face, the
  fallback families and `CharacterFallback` as before; so ◌, which Lato and STIX have, is the icon (asked before the
  library's caches, so what was resolved before the icons came needs no forgetting). Icons are reached only through
  `ResolveFor` (no family names them), so a document's `font-family` never makes an icon face its primary font.
- *Faces:* an icon face is a `FontFace` with no font file (`IsIconFace`, `IconOf(glyph)`): one per box (the icons with
  the same ascent, descent and line gap: the icons of one of Chromium's fonts, so a run of them is one run as the
  font's glyphs were) and per *parent*, the font the text asked for. The parent gives the face its space: Blink shapes a
  space with the first font that has one (HarfBuzzShaper: only what a font lacks goes on to the next), while the engine's
  layout keeps a space in the run before it (`InlineLayout`, `TextLayout`), so "↻ Refresh file" or "★ Favorite" would
  otherwise shape their space with the icon face. The icon face maps U+0020 to a glyph of its own whose advance is the
  parent's space (`TextLayout.Advance` on the parent's glyph): Chromium's width. No weight or style changes an icon (no
  synthetic bold or oblique: the approved sheet drew the 9 px bold uses as the plain outline; no use is italic).
- *Shaping:* HarfBuzz through font functions of the face's own (`IconFaceData`: the nominal glyph, the advance, the
  outline's extents, the box as the font extents; an empty `Face` with no tables, so no substitution or kerning): each
  icon one glyph (glyph i + 1 of its box's icons), the space glyph after them. Units 1000 to the em, so `Ascent`,
  `Descent` and `LineGap` hold the box rounded to them for what reads a face's units (the drawing's bounds); the
  layout's numbers come from the box itself.
- *The advance* (`TextLayout.Advance` → `IconFaceData.AdvancePixels`): the box's advance in em times the size as a font's
  glyphs are scaled, Blink's size kept to 0.01 px (truncated), then Skia's 26.6 (truncated: 26.4 px advances as 26.390625
  px), the result to 1/65536 px, halves up, as skrifa gives a font's (0.6 em at 9 px is 353894/65536 px). For a font
  with a power-of-two em (Adwaita, Noto Emoji, Liberation) that is Chromium's advance to 1/65536 px (icons.json's 10
  digits are its exact value or a hair off it); for Noto Sans Math and Noto Sans Symbols 2 (1000 to the em) Chromium's
  16.16 scale differs by up to about 2·10⁻⁴ px a glyph (1.257 em at 16 px: 20.11199951 here, 20.11184692 in Chromium),
  far inside the layout's 0.5 px. (icons.json gives em, not the fonts' units: the table's numbers, as decided.)
- *The metrics* (`TextLayout.Metrics`): the box's ascent, descent and line gap times the size, rounded as a font's are
  (halves up; on Linux with text at subpixel positions a descent rounded down takes a pixel from the ascent, as Blink's
  `AscentDescentWithHacks` does for every font, a fallback's too). The box is Chromium's (OS/2 typo metrics where the
  font sets USE_TYPO_METRICS: Adwaita Mono's descent 0.285, where the engine's `FontFace` would read hhea's 0.215).

**Line metrics, against what Chromium did with the fallback glyph** (Blink's `InlineBoxState`: `ComputeTextMetrics`
gives a box its primary font's ascent and descent with the line-height's leading; with `line-height: normal`
`AccumulateUsedFonts` unites in every font the shape result used, each with its own line gap shared out, the top half
floored):
- *A line of the canvas's `TextLayout`:* each run's box with its own line height: an icon run takes its box's ascent,
  descent and line gap (`IconFaceTests.ALineOfTheCanvasLayoutTakesTheIconsBox`).
- *A line of the UI* (`InlineLayout`, unchanged): an icon is a fallback run like any other. With a fixed line-height the
  icon changes nothing (Chromium: likewise, the used fonts count only for `normal`). With `line-height: normal` the line
  takes the icon's ascent above the primary font's half-leading (`FallbackAscent`), but **not its descent or line gap**,
  which Chromium unites in. Whether the line holds only the icon or text of another font beside it is the same rule: the
  primary font's box (the strut and the text) and the icon run's ascent. From icons.json against Lato (ascent 0.987,
  descent 0.213 em, no gap): every icon's ascent is under Lato's but Noto Sans Math's (1.069: ↶ ▣ ◈ ◎ ◫) and Noto Sans
  Symbols 2's (1.069: ♛ ♜), which the engine takes as Chromium does; every descent but ▲ ▼'s (0.212) is deeper than Lato's
  (◌ 0.216 with a 0.042 gap, Adwaita Sans 0.241, Noto Emoji 0.244, Adwaita Mono 0.285, Noto Sans Math 0.423, Noto Sans
  Symbols 2 0.63), so a `line-height: normal` Lato line holding one is that much deeper below in Chromium than in the
  engine (♜ at 19 px: 12 px below the baseline against Lato's 4). The same held for the fallback fonts before (the
  engine's rule, not the icons'), and **no captured line meets it**: every captured state at 1 and 1.65 lays out box
  for box with the icons (`IconLayoutTests`), so the old game's icons sit in lines of fixed height or in boxes sized
  otherwise. An engine finding for `UI/Layout/InlineLayout.cs` (not step 7's to change: "Found" below).

**The drawing** (`Engine/Statventure/Canvas/CanvasText.cs`, `CanvasPaths.cs`): `Canvas.DrawGlyphs` gives an icon face's
run to `DrawIcons`: each icon's outline filled by the canvas's path fill (`FillRule.NonZero`), in the colour the run is
drawn with, at the run's size as its glyphs would be made (0.01 px, truncated) times the canvas's scale and transform,
where its glyph would land: on an axis-aligned canvas the baseline rounded to a pixel and the pen to the quarter pixel
at or after it, as Skia places glyphs (`SkGlyphPositionRoundingSpec`; whole pixels without `SubpixelText`), under a
rotation or skew exactly. The outline is kept in em (scaled once from its 1/1000 em): the path cache keeps a transform's
linear part to 1/1000, which in the outline's own units would hold a 20 px size to 1 %. A space draws nothing.
- *Coverage* is grayscale and takes no mask gamma, as Chromium fills an inline SVG path (the icons' references), never
  LCD whatever the text's verdict; the blend mode, colour filter, clip and layers are the canvas's as for any drawing.
- *As a glyph is:* everything the painter does with a run reaches the icons: the text's colour and
  `-webkit-text-fill-color` (`UiPainter.FillOf`), the selection's colour and its clip, a `background-clip: text` mask
  (the mask's black text), text shadows, opacity and transforms (layers), decorations skipping ink (`GlyphOutlines`
  gives an icon's outline at the size, so an underline skips it as a glyph's).
- *How an icon takes part in a text chunk's LCD verdict* (`Docs/Compositing-Port.md`): as the glyph it stands for did.
  The painter records the run as text (`PaintTarget.DrawGlyphs`, `Record(…, text: true)`), so a chunk holding it is a
  text chunk for the layerization (`contents_opaque_for_text`, the opaque-background analysis) as Chromium's chunk with
  the fallback glyph was (its recorded bounds the icon face's box). Only the icon itself is
  drawn grayscale (an SVG path in Chromium is never LCD text), the letters beside it keep the chunk's verdict.

**The game's table** (`Game/Statventure.Game/Icons/`):
- `GameIcons.Table`: `GameIconTable.cs`, generated C# compiled into the game (GM1-P10: not in `ui.pak`), written by
  `Tests/Statventure.Game.Tests/Icons/GameIconTableTests` from `Assets/UI/icons/` with `STATVENTURE_ICON_TABLE=write`
  (then rebuild): each icon's code point, its file's path data as written, and icons.json's four numbers as written
  (a C# literal of the same digits is the same double). Without the variable that test fails when the source isn't what
  the files give. Chosen over embedding the files as resources (a change to step 2's project file) and over parsing at
  start.
- `GameIcons.AddTo(fonts)`: `fonts.AddIcons(…)` with the table; once per library (a second call is refused).
- **The seam's change** (step 2's `GameIcon(Outline, Advance, Ascent, Descent)`): one more positional member at its end,
  `LineGap` (in em); `Outline` is the file's path data in 1/1000 em (the stub's comment said em), filled nonzero.
  `GameIcons` became `partial` (the table in its own file). `AddTo` now gives the icons before any font (the stub's
  comment said last fallback).

**For steps 8, 9 and 10:** `GameUi.Load` calls `GameIcons.AddTo(fonts)` once on the library its document is made with
(after `EngineFonts.AddTo`, before laying anything out: a document laid out before keeps its runs until it lays them out
again; shaped text is cached by face). Then any of the 28
characters written in any text of that document (markup, a button's label, a field's value, the old HTML pieces) is
drawn as its icon: in the text's colour, at the text's size, in the glyph's box; nothing else is needed (no class, no
font). ☾ and ＋ aren't icons (Q6 took their uses away); the other characters the fonts lack still have no glyph. In
Chromium's renders of the port's markup the symbols come from the PC's fallback fonts, so those fixtures mask the icons'
boxes (the design's rule): the boxes are where the layout puts them, the glyph's (pen to pen plus advance, the baseline
less the ascent to plus the descent; a fallback glyph's ink may reach a little past it, so mask a pixel or two wider).

**Tests** (64 in the engine's project, 6 in the game's):
- `Tests/Statventure.Tests/Text/IconFaceTests.cs` (18, made-up icons): before any font (◌ in Lato, also when Lato's ◌
  was resolved before the icons came); one face per box (another descent or line gap: a face of its own) and one run of
  them; one glyph each; the advance at seven sizes and advances (the 0.01 px and 26.6 truncations, the 1/65536
  rounding), bold alike; the
  metrics (the subpixel rule, the box not its unit rounding: 0.5005 em at 1000 px is 501); the canvas layout's line; the
  space the text's font's; no synthesis; the extents; the ink for decorations; refusals; no font rendering of an icon face.
- `Canvas/IconDrawingTests.cs` (15, GPU): the outline filled where the glyph goes at five positions and sizes (against
  the canvas's path fill of the outline at the snapped pen, 2 levels), nonzero (an overlap filled), exact under a
  rotation, with the canvas's scale, grayscale whatever the verdict (and Lato LCD beside it), the space and a transparent
  colour drawing nothing; in documents at 1 and 1.65 against plain text: the fill colour, the selection's colour, the
  text mask (2 levels), a shadow, opacity (2 levels).
- `Ui/IconLayoutTests.cs` (23): the 11 captured states at 1 and 1.65 (`Golden/game-ui.json.gz`, `game-ui@1.65.json.gz`,
  whichever capture is on `main`: the old ones until step 1b lands, then those) laid out with the 28 icons in a library of
  their own (`TestFonts.NewLibraryWithSystemFallback`, Chromium's fallback fonts for everything else; ☾ and ＋ stay
  fonts), compared box for box with `LayoutReferenceTests.Compare` at its 0.5 px, as the game-screen layout tests are;
  the states taken are those whose markup holds an icon's character (all 11 today: 17 of the 28 symbols), and each must
  lay icons out. A recapture is taken as it comes: the check holds as long as the game-screen layout tests hold on it,
  because each icon's box is the glyph Chromium drew there. The game-screen tests' own `Build` isn't changed (step 1b's):
  the test has its copy with the fonts given.
- `Ui/IconReferenceTests.cs` (8): the fixture list and the three fixtures at 1 and 1.65 against Chromium (below).
- `Tests/Statventure.Game.Tests/Icons/GameIconTableTests.cs` (3): the source written from the files; the table the
  committed files' content (28, in code-point order, each outline and number); the set GM1-7 decided. Each file must be
  one nonzero path and nothing else drawn (step 3's format, what the table holds). `GameIconsTests.cs` (3): `AddTo` gives
  every symbol its icon before any font with the table's outline and box; the table's boxes lay out (⚔'s advance, ♜'s
  metrics, ◌'s line gap); once per library.
- `Tests/Statventure.Tests/Text/IconFiles.cs` reads the committed icons for the engine's tests (they can't reference the
  game: GM1-P4); `Canvas/TestFonts.cs` gained `NewLibraryWithSystemFallback()` (the shared library's builder, made
  public: a test that adds to a library must not add to the shared one).

**The fixtures against Chromium** (`Reference/icon-fixtures.json`, written by `IconReferenceTests`; goldens
`Golden/ui-icon-uses@1.png`, `@1.65`, `ui-icon-large@…`, `ui-icon-rules@…`; captured by the new
`Tools/Scripts/make-icon-references.sh [scale …]`, PaintReference under X11 at each scale, on the PC inside a headless
cage with a 2400 × 2400 output, about 15 s):
- *The fixtures:* `icon-uses`, the old game's 83 uses (`Tools/IconSheet/uses.json`: size, weight, the light theme's
  colour, ground, opacity and the sigil's turn); `icon-large`, the 28 at 96 px in the ink on the parchment;
  `icon-rules`, runs of icons (pens at quarter pixels), the fill colour, opacity, a 30° turn, a fractional size (19.2 px),
  the six 9 px bold battle statuses, the dark theme's inks and grounds. Cells of whole device pixels at both scales
  (multiples of 20 px), each icon's text in Lato on a fixed line.
- *What Chromium renders:* the same cells with each icon as an inline SVG of its outline filled with `currentColor`,
  placed where the engine's glyph lands (read from the engine's layout: the pen on the baseline, snapped as the canvas
  snaps a glyph, at the size its glyphs are made), in a box of whole device pixels with a view box putting the outline's
  origin on the pen (Blink snaps an svg root's box to pixels; the CSS lengths a hair over the exact value so the layout's
  truncation to 1/64 px gives the whole pixels). So the list differs between the scales (one fixture per scale; the
  script captures each at its own). Where the glyph lands is the layout check's (`IconLayoutTests`); this compares the
  drawing.
- *The limits:* the inline SVG paint fixture's (`svg-transforms`: 60 levels, 0.2 % of the pixels; the canvas keeps its own
  path antialiasing, the user's decision of 2026-10-25), none set anew. **No pixel of any fixture is past 60 levels**
  (worst: uses 50 and 37 at 1 and 1.65, large 29 and 36, rules 53 and 48; past 12 levels: 766, 1285, 1999, 3697, 318 and
  817 pixels, the outlines' edges).

**Shown able to fail** (`mutate.py` under the shared lock in a headless cage; `claude-work/gm1/step7/make-mutations.py`
writes the lists): 48 rules broken one at a time (`mutations.json`): the icons' place before the fonts (after the text's
font; never), the faces (one per icon; a box without its line gap or descent; a code point twice; synthesized as the
text; the parent not the asker's), the space (not mapped; advancing nothing), the extents, each metric (the 0.01 px and
26.6 truncations, the 1/65536 rounding, HarfBuzz's units for the box's pixels, the face's units for the box, ascent and
descent swapped, no line gap), the outline (its units, y up, the ink for decorations at another size or none, even-odd),
the colour (always the ink; its alpha dropped), the place (the baseline not on a pixel, the pen on whole pixels or not
snapped, snapped under a rotation, the box's top at the pen), the size (not the canvas's scale, 5 % off), LCD for icons,
a space drawn, icons as font glyphs; the painter's rules the document tests show an icon taking (its fill colour, the
mask, the selection's colour, shadows: `UiPainter.Content.cs` broken for the run only, not changed); `AddTo` (no line
gap, ascent and descent swapped, nothing added); the table (a number edited, an icon left out) and its making (the parse,
the source's line gap, the outline). **45 killed, 3 survived:** forgetting what was resolved when icons are added can't
differ (`ResolveFor` asks the icons before its caches; the call was removed); the 0.01 px truncation and the pen's
snapping were gaps (no case where the truncation moves the 26.6 size, now 19.8 px, 12 px at 1.65; no pen on an eighth,
where the path cache's own quarter pixels round to even, now 10.125), closed and killed (`mutations-rerun.json`, 2 of 2).
The standing list `mutations-standing.json`: 47, all killed. Nothing written to the player's real folders.

**Runs** (inside headless cages under the shared lock, nothing audible; the step's commit on `714be21`): the full suite on
the GPU (the engine's tests 2 min 30 s) and on lavapipe (3 min 20 s): 3984 + 223 = 4207 tests (`main`'s 3920 + 217 and
64 + 6 new), none skipped; `Reference/editor-fixtures.json` unchanged; the player's real folders unchanged (this
machine's game folder there before and after, the Documents folder's top level the same).

**Choices** (internal, the step's): the icon face as a `FontFace` with HarfBuzz font functions of its own (the layout and
the painter use faces through `HbFont`, which a face with no file can give); one face per box and parent; the space from
the parent (above); icons never synthesized; the advance through the font path's size truncations; units of 1000 to the
em; the outline kept in em for the path cache; glyph snapping on an axis-aligned canvas, none under a rotation; no LCD
and no mask gamma for icons; the canvas's text API underlining an icon run at 0.1 em, 0.05 em thick (the UI's
decorations take the decorating box's font); a font-file face's FreeType paths refusing an icon face (`SkiaGlyphs.Render`,
`GlyphRasterizer.Render` throw); the generated table; `AddTo` once per library; the fixtures' cells and the per-scale
Chromium side.

**Found:**
- *Line metrics with `line-height: normal`:* the engine's `InlineLayout` unites only a fallback run's ascent into the line
  (`FallbackAscent`); Blink unites each used font's ascent and descent with its line gap's halves
  (`InlineBoxState::AccumulateUsedFonts`, in `~/.cache/stat-fick-build/reference-src/chromium-152-source/icons/`). Icons
  are fallback runs, so a `line-height: normal` line holding a deeper icon (every one but ▲ ▼ under Lato) would be
  shallower than Chromium's; no captured screen meets it. For the manager: an engine item in `UI/Layout/`.
- *A transformed box's overflow:* Chromium gave the fixture page scrollbars where a turned cell reached past it (a
  transformed box's overflow counts in Blink's scrollable overflow); the engine didn't. The fixture hides the root's
  overflow; not looked into further (`UI/`).
- *The path cache's scale:* the canvas keeps a path's linear transform to 1/1000 (`CanvasPaths.cs`), relative to the
  path's units: inline SVG drawn in small user units at a large scale (a 1000-unit view box at 0.03 px a unit) is held to
  about 1 %. Not changed (the icons avoid it by keeping their outlines in em).
- **For GM4:** ◆ and ★ are also SVG `<text>` on the map's guild markers (`map.js` 545); the engine doesn't draw SVG text.
  Recorded, nothing built: the map's milestone.

**Not checked:** Windows; italic text with an icon (none in the game: drawn upright); the icons in the real game's screens
(steps 8–10 show them).

## Status

**Step 5 done (2026-11-13, the branch `gm1-step5`; "How it works", step 5): the save record `save.game` v1, the
settings, the session and the randomness service** filled into step 2's seams (the seams' few changed shapes listed
there for steps 9 and 10); the user's Q25 (Start new's name when nothing reads: the slot's file name) and Q26 (the save
left out of the editor's New entry, `[NotAPackKind]`) answered on the way; the slot naming rule's edge cases decided and
tested; an engine item found (`Settings` can't make a key absent; "Small tasks left from earlier milestones"). The full
suite passes on the GPU and on lavapipe: 3739 + 200 = 3939 tests (`main`'s 3767 and 172 new), none skipped; 169
mutations, 166 killed, 3 that can't differ; the player's real folders unchanged. Not checked: Windows.

**Step 3 done (2026-11-13, the branch `gm1-step3`): the 28 icons' outlines and boxes, approved by the user.**
- The outlines and boxes are in `Assets/UI/icons/`. The style is "Engraved", the user's pick, with the heavier lit
  line on ★ and ✸ (the user's answers at stop 2).
- The generator is `Tools/IconSheet/gen/`. The sheet is made by `Tools/Scripts/make-icon-sheet.sh` from the old game's
  83 uses (`Tools/IconSheet/uses.json`).
- The manager's four decisions are recorded: the icon face draws before any font, the generator is committed, two-meaning
  symbols carry over unchanged, and the smallest uses keep the style.
- Nothing the solution builds or the tests read changed, so no full runs were due ("How it works", step 3).

**Step 4 done (2026-11-13, the branch `gm1-step4`): the eight interface sounds,** approved by the user by ear
(`ui-press` b, `ui-toggle` c, `ui-open` c, `ui-close` c, `game-saved` a, `game-opened` c, `ui-refused` c, `game-deleted`
c; "How it works", step 4), in `Assets/Audio/effects/` and `audio.pak` (126 entries, made by `make-music-data.sh`,
reproducible byte for byte), each a fixture against Chromium; `play-sounds.sh` for the ear. The full suite passes on the GPU and on lavapipe, inside a headless compositor under the shared lock: 3738 tests (`main`'s 3720 and 18 new: 8 fixtures, 10 in `GameSoundsTests`), none skipped; the player's real folders unchanged.
Mutations: 5 of the tests' rules, all killed; 22 of the two scripts' rules by runners of the step's own, 21 killed (the
silent warm-up plays only move the code's first compile off the first sound heard); the 0.3 s rule by hand.

**Step 2 done (2026-11-13, the branch `gm1-step2`; "How it works", step 2):** the game's own program
(`Game/Statventure.Game`, published as `stat-fick` / `Stat Fick.exe`, one instance per per-user folder, its window as the
player left it, its checks' command line) and its real data library (`Game/Statventure.Game.Data`) in place of the
sandbox and the stand-in; the program's seams with a stub for every part, the start sequence and the wiring running on
them (the deviations from the outline listed there); the sandbox the engine's test bed; the game's own test project
(29 tests; the engine's 3738; the suite 3767); the four packages, their check and self-test, the end-to-end flow on the
GPU and on lavapipe; the publish through a symbolic link fixed; `build.sh` unchanged for a failed shader (it stops, exit
code 1). Not checked: Windows; the single instance and the window's states on a real desktop.

**Step 1a done (2026-11-13, the branch `gm1-step1a`; "How it works", step 1a): today's stylesheet in the engine and in
`ui.pak`.**
- *The three rules,* ported from Chromium 152: `@property` (registered `<color>` custom properties),
  `-webkit-text-fill-color` and `background-clip: text`. Each matches its Chromium fixtures at 1 and 1.65 and is shown
  able to fail.
- *A rule found on the way:* a text's y is rounded against its parent inline box's offset.
- *The port's stylesheet:* `Assets/UI/styles/game.css` is in `ui.pak` as `ui/styles/game`, compiled with no problems
  (998,882 → 1,082,802 bytes, reproducible; about 0.2 s to load, Debug).
- *The editor:* its entry tree shows the new `ui/styles` folder; twelve of its screens were captured again.
- *The full suite* passes on the GPU and on lavapipe inside a headless compositor under the shared lock: 3870 + 200 =
  4070 tests (`main`'s 3739 + 200 and 131 new in the engine's project), none skipped; the player's real folders
  unchanged. The first lavapipe run had one failure outside the step:
  `Audio/ScheduledSoundTests.TheClockStandsStillWhileTheSoundIsPaused`. It passed alone and in the run made again. Its
  likely cause is the test's sink, which takes no more buffers after 200 and isn't reset by a pause (reported to the
  manager).
- *Mutations:* 146 of the tests' rules on the final tree, 144 killed, 2 that can't differ ("How it works", step 1a); the pack script's 3, all killed.

**Step 6 done (2026-11-13, the branch `gm1-step6`; "How it works", step 6): the mod check at start.** One check in the
engine (`Packs/ModCheck.cs`) that the editor's save and the game's start share, so the editor refuses exactly what the game
leaves out, in the same words (shown on 25 mods through both); the engine's hook `ApplicationOptions.ModFilter`; `GameMods`
with its entries, log lines and switches; the user's Q28 built (the editor's Import refuses the save). The start time with
the check: +8 ms with no mods, +73 ms with one small mod, +232 ms with five, +111 ms with one of 2,056 entries (wall time,
medians; all before the window opens), which the user kept (Q27); on step 1a's packs, where the game's stylesheet is
checked with every mod, +494 ms, +1,440 ms and +515 ms (gone to the manager). The full suite passes on the GPU and on
lavapipe on the step rebased on `eec6576`: 3920 + 217 = 4137 tests, none skipped; `end-to-end.sh --publish` 157 of 157 on
both; mutations 91 + 36 + 5 (the standing list: 89 killed, 2 that can't differ; the rest all killed); the player's real
folders unchanged.

**Step 7 done (2026-11-14, the branch `gm1-step7`; "How it works", step 7): the icons in the engine.** The engine's icon
face (`FontLibrary.AddIcons`): the 28 symbols' characters taken before any font, ◌ too, each a glyph of a face per box
and parent font (its space the parent's), measured by its box (Chromium's glyph), drawn by the canvas's path fill in the
text's colour where the glyph lands, grayscale; every rule of the painter reaches it as a glyph (fill colour, selection,
the text mask, shadows, opacity, transforms, decorations). The game's table compiled from `Assets/UI/icons/`
(`GameIconTable.cs`, written and checked by the game's tests) and `GameIcons.AddTo`; the seam's `GameIcon` gained
`LineGap`. Against Chromium filling the same outlines (the 83 uses, the 28 at 96 px, the rules; 1 and 1.65): no pixel past
60 levels; the 11 captured states at both scales lay out box for box with the icons. Nothing in `UI/` changed. Found for
the manager: `line-height: normal` lines take a fallback's (an icon's) ascent but not its descent (Blink both; no captured
line meets it); Chromium scrolls a page a transformed box reaches past; ◆ ★ as the map's SVG text left to GM4. The full
suite passes on the GPU and on lavapipe on the step on `714be21`: 3984 + 223 = 4207 tests (64 + 6 new), none skipped; 48
mutations, 45 killed, 1 that can't differ (its code removed), 2 gaps closed and killed (the standing 47 all killed); the
player's real folders unchanged. Not checked: Windows.

**GM1's design decided (2026-11-13, docs only, the branch `gm1-design` from `2bd4da9`):** the user answered the detail
round, complete: Q1–Q24 and the follow-up Q16b (every answer the recommended option except Q6, Q11, Q15, Q16 with Q16b,
Q18, Q22 and Q23; Q24, raised by the answers, as recommended: "Load Character…" in the slot list), and the manager decided
the 19 proposals (GM1-P11's generator and GM1-P19 changed); the old game is frozen until the recapture is on `main`. The
design and twelve steps (1a and 1b, 2–11) in four waves are final; nothing is open. No step started.

**GM1 started (2026-11-13, the same branch):** the user's go; this design doc written after checking the old game and the
engine: what exists, the method, the design, 23 questions for the user, 19 proposals for the manager and 11 steps in four
waves. Found on the way: no symbol icon exists in the engine yet; today's old stylesheet doesn't compile in the engine
(`@property`, `-webkit-text-fill-color`) and the capture script no longer drives today's old game unchanged; slot names
follow the path rule; Force encounter isn't Dev Mode's.

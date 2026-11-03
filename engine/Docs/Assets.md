# Assets and data (M9)

The game's data on disk: binary packs (`.pak`) of versioned records and files, read through a
virtual file system that lets mods and a development folder override them; the serializer that
gives every record a binary and a JSON form; saves, settings and character files in a per-user game
folder; and the importers that fill the packs from the old game, Blender's exports and the
stylesheets.

## Decisions (user, 2026-11-02)

Asked before the milestone started, every answer the recommended option; in full in
`Docs/Engine-Plan.md`, "Decisions for M9". The format details asked after this doc's first draft are
under "Format details" below.

- **Packs by area:** `models.pak` (meshes, clips with their markers, state machines), `audio.pak`
  (themes, instruments, percussion, rooms, effect patches), `ui.pak` (compiled stylesheets, fonts,
  images), `world.pak` (terrain, map data), `data.pak` (the game's tables, once the port defines
  their records). Classes stay loose `.json` (2026-10-16).
- **Compression:** Brotli per entry (.NET's `System.IO.Compression`); an entry that doesn't shrink
  is stored raw, flagged. No new package.
- **Hashes:** SHA-256 from the framework, per entry and one over the index: damaged files are
  caught, and the editor (M11) detects changes by them.
- **Versioning:** a version number per record type, fields by position, upgrade code on read for
  every past layout; fixture files of the old versions are committed and tested, so a layout change
  can't break old files unnoticed. Pack records, saves and settings alike.
- **Overrides:** the virtual file system mounts the game's packs, then extra packs from a mods
  folder, then the loose development folder on top; later mounts win per path. A mod is just another
  pack; the development folder gives live editing with Reload.
- **The game folder:** one per user, saves and settings side by side: Linux
  `~/.local/share/stat-fick/` (`$XDG_DATA_HOME` honoured), Windows `%APPDATA%\Stat Fick\`; the
  in-game "Open game folder" opens it.
- **Saves:** binary versioned records through the same serializer and versioning as the packs'
  data, hashed, written atomically (a temporary file, then a rename), the previous save kept as a
  backup.
- **Character files:** Save Character writes the current save as readable JSON of the new save
  format (versioned), Load Character reads one back; Documents by default, as in the old game;
  through the in-game file picker (M5: no OS dialogs). So every record has a JSON form as well as its
  binary one. The old game's saves aren't imported (2026-10-16).
- **Settings:** `settings.json` in the game folder (System.Text.Json) with a version field; unknown
  keys are kept, missing ones take their defaults. Which settings exist is decided at port time.
- **Scope:** M9 packs the engine's own data (models with clips and state machines; themes,
  instruments, percussion, rooms; effect patches; terrain; compiled stylesheets; fonts) and builds
  the format, the virtual file system, the serializer, the packer and importers, saves and settings.
  The old game's tables (enemies, spells, artifacts, consumables, key items, races) are converted at
  port time, when their C# records exist, checked against the old game in Chromium as the music is.
- **Embedded stays embedded:** the SPIR-V shaders and the UI's PNGs remain resources of the engine
  assembly; the engine and its tests work without a pack.
- **Music data:** `MusicData.g.cs` goes. `make-music-data.sh` becomes the music importer (the old
  scripts read in Chromium as now, the records written into `audio.pak`); the engine loads the themes
  from the pack, and the existing checks (the 55 themes' exact call logs against `music-logs.json`,
  the opt-in renders of `test-themes.sh`) run on the pack-loaded data.
- **Packs are the source of truth:** committed in `engine/Assets/Packs/` and copied into the game
  folder (`$STATVENTURE_GAME`) by the build; importers fill them, the editor (M11) edits them. For
  reviewing their binary diffs the engine dumps a pack as text (JSON).

Decided earlier (`Docs/Engine-Plan.md`, "User decisions" and M5–M8): everything but the classes is
binary packs, created and edited with the editor; CSS-like stylesheets are compiled into the packs;
Linux and Windows only; no OS dialogs (Save/Load Character, file picking and "Open game folder" are
in-game UI); models come from Blender as `.glb` and M9 converts them, reusing `GltfImport`; clip
events are the actions' pose markers in the animations' extras and state machines are data records
(M7); music and effects have no audio files (M8).

## What exists (checked against the code, 2026-11-02)

Nothing of M9 yet: no pack, virtual file system, serializer, save or settings code.

- **Folders.** `engine/Assets/` doesn't exist (git keeps no empty folder; the plan said "empty").
  The sources the Folders table puts there live elsewhere today: the shader sources in
  `Engine/Statventure/Shaders/Source/`, the test models' Blender scripts in `Tools/BlenderReference/`
  (their `.glb` exports in `Tests/Statventure.Tests/Data/Models/`), the fonts in
  `old-game/objects/fonts/` (byte-identical copies in `Tests/Statventure.Tests/Data/Fonts/`), the
  stylesheet in `old-game/styles.css` (a byte-identical copy in `Tests/Statventure.Tests/Data/Game/`,
  read by `GameLayoutTests`). Nothing writes to `$STATVENTURE_GAME` yet: `build.sh` and `build.ps1`
  build into each project's `bin/`, and there is no game executable before the port (M12 does the
  shipped layout).
- **Models** (`Render3D/GltfImport.cs`, `ModelData.cs`, `Model.cs`): `GltfImport.Load(path)` gives a
  `ModelData`: `ModelNode` records (name, parent, children, matrix and TRS, mesh, skin, `Ink`), meshes
  of `ModelPrimitive`s (a class with init properties: positions, normals, texture coordinates,
  `JointIndices`, weights, 32-bit indices, material), `ModelMaterial` records (name = palette slot,
  base colour, double-sided, emission, unlit, `Hatch`), `ModelSkin`s, `ModelAnimation`s (channels with
  times and values as the file has them, events from the extras) and the roots. BRUTAL's `GltfLoader`
  opens only a path (no stream or memory constructor), and the extras (`ink`, `hatch`, `events`) are
  read from the file a second time (`ExtrasOf`), since BRUTAL drops them. `ModelData.Load(path)` and
  `Model.Load(device, path)` go through it; `Model` splits flat faces' vertices and finds the feature
  edges when it is created (`ModelPrimitive.FlatVertices` is internal and set there, not by the
  import). Only the 12 test models exist (385 KB of `.glb`); no game model yet.
- **Clips and state machines** (`Animation/`): clips are built from `ModelAnimation`s at load
  (`AnimationClip`, bound by node names in `Skeleton`). `StateMachineDefinition` and its parts
  (`ParameterDefinition`, `ConditionDefinition`, `StateDefinition`, `TransitionDefinition`,
  `LayerDefinition`) are positional records, built with `StateMachineBuilder` and checked by
  `Validate()`; only the M7 tests' machines exist.
- **Terrain** (`Render3D/Terrain.cs`): `TerrainData.Load(path)` reads a JSON description naming a
  16-bit height PNG and an 8-bit cover PNG beside it (BRUTAL's Stb, by path; Stb also has
  `LoadFromMemory` and `Load16FromMemory`) and turns the samples into float heights
  (`low + s / 65535f · (high − low)`). Only the 64×48 test terrain exists
  (`Tests/Statventure.Tests/Data/Terrain/`).
- **Fonts** (`Text/FontLibrary.cs`): `Add(ReadOnlySpan<byte>, family, weight, italic, weightMax)`
  already loads a face from memory (`FT_New_Memory_Face`); `AddFile(path)` only reads the file for it
  (the plan said "by path"). Faces are registered in code (`Tests/.../Canvas/TestFonts.cs`, the
  Sandbox) with the families and weight ranges the old stylesheet's `@font-face` rules declare (Lato
  400, 500–600 and 700–900; STIX Two Text regular and italic). The parser keeps `@font-face` rules
  (`Stylesheet.FontFaces`), but nothing reads them. The five faces are 2.5 MB.
- **Stylesheets** (`UI/Css/`, `UI/Style/Stylesheet.cs`): parsed from text when added.
  `Stylesheet.Parse(text)` tokenizes, parses the rules, selectors (`SelectorList` of `Selector`s of
  `CompoundSelector`s, attribute tests, pseudo-classes), media queries, keyframes and font faces, and
  prepares each declaration once (shorthands expanded, invalid values dropped, unknown properties
  listed, `var()` marked); `UiDocument.AddStylesheet(css)` calls it. The user-agent sheet is a C#
  string (`UserAgentStyles.Css`), the engine's own like the shaders. The old game's `styles.css` is
  393 KB.
- **Images and SVG text:** the UI asks for them by url through `UiHost.LoadImage` and `LoadText`,
  callbacks the game sets; `ImageFile.Load(bytes)` decodes from memory. Embedded resources
  (`Statventure.csproj`): `Shaders/Bin/*.spv` (compiled by `Tools/ShaderCompiler` before the build,
  not in git; read by `ShaderLibrary`) and `UI/Resources/*.png` (the search field's two cancel
  buttons; read by `ControlImages`).
- **Music** (`Audio/Music/`): `MusicData.g.cs` (272 KB, `internal static class MusicData`: 34
  instruments, 21 percussion voices, 7 rooms, 55 themes) is generated by `make-music-data.sh`, which
  runs `Tools/AudioReference/music.cjs` in the harness's Chromium; its `dataCs` maps every JavaScript
  key to a C# property (`t` → `Type`, `dbl` → `Doublings`) and stops on an unknown key. The records
  (`ThemeData.cs`): `ThemeSpec` (its sections in a dictionary by name), `SectionSpec`, `LayerSpec` (a
  drums layer's patterns are an ordered list of pairs: their order decides the random draws),
  `HarmonySpec`, `DoublingSpec`, `InstrumentSpec`, `PercussionSpec`, `RoomSpec`; an absent key is null
  and the composer applies the old defaults. The data is read statically: `Soundtrack.Themes` (a
  static list), `Soundtrack`'s compiled themes (a static `Lazy`), `Composer` (instruments,
  percussion, rooms), `GraphBuilder`, `AudioSystem` (prepares every room at start), `MusicTests`, and
  through `Soundtrack.Themes` the tools `PlayTheme` and `AudioBenchmark`. Loading it from a pack makes
  it instance data.
- **Effect patches** (`Audio/Effects/EffectPatch.cs`): `EffectPatch`, `EffectLayer`, `EffectFilter`,
  `EffectRange` records and two enums, with defaults in the records (440 Hz, a 2 ms attack, Q 1,
  level 1, 4 voices); registered by code (`SoundEffects.Register(id, patch)`). Only the tests'
  patches exist; which sounds the game gets is decided at port time.
- **Settings to be:** `AudioSystem.MasterVolume`, `EffectsVolume` and `Music.Volume` (M8),
  `UiHost.PageZoom`. The old game keeps `stat-fick-game-settings` (battlePace, combatAnimations,
  battleUi, battleSpeed; `scripts/settings.js`) and the music volume (`music.js`) in localStorage;
  its dark mode is part of the character save, not of the settings.
- **The old game's saves:** one character, in localStorage (`emberfall-character-stats`), written by
  `saveCharacter()` (`app.js` line 1753) after every change (about 80 calls across its scripts) and
  when the window closes (`desktop/main.cjs`, raced against 2.5 s). Save Character (`exportBackup`,
  line 1780) writes the same object as indented JSON through a dialog defaulting to
  `stat-fick-character.json` in `app.getPath("documents")`; Load Character (`importCharacterText`,
  line 1896) takes `{ character: … }` or the bare object, if it has stats. Its "Open game folder"
  opened the game's own folder, whose files were read live (View → Reload after editing them). It
  also caches edited class, spell, item and race files in localStorage (its in-game editing, M11's
  question).
- **The old game's data, measured:** `classes/` 129 class files (263 KB of JSON; the plan's 616 KB
  is the disk usage of all 150 files there, which include 21 draft `.txt` files in `classes/newc/`);
  `enemies/` 169 files (182 KB; 696 KB on disk); `objects/`: `spells.json` 279 KB, `artifacts.json`
  17 KB, `races.json` 17 KB, `consumables.json` 5 KB, `key-items.json` 2 KB, the fonts 2.5 MB, 7 SVGs.
- **Packages:** .NET 10's `System.IO.Compression` (Brotli, Deflate, GZip, ZLib) and
  `System.Security.Cryptography` (SHA-256) cover the decisions; `Ahwoo.BRUTAL.ZLib` ships only native
  binaries. Nothing new is needed.
- **Git:** `../.gitattributes` stores text with LF everywhere and lists binary types by extension;
  `.pak` and the record fixtures' extension aren't there yet.

## Method

The pack format, the serializer and the save files are the engine's own: nothing in Chromium or
Blender defines them, so their rules are checked by
- **round trips:** every field kind in both forms (binary and JSON) gives back the same value, floats
  bit for bit (−0, subnormals; NaN payloads in binary); packs, saves and settings written and read
  back;
- **damaged files:** each kind of damage (truncation at every boundary, a flipped bit in the header,
  the index, an entry or a hash, a wrong magic or version, duplicate or badly formed paths) gives the
  error it should and never wrong data;
- **old versions:** committed fixture files of every past version of every record type, in both
  forms, read and upgraded to the expected current value;
- **determinism:** the same input packs to the same bytes, so the committed packs change only where
  their data does.

Whatever M9 moves into packs must load as it does today: the pack-loaded data equals what the present
loaders give (`GltfImport`, `TerrainData.Load`, `Stylesheet.Parse`, the records of `MusicData.g.cs`)
value for value and bit for bit, and the tests built on them pass unchanged when fed from the packs
(the Blender scenes, the game screens, the music's call logs and fixtures). Whatever is read from the
old game matches what the old game gets in the harness's Chromium, as `make-music-data.sh` does for
the music: the themes and instruments as `MusicThemes.all()` returns them, the fonts as its
`@font-face` rules declare them, the default folder for character files as Electron's
`app.getPath("documents")` gives it. Every new test is shown able to fail by breaking each rule it
covers (`Tools/Scripts/mutate.py`).

## Design

The design the steps work from. "(Qn)" points to the format detail under "Format details" that
settles the point.

### Packs

A pack is a container of entries, each a path and its bytes; it knows nothing of records.

- **Header** (64 bytes at the start, little-endian): the magic `SVPK`, the format version (1),
  flags, the entry count, the index's offset and length, and the SHA-256 of the index's bytes (Q9).
- **Entries:** one after another after the header, in path order, each raw or Brotli-compressed
  (`BrotliEncoder` at quality 11 with a 22-bit window, so the output is deterministic); an entry
  Brotli doesn't shrink is stored raw (decision).
- **Index** (after the entries, written last): per entry its path (UTF-8, length-prefixed), offset,
  stored length, content length, flags (compressed; a record entry, step 3) and the SHA-256 of its
  uncompressed content (Q5). Paths are unique, sorted ordinally, each under its area's folder
  (`audio/themes/alder-vale`, `ui/fonts/lato-regular.ttf`), and use only lowercase ASCII letters,
  digits, `-`, `_`, `.` and `/` (Q4).
- **Writing:** into a temporary file renamed over the old pack. Re-packing keeps an unchanged entry's
  stored bytes (same content hash), so a newer .NET's Brotli can't make every committed pack differ.
- **Reading:** the header and index when mounted, the index hash checked then; entries on demand by
  positional reads (`RandomAccess`, safe from several threads), decompressed into a buffer of the
  known length, each entry's hash checked on every read; a mismatch is an error naming the pack and
  the entry, and the game stops with it (Q6). Packs are opened sharing read, write and delete, so one
  can be rebuilt while the game runs (Windows would lock it otherwise) and picked up on Reload.
- **Tool:** `Tools/Packer` packs a folder into a `.pak`, lists, verifies and extracts one; step 3 adds
  record input and the JSON dump, steps 5–8 their importers.

### Records (the serializer)

Every data record (pack entries, saves, character files) has a binary form and a JSON form, both
versioned.

- **Declaring a record:** a stable type name and version (e.g. `audio.theme`, 1) and explicit field
  numbers on its properties (`[Field(1)]`, `[Field(2)]` …), read by reflection once per type into
  compiled readers and writers; a past version is a frozen copy of the type (e.g. `ThemeSpec.V1`)
  read the same way, with a hand-written upgrade to the next version (Q10).
- **Binary form:** fields by position, no names. Lengths and counts as variable-length integers;
  numbers fixed-width little-endian, floats bit for bit; strings UTF-8; a presence byte before a
  value that may be null (null stays distinct from empty); arrays with their count, arrays of numbers
  and vectors as one block; dictionaries as their pairs in enumeration order (insertion order kept:
  the composer's data depends on it); enums as their numbers (a renamed or reordered member is a
  layout change); unions as the index of a tagged subtype the base type lists (Q12).
- **Versions:** an entry or file starts with a table of the record types it uses and their versions,
  so each type versions on its own and nested values carry no per-value cost. An older version is read
  by its own layout and upgraded step by step to the current one; a version newer than the engine
  knows is an error naming it.
- **JSON form** (Q11): the same fields by camelCase name (`tempo`, `doublings`), the type and version
  table as `"$types"` at the top, a union's subtype as `"$type"`; an absent key means the record's
  default, an unknown key is an error; doubles in their shortest round-trip form, NaN and the
  infinities as the strings `"NaN"`, `"Infinity"`, `"-Infinity"`; byte arrays as base64.
- **Guarding layouts** (Q13): committed fixtures of every version of every record type in both forms
  (`Tests/Statventure.Tests/Data/Records/`), each read, upgraded and compared with its expected value,
  the current version's also written back to the same bytes; and a committed list of every type's
  layout signature per version (field numbers, names, types, enum members): a test fails when a
  type's current layout isn't its version's recorded one, or a version has no fixture.

### The virtual file system

- **Mounts, bottom to top:** the game's packs (models, audio, ui, world, data; their paths don't
  overlap) and the shipped `classes/` folder (Q27), then every `.pak` in the mods folder in file-name
  order (ordinal), then the development folder if one is given. A path resolves to the topmost mount
  that has it; listing a folder gives the union of the mounts' paths, each from its topmost mount. A
  mod adds or replaces entries, it can't delete one; there is no in-game mod list in M9 (Q8).
- **The development folder** (Q7): loose files in the entries' own forms (a file as it is, a record as
  its JSON form at `<path>.json`), mounted only when the game is started with a development-folder
  option (command line or environment variable), re-read on Reload; no importer runs at run time.
- **Reading:** bytes, a stream, and (step 3) a record of a given type (`Load<T>(path)`), from the
  binary form in a pack or the JSON form in a loose folder.
- **Reload:** remounts everything (packs re-read their index, folders are rescanned) and tells its
  listeners; what each system reloads (themes, models, stylesheets) is up to that system.
- **Who owns it** (Q3): one instance per application (`Application.Data`), passed to what loads (the
  audio system gets its music library from it); tests and tools mount their own.
- **Where the files are at run time** (Q2): the packs in `packs/` beside the executable
  (`$STATVENTURE_GAME/packs/` from the build), the classes in `classes/` beside it as in the old game,
  mods in `mods/` in the per-user game folder.

### The game folder: saves, settings, character files

- **The folder:** Linux `$XDG_DATA_HOME/stat-fick/`, else `~/.local/share/stat-fick/`; Windows
  `%APPDATA%\Stat Fick\` (`Environment.SpecialFolder.ApplicationData`); made on first use. Resolved by
  a pure function of the system, the environment and the home folder, so the tests check every case
  without touching the real folder. It holds `settings.json`, `saves/` and `mods/`.
- **Save slots** (Q15): named slots with free names the player gives, each its own file and backup:
  `saves/<name>.dat` and `saves/<name>.dat.bak`, the names under the path rule (Q4), at most 100
  characters (Q40); the engine lists the folder, there's no fixed count, and the slot screen is the
  port's. Not compressed. A slot can be deleted: its save and backup go, files kept aside stay (Q41).
- **A save file:** a 48-byte header (magic `SVSV`, file format version, payload length, the payload's
  SHA-256; Q38), then the payload, a record (its type and version table, then the fields) through the
  serializer. Written to a temporary file in the same folder, flushed to disk, then put in place with
  the previous save kept as the backup (`File.Replace`: one call on Windows; on Linux .NET removes the
  old backup, hard-links the save as the new backup and renames the new file over the save, so the save
  is never missing and a crash can only leave the backup missing; corrected 2026-11-03, the first
  draft said two renames). Which record the save is belongs to the port; M9 tests with test records.
- **When a save is written** (Q16): whenever the game asks, as the old game's `saveCharacter` after
  every change: the record is serialized at once on the caller's thread and written by a background
  writer (the latest state wins), flushed on quit.
- **A damaged or unreadable save** (Q17): nothing is loaded. The engine reports the slot as damaged,
  with its backup's state (readable, damaged or missing), and the game asks the player what to do
  (use the backup, start new); the damaged file is kept aside, never deleted: when the slot is next
  written, as `<name>.dat.damaged` (`-2`, `-3` … when taken), and a damaged backup a write would
  replace likewise as `<name>.dat.bak.damaged`; loading only reports (Q39).
- **Settings:** `settings.json`, `{ "version": n, … }`, read with System.Text.Json into a JSON object
  that keeps every key; typed get and set with defaults; upgrades per version; written atomically when
  changed (the old game wrote its preferences on every change). A file that doesn't parse: the
  defaults are used, the file is kept as `settings.broken.json`, and it is logged (Q18); so is one a
  newer game wrote (Q42). A value of the wrong type gives that setting's default, logged; the other
  settings are used (Q43).
- **Character files:** Save Character writes the save record's JSON form (indented, with its type and
  version table), `stat-fick-character.json` in Documents by default as in the old game; Load
  Character reads one and upgrades an older version; which slot it goes into is the port's screen.
  Documents: on Linux the XDG documents folder, as Electron's `app.getPath("documents")` gives it
  (captured in 46 environments by `Tools/Scripts/make-documents-reference.sh`): Chromium's lookup of
  `XDG_DOCUMENTS_DIR` in `user-dirs.dirs`, the home folder when there is none, and the folder made
  (mode 0700, with its parents) when it is missing, failing when it can't be made; .NET's
  `SpecialFolder.MyDocuments` is no stand-in: it gives another folder in 31 of the 46 cases (with no
  entry `~/Documents`, Chromium the home folder; the first entry, not the last; the
  `XDG_DOCUMENTS_DIR` variable; and without `DoNotVerify` "" for a missing folder). On Windows the
  Documents known folder. M9 gives the functions (export to and import from a path, the Documents
  folder, a folder's listing); the picker screen is made at port time with the other screens (Q19).
  (Corrected 2026-11-03: the first draft knew neither the folder's making nor the differences.)
- **Open game folder:** opens the game folder in the system's file manager (`xdg-open`, Explorer); it
  isn't a dialog.

### Importers

Each area's importer is a class in the engine beside the area's code (so the editor, M11, can reuse
it), found by the packer through an attribute (so the steps add importers without editing a shared
file). M9 commits only the packs whose content exists: `audio.pak` (the music) and `ui.pak` (the old
game's five font faces); every importer is tested on the test assets, packed inside the test run
(Q1).

- **Models (`models.pak`):** one entry per `.glb`, holding its `ModelData` as `GltfImport` reads it
  (nodes, meshes, materials, skins, clips with events; a rig family's clip set is a `.glb` with the
  armature and its actions); state machines are entries of their own, from their JSON form (Q20).
  Pack-loaded models equal `GltfImport`'s bit for bit and render the same.
- **Music (`audio.pak`):** `make-music-data.sh` runs `music.cjs` in Chromium as now, which writes the
  records' JSON form, with the records' own names (Q22), instead of C# (keeping its key map and its
  stop on an unknown key); the packer reads them through the serializer's JSON form. One entry per
  theme, instrument, percussion voice, room and effect patch, plus one listing the themes in the old
  game's order (Q21). Before `MusicData.g.cs` is deleted, every pack-loaded record is compared with it
  once. The game's own effect patches come at port time.
- **UI (`ui.pak`):** a compiled stylesheet is the parsed and prepared rules (selectors, media queries,
  keyframes, font faces, and each declaration with shorthands expanded and invalid values dropped, as
  tokens), checked at import (no unknown property, no dropped rule, every `url()` in the packs) and
  tested by comparing computed styles with the text-parsed sheet (Q23). Fonts are registered from the
  stylesheet's `@font-face` rules (family, style, weight range, `src: url()` through the file system),
  as Chromium does: the engine starts honouring `@font-face` (Q24). Images are stored as their PNG or
  SVG bytes (raw, by the no-shrink rule) and decoded at load (Q25). The port's stylesheet joins
  `ui.pak` when it's written.
- **Terrain (`world.pak`):** the description and its two PNGs into a `TerrainData` record holding the
  16-bit samples and the height range, turned into floats at load exactly as today (Q26). Map data
  comes with the port.
- **The old game's tables (`data.pak`):** at port time (decision "Scope"); the class record and
  loader too, M9 only mounting the shipped `classes/` folder (Q27).

## Steps

Each step ends as every step does (`Docs/Engine-Plan.md`, "How each step is finished"). Steps marked
as able to run side by side touch no file in common except the docs (each its own Progress line and
its own Status entry here) and `engine/Assets/Packs/` (each its own pack file).

Order: **1 and 2** side by side; then **3** (after 1 and 2) beside **4** (after 2 only); then
**5, 6, 7, 8** side by side (after 3; 4 may still be running); **9** last (after 3, 4 and 6).

1. **Pack container and virtual file system.** Independent of every other step.
   - Creates `Engine/Statventure/Packs/` (format, writer, reader, `VirtualFileSystem` with pack and
     folder mounts and Reload), `Tools/Packer/` (pack a folder, list, verify, extract),
     `Tests/Statventure.Tests/Packs/`.
   - Changes `Statventure.slnx` (the packer) and `../.gitattributes` (`*.pak` and the record fixtures'
     extension as binary; only this step and step 3 touch it, one after the other).
   - Tests: round trips (empty, one, many and large entries; raw and compressed), the no-shrink rule,
     determinism, re-pack reuse, each kind of damage (the index at mount, entries on read), mount order
     (later wins, mods in name order, union listing), the path rule, Reload after a pack is rebuilt.
   - Format details: Q2, Q4–Q9; asked during the step: Q28–Q31.
2. **Record serializer: binary and JSON forms, versions, old-version fixtures.** Independent (of 1
   too).
   - Creates `Engine/Statventure/Records/` (declarations, binary reader and writer, JSON reader and
     writer, versions and upgrades, layout signatures), `Tests/Statventure.Tests/Records/`,
     `Tests/Statventure.Tests/Data/Records/` (fixtures, the layouts list).
   - Tests: every field kind in both forms; floats bit for bit; null and empty; dictionary order;
     unions; damaged and truncated input; a newer version refused; upgrade chains from fixtures of
     test types with several versions; a layout changed without a new version caught.
   - Format details: Q10–Q13; asked during the step: Q32–Q37.
3. **Records in packs, the packer's JSON input and dump, `Application.Data`.** After 1 and 2.
   - Changes `Engine/Statventure/Packs/` (record entries, `Load<T>`, the importer attribute: it is in the
     engine, since importers are engine classes and the engine can't reference the packer; this list
     first put it in `Tools/Packer/`), `Tools/Packer/` (JSON-form input, `dump`, the diff driver's
     script), `Engine/Statventure/Core/Application.cs` (`Data`: the game's packs,
     the `classes/` folder and the development folder), `Tests/Statventure.Tests/Infrastructure/TestPaths.cs`
     (`Packs`: the repo's `engine/Assets/Packs/`), `Tests/Statventure.Tests/Packs/`, `../.gitattributes`
     (the `*.pak` diff driver) and `engine/CLAUDE.md` (the one `git config` line per clone; Q14).
   - Tests: a record from a pack and its JSON form in a loose folder load equal; the dump.
   - Format details: Q1, Q3, Q7, Q14; asked during the step: S3-1–S3-5.
4. **Game folder, save slots, settings, character files.** After 2 only; runs beside 3 and 5–8.
   - Creates `Engine/Statventure/Storage/` (the game folder, the save slots with their background
     writer, settings, character files, opening the game folder), `Tests/Statventure.Tests/Storage/`,
     `Tests/Statventure.Tests/Data/Storage/` (old-version saves, settings and character files).
   - Tests: the folder per system and environment; slots listed, written and read back, names under
     the path rule; the background writer (latest state wins, flushed on quit); each damage reported
     with the backup's state and the damaged file kept aside; an interrupted write (a stale temporary
     file, a missing save with its backup); old-version saves and character files upgraded; settings
     keep unknown keys, default missing ones, upgrade old versions, and a broken file is kept as
     `settings.broken.json`; export then import gives the same save; the Documents folder against
     Electron's.
   - Format details: Q15–Q19.
5. **Models into `models.pak`.** After 3; beside 4, 6, 7, 8.
   - Changes `Engine/Statventure/Render3D/ModelData.cs` (record declarations, loading from the file
     system), `Render3D/Model.cs` (`Load` from the file system), `Animation/StateMachineDefinition.cs`
     (record declarations); creates the model importer in `Render3D/`; tests in
     `Tests/Statventure.Tests/Render3D/` (each test model loaded from a pack equals `GltfImport`'s; a
     Blender scene rendered from pack-loaded models matches) and `Tests/Statventure.Tests/Animation/`
     (the M7 machines through both forms).
   - Commits no pack: there is no game model yet (Q1).
   - Format details: Q20.
6. **Music and effects into `audio.pak`; `MusicData.g.cs` removed.** After 3; beside 4, 5, 7, 8.
   - Changes `Engine/Statventure/Audio/Music/` (`ThemeData.cs` declarations; a music library loaded
     from the file system; `Soundtrack`, `Composer` and `GraphBuilder` reading it instead of the static
     `MusicData`; `MusicData.g.cs` deleted), `Audio/AudioSystem.cs`, `Audio/Effects/EffectPatch.cs`
     (declarations, loading), `Core/Application.cs` (the audio system's data: one line),
     `Tools/AudioReference/music.cjs` and `Tools/Scripts/make-music-data.sh` (JSON out, then the
     packer), `Tools/PlayTheme/`, `Tools/AudioBenchmark/`, `Tests/Statventure.Tests/Audio/` (the music
     tests on the pack), `engine/CLAUDE.md` (the script's line). Commits `engine/Assets/Packs/audio.pak`.
   - Checks: the pack-loaded records equal `MusicData.g.cs`'s (once, before it goes); every music
     test unchanged; `test-themes.sh` once at the end.
   - Format details: Q21, Q22.
7. **Stylesheets, fonts and images into `ui.pak`.** After 3; beside 4, 5, 6, 8.
   - Changes `Engine/Statventure/UI/Css/` and `UI/Style/Stylesheet.cs` (declarations of the compiled
     form, a sheet built from it), `Text/FontLibrary.cs` and `UI/Dom/UiDocument.cs` (fonts from
     `@font-face`), `UI/UiHost.cs` (images and SVG text from the file system); creates the UI importer
     in `UI/`; tests in `Tests/Statventure.Tests/Ui/` (the old stylesheet compiled gives the same
     computed styles as it parsed; the 22 game screens lay out and paint as before through it; the
     fonts from `@font-face` as `TestFonts` registers them). Commits `engine/Assets/Packs/ui.pak` with
     the five font faces (Q1).
   - Format details: Q23–Q25.
8. **Terrain into `world.pak`.** After 3; beside 4–7.
   - Changes `Engine/Statventure/Render3D/Terrain.cs` (declarations, loading from the file system);
     creates the terrain importer; tests in `Tests/Statventure.Tests/Render3D/` (the test terrain
     loaded from a pack equals the file-loaded one bit for bit; a terrain scene renders the same).
   - Commits no pack: there is no game terrain yet (Q1).
   - Format details: Q26.
9. **The game's data at run time.** Last: after 3, 4 and 6 (it changes `Application.cs` after them).
   - Changes `Tools/Scripts/build.sh` and `build.ps1` (copy `engine/Assets/Packs/*.pak` into
     `$STATVENTURE_GAME/packs/`), `Engine/Statventure/Core/Application.cs` (the mods folder from step
     4's game folder; Reload), `Docs/Engine-Plan.md`'s Folders table (`Assets/Packs/`; `packs/`,
     `classes/` and the per-user `mods/` at run time).
   - Tests: mount order with mods and a development folder over the game's packs; Reload.
   - Format details: Q27.
   - The shipped `classes/` folder holds the class files under names that follow the path rule
     (Q31: the old game's `Black Knight.json` becomes `black-knight.json`), renamed when the port makes
     it; mounted as the old game names them, they would fail the mount (Q29).

## Format details (user, 2026-11-02, asked after the design doc)

Every answer was the recommended option except Q15 and Q17. Also in `Docs/Engine-Plan.md`,
"Decisions for M9".

- **Q1. What M9 commits as packs:** only what exists: `audio.pak` (55 themes, 34 instruments, 21
  percussion voices, 7 rooms) and `ui.pak` (the old game's five font faces); `models.pak`, `world.pak`
  and `data.pak` appear when the port makes their content, the port's stylesheet joins `ui.pak` when
  it's written; every importer is tested on the test assets, packed inside the test run. Test data
  never ships, and no empty pack has to be kept in step. (Not the old `styles.css` compiled in, not
  test assets as stand-ins, not five packs with empty ones.)
- **Q2. Run-time locations:** `packs/` and `classes/` beside the executable, mods in `mods/` in the
  per-user game folder: an installed game's folder may be read-only, the player's own is writable and
  is what "Open game folder" shows. (M12 decides the rest of the shipped layout.)
- **Q3. How the engine reaches its data:** one file system per application (`Application.Data`),
  passed to the loaders; tests and tools mount their own (the music data being static today would
  otherwise keep tests from mounting different packs side by side). (Not a static one.)
- **Q4. Path rule:** lowercase ASCII `a–z`, `0–9`, `-`, `_`, `.` and `/` only, checked when packing
  and mounting, so Linux and Windows agree on every lookup in a loose folder. (Not any case matched
  exactly.)
- **Q5. Entry hashes** cover the uncompressed content: the same hash whether stored raw, compressed or
  loose, as the editor's change detection and the packer's reuse need. (Not the stored bytes, not
  both.)
- **Q6. Checking:** the index hash at mount, an entry's hash on every read; a mismatch is an error
  naming the pack and the entry, and the game stops. No data is used unchecked. (Not a verify command
  only, not falling back to a lower mount.)
- **Q7. The development folder** holds entries in their own forms (a record as `<path>.json`), is
  mounted only with a development option, and is re-read on Reload. (Not importer sources, not a
  fixed folder always mounted.)
- **Q8. Mods:** every `.pak` in the mods folder, in file-name order; mods add or replace entries, no
  deletes, no in-game list. (Not a `mods.json`, not deletion entries.)
- **Q9. Byte layout** as proposed: the 64-byte `SVPK` header, entries in path order, the index at the
  end, Brotli at quality 11 with a 22-bit window, deterministic output.
- **Q10. Layouts:** `[Field(n)]` attributes read by reflection into compiled readers and writers; past
  versions as frozen copies of the type plus hand-written upgrades. (Not hand-written methods per
  type, not a source generator.)
- **Q11. JSON form:** camelCase names, a `$types` table, absent = default, an unknown key = an error,
  shortest round-trip doubles, NaN and the infinities as strings, bytes as base64. (Not PascalCase,
  not unknown keys ignored.)
- **Q12. Unions** through tagged subtypes (an index in binary, `"$type"` in JSON): the old game's
  tables are full of tagged objects, and the stylesheet's rules need them. (Not left out of M9.)
- **Q13. Layout guard:** committed layout signatures per version plus the fixtures. (Not the fixtures
  only.)
- **Q14. Reviewing packs:** a `dump` command and a git text-conversion driver named in
  `.gitattributes`, so `git diff` shows the dumps; each clone enables it with one documented
  `git config` line. (Not the dump only.)
- **Q15. Saves (not the recommendation):** named save slots. Asked as a follow-up: free names given by
  the player, each slot its own file and backup (`saves/<name>.dat` and `saves/<name>.dat.bak`, the
  names under the path rule), the engine lists the folder, no fixed count; the slot screen is the
  port's. Not compressed. (The recommended single save, as the old game's one character, was not
  chosen; nor one save with rolling backups.)
- **Q16. Writing saves:** on every request: serialized on the caller's thread, written by a background
  writer (the latest state wins), flushed on quit. (Not synchronous writes, not every few seconds.)
- **Q17. A damaged or unreadable save (not the recommendation):** nothing is loaded and the player is
  asked: the engine reports the slot as damaged with its backup's state, the game asks what to do
  (use the backup, start new), and the engine keeps the damaged file aside rather than deleting it.
  (The recommended automatic load of the backup with a notice was not chosen; nor a silent one.)
- **Q18. A `settings.json` that doesn't parse:** the defaults, the file kept as
  `settings.broken.json`, logged; the game always starts. (Not refusing to start.)
- **Q19. The file picker:** M9 gives the export and import functions, the Documents folder and folder
  listings; the picker screen is made at port time with the other screens. (Not an engine picker
  element.)
- **Q20. Model entries:** one entry per `.glb` holding its `ModelData`; state machines separate. (Not
  meshes and clips split.)
- **Q21. Music entries:** one per theme, instrument, percussion voice, room and effect patch, plus a
  theme-order list, so one theme can be replaced alone. (Not one record for the library.)
- **Q22. Music JSON keys:** the records' own names. (Not the old keys `t`, `i`, `dbl`.)
- **Q23. A compiled stylesheet** is the parsed and prepared rules as tokens, checked at import, and
  tested by comparing computed styles with the text-parsed sheet. (Not checked text parsed at load,
  not typed values.)
- **Q24. Fonts** are registered from `@font-face` rules, as Chromium does; the engine starts honouring
  them. (Not a font list record.)
- **Q25. Images** are stored as their PNG or SVG bytes (raw) and decoded at load. (Not decoded RGBA.)
- **Q26. Terrain** is stored as the 16-bit samples plus the height range, turned into floats at load
  as today. (Not float heights.)
- **Q27. Classes:** the class record and loader come at port time; M9 only mounts the shipped
  `classes/` folder, so a mod or the development folder can override a class. (Not a class record
  now.) Its files get names under the path rule when the port makes it (Q31).

Asked during step 1 (2026-11-03), every answer the recommended option:

- **Q28. Names Windows can't hold** are refused too, wherever paths are checked (packing, mounting,
  the file system's calls): a segment ending in a dot, Windows' reserved device names (`con`, `prn`,
  `aux`, `nul`, `com0`–`com9`, `lpt0`–`lpt9`, alone or before an extension, e.g. `aux.json`) and a
  segment longer than 255 characters, so every valid path works the same in a pack and in a loose
  folder on Linux and Windows (Q4's reason). (Not the character set only.)
- **Q29. A file in a loose folder whose name breaks the path rule** (the development folder, the
  shipped `classes/` folder) is an error naming the file, and the mount fails, as packing does. The
  development folder must be kept free of editor backups (`x.json~`) and Windows' `Thumbs.db`. (Not
  skipped with a log line.)
- **Q30. Mods** are the files directly in the mods folder whose names end in `.pak` in any case
  (`MyMod.PAK` too), on both systems, mounted in the ordinal order of their names; a mod's file name
  isn't under the path rule (it isn't an entry path). (Not a lowercase `.pak` only.)
- **Q31. The old game's class files** (129, named with capitals and spaces: `Black Knight.json`) are
  renamed to names under the rule (`black-knight.json`) when the port makes the shipped `classes/`
  folder; the old game's own files stay as they are. One path rule for everything. (Not an exception
  for `classes/`.)

Asked during step 2 (user, 2026-11-03; every answer the recommended option), details of the record
forms the questions above left open:

- **Q32. Enum values in JSON** are the member's name in camelCase (`"lowpass"`, `"cubicSpline"`); an
  unknown name is an error, and renaming a member is a layout change. (Not the name as declared, not
  the number.)
- **Q33. What the JSON writer leaves out:** only a null field whose default is null; every other
  value is written, values equal to a non-null default too, and a null whose default isn't null is
  written as `null`. A field's default is part of its layout signature, so a changed default is a
  new version (absent keys in older JSON depend on it). (Not every default left out, not every null
  written.)
- **Q34. An ordered list of pairs** (`IReadOnlyList<KeyValuePair<K, V>>`, as a drums layer's patterns)
  has the dictionary form: binary a count then the pairs, JSON an object with its keys in order; a
  repeated key is an error. (Not an array of `[key, value]` arrays, not of `{"key", "value"}` objects.)
- **Q35. `$types`** is an object of record type name → version, the root's type first. (Not an array
  of `{"type", "version"}` objects.)
- **Q36. Vectors, quaternions and matrices in JSON** are arrays of numbers in System.Numerics' order
  (`[x, y, z]`, `[x, y, z, w]`, a matrix's M11, M12 … M44). (Not objects with named components.)
- **Q37. Adding a subtype to a union is a new version of the union:** its tag list is part of its
  layout signature. (Not tags that may only be added without a new version.)

Asked during step 3 (user, 2026-11-03; every answer the recommended option; numbered S3-n because
step 4, running beside it, asked Q38 on):

- **S3-1. Which loose `.json` files are records:** by content, one rule everywhere. A `.json` file
  whose top-level object starts with the key `"$types"` (as every record's JSON form does) is the
  record at its path without `.json`, and only that; every other file is a file at its own path. The
  packer's input, mounted loose folders (the development folder, `classes/`) and `extract`'s output
  (records written as `<path>.json`) follow it, so a pack and its extracted folder hold and list the
  same entries. Each loose `.json` file's first bytes are read at mount and on Reload; one whose start
  isn't valid JSON fails the mount naming it (a broken record file never lets the pack's record show
  through). The writer refuses a file entry holding a record's JSON form. (Not by name, a loose
  `<path>.json` answering both `Load<T>(path)` and reads of the file, with listings that differ from the
  pack's.)
- **S3-2. The dump:** one JSON document: the format version and entry count, then each entry in path
  order with its path, kind (`record` or `file`), content length, stored form (`brotli` or `raw`) and
  stored length, SHA-256; a record also its JSON form (the current versions, as extract writes it; a
  type not known, or a record that doesn't read, shows the problem instead); a file that is UTF-8 text
  without control characters but tab, CR and LF also its text as an array of lines; other files nothing
  more; no offsets (they move whenever an earlier entry changes). (Not content only, without the stored
  form and the text; not binary files' whole content as base64.)
- **S3-3. Importers** are chosen by the ending of the source file's name: `[Importer("models", ".glb")]`
  on a class implementing `IImporter.Import(ImportContext)`; packing a folder sends each file with a
  declared ending through its importer (the longest ending wins, two importers with one ending are an
  error), which adds records and files and may read other files of the folder, which then aren't packed
  themselves; then records' JSON forms become records and every other file goes in as it is. A source
  whose name doesn't say what it is gets a distinguishing ending (a terrain description, e.g.
  `<name>.terrain.json`: step 8's to settle). The packer looks for importers in the engine's assembly.
  (Not by name per command, `packer import <importer> …`; not both.)
- **S3-4. The game's packs** in `Application.Data` are the five area packs by name, in the order
  `models`, `audio`, `ui`, `world`, `data`, each mounted if it is in `packs/`; other files there are
  ignored, and no `packs/` folder (the engine's tests, the sandbox) means no packs. Then `classes/`, the
  mods (step 9) and the development folder. (Not every `.pak` in `packs/`.)
- **S3-5. The development folder** is `ApplicationOptions.DevelopmentFolder`, which the game's own
  command line fills at port time, else the folder the environment variable `STATVENTURE_DEV_FOLDER`
  names; a folder named that doesn't exist is an error. (Not the engine reading `--dev` from the command
  line itself; not the option only.)

Asked during step 4 (user, 2026-11-03; every answer the recommended option):

- **Q38. The save file's header:** 48 bytes, little-endian: the magic `SVSV`, the format version
  (u32, 1), the payload's length (u64), the payload's SHA-256; then exactly the payload. A short file,
  another magic, a length that doesn't match or a hash mismatch is damage; a format version above 1 is
  a newer format (unreadable). (Not 64 bytes with flags and reserved bytes as the pack's header.)
- **Q39. Keeping a damaged save aside:** when the slot is next written, never by loading: loading only
  reports; when the game then writes the slot (after "use the backup" or "start new"), the save it
  would replace, damaged or unreadable, is kept as `saves/<name>.dat.damaged` (`-2`, `-3` … when taken,
  never overwritten), the new save put in place and the backup left as it is; a damaged backup a write
  would replace is kept the same way (`<name>.dat.bak.damaged`). If the player decides nothing, the slot
  stays as it was. (Not kept aside at once when loading finds it, not a `saves/damaged/` folder with
  dates.)
- **Q40. Slot names** have at most 100 characters, so every file of a slot fits Linux's and Windows'
  255-character file names and the paths stay under Windows' classic 260. (Not 255 less the longest
  suffix.)
- **Q41. Deleting a slot:** `Delete(name)` removes the save and its backup; files kept aside stay; the
  port's screen asks the player first. (Not moved aside as `.deleted`, not left to the port.)
- **Q42. A `settings.json` of a newer version** than the game's is treated as one that doesn't parse
  (Q18): the defaults, the file kept as `settings.broken.json`, logged. (Not its known keys used with
  the file left unwritten.)
- **Q43. A setting of the wrong type** (`"musicVolume": "loud"`) gives that setting's default, logged;
  the other settings are used; the value stays in the file until the setting is set. (Not the whole
  file treated as broken.)

Asked during step 5 (user, 2026-11-03; the recommended option; numbered S5-n as step 3's were):

- **S5-1. A model or state machine that reads but breaks its rules** (possible only from a hand-edited JSON
  form in the development folder or a mod's pack: the importer and the builder check them) is checked at
  load: `ModelData.Load(files, path)` checks what `GltfImport` checks plus the indices it leaves to BRUTAL
  (node, mesh, skin, material and root indices; joint indices against the skin; attribute lengths and
  indices against the vertex count; channel nodes, key counts, increasing key times), and
  `StateMachineDefinition.Load(files, path)` runs `Validate()`; a failure is a `DataFileException` naming
  the pack and entry or the file, as an unreadable record is. Q6's "no data used unchecked", and an
  out-of-range joint index would otherwise read past the GPU's joint matrices. (Not state machines only,
  not nothing at load.) Accepted with it, as the step chose them: the record type names `model.*` and
  `animation.*`, field numbers in the declarations' order, and machines anywhere by path, nothing tying
  one to a model.

## How it works

Filled in as the steps are done.

### Packs and the virtual file system (step 1)

Code in `Engine/Statventure/Packs/`, namespace `Statventure.Packs`: `PackPath` (the path rule),
`PackFile` (open and check, read, verify, extract), `PackWriter` (add entries or a folder, write),
`VirtualFileSystem` with `Mount` (`Pack`, `Folder`, `Mods`), and `DataFileException` (a pack or loose
file that can't be used: `File`, `Entry`, `Problem`). The tool `Tools/Packer` puts them on the command
line: `pack <folder> <file.pak>`, `list`, `verify` (exit code 1 when an entry is damaged), `extract
<file.pak> <folder>` (the folder must be empty or missing); it is a thin shell, the tests cover the
engine's functions. Tests: `Tests/Statventure.Tests/Packs/`.

- **The path rule** (`PackPath`; Q4, Q28): only `a–z 0–9 - _ . /`; names between `/` not empty (no
  leading, trailing or doubled `/`), at most 255 characters, not ending in a dot (so no `.` or `..`),
  not a Windows device name (`con`, `prn`, `aux`, `nul`, `com0`–`com9`, `lpt0`–`lpt9`, alone or before
  the first dot: `aux.json`, `nul.tar.gz`). Checked when packing, when opening a pack (its index), when
  mounting a loose folder (each file's path, Q29) and on every call (`ArgumentException`).
- **Byte layout, format version 1** (little-endian; `PackFormat`):

  | Bytes | Field |
  | --- | --- |
  | 0–3 | `SVPK` |
  | 4–7 | format version (u32), 1 |
  | 8–11 | flags (u32), none defined: must be 0 |
  | 12–15 | entry count (u32) |
  | 16–23 | the index's offset (u64) |
  | 24–31 | the index's length (u64) |
  | 32–63 | SHA-256 of the index's bytes |

  Then the entries' stored bytes, one after another from byte 64 in ordinal path order, then the
  index, which ends the file. An index entry: the path's length (u16) and its ASCII bytes, the offset,
  stored length and content length (u64 each), the flags (u32: bit 0 compressed, bit 1 a record, added
  by step 3) and the SHA-256 of the content (62 bytes plus the path). An empty pack is the 64-byte header.
- **Opening** (`PackFile.Open`) reads the header and the index and refuses, with a
  `DataFileException` naming the pack (and the entry when one is at fault): a file shorter than the
  header; another magic; a version other than 1; header flags; an index that doesn't run from inside
  the file to its end; an index whose hash doesn't match; then, in the index (whose hash matched, so
  only a badly made pack gets this far): an index ending inside an entry or going on after the
  counted entries, a path outside the rule, a path not after the one before it (a duplicate, or out of
  ordinal order), an entry that doesn't start where the one before it ends or runs into the index,
  unknown entry flags, a content too large for one array, a compressed entry not smaller than its
  content or a raw one whose lengths differ, entries that don't end where the index starts, and an
  entry that is also another entry's folder (`a` beside `a/b`: a loose folder can't hold both). The
  file is opened sharing read, write and delete (`File.OpenHandle`, `FileOptions.RandomAccess`).
- **Reading** (`PackFile.Read`): the stored bytes by positional reads (`RandomAccess.Read`, safe from
  several threads; a file shorter than the index says is an error), decompressed with a
  `BrotliDecoder` into a buffer of the content's length, which must end the stream exactly (status
  done, every stored byte consumed, the whole buffer written; .NET's static `TryDecompress` would accept
  bytes after the stream), then the content's SHA-256 compared with the index's, on every read: nothing
  is cached. A bit flipped in a compressed stream can leave the content as it was (the window size of a
  short stream, say); the content is then right and is given. `Verify` reads every entry and lists the
  damaged ones; `ExtractTo` writes every entry as a file (since step 3 in its loose form: a record as
  its JSON form at `<path>.json`).
- **Writing** (`PackWriter`): entries are added by path (a duplicate, or an entry where another has its
  folder, is refused) or from a folder (every file at any depth, hidden ones too, each name checked).
  Each content is hashed and Brotli-compressed at quality 11 with a 22-bit window (entries in parallel;
  the output is the same every time), stored compressed only when that is strictly shorter. `Write(path)`
  reuses the pack it replaces: an entry whose content hash it already holds keeps that entry's stored
  bytes and flags (also under another path), after decoding them and checking the hash; a damaged
  one is compressed afresh and named in the result (`PackWriteResult.DamagedInOld`), and an unreadable
  old pack is replaced whole (`OldProblem`). The bytes go into a temporary file beside the pack
  (`<name>.<random>.tmp`), flushed to disk, then renamed over it, so a pack someone has open keeps its
  old contents (Linux) until they open it again. `WriteTo(stream, reuse)` writes to a stream.
- **The virtual file system** (`VirtualFileSystem`): mounts are given bottom to top
  (`Mount.Pack(file)`, `Mount.Folder(folder, at)` with an optional place below which the files appear,
  e.g. the shipped classes at `classes`, and `Mount.Mods(folder)`); each path resolves to the topmost
  mount that has it. `Exists`, `ReadAllBytes`, `Open` (a read-only stream over the checked content),
  `Origin` (the pack or loose file that gives a path) and `List(folder)` (every path below the folder,
  at any depth, in ordinal order, each once; `""` lists all). A damaged entry is an error even when a
  lower mount has the path (Q6). A loose folder's set of files is taken when it is mounted, its
  contents when they are read (an edit shows at once, a new file after Reload). Mods are the files
  directly in the folder whose names end in `.pak` in any case, in the ordinal order of their names
  (`C.PAK` before `a.pak`); a missing mods folder means none, each mod is logged ("Data" category); a
  missing pack or loose folder is an error.
- **Reload** mounts everything afresh (packs reopened and checked, folders rescanned, the mods folder
  listed again) and only then swaps the new mounts in, closes the old packs and raises `Reloaded`; when
  mounting fails it throws and the old mounts stay (and what it had opened is closed). Reads, Reload and
  `Dispose` share a reader-writer lock, so reads from several threads go on safely during a Reload.
- **Decided as internal design (2026-11-03, accepted by the user with the step's questions):** the
  well-formedness of paths above, the entry-and-folder rule within one pack (across mounts both are
  allowed), a missing mods folder meaning no mods while a missing pack or development folder is an
  error, and the atomic Reload.
- **Not checked:** Windows. There the sharing matters (Linux's `FileShare` is only an advisory lock, so
  the tests can't tell read sharing from read, write and delete sharing), and whether a pack open with
  delete sharing can be renamed over (Assets.md's reason for the sharing) is still to be seen on the
  user's Windows PC.

### Records (step 2: `Engine/Statventure/Records/`)

- **Declaring** (`RecordAttributes.cs`): `[Record("audio.theme", 2)]` on the type (names: lowercase
  ASCII letters, digits, `.`, `-`, `_`), `[Field(n)]` on each serialized property (numbers from 1,
  unique; on a positional record `[property: Field(n)]`). A record is a class, record or struct made
  either through the public constructor whose parameters are all fields (a positional record, matched
  by name and type) or through the parameterless one, then the other fields' setters (init or set). A
  past version is a frozen copy nested in the current type with the same name and its own version
  (`Creature.V1`), with an instance method `Upgrade()` returning the next version's type; every version
  from 1 must have one. A frozen copy's fields hold current types: each record type versions on its
  own. A union is an abstract type with `[Subtype(tag, typeof(X))]` (tags from 1); its subtypes are
  plain records deriving from it, each with its own name and version; a `[Field]` on the union is a
  field of every subtype. A union's past version is a frozen copy listing the old tags (no upgrade:
  its values are its subtypes'). A plain record may become a union (its last plain version upgrades
  into a subtype), not back.
- **Field kinds** (`RecordCodecs.cs`): bool, the integer types, float, double, string, `byte[]`,
  enums (not `[Flags]`, no two members with one value), `Vector2/3/4`, `Quaternion`, `Matrix4x4`,
  nullable value types, nullable references (from C#'s nullable annotations; an unannotated
  reference is refused), lists (`T[]`, `List<T>`, `IReadOnlyList<T>` and the other list interfaces,
  read as arrays except `List<T>`), dictionaries (`Dictionary`, `IReadOnlyDictionary`, `IDictionary`,
  `OrderedDictionary`; keys strings, integers or enums), lists of pairs, records and unions. Anything
  else is refused when the type is first used, with the field named.
- **Defaults and required keys:** an absent JSON key keeps what the declaration gives (a parameter's
  default, a property's initializer); a `required` property, a positional parameter without a default
  (nullable or not: a state's `string? Clip` must be given, and its null is written), and a
  non-nullable reference whose default is null must be present.
- **Compiled once:** `RecordFamily` (one per record name: the current type, the frozen copies, each
  version's layout built lazily, the upgrades checked when the type is first used) and `RecordLayout`
  (`PlainLayout<T>`: compiled getters per field and one compiled factory `(values, present) => new T(…)
  { … }` per type; `UnionLayout`: the tags of its version). Thread-safe; a bad declaration throws an
  `InvalidOperationException` naming the type and field.
- **Binary form** (`BinaryRecordIO.cs`): the table (a count, then each type's name and version, the
  root's type first, then the others in the order the value meets them; only the types it uses), then
  the root's fields in number order. Counts and lengths are LEB128 (only the shortest form, at most 64
  bits); numbers are little-endian at their width (enums at their underlying type's); floats bit for
  bit, NaN payloads included; strings strict UTF-8; a presence byte (0 or 1) before a nullable value;
  lists a count then the elements (numbers and vectors copied as one block); dictionaries and pair lists
  a count then key and value; a union its tag, then the subtype's fields (the union's own fields first,
  by number). Bools and presence bytes must be 0 or 1, enum numbers members, a count no more than the
  bytes left (records without fields: at most 65 536), nesting at most 256 records deep, and nothing
  may follow the root.
- **JSON form**: `{"$types": {name: version, …}, …}` then the fields by camelCase name in number
  order; a union value's object starts with `"$type"` (its subtype's record name); enum members by
  camelCase name; doubles and floats in their own shortest round-trip form, NaN and the infinities as
  strings; `byte[]` base64; vectors and matrices as one-line arrays; dictionaries as objects (keys as
  text: integers in their one decimal form, enums by name). Written indented by two spaces, LF line
  ends, a final line end, only JSON's escapes (non-ASCII kept). Read strictly: no comments or trailing
  commas, a byte order mark allowed, an unknown or repeated key an error, `$type` only in a union,
  `$types` only at the top.
- **Versions:** each type's version comes from the table; an older one is read by its frozen copy and
  upgraded step by step (an exception in an upgrade becomes a format error); a newer one than the
  engine has, or a type the table doesn't list, is an error naming it; the first table entry must be
  the type asked for.
- **API** (`RecordSerializer.cs`): `ToBinary`, `FromBinary`, `ToJson` (string), `ToJsonUtf8`,
  `FromJson` (string or UTF-8), generic and by `Type`; `RootTypeName` (the record type a binary form
  holds); `LayoutText` (a type's signatures); `FindTypes` (the current record types of assemblies by
  name). Errors are `RecordFormatException`s: `Reason` and `Path` (`$.limbs[1].kind`), on reading and
  on writing (a null in a non-nullable field, a lone surrogate, a repeated key in a pair list, an enum
  value that isn't a member).
- **The guard** (`Tests/Statventure.Tests/Records/`, data in `Tests/Statventure.Tests/Data/Records/`):
  `Layouts/<name>.txt` holds every version's layout signature of each record type of the engine and the
  tests (field numbers, camelCase names, kinds with enum members and their numbers, nested records by
  name, `(required)` or the default as JSON; a union's tags); `Fixtures/<root type>/<label>.rec` and
  `.json` are the fixtures. `RecordLayoutTests` fails when a type's layout isn't its version's recorded
  one, a recorded type is gone, a version of a type has no fixture holding a value of it in either form,
  the two forms of a fixture differ, or a fixture of the current versions isn't written back to the same
  bytes. A new type or version: declare it, add its fixtures with `RecordFixtures.Ensure(label, value)`
  in a test, and run the tests once with `STATVENTURE_RECORD_FIXTURES=1`, which writes missing layouts
  and fixtures (never an existing one). The tests' own types are `test.*` (`TestRecords.cs`: every field
  kind; a family with three versions, a nested type with two, a union that gained a subtype, written
  through a downgrade hook as the past versions would have been); `RecordVariants.cs` has deliberately
  broken or varied declarations, which the guard leaves out.

### Records in packs, the packer, `Application.Data` (step 3)

Code in `Engine/Statventure/Packs/` (`PackWriter`, `PackFile`, `VirtualFileSystem`, `LooseRecord`,
`LooseFolder`, `RecordTypes`, `PackDump`, `Importers`), `Core/Application.cs`, `Tools/Packer/`. Tests in
`Tests/Statventure.Tests/Packs/` (`PackRecordTests`, `LooseRecordTests`, `PackFolderTests`,
`PackDumpTests`, `ApplicationDataTests`, `CommittedPackTests`; the test importers in `TestImporters.cs`;
the records are step 2's test types).

- **Record entries:** bit 1 (value 2) of an index entry's flags; the content is the record's binary
  form, stored, compressed and hashed as any entry. `PackWriter.AddRecord<T>(path, value)` (or by
  `Type`) writes the current versions; `PackEntry.IsRecord`. A record `x` and a file `x.json` can't share
  a pack (the record's loose form is that file): the writer and `PackFile.Open` refuse it; records `x`
  and `x.json` (loose `x.json.json`), or files `x` and `x.json`, are fine. An entry is never also a
  folder, records included. Re-packing keeps an unchanged content's stored bytes and compression, but
  whether the entry is a record is the new entry's.
- **Loose forms** (S3-1, `LooseRecord`, `LooseFolder`): a loose `.json` file is a record when its
  top-level object's first key is `"$types"` (after an optional byte order mark and whitespace; an
  escaped key counts; only as much of the file is read as that takes, so a long whitespace run or first
  key is read in growing chunks, and a read giving fewer bytes than asked for, as some file systems do,
  is waited on, a cut byte order mark too); a start that isn't valid JSON (empty, cut off, a comment, single quotes,
  not UTF-8) is a `DataFileException` naming the file. A record file whose path without `.json` breaks
  the path rule (`a..json`, `.json`), a record `a` beside a file `a`, or a record `a` beside `a/b` fail the
  mount naming the record's file. A file entry named `.json` must start as JSON that isn't a record's
  form (the writer refuses either, with an `ArgumentException`): otherwise its loose form would be a record
  or wouldn't mount.
- **Reading** (`VirtualFileSystem`): `Load<T>(path)` and `Load(Type, path)` take the topmost mount's entry:
  a pack's record (binary form) or a loose folder's `<path>.json` (JSON form), upgraded to the current
  version, read and parsed afresh each time (outside the lock). `IsRecord(path)`. Errors are
  `DataFileException`s naming the pack and entry or the loose file, never a fall back to a lower mount
  (Q6): a file where a record is asked for ("a file, not a record (expected a test.creature)"), a record
  read as a file by `ReadAllBytes` or `Open` ("a record, not a file (records are read with Load)"), a
  record of another type ("a record of type test.creature, not test.part"; a record stored as a union's
  subtype has the subtype as its type), a record that doesn't read ("not a readable test.creature record:
  …", the `RecordFormatException` inside). A type that isn't a record type is an `InvalidOperationException`.
  A loose record file's contents are read when asked for (an edit shows at once); whether it is a record
  is decided at mount and Reload.
- **Packing a folder** (`PackWriter.AddFolder(folder, types, importers)`, `packer pack`; S3-3): files in
  ordinal path order; first each file whose name ends in an importer's ending (and is longer than it)
  goes through that importer, one instance per importer per folder; then every file no importer took or
  read is an entry: a record's JSON form becomes the record (its type found by the first name of its
  `"$types"` in `types`, by default `RecordTypes.Engine`, the engine's record types; packed in the current
  versions), any other file goes in as it is. Each problem is a `DataFileException` naming the file: a
  type not known, a form that doesn't read, an importer's failure ("the models importer: …", the
  exception inside), an entry made twice.
- **Importers** (`Importers.cs`; S3-3): `[Importer(name, endings…)]` on a class implementing
  `IImporter` with a public parameterless constructor. Endings start with a dot, are names under the path
  rule and don't end in a dot; `.json` is refused (it would take records' JSON forms); two importers with
  one ending or one name are an `InvalidOperationException`. `Importers.Engine` (found in the engine's
  assembly; none yet, steps 5–8 add them), `Importers.Find(assemblies[, include])`, `Importers.Of(types)`,
  `Importers.None`, `For(path)` (the longest ending). An importer gets an `ImportContext`: `SourcePath`
  (`models/hero.glb`), `SourceFile` (on disk, for loaders that read by path), `Ending`,
  `PathWithoutEnding` (`models/hero`), `Sources` (every file of the folder), `Read()`, `ReadSource(path)` and
  `SourceFileOf(path)` (another file of the folder, which then isn't packed itself; another importer's
  source can't be used), `Add` and `AddRecord`.
- **Extracting** (`PackFile.ExtractTo(folder, types)`, `packer extract`): every entry in its loose form,
  records as their JSON forms at `<path>.json` (`PackFile.RecordJson`), all read and converted before
  anything is written; a record whose type isn't known stops it with nothing written. Extracting a pack
  and packing the folder again gives the same bytes when its records are in the current versions.
- **The dump** (`PackDump.ToText` / `ToUtf8`, `packer dump`; S3-2): indented by two spaces, LF line ends,
  a final line end, non-ASCII kept (System.Text.Json's relaxed escaping, as the records' JSON form).
  `{"format": 1, "count": n, "entries": [{"path", "kind", "length", "stored", "storedLength", "sha256",
  then "record" (the JSON form, its lines indented into the entry), or "text" (the lines, split at LF, a
  CR kept as `\r`; an empty file is `[""]`), or "problem"}]}`. Text means strict UTF-8 without a
  `char.IsControl` character other than tab, CR and LF (C0, DEL and C1 controls make a file binary).
- **Reviewing packs** (Q14): `../.gitattributes` has `*.pak binary diff=pak` (git applies `binary` then
  `diff=pak`: diff driver `pak`, text and merge unset). Each clone runs once, from the repo's root:
  `git config diff.pak.textconv engine/Tools/Packer/textconv.sh` (also in `engine/CLAUDE.md` and the
  `.gitattributes` comment); git runs it from the top of the working tree, also from a subfolder. The
  script builds the packer (Release; a no-op of about 2 s when it is current) and prints the dump, so
  `git diff`, `git log -p` and `git show` show packs as their dumps (`--stat` still counts them as binary).
  Not checked on Windows (git there runs the script with its own bash; the script needs `env.sh` to find
  dotnet).
- **`Application.Data`** (Q2, Q3, Q27, S3-4, S3-5): mounted at the start of `Run()`, after the log file
  opens and before the window, the GPU, the sound and `Load()` (a damaged pack stops the start, logged,
  before a window opens), each mount logged ("Data" category); disposed last. `Application.DataMounts`
  (internal, tested without a window): the area packs `Application.AreaPacks` (`models`, `audio`, `ui`,
  `world`, `data`) from `packs/` in `AppContext.BaseDirectory`, each if it is a file there; `classes/`
  there at `classes` if it is a folder; then the development folder, `ApplicationOptions.DevelopmentFolder`
  else `$STATVENTURE_DEV_FOLDER` (`Application.DevelopmentFolderVariable`; empty means none), made a full
  path from the current folder; missing, the mount fails ("No folder …"). Mods come between the classes
  and the development folder with step 9.
- **`TestPaths.Packs`:** the repo's `engine/Assets/Packs/`. `CommittedPackTests` checks every committed
  pack there: one of the area packs, opens, every entry's hash, every record readable with the engine's
  record types (none is committed yet: steps 6 and 7 commit the first).
- **Later needs:** packs are the source of truth and the editor (M11) edits them, so once a pack holds
  both imported and editor-made entries (`audio.pak` with the port's effect patches), re-importing must
  keep the other entries: an "update" command (sources added to or replacing entries of an existing pack)
  is needed then; no step of M9 needs it (steps 6 and 7 build their packs whole). The packer reads only the
  engine's record types and importers; the port's own record types will need it to load the game's
  assembly too.

### The game folder, saves, settings and character files (step 4: `Engine/Statventure/Storage/`)

Namespace `Statventure.Storage`. Tests: `Tests/Statventure.Tests/Storage/`, data in
`Tests/Statventure.Tests/Data/Storage/` (its own `.gitattributes` makes `*.dat` binary).

- **The folder** (`GameFolder.Locate(system, environment, home, roamingAppData)`, pure;
  `ForThisMachine()`): Linux `$XDG_DATA_HOME` when it is absolute (the XDG specification ignores a
  relative one, as .NET's LocalApplicationData does), else `<home>/.local/share`, then `stat-fick`;
  Windows the RoamingAppData known folder, then `Stat Fick`. `GameStorage(folder)` is the folder at
  run time (tests give their own): `SettingsFile`, `Saves` (`saves/`), `ModsFolder` (`mods/`, only its
  path: step 9 mounts it). Folders are made on the first write; on Linux every missing level with mode
  0700 (made level by level: .NET's `Directory.CreateDirectory(path, mode)` gives the mode to the last
  folder only), files 0600.
- **The background writer** (`BackgroundWriter`, internal; one per `GameStorage`, shared by saves and
  settings): the caller prepares the bytes; one writer thread does each file's work in the order the
  files were first asked for; a file asked for again before its work started keeps only the latest
  work (the latest state wins), asked for while it runs the new work follows. The thread is a
  foreground thread that ends when nothing is left, so the process can't end before pending files are
  written; `GameStorage.Flush()` and `Dispose()` wait for them (the game calls one when it quits;
  `Application.cs` doesn't yet: step 9 or the port). A failure is logged (Error, "Storage") and raised
  as `GameStorage.WriteFailed` (file, error) on the writer thread; the next request tries again.
- **The save file** (`SaveFile`; Q38): `Write(value)` and `Wrap(payload)` make the 48-byte header
  (`SVSV`, u32 1, u64 payload length, SHA-256 of the payload) and the payload (the record's binary
  form, `RecordSerializer.ToBinary`); `Check` and `Read<T>` give a `SaveState`: `Missing`, `Readable`
  (with the value, upgraded), `Damaged` (fewer bytes than the header, another magic, version 0, a
  length that isn't what follows, a hash mismatch), or `Unreadable` (intact but not readable here: a
  format version above 1, a record version newer than the engine's or of another record type, or a
  file the system won't let it read), each with its problem in words. Reading never throws for what
  the file holds.
- **Slots** (`SaveSlots`; Q15–Q17, Q39–Q41): names one path-rule name, no `/`, at most 100 characters
  (`NameProblem`, `IsValidName`; every call checks: `ArgumentException`). `List()` gives the slots
  whose save or backup is in `saves/` (other files, such as `.tmp`, `.damaged` and names outside the
  rule, aren't slots) in ordinal order, with which files are there and the last write time.
  `Save(name, value)` serializes at once (a value its declaration refuses throws there) and hands the
  file to the writer, which writes `<name>.dat.tmp` (flushed to disk) and then, by the save it would
  replace: none, a move into place (a backup that is there stays); readable, the backup checked first
  (damaged or unreadable: kept aside as `<name>.dat.bak.damaged…`), then `File.Replace(temporary, save,
  backup)`; damaged or unreadable, `File.Replace(temporary, save, <name>.dat.damaged…)`, which keeps it
  aside in the same call and leaves the backup alone (Q39). Kept-aside names: `.damaged`, else
  `.damaged-2`, `-3` …, the first nothing has. `Load<T>(name)` changes no file: a `SaveSlotReading<T>`
  with the save's reading and, when that isn't readable, the backup's (Q17: nothing loaded; "use the
  backup" is saving `Backup.Value` into the slot, "start new" saving a new record). `List`, `Load` and
  `Exists` wait for the slot's pending save. `Delete(name)` drops a pending save, waits for one being
  written, and deletes the save, the backup and a stale `.tmp`, not files kept aside (Q41). A stale
  `.tmp` left by a crash is never read and is written over by the slot's next save; a save missing
  beside its backup (deleted by hand: .NET's `File.Replace` never leaves the save missing on Linux)
  loads nothing and reports the backup, and the next save leaves that backup as it is.
- **Settings** (`GameStorage.LoadSettings(version, upgrades)`, `Settings`; Q18, Q42, Q43): the file
  is read into a `JsonObject` that keeps every key in its order (a byte order mark allowed; comments,
  trailing commas and repeated keys not); it must be an object with a whole-number `version` from 1 up
  to the game's, else (or when an upgrade throws, or it can't be read) the defaults are used and the file
  is moved over `settings.broken.json`, logged (Warning) and given as `Problem`. The upgrades, one per
  past version (each must be given), change the object in place, without the version key, which the
  engine keeps. `Get<T>(key, default)`: the default when the key is missing, null or doesn't convert
  (logged once per key); `Set<T>(key, value)`: refuses the key `version` and values JSON can't hold
  (NaN) before anything changes, writes only when the value changes (by the writer, into `.tmp` then
  renamed over the file). System.Text.Json with camelCase property names and enums as camelCase names
  (integers refused), strict numbers; the file indented by two spaces, LF, a final line end, `version`
  first, characters outside the BMP escaped as the records' JSON form does.
- **Character files** (`CharacterFile`; Q19): `Export(file, value)` writes the record's JSON form
  (`RecordSerializer.ToJsonUtf8`) to a file beside the target (random name), flushed, then renamed over
  it (a failure removes it, the old file stays); `Import<T>(file)` reads and upgrades. The old game's
  limit holds both ways (`desktop/main.cjs`): more than 10 MiB is refused (`InvalidDataException`).
  `DefaultName` is `stat-fick-character.json`.
- **Documents** (`DocumentsFolder`): `Locate(system, environment, readFile, windowsDocuments)` (pure)
  is Chromium 152's lookup on Linux: `$XDG_CONFIG_HOME/user-dirs.dirs` (or `$HOME/.config/…`), read in
  `fgets`'s 511-byte pieces each cut at a NUL, the last line `XDG_DOCUMENTS_DIR="$HOME/…"` or `"/…"`
  (blanks around `=`, backslash escapes, up to the closing quote or the end), else `$HOME` (`/tmp`
  without HOME), with `FilePath::StripTrailingSeparators`; on Windows the Documents known folder.
  `Ensure(folder)` is Chromium's `create_dir`: a path where something is stays as it is (a file too);
  else it is made with its parents (0700), and `IOException` when it can't be (an empty path, a name
  over 255 characters, no permission). `ForThisMachine()` is both. Tested against
  `Data/Storage/documents-reference.json`: Electron's answer and the folders it made in 46 environments
  (`Tools/DocumentsReference`, `Tools/Scripts/make-documents-reference.sh`, about a minute), with .NET's
  `MyDocuments` beside it for the comparison.
- **The picker's listings** (`FolderListing`): `List(folder)` gives every entry directly in it (name,
  full path, folder or file, size, last write time, hidden: a dot-file on Linux, the attribute on
  Windows) in ordinal order; `Roots()` is `/` on Linux, the ready drives on Windows.
- **Open game folder** (`GameStorage.OpenInFileManager()`): makes the folder, then as Electron's
  `shell.openPath` (the old game's menu item): Linux `xdg-open <folder>` from its parent folder with
  `MM_NOTTTY=1`, not waited for (Electron's Wayland activation token isn't ported); Windows the shell's
  "open" (Explorer). Gives whether it launched; a failure is logged (Warning).
- **Decided as internal design (2026-11-03, accepted with the step's questions):** a slot is there when
  its save or backup is; a save missing beside its backup is reported the Q17 way; states `Unreadable`
  beside `Damaged`; one writer for saves and settings on a foreground thread; loading waits for the
  slot's pending save; failed writes logged, raised and retried by the next request; settings that must
  be a whole-number version from 1, enums by camelCase name, written only on change, an older
  `settings.broken.json` replaced; the old game's 10 MiB limit; folders 0700 and files 0600 on Linux;
  `mods/` not made here.

### Models and state machines in packs (step 5)

Code: `Render3D/ModelData.cs` (the model's record types, `ModelData.Load(files, path)`), `Render3D/ModelImporter.cs`,
`Render3D/Model.cs` (`Model.Load(device, files, path)`), `Animation/StateMachineDefinition.cs` (the state machine's
record types, `StateMachineDefinition.Load(files, path)`), `Packs/VirtualFileSystem.cs` (`Problem`). Tests:
`Tests/Statventure.Tests/Render3D/ModelPackTests.cs`,
`Render3D/ModelValidationTests.cs`, `Tests/Statventure.Tests/Animation/StateMachineRecordTests.cs`; layouts and
fixtures in `Data/Records/` (`model` and `animation.state-machine`, each holding a value of every type it uses).
No pack is committed (Q1).

- **The model record** (Q20) is `ModelData` as `GltfImport` gives it, field by field, all version 1:
  `model` (nodes, meshes, materials, skins, animations (default none), roots); `model.node` (name, parent,
  children, local: the matrix as read, TRS times the file's matrix; translation, rotation, scale, mesh, skin (−1:
  none), ink (default true)); `model.mesh` (name, primitives); `model.primitive` (positions, normals, texCoords,
  joints and weights (null when absent), indices, material (default −1)); `model.joints` (j0–j3, the four
  `uint16` joint indices of a vertex); `model.material` (name, baseColor, doubleSided, emission, unlit, hatch
  (default true)); `model.skin` (name, joints, inverseBindMatrices, skeleton); `model.animation` (name, channels,
  events (default none)); `model.channel` (node, path `translation`/`rotation`/`scale`, interpolation
  `step`/`linear`/`cubicSpline`, times and values as the file stores them, `float32`); `model.event` (name, time in
  seconds). Derived values aren't stored: a material's slot and whether it is emission only, a primitive's
  `FlatVertices` (made by `Model`). Floats go bit for bit, so a model from a pack, or from its JSON form (shortest
  round-trip numbers), is `GltfImport`'s bit for bit.
- **The importer** (`ModelImporter`, `[Importer("models", ".glb")]`; S3-3): each `.glb` of a folder being packed
  becomes one record entry at its path without the ending (`models/figure.glb` → `models/figure`) holding
  `GltfImport.Load` of the file, checked as a loaded model is (`ModelData.Validate`, below), so no pack holds a
  model that won't load. A file `GltfImport` can't read, or whose model breaks a rule, stops the packing with a
  `DataFileException` naming it ("the models importer: …"). `.gltf` isn't taken (Blender's exports are `.glb`; a
  `.gltf`'s buffers would be files of their own).
- **Loading** (S5-1): `ModelData.Load(files, path)` takes `files.Load<ModelData>(path)` (a pack's binary form, or
  a loose `<path>.json` in the development folder; step 3's errors: a file, a record of another type, one that
  doesn't read) and checks it with `ModelData.Validate()`, which throws an `InvalidDataException` naming the part
  at fault: children in range, none its own child or the child of two nodes, each node's parent the node that
  lists it; nodes' meshes and skins, primitives' materials (−1 or in range); roots in range and without a
  parent; a primitive's normals, texture coordinates, joints and weights one per vertex, its indices whole
  triangles within its vertices; skins' joints and skeleton nodes, at least as many inverse bind matrices as
  joints; every joint index (all four) within the skin of each node showing the mesh (skin.comp reads the joint
  matrices by them); channels' nodes, values as many as the keys need (three per key for a cubic spline), at
  least two keys for a cubic spline, key times that increase (NaN doesn't). `GltfImport` itself checks the
  hierarchy, attribute lengths, indices, inverse bind matrices and channels while reading; the ranges of the
  node, mesh, skin, material and root indices and the joint indices it left to BRUTAL's parser (which doesn't
  check a material's: a `.glb` patched to name material 9 of 3 reads, and only the importer's check stops it).
  A failure is a `DataFileException` naming the pack and entry or the loose file ("not a valid model: mesh
  0 (Body), primitive 0 has the index 3 beyond its 3 vertices"), the `InvalidDataException` inside, made by
  `VirtualFileSystem.Problem(path, problem, inner)` (added by this step for every loader that checks what it
  read, steps 6 and 8 too): the topmost mount's pack and entry, or its loose file with no entry, as the file
  system's own errors name them; a path no mount has is a `FileNotFoundException`. `Model.Load(device,
  files, path)` makes the GPU model from the checked data, as `Model.Load(device, path)` does from a `.glb`,
  which stays (the tests and the Blender harness read the `.glb` files).
- **State machines** (Q20: entries of their own): `animation.state-machine` (name, parameters, layers),
  `animation.parameter` (name, kind `bool`/`int`/`float`/`trigger`, default (0)), `animation.layer` (name, mode
  `replace`/`combine`, entry, states, transitions), `animation.state` (name, clip (null: an empty state; must be
  given), speed (1), speedParameter (null), loop (false)), `animation.transition` (from (null: from any state; must
  be given), to, conditions, exitTime (null), duration (0), mayRestart (false)), `animation.condition` (parameter,
  compare `equal`/`notEqual`/`less`/`greater`/`lessOrEqual`/`greaterOrEqual` (`equal`), value (1)): the M7 records'
  constructor parameters and their defaults. They are written in their JSON form (by hand, by the editor in
  M11, or as `RecordSerializer.ToJson` of a machine the builder made) anywhere in the folder packed, and packed by
  step 3's rule for records' JSON forms (`models/machines/humanoid.json` → the record `models/machines/humanoid`);
  nothing ties a machine to a model by path (S5-1). `StateMachineDefinition.Load(files, path)` takes
  `files.Load<StateMachineDefinition>(path)` and runs `Validate()` (M7's rules); a machine that doesn't validate is
  a `DataFileException` naming the pack and entry or the loose file ("not a valid state machine: broken/base: a
  transition shut → nowhere names a state that doesn't exist"), the `ArgumentException` inside.
- **Sizes of a model's JSON form** (a fact for the editor's and the dump's later work: a committed
  `models.pak` shows in `git diff` as this): `figure.glb` (105 KB) is a 123 KB record, 24 KB stored, and its JSON
  form (extract, the development folder, the dump) is 22,286 lines and 700 KB: each vertex's joints an object of
  six lines (1,328 of them), a float list (a channel's times and values) a number per line, a list of vectors on
  one line (its positions 34,655 characters). That is the serializer's JSON form (step 2) applied to meshes; a
  more compact form for the editor or the dump would be a later choice.
- **Checked by hand:** `packer pack` of a folder with two `.glb` files and a machine's JSON form, `list`,
  `verify`, `dump` (the records' JSON forms), `extract` and packing the extraction again (the same bytes).

## Status

- **Nothing implemented yet** (2026-11-02). The decisions and the 27 format details are recorded, the
  design and the nine steps with their dependencies are settled.
- **Step 1, pack container and virtual file system: done (2026-11-03).** `Engine/Statventure/Packs/`,
  `Tools/Packer`, 138 tests in `Tests/Statventure.Tests/Packs/` (the path rule; round trips of empty,
  one, many and large packs; ordinal order; the no-shrink boundary, searched rather than fixed so a
  newer Brotli keeps it; the layout against a pack made field by field; determinism; re-pack reuse;
  folders packed and extracted; every truncation and every flipped bit of the header, the index and
  the entries; badly formed indexes with valid hashes; an index too large to read, as a sparse file;
  the hash on every read; mount order, mods, loose folders, listing, Reload, reads during Reloads, open
  handles). Four questions asked on the way (Q28–Q31). The full suite passes on the GPU and on
  lavapipe (1184 tests). Mutations: 95, 93 fail a test after four gaps were closed (paths whose culture
  order differs from ordinal, the duplicate entry's own error, an index over 2 GiB, readers really
  running during Reloads); the 2 survivors can't differ here: opening with read sharing only (Linux's
  `FileShare` is an advisory lock; it matters on Windows) and writing without the flush to disk (it
  matters only after a crash). Not checked: Windows (see "How it works").
- **Step 2, the record serializer, done (2026-11-03):** `Engine/Statventure/Records/` ("How it works",
  "Records"); six format details asked on the way (Q32–Q37, all as recommended). 45 tests in
  `Tests/Statventure.Tests/Records/`: every field kind in both forms, floats bit for bit (2000 random
  values each, NaN payloads in binary), null and empty, dictionary and pair order, unions, the exact bytes
  and text the format gives, defaults and required keys, truncation at every length, 3000 damaged copies
  (a format error or a value, never another exception), each kind of bad byte, table and JSON, a newer
  version refused, depth limits, the upgrade chains from fixtures of past versions (test.creature 1–3,
  test.limb 1–2, test.move 1–2, test.leap 1–2) in both forms, refused declarations, and the layout
  guard. Binary fixtures are `.rec` (`*.rec binary` in `../.gitattributes`, added by step 1). The full
  suite passes on the GPU and on lavapipe (1229 tests). Mutations: 103, all fail a test after one gap
  was closed (a positional parameter without a default that is nullable, as a state's clip, wasn't
  required: no test type had one); one mutation of the list was malformed (it left a dangling `else`)
  and was replaced by two valid ones, both killed.
- **Step 3, records in packs, the packer's JSON input and dump, `Application.Data`, done (2026-11-03):**
  ("How it works", step 3); five questions asked on the way (S3-1–S3-5, all as recommended), the
  importer attribute put in the engine (this doc's step list had it in the packer). 129 new tests in
  `Tests/Statventure.Tests/Packs/` (267 there in all): record entries pinned byte for byte, `Load` from
  packs and loose folders with every error, the same entries from a pack and its extraction (every field
  kind), the loose rule on 16 starts and 11 bad ones (from files, and from a stream giving one byte per
  read), conflicts in loose folders, the development folder
  over a pack, packing folders with records' JSON forms (old versions upgraded) and importers (endings,
  the longest winning, files read by an importer, failures, 15 bad declarations), extract then pack
  giving the same bytes, the dump's exact text, its problems and its text rule on 14 contents, the
  mounts of `Application.Data`, and the committed packs' check. Found on the way: re-pack reuse would
  have copied an old entry's record flag along with its stored bytes (fixed). Checked by hand: the packer's
  commands and `git diff` through the driver in a scratch repo; the sandbox with a missing development
  folder stops, logged, before its window opens. The full suite passes on the GPU and on lavapipe (1358
  tests). Mutations: 101, all fail a test after three gaps were closed: the escaped-key case had lost its
  escape when the test was written, a folder's files made in reverse path order were listed in path order
  (tmpfs lists the newest first; now a shuffled order), and waiting on a cut byte order mark could only
  show with short reads (the classifier now takes a stream, tested one byte at a time). One more was
  reported as a survivor with no result only because its test output held raw control bytes, which
  `mutate.py`'s `grep` treats as binary; run by hand it fails 12 tests. Not checked: Windows (the
  driver's script).
- **Step 4, the game folder, saves, settings and character files, done (2026-11-03):**
  `Engine/Statventure/Storage/` ("How it works", step 4);
  six format details asked on the way (Q38–Q43, all as recommended). 177 tests in
  `Tests/Statventure.Tests/Storage/`: the folder per system and environment; the Documents folder
  against Electron in 46 environments (the folders it made and their modes too); the save header byte
  for byte, every truncation and every flipped bit; slots listed, written and read back, the backup,
  names; the writer (the latest state wins, waits, a foreground thread, failures retried); damage and
  unreadable saves reported with the backup's state and kept aside on the next write, never
  overwritten; interrupted writes (a stale temporary file, a save missing beside its backup); deleting;
  old-version saves and character files (test.creature 1–3) upgraded; settings defaults, unknown keys,
  upgrades from committed files of versions 1–3, values of every kind, writing only on change, 19 kinds
  of unusable file kept as `settings.broken.json`; export then import giving the same save; the 10 MiB
  limit; listings; the file manager's command; the log lines. Three points of this doc were wrong and
  are corrected above: .NET's `File.Replace` on Linux (a hard link, not two renames), Electron's
  `getPath("documents")` making the folder, and `MyDocuments` being no stand-in. The full suite passes
  on the GPU and on lavapipe (1535 tests). Mutations: 137 (after one line found redundant was
  removed), 135 fail a test after one gap was closed (a backslash just before the end of an unclosed
  value: a 46th reference case, Electron gives `<home>/endx`) and two malformed mutations were replaced
  (each had left an `if` without its statement). The 2 survivors can't differ: the writer without the
  flush to disk (only a crash shows it) and settings' `AllowDuplicateProperties = false` (`JsonObject`
  itself refuses a repeated key when it is first read, also reported as an unusable file; the option
  states the rule at the parse). The removed line made `Settings.Set` serialize a value twice to refuse
  NaN: `JsonSerializer.SerializeToNode` already refuses it. The tests that hold the writer release it
  even when they fail (`HeldWriter`): a held writer's foreground thread kept a failing test from ending,
  which `mutate.py` read as a survivor after its 600 s timeout. Not checked: Windows (`File.Replace`
  there, the RoamingAppData and Documents known folders, Explorer).
- **Step 5, models and state machines in packs, done (2026-11-03):** ("How it works", step 5); one
  question asked on the way (S5-1: checked at load, as recommended), the step's record type names, field
  numbers and machines anywhere by path accepted with it. 65 new tests: the 12 test models packed by the
  models importer equal `GltfImport`'s bit for bit from the pack and from their extracted JSON form, each
  `.glb` one record at its path without the ending, the importer taking `.glb` only, a `.glb` it can't read
  or whose model breaks a rule naming the file; four Blender scenes (gear-swing, shadow-stage, glow-figure,
  model-blocks: nine of the twelve models) drawn from pack-loaded models match their goldens as the
  file-loaded ones do; each of the model's rules broken once (39 cases, indices at their limits) with its
  own problem, named as the pack and entry or the loose file, also through `Model.Load`; the M7 state
  machines through both forms, one written by hand in JSON, machines packed beside a model and loaded from
  the pack and a loose folder, the figure and the humanoid machine from a pack playing as the `.glb` and the
  built machine do (notices, events and every frame's pose), a machine that doesn't validate;
  `VirtualFileSystem.Problem` on each kind of mount; layouts and fixtures of the 16 new record types. The
  full suite passes on the GPU and on lavapipe (1600 tests). Mutations: 96 (the records' fields, defaults,
  names, numbers and enum orders; the importer; the loads; `Problem`; each check and each limit), all fail a
  test. Nothing in this doc turned out wrong. Not checked: Windows.

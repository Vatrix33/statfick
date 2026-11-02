# Assets and data (M9)

The game's data on disk: binary packs (`.pak`) of versioned records and files, read through a
virtual file system that lets mods and a development folder override them; the serializer that
gives every record a binary and a JSON form; saves, settings and character files in a per-user game
folder; and the importers that fill the packs from the old game, Blender's exports and the
stylesheets.

## Decisions (user, 2026-11-02)

Asked before the milestone started, every answer the recommended option; in full in
`Docs/Engine-Plan.md`, "Decisions for M9".

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

## Design (proposed)

A proposal for the steps to work from. What the decisions don't settle is marked "(open: Qn)" and
listed under "Open questions"; the rest is the proposal those questions would confirm or change.

### Packs

A pack is a container of entries, each a path and its bytes; it knows nothing of records.

- **Header** (64 bytes at the start, little-endian): the magic `SVPK`, the format version (1),
  flags, the entry count, the index's offset and length, and the SHA-256 of the index's bytes.
- **Entries:** one after another after the header, in path order, each raw or Brotli-compressed
  (`BrotliEncoder` at a fixed quality and window, so the output is deterministic); an entry Brotli
  doesn't shrink is stored raw (decision).
- **Index** (after the entries, written last): per entry its path (UTF-8, length-prefixed), offset,
  stored length, content length, flags (compressed; a record entry, step 3) and the SHA-256 of its
  content (open: Q5). Paths are unique, sorted ordinally, each under its area's folder
  (`audio/themes/alder-vale`, `ui/fonts/lato-regular.ttf`), and follow the path rule (open: Q4).
- **Writing:** into a temporary file renamed over the old pack. Re-packing keeps an unchanged entry's
  stored bytes (same content hash), so a newer .NET's Brotli can't make every committed pack differ.
- **Reading:** the header and index when mounted (the index hash checked); entries on demand by
  positional reads (`RandomAccess`, safe from several threads), decompressed into a buffer of the
  known length. When the hashes are checked and what a mismatch does: open: Q6. Packs are opened
  sharing read, write and delete, so one can be rebuilt while the game runs (Windows would lock it
  otherwise) and picked up on Reload.
- **Tool:** `Tools/Packer` packs a folder into a `.pak`, lists, verifies and extracts one; step 3 adds
  record input and the JSON dump, steps 5–8 their importers.

### Records (the serializer)

Every data record (pack entries, saves, character files) has a binary form and a JSON form, both
versioned.

- **Declaring a record:** a stable type name and version (e.g. `audio.theme`, 1) and the order of its
  fields; how a type states its layout and keeps its past ones: open: Q10.
- **Binary form:** fields by position, no names. Lengths and counts as variable-length integers;
  numbers fixed-width little-endian, floats bit for bit; strings UTF-8; a presence byte before a
  value that may be null (null stays distinct from empty); arrays with their count, arrays of numbers
  and vectors as one block; dictionaries as their pairs in enumeration order (insertion order kept:
  the composer's data depends on it); enums as their numbers (a renamed or reordered member is a
  layout change); unions: open: Q12.
- **Versions:** an entry or file starts with a table of the record types it uses and their versions,
  so each type versions on its own and nested values carry no per-value cost. An older version is read
  by its own layout and upgraded step by step to the current one; a version newer than the engine
  knows is an error naming it.
- **JSON form:** the same fields by name, the type and version table at the top (open: Q11 for
  names, numbers, absent and unknown keys).
- **Guarding layouts:** committed fixtures of every version of every record type in both forms
  (`Tests/Statventure.Tests/Data/Records/`), each read, upgraded and compared with its expected value,
  the current version's also written back to the same bytes; a committed list of layout signatures
  (open: Q13) fails a test when a layout changes without a new version.

### The virtual file system

- **Mounts, bottom to top:** the game's packs (models, audio, ui, world, data; their paths don't
  overlap), then the packs in the mods folder (open: Q8), then the development folder if one is given
  (open: Q7). A path resolves to the topmost mount that has it; listing a folder gives the union of
  the mounts' paths, each from its topmost mount.
- **Reading:** bytes, a stream, and (step 3) a record of a given type (`Load<T>(path)`), from the
  binary form in a pack or the JSON form in a loose folder.
- **Reload:** remounts everything (packs re-read their index, folders are rescanned) and tells its
  listeners; what each system reloads (themes, models, stylesheets) is up to that system.
- **Who owns it:** open: Q3 (proposed: one instance per application, `Application.Data`, passed to
  what loads; tests and tools mount their own).
- **Where the files are at run time:** open: Q2.

### The game folder: saves, settings, character files

- **The folder:** Linux `$XDG_DATA_HOME/stat-fick/`, else `~/.local/share/stat-fick/`; Windows
  `%APPDATA%\Stat Fick\` (`Environment.SpecialFolder.ApplicationData`); made on first use. Resolved by
  a pure function of the system, the environment and the home folder, so the tests check every case
  without touching the real folder.
- **Saves:** a header (magic, file format version, payload length, the payload's SHA-256), then the
  payload, a record (its type and version table, then the fields) through the serializer. Written to
  a temporary file in the same folder, flushed to disk, then put in place with the previous save kept
  as the backup (`File.Replace`: one call on Windows; on Linux two renames, so a crash between them
  leaves the backup, which the loader then reads). Which record the save is belongs to the port; M9
  tests with test records. How many saves, when they're written, what a damaged one does: open:
  Q15–Q17.
- **Settings:** `settings.json`, `{ "version": n, … }`, read with System.Text.Json into a JSON object
  that keeps every key; typed get and set with defaults; upgrades per version; written atomically when
  changed (the old game wrote its preferences on every change). A file that doesn't parse: open: Q18.
- **Character files:** Save Character writes the save record's JSON form (indented, with its type and
  version table), `stat-fick-character.json` in Documents by default as in the old game; Load
  Character reads one, upgrades an older version and makes it the current save (the current one
  becoming the backup). Documents: on Linux the XDG documents folder, as Electron's
  `app.getPath("documents")` gives it (to be captured in the harness; what .NET's
  `SpecialFolder.MyDocuments` returns on Linux is to be compared with it), on Windows the Documents
  known folder. The picker: open: Q19.
- **Open game folder:** opens the game folder in the system's file manager (`xdg-open`, Explorer); it
  isn't a dialog.

### Importers

Each area's importer is a class in the engine beside the area's code (so the editor, M11, can reuse
it), found by the packer through an attribute (so the steps add importers without editing a shared
file).

- **Models (`models.pak`):** a `.glb` through `GltfImport` into its `ModelData` record (open: Q20);
  state machines from their JSON form. Pack-loaded models equal `GltfImport`'s bit for bit and render
  the same.
- **Music (`audio.pak`):** `make-music-data.sh` runs `music.cjs` in Chromium as now, which writes the
  records' JSON form instead of C# (keeping its key map and its stop on an unknown key); the packer
  reads them through the serializer's JSON form into `audio.pak` (open: Q21, Q22). Before
  `MusicData.g.cs` is deleted, every pack-loaded record is compared with it once. Effect patches from
  their JSON form (the game's own come at port time).
- **UI (`ui.pak`):** stylesheets compiled (open: Q23), checked at import (no unknown property, no
  dropped rule, every `url()` found); fonts (open: Q24); images (open: Q25).
- **Terrain (`world.pak`):** the description and its two PNGs into a `TerrainData` record (open: Q26).
  Map data comes with the port.
- **The old game's tables (`data.pak`):** at port time (decision "Scope"); classes: open: Q27.

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
     extension as binary; only this step touches it).
   - Tests: round trips (empty, one, many and large entries; raw and compressed), the no-shrink rule,
     determinism, re-pack reuse, each kind of damage, mount order (later wins, union listing), the path
     rule, Reload after a pack is rebuilt.
   - Asks first: Q2, Q4–Q9.
2. **Record serializer: binary and JSON forms, versions, old-version fixtures.** Independent (of 1
   too).
   - Creates `Engine/Statventure/Records/` (declarations, binary reader and writer, JSON reader and
     writer, versions and upgrades, layout signatures), `Tests/Statventure.Tests/Records/`,
     `Tests/Statventure.Tests/Data/Records/` (fixtures, the layouts list).
   - Tests: every field kind in both forms; floats bit for bit; null and empty; dictionary order;
     unions; damaged and truncated input; a newer version refused; upgrade chains from fixtures of
     test types with several versions; a layout changed without a new version caught.
   - Asks first: Q10–Q13.
3. **Records in packs, the packer's JSON input and dump, `Application.Data`.** After 1 and 2.
   - Changes `Engine/Statventure/Packs/` (record entries, `Load<T>`), `Tools/Packer/` (JSON-form input,
     `dump`, the importer attribute), `Engine/Statventure/Core/Application.cs` (`Data`: the game's packs
     and the development folder), `Tests/Statventure.Tests/Infrastructure/TestPaths.cs` (`Packs`: the
     repo's `engine/Assets/Packs/`), `Tests/Statventure.Tests/Packs/`.
   - Tests: a record from a pack and its JSON form in a loose folder load equal; the dump.
   - Asks first: Q1, Q3, Q14 (Q7 already asked for step 1).
4. **Game folder, saves, settings, character files.** After 2 only; runs beside 3 and 5–8.
   - Creates `Engine/Statventure/Storage/` (the game folder, the save store, settings, character
     files, opening the game folder), `Tests/Statventure.Tests/Storage/`,
     `Tests/Statventure.Tests/Data/Storage/` (old-version saves, settings and character files).
   - Tests: the folder per system and environment; save round trips; each damage → the backup; an
     interrupted write (a stale temporary file, a missing save with its backup); old-version saves
     and character files upgraded; settings keep unknown keys, default missing ones, upgrade old
     versions; export then import gives the same save; the Documents folder against Electron's.
   - Asks first: Q15–Q19.
5. **Models into `models.pak`.** After 3; beside 4, 6, 7, 8.
   - Changes `Engine/Statventure/Render3D/ModelData.cs` (record declarations, loading from the file
     system), `Render3D/Model.cs` (`Load` from the file system), `Animation/StateMachineDefinition.cs`
     (record declarations); creates the model importer in `Render3D/`; tests in
     `Tests/Statventure.Tests/Render3D/` (each test model loaded from a pack equals `GltfImport`'s; a
     Blender scene rendered from pack-loaded models matches) and `Tests/Statventure.Tests/Animation/`
     (the M7 machines through both forms).
   - Commits no pack while there is no game model (open: Q1).
   - Asks first: Q20.
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
   - Asks first: Q21, Q22.
7. **Stylesheets, fonts and images into `ui.pak`.** After 3; beside 4, 5, 6, 8.
   - Changes `Engine/Statventure/UI/Css/` and `UI/Style/Stylesheet.cs` (declarations of the compiled
     form, a sheet built from it), `Text/FontLibrary.cs` and `UI/Dom/UiDocument.cs` (fonts from
     `@font-face`, if Q24 says so), `UI/UiHost.cs` (images and SVG text from the file system); creates
     the UI importer in `UI/`; tests in `Tests/Statventure.Tests/Ui/` (the old stylesheet compiled
     equals it parsed; the 22 game screens lay out and paint as before through it; the fonts as
     `TestFonts` registers them). Commits `engine/Assets/Packs/ui.pak` (open: Q1).
   - Asks first: Q23–Q25.
8. **Terrain into `world.pak`.** After 3; beside 4–7.
   - Changes `Engine/Statventure/Render3D/Terrain.cs` (declarations, loading from the file system);
     creates the terrain importer; tests in `Tests/Statventure.Tests/Render3D/` (the test terrain
     loaded from a pack equals the file-loaded one bit for bit; a terrain scene renders the same).
   - Commits no pack while there is no game terrain (open: Q1).
   - Asks first: Q26.
9. **The game's data at run time.** Last: after 3, 4 and 6 (it changes `Application.cs` after them).
   - Changes `Tools/Scripts/build.sh` and `build.ps1` (copy `engine/Assets/Packs/*.pak` into the game
     folder, open: Q2), `Engine/Statventure/Core/Application.cs` (the mods folder from step 4's game
     folder; Reload), `Docs/Engine-Plan.md`'s Folders table (`Assets/Packs/`, where packs and mods are
     at run time).
   - Tests: mount order with mods and a development folder over the game's packs; Reload.
   - Asks first: Q27.

## Open questions

Data-format details, layout and scope the decisions leave open, each with options (the recommended
one first, with its reason). Ask each before the step named; the answers go into
`Docs/Engine-Plan.md` ("Decisions for M9") and here.

**Before steps 1 and 3 (they shape several steps):**
- **Q1. What M9 commits as packs.** Most of the game's content doesn't exist yet (no game model, state
  machine, effect patch, terrain or port stylesheet; only test assets). (a) Only what exists:
  `audio.pak` (55 themes, 34 instruments, 21 percussion voices, 7 rooms) and `ui.pak` (the old game's
  five font faces); `models.pak`, `world.pak` and `data.pak` appear when the port makes their content,
  the port's stylesheet joins `ui.pak` when it's written; every importer is tested on the test assets
  (test models, test terrain, M7's machines, M8's patches, the old stylesheet), packed inside the test
  run. Recommended: test data never ships, and no empty pack has to be kept in step. (b) As (a), plus
  the old game's `styles.css` compiled into `ui.pak` as the port's starting sheet. (c) Packs of the test
  assets as stand-ins. (d) All five packs, empty where there's nothing yet.
- **Q2. Where the game's files are at run time** (M12 decides the shipped layout; M9's code needs a
  place now). (a) The packs in `packs/` beside the executable (`$STATVENTURE_GAME/packs/` from the
  build), the classes in `classes/` beside it as in the old game, mods in `mods/` in the per-user game
  folder. Recommended: an installed game's folder may be read-only, the player's own is writable and
  is what "Open game folder" shows. (b) Mods in `mods/` beside the executable. (c) The packs straight
  beside the executable.
- **Q3. How the engine reaches its data.** (a) One `VirtualFileSystem` per application
  (`Application.Data`), passed to what loads (the audio system gets its music from it); tests and
  tools mount their own. Recommended: the music data is static today, which would keep tests from
  mounting different packs side by side. (b) One static, process-wide file system.
- **Q4. Path rule for entries and mounts.** (a) Lowercase ASCII letters, digits, `-`, `_`, `.` and `/`
  only, checked when packing and mounting. Recommended: Linux and Windows then agree on every lookup in
  a loose folder (Windows ignores case, Linux doesn't). (b) Any characters and case, matched exactly on
  both systems, two paths differing only in case refused.

**Before step 1:**
- **Q5. What an entry's SHA-256 covers.** (a) Its content, uncompressed. Recommended: the same hash
  whether the entry is stored raw, compressed or as a loose file, which the editor's change detection
  and the packer's reuse of unchanged entries need; damaged compressed bytes still fail (Brotli
  errors, or the hash after decompressing). (b) The stored bytes (checked before decompressing, but
  changing with the compression). (c) Both.
- **Q6. When hashes are checked, and what a damaged pack does.** (a) The index hash when a pack is
  mounted, an entry's hash whenever it's read; a mismatch is an error naming the pack and entry, and
  the game stops with that message. Recommended: the packs are a few MB and SHA-256 costs about a
  millisecond per MB, so no data is used unchecked. (b) Only by a verify command (and the packer).
  (c) As (a), but a damaged entry falls back to the same path in a lower mount.
- **Q7. The development folder: what it holds and when it's mounted.** (a) Loose files in the entries'
  own forms (a file as it is, a record as its JSON form at `<path>.json`), mounted only when the game
  is started with a development-folder option (command line or environment variable), re-read on
  Reload. Recommended: one form per entry, no importer at run time, and a player's game never looks
  for it. (b) Also importer sources (`.glb`, `.css`, terrain PNGs), imported when mounted. (c) Always
  mounted from a fixed folder (e.g. `dev/` in the game folder) when it exists.
- **Q8. Mods: order and switches.** (a) Every `.pak` in the mods folder, mounted in file-name order
  (ordinal), so a later name wins; a mod adds or replaces entries but can't delete one; no in-game mod
  list in M9. Recommended: the simplest rule, and a player orders mods by naming them. (b) A
  `mods.json` in the game folder giving the order and which are on. (c) As (a), plus entries that
  delete a path.
- **Q9. The proposed byte layout** (Design, "Packs"): a 64-byte header at the start (`SVPK`, format
  version, flags, entry count, index offset and length, index SHA-256), entries in path order, the
  index at the end (path, offset, stored length, length, flags, SHA-256), Brotli at quality 11 with a
  4 MB window (22 bits), entries under their area's folder, the same input always giving the same
  bytes. (a) As proposed. Recommended. (b) With the changes the user names.

**Before step 2:**
- **Q10. How a record type states its layout.** (a) Explicit field numbers on its properties
  (`[Field(1)]`, `[Field(2)]` …), read by reflection once per type into compiled readers and writers; a
  past version is a frozen copy of the type (e.g. `ThemeSpec.V1`) read the same way, with a
  hand-written upgrade to the next version. Recommended: little code per type for M9's ~45 types and
  the port's tables, the order explicit, and Q13's list catches a layout changed without a new
  version. (b) Hand-written read, write and JSON methods per type and version. (c) A Roslyn source
  generator emitting (b) from (a)'s attributes (no reflection at run time, which would matter if M12
  chose NativeAOT).
- **Q11. The JSON form's details.** (a) Property names in camelCase (`tempo`, `doublings`), the type
  and version table as `"$types"` at the top, an absent key meaning the record's default, an unknown
  key an error (as `music.cjs` does), doubles in their shortest round-trip form, NaN and the
  infinities as the strings `"NaN"`, `"Infinity"`, `"-Infinity"`, byte arrays as base64. Recommended:
  readable character files, and nothing dropped silently. (b) As (a) with the C# names (PascalCase).
  (c) As (a) with unknown keys ignored.
- **Q12. Unions** (a field holding one of several record types: the CSS rule kinds; the class levels'
  typed effects, `"type": "spell-power-percent"`). (a) Supported: a base type lists its subtypes with
  tags, written as an index in binary and as `"$type"` in JSON. Recommended: the old game's tables are
  full of tagged objects, and step 7's stylesheet rules need it. (b) Not in M9 (each written as one
  record with optional fields).
- **Q13. Guarding layouts.** (a) A committed list of every record type's layout signature per version
  (field numbers, names, types, enum members); a test fails when a type's current layout isn't its
  version's recorded one, or a version has no fixture. Recommended: it catches a change before any
  old file exists to break. (b) The fixtures only.

**Before step 3:**
- **Q14. Reviewing packs in git.** (a) `Tools/Packer dump` writes a pack as JSON (per entry: path,
  type, version, hash and the JSON form; files as size and hash; number arrays longer than 64 as a
  count and hash unless `--full`), and a git text-conversion driver shows those dumps in `git diff`
  (`.gitattributes` names it; each clone enables it once, which the setup scripts do). Recommended:
  diffs of the committed packs read like the data. (b) The dump only, run by hand.

**Before step 4:**
- **Q15. How many saves.** (a) One, the current game: `save.dat` beside `settings.json`, the previous
  one kept as `save.dat.bak`, not compressed (small; the hash covers it). Recommended: the old game
  keeps one character. (b) Named slots. (c) One, plus several rolling automatic backups.
- **Q16. When the save is written.** (a) Whenever the game asks, as the old game's `saveCharacter`
  after every change: the record is serialized at once on the caller's thread and written by a
  background writer (only the newest pending save), flushed on quit. Recommended: no frame waits for
  the disk, and quitting never loses the last change. (b) Written synchronously on every call. (c) At
  most every few seconds.
- **Q17. A damaged or missing save at start.** (a) Load the backup, keep the damaged file aside
  (`save.damaged.dat`) and tell the player in the game. Recommended: nothing is lost silently. (b) Load
  the backup silently. (c) Load nothing and ask the player.
- **Q18. A `settings.json` that doesn't parse.** (a) Start with the defaults, keep the broken file as
  `settings.broken.json` and log it. Recommended: the game always starts. (b) Refuse to start, with a
  message.
- **Q19. The in-game file picker for Save and Load Character.** (a) M9 gives the functions (export to
  and import from a path, the Documents folder, a folder's listing); the picker screen is game UI made
  at port time with the other screens. Recommended: screens are built in C# and styled by the game's
  stylesheet, both at port time. (b) M9 builds the picker as an engine element with a plain default
  style that the game's stylesheet restyles, as M5 did with the select list and tooltips.

**Before step 5:**
- **Q20. Model entries.** (a) One entry per `.glb`, holding its `ModelData` as `GltfImport` reads it
  (nodes, meshes, materials, skins, clips with events); a rig family's clip set is a `.glb` with the
  armature and its actions; state machines are entries of their own. Recommended: the import is a
  faithful copy, checked by equality with `GltfImport`. (b) Meshes and clips split (one entry per
  clip), so one clip can be replaced alone.

**Before step 6:**
- **Q21. Music entries.** (a) One entry per theme, instrument, percussion voice, room and effect patch,
  plus one listing the themes in the old game's order (`Soundtrack.Themes`). Recommended: a mod or the
  editor can replace one theme. (b) One record for the whole library.
- **Q22. The music records' JSON keys.** (a) The records' own names (`type`, `instrument`,
  `doublings`), `music.cjs` mapping the old keys as it does for the C# today. Recommended: one JSON
  form for every record. (b) The old game's keys (`t`, `i`, `dbl`).

**Before step 7:**
- **Q23. What a compiled stylesheet is.** (a) The parsed and prepared rules (selectors, media queries,
  keyframes, font faces, and each declaration with shorthands expanded and invalid values dropped, as
  tokens), checked at import (no unknown property, no dropped rule, every `url()` in the packs).
  Recommended: no parsing at load, and the parser's own result is what the tests compare. (b) The
  checked CSS text, parsed at load. (c) Values resolved to types at import (lengths, colours), which
  `var()` and `calc()` mostly prevent.
- **Q24. How fonts are registered.** (a) From the stylesheet's `@font-face` rules (family, style,
  weight range, `src: url()` through the file system), as Chromium does: the engine starts honouring
  `@font-face`. Recommended: the old sheet already declares its five faces with the weights
  `TestFonts.cs` hand-codes. (b) A font list record in `ui.pak`, registered at start.
- **Q25. How images are stored.** (a) As their PNG (or SVG) files, decoded at load
  (`ImageFile.Load(bytes)`). Recommended: PNG is already compressed (stored raw by the no-shrink rule).
  (b) Decoded RGBA, Brotli-compressed.

**Before step 8:**
- **Q26. Terrain heights in the pack.** (a) The 16-bit samples with the height range, turned into
  floats at load exactly as today. Recommended: half the size, the same floats. (b) The float heights.

**Before step 9:**
- **Q27. Classes and the milestone list's "JSON class loader".** (a) The class record and loader come at
  port time with the other tables (the record is the port's); M9 only lets the file system mount the
  shipped `classes/` folder, so a mod or the development folder can override a class. Recommended: the
  same rule as for the tables (decision "Scope"). (b) M9 defines a class record and loader now from the
  old game's 129 class files.

## How it works

Filled in as the steps are done.

## Status

- **Nothing implemented yet** (2026-11-02). The decisions are recorded, the design is proposed, nine
  steps are proposed with their dependencies, and the open questions above are to be asked before the
  steps that need them.

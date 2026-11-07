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
  stylesheet in `old-game/styles.css` (a copy in `Tests/Statventure.Tests/Data/Game/`, read by
  `GameLayoutTests`: the old game's stylesheet as it was when the game screens were captured, `f45ca7a`,
  2026-10-30; the old game's own has differed since `7cdd47e`, 2026-11-03). Nothing writes to `$STATVENTURE_GAME` yet: `build.sh` and `build.ps1`
  build into each project's `bin/`, and there is no game executable before the port (M12 does the
  shipped layout). (Since step 9 the build publishes the sandbox there with `packs/` beside it: S9-A.)
  (Corrected 2026-11-03, step 7: the fonts' names there have capitals, `Lato-Regular.ttf`, which the path
  rule refuses, so they can't be packed from that folder as they are: `make-ui-pack.sh` copies them under
  names that follow it, `ui/fonts/lato-regular.ttf`, S7-5.)
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
  height PNG read at 16 bits and a cover PNG read at 8 bits beside it, each as one grey channel
  (BRUTAL's Stb, by path; Stb also has `LoadFromMemory` and `Load16FromMemory`), and turns the samples
  into float heights (`low + s / 65535f · (high − low)`). (Corrected 2026-11-03, step 8: this said a
  "16-bit height PNG", but Stb's `Load16` takes 8-bit PNGs too, widening each value v to v · 257, and
  colour PNGs, turned to grey; the loader refused neither.) Only the 64×48 test terrain exists
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
  (`audio/themes/alder-vale`, `audio/themes/combat.alder-vale` for the theme `combat@alder-vale` by
  step 6's name rule (S6-1), `ui/fonts/lato-regular.ttf`), and use only lowercase ASCII letters,
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
- **A mod's info** (M11 step 7; `Docs/Editor.md`, G1, M11-15): a mod may carry its name, author, version and
  description as the record entry `mod/info`, of the engine's kind `mod.info` (`Packs/ModInfo.cs`; the editor
  writes it into every mod it makes). The game never reads it: a mod mounts and loads the same with it or
  without it, and an entry there that can't be read is an error only when something reads it, naming the mod
  (G2 as today). The game's own packs leave `mod/` to mods; the pack's dump and `extract` show the entry as any
  record. A mod list in the game can read each mod file's own entry later.
- **The development folder** (Q7): loose files in the entries' own forms (a file as it is, a record as
  its JSON form at `<path>.json`), mounted only when the game is started with a development-folder
  option (command line or environment variable), re-read on Reload; no importer runs at run time.
- **Reading:** bytes, a stream, and (step 3) a record of a given type (`Load<T>(path)`), from the
  binary form in a pack or the JSON form in a loose folder.
- **Reload:** remounts everything (packs re-read their index, folders are rescanned) and tells its
  listeners; what each system reloads (themes, models, stylesheets) is up to that system. The game calls
  `Application.ReloadData()` (step 9, S9-C), which also reloads the music (S9-D). Known limit: the mounts are
  the ones found at the start (step 3's `DataMounts`), so an area pack first put in `packs/`, or a `classes/`
  folder made, after the start needs a restart, and a pack taken out of `packs/` makes Reload fail (the data
  stays as it was); the mods folder and the development folder are rescanned, so mods added or taken away
  do show.
- **Who owns it** (Q3): one instance per application (`Application.Data`), passed to what loads (the
  audio system gets its music library from it); tests and tools mount their own.
- **Where the files are at run time** (Q2): the packs in `packs/` beside the executable
  (`$STATVENTURE_GAME/packs/` from the build, beside the sandbox until the port: S9-A), the classes in
  `classes/` beside it as in the old game, mods in `mods/` in the per-user game folder.

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
  stop on an unknown key) into a scratch folder in the build cache (S6-5); the packer reads them
  through the serializer's JSON form. One entry per theme, instrument, percussion voice, room and
  effect patch (`audio/themes/<name>`, `audio/instruments/…`, `audio/percussion/…`, `audio/rooms/…`,
  `audio/effects/…`, each name stored by a reversible rule, S6-1), plus one listing the themes in the
  old game's order, `audio/theme-order` (Q21; not `audio/themes`, which is a folder). Before
  `MusicData.g.cs` is deleted, every pack-loaded record is compared with it once. The game's own effect
  patches come at port time.
- **UI (`ui.pak`):** a compiled stylesheet is the parsed and prepared rules (selectors, media queries,
  keyframes, font faces, and each declaration with shorthands expanded and invalid values dropped, as
  tokens), checked at import (no unknown property, no dropped rule, every `url()` in the packs) and
  tested by comparing computed styles with the text-parsed sheet (Q23). Fonts are registered from the
  stylesheet's `@font-face` rules (family, style, weight range, `src: url()` through the file system),
  as Chromium does: the engine starts honouring `@font-face` (Q24). Images are stored as their PNG or
  SVG bytes (raw, by the no-shrink rule) and decoded at load (Q25). The port's stylesheet joins
  `ui.pak` when it's written. (Corrected 2026-11-03, step 7: "no dropped rule" would refuse the old
  game's own `styles.css`, whose two rules for Firefox, `progress::-moz-progress-bar`, Chromium drops
  too: a rule naming another browser's prefixed pseudo-class or pseudo-element is dropped and logged
  instead, S7-2; "every `url()` in the packs" is checked against the folder being packed, the only data
  the importer knows, S7-1; and an SVG's text is stored compressed when Brotli shrinks it, as any entry.)
- **Terrain (`world.pak`):** the description (`<name>.terrain.json`, S8-1) and its two PNGs into a
  `TerrainData` record (`terrain`, S8-2) holding the 16-bit samples and the height range, turned into
  floats at load exactly as today (Q26). Map data comes with the port.
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
   - Reload of what was read: step 6's `MusicLibrary` is read once, when the audio system starts, so a
     theme changed in the development folder is heard only after a restart; reloading it on
     `Data.Reloaded` (a new library for the audio system) is this step's to decide (decided: S9-D).
   - Format details: Q27; asked during the step: S9-A–S9-D.
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
  `<name>.terrain.json`: step 8's to settle; settled as that, S8-1). The packer looks for importers in the engine's assembly.
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
Asked during step 6 (user, 2026-11-03; every answer the recommended option):

- **S6-1. Names outside the path rule** (18 theme ids hold `@`, `combat@alder-vale`; two instruments
  have capitals, `lowSpiccato`, `choirOo`): one fixed, reversible rule makes an entry's name from what
  the music calls it: a capital letter becomes `_` and the small letter (`audio/instruments/low_spiccato`),
  `@` becomes `.` (`audio/themes/combat.alder-vale`); letters a–z, digits and `-` stay. The library maps
  paths back to names; the records stay as they are. A theme whose own `id` isn't the one its path holds
  is an error naming the entry; `make-music-data.sh` stops on a name the rule can't store. (Not a name
  field in every record with free paths; not renaming the ids the game plays and the old data's names.)
- **S6-2. The theme order and the theme entries:** `audio/theme-order` sets the order; theme entries it
  doesn't name follow it in path order (so a mod can add a theme with its entry alone, and two mods
  adding themes both work); an id it names without an entry, or names twice, is an error. (Not the
  order as the exact set of themes, not unlisted themes ignored.)
- **S6-3. No music in the data** (the sandbox and the engine's tests have no `packs/`): the library is
  empty, `Play` refuses every id as it refuses an unknown one, the buses and effects work. When some
  music entries are there, the pieces missing are errors when the library loads (the start stops, as
  for a damaged pack). (Not music required, the start stopping without it.)
- **S6-4. Effect patches in the data** (`audio/effects/<name>`): the audio system registers every one
  under its name when it starts, so the game plays them by name; `Effects.Register` still adds or
  replaces patches made in code. (Not the game loading and registering each patch itself.)
- **S6-5. The importer's JSON sources** go to a scratch folder in the build cache
  (`~/.cache/stat-fick-build/music-data/`), emptied by `make-music-data.sh` and kept for inspection,
  not into the repo: the pack is the source of truth, and `git diff` shows its dump. (Not
  `engine/Assets/Sources/audio/`, ignored or committed.)

Asked during step 8 (user, 2026-11-03; both the recommended option; numbered S8-n as steps 3 and 5 did):

- **S8-1. A terrain description's name ends in `.terrain.json`** (`world/terrain/vale.terrain.json`), the
  terrain importer's ending (S3-3): it stays a JSON file for editors and tools, and the longest-ending
  rule keeps it apart from records' JSON forms. In the development folder, where no importer runs (Q7),
  such a file is only a file. The committed test terrain keeps its name (`Data/Terrain/test.json`, read
  by the Blender harness); the tests copy it under the importer's ending. (Not `<name>.terrain`.)
- **S8-2. The terrain record** is `TerrainData` itself, the record `terrain` version 1: `width`,
  `depth` (int32), `cellSize`, `heightMin`, `heightMax` (float32), `heightSamples` (a list of uint16,
  the height image's samples row by row from Z = 0), `covers` (bytes, each point's index into the
  slots; base64 in JSON), `coverSlots` (strings), `waterLevel` (float32 or null: no water; the
  default), all but `waterLevel` required. The C# names stay, so the record's `covers` are the points'
  indices while a description's `covers` key lists the slot names; the float heights aren't stored.
  (Not `world.terrain`; not names following the description's keys.) Accepted with it: the check at
  load is what the description's loader checks (a sample and a cover for each of the grid's points),
  and a description's unknown keys are ignored as before.

Asked during step 9 (user, 2026-11-03; every answer the recommended option; numbered S9-n as steps 3, 5, 6 and 8
did):

- **S9-A. The game folder the build makes:** `build.sh` and `build.ps1` also publish the sandbox, the game's
  stand-in until the port, into `$STATVENTURE_GAME` (framework-dependent, `dotnet publish --no-build` of the
  build just made), with `packs/` beside it holding exactly the committed packs (a pack no longer in
  `Assets/Packs/` is removed). The port's game executable takes its place; M12 settles the rest of the shipped
  layout. (Not `packs/` alone with no executable to read it; not also copying the packs into the sandbox's
  `bin/`.)
- **S9-B. How the game reaches its game folder:** `Application.Storage`, a `GameStorage` made at the start of
  `Run()` for `ApplicationOptions.GameFolder`, else this machine's folder, before `Data` (which mounts its
  `mods/`); on quit it is flushed and disposed after the game's `Unload()` (where the window's document
  unloads), the window already closed, and the same after a failed start or loop. (Not without the option; not
  the game making its own and handing it in.)
- **S9-C. Reload:** `Application.ReloadData()`, which the game calls (the port binds it); the engine binds no
  key and watches no file. It remounts everything, reloads the music from `Data`, then runs
  `Data.Reloaded`'s other listeners. A Reload that fails is logged and gives false: when the data can't be
  mounted it stays as it was; when only the music doesn't load, the data is the new one and the sound keeps
  the music it had. (Not stopping the game as a failed start does; not a key of the engine's; not a file
  watcher.)
- **S9-D. The music on Reload:** the theme playing restarts from its start with the new data, through the
  usual switch (0.55 s fade out, 0.8 s fade in), only when its music changed (its theme, or any instrument,
  percussion voice or room, compared by binary form); otherwise it plays on untouched. Later `Play` calls use
  the new library. The data's effect patches that are new or changed are registered (added or replaced); one
  gone from the data stays playable until the game quits, and patches registered in code stay. (Not the new
  library only from the next `Play` of another theme; not a restart after every Reload; not leaving the sound
  out of Reload.)

Asked during step 7 (user, 2026-11-03; every answer the recommended option; numbered S7-n):

- **S7-1. A url names data by its full path:** every url, in stylesheets and in the UI's markup (`img
  src`, `cursor`, `background`), is a data path from the root under the path rule
  (`url("ui/fonts/lato-regular.ttf")`, `<img src="ui/images/x.png">`): one form for sheets and markup;
  the importer stores the urls as written; `data:` urls stay as they are; a relative path (`../x`), an
  `http:` url, a path outside the rule or one naming no file of the folder being packed is refused at
  import. The old game's five font urls (`objects/fonts/Lato-Regular.ttf`) aren't data paths: the test
  of its stylesheet compiles a copy with only those five rewritten (font sources are in no computed
  style). (Not resolved relative to the sheet's own path as a browser resolves them.)
- **S7-2. What compiling refuses:** a rule whose selector the engine can't parse, except one naming
  another browser's prefixed pseudo-class or pseudo-element (`-moz-`, `-ms-`, `-o-`), which Chromium
  drops too: dropped and logged; an at-rule the engine doesn't handle (`@supports`, `@import`, `@layer`,
  `@container` …; `@charset` allowed); an unknown property; a value the engine can't parse for a
  property it takes. Properties it deliberately ignores (`fill`, `touch-action` …) stay allowed and are
  dropped, as parsing drops them. (Not every dropped rule refused, the old stylesheet's test then
  compiling a copy without its two `-moz-` rules; not dropped rules only logged.)
- **S7-3. The compiled form's layout:** the rule structure as records, everything inside as CSS tokens:
  a style rule's selector tokens, its media query's tokens (or none) and its prepared declarations
  (property, value tokens, important, the pending shorthand); keyframes with their name, media and
  frames (offsets and the declarations as written, which the style engine prepares as before); a font
  face's descriptors as written; a token's kind, text, number and unit, fields that don't apply null
  and left out of the JSON form. Selectors and media queries are parsed from their tokens by the
  engine's own parser at load, so the format doesn't follow the selector matcher's internals. (Not
  selectors and media queries as structured records, each new pseudo-class a new layout version.)
- **S7-4. Fonts and images at run time:** `UiDocument.Files` (the game sets it to `Application.Data`):
  with it the author sheets' @font-face rules are honoured; `Stylesheet.Load(files, path)` gives a
  compiled sheet, checked at load; `UiHost` reads images (PNG, decoded) and SVG text from the document's
  data when its `LoadImage`/`LoadText` callbacks give nothing. A @font-face font that isn't in the data
  or doesn't load (a mod or the development folder can cause it) is an error naming the sheet that
  stops the game, as a damaged entry does (Q6, S6-3); an image or SVG that is missing or doesn't decode
  shows nothing and is logged once, as Chromium shows a broken image (markup names images freely).
  (Not both errors; not both as Chromium.)
- **S7-5. Where the fonts are packed from:** `Tools/Scripts/make-ui-pack.sh` copies the five faces from
  `old-game/objects/fonts/` into a scratch folder in the build cache under names that follow the path
  rule (`ui/fonts/lato-regular.ttf`, `lato-semibold.ttf`, `lato-bold.ttf`, `stixtwotext-regular.otf`,
  `stixtwotext-italic.otf`) and packs it into `engine/Assets/Packs/ui.pak` (S6-5's way: the pack is the
  source of truth, git gets no third copy). (Not a committed source folder `engine/Assets/Sources/ui/`.)
- **S7-6. The licence texts** go into the pack too, `ui/fonts/lato-ofl.txt` and `ui/fonts/stix-ofl.txt`:
  the OFL asks every copy of the fonts to carry the licence, and the font files hold only its name and
  URL. (Not the five faces only, the licences left to M12's packaging.)

Accepted with them (internal design the step proposed): the importer `[Importer("ui", ".css")]` takes
`.css` only (it can't claim `.png` or `.svg`: step 8's terrain importer reads its PNG maps as its own
sources), a compiled sheet at its path without the ending (`ui/styles/<name>`), fonts and images files
at their paths (`ui/fonts/…`, `ui/images/…`); record types `ui.*`, fields numbered in declaration order;
fonts loaded when a sheet is added, no swap period (the references wait for `document.fonts.ready`); a
@font-face family hides the library's own faces of that name, as Chromium matches a document's
@font-face families before the platform's fonts; faces from the same data and descriptors shared within
a font library and given back when the sheet goes; the descriptors honoured: font-family, src (one
`url()` with an optional `format("truetype"|"opentype")`), font-style normal or italic, font-weight (a
number, a range, `normal`, `bold`), font-display (no effect), anything else refused at import and at
load; the tests' plan (the game screens on the compiled sheet, their documents without a file system so
their fonts stay `TestFonts`'; @font-face from the pack checked against `TestFonts`' registration).

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
  and the development folder since step 9 (below, step 9).
- **`TestPaths.Packs`:** the repo's `engine/Assets/Packs/`. `CommittedPackTests` checks every committed
  pack there: one of the area packs, opens, every entry's hash, every record readable with the engine's
  record types (none is committed yet: steps 6 and 7 commit the first).
- **Later needs:** packs are the source of truth and the editor (M11) edits them, so once a pack holds
  both imported and editor-made entries (`audio.pak` with the port's effect patches), re-importing must
  keep the other entries: an "update" command (sources added to or replacing entries of an existing pack)
  is needed then; no step of M9 needs it (steps 6 and 7 build their packs whole). **Done in M11 step 2
  (2026-11-06):** `packer update <folder> <file.pak>` (`PackWriter.Update`; "Packs for the editor" below);
  `make-music-data.sh` and `make-ui-pack.sh` still pack whole, switching to it once an editor-made entry is
  committed into their pack (Docs/Editor.md, P30). The packer reads only the
  engine's record types and importers; the port's own record types will need it to load the game's
  assembly too (Docs/Editor.md, G3, kept for the port).

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
  Windows) in ordinal order; `Roots()` is `/` on Linux, the ready drives on Windows. A folder that can't be
  read lists as empty (its `IgnoreInaccessible`), without an exception (found in M11 step 3: its comment named
  an `UnauthorizedAccessException`; corrected).
- **The file browser's model** (M11 step 3, beside the listings; `Docs/Editor.md`, "How it works", step 3):
  `FileBrowser.Places` (the mods folder, Documents, home, the roots, the recent places that are there),
  `FileBrowser.List` (folders first, then the files whose names end in the endings asked for, in any case, each
  by name ignoring case; hidden ones when asked), `ReadPath` (the path field's text read against the folder
  shown: a folder, a file, a new file's name, nothing, not a path) and `Complete` (its last name completed), and
  `RecentPlaces` (ten, most recent first, kept by the caller): the editor's browser screen, and later the
  game's Load Character, are built on it.
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
- **Tests never reach this machine's folders** (user decision 2026-11-05; Engine-Plan.md "Decision on test runs and the
  player's folders"): the test assembly's module initializer (`Tests/Statventure.Tests/Infrastructure/PerUserFolders.cs`)
  points .NET's `XDG_DATA_HOME`, `XDG_CONFIG_HOME` and `HOME` (Windows: also `APPDATA`, `LOCALAPPDATA`, `USERPROFILE`) into a
  per-run scratch folder, so `GameFolder.ForThisMachine()`, an `Application` without `GameFolder` and
  `DocumentsFolder.ForThisMachine()` land there (`PerUserFoldersTests`), and `mutate.py` flags any change to the real game
  folder or the Documents folder's top level. Found after M9 step 9's mutation dropping `GameFolder` wrote a test save into
  the PC's real folder. Not checked on Windows, whose known-folder lookup may ignore the variables.

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
- **The model record, version 2** (M10 step 3, `Docs/Scene.md`, "How it works", step 3, S3-4): `model` 2 adds
  `cameras` (`model.camera`: name, projection `orthographic`/`perspective`, verticalFieldOfView in radians (default
  0), orthographicHeight (default 0), near, far) and `lights` (`model.light`: name, strength in raw units, angle in
  degrees), both empty by default; `model.node` 2 adds `camera` and `light` (default −1: none). Version 1 of `model`
  and `model.node` are frozen copies (`ModelData.V1`, `ModelNode.V1`) whose upgrades give no cameras or lights and
  nodes carrying none; the version-1 fixture `model/model` stays (its upgrade is tested) beside the version-2 fixture
  `model/stage`. `ModelData.Validate` adds: nodes' cameras and lights that exist, one light at most and on one node
  at most, and cameras' and suns' values that make a projection and a sun.
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
### Music and effects in `audio.pak` (step 6)

Code in `Engine/Statventure/Audio/Music/` (`ThemeData.cs`, `MusicLibrary.cs`, `Soundtrack.cs`, `Composer.cs`,
`GraphBuilder.cs`), `Audio/AudioSystem.cs`, `Audio/Effects/EffectPatch.cs`, one line of `Core/Application.cs`;
the importer `Tools/Scripts/make-music-data.sh` with `Tools/AudioReference/music.cjs`; the pack
`engine/Assets/Packs/audio.pak`. Tests in `Tests/Statventure.Tests/Audio/` (`MusicLibraryTests`,
`ApplicationMusicTests`, `TestMusic`: the committed pack's library, which every music test now uses).

- **Records** (Q22): `audio.theme` (`ThemeSpec`), `audio.eq`, `audio.section`, `audio.layer`, `audio.harmony`,
  `audio.doubling`, `audio.instrument`, `audio.percussion`, `audio.room` (positional), `audio.theme-order`
  (`ThemeOrder`: the theme ids), and for effects `audio.effect` (`EffectPatch`), `audio.effect-layer`,
  `audio.effect-filter`, `audio.effect-range`; all version 1, fields numbered in declaration order, the
  JSON names the properties' own (`timpaniOctave`, `doublings`, a drums layer's `drums` as an object in the
  spec's order, a layer's `pattern` and `drums`, `degree` and `degrees` apart where the old `p` and `deg`
  held either). An absent key is null and the composer applies the old defaults; a percussion voice's
  `log`, `swell`, `thunder` and `timpani` are plain bools (false when absent), written always (Q33). Layouts
  and fixtures in `Tests/Statventure.Tests/Data/Records/` (`every-field` samples, a room, a theme order, a
  patch on its defaults).
- **Where they are** (Q21, S6-1): `audio/themes/<name>`, `audio/instruments/<name>`, `audio/percussion/<name>`,
  `audio/rooms/<name>`, `audio/effects/<name>`, and `audio/theme-order`. `MusicNames` holds the folders
  and the rule: `ToPathName` (a–z, 0–9 and `-` as they are, a capital as `_` and the small letter, `@`
  as `.`, anything else or a result the path rule refuses: null), `FromPathName` (its exact inverse: a
  `_` must be followed by a small letter, no `/`), `PathOf` (an `ArgumentException` for a name it can't
  store).
- **`MusicLibrary`** (public): `Themes` (the ids in order), `Theme(id)`, `Instruments`, `Percussion`,
  `Rooms`, `Effects` (by name), `Empty`; internal `Compiled(id)` (every theme compiled the first time one
  is asked for, once per library) and `Room(name)` (the hall for a room it doesn't have, as the old
  composer). `MusicLibrary.Load(data)` reads each folder in path order (a path the rule doesn't make, a
  nested one included, is an error), checks each effect patch as `Register` does (`EffectPatch.Problem`,
  shared with `SoundEffects.Register`), each theme's id against its path, and each theme's room (its own,
  or the hall when it names none or ""): there, or the hall there to fall back on. No music entries (no
  theme, instrument, percussion voice or room, no order): a library without music, its effects loaded
  (S6-3). Otherwise the order must be there; its ids each once, each storable and with an entry; themes
  it doesn't name follow in path order (S6-2). Every problem is a `DataFileException` named as the file
  system names its own (`VirtualFileSystem.Problem`, step 5's): the pack and entry, or the loose `.json`
  file alone; a wrong record type or a file where a record should be is the file system's own error.
  Since M11 step 5 (G4, G5) it then checks each theme against the composer, its first problem naming the entry
  and field, and keeps the themes it compiled on the way (`Docs/Audio.md`, "Checks and marks").
- **Instances, not statics** (Q3): `AudioSystem(MusicLibrary)` (public; the internal constructors take
  one too) prepares its library's rooms, registers its effect patches under their names (S6-4), and
  warms up on those of `alder-vale`, `combat` and `great-imp-jiruan` the library has. `Soundtrack` holds
  the `Library`; `Themes` is the instance's (`Application.Audio.Music.Themes`); `Play` of an id the
  library lacks is the `ArgumentException` it always was; `OfflineLength` and `RenderOffline` take the
  library. `GraphBuilder` carries the library for the composer (`Performance` reads instruments,
  percussion and rooms there). Prepared rooms stay cached for the process, keyed by the room's name, its
  values and the rate, so libraries with different rooms of one name don't share them.
  `Application` creates its sound with `new AudioSystem(MusicLibrary.Load(Data))`. `PlayTheme` and
  `AudioBenchmark` load the repo's `Assets/Packs/audio.pak`, found above their folder.
- **The importer** (S6-5): `make-music-data.sh` empties `$STATVENTURE_CACHE/music-data/`, runs `music.cjs`
  (`--music-data=<folder>`), which maps every JavaScript key to its record field (an unknown key stops it,
  as before) and writes each record's JSON form exactly as `RecordSerializer.ToJson` writes it (`$types`
  in the order a depth-first walk meets the types, fields in field order, a non-nullable default written,
  `-0` kept), then packs the folder with the packer into `engine/Assets/Packs/audio.pak` (118 entries, 57
  KB; unchanged entries keep their stored bytes). Extracting the pack gives the importer's files back byte
  for byte (checked by hand).
- **Checks:** before `MusicData.g.cs` was deleted, a one-off test compared every pack-loaded record with it
  (55 themes in order, 34 instruments, 21 percussion voices, 7 rooms, the theme order): equal field for
  field and bit for bit, and each entry's content the binary form of the generated record (it failed on
  one changed drum pattern). Every music test then ran unchanged on the pack (`TestMusic.Library`): the
  scores and lint, the 61 performances' exact call logs, the synthesizer, output and effect fixtures (the
  effect fixtures' patches are now read as the records' JSON form with a `$types` table added).
  `MusicLibraryTests`: the rule both ways (and on every name of the game's music, and 2000 random
  names), loading from a pack and from a loose folder (one theme replaced by the development folder), the
  order rules, ids against paths, rooms and the hall, no music, every unplayable patch (through loading
  and through `Register`), effects registered by name (sample for sample as registered in code, and still
  replaceable), libraries side by side and their prepared rooms, the committed pack's contents, and the
  committed pack repacking to the same bytes (reusing its entries) and to the same entries (compressed
  afresh). `ApplicationMusicTests` runs an application (a real window, the default device) with a
  development folder and finds its sound playing that folder's music. By hand: the sandbox with no data
  (starts, no music), with the pack's extraction as its development folder (starts), with a theme whose id
  breaks its path (stops, logged, naming the file); `play-theme.sh` lists the 55 themes;
  `audio-benchmark.sh --start` times a theme start as before.
- **Later needs:** the library is read once, when the audio system starts; reloading it on
  `Data.Reloaded` is step 9's (Reload; done: `AudioSystem.Reload`, S9-D). The packer still rebuilds `audio.pak` whole from the importer's
  folder: once the pack holds the port's effect patches too, the "update" command above is needed.

### Terrain in packs (step 8)

Code: `Render3D/Terrain.cs` (`TerrainData`: the record, its loaders and check; `Terrain.Load(device, files,
path)`), `Render3D/TerrainImporter.cs`. Tests: `Tests/Statventure.Tests/Render3D/TerrainPackTests.cs`,
`Render3D/TerrainPackRenderTests.cs`; the layout and fixture in `Data/Records/` (`terrain`). No pack is
committed (Q1).

- **The record** (Q26, S8-2): `terrain` 1 (`Layouts/terrain.txt`). `Heights`, the floats the GPU terrain is
  built from, aren't stored: they are worked out from the samples when first read, by M6's formula in
  floats (`min + s / 65535f · (max − min)`), so the test terrain's heights are bit for bit what M6's loader
  gave (pinned by their SHA-256, taken before the change) and the Blender comparisons hold. A
  `ushort[]` is the serializer's list of numbers: in JSON one number per line (the test terrain's
  JSON form has 3,072 lines of samples; a 512 × 512 region would have 262,144), in binary a block of
  2 bytes each, and the covers one base64 string, 1 byte each.
- **The description** (`TerrainData.Load(path)`, and the importer through the same parser): a JSON
  object with `width` and `depth` (whole numbers), `cellSize`, `heightMin`, `heightMax` (numbers),
  `height` and `cover` (the images' file names, relative to the description's folder), `covers` (the
  slot names, strings), and `waterLevel` (a number; missing or anything else: no water); other keys are
  ignored. The images are decoded from memory by Stb as M6's loader decoded them by path (the same
  decoder): the height image at 16 bits as one grey channel (an 8-bit image's v becomes v · 257, a
  colour image grey), the cover image at 8. Each must be `width` × `depth`. A description that can't be
  used is an `InvalidDataException` naming the problem ("the description has no number "width"", ""depth"
  isn't a whole number", "covers[1] isn't a string", "the height image h.png is 64x48, not 32x48", "the
  cover image c.png isn't an image stb_image reads"), from `Load(path)` with the description's path
  before it; text that isn't JSON a `JsonException`. (M6's loader threw whatever `JsonElement` threw for a
  missing key and said "isn't WxD 16-bit" for both a wrong size and an unreadable image; a null slot name,
  which it took and which failed later when drawn, is refused now: a record can't hold one.)
- **The importer** (`TerrainImporter`, `[Importer("world", ".terrain.json")]`; S3-3, S8-1): each
  `<name>.terrain.json` of a folder being packed becomes one record entry at its path without the ending
  (`world/terrain/vale.terrain.json` → `world/terrain/vale`). Its images are read through the context
  (`ReadSource`) at `<the description's folder>/<name>`, so they aren't packed themselves; a name may go
  into a subfolder (`maps/h.png`) but not up (`../h.png` breaks the path rule). Another image beside them
  (the test terrain's one-hot cover image, Blender's) is packed as a file. A problem stops the packing
  with a `DataFileException` naming the description ("the world importer: …", step 3's wrapping): every
  problem of the description above, an image the folder hasn't ("No 'world/terrain/h.png' in the folder
  being packed"), a name outside the path rule, an image another importer takes.
- **Loading** (`TerrainData.Load(files, path)`): `files.Load<TerrainData>(path)` (a pack's binary form or a
  loose `<path>.json`; step 3's errors), then `Validate()`, which checks what the description's loader
  checks of its images: the grid at least 1 × 1, and `width` × `depth` samples and covers (counted in 64
  bits). A failure is a `DataFileException` naming the pack and entry, or the loose file with no entry
  ("not a valid terrain: 3 height samples for 2x2 points"), the `InvalidDataException` inside, made by
  `VirtualFileSystem.Problem` (step 5's).
  `Terrain.Load(device, files, path)` makes the GPU terrain from the checked data. Not checked, as before:
  cover indices beyond the slots and more than 16 slots (terrain.frag weighs a point whose cover has no
  slot without a tone, darkening the ground there, and blends at most 16 covers; it reads nothing out of
  bounds), nor finite or positive sizes and heights.
- **Tests** (`TerrainPackTests`, 14; `TerrainPackRenderTests`, 4): the description-loaded test terrain
  against M6's hashes; the pack-loaded and the JSON-form-loaded terrain equal to it field by field, floats
  by their bits (the derived heights included); the pack's entries; the importer's ending and name; images
  relative to the description's folder (a description at the top of the folder and one deeper); 23
  unusable descriptions each failing the packing with its problem; 8-bit images (grey, as RGBA), unknown keys,
  a water level that isn't a number; six broken records refused at load from a pack and from a loose
  folder (and by `Terrain.Load` before the GPU); the smallest valid grids; a file or another record type
  where a terrain is asked for; both forms with and without water; the three terrain scenes drawn from the
  pack against their Blender renders and pixel for pixel against the description-loaded terrain's.

### The game's data at run time (step 9)

Code: `Tools/Scripts/build.sh` and `build.ps1`, `Core/Application.cs` (`Storage`, `ReloadData`, the mods in
`DataMounts`, `ApplicationOptions.GameFolder`), `Audio/AudioSystem.cs` (`Reload`), `Audio/Music/Soundtrack.cs`
(`Use`), `Audio/Music/MusicLibrary.cs` (`SameMusic`). Tests: `Tests/Statventure.Tests/Packs/ApplicationModsTests.cs`,
`Core/ApplicationStorageTests.cs`, `Core/ApplicationReloadTests.cs`, `Core/AudioReloadTests.cs` (and the
helper `Core/TestApplication.cs`: a real application with a small window, as `ApplicationMusicTests`).

- **The game folder the build makes** (S9-A): after the solution, `build.sh` publishes the sandbox from that
  build into `$STATVENTURE_GAME` (`dotnet publish --no-build`, framework-dependent, so the folder holds the
  BRUTAL natives of every runtime: 154 MB, copied once, then only what changed), then makes `packs/` hold
  exactly `Assets/Packs/*.pak`: a `.pak` there that isn't committed is removed (other files are left), an
  unchanged one is left alone (compared byte for byte), a changed or missing one is copied beside its place
  (`<name>.pak.<pid>.tmp`) and renamed over it, so a game running from the folder keeps its open pack until it
  reloads. `test.sh` runs `build.sh`, so every test run does this. `build.ps1` does the same in PowerShell
  (the extension tested apart from `-Filter *.pak`, which matches longer extensions through 8.3 names; the
  hash compared; `File.Replace` puts a changed pack in place, as Windows PowerShell's .NET has no overwriting
  `File.Move`); not run (no PowerShell here, and Windows isn't checked).
- **The mounts** (Q2, Q8, S3-4): `Application.DataMounts(options, executableFolder, environment, modsFolder)`
  gives the area packs, `classes/`, then `Mount.Mods` of the player's `mods/` (a full path; mounted even when
  the folder isn't there, so a mod put there later shows on Reload), then the development folder. `Run()`
  passes `AppContext.BaseDirectory` and `Storage.ModsFolder`; each mount and each mod is logged ("Data").
  `ApplicationOptions.GamePacksOnly` (M11 step 3, for the editor, which shows one pack over the game's packs: P6;
  `Docs/Editor.md`, "How it works", step 3) leaves the area packs only: no `classes/`, no mods folder, no
  development folder (the option's nor `$STATVENTURE_DEV_FOLDER`), so a damaged mod can't stop its start; Reload
  re-reads the same. Off by default; the game and the sandbox don't set it.
- **`Application.Storage`** (S9-B): made first in `Run()` (`ApplicationOptions.GameFolder`, made a full path,
  else `GameFolder.ForThisMachine()`; logged, "Storage"); creating it touches nothing on disk. `Dispose()`,
  which `Run()` ends with whether the start, the loop or the game's `Unload()` failed or not, disposes it
  after the window and the sound (the window is gone while the last saves are written): pending saves and
  settings are written, then it refuses more (`ObjectDisposedException`); the property is null afterwards.
  Saves the game makes in its `Unload()`, where the window's document runs its unload sequence, are written.
- **`Application.ReloadData()`** (S9-C), on the game's thread while the application runs (else an
  `InvalidOperationException`): `Data.Reload()`; the application's own listener of `Data.Reloaded`,
  subscribed before `Load()` so it runs before the game's, notes that the data was remounted and gives the
  sound `MusicLibrary.Load(Data)`; the game's listeners follow. A mount that fails (any exception before the
  remount) is logged as an Error ("not reloaded, the data stays as it was: …", the problem naming the file)
  and gives false, nothing told. Music that doesn't load (an `IOException`: a `DataFileException` naming the
  entry or file) is logged as an Error ("the music isn't reloaded, the sound keeps what it had: …"), the sound
  keeps its library, the game's listeners still run, and it gives false. Otherwise "reloaded" is logged
  ("reloaded, but not the music" in that case). An exception from a game's listener isn't caught (the data is
  the new one then). Without sound only the data reloads.
- **The sound** (S9-D): `AudioSystem.Reload(music)` (public since M11 step 3, for the editor's preview of an
  edited theme; internal before) starts preparing the new library's rooms on a
  worker (as at the start; `RoomsReady` is then that work), switches the music (`Soundtrack.Use`), and
  registers each effect patch of the new library that the old one didn't have or had with another binary form;
  others aren't touched (a patch gone from the data stays, one registered in code under a name whose data
  patch didn't change stays). `Soundtrack.Use(library, preparedRooms)`, on the game's thread: a new
  `GraphBuilder` for the library (its composer kit made there, as for the first library: the old kit caches
  rooms by name), `Library` and the rooms' preparer taken at once (so `Themes` and `Play` use the new
  library), and, when the theme the game last asked for (`Play`, forgotten on `Stop`: what plays once the
  queued calls are applied) isn't the same music in the new library (`MusicLibrary.SameMusic`: its theme
  record, and every instrument, percussion voice and room of both libraries, equal by binary form, a key
  missing on either side counting), its compiled theme from the new library (every theme is compiled then,
  on the game's thread) and its room prepared; then queued for the audio thread: the new builder, and the
  restart as a switch (`Theme` cleared, then `Play`: the old bus fades out over 0.55 s while the new
  performance fades in over 0.8 s). A theme the new library doesn't have plays on (its compiled theme is
  null). The performance playing keeps its own builder, so it plays on with the old data until replaced.
- **Tests:** `ApplicationModsTests` (4): the mount order with packs, classes, mods and the development folder
  (and without either); mods over packs and classes, two mods in name order, the development folder over a
  mod, records too; Reload after a pack rebuilt by copying and renaming (the open pack read until then);
  mods added, a second before and after, and taken away, the folder made and removed after the start.
  `ApplicationStorageTests` (5): the folder from the option or this machine; a real application's storage at
  the option's folder (relative), logged, its mods mounted below the development folder, gone afterwards,
  nothing written; a save made in `Unload()`, in a `Load()` that throws and in an `Update()` that throws,
  each with the writer taking a second, written when `Run` ends, and the storage refusing more.
  `ApplicationReloadTests` (6): a Reload with a theme changed in the development folder, a new file and a mod
  added (the game's listener running once, after the sound has the new music); a mount that fails, then
  mended, then failing again after one that worked; music that doesn't load, then mended; a game listener's
  exception; no sound; not running. `AudioReloadTests` (15): the restart equal sample for sample to the game
  stopping and playing the new theme at once; playing on, equal sample for sample to no Reload, when another
  theme changed, the same music was read again (equal records, other instances), the order and effects
  changed, the theme is gone, or it was stopped; a restart for its theme, an instrument it doesn't play, one
  added or taken away, a percussion voice, a room it isn't in; later plays on the new instruments, as a
  system started on the new library; the effect patches' rule (rendered against patches registered by hand);
  `SameMusic`'s rule.
- **Checked by hand** (2026-11-03): `build.sh` in the worktree laid out `runtime/` (the sandbox, `packs/audio.pak`);
  a stale `world.pak` there was removed and a `notes.txt` left, an unchanged `audio.pak` kept (same inode and
  time), a damaged copy replaced by a rename while a reader held it open (the reader still read the old byte),
  no temporary file left, a missing `packs/` made. The published sandbox run from that folder with a per-user
  folder of its own (`XDG_DATA_HOME`) holding a mod (`faster-vale.pak`: alder-vale at another tempo and a new
  theme) and a development folder: it logged the game folder, then `Pack …/runtime/packs/audio.pak`, `Mods
  …/stat-fick/mods`, `Folder …/dev`, `mod …/faster-vale.pak`, opened its window, started the sound on the
  music with the mod's themes, captured its frame and quit; the per-user folder held only the mod afterwards.
  With a byte of the mod's index flipped it stopped before its window, logged: "faster-vale.pak: the index
  doesn't match its hash (damaged)". With the mod's alder-vale given another id it stopped when the sound
  started (after the window, as step 6's order has it), naming the mod and entry.

### Stylesheets, fonts and images in `ui.pak` (step 7)

Code: `UI/Css/StylesheetRecords.cs` (the record types), `UI/Css/CssParser.cs` (`CssParseDiagnostics`: what
parsing leaves out), `UI/Css/FontFaceDescriptors.cs`, `UI/Style/Stylesheet.cs` (`Compile`, `ToRecord`,
`FromRecord`, `Load`), `UI/StylesheetImporter.cs`, `Text/FontLibrary.cs` (sheet faces), `UI/Dom/UiDocument.cs`
(`Files`, @font-face), `UI/UiHost.cs` (`UiResources`); the pack `engine/Assets/Packs/ui.pak`, made by
`Tools/Scripts/make-ui-pack.sh`. Tests in `Tests/Statventure.Tests/Ui/`: `StylesheetCompileTests`,
`CompiledGameStylesTests`, `FontFaceRuleTests`, `UiResourcesTests` (with `UiHostResourceTests`), the helper
`GameStylesheets`; layouts and the fixture (`ui.stylesheet/every-kind`) in `Data/Records/`.

- **The records** (S7-3), all version 1: `ui.stylesheet` (rules, keyframes, fontFaces), `ui.style-rule`
  (selectors: tokens, declarations, media: tokens or null), `ui.declaration` (property, value: tokens,
  important (false), shorthand (null): in a style rule prepared, a longhand by its canonical name or a
  custom property, a longhand of a shorthand whose value holds var() keeping the whole value and naming
  the shorthand; in keyframes and font faces as written), `ui.keyframes` (name, frames, media),
  `ui.keyframe` (offsets: float32s, declarations), `ui.font-face` (descriptors), `ui.token` (kind, text,
  number, unit; the last three null where they don't apply). A token's kind is `CssTokenType` with the
  token's flag folded in (`integer` and `integerDimension` for numbers written as integers, `idHash` for a
  hash that starts as an identifier), so a kind says which fields it has: numbers on the five numeric
  kinds, units on the two dimension kinds; text is the token's value as the tokenizer and the shorthands
  give it (a whitespace's " ", a comma made by a shorthand ","), null when empty. Every token the engine
  makes has an exact record (`CssTokenRecord.From` checks the round trip); a record whose fields don't fit
  its kind is refused at load. What a declaration's `HasVar` and `Keyword` are is worked out again from its
  tokens (sizes and times below).
- **Compiling** (`Stylesheet.Compile(text, fileExists)`, S7-2): the text is parsed and prepared exactly as
  `Stylesheet.Parse` does, with `CssParseDiagnostics` collecting what the parser leaves out and `Prepare`
  telling each declaration it drops for an unknown property or a value it can't parse. Problems: a rule
  whose selector doesn't parse, unless the selector names a pseudo-class or pseudo-element with another
  browser's prefix (`:-moz-focusring`, `::-ms-clear`, `:-o-prefocus`: dropped, listed in `Dropped`); an
  at-rule other than @media, @keyframes, @-webkit-keyframes and @font-face (a block or a statement; the
  statement @charset allowed); a declaration that doesn't parse (no name or no colon, CSS nesting among
  them), a stray `}`, a rule or keyframe without a block, a @keyframes without a name, a keyframe selector
  that doesn't parse (any part not `from`, `to` or a percentage from 0% to 100%: Chromium drops the whole
  keyframe); an unknown property and a value the engine can't parse, in style rules and in keyframes (a
  keyframe's !important declarations aside, which CSS ignores); a @font-face the engine can't honour or one
  inside @media (the engine registers fonts unconditionally); a `url()` (an unquoted url token or
  `url("…")`, in any declaration, keyframe or descriptor) that isn't a `data:` url (any case) or a data path
  under the path rule, or that names a file `fileExists` doesn't know. `Files` lists the data paths the urls
  name. Properties the engine deliberately ignores are dropped without a problem. The record is made from
  the parsed sheet (`ToRecord`; the parsed rules keep their selector and media tokens for it:
  `CssStyleRule.SelectorTokens`, `MediaQuery.Tokens`). `Stylesheet.Parse` now also counts the rules dropped
  for their selectors (`DroppedRules`, which nothing set before).
- **Loading** (`Stylesheet.Load(files, path)`, `FromRecord`): the record is read (`files.Load`), each rule's
  selectors and media query parsed from their tokens by the engine's parser, and checked again as importing
  checks it (S5-1's rule): a token that doesn't fit its kind, a selector that doesn't parse, a declaration
  whose property isn't a longhand by its canonical name or a custom property (`--` alone refused), whose value
  the engine can't parse (unless it holds var() or is a CSS-wide keyword), or whose pending shorthand isn't
  a shorthand (by its canonical name) setting the property or comes without var(); a keyframe whose offsets
  are none or outside 0–1 (NaN too), or whose declarations parsing would drop, or naming a pending
  shorthand; a @font-face the engine can't honour; a url that is neither `data:` nor a data path. A problem
  is a `DataFileException` named through `VirtualFileSystem.Problem` ("not a valid stylesheet: rule 12
  (.a): 'color: 12px' isn't a value the engine can parse"). The sheet is the one its text parses to, part
  for part, with the rules' order as their positions, named by its path, remembering where it came from
  (`LoadedFrom`); its rules keep no unprepared declarations.
- **Sizes and load times** (facts for the editor's and the dump's later work, as step 5 noted a model's
  JSON form; measured on the old game's sheet, 2941 rules and 20,275 prepared declarations, Debug build, a
  machine loaded by other workers' runs): the compiled record is 1.79 MB, stored as 78 KB (Brotli); its JSON
  form (the dump, `extract`, the development folder) is 12.5 MB and 576,293 lines, each token an object of
  two to four keys. The sheet parses from its text in 70–80 ms and loads from a pack in 95–180 ms (reading
  the record 30–70 ms, checking it and building the sheet 45–90 ms): the compiled form is checked, not
  faster. A more compact form for the editor or the dump (tokens as text, say) would be a later choice.
- **The importer** (`StylesheetImporter`, `[Importer("ui", ".css")]`): each `.css` (UTF-8, a byte order mark
  allowed; anything else refused) is compiled with the folder's files as `fileExists` and becomes the record
  at its path without the ending; problems stop the packing as a `DataFileException` naming the file ("the
  ui importer: 3 problems: …", at most 20 shown, then "and n more"); dropped rules are logged (Info,
  "Data"). A url naming a file another importer takes as its source passes the check but has no entry (a
  terrain's PNG map): not worth a rule while nothing names one.
- **Fonts** (Q24, S7-4): `FontFaceDescriptors.Of(rule)` reads a @font-face as the engine honours it (the
  family: a string, or identifiers joined by single spaces, not a generic family or a CSS-wide keyword; one
  `url()` source naming a data path with an optional `format()` of `truetype` or `opentype`, as a string or
  an identifier; `font-style` normal or italic; `font-weight` one or two of a whole number 1–1000, `normal`
  (400) or `bold` (700), a range swapped when the first is larger, as Blink does; `font-display` one of its
  five keywords, no effect; the last of a repeated descriptor wins), anything else an
  `InvalidDataException`. `UiDocument.Files` set, each author sheet's faces are read and added to the
  document's `FontLibrary` when the sheet is added (or when `Files` is set: then for every author sheet,
  all or nothing), given back when it is removed (or `Files` changes), and the layout is redone when faces
  come or go. A face that can't be had (the rule, the read, FreeType) is an error naming the sheet: a
  `DataFileException` naming its pack and entry or loose file when it was loaded from the data, else an
  `InvalidDataException` naming the sheet (its name, or its path when its data is gone); the faces already
  taken for it are given back and nothing changes. `FontLibrary.AcquireSheetFace(data, family, weight,
  italic, weightMax)` gives the face made from the same data (SHA-256) and descriptors if there is one, with
  one more user, else makes it (a race between threads ends with one face); `ReleaseSheetFace` takes a user
  away, and the last one takes it out of matching (it is disposed with the library, since layout may still
  hold it). A family some sheet face has is matched among the sheet faces only, the later ones first (Chromium's
  FontFaceCache before the platform's fonts; a later equal @font-face wins); `ResolveDefault` takes the
  first sheet face's family when the library has no face of its own and no `DefaultFamily`. Documents sharing
  a library share its sheet faces (the game has one document; Chromium's are per document).
- **Images and SVG text** (Q25, S7-4): `UiHost` reads them through `UiResources`: what `LoadImage` /
  `LoadText` give first, else the document's data: a PNG (by its signature) decoded with `ImageFile.Load`
  (Stb) when first asked for, an SVG's UTF-8 text (a byte order mark dropped). An SVG (`.svg`, or a
  `data:image/svg+xml` url, which the painter decodes itself) isn't an image to decode. A url that isn't a
  data path, isn't there, is a record, isn't a PNG, doesn't decode or isn't UTF-8, or another `data:` url,
  gives nothing and is logged once per url (Warning, "UI"); a damaged pack entry stays an error (Q6).
- **The pack** (Q1, S7-5, S7-6): `make-ui-pack.sh` empties `$STATVENTURE_CACHE/ui-data/`, copies the five
  faces and the two licence texts there under `ui/fonts/` (`lato-regular.ttf`, `lato-semibold.ttf`,
  `lato-bold.ttf`, `stixtwotext-regular.otf`, `stixtwotext-italic.otf`, `lato-ofl.txt`, `stix-ofl.txt`) and
  packs the folder into `engine/Assets/Packs/ui.pak` (7 entries, 999 KB; unchanged entries keep their stored
  bytes). The game's own sheet and images join that folder at port time.
- **Tests:** the old game's sheet (`GameStylesheets`: the tests' copy with its five font urls rewritten,
  compiled by the importer into a pack made in the test run with the tests' font copies) gives the same
  computed styles as the text-parsed sheet on the 22 captured screens (11 states at device scales 1 and
  1.65), element by element and for
  ::before, ::after, ::placeholder, ::selection, ::marker and the controls' parts, the important longhands
  too; the two hold the same rules part for part; and since `GameLayoutTests.Build` takes the compiled
  sheet, every test of the game's screens (layout, paint, hit testing, compositing, at 1 and 1.65) runs on
  it. The committed pack's faces from the game's @font-face rules are `TestFonts`' (descriptors, metrics,
  matching at every weight, text laid out the same); the compile, load, descriptor, font and resource rules
  each tested at their limits; the committed pack's contents (the old game's files byte for byte) and its
  repacking to the same bytes; the fixture of every record type and token kind.

### Sequences and their stages (M10 step 4; `Docs/Scene.md`, "How it works", step 4)

- **Where:** a cutscene is a record entry at `world/cutscenes/<name>` in `world.pak` (M10-10), written in
  its JSON form and packed by step 3's rule for records' JSON forms; the stages it brings are models at
  `models/stages/<name>` in `models.pak` (imported from `.glb` as every model, Q14); particle effects
  are records at `models/effects/<name>` (M10 step 5, Q25; below). None is committed (M9 Q1; Ji'ruan's proof
  is test data, M10 Q30).
- **The types** (`Engine/Statventure/Scene/Sequence.cs`, all version 1; S4-1): `scene.sequence` (name,
  duration, skippable, blocking, roles, stages, tracks, reduced), `scene.role`, `scene.span`,
  `scene.strip`, the union `scene.track` with tags 1–11 (`scene.clip-track`, `scene.transform-track`,
  `scene.visibility-track`, `scene.animation-track`, `scene.palette-track`, `scene.event-track`,
  `scene.camera-track`, `scene.light-track`, `scene.particle-track`, `scene.audio-track`,
  `scene.ui-track`) and their cues (`scene.visibility-cue`, `scene.parameter-cue`, `scene.palette-cue`,
  `scene.event-cue`, `scene.camera-cut`, `scene.light-cue`, `scene.particle-cue`, `scene.audio-cue`,
  `scene.ui-cue`); curves are step 2's `scene.curve` and `scene.key`. Every track kind is declared now,
  so steps 5 and 6 of M10 add playback without a new layout. Layouts in `Data/Records/Layouts/scene.*.txt`,
  the fixture `Fixtures/scene.sequence/every-field`.
- **Loading:** `Sequence.Load(files, path)` reads the record and its stages from the same data (each with
  `ModelData.Load`) and checks it (`Validate(stages)`); a problem is a `DataFileException` naming the
  sequence's pack and entry or loose file through `VirtualFileSystem.Problem` (a curve that breaks its
  rules arrives from the serializer as an `ArgumentException` and is named the same way), except a broken
  stage, whose own error names the stage's entry.

### Particle effects (M10 step 5; `Docs/Scene.md`, "How it works", step 5)

- **Where:** a record entry at `models/effects/<name>` in `models.pak` (Q25), shared by battles and
  cutscenes, packed from its JSON form by step 3's rule for records' JSON forms (no importer); none is
  committed yet (M9 Q1).
- **The types** (`Engine/Statventure/Render3D/Particles.cs`, all version 1; S5-1): `particle.effect`
  (`ParticleEffect`: shape, startColor, endColor, opacity = 1, outlineWidth, glow, life, size, speed,
  spawn, growIn, fadeOut, gravity, drag, angle, spin, inherit; float32), `particle.range` (min, max),
  the union `particle.shape` (1 `particle.disc`, 2 `particle.ring`, 3 `particle.spark`, 4 `particle.puff`,
  5 `particle.shard`) and the union `particle.spawn` (1 `particle.point`, 2 `particle.sphere`,
  3 `particle.cone`). Layouts in `Data/Records/Layouts/particle.*.txt`, fixtures in
  `Fixtures/particle.effect/` (`every-field`, `defaults`, `ring-sphere`, `puff`, `shard`).
- **Loading:** `ParticleEffect.Load(files, path)` reads it and checks its rules (`Problem()`); a broken
  one is a `DataFileException` naming the pack and entry or the loose file. A sequence's particle cues
  name effects by path; the game loads them and binds them as it binds stages
  (`SequenceBindings.Effect(path, effect)`, S5-2), and `Play` refuses one missing, unnamed, given twice
  or broken.

### Packs for the editor (M11 step 2; `Docs/Editor.md`, "Packs for the editor")

What the editor (M11) needs of packs, beside step 1 and step 3's functions; nothing of the format (version 1),
of `PackFile.Open`'s checks, of `Application.Data`'s mounts or of the packer's other commands changed. Code in
`Engine/Statventure/Packs/` (`PackWriter.cs`, `PackFile.cs`, `VirtualFileSystem.cs`, `Importers.cs`),
`UI/StylesheetImporter.cs` (its urls' lookup), `Tools/Packer/Program.cs`. Tests in `Tests/Statventure.Tests/Packs/`:
`PackRewriteTests`, `CustomMountTests`, `ImportFileTests`, `PackUpdateTests`.

- **Rewriting a pack** (M11-17, P23): `PackWriter.AddFrom(pack, entry, path?)` adds an open pack's entry as it
  is, at its own path or another (a rename): it stays a record or a file, its content is the pack's (a record's
  binary form as stored, so an older version stays in it and a kind not known here is kept, P24), and its
  stored bytes and whether they are compressed go with it into whatever pack the writer writes, so a pack
  written elsewhere (Save As) keeps them too, not only the pack being replaced (whose reuse by content hash
  still applies to every other entry; a kept entry's bytes win over it). The entry is read and checked when
  added: a damaged one is a `DataFileException` naming the pack and entry and isn't added; the writer's rules
  hold as for any entry, a file named `.json` included. `AddFrom(pack, include?)` adds every entry the
  filter accepts, in path order (the ones before a failing one stay added). `Remove(path)` takes an entry out
  (its folders are counted, so a path is free again once nothing is below it), `Contains(path)` asks, and
  `AddRecordBinary(path, binary)` adds a record from its binary form as it is, not read. So a save is: the
  unchanged entries added from the open pack, the edited ones added as records (the current versions), the
  removed ones left out, then `Write` over the file. `PackWriteResult.Reused` counts the kept entries with the
  reused ones.
- **The index's hash** (P23): `PackFile.IndexHash` and `IndexHashHex`, the SHA-256 of the index as the header
  gives it and `Open` checked it. It changes whenever an entry is added, removed, renamed or changed (or its
  stored bytes move), so a save can tell whether another program rewrote the file since it was opened.
- **A custom mount** (P38): `Mount.Custom(name, entries)` mounts what an `IMountEntries` gives (`Paths`,
  `IsRecord(path)`, `Read(path)`: a file's bytes or a record's binary form), read as a pack's entries are, at
  any place among the mounts (`MountKind.Custom`, `Mount.Entries`). Which paths there are and whether each is a
  record are taken when it is mounted and on every `Reload`; the bytes when a path is read, so an edit shows at
  once and a new, removed or renamed entry after a Reload. Mounting checks the entries with the words a pack's
  index gets (a path outside the rule, a second entry with a path, an entry that is also another's folder, a
  record beside the file `<path>.json`), the first in path order named. `name` stands where a pack's file
  would: `Origin` gives it, and every error about its entries, the file system's and the loaders' through
  `Problem`, is a `DataFileException` naming it and the entry. The engine's loaders and cross-entry checks run
  on it unchanged (a terrain's check, `MusicLibrary.Load` over `audio.pak` with a theme edited above it).
- **One file imported** (M11-8, P28): `PackWriter.ImportFile(file, sourcePath, sources?, types?, importers?)`
  imports a file on disk as packing it at `sourcePath` in a folder would (the same importer, chosen by the source
  path's ending, the same records' JSON forms and plain files, the same errors naming the file) and gives the
  entries it made (`ImportedEntry`: path, record or file, content; a record in its binary form, which
  `AddRecordBinary` adds). The file's own name doesn't matter. `ImportSources` stands for the folder: `FileOf` gives
  the file on disk for a data path the importer reads besides (by default the files beside the source on disk
  and below it, by their paths relative to the source path's folder: a terrain's images), `Exists` says which
  data paths the entries go among (a stylesheet's urls; by default none). The importers see this through
  `ImportContext`: `Exists(path)` is new (packing a folder, its files; updating a pack, also its entries;
  importing one file, `ImportSources.Exists`), `Sources` is the file's own path only when importing one file,
  and a file not found is "No '…' among the files the import can read" there. `StylesheetImporter` checks its
  urls through `Exists` (before, the folder's files directly), with the same words ("names no file in the data
  being packed", `Stylesheet.Compile`'s); `TerrainImporter` needed no change.
- **Updating a pack** (`packer update <folder> <file.pak>`, `PackWriter.Update`; P30): the folder is packed as
  `pack` packs it (importers, records' JSON forms, files), its entries added to the pack or replacing its entries
  at their paths (a record may replace a file and back), every other entry kept with `AddFrom`, and the pack
  written over itself. The data a stylesheet's urls are checked against is the folder's files and the pack's
  entries, so a sheet may name an image or font the pack already has (following P28's rule; `pack` alone
  checks against the folder). Refused, the pack left as it was: a pack that isn't there (it isn't made: `pack`
  makes one), one `Open` refuses, a kept entry that is damaged (the folder replacing it, it isn't read), a kept
  entry the folder's entries leave no room for (a folder of one of them, a record beside its loose file:
  "can't be kept beside the folder's entries: …", naming the pack and entry), and any problem of the folder's.
  Output: `<file.pak>: n entries, a added, r replaced, c compressed, k kept from the old pack`
  (`PackUpdateResult`: `Written`, `Added`, `Replaced`).
- **Checked by hand** (2026-11-06): the packer's `pack`, `update` (a new file, a replaced one, a stylesheet
  naming the pack's image, again with nothing new: all five kept), `list`, a missing pack and a stylesheet naming
  a url neither has (exit 1, the pack unchanged), and the usage text, in a scratch folder
  (`claude-work/m11/step2-packer-smoke.sh`).
- **Not checked:** Windows: writing over a pack the editor holds open (sharing read, write and delete; M9's
  "Windows check", item 2a), and `ImportSources`' default lookup on a file system that ignores case (a
  description naming `vale-height.png` would find `Vale-Height.png` there; packing the folder refuses that name).

## Status

**M9 done (2026-11-03): all nine steps.** (Before step 1, on 2026-11-02, nothing was implemented; the
decisions and the 27 format details were recorded, the design and the nine steps with their dependencies settled.)

Windows: not checked for any step; the checklist is in Engine-Plan.md, "Windows check".

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
- **Step 6, the music and effects in `audio.pak`, `MusicData.g.cs` removed, done (2026-11-03):** ("How it
  works", step 6); five questions asked on the way (S6-1–S6-5, all as recommended). The committed
  `engine/Assets/Packs/audio.pak` holds the old game's 55 themes, 34 instruments, 21 percussion voices and 7
  rooms and the theme order (118 record entries, 57 KB), made by `make-music-data.sh` through `music.cjs`'s
  JSON forms and the packer; it passes `CommittedPackTests` and repacks to the same bytes. The one-off
  comparison before `MusicData.g.cs` went: every record equal, field for field and bit for bit. Every music
  test passes unchanged on the pack-loaded data; 58 new tests. The full suite passes on the GPU and on
  lavapipe (1658 tests); `test-themes.sh`: all 55 themes pass on the pack-loaded data (21 minutes). Mutations: 62, 61 fail a test; the one survivor, the rooms not prepared ahead, changes no sample (`Play` prepares a missing room on the caller's thread, so only when the work is done changes: step 7's speed decision, measured by `audio-benchmark.sh --start`). Found on the way: M8's tests
  never refused a patch with a negative attack or delay or with an empty harmonics list (now tested, through
  loading and `Register`). Not checked: Windows.
- **Step 8, terrain into `world.pak`, done (2026-11-03):** ("How it works", step 8); two questions asked on
  the way (S8-1, S8-2, both as recommended). `TerrainData` is the record `terrain` (the 16-bit samples
  and the range, the heights converted at load as M6's loader did, bit for bit), `TerrainImporter` takes
  `<name>.terrain.json` with its two images, `TerrainData.Load(files, path)` and `Terrain.Load(device,
  files, path)` load and check it. 18 tests in `Tests/Statventure.Tests/Render3D/` (`TerrainPackTests`,
  `TerrainPackRenderTests`). No pack committed (Q1). One point of this doc was wrong and is corrected above
  ("What exists"): the height image isn't only 16-bit, Stb's `Load16` takes 8-bit and colour PNGs too.
  The full suite passes on the GPU and on lavapipe (1686 tests). Mutations: 52 (the record's name,
  version, numbers, required fields and default; the height conversion, three ways of changing its float
  arithmetic among them; both loaders, the check and its limits, the problem's naming through
  `VirtualFileSystem.Problem`; every key, kind and image check of the parser; the importer's ending,
  name, folder, record path and reading through the context), all fail a test. Not checked: Windows.
- **Step 9, the game's data at run time, done (2026-11-03):** ("How it works", step 9); four questions asked on
  the way (S9-A–S9-D, all as recommended). The build lays out `$STATVENTURE_GAME` (the sandbox published
  beside `packs/`, which holds exactly the committed packs); `Application.Storage` (the player's game folder,
  `ApplicationOptions.GameFolder`, written out when the application ends, after the game's unload and after a
  failed start or loop); the mods of its `mods/` mounted between `classes/` and the development folder;
  `Application.ReloadData()` (the data remounted, the music reloaded, the theme playing restarted when its music
  changed, the data's new and changed effect patches registered; a failure logged, giving false). 30 new tests
  (`ApplicationModsTests`, `ApplicationStorageTests`, `ApplicationReloadTests`, `AudioReloadTests`);
  `ApplicationMusicTests` points its application at a game folder of its own (it would have mounted the real
  per-user mods). Checked by hand: the build script's copy rules, and the published sandbox in the shipped
  layout with a mod and a development folder (and a damaged mod, and a mod's theme with a wrong id). Two points
  of this doc were found open rather than wrong: nothing ran from `$STATVENTURE_GAME`, so packs copied there
  alone would have been read by nothing (S9-A), and Reload's mounts are the ones found at the start (now noted
  under "The virtual file system"). The full suite passes on the GPU and on lavapipe (1716 tests). Mutations: 54
  (`~/.cache/stat-fick-build/claude-work/m9/step9-mutations.json`), 49 fail a test (one malformed mutation, a `foreach` left
  without its body, was replaced and is killed); the 5 survivors change no sample, only when and on which thread work is done (as step 6's survivor): the restart's room not prepared on the game's thread ahead of it, `Play` preparing rooms through the old library's preparer, the new builder's composer kit made on the audio thread at its first performance (the same fixed-seed noise), the new builder without the prepared rooms (the composer makes the room's response and kernels itself, sample for sample the same, as the exact restart test shows), and the new library's rooms not prepared on a worker (the process-wide cache is filled on demand instead). The build script's
  rules broken by hand one at a time (7: no publish, no `packs/` made, stale packs kept, every stale file removed, unchanged
  packs copied again, copied in place, the temporary file left): each fails the check script
  (`claude-work/m9/step9/build-mutations.py`, `build-rules.sh`). Not checked: Windows (`build.ps1` not run: no PowerShell
  here).
- **Step 7, stylesheets, fonts and images into `ui.pak`, done (2026-11-03):** ("How it works", step 7); six
  questions asked on the way (S7-1–S7-6, all as recommended), the step's internal design accepted with them.
  The committed `engine/Assets/Packs/ui.pak` holds the old game's five font faces and their two licence
  texts (7 entries, 999 KB), made by `Tools/Scripts/make-ui-pack.sh`; it passes `CommittedPackTests` and
  repacks to the same bytes. The old game's stylesheet, compiled by the UI importer in the test run, gives
  the text-parsed sheet's computed styles on all 22 captured screens, and every test of the game's screens
  now runs on it; @font-face from the pack gives `TestFonts`' faces. 191 new tests in
  `Tests/Statventure.Tests/Ui/`. Two points of this doc were wrong and are corrected above ("What exists",
  "Importers"): the fonts' names in `old-game/objects/fonts/` break the path rule (renamed when packed), and
  "no dropped rule" would refuse the old stylesheet itself (two `-moz-` rules Chromium drops too, S7-2).
  Found on the way: the engine's own user-agent sheet sets `word-spacing` and `text-indent`, which the
  engine doesn't know (it stays parsed and embedded, so nothing refuses it); `Stylesheet.DroppedRules` was
  never set. The full suite passes on the GPU and on lavapipe (1907 tests). Mutations: 175 (`~/.cache/stat-fick-build/claude-work/m9/step7-mutations.json`, made by `make-step7-mutations.py` beside it): 162 fail a test; one was malformed (renaming a token kind's member breaks the code naming it; the layout guard is covered by two kinds' numbers swapped); 12 survived, all accounted for. Seven were gaps, closed and then killed (`step7-rerun.json`): keyframes under @media and an !important keyframe declaration were missing from the sample sheet (three mutations; added, its fixture recorded again), a malformed declaration inside an otherwise valid @font-face, family names joined by a comma without a space, a face of another weight with the same range end, and setting `Files` failing at a second sheet after the first's faces were taken. Three were redundant code, removed: the `::` branch of the other-browsers check (the loop meets a pseudo-element's second colon, whose next token is the name), the problems' de-duplication (nothing makes one twice; each is now listed as found) and the `IsShorthand` test of a pending shorthand (a name that isn't one has no longhands). Two can't differ in results, only in work: acquiring a sheet face looks for an existing one before making one (without it the face is made and retired as a duplicate, kept until the library goes: memory and time only), and setting `Files` to the file system it already has returns at once (otherwise its faces are taken again, the old ones given back and the layout redone). Not
  checked: Windows.

# Editor (M11)

The Statventure Editor: a separate program on the engine's UI that opens packs, lists their entries,
edits every record kind through one generic form made from the record definitions, with undo per
entry, the engine's checks as you edit, references between entries, import, a live preview, and
saving: mods for players, the game's own packs only from the repo. It ships with the game for modding
(M12 packages it). M11 builds the editor and what the engine needs for it.

## Decisions (user, 2026-11-05 and 2026-11-06; the manager's for the editor, 2026-11-06)

In full, with the options not chosen, in `Docs/Engine-Plan.md`, "Decisions for M11". Every answer was the
recommended option except the audience (M11-4), undo and redo (M11-7) and the name (M11-19).

**The user's rule for M11 (2026-11-06):** "For M11, only concern me with questions that are meant for the
game itself. The editor is at your full disgression, but do inform me of big decisions". The manager's
reading: design choices about the editor itself (its looks, layout, behaviour, its own file formats, its
tests) are decided by the manager of the session and recorded with their reason; workers still stop and
report such a choice to the manager instead of assuming; choices that change the game (what the game
shows, does or loads, data formats the game reads, the shipped game folder, anything a player meets
outside the editor) still go to the user with the question tool; big editor decisions are told to the
user. Everywhere else the rule to ask about every design choice stands. So this doc keeps the editor's
own choices, decided by the manager ("Decided by the manager (P1–P38)"), apart from the questions that
touch the game ("The game's questions (G1–G6)": two answered by the user, two decided by the manager as
editor matters or as following from the user's answers, two kept for the port).

**The design decided (2026-11-06):**
- **P1–P38, decided by the manager** under the user's rule for M11, every one as recommended (option (a));
  each with its options and reason under "Decided by the manager (P1–P38)".
- **G1, decided by the manager as following from M11-15** (the user chose an info entry inside the mod's
  pack that a mod list in the game can use later): a `mod/info` entry of a record kind the engine knows;
  the game never reads it for now.
- **G4, the user's answer (2026-11-06, the question tool): the game enforces declared limits too.** One
  truth for the editor and the game: a mod made by hand with a value outside a limit is refused by the game
  with a message naming the entry, as the editor refuses it; in M11 only limits the game's checks already
  enforce today are declared, so nothing that loads today stops loading. (Not the editor only; not slider
  ends only.) The recommended option.
- **G5, the user's answer (2026-11-06): the checks that today run only when something plays also run when
  the data loads, with the cost measured first.** A broken theme or cutscene is reported when the game starts
  or reloads its data, naming the entry; the step measures the added loading time first, and if it is
  noticeable the manager goes back to the user before it goes in. (Not the editor only with the game as
  today.) The recommended option.
- **G6, decided by the manager** (the editor's own settings): an `editor/` folder in the player's per-user
  game folder, beside `settings.json`, `saves/` and `mods/`; the game ignores it.
- **G2 and G3, not asked now** (no M11 step waits for them), kept for the port in `Docs/Engine-Plan.md`,
  "Open questions for later milestones", "At port time": the game treats a mod with parts it can't read as
  today (G2), and the editor and the packer take record kinds from any assembly they are given, so every
  option of G3 stays open.

- **Form (M11-1):** a separate editor app in `Tools/`, on the engine's UI, opening the pack files directly;
  the game stays free of editor code.
- **What it edits (M11-2):** every record kind through one generic form (a list of a pack's entries and a
  property grid made from the record definitions) with undo and redo; the port's kinds become editable
  with no new editor code; special views (a cutscene timeline, a map view, sound or particle editors) later.
- **Preview (M11-3):** a live preview pane on the engine's scene view, sound system and sequence player,
  updating as a value changes; its tests play sound at volume 0 only.
- **Audience (M11-4, not the recommendation):** shipped with the game for modding: part of the shipped
  folder (M12), able to make a mod, with player-ready looks and error handling.
- **Where it saves (M11-5):** mods for players (a `.pak` in the player's mods folder, which adds or
  replaces entries and can't delete one, M9 Q8/Q30); the game's data shown read-only; only when started
  from the repo (the dev setup) can it also save into the game's packs in `engine/Assets/Packs`.
- **The look (M11-6):** its own plain tool style: a compact dark layout (lists left, the form in the
  middle, the preview right) in the game's font, with its own small stylesheet.
- **Undo and redo (M11-7, not the recommendation):** per entry: each entry keeps its own history; adding
  or removing entries isn't undoable, the editor asks first instead.
- **Importing (M11-8):** an Import button and drag-and-drop, running the packer's importers; the packer
  stays for builds and scripts.
- **Picking files (M11-9):** the editor's own file browser on the engine's UI (folders, files, a path
  field, recent places) and files dropped from the desktop onto the window, which the window learns (the
  engine has no OS file drop yet). The game's "no OS dialogs" rule; the game can reuse the browser later
  for Load Character.
- **The form's rows (M11-10):** a row's name is made from the field's code name ("Max voices" from
  `MaxVoices`); fields can also declare a short help text, a unit and allowed limits in the record's
  definition (a tooltip, a suffix, slider ends), added to the existing kinds bit by bit where players
  need them.
- **References (M11-11):** fields naming another entry by its path are marked in the record's definition
  with the kind they point at; the form offers a list of matching entries and flags a path that doesn't
  exist; renaming or removing an entry lists who refers to it and offers to update them.
- **Invalid values (M11-12):** the engine's own check runs as you edit; the problem is shown at the field
  and in the entry list, the preview keeps the last valid state, and saving refuses and names the invalid
  entries, so a mod made by the editor always loads in the game.
- **Live preview in M11 (M11-13):** sound effects and music themes (play, stop, volume), particle effects
  (looping), models (their clips, a camera you can turn), cutscenes (play, pause, a time slider). State
  machines, UI files and terrain show their form without a preview until later.
- **Imported entries (M11-14):** models, terrain and stylesheets show in the form; single values (names,
  colours) are editable, bulk data shows as a one-line summary ("2,431 vertices") and changes only by
  importing the file again; import replaces the bulk data and asks before overwriting edits made in the form.
- **Mod info (M11-15):** a mod made by the editor carries an info entry inside its pack: name, author,
  version, description, edited in the editor; the game ignores it for now (a mod list can use it later);
  mods without it still load.
- **A pack it can't fully read (M11-16):** damaged, made for a newer version, or with unknown kinds: open
  what is readable, read-only, each unreadable entry listed with its reason; the pack can't be saved over;
  Save As to a new file keeps only what the user chooses.
- **Saving (M11-17):** on command (Ctrl+S or a button); changed entries marked in the list, the title shows
  unsaved work, closing asks first; a save writes the whole pack safely (a new file swapped in), as the
  packer does.
- **JSON (M11-18):** a read-only JSON view of the entry beside the form, with copy and export to a file; a
  JSON file comes back in through Import.
- **The name (M11-19, not the recommendation):** "Statventure Editor", named after the engine; in the repo
  `Tools/Editor`. Its place in the shipped folder and its executable's file name are asked with M12's
  packaging questions.
- **How its screens are tested (M11-20):** a few editor screens (list, form, file browser, dialogs) rendered
  in Chromium from the same markup and compared, at both scales; everything the editor does (editing, undo,
  saving, import, references) tested through its data and UI events; the preview pane's content left out
  of the picture comparison.
- **Language (M11-21):** English, like the game (the user: "English is fine").

Decided earlier: no OS dialogs, file picking is in-game UI (M5, `Docs/Engine-Plan.md`, "User decisions");
packs, records in two forms, versions, the virtual file system's mounts, mods, the hashes the editor
detects changes by, importers beside their areas' code (M9, `Docs/Assets.md`); cutscenes authored in the
editor or in C# (M10-1, M10-10); sound only at volume 0 from scripts and sessions (the standing rule,
`Docs/Engine-Plan.md`, "Small tasks", the proof's sounds).

## What exists (checked against the code, 2026-11-06)

Nothing of M11 yet: no editor project, no public way to read or set a record's fields, no pack update,
no file drop, no close request the application can refuse.

### Records: fields, values, new records (`Engine/Statventure/Records/`)

- **Public:** the attributes (`[Record(name, version)]`, `[Field(n)]`, `[Subtype(tag, type)]`,
  `RecordAttributes.cs`), the serializer (`RecordSerializer`: `ToBinary`, `FromBinary`, `ToJson`,
  `ToJsonUtf8`, `FromJson`, `RootTypeName`, `LayoutText`, `FindTypes`) and `Packs/RecordTypes` (`Engine`,
  `Of(assemblies)`, `ByName`, `Find`).
- **Internal, and all a form needs:** `RecordFamily` (a kind's versions, layouts, upgrades), `RecordLayout`
  (`PlainLayout<T>`: the fields in number order, each a `FieldLayout` with its property, JSON name (camelCase
  of the property), codec, `Required` and `Default`, a compiled getter per field and one compiled factory
  `(values, present) => new T(…) { … }`; `UnionLayout`: the tags and subtypes), `Codecs` (a codec per field
  type). The engine grants internals only to `Statventure.Tests`, `UiBenchmark` and `AudioBenchmark`. **No
  public way to list a kind's fields, read or set one by field, or make an instance**; C#'s `with` can't do
  it generically, and some records are classes (`ModelData`, `ModelPrimitive`), positional records or
  structs (`JointIndices`).
- **Records that can't hold a broken value:** `FCurve` checks its keys, extrapolation and smoothing when it
  is made or read and throws (`Scene/FCurve.cs`, S2-3: "Immutable and always valid"), so an edited curve
  that breaks a rule can't exist as an object. A new record has no value for its required fields (a
  `required` property, a positional parameter without a default, a non-nullable reference without one);
  the layout makes an instance only to learn the defaults, with placeholders.
- **Field kinds** (`RecordCodecs.cs`, `Docs/Assets.md`, "Records (step 2)") and the control each needs:

  | Kind | Where today | Control |
  | --- | --- | --- |
  | bool | `Glow`, `Skippable`, `Hatch` | checkbox |
  | integers (8–64 bit, signed and not) | `MaxVoices`, `Seed`, `Mesh`, `Width` | number field |
  | float, double | `Level`, `Frequency`, `Time` | number field (slider with limits) |
  | string | names, paths, palette slots, scores (`Chords`, `Melody`) | text field (a list for references and choices) |
  | enum (no flags) | `EffectSource`, `BlendMode`, `AudioCueKind` | select |
  | nullable value or reference | `FrequencyTo`, `Filter`, `Reduced` | "set" checkbox plus the value |
  | list (`T[]`, `List`, `IReadOnlyList`) | `Layers`, `Tracks`, `Cues`, `double[][]` partials | rows with add, remove, move |
  | dictionary (string, integer or enum keys) | `ThemeSpec.Sections` | key and value rows |
  | ordered pairs | `LayerSpec.Drums` | ordered key and value rows |
  | record | `Filter`, `Strip`, `Life` | a group, collapsible |
  | union (`[Subtype]`) | `scene.track`, `particle.shape`, `particle.spawn` | a subtype select and its fields |
  | `Vector2/3/4`, `Quaternion`, `Matrix4x4` | `Gravity`, `BaseColor`, node transforms | 2–16 number fields (a colour for colours) |
  | `byte[]` | terrain covers | a summary |

- **Names and help:** the JSON name is the only name; the C# doc comments on many fields ("Hz at the
  layer's start") aren't there at run time. No help text, unit or limit is declared anywhere. Several
  strings are choices checked only when the music is compiled or played (`LayerSpec.Type`: pad, drone, arp,
  bass, pulse, strum, mel, line, drums, hit, tex, drips; `InstrumentSpec.Kind`; `ThemeSpec.Mode`).
- **The kinds a pack's entries are today** (a record entry's kind is its root type, `RecordSerializer.RootTypeName`):

  | Kind | Where | Made by |
  | --- | --- | --- |
  | `audio.effect` (`EffectPatch`) | `audio/effects/<name>` | JSON form; none in a committed pack yet |
  | `audio.theme`, `audio.instrument`, `audio.percussion`, `audio.room` | `audio/themes/<id>` etc., names by the music's rule (S6-1) | `make-music-data.sh` (55, 34, 21, 7 in `audio.pak`) |
  | `audio.theme-order` | `audio/theme-order` | the same (1) |
  | `model` (`ModelData`) | `models/<name>`, stages at `models/stages/<name>` | the models importer (`.glb`) |
  | `animation.state-machine` | anywhere by path | JSON form |
  | `particle.effect` | `models/effects/<name>` | JSON form |
  | `terrain` (`TerrainData`) | the description's path without `.terrain.json` | the terrain importer |
  | `scene.sequence` | `world/cutscenes/<name>` | JSON form |
  | `ui.stylesheet` | `ui/styles/<name>` | the UI importer (`.css`) |
  | files | `ui/fonts/*.ttf`, `*.otf`, licence texts; `ui/images/` (PNG, SVG) | as they are |

  Committed: `audio.pak` (118 record entries) and `ui.pak` (the five font faces and two licence texts).

### Checks per kind

| Kind | The check today | When |
| --- | --- | --- |
| `audio.effect` | `EffectPatch.Problem()`, **internal**, a string | `SoundEffects.Register`, `MusicLibrary.Load` |
| `audio.theme` | `MusicLibrary.Load`: id matching the path, the room there (or `hall`); `Score.Compile` (`FormatException`: a bad degree, chord, note, bar length or mode, a missing section, no chords) | load; **compile only when the first theme is played or asked for** (all at once) |
| `audio.theme` layers | `Composer`: an unknown instrument, percussion voice, layer type or texture, an instrument that makes no sound, a layer without its pattern (`InvalidOperationException`) | **only while the theme plays, on the sound thread** |
| `audio.instrument`, `.percussion`, `.room` | none of their own | (the composer, while playing) |
| `audio.theme-order` | `MusicLibrary.Load`: each id once, each with an entry | load |
| `particle.effect` | `ParticleEffect.Problem()`, public, a string | `Load`, an emitter's start |
| `model` | `ModelData.Validate()` (`InvalidDataException`; `ModelCamera.Problem()`, `ModelLight.Problem()`) | `Load`, the importer |
| `animation.state-machine` | `Validate()` (`ArgumentException`) | `Load`, `StateMachine` |
| `scene.curve` | its constructor and setters (`ArgumentException`) | made, read |
| `scene.sequence` | `Validate()`, `Validate(stages)` (`ArgumentException`, naming track and cue: "the sequence x, track 3 (audio): cue 2 …"); `Load` checks its stages are in the data | load; **its effects, sounds, clips and UI ids only at `Play`** (S4-6, S6-A) |
| `terrain` | `Validate()` (`InvalidDataException`) | `Load` |
| `ui.stylesheet` | `Stylesheet.FromRecord` (`InvalidDataException`); compiling's `Problems` at import | `Load`, import |

The checks say what is wrong in a message, not at which field: four styles (a string, `ArgumentException`,
`InvalidDataException`, exceptions when compiled or played). Only the serializer's own errors carry a field
path (`RecordFormatException.Path`, `$.limbs[1].kind`). For M11-12 the editor needs one check per kind,
callable on a value, with the field where it can, and the checks that today run only at play time.

### References between entries

All plain strings; nothing marks them.

| Field | Points at | Checked |
| --- | --- | --- |
| `Sequence.Stages[]` | models, by path `models/stages/<name>` | `Sequence.Load` (missing: an error) |
| `ParticleCue.Effect` | particle effects, by path `models/effects/<name>` | `Validate`: only the folder; existence at `Play` |
| `AudioCue.Name` | kind `effect`: an effect patch by name (`audio/effects/<name>`); kind `music`: a theme by id (`audio/themes/<id>` by the music's rule); `stopMusic`: nothing | at `Play` (S6-A) |
| `ThemeSpec.Room` | a room by name (`hall` when none) | `MusicLibrary.Load` |
| `LayerSpec.Instrument` | an instrument by name; for `hit` layers a percussion voice | while playing |
| `HarmonySpec.Instrument`, `DoublingSpec.Instrument` | instruments by name | while playing |
| `LayerSpec.Drums` keys | percussion voices by name | while playing |
| `ThemeOrder.Themes[]` | themes by id | `MusicLibrary.Load` |

Names that aren't entries: inside a sequence its roles, stage names, nodes (`<stage>/<node>`), markers and
clips; inside a state machine its states, parameters and the clips of the rig it is bound to; a theme's
sections (`Like`, `Form`); a model's indices; palette slots (material names, particle colours, terrain
covers, `PaletteCue.Fill` is a colour); UI cue element ids (the game's markup). The sequence's check covers
the names inside it.

### Packs (`Engine/Statventure/Packs/`)

- **Writing** (`PackWriter`): entries by content (`Add`, `AddRecord(value)`, `AddFolder`); `Write` keeps the
  stored bytes of every content the pack it replaces already holds (by content hash, after checking them)
  and writes a temporary file, flushed, renamed over the pack. **Missing:** adding a record entry from its
  binary form (`AddRecord` serializes a value in the current versions, so a record of an older version read
  and added again changes) and copying a pack's entries into a writer; so a pack can't yet be rewritten with
  some entries changed and the rest kept byte for byte. No `update` command (`Docs/Assets.md`, step 3, "Later
  needs").
- **Reading what it can** (`PackFile`): `Open` refuses the whole pack when its header or index is wrong
  (magic, a format version other than 1, header flags, the index's hash, a badly formed index): nothing of
  such a pack is readable. Per entry, `Read` refuses a content whose hash doesn't match, and reading a record
  of a kind the given `RecordTypes` don't know, or of a newer version than the engine reads, fails for that
  entry only (`RecordJson`); `PackDump` writes such an entry with its `"problem"` and goes on, the model for
  M11-16.
- **Hashes:** `PackEntry.Hash`/`HashHex` (SHA-256 of the content); the index's hash in the header, checked at
  `Open` but not exposed by `PackFile`. An edited record is changed when its binary form's hash differs from
  its entry's.
- **Files that aren't records:** the fonts and licence texts in `ui.pak`; images later.
- **The virtual file system** (`VirtualFileSystem`): mounts `Pack`, `Folder(at)` and `Mods` (every `.pak`
  directly in the folder, in ordinal file-name order, Q30), the topmost giving a path, no deletes;
  `Load<T>`, `Problem(path, …)` naming the pack and entry, `List`, `Reload`. Its sources are internal: **no
  mount for data held in memory** (the editor's unsaved entries), so the engine's loaders and cross-entry
  checks (`Sequence.Load` with its stages, `MusicLibrary.Load`) can't run on unsaved work.
- **`Application.Data`** (`Core/Application.cs`, `DataMounts`, internal): the area packs `packs/<area>.pak`
  beside the executable, `classes/`, the player's `mods/`, the development folder (`ApplicationOptions`
  or `$STATVENTURE_DEV_FOLDER`). No option leaves the mods out, so the editor can't use it as the game's
  data as it is (it would mount the player's other mods).
- **Folders:** the player's game folder (`GameStorage.Folder`, `ModsFolder`; Linux `$XDG_DATA_HOME/stat-fick/`
  else `~/.local/share/stat-fick/`, Windows `%APPDATA%\Stat Fick\`); the repo's packs `engine/Assets/Packs/`,
  made whole by `make-music-data.sh` and `make-ui-pack.sh`.

### Importers

- Three, found by `[Importer(name, endings)]`: models `.glb` (`ModelImporter`: `GltfImport`, `Validate`),
  world `.terrain.json` (`TerrainImporter`: its two PNGs named relative to its folder, read with
  `ReadSource`), ui `.css` (`StylesheetImporter`: `Stylesheet.Compile`; every `url()` a `data:` url or a file
  of the folder being packed). A record's JSON form packs as the record (S3-1: `$types` first, the kind
  found by name); any other file as it is.
- `ImportContext` has an internal constructor and exists only inside `PackWriter.AddFolder`: a folder on disk
  mirroring the data's paths (`models/hero.glb` → `models/hero`). **The editor can't import one dropped file
  into a chosen path** without building such a folder; nor can a stylesheet's urls be checked against the
  data a mod sees.
- What lives in the tool and not the engine: nothing of substance (`Tools/Packer/Program.cs` is a thin shell).
  The music's import (`make-music-data.sh`, the old scripts in Chromium) and the fonts' renaming
  (`make-ui-pack.sh`) are scripts, not importers: the editor can't run them (they need the old game).

### Record kinds that aren't the engine's

`RecordSerializer.FindTypes`, `RecordTypes.Of` and `Importers.Find` take any assemblies; the packer, the dump
and `AddFolder` default to the engine's. No game assembly exists yet (the sandbox is the game's stand-in);
the port's kinds (items, enemies, spells, shops) will be in the game's code. How they reach the editor is
G3, kept for the port.

### The UI for a tool (`Engine/Statventure/UI/`)

- **There** (M4, M5, as Chromium 152): the DOM from C# (`UiElement`: `Append`, attributes, classes,
  `AddEventListener`, `Focus`; `Markup.SetInnerHtml` and the serializer), stylesheets parsed from text, flex
  and grid, scroll containers with Chromium's scrollbars, hit testing, pointer, mouse, wheel, key, focus,
  `contextmenu`, `dblclick` and clipboard events, pointer capture, tab order and `tabindex`, `inert`, title
  tooltips (Chromium's rules: 500 ms rest, `sv-tooltip`), the top layer, form controls (text, search,
  number with spin buttons, password, email, url, tel; checkbox, radio, range with its thumb dragged; select
  as a menu list with its popup; textarea, resizable; button; `details`/`summary`; progress), editing
  commands with undo, the system clipboard (`UiHost`), `resize`, page zoom (`UiHost.PageZoom`), CSS
  animations and transitions, frame clients, timers.
- **Missing for a dense tool:** a list or tree with selection and keys (the listbox `<select multiple>` or
  `size` has its look, `ControlLook.Listbox`, but no interaction: the input code handles menu lists only);
  `<input type=color>` (its value is sanitized; nothing is painted, no picker); `<dialog>` and `showModal`
  (the game's modals are `aria-modal` divs with `inert`); HTML drag and drop (none: the game drags with
  pointer capture, M5 checklist); splitters, tabs, menus, context menus and a text field with suggestions
  (all buildable from the events and positioned elements); a virtual list (rows only where seen). How fast
  thousands of rows update is unmeasured (`Tools/UiBenchmark` exists).
- **How UI code is written** (`Samples/Statventure.Sandbox`): a `FontLibrary` (faces added from files, or
  `@font-face` through `UiDocument.Files`), a `UiDocument`, `AddStylesheet(css)`, `Markup.SetInnerHtml(body,
  html)`, listeners, a `UiHost(window, gpu, document)` updated and rendered each frame.

### The window (`Engine/Statventure/Platform/`)

- `Window` (GLFW through BRUTAL): keys, characters, cursor, buttons, wheel, enter and leave, focus,
  minimizing, framebuffer size; `Title` can be set; `CloseRequested` is GLFW's should-close flag (settable);
  the clipboard; cursors; full screen. **No file drop:** BRUTAL's `GlfwWindow` has `OnDrop` (paths only:
  GLFW gives no drag-over events and no drop position) and `OnClose`; the engine subscribes to neither.
- `Application.Run` ends its loop once `CloseRequested` is set (checked before each frame, and after the
  update while minimized): **no request the application can refuse** (clearing the flag in `Update` would
  keep it running, but nothing makes that a contract).

### The preview's parts

- **3D in an element:** a `<surface>` element shown with `UiHost.Views.Show(surface, view)`; `SceneView` (a
  frame client: root node, `Rate`, `Camera`, `Sun`, `Play(sequence, bindings, document)`,
  `Animate(instance, stack)`); `Model.Create(gpu, data)`, `ModelInstance` (its cameras and sun), `Actor`,
  `SceneEmitter(effect)` with `Seed`, `Burst`, `Rate`, `Duration`, `Palette`; `SurfaceViews.Pick` for the
  pointer in 3D.
- **Cutscenes:** `Play` checks everything first and throws (S4-6): every role bound once to something of its
  kind, every stage and effect bound, the audio names registered and themes there (S6-A), actors' clips from
  the bindings' clip source, UI ids in their hosts. The record names roles, not what plays them: the actors'
  figures and clip libraries (S7-5), the UI hosts' markup, palettes and node roles are the game's. A preview
  must bind stand-ins or leave tracks out. `SequencePlayer`: `Pause`, `Resume`, `Seek`, `Rate`, `Skip`,
  `Finish`, `Time`, `Duration`.
- **Sound:** `AudioSystem(MusicLibrary)` on OpenAL: `Music` (`Play`, `Stop`, `PlayAt`, `StopAt`, `Volume`,
  `Themes`, `Library`), `Effects` (`Register`, which replaces, `Play`, `PlayAt`), `MasterVolume`,
  `EffectsVolume`, `Pause`, `Resume`. The library is fixed when it is made: the swap Reload uses
  (`AudioSystem.Reload`, S9-D) and `MusicLibrary`'s constructor are **internal**; `MusicLibrary.Load(data)` is
  public. Themes play cleanly only from a Release build (`play-cutscene.sh`: Debug underruns).
- **Updating as a value changes:** an effect patch is registered again; a particle effect needs a new
  emitter; a model a new `Model` (an upload); a cutscene a new play (`Finish`, its stages removed, `Play`,
  `Seek` to where it was); a theme a new library and the swap.

### A second app on the engine

The sandbox (`Samples/Statventure.Sandbox`, an `Exe` referencing the engine, copying the fonts, the
proof's data and `audio.pak` into its output) is an `Application` (`Load`, `Update`, `Render`, `Unload`;
`ApplicationOptions`: window, VSync, log file, sound, development folder, game folder). `Statventure.slnx`
has the engine, the sandbox, the tests and the tools (`ShaderCompiler`, `UiBenchmark`, `AudioBenchmark`,
`PlayTheme`, `Packer`) under `/Tools/`; `Tools/Editor` goes there. `build.sh` builds the solution and
publishes only the sandbox into `$STATVENTURE_GAME` with `packs/`; run scripts build Release and run the
program (`play-cutscene.sh`, `play-theme.sh`). One test project (`Tests/Statventure.Tests`, xunit), run by
`test.sh`; its `[ModuleInitializer]` points the run's per-user folders at a scratch folder ("Decision on test
runs and the player's folders").

### UI references from Chromium

Paint fixtures are C# records (name, size, CSS, HTML; `Tests/.../Ui/PaintFixtures.cs`) the test run writes
to `Reference/paint-fixtures.json` and `.js`. At 1× the old game's test client screenshots them
(`make-paint-references.sh`); at any scale `Tools/PaintReference` does under X11
(`make-scaled-paint-references.sh <scale> <page zoom> <fixture list>`: any list in `Reference/`, fonts from
`old-game/objects/fonts`), into `Golden/ui-<name>.png` and `@1.65`; `PaintReferenceTests` compares at 12
levels and 0.2 % of pixels. `Tools/GameReference` drives the old game's own screens (not for the editor).
Captures need Chromium: the PC, or the cloud under Xvfb.

### Files and folders

`Storage/FolderListing` (`List(folder)`: folders and files, hidden ones flagged, ordinal order; `Roots()`),
`DocumentsFolder.ForThisMachine()` (Electron's rules), `GameStorage.ModsFolder`, `OpenInFileManager`: what
M9 left for the port's file picker (Q19).

## Method

- **Screens against Chromium 152 (M11-20):** the editor's list, form, file browser and dialogs, each in a
  fixed state, are written out by a test as a fixture (the editor's stylesheet and its document's markup,
  as `PaintFixtures` are), screenshotted by `Tools/PaintReference` at 1 and 1.65, and compared with the
  engine's paint of the live document at the paint fixtures' limits (12 levels, 0.2 %). The preview pane is
  left out (P36). A difference is a rule to find, as everywhere.
- **Behaviour through data and UI events:** tests drive the editor's document with the UI's input (pointer,
  keys, text, as `UiInputTests` do) and check the session's data: drafts, histories, problems, references,
  the packs it writes (read back as the game reads them, mounted in `Application.DataMounts`' order), the
  imports (equal to packing the same files), the JSON exported. No window is needed except where the
  window itself is tested.
- **The engine's additions as M9 tested its own:** round trips (every field kind read into a draft and built
  back to the same binary form), each check's rule broken once, packs rewritten byte for byte, the custom
  mount against packs and folders, imports of one file against `AddFolder`.
- **Sound at volume 0** only; the preview's 3D is checked for what it shows (the right entries, cameras and
  times), not against Blender: it reuses the scene view M10 checked.
- **The player's real folders are never written.** The editor's settings folder (`editor/` in the per-user
  game folder, G6), the mods folder it saves into and the Documents folder it exports to are found through
  the same per-user folders the game uses, which the test run points at a scratch folder of its own
  (`Tests/Statventure.Tests/Infrastructure/PerUserFolders.cs`, "Decision on test runs and the player's
  folders"); tools and scripts that run the editor in a check pass a game folder of their own
  (`ApplicationOptions.GameFolder`). `mutate.py` flags any write to the real game folder or the Documents
  folder's top level.
- Every step ends as `Docs/Engine-Plan.md`, "How each step is finished", says: the full suite on the GPU and
  on lavapipe, each new test shown able to fail (`mutate.py`), nothing written to the player's real folders.
- **Full runs are serialized across workers with a lock**, not by waiting for the manager: the full suite runs
  as `flock ~/.cache/stat-fick-build/claude-work/full-run.lock engine/Tools/Scripts/test.sh` (from the repo's
  root; the same lock for the lavapipe run, with its variables before `flock`'s command, and for
  `Tools/Scripts/mutate.py`), which waits its turn and frees the lock when the run ends. Filtered test runs in a
  worker's own worktree need no lock.

## Design

The design the steps work from, decided: **(P…)** points to the manager's decisions, **(G…)** to the game's
questions and their outcomes, both below.

### The program

- **Statventure Editor** (`Tools/Editor`; the executable's file name and place: M12, M11-19): an
  `Application` with one window showing the editor's document through a `UiHost`. Three panes (M11-6): the
  entry list left, the form in the middle (a Form and a JSON tab, M11-18), the preview right, with
  splitters; a top bar (New mod, Open, Save, Save As, Import, Undo, Redo), a status line, dialogs as
  overlays with the rest `inert`. Text in English (M11-21).
- **One pack open at a time (P6):** a mod (from the player's mods folder by default), or in the dev setup
  (P5) one of the game's packs in `engine/Assets/Packs/` (M11-5). The game's own data, its packs, is the
  base the open pack is seen over: its entries are listed and open read-only; editing one inside a mod makes
  the mod's own copy (P7).
- **The session** (no window; what the tests drive, P2): the base (a `VirtualFileSystem` of the game's packs
  other than the open one), the open pack (its file, its index hash when opened, its entries), and per entry
  a working entry: where it came from (path, kind, content hash), its draft (P10), its history (M11-7), its
  problems; entries added, removed and renamed. **The data as the game would see it after saving** is the
  base with a custom mount on top (step 2, P38) giving the open pack's entries as edited: the engine's own
  loaders and cross-entry checks run on it unchanged (M11-12).
- **Kinds:** record kinds from the engine's assembly and any assemblies the editor is given (the packer
  likewise), so every option of G3 stays open for the port; each kind's rows, check, references, bulk fields, help, units and limits come from its
  definition (step 1's marks; M11-2: no editor code per kind). Files (fonts, images, texts) are entries of
  their own kind by their ending.

### Records: shapes, drafts and marks (step 1)

- **Shapes:** `RecordShape.Of(type)` (working name) describes a kind: its record name and version, display
  name, folder and name rule (P9), and its fields: number, code name, JSON name, display name ("Max voices"
  from `MaxVoices`: words split at capitals, the first capitalized), value shape (number kind, bool, string,
  enum with its members, nullable, list, dictionary, pairs, record, union with its subtypes, vector,
  quaternion, matrix, bytes), required, default, and its marks. Read from the layouts the serializer already
  builds; nothing about the binary or JSON forms or the layout signatures changes.
- **Drafts (P10):** a draft is a typed tree of field values: read from a record through the compiled
  getters, built into a record through the compiled factories, bottom up. Building reports the field path
  of what can't be built (a required field empty, a null where none is allowed, an `FCurve` refusing its
  keys) as a problem at that field. Large values (bulk lists, byte arrays) stay the original objects, so a
  2 MB model isn't copied on every edit. A blank draft of a kind holds its defaults, required fields empty.
- **Marks** (attributes in the record's definition, M11-10, M11-11; none changes the data or the layout
  signatures): `Help` (a short text, the row's tooltip), `Unit` (a suffix: "Hz", "s", "m", "°"), `Limits(min,
  max)` (slider ends, and part of the engine's own check: the game refuses data outside them too, G4),
  `Choices` (allowed strings, a select; only what the engine already accepts),
  `Colour` (a vector or string shown as a colour), `Bulk(summary)` ("{0} vertices", P21), `References`
  (the kind and folder it points at, the name rule, and a condition on another field: `AudioCue.Name` is an
  effect's name when its kind is `effect`, a theme's id when `music`), and on the kind `Kind(display name,
  folder, name rule)` (P9).
- **Checks' shape (P20):** a problem is a field path (`$.layers[0].length`, as `RecordFormatException.Path`
  writes it; empty for the whole entry) and a message. A kind's check is a static method marked on the
  kind and found with it, given the value and the data (for cross-entry checks), so the port's kinds bring
  theirs without editor code. Generic checks from the marks: limits, choices, references that don't
  resolve.

### The engine's kinds: checks and marks (steps 5 and 6)

- Every kind of the table above gets its check through the step 1 shape, with field paths: the existing
  checks keep their messages (the game's logs don't change) and gain the path of what they name.
- New checks for what today fails only when it plays (step 5, audio): a theme against the data (its score
  compiled: chords, melody, notes, bar lengths, mode, sections; each instrument and percussion voice its
  layers name, by the composer's rules for each layer type; its room), an instrument that makes no sound,
  the theme order; (step 6) a sequence's effects and sounds against the data, as `Play` checks them.
  The game's loaders run them too (G5), once the step has measured the added loading time (the
  condition is in steps 5 and 6).
- Marks on the engine's kinds: every reference of the table above; the bulk fields of models (meshes,
  skins, channels, the hierarchy's indices and transforms), terrain (samples, covers) and stylesheets (the
  rules); the strings with choices (layer types, instrument kinds, modes); colours (`BaseColor`, `Emission`,
  `PaletteCue.Fill`); help texts, units and limits first for the kinds with a preview (effects, particle
  effects, cutscene cues), the rest bit by bit (M11-10). In M11 a limit is declared only where the game's
  checks already enforce it today (G4), so nothing that loads today stops loading; a new limit is a question
  for the user.

### Packs for the editor (step 2)

- **Rewriting a pack (M11-17, P23):** a writer is filled from an open pack, entry by entry as it is (record
  flag and content, a record's binary form kept as stored, so an older version stays in its version), and
  the edited, added and renamed entries replace or join them; `Write` then keeps every unchanged entry's
  stored bytes (its existing reuse) and swaps the new file in. Save As (M11-16) is the same with a chosen
  subset.
- **A custom mount (P38):** a public mount whose entries an object gives (paths, record or file, the bytes),
  so the editor's unsaved entries sit over the base and the engine's loaders read them as from a pack.
- **One file imported (P28):** an importer run on one source file for a chosen entry path, with a lookup for
  the files it reads besides (a terrain's images beside its description on disk) and for the data paths
  that exist (a stylesheet's urls checked against the data the mod sees, not a folder).
- **`packer update <folder> <file.pak>`** (`Docs/Assets.md`, "Later needs"): the folder's entries added to or
  replacing the pack's, the rest kept; the scripts that make whole packs switch to it later (P30).
- **The index's hash** of an open pack exposed, so a save can tell whether another program changed the file
  since it was opened (P23).

### The window, the sound and the file browser's model (step 3)

- **Files dropped on the window:** GLFW's drop callback (BRUTAL's `OnDrop`) becomes an event of the window
  with the paths (no position, no drag-over: GLFW has neither); the game doesn't listen, so nothing changes
  for it.
- **A close the application can refuse:** `Application` asks a method the game can override before it ends
  the loop (by default yes, as now); the editor answers no while it shows its "unsaved changes" dialog, and
  closes when the user chooses.
- **A new music library:** the swap S9-D made for Reload (the theme playing restarts through the usual fades
  when its music changed) public, so the preview plays an edited theme.
- **The file browser's model (P26):** places (the mods folder, Documents, home, the roots), recent places kept
  in a list the caller stores (the editor keeps it in its `editor/` folder, G6), a folder's entries sorted and filtered by
  endings, and the path field's reading (a typed path made full, a folder or a file, completion of its last
  name), beside `Storage/FolderListing`, so the game's Load Character can reuse it with its own screen.

### The shell (step 4)

- Built as P1–P4 say: the session and its view apart, parts registered so later steps add their own files,
  widgets of the editor's own on plain elements, its stylesheet embedded, the game's Lato from `ui.pak`.
- Keys, handled by the shell and given to the parts as they come (steps 8–13): Ctrl+S save, Ctrl+Shift+S save
  as, Ctrl+O open, Ctrl+Z and Ctrl+Y (Ctrl+Shift+Z) undo and redo (P13), Ctrl+F the list's filter, F2 rename,
  Delete remove (asks), Ctrl+= and Ctrl+- zoom; the title "Statventure Editor — <pack>" with a "•" while
  there is unsaved work (M11-17).

### Opening packs (step 8)

- **The file browser (M11-9, P26):** places (the mods folder, the game's packs: read-only, the Documents
  folder, home, the roots, recent places), a folder's listing (folders first, then files with size and date,
  hidden ones only when asked), a path field (typing a folder goes there, a file opens it), filtered by what
  is being picked (`.pak` to open; the importable endings to import; any to export). Drops (P27).
- **Opening:** `PackFile.Open`; every entry read and classified: a record of a known kind, a file, or
  unreadable with its reason (a damaged content; a kind the editor doesn't know; a newer version; a record
  that doesn't read). Any unreadable entry makes the pack read-only, its list showing each with its reason
  (M11-16, P24); a pack `Open` refuses shows the reason only.
- **The entry list (P8):** a tree by path: the open pack's entries over the game's (the game's dimmed,
  read-only), marks for changed (•), added (+), the mod's own copy of a game entry, problems (!) and
  unreadable entries; a filter; keys (arrows, Home, End, type-ahead, Enter to open).

### The form (step 9)

- **Rows:** one per field, in number order: the display name (help text as its tooltip), the control (P11),
  the unit after it; records as collapsible groups; lists with add, remove and move; unions with their
  subtype; read-only for the game's entries and bulk fields (their summary, P21).
- **Values:** number fields take text and commit on Enter, leaving the field, or a step (P12); a value that
  can't be parsed stays in the field, marked, and isn't committed; every commit makes a new draft, one step
  in the entry's history (P13).
- **New entries (P15)** from the kind's blank draft or a copy; **rename and remove** ask first (M11-7, P14).
- **The JSON tab (M11-18, P22):** the entry's JSON form (`RecordSerializer.ToJson`) of the draft when it
  builds, else of the last one that did (said so above it); copy to the clipboard, export through the file
  browser.

### Checks and references in the editor (step 10)

- After each commit the entry's check runs (its kind's, the marks', the draft's build); cross-entry checks
  (against the data through the custom mount) shortly after the last change (P17). Problems show at their
  field and at the top of the form, the list marks the entry (P16).
- **References (M11-11):** a referencing field is a text field with a list of matching entries (base and
  pack, by the mark's kind and folder, names by its rule); one that doesn't resolve is a problem that refuses
  saving (P18). Renaming or removing an entry lists its referrers in the pack and offers to update them all
  in the same step (P19).

### Saving and mods (steps 7 and 11)

- **Save (M11-17, P23):** refused while any entry has a problem, naming them (M11-12); otherwise the whole
  pack written as step 2 says; before writing, the file's index hash compared with the one read at open, and
  if another program changed it, the editor asks (P23). Saved entries lose their marks; the title loses "•".
  Closing with unsaved work asks: save, discard, cancel.
- **Mods (M11-5, M11-15):** New mod asks the mod's name and file name (P25) and writes into the player's mods
  folder; the info entry (name, author, version, description) is edited in a form of its own and saved as
  `mod/info`, a record of the engine's kind `mod.info` (G1; step 7). In the dev setup the game's packs save in
  place.
- **Save As (M11-16):** a new file with the entries the user ticks; for a pack that can't be fully read, the
  readable entries (unknown kinds kept byte for byte, P24).

### Import (step 12)

- **Import (M11-8, M11-14, P28):** the button and drops; `.glb` models, `.terrain.json` terrain (its images
  beside it), `.css` stylesheets, record `.json` files (their kind from `$types`; M11-18's way back in) and
  loose files (`.ttf`, `.otf`, `.png`, `.svg`, `.txt`) as file entries. The entry's path is suggested from the
  kind's folder and the file's name made to fit the path rule, and can be changed; an entry already there is
  replaced after asking.
- **Importing again (M11-14, P29):** the bulk data comes from the file; the single values that differ
  between the file and the entry's present values (names, colours, flags edited in the form) are listed, and
  the user keeps or takes each.

### The preview (step 13)

- **Sound effects (P31):** Play (and Space), Stop, the editor's volume; the play's own volume, pan and pitch;
  the patch registered again after each valid change. **Themes:** Play, Stop, volume; the library made from
  the data as edited (`MusicLibrary.Load` on the custom mount) and swapped in (step 3), the theme restarting
  through its fades when its music changed. Instruments, percussion voices and rooms have no preview of
  their own; a playing theme takes their edits.
- **Particle effects (P32):** an emitter at the origin, looping (a burst and a stream, started again when the
  last particle has died), a seed, the preview's palette.
- **Models (P33):** the model in a scene view, its clips (play, pause, loop, a time slider), a camera turned
  by dragging and zoomed by the wheel (a reset), and a stage's own cameras to look through.
- **Cutscenes (P34):** Play, Pause, a time slider (`Seek`); the stages and effects from the data; roles bound
  to stand-ins chosen per cutscene in the preview, the tracks that can't be bound left out of a preview copy
  of the sequence (the record itself untouched).
- **Last valid state (M11-12, P35):** the preview takes an entry only when it builds and its check passes;
  otherwise it keeps the last valid one, saying so; a cutscene goes on from the same time.

### Decided by the manager (P1–P38)

The editor's own choices, decided by the manager on 2026-11-06 under the user's rule for M11 (editor choices
at the manager's discretion), every one as recommended: the option marked "(a, decided by the manager,
2026-11-06)", with its reason, then the options not chosen. The design above rests on them.

**The program**
- **P1. Project, tests and scripts.** (a, decided by the manager, 2026-11-06) One program project
  `Tools/Editor/Editor.csproj` (namespace `Statventure.Editor`, window title "Statventure Editor"), its tests in
  the existing test project (`Tests/Statventure.Tests/Editor/`, a project reference), a run script
  `Tools/Scripts/editor.sh` (Release, like `play-cutscene.sh`); not published into `$STATVENTURE_GAME` until M12
  decides its place. Why: `test.sh`, `mutate.py` and the full run stay as they are. (b) An editor library plus a
  thin program. (c) A test project of its own (`test.sh` grows a second run).
- **P2. Code structure.** (a, decided by the manager, 2026-11-06) A session (the data, no window, no DOM) and a
  view (the document) apart; the shell finds its parts (browser, list, form, checks, saving, import, preview) as
  classes of the editor's assembly, so each step adds its own folder and files without editing shared ones. Why:
  the tests drive the session and the document without a window, and workers can run side by side. (b) One
  application class with partial files. (c) A part list in the shell each step edits.
- **P3. Widgets.** (a, decided by the manager, 2026-11-06) The editor's own list, tree, tabs, splitters, menus,
  dialogs, colour field and suggestion field on plain elements (roles, `tabindex`, its own key handling), using
  the engine's real controls where HTML has them (text and number fields, checkbox, select menu, range). Why:
  the listbox and the colour input don't work yet, and plain elements compare with Chromium as they are. (b)
  Only HTML's own controls (a `select size` list, `details` trees), which first needs the listbox's interaction
  ported. (c) A widget library in the engine for the game too.
- **P4. The look.** (a, decided by the manager, 2026-11-06) The old game's own dark-mode colours (`styles.css`:
  `--paper` `#1a1c1a` for the background, `--paper-deep` `#292c28` for panes, `--line` `#3a3d38`, `--ink`
  `#eeeae1` for text, `--muted` `#aaa9a1`, `--ember` `#d77a50` as the one accent), Lato 13 px from the game's
  `ui.pak` (`@font-face` through `UiDocument.Files`), rows 24 px; Ctrl+= and Ctrl+- zoom the page (remembered,
  G6). Why: a plain dark look the game already has, tied to it by its font and colours. (b) Neutral greys with a
  blue accent, as common tools. (c) Neutral greys with the game's ember accent only.
- **P5. Where the game's data is, and the dev setup.** (a, decided by the manager, 2026-11-06) `--repo <engine
  folder>` (what `editor.sh` passes) means the dev setup: the game's packs are `engine/Assets/Packs/` and can be
  opened and saved in place; without it the editor reads `packs/` beside itself (where M12 puts it, or `--game
  <folder>`) read-only. Why: explicit, and a player's editor can't write the game's packs by accident. (b) The
  dev setup detected when `engine/Assets/Packs` is found above the program. (c) A setting.
- **P6. What it has open.** (a, decided by the manager, 2026-11-06) One pack at a time, over the game's packs
  only (not the player's other mods, not `classes/`, which aren't records until the port). Why: the checks then
  match a game with just this mod, and mods can't depend on each other (M9). (b) Several packs at once. (c) Over
  the installed mods too.
- **P7. A game entry inside a mod.** (a, decided by the manager, 2026-11-06) Editing it makes the mod's own copy
  on the first change ("Revert to the game's" removes the copy); game entries can't be renamed or removed in a
  mod (mods can't delete, Q8). Why: overriding is what a mod does, without a step in between. (b) An explicit
  "Override in this mod" first. (c) Only the mod's own entries editable.
- **P8. The entry list.** (a, decided by the manager, 2026-11-06) A tree by path with the kind's name beside
  each entry, a filter, and marks for changed, added, overriding, problems and unreadable. Why: paths are the
  data's own order (folders by area and kind). (b) Grouped by kind, flat. (c) One flat sorted list.
- **P9. Kinds' names and folders.** (a, decided by the manager, 2026-11-06) Marked on the kind in its definition
  (`Kind("Sound effect", folder "audio/effects", name rule)`), else made from the code name. Why: the port's
  kinds bring theirs, no editor table. (b) Code names only, and folders asked every time. (c) A table in the
  editor.

**Editing**
- **P10. Drafts.** (a, decided by the manager, 2026-11-06) A typed tree of field values built from and into
  records through the layouts' getters and factories, large values kept as the original objects. Why: holds what
  a record can't be (an empty required field, a curve breaking its rules) with its path, and is fast on large
  imported entries. (b) The JSON form as the draft: simple, but a compiled stylesheet's JSON is 12.5 MB,
  rewritten on each edit. (c) Record objects: can't hold an invalid state.
- **P11. Controls per field** (the table in "What exists"). (a, decided by the manager, 2026-11-06) As that
  table: number fields, checkboxes, selects, rows for lists and maps, collapsible groups, a subtype select for
  unions (switching keeps the values of fields with the same name), 2–4 number fields for vectors, a colour
  field (swatch and `#rrggbb`) for colour marks, matrices and quaternions read-only (only imported models have
  them). Why: every kind edited the same way, no per-kind code. (b) A JSON text box for anything nested. (c)
  Text fields everywhere.
- **P12. Typing numbers.** (a, decided by the manager, 2026-11-06) The invariant form (a point, shortest
  round-trip text: `0.1` for a float 0.1), committed on Enter, leaving the field, a spin or slider step; text
  that doesn't parse stays, marked, not committed. Why: one step per change, no half-typed values in the
  history. (b) Committed on every keystroke. (c) A separate OK per field.
- **P13. Undo steps.** (a, decided by the manager, 2026-11-06) One step per commit (a slider drag is one step);
  while a text field has typing to undo, Ctrl+Z undoes that first (the field's own editing undo), then the
  entry's history. Why: as text editors do. (b) Every keystroke a step. (c) The entry's history only.
- **P14. Rename.** (a, decided by the manager, 2026-11-06) Like adding and removing (M11-7): not undoable, asks
  first, updates the referrers the user agrees to in the same step. Why: one rule for what changes the pack's
  set of entries. (b) A step in the entry's history. (c) A history of renames for the pack.
- **P15. New entries.** (a, decided by the manager, 2026-11-06) New (pick a kind and a name in its folder; the
  blank draft, its empty required fields shown as problems) and Duplicate. Why: both are needed, templates are a
  later convenience. (b) Templates per kind. (c) Duplicate only.
- **P16. Where problems show.** (a, decided by the manager, 2026-11-06) Under the field's row (red line and
  text), problems without a field at the form's top, the list's mark, and the save refusal naming the entries.
  Why: M11-12 asks for the field and the list. (b) A problems panel only. (c) Tooltips only.
- **P17. When checks run.** (a, decided by the manager, 2026-11-06) An entry's own check after each commit;
  cross-entry checks (stages, the music library) 200 ms after the last change. Why: loading stages and compiling
  themes on every key would lag. (b) Everything on every keystroke. (c) A Check button.
- **P18. A reference that doesn't resolve.** (a, decided by the manager, 2026-11-06) A problem: refuses saving,
  as everything the engine follows must exist at load or at play. Why: M11-12's "always loads" includes playing.
  (b) A warning only. *Known limit:* since one pack is open over the game's packs only (P6) and a reference that
  doesn't resolve refuses saving, a mod that refers to another mod's entries can't be saved by the editor in
  M11; opening over the installed mods (P6's option (c)) would lift it later.
- **P19. Referrers on rename or remove.** (a, decided by the manager, 2026-11-06) The list of entries in the
  pack referring to it, with "update all" (or leave them, which then shows their problems). Why: a mod's
  references can only be in its own entries and copies. (b) Always update. (c) Never update, only list.
- **P20. Checks with field paths.** (a, decided by the manager, 2026-11-06) Every existing check reports the
  path of what it names (the messages unchanged), found through a static method marked on its kind. Why: M11-12
  shows problems at the field, and the port's kinds work the same way. (b) Paths only for the kinds with a
  preview, the rest at the entry. (c) A table of checks in the editor.
- **P21. Bulk fields.** (a, decided by the manager, 2026-11-06) Marked in the definitions with their summary
  ("{0} vertices"): models' meshes, skins, channels and the hierarchy's indices and transforms; terrain's
  samples and covers; stylesheets' rules. Why: the same place as M11-10's marks; the hierarchy and transforms
  stay as imported so a model can't break. (b) A rule in the editor (lists of numbers or vectors, byte arrays).
  (c) Per-kind lists.
- **P22. The JSON view.** (a, decided by the manager, 2026-11-06) A tab beside the form (Form | JSON), showing
  the last valid version when the entry doesn't build. Why: the form keeps its width. (b) Below the form. (c) A
  window of its own.

**Saving, opening, importing**
- **P23. Saving's rules.** (a, decided by the manager, 2026-11-06) Unchanged entries kept byte for byte (an
  older version stays in it), edited ones written in the current versions; a file changed on disk since it was
  opened asks: overwrite, Save As, cancel. Why: a save changes only what was edited, and nothing is lost
  silently. (b) Everything rewritten in the current versions. (c) Overwrite without asking.
- **P24. Save As from a pack it can't fully read.** (a, decided by the manager, 2026-11-06) Readable entries
  offered (ticked), unknown kinds kept byte for byte, damaged ones not offered. Why: M11-16 keeps what the user
  chooses, and an unknown kind is still intact data. (b) Only entries the editor can read as records or files.
- **P25. A mod's file name.** (a, decided by the manager, 2026-11-06) Suggested from the mod's name under the
  path rule (`my-mod.pak`), editable; the dialog says mods load in file-name order. Why: the name the player
  sees and the file match. (b) The name itself, fixed. (c) Any folder and name.
- **P26. The file browser's place.** (a, decided by the manager, 2026-11-06) Its model (listing, places, recent
  places, the path field's reading and completion) in the engine beside `FolderListing`, its screen and look in
  the editor. Why: M11-9 says the game reuses it for Load Character with its own look. (b) All in the editor.
  (c) All in the engine, looks included.
- **P27. Drops.** (a, decided by the manager, 2026-11-06) A dropped `.pak` opens (asking to save first);
  importable files import into the open pack, in the order dropped; no highlight while dragging (GLFW reports
  only the drop). Why: what each file is for. (b) Drops only import. (c) Drops only open.
- **P28. Import's paths and urls.** (a, decided by the manager, 2026-11-06) The entry's path suggested from the
  kind's folder and the file name, editable; a stylesheet's urls checked against the data the mod sees (base and
  pack). Why: a mod's stylesheet points at the game's images too. (b) Paths typed each time. (c) Urls only
  within the files imported together.
- **P29. Importing again over form edits.** (a, decided by the manager, 2026-11-06) The differing single values
  listed, keep or take each (default keep). Why: M11-14 asks before overwriting, value by value is clearest. (b)
  One question for all. (c) Always take the file's, after one warning.
- **P30. The repo's script-made packs.** (a, decided by the manager, 2026-11-06) `make-music-data.sh` and
  `make-ui-pack.sh` keep packing whole until the first editor-made entry is committed into their pack, then
  switch to `packer update` (which replaces only what they make). Why: no change before it's needed. (b) Switch
  now. (c) Never.

**Preview and tests**
- **P31. Sound.** (a, decided by the manager, 2026-11-06) Play and Stop (Space), the editor's master volume
  (remembered, G6), the play's volume, pan and pitch for effects; a theme restarts with its fades after a
  change; no preview for instruments, percussion voices and rooms (a playing theme takes their edits). Why:
  M11-13's list, nothing more. (b) Also a note keyboard for instruments. (c) Effects only.
- **P32. Particles.** (a, decided by the manager, 2026-11-06) A looping emitter at the origin (a burst of 20 and
  20 a second for two lives, started again when the last has died), a seed and restart, a preview palette with a
  distinct colour per slot named (editable, not saved in the pack). Why: an effect's own record holds no emitter
  or palette. (b) A single burst on a button. (c) M6's grey only.
- **P33. Models.** (a, decided by the manager, 2026-11-06) Clips listed (play, pause, loop, time slider), a
  turnable camera (drag, wheel, reset), a stage's cameras to look through, the model's materials in their own
  colours. Why: M11-13. (b) No stage cameras. (c) A still model only.
- **P34. Cutscene stand-ins.** (a, decided by the manager, 2026-11-06) Per cutscene, kept by the editor (G6),
  not in the mod: each actor role a model and clip library chosen from the data, each palette role a palette,
  element roles an empty host holding the ids the UI cues name (styled by a stylesheet from the data if chosen),
  node roles empty nodes; tracks of roles left unbound are left out of a preview copy of the sequence. Why: the
  record names roles, not what plays them, and `Play` refuses what isn't bound (S4-6). (b) Only stages, cameras,
  light, particles and sound previewed. (c) Default stand-ins named in the sequence record (a change to the
  game's format: would be a question for the user).
- **P35. Updating.** (a, decided by the manager, 2026-11-06) A valid change replaces the preview's entry at once
  (a cutscene from the same time, a theme through its fades); an invalid one keeps the last valid state, saying
  so. Why: M11-12. (b) From the start on every change. (c) A Refresh button.
- **P36. The Chromium screens.** (a, decided by the manager, 2026-11-06) Six fixtures, each at 1 and 1.65: the
  empty shell, the list with every mark, the form with every control kind and a problem, the file browser, the
  unsaved-changes dialog, the JSON tab; written by a test from the live document (properties such as a field's
  value written as their attributes, the preview's `surface` replaced by an empty box of its size, `@font-face`
  left to the harness's fonts), captured by `make-editor-references.sh` (PaintReference with an
  `editor-fixtures.json`). Why: M11-20's "a few screens", the same tool as the paint fixtures. (b) Fewer: the
  list and the form. (c) Every dialog too.
- **P37. Long lists.** (a, decided by the manager, 2026-11-06) Measure a pack of 2,000 entries with
  `UiBenchmark` first; rows only where seen if it's slow. Why: build what's needed. (b) A virtual list from the
  start. (c) Never.
- **P38. The custom mount.** (a, decided by the manager, 2026-11-06) A public interface for a mount's entries
  (paths, record or file, bytes, origin) the editor implements over its session. Why: the engine's loaders read
  unsaved work unchanged. (b) A memory mount filled with every entry's bytes on each change. (c) Writing a
  scratch folder and remounting it.

### The game's questions (G1–G6)

These touch what the game does, reads or ships. Each with its options in plain words (the recommended one
first) and its outcome (2026-11-06): G4 and G5 answered by the user with the question tool; G1 and G6
decided by the manager (G1 follows from the user's M11-15, G6 is the editor's own settings); G2 and G3 not
asked now, since no M11 step waits for them, kept for the port (`Docs/Engine-Plan.md`, "Open questions for
later milestones", "At port time").

- **G1. A mod's name, author, version and description (M11-15): where the editor keeps them in the mod file.**
  The game doesn't show them yet.
  (a) As one small entry at a fixed place in every mod (`mod/info`), of a kind the engine knows (`mod.info`:
  the four as text). The game loads mods as now and never reads it; a mod list in the game can read it later
  from each mod file. *Recommended:* nothing changes for players now, and the game's own tools (the pack dump,
  the packer) can show it. (b) The same entry, its kind known only to the editor: the game never reads it, but
  a later mod list needs the kind moved into the engine first. (c) In the mod file's header instead of an
  entry: a change to the pack format (version 2) every reader of packs must then know.
  **Outcome: (a), decided by the manager as following from M11-15** (the user chose an info entry inside the
  mod's pack that a mod list in the game can use later): `mod/info`, a record kind the engine knows; the game
  never reads it for now. Built by step 7, used by step 11.
- **G2. A mod with parts the game can't read** (made by a newer editor or by hand: a kind the game doesn't
  know, or a newer version of one; the editor itself never saves one, M11-12, M11-16).
  (a) As today: the game loads the mod, and only when it uses such a part does it stop with an error naming
  the mod and the part (M9's rule for damaged data). *Recommended:* nothing changes for players, and the error
  names the mod at fault. (b) When it starts, the game checks every part of every mod and stops at once,
  naming the mod and the part. (c) When it starts, it writes a warning naming such parts in its log and goes
  on as today.
  **Outcome: not asked now, kept for the port** (where mods and the game's mod list are asked); the game stays
  as today (a), and no M11 step changes it.
- **G3. How the game's own kinds of data (items, enemies, spells, shops, once the port makes them) reach the
  editor** (M11-2: editable with no new editor code).
  (a) The port keeps them, with their rules, in a data library of their own (one file shipped beside the
  game) that the game and the editor both load; the editor and the packer find the kinds in it. *Recommended:*
  the rules are written once, the editor never runs the game's own code, and modders get the game's own
  checks. (b) The editor loads the game's program file itself to find them. (c) They go into the engine
  with its own kinds.
  **Outcome: not asked now, kept for the port** (it decides how the port lays out its code and adds a file to
  the shipped folder, M12). No M11 step waits: the editor and the packer take record kinds from any assembly
  they are given, so every option stays open.
- **G4. Limits on values** (M11-10's "allowed limits", e.g. a sound's level from 0 to 1): **does the game
  also refuse data outside them?**
  (a) Yes: a declared limit is part of the engine's own check, so the game refuses data outside it when it
  loads (naming the mod and entry), as it refuses a sound effect without layers today; M11 declares only
  limits today's checks already hold, and any new limit is asked first. *Recommended:* one rule for the
  editor and the game. (b) The editor only: the game loads values outside them as today. (c) Slider ends
  only: the editor's slider stops there, but any value can be typed.
  **Outcome: (a), the user's answer (2026-11-06, the question tool):** the game enforces declared limits too;
  one truth for the editor and the game: a mod made by hand with a value outside a limit is refused by the game
  with a message naming the entry, as the editor refuses it; in M11 only limits the game's checks already
  enforce today are declared, so nothing that loads today stops loading. Steps 1, 5 and 6.
- **G5. Checks that today happen only when something plays** (a music theme's notes and chords, the
  instruments and drums it names, a cutscene's sounds and particle effects by name; a broken theme fails
  when music first plays, an unknown instrument fails on the sound thread mid-game). The editor will check
  them as you edit. **Should the game also check them when it starts?**
  (a) Yes: the game checks them when it loads its data and stops at the start naming the mod and entry, as it
  does for broken models (M9, S5-1); the start takes longer by compiling every theme (measured before it's
  switched on). *Recommended:* a mod's fault shows at once with its name, not as the music failing mid-game.
  (b) No: the game stays as it is; only the editor checks them.
  **Outcome: (a), the user's answer (2026-11-06):** these checks also run when the data loads, with the cost
  measured first: a broken theme or cutscene is reported when the game starts or reloads its data, naming the
  entry; the step measures the added loading time first, and if it is noticeable the manager goes back to the
  user before it goes in (steps 5 and 6).
- **G6. Where the editor keeps its own settings** (recent places, M11-9; its window size and volume; the
  cutscene preview's stand-ins).
  (a) In an `editor/` folder inside the player's game folder, beside `settings.json`, `saves/` and `mods/`
  (what "Open game folder" shows). *Recommended:* one folder holds everything of the game and goes with it.
  (b) In a folder of its own beside it (Linux `~/.local/share/statventure-editor/`, Windows
  `%APPDATA%\Statventure Editor\`), the game's folder untouched. (c) Nowhere: nothing remembered between
  runs (recent places only while it runs).
  **Outcome: (a), decided by the manager** (the editor's own settings): an `editor/` folder in the player's
  per-user game folder, beside `settings.json`, `saves/` and `mods/`; the game ignores it. Tests and tools
  redirect it like the other per-user folders and never write the player's real one (Method). Steps 8 and 13.

Not questions, stated: the engine's window learns file drops and a refusable close (step 3), but the game
doesn't listen, so it closes and ignores drops as now; the marks (step 1) change no data, no layout
signature and nothing the game loads, except that the declared limits are enforced by the game too (G4); the
custom mount, the pack update and single-file import are only used by the editor and the packer.

## Steps

Each step ends as every step does (`Docs/Engine-Plan.md`, "How each step is finished"): its tests, the full
suite on the GPU and on lavapipe, a mutation list covering every rule it adds (each killed or accounted
for), its Progress line, its "How it works" and Status entry here, `Docs/Notes.md`. At most four workers at
once, each in its own worktree; **only one at a time runs the full `Tools/Scripts/test.sh` or `mutate.py`**,
serialized by a lock, not by waiting for the manager: from the repo's root, `flock
~/.cache/stat-fick-build/claude-work/full-run.lock engine/Tools/Scripts/test.sh` (the same lock for the
lavapipe run, its two variables set before `flock`, and for `engine/Tools/Scripts/mutate.py`), which waits its
turn and frees the lock when the run ends; filtered test runs in a worker's own worktree need no lock.
Workers don't touch Engine-Plan's "Current task" (the merge worker does), so their docs commits don't
conflict. Steps marked side by side touch no file in common besides those docs (each its own entry). For
the editor's steps that holds only with step 4's conventions (P2): each part in its own folder with its own
stylesheet, found by the shell without a shared list, and its Chromium fixtures in a file of its own; the
fixture list `Reference/editor-fixtures.json` is written by the test run from all of them, so after two such
steps merge, one test run writes it again (its goldens are a file per fixture).

Order: **wave 1:** 1, 2, 3 and 4 side by side; **wave 2:** 5 and 6 (after 1), 8 (after 2, 3, 4), 9 (after 1,
4), side by side; **wave 3:** 7 (after 2; it may start any time after 2 when a worker is free), 10 (after 5, 6, 8, 9), 12 (after 2, 8, 9), 13 (after 3, 5,
6, 9), side by side; **wave 4:** 11 (after 2, 3, 7, 8, 9, 10), then 14 last. Steps 1–3 and 5–7 are engine
steps (none needs Chromium or Blender); 4 and 8–14 are the editor's (their screens need Chromium: the PC,
or the cloud under Xvfb).

1. **Record shapes, drafts and marks (engine).** Independent.
   - Owns: new files in `Engine/Statventure/Records/` (shapes, drafts, marks, problems and the check
     convention, the generic checks: limits, choices, references given a lookup, build failures);
     `RecordLayout.cs`, `RecordFamily.cs`, `RecordCodecs.cs` only to expose what they hold inside; tests in
     `Tests/Statventure.Tests/Records/` on the `test.*` kinds (marks added to `TestRecords.cs` change no
     signature).
   - Must not touch: the binary and JSON forms, `Data/Records/Layouts/` (no signature changes), any
     declaration outside the tests.
   - Finished: every `test.*` fixture and every committed pack's records read into drafts and built back to
     the same binary form; blank drafts of every engine kind; each build failure (an empty required field, a
     null not allowed, an `FCurve` refusing its keys) at its path; display names; marks read; the generic
     checks. Mutations: each field shape's reading and building, defaults and required, paths, the name rule,
     each mark, each generic check.
2. **Packs for the editor (engine).** Independent.
   - Owns: `Engine/Statventure/Packs/` (`PackWriter.cs`, `VirtualFileSystem.cs`, `Importers.cs`, `PackFile.cs`
     for the index's hash), the importers' source lookups only if they must change (`Render3D/TerrainImporter.cs`,
     `UI/StylesheetImporter.cs`), `Tools/Packer/Program.cs`, tests in `Tests/Statventure.Tests/Packs/`,
     `Docs/Assets.md` (its step 3 section's "Later needs" and a section of its own).
   - Must not touch: the pack format (version 1), `PackFile.Open`'s checks, `Application.Data`'s mounts, the
     packer's existing commands' output.
   - Finished: a pack rewritten with entries changed, added, renamed and removed keeps every other entry's
     stored bytes and flags (older record versions too); a subset written (Save As); the custom mount read
     as a pack is, below and above packs and folders, with `Problem` naming it; one file imported equals the
     same file packed by `AddFolder` (each importer; a terrain's images; a stylesheet's urls against given
     paths); `update` adds and replaces; the index's hash as `Open` checked it. Mutations: each rule.
3. **Window, sound and file-browser hooks (engine).** Independent.
   - Owns: `Engine/Statventure/Platform/Window.cs`, `Platform/Input.cs`, `Core/Application.cs` (the close
     request), `Audio/AudioSystem.cs` (the library swap made public), new files in `Engine/Statventure/Storage/`
     (the file browser's model beside `FolderListing`, P26), tests in
     `Tests/Statventure.Tests/Platform/`, `Core/`, `Audio/`, `Storage/`, `Docs/Audio.md` (a line on the swap),
     `Docs/Assets.md` (a line on the browser's model beside the picker's functions).
   - Must not touch: `UI/` (no HTML drag and drop), the sandbox's behaviour (it closes and ignores drops).
   - Finished: drop events from the callback (called by the test; a real drop checked by hand under X11 and
     Wayland), a refused close keeps the loop running and the next close ends it (also while minimized),
     the default unchanged; the public swap equal to S9-D's (the reload tests' renders); the browser model's
     places, sorting, filters, path reading and completion on scratch folders. Mutations: each.
4. **The editor's shell.** Independent (uses only what exists; Ctrl+S and closing come with step 11).
   - Owns: `Tools/Editor/**` (new: program, application, session skeleton, view, parts' registration,
     layout, splitters, dialogs, keys, title, stylesheet), `Statventure.slnx`,
     `Tests/Statventure.Tests/Statventure.Tests.csproj` (the reference), `Tests/Statventure.Tests/Editor/**`,
     `Tests/.../Reference/editor-fixtures.json`, `Golden/editor-*.png`, `Tools/Scripts/editor.sh`,
     `Tools/Scripts/make-editor-references.sh`, `engine/CLAUDE.md` (the scripts' line).
   - Must not touch: `Engine/`, the sandbox.
   - Finished: the empty shell and a dialog match Chromium at 1 and 1.65; the panes, splitters (by pointer),
     dialogs (focus kept inside, the rest inert, Escape), keys and title through UI events. Mutations: each
     behaviour; the fixture writer.
5. **Audio kinds: checks and marks (engine).** After 1; beside 6.
   - Owns: `Engine/Statventure/Audio/Effects/EffectPatch.cs`, `Audio/Music/ThemeData.cs`,
     `Audio/Music/MusicLibrary.cs`, new check files in `Audio/Music/` (a theme against the data),
     `Score.cs` and `Composer.cs` only to expose the names a layer needs without playing; tests in
     `Tests/Statventure.Tests/Audio/`; `Docs/Audio.md`'s section.
   - Must not touch: the music's sound (every music test unchanged), the layouts.
   - The game's loading (G4, G5): the loaders of the audio kinds (`MusicLibrary.Load`, which also loads the
     effect patches) also run the declared limits (only limits today's checks already enforce, so everything
     that loads today still does: shown on every committed pack and the tests' data) and the new checks.
     **Before the new checks go into loading, measure the added loading time** (the game's data as the
     committed packs hold it, all 55 themes compiled, Release, a cold and a warm start, and a Reload), **report the numbers to the manager and stop for the manager's word**: the manager judges whether
     the time is noticeable and, if so, goes back to the user first. Don't judge it yourself beyond reporting.
   - Finished: each audio kind's check with paths; every rule of the theme check broken once (unknown
     instrument, percussion voice, room, layer type, texture; a bad chord, note, bar, mode, section; an
     instrument that makes no sound); all 55 themes and the committed pack pass; references, choices and
     units marked; the loading as above, each new check and limit at load naming the entry. Mutations: each
     rule and mark, and their wiring into loading.
6. **Scene, model, particle, terrain, state machine and stylesheet kinds: checks and marks (engine).** After
   1; beside 5.
   - Owns: `Engine/Statventure/Scene/Sequence.cs`, `Scene/FCurve.cs`, `Render3D/Particles.cs`,
     `Render3D/ModelData.cs`, `Render3D/Terrain.cs`, `Animation/StateMachineDefinition.cs`,
     `UI/Css/StylesheetRecords.cs` (and `UI/Style/Stylesheet.cs` for paths), tests in their folders,
     `Docs/Scene.md`'s and `Docs/Assets.md`'s sections.
   - Must not touch: the messages the game logs, the layouts.
   - The game's loading (G4, G5): the loaders of these kinds (`Sequence.Load` with its effects and sounds
     against the data, the others' `Load`) also run the declared limits (only limits today's checks already
     enforce, so everything that loads today still does: shown on every committed pack and the tests' data)
     and the new checks. **Before the new checks go into loading, measure the added loading time** (no
     cutscene is committed, so on the proof's sequence from the tests' data with `audio.pak`, Release, cold and
     warm), **report the numbers to the manager and stop for the manager's word**: the manager judges whether
     the time is noticeable and, if so, goes back to the user first. Don't judge it yourself beyond reporting.
   - Finished: each check with the path of what it names (every existing rule's test gains its path); the
     sequence's effects and sounds against the data; bulk, references and colours marked; the proof's
     sequence and every test model pass; the loading as above. Mutations: each path and mark, and their wiring
     into loading.
7. **The mod's info entry (engine).** After 2 (G1 decided: `mod/info`, the engine's kind `mod.info`).
   - Owns: a new file in `Engine/Statventure/Packs/` (the record `mod.info`: name, author, version and
     description as text, and its path `mod/info`), its layout and fixtures in `Tests/.../Data/Records/`, tests
     in `Tests/.../Packs/`.
   - Must not touch: what the game mounts or reads (it never reads the entry; G2 stays as today).
   - Finished: the record's round trips and guard; a mod with and without it mounting and loading as before.
     Mutations: each field, the path.
8. **Opening packs: the browser, drops and the entry list (editor).** After 2, 3, 4; beside 9. Recent places
   kept in the editor's `editor/` folder in the per-user game folder (G6; the run's scratch folder in tests).
   **Also the shell's remembered page zoom and window size** (P4, G6; the manager's decision (a), 2026-11-06, at
   step 4's report: step 8 adds them along with the other `editor/` settings, one owner for the settings files,
   step 4 staying without a file format): the zoom level the shell's Ctrl+= and Ctrl+- set
   (`Shell/EditorView.Keys.cs`: `Zoom` from a private level; a way to set the level at start is needed there) and
   the window's size (`EditorApplication`'s `WindowOptions`, read before `Run`), kept in a file of their own in
   `editor/`.
   - Owns: `Tools/Editor/Browser/` (the browser's screen on step 3's model), `Tools/Editor/Entries/`, the
     session's opening (its own file), tests in `Tests/.../Editor/`, fixtures (list, browser).
   - Finished: the browser's places, listing, path field and filters; drops; packs opened with each kind of
     unreadable entry listed with its reason and the pack read-only; a pack `Open` refuses; the list's tree,
     marks, filter and keys; the two screens against Chromium. Mutations: each.
9. **The form, undo and the JSON view (editor).** After 1, 4; beside 8.
   - Owns: `Tools/Editor/Form/`, the session's drafts and histories (their own files), tests, fixtures (form,
     JSON).
   - Finished: every field shape of the `test.*` kinds and every engine kind shown and edited through UI
     events (a draft built to the expected record), parse failures kept out, undo and redo per entry with the
     fields' own typing undo first, new, duplicate, rename and remove asking, the JSON tab, copy and export;
     the screens against Chromium. Mutations: each.
10. **Checks and references in the editor.** After 5, 6, 8, 9; beside 12, 13.
    - Owns: `Tools/Editor/Checks/`, `Tools/Editor/References/`, tests.
    - Finished: problems at their fields, at the top and in the list, the cross-entry checks after a pause;
      the reference field's list and its flag; rename and remove listing referrers and updating them.
      Mutations: each.
11. **Saving and mods (editor).** After 2, 3, 7, 8, 9, 10.
    - Owns: `Tools/Editor/Saving/`, the mod info's form, tests, the dialog fixture.
    - Finished: save refused with problems; a pack written keeps unchanged entries byte for byte; the changed
      file asked about; Save As subsets and the unreadable pack's case; a new mod in the run's scratch mods
      folder, mounted by `Application.DataMounts` as the game mounts it and read back equal; the dev setup's
      save in place (a scratch copy of the repo's packs); the title's mark; closing asks. Mutations: each.
12. **Import (editor).** After 2, 8, 9; beside 10, 13.
    - Owns: `Tools/Editor/Import/`, tests.
    - Finished: each kind imported by button and by drop equal to the packer's entry, the suggested path, the
      replace question, importing again with form edits kept or taken per value, a JSON file back in.
      Mutations: each.
13. **The preview (editor).** After 3, 5, 6, 9; beside 10, 12. The stand-ins and the volume kept in the
    editor's `editor/` folder (G6).
    - Owns: `Tools/Editor/Preview/`, tests (sound at volume 0, rendered offline where the sound is checked;
      3D through the headless GPU fixture).
    - Finished: effects and themes play, stop and follow edits (renders equal to registering and playing by
      hand); particles loop; a model's clips and camera; a cutscene plays, pauses and seeks with stand-ins, the
      unbound tracks left out; an invalid edit keeps the last valid state. Mutations: each.
14. **The whole editor, end to end.** Last.
    - Owns: tests across the editor, the remaining fixtures, the docs' wrap-up, M11's tick.
    - Finished: a mod made from scratch through the UI (a sound effect, a particle effect, a stage imported
      from a test `.glb` and a cutscene using the three), saved, mounted as `Application.DataMounts` mounts
      mods over the game's packs and loaded by the engine's own loaders (`Sequence.Load`, `ParticleEffect.Load`,
      `MusicLibrary.Load`); a scratch copy of the game's `audio.pak` edited in the dev setup and dumped
      through `textconv.sh`; every editor screen against Chromium at both scales; the full suite; Windows noted
      as not checked.

## How it works

Filled in as the steps are done.

### Step 2: packs for the editor (engine)

The engine's side in full: `Docs/Assets.md`, "How it works", "Packs for the editor" (code in
`Engine/Statventure/Packs/`, `UI/StylesheetImporter.cs`, `Tools/Packer/Program.cs`; tests in
`Tests/Statventure.Tests/Packs/`: `PackRewriteTests`, `CustomMountTests`, `ImportFileTests`, `PackUpdateTests`).
What the editor's steps use:
- **Saving (steps 11 and 7; M11-16, M11-17, P23, P24):** `new PackWriter()`, then `AddFrom(openPack, entry =>
  unchanged)` (each unchanged entry kept as it is: stored bytes, compression, record flag, a record's binary form
  in its own version, an unknown kind byte for byte), `AddFrom(openPack, entry, newPath)` for an unchanged entry
  renamed, `AddRecord(path, type, value)` for an edited one (the current versions), `AddRecordBinary` or `Add`
  for an imported one, removed ones left out; then `Write(file)` (a temporary file, flushed, renamed over it).
  Save As is the same with the entries the user ticks, written to the new file: the kept entries keep their
  stored bytes there too. A damaged entry can't be added (`AddFrom` reads and checks it first: a
  `DataFileException` naming the pack and entry), so P24's "damaged ones not offered" is also what the writer
  enforces. Before writing, open the file again and compare its `PackFile.IndexHashHex` with the one read when it
  was opened (P23: changed by another program); after writing, open it again (the editor's open `PackFile` keeps
  reading the replaced file on Linux).
- **The data as the game would see it after saving (P38; steps 10, 13):** an `IMountEntries` over the session
  (`Paths`, `IsRecord`, `Read`: a file's bytes or a record's binary form; give a fresh array each read), mounted
  with `Mount.Custom(<the pack's file>, entries)` on top of the base's mounts; `Reload()` after an entry is added,
  removed or renamed or turns from record to file (the paths and kinds are taken at mount and Reload; the bytes
  at each read). Its entries are checked at mount as a pack's index is, and errors name the given name and the
  entry, so a loader's message reads as the saved mod's would.
- **Import (step 12; M11-8, P28):** `PackWriter.ImportFile(file, sourcePath, new ImportSources { Exists = path
  => the data the mod sees has it }, types, importers)`, `sourcePath` being the chosen entry path with the
  importer's ending (`models/stages/arena` + `.glb`): the importer is chosen by that ending, ordinal and in lower
  case, so the editor picks it for a file named `Arena.GLB` itself. The entries come back with their binary forms
  (`ImportedEntry`); the files an importer reads besides are found beside the file on disk by default
  (`ImportSources.FileOf` to say otherwise). A record's `.json` form and loose files (`.ttf`, `.png`, …) import
  the same way, with no importer.
- **The packer's `update`** (P30): see `Docs/Assets.md`; the scripts that make whole packs don't use it yet.

Found against this doc's design: "`Write` then keeps every unchanged entry's stored bytes (its existing reuse)"
covers only the file being replaced (the reuse matches contents in the destination), not Save As to a new
file; so an entry added from a pack carries its own stored bytes into whatever pack is written. The stylesheet
importer's url check names a missing url with `Stylesheet.Compile`'s words, "names no file in the data being
packed", also when one file is imported (`UI/Style/` isn't step 2's).

Choices the design left to the step (named here, decided by the step): the names above; a kept entry read and
checked when added, not when written; `AddRecordBinary` reads nothing of the form; the custom mount's name given
to `Mount.Custom` and standing for every entry's origin (P38's "origin"), its paths and kinds taken at mount and
Reload, its entries checked with a pack index's words; `ImportFile`'s defaults (files beside the source on disk
and below it, by paths below the source path's folder; no data path exists) and its `Sources` (the file's own
path); `update`'s refusals (a missing pack isn't made, a damaged kept entry and a clash stop it, nothing written)
and its url check against the folder and the pack, following P28's rule ("the data the mod sees").

### Step 4: the shell (2026-11-06)

**Files.** `Tools/Editor/` (`Editor.csproj`: an `Exe`, assembly `Statventure.Editor`, namespace `Statventure.Editor`,
every `**/*.css` an embedded resource, internals visible to `Statventure.Tests`; in `Statventure.slnx` under
`/Tools/`, referenced by the test project): `Program.cs` and `EditorArguments.cs` (the command line),
`EditorApplication.cs` (the window), `Session/` (`EditorSession.cs`, the session's skeleton; `GameData.cs`, where
the game's packs are), `Shell/` (`EditorView.cs` and `EditorView.Keys.cs`, the document; `EditorPart.cs`;
`EditorCommands.cs`; `Dialogs.cs`; `Splitter.cs`; `Shell.css`; `Fonts.css`). Tests in
`Tests/Statventure.Tests/Editor/`: `EditorHarness.cs`, `EditorFixtures.cs` (the fixture record, the interface, the
writer), `EditorScreenTests.cs` (the list and the Chromium comparisons), `EditorFixtureWriterTests.cs`,
`TestParts.cs`, and the shell's own in `Editor/Shell/` (`ShellScreens.cs`, `ShellTests.cs`, `CommandTests.cs`,
`DialogTests.cs`, `SplitterTests.cs`, `EditorArgumentsTests.cs`). Scripts: `Tools/Scripts/editor.sh`,
`Tools/Scripts/make-editor-references.sh`. References: `Reference/editor-fixtures.json` (written by the test run),
`Golden/editor-shell@1.png`, `@1.65.png`, `Golden/editor-dialog@1.png`, `@1.65.png`.

**Running it.** `Tools/Scripts/editor.sh [--player-folder <folder>]` builds Release and starts the editor in the dev
setup. The program's command line (`EditorArguments`): `--repo <engine folder>` is the dev setup (the game's packs
are `<engine>/Assets/Packs`, `GameData.DevSetup` true; P5); `--game <game folder>` an installed game's
`<folder>/packs`, read-only; neither: `packs/` beside the program, read-only; `--player-folder <folder>` the player's
game folder (`ApplicationOptions.GameFolder`; default this machine's), so a check runs it on a scratch folder. A
wrong command line or a packs folder without `ui.pak` (no fonts) stops it before a window opens, with the usage
(exit code 2). The window: "Statventure Editor", 1280 × 800, at least 800 × 500, no sound (step 13 turns
`ApplicationOptions.Audio` on in `EditorApplication`, the one line of this step's files it changes); its title and
page zoom follow the view's (`View.TitleChanged`, `View.ApplyZoom` → `UiHost.PageZoom`).

**The shell's layout** (`Shell.css`, P4): the old game's dark-mode colours as variables (`--paper`, `--paper-deep`,
`--line`, `--ink`, `--muted`, `--soft`, `--ember`, `--ember-pale`; `--row` 24 px), Lato 13 px with 24 px lines.
Top to bottom in normal flow (`html`, `body` and `.shell` 100 % high, the body `overflow: hidden`): the top bar
(`header.topbar`, 33 px with its border: New mod, Open | Save, Save As | Import | Undo, Redo, buttons 24 px high), the
panes (`main.panes`: `section.pane[data-pane=entries]` 280 px, a 4 px splitter, `[data-pane=form]` the rest, a
splitter, `[data-pane=preview]` 360 px; each pane scrolls on its own), the status line (`footer.status`, 25 px).
A narrower window takes the width from the middle pane first. Focus shows as a 2 px ember outline.

**The conventions the editor's later steps follow (P2).** Each step works in its own folder and files; nothing
below needs a shared file edited.
1. **A part** is a non-abstract subclass of `Statventure.Editor.Shell.EditorPart` in the editor's assembly, in a
   folder of its own (`Tools/Editor/<Part>/`, namespace `Statventure.Editor.<Part>`), with a parameterless
   constructor. The shell finds and makes one of each; nothing is registered. It says where it shows
   (`Pane`: `EditorPane.Entries`, `.Form`, `.Preview`, or null for none, e.g. a part that only opens dialogs or
   adds to other parts) and its `Order` (default 0), and builds itself in `Attach(UiElement? element)`. Parts are
   attached in this order: those without a pane, then the entry pane's, the form pane's, the preview pane's; within
   that by `Order`, then by the class's full name. A part with a pane gets its own `<div class="part">` in that pane
   (the pane's children in that same order) as `element`; one without gets null. `Session` and `View` are set
   before `Attach`. Parts reach each other with `View.Part<T>()`; in `Attach` the later parts exist but aren't
   attached yet, so use them from event handlers.
2. **A part's stylesheet** is any `.css` file in its folder: the project embeds every `.css` file and the shell adds
   them all to the document, `Shell/Fonts.css` first, `Shell/Shell.css` second, then the rest ordered by resource
   name (`Statventure.Editor.<Folder>.<File>.css`, ordinal). Scope every selector under the part's own classes;
   use the shell's variables for colours. Rules the Chromium comparison needs: no `@font-face` (only `Fonts.css`
   has them: Lato 400 and 700 from `ui.pak`, the faces the capture harness also has, so use the weights 400 and
   700 only); nothing in a compositing layer of its own over the root: no `position: fixed` (use `absolute`
   inside the root), no `will-change`, no transform or opacity animations (the editor runs as a main frame, whose
   root scroller is composited, while Chromium's captures are made in an iframe, whose root isn't, so such a box
   is a layer of its own, with grayscale text, only in the editor; see "Found" below); no `color-scheme` (the
   engine ignores it, Chromium would draw its native controls dark): give controls their own colours, and
   `font: inherit` (the shell does it for buttons).
3. **Commands** (`View.Commands`, `EditorCommand`): `Handle(command, request => acted, () => canRun)`. A command runs
   its handlers that can run in the order they were registered until one returns true. The top bar's button of a
   command (New mod, Open, Save, Save As, Import, Undo, Redo) is enabled while one of its handlers can run; call
   `View.Refresh()` (or `Session.NotifyChanged()`) when that may have changed. The keys (at the document, after
   the focused element's own listeners): Ctrl+S Save, Ctrl+Shift+S SaveAs, Ctrl+O Open, Ctrl+Z Undo, Ctrl+Y and
   Ctrl+Shift+Z Redo, Ctrl+F Filter, F2 Rename, Delete Remove, Ctrl+= and Ctrl++ (also the keypad's) ZoomIn,
   Ctrl+- ZoomOut; Alt or the Super key with any of them is no command. A request carries the key event
   (`Key`) and the element it went to (`Target`; for a click, the button) and `FromTextField` (a text field,
   a textarea or contenteditable). A key an element's listener cancelled is no command; a command that acted
   cancels the key's default action; one that didn't leaves it (return false from Undo while a text field has
   typing to undo, P13, and the field undoes it). Delete in a text field never reaches Remove. While a dialog is
   open only the zoom keys are commands, and Escape cancels the topmost dialog. The zoom is the shell's own
   handler: half a zoom level a step, the page zoom 1.2 to the power of the level (the old game's View menu,
   Electron's zoomIn and zoomOut roles), from 53 % (level −3.5) to 299 % (level 6), Electron's default limits
   of 50 % and 300 %; not remembered yet: step 8 adds that, with the window's size, to the `editor/` settings
   (the manager's decision (a), 2026-11-06; "Steps", step 8).
4. **Dialogs** (`View.Dialogs`): `Ask(title, message, (text, value) ...)` gives a `Task<string?>`: the value of the
   button clicked, null for a button with a null value or Escape; the first button with a value is the primary
   one (ember). Your own: `var d = View.Dialogs.Create(title)`, fill `d.Content` (and `d.Buttons`, or
   `d.AddButton(text, value, primary)`), `d.Show()`, await `d.Closed`; `d.Close(value)` from code. Markup:
   `div.dialog-layer` (the last child of the body, over the window, translucent) holding `div.dialog` (role
   dialog, `aria-modal`, `aria-labelledby` its `h2`) with `div.dialog-content` and `div.dialog-buttons`. While one
   is open the shell and every lower dialog are `inert` (neither the pointer nor Tab reaches them: Tab and
   Shift+Tab go round the top dialog's controls). Shown, it takes the focus as Chromium's modal dialog does
   (`Element::GetFocusDelegate`): its first `autofocus` element, else its first keyboard-focusable element, else
   the box; a press on the layer outside the box leaves the focus where it is, one on the box's text focuses the
   box. Closed, the focus goes back to where it was before it opened if it was in the dialog (HTML's rule).
5. **The session** (`Statventure.Editor.Session.EditorSession`, no DOM) is a partial class: a step adds its own file
   to `Tools/Editor/Session/` (`EditorSession.Opening.cs` for step 8, `EditorSession.Drafts.cs` for step 9, …) with
   its own fields and methods. The skeleton gives: `Game` (`GameData`: `PacksFolder`, `DevSetup`, `UiPack`);
   `PackName` (the open pack as the title names it, its file name; null when none, setting it notifies);
   `AddUnsavedWork(Func<bool>)` (a source the title's "•" and closing ask; register it once where your state is
   made, e.g. when a pack opens, or in your part's `Attach`); `NotifyChanged()` and `Changed` (the view refreshes
   its title, status and buttons); `Own(disposable)` (disposed with the session, last first).
6. **The view's other parts:** `View.Status` (the status line's text, "No pack open" at the start),
   `View.Title` ("Statventure Editor — <pack>", with "• " first while there is unsaved work; M11-17),
   `View.Pane(pane)`, `View.Button(command)`, `View.Shell` (the shell's root), `View.Document`.
7. **Screens against Chromium** (M11-20, P36): a step's fixtures are a class implementing `IEditorFixtures` in a
   file of its own beside its tests (`Tests/Statventure.Tests/Editor/<Part>/<Part>Screens.cs`), giving
   `EditorFixture(name, width, height, setup, windowFocused = true)`: the live editor (`EditorHarness` on the repo's
   packs with the editor's parts) at that size in CSS px, put into its state by `setup`. Names are unique across
   all steps and name the goldens (`Golden/editor-<name>@1.png`, `@1.65.png`), so start them with the part's name
   (`list-marks`, `form-controls`, …). Every class implementing the interface is found in the test assembly; the
   test run writes the list of all of them, by name, into `Reference/editor-fixtures.json` (after two steps
   merge, one run writes it again); `Tools/Scripts/make-editor-references.sh <name> ...` captures only the named
   ones in Chromium 152 (PaintReference, X11, an invisible window) at 1 and 1.65, leaving the other goldens;
   `EditorScreenTests` compares each at both scales at the paint fixtures' limits (12 levels, 0.2 % of the pixels).
   The writer (`EditorFixtures.Html`, `EditorFixtures.Css`): the editor's stylesheets without `Fonts.css` (the
   harness gives Lato), and the body's children (keep attributes off `html` and `body`) with the state that isn't
   in attributes written as attributes: an input's value as `value`, checked boxes `checked`, selected options
   `selected`, a textarea's value as its text, `data-indeterminate`, `data-focus` on the focused element,
   `data-scroll-top` and `data-scroll-left` on scrolled elements, and a `surface` (the preview's 3D) as an empty
   `div` of its border-box size (P36). What doesn't carry over and so stays out of fixtures: hover and pressed
   states, the caret and selections, the engine's top layer (tooltips, a select's popup, validation bubbles).
8. **Tests** drive the editor without a window: `new EditorHarness(width, height, scale, parts)` (test parts, from the
   test assembly, besides the editor's; `RecordingPart` handles every command and records the requests), then
   `Click`, `Key`, `Drag`, `Frame` and `Q`, `Rect`, `Middle` (an element's middle in window units).
9. **The player's folders:** step 4 writes nothing; the editor's own settings go into `<player folder>/editor/` (G6),
   a file per step that keeps any, through `Application.Storage.Folder` (tests: the run's redirected folder).

**Splitters** (`Splitter.cs`): a press of the main button on a splitter takes the pointer's capture and is cancelled
(no focus change, no selection, no compatibility mouse events); moves set the side pane's inline `width` in whole
CSS px (the left splitter the entry list, the right one the preview, mirrored), the side pane at least 120 px and
the middle one at least 200 px; the release or a lost capture (the window losing the focus) ends the drag. While
dragging the splitter has `.dragging` (ember) and the shell `.resizing` (the column-resize cursor everywhere).

**Choices the design left to the step** (made here): the window's size (1280 × 800, at least 800 × 500); the panes'
widths (280 and 360 px) and narrowest widths by a splitter (120, the middle 200); the title's mark first ("• " at the
start, so a shortened title keeps it); the zoom's steps and limits (the old game's View menu as above; no reset key,
Ctrl+0 not being in the design); the dialog's look (a box 420 px wide on a half-dark layer, the primary button
ember, buttons right-aligned in the order given); the empty status line's "No pack open"; the dialog fixture's
sample text; the `--player-folder` option and the assembly's name `Statventure.Editor`.

**Found** (reported to the manager, the engine not changed):
- *Text that is the first thing drawn in a paint chunk is drawn with the previous chunk's LCD verdict.*
  `PaintTarget.DrawText` and `DrawGlyphs` draw at once, but the chunk (and with it `Canvas.TextLcd`) is made in
  `AddItem` when the item ends; so text first in a new property state (in a box with `overflow: hidden` or `clip`
  with nothing drawn before it inside the clip) takes the verdict of the chunk before, often one without text,
  grayscale. Chromium draws it LCD. In the shell: the status line's text, 435 pixels at 1× and 843 at 1.65, within
  the limits (with the status line's `overflow: hidden` taken out the shell matches at 0 and 28 pixels). Later
  steps' scrolling panes meet it where a pane's first drawing is text. A minimal repro (a test file, not
  committed): `~/.cache/stat-fick-build/claude-work/m11/step4/ScratchLcdRepro.cs.txt` on the user's PC (on
  `html { background: #1a1c1a }`, `<div style="overflow: hidden">No pack open</div>`: the chunk's verdict LCD, the
  text drawn grayscale, colour spread 9; without `overflow: hidden` LCD, spread 88). **Handled outside this step
  (the manager, 2026-11-06):** a worker of its own on the painter fixes it.
- *The editor can't start without the player's mods.* `Application.Run` mounts the player's `mods/` (and
  `$STATVENTURE_DEV_FOLDER`) into `Application.Data` for every application, so a damaged mod in the player's mods
  folder stops the editor's start, though the editor doesn't read `Application.Data`. **Handled outside this step
  (the manager, 2026-11-06):** an option to start without mounting the player's mods goes to step 3
  (`Core/Application.cs`); `EditorApplication` uses it in a later step, not in step 4.
- *Chromium's reference pages are iframes.* `Tools/PaintReference` lays each fixture out in a srcdoc iframe, whose
  root isn't the global root scroller (`CompositingReasonFinder::RequiresCompositingForRootScroller`) and, with the
  body's `overflow: hidden`, not user scrollable, so its root isn't composited; the engine's documents are main
  frames (the root scroller always composited). A fixed box over the root is a layer of its own in the editor (by
  overlap; its text grayscale where no opaque background is known under it) and part of the root in the
  capture: the shell's first version (a fixed `.shell`) failed at 1× (0.26 %) on its top bar's text for this.
  Hence the rule in convention 2.

**Results.** The screens against Chromium (differing pixels past 12 levels; the limit 0.2 %): `shell` 435 of
640,000 at 1× (0.068 %, worst 60), 843 of 1,742,400 at 1.65 (0.048 %, worst 58); `dialog` 389 at 1× (0.061 %,
worst 62), 598 at 1.65 (0.034 %, worst 99), all the status line's text above and glyph antialiasing.
Mutations (`claude-work/m11/step4-mutations.json`, made by `step4/make-mutations.py`): 189, every behaviour of the
shell, the stylesheet's look and the fixture writer broken once; 183 killed (three after closing the gaps the first
run showed); the 6 survivors can't change what the checks see: the other stylesheets' order and the parts of the
editor's own assembly (none before step 8), and four looks under the screens' limits (the focus ring 1 px narrower,
about 0.02 % of the pixels; the dialog layer 10 % lighter, 3–4 levels; the dialog title 2 px smaller on its fixed
24 px line; the dialog layer fixed: its text lies on the box's own opaque background and keeps LCD text).
Lesson for later steps' tests: never read `Task.Result` of a dialog a broken rule may leave open (assert
`IsCompleted` first): `mutate.py` waits 10 minutes on the hung test and reads it as a survivor.

**Not checked:** the program in a real window (none may be shown on the PC; to check by hand: `editor.sh` opens the
shell, the window's title, the zoom keys through `UiHost.PageZoom`, a splitter dragged with the mouse and its
cursor); Windows.

## Status

**M11 started (2026-11-06):** the user's go; the decisions recorded (M11-1–M11-21 and the user's rule for
M11); what exists checked against the code; the method; the design proposed, with 38 editor choices for the
manager (P1–P38) and six questions for the user about the game (G1–G6); fourteen steps proposed with their
dependencies. No step started; nothing implemented.

**The design decided (2026-11-06):** P1–P38 by the manager, all as recommended (P18 with its known limit:
a mod referring to another mod's entries can't be saved in M11); G1 and G6 by the manager; G4 and G5 by the
user (the game enforces declared limits; the play-time checks also run at load, the cost measured first);
G2 and G3 kept for the port. The fourteen steps under "Steps" are the plan; none has started.

**Step 2, packs for the editor (engine): done (2026-11-06, on the worker branch `m11-step2`).** ("How it
works", step 2; `Docs/Assets.md`, "Packs for the editor".) A pack rewritten from an open pack with entries
changed, added, renamed and removed keeps every other entry's stored bytes, compression and record flag (an
older record version, an unknown kind, entries stored as the writer wouldn't store them now); a subset written
to another file (Save As) keeps them too; the index's hash exposed; the custom mount read as a pack is, below
and above packs and folders, named by its name and the entry in every error, checked as a pack's index is,
under the engine's loaders and the music library's cross-entry checks; one file imported equals the same file
packed by `AddFolder` (models, terrain with its images beside it or where the caller says, a stylesheet's
urls against the caller's paths, records' JSON forms, plain files, the errors); `packer update` adds and
replaces, keeps the rest, checks urls against the folder and the pack, and refuses what it can't keep. 33 new
tests (`PackRewriteTests` 9, `CustomMountTests` 13, `ImportFileTests` 7, `PackUpdateTests` 4). The full suite
passes on the GPU and on lavapipe: 2580 tests (`main`'s 2547 and 33 new). Mutations: 68
(`~/.cache/stat-fick-build/claude-work/m11/step2-mutations.json`, made by `make-step2-mutations.py` beside it),
all killed; nothing written to the player's real folders.
Nothing of P1–P38 or G1–G6 changed; no question came up that the design doesn't answer (the step's own choices
are listed at the end of "How it works", step 2). Not checked: Windows
(writing over a pack held open, a case-insensitive file system under `ImportSources`' default lookup).

**Step 4, the shell: done (2026-11-06, branch `m11-step4`).** The program, the session's skeleton, the view (top
bar, panes, splitters, status line), parts found without a list, commands and keys, dialogs, the title and the zoom;
the empty shell and a dialog match Chromium at 1 and 1.65; the conventions for steps 8–13 ("How it works", step 4).
79 new tests (`Tests/Statventure.Tests/Editor/`); the full suite passes on the GPU and on lavapipe: 2676 tests
(`main`'s 2597 at `2323779` and 79 new). Mutations: 189 (`~/.cache/stat-fick-build/claude-work/m11/step4-mutations.json`),
183 killed, 6 accounted for ("How it works", step 4, "Results"), nothing written to the player's real folders.
Found and reported (the engine not changed): text first in a paint chunk is drawn with the previous chunk's LCD
verdict (a worker of its own on the painter); the editor can't start without mounting the player's mods (step 3);
the capture harness's iframe root isn't composited as a main frame's is (the stylesheet rule, convention 2). Not
checked: the program in a real window (to check by hand), Windows. Remembering the zoom and the window's size (P4,
G6): step 8, the manager's decision (a) of 2026-11-06 ("Steps", step 8).

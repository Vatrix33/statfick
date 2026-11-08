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
  today.) The recommended option. **For cutscenes (step 6), the user's decision of 2026-11-07**, shown the measured
  time: the declared limits and the effects-and-sounds check go into `Sequence.Load`, and the loaders that built the
  proof's data without the sound pack (the sandbox's cutscene mode, `JiruanIntroTests`) mount it as the game does
  ("How it works", step 6).
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
- **Capturing the screens without a window on the user's screen** (found by step 9, 2026-11-07): run
  `make-editor-references.sh`'s harness inside a headless `cage` (`WLR_BACKENDS=headless`, the user's `DISPLAY` and
  `WAYLAND_DISPLAY` unset; cage gives it an Xwayland of its own). Two quirks there: **at 1.65 a PaintReference run
  stalls after its first fixture, so capture one fixture per run** (`claude-work/m11/step9-capture-scale.sh 1.65 -
  <names>`); and cage makes a window larger than its 1280 × 720 output that size, cutting the capture, so at 1× give the
  headless output a large mode first (`wlr-randr --output HEADLESS-1 --custom-mode 2400x2400`;
  `claude-work/m11/step9-capture.sh <names>`), and at 1.65 leave it as it is (with the large mode even a first fixture
  stalled). Both ways give the shell's committed goldens pixel for pixel. (Steps 8 and 10 captured at 1.65 with the large
  mode too, one fixture per run, without a stall; a fixture wider than 1280 / 1.65 CSS px needs it, or the window is
  made the output's size and the capture cut: `claude-work/m11/step10-capture.sh 1.65 2400x2400 <name>`.) A fixture far taller than a screen (2,460 px)
  never captured at 1.65: split such a screen into parts (step 9's form is two fixtures, the second scrolled).
  End a wrapper's own cage when its command ends (a cage outliving its child held the shared lock once).
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
  *Revert (the manager, 2026-11-06, at step 9's report):* the data side is step 9's: the session reverts an entry
  that is the mod's own copy over a game entry (the copy dropped, the game's entry shown again with its history
  cleared), not undoable and asking first like a removal (M11-7), and the form shows the button only for such
  entries; step 11 wires it to saving ("How it works", step 9).
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
  **For the music (step 5): wired, the user's decision of 2026-11-07** (the question tool), after the step measured
  it and stopped: a cold start's loading 48.9 → 69.6 ms on the game's thread (the 17.2 ms compile of all themes moved
  there from the warm-up thread; 3.5 ms more work in all), warm about 3 ms more, a Reload about 5 ms more; both
  consequences accepted (a silent layer naming an instrument that isn't there is refused; the tests' sample theme
  that doesn't compile is refused, the tests given a playable one). "How it works", step 5.
  **For cutscenes (step 6): wired, the user's decision of 2026-11-07**, with the
  measurement shown (the proof's `Sequence.Load` cold 137 → 159 → 192 ms, +12 ms once the music is loaded, warm
  +0.2 ms): the declared limits and the effects-and-sounds check go into loading, and the loaders that built the
  proof's data without the sound pack mount it as the game does ("How it works", step 6).
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
    - **One problem per field (the manager's decision, 2026-11-06, on step 6's report):** the engine reports a broken
      value once per check that holds it, so a declared limit and the kind's own rule (G4: every limit in M11 is one
      an own rule already holds), or a reference mark and a kind's own check against the data (a sequence's stage or
      effect missing), give two problems at one path in different words; both stay reported (`RecordChecks.Check`
      lists the kind's own checks first), and **the editor shows only the first problem per field**, which is the
      kind's own, the words the game logs.
    - **`AudioCue.Name`'s references** (the manager's decision, 2026-11-06: with step 5's music name rule once steps 5
      and 6 are both merged): marked by step 6 itself once rebased on step 5 (`3e6df0f`), where it was trivial (the
      coordinator's go, 2026-11-07): a sound effect (`audio.effect`, `audio/effects`) when its kind is `Effect`, a theme
      (`audio.theme`, `audio/themes`) when `Music`, both by `MusicNameRule`; nothing when it stops the music.
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

### Step 1: record shapes, drafts and marks (`Engine/Statventure/Records/`)

All public, in `Statventure.Records`; nothing about the binary or JSON forms or the layout signatures
changed (the layout guard passes with marks on the `test.*` kinds). The serializer's internals are reached
through exposures only: `RecordLayout.Fields`, `Subtypes` and `Make` (the compiled factory), `FieldLayout.Get`
(the compiled getter), and on the codecs what they hold (`Inner`, `Element`, `KeyCodec`, `ValueCodec`,
`IsPairs`, `Family`, the enum codec's `IsMember` and `JsonNameOf`; `MapCodec.Make` made public).

- **Shapes** (`RecordShape.cs`, `ValueShape.cs`): `RecordShape.Of(type)` (once per current record type; a
  past version or a type without `[Record]` throws): `Name`, `Version`, `DisplayName`, `Help`, `Folder`,
  `Rule`, `IsUnion`, `Subtypes` (in tag order), `Fields` (number order), `Field(name)` (JSON or code name),
  `PathOf(name)` and `NameOf(path)` (null without a folder). `FieldShape`: `Number`, `Index`, `Name` (code),
  `JsonName`, `DisplayName`, `Value`, `Required`, `Default` (the serializer's shared instance: not to be
  changed), `Get(record)`, and the marks: `Help`, `Unit`, `Limits` (`FieldLimits`: `Min`, `Max`,
  `MinExclusive`, `MaxExclusive`, `Contains`, `Describe`), `Choices`, `IsColour`, `IsBulk`, `BulkSummary`,
  `Summarize(value)`, `References` (`ReferenceShape`: `Kind` as a `RecordShape`, `Folder`, `Rule`, `Keys`,
  `When`, `Is`, `PathOf`, `NameOf`), `ReferenceCondition`, `ReferenceFor(conditionValue)`,
  `ReferenceIn(record)`. `ValueShape`: `Kind` (`ValueKind`: `Bool`, the eight integer kinds, `Float32`,
  `Float64`, `String`, `Enum`, `Bytes`, `Vector2`–`Vector4`, `Quaternion`, `Matrix4x4`, `Nullable`, `List`,
  `Map`, `Pairs`, `Record`, `Union`), `Type` (as declared: `List<string>`, `int?`), `Element` (a nullable's
  value, a list's elements, a map's values), `Key`, `Record` (for a union the union's shape), `Members`
  (`EnumMember`: code name, JSON name, display name, value; in declaration order, not by number),
  `Signature`, `HoldsRecords`, `IsInteger`, `IsFloat`, `IsNumber`.
- **Display names** (M11-10): words split where a capital starts one (after a small letter or a digit, or the
  last capital of a run followed by a small letter) and at `_`; the first word capitalized, the others in
  small letters unless they have none: `MaxVoices` "Max voices", `HDRFactor` "HDR factor", `ScaleX`
  "Scale X", `UInt8` "U int8", `FCurve` "F curve" (until step 6 marks it). A type's display name is its
  `Kind` mark's, else made from its code name the same way; an enum member's from its code name.
- **Marks** (`RecordMarks.cs`): `[Kind(displayName, folder) { Rule = typeof(…) }]`, `[Help(text)]` (fields and
  types), `[Unit(text)]`, `[Limits(min, max) { MinExclusive, MaxExclusive }]`, `[Choices(…)]`, `[Colour]`,
  `[Bulk(summary)]`, `[References(typeof(Kind)) { Folder, Rule, Keys, When, Is }]` (several per field),
  `[Check]` (on methods). A mark that doesn't fit its field throws an `InvalidOperationException` naming the
  type and field when the shape's fields are first asked for: limits on what holds no numbers, a min above
  the max or NaN; choices or references on what holds no strings; a colour that isn't a `Vector3`,
  `Vector4` or string (or a list of them; maps refused); `Keys` on what isn't a map with string keys; a
  reference without a folder (neither its own nor its kind's) or with one the path rule refuses; a rule that
  isn't a `NameRule` type with a public parameterless constructor; `When` without `Is` or the reverse, an
  empty `Is`; two marks without a condition; conditions on two fields, on a field that isn't one, on one
  that isn't a string or an enum (nullable or not), on an enum value that isn't a member's code name, or
  two marks holding for one value. A `Kind` folder the path rule refuses throws when the shape is made.
  What "holds" means: the field itself, through a nullable, each of a list's elements (nested lists too) and
  each of a map's values; `Keys` the map's keys.
- **Name rules** (`NameRule.cs`, P9): a rule is a `NameRule` type (`PathOf(folder, name)`,
  `NameOf(folder, path)`, null when it can't), named by the marks and made once. `PlainNames` (the default):
  a name is one segment directly in the folder, under the path rule (`models/effects/sparks` is "sparks").
  `PathNames` (`NameRule.FullPath`): a name is the whole entry path at any depth below the folder (a
  sequence's stages, a particle cue's effect). The music's rule (S6-1) is step 5's to add as a `NameRule`
  over `MusicNames`.
- **Drafts** (`RecordDraft.cs`, P10): immutable; every change makes a new draft sharing the rest.
  `DraftValue.Missing` (not given: a required field is a problem when built, any other means its default,
  as an absent JSON key; a list's element or a map's value not given is a problem), `DraftValue.Null`,
  `DraftLeaf` (numbers, bools, strings, enum members, vectors, matrices, byte arrays, and a bulk field's
  original object, kept by reference into the record built), `DraftList` (`With`, `Insert`, `RemoveAt`,
  `Move`), `DraftMap` of `DraftEntry` (key and value, in order; the same operations), `RecordDraft` (`Shape`,
  `Fields`, `this[name or field]`, `With(name or field, value)`, `At(path)`, `With(path, value)`;
  `From(record)`, `Blank(shape or type)`: the defaults, required fields `Missing`; a union has no blank and
  no draft of its own, only its subtypes). A nullable's value is the inner node itself; a union's is the
  record draft of its subtype. Paths through maps find the first entry whose key's text matches.
- **Building** (`RecordDraft.Build()` → `DraftBuild`: `Value`, `Problems`, `Built`): bottom up through the
  compiled factories, every problem collected at its path: "needs a value" (a required field not given, a
  list's element or a map's value not given), "null in a field that can't be null" (the serializer's words;
  a field, an element, a value), "3 isn't a member of Shade" (a value or a key), "the key a is there twice"
  (at the map), the serializer's "the string isn't valid UTF-16 (a lone surrogate)" (a value or a key), and
  a record whose type refuses its values: an `ArgumentException` from its constructor or setters, with its
  message, at the record's path (so an `FCurve` refusing its keys is reported at the curve,
  `$.tracks[0].locationX`, its message naming the key). Other exceptions from a record's construction
  propagate. A node of the wrong kind or CLR type for its place (a `DraftList` where a string goes, a
  `float` where a `double` goes, a record draft of another type or a type the union doesn't list) is the
  caller's mistake: an `InvalidOperationException` naming the path.
- **Problems and paths** (`RecordPath.cs`): `RecordProblem(Path, Message)`, its text as
  `RecordFormatException.Message` writes it ("message (at $.layers[0].length)", the message alone at the
  root). `RecordPath`: `Root` (the whole entry: "$", no segments), `Field(name)` (a code name made camelCase,
  so `Field(nameof(Layers))` works), `Index(i)`, `Key(text)` (a key's text: a string as it is, an integer in
  decimal, an enum member by its JSON name, written JSON-quoted as the serializer writes it), `Append`,
  `Segments`, `Depth`, `IsRoot`; equal when their text is. A union's value and a nullable add no segment.
- **Checks** (`RecordChecks.cs`, P20): `RecordChecks.Check(record, data?, references?)` walks a built value
  through the getters: each record, wherever it is held, gets its type's own checks (the unions it is a
  subtype of first, then its own; one type's in the order of their method names), then field by field the
  marks' checks and the nested records'. A type's own check is `[Check] static void Name(T value,
  CheckContext check)` (on any record type, nested ones too; the first parameter's type may be a base the
  record derives from); anything else marked `[Check]` throws when the type's checks are first found.
  `CheckContext`: `Data` (the `VirtualFileSystem` to check against, null when checked alone: checks across
  entries run only with it), `References`, `Report(message)` and `Report(path, message)`, paths relative to
  the value checked. The generic checks: a number outside its limits ("must be above 0 and at most 1 (it is
  2)"), a string not among its choices ("\"x\" isn't one of the choices: a, b"), and, only when an
  `IReferenceLookup` is given (`KindAt(path)`: the record name of the record entry there, or null), a name
  its rule can't store ("\"an imp\" can't name a creature in creatures"), one with no entry ("no sound
  effect \"boom\" (audio/effects/boom)") and one whose entry is another kind ("… isn't a creature: it holds a
  test.part record"). A display name inside a sentence has its first letter made small unless the second is a
  capital (an acronym). A draft is checked once it builds; while it doesn't, only its build problems exist.

Choices the doc left to the step (recorded here; none changes the game): the type and member names above;
drafts immutable (P13's history is a list of drafts); `Missing` in a field that isn't required means its
default, as an absent JSON key does; limits with exclusive ends (today's checks need them: "above 0 and at
most 1"), NaN never inside, integers compared as doubles, on numbers and lists and maps of them but not on
vectors' components; a colour also on lists of them; `Bulk` on any field (a single value too, for step 6's
node transforms), its summary formatted with the count of a list, map or byte array, else with the value;
a reference names its kind by type (`typeof(EffectPatch)`), its folder and rule defaulting to the kind's
`Kind` mark; conditions only on one string or enum field, with disjoint values and at most one mark without
a condition, which holds when none of the others does (when none holds, the strings name nothing); name
rules as types; a type's own checks found on any record type and run wherever its values are held; the
generic checks on built values only.

For later steps: step 5 adds the music's rule (a `NameRule` over `MusicNames.ToPathName` and
`FromPathName`) and marks `LayerSpec.Instrument` as `[References(typeof(PercussionSpec), When =
nameof(Type), Is = ["hit"])]` with an unconditional `[References(typeof(InstrumentSpec))]`, and the drums'
keys with `Keys = true`. Step 6: a sequence's `Stages` are whole paths exactly one level below
`models/stages` (`PathNames` accepts deeper ones; the sequence's own check refuses them), a particle cue's
effect any depth below `models/effects` (`PathNames`); an `FCurve` that refuses its keys is reported at the
curve's path, so a finer path (the key) needs the curve's rules as a check the form runs on its keys. Step 10:
the `VirtualFileSystem` has no public way to tell a record entry's kind without loading it as a type (step 2
added none), so the lookup comes from what the session knows: its entries as classified when opened (step 8;
a record's kind is `RecordSerializer.RootTypeName` of its binary form, as its `IMountEntries` gives it) and
the base's.

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

### Step 3: the window, the sound and the file browser's model (engine)

Code in `Engine/Statventure/Platform/Window.cs`, `Core/Application.cs`, `Audio/AudioSystem.cs` and two new files,
`Storage/FileBrowser.cs` and `Storage/RecentPlaces.cs`; tests in `Tests/Statventure.Tests/Platform/WindowDropTests.cs`,
`Core/ApplicationCloseTests.cs`, `Core/ApplicationGamePacksTests.cs`, `Core/AudioReloadTests.cs` (`TheSwapIsPublic`)
and `Storage/FileBrowserTests.cs` (with `FileBrowserDocumentsTests`). What the editor's steps use:
- **Drops (steps 8 and 12; M11-9, P27):** `Window.PathsDropped` (`event Action<IReadOnlyList<string>>`), raised
  from `Window.PollEvents` inside GLFW's drop callback (BRUTAL's `OnDrop`), so on the loop's thread before the
  frame's input and `Update`: the paths in GLFW's order, as GLFW gives them (it reads the drag's `text/uri-list`,
  turns `file://` URIs into paths and passes any other URI on as it is, both percent-decoded, in `src/init.c`'s
  `_glfwParseUriList`; so a listener checks what each is). No position and nothing while dragging (GLFW has
  neither). A drop isn't an input event: `Input` and the UI never see it, and nothing listens in the game or the
  sandbox, where a drop does nothing as before.
  GLFW 3.4 (the system library the engine loads on Linux; BRUTAL's own `glfw3.dll` on Windows) implements drops on
  X11 (XDND) and on Wayland (the data device; BRUTAL's comment "unimplemented on Wayland" is GLFW 3.3's). An
  exception from a listener is held by BRUTAL and comes out of the next `PollEvents`.
- **A close the application can refuse (step 11; M11-17):** `protected virtual bool Application.ConfirmClose()`,
  asked when the window's close flag (`Window.CloseRequested`, which GLFW sets when the player closes the window)
  is found set: before the next frame, and while the window is minimized after the update, the two places the loop
  always ended. True (the default) ends the loop exactly there, as before; false clears the flag and goes on (while
  minimized, back to sleep until the next wake-up or event), so each request is asked once and the next close asks
  again. `Quit()` doesn't ask (`Running` is checked first). The editor answers false while it shows its "unsaved
  changes" dialog; when the player then chooses to close, it calls `Quit()` (or sets `Window.CloseRequested` and
  answers true).
- **The editor's own data (P6; added on the manager's word, 2026-11-06):** `ApplicationOptions.GamePacksOnly =
  true` starts the application with `Application.Data` holding the game's area packs only: no `classes/`, no mods
  folder, no development folder (neither `DevelopmentFolder` nor `$STATVENTURE_DEV_FOLDER`), so a damaged mod in
  the player's mods folder (or a missing development folder) can't stop the editor's start; `ReloadData` re-reads
  the same mounts. The default (false) is the game's data as before; the game and the sandbox don't set it.
  `classes/` is left out too, as P6 says (confirmed by the manager, 2026-11-06).
- **For the step that wires `EditorApplication`** (step 4 left both to a later step, "How it works", step 4):
  its options set `GamePacksOnly = true` (the no-mods start step 4 found it needed), and it overrides
  `ConfirmClose` (closing with unsaved work asks: step 11).
- **The music swap (step 13; P31, P35):** `AudioSystem.Reload(MusicLibrary)`, S9-D's swap, is public as it was:
  the new library's rooms prepared on a worker, the theme playing restarted through music.js's switch when its
  music changed (its theme, or any instrument, percussion voice or room, by binary form) and playing on otherwise,
  the library's new or changed effect patches registered. The preview gives it `MusicLibrary.Load` of the data as
  edited (the custom mount, step 2); on the game's thread, as every call.
- **The file browser's model (step 8; M11-9, P26)** (`Statventure.Storage`; the game's Load Character can use it
  later with its own screen):
  - `FileBrowser.Places(modsFolder, recent?)`: the mods folder given (the editor's `Application.Storage.ModsFolder`,
    so a game folder of the options' own is followed; listed before it exists), Documents
    (`DocumentsFolder.ForThisMachine()`: Electron's rule, which makes it when it is missing; left out when it can't
    be made), home (left out when not known), the roots (`FolderListing.Roots()`), then the recent places that are
    folders now; each a `BrowserPlace(Kind, Folder)` (`BrowserPlaceKind`: `Mods`, `Documents`, `Home`, `Root`,
    `Recent`), the screen naming them. The editor adds the game's packs folder itself (P5). A pure overload takes
    the folders.
  - `FileBrowser.List(folder, endings?, showHidden)`: `FolderListing.List`'s entries, folders first, then files,
    each by `NameOrder`; files only those whose names end in one of the endings in any case (`Shows`: as the mods
    folder's `.pak` and the importers' endings are read; none given: every file), every folder (to go into); hidden
    ones only when asked. `NameOrder` is the same on every system: ignoring case (ordinal on the upper-case forms),
    names equal so by their ordinal order ("Readme" before "readme"). A folder that can't be read lists as empty.
  - `FileBrowser.ReadPath(folder, typed)`: a `TypedPath(FullPath, Kind)`: the text relative to the folder shown
    (".." and "." resolved) or a full path, `~` and `~/…` (Windows also `~\…`) in the home folder, no separator at
    the end; `TypedPathKind`: `Folder` (go there), `File` (pick it), `New` (nothing there but its folder is: a new
    file's name, for an export), `Missing` (nor its folder; or a separator at the end naming no folder), `Invalid`
    (not a path here; `FullPath` is the text). Nothing typed reads as the folder shown.
  - `FileBrowser.Complete(folder, typed, endings?, showHidden)`: a `PathCompletion(Text, Matches)`: the last name
    typed (after the last separator) completed among what `List` shows in the folder the text before it reads as,
    the names starting with it as the system compares names (`NameComparison`: case matters on Linux, not on
    Windows), hidden ones too when it starts with a dot; the text is the typed folder part as typed and the longest
    start the matches share, a separator after a single folder; no match (or a folder part that isn't a folder):
    the text unchanged, no matches.
  - `RecentPlaces`: the folders used lately, most recent first, at most `Limit` (10), each once (as the system
    compares names), as a full path without a separator at its end; `Add(folder)`; `Folders` is what the caller
    stores (the editor in its `editor/` folder, G6) and gives back to the constructor next time (entries that
    aren't paths here are dropped there). Folders that are gone stay listed (a drive may come back); `Places`
    leaves them out.

How it is checked: the drop callback the window registered with GLFW, taken from GLFW and called by the test as
GLFW calls it on a drop (the window and the paths as UTF-8 strings, one with a space and one not ASCII), on a
hidden window: the paths in order, every listener, one removed, none (then nothing, no exception at the next poll,
no input event). The close on a real application in a hidden window (nothing shown), made to count as minimized
(`Window.MinimizedForTests`, internal) so the loop presents nothing and runs its minimized branch; a close is
requested as the player's is, by GLFW's flag, in an update (the check after it) or in `Load` (the check before the
next frame, which also follows every frame): the default ends where it always did (no update after a request in
`Load`; none after the update that brought it, and without sleeping first: a minute's wake-up asked, a watchdog
that would wake it after 20 s); refusals at both places keep the loop running with the flag cleared, the next
accepted one ends it; each request asked once; `Quit` ends without asking, also with a request pending, at the
check after an update and at the check before the next frame (the request and the `Quit` made by another thread
while the loop sleeps between minimized updates). `GamePacksOnly`: the mounts it leaves (the area packs, with
`classes/`, a mods folder, the development folder by option and by variable all there), and a real start with a
damaged mod and a development folder (the data without either, before and after `ReloadData`; without the option
the damaged mod stops the start as always, and once it is gone the data has the mod and the development folder).
The swap: the reload tests' renders (S9-D's) are its tests, and `TheSwapIsPublic` keeps it public. The browser on scratch
folders, its places for this machine through the run's redirected folders, and Documents that can't be made (the
run's config folder pointed for a moment at a `user-dirs.dirs` naming a folder under `/proc`) left out.

Not checked here (by hand): a real drop from a file manager onto a window under X11 and under Wayland, and on
Windows; closing a real window by its title bar and from the taskbar while minimized (the tests set GLFW's flag
as the close callback does). The rendering branch's own frames with a refused close aren't run (no window is shown
in tests): it has no check of its own, the check before the next frame being the one `Load`'s request goes
through.

Found, and handled in this step on the manager's word (2026-11-06): the tests that made windows before it opened
visible ones (`WindowOptions.Visible` is true by default, the game's window), so every full run on the desktop
showed small windows for a moment: `TestApplication` (`ApplicationReloadTests`, `ApplicationStorageTests`),
`ApplicationMusicTests`' own application, the swapchain test in `GpuDeviceTests` and `UiResourcesTests`. **Tests make
their windows hidden now** (`Visible = false`; `engine/CLAUDE.md`), but for one; the default is unchanged. Neither X11
nor Wayland shows a hidden GLFW window (GLFW 3.4 doesn't map it on X11 and gives it no shell surface on Wayland), and
it still gives what these tests need: a surface and a swapchain that acquires and presents a frame
(`TestApplication`'s and `ApplicationMusicTests`' one frame each), a host for `UiResources`. Checked with those tests
and this step's on the GPU and on lavapipe, under Wayland and under X11 (Xwayland: `WAYLAND_DISPLAY` unset, which
GLFW's libdecor messages confirm), all passing, with a read-only KWin script watching the real desktop's compositor:
it listed the windows open when loaded and logged none added during any of the four runs
(`claude-work/m11/step3/hidden-windows.sh`, `window-watch/`); and inside a headless wlroots compositor (`cage`,
`hidden-windows-cage.sh`). **The swapchain test stays visible:** it presents five frames with vsync (FIFO), and a
hidden window's surface, which no compositor shows, isn't paced the same everywhere: KWin lets the frames through,
wlroots (cage) never lets the second one through, so the test hangs there; hiding it would make it check a
compositor's leniency rather than the swapchain, and presenting without vsync would check less. Its window is the
one a run on the desktop still shows (320×200, five frames). And a hidden window presenting *beside* it stalled it
too: under wlroots, with `ApplicationMusicTests`' window hidden and the two run in parallel, the swapchain test hung
in 5 of 10 runs (GPU and lavapipe), 0 of 10 with that window visible (GLFW is one for the process and not
thread-safe); so `SwapchainTests` and `ApplicationMusicTests` joined the non-parallel "Window" collection (no test
changed otherwise), and every test that presents to a window now runs alone: 10 of 10 runs of all the window tests
passed in cage. And `FolderListing`'s comment
said a folder that can't be listed throws `UnauthorizedAccessException`; with its `IgnoreInaccessible` an unreadable
folder lists as empty: the comment is corrected (no behaviour change).

Choices the design left to the step, all confirmed by the manager (2026-11-06): the names above; the drop as an
event of the window, not an input event (`Input.cs` unchanged), its paths passed on as GLFW gives them; the close
asked through a protected method on `Application` (`ConfirmClose`), once per request, the flag cleared on a refusal,
`Quit` not asking; the option's name `GamePacksOnly`, `classes/` left out with the mods and the development folder
(P6); the swap made public under its name `Reload`; the test hook `MinimizedForTests`; the places' order (mods,
Documents, home, roots, recent) and what each does when it isn't there; names ordered ignoring case with an ordinal
tie-break, the same on every system (not by the culture, not "natural" with numbers by value); endings in any case
and folders never filtered; `~` for the home folder; a separator at the end naming a folder; `New` for a name in an
existing folder; completion's shared start, the separator after a single folder, hidden names on a leading dot, case
as the system; ten recent places, kept even when gone, left out of the places.

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
   of 50 % and 300 %; remembered since step 8, with the window's size, in `editor/shell.json` (the manager's decision
   (a), 2026-11-06; "How it works", step 8: `EditorView.ZoomLevel` is set at start).
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
  (the manager, 2026-11-06):** a worker of its own on the painter fixes it. **Fixed (2026-11-06, branch
  `lcd-first-text`; Engine-Plan.md Progress, "Text first in a paint chunk"):** each text drawing takes the
  verdict of the chunk its display item joins, as Chromium rasters it; the status line is LCD as in Chromium. Against
  Chromium (pixels past 12 levels, the GPU; lavapipe within a pixel or two of it): `shell` 435 → 0 at 1×, 843 → 28 at
  1.65; `dialog` 389 → 57 at 1×, 598 → 110 at 1.65. What is left is rounded corners' antialiasing (the top bar's
  buttons at 1.65, the dialog's box and its focused OK button; M5's known differences), no text. The goldens are
  Chromium's captures and stay as they are.
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

### Step 5: audio kinds: checks and marks (engine)

The audio side in full: `Docs/Audio.md`, "Checks and marks". Code: `Engine/Statventure/Audio/Music/ThemeCheck.cs`
(new: `ThemeCheck`, `MusicParts`), `Audio/Music/ThemeData.cs` and `Audio/Effects/EffectPatch.cs` (the marks and
the kinds' `[Check]`s), `Audio/Music/MusicLibrary.cs` (`MusicNameRule`; the room's and the order's rules shared
with loading as `RoomProblem` and `OrderProblems`, their messages unchanged), `Score.cs` and `Composer.cs` only
exposing (three of the score's functions made internal; `ComposerNames`; the composer's texture table internal).
Tests: `Tests/Statventure.Tests/Audio/ThemeCheckTests.cs`, `AudioMarksTests.cs`.
- **Each kind's check, with paths (P20):** `audio.effect`: the patch's rules at the field each names, in its old
  words, and `Problem()` (what registering and loading call) now the first of `RecordChecks.Check`'s problems, so the
  game's messages are as they were and it enforces the declared limits (G4). `audio.theme`: the check against the
  composer (G5; the rules, their order and what isn't checked: `Docs/Audio.md`), given the data through the
  `CheckContext` for the room and the names its layers play, else only the score and the layers' own rules.
  `audio.theme-order`: loading's rules at the id each names (with the data, each id's entry). `audio.instrument`,
  `.percussion`, `.room` have none of their own (as before: they fail only when a theme plays them, which the theme's
  check covers at the theme's layer).
- **Marks:** kinds with their folders and `MusicNameRule` (the music's rule as a `NameRule`, as step 1 left it); help
  on every audio kind and field (M11-21: plain English, what the field does for someone making a mod); units;
  choices on a theme's mode, a layer's type and texture and an instrument's kind; references (six fields, below);
  limits only on the effect patch (G4: the four numbers its check held). No colours, no bulk fields (none in audio).
- **References (M11-11):** `ThemeSpec.Room` → rooms; `LayerSpec.Instrument` → instruments when its type is one that
  plays an instrument (pad, drone, arp, bass, pulse, strum, mel, line, drips), percussion voices when it is `hit`,
  nothing otherwise (tex and drums read no name); `LayerSpec.Drums` keys → percussion voices (`Keys = true`);
  `HarmonySpec.Instrument`, `DoublingSpec.Instrument` → instruments; `ThemeOrder.Themes` → themes. Each takes its
  kind's folder and rule.
- **Loading (G4, G5): the declared limits and, by the user's decision of 2026-11-07, the theme check.**
  `MusicLibrary.Load` runs what it ran, first and with the same messages, and through `EffectPatch.Problem()` the
  declared limits (which are those rules): everything that loaded before still does (the committed `audio.pak`, the
  tests' data, the proof cutscene's three patches). Then each theme is checked against the parts the library read, in
  path order, the first problem thrown as the file system's error naming the pack and entry, its text "message (at
  path)"; the themes compiled on the way are the library's, so the warm-up and the first theme played don't compile
  them again. The step measured the added time first, as its condition said, and stopped; the manager brought it to
  the user, who decided on 2026-11-07 (the question tool) to wire it, with these numbers shown (Release, the committed
  `audio.pak` and `ui.pak` mounted as the game mounts them, `MusicLibrary.Load` alone, no sound device, this PC):

  | | Before | With the check |
  | --- | --- | --- |
  | Cold start (15 fresh processes, medians) | 48.9 ms, and 17.2 ms of compiling all 55 themes a moment later (the audio system's warm-up thread, or the first theme played) | 69.6 ms, the compile included, none later |
  | Warm (200 loads in one process, each from a collected heap, medians) | 1.4 ms (3.4 ms of compiling later) | 4.5 ms |
  | Reload (remount and load, medians) | 3.8 ms (3.4 ms of compiling again when the theme playing changed) | 8.6 ms |

  So 20.7 ms more at the start on the game's thread and 3.5 ms more work in all, about 3 ms more warm and 5 ms more
  on a Reload; compiling the 55 themes takes 1.9 ms warm, the whole check 2.0 ms. The user accepted both consequences
  the step named: a layer that never sounds (no bar of its cycle, a pattern of rests) naming an instrument that isn't
  there is refused too (`ThemeCheckTests.ASilentLayerNamingAnUnknownInstrumentIsRefused`); and four of
  `MusicLibraryTests`' libraries, whose sample theme doesn't compile (its melody's bars hold 3 of the meter's 12
  steps) and whose instruments weren't in them, are given a playable theme (`PlayableTheme`, every field still set)
  and the instruments it plays, the record fixtures keeping the sample, whose refusal at load a test shows. Before,
  a theme that didn't compile failed the audio system's warm-up thread at the start (an exception on a background
  thread ends the process); now loading stops naming its entry, and a Reload keeps the music it had.
- **Duplicates the editor will meet (step 10):** a declared limit and the kind's own rule report one mistake twice
  at one path (the patch's "has a layer 1 that needs a length above 0 …" and the limit's "must be above 0 (it is 0)"),
  and so do a choice and the theme's check ("Unknown mode blues" and "\"blues\" isn't one of the choices: …") and,
  given a lookup, a reference and the theme's check ("Unknown instrument theremin" and "no instrument \"theremin\"
  (audio/instruments/theremin)"); the kind's own words come first. Step 6's kinds do the same.

Choices the design left to the step (named here, decided by the step): the names (`ThemeCheck`, `MusicParts`,
`ComposerNames`, `MusicNameRule`, the kinds' display names); `LayerSpec.Instrument` marked for exactly the types
that play an instrument or a percussion voice, rather than an instrument unconditionally (step 1's suggestion), so a
tex or drums layer's name names nothing, as the composer reads none; the room marked as a reference, so the editor
(given a lookup) flags a room that isn't there, and `""`, where the game plays the hall; help on every audio field;
the units above, none for octaves; the theme checked by what each layer's type reads, not by whether a note is
reached, and only in the sections the form plays; the score compiled once and walked only when it fails; words of
the step's own for what fails today without words (a meter of one number, an empty form, a loop point outside it,
a first bar of `%`, a strum or an empty pattern: "a strum layer without a pattern", in the composer's form).

Found against step 1's API: a kind's check isn't told its entry's path (`CheckContext` has the data and the
lookup only), so the theme's rule that its id is the one its path stores (and that the order is at
`audio/theme-order`) can't be a `[Check]`; it stays in `MusicLibrary.Load`, and the editor (step 10) checks it
itself or step 1's `CheckContext` gains the path.

For later steps: step 6's `AudioCue.Name` can reference `typeof(EffectPatch)` and `typeof(ThemeSpec)` with no folder
of its own (these kinds bring theirs and the music's rule). Step 10: the duplicates above; the theme's id against its
path; a theme's check with the data loads each instrument it names through the custom mount (once per check), and
an instrument entry that isn't one is reported at the layer in the file system's words. Step 13: `MusicLibrary.Load` on
the custom mount checks and compiles every theme (the game's 55 in about 2 ms warm), and a theme that doesn't pass
refuses the whole library, so the preview keeps its last valid library then (P35).
### Step 6: the scene, model, particle, terrain, state machine and stylesheet kinds (engine)

Code: `Engine/Statventure/Scene/Sequence.cs`, `Scene/FCurve.cs`, `Render3D/Particles.cs`, `Render3D/ModelData.cs`,
`Render3D/Terrain.cs`, `Animation/StateMachineDefinition.cs`, `UI/Css/StylesheetRecords.cs`, `UI/Style/Stylesheet.cs`.
Tests: `Tests/Statventure.Tests/Scene/EngineKindMarksTests.cs` (the marks, G4, what loads today, references, bulk),
`Scene/SequenceDataCheckTests.cs` (the sequence against the data), and every existing rule's test with its path
(`SequenceRecordTests`, `FCurveTests`, `ParticleTests`, `ModelValidationTests`, `ModelCameraLightTests`,
`TerrainPackTests`, `StateMachineTests`, `StylesheetCompileTests`). The marks as recorded:
`Tests/Statventure.Tests/Data/Records/Marks/<kind>.txt` (one per kind, its nested types with it; a missing file is
written by a run with `STATVENTURE_RECORD_FIXTURES=1`, a changed mark fails until the file is written again, as a
layout does).

- **Each kind's check with paths (P20).** The rules each kind kept are now its `[Check]` method (`Rules`; the
  stylesheet record's `Sheet`), reporting every problem with the words it had at the path of what it names, in the
  order it met them. The rule for a path: the field the message is about; the record for a relation or the whole
  ("names a palette and a fill", "has no curve", "plays for no time", "has neither conditions nor an exit time");
  a message about several fields at each field that breaks its part ("its scale and repeat must be above 0, its
  blends not negative" at `scale` and `blendIn` when both are wrong; both colours; grow-in and fade-out). The old
  entry points give the first problem, with their exception types: `Sequence.Validate()`, `ModelData.Validate()`,
  `TerrainData.Validate()`, `StateMachineDefinition.Validate()` throw the first of `RecordChecks.Check(value)`,
  `ParticleEffect.Problem()` returns its message; so the kind's own rules come first and the declared limits after
  them, and the messages the game logs are as they were (every existing rule's test checks both). Two kinds can't
  go through `RecordChecks` this way: an `FCurve` refuses its keys when it is made, so its rules are
  `FCurve.ProblemsOf(keys)` (paths within the curve, `$.keys[2].leftTime`; the constructor throws the first), for a
  form to put below the curve's own path (step 1's build reports the curve); and `Stylesheet.FromRecord` builds the
  sheet as it checks, so it reads in two modes (`RecordReading`: without a list it throws the first problem as an
  `InvalidDataException`, as before; with one, `Stylesheet.ProblemsOf`, each problem goes in at its path and its part
  is left out), the record's `[Check]` reporting those.
- **Going on past a problem.** A check reports every problem it can tell apart: the sequence's parts (a role, a stage,
  each check of a track's field, a strip, a cue, a span) stop at their own first problem and the rest is checked on;
  a model's, a state machine's and a terrain's rules each report and go on, skipping what would read past an index an
  earlier rule found broken (a node showing a mesh that isn't there, a state defined twice that a transition with an
  exit time comes from) or what has nothing to compare with (a grid of no points); a model reports one index past
  its vertices, one joint index past its skin per primitive and once a channel's times that don't increase (each
  message names one); a stylesheet's rule, keyframe and font face stop at their first problem, a style rule's
  declaration at its own; a curve's keys are each checked in full.
- **The paths per kind:** a sequence: `$.name`, `$.duration`, `$.roles[i].name|kind`, `$.stages[i]`, a track's field
  (`$.tracks[t].node`, `.actor`, `.mark`, `.target`, `.host`, `.stage`), a strip's (`.strip.clip`,
  `.strips[i].scale`), a cue's (`$.tracks[t].cues[i].time`, `.marker`, `.parameter`, `.kind`, `.value`, `.palette`,
  `.fill`, `.name`, `.effect`, `.node`, `.burst`, `.rate`, `.duration`, `.volume`, `.pan`, `.pitch`, `.action`,
  `.element`; a camera cut's `$.tracks[t].cuts[i].camera`), the two-marks rule at each such track's `.mark`, a span's
  (`$.reduced[i].from|to|hold`, `$.reduced` for none); a curve's key fields; a particle effect's fields, its shape's
  and spawn's (`$.shape.thickness`, `$.spawn.halfAngle`), a range itself or its `min` (`$.life`, `$.life.min`); a
  model's node fields (`$.nodes[i].children[k]`, `.parent`, `.mesh`, `.skin`, `.camera`, `.light`), `$.lights`,
  `$.roots[r]`, a primitive's (`$.meshes[m].primitives[p].normals`, `.indices`, `.indices[k]`, `.material`,
  `.joints[v]`), a skin's (`.joints[k]`, `.inverseBindMatrices`, `.skeleton`), a channel's (`.node`, `.values`,
  `.times`), a camera's and the sun's values (`$.cameras[c].verticalFieldOfView`, `$.lights[0].angle`); a terrain's
  `$.width`, `$.depth`, `$.heightSamples`, `$.covers`; a state machine's `$.parameters[i].name`, `$.layers`, a state's
  `.name` or `.speedParameter`, a layer's `.entry`, a transition's `.from`, `.to`, `.duration`, `.exitTime`, a
  condition's `.parameter`, `.compare`, `.value`, or the transition; a stylesheet's selector tokens
  (`$.rules[i].selectors[j]`), selector, media tokens, a declaration's `.property`, `.shorthand`, `.value` or token
  (`.value[j]`, a url), a keyframe's `.offsets` and declarations, a font face.
- **The sequence against the data (G5).** Given the data (`CheckContext.Data`) and keeping its own rules, the
  sequence's check reads its stages from it (one the data hasn't is `Sequence.Load`'s words at `$.stages[i]`, a broken
  one its own error naming its entry), checks the names in them when all are there, and checks its particle effects
  and sounds as `Play` checks them against what is bound and registered (`SequenceChecks.AgainstData`): each particle
  cue's effect there and keeping its rules (`ParticleEffect.Load`), each audio cue's effect patch
  (`audio/effects/<name>`) and theme (`audio/themes/<id>`), named by the music's rule (`MusicNames`), there as records
  of their kind; at the cue's `effect` or `name`, in the sequence's words ("the sequence jiruan, track 10 (audio): cue
  0 plays the theme great-imp-jiruan, which isn't in the game's data (audio/themes/great-imp-jiruan)"), an entry that
  is there but broken with its own error (reported in the editor; given `reportBroken: false`, thrown as it is,
  naming its entry, as a broken stage is). Each entry is read once per check. **In `Sequence.Load` too** (the user's
  decision of 2026-11-07, below), after its stages' names.
- **Marks.** Kinds (P9; a kind's folder is the one its loader reads it from in the data today, the committed packs
  and the tests' data, the manager's rule of 2026-11-06): `scene.sequence` "Cutscene" in `world/cutscenes`,
  `particle.effect` "Particle effect" in `models/effects`, `model` "Model" in `models` (a stage is a model in
  `models/stages`, named by its whole path through the sequence's reference), `terrain` "Terrain" in `world/terrain`
  (the test terrain packed at `world/terrain/test`), `ui.stylesheet` "Stylesheet" in `ui/styles`, and without a folder
  `animation.state-machine` "State machine" (no committed data holds one: the tests' machines are built in C# and
  written by the tests) and `scene.curve` "F-curve" (curves live inside sequences); all with plain names; display
  names "Track" (the union), "Animation track" (`ActorTrack`, the record `scene.animation-track`) and "Key"
  (`scene.key`). Help texts (M11-10, the kinds with a preview first): every field and type of a particle effect and
  of a cutscene's records (the sequence, roles, spans, strips, every track and cue, curves and keys), and every kind;
  units on the same (s, /s, m, m/s, m/s², rad, °, °/s, px) and the sun's angle. Limits (G4), each one a rule of the
  kind's own already holds, the minimum or maximum excluded where the rule's is: a sequence's duration (above 0), a
  span's from, to and hold, a strip's clip start and end, blends (0 or more), scale and repeat (above 0), influence
  (0–1), a particle cue's burst, rate and duration, an audio cue's volume (0 or more), pan (−1–1) and pitch (above 0),
  a key's amplitude; a particle effect's opacity, grow-in and fade-out (0–1), ink line and drag (0 or more), a ring's
  thickness, a spark's, puff's and shard's proportions (above 0, at most 1), a spark's stretch and a sphere's radius
  (0 or more), a cone's half angle (0–180); the sun's strength (0 or more) and angle (0, below 180); a terrain's
  width and depth (1 or more): 34 fields. Not declared, as their rules let NaN through (a limit never contains NaN):
  a transition's duration and exit time; nor where the rule depends on another field (a camera's values, by its
  projection; a range's ends, by which field holds it); nor on a stylesheet (its rules are bulk). Bulk (P21, M11-14):
  a model's meshes, skins, roots, channels and its nodes' parent, children, local matrix, translation, rotation,
  scale, mesh, skin, camera and light (the hierarchy's indices and transforms); a terrain's height samples and
  covers; a stylesheet's style rules, @keyframes and @font-face rules. Colours: a material's base colour and emission,
  a palette cue's fill. References: a sequence's stages (models in `models/stages` by their whole path), a particle
  cue's effect (particle effects in `models/effects` by their whole path, `PathNames`), an audio cue's name (a sound
  effect when its kind is `Effect`, a theme when `Music`, by step 5's `MusicNameRule`; marked once rebased on step 5).
- **The game's loading (G4, G5).** The declared limits are enforced wherever the kinds' checks run, since
  `Validate()`/`Problem()` are the first problem of `RecordChecks.Check`: in `Sequence.Load` (alone and with its
  stages), `ParticleEffect.Load`, `ModelData.Load` and the models importer, `TerrainData.Load`,
  `StateMachineDefinition.Load` and `StateMachineBuilder.Build`, `SequenceBuilder.Build`, `SceneView.Play` and an
  emitter's start; a limit can't fire before the kind's own rule there (`EveryDeclaredLimitIsHeldByTheKindsOwnCheck`),
  so nothing that loaded stops loading and no logged message changes. `Stylesheet.Load` runs no `RecordChecks`: no
  limit is declared on a stylesheet, and the walk would cost more than reading it (below). **`Sequence.Load` also
  checks the sequence's effects and sounds against the data** (G5; the user's decision of 2026-11-07, below): after
  `Validate(stages)`, the first of `SequenceChecks.AgainstData(sequence, files, reportBroken: false)` is a
  `DataFileException` naming the sequence's pack and entry ("not a valid sequence: the sequence jiruan, track 10
  (audio): cue 0 plays the theme great-imp-jiruan, which isn't in the game's data (audio/themes/great-imp-jiruan)"),
  and an effect, patch or theme there but broken is its own error naming its entry, as a broken stage's is.
- **The loading time measured (2026-11-07, the PC, 8 cores, idle: load about 1).** `Sequence.Load` of the proof (the
  committed JSON form from `Data/Cutscenes`, its stage's record in a pack made from `jiruan.glb`, 85,167 vertices,
  `audio.pak` mounted), Release, three copies of the engine: `main` (the branch's base, `42fdd90`), the branch (the
  limits through `RecordChecks` in `Validate`, the data checks not wired) and the branch wired. A scratch bench outside
  the repo (`claude-work/m11/step6/bench/`: `Program.cs`, `setup.sh`, `run.sh`, `run-game.sh`, results beside them).
  *Cold*, one load in a fresh process, 30 runs each, interleaved, medians: 137.1, 159.0 and 191.6 ms (the limits
  +21.9 ms, the data checks +32.6 ms, both +54.5 ms). *Cold after the game's other loading* (the music library and the
  two particle effects loaded first in the process, as the game has them before a cutscene plays): 110.3, 122.0 and
  122.7 ms (the limits +11.7 ms, the data checks +0.8 ms, both +12.4 ms). *Warm*, 500 loads after 10, three runs,
  medians: 12.06, 12.15 and 12.24 ms (+0.08 and +0.09 ms, both +0.17 ms). The limits' cold cost is the first
  `RecordChecks` of a sequence and of a model in a process (their shapes made, their getters and checks compiled);
  the data checks' cold cost is mostly reading an effect patch and a theme for the first time, which the game's
  music library has done before a cutscene loads.
- **What the wiring would have refused that loaded before:** the proof's own data, as the sandbox's cutscene mode
  (`Samples/Statventure.Sandbox/Cutscene.cs`) and `JiruanIntroTests` built it (the stage's pack and `Data/Cutscenes`, no
  `audio.pak`), held no theme `great-imp-jiruan`; and the test sequence's load test (`SequenceRecordTests`) had neither
  its particle effect nor its two sounds nor its theme in its data. The game itself mounts `audio.pak` with its other
  packs. The options put to the user: wire it in and give those loaders the sound pack; wire it in but check a music
  cue's theme only when the data holds music; keep it out of loading, the editor only.
- **The user's decisions (2026-11-07, the question tool, with the measurement above shown: cold 137 → 159 → 192 ms,
  +12 ms once the music is loaded, warm +0.2 ms):** (1) the declared limits and the effects-and-sounds check go into
  loading (`Sequence.Load`, as above); (2) the loaders that built the proof's data without `audio.pak` get the sound
  pack, as the game mounts it: the sandbox's cutscene mode mounts `packs/audio.pak` beside it (below the stage's pack
  and the proof's folder; nothing else of it changed) and `JiruanIntroTests` the committed `audio.pak`
  (`JiruanIntro.SoundPack`); the test sequence's load test holds its effect, its sounds and the sound pack. So the proof
  loads and the check holds everywhere. Checked: the sandbox's cutscene mode run headless at volume 0 (`claude-work/m11/`
  `step6-sandbox-proof.sh`: "sound: great-imp-jiruan and jiruan-rift-opens, jiruan-flash, jiruan-title-card at volume 0",
  played, frame 30 captured; the per-user folders pointed at a scratch folder), and every test loading
  `Data/Cutscenes` passes.

**Decided by the manager on step 6's report (2026-11-06):** the same broken value reported twice (a limit and the kind's
own rule; a reference mark and the sequence's data check) stays so, and the editor shows the first problem per field
(step 10's entry under "Steps"); `AudioCue.Name` marked with step 5's rule once both are merged (step 10's entry;
in the end marked here, once rebased on step 5, where it was trivial); a kind's folder is the one its loader reads it from in the data today, none when there is no such folder
(the editor's New then can't make one, as for `mod.info`); `RecordChecks` walking every nested record (about 61 ms on
the compiled game stylesheet, Debug) is an API gap left for step 1's author (a layout could note whether any check
or mark applies below a field), so no check is wired into `Stylesheet.Load`; the sequence's effects-and-sounds check
went to the user (answered 2026-11-07, above).

Choices the doc left to the step (made here): the path rule above and going on past a problem; the check methods'
names; `FCurve.ProblemsOf` and `Stylesheet.ProblemsOf` (internal) with their paths; the sequence's data check only once
its own rules hold, its missing-entry words in its own style; which fields get units beside the help texts; the bulk
summaries ("{0} meshes", "{0:N0} height samples", "a matrix, as imported" for a node's local matrix, the value itself
for a node's indices and transforms); the display names "Cutscene", "Track", "Animation track", "Key", "F-curve"; the
marks guard as text files per kind.

A stylesheet's url check leaves @font-faces' urls to `FontFaceDescriptors.Of`: the one url a face the engine reads can
hold is its src, which that checks by the same rule, so a face refused for its src isn't reported twice. Changed in step 9's `EngineKindsFormTests` (the same as in step 1's): its filler gives a bulk field
its object, and a kind whose fields are all bulk (a compiled stylesheet) is shown but not edited (P21). Changed in step
1's tests (their own words anticipated it): `RecordShapeTests` expects the engine's F-curve's display
name from its mark, "F-curve" (it was made from the code name, "F curve"); `RecordDraftTests`' helper filling a
blank draft's required fields gives a bulk field its object (an empty list or the type's default), as a draft holds
one, now that the engine's kinds have bulk fields.

### Step 7: the mod's info entry (engine)

Code: `Engine/Statventure/Packs/ModInfo.cs`. Its layout `Tests/Statventure.Tests/Data/Records/Layouts/mod.info.txt`
and fixtures `Data/Records/Fixtures/mod.info/` (`every-field`, `empty`), guarded as every kind's are
(`RecordLayoutTests`); tests `Tests/Statventure.Tests/Packs/ModInfoTests.cs` (`ModInfoTests`, and
`ModInfoModsTests`, which reads the log and so runs alone, collection "Mod info log").
- **The record (G1, M11-15):** `ModInfo`, the engine's kind `mod.info`, version 1: `Name`, `Author`, `Version`,
  `Description` (fields 1–4; JSON `name`, `author`, `version`, `description`), each a `string` whose default is
  empty text. Any of them may be empty; a key left out of a JSON form written by hand reads as empty text; the
  writer writes all four (Q33). No check of its own and no loader: the game never reads it, and G4 declares no
  limit for it. It is found with the engine's other kinds by its `[Record]` attribute (`RecordTypes.Engine`,
  which the dump, `extract`, `AddFolder` and the packer use): that attribute is the registration, there is no
  list of kinds to add it to. Public, for the editor and a later mod list.
- **Its path:** `ModInfo.Path`, `mod/info`. The committed packs hold nothing there or in `mod/` (a test), and
  no folder the game lists (only the music's, under `audio/`) holds it, so the entry never stands in for any of
  the game's data.
- **What the game does with it: nothing.** Mounting reads a pack's index, not its records, so a mod with the
  entry mounts as without it; the game reads its data by its own paths and lists only the music's folders; the
  log names each mod by its file. `ModInfoModsTests` shows it on the game's mounts (`Application.DataMounts`)
  over copies of the committed `audio.pak` and `ui.pak`, a `classes/` folder and two mods (a theme at another
  tempo and a class; an effect patch): the "Data" lines logged while mounting, loading the music and reloading
  (as `Run` and `ReloadData` log them), every other path with its kind, origin and content, and the music
  library are the same with the entry in both mods, in one and in neither. An entry there that the engine can't
  read as a mod's info (a newer version, damaged bytes, a file, a record of another kind, bytes that aren't a
  record) changes nothing either, and reading it (`Load<ModInfo>`) is an error naming the mod and the entry, as
  for any part of a mod (G2 as today). In the file system the entry is there as any mod entry: the topmost
  mod's at `mod/info`; each mod file holds its own, which is what a mod list reads later.
- **What step 11 uses:** `writer.AddRecord(ModInfo.Path, info)` when it writes a mod; a mod's own info read
  from its pack: `RecordSerializer.FromBinary<ModInfo>(pack.Read(ModInfo.Path))`, after `pack.Find(ModInfo.Path)`
  (null for a mod without one, which still loads, M11-15). The pack's dump and `extract` (`mod/info.json`) show
  it as any record, and packing that folder again gives the same pack.

Choices the design left to the step (named here, decided by the step): the type's name `ModInfo` and the
constant `ModInfo.Path`; the fields' numbers in the design's order (name 1, author 2, version 3, description 4)
and their names; each field a non-null `string` defaulting to empty text, rather than `required` (a hand-written
info would have to give all four keys, and a blank draft would show all four as empty required fields, P15) or
nullable (`string?`, "not given" apart from empty, a "set" checkbox per row in the form); no check and no loader;
no helper to read a pack's info (the two calls above). **Confirmed by the manager (2026-11-06):** each of the four
fields a plain `string` defaulting to empty text (not `required`, not `string?`), for the reasons above; the game
never reads the kind, so it is an editor choice; the names and "no helper" confirmed too. A later change to the
defaults is a version 2.

With step 1's shapes and drafts (rebased on it): `mod.info` passes the tests that go over every engine kind and
fixture (`RecordDraftTests`: its blank draft, with no required field, builds to the same binary form as the JSON
form without keys, four empty strings; its two fixtures read into drafts build back to the same binary form). It
has no marks, and no `[Kind]`: without it the kind's display name is made from the code name, "Mod info", and it
has no folder, which is right, as a mod has one info at the fixed path `mod/info` rather than entries named in a
folder (a folder `mod` would let P15's New make `mod/<name>` entries); step 11 edits it in a form of its own.

### Step 8: opening packs, the browser, drops and the entry list (2026-11-07)

**Files.** `Tools/Editor/Session/EditorSession.Opening.cs` (the session's opening, `OpenPack`), `Tools/Editor/Settings/`
`EditorSettings.cs` (the `editor/` settings files: `EditorSettings`, `ShellSettings`, `RecentPlacesFile`),
`Tools/Editor/Browser/` (`FileBrowserPart.cs`, `FileBrowserDialog.cs`: the file browser; `Opener.cs`: Open, drops, the
unsaved-work question, the close; `Browser.css`), `Tools/Editor/Entries/` (`EntryList.cs`, `Entries.css`), and the two
hooks in step 4's files: `Shell/EditorView.Keys.cs` (`ZoomLevel`, settable, and `ZoomChanged`) and `EditorApplication.cs`
(the window's size from the settings, `GamePacksOnly`, `ConfirmClose`, drops, the settings written at close). Tests in
`Tests/Statventure.Tests/Editor/Opening/`: `OpeningTesting.cs` (scratch games and packs, each kind of unreadable entry),
`SessionOpeningTests.cs`, `EntryListTests.cs`, `BrowserTests.cs`, `OpenerTests.cs`, `SettingsTests.cs`,
`EditorApplicationTests.cs` (a real application in a hidden window, the "Window" collection), `OpeningScreens.cs` (the
fixtures `list-marks`, `list-unreadable`, `browser`); goldens `Golden/editor-list-marks@1.png` and `@1.65.png`,
`editor-list-unreadable@…`, `editor-browser@…`.

**Decided by the manager (2026-11-07), at this step's questions:**
- *The settings files:* one small JSON file per concern in `<player folder>/editor/`: `recent-places.json` (what
  `RecentPlaces.Folders` holds) and `shell.json` (the page zoom level and the window's size). Read tolerantly: a missing,
  damaged or foreign-version file is ignored and written again on the next save; written whole and swapped in, like the
  game's `settings.json`. Later steps add their own files there (the stand-ins, the volume).
- *The browser is a dialog over the shell* (step 4's dialogs: the rest inert, the focus kept inside, Escape cancels), not
  a pane: Open, New mod's folder, Save As and export all use the same dialog, and the three panes keep their space.
- *The window size saved is the last normal size at close*, at least the shell's smallest (800 × 500); not taken while
  maximized or full screen (the last normal size seen stays); applied at start through `EditorApplication`'s
  `WindowOptions`.

**The session's opening** (`EditorSession.Opening.cs`; no DOM):
- `Session.PlayerFolder` (the application's `Storage.Folder`, which follows `--player-folder`; by default this machine's
  game folder, in tests the run's redirected one), `ModsFolder`, `SettingsFolder` (`<player folder>/editor`, G6).
- `Session.GamePacks`: the area packs in `Game.PacksFolder` that are there, in the game's order (`Application.AreaPacks`);
  another `.pak` there isn't the game's. `IsGamePack(file)`.
- `Session.Open(file)` (`PackFile.Open`, which throws for a pack it refuses: nothing changes) and `Session.Open(PackFile)`
  (the session owns it): every entry of the pack and of the game's other packs read and classified by
  `EditorSession.ReadEntry` (public, for step 10's lookup and step 11): a record of a kind `Session.Types` knows
  (`EditorEntry.ForRecord`), a file (`ForFile`, its content length), or unreadable (`ForUnreadable`) with the reason in
  the words of the pack's dump: "the content doesn't match its hash (damaged)", "not a readable record: …" (the binary
  form doesn't start as a record), "a record of type x, which isn't a record type known here", "not a readable x record:
  …" (a newer version than the engine reads, damaged data, or values its type refuses: an `ArgumentException` from its
  constructor, e.g. an `FCurve`'s keys out of order). The pack's entries over the game's (step 9's `ShowEntries`).
- `Session.Pack` (`OpenPack`: `File`, `Name`, `IsGamePack`, `IndexHashHex` as opened, for step 11's P23 check,
  `ReadOnlyReason`, `Writable`, `Unreadable`), `Session.PackFile` (kept open for step 11's `AddFrom`), `Session.Base` (a
  `VirtualFileSystem` of the game's other packs in the game's order, for steps 10 and 13's custom mount on top),
  `Session.GamePackProblems` (a game pack `Open` refuses is left out of the list and the base and named here),
  `PackOpened`. The previous pack's file and base are closed; the session closes the last with itself.
- **Read-only** (`Session.Writable` false, `ReadOnlyReason`): any entry it can't read ("an entry can't be read", "3
  entries can't be read"; M11-16), or one of the game's packs outside the dev setup ("the game's packs open read-only
  (only the repo's dev setup saves into them)"; M11-5, P5). In the dev setup a game pack opened takes changes, and the
  game's other packs' entries are shown beside it read-only (copying one would put its path in two of the game's packs);
  over a mod, the game's records can be edited (the mod's copy, P7).
- **The import queue** (drops, P27; step 12 implements import): `Session.QueueImports(files)` (in order; `ImportsQueued`
  told when any came), `PendingImports`, `TakeImports()` (the queue emptied), `CanImport` (something listens).

**The editor's settings** (`Settings/EditorSettings.cs`): `EditorSettings.Read(file)` gives a file's values or null (a
file that is missing, can't be read, isn't JSON, isn't an object, repeats a key or has another `"version"` than 1);
`Write(file, values)` writes `{ "version": 1, … }` indented by two spaces into `<file>.tmp`, flushed, then moved over the
file (the `editor/` folder made, mode 0700 on Linux); a failure is logged and gives false. `ShellSettings(ZoomLevel,
Width, Height)` in `editor/shell.json`: a zoom level the shell can't have (not a half step, past 53–299 %) or a size that
isn't a whole number from 1 is left out (the default), a size under 800 × 500 made the smallest, value by value.
`RecentPlacesFile` in `editor/recent-places.json` (`"folders"`, strings only; step 3's `RecentPlaces` drops what isn't a
path here). The zoom: `EditorView.ZoomLevel` (set at start, `IsZoomLevel`, `ZoomChanged`).

**The program** (`EditorApplication`): `OptionsFor(shell, playerFolder, visible)`: the window 1280 × 800 unless
`shell.json` has a size (read before the window opens), at least 800 × 500, hidden in tests, no sound, the player's
folder, **`GamePacksOnly = true`** (step 3). `Load`: the session's player folder is `Storage.Folder`, the view's zoom level
the file's, the window's drops go to the opener. Each update keeps the window's size while it is normal (not iconified,
maximized or full screen); `Unload` writes `shell.json` (that size and the zoom level). **`ConfirmClose`** asks the
opener: with unsaved work it shows the question and refuses (the loop goes on), once however often the window is closed
meanwhile; Discard quits.

**Opening from the view** (`Browser/Opener.cs`, a part without a pane): the Open command (Ctrl+O, the top bar's Open)
asks the browser for a `.pak` and opens it. `OpenFile(file)`: a pack `Open` refuses is said in a dialog, "x.pak can't be
opened: <why>." (the problem without the paths; "the file isn't there"), and nothing else changes; over unsaved work it
asks first, "Unsaved changes: a.pak has changes that aren't saved. Discard them and open b.pak?", Discard and Cancel —
**step 11 adds Save** by setting `Opener.SaveFirst` (`Func<Task<bool>>`: offered first; true goes on). The status line
says "Opened x.pak: n entries", with ", read-only: <why>" when so. **Drops** (`Drop(paths)`, P27): the first `.pak` (any
case) opens (asking first over unsaved work), then the other files are queued for import in their order; further packs
aren't opened (one pack at a time, P6), and what isn't a file here (a folder, a URI GLFW passes on, a missing file) is
passed over; the status line says each ("…; 2 files waiting for import; not opened: drops, b.pak."; "waiting" only while
nothing takes imports). Every action starts as a task of the document's loop (`RunScript`), so what follows a dialog
runs there.

**The file browser** (`Browser/FileBrowserPart.cs`, `FileBrowserDialog.cs`; M11-9, P26): a dialog over the shell, 760 px
wide (`.dialog.browser-dialog`). `Pick(BrowserRequest)` gives the file picked (a full path) or null; one browser at a
time (another request gives null). `PickPack()` (".pak", button Open); `PickExportFile(title, name)` (any file, a new
name allowed, button Export: **the JSON tab's export picker**, step 9's `IExportFilePicker`). `BrowserRequest`'s
`Endings`, `NewFile`, `SuggestedName`, `StartFolder` and `Fallback` place serve New mod's folder, Save As and import.
- *Places* (left, buttons, the folder as tooltip): Mods, Game packs ("Game packs (read-only)" outside the dev setup; P5:
  the editor adds it after the mods folder), Documents, Home, the roots, then "Recent" and the recent places that are
  folders now (step 3's `FileBrowser.Places`), the current one marked.
- *Where it starts:* the request's folder, else the most recent place that is a folder, else the request's fallback place
  (the mods folder to open, Documents to export), else the first place there. A start folder that isn't there yet (the
  mods folder before a mod is saved) is shown empty with "This folder isn't there yet."; going to a folder that isn't
  there later says so and stays.
- *The listing:* step 3's `FileBrowser.List` (folders first, then the files the endings show, hidden ones with the
  "Hidden" box), a row per entry: the name (a folder's with "/"), a file's size (bytes below 1 KB, then KB, MB, GB, TB of
  1,024, one decimal below 10: "1.5 KB", "12 bytes") and date ("2026-11-07 16:50", this machine's time zone). Over it
  Up (disabled at a root) and the folder named from the deepest of the mods folder, the game's packs, Documents and home
  that holds it ("Mods / old"), else its full path. The listing is one stop for Tab (focused first, unless a name is
  suggested): arrows, Home and End select (Up from none: the last), Enter or a double click opens a folder or picks a
  file, Backspace goes up; a press selects; the selection is scrolled into view.
- *The path field:* Enter (or the button with text typed) reads it (step 3's `ReadPath`): a folder is gone to (the field
  emptied); a file the endings show is picked ("x isn't a .pak file." otherwise); a new name is picked when the request
  allows it; else "x isn't there." or "x isn't a path here.". Tab completes its last name (`FileBrowser.Complete`) when
  that changes the text, else moves the focus as usual; Shift+Tab never completes. The button with nothing typed and
  nothing selected says "Choose a file." ("Type a file name." to export). An export's suggested name is in the field,
  its name before the ending selected.
- *Picking:* an export over a file that is there asks "Replace the file?" first (the browser inert meanwhile); the dialog
  closes with the file, its folder becomes the most recent place and `editor/recent-places.json` is written. The recent
  places are read from that file when the browser is first asked for them.
- Seams for the screens' fixture: `PlacesOverride`, `ListOverride` (a listing of the fixture's own, so no path of this
  machine's is in its markup), `TimeZone`.

**The entry list** (`Entries/EntryList.cs`, the entry pane; P8):
- *Head:* the pack's name (bold) and its state ("mod", "game pack", "read-only"; "No pack open" before one is), and a
  notice when there is something to say: how to open a pack (none open), "Read-only: <why>.", each unreadable entry as
  "path: reason" (red), each game pack that couldn't be read. Then the filter (`input type=search`, "Filter").
- *The tree* (`role=tree`, one stop for Tab): the session's entries by path, folders first then entries, each by its
  name (ordinal); a folder row with a disclosure triangle (`display: list-item; list-style: inside disclosure-closed`,
  `disclosure-open` when open, as a summary's), an entry row with its name, its marks and its kind's name
  (`EditorEntry.KindName`: the kind's display name, "File", "Unreadable"); indented 14 px a level; `aria-level`,
  `aria-expanded`, `aria-selected`. When a pack opens its top folders are open, the rest closed; open folders are kept
  across changes; an entry added or renamed, or selected, opens the folders that hold it.
- *Marks:* ↑ the mod's own copy of a game entry (`Session.IsModCopy`), + added (a new entry), • changed (`IsChanged` or
  renamed), ! problems ("A problem", "3 problems"), × unreadable (its reason as tooltip); the game's entries not copied
  dimmed (`--muted`), an unreadable entry's name red. Rows follow `EntryChanged`, `EntriesChanged` and `SelectionChanged`.
- *Keys and pointer:* an active item (outlined while the tree has the focus) apart from the selection (the form's entry,
  ember-pale): Down and Up move it (Down from none: the first), Home and End, Right opens a folder then goes to its first
  item, Left closes an open folder (not while filtering) or goes to the item's folder, Enter selects an entry or opens and
  closes a folder, letters type ahead (Blink's list box: within 1 s of each other a text; the next item whose name starts
  with it, ignoring case, from the one after the active for a first letter, going round). A press focuses the tree (its
  rows are no stops of their own); a click makes the item active and opens it as Enter does. Closing a folder that holds the active item makes the
  folder active. Ctrl+F (`EditorCommand.Filter`) focuses the filter, its text selected.
- *Filter:* entries whose path or kind's name holds the text (ignoring case), their folders all open; Escape empties it;
  Down goes into the tree; "No entry matches the filter." when none does.
- *Rows only where seen (P37).* Measured first (Release, a pack of 2,000 entries in 20 folders over the repo's packs, the
  editor at 1000 × 640; `claude-work/m11/step8/ListBenchmark`, a scratch program like `Tools/UiBenchmark`): with every row
  an element, opening took 214 ms, but painting a frame with the 2,021 rows of an open tree took 109 ms (the painter draws
  every row of the scroll container, seen or not), and typing in the filter or adding an entry 99–106 ms; so the tree is
  now a flat list of its shown items of which only those in the pane's view and 10 on either side are elements, between
  two spacers as high as the rows they stand for (rows 24 px, `EntryList.RowHeight`), made again when the pane scrolls or
  the window resizes, and the active item scrolled into view as Blink's "nearest" does. Then: opening 34–41 ms, a frame
  1.4 ms with 2,021 rows shown, the filter 2.8 ms, an added entry 4.2 ms, a selection 4.8 ms, an arrow key 3.8 ms, a
  commit's marks under 0.1 ms.

**Screens against Chromium (P36):** `list-marks` (1000 × 640: a mod over the repo's packs with a copy of the game's hall,
its own crypt changed, a new room with problems, a game room changed in the session, the crypt selected and active in the
focused tree, the form showing it), `list-unreadable` (1000 × 800: a pack with an entry of each unreadable way, read-only,
the notice with every reason, the game's audio closed) and `browser` (1000 × 640: Open over the empty editor, a mods
folder of the fixture's own with a folder and three packs, a row selected, the listing focused). Every editor fixture
shows the entry pane, so this step's list changed the shell's, the dialog's and step 9's screens too: all eight were
captured again (`claude-work/m11/step8-capture.sh`: make-editor-references.sh's harness, flags and fonts inside a
headless cage with its own Xwayland, the output made 2400 × 2400, one fixture per Chromium run; no window on the user's
screen); the dialog's box is pixel for pixel the committed capture's. Pixels past 12 levels (the limit 0.2 %), GPU:
`list-marks` 0 at 1× and 28 at 1.65 (0.002 %), `list-unreadable` 0 and 28 (0.001 %), `browser` 28 (0.004 %) and 147
(0.008 %); the others as before: `shell` 0 and 28, `dialog` 57 and 110, `form-json` 0 and 28, `form-controls` 0.108 % and
0.139 %, `form-controls-end` 0.115 % and 0.155 %.

**Found:**
- *The capture harness's window was out of X11's range at 1.65.* `Tools/PaintReference` placed its window at -20000 DIPs,
  which at a forced scale of 1.65 is -33000 px, past X11's 16-bit window positions: under a headless cage's Xwayland such a
  window isn't managed and never gets the focus, so the 1.65 captures had no focus rings (`:focus`, `:focus-visible`;
  the dialog's OK button, the tree's active item, the browser's listing), while 1× and the user's desktop (step 4's
  captures) had them. Fixed in the harness: -10000 DIPs (-16500 px at 1.65), off every screen all the same; the
  captures are otherwise the same (the dialog's box at 1.65 now pixel for pixel step 4's capture on the desktop).
- *The painter paints every row of a scroll container, seen or not* (no cull rect as Blink's): 0.05 ms a row on the GPU,
  which is what made P37's list slow. The engine wasn't changed; the editor's list makes only the rows in view.

**Choices the design left to the step** (made here): the names above; the marks' glyphs for a copy (↑) and an unreadable
entry (×) (in Lato, which has no ▸ or ⚠); folders first, then entries; the top folders open when a pack opens; an active
item apart from the selection (arrows move it, Enter or a click opens it); type-ahead as Blink's list box; the filter
matching paths and kinds' names; the browser's 760 px width, its places' labels, the listing's columns and size and
date formats, the start folder's order, Tab completing in the path field only when it changes the text, the replace
question for an export; a refused pack said in a dialog; the unsaved-work question's words (Discard, Cancel; Save first
once step 11 sets it); a drop's first `.pak` opened and the rest passed over; the status line's words; the session's
reasons in the dump's words; a game pack that can't be read left out and named; in the dev setup the other game packs'
entries read-only beside an open game pack; the settings files' format (`{ "version": 1, … }`, two-space indents) and the
`ShellSettings` and `RecentPlacesFile` names; the list's rows only where seen with 10 rows of overscan.

### Step 9: the form, undo and the JSON view (2026-11-07)

**Files.** `Tools/Editor/Session/`: `EditorEntry.cs` (the working entry), `EditorSession.Drafts.cs` (the session's
entries, selection, commits and histories, New, Duplicate, Rename, Remove, Revert). `Tools/Editor/Form/`:
`FormPart.cs` (the form pane: head, tabs, Undo and Redo, the JSON tab; `IExportFilePicker`), `FormView.cs` (one
entry's rows, problems and typing state), `ValueEditors.cs` (a control per value shape, and `Rows`), `Slot.cs`
(where a value sits in a draft), `ValueText.cs` (numbers, colours and start values as text), `EntryActions.cs` (the
entry dialogs, F2 and Delete), `Form.css`. Tests in `Tests/Statventure.Tests/Editor/Form/`: `FormTesting.cs` (the
helpers and `ScratchExportPicker`), `SessionEntriesTests.cs`, `FormEditingTests.cs`, `FormUndoTests.cs`,
`EntryActionTests.cs`, `JsonTabTests.cs`, `EngineKindsFormTests.cs`, `FormVariants.cs` (a union whose subtypes share a field name with two kinds of value, in the variants' namespace), `FormScreens.cs` (the fixtures `form-controls`,
`form-controls-end` and `form-json`); goldens `Golden/editor-form-controls@1.png` and `@1.65.png`,
`editor-form-controls-end@…`, `editor-form-json@…`. Two tests of step 4's written before the editor had parts of its
own now look at their own parts among the editor's (`Shell/ShellTests.cs`, the parts' order; `EditorFixtureWriterTests.cs`,
the fixture list), as step 8's parts will need too.

**The session's entries: the API step 8 (and steps 10, 11, 13) use.** All in `Statventure.Editor.Session`; no DOM.
- **An entry** is an `EditorEntry`, made by whoever lists entries: `EditorEntry.ForRecord(path, record, source,
  readOnly = false)` (the record as `RecordSerializer.FromBinary` gives it, of any version), `ForFile(path, size,
  source)`, `ForUnreadable(path, reason, source)` (both always read-only); `source` is `EntrySource.Pack` (the open
  pack's) or `EntrySource.Game` (a game pack's, the base the open pack is seen over); `EntrySource.New` entries are
  made only by the session. `readOnly` is for a game entry that can't be copied into the open pack (P7: the step
  that lists decides). An entry has `Path` (changed by a rename), `SavedPath` (the path in its pack; null for a new
  one), `Source`, `Content` (`Record`, `File`, `Unreadable`), `IsRecord`, `Kind` (the `RecordShape`; null for files
  and unreadable entries), `KindName` (its display name, "File", "Unreadable"), `Name` (in its kind's folder by its
  rule, else the last path segment), `Saved` (the record as read), `SavedDraft`, `Draft` (the current step of its
  history), `Build` (the current draft built), `Builds`, `LastBuilt` (the last value that built: the JSON tab's and
  the preview's last valid state), `Problems` (the draft's build problems while it doesn't build, else what the
  session's checks give), `History` and `HistoryPosition`, `CanUndo`, `CanRedo`, `IsChanged` (a new entry always; a
  record whose current draft isn't the saved one and doesn't build into the saved record's binary form in the
  current versions: an edit taken back by another isn't a change), `IsRenamed`, `ReadOnly`, `FileSize`, `Reason`.
- **The set:** `Session.ShowEntries(entries)` replaces it (the open pack's entries and the game's; where both have a
  path, the pack's is shown and the game's kept under it, `Session.Overridden(path)`); it clears the selection and
  the removed list and reports `EntriesChangeKind.Shown`. `Session.Entries` (shown, by path, ordinal),
  `Session.Entry(path)`, `Session.Removed` (the pack's entries removed since: saving leaves them out). `Session.Types`
  (the record kinds known; the engine's by default) and `Session.Writable` (the open pack takes changes: a mod, or in
  the dev setup a game pack; false with no pack or one that can't be fully read, M11-16; set by step 8 when it opens
  a pack). Nothing can be edited, added, renamed, removed or reverted while it is false.
- **Selected and shown:** `Session.Select(entry or null)` (one of `Entries`); `Session.Selected`;
  `Session.SelectionChanged`. The form shows the selected entry; the list (step 8) selects on a click or Enter and
  highlights on `SelectionChanged` (the session also selects: New and Duplicate select what they made, Remove clears
  the selection, Revert moves it to the game's entry).
- **Editing:** `Session.CanEdit(entry)` (writable, a record, not read-only; a game entry too: its first change makes
  the mod's own copy, P7); `Session.Commit(entry, draft)` (one step of the entry's history; false when it is the
  current draft), `Session.Undo(entry)`, `Session.Redo(entry)` (M11-7: only that entry), each reported by
  `Session.EntryChanged`. Checks (step 10): `Session.AddCheck((entry, built) => problems)` (run when an entry's draft
  changes and builds) and `Session.Recheck(entry)` (run them again, e.g. after a pause; reported as changed).
- **New, Duplicate, Rename, Remove, Revert** (not undoable; M11-7, P14, P15, P7). The session's methods act at once:
  `Session.Add(path, draft)` (a New entry, `Source` New), `Session.Duplicate(entry, path)` (a copy of its current
  draft), `Session.Rename(entry, path)`, `Session.Remove(entry)`, `Session.Revert(entry)`; with `Session.CanAdd`,
  `Session.CanRenameOrRemove(entry)` (the pack's own and new entries; never a game entry, P7), `Session.IsModCopy(entry)`
  (an entry of the pack over a game entry, or a game entry changed in this session), `Session.CanRevert(entry)`,
  `Session.PathProblem(path, except)` (the path rule, an entry there, an entry that is a folder of it or below it, in
  words), `Session.NewKinds()` (every plain kind of `Types` by display name; unions offer their subtypes). A rename
  or removal of the pack's entry over a game entry shows the game's again; a revert drops the copy (the pack's goes to
  `Removed`) and shows the game's with its history cleared. Every change of the set is reported by
  `Session.EntriesChanged` with an `EntriesChange(Kind, Entry, OldPath)`: `Shown`, `Added` (made, or a game entry
  shown again), `Removed`, `Renamed` (with the old path).
- **Asking first** is the view's: `View.Part<EntryActions>()` has `New(kind?)`, `Duplicate(entry)`, `Rename(entry)`,
  `Remove(entry)`, `Revert(entry)`, each showing its dialog and acting only when the user confirms (giving the entry
  made, or whether it acted). Step 8's list calls these (from its buttons, menus or keys), never the session's
  methods directly; call them from the editor's own listeners (a task of the document's loop), where what they do
  after the dialog closes runs. F2 and Delete already ask for the selected entry (Delete only when the key isn't in
  the form's rows, whose controls own it). Other parts add to these dialogs through `EntryActions.Asking` (an
  `EntryDialog`: its action, entry and dialog, and `Confirmed`, actions run after the session's change with the new
  path, null for a removal): step 10's referrers (P19).
- **Unsaved work:** the entries register `Session.HasUnsavedEntries` (an entry changed, renamed or added, or one of
  the pack's removed) as a source of the title's "•" when entries are first shown or added. Saving (step 11)
  resets what it saved in a file of its own (the classes are partial).

**The form** (`FormPart`, the form pane; nothing shows while no entry is selected). The head: the entry's path,
its kind's name, "read-only" when it can't be edited, and Revert to the game's (shown only for the pack's own copy
of a game entry), Duplicate, Rename, Remove (each enabled as the session allows). Two tabs (Form, JSON; P22; the
arrow keys move between them; the tab chosen stays when another entry is selected). A record entry's rows come from
its kind's shape (P11) and are brought up to date in place after every change of the draft (`FormView.Update`): a
group is rebuilt only when its structure changes (a list's or map's count, a union's subtype, a nullable set or
unset), so focus, open groups and a field's text survive. A file says its size; an unreadable entry its reason.
- **Rows:** a leaf's row is `div.row` with a line (`div.line`: the name, the controls, tools); a record, union, list,
  map (or a nullable one of them) is a group, `details.group` (open) with the same line as its summary and its rows
  indented under it. Each row has `data-path`, its path as problems name it (`$.layers[0].length`; a map's value by
  its key's text). Names are the fields' display names (M11-10), their help as the name's tooltip, units after the
  numbers; list elements are named by index from 0 (as the engine's messages count them).
- **Controls** (P11): a bool a checkbox; a number a number field (`step` 1 for whole numbers, `any` for floats; `min`
  and `max` its kind's range and the field's limits) with a slider (`input type=range`, out of the tab order) when the
  field's limits are both finite (M11-10); a string a text field, with a swatch before it for a colour mark; a string
  with choices and an enum a select (a value not among them shows as "x (not a choice)", "3 (not a member)"; the
  empty choice as "(empty)"); a vector 2–4 number fields (X Y Z W), a colour vector a swatch and `#rrggbb`
  (`#rrggbbaa` for a Vector4, `#rrggbb` keeping alpha 1; 255 steps, rounded to the nearest, clamped); byte arrays,
  quaternions, matrices and bulk fields read-only text (a bulk field its `Summarize`, P21; a list of matrices can't
  grow); a nullable a "set" box before its value's controls ("none" when unset); a record its rows (one not given
  yet: Make, its blank draft); a union a select of its subtypes and the chosen one's rows (switching keeps the values
  of fields of the same JSON name and value signature); a list its elements with Up, Down, Remove and Add at the end;
  a dictionary its entries, the key in the name's place, with Remove and Add; ordered pairs with Up and Down too. A
  value made by the form starts as `ValueText.Start`: zero, false, "", the first member, an empty list or map, a
  record's blank, a union's first subtype's blank, and a nullable as its value's start, not null; a map's new key is
  "" (then "new", "new 2" …), the next integer above the largest, or the first member not used.
- **Commits** (P12, P13): a text field commits on its change event (Enter, leaving it, a spin button's release, an
  arrow key's step); its text is read in the invariant form (`ValueText.TryNumber`: a point, an exponent allowed;
  whole numbers in their kind's range; a float32 not past its range); text that doesn't read stays in the field,
  marked (`bad`, a red border) with why under its row ("\"300\" isn't a whole number from -128 to 127", "needs a
  number", "\"#12\" isn't a colour (#rrggbb)"), and isn't committed; the same value typed another way ("0.10") is no
  commit, and the field shows it as the form writes it (the shortest round-trip text). A slider moves the field's
  text while dragged and commits once on release; a checkbox, select, set box and the list and map buttons commit at
  once. Enter in the text field of a required value not given yet gives it (an empty text too). A field's text is
  put back from the draft only when the draft's value there changed (undo, redo, another commit), so text that
  doesn't read survives other commits.
- **Undo and redo** (M11-7, P13): the shell's Undo and Redo (Ctrl+Z, Ctrl+Y, Ctrl+Shift+Z, the top bar's buttons)
  are the selected entry's history, enabled while it has a step to go to and can be edited. A key in one of the
  form's fields that has typing of its own is left to the field: Undo while its text isn't the committed one, Redo
  while it has undone its own typing since (or has typing not committed); the field's own editing undo does it (the
  document's undo stack, Blink's). The top bar's buttons always act on the entry (the field commits as it loses the
  focus first).
- **Problems** (P16; step 10 adds its checks through `Session.AddCheck`): under their row's line in red (a group's
  before its nested rows), the row's name red; a path without a row of its own under its nearest row above (a bulk
  field's element); the whole entry's at the form's top; and why a field's text doesn't read, under its row.
- **The JSON tab** (M11-18, P22): `RecordSerializer.ToJson` of the draft when it builds, else of the last one that
  did ("The entry doesn't build now: this is the last version that did.", in ember), or nothing for an entry that
  never built; at most the first 2,000 lines shown (`FormPart.JsonLines`; "N more lines: copy or export the entry for
  all of it."), in the editor's Lato (not monospace: the fonts the Chromium comparison has). Copy writes it all to the
  document's clipboard (the system's in the program, `UiHost`; tests: the document's own stand-in), with a status
  line. Export asks an `IExportFilePicker` for a file (`PickExportFile(title, suggestedName)`: "Export <path>",
  "<name>.json") and writes it all there; a write that fails says why in a dialog. The picker is `FormPart.ExportPicker`
  when set, else the first part that implements the interface: **step 8's file browser implements
  `IExportFilePicker`** (any ending, M11-9); until then Export is disabled ("Export needs the file browser").

**Screens against Chromium (P36):** the form with every control kind and a problem, in two halves of one window
(`form-controls` and `form-controls-end`, 1280 × 1400: a `test.kinds` record with a value in every kind of field and
an element in a few lists and maps, its required text taken out so "needs a value" shows; the second with the form
pane scrolled by 1,020 px, short of its end so a pixel of height can't move it), and `form-json` (1280 × 800: the JSON
tab of audio.pak's `audio/instruments/harp`, Export enabled through a picker set in the fixture). Captured with
PaintReference, `make-editor-references.sh`'s harness and flags, inside a headless `cage` (its own Xwayland: no
window reaches the user's screen): at 1× the repo's script with the headless output made 2400 × 2400 first
(`wlr-randr --custom-mode`; cage made a window larger than its 1280 × 720 output that size, cutting the capture),
at 1.65 one fixture per Chromium run with the output as it is (a 1.65 run there stalls after its first fixture);
`claude-work/m11/step9-capture.sh` and `step9-capture-scale.sh`. Both ways give the shell's committed goldens pixel
for pixel at 1 and 1.65. One fixture as tall as the whole form (2,460 px) stalled at 1.65 in any one run, hence the
two halves.

**Choices the design left to the step** (made here, the manager's word on those marked so, 2026-11-06): the names
above; the session API's shape (the entries' set with the game's kept under the pack's, `Writable`, the events, the
asking methods on `EntryActions` and the `Asking` hook); New offering every plain kind until kinds have folder marks
(confirmed by the manager); Export disabled until the file browser exists (confirmed); the JSON tab at most 2,000
lines, copy and export giving all (confirmed); Delete only from outside the form's rows (confirmed); list elements
numbered from 0 (confirmed); a red of the editor's own for problems, `--problem: #e5735f` (the game's dark colours
have none; confirmed); "Revert to the game's" (P7) built here on the data side and as the head's button (the
manager's decision, 2026-11-06; step 11 wires it to saving); changed meaning a different binary form (an edit taken
back isn't a change); a field's own typing undone first while its text isn't the committed one; Enter giving a
required value not given yet; a new map key's and a started value's rules; the rows' look (names 140 px, rows 26 px,
groups open, nested rows indented with a line); the dialogs' words ("That can't be its path: …").

**Found** (reported to the manager, the engine not changed):
- *A select with author padding is 6–7 px wider in the engine than in Chromium.* The form's selects (`padding: 0 4px;
  border: 1px solid; height: 22px; font: 13px Lato`, `appearance` left auto) measure 79, 67 and 80 px in the engine and
  72, 61 and 73 px in Chromium (`form-controls` at 1×: the Shade, Huge and Words selects, their borders found in the
  render and the golden), so what follows a select on its line moves too. Within the limits (`form-controls` 0.108 % at
  1× and 0.139 % at 1.65, `form-controls-end` 0.115 % and 0.155 %, mostly this); a rule of the menu list's intrinsic
  width to find (the engine's own UI, not this step's): an open item in `Docs/Engine-Plan.md`, "Small tasks left from
  earlier milestones", and in M5's known differences.

### Step 10: checks and references in the editor (2026-11-07)

**Files.** `Tools/Editor/Checks/` (`EntryChecks.cs`: the checks, no DOM; `ChecksPart.cs`: the pause on the document's
clock), `Tools/Editor/Session/EditorSession.Checks.cs` (`Session.Checks`, `Session.EntriesWithProblems`),
`Tools/Editor/References/` (`EntryReferences.cs`: `SessionReferences`, `ReferenceAt`, `EntryReferences`;
`ReferenceListPart.cs`: the reference field's list; `ReferrersPart.cs`: the referrers in the rename and remove dialogs;
`References.css`). It builds on step 13's `Session.EditedData` (the manager's decision of 2026-11-07: step 13 owns the
data as edited, this step reads it and makes none of its own). Tests in `Tests/Statventure.Tests/Editor/Checks/`:
`CheckTesting.cs` (mods in the run's scratch folder opened over the repo's packs, the pause passed on the document's
clock), `EntryChecksTests.cs`, `ChecksViewTests.cs`, `EntryReferencesTests.cs`, `ReferenceListTests.cs`,
`ReferrersTests.cs`, `CheckVariants.cs` (two record types in the variants' namespace, which the layout guard leaves
out: a kind whose check throws, references in a map with enum keys), `ChecksScreens.cs` (the fixtures `checks-problems`
and `references-list`); goldens `Golden/editor-checks-problems@1.png` and `@1.65.png`, `editor-references-list@…`.

**The manager's decisions (2026-11-07), at this step's questions:**
- *Q-A, what a removal's update is (P19, M11-11):* (a) the remove dialog lists the referrers and offers "Point them
  at:" a select of the removed entry's kind's other entries (the game's and the pack's, each named by the first
  referrer's mark), "Leave them" first and the default; left ones show their problems once the checks across entries
  run (P18). A rename: the list and "Update them to the new name", ticked by default. (Not taking the references out;
  not listing only.)
- *Q-B, what shows between a commit and the pause (P17):* (a) after a commit the entry's own problems are fresh at
  once, and what the last run found across entries stays shown until the next run replaces it (stale for at most the
  pause; nothing flickers). (Not dropping them until the pause.)
- *Confirmed with them:* the checks across entries go over every record entry, the game's too (a mod's broken
  instrument breaks a game theme, which the game would refuse: M11-12), the cost measured below; "the first problem per
  field" is per path; the music's load-time rules outside the kinds' checks as below; the reference list attached to
  the form's documented markup (a row's `data-path`, its text input) without changing the form, open while the field
  has the focus.

**The checks** (`EntryChecks`, `Session.Checks`; M11-12, P16, P17). Started the first time `Session.Checks` is asked
for (the view's `ChecksPart` asks when it is attached; a session without a view when its user asks), through step 9's
`Session.AddCheck`. Each record entry that builds shows:
- *its own problems*, worked out at every change of its draft (each commit, undo and redo, at once; P17's "the entry's
  own check per commit") and again at once when it is renamed: `RecordChecks.Check(value)` with no data and no lookup
  (the kind's checks, its marks' limits and choices) and the rule the music's loading keeps that only the entry's path
  tells: a theme in `audio/themes` must hold the id its path stores, at `$.id`, in `MusicLibrary.Load`'s words ("the
  theme's id is 'x', but its path holds 'y' (a theme's path is made from its id)"; step 5 left it to the editor).
  Kept per entry while its value and path are the same (a commit of a theme: 3.4 ms the first time, 0.3 ms after,
  Debug);
- *what the checks across entries add* (`EntryChecks.Run`), a pause after the last change (`EntryChecks.Pause`,
  200 ms, P17): every record entry that builds, the game's too, checked with `RecordChecks.Check(value,
  Session.EditedData, new SessionReferences(session))` (its kind's checks against the data as the game would see it
  after saving, unsaved work included, and its references resolved), and what that adds to its own problems (the same
  path and words are the same problem) kept for it; then, while no record entry under `audio/` has a problem, the
  game's own music loading, `MusicLibrary.Load(Session.EditedData)`, whose first problem goes at the root of the record
  entry it names, in its words (what only it refuses: a music folder holding another kind, a name the music's rule
  can't store, music without a theme order), or, when no record entry can show it (a file there), is
  `Checks.DataProblem`, as is `Session.EditedDataProblem` when the entries can't be mounted together. Each entry whose
  problems change is reported changed (`Session.Recheck`: the list's mark and the form follow); `Checks.Ran` after.
- *shown:* its own problems, then what the last run added (the manager's Q-B), **only the first at each path** (the
  manager's decision of 2026-11-06: `EntryChecks.FirstPerPath`; RecordChecks lists a kind's own checks first, so the
  first is the kind's own, in the words the game logs: "Unknown mode blues", not the choice's "\"blues\" isn't one of
  the choices: …"; the sequence's "the sequence intro, track 0 (audio): cue 0 plays the effect boom, which isn't in the
  game's data (…)", not the reference's "no sound effect \"boom\" (…)"). A draft that doesn't build shows its build
  problems only (step 9).
- *A check that throws* (a port kind's check with a bug, or one reading an entry the data can't give) is the entry's
  problem at its root ("its check failed: <message>") rather than the end of the checks. An entry whose draft doesn't
  build has no value and isn't checked; once it builds, its own problems show at once and nothing of an earlier run.
- *Where they show* is step 9's and step 8's: under the field's row (P16; a path without a row of its own under its
  nearest row), the whole entry's at the form's top, the list's `!` mark with the count. Nothing of the form or the list
  changed.
- *When:* `Checks.Pending` (something changed since the last run: a commit, undo, redo, a recheck, an entry added,
  removed, renamed, reverted, a pack opened), `Checks.Due` (the view's `ChecksPart` starts the pause again on each, a
  timer of the document's loop), `Checks.RunPending()` (runs only when due; the timer calls it, saving calls it first).
- *The cost* (Debug, this PC; `claude-work/m11/step10/measure.txt`): a run over a mod opened over the repo's packs
  (126 entries, the game's 55 themes checked against their instruments and the music loaded once) took 53 ms the first
  time and 23–28 ms after; with the dev setup's `audio.pak` itself open, 39 ms, then 23–32 ms.

**What saving reads (step 11):** `Session.Checks.RunPending()` first, then `Session.EntriesWithProblems` (the entries
shown with a problem, in path order; each one's first problem is `Problems[0]`; removed entries aren't among them) and
`Session.Checks.DataProblem` (a problem no entry can show; saving refuses while either has something). The preview
(step 13) reads an entry's `Builds` and `Problems` to tell "valid" (M11-12, P35).

**References** (`EntryReferences`; M11-11). `SessionReferences` is the lookup the checks resolve names against: the kind
(`RecordShape.Name`) of the entry the session shows at a path, the open pack's over the game's (step 1's note: the file
system can't tell a record entry's kind without loading it); null for a file, an unreadable entry or nothing there.
`EntryReferences.In(draft)` finds every name a draft holds that a mark points at (a string, through nullables, lists'
elements, maps' values, a map's keys for a `Keys` mark, in nested records and unions' subtypes; each mark's condition
read from the draft, so a draft that doesn't build is read too; bulk values aren't walked), each a `ReferenceAt`: its
path, the mark, the name, the entry path it names (`Target`, null when the rule can't store it) and for a key the map's
path. `At(draft, path, key)` is the one at a form row's path (a required name not given yet too, its name empty).
`To(session, entry)` gives the entries naming an entry (P19: the open pack's own and new entries and the mod's copies of
game entries, `IsThePacks`; a mark of another kind pointing at the same path isn't one). `PointAt(draft, references,
path)` points names at another entry (a string written again, a key replaced in its place, the map's order kept; a name
whose mark's rule can't give the path is left and counted). `Candidates(session, mark)` are the entries a mark can name:
the session's records of its kind its rule names, by name (ordinal).

**The reference field's list** (`ReferenceListPart`, a part without a pane; M11-11). While a form field naming another
entry has the focus (a text input in the form's rows whose row's `data-path` a reference mark holds for, by the draft's
condition; a map's key in the row's name too; not another control of the row, such as a nullable name's "set" box), a
list opens in the row under its line (before the row's problem lines), at the
field's left and as wide (`margin-left` and `width` set from the field's box; the rows below make room): the
`Candidates` by name, the game's own dimmed (`--muted`, a mod's copy not), each with its path as tooltip; every name when
it opens, then, once something is typed, those holding the text (ignoring case); its last line in the problem red flags
a text that names no entry ("\"c\" names no room") or a kind with none ("No creature in the data"). Down and Up move
an active option (the line colour behind it; clamped at the ends; scrolled as little as shows it), Enter
picks it (the name put in the field and the field's own change event sent, so the form commits it: one step of the
history, at once, so the field's own Enter and change after it find the value given), Enter with none moved to commits
the text typed and closes it, a click picks (a press in the list keeps the focus in the field), Escape closes it,
typing or Down opens it again (Down and Up, and Down opening it, don't move the field's caret; with a modifier they are
the field's), leaving the field (for anywhere) or selecting another entry closes it; after a change of the entries it
follows its field in a task after the change's listeners (parts without a pane are attached before the form, so they
hear the session first): filled again, or closed when the form made its row again. `role=listbox`, the field's
`aria-expanded` and `aria-autocomplete`.

**Referrers** (`ReferrersPart`, through step 9's `EntryActions.Asking`; P19, Q-A). A rename's and a removal's dialog
gets, when entries of the pack name the one renamed or removed (a removed entry isn't its own referrer), "N entries
refer to it:" and their paths (the first `ReferrersPart.Listed`, 8, then "and N more"); a rename "Update them to the new
name" (ticked), a removal "Point them at:" (the select above, "Leave them" chosen). Confirmed, after the session's
rename or removal, each referrer's names are pointed at the new path in one commit of its own history (undone per entry,
M11-7), and the status line says "Pointed 3 entries that referred to parts/arm at parts/limb" (with "; 1 name can't name
it and stay as they were" when a mark's rule can't give the new path, e.g. renamed out of its folder). A duplicate's
dialog gets nothing.

**Screens against Chromium (P36):** `checks-problems` (1280 × 665: a mod's copy of the game's `alder-vale` with its id,
mode and room wrong after a run: the id's and the mode's own problems, one of the mode's two, the room's found across
entries, the names red, the list's `↑ !`) and `references-list` (1280 × 665: the room's field focused without a press,
"c" typed, Down pressed: three rooms, the first active, the game's dimmed, and the flag). Pixels past 12 levels (the
limit 0.2 %), the GPU: `checks-problems` 0.050 % at 1× and 0.187 % at 1.65, `references-list` 0.046 % and 0.152 % (rebased on step 13; before it 0.047 % and 0.185 %, 0.042 % and 0.150 %); the
rest the known select width (step 9's "Found"), the status line below (and at 1.65 the caret Chromium draws in the
focused field). Captured with step 8's capture script's way (`claude-work/m11/step10-capture.sh`: a headless cage, one
fixture per run): **a fixture wider than the cage's 1280 × 720 output needs the large output mode at 1.65 too**
(`2400x2400`; without it the window was made the output's size and the capture cut, 1281 × 722); the 1.65 runs didn't
stall with it here.

**Found** (reported to the manager; the engine not changed; open, no go yet, in `Docs/Engine-Plan.md`, "Small tasks left from earlier milestones", and M5's known differences; the manager's word of 2026-11-07: the list stays in the flow, the overlay coming back only if the engine composites such boxes as Chromium does):
- *At 1.65 the status line's text is grayscale in the engine where Chromium's is LCD while the form pane is a tall
  scroller* (1,823 pixels of `checks-problems@1.65` while its pack was named `broken-vale.pak`, 1,440 of
  `references-list@1.65`; step 9's `form-controls@1.65` has 815 of it already; none at 1×, none while the form pane
  doesn't scroll). The status line has no background of its own. Why the engine draws it grayscale there isn't looked
  into (the painter's).
  Not the status line's top on a fractional device pixel: the fixtures' height, 665, puts it on a whole one at 1.65 and
  the difference stayed. The pack's short name (fewer of the status line's pixels) keeps both screens within the limit.
- *A positioned box with a z-index inside the form pane's scroller paints from the root's stacking context; Chromium
  composites it apart (it must scroll with the scroller but paint after it), its text grayscale; the engine paints it in
  the scroller with LCD text.* The list was first an overlay (`position: absolute; z-index: 2`): `references-list@1`
  0.21 % and `@1.65` 0.26 %, the list's text. A stacking context on the form's rows to keep it inside (`isolation:
  isolate`, or `position: relative; z-index: 0`) turned the whole form's text grayscale in the engine at 1.65
  (`form-controls@1.65` 1.12 %; Chromium's look with it not captured). So the list is in the rows' flow (the step's
  choice, recorded above), no positioning, which both draw alike.

**With step 13 (rebased on its merge, `034b83c`, 2026-11-08).** `Session.EditedData` is step 13's final one (its paths
taken again on every read, imported files' bytes served); this step's own copy of it was dropped. The preview takes an
entry only when it builds and has no problem (P35, step 13's `PreviewView.Take`), so with the checks running an invalid
entry's note is now its first problem, in the kind's own words ("This version has problems (Unknown mode blues): the
preview keeps the last valid version."), where step 13's tests, written before the checks existed, met the loaders'
words further on ("The music doesn't load (…)", "The sound effect isn't playable (…)", "The particle effect isn't valid
(…)", "The model isn't valid (…)", the sequence's load error): six of their expectations take the problem's words now
(`Tests/.../Editor/Preview/SoundPreviewTests.cs`, `ScenePreviewTests.cs`, `CutscenePreviewTests.cs`), nothing of the
preview's code changed. The two screens were captured again, the preview pane showing the theme's sound controls and that
note: `checks-problems` 0.050 % at 1× and 0.187 % at 1.65, `references-list` 0.046 % and 0.152 % (before, 0.047 % and
0.185 %, 0.042 % and 0.150 %); the other fixtures, step 12's and 13's among them, as they were.

**Mutations** (`~/.cache/stat-fick-build/claude-work/m11/step10-mutations.json`, made by `make-step10-mutations.py`
beside it, run in four chunks; then `step10-mutations-rerun.json`, made by `make-step10-rerun.py`): 176 in the first
run, every rule of the checks, the pause, the references, the list, the referrers and the list's look broken once: 146
killed, 4 didn't build (rewritten), 26 survived. Of those, 11 were gaps closed by tests (an entry made to build after a
run, the music's loading held back by a music entry's problem found across entries and not by another entry's, leaving
the field for outside the form, a nullable name's "set" box, the list before the row's problem lines, a mod's copy not
dimmed, one referrer's words, the number listed, an entry of another kind a mark could name not offered, "Leave them"
pointing nothing anywhere); 7 were redundant code, removed (a second test of the data's problem, the run's empty
findings dropped, the list's tests of a disabled field, of the form's rows and of the key's target, which focus and the
row's path already make, and Enter's default and propagation, which the pick's commit at once makes harmless); 2 are
equivalent (each entry's own check kept while its value and path are the same, a cache; the candidates sorted by name,
which every name rule there is keeps in path order); 6 are looks (the list's corners, the game's options' grey and the
flag's red under the screens' limits; the referrers' three rules in the dialogs, which no fixture shows). The rerun
(20: the 11 gaps, the 4 rewritten, 5 on the code changed since) killed 19; the one left, checking an entry that doesn't
build, is equivalent (it has no value: its check fails alike alone and across entries, so nothing of it shows). Nothing
written to the player's real folders.

**Choices the design left to the step** (made here): the names above; the list in the row's flow under the field
rather than an overlay (the "Found" above), every name when it opens and filtered once typed, a name matching anywhere
ignoring case, the flag's words, at most 8 rows seen (180 px) before it scrolls, the active option's look; Enter
without an active option closing it; a removal's options named by the first referrer's mark; the referrers listed up to
8; the status line's words; a check that throws shown as the entry's problem; the theme's id rule as the entry's own
check, and the music's loading as the last check (confirmed by the manager).

### Step 11: saving and mods (2026-11-08)

**Files.** `Tools/Editor/Session/EditorSession.Saving.cs` (the session's saving, no DOM: `SaveProblem`, `SaveAsChoice`, and
the partial `EditorEntry.MarkSaved`), `Tools/Editor/Saving/` (`SavingPart.cs`: Save, Save As, New mod, the refusal and the
changed-file question, `Opener.SaveFirst`; `SaveAsDialog.cs`; `NewModDialog.cs`; `ModInfoForm.cs`: the mod's info in a form
of its own; `Saving.css`), and the one hook in step 9's `Form/FormPart.cs` (`IEntryForm`, below). Tests in
`Tests/Statventure.Tests/Editor/Saving/`: `SavingTesting.cs` (a scratch player's folder and mods, a scratch copy of the
repo's packs for the dev setup, packs read back as stored), `SessionSavingTests.cs`, `SessionSaveAsTests.cs`,
`SessionNewModTests.cs`, `SavingViewTests.cs`, `NewModViewTests.cs`, `ModInfoFormTests.cs` (with `EntryFormHookTests`, the
hook with a form of the tests' own), `SavingScreens.cs` (the fixtures `saving-save-as`, `saving-new-mod`,
`saving-mod-info`); goldens `Golden/editor-saving-save-as@1.png` and `@1.65.png`, `editor-saving-new-mod@…`,
`editor-saving-mod-info@…`. Every other editor golden was captured again (below).

**The manager's decisions (2026-11-08), at this step's questions** (the step stopped and reported them before writing
code; each the recommended option, (a)):
- *Q1, where the mod's info gets "a form of its own" (M11-15, G1):* in the form pane: choosing `mod/info` (the engine's
  `mod.info`) shows a form of its own instead of the generic rows, through a small documented hook in step 9's `FormPart`
  (a part can supply an entry's form; the Undo key's "field has typing of its own" asks that form), with a test; no
  existing screen changes because of the hook. New mod selects it after making the mod. (Not a "Mod info" dialog from a
  top-bar button, which would change every screen's top bar; not a dialog opened when the entry is chosen in the list.)
- *Q2, when New mod writes the file:* at once: the pack holding only `mod/info` (the name typed; author, version and
  description empty) is written into the player's mods folder and opened. (Not made in memory and written at the first
  save.)
- *Q3, Save As's order and what is open afterwards (M11-16, P24):* the ticks first, then the file browser; written, the
  new file opens in place of the old one; entries left unticked with unsaved changes are named as let go. (Not the
  browser first; not the old pack kept open.)
- *Q4, a pack it can't fully read:* the entries it reads offered ticked; every entry whose content is intact but which it
  can't read as a record (a kind not known here, a newer version, a record that doesn't read) offered unticked with its
  reason, kept byte for byte when ticked; a damaged content (its hash) listed last, not offered. (Not those intact entries
  ticked too; not only unknown kinds and newer versions offered.)
- *Q5, a subset (M11-12, P18):* Save As checks the entries ticked as the game would see them: the game's packs the open
  pack is seen over with only the ticked entries mounted on top, step 10's checks across entries on that data (every
  record it shows, the ticked ones and the game's: the kind's checks against the data, references resolved there, the
  theme's path rule), then the music's loading; a problem refuses it, naming the entries. (Not only the session's own
  problems on the ticked entries.)
- *Q6, histories across a save (M11-7):* kept: the entries stay, each saved one's saved state becomes its current draft;
  Undo goes back past the save and marks the entry changed again. (Not cleared by opening the pack again.)
- *The step's smaller choices, confirmed as listed* ("Choices the design left to the step" below).

**The session's saving** (`EditorSession.Saving.cs`; no DOM):
- `Session.CanSave` (a pack is open and takes changes: a mod, or in the dev setup one of the game's packs, M11-5;
  not a pack it can't fully read, M11-16), `Session.CanSaveAs` (a pack is open), `Session.SaveEntries` (what a save
  writes, by path: the open pack's own entries at their paths now, the new ones, made, duplicated or imported, and the
  game's entries changed in this session, the mod's own copies, P7; removed entries and the game's unchanged ones aren't
  among them).
- `Session.SaveProblems()` (M11-12, P18): step 10's `Checks.RunPending()` first, then each of `Session.EntriesWithProblems`
  with its first problem (`SaveProblem(Entry, Problem)`, the problem as the form shows it: "allows no voices (at
  $.maxVoices)"), then `Checks.DataProblem` (`Entry` null); empty when it can be saved.
- `Session.ChangedOnDisk()` (P23): the file opened again and its index hash compared with the one read when it was opened
  or last saved (`OpenPack.IndexHashHex`): null when the same; else "was changed by another program since it was opened
  here", "isn't there any more", "can't be read now: <the pack's problem>". A file written again with the same index (the
  same entries stored the same way) is no change.
- `Session.Save()` (M11-17, P23): refused (an `InvalidOperationException`, nothing written) unless `CanSave` and with
  `SaveProblems()` empty; every entry of `SaveEntries` written as step 2 says: an imported file with its bytes
  (`PackWriter.Add`, `EditorEntry.ImportedFile`), an edited, new or copied record from its draft in the current versions
  (`AddRecord`), anything else as the open pack stores it at its path now (`AddFrom(pack, entry, path)`: its stored bytes,
  compression and record flag, an older record version, a renamed one at its new path); removed ones (and a copy reverted
  to the game's, step 9's revert, which puts it in `Removed`) left out; then `Write` (a temporary file swapped in). The
  view asks about a changed file first. After the write the pack is read again (`Session.PackFile` the new file, so later
  reads see it; `Pack` with its new index hash, the one compared next time), `Removed` emptied, and each entry whose marks
  change is saved in place (`EditorEntry.MarkSaved`: it is the pack's at its path, `SavedPath` its path, an edited, new or
  copied record's built value its `Saved` and its current draft its `SavedDraft`, an imported file read from the pack from
  then on) and reported (`EntryChanged`, then `Changed`): the list's marks and the title's "•" go. A game entry the mod
  now holds its own copy of gets the game's entry under it again (`Overridden`), so "Revert to the game's" (P7) works on
  it as on any copy. Histories stay (Q6).
- **Save As** (M11-16, P24, Q3–Q5): `Session.SaveAsChoices()` (Q4: `SaveAsChoice(Entry, Offered, Ticked, Reason)` for each of
  `SaveEntries`; `KeptUnread` for an offered one it can't read); `Session.SaveAsFileProblem(file)` (one of the game's packs
  outside the dev setup, M11-5: "x.pak is one of the game's packs, which are read-only (only the repo's dev setup saves into
  them)"; the open pack when it can't be saved over: "x.pak is the pack open here, which can't be saved over: <why>"; null
  otherwise, the open pack too when it takes changes); `Session.SaveAsProblems(ticked, file)` (Q5: the data
  `Session.SaveAsData(ticked, file)`, the base with a custom mount of the ticked entries named by the new file, each as
  Save As writes it; every record that data shows checked as step 10's checks across entries check the session, the
  ticked ones by their built value and the game's by theirs (those under a ticked entry left out, those under an entry of
  the pack left unticked put back), `RecordChecks.Check` with that data and references resolved against it (the ticked
  entries over the game's), then `EntryChecks.PathRules`, the first problem per path, a check that throws as "its check
  failed: …"; then, while no music entry has a problem, `MusicLibrary.Load` on it, its problem at the record it names or
  as the data's; a ticked record whose draft doesn't build gives its first build problem; entries that can't be mounted
  together are the data's problem; in path order, the data's last); `Session.SaveAs(file, ticked)` (only entries of
  `SaveEntries`; refused for the file and for problems as above; the ticked ones written as a save writes them, then
  `Open(file)`: the new file opens in place of the open pack, read again, histories begun anew).
- **New mod** (M11-5, M11-15, P25, Q2): `EditorSession.NewModFileName(name)` (the name trimmed, `ImportNames.Fit`, `.pak`
  added: "My Mod" → `my-mod.pak`; "mod.pak" when nothing of the name fits, import's "imported" only for a mod named so;
  "" for no name); `Session.NewModProblem(name, fileName)` (in words for the dialog's line: "Give the mod a name.", "Give
  the mod's file a name.", "A mod's file name ends in .pak.", "That can't be the file's name: it holds a '/'." or the path
  rule's words, "There is already x.pak in the mods folder." for a file or folder of that name in any case: never
  replaced); `Session.NewMod(name, fileName)` (the pack holding `ModInfo { Name = name }` at `ModInfo.Path` written into
  `Session.ModsFolder`, made when missing, then opened).

**The form pane's hook** (`Form/FormPart.cs`, the one change to step 9's files, the manager's word Q1): a part implementing
`IEntryForm` (`Shows(entry)`, `Show(entry, element)`, `Update()`, `HasTypingToUndo(element)`, `HasTypingToRedo(element)`)
is asked, in the parts' order, whether it shows the entry selected; the first that does gets the form pane's content
(a `div.form-own`, nothing of the generic head, tabs or rows; `FormPart.OwnForm`), is told when that entry changes, and is
asked first by the Undo and Redo keys (not the top bar's buttons) whether the field the key went to has typing of its own
(P13). A rename that takes the entry into or out of such a form shows it again.

**The mod's info in a form of its own** (`Saving/ModInfoForm.cs`, an `IEntryForm`; M11-15, G1, Q1): the record
`mod/info` of the kind `mod.info`: a head ("Mod info", its path, "read-only" when it can't change), what it is for ("What
players will see of this mod in a mod list (the game doesn't show it yet). It is saved with the mod."), then Name,
Author and Version as text fields and Description as a textarea, labelled as the generic form's rows. A field commits on
its change event (Enter in a text field, leaving it) as the generic form's do: one step of the entry's history unless the
text is the draft's; the fields follow the draft only where its value changed (typing elsewhere survives); its own typing
undone and redone first, as `FormView` does (P13). A `mod.info` record elsewhere, or another kind at `mod/info`, shows in
the generic form. A mod without the entry (made by hand) can get one through New (Mod info at `mod/info`), as before.

**From the view** (`Saving/SavingPart.cs`, a part without a pane; each action a task of the document's loop):
- *Save* (Ctrl+S, the top bar's button; enabled while `CanSave`): a text field being typed in commits first (blurred and
  focused again, as the button's click does by taking the focus); nothing unsaved: "Nothing to save: x.pak is as saved."
  and nothing written; problems: "Can't save" — "x.pak can't be saved until these are fixed:" and a list, each entry (bold)
  with its first problem, the first `SavingPart.Listed` (8) then "and N more", the data's problem as a line of its own;
  OK. A changed file: "The file changed" — "x.pak <why>. Overwrite it with the editor's version, save as another file, or
  cancel?" (Overwrite, Save As, Cancel; Save As goes on to Save As). A write that fails: "Can't save" — "x.pak couldn't be
  written: <why>." (the pack's problem for a damaged entry, else the system's message). Saved: "Saved x.pak."
- *The unsaved-changes question's Save* (step 8's `Opener.SaveFirst` is `Save`): opening another pack, New mod and closing
  the editor offer Save, Discard, Cancel; a save refused or cancelled keeps the pack open and the editor running.
- *Save As* (Ctrl+Shift+S, the button; enabled while a pack is open): the field committed first; the **Save As dialog**
  (`SaveAsDialog`, 560 px): "x.pak is read-only (<why>). Tick the entries to keep in a new pack; it opens in place of x.pak
  once saved." (or "Tick the entries of x.pak to save in a new pack; …"), All and None, the list (the offered entries by
  path, then those not offered; a row is a label: the tick, the path, the kind's name, and under it "Kept as it is, unread:
  <reason>" or "Not offered: <reason>"), a note (in ember) naming what is let go ("Left out, their unsaved changes are let go:
  a, b." — entries changed, new or renamed left unticked, the first 8 then "and N more"), "The new pack keeps entries the
  editor can't read, so it opens read-only." when one is ticked, "Tick the entries to save." with "Choose file…" disabled
  when none is; "Choose file…" (focused) and Cancel. Then the file browser ("Save As", button "Save", `.pak`, a new name
  allowed, the open pack's name suggested, from the most recent place else the mods folder); a name without `.pak` gets it
  (asking "Replace the file?" when that one is there); a file `SaveAsFileProblem` refuses is said ("Can't save there" —
  "<why>. Choose another file.") and the browser shows again; over the open pack changed on disk: "The file changed" —
  "… Overwrite it with the entries ticked, choose another file, or cancel?" (Overwrite, Another file, Cancel); the subset's
  problems refuse it ("The entries ticked can't be saved as y.pak until these are fixed:", the list as Save's); a write that
  fails is said; then the new file is open: "Saved 3 entries as y.pak." (", open read-only: <why>." when it keeps entries
  it can't read).
- *New mod* (the button; always enabled): the **New mod dialog** (`NewModDialog`): "The mod is made in your mods folder: a
  pack whose entries add to the game's data or replace its entries.", Name (focused) and File (the suggestion following the
  name until the file name is edited by hand), a line for what is wrong (typing clears it), "Mods load in file-name order:
  where two mods hold the same entry, the one whose file name comes later wins." (P25), Create (or Enter in a field, its
  keypress kept from the New mod button the focus returns to) and Cancel. Then the open pack's unsaved work asked about
  ("…Discard them and make the mod x.pak?", Save, Discard, Cancel), the names checked again (a file of that name come
  meanwhile is said, "Can't make the mod"), the mod written and opened, `mod/info` selected (its own form), "Made x.pak in the
  mods folder." A write that fails: "Can't make the mod" — "x.pak couldn't be made: <why>."
- Its stylesheet (`Saving.css`): the Save As list as step 12's keep-or-take list (rows with lines between, the reasons in the
  problem red indented under the path, not-offered rows soft), the note in ember, the New mod fields as the import dialog's,
  the mod info's rows as the form's (names 140 px, muted), the textarea 360 px wide and 106 px high, not resizable,
  `line-height: normal` (below, "Found").

**Screens against Chromium (P36):** `saving-save-as` (1000 × 800: a mod it can't fully read, the game's audio closed so the
entry pane doesn't scroll: its room and text ticked, an unknown kind ticked with the read-only note, a newer version
unticked, a damaged entry not offered; "Choose file…" focused by code), `saving-new-mod` (1000 × 640: the empty editor,
"Brighter Halls" and its file name, a mod of that file name already in the fixture's mods folder so the problem line shows;
Create focused by code) and `saving-mod-info` (1000 × 640: a mod with its info filled in, two lines of description, selected).
**Every other editor screen was captured again:** New mod is now always enabled, and Save and Save As are enabled with a
pack open, so every top bar changed (before the captures `checks-problems`, `references-list` and `preview-model` at 1.65
were past the limit, 0.26 %, 0.22 % and 0.22 %, the rest within it). Captured with `claude-work/m11/step11-capture.sh`
(step 13's: make-editor-references.sh's harness, flags and fonts in a headless cage, the output 2400 × 2400 at both scales,
one fixture per Chromium run). Pixels past 12 levels (the limit 0.2 %), the GPU: `saving-save-as` 80 of 800,000 at 1×
(0.010 %, worst 62) and 458 of 2,178,000 at 1.65 (0.021 %, worst 99), `saving-new-mod` 57 (0.009 %) and 1,067 (0.061 %,
worst 197: its buttons' labels a device pixel low at 1.65, the known open item), `saving-mod-info` 0 and 28 (0.002 %); the
16 captured again give the numbers they gave before, pixel for pixel the same counts (`shell` 0 and 28, `dialog` 57 and
110, `browser` 28 and 147, `list-marks` 0 and 28, `list-unreadable` 0 and 28, `form-controls` 0.108 % and 0.139 %,
`form-controls-end` 0.115 % and 0.155 %, `form-json` 0 and 28, `import-path` 0.009 % and 0.010 %, `import-replace`
0.009 % and 0.006 %, `import-again` 0.055 % and 0.094 %, `checks-problems` 0.050 % and 0.187 %, `references-list`
0.046 % and 0.152 %, `preview-effect` 0.070 % and 0.089 %, `preview-model` 0.004 % and 0.126 %, `preview-cutscene`
0.010 % and 0.017 %). On lavapipe within a few pixels of these (`saving-save-as` 80 and 466, `saving-new-mod` 57 and
1,069, `saving-mod-info` 0 and 28).

**Found** (reported to the manager; the engine not changed):
- *The engine's textarea lays its lines out at the normal line height whatever the author's `line-height`.* With
  `line-height: 20px` (and `font: inherit` from a body of 24 px lines), Chromium's textarea put the description's lines 20
  px apart, the engine's 16 px (Lato 13 px's normal height), its first line 2 px higher: `saving-mod-info` 0.53 % at 1× and
  0.43 % at 1.65. The editor's textarea is `line-height: normal`, which both lay out alike (0 and 0.002 %). The input fixtures
  (`Ui/InputFixtures.cs`) have textareas only at the normal height. An open item for the engine's UI (`Docs/Engine-Plan.md`,
  "Small tasks left from earlier milestones").
- *Known, met again:* a dialog over a scroller composited at 1.65 has grayscale text (step 12's "Found"): the Save As
  dialog over the entry pane holding the long notice of a pack it can't read was 2.08 % at 1.65; the fixture closes the
  game's audio so the pane doesn't scroll, as step 8's `list-unreadable` does.
- *A key whose listener closes a dialog sends its keypress to what has the focus after:* Enter in the New mod dialog
  created the mod and then pressed the New mod button the focus went back to, opening the dialog again (Chromium does the
  same: keypress goes to the focused element); the field's listener cancels the key's default action.

**Mutations** (`~/.cache/stat-fick-build/claude-work/m11/step11-mutations.json`, made by `step11/make-mutations.py`, run in
four chunks under the shared lock; every rule of the session's saving, Save As and New mod, the saving part, the two
dialogs, the mod info's form, the form pane's hook and the stylesheet broken once): 232 in the first run, 205 killed, 27
survived, none failed to build. Of the survivors, 14 were gaps closed by new tests (Save As by its key and button, Save
giving true when nothing is unsaved, the ticks dialog cancelled ending Save As (two), a damaged entry not ticked by All or
by code, no read-only note while nothing kept unread is ticked, `ChangedOnDisk` with no pack, a game entry under a ticked
one not checked, the theme's path rule in Save As's check, the mod info's own undos counted down by a redo, started again by
typing and by the draft's value changing, the first of two forms of its own in the parts' order); 6 were redundant code,
removed (Save As committing the field, which its dialog's focus does; `Ticked` excluding what can't be ticked; the saved
entries' problems refreshed, the same values at the same paths; `MarkSaved` sparing an unchanged record, whose built value
is the one it was read as; Save As's first problem per path, of which only the first was taken; the mod info's commit
skipping the draft's own text, which its change event never carries); 7 are accounted for: 1 equivalent (a mod info field
"not given" shown as empty text: no `mod.info` draft holds one, its four fields having defaults), 1 the engine's known
difference (the textarea at `line-height: 20px` renders as at normal in the engine, the "Found" above) and 5 looks under
the screens' limits or in no fixture (the kind's names and the not-offered rows in ink, the problem line's height while it
holds text, the mod info's title not bold, the refusal's list not indented: no fixture shows the refusal). The rerun
(`step11-mutations-rerun.json`, made by `step11/make-rerun.py`: the 14 and 6 on the code changed since) killed all 20; the
list as it stands (`step11-mutations-standing.json`, 228) has 221 killed and 7 accounted for. Nothing written to the
player's real folders (`mutate.py`'s watch; the game folder missing and the Documents folder's top level the same before
and after, `claude-work/m11/step11/real-folders-*.txt`), nor to the repo's packs. Lesson: a mutation run's screen tests
rewrite `Reference/editor-fixtures.json` from the mutated stylesheet or markup and leave it so: put it back (`git checkout`)
after a run.

**Choices the design left to the step** (made here, confirmed by the manager on 2026-11-08): the names above; Save enabled
while the pack takes changes and writing nothing when nothing is unsaved ("Nothing to save"); Save As enabled while a pack
is open, New mod always; Ctrl+S and Ctrl+Shift+S committing the field being typed in first; the refusal's dialog (the
first 8 entries, "and N more", the data's problem last, OK only); the changed-file question's words and buttons (Overwrite,
Save As, Cancel; in Save As over the open pack, Another file); `.pak` added to a Save As name without it (asking before
replacing that file); the game's packs outside the dev setup and the open pack that can't be saved over refused as Save
As's file, the browser shown again; the open pack's name suggested; the Save As dialog's look, words, All and None, its
note; the New mod dialog's fields and words, the file name following the name until edited, the path rule on the file name
and `.pak`, a name already in the mods folder in any case refused (never replaced); "mod.pak" for a name of which nothing
fits; the mod's info form's words and look; the status line's words.

### Step 12: import (2026-11-07)

**Files.** `Tools/Editor/Session/EditorSession.Import.cs` (the session's import, no DOM: `ImportSource`, `ImportWay`,
`ImportPlan`, `ImportTarget`, `ImportAction`, `ImportException`, and the partial `EditorEntry` member for imported files),
`Tools/Editor/Import/` (`ImportPart.cs`: the Import command, the queue of dropped files, the import dialog, the replace
question and the keep-or-take list; `ImportNames.cs`: the path suggested; `ImportMerge.cs`: `ImportComparison` and
`ImportDifference`, how an entry and a file imported again over it are compared, P29; `Import.css`). Tests in
`Tests/Statventure.Tests/Editor/Import/`: `ImportTesting.cs` (a scratch folder of files to import, the player's folder, a mod
over the repo's packs, the Import button and the browser, drops, the packer's entry of the same file to compare with),
`ImportKindsTests.cs`, `ImportPathTests.cs`, `ImportReplaceTests.cs`, `ImportAgainTests.cs`, `ImportComparisonTests.cs`,
`ImportVariants.cs` (record kinds in the tests' variants namespace, which the layout guard leaves out: bulk data under a
nullable, in a map, in a union's subtype and two records deep; a nullable member of two words), `ImportScreens.cs` (the fixtures `import-path`, `import-replace`, `import-again`); goldens `Golden/editor-import-path@1.png`
and `@1.65.png`, `editor-import-replace@…`, `editor-import-again@…`.

**Decided by the manager (2026-11-07), at this step's questions** (each the recommended option; the step stopped and
reported them before writing code):
- *Q1, how values are matched when importing again (P29):* by path along the kind's shape. Bulk fields come from the file
  and are never listed; records are compared field by field; a list whose elements hold bulk data (a model's nodes and
  animations) keeps the file's length and is compared element by element by index; any other list, and a map or pairs,
  element by element when the lengths (a map's keys, in order) are the same, else as one value ("Cover slots: 3 elements
  in the entry, 4 in the file"; keep keeps the entry's whole list); a nullable set on one side only, a union of another
  subtype, a value given on one side only: one value; a whole value that holds bulk data is never listed (its structure
  is the file's); a plain value (number, text, flag, member, vector, colour, matrix) when it isn't the same bit for bit.
  Keep by default, with Keep all and Take all. **Known limit** (recorded with the decision): a node inserted in the file
  shifts the names after it by index, so each shifted name is listed as differing (kept by default); Take all takes them.
- *Q2, the folder suggested when the kind has none:* by ending: fonts (`.ttf`, `.otf`) in `ui/fonts` (as `ui.pak` holds
  them), images (`.png`, `.svg`) in `ui/images` (where `StylesheetImporter`'s notes put them, a stylesheet's urls pointing
  there), texts (`.txt`) and record kinds without a folder (a state machine) at the top of the data, the mod's info
  (`mod.info`) at `mod/info` (`ModInfo.Path`, its one place).
- *Q3, what an import does to the session:* importing again over a record of the same kind is one step of that entry's
  history (undoable as an edit; over a game entry in a mod it makes the mod's own copy, P7); into a free path a new entry
  (`EntrySource.New`, not undoable, as New is, M11-7); over an entry of another kind, or a file over a file, a replacement,
  asked first and not undoable: a pack's entry replaced is listed in `Session.Removed` for saving, a new one goes, a game
  entry stays under the new one (`Session.Overridden`), back to the game's draft, and shows again when the new one is
  removed or renamed.
- *Q4, the dialogs:* one file at a time in the order given (the button's pick, or the files dropped in their order, P27):
  the import dialog first, then only when the path is taken the replace question, or for a record of the same kind whose
  values differ the keep-or-take list; Cancel skips only that file. Screens: `import-path`, `import-replace`,
  `import-again`.
- *The step's own choices, confirmed:* below ("Choices the design left to the step"), among them the partial `EditorEntry`
  member holding an imported file's bytes.

**The session's import** (`EditorSession.Import.cs`; no DOM):
- `Session.Importers` (the engine's by default, like `Session.Types`: G3 keeps every option open), `Session.ImportEndings`
  (the importers' endings in ordinal order, then `.json`, then `FileEndings` `.ttf .otf .png .svg .txt`, each once: the
  browser's filter and the refusal's list), `Session.CanImportFiles` (a pack is open and takes changes).
- `ImportSourceOf(file)` → `ImportSource(File, Way, Ending, Stem, Importer)`: by the file's name in any case
  (`Arena.GLB`'s ending is `.glb`, step 2's note): the importer its lower-case name's ending chooses (`ImportWay.Importer`),
  else a `.json` (`ImportWay.Record`: whether it starts as a record's JSON form and of which kind is the import's to find,
  as packing finds it; `LooseRecord` is the engine's internal), else one of `FileEndings` (`ImportWay.File`); anything else,
  a name that is only its ending, or a file that isn't there is an `ImportException` ("the editor imports .css, .glb,
  .terrain.json, .json, .ttf, .otf, .png, .svg and .txt files"). `SourcePath(path)`: the entry path with the ending for an
  importer or a record (`models/stages/arena` + `.glb`), a file's path as it is (with its own ending).
- `SuggestImportPath(source)` → (path, kind): the file imported once at its fitted name at the top of the data (a file at
  its ending's folder) to learn what it makes (`PackWriter.ImportFile`; a problem is the `ImportException` the dialogs
  show before asking a path), the entry there (else the first made) read as the session reads a pack's record
  (`RecordSerializer.RootTypeName`, `Session.Types`); a record suggested by `ImportNames.ForRecord(kind, stem)`, a file
  (or an importer that makes a file, not a record) at the trial path. `ImportNames.Fit(name)`: accents dropped (FormD,
  non-spacing marks), small letters, every character the path rule doesn't take made `-`, runs of `-` made one, `-` and
  dots taken off both ends, at most 120 characters (`MaxLength`), a Windows device name given `-1` before its first dot
  (`con-1`, `aux-1.tar`), `imported` when nothing is left. `ForRecord`: `mod/info` for `ModInfo`, else the kind's rule
  on the name as it is (a theme exported as `combat@alder-vale.json` comes back to its own path, M11-18), else on the
  fitted name, else the fitted name at the top (a kind without a folder). `ForFile`: `FolderOf(ending)` and the fitted
  name with the ending.
- `ImportPathProblem(source, path)`: empty ("Give the entry a path."), the path rule ("That can't be its path: <the rule's
  words>."), the path rule with the ending added (a name over 255 characters), and an ending that would import it as
  something else (a file's path ending as an importer's or `.json`; a record's or importer's path whose ending, added,
  chooses another importer: `a/b.terrain` + `.json`).
- `PlanImport(source, path)` → `ImportPlan` (`Source`, `Path`, `Targets`, `Main`: the target at the path chosen, else the
  first): the path's problem, the import at `SourcePath(path)` with `ImportSources.Exists = DataHas` (P28: the data the
  mod sees, which is the entries shown: the game's other packs' and the open pack's as edited, new and imported ones
  included, so a stylesheet may name an image imported a moment before) and the session's types and importers; a `.json`
  that made a file, not a record, refused ("it isn't a record's JSON form (its first key isn't $types)"); then per entry
  made (`ImportTarget`: `Entry`, `Record`, `Kind`, `Existing`, `Action`, `Comparison`): a path that would be the folder of
  an entry shown or below one refused in `Session.PathProblem`'s words; free: `Add`; an entry of another of the game's
  packs beside a game pack in the dev setup: refused ("…is in another of the game's packs, which is read-only beside this
  one."); a record of the same kind: `ImportAgain` with `ImportComparison.Of(existing draft, file's draft)`; anything else:
  `Replace`. Problems are `ImportException`s in the importer's or the packer's words (`DataFileException.Problem`, "the
  file isn't there", an unreadable file's .NET message, "the import made no entry", "a record of type x, which isn't a
  record type known here"); no pack that takes changes is an `InvalidOperationException`. The session isn't changed.
- `Import(plan, take?)` (Q3): the plan must still fit (each target's path holding the entry it was planned over, else an
  `InvalidOperationException`); `ImportAgain` commits `Comparison.Merge(take)` to the entry (`Session.Commit`: one step);
  `Replace` takes the entry out first (`EntriesChanged` Removed, the selection cleared when it was the one); then a new
  entry: a record `EditorEntry.ForNew(path, RecordDraft.From(record))`, a file `EditorEntry.ForImportedFile(path, bytes)`,
  refreshed with the session's checks, reported Added. Gives the entries at the plan's paths in its order; `Changed`
  told once. Nothing is written: the pack file is as it was until step 11 saves.
- **For step 11 (saving) and steps 10 and 13 (the custom mount):** an imported record is a new entry like New's (its
  draft built gives the binary form the packer gives the same file: the tests compare); an imported file is a new entry
  (`EntrySource.New`, `EntryContent.File`, read-only, `FileSize`) whose bytes are `EditorEntry.ImportedFile`
  (`ReadOnlyMemory<byte>?`; null for every other entry): save it with `PackWriter.Add(path, entry.ImportedFile.Value.Span)`,
  and the custom mount reads it from there (a copy per read, `ToArray()`), not from the pack. A replaced pack entry is in
  `Removed`; a game entry replaced in a mod is under the new one (`Overridden(path)`).

**Comparing when importing again** (`ImportMerge.cs`; P29 as Q1 decided): `ImportComparison.Of(present, file)` (one kind,
else an `ArgumentException`) walks the kind's fields in number order, depth first, giving `Differences`
(`ImportDifference(Path, Name, Present, File, Shape, Field)`, `IsWhole` when either side isn't a plain value): the path as
problems write it (`$.materials[0].baseColor`, `$.scores["b"]`), the name as the form names rows ("Materials › 0 › Base
color": display names, list elements from 0, a map's values by their key's text). `Merge(take?)`: the file's draft with the
entry's value put back at every difference not taken (none taken: the default, keep). `HoldsBulk(shape)` (remembered per
kind, a kind that holds itself guarded): a field marked bulk anywhere below, through records, unions' subtypes, lists, maps
and nullables. `Same(a, b)`: floats and doubles, vectors, quaternions and matrices bit for bit (0 and −0 differ, NaN is
NaN), byte arrays byte for byte, the rest by `Equals`.

**The import part** (`Import/ImportPart.cs`, a part without a pane):
- *Import* (the top bar's button; no key: the design has none) asks the file browser (`BrowserRequest("Import a file",
  "Import", Session.ImportEndings)`, from the most recent place, else Documents) and imports the file picked; enabled while
  `CanImportFiles`. *Drops:* step 8's opener queues the files that aren't packs (P27); the part listens to
  `ImportsQueued` and takes the queue at once (so step 8's "waiting for import" is no longer said in the editor), in order.
  `ImportFiles(files)` imports one file at a time; files given while one is being asked about (a drop meanwhile, another
  call) are queued and taken after it. With no pack open, or a read-only one, one dialog says so for the files given
  ("Nothing imported": "notes.txt and a.ttf can't be imported: no pack is open. Open a mod or make one first."; "…: x.pak
  is read-only (<why>)…").
- *Per file:* a file that can't be imported (its kind, the import at its trial path) is said ("Can't import": "x can't be
  imported: <why>.", OK) and the next comes; else the **import dialog** ("Import": "Arena Stage.GLB imports as a model.
  Its entry's path:" — "a file", "a room (its JSON form)", "an instrument …"; the path field with the suggestion,
  focused; a line for what is wrong; Import, Cancel). Import (or Enter in the field, without Shift, Ctrl, Alt or the Super
  key) plans the import at the path typed: a problem shows on the line (typing clears it) and the dialog stays; else it
  closes. Then, only when a path is taken: the **replace question** for what is replaced or imported again with no value
  differing ("Replace the entry": "notes/readme.txt is already there (a file). Replace it with the imported file? This
  can't be undone." — "(a file of the game's)… The game's own entry shows again if the new one is removed." for a game
  entry; "Import again": "audio/rooms/crypt is already a room. Import crypt.json again over it? Its bulk data comes from
  the file; no value edited in the form differs."; Replace or Import again, Cancel), and for each record imported again
  whose values differ the **keep-or-take list** ("Import again": "models/blocks is already a model: its bulk data comes from
  blocks.glb. 3 values differ between the entry and the file: keep the entry's or take the file's."; Keep all, Take all;
  a row per difference: its name (its path as tooltip), the entry's value and the file's, each a radio button (keep checked
  first) with the value as the form writes it (a number in its shortest form with its unit, "yes"/"no", a member's display
  name, "(empty)", a colour's `#rrggbb`/`#rrggbbaa` with a swatch, a vector's numbers, a matrix's, "3 elements", "1 entry",
  a record's kind, "none", "not given"); Import again (focused), Cancel). Cancel anywhere skips the file. Then the entries
  join the session, the main one selected, and the status line says "Imported x as path." ("…again into path.", "…as path,
  replacing what was there.").
- Its stylesheet (`Import.css`): the path field as the form's fields, the list's dialog 640 px wide, rows 26 px with lines
  between, names 200 px, radios ember, swatches 14 px; the list scrolls past 290 px.

**Screens against Chromium (P36):** `import-path` (1000 × 640: a mod open, the import dialog for `Arena Stage.GLB` with
`models/arena-stage`), `import-replace` (the replace question for a text over the mod's `notes/readme.txt`) and
`import-again` (the keep-or-take list of `blocks.glb` imported again over the mod's model with a node's name, a material's
colour and a flag edited), the focus on each dialog's first button, moved there by code. Captured with
`claude-work/m11/step12-capture.sh` (step 8's: make-editor-references.sh's harness, flags and fonts in a headless cage
with its own Xwayland, one fixture per Chromium run; **at 1.65 too the headless output made 2400 × 2400 first**, else
cage cut the 1650 × 1056 capture to its 1280 × 720 output). Pixels past 12 levels (the limit 0.2 %), GPU: `import-path` 57 of 640,000 at 1× (0.009 %, worst 62) and 166 of 1,742,400 at 1.65 (0.010 %, worst 99), `import-replace` 57 (0.009 %) and 110 (0.006 %), the same as step 4's `dialog` (the dialog box's and its focused button's rounded corners), `import-again` 353 (0.055 %, worst 62) and 1,646 (0.094 %, worst 197): the radio buttons' rims, the swatches' corners, and at 1.65 its buttons' labels a device pixel low (below); on lavapipe within 12 pixels of these.

**Found** (reported to the manager, the engine not changed):
- *A dialog over a composited scroller has grayscale text at 1.65 where Chromium's is LCD.* The keep-or-take list's first
  version had the model's form showing behind the dialog (the form pane a scroller with more rows than it shows): at 1.65
  every glyph of the dialog was grayscale in the engine (colour spread 9–13) and LCD in Chromium (93–130), 23,374 pixels
  past 12 levels (1.34 %); at 1× both LCD, and with the form pane empty both LCD at 1.65. At 1.65 the pane's scroller is
  composited and the dialog over it with it, and the engine's verdict for that layer's text differs from cc's; perhaps
  the same rule as the open item "A composited box's text grayscale at 1.65 where Chromium's is LCD". Recorded as an open
  item of its own (`Docs/Engine-Plan.md`, "Small tasks left from earlier milestones", "An editor dialog over a
  composited scroller"). The fixture shows the dialog over an empty form pane.
- *Within the limits, known:* the keep-or-take list's buttons' labels at 1.65 (the open item "Control labels a device pixel
  low at 1.65", here `<button>`s at that dialog's height) and the radio buttons' rims (a few pixels each); the focus ring
  of a button focused by a script after a click isn't `:focus-visible` (Blink's rule, which the engine has too), so the
  fixtures focus without a click.

**Mutations** (`claude-work/m11/step12-mutations.json`, made by `step12/make-mutations.py`; every rule of the session's
import, the names, the comparison, the part and its stylesheet broken once; run in four chunks, the editor's tests and the
import screens): 221 in the first run, 195 killed, 26 survived, none failed to build. Of the survivors, 17 were gaps,
closed by new tests (an importer's stem, a file named only `.json`, an import refused once the pack no longer takes
changes, the session's checks run on an imported record, a record's JSON form read in the session's kinds, the
comparison's own words for two kinds, a list holding bulk data longer in the file, a structure holding bulk data that
differs left the file's, a difference's shape, bulk data under a nullable, in a union, in a union's subtype and two records
deep (record kinds of the tests' variants, `ImportVariants.cs`), a problem made a sentence, a member's display name and a
nullable one's, no swatch for a text not marked a colour); 3 were redundant code, removed (the import dialog's check that
it closed by Import before giving its plan, which is made only then; its path field's `autofocus`, the dialog's first
control anyway; its class, whose rule gave the problems' red every dialog already has); 2 were killed in fact: the test run
crashed on a stack overflow (the bulk check without its guard on a kind that holds itself, the queue taken while an import
runs), which `mutate.py` reads as a survivor since an aborted run still prints "Passed!" for the tests it ran (checked by
hand, `step12/crash-check.py`); 1 can't change any answer (the bulk check's answers remembered per kind, a cache); 3 are
looks under the screens' limits (the radio buttons' ember, the path label's muted colour, the path field's border colour:
a few hundred pixels each). The reruns (`step12-mutations-rerun.json`, 17, then `-rerun2.json`, the one the first rerun
left) killed all. So the list as it stands (218, the three of removed code taken out) is all killed but those 4 accounted
for, the 2 crashes checked by hand. Nothing written to the player's real folders.

**Changed in earlier steps' tests:** step 8's `OpenerTests.ADroppedPackOpensAndOtherFilesWaitForImport` (with step 12's
part listening, the dropped files are taken at once and asked about, so the status no longer says they wait; the files'
order and the passed-over ones checked as before) and `EditorApplicationTests.TheEditorStartsAsItWasLeftAsksBeforeClosingOverUnsavedWorkAndRemembers`
(the image dropped with the pack is taken by the import, whose dialog the test cancels, instead of waiting; it runs only
with a display, so only the full run in a headless cage showed it); a comment in step 4's `CommandTests` (Import has a handler now, which
can't run with no pack open).

**Choices the design left to the step** (made here, confirmed by the manager on 2026-11-07): the names above; the
importable endings exactly the design's list (the importers', a record's `.json`, `.ttf .otf .png .svg .txt`), anything
else and a `.json` that isn't a record's form refused with why; the file's name tried as it is by the kind's rule first,
then fitted (lower case, accents dropped, other characters `-`, runs made one, ends trimmed, 120 characters, device names
`-1`, `imported` for nothing); the path field a whole path (any folder: `models/stages/arena`); the suggestion found by
importing once at the fitted name (the kind an importer makes is known only by running it), the import at the chosen path
done again; P28's data the entries shown (the base's and the pack's as edited); imports with no pack that takes changes
refused in one dialog; the imported entry selected and the status line's words; the browser for import from Documents
unless a recent place is known; the dialogs' words and look; imported files as new entries of the session holding their
bytes (`EditorEntry.ImportedFile`, a partial member in the session's import file).
### Step 13: the preview (2026-11-07)

**The data as edited (`Session/EditorSession.EditedData.cs`; the contract steps 10 and 13 share, written first so
step 10 builds on it; the manager's decision Q4 of 2026-11-07: step 13 owns it, step 10 uses it and makes none of its
own).** `Session.EditedData` is "the data as the game would see it after saving" (P38): a `VirtualFileSystem` of
`Session.Base`'s mounts (the game's packs other than the open one, in the game's order) with `Mount.Custom(<the open
pack's file>, entries)` on top, so a loader's error names the open pack and the entry as the saved pack's would.
- **What the custom mount gives:** every entry of the open pack (`EntrySource.Pack`) at its path now (a renamed one at
  its new path; a removed one not at all, so a game entry under it shows through, as in the game after saving), every
  new entry that has built once, and every game entry changed in this session (the mod's own copy, P7). A record is
  given as the binary form of its `EditorEntry.LastBuilt` value (current versions), so **an entry whose draft doesn't
  build is given as the last value that did** (M11-12; a new entry that never built isn't there); an unchanged record
  of the pack, and every file and unreadable entry, as the open pack stores it (`Session.PackFile`, at its
  `SavedPath`: an older record version as stored; reading a damaged entry throws the pack's error); a file imported in
  this session (step 12) as a file with its bytes (`EditorEntry.ImportedFile`). Game entries not changed come from
  `Base`'s own mounts below.
- **Kept up to date when read:** each read of the property takes the custom mount's paths and kinds again (so it is
  current also inside a listener of the session's events that runs before any other, or inside a check during a
  commit) and, when they differ from those mounted (an entry added, removed or renamed, a game entry becoming the mod's
  copy or taken back), calls `VirtualFileSystem.Reload()` on it (its `Reloaded` listeners hear that); the bytes are read
  from the entries whenever a path is read (a record's binary form cached per value built, a fresh array each read). A pack opened
  since gives a new file system (the old one disposed with its open files), so **read `Session.EditedData` each time
  the data is used**, don't keep it across changes; disposed with the session. On the editor's thread, as the session.
- **Null** while no pack is open, or when the session's entries can't be mounted together (a record at `x` beside a
  file `x.json`, which no pack holds either): `Session.EditedDataProblem` says why (the custom mount's check, in
  `DataFileException`'s words naming the open pack and the entry); a read after the entries changed tries again (the
  same entries aren't tried twice).

**The manager's decisions on the step's questions (2026-11-07):**
- *Q1, the wiring:* `EditorApplication.Load` gains, after the host is made, `_host.Audio = Audio;` (the document's
  audibility, as the sandbox does) and the preview part's `Connect(Gpu, _host.Views, Audio)`, as step 8 wired
  `Opener.Drop`; and `ApplicationOptions.Audio` is on. Nothing else of step 4's files changes.
- *Q2, Stop for a sound effect:* the engine's `SoundEffects` has no stop (`Register`, `Play`, `PlayAt`; a voice once
  started plays to its end; `ScheduledSound.Cancel` works only before its time). In M11 Stop stops the theme and
  cancels effect plays not yet started; **a sounding effect isn't cut, it ends by itself.** The gap (a stop for sound
  effects, e.g. `SoundEffects.StopAll` with the steal's 5 ms fade, by the audio's owner) is an open small task in
  `Docs/Engine-Plan.md`.
- *Q3, the editor's volume when nothing is remembered:* 50 %.
- *Q4, the data as edited:* above.
- *Q5, element roles' stand-ins:* the host is an element of the preview holding empty elements with the ids the UI
  cues name, with no stylesheet in M11 (a game stylesheet added to the editor's document would restyle the editor:
  the engine's stylesheets apply to the whole document). Later: scoped stylesheets or a second document drawn into the
  pane (engine work).
- *Q6, palette roles' stand-ins:* a preview palette per palette role, as P32's (a distinct colour per slot its cues
  use), editable, remembered with the stand-ins.
- *Confirmed choices:* selecting another entry stops the previous preview (a theme fades out, a cutscene is finished
  and its stages removed), except that a theme keeps playing while an instrument, percussion voice or room is selected
  (P31); Space is Play/Stop for sound only (P31); the particle loop's stream lasts 2 × the effect's longest life; the
  particle and model previews share the turnable camera (drag, wheel, reset).

**Files.** `Tools/Editor/Session/EditorSession.EditedData.cs` (the data as edited, above). `Tools/Editor/Preview/`:
`PreviewPart.cs` (the pane: the selection, the devices, the editor's volume, the stand-ins store, Space, the preview's
clock), `PreviewView.cs` (a preview's base: the kinds, the last valid state and its note, the controls, the 3D view's
surface with the camera's drag and wheel), `SoundPreview.cs` (`EffectPreview`, `ThemePreview`, `MusicPartPreview`),
`PreviewSound.cs` (the music library from the data as edited, the theme playing), `ParticlePreview.cs`,
`ModelPreview.cs`, `CutscenePreview.cs`, `Viewport.cs` (the shared `View3D` and `SceneView`; `OrbitCamera`),
`PreviewColours.cs` (the preview's palettes), `PreviewSettings.cs` (`VolumeFile`, `StandIn`, `StandInsFile`),
`Preview.css`. `EditorApplication.cs`: `Audio = true` and the two lines of Q1. Tests in
`Tests/Statventure.Tests/Editor/Preview/`: `PreviewTesting.cs` (`PreviewEditor`: the editor over a scratch mod with
the preview connected to an offline sound and, for 3D, the tests' headless GPU), `EditedDataTests.cs`,
`SoundPreviewTests.cs`, `ScenePreviewTests.cs` (particles, models, the camera), `CutscenePreviewTests.cs`,
`PreviewSettingsTests.cs`, `PreviewApplicationTests.cs` (the program's wiring, a hidden window), `PreviewScreens.cs`
(the fixtures `preview-effect`, `preview-model`, `preview-cutscene`); goldens `Golden/editor-preview-*@1.png` and
`@1.65.png`. Changed in step 8's tests: `Opening/SettingsTests.cs` expects the sound on.

**The pane** (`PreviewPart`, the preview pane's part). It shows the selected entry's preview, chosen by the entry's
record type (M11-13): a sound effect (`audio.effect`), a theme (`audio.theme`), an instrument, percussion voice or room
(no preview of its own), a particle effect, a model (a stage too), a cutscene; nothing for any other kind, a file or an
unreadable entry (and nothing for the music's parts while no theme plays, so the screens of steps 4, 8 and 9 are
unchanged). Selecting another entry takes the shown preview away (what it played stopped, its scene and hosts removed);
a theme playing fades out then, unless the entry selected is an instrument, percussion voice or room (a theme playing
takes their edits, P31) or that theme itself. **The program connects it** (`Connect(gpu, views, audio)`, Q1): the 3D view
then exists (one `View3D` on the pane's colour `#292c28` and its `SceneView`, a frame client of the editor's document,
shown in the preview's `surface`), and the sound takes the editor's volume. Without a GPU the controls show the same
and the view stays empty; without sound nothing plays (the tests' editors).
- **The last valid state (M11-12, P35):** a preview takes the entry's value when its draft builds, it has no problems
  (`EditorEntry.Problems`: step 10's checks once merged) and the engine takes it (registering a patch, an emitter of the
  effect, `ModelData.Validate`, `Sequence.Load` on the data as edited, a play); otherwise what it took last stays and a
  note under the controls (ember) says why, in the engine's words: "This version doesn't build: the preview keeps the
  last valid version.", "The sound effect isn't playable (…): …", "… : nothing to preview yet." before anything was
  valid. An edit of the entry shown (a commit, undo, redo) or a change of the set of entries is taken at once.
- **The editor's volume** (P31, Q3): `PreviewPart.Volume`, the sound's master volume (`AudioSystem.MasterVolume`, its
  40 ms ramp), 0–100 % on a slider in the sound previews, remembered in `editor/volume.json` (`{ "version": 1, "volume":
  0.5 }`, read tolerantly as step 8's files: a value that isn't a number from 0 to 1 is the default, 50 %); set, it is
  written at once.
- **Space** plays or stops the sound previewed (P31): an effect plays, a theme plays or stops; for the music's parts it
  stops the theme playing. Only when the key reaches the document not cancelled, with no modifier and no dialog open,
  and isn't its target's own (a field that takes text, a form control, a summary, a link, a tab, button or checkbox
  role); the entry list's Space is passed on unless it typed ahead, so Space plays what was just chosen in the list.
  Nothing for particles, models and cutscenes (P31 names sound only; the manager's word).

**Sound effects** (`EffectPreview`, P31). Play, Stop, the editor's volume, and the play's own volume (a factor, 0–2,
1 as made), pan (−1–1) and pitch (in semitones, −24–24, the factor 2^(s/12)), kept while the editor runs, not
remembered. A valid value is registered under the entry's name (its kind's rule: `audio/effects/chime` is `chime`) in
the editor's sound at once (P35), checked first with the patch's own rules (`RecordChecks.Check`, its declared limits
among them), so the next Play is the new patch; Play is `Effects.Play(name, volume, pan, pitch)`, exactly as by hand (the
tests' renders). **Stop doesn't cut a sounding effect** (Q2: the engine's effects have no stop); it stops a theme
playing.

**Themes** (`ThemePreview`, `PreviewSound`, P31). Play makes the music library from the data as edited
(`MusicLibrary.Load(Session.EditedData)`), gives it to the sound (`AudioSystem.Reload`, step 3) and plays the theme
(`Music.Play`); Stop fades it out (`Music.Stop`); a state says "Playing" or "Stopped". While it plays, every valid edit
of the theme, or of an instrument, percussion voice, room or effect while one of those is selected, makes the library
again and swaps it in: the theme restarts through music.js's fades when its music changed, plays on otherwise. One theme
that doesn't pass refuses the whole library (step 5): the last library that loaded plays on, the note naming the entry
and why ("The music doesn't load (… Unknown mode blues …)"). The library is made again only after the data changed.

**Particle effects** (`ParticlePreview`, P32). An emitter of the effect at the origin (a node of the preview's at the
scene's root), looping: a burst of 20 and 20 a second for 2 × the effect's longest life, started again (a new emitter)
in the first frame after the one its clock reached its end (its last birth plus the longest life), seeded by the seed
field (1 at first; Restart starts it again). The preview's palette: a distinct colour per slot the effect names (start,
then end), from `PreviewColours.Distinct` (the ember first, then blue, green, yellow, purple, teal, pink, grey), each
editable as `#rrggbb` beside a swatch, kept per slot name while the editor runs (P32: not in the pack). A valid edit
starts the loop again with the new effect. The camera frames the effect's reach: the farthest its particles go in the
longest life at the greatest speed under gravity and drag (the motion's closed form, each part at its greatest), half
its greatest size and a sphere spawn's radius, at least a quarter metre.

**Models** (`ModelPreview`, P33). The model in its materials' own colours (no palette), instanced on a node of the
preview's (each top-level object its own, its cameras and sun on their nodes, as a stage is), checked first
(`ModelData.Validate`); a stage's sun lights it when it has one. The clips (`AnimationClip.AllOf`) in a select ("(none)":
the rest pose), Play/Pause, Loop (on at first), a time slider and the time; the preview's clock moves the clip on and
applies it to the instance's nodes (`ModelInstance.Apply`: equal to applying it by hand). The turnable camera
(`OrbitCamera`) framed on the model's rest pose (a ball round its meshes' points, a skinned mesh's at its bind pose):
dragged with the main button under the pointer's capture (half a degree per CSS px), the wheel a fifth nearer or
farther per notch (a hundredth to a hundred times the framing), Reset view; the near and far planes from what was
framed. The camera select lists the stage's cameras by their nodes' names ("Free camera" first; disabled when it has
none): looking through one makes it the scene's camera (drag and wheel then do nothing). A valid edit shows the new
model at once (a new GPU model; the old one given back once the scene no longer draws it), the clip, its time, the
camera chosen and the turned camera kept.

**Cutscenes** (`CutscenePreview`, P34). The sequence as the game loads it from the data as edited (`Sequence.Load`:
its stages, particle effects, sounds and theme checked against the same data), played on the shared scene as a
**preview copy** (`sequence with` its roles and tracks; the record untouched), starting paused at its start: Play/Pause,
a time slider (`Seek`: nothing fires; the state there in the next frame), the time "1.25 / 7.00 s". At its end it ends
(its end state); Play then plays it again from its start, a seek plays it again from there, paused. **Stand-ins**
(`editor/stand-ins.json`, `{ "version": 1, "cutscenes": { "<path>": { "<role>": { "model", "clips", "colours" } } } }`,
per cutscene, read tolerantly, written whole at each choice; not in the mod): each actor role a figure (a select of the
data's model entries; `Actor` on a node of the preview's at the origin, its animator on its own stack) and a clip library
(a select of the model entries with clips, bound by bone names as S7-5; none chosen: the figure's own clips); each model
role a model of the data (`ModelInstance`); each palette role the preview's palette (Q6): the slots its cues use (the
start and end slots of the particle effects its particle cues name, then the material slots of what its palette tracks
colour: a chosen model's, a stage's) in distinct colours, each editable and remembered; each element role an empty host
over the view (`div.preview-host`) holding an empty element for each id its UI cues name, no stylesheet (Q5; the cues'
texts show plainly over the view); each node role an empty node at the origin. **Left out of the copy, each listed with
why** ("Left out: track 3 (animation track): no figure chosen for king"): an actor's or model's tracks (animation,
palette) with no stand-in chosen (or one not a valid model in the data), an animation track whose strips name a clip
its clip library hasn't, or with parameter cues (a stand-in has no state machine), a camera track cutting to a node
role or a light track to one (an empty node carries neither), an audio track when the sound is off or the music
doesn't load (the data's patches the cues name are registered first, over whatever the sound had under those names);
roles left unbound go from the copy's roles. The play is made beside the one before, which is ended and taken away
only once the new one plays (so a version `Play` refuses keeps the last); its stages instanced by the player at the
scene's root, the stand-ins under the preview's node. **A valid edit, or a stand-in chosen, plays the new copy from the
same time, paused as it was** (P35). Taken away (another entry selected): the copy finished, its stages, stand-ins and
hosts removed, the music it started stopped.

**The screens against Chromium** (P36, M11-20; the preview's 3D view left out: the fixture writer puts an empty box of
the `surface`'s size in its place): `preview-effect` (1000 × 640: a sound effect's controls), `preview-model` (1000 ×
640: the dolly stage's camera, clip, loop and time controls), `preview-cutscene` (1000 × 800: a small cutscene on the
dolly with a role of every kind, a figure chosen, a palette colour edited, a model role not chosen and its track left
out; its form's roles and tracks folded, their selects being step 9's known difference). Captured with
`claude-work/m11/step13-capture.sh` (step 8's: make-editor-references.sh's harness inside a headless cage, the output
2400 × 2400, one fixture per Chromium run at 1.65). The preview's selects take their line's room from no width of their
own (`flex: 1 1 0`): a select's width from its options is wider in the engine than in Chromium (M5's known difference,
14 px on the cutscene's), which put the first capture over the limit.

**Choices the design left to the step** (made here; the manager's word on those marked so, 2026-11-07): the names above;
the pane's layout (a 200 px high view, names 96 px, sliders with their value after them); the play's volume 0–2 and pitch
in semitones ±24; the theme's state line; the music part's line ("Playing alder-vale, which takes this entry's changes");
the stand-ins' rows and the left-out list's words; the cutscene starting paused; a model's Loop on at first and "Free
camera" first; the orbit camera's turn, zoom, limits and planes; the particle seed starting at 1; the distinct colours'
list; the files' names `volume.json` and `stand-ins.json` (one per concern, step 8's convention); model roles bound to a
model of the data (P34 names actor, palette, element and node roles; a model role takes a model as an actor takes a
figure); the confirmed choices above.

**Found** (reported to the manager): the engine's sound effects have no stop (Q2; a small task in Engine-Plan.md). The
first `Session.EditedData` relied on the session's events to know it was stale, and a listener subscribed before it
read a game entry's old value on the edit that made the mod's copy: it now takes its paths on every read.

**Results.** The screens against Chromium (pixels past 12 levels; the limit 0.2 %), GPU: `preview-effect` 448 and 1,556 pixels (0.070 % at 1×, 0.089 % at 1.65), `preview-model` 27 and 2,190 (0.004 %, 0.126 %), `preview-cutscene` 81 and 361 (0.010 %, 0.017 %); what is left is the form's (its selects, step 9's known difference) and the range thumbs' antialiasing. Silence: every sound test renders an audio system with no device and no thread (`new AudioSystem(new AudioGraph(rate, 2), library)`, rendered quantum by quantum by the test); the two program tests open the default device and play nothing on it. Mutations: 183 in the first run (`~/.cache/stat-fick-build/claude-work/m11/step13-mutations-{1,2,3,4,window}.json`, made by `make-step13-mutations.py` beside them; the program's wiring inside a headless cage): 154 killed, 24 survived, 5 didn't build. Of the survivors, 17 were gaps closed by new tests (an old emitter left on its node at Restart; a clip chosen keeping the time, played again from its end, looping from 0 instead of on, no rest pose without one; the note never hidden again; the drag without the pointer's capture and never ending; a slider's value not written; a library that doesn't load dropping the last; the framing on a skinned mesh's node and on the box's corner; a cutscene's view not ended when it goes, a refused play keeping its stand-ins; an unchanged record of an older version written again in the current one; Space with a dialog open, and Space the list typed ahead), 4 were redundant code, removed (the loop's own end check: the scene takes a done emitter off its node in the frame its clock got there; the sliders' change listeners: a range's change always follows its input; the cutscene registering the data's patches again: the swap registers those new or changed, and the effect preview only what the data holds; a `[hidden]` rule the UA's already holds), and 3 can't change what the checks see (two assignments swapped with nothing between them; the note's colour, in no screen: the fixtures show valid entries; the host's sound, which only gives the document its audibility, seen only with sound on a device). The rerun (`step13-mutations-rerun.json`, made by `make-step13-rerun.py`: the 17, the 5 rewritten to build, and 5 for the imported files the data as edited serves and the loop's restart as it is now) killed all 27: the list as it stands, 183, has 180 killed and 3 accounted for. Nothing written to the player's real folders.

**Not checked here** (by hand, in a real terminal: nothing may be heard or shown from a session): the preview in the
program's window (`editor.sh`: a model turned with the mouse and the wheel, a cutscene played); the sound by ear (an
effect at its play's volume, pan and pitch; a theme, its restart through the fades after an edit; the editor's volume
and its 40 ms ramp); Windows.

### Fixes after step 14 (2026-11-08)

Step 14's end-to-end flow found two faults in the editor and one look; **the manager's decisions (2026-11-08)**, fixed on
the branch `m11-fixes-1`:
- **Nothing in the UI started New entry (P15):** `EntryActions.New` had no caller. Decision (a): a **"New entry" button in
  the top bar, between Save As and Import, a command of its own (`EditorCommand.NewEntry`) with Ctrl+N**, enabled while
  `Session.CanAdd` (a pack that takes changes), opening step 9's New entry dialog (the selected entry's kind first); it
  works from anywhere a command does, also from a field of the mod info's own form. The button's tooltip is "New entry
  (Ctrl+N)". Its handler is `EntryActions`' own (`Form/EntryActions.cs`), the command and the key the shell's
  (`Shell/EditorCommands.cs`, `Shell/EditorView.cs`, `Shell/EditorView.Keys.cs`). Tests:
  `Editor/Form/NewEntryCommandTests.cs`, step 4's `ShellTests` and `CommandTests` (Ctrl+N; Ctrl+Shift+N none).
- **Enter in the import dialog opened the file browser again:** its path field's Enter listener closed the dialog without
  cancelling the key's default action, so the keypress reached the top bar's Import button the focus went back to (step
  14's probe: `keydown` on `input.import-path`, `keypress` and `click` on the Import button). Fixed as the New mod dialog
  and the entry dialogs do it: the listener cancels the key's default action (`Import/ImportPart.cs`). Test:
  `Editor/Import/ImportEnterTests.cs` (Enter imports and opens nothing else, the focus back on the Import button; Enter
  with a path refused keeps the dialog). The other dialogs' Enter handlers checked again: the entry dialogs (`NameForm`),
  New mod and the file browser's listing and path field cancel it; the reference list's Enter is a form field's own (no
  dialog closes); the Save As, keep-or-take and question dialogs have no text field, their buttons activated by Enter
  themselves.
- **Look: an empty "Stand-ins" heading:** the cutscene preview showed its heading with nothing under it for a cutscene
  without roles. Now hidden until a cutscene is taken and while the one taken has no roles (`Preview/CutscenePreview.cs`).
  Test: `Editor/Preview/StandInsHeadingTests.cs`.

**All 19 editor screens captured again** (the top bar has the new button), with `claude-work/m11/step11-capture.sh` (a
headless cage, the output 2400 × 2400 at both scales, one fixture per Chromium run). Each gives the counts it gave before
the button (main `4f6120e`, step 11's numbers) within 4 pixels at 1.65 and the same at 1×; with the new top bar against
the old goldens they were 0.16–0.70 % (`claude-work/m11/fixes-1/table.md`). One capture at 1× (`list-marks`) came without
the focused tree's outline (Chromium's window not focused in that run: 604 pixels) and was captured again, then 0.

### Step 14: the whole editor, end to end (2026-11-08)

**Files.** Tests only, in `Tests/Statventure.Tests/Editor/EndToEnd/`: `EndToEndTesting.cs` (a folder of the test's own in
the run's scratch folder, `PerUserFolders.Root/editor-end-to-end/<id>/`, holding the player's folder `player/` with its
mods, an installed game's folder `game/packs/` with copies of the repo's packs, and the files a modder imports,
`downloads/`; and the editor driven as a modder drives it: the top bar's buttons, New entry by its button or Ctrl+N, the
form's fields typed and committed with Enter, its selects, Add, Make and set boxes, the reference lists, the entry list's
filter (Ctrl+F) and rows, Import and Open through the file browser's path field, the JSON tab, the mod info's own form),
`ModFromScratchTests.cs`, `DevSetupTests.cs`, `FoundTests.cs`. Nothing outside the tests changed in this step (the faults
it found were fixed apart, "Fixes after step 14" above).

**A mod made from scratch through the UI** (`ModFromScratchTests`; the editor over the repo's packs, the harness's game
data, the player's folder the test's own):
1. *New mod* (the top bar): "Ember Rite" typed, the file name `ember-rite.pak` suggested, Enter: the mod written into the
   player's mods folder and opened, `mod/info` selected in its own form; its author, version and description typed there.
2. *A game entry changed in the mod:* the game's `audio/rooms/hall` found with the list's filter and clicked, its echo's
   seconds typed (4.5): the mod's own copy (P7).
3. *A sound effect* (`audio/effects/chime`): New entry by Ctrl+N (from a field of the mod info's form), the kind "Sound
   effect" and the name chosen in step 9's dialog; its required layers shown as a problem, a layer added (Add), its
   source chosen (Sine), its frequency, length and the effect's level typed.
4. *A particle effect* (`models/effects/sparks`): New entry by the top bar's button; the shape and spawn chosen (Disc,
   Point), the colours' slots typed, the life, size and speed ranges made (Make) and set, the fade-out and gravity typed.
5. *A stage:* `Dolly Stage.glb` (the tests' `dolly.glb`, a stage with cameras, a sun and its clip) imported by the
   Import button through the file browser, the path suggested (`models/dolly-stage`) typed over with `models/stages/dolly`,
   Enter: the entry is the packer's entry of the same file at that path. And the mod's readme, a `.txt` imported the same
   way at the path suggested (`readme.txt`), a file entry.
6. *A cutscene* (`world/cutscenes/rite`): New entry's button; its name and duration; its stage added and picked from the
   reference list; four tracks (a clip track playing the stage's timeline; a camera track; a particle track starting the
   sparks on the stage's crate, the effect picked from its list after typing "spa"; an audio track playing the chime, the
   cue's name set and picked from its list), the union's kind chosen per track.
7. *Save* (Ctrl+S): refused while the camera cut names `dolly/Lens` (the camera's name, not its node's): "Can't save" names
   the cutscene with its problem, which also shows under the field ("… cue 0's camera dolly/Lens: the stage
   models/stages/dolly has no node Lens", found by the checks across entries against the stage); the field typed again
   (`dolly/Camera`), Ctrl+S saves ("Saved ember-rite.pak.", the title's "•" gone). Then each entry's value and its JSON tab
   as the editor shows them.
8. *The game:* the saved mod mounted as the game mounts mods (`Application.DataMounts` over the game folder's packs and
   the player's mods folder) and read by the engine's own loaders: `Sequence.Load` (its stage, effect and sound checked
   against the same data), `ParticleEffect.Load`, `MusicLibrary.Load` (the chime among the game's effects, the mod's hall
   over the game's, the game's 55 themes all there), `ModelData.Load`, the mod's info: each the value the editor showed,
   by binary form and by the JSON tab's text; the readme's bytes as imported.
9. *Opened again* (Open, the browser's path field): the same seven entries, unchanged, no problem, the same JSON.

What it shows: a modder can make a mod of every kind the preview covers from nothing through the UI alone; what the editor
shows is what the game loads (M11-12's "a mod made by the editor always loads": the save refused the one broken
reference, the game's own loaders take everything it saved); the mod's copy of a game entry wins over the game's in the
game's own mounts.

**The dev setup** (`DevSetupTests`; P5, M11-5): the program's own reading of `--repo <engine folder>` and `--player-folder`
(`EditorArguments.Parse`) on a scratch copy of the repo's `engine/Assets/Packs` (never the real one), the editor's view
over that session (a small driver of the test's own: the shared harness is fixed to the repo's packs); Ctrl+O and the
game's `audio.pak` typed into the browser's path field: open as a game pack that takes changes; the harp found with the
filter, its level typed (0.375), Ctrl+S: saved in place. Every other entry of the pack keeps its stored bytes, `ui.pak`
and the repo's own `audio.pak` are untouched, and the pack's dump (`PackDump.ToText`, what `packer dump` and so
`textconv.sh` print) differs from the one before only at the harp: its `level`, `storedLength` and `sha256`. **The
manager's decision (2026-11-08):** the test reads the dump in-process, and `textconv.sh` itself is run once by hand on the
test's saved pack and recorded (running it inside a test would build the packer in Release during the run and again for
every mutation of the engine): `claude-work/m11/step14-textconv.sh` dumped the committed `audio.pak` and the saved copy
through the repo's `textconv.sh` as git runs it; the diff (`claude-work/m11/step14/textconv.diff`) is three lines of the
harp's entry, `"storedLength": 95` → `82`, its `"sha256"`, and `"level": 0.22` → `0.375`.

**Found** (reported to the manager; fixed apart on the branch `m11-fixes-1`, "Fixes after step 14" above, then this step
rebased on it, `00e5bd4`):
- *Nothing in the UI started New entry* (P15: `EntryActions.New` had no caller; the flow started it as step 9's tests do,
  until the fix's top-bar button and Ctrl+N; the flow now uses both).
- *Enter in the import dialog opened the file browser again* (the keypress reached the top bar's Import button the focus
  went back to; a probe logging every key and click event showed `keydown` on `input.import-path`, then `keypress` and
  `click` on the Import button). `FoundTests.EnterInTheImportDialogImportsAndOpensNothingElse` stays as the end-to-end
  witness (the fix's own test is step 12's `ImportEnterTests`).
- *A look:* the cutscene preview's "Stand-ins" heading with nothing under it for a cutscene without roles (now hidden).

**Every editor screen against Chromium at both scales**, rerun on the GPU and on lavapipe with the goldens `m11-fixes-1`
captured (none captured again here): pixels past 12 levels, the limit 0.2 %.

| Fixture | GPU 1× | GPU 1.65 | lavapipe 1× | lavapipe 1.65 |
| --- | --- | --- | --- | --- |
| `browser` | 0.004 % | 0.008 % | 0.004 % | 0.009 % |
| `checks-problems` | 0.050 % | 0.187 % | 0.050 % | 0.188 % |
| `dialog` | 0.009 % | 0.006 % | 0.009 % | 0.006 % |
| `form-controls` | 0.108 % | 0.139 % | 0.109 % | 0.140 % |
| `form-controls-end` | 0.115 % | 0.155 % | 0.116 % | 0.156 % |
| `form-json` | 0 | 0.001 % | 0 | 0.001 % |
| `import-again` | 0.055 % | 0.094 % | 0.057 % | 0.094 % |
| `import-path` | 0.009 % | 0.010 % | 0.009 % | 0.009 % |
| `import-replace` | 0.009 % | 0.006 % | 0.009 % | 0.006 % |
| `list-marks` | 0 | 0.002 % | 0 | 0.002 % |
| `list-unreadable` | 0 | 0.001 % | 0 | 0.001 % |
| `preview-cutscene` | 0.010 % | 0.017 % | 0.010 % | 0.017 % |
| `preview-effect` | 0.070 % | 0.090 % | 0.071 % | 0.093 % |
| `preview-model` | 0.004 % | 0.126 % | 0.004 % | 0.126 % |
| `references-list` | 0.046 % | 0.152 % | 0.046 % | 0.153 % |
| `saving-mod-info` | 0 | 0.002 % | 0 | 0.002 % |
| `saving-new-mod` | 0.009 % | 0.061 % | 0.009 % | 0.061 % |
| `saving-save-as` | 0.010 % | 0.021 % | 0.010 % | 0.021 % |
| `shell` | 0 | 0.002 % | 0 | 0.002 % |

The worst is `checks-problems@1.65` (0.187 % and 0.188 %: the known status line beside a tall scroller, and the select
widths). The same numbers as before the button (step 11's, also measured again by this step on `4f6120e`) within 4 pixels.
P36's six screens are all there (the unsaved-changes dialog is step 4's `dialog`, the same `Dialogs.Ask` markup), so no
fixture remained to add. Counted by `claude-work/m11/step14-screens.sh` (the screen tests with `STATVENTURE_KEEP_RENDERS=1`,
then `step14-compare.py`: any channel past 12 levels, as `Golden.Match` counts); tables in `claude-work/m11/step14/`
`screens-{gpu,lavapipe}.txt`.

**Mutations** (`~/.cache/stat-fick-build/claude-work/m11/step14-mutations.json`, made by `step14/make-mutations.py`): the
end-to-end tests add no code, so each mutation breaks a rule of the editor or the engine the flow goes through, and only
this step's tests run against it (`Editor.EndToEnd`): New entry's command, key and selection, New mod's folder, the mod
info's commit, the JSON tab's text, the pack's dump, the form's Add, Make, union switch and set box, the reference list's
pick, the list's filter, the import dialog's Enter, the checks across entries with the data, Save's refusal, what a save
writes (edited records, imported files, kept entries), `--repo`, the dev setup's writable game pack, the game's mounts
(the mods there and above the packs, the mods listed, the topmost source winning) and the loaders (the sequence's stages
and data check, the particle effect's check, the music's effects). 30 in the first run: 25 killed, 5 survived. Of those,
1 was a gap, closed (the flow imported no loose file, so leaving imported files out of a save went unseen: the mod's
readme is imported now), 1 a malformed mutation, rewritten (the mods mounted below the packs, but also still above
them), and 3 can't differ on what the flow saves: a kept entry added again from its record (every record of the
committed `audio.pak` is in the current version, and the writer keeps a content's stored bytes by its hash, so the bytes
are the same; a kept entry left out is killed), and `Sequence.Load`'s and `ParticleEffect.Load`'s own checks not run
(a mod the editor saved passes them by construction: Save refuses what they refuse, which the flow shows through the
checks across entries; their rules are step 6's and M10's tests). The rerun (`step14-mutations-rerun.json`, made by
`step14/make-rerun.py`: the gap, the rewritten one and a kept entry left out) killed all 3. The list as it stands, 31,
has 28 killed and 3 accounted for. Nothing written to the player's real folders (`mutate.py`'s watch), the repo's packs
unchanged.

**The full suite** passes on the GPU (1 min 33 s) and on lavapipe (1 min 57 s), inside a headless Wayland compositor under the
shared lock (`claude-work/m11/step14-full-run.sh`): 3496 tests (`main`'s 3493 at `00e5bd4` and this step's 3),
none skipped; the player's real game folder stayed missing, the Documents folder's top level and the repo's packs (this
worktree's and the main checkout's) unchanged.

**Renders for the user** (`claude-work/m11/step14/`): `render-cutscene@1.png` and `@1.65.png`, the whole editor (1280 ×
800 CSS px) with the finished mod's cutscene selected: the list, the form and the preview (the 3D view drawn on the
tests' GPU, the cutscene played from its start and paused at 0.8 s, the sparks out on the crate in M6's grey, as no
palette role colours them); and `ember-rite.pak`, the saved mod. To try it: copy the `.pak` into the player's mods folder
(Linux `~/.local/share/stat-fick/mods/`, Windows `%APPDATA%\Stat Fick\mods\`), or, to keep the real folder as it is, run
`editor.sh --player-folder <a scratch folder>` with the `.pak` in `<that folder>/mods/`, then Open it and select
`world/cutscenes/rite`; the game mounts it over its packs, but nothing in the sandbox plays that cutscene (the editor's
preview does). Its chime is heard only when the user plays it, in a real terminal, at a volume of the user's choosing.

**Choices the design left to the step** (made here; the dev-setup dump confirmed by the manager, 2026-11-08): the test
.glb (`dolly.glb`: a stage with a clip, cameras and a sun, small enough to import in a test); the mod's folder in the
run's scratch folder (what PerUserFolders redirects) rather than the system's temporary folder; the editor over the
repo's packs and the game's mounts over copies of them in a game folder of the test's own; comparing what the editor
showed by binary form (records holding lists don't compare by equality) and by the JSON tab's text; the mod's content
(a camera node typed wrong to show Save's refusal, a game entry changed to show the mods' order); the dev-setup test's own
driver; the two faults found kept as end-to-end witnesses (`FoundTests` and the flow's New entry).

## Status

**M11 done (2026-11-08): all fourteen steps on `main`, and the fixes step 14 found; ticked in `Docs/Engine-Plan.md`,
"Milestones".** The Statventure Editor makes a mod from nothing through its UI that the game loads as the editor showed it
(step 14), edits the game's own packs in the dev setup, and every one of its 19 screens matches Chromium 152 at 1 and 1.65
on the GPU and on lavapipe. **The full suite: 3496 tests, passing on the GPU and on lavapipe, nothing held**
(inside a headless Wayland compositor under the shared lock). Each step's own entry follows; what they left, gathered:

- **To check by hand, in a real window and by ear** (none may be shown or heard from a session; the user's, in a real
  terminal, when the user decides; `Docs/Engine-Plan.md`, "Small tasks left from earlier milestones", "By-hand checks"):
  (a) the shell (`editor.sh`): the window and its title, the zoom keys (Ctrl+= and Ctrl+-) through the host, a splitter
  dragged with the mouse and its cursor (step 4); (b) a real drop of files from a file manager under Wayland and X11 (a
  dropped `.pak` opens, other files import), closing by the title bar (asked once over unsaved work, the next close ends
  it) and from the taskbar while minimized (steps 3, 8, 12; `claude-work/m11/drop-check.sh` for the bare window); (c) the
  preview: a sound effect at its play's volume, pan and pitch, a theme played, edited while it plays (its restart through
  the fades) and stopped, Space from the list, the editor's volume and its ramp; a model turned with the mouse and the
  wheel, its clips, a stage's cameras; a particle effect looping; a cutscene with stand-ins played, paused and sought
  (step 13); (d) saving: New mod, Ctrl+S (the title's "•" and the list's marks go), Save As with entries unticked, closing
  with unsaved work (Save, Discard, Cancel) by the title bar (step 11), with `editor.sh --player-folder <a scratch
  folder>`; (e) step 14's mod by hand: New entry by the top bar's button and by Ctrl+N, the reference lists and the JSON
  tab with the mouse, and `claude-work/m11/step14/ember-rite.pak` opened (its cutscene previewed, the chime heard at a
  volume of the user's choosing). The dialogs and the lists of steps 8–12 are part of these.
- **Windows** (nothing of M11 has run there; `Docs/Engine-Plan.md`, "Windows check", items 1 and 8): the whole suite
  (`test.ps1`, the editor's tests and screens among them); writing over a pack held open (steps 2 and 11, item 8a);
  import's default lookup on a file system that ignores case (steps 2 and 12, 8b); a real drop and a real close (steps 3,
  8 and 11, 8c); the editor in a real window, with no `editor.ps1` yet (step 4, 8d); the file browser's places (drive
  roots, the Documents known folder, names compared ignoring case; step 3 and 8, 8e); the preview's sound through OpenAL
  Soft (step 13, 8f); a mod made and saved into `%APPDATA%\Stat Fick\mods\` and loaded by the sandbox (steps 11 and 14,
  8g).
- **Known limits, by design** (each recorded with its decision): one pack open at a time, over the game's packs only (P6),
  so a mod that refers to another mod's entries can't be saved (P18); adding, removing and renaming entries aren't undoable
  (M11-7); importing again matches values by path, so a node inserted in the file shifts the names after it (P29, step 12);
  the editor shows the first problem per field (step 10); the JSON tab shows at most 2,000 lines (copy and export give
  all); Stop doesn't cut a sounding effect (the engine's effects have no stop; step 13); a cutscene's element roles' stand-ins
  have no stylesheet (step 13, Q5); no preview for state machines, UI files and terrain, and no special views (a cutscene
  timeline, a map view; M11-2, M11-13); the editor and the packer know the engine's kinds only until the port answers G3;
  the repo's script-made packs still pack whole (P30).
- **Engine findings, recorded as small tasks** (open, no go yet; `Docs/Engine-Plan.md`, "Small tasks left from earlier
  milestones"): control labels a device pixel low at 1.65 (step 4 and on); a composited box's text grayscale at 1.65 where
  Chromium's is LCD (and an editor dialog over a composited scroller, step 12; a status line beside a tall scroller, step
  10); a select with its own padding 6–7 px wider than Chromium's (step 9); the painter paints every row of a scroll
  container (step 8); a textarea's `line-height` (step 11); a positioned box with a z-index inside a scroller, composited
  apart in Chromium, and a stacking context on the form's rows (step 10); `ValueShape.TextOf` made public (step 10); a stop
  for sound effects (step 13).

**M11 started (2026-11-06):** the user's go; the decisions recorded (M11-1–M11-21 and the user's rule for
M11); what exists checked against the code; the method; the design proposed, with 38 editor choices for the
manager (P1–P38) and six questions for the user about the game (G1–G6); fourteen steps proposed with their
dependencies. No step started; nothing implemented.

**The design decided (2026-11-06):** P1–P38 by the manager, all as recommended (P18 with its known limit:
a mod referring to another mod's entries can't be saved in M11); G1 and G6 by the manager; G4 and G5 by the
user (the game enforces declared limits; the play-time checks also run at load, the cost measured first);
G2 and G3 kept for the port. The fourteen steps under "Steps" are the plan; none has started.

**Step 1, record shapes, drafts and marks (2026-11-06, on its worker branch `m11-step1`):** done as "How it
works", step 1, says. Tests in `Tests/Statventure.Tests/Records/`: `RecordShapeTests` (every field kind's
value shape on `test.kinds`, fields in number order with names, defaults and required ones, display names,
kinds' names, folders and rules, every mark read, conditions, bulk summaries, the name rules, 26 marks that
don't fit refused), `RecordDraftTests` (all 50 fixtures and audio.pak's 118 records read into drafts and built
back to the same binary form, a blank draft of each of the 78 engine record types a draft can be of, every
build failure at its path, an `FCurve` refusing its keys at the curve, wrong nodes refused, reading through
the shapes with bulk values kept by reference, changes by field and by path, through lists and maps of maps,
list and map operations),
`RecordCheckTests` (limits, choices and references with their paths and conditions, each type's own checks
nested and in unions, checks across entries given the data, misdeclared checks refused, paths as the
serializer writes them); marks and checks on the `test.*` kinds in `TestRecords.cs` (no signature changed),
misdeclared ones and shapes the guarded kinds lack (lists and maps of maps, an enum declared out of order, a
kind without a folder) in `MarkVariants.cs`. 106 new tests; the full suite passes on the GPU and on lavapipe
(2782 on `0d8e175`: main's 2676 and 106 new), run inside a headless Wayland compositor so no test window showed on the user's screen
(`Docs/Notes.md`, 2026-11-06). Mutations: 277 in the first run (`~/.cache/stat-fick-build/claude-work/m11/`
`step1-mutations.json`, made by `make-step1-mutations.py` beside it, run in four chunks), 266 killed; the 11
survivors: 7 gaps, closed by new tests (an enum declared out of numeric order, a kind without a folder whose rule
ignores folders, an abstract rule with a public constructor, a record not made when a field it can't do without
failed, paths through a list of maps and a map of maps, limits on a nullable list), 3 redundant code, removed (a
blank's own union check, which the draft's constructor makes in the same words; the "folder itself" length test,
which the path rule covers; a non-member enum key's text, which its `ToString` already gives), and 1 gap in code then
rewritten (the folder test of the name rules, a path of the folder's length differing in its letters); the rerun
(`step1-rerun.json`, 16: the 7 survivors and 9 mutations of the code changed) killed all 16, so the list as it stands
(276) is all killed. Nothing written to the player's real folders. No question came up that the design doesn't
answer; nothing of P1–P38 or G1–G6 changed. Not checked: Windows.

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

**Step 3, the window, the sound and the file browser's model (engine): done (2026-11-06, on the worker branch
`m11-step3`).** ("How it works", step 3; `Docs/Assets.md`, "The file browser's model" and "The mounts"; `Docs/Audio.md`,
"The output".) Files dropped on the window are an event of the window with their paths (`Window.PathsDropped`), which
the game doesn't listen to; a close the application can refuse (`Application.ConfirmClose`, true by default: the loop
ends where it always did), asked once per request before the next frame and, while minimized, after the update, a
refusal keeping the loop running until the next close or `Quit`; S9-D's music swap public (`AudioSystem.Reload`); the
file browser's model (`FileBrowser`: places, listing, path reading, completion; `RecentPlaces`); and, added on the
manager's word, `ApplicationOptions.GamePacksOnly` (the editor's data: the area packs only, so a damaged mod can't stop
its start). 22 new tests (`WindowDropTests` 3, `ApplicationCloseTests` 6, `ApplicationGamePacksTests` 2,
`AudioReloadTests.TheSwapIsPublic`, `FileBrowserTests` 9, `FileBrowserDocumentsTests` 1). The full suite passes on the
GPU and on lavapipe (inside a headless compositor): 2825 tests (`main`'s 2803 at `d5b8b39` and 22 new), none skipped. On the manager's word the older tests' windows are hidden too (`TestApplication`, `ApplicationMusicTests`, `UiResourcesTests`), all but the swapchain test's ("How it works", step 3, "Found"), and `FolderListing`'s comment is corrected; the step's choices are confirmed by the manager (2026-11-06). Mutations: 84
(`~/.cache/stat-fick-build/claude-work/m11/step3-mutations.json` and `-2.json`, made by `make-step3-mutations.py`
and `-2.py` beside them), 83 killed; one accounted for (a folder removed between the path's reading and its listing
during a completion: only a race can reach that catch, as a folder that can't be read lists as empty); nothing
written to the player's real folders. The game and the sandbox behave as before: neither listens for drops, refuses a
close or sets the option. Not checked: a real drop from a file manager (X11, Wayland, Windows) and a real close by the
title bar and from the taskbar while minimized, by hand (`claude-work/m11/drop-check.sh` runs a window that logs
both, once this is on `main`); Windows.

**Step 4, the shell: done (2026-11-06, branch `m11-step4`).** The program, the session's skeleton, the view (top
bar, panes, splitters, status line), parts found without a list, commands and keys, dialogs, the title and the zoom;
the empty shell and a dialog match Chromium at 1 and 1.65; the conventions for steps 8–13 ("How it works", step 4).
79 new tests (`Tests/Statventure.Tests/Editor/`); the full suite passes on the GPU and on lavapipe: 2676 tests
(`main`'s 2597 at `2323779` and 79 new). Mutations: 189 (`~/.cache/stat-fick-build/claude-work/m11/step4-mutations.json`),
183 killed, 6 accounted for ("How it works", step 4, "Results"), nothing written to the player's real folders.
Found and reported (the engine not changed): text first in a paint chunk is drawn with the previous chunk's LCD
verdict (a worker of its own on the painter; fixed on 2026-11-06, branch `lcd-first-text`: "How it works", step 4,
"Found"); the editor can't start without mounting the player's mods (step 3);
the capture harness's iframe root isn't composited as a main frame's is (the stylesheet rule, convention 2). Not
checked: the program in a real window (to check by hand), Windows. Remembering the zoom and the window's size (P4,
G6): step 8, the manager's decision (a) of 2026-11-06 ("Steps", step 8).

**Step 5, audio kinds: checks and marks (engine): done (2026-11-07, on the worker branch `m11-step5`, rebased on
`1f598ec`).** ("How it works", step 5; `Docs/Audio.md`, "Checks and marks".) Each audio kind's check with the paths
of what it names: the effect patch's rules in their old words (`Problem()` now the first of `RecordChecks.Check`, so
the game enforces the declared limits, G4), the theme against the composer (G5: the room, the score compiled once and
walked only where it fails, each played section's layers by the composer's rules; every rule broken once, each
verdict the composer's own), the theme order's. Marks: the kinds with their folders and the music's name rule
(`MusicNameRule`), help on every audio field, units, choices (mode, layer type and texture, instrument kind),
references (six fields), limits on the effect patch only (the four numbers its check held). **The theme check runs
when the data loads** (after the older checks, keeping their words), the user's decision of 2026-11-07 after the step
measured it and stopped: a cold start's loading 48.9 → 69.6 ms on the game's thread (the compile of all 55 themes
moved there from the warm-up thread, 3.5 ms more work in all), warm about 3 ms more, a Reload about 5 ms more; the
user accepted that a silent layer naming an instrument that isn't there is refused, and that four of the tests'
libraries, whose sample theme doesn't compile, are given a playable one. All 55 themes, the committed `audio.pak`'s
118 records and the proof cutscene's three patches pass every check, mark and reference; every music test is
unchanged (no line of the score's or the composer's logic changed). 115 new tests (`ThemeCheckTests` 93,
`AudioMarksTests` 19, `MusicLibraryTests` 3). The full suite passes on the GPU and on lavapipe, headless: 2940 tests
(`main`'s 2825 and 115 new). Mutations: 124 (`~/.cache/stat-fick-build/claude-work/m11/step5-mutations.json`, made by
`make-step5-mutations.py` beside it; the wiring's 8 also in `step5-mutations-2.json`), all killed, nothing written to
the player's real folders. Found: step 1's `CheckContext` gives no entry path, so the theme's id rule stays in `Load`;
the duplicates step 10 will meet. Not checked: Windows (nothing of the step depends on the system).
**Step 6, the scene, model, particle, terrain, state machine and stylesheet kinds (engine): done (2026-11-07, on the
worker branch `m11-step6`, rebased on `3ee8d7b`).** ("How it works", step 6; `Docs/Scene.md`, "M11 step 6";
`Docs/Assets.md`, "Checks with paths and marks".) Each kind's rules with the path of what they name, their words
unchanged (every existing rule's test checks both); going on past a problem; `FCurve.ProblemsOf`; the stylesheet's
rules in a reporting mode; the marks (names, folders, help and units on every particle effect and cutscene field, 34
limits each held by the kind's own rule, bulk, colours, three references), recorded per kind and guarded; the sequence
against the data, **wired into `Sequence.Load` by the user's decision of 2026-11-07** after the measurement, the
sandbox's cutscene mode and the proof's tests mounting the sound pack; everything that loads today passes (the
fixtures, the test models and terrain, the proof and its effects, the test sequence, the machines, the game's
compiled stylesheet; the committed packs hold none of these kinds). 67 new tests; the full suite passes on the GPU
and on lavapipe, headless, on `3ee8d7b`: 3156 tests (`main`'s 3089 and 67 new; one flaky failure outside the step in
the first GPU run, `ScriptApiTests.WorkFromAnotherThreadComesBackAsATaskOfTheLoop`, reported). Mutations: 347 (`~/.cache/stat-fick-build/claude-work/m11/step6-mutations.json`, made by `make-step6-mutations.py` beside it; five chunks and a rerun): 344 in the first run, 333 killed; of the 11 survivors 7 were gaps, closed by new tests (a visibility track's node at its path, a curve made with several problems refused with the first, a terrain without a grid not compared with its counts, a stylesheet's value token, unquoted url, keyframe url and a keyframe's refused second declaration at their paths), 1 was code rewritten (the stylesheet's url check scanned @font-faces too, reporting a face refused for its src url twice: it now leaves them to `FontFaceDescriptors.Of`), and 3 can't change what any check sees (`Sequence.Validate()` and `Validate(stages)` without the declared limits: every declared limit is one the kind's own rule holds first, as the G4 test shows; each data entry read once per check, a cache); the rerun (11: the 7, the url check's new mutation and the audio cue's three reference marks) all killed. Nothing written to the player's real folders. Nothing written to the
player's real folders. Decided by the manager on the report: one problem per field shown (step 10), `AudioCue.Name`
with step 5's rule (marked here once rebased on step 5), kinds' folders, no check in `Stylesheet.Load`. Step 9's
`EngineKindsFormTests` gives a bulk field its object when it fills a draft and leaves a kind of bulk fields only (a
compiled stylesheet) unedited, as step 1's draft test does. Not checked: Windows.

**Step 7, the mod's info entry (engine): done (2026-11-06, on the worker branch `m11-step7`).** ("How it
works", step 7; `Docs/Assets.md`, "The virtual file system", "A mod's info".) The record `mod.info` (`ModInfo`:
name, author, version, description, as text, each empty by default) at `mod/info`, with its layout and two
fixtures (`every-field`, `empty`) under the layout guard; both forms' round trips, its JSON form exactly, keys left
out of a hand-written form, a pack's info in the dump, extracted and packed again to the same bytes, loaded from a
pack and a loose folder; a mod with the entry (in both of two mods, in one, in neither) giving the game the same
mounts, logged lines, paths, contents and music as without it; an entry the engine can't read (five kinds)
changing nothing until it is read, then an error naming the mod; the committed packs holding nothing in `mod/`.
13 new tests (`ModInfoTests` 6, `ModInfoModsTests` 7). The full suite passes on the GPU and on lavapipe, inside a
headless Wayland compositor (`cage`), rebased on step 1 (`65ee082`): 2796 tests (`main`'s 2782, 13 new, and one
more case of step 1's blank-draft test over every engine kind, `mod.info`'s). Mutations: 54 run (`~/.cache/stat-fick-build/claude-work/m11/step7-mutations.json` and
`step7-mutations-2.json`, made by `make-step7-mutations.py` and `make-step7-mutations-2.py` beside them): 43
killed (the kind's name, version, attribute and visibility; five paths; each field's number, attribute, name,
nullability, required flag and default, two fields swapped, two on one number, a fifth field; and four ways of
the game reading or checking a mod's info: mounting that logs its name, reads it, verifies every entry, or a file
system hiding it), none survived; 11 of the first list didn't build (renames, `required` and three type changes
break the tests' own code), and the 8 renames and `required` were rewritten to build (a renamed field with the C#
name kept as a plain property; `required` with a `[SetsRequiredMembers]` constructor), all killed; the three type
changes (`string[]`, `int`) have no form every user of the record still compiles against (the nullable ones are
the type changes that build, killed). Nothing written to the player's real folders. A mod carrying the entry changes nothing the game
does (no listing, check or log line of the game's); nothing of P1–P38 or G1–G6 changed; the step's own choices are
at the end of "How it works", step 7 (among them each field defaulting to empty text, not required). Not checked:
Windows (nothing of the step depends on the system).

**Step 9, the form, undo and the JSON view (editor): done (2026-11-07, on the worker branch `m11-step9`, rebased on
`6a8cd78`).** ("How it works", step 9.) The session's entries (the set shown, the pack's over the game's, the
selection, commits with each entry's own history, what counts as changed, the problems with a hook for step 10's
checks; New, Duplicate, Rename, Remove and Revert, each reported) and the form over them: a row per field of every
kind of value through UI events (`test.kinds`' 63 fields: each integer kind, floats in their shortest form, text,
colour text and vectors, choices and members, nullables set and unset, vectors, lists, maps and pairs with their
keys, records, a record not given made, unions switched keeping their shared fields, bulk and read-only values never
changed), text that doesn't read kept out and marked, a slider's drag one step; undo and redo per entry by the keys
and buttons, a field's own typing undone and redone first; the entry dialogs asking first (names in the kind's
folder by its rule, paths checked), F2 and Delete; the JSON tab with copy and export (through the file browser's
`IExportFilePicker`, step 8's); every engine record kind shown and edited, every record of audio.pak shown as read.
The screens against Chromium (P36, at 1 and 1.65; the paint fixtures' limits, 12 levels and 0.2 %):
`form-controls` 0.108 % and 0.139 %, `form-controls-end` 0.115 % and 0.155 %, `form-json` 0 and 0.001 %; most of
the form's differences are one engine rule, found and reported (a select with author padding 6–7 px wider than
Chromium's). 149 tests (`Tests/Statventure.Tests/Editor/Form/`); the full suite passes on the GPU and on lavapipe
inside a headless Wayland compositor: 3089 tests (`main`'s 2940 on `3e6df0f`, where they ran, and 149 new). Mutations: 231 run (`~/.cache/stat-fick-build/claude-work/m11/step9-mutations.json`, made by `make-step9-mutations.py` beside it; four chunks, then two reruns, `step9-rerun.json` and `step9-mutations-rerun2.json`): 207 killed at first; of the 22 that survived, 16 were gaps closed by new tests or assertions (a new entry shown, the removed list on a new set, a removal and a rename alone as unsaved work, an untouched entry whose record can't be written, an alpha typed into a Vector3, a new integer key above the largest, a colour text that isn't one in the swatch (two), a member's display name of two words, a union's switch between fields of one name and two kinds, a map entry removing itself, Undo and Revert once the pack takes no changes (two), a rename to the same name, a cancelled dialog's additions: with it, a same-name rename no longer runs them), 3 were redundant code, removed (a game entry's history cleared on a revert, though it was never shown; a field's mark cleared twice; the read-only guards no event reaches), and 3 are looks under the screens' limits (the problems' red, the selected tab's underline, the head's bold path: a few hundred pixels each where the form screens are at 0.11–0.16 % of the 0.2 %); 2 didn't build and were rewritten; the reruns (24) killed all, so the list as it stands (228) is all killed but those 3 looks; nothing written to the player's real folders. The manager's decisions of 2026-11-06 recorded (Revert,
P7; New's kinds, Export until the browser, the JSON tab's 2,000 lines, Delete outside the rows, elements from 0,
the problems' red, confirmed). Not checked: the program in a real window (to check by hand), Windows.

**Step 8, opening packs: the browser, drops and the entry list: done (2026-11-07, on the worker branch `m11-step8`).**
("How it works", step 8.) The session opens one pack over the game's packs (P6), every entry read and classified (a
record, a file, or unreadable with its reason in the dump's words: damaged, not a record, a kind not known, a newer
version, values its type refuses), read-only when any can't be read (M11-16) or when it is one of the game's packs
outside the dev setup (M11-5), a pack `Open` refuses said with why and nothing changed, the base of the game's other packs,
and the import queue drops fill (step 12 takes it); the file browser as a dialog over the shell (places with the game's
packs, a folder's listing with sizes and dates, the path field with completion, the endings of what is picked, recent
places), Ctrl+O and drops (P27), the unsaved-work question when opening and closing (Discard, Cancel; step 11 adds Save),
`GamePacksOnly` and `ConfirmClose` wired; the entry list (P8: a tree by path with the kinds' names, the marks, the
filter, the keys and type-ahead), its rows made only where seen after P37's measurement (a frame took 109 ms with 2,021
rows painted; 1.4 ms now); the `editor/` settings (`shell.json`: the zoom level and the window's last normal size;
`recent-places.json`), the manager's decisions of 2026-11-07. The screens against Chromium at both scales: `list-marks`
0 and 0.002 %, `list-unreadable` 0 and 0.001 %, `browser` 0.004 % and 0.008 % (the limit 0.2 %); the shell's, the dialog's
and step 9's screens captured again with the list in their entry pane, as before. Found and fixed: the capture harness's
window sat out of X11's range at 1.65 (no focus rings in captures made under cage); found, not changed: the painter
paints every row of a scroll container, seen or not. 94 new tests (`Tests/Statventure.Tests/Editor/Opening/`); step 4's
and step 9's tests that assumed no Open handler, no file browser or the dialog first in the fixture list now see this
step's parts. The full suite passes on the GPU and on lavapipe, inside a headless Wayland compositor: 3250 tests (`main`'s 3156 at `22ee667` and 94 new), none skipped (the GPU 1 min 15 s, lavapipe 1 min 41 s; the screens on lavapipe within a few pixels of the GPU's).
Mutations: 251 in the first run (`~/.cache/stat-fick-build/claude-work/m11/step8-mutations.json`, its chunks `-1` to `-4` and `-window`, the editor application's wiring run inside a headless cage; made by `step8/make-mutations.py`): 229 killed, 18 survived, 4 didn't build; 3 survivors were redundant code, removed (the tree's press handler: a press focuses the tree anyway; Right's check that an open folder's next item is inside it, which it always is; a `[hidden]` rule the browser's own stylesheet has), 12 were gaps closed by tests (a game pack's problem named without its folder, a compressed file's size, a settings file that isn't an object, the "Hidden" box kept when going to a folder, the selection kept when listing again, a size between 1,000 and 1,024 of a unit, a dialog's task read only once done (one survivor was a test waiting on a question a broken rule left open), a refused drop's own task, a relative name dropped, the status line's capital, the overscan's number, and the window's options in the settings tests' filter), and the 4 that didn't build were rewritten to build; the rerun (`step8-mutations-rerun.json`, 16) killed all 16. The list as it stands, 248, has 245 killed and 3 accounted for: looks under the screens' limits (the marks not ember, the active row's 1 px outline, an open folder's triangle closed: a few hundred pixels of 640,000). Nothing written to the player's real folders. Renders for the user:
`claude-work/m11/step8/render-{list,browser}-audio@{1,1.65}.png`. Not checked: a real drop from a file manager and a real
close by the title bar (to check by hand), the editor in a real window, Windows.

**Step 10, checks and references in the editor: done (2026-11-07, on the worker branch `m11-step10`).** ("How it works",
step 10.) The manager's decisions Q-A (a removal's referrers offered "Point them at:" another entry of the kind, "Leave
them" by default; a rename's "Update them to the new name", ticked) and Q-B (the cross-entry problems of the last run
stay until the next replaces them) recorded there. Each entry's own check at every change of its draft; the checks across
entries (step 13's `Session.EditedData`, the session's own references, then the game's music loading) 200 ms after the
last change, over every record entry, the game's too; only the first problem per path; problems at their fields, at the
top and in the list through steps 8's and 9's display; `Session.EntriesWithProblems`, `Checks.DataProblem` and
`Checks.RunPending()` for step 11's save; the reference field's list (in the row under the field, filtered as typed, the
flag); rename and remove listing the referrers and updating them in the same step. Tests: 46 (42 in
`Tests/Statventure.Tests/Editor/Checks/`, the two screens at two scales). Screens: `checks-problems` 0.050 % and 0.187 %,
`references-list` 0.046 % and 0.152 % (1 and 1.65, the GPU). The full suite passes on the GPU (1 min 28 s) and on lavapipe (1 min 55 s), inside a headless Wayland compositor, rebased on step 13's merge (`034b83c`): 3423 tests (`main`'s 3377 and 46 new), none skipped (before the rebase, on `ac2306e` with step 13's `Session.EditedData`: 3296, 1 min 24 s and 1 min 52 s); the player's real game folder stayed missing and the Documents folder's top level unchanged. Mutations: 176 in the first run (146 killed, 4 didn't build and were rewritten, 26 survived: 11 gaps closed by tests, 7 redundant code removed, 2 equivalent, 6 looks: 3 under the screens' limits and 3 of the dialogs no fixture shows), then a rerun of 20 (the gaps, the rewritten, the code changed since): 19 killed, 1 equivalent (an entry that doesn't build has no value to check); nothing written to the player's real folders. Not
checked: the editor in a real window (the list, the dialogs), Windows.

**Step 11, saving and mods (editor): done (2026-11-08, on the worker branch `m11-step11`, rebased on step 10's merge `7e3ff8d`).** ("How it works", step 11.)
The step stopped at six questions before writing code; the manager decided them on 2026-11-08, each (a): the mod's info in a
form of its own in the form pane through a small documented hook in step 9's `FormPart` (`IEntryForm`); New mod writing the
mod at once with `mod/info` holding its name; Save As's ticks first, then the browser, then the new file open in place of
the old, the unticked entries with unsaved changes named as let go; a pack it can't fully read offering its readable
entries ticked, the intact ones it can't read unticked (kept byte for byte when ticked) and the damaged ones not offered;
the subset checked as the game would see it (step 10's checks against the game's packs with only the ticked entries on
top, then the music's loading); histories kept across a save; and the step's smaller choices confirmed. Save (Ctrl+S, the
button) writes the whole pack as step 2 says (unchanged entries as stored, older versions too; edited, new and copied
records in the current versions; imported files with their bytes; renamed ones at their paths; removed and reverted ones
left out; the file swapped in), refused while an entry has a problem, naming each with its first (M11-12, step 10's
`RunPending`, `EntriesWithProblems`, `DataProblem`); the file's index hash compared first (P23: Overwrite, Save As, Cancel);
the pack read again after (its hash refreshed), the entries saved in place (their marks and the title's "•" gone, their
histories kept). Save As as above; New mod (its name and file name under the path rule, mods' file-name order said) into
the player's mods folder, its info selected in its own form; a new mod saved and mounted by `Application.DataMounts` as
the game mounts mods reads back equal; the dev setup's game pack saved in place (a scratch copy of the repo's packs);
closing and opening over unsaved work offer Save (step 8's `Opener.SaveFirst`). 62 new tests (`Tests/Statventure.Tests/Editor/Saving/`: 56,
and the three screens at both scales); step 4's `ShellTests` (New mod always enabled) and step 8's `OpenerTests` (its
Discard-and-Cancel part sets `SaveFirst` to null) take step 11's handlers. The screens against Chromium at 1 and 1.65 (pixels past 12 levels, the limit 0.2 %, the
GPU): `saving-save-as` 0.010 % and 0.021 %, `saving-new-mod` 0.009 % and 0.061 %, `saving-mod-info` 0 and 0.002 %; every
other editor screen captured again (its top bar changed: New mod always enabled, Save and Save As with a pack open), each
giving the counts it gave before. The full suite passes on the GPU (1 min 33 s) and on lavapipe (2 min 2 s), inside a
headless Wayland compositor, rebased on step 10's merge (`7e3ff8d`): 3485 tests (`main`'s 3423 and 62 new), none skipped;
the player's real game folder stayed missing, the Documents folder's top level and the repo's packs (this worktree's and the
main checkout's) unchanged. Mutations: 232 in the first run (`~/.cache/stat-fick-build/claude-work/m11/step11-mutations.json`,
made by `step11/make-mutations.py`): 205 killed, 27 survived: 14 gaps closed by new tests, 6 redundant lines removed, 7
accounted for (1 equivalent, 1 the engine's textarea line height, 5 looks under the screens' limits or in no fixture); the
rerun (20) killed all; the list as it stands, 228, has 221 killed and 7 accounted for; nothing written to the player's real
folders.
Renders for the user: `claude-work/m11/step11/render-saving-{save-as,new-mod,mod-info}@{1,1.65}.png`. Found: the engine's
textarea ignores the author's `line-height` (an open item in `Docs/Engine-Plan.md`). Not checked: the editor in a real
window (saving, New mod, Save As and the close by hand: Engine-Plan.md, "By-hand checks", (d)); Windows (writing over the
open pack, "Windows check", item 8a).

**Step 12, import (editor): done (2026-11-07, on the worker branch `m11-step12`, rebased on `c26a6ed`, its full runs on `ac2306e`).** ("How it works", step 12.)
The step stopped at four questions before writing code; the manager decided them on 2026-11-07 (each the recommended option:
P29's values matched by path along the kind's shape with its known limit, files' folders by ending, a re-import one
undoable step and anything else a new entry or a replacement asked first, one file at a time with the dialogs only when
the path is taken) and confirmed the step's own choices. Each kind the editor imports, by the Import button through the
file browser and by a drop, is the packer's entry of the same file at the same path (a `.glb` model, a `.terrain.json`
terrain with its images beside it, a `.css` stylesheet whose urls are checked against the game's packs, the mod and what
the session imported, a record's `.json` form, `.ttf`, `.otf`, `.png`, `.svg` and `.txt` files); the path suggested from the
kind's folder by its rule and the file's name made to fit the path rule, editable, its problems said in the dialog; the
replace question for an entry already there (the pack's listed as removed, a game entry kept under the new one); a record
imported again over its kind with the bulk data from the file and each differing value kept or taken (Keep all, Take all;
one step of the entry's history; over a game entry, the mod's copy); a record's JSON form exported from the JSON tab back
in (M11-18). Importing never writes the pack. The screens against Chromium at 1 and 1.65: `import-path` 0.009 % and 0.010 %, `import-replace` 0.009 % and 0.006 %, `import-again` 0.055 % and 0.094 % (pixels past 12 levels, the limit 0.2 %). Found and recorded:
an editor dialog over a composited scroller has grayscale text at 1.65 where Chromium's is LCD (an open item in
`Docs/Engine-Plan.md`, "Small tasks left from earlier milestones"). 61 new tests (`Tests/Statventure.Tests/Editor/Import/`: 55 and the three screens at both scales);
step 8's drop test and application test see step 12's part take the queue. The full suite passes on the GPU and on lavapipe, inside a headless
Wayland compositor: 3311 tests (`main`'s 3250 at `ac2306e` and 61 new), none skipped. Mutations: 221 (`~/.cache/stat-fick-build/claude-work/m11/step12-mutations.json`, made by `step12/make-mutations.py`): 195 killed; of the 26 survivors 17 were gaps closed by new tests, 3 redundant lines removed, 2 killed in fact (the test run crashed on a stack overflow, which `mutate.py` reads as passing: checked by hand), 1 a cache, 3 looks under the screens' limits; the reruns (17, then 1) killed all; nothing written to the player's real folders. Not checked: a real drop and the dialogs in a real window (by
hand), Windows (`ImportSources`' default lookup on a file system that ignores case: "Windows check", item 8b, can now be
done in the editor).
**Step 13, the preview (editor): done (2026-11-07, on the worker branch `m11-step13`, rebased on `fa56257`).** ("How it
works", step 13.) The manager's decisions of 2026-11-07 on the step's six questions recorded there (the program's
wiring, Stop and a sounding effect, the editor's volume at 50 %, the data as edited owned here, element roles without a
stylesheet, a preview palette per palette role). `Session.EditedData`, the data as the game would see it after saving
(P38), written first for step 10 too. The preview pane: sound effects (Play, Stop, Space, the editor's volume, the play's
volume, pan and pitch; each valid edit registered at once) and themes (the library from the data as edited swapped in,
the theme following valid edits of itself and of its music while it plays, the last library that loaded kept), particle
effects looping at the origin in the preview's palette with a seed and Restart, models with their clips, the turnable
camera and a stage's cameras, cutscenes played, paused and sought with stand-ins chosen per cutscene and remembered, the
tracks that can't be bound left out and listed; an invalid edit keeps the last valid state, saying why (M11-12, P35).
`editor/volume.json` and `editor/stand-ins.json`. The screens against Chromium at both scales (the preview's 3D view left
out): `preview-effect` 448 and 1,556 pixels (0.070 % at 1×, 0.089 % at 1.65), `preview-model` 27 and 2,190 (0.004 %, 0.126 %), `preview-cutscene` 81 and 361 (0.010 %, 0.017 %) (pixels past 12
levels, GPU; the limit 0.2 %). 60 new tests (`Tests/Statventure.Tests/Editor/Preview/`) and the screens' six cases;
step 8's options test expects the sound on. The full suite passes on the GPU and on lavapipe, inside a headless Wayland compositor (`claude-work/m11/step13-full-run.sh`): 3377 tests (`main`'s 3311 at `fa56257` and 66 new), none skipped (the GPU 1 min 48 s, lavapipe 1 min 31 s; the screens on lavapipe within a few pixels of the GPU's: `preview-effect` 0.071 % and 0.093 %, `preview-model` 0.004 % and 0.126 %, `preview-cutscene` 0.010 % and 0.017 %); the player's real game folder stayed missing and the Documents folder's top level unchanged (also on `ac2306e` before the rebase, 3310 tests). Mutations: 183 in the first run (`~/.cache/stat-fick-build/claude-work/m11/step13-mutations-{1,2,3,4,window}.json`, made by `make-step13-mutations.py` beside them; the program's wiring inside a headless cage): 154 killed, 24 survived, 5 didn't build. Of the survivors, 17 were gaps closed by new tests (an old emitter left on its node at Restart; a clip chosen keeping the time, played again from its end, looping from 0 instead of on, no rest pose without one; the note never hidden again; the drag without the pointer's capture and never ending; a slider's value not written; a library that doesn't load dropping the last; the framing on a skinned mesh's node and on the box's corner; a cutscene's view not ended when it goes, a refused play keeping its stand-ins; an unchanged record of an older version written again in the current one; Space with a dialog open, and Space the list typed ahead), 4 were redundant code, removed (the loop's own end check: the scene takes a done emitter off its node in the frame its clock got there; the sliders' change listeners: a range's change always follows its input; the cutscene registering the data's patches again: the swap registers those new or changed, and the effect preview only what the data holds; a `[hidden]` rule the UA's already holds), and 3 can't change what the checks see (two assignments swapped with nothing between them; the note's colour, in no screen: the fixtures show valid entries; the host's sound, which only gives the document its audibility, seen only with sound on a device). The rerun (`step13-mutations-rerun.json`, made by `make-step13-rerun.py`: the 17, the 5 rewritten to build, and 5 for the imported files the data as edited serves and the loop's restart as it is now) killed all 27: the list as it stands, 183, has 180 killed and 3 accounted for. Nothing written to the player's real folders. Renders for the user:
`claude-work/m11/step13/render-{model,particles,cutscene}@{1,1.65}.png`. Found: the engine's sound effects have no stop
(a small task, Engine-Plan.md). Not checked: the preview in a real window and its sound by ear (to check by hand,
Engine-Plan.md, "By-hand checks", (c)); Windows.

**Fixes after step 14 (2026-11-08, the branch `m11-fixes-1` from `4f6120e`, merged as `00e5bd4`).** ("How it works",
"Fixes after step 14".) The manager's decisions on what step 14's flow found: New entry in the top bar between Save As and
Import, a command of its own (`EditorCommand.NewEntry`) with Ctrl+N, enabled while entries can be added; Enter in the
import dialog cancelling the key's default action; the empty "Stand-ins" heading hidden. All 19 editor screens captured
again (the top bar changed), each giving step 11's counts again. 8 new tests; the full suite passes on the GPU and on
lavapipe: 3493 tests. Mutations: 13, all killed.

**Step 14, the whole editor, end to end: done (2026-11-08, on the worker branch `m11-step14`, rebased on `00e5bd4`).**
("How it works", step 14.) A mod made from scratch through the UI alone (New mod and its info; a game entry changed in
the mod; a sound effect by New entry's Ctrl+N, a particle effect by its button, both filled in the form; a stage imported
from a test `.glb` and a readme from a `.txt` through Import and the file browser; a cutscene by New entry using the three,
its references picked from the reference lists; Save refused for a camera node typed wrong, the problem fixed, saved),
mounted as the game mounts mods over its packs and read by `Sequence.Load`, `ParticleEffect.Load`, `MusicLibrary.Load`
and `ModelData.Load`, each value the one the editor showed (binary form and JSON tab), the mod's hall over the game's;
opened again equal. A scratch copy of the game's `audio.pak` edited in the dev setup (`--repo` read by the program's own
arguments): the harp's level changed and saved in place, every other entry's stored bytes kept, the dump changed only at
the harp (in the test; `textconv.sh` itself run by hand once, the manager's decision of 2026-11-08: the diff is the
harp's level, stored length and hash). Every editor screen against Chromium at 1 and 1.65 on the GPU and on lavapipe:
all 19 within the limit (worst `checks-problems@1.65`, 0.187 % and 0.188 %), nothing captured again. **Found, and fixed
apart before the step went on** (the manager's decisions of 2026-11-08, "Fixes after step 14"): nothing in the UI started
New entry (P15); Enter in the import dialog opened the file browser again; the empty "Stand-ins" heading. Tests: 3
(`Tests/Statventure.Tests/Editor/EndToEnd/`: `ModFromScratchTests`, `DevSetupTests`, and `FoundTests`, the import
dialog's Enter as the end-to-end witness). The full suite passes on the GPU and on lavapipe, headless: 3496 tests
(`main`'s 3493 and 3 new), none skipped; nothing written to the player's real folders, the repo's packs unchanged.
Mutations (`~/.cache/stat-fick-build/claude-work/m11/step14-mutations.json` and `-rerun.json`, made by
`step14/make-mutations.py` and `make-rerun.py`): 30 in the first run, 25 killed, 5 survived (1 gap closed by the flow
importing a loose file, 1 malformed mutation rewritten, 3 that can't differ on what the flow saves); the rerun of 3 killed
all; the list as it stands, 31, has 28 killed and 3 accounted for. Renders for the user: `claude-work/m11/step14/`
`render-cutscene@{1,1.65}.png` and the mod itself, `ember-rite.pak`. Not checked: the editor in a real window and its
sound by ear (the by-hand checks, (a)–(e) above), Windows ("Windows check", items 1 and 8).
